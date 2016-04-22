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
using System.Diagnostics;
using System.Threading;
using System.Diagnostics.Contracts;
//^ using Microsoft.Contracts;

namespace Microsoft.Cci {

#pragma warning disable 1591

  public static class CodeDummy {

    public static IAddressableExpression AddressableExpression {
      get {
        if (CodeDummy.addressableExpression == null)
          Interlocked.CompareExchange(ref CodeDummy.addressableExpression, new DummyAddressableExpression(), null);
        return CodeDummy.addressableExpression;
      }
    }
    private static IAddressableExpression/*?*/ addressableExpression;

    public static IAssignment Assignment {
      get {
        if (CodeDummy.assignment == null)
          Interlocked.CompareExchange(ref CodeDummy.assignment, new DummyAssignment(), null);
        return CodeDummy.assignment;
      }
    }
    private static IAssignment/*?*/ assignment;

    public static IBlockStatement Block {
      get {
        Contract.Ensures(Contract.Result<IBlockStatement>() != null);
        if (CodeDummy.block == null)
          Interlocked.CompareExchange(ref CodeDummy.block, new DummyBlock(), null);
        return CodeDummy.block;
      }
    }
    private static IBlockStatement/*?*/ block;

    public static ICompileTimeConstant Constant {
      get {
        Contract.Ensures(Contract.Result<ICompileTimeConstant>() != null);
        if (CodeDummy.constant == null)
          Interlocked.CompareExchange(ref CodeDummy.constant, new DummyCompileTimeConstant(), null);
        return CodeDummy.constant;
      }
    }
    private static ICompileTimeConstant/*?*/ constant;

    public static ICreateArray CreateArray {
      get {
        if (CodeDummy.createArray == null)
          Interlocked.CompareExchange(ref CodeDummy.createArray, new DummyCreateArray(), null);
        return CodeDummy.createArray;
      }
    }
    private static ICreateArray/*?*/ createArray;

    public static IExpression Expression {
      get {
        Contract.Ensures(Contract.Result<IExpression>() != null);
        if (CodeDummy.expression == null)
          Interlocked.CompareExchange(ref CodeDummy.expression, new DummyExpression(), null);
        return CodeDummy.expression;
      }
    }
    private static IExpression/*?*/ expression;

    public static IGotoStatement GotoStatement {
      get {
        if (CodeDummy.gotoStatement == null)
          Interlocked.CompareExchange(ref CodeDummy.gotoStatement, new DummyGotoStatement(), null);
        return CodeDummy.gotoStatement;
      }
    }
    private static IGotoStatement/*?*/ gotoStatement;

    public static ILabeledStatement LabeledStatement {
      get {
        if (CodeDummy.labeledStatement == null)
          Interlocked.CompareExchange(ref CodeDummy.labeledStatement, new DummyLabeledStatement(), null);
        return CodeDummy.labeledStatement;
      }
    }
    private static ILabeledStatement/*?*/ labeledStatement;

    public static IMethodCall MethodCall {
      get {
        if (CodeDummy.methodCall == null)
          Interlocked.CompareExchange(ref CodeDummy.methodCall, new DummyMethodCall(), null);
        return CodeDummy.methodCall;
      }
    }
    private static IMethodCall/*?*/ methodCall;

    public static ISwitchCase SwitchCase {
      get {
        if (CodeDummy.switchCase == null)
          Interlocked.CompareExchange(ref CodeDummy.switchCase, new DummySwitchCase(), null);
        return CodeDummy.switchCase;
      }
    }
    private static ISwitchCase/*?*/ switchCase;

    public static ISwitchStatement SwitchStatement {
      get {
        if (CodeDummy.switchStatement == null)
          Interlocked.CompareExchange(ref CodeDummy.switchStatement, new DummySwitchStatement(), null);
        return CodeDummy.switchStatement;
      }
    }
    private static ISwitchStatement/*?*/ switchStatement;

