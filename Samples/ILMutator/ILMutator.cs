//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.IO;
using Microsoft.Cci;
using Microsoft.Cci.MetadataReader;
using Microsoft.Cci.MutableCodeModel;
using System.Collections.Generic;
using System.Runtime.Serialization; // needed for defining exception .ctors

namespace ILMutator {

  class Program {
    static int Main(string[] args) {
      if (args == null || args.Length < 1) {
        Console.WriteLine("Usage: ILMutator <assembly> [<outputPath>]");
        return 1;
      }

      var host = new PeReader.DefaultHost();

      IModule/*?*/ module = host.LoadUnitFrom(args[0]) as IModule;
      if (module == null || module == Dummy.Module || module == Dummy.Assembly) {
        Console.WriteLine(args[0] + " is not a PE file containing a CLR assembly, or an error occurred when loading it.");
        return 1;
      }

      PdbReader/*?*/ pdbReader = null;
      string pdbFile = Path.ChangeExtension(module.Location, "pdb");
      if (File.Exists(pdbFile)) {
        Stream pdbStream = File.OpenRead(pdbFile);
        pdbReader = new PdbReader(pdbStream, host);
      } else {
        Console.WriteLine("Could not load the PDB file for '" + module.Name.Value + "' . Proceeding anyway.");
      }

      ILMutator mutator = new ILMutator(host, pdbReader);
      module = mutator.Visit(module);

      string outputPath;
      if (args.Length == 2)
        outputPath = args[1];
      else
        outputPath = module.Location + ".pe";

      var outputFileName = Path.GetFileNameWithoutExtension(outputPath);

      // Need to not pass in a local scope provider until such time as we have one that will use the mutator
      // to remap things (like the type of a scope constant) from the original assembly to the mutated one.
      using (var pdbWriter = new PdbWriter(outputFileName + ".pdb", pdbReader)) {
        PeWriter.WritePeToStream(module, host, File.Create(outputPath), pdbReader, null, pdbWriter);
      }
      return 0; // success
    }
  }

  /// <summary>
  /// A mutator that modifies method bodies at the IL level.
  /// It injects a call to Console.WriteLine for each store
  /// to a local for which the PDB reader is able to provide a name.
  /// This is meant to distinguish programmer-defined locals from
  /// those introduced by the compiler.
  /// </summary>
  public class ILMutator : MetadataMutator {

    PdbReader/*?*/ pdbReader = null;
    IMethodReference consoleDotWriteLine;

    public ILMutator(IMetadataHost host, PdbReader/*?*/ pdbReader)
      : base(host, true) {
      this.pdbReader = pdbReader;
      #region Get reference to Console.WriteLine
      var nameTable = host.NameTable;
      var platformType = host.PlatformType;
      var systemString = platformType.SystemString;
      var systemVoid = platformType.SystemVoid;
      var SystemDotConsoleType =
        new Microsoft.Cci.NamespaceTypeReference(
          host,
          systemString.ContainingUnitNamespace,
          nameTable.GetNameFor("Console"),
          0, false, false, PrimitiveTypeCode.NotPrimitive);
      this.consoleDotWriteLine = new Microsoft.Cci.MethodReference(
        this.host, SystemDotConsoleType,
        CallingConvention.Default,
        systemVoid,
        nameTable.GetNameFor("WriteLine"),
        0, systemString);
      #endregion Get reference to Console.WriteLine
    }

    List<ILocalDefinition> currentLocals;
    public override MethodBody Visit(MethodBody methodBody) {
      IMethodDefinition currentMethod = this.GetCurrentMethod();
      methodBody.MethodDefinition = currentMethod;
      this.currentLocals = new List<ILocalDefinition>(methodBody.LocalVariables);

      try {
        ProcessOperations(methodBody);
      } catch (ILMutatorException) {
        Console.WriteLine("Internal error during IL mutation for the method '{0}'.",
          MemberHelper.GetMemberSignature(currentMethod, NameFormattingOptions.SmartTypeName));
      } finally {
        this.currentLocals = null;
      }

      return methodBody;
    }

