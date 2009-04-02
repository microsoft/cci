//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci.MutableCodeModel {

  public sealed class Addition : BinaryOperation, IAddition {

    public Addition() {
    }

    public Addition(IAddition addition)
      : base(addition) {
      this.CheckOverflow = addition.CheckOverflow;
    }

    public bool CheckOverflow { get; set; }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }
  }

  public sealed class AddressableExpression : Expression, IAddressableExpression {

    public AddressableExpression() {
      this.definition = null;
      this.instance = null;
    }

    public AddressableExpression(IAddressableExpression addressableExpression)
      : base(addressableExpression) {
      this.definition = addressableExpression.Definition;
      this.instance = addressableExpression.Instance;
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    public object/*?*/ Definition {
      get { return this.definition; }
      set
        //^ requires result == null || result is ILocalDefinition || result is IParameterDefinition || result is IFieldReference || result is IArrayIndexer 
        //^   || result is IAddressDereference || result is IMethodReference || result is IThisReference;
      {
        this.definition = value;
      }
    }
    object/*?*/ definition;
    //^ invariant definition == null || definition is ILocalDefinition || definition is IParameterDefinition || definition is IFieldReference || definition is IArrayIndexer
    //^   || definition is IAddressDereference || definition is IMethodReference || definition is IThisReference;

    public IExpression/*?*/ Instance {
      get { return this.instance; }
      set { this.instance = value; }
    }
    IExpression/*?*/ instance;

  }

  public sealed class AddressOf : Expression, IAddressOf {

    public AddressOf() {
      this.expression = CodeDummy.AddressableExpression;
    }

    public AddressOf(IAddressOf addressOf)
      : base(addressOf) {
      this.expression = addressOf.Expression;
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    public IAddressableExpression Expression {
      get { return this.expression; }
      set { this.expression = value; }
    }
    IAddressableExpression expression;

    public bool ObjectControlsMutability {
      get { return this.objectControlsMutability; }
      set { this.objectControlsMutability = value; }
    }
    bool objectControlsMutability;

  }

  public sealed class AddressDereference : Expression, IAddressDereference {

    public AddressDereference() {
      this.address = CodeDummy.Expression;
      this.alignment = 0;
      this.isVolatile = false;
    }

    public AddressDereference(IAddressDereference addressDereference)
      : base(addressDereference) {
      this.address = addressDereference.Address;
      if (addressDereference.IsUnaligned)
        this.alignment = addressDereference.Alignment;
      else
        this.alignment = 0;
      this.isVolatile = addressDereference.IsVolatile;
    }

    public IExpression Address {
      get { return this.address; }
      set { this.address = value; }
    }
    IExpression address;

    public ushort Alignment {
      get { return this.alignment; }
      set 
        //^ requires value == 1 || value == 2 || value == 4;
      { 
        this.alignment = value; 
      }
    }
    ushort alignment;

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    public bool IsUnaligned {
      get { return this.alignment > 0; }
    }

    public bool IsVolatile {
      get { return this.isVolatile; }
      set { this.isVolatile = value; }
    }
    bool isVolatile;

  }

  public sealed class AnonymousDelegate : Expression, IAnonymousDelegate {

    public AnonymousDelegate() {
      this.body = CodeDummy.Block;
      this.callingConvention = CallingConvention.Default;
      this.parameters = new List<IParameterDefinition>();
      this.returnType = Dummy.TypeReference;
      this.returnValueCustomModifiers = new List<ICustomModifier>();
      this.returnValueIsByRef = false;
    }

    public AnonymousDelegate(IAnonymousDelegate anonymousDelegate) 
      : base (anonymousDelegate) {
      this.body = anonymousDelegate.Body;
      this.callingConvention = anonymousDelegate.CallingConvention;
      this.parameters = new List<IParameterDefinition>(anonymousDelegate.Parameters);
      this.returnType = anonymousDelegate.ReturnType;
      this.returnValueCustomModifiers = new List<ICustomModifier>(anonymousDelegate.ReturnValueCustomModifiers);
      this.returnValueIsByRef = anonymousDelegate.ReturnValueIsByRef;
    }

    public IBlockStatement Body {
      get { return this.body; }
      set { this.body = value; }
    }
    IBlockStatement body;

    public CallingConvention CallingConvention {
      get { return this.callingConvention; }
      set { this.callingConvention = value; }
    }
    CallingConvention callingConvention;

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    public List<IParameterDefinition> Parameters {
      get { return this.parameters; }
      set { this.parameters = value; }
    }
    List<IParameterDefinition> parameters;

    public ITypeReference ReturnType {
      get { return this.returnType; }
      set { this.returnType = value; }
    }
    ITypeReference returnType;

    public List<ICustomModifier> ReturnValueCustomModifiers {
      get { return this.returnValueCustomModifiers; }
      set { this.returnValueCustomModifiers = value; }
    }
    List<ICustomModifier> returnValueCustomModifiers;

    public bool ReturnValueIsByRef {
      get { return this.returnValueIsByRef; }
      set { this.returnValueIsByRef = value; }
    }
    bool returnValueIsByRef;

    public bool ReturnValueIsModified {
      get { return this.returnValueCustomModifiers.Count > 0; }
    }

    #region IAnonymousDelegate Members

    IEnumerable<IParameterDefinition> IAnonymousDelegate.Parameters {
      get { return this.Parameters.AsReadOnly(); }
    }

    #endregion

    #region ISignature Members

    IEnumerable<IParameterTypeInformation> ISignature.Parameters {
      get { return IteratorHelper.GetConversionEnumerable<IParameterDefinition, IParameterTypeInformation>(this.parameters); }
    }

    IEnumerable<ICustomModifier> ISignature.ReturnValueCustomModifiers {
      get { return this.returnValueCustomModifiers.AsReadOnly(); }
    }

    #endregion
  }

  /// <summary>
  /// An expression that creates an instance of an array whose element type is determined by the initial values of the elements.
  /// </summary>
  public sealed class ArrayIndexer : Expression, IArrayIndexer {

    public ArrayIndexer() {
      this.indexedObject = CodeDummy.Expression;
      this.indices = new List<IExpression>();
    }

    public ArrayIndexer(IArrayIndexer arrayIndexer)
      : base(arrayIndexer) {
      this.indexedObject = arrayIndexer.IndexedObject;
      this.indices = new List<IExpression>(arrayIndexer.Indices);
    }

    public IExpression IndexedObject {
      get { return this.indexedObject; }
      set {
        if (value is Conditional) {
        }
        this.indexedObject = value; 
      }
    }
    IExpression indexedObject;

    public List<IExpression> Indices {
      get { return this.indices; }
      set { this.indices = value; }
    }
    List<IExpression> indices;

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    #region IArrayIndexer Members

    IEnumerable<IExpression> IArrayIndexer.Indices {
      get { return this.indices.AsReadOnly(); }
    }

    #endregion
  }

  public sealed class Assignment : Expression, IAssignment {

    public Assignment() {
      this.source = CodeDummy.Expression;
      this.target = CodeDummy.TargetExpression;
    }

    public Assignment(IAssignment assignment)
      : base(assignment) {
      this.source = assignment.Source;
      this.target = assignment.Target;
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    public IExpression Source {
      get { return this.source; }
      set { this.source = value; }
    }
    IExpression source;

    public ITargetExpression Target {
      get { return this.target; }
      set { this.target = value; }
    }
    ITargetExpression target;
 
  }

  public sealed class BaseClassReference : Expression, IBaseClassReference {

    public BaseClassReference() {
    }

    public BaseClassReference(IBaseClassReference baseClassReference)
      : base(baseClassReference) {
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

   }

  /// <summary>
  /// A binary operation performed on a left and right operand.
  /// </summary>
  public abstract class BinaryOperation : Expression, IBinaryOperation {

    internal BinaryOperation() {
      this.leftOperand = CodeDummy.Expression;
      this.rightOperand = CodeDummy.Expression;
    }

    internal BinaryOperation(IBinaryOperation binaryOperation)
      : base(binaryOperation) {
      this.leftOperand = binaryOperation.LeftOperand;
      this.rightOperand = binaryOperation.RightOperand;
    }

    /// <summary>
    /// The left operand.
    /// </summary>
    public IExpression LeftOperand {
      get { return this.leftOperand; }
      set { this.leftOperand = value; }
    }
    IExpression leftOperand;

    /// <summary>
    /// The right operand.
    /// </summary>
    public IExpression RightOperand {
      get { return this.rightOperand; }
      set { this.rightOperand = value; }
    }
    IExpression rightOperand;

  }

  public sealed class BitwiseAnd : BinaryOperation, IBitwiseAnd {

    public BitwiseAnd() {
    }

    public BitwiseAnd(IBitwiseAnd bitwiseAnd)
      : base(bitwiseAnd) {
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

  }

  public sealed class BitwiseOr : BinaryOperation, IBitwiseOr {

    public BitwiseOr() {
    }

    public BitwiseOr(IBitwiseOr bitwiseOr)
      : base(bitwiseOr) {
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

  }

  public sealed class BlockExpression : Expression, IBlockExpression {

    public BlockExpression() {
      this.blockStatement = CodeDummy.Block;
      this.expression = CodeDummy.Expression;
    }

    public BlockExpression(IBlockExpression blockExpression)
      : base(blockExpression) {
      this.blockStatement = blockExpression.BlockStatement;
      this.expression = blockExpression.Expression;
    }

    public IBlockStatement BlockStatement {
      get { return this.blockStatement; }
      set { this.blockStatement = value; }
    }
    IBlockStatement blockStatement;

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    public IExpression Expression {
      get { return this.expression; }
      set { this.expression = value; }
    }
    IExpression expression;

  }

  public sealed class BoundExpression : Expression, IBoundExpression {

    public BoundExpression() {
      this.alignment = 0;
      this.definition = CodeDummy.Expression;
      this.instance = null;
      this.isVolatile = false;
    }

    public BoundExpression(IBoundExpression boundExpression)
      : base(boundExpression) {
      if (boundExpression.IsUnaligned)
        this.alignment = boundExpression.Alignment;
      else
        this.alignment = 0;
      this.definition = boundExpression.Definition;
      this.instance = boundExpression.Instance;
      this.isVolatile = boundExpression.IsVolatile;
    }

    public byte Alignment {
      get
        //^^ requires !this.IsUnaligned;
        //^^ ensures result == 1 || result == 2 || result == 4;
      {
        //^ assume this.alignment == 1 || this.alignment == 2 || this.alignment == 4;
        return this.alignment; 
      }
      set 
        //^ requires value == 1 || value == 2 || value == 4;
      { 
        this.alignment = value; 
      }
    }
    //^ [SpecPublic]
    byte alignment;
    //^ invariant alignment == 0 || alignment == 1 || alignment == 2 || alignment == 4;

    public object Definition {
      get { return this.definition; }
      set { this.definition = value; }
    }
    object definition;

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    public IExpression/*?*/ Instance {
      get { return this.instance; }
      set { this.instance = value; }
    }
    IExpression/*?*/ instance;

    public bool IsUnaligned {
      get
        //^^ ensures result == (this.alignment != 1 && this.alignment != 2 && this.alignment != 4; )
      { 
        return this.alignment != 1 && this.alignment != 2 && this.alignment != 4; 
      }
    }

    public bool IsVolatile {
      get { return this.isVolatile; }
      set { this.isVolatile = value; }
    }
    bool isVolatile;
  }

  public sealed class CastIfPossible : Expression, ICastIfPossible {

    public CastIfPossible() {
      this.targetType = Dummy.TypeReference;
      this.valueToCast = CodeDummy.Expression;
    }

    public CastIfPossible(ICastIfPossible castIfPossible)
      : base(castIfPossible) {
      this.targetType = castIfPossible.TargetType;
      this.valueToCast = castIfPossible.ValueToCast;
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    public ITypeReference TargetType {
      get { return this.targetType; }
      set { this.targetType = value; }
    }
    ITypeReference targetType;

    public IExpression ValueToCast {
      get { return this.valueToCast; }
      set { this.valueToCast = value; }
    }
    IExpression valueToCast;

  }

  public sealed class CheckIfInstance : Expression, ICheckIfInstance {

    public CheckIfInstance() {
      this.operand = CodeDummy.Expression;
      this.type = Dummy.TypeReference;
    }

    public CheckIfInstance(ICheckIfInstance checkIfInstance)
      : base(checkIfInstance) {
      this.operand = checkIfInstance.Operand;
      this.type = checkIfInstance.TypeToCheck;
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    public IExpression Operand {
      get { return this.operand; }
      set { this.operand = value; }
    }
    IExpression operand;

    public ITypeReference TypeToCheck {
      get { return this.type; }
      set { this.type = value; }
    }
    ITypeReference type;

  }

  public sealed class Conversion : Expression, IConversion {

    public Conversion() {
      this.checkNumericRange = false;
      this.typeAfterConversion = Dummy.TypeReference;
      this.valueToConvert = CodeDummy.Expression;
    }

    public Conversion(IConversion conversion)
      : base(conversion) {
      this.checkNumericRange = conversion.CheckNumericRange;
      this.typeAfterConversion = conversion.TypeAfterConversion;
      this.valueToConvert = conversion.ValueToConvert;
    }

    public bool CheckNumericRange {
      get { return this.checkNumericRange; }
      set { this.checkNumericRange = value; }
    }
    bool checkNumericRange;

    //^ [MustOverride]
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    public ITypeReference TypeAfterConversion {
      get { return this.typeAfterConversion; }
      set { this.typeAfterConversion = value; }
    }
    ITypeReference typeAfterConversion;

    public IExpression ValueToConvert {
      get { return this.valueToConvert; }
      set { this.valueToConvert = value; }
    }
    IExpression valueToConvert;

  }

  /// <summary>
  /// An expression that does not change its value at runtime and that can be evaluated at compile time.
  /// </summary>
  public sealed class CompileTimeConstant : Expression, ICompileTimeConstant, IMetadataConstant {

    public CompileTimeConstant() {
      this.value = null;
    }

    public CompileTimeConstant(ICompileTimeConstant compileTimeConstant)
      : base(compileTimeConstant) {
      this.value = compileTimeConstant.Value;
      this.Type = compileTimeConstant.Type;
    }

    /// <summary>
    /// Calls the visitor.Visit(T) method where T is the most derived object model node interface type implemented by the concrete type
    /// of the object implementing IExpression. The dispatch method does not invoke Dispatch on any child objects. If child traversal
    /// is desired, the implementations of the Visit methods should do the subsequent dispatching.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(ICompileTimeConstant) method.
    /// </summary>
    public void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The compile time value of the expression. Can be null.
    /// </summary>
    public object/*?*/ Value {
      get { return this.value; }
      set { this.value = value; }
    }
    object/*?*/ value;

    #region IMetadataExpression Members

    IEnumerable<ILocation> IMetadataExpression.Locations {
      get { return ((IExpression)this).Locations; }
    }

    ITypeReference IMetadataExpression.Type {
      get { return this.Type; }
    }

    #endregion
  }

  public sealed class Conditional : Expression, IConditional {

    public Conditional() {
      this.condition = CodeDummy.Expression;
      this.resultIfFalse = CodeDummy.Expression;
      this.resultIfTrue = CodeDummy.Expression;
    }

    public Conditional(IConditional conditional)
      : base(conditional) {
      this.condition = conditional.Condition;
      this.resultIfFalse = conditional.ResultIfFalse;
      this.resultIfTrue = conditional.ResultIfTrue;
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    public IExpression Condition {
      get { return this.condition; }
      set { this.condition = value; }
    }
    IExpression condition;

    public IExpression ResultIfFalse {
      get { return this.resultIfFalse; }
      set { this.resultIfFalse = value; }
    }
    IExpression resultIfFalse;

    public IExpression ResultIfTrue {
      get { return this.resultIfTrue; }
      set { this.resultIfTrue = value; }
    }
    IExpression resultIfTrue;

   }

  public abstract class ConstructorOrMethodCall : Expression {

    internal ConstructorOrMethodCall() {
      this.arguments = new List<IExpression>();
      this.methodToCall = Dummy.MethodReference;
    }

    internal ConstructorOrMethodCall(ICreateObjectInstance createObjectInstance)
      : base(createObjectInstance) {
      this.arguments = new List<IExpression>(createObjectInstance.Arguments);
      this.methodToCall = createObjectInstance.MethodToCall;
    }

    internal ConstructorOrMethodCall(IMethodCall methodCall)
      : base(methodCall) {
      this.arguments = new List<IExpression>(methodCall.Arguments);
      this.methodToCall = methodCall.MethodToCall;
    }

    public List<IExpression> Arguments {
      get { return this.arguments; }
      set { this.arguments = value; }
    }
    internal List<IExpression> arguments;

    public IMethodReference MethodToCall {
      get { return this.methodToCall; }
      set { this.methodToCall = value; }
    }
    IMethodReference methodToCall;

  }

  public sealed class CreateArray : Expression, ICreateArray, IMetadataCreateArray {

    public CreateArray() {
      this.elementType = Dummy.TypeReference;
      this.initializers = new List<IExpression>();
      this.lowerBounds = new List<int>();
      this.rank = 0;
      this.sizes = new List<IExpression>();
    }

    public CreateArray(ICreateArray createArray)
      : base(createArray) {
      this.elementType = createArray.ElementType;
      this.initializers = new List<IExpression>(createArray.Initializers);
      this.lowerBounds = new List<int>(createArray.LowerBounds);
      this.rank = createArray.Rank;
      this.sizes = new List<IExpression>(createArray.Sizes);
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(ICreateArray) method.
    /// </summary>
    public void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public ITypeReference ElementType {
      get { return this.elementType; }
      set { this.elementType = value; }
    }
    ITypeReference elementType;

    public List<IExpression> Initializers {
      get { return this.initializers; }
      set { this.initializers = value; }
    }
    List<IExpression> initializers;

    public List<int> LowerBounds {
      get { return this.lowerBounds; }
      set { this.lowerBounds = value; }
    }
    List<int> lowerBounds;

    public uint Rank {
      get { return this.rank; }
      set
        //^ requires value > 0;
      {
        this.rank = value;
      }
    }
    uint rank;

    public List<IExpression> Sizes {
      get { return this.sizes; }
      set { this.sizes = value; }
    }
    List<IExpression> sizes;

    #region IArrayCreate Members

    IEnumerable<IExpression> ICreateArray.Initializers {
      get { return this.initializers.AsReadOnly(); }
    }

    IEnumerable<int> ICreateArray.LowerBounds {
      get { return this.lowerBounds.AsReadOnly(); }
    }

    IEnumerable<IExpression> ICreateArray.Sizes {
      get { return this.sizes.AsReadOnly(); }
    }

    #endregion

    #region IMetadataCreateArray Members

    IEnumerable<IMetadataExpression> IMetadataCreateArray.Initializers {
      get {
        foreach (IExpression expression in this.initializers) {
          IMetadataExpression/*?*/ metadataExpression = expression as IMetadataExpression;
          if (metadataExpression != null) yield return metadataExpression;
        }
      }
    }

    IEnumerable<int> IMetadataCreateArray.LowerBounds {
      get { return this.lowerBounds.AsReadOnly(); }
    }

    IEnumerable<ulong> IMetadataCreateArray.Sizes {
      get {
        foreach (IExpression size in this.Sizes) {
          ulong s = 0;
          IMetadataConstant/*?*/ cconst = size as IMetadataConstant;
          if (cconst != null && cconst.Value is ulong)
            s = (ulong)cconst.Value;
          yield return s;
        }
      }
    }
    #endregion

    #region IMetadataExpression Members

    IEnumerable<ILocation> IMetadataExpression.Locations {
      get { return ((IExpression)this).Locations; }
    }

    ITypeReference IMetadataExpression.Type {
      get { return this.Type; }
    }

    #endregion

  }

  public sealed class CreateDelegateInstance : Expression, ICreateDelegateInstance {

    public CreateDelegateInstance() {
      this.instance = null;
      this.methodToCallViaDelegate = Dummy.MethodReference;
    }

    public CreateDelegateInstance(ICreateDelegateInstance createDelegateInstance)
      : base(createDelegateInstance) {
      this.instance = createDelegateInstance.Instance;
      this.methodToCallViaDelegate = createDelegateInstance.MethodToCallViaDelegate;
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    public IExpression/*?*/ Instance {
      get { return this.instance; }
      set
        //^ requires this.MethodToCallViaDelegate.ResolvedMethod.IsStatic <==> value == null;
      { 
        this.instance = value; 
      }
    }
    IExpression/*?*/ instance;

    public IMethodReference MethodToCallViaDelegate {
      get { return this.methodToCallViaDelegate; }
      set { this.methodToCallViaDelegate = value; }
    }
    IMethodReference methodToCallViaDelegate;

  }

  public sealed class CreateObjectInstance : ConstructorOrMethodCall, ICreateObjectInstance {

    public CreateObjectInstance() {
    }

    public CreateObjectInstance(ICreateObjectInstance createObjectInstance)
      : base(createObjectInstance) {
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    #region ICreateObjectInstance Members

    IEnumerable<IExpression> ICreateObjectInstance.Arguments {
      get { return this.arguments.AsReadOnly(); }
    }

    #endregion
  }

  public sealed class DefaultValue : Expression, IDefaultValue {

    public DefaultValue() {
      this.defaultValueType = Dummy.TypeReference;
    }

    public DefaultValue(IDefaultValue defaultValue)
      : base(defaultValue) {
      this.defaultValueType = defaultValue.DefaultValueType;
    }

    public ITypeReference DefaultValueType {
      get { return this.defaultValueType; }
      set { this.defaultValueType = value; }
    }
    ITypeReference defaultValueType;

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

  }

  public sealed class Division : BinaryOperation, IDivision {

    public Division() {
    }

    public Division(IDivision division)
      : base(division) {
      this.CheckOverflow = division.CheckOverflow;
    }

    public bool CheckOverflow { get; set; }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }
  }

  public sealed class Equality : BinaryOperation, IEquality {

    public Equality() {
    }

    public Equality(IEquality equality)
      : base(equality) {
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

  }

  public sealed class ExclusiveOr : BinaryOperation, IExclusiveOr {

    public ExclusiveOr() {
    }

    public ExclusiveOr(IExclusiveOr exclusiveOr)
      : base(exclusiveOr) {
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

  }

  /// <summary>
  /// An expression results in a value of some type.
  /// </summary>
  public abstract class Expression : IExpression {

    protected Expression() {
      this.locations = new List<ILocation>(1);
      this.type = Dummy.TypeReference;
    }

    protected Expression(IExpression expression) {
      this.locations = new List<ILocation>(expression.Locations);
      this.type = expression.Type;
    }

    /// <summary>
    /// Calls the visitor.Visit(T) method where T is the most derived object model node interface type implemented by the concrete type
    /// of the object implementing IDoubleDispatcher. The dispatch method does not invoke Dispatch on any child objects. If child traversal
    /// is desired, the implementations of the Visit methods should do the subsequent dispatching.
    /// </summary>
    public abstract void Dispatch(ICodeVisitor visitor);

    /// <summary>
    /// Checks the expression for errors and returns true if any were found.
    /// </summary>
    public bool HasErrors() {
      return false;
    }

    public bool IsPure
    {
      get { return false; }
    }

    public List<ILocation> Locations {
      get { return this.locations; }
      set { this.locations = value; }
    }
    List<ILocation> locations;

    /// <summary>
    /// The type of value the expression will evaluate to, as determined at compile time.
    /// </summary>
    public ITypeReference Type {
      get { return this.type; }
      set { this.type = value; }
    }
    ITypeReference type;

    #region IExpression Members

    IEnumerable<ILocation> IExpression.Locations {
      get { return this.locations.AsReadOnly(); }
    }

    #endregion
  }

  public sealed class GetTypeOfTypedReference : Expression, IGetTypeOfTypedReference {

    public GetTypeOfTypedReference() {
      this.typedReference = CodeDummy.Expression;
    }

    public GetTypeOfTypedReference(IGetTypeOfTypedReference getTypeOfTypedReference)
      : base(getTypeOfTypedReference) {
      this.typedReference = getTypeOfTypedReference.TypedReference;
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    public IExpression TypedReference {
      get { return this.typedReference; }
      set { this.typedReference = value; }
    }
    IExpression typedReference;

  }

  public sealed class GetValueOfTypedReference : Expression, IGetValueOfTypedReference {

    public GetValueOfTypedReference() {
      this.targetType = Dummy.TypeReference;
      this.typedReference = CodeDummy.Expression;
    }

    public GetValueOfTypedReference(IGetValueOfTypedReference getValueOfTypedReference)
      : base(getValueOfTypedReference) {
      this.targetType = getValueOfTypedReference.TargetType;
      this.typedReference = getValueOfTypedReference.TypedReference;
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    public ITypeReference TargetType {
      get { return this.targetType; }
      set { this.targetType = value; }
    }
    ITypeReference targetType;

    public IExpression TypedReference {
      get { return this.typedReference; }
      set { this.typedReference = value; }
    }
    IExpression typedReference;

  }

  public sealed class GreaterThan : BinaryOperation, IGreaterThan {

    public GreaterThan() {
    }

    public GreaterThan(IGreaterThan greaterThan)
      : base(greaterThan) {
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

  }

  public sealed class GreaterThanOrEqual : BinaryOperation, IGreaterThanOrEqual {

    public GreaterThanOrEqual() {
    }

    public GreaterThanOrEqual(IGreaterThanOrEqual greaterThanOrEqual)
      : base(greaterThanOrEqual) {
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

  }

  public sealed class LeftShift : BinaryOperation, ILeftShift {

    public LeftShift() {
    }

    public LeftShift(ILeftShift leftShift)
      : base(leftShift) {
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

  }

  public sealed class LessThan : BinaryOperation, ILessThan {

    public LessThan() {
    }

    public LessThan(ILessThan lessThan)
      : base(lessThan) {
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

  }

  public sealed class LessThanOrEqual : BinaryOperation, ILessThanOrEqual {

    public LessThanOrEqual() {
    }

    public LessThanOrEqual(ILessThanOrEqual lessThanOrEqual)
      : base(lessThanOrEqual) {
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

  }

  public sealed class LogicalNot : UnaryOperation, ILogicalNot {

    public LogicalNot() {
    }

    public LogicalNot(ILogicalNot logicalNot)
      : base(logicalNot) {
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

  }

  public sealed class MakeTypedReference : Expression, IMakeTypedReference {

    public MakeTypedReference() {
      this.operand = CodeDummy.Expression;
    }

    public MakeTypedReference(IMakeTypedReference makeTypedReference)
      : base(makeTypedReference) {
      this.operand = makeTypedReference.Operand;
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    public IExpression Operand {
      get { return this.operand; }
      set { this.operand = value; }
    }
    IExpression operand;


  }

  /// <summary>
  /// An expression that invokes a method.
  /// </summary>
  public sealed class MethodCall : ConstructorOrMethodCall, IMethodCall {

    public MethodCall() {
      this.isVirtualCall = false;
      this.isStaticCall = false;
      this.thisArgument = CodeDummy.Expression;
    }

    public MethodCall(IMethodCall methodCall)
      : base(methodCall) 
    {
      this.isVirtualCall = methodCall.IsVirtualCall;
      this.isStaticCall = methodCall.IsStaticCall;
      if (!methodCall.IsStaticCall) {
        this.thisArgument = methodCall.ThisArgument;
      } else
        this.thisArgument = CodeDummy.Expression;
      //^ assume false; //invariant involves properties
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    public bool IsVirtualCall {
      get
        //^ ensures result ==> this.MethodToCall.ResolvedMethod.IsVirtual;
      { 
        //^ assume false;
        return this.isVirtualCall; 
      }
      set
        //^ requires value ==> this.MethodToCall.ResolvedMethod.IsVirtual;
      { 
        this.isVirtualCall = value; 
      }
    }
    bool isVirtualCall;
    //^ invariant isVirtualCall ==> this.MethodToCall.ResolvedMethod.IsVirtual;

    public bool IsStaticCall {
      get
        //^ ensures result ==> this.MethodToCall.ResolvedMethod.IsStatic;
      {
        //^ assume false;
        return this.isStaticCall;
      }
      set
        //^ requires value ==> this.MethodToCall.ResolvedMethod.IsStatic;
      {
        this.isStaticCall = value;
      }
    }
    bool isStaticCall;
    //^ invariant isStaticCall ==> this.MethodToCall.ResolvedMethod.IsStatic;

    public bool IsTailCall {
      get { return this.isTailCall; }
      set { this.isTailCall = value; }
    }
    bool isTailCall;

    public IExpression ThisArgument {
      get { return this.thisArgument; }
      set { this.thisArgument = value; }
    }
    IExpression thisArgument;

    #region IMethodCall Members

    IEnumerable<IExpression> IMethodCall.Arguments {
      get { return this.arguments.AsReadOnly(); }
    }

    #endregion
  }

  public sealed class Modulus : BinaryOperation, IModulus {

    public Modulus() {
    }

    public Modulus(IModulus modulus)
      : base(modulus) {
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

  }

  public sealed class Multiplication : BinaryOperation, IMultiplication {

    public Multiplication() {
    }

    public Multiplication(IMultiplication multiplication)
      : base(multiplication) {
      this.CheckOverflow = multiplication.CheckOverflow;
    }

    public bool CheckOverflow { get; set; }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }
  }

  public sealed class NamedArgument : Expression, INamedArgument, IMetadataNamedArgument {

    public NamedArgument() {
      this.argumentName = Dummy.Name;
      this.argumentValue = CodeDummy.Expression;
      this.resolvedDefinition = null;
    }

    public NamedArgument(INamedArgument namedArgument)
      : base(namedArgument) {
      this.argumentName = namedArgument.ArgumentName;
      this.argumentValue = namedArgument.ArgumentValue;
      this.resolvedDefinition = namedArgument.ResolvedDefinition;
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    public IName ArgumentName {
      get { return this.argumentName; }
      set { this.argumentName = value; }
    }
    IName argumentName;

    public IExpression ArgumentValue {
      get { return this.argumentValue; }
      set { this.argumentValue = value; }
    }
    IExpression argumentValue;

    /// <summary>
    /// Returns either null or the parameter or property or field that corresponds to this argument.
    /// </summary>
    public object/*?*/ ResolvedDefinition{
      get { return this.resolvedDefinition; }
      set
        //^ requires value == null || value is IParameterDefinition || value is IPropertyDefinition || value is IFieldDefinition;
      { 
        this.resolvedDefinition = value; 
      }
    }
    object/*?*/ resolvedDefinition;

    #region IMetadataNamedArgument

    IMetadataExpression IMetadataNamedArgument.ArgumentValue {
      get {
        IMetadataExpression/*?*/ result = this.ArgumentValue as IMetadataExpression;
        if (result == null) result = Dummy.Expression;
        return result;
      }
    }

    bool IMetadataNamedArgument.IsField {
      get { return this.ResolvedDefinition is IFieldDefinition; }
    }

    #endregion

    #region IMetadataExpression Members

    void IMetadataExpression.Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    IEnumerable<ILocation> IMetadataExpression.Locations {
      get { return ((IExpression)this).Locations; }
    }

    ITypeReference IMetadataExpression.Type {
      get { return this.Type; }
    }

    #endregion
  }

  //TODO: nested method to which both anon del and lambda can project without needing to introduce explicit closure classes.

  public sealed class NotEquality : BinaryOperation, INotEquality {

    public NotEquality() {
    }

    public NotEquality(INotEquality notEquality)
      : base(notEquality) {
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

  }

  public sealed class OldValue : Expression, IOldValue {
    
    public OldValue() {
      this.expression = CodeDummy.Expression;
    }

    public OldValue(IOldValue oldValue)
      : base(oldValue) {
      this.expression = oldValue.Expression;
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    public IExpression Expression {
      get { return this.expression; }
      set { this.expression = value; }
    }
    IExpression expression;

  }

  public sealed class OutArgument : Expression, IOutArgument {

    public OutArgument() {
      this.expression = CodeDummy.TargetExpression;
    }

    public OutArgument(IOutArgument outArgument)
      : base(outArgument) {
      this.expression = outArgument.Expression;
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    public ITargetExpression Expression {
      get { return this.expression; }
      set { this.expression = value; }
    }
    ITargetExpression expression;

  }

  public sealed class OnesComplement : UnaryOperation, IOnesComplement {

    public OnesComplement() {
    }

    public OnesComplement(IOnesComplement onesComplement)
      : base(onesComplement) {
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

  }

  public sealed class PointerCall : Expression, IPointerCall {

    public PointerCall() {
      this.arguments = new List<IExpression>();
      this.pointer = CodeDummy.Expression;
    }

    public PointerCall(IPointerCall pointerCall)
      : base(pointerCall) {
      this.arguments = new List<IExpression>(pointerCall.Arguments);
      this.pointer = pointerCall.Pointer;
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    public List<IExpression> Arguments {
      get { return this.arguments; }
      set { this.arguments = value; }
    }
    internal List<IExpression> arguments;

    public IExpression Pointer {
      get { return this.pointer; }
      set { this.pointer = value; }
    }
    IExpression pointer;

    public bool IsTailCall {
      get { return this.isTailCall; }
      set { this.isTailCall = value; }
    }
    bool isTailCall;

    #region IPointerCall Members

    IEnumerable<IExpression> IPointerCall.Arguments {
      get { return this.arguments.AsReadOnly(); }
    }

    #endregion
  }

  public sealed class RefArgument : Expression, IRefArgument {

    public RefArgument()
      : base() {
      this.expression = CodeDummy.AddressableExpression;
    }

    public RefArgument(IRefArgument refArgument)
      : base(refArgument) {
      this.expression = refArgument.Expression;
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    public IAddressableExpression Expression {
      get { return this.expression; }
      set { this.expression = value; }
    }
    IAddressableExpression expression;

  }

  public sealed class ReturnValue : Expression, IReturnValue {

    public ReturnValue() {
    }

    public ReturnValue (IReturnValue returnValue)
      : base(returnValue) {
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

  }

  public sealed class RightShift : BinaryOperation, IRightShift {

    public RightShift() {
    }

    public RightShift(IRightShift rightShift)
      : base(rightShift) {
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

  }

  public sealed class RuntimeArgumentHandleExpression : Expression, IRuntimeArgumentHandleExpression {

    public RuntimeArgumentHandleExpression() {
    }

    public RuntimeArgumentHandleExpression(IRuntimeArgumentHandleExpression runtimeArgumentHandleExpression)
      : base(runtimeArgumentHandleExpression) {
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

  }

  public sealed class SizeOf : Expression, ISizeOf {

    public SizeOf() {
      this.typeToSize = Dummy.TypeReference;
    }

    public SizeOf(ISizeOf sizeOf)
      : base(sizeOf) {
      this.typeToSize = sizeOf.TypeToSize;
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    public ITypeReference TypeToSize {
      get { return this.typeToSize; }
      set { this.typeToSize = value; }
    }
    ITypeReference typeToSize;

  }

  public sealed class StackArrayCreate : Expression, IStackArrayCreate {

    public StackArrayCreate() {
      this.elementType = Dummy.TypeReference;
      this.size = CodeDummy.Expression;
    }

    public StackArrayCreate(IStackArrayCreate stackArrayCreate)
      : base(stackArrayCreate) {
      this.elementType = stackArrayCreate.ElementType;
      this.size = stackArrayCreate.Size;
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    public ITypeReference ElementType {
      get { return this.elementType; }
      set { this.elementType = value; }
    }
    ITypeReference elementType;

    public IExpression Size {
      get { return this.size; }
      set { this.size = value; }
    }
    IExpression size;

  }

  public sealed class Subtraction : BinaryOperation, ISubtraction {

    public Subtraction() {
    }

    public Subtraction(ISubtraction subtraction)
      : base(subtraction) {
      this.CheckOverflow = subtraction.CheckOverflow;
    }

    public bool CheckOverflow { get; set; }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }
  }

  public sealed class TargetExpression : Expression, ITargetExpression {

    public TargetExpression() {
      this.definition = null;
      this.instance = null;
    }

    public TargetExpression(ITargetExpression targetExpression)
      : base(targetExpression) {
      this.definition = targetExpression.Definition;
      this.instance = targetExpression.Instance;
    }

    public byte Alignment {
      get
        //^^ requires !this.IsUnaligned;
        //^^ ensures result == 1 || result == 2 || result == 4;
      {
        //^ assume this.alignment == 1 || this.alignment == 2 || this.alignment == 4;
        return this.alignment;
      }
      set
        //^ requires value == 1 || value == 2 || value == 4;
      {
        this.alignment = value;
      }
    }
    //^ [SpecPublic]
    byte alignment;
    //^ invariant alignment == 0 || alignment == 1 || alignment == 2 || alignment == 4;

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    public object/*?*/ Definition {
      get { return this.definition; }
      set
        //^ requires value == null || value is ILocalDefinition || value is IParameterDefinition || value is IFieldDefinition || value is IArrayIndexer || value is IAddressDereference;
      { 
        this.definition = value; 
      }
    }
    object/*?*/ definition;
    //^ invariant definition == null || definition is ILocalDefinition || definition is IParameterDefinition || definition is IFieldDefinition || definition is IArrayIndexer || definition is IAddressDereference;

    public IExpression/*?*/ Instance {
      get { return this.instance; }
      set { this.instance = value; }
    }
    IExpression/*?*/ instance;

    /// <summary>
    /// True if the definition is a field and the field is not aligned with the natural size of its type.
    /// For example if the field type is Int32 and the field is aligned on an Int16 boundary.
    /// </summary>
    public bool IsUnaligned {
      get { return this.alignment != 0; }
    }

    public bool IsVolatile {
      get { return this.isVolatile; }
      set { this.isVolatile = value; }
    }
    bool isVolatile;
  }

  public sealed class ThisReference : Expression, IThisReference {

    public ThisReference() {
    }

    public ThisReference(IThisReference thisReference)
      : base(thisReference) {
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

  }

  public sealed class TokenOf : Expression, ITokenOf {

    public TokenOf() {
      this.definition = Dummy.TypeReference;
    }

    public TokenOf(ITokenOf tokenOf)
      : base(tokenOf) {
      this.definition = tokenOf.Definition;
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    public object Definition {
      get { return this.definition; }
      set { this.definition = value; }
    }
    object definition;

  }

  public sealed class TypeOf : Expression, ITypeOf, IMetadataTypeOf {

    public TypeOf() {
      this.typeToGet = Dummy.TypeReference;
    }

    public TypeOf(ITypeOf typeOf)
      : base(typeOf) {
      this.typeToGet = typeOf.TypeToGet;
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    public ITypeReference TypeToGet {
      get { return this.typeToGet; }
      set { this.typeToGet = value; }
    }
    ITypeReference typeToGet;

    #region IMetadataExpression Members

    void IMetadataExpression.Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    IEnumerable<ILocation> IMetadataExpression.Locations {
      get { return ((IExpression)this).Locations; }
    }

    ITypeReference IMetadataExpression.Type {
      get { return this.Type; }
    }

    #endregion
  }

  public sealed class UnaryNegation : UnaryOperation, IUnaryNegation {

    public UnaryNegation() {
    }

    public UnaryNegation(IUnaryNegation unaryNegation)
      : base(unaryNegation) {
      this.CheckOverflow = unaryNegation.CheckOverflow;
    }

    public bool CheckOverflow { get; set; }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

  }

  public abstract class UnaryOperation : Expression, IUnaryOperation {

    internal UnaryOperation() {
      this.operand = CodeDummy.Expression;
    }

    internal UnaryOperation(IUnaryOperation unaryOperation)
      : base(unaryOperation) {
      this.operand = unaryOperation.Operand;
    }

    public IExpression Operand {
      get { return this.operand; }
      set { this.operand = value; }
    }
    IExpression operand;

  }

  public sealed class UnaryPlus : UnaryOperation, IUnaryPlus {

    public UnaryPlus() {
    }

    public UnaryPlus(IUnaryPlus unaryPlus)
      : base(unaryPlus) {
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

  }

  public sealed class VectorLength : Expression, IVectorLength {

    public VectorLength() {
      this.vector = CodeDummy.Expression;
    }

    public VectorLength(IVectorLength vectorLength)
      : base(vectorLength) {
      this.vector = vectorLength.Vector;
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    public IExpression Vector {
      get { return this.vector; }
      set { this.vector = value; }
    }
    IExpression vector;

  }
}