    public static ITargetExpression TargetExpression {
      get {
        if (CodeDummy.targetExpression == null)
          Interlocked.CompareExchange(ref CodeDummy.targetExpression, new DummyTargetExpression(), null);
        return CodeDummy.targetExpression;
      }
    }
    private static ITargetExpression/*?*/ targetExpression;

  }

  internal sealed class DummyAddressableExpression : IAddressableExpression {

    #region IAddressableExpression Members

    public object Definition {
      get { return Dummy.Field; }
    }

    public IExpression/*?*/ Instance {
      get { return null; }
    }

    #endregion

    #region IExpression Members

    public void Dispatch(ICodeVisitor visitor) {
    }

    public IEnumerable<ILocation> Locations {
      get { return Enumerable<ILocation>.Empty; }
    }

    public ITypeReference Type {
      get { return Dummy.TypeReference; }
    }

    public bool IsPure {
      get { return false; }
    }

    #endregion

  }

  internal sealed class DummyAssignment : IAssignment {
    #region IAssignment Members

    public IExpression Source {
      get { return CodeDummy.Expression; }
    }

    public ITargetExpression Target {
      get { return CodeDummy.TargetExpression; }
    }

    #endregion

    #region IExpression Members

    public void Dispatch(ICodeVisitor visitor) {
    }

    public IEnumerable<ILocation> Locations {
      get { return Enumerable<ILocation>.Empty; }
    }

    public ITypeReference Type {
      get { return Dummy.TypeReference; }
    }

    public bool IsPure {
      get { return false; }
    }

    #endregion
  }

  internal sealed class DummyBlock : IBlockStatement {

    #region IBlockStatement Members

    public IEnumerable<IStatement> Statements {
      get { return Enumerable<IStatement>.Empty; }
    }

    public bool UseCheckedArithmetic {
      get { return false; }
    }

    #endregion

    #region IStatement Members

    public IEnumerable<ILocation> Locations {
      get { return Enumerable<ILocation>.Empty; }
    }

    #endregion

    #region IDoubleDispatcher Members

    public void Dispatch(ICodeVisitor visitor) {
    }

    #endregion

  }

  internal sealed class DummyCompileTimeConstant : ICompileTimeConstant {

    #region IExpression Members

    public void Dispatch(ICodeVisitor visitor) {
    }

    public bool IsPure {
      get { return false; }
    }

    #endregion

    #region ICompileTimeConstant Members

    public object/*?*/ Value {
      get { return null; }
    }

    #endregion

    #region IExpression Members

    public IEnumerable<ILocation> Locations {
      get { return Enumerable<ILocation>.Empty; }
    }

    public ITypeReference Type {
      get { return Dummy.TypeReference; }
    }

    #endregion

  }

  internal sealed class DummyCreateArray : ICreateArray {
    #region ICreateArray Members

    public ITypeReference ElementType {
      get { return Dummy.TypeReference; }
    }

    public IEnumerable<IExpression> Initializers {
      get { return Enumerable<IExpression>.Empty; }
    }

    public IEnumerable<int> LowerBounds {
      get { return IteratorHelper.GetSingletonEnumerable<int>(0); }
    }

    public uint Rank {
      get { return 1; }
    }

    public IEnumerable<IExpression> Sizes {
      get { return IteratorHelper.GetSingletonEnumerable<IExpression>(CodeDummy.Expression); }
    }

    #endregion

    #region IExpression Members

    public void Dispatch(ICodeVisitor visitor) {
    }

    public IEnumerable<ILocation> Locations {
      get { return Enumerable<ILocation>.Empty; }
    }

    public ITypeReference Type {
      get { return Dummy.TypeReference; }
    }

    public bool IsPure {
      get { return false; }
    }

    #endregion
  }

  internal sealed class DummyExpression : IExpression {

    #region IExpression Members

    public IEnumerable<ILocation> Locations {
      get { return Enumerable<ILocation>.Empty; }
    }

    public ITypeReference Type {
      get { return Dummy.TypeReference; }
    }

    public bool IsPure {
      get { return false; }
    }

    #endregion

    #region IDoubleDispatcher Members

