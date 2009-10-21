//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System.Collections.Generic;
using Microsoft.Cci.MutableCodeModel;

namespace Microsoft.Cci.ILToCodeModel {
  internal class MoveNextDecompiler : MethodBodyMutator {
    public const string StateFieldPostfix = "__state";
    public const string ClosureFieldPrefix = "<>";
    public const string CurrentFieldPostfix = "__current";
    public const string ThisFieldPostfix = "__this";
    public const string InitialThreadIdPostfix = "__initialThreadId";
    ITypeDefinition containingType;
    MoveNextSourceMethodBody sourceMethodBody;
    bool IsTopLevel;
    int initialStateValue = 0;
    internal MoveNextDecompiler(MoveNextSourceMethodBody sourceMethodBody)
      : base(sourceMethodBody.host, true) {
      this.containingType = sourceMethodBody.ilMethodBody.MethodDefinition.ContainingTypeDefinition;
      this.sourceMethodBody = sourceMethodBody;
    }

    public IBlockStatement Decompile(IBlockStatement block) {
      IsTopLevel = true;
      return this.Visit(block);
    }

    public override IBlockStatement Visit(BlockStatement blockStatement) {
      if (IsTopLevel) {
        IsTopLevel = false;
        List<ILocalDefinition> localsForThisDotState;
        this.RemovePossibleTryCatchBlock(blockStatement);
        this.RemoveAssignmentsByOrToThisDotState(blockStatement, out localsForThisDotState);
        this.ReplaceReturnWithYieldReturn(blockStatement);
        this.RemoveToplevelSwitch(blockStatement, localsForThisDotState);
        this.RemoveUnnecessaryGotosAndLabels(blockStatement);
        this.RemoveUnreferencedTempVariables(blockStatement);
      }
      return base.Visit(blockStatement);
    }

    delegate bool ProcessList<T>(List<T> list, ref int index);
    delegate int IntDependingOnList<T>(List<T> list);

    int Count<T>(List<T> list) { return list.Count; }

    class LinearCodeTraverser : BaseCodeTraverser {
      ProcessList<IStatement> process;
      public LinearCodeTraverser(ProcessList<IStatement> process) {
        this.process = process;
      }
      public override void Visit(IBlockStatement block) {
        BasicBlock blockStatement = block as BasicBlock;
        if (blockStatement != null) {
          for (int i = 0; i < blockStatement.Statements.Count; i++) {
            base.Visit(blockStatement.Statements[i]);
            if (process(blockStatement.Statements, ref i)) continue;
            else break;
          }
        }
      }
    }

    void OverLinearViewOfStatements(BlockStatement blockStatement, ProcessList<IStatement> process) {
      LinearCodeTraverser linearCodeTraverser = new LinearCodeTraverser(process);
      linearCodeTraverser.Visit(blockStatement);
    }

    class RemovePossibleTryCatchBlockHelper {
      public bool FindTryBlock = false;
      public bool process(List<IStatement> statements, ref int i) {
        ILocalDeclarationStatement localDeclaractionStatement = statements[i] as ILocalDeclarationStatement;
        if (localDeclaractionStatement != null) {
          return true;
        }
        ITryCatchFinallyStatement tryCatchFinallyStatement = statements[i] as ITryCatchFinallyStatement;
        if (tryCatchFinallyStatement != null) {
          bool IsCompilerGenerated = false;
          if (tryCatchFinallyStatement.FinallyBody != null) {
            foreach (IStatement finalStatement in tryCatchFinallyStatement.FinallyBody.Statements) {
              IExpressionStatement expressionStatement = finalStatement as IExpressionStatement;
              if (expressionStatement != null) {
                IMethodCall methodCall = expressionStatement.Expression as IMethodCall;
                if (methodCall != null) {
                  if (methodCall.MethodToCall.Name.Value.Contains("Dispose") && methodCall.ThisArgument is IThisReference) {
                    IsCompilerGenerated = true;
                    break;
                  }
                }
              }
            }
          }
          if (tryCatchFinallyStatement.CatchClauses != null) {
            foreach (ICatchClause catchClause in tryCatchFinallyStatement.CatchClauses) {
              foreach (IStatement catchStatement in catchClause.Body.Statements) {
                IExpressionStatement expressionStatement = catchStatement as IExpressionStatement;
                if (expressionStatement != null) {
                  IMethodCall methodCall = expressionStatement.Expression as IMethodCall;
                  if (methodCall != null) {
                    if (methodCall.MethodToCall.Name.Value.Contains("Dispose") && methodCall.ThisArgument is IThisReference) {
                      IsCompilerGenerated = true;
                      break;
                    }
                  }
                }
              }
              if (IsCompilerGenerated) break;
            }
          }
          if (IsCompilerGenerated) {
            foreach (IStatement statement in tryCatchFinallyStatement.TryBody.Statements) {
              statements.Insert(i, statement); i++;
            }
            statements.RemoveAt(i); i--; FindTryBlock = true;
            return false;
          }
        }
        return false;
      }
    }

