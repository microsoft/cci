using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Cci;
using System.IO;
using Microsoft.Cci.MutableCodeModel;
using System.Diagnostics.Contracts;
using ILGarbageCollect.Mark;

namespace ILGarbageCollect.Sweep {
  internal abstract class StubMethodBodyEmitter {
    protected readonly IMetadataHost host;

    protected readonly IAssemblyReference coreAssemblyReference;

    public StubMethodBodyEmitter(IMetadataHost host) {
      this.host = host;

      this.coreAssemblyReference = new Microsoft.Cci.Immutable.AssemblyReference(host, host.CoreAssemblySymbolicIdentity);
    }

    public abstract ILGenerator DebugReplacementGeneratorForBody(IMethodBody methodBody);

    public virtual ILGenerator MinimalReplacementGeneratorForBody(IMethodBody methodBody) {
      var generator = new ILGenerator(host, methodBody.MethodDefinition);

      AppendEmitExceptionThrow(generator);

      return generator;
    }



    protected void AppendEmitExceptionThrow(ILGenerator generator) {
      var systemExceptionTypeReference = GarbageCollectHelper.CreateTypeReference(host, coreAssemblyReference, "System.Exception");

      IMethodReference exceptionConstructor = new Microsoft.Cci.MethodReference(
        host,
        systemExceptionTypeReference,
        CallingConvention.HasThis,
        host.PlatformType.SystemVoid,
        host.NameTable.Ctor,
        0);

      generator.Emit(OperationCode.Newobj, exceptionConstructor);
      generator.Emit(OperationCode.Throw);
    }

  }

  internal class DotNetDesktopStubMethodBodyEmitter : StubMethodBodyEmitter {
    private readonly IMethodReference consoleWriteLine;
    private readonly IMethodReference environmentGetStackTrace;
    private readonly IMethodReference environmentExit;



    public DotNetDesktopStubMethodBodyEmitter(IMetadataHost host)
      : base(host) {

      this.consoleWriteLine = new Microsoft.Cci.MethodReference(host,
        GarbageCollectHelper.CreateTypeReference(host, coreAssemblyReference, "System.Console"),
        CallingConvention.Default,
        host.PlatformType.SystemVoid,
        host.NameTable.GetNameFor("WriteLine"),
        0,
        host.PlatformType.SystemString);

      this.environmentGetStackTrace = new Microsoft.Cci.MethodReference(host,
        GarbageCollectHelper.CreateTypeReference(host, coreAssemblyReference, "System.Environment"),
        CallingConvention.Default,
        host.PlatformType.SystemString,
        host.NameTable.GetNameFor("get_StackTrace"),
        0);

      this.environmentExit = new Microsoft.Cci.MethodReference(host,
        GarbageCollectHelper.CreateTypeReference(host, coreAssemblyReference, "System.Environment"),
        CallingConvention.Default,
        host.PlatformType.SystemVoid,
        host.NameTable.GetNameFor("Exit"),
        0,
        host.PlatformType.SystemInt32);
    }

    public override ILGenerator DebugReplacementGeneratorForBody(IMethodBody methodBody) {
      string warningText = "Attempt to execute garbage collected method: " + methodBody.MethodDefinition.ToString();

      var generator = new ILGenerator(host, methodBody.MethodDefinition);

      // emit console warning
      generator.Emit(OperationCode.Ldstr, warningText);
      generator.Emit(OperationCode.Call, this.consoleWriteLine);

      //emit stack trace

      // pushes stack trace on stack
      generator.Emit(OperationCode.Call, this.environmentGetStackTrace);
      // consumes stack trace
      generator.Emit(OperationCode.Call, this.consoleWriteLine);

      // may want to flush output?

      // emit exit

      generator.Emit(OperationCode.Ldc_I4_M1);
      generator.Emit(OperationCode.Call, this.environmentExit);

      // Makes the verifier happy; this should never be reached

      AppendEmitExceptionThrow(generator);

      return generator;
    }
  }

  internal class WindowsPhoneStubMethodBodyEmitter : StubMethodBodyEmitter {


    private readonly IMethodReference consoleWriteLine;

