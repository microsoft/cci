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
using System.Text;
using Microsoft.Cci;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CSharpSourceEmitter {
  public partial class SourceEmitter : CodeTraverser, ICSharpSourceEmitter {

    public override void TraverseChildren(IAddition addition) {
      this.sourceEmitterOutput.Write("(");
      this.Traverse(addition.LeftOperand);
      this.sourceEmitterOutput.Write(" + ");
      this.Traverse(addition.RightOperand);
      this.sourceEmitterOutput.Write(")");
    }

    public override void TraverseChildren(IAddressableExpression addressableExpression) {
      ILocalDefinition/*?*/ local = addressableExpression.Definition as ILocalDefinition;
      if (local != null) {
        this.PrintLocalName(local);
        return;
      }
      IParameterDefinition/*?*/ param = addressableExpression.Definition as IParameterDefinition;
      if (param != null) {
        this.PrintParameterDefinitionName(param);
        return;
      }
      IArrayIndexer/*?*/ arrayIndexer = addressableExpression.Definition as IArrayIndexer;
      if (arrayIndexer != null) {
        this.Traverse(arrayIndexer);
        return;
      }
      IAddressDereference/*?*/ addressDereference = addressableExpression.Definition as IAddressDereference;
      if (addressDereference != null) {
        this.Traverse(addressDereference);
        return;
      }
      if (addressableExpression.Instance != null) {
        var addrOf = addressableExpression.Instance as IAddressOf;
        if (addrOf != null && addrOf.Expression.Type.IsValueType)
          this.Traverse(addrOf.Expression);
        else
          this.Traverse(addressableExpression.Instance);
        this.sourceEmitterOutput.Write(".");
      }
      IFieldReference/*?*/ field = addressableExpression.Definition as IFieldReference;
      if (field != null) {
        if (addressableExpression.Instance == null) {
          this.PrintTypeReferenceName(field.ContainingType);
          this.sourceEmitterOutput.Write(".");
        }
        this.sourceEmitterOutput.Write(field.Name.Value);
        return;
      }
      IMethodReference/*?*/ method = addressableExpression.Definition as IMethodReference;
      if (method != null) {
        this.sourceEmitterOutput.Write(MemberHelper.GetMethodSignature(method, NameFormattingOptions.Signature));
        return;
      }
      Debug.Assert(addressableExpression.Definition is IThisReference);
      this.sourceEmitterOutput.Write("this");
    }

    public override bool Equals(object obj) {
      return base.Equals(obj);
    }

    public override int GetHashCode() {
      return base.GetHashCode();
    }

    public override string ToString() {
      return base.ToString();
    }

    public override void TraverseChildren(IAddressDereference addressDereference) {
      if (addressDereference.Address.Type is IPointerTypeReference || addressDereference.Address.Type.TypeCode == PrimitiveTypeCode.IntPtr)
        this.sourceEmitterOutput.Write("*");
      else {
        var addrOf = addressDereference.Address as IAddressOf;
        if (addrOf != null) {
          this.Traverse(addrOf.Expression);
          return;
        }
      }
      this.Traverse(addressDereference.Address);
    }

    public override void TraverseChildren(IAddressOf addressOf) {
      this.sourceEmitterOutput.Write("&");
      this.Traverse(addressOf.Expression);
    }

    public override void TraverseChildren(IAliasForType aliasForType) {
      // Already outputted these at the top for IAssembly
    }

    public override void TraverseChildren(IAnonymousDelegate anonymousDelegate) {
      if (IteratorHelper.EnumerableHasLength(anonymousDelegate.Body.Statements, 1)) {
        var returnStatement = IteratorHelper.Single(anonymousDelegate.Body.Statements) as IReturnStatement;
        if (returnStatement != null && returnStatement.Expression != null) {
          this.Traverse(anonymousDelegate.Parameters);
          this.sourceEmitterOutput.Write(" => ");
          this.Traverse(returnStatement.Expression);
          return;
        }
      }
      this.sourceEmitterOutput.Write("delegate ");
      this.Traverse(anonymousDelegate.Parameters);
      this.sourceEmitterOutput.WriteLine(" {");
      this.sourceEmitterOutput.IncreaseIndent();
      this.Traverse(anonymousDelegate.Body.Statements);
      this.sourceEmitterOutput.DecreaseIndent();
      this.sourceEmitterOutput.Write("}", true);
    }

    public override void TraverseChildren(IArrayIndexer arrayIndexer) {
      this.Traverse(arrayIndexer.IndexedObject);
      this.sourceEmitterOutput.Write("[");
      this.Traverse(arrayIndexer.Indices);
      this.sourceEmitterOutput.Write("]");
    }

    public override void TraverseChildren(IArrayTypeReference arrayTypeReference) {
      base.TraverseChildren(arrayTypeReference);
    }

    public override void TraverseChildren(IAssembly assembly) {
      foreach (var attr in assembly.Attributes) {
        var at = Utils.GetAttributeType(attr);
        if (at == SpecialAttribute.Extension ||
          at == SpecialAttribute.AssemblyDelaySign ||
          at == SpecialAttribute.AssemblyKeyFile)
          continue;
        PrintAttribute(assembly, attr, true, "assembly");
      }

      // Assembly-level pseudo-custom attributes
      foreach (var alias in assembly.ExportedTypes) {
        // Nested type are automatically included
        if (!(alias.AliasedType is INestedTypeReference)) {
          sourceEmitterOutput.Write("[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(");
          PrintTypeReference(alias.AliasedType);
          sourceEmitterOutput.WriteLine("))]");
        }
      }
      PrintToken(CSharpToken.NewLine);
      base.TraverseChildren(assembly);
    }

    public override void TraverseChildren(IAssemblyReference assemblyReference) {
      base.TraverseChildren(assemblyReference);
    }

    public override void TraverseChildren(IAssignment assignment) {
      this.Traverse(assignment.Target);
      this.PrintToken(CSharpToken.Space);
      this.PrintToken(CSharpToken.Assign);
      this.PrintToken(CSharpToken.Space);
      this.Traverse(assignment.Source);
    }

    public override void TraverseChildren(IBitwiseAnd bitwiseAnd) {
      this.sourceEmitterOutput.Write("(");
      this.Traverse(bitwiseAnd.LeftOperand);
      this.sourceEmitterOutput.Write(" & ");
      this.Traverse(bitwiseAnd.RightOperand);
      this.sourceEmitterOutput.Write(")");
    }

    public override void TraverseChildren(IBitwiseOr bitwiseOr) {
      this.sourceEmitterOutput.Write("(");
      this.Traverse(bitwiseOr.LeftOperand);
      this.sourceEmitterOutput.Write(" | ");
      this.Traverse(bitwiseOr.RightOperand);
      this.sourceEmitterOutput.Write(")");
    }

    public override void TraverseChildren(IBlockExpression blockExpression) {
      this.sourceEmitterOutput.WriteLine("(() => {");
      this.sourceEmitterOutput.IncreaseIndent();
      this.Traverse(blockExpression.BlockStatement);
      this.sourceEmitterOutput.Write("return ", true);
      this.Traverse(blockExpression.Expression);
      this.sourceEmitterOutput.Write("; })()");
      this.sourceEmitterOutput.DecreaseIndent();
    }

    public override void TraverseChildren(IBoundExpression boundExpression) {
      if (boundExpression.Instance != null) {
        IAddressOf/*?*/ addressOf = boundExpression.Instance as IAddressOf;
        if (addressOf != null && addressOf.Expression.Type.IsValueType) {
          this.Traverse(addressOf.Expression);
        } else {
          this.Traverse(boundExpression.Instance);
        }
        this.PrintToken(CSharpToken.Dot);
      }
      ILocalDefinition/*?*/ local = boundExpression.Definition as ILocalDefinition;
      if (local != null)
        this.PrintLocalName(local);
      else {
        IFieldReference/*?*/ fr = boundExpression.Definition as IFieldReference;
        if (fr != null) {
          if (boundExpression.Instance == null) {
            this.PrintTypeReferenceName(fr.ContainingType);
            this.sourceEmitterOutput.Write(".");
          }
          this.sourceEmitterOutput.Write(fr.Name.Value);
        } else {
          INamedEntity/*?*/ ne = boundExpression.Definition as INamedEntity;
          if (ne != null)
            this.sourceEmitterOutput.Write(ne.Name.Value);
        }
      }
    }

    public override void TraverseChildren(ICastIfPossible castIfPossible) {
      this.sourceEmitterOutput.Write("(");
      this.Traverse(castIfPossible.ValueToCast);
      this.sourceEmitterOutput.Write(" as ");
      this.PrintTypeReference(castIfPossible.TargetType);
      this.sourceEmitterOutput.Write(")");
    }

    public override void TraverseChildren(ICheckIfInstance checkIfInstance) {
      this.sourceEmitterOutput.Write("(");
      this.Traverse(checkIfInstance.Operand);
      this.sourceEmitterOutput.Write(" is ");
      this.PrintTypeReference(checkIfInstance.TypeToCheck);
      this.sourceEmitterOutput.Write(")");
    }

    public override void TraverseChildren(ICompileTimeConstant constant) {
      if (constant.Value == null)
        this.PrintToken(CSharpToken.Null);
      else if (constant.Value is bool)
        this.sourceEmitterOutput.Write(((bool)constant.Value) ? "true" : "false");
      else if (constant.Value is string)
        this.PrintString((string)constant.Value);
      else if (constant.Value is char)
        this.sourceEmitterOutput.Write("'"+constant.Value+"'");
      else
        this.sourceEmitterOutput.Write(constant.Value.ToString());
    }

    public override void TraverseChildren(IConditional conditional) {
      if (conditional.Type.TypeCode == PrimitiveTypeCode.Boolean) {
        if (ExpressionHelper.IsIntegralOne(conditional.ResultIfTrue)) {
          this.sourceEmitterOutput.Write("(");
          this.Traverse(conditional.Condition);
          this.sourceEmitterOutput.Write(" || ");
          this.Traverse(conditional.ResultIfFalse);
          this.sourceEmitterOutput.Write(")");
          return;
        }
        if (ExpressionHelper.IsIntegralZero(conditional.ResultIfFalse)) {
          this.sourceEmitterOutput.Write("(");
          this.Traverse(conditional.Condition);
          this.sourceEmitterOutput.Write(" && ");
          this.Traverse(conditional.ResultIfTrue);
          this.sourceEmitterOutput.Write(")");
          return;
        }
        if (ExpressionHelper.IsIntegralZero(conditional.ResultIfTrue)) {
          this.sourceEmitterOutput.Write("!(");
          this.Traverse(conditional.Condition);
          this.sourceEmitterOutput.Write(" || ");
          var ln = conditional.ResultIfFalse as ILogicalNot;
          if (ln != null)
            this.Traverse(ln.Operand);
          else {
            this.sourceEmitterOutput.Write("!");
            this.Traverse(conditional.ResultIfFalse);
          }
          this.sourceEmitterOutput.Write(")");
          return;
        }
      }
      this.sourceEmitterOutput.Write("(");
      this.Traverse(conditional.Condition);
      this.sourceEmitterOutput.Write(" ? ");
      this.Traverse(conditional.ResultIfTrue);
      this.sourceEmitterOutput.Write(" : ");
      this.Traverse(conditional.ResultIfFalse);
      this.sourceEmitterOutput.Write(")");
    }

    public override void TraverseChildren(IConversion conversion) {
      if (conversion.ValueToConvert is IVectorLength) {
        this.Traverse(((IVectorLength)conversion.ValueToConvert).Vector);
        if (conversion.TypeAfterConversion.TypeCode == PrimitiveTypeCode.Int64 || conversion.TypeAfterConversion.TypeCode == PrimitiveTypeCode.UInt64)
          this.sourceEmitterOutput.Write(".LongLength");
        else
          this.sourceEmitterOutput.Write(".Length");
        return;
      }
      if (conversion.CheckNumericRange)
        this.sourceEmitterOutput.Write("checked");
      this.sourceEmitterOutput.Write("((");
      this.PrintTypeReferenceName(conversion.TypeAfterConversion);
      this.sourceEmitterOutput.Write(")");
      this.Traverse(conversion.ValueToConvert);
      this.sourceEmitterOutput.Write(")");
    }

    public override void TraverseChildren(ICreateArray createArray) {
      this.sourceEmitterOutput.Write("new ");
      this.PrintTypeReference(createArray.ElementType);
      this.sourceEmitterOutput.Write("[");
      this.Traverse(createArray.Sizes);
      this.sourceEmitterOutput.Write("]");
      if (IteratorHelper.EnumerableIsNotEmpty(createArray.Initializers)) {
        this.sourceEmitterOutput.Write(" {");
        this.Traverse(createArray.Initializers);
        this.sourceEmitterOutput.Write("}");
      }
    }

    private void TraverseChildren(IEnumerable<ulong> sizes) {
      bool emitComma = false;
      foreach (ulong size in sizes) {
        if (emitComma) this.sourceEmitterOutput.Write(", ");
        this.sourceEmitterOutput.Write(size.ToString());
        emitComma = true;
      }
    }

    public override void TraverseChildren(ICreateDelegateInstance/*!*/ createDelegateInstance) {
      if (createDelegateInstance.Instance != null) {
        ICompileTimeConstant constant = createDelegateInstance.Instance as ICompileTimeConstant;
        if (constant == null || constant.Value != null) {
          this.Traverse(createDelegateInstance.Instance);
          this.PrintToken(CSharpToken.Dot);
        }
      }
      this.PrintMethodDefinitionName(createDelegateInstance.MethodToCallViaDelegate.ResolvedMethod);
      //base.TraverseChildren(createDelegateInstance);
    }

    public override void TraverseChildren(ICreateObjectInstance createObjectInstance) {
      this.PrintToken(CSharpToken.New);
      this.PrintTypeReferenceName(createObjectInstance.MethodToCall.ContainingType);
      this.PrintArgumentList(createObjectInstance.Arguments);
    }

    public override void TraverseChildren(ICustomAttribute customAttribute) {
      // Different uses of custom attributes must print them directly based on context
      //base.TraverseChildren(customAttribute);
    }

    public override void TraverseChildren(ICustomModifier customModifier) {
      base.TraverseChildren(customModifier);
    }

    public override void TraverseChildren(IDefaultValue defaultValue) {
      this.sourceEmitterOutput.Write("default(");
      this.PrintTypeReference(defaultValue.DefaultValueType);
      this.sourceEmitterOutput.Write(")");
    }

    public override void TraverseChildren(IDivision division) {
      this.sourceEmitterOutput.Write("(");
      this.Traverse(division.LeftOperand);
      this.sourceEmitterOutput.Write(" / ");
      this.Traverse(division.RightOperand);
      this.sourceEmitterOutput.Write(")");
    }

    public override void TraverseChildren(IDupValue dupValue) {
      this.sourceEmitterOutput.Write("dup");
    }

    public override void TraverseChildren(IEquality equality) {
      this.sourceEmitterOutput.Write("(");
      this.Traverse(equality.LeftOperand);
      this.sourceEmitterOutput.Write(" == ");
      this.Traverse(equality.RightOperand);
      this.sourceEmitterOutput.Write(")");
    }

    public override void TraverseChildren(IExclusiveOr exclusiveOr) {
      this.sourceEmitterOutput.Write("(");
      this.Traverse(exclusiveOr.LeftOperand);
      this.sourceEmitterOutput.Write(" ^ ");
      this.Traverse(exclusiveOr.RightOperand);
      this.sourceEmitterOutput.Write(")");
    }

    public new void Traverse(IEnumerable<IExpression> arguments) {
      bool needComma = false;
      foreach (IExpression argument in arguments) {
        if (needComma) {
          this.PrintToken(CSharpToken.Comma);
          this.PrintToken(CSharpToken.Space);
        }
        this.Traverse(argument);
        needComma = true;
      }
    }

    public override void TraverseChildren(IExpression expression) {
      base.TraverseChildren(expression);
    }

    public override void TraverseChildren(IGetTypeOfTypedReference getTypeOfTypedReference) {
      this.sourceEmitterOutput.Write("__reftype(");
      this.Traverse(getTypeOfTypedReference.TypedReference);
      this.sourceEmitterOutput.Write(")");
    }

    public override void TraverseChildren(IGetValueOfTypedReference getValueOfTypedReference) {
      this.sourceEmitterOutput.Write("__refvalue(");
      this.Traverse(getValueOfTypedReference.TypedReference);
      this.sourceEmitterOutput.Write(", ");
      this.PrintTypeReferenceName(getValueOfTypedReference.TargetType);
      this.sourceEmitterOutput.Write(")");
    }

    public override void TraverseChildren(IGreaterThan greaterThan) {
      if (greaterThan.IsUnsignedOrUnordered && !TypeHelper.IsPrimitiveInteger(greaterThan.LeftOperand.Type)) {
        this.sourceEmitterOutput.Write("!(");
        this.Traverse(greaterThan.LeftOperand);
        this.sourceEmitterOutput.Write(" <= ");
        this.Traverse(greaterThan.RightOperand);
        this.sourceEmitterOutput.Write(")");
        return;
      }
      this.sourceEmitterOutput.Write("(");
      if (greaterThan.IsUnsignedOrUnordered && TypeHelper.IsPrimitiveInteger(greaterThan.LeftOperand.Type) && 
        greaterThan.LeftOperand.Type != TypeHelper.UnsignedEquivalent(greaterThan.LeftOperand.Type)) {
        this.sourceEmitterOutput.Write("(");
        this.PrintTypeReferenceName(TypeHelper.UnsignedEquivalent(greaterThan.LeftOperand.Type));
        this.sourceEmitterOutput.Write(")");
      }
      this.Traverse(greaterThan.LeftOperand);
      this.sourceEmitterOutput.Write(" > ");
      if (greaterThan.IsUnsignedOrUnordered && TypeHelper.IsPrimitiveInteger(greaterThan.RightOperand.Type) && 
        greaterThan.RightOperand.Type != TypeHelper.UnsignedEquivalent(greaterThan.RightOperand.Type)) {
        this.sourceEmitterOutput.Write("(");
        this.PrintTypeReferenceName(TypeHelper.UnsignedEquivalent(greaterThan.RightOperand.Type));
        this.sourceEmitterOutput.Write(")");
      }
      this.Traverse(greaterThan.RightOperand);
      this.sourceEmitterOutput.Write(")");
    }

    public override void TraverseChildren(IGreaterThanOrEqual greaterThanOrEqual) {
      if (greaterThanOrEqual.IsUnsignedOrUnordered && !TypeHelper.IsPrimitiveInteger(greaterThanOrEqual.LeftOperand.Type)) {
        this.sourceEmitterOutput.Write("!(");
        this.Traverse(greaterThanOrEqual.LeftOperand);
        this.sourceEmitterOutput.Write(" < ");
        this.Traverse(greaterThanOrEqual.RightOperand);
        this.sourceEmitterOutput.Write(")");
        return;
      }
      this.sourceEmitterOutput.Write("(");
      if (greaterThanOrEqual.IsUnsignedOrUnordered && TypeHelper.IsPrimitiveInteger(greaterThanOrEqual.LeftOperand.Type) && 
        greaterThanOrEqual.LeftOperand.Type != TypeHelper.UnsignedEquivalent(greaterThanOrEqual.LeftOperand.Type)) {
        this.sourceEmitterOutput.Write("(");
        this.PrintTypeReferenceName(TypeHelper.UnsignedEquivalent(greaterThanOrEqual.LeftOperand.Type));
        this.sourceEmitterOutput.Write(")");
      }
      this.Traverse(greaterThanOrEqual.LeftOperand);
      this.sourceEmitterOutput.Write(" >= ");
      if (greaterThanOrEqual.IsUnsignedOrUnordered && TypeHelper.IsPrimitiveInteger(greaterThanOrEqual.RightOperand.Type) && 
        greaterThanOrEqual.RightOperand.Type != TypeHelper.UnsignedEquivalent(greaterThanOrEqual.RightOperand.Type)) {
        this.sourceEmitterOutput.Write("(");
        this.PrintTypeReferenceName(TypeHelper.UnsignedEquivalent(greaterThanOrEqual.RightOperand.Type));
        this.sourceEmitterOutput.Write(")");
      }
      this.Traverse(greaterThanOrEqual.RightOperand);
      this.sourceEmitterOutput.Write(")");
    }

    public override void TraverseChildren(ILeftShift leftShift) {
      this.sourceEmitterOutput.Write("(");
      this.Traverse(leftShift.LeftOperand);
      this.sourceEmitterOutput.Write(" << ");
      this.Traverse(leftShift.RightOperand);
      this.sourceEmitterOutput.Write(")");
    }

    public override void TraverseChildren(ILessThan lessThan) {
      if (lessThan.IsUnsignedOrUnordered && !TypeHelper.IsPrimitiveInteger(lessThan.LeftOperand.Type)) {
        this.sourceEmitterOutput.Write("!(");
        this.Traverse(lessThan.LeftOperand);
        this.sourceEmitterOutput.Write(" >= ");
        this.Traverse(lessThan.RightOperand);
        this.sourceEmitterOutput.Write(")");
        return;
      }
      this.sourceEmitterOutput.Write("(");
      if (lessThan.IsUnsignedOrUnordered && TypeHelper.IsPrimitiveInteger(lessThan.LeftOperand.Type) && 
        lessThan.LeftOperand.Type != TypeHelper.UnsignedEquivalent(lessThan.LeftOperand.Type)) {
        this.sourceEmitterOutput.Write("(");
        this.PrintTypeReferenceName(TypeHelper.UnsignedEquivalent(lessThan.LeftOperand.Type));
        this.sourceEmitterOutput.Write(")");
      }
      this.Traverse(lessThan.LeftOperand);
      this.sourceEmitterOutput.Write(" < ");
      if (lessThan.IsUnsignedOrUnordered && TypeHelper.IsPrimitiveInteger(lessThan.RightOperand.Type) && 
        lessThan.RightOperand.Type != TypeHelper.UnsignedEquivalent(lessThan.RightOperand.Type)) {
        this.sourceEmitterOutput.Write("(");
        this.PrintTypeReferenceName(TypeHelper.UnsignedEquivalent(lessThan.RightOperand.Type));
        this.sourceEmitterOutput.Write(")");
      }
      this.Traverse(lessThan.RightOperand);
      this.sourceEmitterOutput.Write(")");
    }

    public override void TraverseChildren(ILessThanOrEqual lessThanOrEqual) {
      if (lessThanOrEqual.IsUnsignedOrUnordered && !TypeHelper.IsPrimitiveInteger(lessThanOrEqual.LeftOperand.Type)) {
        this.sourceEmitterOutput.Write("!(");
        this.Traverse(lessThanOrEqual.LeftOperand);
        this.sourceEmitterOutput.Write(" > ");
        this.Traverse(lessThanOrEqual.RightOperand);
        this.sourceEmitterOutput.Write(")");
        return;
      }
      this.sourceEmitterOutput.Write("(");
      if (lessThanOrEqual.IsUnsignedOrUnordered && TypeHelper.IsPrimitiveInteger(lessThanOrEqual.LeftOperand.Type) && 
        lessThanOrEqual.LeftOperand.Type != TypeHelper.UnsignedEquivalent(lessThanOrEqual.LeftOperand.Type)) {
        this.sourceEmitterOutput.Write("(");
        this.PrintTypeReferenceName(TypeHelper.UnsignedEquivalent(lessThanOrEqual.LeftOperand.Type));
        this.sourceEmitterOutput.Write(")");
      }
      this.Traverse(lessThanOrEqual.LeftOperand);
      this.sourceEmitterOutput.Write(" <= ");
      if (lessThanOrEqual.IsUnsignedOrUnordered && TypeHelper.IsPrimitiveInteger(lessThanOrEqual.RightOperand.Type) && 
        lessThanOrEqual.RightOperand.Type != TypeHelper.UnsignedEquivalent(lessThanOrEqual.RightOperand.Type)) {
        this.sourceEmitterOutput.Write("(");
        this.PrintTypeReferenceName(TypeHelper.UnsignedEquivalent(lessThanOrEqual.RightOperand.Type));
        this.sourceEmitterOutput.Write(")");
      }
      this.Traverse(lessThanOrEqual.RightOperand);
      this.sourceEmitterOutput.Write(")");
    }

    public override void TraverseChildren(ILogicalNot logicalNot) {
      this.sourceEmitterOutput.Write("!");
      this.Traverse(logicalNot.Operand);
    }

    public override void TraverseChildren(IMakeTypedReference makeTypedReference) {
      this.sourceEmitterOutput.Write("__makeref(");
      this.Traverse(makeTypedReference.Operand);
      this.sourceEmitterOutput.Write(")");
    }

    public override void TraverseChildren(IManagedPointerTypeReference managedPointerTypeReference) {
      base.TraverseChildren(managedPointerTypeReference);
    }

    public override void TraverseChildren(IMarshallingInformation marshallingInformation) {
      base.TraverseChildren(marshallingInformation);
    }

    public override void TraverseChildren(IMetadataConstant constant) {
      var val = constant.Value;
      if (val == null)
        this.PrintToken(CSharpToken.Null);
      else if (constant.Type.ResolvedType.IsEnum)
        PrintEnumValue(constant.Type.ResolvedType, val);
      else if (val is string)
        PrintString((string)val);
      else if (val is bool)
        PrintToken((bool)val ? CSharpToken.True : CSharpToken.False);
      else if (val is char)
        this.sourceEmitterOutput.Write(String.Format("'{0}'", EscapeChar((char)val, false)));
      else if (val is float)
        PrintFloat((float)val);
      else if (val is double)
        PrintDouble((double)val);
      else if (val is int)
        PrintInt((int)val);
      else if (val is uint)
        PrintUint((uint)val);
      else if (val is long)
        PrintLong((long)val);
      else if (val is ulong)
        PrintUlong((ulong)val);
      else
        this.sourceEmitterOutput.Write(constant.Value.ToString());
    }

    public virtual void PrintFloat(float value) {
      // Use symbolic names for common constants
      if (float.IsNaN(value))
        sourceEmitterOutput.Write("float.NaN");
      else if (float.IsPositiveInfinity(value))
        sourceEmitterOutput.Write("float.PositiveInfinity");
      else if (float.IsNegativeInfinity(value))
        sourceEmitterOutput.Write("float.NegativeInfinity");
      else if (value == float.Epsilon)
        sourceEmitterOutput.Write("float.Epsilon");
      else if (value == float.MaxValue)
        sourceEmitterOutput.Write("float.MaxValue");
      else if (value == float.MinValue)
        sourceEmitterOutput.Write("float.MinValue");
      else
        sourceEmitterOutput.Write(value.ToString("R") + "f"); // round-trip format 
    }

    public virtual void PrintDouble(double value) {
      // Use symbolic names for common constants
      if (double.IsNaN(value))
        sourceEmitterOutput.Write("double.NaN");
      else if (double.IsPositiveInfinity(value))
        sourceEmitterOutput.Write("double.PositiveInfinity");
      else if (double.IsNegativeInfinity(value))
        sourceEmitterOutput.Write("double.NegativeInfinity");
      else if (value == double.Epsilon)
        sourceEmitterOutput.Write("double.Epsilon");
      else if (value == double.MaxValue)
        sourceEmitterOutput.Write("double.MaxValue");
      else if (value == double.MinValue)
        sourceEmitterOutput.Write("double.MinValue");
      else
        sourceEmitterOutput.Write(value.ToString("R")); // round-trip format 
    }

    public virtual void PrintLong(long value) {
      if (value == long.MaxValue)
        sourceEmitterOutput.Write("long.MaxValue");
      else if (value == long.MinValue)
        sourceEmitterOutput.Write("long.MinValue");
      else
        sourceEmitterOutput.Write(value.ToString());
    }

    public virtual void PrintUlong(ulong value) {
      if (value == ulong.MaxValue)
        sourceEmitterOutput.Write("ulong.MaxValue");
      else
        sourceEmitterOutput.Write(value.ToString());
    }

    public virtual void PrintInt(int value) {
      if (value == int.MaxValue)
        sourceEmitterOutput.Write("int.MaxValue");
      else if (value == int.MinValue)
        sourceEmitterOutput.Write("int.MinValue");
      else
        sourceEmitterOutput.Write(value.ToString());
    }

    public virtual void PrintUint(uint value) {
      if (value == uint.MaxValue)
        sourceEmitterOutput.Write("uint.MaxValue");
      else
        sourceEmitterOutput.Write(value.ToString());
    }

    public virtual void PrintEnumValue(ITypeDefinition enumType, object valObj) {
      bool flags = (Utils.FindAttribute(enumType.Attributes, SpecialAttribute.Flags) != null);

      // Loop through all the enum constants looking for a match
      ulong value = UnboxToULong(valObj);
      bool success = false;
      List<IFieldDefinition> constants = new List<IFieldDefinition>();
      foreach (var f in enumType.Fields) {
        if (f.IsCompileTimeConstant && TypeHelper.TypesAreEquivalent(f.Type, enumType))
          constants.Add(f);
      }
      // Sort by order in type - hopefully this gives the minimum set of flags
      constants.Sort((f1, f2) => {
        if (f1 is IMetadataObjectWithToken && f2 is IMetadataObjectWithToken)
          return ((IMetadataObjectWithToken)f1).TokenValue.CompareTo(((IMetadataObjectWithToken)f2).TokenValue);
        return UnboxToULong(f1.CompileTimeValue.Value).CompareTo(UnboxToULong(f2.CompileTimeValue.Value));
      });

      // If the value has the high bit set, and no constant does, then it's probably best represented as a negation
      bool negate = false;
      int nBits = Marshal.SizeOf(valObj)*8;
      ulong highBit = 1ul << (nBits - 1);
      if (flags && (value & highBit) == highBit && constants.Count > 0 && (UnboxToULong(constants[0].CompileTimeValue.Value) & highBit) == 0) {
        value = (~value) & ((1UL << nBits) - 1);
        negate = true;
        sourceEmitterOutput.Write("~(");
      }
      ulong valLeft = value;
      foreach (var c in constants) {
        ulong fv = UnboxToULong(c.CompileTimeValue.Value);
        if (valLeft == fv || (flags && (fv != 0) && ((valLeft & fv) == fv))) {
          if (valLeft != value)
            sourceEmitterOutput.Write(" | ");
          TraverseChildren((IFieldReference)c);
          valLeft -= fv;
          if (valLeft == 0) {
            success = true;
            break;
          }
        }
      }
      // No match, output cast
      if (!success) {
        if (valLeft != value)
          sourceEmitterOutput.Write(" | ");
        sourceEmitterOutput.Write("unchecked((");
        TraverseChildren((ITypeReference)enumType);
        sourceEmitterOutput.Write(")0x" + valLeft.ToString("X") + ")");
      }
      if (negate)
        sourceEmitterOutput.Write(")");
    }

    private static ulong UnboxToULong(object obj) {
      // Can't just cast - must unbox to specific type.
      // Can't use Convert.ToUInt64 - it'll throw for negative numbers
      switch (Convert.GetTypeCode(obj)) {
        case TypeCode.Byte:
          return (ulong)(Byte)obj;
        case TypeCode.SByte:
          return (ulong)(Byte)(SByte)obj;
        case TypeCode.UInt16:
          return (ulong)(UInt16)obj;
        case TypeCode.Int16:
          return (ulong)(UInt16)(Int16)obj;
        case TypeCode.UInt32:
          return (ulong)(UInt32)obj;
        case TypeCode.Int32:
          return (ulong)(UInt32)(Int32)obj;
        case TypeCode.UInt64:
          return (ulong)obj;
        case TypeCode.Int64:
          return (ulong)(Int64)obj;
      }
      // Argument must be of integral type (not in message becaseu we don't want english strings in CCI)
      throw new ArgumentException();
    }

    public override void TraverseChildren(IMetadataCreateArray createArray) {
      base.TraverseChildren(createArray);
    }

    public override void TraverseChildren(IMetadataNamedArgument namedArgument) {
      this.sourceEmitterOutput.Write(namedArgument.ArgumentName.Value+" = ");
      this.Traverse(namedArgument.ArgumentValue);
    }

    public override void TraverseChildren(IMetadataTypeOf typeOf) {
      PrintToken(CSharpToken.TypeOf);
      PrintToken(CSharpToken.LeftParenthesis);
      this.PrintTypeReference(typeOf.TypeToGet);
      PrintToken(CSharpToken.RightParenthesis);
    }

    public override void TraverseChildren(IMethodCall methodCall) {
      NameFormattingOptions options = NameFormattingOptions.None;
      if (!methodCall.IsStaticCall) {
        IAddressOf/*?*/ addressOf = methodCall.ThisArgument as IAddressOf;
        if (addressOf != null) {
          this.Traverse(addressOf.Expression);
        } else {
          this.Traverse(methodCall.ThisArgument);
        }
        this.PrintToken(CSharpToken.Dot);
        options |= NameFormattingOptions.OmitContainingNamespace|NameFormattingOptions.OmitContainingType;
      }
      this.PrintMethodReferenceName(methodCall.MethodToCall, options);
      this.PrintArgumentList(methodCall.Arguments);
    }

    private void PrintArgumentList(IEnumerable<IExpression> arguments) {
      this.sourceEmitterOutput.Write("(");
      this.Traverse(arguments);
      this.sourceEmitterOutput.Write(")");
    }

    public override void TraverseChildren(IMethodImplementation methodImplementation) {
      base.TraverseChildren(methodImplementation);
    }

    public override void TraverseChildren(IMethodReference methodReference) {
      base.TraverseChildren(methodReference);
    }

    public override void TraverseChildren(IModifiedTypeReference modifiedTypeReference) {
      base.TraverseChildren(modifiedTypeReference);
    }

    public override void TraverseChildren(IModule module) {
      if (!(module is IAssembly)) {
        foreach (var attr in module.Attributes) {
          PrintAttribute(module, attr, true, "module");
        }
      }

      base.TraverseChildren(module);
    }

    public override void TraverseChildren(IModuleReference moduleReference) {
      base.TraverseChildren(moduleReference);
    }

    public override void TraverseChildren(IModulus modulus) {
      this.sourceEmitterOutput.Write("(");
      this.Traverse(modulus.LeftOperand);
      this.sourceEmitterOutput.Write(" % ");
      this.Traverse(modulus.RightOperand);
      this.sourceEmitterOutput.Write(")");
    }

    public override void TraverseChildren(IMultiplication multiplication) {
      this.sourceEmitterOutput.Write("(");
      this.Traverse(multiplication.LeftOperand);
      this.sourceEmitterOutput.Write(" * ");
      this.Traverse(multiplication.RightOperand);
      this.sourceEmitterOutput.Write(")");
    }

    public override void TraverseChildren(INamedArgument namedArgument) {
      base.TraverseChildren(namedArgument);
    }

    public override void TraverseChildren(INamespaceAliasForType namespaceAliasForType) {
      base.TraverseChildren(namespaceAliasForType);
    }

    public override void TraverseChildren(INamespaceTypeReference namespaceTypeReference) {
      base.TraverseChildren(namespaceTypeReference);
    }

    public override void TraverseChildren(INestedTypeReference nestedTypeReference) {
      base.TraverseChildren(nestedTypeReference);
    }

    public override void TraverseChildren(INotEquality notEquality) {
      this.sourceEmitterOutput.Write("(");
      this.Traverse(notEquality.LeftOperand);
      this.sourceEmitterOutput.Write(" != ");
      this.Traverse(notEquality.RightOperand);
      this.sourceEmitterOutput.Write(")");
    }

    public override void TraverseChildren(IOldValue oldValue) {
      base.TraverseChildren(oldValue);
    }

    public override void TraverseChildren(IOnesComplement onesComplement) {
      base.TraverseChildren(onesComplement);
    }

    public override void TraverseChildren(IOperation operation) {
      base.TraverseChildren(operation);
    }

    public override void TraverseChildren(IOperationExceptionInformation operationExceptionInformation) {
      base.TraverseChildren(operationExceptionInformation);
    }

    public override void TraverseChildren(IOutArgument outArgument) {
      base.TraverseChildren(outArgument);
    }

    public override void TraverseChildren(IParameterTypeInformation parameterTypeInformation) {
      base.TraverseChildren(parameterTypeInformation);
    }

    public override void TraverseChildren(IPlatformInvokeInformation platformInvokeInformation) {
      base.TraverseChildren(platformInvokeInformation);
    }

    public override void TraverseChildren(IPointerCall pointerCall) {
      base.TraverseChildren(pointerCall);
    }

    public override void TraverseChildren(IPointerTypeReference pointerTypeReference) {
      base.TraverseChildren(pointerTypeReference);
    }

    public override void TraverseChildren(IPopValue popValue) {
      this.sourceEmitterOutput.Write("pop");
    }

    public override void TraverseChildren(IRefArgument refArgument) {
      base.TraverseChildren(refArgument);
    }

    public override void TraverseChildren(IResourceReference resourceReference) {
      base.TraverseChildren(resourceReference);
    }

    public override void TraverseChildren(IReturnValue returnValue) {
      this.sourceEmitterOutput.Write("result");
    }

    public override void TraverseChildren(IRightShift rightShift) {
      this.sourceEmitterOutput.Write("(");
      this.Traverse(rightShift.LeftOperand);
      this.sourceEmitterOutput.Write(" >> ");
      this.Traverse(rightShift.RightOperand);
      this.sourceEmitterOutput.Write(")");
    }

    public override void TraverseChildren(IRuntimeArgumentHandleExpression runtimeArgumentHandleExpression) {
      base.TraverseChildren(runtimeArgumentHandleExpression);
    }

    public override void TraverseChildren(ISecurityAttribute securityAttribute) {
      base.TraverseChildren(securityAttribute);
    }

    public override void TraverseChildren(ISizeOf sizeOf) {
      this.sourceEmitterOutput.Write("sizeOf(");
      this.PrintTypeReference(sizeOf.TypeToSize);
      this.sourceEmitterOutput.Write(")");
    }

    public override void TraverseChildren(ISourceMethodBody methodBody) {
      base.TraverseChildren(methodBody);
    }

    public override void TraverseChildren(IStackArrayCreate stackArrayCreate) {
      base.TraverseChildren(stackArrayCreate);
    }

    public override void TraverseChildren(ISubtraction subtraction) {
      this.sourceEmitterOutput.Write("(");
      this.Traverse(subtraction.LeftOperand);
      this.sourceEmitterOutput.Write(" - ");
      this.Traverse(subtraction.RightOperand);
      this.sourceEmitterOutput.Write(")");
    }

    public override void TraverseChildren(ITargetExpression targetExpression) {
      IArrayIndexer/*?*/ indexer = targetExpression.Definition as IArrayIndexer;
      if (indexer != null) {
        this.Traverse(indexer);
        return;
      }
      IAddressDereference/*?*/ deref = targetExpression.Definition as IAddressDereference;
      if (deref != null) {
        IAddressOf/*?*/ addressOf = deref.Address as IAddressOf;
        if (addressOf != null) {
          this.Traverse(addressOf.Expression);
          return;
        }
        if (targetExpression.Instance != null) {
          this.Traverse(targetExpression.Instance);
          this.sourceEmitterOutput.Write("->");
        } else if (deref.Address.Type is IPointerTypeReference || deref.Address.Type.TypeCode == PrimitiveTypeCode.IntPtr)
          this.sourceEmitterOutput.Write("*");
        this.Traverse(deref.Address);
        return;
      } else {
        if (targetExpression.Instance != null) {
          IAddressOf/*?*/ addressOf = targetExpression.Instance as IAddressOf;
          if (addressOf != null)
            this.Traverse(addressOf.Expression);
          else
            this.Traverse(targetExpression.Instance);
          this.sourceEmitterOutput.Write(".");
        }
      }
      ILocalDefinition/*?*/ local = targetExpression.Definition as ILocalDefinition;
      if (local != null)
        this.PrintLocalName(local);
      else {
        INamedEntity/*?*/ ne = targetExpression.Definition as INamedEntity;
        if (ne != null)
          this.sourceEmitterOutput.Write(ne.Name.Value);
      }
    }

    public virtual void PrintLocalName(ILocalDefinition local) {
      this.sourceEmitterOutput.Write(local.Name.Value);
    }

    public override void TraverseChildren(IThisReference thisReference) {
      this.PrintToken(CSharpToken.This);
    }

    public override void TraverseChildren(ITypeDefinitionMember typeMember) {
      base.TraverseChildren(typeMember);
    }

    public override void TraverseChildren(ITokenOf tokenOf) {
      this.sourceEmitterOutput.Write("tokenof(");
      base.TraverseChildren(tokenOf);
      this.sourceEmitterOutput.Write(")");
    }

    public override void TraverseChildren(ITypeOf typeOf) {
      this.sourceEmitterOutput.Write("typeof(");
      this.PrintTypeReference(typeOf.TypeToGet);
      this.sourceEmitterOutput.Write(")");
    }

    public override void TraverseChildren(ITypeReference typeReference) {
      this.PrintTypeReference(typeReference);
    }

    public override void TraverseChildren(IUnaryNegation unaryNegation) {
      base.TraverseChildren(unaryNegation);
    }

    public override void TraverseChildren(IUnaryPlus unaryPlus) {
      base.TraverseChildren(unaryPlus);
    }

    public override void TraverseChildren(IUnitNamespaceReference unitNamespaceReference) {
      base.TraverseChildren(unitNamespaceReference);
    }

    public override void TraverseChildren(IVectorLength vectorLength) {
      this.Traverse(vectorLength.Vector);
      this.sourceEmitterOutput.Write(".Length");
    }

  }
}