    /// <summary>
    /// If the body of MoveNext has a try/catch block at the beginning inserted by compiler and the catch block contains
    /// only a call to call this (the closure class).dispose, remove the try block. 
    /// </summary>
    /// <param name="blockStatement"></param>
    void RemovePossibleTryCatchBlock(BlockStatement blockStatement) {
      // Remove the try catch block
      RemovePossibleTryCatchBlockHelper tryCatchRemover = new RemovePossibleTryCatchBlockHelper();
      tryCatchRemover.FindTryBlock = false;
      OverLinearViewOfStatements(blockStatement, tryCatchRemover.process);
      // Remove th call to m_Finally, which is considered the only statement in the finally body.
      if (tryCatchRemover.FindTryBlock) {
        OverLinearViewOfStatements(blockStatement, delegate(List<IStatement> stmts, ref int i) {
          IExpressionStatement expressionStatement = stmts[i] as IExpressionStatement;
          if (expressionStatement != null) {
            IMethodCall methodCall = expressionStatement.Expression as IMethodCall;
            if (methodCall != null) {
              if (methodCall.MethodToCall.Name.Value.Contains("m__Finally") && methodCall.ThisArgument is IThisReference) {
                stmts.RemoveAt(i); // no need to decrement i. 
                return false;
              }
            }
          }
          return true;
        });
      }
      return;
    }

    class RemoveAssignmentsOneThisDotStateHelper {
      public List<ILocalDefinition> thisDotStateLocals;
      public RemoveAssignmentsOneThisDotStateHelper() {
        this.thisDotStateLocals = new List<ILocalDefinition>();
      }
      public bool process(List<IStatement> statements, ref int i) {
        IExpressionStatement/*?*/ expressionStatement = statements[i] as IExpressionStatement;
        if (expressionStatement != null) {
          IAssignment/*?*/ assignment = expressionStatement.Expression as IAssignment;
          if (assignment != null) {
            IFieldReference/*?*/ closureField = assignment.Target.Definition as IFieldReference;
            if (closureField != null && closureField.Name.Value.StartsWith(ClosureFieldPrefix) && closureField.Name.Value.EndsWith(StateFieldPostfix)) {
              statements.RemoveAt(i);
              i--;
              return true;
            }
            IBoundExpression boundExpression = assignment.Source as IBoundExpression;
            if (boundExpression != null) {
              closureField = boundExpression.Definition as IFieldReference;
              if (closureField != null && closureField.Name.Value.StartsWith(ClosureFieldPrefix) && closureField.Name.Value.EndsWith(StateFieldPostfix)) {
                ILocalDefinition thisDotStateLocal = assignment.Target.Definition as ILocalDefinition;
                if (assignment.Target.Instance == null && thisDotStateLocal != null) {
                  if (!thisDotStateLocals.Contains(thisDotStateLocal)) thisDotStateLocals.Add(thisDotStateLocal);
                  statements.RemoveAt(i);
                  i--;
                  return true;
                } else throw new System.ApplicationException("Unexpected: assign this.<>?__state to something other than a local.");
              }
            }
          }
        }
        return true;
      }
    }

    /// <summary>
    /// Remove any assignment to <code>this.&lt;&gt;?_state</code>. Remember the locals used to hold values of this.&lt;&gt;?state.
    /// Assumption: .net compilers never assign this.&lt;&gt;?state to anything other than a local variable. 
    /// </summary>
    /// <param name="blockStatement">A BlockStatement representing the body of MoveNext.</param>
    /// <param name="thisDotStateLocals">Locals that hold the value of thisDotState</param>
    void RemoveAssignmentsByOrToThisDotState(BlockStatement blockStatement, out List<ILocalDefinition> thisDotStateLocals) {
      RemoveAssignmentsOneThisDotStateHelper helper = new RemoveAssignmentsOneThisDotStateHelper();
      OverLinearViewOfStatements(blockStatement, helper.process);
      thisDotStateLocals = helper.thisDotStateLocals;
    }

    class YieldReturnYieldBreakHelper {
      bool IsAssignmentToThisDotField(IStatement/*?*/ statement, string fieldPostfix, out IExpression/*?*/ expression) {
        expression = null;
        IExpressionStatement/*?*/ expressionStatement = statement as IExpressionStatement;
        if (expressionStatement == null) return false;
        IAssignment/*?*/ assignment = expressionStatement.Expression as IAssignment;
        if (assignment == null) return false;
        IFieldReference/*?*/ closureField = assignment.Target.Definition as IFieldReference;
        //if (!(assignment.Target.Instance is IThisReference)) return false;
        if (closureField == null || !closureField.Name.Value.StartsWith(ClosureFieldPrefix) || !closureField.Name.Value.EndsWith(fieldPostfix)) return false;
        expression = assignment.Source;
        return true;
      }

