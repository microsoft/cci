using System.Collections.Generic;
using Microsoft.Cci;
using Microsoft.Cci.Analysis;
using Microsoft.Cci.MutableCodeModel;
using Microsoft.Cci.UtilityDataStructures;

namespace EdgeProfiler {
  internal class Instrumenter : MetadataRewriter {

    private Instrumenter(IMetadataHost host, PdbReader/*?*/ pdbReader, IMethodReference logger)
      : base(host) {
      this.logger = logger;
      this.pdbReader = pdbReader;
    }

    internal static IModule GetInstrumented(IMetadataHost host, IModule module, PdbReader/*?*/ pdbReader, INamespaceTypeDefinition logger) {
      var copier = new MetadataDeepCopier(host);
      var copy = copier.Copy(module);
      var loggerCopy = copier.Copy(logger);
      loggerCopy.ContainingUnitNamespace = copy.UnitNamespaceRoot;
      var logEdgeCount = TypeHelper.GetMethod(loggerCopy, host.NameTable.GetNameFor("LogEdgeCount"), host.PlatformType.SystemUInt32);
      new Instrumenter(host, pdbReader, logEdgeCount).Rewrite(copy);
      copy.AllTypes.Add(loggerCopy);
      return copy;
    }

    IMethodReference logger;
    PdbReader/*?*/ pdbReader;

    IEnumerator<ILocalScope>/*?*/ scopeEnumerator;
    bool scopeEnumeratorIsValid;
    Stack<ILocalScope> scopeStack = new Stack<ILocalScope>();

    ILGenerator ilGenerator;
    NamedTypeDefinition counterFieldsForCurrentMethod;
    List<uint> fieldOffsets;
    Hashtable<ILGeneratorLabel> labelFor;
    ControlAndDataFlowGraph<BasicBlock<Instruction>, Instruction> cdfg;
    List<MethodDefinition> dumperMethods = new List<MethodDefinition>();

    public override void RewriteChildren(Assembly assembly) {
      foreach (var t in assembly.AllTypes) this.Rewrite(t);
      assembly.EntryPoint = this.InjectNewEntryPoint(assembly.EntryPoint.ResolvedMethod);
    }

    public override IMethodBody Rewrite(IMethodBody methodBody) {
      this.cdfg = ControlAndDataFlowGraph<BasicBlock<Instruction>, Instruction>.GetControlAndDataFlowGraphFor(this.host, methodBody);
      this.ilGenerator = new ILGenerator(host, methodBody.MethodDefinition);

      var numberOfBlocks = this.cdfg.BlockFor.Count;
      this.labelFor = new Hashtable<ILGeneratorLabel>(numberOfBlocks);
      this.counterFieldsForCurrentMethod = new NestedTypeDefinition() {
        BaseClasses = new List<ITypeReference>(1) { this.host.PlatformType.SystemObject },
        ContainingTypeDefinition = methodBody.MethodDefinition.ContainingTypeDefinition,
        Fields = new List<IFieldDefinition>((int)numberOfBlocks*2),
        Methods = new List<IMethodDefinition>(1),
        InternFactory = this.host.InternFactory,
        IsBeforeFieldInit = true,
        IsClass = true,
        IsSealed = true,
        IsAbstract = true,
        Name = this.host.NameTable.GetNameFor(methodBody.MethodDefinition.Name+"_Counters"+methodBody.MethodDefinition.InternedKey),
        Visibility = TypeMemberVisibility.Assembly,
      };
      this.fieldOffsets = new List<uint>((int)numberOfBlocks*2);

      foreach (var exceptionInfo in methodBody.OperationExceptionInformation) {
        this.ilGenerator.AddExceptionHandlerInformation(exceptionInfo.HandlerKind, exceptionInfo.ExceptionType,
          this.GetLabelFor(exceptionInfo.TryStartOffset), this.GetLabelFor(exceptionInfo.TryEndOffset),
          this.GetLabelFor(exceptionInfo.HandlerStartOffset), this.GetLabelFor(exceptionInfo.HandlerEndOffset),
          exceptionInfo.HandlerKind == HandlerKind.Filter ? this.GetLabelFor(exceptionInfo.FilterDecisionStartOffset) : null);
      }

      if (this.pdbReader == null) {
        foreach (var localDef in methodBody.LocalVariables)
          this.ilGenerator.AddVariableToCurrentScope(localDef);
      } else {
        foreach (var ns in this.pdbReader.GetNamespaceScopes(methodBody)) {
          foreach (var uns in ns.UsedNamespaces)
            this.ilGenerator.UseNamespace(uns.NamespaceName.Value);
        }
        this.scopeEnumerator = this.pdbReader.GetLocalScopes(methodBody).GetEnumerator();
        this.scopeEnumeratorIsValid = this.scopeEnumerator.MoveNext();
      }

      foreach (var block in this.cdfg.AllBlocks)
        this.InstrumentBlock(block);

      while (this.scopeStack.Count > 0) {
        this.ilGenerator.EndScope();
        this.scopeStack.Pop();
      }

      this.ilGenerator.AdjustBranchSizesToBestFit();

      this.InjectMethodToDumpCounters();

      return new ILGeneratorMethodBody(this.ilGenerator, methodBody.LocalsAreZeroed, (ushort)(methodBody.MaxStack+2), methodBody.MethodDefinition,
        methodBody.LocalVariables, IteratorHelper.GetSingletonEnumerable((ITypeDefinition)this.counterFieldsForCurrentMethod));
    }