    private void ProcessOperations(MethodBody methodBody) {

      List<IOperation> operations = methodBody.Operations;
      int count = methodBody.Operations.Count;

      ILGenerator generator = new ILGenerator(this.host);

      var methodName = MemberHelper.GetMemberSignature(methodBody.MethodDefinition, NameFormattingOptions.SmartTypeName);

      #region Record all offsets that appear as part of an exception handler
      Dictionary<uint, bool> offsetsUsedInExceptionInformation = new Dictionary<uint, bool>();
      foreach (var exceptionInfo in methodBody.OperationExceptionInformation) {
        uint x = exceptionInfo.TryStartOffset;
        if (!offsetsUsedInExceptionInformation.ContainsKey(x)) offsetsUsedInExceptionInformation.Add(x, true);
        x = exceptionInfo.TryEndOffset;
        if (!offsetsUsedInExceptionInformation.ContainsKey(x)) offsetsUsedInExceptionInformation.Add(x, true);
        x = exceptionInfo.HandlerStartOffset;
        if (!offsetsUsedInExceptionInformation.ContainsKey(x)) offsetsUsedInExceptionInformation.Add(x, true);
        x = exceptionInfo.HandlerEndOffset;
        if (!offsetsUsedInExceptionInformation.ContainsKey(x)) offsetsUsedInExceptionInformation.Add(x, true);
        if (exceptionInfo.HandlerKind == HandlerKind.Filter) {
          x = exceptionInfo.FilterDecisionStartOffset;
          if (!offsetsUsedInExceptionInformation.ContainsKey(x)) offsetsUsedInExceptionInformation.Add(x, true);
        }
      }
      #endregion Record all offsets that appear as part of an exception handler

      Dictionary<uint, ILGeneratorLabel> offset2Label = new Dictionary<uint, ILGeneratorLabel>();
      #region Pass 1: Make a label for each branch target
      for (int i = 0; i < count; i++) {
        IOperation op = operations[i];
        switch (op.OperationCode) {
          case OperationCode.Beq:
          case OperationCode.Bge:
          case OperationCode.Bge_Un:
          case OperationCode.Bgt:
          case OperationCode.Bgt_Un:
          case OperationCode.Ble:
          case OperationCode.Ble_Un:
          case OperationCode.Blt:
          case OperationCode.Blt_Un:
          case OperationCode.Bne_Un:
          case OperationCode.Br:
          case OperationCode.Brfalse:
          case OperationCode.Brtrue:
          case OperationCode.Leave:
          case OperationCode.Beq_S:
          case OperationCode.Bge_S:
          case OperationCode.Bge_Un_S:
          case OperationCode.Bgt_S:
          case OperationCode.Bgt_Un_S:
          case OperationCode.Ble_S:
          case OperationCode.Ble_Un_S:
          case OperationCode.Blt_S:
          case OperationCode.Blt_Un_S:
          case OperationCode.Bne_Un_S:
          case OperationCode.Br_S:
          case OperationCode.Brfalse_S:
          case OperationCode.Brtrue_S:
          case OperationCode.Leave_S:
            uint x = (uint)op.Value;
            if (!offset2Label.ContainsKey(x))
              offset2Label.Add(x, new ILGeneratorLabel());
            break;
          case OperationCode.Switch:
            uint[] offsets = op.Value as uint[];
            foreach (var offset in offsets) {
              if (!offset2Label.ContainsKey(offset))
                offset2Label.Add(offset, new ILGeneratorLabel());
            }
            break;
          default:
            break;
        }
      }
      #endregion Pass 1: Make a label for each branch target

      #region Pass 2: Emit each operation, along with labels
      for (int i = 0; i < count; i++) {
        IOperation op = operations[i];
        ILGeneratorLabel label;
        if (op.Location is IILLocation) {
          generator.MarkSequencePoint(op.Location);
        }
        #region Mark operation if it is a label for a branch
        if (offset2Label.TryGetValue(op.Offset, out label)) {
          generator.MarkLabel(label);
        }
        #endregion Mark operation if it is a label for a branch

        #region Mark operation if it is pointed to by an exception handler
        bool ignore;
        uint offset = op.Offset;
        if (offsetsUsedInExceptionInformation.TryGetValue(offset, out ignore)) {
          foreach (var exceptionInfo in methodBody.OperationExceptionInformation) {
            if (offset == exceptionInfo.TryStartOffset)
              generator.BeginTryBody();

            // Never need to do anthing when offset == exceptionInfo.TryEndOffset because
            // we pick up an EndTryBody from the HandlerEndOffset below
            //  generator.EndTryBody();

            if (offset == exceptionInfo.HandlerStartOffset) {
              switch (exceptionInfo.HandlerKind) {
                case HandlerKind.Catch:
                  generator.BeginCatchBlock(exceptionInfo.ExceptionType);
                  break;
                case HandlerKind.Fault:
                  generator.BeginFaultBlock();
                  break;
                case HandlerKind.Filter:
                  generator.BeginFilterBody();
                  break;
                case HandlerKind.Finally:
                  generator.BeginFinallyBlock();
                  break;
              }
            }
            if (exceptionInfo.HandlerKind == HandlerKind.Filter && offset == exceptionInfo.FilterDecisionStartOffset) {
              generator.BeginFilterBlock();
            }
            if (offset == exceptionInfo.HandlerEndOffset)
              generator.EndTryBody();
          }
        }
        #endregion Mark operation if it is pointed to by an exception handler

        #region Emit operation along with any injection
        switch (op.OperationCode) {
          #region Branches
          case OperationCode.Beq:
          case OperationCode.Bge:
          case OperationCode.Bge_Un:
          case OperationCode.Bgt:
          case OperationCode.Bgt_Un:
          case OperationCode.Ble:
          case OperationCode.Ble_Un:
          case OperationCode.Blt:
          case OperationCode.Blt_Un:
          case OperationCode.Bne_Un:
          case OperationCode.Br:
          case OperationCode.Brfalse:
          case OperationCode.Brtrue:
          case OperationCode.Leave:
          case OperationCode.Beq_S:
          case OperationCode.Bge_S:
          case OperationCode.Bge_Un_S:
          case OperationCode.Bgt_S:
          case OperationCode.Bgt_Un_S:
          case OperationCode.Ble_S:
          case OperationCode.Ble_Un_S:
          case OperationCode.Blt_S:
          case OperationCode.Blt_Un_S:
          case OperationCode.Bne_Un_S:
          case OperationCode.Br_S:
          case OperationCode.Brfalse_S:
          case OperationCode.Brtrue_S:
          case OperationCode.Leave_S:
            generator.Emit(ILGenerator.LongVersionOf(op.OperationCode), offset2Label[(uint)op.Value]);
            break;
          case OperationCode.Switch:
            uint[] offsets = op.Value as uint[];
            ILGeneratorLabel[] labels = new ILGeneratorLabel[offsets.Length];
            for (int j = 0, n = offsets.Length; j < n; j++) {
              labels[j] = offset2Label[offsets[j]];
            }
            generator.Emit(OperationCode.Switch, labels);
            break;
          #endregion Branches
          #region Everything else
          case OperationCode.Stloc:
          case OperationCode.Stloc_0:
          case OperationCode.Stloc_1:
          case OperationCode.Stloc_2:
          case OperationCode.Stloc_3:
          case OperationCode.Stloc_S:
            EmitStoreLocal(generator, op);
            break;
          default:
            if (op.Value == null) {
              generator.Emit(op.OperationCode);
              break;
            }
            var typeCode = System.Convert.GetTypeCode(op.Value);
            switch (typeCode) {
              case TypeCode.Byte:
                generator.Emit(op.OperationCode, (byte)op.Value);
                break;
              case TypeCode.Double:
                generator.Emit(op.OperationCode, (double)op.Value);
                break;
              case TypeCode.Int16:
                generator.Emit(op.OperationCode, (short)op.Value);
                break;
              case TypeCode.Int32:
                generator.Emit(op.OperationCode, (int)op.Value);
                break;
              case TypeCode.Int64:
                generator.Emit(op.OperationCode, (long)op.Value);
                break;
              case TypeCode.Object:
                IFieldReference fieldReference = op.Value as IFieldReference;
                if (fieldReference != null) {
                  generator.Emit(op.OperationCode, this.Visit(fieldReference));
                  break;
                }
                ILocalDefinition localDefinition = op.Value as ILocalDefinition;
                if (localDefinition != null) {
                  generator.Emit(op.OperationCode, localDefinition);
                  break;
                }
                IMethodReference methodReference = op.Value as IMethodReference;
                if (methodReference != null) {
                  generator.Emit(op.OperationCode, this.Visit(methodReference));
                  break;
                }
                IParameterDefinition parameterDefinition = op.Value as IParameterDefinition;
                if (parameterDefinition != null) {
                  generator.Emit(op.OperationCode, parameterDefinition);
                  break;
                }
                ISignature signature = op.Value as ISignature;
                if (signature != null) {
                  generator.Emit(op.OperationCode, signature);
                  break;
                }
                ITypeReference typeReference = op.Value as ITypeReference;
                if (typeReference != null) {
                  generator.Emit(op.OperationCode, this.Visit(typeReference));
                  break;
                }
                throw new ILMutatorException("Should never get here: no other IOperation argument types should exist");
              case TypeCode.SByte:
                generator.Emit(op.OperationCode, (sbyte)op.Value);
                break;
              case TypeCode.Single:
                generator.Emit(op.OperationCode, (float)op.Value);
                break;
              case TypeCode.String:
                generator.Emit(op.OperationCode, (string)op.Value);
                break;
              default:
                // The other cases are the other enum values that TypeCode has.
                // But no other argument types should be in the Operations. ILGenerator cannot handle anything else,
                // so such IOperations should never exist.
                //case TypeCode.Boolean:
                //case TypeCode.Char:
                //case TypeCode.DateTime:
                //case TypeCode.DBNull:
                //case TypeCode.Decimal:
                //case TypeCode.Empty: // this would be the value for null, but the case when op.Value is null is handled before the switch statement
                //case TypeCode.UInt16:
                //case TypeCode.UInt32:
                //case TypeCode.UInt64:
                throw new ILMutatorException("Should never get here: no other IOperation argument types should exist");
            }
            break;
          #endregion Everything else
        }
        #endregion Emit operation along with any injection

      }
      #endregion Pass 2: Emit each operation, along with labels

      #region Retrieve the operations (and the exception information) from the generator
      generator.AdjustBranchSizesToBestFit();
      methodBody.OperationExceptionInformation = new List<IOperationExceptionInformation>(generator.GetOperationExceptionInformation());
      methodBody.Operations = new List<IOperation>(generator.GetOperations());
      #endregion Retrieve the operations (and the exception information) from the generator
      methodBody.MaxStack++;

    }