      ILabeledStatement currentLabeledStatement = null;
      public bool ReplaceAndRememberYieldReturn(List<IStatement> statements, ref int i) {
        IExpression exp;
        IStatement statement = statements[i];
        if (IsAssignmentToThisDotField(statement, CurrentFieldPostfix, out exp)) {
          statements.RemoveAt(i);
          YieldReturnStatement yieldReturnStatement = new YieldReturnStatement();
          yieldReturnStatement.Expression = exp;
          yieldReturnStatement.Locations.AddRange(statement.Locations);
          statements.Insert(i, yieldReturnStatement);
          return true;
        }
        IReturnStatement returnStatement = statement as IReturnStatement;
        if (returnStatement != null) {
          statements.RemoveAt(i); i--;
          if (currentLabeledStatement != null) labelBeforeReturn.Add(currentLabeledStatement.Label);
          IBoundExpression boundExpression = returnStatement.Expression as IBoundExpression;
          if (boundExpression != null) {
            ILocalDefinition localDefinition = boundExpression.Definition as ILocalDefinition;
            returnLocals.Add(localDefinition);
          } else {
            return false;
          }
        }
        ILabeledStatement labeledStatement = statement as ILabeledStatement;
        if (labeledStatement != null) {
          currentLabeledStatement = labeledStatement;
        }
        return true;
      }

      public List<ILocalDefinition> returnLocals = new List<ILocalDefinition>();
      public List<IName> labelBeforeReturn = new List<IName>();

      public bool RemoveGotosAndCreateYieldBreak(List<IStatement> statements, ref int i) {
        IStatement statement = statements[i];
        IGotoStatement gotoStatement = statement as IGotoStatement;
        if (gotoStatement != null) {
          if (labelBeforeReturn.Contains(gotoStatement.TargetStatement.Label)) { statements.RemoveAt(i); i--; } else return true;
        }
        IExpressionStatement expressionStatement = statement as IExpressionStatement;
        if (expressionStatement != null) {
          IAssignment assignment = expressionStatement.Expression as IAssignment;
          if (assignment != null) {
            if (assignment.Target.Instance == null && assignment.Target.Definition is ILocalDefinition) {
              if (returnLocals.Contains(assignment.Target.Definition as ILocalDefinition)) {
                ICompileTimeConstant ctc = assignment.Source as ICompileTimeConstant;
                System.Diagnostics.Debug.Assert(ctc != null);
                if ((int)ctc.Value == 1) {
                  statements.RemoveAt(i); i--;
                } else {
                  statements.RemoveAt(i);
                  YieldBreakStatement yieldBreakStatement = new YieldBreakStatement();
                  yieldBreakStatement.Locations.AddRange(expressionStatement.Locations);
                  statements.Insert(i, yieldBreakStatement);
                }
              }
            }
          }
        }
        ILabeledStatement labeledStatement = statement as ILabeledStatement;
        if (labeledStatement != null) {
          if (labelBeforeReturn.Contains(labeledStatement.Label)) { statements.RemoveAt(i); i--; }
        }
        return true;
      }
    }

    /// <summary>
    /// Replace every return true with yield return, and every return false with yield break. 
    /// </summary>
    /// <param name="blockStatement"></param>
    void ReplaceReturnWithYieldReturn(BlockStatement blockStatement) {
      // First pass, replace this.<>?__current = exp with "yield return exp";
      //             remove return exp, remember its location and the local variable corresponding to exp
      YieldReturnYieldBreakHelper yieldInserter = new YieldReturnYieldBreakHelper();
      OverLinearViewOfStatements(blockStatement, yieldInserter.ReplaceAndRememberYieldReturn);

      // Second pass: remove goto statement to the label before return statement
      //              remove assignment to locals that holds the return value if the source is true
      //              replace it with yield break if the source is false;
      OverLinearViewOfStatements(blockStatement, yieldInserter.RemoveGotosAndCreateYieldBreak);
    }

