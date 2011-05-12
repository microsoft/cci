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
using System.Collections.Generic;
using Microsoft.Cci.MutableCodeModel;

namespace Microsoft.Cci.ILToCodeModel {

  /// <summary>
  /// A block of statements that can only be reached by branching to the first statement in the block.
  /// </summary>
  public sealed class BasicBlock : BlockStatement {

    /// <summary>
    /// Allocates a block of statements that can only be reached by branching to the first statement in the block.
    /// </summary>
    /// <param name="startOffset">The IL offset of the first statement in the block.</param>
    public BasicBlock(uint startOffset) {
      this.StartOffset = startOffset;
    }

    internal uint EndOffset;

    internal IOperationExceptionInformation/*?*/ ExceptionInformation;

    internal List<ILocalDefinition>/*?*/ LocalVariables;

    internal int NumberOfTryBlocksStartingHere;

    internal uint StartOffset;

  }

  internal sealed class ConvertToUnsigned : Expression, IConversion {

    internal ConvertToUnsigned(IExpression valueToConvert) {
      this.valueToConvert = valueToConvert;
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    public IExpression ValueToConvert {
      get { return this.valueToConvert; }
    }
    IExpression valueToConvert;

    public bool CheckNumericRange {
      get { return false; }
    }

    public ITypeReference TypeAfterConversion {
      get { return TypeHelper.UnsignedEquivalent(this.ValueToConvert.Type); }
    }

    ITypeReference IExpression.Type {
      get { return this.TypeAfterConversion; }
    }

  }

  internal sealed class Dup : Expression {
    public override void Dispatch(ICodeVisitor visitor) {
      //Debug.Assert(false); //Objects of this class are not supposed to escape.
    }
  }

  internal sealed class EndFilter : Statement {
    internal Expression FilterResult;

    public override void Dispatch(ICodeVisitor visitor) {
      //Debug.Assert(false); //Objects of this class are not supposed to escape.
    }
  }

  internal sealed class EndFinally : Statement {
    public override void Dispatch(ICodeVisitor visitor) {
      //Debug.Assert(false); //Objects of this class are not supposed to escape.
    }
  }

  internal class Pop : Expression, IPopValue {
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }
  }

  internal sealed class PopAsUnsigned : Pop {
  }

  internal sealed class SwitchInstruction : Statement {
    internal IExpression switchExpression;
    internal readonly List<GotoStatement> switchCases = new List<GotoStatement>();

    public override void Dispatch(ICodeVisitor visitor) {
      //Debug.Assert(false); //Objects of this class are not supposed to escape.
    }
  }

  internal class UnspecializedMethods {
    /// <summary>
    /// Get the unspecialized method definition is <paramref name="methodDefinition"/> is a specialized
    /// version, or itself otherwise. 
    /// </summary>
    /// <param name="methodDefinition"></param>
    /// <returns></returns>
    internal static IMethodDefinition UnspecializedMethodDefinition(IMethodDefinition methodDefinition) {
      IGenericMethodInstance genericMethodInstance = methodDefinition as IGenericMethodInstance;
      if (genericMethodInstance != null) {
        methodDefinition = genericMethodInstance.GenericMethod.ResolvedMethod;
      }
      ISpecializedMethodDefinition specializedMethodDefinition = methodDefinition as ISpecializedMethodDefinition;
      if (specializedMethodDefinition != null)
        return specializedMethodDefinition.UnspecializedVersion;
      return methodDefinition;
    }

    /// <summary>
    /// Get the unspecialized version of the given field definition, if it is specialized. Or the field 
    /// definition itself, otherwise.
    /// </summary>
    /// <param name="fieldDefinition"></param>
    /// <returns></returns>
    internal static IFieldDefinition UnspecializedFieldDefinition(IFieldDefinition fieldDefinition) {
      ISpecializedFieldDefinition specializedFieldDefinition = fieldDefinition as ISpecializedFieldDefinition;
      if (specializedFieldDefinition != null) return specializedFieldDefinition.UnspecializedVersion;
      return fieldDefinition;
    }

    /// <summary>
    /// Get the unspecialized version of the given field reference, if it is specialized. Or the field 
    /// reference itself, otherwise.
    /// </summary>
    /// <param name="fieldReference"></param>
    /// <returns></returns>
    internal static IFieldReference UnspecializedFieldReference(IFieldReference fieldReference) {
      ISpecializedFieldReference specializedFieldReference = fieldReference as ISpecializedFieldReference;
      if (specializedFieldReference != null) return specializedFieldReference.UnspecializedVersion;
      return fieldReference;
    }

    /// <summary>
    /// A specialized method definition or generic method instance does not have a body. Given a method definition,
    /// find the unspecialized version of the definition and fetch the body. 
    /// </summary>
    /// <param name="methodDefinition"></param>
    /// <returns></returns>
    internal static IMethodBody GetMethodBodyFromUnspecializedVersion(IMethodDefinition methodDefinition) {
      IGenericMethodInstance genericMethodInstance = methodDefinition as IGenericMethodInstance;
      if (genericMethodInstance != null)
        return GetMethodBodyFromUnspecializedVersion(genericMethodInstance.GenericMethod.ResolvedMethod);
      ISpecializedMethodDefinition specializedMethodDefinition = methodDefinition as ISpecializedMethodDefinition;
      if (specializedMethodDefinition != null)
        return GetMethodBodyFromUnspecializedVersion(specializedMethodDefinition.UnspecializedVersion.ResolvedMethod);
      return methodDefinition.Body;
    }