    private readonly IMethodReference stackTraceConstructor;

    private readonly IMethodReference toString;

    public WindowsPhoneStubMethodBodyEmitter(IMetadataHost host)
      : base(host) {

      this.consoleWriteLine = new Microsoft.Cci.MethodReference(host,
        GarbageCollectHelper.CreateTypeReference(host, coreAssemblyReference, "System.Console"),
        CallingConvention.Default,
        host.PlatformType.SystemVoid,
        host.NameTable.GetNameFor("WriteLine"),
        0,
        host.PlatformType.SystemString);

      this.stackTraceConstructor = new Microsoft.Cci.MethodReference(host,
          GarbageCollectHelper.CreateTypeReference(host, coreAssemblyReference, "System.Diagnostics.StackTrace"),
          CallingConvention.HasThis,
          host.PlatformType.SystemVoid,
          host.NameTable.GetNameFor(".ctor"),
          0);

      this.toString = new Microsoft.Cci.MethodReference(host,
            host.PlatformType.SystemObject,
            CallingConvention.HasThis,
            host.PlatformType.SystemString,
            host.NameTable.GetNameFor("ToString"),
            0);
    }

    public override ILGenerator DebugReplacementGeneratorForBody(IMethodBody methodBody) {

      var generator = new ILGenerator(host, methodBody.MethodDefinition);


      string warningText = "\n\n!!!!!!!!!!\nAttempt to execute garbage collected method: " + methodBody.MethodDefinition.ToString() + "\n\n";

      // emit console warning
      generator.Emit(OperationCode.Ldstr, warningText);
      generator.Emit(OperationCode.Call, this.consoleWriteLine);

      // emit stack trace
      generator.Emit(OperationCode.Newobj, this.stackTraceConstructor);
      generator.Emit(OperationCode.Callvirt, this.toString);
      generator.Emit(OperationCode.Call, this.consoleWriteLine);

      // This will actually exit the app, assuming it is not caught.
      // Apparently there is no programmatic Exit(-1) in SilverLight for Windows Phone. Hmm.

      AppendEmitExceptionThrow(generator);

      return generator;
    }
  }

  internal class TreeShakingRewriter : MetadataRewriter {
    private readonly AssemblyReport analysisReport;


    private readonly StubMethodBodyEmitter stubMethodBodyEmitter;

    private readonly bool removeMethodsWhenPossible;

    private readonly bool fullDebugStubs;

    private readonly bool dryRun;

    internal TreeShakingRewriter(IMetadataHost host, AssemblyReport analysisReport, bool dryRun, bool removeMethods, bool debugStubs, StubMethodBodyEmitter stubEmitter)
      : base(host) {
      this.analysisReport = analysisReport;
      this.removeMethodsWhenPossible = removeMethods;
      this.fullDebugStubs = debugStubs;
      this.dryRun = dryRun;

      this.stubMethodBodyEmitter = stubEmitter;
    }

    /// <summary>
    /// Replace each method that is not reachable with opcodes to throw an exception.
    /// </summary>
    public override void RewriteChildren(MethodBody methodBody) {

      IMethodDefinition methodDefinition = methodBody.MethodDefinition;


      Contract.Assert(analysisReport.ReachableMethods.Contains(methodDefinition) || analysisReport.UnreachableMethods.Contains(methodDefinition));

      if (!dryRun && analysisReport.ReachableMethods.Contains(methodBody.MethodDefinition) == false) {
        ILGenerator generator;

        if (this.fullDebugStubs) {
          // We'll leave helpful stubs around that emit debug info when called


          //Console.WriteLine("removing: " + methodBody.MethodDefinition);

          // create an ILGenerator to build up IL for this method that 
          // emits a warning to the console and then throws an exception

          generator = stubMethodBodyEmitter.DebugReplacementGeneratorForBody(methodBody);
        }
        else {
          // We'll leave a throw to make the verifer happy

          generator = stubMethodBodyEmitter.MinimalReplacementGeneratorForBody(methodBody);
        }



        // replace all of the IL and exception information
        methodBody.Operations = new List<IOperation>(generator.GetOperations());
        methodBody.OperationExceptionInformation = new List<IOperationExceptionInformation>(generator.GetOperationExceptionInformation());
      }
      base.RewriteChildren(methodBody);
    }