    private void InjectMethodToDumpCounters() {
      var dumper = new MethodDefinition() {
        ContainingTypeDefinition = this.counterFieldsForCurrentMethod,
        InternFactory = this.host.InternFactory,
        IsCil = true,
        IsStatic = true,
        Name = this.host.NameTable.GetNameFor("DumpCounters"),
        Type = this.host.PlatformType.SystemVoid,
        Visibility = TypeMemberVisibility.Public,
      };
      this.counterFieldsForCurrentMethod.Methods.Add(dumper);
      this.dumperMethods.Add(dumper);

      var ilGenerator = new ILGenerator(this.host, dumper);
      for (int i = 0, n = this.fieldOffsets.Count; i < n; i++) {
        ilGenerator.Emit(OperationCode.Ldsfld, this.counterFieldsForCurrentMethod.Fields[i]);
        ilGenerator.Emit(OperationCode.Call, this.logger);
      }
      ilGenerator.Emit(OperationCode.Ret);
      var body = new ILGeneratorMethodBody(ilGenerator, false, 2, dumper, Enumerable<ILocalDefinition>.Empty, Enumerable<ITypeDefinition>.Empty );
      dumper.Body = body;

    }

    private IMethodReference InjectNewEntryPoint(IMethodDefinition oldEntryPoint) {
      var containingType = (NamespaceTypeDefinition)oldEntryPoint.ContainingTypeDefinition;
      var entryPoint = new MethodDefinition() {
        ContainingTypeDefinition = containingType,
        InternFactory = this.host.InternFactory,
        IsCil = true,
        IsStatic = true,
        Name = this.host.NameTable.GetNameFor("InstrumentedMain"),
        Parameters = new List<IParameterDefinition>(oldEntryPoint.Parameters),
        Type = this.host.PlatformType.SystemVoid,
        Visibility = TypeMemberVisibility.Public,
      };
      containingType.Methods.Add(entryPoint);

      var ilGenerator = new ILGenerator(host, entryPoint);
      foreach (var par in entryPoint.Parameters) ilGenerator.Emit(OperationCode.Ldarg, par);
      ilGenerator.Emit(OperationCode.Call, oldEntryPoint);
      foreach (var dumper in this.dumperMethods) {
        ilGenerator.Emit(OperationCode.Call, dumper);
      }
      ilGenerator.Emit(OperationCode.Ret);

      var body = new ILGeneratorMethodBody(ilGenerator, true, (ushort)entryPoint.Parameters.Count, entryPoint,
        Enumerable<ILocalDefinition>.Empty, Enumerable<ITypeDefinition>.Empty);
      entryPoint.Body = body;
      return entryPoint;
    }

    private void EmitDebugInformationFor(IOperation operation) {
      this.ilGenerator.MarkSequencePoint(operation.Location);
      if (this.scopeEnumerator == null) return;
      ILocalScope/*?*/ currentScope = null;
      while (this.scopeStack.Count > 0) {
        currentScope = this.scopeStack.Peek();
        if (operation.Offset < currentScope.Offset+currentScope.Length) break;
        this.scopeStack.Pop();
        this.ilGenerator.EndScope();
        currentScope = null;
      }
      while (this.scopeEnumeratorIsValid) {
        currentScope = this.scopeEnumerator.Current;
        if (currentScope.Offset <= operation.Offset && operation.Offset < currentScope.Offset+currentScope.Length) {
          this.scopeStack.Push(currentScope);
          this.ilGenerator.BeginScope();
          foreach (var local in this.pdbReader.GetVariablesInScope(currentScope))
            this.ilGenerator.AddVariableToCurrentScope(local);
          foreach (var constant in this.pdbReader.GetConstantsInScope(currentScope))
            this.ilGenerator.AddConstantToCurrentScope(constant);
          this.scopeEnumeratorIsValid = this.scopeEnumerator.MoveNext();
        } else
          break;
      }
    }

