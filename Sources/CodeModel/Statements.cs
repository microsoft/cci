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
using System.Collections.Generic;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci {
  /// <summary>
  /// A statement that asserts that a condition must always be true when execution reaches it. For example the assert statement of Spec#
  /// or a call to System.Diagnostics.Debug.Assert in C#.
  /// This node must be replaced before converting the Code Model to IL.
  /// </summary>
  public interface IAssertStatement : IStatement {
    /// <summary>
    /// A condition that must be true when execution reaches this statement.
    /// </summary>
    IExpression Condition { get; }

    /// <summary>
    /// An optional expression that is associated with the assertion. Generally, it would
    /// be a message that was written at the same time as the contract and is meant to be used as a description
    /// when the contract fails.
    /// </summary>
    IExpression/*?*/ Description { get; }

    /// <summary>
    /// The original source representation of the assertion.
    /// </summary>
    /// <remarks>
    /// Normally this would be extracted directly from the source file.
    /// The expectation is that one would translate the Condition into
    /// a source string in a source language appropriate for
    /// the particular tool environment, e.g., when doing static analysis,
    /// in the language the client code uses, not the language the contract
    /// was written.
    /// </remarks>
    string/*?*/ OriginalSource { get; }

    /// <summary>
    /// True if a static verification tool has determined that the condition will always be true when execution reaches this statement.
    /// </summary>
    bool HasBeenVerified { get; }
  }

  /// <summary>
  /// A statement that assumes that a condition will always be true when execution reaches it. For example the assume statement of Spec#.
  /// This node must be replaced before converting the Code Model to IL.
  /// </summary>
  public interface IAssumeStatement : IStatement {
    /// <summary>
    /// A condition that will be true when execution reaches this statement.
    /// </summary>
    IExpression Condition { get; }

    /// <summary>
    /// An optional expression that is associated with the assumption. Generally, it would
    /// be a message that was written at the same time as the contract and is meant to be used as a description
    /// when the contract fails.
    /// </summary>
    IExpression/*?*/ Description { get; }

    /// <summary>
    /// The original source representation of the assumption.
    /// </summary>
    /// <remarks>
    /// Normally this would be extracted directly from the source file.
    /// The expectation is that one would translate the Condition into
    /// a source string in a source language appropriate for
    /// the particular tool environment, e.g., when doing static analysis,
    /// in the language the client code uses, not the language the contract
    /// was written.
    /// </remarks>
    string/*?*/ OriginalSource { get; }
  }

  /// <summary>
  /// A delimited collection of statements to execute in a new (nested) scope.
  /// </summary>
  public interface IBlockStatement : IStatement {
    /// <summary>
    /// The statements making up the block.
    /// </summary>
    IEnumerable<IStatement> Statements { get; }

    /// <summary>
    /// True if, by default, all arithmetic expressions in the block must be checked for overflow. This setting is inherited by nested blocks and
    /// can be overridden by nested blocks and expressions.
    /// </summary>
    bool UseCheckedArithmetic { get; }
  }

  /// <summary>
  /// Terminates execution of the innermost loop statement or switch case containing this statement directly or indirectly.
  /// </summary>
  public interface IBreakStatement : IStatement {
  }

  /// <summary>
  /// Represents a catch clause of a try-catch statement or a try-catch-finally statement. 
  /// </summary>
  public interface ICatchClause : IObjectWithLocations {
    /// <summary>
    /// The statements within the catch clause.
    /// </summary>
    IBlockStatement Body { get; }

    /// <summary>
    /// The local that contains the exception instance when executing the catch clause body.
    /// </summary>
    ILocalDefinition ExceptionContainer { get; }

    /// <summary>
    /// The type of the exception to handle.
    /// </summary>
    ITypeReference ExceptionType { get; }

    /// <summary>
    /// A condition that must evaluate to true if the catch clause is to be executed. 
    /// May be null, in which case any exception of ExceptionType will cause the handler to execute.
    /// </summary>
    IExpression/*?*/ FilterCondition { get; }

  }

  /// <summary>
  /// An object that represents a statement consisting of two sub statements and a condition that governs which one of the two gets executed. Most languages refer to this as an "if statement".
  /// </summary>
  public interface IConditionalStatement : IStatement {
    /// <summary>
    /// The expression to evaluate as true or false.
    /// </summary>
    IExpression Condition { get; }

    /// <summary>
    /// Statement to execute if the conditional expression evaluates to true. 
    /// </summary>
    IStatement TrueBranch { get; }

    /// <summary>
    /// Statement to execute if the conditional expression evaluates to false. 
    /// </summary>
    IStatement FalseBranch { get; }
  }

  /// <summary>
  /// Terminates execution of the loop body containing this statement directly or indirectly and continues on to the loop exit condition test.
  /// </summary>
  public interface IContinueStatement : IStatement {
  }

  /// <summary>
  /// Signals a breakpoint to an attached debugger.
  /// </summary>
  public interface IDebuggerBreakStatement : IStatement {
  }

  /// <summary>
  /// Do statements until condition. Tests the condition after the body. Exits when the condition is true.
  /// </summary>
  public interface IDoUntilStatement : IStatement {
    /// <summary>
    /// The body of the loop.
    /// </summary>
    IStatement Body { get; }

    /// <summary>
    /// The condition to evaluate as false or true.
    /// </summary>
    IExpression Condition { get; }
  }

  /// <summary>
  /// An empty statement.
  /// </summary>
  public interface IEmptyStatement : IStatement {
    /// <summary>
    /// True if this statement is a sentinel that should never be reachable.
    /// </summary>
    bool IsSentinel { get; }
  }

  /// <summary>
  /// An object that represents a statement that consists of a single expression.
  /// </summary>
  public interface IExpressionStatement : IStatement {
    /// <summary>
    /// The expression.
    /// </summary>
    IExpression Expression { get; }
  }

  /// <summary>
  /// Represents a foreach statement. Executes the loop body for each element of a collection.
  /// </summary>
  public interface IForEachStatement : IStatement {
    /// <summary>
    /// The body of the loop.
    /// </summary>
    IStatement Body { get; }

    /// <summary>
    /// An epxression resulting in an enumerable collection of values (an object implementing System.Collections.IEnumerable).
    /// </summary>
    IExpression Collection { get; }

    /// <summary>
    /// The foreach loop variable that holds the current element from the collection.
    /// </summary>
    ILocalDefinition Variable { get; }

  }

  /// <summary>
  /// Represents a for statement, or a loop through a block of statements, using a test expression as a condition for continuing to loop.
  /// </summary>
  public interface IForStatement : IStatement {
    /// <summary>
    /// The statements making up the body of the loop.
    /// </summary>
    IStatement Body { get; }

    /// <summary>
    /// The expression to evaluate as true or false, which determines if the loop is to continue.
    /// </summary>
    IExpression Condition { get; }

    /// <summary>
    /// Statements that are called after each loop cycle, typically to increment a counter.
    /// </summary>
    IEnumerable<IStatement> IncrementStatements { get; }

    /// <summary>
    /// The loop initialization statements.
    /// </summary>
    IEnumerable<IStatement> InitStatements { get; }

  }

  /// <summary>
  /// Represents a goto statement.
  /// </summary>
  public interface IGotoStatement : IStatement {
    /// <summary>
    /// The statement at which the program execution is to continue.
    /// </summary>
    ILabeledStatement TargetStatement { get; }
  }

  /// <summary>
  /// Represents a "goto case x;" or "goto default" statement in C#.
  /// </summary>
  public interface IGotoSwitchCaseStatement : IStatement {
    /// <summary>
    /// The switch statement case clause to which this statement transfers control to.
    /// </summary>
    ISwitchCase TargetCase { get; }

  }

  /// <summary>
  /// An object that represents a labeled statement or a stand-alone label.
  /// </summary>
  public interface ILabeledStatement : IStatement {
    /// <summary>
    /// The label.
    /// </summary>
    IName Label { get; }

    /// <summary>
    /// The associated statement. Contains an empty statement if this is a stand-alone label.
    /// </summary>
    IStatement Statement { get; }
  }

  /// <summary>
  /// An object that represents the declaration of a local variable or constant, with optional initializer.
  /// </summary>
  public interface ILocalDeclarationStatement : IStatement {

    /// <summary>
    /// The initial value of the local variable. This may be null.
    /// </summary>
    IExpression/*?*/ InitialValue { get; }

    /// <summary>
    /// The local variable or constant declared by this statement.
    /// </summary>
    ILocalDefinition LocalVariable { get; }

  }

  /// <summary>
  /// Represents matched monitor enter and exit calls, together with a try-finally to ensure that the exit call always happens.
  /// </summary>
  public interface ILockStatement : IStatement {

    /// <summary>
    /// The statement to execute inside the try body after the monitor has been entered.
    /// </summary>
    IStatement Body { get; }

    /// <summary>
    /// The monitor object (which gets locked when the monitor is entered and unlocked in the finally clause).
    /// </summary>
    IExpression Guard { get; }

  }

  /// <summary>
  /// Pushes a value onto an implicit operand stack.
  /// </summary>
  public interface IPushStatement : IStatement {
    /// <summary>
    /// A value that is to be pushed onto the implicit operand stack.
    /// </summary>
    IExpression ValueToPush { get; }
  }

  /// <summary>
  /// Represents a using statement block (of one or more IDisposable resources).
  /// </summary>
  public interface IResourceUseStatement : IStatement {

    /// <summary>
    /// The body of the resource use statement.
    /// </summary>
    IStatement Body { get; }

    /// <summary>
    /// Statements to initialize local definitions with the resources to use.
    /// </summary>
    IStatement ResourceAcquisitions { get; }

  }

  /// <summary>
  /// Represents a statement that can only appear inside a catch clause or a filter clause and which rethrows the exception that caused the clause to be invoked.
  /// </summary>
  public interface IRethrowStatement : IStatement {
  }

  /// <summary>
  /// Represents a return statement.
  /// </summary>
  public interface IReturnStatement : IStatement {
    /// <summary>
    /// The return value, if any.
    /// </summary>
    IExpression/*?*/ Expression { get; }
  }

  /// <summary>
  /// An executable statement.
  /// </summary>
  public interface IStatement : IObjectWithLocations {

    /// <summary>
    /// Calls the visitor.Visit(T) method where T is the most derived object model node interface type implemented by the concrete type
    /// of the object implementing IStatement. The dispatch method does not invoke Dispatch on any child objects. If child traversal
    /// is desired, the implementations of the Visit methods should do the subsequent dispatching.
    /// </summary>
    void Dispatch(ICodeVisitor visitor);

  }

  /// <summary>
  /// An object representing a switch case.
  /// </summary>
  public interface ISwitchCase : IObjectWithLocations {

    /// <summary>
    /// The statements representing this switch case.
    /// </summary>
    IEnumerable<IStatement> Body { get; }

    /// <summary>
    /// A compile time constant of the same type as the switch expression.
    /// </summary>
    ICompileTimeConstant Expression {
      get;
      //^ requires !this.IsDefault;
    }

    /// <summary>
    /// True if this case will be branched to for all values where no other case is applicable. Only one of these is legal per switch statement.
    /// </summary>
    bool IsDefault {
      get;
    }

  }

  /// <summary>
  /// An object that represents a switch statement. Branches to one of a list of cases based on the value of a single expression.
  /// </summary>
  public interface ISwitchStatement : IStatement {
    /// <summary>
    /// The switch cases.
    /// </summary>
    IEnumerable<ISwitchCase> Cases { get; }

    /// <summary>
    /// The expression to evaluate in order to determine with switch case to branch to.
    /// </summary>
    IExpression Expression { get; }
  }

  /// <summary>
  /// Represents a statement that throws an exception.
  /// </summary>
  public interface IThrowStatement : IStatement {
    /// <summary>
    /// The exception to throw.
    /// </summary>
    IExpression Exception { get; }
  }

  /// <summary>
  /// Represents a try block with any number of catch clauses, any number of filter clauses and, optionally, a finally or fault block.
  /// </summary>
  public interface ITryCatchFinallyStatement : IStatement {
    /// <summary>
    /// The catch clauses.
    /// </summary>
    IEnumerable<ICatchClause> CatchClauses { get; }

    /// <summary>
    /// The body of the finally clause, if any. May be null.
    /// </summary>
    IBlockStatement/*?*/ FinallyBody { get; }

    /// <summary>
    /// The body of the fault clause, if any. May be null.
    /// There is no C# equivalent of a fault clause. It is just like a finally clause, but is only invoked if an exception occurred.
    /// </summary>
    IBlockStatement/*?*/ FaultBody { get; }

    /// <summary>
    /// The body of the try clause.
    /// </summary>
    IBlockStatement TryBody { get; }

  }

  /// <summary>
  /// While condition do statements. Tests the condition before the body. Exits when the condition is true.
  /// </summary>
  public interface IWhileDoStatement : IStatement {
    /// <summary>
    /// The body of the loop.
    /// </summary>
    IStatement Body { get; }

    /// <summary>
    /// The condition to evaluate as false or true.
    /// </summary>
    IExpression Condition { get; }
  }

  /// <summary>
  /// Terminates the iteration of values produced by the iterator method containing this statement.
  /// </summary>
  public interface IYieldBreakStatement : IStatement {
  }

  /// <summary>
  /// Yields the next value in the stream produced by the iterator method containing this statement.
  /// </summary>
  public interface IYieldReturnStatement : IStatement {
    /// <summary>
    /// The value to yield.
    /// </summary>
    IExpression Expression { get; }
  }
}
