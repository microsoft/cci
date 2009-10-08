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
        this.RemovePossibleTryCatchBlock(blockStatement.Statements);
        this.RemoveAssignmentsByOrToThisDotState(blockStatement.Statements, out localsForThisDotState);
        this.ReplaceReturnWithYieldReturn(blockStatement.Statements);
        this.RemoveToplevelSwitch(blockStatement.Statements, localsForThisDotState);
        this.RemoveUnnecessaryGotosAndLabels(blockStatement.Statements);
      }
      return base.Visit(blockStatement);
    }

    /// <summary>
    /// If the body of MoveNext has a try... catch block at the beginning inserted by compiler and the catch block contains
    /// only a call to call this (the closure class).dispose, remove the try block. 
    /// </summary>
    /// <param name="statements"></param>
    void RemovePossibleTryCatchBlock(List<IStatement> statements) {
      bool FindTryBlock = false;
      for (int/* modified in body.*/ i = 0; i < statements.Count; i++) {
        // Skip any local variable declarations
        ILocalDeclarationStatement localDeclaractionStatement = statements[i] as ILocalDeclarationStatement;
        if (localDeclaractionStatement != null) {
          continue;
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
            break;
          }
        }
        break;
      }
      // Remove th call to m_Finally. Note that now m_Finally must be in the toplevel. 
      if (FindTryBlock) {
        for (int/*modified in body*/ i = 0; i < statements.Count; i++) {
          IExpressionStatement expressionStatement = statements[i] as IExpressionStatement;
          if (expressionStatement != null) {
            IMethodCall methodCall = expressionStatement.Expression as IMethodCall;
            if (methodCall != null) {
              if (methodCall.MethodToCall.Name.Value.Contains("m__Finally") && methodCall.ThisArgument is IThisReference) {
                statements.RemoveAt(i);
                break;
              }
            }
          }
        }
      }
      return;
    }

    /// <summary>
    /// Remove any assignment to <code>this.&lt;&gt;?_state</code>. Remember the locals used to hold values of this.&lt;&gt;?state.
    /// Assumption: .net compilers never assign this.&lt;&gt;?state to anything other than a local variable. 
    /// </summary>
    /// <param name="statements"></param>
    /// <param name="thisDotStateLocals"></param>
    void RemoveAssignmentsByOrToThisDotState(List<IStatement> statements, out List<ILocalDefinition> thisDotStateLocals) {
      thisDotStateLocals = new List<ILocalDefinition>();
      for (int i = 0; i < statements.Count; i++) {
        IExpressionStatement/*?*/ expressionStatement = statements[i] as IExpressionStatement;
        if (expressionStatement != null) {
          IAssignment/*?*/ assignment = expressionStatement.Expression as IAssignment;
          if (assignment != null) {
            IFieldReference/*?*/ closureField = assignment.Target.Definition as IFieldReference;
            if (closureField != null && closureField.Name.Value.StartsWith(ClosureFieldPrefix) && closureField.Name.Value.EndsWith(StateFieldPostfix)) {
              statements.RemoveAt(i);
              i--;
              continue;
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
                  continue;
                } else throw new System.ApplicationException("Unexpected: assign this.<>?__state to something other than a local.");
              }
            }
          }
        }
      }
    }

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

    /// <summary>
    /// Replace every return true with yield return, and every return false with yield break. 
    /// </summary>
    /// <param name="statements"></param>
    void ReplaceReturnWithYieldReturn(List<IStatement> statements) {
      IExpression exp = null;
      // First pass, replace this.<>?__current = exp with "yield return exp";
      //             remove return exp, remember its location and the local variable corresponding to exp
      List<ILocalDefinition> returnLocals = new List<ILocalDefinition>();
      List<IName> labelBeforeReturn = new List<IName>();
      ILabeledStatement currentLabeledStatement = null;

      for (int/* modified in body*/ i = 0; i < statements.Count; i++) {
        IStatement statement = statements[i];
        if (IsAssignmentToThisDotField(statement, CurrentFieldPostfix, out exp)) {
          statements.RemoveAt(i);
          YieldReturnStatement yieldReturnStatement = new YieldReturnStatement();
          yieldReturnStatement.Expression = exp;
          yieldReturnStatement.Locations = new List<ILocation>();
          foreach (ILocation loc in statement.Locations) yieldReturnStatement.Locations.Add(loc);
          statements.Insert(i, yieldReturnStatement);
          continue;
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
            throw new System.ApplicationException("return in move next does not return a local variable.");
          }
        }
        ILabeledStatement labeledStatement = statement as ILabeledStatement;
        if (labeledStatement != null) {
          currentLabeledStatement = labeledStatement;
        }
      }

      // Second pass: remove goto statement to the label before return statement
      //              remove assignment to locals that holds the return value if the source is true
      //              replace it with yield break if the source is false;
      for (int/*modified in body.*/ i = 0; i < statements.Count; i++) {
        IStatement statement = statements[i];
        IGotoStatement gotoStatement = statement as IGotoStatement;
        if (gotoStatement != null) {
          if (labelBeforeReturn.Contains(gotoStatement.TargetStatement.Label)) { statements.RemoveAt(i); i--; } else continue;
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
                  foreach (ILocation loc in assignment.Locations) yieldBreakStatement.Locations.Add(loc);
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
      }
    }

    /// <summary>
    /// Remove the switch statement, if any, on <code>this.&lt;&gt;?_state</code>
    /// </summary>
    /// <param name="statements"></param>
    /// <param name="localForThisDotStates"></param>
    void RemoveToplevelSwitch(List<IStatement> statements, List<ILocalDefinition> localForThisDotStates) {
      List<IStatement> toBeRemoved = new List<IStatement>(); // holds all the statements to be removed.
      // First: collect the statements in the switch case bodies for continuing/end states
      // These are bound to be removed. 
      for (int/*modified in body*/ i = 0; i < statements.Count; i++) {
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
            if (boundExpression.Instance == null && localDefinition != null && localForThisDotStates.Contains(localDefinition)) {
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
              statements.RemoveAt(i); break;
            }
          }
        }
      }

      List<IName> continueingTargets = new List<IName>();
      // Second: Find Labels that follow a yield return. The goto chain from the entrance point of a continueing state
      // will end at these labels. 
      int state = 0;
      for (int i = 0; i < statements.Count; i++) {
        IStatement statement = statements[i];
        if (state == 0 && statement is IYieldReturnStatement)
          state = 1;
        ILabeledStatement labeledStatement = statement as ILabeledStatement;
        if (state == 1 && labeledStatement != null) {
          if (!continueingTargets.Contains(labeledStatement.Label)) continueingTargets.Add(labeledStatement.Label);
          state = 0;
        }
      }

      // Now, follow the link from tobeRemoved until labels in continueingTargets is hit
      int count = toBeRemoved.Count;
      for (int i = 0; i < count; i++) {
        IGotoStatement gotoStatement = toBeRemoved[i] as IGotoStatement;
        while (gotoStatement != null) {
          IName label = gotoStatement.TargetStatement.Label;
          for (int j = 0; j < statements.Count; j++) {
            ILabeledStatement labeledStatement = statements[j] as ILabeledStatement;
            if (labeledStatement != null && labeledStatement.Label.Equals(label)) {
              bool hitGoto = false;
              while (!hitGoto && j < statements.Count) {
                gotoStatement = statements[j] as IGotoStatement;
                if (gotoStatement != null) {
                  if (continueingTargets.Contains(gotoStatement.TargetStatement.Label))
                    gotoStatement = null;
                  hitGoto = true;
                }
                if (!toBeRemoved.Contains(statements[j])) toBeRemoved.Add(statements[j]);
                j++;
              }
              break;
            }
          }
        }
      }

      // Remove all those in toBeReomoved
      for (int i = 0; i < statements.Count; i++) {
        if (toBeRemoved.Contains(statements[i])) {
          statements.RemoveAt(i); i--;
        }
      }
    }

    /// <summary>
    /// Remove the unnecessary gotos and labels at the beginning of the method body.
    /// </summary>
    /// <param name="statements"></param>
    void RemoveUnnecessaryGotosAndLabels(List<IStatement> statements) {
      IGotoStatement/*?*/ gotoStatement = null;
      int state = 0;
      for (int/*modified in body*/ i = 0; i < statements.Count; i++) {
        ILabeledStatement labeledStatement = statements[i] as ILabeledStatement;
        if (i == 0 && labeledStatement != null && state == 0) {
          statements.RemoveAt(0);
          i--;
          continue;
        }
        if (i == 0 && gotoStatement == null && state == 0) {
          gotoStatement = statements[i] as IGotoStatement;
          state = 1;
          continue;
        }
        if (i == 1 && gotoStatement != null && state == 1) {
          IGotoStatement deadGotoStatement = statements[i] as IGotoStatement;
          if (deadGotoStatement != null) {
            statements.RemoveAt(1); i--; continue;
          }
        }
        if (gotoStatement != null) {
          labeledStatement = statements[i] as ILabeledStatement;
          if (labeledStatement != null && labeledStatement.Label == gotoStatement.TargetStatement.Label && state == 1) {
            state = 2;
            if (i == 1) {
              // The target of the first goto is right after, remove both the first goto and its target. 
              statements.RemoveAt(i); i--;
              statements.RemoveAt(0); i--;
            }
            continue;
          }
          IGotoStatement nextGotoStatement = statements[i] as IGotoStatement;
          if (nextGotoStatement != null && state == 2) {
            gotoStatement = nextGotoStatement;
            statements.RemoveAt(i); i--; state = 3;
            continue;
          }
          if (nextGotoStatement != null && state == 3) {
            statements.RemoveAt(i); i--; state = 4;
            continue;
          }
        }
        if (state != 0 && state != 1) break;
      }
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
        IFieldReference/*?*/ closureFieldRef = targetExpression.Definition as IFieldReference;
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
  /// It is probably sufficient to add the declarations at the top level (not for example, in a try block). 
  /// </summary>
  internal class ClosureLocalVariableDeclarationAdder : MethodBodyMutator {
    bool IsTopLevel;
    internal ClosureLocalVariableDeclarationAdder(MoveNextSourceMethodBody sourceMethodBody)
      : base(sourceMethodBody.host, true) {
    }

    public IBlockStatement Transform(IBlockStatement block) {
      IsTopLevel = true;
      return this.Visit(block);
    }

    public override IBlockStatement Visit(BlockStatement blockStatement) {
      if (IsTopLevel) {
        IsTopLevel = false;
        this.AddLocalDeclarationIfNecessary(blockStatement.Statements, new Dictionary<ILocalDefinition, bool>());
      }
      return base.Visit(blockStatement);
    }

    /// <summary>
    /// Turn an assignment to a local variable that is not yet declared into a local variable declaration statement.
    /// </summary>
    /// <param name="statements"></param>
    /// <param name="locals">locals[l] is true if l has been declared and initialized, false if declared but not initialized.
    /// l is in locals only if it has been declared. 
    /// </param>
    void AddLocalDeclarationIfNecessary(List<IStatement> statements, Dictionary<ILocalDefinition, bool> locals) {
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
            ILocalDefinition localDefinition = assignment.Target.Definition as ILocalDefinition;
            if (localDefinition != null && assignment.Source is CreateObjectInstance && (!locals.ContainsKey(localDefinition) || !locals[localDefinition])) {
              statements.RemoveAt(i);
              LocalDeclarationStatement localDeclarationStatement = new LocalDeclarationStatement() {
                LocalVariable = localDefinition,
                Locations = new List<ILocation>(expressionStatement.Locations),
                InitialValue = assignment.Source
              };
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
