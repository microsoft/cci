//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
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

    //internal bool StartsSwitchCase;

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

  internal class Pop : Expression {
    public override void Dispatch(ICodeVisitor visitor) {
      //Debug.Assert(false); //Objects of this class are not supposed to escape.
    }
  }

  internal sealed class PopAsUnsigned : Pop {
    public override void Dispatch(ICodeVisitor visitor) {
      //Debug.Assert(false); //Objects of this class are not supposed to escape.
    }
  }

  internal sealed class Push : Statement {
    internal IExpression ValueToPush;

    public override void Dispatch(ICodeVisitor visitor) {
      this.ValueToPush.Dispatch(visitor);
      //Debug.Assert(false); //Objects of this class are not supposed to escape.
    }
  }

  internal sealed class SwitchInstruction : Statement {
    internal IExpression switchExpression;
    internal readonly List<BasicBlock> switchCases = new List<BasicBlock>();

    public override void Dispatch(ICodeVisitor visitor) {
      //Debug.Assert(false); //Objects of this class are not supposed to escape.
    }
  }

  internal class UnSpecializedMethods {
    /// <summary>
    /// Get the unspecialized method definition is <paramref name="methodDefinition"/> is a specialized
    /// version, or itself otherwise. 
    /// </summary>
    /// <param name="methodDefinition"></param>
    /// <returns></returns>
    internal static IMethodDefinition UnSpecializedMethodDefinition(IMethodDefinition methodDefinition) {
      IGenericMethodInstance genericMethodInstance = methodDefinition as IGenericMethodInstance;
      if (genericMethodInstance != null) {
        return genericMethodInstance.GenericMethod.ResolvedMethod;
      }
      ISpecializedMethodDefinition specializedMethodDefinition = methodDefinition as ISpecializedMethodDefinition;
      if (specializedMethodDefinition != null)
        return specializedMethodDefinition.UnspecializedVersion;
      return methodDefinition;
    }

    internal static IMethodBody GetMethodBodyFromUnspecializedVersion(IMethodDefinition methodDefinition) {
      if (!methodDefinition.Body.Equals(Dummy.MethodBody)) return methodDefinition.Body;
      IGenericMethodInstance genericMethodInstance = methodDefinition as IGenericMethodInstance;
      if (genericMethodInstance != null)
        return GetMethodBodyFromUnspecializedVersion(genericMethodInstance.GenericMethod.ResolvedMethod);
      ISpecializedMethodDefinition specializedMethodDefinition = methodDefinition as ISpecializedMethodDefinition;
      if (specializedMethodDefinition != null)
        return GetMethodBodyFromUnspecializedVersion(specializedMethodDefinition.UnspecializedVersion.ResolvedMethod);
      return methodDefinition.Body;
    }

    public static bool IsCompilerGenerated(ITypeReference/*!*/ typeReference) {
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
      INestedTypeReference nestedTypeReference = UnSpecializedMethods.AsUnSpecializedNestedTypeReference(typeReference);
      if (nestedTypeReference != null) return IsCompilerGenerated(nestedTypeReference.ContainingType);
      return false;
    }

    public static bool IsCompilerGenerated(IMethodDefinition/*!*/ methodDefinition) {
      if (AttributeHelper.Contains(methodDefinition.Attributes, methodDefinition.ContainingType.PlatformType.SystemRuntimeCompilerServicesCompilerGeneratedAttribute))
        return true;
      IGenericMethodInstance genericMethodInstance = methodDefinition as IGenericMethodInstance;
      if (genericMethodInstance != null) return IsCompilerGenerated(genericMethodInstance.GenericMethod.ResolvedMethod);
      if (methodDefinition.ContainingType == null) return false;
      return IsCompilerGenerated(methodDefinition.ContainingType);
    }

    public static bool IsCompilerGenerated(IFieldReference/*!*/ fieldReference) {
      if (AttributeHelper.Contains(fieldReference.ResolvedField.Attributes, fieldReference.ContainingType.PlatformType.SystemRuntimeCompilerServicesCompilerGeneratedAttribute))
        return true;
      ISpecializedFieldReference specializedFieldReference = fieldReference as ISpecializedFieldReference;
      if (specializedFieldReference != null)
        return IsCompilerGenerated(specializedFieldReference.UnspecializedVersion);
      return IsCompilerGenerated(fieldReference.ContainingType);
    }
    /// <summary>
    /// Given a type reference <paramref name="typeReference"/>, convert it to an INestedTypeReference object if
    /// it is one, or if its unspecialized version is a INestedTypeReference. Otherwise return null. 
    /// </summary>
    /// <param name="typeReference"></param>
    /// <returns></returns>
    internal static INestedTypeReference/*?*/ AsUnSpecializedNestedTypeReference(ITypeReference typeReference) {
      INestedTypeReference nestedTypeReference = typeReference as INestedTypeReference;
      if (nestedTypeReference != null) return nestedTypeReference;
      IGenericTypeInstanceReference genericTypeInstanceReference = typeReference as IGenericTypeInstanceReference;
      if (genericTypeInstanceReference != null) {
        return genericTypeInstanceReference.GenericType as INestedTypeReference;
      }
      return null;
    }

    /// <summary>
    /// Given a type, if it is a specialized type, return its generic type. Otherwise return itself.
    /// </summary>
    /// <param name="typeReference"></param>
    /// <returns></returns>
    internal static ITypeReference/*!*/ AsUnSpecializedTypeReference(ITypeReference/*!*/ typeReference) {
      IGenericTypeInstanceReference genericTypeInstanceReference = typeReference as IGenericTypeInstanceReference;
      if (genericTypeInstanceReference != null)
        return AsUnSpecializedTypeReference(genericTypeInstanceReference.GenericType);
      ISpecializedNestedTypeReference specializedNestedTypeReference = typeReference as ISpecializedNestedTypeReference;
      if (specializedNestedTypeReference != null)
        return specializedNestedTypeReference.UnspecializedVersion;
      ISpecializedNestedTypeDefinition specializedNestedTypeDefinition = typeReference.ResolvedType as ISpecializedNestedTypeDefinition;
      if (specializedNestedTypeDefinition != null)
        return specializedNestedTypeDefinition.UnspecializedVersion;
      return typeReference;
    }
  }

  internal struct Pair<T1, T2> {
    public T1 One;
    public T2 Two;
    public Pair(T1 one, T2 two) {
      One = one; Two = two;
    }
  }
}