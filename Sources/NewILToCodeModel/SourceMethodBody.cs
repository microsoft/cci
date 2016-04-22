//-----------------------------------------------------------------------------
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the Microsoft Public License.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using Microsoft.Cci.Analysis;
using Microsoft.Cci.MutableCodeModel;
using Microsoft.Cci.UtilityDataStructures;

namespace Microsoft.Cci.ILToCodeModel {
  /// <summary>
  /// A metadata (IL) representation along with a source level representation of the body of a method or of a property/event accessor.
  /// </summary>
  public class SourceMethodBody : Microsoft.Cci.MutableCodeModel.SourceMethodBody, IMethodBody {

    /// <summary>
    /// Allocates a metadata (IL) representation along with a source level representation of the body of a method or of a property/event accessor.
    /// </summary>
    /// <param name="ilMethodBody">A method body whose IL operations should be decompiled into a block of statements that will be the
    /// result of the Block property of the resulting source method body.</param>
    /// <param name="host">An object representing the application that is hosting the converter. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.</param>
    /// <param name="sourceLocationProvider">An object that can map some kinds of ILocation objects to IPrimarySourceLocation objects. May be null.</param>
    /// <param name="localScopeProvider">An object that can provide information about the local scopes of a method.</param>
    /// <param name="options">Set of options that control decompilation.</param>
    public SourceMethodBody(IMethodBody ilMethodBody, IMetadataHost host, ISourceLocationProvider/*?*/ sourceLocationProvider,
      ILocalScopeProvider/*?*/ localScopeProvider, DecompilerOptions options = DecompilerOptions.None)
      : base(host, sourceLocationProvider, localScopeProvider) {
      Contract.Requires(ilMethodBody != null);
      Contract.Requires(host != null);

      this.ilMethodBody = ilMethodBody;
      this.host = host;
      this.nameTable = host.NameTable;
      this.sourceLocationProvider = sourceLocationProvider;
      this.pdbReader = sourceLocationProvider as PdbReader;
      this.localScopeProvider = localScopeProvider;
      this.options = options;
      this.platformType = ilMethodBody.MethodDefinition.ContainingTypeDefinition.PlatformType;
      if (IteratorHelper.EnumerableIsNotEmpty(ilMethodBody.LocalVariables))
        this.LocalsAreZeroed = ilMethodBody.LocalsAreZeroed;
      else
        this.LocalsAreZeroed = true;
      this.MethodDefinition = ilMethodBody.MethodDefinition;
      this.privateHelperFieldsToRemove = null;
      this.privateHelperMethodsToRemove = null;
      this.privateHelperTypesToRemove = null;
      this.cdfg = ControlAndDataFlowGraph<BasicBlock<Instruction>, Instruction>.GetControlAndDataFlowGraphFor(host, ilMethodBody, localScopeProvider);
    }

    internal readonly IMetadataHost host;
    internal readonly IMethodBody ilMethodBody;
    internal readonly INameTable nameTable;
    internal readonly IPlatformType platformType;
    internal readonly ControlAndDataFlowGraph<BasicBlock<Instruction>, Instruction> cdfg;

    internal readonly ISourceLocationProvider/*?*/ sourceLocationProvider;
    internal readonly ILocalScopeProvider/*?*/ localScopeProvider;
    internal readonly DecompilerOptions options;
    private readonly PdbReader/*?*/ pdbReader;
    internal List<ITypeDefinition>/*?*/ privateHelperTypesToRemove;
    internal Dictionary<uint, IMethodDefinition>/*?*/ privateHelperMethodsToRemove;
    internal List<IFieldDefinition>/*?*/ privateHelperFieldsToRemove;
    Hashtable<ILocalDefinition, LocalDefinition> localMap = new Hashtable<ILocalDefinition, LocalDefinition>();
    internal readonly Hashtable<List<IGotoStatement>> gotosThatTarget = new Hashtable<List<IGotoStatement>>();
    internal readonly HashtableForUintValues<object> numberOfReferencesToLocal = new HashtableForUintValues<object>();
    internal readonly HashtableForUintValues<object> numberOfAssignmentsToLocal = new HashtableForUintValues<object>();
    internal readonly SetOfObjects bindingsThatMakeALastUseOfALocalVersion = new SetOfObjects();

