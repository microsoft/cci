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
using Microsoft.Cci;
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
      if (this.Statements.Count == 0) return null;
      var firstStatement = this.Statements[0];
      var result = firstStatement as LabeledStatement;
      if (result != null) this.Statements.RemoveAt(0);
      var firstBlock = firstStatement as DecompiledBlock;
      if (firstBlock != null) return firstBlock.RemoveAndReturnInitialLabel();
      return result;
    }

    internal LabeledStatement/*?*/ ReturnInitialLabel() {
      if (this.Statements.Count == 0) return null;
      var firstStatement = this.Statements[0];
      var result = firstStatement as LabeledStatement;
      if (result != null) return result;
      var firstBlock = firstStatement as DecompiledBlock;
      if (firstBlock != null) return firstBlock.ReturnInitialLabel();
      return null;
    }

    internal void ReplaceInitialLabel(LabeledStatement label) {
      Contract.Requires(label != null);
      if (this.Statements.Count == 0) return;
      var firstStatement = this.Statements[0];
      var result = firstStatement as LabeledStatement;
      if (result != null) { this.Statements[0] = label; return; }
      var firstBlock = firstStatement as DecompiledBlock;
      if (firstBlock != null) firstBlock.ReplaceInitialLabel(label);
    }

    internal bool FirstStatementIs(IStatement statement) {
      if (this.Statements.Count == 0) return false;
      var firstStatement = this.Statements[0];
      if (firstStatement == statement) return true;
      var firstBlock = firstStatement as DecompiledBlock;
      if (firstBlock == null) return false;
      return firstBlock.FirstStatementIs(statement);
    }
  }

  internal sealed class ConvertToUnsigned : Expression, IConversion {

    internal ConvertToUnsigned(IExpression valueToConvert) {
      Contract.Requires(valueToConvert != null);
      this.valueToConvert = valueToConvert;
    }

    [ContractInvariantMethod]
    private void ObjectInvariant() {
      Contract.Invariant(this.valueToConvert != null);
    }


    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    public IExpression ValueToConvert {
      get { return this.valueToConvert; }
    }
    IExpression valueToConvert;

    public bool CheckNumericRange {
      get { return false; }
    }

    public ITypeReference TypeAfterConversion {
      get { return TypeHelper.UnsignedEquivalent(this.ValueToConvert.Type); }
    }

    ITypeReference IExpression.Type {
      get { return this.TypeAfterConversion; }
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