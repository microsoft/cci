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
using System;
using System.Diagnostics.Contracts;
using Microsoft.Cci.UtilityDataStructures;
using System.Collections.Generic;
using Microsoft.Cci.MutableCodeModel;

namespace Microsoft.Cci.Analysis {

  /// <summary>
  /// Implemented by an object that implements a Boolean Satisfibility Solver.
  /// </summary>
  public interface ISatSolver {

    /// <summary>
    /// An expression for which ISatSolverContext.Check always returns null.
    /// </summary>
    ISatExpressionWrapper Dummy { get; }

    /// <summary>
    /// An expression for which ISatSolverContext.Check always returns false.
    /// </summary>
    ISatExpressionWrapper False { get; }

    /// <summary>
    /// Provides a context that can hold a number of boolean expressions. 
    /// The solver is used to see if there exists an assignment of values to variables that will make all of the Boolean expressions true.
    /// </summary>
    ISatSolverContext GetNewContext();

    /// <summary>
    /// Returns an ISatExpressionWrapper instance that corresponds to the given operation. The operation takes no arguments, so it is expected
    /// to be a constant value or a variable reference. May return null if operation cannot be mapped to an expression.
    /// </summary>
    ISatExpressionWrapper/*?*/ MakeExpression(IOperation operation, ITypeReference expressionType);

    /// <summary>
    /// Returns an ISatExpressionWrapper instance that corresponds to the given operation. The operation takes a single argument, so it is expected
    /// to involve a unary operator, such as - or ! or ~. May return null if operation cannot be mapped to an expression.
    /// </summary>
    ISatExpressionWrapper/*?*/ MakeExpression(IOperation operation, ITypeReference expressionType, ISatExpressionWrapper operand1);

    /// <summary>
    /// Returns an ISatExpressionWrapper instance that corresponds to the given operation. The operation takes two arguments, so it is expected
    /// to involve a binary operator, such as * or / or ^. May return null if operation cannot be mapped to an expression.
    /// </summary>
    ISatExpressionWrapper/*?*/ MakeExpression(IOperation operation, ITypeReference expressionType, ISatExpressionWrapper operand1, ISatExpressionWrapper operand2);

    /// <summary>
    /// An expression for which ISatSolverContext.Check always returns true.
    /// </summary>
    ISatExpressionWrapper True { get; }

    //TODO: add a way to make predicates that check for overflow. I.e. like MakeExpression, but the result is a boolean expression that indicates overflow can happen if it is satisfiable.

  }

  /// <summary>
  /// A context that can hold a number of boolean expressions. 
  /// The solver is used to see if there exists an assignment of values to variables that will make all of the Boolean expressions true.
  /// </summary>
  public interface ISatSolverContext {

    /// <summary>
    /// Adds a boolean expression to the context.
    /// </summary>
    void Add(ISatExpressionWrapper expression);

    /// <summary>
    /// Adds the inverse of the given boolean expression to the context.
    /// </summary>
    void AddInverse(ISatExpressionWrapper expression);

    /// <summary>
    /// Checks if there exists an assigment of values to variables that will make all of the Boolean expressions in the context true.
    /// Since this problem is not decidable, the solver may not be able to return an answer, in which case the return result is null
    /// rather than false or true.
    /// </summary>
    bool? Check();

    /// <summary>
    /// Creates a check point from the expressions currently in the context. Any expressions added to the context after this call will be
    /// discarded when a corresponding call is made to RestoreCheckPoint.
    /// </summary>
    void MakeCheckPoint();

    /// <summary>
    /// The number of check points that have been created.
    /// </summary>
    uint NumberOfCheckPoints { get; }

    /// <summary>
    /// Discards any expressions added to the context since the last call to MakeCheckPoint. At least one check point must exist.
    /// </summary>
    void RestoreCheckPoint();
  }

  /// <summary>
  /// A wrapper for an expression encoded in a format understood by the SAT solver.
  /// </summary>
  public interface ISatExpressionWrapper {