    [ContractInvariantMethod]
    private void ObjectInvariant() {
      Contract.Invariant(this.host != null);
      Contract.Invariant(this.ilMethodBody != null);
      Contract.Invariant(this.nameTable != null);
      Contract.Invariant(this.platformType != null);
      Contract.Invariant(this.cdfg != null);
      Contract.Invariant(this.localMap != null);
      Contract.Invariant(this.gotosThatTarget != null);
      Contract.Invariant(this.numberOfReferencesToLocal != null);
      Contract.Invariant(this.numberOfAssignmentsToLocal != null);
      Contract.Invariant(this.bindingsThatMakeALastUseOfALocalVersion != null);
    }

    /// <summary>
    /// Decompile the IL operations of this method body into a block of statements.
    /// </summary>
    protected override IBlockStatement GetBlock() {
      var block = new DecompiledBlock(0, this.ilMethodBody.Size, new Sublist<BasicBlock<Instruction>>(this.cdfg.AllBlocks, 0, this.cdfg.AllBlocks.Count), isLexicalScope: true);
      this.CreateExceptionBlocks(block);
      bool addDeclarations = true;
      if (this.localScopeProvider != null) {
        var scopes = new List<ILocalScope>(this.localScopeProvider.GetLocalScopes(this.ilMethodBody));
        if (scopes.Count > 0) {
          this.CreateLexicalScopes(block, new Sublist<ILocalScope>(scopes, 0, scopes.Count));
          addDeclarations = false;
        }
      }
      if (addDeclarations) {
        int counter = 0;
        foreach (var local in this.ilMethodBody.LocalVariables) {
          Contract.Assume(local != null);
          Contract.Assume(counter <= block.Statements.Count);
          block.Statements.Insert(counter++, new LocalDeclarationStatement() { LocalVariable = this.GetLocalWithSourceName(local) });
        }
      }
      new InstructionParser(this).Traverse(block);
      new SwitchReplacer(this).Traverse(block);
      DeleteNops(block);
      DeleteLocalAssignedLocal(block);
      new PatternReplacer(this, block).Traverse(block);
      new TryCatchReplacer(this, block).Traverse(block);
      new RemoveNonLexicalBlocks().Traverse(block);
      new DeclarationUnifier(this).Traverse(block);
	  new ResourceUseReplacer(this).Traverse(block);
      new IfThenElseReplacer(this).Traverse(block);
      if ((this.options & DecompilerOptions.Loops) != 0) {
          new WhileLoopReplacer(this).Traverse(block);
          new ForLoopReplacer(this).Traverse(block);
          new ForEachLoopReplacer(this).Traverse(block);
      }
      new UnreferencedLabelRemover(this).Traverse(block);
      new LockReplacer(this).Traverse(block);
      new BlockFlattener().Traverse(block);
      this.RemoveRedundantFinalReturn(block);
      var result = new CompilationArtifactRemover(this).Rewrite(block);
      if ((this.options & DecompilerOptions.AnonymousDelegates) != 0) {
        bool didNothing;
        result = new AnonymousDelegateInserter(this).InsertAnonymousDelegates(result, out didNothing);
        if (!didNothing) new DeclarationUnifier(this).Traverse(result);
      }
      this.AddBackFirstNop(result as BlockStatement);
      if ((this.options & DecompilerOptions.Unstack) != 0)
        result = Unstacker.GetRidOfStack(this, result);
      return result;
    }

    private void AddBackFirstNop(BlockStatement block) {
      if (this.sourceLocationProvider == null) return;
      if (block == null) return;
      foreach (var op in this.ilMethodBody.Operations) {
        if (op.OperationCode != OperationCode.Nop) break;
        block.Statements.Insert(0, new EmptyStatement() { Locations = new List<ILocation>() { op.Location } });
        break;
      }
    }

