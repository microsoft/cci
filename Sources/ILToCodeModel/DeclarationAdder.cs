//-----------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.  All Rights Reserved.
// This code is licensed under the Microsoft Public License.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
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
      // For the first assignment to a local variable in the rootblock before a control statement is hit,
      // if the local variable is not mentioned previously, we turn this assignment into a local declaration. 
      List<ILocalDefinition> topLevelLocals = new List<ILocalDefinition>(body.LocalVariables);
      List<ILocalDefinition> localsMet = new List<ILocalDefinition>();
      for (int i = 0; i < rootBlock.Statements.Count; i++) {
        if (topLevelLocals.Count ==0) break;
        IExpressionStatement expressionStatement = rootBlock.Statements[i] as IExpressionStatement;
        if (expressionStatement != null) {
          IAssignment assignment = expressionStatement.Expression as IAssignment;
          if (assignment != null) {
            ILocalDefinition localDef = assignment.Target.Definition as ILocalDefinition;
            if (localDef != null && topLevelLocals.Contains(localDef) && !localsMet.Contains(localDef)) {
              LocalDeclarationStatement localDecl = new LocalDeclarationStatement() {
                LocalVariable = localDef, InitialValue = assignment.Source, 
              };
              rootBlock.Statements[i] = localDecl;
              topLevelLocals.Remove(localDef);
              localsMet.Add(localDef);
            }
          }
        }
        LocalFinder finder = new LocalFinder();
        finder.Visit(rootBlock.Statements[i]);
        foreach (ILocalDefinition local in finder.FoundLocals) {
          if (!localsMet.Contains(local)) localsMet.Add(local);
        }
        IGotoStatement gotoStatement = rootBlock.Statements[i] as IGotoStatement;
        if (gotoStatement != null) break;
        IConditionalStatement conditionalStatement = rootBlock.Statements[i] as IConditionalStatement;
        if (conditionalStatement != null) break;
        ISwitchStatement switchStatement = rootBlock.Statements[i] as ISwitchStatement;
        if (switchStatement != null) break;
        IForEachStatement foreachStatement = rootBlock.Statements[i] as IForEachStatement;
        if (foreachStatement != null) break;
        IForStatement forStatement = rootBlock.Statements[i] as IForStatement;
        if (forStatement != null) break;
        ITryCatchFinallyStatement tryStatement = rootBlock.Statements[i] as ITryCatchFinallyStatement;
        if (tryStatement != null) break;
      }
      // For the remaining locals in the body, generate local declarations. 
      foreach (var localDef in topLevelLocals) {
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

    class LocalFinder : BaseCodeTraverser {
      List<ILocalDefinition> foundLocals = new List<ILocalDefinition>();

      public List<ILocalDefinition> FoundLocals {
        get { return foundLocals; }
      }

      public override void Visit(ILocalDefinition localDefinition) {
        if (!foundLocals.Contains(localDefinition)) foundLocals.Add(localDefinition);
        base.Visit(localDefinition);
      }
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