using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Cci;
using Microsoft.Cci.MutableCodeModel;
using System.IO;
using System.Diagnostics.Contracts;

namespace ILGarbageCollect.Instrumentation {

  class AddGCWaitForFinalization : ILMethodBodyRewriter {
    internal AddGCWaitForFinalization(IMetadataHost host, MethodBody methodBody) : base(host, methodBody) {}

    protected override void RewriteReturn(IOperation op) {
      // get a reference to System.GC
      var systemGC = UnitHelper.FindType(
        base.host.NameTable,
        base.host.FindAssembly(host.CoreAssemblySymbolicIdentity),
        "System.GC"
      );

      Contract.Assert(systemGC != Dummy.Type);

      // store an IMethodReference to System.GC.WaitForPendingFinalizers()
      var systemGCWaitForPendingFinalizers = TypeHelper.GetMethod(
        systemGC,
        base.host.NameTable.GetNameFor("WaitForPendingFinalizers")
      );

      Contract.Assert(systemGCWaitForPendingFinalizers != null);
      Contract.Assert(systemGCWaitForPendingFinalizers != Dummy.Method);

      // call method.
      base.generator.Emit(OperationCode.Call, systemGCWaitForPendingFinalizers);

      base.RewriteReturn(op);
    }
  }

  class ShadowFieldRewriter : MetadataRewriter {
    internal ShadowFieldRewriter(IMetadataHost host) : base(host) { }
    public override List<IFieldDefinition> Rewrite(List<IFieldDefinition> fields) {
      if (fields != null) {
        var addedFields = new List<IFieldDefinition>();
        foreach (var field in fields) {
          if (field.IsStatic == false && field.ContainingType.IsEnum == false) {
            // private long fieldname$$storeCount; 
            var storeField = new FieldDefinition() {
              Name = this.host.NameTable.GetNameFor(MemberHelper.GetMemberSignature(field, NameFormattingOptions.None) + "$$storeCount"),
              Type = this.host.PlatformType.SystemInt64,
              InternFactory = base.host.InternFactory,
              ContainingTypeDefinition = field.ContainingTypeDefinition,
              Visibility = field.Visibility,
              IsStatic = false
            };

            var loadField = new FieldDefinition() {
              Name = this.host.NameTable.GetNameFor(MemberHelper.GetMemberSignature(field, NameFormattingOptions.None) + "$$loadCount"),
              Type = this.host.PlatformType.SystemInt64,
              InternFactory = base.host.InternFactory,
              ContainingTypeDefinition = field.ContainingTypeDefinition,
              Visibility = field.Visibility,
              IsStatic = false
            };

            addedFields.Add(storeField);
            addedFields.Add(loadField);
            Console.WriteLine("Added shadow field to : " + MemberHelper.GetMemberSignature(field, NameFormattingOptions.None));
          }
        }

        foreach (var field in addedFields) fields.Add(field);
      }
      return fields;
    }
  }

  class FinalizeMethodRewriter : MetadataRewriter {
    private class FinalizeWriter : ILMethodBodyRewriter {

      private readonly IMethodReference consoleDotWriteLineString;
      private readonly IMethodReference consoleDotWriteString;
      private readonly IMethodReference consoleDotWriteLong;

      internal FinalizeWriter(IMetadataHost host, MethodBody methodBody) : base(host, methodBody) {
        Contract.Requires(host != null);
        Contract.Requires(methodBody != null);
        Contract.Requires(methodBody != Dummy.Method);
        Contract.Requires(
          methodBody.Operations.Count > 0 &&
          Contract.Exists(methodBody.Operations, x => x.OperationCode == OperationCode.Ret)
        );

        // get a reference to System.Console
        var systemConsole = UnitHelper.FindType(
          base.host.NameTable,
          base.host.FindAssembly(host.CoreAssemblySymbolicIdentity),
          "System.Console"
        );

        // store an IMethodReference to System.Console.WriteLine(String)
        this.consoleDotWriteLineString = TypeHelper.GetMethod(
          systemConsole,
          base.host.NameTable.GetNameFor("WriteLine"),
          host.PlatformType.SystemString
        );

        // store an IMethodReference to System.Console.Write(String)
        this.consoleDotWriteString = TypeHelper.GetMethod(
          systemConsole,
          base.host.NameTable.GetNameFor("Write"),
          host.PlatformType.SystemString
        );


        // store an IMethodReference to System.Console.Write(long)
        this.consoleDotWriteLong = TypeHelper.GetMethod(
          systemConsole,
          base.host.NameTable.GetNameFor("Write"),
          host.PlatformType.SystemInt64
        );

      }