    private void CreateExceptionBlocks(DecompiledBlock block) {
      Contract.Requires(block != null);

      if (IteratorHelper.EnumerableIsEmpty(this.ilMethodBody.OperationExceptionInformation)) return;
      List<IOperationExceptionInformation> handlers = new List<IOperationExceptionInformation>(this.ilMethodBody.OperationExceptionInformation);
      handlers.Sort(CompareHandlers);
      foreach (var exInfo in handlers) {
        Contract.Assume(exInfo != null);
        this.CreateNestedBlock(block, exInfo.TryStartOffset, exInfo.TryEndOffset);
        if (exInfo.HandlerKind == HandlerKind.Filter)
          this.CreateNestedBlock(block, exInfo.FilterDecisionStartOffset, exInfo.HandlerEndOffset);
        else
          this.CreateNestedBlock(block, exInfo.HandlerStartOffset, exInfo.HandlerEndOffset);
      }
    }

    private static int CompareHandlers(IOperationExceptionInformation handler1, IOperationExceptionInformation handler2) {
      Contract.Requires(handler1 != null);
      Contract.Requires(handler2 != null);
      if (handler1.TryStartOffset < handler2.TryStartOffset) return -1;
      if (handler1.TryStartOffset > handler2.TryStartOffset) return 1;
      if (handler1.TryEndOffset > handler2.TryEndOffset) return -1;
      if (handler1.TryEndOffset < handler2.TryEndOffset) return 1;
      if (handler1.HandlerStartOffset < handler2.HandlerStartOffset) return -1;
      if (handler2.HandlerStartOffset > handler2.HandlerStartOffset) return 1;
      if (handler1.HandlerEndOffset > handler2.HandlerEndOffset) return -1;
      if (handler1.HandlerEndOffset < handler2.HandlerEndOffset) return 1;
      return 0;
    }

    [ContractVerification(false)]
    private DecompiledBlock CreateNestedBlock(DecompiledBlock block, uint startOffset, uint endOffset) {
      Contract.Requires(block != null);
      Contract.Requires(startOffset <= endOffset);
      Contract.Ensures(Contract.Result<DecompiledBlock>() != null);

      {
      nextBlock:
        foreach (var statement in block.Statements) {
          var nestedBlock = statement as DecompiledBlock;
          if (nestedBlock == null) continue;
          if (nestedBlock.StartOffset <= startOffset && nestedBlock.EndOffset >= endOffset) {
            block = nestedBlock;
            goto nextBlock;
          }
        }
      }
      if (block.StartOffset == startOffset && block.EndOffset == endOffset) return block;
      //replace block.Statements with three nested blocks, the middle one corresponding to one we have to create
      //but keep any declarations.
      int n = block.Statements.Count;
      var oldStatements = block.Statements;
      var newStatements = block.Statements;
      int m = 0;
      if (n >= 0) {
        while (m < n && oldStatements[m] is LocalDeclarationStatement) m++;
        if (m < n) {
          newStatements = new List<IStatement>(m+3);
          for (int i = 0; i < m; i++) newStatements.Add(oldStatements[i]);
        }
      }
      block.Statements = newStatements;
      var basicBlocks = block.GetBasicBlocksForRange(startOffset, endOffset);
      var newNestedBlock = new DecompiledBlock(startOffset, endOffset, basicBlocks, isLexicalScope: true);
      DecompiledBlock beforeBlock = null;
      if (block.StartOffset < startOffset) {
        var basicBlocksBefore = block.GetBasicBlocksForRange(block.StartOffset, startOffset);
        Contract.Assume(block.StartOffset < startOffset);
        beforeBlock = new DecompiledBlock(block.StartOffset, startOffset, basicBlocksBefore, isLexicalScope: false);
        newStatements.Add(beforeBlock);
      }
      newStatements.Add(newNestedBlock);
      DecompiledBlock afterBlock = null;
      if (block.EndOffset > endOffset) {
        var basicBlocksAfter = block.GetBasicBlocksForRange(endOffset, block.EndOffset);
        Contract.Assume(block.EndOffset > endOffset);
        afterBlock = new DecompiledBlock(endOffset, block.EndOffset, basicBlocksAfter, isLexicalScope: false);
        newStatements.Add(afterBlock);
      }
      if (newStatements != oldStatements) {
        //In this case there were already some nested blocks, which happens when there are nested exception blocks
        //and we are creating an enclosing lexical block that does not quite coincide with block.
        //We now have to populate the newly created blocks with these nested blocks, splitting them if necessary.
        for (int i = m; i < n; i++) {
          var nb = oldStatements[i] as DecompiledBlock;
          Contract.Assume(nb != null);
          if (nb.EndOffset <= startOffset) {
            Contract.Assume(beforeBlock != null);
            beforeBlock.Statements.Add(nb);
          } else if (nb.StartOffset < startOffset) {
            Contract.Assume(nb.EndOffset <= endOffset);  //nb starts before newNestedBlock but ends inside it.
            Contract.Assume(beforeBlock != null);
            if (nb.IsLexicalScope) {
              //Lexical scopes are assumed to nest cleanly. But the C# is not playing along when one using statement immediately follows another
              //Well fix matters by making nb.EndOffset == startOffset
              nb.EndOffset = startOffset;
              beforeBlock.Statements.Add(nb);
            } else 
              this.SplitBlock(nb, startOffset, beforeBlock.Statements, newNestedBlock.Statements);
          } else if (nb.EndOffset <= endOffset) {
            newNestedBlock.Statements.Add(nb);
          } else if (nb.StartOffset < endOffset) {
            Contract.Assert(nb.EndOffset > endOffset); //nb starts inside newNestedBlock but ends after it.
            Contract.Assume(!nb.IsLexicalScope); //lexical scopes are assumed to nest cleanly.
            Contract.Assume(afterBlock != null);
            this.SplitBlock(nb, endOffset, newNestedBlock.Statements, afterBlock.Statements);
          } else {
            Contract.Assume(afterBlock != null);
            afterBlock.Statements.Add(nb);
          }
        }
        //consolidate blocks consisting of a single block
        Consolidate(beforeBlock);
        Consolidate(newNestedBlock);
        Consolidate(afterBlock);
      }
      return newNestedBlock;
    }