    public override List<IFieldDefinition> Rewrite(List<IFieldDefinition> fields) {
      if (!dryRun) {
        if (fields != null) {
          List<IFieldDefinition> fieldsToRemove = new List<IFieldDefinition>();

          foreach (var field in fields) {
            ITypeDefinition containingType = field.ContainingTypeDefinition;

            if (containingType.IsEnum == false && containingType.IsStruct == false) {
              if (!analysisReport.ReachableFields.Contains(field)) {
                fieldsToRemove.Add(field);
              }
            }
          }

          foreach (IFieldDefinition fieldToRemove in fieldsToRemove) {
            //Console.WriteLine("Removing field {0}", fieldToRemove);
            fields.Remove(fieldToRemove);
          }
        }
      }

      return base.Rewrite(fields);
    }


    private static bool MethodImplementsInterface(IMethodDefinition method) {
      if (MemberHelper.GetImplicitlyImplementedInterfaceMethods(method).Count() > 0) {
        return true;
      }

      foreach (IMethodImplementation methodImplementation in method.ContainingTypeDefinition.ExplicitImplementationOverrides) {
        if (methodImplementation.ImplementingMethod.ResolvedMethod.InternedKey == method.InternedKey) {
          return true;
        }
      }

      return false;
    }

    private static bool MethodOverridesAbstractMethod(IMethodDefinition method) {
      IMethodDefinition overridenBaseClassMethod = MemberHelper.GetImplicitlyOverriddenBaseClassMethod(method);

      return (!(overridenBaseClassMethod is Dummy) && overridenBaseClassMethod.IsAbstract);
    }

    private static bool MethodIsPropertyAccessor(IMethodDefinition method) {
      string methodName = method.Name.Value;

      // We could be more principled here.
      return methodName.StartsWith("get_") || methodName.StartsWith("set_");
    }

    private static bool MethodIsEventAccessor(IMethodDefinition method) {
      string methodName = method.Name.Value;

      // We could be more principled here also.
      return methodName.StartsWith("add_") || methodName.StartsWith("remove_");
    }

    private static bool MethodIsCustomAttributeConstructor(IMethodDefinition methodDefinition) {
      if (methodDefinition.IsConstructor) {

        foreach (ITypeDefinition superClass in GarbageCollectHelper.AllSuperClasses(methodDefinition.ContainingTypeDefinition)) {
          if (superClass is INamedTypeDefinition) {
            // t-devinc: Could be more principled here, for sure
            string typeName = superClass.ToString();
            if (typeName.Equals("System.Attribute")) {
              return true;
            }
          }
        }
      }

      return false;
    }

    private static bool IsSafeToRemoveMethod(IMethodDefinition method) {

      return !method.IsVirtual &&  /* This is quite conservative -- could check RTA's virtual methods in demand here. */
             !method.IsAbstract &&
             !method.IsExternal &&
             !MethodIsPropertyAccessor(method) &&
             !MethodIsEventAccessor(method) &&
             !MethodImplementsInterface(method) &&
             !MethodOverridesAbstractMethod(method) &&
             !MethodIsCustomAttributeConstructor(method);  /* This is also probably too conservative */

    }

    public override List<IMethodDefinition> Rewrite(List<IMethodDefinition> methods) {
      if (!dryRun) {
        if (methods != null && this.removeMethodsWhenPossible) {
          List<IMethodDefinition> methodsToRemove = new List<IMethodDefinition>();

          foreach (var method in methods) {
            if (IsSafeToRemoveMethod(method) && !analysisReport.ReachableMethods.Contains(method)) {
              methodsToRemove.Add(method);
            }
          }

          foreach (IMethodDefinition methodToRemove in methodsToRemove) {
            //Console.WriteLine("Removing method {0} entirely", methodToRemove);
            methods.Remove(methodToRemove);
          }
        }
      }

      return base.Rewrite(methods);
    }
  }

}
