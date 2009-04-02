//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using Microsoft.Cci.Ast;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci.SmallBasic {
  internal sealed class GosubStatement : Statement {

    internal GosubStatement(SimpleName targetLabel, ISourceLocation sourceLocation, RootClassDeclaration rootClass)
      : base(sourceLocation) {
      this.targetLabel = targetLabel;
      this.rootClass = rootClass;
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// Do not call this method directly, but call the HasErrors method. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return false;
      //TODO: give an error if the label is not found
    }

    public override void Dispatch(ICodeVisitor visitor) {
      CompileTimeConstant labelIndex = new CompileTimeConstant(this.rootClass.GetLabelIndex(this.TargetLabel.Name), this.TargetLabel.SourceLocation);
      labelIndex.SetContainingExpression(this.TargetLabel);
      List<Expression> arguments = new List<Expression>(1);
      arguments.Add(labelIndex);
      IMethodDefinition constructor = Dummy.Method;
      foreach (IMethodDefinition cons in this.rootClass.TypeDefinition.GetMembersNamed(this.Compilation.NameTable.Ctor, false)) {
        constructor = cons; break;
      }
      Expression thisArgument = new CreateObjectInstanceForResolvedConstructor(constructor, arguments, this.SourceLocation);
      //^ assume this.ContainingBlock.ContainingMethodDeclaration != null;
      MethodCall mcall = new ResolvedMethodCall(this.rootClass.MainMethod.MethodDefinition, thisArgument, new List<Expression>(0), this.SourceLocation);
      ExpressionStatement gosub = new ExpressionStatement(mcall);
      gosub.Dispatch(visitor);
    }

    readonly RootClassDeclaration rootClass;

    public SimpleName TargetLabel {
      get { return this.targetLabel; }
    }
    readonly SimpleName targetLabel;

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a Statement before constructing the containing Block.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingBlock(BlockStatement containingBlock) {
      base.SetContainingBlock(containingBlock);
      DummyExpression containingExpression = new DummyExpression(containingBlock, SourceDummy.SourceLocation);
      this.TargetLabel.SetContainingExpression(containingExpression);
    }

  }
}