    private void EmitStoreLocal(ILGenerator generator, IOperation op) {

      #region Emit: call Console.WriteLine("foo");
      //generator.Emit(OperationCode.Ldstr, "foo");
      //generator.Emit(OperationCode.Call, this.consoleDotWriteLine);
      #endregion Emit: call Console.WriteLine("foo");

      string localName;
      switch (op.OperationCode) {
        case OperationCode.Stloc:
        case OperationCode.Stloc_S:
          ILocalDefinition loc = op.Value as ILocalDefinition;
          if (loc == null) throw new ILMutatorException("Stloc operation found without a valid operand");
          if (TryGetLocalName(loc, out localName)) {
            generator.Emit(OperationCode.Ldstr, localName);
            generator.Emit(OperationCode.Call, this.consoleDotWriteLine);
          }
          generator.Emit(op.OperationCode, loc);
          break;

        case OperationCode.Stloc_0:
          if (this.currentLocals.Count < 1)
            throw new ILMutatorException("stloc.0 operation found but no corresponding local in method body");
          if (TryGetLocalName(this.currentLocals[0], out localName)) {
            generator.Emit(OperationCode.Ldstr, localName);
            generator.Emit(OperationCode.Call, this.consoleDotWriteLine);
          }
          generator.Emit(op.OperationCode);
          break;

        case OperationCode.Stloc_1:
          if (this.currentLocals.Count < 2)
            throw new ILMutatorException("stloc.1 operation found but no corresponding local in method body");
          if (TryGetLocalName(this.currentLocals[1], out localName)) {
            generator.Emit(OperationCode.Ldstr, localName);
            generator.Emit(OperationCode.Call, this.consoleDotWriteLine);
          }
          generator.Emit(op.OperationCode);
          break;

        case OperationCode.Stloc_2:
          if (this.currentLocals.Count < 3)
            throw new ILMutatorException("stloc.2 operation found but no corresponding local in method body");
          if (TryGetLocalName(this.currentLocals[2], out localName)) {
            generator.Emit(OperationCode.Ldstr, localName);
            generator.Emit(OperationCode.Call, this.consoleDotWriteLine);
          }
          generator.Emit(op.OperationCode);
          break;

        case OperationCode.Stloc_3:
          if (this.currentLocals.Count < 4)
            throw new ILMutatorException("stloc.3 operation found but no corresponding local in method body");
          if (TryGetLocalName(this.currentLocals[3], out localName)) {
            generator.Emit(OperationCode.Ldstr, localName);
            generator.Emit(OperationCode.Call, this.consoleDotWriteLine);
          }
          generator.Emit(op.OperationCode);
          break;

        default:
          throw new ILMutatorException("Should never get here: switch statement was meant to be exhaustive");
      }
    }

    private bool TryGetLocalName(ILocalDefinition local, out string localNameFromPDB) {
      string localName = local.Name.Value;
      localNameFromPDB = null;
      if (this.pdbReader != null) {
        foreach (IPrimarySourceLocation psloc in this.pdbReader.GetPrimarySourceLocationsForDefinitionOf(local)) {
          if (psloc.Source.Length > 0) {
            localNameFromPDB = psloc.Source;
            break;
          }
        }
      }
      return localNameFromPDB != null;
    }

    /// <summary>
    /// Exceptions thrown during extraction. Should not escape this class.
    /// </summary>
    private class ILMutatorException : Exception {
      /// <summary>
      /// Exception specific to an error occurring in the contract extractor
      /// </summary>
      public ILMutatorException() { }
      /// <summary>
      /// Exception specific to an error occurring in the contract extractor
      /// </summary>
      public ILMutatorException(string s) : base(s) { }
      /// <summary>
      /// Exception specific to an error occurring in the contract extractor
      /// </summary>
      public ILMutatorException(string s, Exception inner) : base(s, inner) { }
      /// <summary>
      /// Exception specific to an error occurring in the contract extractor
      /// </summary>
      public ILMutatorException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
  }

}
