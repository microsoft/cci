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
using System.Diagnostics.Contracts;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci {
  /// <summary>
  /// An expression that adds the value of the left operand to the value of the right operand.
  /// Both operands must be primitive numeric types.
  /// </summary>
  public interface IAddition : IBinaryOperation {
    /// <summary>
    /// The addition must be performed with a check for arithmetic overflow and the operands must be integers.
    /// </summary>
    bool CheckOverflow {
      get;
    }

    /// <summary>
    /// If true the operands must be integers and are treated as being unsigned for the purpose of the addition. This only makes a difference if CheckOverflow is true as well.
    /// </summary>
    bool TreatOperandsAsUnsignedIntegers {
      get;
    }
  }

  /// <summary>
  /// An expression that denotes a value that has an address in memory, such as a local variable, parameter, field, array element, pointer target, or method.
  /// </summary>
  [ContractClass(typeof(IAddressableExpressionContract))]
  public interface IAddressableExpression : IExpression {

    /// <summary>
    /// The local variable, parameter, field, array element, pointer target or method that this expression denotes.
    /// </summary>
    object Definition {
      get;
      //^ ensures result is ILocalDefinition || result is IParameterDefinition || result is IFieldReference || result is IMethodReference || result is IExpression;
    }

    /// <summary>
    /// The instance to be used if this.Definition is an instance field/method or array indexer.
    /// </summary>
    IExpression/*?*/ Instance { get; }

  }

  #region IAdddressableExpression contract binding
  [ContractClassFor(typeof(IAddressableExpression))]
  abstract class IAddressableExpressionContract : IAddressableExpression {
    #region IAddressableExpression Members

    public object Definition {
      get {
        Contract.Ensures(Contract.Result<object>() is ILocalDefinition || Contract.Result<object>()  is IParameterDefinition ||
          Contract.Result<object>() is IFieldReference || Contract.Result<object>() is IMethodReference || Contract.Result<object>() is IExpression);
        throw new NotImplementedException(); 
      }
    }

    public IExpression Instance {
      get { throw new NotImplementedException(); }
    }

    #endregion

    #region IExpression Members

    public void Dispatch(ICodeVisitor visitor) {
      throw new NotImplementedException();
    }

    public ITypeReference Type {
      get { throw new NotImplementedException(); }
    }

    #endregion

    #region IObjectWithLocations Members

    public IEnumerable<ILocation> Locations {
      get { throw new NotImplementedException(); }
    }

    #endregion
  }
  #endregion


  /// <summary>
  /// An expression that takes the address of a target expression.
  /// </summary>
  [ContractClass(typeof(IAddressOfContract))]
  public interface IAddressOf : IExpression {
    /// <summary>
    /// An expression that represents an addressable location in memory.
    /// </summary>
    IAddressableExpression Expression { get; }

    /// <summary>
    /// If true, the address can only be used in operations where defining type of the addressed
    /// object has control over whether or not the object is mutated. For example, a value type that
    /// exposes no public fields or mutator methods cannot be changed using this address.
    /// </summary>
    bool ObjectControlsMutability { get; }
  }

  #region IAddressOf contract binding
  [ContractClassFor(typeof(IAddressOf))]
  abstract class IAddressOfContract : IAddressOf {
    #region IAddressOf Members

    public IAddressableExpression Expression {
      get {
        Contract.Ensures(Contract.Result<IAddressableExpression>() != null);
        throw new NotImplementedException(); 
      }
    }

    public bool ObjectControlsMutability {
      get { throw new NotImplementedException(); }
    }

    #endregion

    #region IExpression Members

    public void Dispatch(ICodeVisitor visitor) {
      throw new NotImplementedException();
    }

    public ITypeReference Type {
      get { throw new NotImplementedException(); }
    }

    #endregion

    #region IObjectWithLocations Members

    public IEnumerable<ILocation> Locations {
      get { throw new NotImplementedException(); }
    }

    #endregion
  }
  #endregion


  /// <summary>
  /// An expression that deferences an address (pointer).
  /// </summary>
  [ContractClass(typeof(IAddressDereferenceContract))]
  public interface IAddressDereference : IExpression {
    /// <summary>
    /// The address to dereference.
    /// </summary>
    IExpression Address {
      get;
      // ^ ensures result.Type is IPointerTypeReference;
    }

    /// <summary>
    /// If the addres to dereference is not aligned with the size of the target type, this property specifies the actual alignment.
    /// For example, a value of 1 specifies that the pointer is byte aligned, whereas the target type may be word sized.
    /// </summary>
    byte Alignment {
      get;
      //^ requires this.IsUnaligned;
      //^ ensures result == 1 || result == 2 || result == 4;
    }

    /// <summary>
    /// True if the address is not aligned to the natural size of the target type. If true, the actual alignment of the
    /// address is specified by this.Alignment.
    /// </summary>
    bool IsUnaligned { get; }

    /// <summary>
    /// The location at Address is volatile and its contents may not be cached.
    /// </summary>
    bool IsVolatile { get; }
  }

  #region IAddressDereference contract binding
  [ContractClassFor(typeof(IAddressDereference))]
  abstract class IAddressDereferenceContract : IAddressDereference {
    #region IAddressDereference Members

    public IExpression Address {
      get {
        Contract.Ensures(Contract.Result<IExpression>() != null);
        throw new NotImplementedException(); 
      }
    }

    public byte Alignment {
      get { throw new NotImplementedException(); }
    }

    public bool IsUnaligned {
      get { throw new NotImplementedException(); }
    }

    public bool IsVolatile {
      get { throw new NotImplementedException(); }
    }

    #endregion

    #region IExpression Members

    public void Dispatch(ICodeVisitor visitor) {
      throw new NotImplementedException();
    }

    public ITypeReference Type {
      get { throw new NotImplementedException(); }
    }

    #endregion

    #region IObjectWithLocations Members

    public IEnumerable<ILocation> Locations {
      get { throw new NotImplementedException(); }
    }

    #endregion
  }
  #endregion


  /// <summary>
  /// An expression that evaluates to an instance of a delegate type where the body of the method called by the delegate is specified by the expression.
  /// </summary>
  public interface IAnonymousDelegate : IExpression, ISignature {

    /// <summary>
    /// A block of statements providing the implementation of the anonymous method that is called by the delegate that is the result of this expression.
    /// </summary>
    IBlockStatement Body { get; }

    /// <summary>
    /// The parameters this anonymous method.
    /// </summary>
    new IEnumerable<IParameterDefinition> Parameters { get; }

    /// <summary>
    /// The return type of the delegate.
    /// </summary>
    ITypeReference ReturnType { get; }

    /// <summary>
    /// The type of delegate that this expression results in.
    /// </summary>
    new ITypeReference Type { get; }

  }

  /// <summary>
  /// An expression that represents an array element access.
  /// </summary>
  [ContractClass(typeof(IArrayIndexerContract))]
  public interface IArrayIndexer : IExpression {

    /// <summary>
    /// An expression that results in value of an array type.
    /// </summary>
    IExpression IndexedObject { get; }

    /// <summary>
    /// The array indices.
    /// </summary>
    IEnumerable<IExpression> Indices { get; }

  }

  #region IArrayIndexer contract binding
  [ContractClassFor(typeof(IArrayIndexer))]
  abstract class IArrayIndexerContract : IArrayIndexer {
    public IExpression IndexedObject {
      get {
        Contract.Ensures(Contract.Result<IExpression>() != null);
        throw new NotImplementedException(); 
      }
    }

    public IEnumerable<IExpression> Indices {
      get {
        Contract.Ensures(Contract.Result<IEnumerable<IExpression>>() != null);
        throw new NotImplementedException(); 
      }
    }

    public void Dispatch(ICodeVisitor visitor) {
      throw new NotImplementedException();
    }

    public ITypeReference Type {
      get { throw new NotImplementedException(); }
    }

    public IEnumerable<ILocation> Locations {
      get { throw new NotImplementedException(); }
    }
  }
  #endregion


  /// <summary>
  /// An expression that assigns the value of the source (right) operand to the location represented by the target (left) operand.
  /// The expression result is the value of the source expression.
  /// </summary>
  [ContractClass(typeof(IAssignmentContract))]
  public interface IAssignment : IExpression {
    /// <summary>
    /// The expression representing the value to assign. 
    /// </summary>
    IExpression Source { get; }

    /// <summary>
    /// The expression representing the target to assign to.
    /// </summary>
    ITargetExpression Target { get; }
  }

  #region IAssignment contract binding
  [ContractClassFor(typeof(IAssignment))]
  abstract class IAssignmentContract : IAssignment {
    #region IAssignment Members

    public IExpression Source {
      get {
        Contract.Ensures(Contract.Result<IExpression>() != null);
        throw new NotImplementedException(); 
      }
    }

    public ITargetExpression Target {
      get {
        Contract.Ensures(Contract.Result<ITargetExpression>() != null);
        throw new NotImplementedException(); 
      }
    }

    #endregion

    #region IExpression Members

    public void Dispatch(ICodeVisitor visitor) {
      throw new NotImplementedException();
    }

    public ITypeReference Type {
      get { throw new NotImplementedException(); }
    }

    #endregion

    #region IObjectWithLocations Members

    public IEnumerable<ILocation> Locations {
      get { throw new NotImplementedException(); }
    }

    #endregion
  }
  #endregion


  /// <summary>
  /// A binary operation performed on a left and right operand.
  /// </summary>
  [ContractClass(typeof(IBinaryOperationContract))]
  public interface IBinaryOperation : IExpression {
    /// <summary>
    /// The left operand.
    /// </summary>
    IExpression LeftOperand { get; }

    /// <summary>
    /// The right operand.
    /// </summary>
    IExpression RightOperand { get; }

    /// <summary>
    /// If true, the left operand must be a target expression and the result of the binary operation is the
    /// value of the target expression before it is assigned the value of the operation performed on
    /// (right hand) values of the left and right operands.
    /// </summary>
    bool ResultIsUnmodifiedLeftOperand { get; }
  }

  [ContractClassFor(typeof(IBinaryOperation))]
  abstract class IBinaryOperationContract : IBinaryOperation {
    public IExpression LeftOperand {
      get {
        Contract.Ensures(Contract.Result<IExpression>() != null);
        throw new NotImplementedException();
      }
    }

    public IExpression RightOperand {
      get {
        Contract.Ensures(Contract.Result<IExpression>() != null);
        throw new NotImplementedException();
      }
    }

    /// <summary>
    /// If true, the left operand must be a target expression and the result of the binary operation is the
    /// value of the target expression before it is assigned the value of the operation performed on
    /// (right hand) values of the left and right operands.
    /// </summary>
    public bool ResultIsUnmodifiedLeftOperand {
      get {
        Contract.Ensures(!Contract.Result<bool>() || this.LeftOperand is ITargetExpression);
        throw new NotImplementedException();
      }
    }

    public void Dispatch(ICodeVisitor visitor) {
      throw new NotImplementedException();
    }

    public ITypeReference Type {
      get { throw new NotImplementedException(); }
    }

    public IEnumerable<ILocation> Locations {
      get { throw new NotImplementedException(); }
    }
  }

  /// <summary>
  /// An expression that computes the bitwise and of the left and right operands. 
  /// </summary>
  public interface IBitwiseAnd : IBinaryOperation {
  }

  /// <summary>
  /// An expression that computes the bitwise or of the left and right operands. 
  /// </summary>
  public interface IBitwiseOr : IBinaryOperation {
  }

  /// <summary>
  /// An expression that introduces a new block scope and that references local variables
  /// that are defined and initialized by embedded statements when control reaches the expression.
  /// </summary>
  [ContractClass(typeof(IBlockExpressionContract))]
  public interface IBlockExpression : IExpression {

    /// <summary>
    /// A block of statements that typically introduce local variables to hold sub expressions.
    /// The scope of these declarations coincides with the block expression. 
    /// The statements are executed before evaluation of Expression occurs.
    /// </summary>
    IBlockStatement BlockStatement { get; }

    /// <summary>
    /// The expression that computes the result of the entire block expression.
    /// This expression can contain references to the local variables that are declared inside BlockStatement.
    /// </summary>
    IExpression Expression { get; }
  }

  #region IBlockExpression contract binding
  [ContractClassFor(typeof(IBlockExpression))]
  abstract class IBlockExpressionContract : IBlockExpression {
    #region IBlockExpression Members

    public IBlockStatement BlockStatement {
      get {
        Contract.Ensures(Contract.Result<IBlockStatement>() != null);
        throw new NotImplementedException(); 
      }
    }

    public IExpression Expression {
      get {
        Contract.Ensures(Contract.Result<IExpression>() != null);
        throw new NotImplementedException(); 
      }
    }

    #endregion

    #region IExpression Members

    public void Dispatch(ICodeVisitor visitor) {
      throw new NotImplementedException();
    }

    public ITypeReference Type {
      get { throw new NotImplementedException(); }
    }

    #endregion

    #region IObjectWithLocations Members

    public IEnumerable<ILocation> Locations {
      get { throw new NotImplementedException(); }
    }

    #endregion
  }
  #endregion


  /// <summary>
  /// An expression that binds to a local variable, parameter or field.
  /// </summary>
  [ContractClass(typeof(IBoundExpressionContract))]
  public interface IBoundExpression : IExpression {

    /// <summary>
    /// If Definition is a field and the field is not aligned with natural size of its type, this property specifies the actual alignment.
    /// For example, if the field is byte aligned, then the result of this property is 1. Likewise, 2 for word (16-bit) alignment and 4 for
    /// double word (32-bit alignment). 
    /// </summary>
    byte Alignment {
      get;
      //^ requires IsUnaligned;
      //^ ensures result == 1 || result == 2 || result == 4;
    }

    /// <summary>
    /// The local variable, parameter or field that this expression binds to.
    /// </summary>
    object Definition {
      get;
      //^ ensures result is ILocalDefinition || result is IParameterDefinition || result is IFieldReference;
    }

    /// <summary>
    /// If the expression binds to an instance field then this property is not null and contains the instance.
    /// </summary>
    IExpression/*?*/ Instance {
      get;
      //^ ensures this.Definition is IFieldReference && !((IFieldReference)this.Definition).ResolvedField.IsStatic <==> result != null;
    }

    /// <summary>
    /// True if the definition is a field and the field is not aligned with the natural size of its type.
    /// For example if the field type is Int32 and the field is aligned on an Int16 boundary.
    /// </summary>
    bool IsUnaligned { get; }

    /// <summary>
    /// The bound Definition is a volatile field and its contents may not be cached.
    /// </summary>
    bool IsVolatile { get; }

  }

  #region IBoundExpression contract binding
  [ContractClassFor(typeof(IBoundExpression))]
  abstract class IBoundExpressionContract : IBoundExpression {
    #region IBoundExpression Members

    public byte Alignment {
      get {
        Contract.Requires(this.IsUnaligned);
        //Contract.Ensures(Contract.Result<byte>() == 1 || Contract.Result<byte>() == 2 || Contract.Result<byte>() == 4);
        throw new NotImplementedException(); 
      }
    }

    public object Definition {
      get {
        Contract.Ensures(Contract.Result<object>() != null);
        Contract.Ensures(Contract.Result<object>() is ILocalDefinition || Contract.Result<object>() is IParameterDefinition || Contract.Result<object>() is IFieldReference);
        throw new NotImplementedException(); 
      }
    }

    public IExpression Instance {
      get {
        //Contract.Ensures((Contract.Result<IExpression>() != null) == (this.Definition is IFieldReference && !((IFieldReference)this.Definition).IsStatic));
        throw new NotImplementedException(); 
      }
    }

    public bool IsUnaligned {
      get { throw new NotImplementedException(); }
    }

    public bool IsVolatile {
      get { throw new NotImplementedException(); }
    }

    #endregion

    #region IExpression Members

    public void Dispatch(ICodeVisitor visitor) {
      throw new NotImplementedException();
    }

    public ITypeReference Type {
      get { throw new NotImplementedException(); }
    }

    #endregion

    #region IObjectWithLocations Members

    public IEnumerable<ILocation> Locations {
      get { throw new NotImplementedException(); }
    }

    #endregion
  }
  #endregion


  /// <summary>
  /// An expression that casts the value to the given type, resulting in a null value if the cast does not succeed.
  /// </summary>
  [ContractClass(typeof(ICastIfPossibleContract))]
  public interface ICastIfPossible : IExpression {
    /// <summary>
    /// The value to cast if possible.
    /// </summary>
    IExpression ValueToCast { get; }

    /// <summary>
    /// The type to which the value must be cast. If the value is not of this type, the expression results in a null value of this type.
    /// </summary>
    ITypeReference TargetType { get; }
  }

  #region ICastIfPossible contract binding
  [ContractClassFor(typeof(ICastIfPossible))]
  abstract class ICastIfPossibleContract : ICastIfPossible {
    #region ICastIfPossible Members

    public IExpression ValueToCast {
      get {
        Contract.Ensures(Contract.Result<IExpression>() != null);
        throw new NotImplementedException(); 
      }
    }

    public ITypeReference TargetType {
      get {
        Contract.Ensures(Contract.Result<ITypeReference>() != null);
        throw new NotImplementedException();
      }
    }

    #endregion

    #region IExpression Members

    public void Dispatch(ICodeVisitor visitor) {
      throw new NotImplementedException();
    }

    public ITypeReference Type {
      get { throw new NotImplementedException(); }
    }

    #endregion

    #region IObjectWithLocations Members

    public IEnumerable<ILocation> Locations {
      get { throw new NotImplementedException(); }
    }

    #endregion
  }
  #endregion


  /// <summary>
  /// An expression that results in true if the given operand is an instance of the given type.
  /// </summary>
  [ContractClass(typeof(ICheckIfInstanceContract))]
  public interface ICheckIfInstance : IExpression {
    /// <summary>
    /// The value to check.
    /// </summary>
    IExpression Operand { get; }

    /// <summary>
    /// The type to which the value must belong for this expression to evaluate as true.
    /// </summary>
    ITypeReference TypeToCheck { get; }
  }

  #region ICheckIfInstance contract binding

  [ContractClassFor(typeof(ICheckIfInstance))]
  abstract class ICheckIfInstanceContract : ICheckIfInstance {
    #region ICheckIfInstance Members

    public IExpression Operand {
      get {
        Contract.Ensures(Contract.Result<IExpression>() != null);
        throw new NotImplementedException(); 
      }
    }

    public ITypeReference TypeToCheck {
      get {
        Contract.Ensures(Contract.Result<ITypeReference>() != null);
        throw new NotImplementedException(); 
      }
    }

    #endregion

    #region IExpression Members

    public void Dispatch(ICodeVisitor visitor) {
      throw new NotImplementedException();
    }

    public ITypeReference Type {
      get { throw new NotImplementedException(); }
    }

    #endregion

    #region IObjectWithLocations Members

    public IEnumerable<ILocation> Locations {
      get { throw new NotImplementedException(); }
    }

    #endregion
  }
  #endregion

  /// <summary>
  /// Converts a value to a given type using a primitive type conversion for which an IL instruction exsists.
  /// </summary>
  /// <remarks>User defined conversions are modeled as method calls.</remarks>
  [ContractClass(typeof(IConversionContract))]
  public interface IConversion : IExpression {
    /// <summary>
    /// The value to convert. If the type of this value is an enumeration, the target type must have the same size and may not itself be an enumeration.
    /// </summary>
    IExpression ValueToConvert { get; }

    /// <summary>
    /// If true and ValueToConvert is a number and ResultType is a numeric type, check that ValueToConvert falls within the range of ResultType and throw an exception if not.
    /// </summary>
    bool CheckNumericRange { get; }

    /// <summary>
    /// The type to which the value is to be converted. If the type of this value is an enumeration, the source type must have the same size and may not itself be an enumeration.
    /// </summary>
    ITypeReference TypeAfterConversion { get; }
  }

  #region IConversion contract binding
  [ContractClassFor(typeof(IConversion))]
  abstract class IConversionContract : IConversion {

    public IExpression ValueToConvert {
      get {
        Contract.Ensures(Contract.Result<IExpression>() != null);
        throw new NotImplementedException(); 
      }
    }

    public bool CheckNumericRange {
      get { throw new NotImplementedException(); }
    }

    public ITypeReference TypeAfterConversion {
      get {
        Contract.Ensures(Contract.Result<ITypeReference>() != null);
        throw new NotImplementedException(); 
      }
    }

    public void Dispatch(ICodeVisitor visitor) {
      throw new NotImplementedException();
    }

    public ITypeReference Type {
      get { throw new NotImplementedException(); }
    }

    public IEnumerable<ILocation> Locations {
      get { throw new NotImplementedException(); }
    }
  }
  #endregion


  /// <summary>
  /// An expression that does not change its value at runtime and can be evaluated at compile time.
  /// </summary>
  public interface ICompileTimeConstant : IExpression {

    /// <summary>
    /// The compile time value of the expression. Can be null.
    /// </summary>
    object/*?*/ Value { get; }
  }

  /// <summary>
  /// An expression that results in one of two values, depending on the value of a condition.
  /// </summary>
  [ContractClass(typeof(IConditionalContract))]
  public interface IConditional : IExpression {
    /// <summary>
    /// The condition that determines which subexpression to evaluate.
    /// </summary>
    IExpression Condition { get; }

    /// <summary>
    /// The expression to evaluate as the value of the overall expression if the condition is true.
    /// </summary>
    IExpression ResultIfTrue { get; }

    /// <summary>
    /// The expression to evaluate as the value of the overall expression if the condition is false.
    /// </summary>
    IExpression ResultIfFalse { get; }
  }

  #region IConditional contract binding
  [ContractClassFor(typeof(IConditional))]
  abstract class IConditionalContract : IConditional {
    #region IConditional Members

    public IExpression Condition {
      get {
        Contract.Ensures(Contract.Result<IExpression>() != null);
        throw new NotImplementedException(); 
      }
    }

    public IExpression ResultIfTrue {
      get {
        Contract.Ensures(Contract.Result<IExpression>() != null);
        throw new NotImplementedException(); 
      }
    }

    public IExpression ResultIfFalse {
      get {
        Contract.Ensures(Contract.Result<IExpression>() != null);
        throw new NotImplementedException(); 
      }
    }

    #endregion

    #region IExpression Members

    public void Dispatch(ICodeVisitor visitor) {
      throw new NotImplementedException();
    }

    public ITypeReference Type {
      get { throw new NotImplementedException(); }
    }

    #endregion

    #region IObjectWithLocations Members

    public IEnumerable<ILocation> Locations {
      get { throw new NotImplementedException(); }
    }

    #endregion
  }
  #endregion


  /// <summary>
  /// An expression that creates an array instance.
  /// </summary>
  [ContractClass(typeof(ICreateArrayContract))]
  public interface ICreateArray : IExpression {
    /// <summary>
    /// The element type of the array.
    /// </summary>
    ITypeReference ElementType { get; }

    /// <summary>
    /// The initial values of the array elements. May be empty.
    /// This must be a flat list of the initial values. Its length
    /// must be the product of the size of each dimension.
    /// </summary>
    IEnumerable<IExpression> Initializers {
      get;
    }

    /// <summary>
    /// The index value of the first element in each dimension.
    /// </summary>
    IEnumerable<int> LowerBounds {
      get;
      // ^ ensures count{int lb in result} == Rank;
    }

    /// <summary>
    /// The number of dimensions of the array.
    /// </summary>
    uint Rank {
      get;
      //^ ensures result > 0;
    }

    /// <summary>
    /// The number of elements allowed in each dimension.
    /// </summary>
    IEnumerable<IExpression> Sizes {
      get;
      // ^ ensures count{int size in result} == Rank;
    }

  }

  #region ICreateArray contract binding
  [ContractClassFor(typeof(ICreateArray))]
  abstract class ICreateArrayContract : ICreateArray {
    public ITypeReference ElementType {
      get {
        Contract.Ensures(Contract.Result<ITypeReference>() != null);
        throw new NotImplementedException(); 
      }
    }

    public IEnumerable<IExpression> Initializers {
      get {
        Contract.Ensures(Contract.Result<IEnumerable<IExpression>>() != null);
        throw new NotImplementedException(); 
      }
    }

    public IEnumerable<int> LowerBounds {
      get {
        Contract.Ensures(Contract.Result<IEnumerable<int>>() != null);
        throw new NotImplementedException(); 
      }
    }

    public uint Rank {
      get {
        Contract.Ensures(Contract.Result<uint>() > 0);
        throw new NotImplementedException(); 
      }
    }

    public IEnumerable<IExpression> Sizes {
      get {
        Contract.Ensures(Contract.Result<IEnumerable<IExpression>>() != null);
        throw new NotImplementedException(); 
      }
    }

    public void Dispatch(ICodeVisitor visitor) {
      throw new NotImplementedException();
    }

    public ITypeReference Type {
      get {
        throw new NotImplementedException(); 
      }
    }

    public IEnumerable<ILocation> Locations {
      get {
        throw new NotImplementedException(); 
      }
    }
  }
  #endregion

  /// <summary>
  /// Creates an instance of the delegate type return by this.Type, using the method specified by this.MethodToCallViaDelegate.
  /// If the method is an instance method, then this.Instance specifies the expression that results in the instance on which the 
  /// method will be called.
  /// </summary>
  public partial interface ICreateDelegateInstance : IExpression {

    /// <summary>
    /// An expression that evaluates to the instance (if any) on which this.MethodToCallViaDelegate must be called (via the delegate).
    /// </summary>
    IExpression/*?*/ Instance { get; }

    /// <summary>
    /// True if the delegate encapsulates a virtual method.
    /// </summary>
    bool IsVirtualDelegate { get; }

    /// <summary>
    /// The method that is to be be called when the delegate instance is invoked.
    /// </summary>
    IMethodReference MethodToCallViaDelegate { get; }

  }

  #region ICreateDelegateInstance contract binding
  [ContractClass(typeof(ICreateDelegateInstanceContract))]
  public partial interface ICreateDelegateInstance {

  }

  [ContractClassFor(typeof(ICreateDelegateInstance))]
  abstract class ICreateDelegateInstanceContract : ICreateDelegateInstance {
    #region ICreateDelegateInstance Members

    public IExpression Instance {
      get { throw new NotImplementedException(); }
    }

    public bool IsVirtualDelegate {
      get { throw new NotImplementedException(); }
    }

    public IMethodReference MethodToCallViaDelegate {
      get {
        Contract.Ensures(Contract.Result<IMethodReference>() != null);
        throw new NotImplementedException();
      }
    }

    #endregion

    #region IExpression Members

    public void Dispatch(ICodeVisitor visitor) {
      throw new NotImplementedException();
    }

    public ITypeReference Type {
      get { throw new NotImplementedException(); }
    }

    #endregion

    #region IObjectWithLocations Members

    public IEnumerable<ILocation> Locations {
      get { throw new NotImplementedException(); }
    }

    #endregion
  }
  #endregion


  /// <summary>
  /// An expression that invokes an object constructor.
  /// </summary>
  [ContractClass(typeof(ICreateObjectInstanceContract))]
  public interface ICreateObjectInstance : IExpression {
    /// <summary>
    /// The arguments to pass to the constructor.
    /// </summary>
    IEnumerable<IExpression> Arguments { get; }

    /// <summary>
    /// The contructor method to call.
    /// </summary>
    IMethodReference MethodToCall { get; }

  }

  #region ICreateObjectInstance contract binding
  [ContractClassFor(typeof(ICreateObjectInstance))]
  abstract class ICreateObjectInstanceContract : ICreateObjectInstance {
    #region ICreateObjectInstance Members

    public IEnumerable<IExpression> Arguments {
      get {
        Contract.Ensures(Contract.Result<IEnumerable<IExpression>>() != null);
        throw new NotImplementedException(); 
      }
    }

    public IMethodReference MethodToCall {
      get {
        Contract.Ensures(Contract.Result<IMethodReference>() != null);
        throw new NotImplementedException(); 
      }
    }

    #endregion

    #region IExpression Members

    public void Dispatch(ICodeVisitor visitor) {
      throw new NotImplementedException();
    }

    public ITypeReference Type {
      get { throw new NotImplementedException(); }
    }

    #endregion

    #region IObjectWithLocations Members

    public IEnumerable<ILocation> Locations {
      get { throw new NotImplementedException(); }
    }

    #endregion
  }
  #endregion


  /// <summary>
  /// An expression that results in the default value of a given type.
  /// </summary>
  [ContractClass(typeof(IDefaultValueContract))]
  public interface IDefaultValue : IExpression {
    /// <summary>
    /// The type whose default value is the result of this expression.
    /// </summary>
    ITypeReference DefaultValueType { get; }
  }

  #region IDefaultValue contract binding
  [ContractClassFor(typeof(IDefaultValue))]
  abstract class IDefaultValueContract : IDefaultValue {
    #region IDefaultValue Members

    public ITypeReference DefaultValueType {
      get {
        Contract.Ensures(Contract.Result<ITypeReference>() != null);
        throw new NotImplementedException(); 
      }
    }

    #endregion

    #region IExpression Members

    public void Dispatch(ICodeVisitor visitor) {
      throw new NotImplementedException();
    }

    public ITypeReference Type {
      get { throw new NotImplementedException(); }
    }

    #endregion

    #region IObjectWithLocations Members

    public IEnumerable<ILocation> Locations {
      get { throw new NotImplementedException(); }
    }

    #endregion
  }
  #endregion


  /// <summary>
  /// An expression that divides the value of the left operand by the value of the right operand. 
  /// </summary>
  public interface IDivision : IBinaryOperation {
    /// <summary>
    /// If true the operands must be integers and are treated as being unsigned for the purpose of the division.
    /// </summary>
    bool TreatOperandsAsUnsignedIntegers {
      get;
    }
  }

  /// <summary>
  /// An expression that results in the value on top of the implicit operand stack.
  /// </summary>
  public interface IDupValue : IExpression {
  }

  /// <summary>
  /// An expression that results in true if both operands represent the same value or object.
  /// </summary>
  public interface IEquality : IBinaryOperation {
  }

  /// <summary>
  /// An expression that computes the bitwise exclusive or of the left and right operands. 
  /// </summary>
  public interface IExclusiveOr : IBinaryOperation {
  }

  /// <summary>
  /// An expression results in a value of some type.
  /// </summary>
  [ContractClass(typeof(IExpressionContract))]
  public interface IExpression : IObjectWithLocations {

    /// <summary>
    /// Calls the visitor.Visit(T) method where T is the most derived object model node interface type implemented by the concrete type
    /// of the object implementing IStatement. The dispatch method does not invoke Dispatch on any child objects. If child traversal
    /// is desired, the implementations of the Visit methods should do the subsequent dispatching.
    /// </summary>
    void Dispatch(ICodeVisitor visitor);

    /// <summary>
    /// The type of value the expression will evaluate to, as determined at compile time.
    /// </summary>
    ITypeReference Type { get; }

  }

  [ContractClassFor(typeof(IExpression))]
  abstract class IExpressionContract : IExpression {
    public void Dispatch(ICodeVisitor visitor) {
      Contract.Requires(visitor != null);
      throw new NotImplementedException();
    }

    public ITypeReference Type {
      get {
        Contract.Ensures(Contract.Result<ITypeReference>() != null);
        throw new NotImplementedException();
      }
    }

    public IEnumerable<ILocation> Locations {
      get { throw new NotImplementedException();  }
    }
  }

  /// <summary>
  /// A source location to proffer from IOperation.Location and indicating the source extent (or extents) of an expression in the source language.
  /// Such locations are ignored when producing a PDB file, since PDB files only record the source extents of statements.
  /// </summary>
  [ContractClass(typeof(IExpressionSourceLocationContract))]
  public interface IExpressionSourceLocation : ILocation {

    /// <summary>
    /// Zero or more locations that correspond to an expression.
    /// </summary>
    IPrimarySourceLocation PrimarySourceLocation { get; }
  }

  #region IExpressionSourceLocation contract binding

  [ContractClassFor(typeof(IExpressionSourceLocation))]
  abstract class IExpressionSourceLocationContract : IExpressionSourceLocation {

    public IPrimarySourceLocation PrimarySourceLocation {
      get {
        Contract.Ensures(Contract.Result<IPrimarySourceLocation>() != null);
        throw new NotImplementedException(); 
      }
    }

    public IDocument Document {
      get { throw new NotImplementedException(); }
    }

  }
  #endregion


  /// <summary>
  /// An expression that results in an instance of System.Type that represents the compile time type that has been paired with a runtime value via a typed reference.
  /// This corresponds to the __reftype operator in C#.
  /// </summary>
  public interface IGetTypeOfTypedReference : IExpression {
    /// <summary>
    /// An expression that results in a value of type System.TypedReference.
    /// </summary>
    IExpression TypedReference { get; }
  }

  /// <summary>
  /// An expression that converts the typed reference value resulting from evaluating TypedReference to a value of the type specified by TargetType.
  /// This corresponds to the __refvalue operator in C#.
  /// </summary>
  [ContractClass(typeof(IGetValueOfTypedReferenceContract))]
  public interface IGetValueOfTypedReference : IExpression {
    /// <summary>
    /// An expression that results in a value of type System.TypedReference.
    /// </summary>
    IExpression TypedReference { get; }

    /// <summary>
    /// The type to which the value part of the typed reference must be converted.
    /// </summary>
    ITypeReference TargetType { get; }
  }

  #region IGetValueOfTypedReference contract binding
  [ContractClassFor(typeof(IGetValueOfTypedReference))]
  abstract class IGetValueOfTypedReferenceContract : IGetValueOfTypedReference {
    #region IGetValueOfTypedReference Members

    public IExpression TypedReference {
      get {
        Contract.Ensures(Contract.Result<IExpression>() != null);
        throw new NotImplementedException(); 
      }
    }

    public ITypeReference TargetType {
      get {
        Contract.Ensures(Contract.Result<ITypeReference>() != null);
        throw new NotImplementedException(); 
      }
    }

    #endregion

    #region IExpression Members

    public void Dispatch(ICodeVisitor visitor) {
      throw new NotImplementedException();
    }

    public ITypeReference Type {
      get { throw new NotImplementedException(); }
    }

    #endregion

    #region IObjectWithLocations Members

    public IEnumerable<ILocation> Locations {
      get { throw new NotImplementedException(); }
    }

    #endregion
  }
  #endregion


  /// <summary>
  /// An expression that results in true if the value of the left operand is greater than the value of the right operand.
  /// </summary>
  public interface IGreaterThan : IBinaryOperation {
    /// <summary>
    /// If the operands are integers, use unsigned comparison. If the operands are floating point numbers, return true if the operands are unordered.
    /// </summary>
    bool IsUnsignedOrUnordered { get; }
  }

  /// <summary>
  /// An expression that results in true if the value of the left operand is greater than or equal to the value of the right operand.
  /// </summary>
  public interface IGreaterThanOrEqual : IBinaryOperation {
    /// <summary>
    /// If the operands are integers, use unsigned comparison. If the operands are floating point numbers, return true if the operands are unordered.
    /// </summary>
    bool IsUnsignedOrUnordered { get; }
  }

  /// <summary>
  /// An expression that results in the value of the left operand, shifted left by the number of bits specified by the value of the right operand.
  /// </summary>
  public interface ILeftShift : IBinaryOperation {
  }

  /// <summary>
  /// An expression that results in true if the value of the left operand is less than the value of the right operand.
  /// </summary>
  public interface ILessThan : IBinaryOperation {
    /// <summary>
    /// If the operands are integers, use unsigned comparison. If the operands are floating point numbers, return true if the operands are unordered.
    /// </summary>
    bool IsUnsignedOrUnordered { get; }
  }

  /// <summary>
  /// An expression that results in true if the value of the left operand is less than or equal to the value of the right operand.
  /// </summary>
  public interface ILessThanOrEqual : IBinaryOperation {
    /// <summary>
    /// If the operands are integers, use unsigned comparison. If the operands are floating point numbers, return true if the operands are unordered.
    /// </summary>
    bool IsUnsignedOrUnordered { get; }
  }

  /// <summary>
  /// An expression that results in the logical negation of the boolean value of the given operand.
  /// </summary>
  public interface ILogicalNot : IUnaryOperation {
  }

  /// <summary>
  /// An expression that creates a typed reference (a pair consisting of a reference to a runtime value and a compile time type).
  /// This is similar to what happens when a value type is boxed, except that the boxed value can be an object and
  /// the runtime type of the boxed value can be a subtype of the compile time type that is associated with the boxed valued.
  /// </summary>
  public interface IMakeTypedReference : IExpression {
    /// <summary>
    /// The value to box in a typed reference.
    /// </summary>
    IExpression Operand { get; }

  }

  /// <summary>
  /// An expression that invokes a method.
  /// </summary>
  [ContractClass(typeof(IMethodCallContract))]
  public interface IMethodCall : IExpression {

    /// <summary>
    /// The arguments to pass to the method, after they have been converted to match the parameters of the resolved method.
    /// </summary>
    IEnumerable<IExpression> Arguments { get; }

    /// <summary>
    /// True if this method call terminates the calling method and reuses the arguments of the calling method as the arguments of the called method.
    /// </summary>
    bool IsJumpCall {
      get;
    }

    /// <summary>
    /// True if the method to call is determined at run time, based on the runtime type of ThisArgument.
    /// </summary>
    bool IsVirtualCall {
      get;
      //^ ensures result ==> this.MethodToCall.ResolvedMethod.IsVirtual;
    }

    /// <summary>
    /// True if the method to call is static (has no this parameter).
    /// </summary>
    bool IsStaticCall {
      get;
      //^ ensures result ==> this.MethodToCall.ResolvedMethod.IsStatic;
    }

    /// <summary>
    /// True if this method call terminates the calling method. It indicates that the calling method's stack frame is not required
    /// and can be removed before executing the call.
    /// </summary>
    bool IsTailCall {
      get;
    }

    /// <summary>
    /// The method to call.
    /// </summary>
    IMethodReference MethodToCall { get; }

    /// <summary>
    /// The expression that results in the value that must be passed as the value of the this argument of the resolved method.
    /// </summary>
    IExpression ThisArgument {
      get;
      //^ requires !this.IsStaticCall;
    }

  }

  #region IMethodCall contract binding
  [ContractClassFor(typeof(IMethodCall))]
  abstract class IMethodCallContract : IMethodCall {
    #region IMethodCall Members

    public IEnumerable<IExpression> Arguments {
      get {
        Contract.Ensures(Contract.Result<IEnumerable<IExpression>>() != null);
        throw new NotImplementedException(); 
      }
    }

    public bool IsJumpCall {
      get { throw new NotImplementedException(); }
    }

    public bool IsVirtualCall {
      get { throw new NotImplementedException(); }
    }

    public bool IsStaticCall {
      get { throw new NotImplementedException(); }
    }

    public bool IsTailCall {
      get { throw new NotImplementedException(); }
    }

    public IMethodReference MethodToCall {
      get {
        Contract.Ensures(Contract.Result<IMethodReference>() != null);
        throw new NotImplementedException(); 
      }
    }

    public IExpression ThisArgument {
      get {
        Contract.Requires(!this.IsStaticCall && !this.IsJumpCall);
        Contract.Ensures(Contract.Result<IExpression>() != null);
        throw new NotImplementedException(); 
      }
    }

    #endregion

    #region IExpression Members

    public void Dispatch(ICodeVisitor visitor) {
      throw new NotImplementedException();
    }

    public ITypeReference Type {
      get { throw new NotImplementedException(); }
    }

    #endregion

    #region IObjectWithLocations Members

    public IEnumerable<ILocation> Locations {
      get { throw new NotImplementedException(); }
    }

    #endregion
  }
  #endregion


  /// <summary>
  /// An expression that results in the remainder of dividing value the left operand by the value of the right operand. 
  /// </summary>
  public interface IModulus : IBinaryOperation {
    /// <summary>
    /// If true the operands must be integers and are treated as being unsigned for the purpose of the modulus.
    /// </summary>
    bool TreatOperandsAsUnsignedIntegers {
      get;
    }
  }

  /// <summary>
  /// An expression that multiplies the value of the left operand by the value of the right operand. 
  /// </summary>
  public interface IMultiplication : IBinaryOperation {
    /// <summary>
    /// The multiplication must be performed with a check for arithmetic overflow and the operands must be integers.
    /// </summary>
    bool CheckOverflow {
      get;
    }

    /// <summary>
    /// If true the operands must be integers and are treated as being unsigned for the purpose of the multiplication. This only makes a difference if CheckOverflow is true as well.
    /// </summary>
    bool TreatOperandsAsUnsignedIntegers {
      get;
    }
  }

  /// <summary>
  /// An expression that represents a (name, value) pair and that is typically used in method calls, custom attributes and object initializers.
  /// </summary>
  [ContractClass(typeof(INamedArgumentContract))]
  public interface INamedArgument : IExpression {
    /// <summary>
    /// The name of the parameter or property or field that corresponds to the argument.
    /// </summary>
    IName ArgumentName { get; }

    /// <summary>
    /// The value of the argument.
    /// </summary>
    IExpression ArgumentValue { get; }

    /// <summary>
    /// Returns either null or the parameter or property or field that corresponds to this argument.
    /// </summary>
    object/*?*/ ResolvedDefinition {
      get;
      //^ ensures result == null || result is IParameterDefinition || result is IPropertyDefinition || result is IFieldDefinition;
    }

    /// <summary>
    /// If true, the resolved definition is a property whose getter is virtual.
    /// </summary>
    bool GetterIsVirtual { get; }

    /// <summary>
    /// If true, the resolved definition is a property whose setter is virtual.
    /// </summary>
    bool SetterIsVirtual { get; }
  }

  #region INamedArgument contract binding
  [ContractClassFor(typeof(INamedArgument))]
  abstract class INamedArgumentContract : INamedArgument {
    #region INamedArgument Members

    /// <summary>
    /// The name of the parameter or property or field that corresponds to the argument.
    /// </summary>
    public IName ArgumentName {
      get {
        Contract.Ensures(Contract.Result<IName>() != null);
        throw new NotImplementedException(); 
      }
    }

    /// <summary>
    /// The value of the argument.
    /// </summary>
    public IExpression ArgumentValue {
      get {
        Contract.Ensures(Contract.Result<IExpression>() != null);
        throw new NotImplementedException(); 
      }
    }

    /// <summary>
    /// Returns either null or the parameter or property or field that corresponds to this argument.
    /// </summary>
    public object ResolvedDefinition {
      get {
        Contract.Ensures(Contract.Result<object>() == null || Contract.Result<object>() is IParameterDefinition || 
          Contract.Result<object>() is IPropertyDefinition || Contract.Result<object>() is IFieldDefinition);
        throw new NotImplementedException(); 
      }
    }

    /// <summary>
    /// If true, the resolved definition is a property whose getter is virtual.
    /// </summary>
     public bool GetterIsVirtual {
      get { throw new NotImplementedException(); }
    }

     /// <summary>
     /// If true, the resolved definition is a property whose setter is virtual.
     /// </summary>
    public bool SetterIsVirtual {
      get { throw new NotImplementedException(); }
    }

   #endregion

    #region IExpression Members

    public void Dispatch(ICodeVisitor visitor) {
      throw new NotImplementedException();
    }

    public ITypeReference Type {
      get { throw new NotImplementedException(); }
    }

    #endregion

    #region IObjectWithLocations Members

    public IEnumerable<ILocation> Locations {
      get { throw new NotImplementedException(); }
    }

    #endregion
  }
  #endregion

  /// <summary>
  /// An expression that results in false if both operands represent the same value or object.
  /// </summary>
  public interface INotEquality : IBinaryOperation {
  }

  /// <summary>
  /// An expression that represents the value that a target expression had at the start of the method that has a postcondition that includes this expression.
  /// This node must be replaced before converting the Code Model to IL.
  /// </summary>
  [ContractClass(typeof(IOldValueContract))]
  public interface IOldValue : IExpression {
    /// <summary>
    /// The expression whose value at the start of method execution is referred to in the method postcondition.
    /// </summary>
    IExpression Expression { get; }
  }

  #region IOldValue contract binding
  [ContractClassFor(typeof(IOldValue))]
  abstract class IOldValueContract : IOldValue {
    #region IOldValue Members

    public IExpression Expression {
      get {
        Contract.Ensures(Contract.Result<IExpression>() != null);
        throw new NotImplementedException(); 
      }
    }

    #endregion

    #region IExpression Members

    public void Dispatch(ICodeVisitor visitor) {
      throw new NotImplementedException();
    }

    public ITypeReference Type {
      get { throw new NotImplementedException(); }
    }

    #endregion

    #region IObjectWithLocations Members

    public IEnumerable<ILocation> Locations {
      get { throw new NotImplementedException(); }
    }

    #endregion
  }
  #endregion

  /// <summary>
  /// An expression that results in the bitwise not (1's complement) of the operand.
  /// </summary>
  public interface IOnesComplement : IUnaryOperation {
  }

  /// <summary>
  /// An expression that must match an out parameter of a method. The method assigns a value to the target Expression.
  /// </summary>
  public interface IOutArgument : IExpression {
    /// <summary>
    /// The target that is assigned to as a result of the method call.
    /// </summary>
    ITargetExpression Expression { get; }
  }

  /// <summary>
  /// An expression that calls a method indirectly via a function pointer.
  /// </summary>
  [ContractClass(typeof(IPointerCallContract))]
  public interface IPointerCall : IExpression {

    /// <summary>
    /// The arguments to pass to the method, after they have been converted to match the parameters of the method.
    /// </summary>
    IEnumerable<IExpression> Arguments { get; }

    /// <summary>
    /// True if this method call terminates the calling method. It indicates that the calling method's stack frame is not required
    /// and can be removed before executing the call.
    /// </summary>
    bool IsTailCall {
      get;
    }

    /// <summary>
    /// An expression that results at runtime in a function pointer that points to the actual method to call.
    /// </summary>
    IExpression Pointer {
      get;
    }

  }

  #region IPointerCall contract binding
  [ContractClassFor(typeof(IPointerCall))]
  abstract class IPointerCallContract : IPointerCall {
    public IEnumerable<IExpression> Arguments {
      get {
        Contract.Ensures(Contract.Result<IEnumerable<IExpression>>() != null);
        throw new NotImplementedException(); 
      }
    }

    public bool IsTailCall {
      get { throw new NotImplementedException(); }
    }

    public IExpression Pointer {
      get {
        Contract.Ensures(Contract.Result<IExpression>() != null);
        Contract.Ensures(Contract.Result<IExpression>().Type is IFunctionPointerTypeReference);
        throw new NotImplementedException(); 
      }
    }

    public void Dispatch(ICodeVisitor visitor) {
      throw new NotImplementedException();
    }

    public ITypeReference Type {
      get { throw new NotImplementedException(); }
    }

    public IEnumerable<ILocation> Locations {
      get { throw new NotImplementedException(); }
    }
  }
  #endregion


  /// <summary>
  /// An expression that results in the value on top of the implicit operand stack and that also pops that value from the stack.
  /// </summary>
  public interface IPopValue : IExpression {
  }

  /// <summary>
  /// An expression that must match a ref parameter of a method. 
  /// The value, before the call, of the addressable Expression is passed to the method and the method may assign a new value to the 
  /// addressable Expression during the call.
  /// </summary>
  [ContractClass(typeof(IRefArgumentContract))]
  public interface IRefArgument : IExpression {
    /// <summary>
    /// The target that is assigned to as a result of the method call, but whose value is also passed to the method at the start of the call.
    /// </summary>
    IAddressableExpression Expression { get; }
  }

  #region IRefArgument contract binding
  [ContractClassFor(typeof(IRefArgument))]
  abstract class IRefArgumentContract : IRefArgument {
    #region IRefArgument Members

    public IAddressableExpression Expression {
      get {
        Contract.Ensures(Contract.Result<IAddressableExpression>() != null);
        throw new NotImplementedException(); 
      }
    }

    #endregion

    #region IExpression Members

    public void Dispatch(ICodeVisitor visitor) {
      throw new NotImplementedException();
    }

    public ITypeReference Type {
      get { throw new NotImplementedException(); }
    }

    #endregion

    #region IObjectWithLocations Members

    public IEnumerable<ILocation> Locations {
      get { throw new NotImplementedException(); }
    }

    #endregion
  }
  #endregion


  /// <summary>
  /// An expression that refers to the return value of a method.
  /// This node must be replaced before converting the Code Model to IL.
  /// </summary>
  public interface IReturnValue : IExpression {
  }

  /// <summary>
  /// An expression that results in the value of the left operand, shifted right by the number of bits specified by the value of the right operand, duplicating the sign bit.
  /// </summary>
  public interface IRightShift : IBinaryOperation {
  }

  /// <summary>
  /// An expression that denotes the runtime argument handle of a method that accepts extra arguments. 
  /// This expression corresponds to __arglist in C# and results in a value that can be used as the argument to the constructor for System.ArgIterator.
  /// </summary>
  public interface IRuntimeArgumentHandleExpression : IExpression {
  }

  /// <summary>
  /// An expression that computes the memory size of instances of a given type at runtime.
  /// </summary>
  public interface ISizeOf : IExpression {
    /// <summary>
    /// The type to size.
    /// </summary>
    ITypeReference TypeToSize { get; }
  }

  /// <summary>
  /// An expression that allocates an array on the call stack.
  /// </summary>
  public interface IStackArrayCreate : IExpression {

    /// <summary>
    /// The type of the elements of the stack array. This type must be unmanaged (contain no pointers to objects on the heap managed by the garbage collector).
    /// </summary>
    ITypeReference ElementType { get; }

    /// <summary>
    /// The size (number of bytes) of the stack array.
    /// </summary>
    IExpression Size { get; }

  }

  /// <summary>
  /// An expression that subtracts the value of the right operand from the value of the left operand. 
  /// </summary>
  public interface ISubtraction : IBinaryOperation {
    /// <summary>
    /// The subtraction must be performed with a check for arithmetic overflow and the operands must be integers.
    /// </summary>
    bool CheckOverflow {
      get;
    }

    /// <summary>
    /// If true the operands must be integers and are treated as being unsigned for the purpose of the subtraction. This only makes a difference if CheckOverflow is true as well.
    /// </summary>
    bool TreatOperandsAsUnsignedIntegers {
      get;
    }
  }

  /// <summary>
  /// An expression that can be the target of an assignment statement or that can be passed an argument to an out parameter.
  /// </summary>
  [ContractClass(typeof(ITargetExpressionContract))]
  public interface ITargetExpression : IExpression {

    /// <summary>
    /// If Definition is a field and the field is not aligned with natural size of its type, this property specifies the actual alignment.
    /// For example, if the field is byte aligned, then the result of this property is 1. Likewise, 2 for word (16-bit) alignment and 4 for
    /// double word (32-bit alignment). 
    /// </summary>
    byte Alignment {
      get;
      //^ requires IsUnaligned;
      //^ ensures result == 1 || result == 2 || result == 4;
    }

    /// <summary>
    /// The local variable, parameter, field, property, array element or pointer target that this expression denotes.
    /// </summary>
    object Definition {
      get;
      //^ ensures result is ILocalDefinition || result is IParameterDefinition || result is IFieldReference || result is IArrayIndexer 
      //^   || result is IAddressDereference || result is IPropertyDefinition || result is IThisReference;
      //^ ensures result is IPropertyDefinition ==> ((IPropertyDefinition)result).Setter != null;
    }

    /// <summary>
    /// If true, the resolved definition is a property whose getter is virtual.
    /// </summary>
    bool GetterIsVirtual { get; }

    /// <summary>
    /// If true, the resolved definition is a property whose setter is virtual.
    /// </summary>
    bool SetterIsVirtual { get; }

    /// <summary>
    /// The instance to be used if this.Definition is an instance field/property or array indexer.
    /// </summary>
    IExpression/*?*/ Instance { get; }

    /// <summary>
    /// True if the definition is a field and the field is not aligned with the natural size of its type.
    /// For example if the field type is Int32 and the field is aligned on an Int16 boundary.
    /// </summary>
    bool IsUnaligned { get; }

    /// <summary>
    /// The bound Definition is a volatile field and its contents may not be cached.
    /// </summary>
    bool IsVolatile { get; }

  }

  #region ITargetExpression contract binding
  [ContractClassFor(typeof(ITargetExpression))]
  abstract class ITargetExpressionContract : ITargetExpression {
    #region ITargetExpression Members

    public byte Alignment {
      get {
        Contract.Requires(this.IsUnaligned);
        Contract.Ensures(Contract.Result<byte>() == 1 || Contract.Result<byte>() == 2 || Contract.Result<byte>() == 4);
        throw new NotImplementedException(); 
      }
    }

    public object Definition {
      get {
        Contract.Ensures(Contract.Result<object>() != null);
        Contract.Ensures(Contract.Result<object>() is ILocalDefinition || Contract.Result<object>() is IParameterDefinition || 
          Contract.Result<object>() is IFieldReference || Contract.Result<object>() is IArrayIndexer || 
          Contract.Result<object>() is IAddressDereference || Contract.Result<object>() is IPropertyDefinition || Contract.Result<object>() is IThisReference);
        Contract.Ensures(!(Contract.Result<object>() is IPropertyDefinition) || ((IPropertyDefinition)Contract.Result<object>()).Setter != null);
        throw new NotImplementedException(); 
      }
    }

    /// <summary>
    /// If true, the resolved definition is a property whose getter is virtual.
    /// </summary>
    public bool GetterIsVirtual {
      get { throw new NotImplementedException(); }
    }

    /// <summary>
    /// If true, the resolved definition is a property whose setter is virtual.
    /// </summary>
    public bool SetterIsVirtual {
      get { throw new NotImplementedException(); }
    }

    public IExpression Instance {
      get { throw new NotImplementedException(); }
    }

    public bool IsUnaligned {
      get { throw new NotImplementedException(); }
    }

    public bool IsVolatile {
      get { throw new NotImplementedException(); }
    }

    #endregion

    #region IExpression Members

    public void Dispatch(ICodeVisitor visitor) {
      throw new NotImplementedException();
    }

    public ITypeReference Type {
      get { throw new NotImplementedException(); }
    }

    #endregion

    #region IObjectWithLocations Members

    public IEnumerable<ILocation> Locations {
      get { throw new NotImplementedException(); }
    }

    #endregion
  }
  #endregion


  /// <summary>
  /// Wraps an expression that represents a storage location that can be assigned to or whose address can be computed and passed as a parameter.
  /// Furthermore, this storage location must a string. Also wraps expressions supplying the starting position and length of a substring of the target string.
  /// An assignment of a string to a slice results in a new string where the slice has been replaced with given string.
  /// </summary>
  public interface ITargetSliceExpression : IAddressableExpression {
    /// <summary>
    /// An expression that represents the index of the first character of a string slice.
    /// </summary>
    IExpression StartOfSlice { get; }

    /// <summary>
    /// An expression that represents the length of the slice.
    /// </summary>
    IExpression LengthOfSlice { get; }
  }

  /// <summary>
  /// An expression that binds to the current object instance.
  /// </summary>
  public interface IThisReference : IExpression {
  }

  /// <summary>
  /// An expression that results in an instance of RuntimeFieldHandle, RuntimeMethodHandle or RuntimeTypeHandle.
  /// </summary>
  public interface ITokenOf : IExpression {
    /// <summary>
    /// An instance of IFieldReference, IMethodReference or ITypeReference.
    /// </summary>
    object Definition {
      get;
      //^ ensures result is IFieldReference || result is IMethodReference || result is ITypeReference;
    }
  }

  /// <summary>
  /// An expression that results in a System.Type instance.
  /// </summary>
  public interface ITypeOf : IExpression {
    /// <summary>
    /// The type that will be represented by the System.Type instance.
    /// </summary>
    ITypeReference TypeToGet { get; }
  }

  /// <summary>
  /// An expression that results in the arithmetic negation of the given operand.
  /// </summary>
  public interface IUnaryNegation : IUnaryOperation {
    /// <summary>
    /// The negation must be performed with a check for arithmetic overflow and the operand must be an integer.
    /// </summary>
    bool CheckOverflow {
      get;
    }
  }

  /// <summary>
  /// An operation performed on a single operand.
  /// </summary>
  [ContractClass(typeof(IUnaryOperationContract))]
  public interface IUnaryOperation : IExpression {
    /// <summary>
    /// The value on which the operation is performed.
    /// </summary>
    IExpression Operand { get; }
  }

  #region IUnaryOperation contract binding
  [ContractClassFor(typeof(IUnaryOperation))]
  abstract class IUnaryOperationContract : IUnaryOperation {
    #region IUnaryOperation Members

    public IExpression Operand {
      get {
        Contract.Ensures(Contract.Result<IExpression>() != null);
        throw new NotImplementedException(); 
      }
    }

    #endregion

    #region IExpression Members

    public void Dispatch(ICodeVisitor visitor) {
      throw new NotImplementedException();
    }

    public ITypeReference Type {
      get { throw new NotImplementedException(); }
    }

    #endregion

    #region IObjectWithLocations Members

    public IEnumerable<ILocation> Locations {
      get { throw new NotImplementedException(); }
    }

    #endregion
  }
  #endregion


  /// <summary>
  /// An expression that results in the arithmetic value of the given operand.
  /// </summary>
  public interface IUnaryPlus : IUnaryOperation {
  }

  /// <summary>
  /// An expression that results in the length of a vector (zero-based one-dimensional array).
  /// </summary>
  [ContractClass(typeof(IVectorLengthContract))]
  public interface IVectorLength : IExpression {

    /// <summary>
    /// An expression that results in a value of a vector (zero-based one-dimensional array) type.
    /// </summary>
    IExpression Vector { get; }
  }

  #region IVectorLength contract binding

  [ContractClassFor(typeof(IVectorLength))]
  abstract class IVectorLengthContract : IVectorLength {
    #region IVectorLength Members

    public IExpression Vector {
      get {
        Contract.Ensures(Contract.Result<IExpression>() != null);
        throw new NotImplementedException(); 
      }
    }

    #endregion

    #region IExpression Members

    public void Dispatch(ICodeVisitor visitor) {
      throw new NotImplementedException();
    }

    public ITypeReference Type {
      get { throw new NotImplementedException(); }
    }

    #endregion

    #region IObjectWithLocations Members

    public IEnumerable<ILocation> Locations {
      get { throw new NotImplementedException(); }
    }

    #endregion
  }
  #endregion


}