    /// <summary>
    /// See if a type reference refers to a type definition that is compiler generated. 
    /// </summary>
    /// <param name="typeReference"></param>
    /// <returns></returns>
    internal static bool IsCompilerGenerated(ITypeReference/*!*/ typeReference) {
      if (AttributeHelper.Contains(typeReference.ResolvedType.Attributes, typeReference.PlatformType.SystemRuntimeCompilerServicesCompilerGeneratedAttribute))
        return true;
      IGenericTypeInstanceReference genericTypeInstanceReference = typeReference as IGenericTypeInstanceReference;
      if (genericTypeInstanceReference != null && IsCompilerGenerated(genericTypeInstanceReference.GenericType)) {
        return true;
      }
      ISpecializedNestedTypeReference specializedNestedType = typeReference as ISpecializedNestedTypeReference;
      if (specializedNestedType != null && IsCompilerGenerated(specializedNestedType.UnspecializedVersion)) {
        return true;
      }
      ISpecializedNestedTypeDefinition specializedNestedTypeDefinition = typeReference as ISpecializedNestedTypeDefinition;
      if (specializedNestedTypeDefinition != null && IsCompilerGenerated(specializedNestedTypeDefinition.UnspecializedVersion))
        return true;
      INestedTypeReference nestedTypeReference = UnspecializedMethods.AsUnspecializedNestedTypeReference(typeReference);
      if (nestedTypeReference != null) return IsCompilerGenerated(nestedTypeReference.ContainingType);
      return false;
    }

    /// <summary>
    /// See if a method definition is compiler generated, or is inside a compiler generated type.
    /// </summary>
    /// <param name="methodDefinition"></param>
    /// <returns></returns>
    internal static bool IsCompilerGenerated(IMethodDefinition/*!*/ methodDefinition) {
      if (AttributeHelper.Contains(methodDefinition.Attributes, methodDefinition.ContainingType.PlatformType.SystemRuntimeCompilerServicesCompilerGeneratedAttribute))
        return true;
      IGenericMethodInstance genericMethodInstance = methodDefinition as IGenericMethodInstance;
      if (genericMethodInstance != null) return IsCompilerGenerated(genericMethodInstance.GenericMethod.ResolvedMethod);
      if (methodDefinition.ContainingType == null) return false;
      return IsCompilerGenerated(methodDefinition.ContainingType);
    }

    /// <summary>
    /// Returns true if the given field reference refers to a field that is compiler generated.
    /// </summary>
    public static bool IsCompilerGenerated(IFieldReference/*!*/ fieldReference) {
      var specializedFieldReference = fieldReference as ISpecializedFieldReference;
      if (specializedFieldReference != null) return IsCompilerGenerated(specializedFieldReference.UnspecializedVersion);
      if (IsCompilerGenerated(fieldReference.ContainingType)) return true;
      var field = fieldReference.ResolvedField; //This is unfortunate, but in the case of a (specialized) field reference where the containing type is a generic type instance
      //the unspecialized field is not a definition even if the generic type template is defined in the same assembly. Resolving seems to be the only way to tell.
      return AttributeHelper.Contains(field.Attributes, field.ContainingType.PlatformType.SystemRuntimeCompilerServicesCompilerGeneratedAttribute);
    }

    /// <summary>
    /// If the type reference refers to a nested type, return a reference to the uninstantiated and/or unspecialized version 
    /// of the nested type. Otherwise return null. 
    /// </summary>
    internal static INestedTypeReference/*?*/ AsUnspecializedNestedTypeReference(ITypeReference typeReference) {
      var genericTypeInstanceReference = typeReference as IGenericTypeInstanceReference;
      if (genericTypeInstanceReference != null) return AsUnspecializedNestedTypeReference(genericTypeInstanceReference.GenericType);
      var specializedNestedTypeReference = typeReference as ISpecializedNestedTypeReference;
      if (specializedNestedTypeReference != null) return specializedNestedTypeReference.UnspecializedVersion;
      return typeReference as INestedTypeReference;
    }

    /// <summary>
    /// Given a type, if it is a specialized type, return its generic type. Otherwise return itself.
    /// If the type reference refers to a nested type, return a reference to the uninstantiated and/or unspecialized version 
    /// of the nested type. Otherwise return the given type reference. 
    /// </summary>
    internal static ITypeReference/*!*/ AsUnspecializedTypeReference(ITypeReference/*!*/ typeReference) {
      IGenericTypeInstanceReference genericTypeInstanceReference = typeReference as IGenericTypeInstanceReference;
      if (genericTypeInstanceReference != null) return AsUnspecializedTypeReference(genericTypeInstanceReference.GenericType);
      var specializedNestedTypeReference = typeReference as ISpecializedNestedTypeReference;
      if (specializedNestedTypeReference != null) return specializedNestedTypeReference.UnspecializedVersion;
      return typeReference;
    }
  }

}