      protected override void RewriteReturn(IOperation op) {
        // if we are here we have already copied all of the other existing MSIL in this Finalize method
        base.generator.Emit(OperationCode.Nop);

        var className = base.methodBody.MethodDefinition.ContainingType.ToString();
        base.generator.Emit(OperationCode.Ldstr, className);
        base.generator.Emit(OperationCode.Call, this.consoleDotWriteString);

        foreach (var field in this.methodBody.MethodDefinition.ContainingTypeDefinition.Fields) {
          var fieldName = MemberHelper.GetMemberSignature(field, NameFormattingOptions.None);
          if (fieldName.Contains("$$loadCount") || fieldName.Contains("$$storeCount")) {
            Console.WriteLine("Finalizing: " + fieldName);
            base.generator.Emit(OperationCode.Ldstr, "\t " + fieldName + " ");
            base.generator.Emit(OperationCode.Call, this.consoleDotWriteString);
            base.generator.Emit(OperationCode.Ldarg_0);
            base.generator.Emit(OperationCode.Ldfld, field);
            base.generator.Emit(OperationCode.Call, this.consoleDotWriteLong);
          }
        }

        base.generator.Emit(OperationCode.Ldstr, "");
        base.generator.Emit(OperationCode.Call, this.consoleDotWriteLineString);

        // now copy the final return statement.
        base.RewriteReturn(op);
      }

      protected override void RewriteMaxStack(ushort maxstack) {
        ushort n = (ushort)(maxstack + 4U);
        base.RewriteMaxStack(n);
      }
    }

    internal FinalizeMethodRewriter(IMetadataHost host) : base(host) { }

    private static ITypeDefinition GetBaseClass(ITypeDefinition type) {
      Contract.Requires(type != null);
      Contract.Requires(type != Dummy.Type);
      Contract.Requires(type.BaseClasses != null);
      Contract.Ensures(Contract.Result<ITypeDefinition>() != null);
      Contract.Ensures(Contract.Result<ITypeDefinition>() != Dummy.Type);

      ITypeDefinition baseClass = null;
      foreach (var b in type.BaseClasses) {
        baseClass = b.ResolvedType;
        break;
      }

      return baseClass;
    }

    private IMethodDefinition GetOrCreateFinalizer(ITypeDefinition type) {
      Contract.Requires(type != null);
      Contract.Requires(type != Dummy.Type);
      Contract.Ensures(Contract.Result<IMethodDefinition>() != null);
      Contract.Ensures(Contract.Result<IMethodDefinition>() != Dummy.Method);

      var finalizer = TypeHelper.GetMethod(type, base.host.NameTable.GetNameFor("Finalize"));
      if (finalizer == Dummy.Method) {
        // create a method:  virtual void Finalize()
        finalizer = new MethodDefinition() {
          IsVirtual = true,
          CallingConvention = CallingConvention.HasThis,
          ContainingTypeDefinition = type,
          InternFactory = host.InternFactory,
          IsCil = true,
          IsStatic = false,
          Name = base.host.NameTable.GetNameFor("Finalize"),
          Type = host.PlatformType.SystemVoid,
          Visibility = TypeMemberVisibility.Family,
        };

        // create a list of operations that holds (i) call to baseclass finalize if there is one and (ii) a return statement.
        var ops = new List<IOperation>();

        // call base class Finalize method    
        var baseClass = GetBaseClass(type);
        var baseClassFinalize = new Microsoft.Cci.MethodReference(
          base.host,
          baseClass,
          CallingConvention.HasThis,
          base.host.PlatformType.SystemVoid,
          base.host.NameTable.GetNameFor("Finalize"),
          0
        );

        ops.Add(new Operation() {
          Location = Dummy.Location,
          Offset = 0,
          OperationCode = OperationCode.Ldarg_0,
          Value = null
        });

        ops.Add(new Operation() {
          Location = Dummy.Location,
          Offset = 0,
          OperationCode = OperationCode.Call,
          Value = baseClassFinalize
        });

        ops.Add(new Operation() {
          Location = Dummy.Location,
          Offset = 0,
          OperationCode = OperationCode.Ret,
          Value = null
        });

        // create a method body with that single operation and no exception information
        var body = new MethodBody {
          LocalsAreZeroed = false,
          LocalVariables = null,
          MaxStack = 0,
          MethodDefinition = finalizer,
          OperationExceptionInformation = new List<IOperationExceptionInformation>(0),
          Operations = ops,
          PrivateHelperTypes = null
        };

        // update the method definition and the body to point to each other.
        body.MethodDefinition = finalizer;
        ((MethodDefinition)finalizer).Body = body;

        // add it to this typeDefinition
        var containingType = type as NamedTypeDefinition;
        if (containingType != null) {
          if (containingType.Methods == null) containingType.Methods = new List<IMethodDefinition>();
          containingType.Methods.Add(finalizer);
        } else {
          var nestedContainingType = type as NestedTypeDefinition;
          if (nestedContainingType != null) {
            if (nestedContainingType.Methods == null) nestedContainingType.Methods = new List<IMethodDefinition>();
            nestedContainingType.Methods.Add(finalizer);
          } else {
            var specializedContainingType = type as SpecializedNestedTypeDefinition;
            if (specializedContainingType != null) {
              if (specializedContainingType.Methods == null) specializedContainingType.Methods = new List<IMethodDefinition>();
              specializedContainingType.Methods.Add(finalizer);
            } else {
              Contract.Assert(false);
            }
          }
        }

      }
      return finalizer;
    }