    private void Consolidate(DecompiledBlock block) {
      if (block == null) return;
      if (block.Statements.Count != 1) return;
      var nestedBlock = block.Statements[0] as DecompiledBlock;
      if (nestedBlock == null) return;
      Consolidate(nestedBlock);
      block.Statements = nestedBlock.Statements;
    }

    private void SplitBlock(DecompiledBlock blockToSplit, uint splitOffset, List<IStatement> leftList, List<IStatement> rightList) {
      Contract.Requires(blockToSplit != null);
      Contract.Requires(leftList != null);
      Contract.Requires(rightList != null);
      Contract.Requires(splitOffset >= blockToSplit.StartOffset);
      Contract.Requires(splitOffset <= blockToSplit.EndOffset);

      var leftBasicBlocks = blockToSplit.GetBasicBlocksForRange(blockToSplit.StartOffset, splitOffset);
      Contract.Assume(splitOffset >= blockToSplit.StartOffset);
      var leftBlock = new DecompiledBlock(blockToSplit.StartOffset, splitOffset, leftBasicBlocks, isLexicalScope: false);
      leftList.Add(leftBlock);
      var rightBasicBlocks = blockToSplit.GetBasicBlocksForRange(splitOffset, blockToSplit.EndOffset);
      Contract.Assume(splitOffset <= blockToSplit.EndOffset);
      var rightBlock = new DecompiledBlock(splitOffset, blockToSplit.EndOffset, rightBasicBlocks, isLexicalScope: false);
      rightList.Add(rightBlock);

      var n = blockToSplit.Statements.Count;
      if (n == 0) return;
      var leftStatements = leftBlock.Statements = new List<IStatement>();
      var rightStatements = rightBlock.Statements = new List<IStatement>();
      for (int i = 0; i < n; i++) {
        var nb = blockToSplit.Statements[i] as DecompiledBlock;
        Contract.Assume(nb != null);
        if (nb.EndOffset <= splitOffset) {
          leftStatements.Add(nb);
        } else if (nb.StartOffset < splitOffset) {
          Contract.Assume(!nb.IsLexicalScope); //lexical scopes are assumed to nest cleanly.
          this.SplitBlock(nb, splitOffset, leftStatements, rightStatements);
        } else {
          rightStatements.Add(nb);
        }
      }
      Consolidate(leftBlock);
      Consolidate(rightBlock);
    }

