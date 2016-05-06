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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Cci.Contracts;
using System.Globalization;
using Microsoft.Cci.Immutable;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci.Ast {

  /// <summary>
  /// An expression that adds or concatenates the value of the left operand to the value of the right operand. When overloaded, this expression corresponds to a call to op_Addition.
  /// </summary>
  public class Addition : BinaryOperation, IAddition {

    /// <summary>
    /// Allocates an expression that adds or concatenates the value of the left operand to the value of the right operand. When overloaded, this expression corresponds to a call to op_Addition.
    /// </summary>
    /// <param name="leftOperand">The left operand.</param>
    /// <param name="rightOperand">The right operand.</param>
    /// <param name="sourceLocation">The source location of the operation.</param>
    public Addition(Expression leftOperand, Expression rightOperand, ISourceLocation sourceLocation)
      : base(leftOperand, rightOperand, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected Addition(BlockStatement containingBlock, Addition template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.flags = template.flags;
    }

    /// <summary>
    /// The addition must be performed with a check for arithmetic overflow if the operands are integers.
    /// </summary>
    public virtual bool CheckOverflow {
      get {
        return (this.flags & 1) != 0;
      }
    }

    /// <summary>
    /// Returns a method call object that calls the given overloadMethod with this.LeftOperand and this.RightOperands as arguments.
    /// The operands are converted to the corresponding parameter types using implicit conversions.
    /// If overloadMethod is the Dummy.Method a DummyMethodCall is returned.
    /// </summary>
    /// <param name="overloadMethod">A user defined operator overload method or a "builtin" operator overload method, or a dummy method.
    /// The latter can be supplied when the expression is in error because one or both of the arguments cannot be converted the correct parameter type for a valid overload.</param>
    protected override MethodCall CreateOverloadMethodCall(IMethodDefinition overloadMethod) {
      BuiltinMethods dummyMethods = this.Compilation.BuiltinMethods;
      if (overloadMethod == dummyMethods.ObjectOpString ||
        overloadMethod == dummyMethods.StringOpObject || overloadMethod == dummyMethods.StringOpString) {
        ITypeDefinition stringOrObject = overloadMethod == dummyMethods.StringOpString ? this.PlatformType.SystemString.ResolvedType : this.PlatformType.SystemObject.ResolvedType;
        foreach (ITypeDefinitionMember member in this.PlatformType.SystemString.ResolvedType.GetMembersNamed(this.NameTable.Concat, false)) {
          IMethodDefinition/*?*/ concat = member as IMethodDefinition;
          if (concat != null && IteratorHelper.EnumerableHasLength(concat.Parameters, 2)) {
            IEnumerator<IParameterDefinition> concatEnum = concat.Parameters.GetEnumerator();
            if (concatEnum.MoveNext() && TypeHelper.TypesAreEquivalent(concatEnum.Current.Type.ResolvedType, stringOrObject)) {
              overloadMethod = concat;
              break;
            }
          }
        }
      }
      //Note that del + del is not replaced with a call to Delegate.Combine at this stage, since the latter call has System.Delegate as its return type.
      //The actual transformation happens in ProjectAsIExpression, which also adds an explicit cast to preserve the expression type.
      return base.CreateOverloadMethodCall(overloadMethod);
    }

    /// <summary>
    /// Calls the visitor.Visit(IAddition) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(Addition) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Storage for boolean properties. 1=Use checked arithmetic.
    /// </summary>
    protected int flags;

    /// <summary>
    /// Returns the string used to identify the operator in error messages
    /// </summary>
    /// <value></value>
    protected override string OperationSymbolForErrorMessage {
      get { return "+"; }
    }


    /// <summary>
    /// Returns the name a method must have to overload this operator.
    /// </summary>
    protected override IName GetOperatorName() {
      return this.NameTable.OpAddition;
    }

    /// <summary>
    /// Computes the compile time value of the expression. Can be null.
    /// </summary>
    protected override object/*?*/ GetValue() {
      object/*?*/ left = this.ConvertedLeftOperand.Value;
      object/*?*/ right = this.ConvertedRightOperand.Value;
      if (left == null) {
        if (this.ConvertedLeftOperand is CompileTimeConstant) return right as string;
        return null;
      }
      if (right == null) {
        if (this.ConvertedRightOperand is CompileTimeConstant) return left as string;
        return null;
      }
      switch (System.Convert.GetTypeCode(left)) {
        case TypeCode.Int32:
          //^ assume left is int && right is int;
          return (int)left + (int)right; //TODO: overflow check
        case TypeCode.UInt32:
          //^ assume left is uint && right is uint;
          return (uint)left + (uint)right; //TODO: overflow check
        case TypeCode.Int64:
          //^ assume left is long && right is long;
          return (long)left + (long)right; //TODO: overflow check
        case TypeCode.UInt64:
          //^ assume left is ulong && right is ulong;
          return (ulong)left + (ulong)right; //TODO: overflow check
        case TypeCode.Single:
          //^ assume left is float && right is float;
          return (float)left + (float)right;
        case TypeCode.Double:
          //^ assume left is double && right is double;
          return (double)left + (double)right;
        case TypeCode.Decimal:
          //^ assume left is decimal && right is decimal;
          return (decimal)left + (decimal)right;
        case TypeCode.String:
          //^ assume left is string && right is string;
          return (string)left + (string)right;
      }
      return null;
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new Addition(containingBlock, this);
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      bool result = base.CheckForErrorsAndReturnTrueIfAnyAreFound();
      if (!result) {
        IMethodCall overloadMethodCall = this.OverloadMethodCall;
        if (overloadMethodCall != null) {
          IMethodDefinition/*?*/ overloadMethod = overloadMethodCall.MethodToCall.ResolvedMethod;
          if (overloadMethod is BuiltinMethodDefinition) {
            if (this.Helper.IsPointerType(this.LeftOperand.Type)) {
              if (this.Helper.GetPointerTargetType(this.LeftOperand.Type).ResolvedType.TypeCode == PrimitiveTypeCode.Void) {
                this.Helper.ReportError(new AstErrorMessage(this, Error.UndefinedOperationOnVoidPointers));
                result = true;
              }
            }
            if (this.Helper.IsPointerType(this.RightOperand.Type)) {
              if (this.Helper.GetPointerTargetType(this.RightOperand.Type).ResolvedType.TypeCode == PrimitiveTypeCode.Void) {
                this.Helper.ReportError(new AstErrorMessage(this, Error.UndefinedOperationOnVoidPointers));
                result = true;
              }
            }
          }
        }
      }
      return result;
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      IMethodCall/*?*/ overloadMethodCall = this.OverloadMethodCall;
      if (overloadMethodCall != null) {
        IMethodDefinition/*?*/ overloadMethod = overloadMethodCall.MethodToCall.ResolvedMethod;
        if (overloadMethod != null) {
          List<Expression> args = new List<Expression>(2);
          args.Add(this.LeftOperand);
          args.Add(this.RightOperand);
          args = this.Helper.ConvertArguments(this, args, overloadMethod.Parameters);
          if (overloadMethod.Name.UniqueKey == this.NameTable.DelegateOpDelegate.UniqueKey) {
            foreach (ITypeDefinitionMember member in this.PlatformType.SystemDelegate.ResolvedType.GetMembersNamed(this.NameTable.Combine, false)) {
              IMethodDefinition/*?*/ combine = member as IMethodDefinition;
              if (combine != null && IteratorHelper.EnumerableHasLength(combine.Parameters, 2)) {
                overloadMethod = combine;
                break;
              }
            }
            ResolvedMethodCall combineCall = new ResolvedMethodCall(overloadMethod, args, this.SourceLocation);
            combineCall.SetContainingExpression(this);
            return this.Helper.ExplicitConversion(combineCall, this.Type).ProjectAsIExpression();
          } else if (overloadMethod is BuiltinMethodDefinition) {
            if (this.Helper.IsPointerType(this.LeftOperand.Type))
              return this.ProjectAsPointerPlusIndex(args, this.Helper.GetPointerTargetType(this.LeftOperand.Type).ResolvedType);
            else if (this.Helper.IsPointerType(this.RightOperand.Type))
              return this.ProjectAsIndexPlusPointer(args, this.Helper.GetPointerTargetType(this.RightOperand.Type).ResolvedType);
          }
        }
      }
      return base.ProjectAsNonConstantIExpression();
    }

    /// <summary>
    /// Returns an expression corresponding to ptr + index*sizeof(T) where ptr is the first element of args, index is the second
    /// and T is targetType, the type of element that ptr points to.
    /// </summary>
    protected virtual IExpression ProjectAsPointerPlusIndex(List<Expression> args, ITypeDefinition targetType) {
      IEnumerator<Expression> argumentEnumerator = args.GetEnumerator();
      if (!argumentEnumerator.MoveNext()) return new DummyExpression(this.SourceLocation);
      Expression ptr = argumentEnumerator.Current;
      if (!argumentEnumerator.MoveNext()) return new DummyExpression(this.SourceLocation);
      Expression index = argumentEnumerator.Current;
      Expression sizeOf = new SizeOf(TypeExpression.For(targetType), index.SourceLocation);
      if (TypeHelper.IsUnsignedPrimitiveInteger(index.Type)) sizeOf = new Cast(sizeOf, TypeExpression.For(index.Type), index.SourceLocation);
      ScaledIndex scaledIndex = new ScaledIndex(index, sizeOf, sizeOf.SourceLocation);
      scaledIndex.SetContainingExpression(this);
      PointerAddition pointerPlusIndex = new PointerAddition(this, ptr, scaledIndex);
      return pointerPlusIndex;
    }

    /// <summary>
    /// Returns an expression corresponding to index*sizeof(T) + ptr where index is the first element of args, ptr is the second
    /// and T is targetType, the type of element that ptr points to.
    /// </summary>
    protected virtual IExpression ProjectAsIndexPlusPointer(List<Expression> args, ITypeDefinition targetType) {
      IEnumerator<Expression> argumentEnumerator = args.GetEnumerator();
      if (!argumentEnumerator.MoveNext()) return new DummyExpression(this.SourceLocation);
      Expression index = argumentEnumerator.Current;
      if (!argumentEnumerator.MoveNext()) return new DummyExpression(this.SourceLocation);
      Expression ptr = argumentEnumerator.Current;
      Expression sizeOf = new SizeOf(TypeExpression.For(targetType), index.SourceLocation);
      if (TypeHelper.IsUnsignedPrimitiveInteger(index.Type)) sizeOf = new Cast(sizeOf, TypeExpression.For(index.Type), index.SourceLocation);
      ScaledIndex scaledIndex = new ScaledIndex(index, sizeOf, sizeOf.SourceLocation);
      scaledIndex.SetContainingExpression(this);
      PointerAddition indexPlusPointer = new PointerAddition(this, scaledIndex, ptr);
      return indexPlusPointer;
    }

    class PointerAddition : CheckableSourceItem, IAddition {

      internal PointerAddition(Addition addition, Expression leftOperand, Expression rightOperand)
        : base(addition.SourceLocation) {
        this.addition = addition;
        this.leftOperand = leftOperand;
        this.rightOperand = rightOperand;
      }

      readonly Addition addition;

      public bool CheckOverflow {
        get { return this.addition.CheckOverflow; }
      }

      public override void Dispatch(ICodeVisitor visitor) {
        visitor.Visit(this);
      }

      protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
        return this.addition.HasErrors;
      }

      public IExpression LeftOperand {
        get { return this.leftOperand.ProjectAsIExpression(); }
      }
      readonly Expression leftOperand;

      public IExpression RightOperand {
        get { return this.rightOperand.ProjectAsIExpression(); }
      }
      readonly Expression rightOperand;

      /// <summary>
      /// If true, the left operand must be a target expression and the result of the binary operation is the
      /// value of the target expression before it is assigned the value of the operation performed on
      /// (right hand) values of the left and right operands.
      /// </summary>
      public bool ResultIsUnmodifiedLeftOperand {
        get { return false; }
      }

      /// <summary>
      /// If true the operands must be integers and are treated as being unsigned for the purpose of the addition. This only makes a difference if CheckOverflow is true as well.
      /// </summary>
      public bool TreatOperandsAsUnsignedIntegers {
        get { return this.addition.TreatOperandsAsUnsignedIntegers; }
      }

      public ITypeDefinition Type {
        get { return this.addition.Type; }
      }

      #region IExpression Members

      ITypeReference IExpression.Type {
        get { return this.Type; }
      }

      #endregion
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingExpression(Expression containingExpression) {
      base.SetContainingExpression(containingExpression);
      if (containingExpression.ContainingBlock.UseCheckedArithmetic)
        this.flags |= 1;
      else
        this.flags &= ~1;
      //Note that checked/unchecked expressions intercept this call and provide a dummy block that has the flag set appropriately.
    }

    /// <summary>
    /// A list of dummy methods that correspond to operations that are built into IL. The dummy methods are used, via overload resolution,
    /// to determine how the operands are to be converted before the operation is carried out.
    /// </summary>
    protected override IEnumerable<IMethodDefinition> StandardOperators {
      get {
        BuiltinMethods dummyMethods = this.Compilation.BuiltinMethods;
        yield return dummyMethods.Int32opInt32;
        yield return dummyMethods.UInt32opUInt32;
        yield return dummyMethods.Int64opInt64;
        yield return dummyMethods.UInt64opUInt64;
        yield return dummyMethods.Float32opFloat32;
        yield return dummyMethods.Float64opFloat64;
        yield return dummyMethods.DecimalOpDecimal;
        ITypeDefinition leftOperandType = this.LeftOperand.Type;
        ITypeDefinition rightOperandType = this.RightOperand.Type;
        if (TypeHelper.TypesAreEquivalent(leftOperandType, this.PlatformType.SystemString)) {
          if (TypeHelper.TypesAreEquivalent(rightOperandType, this.PlatformType.SystemString))
            yield return dummyMethods.StringOpString;
          else
            yield return dummyMethods.StringOpObject;
        } else if (TypeHelper.TypesAreEquivalent(rightOperandType, this.PlatformType.SystemString)) {
          yield return dummyMethods.ObjectOpString;
        } else {
          if (leftOperandType.IsEnum)
            yield return dummyMethods.GetDummyEnumOpNum(leftOperandType);
          else if (rightOperandType.IsEnum)
            yield return dummyMethods.GetDummyNumOpEnum(rightOperandType);
          else if (leftOperandType.IsDelegate)
            yield return dummyMethods.GetDummyDelegateOpDelegate(leftOperandType);
          else if (rightOperandType.IsDelegate)
            yield return dummyMethods.GetDummyDelegateOpDelegate(rightOperandType);
          else if (this.Helper.IsPointerType(leftOperandType)) {
            yield return dummyMethods.GetDummyOp(leftOperandType, leftOperandType, this.PlatformType.SystemInt32.ResolvedType);
            yield return dummyMethods.GetDummyOp(leftOperandType, leftOperandType, this.PlatformType.SystemUInt32.ResolvedType);
            yield return dummyMethods.GetDummyOp(leftOperandType, leftOperandType, this.PlatformType.SystemInt64.ResolvedType);
            yield return dummyMethods.GetDummyOp(leftOperandType, leftOperandType, this.PlatformType.SystemUInt64.ResolvedType);
          } else if (this.Helper.IsPointerType(rightOperandType)) {
            yield return dummyMethods.GetDummyOp(rightOperandType, this.PlatformType.SystemInt32.ResolvedType, rightOperandType.ResolvedType);
            yield return dummyMethods.GetDummyOp(rightOperandType, this.PlatformType.SystemUInt32.ResolvedType, rightOperandType.ResolvedType);
            yield return dummyMethods.GetDummyOp(rightOperandType, this.PlatformType.SystemInt64.ResolvedType, rightOperandType.ResolvedType);
            yield return dummyMethods.GetDummyOp(rightOperandType, this.PlatformType.SystemUInt64.ResolvedType, rightOperandType.ResolvedType);
          }
        }
      }
    }

    /// <summary>
    /// If true the operands must be integers and are treated as being unsigned for the purpose of the addition. This only makes a difference if CheckOverflow is true as well.
    /// </summary>
    public virtual bool TreatOperandsAsUnsignedIntegers {
      get { return TypeHelper.IsUnsignedPrimitiveInteger(this.ConvertedLeftOperand.Type) && TypeHelper.IsUnsignedPrimitiveInteger(this.ConvertedRightOperand.Type); }
    }

  }

  /// <summary>
  /// An expression that adds or concatenates the value of the left operand with the value of the right operand.
  /// The result of the expression is assigned to the left operand, which must be a target expression.
  /// Both operands must be primitives types.
  /// </summary>
  public class AdditionAssignment : BinaryOperationAssignment {

    /// <summary>
    /// Allocates an expression that adds or concatenates the value of the left operand with the value of the right operand.
    /// The result of the expression is assigned to the left operand, which must be a target expression.
    /// Both operands must be primitives types.
    /// </summary>
    /// <param name="leftOperand">The left operand and target of the assignment.</param>
    /// <param name="rightOperand">The right operand.</param>
    /// <param name="sourceLocation">The source location of the operation.</param>
    public AdditionAssignment(TargetExpression leftOperand, Expression rightOperand, ISourceLocation sourceLocation)
      : base(leftOperand, rightOperand, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected AdditionAssignment(BlockStatement containingBlock, AdditionAssignment template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Calls the visitor.Visit(AdditionAssignment) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (this.ContainingBlock == containingBlock) return this;
      AdditionAssignment result = new AdditionAssignment(containingBlock, this);
      //^ assume result.ContainingBlock == containingBlock; //This should be a post condition of the constructor, but such post conditions are not currently permitted by the methodology.
      return result;
    }

    /// <summary>
    /// Creates an addition expression with the given left operand and this.RightOperand.
    /// The method does not use this.LeftOperand.Expression, since it may be necessary to factor out any subexpressions so that
    /// they are evaluated only once. The given left operand expression is expected to be the expression that remains after factoring.
    /// </summary>
    /// <param name="leftOperand">An expression to combine with this.RightOperand into a binary expression.</param>
    protected override Expression CreateBinaryExpression(Expression leftOperand) {
      Expression result = new Addition(leftOperand, this.RightOperand, this.SourceLocation);
      result.SetContainingExpression(this);
      return result;
    }
  }

  /// <summary>
  /// An expression that denotes a value that has an address in memory, such as a local variable, parameter, field, array element, pointer target, or method.
  /// </summary>
  public class AddressableExpression : LeftHandExpression, IAddressableExpression {

    /// <summary>
    /// Allocates an expression that denotes a value that has an address in memory, such as a local variable, parameter, field, array element, pointer target, or method.
    /// </summary>
    /// <param name="expression">An expression that is expected to denote a value that has an address in memory.</param>
    public AddressableExpression(Expression expression)
      : base(expression) {
    }

    /// <summary>
    /// Allocates an expression that denotes a value that has an address in memory, such as a local variable, parameter, field, array element, pointer target, or method.
    /// </summary>
    /// <param name="expression">The expression that is used as the target of an explicit or implicit assignment.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated target expression.</param>
    public AddressableExpression(Expression expression, ISourceLocation sourceLocation)
      : base(expression, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected AddressableExpression(BlockStatement containingBlock, AddressableExpression template)
      : base(containingBlock, template)
      //^ requires template.Expression.ContainingBlock != containingBlock;
    {
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return this.CheckForErrorsAndReturnTrueIfAnyAreFound(false, false);
    }

    /// <summary>
    /// The local variable, parameter, field, array element, pointer target or method that this expression denotes.
    /// </summary>
    public new object Definition {
      get
        //^^ ensures result is ILocalDefinition || result is IParameterDefinition || result is IFieldReference || result is IArrayIndexer 
        //^^   || result is IAddressDereference || result is IMethodReference || result is IThisReference;
      {
        return base.Definition;
      }
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      return this;
    }

    /// <summary>
    /// Resolves this instance.
    /// </summary>
    /// <returns></returns>
    public override object Resolve() {
      object/*?*/ definition = base.Resolve();
      if (definition is IEventDefinition || definition is IPropertyDefinition)
        return null;
      return definition;
    }

    /// <summary>
    /// Calls the visitor.Visit(IAddressableExpression) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(AddressableExpression) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.Expression.ContainingBlock) return this;
      return new AddressableExpression(containingBlock, this);
    }

    /// <summary>
    /// Reports the error.
    /// </summary>
    protected override void ReportError() {
      this.Helper.ReportError(new AstErrorMessage(this, Error.CannotTakeAddress));
    }

    #region IAddressableExpression Members

    IExpression/*?*/ IAddressableExpression.Instance {
      get {
        Expression/*?*/ instance = this.Instance;
        if (instance == null) return null;
        return instance.ProjectAsIExpression(); //TODO: cache result?
      }
    }

    #endregion

    #region IExpression Members

    ITypeReference IExpression.Type {
      get { return this.Type; }
    }

    #endregion

  }

  /// <summary>
  /// An expression that refers to an attribute type by specificing the type name, possibly missing the "Attribute" suffix.
  /// </summary>
  public class AttributeTypeExpression : NamedTypeExpression {

    /// <summary>
    /// Allocates an expression that refers to an attribute type by specificing the type name, possibly missing the "Attribute" suffix.
    /// </summary>
    /// <param name="expression">An expression that names a type. 
    /// Must be an instance of SimpleName, QualifiedName or AliasQualifiedName.</param>
    public AttributeTypeExpression(Expression expression)
      : base(expression)
      //^ requires expression is SimpleName || expression is QualifiedName || expression is AliasQualifiedName;
    {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected AttributeTypeExpression(BlockStatement containingBlock, AttributeTypeExpression template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Calls visitor.Visit(AttributeTypeExpression).
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Resolves the expression as a non generic type. If expression cannot be resolved, a dummy type is returned. 
    /// If the expression is ambiguous the first matching type is returned.
    /// If the expression does not resolve to exactly one type, an error is added to the error collection of the compilation context.
    /// </summary>
    protected override ITypeDefinition Resolve() {
      ITypeDefinition result = Dummy.Type;
      SimpleName/*?*/ simpleName = this.Expression as SimpleName;
      if (simpleName != null) return this.ResolveSimpleName(simpleName, true);
      QualifiedName/*?*/ qualifiedName = this.Expression as QualifiedName;
      if (qualifiedName != null) return this.ResolveQualifiedName(qualifiedName.ResolveQualifierAsNamespaceOrType(true), qualifiedName.SimpleName);
      AliasQualifiedName/*?*/ aliasQualName = this.Expression as AliasQualifiedName;
      if (aliasQualName != null) return this.ResolveQualifiedName(aliasQualName.ResolveAlias(), aliasQualName.SimpleName);
      //^ assert false;
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="simpleName"></param>
    /// <param name="mayAppendSuffix"></param>
    /// <returns></returns>
    protected virtual ITypeDefinition ResolveSimpleName(SimpleName simpleName, bool mayAppendSuffix) {
      object/*?*/ definition = simpleName.ResolveAsNamespaceOrType();
      ITypeDefinition/*?*/ result = definition as ITypeDefinition;
      if (result != null && TypeHelper.IsAttributeType(result)) return result;
      ITypeGroup/*?*/ typeGroup = definition as ITypeGroup;
      if (typeGroup != null) {
        foreach (ITypeDefinition type in typeGroup.GetTypes(0)) {
          if (TypeHelper.IsAttributeType(type)) return type;
        }
      }
      if (mayAppendSuffix) {
        SimpleName suffixedName = new SimpleName(this.Helper.NameTable.GetNameFor(simpleName.Name.Value+"Attribute"), simpleName.SourceLocation, false);
        suffixedName.SetContainingExpression(simpleName);
        return this.ResolveSimpleName(suffixedName, false);
      }
      this.Helper.ReportError(new AstErrorMessage(this, Error.SingleTypeNameNotFound, simpleName.Name.Value));
      return Dummy.Type;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="qualifier"></param>
    /// <param name="simpleName"></param>
    /// <returns></returns>
    protected virtual ITypeDefinition ResolveQualifiedName(object/*?*/ qualifier, SimpleName simpleName) {
      if (qualifier == null) return Dummy.Type;
      INamespaceDefinition/*?*/ nspace = qualifier as INamespaceDefinition;
      if (nspace != null) return ResolveQualifiedName(nspace, simpleName);
      ITypeDefinition/*?*/ qualifyingType = qualifier as ITypeDefinition;
      if (qualifyingType != null) return this.ResolveQualifiedName(qualifyingType, simpleName);
      ITypeGroup/*?*/ qualifyingTypeGroup = qualifier as ITypeGroup;
      if (qualifyingTypeGroup != null) {
        foreach (ITypeDefinition type in qualifyingTypeGroup.GetTypes(0))
          return this.ResolveQualifiedName(type, simpleName);
      }
      Error e = Error.ToBeDefined;
      this.Helper.ReportError(new AstErrorMessage(this.Expression, e));
      return Dummy.Type;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="qualifyingType"></param>
    /// <param name="simpleName"></param>
    /// <returns></returns>
    protected virtual ITypeDefinition ResolveQualifiedName(ITypeDefinition qualifyingType, SimpleName simpleName) {
      foreach (ITypeDefinitionMember member in qualifyingType.GetMembersNamed(simpleName.Name, simpleName.IgnoreCase)) {
        ITypeDefinition/*?*/ type = member as ITypeDefinition;
        if (type != null && TypeHelper.IsAttributeType(type)) return type;
      }
      IName suffixedName = this.Helper.NameTable.GetNameFor(simpleName.Name.Value+"Attribute");
      foreach (ITypeDefinitionMember member in qualifyingType.GetMembersNamed(suffixedName, simpleName.IgnoreCase)) {
        ITypeDefinition/*?*/ type = member as ITypeDefinition;
        if (type != null && TypeHelper.IsAttributeType(type)) return type;
      }
      Error e = Error.ToBeDefined;
      this.Helper.ReportError(new AstErrorMessage(this.Expression, e));
      return Dummy.Type;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="nspace"></param>
    /// <param name="simpleName"></param>
    /// <returns></returns>
    protected virtual ITypeDefinition ResolveQualifiedName(INamespaceDefinition nspace, SimpleName simpleName) {
      foreach (INamespaceMember member in nspace.GetMembersNamed(simpleName.Name, simpleName.IgnoreCase)) {
        ITypeDefinition/*?*/ type = member as ITypeDefinition;
        if (type != null && TypeHelper.IsAttributeType(type)) return type;
      }
      IName suffixedName = this.Helper.NameTable.GetNameFor(simpleName.Name.Value+"Attribute");
      foreach (INamespaceMember member in nspace.GetMembersNamed(suffixedName, simpleName.IgnoreCase)) {
        ITypeDefinition/*?*/ type = member as ITypeDefinition;
        if (type != null && TypeHelper.IsAttributeType(type)) return type;
      }
      this.Helper.ReportError(new AstErrorMessage(simpleName, Error.TypeNameNotFound, nspace.Name.Value, simpleName.Name.Value));
      return Dummy.Type;
    }

  }

  /// <summary>
  /// An expression that can be the target of an assignment statement or that can be passed an argument to an out parameter.
  /// </summary>
  public class TargetExpression : LeftHandExpression, ITargetExpression {

    /// <summary>
    /// Allocates an expression that can be the target of an assignment statement or that can be passed an argument to an out parameter.
    /// </summary>
    /// <param name="expression">An expression that is expected to denote a value that has an address in memory.</param>
    public TargetExpression(Expression expression)
      : base(expression) {
    }

    /// <summary>
    /// Allocates an expression that can be the target of an assignment statement or that can be passed an argument to an out parameter.
    /// </summary>
    /// <param name="expression">The expression that is used as the target of an explicit or implicit assignment.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated target expression.</param>
    public TargetExpression(Expression expression, ISourceLocation sourceLocation)
      : base(expression, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected TargetExpression(BlockStatement containingBlock, TargetExpression template)
      : base(containingBlock, template)
      //^ requires template.Expression.ContainingBlock != containingBlock;
    {
    }

    /// <summary>
    /// If Definition is a field and the field is not aligned with natural size of its type, this property specifies the actual alignment.
    /// For example, if the field is byte aligned, then the result of this property is 1. Likewise, 2 for word (16-bit) alignment and 4 for
    /// double word (32-bit alignment). 
    /// </summary>
    public byte Alignment {
      get
        //^^ requires IsUnaligned;
        //^^ ensures result == 1 || result == 2 || result == 4;
      {
        //^ assume false; //TODO: need some work here
        return this.alignment;
      }
    }
    byte alignment = 0;

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return this.CheckForErrorsAndReturnTrueIfAnyAreFound(true, true);
    }

    /// <summary>
    /// The local variable, parameter, field, property, array element or pointer target that this expression denotes.
    /// </summary>
    public new object Definition {
      get
        //^ ensures result is ILocalDefinition || result is IParameterDefinition || result is IFieldDefinition || result is IArrayIndexer 
        //^   || result is IAddressDereference || result is IPropertyDefinition;
        //^ ensures result is IPropertyDefinition ==> ((IPropertyDefinition)result).Setter != null;
      {
        return base.Definition;
      }
    }

    /// <summary>
    /// If true, the resolved definition is a property whose getter is virtual.
    /// </summary>
    public bool GetterIsVirtual {
      get {
        var prop = this.Definition as IPropertyDefinition;
        if (prop != null && prop.Getter != null) return prop.Getter.ResolvedMethod.IsVirtual;
        return false;
      }
    }

    /// <summary>
    /// If true, the resolved definition is a property whose setter is virtual.
    /// </summary>
    public bool SetterIsVirtual {
      get {
        var prop = this.Definition as IPropertyDefinition;
        if (prop != null && prop.Setter != null) return prop.Setter.ResolvedMethod.IsVirtual;
        return false;
      }
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      return this;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public override object Resolve() {
      object/*?*/ definition = base.Resolve();
      if (definition is IThisReference || definition is IMethodDefinition || definition is IEventDefinition)
        return null;
      //TODO: need support for event += handler and event -= handler.
      return definition;
    }

    /// <summary>
    /// Calls the visitor.Visit(ITargetExpression) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(TargetExpression) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.Expression.ContainingBlock) return this;
      return new TargetExpression(containingBlock, this);
    }

    /// <summary>
    /// True if the definition is a field and the field is not aligned with the natural size of its type.
    /// For example if the field type is Int32 and the field is aligned on an Int16 boundary.
    /// </summary>
    public bool IsUnaligned {
      get { return this.alignment != 0; }
    }

    /// <summary>
    /// The bound Definition is a volatile field and its contents may not be cached.
    /// </summary>
    public bool IsVolatile {
      get {
        IFieldDefinition/*?*/ field = this.Definition as IFieldDefinition;
        return field != null && MemberHelper.IsVolatile(field);
      }
    }

    #region ITargetExpression Members

    IExpression/*?*/ ITargetExpression.Instance {
      get {
        Expression/*?*/ instance = this.Instance;
        if (instance == null) return null;
        return instance.ProjectAsIExpression(); //TODO: cache result?
      }
    }

    #endregion

    #region IExpression Members

    ITypeReference IExpression.Type {
      get { return this.Type; }
    }

    #endregion
  }

  /// <summary>
  /// An expression that can have its address taken, or that can appear on the left hand side of an assignment statement.
  /// </summary>
  public abstract class LeftHandExpression : Expression {

    /// <summary>
    /// Allocates an expression that can have its address taken, or that can appear on the left hand side of an assignment statement.
    /// </summary>
    /// <param name="expression">An expression that is expected to denote a value that has an address in memory, or that can be assigned to.</param>
    protected LeftHandExpression(Expression expression)
      : base(expression.SourceLocation) {
      while (true) {
        Parenthesis/*?*/ parenExpr = expression as Parenthesis;
        if (parenExpr != null) { expression = parenExpr.ParenthesizedExpression; continue; }
        break;
      }
      this.expression = expression;
    }

    /// <summary>
    /// Allocates an expression that can have its address taken, or that can appear on the left hand side of an assignment statement.
    /// </summary>
    /// <param name="expression">An expression that is expected to denote a value that has an address in memory, or that can be assigned to.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated target expression.</param>
    protected LeftHandExpression(Expression expression, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.expression = expression;
    }

    /// <summary>
    /// Allocates an expression that can have its address taken, or that can appear on the left hand side of an assignment statement.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected LeftHandExpression(BlockStatement containingBlock, LeftHandExpression template)
      : base(containingBlock, template.SourceLocation)
      //^ requires template.ContainingBlock != containingBlock;
    {
      this.expression = template.Expression.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// </summary>
    protected bool CheckForErrorsAndReturnTrueIfAnyAreFound(bool allowProperty, bool allowAssignment) {
      object/*?*/ resolvedTarget = this.Resolve();
      IPropertyDefinition/*?*/ property = resolvedTarget as IPropertyDefinition;
      if (property != null) {
        if (!allowProperty) {
          //TODO: error message
          return true;
        }
        IMethodReference/*?*/ setter = property.Setter;
        if (setter != null) {
          Expression/*?*/ instance = this.Instance;
          if (setter.ResolvedMethod.IsStatic) {
            if (instance != null) {
              string setterSig = this.Helper.GetMethodSignature(setter, NameFormattingOptions.None);
              this.Helper.ReportError(new AstErrorMessage(instance, Error.ObjectProhibited, setter.Locations, setterSig));
              return true;
            }
          } else {
            if (instance == null) {
              instance = this.Expression;
              Parenthesis/*?*/ parExpr = instance as Parenthesis;
              while (parExpr != null) { instance = parExpr.ParenthesizedExpression; parExpr = instance as Parenthesis; }
              QualifiedName/*?*/ qualName = instance as QualifiedName;
              if (qualName != null)
                instance = qualName.Qualifier;
              else {
                Indexer/*?*/ indexer = instance as Indexer;
                if (indexer != null)
                  instance = indexer.IndexedObject;
              }
              string setterSig = this.Helper.GetMethodSignature(setter, NameFormattingOptions.None);
              //^ assume instance != null;
              this.Helper.ReportError(new AstErrorMessage(instance, Error.ObjectRequired, setter.Locations, setterSig));
              return true;
            }
          }
        } else {
          //TODO: complain about assignment to readonly property
        }
      } else {
        IFieldDefinition/*?*/ field = resolvedTarget as IFieldDefinition;
        if (field != null) {
          Expression/*?*/ instance = this.Instance;
          if (field.IsStatic) {
            if (instance != null) {
              this.Helper.ReportError(new AstErrorMessage(instance, Error.ObjectProhibited, field.Locations, field.Name.Value.ToString()));
              return true;
            }
          } else {
            if (instance == null) {
              this.Helper.ReportError(new AstErrorMessage(this, Error.ObjectRequired, field.Locations, field.Name.Value.ToString()));
              return true;
            }
          }
        } else {
          AddressDereference/*?*/ addrDeref = resolvedTarget as AddressDereference;
          if (addrDeref != null) {
            if (addrDeref.HasErrors)
              return true;
            if (allowAssignment && DerefConstFinder.Check(addrDeref)) {
              this.Helper.ReportError(new AstErrorMessage(this, Error.AssignmentLeftHandValueExpected, addrDeref.Locations));
              return true;
            }
          } else {
            LocalDefinition local = resolvedTarget as LocalDefinition;
            if (local != null) {
              if (allowAssignment && local.IsConstant) {
                this.Helper.ReportError(new AstErrorMessage(this, Error.AssignmentLeftHandValueExpected));
                return true;
              }
            }
          }
          if (resolvedTarget == null) {
            if (!this.Expression.HasErrors)
              this.ReportError();
            return true;
          }
        }
      }
      //TODO: if assignments are not allowed, complain if the definition is a this reference, method or a dereference of a pointer to constant memory
      this.definition = resolvedTarget;
      return this.Expression.HasErrors;
    }

    private class DerefConstFinder : CodeTraverser {

      public static bool Check(IExpression expr) {
        var visitor = new DerefConstFinder();
        visitor.Traverse(expr);
        return visitor.result;

      }

      private bool result = false;

      public override void TraverseChildren(IAddressDereference addressDereference) {
        this.TraverseChildren(addressDereference.Address);
      }

      public override void TraverseChildren(IAddition addition) {
        this.TraverseChildren(addition.LeftOperand);
      }

      public override void TraverseChildren(IConversion conversion) {
        this.TraverseChildren(conversion.ValueToConvert);
      }

      public override void TraverseChildren(IAddressOf addressOf) {
        this.TraverseChildren(addressOf.Expression);
      }

      public override void TraverseChildren(IAddressableExpression addressableExpression) {
        IFieldDefinition fieldDef = addressableExpression.Definition as IFieldDefinition;
        if (fieldDef != null && fieldDef.IsReadOnly)
          this.result = true;
      }
    }

    /// <summary>
    /// 
    /// </summary>
    protected virtual void ReportError() {
      this.Helper.ReportError(new AstErrorMessage(this, Error.AssignmentLeftHandValueExpected));
    }

    /// <summary>
    /// The local variable, parameter, field, array element, pointer target or method that this expression denotes.
    /// </summary>
    public object Definition {
      get
        //^ ensures result is ILocalDefinition || result is IParameterDefinition || result is IEventDefinition || 
        //^   result is IFieldDefinition || result is IArrayIndexer || result is IAddressDereference || result is IMethodDefinition || 
        //^   result is IThisReference || result is IPropertyDefinition; 
        //^ ensures result is IPropertyDefinition ==> ((IPropertyDefinition)result).Setter != null;
      {
        object/*?*/ definition = this.definition;
        if (definition == null) {
          definition = this.Resolve();
          if (definition == null) definition = Dummy.Field;
          this.definition = definition;
        }
        return definition;
      }
    }
    object definition;
    //^ invariant definition is ILocalDefinition || definition is IParameterDefinition || definition is IEventDefinition ||
    //^   definition is IFieldDefinition || definition is IArrayIndexer || definition is IAddressDereference || definition is IMethodDefinition ||
    //^   definition is IThisReference || definition is IPropertyDefinition;
    //^ invariant definition is IPropertyDefinition ==> ((IPropertyDefinition)definition).Setter != null;

    /// <summary>
    /// An expression that is expected to denote a value that has an address in memory.
    /// </summary>
    public Expression Expression {
      get { return this.expression; }
    }
    readonly Expression expression;

    /// <summary>
    /// Checks if the expression has a side effect and reports an error unless told otherwise.
    /// </summary>
    /// <param name="reportError">If true, report an error if the expression has a side effect.</param>
    public override bool HasSideEffect(bool reportError) {
      return this.Expression.HasSideEffect(reportError);
    }

    /// <summary>
    /// Create and expression that represents the address of the instance
    /// </summary>
    protected virtual AddressOf GetAddressOfForInstance(Expression instance) {
      return this.Helper.GetAddressOf(this.instance, this.instance.SourceLocation);
    }

    /// <summary>
    /// If the instance to be used with an instance field or the array.
    /// </summary>
    public Expression/*?*/ Instance {
      get {
        if (this.instance == null) {
          Expression expression = this.Expression;
          Parenthesis/*?*/ parExpr = expression as Parenthesis;
          while (parExpr != null) { expression = parExpr.ParenthesizedExpression; parExpr = expression as Parenthesis; }
          QualifiedName/*?*/ qualName = expression as QualifiedName;
          if (qualName != null) {
            this.instance = qualName.Instance;
            if (this.instance != null && this.instance.Type.IsValueType) {
              this.instance = this.GetAddressOfForInstance(this.instance);
              this.instance.SetContainingExpression(this);
            }
            return this.instance;
          }
          Indexer/*?*/ indexer = expression as Indexer;
          if (indexer != null) {
            QualifiedName qName = indexer.IndexedObject as QualifiedName;
            if (qName != null && qName.Instance == null)
              return null;
            else
              return this.instance = indexer.IndexedObject;
          }
          IFieldDefinition/*?*/ field = this.Definition as IFieldDefinition;
          if (field != null && !field.IsStatic)
            return this.instance = new ThisReference(this.ContainingBlock, this.SourceLocation);
          IPropertyDefinition/*?*/ property = this.Definition as IPropertyDefinition;
          if (property != null && !property.Setter.ResolvedMethod.IsStatic)
            return this.instance = new ThisReference(this.ContainingBlock, this.SourceLocation);
          this.instance = this;
        }
        if (this.instance == this) return null;
        return this.instance;
      }
    }
    Expression/*?*/ instance;

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public virtual object/*?*/ Resolve()
      //^ ensures result == null || result is ILocalDefinition || result is IParameterDefinition || result is IEventDefinition || 
      //^   result is IFieldDefinition || result is IArrayIndexer || result is IAddressDereference || result is IMethodDefinition || 
      //^   result is IThisReference || result is IPropertyDefinition; 
      //^ ensures result is IPropertyDefinition ==> ((IPropertyDefinition)result).Setter != null;
    {
      Expression expression = this.Expression;
      Parenthesis/*?*/ parExpr = expression as Parenthesis;
      while (parExpr != null) { expression = parExpr.ParenthesizedExpression; parExpr = expression as Parenthesis; }
      SimpleName/*?*/ simpleName = expression as SimpleName;
      if (simpleName != null) {
        object/*?*/ result = simpleName.Resolve();
        IPropertyDefinition/*?*/ propertyDefinition = result as IPropertyDefinition;
        if (propertyDefinition != null) {
          if (propertyDefinition.Setter == null) return null;
          return propertyDefinition;
        }
        if (!(result is ILocalDefinition || result is IParameterDefinition || result is IEventDefinition || result is IFieldDefinition || result is IMethodDefinition))
          return null;
        else
          return result;
      }
      QualifiedName/*?*/ qualName = expression as QualifiedName;
      if (qualName != null) {
        object/*?*/ result = qualName.Resolve(false);
        IPropertyDefinition/*?*/ propertyDefinition = result as IPropertyDefinition;
        if (propertyDefinition != null) {
          if (propertyDefinition.Setter == null) return null;
          return propertyDefinition;
        }
        if (!(result is IEventDefinition || result is IFieldDefinition || result is IMethodDefinition))
          return null;
        else
          return result;
      }
      Indexer/*?*/ indexer = expression as Indexer;
      if (indexer != null) return indexer.ResolveAsValueContainer();
      BoundExpression/*?*/ boundExpression = expression as BoundExpression;
      if (boundExpression != null) return boundExpression.Definition;
      AddressDereference/*?*/ addressDereference = expression as AddressDereference;
      if (addressDereference != null) return addressDereference;
      ThisReference/*?*/ thisReference = expression as ThisReference;
      if (thisReference != null) return thisReference;
      return null;
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingExpression(Expression containingExpression) {
      base.SetContainingExpression(containingExpression);
      this.expression.SetContainingExpression(containingExpression);
    }

    /// <summary>
    /// The type of value that the expression will evaluate to, as determined at compile time.
    /// </summary>
    public override ITypeDefinition Type {
      get { return this.Expression.Type; }
    }

  }

  /// <summary>
  /// An expression that takes the address of a target expression.
  /// </summary>
  public class AddressOf : Expression, IAddressOf {

    /// <summary>
    /// Allocates an expression that takes the address of a target expression.
    /// </summary>
    /// <param name="address">An expression that represents an addressable location in memory.</param>
    /// <param name="sourceLocation"></param>
    public AddressOf(AddressableExpression address, ISourceLocation sourceLocation)
      : this(address, false, sourceLocation) {
    }

    /// <summary>
    /// Allocates an expression that takes the address of a target expression.
    /// </summary>
    /// <param name="address">An expression that represents an addressable location in memory.</param>
    /// <param name="objectControlsMutability">
    /// If true, the address can only be used in operations where defining type of the addressed
    /// object has control over whether or not the object is mutated. For example, a value type that
    /// exposes no public fields or mutator methods cannot be changed using this address.
    /// </param>
    /// <param name="sourceLocation"></param>
    public AddressOf(AddressableExpression address, bool objectControlsMutability, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.address = address;
      this.objectControlsMutability = objectControlsMutability;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    public AddressOf(BlockStatement containingBlock, AddressOf template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.address = (AddressableExpression)template.Address.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// An expression that represents an addressable location in memory.
    /// </summary>
    public AddressableExpression Address {
      get { return this.address; }
    }
    readonly AddressableExpression address;

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return this.Address.HasErrors;
    }

    /// <summary>
    /// Calls the visitor.Visit(IAddressOf) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(AddressOf) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Checks if the expression has a side effect and reports an error unless told otherwise.
    /// </summary>
    /// <param name="reportError">If true, report an error if the expression has a side effect.</param>
    public override bool HasSideEffect(bool reportError) {
      return this.Address.HasSideEffect(reportError);
    }

    /// <summary>
    /// Infers the type of value that this expression will evaluate to. At runtime the actual value may be an instance of subclass of the result of this method.
    /// Calling this method does not cache the computed value and does not generate any error messages. In some cases, such as references to the parameters of lambda
    /// expressions during type overload resolution, the value returned by this method may be different from one call to the next.
    /// When type inference fails, Dummy.Type is returned.
    /// </summary>
    public override ITypeDefinition InferType() {
      if (this.Address.Type is Dummy) return Dummy.Type;
      return PointerType.GetPointerType(this.Address.Type, this.Compilation.HostEnvironment.InternFactory);
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new AddressOf(containingBlock, this);
    }

    /// <summary>
    /// If true, the address can only be used in operations where defining type of the addressed
    /// object has control over whether or not the object is mutated. For example, a value type that
    /// exposes no public fields or mutator methods cannot be changed using this address.
    /// </summary>
    public bool ObjectControlsMutability {
      get { return this.objectControlsMutability; }
    }
    readonly bool objectControlsMutability;

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      return this;
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingExpression(Expression containingExpression) {
      base.SetContainingExpression(containingExpression);
      this.address.SetContainingExpression(this);
    }

    /// <summary>
    /// The type of value that the expression will evaluate to, as determined at compile time.
    /// </summary>
    public sealed override ITypeDefinition Type {
      get {
        if (this.type == null)
          this.type = this.InferType();
        return this.type;
      }
    }
    //^ [Once]
    ITypeDefinition/*?*/ type;

    #region IAddressOf Members

    IAddressableExpression IAddressOf.Expression {
      get { return this.Address; }
    }

    #endregion

    #region IExpression Members

    ITypeReference IExpression.Type {
      get { return this.Type; }
    }

    #endregion

  }

  /// <summary>
  /// An expression that deferences an address (pointer).
  /// </summary>
  public class AddressDereference : Expression, IAddressDereference {

    /// <summary>
    /// Allocates an expression that deferences an address (pointer).
    /// </summary>
    /// <param name="address">The address to dereference.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public AddressDereference(Expression address, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.address = address;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected AddressDereference(BlockStatement containingBlock, AddressDereference template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.address = template.address.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// The address to dereference.
    /// </summary>
    public Expression Address {
      get { return this.address; }
    }
    readonly Expression address;

    /// <summary>
    /// If the addres to dereference is not aligned with the size of the target type, this property specifies the actual alignment.
    /// For example, a value of 1 specifies that the pointer is byte aligned, whereas the target type may be word sized.
    /// </summary>
    public virtual byte Alignment {
      get { return 1; }
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return this.Address.HasErrors || this.Type is Dummy;
    }

    /// <summary>
    /// Calls the visitor.Visit(IAddressDereference) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(AddressDeference) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Either returns this expression, or returns a BlockExpression that assigns each subexpression to a temporary local variable
    /// and then evaluates an expression that is the same as this expression, but which refers to the temporaries rather than the 
    /// factored out subexpressions. This transformation is useful when expressing the semantics of operation assignments and increment/decrement operations.
    /// </summary>
    public override Expression FactoredExpression()
      //^^ ensures result == this || result is BlockExpression;
    {
      //TODO: check if this.Address can be factored
      return this;
    }

    /// <summary>
    /// Checks if the expression has a side effect and reports an error unless told otherwise.
    /// </summary>
    /// <param name="reportError">If true, report an error if the expression has a side effect.</param>
    public override bool HasSideEffect(bool reportError) {
      return this.Address.HasSideEffect(reportError);
    }

    /// <summary>
    /// True if the address is not aligned to the natural size of the target type. If true, the actual alignment of the
    /// address is specified by this.Alignment.
    /// </summary>
    public virtual bool IsUnaligned {
      get { return false; }
    }

    /// <summary>
    /// The location at Address is volatile and its contents may not be cached.
    /// </summary>
    public bool IsVolatile {
      get { return false; }
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new AddressDereference(containingBlock, this);
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      return this;
    }

    /// <summary>
    /// Infers the type of value that this expression will evaluate to. At runtime the actual value may be an instance of subclass of the result of this method.
    /// Calling this method does not cache the computed value and does not generate any error messages. In some cases, such as references to the parameters of lambda
    /// expressions during type overload resolution, the value returned by this method may be different from one call to the next.
    /// When type inference fails, Dummy.Type is returned.
    /// </summary>
    public override ITypeDefinition InferType() {
      IManagedPointerTypeReference/*?*/ managedPointerToDereference = this.Address.Type as IManagedPointerTypeReference;
      if (managedPointerToDereference != null) return managedPointerToDereference.TargetType.ResolvedType;
      IPointerTypeReference/*?*/ pointerToDeference = this.Address.Type as IPointerTypeReference;
      if (pointerToDeference != null) return pointerToDeference.TargetType.ResolvedType;
      IFunctionPointerTypeReference functionPtr = this.Address.Type as IFunctionPointerTypeReference;
      if (functionPtr != null) return functionPtr.ResolvedType;
      return Dummy.Type;
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingExpression(Expression containingExpression) {
      base.SetContainingExpression(containingExpression);
      this.address.SetContainingExpression(this);
    }

    /// <summary>
    /// The type of value that the expression will evaluate to, as determined at compile time.
    /// </summary>
    public sealed override ITypeDefinition Type {
      get {
        if (this.type == null)
          this.type = this.InferType();
        return this.type;
      }
    }
    //^ [Once]
    ITypeDefinition/*?*/ type;

    #region IAddressDereference Members

    IExpression IAddressDereference.Address {
      get { return this.Address.ProjectAsIExpression(); }
    }

    #endregion

    #region IExpression Members

    ITypeReference IExpression.Type {
      get { return this.Type; }
    }

    #endregion

  }

  /// <summary>
  /// vb
  /// </summary>
  public class AdhocProperty : Expression {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="qualifier"></param>
    /// <param name="nameExpression"></param>
    /// <param name="sourceLocation"></param>
    public AdhocProperty(Expression qualifier, SimpleName nameExpression, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.qualifier = qualifier;
      this.nameExpression = nameExpression;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected AdhocProperty(BlockStatement containingBlock, AdhocProperty template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.qualifier = template.qualifier.MakeCopyFor(containingBlock);
      this.nameExpression = (SimpleName)template.nameExpression.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// Calls the visitor.Visit(AdhocProperty) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      //visitor.Visit(this);
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      //return this;
      return new DummyExpression(this.SourceLocation); //TODO: what does an adhoc property project as?
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new AdhocProperty(containingBlock, this);
    }

    /// <summary>
    /// 
    /// </summary>
    public SimpleName NameExpression {
      get { return this.nameExpression; }
    }
    readonly SimpleName nameExpression;

    /// <summary>
    /// 
    /// </summary>
    public Expression Qualifier {
      get { return this.qualifier; }
    }
    readonly Expression qualifier;

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public object/*?*/ Resolve() {
      return null; //TODO: implement this
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingExpression(Expression containingExpression) {
      base.SetContainingExpression(containingExpression);
      this.nameExpression.SetContainingExpression(this);
    }

    /// <summary>
    /// The type of value that the expression will evaluate to, as determined at compile time.
    /// </summary>
    public override ITypeDefinition Type {
      get { return Dummy.Type; } //TODO: implement this
    }
  }

  /// <summary>
  /// 
  /// </summary>
  public class AnonymousDelegate : Expression, IAnonymousDelegate, ISignatureDeclaration {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="anonymousMethod"></param>
    /// <param name="delegateType"></param>
    public AnonymousDelegate(AnonymousMethod anonymousMethod, ITypeDefinition delegateType)
      : base(anonymousMethod.SourceLocation)
      //^ requires targetType.IsDelegate;
    {
      this.anonymousMethod = anonymousMethod;
      this.body = anonymousMethod.Body;
      this.delegateType = delegateType;
      this.parameters = new List<ParameterDeclaration>(anonymousMethod.Parameters);
    }

    private readonly AnonymousMethod anonymousMethod;

    /// <summary>
    /// A block of statements providing the implementation of the anonymous method that is called by the delegate that is the result of this expression.
    /// </summary>
    /// <value></value>
    public BlockStatement Body {
      get {
        return this.body = (BlockStatement)this.body.MakeCopyFor(this.DummyBlock);
      }
    }
    BlockStatement body;

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// </summary>
    /// <returns></returns>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      bool result = this.Body.HasErrors;
      foreach (ParameterDeclaration parDecl in this.Parameters)
        result |= parDecl.Type.HasErrors;
      return result;
    }

    private readonly ITypeDefinition delegateType;
    //^ invariant delegateType.IsDelegate;

    /// <summary>
    /// Calls visitor.Visit(IAnonymousDelegateExpression).
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    private BlockStatement DummyBlock {
      get {
        BlockStatement/*?*/ result = this.dummyBlock;
        if (result == null) {
          result = BlockStatement.CreateDummyFor(SourceDummy.SourceLocation);
          result.SetContainers(this.ContainingBlock, this);
          this.dummyBlock = result;
        }
        return result;
      }
    }
    BlockStatement/*?*/ dummyBlock;

    /// <summary>
    /// Checks if the expression has a side effect and reports an error unless told otherwise.
    /// </summary>
    /// <param name="reportError">If true, report an error if the expression has a side effect.</param>
    /// <returns></returns>
    public override bool HasSideEffect(bool reportError) {
      return false;
    }

    /// <summary>
    /// True if the referenced method or property does not require an instance of its declaring type as its first argument.
    /// </summary>
    public bool IsStatic {
      get { return false; } //TODO: if the delegate captures nothing, it can be static
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    /// <param name="containingBlock"></param>
    /// <returns></returns>
    public override Expression MakeCopyFor(BlockStatement containingBlock) {
      //^ assume false;
      return this;
    }

    /// <summary>
    /// The parameters this anonymous method.
    /// </summary>
    public IEnumerable<ParameterDeclaration> Parameters {
      get {
        for (int i = 0, n = this.parameters.Count; i < n; i++)
          yield return this.parameters[i] = this.parameters[i].MakeShallowCopyFor(this, this.ContainingBlock);
      }
    }
    readonly List<ParameterDeclaration> parameters;

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    /// <returns></returns>
    protected override IExpression ProjectAsNonConstantIExpression() {
      return this;
    }

    /// <summary>
    /// The return type of the delegate.
    /// </summary>
    /// <value></value>
    public ITypeReference ReturnType {
      get { return this.ContainingBlock.Helper.GetInvokeMethod(this.delegateType).Type; }
    }

    /// <summary>
    /// The type of value that the expression will evaluate to, as determined at compile time.
    /// </summary>
    /// <value></value>
    public override ITypeDefinition Type {
      get { return this.delegateType; }
    }

    #region IAnonymousDelegate Members

    IEnumerable<IParameterDefinition> IAnonymousDelegate.Parameters {
      get {
        foreach (ParameterDeclaration parameterDeclaration in this.Parameters)
          yield return parameterDeclaration.ParameterDefinition;
      }
    }

    ITypeReference IAnonymousDelegate.Type {
      get { return this.Type; }
    }

    #endregion

    #region IExpression Members


    ITypeReference IExpression.Type {
      get { return this.Type; }
    }

    #endregion

    #region ISignature Members

    CallingConvention ISignature.CallingConvention {
      get { return CallingConvention.HasThis; }
    }

    IEnumerable<IParameterTypeInformation> ISignature.Parameters {
      get {
        foreach (var par in this.Parameters) yield return par.ParameterDefinition;
      }
    }

    IEnumerable<ICustomModifier> ISignature.ReturnValueCustomModifiers {
      get { return Enumerable<ICustomModifier>.Empty; }
    }

    bool ISignature.ReturnValueIsByRef {
      get { return false; }
    }

    bool ISignature.ReturnValueIsModified {
      get { return false; }
    }

    ITypeReference ISignature.Type {
      get { return this.ReturnType; }
    }

    #endregion

    #region ISignatureDeclaration Members

    TypeExpression ISignatureDeclaration.Type {
      get {
        if (this.returnType == null)
          this.returnType = TypeExpression.For(this.Helper.GetInvokeMethod(this.delegateType).Type);
        return this.returnType;
      }
    }
    TypeExpression/*?*/ returnType;

    ISignature ISignatureDeclaration.SignatureDefinition {
      get { return this; }
    }

    #endregion

    #region IAnonymousDelegate Members

    IBlockStatement IAnonymousDelegate.Body {
      get { return this.Body; }
    }

    #endregion
  }

  /// <summary>
  /// An expression that defines an anonymous method and that evaluates to a method group containing just the defined method.
  /// </summary>
  public class AnonymousMethod : Expression, ISignatureDeclaration, ISignature {

    /// <summary>
    /// Allocates an expression that defines an anonymous method and that evaluates to a method group containing just the defined method.
    /// </summary>
    /// <param name="parameters">The parameters of this anonymous method.</param>
    /// <param name="body">A block of statements providing the implementation of this anonymous method.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public AnonymousMethod(IEnumerable<ParameterDeclaration> parameters, BlockStatement body, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.parameters = new List<ParameterDeclaration>(parameters);
      this.body = body;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected AnonymousMethod(BlockStatement containingBlock, AnonymousMethod template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.parameters = new List<ParameterDeclaration>(template.parameters);
      this.body = (BlockStatement)template.Body.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// A block of statements providing the implementation of this anonymous method.
    /// </summary>
    public BlockStatement Body {
      get {
        return this.body = (BlockStatement)this.body.MakeCopyFor(this.DummyBlock);
      }
    }
    BlockStatement body;

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return false;
    }

    /// <summary>
    /// Calls the visitor.Visit(AnonymousMethod) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    private BlockStatement DummyBlock {
      get {
        BlockStatement/*?*/ result = this.dummyBlock;
        if (result == null) {
          result = BlockStatement.CreateDummyFor(SourceDummy.SourceLocation);
          result.SetContainers(this.ContainingBlock, this);
          this.dummyBlock = result;
        }
        return result;
      }
    }
    BlockStatement/*?*/ dummyBlock;

    /// <summary>
    /// Checks if the expression has a side effect and reports an error unless told otherwise.
    /// </summary>
    /// <param name="reportError">If true, report an error if the expression has a side effect.</param>
    public override bool HasSideEffect(bool reportError) {
      return false; //TODO: implement this
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new AnonymousMethod(containingBlock, this);
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      //^ assume false;
      return new DummyExpression(this.SourceLocation);
    }

    /// <summary>
    /// The type of value that the expression will evaluate to, as determined at compile time.
    /// </summary>
    public override ITypeDefinition Type {
      get { return Dummy.Type; }  //Think of an anonymous method as a method group with a single method.
    }

    /// <summary>
    /// The parameters this anonymous method.
    /// </summary>
    public IEnumerable<ParameterDeclaration> Parameters {
      get {
        for (int i = 0, n = this.parameters.Count; i < n; i++)
          yield return this.parameters[i] = this.parameters[i].MakeShallowCopyFor(this, this.ContainingBlock);
      }
    }
    readonly List<ParameterDeclaration> parameters;

    /// <summary>
    /// The symbol table object that represents the metadata for this signature.
    /// </summary>
    public ISignature SignatureDefinition {
      get { return this; }
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingExpression(Expression containingExpression) {
      base.SetContainingExpression(containingExpression);
      this.body.SetContainers(containingExpression.ContainingBlock, this);
      foreach (ParameterDeclaration parameter in this.parameters)
        parameter.SetContainingSignatureAndExpression(this, containingExpression);
    }

    #region ISignatureDeclaration Members

    TypeExpression ISignatureDeclaration.Type {
      get { return TypeExpression.For(Dummy.Type); }
    }

    #endregion

    #region ISignature Members

    /// <summary>
    /// Calling convention of the signature.
    /// </summary>
    /// <value></value>
    public CallingConvention CallingConvention {
      get { return CallingConvention.HasThis; }
    }

    /// <summary>
    /// True if the referenced method or property does not require an instance of its declaring type as its first argument.
    /// </summary>
    public bool IsStatic {
      get { return false; }
    }

    IEnumerable<IParameterTypeInformation> ISignature.Parameters {
      get {
        foreach (ParameterDeclaration parDecl in this.Parameters)
          yield return parDecl.ParameterDefinition;
      }
    }

    /// <summary>
    /// Returns the list of custom modifiers, if any, associated with the returned value. Evaluate this property only if ReturnValueIsModified is true.
    /// </summary>
    /// <value></value>
    public IEnumerable<ICustomModifier> ReturnValueCustomModifiers {
      get { return Enumerable<ICustomModifier>.Empty; }
    }

    /// <summary>
    /// True if the return value is passed by reference (using a managed pointer).
    /// </summary>
    /// <value></value>
    public bool ReturnValueIsByRef {
      get { return false; }
    }

    /// <summary>
    /// True if the return value has one or more custom modifiers associated with it.
    /// </summary>
    /// <value></value>
    public bool ReturnValueIsModified {
      get { return false; }
    }

    ITypeReference ISignature.Type {
      get { return Dummy.Type; }
    }

    #endregion
  }

  /// <summary>
  /// The name of a group of types or a nested namespace, qualified by a type or namespace alias. 
  /// For example an expression such as Sys::Collections in C#, where previously using Sys = System; has been defined.
  /// </summary>
  public class AliasQualifiedName : Expression {

    /// <summary>
    /// Allocates an expression which serves a the name of a group of types or a nested namespace, qualified by a type or namespace alias. 
    /// For example an expression such as Sys::Collections in C#, where previously using Sys = System; has been defined.
    /// </summary>
    public AliasQualifiedName(Expression alias, SimpleName simpleName, ISourceLocation sourceLocation)
      : base(sourceLocation)
      //^ requires alias is SimpleName || alias is RootNamespaceExpression;
    {
      this.alias = alias;
      this.simpleName = simpleName;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected AliasQualifiedName(BlockStatement containingBlock, AliasQualifiedName template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.alias = template.Alias.MakeCopyFor(containingBlock);
      this.simpleName = (SimpleName)template.SimpleName.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// The alias that qualifies SimpleName. I.e. the a in a::b.
    /// </summary>
    public Expression Alias {
      get
        //^ ensures result is SimpleName || result is RootNamespaceExpression;
      {
        return this.alias;
      }
    }
    readonly Expression alias;
    //^ invariant alias is SimpleName || alias is RootNamespaceExpression;

    /// <summary>
    /// Calls the visitor.Visit(AliasQualifiedName) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      //^ assume false;
      return new DummyExpression(this.SourceLocation); //This expression should always be part of another expression that projects onto something that does not include this object as a subexpression.
    }

    /// <summary>
    /// Makes a copy of this alias qualified name, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new AliasQualifiedName(containingBlock, this);
    }

    /// <summary>
    /// Returns a namespace or type group that binds to the simple name part of this using the resolved value of this.Alias as the resolution scope.
    /// </summary>
    public virtual object/*?*/ Resolve()
      //^ ensures result == null || result is INamespaceDefinition || result is ITypeGroup;
    {
      object/*?*/ root = this.ResolveAlias();
      INamespaceDefinition/*?*/ nsDef = root as INamespaceDefinition;
      if (nsDef != null) return this.Resolve(nsDef);
      ITypeDefinition/*?*/ typeDef = root as ITypeDefinition;
      if (typeDef != null) return this.Resolve(typeDef);
      //^ assert root == null;
      return null;
    }

    /// <summary>
    /// Returns a namespace or type that binds to the simple name part of this using the resolved value of this.Alias as the resolution scope.
    /// </summary>
    public virtual object/*?*/ ResolveAsNamespaceOrType()
      //^ ensures result == null || result is INamespaceDefinition || result is ITypeDefinition;
    {
      object/*?*/ result = this.Resolve();
      INamespaceDefinition/*?*/ nsDef = result as INamespaceDefinition;
      if (nsDef != null) return nsDef;
      ITypeGroup/*?*/ typeGroup = result as ITypeGroup;
      if (typeGroup != null) {
        foreach (ITypeDefinition type in typeGroup.Types) {
          if (!type.IsGeneric) return type;
        }
        return null;
      }
      //^ assert result == null;
      return null;
    }

    /// <summary>
    /// Resolve the alias qualified name, given that the alias has resolved to the given namespace definition.
    /// </summary>
    /// <param name="namespaceDefinition">The namespace definition to which this.Alias has resolved.</param>
    /// <returns>The namespace or type group to which this.Alias::this.SimpleName resolves to.</returns>
    protected virtual object/*?*/ Resolve(INamespaceDefinition namespaceDefinition)
      //^ ensures result == null || result is INamespaceDefinition || result is ITypeGroup;
    {
      INamespaceDefinition/*?*/ nsDef = null;
      foreach (INamespaceMember member in namespaceDefinition.GetMembersNamed(this.SimpleName.Name, false)) {
        ITypeDefinition/*?*/ typeDef = member as ITypeDefinition;
        if (typeDef != null) return new NamespaceTypeGroup(this, namespaceDefinition, this.SimpleName);
        if (nsDef == null) nsDef = member as INamespaceDefinition;
      }
      return nsDef;
    }

    /// <summary>
    /// Resolve the alias qualified name, given that the alias has resolved to the given type definition.
    /// </summary>
    /// <param name="typeDefinition">The type definition to which this.Alias has resolved.</param>
    /// <returns>The nested type group to which this.Alias::this.SimpleName resolves to.</returns>
    protected virtual ITypeGroup/*?*/ Resolve(ITypeDefinition typeDefinition) {
      foreach (ITypeDefinitionMember member in typeDefinition.Members) {
        ITypeDefinition/*?*/ nestedType = member as ITypeDefinition;
        if (nestedType != null) return new NestedTypeGroup(this, typeDefinition, this.SimpleName);
      }
      foreach (ITypeReference baseClassRef in typeDefinition.BaseClasses) {
        ITypeGroup/*?*/ result = this.Resolve(baseClassRef.ResolvedType);
        if (result != null) return result;
      }
      return null;
    }

    /// <summary>
    /// Resolves this.Alias to either a namespace definition or a type group, by looking at the alias
    /// definitions of them immediately enclosing namespace declaration and then proceeding outwards.
    /// If the alias qualified name forms part of an alias definition in the immediately enclosing namespace
    /// declaration, then the search proceeds directly to the next enclosing namespace declaration.
    /// If the alias does not resolve an error is reported and null is returned.
    /// </summary>
    public virtual object/*?*/ ResolveAlias()
      //^ ensures result == null || result is INamespaceDefinition || result is ITypeDefinition;
    {
      object/*?*/ result = this.resolvedAlias;
      if (result == null) {
        if (this.Alias is RootNamespaceExpression) {
          this.resolvedAlias = result = this.Compilation.UnitSet.UnitSetNamespaceRoot;
          //^ assume result is INamespaceDefinition; //IUnitSetNamespace : INamespaceDefinition
        } else {
          SimpleName aliasName = (SimpleName)this.Alias;
          NamespaceDeclaration containingNamespace = this.ContainingBlock.ContainingNamespaceDeclaration;
          while (result == null) {
            AliasDeclaration/*?*/ aliasDeclaration = null;
            UnitSetAliasDeclaration/*?*/ unitSetAliasDeclaration = null;
            if (!containingNamespace.BusyResolvingAnAliasOrImport) {
              containingNamespace.GetAliasNamed(aliasName.Name, aliasName.IgnoreCase, ref aliasDeclaration, ref unitSetAliasDeclaration);
              if (aliasDeclaration != null) {
                this.resolvedAlias = result = aliasDeclaration.ResolvedNamespaceOrType;
                break;
              }
              if (unitSetAliasDeclaration != null) {
                this.resolvedAlias = result = unitSetAliasDeclaration.UnitSet.UnitSetNamespaceRoot;
                //^ assume result is INamespaceDefinition; //IUnitSetNamespace : INamespaceDefinition
                break;
              }
            }
            NestedNamespaceDeclaration/*?*/ nestedContainingNamespace = containingNamespace as NestedNamespaceDeclaration;
            if (nestedContainingNamespace == null) {
              this.Helper.ReportError(new AstErrorMessage(this, Error.AliasNotFound, aliasName.Name.Value));
              this.resolvedAlias = result = Dummy.RootUnitNamespace;
              //^ assume result is INamespaceDefinition; //IUnitSetNamespace : INamespaceDefinition
              break;
            }
            containingNamespace = nestedContainingNamespace.ContainingNamespaceDeclaration;
          }
        }
      }
      if (result is Dummy) return null;
      //^ assume result is INamespaceDefinition || result is ITypeDefinition;
      return result;
    }
    private object/*?*/ resolvedAlias;

    /// <summary>
    /// Completes the two stage construction of the object. This allows bottom up parsers to construct a QualifiedName before constructing the containing Block.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itslef should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingExpression(Expression containingExpression) {
      base.SetContainingExpression(containingExpression);
      this.alias.SetContainingExpression(this);
      this.simpleName.SetContainingExpression(this);
    }

    /// <summary>
    /// The name of a member of the namespace or type denoted by Alias.
    /// </summary>
    public SimpleName SimpleName {
      get { return this.simpleName; }
    }
    readonly SimpleName simpleName;

    /// <summary>
    /// Always returns Dummy.Type since an alias qualified name can only denote a namespace or a type. Neither corresponds to a runtime value.
    /// </summary>
    public override ITypeDefinition Type {
      get { return Dummy.Type; }
    }
  }

  /// <summary>
  /// An expression that denotes an array type.
  /// </summary>
  public class ArrayTypeExpression : TypeExpression {

    /// <summary>
    /// Allocates an expression that denotes an array type.
    /// </summary>
    /// <param name="elementType">The type of the elements of this array.</param>
    /// <param name="rank">The number of array dimensions.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public ArrayTypeExpression(TypeExpression elementType, uint rank, ISourceLocation sourceLocation)
      : base(sourceLocation)
      //^ requires rank > 0;
    {
      this.elementType = elementType;
      this.rank = rank;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected ArrayTypeExpression(BlockStatement containingBlock, ArrayTypeExpression template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.elementType = (TypeExpression)template.ElementType.MakeCopyFor(containingBlock);
      this.rank = template.Rank;
    }

    /// <summary>
    /// Calls the visitor.Visit(ArrayTypeExpression) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The type of the elements of this array.
    /// </summary>
    public TypeExpression ElementType {
      get { return this.elementType; }
    }
    readonly TypeExpression elementType;

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new ArrayTypeExpression(containingBlock, this);
    }

    /// <summary>
    /// The number of array dimensions.
    /// </summary>
    public uint Rank {
      get
        //^ ensures result > 0;
      {
        return this.rank;
      }
    }
    readonly uint rank; //^ invariant rank > 0;

    /// <summary>
    /// The type denoted by the expression. If expression cannot be resolved, a dummy type is returned. If the expression is ambiguous the first matching type is returned.
    /// If the expression does not resolve to exactly one type, an error is added to the error collection of the compilation context.
    /// </summary>
    protected override ITypeDefinition Resolve() {
      if (this.rank == 1) return Vector.GetVector(this.ElementType.ResolvedType, this.Compilation.HostEnvironment.InternFactory);
      return Matrix.GetMatrix(this.ElementType.ResolvedType, this.rank, this.Compilation.HostEnvironment.InternFactory);
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingExpression(Expression containingExpression) {
      base.SetContainingExpression(containingExpression);
      this.elementType.SetContainingExpression(this);
    }

  }

  /// <summary>
  /// An expression that assigns the value of the source (right) operand to the location represented by the target (left) operand.
  /// The expression result is the value of the source expression.
  /// </summary>
  public class Assignment : Expression, IAssignment {

    /// <summary>
    /// Allocates an expression that assigns the value of the source (right) operand to the location represented by the target (left) operand.
    /// The expression result is the value of the source expression.
    /// </summary>
    /// <param name="target">The target of the assignment, for example simple name or a qualified name or an indexer.</param>
    /// <param name="source">An expression that results in a value that is to be assigned to the target.</param>
    /// <param name="sourceLocation">The source location of the operation.</param>
    public Assignment(TargetExpression target, Expression source, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.target = target;
      this.source = source;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected Assignment(BlockStatement containingBlock, Assignment template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.target = (TargetExpression)template.target.MakeCopyFor(containingBlock);
      this.source = template.source.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return this.Target.HasErrors || this.ConvertedSourceExpression is DummyExpression || this.ConvertedSourceExpression.HasErrors;
    }

    /// <summary>
    /// An expression that performs an implicit conversion of this.Source to this.Type.
    /// </summary>
    public Expression ConvertedSourceExpression {
      get {
        if (this.convertedSourceExpression == null)
          this.convertedSourceExpression = this.GetConvertedSourceExpression();
        return this.convertedSourceExpression;
      }
    }
    Expression/*?*/ convertedSourceExpression;

    /// <summary>
    /// Calls the visitor.Visit(IAssignment) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(Assignment) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Returns an expression that performs an implicit conversion of this.Source to this.Type.
    /// </summary>
    protected virtual Expression GetConvertedSourceExpression() {
      Expression result = this.Helper.ImplicitConversionInAssignmentContext(this.source, this.Type);
      if (result is DummyExpression && !this.Source.HasErrors && !this.Target.HasErrors)
        this.Helper.ReportFailedImplicitConversion(this.Source, this.Type);
      return result;
    }

    /// <summary>
    /// Checks if the expression has a side effect and reports an error unless told otherwise.
    /// </summary>
    /// <param name="reportError">If true, report an error if the expression has a side effect.</param>
    public override bool HasSideEffect(bool reportError) {
      if (this.hasSideEffect == null) {
        if (reportError) {
          this.Helper.ReportError(new AstErrorMessage(this, Error.ExpressionHasSideEffect));
          this.hasSideEffect = true;
        }
        return true;
      }
      return this.hasSideEffect.Value;
    }
    bool? hasSideEffect;

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new Assignment(containingBlock, this);
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      if (this.cachedProjection != null) return this.cachedProjection;
      if (this.HasErrors)
        return this.cachedProjection = new DummyExpression(this.SourceLocation);
      object resolvedTarget = this.Target.Definition;
      IPropertyDefinition/*?*/ property = resolvedTarget as IPropertyDefinition;
      if (property != null) {
        IMethodReference/*?*/ setter = property.Setter;
        if (setter != null) {
          Expression/*?*/ instance = this.Target.Instance;
          List<Expression> arguments = new List<Expression>(1);
          Indexer/*?*/ idx = this.Target.Expression as Indexer;
          if (idx != null)
            arguments.AddRange(idx.ConvertedArguments);
          arguments.Add(this.ConvertedSourceExpression);
          if (setter.ResolvedMethod.IsStatic) {
            return this.cachedProjection = new ResolvedMethodCall(setter.ResolvedMethod, arguments, this.SourceLocation);
          } else {
            if (instance == null) instance = new DummyExpression(this.Target.SourceLocation);
            return this.cachedProjection = new ResolvedMethodCall(setter.ResolvedMethod, instance, arguments, this.SourceLocation);
          }
        }
      }
      IParameterDefinition/*?*/ parameter = resolvedTarget as IParameterDefinition;
      if (parameter != null && parameter.IsByReference) {
        var dereferencedParameter = new Microsoft.Cci.MutableCodeModel.AddressDereference();
        dereferencedParameter.Address = new BoundExpression(this.Target, parameter, this.ContainingBlock);
        dereferencedParameter.Type = parameter.Type;
        dereferencedParameter.Locations.Add(this.Target.SourceLocation);

        var target = new Microsoft.Cci.MutableCodeModel.TargetExpression();
        target.Definition = dereferencedParameter;
        target.Locations = dereferencedParameter.Locations;
        target.Type = parameter.Type;

        var assignToDeferencedParameter = new Microsoft.Cci.MutableCodeModel.Assignment(this);
        assignToDeferencedParameter.Target = target;
        assignToDeferencedParameter.Source = this.ConvertedSourceExpression.ProjectAsIExpression();
        assignToDeferencedParameter.Type = this.Type;
        return this.cachedProjection = assignToDeferencedParameter;
      }
      return this.cachedProjection = this;
    }
    IExpression/*?*/ cachedProjection;

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingExpression(Expression containingExpression) {
      base.SetContainingExpression(containingExpression);
      this.source.SetContainingExpression(this);
      this.target.SetContainingExpression(this);
    }

    /// <summary>
    /// The expression representing the value to assign. 
    /// </summary>
    public Expression Source {
      get { return this.source; }
    }
    readonly Expression source;

    /// <summary>
    /// The expression representing the target to assign to. This expression may also appear as the left operand of an IBinaryExpression instance returned by this.Source.
    /// In that case any sub expressions that are evaluated to obtain the value of the Source will not be re-evaluated in order to obtain the address of the variable
    /// represented by Target. In C#, the += operator is an example of such usage.
    /// </summary>
    public TargetExpression Target {
      get { return this.target; }
    }
    readonly TargetExpression target;

    /// <summary>
    /// The type of value that the expression will evaluate to, as determined at compile time.
    /// </summary>
    public override ITypeDefinition Type {
      get { return this.Target.Expression.Type; }
    }

    #region IAssignment Members

    IExpression IAssignment.Source {
      get { return this.ConvertedSourceExpression.ProjectAsIExpression(); }
    }

    ITargetExpression IAssignment.Target {
      get { return this.Target; }
    }

    #endregion

    #region IExpression Members

    ITypeReference IExpression.Type {
      get { return this.Type; }
    }

    #endregion
  }

  /// <summary>
  /// An expression that invokes a base class constructor.
  /// </summary>
  public class BaseClassConstructorCall : MethodCall {

    /// <summary>
    /// Allocates an expression that invokes a base class constructor.
    /// </summary>
    /// <param name="originalArguments">Expressions that result in the arguments to be passed to the base class constructor.</param>
    /// <param name="sourceLocation">The source location of the call expression.</param>
    public BaseClassConstructorCall(IEnumerable<Expression> originalArguments, ISourceLocation sourceLocation)
      : base(new DummyExpression(sourceLocation), originalArguments, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected BaseClassConstructorCall(BlockStatement containingBlock, MethodCall template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Returns a collection of methods that represent the the base class constructors of the declaring type of the method containing this call.
    /// </summary>
    /// <param name="allowMethodParameterInferencesToFail">Ignored.</param>
    public override IEnumerable<IMethodDefinition> GetCandidateMethods(bool allowMethodParameterInferencesToFail) {
      TypeDeclaration/*?*/ declaringType = this.ContainingBlock.ContainingTypeDeclaration;
      if (declaringType == null || !(declaringType is IClassDeclaration)) yield break;
      //^ assert declaringType != null;
      foreach (ITypeReference baseClassRef in declaringType.TypeDefinition.BaseClasses) {
        foreach (ITypeDefinitionMember member in baseClassRef.ResolvedType.GetMembersNamed(this.NameTable.Ctor, false)) {
          IMethodDefinition/*?*/ meth = member as IMethodDefinition;
          if (meth != null && meth.IsSpecialName) yield return meth;
        }
        yield break;
      }
      foreach (ITypeDefinitionMember member in this.PlatformType.SystemObject.ResolvedType.GetMembersNamed(this.NameTable.Ctor, false)) {
        IMethodDefinition/*?*/ meth = member as IMethodDefinition;
        if (meth != null && meth.IsSpecialName) yield return meth;
      }
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (this.ContainingBlock == containingBlock) return this;
      return new BaseClassConstructorCall(containingBlock, this);
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      return this;
    }

    /// <summary>
    /// The this argument of the base class constructor.
    /// </summary>
    public override Expression ThisArgument {
      get
        //^^ requires !this.ResolvedMethod.IsStatic;
      {
        BaseClassReference result = new BaseClassReference(this.MethodExpression.SourceLocation);
        result.SetContainingExpression(this);
        return result;
      }
    }

  }

  /// <summary>
  /// An expression that represents a reference to the base class instance of the current object instance. 
  /// Used to qualify calls to base class methods from inside overrides, and so on.
  /// </summary>
  public class BaseClassReference : Expression, IThisReference {

    /// <summary>
    /// Allocates an expression that represents a reference to the base class instance of the current object instance. 
    /// Used to qualify calls to base class methods from inside overrides, and so on.
    /// </summary>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public BaseClassReference(ISourceLocation sourceLocation)
      : base(sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected BaseClassReference(BlockStatement containingBlock, BaseClassReference template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Calls the visitor.Visit(IThisReference) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(BaseClassReference) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Infers the type of value that this expression will evaluate to. At runtime the actual value may be an instance of subclass of the result of this method.
    /// Calling this method does not cache the computed value and does not generate any error messages. In some cases, such as references to the parameters of lambda
    /// expressions during type overload resolution, the value returned by this method may be different from one call to the next.
    /// When type inference fails, Dummy.Type is returned.
    /// </summary>
    public override ITypeDefinition InferType() {
      TypeDeclaration/*?*/ declaringType = this.ContainingBlock.ContainingTypeDeclaration;
      if (declaringType == null) {
        //TODO: error base in the wrong place
        return Dummy.Type;
      }
      IEnumerator<ITypeReference> baseClasses = declaringType.TypeDefinition.BaseClasses.GetEnumerator();
      if (baseClasses.MoveNext()) return baseClasses.Current.ResolvedType; //TODO: what if more than one base type?
      //TODO: error base in wrong place
      return Dummy.Type;
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new BaseClassReference(containingBlock, this);
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      return this;
    }

    /// <summary>
    /// The type of value that the expression will evaluate to, as determined at compile time.
    /// </summary>
    public sealed override ITypeDefinition Type {
      get {
        if (this.type == null)
          this.type = this.InferType();
        return this.type;
      }
    }
    //^ [Once]
    ITypeDefinition/*?*/ type;


    #region IExpression Members

    ITypeReference IExpression.Type {
      get { return this.Type; }
    }

    #endregion
  }

  /// <summary>
  /// A binary operation performed on a left and right operand.
  /// </summary>
  public abstract class BinaryOperation : Expression, IBinaryOperation {

    /// <summary>
    /// Initializes a binary operation performed on a left and right operand.
    /// </summary>
    /// <param name="leftOperand">The left operand.</param>
    /// <param name="rightOperand">The right operand.</param>
    /// <param name="sourceLocation">The source location of the operation.</param>
    protected BinaryOperation(Expression leftOperand, Expression rightOperand, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.leftOperand = leftOperand;
      this.rightOperand = rightOperand;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected BinaryOperation(BlockStatement containingBlock, BinaryOperation template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.leftOperand = template.LeftOperand.MakeCopyFor(containingBlock);
      this.rightOperand = template.RightOperand.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// 
    /// </summary>
    public Expression ConvertedLeftOperand {
      get {
        MethodCall/*?*/ overloadedCall = this.OverloadMethodCall;
        if (overloadedCall != null) {
          IEnumerator<Expression> args = overloadedCall.ConvertedArguments.GetEnumerator();
          args.MoveNext();
          return args.Current;
        }
        return new DummyExpression(this.LeftOperand.SourceLocation);
      }
    }

    /// <summary>
    /// 
    /// </summary>
    public Expression ConvertedRightOperand {
      get {
        MethodCall/*?*/ overloadedCall = this.OverloadMethodCall;
        if (overloadedCall != null) {
          IEnumerator<Expression> args = overloadedCall.ConvertedArguments.GetEnumerator();
          args.MoveNext();
          args.MoveNext();
          return args.Current;
        }
        return new DummyExpression(this.RightOperand.SourceLocation);
      }
    }

    /// <summary>
    /// True if the constant is a positive integer that could be interpreted as a negative signed integer.
    /// For example, 0x80000000, could be interpreted as a convenient way of writing int.MinValue.
    /// </summary>
    public override bool CouldBeInterpretedAsNegativeSignedInteger {
      get {
        if (this.Value == null) return false;
        if (this.LeftOperand.CouldBeInterpretedAsNegativeSignedInteger)
          return this.RightOperand.ValueIsPolymorphicCompileTimeConstant || this.LeftOperand.Type.TypeCode == this.Type.TypeCode;
        if (this.RightOperand.CouldBeInterpretedAsNegativeSignedInteger)
          return this.LeftOperand.ValueIsPolymorphicCompileTimeConstant || this.RightOperand.Type.TypeCode == this.Type.TypeCode;
        return false;
      }
    }

    /// <summary>
    /// Returns an error message stating that the operands of this operation are not of the right type for the operator.
    /// </summary>
    protected AstErrorMessage GetBinaryBadOperandsTypeErrorMessage() {
      string leftOperandTypeName = this.Helper.GetTypeName(this.LeftOperand.Type);
      string rightOperandTypeName = this.Helper.GetTypeName(this.RightOperand.Type);
      return new AstErrorMessage(this, Error.BadBinaryOperation, this.OperationSymbolForErrorMessage, leftOperandTypeName, rightOperandTypeName);
    }

    /// <summary>
    /// Returns the string used to identify the operator in error messages
    /// </summary>
    protected abstract string OperationSymbolForErrorMessage { get; }

    /// <summary>
    /// Returns an enumeration of the methods in operators1 followed by the methods in operators2.
    /// </summary>
    /// <param name="operators1">An enumeration of methods corresponding to operators that have an operand of type this.LeftOperand.Type.</param>
    /// <param name="operators2">An enumeration of methods corresponding to operators that have an operand of type this.RightOperand.Type.</param>
    /// <returns></returns>
    protected IEnumerable<IMethodDefinition> GetCombinedOperators(IEnumerable<IMethodDefinition> operators1, IEnumerable<IMethodDefinition> operators2) {
      foreach (IMethodDefinition method in operators1) yield return method;
      foreach (IMethodDefinition method in operators2) yield return method;
    }

    /// <summary>
    /// Returns the name a method must have to overload this operator.
    /// </summary>
    protected abstract IName GetOperatorName();

    /// <summary>
    /// Returns an enumeration of methods that overload this operator. 
    /// If no user defined methods exists, it returns a list of dummy methods that correspond to operators built into IL.
    /// </summary>
    /// <param name="leftType">The type of the left hand operand.</param>
    /// <param name="rightType">The type of the right hand operand.</param>
    protected virtual IEnumerable<IMethodDefinition> GetOperatorMethods(ITypeDefinition leftType, ITypeDefinition rightType) {
      IEnumerable<IMethodDefinition> userOperators = this.GetUserDefinedOperators(leftType, rightType);
      if (userOperators.GetEnumerator().MoveNext()) return userOperators;
      return this.StandardOperators;
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      bool result = this.LeftOperand.HasErrors;
      result |= this.RightOperand.HasErrors;
      if (!result) {
        IMethodCall/*?*/ overloadCall = this.OverloadMethodCall;
        if (overloadCall == null) {
          result = true;
          this.Helper.ReportError(this.GetBinaryBadOperandsTypeErrorMessage());
        }
      }
      return result;
    }

    /// <summary>
    /// Checks if the expression has a side effect and reports an error unless told otherwise.
    /// </summary>
    /// <param name="reportError">If true, report an error if the expression has a side effect.</param>
    public override bool HasSideEffect(bool reportError) {
      bool result = this.LeftOperand.HasSideEffect(reportError);
      result |= this.RightOperand.HasSideEffect(reportError);
      return result;
    }

    /// <summary>
    /// If true, the left operand must be a target expression and the result of the binary operation is the
    /// value of the target expression before it is assigned the value of the operation performed on
    /// (right hand) values of the left and right operands.
    /// </summary>
    public bool ResultIsUnmodifiedLeftOperand {
      get { return false; }
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingExpression(Expression containingExpression) {
      base.SetContainingExpression(containingExpression);
      this.leftOperand.SetContainingExpression(this);
      this.rightOperand.SetContainingExpression(this);
    }

    /// <summary>
    /// A list of dummy methods that correspond to operations that are built into IL. The dummy methods are used, via overload resolution,
    /// to determine how the operands are to be converted before the operation is carried out.
    /// </summary>
    protected virtual IEnumerable<IMethodDefinition> StandardOperators {
      get {
        return Enumerable<IMethodDefinition>.Empty;
      }
    }

    /// <summary>
    /// Returns an enumeration of user defined methods that overload this operator. 
    /// </summary>
    /// <param name="leftType">The type of the left hand operand.</param>
    /// <param name="rightType">The type of the right hand operand.</param>
    protected virtual IEnumerable<IMethodDefinition> GetUserDefinedOperators(ITypeDefinition leftType, ITypeDefinition rightType) {
      if (TypeHelper.Type1DerivesFromOrIsTheSameAsType2(leftType, rightType)) return this.GetUserDefinedOperators(leftType);
      if (TypeHelper.Type1DerivesFromType2(rightType, leftType)) return this.GetUserDefinedOperators(rightType);
      IEnumerable<IMethodDefinition> nonDerivedLeftOperators = this.GetNonDerivedUserDefinedOperators(leftType);
      if (!nonDerivedLeftOperators.GetEnumerator().MoveNext()) {
        //Left type has no appplicable operators of its own. Try its base type.
        foreach (ITypeReference baseClassReference in leftType.BaseClasses)
          return this.GetUserDefinedOperators(baseClassReference.ResolvedType, rightType);
        //Left type has no base type, try the right type on its own.
        return this.GetUserDefinedOperators(rightType);
      }
      IEnumerable<IMethodDefinition>/*?*/ derivedAndRightOperators = null;
      foreach (ITypeReference baseClassReference in leftType.BaseClasses) {
        derivedAndRightOperators = this.GetUserDefinedOperators(baseClassReference.ResolvedType, rightType);
        break;
      }
      if (derivedAndRightOperators == null) {
        //Left type has no base type, try the right type on its own.
        derivedAndRightOperators = this.GetUserDefinedOperators(rightType);
      }
      if (!derivedAndRightOperators.GetEnumerator().MoveNext())
        return nonDerivedLeftOperators;
      return this.GetCombinedOperators(nonDerivedLeftOperators, derivedAndRightOperators);
    }

    /// <summary>
    /// Returns an enumeration of user defined methods that overload this operator, defined by the given type or one of its base classes.
    /// </summary>
    /// <param name="type">The type whose user defined operator methods should be searched.</param>
    protected virtual IEnumerable<IMethodDefinition> GetUserDefinedOperators(ITypeDefinition type) {
      IEnumerable<IMethodDefinition> operatorMethods = this.GetNonDerivedUserDefinedOperators(type);
      if (!operatorMethods.GetEnumerator().MoveNext()) {
        foreach (ITypeReference baseClassReference in type.BaseClasses)
          return this.GetUserDefinedOperators(baseClassReference.ResolvedType);
      }
      return operatorMethods;
    }

    /// <summary>
    /// Returns an enumeration of user defined methods that overload this operator, defined directly by the given type.
    /// </summary>
    /// <param name="type">The type whose user defined operator methods should be searched.</param>
    protected virtual IEnumerable<IMethodDefinition> GetNonDerivedUserDefinedOperators(ITypeDefinition type) {
      type = this.Helper.RemoveNullableWrapper(type);
      foreach (ITypeDefinitionMember member in type.GetMembersNamed(this.GetOperatorName(), false)) {
        IMethodDefinition/*?*/ method = member as IMethodDefinition;
        if (method == null || !method.IsStatic) continue;
        IEnumerator<IParameterDefinition> paramEnumerator = method.Parameters.GetEnumerator();
        if (!paramEnumerator.MoveNext()) continue;
        if (!this.Helper.ImplicitConversionExists(this.leftOperand, paramEnumerator.Current.Type.ResolvedType)) continue;
        if (!paramEnumerator.MoveNext()) continue;
        if (!this.Helper.ImplicitConversionExists(this.rightOperand, paramEnumerator.Current.Type.ResolvedType)) continue;
        if (paramEnumerator.MoveNext()) continue;
        yield return method;
      }
    }

    /// <summary>
    /// Infers the type of value that this expression will evaluate to. At runtime the actual value may be an instance of subclass of the result of this method.
    /// Calling this method does not cache the computed value and does not generate any error messages. In some cases, such as references to the parameters of lambda
    /// expressions during type overload resolution, the value returned by this method may be different from one call to the next.
    /// When type inference fails, Dummy.Type is returned.
    /// </summary>
    public override ITypeDefinition InferType() {
      MethodCall/*?*/ overloadCall = this.OverloadMethodCall;
      if (overloadCall != null) return overloadCall.Type;
      return Dummy.Type;
    }

    /// <summary>
    /// The left operand.
    /// </summary>
    public Expression LeftOperand {
      get { return this.leftOperand; }
    }
    readonly Expression leftOperand;

    /// <summary>
    /// Returns the user defined operator overload method, or a dummy method corresponding to an IL operation, that best
    /// matches the operand types of this operation.
    /// </summary>
    protected virtual IMethodDefinition LookForOverloadMethod() {
      ITypeDefinition leftType = this.leftOperand.Type;
      ITypeDefinition rightType = this.rightOperand.Type;
      IEnumerable<IMethodDefinition> candidateOperators = this.GetOperatorMethods(leftType, rightType);
      IMethodDefinition result = this.Helper.ResolveOverload(candidateOperators, this.leftOperand, this.rightOperand);
      if (this.Helper.StandardBinaryOperatorOverloadIsUndesirable(result, this.leftOperand, this.rightOperand)) return Dummy.Method;
      return result;
    }

    /// <summary>
    /// An expression that calls the user defined operator overload method that best matches the operand types of this operation.
    /// If no such method can be found, the value of this property is null.
    /// </summary>
    public MethodCall/*?*/ OverloadMethodCall {
      get {
        if (this.overloadMethodCall == null) {
          lock (GlobalLock.LockingObject) {
            if (this.overloadMethodCall == null) {
              IMethodDefinition overloadMethod = this.LookForOverloadMethod();
              this.overloadMethodCall = this.CreateOverloadMethodCall(overloadMethod);
            }
          }
        }
        //^ assert this.overloadMethodCall != null;
        if (this.overloadMethodCall is DummyMethodCall) return null;
        return this.overloadMethodCall;
      }
    }
    MethodCall/*?*/ overloadMethodCall;

    /// <summary>
    /// Returns a method call object that calls the given overloadMethod with this.LeftOperand and this.RightOperands as arguments.
    /// The operands are converted to the corresponding parameter types using implicit conversions.
    /// If overloadMethod is the Dummy.Method a DummyMethodCall is returned.
    /// </summary>
    /// <param name="overloadMethod">A user defined operator overload method or a "builtin" operator overload method, or a dummy method.
    /// The latter can be supplied when the expression is in error because one or both of the arguments cannot be converted the correct parameter type for a valid overload.</param>
    /// <returns></returns>
    protected virtual MethodCall CreateOverloadMethodCall(IMethodDefinition overloadMethod) {
      if (overloadMethod is Dummy)
        return new DummyMethodCall(this);
      else {
        List<Expression> args = new List<Expression>(2);
        args.Add(this.LeftOperand);
        args.Add(this.RightOperand);
        args = this.Helper.ConvertArguments(this, args, overloadMethod.Parameters);
        ResolvedMethodCall overloadMethodCall = new ResolvedMethodCall(overloadMethod, args, this.SourceLocation);
        overloadMethodCall.SetContainingExpression(this);
        return overloadMethodCall;
      }
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      if (!this.HasErrors) {
        MethodCall/*?*/ overloadedMethodCall = this.OverloadMethodCall;
        if (overloadedMethodCall != null) {
          if (!(overloadedMethodCall.ResolvedMethod is BuiltinMethodDefinition))
            return overloadedMethodCall;
        }
      }
      return this;
    }

    /// <summary>
    /// The right operand.
    /// </summary>
    public Expression RightOperand {
      get { return this.rightOperand; }
    }
    readonly Expression rightOperand;

    /// <summary>
    /// The type of value that the expression will evaluate to, as determined at compile time.
    /// </summary>
    public sealed override ITypeDefinition Type {
      get {
        if (this.type == null)
          this.type = this.InferType();
        return this.type;
      }
    }
    ITypeDefinition/*?*/ type;

    /// <summary>
    /// Returns true if the expression represents a compile time constant without an explicitly specified type. For example, 1 rather than 1L.
    /// Constant expressions such as 2*16 are polymorhpic if both operands are polymorhic.
    /// </summary>
    public override bool ValueIsPolymorphicCompileTimeConstant {
      get {
        return this.Value != null && this.LeftOperand.ValueIsPolymorphicCompileTimeConstant && this.RightOperand.ValueIsPolymorphicCompileTimeConstant;
      }
    }

    #region IBinaryOperation Members

    IExpression IBinaryOperation.LeftOperand {
      get { return this.ConvertedLeftOperand.ProjectAsIExpression(); }
    }

    IExpression IBinaryOperation.RightOperand {
      get { return this.ConvertedRightOperand.ProjectAsIExpression(); }
    }

    #endregion

    #region IExpression Members

    ITypeReference IExpression.Type {
      get { return this.Type; }
    }

    #endregion
  }

  /// <summary>
  /// A binary operation performed on a left and right operand, with the result being assigned to the left operand, which must be a target expression.
  /// </summary>
  public abstract class BinaryOperationAssignment : Expression {

    /// <summary>
    /// Allocates a binary operation performed on a left and right operand, with the result being assigned to the left operand, which must be a target expression.
    /// </summary>
    /// <param name="leftOperand">The left operand and target of the assignment.</param>
    /// <param name="rightOperand">The right operand.</param>
    /// <param name="sourceLocation">The source location of the operation.</param>
    protected BinaryOperationAssignment(TargetExpression leftOperand, Expression rightOperand, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.leftOperand = leftOperand;
      this.rightOperand = rightOperand;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected BinaryOperationAssignment(BlockStatement containingBlock, BinaryOperationAssignment template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.leftOperand = (TargetExpression)template.leftOperand.MakeCopyFor(containingBlock);
      this.rightOperand = template.rightOperand.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      if (this.LeftOperand.HasErrors || this.RightOperand.HasErrors) return true;
      var projectedExpression = this.ProjectAsIExpression();
      return projectedExpression is DummyExpression || projectedExpression == CodeDummy.Expression;
    }

    /// <summary>
    /// Checks if the expression has a side effect and reports an error unless told otherwise.
    /// </summary>
    /// <param name="reportError">If true, report an error if the expression has a side effect.</param>
    public override bool HasSideEffect(bool reportError) {
      if (this.hasSideEffect == null) {
        if (reportError) {
          this.Helper.ReportError(new AstErrorMessage(this, Error.ExpressionHasSideEffect));
          this.hasSideEffect = true;
        }
        return true;
      }
      return this.hasSideEffect.Value;
    }
    bool? hasSideEffect;

    /// <summary>
    /// The left operand and assignment target.
    /// </summary>
    public TargetExpression LeftOperand {
      get { return this.leftOperand; }
    }
    readonly TargetExpression leftOperand;

    /// <summary>
    /// Creates a binary expression with the given left operand and this.RightOperand, using the appropriate operator for this expression.
    /// The method does not use this.LeftOperand.Expression, since it may be necessary to factor out any subexpressions so that
    /// they are evaluated only once. The given left operand expression is expected to be the expression that remains after factoring.
    /// </summary>
    /// <param name="leftOperand">An expression to combine with this.RightOperand into a binary expression.</param>
    protected abstract Expression CreateBinaryExpression(Expression leftOperand);

    /// <summary>
    /// Creates a binary expression with the given left operand and this.RightOperand, using the appropriate operator for this expression.
    /// The method does not use this.LeftOperand.Expression, since it may be necessary to factor out any subexpressions so that
    /// they are evaluated only once. The given left operand expression is expected to be the expression that remains after factoring.
    /// If the resulting expression involves a predefined operator and results in a type that is not implicitly convertible to leftOperand.Type
    /// but this.RightOperand.Type is implicitly convertible to leftOperand.Type and there is an explicit conversion from the resulting value to 
    /// leftOperand.Type, then the resulting expression is first wrapped with an explicit conversion to leftOperand.Type.
    /// </summary>
    /// <param name="leftOperand">An expression to combine with this.RightOperand into a binary expression.</param>
    /// <returns></returns>
    protected virtual Expression CreateConvertedBinaryExpression(Expression leftOperand) {
      Expression result = this.CreateBinaryExpression(leftOperand);
      if (!(result is BinaryOperation)) return result; //leave overloaded operators alone
      if (!this.Helper.ImplicitConversionExists(result.Type, leftOperand.Type)) {
        if (result is LeftShift || result is RightShift || this.Helper.ImplicitConversionExists(this.RightOperand.Type, leftOperand.Type)) {
          Expression convertedExpression = this.Helper.ExplicitConversion(result, leftOperand.Type);
          if (!(convertedExpression is DummyExpression)) return convertedExpression;
        }
      }
      return result;
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected sealed override IExpression ProjectAsNonConstantIExpression() {
      if (this.cachedProjection != null) return this.cachedProjection;
      Expression factored = this.LeftOperand.Expression.FactoredExpression();
      if (factored == this.LeftOperand.Expression) {
        Assignment result = new Assignment(this.LeftOperand, this.CreateConvertedBinaryExpression(factored), this.SourceLocation);
        result.SetContainingExpression(this);
        return this.cachedProjection = result.ProjectAsIExpression();
      } else {
        //^ assume factored is BlockExpression; //the post condition of FactoredExpression says so
        BlockExpression be = (BlockExpression)factored;
        TargetExpression target = new TargetExpression(be.Expression);
        Assignment assignment = new Assignment(target, this.CreateConvertedBinaryExpression(be.Expression), this.SourceLocation);
        assignment.SetContainingExpression(be);
        be.expression = assignment;
        return this.cachedProjection = be.ProjectAsIExpression();
      }
    }
    IExpression/*?*/ cachedProjection;

    /// <summary>
    /// The right operand.
    /// </summary>
    public Expression RightOperand {
      get { return this.rightOperand; }
    }
    readonly Expression rightOperand;

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingExpression(Expression containingExpression) {
      base.SetContainingExpression(containingExpression);
      this.LeftOperand.SetContainingExpression(this);
      this.RightOperand.SetContainingExpression(this);
    }

    /// <summary>
    /// The type of value that the expression will evaluate to, as determined at compile time.
    /// </summary>
    public override ITypeDefinition Type {
      get { return this.LeftOperand.Type; }
    }

  }

  /// <summary>
  /// An expression that computes the bitwise and of the left and right operands. 
  /// When the operator is overloaded, this expression corresponds to a call to op_BitwiseAnd.
  /// </summary>
  public class BitwiseAnd : BinaryOperation, IBitwiseAnd {

    /// <summary>
    /// Allocates an expression that computes the bitwise and of the left and right operands. 
    /// When the operator is overloaded, this expression corresponds to a call to op_BitwiseAnd.
    /// </summary>
    /// <param name="leftOperand">The left operand.</param>
    /// <param name="rightOperand">The right operand.</param>
    /// <param name="sourceLocation">The source location of the operation.</param>
    public BitwiseAnd(Expression leftOperand, Expression rightOperand, ISourceLocation sourceLocation)
      : base(leftOperand, rightOperand, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected BitwiseAnd(BlockStatement containingBlock, BitwiseAnd template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Calls the visitor.Visit(IBitwiseAnd) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(BitwiseAnd) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Returns the string used to identify the operator in error messages
    /// </summary>
    protected override string OperationSymbolForErrorMessage {
      get { return "&"; }
    }

    /// <summary>
    /// Returns the name a method must have to overload this operator.
    /// </summary>
    protected override IName GetOperatorName() {
      return this.NameTable.OpBitwiseAnd;
    }

    /// <summary>
    /// Computes the compile time value of the expression. Can be null.
    /// </summary>
    protected override object/*?*/ GetValue() {
      object/*?*/ left = this.ConvertedLeftOperand.Value;
      object/*?*/ right = this.ConvertedRightOperand.Value;
      if (left == null || right == null) return null;
      switch (System.Convert.GetTypeCode(left)) {
        case TypeCode.Int32:
          //^ assume left is int && right is int;
          return (int)left & (int)right;
        case TypeCode.UInt32:
          //^ assume left is uint && right is uint;
          return (uint)left & (uint)right;
        case TypeCode.Int64:
          //^ assume left is long && right is long;
          return (long)left & (long)right;
        case TypeCode.UInt64:
          //^ assume left is ulong && right is ulong;
          return (ulong)left & (ulong)right;
        case TypeCode.Boolean:
          //^ assume left is bool && right is bool;
          return (bool)left & (bool)right;
      }
      return null;
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new BitwiseAnd(containingBlock, this);
    }

    /// <summary>
    /// Returns true if no information is lost if the integer value of this expression is converted to the target integer type.
    /// </summary>
    public override bool IntegerConversionIsLossless(ITypeDefinition targetType) {
      if (!TypeHelper.IsPrimitiveInteger(this.LeftOperand.Type) || !TypeHelper.IsPrimitiveInteger(this.RightOperand.Type)) return false;
      return this.LeftOperand.IntegerConversionIsLossless(targetType) || this.RightOperand.IntegerConversionIsLossless(targetType);
    }

    /// <summary>
    /// A list of dummy methods that correspond to operations that are built into IL. The dummy methods are used, via overload resolution,
    /// to determine how the operands are to be converted before the operation is carried out.
    /// </summary>
    protected override IEnumerable<IMethodDefinition> StandardOperators {
      get {
        BuiltinMethods dummyMethods = this.Compilation.BuiltinMethods;
        yield return dummyMethods.Int32opInt32;
        yield return dummyMethods.UInt32opUInt32;
        yield return dummyMethods.Int64opInt64;
        yield return dummyMethods.UInt64opUInt64;
        ITypeDefinition leftOperandType = this.LeftOperand.Type;
        ITypeDefinition rightOperandType = this.RightOperand.Type;
        if (leftOperandType.IsEnum) {
          yield return dummyMethods.GetDummyEnumOpEnum(leftOperandType);
        } else if (rightOperandType.IsEnum)
          yield return dummyMethods.GetDummyEnumOpEnum(rightOperandType);
        yield return dummyMethods.BoolOpBool;
      }
    }

  }

  /// <summary>
  /// An expression that computes the bitwise and of the left and right operands.
  /// The result of the expression is assigned to the left operand, which must be a target expression.
  /// When the operator is overloaded, this expression corresponds to a call to op_BitwiseAnd.
  /// </summary>
  public class BitwiseAndAssignment : BinaryOperationAssignment {

    /// <summary>
    /// Allocates an expression that computes the bitwise and of the left and right operands.
    /// The result of the expression is assigned to the left operand, which must be a target expression.
    /// When the operator is overloaded, this expression corresponds to a call to op_BitwiseAnd.
    /// </summary>
    /// <param name="leftOperand">The left operand and target of the assignment.</param>
    /// <param name="rightOperand">The right operand.</param>
    /// <param name="sourceLocation">The source location of the operation.</param>
    public BitwiseAndAssignment(TargetExpression leftOperand, Expression rightOperand, ISourceLocation sourceLocation)
      : base(leftOperand, rightOperand, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected BitwiseAndAssignment(BlockStatement containingBlock, BitwiseAndAssignment template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Calls the visitor.Visit(BitwiseAndAssignment) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (this.ContainingBlock == containingBlock) return this;
      return new BitwiseAndAssignment(containingBlock, this);
    }

    /// <summary>
    /// Creates a bitwise and expression using the given left Operand and this.RightOperand as the two operands.
    /// The method does not use this.LeftOperand.Expression, since it may be necessary to factor out any subexpressions so that
    /// they are evaluated only once. The given left operand expression is expected to be the expression that remains after factoring.
    /// </summary>
    /// <param name="leftOperand">An expression to combine with this.RightOperand into a binary expression.</param>
    protected override Expression CreateBinaryExpression(Expression leftOperand) {
      Expression result = new BitwiseAnd(leftOperand, this.RightOperand, this.SourceLocation);
      result.SetContainingExpression(this);
      return result;
    }
  }

  /// <summary>
  /// An expression that computes the bitwise or of the left and right operands. 
  /// When the operator is overloaded, this expression corresponds to a call to op_BitwiseOr.
  /// </summary>
  public class BitwiseOr : BinaryOperation, IBitwiseOr {

    /// <summary>
    /// Allocates an expression that computes the bitwise or of the left and right operands. 
    /// When the operator is overloaded, this expression corresponds to a call to op_BitwiseOr.
    /// </summary>
    /// <param name="leftOperand">The left operand.</param>
    /// <param name="rightOperand">The right operand.</param>
    /// <param name="sourceLocation">The source location of the operation.</param>
    public BitwiseOr(Expression leftOperand, Expression rightOperand, ISourceLocation sourceLocation)
      : base(leftOperand, rightOperand, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected BitwiseOr(BlockStatement containingBlock, BitwiseOr template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Calls the visitor.Visit(IBitwiseOr) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(BitwiseOr) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Returns the string used to identify the operator in error messages
    /// </summary>
    protected override string OperationSymbolForErrorMessage {
      get { return "|"; }
    }


    /// <summary>
    /// Returns the name a method must have to overload this operator.
    /// </summary>
    protected override IName GetOperatorName() {
      return this.NameTable.OpBitwiseOr;
    }

    /// <summary>
    /// Computes the compile time value of the expression. Can be null.
    /// </summary>
    protected override object/*?*/ GetValue() {
      object/*?*/ left = this.ConvertedLeftOperand.Value;
      object/*?*/ right = this.ConvertedRightOperand.Value;
      if (left == null || right == null) return null;
      switch (System.Convert.GetTypeCode(left)) {
        case TypeCode.Int32:
          //^ assume left is int && right is int;
          return (int)left | (int)right;
        case TypeCode.UInt32:
          //^ assume left is uint && right is uint;
          return (uint)left | (uint)right;
        case TypeCode.Int64:
          //^ assume left is long && right is long;
          return (long)left | (long)right;
        case TypeCode.UInt64:
          //^ assume left is ulong && right is ulong;
          return (ulong)left | (ulong)right;
        case TypeCode.Boolean:
          //^ assume left is bool && right is bool;
          return (bool)left | (bool)right;
      }
      return null;
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new BitwiseOr(containingBlock, this);
    }

    /// <summary>
    /// Returns true if no information is lost if the integer value of this expression is converted to the target integer type.
    /// </summary>
    public override bool IntegerConversionIsLossless(ITypeDefinition targetType) {
      if (!TypeHelper.IsPrimitiveInteger(this.LeftOperand.Type) || !TypeHelper.IsPrimitiveInteger(this.RightOperand.Type)) return false;
      return this.LeftOperand.IntegerConversionIsLossless(targetType) && this.RightOperand.IntegerConversionIsLossless(targetType);
    }

    /// <summary>
    /// A list of dummy methods that correspond to operations that are built into IL. The dummy methods are used, via overload resolution,
    /// to determine how the operands are to be converted before the operation is carried out.
    /// </summary>
    protected override IEnumerable<IMethodDefinition> StandardOperators {
      get {
        BuiltinMethods dummyMethods = this.Compilation.BuiltinMethods;
        yield return dummyMethods.Int32opInt32;
        yield return dummyMethods.UInt32opUInt32;
        yield return dummyMethods.Int64opInt64;
        yield return dummyMethods.UInt64opUInt64;
        ITypeDefinition leftOperandType = this.LeftOperand.Type;
        ITypeDefinition rightOperandType = this.RightOperand.Type;
        if (leftOperandType.IsEnum) {
          yield return dummyMethods.GetDummyEnumOpEnum(leftOperandType);
        } else if (rightOperandType.IsEnum)
          yield return dummyMethods.GetDummyEnumOpEnum(rightOperandType);
        yield return dummyMethods.BoolOpBool;
      }
    }

  }

  /// <summary>
  /// An expression that computes the bitwise or of the left and right operands. 
  /// The result of the expression is assigned to the left operand, which must be a target expression.
  /// When the operator is overloaded, this expression corresponds to a call to op_BitwiseOr.
  /// </summary>
  public class BitwiseOrAssignment : BinaryOperationAssignment {

    /// <summary>
    /// An expression that computes the bitwise or of the left and right operands. 
    /// The result of the expression is assigned to the left operand, which must be a target expression.
    /// When the operator is overloaded, this expression corresponds to a call to op_BitwiseOr.
    /// </summary>
    /// <param name="leftOperand">The left operand and target of the assignment.</param>
    /// <param name="rightOperand">The right operand.</param>
    /// <param name="sourceLocation">The source location of the operation.</param>
    public BitwiseOrAssignment(TargetExpression leftOperand, Expression rightOperand, ISourceLocation sourceLocation)
      : base(leftOperand, rightOperand, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected BitwiseOrAssignment(BlockStatement containingBlock, BitwiseOrAssignment template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Calls the visitor.Visit(BitwiseOrAssignment) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (this.ContainingBlock == containingBlock) return this;
      return new BitwiseOrAssignment(containingBlock, this);
    }

    /// <summary>
    /// Creates a bitwise or expression using the given left Operand and this.RightOperand as the two operands.
    /// The method does not use this.LeftOperand.Expression, since it may be necessary to factor out any subexpressions so that
    /// they are evaluated only once. The given left operand expression is expected to be the expression that remains after factoring.
    /// </summary>
    /// <param name="leftOperand">An expression to combine with this.RightOperand into a binary expression.</param>
    protected override Expression CreateBinaryExpression(Expression leftOperand) {
      Expression result = new BitwiseOr(leftOperand, this.RightOperand, this.SourceLocation);
      result.SetContainingExpression(this);
      return result;
    }
  }

  /// <summary>
  /// An expression that introduces a new block scope and that references local variables
  /// that are defined and initialized by embedded statements when control reaches the expression.
  /// </summary>
  public class BlockExpression : Expression, IBlockExpression {

    /// <summary>
    /// Allocates an expression that introduces a new block scope and that references local variables
    /// that are defined and initialized by embedded statements when control reaches the expression.
    /// </summary>
    /// <param name="block">
    /// A block of statements that typically introduce local variables to hold sub expressions.
    /// The scope of these declarations coincides with the block expression. 
    /// The statements are executed before evaluation of Expression occurs.
    /// </param>
    /// <param name="expression">The expression that computes the result of the entire block expression.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public BlockExpression(BlockStatement block, Expression expression, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.blockStatement = block;
      this.expression = expression;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected BlockExpression(BlockStatement containingBlock, BlockExpression template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.blockStatement = (BlockStatement)template.BlockStatement.MakeCopyFor(containingBlock);
      this.expression = template.Expression.MakeCopyFor(this.blockStatement);
    }

    /// <summary>
    /// A block of statements that typically introduce local variables to hold sub expressions.
    /// The scope of these declarations coincides with the block expression. 
    /// The statements are executed before evaluation of Expression occurs.
    /// </summary>
    public BlockStatement BlockStatement {
      get { return this.blockStatement; }
    }
    readonly BlockStatement blockStatement;

    /// <summary>
    /// Calls the visitor.Visit(IBlockExpression) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(BlockExpression) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The expression that computes the result of the entire block expression.
    /// This expression can contain references to the local variables that are declared inside BlockStatement.
    /// </summary>
    public Expression Expression {
      get { return this.expression; }
    }
    internal Expression expression;

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new BlockExpression(containingBlock, this);
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// </summary>
    /// <returns></returns>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return this.Expression.HasErrors;
    }

    /// <summary>
    /// Checks if the expression has a side effect and reports an error unless told otherwise.
    /// </summary>
    /// <param name="reportError">If true, report an error if the expression has a side effect.</param>
    /// <returns></returns>
    public override bool HasSideEffect(bool reportError) {
      return this.Expression.HasSideEffect(reportError);
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      return this;
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingExpression(Expression containingExpression) {
      base.SetContainingExpression(containingExpression);
      this.BlockStatement.SetContainingBlock(containingExpression.ContainingBlock);
      this.Expression.SetContainingExpression(new DummyExpression(this.BlockStatement, SourceDummy.SourceLocation));
    }

    /// <summary>
    /// The type of value that the expression will evaluate to, as determined at compile time.
    /// </summary>
    public override ITypeDefinition Type {
      get { return this.Expression.Type; }
    }

    #region IBlockExpression Members

    IBlockStatement IBlockExpression.BlockStatement {
      get { return this.BlockStatement; }
    }

    IExpression IBlockExpression.Expression {
      get { return this.Expression.ProjectAsIExpression(); }
    }

    #endregion


    #region IExpression Members

    ITypeReference IExpression.Type {
      get { return this.Type; }
    }

    #endregion
  }

  /// <summary>
  /// An expression that binds to a local variable, parameter or field.
  /// </summary>
  public class BoundExpression : Expression, IBoundExpression {

    /// <summary>
    /// Allocates an expression that binds to a local variable, parameter or field.
    /// </summary>
    /// <param name="expression">The expression that was bound.</param>
    /// <param name="definition">The local variable, parameter or field that this expression binds to.</param>
    public BoundExpression(Expression expression, object definition)
      : base(expression.SourceLocation)
      //^ requires definition is ILocalDefinition || definition is IParameterDefinition || definition is IFieldDefinition;
    {
      //TODO: figure out the alignment of the field and see if it matches the type of the field.
      this.expression = expression;
      this.definition = definition;
    }

    /// <summary>
    /// Allocates an expression that binds to a local variable, parameter or field.
    /// </summary>
    /// <param name="expression">The expression that was bound.</param>
    /// <param name="definition">The local variable, parameter or field that this expression binds to.</param>
    /// <param name="containingBlock"></param>
    public BoundExpression(Expression expression, object definition, BlockStatement containingBlock)
      : base(containingBlock, expression.SourceLocation)
      //^ requires definition is ILocalDefinition || definition is IParameterDefinition || definition is IFieldDefinition || definition is IMethodDefinition;
    {
      this.expression = expression;
      this.definition = definition;
    }

    /// <summary>
    /// If Definition is a field and the field is not aligned with natural size of its type, this property specifies the actual alignment.
    /// For example, if the field is byte aligned, then the result of this property is 1. Likewise, 2 for word (16-bit) alignment and 4 for
    /// double word (32-bit alignment). 
    /// </summary>
    public byte Alignment {
      get
        //^^ requires IsUnaligned;
        //^^ ensures result == 1 || result == 2 || result == 4;
      {
        //^ assume false; //TODO: need some work here
        return this.alignment;
      }
    }
    byte alignment = 0;

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      IFieldDefinition field = this.definition as IFieldDefinition;
      if (field != null && field is Dummy) return true;
      return false;
    }

    /// <summary>
    /// The local variable, parameter or field that this expression binds to.
    /// </summary>
    public object Definition {
      get
        //^ ensures result is ILocalDefinition || result is IParameterDefinition || result is IFieldDefinition;
      {
        return this.definition;
      }
    }
    object definition;
    //^ invariant definition is ILocalDefinition || definition is IParameterDefinition || definition is IFieldDefinition;

    /// <summary>
    /// Calls the visitor.Visit(IBoundExpression) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The expression that was bound.
    /// </summary>
    Expression expression;

    /// <summary>
    /// Infers the type of value that this expression will evaluate to. At runtime the actual value may be an instance of subclass of the result of this method.
    /// Calling this method does not cache the computed value and does not generate any error messages. In some cases, such as references to the parameters of lambda
    /// expressions during type overload resolution, the value returned by this method may be different from one call to the next.
    /// When type inference fails, Dummy.Type is returned.
    /// </summary>
    public override ITypeDefinition InferType() {
      object definition = this.Definition;
      ILocalDefinition/*?*/ local = definition as ILocalDefinition;
      if (local != null) return local.Type.ResolvedType;
      IParameterDefinition/*?*/ parameter = definition as IParameterDefinition;
      if (parameter != null) {
        if (parameter.IsByReference) return ManagedPointerType.GetManagedPointerType(parameter.Type, this.Compilation.HostEnvironment.InternFactory);
        return parameter.Type.ResolvedType;
      }
      IFieldDefinition/*?*/ field = definition as IFieldDefinition;
      if (field != null) {
        if (field.ContainingTypeDefinition.IsEnum && field.IsCompileTimeConstant) return field.ContainingTypeDefinition;
        return field.Type.ResolvedType;
      }
      return Dummy.Type;
    }

    /// <summary>
    /// If the expression binds to an instance field then this property is not null and contains the instance.
    /// </summary>
    public IExpression/*?*/ Instance {
      get {
        if (this.cachedInstance == null) {
          IFieldDefinition/*?*/ field = this.definition as IFieldDefinition;
          if (field != null && !field.IsStatic) {
            QualifiedName/*?*/ qualName = this.expression as QualifiedName;
            if (qualName != null) {
              Expression/*?*/ instance = qualName.Instance;
              if (instance == null) return null;
              this.cachedInstance = instance.ProjectAsIExpression();
            } else
              this.cachedInstance = new ThisReference(this.expression.ContainingBlock, this.expression.SourceLocation);
          } else
            this.cachedInstance = this;
        }
        if (this.cachedInstance == this) return null;
        return this.cachedInstance;
      }
    }
    IExpression/*?*/ cachedInstance;

    /// <summary>
    /// True if the definition is a field and the field is not aligned with the natural size of its type.
    /// For example if the field type is Int32 and the field is aligned on an Int16 boundary.
    /// </summary>
    public bool IsUnaligned {
      get { return this.alignment != 0; }
    }

    /// <summary>
    /// The bound Definition is a volatile field and its contents may not be cached.
    /// </summary>
    public bool IsVolatile {
      get {
        IFieldDefinition field = this.Definition as IFieldDefinition;
        return field != null && MemberHelper.IsVolatile(field);
      }
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      //^ assume false; //This node should never be part of a source AST.
      return this;
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      return this;
    }

    /// <summary>
    /// The type of value that the expression will evaluate to, as determined at compile time.
    /// </summary>
    public sealed override ITypeDefinition Type {
      get {
        if (this.type == null)
          this.type = this.InferType();
        return this.type;
      }
    }
    //^ [Once]
    ITypeDefinition/*?*/ type;


    #region IExpression Members

    ITypeReference IExpression.Type {
      get { return this.Type; }
    }

    #endregion
  }

  /// <summary>
  /// An expression that casts the value to the given type or throws an InvalidCastException.
  /// </summary>
  public class Cast : Expression {

    /// <summary>
    /// Allocates an expression that casts the value to the given type or throws an InvalidCastException.
    /// </summary>
    /// <param name="valueToCast">The value to cast if possible.</param>
    /// <param name="targetType">The type to which the value must be cast. If the value is not of this type, the expression results in a null value of this type.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public Cast(Expression valueToCast, TypeExpression targetType, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.valueToCast = valueToCast;
      this.targetType = targetType;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected Cast(BlockStatement containingBlock, Cast template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.valueToCast = template.valueToCast.MakeCopyFor(containingBlock);
      this.targetType = (TypeExpression)template.targetType.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return this.GetConversion().HasErrors;
    }

    /// <summary>
    /// Calls the visitor.Visit(Cast) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Allocates, caches and returns an expression that will convert this.ValueToCast to a value of type this.TargetType.
    /// </summary>
    private Expression GetConversion() {
      if (this.conversion == null) {
        if (this.ValueToCast.HasErrors || this.TargetType.HasErrors)
          this.conversion = new DummyExpression(this.SourceLocation);
        else {
          this.conversion = this.Helper.ExplicitConversion(this.ValueToCast, this.TargetType.ResolvedType, this.SourceLocation);
          if (!this.ValueToCast.HasErrors && !this.TargetType.HasErrors && this.conversion.HasErrors) {
            string sourceTypeName = this.Helper.GetTypeName(this.ValueToCast.Type);
            string targetTypeName = this.Helper.GetTypeName(this.TargetType.ResolvedType);
            if (this.Helper.ExpressionIsNumericLiteral(this.ValueToCast)) {
              //^ assert this.ValueToCast.Value != null;
              if (this.ContainingBlock.UseCheckedArithmetic)
                this.Helper.ReportError(new AstErrorMessage(this, Error.ConstOutOfRangeChecked, String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}", this.ValueToCast.Value), targetTypeName));
              else
                this.Helper.ReportError(new AstErrorMessage(this, Error.ConstOutOfRange, String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}", this.ValueToCast.Value), targetTypeName));
            } else {
              this.Helper.ReportError(new AstErrorMessage(this, Error.NoExplicitConversion, sourceTypeName, targetTypeName));
            }
          }
        }
      }
      return this.conversion;
    }
    private Expression/*?*/ conversion;

    /// <summary>
    /// Computes the compile time value of the expression. Can be null.
    /// </summary>
    protected override object/*?*/ GetValue() {
      return this.GetConversion().Value;
    }

    /// <summary>
    /// Checks if the expression has a side effect and reports an error unless told otherwise.
    /// </summary>
    /// <param name="reportError">If true, report an error if the expression has a side effect.</param>
    public override bool HasSideEffect(bool reportError) {
      return this.ValueToCast.HasSideEffect(reportError);
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new Cast(containingBlock, this);
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      return this.GetConversion().ProjectAsIExpression();
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingExpression(Expression containingExpression) {
      base.SetContainingExpression(containingExpression);
      this.targetType.SetContainingExpression(this);
      this.valueToCast.SetContainingExpression(this);
    }

    /// <summary>
    /// The type to which the value must be cast. If the value is not of this type, the expression results in a null value of this type.
    /// </summary>
    public TypeExpression TargetType {
      get { return this.targetType; }
    }
    readonly TypeExpression targetType;

    /// <summary>
    /// The type of value that the expression will evaluate to, as determined at compile time.
    /// </summary>
    public override ITypeDefinition Type {
      get { return this.targetType.ResolvedType; }
    }

    /// <summary>
    /// The value to cast if possible.
    /// </summary>
    public Expression ValueToCast {
      get { return this.valueToCast; }
    }
    readonly Expression valueToCast;

    /// <summary>
    /// Returns a string representation of the expression for debugging and logging uses.
    /// </summary>
    public override string ToString() {
      return "(" + this.Type.ToString() + ")" + this.ValueToCast.ToString();
    }
  }

  /// <summary>
  /// An expression that casts the value to the given type, resulting in a null value if the cast does not succeed.
  /// </summary>
  public class CastIfPossible : Expression, ICastIfPossible {

    /// <summary>
    /// Allocates an expression that casts the value to the given type, resulting in a null value if the cast does not succeed.
    /// </summary>
    /// <param name="valueToCast">The value to cast if possible.</param>
    /// <param name="targetType">The type to which the value must be cast. If the value is not of this type, the expression results in a null value of this type.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public CastIfPossible(Expression valueToCast, TypeExpression targetType, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.valueToCast = valueToCast;
      this.targetType = targetType;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected CastIfPossible(BlockStatement containingBlock, CastIfPossible template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.valueToCast = template.valueToCast.MakeCopyFor(containingBlock);
      this.targetType = (TypeExpression)template.targetType.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// Calls the visitor.Visit(ICastIfPossible) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(CastIfPossible) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new CastIfPossible(containingBlock, this);
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      return this;
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingExpression(Expression containingExpression) {
      base.SetContainingExpression(containingExpression);
      this.targetType.SetContainingExpression(this);
      this.valueToCast.SetContainingExpression(this);
    }

    /// <summary>
    /// The type to which the value must be cast. If the value is not of this type, the expression results in a null value of this type.
    /// </summary>
    public TypeExpression TargetType {
      get { return this.targetType; }
    }
    readonly TypeExpression targetType;

    /// <summary>
    /// The type of value that the expression will evaluate to, as determined at compile time.
    /// </summary>
    public override ITypeDefinition Type {
      get { return this.TargetType.ResolvedType; }
    }

    /// <summary>
    /// The value to cast if possible.
    /// </summary>
    public Expression ValueToCast {
      get { return this.valueToCast; }
    }
    readonly Expression valueToCast;

    #region ICastIfPossible Members

    IExpression ICastIfPossible.ValueToCast {
      get { return this.ValueToCast.ProjectAsIExpression(); }
    }

    ITypeReference ICastIfPossible.TargetType {
      get { return this.TargetType.ResolvedType; }
    }

    #endregion


    #region IExpression Members

    ITypeReference IExpression.Type {
      get { return this.Type; }
    }

    #endregion
  }

  /// <summary>
  /// An expression that invokes a different overload of the constructor containing this call.
  /// </summary>
  public class ChainedConstructorCall : MethodCall {

    /// <summary>
    /// Allocates an expression that invokes a different overload of the constructor containing this call.
    /// </summary>
    /// <param name="originalArguments"></param>
    /// <param name="sourceLocation"></param>
    public ChainedConstructorCall(IEnumerable<Expression> originalArguments, ISourceLocation sourceLocation)
      : base(new DummyExpression(sourceLocation), originalArguments, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected ChainedConstructorCall(BlockStatement containingBlock, MethodCall template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Returns a collection of methods that represent the constructors for declaring type of the constructor containing this call.
    /// </summary>
    /// <param name="allowMethodParameterInferencesToFail">This flag is ignored, since constructors cannot have generic parameters.</param>
    public override IEnumerable<IMethodDefinition> GetCandidateMethods(bool allowMethodParameterInferencesToFail) {
      TypeDeclaration/*?*/ declaringType = this.ContainingBlock.ContainingTypeDeclaration;
      if (declaringType == null) yield break;
      //^ assume declaringType != null;
      foreach (ITypeDefinitionMember member in declaringType.TypeDefinition.GetMembersNamed(this.NameTable.Ctor, false)) {
        IMethodDefinition/*?*/ meth = member as IMethodDefinition;
        if (meth != null && meth.IsSpecialName) yield return meth;
      }
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (this.ContainingBlock == containingBlock) return this;
      return new ChainedConstructorCall(containingBlock, this);
    }

    /// <summary>
    /// The this argument of the chained constructor.
    /// </summary>
    public override Expression ThisArgument {
      get
        //^^ requires !this.ResolvedMethod.IsStatic;
      {
        return new ThisReference(this.MethodExpression.ContainingBlock, this.MethodExpression.SourceLocation);
      }
    }

  }

  /// <summary>
  /// An expression that wraps an inner expression and causes the inner expression to be evaluated using arithmetic operators that check for overflow.
  /// </summary>
  public class CheckedExpression : Expression {

    /// <summary>
    /// Allocates an expression that wraps an inner expression and causes the inner expression to be evaluated using arithmetic operators that check for overflow.
    /// </summary>
    /// <param name="operand">The expression to evaluate while checking for arithmetic overflow.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public CheckedExpression(Expression operand, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.operand = operand;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected CheckedExpression(BlockStatement containingBlock, CheckedExpression template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.operand = template.operand.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return this.Operand.HasErrors;
    }

    /// <summary>
    /// True if the constant is a positive integer that could be interpreted as a negative signed integer.
    /// For example, 0x80000000, could be interpreted as a convenient way of writing int.MinValue.
    /// </summary>
    public override bool CouldBeInterpretedAsNegativeSignedInteger {
      get { return this.Operand.CouldBeInterpretedAsNegativeSignedInteger; }
    }

    /// <summary>
    /// Calls the visitor.Visit(CheckedExpression) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Computes the compile time value of the expression. Can be null.
    /// </summary>
    protected override object/*?*/ GetValue() {
      return this.Operand.Value;
    }

    /// <summary>
    /// Checks if the expression has a side effect and reports an error unless told otherwise.
    /// </summary>
    /// <param name="reportError">If true, report an error if the expression has a side effect.</param>
    public override bool HasSideEffect(bool reportError) {
      return this.Operand.HasSideEffect(reportError);
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      if (!containingBlock.UseCheckedArithmetic) {
        var dummyBlockStatement = BlockStatement.CreateDummyFor(BlockStatement.Options.UseCheckedArithmetic, this.SourceLocation);
        dummyBlockStatement.SetContainingBlock(containingBlock);
        containingBlock = dummyBlockStatement;
      }
      return new CheckedExpression(containingBlock, this);
    }

    /// <summary>
    /// The value on which the operation is performed.
    /// </summary>
    public Expression Operand {
      get { return this.operand; }
    }
    readonly Expression operand;

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      return this.Operand.ProjectAsIExpression();
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingExpression(Expression containingExpression) {
      BlockStatement dummyContainingBlock = BlockStatement.CreateDummyFor(BlockStatement.Options.UseCheckedArithmetic, this.SourceLocation);
      dummyContainingBlock.SetContainingBlock(containingExpression.ContainingBlock);
      base.SetContainingBlock(dummyContainingBlock);
      this.Operand.SetContainingExpression(this);
    }

    /// <summary>
    /// The type of value that the expression will evaluate to, as determined at compile time.
    /// </summary>
    public override ITypeDefinition Type {
      get { return this.Operand.Type; }
    }

    /// <summary>
    /// Returns true if the expression represents a compile time constant without an explicitly specified type. For example, 1 rather than 1L.
    /// Constant expressions such as 2*16 are polymorhpic if both operands are polymorhic.
    /// </summary>
    public override bool ValueIsPolymorphicCompileTimeConstant {
      get { return this.Operand.ValueIsPolymorphicCompileTimeConstant; }
    }

  }

  /// <summary>
  /// An expression that results in true if the given operand is an instance of the given type.
  /// </summary>
  public class CheckIfInstance : Expression, ICheckIfInstance {

    /// <summary>
    /// Allocates an expression that results in true if the given operand is an instance of the given type.
    /// </summary>
    /// <param name="operand">The value to check.</param>
    /// <param name="typeToCheck">The type to which the value must belong for this expression to evaluate as true.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public CheckIfInstance(Expression operand, TypeExpression typeToCheck, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.operand = operand;
      this.typeToCheck = typeToCheck;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected CheckIfInstance(BlockStatement containingBlock, CheckIfInstance template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.operand = template.operand.MakeCopyFor(containingBlock);
      this.typeToCheck = (TypeExpression)template.TypeToCheck.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// Calls the visitor.Visit(ICheckIfInstance) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(CheckIfInstance) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new CheckIfInstance(containingBlock, this);
    }

    /// <summary>
    /// The value to check.
    /// </summary>
    public Expression Operand {
      get { return this.operand; }
    }
    readonly Expression operand;

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      return this;
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingExpression(Expression containingExpression) {
      base.SetContainingExpression(containingExpression);
      this.Operand.SetContainingExpression(this);
      this.TypeToCheck.SetContainingExpression(this);
    }

    /// <summary>
    /// The type of value that the expression will evaluate to, as determined at compile time.
    /// </summary>
    public override ITypeDefinition Type {
      get { return this.PlatformType.SystemBoolean.ResolvedType; }
    }

    /// <summary>
    /// The type to which the value must belong for this expression to evaluate as true.
    /// </summary>
    public TypeExpression TypeToCheck {
      get { return this.typeToCheck; }
    }
    readonly TypeExpression typeToCheck;

    #region ICheckIfInstance Members

    IExpression ICheckIfInstance.Operand {
      get { return this.Operand.ProjectAsIExpression(); }
    }

    ITypeReference ICheckIfInstance.TypeToCheck {
      get { return this.TypeToCheck.ResolvedType; }
    }

    #endregion


    #region IExpression Members

    ITypeReference IExpression.Type {
      get { return this.Type; }
    }

    #endregion
  }

  /// <summary>
  /// The common base class for calls to constructors (new foo(..)), indexers (foo[..]) and method calls (foo(...)).
  /// </summary>
  public abstract class ConstructorIndexerOrMethodCall : Expression {

    /// <summary>
    /// Intializes the common base class for calls to constructors (new foo(..)), indexers (foo[..]) and method calls (foo(...)).
    /// </summary>
    /// <param name="arguments">The arguments to pass to the constructor, indexer or method</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    protected ConstructorIndexerOrMethodCall(IEnumerable<Expression> arguments, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.originalArguments = arguments;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected ConstructorIndexerOrMethodCall(BlockStatement containingBlock, ConstructorIndexerOrMethodCall template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.originalArguments = Expression.CopyExpressions(template.OriginalArguments, containingBlock);
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      bool result = false;
      // Before checking converted arguments we must ensure that resolvedMethod has 
      // successfully bound the methodExpression. If the method does not resolve, 
      // checking conversion of the arguments against Dummy.Method is meaningless.
      if (!(this.ResolvedMethod is Dummy))
        foreach (Expression arg in this.ConvertedArguments)
          result |= arg.HasErrors;
      result |= this.Type is Dummy;
      return result;
    }

    /// <summary>
    /// The arguments to pass to the constructor, indexer or method, after they have been converted to match the parameters of the resolved method.
    /// </summary>
    public IEnumerable<Expression> ConvertedArguments {
      get {
        if (this.convertedArguments == null)
          this.convertedArguments = this.ConvertArguments();
        return this.convertedArguments.AsReadOnly();
      }
    }
    List<Expression>/*?*/ convertedArguments;

    /// <summary>
    /// Returns a list of the arguments to pass to the constructor, indexer or method, after they have been converted to 
    /// match the parameters of the resolved method. This call uses ApplicableArguments in case this is an extension method call.
    /// </summary>
    protected virtual List<Expression> ConvertArguments() {
      return this.Helper.ConvertArguments(this, this.ApplicableArguments, this.ResolvedMethod.Parameters, false);
    }

    /// <summary>
    /// Returns a collection of methods that match the name of the method/indexer to call, or that represent the
    /// collection of constructors for the named type.
    /// </summary>
    /// <param name="allowMethodParameterInferencesToFail">If this flag is true, 
    /// generic methods should be included in the collection if their method parameter types could not be inferred from the argument types.</param>
    public abstract IEnumerable<IMethodDefinition> GetCandidateMethods(bool allowMethodParameterInferencesToFail);

    /// <summary>
    /// Returns a default value to pass as an argument corresponding the the given parameter.
    /// If the parameter does not specify the default value to use, an instance of DefaultValue is returned.
    /// </summary>
    /// <param name="par">The parameter for which a default argument value is needed.</param>
    protected virtual Expression GetDefaultValueFor(IParameterDefinition par) {
      if (par.HasDefaultValue) return new CompileTimeConstant(par.DefaultValue.Value, SourceDummy.SourceLocation);
      return new DefaultValue(TypeExpression.For(par.Type), SourceDummy.SourceLocation);
    }

    /// <summary>
    /// Returns an expression that collects the remaining argument expressions produced by the given enumerator into a parameter array.
    /// </summary>
    /// <param name="par">The parameter that corresponds to the parameter array.</param>
    /// <param name="args">An enumerator that will produce zero or more expressions that result in values that must become part of the argument array. May be null (empty).</param>
    protected virtual Expression GetParamArray(IParameterDefinition par, IEnumerator<Expression>/*?*/ args)
      //^ requires par.IsParameterArray;
    {
      SourceLocationBuilder slb;
      List<Expression> initializers = new List<Expression>();
      if (args != null) {
        slb = new SourceLocationBuilder(args.Current.SourceLocation);
        do { initializers.Add(args.Current); slb.UpdateToSpan(args.Current.SourceLocation); } while (args.MoveNext());
      } else {
        slb = new SourceLocationBuilder(this.SourceLocation);
      }
      //^ assume par.IsParameterArray;
      CreateArray createArray = new CreateArray(par.ParamArrayElementType, initializers, slb);
      createArray.SetContainingExpression(this);
      return createArray;
    }

    /// <summary>
    /// Checks if the expression has a side effect and reports an error unless told otherwise.
    /// </summary>
    /// <param name="reportError">If true, report an error if the expression has a side effect.</param>
    public override bool HasSideEffect(bool reportError) {
      if (this.hasSideEffect == null) {
        bool parameterHasSideEffect = false;
        foreach (Expression argument in this.ConvertedArguments)
          parameterHasSideEffect |= argument.HasSideEffect(reportError);

        bool methodHasSideEffect = false;
        IMethodContract/*?*/ contract = this.Compilation.ContractProvider.GetMethodContractFor(this.ResolvedMethod);
        if (contract != null) {
          methodHasSideEffect =
             IteratorHelper.EnumerableIsNotEmpty(contract.Writes) ||
             IteratorHelper.EnumerableIsNotEmpty(contract.ModifiedVariables) ||
             IteratorHelper.EnumerableIsNotEmpty(contract.Allocates) ||
             IteratorHelper.EnumerableIsNotEmpty(contract.Frees);
        }
        bool result = methodHasSideEffect | parameterHasSideEffect;
        if (reportError) {
          if (methodHasSideEffect)
            this.Helper.ReportError(new AstErrorMessage(this, Error.ExpressionHasSideEffect));
          this.hasSideEffect = result;
        }
        return result;
      }
      return this.hasSideEffect.Value;
    }
    bool? hasSideEffect;

    /// <summary>
    /// True if the method to call is determined at run time, based on the runtime type of ThisArgument.
    /// </summary>
    public abstract bool IsVirtualCall {
      get;
    }

    /// <summary>
    /// The arguments to pass to the constructor, indexer or method, as they are before they have been converted to match the parameters of the resolved method.
    /// </summary>
    public virtual IEnumerable<Expression> OriginalArguments {
      get { return this.originalArguments; }
    }
    readonly IEnumerable<Expression> originalArguments;

    /// <summary>
    /// The arguments to pass to the constructor, index or method, before conversion.
    /// In the case of an extension method call, these will be the static call arguments, 
    /// in all other cases the result is the original argument enumeration.
    /// </summary>
    public virtual IEnumerable<Expression> ApplicableArguments {
      get {
        IMethodDefinition dummy = this.ResolvedMethod; // Called for side-effect;
        return (this.argumentsForExtensionCall != null ? this.argumentsForExtensionCall : this.originalArguments);
      }
    }
    private IEnumerable<Expression> argumentsForExtensionCall;

    /// <summary>
    /// The constructor, indexer or method that is being called.
    /// </summary>
    public IMethodDefinition ResolvedMethod {
      get {
        if (this.resolvedMethod == null)
          this.resolvedMethod = this.ResolveMethod();
        return this.resolvedMethod;
      }
    }
    /// <summary>
    /// The constructor, indexer or method that is being called. Visible to derived types in order to allow 
    /// initialization during construction.
    /// </summary>
    protected IMethodDefinition/*?*/ resolvedMethod;

    /// <summary>
    /// Uses the this.OriginalArguments and this.GetCandidateMethods to resolve the actual method to call.
    /// </summary>
    protected virtual IMethodDefinition ResolveMethod() {
      MethodCall methodCall;
      QualifiedName callExpression;
      IMethodDefinition resolvedMethod = Dummy.Method;
      IEnumerable<IMethodDefinition> candidateMethods = this.GetCandidateMethods(false);

      if (IteratorHelper.EnumerableIsNotEmpty(candidateMethods))
        resolvedMethod = this.Helper.ResolveOverload(candidateMethods, this.OriginalArguments, false);
      if (resolvedMethod is Dummy &&
        (methodCall = this as MethodCall) != null && 
        (callExpression = methodCall.MethodExpression as QualifiedName) != null) {
        // Cannot reuse local variable "candidateMethods" here, as the current
        // value is still live for use in error reporting in case of failure.
        IEnumerable<Expression> argumentsForStaticCall =
          LanguageSpecificCompilationHelper.MakeExtensionArgumentList(callExpression, methodCall.OriginalArguments);
        IEnumerable<IMethodDefinition> extensionCandidates = methodCall.GetCandidateExtensionMethods(argumentsForStaticCall);
        resolvedMethod = this.Helper.ResolveOverload(extensionCandidates, argumentsForStaticCall, false);
        if (!(resolvedMethod is Dummy))
          this.argumentsForExtensionCall = argumentsForStaticCall;
      }

      if (resolvedMethod is Dummy) {
        if (this.ComplainedAboutArguments()) return resolvedMethod;
        if (this.ComplainedAboutFailedInferences()) return resolvedMethod;
        resolvedMethod = this.Helper.ResolveOverload(candidateMethods, this.OriginalArguments, true);
        if (!(resolvedMethod is Dummy)) {
          this.Helper.ReportError(new AstErrorMessage(this, Error.BadArgumentTypes, this.Helper.GetMethodSignature(resolvedMethod,
            NameFormattingOptions.Signature | NameFormattingOptions.UseTypeKeywords)));
        } else {
          this.ComplainAboutCallee();
        }
      }
      return resolvedMethod;
    }

    /// <summary>
    /// Check the arguments for errors and return true if any are found.
    /// </summary>
    protected virtual bool ComplainedAboutArguments() {
      bool badSubExpression = false;
      foreach (Expression argument in this.OriginalArguments) {
        if (argument.HasErrors) badSubExpression = true;
      }
      return badSubExpression;
    }

    /// <summary>
    /// Called when the arguments are good and no type inferences have failed. This means that the callee could not be found. Complain.
    /// </summary>
    protected abstract void ComplainAboutCallee();

    /// <summary>
    /// If the candidate methods for this call are all generic methods, pick the first one and complain that type inference has failed for it.
    /// Call this method only if overload resolution has failed to select a candidate method.
    /// </summary>
    protected virtual bool ComplainedAboutFailedInferences() {
      IEnumerable<IMethodDefinition> candidateMethods = this.GetCandidateMethods(true);
      IMethodDefinition/*?*/ methodToComplainAbout = null;
      bool sawOnlyGenericMethods = true;
      foreach (IMethodDefinition candidate in candidateMethods) {
        if (!candidate.IsGeneric) { sawOnlyGenericMethods = false; break; }
        if (methodToComplainAbout == null) methodToComplainAbout = candidate;
      }
      if (sawOnlyGenericMethods && methodToComplainAbout != null) {
        this.Helper.ReportError(new AstErrorMessage(this, Error.CantInferMethTypeArgs,
          this.Helper.GetMethodSignature(methodToComplainAbout, NameFormattingOptions.Signature|NameFormattingOptions.TypeParameters|NameFormattingOptions.UseTypeKeywords)));
        return true;
      }
      return false;
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingExpression(Expression containingExpression) {
      base.SetContainingExpression(containingExpression);
      foreach (Expression argument in this.originalArguments) argument.SetContainingExpression(this);
    }

    /// <summary>
    /// The type of value that the expression will evaluate to, as determined at compile time.
    /// </summary>
    public override ITypeDefinition Type {
      get { return this.ResolvedMethod.Type.ResolvedType; }
    }
  }

  /// <summary>
  /// Converts a value to a given (resolved) type. Not for use by parsers.
  /// </summary>
  public class Conversion : Expression, IConversion {

    /// <summary>
    /// Converts a value to a given (resolved) type. Not for use by parsers.
    /// </summary>
    /// <param name="valueToConvert">The value to convert. Must be fully initialized.</param>
    /// <param name="resultType">The type to which the value is to be converted.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public Conversion(Expression valueToConvert, ITypeDefinition resultType, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.valueToConvert = valueToConvert;
      this.type = resultType;
      this.SetContainingBlock(valueToConvert.ContainingBlock);
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    public Conversion(BlockStatement containingBlock, Conversion template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.valueToConvert = template.valueToConvert.MakeCopyFor(containingBlock);
      this.type = template.type;
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return this.ValueToConvert.HasErrors;
    }

    /// <summary>
    /// Returns true if no information is lost if the integer value of this expression is converted to the target integer type.
    /// </summary>
    /// <param name="targetType"></param>
    /// <returns></returns>
    public override bool IntegerConversionIsLossless(ITypeDefinition targetType) {
      return base.IntegerConversionIsLossless(targetType);
    }

    /// <summary>
    /// If true and ValueToConvert is a number and ResultType is a numeric type, check that ValueToConvert falls within the range of ResultType and throw an exception if not.
    /// </summary>
    public bool CheckNumericRange {
      get { return this.ValueToConvert.ContainingBlock.UseCheckedArithmetic; }
    }

    /// <summary>
    /// Calls the visitor.Visit(IConversion) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(Conversion) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Computes the compile time value of the expression. Can be null.
    /// </summary>
    protected override object/*?*/ GetValue() {
      object/*?*/ valueToConvert = this.ValueToConvert.Value;
      if (valueToConvert == null) return null;
      ITypeDefinition targetType = this.Type;
      if (targetType.IsEnum) targetType = targetType.UnderlyingType.ResolvedType;
      try {
        switch (Convert.GetTypeCode(valueToConvert)) {
          case TypeCode.Boolean:
          case TypeCode.Byte:
          case TypeCode.Char:
          case TypeCode.UInt16:
          case TypeCode.UInt32:
          case TypeCode.UInt64:
            ulong u = Convert.ToUInt64(valueToConvert);
            switch (targetType.TypeCode) {
              case PrimitiveTypeCode.Boolean: return u != 0;
              case PrimitiveTypeCode.Char: return (char)u;
              case PrimitiveTypeCode.Float32: return (float)u;
              case PrimitiveTypeCode.Float64: return (double)u;
              case PrimitiveTypeCode.Int16: return (short)u;
              case PrimitiveTypeCode.Int32: return (int)u;
              case PrimitiveTypeCode.Int64: return (long)u;
              case PrimitiveTypeCode.Int8: return (sbyte)u;
              case PrimitiveTypeCode.UInt16: return (ushort)u;
              case PrimitiveTypeCode.UInt32: return (uint)u;
              case PrimitiveTypeCode.UInt64: return u;
              case PrimitiveTypeCode.UInt8: return (byte)u;
            }
            break;
          case TypeCode.Int16:
          case TypeCode.Int32:
          case TypeCode.Int64:
            long l = Convert.ToInt64(valueToConvert);
            switch (targetType.TypeCode) {
              case PrimitiveTypeCode.Boolean: return l != 0;
              case PrimitiveTypeCode.Char: return (char)l;
              case PrimitiveTypeCode.Float32: return (float)l;
              case PrimitiveTypeCode.Float64: return (double)l;
              case PrimitiveTypeCode.Int16: return (short)l;
              case PrimitiveTypeCode.Int32: return (int)l;
              case PrimitiveTypeCode.Int64: return l;
              case PrimitiveTypeCode.Int8: return (sbyte)l;
              case PrimitiveTypeCode.UInt16: return (ushort)l;
              case PrimitiveTypeCode.UInt32: return (uint)l;
              case PrimitiveTypeCode.UInt64: return (ulong)l;
              case PrimitiveTypeCode.UInt8: return (byte)l;
            }
            break;
          default:
            switch (targetType.TypeCode) {
              case PrimitiveTypeCode.Boolean: return Convert.ToBoolean(valueToConvert);
              case PrimitiveTypeCode.Char: return Convert.ToChar(valueToConvert);
              case PrimitiveTypeCode.Float32: return Convert.ToSingle(valueToConvert);
              case PrimitiveTypeCode.Float64: return Convert.ToDouble(valueToConvert);
              case PrimitiveTypeCode.Int16: return Convert.ToInt16(valueToConvert);
              case PrimitiveTypeCode.Int32: return Convert.ToInt32(valueToConvert);
              case PrimitiveTypeCode.Int64: return Convert.ToInt64(valueToConvert);
              case PrimitiveTypeCode.Int8: return Convert.ToSByte(valueToConvert);
              case PrimitiveTypeCode.UInt16: return Convert.ToUInt16(valueToConvert);
              case PrimitiveTypeCode.UInt32: return Convert.ToUInt32(valueToConvert);
              case PrimitiveTypeCode.UInt64: return Convert.ToUInt64(valueToConvert);
              case PrimitiveTypeCode.UInt8: return Convert.ToByte(valueToConvert);
            }
            break;
        }
      } catch (InvalidCastException) {
      }
      return null;
    }

    /// <summary>
    /// Checks if the expression has a side effect and reports an error unless told otherwise.
    /// </summary>
    /// <param name="reportError">If true, report an error if the expression has a side effect.</param>
    public override bool HasSideEffect(bool reportError) {
      return this.ValueToConvert.HasSideEffect(reportError);
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new Conversion(containingBlock, this);
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      return this;
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingExpression(Expression containingExpression) {
      base.SetContainingExpression(containingExpression);
      this.valueToConvert.SetContainingExpression(this);
    }

    /// <summary>
    /// The type to which the value is to be converted.
    /// </summary>
    public sealed override ITypeDefinition Type {
      get { return this.type; }
    }
    readonly ITypeDefinition type;

    /// <summary>
    /// The value to convert.
    /// </summary>
    public Expression ValueToConvert {
      get { return this.valueToConvert; }
    }
    readonly Expression valueToConvert;

    #region IConversion Members

    ITypeReference IConversion.TypeAfterConversion {
      get { return this.Type; }
    }

    IExpression IConversion.ValueToConvert {
      get { return this.ValueToConvert.ProjectAsIExpression(); }
    }

    #endregion

    /// <summary>
    /// Returns a string representation of the expression for debugging and logging uses.
    /// </summary>
    public override string ToString() {
      return this.Type.ToString() + "(" + this.ValueToConvert.ToString() + ")";
    }

    #region IExpression Members

    ITypeReference IExpression.Type {
      get { return this.Type; }
    }

    #endregion
  }

  /// <summary>
  /// A pair of expressions. The first is evaluated for side-effects only. The value of the second expression is the value of the overall expression.
  /// When overloaded, this expression corresponds to a call to op_Comma.
  /// </summary>
  public class Comma : BinaryOperation {

    /// <summary>
    /// Allocates a pair of expressions. The first is evaluated for side-effects only. The value of the second expression is the value of the overall expression.
    /// When overloaded, this expression corresponds to a call to op_Comma.
    /// </summary>
    /// <param name="leftOperand">The left operand.</param>
    /// <param name="rightOperand">The right operand.</param>
    /// <param name="sourceLocation">The source location of the operation.</param>
    public Comma(Expression leftOperand, Expression rightOperand, ISourceLocation sourceLocation)
      : base(leftOperand, rightOperand, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected Comma(BlockStatement containingBlock, Comma template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Calls the visitor.Visit(Comma) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Returns the string used to identify the operator in error messages
    /// </summary>
    protected override string OperationSymbolForErrorMessage {
      get { return ","; }
    }


    /// <summary>
    /// Returns the name a method must have to overload this operator.
    /// </summary>
    protected override IName GetOperatorName() {
      return this.NameTable.OpComma;
    }

    /// <summary>
    /// Infers the type of value that this expression will evaluate to. At runtime the actual value may be an instance of subclass of the result of this method.
    /// Calling this method does not cache the computed value and does not generate any error messages. In some cases, such as references to the parameters of lambda
    /// expressions during type overload resolution, the value returned by this method may be different from one call to the next.
    /// When type inference fails, Dummy.Type is returned.
    /// </summary>
    public override ITypeDefinition InferType() {
      return this.RightOperand.Type;
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// </summary>
    /// <returns></returns>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return this.LeftOperand.HasErrors || this.RightOperand.HasErrors;
    }

    /// <summary>
    /// Returns true if no information is lost if the integer value of this expression is converted to the target integer type.
    /// </summary>
    public override bool IntegerConversionIsLossless(ITypeDefinition targetType) {
      return this.RightOperand.IntegerConversionIsLossless(targetType);
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new Comma(containingBlock, this);
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      if (this.OverloadMethodCall != null) return base.ProjectAsNonConstantIExpression();
      if (this.cachedProjection == null) {
        List<Statement> stmts = new List<Statement>(1);
        stmts.Add(new ExpressionStatement(this.LeftOperand));
        BlockStatement block = new BlockStatement(stmts, this.LeftOperand.SourceLocation);
        BlockExpression be = new BlockExpression(block, this.RightOperand, this.SourceLocation);
        be.SetContainingExpression(this);
        this.cachedProjection = be.ProjectAsIExpression();
      }
      return this.cachedProjection;
    }
    IExpression/*?*/ cachedProjection;

    /// <summary>
    /// A list of dummy methods that correspond to operations that are built into IL. The dummy methods are used, via overload resolution,
    /// to determine how the operands are to be converted before the operation is carried out.
    /// </summary>
    protected override IEnumerable<IMethodDefinition> StandardOperators {
      get { return Enumerable<IMethodDefinition>.Empty; }
    }

  }

  /// <summary>
  /// An expression that does not change its value at runtime and that can be evaluated at compile time.
  /// </summary>
  public class CompileTimeConstant : Expression, ICompileTimeConstant, IMetadataConstant {

    /// <summary>
    /// Initializes an expression that does not change its value at runtime and that can be evaluated at compile time.
    /// </summary>
    /// <param name="expression">An expression with a non null Value.</param>
    public CompileTimeConstant(Expression expression)
      : base(expression.CouldBeInterpretedAsNegativeSignedInteger ? ConvertToUnsigned(expression.Value) : expression.Value, expression.SourceLocation)
      //^ requires expression.Value != null;
    {
      this.couldBeInterpretedAsNegativeSignedInteger = expression.CouldBeInterpretedAsNegativeSignedInteger;
      this.couldBeInterpretedAsUnsignedInteger = expression.CouldBeInterpretedAsUnsignedInteger;
      this.valueIsPolymorphicCompileTimeConstant = expression.ValueIsPolymorphicCompileTimeConstant;
    }

    /// <summary>
    /// Initializes an expression that does not change its value at runtime and that can be evaluated at compile time.
    /// </summary>
    /// <param name="value">The actual value of the expression. Can be null.</param>
    /// <param name="sourceLocation">The location in the source text of the expression that corresponds to this constant.</param>
    public CompileTimeConstant(object/*?*/ value, ISourceLocation sourceLocation)
      : base(value, sourceLocation) {
      this.valueIsPolymorphicCompileTimeConstant = true;
      this.couldBeInterpretedAsNegativeSignedInteger = false;
    }

    /// <summary>
    /// Initializes an expression that does not change its value at runtime and that can be evaluated at compile time.
    /// </summary>
    /// <param name="value">The actual value of the expression. Can be null.</param>
    /// <param name="valueIsPolymorhpicCompileTimeConstant">If false, then the expression is is numerlic literal that includes an explicit indication of the numeric type to use. For example 1.1f. </param>
    /// <param name="sourceLocation">The location in the source text of the expression that corresponds to this constant.</param>
    public CompileTimeConstant(object/*?*/ value, bool valueIsPolymorhpicCompileTimeConstant, ISourceLocation sourceLocation)
      : base(value, sourceLocation) {
      this.valueIsPolymorphicCompileTimeConstant = valueIsPolymorhpicCompileTimeConstant;
      this.couldBeInterpretedAsNegativeSignedInteger = false;
    }

    /// <summary>
    /// Initializes an expression that does not change its value at runtime and that can be evaluated at compile time.
    /// </summary>
    /// <param name="value">The actual value of the expression. Can be null.</param>
    /// <param name="valueIsPolymorphicCompileTimeConstant">If false, then the expression is is numerlic literal that includes an explicit indication of the numeric type to use. For example 1.1f. </param>
    /// <param name="couldBeInterpretedAsNegativeSignedInteger">True if the constant is a positive integer that could be interpreted as a negative signed integer.
    /// <param name="sourceLocation">The location in the source text of the expression that corresponds to this constant.</param>
    /// For example, 0x80000000, could be interpreted as a convenient way of writing int.MinValue.</param>
    public CompileTimeConstant(object/*?*/ value, bool valueIsPolymorphicCompileTimeConstant, bool couldBeInterpretedAsNegativeSignedInteger, ISourceLocation sourceLocation)
      : base(couldBeInterpretedAsNegativeSignedInteger ? ConvertToUnsigned(value) : value, sourceLocation) {
      this.valueIsPolymorphicCompileTimeConstant = valueIsPolymorphicCompileTimeConstant;
      this.couldBeInterpretedAsNegativeSignedInteger = couldBeInterpretedAsNegativeSignedInteger;
    }

    /// <summary>
    /// Initializes an expression that does not change its value at runtime and that can be evaluated at compile time.
    /// </summary>
    /// <param name="value">The actual value of the expression. Can be null.</param>
    /// <param name="valueIsPolymorphicCompileTimeConstant">If false, then the expression is is numerlic literal that includes an explicit indication of the numeric type to use. For example 1.1f. </param>
    /// <param name="couldBeInterpretedAsNegativeSignedInteger">True if the constant is a positive integer that could be interpreted as a negative signed integer.
    /// <param name="type">The type of value that the expression will evaluate to, as determined at compile time.</param>
    /// <param name="sourceLocation">The location in the source text of the expression that corresponds to this constant.</param>
    /// For example, 0x80000000, could be interpreted as a convenient way of writing int.MinValue.</param>
    public CompileTimeConstant(object/*?*/ value, bool valueIsPolymorphicCompileTimeConstant, bool couldBeInterpretedAsNegativeSignedInteger, ITypeDefinition type, ISourceLocation sourceLocation)
      : base(couldBeInterpretedAsNegativeSignedInteger ? ConvertToUnsigned(value) : value, sourceLocation) {
      this.valueIsPolymorphicCompileTimeConstant = valueIsPolymorphicCompileTimeConstant;
      this.couldBeInterpretedAsNegativeSignedInteger = couldBeInterpretedAsNegativeSignedInteger;
      this.type = type;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected CompileTimeConstant(BlockStatement containingBlock, CompileTimeConstant template)
      : base(containingBlock, template.Value, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.valueIsPolymorphicCompileTimeConstant = template.ValueIsPolymorphicCompileTimeConstant;
      this.couldBeInterpretedAsNegativeSignedInteger = template.CouldBeInterpretedAsNegativeSignedInteger;
      this.unfoldedExpression = template.UnfoldedExpression;
    }

    /// <summary>
    /// If the value of this compile time constant is an integer that falls in the range of the given
    /// (numeric) target type, the method returns a polymorhpic compile time constant whose Type value is the given
    /// target type. Otherwise the method returns this constant.
    /// </summary>
    /// <param name="targetType">The (numeric) target type.</param>
    /// <param name="allowLossOfSign">If true, a negative value may be converted to an unsigned value by reinterpreting the bits.</param>
    //^ [Pure]
    public CompileTimeConstant ConvertToTargetTypeIfIntegerInRangeOf(ITypeDefinition targetType, bool allowLossOfSign) {
      object/*?*/ newBoxedValue = this.ConvertToBoxedValueOfTargetTypeIfIntegerInRangeOf(targetType, allowLossOfSign);
      if (newBoxedValue != this.Value) {
        CompileTimeConstant result = new CompileTimeConstant(newBoxedValue, true, this.SourceLocation);
        result.UnfoldedExpression = this.UnfoldedExpression;
        result.SetContainingBlock(this.ContainingBlock);
        return result;
      }
      return this;
    }

    /// <summary>
    /// If the value of this compile time constant is a boxed integer and if that integer value falls in the range of the 
    /// given (numeric) target type, then convert the value of this constant to the target type and return it as a boxed value of that type.
    /// If the target type if not an integral type, or if the conversion is not possible, then this.Value is returned. Note that this.Value
    /// may be null.
    /// </summary>
    private object/*?*/ ConvertToBoxedValueOfTargetTypeIfIntegerInRangeOf(ITypeDefinition targetType, bool allowLossOfSign) {
      object/*?*/ value = this.Value;
      switch (targetType.TypeCode) {
        case PrimitiveTypeCode.UInt8: {
            if (value is IntPtr) {
              long lng = ((IntPtr)value).ToInt64();
              if ((allowLossOfSign || 0 <= lng) && lng <= byte.MaxValue) return (byte)lng;
            } else {
              IConvertible/*?*/ ic = value as IConvertible;
              if (ic != null) {
                switch (ic.GetTypeCode()) {
                  case System.TypeCode.Byte:
                    return value;
                  case System.TypeCode.SByte:
                    sbyte sb = ic.ToSByte(null);
                    if (allowLossOfSign || byte.MinValue <= sb) return (byte)sb;
                    break;
                  case System.TypeCode.Int16:
                    short s = ic.ToInt16(null);
                    if ((allowLossOfSign || byte.MinValue <= s) && s <= byte.MaxValue) return (byte)s;
                    break;
                  case System.TypeCode.Int32:
                    int i = ic.ToInt32(null);
                    if ((allowLossOfSign || byte.MinValue <= i) && i <= byte.MaxValue) return (byte)i;
                    break;
                  case System.TypeCode.Int64:
                    long lng = ic.ToInt64(null);
                    if ((allowLossOfSign || byte.MinValue <= lng) && lng <= byte.MaxValue) return (byte)lng;
                    break;
                  case System.TypeCode.UInt16:
                    ushort us = ic.ToUInt16(null);
                    if (us <= byte.MaxValue) return (byte)us;
                    break;
                  case System.TypeCode.UInt32:
                    uint ui = ic.ToUInt32(null);
                    if (ui <= byte.MaxValue) return (byte)ui;
                    break;
                  case System.TypeCode.UInt64:
                    ulong ul = ic.ToUInt64(null);
                    if (ul <= byte.MaxValue) return (byte)ul;
                    break;
                }
              }
            }
            return value;
          }
        case PrimitiveTypeCode.UInt16: {
            if (value is IntPtr) {
              long lng = ((IntPtr)value).ToInt64();
              if ((allowLossOfSign || 0 <= lng) && lng <= ushort.MaxValue) return (ushort)lng;
            } else {
              IConvertible/*?*/ ic = value as IConvertible;
              if (ic != null) {
                switch (ic.GetTypeCode()) {
                  case System.TypeCode.Byte:
                    return (ushort)ic.ToByte(null);
                  case System.TypeCode.UInt16:
                    return value;
                  case System.TypeCode.SByte:
                    sbyte sb = ic.ToSByte(null);
                    if (allowLossOfSign || ushort.MinValue <= sb) return (ushort)sb;
                    break;
                  case System.TypeCode.Int16:
                    short s = ic.ToInt16(null);
                    if (allowLossOfSign || ushort.MinValue <= s) return (ushort)s;
                    break;
                  case System.TypeCode.Int32:
                    int i = ic.ToInt32(null);
                    if ((allowLossOfSign || ushort.MinValue <= i) && i <= ushort.MaxValue) return (ushort)i;
                    break;
                  case System.TypeCode.Int64:
                    long lng = ic.ToInt64(null);
                    if ((allowLossOfSign || ushort.MinValue <= lng) && lng <= ushort.MaxValue) return (ushort)lng;
                    break;
                  case System.TypeCode.UInt32:
                    uint ui = ic.ToUInt32(null);
                    if (ui <= ushort.MaxValue) return (ushort)ui;
                    break;
                  case System.TypeCode.UInt64:
                    ulong ul = ic.ToUInt64(null);
                    if (ul <= ushort.MaxValue) return (ushort)ul;
                    break;
                }
              }
            }
            return value;
          }
        case PrimitiveTypeCode.UInt32: {
            if (value is IntPtr) {
              long lng = ((IntPtr)value).ToInt64();
              if ((allowLossOfSign || 0 <= lng) && lng <= uint.MaxValue) return (uint)lng;
            } else {
              IConvertible/*?*/ ic = value as IConvertible;
              if (ic != null) {
                switch (ic.GetTypeCode()) {
                  case System.TypeCode.Byte:
                    return (uint)ic.ToByte(null);
                  case System.TypeCode.UInt16:
                    return (uint)ic.ToUInt16(null);
                  case System.TypeCode.UInt32:
                    return value;
                  case System.TypeCode.SByte:
                    sbyte sb = ic.ToSByte(null);
                    if (allowLossOfSign || uint.MinValue <= sb) return (uint)sb;
                    break;
                  case System.TypeCode.Int16:
                    short s = ic.ToInt16(null);
                    if (allowLossOfSign || uint.MinValue <= s) return (uint)s;
                    break;
                  case System.TypeCode.Int32:
                    int i = ic.ToInt32(null);
                    if (allowLossOfSign || uint.MinValue <= i) return (uint)i;
                    break;
                  case System.TypeCode.Int64:
                    long lng = ic.ToInt64(null);
                    if ((allowLossOfSign || uint.MinValue <= lng) && lng <= uint.MaxValue) return (uint)lng;
                    break;
                  case System.TypeCode.UInt64:
                    ulong ul = ic.ToUInt64(null);
                    if (ul <= uint.MaxValue) return (uint)ul;
                    break;
                }
              }
            }
            return value;
          }
        case PrimitiveTypeCode.UInt64: {
            if (value is IntPtr) {
              long lng = ((IntPtr)value).ToInt64();
              if (allowLossOfSign || 0 <= lng) return (ulong)lng;
            } else {
              IConvertible/*?*/ ic = value as IConvertible;
              if (ic != null) {
                switch (ic.GetTypeCode()) {
                  case System.TypeCode.Byte:
                    return (ulong)ic.ToByte(null);
                  case System.TypeCode.UInt16:
                    return (ulong)ic.ToUInt16(null);
                  case System.TypeCode.UInt32:
                    return (ulong)ic.ToUInt32(null);
                  case System.TypeCode.UInt64:
                    return value;
                  case System.TypeCode.SByte:
                    sbyte sb = ic.ToSByte(null);
                    if (allowLossOfSign || 0 <= sb) return (ulong)sb;
                    break;
                  case System.TypeCode.Int16:
                    short s = ic.ToInt16(null);
                    if (allowLossOfSign || 0 <= s) return (ulong)s;
                    break;
                  case System.TypeCode.Int32:
                    int i = ic.ToInt32(null);
                    if (allowLossOfSign || 0 <= i) return (ulong)i;
                    break;
                  case System.TypeCode.Int64:
                    long lng = ic.ToInt64(null);
                    if (allowLossOfSign || 0 <= lng) return (ulong)lng;
                    break;
                }
              }
            }
            return value;
          }
        case PrimitiveTypeCode.Int8: {
            if (value is IntPtr) {
              long lng = ((IntPtr)value).ToInt64();
              if (sbyte.MinValue <= lng && lng <= sbyte.MaxValue) return (sbyte)lng;
            } else {
              IConvertible/*?*/ ic = value as IConvertible;
              if (ic != null) {
                switch (ic.GetTypeCode()) {
                  case System.TypeCode.SByte:
                    return value;
                  case System.TypeCode.Int16:
                    short s = ic.ToInt16(null);
                    if (sbyte.MinValue <= s && s <= sbyte.MaxValue) return (sbyte)s;
                    break;
                  case System.TypeCode.Int32:
                    int i = ic.ToInt32(null);
                    if (sbyte.MinValue <= i && i <= sbyte.MaxValue) return (sbyte)i;
                    break;
                  case System.TypeCode.Int64:
                    long lng = ic.ToInt64(null);
                    if (sbyte.MinValue <= lng && lng <= sbyte.MaxValue) return (sbyte)lng;
                    break;
                  case System.TypeCode.Byte:
                    byte b = ic.ToByte(null);
                    if (this.CouldBeInterpretedAsNegativeSignedInteger || b <= sbyte.MaxValue) return (sbyte)b;
                    break;
                  case System.TypeCode.UInt16:
                    ushort us = ic.ToUInt16(null);
                    if (us <= sbyte.MaxValue) return (sbyte)us;
                    break;
                  case System.TypeCode.UInt32:
                    uint ui = ic.ToUInt32(null);
                    if (ui <= sbyte.MaxValue) return (sbyte)ui;
                    break;
                  case System.TypeCode.UInt64:
                    ulong ul = ic.ToUInt64(null);
                    if (ul <= (ulong)sbyte.MaxValue) return (sbyte)ul;
                    break;
                }
              }
            }
            return value;
          }
        case PrimitiveTypeCode.Int16: {
            if (value is IntPtr) {
              long lng = ((IntPtr)value).ToInt64();
              if (short.MinValue <= lng && lng <= short.MaxValue) return (short)lng;
            } else {
              IConvertible/*?*/ ic = value as IConvertible;
              if (ic != null) {
                switch (ic.GetTypeCode()) {
                  case System.TypeCode.SByte:
                    return (short)ic.ToSByte(null);
                  case System.TypeCode.Byte:
                    return (short)ic.ToByte(null);
                  case System.TypeCode.Int16:
                    return value;
                  case System.TypeCode.Int32:
                    int i = ic.ToInt32(null);
                    if (short.MinValue <= i && i <= short.MaxValue) return (short)i;
                    break;
                  case System.TypeCode.Int64:
                    long lng = ic.ToInt64(null);
                    if (short.MinValue <= lng && lng <= short.MaxValue) return (short)lng;
                    break;
                  case System.TypeCode.UInt16:
                    ushort us = ic.ToUInt16(null);
                    if (this.CouldBeInterpretedAsNegativeSignedInteger || us <= short.MaxValue) return (short)us;
                    break;
                  case System.TypeCode.UInt32:
                    uint ui = ic.ToUInt32(null);
                    if (ui <= short.MaxValue) return (short)ui;
                    break;
                  case System.TypeCode.UInt64:
                    ulong ul = ic.ToUInt64(null);
                    if (ul <= (ulong)short.MaxValue) return (short)ul;
                    break;
                }
              }
            }
            return value;
          }
        case PrimitiveTypeCode.Int32: {
            if (value is IntPtr) {
              long lng = ((IntPtr)value).ToInt64();
              if (int.MinValue <= lng && lng <= int.MaxValue) return (int)lng;
            } else {
              IConvertible/*?*/ ic = value as IConvertible;
              if (ic != null)
                switch (ic.GetTypeCode()) {
                  case System.TypeCode.SByte:
                    return (int)ic.ToSByte(null);
                  case System.TypeCode.Byte:
                    return (int)ic.ToByte(null);
                  case System.TypeCode.Int16:
                    return (int)ic.ToInt16(null);
                  case System.TypeCode.UInt16:
                    return (int)ic.ToUInt16(null);
                  case System.TypeCode.Int32:
                    return value;
                  case System.TypeCode.Int64:
                    long lng = ic.ToInt64(null);
                    if (int.MinValue <= lng && lng <= int.MaxValue) return (int)lng;
                    break;
                  case System.TypeCode.UInt32:
                    uint ui = ic.ToUInt32(null);
                    if (this.CouldBeInterpretedAsNegativeSignedInteger || ui <= int.MaxValue) return (int)ui;
                    break;
                  case System.TypeCode.UInt64:
                    ulong ul = ic.ToUInt64(null);
                    if (ul <= int.MaxValue) return (int)ul;
                    break;
                }
            }
            return value;
          }
        case PrimitiveTypeCode.Int64: {
            if (value is IntPtr) {
              return ((IntPtr)value).ToInt64();
            } else {
              IConvertible/*?*/ ic = value as IConvertible;
              if (ic != null) {
                switch (ic.GetTypeCode()) {
                  case System.TypeCode.SByte:
                    return (long)ic.ToSByte(null);
                  case System.TypeCode.Byte:
                    return (long)ic.ToByte(null);
                  case System.TypeCode.Int16:
                    return (long)ic.ToInt16(null);
                  case System.TypeCode.UInt16:
                    return (long)ic.ToUInt16(null);
                  case System.TypeCode.Int32:
                    return (long)ic.ToInt32(null);
                  case System.TypeCode.UInt32:
                    return (long)ic.ToUInt32(null);
                  case System.TypeCode.Int64:
                    return value;
                  case System.TypeCode.UInt64:
                    ulong ul = ic.ToUInt64(null);
                    if (this.CouldBeInterpretedAsNegativeSignedInteger || ul <= long.MaxValue) return (long)ul;
                    break;
                }
              }
            }
            return value;
          }
        case PrimitiveTypeCode.Float32: {
            IConvertible/*?*/ ic = value as IConvertible;
            if (ic != null) {
              switch (ic.GetTypeCode()) {
                case System.TypeCode.SByte:
                  return (float)ic.ToSByte(null);
                case System.TypeCode.Byte:
                  return (float)ic.ToByte(null);
                case System.TypeCode.Int16:
                  return (float)ic.ToInt16(null);
                case System.TypeCode.UInt16:
                  return (float)ic.ToUInt16(null);
                case System.TypeCode.Int32:
                  return (float)ic.ToUInt32(null);
                case System.TypeCode.Int64:
                  return (float)ic.ToInt64(null);
                case System.TypeCode.UInt32:
                  return (float)ic.ToUInt32(null);
                case System.TypeCode.UInt64:
                  return (float)ic.ToUInt64(null);
              }
            }
            return value;
          }
        case PrimitiveTypeCode.Float64: {
            IConvertible/*?*/ ic = value as IConvertible;
            if (ic != null) {
              switch (ic.GetTypeCode()) {
                case System.TypeCode.SByte:
                  return (double)ic.ToSByte(null);
                case System.TypeCode.Byte:
                  return (double)ic.ToByte(null);
                case System.TypeCode.Int16:
                  return (double)ic.ToInt16(null);
                case System.TypeCode.UInt16:
                  return (double)ic.ToUInt16(null);
                case System.TypeCode.Int32:
                  return (double)ic.ToUInt32(null);
                case System.TypeCode.Int64:
                  return (double)ic.ToInt64(null);
                case System.TypeCode.UInt32:
                  return (double)ic.ToUInt32(null);
                case System.TypeCode.UInt64:
                  return (double)ic.ToUInt64(null);
              }
            }
            return value;
          }
        case PrimitiveTypeCode.Pointer: {
            IConvertible/*?*/ ic = value as IConvertible;
            if (ic != null) {
              switch (ic.GetTypeCode()) {
                case System.TypeCode.SByte:
                  return new IntPtr((long)ic.ToSByte(null));
                case System.TypeCode.Byte:
                  return new IntPtr((long)ic.ToByte(null));
                case System.TypeCode.Int16:
                  return new IntPtr((long)ic.ToInt16(null));
                case System.TypeCode.UInt16:
                  return new IntPtr((long)ic.ToUInt16(null));
                case System.TypeCode.Int32:
                  return new IntPtr((long)ic.ToInt32(null));
                case System.TypeCode.UInt32:
                  if (IntPtr.Size > 4) return new IntPtr((long)ic.ToUInt32(null));
                  break;
                case System.TypeCode.Int64:
                  if (IntPtr.Size > 4) return new IntPtr((long)value);
                  break;
                case System.TypeCode.UInt64:
                  if (IntPtr.Size > 4) {
                    ulong ul = ic.ToUInt64(null);
                    if (ul <= long.MaxValue) return new IntPtr((long)ul);
                  }
                  break;
              }
            }
            return value;
          }
      }
      return value;
    }

    /// <summary>
    /// If the given object is a signed integer, convert it to an unsigned integer with the same bit pattern.
    /// </summary>
    private static object/*?*/ ConvertToUnsigned(object/*?*/ value) {
      IConvertible/*?*/ ic = value as IConvertible;
      if (ic == null) return null;
      switch (ic.GetTypeCode()) {
        case System.TypeCode.Int16: return (ushort)ic.ToInt16(null);
        case System.TypeCode.Int32: return (uint)ic.ToInt32(null);
        case System.TypeCode.Int64: return (ulong)ic.ToInt64(null);
        case System.TypeCode.SByte: return (byte)ic.ToSByte(null);
      }
      return value;
    }

    /// <summary>
    /// True if the constant is a positive integer that could be interpreted as a negative signed integer.
    /// For example, 0x80000000, could be interpreted as a convenient way of writing int.MinValue.
    /// </summary>
    public override bool CouldBeInterpretedAsNegativeSignedInteger {
      get { return this.couldBeInterpretedAsNegativeSignedInteger; }
    }
    readonly bool couldBeInterpretedAsNegativeSignedInteger;

    /// <summary>
    /// True if this expression is a constant negative integer that could also be interpreted as a unsigned integer.
    /// For example, 1 &lt;&lt; 31 could also be interpreted as a convenient way of writing 0x80000000.
    /// </summary>
    public override bool CouldBeInterpretedAsUnsignedInteger {
      get { return this.couldBeInterpretedAsUnsignedInteger; }
    }
    readonly bool couldBeInterpretedAsUnsignedInteger;

    /// <summary>
    /// Calls the visitor.Visit(ICompileTimeConstant) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit((ICompileTimeConstant)this);
    }

    /// <summary>
    /// Calls the visitor.Visit(CompileTimeConstant) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Returns a CompileTimeConstant instance that corresponds to the given IMetadataConstant instance.
    /// </summary>
    public static CompileTimeConstant For(IMetadataConstant metadataConstant, Expression containingExpression) {
      CompileTimeConstant/*?*/ result = metadataConstant as CompileTimeConstant;
      if (result != null) return result;
      ISourceLocation/*?*/ sourceLocation = null;
      foreach (ILocation location in metadataConstant.Locations) {
        sourceLocation = location as ISourceLocation;
        if (sourceLocation != null) break;
      }
      if (sourceLocation == null) sourceLocation = SourceDummy.SourceLocation;
      result = new CompileTimeConstant(metadataConstant.Value, sourceLocation);
      result.SetContainingExpression(containingExpression);
      return result;
    }

    /// <summary>
    /// Infers the type of value that this expression will evaluate to. At runtime the actual value may be an instance of subclass of the result of this method.
    /// Calling this method does not cache the computed value and does not generate any error messages. In some cases, such as references to the parameters of lambda
    /// expressions during type overload resolution, the value returned by this method may be different from one call to the next.
    /// When type inference fails, Dummy.Type is returned.
    /// </summary>
    public override ITypeDefinition InferType() {
      var platformType = this.PlatformType;
      object/*?*/ value = this.Value;
      IConvertible/*?*/ ic = value as IConvertible;
      if (ic == null) {
        if (value is System.IntPtr) return platformType.SystemIntPtr.ResolvedType;
        if (value is System.UIntPtr) return platformType.SystemUIntPtr.ResolvedType;
        return platformType.SystemObject.ResolvedType;
      }
      switch (ic.GetTypeCode()) {
        case System.TypeCode.Boolean: return platformType.SystemBoolean.ResolvedType;
        case System.TypeCode.Byte: return platformType.SystemUInt8.ResolvedType;
        case System.TypeCode.Char: return platformType.SystemChar.ResolvedType;
        case System.TypeCode.DateTime: return platformType.SystemDateTime.ResolvedType;
        case System.TypeCode.DBNull: return platformType.SystemDBNull.ResolvedType;
        case System.TypeCode.Decimal: return platformType.SystemDecimal.ResolvedType;
        case System.TypeCode.Double: return platformType.SystemFloat64.ResolvedType;
        case System.TypeCode.Empty: return platformType.SystemObject.ResolvedType; //TODO: figure out better type
        case System.TypeCode.Int16: return platformType.SystemInt16.ResolvedType;
        case System.TypeCode.Int32: return platformType.SystemInt32.ResolvedType;
        case System.TypeCode.Int64: return platformType.SystemInt64.ResolvedType;
        case System.TypeCode.Object: return platformType.SystemObject.ResolvedType;
        case System.TypeCode.SByte: return platformType.SystemInt8.ResolvedType;
        case System.TypeCode.Single: return platformType.SystemFloat32.ResolvedType;
        case System.TypeCode.String: return platformType.SystemString.ResolvedType;
        case System.TypeCode.UInt16: return platformType.SystemUInt16.ResolvedType;
        case System.TypeCode.UInt32: return platformType.SystemUInt32.ResolvedType;
        case System.TypeCode.UInt64: return platformType.SystemUInt64.ResolvedType;
      }
      return platformType.SystemObject.ResolvedType;
    }

    /// <summary>
    /// Returns true if no information is lost if the integer value of this expression is converted to the target integer type.
    /// </summary>
    public override bool IntegerConversionIsLossless(ITypeDefinition targetType) {
      if (!TypeHelper.IsPrimitiveInteger(this.Type)) return false;
      if (TypeHelper.TypesAreEquivalent(this.Type, targetType)) return true;
      return this.ConvertToBoxedValueOfTargetTypeIfIntegerInRangeOf(targetType, true) != this.Value;
    }

    /// <summary>
    /// Returns true if the value of this compile constant is numeric.
    /// </summary>
    public bool IsNumericConstant {
      get {
        IConvertible/*?*/ ic = this.Value as IConvertible;
        if (ic == null) return false;
        switch (ic.GetTypeCode()) {
          case System.TypeCode.Byte:
          case System.TypeCode.Double:
          case System.TypeCode.Int16:
          case System.TypeCode.Int32:
          case System.TypeCode.Int64:
          case System.TypeCode.SByte:
          case System.TypeCode.Single:
          case System.TypeCode.UInt16:
          case System.TypeCode.UInt32:
          case System.TypeCode.UInt64:
            return true;
        }
        return false;
      }
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new CompileTimeConstant(containingBlock, this);
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      return this;
    }

    /// <summary>
    /// The type of value that the expression will evaluate to, as determined at compile time.
    /// </summary>
    public sealed override ITypeDefinition Type {
      get {
        if (this.type == null)
          this.type = this.InferType();
        return this.type;
      }
    }
    ITypeDefinition/*?*/ type;

    /// <summary>
    /// Returns true if the expression represents a compile time constant without an explicitly specified type. For example, 1 rather than 1L.
    /// Constant expressions such as 2*16 are polymorhpic if both operands are polymorhic.
    /// </summary>
    public override bool ValueIsPolymorphicCompileTimeConstant {
      get { return this.valueIsPolymorphicCompileTimeConstant; }
    }
    readonly bool valueIsPolymorphicCompileTimeConstant;

    /// <summary>
    /// If this expression is the result of constant folding performed on another expression, then this property returns that other expression.
    /// Otherwise it returns this object.
    /// </summary>
    public Expression UnfoldedExpression {
      get {
        return this.unfoldedExpression??this;
      }
      set {
        this.unfoldedExpression = value;
      }
    }
    Expression/*?*/ unfoldedExpression;

    #region IMetadataExpression Members

    void IMetadataExpression.Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    ITypeReference IMetadataExpression.Type {
      get { return this.Type; }
    }

    #endregion

    #region IExpression Members

    ITypeReference IExpression.Type {
      get { return this.Type; }
    }

    #endregion
  }

  /// <summary>
  /// A dummy constant expression with a source location. Used to provide a non null constant expression in error situations.
  /// </summary>
  public sealed class DummyConstant : CompileTimeConstant {

    /// <summary>
    /// Allocates a dummy constant expression with a source location. Used to provide a non null constant expression in error situations.
    /// </summary>
    /// <param name="sourceLocation">The location in the source text of the expression that corresponds to this constant.</param>
    public DummyConstant(ISourceLocation sourceLocation)
      : base(null, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    private DummyConstant(BlockStatement containingBlock, CompileTimeConstant template)
      : base(containingBlock, template) {
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new DummyConstant(containingBlock, this);
    }

    /// <summary>
    /// Infers the type of value that this expression will evaluate to. At runtime the actual value may be an instance of subclass of the result of this method.
    /// Calling this method does not cache the computed value and does not generate any error messages. In some cases, such as references to the parameters of lambda
    /// expressions during type overload resolution, the value returned by this method may be different from one call to the next.
    /// When type inference fails, Dummy.Type is returned.
    /// </summary>
    public override ITypeDefinition InferType() {
      return Dummy.Type;
    }
  }

  /// <summary>
  /// A comparison performed on a left and right operand.
  /// </summary>
  public abstract class Comparison : BinaryOperation {

    /// <summary>
    /// Initializes a comparison operation performed on a left and right operand.
    /// </summary>
    /// <param name="leftOperand">The left operand.</param>
    /// <param name="rightOperand">The right operand.</param>
    /// <param name="sourceLocation">The source location of the operation.</param>
    protected Comparison(Expression leftOperand, Expression rightOperand, ISourceLocation sourceLocation)
      : base(leftOperand, rightOperand, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected Comparison(BlockStatement containingBlock, Comparison template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Infers the type of value that this expression will evaluate to. At runtime the actual value may be an instance of subclass of the result of this method.
    /// Calling this method does not cache the computed value and does not generate any error messages. In some cases, such as references to the parameters of lambda
    /// expressions during type overload resolution, the value returned by this method may be different from one call to the next.
    /// When type inference fails, Dummy.Type is returned.
    /// </summary>
    /// <remarks>This override allows StandardOperators to use the same dummy methods as the arithmetic operations.</remarks>
    public override ITypeDefinition InferType() {
      return this.PlatformType.SystemBoolean.ResolvedType;
    }

    /// <summary>
    /// If the operands are integers, use unsigned comparison. If the operands are floating point numbers, return true if the operands are unordered.
    /// </summary>
    public bool IsUnsignedOrUnordered {
      get {
        return TypeHelper.IsUnsignedPrimitiveInteger(this.ConvertedLeftOperand.Type);
      }
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// </summary>
    /// <returns></returns>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      bool leftOpIsComparison = this.LeftOperand is Comparison;
      bool rightOpIsComparison = this.RightOperand is Comparison;
      if (leftOpIsComparison || rightOpIsComparison) {
        this.Helper.ReportError(new AstErrorMessage(this, Error.PotentialUnintendRangeComparison, this.SourceLocation.Source, leftOpIsComparison? "left" : "right"));
      }
      return base.CheckForErrorsAndReturnTrueIfAnyAreFound();
    }


    /// <summary>
    /// A list of dummy methods that correspond to operations that are built into IL. The dummy methods are used, via overload resolution,
    /// to determine how the operands are to be converted before the operation is carried out.
    /// </summary>
    protected override IEnumerable<IMethodDefinition> StandardOperators {
      get {
        BuiltinMethods dummyMethods = this.Compilation.BuiltinMethods;
        yield return dummyMethods.Int32opInt32;
        yield return dummyMethods.UInt32opUInt32;
        yield return dummyMethods.Int64opInt64;
        yield return dummyMethods.UInt64opUInt64;
        yield return dummyMethods.Float32opFloat32;
        yield return dummyMethods.Float64opFloat64;
        yield return dummyMethods.DecimalOpDecimal;
        yield return dummyMethods.UIntPtrOpUIntPtr;
        yield return dummyMethods.VoidPtrOpVoidPtr;
        if (this is EqualityComparison)
          yield return dummyMethods.BoolOpBool;
        ITypeDefinition leftOperandType = this.LeftOperand.Type;
        ITypeDefinition rightOperandType = this.RightOperand.Type;
        if (leftOperandType.IsEnum)
          yield return dummyMethods.GetDummyEnumOpEnum(leftOperandType);
        else if (rightOperandType.IsEnum)
          yield return dummyMethods.GetDummyEnumOpEnum(rightOperandType);
        if (this is EqualityComparison && !(leftOperandType.IsValueType || rightOperandType.IsValueType))
          yield return dummyMethods.ObjectOpObject;
      }
    }

  }

  /// <summary>
  /// An expression that results in one of two values, depending on the value of a condition.
  /// </summary>
  public class Conditional : Expression, IConditional {

    /// <summary>
    /// Allocates an expression that results in one of two values, depending on the value of a condition.
    /// </summary>
    /// <param name="condition">The condition that determines which subexpression to evaluate.</param>
    /// <param name="resultIfTrue">The expression to evaluate as the value of the overall expression if the condition is true.</param>
    /// <param name="resultIfFalse">The expression to evaluate as the value of the overall expression if the condition is false.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public Conditional(Expression condition, Expression resultIfTrue, Expression resultIfFalse, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.condition = condition;
      this.resultIfTrue = resultIfTrue;
      this.resultIfFalse = resultIfFalse;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected Conditional(BlockStatement containingBlock, Conditional template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.condition = template.condition.MakeCopyFor(containingBlock);
      this.resultIfTrue = template.resultIfTrue.MakeCopyFor(containingBlock);
      this.resultIfFalse = template.resultIfFalse.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      bool result = this.ConvertedCondition.HasErrors;
      result |= this.ConvertedResultIfTrue.HasErrors;
      result |= this.ConvertedResultIfFalse.HasErrors;
      result |= this.Type is Dummy;
      if (this.Type is Dummy) {
        var leftType = this.ResultIfTrue.Type;
        var rightType = this.ResultIfFalse.Type;
        if (this.Helper.ImplicitConversionExists(this.ResultIfFalse, leftType) &&
            this.Helper.ImplicitConversionExists(this.ResultIfTrue, rightType))
          this.Helper.ReportError(new AstErrorMessage(this, Error.CannotInferTypeOfConditionalDueToAmbiguity,
            this.Helper.GetTypeName(leftType), this.Helper.GetTypeName(rightType)));
        else
          this.Helper.ReportError(new AstErrorMessage(this, Error.CannotInferTypeOfConditional));
      }
      return result;
    }

    /// <summary>
    /// The condition that determines which subexpression to evaluate.
    /// </summary>
    public Expression Condition {
      get { return this.condition; }
    }
    readonly Expression condition;

    /// <summary>
    /// IsTrue(this.Condition)
    /// </summary>
    private Expression ConvertedCondition {
      get {
        if (this.convertedCondition == null)
          this.convertedCondition = new IsTrue(this.Condition);
        return this.convertedCondition;
      }
    }
    Expression/*?*/ convertedCondition;

    /// <summary>
    /// The expression to evaluate as the value of the overall expression if the condition is false, after conversion has been applied to it.
    /// </summary>
    public Expression ConvertedResultIfFalse {
      get { return this.Helper.ImplicitConversion(this.ResultIfFalse, this.Type); }
    }

    /// <summary>
    /// The expression to evaluate as the value of the overall expression if the condition is false, after conversion has been applied to it.
    /// </summary>
    public Expression ConvertedResultIfTrue {
      get { return this.Helper.ImplicitConversion(this.ResultIfTrue, this.Type); }
    }

    /// <summary>
    /// True if the constant is a positive integer that could be interpreted as a negative signed integer.
    /// For example, 0x80000000, could be interpreted as a convenient way of writing int.MinValue.
    /// </summary>
    public override bool CouldBeInterpretedAsNegativeSignedInteger {
      get {
        return this.Value != null && this.ResultIfTrue.CouldBeInterpretedAsNegativeSignedInteger && this.ResultIfFalse.CouldBeInterpretedAsNegativeSignedInteger;
      }
    }

    /// <summary>
    /// Returns true if no information is lost if the integer value of this expression is converted to the target integer type.
    /// </summary>
    /// <param name="targetType"></param>
    /// <returns></returns>
    public override bool IntegerConversionIsLossless(ITypeDefinition targetType) {
      if (!TypeHelper.IsPrimitiveInteger(this.ResultIfTrue.Type) || !TypeHelper.IsPrimitiveInteger(this.ResultIfFalse.Type)) return false;
      return this.ResultIfTrue.IntegerConversionIsLossless(targetType) && this.ResultIfFalse.IntegerConversionIsLossless(targetType);
    }

    /// <summary>
    /// Calls the visitor.Visit(IConditional) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(Conditional) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Computes the compile time value of the expression. Can be null.
    /// </summary>
    protected override object/*?*/ GetValue() {
      object/*?*/ condition = this.ConvertedCondition.Value;
      if (!(condition is bool)) return null;
      if ((bool)condition)
        return this.ConvertedResultIfTrue.Value;
      else
        return this.ConvertedResultIfFalse.Value;
    }

    /// <summary>
    /// Checks if the expression has a side effect and reports an error unless told otherwise.
    /// </summary>
    /// <param name="reportError">If true, report an error if the expression has a side effect.</param>
    public override bool HasSideEffect(bool reportError) {
      return this.Condition.HasSideEffect(reportError) || this.ResultIfTrue.HasSideEffect(reportError) || this.ResultIfFalse.HasSideEffect(reportError);
    }

    /// <summary>
    /// Infers the type of value that this expression will evaluate to. At runtime the actual value may be an instance of subclass of the result of this method.
    /// Calling this method does not cache the computed value and does not generate any error messages. In some cases, such as references to the parameters of lambda
    /// expressions during type overload resolution, the value returned by this method may be different from one call to the next.
    /// When type inference fails, Dummy.Type is returned.
    /// </summary>
    public override ITypeDefinition InferType() {
      ITypeDefinition leftType = this.ResultIfTrue.Type;
      ITypeDefinition rightType = this.ResultIfFalse.Type;
      if (TypeHelper.TypesAreEquivalent(leftType, rightType))
        return leftType;
      else if (this.Helper.ImplicitConversionExists(this.ResultIfFalse, leftType)) {
        if (!this.Helper.ImplicitConversionExists(this.ResultIfTrue, rightType))
          return leftType;
      } else if (this.Helper.ImplicitConversionExists(this.ResultIfTrue, rightType)) {
        if (!this.Helper.ImplicitConversionExists(this.ResultIfFalse, leftType))
          return rightType;
      }
      return Dummy.Type;
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new Conditional(containingBlock, this);
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      return this;
    }

    /// <summary>
    /// The expression to evaluate as the value of the overall expression if the condition is false.
    /// </summary>
    public Expression ResultIfFalse {
      get { return this.resultIfFalse; }
    }
    readonly Expression resultIfFalse;

    /// <summary>
    /// The expression to evaluate as the value of the overall expression if the condition is true.
    /// </summary>
    public Expression ResultIfTrue {
      get { return this.resultIfTrue; }
    }
    readonly Expression resultIfTrue;

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingExpression(Expression containingExpression) {
      base.SetContainingExpression(containingExpression);
      this.condition.SetContainingExpression(this);
      this.resultIfTrue.SetContainingExpression(this);
      this.resultIfFalse.SetContainingExpression(this);
    }

    /// <summary>
    /// The type of value that the expression will evaluate to, as determined at compile time.
    /// </summary>
    public sealed override ITypeDefinition Type {
      get {
        if (this.type == null)
          this.type = this.InferType();
        return this.type;
      }
    }
    //^ [Once]
    ITypeDefinition/*?*/ type;

    /// <summary>
    /// Returns true if the expression represents a compile time constant without an explicitly specified type. For example, 1 rather than 1L.
    /// Constant expressions such as 2*16 are polymorhpic if both operands are polymorhic.
    /// </summary>
    public override bool ValueIsPolymorphicCompileTimeConstant {
      get {
        return this.Value != null && this.ResultIfTrue.ValueIsPolymorphicCompileTimeConstant && this.ResultIfFalse.ValueIsPolymorphicCompileTimeConstant;
      }
    }

    #region IConditional Members

    IExpression IConditional.Condition {
      get { return this.ConvertedCondition.ProjectAsIExpression(); }
    }

    IExpression IConditional.ResultIfTrue {
      get { return this.ConvertedResultIfTrue.ProjectAsIExpression(); }
    }

    IExpression IConditional.ResultIfFalse {
      get { return this.ConvertedResultIfFalse.ProjectAsIExpression(); }
    }

    #endregion

    #region IExpression Members

    ITypeReference IExpression.Type {
      get { return this.Type; }
    }

    #endregion

  }

  /// <summary>
  /// An expression that invokes an object constructor.
  /// </summary>
  public class CreateObjectInstance : ConstructorIndexerOrMethodCall, ICreateObjectInstance {

    /// <summary>
    /// Allocates an expression that invokes an object constructor.
    /// </summary>
    /// <param name="objectType">The type of object to create.</param>
    /// <param name="arguments">The arguments to pass to the constructor, indexer or method</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public CreateObjectInstance(TypeExpression objectType, IEnumerable<Expression> arguments, ISourceLocation sourceLocation)
      : base(arguments, sourceLocation) {
      this.objectType = objectType;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected CreateObjectInstance(BlockStatement containingBlock, CreateObjectInstance template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.objectType = (TypeExpression)template.objectType.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// Calls the visitor.Visit(ICreateObjectInstance) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(CreateObjectInstance) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Returns a collection of methods that represent the constructors for the named type.
    /// </summary>
    /// <param name="allowMethodParameterInferencesToFail">This flag is ignored, since constructors cannot have generic parameters.</param>
    public override IEnumerable<IMethodDefinition> GetCandidateMethods(bool allowMethodParameterInferencesToFail) {
      foreach (ITypeDefinitionMember member in this.Type.GetMembersNamed(this.NameTable.Ctor, false)) {
        IMethodDefinition/*?*/ meth = member as IMethodDefinition;
        if (meth != null && meth.IsSpecialName) yield return meth;
      }
    }

    /// <summary>
    /// True if the method to call is determined at run time.
    /// </summary>
    public override bool IsVirtualCall {
      get { return false; }
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new CreateObjectInstance(containingBlock, this);
    }

    /// <summary>
    /// The type of object to create.
    /// </summary>
    public TypeExpression ObjectType {
      get { return this.objectType; }
    }
    readonly TypeExpression objectType;

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      if (this.ResolvedMethod is Dummy && !this.HasErrors)
        // The only unresolved constructor call is a no-arg "new" on a value type.
        // Value types do not have no-arg constructors, but translate to IL "initobj TypeRef"
        return new DefaultValue(this.ObjectType, this.SourceLocation);
      return this;
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingExpression(Expression containingExpression) {
      base.SetContainingExpression(containingExpression);
      this.objectType.SetContainingExpression(this);
    }

    /// <summary>
    /// The type of value that the expression will evaluate to, as determined at compile time.
    /// </summary>
    public override ITypeDefinition Type {
      get { return this.objectType.ResolvedType; }
    }

    /// <summary>
    /// Called when the arguments are good and no type inferences have failed. This means that the callee could not be found. Complain.
    /// </summary>
    protected override void ComplainAboutCallee() {
      // If this.Type is a value type and this is a no-arg call, then do not issue an
      // error message.  The "new MyType()" AST node will be projected as "default(MyType)".
      uint argCount = IteratorHelper.EnumerableCount(this.OriginalArguments);
      if (this.Type.IsValueType && argCount == 0)
        return;

      List<IMethodDefinition> candidates = new List<IMethodDefinition>(this.GetCandidateMethods(true));

      // TODO: The candidate list will contain methods that do not even conform in number of parameters.
      // The list should be thinned out to those with matching signature length so a more specific and
      // correct error message may be given.  A corrected version of MethodIsEligible(x, x, true) maybe.

      if (candidates.Count > 1) {
        string cand0 = this.Helper.GetMethodSignature(candidates[0], NameFormattingOptions.Signature | NameFormattingOptions.ParameterModifiers);
        string cand1 = this.Helper.GetMethodSignature(candidates[1], NameFormattingOptions.Signature | NameFormattingOptions.ParameterModifiers);
        this.Helper.ReportError(new AstErrorMessage(this, Error.AmbiguousCall, cand0, cand1));
        return;
      } else if (candidates.Count == 1) {
        this.Helper.ReportError(new AstErrorMessage(this, Error.WrongNumberOfArgumentsInConstructorCall,
          this.Helper.GetTypeName(this.Type), argCount.ToString()));
      } else if (candidates.Count == 0) {
        this.Helper.ReportError(new AstErrorMessage(this, Error.WrongNumberOfArgumentsInConstructorCall,
          this.Helper.GetTypeName(this.Type), argCount.ToString()));
      } else
        Debug.Assert(false, "Candidate count should not be negative");
    }

    #region ICreateObjectInstance Members

    IEnumerable<IExpression> ICreateObjectInstance.Arguments {
      get {
        foreach (Expression convertedArgument in this.ConvertedArguments)
          yield return convertedArgument.ProjectAsIExpression();
      }
    }

    IMethodReference ICreateObjectInstance.MethodToCall {
      get { return this.ResolvedMethod; }
    }

    #endregion

    #region IExpression Members

    ITypeReference IExpression.Type {
      get { return this.Type; }
    }

    #endregion

  }

  /// <summary>
  /// An expression that invokes an object constructor that has already been resolved.
  /// Only for use during projection. I.e. do not contruct this node from a parser.
  /// </summary>
  public class CreateObjectInstanceForResolvedConstructor : CreateObjectInstance {

    /// <summary>
    /// An expression that invokes an object constructor that has already been resolved.
    /// Only for use during projection. I.e. do not contruct this node from a parser.
    /// </summary>
    /// <param name="resolvedConstructor">The constructor to invoke.</param>
    /// <param name="convertedArguments">The arguments to pass to the constructor, indexer or method. These are expected to already be converted to match the constructor's parameter types.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public CreateObjectInstanceForResolvedConstructor(IMethodDefinition resolvedConstructor, List<Expression> convertedArguments, ISourceLocation sourceLocation)
      : base(TypeExpression.For(resolvedConstructor.ContainingTypeDefinition), convertedArguments, sourceLocation) {
      this.convertedArguments = convertedArguments;
      this.resolvedConstructor = resolvedConstructor;
    }

    /// <summary>
    /// Returns the list of the arguments that was provided to the constructor. The caller of the constructor already performed the conversions, so this routine does nothing.
    /// </summary>
    protected override List<Expression> ConvertArguments() {
      return this.convertedArguments;
    }
    readonly List<Expression> convertedArguments;

    /// <summary>
    /// Returns an empty collection. This routine should never be called since ResolveMethod is overridden with a trivial implementation.
    /// </summary>
    public override IEnumerable<IMethodDefinition> GetCandidateMethods(bool allowMethodParameterInferencesToFail) {
      //^ assume false;
      return Enumerable<IMethodDefinition>.Empty;
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      //^ assume false; //This class should never be instantiated by a parser.
      return this;
    }

    /// <summary>
    /// Returns the constructor that was provided when this object was construct. Does not re-resolve the method.
    /// </summary>
    protected override IMethodDefinition ResolveMethod() {
      return this.resolvedConstructor;
    }
    readonly IMethodDefinition resolvedConstructor;

  }

  /// <summary>
  /// C# v3
  /// </summary>
  public class CreateAnonymousObject : Expression {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="initializers"></param>
    /// <param name="sourceLocation"></param>
    public CreateAnonymousObject(IEnumerable<Expression> initializers, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.initializers = initializers;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected CreateAnonymousObject(BlockStatement containingBlock, CreateAnonymousObject template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.initializers = Expression.CopyExpressions(template.initializers, containingBlock);
    }

    /// <summary>
    /// Calls the visitor.Visit(CreateAnonymousObject) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// A sequence of assignments that collectively initialize the new object instance.
    /// </summary>
    public IEnumerable<Expression> Initializers {
      get { return this.initializers; }
    }
    readonly IEnumerable<Expression> initializers;

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new CreateAnonymousObject(containingBlock, this);
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      return new DummyExpression(this.SourceLocation); //TODO: block expression
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingExpression(Expression containingExpression) {
      base.SetContainingExpression(containingExpression);
      foreach (Expression initializer in this.initializers) initializer.SetContainingExpression(this);
    }

    /// <summary>
    /// The type of value that the expression will evaluate to, as determined at compile time.
    /// </summary>
    public override ITypeDefinition Type {
      get { return Dummy.Type; }
    }

  }

  /// <summary>
  /// An expression that creates an array instance.
  /// </summary>
  public class CreateArray : Expression, ICreateArray, IMetadataCreateArray {

    /// <summary>
    /// Allocates an expression that creates an array instance.
    /// </summary>
    /// <param name="elementTypeExpression">A type expression for the element type of the array.</param>
    /// <param name="initializers">The initial values of the array elements. May be empty.</param>
    /// <param name="lowerBounds">A potentially empty list of expressions that supply lower bounds for the dimensions of the array instance.</param>
    /// <param name="rank">The number of dimensions of the array instance.</param>
    /// <param name="sizes">A list of expressions that supply sizes for the dimensions of the array instance. May be empty if the sizes are to be determined from the initializers.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public CreateArray(TypeExpression elementTypeExpression, IEnumerable<Expression> initializers, IEnumerable<Expression> lowerBounds,
      uint rank, IEnumerable<Expression> sizes, ISourceLocation sourceLocation)
      : base(sourceLocation)
      //^ requires rank > 0;
      // ^ requires count{Expression lb in lowerBounds} <= rank;
      // ^ requires count{Expression size in sizes} <= rank;
    {
      this.elementType = null;
      this.elementTypeExpression = elementTypeExpression;
      this.initializers = initializers;
      this.lowerBounds = lowerBounds;
      this.rank = rank;
      this.sizes = sizes;
    }

    /// <summary>
    /// Allocates an expression that creates an array instance.
    /// </summary>
    /// <param name="elementType">The element type of the array.</param>
    /// <param name="initializers">The initial values of the array elements. May be empty.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public CreateArray(ITypeReference elementType, IEnumerable<Expression> initializers, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.elementType = elementType;
      this.elementTypeExpression = null;
      this.initializers = initializers;
      this.lowerBounds = new Expression[0];
      this.rank = 1;
      // Note:
      // The C# V3 spec only requires that if both sizes AND an initializer are given the sizes must match the initializers and be constant.
      // However CSC rejects cases where the size expressions are not "int", but gives a "not constant" error message.
      // Thus, to avoid failure of the CSC-compatible test, the EnumerableCount must be cast to int type.
      this.sizes = new Expression[] { new CompileTimeConstant((int)IteratorHelper.EnumerableCount(initializers), SourceDummy.SourceLocation) };
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected CreateArray(BlockStatement containingBlock, CreateArray template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      if (template.elementTypeExpression != null)
        this.elementTypeExpression = (TypeExpression)template.elementTypeExpression.MakeCopyFor(containingBlock);
      this.initializers = Expression.CopyExpressions(template.initializers, containingBlock);
      this.lowerBounds = Expression.CopyExpressions(template.lowerBounds, containingBlock);
      this.rank = template.rank;
      this.sizes = Expression.CopyExpressions(template.sizes, containingBlock);
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      bool failed = this.Type is Dummy;
      if (!failed) {
        uint sizeCount = IteratorHelper.EnumerableCount(this.Sizes);
        uint initCount = IteratorHelper.EnumerableCount(this.Initializers);
        ITypeDefinition elemType = this.ElementType.ResolvedType;

        if (sizeCount > 0)
          foreach (Expression size in this.Sizes)
            failed |= size.HasErrors;
        if (failed) return true;

        if (initCount > 0) {
          foreach (Expression init in this.Initializers)
            failed |= init.HasErrors;
          if (failed) return true;

          foreach (Expression init in this.ConvertedInitializers)
            failed |= init.HasErrors;
          if (failed) return true;
        }

        AstErrorMessage message = null;
        if (initCount > 0) {
          // Check correctness of initializers.
          if (this.Rank == 1) {
            //   sizes must be 1 or zero. 
            //   if size is one, that size must be constant and equal to the initializer count;
            //   each intializer type must be compatible with array element type.
            if (sizeCount != 0) {
              Expression size0 = IteratorHelper.First<Expression>(this.Sizes);
              object sizeValue = size0.Value;
              if (sizeValue == null || !(sizeValue is int)) {
                this.Helper.ReportError(new AstErrorMessage(size0, Error.MustBeConstInt)); // Size must be constant int
                return true;
              }
              int size = (int)sizeValue;
              if (size != (int)initCount) {
                this.Helper.ReportError(new AstErrorMessage(size0, Error.ExplicitSizeDoesNotMatchInitializer, size.ToString(), initCount.ToString()));
                return true;
              }
            }
          } else if (this.rank > 1) {
            //   each initializer must define an array of the same shape
            //   if sizes is not empty, sizes must be constant and compatible with shape
            //   each initializer leaf element must be compatible with element type.
            IEnumerator<Expression> initializerEnumerator = this.Initializers.GetEnumerator();
            initializerEnumerator.MoveNext(); // Guaranteed by if predicate "initCount > 0"
            Expression elementZero = initializerEnumerator.Current;
            while (initializerEnumerator.MoveNext()) {
              Expression otherElement = initializerEnumerator.Current;
              if (!CreateArray.SameShape(elementZero, otherElement, ref message)) {
                this.Helper.ReportError(message);
                return true;
              }
            }
            if (sizeCount != 0 && !this.SameStaticShape(ref message)) {
              this.Helper.ReportError(message);
              return true;
            }
          }
        }
      }
      return failed;
    }

    private void ConvertInitializers() {
      ITypeDefinition elemType = this.ElementType.ResolvedType;
      if (this.Rank == 1) {
        List<Expression> convertedList = new List<Expression>();
        foreach (Expression exp in this.Initializers) {
          Expression convertedInit = this.Helper.ImplicitConversionInAssignmentContext(exp, elemType);
          convertedList.Add(convertedInit);
          if (convertedInit.HasErrors) {
            this.Helper.ReportFailedImplicitConversion(exp, elemType);
            this.hasErrors = true;
          }
        }
        this.convertedInitializers = convertedList;
      } else {
        CreateArray nestedCreate = null;
        foreach (Expression exp in this.Initializers) {
          if ((nestedCreate = exp as CreateArray) != null)
            nestedCreate.ConvertInitializers();
          else
            nestedCreate.hasErrors = true;
        }
        this.convertedInitializers = this.Initializers;
      }
    }

    /// <summary>
    /// Compares the shape of the first element of the initializer against
    /// another element of the initializer.  Called recursively to whatever
    /// depth the initializer is nested.
    /// </summary>
    /// <param name="first">First element of initializer</param>
    /// <param name="other">Other element of initializer</param>
    /// <param name="message">Error object, if returning false</param>
    /// <returns></returns>
    private static bool SameShape(Expression first, Expression other, ref AstErrorMessage message) {
      CreateArray sample = first as CreateArray;
      CreateArray example = other as CreateArray;
      if (sample == null && example == null)
        return true;
      int sampleCount = (sample == null ? 0 : (int)IteratorHelper.EnumerableCount(sample.Initializers));
      int exampleCount = (example == null ? 0 : (int)IteratorHelper.EnumerableCount(example.Initializers));
      if (sampleCount != exampleCount) {
        message = new AstErrorMessage(other, Error.InitializerCountInconsistent, sampleCount.ToString(), exampleCount.ToString());
        return false;
      } else {
        IEnumerator<Expression> enumerator = sample.Initializers.GetEnumerator();
        if (enumerator.MoveNext()) {
          Expression elementZero = enumerator.Current;
          while (enumerator.MoveNext())
            if (!CreateArray.SameShape(elementZero, enumerator.Current, ref message))
              return false;
        }
      }
      return true;
    }

    /// <summary>
    /// Recursively checks that each size expression is a compile time constant
    /// matching the number of elements at the corresponding level in the initializer.
    /// Also checks that size expressions are of int32 type.
    /// </summary>
    /// <returns>True if sizes match correctly</returns>
    private bool SameStaticShape(ref AstErrorMessage message) {
      IEnumerator<Expression> sizeEnumerator = this.Sizes.GetEnumerator();
      return CreateArray.LengthMatches(1, this, sizeEnumerator, ref message);
    }

    private static bool LengthMatches(int depth, Expression expr, IEnumerator<Expression> sizes, ref AstErrorMessage message) {
      // A match is achieved if the enumerator runs out at the same recursion level
      // as expr is an ordinary expression rather than a nested CreateArray object.
      CreateArray create = expr as CreateArray;
      if (sizes.MoveNext()) {
        object sizeValue;
        Expression size = sizes.Current; // Size at this dimension.
        CompileTimeConstant sizeConst = size as CompileTimeConstant;
        if ((sizeConst == null) ||
          ((sizeValue = sizeConst.Value) == null) ||
          !(sizeValue is int)) {
          message = new AstErrorMessage(size, Error.MustBeConstInt);
          return false;
        }
        if (create == null) // Mismatching rank is detected and reported elsewhere.
          return true;

        int initCount = (int)IteratorHelper.EnumerableCount(create.Initializers);
        int sizeCount = (int)sizeValue;
        if (sizeCount != initCount) {
          message = new AstErrorMessage(size, Error.ExplicitSizeDoesNotMatchInitializer, sizeCount.ToString(), initCount.ToString());
          return false;
        } else
          return CreateArray.LengthMatches(depth + 1, IteratorHelper.First(create.Initializers), sizes, ref message);
      } else
        return true;
    }

    /// <summary>
    /// The lowerbound for each dimension of the array instance.
    /// </summary>
    public IEnumerable<int> ComputedLowerBounds {
      get
        // ^ ensures count{int lb in result} == this.Rank;
      {
        int i = 1;
        foreach (Expression lowerBound in this.LowerBounds) {
          object/*?*/ val = lowerBound.Value;
          if (val is int)
            yield return (int)val;
          else
            yield return 0;
          i++;
        }
        while (i++ < this.Rank)
          yield return 0;
      }
    }

    /// <summary>
    /// The size of each dimension of the array instance.
    /// </summary>
    public IEnumerable<ulong> ComputedSizes {
      get
        // ^ ensures count{int lb in result;true} == this.Rank;
      {
        foreach (Expression size in this.ConvertedSizes) {
          IConvertible/*?*/ ic = size.Value as IConvertible;
          if (ic == null) yield return 0; //TODO: error
          switch (ic.GetTypeCode()) {
            case TypeCode.Int32: yield return (ulong)ic.ToInt32(null); break;
            case TypeCode.UInt32: yield return (ulong)ic.ToUInt32(null); break;
            case TypeCode.Int64: yield return (ulong)ic.ToInt64(null); break;
            case TypeCode.UInt64: yield return ic.ToUInt64(null); break;
            default: yield return 0; break; //TODO: error
          }
        }
      }
    }

    /// <summary>
    /// The size of each dimension of the array instance, implicitly converted to an integral type.
    /// When no explicit size expression is available, the size is computed from the initializers.
    /// If no initializers are present, the size defaults to zero.
    /// </summary>
    public IEnumerable<Expression> ConvertedSizes {
      get {
        int i = 0;
        foreach (Expression size in this.Sizes) {
          if (this.Helper.ImplicitConversionExists(size, this.PlatformType.SystemUInt32.ResolvedType))
            yield return this.Helper.ImplicitConversion(size, this.PlatformType.SystemUInt32.ResolvedType);
          else if (this.Helper.ImplicitConversionExists(size, this.PlatformType.SystemInt32.ResolvedType))
            yield return this.Helper.ImplicitConversion(size, this.PlatformType.SystemInt32.ResolvedType);
          else if (this.Helper.ImplicitConversionExists(size, this.PlatformType.SystemUInt64.ResolvedType))
            yield return this.Helper.ImplicitConversion(size, this.PlatformType.SystemUInt64.ResolvedType);
          else if (this.Helper.ImplicitConversionExists(size, this.PlatformType.SystemInt64.ResolvedType))
            yield return this.Helper.ImplicitConversion(size, this.PlatformType.SystemInt64.ResolvedType);
          else
            yield return this.Helper.ImplicitConversion(size, this.PlatformType.SystemInt32.ResolvedType);
          i++;
        }
        while (i < this.Rank) {
          ulong d = this.GetDimensionSizeFromInitializers(i);
          object o = null;
          if (d <= int.MaxValue) o = (int)d;
          else if (d <= uint.MaxValue) o = (uint)d;
          else if (d <= long.MaxValue) o = (long)d;
          else o = d;
          var s = new CompileTimeConstant(o, true, SourceDummy.SourceLocation);
          s.SetContainingExpression(this);
          yield return s;
          i++;
        }
      }
    }

    /// <summary>
    /// Calls the visitor.Visit(ICreateArray) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit((ICreateArray)this);
    }

    /// <summary>
    /// Calls the visitor.Visit(CreateArray) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The element type of the array.
    /// </summary>
    public ITypeReference ElementType {
      get {
        if (this.elementType == null)
          this.elementType = this.ElementTypeExpression.ResolvedType;
        return this.elementType;
      }
    }
    ITypeReference/*?*/ elementType;

    /// <summary>
    /// A type expression for the element type of the array.
    /// </summary>
    public TypeExpression ElementTypeExpression {
      get {
        if (this.elementTypeExpression == null) {
          this.elementTypeExpression = new NamedTypeExpression(new SimpleName(Dummy.Name, SourceDummy.SourceLocation, true));
          this.elementTypeExpression.SetContainingExpression(this);
        }
        return this.elementTypeExpression;
      }
    }
    TypeExpression/*?*/ elementTypeExpression;

    /// <summary>
    /// Calculates the size of the given dimension by counting up the number of initializer expressions in the given dimension.
    /// </summary>
    /// <param name="dimension">The dimension to size.</param>
    public ulong GetDimensionSizeFromInitializers(int dimension)
      //^ requires dimension >= 0;
    {
      if (dimension == 0) return (ulong)IteratorHelper.EnumerableCount(this.Initializers);
      foreach (Expression initialValue in this.Initializers) {
        CreateArray/*?*/ createSubArray = initialValue as CreateArray;
        if (createSubArray == null) continue;
        return createSubArray.GetDimensionSizeFromInitializers(dimension - 1);
      }
      return 0;
    }

    /// <summary>
    /// Infers the type of value that this expression will evaluate to. At runtime the actual value may be an instance of subclass of the result of this method.
    /// Calling this method does not cache the computed value and does not generate any error messages. In some cases, such as references to the parameters of lambda
    /// expressions during type overload resolution, the value returned by this method may be different from one call to the next.
    /// When type inference fails, Dummy.Type is returned.
    /// </summary>
    public override ITypeDefinition InferType() {
      if (this.Rank == 1 && IteratorHelper.EnumerableIsEmpty(this.LowerBounds)) {
        // TODO: check that all initializer values are implicitly convertible to this.ElementType.
        return Vector.GetVector(this.ElementType, this.Compilation.HostEnvironment.InternFactory);
      } else {
        // TODO: check that all initializer leaf values are implicitly convertible to this.ElementType.
        return Matrix.GetMatrix(this.ElementType, this.Rank, this.ComputedLowerBounds, this.ComputedSizes, this.Compilation.HostEnvironment.InternFactory);
      }
    }

    /// <summary>
    /// The initial values of the array elements. May be empty.
    /// </summary>
    public IEnumerable<Expression> Initializers {
      get { return this.initializers; }
    }
    readonly IEnumerable<Expression> initializers;

    /// <summary>
    /// Returns the initializers, modified by any required implicit conversion
    /// </summary>
    public IEnumerable<Expression> ConvertedInitializers {
      get {
        if (convertedInitializers == null)
          this.ConvertInitializers();
        return this.convertedInitializers;
      }
    }
    private IEnumerable<Expression> convertedInitializers = null;



    /// <summary>
    /// A potentially empty list of expressions that supply lower bounds for the dimensions of the array instance.
    /// </summary>
    public IEnumerable<Expression> LowerBounds {
      get { return this.lowerBounds; }
    }
    readonly IEnumerable<Expression> lowerBounds;

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new CreateArray(containingBlock, this);
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      return this;
    }

    /// <summary>
    /// The number of dimensions of the array instance.
    /// </summary>
    public uint Rank {
      get { return this.rank; }
    }
    readonly uint rank; //^ invariant rank > 0;

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingExpression(Expression containingExpression) {
      base.SetContainingExpression(containingExpression);
      if (this.elementTypeExpression != null)
        this.elementTypeExpression.SetContainingExpression(this);
      foreach (Expression initializer in this.initializers) initializer.SetContainingExpression(this);
      foreach (Expression lowerBound in this.lowerBounds) lowerBound.SetContainingExpression(this);
      foreach (Expression size in this.sizes) size.SetContainingExpression(this);
    }

    /// <summary>
    /// A list of expressions that supply sizes for the dimensions of the array instance. May be empty if the sizes are to be determined from the initializers.
    /// </summary>
    public IEnumerable<Expression> Sizes {
      get { return this.sizes; }
    }
    readonly IEnumerable<Expression> sizes;

    /// <summary>
    /// The type of value that the expression will evaluate to, as determined at compile time.
    /// </summary>
    public sealed override ITypeDefinition Type {
      get {
        if (this.type == null)
          this.type = this.InferType();
        return this.type;
      }
    }
    //^ [Once]
    ITypeDefinition/*?*/ type;

    #region ICreateArray Members

    ITypeReference ICreateArray.ElementType {
      get {
        if (this.elementTypeExpression != null)
          return this.elementTypeExpression.ResolvedType;
        else
          return this.ElementType;
      }
    }

    IEnumerable<IExpression> ICreateArray.Initializers {
      // Return the converted initializers rather than initializers as parsed. Also flattens the list if the rank is > 1.
      get {
        if (this.Rank == 1) {
          foreach (Expression initializer in this.ConvertedInitializers) yield return initializer.ProjectAsIExpression();
        } else {
          foreach (Expression initializer in this.ConvertedInitializers) {
            var nestedArray = initializer as ICreateArray;
            if (nestedArray != null) {
              foreach (var nestedInitializer in nestedArray.Initializers)
                yield return nestedInitializer;
            } else
              yield return initializer.ProjectAsIExpression();
          }
        }
      }
    }

    IEnumerable<int> ICreateArray.LowerBounds {
      get { return this.ComputedLowerBounds; }
    }

    IEnumerable<IExpression> ICreateArray.Sizes {
      get { foreach (Expression size in this.ConvertedSizes) yield return size.ProjectAsIExpression(); }
    }

    #endregion

    #region IMetadataCreateArray Members

    ITypeReference IMetadataCreateArray.ElementType {
      get { return ((ICreateArray)this).ElementType; }
    }

    IEnumerable<IMetadataExpression> IMetadataCreateArray.Initializers {
      get { foreach (Expression initializer in this.Initializers) yield return initializer.ProjectAsIMetadataExpression(); }
    }

    IEnumerable<int> IMetadataCreateArray.LowerBounds {
      get { return ((ICreateArray)this).LowerBounds; }
    }

    uint IMetadataCreateArray.Rank {
      get { return this.Rank; }
    }



    IEnumerable<ulong> IMetadataCreateArray.Sizes {
      get { return this.ComputedSizes; }
    }

    #endregion

    #region IMetadataExpression Members

    void IMetadataExpression.Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    ITypeReference IMetadataExpression.Type {
      get { return this.Type; }
    }

    #endregion

    #region IExpression Members

    ITypeReference IExpression.Type {
      get { return this.Type; }
    }

    #endregion
  }

  /// <summary>
  /// Creates an instance of the delegate type return by this.Type, using the method specified by this.MethodToCallViaDelegate.
  /// If the method is an instance method, then this.Instance specifies the expression that results in the instance on which the 
  /// method will be called.
  /// </summary>
  public class CreateDelegateInstance : Expression, ICreateDelegateInstance {

    /// <summary>
    /// Allocates an expression that creates an instance of the given delegate type, using the method specified by methodToCallViaDelegate.
    /// If the method is an instance method, then this.Instance specifies the expression that results in the instance on which the 
    /// method will be called.
    /// </summary>
    /// <param name="delegateType">The type of delegate value this expression will create.</param>
    /// <param name="methodToCallViaDelegate">The method that is to be be called when the delegate instance is invoked.</param>
    /// <param name="instance">An expression that evaluates to the instance (if any) on which this.MethodToCallViaDelegate must be called (via the delegate).</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public CreateDelegateInstance(Expression/*?*/ instance, ITypeDefinition delegateType, IMethodDefinition methodToCallViaDelegate, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.instance = instance;
      this.type = delegateType;
      this.methodToCallViaDelegate = methodToCallViaDelegate;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected CreateDelegateInstance(BlockStatement containingBlock, CreateDelegateInstance template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      if (template.Instance != null)
        this.instance = template.Instance.MakeCopyFor(containingBlock);
      this.type = template.type;
      this.methodToCallViaDelegate = template.methodToCallViaDelegate;
    }

    /// <summary>
    /// Calls the visitor.Visit(ICreateDelegateInstance) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(CreateDelegateInstance) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// An expression that evaluates to the instance (if any) on which this.MethodToCallViaDelegate must be called (via the delegate).
    /// </summary>
    public Expression/*?*/ Instance {
      get
        //^ ensures this.MethodToCallViaDelegate.IsStatic <==> result == null;
      {
        //^ assume false; //TODO: need some work here
        return this.instance;
      }
    }
    readonly Expression/*?*/ instance;

    /// <summary>
    /// True if the delegate encapsulates a virtual method.
    /// </summary>
    public bool IsVirtualDelegate {
      get { return this.methodToCallViaDelegate.IsVirtual; }
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (this.ContainingBlock == containingBlock) return this;
      return new CreateDelegateInstance(containingBlock, this);
    }

    /// <summary>
    /// The method that is to be be called when the delegate instance is invoked.
    /// </summary>
    public IMethodDefinition MethodToCallViaDelegate {
      get { return this.methodToCallViaDelegate; }
    }
    readonly IMethodDefinition methodToCallViaDelegate;

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      return this;
    }

    /// <summary>
    /// The type of value that the expression will evaluate to, as determined at compile time.
    /// </summary>
    public override ITypeDefinition Type {
      get { return this.type; }
    }
    readonly ITypeDefinition type;

    #region ICreateDelegateInstance Members

    IExpression/*?*/ ICreateDelegateInstance.Instance {
      get
        //^^ ensures this.MethodToCallViaDelegate.Method.IsStatic <==> result == null;
      {
        if (this.Instance == null) return null;
        return this.Instance.ProjectAsIExpression();
      }
    }

    IMethodReference ICreateDelegateInstance.MethodToCallViaDelegate {
      get { return this.MethodToCallViaDelegate; }
    }

    #endregion

    #region IExpression Members

    ITypeReference IExpression.Type {
      get { return this.Type; }
    }

    #endregion
  }

  /// <summary>
  /// An expression that creates an instance of an array whose element type is determined by the initial values of the elements.
  /// </summary>
  public class CreateImplicitlyTypedArray : Expression {

    /// <summary>
    /// Allocates an expression that creates an instance of an array whose element type is determined by the initial values of the elements.
    /// </summary>
    /// <param name="initializers">The initial values of the array elements. May not be empty. Used to determine the element type of the array.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public CreateImplicitlyTypedArray(IEnumerable<Expression> initializers, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.initializers = initializers;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected CreateImplicitlyTypedArray(BlockStatement containingBlock, CreateImplicitlyTypedArray template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.initializers = Expression.CopyExpressions(template.initializers, containingBlock);
    }

    /// <summary>
    /// Calls the visitor.Visit(CreateImplicitlyTypedArray) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The element type of the array. This is computed from the types of the intial values of the elements.
    /// </summary>
    public ITypeDefinition ElementType {
      get { return Dummy.Type; } //TODO: unify types of all the initializer expressions
    }

    /// <summary>
    /// Infers the type of value that this expression will evaluate to. At runtime the actual value may be an instance of subclass of the result of this method.
    /// Calling this method does not cache the computed value and does not generate any error messages. In some cases, such as references to the parameters of lambda
    /// expressions during type overload resolution, the value returned by this method may be different from one call to the next.
    /// When type inference fails, Dummy.Type is returned.
    /// </summary>
    public override ITypeDefinition InferType() {
      return Vector.GetVector(this.ElementType, this.Compilation.HostEnvironment.InternFactory);
    }

    /// <summary>
    /// The initial values of the array elements. May not be empty. Used to determine the element type of the array.
    /// </summary>
    public IEnumerable<Expression> Initializers {
      get { return this.initializers; }
    }
    readonly IEnumerable<Expression> initializers;

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new CreateImplicitlyTypedArray(containingBlock, this);
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      CreateArray result = new CreateArray(this.ElementType.ResolvedType, this.Initializers, this.SourceLocation);
      result.SetContainingExpression(this);
      return result;
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingExpression(Expression containingExpression) {
      base.SetContainingExpression(containingExpression);
      foreach (Expression initializer in this.initializers) initializer.SetContainingExpression(this);
    }

    /// <summary>
    /// The type of value that the expression will evaluate to, as determined at compile time.
    /// </summary>
    public sealed override ITypeDefinition Type {
      get {
        if (this.type == null)
          this.type = this.InferType();
        return this.type;
      }
    }
    ITypeDefinition/*?*/ type;

  }

  /// <summary>
  /// An expression that allocates an array on the call stack
  /// </summary>
  public class CreateStackArray : Expression, IStackArrayCreate {

    /// <summary>
    /// Allocates an expression that allocates an array on the call stack.
    /// </summary>
    /// <param name="elementType">The type of the elements of the stack array.</param>
    /// <param name="size">The size (number of elements) of the stack array.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public CreateStackArray(TypeExpression elementType, Expression size, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.elementType = elementType;
      this.size = size;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected CreateStackArray(BlockStatement containingBlock, CreateStackArray template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.elementType = (TypeExpression)template.elementType.MakeCopyFor(containingBlock);
      this.size = template.size.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// Calls the visitor.Visit(IStackArrayCreate) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(StackArrayCreate) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Infers the type of value that this expression will evaluate to. At runtime the actual value may be an instance of subclass of the result of this method.
    /// Calling this method does not cache the computed value and does not generate any error messages. In some cases, such as references to the parameters of lambda
    /// expressions during type overload resolution, the value returned by this method may be different from one call to the next.
    /// When type inference fails, Dummy.Type is returned.
    /// </summary>
    public override ITypeDefinition InferType() {
      return PointerType.GetPointerType(this.ElementType.ResolvedType, this.Compilation.HostEnvironment.InternFactory);
    }

    /// <summary>
    /// The type of the elements of the stack array.
    /// </summary>
    public TypeExpression ElementType {
      get { return this.elementType; }
    }
    readonly TypeExpression elementType;

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^ ensures result.GetType() == this.GetType();
      //^ ensures result.ContainingBlock == containingBlock;
    {
      if (this.ContainingBlock == containingBlock) return this;
      return new CreateStackArray(containingBlock, this);
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      return this;
    }

    /// <summary>
    /// The size (number of elements) of the stack array.
    /// </summary>
    public Expression Size {
      get { return this.size; }
    }
    readonly Expression size;

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingExpression(Expression containingExpression) {
      this.ElementType.SetContainingExpression(containingExpression);
      this.Size.SetContainingExpression(containingExpression);
      base.SetContainingExpression(containingExpression);
    }

    /// <summary>
    /// The type of value that the expression will evaluate to, as determined at compile time.
    /// </summary>
    public sealed override ITypeDefinition Type {
      get {
        if (this.type == null)
          this.type = this.InferType();
        return this.type;
      }
    }
    //^ [Once]
    ITypeDefinition/*?*/ type;

    #region IStackArrayCreate Members

    ITypeReference IStackArrayCreate.ElementType {
      get { return this.ElementType.ResolvedType; }
    }

    IExpression IStackArrayCreate.Size {
      get {
        CompileTimeConstant elementSize = new CompileTimeConstant(TypeHelper.SizeOfType(this.ElementType.ResolvedType), this.ElementType.SourceLocation);
        Multiplication sizeInBytes = new Multiplication(this.Size, elementSize, this.Size.SourceLocation);
        sizeInBytes.SetContainingExpression(this);
        return sizeInBytes.ProjectAsIExpression();
      }
    }

    #endregion

    #region IExpression Members

    ITypeReference IExpression.Type {
      get { return this.Type; }
    }

    #endregion

  }

  /// <summary>
  /// An expression that results in the default value of a given type.
  /// </summary>
  public class DefaultValue : Expression, IDefaultValue {

    /// <summary>
    /// Allocates an expression that results in the default value of a given type.
    /// </summary>
    /// <param name="defaultValueType">The type whose default value is the result of this expression.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public DefaultValue(TypeExpression defaultValueType, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.defaultValueType = defaultValueType;
    }

    /// <summary>
    /// Allocates a default value expression that is equivalent in effect to the given nullLiteral when converted to the given type.
    /// </summary>
    /// <param name="nullLiteral">A null literal expression.</param>
    /// <param name="type">The type whose default value is the result of the expression being allocated.</param>
    public DefaultValue(NullLiteral nullLiteral, ITypeDefinition type)
      : base(nullLiteral.SourceLocation) {
      this.defaultValueType = TypeExpression.For(type);
      this.SetContainingExpression(nullLiteral);
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected DefaultValue(BlockStatement containingBlock, DefaultValue template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.defaultValueType = (TypeExpression)template.defaultValueType.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// The type whose default value is the result of this expression.
    /// </summary>
    public TypeExpression DefaultValueType {
      get { return this.defaultValueType; }
    }
    readonly TypeExpression defaultValueType;

    /// <summary>
    /// Calls the visitor.Visit(IDefaultValue) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(DefaultValue) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Computes the compile time value of the expression. Can be null.
    /// </summary>
    protected override object/*?*/ GetValue() {
      switch (this.Type.TypeCode) {
        case PrimitiveTypeCode.Boolean:
          return false;
        case PrimitiveTypeCode.Int8:
          return (sbyte)0;
        case PrimitiveTypeCode.UInt8:
          return (byte)0;
        case PrimitiveTypeCode.Char:
          return (char)0;
        case PrimitiveTypeCode.Int16:
          return (short)0;
        case PrimitiveTypeCode.UInt16:
          return (ushort)0;
        case PrimitiveTypeCode.Int32:
          return (int)0;
        case PrimitiveTypeCode.UInt32:
          return (uint)0;
        case PrimitiveTypeCode.Float32:
          return (float)0;
        case PrimitiveTypeCode.Float64:
          return (double)0;
        case PrimitiveTypeCode.NotPrimitive:
          if (TypeHelper.TypesAreEquivalent(this.Type, this.PlatformType.SystemDecimal))
            return (decimal)0;
          break;
      }
      return null;
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new DefaultValue(containingBlock, this);
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      return this;
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingExpression(Expression containingExpression) {
      base.SetContainingExpression(containingExpression);
      this.defaultValueType.SetContainingExpression(this);
    }

    /// <summary>
    /// The type of value that the expression will evaluate to, as determined at compile time.
    /// </summary>
    public override ITypeDefinition Type {
      get { return this.DefaultValueType.ResolvedType; }
    }

    #region IDefaultValue Members

    ITypeReference IDefaultValue.DefaultValueType {
      get { return this.DefaultValueType.ResolvedType; }
    }

    #endregion

    #region IExpression Members

    ITypeReference IExpression.Type {
      get { return this.Type; }
    }

    #endregion

  }

  /// <summary>
  /// An expression that divides the value of the left operand by the value of the right operand. 
  /// When the operator is overloaded, this expression corresponds to a call to op_Division.
  /// </summary>
  public class Division : BinaryOperation, IDivision {

    /// <summary>
    /// Allocates an expression that divides the value of the left operand by the value of the right operand. 
    /// When the operator is overloaded, this expression corresponds to a call to op_Division.
    /// </summary>
    /// <param name="leftOperand">The left operand.</param>
    /// <param name="rightOperand">The right operand.</param>
    /// <param name="sourceLocation">The source location of the operation.</param>
    public Division(Expression leftOperand, Expression rightOperand, ISourceLocation sourceLocation)
      : base(leftOperand, rightOperand, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected Division(BlockStatement containingBlock, Division template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// </summary>
    /// <returns></returns>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return base.CheckForErrorsAndReturnTrueIfAnyAreFound();
    }

    /// <summary>
    /// Calls the visitor.Visit(IDivision) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(Division) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Returns the string used to identify the operator in error messages
    /// </summary>
    protected override string OperationSymbolForErrorMessage {
      get { return "/"; }
    }

    /// <summary>
    /// Returns the name a method must have to overload this operator.
    /// </summary>
    protected override IName GetOperatorName() {
      return this.NameTable.OpDivision;
    }

    /// <summary>
    /// Computes the compile time value of the expression. Can be null.
    /// </summary>
    protected override object/*?*/ GetValue() {
      object/*?*/ left = this.ConvertedLeftOperand.Value;
      object/*?*/ right = this.ConvertedRightOperand.Value;
      if (left == null || right == null) return null;
      switch (System.Convert.GetTypeCode(left)) {
        case TypeCode.Int32:
          //^ assume left is int && right is int;
          int ri = (int)right;
          if (ri == 0) return null;
          return (int)left / ri;
        case TypeCode.UInt32:
          //^ assume left is uint && right is uint;
          uint rui = (uint)right;
          if (rui == 0) return null;
          return (uint)left / rui;
        case TypeCode.Int64:
          //^ assume left is long && right is long;
          long rl = (long)right;
          if (rl == 0) return null;
          return (long)left / rl;
        case TypeCode.UInt64:
          //^ assume left is ulong && right is ulong;
          ulong rul = (ulong)right;
          if (rul == 0) return null;
          return (ulong)left / rul;
        case TypeCode.Single:
          //^ assume left is float && right is float;
          return (float)left / (float)right;
        case TypeCode.Double:
          //^ assume left is double && right is double;
          return (double)left / (double)right;
        case TypeCode.Decimal:
          //^ assume left is decimal && right is decimal;
          return (decimal)left / (decimal)right;
      }
      return null;
    }

    /// <summary>
    /// Returns true if no information is lost if the integer value of this expression is converted to the target integer type.
    /// </summary>
    public override bool IntegerConversionIsLossless(ITypeDefinition targetType) {
      if (TypeHelper.IsUnsignedPrimitiveInteger(this.RightOperand.Type))
        return this.LeftOperand.IntegerConversionIsLossless(targetType); //Integer division by a positive integer can only make result smaller than or equal to left operand
      return base.IntegerConversionIsLossless(targetType);
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new Division(containingBlock, this);
    }

    /// <summary>
    /// A list of dummy methods that correspond to operations that are built into IL. The dummy methods are used, via overload resolution,
    /// to determine how the operands are to be converted before the operation is carried out.
    /// </summary>
    protected override IEnumerable<IMethodDefinition> StandardOperators {
      get {
        BuiltinMethods dummyMethods = this.Compilation.BuiltinMethods;
        yield return dummyMethods.Int32opInt32;
        yield return dummyMethods.UInt32opUInt32;
        yield return dummyMethods.Int64opInt64;
        yield return dummyMethods.UInt64opUInt64;
        yield return dummyMethods.Float32opFloat32;
        yield return dummyMethods.Float64opFloat64;
        yield return dummyMethods.DecimalOpDecimal;
      }
    }

    /// <summary>
    /// If true the operands must be integers and are treated as being unsigned for the purpose of the division.
    /// </summary>
    public virtual bool TreatOperandsAsUnsignedIntegers {
      get { return TypeHelper.IsUnsignedPrimitiveInteger(this.ConvertedLeftOperand.Type) && TypeHelper.IsUnsignedPrimitiveInteger(this.ConvertedRightOperand.Type); }
    }

  }

  /// <summary>
  /// An expression that divides the value of the left operand by the value of the right operand. 
  /// The result of the expression is assigned to the left operand, which must be a target expression.
  /// When the operator is overloaded, this expression corresponds to a call to op_Division.
  /// </summary>
  public class DivisionAssignment : BinaryOperationAssignment {

    /// <summary>
    /// Allocates an expression that divides the value of the left operand by the value of the right operand. 
    /// The result of the expression is assigned to the left operand, which must be a target expression.
    /// When the operator is overloaded, this expression corresponds to a call to op_Division.
    /// </summary>
    /// <param name="leftOperand">The left operand and target of the assignment.</param>
    /// <param name="rightOperand">The right operand.</param>
    /// <param name="sourceLocation">The source location of the operation.</param>
    public DivisionAssignment(TargetExpression leftOperand, Expression rightOperand, ISourceLocation sourceLocation)
      : base(leftOperand, rightOperand, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected DivisionAssignment(BlockStatement containingBlock, DivisionAssignment template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Calls the visitor.Visit(DivisionAssignment) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (this.ContainingBlock == containingBlock) return this;
      return new DivisionAssignment(containingBlock, this);
    }

    /// <summary>
    /// Creates a division expression with the given left operand and this.RightOperand.
    /// The method does not use this.LeftOperand.Expression, since it may be necessary to factor out any subexpressions so that
    /// they are evaluated only once. The given left operand expression is expected to be the expression that remains after factoring.
    /// </summary>
    /// <param name="leftOperand">An expression to combine with this.RightOperand into a binary expression.</param>
    protected override Expression CreateBinaryExpression(Expression leftOperand) {
      Expression result = new Division(leftOperand, this.RightOperand, this.SourceLocation);
      result.SetContainingExpression(this);
      return result;
    }
  }

  /// <summary>
  /// A dummy method call expression for use during error recovery. 
  /// </summary>
  internal sealed class DummyMethodCall : MethodCall {

    /// <summary>
    /// Allocates a dummy method call expression for use during error recovery. 
    /// </summary>
    /// <param name="operationExpression">An operation expression that failed to match an operator overload.</param>
    internal DummyMethodCall(Expression operationExpression)
      : base(operationExpression, new List<Expression>(0), operationExpression.SourceLocation) {
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block. This method should never be called.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      //^ assume false;
      return this;
    }

    /// <summary>
    /// Returns an empty collection. This method should never be called.
    /// </summary>
    public override IEnumerable<IMethodDefinition> GetCandidateMethods(bool allowMethodParameterInferencesToFail) {
      //^ assume false;
      return Enumerable<IMethodDefinition>.Empty;
    }
  }

  /// <summary>
  /// A dummy expression for miscellaneous uses.
  /// </summary>
  public sealed class DummyExpression : Expression, IExpression {

    /// <summary>
    /// Allocates a dummy expression for use during error recovery.
    /// </summary>
    /// <param name="sourceLocation">The source location associated with the AST node that projects onto this expression.</param>
    public DummyExpression(ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.type = Dummy.Type;
    }

    /// <summary>
    /// Allocates a dummy expression for use in calls to SetContainingExpression.
    /// </summary>
    /// <param name="containingBlock">A containing block for the expression. Used by calls to SetContainingExpression.</param>
    /// <param name="sourceLocation">The source location associated with the AST node that projects onto this expression.</param>
    public DummyExpression(BlockStatement containingBlock, ISourceLocation sourceLocation)
      : base(containingBlock, sourceLocation) {
      this.type = Dummy.Type;
    }

    /// <summary>
    /// Allocates a dummy expression for use by the method overload resolution machinary when converting a method group to a delegate.
    /// </summary>
    /// <param name="containingBlock">A containing block for the expression. Used by calls to SetContainingExpression.</param>
    /// <param name="sourceLocation">The source location associated with the AST node that projects onto this expression.</param>
    /// <param name="type">The type of the result. This matters because the result is going to be argument supplied to the method overload machinary.</param>
    public DummyExpression(BlockStatement containingBlock, ISourceLocation sourceLocation, ITypeDefinition type)
      : base(containingBlock, sourceLocation) {
      this.type = type;
    }

    /// <summary>
    /// Does nothing. Should never be called.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      //^ assume false;
    }

    /// <summary>
    /// Does nothing. Should never be called.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      //^ assume false;
    }

    /// <summary>
    /// Does nothing. Should never be called.
    /// </summary>
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      //^ assume false;
      return this;
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      return this;
    }

    /// <summary>
    /// The type of value that the expression will evaluate to, as determined at compile time.
    /// </summary>
    public override ITypeDefinition Type {
      get { return this.type; }
    }
    readonly ITypeDefinition type;

    #region IExpression Members

    ITypeReference IExpression.Type {
      get { return this.Type; }
    }

    #endregion

  }

  /// <summary>
  /// A type expression that does not resolve to a type, but serves only to denote a "missing" type expression.
  /// For example, the following C# type expression "typeof(GenericType&lt;,&gt;)" will feature a list of two empty type expressions.
  /// </summary>
  public class EmptyTypeExpression : TypeExpression {

    /// <summary>
    /// Allocates a type expression that does not resolve to a type, but serves only to denote a "missing" type expression.
    /// For example, the following C# type expression "typeof(GenericType&lt;,&gt;)" will feature a list of two empty type expressions.
    /// </summary>
    public EmptyTypeExpression(ISourceLocation sourceLocation)
      : base(sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected EmptyTypeExpression(BlockStatement containingBlock, EmptyTypeExpression template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (this.ContainingBlock == containingBlock) return this;
      return new EmptyTypeExpression(containingBlock, this);
    }

    /// <summary>
    /// Always returns Dummy.Type.
    /// </summary>
    protected override ITypeDefinition Resolve() {
      //^ assume false; //It is an error to include an empty type expression in a context where Resolve will be called.
      return Dummy.Type;
    }

    /// <summary>
    /// Calls visitor.Visit(EmptyTypeExpression).
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

  }

  /// <summary>
  /// An expression that results in true if both operands represent the same value or object. When overloaded, this expression corresponds to a call to op_Equality.
  /// </summary>
  public class Equality : EqualityComparison, IEquality {

    /// <summary>
    /// Allocates an expression that results in true if both operands represent the same value or object. When overloaded, this expression corresponds to a call to op_Equality.
    /// </summary>
    public Equality(Expression leftOperand, Expression rightOperand, ISourceLocation sourceLocation)
      : base(leftOperand, rightOperand, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected Equality(BlockStatement containingBlock, Equality template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Returns a call to the getter of the HasValue property of the type of the given operand, if such a getter can be found. 
    /// If not, it returns this.
    /// </summary>
    /// <param name="operand">An operand that is being tested for equality or inequality with the null literal.</param>
    protected override Expression CallHasValue(Expression operand) {
      Expression result = base.CallHasValue(operand);
      if (result != this) {
        //^ assume result is ResolvedMethodCall;
        result = new LogicalNot(result, this.SourceLocation);
        result.SetContainingExpression(this);
      }
      return result;
    }

    /// <summary>
    /// Calls the visitor.Visit(IEquality) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(Equality) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Returns the string used to identify the operator in error messages
    /// </summary>
    protected override string OperationSymbolForErrorMessage {
      get { return "=="; }
    }


    /// <summary>
    /// Returns the name a method must have to overload this operator.
    /// </summary>
    protected override IName GetOperatorName() {
      return this.NameTable.OpEquality;
    }

    /// <summary>
    /// Computes the compile time value of the expression. Can be null.
    /// </summary>
    protected override object/*?*/ GetValue() {
      object/*?*/ left = this.ConvertedLeftOperand.Value;
      object/*?*/ right = this.ConvertedRightOperand.Value;
      if (left == null || right == null) return null;
      switch (System.Convert.GetTypeCode(left)) {
        case TypeCode.Int32:
          //^ assume left is int && right is int;
          return (int)left == (int)right;
        case TypeCode.UInt32:
          //^ assume left is uint && right is uint;
          return (uint)left == (uint)right;
        case TypeCode.Int64:
          //^ assume left is long && right is long;
          return (long)left == (long)right;
        case TypeCode.UInt64:
          //^ assume left is ulong && right is ulong;
          return (ulong)left == (ulong)right;
        case TypeCode.Single:
          //^ assume left is float && right is float;
          return (float)left == (float)right;
        case TypeCode.Double:
          //^ assume left is double && right is double;
          return (double)left == (double)right;
        case TypeCode.Decimal:
          //^ assume left is decimal && right is decimal;
          return (decimal)left == (decimal)right;
        case TypeCode.Boolean:
          //^ assume left is bool && right is bool;
          return (bool)left == (bool)right;
        case TypeCode.String:
          //^ assume left is string && right is string;
          return (string)left == (string)right;
        case TypeCode.Object:
          if (left is IntPtr && right is IntPtr)
            return (IntPtr)left == (IntPtr)right;
          break;
      }
      return null;
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new Equality(containingBlock, this);
    }

  }

  /// <summary>
  /// An equality or inequality comparison performed on a left and right operand.
  /// </summary>
  public abstract class EqualityComparison : Comparison {
    /// <summary>
    /// Initializes a comparison operation performed on a left and right operand.
    /// </summary>
    /// <param name="leftOperand">The left operand.</param>
    /// <param name="rightOperand">The right operand.</param>
    /// <param name="sourceLocation">The source location of the operation.</param>
    protected EqualityComparison(Expression leftOperand, Expression rightOperand, ISourceLocation sourceLocation)
      : base(leftOperand, rightOperand, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected EqualityComparison(BlockStatement containingBlock, Comparison template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Returns a call to the getter of the HasValue property of the type of the given operand, if such a getter can be found. 
    /// If not, it returns this.
    /// </summary>
    /// <param name="operand">An operand that is being tested for equality or inequality with the null literal.</param>
    protected virtual Expression CallHasValue(Expression operand)
      //^ requires operand.Type.IsValueType;
    {
      List<Expression> args = new List<Expression>(0);
      foreach (ITypeDefinitionMember member in operand.Type.GetMembersNamed(this.NameTable.HasValue, false)) {
        IPropertyDefinition/*?*/ hasValue = member as IPropertyDefinition;
        if (hasValue == null) continue;
        IMethodReference/*?*/ get_HasValue = hasValue.Getter;
        if (get_HasValue == null || get_HasValue.ResolvedMethod.IsStatic || IteratorHelper.EnumerableIsNotEmpty(get_HasValue.Parameters)) {
          //^ assume false;
          continue;
        }
        operand = new AddressOf(new AddressableExpression(operand), true, operand.SourceLocation);
        operand.SetContainingExpression(this);
        ResolvedMethodCall hasValueCall = new ResolvedMethodCall(get_HasValue.ResolvedMethod, operand, args, this.SourceLocation);
        hasValueCall.SetContainingExpression(this);
        return hasValueCall;
      }
      //^ assume false;
      return this;
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      if (this.LeftOperand.HasErrors || this.RightOperand.HasErrors) return true;
      //The point of all this is to check that the operands are not value types and are not known at compile time to never be equal or never be unequal.
      MethodCall/*?*/ overloadCall = this.OverloadMethodCall;
      if (overloadCall != null && overloadCall.ResolvedMethod.Name != this.NameTable.ObjectOpObject) return false;
      ITypeDefinition ltype = this.LeftOperand.Type;
      ITypeDefinition rtype = this.RightOperand.Type;
      if (ltype.IsInterface && rtype.IsReferenceType && !rtype.IsSealed) return false;
      if (rtype.IsInterface && ltype.IsReferenceType && !ltype.IsSealed) return false;
      if (!this.Helper.ImplicitConversionExists(this.LeftOperand, rtype) && !this.Helper.ImplicitConversionExists(this.RightOperand, ltype)) {
        this.ReportBadOperands();
        return true;
      }
      if (!ltype.IsReferenceType) {
        ICompileTimeConstant/*?*/ c = this.RightOperand.ProjectAsIExpression() as ICompileTimeConstant;
        if (c != null && c.Value == null) {
          //The right operand is the null literal. This is OK as long as the left operand is a nullable value type or a generic parameter that is not constrained to be a value type.
          if (!ltype.IsValueType || this.Helper.RemoveNullableWrapper(ltype) != ltype) return false;
        }
        this.ReportBadOperands();
        return true;
      }
      if (!rtype.IsReferenceType) {
        ICompileTimeConstant/*?*/ c = this.LeftOperand.ProjectAsIExpression() as ICompileTimeConstant;
        if (c != null && c.Value == null) {
          //The left operand is the null literal. This is OK as long as the right operand is a nullable value type or a generic parameter that is not constrained to be a value type.
          if (!rtype.IsValueType || this.Helper.RemoveNullableWrapper(rtype) != rtype) return false;
        }
        this.ReportBadOperands();
        return true;
      }
      if (TypeHelper.TypesAreEquivalent(ltype, ltype.PlatformType.SystemObject) && this.TypeHasUserDefinedEqualityOperator(rtype)) {
        string rightOperandTypeName = this.Helper.GetTypeName(rtype);
        this.Helper.ReportError(new AstErrorMessage(this, Error.BadReferenceCompareLeft, rightOperandTypeName));
        return false;
      }
      if (TypeHelper.TypesAreEquivalent(rtype, rtype.PlatformType.SystemObject) && this.TypeHasUserDefinedEqualityOperator(ltype)) {
        string leftOperandTypeName = this.Helper.GetTypeName(ltype);
        this.Helper.ReportError(new AstErrorMessage(this, Error.BadReferenceCompareRight, leftOperandTypeName));
        return false;
      }
      return false;
    }

    /// <summary>
    /// Reports an error indicating that an equality comparison cannot be carried out on these types of operands.
    /// </summary>
    private void ReportBadOperands() {
      this.Helper.ReportError(this.GetBinaryBadOperandsTypeErrorMessage());
    }

    /// <summary>
    /// Returns true if the given type or one of its base types defines an overload for == or !=.
    /// </summary>
    private bool TypeHasUserDefinedEqualityOperator(ITypeDefinition type) {
      ITypeDefinition/*?*/ t = type;
      while (t != null) {
        t = this.Helper.RemoveNullableWrapper(t);
        foreach (ITypeDefinitionMember member in type.GetMembersNamed(this.GetOperatorName(), false)) {
          IMethodDefinition/*?*/ method = member as IMethodDefinition;
          if (method == null || !method.IsStatic) continue;
          bool eligible = false;
          IEnumerator<IParameterDefinition> paramEnumerator = method.Parameters.GetEnumerator();
          if (!paramEnumerator.MoveNext()) continue;
          if (this.Helper.ImplicitConversionExists(type, paramEnumerator.Current.Type.ResolvedType)) eligible = true;
          if (!paramEnumerator.MoveNext()) continue;
          if (this.Helper.ImplicitConversionExists(type, paramEnumerator.Current.Type.ResolvedType)) eligible = true;
          if (paramEnumerator.MoveNext()) continue;
          if (eligible) return true;
        }
        t = TypeHelper.BaseClass(t);
      }
      return false;
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      if (!this.HasErrors) {
        IExpression result = base.ProjectAsNonConstantIExpression();
        if (result == this) {
          if (this.LeftOperand.Type.IsValueType && this.RightOperand.Type.IsReferenceType) {
            //^ assume this.RightOperand is ICompileTimeConstant;
            return this.CallHasValue(this.LeftOperand).ProjectAsIExpression();
          }
          if (this.RightOperand.Type.IsValueType && this.LeftOperand.Type.IsReferenceType) {
            //^ assume this.LeftOperand is ICompileTimeConstant;
            return this.CallHasValue(this.RightOperand).ProjectAsIExpression();
          }
        }
        return result;
      }
      return new DummyExpression(this.SourceLocation);
    }

  }

  /// <summary>
  /// An expression that computes the bitwise exclusive or of the left and right operands. 
  /// When the operator is overloaded, this expression corresponds to a call to op_ExclusiveOr.
  /// </summary>
  public class ExclusiveOr : BinaryOperation, IExclusiveOr {

    /// <summary>
    /// Allocates an expression that computes the bitwise exclusive or of the left and right operands. 
    /// When the operator is overloaded, this expression corresponds to a call to op_ExclusiveOr.
    /// </summary>
    /// <param name="leftOperand">The left operand.</param>
    /// <param name="rightOperand">The right operand.</param>
    /// <param name="sourceLocation">The source location of the operation.</param>
    public ExclusiveOr(Expression leftOperand, Expression rightOperand, ISourceLocation sourceLocation)
      : base(leftOperand, rightOperand, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected ExclusiveOr(BlockStatement containingBlock, ExclusiveOr template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Calls the visitor.Visit(IExclusiveOr) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(ExclusiveOr) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Returns the string used to identify the operator in error messages
    /// </summary>
    protected override string OperationSymbolForErrorMessage {
      get { return "^"; }
    }


    /// <summary>
    /// Returns the name a method must have to overload this operator.
    /// </summary>
    protected override IName GetOperatorName() {
      return this.NameTable.OpExclusiveOr;
    }

    /// <summary>
    /// Computes the compile time value of the expression. Can be null.
    /// </summary>
    protected override object/*?*/ GetValue() {
      object/*?*/ left = this.ConvertedLeftOperand.Value;
      object/*?*/ right = this.ConvertedRightOperand.Value;
      if (left == null || right == null) return null;
      switch (System.Convert.GetTypeCode(left)) {
        case TypeCode.Int32:
          //^ assume left is int && right is int;
          return (int)left ^ (int)right;
        case TypeCode.UInt32:
          //^ assume left is uint && right is uint;
          return (uint)left ^ (uint)right;
        case TypeCode.Int64:
          //^ assume left is long && right is long;
          return (long)left ^ (long)right;
        case TypeCode.UInt64:
          //^ assume left is ulong && right is ulong;
          return (ulong)left ^ (ulong)right;
        case TypeCode.Boolean:
          //^ assume left is bool && right is bool;
          return (bool)left ^ (bool)right;
      }
      return null;
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new ExclusiveOr(containingBlock, this);
    }

    /// <summary>
    /// Returns true if no information is lost if the integer value of this expression is converted to the target integer type.
    /// </summary>
    public override bool IntegerConversionIsLossless(ITypeDefinition targetType) {
      if (!TypeHelper.IsPrimitiveInteger(this.LeftOperand.Type) || !TypeHelper.IsPrimitiveInteger(this.RightOperand.Type)) return false;
      return this.LeftOperand.IntegerConversionIsLossless(targetType) && this.RightOperand.IntegerConversionIsLossless(targetType);
    }

    /// <summary>
    /// A list of dummy methods that correspond to operations that are built into IL. The dummy methods are used, via overload resolution,
    /// to determine how the operands are to be converted before the operation is carried out.
    /// </summary>
    protected override IEnumerable<IMethodDefinition> StandardOperators {
      get {
        BuiltinMethods dummyMethods = this.Compilation.BuiltinMethods;
        yield return dummyMethods.Int32opInt32;
        yield return dummyMethods.UInt32opUInt32;
        yield return dummyMethods.Int64opInt64;
        yield return dummyMethods.UInt64opUInt64;
        ITypeDefinition leftOperandType = this.LeftOperand.Type;
        ITypeDefinition rightOperandType = this.RightOperand.Type;
        if (leftOperandType.IsEnum) {
          yield return dummyMethods.GetDummyEnumOpEnum(leftOperandType);
        } else if (rightOperandType.IsEnum)
          yield return dummyMethods.GetDummyEnumOpEnum(rightOperandType);
        yield return dummyMethods.BoolOpBool;
      }
    }

  }

  /// <summary>
  /// An expression that computes the bitwise exclusive or of the left and right operands. 
  /// The result of the expression is assigned to the left operand, which must be a target expression.
  /// When the operator is overloaded, this expression corresponds to a call to op_ExclusiveOr.
  /// </summary>
  public class ExclusiveOrAssignment : BinaryOperationAssignment {

    /// <summary>
    /// An expression that computes the bitwise exclusive or of the left and right operands. 
    /// The result of the expression is assigned to the left operand, which must be a target expression.
    /// When the operator is overloaded, this expression corresponds to a call to op_ExclusiveOr.
    /// </summary>
    /// <param name="leftOperand">The left operand and target of the assignment.</param>
    /// <param name="rightOperand">The right operand.</param>
    /// <param name="sourceLocation">The source location of the operation.</param>
    public ExclusiveOrAssignment(TargetExpression leftOperand, Expression rightOperand, ISourceLocation sourceLocation)
      : base(leftOperand, rightOperand, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected ExclusiveOrAssignment(BlockStatement containingBlock, ExclusiveOrAssignment template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Calls the visitor.Visit(ExclusiveOrAssignment) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (this.ContainingBlock == containingBlock) return this;
      return new ExclusiveOrAssignment(containingBlock, this);
    }

    /// <summary>
    /// Creates an exclusive or expression using the given left Operand and this.RightOperand as the two operands.
    /// The method does not use this.LeftOperand.Expression, since it may be necessary to factor out any subexpressions so that
    /// they are evaluated only once. The given left operand expression is expected to be the expression that remains after factoring.
    /// </summary>
    /// <param name="leftOperand">An expression to combine with this.RightOperand into a binary expression.</param>
    protected override Expression CreateBinaryExpression(Expression leftOperand) {
      Expression result = new ExclusiveOr(leftOperand, this.RightOperand, this.SourceLocation);
      result.SetContainingExpression(this);
      return result;
    }
  }

  /// <summary>
  /// An expression that results in true if an embedded parameterized expression results in true for at least one binding of values to the parameters.
  /// </summary>
  public class Exists : Quantifier {

    /// <summary>
    /// Allocates an expression that results in true if an embedded parameterized expression results in true for at least one binding of values to the parameters.
    /// </summary>
    /// <param name="boundVariables">
    /// One of more local declarations statements. Typically these locals are referenced in this.Condition.
    /// The Exists expression is true if this.Condition is true for at least one binding of values to these locals.
    /// </param>
    /// <param name="condition">
    /// An expression that is evaluated for every tuple of values that may be bound to the variables defined by this.BoundVariables.
    /// If the expression evaluates to true for at least one such tuple, the result of the Exists expression is true.
    /// Typically, the expression contains references to the variables defined in this.BoundVariables.
    /// </param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public Exists(List<LocalDeclarationsStatement> boundVariables, Expression condition, ISourceLocation sourceLocation)
      : base(boundVariables, condition, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected Exists(BlockStatement containingBlock, Exists template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Calls the visitor.Visit(Exists) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      base.Dispatch(visitor);
    }

    /// <summary>
    /// A string that names the quantifier.
    /// </summary>
    protected override string GetQuantifierName() {
      return "Exists";
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      Exists result = new Exists(containingBlock, this);
      result.CopyTriggersFromTemplate(this);
      return result;
    }

  }

  /// <summary>
  /// The value of the left operand raised to the power of the value of the right operand. In VB the ^ operand corresponds to this expression. 
  /// </summary>
  public class Exponentiation : BinaryOperation {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="leftOperand"></param>
    /// <param name="rightOperand"></param>
    /// <param name="sourceLocation"></param>
    public Exponentiation(Expression leftOperand, Expression rightOperand, ISourceLocation sourceLocation)
      : base(leftOperand, rightOperand, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected Exponentiation(BlockStatement containingBlock, Exponentiation template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Calls the visitor.Visit(Exponentiation) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Returns the string used to identify the operator in error messages
    /// </summary>
    protected override string OperationSymbolForErrorMessage {
      get { return "^"; }
    }


    /// <summary>
    /// Returns the name a method must have to overload this operator.
    /// </summary>
    protected override IName GetOperatorName() {
      return this.NameTable.OpExponentiation;
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new Exponentiation(containingBlock, this);
    }

    /// <summary>
    /// A list of dummy methods that correspond to operations that are built into IL. The dummy methods are used, via overload resolution,
    /// to determine how the operands are to be converted before the operation is carried out.
    /// </summary>
    protected override IEnumerable<IMethodDefinition> StandardOperators {
      get {
        return Enumerable<IMethodDefinition>.Empty; //TODO: implement this
      }
    }

  }

  /// <summary>
  /// An expression results in a value of some type.
  /// </summary>
  public abstract class Expression : CheckableSourceItem {

    /// <summary>
    /// Use this constructor when allocating a new expression. Do not give out the resulting instance to client code before
    /// calling SetContainingExpression to finish initialization of the expression.
    /// </summary>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    protected Expression(ISourceLocation sourceLocation)
      : base(sourceLocation) {
    }

    /// <summary>
    /// Use this constructor when allocating a new expression. Do not give out the resulting instance to client code before
    /// calling SetContainingExpression to finish initialization of the expression.
    /// </summary>
    /// <param name="value">The compile time value of the expression. Can be null.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    protected Expression(object/*?*/ value, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.value = value;
    }

    /// <summary>
    /// Use this constructor to make an expression that is already initialized with its containing block.
    /// </summary>
    protected Expression(BlockStatement containingBlock, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.containingBlock = containingBlock;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">The containing block of the copied expression. This should be different from the containing block of the template expression.</param>
    /// <param name="template">The expression to copy.</param>
    protected Expression(BlockStatement containingBlock, Expression template)
      : base(template.SourceLocation)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.containingBlock = containingBlock;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">The containing block of the copied expression. This should be different from the containing block of the template expression.</param>
    /// <param name="value">The compile time value of the expression. Can be null.</param>
    /// <param name="template">The expression to copy.</param>
    protected Expression(BlockStatement containingBlock, object/*?*/ value, Expression template)
      : base(template.SourceLocation)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.containingBlock = containingBlock;
      this.value = value;
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the item or a constituent part of the item.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return this.Type is Dummy;
    }

    /// <summary>
    /// The block in which the expression is nested.
    /// </summary>
    public BlockStatement ContainingBlock {
      get
        //^ ensures result == this.containingBlock;
      {
        //^ assume this.containingBlock != null;
        return this.containingBlock;
      }
    }
    //^ [SpecPublic]
    BlockStatement/*?*/ containingBlock;

    /// <summary>
    /// The compilation that contains this expression.
    /// </summary>
    protected Compilation Compilation {
      get {
        return this.ContainingBlock.Compilation;
      }
    }

    /// <summary>
    /// Returns a new read only list that contains copies of the elements of the given collection of expressions, where each element
    /// expression has been copied by call Expression.MakeCopyFor, using the given containingBlock.
    /// </summary>
    /// <typeparam name="T">The type of expression.</typeparam>
    /// <param name="collection">A collection of expressions to be copied.</param>
    /// <param name="containingBlock">The containing block that the copied expressions must get.</param>
    public static IEnumerable<T/*!*/> CopyExpressions<T>(IEnumerable<T/*!*/> collection, BlockStatement containingBlock)
      where T : Expression {
      if (collection == Expression.emptyCollection) return collection;
      List<T/*!*/> result = new List<T/*!*/>();
      foreach (T/*!*/ t in collection) {
        //^ assume t != null;
        T copy = (T)t.MakeCopyFor(containingBlock);
        result.Add(copy);
      }
      if (result.Count == 0) return collection;
      result.TrimExcess();
      return result.AsReadOnly();
    }

    /// <summary>
    /// True if this expression is a constant positive integer that could also be interpreted as a negative integer.
    /// For example, 0x80000000, could also be interpreted as a convenient way of writing int.MinValue.
    /// </summary>
    public virtual bool CouldBeInterpretedAsNegativeSignedInteger {
      get { return false; }
    }

    /// <summary>
    /// True if this expression is a constant negative integer that could also be interpreted as a unsigned integer.
    /// For example, 1 &lt;&lt; 31 could also be interpreted as a convenient way of writing 0x80000000.
    /// </summary>
    public virtual bool CouldBeInterpretedAsUnsignedInteger {
      get { return false; }
    }

    /// <summary>
    /// Creates a temporary variable that is initialized with the given value and adds a corresponding declaration statement to the given statement list.
    /// Returns the variable definition.
    /// </summary>
    /// <param name="initialValue">The value to store the temporary variable.</param>
    /// <param name="statements">The statement list to which the declaration for the temporary will be added.</param>
    public static LocalDefinition CreateInitializedLocalDeclarationAndAddDeclarationsStatementToList(Expression initialValue, List<Statement> statements) {
      IName dummyName = initialValue.Helper.NameTable.GetNameFor("__temp" + initialValue.SourceLocation.StartIndex);
      NameDeclaration tempName = new NameDeclaration(dummyName, SourceDummy.SourceLocation);
      LocalDeclaration temp = new LocalDeclaration(false, false, tempName, initialValue, initialValue.SourceLocation);
      List<LocalDeclaration> declarations = new List<LocalDeclaration>(1);
      declarations.Add(temp);
      LocalDeclarationsStatement statement = new LocalDeclarationsStatement(false, false, false, TypeExpression.For(initialValue.Type), declarations, initialValue.SourceLocation);
      statements.Add(statement);
      statement.SetContainingBlock(initialValue.ContainingBlock);
      temp.SetContainingLocalDeclarationsStatement(statement);
      return temp.LocalVariable;
    }

    /// <summary>
    /// Calls this.ProjectAsIExpression().Dispatch(visitor).
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      IExpression expr = this.ProjectAsIExpression();
      if (expr != this) expr.Dispatch(visitor);
    }

    /// <summary>
    /// An empty collection of expressions.
    /// </summary>
    public static IEnumerable<Expression> EmptyCollection {
      get { return Expression.emptyCollection; }
    }
    static readonly IEnumerable<Expression> emptyCollection = new List<Expression>(0).AsReadOnly();

    /// <summary>
    /// Either returns this expression, or returns a BlockExpression that assigns each subexpression to a temporary local variable
    /// and then evaluates an expression that is the same as this expression, but which refers to the temporaries rather than the 
    /// factored out subexpressions. This transformation is useful when expressing the semantics of operation assignments and increment/decrement operations.
    /// </summary>
    public virtual Expression FactoredExpression()
      //^ ensures result == this || result is BlockExpression;
    {
      return this;
    }

    /// <summary>
    /// Returns an instance of the CompileTimeConstant that represents the same source location as this value. 
    /// this.Value must be non null.
    /// </summary>
    /// <returns></returns>
    public virtual CompileTimeConstant GetAsConstant()
      //^ requires this.Value != null;
    {
      CompileTimeConstant result = new CompileTimeConstant(this);
      result.UnfoldedExpression = this;
      result.SetContainingExpression(this);
      return result;
    }

    /// <summary>
    /// Computes the compile time value of the expression. Can be null.
    /// </summary>
    protected virtual object/*?*/ GetValue() {
      return null;
    }



    /// <summary>
    /// Checks if the expression has a side effect and reports an error unless told otherwise.
    /// </summary>
    /// <param name="reportError">If true, report an error if the expression has a side effect.</param>
    public virtual bool HasSideEffect(bool reportError) {
      return false;
    }

    /// <summary>
    /// Gets a value indicating whether this instance is pure.
    /// </summary>
    /// <value><c>true</c> if this instance is pure; otherwise, <c>false</c>.</value>
    public bool IsPure {
      get { return !this.HasSideEffect(false); }
    }

    /// <summary>
    /// A language specific helper class containing methods that are of general utility for semantic analysis.
    /// </summary>
    protected LanguageSpecificCompilationHelper Helper {
      get {
        return this.ContainingBlock.Helper;
      }
    }

    /// <summary>
    /// Infers the type of value that this expression will evaluate to. At runtime the actual value may be an instance of subclass of the result of this method.
    /// Calling this method does not cache the computed value and does not generate any error messages. In some cases, such as references to the parameters of lambda
    /// expressions during type overload resolution, the value returned by this method may be different from one call to the next.
    /// When type inference fails, Dummy.Type is returned.
    /// </summary>
    public virtual ITypeDefinition InferType() {
      return this.Type;
    }

    /// <summary>
    /// Returns true if the expression represents a compile time constant without an explicitly specified type. For example, 1 rather than 1L.
    /// Constant expressions such as 2*16 are polymorhpic if both operands are polymorhic.
    /// </summary>
    public virtual bool ValueIsPolymorphicCompileTimeConstant {
      get {
        return false;
      }
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    public abstract Expression MakeCopyFor(BlockStatement containingBlock);
    //^ ensures result.GetType() == this.GetType();
    //^ ensures result.ContainingBlock == containingBlock;

    /// <summary>
    /// A table used to intern strings used as names. This table is obtained from the host environment.
    /// It is mutuable, in as much as it is possible to add new names to the table.
    /// </summary>
    public INameTable NameTable {
      get { return this.Compilation.NameTable; }
    }

    /// <summary>
    /// Returns true if no information is lost if the integer value of this expression is converted to the target integer type.
    /// </summary>
    public virtual bool IntegerConversionIsLossless(ITypeDefinition targetType) {
      if (TypeHelper.IsSignedPrimitiveInteger(this.Type)) {
        if (!TypeHelper.IsSignedPrimitiveInteger(targetType)) return false;
      } else if (TypeHelper.IsUnsignedPrimitiveInteger(this.Type)) {
        if (!TypeHelper.IsUnsignedPrimitiveInteger(targetType)) return false;
      }
      return TypeHelper.SizeOfType(this.Type) <= TypeHelper.SizeOfType(targetType);
    }

    /// <summary>
    /// A collection of well known types that must be part of every target platform and that are fundamental to modeling compiled code.
    /// The types are obtained by querying the unit set of the compilation and thus can include types that are defined by the compilation itself.
    /// </summary>
    public IPlatformType PlatformType {
      get { return this.Compilation.PlatformType; }
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    public IExpression ProjectAsIExpression() {
      if (!(this is CompileTimeConstant)) {
        object/*?*/ value = this.Value;
        if (value != null) {
          CompileTimeConstant cconst = new CompileTimeConstant(value, this.SourceLocation);
          cconst.UnfoldedExpression = this;
          cconst.SetContainingExpression(this);
          return cconst;
        }
      }
      return this.ProjectAsNonConstantIExpression();
    }

    /// <summary>
    /// Returns an object that implements IMetadataExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and suitable for direct use in metadata.
    /// </summary>
    public virtual IMetadataExpression ProjectAsIMetadataExpression() {
      IExpression iexpr = this.ProjectAsIExpression();
      IMetadataExpression/*?*/ mdexpr = iexpr as IMetadataExpression;
      if (mdexpr != null) return mdexpr;
      return Dummy.Expression;
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected abstract IExpression ProjectAsNonConstantIExpression();

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Block.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    protected void SetContainingBlock(BlockStatement containingBlock) {
      this.containingBlock = containingBlock;
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public virtual void SetContainingExpression(Expression containingExpression) {
      this.containingBlock = containingExpression.ContainingBlock;
    }

    /// <summary>
    /// The type of value that the expression will evaluate to, as determined at compile time.
    /// </summary>
    public abstract ITypeDefinition Type { get; }

    /// <summary>
    /// The compile time value of the expression. Can be null.
    /// </summary>
    public object/*?*/ Value {
      get {
        if (this.value == null) {
          this.value = this.GetValue();
          if (this.value == null) this.value = Dummy.Constant;
        }
        if (this.value is Dummy) return null;
        return this.value;
      }
    }
    object/*?*/ value;

  }

  /// <summary>
  /// An expression that results in true if an embedded parameterized expression results in true for every possible binding of values to the parameters.
  /// </summary>
  public class Forall : Quantifier {

    /// <summary>
    /// Allocates an expression that results in true if an embedded parameterized expression results in true for every possible binding of values to the parameters.
    /// </summary>
    /// <param name="boundVariables">
    /// One of more local declarations statements. Typically these locals are referenced in this.Condition.
    /// The Forall expression is true if this.Condition is true for every possible binding of values to these variables.
    /// </param>
    /// <param name="condition">
    /// An expression that is evaluated for every tuple of values that may be bound to the variables defined by this.BoundVariables.
    /// If the expression evaluates to true for every such tuple, the result of the Forall expression is true.
    /// Typically, the expression contains references to the variables defined in this.BoundVariables.
    /// </param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public Forall(List<LocalDeclarationsStatement> boundVariables, Expression condition, ISourceLocation sourceLocation)
      : base(boundVariables, condition, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected Forall(BlockStatement containingBlock, Forall template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Calls the visitor.Visit(Forall) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      base.Dispatch(visitor);
    }

    /// <summary>
    /// A string that names the quantifier.
    /// </summary>
    protected override string GetQuantifierName() {
      return "ForAll";
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      Forall result = new Forall(containingBlock, this);
      result.CopyTriggersFromTemplate(this);
      return result;
    }

  }

  /// <summary>
  /// An expression that refers to a generic type or method instance by specifying the generic type or method group name and the argument types.
  /// </summary>
  public class GenericInstanceExpression : Expression {

    /// <summary>
    /// Allocates an expression that refers to a generic type or method instance by specifying the generic type or method group name and the argument types.
    /// </summary>
    /// <param name="genericTypeOrMethod">An expression that should resolve to a method group or a type group.</param>
    /// <param name="argumentTypes">The type values to match with the generic parameters of the method or type to instantiate.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public GenericInstanceExpression(Expression genericTypeOrMethod, IEnumerable<TypeExpression> argumentTypes, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.genericTypeOrMethod = genericTypeOrMethod;
      this.argumentTypes = argumentTypes;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected GenericInstanceExpression(BlockStatement containingBlock, GenericInstanceExpression template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.genericTypeOrMethod = template.genericTypeOrMethod.MakeCopyFor(containingBlock);
      this.argumentTypes = Expression.CopyExpressions(template.argumentTypes, containingBlock);
    }

    /// <summary>
    /// The type values to match with the generic parameters of the method or type to instantiate.
    /// </summary>
    public IEnumerable<TypeExpression> ArgumentTypes {
      get { return this.argumentTypes; }
    }
    readonly IEnumerable<TypeExpression> argumentTypes;

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      bool result = this.GenericTypeOrMethod.HasErrors;
      foreach (TypeExpression typeArg in this.ArgumentTypes) result |= typeArg.HasErrors;
      return result;
    }

    /// <summary>
    /// Calls the visitor.Visit(GenericInstanceExpression) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// An expression that should resolve to a method group or a type group.
    /// </summary>
    public Expression GenericTypeOrMethod {
      get { return this.genericTypeOrMethod; }
    }
    readonly Expression genericTypeOrMethod;

    /// <summary>
    /// Resolves the list of type expressions in this.ArgumentTypes and returns the resolved types as a list of type referneces.
    /// </summary>
    public IEnumerable<ITypeReference> GetArgumentTypeReferences() {
      List<ITypeReference> genericArgumentList = new List<ITypeReference>();
      foreach (TypeExpression typeArg in this.ArgumentTypes) genericArgumentList.Add(typeArg.ResolvedType);
      genericArgumentList.TrimExcess();
      IEnumerable<ITypeReference> genericArguments = genericArgumentList.AsReadOnly();
      return genericArguments;
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new GenericInstanceExpression(containingBlock, this);
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      return new DummyExpression(this.SourceLocation);
    }

    /// <summary>
    /// If this expression resolves to a generic type instance, return the instance. Otherwise return null;
    /// </summary>
    public virtual IGenericTypeInstanceReference/*?*/ ResolveAsGenericTypeInstance() {
      object/*?*/ resolvedGenericTypeOrMethod = null;
      SimpleName/*?*/ simpleName = this.GenericTypeOrMethod as SimpleName;
      if (simpleName != null)
        resolvedGenericTypeOrMethod = simpleName.ResolveAsNamespaceOrType();
      else {
        QualifiedName/*?*/ qualifiedName = this.GenericTypeOrMethod as QualifiedName;
        if (qualifiedName != null)
          resolvedGenericTypeOrMethod = qualifiedName.ResolveAsNamespaceOrTypeGroup();
        //TODO: namespace qualified name
      }
      var resolvedGenericType = resolvedGenericTypeOrMethod as INamedTypeDefinition;
      if (resolvedGenericType == null) {
        ITypeGroup/*?*/ typeGroup = resolvedGenericTypeOrMethod as ITypeGroup;
        if (typeGroup != null) {
          uint numTypeArgs = IteratorHelper.EnumerableCount(this.ArgumentTypes);
          foreach (INamedTypeDefinition type in typeGroup.Types) {
            if (type.GenericParameterCount != numTypeArgs) continue;
            resolvedGenericType = type;
            break;
          }
        }
      }
      if (resolvedGenericType != null && resolvedGenericType.IsGeneric)
        return GenericTypeInstance.GetGenericTypeInstance(resolvedGenericType, this.GetArgumentTypeReferences(), this.Compilation.HostEnvironment.InternFactory);
      return null;
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingExpression(Expression containingExpression) {
      base.SetContainingExpression(containingExpression);
      this.genericTypeOrMethod.SetContainingExpression(this);
      foreach (TypeExpression argumentType in this.argumentTypes) argumentType.SetContainingExpression(this);
    }

    /// <summary>
    /// The type of value that the expression will evaluate to, as determined at compile time.
    /// </summary>
    public override ITypeDefinition Type {
      get { return this.GenericTypeOrMethod.Type; }
    }

  }

  /// <summary>
  /// An expression that refers to a generic type instance by specifying the generic type group name and the argument types.
  /// </summary>
  public class GenericTypeInstanceExpression : TypeExpression {

    /// <summary>
    /// Allocates an expression that refers to a generic type instance by specifying the generic type group name and the argument types.
    /// </summary>
    /// <param name="genericType">An expression that should resolve to a type group.</param>
    /// <param name="argumentTypes">The type values to match with the generic parameters of the type to instantiate.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public GenericTypeInstanceExpression(TypeExpression genericType, IEnumerable<TypeExpression> argumentTypes, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.genericType = genericType;
      this.argumentTypes = argumentTypes;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected GenericTypeInstanceExpression(BlockStatement containingBlock, GenericTypeInstanceExpression template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.genericType = (TypeExpression)template.genericType.MakeCopyFor(containingBlock);
      this.argumentTypes = Expression.CopyExpressions(template.argumentTypes, containingBlock);
    }

    /// <summary>
    /// The type values to match with the generic parameters of the type to instantiate.
    /// </summary>
    public IEnumerable<TypeExpression> ArgumentTypes {
      get { return this.argumentTypes; }
    }
    readonly IEnumerable<TypeExpression> argumentTypes;

    /// <summary>
    /// Calls the visitor.Visit(GenericTypeInstanceExpression) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// An expression that should resolve to a type group.
    /// </summary>
    public TypeExpression GenericType {
      get { return this.genericType; }
    }
    readonly TypeExpression genericType;

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new GenericTypeInstanceExpression(containingBlock, this);
    }

    /// <summary>
    /// The type denoted by the expression. If expression cannot be resolved, a dummy type is returned. If the expression is ambiguous the first matching type is returned.
    /// If the expression does not resolve to exactly one type, an error is added to the error collection of the compilation context.
    /// </summary>
    protected override ITypeDefinition Resolve() {
      List<ITypeReference> argumentTypes = new List<ITypeReference>();
      foreach (TypeExpression typeArgument in this.argumentTypes)
        argumentTypes.Add(typeArgument.ResolvedType);
      INamedTypeDefinition genericType = this.GenericType.Resolve(argumentTypes.Count) as INamedTypeDefinition;
      if (genericType == null || !genericType.IsGeneric) {
        //TODO: error message
        return Dummy.Type;
      }
      return GenericTypeInstance.GetGenericTypeInstance(genericType, argumentTypes, this.Compilation.HostEnvironment.InternFactory);
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingExpression(Expression containingExpression) {
      base.SetContainingExpression(containingExpression);
      this.genericType.SetContainingExpression(this);
      foreach (TypeExpression argumentType in this.argumentTypes) argumentType.SetContainingExpression(this);
    }

  }

  /// <summary>
  /// An expression that results in an instance of System.Type that represents the compile time type that has been paired with a runtime value via a typed reference.
  /// This corresponds to the __reftype operator in C#.
  /// </summary>
  public class GetTypeOfTypedReference : Expression, IGetTypeOfTypedReference {

    /// <summary>
    /// Allocates an expression that results in an instance of System.Type that represents the compile time type that has been paired with a runtime value via a typed reference.
    /// This corresponds to the __reftype operator in C#.
    /// </summary>
    /// <param name="typedReference">An expression that should result in a value of type System.TypedReference.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public GetTypeOfTypedReference(Expression typedReference, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.typedReference = typedReference;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected GetTypeOfTypedReference(BlockStatement containingBlock, GetTypeOfTypedReference template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.typedReference = template.typedReference.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// Calls the visitor.Visit(IGetTypeOfTypedReference) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(GetTypeOfTypedReference) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (this.ContainingBlock == containingBlock) return this;
      return new GetTypeOfTypedReference(containingBlock, this);
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      return this;
    }

    /// <summary>
    /// The type of value that the expression will evaluate to, as determined at compile time.
    /// </summary>
    public override ITypeDefinition Type {
      get { return this.PlatformType.SystemType.ResolvedType; }
    }

    /// <summary>
    /// An expression that should result in a value of type System.TypedReference.
    /// </summary>
    public Expression TypedReference {
      get { return this.typedReference; }
    }
    readonly Expression typedReference;

    #region IGetTypeOfTypedReference Members

    IExpression IGetTypeOfTypedReference.TypedReference {
      get { return this.TypedReference.ProjectAsIExpression(); }
    }

    #endregion

    #region IExpression Members

    ITypeReference IExpression.Type {
      get { return this.Type; }
    }

    #endregion
  }

  /// <summary>
  /// An expression that converts the typed reference value resulting from evaluating TypedReference to a value of the type specified by TargetType.
  /// This corresponds to the __refvalue operator in C#.
  /// </summary>
  public class GetValueOfTypedReference : Expression, IGetValueOfTypedReference {

    /// <summary>
    /// Allocates an expression that converts the typed reference value resulting from evaluating TypedReference to a value of the type specified by TargetType.
    /// This corresponds to the __refvalue operator in C#.
    /// </summary>
    /// <param name="typedReference">An expression that should result in a value of type System.TypedReference.</param>
    /// <param name="targetType">The type to which the value part of the typed reference must be converted.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public GetValueOfTypedReference(Expression typedReference, TypeExpression targetType, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.typedReference = typedReference;
      this.targetType = targetType;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected GetValueOfTypedReference(BlockStatement containingBlock, GetValueOfTypedReference template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.targetType = (TypeExpression)template.targetType.MakeCopyFor(containingBlock);
      this.typedReference = template.typedReference.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// Calls the visitor.Visit(IGetValueOfTypedReference) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(GetValueOfTypedReference) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (this.ContainingBlock == containingBlock) return this;
      return new GetValueOfTypedReference(containingBlock, this);
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      return this;
    }

    /// <summary>
    /// The type to which the value part of the typed reference must be converted.
    /// </summary>
    public TypeExpression TargetType {
      get { return this.targetType; }
    }
    readonly TypeExpression targetType;

    /// <summary>
    /// The type of value that the expression will evaluate to, as determined at compile time.
    /// </summary>
    public override ITypeDefinition Type {
      get { return this.TargetType.ResolvedType; }
    }

    /// <summary>
    /// An expression that results in a value of type System.TypedReference.
    /// </summary>
    public Expression TypedReference {
      get { return this.typedReference; }
    }
    readonly Expression typedReference;

    #region IGetValueOfTypedReference Members

    IExpression IGetValueOfTypedReference.TypedReference {
      get { return this.TypedReference.ProjectAsIExpression(); }
    }

    ITypeReference IGetValueOfTypedReference.TargetType {
      get { return this.TargetType.ResolvedType; }
    }

    #endregion

    #region IExpression Members

    ITypeReference IExpression.Type {
      get { return this.Type; }
    }

    #endregion
  }

  /// <summary>
  /// An expression that results in true if the value of the left operand is greater than the value of the right operand.
  /// When overloaded, this expression corresponds to a call to op_GreaterThan.
  /// </summary>
  public class GreaterThan : Comparison, IGreaterThan {

    /// <summary>
    /// Allocates an expression that results in true if the value of the left operand is greater than the value of the right operand.
    /// When overloaded, this expression corresponds to a call to op_GreaterThan.
    /// </summary>
    /// <param name="leftOperand">The left operand.</param>
    /// <param name="rightOperand">The right operand.</param>
    /// <param name="sourceLocation">The source location of the operation.</param>
    public GreaterThan(Expression leftOperand, Expression rightOperand, ISourceLocation sourceLocation)
      : base(leftOperand, rightOperand, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected GreaterThan(BlockStatement containingBlock, GreaterThan template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Calls the visitor.Visit(IGreaterThan) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(GreaterThan) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Returns the string used to identify the operator in error messages
    /// </summary>
    protected override string OperationSymbolForErrorMessage {
      get { return ">"; }
    }


    /// <summary>
    /// Returns the name a method must have to overload this operator.
    /// </summary>
    protected override IName GetOperatorName() {
      return this.NameTable.OpGreaterThan;
    }

    /// <summary>
    /// Computes the compile time value of the expression. Can be null.
    /// </summary>
    protected override object/*?*/ GetValue() {
      object/*?*/ left = this.ConvertedLeftOperand.Value;
      object/*?*/ right = this.ConvertedRightOperand.Value;
      if (left == null || right == null) return null;
      switch (System.Convert.GetTypeCode(left)) {
        case TypeCode.Int32:
          //^ assume left is int && right is int;
          return (int)left > (int)right;
        case TypeCode.UInt32:
          //^ assume left is uint && right is uint;
          return (uint)left > (uint)right;
        case TypeCode.Int64:
          //^ assume left is long && right is long;
          return (long)left > (long)right;
        case TypeCode.UInt64:
          //^ assume left is ulong && right is ulong;
          return (ulong)left > (ulong)right;
        case TypeCode.Single:
          //^ assume left is float && right is float;
          return (float)left > (float)right;
        case TypeCode.Double:
          //^ assume left is double && right is double;
          return (double)left > (double)right;
        case TypeCode.Decimal:
          //^ assume left is decimal && right is decimal;
          return (decimal)left > (decimal)right;
      }
      return null;
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new GreaterThan(containingBlock, this);
    }

  }

  /// <summary>
  /// An expression that results in true if the value of the left operand is greater than or equal to the value of the right operand.
  /// When overloaded, this expression corresponds to a call to op_GreaterThanOrEqual.
  /// </summary>
  public class GreaterThanOrEqual : Comparison, IGreaterThanOrEqual {

    /// <summary>
    /// Allocates an expression that results in true if the value of the left operand is greater than or equal to the value of the right operand.
    /// When overloaded, this expression corresponds to a call to op_GreaterThanOrEqual.
    /// </summary>
    /// <param name="leftOperand">The left operand.</param>
    /// <param name="rightOperand">The right operand.</param>
    /// <param name="sourceLocation">The source location of the operation.</param>
    public GreaterThanOrEqual(Expression leftOperand, Expression rightOperand, ISourceLocation sourceLocation)
      : base(leftOperand, rightOperand, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected GreaterThanOrEqual(BlockStatement containingBlock, GreaterThanOrEqual template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Calls the visitor.Visit(IGreaterThanOrEqual) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(GreaterThanOrEqual) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Returns the string used to identify the operator in error messages
    /// </summary>
    protected override string OperationSymbolForErrorMessage {
      get { return ">="; }
    }


    /// <summary>
    /// Returns the name a method must have to overload this operator.
    /// </summary>
    protected override IName GetOperatorName() {
      return this.NameTable.OpGreaterThanOrEqual;
    }

    /// <summary>
    /// Computes the compile time value of the expression. Can be null.
    /// </summary>
    protected override object/*?*/ GetValue() {
      object/*?*/ left = this.ConvertedLeftOperand.Value;
      object/*?*/ right = this.ConvertedRightOperand.Value;
      if (left == null || right == null) return null;
      switch (System.Convert.GetTypeCode(left)) {
        case TypeCode.Int32:
          //^ assume left is int && right is int;
          return (int)left >= (int)right;
        case TypeCode.UInt32:
          //^ assume left is uint && right is uint;
          return (uint)left >= (uint)right;
        case TypeCode.Int64:
          //^ assume left is long && right is long;
          return (long)left >= (long)right;
        case TypeCode.UInt64:
          //^ assume left is ulong && right is ulong;
          return (ulong)left >= (ulong)right;
        case TypeCode.Single:
          //^ assume left is float && right is float;
          return (float)left >= (float)right;
        case TypeCode.Double:
          //^ assume left is double && right is double;
          return (double)left >= (double)right;
        case TypeCode.Decimal:
          //^ assume left is decimal && right is decimal;
          return (decimal)left >= (decimal)right;
      }
      return null;
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new GreaterThanOrEqual(containingBlock, this);
    }

  }

  /// <summary>
  /// An expression without an explicit source representation that results in a value specified by a containing expression or statement.
  /// In VB the . and ! operators can be implicitly qualified by the value of an outer With statement.
  /// </summary>
  public class ImplicitQualifier : Expression {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sourceLocation"></param>
    public ImplicitQualifier(ISourceLocation sourceLocation)
      : base(sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected ImplicitQualifier(BlockStatement containingBlock, ImplicitQualifier template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Calls the visitor.Visit(ImplicitQualifier) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new ImplicitQualifier(containingBlock, this);
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      return new DummyExpression(this.SourceLocation);
    }

    /// <summary>
    /// The type of value that the expression will evaluate to, as determined at compile time.
    /// </summary>
    public override ITypeDefinition Type {
      get { return Dummy.Type; }
    }

  }

  /// <summary>
  /// An expression that results in true if the right operand is true whenever the left operand is true.
  /// If the left operand results in false, the right operand is not evaluated.
  /// </summary>
  public class Implies : LogicalBinaryOperation {

    /// <summary>
    /// Allocates an expression that results in true if the right operand is true whenever the left operand is true.
    /// If the left operand results in false, the right operand is not evaluated.
    /// </summary>
    /// <param name="leftOperand">The left operand.</param>
    /// <param name="rightOperand">The right operand.</param>
    /// <param name="sourceLocation">The source location of the operation.</param>
    public Implies(Expression leftOperand, Expression rightOperand, ISourceLocation sourceLocation)
      : base(leftOperand, rightOperand, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected Implies(BlockStatement containingBlock, Implies template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Calls the visitor.Visit(Implies) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Returns the string used to identify the operator in error messages
    /// </summary>
    protected override string OperationSymbolForErrorMessage {
      get { return "==>"; }
    }


    /// <summary>
    /// Returns the name a method must have to overload this operator.
    /// </summary>
    protected override IName GetOperatorName() {
      return this.NameTable.EmptyName;
    }

    /// <summary>
    /// Computes the compile time value of the expression. Can be null.
    /// </summary>
    protected override object/*?*/ GetValue() {
      object/*?*/ left = this.ConvertedLeftOperand.Value;
      if (left is bool) {
        if (!(bool)left) return true;
        object/*?*/ right = this.ConvertedRightOperand.Value;
        if (right is bool) return (bool)right;
      }
      return null;
    }

    /// <summary>
    /// Returns System.Boolean.
    /// </summary>
    public override ITypeDefinition InferType() {
      return this.Compilation.PlatformType.SystemBoolean.ResolvedType;
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new Implies(containingBlock, this);
    }

    /// <summary>
    /// Returns the projection of (bool)this.LeftOperand ? (bool)this.RightOperand : true;
    /// </summary>
    /// <returns></returns>
    protected override IExpression ProjectAsStandardLogicalOperator() {
      Expression x = this.Helper.ImplicitConversion(this.LeftOperand, this.PlatformType.SystemBoolean.ResolvedType);
      Expression y = this.Helper.ImplicitConversion(this.RightOperand, this.PlatformType.SystemBoolean.ResolvedType);
      Expression result = new Conditional(x, y, new CompileTimeConstant(true, SourceDummy.SourceLocation), this.SourceLocation);
      result.SetContainingExpression(this);
      return result.ProjectAsIExpression();
    }

    /// <summary>
    /// Returns a dummy expression since this operator is not overloadable.
    /// </summary>
    protected override IExpression ProjectAsUserDefinedOverloadAsLogicalOperator(IMethodCall overloadMethodCall) {
      return new DummyExpression(this.SourceLocation);
    }

  }

  /// <summary>
  /// An expression that represents a call to the getter or setter of a default indexed property, or an access to an array element or string character.
  /// </summary>
  public class Indexer : ConstructorIndexerOrMethodCall, IArrayIndexer {

    /// <summary>
    /// Allocates an expression that represents a call to the getter or setter of a default indexed property, or an access to an array element or string character.
    /// </summary>
    /// <param name="indexedObject">An expression that results in value whose type is expected to be an array, or string, or to define a default indexed property that matches the indices.</param>
    /// <param name="indices">The indices to pass to the accessor.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public Indexer(Expression indexedObject, IEnumerable<Expression> indices, ISourceLocation sourceLocation)
      : base(indices, sourceLocation) {
      this.indexedObject = indexedObject;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected Indexer(BlockStatement containingBlock, Indexer template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.indexedObject = template.indexedObject.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="indexedObject"></param>
    /// <param name="indices"></param>
    /// <param name="sourceLocation"></param>
    /// <returns></returns>
    protected virtual Indexer CreateNewIndexerForFactoring(Expression indexedObject, IEnumerable<Expression> indices, ISourceLocation sourceLocation) {
      return new Indexer(indexedObject, indices, sourceLocation);
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      bool result = base.CheckForErrorsAndReturnTrueIfAnyAreFound();
      result |= this.IndexedObject.HasErrors;
      return result;
    }

    /// <summary>
    /// Returns a list of the arguments to pass to the constructor, indexer or method, after they have been converted to match the parameters of the resolved method.
    /// </summary>
    protected override List<Expression> ConvertArguments() {
      List<Expression> result = base.ConvertArguments();
      IArrayTypeReference/*?*/ arrayType = this.IndexedObject.Type as IArrayTypeReference;
      if (arrayType != null) {
        for (int i = 0, n = result.Count; i < n; i++) {
          Expression index = result[i];
          if (!TypeHelper.TypesAreEquivalent(index.Type, this.PlatformType.SystemInt32))
            result[i] = this.Helper.ExplicitConversion(index, this.PlatformType.SystemIntPtr.ResolvedType);
        }
      }
      return result;
    }

    /// <summary>
    /// Calls visitor.Visit(IArrayIndexer) if this.IndexedObject is an array, otherwise calls this.ProjectAsIExpression().Dispatch(visitor).
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      IExpression expr = this.ProjectAsIExpression();
      if (expr == this)
        visitor.Visit((IArrayIndexer)this);
      else
        expr.Dispatch(visitor);
    }

    /// <summary>
    /// Calls the visitor.Visit(Indexer) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Either returns this expression, or returns a BlockExpression that assigns each subexpression to a temporary local variable
    /// and then evaluates an expression that is the same as this expression, but which refers to the temporaries rather than the 
    /// factored out subexpressions. This transformation is useful when expressing the semantics of operation assignments and increment/decrement operations.
    /// </summary>
    public override Expression FactoredExpression()
      //^^ ensures result == this || result is BlockExpression;
    {
      bool needToFactor = false;
      Expression indexedObject = this.IndexedObject;
      IExpression indexedObjectAfterProjection = indexedObject.ProjectAsIExpression();
      if (!(indexedObjectAfterProjection is BoundExpression || indexedObjectAfterProjection is CompileTimeConstant)) {
        needToFactor = true;
      } else {
        foreach (Expression index in this.ConvertedArguments) {
          IExpression i = index.ProjectAsIExpression();
          if (i is BoundExpression || i is CompileTimeConstant) continue;
          needToFactor = true;
          break;
        }
      }
      if (!needToFactor) return this;
      List<Statement> statements = new List<Statement>(3);
      BlockStatement block = new BlockStatement(statements, this.SourceLocation);
      indexedObject = new BoundExpression(indexedObject, Expression.CreateInitializedLocalDeclarationAndAddDeclarationsStatementToList(indexedObject, statements), block);
      List<Expression> indices = new List<Expression>(1);
      foreach (Expression index in this.ConvertedArguments) {
        LocalDefinition temp = Expression.CreateInitializedLocalDeclarationAndAddDeclarationsStatementToList(index, statements);
        BoundExpression boundExpr = new BoundExpression(index, temp, block);
        indices.Add(boundExpr);
      }
      Indexer factoredIndexer = this.CreateNewIndexerForFactoring(indexedObject, indices, this.SourceLocation);
      BlockExpression be = new BlockExpression(block, factoredIndexer, this.SourceLocation);
      be.SetContainingExpression(this);
      return be;
    }

    /// <summary>
    /// Returns a collection of methods that match the name of the method/indexer to call, or that represent the
    /// collection of constructors for the named type.
    /// </summary>
    /// <param name="allowMethodParameterInferencesToFail">If this flag is true, 
    /// generic methods should be included in the collection if their method parameter types could not be inferred from the argument types.</param>
    public override IEnumerable<IMethodDefinition> GetCandidateMethods(bool allowMethodParameterInferencesToFail) {
      ITypeDefinition indexedObjectType = this.IndexedObject.Type;
      IArrayTypeReference/*?*/ arrayType = indexedObjectType as IArrayTypeReference;
      if (arrayType != null)
        return this.Compilation.BuiltinMethods.GetDummyArrayGetters(arrayType);
      else {
        IPointerTypeReference/*?*/ pointerType = indexedObjectType as IPointerTypeReference;
        if (pointerType != null)
          return this.GetPointerAdditionMethods(pointerType.TargetType.ResolvedType);
        else
          return this.Helper.GetDefaultIndexedPropertyGetters(this.IndexedObject.Type);
      }
    }

    /// <summary>
    /// Returns a collection of methods that represents the overloads for ptr + index.
    /// </summary>
    protected virtual IEnumerable<IMethodDefinition> GetPointerAdditionMethods(ITypeDefinition indexedObjectElementType) {
      BuiltinMethods dummyMethods = this.Compilation.BuiltinMethods;
      yield return dummyMethods.GetDummyIndexerOp(indexedObjectElementType, this.PlatformType.SystemInt32.ResolvedType);
      yield return dummyMethods.GetDummyIndexerOp(indexedObjectElementType, this.PlatformType.SystemUInt32.ResolvedType);
      yield return dummyMethods.GetDummyIndexerOp(indexedObjectElementType, this.PlatformType.SystemInt64.ResolvedType);
      yield return dummyMethods.GetDummyIndexerOp(indexedObjectElementType, this.PlatformType.SystemUInt64.ResolvedType);
    }

    /// <summary>
    /// An expression that results in value whose type is expected to be an array, or to define a default indexed property that matches the indices.
    /// </summary>
    public Expression IndexedObject {
      get { return this.indexedObject; }
    }
    readonly Expression indexedObject;

    /// <summary>
    /// True if the method to call is determined at run time, based on the runtime type of IndexedObject.
    /// </summary>
    public override bool IsVirtualCall {
      get {
        IMethodDefinition methodToCall = this.ResolvedMethod;
        return (methodToCall.IsVirtual && !(methodToCall.ContainingTypeDefinition.IsStruct) &&
          !(this.IndexedObject is BaseClassReference));
      }
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new Indexer(containingBlock, this);
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      IMethodDefinition resolvedMethod = this.ResolvedMethod;
      if (resolvedMethod is Dummy || resolvedMethod is BuiltinMethodDefinition) {
        IPointerTypeReference/*?*/ pointerType = this.indexedObject.Type as IPointerTypeReference;
        if (pointerType != null)
          return this.ProjectAsDereferencedPointerAddition();
        return this;
      }
      ResolvedMethodCall result = new ResolvedMethodCall(resolvedMethod, this.IndexedObject, new List<Expression>(this.ConvertedArguments), this.SourceLocation);
      result.SetContainingExpression(this);
      return result;
    }

    /// <summary>
    /// Returns an expression corresponding to *(ptr + index) where ptr is this.IndexedObject and index is the first element of this.ConvertedArguments.
    /// </summary>
    protected virtual IExpression ProjectAsDereferencedPointerAddition() {
      IEnumerator<Expression> indexEnumerator = this.ConvertedArguments.GetEnumerator();
      if (!indexEnumerator.MoveNext()) return new DummyExpression(this.SourceLocation);
      Addition addition = new Addition(this.IndexedObject, indexEnumerator.Current, this.SourceLocation);
      AddressDereference aderef = new AddressDereference(addition, this.SourceLocation);
      aderef.SetContainingExpression(this);
      return aderef.ProjectAsIExpression();
    }

    /// <summary>
    /// Results in null or an array indexer or an indexer (property) definition.
    /// </summary>
    public virtual object/*?*/ ResolveAsValueContainer()
      //^ ensures result == null || result is IArrayIndexer || result is IAddressDereference || result is IPropertyDefinition;
    {
      if (this.IndexedObject.Type is IArrayTypeReference) return this;
      IMethodDefinition resolvedMethod = this.ResolvedMethod;
      IPointerTypeReference/*?*/ pointerType = this.indexedObject.Type as IPointerTypeReference;
      if (pointerType != null)
        return this.ProjectAsDereferencedPointerAddition() as IAddressDereference;
      foreach (IPropertyDefinition indexerProperty in this.Helper.GetDefaultIndexedProperties(this.IndexedObject.Type)) {
        if (indexerProperty.Getter != null && indexerProperty.Getter.ResolvedMethod == resolvedMethod) {
          if (indexerProperty.Setter == null) return null;
          return indexerProperty;
        }
      }
      return null;
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingExpression(Expression containingExpression) {
      base.SetContainingExpression(containingExpression);
      this.indexedObject.SetContainingExpression(this);
    }

    /// <summary>
    /// 
    /// </summary>
    protected override void ComplainAboutCallee() {
      // TODO: complain
    }

    #region IArrayIndexer Members

    IEnumerable<IExpression> IArrayIndexer.Indices {
      get { foreach (Expression index in this.ConvertedArguments) yield return index.ProjectAsIExpression(); }
    }

    IExpression IArrayIndexer.IndexedObject {
      get { return this.IndexedObject.ProjectAsIExpression(); }
    }

    #endregion

    #region IExpression Members

    ITypeReference IExpression.Type {
      get { return this.Type; }
    }

    #endregion

  }

  /// <summary>
  /// An expression that intializes a newly constructed object instance by assigning values to fields and properties.
  /// </summary>
  public class InitializeObject : Expression {

    /// <summary>
    /// Allocates an expression that intializes a newly constructed object instance by assigning values to fields and properties.
    /// </summary>
    /// <param name="objectToInitialize">The newly constructed object to initialize.</param>
    /// <param name="namedArguments">A list of (identifier, expression) pairs used to initialize the contructed object via assignments to fields and properties.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public InitializeObject(Expression/*?*/ objectToInitialize, IEnumerable<NamedArgument> namedArguments, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.namedArguments = namedArguments;
      this.objectToInitialize = objectToInitialize;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected InitializeObject(BlockStatement containingBlock, InitializeObject template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      if (template.objectToInitialize != null)
        this.objectToInitialize = template.objectToInitialize.MakeCopyFor(containingBlock);
      this.namedArguments = Expression.CopyExpressions(template.namedArguments, containingBlock);
    }

    /// <summary>
    /// Calls the visitor.Visit(InitializeObject) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (this.ContainingBlock == containingBlock) return this;
      return new InitializeObject(containingBlock, this);
    }

    /// <summary>
    /// A list of (identifier, expression) pairs used to initialize the contructed object via assignments to fields and properties.
    /// </summary>
    public IEnumerable<NamedArgument> NamedArguments {
      get { return this.namedArguments; }
    }
    readonly IEnumerable<NamedArgument> namedArguments;

    /// <summary>
    /// The newly constructed object to initialize.
    /// </summary>
    public Expression ObjectToInitialize {
      get {
        //^ assume this.objectToInitialize != null;
        return this.objectToInitialize;
      }
    }
    Expression/*?*/ objectToInitialize;

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      return new DummyExpression(this.SourceLocation);
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingExpression(Expression containingExpression) {
      base.SetContainingExpression(containingExpression);
      this.objectToInitialize = containingExpression;
    }

    /// <summary>
    /// The type of value that the expression will evaluate to, as determined at compile time.
    /// </summary>
    public override ITypeDefinition Type {
      get { return Dummy.Type; }
    }

  }

  /// <summary>
  /// An expression that divides the value of the left operand by the value of the right operand. The result is always an integer (i.e. the fraction is discarded).
  /// When overloaded, this expression corresponds to a call to op_IntegerDivision.
  /// </summary>
  public class IntegerDivision : BinaryOperation {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="leftOperand"></param>
    /// <param name="rightOperand"></param>
    /// <param name="sourceLocation"></param>
    public IntegerDivision(Expression leftOperand, Expression rightOperand, ISourceLocation sourceLocation)
      : base(leftOperand, rightOperand, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected IntegerDivision(BlockStatement containingBlock, IntegerDivision template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Calls the visitor.Visit(IntegerDivision) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Returns the string used to identify the operator in error messages
    /// </summary>
    protected override string OperationSymbolForErrorMessage {
      get { return "\\"; }
    }


    /// <summary>
    /// Returns the name a method must have to overload this operator.
    /// </summary>
    protected override IName GetOperatorName() {
      return this.NameTable.OpIntegerDivision;
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new IntegerDivision(containingBlock, this);
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      MethodCall/*?*/ overloadedMethodCall = this.OverloadMethodCall;
      if (overloadedMethodCall != null) return overloadedMethodCall;
      //TODO: coerce the operands and/or the result;
      return new Division(this.LeftOperand, this.RightOperand, this.SourceLocation);
    }

    /// <summary>
    /// A list of dummy methods that correspond to operations that are built into IL. The dummy methods are used, via overload resolution,
    /// to determine how the operands are to be converted before the operation is carried out.
    /// </summary>
    protected override IEnumerable<IMethodDefinition> StandardOperators {
      get {
        return Enumerable<IMethodDefinition>.Empty; //TODO: implement this
      }
    }

  }

  /// <summary>
  /// An object that represents a group of types. It is an intermediate artifact
  /// to be used while resolving a name forming part of a generic type instance expression in the absence of knowledge of how many type arguments will be used to
  /// make the instantiation.
  /// </summary>
  public interface ITypeGroup {

    /// <summary>
    /// The (name) expression to resolve.
    /// </summary>
    IExpression Expression { get; }

    /// <summary>
    /// The types that Expression resolves to, given a particular number of generic parameters.
    /// </summary>
    IEnumerable<ITypeDefinition> GetTypes(int numberOfTypeParameters);
    // ^ ensures forall{ITypeDefinition type in result; type.GenericParameterCount == numberOfTypeParameters};

    /// <summary>
    /// The types that Expression resolves to.
    /// </summary>
    IEnumerable<ITypeDefinition> Types { get; }
  }

  /// <summary>
  /// An expression that returns true if the value of the its operand is false or can be converted to false. 
  /// In VB the IsFalse operator corresponds to this kind of expression. When overloaded, this expression corresponds to a call to op_False.
  /// </summary>
  public class IsFalse : UnaryOperation {

    /// <summary>
    /// Allocates an expression that returns true if the value of the its operand is false or can be converted to false. 
    /// In VB the IsFalse operator corresponds to this kind of expression. When overloaded, this expression corresponds to a call to op_False.
    /// </summary>
    /// <param name="operand">The value on which the operation is performed.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public IsFalse(Expression operand, ISourceLocation sourceLocation)
      : base(operand, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected IsFalse(BlockStatement containingBlock, IsFalse template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Calls the visitor.Visit(IsFalse) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Returns the name a method must have to overload this operator.
    /// </summary>
    protected override IName GetOperatorName() {
      return this.NameTable.OpFalse;
    }

    /// <summary>
    /// Returns the string used to identify the operator in error messages
    /// </summary>
    protected override string OperationSymbolForErrorMessage {
      get { return "IsFalse"; }
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new IsFalse(containingBlock, this);
    }

    /// <summary>
    /// A list of dummy methods that correspond to operations that are built into IL. The dummy methods are used, via overload resolution,
    /// to determine how the operands are to be converted before the operation is carried out.
    /// </summary>
    protected override IEnumerable<IMethodDefinition> StandardOperators {
      get {
        BuiltinMethods dummyMethods = this.Compilation.BuiltinMethods;
        yield return dummyMethods.OpBoolean;
      }
    }
  }

  /// <summary>
  /// An expression that returns true if the value of the its operand is true or can be converted to true. 
  /// When overloaded, this expression corresponds to a call to op_True.
  /// </summary>
  public class IsTrue : UnaryOperation {

    /// <summary>
    /// Allocates an expression that returns true if the value of the its operand is true or can be converted to true. 
    /// When overloaded, this expression corresponds to a call to op_True.
    /// </summary>
    /// <param name="operand">The value on which the operation is performed.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public IsTrue(Expression operand, ISourceLocation sourceLocation)
      : base(operand, sourceLocation) {
    }

    /// <summary>
    /// Allocates an expression that returns true if the value of the its operand is true or can be converted to true. 
    /// When overloaded, this expression corresponds to a call to op_True.
    /// </summary>
    /// <param name="operand">The value on which the operation is performed. Must be fully initialized.</param>
    public IsTrue(Expression operand)
      : base(operand, operand.SourceLocation) {
      this.SetContainingBlock(operand.ContainingBlock);
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected IsTrue(BlockStatement containingBlock, IsTrue template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      if (this.Operand.HasErrors) return true;
      return this.Type is Dummy;
    }

    /// <summary>
    /// Calls the visitor.Visit(IsTrue) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Returns the name a method must have to overload this operator.
    /// </summary>
    protected override IName GetOperatorName() {
      return this.NameTable.OpTrue;
    }

    /// <summary>
    /// Returns the string used to identify the operator in error messages
    /// </summary>
    protected override string OperationSymbolForErrorMessage {
      get { return "IsTrue"; }
    }

    /// <summary>
    /// Computes the compile time value of the expression. Can be null.
    /// </summary>
    protected override object/*?*/ GetValue() {
      Expression/*?*/ e = this.ProjectAsNonConstantIExpression() as Expression;
      if (e != null && e != this) return e.Value;
      return null;
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new IsTrue(containingBlock, this);
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    /// <returns></returns>
    protected override IExpression ProjectAsNonConstantIExpression() {
      if (!this.HasErrors) {
        MethodCall/*?*/ overloadedMethodCall = this.OverloadMethodCall;
        if (overloadedMethodCall != null) {
          if (overloadedMethodCall.ResolvedMethod is BuiltinMethodDefinition)
            return ((IUnaryOperation)this).Operand;
          else
            return overloadedMethodCall;
        }
      }
      return this;
    }

    /// <summary>
    /// A list of dummy methods that correspond to operations that are built into IL. The dummy methods are used, via overload resolution,
    /// to determine how the operands are to be converted before the operation is carried out.
    /// </summary>
    protected override IEnumerable<IMethodDefinition> StandardOperators {
      get {
        BuiltinMethods dummyMethods = this.Compilation.BuiltinMethods;
        yield return dummyMethods.OpBoolean;
      }
    }

  }

  /// <summary>
  /// A list of parameters and a body in the form of an expression or a block, that together form an anonymous delegate value.
  /// </summary>
  public class Lambda : Expression {

    /// <summary>
    /// Allocates a list of parameters and a body in the form of an expression or a block, that together form an anonymous delegate value.
    /// </summary>
    /// <param name="parameters">The parameters.</param>
    /// <param name="expression">The expression that specifies the result of calling the lambda. May be null if body is not null.</param>
    /// <param name="body">A block of statements that contains a return statement that returns the value of calling the lambda. May be null if expression is not null.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public Lambda(List<LambdaParameter> parameters, Expression/*?*/ expression, BlockStatement/*?*/ body, ISourceLocation sourceLocation)
      : base(sourceLocation)
      //^ requires expression == null <==> body != null;
      //^ requires expression != null <==> body == null;
    {
      this.parameters = parameters;
      this.expression = expression;
      this.body = body;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected Lambda(BlockStatement containingBlock, Lambda template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.parameters = new List<LambdaParameter>(template.Parameters);
      if (template.expression != null) this.expression = template.expression.MakeCopyFor(containingBlock);
      if (template.body != null) this.body = template.body; // TODO: do we copy this?
    }

    /// <summary>
    /// A block of statements that contains a return statement that returns the value of calling the lambda.
    /// </summary>
    public BlockStatement/*?*/ Body {
      get
        //^ ensures result == null <==> this.Expression != null;
        //^ ensures result != null <==> this.Expression == null;
        //^ ensures result == this.body;
      {
        //^ assume this.expression == this.Expression;
        return this.body;
      }
    }
    //^ [SpecPublic]
    readonly BlockStatement/*?*/ body;

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return false; //TODO: implement this
    }

    /// <summary>
    /// Calls the visitor.Visit(Lambda) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The expression that specifies the result of calling the lambda.
    /// </summary>
    public Expression/*?*/ Expression {
      get
        //^ ensures result == null <==> this.Body != null;
        //^ ensures result != null <==> this.Body == null;
        //^ ensures result == this.expression;
      {
        //^ assume this.body == this.Body;
        return this.expression;
      }
    }
    //^ [SpecPublic]
    readonly Expression/*?*/ expression;
    //^ invariant this.expression == null <==> this.body != null;
    //^ invariant this.expression != null <==> this.body == null;

    /// <summary>
    /// Checks if the expression has a side effect and reports an error unless told otherwise.
    /// </summary>
    /// <param name="reportError">If true, report an error if the expression has a side effect.</param>
    public override bool HasSideEffect(bool reportError) {
      return false; //TODO: implement this
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new Lambda(containingBlock, this);
    }

    /// <summary>
    /// The parameters.
    /// </summary>
    public IEnumerable<LambdaParameter> Parameters {
      get {
        for (int i = 0; i < this.parameters.Count; i++) {
          LambdaParameter param = this.parameters[i];
          this.parameters[i] = param = param.MakeCopyFor(this);
          yield return param;
        }
      }
    }
    readonly List<LambdaParameter> parameters;

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      return new DummyExpression(this.SourceLocation);
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingExpression(Expression containingExpression) {
      base.SetContainingExpression(containingExpression);
      if (this.expression != null) this.expression.SetContainingExpression(this);
      if (this.body != null) this.body.SetContainingBlock(containingExpression.ContainingBlock);
      foreach (LambdaParameter parameter in this.parameters) parameter.SetContainingLambda(this);
    }

    /// <summary>
    /// The type of value that the expression will evaluate to, as determined at compile time.
    /// </summary>
    public override ITypeDefinition Type {
      get { return Dummy.Type; }
    }

  }

  /// <summary>
  /// A parameter for a lambda expression.
  /// </summary>
  public class LambdaParameter : SourceItem {

    /// <summary>
    /// Allocates a parameter for a lambda expression.
    /// </summary>
    /// <param name="isOut">True if the lambda assigns a value to this parameter.</param>
    /// <param name="isRef">True if the argument value is passed by reference.</param>
    /// <param name="parameterType">The type of value that may be assigned to this parameter. May be null, in which case the type is inferred from usage.</param>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public LambdaParameter(bool isOut, bool isRef, TypeExpression/*?*/ parameterType, NameDeclaration parameterName, ISourceLocation sourceLocation)
      : base(sourceLocation)
      //^ requires isOut ==> !isRef;
    {
      int flags = 0;
      if (isOut) flags |= 1;
      if (isRef) flags |= 2;
      //^ assume flags >= 0 && flags <= 2;
      this.flags = flags;
      this.parameterType = parameterType;
      this.parameterName = parameterName;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingLambda">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected LambdaParameter(Lambda containingLambda, LambdaParameter template)
      : base(template.SourceLocation)
      //^ requires template.ContainingLambda != containingLambda;
    {
      this.containingLambda = containingLambda;
      this.flags = template.flags;
      if (template.parameterType != null) this.parameterType = (TypeExpression)template.parameterType.MakeCopyFor(containingLambda.ContainingBlock);
      this.parameterName = template.parameterName.MakeCopyFor(containingLambda.ContainingBlock.Compilation);
    }

    /// <summary>
    /// The lambda expression that is parameterized by this parameter.
    /// </summary>
    public Lambda ContainingLambda {
      get {
        //^ assume this.containingLambda != null;
        return this.containingLambda;
      }
    }
    Lambda/*?*/ containingLambda;

    /// <summary>
    /// Calls the visitor.Visit(LambdaParameter) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// 1 == IsOut, 2 == IsRef, 
    /// </summary>
    readonly int flags; //^ invariant flags >= 0 && flags <= 2;

    /// <summary>
    /// True if the lambda assigns a value to this parameter.
    /// </summary>
    public bool IsOut {
      get { return (this.flags & 1) != 0; }
    }

    /// <summary>
    /// True if the argument value is passed by reference.
    /// </summary>
    public bool IsRef {
      get { return (this.flags & 2) != 0; }
    }

    /// <summary>
    /// Makes a copy of this lambda parameter, changing the ContainingLambda to the given lambda.
    /// </summary>
    //^ [MustOverride]
    public virtual LambdaParameter MakeCopyFor(Lambda containingLambda) {
      if (this.ContainingLambda == containingLambda) return this;
      return new LambdaParameter(containingLambda, this);
    }

    /// <summary>
    /// The type of value that may be assigned to this parameter. May be null, in which case the type is inferred from usage.
    /// </summary>
    public TypeExpression/*?*/ ParameterType {
      get { return this.parameterType; }
    }
    readonly TypeExpression/*?*/ parameterType;

    /// <summary>
    /// The name of the parameter.
    /// </summary>
    public NameDeclaration ParameterName {
      get { return this.parameterName; }
    }
    readonly NameDeclaration parameterName;

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a LambdaParameter before constructing the containing Lambda.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public virtual void SetContainingLambda(Lambda containingLambda) {
      this.containingLambda = containingLambda;
      if (this.parameterType != null) this.parameterType.SetContainingExpression(containingLambda);
    }

  }

  /// <summary>
  /// An expression that results in the value of the left operand, shifted left by the number of bits specified by the value of the right operand.
  /// When the operator is overloaded, this expression corresponds to a call to op_LeftShift.
  /// </summary>
  public class LeftShift : BinaryOperation, ILeftShift {

    /// <summary>
    /// Allocates an expression that results in the value of the left operand, shifted left by the number of bits specified by the value of the right operand.
    /// When the operator is overloaded, this expression corresponds to a call to op_LeftShift.
    /// </summary>
    /// <param name="leftOperand">The left operand.</param>
    /// <param name="rightOperand">The right operand.</param>
    /// <param name="sourceLocation">The source location of the operation.</param>
    public LeftShift(Expression leftOperand, Expression rightOperand, ISourceLocation sourceLocation)
      : base(leftOperand, rightOperand, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected LeftShift(BlockStatement containingBlock, LeftShift template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// True if the constant is a positive integer that could be interpreted as a negative signed integer.
    /// For example, 0x80000000, could be interpreted as a convenient way of writing int.MinValue.
    /// </summary>
    public override bool CouldBeInterpretedAsNegativeSignedInteger {
      get { return this.Value != null && this.LeftOperand.CouldBeInterpretedAsNegativeSignedInteger; }
    }

    /// <summary>
    /// Calls the visitor.Visit(ILeftShift) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(LeftShift) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Returns the string used to identify the operator in error messages
    /// </summary>
    protected override string OperationSymbolForErrorMessage {
      get { return "<<"; }
    }


    /// <summary>
    /// Returns the name a method must have to overload this operator.
    /// </summary>
    protected override IName GetOperatorName() {
      return this.NameTable.OpLeftShift;
    }

    /// <summary>
    /// Computes the compile time value of the expression. Can be null.
    /// </summary>
    protected override object/*?*/ GetValue() {
      object/*?*/ left = this.ConvertedLeftOperand.Value;
      object/*?*/ right = this.ConvertedRightOperand.Value;
      if (left == null || right == null) return null;
      switch (System.Convert.GetTypeCode(left)) {
        case TypeCode.Int32:
          //^ assume left is int && right is int;
          return (int)left << (int)right;
        case TypeCode.UInt32:
          //^ assume left is uint && right is int;
          return (uint)left << (int)right;
        case TypeCode.Int64:
          //^ assume left is long && right is int;
          return (long)left << (int)right;
        case TypeCode.UInt64:
          //^ assume left is ulong && right is int;
          return (ulong)left << (int)right;
      }
      return null;
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new LeftShift(containingBlock, this);
    }

    /// <summary>
    /// A list of dummy methods that correspond to operations that are built into IL. The dummy methods are used, via overload resolution,
    /// to determine how the operands are to be converted before the operation is carried out.
    /// </summary>
    protected override IEnumerable<IMethodDefinition> StandardOperators {
      get {
        BuiltinMethods dummyMethods = this.Compilation.BuiltinMethods;
        yield return dummyMethods.Int32opInt32;
        yield return dummyMethods.UInt32opInt32;
        yield return dummyMethods.Int64opInt32;
        yield return dummyMethods.UInt64opInt32;
      }
    }

    /// <summary>
    /// Returns true if the expression represents a compile time constant without an explicitly specified type. For example, 1 rather than 1L.
    /// Constant expressions such as 2*16 are polymorhpic if both operands are polymorhic.
    /// </summary>
    public override bool ValueIsPolymorphicCompileTimeConstant {
      get { return this.Value != null && this.LeftOperand.ValueIsPolymorphicCompileTimeConstant; }
    }

  }

  /// <summary>
  /// An expression that results in the value of the left operand, shifted left by the number of bits specified by the value of the right operand.
  /// The result of the expression is assigned to the left operand, which must be a target expression.
  /// When the operator is overloaded, this expression corresponds to a call to op_LeftShift.
  /// </summary>
  public class LeftShiftAssignment : BinaryOperationAssignment {

    /// <summary>
    /// Allocates an expression that results in the value of the left operand, shifted left by the number of bits specified by the value of the right operand.
    /// The result of the expression is assigned to the left operand, which must be a target expression.
    /// When the operator is overloaded, this expression corresponds to a call to op_LeftShift.
    /// </summary>
    /// <param name="leftOperand">The left operand and target of the assignment.</param>
    /// <param name="rightOperand">The right operand.</param>
    /// <param name="sourceLocation">The source location of the operation.</param>
    public LeftShiftAssignment(TargetExpression leftOperand, Expression rightOperand, ISourceLocation sourceLocation)
      : base(leftOperand, rightOperand, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected LeftShiftAssignment(BlockStatement containingBlock, LeftShiftAssignment template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Calls the visitor.Visit(LeftShiftAssignment) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (this.ContainingBlock == containingBlock) return this;
      return new LeftShiftAssignment(containingBlock, this);
    }

    /// <summary>
    /// Creates a left shift expression with the given left operand and this.RightOperand.
    /// The method does not use this.LeftOperand.Expression, since it may be necessary to factor out any subexpressions so that
    /// they are evaluated only once. The given left operand expression is expected to be the expression that remains after factoring.
    /// </summary>
    /// <param name="leftOperand">An expression to combine with this.RightOperand into a binary expression.</param>
    protected override Expression CreateBinaryExpression(Expression leftOperand) {
      Expression result = new LeftShift(leftOperand, this.RightOperand, this.SourceLocation);
      result.SetContainingExpression(this);
      return result;
    }
  }

  /// <summary>
  /// An expression that results in true if the value of the left operand is less than the value of the right operand.
  /// When overloaded, this expression corresponds to a call to op_LessThan.
  /// </summary>
  public class LessThan : Comparison, ILessThan {

    /// <summary>
    /// Allocates an expression that results in true if the value of the left operand is less than the value of the right operand.
    /// When overloaded, this expression corresponds to a call to op_LessThan.
    /// </summary>
    /// <param name="leftOperand">The left operand.</param>
    /// <param name="rightOperand">The right operand.</param>
    /// <param name="sourceLocation">The source location of the operation.</param>
    public LessThan(Expression leftOperand, Expression rightOperand, ISourceLocation sourceLocation)
      : base(leftOperand, rightOperand, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected LessThan(BlockStatement containingBlock, LessThan template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Calls the visitor.Visit(ILessThan) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(LessThan) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Returns the string used to identify the operator in error messages
    /// </summary>
    protected override string OperationSymbolForErrorMessage {
      get { return "<"; }
    }


    /// <summary>
    /// Returns the name a method must have to overload this operator.
    /// </summary>
    protected override IName GetOperatorName() {
      return this.NameTable.OpLessThan;
    }

    /// <summary>
    /// Computes the compile time value of the expression. Can be null.
    /// </summary>
    protected override object/*?*/ GetValue() {
      object/*?*/ left = this.ConvertedLeftOperand.Value;
      object/*?*/ right = this.ConvertedRightOperand.Value;
      if (left == null || right == null) return null;
      switch (System.Convert.GetTypeCode(left)) {
        case TypeCode.Int32:
          //^ assume left is int && right is int;
          return (int)left < (int)right;
        case TypeCode.UInt32:
          //^ assume left is uint && right is uint;
          return (uint)left < (uint)right;
        case TypeCode.Int64:
          //^ assume left is long && right is long;
          return (long)left < (long)right;
        case TypeCode.UInt64:
          //^ assume left is ulong && right is ulong;
          return (ulong)left < (ulong)right;
        case TypeCode.Single:
          //^ assume left is float && right is float;
          return (float)left < (float)right;
        case TypeCode.Double:
          //^ assume left is double && right is double;
          return (double)left < (double)right;
        case TypeCode.Decimal:
          //^ assume left is decimal && right is decimal;
          return (decimal)left < (decimal)right;
      }
      return null;
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new LessThan(containingBlock, this);
    }

  }

  /// <summary>
  /// An expression that results in true if the value of the left operand is less than or equal to the value of the right operand.
  /// When overloaded, this expression corresponds to a call to op_LessThanOrEqual.
  /// </summary>
  public class LessThanOrEqual : Comparison, ILessThanOrEqual {

    /// <summary>
    /// Allocates an expression that results in true if the value of the left operand is less than or equal to the value of the right operand.
    /// When overloaded, this expression corresponds to a call to op_LessThanOrEqual.
    /// </summary>
    /// <param name="leftOperand">The left operand.</param>
    /// <param name="rightOperand">The right operand.</param>
    /// <param name="sourceLocation">The source location of the operation.</param>
    public LessThanOrEqual(Expression leftOperand, Expression rightOperand, ISourceLocation sourceLocation)
      : base(leftOperand, rightOperand, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected LessThanOrEqual(BlockStatement containingBlock, LessThanOrEqual template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Calls the visitor.Visit(ILessThanOrEqual) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(LessThanOrEqual) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Returns the string used to identify the operator in error messages
    /// </summary>
    protected override string OperationSymbolForErrorMessage {
      get { return "<="; }
    }


    /// <summary>
    /// Returns the name a method must have to overload this operator.
    /// </summary>
    protected override IName GetOperatorName() {
      return this.NameTable.OpLessThanOrEqual;
    }

    /// <summary>
    /// Computes the compile time value of the expression. Can be null.
    /// </summary>
    protected override object/*?*/ GetValue() {
      object/*?*/ left = this.ConvertedLeftOperand.Value;
      object/*?*/ right = this.ConvertedRightOperand.Value;
      if (left == null || right == null) return null;
      switch (System.Convert.GetTypeCode(left)) {
        case TypeCode.Int32:
          //^ assume left is int && right is int;
          return (int)left <= (int)right;
        case TypeCode.UInt32:
          //^ assume left is uint && right is uint;
          return (uint)left <= (uint)right;
        case TypeCode.Int64:
          //^ assume left is long && right is long;
          return (long)left <= (long)right;
        case TypeCode.UInt64:
          //^ assume left is ulong && right is ulong;
          return (ulong)left <= (ulong)right;
        case TypeCode.Single:
          //^ assume left is float && right is float;
          return (float)left <= (float)right;
        case TypeCode.Double:
          //^ assume left is double && right is double;
          return (double)left <= (double)right;
        case TypeCode.Decimal:
          //^ assume left is decimal && right is decimal;
          return (decimal)left <= (decimal)right;
      }
      return null;
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new LessThanOrEqual(containingBlock, this);
    }

  }

  /// <summary>
  /// Converts a nullable value of one type into a nullable value of another type. Does not unbox the source value if it is null.
  /// </summary>
  public class LiftedConversion : Conversion {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="valueToConvert"></param>
    /// <param name="userDefinedUnliftedConversion"></param>
    /// <param name="resultType"></param>
    /// <param name="sourceLocation"></param>
    public LiftedConversion(Expression valueToConvert, IMethodDefinition/*?*/ userDefinedUnliftedConversion, ITypeDefinition resultType, ISourceLocation sourceLocation)
      : base(valueToConvert, resultType, sourceLocation) {
      this.userDefinedUnliftedConversion = userDefinedUnliftedConversion;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    public LiftedConversion(BlockStatement containingBlock, LiftedConversion template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.userDefinedUnliftedConversion = template.UserDefinedUnliftedConversion;
    }

    /// <summary>
    /// Should never be called
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      Debug.Assert(false, "A lifted conversion expression should never escape into the CodeModel");
    }

    /// <summary>
    /// Calls the visitor.Visit(LiftedConversion) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new LiftedConversion(containingBlock, this);
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    /// <returns></returns>
    protected override IExpression ProjectAsNonConstantIExpression() {
      if (this.cachedProjection != null) return this.cachedProjection;
      NameDeclaration tempName = new NameDeclaration(Dummy.Name, SourceDummy.SourceLocation);
      LocalDeclaration valToConvert = new LocalDeclaration(false, false, tempName, this.ValueToConvert, this.SourceLocation);
      Expression addressOfValToConvert = new AddressOf(new AddressableExpression(new BoundExpression(this, valToConvert.LocalVariable, this.ContainingBlock)), this.SourceLocation);
      addressOfValToConvert.SetContainingExpression(this);

      IMethodDefinition getHasValue = Dummy.Method;
      foreach (ITypeDefinitionMember member in this.ValueToConvert.Type.GetMembersNamed(this.NameTable.GetNameFor("get_HasValue"), false)) {
        if (member is IMethodDefinition) { getHasValue = (IMethodDefinition)member; break; }
      }
      Expression valueToConvertIsNotNull = new ResolvedMethodCall(getHasValue, addressOfValToConvert, new List<Expression>(0), this.SourceLocation);

      IMethodDefinition getValueOrDefault = Dummy.Method;
      foreach (ITypeDefinitionMember member in this.ValueToConvert.Type.GetMembersNamed(this.NameTable.GetNameFor("GetValueOrDefault"), false)) {
        IMethodDefinition/*?*/ meth = member as IMethodDefinition;
        if (meth == null || IteratorHelper.EnumerableIsNotEmpty(meth.Parameters)) continue;
        getValueOrDefault = meth;
        break;
      }
      Expression unboxedValue = new ResolvedMethodCall(getValueOrDefault, addressOfValToConvert, new List<Expression>(0), this.SourceLocation);

      List<Expression> argumentListContainingUnboxedValue = new List<Expression>(1);
      argumentListContainingUnboxedValue.Add(unboxedValue);

      Expression convertedUnboxedValue;
      if (this.UserDefinedUnliftedConversion != null)
        convertedUnboxedValue = new ResolvedMethodCall(this.UserDefinedUnliftedConversion, argumentListContainingUnboxedValue, this.SourceLocation);
      else
        convertedUnboxedValue = new Conversion(unboxedValue, this.Helper.RemoveNullableWrapper(this.Type), this.SourceLocation);

      Expression convertedBoxedValue = this.Helper.ExplicitConversion(convertedUnboxedValue, this.Type);
      Expression targetNullValue = new DefaultValue(TypeExpression.For(this.Type), this.SourceLocation);

      List<LocalDeclaration> declarations = new List<LocalDeclaration>(1);
      declarations.Add(valToConvert);
      List<Statement> statements = new List<Statement>(2);
      statements.Add(new LocalDeclarationsStatement(false, false, false, TypeExpression.For(this.ValueToConvert.Type), declarations, this.SourceLocation));
      BlockStatement block = new BlockStatement(statements, this.SourceLocation);
      Conditional conditional = new Conditional(valueToConvertIsNotNull, convertedBoxedValue, targetNullValue, this.SourceLocation);
      BlockExpression be = new BlockExpression(block, conditional, this.SourceLocation);
      be.SetContainingExpression(this);
      return this.cachedProjection = be;
    }
    IExpression/*?*/ cachedProjection;

    /// <summary>
    /// The conversion to lift. If this is null, the unlifted conversion is built-in (for example from int to long).
    /// </summary>
    public IMethodDefinition/*?*/ UserDefinedUnliftedConversion {
      get { return this.userDefinedUnliftedConversion; }
    }
    readonly IMethodDefinition/*?*/ userDefinedUnliftedConversion;

  }

  /// <summary>
  /// An expression that results in true if the string value of the left operand matches the regular expression contained in the right operand.
  /// In VB the Like operator corresponds to this expression. When overloaded this expression corresponds to a call to op_Like.
  /// </summary>
  public class Like : BinaryOperation {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="leftOperand"></param>
    /// <param name="rightOperand"></param>
    /// <param name="sourceLocation"></param>
    public Like(Expression leftOperand, Expression rightOperand, ISourceLocation sourceLocation)
      : base(leftOperand, rightOperand, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected Like(BlockStatement containingBlock, Like template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Calls the visitor.Visit(Like) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Returns the string used to identify the operator in error messages
    /// </summary>
    protected override string OperationSymbolForErrorMessage {
      get { return "Like"; }
    }


    /// <summary>
    /// Returns the name a method must have to overload this operator.
    /// </summary>
    protected override IName GetOperatorName() {
      return this.NameTable.OpLike;
    }

    /// <summary>
    /// Infers the type of value that this expression will evaluate to. At runtime the actual value may be an instance of subclass of the result of this method.
    /// Calling this method does not cache the computed value and does not generate any error messages. In some cases, such as references to the parameters of lambda
    /// expressions during type overload resolution, the value returned by this method may be different from one call to the next.
    /// When type inference fails, Dummy.Type is returned.
    /// </summary>
    public override ITypeDefinition InferType() {
      return this.PlatformType.SystemBoolean.ResolvedType;
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new Like(containingBlock, this);
    }

  }

  /// <summary>
  /// An expression that results in true if both operands result in true. If the left operand results in false, the right operand is not evaluated.
  /// When overloaded, this expression corresponds to calls to op_False and op_BitwiseAnd.
  /// </summary>
  public class LogicalAnd : LogicalBinaryOperation {

    /// <summary>
    /// Allocates an expression that results in true if both operands result in true. If the left operand results in false, the right operand is not evaluated.
    /// When overloaded, this expression corresponds to calls to op_False and op_BitwiseAnd.
    /// </summary>
    /// <param name="leftOperand">The left operand.</param>
    /// <param name="rightOperand">The right operand.</param>
    /// <param name="sourceLocation">The source location of the operation.</param>
    public LogicalAnd(Expression leftOperand, Expression rightOperand, ISourceLocation sourceLocation)
      : base(leftOperand, rightOperand, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected LogicalAnd(BlockStatement containingBlock, LogicalAnd template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Calls the visitor.Visit(LogicalAnd) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Returns the string used to identify the operator in error messages
    /// </summary>
    protected override string OperationSymbolForErrorMessage {
      get { return "&&"; }
    }


    /// <summary>
    /// Returns the name a method must have to overload this operator.
    /// </summary>
    protected override IName GetOperatorName() {
      return this.NameTable.OpBitwiseAnd;
    }

    /// <summary>
    /// Computes the compile time value of the expression. Can be null.
    /// </summary>
    protected override object/*?*/ GetValue() {
      object/*?*/ left = this.ConvertedLeftOperand.Value;
      if (left is bool) {
        if (!(bool)left) return false;
        object/*?*/ right = this.ConvertedRightOperand.Value;
        if (right is bool) return (bool)right;
      }
      return null;
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new LogicalAnd(containingBlock, this);
    }

    /// <summary>
    /// Returns the projection of (bool)this.LeftOperand ? (bool)this.RightOperand : false;
    /// </summary>
    protected override IExpression ProjectAsStandardLogicalOperator() {
      Expression x = this.Helper.ImplicitConversion(this.LeftOperand, this.PlatformType.SystemBoolean.ResolvedType);
      Expression y = this.Helper.ImplicitConversion(this.RightOperand, this.PlatformType.SystemBoolean.ResolvedType);
      Expression result = new Conditional(x, y, new CompileTimeConstant(false, SourceDummy.SourceLocation), this.SourceLocation);
      result.SetContainingExpression(this);
      return result.ProjectAsIExpression();
    }

    /// <summary>
    /// Returns the projection of let tx = (T)x in T.op_False(tx) ? tx : T.op_BitwiseAnd(tx, (T)y).
    /// </summary>
    /// <param name="overloadMethodCall">The user defined overload of the bitwise and operator.</param>
    protected override IExpression ProjectAsUserDefinedOverloadAsLogicalOperator(IMethodCall overloadMethodCall) {
      ITypeDefinition T = overloadMethodCall.MethodToCall.ResolvedMethod.Type.ResolvedType;

      //let tx = (T)x
      List<Statement> statements = new List<Statement>(1);
      Expression txe = this.Helper.ImplicitConversion(this.LeftOperand, T);
      ILocalDefinition txVar = Expression.CreateInitializedLocalDeclarationAndAddDeclarationsStatementToList(txe, statements);
      BoundExpression tx = new BoundExpression(txe, txVar);
      BlockStatement block = new BlockStatement(statements, this.SourceLocation);

      //T.op_False(tx)
      IsFalse convertedLeftOperandIsFalse = new IsFalse(tx, tx.SourceLocation);

      //T.op_BitwiseAnd(tx, y)
      List<Expression> args = new List<Expression>(2);
      args.Add(tx);
      args.Add(this.Helper.ImplicitConversion(this.RightOperand, T));
      ResolvedMethodCall bitWiseAnd = new ResolvedMethodCall(overloadMethodCall.MethodToCall.ResolvedMethod, args, this.SourceLocation);

      Conditional conditional = new Conditional(convertedLeftOperandIsFalse, tx, bitWiseAnd, this.SourceLocation);
      BlockExpression be = new BlockExpression(block, conditional, this.SourceLocation);
      be.SetContainingExpression(this);
      return be.ProjectAsIExpression();
    }

  }

  /// <summary>
  /// A binary operation performed on a left and right operand, but with short circuiting (not evaluating the right operand, depending on the value of the left operand).
  /// </summary>
  public abstract class LogicalBinaryOperation : BinaryOperation {

    /// <summary>
    /// Initializes a binary operation performed on a left and right operand, but with short circuiting (not evaluating the right operand, depending on the value of the left operand).
    /// </summary>
    /// <param name="leftOperand">The left operand.</param>
    /// <param name="rightOperand">The right operand.</param>
    /// <param name="sourceLocation">The source location of the operation.</param>
    protected LogicalBinaryOperation(Expression leftOperand, Expression rightOperand, ISourceLocation sourceLocation)
      : base(leftOperand, rightOperand, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected LogicalBinaryOperation(BlockStatement containingBlock, BinaryOperation template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      if (!this.HasErrors) {
        IMethodCall/*?*/ overloadMethodCall = this.OverloadMethodCall;
        if (overloadMethodCall != null) {
          if (!(overloadMethodCall.MethodToCall.ResolvedMethod is BuiltinMethodDefinition))
            return this.ProjectAsUserDefinedOverloadAsLogicalOperator(overloadMethodCall);
          else
            return this.ProjectAsStandardLogicalOperator();
        } else {
          //^ assume false; //the call to this.HasErrors will return false if this.OverloadMethodCall returns null.
        }
      }
      return new DummyExpression(this.SourceLocation);
    }

    /// <summary>
    /// Returns an expression that is the projection of a standard short circuit operator.
    /// In the case of a logical and, this is the projection of (bool)x ? (bool)y : false;
    /// </summary>
    protected abstract IExpression ProjectAsStandardLogicalOperator();

    /// <summary>
    /// Returns an expression that is the projection of a user defined short circuit operator.
    /// In the case of a logical or, this is the projection of let tx = (T)x in T.op_True(tx) ? tx : T.op_BitwiseOr(tx, (T)y).
    /// </summary>
    protected abstract IExpression ProjectAsUserDefinedOverloadAsLogicalOperator(IMethodCall overloadMethodCall);

    /// <summary>
    /// A list of dummy methods that correspond to operations that are built into IL. The dummy methods are used, via overload resolution,
    /// to determine how the operands are to be converted before the operation is carried out.
    /// </summary>
    protected override IEnumerable<IMethodDefinition> StandardOperators {
      get {
        BuiltinMethods dummyMethods = this.Compilation.BuiltinMethods;
        yield return dummyMethods.BoolOpBool;
      }
    }

  }

  /// <summary>
  /// An expression that results in the logical negation of the boolean value of the given operand. When overloaded, this expression corresponds to a call to op_LogicalNot.
  /// </summary>
  public class LogicalNot : UnaryOperation, ILogicalNot {

    /// <summary>
    /// Allocates an expression that results in the logical negation of the boolean value of the given operand. When overloaded, this expression corresponds to a call to op_LogicalNot.
    /// </summary>
    /// <param name="operand">The value on which the operation is performed.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public LogicalNot(Expression operand, ISourceLocation sourceLocation)
      : base(operand, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected LogicalNot(BlockStatement containingBlock, LogicalNot template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Calls the visitor.Visit(ILogicalNot) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(LogicalNot) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Returns the name a method must have to overload this operator.
    /// </summary>
    protected override IName GetOperatorName() {
      return this.NameTable.OpLogicalNot;
    }

    /// <summary>
    /// Computes the compile time value of the expression. Can be null.
    /// </summary>
    protected override object/*?*/ GetValue() {
      object/*?*/ val = this.ConvertedOperand.Value;
      if (val is bool) return !(bool)val;
      return null;
    }

    /// <summary>
    /// Returns the string used to identify the operator in error messages
    /// </summary>
    protected override string OperationSymbolForErrorMessage {
      get { return "!"; }
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new LogicalNot(containingBlock, this);
    }

    /// <summary>
    /// A list of dummy methods that correspond to operations that are built into IL. The dummy methods are used, via overload resolution,
    /// to determine how the operands are to be converted before the operation is carried out.
    /// </summary>
    protected override IEnumerable<IMethodDefinition> StandardOperators {
      get {
        BuiltinMethods dummyMethods = this.Compilation.BuiltinMethods;
        yield return dummyMethods.OpBoolean;
      }
    }

    #region IExpression Members

    ITypeReference IExpression.Type {
      get { return this.Type; }
    }

    #endregion
  }

  /// <summary>
  /// An expression that results in true if either operand results in true. If the left operand results in true, the right operand is not evaluated.
  /// When overloaded, this expression corresponds to calls to op_True and op_BitwiseOr.
  /// </summary>
  public class LogicalOr : LogicalBinaryOperation {

    /// <summary>
    /// Allocates an expression that results in true if either operand results in true. If the left operand results in true, the right operand is not evaluated.
    /// When overloaded, this expression corresponds to calls to op_True and op_BitwiseOr.
    /// </summary>
    /// <param name="leftOperand">The left operand.</param>
    /// <param name="rightOperand">The right operand.</param>
    /// <param name="sourceLocation">The source location of the operation.</param>
    public LogicalOr(Expression leftOperand, Expression rightOperand, ISourceLocation sourceLocation)
      : base(leftOperand, rightOperand, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected LogicalOr(BlockStatement containingBlock, LogicalOr template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Calls the visitor.Visit(LogicalOr) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Returns the string used to identify the operator in error messages
    /// </summary>
    protected override string OperationSymbolForErrorMessage {
      get { return "||"; }
    }

    /// <summary>
    /// Returns the name a method must have to overload this operator.
    /// </summary>
    protected override IName GetOperatorName() {
      return this.NameTable.OpBitwiseOr;
    }

    /// <summary>
    /// Computes the compile time value of the expression. Can be null.
    /// </summary>
    protected override object/*?*/ GetValue() {
      object/*?*/ left = this.ConvertedLeftOperand.Value;
      if (left is bool) {
        if ((bool)left) return true;
        object/*?*/ right = this.ConvertedRightOperand.Value;
        if (right is bool) return (bool)right;
      }
      return null;
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new LogicalOr(containingBlock, this);
    }

    /// <summary>
    /// Returns the projection of (bool)this.LeftOperand ? true : (bool)this.RightOperand;
    /// </summary>
    /// <returns></returns>
    protected override IExpression ProjectAsStandardLogicalOperator() {
      Expression x = this.Helper.ImplicitConversion(this.LeftOperand, this.PlatformType.SystemBoolean.ResolvedType);
      Expression y = this.Helper.ImplicitConversion(this.RightOperand, this.PlatformType.SystemBoolean.ResolvedType);
      Expression result = new Conditional(x, new CompileTimeConstant(true, SourceDummy.SourceLocation), y, this.SourceLocation);
      result.SetContainingExpression(this);
      return result.ProjectAsIExpression();
    }

    /// <summary>
    /// Returns the projection of let tx = (T)x in T.op_True(tx) ? tx : T.op_BitwiseOr(tx, (T)y).
    /// </summary>
    /// <param name="overloadMethodCall">The user defined overload of the bitwise or operator.</param>
    protected override IExpression ProjectAsUserDefinedOverloadAsLogicalOperator(IMethodCall overloadMethodCall) {
      ITypeDefinition T = overloadMethodCall.MethodToCall.ResolvedMethod.Type.ResolvedType;

      //let tx = (T)x
      List<Statement> statements = new List<Statement>(1);
      Expression txe = this.Helper.ImplicitConversion(this.LeftOperand, T);
      ILocalDefinition txVar = Expression.CreateInitializedLocalDeclarationAndAddDeclarationsStatementToList(txe, statements);
      BoundExpression tx = new BoundExpression(txe, txVar);
      BlockStatement block = new BlockStatement(statements, this.SourceLocation);

      //T.op_True(tx)
      IsTrue convertedLeftOperandIsFalse = new IsTrue(tx);

      //T.op_BitwiseOr(tx, y)
      List<Expression> args = new List<Expression>(2);
      args.Add(tx);
      args.Add(this.Helper.ImplicitConversion(this.RightOperand, T));
      ResolvedMethodCall bitWiseOr = new ResolvedMethodCall(overloadMethodCall.MethodToCall.ResolvedMethod, args, this.SourceLocation);

      Conditional conditional = new Conditional(convertedLeftOperandIsFalse, tx, bitWiseOr, this.SourceLocation);
      BlockExpression be = new BlockExpression(block, conditional, this.SourceLocation);
      be.SetContainingExpression(this);
      return be.ProjectAsIExpression();
    }

  }

  /// <summary>
  /// An expression that creates a typed reference (a pair consisting of a reference to a runtime value and a compile time type).
  /// This is similar to what happens when a value type is boxed, except that the boxed value can be an object and
  /// the runtime type of the boxed value can be a subtype of the compile time type that is associated with the boxed valued.
  /// </summary>
  public class MakeTypedReference : Expression, IMakeTypedReference {

    /// <summary>
    /// Allocates an expression that creates a typed reference (a pair consisting of a reference to a runtime value and a compile time type).
    /// This is similar to what happens when a value type is boxed, except that the boxed value can be an object and
    /// the runtime type of the boxed value can be a subtype of the compile time type that is associated with the boxed valued.
    /// </summary>
    /// <param name="operand">The value to box in a typed reference.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public MakeTypedReference(Expression operand, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.operand = operand;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected MakeTypedReference(BlockStatement containingBlock, MakeTypedReference template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.operand = template.operand.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// Calls the visitor.Visit(IMakeTypedReference) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(MakeTypedReference) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (this.ContainingBlock == containingBlock) return this;
      return new MakeTypedReference(containingBlock, this);
    }

    /// <summary>
    /// The value to box in a typed reference.
    /// </summary>
    public Expression Operand {
      get { return this.operand; }
    }
    readonly Expression operand;

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      return this;
    }

    /// <summary>
    /// The type of value that the expression will evaluate to, as determined at compile time.
    /// </summary>
    public override ITypeDefinition Type {
      get { return this.PlatformType.SystemTypedReference.ResolvedType; }
    }

    #region IMakeTypedReference Members

    IExpression IMakeTypedReference.Operand {
      get { return this.Operand.ProjectAsIExpression(); }
    }

    #endregion

    #region IExpression Members

    ITypeReference IExpression.Type {
      get { return this.Type; }
    }

    #endregion
  }

  /// <summary>
  /// Sing#
  /// </summary>
  public class ManagedPointerTypeExpression : TypeExpression {

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedPointerTypeExpression"/> class.
    /// </summary>
    /// <param name="targetType">Type of the target.</param>
    /// <param name="sourceLocation">The source location.</param>
    public ManagedPointerTypeExpression(TypeExpression targetType, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.targetType = targetType;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected ManagedPointerTypeExpression(BlockStatement containingBlock, ManagedPointerTypeExpression template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.targetType = (TypeExpression)template.targetType.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// Calls the visitor.Visit(ManagedPointerTypeExpression) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Gets the type of the target.
    /// </summary>
    /// <value>The type of the target.</value>
    public TypeExpression TargetType {
      get { return this.targetType; }
    }
    readonly TypeExpression targetType;

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new ManagedPointerTypeExpression(containingBlock, this);
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      //^ assume false;
      return new DummyExpression(this.SourceLocation);
    }

    /// <summary>
    /// The type denoted by the expression. If expression cannot be resolved, a dummy type is returned. If the expression is ambiguous the first matching type is returned.
    /// If the expression does not resolve to exactly one type, an error is added to the error collection of the compilation context.
    /// </summary>
    protected override ITypeDefinition Resolve() {
      return ManagedPointerType.GetManagedPointerType(this.TargetType.ResolvedType, this.Compilation.HostEnvironment.InternFactory);
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingExpression(Expression containingExpression) {
      base.SetContainingExpression(containingExpression);
      this.targetType.SetContainingExpression(this);
    }

  }

  /// <summary>
  /// An expression that invokes a method.
  /// </summary>
  public class MethodCall : ConstructorIndexerOrMethodCall, IMethodCall {

    /// <summary>
    /// Allocates an expression that invokes a method.
    /// </summary>
    /// <param name="methodExpression">An expression that, if correct, results in a delegate or method group.</param>
    /// <param name="originalArguments">Expressions that result in the arguments to be passed to the called method.</param>
    /// <param name="sourceLocation">The source location of the call expression.</param>
    public MethodCall(Expression methodExpression, IEnumerable<Expression> originalArguments, ISourceLocation sourceLocation)
      : base(originalArguments, sourceLocation) {
      this.methodExpression = methodExpression;
    }

    /// <summary>
    /// Initializes an expression that invokes a method that has already been resolved.
    /// </summary>
    /// <param name="methodExpression">The expression that was resolved as resolvedMethod.</param>
    /// <param name="originalArguments">Expressions that result in the arguments to be passed to the called method.</param>
    /// <param name="sourceLocation">The source location of the call expression.</param>
    /// <param name="resolvedMethod">The method to call.</param>
    public MethodCall(Expression methodExpression, IEnumerable<Expression> originalArguments, ISourceLocation sourceLocation, IMethodDefinition resolvedMethod)
      : base(originalArguments, sourceLocation) {
      this.methodExpression = methodExpression;
      this.resolvedMethod = resolvedMethod;
    }

    /// <summary>
    /// Initializes an expression that respresents a call to a method that has already been resolved by the time this expression is constructed.
    /// Only for use during projection. I.e. do not contruct this kind of expression from a parser.
    /// </summary>
    /// <param name="resolvedMethod">The resolved method to call. This method must be static.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    protected MethodCall(ISourceLocation sourceLocation, IMethodDefinition resolvedMethod)
      : base(new List<Expression>(0).AsReadOnly(), sourceLocation) {
      this.methodExpression = new DummyExpression(sourceLocation);
      this.resolvedMethod = resolvedMethod;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected MethodCall(BlockStatement containingBlock, MethodCall template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.methodExpression = template.methodExpression.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// Calls this.ProjectAsIExpression(visitor) unless the project is this expression, in which case it call visitor.Visit(IMethodCall).
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      IExpression expr = this.ProjectAsIExpression();
      if (expr == this)
        visitor.Visit(this);
      else
        expr.Dispatch(visitor);
    }

    /// <summary>
    /// Calls the visitor.Visit(MethodCall) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// A reference to this.ResolvedMethod. If the method call has extra arguments, the reference is not the method itself, but
    /// a distinct object that includes references to the types of the extra arguments.
    /// </summary>
    protected virtual IMethodReference GetReferenceToMethodToCall() {
      IMethodDefinition method = this.ResolvedMethod;
      if (method is Dummy) return Dummy.MethodReference;
      if (!method.AcceptsExtraArguments) return method;
      ushort nPars = (ushort)IteratorHelper.EnumerableCount(method.Parameters);
      ushort nArgs = (ushort)IteratorHelper.EnumerableCount(this.ConvertedArguments);
      if (nArgs <= nPars) return method;
      IEnumerator<Expression> args = this.ConvertedArguments.GetEnumerator();
      List<ITypeReference> parameterTypesForExtraArguments = new List<ITypeReference>();
      ushort i = 0;
      while (i < nPars) {
        i++; args.MoveNext();
      }
      while (i < nArgs) {
        args.MoveNext();
        parameterTypesForExtraArguments.Add(args.Current.Type);
        i++;
      }
      return new MethodReference(this.ContainingBlock.Compilation.HostEnvironment, this.ResolvedMethod.ContainingType,
        this.ResolvedMethod.CallingConvention, this.ResolvedMethod.Type,
        this.ResolvedMethod.Name, method.GenericParameterCount, ((IMethodReference)method).Parameters, parameterTypesForExtraArguments.ToArray());
    }

    /// <summary>
    /// Returns a collection of methods that match this.MethodExpression.
    /// </summary>
    /// <param name="allowMethodParameterInferencesToFail">If this flag is true, 
    /// generic methods should be included in the collection if their method parameter types could not be inferred from the argument types.</param>
    public override IEnumerable<IMethodDefinition> GetCandidateMethods(bool allowMethodParameterInferencesToFail) {
      ITypeDefinition methodExpressionType = this.MethodExpression.Type;

      //If this.MethodExpression binds to a delegate, return the Invoke method
      if (methodExpressionType.IsDelegate)
        return this.GetInvokeMethod(methodExpressionType);

      if (methodExpression.Type is IFunctionPointerTypeReference)
        return IteratorHelper.GetSingletonEnumerable<IMethodDefinition>(new FunctionPointerMethod((IFunctionPointerTypeReference)methodExpression.Type));

      //If this.MethodExpression binds to a method group or a group of generic method instances, return all methods in the group
      if (methodExpressionType is Dummy) {
        //Check if expression binds to a method group
        IMethodDefinition/*?*/ methodGroupRepresentative = this.ResolveMethodExpression(this.MethodExpression) as IMethodDefinition;
        if (methodGroupRepresentative != null) {
          IEnumerable<IMethodDefinition> result = this.GetInstantiatedMethodGroupMethods(methodGroupRepresentative, allowMethodParameterInferencesToFail);
          return result;
        }
        //Check if expression binds to a group of generic methods
        GenericInstanceExpression/*?*/ genericInstance = this.MethodExpression as GenericInstanceExpression;
        if (genericInstance != null) {
          methodGroupRepresentative = this.ResolveMethodExpression(genericInstance.GenericTypeOrMethod) as IMethodDefinition;
          if (methodGroupRepresentative != null) return this.GetInstantiatedMethodGroupMethods(methodGroupRepresentative, genericInstance.ArgumentTypes);
          return MethodDefinition.EmptyCollection;
        }

        return MethodDefinition.EmptyCollection;
      }

      return MethodDefinition.EmptyCollection;
    }

    /// <summary>
    /// Find applicable extension methods for this call, if possible.
    /// Precondition: this.MethodExpression must be a QualifiedName.
    /// </summary>
    /// <returns></returns>
    public virtual IEnumerable<IMethodDefinition> GetCandidateExtensionMethods(IEnumerable<Expression> arguments) {
      // Binding extension methods requires a special traversal of the namespaces. Very C# specific.
      List<IMethodDefinition> result = new List<IMethodDefinition>();
      NamespaceDeclaration enclosingNamespace = this.ContainingBlock.ContainingNamespaceDeclaration;
      SimpleName simpleName = ((QualifiedName)this.MethodExpression).SimpleName;
      enclosingNamespace.GetApplicableExtensionMethods(result, simpleName, arguments);
      if (result.Count != 0)
        return result;
      else
        return MethodDefinition.EmptyCollection;
    }

    //internal IEnumerable<Expression> MakeExtensionArgumentList(QualifiedName callExpression)  { ...}
    //    This method replaced by a static method in LanguageSpecificCompilationHelper

    /// <summary>
    /// For each method in the group of methods defined by the given representative method, try to infer the type arguments
    /// from the types of the actual arguments of this call.
    /// Returns the intantiated versions of all methods for which type inference succeeds.
    /// </summary>
    private IEnumerable<IMethodDefinition> GetInstantiatedMethodGroupMethods(IMethodDefinition methodGroupRepresentative, bool allowMethodParameterInferencesToFail) {
      foreach (IMethodDefinition method in this.GetMethodGroupMethods(methodGroupRepresentative)) {
        if (method.IsGeneric) {
          IMethodDefinition instantiatedMethod = this.GetInstantiatedMethodAfterInferringGenericArguments(method, allowMethodParameterInferencesToFail);
          if (instantiatedMethod is Dummy) continue; //Type inference did not succeed, so method must be ignored
          yield return instantiatedMethod;
        } else
          yield return method;
      }
    }

    /// <summary>
    /// Try to infer the type arguments of method from the types of the actual arguments of this call.
    /// If the inference succeeds, return an instantiation of method with the inferred type arguments, otherwise return Dummy.Method.
    /// </summary>
    private IMethodDefinition GetInstantiatedMethodAfterInferringGenericArguments(IMethodDefinition method, bool allowMethodParameterInferencesToFail)
      //^ requires method.IsGeneric;
    {
      Dictionary<IGenericMethodParameter, ITypeDefinition> inferredTypeArgumentFor = new Dictionary<IGenericMethodParameter, ITypeDefinition>();
      List<Expression> argumentsToInferIteratively = new List<Expression>();
      bool argumentListContainsLambdasOrAnonymousMethods = false;
      Expression dummyExpression = new DummyExpression(SourceDummy.SourceLocation);

      //Start by inferring type arguments while ignoring lambdas and anonymous methods in the argument list
      IEnumerator<IParameterDefinition> parameterEnumerator = method.Parameters.GetEnumerator();
      IEnumerator<Expression> argumentEnumerator = this.OriginalArguments.GetEnumerator();
      while (argumentEnumerator.MoveNext() && parameterEnumerator.MoveNext()) {
        Expression argument = argumentEnumerator.Current;
        IParameterDefinition parameter = parameterEnumerator.Current;
        if (argument is Lambda || argument is AnonymousMethod) {
          argumentListContainsLambdasOrAnonymousMethods = true;
          argumentsToInferIteratively.Add(argument);
        } else {
          if (this.InferTypesAndReturnTrueIfUnificationFails(inferredTypeArgumentFor, argument, parameter.Type.ResolvedType)) {
            if (parameter.IsParameterArray) {
              ITypeDefinition paramArrayElementType = parameter.ParamArrayElementType.ResolvedType;
              do {
                if (this.InferTypesAndReturnTrueIfUnificationFails(inferredTypeArgumentFor, argumentEnumerator.Current, paramArrayElementType) && !allowMethodParameterInferencesToFail)
                  return Dummy.Method; //It is not possible to infer the types of this method from the argument types
              } while (argumentEnumerator.MoveNext());
            } else if (!allowMethodParameterInferencesToFail) {
              return Dummy.Method; //It is not possible to infer the types of this method from the argument types
            }
          }
          argumentsToInferIteratively.Add(dummyExpression);
        }
      }
      if (parameterEnumerator.MoveNext()) {
        //More parameters than arguments (might get here in the future when resolving overloads for parameter help during editing).
        if (!parameterEnumerator.Current.IsParameterArray) return Dummy.Method;
      } else if (argumentEnumerator.MoveNext()) {
        //More arguments than parameters and last parameter is not a parameter array (might get here in the future when resolving overloads for parameter help during editing).
        return Dummy.Method;
      }

      if (allowMethodParameterInferencesToFail) return method;

      //Now infer iteratively, looking only at lambdas and anonymous methods
      int numberOfInferredTypeArguments = inferredTypeArgumentFor.Count;
      while (numberOfInferredTypeArguments < method.GenericParameterCount && argumentListContainsLambdasOrAnonymousMethods) {
        argumentListContainsLambdasOrAnonymousMethods = false;
        parameterEnumerator = method.Parameters.GetEnumerator();
        int argumentIndex = 0;
        while (argumentIndex < argumentsToInferIteratively.Count && parameterEnumerator.MoveNext()) {
          Expression argument = argumentsToInferIteratively[argumentIndex++];
          if (argument == dummyExpression) continue;
          IParameterDefinition parameter = parameterEnumerator.Current;
          if (this.InferTypesAndReturnTrueIfUnificationFails(inferredTypeArgumentFor, argument, parameter.Type.ResolvedType)) {
            if (parameter.IsParameterArray) {
              ITypeDefinition paramArrayElemenType = parameter.ParamArrayElementType.ResolvedType;
              argumentIndex--;
              do {
                if (this.InferTypesAndReturnTrueIfUnificationFails(inferredTypeArgumentFor, argumentsToInferIteratively[argumentIndex++], paramArrayElemenType)) {
                  //With the current state of inferredTypeArgumentFor it is not possible to unify this lambda/anonymous method with the parameter
                  argumentListContainsLambdasOrAnonymousMethods = true;
                  goto tryNextIteration;
                }
              } while (argumentIndex < argumentsToInferIteratively.Count);
            } else {
              //With the current state of inferredTypeArgumentFor it is not possible to unify this lambda/anonymous method with the parameter
              argumentListContainsLambdasOrAnonymousMethods = true;
              goto tryNextArgument;
            }
          }
          argumentsToInferIteratively[argumentIndex - 1] = dummyExpression;
        tryNextArgument: ;
        }
      tryNextIteration:
        int newNumberOfInferredTypeArguments = inferredTypeArgumentFor.Count;
        if (newNumberOfInferredTypeArguments <= numberOfInferredTypeArguments) return Dummy.Method;
      }
      if (numberOfInferredTypeArguments < method.GenericParameterCount) return Dummy.Method;

      //Type argument inference succeeded for all type parameters. Contruct an instance of the generic method, using the inferred type arguments.
      List<ITypeReference> genericArguments = new List<ITypeReference>((int)method.GenericParameterCount);
      //^ assume method.IsGeneric; //precondition
      foreach (IGenericMethodParameter typeParameter in method.GenericParameters) {
        ITypeDefinition/*?*/ inferredArgumentType;
        if (!inferredTypeArgumentFor.TryGetValue(typeParameter, out inferredArgumentType)) {
          //^ assume false; //inferredTypeArguments should contain only keys that are generic type parameters of method. If we get here the number of keys equals the number of parameters.
          return Dummy.Method;
        }
        //^ assume inferredArgumentType != null;
        genericArguments.Add(inferredArgumentType);
      }
      //^ assume method.ResolvedMethod.IsGeneric;
      //^ assume false;
      return new GenericMethodInstance(method, genericArguments.AsReadOnly(), this.Compilation.HostEnvironment.InternFactory);
    }

    /// <summary>
    /// Try to unify the type of the given expression with the given parameter type by replacing any occurrences of type parameters in parameterType with corresponding type
    /// arguments obtained from inferredTypeArgumentsFor. Returns true if unification fails. Updates inferredTypeArgumentsFor with any new inferences made during
    /// a successful unification.
    /// </summary>
    //^ [Confined]
    private bool InferTypesAndReturnTrueIfUnificationFails(Dictionary<IGenericMethodParameter, ITypeDefinition> inferredTypeArgumentFor, Expression expression, ITypeDefinition parameterType) {
      if (parameterType is INamespaceTypeDefinition || parameterType is INestedTypeDefinition || parameterType is IGenericTypeParameter)
        return false; //parameterType does not involve any method type parameters 

      ICompileTimeConstant/*?*/ constant = expression as ICompileTimeConstant;
      if (constant != null && constant.Value == null)
        return false; //The argument has the null type

      if (this.ResolveMethodExpression(expression) is IMethodDefinition)
        return false; //The argument is a method group

      AnonymousMethod/*?*/ anonymousMethod = expression as AnonymousMethod;
      if (anonymousMethod != null) return this.InferTypesAndReturnTrueIfUnificationFails(inferredTypeArgumentFor, anonymousMethod, parameterType);

      Lambda/*?*/ lambda = expression as Lambda;
      if (lambda != null) return this.InferTypesAndReturnTrueIfUnficationFails(inferredTypeArgumentFor, lambda, parameterType);

      ITypeDefinition expressionType = expression.InferType();
      return this.Helper.InferTypesAndReturnTrueIfInferenceFails(inferredTypeArgumentFor, expressionType, parameterType);
    }

    /// <summary>
    /// Try to unify the signature of the given anonymous method with the given parameter type by replacing any occurrences of type parameters in parameterType with corresponding type
    /// arguments obtained from inferredTypeArgumentsFor. Returns true if unification fails. Updates inferredTypeArgumentsFor with any new inferences made during
    /// a successful unification.
    /// </summary>
    //^ [Confined]
    private bool InferTypesAndReturnTrueIfUnificationFails(Dictionary<IGenericMethodParameter, ITypeDefinition> inferredTypeArgumentFor, AnonymousMethod anonymousMethod, ITypeDefinition parameterType) {
      if (!parameterType.IsDelegate) return false;
      //TODO: if parameter types have been specified, unify them with parameters from invoke method
      //TODO: infer return type of anonymous method. Unify it with return type of delegate.
      //TODO: use inferredTypeArgumentFor to infer parameter types of delegate
      if (inferredTypeArgumentFor == null || anonymousMethod == null) return false; //dummy to shut up FxCop
      return false;
    }

    /// <summary>
    /// Try to unify the signature of the given lambda with the given parameter type by replacing any occurrences of type parameters in parameterType with corresponding type
    /// arguments obtained from inferredTypeArgumentsFor. Returns true if unification fails. Updates inferredTypeArgumentsFor with any new inferences made during
    /// a successful unification.
    /// </summary>
    private bool InferTypesAndReturnTrueIfUnficationFails(Dictionary<IGenericMethodParameter, ITypeDefinition> inferredTypeArgumentFor, Lambda lambda, ITypeDefinition parameterType) {
      if (!parameterType.IsDelegate) return false;
      //TODO: use inferredTypeArgumentFor to infer parameter types of delegate
      //TODO: infer return type of lambda. Unify it with return type of delegate.
      if (inferredTypeArgumentFor == null || lambda == null) return false; //dummy to shut up FxCop
      return false;
    }

    /// <summary>
    /// For each generic method with the right number of type parameters in the group of methods defined by the given representative method return an
    /// instance of the method (using the given type arguments to make the instantiation).
    /// </summary>
    private IEnumerable<IMethodDefinition> GetInstantiatedMethodGroupMethods(IMethodDefinition methodGroupRepresentative, IEnumerable<TypeExpression> typeArguments) {
      List<ITypeReference> genericArgumentList = new List<ITypeReference>();
      foreach (TypeExpression typeArg in typeArguments) genericArgumentList.Add(typeArg.ResolvedType);
      genericArgumentList.TrimExcess();
      IEnumerable<ITypeReference> genericArguments = genericArgumentList.AsReadOnly();
      int numberOfTypeArguments = genericArgumentList.Count;
      foreach (IMethodDefinition method in this.GetMethodGroupMethods(methodGroupRepresentative)) {
        if (method.GenericParameterCount != numberOfTypeArguments) continue;
        yield return new GenericMethodInstance(method, genericArguments, this.Compilation.HostEnvironment.InternFactory);
      }
    }

    /// <summary>
    /// Returns the collection of methods that overload the given method and might correpond to this call (based on their parameter counts). Includes inherited methods.
    /// </summary>
    private IEnumerable<IMethodDefinition> GetMethodGroupMethods(IMethodDefinition methodGroupRepresentative) {
      uint argumentCount = 0;
      bool argumentListIsIncomplete = false;
      foreach (var argument in this.OriginalArguments) {
        argumentCount++;
        argumentListIsIncomplete = argument is DummyExpression;
      }
      if (argumentListIsIncomplete) argumentCount--;
      return this.Helper.GetMethodGroupMethods(methodGroupRepresentative, argumentCount, argumentListIsIncomplete);
      //TODO: pass in "this", so that visibility can be checked.
    }

    /// <summary>
    /// Gets the Invoke method from the delegate. Returns an empty collection if the delegate type is malformed.
    /// </summary>
    private IEnumerable<IMethodDefinition> GetInvokeMethod(ITypeDefinition methodExpressionType)
      //^ requires methodExpressionType.IsDelegate;
    {
      IMethodDefinition invokeMethod = this.Helper.GetInvokeMethod(methodExpressionType);
      if (invokeMethod is Dummy) return Enumerable<IMethodDefinition>.Empty; //Will get here only when referencing a malformed/malicious assembly. 
      return IteratorHelper.GetSingletonEnumerable<IMethodDefinition>(invokeMethod);
    }

    /// <summary>
    /// True if this method call terminates the calling method and reuses the arguments of the calling method as the arguments of the called method.
    /// </summary>
    public bool IsJumpCall {
      get { return false; }
    }

    /// <summary>
    /// True if the method to call is static (has no this parameter).
    /// </summary>
    public bool IsStaticCall {
      get { return this.ResolvedMethod.IsStatic; }
    }

    /// <summary>
    /// True if this method call terminates the calling method. It indicates that the calling method's stack frame is not required
    /// and can be removed before executing the call.
    /// </summary>
    public virtual bool IsTailCall {
      get { return false; }
    }

    /// <summary>
    /// True if the method to call is determined at run time, based on the runtime type of ThisArgument.
    /// </summary>
    public override bool IsVirtualCall {
      get {
        IMethodDefinition methodToCall = this.ResolvedMethod;
        if (!methodToCall.IsVirtual) return false;
        bool result = !(this.ThisArgument is BaseClassReference) && !(methodToCall.ContainingTypeDefinition.IsStruct);
        //^ assume methodToCall.IsVirtual;
        //^ assume methodToCall.ResolvedMethod == methodToCall;
        return result;
      }
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new MethodCall(containingBlock, this);
    }

    /// <summary>
    /// An expression that, if correct, results in a delegate or method group.
    /// </summary>
    public Expression MethodExpression {
      get { return this.methodExpression; }
    }
    readonly Expression methodExpression;

    /// <summary>
    /// A reference to the method to call. If the method accepts extra arguments and this call supplies extra arguments,
    /// the reference will contain the types of the extra arguments.
    /// </summary>
    public IMethodReference MethodToCall {
      get {
        if (this.methodToCall == null)
          this.methodToCall = this.GetReferenceToMethodToCall();
        return this.methodToCall;
      }
    }
    IMethodReference/*?*/ methodToCall;

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      FunctionPointerMethod/*?*/ fpMethod = this.ResolvedMethod as FunctionPointerMethod;
      if (fpMethod != null)
        return new PointerCall(((IMethodCall)this).Arguments, this.MethodExpression.ProjectAsIExpression(), this.SourceLocation);
      IArrayTypeReference/*?*/ atr = this.ResolvedMethod.ContainingType as IArrayTypeReference;
      if (atr != null && atr.IsVector) {
        int key = this.ResolvedMethod.Name.UniqueKey;
        if (key == this.NameTable.Length.UniqueKey) {
          VectorLength vecLen = new VectorLength(this.ThisArgument, this.SourceLocation);
          return this.Helper.ExplicitConversion(vecLen, this.PlatformType.SystemInt32.ResolvedType).ProjectAsIExpression();
        } else if (key == this.NameTable.LongLength.UniqueKey) {
          VectorLength vecLen = new VectorLength(this.ThisArgument, this.SourceLocation);
          return this.Helper.ExplicitConversion(vecLen, this.PlatformType.SystemInt64.ResolvedType).ProjectAsIExpression();
        }
      }
      return this;
    }

    /// <summary>
    /// Resolves the given method expression and returns the result.
    /// Always returns null if the method expression is not a simple name or a qualified name.
    /// </summary>
    protected virtual object/*?*/ ResolveMethodExpression(Expression methodExpression) {
      SimpleName/*?*/ simpleName = methodExpression as SimpleName;
      if (simpleName != null) return simpleName.Resolve();
      QualifiedName/*?*/ qualifiedName = methodExpression as QualifiedName;
      if (qualifiedName != null) return qualifiedName.Resolve(false);
      return null;
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingExpression(Expression containingExpression) {
      base.SetContainingExpression(containingExpression);
      this.methodExpression.SetContainingExpression(this);
    }

    /// <summary>
    /// The this argument of the method to call.
    /// </summary>
    public virtual Expression ThisArgument {
      get
        //^ requires !this.ResolvedMethod.IsStatic;
      {
        if (this.thisArgument == null)
          this.thisArgument = this.GetThisArgument();
        return this.thisArgument;
      }
    }
    Expression/*?*/ thisArgument;

    private Expression GetThisArgument() {
      Expression result;
      if (this.MethodExpression.Type.IsDelegate)
        result = this.MethodExpression;
      else {
        QualifiedName/*?*/ qualName = this.MethodExpression as QualifiedName;
        if (qualName == null) {
          result = new ThisReference(this.MethodExpression.SourceLocation);
          result.SetContainingExpression(this);
        } else
          result = qualName.Qualifier;
      }
      return this.GetAsReferenceIfValueType(result);
    }

    /// <summary>
    /// If the type of the given expression is a value type, return an expression that results in a reference to the value that expression results in.
    /// </summary>
    protected Expression GetAsReferenceIfValueType(Expression expression) {
      if (expression.Type.IsReferenceType) return expression;
      if (expression is ThisReference || expression is BaseClassReference) return expression;
      if (this.IsStoredInWritableMemory(expression))
        expression = new AddressOf(new AddressableExpression(expression), expression.SourceLocation);
      else if (!TypeHelper.TypesAreEquivalent(this.ResolvedMethod.ContainingTypeDefinition, expression.Type))
        //The type will be System.Object, System.ValueType or System.Enum. Hence boxing is required.
        expression = new Conversion(expression, this.PlatformType.SystemObject.ResolvedType, expression.SourceLocation);
      else {
        var statements = new List<Statement>(1);
        var block = new BlockStatement(statements, this.SourceLocation);
        var temp = Expression.CreateInitializedLocalDeclarationAndAddDeclarationsStatementToList(expression, statements);
        var addressOf = new AddressOf(new AddressableExpression(new BoundExpression(expression, temp)), expression.SourceLocation);
        var be = new BlockExpression(block, addressOf, this.SourceLocation);
        be.SetContainingExpression(this);
        expression = be;
      }
      expression.SetContainingExpression(this);
      return expression;
    }

    /// <summary>
    /// Returns true if the expression binds to a writeable memory location, such as a local, a parameter, a field
    /// that is not readonly, or an array element.
    /// </summary>
    private bool IsStoredInWritableMemory(Expression expression) {
      var projectedExpression = expression.ProjectAsIExpression();
      if (projectedExpression is IArrayIndexer) return true;
      var boundExpression = projectedExpression as IBoundExpression;
      if (boundExpression == null) return false;
      var field = boundExpression.Definition as IFieldReference;
      if (field == null) return true;
      return !field.ResolvedField.IsReadOnly;
    }

    /// <summary>
    /// Called when the arguments are good and no type inferences have failed. This means that the callee could not be found. Complain.
    /// </summary>
    protected override void ComplainAboutCallee() {
      List<IMethodDefinition> candidates = new List<IMethodDefinition>(this.GetCandidateMethods(true));
      if (candidates.Count > 1) {
        string cand0 = this.Helper.GetMethodSignature(candidates[0], NameFormattingOptions.Signature|NameFormattingOptions.ParameterModifiers);
        string cand1 = this.Helper.GetMethodSignature(candidates[1], NameFormattingOptions.Signature|NameFormattingOptions.ParameterModifiers);
        this.Helper.ReportError(new AstErrorMessage(this, Error.AmbiguousCall, cand0, cand1));
        return;
      }
      object/*?*/ resolvedMethodExpression = this.ResolveMethodExpression(this.MethodExpression);
      IMethodDefinition/*?*/ methodGroupRepresentative = resolvedMethodExpression as IMethodDefinition;
      if (methodGroupRepresentative != null && !(methodGroupRepresentative is Dummy)) {
        string/*?*/ numberOfArguments = IteratorHelper.EnumerableCount(this.OriginalArguments).ToString();
        //^ assume numberOfArguments != null;
        this.Helper.ReportError(new AstErrorMessage(this, Error.BadNumberOfArguments, this.Helper.GetMethodSignature(methodGroupRepresentative, NameFormattingOptions.None), numberOfArguments));
        return;
      }
      if (candidates.Count == 0) {
        if (!this.MethodExpression.HasErrors) {
          //this.MethodExpression binds to something that is not callable.
          object/*?*/ badSymbol = this.ResolveMethodExpression(this.MethodExpression);
          string/*?*/ symbolKind = null;
          //Review should "field" and "method" be localized?
          if (badSymbol is IFieldDefinition) symbolKind = "field";
          else if (badSymbol is IParameterDefinition) symbolKind = "parameter";
          else if (badSymbol is ILocalDefinition) symbolKind = "local";
          else if (badSymbol is IEventDefinition) symbolKind = "event";
          else if (badSymbol is IPropertyDefinition) symbolKind = "property";
          if (symbolKind != null) {
            this.Helper.ReportError(new AstErrorMessage(this.MethodExpression, Error.BadUseOfSymbol, this.MethodExpression.SourceLocation.Source, symbolKind, "method"));
          } else {
            this.Helper.ReportError(new AstErrorMessage(this.MethodExpression, Error.CannotCallNonMethod, this.MethodExpression.SourceLocation.Source));
          }
        }
      }
    }

    #region IMethodCall Members

    IEnumerable<IExpression> IMethodCall.Arguments {
      get {
        foreach (Expression convertedArgument in this.ConvertedArguments)
          yield return convertedArgument.ProjectAsIExpression();
      }
    }

    IExpression IMethodCall.ThisArgument {
      get {
        //^ assume this.ResolvedMethod == ((IMethodCall)this).MethodToCall.ResolvedMethod;
        return this.ThisArgument.ProjectAsIExpression();
      }
    }

    #endregion

    #region IExpression Members

    ITypeReference IExpression.Type {
      get { return this.Type; }
    }

    #endregion
  }

  /// <summary>
  /// A group of methods. In VB the AddressOf operator corresponds to this expression.
  /// </summary>
  public class MethodGroup : Expression {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="selector"></param>
    /// <param name="sourceLocation"></param>
    public MethodGroup(Expression selector, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.selector = selector;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected MethodGroup(BlockStatement containingBlock, MethodGroup template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.selector = template.selector.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// Calls the visitor.Visit(MethodGroup) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new MethodGroup(containingBlock, this);
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      //^ assume false; //It is a mistake to include a method group expression in a context where this method can get called.
      return new DummyExpression(this.SourceLocation);
    }

    /// <summary>
    /// An expression that selects one or more methods. For example, this can be a SimpleName or a QualifiedName.
    /// </summary>
    public Expression Selector {
      get { return this.selector; }
    }
    readonly Expression selector;

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingExpression(Expression containingExpression) {
      base.SetContainingExpression(containingExpression);
      this.selector.SetContainingExpression(this);
    }

    /// <summary>
    /// The type of value that the expression will evaluate to, as determined at compile time.
    /// </summary>
    public override ITypeDefinition Type {
      get { return Dummy.Type; }
    }

  }

  /// <summary>
  /// An expression that results in the remainder of dividing value the left operand by the value of the right operand. 
  /// When the operator is overloaded, this expression corresponds to a call to op_Modulus.
  /// </summary>
  public class Modulus : BinaryOperation, IModulus {

    /// <summary>
    /// Allocates an expression that results in the remainder of dividing value the left operand by the value of the right operand. 
    /// When the operator is overloaded, this expression corresponds to a call to op_Modulus.
    /// </summary>
    /// <param name="leftOperand">The left operand.</param>
    /// <param name="rightOperand">The right operand.</param>
    /// <param name="sourceLocation">The source location of the operation.</param>
    public Modulus(Expression leftOperand, Expression rightOperand, ISourceLocation sourceLocation)
      : base(leftOperand, rightOperand, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected Modulus(BlockStatement containingBlock, Modulus template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Calls the visitor.Visit(IModulus) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(Modulus) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Returns the string used to identify the operator in error messages
    /// </summary>
    protected override string OperationSymbolForErrorMessage {
      get { return "%"; }
    }


    /// <summary>
    /// Returns the name a method must have to overload this operator.
    /// </summary>
    protected override IName GetOperatorName() {
      return this.NameTable.OpModulus;
    }

    /// <summary>
    /// Computes the compile time value of the expression. Can be null.
    /// </summary>
    protected override object/*?*/ GetValue() {
      object/*?*/ left = this.ConvertedLeftOperand.Value;
      object/*?*/ right = this.ConvertedRightOperand.Value;
      if (left == null || right == null) return null;
      switch (System.Convert.GetTypeCode(left)) {
        case TypeCode.Int32:
          //^ assume left is int && right is int;
          int ri = (int)right;
          if (ri == 0) return null;
          return (int)left % ri;
        case TypeCode.UInt32:
          //^ assume left is uint && right is uint;
          uint rui = (uint)right;
          if (rui == 0) return null;
          return (uint)left % rui;
        case TypeCode.Int64:
          //^ assume left is long && right is long;
          long rl = (long)right;
          if (rl == 0) return null;
          return (long)left % rl;
        case TypeCode.UInt64:
          //^ assume left is ulong && right is ulong;
          ulong rul = (ulong)right;
          if (rul == 0) return null;
          return (ulong)left % rul;
        case TypeCode.Single:
          //^ assume left is float && right is float;
          return (float)left % (float)right;
        case TypeCode.Double:
          //^ assume left is double && right is double;
          return (double)left % (double)right;
        case TypeCode.Decimal:
          //^ assume left is decimal && right is decimal;
          return (decimal)left % (decimal)right;
      }
      return null;
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new Modulus(containingBlock, this);
    }

    /// <summary>
    /// A list of dummy methods that correspond to operations that are built into IL. The dummy methods are used, via overload resolution,
    /// to determine how the operands are to be converted before the operation is carried out.
    /// </summary>
    protected override IEnumerable<IMethodDefinition> StandardOperators {
      get {
        BuiltinMethods dummyMethods = this.Compilation.BuiltinMethods;
        yield return dummyMethods.Int32opInt32;
        yield return dummyMethods.UInt32opUInt32;
        yield return dummyMethods.Int64opInt64;
        yield return dummyMethods.UInt64opUInt64;
        yield return dummyMethods.Float32opFloat32;
        yield return dummyMethods.Float64opFloat64;
        yield return dummyMethods.DecimalOpDecimal;
      }
    }

    /// <summary>
    /// If true the operands must be integers and are treated as being unsigned for the purpose of the modulus.
    /// </summary>
    public bool TreatOperandsAsUnsignedIntegers {
      get { return TypeHelper.IsUnsignedPrimitiveInteger(this.ConvertedLeftOperand.Type) && TypeHelper.IsUnsignedPrimitiveInteger(this.ConvertedRightOperand.Type); }
    }
  }

  /// <summary>
  /// An expression that results in the remainder of dividing value the left operand by the value of the right operand. 
  /// The result of the expression is assigned to the left operand, which must be a target expression.
  /// When the operator is overloaded, this expression corresponds to a call to op_Modulus.
  /// </summary>
  public class ModulusAssignment : BinaryOperationAssignment {

    /// <summary>
    /// Allocates an expression that results in the remainder of dividing value the left operand by the value of the right operand. 
    /// The result of the expression is assigned to the left operand, which must be a target expression.
    /// When the operator is overloaded, this expression corresponds to a call to op_Modulus.
    /// </summary>
    /// <param name="leftOperand">The left operand and target of the assignment.</param>
    /// <param name="rightOperand">The right operand.</param>
    /// <param name="sourceLocation">The source location of the operation.</param>
    public ModulusAssignment(TargetExpression leftOperand, Expression rightOperand, ISourceLocation sourceLocation)
      : base(leftOperand, rightOperand, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected ModulusAssignment(BlockStatement containingBlock, ModulusAssignment template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Calls the visitor.Visit(ModulusAssignment) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (this.ContainingBlock == containingBlock) return this;
      return new ModulusAssignment(containingBlock, this);
    }

    /// <summary>
    /// Creates a modulus expression with the given left operand and this.RightOperand.
    /// The method does not use this.LeftOperand.Expression, since it may be necessary to factor out any subexpressions so that
    /// they are evaluated only once. The given left operand expression is expected to be the expression that remains after factoring.
    /// </summary>
    /// <param name="leftOperand">An expression to combine with this.RightOperand into a binary expression.</param>
    protected override Expression CreateBinaryExpression(Expression leftOperand) {
      Expression result = new Modulus(leftOperand, this.RightOperand, this.SourceLocation);
      result.SetContainingExpression(this);
      return result;
    }
  }

  /// <summary>
  /// An expression that multiplies the value of the left operand by the value of the right operand. 
  /// When the operator is overloaded, this expression corresponds to a call to op_Multiplication.
  /// </summary>
  public class Multiplication : BinaryOperation, IMultiplication {

    /// <summary>
    /// Allocates an expression that multiplies the value of the left operand by the value of the right operand. 
    /// When the operator is overloaded, this expression corresponds to a call to op_Multiplication.
    /// </summary>
    /// <param name="leftOperand">The left operand.</param>
    /// <param name="rightOperand">The right operand.</param>
    /// <param name="sourceLocation">The source location of the operation.</param>
    public Multiplication(Expression leftOperand, Expression rightOperand, ISourceLocation sourceLocation)
      : base(leftOperand, rightOperand, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected Multiplication(BlockStatement containingBlock, Multiplication template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.flags = template.flags;
    }

    /// <summary>
    /// The multiplication must be performed with a check for arithmetic overflow if the operands are integers.
    /// </summary>
    public virtual bool CheckOverflow {
      get {
        return (this.flags & 1) != 0;
      }
    }

    /// <summary>
    /// Calls the visitor.Visit(IMultiplication) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(Multiplication) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Storage for boolean properties. 1=Use checked arithmetic.
    /// </summary>
    protected int flags;

    /// <summary>
    /// Returns the string used to identify the operator in error messages
    /// </summary>
    protected override string OperationSymbolForErrorMessage {
      get { return "*"; }
    }


    /// <summary>
    /// Returns the name a method must have to overload this operator.
    /// </summary>
    protected override IName GetOperatorName() {
      return this.NameTable.OpMultiply;
    }

    /// <summary>
    /// Computes the compile time value of the expression. Can be null.
    /// </summary>
    protected override object/*?*/ GetValue() {
      object/*?*/ left = this.ConvertedLeftOperand.Value;
      object/*?*/ right = this.ConvertedRightOperand.Value;
      if (left == null || right == null) return null;
      switch (System.Convert.GetTypeCode(left)) {
        case TypeCode.Int32:
          //^ assume left is int && right is int;
          return (int)left * (int)right; //TODO: overflow check
        case TypeCode.UInt32:
          //^ assume left is uint && right is uint;
          return (uint)left * (uint)right; //TODO: overflow check
        case TypeCode.Int64:
          //^ assume left is long && right is long;
          return (long)left * (long)right; //TODO: overflow check
        case TypeCode.UInt64:
          //^ assume left is ulong && right is ulong;
          return (ulong)left * (ulong)right; //TODO: overflow check
        case TypeCode.Single:
          //^ assume left is float && right is float;
          return (float)left * (float)right;
        case TypeCode.Double:
          //^ assume left is double && right is double;
          return (double)left * (double)right;
        case TypeCode.Decimal:
          //^ assume left is decimal && right is decimal;
          return (decimal)left * (decimal)right;
      }
      return null;
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new Multiplication(containingBlock, this);
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingExpression(Expression containingExpression) {
      base.SetContainingExpression(containingExpression);
      if (containingExpression.ContainingBlock.UseCheckedArithmetic)
        this.flags |= 1;
      //Note that checked/unchecked expressions intercept this call and provide a dummy block that has the flag set appropriately.
    }

    /// <summary>
    /// A list of dummy methods that correspond to operations that are built into IL. The dummy methods are used, via overload resolution,
    /// to determine how the operands are to be converted before the operation is carried out.
    /// </summary>
    protected override IEnumerable<IMethodDefinition> StandardOperators {
      get {
        BuiltinMethods dummyMethods = this.Compilation.BuiltinMethods;
        yield return dummyMethods.Int32opInt32;
        yield return dummyMethods.UInt32opUInt32;
        yield return dummyMethods.Int64opInt64;
        yield return dummyMethods.UInt64opUInt64;
        yield return dummyMethods.Float32opFloat32;
        yield return dummyMethods.Float64opFloat64;
        yield return dummyMethods.DecimalOpDecimal;
      }
    }

    /// <summary>
    /// If true the operands must be integers and are treated as being unsigned for the purpose of the multiplication. This only makes a difference if CheckOverflow is true as well.
    /// </summary>
    public virtual bool TreatOperandsAsUnsignedIntegers {
      get { return TypeHelper.IsUnsignedPrimitiveInteger(this.ConvertedLeftOperand.Type) && TypeHelper.IsUnsignedPrimitiveInteger(this.ConvertedRightOperand.Type); }
    }
  }

  /// <summary>
  /// An expression that multiplies the value of the left operand by the value of the right operand. 
  /// The result of the expression is assigned to the left operand, which must be a target expression.
  /// When the operator is overloaded, this expression corresponds to a call to op_Multiplication.
  /// </summary>
  public class MultiplicationAssignment : BinaryOperationAssignment {

    /// <summary>
    /// Allocates an expression that multiplies the value of the left operand by the value of the right operand. 
    /// The result of the expression is assigned to the left operand, which must be a target expression.
    /// When the operator is overloaded, this expression corresponds to a call to op_Multiplication.
    /// </summary>
    /// <param name="leftOperand">The left operand and target of the assignment.</param>
    /// <param name="rightOperand">The right operand.</param>
    /// <param name="sourceLocation">The source location of the operation.</param>
    public MultiplicationAssignment(TargetExpression leftOperand, Expression rightOperand, ISourceLocation sourceLocation)
      : base(leftOperand, rightOperand, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected MultiplicationAssignment(BlockStatement containingBlock, MultiplicationAssignment template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Calls the visitor.Visit(MultiplicationAssignment) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (this.ContainingBlock == containingBlock) return this;
      return new MultiplicationAssignment(containingBlock, this);
    }

    /// <summary>
    /// Creates a multiplication expression with the given left operand and this.RightOperand.
    /// The method does not use this.LeftOperand.Expression, since it may be necessary to factor out any subexpressions so that
    /// they are evaluated only once. The given left operand expression is expected to be the expression that remains after factoring.
    /// </summary>
    /// <param name="leftOperand">An expression to combine with this.RightOperand into a binary expression.</param>
    protected override Expression CreateBinaryExpression(Expression leftOperand) {
      Expression result = new Multiplication(leftOperand, this.RightOperand, this.SourceLocation);
      result.SetContainingExpression(this);
      return result;
    }
  }

  /// <summary>
  /// An expression that represents a (name, value) pair and that is typically used in method calls, custom attributes and object initializers.
  /// </summary>
  public class NamedArgument : Expression, INamedArgument, IMetadataNamedArgument {

    /// <summary>
    /// Allocates an expression that represents a (name, value) pair and that is typically used in method calls, custom attributes and object initializers.
    /// </summary>
    /// <param name="argumentName">The name of the parameter or property or field that corresponds to the argument.</param>
    /// <param name="argumentValue">The value of the argument.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public NamedArgument(SimpleName argumentName, Expression argumentValue, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.argumentName = argumentName;
      this.argumentValue = argumentValue;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected NamedArgument(BlockStatement containingBlock, NamedArgument template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.argumentName = (SimpleName)template.argumentName.MakeCopyFor(containingBlock);
      this.argumentValue = template.argumentValue.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// Calls the visitor.Visit(INamedArgument) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit((INamedArgument)this);
    }

    /// <summary>
    /// Calls the visitor.Visit(NamedArgument) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// An empty collection of NamedArguments.
    /// </summary>
    new public static IEnumerable<NamedArgument> EmptyCollection {
      get { return NamedArgument.emptyCollection; }
    }
    readonly static IEnumerable<NamedArgument> emptyCollection = new List<NamedArgument>(0).AsReadOnly();

    /// <summary>
    /// The name of the parameter or property or field that corresponds to the argument.
    /// </summary>
    public SimpleName ArgumentName {
      get { return this.argumentName; }
    }
    readonly SimpleName argumentName;

    /// <summary>
    /// The value of the argument.
    /// </summary>
    public Expression ArgumentValue {
      get { return this.argumentValue; }
    }
    readonly Expression argumentValue;

    /// <summary>
    /// The expression that contains the named argument. For example, an object construction expression used in a custom attribute.
    /// </summary>
    public Expression ContainingExpression {
      get {
        //^ assume containingExpression != null;
        return this.containingExpression;
      }
    }
    Expression/*?*/ containingExpression;

    /// <summary>
    /// True if the named argument provides the value of a field.
    /// </summary>
    public bool IsField {
      get { return this.ResolvedDefinition is IFieldDefinition; }
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new NamedArgument(containingBlock, this);
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      return this;
    }

    /// <summary>
    /// Returns either null or the parameter or property or field that corresponds to this argument.
    /// </summary>
    public object/*?*/ ResolvedDefinition {
      get
        //^ ensures result == null || result is IParameterDefinition || result is IPropertyDefinition || result is IFieldDefinition;
      {
        object/*?*/ result = this.ArgumentName.Resolve(); //TODO: worry about setting up an appropriate scope
        if (result is IParameterDefinition || result is IPropertyDefinition || result is IFieldDefinition) return result;
        return null;
      }
    }

    /// <summary>
    /// If true, the resolved definition is a property whose getter is virtual.
    /// </summary>
    public bool GetterIsVirtual {
      get {
        var prop = this.ResolvedDefinition as IPropertyDefinition;
        if (prop != null && prop.Getter != null) return prop.Getter.ResolvedMethod.IsVirtual;
        return false;
      }
    }

    /// <summary>
    /// If true, the resolved definition is a property whose setter is virtual.
    /// </summary>
    public bool SetterIsVirtual {
      get {
        var prop = this.ResolvedDefinition as IPropertyDefinition;
        if (prop != null && prop.Setter != null) return prop.Setter.ResolvedMethod.IsVirtual;
        return false;
      }
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingExpression(Expression containingExpression) {
      base.SetContainingExpression(containingExpression);
      this.argumentName.SetContainingExpression(this);
      this.argumentValue.SetContainingExpression(this);
      this.containingExpression = containingExpression;
    }

    /// <summary>
    /// The type of value that the expression will evaluate to, as determined at compile time.
    /// </summary>
    public override ITypeDefinition Type {
      get { return argumentValue.Type; }
    }

    #region INamedArgument Members

    IName INamedArgument.ArgumentName {
      get { return this.ArgumentName.Name; }
    }

    IExpression INamedArgument.ArgumentValue {
      get { return this.ArgumentValue.ProjectAsIExpression(); }
    }

    #endregion

    #region IMetadataNamedArgument Members

    IName IMetadataNamedArgument.ArgumentName {
      get { return this.ArgumentName.Name; }
    }

    IMetadataExpression IMetadataNamedArgument.ArgumentValue {
      get { return this.ArgumentValue.ProjectAsIMetadataExpression(); }
    }

    object/*?*/ IMetadataNamedArgument.ResolvedDefinition {
      get { return this.ResolvedDefinition; }
    }

    #endregion

    #region IMetadataExpression Members

    void IMetadataExpression.Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    ITypeReference IMetadataExpression.Type {
      get { return this.Type; }
    }

    #endregion

    #region IExpression Members

    ITypeReference IExpression.Type {
      get { return this.Type; }
    }

    #endregion
  }

  /// <summary>
  /// An expression that refers to a type by specifying the type name.
  /// </summary>
  public class NamedTypeExpression : TypeExpression {

    /// <summary>
    /// Allocates an expression that refers to a type by specifying the type name.
    /// </summary>
    /// <param name="expression">An expression that names a type. 
    /// Must be an instance of SimpleName, QualifiedName or AliasQualifiedName.</param>
    public NamedTypeExpression(Expression expression)
      : base(expression.SourceLocation)
      //^ requires expression is SimpleName || expression is QualifiedName || expression is AliasQualifiedName;
    {
      this.expression = expression;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected NamedTypeExpression(BlockStatement containingBlock, NamedTypeExpression template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.expression = template.Expression.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// Calls visitor.Visit(NamedTypeExpression).
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// An expression that incorporates a type name. 
    /// Will be an instance of SimpleName, QualifiedName or AliasQualifiedName.
    /// </summary>
    public Expression Expression {
      get
        //^ ensures result is SimpleName || result is QualifiedName || result is AliasQualifiedName;
      {
        return this.expression;
      }
    }
    readonly Expression expression;
    //^ invariant expression is SimpleName || expression is QualifiedName || expression is AliasQualifiedName;

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new NamedTypeExpression(containingBlock, this);
    }

    /// <summary>
    /// Resolves the expression as a non generic type. If expression cannot be resolved, a dummy type is returned. 
    /// If the expression is ambiguous the first matching type is returned.
    /// If the expression does not resolve to exactly one type, an error is added to the error collection of the compilation context.
    /// </summary>
    protected override ITypeDefinition Resolve() {
      return this.Resolve(0);
    }

    /// <summary>
    /// Resolves the expression as a type with the given number of generic parameters. 
    /// If expression cannot be resolved an error is reported and a dummy type is returned. 
    /// If the expression is ambiguous (resolves to more than one type) an error is reported and the first matching type is returned.
    /// </summary>
    /// <param name="numberOfTypeParameters">The number of generic parameters the resolved type must have. This number must be greater than or equal to zero.</param>
    /// <returns>The resolved type if there is one, or Dummy.Type.</returns>
    public override ITypeDefinition Resolve(int numberOfTypeParameters)
      //^^ requires numberOfTypeParameters >= 0;
      //^^ ensures result == Dummy.Type || result.GenericParameterCount == numberOfTypeParameters;
    {
      ITypeDefinition result = Dummy.Type;
      SimpleName/*?*/ simpleName = this.Expression as SimpleName;
      if (simpleName != null) {
        result = this.Resolve(simpleName.ResolveAsNamespaceOrType(), numberOfTypeParameters);
        //^ assert result == Dummy.Type || result.GenericParameterCount == numberOfTypeParameters;
        if (result is Dummy)
          this.Helper.ReportError(new AstErrorMessage(this, Error.SingleTypeNameNotFound, simpleName.Name.Value));
        //^ assume result == Dummy.Type || result.GenericParameterCount == numberOfTypeParameters;
        return result;
      }
      QualifiedName/*?*/ qualifiedName = this.Expression as QualifiedName;
      if (qualifiedName != null) return this.Resolve(qualifiedName.ResolveAsNamespaceOrTypeGroup(), numberOfTypeParameters);
      AliasQualifiedName/*?*/ aliasQualName = this.Expression as AliasQualifiedName;
      if (aliasQualName != null) return this.Resolve(aliasQualName.Resolve(), numberOfTypeParameters);
      //^ assert false;
      return result;
    }

    /// <summary>
    /// Uses the given resolved expression to select a type with the given number of generic parameters.
    /// If the resolved expression is not a type or type group, or if the type does not have the required number of generic parameters, or if the type group
    /// does not include such a type, it reports an error and returns Dummy.Type.
    /// </summary>
    /// <param name="resolvedExpression">The resolved (compile time) value of an expression that is expected to resolve to a type.</param>
    /// <param name="numberOfTypeParameters">The number of generic parameters the resolved type must have. This number must be greater than or equal to zero.</param>
    /// <returns>The resolved type if there is one, or Dummy.Type.</returns>
    protected virtual ITypeDefinition Resolve(object/*?*/ resolvedExpression, int numberOfTypeParameters)
      //^ requires resolvedExpression == null || resolvedExpression is INamespaceDefinition || resolvedExpression is ITypeDefinition || resolvedExpression is ITypeGroup;
      //^ requires numberOfTypeParameters >= 0;
      //^ ensures result == Dummy.Type || result.GenericParameterCount == numberOfTypeParameters;
    {
      ITypeDefinition/*?*/ type = null;
      ITypeGroup/*?*/ typeGroup = resolvedExpression as ITypeGroup;
      if (typeGroup != null) {
        foreach (ITypeDefinition t in typeGroup.GetTypes(numberOfTypeParameters)) {
          if (type == null) type = t; //else TODO: give an error
        }
      } else {
        type = resolvedExpression as ITypeDefinition;
        if (type != null && type.GenericParameterCount != numberOfTypeParameters) type = null;
      }
      if (type != null) {
        //^ assume type.GenericParameterCount == numberOfTypeParameters;
        return type;
      }
      if (resolvedExpression != null) {
        Error e = Error.ToBeDefined;
        this.Helper.ReportError(new AstErrorMessage(this.Expression, e));
      }
      return Dummy.Type;
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a NamedTypeExpression before constructing the containing Block.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingExpression(Expression containingExpression) {
      base.SetContainingExpression(containingExpression);
      this.Expression.SetContainingExpression(this);
    }

  }

  /// <summary>
  /// An expression that refers to a namespace by specifying its name.
  /// </summary>
  public class NamespaceReferenceExpression : Expression {

    /// <summary>
    /// Allocates an expression that refers to a namespace by specifying its name.
    /// </summary>
    /// <param name="expression">An expression that names a namespace. 
    /// Must be an instance of SimpleName, QualifiedName or AliasQualifiedName.</param>
    /// <param name="sourceLocation">The source locations of the expression.</param>
    public NamespaceReferenceExpression(Expression expression, ISourceLocation sourceLocation)
      : base(sourceLocation)
      //^ requires expression is SimpleName || expression is QualifiedName || expression is AliasQualifiedName;
    {
      this.expression = expression;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected NamespaceReferenceExpression(BlockStatement containingBlock, NamespaceReferenceExpression template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.expression = template.expression.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// Calls the visitor.Visit(NamespaceReferenceExpression) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// An expression that should be the name of an INamespace instance. Must be an instance of SimpleName, QualifiedName or AliasQualifiedName.
    /// </summary>
    public Expression Expression {
      get
        //^ ensures result is SimpleName || result is QualifiedName || result is AliasQualifiedName;
      {
        return this.expression;
      }
    }
    readonly Expression expression;
    //^ invariant expression is SimpleName || expression is QualifiedName || expression is AliasQualifiedName;

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new NamespaceReferenceExpression(containingBlock, this);
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      //^ assume false; //no
      return new DummyExpression(this.SourceLocation);
    }

    /// <summary>
    /// Returns an INamespace instance that matches the expression. If no match is found Dummy.UnitNamespace is returned.
    /// If more than one match if found, an error is reported and one of the matches is returned.
    /// </summary>
    public virtual INamespaceDefinition Resolve() {
      object/*?*/ result = null;
      SimpleName/*?*/ simpleName = this.Expression as SimpleName;
      if (simpleName != null)
        result = simpleName.ResolveAsNamespace();
      else {
        QualifiedName/*?*/ qualifiedName = this.Expression as QualifiedName;
        if (qualifiedName != null)
          result = qualifiedName.ResolveAsNamespaceOrTypeGroup();
        else {
          AliasQualifiedName/*?*/ aliasQualName = this.Expression as AliasQualifiedName;
          if (aliasQualName != null) {
            result = aliasQualName.Resolve();
          } else {
            //^ assert false; //The post condition of this.Expression should prevent us from getting here.
          }
        }
      }
      INamespaceDefinition/*?*/ resolvedNamespace = result as INamespaceDefinition;
      if (resolvedNamespace != null) return resolvedNamespace;
      //TODO: error if result != null.
      return Dummy.RootUnitNamespace;
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a NamespaceReferenceExpression before constructing the containing Block.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingExpression(Expression containingExpression) {
      base.SetContainingExpression(containingExpression);
      this.expression.SetContainingExpression(this);
    }

    /// <summary>
    /// Returns Dummy.Type. Should never be called, since a NamespaceReferenceExpression should not be included in any context
    /// where the type of the (non existant) runtime value of the referenced namespace is called for.
    /// </summary>
    public override ITypeDefinition Type {
      get {
        //^ assume false; //It is a mistake to include a NamespaceReferenceExpression in a context where Type will be called.
        return Dummy.Type;
      }
    }

  }

  /// <summary>
  /// An object that represents a group of namespace types. It is an intermediate artifact
  /// to be used while resolving a name forming part of a generic type instance expression in the absence of knowledge of how many type arguments will be used to
  /// make the instantiation.
  /// </summary>
  public class NamespaceTypeGroup : ITypeGroup {

    /// <summary>
    /// Allocates an object that represents a group of types. It is an intermediate artifact
    /// to be used while resolving a name forming part of a generic type instance expression in the absence of knowledge of how many type arguments will be used to
    /// make the instantiation.
    /// </summary>
    /// <param name="expression">The (name) expression to resolve.</param>
    /// <param name="namespaceScope">The namespace scope to use to resolve the name.</param>
    /// <param name="simpleName">The name to resolve.</param>
    public NamespaceTypeGroup(Expression expression, IScope<INamespaceMember> namespaceScope, SimpleName simpleName) {
      this.expression = expression;
      this.simpleName = simpleName;
      this.namespaceScope = namespaceScope;
    }

    /// <summary>
    /// The (name) expression to resolve.
    /// </summary>
    public Expression Expression {
      get { return this.expression; }
    }
    readonly Expression expression;

    /// <summary>
    /// The types that Expression resolves to, given a particular number of generic parameters.
    /// </summary>
    public virtual IEnumerable<ITypeDefinition> GetTypes(int numberOfTypeParameters) {
      foreach (INamespaceMember member in this.namespaceScope.GetMembersNamed(this.simpleName.Name, this.simpleName.IgnoreCase)) {
        ITypeDefinition/*?*/ typeDefinition = member as ITypeDefinition;
        if (typeDefinition != null && typeDefinition.GenericParameterCount == numberOfTypeParameters)
          yield return typeDefinition;
      }
    }

    /// <summary>
    /// The name to resolve.
    /// </summary>
    readonly SimpleName simpleName;

    /// <summary>
    /// The namespace scope to use to resolve the name.
    /// </summary>
    readonly IScope<INamespaceMember> namespaceScope;

    /// <summary>
    /// The types that the expression has resolved to.
    /// </summary>
    public IEnumerable<ITypeDefinition> Types {
      get {
        return IteratorHelper.GetFilterEnumerable<INamespaceMember, ITypeDefinition>(this.namespaceScope.GetMembersNamed(this.simpleName.Name, this.simpleName.IgnoreCase));
      }
    }

    #region ITypeGroup Members

    IExpression ITypeGroup.Expression {
      get { return this.Expression.ProjectAsIExpression(); }
    }

    #endregion
  }

  /// <summary>
  /// An object that represents a group of nested types. It is an intermediate artifact
  /// to be used while resolving a name forming part of a generic type instance expression in the absence of knowledge of how many type arguments will be used to
  /// make the instantiation.
  /// </summary>
  public class NestedTypeGroup : ITypeGroup {

    /// <summary>
    /// Allocates an object that represents a group of types. It is an intermediate artifact
    /// to be used while resolving a name forming part of a generic type instance expression in the absence of knowledge of how many type arguments will be used to
    /// make the instantiation.
    /// </summary>
    /// <param name="expression">The (name) expression to resolve.</param>
    /// <param name="containingType">The type definition to use to resolve the name.</param>
    /// <param name="simpleName">The name to resolve.</param>
    public NestedTypeGroup(Expression expression, ITypeDefinition containingType, SimpleName simpleName) {
      this.expression = expression;
      this.simpleName = simpleName;
      this.containingType = containingType;
    }

    /// <summary>
    /// The (name) expression to resolve.
    /// </summary>
    public Expression Expression {
      get { return this.expression; }
    }
    readonly Expression expression;

    /// <summary>
    /// The name to resolve.
    /// </summary>
    readonly SimpleName simpleName;

    /// <summary>
    /// The type definition to use to resolve the name.
    /// </summary>
    readonly ITypeDefinition containingType;

    /// <summary>
    /// The types that Expression resolves to, given a particular number of generic parameters.
    /// </summary>
    public virtual IEnumerable<ITypeDefinition> GetTypes(int numberOfTypeParameters) {
      ITypeDefinition/*?*/ type = this.GetNestedType(this.containingType, numberOfTypeParameters);
      if (type == null)
        return Enumerable<ITypeDefinition>.Empty;
      else
        return IteratorHelper.GetSingletonEnumerable<ITypeDefinition>(type);
    }

    /// <summary>
    /// Starting with the given containingType and running up the base hierarchy if needed, return the first nested
    /// type with the given number of type parameters and name that matches this.simpleName. Returns null if no such type is found.
    /// </summary>
    /// <param name="containingType">The type where the search should start and whose base types should be searched if necessary.</param>
    /// <param name="numberOfTypeParameters">The number of type parameters the resulting type should have.</param>
    /// <returns></returns>
    ITypeDefinition/*?*/ GetNestedType(ITypeDefinition containingType, int numberOfTypeParameters)
      //^ ensures result == null || result.GenericParameterCount == numberOfTypeParameters;
    {
      foreach (ITypeDefinitionMember member in containingType.GetMembersNamed(this.simpleName.Name, this.simpleName.IgnoreCase)) {
        ITypeDefinition/*?*/ typeDefinition = member as ITypeDefinition;
        if (typeDefinition != null && typeDefinition.GenericParameterCount == numberOfTypeParameters) return typeDefinition;
      }
      foreach (ITypeReference baseClassRef in containingType.BaseClasses) {
        ITypeDefinition/*?*/ typeDefinition = this.GetNestedType(baseClassRef.ResolvedType, numberOfTypeParameters);
        if (typeDefinition != null) return typeDefinition;
      }
      return null;
    }

    /// <summary>
    /// Starting with the given containingType and running up the base hierarchy return all the nested
    /// types whose names match this.simpleName.
    /// </summary>
    /// <param name="containingType">The type where the search should start and whose base types should be also be searched.</param>
    /// <returns></returns>
    IEnumerable<ITypeDefinition> GetNestedTypes(ITypeDefinition containingType) {
      foreach (ITypeDefinitionMember member in containingType.GetMembersNamed(this.simpleName.Name, this.simpleName.IgnoreCase)) {
        ITypeDefinition/*?*/ typeDefinition = member as ITypeDefinition;
        if (typeDefinition != null) yield return typeDefinition;
      }
      foreach (ITypeReference baseClassRef in containingType.BaseClasses) {
        foreach (ITypeDefinition nestedType in this.GetNestedTypes(baseClassRef.ResolvedType))
          yield return nestedType;
      }
    }

    /// <summary>
    /// The types that the expression has resolved to.
    /// </summary>
    public IEnumerable<ITypeDefinition> Types {
      get {
        return this.GetNestedTypes(this.containingType);
      }
    }

    #region ITypeGroup Members

    IExpression ITypeGroup.Expression {
      get { return this.Expression.ProjectAsIExpression(); }
    }

    #endregion
  }

  /// <summary>
  /// Spec#
  /// </summary>
  public class NonNullTypeExpression : TypeExpression {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="elementType"></param>
    /// <param name="sourceLocation"></param>
    public NonNullTypeExpression(TypeExpression elementType, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.elementType = elementType;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected NonNullTypeExpression(BlockStatement containingBlock, NonNullTypeExpression template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.elementType = (TypeExpression)template.elementType.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// Calls the visitor.Visit(NonNullTypeExpression) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Gets the type of the element.
    /// </summary>
    /// <value>The type of the element.</value>
    public TypeExpression ElementType {
      get { return this.elementType; }
    }
    readonly TypeExpression elementType;

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new NonNullTypeExpression(containingBlock, this);
    }

    /// <summary>
    /// The type denoted by the expression. If expression cannot be resolved, a dummy type is returned. If the expression is ambiguous the first matching type is returned.
    /// If the expression does not resolve to exactly one type, an error is added to the error collection of the compilation context.
    /// </summary>
    protected override ITypeDefinition Resolve() {
      return this.ElementType.ResolvedType;
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingExpression(Expression containingExpression) {
      base.SetContainingExpression(containingExpression);
      this.elementType.SetContainingExpression(this);
    }

  }

  /// <summary>
  /// An expression that results in false if both operands represent the same value or object. When overloaded, this expression corresponds to a call to op_Inequality.
  /// </summary>
  public class NotEquality : EqualityComparison, INotEquality {

    /// <summary>
    /// Allocates an expression that results in false if both operands represent the same value or object. When overloaded, this expression corresponds to a call to op_Inequality.
    /// </summary>
    /// <param name="leftOperand">The left operand.</param>
    /// <param name="rightOperand">The right operand.</param>
    /// <param name="sourceLocation">The source location of the operation.</param>
    public NotEquality(Expression leftOperand, Expression rightOperand, ISourceLocation sourceLocation)
      : base(leftOperand, rightOperand, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected NotEquality(BlockStatement containingBlock, NotEquality template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Calls the visitor.Visit(INotEquality) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(NotEquality) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Returns the string used to identify the operator in error messages
    /// </summary>
    protected override string OperationSymbolForErrorMessage {
      get { return "!="; }
    }


    /// <summary>
    /// Returns the name a method must have to overload this operator.
    /// </summary>
    protected override IName GetOperatorName() {
      return this.NameTable.OpInequality;
    }

    /// <summary>
    /// Computes the compile time value of the expression. Can be null.
    /// </summary>
    protected override object/*?*/ GetValue() {
      object/*?*/ left = this.ConvertedLeftOperand.Value;
      object/*?*/ right = this.ConvertedRightOperand.Value;
      if (left == null || right == null) return null;
      switch (System.Convert.GetTypeCode(left)) {
        case TypeCode.Int32:
          //^ assume left is int && right is int;
          return (int)left != (int)right;
        case TypeCode.UInt32:
          //^ assume left is uint && right is uint;
          return (uint)left != (uint)right;
        case TypeCode.Int64:
          //^ assume left is long && right is long;
          return (long)left != (long)right;
        case TypeCode.UInt64:
          //^ assume left is ulong && right is ulong;
          return (ulong)left != (ulong)right;
        case TypeCode.Single:
          //^ assume left is float && right is float;
          return (float)left != (float)right;
        case TypeCode.Double:
          //^ assume left is double && right is double;
          return (double)left != (double)right;
        case TypeCode.Decimal:
          //^ assume left is decimal && right is decimal;
          return (decimal)left != (decimal)right;
        case TypeCode.Boolean:
          //^ assume left is bool && right is bool;
          return (bool)left != (bool)right;
        case TypeCode.String:
          //^ assume left is string && right is string;
          return (string)left != (string)right;
        case TypeCode.Object:
          if (left is IntPtr && right is IntPtr)
            return (IntPtr)left != (IntPtr)right;
          break;
      }
      return null;
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new NotEquality(containingBlock, this);
    }

  }

  /// <summary>
  /// An expression that denotes a nullable type.
  /// </summary>
  public class NullableTypeExpression : TypeExpression {

    /// <summary>
    /// Allocates an expression that denotes a nullable type.
    /// </summary>
    /// <param name="elementType">The non nullable type on which this nullable type is to be based.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public NullableTypeExpression(TypeExpression elementType, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.elementType = elementType;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected NullableTypeExpression(BlockStatement containingBlock, NullableTypeExpression template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.elementType = (TypeExpression)template.elementType.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// Calls the visitor.Visit(NullableTypeExpression) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The non nullable type on which this nullable type is to be based.
    /// </summary>
    public TypeExpression ElementType {
      get { return this.elementType; }
    }
    readonly TypeExpression elementType;

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new NullableTypeExpression(containingBlock, this);
    }

    /// <summary>
    /// The type denoted by the expression. If expression cannot be resolved, a dummy type is returned. If the expression is ambiguous the first matching type is returned.
    /// If the expression does not resolve to exactly one type, an error is added to the error collection of the compilation context.
    /// </summary>
    protected override ITypeDefinition Resolve() {
      var systemNullable = this.PlatformType.SystemNullable.ResolvedType;
      if (!systemNullable.IsGeneric) return Dummy.Type;
      List<ITypeReference> genericArguments = new List<ITypeReference>(1);
      genericArguments.Add(this.ElementType.ResolvedType);
      //^ assume systemNullable.ResolvedType == systemNullable;
      return GenericTypeInstance.GetGenericTypeInstance(systemNullable, genericArguments, this.Compilation.HostEnvironment.InternFactory);
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingExpression(Expression containingExpression) {
      base.SetContainingExpression(containingExpression);
      this.elementType.SetContainingExpression(this);
    }

  }

  /// <summary>
  /// An expression that evaluates the first operand and results in its value if it is not null. Otherwise the second operand is evaluated and its value becomes the value
  /// of the expression. This corresponds to the ?? operator in C#.
  /// </summary>
  public class NullCoalescing : Expression {

    /// <summary>
    /// Allocates an expression that evaluates the first operand and results in its value if it is not null. Otherwise the second operand is evaluated and its value becomes the value
    /// of the expression. This corresponds to the ?? operator in C#.
    /// </summary>
    /// <param name="leftOperand">The left operand.</param>
    /// <param name="rightOperand">The right operand.</param>
    /// <param name="sourceLocation">The source location of the operation.</param>
    public NullCoalescing(Expression leftOperand, Expression rightOperand, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.leftOperand = leftOperand;
      this.rightOperand = rightOperand;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected NullCoalescing(BlockStatement containingBlock, NullCoalescing template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.leftOperand = template.LeftOperand.MakeCopyFor(containingBlock);
      this.rightOperand = template.RightOperand.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// Calls the visitor.Visit(NullCoalescing) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Infers the type of value that this expression will evaluate to. At runtime the actual value may be an instance of subclass of the result of this method.
    /// Calling this method does not cache the computed value and does not generate any error messages. In some cases, such as references to the parameters of lambda
    /// expressions during type overload resolution, the value returned by this method may be different from one call to the next.
    /// When type inference fails, Dummy.Type is returned.
    /// </summary>
    public override ITypeDefinition InferType() {
      ITypeDefinition A = this.LeftOperand.Type;
      ITypeDefinition A0 = this.Helper.RemoveNullableWrapper(A);
      ITypeDefinition B = this.RightOperand.Type;
      if (this.Helper.ImplicitConversionExists(B, A0))
        return A0;
      else if (A != A0 && this.Helper.ImplicitConversionExists(B, A))
        return A;
      else if (this.Helper.ImplicitConversionExists(A0, B))
        return B;
      return Dummy.Type;
    }

    /// <summary>
    /// The left operand.
    /// </summary>
    public Expression LeftOperand {
      get { return this.leftOperand; }
    }
    readonly Expression leftOperand;

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (this.ContainingBlock == containingBlock) return this;
      return new NullCoalescing(containingBlock, this);
    }

    /// <summary>
    /// Returns let temp = a in temp == null ? b : (T)temp where T is the type of a (after removing ? if there) or type of b.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      if (this.projectedExpression == null) {
        ITypeDefinition T = this.Type;

        List<Statement> statements = new List<Statement>(1);
        ILocalDefinition tempVar = Expression.CreateInitializedLocalDeclarationAndAddDeclarationsStatementToList(this.LeftOperand, statements);
        BlockStatement block = new BlockStatement(statements, this.SourceLocation);
        BoundExpression temp = new BoundExpression(this.LeftOperand, tempVar);

        Equality tIsNull = new Equality(temp, new NullLiteral(SourceDummy.SourceLocation), temp.SourceLocation);
        Conditional conditional = new Conditional(tIsNull, this.Helper.ImplicitConversion(this.RightOperand, T), this.Helper.ExplicitConversion(this.LeftOperand, T), this.SourceLocation);
        BlockExpression be = new BlockExpression(block, conditional, this.SourceLocation);
        be.SetContainingExpression(this);
        this.projectedExpression = be.ProjectAsIExpression();
      }
      return this.projectedExpression;
    }
    //^ [Once]
    IExpression/*?*/ projectedExpression;

    /// <summary>
    /// The right operand.
    /// </summary>
    public Expression RightOperand {
      get { return this.rightOperand; }
    }
    readonly Expression rightOperand;

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingExpression(Expression containingExpression) {
      base.SetContainingExpression(containingExpression);
      this.LeftOperand.SetContainingExpression(this);
      this.RightOperand.SetContainingExpression(this);
    }

    /// <summary>
    /// The type of value that the expression will evaluate to, as determined at compile time.
    /// </summary>
    public sealed override ITypeDefinition Type {
      get {
        if (this.type == null)
          this.type = this.InferType();
        return this.type;
      }
    }
    //^ [Once]
    ITypeDefinition/*?*/ type;

  }

  /// <summary>
  /// A compile time constant that represents the null literal.
  /// </summary>
  public class NullLiteral : CompileTimeConstant {

    /// <summary>
    /// Allocates a compile time constant that represents the null literal.
    /// </summary>
    /// <param name="sourceLocation">The location in the source text of the expression that corresponds to this constant.</param>
    public NullLiteral(ISourceLocation sourceLocation)
      : base(null, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    public NullLiteral(BlockStatement containingBlock, CompileTimeConstant template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Infers the type of value that this expression will evaluate to. At runtime the actual value may be an instance of subclass of the result of this method.
    /// Calling this method does not cache the computed value and does not generate any error messages. In some cases, such as references to the parameters of lambda
    /// expressions during type overload resolution, the value returned by this method may be different from one call to the next.
    /// When type inference fails, Dummy.Type is returned.
    /// </summary>
    public override ITypeDefinition InferType() {
      return this.PlatformType.SystemObject.ResolvedType;
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new NullLiteral(containingBlock, this);
    }

  }

  /// <summary>
  /// An expression that must match an out parameter of a method. The method assigns a value to the target Expression.
  /// </summary>
  public class OutArgument : Expression, IOutArgument {

    /// <summary>
    /// Allocates an expression that must match an out parameter of a method. The method assigns a value to the target Expression.
    /// </summary>
    /// <param name="expression">The target that is assigned to as a result of the method call that contains this out argument.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public OutArgument(TargetExpression expression, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.expression = expression;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected OutArgument(BlockStatement containingBlock, OutArgument template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.expression = (TargetExpression)template.expression.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return this.Expression.HasErrors;
    }

    /// <summary>
    /// Calls the visitor.Visit(IOutArgument) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(OutArgument) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The target that is assigned to as a result of the method call.
    /// </summary>
    public TargetExpression Expression {
      get { return this.expression; }
    }
    readonly TargetExpression expression;

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new OutArgument(containingBlock, this);
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      return this;
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingExpression(Expression containingExpression) {
      base.SetContainingExpression(containingExpression);
      this.expression.SetContainingExpression(this);
    }

    /// <summary>
    /// The type of value that the expression will evaluate to, as determined at compile time.
    /// </summary>
    public override ITypeDefinition Type {
      get { return this.Expression.Type; }
    }

    #region IOutArgument Members

    ITargetExpression IOutArgument.Expression {
      get { return this.expression; }
    }

    #endregion

    #region IExpression Members

    ITypeReference IExpression.Type {
      get { return this.Type; }
    }

    #endregion

  }

  /// <summary>
  /// An expression that represents the value that a target expression had at the start of the method that as a postcondition that includes this expression.
  /// </summary>
  // ^ [Immutable]
  public sealed class OldValue : Expression, IOldValue {

    /// <summary>
    /// Allocates an expression that represents the value that a target expression had at the start of the method that as a postcondition that includes this expression.
    /// </summary>
    /// <param name="expression">The expression whose value at the start of method execution is referred to in the method postcondition.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated source item.</param>
    public OldValue(Expression expression, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.expression = expression;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">The containing block of the copied postcondition. This should be different from the containing block of the template postcondition.</param>
    /// <param name="template">The statement to copy.</param>
    private OldValue(BlockStatement containingBlock, OldValue template)
      : base(containingBlock, template.SourceLocation)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.expression = template.Expression.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return this.Expression.HasErrors;
    }

    /// <summary>
    /// Calls visitor.Visit(IOldValue).
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls visitor.Visit(OldValue).
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      base.Dispatch(visitor); //TODO: implement this
    }

    /// <summary>
    /// The expression whose value at the start of method execution is referred to in the method postcondition.
    /// </summary>
    public Expression Expression {
      get { return this.expression; }
    }
    readonly Expression expression;

    /// <summary>
    /// Checks if the expression has a side effect and reports an error unless told otherwise.
    /// </summary>
    /// <param name="reportError">If true, report an error if the expression has a side effect.</param>
    public override bool HasSideEffect(bool reportError) {
      return this.Expression.HasSideEffect(reportError);
    }

    /// <summary>
    /// Makes a copy of this statement, changing the ContainingBlock to the given block.
    /// </summary>
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
    {
      if (this.ContainingBlock == containingBlock) return this;
      return new OldValue(containingBlock, this);
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      return this;
      //TODO: return a binding to a local and make sure that method generates code to initialize the local with the value of target expression.
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingExpression(Expression containingExpression) {
      base.SetContainingExpression(containingExpression);
      this.Expression.SetContainingExpression(containingExpression);
    }

    /// <summary>
    /// The type of value that the expression will evaluate to, as determined at compile time.
    /// </summary>
    /// <value></value>
    public override ITypeDefinition Type {
      get { return this.Expression.Type; }
    }

    #region IOldValue Members

    IExpression IOldValue.Expression {
      get { return this.Expression.ProjectAsIExpression(); }
    }

    #endregion

    #region IExpression Members

    ITypeReference IExpression.Type {
      get { return this.Type; }
    }

    #endregion
  }

  /// <summary>
  /// An expression that results in the bitwise not (1's complement) of the operand. When overloaded, this expression corresponds to a call to op_OnesComplement.
  /// </summary>
  public class OnesComplement : UnaryOperation, IOnesComplement {

    /// <summary>
    /// Allocates an expression that results in the bitwise not (1's complement) of the operand. When overloaded, this expression corresponds to a call to op_OnesComplement.
    /// </summary>
    /// <param name="operand">The value on which the operation is performed.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public OnesComplement(Expression operand, ISourceLocation sourceLocation)
      : base(operand, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected OnesComplement(BlockStatement containingBlock, OnesComplement template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// True if the constant is a positive integer that could be interpreted as a negative signed integer.
    /// For example, 0x80000000, could be interpreted as a convenient way of writing int.MinValue.
    /// </summary>
    public override bool CouldBeInterpretedAsNegativeSignedInteger {
      get {
        if (this.Operand.ValueIsPolymorphicCompileTimeConstant && !this.Operand.CouldBeInterpretedAsNegativeSignedInteger) return true;
        return false;
      }
    }

    /// <summary>
    /// True if this expression is a constant negative integer that could also be interpreted as a unsigned integer.
    /// For example, 1 &lt;&lt; 31 could also be interpreted as a convenient way of writing 0x80000000.
    /// </summary>
    public override bool CouldBeInterpretedAsUnsignedInteger {
      get {
        if (this.Operand.ValueIsPolymorphicCompileTimeConstant && this.Operand.CouldBeInterpretedAsNegativeSignedInteger) return true;
        return false;
      }
    }

    /// <summary>
    /// Calls the visitor.Visit(IOnesComplement) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(OnesComplement) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Returns the name a method must have to overload this operator.
    /// </summary>
    protected override IName GetOperatorName() {
      return this.NameTable.OpOnesComplement;
    }

    /// <summary>
    /// Computes the compile time value of the expression. Can be null.
    /// </summary>
    protected override object/*?*/ GetValue() {
      object/*?*/ val = this.ConvertedOperand.Value;
      switch (System.Convert.GetTypeCode(val)) {
        case TypeCode.Int32:
          //^ assume val is int;
          return ~(int)val;
        case TypeCode.UInt32:
          //^ assume val is uint;
          return ~(uint)val;
        case TypeCode.Int64:
          //^ assume val is long;
          return ~(long)val;
        case TypeCode.UInt64:
          //^ assume val is ulong;
          return ~(ulong)val;
      }
      return null;
    }

    /// <summary>
    /// Returns the string used to identify the operator in error messages
    /// </summary>
    protected override string OperationSymbolForErrorMessage {
      get { return "~"; }
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new OnesComplement(containingBlock, this);
    }

    /// <summary>
    /// A list of dummy methods that correspond to operations that are built into IL. The dummy methods are used, via overload resolution,
    /// to determine how the operands are to be converted before the operation is carried out.
    /// </summary>
    protected override IEnumerable<IMethodDefinition> StandardOperators {
      get {
        BuiltinMethods dummyMethods = this.Compilation.BuiltinMethods;
        yield return dummyMethods.OpInt32;
        yield return dummyMethods.OpUInt32;
        yield return dummyMethods.OpInt64;
        yield return dummyMethods.OpUInt64;
        if (this.Operand.Type.IsEnum)
          yield return dummyMethods.GetDummyOpEnum(this.Operand.Type);
      }
    }


    #region IExpression Members

    ITypeReference IExpression.Type {
      get { return this.Type; }
    }

    #endregion
  }

  /// <summary>
  /// An expression that has been explicitly parenthesized in the source code.
  /// </summary>
  public class Parenthesis : Expression {

    /// <summary>
    /// Allocates an expression that has been explicitly parenthesized in the source code.
    /// </summary>
    /// <param name="parenthesizedExpression">The parenthesized expression.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public Parenthesis(Expression parenthesizedExpression, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.parenthesizedExpression = parenthesizedExpression;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected Parenthesis(BlockStatement containingBlock, Parenthesis template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.parenthesizedExpression = template.parenthesizedExpression.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return this.ParenthesizedExpression.HasErrors;
    }

    /// <summary>
    /// True if the constant is a positive integer that could be interpreted as a negative signed integer.
    /// For example, 0x80000000, could be interpreted as a convenient way of writing int.MinValue.
    /// </summary>
    public override bool CouldBeInterpretedAsNegativeSignedInteger {
      get { return this.ParenthesizedExpression.CouldBeInterpretedAsNegativeSignedInteger; }
    }

    /// <summary>
    /// Returns true if no information is lost if the integer value of this expression is converted to the target integer type.
    /// </summary>
    /// <param name="targetType"></param>
    /// <returns></returns>
    public override bool IntegerConversionIsLossless(ITypeDefinition targetType) {
      return this.ParenthesizedExpression.IntegerConversionIsLossless(targetType);
    }

    /// <summary>
    /// Calls the visitor.Visit(Parenthesis) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Computes the compile time value of the expression. Can be null.
    /// </summary>
    protected override object/*?*/ GetValue() {
      return this.ParenthesizedExpression.Value;
    }

    /// <summary>
    /// Checks if the expression has a side effect and reports an error unless told otherwise.
    /// </summary>
    /// <param name="reportError">If true, report an error if the expression has a side effect.</param>
    public override bool HasSideEffect(bool reportError) {
      return this.ParenthesizedExpression.HasSideEffect(reportError);
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new Parenthesis(containingBlock, this);
    }

    /// <summary>
    /// The parenthesized expression.
    /// </summary>
    public Expression ParenthesizedExpression {
      get { return this.parenthesizedExpression; }
    }
    readonly Expression parenthesizedExpression;

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      return this.ParenthesizedExpression.ProjectAsIExpression(); ;
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingExpression(Expression containingExpression) {
      base.SetContainingExpression(containingExpression);
      this.ParenthesizedExpression.SetContainingExpression(this);
    }

    /// <summary>
    /// Returns a string representation of the expression for debugging and logging uses.
    /// </summary>
    public override string ToString() {
      return "(" + this.ParenthesizedExpression.ToString() + ")";
    }

    /// <summary>
    /// The type of value that the expression will evaluate to, as determined at compile time.
    /// </summary>
    public override ITypeDefinition Type {
      get { return this.ParenthesizedExpression.Type; }
    }

    /// <summary>
    /// Returns true if the expression represents a compile time constant without an explicitly specified type. For example, 1 rather than 1L.
    /// Constant expressions such as 2*16 are polymorhpic if both operands are polymorhic.
    /// </summary>
    public override bool ValueIsPolymorphicCompileTimeConstant {
      get { return this.ParenthesizedExpression.ValueIsPolymorphicCompileTimeConstant; }
    }

  }

  /// <summary>
  /// An expression consisting of a the prefix of a simple name, for example "SimpleN".
  /// </summary>
  /// <remarks>This is intended for use by an editor that want to offer a list of completions for partially typed simple name.</remarks>
  public class PartialName : Expression {

    /// <summary>
    /// Constructs an expression consisting of a the prefix of a simple name, for example "SimpleN".
    /// </summary>
    /// <param name="simpleName">The simple name to treat as partial name. Must be fully constructed.</param>
    public PartialName(SimpleName simpleName)
      : base(simpleName.ContainingBlock, simpleName.SourceLocation) {
      this.ignoreCase = simpleName.IgnoreCase;
      this.name = simpleName.Name;
    }

    /// <summary>
    /// Constructs an expression consisting of a the prefix of a simple name, for example "SimpleN".
    /// </summary>
    /// <param name="simpleName">The simple name to treat as partial name. Must be fully constructed.</param>
    /// <param name="ignoreCase">True if the case of the partial name must be ignored when resolving it.</param>
    public PartialName(SimpleName simpleName, bool ignoreCase)
      : base(simpleName.ContainingBlock, simpleName.SourceLocation) {
      this.ignoreCase = ignoreCase;
      this.name = simpleName.Name;
    }

    /// <summary>
    /// Does nothing and always returns false.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return false;
    }

    /// <summary>
    /// Does nothing.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
    }

    /// <summary>
    /// Always returns null.
    /// </summary>
    protected override object/*?*/ GetValue() {
      return null;
    }

    /// <summary>
    /// Always return Dumm.Type.
    /// </summary>
    public override ITypeDefinition InferType() {
      return Dummy.Type;
    }

    /// <summary>
    /// Gets the namespace in which the given type is contained. Also works for nested types.
    /// </summary>
    protected NamespaceDeclaration GetContainingNamespace(TypeDeclaration typeDeclaration) {
      while (true) {
        NestedTypeDeclaration/*?*/ ntd = typeDeclaration as NestedTypeDeclaration;
        if (ntd == null) break;
        typeDeclaration = ntd.ContainingTypeDeclaration;
      }
      NamespaceTypeDeclaration/*?*/ nd = typeDeclaration as NamespaceTypeDeclaration;
      if (nd != null) return nd.ContainingNamespaceDeclaration;
      //The given type declaration is not nested and not in a namespace declaration. That cannot be statically precluded, but is an invalid way to construct an AST.
      //^ assume nd != null;
      return nd.ContainingNamespaceDeclaration; //fail so that AST constructor gets some feedback while debugging.
    }

    /// <summary>
    /// Always return false;
    /// </summary>
    /// <param name="reportError">If true, report an error if the expression has a side effect.</param>
    public override bool HasSideEffect(bool reportError) {
      return false;
    }

    /// <summary>
    /// True if the case of the partial name must be ignored when resolving it.
    /// </summary>
    public bool IgnoreCase {
      get { return this.ignoreCase; }
    }
    private readonly bool ignoreCase;

    /// <summary>
    /// Always returns this.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      return this;
    }

    /// <summary>
    /// The name corresponding to the source expression.
    /// </summary>
    public IName Name {
      get { return this.name; }
    }
    readonly IName name;

    /// <summary>
    /// Always return CodeDummy.Expression.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      return CodeDummy.Expression;
      //if (this.cachedProjection != null) return this.cachedProjection;
      //object/*?*/ container = this.ResolveAsValueContainer();
      //IPropertyDefinition/*?*/ property = container as IPropertyDefinition;
      //if (property != null) {
      //  if (property.Getter != null) {
      //    if (property.Getter.ResolvedMethod.IsStatic)
      //      return this.cachedProjection = new ResolvedMethodCall(property.Getter.ResolvedMethod, new List<Expression>(0), this.SourceLocation);
      //    else {
      //      ThisReference thisRef = new ThisReference(this.SourceLocation);
      //      thisRef.SetContainingExpression(this);
      //      return this.cachedProjection = new ResolvedMethodCall(property.Getter.ResolvedMethod, thisRef, new List<Expression>(0), this.SourceLocation);
      //    }
      //  }
      //  return this.cachedProjection = new DummyExpression(this.SourceLocation);
      //}
      //if (container is IEventDefinition) return this.cachedProjection = new DummyExpression(this.SourceLocation); //TODO: is this right?
      //IFieldDefinition/*?*/ field = container as IFieldDefinition;
      //if (field != null) {
      //  if (this.Helper.UseCompileTimeValueOfField(field)) return this.cachedProjection = CompileTimeConstant.For(field.CompileTimeValue, this);
      //} else {
      //  LocalDefinition/*?*/ local = container as LocalDefinition;
      //  if (local != null) {
      //    if (local.IsConstant) {
      //      var compileTimeVal = local.CompileTimeValue.ProjectAsIExpression();
      //      if (!(compileTimeVal is DummyConstant)) return cachedProjection = compileTimeVal;
      //    }
      //  }
      //}
      //if (container == null) container = Dummy.Field;
      //Expression expr =  new BoundExpression(this, container);
      //IParameterDefinition/*?*/ parameter = container as IParameterDefinition;
      //if (parameter != null && parameter.IsByReference) {
      //  expr = new AddressDereference(expr, expr.SourceLocation);
      //}
      //expr.SetContainingExpression(this);
      //return this.cachedProjection = (IExpression)expr;
    }

    /// <summary>
    /// Returns a list of named entities that might bind to the partial name once completed.
    /// </summary>
    public virtual List<INamedEntity> GetCandidates()
      //^^ ensures result == null || result is ILocalDefinition || result is IParameterDefinition || result is ITypeDefinitionMember || result is INamespaceMember || 
      //^^ result is ITypeDefinition || result is ITypeGroup || result is INamespaceDefinition;
    {
      List<INamedEntity> candidates = new List<INamedEntity>();
      this.PopulateUsing(this.ContainingBlock, candidates);
      return candidates;
    }

    /// <summary>
    /// Populates the given list with named entities that might bind to the partial name once completed,
    /// by using the scope chain of the given block.
    /// </summary>
    /// <param name="block">The block whose scope chain is used to resolve this name.</param>
    /// <param name="candidates">A list of named entities to which candidates should be added.</param>
    protected virtual void PopulateUsing(BlockStatement block, List<INamedEntity> candidates)
      //^ ensures result == null || result is ITypeDefinition || result is INamespaceDefinition || result is ITypeGroup ||
      //^ (!restrictToNamespacesAndTypes && (result is ILocalDefinition || result is IParameterDefinition || result is ITypeDefinitionMember || result is INamespaceMember));
    {
      for (BlockStatement b = block; b.ContainingSignatureDeclaration == block.ContainingSignatureDeclaration && b.ContainingBlock != b && b.Scope is StatementScope; b = b.ContainingBlock) {
        foreach (var local in b.Scope.Members) {
          if (local.Name.Value.StartsWith(this.Name.Value, this.ignoreCase, CultureInfo.InvariantCulture))
            candidates.Add(local.LocalVariable);
        }
      }
      if (block.ContainingSignatureDeclaration != null) {
        foreach (ParameterDeclaration par in block.ContainingSignatureDeclaration.Parameters) {
          if (par.Name.Value.StartsWith(this.Name.Value, this.ignoreCase, CultureInfo.InvariantCulture))
            candidates.Add(par.ParameterDefinition);
        }
      }
      AnonymousMethod/*?*/ anonymousMethod = block.ContainingSignatureDeclaration as AnonymousMethod;
      if (anonymousMethod != null) {
        this.PopulateUsing(anonymousMethod.ContainingBlock, candidates);
        return;
      }
      AnonymousDelegate/*?*/ anonymousDelegate = block.ContainingSignatureDeclaration as AnonymousDelegate;
      if (anonymousDelegate != null) {
        this.PopulateUsing(anonymousDelegate.ContainingBlock, candidates);
        return;
      }
      if (block.ContainingSignatureDeclaration != null) {
        this.PopulateUsing(block.ContainingSignatureDeclaration, candidates);
        return;
      }
      if (block.ContainingTypeDeclaration != null) {
        this.PopulateUsing(block.ContainingTypeDeclaration, candidates);
        return;
      }
      this.PopulateUsing(block.ContainingNamespaceDeclaration, candidates);
    }

    /// <summary>
    /// Populates the given list with named entities that might bind to the partial name once completed,
    /// by using the scope chain of the given method.
    /// </summary>
    /// <param name="signatureDeclaration">The signature bearing object whose scope chain is used to resolve this name.</param>
    /// <param name="candidates">A list of named entities to which candidates should be added.</param>
    protected virtual void PopulateUsing(ISignatureDeclaration signatureDeclaration, List<INamedEntity> candidates)
      //^ ensures result == null || result is ITypeDefinition || result is INamespaceDefinition || result is ITypeGroup ||
      //^ (!restrictToNamespacesAndTypes && (result is IParameterDefinition || result is ITypeDefinitionMember || result is INamespaceMember));
    {
      MethodDeclaration/*?*/ method = signatureDeclaration as MethodDeclaration;
      if (method != null && method.IsGeneric) {
        foreach (GenericMethodParameterDeclaration gpar in method.GenericParameters) {
          if (gpar.Name.Value.StartsWith(this.Name.Value, this.ignoreCase, CultureInfo.InvariantCulture))
            candidates.Add(gpar.GenericMethodParameterDefinition);
        }
      }
      ITypeDeclarationMember/*?*/ typeDeclarationMember = signatureDeclaration as ITypeDeclarationMember;
      if (typeDeclarationMember == null) return;
      this.PopulateUsing(typeDeclarationMember.ContainingTypeDeclaration, candidates);
    }

    /// <summary>
    /// Populates the given list with named entities that might bind to the partial name once completed,
    /// by using the scope chain of the given type declaration.
    /// </summary>
    /// <param name="typeDeclaration">The type whose scope chain is used to resolve this name.</param>
    /// <param name="candidates">A list of named entities to which candidates should be added.</param>
    protected virtual void PopulateUsing(TypeDeclaration typeDeclaration, List<INamedEntity> candidates)
      //^ ensures result == null || result is ITypeDefinition || result is INamespaceDefinition || result is ITypeGroup ||
      //^ (!restrictToNamespacesAndTypes && (result is ITypeDefinitionMember || result is INamespaceMember));
    {
      //Look for type parameter
      ITypeDefinition type = typeDeclaration.TypeDefinition;
      if (type.IsGeneric) {
        foreach (IGenericTypeParameter gpar in type.GenericParameters) {
          if (gpar.Name.Value.StartsWith(this.Name.Value, this.ignoreCase, CultureInfo.InvariantCulture))
            candidates.Add(gpar);
        }
      }

      //Ignore members if resolving a base type or interface expression
      if (typeDeclaration.OuterDummyBlock == this.ContainingBlock) {
        //This partial name is part of the signature of typeDeclaration. It should not be resolved using typeDeclaration.
        //Proceed to the outer type, if any, otherwise proceed to the namespace.
        NestedTypeDeclaration/*?*/ nestedTypeDeclaration = typeDeclaration as NestedTypeDeclaration;
        if (nestedTypeDeclaration != null)
          this.PopulateUsing(nestedTypeDeclaration.ContainingTypeDeclaration, candidates);
        else
          this.PopulateUsing(this.GetContainingNamespace(typeDeclaration), candidates);
        return;
      }

      //Look for member (including inherited members)
      if (type.IsGeneric) type = type.InstanceType.ResolvedType;
      this.PopulateUsing(type, candidates);

      //Look for outer declaration
      this.PopulateUsing(this.GetContainingNamespace(typeDeclaration), candidates);

    }

    /// <summary>
    /// Populates the given list with named entities that might bind to the partial name once completed,
    /// by using the scope chain of the given type definition.
    /// </summary>
    /// <param name="typeDefinition">The type whose members and inherited members are used to resolve this name.</param>
    /// <param name="candidates">A list of named entities to which candidates should be added.</param>
    protected virtual void PopulateUsing(ITypeDefinition typeDefinition, List<INamedEntity> candidates)
      //^ requires !typeDefinition.IsGeneric;
      //^ ensures result == null || result is ITypeGroup || (!restrictToNamespacesAndTypes && result is ITypeDefinitionMember);
    {
      if (typeDefinition is Dummy) return;
      foreach (ITypeDefinitionMember member in typeDefinition.Members) {
        if (!member.Name.Value.StartsWith(this.Name.Value, this.ignoreCase, CultureInfo.InvariantCulture)) continue;
        if (!this.ContainingBlock.ContainingTypeDeclaration.CanAccess(member)) continue;
        candidates.Add(member);
      }
      IEnumerable<ITypeReference> baseClasses = typeDefinition.BaseClasses;
      foreach (ITypeReference baseClassReference in baseClasses) {
        ITypeDefinition baseClass = baseClassReference.ResolvedType;
        if (baseClass.IsGeneric) baseClass = baseClass.InstanceType.ResolvedType;
        this.PopulateUsing(baseClass, candidates);
      }
      INestedTypeDefinition/*?*/ nestedTypeDefinition = typeDefinition as INestedTypeDefinition;
      if (nestedTypeDefinition != null) {
        ITypeDefinition containingTypeDefinition = nestedTypeDefinition.ContainingTypeDefinition;
        this.PopulateUsing(containingTypeDefinition, candidates);
      }
      if (TypeHelper.TypesAreEquivalent(typeDefinition, this.PlatformType.SystemObject)) return;
      this.PopulateUsing(this.PlatformType.SystemObject.ResolvedType, candidates);
    }

    /// <summary>
    /// Populates the given list with named entities that might bind to the partial name once completed,
    /// by using the scope chain of the given namespace declaration.
    /// </summary>
    /// <param name="namespaceDeclaration">The namespace to use to resolve this name.</param>
    /// <param name="candidates">A list of named entities to which candidates should be added.</param>
    protected virtual void PopulateUsing(NamespaceDeclaration namespaceDeclaration, List<INamedEntity> candidates)
      //^ ensures result == null || result is ITypeDefinition || result is INamespaceDefinition || result is ITypeGroup ||
      //^ (!restrictToNamespacesAndTypes && result is INamespaceMember);
    {
      foreach (var member in namespaceDeclaration.Aliases) {
        if (!member.Name.Value.StartsWith(this.Name.Value, this.ignoreCase, CultureInfo.InvariantCulture)) continue;
        candidates.Add(member);
      }
      foreach (var member in namespaceDeclaration.UnitSetAliases) {
        if (!member.Name.Value.StartsWith(this.Name.Value, this.ignoreCase, CultureInfo.InvariantCulture)) continue;
        candidates.Add(member);
      }
      IEnumerable<INamespaceMember> members;
      if (namespaceDeclaration.BusyResolvingAnAliasOrImport) {
        //Have to ignore using statements.
        members = namespaceDeclaration.UnitSetNamespace.Members;
      } else {
        //Consider types that were imported into the namespace via using statements
        members = namespaceDeclaration.Scope.Members;
      }
      foreach (var member in members) {
        if (!member.Name.Value.StartsWith(this.Name.Value, this.ignoreCase, CultureInfo.InvariantCulture)) continue;
        var nestedNamespaceDefinition = member as INamespaceDefinition;
        if (nestedNamespaceDefinition != null) {
          if (this.ContainingBlock.ContainingTypeDeclaration.CanAccess(nestedNamespaceDefinition))
            candidates.Add(nestedNamespaceDefinition);
          continue;
        }
        var namespaceTypeDefinition = member as INamespaceTypeDefinition;
        if (namespaceTypeDefinition != null && this.ContainingBlock.ContainingTypeDeclaration.CanAccess(namespaceTypeDefinition))
          candidates.Add(member);
      }
      NestedNamespaceDeclaration/*?*/ nestedNamespace = namespaceDeclaration as NestedNamespaceDeclaration;
      if (nestedNamespace != null) this.PopulateUsing(nestedNamespace.ContainingNamespaceDeclaration, candidates);
    }

    /// <summary>
    /// Returns this.Name.Value.
    /// </summary>
    //^ [Confined]
    public override string ToString() {
      return this.Name.Value;
    }

    /// <summary>
    /// Always return Dummy.Type.
    /// </summary>
    public sealed override ITypeDefinition Type {
      get { return Dummy.Type; }
    }

  }
  /// <summary>
  /// 
  /// </summary>
  public class PointerQualifiedName : QualifiedName {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="qualifier"></param>
    /// <param name="simpleName"></param>
    /// <param name="sourceLocation"></param>
    public PointerQualifiedName(Expression qualifier, SimpleName simpleName, ISourceLocation sourceLocation)
      : base(qualifier, simpleName, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected PointerQualifiedName(BlockStatement containingBlock, PointerQualifiedName template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      ITypeDefinitionMember/*?*/ resolvedMember = this.ResolveAsValueContainer(false) as ITypeDefinitionMember;
      if (resolvedMember == null) {
        ITypeDefinition/*?*/ qualifierType = this.Qualifier.Type;
        if (qualifierType != null && !(qualifierType is Dummy)) {
          IPointerTypeReference/*?*/ pqType = qualifierType as IPointerTypeReference;
          if (pqType != null) {
            string typeName = this.ContainingBlock.Helper.GetTypeName(pqType.TargetType.ResolvedType);
            this.ContainingBlock.Helper.ReportError(new AstErrorMessage(this, Error.NoSuchMember, typeName, this.SimpleName.Name.Value));
          } else {
            this.ContainingBlock.Helper.ReportError(new AstErrorMessage(this.Qualifier, Error.PointerExpected, RhsToStringForError()));
          }
        } else {
          if (!this.Qualifier.HasErrors) // qualifier is ok, but not of pointer type
            this.ContainingBlock.Helper.ReportError(new AstErrorMessage(this.Qualifier, Error.PointerExpected, RhsToStringForError()));
        }
        return true;
      } else
        return base.CheckForErrorsAndReturnTrueIfAnyAreFound();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    protected virtual string RhsToStringForError() {
      return "->" + this.SimpleName.Name.Value;
    }

    /// <summary>
    /// Either returns this expression, or returns a BlockExpression that assigns each subexpression to a temporary local variable
    /// and then evaluates an expression that is the same as this expression, but which refers to the temporaries rather than the 
    /// factored out subexpressions. This transformation is useful when expressing the semantics of operation assignments and increment/decrement operations.
    /// </summary>
    public override Expression FactoredExpression()
      //^^ ensures result == this || result is BlockExpression;
    {
      return this;
    }

    /// <summary>
    /// If the expression binds to an instance field then this property is not null and contains the instance.
    /// </summary>
    public override Expression/*?*/ Instance {
      get {
        ITypeDefinition/*?*/ qualifierType = this.Qualifier.Type;
        if (qualifierType is Dummy) return null;
        IPointerTypeReference/*?*/ ptr = qualifierType as IPointerTypeReference;
        if (ptr != null && ptr.TargetType.IsValueType)
          return this.Qualifier;
        AddressDereference deref = new AddressDereference(this.Qualifier, this.Qualifier.SourceLocation);
        deref.SetContainingExpression(this);
        return deref;
      }
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new PointerQualifiedName(containingBlock, this);
    }

    /// <summary>
    /// Finds a type member with this.SimpleName as its name. Runs up the base type chain if necessary. Returns strictly the first match.
    /// </summary>
    protected override ITypeDefinitionMember/*?*/ ResolveTypeMember(ITypeDefinition qualifyingType, bool ignoreAccessibility) {
      IPointerTypeReference/*?*/ pointerQualifyingType = qualifyingType as IPointerTypeReference;
      if (pointerQualifyingType == null) return null;
      return base.ResolveTypeMember(pointerQualifyingType.TargetType.ResolvedType, ignoreAccessibility);
    }

    //^ [Confined]
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public override string ToString() {
      return this.Qualifier.ToString() + "->" + this.SimpleName.ToString();
    }
  }

  /// <summary>
  /// An expression that calls a method indirectly via a function pointer.
  /// </summary>
  internal sealed class PointerCall : Expression, IPointerCall {

    /// <summary>
    /// Allocates an expression that calls a method indirectly via a function pointer.
    /// </summary>
    /// <param name="arguments">The arguments to pass to the method, after they have been converted to match the parameters of the method.</param>
    /// <param name="pointer">An expression that results at runtime in a function pointer that points to the actual method to call.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    internal PointerCall(IEnumerable<IExpression> arguments, IExpression pointer, ISourceLocation sourceLocation)
      : base(sourceLocation)
      //^ requires pointer.Type is IFunctionPointer;
    {
      this.arguments = arguments;
      this.pointer = pointer;
    }

    /// <summary>
    /// The arguments to pass to the method, after they have been converted to match the parameters of the method.
    /// </summary>
    public IEnumerable<IExpression> Arguments {
      get { return this.arguments; }
    }
    readonly IEnumerable<IExpression> arguments;

    /// <summary>
    /// Calls visitor.Visit(IPointerCall).
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// True if this method call terminates the calling method. It indicates that the calling method's stack frame is not required
    /// and can be removed before executing the call.
    /// </summary>
    public bool IsTailCall {
      get { return false; }
    }

    /// <summary>
    /// An expression that results at runtime in a function pointer that points to the actual method to call.
    /// </summary>
    public IExpression Pointer {
      get
        //^ ensures result.Type is IFunctionPointer;
      {
        return this.pointer;
      }
    }
    readonly IExpression pointer;
    //^ invariant pointer.Type is IFunctionPointer;


    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    public override Expression MakeCopyFor(BlockStatement containingBlock) {
      //^ assume false;
      return this;
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      return this;
    }

    /// <summary>
    /// The type of value that the expression will evaluate to, as determined at compile time.
    /// </summary>
    public override ITypeDefinition Type {
      get { return ((IFunctionPointerTypeReference)this.Pointer.Type).Type.ResolvedType; }
    }

    #region IExpression Members

    ITypeReference IExpression.Type {
      get { return this.Type; }
    }

    #endregion
  }

  /// <summary>
  /// An expression that denotes a pointer type.
  /// </summary>
  public class PointerTypeExpression : TypeExpression {

    /// <summary>
    /// Allocates an expression that denotes a pointer type.
    /// </summary>
    /// <param name="elementType">The type of value that the pointer points to.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public PointerTypeExpression(TypeExpression elementType, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.elementType = elementType;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected PointerTypeExpression(BlockStatement containingBlock, PointerTypeExpression template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.elementType = (TypeExpression)template.elementType.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// </summary>
    /// <returns></returns>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return this.elementType.HasErrors;
    }

    /// <summary>
    /// Calls the visitor.Visit(PointerTypeExpression) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The type of value that the pointer points to.
    /// </summary>
    public TypeExpression ElementType {
      get { return this.elementType; }
    }
    readonly TypeExpression elementType;

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new PointerTypeExpression(containingBlock, this);
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      //^ assume false;
      return new DummyExpression(this.SourceLocation);
    }

    /// <summary>
    /// Returns the type denoted by the expression. If expression cannot be resolved, a dummy type is returned. If the expression is ambiguous the first matching type is returned.
    /// If the expression does not resolve to exactly one type, an error is added to the error collection of the compilation context.
    /// </summary>
    protected override ITypeDefinition Resolve() {
      return PointerType.GetPointerType(this.ElementType.ResolvedType, this.Compilation.HostEnvironment.InternFactory);
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingExpression(Expression containingExpression) {
      base.SetContainingExpression(containingExpression);
      this.elementType.SetContainingExpression(this);
    }

  }

  /// <summary>
  /// An expression that Populates a newly constructed collection by adding zero or more elements to it via ICollection.Add.
  /// </summary>
  public class PopulateCollection : Expression {

    /// <summary>
    /// Allocates an expression that Populates a newly constructed collection by adding zero or more elements to it via ICollection.Add.
    /// </summary>
    /// <param name="collectionToPopulate">The newly constructed collection to populate.</param>
    /// <param name="elementValues">A list of expressions that result in values that are added to the constructed object via calls to ICollection.Add.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public PopulateCollection(Expression/*?*/ collectionToPopulate, IEnumerable<Expression> elementValues, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.collectionToPopulate = collectionToPopulate;
      this.elementValues = elementValues;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected PopulateCollection(BlockStatement containingBlock, PopulateCollection template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      if (template.collectionToPopulate != null)
        this.collectionToPopulate = template.collectionToPopulate.MakeCopyFor(containingBlock);
      this.elementValues = Expression.CopyExpressions(template.elementValues, containingBlock);
    }

    /// <summary>
    /// The newly constructed collection to populate.
    /// </summary>
    public Expression CollectionToPopulate {
      get {
        //^ assume this.collectionToPopulate != null;
        return this.collectionToPopulate;
      }
    }
    Expression/*?*/ collectionToPopulate;

    /// <summary>
    /// Calls the visitor.Visit(PopulateCollection) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// A list of expressions that result in values that are added to the constructed object via calls to ICollection.Add.
    /// </summary>
    public IEnumerable<Expression> ElementValues {
      get { return this.elementValues; }
    }
    readonly IEnumerable<Expression> elementValues;

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (this.ContainingBlock == containingBlock) return this;
      return new PopulateCollection(containingBlock, this);
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      return new DummyExpression(this.SourceLocation); //TODO: implement this
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingExpression(Expression containingExpression) {
      base.SetContainingExpression(containingExpression);
      if (this.collectionToPopulate != null)
        this.collectionToPopulate.SetContainingExpression(this);
    }

    /// <summary>
    /// The type of value that the expression will evaluate to, as determined at compile time.
    /// </summary>
    public override ITypeDefinition Type {
      get { return Dummy.Type; }
    }

  }

  /// <summary>
  /// An expression that subtracts one from the arithmetic value at the location denoted by the target expression, but results in the value at the location before the addition was performed.
  /// When overloaded, this expression corresponds to a call to op_Decrement.
  /// </summary>
  public class PostfixDecrement : PostfixUnaryOperationAssignment {

    /// <summary>
    /// Allocates an expression that subtracts one from the arithmetic value at the location denoted by the target expression, but results in the value at the location before the addition was performed.
    /// When overloaded, this expression corresponds to a call to op_Decrement.
    /// </summary>
    /// <param name="target">The value on which the operation is performed.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public PostfixDecrement(TargetExpression target, ISourceLocation sourceLocation)
      : base(target, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected PostfixDecrement(BlockStatement containingBlock, PostfixDecrement template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Calls the visitor.Visit(PostfixDecrement) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Returns the name a method must have to overload this operator.
    /// </summary>
    protected override IName GetOperatorName() {
      return this.NameTable.OpDecrement;
    }

    /// <summary>
    /// Returns the string used to identify the operator in error messages
    /// </summary>
    protected override string OperationSymbolForErrorMessage {
      get { return "--"; }
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new PostfixDecrement(containingBlock, this);
    }

  }

  /// <summary>
  /// An expression that adds one to the arithmetic value at the location denoted by the target expression, but results in the value at the location before the addition was performed.
  /// When overloaded, this expression corresponds to a call to op_Increment.
  /// </summary>
  public class PostfixIncrement : PostfixUnaryOperationAssignment {

    /// <summary>
    /// Allocates an expression that adds one to the arithmetic value at the location denoted by the target expression, but results in the value at the location before the addition was performed.
    /// When overloaded, this expression corresponds to a call to op_Increment.
    /// </summary>
    /// <param name="target">The value on which the operation is performed.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public PostfixIncrement(TargetExpression target, ISourceLocation sourceLocation)
      : base(target, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected PostfixIncrement(BlockStatement containingBlock, PostfixIncrement template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Calls the visitor.Visit(PostfixIncrement) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Returns the name a method must have to overload this operator.
    /// </summary>
    protected override IName GetOperatorName() {
      return this.NameTable.OpIncrement;
    }

    /// <summary>
    /// Returns the string used to identify the operator in error messages
    /// </summary>
    protected override string OperationSymbolForErrorMessage {
      get { return "++"; }
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new PostfixIncrement(containingBlock, this);
    }

  }

  /// <summary>
  /// An expression that performs an operation on the value at the location denoted by the target expression and results in the new value of the target.
  /// </summary>
  public abstract class PostfixUnaryOperationAssignment : UnaryOperationAssignment {

    /// <summary>
    /// Initializes an operation performed on a single operand and that also assigns a new value to the memory location represented by the expression.
    /// </summary>
    /// <param name="target">The value on which the operation is performed.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    protected PostfixUnaryOperationAssignment(TargetExpression target, ISourceLocation sourceLocation)
      : base(target, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected PostfixUnaryOperationAssignment(BlockStatement containingBlock, PostfixUnaryOperationAssignment template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      if (this.cachedProjection != null) return this.cachedProjection;
      MethodCall/*?*/ overloadedMethodCall = this.OverloadMethodCall;
      List<Statement> statements = new List<Statement>();
      Expression expression = this.Target.Expression;
      Expression factored = expression.FactoredExpression();
      if (factored != expression) {
        //^ assume factored is BlockExpression; //the post condition of FactoredExpression says so
        BlockExpression be = (BlockExpression)factored;
        statements.AddRange(be.BlockStatement.Statements);
        expression = be.Expression;
      }
      LocalDefinition originalValueVar = Expression.CreateInitializedLocalDeclarationAndAddDeclarationsStatementToList(expression, statements);
      BoundExpression originalValue = new BoundExpression(this.Target.Expression, originalValueVar);
      TargetExpression target = new TargetExpression(expression);
      Expression source;
      if (overloadedMethodCall != null && !(overloadedMethodCall.ResolvedMethod is BuiltinMethodDefinition)) {
        BlockExpression/*?*/ be = factored as BlockExpression;
        if (be != null)
          source = this.FactoredOverloadCall(originalValue, overloadedMethodCall.ResolvedMethod);
        else
          source = overloadedMethodCall;
        statements.Add(new ExpressionStatement(new Assignment(target, source, this.SourceLocation), this.SourceLocation));
      } else {
        // For the postfix case we must make another temporary to hold the 
        // initial value of the target, while the target is being incremented/decremented.
        source = NewAssignUsingTemporary(target, this, originalValue);
        statements.Add(new ExpressionStatement(source, this.SourceLocation));
      }
      BlockStatement block = new BlockStatement(statements, this.SourceLocation);
      BlockExpression bexpr = new BlockExpression(block, originalValue, this.SourceLocation);
      bexpr.SetContainingExpression(this);
      return this.cachedProjection = bexpr.ProjectAsIExpression();
    }
    IExpression/*?*/ cachedProjection;

    private Expression NewAssignUsingTemporary(TargetExpression target, PostfixUnaryOperationAssignment parent, BoundExpression temporary) {
      Expression source;
      object one = GetConstantOneOfMatchingTypeForIncrementDecrement(target.Type);
      CompileTimeConstant delta = new CompileTimeConstant(one, SourceDummy.SourceLocation);
      if (parent is PostfixDecrement)
        source = new Subtraction(temporary, delta, parent.SourceLocation);
      else
        source = new Addition(temporary, delta, parent.SourceLocation);
      return new Assignment(target, source, parent.SourceLocation);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="visitor"></param>
    public virtual void VisitAsUnaryOperationAssignment(ICodeVisitor visitor) {
      MethodCall/*?*/ overloadedMethodCall = this.OverloadMethodCall;
      Expression expression = this.Target.Expression;
      Expression factored = expression.FactoredExpression();
      if (factored != expression) {
        //^ assume factored is BlockExpression; //the post condition of FactoredExpression says so
        BlockExpression be = (BlockExpression)factored;
        be.BlockStatement.Dispatch(visitor);
        expression = be.Expression;
      }
      TargetExpression target = new TargetExpression(expression);
      Expression source;
      ExpressionStatement assignmentStatement;
      if (overloadedMethodCall != null && !(overloadedMethodCall.ResolvedMethod is BuiltinMethodDefinition)) {
        BlockExpression/*?*/ be = factored as BlockExpression;
        if (be != null)
          source = this.FactoredOverloadCall(expression, overloadedMethodCall.ResolvedMethod);
        else
          source = overloadedMethodCall;
        assignmentStatement = new ExpressionStatement(new Assignment(target, source, this.SourceLocation), this.SourceLocation);
      } else {
        object one = GetConstantOneOfMatchingTypeForIncrementDecrement(target.Type);
        if (this is PostfixDecrement)
          source = new SubtractionAssignment(target, new CompileTimeConstant(one, SourceDummy.SourceLocation), this.SourceLocation);
        else
          source = new AdditionAssignment(target, new CompileTimeConstant(one, SourceDummy.SourceLocation), this.SourceLocation);
        assignmentStatement = new ExpressionStatement(source, this.SourceLocation);
      }
      assignmentStatement.SetContainingBlock(this.ContainingBlock);
      assignmentStatement.Dispatch(visitor);
    }

  }

  /// <summary>
  /// An expression that subtracts one from the arithmetic value at the location denoted by the target expression and results in the decremented value.
  /// When overloaded, this expression corresponds to a call to op_Decrement.
  /// </summary>
  public class PrefixDecrement : PrefixUnaryOperationAssignment {

    /// <summary>
    /// Allocates an expression that subtracts one from the arithmetic value at the location denoted by the target expression and results in the decremented value.
    /// When overloaded, this expression corresponds to a call to op_Decrement.
    /// </summary>
    /// <param name="target">The value on which the operation is performed.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public PrefixDecrement(TargetExpression target, ISourceLocation sourceLocation)
      : base(target, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected PrefixDecrement(BlockStatement containingBlock, PrefixDecrement template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Calls the visitor.Visit(PrefixDecrement) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Returns the name a method must have to overload this operator.
    /// </summary>
    protected override IName GetOperatorName() {
      return this.NameTable.OpDecrement;
    }

    /// <summary>
    /// Returns the string used to identify the operator in error messages
    /// </summary>
    protected override string OperationSymbolForErrorMessage {
      get { return "--"; }
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new PrefixDecrement(containingBlock, this);
    }

  }

  /// <summary>
  /// An expression that adds one to the arithmetic value at the location denoted by the target expression and results in the incremented value.
  /// When overloaded, this expression corresponds to a call to op_Increment.
  /// </summary>
  public class PrefixIncrement : PrefixUnaryOperationAssignment {

    /// <summary>
    /// Allocates an expression that adds one to the arithmetic value at the location denoted by the target expression and results in the incremented value.
    /// When overloaded, this expression corresponds to a call to op_Increment.
    /// </summary>
    /// <param name="target">The value on which the operation is performed.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public PrefixIncrement(TargetExpression target, ISourceLocation sourceLocation)
      : base(target, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected PrefixIncrement(BlockStatement containingBlock, PrefixIncrement template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Calls the visitor.Visit(PrefixIncrement) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Returns the name a method must have to overload this operator.
    /// </summary>
    protected override IName GetOperatorName() {
      return this.NameTable.OpIncrement;
    }

    /// <summary>
    /// Returns the string used to identify the operator in error messages
    /// </summary>
    protected override string OperationSymbolForErrorMessage {
      get { return "++"; }
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new PrefixIncrement(containingBlock, this);
    }

  }

  /// <summary>
  /// An expression that performs an operation on the value at the location denoted by the target expression and results in the new value of the target.
  /// </summary>
  public abstract class PrefixUnaryOperationAssignment : UnaryOperationAssignment {

    /// <summary>
    /// Initializes an operation performed on a single operand and that also assigns a new value to the memory location represented by the expression.
    /// </summary>
    /// <param name="target">The value on which the operation is performed.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    protected PrefixUnaryOperationAssignment(TargetExpression target, ISourceLocation sourceLocation)
      : base(target, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected PrefixUnaryOperationAssignment(BlockStatement containingBlock, PrefixUnaryOperationAssignment template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      if (this.cachedProjection != null) return this.cachedProjection;
      MethodCall/*?*/ overloadedMethodCall = this.OverloadMethodCall;
      if (overloadedMethodCall != null && !(overloadedMethodCall.ResolvedMethod is BuiltinMethodDefinition)) {
        Expression factored = this.Target.Expression.FactoredExpression();
        if (factored == this.Target.Expression) {
          Expression result = new Assignment(this.Target, overloadedMethodCall, this.SourceLocation);
          result.SetContainingExpression(this);
          return this.cachedProjection = result.ProjectAsIExpression();
        } else {
          //^ assume factored is BlockExpression; //the post condition of FactoredExpression says so
          BlockExpression be = (BlockExpression)factored;
          TargetExpression target = new TargetExpression(be.Expression);
          Assignment assignment = new Assignment(target, this.FactoredOverloadCall(be.Expression, overloadedMethodCall.ResolvedMethod), this.SourceLocation);
          assignment.SetContainingExpression(be);
          be.expression = assignment;
          return this.cachedProjection = be.ProjectAsIExpression();
        }
      } else {
        //TODO: factor this out into a virtual method.
        object one = GetConstantOneOfMatchingTypeForIncrementDecrement(this.Target.Type);
        Expression assignment;
        if (this is PrefixDecrement)
          assignment = new SubtractionAssignment(this.Target, new CompileTimeConstant(one, SourceDummy.SourceLocation), this.SourceLocation);
        else
          assignment = new AdditionAssignment(this.Target, new CompileTimeConstant(one, SourceDummy.SourceLocation), this.SourceLocation);
        assignment.SetContainingExpression(this);
        return this.cachedProjection = assignment.ProjectAsIExpression();
      }
    }
    IExpression/*?*/ cachedProjection;

  }

  /// <summary>
  /// An expression that binds to the last parameter of a property setter.
  /// </summary>
  public class PropertySetterValue : Expression {

    /// <summary>
    /// Allocates an expression that binds to the last parameter of a property setter.
    /// </summary>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public PropertySetterValue(ISourceLocation sourceLocation)
      : base(sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected PropertySetterValue(BlockStatement containingBlock, PropertySetterValue template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Calls the visitor.Visit(PropertySetterValue) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new PropertySetterValue(containingBlock, this);
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      return new DummyExpression(this.SourceLocation); //TODO: return binding to last param of setter
    }

    /// <summary>
    /// The type of value that the expression will evaluate to, as determined at compile time.
    /// </summary>
    public override ITypeDefinition Type {
      get { return Dummy.Type; }
    }

  }

  /// <summary>
  /// A name that is meaningful with respect to an object specified by the qualifier expression.
  /// </summary>
  public class QualifiedName : Expression {

    /// <summary>
    /// Allocates a name that is meaningful with respect to an object specified by the qualifier expression.
    /// </summary>
    /// <param name="qualifier">An expression that results in the qualifier object.</param>
    /// <param name="simpleName">A simple name that is resolved with respect to the object that the qualifier expression results in.</param>
    /// <param name="sourceLocation">The source location of the qualifier plus name.</param>
    public QualifiedName(Expression qualifier, SimpleName simpleName, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.qualifier = qualifier;
      this.simpleName = simpleName;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected QualifiedName(BlockStatement containingBlock, QualifiedName template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
    {
      this.qualifier = template.qualifier.MakeCopyFor(containingBlock);
      this.simpleName = (SimpleName)template.simpleName.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      ITypeDefinitionMember/*?*/ resolvedMember = this.ResolveAsValueContainer(false);
      if (resolvedMember == null) {
        if (this.Qualifier.HasErrors) return true;
        resolvedMember = this.ResolveAsValueContainer(true);
      }
      if (resolvedMember == null) {
        ITypeDefinition qualifierType = this.Qualifier.Type;
        string qualifier;
        if (qualifierType is Dummy)
          qualifier = this.Qualifier.SourceLocation.Source;
        else
          qualifier = this.ContainingBlock.Helper.GetTypeName(qualifierType);
        this.ContainingBlock.Helper.ReportError(new AstErrorMessage(this, Error.NoSuchMember, qualifier, this.SimpleName.Name.Value));
        return true;
      } else {
        if (!this.ContainingBlock.ContainingTypeDeclaration.CanAccess(resolvedMember)) {
          var relatedLocations = this.ContainingBlock.Helper.GetRelatedLocations(resolvedMember);
          this.ContainingBlock.Helper.ReportError(new AstErrorMessage(this, Error.InaccessibleTypeMember, relatedLocations,
            MemberHelper.GetMemberSignature(resolvedMember, NameFormattingOptions.None)));
          return true;
        } else
          return false;
      }
    }

    /// <summary>
    /// Either returns this expression, or returns a BlockExpression that assigns each subexpression to a temporary local variable
    /// and then evaluates an expression that is the same as this expression, but which refers to the temporaries rather than the 
    /// factored out subexpressions. This transformation is useful when expressing the semantics of operation assignments and increment/decrement operations.
    /// </summary>
    public override Expression FactoredExpression()
      //^^ ensures result == this || result is BlockExpression;
    {
      SimpleName simpleQualifier = this.Qualifier as SimpleName;
      if (simpleQualifier != null && (simpleQualifier.ResolvesToLocalOrParameter || !simpleQualifier.Type.IsReferenceType))
        return this;
      else if (!this.Qualifier.Type.IsReferenceType) {
        // If Qualifier denotes a value-type we cannot use that as the cached 
        // value because we would end up mutating a *copy* rather than the target.
        // Thus we must recurse in case there are side-effects nested in Qualifier.
        Expression factored = this.Qualifier.FactoredExpression();
        if (factored != this.Qualifier) {
          // Suppose Qualifier was "SomeExpression.ValueField". 
          // Then: factored might be a BlockExpression with block "temp = SomeExpression", 
          // and the expression will be QualifiedName denoting "temp.ValueField".
          // In this case we want to return the same block, with expression "temp.ValueField.SimpleNameOfThis".
          BlockExpression bExp = (BlockExpression)factored; // Asserted by postcondition
          QualifiedName aliasName = new QualifiedName(bExp.Expression, this.SimpleName, this.SourceLocation);
          BlockExpression result = new BlockExpression(bExp.BlockStatement, aliasName, this.sourceLocation);
          result.SetContainingExpression(this);
          return result;
        } else
          return this;
      } else {
        // In this case Qualifier is a reference to the target object,
        // and the cached factor will be a safe alias to the target.
        Expression objectRef = this.Qualifier;
        List<Statement> statements = new List<Statement>();
        LocalDefinition cachedQualifier =
            Expression.CreateInitializedLocalDeclarationAndAddDeclarationsStatementToList(objectRef, statements);
        BoundExpression cachedExpression = new BoundExpression(objectRef, cachedQualifier);
        QualifiedName aliasName = new QualifiedName(cachedExpression, this.SimpleName, this.SourceLocation);
        BlockStatement block = new BlockStatement(statements, this.SourceLocation);
        BlockExpression result = new BlockExpression(block, aliasName, this.SourceLocation);
        result.SetContainingExpression(this);
        return result;
      }
    }


    /// <summary>
    /// Calls the visitor.Visit(QualifiedName) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Computes the compile time value of the expression. Can be null.
    /// </summary>
    protected override object/*?*/ GetValue() {
      ITypeDefinitionMember/*?*/ container = this.ResolveAsValueContainer(false);
      if (container != null) {
        IFieldDefinition/*?*/ field = container as IFieldDefinition;
        if (field != null) {
          if (this.Helper.UseCompileTimeValueOfField(field)) {
            object/*?*/ result = field.CompileTimeValue.Value;
            if (result != null) return result;
          }
        }
      }
      return null;
    }

    /// <summary>
    /// Checks if the expression has a side effect and reports an error unless told otherwise.
    /// </summary>
    /// <param name="reportError">If true, report an error if the expression has a side effect.</param>
    public override bool HasSideEffect(bool reportError) {
      if (this.Instance != null)
        return this.Instance.HasSideEffect(reportError);
      else
        return false;
      //TODO: if this is a property getter, check the getter for side effects
    }

    /// <summary>
    /// Infers the type of value that this expression will evaluate to. At runtime the actual value may be an instance of subclass of the result of this method.
    /// Calling this method does not cache the computed value and does not generate any error messages. In some cases, such as references to the parameters of lambda
    /// expressions during type overload resolution, the value returned by this method may be different from one call to the next.
    /// When type inference fails, Dummy.Type is returned.
    /// </summary>
    public override ITypeDefinition InferType() {
      ITypeDefinitionMember/*?*/ boundDefinition = this.ResolveAsValueContainer(false);
      IEventDefinition/*?*/ eventDef = boundDefinition as IEventDefinition;
      if (eventDef != null) return eventDef.Type.ResolvedType;
      IFieldDefinition/*?*/ field = boundDefinition as IFieldDefinition;
      if (field != null) return field.Type.ResolvedType;
      IPropertyDefinition/*?*/ property = boundDefinition as IPropertyDefinition;
      if (property != null) return property.Type.ResolvedType;
      //^ assert boundDefinition == null;
      return Dummy.Type;
    }

    /// <summary>
    /// 
    /// </summary>
    public virtual Expression/*?*/ Instance {
      get {
        ITypeDefinition/*?*/ qualifierType = this.Qualifier.Type;
        if (qualifierType is Dummy) return null;
        return this.Qualifier;
      }
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new QualifiedName(containingBlock, this);
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      if (this.cachedProjection != null) return this.cachedProjection;
      ITypeDefinitionMember/*?*/ boundDefinition = this.ResolveAsValueContainer(false);
      IFieldDefinition/*?*/ field = boundDefinition as IFieldDefinition;
      if (field != null && this.Helper.UseCompileTimeValueOfField(field)) return this.cachedProjection = CompileTimeConstant.For(field.CompileTimeValue, this);
      IPropertyDefinition/*?*/ property = boundDefinition as IPropertyDefinition;
      if (property != null) {
        Expression e;
        if (property.Getter != null) {
          if (property.Getter.ResolvedMethod.IsStatic)
            e = new ResolvedMethodCall(property.Getter.ResolvedMethod, new List<Expression>(0), this.SourceLocation);
          else
            e = new ResolvedMethodCall(property.Getter.ResolvedMethod, this.Qualifier, new List<Expression>(0), this.SourceLocation);
        } else 
          e = new DummyExpression(this.SourceLocation);
        e.SetContainingExpression(this);
        return this.cachedProjection = (IExpression)e;
      }
      if (boundDefinition is IEventDefinition) return this.cachedProjection = new DummyExpression(this.SourceLocation); //TODO: is this right?
      if (boundDefinition == null) boundDefinition = Dummy.Field;
      BoundExpression be = new BoundExpression(this, boundDefinition);
      be.SetContainingExpression(this);
      return this.cachedProjection = be;
    }
    IExpression/*?*/ cachedProjection;

    /// <summary>
    /// An expression that binds to an object or a type or a namespace, or something else that can serve to constrain what the name binds to.
    /// </summary>
    public Expression Qualifier {
      get { return this.qualifier; }
    }
    readonly Expression qualifier;

    /// <summary>
    /// Returns either null or the type member that binds to this name using the compile time type of the Qualifier as resolution context.
    /// Method groups are represented by the first matching method definition in the most derived type that defines or overrides methods in the group. 
    /// Likewise for property groups and type groups.
    /// </summary>
    /// <param name="ignoreAccessibility">Set this to true when the qualified name has failed to resolve and an appropriate error has to be
    /// generated. In particular, if the qualified name can resolve to an inaccessible member, the error will say as much.</param>
    public virtual ITypeDefinitionMember/*?*/ Resolve(bool ignoreAccessibility) {
      var result = this.resolvedMember;
      if (result == null || ignoreAccessibility) {
        ITypeDefinition/*?*/ qualifierType = this.Qualifier.Type;
        if (qualifierType is Dummy)
          qualifierType = this.ResolveAsType(this.ResolveQualifierAsNamespaceOrType());
        if (!(qualifierType is Dummy) && qualifierType != null)
          result = this.ResolveTypeMember(qualifierType, ignoreAccessibility);
        if (ignoreAccessibility) return result;
        if (result == null)
          this.resolvedMember = Dummy.Method;
      }
      if (result != null && !(result is Dummy)) this.hasErrors = false; //If the qualified name resolved, it is error free and need not be checked for errors.
      return result;
    }
    ITypeDefinitionMember/*?*/ resolvedMember;

    /// <summary>
    /// Returns either null or the namespace or type group that binds to this name after resolving the Qualifier as a namespace or type.
    /// </summary>
    public object/*?*/ ResolveAsNamespaceOrTypeGroup(bool reportError = false)
      //^ ensures result == null || result is INamespaceDefinition || result is ITypeGroup;
    {
      object/*?*/ resolvedQualifier = this.ResolveQualifierAsNamespaceOrType(reportError);
      ITypeDefinition/*?*/ qualifyingType = this.ResolveAsType(resolvedQualifier);
      if (qualifyingType != null) {
        if (!this.ContainingBlock.ContainingTypeDeclaration.CanAccess(qualifyingType))
          resolvedQualifier = this.ResolveQualifierAsNamespace(reportError);
        else {
          INestedTypeDefinition/*?*/ resolvedTypeMember = this.ResolveTypeMember(qualifyingType, false) as INestedTypeDefinition;
          if (resolvedTypeMember != null) {
            this.hasErrors = false;
            return new NestedTypeGroup(this, resolvedTypeMember.ContainingTypeDefinition, this.SimpleName);
          }
        }
      }
      INamespaceDefinition/*?*/ qualifyingNamespace = resolvedQualifier as INamespaceDefinition;
      if (qualifyingNamespace != null) {
        //Search namespace for nested type or namespace, preferring the type over the namespace if both are present.
        INamespaceMember/*?*/ resolvedNamespaceMember = null;
        foreach (INamespaceMember nsMember in qualifyingNamespace.GetMembersNamed(this.SimpleName.Name, this.SimpleName.IgnoreCase)) {
          INamespaceTypeDefinition/*?*/ resolvedNamespaceType = nsMember as INamespaceTypeDefinition;
          if (resolvedNamespaceType != null) {
            this.hasErrors = false;
            return new NamespaceTypeGroup(this, resolvedNamespaceType.ContainingScope, this.SimpleName);
          }
          if (nsMember is INamespaceDefinition) resolvedNamespaceMember = nsMember;
        }
        //^ assume resolvedNamespaceMember == null || resolvedNamespaceMember is INamespaceDefinition; //follows from logic above
        if (resolvedNamespaceMember != null) {
          this.hasErrors = false;
          return resolvedNamespaceMember;
        }
      }
      if (reportError) {
        this.Helper.ReportError(new AstErrorMessage(this, Error.TypeNameNotFound, this.SimpleName.Name.Value, this.Qualifier.SourceLocation.Source));
        this.hasErrors = true;
      }
      return null;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="resolvedQualifier"></param>
    /// <returns></returns>
    public ITypeDefinition/*?*/ ResolveAsType(object/*?*/ resolvedQualifier) {
      ITypeDefinition/*?*/ qualifyingType = null;
      ITypeGroup/*?*/ qualifyingTypeGroup = resolvedQualifier as ITypeGroup;
      if (qualifyingTypeGroup != null) {
        foreach (ITypeDefinition qtCandidate in qualifyingTypeGroup.Types) {
          if (qtCandidate.IsGeneric) continue;
          if (qualifyingType == null) qualifyingType = qtCandidate; //else TODO: error about ambiguous type
        }
      } else
        qualifyingType = resolvedQualifier as ITypeDefinition;
      if (qualifyingType != null) this.hasErrors = false;
      return qualifyingType;
    }

    /// <summary>
    /// Returns the event, field or property that binds to this qualified name using the applicable scope chain and compilation context.
    /// Returns null if qualified name does not bind to one of the above cases.
    /// </summary>
    public virtual ITypeDefinitionMember/*?*/ ResolveAsValueContainer(bool ignoreAccessibility)
      //^ ensures result == null || result is IEventDefinition || result is IFieldDefinition || result is IPropertyDefinition;
    {
      ITypeDefinitionMember/*?*/ result = this.Resolve(ignoreAccessibility);
      this.hasErrors = null;
      IPropertyDefinition/*?*/ property = result as IPropertyDefinition;
      if (property != null && property.Parameters.GetEnumerator().MoveNext()) {
        //TODO: error, indexer not expected here
        //TODO: C# does not allow indexers and properties to have the same name, but some other language could. The language specification does not spell out
        //what should happen in such a case. Need to check up what the original C# compiler does.
        return null;
      }
      if (!(result is IEventDefinition || result is IFieldDefinition || result is IPropertyDefinition || result is INestedTypeDefinition)) {
        if (result == null) {
          if (this.Qualifier.HasErrors) return null;
        }
        //TODO: error, result not expected here
        return null;
      }
      return result;
    }

    /// <summary>
    /// Resolves the qualifier as a namespace, provided that it is a simple name. Returns null if this is not possible.
    /// </summary>
    private INamespaceDefinition/*?*/ ResolveQualifierAsNamespace(bool reportError = false) {
      SimpleName/*?*/ simpleName = this.Qualifier as SimpleName;
      if (simpleName != null) return simpleName.ResolveAsNamespace();
      return null;
    }

    /// <summary>
    /// Resolves the qualifier as a namespace or as a type. Returns null if this is not possible.
    /// </summary>
    public object/*?*/ ResolveQualifierAsNamespaceOrType(bool reportError = false)
      //^ ensures result == null || result is INamespaceDefinition || result is ITypeDefinition || result is ITypeGroup;
    {
      SimpleName/*?*/ simpleName = this.Qualifier as SimpleName;
      if (simpleName != null) return simpleName.ResolveAsNamespaceOrType(reportError);
      QualifiedName/*?*/ qualifiedName = this.Qualifier as QualifiedName;
      if (qualifiedName != null) return qualifiedName.ResolveAsNamespaceOrTypeGroup(reportError);
      AliasQualifiedName/*?*/ aliasQualifiedName = this.Qualifier as AliasQualifiedName;
      if (aliasQualifiedName != null) return aliasQualifiedName.ResolveAsNamespaceOrType();
      GenericInstanceExpression/*?*/ genericInstanceExpression = this.Qualifier as GenericInstanceExpression;
      if (genericInstanceExpression != null) return genericInstanceExpression.ResolveAsGenericTypeInstance();
      //In C#, no other kind of expression will resolve to a namespace or type
      //TODO: perhaps IExpression should support a ResolveAsNamespaceOrType method? That would be more extensible.
      return null;
    }

    /// <summary>
    /// Finds a type member with this.SimpleName as its name. Runs up the base type chain if necessary. Returns strictly the first match.
    /// </summary>
    protected virtual ITypeDefinitionMember/*?*/ ResolveTypeMember(ITypeDefinition qualifyingType, bool ignoreAccessibility) {
      foreach (ITypeDefinitionMember member in qualifyingType.GetMembersNamed(this.SimpleName.Name, this.SimpleName.IgnoreCase)) {
        if (ignoreAccessibility || this.ContainingBlock.ContainingTypeDeclaration.CanAccess(member)) return member;
      }
      foreach (ITypeReference baseClass in qualifyingType.BaseClasses) {
        ITypeDefinitionMember/*?*/ result = this.ResolveTypeMember(baseClass.ResolvedType, ignoreAccessibility);
        if (result != null) return result;
      }
      if (qualifyingType.IsInterface) {
        foreach (ITypeReference iface in qualifyingType.Interfaces) {
          ITypeDefinitionMember/*?*/ result = this.ResolveTypeMember(iface.ResolvedType, ignoreAccessibility);
          if (result != null) return result;
        }
      }
      foreach (ITypeDefinitionMember member in this.Helper.GetExtensionMembers(qualifyingType, this.SimpleName.Name, this.SimpleName.IgnoreCase)) {
        if (ignoreAccessibility || this.ContainingBlock.ContainingTypeDeclaration.CanAccess(member)) return member;
      }
      return null;
    }

    /// <summary>
    /// Completes the two stage construction of the object. This allows bottom up parsers to construct a QualifiedName before constructing the containing Block.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itslef should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingExpression(Expression containingExpression) {
      base.SetContainingExpression(containingExpression);
      this.qualifier.SetContainingExpression(this);
      this.simpleName.SetContainingExpression(this);
    }

    /// <summary>
    /// The name of an entity that is associated with the qualifier object.
    /// </summary>
    public SimpleName SimpleName {
      get { return this.simpleName; }
    }
    readonly SimpleName simpleName;

    //^ [Confined]
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public override string ToString() {
      return this.Qualifier.ToString() + "." + this.SimpleName.ToString();
    }

    /// <summary>
    /// The type of value this qualified name will evaluate to, as determined at compile time. 
    /// If the qualified name does not represent an expression that results in a value, Dummy.Type is returned.
    /// </summary>
    public sealed override ITypeDefinition Type {
      get {
        if (this.type == null)
          this.type = this.InferType();
        return this.type;
      }
    }
    //^ [Once]
    ITypeDefinition/*?*/ type;

  }

  /// <summary>
  /// An expression that repeatedly evaluates an embedded parameterized condition by binding a succession of values to the parameters and reduces the collection of results to 
  /// a single boolen value. For example: forall and exists.
  /// </summary>
  public abstract class Quantifier : Expression {

    /// <summary>
    /// Allocates an expression that repeatedly evaluates an embedded parameterized condition by binding a succession of values to the parameters and reduces the collection of results to 
    /// a single boolen value. For example: forall and exists.
    /// </summary>
    /// <param name="boundVariables">
    /// One of more local declarations statements. Typically these locals are referenced in this.Condition.
    /// </param>
    /// <param name="condition">
    /// An expression that is evaluated for every tuple of values that may be bound to the variables defined by this.BoundVariables.
    /// Typically, the expression contains references to the variables defined in this.BoundVariables.
    /// </param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    protected Quantifier(List<LocalDeclarationsStatement> boundVariables, Expression condition, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.boundVariables = boundVariables;
      this.condition = condition;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected Quantifier(BlockStatement containingBlock, Quantifier template)
      : base(containingBlock, template) {
      this.boundVariables = new List<LocalDeclarationsStatement>(template.BoundVariables);
      List<Statement> statements = new List<Statement>(1);
      BlockStatement dummyBlock = new BlockStatement(statements, this.SourceLocation);
      dummyBlock.SetContainingBlock(containingBlock);
      this.condition = template.condition.MakeCopyFor(dummyBlock);
      foreach (LocalDeclarationsStatement locDecls in this.BoundVariables) statements.Add(locDecls);
    }

    /// <summary>
    /// One of more variables of a the same type. Typically these variables are referenced in this.Condition.
    /// The Forall expression is true if this.Condition is true for every possible binding of values to these variables.
    /// </summary>
    public IEnumerable<LocalDeclarationsStatement> BoundVariables {
      get {
        for (int i = 0, n = this.boundVariables.Count; i < n; i++)
          yield return this.boundVariables[i] = (LocalDeclarationsStatement)this.boundVariables[i].MakeCopyFor(this.ContainingBlock);
      }
    }
    readonly List<LocalDeclarationsStatement> boundVariables;

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      bool result = false;
      foreach (LocalDeclarationsStatement decl in this.BoundVariables)
        result |= decl.HasErrors;
      result |= this.ConvertedCondition.HasErrors;
      result |= this.HasSideEffect(true);
      return result;
    }

    /// <summary>
    /// An expression that is evaluated for every tuple of values that may be bound to the variables defined by this.BoundVariables.
    /// If the expression evaluates to true for every such tuple, the result of the Forall expression is true.
    /// Typically, the expression contains references to the variables defined in this.BoundVariables.
    /// </summary>
    public virtual Expression Condition {
      get { return this.condition; }
    }
    readonly Expression condition;

    /// <summary>
    /// IsTrue(this.Condition)
    /// </summary>
    public Expression ConvertedCondition {
      get {
        if (this.convertedCondition == null)
          this.convertedCondition = new IsTrue(this.Condition);
        return this.convertedCondition;
      }
    }
    Expression/*?*/ convertedCondition;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="template"></param>
    protected void CopyTriggersFromTemplate(Quantifier template) {
      IEnumerable<IEnumerable<Expression>>/*?*/ triggers = template.Compilation.ContractProvider.GetTriggersFor(template);
      if (triggers != null) {
        IEnumerable<IEnumerable<Expression>> copiedTriggers = this.CopyTriggers(triggers, this.ContainingBlock);
        this.Compilation.ContractProvider.AssociateTriggersWithQuantifier(this, copiedTriggers);
      }
    }

    internal IEnumerable<IEnumerable<Expression>> CopyTriggers(IEnumerable<IEnumerable<Expression>> triggers, BlockStatement containingBlock) {
      List<IEnumerable<Expression>> copiedTriggers = new List<IEnumerable<Expression>>();
      foreach (IEnumerable<Expression> trigger in triggers) {
        List<Expression> copiedTrigger = new List<Expression>();
        foreach (Expression e in trigger) {
          copiedTrigger.Add(e.MakeCopyFor(containingBlock));
        }
        copiedTriggers.Add(copiedTrigger.AsReadOnly());
      }
      return copiedTriggers.AsReadOnly();
    }

    /// <summary>
    /// Checks if the expression has a side effect and reports an error unless told otherwise.
    /// </summary>
    /// <param name="reportError">If true, report an error if the expression has a side effect.</param>
    public override bool HasSideEffect(bool reportError) {
      return this.ConvertedCondition.HasSideEffect(reportError);
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      //TODO: global qualifier
      IExpression/*?*/ result = this.projectedExpression;
      if (result == null) {
        if (this.HasErrors)
          result = new DummyExpression(this.SourceLocation);
        else {
          Expression forallRef = NamespaceHelper.CreateInSystemDiagnosticsContractsCodeContractExpr(this.NameTable, this.GetQuantifierName());
          IEnumerator<LocalDeclarationsStatement> locDeclsEnumerator = this.BoundVariables.GetEnumerator();
          if (locDeclsEnumerator.MoveNext()) {
            IEnumerator<LocalDeclaration> locDeclEnumerator = locDeclsEnumerator.Current.Declarations.GetEnumerator();
            if (locDeclEnumerator.MoveNext()) {
              QuantifierCall call = this.GetCallToQuantifierMethod(forallRef, locDeclEnumerator.Current, locDeclsEnumerator, locDeclEnumerator);
              call.SetContainingExpression(this);
              this.projectedExpression = result = call;
            } else
              result = new DummyExpression(this.SourceLocation);
          } else {
            result = new DummyExpression(this.SourceLocation);
          }
        }
        this.projectedExpression = result;
      }
      return result;
    }
    IExpression/*?*/ projectedExpression;


    /// <summary>
    /// Add any additional type parameters the methods used in the projection (named by GetQuantifierName()) might need.
    /// </summary>
    protected virtual void AddAdditionalTypeParameters(List<TypeExpression> genericInstanceParameters) {
    }

    private QuantifierCall GetCallToQuantifierMethod(Expression forallRef, LocalDeclaration localDeclaration, IEnumerator<LocalDeclarationsStatement> locDeclsEnumerator, IEnumerator<LocalDeclaration> locDeclEnumerator) {
      Expression condition;
      if (locDeclEnumerator.MoveNext()) {
        condition = this.GetCallToQuantifierMethod(forallRef, locDeclEnumerator.Current, locDeclsEnumerator, locDeclEnumerator);
      } else {
        if (locDeclsEnumerator.MoveNext()) {
          locDeclEnumerator = locDeclsEnumerator.Current.Declarations.GetEnumerator();
          if (locDeclEnumerator.MoveNext())
            condition = this.GetCallToQuantifierMethod(forallRef, locDeclEnumerator.Current, locDeclsEnumerator, locDeclEnumerator);
          else
            condition = this.Condition;
        } else
          condition = this.Condition;
      }
      AnonymousMethod anonMethod = this.GetAnonymousMethod(localDeclaration, condition);
      List<TypeExpression> argumentTypes = new List<TypeExpression>(1);
      if (localDeclaration.ContainingLocalDeclarationsStatement.TypeExpression != null)
        argumentTypes.Add(localDeclaration.ContainingLocalDeclarationsStatement.TypeExpression);
      this.AddAdditionalTypeParameters(argumentTypes);
      GenericInstanceExpression forallInst = new GenericInstanceExpression(forallRef, argumentTypes, SourceDummy.SourceLocation);
      return new QuantifierCall(forallInst, anonMethod, this.SourceLocation);
    }

    /// <summary>
    /// Returns an anonymous method with a single parameter, specified by locDef, and a body consisting of a single return statement
    /// that returns the given expression.
    /// </summary>
    private AnonymousMethod GetAnonymousMethod(LocalDeclaration localDeclaration, Expression condition) {
      SourceLocationBuilder slb = new SourceLocationBuilder(localDeclaration.SourceLocation);
      slb.UpdateToSpan(condition.SourceLocation);
      List<ParameterDeclaration> parameters = new List<ParameterDeclaration>(1);
      if (localDeclaration.ContainingLocalDeclarationsStatement.TypeExpression != null)
        parameters.Add(new ParameterDeclaration(null, localDeclaration.ContainingLocalDeclarationsStatement.TypeExpression, localDeclaration.Name, null, 0, false, false, false, false, localDeclaration.SourceLocation));
      List<Statement> statements = new List<Statement>(1);
      BlockStatement.Options options = BlockStatement.Options.Default;
      if (this.ContainingBlock.UseCheckedArithmetic) options = BlockStatement.Options.UseCheckedArithmetic;
      else options = BlockStatement.Options.UseUncheckedArithmetic;
      BlockStatement body = new BlockStatement(statements, options, condition.SourceLocation);
      AnonymousMethod result = new AnonymousMethod(parameters, body, slb);
      result.SetContainingExpression(this);
      body.SetContainers(result.ContainingBlock, result);
      condition.SetContainingExpression(new DummyExpression(body, SourceDummy.SourceLocation));
      statements.Add(new ReturnStatement(condition, condition.SourceLocation));
      return result;
    }

    /// <summary>
    /// A string that names the quantifier, for example "ForAll".
    /// </summary>
    protected abstract string GetQuantifierName();

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingExpression(Expression containingExpression) {
      List<Statement> statements = new List<Statement>(1);
      foreach (LocalDeclarationsStatement locDecls in this.BoundVariables) statements.Add(locDecls);
      BlockStatement dummyBlock = new BlockStatement(statements, this.SourceLocation);
      dummyBlock.SetContainingBlock(containingExpression.ContainingBlock);
      Expression dummyExpression = new DummyExpression(dummyBlock, this.SourceLocation);
      base.SetContainingExpression(dummyExpression);
      this.condition.SetContainingExpression(dummyExpression);
      IEnumerable<IEnumerable<Expression>>/*?*/ triggers = this.Compilation.ContractProvider.GetTriggersFor(this);
      if (triggers != null) {
        foreach (IEnumerable<Expression> trigger in triggers)
          foreach (Expression e in trigger)
            e.SetContainingExpression(this);
      }
    }

    /// <summary>
    /// The type of value that the expression will evaluate to, as determined at compile time.
    /// </summary>
    public override ITypeDefinition Type {
      get { return this.PlatformType.SystemBoolean.ResolvedType; }
    }

  }


  internal sealed class QuantifierCall : MethodCall {

    public QuantifierCall(Expression methodExpression, AnonymousMethod predicate, ISourceLocation sourceLocation)
      : base(methodExpression, IteratorHelper.GetSingletonEnumerable<Expression>(predicate), sourceLocation) {
      this.predicate = predicate;
    }

    private QuantifierCall(BlockStatement containingBlock, QuantifierCall template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.predicate = (AnonymousMethod)template.predicate.MakeCopyFor(containingBlock);
    }

    readonly AnonymousMethod predicate;

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new QuantifierCall(containingBlock, this);
    }

    internal void SetContainingExpression(Quantifier correspondingQuantifier) {
      base.SetContainingExpression(correspondingQuantifier);
      IEnumerable<IEnumerable<Expression>>/*?*/ triggers = this.Compilation.ContractProvider.GetTriggersFor(correspondingQuantifier);
      if (triggers != null) {
        AnonymousMethod predicate = this.GetMostNestedPredicate();
        triggers = correspondingQuantifier.CopyTriggers(triggers, predicate.Body.ContainingBlock);
        this.Compilation.ContractProvider.AssociateTriggersWithQuantifier(this, triggers);
      }
    }

    private AnonymousMethod GetMostNestedPredicate() {
      AnonymousMethod result = this.predicate;
      while (true) {
        IEnumerator<Statement> se = result.Body.Statements.GetEnumerator();
        if (se.MoveNext()) {
          ReturnStatement/*?*/ ret = se.Current as ReturnStatement;
          if (ret != null && ret.Expression is QuantifierCall && !se.MoveNext()) {
            result = ((QuantifierCall)ret.Expression).predicate;
            continue;
          }
        }
        return result;
      }
    }
  }


  /// <summary>
  /// A numeric range. Corresponds to expr To expr in VB. For example 1 to 5.
  /// </summary>
  public class Range : Expression {

    /// <summary>
    /// Allocates a numeric range. Corresponds to expr To expr in VB. For example 1 to 5.
    /// </summary>
    /// <param name="startValue">An expression resulting in the number starting the range.</param>
    /// <param name="endValue">An expression resulting in the number ending the range.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public Range(Expression startValue, Expression endValue, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.startValue = startValue;
      this.endValue = endValue;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected Range(BlockStatement containingBlock, Range template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.endValue = template.EndValue.MakeCopyFor(containingBlock);
      this.startValue = template.StartValue.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// Calls the visitor.Visit(Range) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// An expression resulting in the number ending the range.
    /// </summary>
    public Expression EndValue {
      get { return this.endValue; }
    }
    readonly Expression endValue;

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (this.ContainingBlock == containingBlock) return this;
      return new Range(containingBlock, this);
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      return new DummyExpression(this.SourceLocation);
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingExpression(Expression containingExpression) {
      base.SetContainingExpression(containingExpression);
      this.startValue.SetContainingExpression(containingExpression);
      this.endValue.SetContainingExpression(containingExpression);
    }

    /// <summary>
    /// An expression resulting in the number starting the range.
    /// </summary>
    public Expression StartValue {
      get { return this.startValue; }
    }
    readonly Expression startValue;

    /// <summary>
    /// The type of value that the expression will evaluate to, as determined at compile time.
    /// </summary>
    public override ITypeDefinition Type {
      get { return Dummy.Type; }
    }

  }

  /// <summary>
  /// An expression that must match a ref parameter of a method. 
  /// The value of the target Expression before the call is passed to the method and the method may assigns a new value to the target Expression.
  /// </summary>
  public class RefArgument : Expression, IRefArgument {

    /// <summary>
    /// Allocates an expression that must match a ref parameter of a method. 
    /// The value of the target Expression before the call is passed to the method and the method may assigns a new value to the target Expression.
    /// </summary>
    /// <param name="expression">The target that is assigned to as a result of the method call, but whose value is also passed to the method at the start of the call.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public RefArgument(AddressableExpression expression, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.expression = expression;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected RefArgument(BlockStatement containingBlock, RefArgument template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.expression = (AddressableExpression)template.expression.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// Calls the visitor.Visit(IRefArgument) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(RefArgument) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The target that is assigned to as a result of the method call, but whose value is also passed to the method at the start of the call.
    /// </summary>
    public AddressableExpression Expression {
      get { return this.expression; }
    }
    readonly AddressableExpression expression;

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new RefArgument(containingBlock, this);
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      return this;
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingExpression(Expression containingExpression) {
      base.SetContainingExpression(containingExpression);
      this.expression.SetContainingExpression(this);
    }

    /// <summary>
    /// The type of value that the expression will evaluate to, as determined at compile time.
    /// </summary>
    public override ITypeDefinition Type {
      get { return this.Expression.Type; }
    }

    #region IRefArgument Members

    IAddressableExpression IRefArgument.Expression {
      get { return this.expression; }
    }

    #endregion


    #region IExpression Members

    ITypeReference IExpression.Type {
      get { return this.Type; }
    }

    #endregion
  }

  /// <summary>
  /// An expression that results in the value of the left operand, shifted right by the number of bits specified by the value of the right operand, duplicating the sign bit.
  /// When the operator is overloaded, this expression corresponds to a call to op_RightShift.
  /// </summary>
  public class RightShift : BinaryOperation, IRightShift {

    /// <summary>
    /// Allocates an expression that results in the value of the left operand, shifted right by the number of bits specified by the value of the right operand, duplicating the sign bit.
    /// When the operator is overloaded, this expression corresponds to a call to op_RightShift.
    /// </summary>
    /// <param name="leftOperand">The left operand.</param>
    /// <param name="rightOperand">The right operand.</param>
    /// <param name="sourceLocation">The source location of the operation.</param>
    public RightShift(Expression leftOperand, Expression rightOperand, ISourceLocation sourceLocation)
      : base(leftOperand, rightOperand, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected RightShift(BlockStatement containingBlock, RightShift template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Calls the visitor.Visit(IRightShift) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(RightShift) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// True if the constant is a positive integer that could be interpreted as a negative signed integer.
    /// For example, 0x80000000, could be interpreted as a convenient way of writing int.MinValue.
    /// </summary>
    public override bool CouldBeInterpretedAsNegativeSignedInteger {
      get { return this.Value != null && this.LeftOperand.CouldBeInterpretedAsNegativeSignedInteger; }
    }

    /// <summary>
    /// Returns the string used to identify the operator in error messages
    /// </summary>
    protected override string OperationSymbolForErrorMessage {
      get { return ">>"; }
    }


    /// <summary>
    /// Returns the name a method must have to overload this operator.
    /// </summary>
    protected override IName GetOperatorName() {
      return this.NameTable.OpRightShift;
    }

    /// <summary>
    /// Computes the compile time value of the expression. Can be null.
    /// </summary>
    protected override object/*?*/ GetValue() {
      object/*?*/ left = this.ConvertedLeftOperand.Value;
      object/*?*/ right = this.ConvertedRightOperand.Value;
      if (left == null || right == null) return null;
      switch (System.Convert.GetTypeCode(left)) {
        case TypeCode.Int32:
          //^ assume left is int && right is int;
          return (int)left >> (int)right;
        case TypeCode.UInt32:
          //^ assume left is uint && right is int;
          return (uint)left >> (int)right;
        case TypeCode.Int64:
          //^ assume left is long && right is int;
          return (long)left >> (int)right;
        case TypeCode.UInt64:
          //^ assume left is ulong && right is int;
          return (ulong)left >> (int)right;
      }
      return null;
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new RightShift(containingBlock, this);
    }

    /// <summary>
    /// A list of dummy methods that correspond to operations that are built into IL. The dummy methods are used, via overload resolution,
    /// to determine how the operands are to be converted before the operation is carried out.
    /// </summary>
    protected override IEnumerable<IMethodDefinition> StandardOperators {
      get {
        BuiltinMethods dummyMethods = this.Compilation.BuiltinMethods;
        yield return dummyMethods.Int32opInt32;
        yield return dummyMethods.UInt32opInt32;
        yield return dummyMethods.Int64opInt32;
        yield return dummyMethods.UInt64opInt32;
      }
    }

    /// <summary>
    /// Returns true if the expression represents a compile time constant without an explicitly specified type. For example, 1 rather than 1L.
    /// Constant expressions such as 2*16 are polymorhpic if both operands are polymorhic.
    /// </summary>
    public override bool ValueIsPolymorphicCompileTimeConstant {
      get { return this.Value != null && this.LeftOperand.ValueIsPolymorphicCompileTimeConstant; }
    }

  }

  /// <summary>
  /// An expression that results in the value of the left operand, shifted right by the number of bits specified by the value of the right operand, duplicating the sign bit.
  /// The result of the expression is assigned to the left operand, which must be a target expression.
  /// When the operator is overloaded, this expression corresponds to a call to op_RightShift.
  /// </summary>
  public class RightShiftAssignment : BinaryOperationAssignment {

    /// <summary>
    /// Allocates an expression that results in the value of the left operand, shifted right by the number of bits specified by the value of the right operand, duplicating the sign bit.
    /// The result of the expression is assigned to the left operand, which must be a target expression.
    /// When the operator is overloaded, this expression corresponds to a call to op_RightShift.
    /// </summary>
    /// <param name="leftOperand">The left operand and target of the assignment.</param>
    /// <param name="rightOperand">The right operand.</param>
    /// <param name="sourceLocation">The source location of the operation.</param>
    public RightShiftAssignment(TargetExpression leftOperand, Expression rightOperand, ISourceLocation sourceLocation)
      : base(leftOperand, rightOperand, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected RightShiftAssignment(BlockStatement containingBlock, RightShiftAssignment template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Calls the visitor.Visit(RightShiftAssignment) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (this.ContainingBlock == containingBlock) return this;
      return new RightShiftAssignment(containingBlock, this);
    }

    /// <summary>
    /// Creates a right shift expression with the given left operand and this.RightOperand.
    /// The method does not use this.LeftOperand.Expression, since it may be necessary to factor out any subexpressions so that
    /// they are evaluated only once. The given left operand expression is expected to be the expression that remains after factoring.
    /// </summary>
    /// <param name="leftOperand">An expression to combine with this.RightOperand into a binary expression.</param>
    protected override Expression CreateBinaryExpression(Expression leftOperand) {
      Expression result = new RightShift(leftOperand, this.RightOperand, this.SourceLocation);
      result.SetContainingExpression(this);
      return result;
    }
  }

  /// <summary>
  /// An expression that results in true if both operands represent the object. In VB the Is operator corresponds to this expression.
  /// </summary>
  public class ReferenceEquality : BinaryOperation {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="leftOperand"></param>
    /// <param name="rightOperand"></param>
    /// <param name="sourceLocation"></param>
    public ReferenceEquality(Expression leftOperand, Expression rightOperand, ISourceLocation sourceLocation)
      : base(leftOperand, rightOperand, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected ReferenceEquality(BlockStatement containingBlock, ReferenceEquality template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Calls the visitor.Visit(xxxxx) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      //visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(ReferenceEquality) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Returns the string used to identify the operator in error messages
    /// </summary>
    protected override string OperationSymbolForErrorMessage {
      get { return "Is"; }
    }


    /// <summary>
    /// Returns the name a method must have to overload this operator.
    /// </summary>
    protected override IName GetOperatorName() {
      return this.NameTable.EmptyName;
    }

    /// <summary>
    /// Infers the type of value that this expression will evaluate to. At runtime the actual value may be an instance of subclass of the result of this method.
    /// Calling this method does not cache the computed value and does not generate any error messages. In some cases, such as references to the parameters of lambda
    /// expressions during type overload resolution, the value returned by this method may be different from one call to the next.
    /// When type inference fails, Dummy.Type is returned.
    /// </summary>
    public override ITypeDefinition InferType() {
      return this.PlatformType.SystemBoolean.ResolvedType;
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new ReferenceEquality(containingBlock, this);
    }

    /// <summary>
    /// A list of dummy methods that correspond to operations that are built into IL. The dummy methods are used, via overload resolution,
    /// to determine how the operands are to be converted before the operation is carried out.
    /// </summary>
    protected override IEnumerable<IMethodDefinition> StandardOperators {
      get {
        return Enumerable<IMethodDefinition>.Empty; //TODO: implement this
      }
    }

  }

  /// <summary>
  /// An expression that results in true if the left operand is a different object than the right operand.  In VB the IsNot operator corresponds to this expression.
  /// </summary>
  public class ReferenceInequality : BinaryOperation {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="leftOperand"></param>
    /// <param name="rightOperand"></param>
    /// <param name="sourceLocation"></param>
    public ReferenceInequality(Expression leftOperand, Expression rightOperand, ISourceLocation sourceLocation)
      : base(leftOperand, rightOperand, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected ReferenceInequality(BlockStatement containingBlock, ReferenceInequality template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Calls the visitor.Visit(xxxxx) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      //visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(ReferenceInequality) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Returns the string used to identify the operator in error messages
    /// </summary>
    protected override string OperationSymbolForErrorMessage {
      get { return "IsNot"; }
    }


    /// <summary>
    /// Returns the name a method must have to overload this operator.
    /// </summary>
    protected override IName GetOperatorName() {
      return this.NameTable.EmptyName;
    }

    /// <summary>
    /// Infers the type of value that this expression will evaluate to. At runtime the actual value may be an instance of subclass of the result of this method.
    /// Calling this method does not cache the computed value and does not generate any error messages. In some cases, such as references to the parameters of lambda
    /// expressions during type overload resolution, the value returned by this method may be different from one call to the next.
    /// When type inference fails, Dummy.Type is returned.
    /// </summary>
    public override ITypeDefinition InferType() {
      return this.PlatformType.SystemBoolean.ResolvedType;
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new ReferenceInequality(containingBlock, this);
    }

    /// <summary>
    /// A list of dummy methods that correspond to operations that are built into IL. The dummy methods are used, via overload resolution,
    /// to determine how the operands are to be converted before the operation is carried out.
    /// </summary>
    protected override IEnumerable<IMethodDefinition> StandardOperators {
      get {
        return Enumerable<IMethodDefinition>.Empty; //TODO: implement this
      }
    }

  }

  /// <summary>
  /// An expression that respresents a call to a method that has already been resolved by the time this expression is constructed.
  /// Only for use during projection. I.e. do not contruct this kind of expression from a parser.
  /// </summary>
  public class ResolvedMethodCall : MethodCall {

    /// <summary>
    /// Allocates an expression that respresents a call to a method that has already been resolved by the time this expression is constructed.
    /// Only for use during projection. I.e. do not construct this kind of expression from a parser.
    /// </summary>
    /// <param name="resolvedMethod">The resolved method to call. This method must be static.</param>
    /// <param name="convertedArguments">
    /// The arguments to pass to the resolved method, after they have been converted to match the parameters of the resolved method.
    /// Must be fully initialized.
    /// </param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public ResolvedMethodCall(IMethodDefinition resolvedMethod, List<Expression> convertedArguments, ISourceLocation sourceLocation)
      : base(sourceLocation, resolvedMethod)
      // ^ requires resolvedMethod.IsStatic;
    {
      this.convertedArguments = convertedArguments;
    }

    /// <summary>
    /// Only for use during projection. I.e. do not construct this node from a parser.
    /// </summary>
    /// <param name="resolvedMethod">The resolved method to call. This method must not be static.</param>
    /// <param name="thisArgument">The this argument of the method. Must be fully initialized.</param>
    /// <param name="convertedArguments">
    /// The arguments to pass to the resolved method, after they have been converted to match the parameters of the resolved method.
    /// Must be fully initialized.
    /// </param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public ResolvedMethodCall(IMethodDefinition resolvedMethod, Expression thisArgument, List<Expression> convertedArguments, ISourceLocation sourceLocation)
      : base(sourceLocation, resolvedMethod)
      // ^ requires !resolvedMethod.IsStatic;
    {
      this.convertedArguments = convertedArguments;
      this.thisArgument = thisArgument;
    }

    /// <summary>
    /// The arguments to pass to the resolved method, after they have been converted to match the parameters of the resolved method.
    /// </summary>
    protected override List<Expression> ConvertArguments() {
      return this.convertedArguments;
    }
    readonly List<Expression> convertedArguments;

    /// <summary>
    /// Returns a collection of methods that represent the constructors for the named type.
    /// </summary>
    /// <param name="allowMethodParameterInferencesToFail">This flag is ignored, since constructors cannot have generic parameters.</param>
    public override IEnumerable<IMethodDefinition> GetCandidateMethods(bool allowMethodParameterInferencesToFail) {
      return Enumerable<IMethodDefinition>.Empty;
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingExpression(Expression containingExpression) {
      base.SetContainingExpression(containingExpression);
      foreach (Expression arg in convertedArguments) arg.SetContainingExpression(this);
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      //^ assume false; //This class should never be instantiated by a parser.
      return this;
    }

    /// <summary>
    /// The this argument of the method.
    /// </summary>
    public override Expression ThisArgument {
      get
        //^^ requires !this.ResolvedMethod.IsStatic;
      {
        if (this.convertedThisArgument == null)
          this.convertedThisArgument = this.GetAsReferenceIfValueType(this.thisArgument);
        return this.convertedThisArgument;
      }
    }
    readonly Expression/*?*/ thisArgument;
    Expression/*?*/ convertedThisArgument;

  }

  /// <summary>
  /// An expression that refers to the return value of a method.
  /// </summary>
  public class ReturnValue : Expression, IReturnValue {

    /// <summary>
    /// Allocates an expression that refers to the return value of a method.
    /// </summary>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public ReturnValue(ISourceLocation sourceLocation)
      : base(sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected ReturnValue(BlockStatement containingBlock, ReturnValue template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Calls the visitor.Visit(IReturnValue) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(ReturnValue) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new ReturnValue(containingBlock, this);
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      return this;
    }

    /// <summary>
    /// The type of value that the expression will evaluate to, as determined at compile time.
    /// </summary>
    public sealed override ITypeDefinition Type {
      get {
        if (this.type == null)
          this.type = this.InferType();
        return this.type;
      }
    }
    //^ [Once]
    ITypeDefinition/*?*/ type;

    /// <summary>
    /// Infers the type of value that this expression will evaluate to. At runtime the actual value may be an instance of subclass of the result of this method.
    /// Calling this method does not cache the computed value and does not generate any error messages. In some cases, such as references to the parameters of lambda
    /// expressions during type overload resolution, the value returned by this method may be different from one call to the next.
    /// When type inference fails, Dummy.Type is returned.
    /// </summary>
    public override ITypeDefinition InferType() {
      ISignatureDeclaration/*?*/ containingSignatureDeclaration = this.ContainingBlock.ContainingSignatureDeclaration;
      AnonymousDelegate/*?*/ anonDel = containingSignatureDeclaration as AnonymousDelegate;
      while (anonDel != null) {
        containingSignatureDeclaration = anonDel.ContainingBlock.ContainingSignatureDeclaration;
        anonDel = containingSignatureDeclaration as AnonymousDelegate;
      }
      AnonymousMethod/*?*/ anonMethod = containingSignatureDeclaration as AnonymousMethod;
      while (anonMethod != null) {
        containingSignatureDeclaration = anonMethod.ContainingBlock.ContainingSignatureDeclaration;
        anonMethod = containingSignatureDeclaration as AnonymousMethod;
      }
      if (containingSignatureDeclaration != null)
        return containingSignatureDeclaration.Type.ResolvedType;
      //TODO: error if signature has void type
      this.ReportNoContainingSignatureError();
      return Dummy.Type;
    }

    /// <summary>
    /// 
    /// </summary>
    protected virtual void ReportNoContainingSignatureError() {
      this.Helper.ReportError(new AstErrorMessage(this, Error.NameNotInContext, "result"));
    }

    #region IExpression Members

    ITypeReference IExpression.Type {
      get { return this.Type; }
    }

    #endregion

  }

  /// <summary>
  /// An expression that denotes the root namespace of a compilation.
  /// </summary>
  public class RootNamespaceExpression : Expression {

    /// <summary>
    /// Allocates an expression that denotes the root namespace of a compilation.
    /// </summary>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public RootNamespaceExpression(ISourceLocation sourceLocation)
      : base(sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected RootNamespaceExpression(BlockStatement containingBlock, RootNamespaceExpression template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Calls the visitor.Visit(RootNamespaceExpression) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (this.ContainingBlock == containingBlock) return this;
      return new RootNamespaceExpression(containingBlock, this);
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      //^ assume false;
      return new DummyExpression(this.SourceLocation);
    }

    /// <summary>
    /// The type of value that the expression will evaluate to, as determined at compile time.
    /// </summary>
    public override ITypeDefinition Type {
      get { return this.PlatformType.SystemVoid.ResolvedType; }
    }

  }

  /// <summary>
  /// An expression that denotes the runtime argument handle of a method that accepts extra arguments. 
  /// This expression corresponds to __arglist in C# and results in a value that can be used as the argument to the constructor for System.ArgIterator.
  /// </summary>
  public class RuntimeArgumentHandleExpression : Expression, IRuntimeArgumentHandleExpression {

    /// <summary>
    /// Allocates an expression that denotes the runtime argument handle of a method that accepts extra arguments. 
    /// This expression corresponds to __arglist in C# and results in a value that can be used as the argument to the constructor for System.ArgIterator.
    /// </summary>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public RuntimeArgumentHandleExpression(ISourceLocation sourceLocation)
      : base(sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected RuntimeArgumentHandleExpression(BlockStatement containingBlock, RuntimeArgumentHandleExpression template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Calls the visitor.Visit(IRuntimeArgumentHandleExpression) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(RuntimeArgumentHandleExpression) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (this.ContainingBlock == containingBlock) return this;
      return new RuntimeArgumentHandleExpression(containingBlock, this);
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      return this;
    }

    /// <summary>
    /// The type of value that the expression will evaluate to, as determined at compile time.
    /// </summary>
    public override ITypeDefinition Type {
      get { return this.PlatformType.SystemArgIterator.ResolvedType; }
    }

    #region IExpression Members

    ITypeReference IExpression.Type {
      get { return this.Type; }
    }

    #endregion

  }

  /// <summary>
  /// An index expression multiplied by a scale factor, representing a pointer offset.
  /// </summary>
  public class ScaledIndex : Multiplication {

    /// <summary>
    /// Allocates an index expression multiplied by a scale factor, representing a pointer offset.
    /// </summary>
    /// <param name="leftOperand">The left operand.</param>
    /// <param name="rightOperand">The right operand.</param>
    /// <param name="sourceLocation">The source location of the operation.</param>
    public ScaledIndex(Expression leftOperand, Expression rightOperand, ISourceLocation sourceLocation)
      : base(leftOperand, rightOperand, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected ScaledIndex(BlockStatement containingBlock, ScaledIndex template)
      : base(containingBlock, template) {
    }

    /// <summary>
    /// Computes the compile time value of the expression. Can be null.
    /// </summary>
    protected override object/*?*/ GetValue() {
      return null;
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock) {
      return base.MakeCopyFor(containingBlock);
    }

  }

  /// <summary>
  /// An expression consisting of a simple name, for example "SimpleName".
  /// </summary>
  public class SimpleName : Expression {

    /// <summary>
    /// Constructs an expression consisting of a simple name, for example "SimpleName".
    /// Use this constructor when constructing a new simple name. Do not give out the resulting instance to client
    /// code before completing the initialization by calling SetContainingExpression on the instance.
    /// </summary>
    /// <param name="name">The name to be wrapped as an expression.</param>
    /// <param name="sourceLocation">The source location of the SimpleName expression.</param>
    /// <param name="ignoreCase">True if the case of the simple name must be ignored when resolving it.</param>
    public SimpleName(IName name, ISourceLocation sourceLocation, bool ignoreCase)
      : base(sourceLocation) {
      this.ignoreCase = ignoreCase;
      this.name = name;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected SimpleName(BlockStatement containingBlock, SimpleName template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.ignoreCase = template.ignoreCase;
      this.name = containingBlock.Compilation.NameTable.GetNameFor(template.Name.Value);
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      object/*?*/ container = this.Resolve();
      if (container == null) {
        this.Helper.ReportError(new AstErrorMessage(this, Error.NameNotInContext, this.Name.Value));
        return true;
      }
      if (container is ILocalDefinition || container is IParameterDefinition || container is IFieldDefinition || container is IPropertyDefinition)
        return false;
      this.Helper.ReportError(new AstErrorMessage(this, Error.NameNotInContext, this.Name.Value)); //TODO: better error message
      return true;
    }

    /// <summary>
    /// Calls visitor.Visit(SimpleName).
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Computes the compile time value of the expression. Can be null.
    /// </summary>
    protected override object/*?*/ GetValue() {
      object/*?*/ container = this.ResolveAsValueContainer();
      if (container != null) {
        IFieldDefinition/*?*/ field = container as IFieldDefinition;
        if (field != null) {
          if (this.Helper.UseCompileTimeValueOfField(field)) return field.CompileTimeValue.Value;
        }
      }
      return null;
    }

    /// <summary>
    /// Determines the type of value this simple name will evaluate to, as determined at compile time. 
    /// If the simple name does not represent an expression that results in a value, Dummy.Type is returned.
    /// (The latter can happen when the simple name serves as the qualifier in a qualified name.)
    /// </summary>
    public override ITypeDefinition InferType() {
      object/*?*/ boundDefinition = this.ResolveAsValueContainer();
      ILocalDefinition/*?*/ local = boundDefinition as ILocalDefinition;
      if (local != null) return local.Type.ResolvedType;
      IParameterDefinition/*?*/ parameter = boundDefinition as IParameterDefinition;
      if (parameter != null) return parameter.Type.ResolvedType;
      IEventDefinition/*?*/ eventDef = boundDefinition as IEventDefinition;
      if (eventDef != null) return eventDef.Type.ResolvedType;
      IFieldDefinition/*?*/ field = boundDefinition as IFieldDefinition;
      if (field != null) return field.Type.ResolvedType;
      IPropertyDefinition/*?*/ property = boundDefinition as IPropertyDefinition;
      if (property != null) return property.Type.ResolvedType;
      //^ assert boundDefinition == null;
      return Dummy.Type;
    }

    /// <summary>
    /// Gets the namespace in which the given type is contained. Also works for nested types.
    /// </summary>
    protected NamespaceDeclaration GetContainingNamespace(TypeDeclaration typeDeclaration) {
      while (true) {
        NestedTypeDeclaration/*?*/ ntd = typeDeclaration as NestedTypeDeclaration;
        if (ntd == null) break;
        typeDeclaration = ntd.ContainingTypeDeclaration;
      }
      NamespaceTypeDeclaration/*?*/ nd = typeDeclaration as NamespaceTypeDeclaration;
      if (nd != null) return nd.ContainingNamespaceDeclaration;
      //The given type declaration is not nested and not in a namespace declaration. That cannot be statically precluded, but is an invalid way to construct an AST.
      //^ assume nd != null;
      return nd.ContainingNamespaceDeclaration; //fail so that AST constructor gets some feedback while debugging.
    }

    /// <summary>
    /// Checks if the expression has a side effect and reports an error unless told otherwise.
    /// </summary>
    /// <param name="reportError">If true, report an error if the expression has a side effect.</param>
    public override bool HasSideEffect(bool reportError) {
      return false;
      //TODO: if this is a property getter, check the getter for side effects
    }

    /// <summary>
    /// True if the case of the simple name must be ignored when resolving it.
    /// </summary>
    public bool IgnoreCase {
      get { return this.ignoreCase; }
    }
    private readonly bool ignoreCase;

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new SimpleName(containingBlock, this);
    }

    /// <summary>
    /// The name corresponding to the source expression.
    /// </summary>
    public IName Name {
      get { return this.name; }
    }
    readonly IName name;

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      if (this.cachedProjection != null) return this.cachedProjection;
      object/*?*/ container = this.ResolveAsValueContainer();
      IPropertyDefinition/*?*/ property = container as IPropertyDefinition;
      if (property != null) {
        if (property.Getter != null) {
          if (property.Getter.ResolvedMethod.IsStatic)
            return this.cachedProjection = new ResolvedMethodCall(property.Getter.ResolvedMethod, new List<Expression>(0), this.SourceLocation);
          else {
            ThisReference thisRef = new ThisReference(this.SourceLocation);
            thisRef.SetContainingExpression(this);
            return this.cachedProjection = new ResolvedMethodCall(property.Getter.ResolvedMethod, thisRef, new List<Expression>(0), this.SourceLocation);
          }
        }
        return this.cachedProjection = new DummyExpression(this.SourceLocation);
      }
      if (container is IEventDefinition) return this.cachedProjection = new DummyExpression(this.SourceLocation); //TODO: is this right?
      IFieldDefinition/*?*/ field = container as IFieldDefinition;
      if (field != null) {
        if (this.Helper.UseCompileTimeValueOfField(field)) return this.cachedProjection = CompileTimeConstant.For(field.CompileTimeValue, this);
      } else {
        LocalDefinition/*?*/ local = container as LocalDefinition;
        if (local != null) {
          if (local.IsConstant) {
            var compileTimeVal = local.CompileTimeValue.ProjectAsIExpression();
            if (!(compileTimeVal is DummyConstant)) return cachedProjection = compileTimeVal;
          }
        }
      }
      if (container == null) container = Dummy.Field;
      Expression expr =  new BoundExpression(this, container);
      IParameterDefinition/*?*/ parameter = container as IParameterDefinition;
      if (parameter != null && parameter.IsByReference) {
        expr = new AddressDereference(expr, expr.SourceLocation);
      }
      expr.SetContainingExpression(this);
      return this.cachedProjection = (IExpression)expr;
    }
    IExpression/*?*/ cachedProjection;

    /// <summary>
    /// Returns either null or the local variable, parameter, type parameter, type member, namespace member or type
    /// that binds to this name using the applicable scope chain and compilation context. Method groups are represented by the
    /// first matching method definition in the most derived type that defines or overrides methods in the group. Likewise for
    /// property groups and type groups.
    /// </summary>
    public virtual object/*?*/ Resolve()
      //^^ ensures result == null || result is ILocalDefinition || result is IParameterDefinition || result is ITypeDefinitionMember || result is INamespaceMember || 
      //^^ result is ITypeDefinition || result is ITypeGroup || result is INamespaceDefinition;
    {
      if (this.hasErrors == null) this.hasErrors = false;
      if (this.resolvedValue == null)
        this.resolvedValue = this.ResolveUsing(this.ContainingBlock, false);
      if (this.resolvedValue == null && this.hasErrors != null && !(bool)this.hasErrors)
        this.hasErrors = null;
      return this.resolvedValue;
    }
    object/*?*/ resolvedValue;

    /// <summary>
    /// Returns true if this resolves to a local declaration or a 
    /// call parameter of the current method. Used for finding a
    /// worthwhile QualifiedName.Qualifier to cache in FactoredExpression.
    /// </summary>
    public bool ResolvesToLocalOrParameter {
      get { return (resolvedValue is LocalDefinition || resolvedValue is ParameterDefinition); }
    }

    /// <summary>
    /// Returns either null or the namespace or type that binds to this name using the applicable scope chain and compilation context.
    /// When the name binds to both a type and and a namespace, only the type is returned. When the name binds to more than one
    /// type, a type group is returned.
    /// </summary>
    public virtual object/*?*/ ResolveAsNamespaceOrType(bool reportError = false)
      //^ ensures result == null || result is INamespaceDefinition || result is ITypeDefinition || result is ITypeGroup;
    {
      this.hasErrors = false;
      object/*?*/ result = this.ResolveUsing(this.ContainingBlock, true);
      if (result == null) {
        if (reportError) {
          this.Helper.ReportError(new AstErrorMessage(this, Error.SingleTypeNameNotFound, this.Name.Value));
          this.hasErrors = true;
        } else
          this.hasErrors = null;
      }
      return result;
    }

    /// <summary>
    /// Returns either null or the namespace or that binds to this name using the applicable scope chain and compilation context.
    /// </summary>
    public virtual INamespaceDefinition/*?*/ ResolveAsNamespace() {
      this.hasErrors = false;
      NamespaceDeclaration containingNamespace = this.ContainingBlock.ContainingNamespaceDeclaration;
      while (true) {
        AliasDeclaration/*?*/ aliasDeclaration = null;
        UnitSetAliasDeclaration/*?*/ unitSetAliasDeclaration = null;
        if (!containingNamespace.BusyResolvingAnAliasOrImport)
          containingNamespace.GetAliasNamed(this.Name, this.ignoreCase, ref aliasDeclaration, ref unitSetAliasDeclaration);
        foreach (INamespaceMember member in containingNamespace.UnitSetNamespace.GetMembersNamed(this.Name, this.ignoreCase)) {
          INamespaceDefinition/*?*/ nsDef = member as INamespaceDefinition;
          if (nsDef != null) {
            //TODO: if aliasDeclaration != null, then report an error
            return nsDef;
          }
        }
        if (aliasDeclaration != null) return aliasDeclaration.ResolvedNamespaceOrType as INamespaceDefinition;
        if (unitSetAliasDeclaration != null) return unitSetAliasDeclaration.UnitSet.UnitSetNamespaceRoot;
        NestedNamespaceDeclaration/*?*/ nestedContainingNamespace = containingNamespace as NestedNamespaceDeclaration;
        if (nestedContainingNamespace == null) return null;
        containingNamespace = nestedContainingNamespace.ContainingNamespaceDeclaration;
      }
    }

    /// <summary>
    /// Returns the statement labeled with this name and located in the same block as this name, or in a containing block.
    /// Returns Dummy.LabeledStatement if no such statement can be found.
    /// </summary>
    public ILabeledStatement ResolveAsTargetStatement() {
      for (BlockStatement b = this.ContainingBlock; b.ContainingBlock != b && b.Scope is StatementScope; b = b.ContainingBlock) {
        LabeledStatement/*?*/ target = b.Scope.GetStatementLabeled(this.Name, this.ignoreCase);
        if (target != null) return target;
      }
      //TODO: report error
      return CodeDummy.LabeledStatement;
    }

    /// <summary>
    /// Returns either null or the local variable, parameter, field or property
    /// that binds to this name using the applicable scope chain and compilation context.
    /// </summary>
    public virtual object/*?*/ ResolveAsValueContainer()
      //^ ensures result == null || result is ILocalDefinition || result is IParameterDefinition || result is IEventDefinition || result is IFieldDefinition || 
      //^    result is IPropertyDefinition;
    {
      object/*?*/ result = this.Resolve();
      this.hasErrors = null;
      IPropertyDefinition/*?*/ property = result as IPropertyDefinition;
      if (property != null && property.Parameters.GetEnumerator().MoveNext()) {
        //TODO: error, indexer not expected here
        //TODO: C# does not allow indexers and properties to have the same name, but some other language could. The language specification does not spell out
        //what should happen in such a case. Need to check up what the original C# compiler does.
        return null;
      }
      if (!(result is ILocalDefinition || result is IParameterDefinition || result is IEventDefinition || result is IFieldDefinition || result is IPropertyDefinition)) {
        return null;
      }
      return result;
    }

    /// <summary>
    /// Returns either null or the local variable, parameter, type parameter, type member, namespace member or type
    /// that binds to this name using the scope chain of the given block.
    /// </summary>
    /// <param name="block">The block whose scope chain is used to resolve this name.</param>
    /// <param name="restrictToNamespacesAndTypes">True if only namespaces and types should be considered when resolving this name.</param>
    protected virtual object/*?*/ ResolveUsing(BlockStatement block, bool restrictToNamespacesAndTypes)
      //^ ensures result == null || result is ITypeDefinition || result is INamespaceDefinition || result is ITypeGroup ||
      //^ (!restrictToNamespacesAndTypes && (result is ILocalDefinition || result is IParameterDefinition || result is ITypeDefinitionMember || result is INamespaceMember));
    {
      if (!restrictToNamespacesAndTypes) {
        for (BlockStatement b = block; b.ContainingSignatureDeclaration == block.ContainingSignatureDeclaration && b.ContainingBlock != b && b.Scope is StatementScope; b = b.ContainingBlock) {
          IEnumerator<LocalDeclaration> locals = b.Scope.GetMembersNamed(this.Name, this.ignoreCase).GetEnumerator();
          if (locals.MoveNext()) {
            LocalDefinition localVar = locals.Current.LocalVariable;
            //^ assume localVar is ILocalDefinition; //LocalDefinition : ILocalDefinition
            return localVar;
          }
        }
        if (block.ContainingSignatureDeclaration != null) {
          int myKey = this.ignoreCase ? this.Name.UniqueKeyIgnoringCase : this.Name.UniqueKey;
          foreach (ParameterDeclaration par in block.ContainingSignatureDeclaration.Parameters) {
            int parKey = this.ignoreCase ? par.Name.UniqueKeyIgnoringCase : par.Name.UniqueKey;
            if (parKey == myKey) return par.ParameterDefinition;
          }
        }
      }
      AnonymousMethod/*?*/ anonymousMethod = block.ContainingSignatureDeclaration as AnonymousMethod;
      if (anonymousMethod != null)
        return this.ResolveUsing(anonymousMethod.ContainingBlock, restrictToNamespacesAndTypes);
      AnonymousDelegate/*?*/ anonymousDelegate = block.ContainingSignatureDeclaration as AnonymousDelegate;
      if (anonymousDelegate != null)
        return this.ResolveUsing(anonymousDelegate.ContainingBlock, restrictToNamespacesAndTypes);
      if (block.ContainingSignatureDeclaration != null)
        return this.ResolveUsing(block.ContainingSignatureDeclaration, restrictToNamespacesAndTypes);
      if (block.ContainingTypeDeclaration != null)
        return this.ResolveUsing(block.ContainingTypeDeclaration, restrictToNamespacesAndTypes);
      return this.ResolveUsing(block.ContainingNamespaceDeclaration, restrictToNamespacesAndTypes);
    }

    /// <summary>
    /// Returns either null or the local variable, parameter, type parameter, type member, namespace member or type
    /// that binds to this name using the scope chain of the given method.
    /// </summary>
    /// <param name="signatureDeclaration">The signature bearing object whose scope chain is used to resolve this name.</param>
    /// <param name="restrictToNamespacesAndTypes">True if only namespaces and types should be considered when resolving this name.</param>
    protected virtual object/*?*/ ResolveUsing(ISignatureDeclaration signatureDeclaration, bool restrictToNamespacesAndTypes)
      //^ ensures result == null || result is ITypeDefinition || result is INamespaceDefinition || result is ITypeGroup ||
      //^ (!restrictToNamespacesAndTypes && (result is IParameterDefinition || result is ITypeDefinitionMember || result is INamespaceMember));
    {
      MethodDeclaration/*?*/ method = signatureDeclaration as MethodDeclaration;
      if (method != null && method.IsGeneric) {
        int myKey = this.ignoreCase ? this.Name.UniqueKeyIgnoringCase : this.Name.UniqueKey;
        foreach (GenericMethodParameterDeclaration gpar in method.GenericParameters) {
          int gparKey = this.ignoreCase ? gpar.Name.UniqueKeyIgnoringCase : gpar.Name.UniqueKey;
          if (gparKey == myKey) {
            return gpar.GenericMethodParameterDefinition;
          }
        }
      }
      ITypeDeclarationMember/*?*/ typeDeclarationMember = signatureDeclaration as ITypeDeclarationMember;
      if (typeDeclarationMember == null) return null;
      return this.ResolveUsing(typeDeclarationMember.ContainingTypeDeclaration, restrictToNamespacesAndTypes);
    }

    /// <summary>
    /// Returns either null or the local variable, parameter, type parameter, type member, namespace member or type
    /// that binds to this name using the scope chain of the given type declaration. Uses a cache.
    /// </summary>
    /// <param name="typeDeclaration">The type whose scope chain is used to resolve this name.</param>
    /// <param name="restrictToNamespacesAndTypes">True if only namespaces and types should be considered when resolving this name.</param>
    protected virtual object/*?*/ ResolveUsing(TypeDeclaration typeDeclaration, bool restrictToNamespacesAndTypes)
      //^ ensures result == null || result is ITypeDefinition || result is INamespaceDefinition || result is ITypeGroup ||
      //^ (!restrictToNamespacesAndTypes && (result is ITypeDefinitionMember || result is INamespaceMember));
    {
      //First look for a cached result of previous search for same name
      Dictionary<int, object/*?*/> cache;
      int key;
      object/*?*/ result;
      if (this.ignoreCase) {
        cache = typeDeclaration.caseInsensitiveCache;
        key = this.Name.UniqueKeyIgnoringCase;
      } else {
        key = this.Name.UniqueKey;
        cache = typeDeclaration.caseSensitiveCache;
      }
      lock (GlobalLock.LockingObject) {
        if (cache.TryGetValue(key, out result)) {
          //Assume that no one other method is making use of the cache. In principle, this could be enforced by an invariant in
          //typeDeclaration (along with some accessor routines), or by creating a new kind of dictionary. But the assumption is much more convenient.
          //^ assume result == null || result is ITypeDefinitionMember || result is INamespaceMember || result is ITypeDefinition || result is ITypeGroup || result is INamespaceDefinition;
          if (restrictToNamespacesAndTypes) {
            if (result is ITypeDefinition || result is INamespaceDefinition) return result;
          } else {
            return result;
          }
        }
        result = null;
      }

      //Look for type parameter
      ITypeDefinition type = typeDeclaration.TypeDefinition;
      if (type.IsGeneric) {
        if (this.ignoreCase) {
          foreach (IGenericTypeParameter gpar in type.GenericParameters) {
            if (gpar.Name.UniqueKeyIgnoringCase == key) {
              result = gpar;
              //^ assume result is ITypeDefinition; //IGenericTypeParameter : IGenericParameter : ITypeDefinition
              goto cacheResult;
            }
          }
        } else {
          foreach (IGenericTypeParameter gpar in type.GenericParameters) {
            if (gpar.Name.UniqueKey == key) {
              result = gpar;
              //^ assume result is ITypeDefinition; //IGenericTypeParameter : IGenericParameter : ITypeDefinition
              goto cacheResult;
            }
          }
        }
      }

      //Ignore members if resolving a base type or interface expression
      if (typeDeclaration.OuterDummyBlock == this.ContainingBlock) {
        //This simple name is part of the signature of typeDeclaration. It should not be resolved using typeDeclaration.
        //Proceed to the outer type, if any, otherwise proceed to the namespace.
        NestedTypeDeclaration/*?*/ nestedTypeDeclaration = typeDeclaration as NestedTypeDeclaration;
        if (nestedTypeDeclaration != null) return this.ResolveUsing(nestedTypeDeclaration.ContainingTypeDeclaration, restrictToNamespacesAndTypes);
        result = this.ResolveUsing(this.GetContainingNamespace(typeDeclaration), restrictToNamespacesAndTypes);
        goto cacheResult;
      }

      //Look for member (including inherited members)
      if (type.IsGeneric) type = type.InstanceType.ResolvedType;
      result = this.ResolveUsing(type, restrictToNamespacesAndTypes);

      //Look for outer declaration
      if (result == null)
        result = this.ResolveUsing(this.GetContainingNamespace(typeDeclaration), restrictToNamespacesAndTypes);

    cacheResult:
      if (!restrictToNamespacesAndTypes) {
        lock (GlobalLock.LockingObject) {
          //^ assert result == null || result is ITypeDefinitionMember || result is INamespaceMember || result is ITypeDefinition || result is ITypeGroup || result is INamespaceDefinition;
          cache[key] = result; //If another thread has meanwhile initialized the cache, it is no problem since its result would be the same.
        }
      }
      return result;
    }

    /// <summary>
    /// Returns either null or the type member group or namespace member that binds to this name.
    /// </summary>
    /// <param name="typeDefinition">The type whose members and inherited members are used to resolve this name.</param>
    /// <param name="restrictToNamespacesAndTypes">True if only namespaces and types should be considered when resolving this name.</param>
    /// <returns></returns>
    protected virtual object/*?*/ ResolveUsing(ITypeDefinition typeDefinition, bool restrictToNamespacesAndTypes)
      //^ requires !typeDefinition.IsGeneric;
      //^ ensures result == null || result is ITypeGroup || (!restrictToNamespacesAndTypes && result is ITypeDefinitionMember);
    {
      if (typeDefinition is Dummy) return null;
      IEnumerable<ITypeDefinitionMember> members = typeDefinition.GetMembersNamed(this.Name, this.ignoreCase);
      foreach (ITypeDefinitionMember member in members) {
        //TODO: filter out members that are not visible to this expression
        ITypeDefinition/*?*/ nestedType = member as ITypeDefinition;
        if (nestedType != null) return new NestedTypeGroup(this, typeDefinition, this);
        if (!restrictToNamespacesAndTypes) return member;
      }
      ITypeContract/*?*/ contract = this.Compilation.ContractProvider.GetTypeContractFor(typeDefinition);
      if (contract != null) {
        foreach (IFieldDefinition contractField in contract.ContractFields) {
          if (this.ignoreCase) {
            if (contractField.Name.UniqueKeyIgnoringCase == this.Name.UniqueKeyIgnoringCase) return contractField;
          } else {
            if (contractField.Name.UniqueKey == this.Name.UniqueKey) return contractField;
          }
        }
      }
      IEnumerable<ITypeReference> baseClasses = typeDefinition.BaseClasses;
      foreach (ITypeReference baseClassReference in baseClasses) {
        ITypeDefinition baseClass = baseClassReference.ResolvedType;
        if (baseClass.IsGeneric) baseClass = baseClass.InstanceType.ResolvedType;
        object/*?*/ result = this.ResolveUsing(baseClass, restrictToNamespacesAndTypes);
        if (result != null) return result;
      }
      INestedTypeDefinition/*?*/ nestedTypeDefinition = typeDefinition as INestedTypeDefinition;
      if (nestedTypeDefinition != null) {
        ITypeDefinition containingTypeDefinition = nestedTypeDefinition.ContainingTypeDefinition;
        //^ assume !containingTypeDefinition.IsGeneric; 
        //follows from post condition of ContainingTypeDefinition and precondition of method and fact that typeDefinition is immutable
        return this.ResolveUsing(containingTypeDefinition, restrictToNamespacesAndTypes);
      }
      if (TypeHelper.TypesAreEquivalent(typeDefinition, this.PlatformType.SystemObject)) return null;
      return this.ResolveUsing(this.PlatformType.SystemObject.ResolvedType, restrictToNamespacesAndTypes);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="nd"></param>
    /// <returns></returns>
    protected bool NamespaceDeclIsBusy(NamespaceDeclaration nd) {
      return nd.BusyResolvingAnAliasOrImport;
    }

    /// <summary>
    /// Returns either null or the namespace member (group) that binds to this name. 
    /// This implementation of this method ignores global methods and variables, as is the case for C#. //TODO: is this really the case?
    /// </summary>
    /// <param name="namespaceDeclaration">The namespace to use to resolve this name.</param>
    /// <param name="restrictToNamespacesAndTypes">True if only namespaces and types should be considered when resolving this name.</param>
    protected virtual object/*?*/ ResolveUsing(NamespaceDeclaration namespaceDeclaration, bool restrictToNamespacesAndTypes)
      //^ ensures result == null || result is ITypeDefinition || result is INamespaceDefinition || result is ITypeGroup ||
      //^ (!restrictToNamespacesAndTypes && result is INamespaceMember);
    {
      IScope<INamespaceMember> scope = namespaceDeclaration.UnitNamespace;
      AliasDeclaration/*?*/ aliasDeclaration = null;
      UnitSetAliasDeclaration/*?*/ unitSetAliasDeclaration = null;
      if (!namespaceDeclaration.BusyResolvingAnAliasOrImport)
        namespaceDeclaration.GetAliasNamed(name, ignoreCase, ref aliasDeclaration, ref unitSetAliasDeclaration);
      IEnumerable<INamespaceMember> members = scope.GetMembersNamed(this.Name, this.ignoreCase);
      INamespaceTypeDefinition/*?*/ namespaceTypeDefinition = null;
      INamespaceDefinition/*?*/ nestedNamespaceDefinition = null;
      foreach (INamespaceMember member in members) {
        if (!restrictToNamespacesAndTypes) {
          var globalField = member as IGlobalFieldDefinition;
          if (globalField != null) return globalField;
        }
        nestedNamespaceDefinition = member as INamespaceDefinition;
        if (nestedNamespaceDefinition != null) {
          //TODO: if aliasDeclaration != null give an ambiguous reference error
          return nestedNamespaceDefinition;
        }
        if (namespaceTypeDefinition == null) {
          namespaceTypeDefinition = member as INamespaceTypeDefinition;
          if (namespaceTypeDefinition != null && (aliasDeclaration == null || namespaceTypeDefinition.IsGeneric)) break;
          //carry on in case there is a generic type with this name. If not there is an ambiguity between the type and the alias.
        }
      }
      if (namespaceTypeDefinition != null) {
        //TODO: if aliasDeclaration != null give an ambiguous reference error if namespaceTypeDef is not generic
        return new NamespaceTypeGroup(this, scope, this);
      }
      if (namespaceDeclaration.BusyResolvingAnAliasOrImport) {
        //Have to ignore using statements.
        scope = namespaceDeclaration.UnitSetNamespace;
        members = scope.GetMembersNamed(this.Name, this.ignoreCase);
      } else {
        if (aliasDeclaration != null) return aliasDeclaration.ResolvedNamespaceOrType;
        if (unitSetAliasDeclaration != null) {
          IUnitSetNamespace usns = unitSetAliasDeclaration.UnitSet.UnitSetNamespaceRoot;
          //^ assume usns is INamespaceDefinition; //IUnitSetNamespace : INamespaceDefinition
          return usns;
        }
        scope = namespaceDeclaration.Scope;
        members = scope.GetMembersNamed(this.Name, this.ignoreCase); //Considers types that were imported into the namespace via using statements
      }
      foreach (INamespaceMember member in members) {
        if (nestedNamespaceDefinition == null) nestedNamespaceDefinition = member as INamespaceDefinition;
        namespaceTypeDefinition = member as INamespaceTypeDefinition;
        if (namespaceTypeDefinition != null) return new NamespaceTypeGroup(this, scope, this);
      }
      if (nestedNamespaceDefinition != null) return nestedNamespaceDefinition;
      NestedNamespaceDeclaration/*?*/ nestedNamespace = namespaceDeclaration as NestedNamespaceDeclaration;
      if (nestedNamespace != null) return this.ResolveUsing(nestedNamespace.ContainingNamespaceDeclaration, restrictToNamespacesAndTypes);
      return null;
    }

    /// <summary>
    /// Returns this.Name.Value.
    /// </summary>
    //^ [Confined]
    public override string ToString() {
      return this.Name.Value;
    }

    /// <summary>
    /// The type of value this simple name will evaluate to, as determined at compile time. 
    /// If the simple name does not represent an expression that results in a value, Dummy.Type is returned.
    /// </summary>
    public sealed override ITypeDefinition Type {
      get {
        if (this.type == null)
          this.type = this.InferType();
        return this.type;
      }
    }
    //^ [Once]
    ITypeDefinition/*?*/ type;

  }

  /// <summary>
  /// An expression that computes the memory size of instances of a given type at runtime.
  /// </summary>
  public class SizeOf : Expression, ISizeOf {

    /// <summary>
    /// Allocates an expression that computes the memory size of instances of a given type at runtime.
    /// </summary>
    /// <param name="expression">The type to size.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public SizeOf(TypeExpression expression, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.expression = expression;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected SizeOf(BlockStatement containingBlock, SizeOf template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.expression = (TypeExpression)template.expression.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// Calls the visitor.Visit(ISizeOf) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(SizeOf) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The type to size.
    /// </summary>
    public TypeExpression Expression {
      get { return this.expression; }
    }
    readonly TypeExpression expression;

    /// <summary>
    /// Computes the compile time value of the expression. Can be null.
    /// </summary>
    protected override object/*?*/ GetValue() {
      switch (this.Expression.ResolvedType.TypeCode) {
        case PrimitiveTypeCode.Boolean:
        case PrimitiveTypeCode.Int8:
        case PrimitiveTypeCode.UInt8:
          return 1;
        case PrimitiveTypeCode.Char:
        case PrimitiveTypeCode.Int16:
        case PrimitiveTypeCode.UInt16:
          return 2;
        case PrimitiveTypeCode.Int32:
        case PrimitiveTypeCode.UInt32:
        case PrimitiveTypeCode.Float32:
          return 4;
        case PrimitiveTypeCode.Float64:
          return 8;
        case PrimitiveTypeCode.NotPrimitive:
          if (TypeHelper.TypesAreEquivalent(this.Expression.ResolvedType, this.PlatformType.SystemDecimal))
            return 16;
          break;
        case PrimitiveTypeCode.Pointer:
          return (int)this.Compilation.HostEnvironment.PointerSize;
      }
      return null;
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new SizeOf(containingBlock, this);
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      object/*?*/ value = this.Value;
      if (value != null) {
        CompileTimeConstant result = new CompileTimeConstant(value, this.SourceLocation);
        result.UnfoldedExpression = this;
        result.SetContainingExpression(this);
        return result;
      } else
        return this;
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingExpression(Expression containingExpression) {
      base.SetContainingExpression(containingExpression);
      this.expression.SetContainingExpression(this);
    }

    /// <summary>
    /// The type of value that the expression will evaluate to, as determined at compile time.
    /// </summary>
    public override ITypeDefinition Type {
      get { return this.PlatformType.SystemInt32.ResolvedType; }
    }

    /// <summary>
    /// Returns true if the expression represents a compile time constant without an explicitly specified type. For example, 1 rather than 1L.
    /// Constant expressions such as 2*16 are polymorhpic if both operands are polymorhic.
    /// </summary>
    public override bool ValueIsPolymorphicCompileTimeConstant {
      get { return this.Value != null; }
    }

    #region ISizeOf Members

    ITypeReference ISizeOf.TypeToSize {
      get { return this.Expression.ResolvedType; }
    }

    #endregion

    #region IExpression Members

    ITypeReference IExpression.Type {
      get { return this.Type; }
    }

    #endregion

  }

  /// <summary>
  /// An expression that represents a contiguous range of collection elements such as a substring.
  /// In VB the target expression of a Mid assignment statement corresponds to this expression.
  /// </summary>
  public class Slice : Expression {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="collection"></param>
    /// <param name="startIndex"></param>
    /// <param name="length"></param>
    /// <param name="sourceLocation"></param>
    public Slice(Expression collection, Expression startIndex, Expression length, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.collection = collection;
      this.startIndex = startIndex;
      this.length = length;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    //^ [NotDelayed]
    protected Slice(BlockStatement containingBlock, Slice template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.collection = template.collection.MakeCopyFor(containingBlock);
      this.startIndex = template.startIndex.MakeCopyFor(containingBlock);
      this.length = template.length.MakeCopyFor(containingBlock);
      //^ base;
      //^ assume this.ContainingBlock == containingBlock;
    }

    /// <summary>
    /// An expression that results in a collection or sort (such as a string).
    /// </summary>
    public Expression Collection {
      get { return this.collection; }
    }
    readonly Expression collection;

    /// <summary>
    /// Calls the visitor.Visit(xxxxx) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      //visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(Slice) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The number of elements that form part of the slice.
    /// </summary>
    public Expression Length {
      get { return this.length; }
    }
    readonly Expression length;

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new Slice(containingBlock, this);
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      return new DummyExpression(this.SourceLocation);
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingExpression(Expression containingExpression) {
      base.SetContainingExpression(containingExpression);
      this.collection.SetContainingExpression(this);
      this.startIndex.SetContainingExpression(this);
      this.length.SetContainingExpression(this);
    }

    /// <summary>
    /// The index of the first element from the collection that forms part of the slice.
    /// </summary>
    public Expression StartIndex {
      get { return this.startIndex; }
    }
    readonly Expression startIndex;

    /// <summary>
    /// The type of value that the expression will evaluate to, as determined at compile time.
    /// </summary>
    public override ITypeDefinition Type {
      get { return this.PlatformType.SystemString.ResolvedType; }
    }

  }

  /// <summary>
  /// An expression that results in the concatenation of the string representations of its left and right operands. In VB the &amp; operator corresponds to this expression.
  /// When overloaded, this expression corresponds to a call to op_Concatenation.
  /// </summary>
  public class StringConcatenation : BinaryOperation {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="leftOperand"></param>
    /// <param name="rightOperand"></param>
    /// <param name="sourceLocation"></param>
    public StringConcatenation(Expression leftOperand, Expression rightOperand, ISourceLocation sourceLocation)
      : base(leftOperand, rightOperand, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected StringConcatenation(BlockStatement containingBlock, StringConcatenation template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Calls the visitor.Visit(xxxxx) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      //visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(StringConcatenation) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Returns the string used to identify the operator in error messages
    /// </summary>
    protected override string OperationSymbolForErrorMessage {
      get { return "&"; }
    }


    /// <summary>
    /// Returns the name a method must have to overload this operator.
    /// </summary>
    protected override IName GetOperatorName() {
      return this.NameTable.OpConcatentation;
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new StringConcatenation(containingBlock, this);
    }

    /// <summary>
    /// A list of dummy methods that correspond to operations that are built into IL. The dummy methods are used, via overload resolution,
    /// to determine how the operands are to be converted before the operation is carried out.
    /// </summary>
    protected override IEnumerable<IMethodDefinition> StandardOperators {
      get {
        return Enumerable<IMethodDefinition>.Empty; //TODO: implement this
      }
    }

  }

  /// <summary>
  /// An expression that subtracts the value of the right operand from the value of the left operand. 
  /// When the operator is overloaded, this expression corresponds to a call to op_Subtraction.
  /// </summary>
  public class Subtraction : BinaryOperation, ISubtraction {

    /// <summary>
    /// An expression that subtracts the value of the right operand from the value of the left operand. 
    /// When the operator is overloaded, this expression corresponds to a call to op_Subtraction.
    /// </summary>
    /// <param name="leftOperand">The left operand.</param>
    /// <param name="rightOperand">The right operand.</param>
    /// <param name="sourceLocation">The source location of the operation.</param>
    public Subtraction(Expression leftOperand, Expression rightOperand, ISourceLocation sourceLocation)
      : base(leftOperand, rightOperand, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected Subtraction(BlockStatement containingBlock, Subtraction template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.flags = template.flags;
    }

    /// <summary>
    /// The subtraction must be performed with a check for arithmetic overflow if the operands are integers.
    /// </summary>
    public virtual bool CheckOverflow {
      get {
        return (this.flags & 1) != 0;
      }
    }

    /// <summary>
    /// Calls the visitor.Visit(ISubtraction) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(Subtraction) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Storage for boolean properties. 1=Use checked arithmetic.
    /// </summary>
    protected int flags;

    /// <summary>
    /// Returns the string used to identify the operator in error messages
    /// </summary>
    protected override string OperationSymbolForErrorMessage {
      get { return "-"; }
    }


    /// <summary>
    /// Returns the name a method must have to overload this operator.
    /// </summary>
    protected override IName GetOperatorName() {
      return this.NameTable.OpSubtraction;
    }

    /// <summary>
    /// Computes the compile time value of the expression. Can be null.
    /// </summary>
    protected override object/*?*/ GetValue() {
      object/*?*/ left = this.ConvertedLeftOperand.Value;
      object/*?*/ right = this.ConvertedRightOperand.Value;
      if (left == null || right == null) return null;
      switch (System.Convert.GetTypeCode(left)) {
        case TypeCode.Int32:
          //^ assume left is int && right is int;
          return (int)left - (int)right; //TODO: overflow check
        case TypeCode.UInt32:
          //^ assume left is uint && right is uint;
          return (uint)left - (uint)right; //TODO: overflow check
        case TypeCode.Int64:
          //^ assume left is long && right is long;
          return (long)left - (long)right; //TODO: overflow check
        case TypeCode.UInt64:
          //^ assume left is ulong && right is ulong;
          return (ulong)left - (ulong)right; //TODO: overflow check
        case TypeCode.Single:
          //^ assume left is float && right is float;
          return (float)left - (float)right;
        case TypeCode.Double:
          //^ assume left is double && right is double;
          return (double)left - (double)right;
        case TypeCode.Decimal:
          //^ assume left is decimal && right is decimal;
          return (decimal)left - (decimal)right;
      }
      return null;
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new Subtraction(containingBlock, this);
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      bool result = base.CheckForErrorsAndReturnTrueIfAnyAreFound();
      if (!result) {
        IMethodCall overloadMethodCall = this.OverloadMethodCall;
        if (overloadMethodCall != null) {
          IMethodDefinition/*?*/ overloadMethod = overloadMethodCall.MethodToCall.ResolvedMethod;
          if (overloadMethod is BuiltinMethodDefinition) {
            if (this.Helper.IsPointerType(this.LeftOperand.Type)) {
              if (this.Helper.GetPointerTargetType(this.LeftOperand.Type).ResolvedType.TypeCode == PrimitiveTypeCode.Void) {
                this.Helper.ReportError(new AstErrorMessage(this, Error.UndefinedOperationOnVoidPointers));
                result = true;
              }
            }
            if (this.Helper.IsPointerType(this.RightOperand.Type)) {
              if (this.Helper.GetPointerTargetType(this.RightOperand.Type).ResolvedType.TypeCode == PrimitiveTypeCode.Void) {
                this.Helper.ReportError(new AstErrorMessage(this, Error.UndefinedOperationOnVoidPointers));
                result = true;
              }
            }
          }
        }
      }
      return result;
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      // If one of the operands is a pointer and the other is a constant, scale the constant with the size of the target type
      IMethodCall/*?*/ overloadMethodCall = this.OverloadMethodCall;
      if (overloadMethodCall != null) {
        IMethodDefinition/*?*/ overloadMethod = overloadMethodCall.MethodToCall.ResolvedMethod;
        if (overloadMethod != null) {
          List<Expression> args = new List<Expression>(2);
          args.Add(this.LeftOperand);
          args.Add(this.RightOperand);
          args = this.Helper.ConvertArguments(this, args, overloadMethod.Parameters);
          if (overloadMethod.Name.UniqueKey == this.NameTable.DelegateOpDelegate.UniqueKey) {
            foreach (ITypeDefinitionMember member in this.PlatformType.SystemDelegate.ResolvedType.GetMembersNamed(this.NameTable.Remove, false)) {
              IMethodDefinition/*?*/ combine = member as IMethodDefinition;
              if (combine != null && IteratorHelper.EnumerableHasLength(combine.Parameters, 2)) {
                overloadMethod = combine;
                break;
              }
            }
            ResolvedMethodCall removeCall = new ResolvedMethodCall(overloadMethod, args, this.SourceLocation);
            removeCall.SetContainingExpression(this);
            return this.Helper.ExplicitConversion(removeCall, this.Type).ProjectAsIExpression();
          } else if (overloadMethod is BuiltinMethodDefinition) {
            if (this.Helper.IsPointerType(this.LeftOperand.Type) && !(this.Helper.IsPointerType(this.RightOperand.Type)))
              return this.ProjectAsPointerMinusIndex(args, this.Helper.GetPointerTargetType(this.LeftOperand.Type).ResolvedType);
          }
        }
      }
      return base.ProjectAsNonConstantIExpression();
    }

    /// <summary>
    /// Returns an expression corresponding to ptr - index*sizeof(T) where ptr is the first element of args, index is the second
    /// and T is targetType, the type of element that ptr points to.
    /// </summary>
    protected virtual IExpression ProjectAsPointerMinusIndex(List<Expression> args, ITypeDefinition targetType) {
      IEnumerator<Expression> argumentEnumerator = args.GetEnumerator();
      if (!argumentEnumerator.MoveNext()) return new DummyExpression(this.SourceLocation);
      Expression ptr = argumentEnumerator.Current;
      if (!argumentEnumerator.MoveNext()) return new DummyExpression(this.SourceLocation);
      Expression index = argumentEnumerator.Current;
      Expression sizeOf = new SizeOf(TypeExpression.For(targetType), index.SourceLocation);
      if (TypeHelper.IsUnsignedPrimitiveInteger(index.Type)) sizeOf = new Cast(sizeOf, TypeExpression.For(index.Type), index.SourceLocation);
      Multiplication scaledIndex = new Multiplication(index, sizeOf, sizeOf.SourceLocation);
      scaledIndex.SetContainingExpression(this);
      PointerSubtraction pointerMinusIndex = new PointerSubtraction(this, ptr, scaledIndex);
      return pointerMinusIndex;
    }

    class PointerSubtraction : CheckableSourceItem, ISubtraction {

      internal PointerSubtraction(Subtraction subtraction, Expression leftOperand, Expression rightOperand)
        : base(subtraction.SourceLocation) {
        this.subtraction = subtraction;
        this.leftOperand = leftOperand;
        this.rightOperand = rightOperand;
      }

      readonly Subtraction subtraction;

      public bool CheckOverflow {
        get { return this.subtraction.CheckOverflow; }
      }

      public override void Dispatch(ICodeVisitor visitor) {
        visitor.Visit(this);
      }

      protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
        return this.subtraction.HasErrors;
      }

      public IExpression LeftOperand {
        get { return this.leftOperand.ProjectAsIExpression(); }
      }
      readonly Expression leftOperand;

      public IExpression RightOperand {
        get { return this.rightOperand.ProjectAsIExpression(); }
      }
      readonly Expression rightOperand;

      /// <summary>
      /// If true, the left operand must be a target expression and the result of the binary operation is the
      /// value of the target expression before it is assigned the value of the operation performed on
      /// (right hand) values of the left and right operands.
      /// </summary>
      public bool ResultIsUnmodifiedLeftOperand {
        get { return false; }
      }

      public ITypeDefinition Type {
        get { return this.subtraction.Type; }
      }

      public bool TreatOperandsAsUnsignedIntegers {
        get { return this.subtraction.TreatOperandsAsUnsignedIntegers; }
      }

      #region IExpression Members

      ITypeReference IExpression.Type {
        get { return this.Type; }
      }

      #endregion
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingExpression(Expression containingExpression) {
      base.SetContainingExpression(containingExpression);
      if (containingExpression.ContainingBlock.UseCheckedArithmetic)
        this.flags |= 1;
      //Note that checked/unchecked expressions intercept this call and provide a dummy block that has the flag set appropriately.
    }

    /// <summary>
    /// A list of dummy methods that correspond to operations that are built into IL. The dummy methods are used, via overload resolution,
    /// to determine how the operands are to be converted before the operation is carried out.
    /// </summary>
    protected override IEnumerable<IMethodDefinition> StandardOperators {
      get {
        BuiltinMethods dummyMethods = this.Compilation.BuiltinMethods;
        yield return dummyMethods.Int32opInt32;
        yield return dummyMethods.UInt32opUInt32;
        yield return dummyMethods.Int64opInt64;
        yield return dummyMethods.UInt64opUInt64;
        yield return dummyMethods.Float32opFloat32;
        yield return dummyMethods.Float64opFloat64;
        yield return dummyMethods.DecimalOpDecimal;
        ITypeDefinition leftOperandType = this.LeftOperand.Type;
        ITypeDefinition rightOperandType = this.RightOperand.Type;
        if (leftOperandType.IsEnum) {
          if (rightOperandType.IsEnum)
            yield return dummyMethods.GetDummyEnumMinusEnum(rightOperandType);
          else
            yield return dummyMethods.GetDummyEnumOpNum(leftOperandType);
        } else if (rightOperandType.IsEnum)
          yield return dummyMethods.GetDummyNumOpEnum(rightOperandType); //The standard does not include this overload, but the legacy C# does.
        else if (leftOperandType.IsDelegate)
          yield return dummyMethods.GetDummyDelegateOpDelegate(leftOperandType);
        else if (rightOperandType.IsDelegate)
          yield return dummyMethods.GetDummyDelegateOpDelegate(rightOperandType);
        else if (this.Helper.IsPointerType(leftOperandType)) {
          yield return dummyMethods.GetDummyOp(leftOperandType, leftOperandType, this.PlatformType.SystemInt32.ResolvedType);
          yield return dummyMethods.GetDummyOp(leftOperandType, leftOperandType, this.PlatformType.SystemUInt32.ResolvedType);
          yield return dummyMethods.GetDummyOp(leftOperandType, leftOperandType, this.PlatformType.SystemInt64.ResolvedType);
          yield return dummyMethods.GetDummyOp(leftOperandType, leftOperandType, this.PlatformType.SystemUInt64.ResolvedType);
          yield return dummyMethods.GetDummyOp(this.PlatformType.SystemInt64.ResolvedType, leftOperandType, leftOperandType);
        }
      }
    }

    /// <summary>
    /// If true the operands must be integers and are treated as being unsigned for the purpose of the subtraction. This only makes a difference if CheckOverflow is true as well.
    /// </summary>
    public virtual bool TreatOperandsAsUnsignedIntegers {
      get { return TypeHelper.IsUnsignedPrimitiveInteger(this.ConvertedLeftOperand.Type) && TypeHelper.IsUnsignedPrimitiveInteger(this.ConvertedRightOperand.Type); }
    }
  }

  /// <summary>
  /// An expression that subtracts the value of the right operand from the value of the left operand. 
  /// The result of the expression is assigned to the left operand, which must be a target expression.
  /// When the operator is overloaded, this expression corresponds to a call to op_Subtraction.
  /// </summary>
  public class SubtractionAssignment : BinaryOperationAssignment {

    /// <summary>
    /// An expression that subtracts the value of the right operand from the value of the left operand. 
    /// The result of the expression is assigned to the left operand, which must be a target expression.
    /// When the operator is overloaded, this expression corresponds to a call to op_Subtraction.
    /// </summary>
    /// <param name="leftOperand">The left operand and target of the assignment.</param>
    /// <param name="rightOperand">The right operand.</param>
    /// <param name="sourceLocation">The source location of the operation.</param>
    public SubtractionAssignment(TargetExpression leftOperand, Expression rightOperand, ISourceLocation sourceLocation)
      : base(leftOperand, rightOperand, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected SubtractionAssignment(BlockStatement containingBlock, SubtractionAssignment template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Calls the visitor.Visit(SubtractionAssignment) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (this.ContainingBlock == containingBlock) return this;
      return new SubtractionAssignment(containingBlock, this);
    }

    /// <summary>
    /// Creates a subtraction expression with the given left operand and this.RightOperand.
    /// The method does not use this.LeftOperand.Expression, since it may be necessary to factor out any subexpressions so that
    /// they are evaluated only once. The given left operand expression is expected to be the expression that remains after factoring.
    /// </summary>
    /// <param name="leftOperand">An expression to combine with this.RightOperand into a binary expression.</param>
    protected override Expression CreateBinaryExpression(Expression leftOperand) {
      Expression result = new Subtraction(leftOperand, this.RightOperand, this.SourceLocation);
      result.SetContainingExpression(this);
      return result;
    }
  }

  ///// <summary>
  ///// Represents the value of the SwitchExpression of a SwitchStatement. Used to model VB case clauses such as "Case > 5", by rewriting it as "case switchvalue > 5"
  ///// </summary>
  //public class SwitchValue : IExpression {
  //}

  /// <summary>
  /// An expression that binds to the current object instance.
  /// </summary>
  public class ThisReference : Expression, IThisReference {

    /// <summary>
    /// Allocates an expression that binds to the current object instance.
    /// </summary>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public ThisReference(ISourceLocation sourceLocation)
      : base(sourceLocation) {
    }

    /// <summary>
    /// Allocates an expression that binds to the current object instance.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public ThisReference(BlockStatement containingBlock, ISourceLocation sourceLocation)
      : base(containingBlock, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected ThisReference(BlockStatement containingBlock, ThisReference template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Calls the visitor.Visit(IThisReference) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(ThisReference) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new ThisReference(containingBlock, this);
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      return this;
    }

    /// <summary>
    /// The type of value that the expression will evaluate to, as determined at compile time.
    /// </summary>
    public override ITypeDefinition Type {
      get {
        TypeDeclaration/*?*/ typeDeclaration = this.ContainingBlock.ContainingTypeDeclaration;
        if (typeDeclaration == null) return Dummy.Type;
        return typeDeclaration.TypeDefinition;
      }
    }

    #region IExpression Members

    ITypeReference IExpression.Type {
      get { return this.Type; }
    }

    #endregion

  }

  /// <summary>
  /// An expression that denotes a type.
  /// </summary>
  public abstract class TypeExpression : Expression {

    /// <summary>
    /// Initializes an expression that denotes a type.
    /// </summary>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    protected TypeExpression(ISourceLocation sourceLocation)
      : base(sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected TypeExpression(BlockStatement containingBlock, TypeExpression template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return this.ResolvedType is Dummy;
    }

    /// <summary>
    /// Returns a type expression that has a dummy source location and that resolves back to the given type definition.
    /// This useful for constructing AST nodes that do not correspond directly to source text. Not expected to be used by a parser.
    /// </summary>
    /// <param name="typeReference">A reference to the type to which the type expression must resolve.</param>
    public static TypeExpression For(ITypeReference typeReference) {
      return new TypeExpressionWrapper(typeReference);
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      //^ assume false;
      return new DummyExpression(this.SourceLocation);
    }

    /// <summary>
    /// A type expression that has a dummy source location and that resolves back to a given type definition.
    /// This useful for constructing AST nodes that do not correspond directly to source text. Not expected to be used by a parser.
    /// </summary>
    class TypeExpressionWrapper : TypeExpression {

      /// <summary>
      /// Allocates type expression that has a dummy source location and that resolves back to a given type definition.
      /// This useful for constructing AST nodes that do not correspond directly to source text. Not expected to be used by a parser.
      /// </summary>
      /// <param name="typeReference">A reference to the type to which the type expression must resolve.</param>
      internal TypeExpressionWrapper(ITypeReference typeReference)
        : base(SourceDummy.SourceLocation) {
        this.typeReference = typeReference;
      }

      /// <summary>
      /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
      /// </summary>
      /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
      /// <param name="template">The template to copy.</param>
      protected TypeExpressionWrapper(BlockStatement containingBlock, TypeExpressionWrapper template)
        : base(containingBlock, template)
        //^ requires template.ContainingBlock != containingBlock;
        //^ ensures this.containingBlock == containingBlock;
      {
        this.typeReference = template.typeReference; //TODO: find the corresponding type inside the compilation of the containingblock.
      }

      /// <summary>
      /// A reference to the type to which the type expression must resolve.
      /// </summary>
      readonly ITypeReference typeReference;

      /// <summary>
      /// Makes a copy of this expression, changing the ContainingBlock to the given block.
      /// </summary>
      //^ [MustOverride]
      public override Expression MakeCopyFor(BlockStatement containingBlock)
        //^^ ensures result.GetType() == this.GetType();
        //^^ ensures result.ContainingBlock == containingBlock;
      {
        if (this.ContainingBlock == containingBlock) return this;
        return new TypeExpressionWrapper(containingBlock, this);
      }

      /// <summary>
      /// The type denoted by the expression. If expression cannot be resolved, a dummy type is returned. If the expression is ambiguous the first matching type is returned.
      /// If the expression does not resolve to exactly one type, an error is added to the error collection of the compilation context.
      /// </summary>
      protected override ITypeDefinition Resolve() {
        return this.typeReference.ResolvedType;
      }
    }

    /// <summary>
    /// The type denoted by the expression. If expression cannot be resolved, a dummy type is returned. If the expression is ambiguous the first matching type is returned.
    /// If the expression does not resolve to exactly one type, an error is added to the error collection of the compilation context.
    /// </summary>
    protected abstract ITypeDefinition Resolve();

    /// <summary>
    /// Resolves the expression as a type with the given number of generic parameters. If expression cannot be resolved, a dummy type is returned. 
    /// If the expression is ambiguous the first matching type is returned.
    /// If the expression does not resolve to exactly one type, an error is added to the error collection of the compilation context.
    /// </summary>
    /// <param name="numberOfTypeParameters">The number of generic parameters the resolved type must have. This number must be greater than or equal to zero.</param>
    /// <returns>The resolved type if there is one, or Dummy.Type.</returns>
    public virtual ITypeDefinition Resolve(int numberOfTypeParameters)
      //^ requires numberOfTypeParameters >= 0;
      //^ ensures result == Dummy.Type || result.GenericParameterCount == numberOfTypeParameters;
    {
      return Dummy.Type;
    }

    /// <summary>
    /// The type denoted by the expression. If expression cannot be resolved, a dummy type is returned. If the expression is ambiguous the first matching type is returned.
    /// If the expression does not resolve to exactly one type, an error is added to the error collection of the compilation context.
    /// </summary>
    public ITypeDefinition ResolvedType {
      get {
        if (this.resolvedType == null) {
          //this.resolvedType = Dummy.Type;
          this.resolvedType = this.Resolve();
        }
        return this.resolvedType;
      }
    }
    ITypeDefinition/*?*/ resolvedType;

    /// <summary>
    /// The type of value that the expression will evaluate to, as determined at compile time.
    /// </summary>
    public sealed override ITypeDefinition Type {
      get { return this.PlatformType.SystemType.ResolvedType; }
    }

  }

  /// <summary>
  /// An expression that results in a System.Type instance.
  /// </summary>
  public class TypeOf : Expression, ITypeOf, IMetadataTypeOf {

    /// <summary>
    /// Allocates an expression that results in a System.Type instance.
    /// </summary>
    /// <param name="expression">The type that will be represented by the System.Type instance.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public TypeOf(TypeExpression expression, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.expression = expression;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected TypeOf(BlockStatement containingBlock, TypeOf template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.expression = (TypeExpression)template.expression.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// The type that will be represented by the System.Type instance.
    /// </summary>
    public TypeExpression Expression {
      get { return this.expression; }
    }
    readonly TypeExpression expression;

    /// <summary>
    /// Calls the visitor.Visit(ITypeOf) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit((ITypeOf)this);
    }

    /// <summary>
    /// Calls the visitor.Visit(TypeOf) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new TypeOf(containingBlock, this);
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      return this;
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingExpression(Expression containingExpression) {
      base.SetContainingExpression(containingExpression);
      this.expression.SetContainingExpression(this);
    }

    /// <summary>
    /// The type of value that the expression will evaluate to, as determined at compile time.
    /// </summary>
    public override ITypeDefinition Type {
      get { return Compilation.PlatformType.SystemType.ResolvedType; }
    }

    #region ITypeOf Members

    ITypeReference ITypeOf.TypeToGet {
      get { return this.Expression.ResolvedType; }
    }

    #endregion


    #region IMetadataTypeOf Members

    ITypeReference IMetadataTypeOf.TypeToGet {
      get { return this.Expression.ResolvedType; }
    }

    #endregion

    #region IMetadataExpression Members

    void IMetadataExpression.Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    ITypeReference IMetadataExpression.Type {
      get { return this.Type; }
    }

    #endregion

    #region IExpression Members

    ITypeReference IExpression.Type {
      get { return this.Type; }
    }

    #endregion
  }

  /// <summary>
  /// An expression that results in the arithmetic negation of the given operand. When overloaded, this expression corresponds to a call to op_UnaryNegation.
  /// </summary>
  public class UnaryNegation : UnaryOperation, IUnaryNegation {

    /// <summary>
    /// Allocates an expression that results in the arithmetic negation of the given operand. When overloaded, this expression corresponds to a call to op_UnaryNegation.
    /// </summary>
    /// <param name="operand">The value on which the operation is performed.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public UnaryNegation(Expression operand, ISourceLocation sourceLocation)
      : base(operand, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected UnaryNegation(BlockStatement containingBlock, UnaryNegation template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.flags = template.flags;
    }

    /// <summary>
    /// The subtraction must be performed with a check for arithmetic overflow if the operands are integers.
    /// </summary>
    public virtual bool CheckOverflow {
      get {
        return (this.flags & 1) != 0;
      }
    }

    /// <summary>
    /// Calls the visitor.Visit(IUnaryNegation) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(UnaryNegation) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Returns the user defined or standard operator method that matches this operation.
    /// </summary>
    /// <returns></returns>
    protected override MethodCall/*?*/ OverloadCall() {
      MethodCall/*?*/ overloadedMethodCall = base.OverloadCall();
      if (overloadedMethodCall != null) {
        if (overloadedMethodCall.ResolvedMethod.Name == this.NameTable.OpUInt64)
          return null;
      }
      return overloadedMethodCall;
    }

    /// <summary>
    /// Storage for boolean properties. 1=Use checked arithmetic.
    /// </summary>
    protected int flags;

    /// <summary>
    /// Returns the name a method must have to overload this operator.
    /// </summary>
    protected override IName GetOperatorName() {
      return this.NameTable.OpUnaryNegation;
    }

    /// <summary>
    /// Returns the string used to identify the operator in error messages
    /// </summary>
    protected override string OperationSymbolForErrorMessage {
      get { return "-"; }
    }

    //public override bool CouldBeInterpretedAsNegativeSignedInteger  {
    //  get { return !this.Operand.CouldBeInterpretedAsNegativeSignedInteger }
    //}

    /// <summary>
    /// Computes the compile time value of the expression. Can be null.
    /// </summary>
    protected override object/*?*/ GetValue() {
      object/*?*/ val = this.ConvertedOperand.Value;
      switch (System.Convert.GetTypeCode(val)) {
        case TypeCode.Int32:
          //^ assume val is int;
          return -(int)val;
        case TypeCode.Int64:
          //^ assume val is long;
          return -(long)val;
        case TypeCode.Single:
          //^ assume val is float;
          return -(float)val;
        case TypeCode.Double:
          //^ assume val is double;
          return -(double)val;
        case TypeCode.Decimal:
          //^ assume val is decimal;
          return -(decimal)val;
      }
      return null;
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new UnaryNegation(containingBlock, this);
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingExpression(Expression containingExpression) {
      base.SetContainingExpression(containingExpression);
      if (containingExpression.ContainingBlock.UseCheckedArithmetic)
        this.flags |= 1;
      //Note that checked/unchecked expressions intercept this call and provide a dummy block that has the flag set appropriately.
    }

    /// <summary>
    /// A list of dummy methods that correspond to operations that are built into IL. The dummy methods are used, via overload resolution,
    /// to determine how the operands are to be converted before the operation is carried out.
    /// </summary>
    protected override IEnumerable<IMethodDefinition> StandardOperators {
      get {
        BuiltinMethods dummyMethods = this.Compilation.BuiltinMethods;
        yield return dummyMethods.OpInt32;
        yield return dummyMethods.OpInt64;
        yield return dummyMethods.OpFloat32;
        yield return dummyMethods.OpFloat64;
        yield return dummyMethods.OpDecimal;
      }
    }

    #region IExpression Members

    ITypeReference IExpression.Type {
      get { return this.Type; }
    }

    #endregion

  }

  /// <summary>
  /// An operation performed on a single operand.
  /// </summary>
  public abstract class UnaryOperation : Expression, IUnaryOperation {

    /// <summary>
    /// Initializes an operation performed on a single operand.
    /// </summary>
    /// <param name="operand">The value on which the operation is performed.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    protected UnaryOperation(Expression operand, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.operand = operand;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected UnaryOperation(BlockStatement containingBlock, UnaryOperation template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.operand = template.operand.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// 
    /// </summary>
    public Expression ConvertedOperand {
      get {
        MethodCall/*?*/ overloadedCall = this.OverloadCall();
        if (overloadedCall != null) {
          IEnumerator<Expression> args = overloadedCall.ConvertedArguments.GetEnumerator();
          args.MoveNext();
          return args.Current;
        }
        return new DummyExpression(this.Operand.SourceLocation);
      }
    }

    /// <summary>
    /// True if the constant is a positive integer that could be interpreted as a negative signed integer.
    /// For example, 0x80000000, could be interpreted as a convenient way of writing int.MinValue.
    /// </summary>
    public override bool CouldBeInterpretedAsNegativeSignedInteger {
      get { return this.Operand.CouldBeInterpretedAsNegativeSignedInteger; }
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// </summary>
    /// <returns></returns>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return this.Operand.HasErrors || this.Type is Dummy;
    }

    /// <summary>
    /// Returns a method call object that calls the given overloadMethod with this.LeftOperand and this.RightOperands as arguments.
    /// The operands are converted to the corresponding parameter types using implicit conversions.
    /// If overloadMethod is the Dummy.Method a DummyMethodCall is returned.
    /// </summary>
    /// <param name="overloadMethod">A user defined operator overload method or a "builtin" operator overload method, or a dummy method.
    /// The latter can be supplied when the expression is in error because one or both of the arguments cannot be converted the correct parameter type for a valid overload.</param>
    /// <returns></returns>
    protected virtual MethodCall CreateOverloadMethodCall(IMethodDefinition overloadMethod) {
      if (overloadMethod is Dummy)
        return new DummyMethodCall(this);
      else {
        List<Expression> args = new List<Expression>(1);
        args.Add(this.Operand);
        args = this.Helper.ConvertArguments(this, args, overloadMethod.Parameters);
        ResolvedMethodCall overloadMethodCall = new ResolvedMethodCall(overloadMethod, args, this.SourceLocation);
        overloadMethodCall.SetContainingExpression(this);
        return overloadMethodCall;
      }
    }

    /// <summary>
    /// Returns an enumeration of methods that overload this operator. 
    /// If no user defined methods exists, it returns a list of dummy methods that correspond to operators built into IL.
    /// </summary>
    /// <param name="operandType">The type of the operand.</param>
    protected virtual IEnumerable<IMethodDefinition> GetOperatorMethods(ITypeDefinition operandType) {
      IEnumerable<IMethodDefinition> userOperators = this.GetUserDefinedOperators(operandType);
      if (userOperators.GetEnumerator().MoveNext()) return userOperators;
      return this.StandardOperators;
    }

    /// <summary>
    /// Returns the name a method must have to overload this operator.
    /// </summary>
    protected abstract IName GetOperatorName();

    /// <summary>
    /// Returns the string used to identify the operator in error messages
    /// </summary>
    protected abstract string OperationSymbolForErrorMessage { get; }

    /// <summary>
    /// Returns an error message stating that the operand of this operation is not of the right type for the operator.
    /// </summary>
    protected IErrorMessage GetUnaryBadOperandErrorMessage() {
      string operandTypeName = this.Helper.GetTypeName(this.Operand.Type);
      return new AstErrorMessage(this, Error.BadUnaryOperation, this.OperationSymbolForErrorMessage, operandTypeName);
    }

    /// <summary>
    /// Returns an enumeration of user defined methods that overload this operator, defined by the given type or one of its base classes.
    /// </summary>
    /// <param name="type">The type whose user defined operator methods should be searched.</param>
    protected virtual IEnumerable<IMethodDefinition> GetUserDefinedOperators(ITypeDefinition type) {
      IEnumerable<IMethodDefinition> operatorMethods = this.GetNonDerivedUserDefinedOperators(type);
      if (!operatorMethods.GetEnumerator().MoveNext()) {
        foreach (ITypeReference baseClassReference in type.BaseClasses)
          return this.GetUserDefinedOperators(baseClassReference.ResolvedType);
      }
      return operatorMethods;
    }

    /// <summary>
    /// Returns an enumeration of user defined methods that overload this operator, defined directly by the given type.
    /// </summary>
    /// <param name="type">The type whose user defined operator methods should be searched.</param>
    protected virtual IEnumerable<IMethodDefinition> GetNonDerivedUserDefinedOperators(ITypeDefinition type) {
      type = this.Helper.RemoveNullableWrapper(type);
      foreach (ITypeDefinitionMember member in type.GetMembersNamed(this.GetOperatorName(), false)) {
        IMethodDefinition/*?*/ method = member as IMethodDefinition;
        if (method == null || !method.IsStatic) continue;
        IEnumerator<IParameterDefinition> paramEnumerator = method.Parameters.GetEnumerator();
        if (!paramEnumerator.MoveNext()) continue;
        if (!this.Helper.ImplicitConversionExists(this.Operand, paramEnumerator.Current.Type.ResolvedType)) continue;
        if (paramEnumerator.MoveNext()) continue;
        yield return method;
      }
    }

    /// <summary>
    /// Checks if the expression has a side effect and reports an error unless told otherwise.
    /// </summary>
    /// <param name="reportError">If true, report an error if the expression has a side effect.</param>
    public override bool HasSideEffect(bool reportError) {
      return this.Operand.HasSideEffect(reportError);
    }

    /// <summary>
    /// Infers the type of value that this expression will evaluate to. At runtime the actual value may be an instance of subclass of the result of this method.
    /// Calling this method does not cache the computed value and does not generate any error messages. In some cases, such as references to the parameters of lambda
    /// expressions during type overload resolution, the value returned by this method may be different from one call to the next.
    /// When type inference fails, Dummy.Type is returned.
    /// </summary>
    public override ITypeDefinition InferType() {
      MethodCall/*?*/ overloadCall = this.OverloadCall();
      if (overloadCall != null) return overloadCall.Type;
      //The operation has a bad operand that cannot be matched to a single overload method. For example + string.
      if (!this.Operand.HasErrors) {
        this.Helper.ReportError(this.GetUnaryBadOperandErrorMessage());
      }
      return Dummy.Type;
    }

    /// <summary>
    /// Returns the user defined operator overload method, or a dummy method corresponding to an IL operation, that best
    /// matches the operand type of this operation.
    /// </summary>
    protected virtual IMethodDefinition LookForOverloadMethod() {
      IEnumerable<IMethodDefinition> candidateOperators = this.GetOperatorMethods(this.Operand.Type);
      return this.Helper.ResolveOverload(candidateOperators, this.Operand);
    }

    /// <summary>
    /// The value on which the operation is performed.
    /// </summary>
    public Expression Operand {
      get { return this.operand; }
    }
    readonly Expression operand;

    /// <summary>
    /// Returns the user defined or standard operator method that matches this operation.
    /// </summary>
    protected virtual MethodCall/*?*/ OverloadCall() {
      MethodCall/*?*/ call = this.OverloadMethodCall;
      if (call == null) call = this.overloadMethodCall;
      if (call is DummyMethodCall) return null;
      return call;
    }

    /// <summary>
    /// An expression that calls the user defined operator overload method that best matches the operand type of this operation.
    /// If no such method can be found, the value of this property is null.
    /// </summary>
    public MethodCall/*?*/ OverloadMethodCall {
      get {
        if (this.overloadMethodCall == null) {
          lock (GlobalLock.LockingObject) {
            if (this.overloadMethodCall == null) {
              IMethodDefinition overloadMethod = this.LookForOverloadMethod();
              this.overloadMethodCall = this.CreateOverloadMethodCall(overloadMethod);
            }
          }
        }
        //^ assert this.overloadMethodCall != null;
        if (this.overloadMethodCall is DummyMethodCall) return null;
        return this.overloadMethodCall;
      }
    }
    MethodCall/*?*/ overloadMethodCall;

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      if (!this.HasErrors) {
        MethodCall/*?*/ overloadedMethodCall = this.OverloadMethodCall;
        if (overloadedMethodCall != null) {
          if (!(overloadedMethodCall.ResolvedMethod is BuiltinMethodDefinition))
            return overloadedMethodCall;
        }
      }
      return this;
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingExpression(Expression containingExpression) {
      base.SetContainingExpression(containingExpression);
      this.operand.SetContainingExpression(this);
    }

    /// <summary>
    /// A list of dummy methods that correspond to operations that are built into IL. The dummy methods are used, via overload resolution,
    /// to determine how the operands are to be converted before the operation is carried out.
    /// </summary>
    protected abstract IEnumerable<IMethodDefinition> StandardOperators {
      get;
    }

    /// <summary>
    /// The type of value that the expression will evaluate to, as determined at compile time.
    /// </summary>
    public sealed override ITypeDefinition Type {
      get {
        if (this.type == null)
          this.type = this.InferType();
        return this.type;
      }
    }
    //^ [Once]
    ITypeDefinition/*?*/ type;

    /// <summary>
    /// Returns true if the expression represents a compile time constant without an explicitly specified type. For example, 1 rather than 1L.
    /// Constant expressions such as 2*16 are polymorhpic if both operands are polymorhic.
    /// </summary>
    public override bool ValueIsPolymorphicCompileTimeConstant {
      get { return this.Operand.ValueIsPolymorphicCompileTimeConstant; }
    }

    #region IUnaryOperation Members

    IExpression IUnaryOperation.Operand {
      get { return this.ConvertedOperand.ProjectAsIExpression(); }
    }

    #endregion

    #region IExpression Members

    ITypeReference IExpression.Type {
      get { return this.Type; }
    }

    #endregion
  }

  /// <summary>
  /// An operation performed on a single operand and that also assigns a new value to the memory location represented by the expression.
  /// </summary>
  public abstract class UnaryOperationAssignment : UnaryOperation {

    /// <summary>
    /// Initializes an operation performed on a single operand and that also assigns a new value to the memory location represented by the expression.
    /// </summary>
    /// <param name="target">The value on which the operation is performed.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    protected UnaryOperationAssignment(TargetExpression target, ISourceLocation sourceLocation)
      : base(target.Expression, sourceLocation) {
      this.target = target;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected UnaryOperationAssignment(BlockStatement containingBlock, UnaryOperationAssignment template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.target = (TargetExpression)template.target.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// Calls this.ProjectAsIExpression().Dispatch(visitor);
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      this.ProjectAsIExpression().Dispatch(visitor);
    }

    /// <summary>
    /// Returns a call to the operator overload method, using the factored version of this.Operand.
    /// </summary>
    /// <param name="operand">The factored version of this.Operand.</param>
    /// <param name="overloadMethod">The operator overload method.</param>
    protected ResolvedMethodCall FactoredOverloadCall(Expression operand, IMethodDefinition overloadMethod) {
      List<Expression> args = new List<Expression>(1);
      args.Add(operand);
      args = this.Helper.ConvertArguments(this, args, overloadMethod.Parameters);
      return new ResolvedMethodCall(overloadMethod, args, this.SourceLocation);
    }

    /// <summary>
    /// Checks if the expression has a side effect and reports an error unless told otherwise.
    /// </summary>
    /// <param name="reportError">If true, report an error if the expression has a side effect.</param>
    public override bool HasSideEffect(bool reportError) {
      if (this.hasSideEffect == null) {
        if (reportError) {
          this.Helper.ReportError(new AstErrorMessage(this, Error.ExpressionHasSideEffect));
          this.hasSideEffect = true;
        }
        return true;
      }
      return this.hasSideEffect.Value;
    }
    bool? hasSideEffect;

    /// <summary>
    /// Returns a constant one to be used in pre-/post-fix increment/decrement operations
    /// </summary>
    /// <param name="targetType"></param>
    /// <returns></returns>
    protected object GetConstantOneOfMatchingTypeForIncrementDecrement(ITypeDefinition targetType) {
      if (TypeHelper.TypesAreEquivalent(targetType, this.PlatformType.SystemChar))
        return (char)1;
      else if (TypeHelper.TypesAreEquivalent(targetType, this.PlatformType.SystemInt8))
        return (sbyte)1;
      else if (TypeHelper.TypesAreEquivalent(targetType, this.PlatformType.SystemUInt8))
        return (byte)1;
      else if (TypeHelper.TypesAreEquivalent(targetType, this.PlatformType.SystemInt16))
        return (short)1;
      else if (TypeHelper.TypesAreEquivalent(targetType, this.PlatformType.SystemUInt16))
        return (ushort)1;
      return 1;
    }

    /// <summary>
    /// A list of dummy methods that correspond to operations that are built into IL. The dummy methods are used, via overload resolution,
    /// to determine how the operands are to be converted before the operation is carried out.
    /// </summary>
    protected override IEnumerable<IMethodDefinition> StandardOperators {
      get {
        BuiltinMethods dummyMethods = this.Compilation.BuiltinMethods;
        yield return dummyMethods.OpInt8;
        yield return dummyMethods.OpUInt8;
        yield return dummyMethods.OpInt16;
        yield return dummyMethods.OpUInt16;
        yield return dummyMethods.OpInt32;
        yield return dummyMethods.OpUInt32;
        yield return dummyMethods.OpInt64;
        yield return dummyMethods.OpUInt64;
        yield return dummyMethods.OpChar;
        yield return dummyMethods.OpFloat32;
        yield return dummyMethods.OpFloat64;
        yield return dummyMethods.OpDecimal;
        ITypeDefinition operandType = this.Operand.Type;
        if (operandType.IsEnum)
          yield return dummyMethods.GetDummyOpEnum(operandType);
        else if (this.Helper.IsPointerType(operandType))
          yield return dummyMethods.GetDummyOp(operandType, operandType);
      }
    }

    /// <summary>
    /// An assignable entity that contains the value to be operated upon and that will be updated by the assignment.
    /// </summary>
    public TargetExpression Target {
      get { return this.target; }
    }
    readonly TargetExpression target;

  }

  /// <summary>
  /// An expression that results in the arithmetic value of the given operand. When overloaded, this expression corresponds to a call to op_UnaryPlus.
  /// </summary>
  public class UnaryPlus : UnaryOperation, IUnaryPlus {

    /// <summary>
    /// Allocates an expression that results in the arithmetic value of the given operand. When overloaded, this expression corresponds to a call to op_UnaryPlus.
    /// </summary>
    /// <param name="operand">The value on which the operation is performed.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public UnaryPlus(Expression operand, ISourceLocation sourceLocation)
      : base(operand, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected UnaryPlus(BlockStatement containingBlock, UnaryPlus template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Calls the visitor.Visit(IUnaryPlus) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(UnaryPlus) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Returns the name a method must have to overload this operator.
    /// </summary>
    protected override IName GetOperatorName() {
      return this.NameTable.OpUnaryPlus;
    }

    /// <summary>
    /// Returns the string used to identify the operator in error messages
    /// </summary>
    protected override string OperationSymbolForErrorMessage {
      get { return "+"; }
    }

    /// <summary>
    /// Computes the compile time value of the expression. Can be null.
    /// </summary>
    protected override object/*?*/ GetValue() {
      return this.ConvertedOperand.Value;
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new UnaryPlus(containingBlock, this);
    }

    /// <summary>
    /// A list of dummy methods that correspond to operations that are built into IL. The dummy methods are used, via overload resolution,
    /// to determine how the operands are to be converted before the operation is carried out.
    /// </summary>
    protected override IEnumerable<IMethodDefinition> StandardOperators {
      get {
        BuiltinMethods dummyMethods = this.Compilation.BuiltinMethods;
        yield return dummyMethods.OpInt32;
        yield return dummyMethods.OpUInt32;
        yield return dummyMethods.OpInt64;
        yield return dummyMethods.OpUInt64;
        yield return dummyMethods.OpFloat32;
        yield return dummyMethods.OpFloat64;
        yield return dummyMethods.OpDecimal;
      }
    }

    #region IExpression Members

    ITypeReference IExpression.Type {
      get { return this.Type; }
    }

    #endregion

  }

  /// <summary>
  /// An expression that wraps an inner expression and causes the inner expression to be evaluated using arithmetic operators that do not check for overflow (by using modulo arithmentic).
  /// </summary>
  public class UncheckedExpression : Expression {

    /// <summary>
    /// An expression that wraps an inner expression and causes the inner expression to be evaluated using arithmetic operators that do not check for overflow (by using modulo arithmentic).
    /// </summary>
    /// <param name="operand">The expression to evaluate while ignoring arithmetic overflow.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public UncheckedExpression(Expression operand, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.operand = operand;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected UncheckedExpression(BlockStatement containingBlock, UncheckedExpression template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
      this.operand = template.operand.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return this.Operand.HasErrors;
    }

    /// <summary>
    /// True if the constant is a positive integer that could be interpreted as a negative signed integer.
    /// For example, 0x80000000, could be interpreted as a convenient way of writing int.MinValue.
    /// </summary>
    public override bool CouldBeInterpretedAsNegativeSignedInteger {
      get { return this.Operand.CouldBeInterpretedAsNegativeSignedInteger; }
    }

    /// <summary>
    /// Calls the visitor.Visit(UncheckedExpression) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Computes the compile time value of the expression. Can be null.
    /// </summary>
    protected override object/*?*/ GetValue() {
      return this.Operand.Value;
    }

    /// <summary>
    /// Checks if the expression has a side effect and reports an error unless told otherwise.
    /// </summary>
    /// <param name="reportError">If true, report an error if the expression has a side effect.</param>
    public override bool HasSideEffect(bool reportError) {
      return this.Operand.HasSideEffect(reportError);
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      if (containingBlock.UseCheckedArithmetic) {
        BlockStatement dummyContainingBlock =  BlockStatement.CreateDummyFor(BlockStatement.Options.UseUncheckedArithmetic, this.SourceLocation);
        dummyContainingBlock.SetContainingBlock(containingBlock);
        containingBlock = dummyContainingBlock;
      }
      return new UncheckedExpression(containingBlock, this);
    }

    /// <summary>
    /// The value on which the operation is performed.
    /// </summary>
    public Expression Operand {
      get { return this.operand; }
    }
    readonly Expression operand;

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    protected override IExpression ProjectAsNonConstantIExpression() {
      return this.Operand.ProjectAsIExpression();
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingExpression(Expression containingExpression) {
      BlockStatement dummyContainingBlock = BlockStatement.CreateDummyFor(BlockStatement.Options.UseUncheckedArithmetic, this.SourceLocation);
      dummyContainingBlock.SetContainingBlock(containingExpression.ContainingBlock);
      base.SetContainingBlock(dummyContainingBlock);
      this.Operand.SetContainingExpression(this);
    }

    /// <summary>
    /// The type of value that the expression will evaluate to, as determined at compile time.
    /// </summary>
    public override ITypeDefinition Type {
      get { return this.Operand.Type; }
    }

    /// <summary>
    /// Returns true if the expression represents a compile time constant without an explicitly specified type. For example, 1 rather than 1L.
    /// Constant expressions such as 2*16 are polymorhpic if both operands are polymorhic.
    /// </summary>
    public override bool ValueIsPolymorphicCompileTimeConstant {
      get { return this.Operand.ValueIsPolymorphicCompileTimeConstant; }
    }

  }

  /// <summary>
  /// An expression that results in the value of the left operand, shifted right by the number of bits specified by the value of the right operand, filling with zeros.
  /// This is for >>>.
  /// </summary>
  public class UnsignedRightShift : BinaryOperation {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="leftOperand"></param>
    /// <param name="rightOperand"></param>
    /// <param name="sourceLocation"></param>
    public UnsignedRightShift(Expression leftOperand, Expression rightOperand, ISourceLocation sourceLocation)
      : base(leftOperand, rightOperand, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected UnsignedRightShift(BlockStatement containingBlock, UnsignedRightShift template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new UnsignedRightShift(containingBlock, this);
    }

    /// <summary>
    /// Calls the visitor.Visit(xxxxx) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      //visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(UnsignedRightShift) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Returns the string used to identify the operator in error messages
    /// </summary>
    protected override string OperationSymbolForErrorMessage {
      get { return ">>>"; }
    }


    /// <summary>
    /// Returns the name a method must have to overload this operator.
    /// </summary>
    protected override IName GetOperatorName() {
      return this.NameTable.OpRightShift;
    }

    /// <summary>
    /// A list of dummy methods that correspond to operations that are built into IL. The dummy methods are used, via overload resolution,
    /// to determine how the operands are to be converted before the operation is carried out.
    /// </summary>
    protected override IEnumerable<IMethodDefinition> StandardOperators {
      get {
        return Enumerable<IMethodDefinition>.Empty; //TODO: implement this
      }
    }

  }

  /// <summary>
  /// An expression that results in the value of the left operand, shifted right by the number of bits specified by the value of the right operand, filling with zeros.
  /// The result of the expression is assigned to the left operand, which must be a target expression.
  /// This is for >>>=.
  /// </summary>
  public class UnsignedRightShiftAssignment : BinaryOperationAssignment {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="leftOperand"></param>
    /// <param name="rightOperand"></param>
    /// <param name="sourceLocation"></param>
    public UnsignedRightShiftAssignment(TargetExpression leftOperand, Expression rightOperand, ISourceLocation sourceLocation)
      : base(leftOperand, rightOperand, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected UnsignedRightShiftAssignment(BlockStatement containingBlock, UnsignedRightShiftAssignment template)
      : base(containingBlock, template)
      //^ requires template.ContainingBlock != containingBlock;
      //^ ensures this.containingBlock == containingBlock;
    {
    }

    /// <summary>
    /// Calls the visitor.Visit(xxxxx) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      //visitor.Visit(this);
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Expression MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingBlock == containingBlock;
    {
      if (this.ContainingBlock == containingBlock) return this;
      return new UnsignedRightShiftAssignment(containingBlock, this);
    }

    /// <summary>
    /// Creates an unsigned right shift expression with the given left operand and this.RightOperand.
    /// The method does not use this.LeftOperand.Expression, since it may be necessary to factor out any subexpressions so that
    /// they are evaluated only once. The given left operand expression is expected to be the expression that remains after factoring.
    /// </summary>
    /// <param name="leftOperand">An expression to combine with this.RightOperand into a binary expression.</param>
    protected override Expression CreateBinaryExpression(Expression leftOperand) {
      Expression result = new RightShift(leftOperand, this.RightOperand, this.SourceLocation);
      result.SetContainingExpression(this);
      return result;
    }
  }


  /// <summary>
  /// 
  /// </summary>
  internal sealed class VectorLength : Expression, IVectorLength {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="vector"></param>
    /// <param name="sourceLocation"></param>
    internal VectorLength(Expression vector, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.vector = vector;
    }

    /// <summary>
    /// Calls the visitor.Visit(IVectorLength) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Makes a copy of this expression, changing the ContainingBlock to the given block.
    /// </summary>
    /// <param name="containingBlock"></param>
    /// <returns></returns>
    public override Expression MakeCopyFor(BlockStatement containingBlock) {
      Debug.Assert(false);
      return this;
    }

    /// <summary>
    /// Returns an object that implements IExpression and that represents this expression after language specific rules have been
    /// applied to it in order to determine its semantics. The resulting expression is a standard representation of the semantics
    /// of this expression, suitable for use by language agnostic clients and complete enough for translation of the expression
    /// into IL.
    /// </summary>
    /// <returns></returns>
    protected override IExpression ProjectAsNonConstantIExpression() {
      return this;
    }

    /// <summary>
    /// The type of value that the expression will evaluate to, as determined at compile time.
    /// </summary>
    /// <value></value>
    public override ITypeDefinition Type {
      get { return this.Vector.Type.PlatformType.SystemIntPtr.ResolvedType; }
    }

    /// <summary>
    /// An expression that results in a value of a vector (zero-based one-dimensional array) type.
    /// </summary>
    /// <value></value>
    public Expression Vector {
      get { return this.vector; }
    }
    readonly Expression vector;


    #region IVectorLength Members

    IExpression IVectorLength.Vector {
      get { return this.Vector.ProjectAsIExpression(); }
    }

    #endregion

    #region IExpression Members

    ITypeReference IExpression.Type {
      get { return this.Type; }
    }

    #endregion
  }

  /// <summary>
  /// 
  /// </summary>
  public static class NamespaceHelper {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="nameTable"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static Expression CreateInSystemDiagnosticsContractsCodeContractExpr(INameTable nameTable, string name) {
      SimpleName System = new SimpleName(nameTable.GetNameFor("System"), SourceDummy.SourceLocation, false);
      SimpleName Diagnostics = new SimpleName(nameTable.GetNameFor("Diagnostics"), SourceDummy.SourceLocation, false);
      SimpleName Contracts = new SimpleName(nameTable.GetNameFor("Contracts"), SourceDummy.SourceLocation, false);
      SimpleName CodeContract = new SimpleName(nameTable.GetNameFor("CodeContract"), SourceDummy.SourceLocation, false);
      SimpleName sName = new SimpleName(nameTable.GetNameFor(name), SourceDummy.SourceLocation, false);
      Expression SystemDiagnostics = new QualifiedName(System, Diagnostics, SourceDummy.SourceLocation);
      Expression SystemDiagnosticsContracts = new QualifiedName(SystemDiagnostics, Contracts, SourceDummy.SourceLocation);
      Expression SystemDiagnosticsContractsCodeContract = new QualifiedName(SystemDiagnosticsContracts, CodeContract, SourceDummy.SourceLocation);
      return new QualifiedName(SystemDiagnosticsContractsCodeContract, sName, SourceDummy.SourceLocation);
    }

    /// <summary>
    /// 
    /// </summary>
    public static string SystemDiagnosticsContractsCodeContractString {
      get { return "System.Diagnostics.Contracts.CodeContract"; }
    }
  }
}