    public override void RewriteChildren(NamedTypeDefinition typeDefinition) {
      if (typeDefinition != null && (typeDefinition.IsClass)) {
        Console.WriteLine("Rewriting finalize method of " + typeDefinition);
        var finalizer = this.GetOrCreateFinalizer(typeDefinition);
        var finalizeWriter = new FinalizeWriter(base.host, finalizer.Body as MethodBody);
        finalizeWriter.Rewrite();
      }
      base.RewriteChildren(typeDefinition);
    }
  }

  class ShadowMethodRewriter : MetadataRewriter {
    internal ShadowMethodRewriter(IMetadataHost host) : base(host) { }

    private class ShadowMethodILRewriter : ILMethodBodyRewriter {
      internal ShadowMethodILRewriter(IMetadataHost host, MethodBody body) : base(host, body) { }

      protected override void RewriteLoadField(IOperation op) {
        var fieldReference = op.Value as IFieldReference;
        Contract.Assert(fieldReference != null);

        if (fieldReference.ContainingType.IsEnum == false) {
          var loadFieldCounterReference = new FieldReference() {
            Name = base.host.NameTable.GetNameFor(MemberHelper.GetMemberSignature(fieldReference, NameFormattingOptions.None) + "$$loadCount"),
            Type = base.host.PlatformType.SystemInt64,
            InternFactory = base.host.InternFactory,
            ContainingType = fieldReference.ContainingType,
            IsStatic = false
          };

          base.generator.Emit(OperationCode.Dup);  // load "this" onto stack
          base.generator.Emit(OperationCode.Dup); // load "this" onto stack
          base.generator.Emit(OperationCode.Ldfld, loadFieldCounterReference); // load field$$loadCount onto stack
          base.generator.Emit(OperationCode.Ldc_I4_1); // load 1 onto stack
          base.generator.Emit(OperationCode.Conv_I8);  // convert to int64
          base.generator.Emit(OperationCode.Add);      // add field$loadCount + 1            
          base.generator.Emit(OperationCode.Stfld, loadFieldCounterReference); // store result of add to field$$loadCount
        }
        // now do the actual ldfld
        base.RewriteLoadField(op);
      }

