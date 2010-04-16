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
  public partial class SourceEmitter : BaseCodeTraverser, ICSharpSourceEmitter {

    public override void Visit(IAddition addition) {
      this.sourceEmitterOutput.Write("(");
      this.Visit(addition.LeftOperand);
      this.sourceEmitterOutput.Write(" + ");
      this.Visit(addition.RightOperand);
      this.sourceEmitterOutput.Write(")");
    }

    public override void Visit(IAddressableExpression addressableExpression) {
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
      IFieldReference/*?*/ field = addressableExpression.Definition as IFieldReference;
      if (field != null) {
        this.sourceEmitterOutput.Write(field.Name.Value);
        return;
      }
      IArrayIndexer/*?*/ arrayIndexer = addressableExpression.Definition as IArrayIndexer;
      if (arrayIndexer != null) {
        this.Visit(arrayIndexer);
        return;
      }
      IAddressDereference/*?*/ addressDereference = addressableExpression.Definition as IAddressDereference;
      if (addressDereference != null) {
        this.Visit(addressDereference);
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

    public override void Visit(IAddressDereference addressDereference) {
      if (addressDereference.Address.Type is IPointerTypeReference)
        this.sourceEmitterOutput.Write("*");
      else {
        var addrOf = addressDereference.Address as IAddressOf;
        if (addrOf != null) {
          this.Visit(addrOf.Expression);
          return;
        }
      }
      this.Visit(addressDereference.Address);
    }

    public override void Visit(IAddressOf addressOf) {
      this.sourceEmitterOutput.Write("&");
      this.Visit(addressOf.Expression);
    }

    public override void Visit(IAliasForType aliasForType) {
      // Already outputted these at the top for IAssembly
    }

    public override void Visit(IAnonymousDelegate anonymousDelegate) {
      this.sourceEmitterOutput.Write("delegate ");
      this.Visit(anonymousDelegate.Parameters);
      this.sourceEmitterOutput.WriteLine(" {");
      this.sourceEmitterOutput.IncreaseIndent();
      this.Visit(anonymousDelegate.Body.Statements);
      this.sourceEmitterOutput.DecreaseIndent();
      this.sourceEmitterOutput.Write("}", true);
    }

    public override void Visit(IArrayIndexer arrayIndexer) {
      this.Visit(arrayIndexer.IndexedObject);
      this.sourceEmitterOutput.Write("[");
      this.Visit(arrayIndexer.Indices);
      this.sourceEmitterOutput.Write("]");
    }

    public override void Visit(IArrayTypeReference arrayTypeReference) {
      base.Visit(arrayTypeReference);
    }

    public override void Visit(IAssembly assembly) {
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
          Visit(alias.AliasedType);
          sourceEmitterOutput.WriteLine("))]");
        }
      }
      PrintToken(CSharpToken.NewLine);
      base.Visit(assembly);
    }

    public override void Visit(IAssemblyReference assemblyReference) {
      base.Visit(assemblyReference);
    }

    public override void Visit(IAssignment assignment) {
      this.Visit(assignment.Target);
      this.PrintToken(CSharpToken.Space);
      this.PrintToken(CSharpToken.Assign);
      this.PrintToken(CSharpToken.Space);
      this.Visit(assignment.Source);
    }

    public override void Visit(IBaseClassReference baseClassReference) {
      base.Visit(baseClassReference);
    }

    public override void Visit(IBitwiseAnd bitwiseAnd) {
      this.sourceEmitterOutput.Write("(");
      this.Visit(bitwiseAnd.LeftOperand);
      this.sourceEmitterOutput.Write(" & ");
      this.Visit(bitwiseAnd.RightOperand);
      this.sourceEmitterOutput.Write(")");
    }

    public override void Visit(IBitwiseOr bitwiseOr) {
      this.sourceEmitterOutput.Write("(");
      this.Visit(bitwiseOr.LeftOperand);
      this.sourceEmitterOutput.Write(" | ");
      this.Visit(bitwiseOr.RightOperand);
      this.sourceEmitterOutput.Write(")");
    }

    public override void Visit(IBlockExpression blockExpression) {
      this.sourceEmitterOutput.WriteLine("(() => {");
      this.sourceEmitterOutput.IncreaseIndent();
      this.Visit(blockExpression.BlockStatement);
      this.sourceEmitterOutput.Write("return ", true);
      this.Visit(blockExpression.Expression);
      this.sourceEmitterOutput.Write("; })()");
      this.sourceEmitterOutput.DecreaseIndent();
    }

    public override void Visit(IBoundExpression boundExpression) {
      if (boundExpression.Instance != null) {
        IAddressOf/*?*/ addressOf = boundExpression.Instance as IAddressOf;
        if (addressOf != null && addressOf.Expression.Type.IsValueType) {
          this.Visit(addressOf.Expression);
        } else {
          this.Visit(boundExpression.Instance);
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

    public override void Visit(ICastIfPossible castIfPossible) {
      this.sourceEmitterOutput.Write("(");
      this.Visit(castIfPossible.ValueToCast);
      this.sourceEmitterOutput.Write(" as ");
      this.Visit(castIfPossible.TargetType);
      this.sourceEmitterOutput.Write(")");
    }

    public override void Visit(ICheckIfInstance checkIfInstance) {
      this.sourceEmitterOutput.Write("(");
      this.Visit(checkIfInstance.Operand);
      this.sourceEmitterOutput.Write(" is ");
      this.Visit(checkIfInstance.TypeToCheck);
      this.sourceEmitterOutput.Write(")");
    }

    public override void Visit(ICompileTimeConstant constant) {
      if (constant.Value == null)
        this.PrintToken(CSharpToken.Null);
      else if (constant.Value is bool)
        this.sourceEmitterOutput.Write(((bool)constant.Value) ? "true" : "false");
      else if (constant.Value is string)
        this.PrintString((string)constant.Value);
      else
        this.sourceEmitterOutput.Write(constant.Value.ToString());
    }

    public override void Visit(IConditional conditional) {
      this.sourceEmitterOutput.Write("(");
      this.Visit(conditional.Condition);
      if (ExpressionHelper.IsIntegralOne(conditional.ResultIfTrue)) {
        this.sourceEmitterOutput.Write(" || ");
        this.Visit(conditional.ResultIfFalse);
        this.sourceEmitterOutput.Write(")");
        return;
      }
      if (ExpressionHelper.IsIntegralZero(conditional.ResultIfFalse)) {
        this.sourceEmitterOutput.Write(" && ");
        this.Visit(conditional.ResultIfTrue);
        this.sourceEmitterOutput.Write(")");
        return;
      }
      this.sourceEmitterOutput.Write(" ? ");
      this.Visit(conditional.ResultIfTrue);
      this.sourceEmitterOutput.Write(" : ");
      this.Visit(conditional.ResultIfFalse);
      this.sourceEmitterOutput.Write(")");
    }

    public override void Visit(IConversion conversion) {
      if (conversion.CheckNumericRange)
        this.sourceEmitterOutput.Write("checked");
      this.sourceEmitterOutput.Write("((");
      this.PrintTypeReferenceName(conversion.TypeAfterConversion);
      this.sourceEmitterOutput.Write(")");
      this.Visit(conversion.ValueToConvert);
      this.sourceEmitterOutput.Write(")");
    }

    public override void Visit(ICreateArray createArray) {
      this.sourceEmitterOutput.Write("new ");
      this.PrintTypeReference(createArray.ElementType);
      this.sourceEmitterOutput.Write("[");
      this.Visit(createArray.Sizes);
      this.sourceEmitterOutput.Write("]");
      if (IteratorHelper.EnumerableIsNotEmpty(createArray.Initializers)) {
        this.sourceEmitterOutput.Write(" {");
        this.Visit(createArray.Initializers);
        this.sourceEmitterOutput.Write("}");
      }
    }

    private void Visit(IEnumerable<ulong> sizes) {
      bool emitComma = false;
      foreach (ulong size in sizes) {
        if (emitComma) this.sourceEmitterOutput.Write(", ");
        this.sourceEmitterOutput.Write(size.ToString());
        emitComma = true;
      }
    }

    public override void Visit(ICreateDelegateInstance/*!*/ createDelegateInstance) {
      if (createDelegateInstance.Instance != null) {
        ICompileTimeConstant constant = createDelegateInstance.Instance as ICompileTimeConstant;
        if (constant == null || constant.Value != null) {
          this.Visit(createDelegateInstance.Instance);
          this.PrintToken(CSharpToken.Dot);
        }
      }
      this.PrintMethodDefinitionName(createDelegateInstance.MethodToCallViaDelegate.ResolvedMethod);
      //base.Visit(createDelegateInstance);
    }

    public override void Visit(ICreateObjectInstance createObjectInstance) {
      this.PrintToken(CSharpToken.New);
      this.PrintTypeReferenceName(createObjectInstance.MethodToCall.ContainingType);
      this.PrintArgumentList(createObjectInstance.Arguments);
    }

    public override void Visit(ICustomAttribute customAttribute) {
      // Different uses of custom attributes must print them directly based on context
      //base.Visit(customAttribute);
    }

    public override void Visit(ICustomModifier customModifier) {
      base.Visit(customModifier);
    }

    public override void Visit(IDefaultValue defaultValue) {
      this.sourceEmitterOutput.Write("default(");
      this.PrintTypeReference(defaultValue.DefaultValueType);
      this.sourceEmitterOutput.Write(")");
    }

    public override void Visit(IDivision division) {
      this.sourceEmitterOutput.Write("(");
      this.Visit(division.LeftOperand);
      this.sourceEmitterOutput.Write(" / ");
      this.Visit(division.RightOperand);
      this.sourceEmitterOutput.Write(")");
    }

    public override void Visit(IDupValue dupValue) {
      this.sourceEmitterOutput.Write("dup");
    }

    public override void Visit(IEquality equality) {
      this.sourceEmitterOutput.Write("(");
      this.Visit(equality.LeftOperand);
      this.sourceEmitterOutput.Write(" == ");
      this.Visit(equality.RightOperand);
      this.sourceEmitterOutput.Write(")");
    }

    public override void Visit(IExclusiveOr exclusiveOr) {
      this.sourceEmitterOutput.Write("(");
      this.Visit(exclusiveOr.LeftOperand);
      this.sourceEmitterOutput.Write(" ^ ");
      this.Visit(exclusiveOr.RightOperand);
      this.sourceEmitterOutput.Write(")");
    }

    public override void Visit(IEnumerable<IExpression> arguments) {
      bool needComma = false;
      foreach (IExpression argument in arguments) {
        if (needComma) {
          this.PrintToken(CSharpToken.Comma);
          this.PrintToken(CSharpToken.Space);
        }
        this.Visit(argument);
        needComma = true;
      }
    }

    public override void Visit(IExpression expression) {
      base.Visit(expression);
    }

    public override void Visit(IGetTypeOfTypedReference getTypeOfTypedReference) {
      this.sourceEmitterOutput.Write("__reftype(");
      this.Visit(getTypeOfTypedReference.TypedReference);
      this.sourceEmitterOutput.Write(")");
    }

    public override void Visit(IGetValueOfTypedReference getValueOfTypedReference) {
      this.sourceEmitterOutput.Write("__refvalue(");
      this.Visit(getValueOfTypedReference.TypedReference);
      this.sourceEmitterOutput.Write(", ");
      this.PrintTypeReferenceName(getValueOfTypedReference.TargetType);
      this.sourceEmitterOutput.Write(")");
    }

    public override void Visit(IGlobalFieldDefinition globalFieldDefinition) {
      base.Visit(globalFieldDefinition);
    }


    public override void Visit(IGlobalMethodDefinition globalMethodDefinition) {
      base.Visit(globalMethodDefinition);
    }

    public override void Visit(IGreaterThan greaterThan) {
      this.sourceEmitterOutput.Write("(");
      this.Visit(greaterThan.LeftOperand);
      this.sourceEmitterOutput.Write(" > ");
      this.Visit(greaterThan.RightOperand);
      this.sourceEmitterOutput.Write(")");
    }

    public override void Visit(IGreaterThanOrEqual greaterThanOrEqual) {
      this.sourceEmitterOutput.Write("(");
      this.Visit(greaterThanOrEqual.LeftOperand);
      this.sourceEmitterOutput.Write(" >= ");
      this.Visit(greaterThanOrEqual.RightOperand);
      this.sourceEmitterOutput.Write(")");
    }

    public override void Visit(ILeftShift leftShift) {
      this.sourceEmitterOutput.Write("(");
      this.Visit(leftShift.LeftOperand);
      this.sourceEmitterOutput.Write(" << ");
      this.Visit(leftShift.RightOperand);
      this.sourceEmitterOutput.Write(")");
    }

    public override void Visit(ILessThan lessThan) {
      this.sourceEmitterOutput.Write("(");
      this.Visit(lessThan.LeftOperand);
      this.sourceEmitterOutput.Write(" < ");
      this.Visit(lessThan.RightOperand);
      this.sourceEmitterOutput.Write(")");
    }

    public override void Visit(ILessThanOrEqual lessThanOrEqual) {
      this.sourceEmitterOutput.Write("(");
      this.Visit(lessThanOrEqual.LeftOperand);
      this.sourceEmitterOutput.Write(" <= ");
      this.Visit(lessThanOrEqual.RightOperand);
      this.sourceEmitterOutput.Write(")");
    }

    public override void Visit(ILogicalNot logicalNot) {
      this.sourceEmitterOutput.Write("!");
      this.Visit(logicalNot.Operand);
    }

    public override void Visit(IMakeTypedReference makeTypedReference) {
      this.sourceEmitterOutput.Write("__makeref(");
      this.Visit(makeTypedReference.Operand);
      this.sourceEmitterOutput.Write(")");
    }

    public override void Visit(IManagedPointerTypeReference managedPointerTypeReference) {
      base.Visit(managedPointerTypeReference);
    }

    public override void Visit(IMarshallingInformation marshallingInformation) {
      base.Visit(marshallingInformation);
    }

    public override void Visit(IMetadataConstant constant) {
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
      if (flags && (value & highBit) == highBit && constants.Count > 0 && (UnboxToULong(constants[0].CompileTimeValue.Value) & highBit) == 0)
      {
        value = (~value) & ((1UL << nBits) - 1);
        negate = true;
        sourceEmitterOutput.Write("~(");
      }
      ulong valLeft = value;
      foreach (var c in constants)
      {
        ulong fv = UnboxToULong(c.CompileTimeValue.Value);
        if (valLeft == fv || (flags && (fv != 0) && ((valLeft & fv) == fv))) {
          if (valLeft != value)
            sourceEmitterOutput.Write(" | ");
          Visit((IFieldReference)c);
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
        Visit((ITypeReference)enumType);
        sourceEmitterOutput.Write(")0x" + valLeft.ToString("X") + ")");
      }
      if (negate)
        sourceEmitterOutput.Write(")");
    }

    private static ulong UnboxToULong(object obj)
    {
      // Can't just cast - must unbox to specific type.
      // Can't use Convert.ToUInt64 - it'll throw for negative numbers
      switch (Convert.GetTypeCode(obj))
      {
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

    public override void Visit(IMetadataCreateArray createArray) {
      base.Visit(createArray);
    }

    public override void Visit(IMetadataExpression expression) {
      expression.Dispatch(this);
    }

    public override void Visit(IMetadataNamedArgument namedArgument) {
      this.sourceEmitterOutput.Write(namedArgument.ArgumentName.Value+" = ");
      this.Visit(namedArgument.ArgumentValue);
    }

    public override void Visit(IMetadataTypeOf typeOf) {
      PrintToken(CSharpToken.TypeOf);
      PrintToken(CSharpToken.LeftParenthesis);
      base.Visit(typeOf);
      PrintToken(CSharpToken.RightParenthesis);
    }

    public override void Visit(IMethodCall methodCall) {
      NameFormattingOptions options = NameFormattingOptions.None;
      if (!methodCall.IsStaticCall) {
        IAddressOf/*?*/ addressOf = methodCall.ThisArgument as IAddressOf;
        if (addressOf != null) {
          this.Visit(addressOf.Expression);
        } else {
          this.Visit(methodCall.ThisArgument);
        }
        this.PrintToken(CSharpToken.Dot);
        options |= NameFormattingOptions.OmitContainingNamespace|NameFormattingOptions.OmitContainingType;
      }
      this.PrintMethodReferenceName(methodCall.MethodToCall, options);
      this.PrintArgumentList(methodCall.Arguments);
    }

    private void PrintArgumentList(IEnumerable<IExpression> arguments) {
      this.sourceEmitterOutput.Write("(");
      this.Visit(arguments);
      this.sourceEmitterOutput.Write(")");
    }

    public override void Visit(IMethodImplementation methodImplementation) {
      base.Visit(methodImplementation);
    }

    public override void Visit(IMethodReference methodReference) {
      base.Visit(methodReference);
    }

    public override void Visit(IModifiedTypeReference modifiedTypeReference) {
      base.Visit(modifiedTypeReference);
    }

    public override void Visit(IModule module) {
      if (!(module is IAssembly)) {
        foreach (var attr in module.Attributes) {
          PrintAttribute(module, attr, true, "module");
        }
      }

      base.Visit(module);
    }

    public override void Visit(IModuleReference moduleReference) {
      base.Visit(moduleReference);
    }

    public override void Visit(IModulus modulus) {
      this.sourceEmitterOutput.Write("(");
      this.Visit(modulus.LeftOperand);
      this.sourceEmitterOutput.Write(" % ");
      this.Visit(modulus.RightOperand);
      this.sourceEmitterOutput.Write(")");
    }

    public override void Visit(IMultiplication multiplication) {
      this.sourceEmitterOutput.Write("(");
      this.Visit(multiplication.LeftOperand);
      this.sourceEmitterOutput.Write(" * ");
      this.Visit(multiplication.RightOperand);
      this.sourceEmitterOutput.Write(")");
    }

    public override void Visit(INamedArgument namedArgument) {
      base.Visit(namedArgument);
    }

    public override void Visit(INamespaceAliasForType namespaceAliasForType) {
      base.Visit(namespaceAliasForType);
    }

    public override void Visit(INamespaceTypeReference namespaceTypeReference) {
      base.Visit(namespaceTypeReference);
    }

    public override void Visit(INestedAliasForType nestedAliasForType) {
      base.Visit(nestedAliasForType);
    }

    public override void Visit(INestedTypeReference nestedTypeReference) {
      base.Visit(nestedTypeReference);
    }

    public override void Visit(INestedUnitNamespaceReference nestedUnitNamespaceReference) {
      base.Visit(nestedUnitNamespaceReference);
    }

    public override void Visit(INotEquality notEquality) {
      this.sourceEmitterOutput.Write("(");
      this.Visit(notEquality.LeftOperand);
      this.sourceEmitterOutput.Write(" != ");
      this.Visit(notEquality.RightOperand);
      this.sourceEmitterOutput.Write(")");
    }

    public override void Visit(IOldValue oldValue) {
      base.Visit(oldValue);
    }

    public override void Visit(IOnesComplement onesComplement) {
      base.Visit(onesComplement);
    }

    public override void Visit(IOperation operation) {
      base.Visit(operation);
    }

    public override void Visit(IOperationExceptionInformation operationExceptionInformation) {
      base.Visit(operationExceptionInformation);
    }

    public override void Visit(IOutArgument outArgument) {
      base.Visit(outArgument);
    }

    public override void Visit(IParameterTypeInformation parameterTypeInformation) {
      base.Visit(parameterTypeInformation);
    }

    public override void Visit(IPlatformInvokeInformation platformInvokeInformation) {
      base.Visit(platformInvokeInformation);
    }

    public override void Visit(IPointerCall pointerCall) {
      base.Visit(pointerCall);
    }

    public override void Visit(IPointerTypeReference pointerTypeReference) {
      base.Visit(pointerTypeReference);
    }

    public override void Visit(IPopValue popValue) {
      this.sourceEmitterOutput.Write("pop");
    }

    public override void Visit(IRefArgument refArgument) {
      base.Visit(refArgument);
    }

    public override void Visit(IResourceReference resourceReference) {
      base.Visit(resourceReference);
    }

    public override void Visit(IReturnValue returnValue) {
      this.sourceEmitterOutput.Write("result");
    }

    public override void Visit(IRightShift rightShift) {
      this.sourceEmitterOutput.Write("(");
      this.Visit(rightShift.LeftOperand);
      this.sourceEmitterOutput.Write(" >> ");
      this.Visit(rightShift.RightOperand);
      this.sourceEmitterOutput.Write(")");
    }

    public override void Visit(IRootUnitNamespaceReference rootUnitNamespaceReference) {
      base.Visit(rootUnitNamespaceReference);
    }

    public override void Visit(IRuntimeArgumentHandleExpression runtimeArgumentHandleExpression) {
      base.Visit(runtimeArgumentHandleExpression);
    }

    public override void Visit(ISecurityAttribute securityAttribute) {
      base.Visit(securityAttribute);
    }

    public override void Visit(ISizeOf sizeOf) {
      base.Visit(sizeOf);
    }

    public override void Visit(ISourceMethodBody methodBody) {
      base.Visit(methodBody);
    }

    public override void Visit(IStackArrayCreate stackArrayCreate) {
      base.Visit(stackArrayCreate);
    }

    public override void Visit(ISubtraction subtraction) {
      this.sourceEmitterOutput.Write("(");
      this.Visit(subtraction.LeftOperand);
      this.sourceEmitterOutput.Write(" - ");
      this.Visit(subtraction.RightOperand);
      this.sourceEmitterOutput.Write(")");
    }

    public override void Visit(ITargetExpression targetExpression) {
      IArrayIndexer/*?*/ indexer = targetExpression.Definition as IArrayIndexer;
      if (indexer != null) {
        this.Visit(indexer);
        return;
      }
      IAddressDereference/*?*/ deref = targetExpression.Definition as IAddressDereference;
      if (deref != null) {
        IAddressOf/*?*/ addressOf = deref.Address as IAddressOf;
        if (addressOf != null) {
          this.Visit(addressOf.Expression);
          return;
        }
        if (targetExpression.Instance != null) {
          this.Visit(targetExpression.Instance);
          this.sourceEmitterOutput.Write("->");
        } else if (deref.Address.Type is IPointerTypeReference)
          this.sourceEmitterOutput.Write("*");
        this.Visit(deref.Address);
        return;
      } else {
        if (targetExpression.Instance != null) {
          this.Visit(targetExpression.Instance);
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

    public override void Visit(IThisReference thisReference) {
      this.PrintToken(CSharpToken.This);
    }

    public override void Visit(ITypeDefinitionMember typeMember) {
      base.Visit(typeMember);
    }

    public override void Visit(ITypeMemberReference typeMemberReference) {
    }

    public override void Visit(ITokenOf tokenOf) {
      this.sourceEmitterOutput.Write("tokenof(");
      base.Visit(tokenOf);
      this.sourceEmitterOutput.Write(")");
    }

    public override void Visit(ITypeOf typeOf) {
      this.sourceEmitterOutput.Write("typeof(");
      this.Visit(typeOf.TypeToGet);
      this.sourceEmitterOutput.Write(")");
    }

    public override void Visit(ITypeReference typeReference) {
      this.PrintTypeReference(typeReference);
    }

    public override void Visit(IUnaryNegation unaryNegation) {
      base.Visit(unaryNegation);
    }

    public override void Visit(IUnaryPlus unaryPlus) {
      base.Visit(unaryPlus);
    }

    public override void Visit(IUnit unit) {
      base.Visit(unit);
    }

    public override void Visit(IUnitNamespaceReference unitNamespaceReference) {
      base.Visit(unitNamespaceReference);
    }

    public override void Visit(IUnitReference unitReference) {
      base.Visit(unitReference);
    }

    public override void Visit(IUnitSet unitSet) {
      base.Visit(unitSet);
    }

    public override void Visit(IUnitSetNamespace unitSetNamespace) {
      base.Visit(unitSetNamespace);
    }

    public override void Visit(IVectorLength vectorLength) {
      this.Visit(vectorLength.Vector);
      this.sourceEmitterOutput.Write(".Length");
    }

  }
}