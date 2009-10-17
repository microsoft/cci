//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System.Collections.Generic;
using Microsoft.Cci.MutableCodeModel;

namespace Microsoft.Cci.ILToCodeModel {

  internal class DeclarationAdder : BaseCodeTraverser {

    Dictionary<ILocalDefinition, bool> declaredLocals = new Dictionary<ILocalDefinition, bool>();

    internal void Visit(SourceMethodBody body, BasicBlock rootBlock) {
      this.Visit(rootBlock);
      List<IStatement> prelude = new List<IStatement>();
      foreach (var localDef in body.LocalVariables) {
        if (this.declaredLocals.ContainsKey(localDef)) continue;
        int numRefs;
        if (!body.numberOfReferences.TryGetValue(localDef, out numRefs) || numRefs == 0) continue;
        LocalDeclarationStatement localDecl = new LocalDeclarationStatement();
        localDecl.LocalVariable = localDef;
        prelude.Add(localDecl);
      }
      if (prelude.Count > 0)
        rootBlock.Statements.InsertRange(0, prelude); //TODO: use pdb info to insert them in the same order they appear in the source
    }

    public override void Visit(IBlockStatement block) {
      BasicBlock basicBlock = (BasicBlock)block;
      List<ILocalDefinition>/*?*/ localsInCurrentScope = basicBlock.LocalVariables;
      if (localsInCurrentScope != null) {
        localsInCurrentScope = new List<ILocalDefinition>(localsInCurrentScope);
        for (int i = 0, n = basicBlock.Statements.Count; i < n; i++) {
          ExpressionStatement/*?*/ expressionStatement = basicBlock.Statements[i] as ExpressionStatement;
          if (expressionStatement == null) continue;
          Assignment/*?*/ assignment = expressionStatement.Expression as Assignment;
          if (assignment == null) continue;
          ILocalDefinition/*?*/ localDef = assignment.Target.Definition as ILocalDefinition;
          if (localDef == null) continue;
          if (localsInCurrentScope.Contains(localDef)) {
            LocalDeclarationStatement localDecl = new LocalDeclarationStatement();
            localDecl.LocalVariable = localDef;
            localDecl.InitialValue = assignment.Source;
            localDecl.Locations = expressionStatement.Locations;
            if (localDef.Type == Dummy.TypeReference && localDef is LocalDefinition) {
              ((LocalDefinition)localDef).Type = assignment.Source.Type;
            }
            basicBlock.Statements[i] = localDecl;
            localsInCurrentScope.Remove(localDef);
            this.declaredLocals[localDef] = true;
          }
        }
        if (localsInCurrentScope.Count > 0) {
          List<IStatement> prelude = new List<IStatement>(localsInCurrentScope.Count);
          foreach (ILocalDefinition localDef in localsInCurrentScope) {
            LocalDeclarationStatement localDecl = new LocalDeclarationStatement();
            localDecl.LocalVariable = localDef;
            prelude.Add(localDecl);
          }
          basicBlock.Statements.InsertRange(0, prelude); //TODO: use pdb info to insert them in the same order they appear in the source
        }
      }
      this.Visit(basicBlock.Statements);
    }

    public override void Visit(ILocalDeclarationStatement localDeclarationStatement) {
      this.declaredLocals[localDeclarationStatement.LocalVariable] = true;
      base.Visit(localDeclarationStatement);
    }

    public override void Visit(ICatchClause catchClause) {
      this.declaredLocals[catchClause.ExceptionContainer] = true;
      base.Visit(catchClause);
    }
  }
}