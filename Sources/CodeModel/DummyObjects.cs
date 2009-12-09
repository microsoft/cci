//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
//^ using Microsoft.Contracts;

namespace Microsoft.Cci {

#pragma warning disable 1591

  public static class CodeDummy {

    public static IAddressableExpression AddressableExpression {
      [DebuggerNonUserCode]
      get {
        if (CodeDummy.addressableExpression == null)
          CodeDummy.addressableExpression = new DummyAddressableExpression();
        return CodeDummy.addressableExpression;
      }
    }
    private static IAddressableExpression/*?*/ addressableExpression;

    public static IAssignment Assignment {
      [DebuggerNonUserCode]
      get {
        if (CodeDummy.assignment == null)
          CodeDummy.assignment = new DummyAssignment();
        return CodeDummy.assignment;
      }
    }
    private static IAssignment/*?*/ assignment;

    public static IBlockStatement Block {
      [DebuggerNonUserCode]
      get {
        if (CodeDummy.block == null)
          CodeDummy.block = new DummyBlock();
        return CodeDummy.block;
      }
    }
    private static IBlockStatement/*?*/ block;

    public static ICompileTimeConstant Constant {
      [DebuggerNonUserCode]
      get {
        if (CodeDummy.constant == null)
          CodeDummy.constant = new DummyCompileTimeConstant();
        return CodeDummy.constant;
      }
    }
    private static ICompileTimeConstant/*?*/ constant;

    public static ICreateArray CreateArray {
      [DebuggerNonUserCode]
      get {
        if (CodeDummy.createArray == null)
          CodeDummy.createArray = new DummyCreateArray();
        return CodeDummy.createArray;
      }
    }
    private static ICreateArray/*?*/ createArray;

    public static IExpression Expression {
      [DebuggerNonUserCode]
      get {
        if (CodeDummy.expression == null)
          CodeDummy.expression = new DummyExpression();
        return CodeDummy.expression;
      }
    }
    private static IExpression/*?*/ expression;

    public static IGotoStatement GotoStatement {
      [DebuggerNonUserCode]
      get {
        if (CodeDummy.gotoStatement == null)
          CodeDummy.gotoStatement = new DummyGotoStatement();
        return CodeDummy.gotoStatement;
      }
    }
    private static IGotoStatement/*?*/ gotoStatement;

    public static ILabeledStatement LabeledStatement {
      [DebuggerNonUserCode]
      get {
        if (CodeDummy.labeledStatement == null)
          CodeDummy.labeledStatement = new DummyLabeledStatement();
        return CodeDummy.labeledStatement;
      }
    }
    private static ILabeledStatement/*?*/ labeledStatement;

    public static IMethodCall MethodCall {
      [DebuggerNonUserCode]
      get {
        if (CodeDummy.methodCall == null)
          CodeDummy.methodCall = new DummyMethodCall();
        return CodeDummy.methodCall;
      }
    }
    private static IMethodCall/*?*/ methodCall;

    public static ISwitchCase SwitchCase {
      [DebuggerNonUserCode]
      get {
        if (CodeDummy.switchCase == null)
          CodeDummy.switchCase = new DummySwitchCase();
        return CodeDummy.switchCase;
      }
    }
    private static ISwitchCase/*?*/ switchCase;

    public static ISwitchStatement SwitchStatement {
      [DebuggerNonUserCode]
      get {
        if (CodeDummy.switchStatement == null)
          CodeDummy.switchStatement = new DummySwitchStatement();
        return CodeDummy.switchStatement;
      }
    }
    private static ISwitchStatement/*?*/ switchStatement;

    public static ITargetExpression TargetExpression {
      [DebuggerNonUserCode]
      get {
        if (CodeDummy.targetExpression == null)
          CodeDummy.targetExpression = new DummyTargetExpression();
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

    public bool HasErrors {
      get {
        return true;
      }
    }

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
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

    public bool HasErrors {
      get {
        return true;
      }
    }

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
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
      get { return IteratorHelper.GetEmptyEnumerable<IStatement>(); }
    }

    public bool UseCheckedArithmetic {
      get { return false; }
    }

    #endregion

    #region IStatement Members

    public bool HasErrors {
      get {
        return true;
      }
    }

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
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

    public bool HasErrors {
      get {
        return true;
      }
    }

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
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
      get { return IteratorHelper.GetEmptyEnumerable<IExpression>(); }
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

    public bool HasErrors {
      get {
        return true;
      }
    }

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
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

    public bool HasErrors {
      get {
        return true;
      }
    }

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
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

    public bool HasErrors {
      get {
        return true;
      }
    }

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
    }

    #endregion
  }

  internal sealed class DummyMethodCall : IMethodCall {
    #region IMethodCall Members

    public IEnumerable<IExpression> Arguments {
      get { return IteratorHelper.GetEmptyEnumerable<IExpression>(); }
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

    public bool HasErrors {
      get {
        return true;
      }
    }

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
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
      get { return IteratorHelper.GetEmptyEnumerable<IStatement>(); }
    }

    public ICompileTimeConstant Expression {
      get { return CodeDummy.Constant; }
    }

    public bool IsDefault {
      get { return false; }
    }

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
    }

    #endregion
  }

  internal sealed class DummySwitchStatement : ISwitchStatement {
    #region ISwitchStatement Members

    public IEnumerable<ISwitchCase> Cases {
      get { return IteratorHelper.GetEmptyEnumerable<ISwitchCase>(); }
    }

    public IExpression Expression {
      get { return CodeDummy.Expression; }
    }

    #endregion

    #region IStatement Members

    public void Dispatch(ICodeVisitor visitor) {
    }

    public bool HasErrors {
      get {
        return true;
      }
    }

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
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

    public bool HasErrors {
      get {
        return true;
      }
    }

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
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

    public bool HasErrors {
      get {
        return true;
      }
    }

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
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