    /// <summary>
    /// The type of value this expression results in.
    /// </summary>
    ITypeReference Type { get; }

    /// <summary>
    /// Unwraps the wrapped expression, returning a value of the type expected by the SAT solver.
    /// </summary>
    T Unwrap<T>();
  }

  internal class SatSolverHelper<Instruction> where Instruction : Microsoft.Cci.Analysis.Instruction, new() {

    internal SatSolverHelper(ISatSolver satSolver) {
      Contract.Requires(satSolver != null);
      this.satSolver = satSolver;
      this.expressionMap = new Hashtable<Instruction, object>();
      this.andOp = new Operation() { OperationCode = OperationCode.And };
      this.orOp = new Operation() { OperationCode = OperationCode.Or };
    }

    IOperation andOp;
    IOperation orOp;
    ISatSolver satSolver;
    Hashtable<Instruction, object> expressionMap;

    [ContractInvariantMethod]
    private void ObjectInvariant() {
      Contract.Invariant(this.satSolver != null);
      Contract.Invariant(this.expressionMap != null);
    }

    internal ISatExpressionWrapper GetSolverExpressionFor(Instruction expression) {
      Contract.Requires(expression != null);
      Contract.Ensures(Contract.Result<ISatExpressionWrapper>() != null);

      var wrapper = this.expressionMap[expression] as ISatExpressionWrapper;
      if (wrapper == null) {
        var operand1 = expression.Operand1 as Instruction;
        if (operand1 == null) {
          wrapper = this.satSolver.MakeExpression(expression.Operation, expression.Type);
        } else {
          var operand1wrapper = this.GetSolverExpressionFor(operand1);
          var operand2 = expression.Operand2 as Instruction;
          if (operand2 == null) {
            wrapper = this.satSolver.MakeExpression(expression.Operation, expression.Type, operand1wrapper);
          } else {
            var operand2wrapper = this.GetSolverExpressionFor(operand2);
            wrapper = this.satSolver.MakeExpression(expression.Operation, expression.Type, operand1wrapper, operand2wrapper);
          }
        }
        if (wrapper == null) wrapper = this.satSolver.Dummy;
        Contract.Assume(wrapper != null);
        this.expressionMap[expression] = wrapper;
      }
      return wrapper;
    }

    internal ISatExpressionWrapper/*?*/ GetSolverExpressionFor(List<List<Instruction>> listOfConjunctions) {
      Contract.Requires(listOfConjunctions != null);

      var n = listOfConjunctions.Count;
      if (n == 0) return null;
      ISatExpressionWrapper/*?*/ disjunct = null;
      for (int i = 0; i < n; i++) {
        if (listOfConjunctions[i] == null) return null;
        var conjunct = this.GetSolverExpressionFor(listOfConjunctions[i]);
        if (conjunct == null) return null;
        if (disjunct == null)
          disjunct = conjunct;
        else
          disjunct = this.satSolver.MakeExpression(this.orOp, disjunct.Type, disjunct, conjunct);
      }
      return disjunct;
    }

    private ISatExpressionWrapper/*?*/ GetSolverExpressionFor(List<Instruction> listOfExpressions) {
      Contract.Requires(listOfExpressions != null);

      var n = listOfExpressions.Count;
      if (n == 0) return null;
      ISatExpressionWrapper/*?*/ conjunct = null;
      for (int i = 0; i < n; i++) {
        if (listOfExpressions[i] == null) return null;
        var expression = this.GetSolverExpressionFor(listOfExpressions[i]);
        if (conjunct == null)
          conjunct = expression;
        else
          conjunct = this.satSolver.MakeExpression(this.andOp, conjunct.Type, conjunct, expression);
      }
      return conjunct;
    }

    internal ISatSolver SatSolver {
      get {
        Contract.Ensures(Contract.Result<ISatSolver>() != null);
        return this.satSolver;
      }
    }


  }

}