    class RemoveTopLevelSwitchHelper {
      List<IStatement> toBeRemoved = new List<IStatement>(); // holds all the statements to be removed.
      IList<ILocalDefinition> localsForThisDotState;
      int initialStateValue;
      public List<IStatement> ToBeRemoved {
        get {
          return toBeRemoved;
        }
      }
      public RemoveTopLevelSwitchHelper(List<ILocalDefinition> localsForThisDotState, int initialStateValue) {
        this.localsForThisDotState = localsForThisDotState;
        this.initialStateValue = initialStateValue;
      }
      public bool CollectGotosInSwitchBody(List<IStatement> statements, ref int i) {
        IStatement statement = statements[i];
        ISwitchStatement switchStatement = statement as ISwitchStatement;
        if (switchStatement != null) {
          IBoundExpression boundExpression = switchStatement.Expression as IBoundExpression;
          if (boundExpression != null) {
            bool switchOnThisDotState = false;
            IFieldReference/*?*/ thisDotState = boundExpression.Definition as IFieldReference;
            if (thisDotState != null) {
              ISpecializedFieldReference specializedThisDotState = thisDotState as ISpecializedFieldReference;
              if (specializedThisDotState != null) thisDotState = specializedThisDotState.UnspecializedVersion.ResolvedField;
            }
            if (boundExpression.Instance is ThisReference && thisDotState != null) {
              if (thisDotState.Name.Value.StartsWith("<>") && thisDotState.Name.Value.EndsWith(StateFieldPostfix)) {
                switchOnThisDotState = true;
              }
            }
            ILocalDefinition localDefinition = boundExpression.Definition as ILocalDefinition;
            if (boundExpression.Instance == null && localDefinition != null && localsForThisDotState.Contains(localDefinition)) {
              switchOnThisDotState = true;
            }
            if (switchOnThisDotState) {
              foreach (ISwitchCase casee in switchStatement.Cases) {
                ICompileTimeConstant ctc = casee.Expression as ICompileTimeConstant;
                if (ctc != null) {
                  if (!ctc.Equals(CodeDummy.Constant) && (int)ctc.Value == this.initialStateValue) {
                    // Two things needs to be done: 1) If the body contains { l1: goto l2;} delete them, and repeat the process for l2
                    // if it is {l2: goto l3;}.
                    // Often there is a goto right after the l2, which is for the "default case" delete that goto if it exists.
                  } else {
                    foreach (IStatement st in casee.Body) {
                      if (!toBeRemoved.Contains(st))
                        toBeRemoved.Add(st);
                    }
                  }
                }
              }
              // remove the switch itself
              statements.RemoveAt(i); return false;
            }
          }
        }
        return true;
      }
      List<IName> continueingTargets = new List<IName>();
      public int state = 0;

      public bool FindLabelsFollowingYieldReturn(List<IStatement> statements, ref int i) {
        IStatement statement = statements[i];
        if (state == 0 && statement is IYieldReturnStatement)
          state = 1;
        ILabeledStatement labeledStatement = statement as ILabeledStatement;
        if (state == 1 && labeledStatement != null) {
          if (!continueingTargets.Contains(labeledStatement.Label)) continueingTargets.Add(labeledStatement.Label);
          state = 0;
        }
        return true;
      }

      public bool foundGotoTarget;
      public bool hitNextGoto;
      public bool hitContinueTargetOrDefault;
      public IGotoStatement gotoStatement;
      IStatement previousLabelStatement = null;
      public bool FollowGotosInContinuingState(List<IStatement> statements, ref int j) {
        ILabeledStatement labeledStatement = statements[j] as ILabeledStatement;
        if (labeledStatement != null) {
          if (labeledStatement.Label.Equals(gotoStatement.TargetStatement.Label)) {
            foundGotoTarget = true;
            hitNextGoto = false;
            previousLabelStatement = labeledStatement;
            return true;
            //while (!hitNextGoto && j < statements.Count && !hitContinueTargetOrDefault) {
            //  IGotoStatement gotoStmt = statements[j] as IGotoStatement;
            //  if (gotoStmt != null) {
            //    if (continueingTargets.Contains(gotoStatement.TargetStatement.Label)) {
            //      gotoStatement = null;
            //      hitContinueTargetOrDefault = true;
            //    } else
            //      gotoStatement = gotoStmt;
            //      hitNextGoto = true;
            //  }
            //  if (!toBeRemoved.Contains(statements[j])) toBeRemoved.Add(statements[j]);
            //  j++;
            //}
            //return false;
          }
          if (continueingTargets.Contains(labeledStatement.Label)) {
            hitContinueTargetOrDefault = true;
            return false;
          }
        }
        IGotoStatement goStmt = statements[j] as IGotoStatement;
        if (goStmt != null && !hitNextGoto && previousLabelStatement != null) {
          hitNextGoto = true;
          gotoStatement = goStmt;
          previousLabelStatement = null;
          if (!toBeRemoved.Contains(previousLabelStatement)) toBeRemoved.Add(previousLabelStatement);
          if (!toBeRemoved.Contains(goStmt)) toBeRemoved.Add(goStmt);
          return false;
        }
        if (foundGotoTarget) {
          previousLabelStatement = null;
          hitContinueTargetOrDefault = true;
          return false;
        }
        return true;
      }
    }