    private void InstrumentBlock(BasicBlock<Instruction> block) {
      this.ilGenerator.MarkLabel(this.GetLabelFor(block.Offset));
      for (int i = 0, n = block.Instructions.Count-1; i < n; i++) {
        var operation = block.Instructions[i].Operation;
        this.EmitDebugInformationFor(operation);
        this.ilGenerator.Emit(operation.OperationCode, operation.Value);
      }

      var lastOperation = block.Instructions[block.Instructions.Count-1].Operation;
      this.EmitDebugInformationFor(lastOperation);
      switch (lastOperation.OperationCode) {
        case OperationCode.Beq:
        case OperationCode.Beq_S:
        case OperationCode.Bge:
        case OperationCode.Bge_S:
        case OperationCode.Bge_Un:
        case OperationCode.Bge_Un_S:
        case OperationCode.Bgt:
        case OperationCode.Bgt_S:
        case OperationCode.Bgt_Un:
        case OperationCode.Bgt_Un_S:
        case OperationCode.Ble:
        case OperationCode.Ble_S:
        case OperationCode.Ble_Un:
        case OperationCode.Ble_Un_S:
        case OperationCode.Blt:
        case OperationCode.Blt_S:
        case OperationCode.Blt_Un:
        case OperationCode.Blt_Un_S:
        case OperationCode.Bne_Un:
        case OperationCode.Bne_Un_S:
        case OperationCode.Brfalse:
        case OperationCode.Brfalse_S:
        case OperationCode.Brtrue:
        case OperationCode.Brtrue_S:
          var unconditionalBranch = new ILGeneratorLabel();
          var fallThrough = new ILGeneratorLabel();
          this.ilGenerator.Emit(lastOperation.OperationCode, unconditionalBranch);
          this.ilGenerator.Emit(OperationCode.Br_S, fallThrough);
          this.ilGenerator.MarkLabel(unconditionalBranch);
          this.EmitCounterIncrement(lastOperation.Offset);
          this.ilGenerator.Emit(OperationCode.Br, this.GetLabelFor((uint)lastOperation.Value));
          this.ilGenerator.MarkLabel(fallThrough);
          this.EmitCounterIncrement(lastOperation.Offset+1);
          break;

        case OperationCode.Br:
        case OperationCode.Br_S:
          this.EmitCounterIncrement(lastOperation.Offset);
          this.ilGenerator.Emit(OperationCode.Br, this.GetLabelFor((uint)lastOperation.Value));
          break;

        case OperationCode.Leave:
        case OperationCode.Leave_S:
          this.EmitCounterIncrement(lastOperation.Offset);
          this.ilGenerator.Emit(OperationCode.Leave, this.GetLabelFor((uint)lastOperation.Value));
          break;

        case OperationCode.Endfilter:
        case OperationCode.Endfinally:
        case OperationCode.Jmp:
        case OperationCode.Ret:
        case OperationCode.Rethrow:
        case OperationCode.Throw:
          //No need to count the outgoing edge. Its count is the same as the number of times this block executes.
          this.ilGenerator.Emit(lastOperation.OperationCode, lastOperation.Value);
          break;

        case OperationCode.Switch:
          fallThrough = new ILGeneratorLabel();
          uint[] targets = (uint[])lastOperation.Value;
          ILGeneratorLabel[] counters = new ILGeneratorLabel[targets.Length];
          for (int i = 0, n = counters.Length; i < n; i++) counters[i] = new ILGeneratorLabel();
          this.ilGenerator.Emit(OperationCode.Switch, counters);
          this.EmitCounterIncrement(lastOperation.Offset);
          this.ilGenerator.Emit(OperationCode.Br, fallThrough);
          for (int i = 0, n = counters.Length; i < n; i++) {
            var counterLabel = counters[i];
            this.ilGenerator.MarkLabel(counterLabel);
            this.EmitCounterIncrement(lastOperation.Offset+1+(uint)i);
            this.ilGenerator.Emit(OperationCode.Br, this.GetLabelFor(targets[i]));
          }
          this.ilGenerator.MarkLabel(fallThrough);
          break;

        default:
          this.ilGenerator.Emit(lastOperation.OperationCode, lastOperation.Value);
          this.EmitCounterIncrement(lastOperation.Offset);
          break;
      }
    }

    private ILGeneratorLabel GetLabelFor(uint offset) {
      var result = this.labelFor[offset];
      if (result == null)
        this.labelFor[offset] = result = new ILGeneratorLabel();
      return result;
    }

    private void EmitCounterIncrement(uint offset) {
      var blockEntryCounter = new FieldDefinition() {
        ContainingTypeDefinition = this.counterFieldsForCurrentMethod,
        InternFactory = this.host.InternFactory,
        IsStatic = true,
        Name = this.host.NameTable.GetNameFor("Counter"+offset.ToString("x4")),
        Type = this.host.PlatformType.SystemUInt32,
        Visibility = TypeMemberVisibility.Assembly,
      };
      this.fieldOffsets.Add(offset);
      this.counterFieldsForCurrentMethod.Fields.Add(blockEntryCounter);
      this.ilGenerator.Emit(OperationCode.Ldsfld, blockEntryCounter);
      this.ilGenerator.Emit(OperationCode.Ldc_I4_1);
      this.ilGenerator.Emit(OperationCode.Add);
      this.ilGenerator.Emit(OperationCode.Stsfld, blockEntryCounter);
    }

  }

}