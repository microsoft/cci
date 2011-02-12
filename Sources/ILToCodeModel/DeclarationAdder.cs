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
using Microsoft.Cci.MutableCodeModel;

namespace Microsoft.Cci.ILToCodeModel {

  /// <summary>
  /// Add LocalDeclarationStatement instances to the appropriate blocks.
  /// </summary>
  /// <remarks>
  /// The metadata model does not have the concept of block scopes. All local variables used in a method body are listed in the LocalVariables property of IMethodBody.
  /// The CodeModel on the other hand, has explicit blocks and these blocks contain explicit LocalDeclarationStatement instances that declare the variables that are local
  /// to a block. This visitor decompiles the MetaData model to the CodeModel by inserting (adding) the necessary LocalDeclarationStatement instances in the 
  /// appropriate places. 
  /// </remarks>
  internal class DeclarationAdder : BaseCodeTraverser {

    Dictionary<ILocalDefinition, bool> declaredLocals = new Dictionary<ILocalDefinition, bool>();

    internal void Visit(SourceMethodBody body, BasicBlock rootBlock) {
      this.Visit(rootBlock);
      //Now add declarations for any locals declared only on the body (for example temporary variables introduced by Unstacker).
      List<IStatement> prelude = new List<IStatement>();
      var localsAndTemps = body.LocalVariables;
      if (body.localVariablesAndTemporaries != null) localsAndTemps = body.localVariablesAndTemporaries;
      this.AddDeclarationsWithInitialValues(localsAndTemps, rootBlock);
      foreach (var localDef in localsAndTemps) {
        if (this.declaredLocals.ContainsKey(localDef)) continue;
        LocalDeclarationStatement localDecl = new LocalDeclarationStatement();
        localDecl.LocalVariable = localDef;
        prelude.Add(localDecl);
      }
      if (prelude.Count > 0)
        rootBlock.Statements.InsertRange(0, prelude); //TODO: use pdb info to insert them in the same order they appear in the source
    }

    // For the first assignment to a local variable in a block before a control statement is hit,
    // if the local variable is not mentioned previously, we turn this assignment into a local declaration. 
    private void AddDeclarationsWithInitialValues(IEnumerable<ILocalDefinition> localVariables, BasicBlock block) {
      List<ILocalDefinition> topLevelLocals = new List<ILocalDefinition>(localVariables);
      List<ILocalDefinition> localsMet = new List<ILocalDefinition>();
      for (int i = 0; i < block.Statements.Count; i++) {
        if (topLevelLocals.Count == 0) break;
        IExpressionStatement expressionStatement = block.Statements[i] as IExpressionStatement;
        if (expressionStatement != null) {
          IAssignment assignment = expressionStatement.Expression as IAssignment;
          if (assignment != null) {
            ILocalDefinition localDef = assignment.Target.Definition as ILocalDefinition;
            if (localDef != null && topLevelLocals.Contains(localDef) && !localsMet.Contains(localDef) && !this.declaredLocals.ContainsKey(localDef)) {
              LocalDeclarationStatement localDecl = new LocalDeclarationStatement() {
                LocalVariable = localDef, InitialValue = assignment.Source, Locations = new List<ILocation>(expressionStatement.Locations),
              };
              this.declaredLocals.Add(localDef, true);
              block.Statements[i] = localDecl;
              topLevelLocals.Remove(localDef);
              localsMet.Add(localDef);
            }
          }
        }
        LocalFinder finder = new LocalFinder();
        finder.Visit(block.Statements[i]);
        foreach (ILocalDefinition local in finder.FoundLocals) {
          if (!localsMet.Contains(local)) localsMet.Add(local);
        }
        //Once we see a statement that can transfer control somewhere else, we
        //no longer know that any subsequent assignment dominates all references
        //and hence cannot postpone adding the declaration until we can unify it with the assignment.
        IGotoStatement gotoStatement = block.Statements[i] as IGotoStatement;
        if (gotoStatement != null) break;
        IConditionalStatement conditionalStatement = block.Statements[i] as IConditionalStatement;
        if (conditionalStatement != null) break;
        ISwitchStatement switchStatement = block.Statements[i] as ISwitchStatement;
        if (switchStatement != null) break;
        IForEachStatement foreachStatement = block.Statements[i] as IForEachStatement;
        if (foreachStatement != null) break;
        IForStatement forStatement = block.Statements[i] as IForStatement;
        if (forStatement != null) break;
        ITryCatchFinallyStatement tryStatement = block.Statements[i] as ITryCatchFinallyStatement;
        if (tryStatement != null) break;
      }
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

      public override void VisitReference(ILocalDefinition local) {
        if (!foundLocals.Contains(local)) foundLocals.Add(local);
        base.Visit(local);
      }

    }

    public override void Visit(IBlockStatement block) {
      BasicBlock basicBlock = (BasicBlock)block;
      List<ILocalDefinition>/*?*/ localsInCurrentScope = basicBlock.LocalVariables;
      if (localsInCurrentScope != null) {
        this.AddDeclarationsWithInitialValues(localsInCurrentScope, basicBlock);
        List<IStatement> prelude = new List<IStatement>(localsInCurrentScope.Count);
        foreach (ILocalDefinition localDef in localsInCurrentScope) {
          if (this.declaredLocals.ContainsKey(localDef)) continue;
          LocalDeclarationStatement localDecl = new LocalDeclarationStatement();
          localDecl.LocalVariable = localDef;
          prelude.Add(localDecl);
        }
        if (prelude.Count > 0)
          basicBlock.Statements.InsertRange(0, prelude); //TODO: use pdb info to insert them in the same order they appear in the source
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