    /// <summary>
    /// Remove the switch statement, if any, on <code>this.&lt;&gt;?_state</code>
    /// </summary>
    /// <param name="blockStatement"></param>
    /// <param name="localForThisDotStates"></param>
    void RemoveToplevelSwitch(BlockStatement blockStatement, List<ILocalDefinition> localForThisDotStates) {

      // First: collect the statements in the switch case bodies for continuing/end states
      RemoveTopLevelSwitchHelper helper = new RemoveTopLevelSwitchHelper(localForThisDotStates, this.initialStateValue);
      OverLinearViewOfStatements(blockStatement, helper.CollectGotosInSwitchBody);

      // Second: Find Labels that follow a yield return. The goto chain from the entrance point of a continueing state
      // will end at these labels. 
      helper.state = 0;
      OverLinearViewOfStatements(blockStatement, helper.FindLabelsFollowingYieldReturn);

      // Now, follow the link from tobeRemoved until labels in continueingTargets is hit
      for (int i = 0; i < helper.ToBeRemoved.Count; i++) {
        IGotoStatement gotoStatement = helper.ToBeRemoved[i] as IGotoStatement;
        if (gotoStatement == null) continue;
        helper.gotoStatement = gotoStatement;
        bool finished = false;
        while (!finished) {
          helper.foundGotoTarget = false;
          helper.hitNextGoto = false;
          helper.hitContinueTargetOrDefault = false;
          OverLinearViewOfStatements(blockStatement, helper.FollowGotosInContinuingState);
          finished = !helper.foundGotoTarget || !helper.hitNextGoto || helper.hitContinueTargetOrDefault;
        }
      }
      // Remove all those in toBeReomoved
      OverLinearViewOfStatements(blockStatement, delegate(List<IStatement> stmts, ref int i) {
        if (helper.ToBeRemoved.Contains(stmts[i])) {
          stmts.RemoveAt(i); i--;
        }
        return true;
      });
    }

    class RemoveGotosLabelsHelper {
      public int state = 0;
      IGotoStatement gotoStatement = null;
      int preludeSize =0;
      bool preludeFinished = false;
      int depth = -1;
      public bool RemoveGotosLabels(List<IStatement> statements, ref int i) {
        if (i == 0) {
          preludeSize = 0;
          preludeFinished = false;
          depth++;
        }
        ILocalDeclarationStatement localDeclarationStatement = statements[i] as ILocalDeclarationStatement;
        if (localDeclarationStatement != null) {
          if (!preludeFinished) preludeSize++;
          return true;
        } else {
          preludeFinished = true;
        }
        ILabeledStatement labeledStatement = statements[i] as ILabeledStatement;
        if (preludeFinished && i == preludeSize && labeledStatement != null && state == 0 && depth ==0) {
          statements.RemoveAt(i);
          i--;
          return true;
        }
        if (preludeFinished && i == preludeSize  && gotoStatement == null && state == 0) {
          gotoStatement = statements[i] as IGotoStatement;
          if (gotoStatement != null) {
            statements.RemoveAt(i); i--;
            state = 1;
            return true;
          } else {
            return false;
          }
        }
        if (preludeFinished && i == preludeSize && gotoStatement != null && state == 1) {
          IGotoStatement deadGotoStatement = statements[i] as IGotoStatement;
          if (deadGotoStatement != null) {
            statements.RemoveAt(i); i--; return true;
          }
        }
        if (gotoStatement != null) {
          labeledStatement = statements[i] as ILabeledStatement;
          if (labeledStatement != null && labeledStatement.Label == gotoStatement.TargetStatement.Label && state == 1) {
            state = 2;
            if (i == preludeSize) {
              statements.RemoveAt(i); i--;
            }
            return true;
          }
          IGotoStatement nextGotoStatement = statements[i] as IGotoStatement;
          if (nextGotoStatement != null && state == 2) {
            gotoStatement = nextGotoStatement;
            statements.RemoveAt(i); i--; state = 3;
            return true;
          }
          if (nextGotoStatement != null && state == 3) {
            statements.RemoveAt(i); i--; state = 4;
            return true;
          }
        }
        if (state != 0 && state != 1) return false;
        return true;
      }
    }

    /// <summary>
    /// Remove the some of the unnecessary gotos.
    /// </summary>
    /// <param name="blockStatement">BlockStatement representing the MoveNext body.</param>
    void RemoveUnnecessaryGotosAndLabels(BlockStatement blockStatement) {
      RemoveGotosLabelsHelper helper = new RemoveGotosLabelsHelper();
      OverLinearViewOfStatements(blockStatement, helper.RemoveGotosLabels);
    }