    private void CreateLexicalScopes(DecompiledBlock block, Sublist<ILocalScope> scopes) {
      Contract.Requires(block != null);
      Contract.Requires(this.localScopeProvider != null);

      for (int i = 0, n = scopes.Count; i < n; ) {
        var scope = scopes[i++];
        if (scope.Length == 0) continue;
        var nestedBlock = this.CreateNestedBlock(block, scope.Offset, scope.Offset+scope.Length);
        this.AddLocalsAndConstants(nestedBlock, scope);
      }
    }

    private void AddLocalsAndConstants(DecompiledBlock block, ILocalScope scope) {
      Contract.Requires(block != null);
      Contract.Requires(scope != null);
      Contract.Requires(this.localScopeProvider != null);

      bool isLexicalScope = false;
      int counter = 0;
      foreach (var local in this.localScopeProvider.GetConstantsInScope(scope)) {
        Contract.Assume(local != null);
        var localVariable = this.GetLocalWithSourceName(local);
        Contract.Assume(counter <= block.Statements.Count);
        block.Statements.Insert(counter++, new LocalDeclarationStatement() { LocalVariable = localVariable });
      }
      foreach (var local in this.localScopeProvider.GetVariablesInScope(scope)) {
        Contract.Assume(local != null);
        var localVariable = this.GetLocalWithSourceName(local);
        Contract.Assume(counter <= block.Statements.Count);
        block.Statements.Insert(counter++, new LocalDeclarationStatement() { LocalVariable = localVariable });
      }
      if (isLexicalScope) block.IsLexicalScope = true;
    }

    internal ILocalDefinition GetLocalWithSourceName(ILocalDefinition localDef) {
      Contract.Requires(localDef != null);
      Contract.Ensures(Contract.Result<ILocalDefinition>() != null);

      if (this.sourceLocationProvider == null) return localDef;
      var mutableLocal = this.localMap[localDef];
      if (mutableLocal != null) return mutableLocal;
      mutableLocal = localDef as LocalDefinition;
      if (mutableLocal == null) {
        mutableLocal = new LocalDefinition();
        mutableLocal.Copy(localDef, this.host.InternFactory);
      }
      this.localMap.Add(localDef, mutableLocal);
      bool isCompilerGenerated;
      var sourceName = this.sourceLocationProvider.GetSourceNameFor(localDef, out isCompilerGenerated);
      if (sourceName != localDef.Name.Value) {
        mutableLocal.Name = this.host.NameTable.GetNameFor(sourceName);
      }
      return mutableLocal;
    }

    private void RemoveRedundantFinalReturn(DecompiledBlock block) {
      Contract.Requires(block != null);
      var n = block.Statements.Count;
      if (n > 0) {
        var returnStatement = block.Statements[n-1] as ReturnStatement;
        if (returnStatement == null) return;
        if (this.ilMethodBody.MethodDefinition.Type.TypeCode != PrimitiveTypeCode.Void) {
          var boundExpression = returnStatement.Expression as IBoundExpression;
          if (boundExpression == null) return;
          var local = boundExpression.Definition as ILocalDefinition;
          if (local == null) return;
          if (this.numberOfAssignmentsToLocal[local] > 0) return;
          this.numberOfReferencesToLocal[local]--;
          if (this.numberOfReferencesToLocal[local] == 0 && this.numberOfAssignmentsToLocal[local] == 0) {
            for (int j = 0; j < n-1; j++) {
              var locDecl = block.Statements[j] as ILocalDeclarationStatement;
              if (locDecl != null && locDecl.LocalVariable == local) {
                block.Statements.RemoveAt(j);
                n--;
                break;
              }
            }
          }
        }
        block.Statements.RemoveAt(n-1);
      }
    }

