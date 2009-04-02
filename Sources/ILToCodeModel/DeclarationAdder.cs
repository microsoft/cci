//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System.Collections.Generic;
using Microsoft.Cci.MutableCodeModel;

namespace Microsoft.Cci.ILToCodeModel {

  internal class DeclarationAdder : BaseCodeTraverser {

    Stack<BasicBlock> scopeStack = new Stack<BasicBlock>();

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
            if (localDef.Type == Dummy.TypeReference && localDef is LocalDefinition) {
              ((LocalDefinition)localDef).Type = assignment.Source.Type;
            }
            basicBlock.Statements[i] = localDecl;
            localsInCurrentScope.Remove(localDef);
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



  }
}