    class RemoveUnreferencedLocalHelper {
      List<ILocalDefinition> referencedLocals;
      public RemoveUnreferencedLocalHelper(List<ILocalDefinition>/*!*/ referencedLocals) {
        this.referencedLocals = referencedLocals;
      }
      public bool RemoveUnreferencedLocal(List<IStatement> statements, ref int i) {
        ILocalDeclarationStatement localDeclarationStatement = statements[i] as ILocalDeclarationStatement;
        if (localDeclarationStatement != null && !this.referencedLocals.Contains(localDeclarationStatement.LocalVariable)) {
          statements.RemoveAt(i); i--; return true;
        }
        return true;
      }
    }

    class LocalReferencer : BaseCodeTraverser {
      List<ILocalDefinition> referencedLocals = new List<ILocalDefinition>();

      public List<ILocalDefinition> ReferencedLocals {
        get { return referencedLocals; }
      }
      public override void Visit(IAddressableExpression addressableExpression) {
        ILocalDefinition localdef = addressableExpression.Instance as ILocalDefinition;
        if (localdef != null && !this.referencedLocals.Contains(localdef)) this.referencedLocals.Add(localdef);
        base.Visit(addressableExpression);
      }

      public override void Visit(IBoundExpression boundExpression) {
        ILocalDefinition localdef = boundExpression.Instance as ILocalDefinition;
        if (localdef != null && !this.referencedLocals.Contains(localdef)) this.referencedLocals.Add(localdef);
        base.Visit(boundExpression);
      }

      public override void Visit(ITargetExpression targetExpression) {
        ILocalDefinition localdef = targetExpression.Instance as ILocalDefinition;
        if (localdef != null && !this.referencedLocals.Contains(localdef))
          this.referencedLocals.Add(localdef);
        base.Visit(targetExpression);
      }
    }

    void RemoveUnreferencedTempVariables(BlockStatement blockStatement) {
      LocalReferencer localReferencer = new LocalReferencer();
      localReferencer.Visit(blockStatement);
      RemoveUnreferencedLocalHelper helper = new RemoveUnreferencedLocalHelper(localReferencer.ReferencedLocals);
      OverLinearViewOfStatements(blockStatement, helper.RemoveUnreferencedLocal);
    }
  }

  /// <summary>
  /// An iterator method is compiled to a closure class with a MoveNext method. This class represents a block of decompiled
  /// statements in the MoveNext method, often the body of the MoveNext. Its TransformedBlock Property is the result of transforming
  /// the MoveNext block back to the block meaningful in the original method. 
  /// </summary>
  internal class DecompiledMoveNextBlock {
    BlockStatement/*!*/ decompiledMoveNextBody;
    IMethodDefinition/*!*/ originalMethodDefinition;
    IMethodDefinition/*!*/ moveNextMethodDefinition;
    Dictionary<IFieldDefinition, object> closureFieldMapping;
    Dictionary<IGenericParameter, IGenericParameter> typeParameterMapping;
    MoveNextSourceMethodBody/*!*/ sourceMethodBody;
    internal DecompiledMoveNextBlock(IMethodDefinition originalMethodDefinition, IMethodDefinition moveNextMethod, BlockStatement decompiledMoveNextBody, MoveNextSourceMethodBody sourceMethodBody) {
      this.decompiledMoveNextBody = decompiledMoveNextBody;
      this.originalMethodDefinition = originalMethodDefinition;
      this.moveNextMethodDefinition = moveNextMethod;
      this.sourceMethodBody = sourceMethodBody;
      closureFieldMapping = ComputeFieldParameterMapping(originalMethodDefinition, decompiledMoveNextBody);
      typeParameterMapping = ComputeTypeParameterMapping(originalMethodDefinition, moveNextMethodDefinition);
    }

    Dictionary<IFieldDefinition, object> ComputeFieldParameterMapping(IMethodDefinition/*!*/ originalMethodDefinition,
      IBlockStatement/*!*/ decompiledMoveNextBody) {
      return new ClosureFieldOpenner(originalMethodDefinition, moveNextMethodDefinition, decompiledMoveNextBody).Mapping;
    }

    Dictionary<IGenericParameter, IGenericParameter> ComputeTypeParameterMapping(IMethodDefinition original, IMethodDefinition moveNext) {
      Dictionary<IGenericParameter, IGenericParameter> result = new Dictionary<IGenericParameter, IGenericParameter>();
      ITypeDefinition/*!*/ closureType = moveNext.ContainingType.ResolvedType;
      if (closureType.GenericParameterCount != original.GenericParameterCount) {
        throw new System.ApplicationException("Closure class and the original iterator method do not have same number of type parameters. ");
      }
      foreach (var t1 in closureType.GenericParameters) {
        foreach (var t2 in original.GenericParameters) {
          if (result.ContainsKey(t1)) {
            throw new System.ApplicationException("Duplicate type parameter.");
          }
          result.Add(t1, t2);
        }
      }
      return result;
    }