    private static void DeleteNops(BlockStatement block) {
      Contract.Requires(block != null);
      var statements = block.Statements;
      var n = statements.Count;
      var numberOfStatementsToDelete = 0;
      for (int i = 0; i < n; i++) {
        var s = statements[i];
        if (s is IEmptyStatement && !(s is EndFilter || s is EndFinally || s is SwitchInstruction)) {
          numberOfStatementsToDelete++;
        } else {
          var bs = s as BlockStatement;
          if (bs != null)
            DeleteNops(bs);
        }
      }
      if (0 < numberOfStatementsToDelete){
        var newStmts = new List<IStatement>(n - numberOfStatementsToDelete);
        for (int i = 0; i < n; i++) {
          var s = statements[i];
          if (!(s is IEmptyStatement))
            newStmts.Add(s);
        }
        block.Statements = newStmts;
      }
    }

    private void DeleteLocalAssignedLocal(BlockStatement block) {
      Contract.Requires(block != null);
      var statements = block.Statements;
      List<IStatement> newStatements = null;
      var n = statements.Count;
      for (int i = 0; i < n; i++) {
        var s = statements[i];
        ILocalDefinition local;
        if (IsAssignmentOfLocalToLocal(s, out local)) {
          Contract.Assert(local != null);
          if (newStatements == null) {
            newStatements = new List<IStatement>(n - 1);
            for (int j = 0; j < i; j++) newStatements.Add(statements[j]);
          }
          this.numberOfAssignmentsToLocal[local]--;
          this.numberOfReferencesToLocal[local]--;
        } else {
          var bs = s as BlockStatement;
          if (bs != null)
            DeleteLocalAssignedLocal(bs);
          if (newStatements != null)
            newStatements.Add(s);
        }
      }
      if (newStatements != null)
        block.Statements = newStatements;
    }

    private static bool IsAssignmentOfLocalToLocal(IStatement s, out ILocalDefinition local) {
      Contract.Ensures(!Contract.Result<bool>() || Contract.ValueAtReturn<ILocalDefinition>(out local) != null);
      var es = s as IExpressionStatement;
      if (es != null) {
        var assign = es.Expression as IAssignment;
        if (assign != null) {
          var be = assign.Source as IBoundExpression;
          if (be != null && (local = be.Definition as ILocalDefinition) != null) {
            return be.Definition == assign.Target.Definition;
          }
        }
      }
      local = null;
      return false;
    }




    #region IMethodBody Members
    private bool IsReadOnly { get { return (this.options & DecompilerOptions.ReadOnly) != 0; } }

    IEnumerable<IOperationExceptionInformation> IMethodBody.OperationExceptionInformation
    {
      get
      {
        if (IsReadOnly) return this.ilMethodBody.OperationExceptionInformation;
        return this.OperationExceptionInformation;
      }
    }

    IEnumerable<ILocalDefinition> IMethodBody.LocalVariables
    {
      get
      {
        if (IsReadOnly) return this.ilMethodBody.LocalVariables;
        return this.LocalVariables;
      }
    }

    IMethodDefinition IMethodBody.MethodDefinition
    {
      get
      {
        if (IsReadOnly) return this.ilMethodBody.MethodDefinition;
        return this.MethodDefinition;
      }
    }

    IEnumerable<IOperation> IMethodBody.Operations
    {
      get
      {
        if (IsReadOnly) return this.ilMethodBody.Operations;
        return this.Operations;
      }
    }

    ushort IMethodBody.MaxStack
    {
      get
      {
        if (IsReadOnly) return this.ilMethodBody.MaxStack;
        return this.MaxStack;
      }
    }

    IEnumerable<ITypeDefinition> IMethodBody.PrivateHelperTypes
    {
      get
      {
        if (IsReadOnly) return this.ilMethodBody.PrivateHelperTypes;
        return this.PrivateHelperTypesImplementation;
      }
    }

    uint IMethodBody.Size
    {
      get
      {
        if (IsReadOnly) return this.ilMethodBody.Size;
        return this.Size;
      }
    }

    #endregion
  }
}
