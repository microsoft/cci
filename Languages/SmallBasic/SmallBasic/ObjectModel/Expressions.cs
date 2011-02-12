//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System.Collections.Generic;
using System.Resources;
using Microsoft.Cci.Ast;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci.SmallBasic {

  internal sealed class SmallBasicAssignment : Assignment {

    public SmallBasicAssignment(TargetExpression target, Expression source, ISourceLocation sourceLocation)
      : base(target, source, sourceLocation) {
    }

    private SmallBasicAssignment(BlockStatement containingBlock, Assignment template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
    {
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    public override Expression MakeCopyFor(BlockStatement containingBlock) {
      if (containingBlock == this.ContainingBlock) return this;
      return new SmallBasicAssignment(containingBlock, this);
    }

    protected override IExpression ProjectAsNonConstantIExpression() {
      CreateDelegateInstance/*?*/ createDelegate = this.ConvertedSourceExpression as CreateDelegateInstance;
      if (createDelegate != null) {
        QualifiedName/*?*/ qualName = this.Target.Expression as QualifiedName;
        if (qualName != null) {
          IEventDefinition/*?*/ ev = qualName.Resolve(false) as IEventDefinition;
          if (ev != null) {
            List<Expression> arguments = new List<Expression>(1);
            arguments.Add(createDelegate);
            return new ResolvedMethodCall(ev.Adder.ResolvedMethod, qualName.Qualifier, arguments, this.SourceLocation);
          }
        }
      }
      return base.ProjectAsNonConstantIExpression();
    }

  }

  internal sealed class SmallBasicIndexer : Indexer {

    public SmallBasicIndexer(Expression indexedObject, IEnumerable<Expression> indices, ISourceLocation sourceLocation)
      : base(indexedObject, indices, sourceLocation) {
    }

    private SmallBasicIndexer(BlockStatement containingBlock, Indexer template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
    {
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock) {
      if (containingBlock == this.ContainingBlock) return this;
      return new SmallBasicIndexer(containingBlock, this);
    }

  }

  internal sealed class SmallBasicSimpleName : SimpleName {

    internal SmallBasicSimpleName(IName name, ISourceLocation sourceLocation)
      : base(name, sourceLocation, true) {
    }

    private SmallBasicSimpleName(BlockStatement containingBlock, SmallBasicSimpleName template)
      : base(containingBlock, template) {
      if (template.rootClass != null)
        this.rootClass = (RootClassDeclaration)template.rootClass.MakeShallowCopyFor(containingBlock.ContainingNamespaceDeclaration);
      if (template.expressionToInferTargetTypeFrom != null)
        this.expressionToInferTargetTypeFrom = template.expressionToInferTargetTypeFrom.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    public override Expression MakeCopyFor(BlockStatement containingBlock) {
      if (this.ContainingBlock == containingBlock) return this;
      return new SmallBasicSimpleName(containingBlock, this);
    }

    public override object/*?*/ Resolve() {
      if (this.rootClass != null) {
        FieldDefinition/*?*/ localField = null;
        this.rootClass.localFieldFor.TryGetValue(this.Name.UniqueKeyIgnoringCase, out localField);
        if (localField != null) return localField;
      }
      object/*?*/ binding = base.Resolve();
      if (binding == null && this.expressionToInferTargetTypeFrom != null) {
        SourceLocationBuilder slb = new SourceLocationBuilder(this.SourceLocation);
        slb.UpdateToSpan(this.expressionToInferTargetTypeFrom.SourceLocation);
        return this.rootClass.AddFieldForLocal(this, this.expressionToInferTargetTypeFrom, slb).FieldDefinition;
      }
      return binding;
    }

    internal RootClassDeclaration/*?*/ rootClass;
    internal Expression/*?*/ expressionToInferTargetTypeFrom;
  
  }
}