    /// <summary>
    /// A block to be used in the original method
    /// </summary>
    public IBlockStatement TransformedBlock {
      get {
        IBlockStatement block = new ClosureBlockTransformer(typeParameterMapping, closureFieldMapping, sourceMethodBody).Visit(decompiledMoveNextBody);
        return block;
      }
    }

    class ClosureBlockTransformer : MethodBodyMutator {
      Dictionary<IGenericParameter, IGenericParameter>/*!*/ typeParameterMapping;
      Dictionary<IFieldDefinition, object>/*!*/ fieldMapping;
      internal ClosureBlockTransformer(Dictionary<IGenericParameter, IGenericParameter> typeParameterMapping, Dictionary<IFieldDefinition, object> fieldMapping, MoveNextSourceMethodBody sourceMethodBody)
        : base(sourceMethodBody.host, true) {
        this.typeParameterMapping = typeParameterMapping;
        this.fieldMapping = fieldMapping;
      }

      public override IBlockStatement Visit(BlockStatement blockStatement) {
        return base.Visit(blockStatement);
      }

      public override IAddressableExpression Visit(AddressableExpression addressableExpression) {
        IFieldReference/*?*/ closureFieldRef = addressableExpression.Definition as IFieldReference;
        if (closureFieldRef != null) {
          object localOrParameter = null;
          IFieldDefinition closureFieldDefinition = closureFieldRef.ResolvedField;
          ISpecializedFieldDefinition specialedClosureFieldDef = closureFieldDefinition as ISpecializedFieldDefinition;
          if (specialedClosureFieldDef != null)
            closureFieldDefinition = specialedClosureFieldDef.UnspecializedVersion;
          if (this.fieldMapping.TryGetValue(closureFieldDefinition, out localOrParameter)) {
            addressableExpression.Definition = localOrParameter;
            addressableExpression.Instance = null;
            return addressableExpression;
          }
        }
        return base.Visit(addressableExpression);
      }

      /// <summary>
      /// Replace the field binding in a target expression with an approppriate local or parameter.
      /// </summary>
      /// <param name="targetExpression"></param>
      /// <returns></returns>
      public override ITargetExpression Visit(TargetExpression targetExpression) {
        // ^Requires: targetExpression.Definition is not the field in the closure class that captures THIS of the original.
        IFieldReference/*?*/ closureFieldRef= targetExpression.Definition as IFieldReference;
        if (closureFieldRef != null) {
          object localOrParameter = null;
          IFieldDefinition closureFieldDefinition = closureFieldRef.ResolvedField;
          ISpecializedFieldDefinition specialedClosureFieldDef = closureFieldDefinition as ISpecializedFieldDefinition;
          if (specialedClosureFieldDef != null)
            closureFieldDefinition = specialedClosureFieldDef.UnspecializedVersion;
          if (this.fieldMapping.TryGetValue(closureFieldDefinition, out localOrParameter)) {
            targetExpression.Definition = localOrParameter;
            targetExpression.Instance = null;
            return targetExpression;
          }
        }
        return base.Visit(targetExpression);
      }

      /// <summary>
      /// Replace the field binding in a bound expression with an approppriate local or parameter.
      /// If the field is _this that captures this, replace the whole bound expression with the self
      /// of the original method. 
      /// </summary>
      /// <param name="boundExpression"></param>
      /// <returns></returns>
      public override IExpression Visit(BoundExpression boundExpression) {
        IFieldReference/*?*/ closureFieldRef = boundExpression.Definition as IFieldReference;
        if (closureFieldRef != null) {
          object/*?*/ localOrParameter = null;
          IFieldDefinition closureFieldDefinition = closureFieldRef.ResolvedField;
          ISpecializedFieldDefinition specialedClosureFieldDef = closureFieldDefinition as ISpecializedFieldDefinition;
          if (specialedClosureFieldDef != null)
            closureFieldDefinition = specialedClosureFieldDef.UnspecializedVersion;
          if (this.fieldMapping.TryGetValue(closureFieldDefinition, out localOrParameter)) {
            if (localOrParameter is ThisReference)
              return (ThisReference)localOrParameter;
            boundExpression.Definition = localOrParameter;
            boundExpression.Instance = null;
            return boundExpression;
          }
        }
        return base.Visit(boundExpression);
      }
    }