    public void Dispatch(ICodeVisitor visitor) {
    }

    #endregion
  }

  internal sealed class DummyGotoStatement : IGotoStatement {
    #region IGotoStatement Members

    public ILabeledStatement TargetStatement {
      get { return CodeDummy.LabeledStatement; }
    }

    #endregion

    #region IStatement Members

    public void Dispatch(ICodeVisitor visitor) {
    }

    public IEnumerable<ILocation> Locations {
      get { return Enumerable<ILocation>.Empty; }
    }

    #endregion
  }

  internal sealed class DummyMethodCall : IMethodCall {
    #region IMethodCall Members

    public IEnumerable<IExpression> Arguments {
      get { return Enumerable<IExpression>.Empty; }
    }

    public bool IsJumpCall {
      get { return false; }
    }

    public bool IsVirtualCall {
      get { return false; }
    }

    public bool IsStaticCall {
      get { return false; }
    }

    public bool IsTailCall {
      get { return false; }
    }

    public IMethodReference MethodToCall {
      get { return Dummy.MethodReference; }
    }

    public IExpression ThisArgument {
      get { return CodeDummy.Expression; }
    }

    #endregion

    #region IExpression Members

    public void Dispatch(ICodeVisitor visitor) {
    }

    public IEnumerable<ILocation> Locations {
      get { return Enumerable<ILocation>.Empty; }
    }

    public ITypeReference Type {
      get { return Dummy.TypeReference; }
    }

    public bool IsPure {
      get { return false; }
    }

    #endregion
  }

  internal sealed class DummySwitchCase : ISwitchCase {
    #region ISwitchCase Members

    public IEnumerable<IStatement> Body {
      get { return Enumerable<IStatement>.Empty; }
    }

    public ICompileTimeConstant Expression {
      get { return CodeDummy.Constant; }
    }

    public bool IsDefault {
      get { return false; }
    }

    public IEnumerable<ILocation> Locations {
      get { return Enumerable<ILocation>.Empty; }
    }

    #endregion
  }

  internal sealed class DummySwitchStatement : ISwitchStatement {
    #region ISwitchStatement Members

    public IEnumerable<ISwitchCase> Cases {
      get { return Enumerable<ISwitchCase>.Empty; }
    }

    public IExpression Expression {
      get { return CodeDummy.Expression; }
    }

    #endregion

    #region IStatement Members

    public void Dispatch(ICodeVisitor visitor) {
    }

    public IEnumerable<ILocation> Locations {
      get { return Enumerable<ILocation>.Empty; }
    }

    #endregion
  }

  internal sealed class DummyLabeledStatement : ILabeledStatement {
    #region ILabeledStatement Members

    public IName Label {
      get { return Dummy.Name; }
    }

    public IStatement Statement {
      get { return CodeDummy.Block; }
    }

    #endregion

    #region IStatement Members

    public void Dispatch(ICodeVisitor visitor) {
    }

    public IEnumerable<ILocation> Locations {
      get { return Enumerable<ILocation>.Empty; }
    }

    #endregion
  }

  internal sealed class DummyTargetExpression : ITargetExpression {

    #region ITargetExpression Members

    public byte Alignment {
      get { return 0; }
    }

    public object Definition {
      get { return Dummy.Field; }
    }

    public IExpression/*?*/ Instance {
      get { return null; }
    }

    public bool GetterIsVirtual {
      get { return false; }
    }

    public bool SetterIsVirtual {
      get { return false; }
    }

    public bool IsUnaligned {
      get { return false; }
    }

    public bool IsVolatile {
      get { return false; }
    }

    #endregion

    #region IExpression Members

    public void Dispatch(ICodeVisitor visitor) {
    }

    public IEnumerable<ILocation> Locations {
      get { return Enumerable<ILocation>.Empty; }
    }

    public ITypeReference Type {
      get { return Dummy.TypeReference; }
    }

    public bool IsPure {
      get { return false; }
    }

    #endregion

  }


#pragma warning restore 1591
}
