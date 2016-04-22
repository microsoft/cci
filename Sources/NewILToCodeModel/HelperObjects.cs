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
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Microsoft.Cci.Analysis;
using Microsoft.Cci.MutableCodeModel;
using Microsoft.Cci.UtilityDataStructures;

namespace Microsoft.Cci.ILToCodeModel {

  /// <summary>
  /// A delimited collection of statements to execute in a new (nested) scope.
  /// </summary>
  public sealed class DecompiledBlock : BlockStatement {

    /// <summary>
    /// A delimited collection of statements to execute in a new (nested) scope.
    /// </summary>
    /// <param name="startOffset">The IL offset of the first statement in the block.</param>
    /// <param name="endOffset">The IL offset of the first statement following the block.</param>
    /// <param name="containedBlocks">A list of basic blocks that are contained inside this source block.</param>
    /// <param name="isLexicalScope">If false, the block is a helper block for the decompilation process and it should be removed during final cleanup.</param>
    internal DecompiledBlock(uint startOffset, uint endOffset, Sublist<BasicBlock<Instruction>> containedBlocks, bool isLexicalScope) {
      Contract.Requires(endOffset >= startOffset);
      this.StartOffset = startOffset;
      this.EndOffset = endOffset;
      this.ContainedBlocks = containedBlocks;
      this.IsLexicalScope = isLexicalScope;
    }

    /// <summary>
    /// The IL offset of the first statement in the block.
    /// </summary>
    internal uint StartOffset;

    /// <summary>
    /// The IL offset of the first statement following the block. 
    /// </summary>
    internal uint EndOffset;

    /// <summary>
    /// If false, the block is a helper block for the decompilation process and it should be removed during final cleanup.
    /// </summary>
    internal bool IsLexicalScope;

    /// <summary>
    /// A list of basic blocks that are contained inside this source block.
    /// </summary>
    internal Sublist<BasicBlock<Instruction>> ContainedBlocks;

    internal Sublist<BasicBlock<Instruction>> GetBasicBlocksForRange(uint startOffset, uint endOffset) {
      var n = this.ContainedBlocks.Count;
      var i = 0;
      while (i < n && GetStartOffset(this.ContainedBlocks[i]) < startOffset) i++;
      if (i >= n) return new Sublist<BasicBlock<Instruction>>();
      var j = i+1;
      while (j < n && GetStartOffset(this.ContainedBlocks[j]) < endOffset) j++;
      return this.ContainedBlocks.GetSublist(i, j-i);
    }

    internal static uint GetStartOffset(BasicBlock<Instruction> basicBlock) {
      Contract.Assume(basicBlock != null);
      Contract.Assume(basicBlock.Instructions.Count > 0);
      return basicBlock.Instructions[0].Operation.Offset;
    }

    internal LabeledStatement/*?*/ RemoveAndReturnInitialLabel() {
      for (int i = 0, n = this.Statements.Count; i < n; i++) {
        var s = this.Statements[i];
        var ls = s as LabeledStatement;
        if (ls != null) { this.Statements.RemoveAt(i); return ls; }
        var b = s as DecompiledBlock;
        if (b != null) return b.RemoveAndReturnInitialLabel();
        if (s is IEmptyStatement) continue;
        var d = s as LocalDeclarationStatement;
        if (d != null && d.InitialValue == null) continue;
        break;
      }
      return null;
    }

    internal LabeledStatement/*?*/ ReturnInitialLabel() {
      for (int i = 0, n = this.Statements.Count; i < n; i++) {
        var s = this.Statements[i];
        var ls = s as LabeledStatement;
        if (ls != null) return ls;
        var b = s as DecompiledBlock;
        if (b != null) return b.ReturnInitialLabel();
        if (s is IEmptyStatement) continue;
        var d = s as LocalDeclarationStatement;
        if (d != null && d.InitialValue == null) continue;
        break;
      }
      return null;
    }

    internal void ReplaceInitialLabel(LabeledStatement labeledStatement) {
      Contract.Requires(labeledStatement != null);
      for (int i = 0, n = this.Statements.Count; i < n; i++) {
        var s = this.Statements[i];
        var ls = s as LabeledStatement;
        if (ls != null) { this.Statements[i] = labeledStatement; return; }
        var b = s as DecompiledBlock;
        if (b != null) b.ReplaceInitialLabel(labeledStatement);
        var d = s as LocalDeclarationStatement;
        if (d != null && d.InitialValue == null) continue;
        break;
      }
    }

    internal bool FirstExecutableStatementIs(IStatement statement) {
      for (int i = 0, n = this.Statements.Count; i < n; i++) {
        var s = this.Statements[i];
        if (s == statement) return true;
        var b = s as DecompiledBlock;
        if (b != null) return b.FirstExecutableStatementIs(statement);
        if (s is IEmptyStatement) continue;
        var d = s as LocalDeclarationStatement;
        if (d != null && d.InitialValue == null) continue;
        break;
      }
      return false;
    }
  }

  internal sealed class EndFilter : EmptyStatement {
    internal Expression FilterResult;

    internal EndFilter() {
    }

    internal EndFilter(EndFilter endFilter)
      : base(endFilter) {
      Contract.Requires(endFilter != null);
      this.FilterResult = endFilter.FilterResult;
    }

    public override EmptyStatement Clone() {
      return new EndFilter(this);
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }
  }

  internal sealed class EndFinally : EmptyStatement {

    internal EndFinally() { }

    internal EndFinally(EndFinally endFinally)
      : base(endFinally) {
      Contract.Requires(endFinally != null);
    }

    public override EmptyStatement Clone() {
      return new EndFinally(this);
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }
  }

  internal sealed class SwitchInstruction : EmptyStatement {

    internal SwitchInstruction() {
      this.switchCases = new List<GotoStatement>();
    }

    internal SwitchInstruction(SwitchInstruction switchInstruction)
      : base(switchInstruction) {
      Contract.Requires(switchInstruction != null);
      this.switchExpression = switchInstruction.switchExpression;
      Contract.Assume(switchInstruction.switchCases != null);
      this.switchCases = switchInstruction.switchCases;
    }

    internal IExpression switchExpression;
    List<GotoStatement> switchCases;

    [ContractInvariantMethod]
    private void ObjectInvariant() {
      Contract.Invariant(this.switchCases != null);
    }

    public override EmptyStatement Clone() {
      return new SwitchInstruction(this);
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    internal List<GotoStatement> SwitchCases {
      get {
        Contract.Ensures(Contract.Result<List<GotoStatement>>() != null);
        return this.switchCases;
      }
    }

  }

}