    class ClosureFieldOpenner {
      IBlockStatement/*!*/ decompiledMoveNextBlock;
      IMethodDefinition/*!*/ originalMethodDefinition;
      IMethodDefinition/*!*/ moveNextMethod;
      public ClosureFieldOpenner(IMethodDefinition/*!*/ originalMethodDefinition, IMethodDefinition/*!*/moveNextMethod, IBlockStatement/*!*/ decompiledMoveNextBlock) {
        this.decompiledMoveNextBlock = decompiledMoveNextBlock;
        this.originalMethodDefinition = originalMethodDefinition;
        this.moveNextMethod = moveNextMethod;
      }

      Dictionary<IFieldDefinition, object>/*?*/ mapping;
      public Dictionary<IFieldDefinition, object>/*!*/ Mapping {
        get {
          if (mapping != null) return mapping;
          mapping = new Dictionary<IFieldDefinition, object>();
          ITypeDefinition/*!*/ closureType = moveNextMethod.ContainingTypeDefinition;
          foreach (var field in closureType.Fields) {
            string fieldName = field.Name.Value;
            bool IsParameter = false;
            foreach (var parameter in originalMethodDefinition.Parameters) {
              if (field.Name.Value.EndsWith("__" + parameter.Name.Value) || field.Name.Value == parameter.Name.Value) {
                mapping[field] = parameter;
                IsParameter = true; break;
              }
            }
            if (IsParameter) continue;
            if (fieldName.StartsWith(MoveNextDecompiler.ClosureFieldPrefix) && fieldName.EndsWith(MoveNextDecompiler.StateFieldPostfix)) continue;
            if (fieldName.StartsWith(MoveNextDecompiler.ClosureFieldPrefix) && fieldName.EndsWith(MoveNextDecompiler.CurrentFieldPostfix)) continue;
            if (fieldName.StartsWith(MoveNextDecompiler.ClosureFieldPrefix) && fieldName.EndsWith(MoveNextDecompiler.InitialThreadIdPostfix)) continue;
            if (fieldName.StartsWith(MoveNextDecompiler.ClosureFieldPrefix) && fieldName.EndsWith(MoveNextDecompiler.ThisFieldPostfix)) {
              mapping[field] = new ThisReference() { Type = field.Type };
              continue;
            }
            LocalDefinition newLocal = new LocalDefinition() { Name = field.Name, Type = field.Type };
            mapping[field] = newLocal;
          }
          return mapping;
        }
      }
    }
  }

  /// <summary>
  /// Turn an assignment to a local variable that is not yet declared into a local variable declaration statement.
  /// 
  /// </summary>
  internal class ClosureLocalVariableDeclarationAdder : MethodBodyMutator {
    Dictionary<ILocalDefinition, bool> locals = new Dictionary<ILocalDefinition, bool>();
    internal ClosureLocalVariableDeclarationAdder(MoveNextSourceMethodBody sourceMethodBody)
      : base(sourceMethodBody.host, true) {
    }

    public IBlockStatement Transform(IBlockStatement block) {
      return this.Visit(block);
    }

    public override IBlockStatement Visit(BlockStatement blockStatement) {
      this.AddLocalDeclarationIfNecessary(blockStatement.Statements);
      return base.Visit(blockStatement);
    }

    /// <summary>
    /// Turn an assignment to a local variable that is not yet declared into a local variable declaration statement.
    /// </summary>
    /// <param name="statements"></param>
    void AddLocalDeclarationIfNecessary(List<IStatement> statements) {
      for (int i = 0; i < statements.Count; i++) {
        ILocalDeclarationStatement localDeclaration = statements[i] as ILocalDeclarationStatement;
        if (localDeclaration != null) {
          if (!locals.ContainsKey(localDeclaration.LocalVariable))
            locals.Add(localDeclaration.LocalVariable, localDeclaration.InitialValue != null);
          continue;
        }
        IExpressionStatement expressionStatement = statements[i] as IExpressionStatement;
        if (expressionStatement != null) {
          IAssignment assignment = expressionStatement.Expression as IAssignment;
          if (assignment != null) {
            ILocalDefinition/*?*/ localDefinition = assignment.Target.Definition as ILocalDefinition;
            if (localDefinition != null && assignment.Source is CreateObjectInstance && (!locals.ContainsKey(localDefinition) || !locals[localDefinition])) {
              statements.RemoveAt(i);
              LocalDeclarationStatement localDeclarationStatement = new LocalDeclarationStatement() {
                LocalVariable = localDefinition,
                InitialValue = assignment.Source
              };
              localDeclarationStatement.Locations.AddRange(expressionStatement.Locations);
              locals[localDefinition] = true;
              statements.Insert(i, localDeclarationStatement);
            }
          }
          continue;
        }
      }
    }
  }
}