      protected override void RewriteStoreField(IOperation op) {

        var fieldReference = op.Value as IFieldReference;
        Contract.Assert(fieldReference != null);

        if (fieldReference.ContainingType.IsEnum == false) {
          var storeFieldCounter = new FieldReference() {
            Name = base.host.NameTable.GetNameFor(MemberHelper.GetMemberSignature(fieldReference, NameFormattingOptions.None) + "$$storeCount"),
            Type = base.host.PlatformType.SystemInt64,
            InternFactory = base.host.InternFactory,
            ContainingType = fieldReference.ContainingType,
            IsStatic = false
          };

          // save the variable that is on the top of stack
          var name = "XXX_" + fieldReference.Name.ToString();
          var def = new LocalDefinition {
            Name = base.host.NameTable.GetNameFor(name),
            Type = fieldReference.Type
          };
          if (base.methodBody.LocalVariables == null) base.methodBody.LocalVariables = new List<ILocalDefinition>(1);
          base.methodBody.LocalVariables.Add(def);

          // store top-of-stack into a local.  This is the value the stfld uses
          generator.Emit(OperationCode.Stloc, def);

          base.generator.Emit(OperationCode.Dup);  // load "this" onto stack
          base.generator.Emit(OperationCode.Dup); // load "this" onto stack
          base.generator.Emit(OperationCode.Ldfld, storeFieldCounter); // load field$$storeCount onto stack
          base.generator.Emit(OperationCode.Ldc_I4_1); // load 1 onto stack
          base.generator.Emit(OperationCode.Conv_I8);  // convert to int64
          base.generator.Emit(OperationCode.Add);      // add field$storeCount + 1            
          base.generator.Emit(OperationCode.Stfld, storeFieldCounter); // store result of add to field$$storeCount

          // restore the var we saved from the local
          generator.Emit(OperationCode.Ldloc, def);
        }

        // now do the original stfld
        base.RewriteStoreField(op);
      }


      protected override void RewriteLocalsAreZeroed(bool b) {
        base.RewriteLocalsAreZeroed(true);
      }

      protected override void RewriteMaxStack(ushort maxstack) {
        this.methodBody.MaxStack = (ushort)(maxstack + 4);
      }
    }

    public override void RewriteChildren(MethodBody methodBody) {
      if (methodBody.MethodDefinition.IsExternal == false && methodBody.MethodDefinition.IsCil) {
        var methodBodyRewriter = new ShadowMethodILRewriter(base.host, methodBody);
        methodBodyRewriter.Rewrite();
      }
      base.RewriteChildren(methodBody);
    }
  }

  class ZombieFieldInstrumentor {
    static void Main(string[] argv) {
      if (argv == null || argv.Length < 1) {
        Console.WriteLine("Usage: Main <assemblys> [<outputPath>]");
      }

      using (var host = new PeReader.DefaultHost()) {
        var module = host.LoadUnitFrom(argv[0]) as IModule;
        if (module == null || module == Dummy.Module || module == Dummy.Assembly) {
          throw new Exception(argv[0] + " is not a PE file containing a CLR assembly, or an error occurred when loading it.");
        } 

        PdbReader pdbReader = null;
        string pdbFile = Path.ChangeExtension(module.Location, "pdb");
        if (File.Exists(pdbFile)) {
          using (var pdbStream = File.OpenRead(pdbFile)) {
            pdbReader = new PdbReader(pdbStream, host);
          }
        } else
          Console.WriteLine("Could not load the PDB file for '" + module.Name.Value + "' . Proceeding anyway.");

        using (pdbReader) {

          var copy = new MetadataDeepCopier(host).Copy(module);
          var shadowFieldsAddedAssembly = new ShadowFieldRewriter(host).Rewrite(copy);
          var shadowFieldsAndMethodsAddedAssembly = new ShadowMethodRewriter(host).Rewrite(shadowFieldsAddedAssembly);
          var rewrittenAssembly = new FinalizeMethodRewriter(host).Rewrite(shadowFieldsAndMethodsAddedAssembly);

          var main = rewrittenAssembly.EntryPoint.ResolvedMethod;
          if (main != Dummy.Method) {
            var body = main.Body as MethodBody;
            if (body != null)
              new AddGCWaitForFinalization(host, body).Rewrite();
          }

          var validator = new MetadataValidator(host);
          validator.Validate(rewrittenAssembly as IAssembly);

          string outputPath = rewrittenAssembly.Location + ".meta";
          var outputFileName = Path.GetFileNameWithoutExtension(outputPath);

          // Need to not pass in a local scope provider until such time as we have one that will use the mutator
          // to remap things (like the type of a scope constant) from the original assembly to the mutated one.
          using (var peStream = File.Create(outputPath)) {
            using (var pdbWriter = new PdbWriter(outputFileName + ".pdb", pdbReader)) {
              PeWriter.WritePeToStream(rewrittenAssembly, host, peStream, pdbReader, null, pdbWriter);
            }
          }
        }
      }
    }
  }
}
