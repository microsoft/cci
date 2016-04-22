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
using System.Diagnostics.Contracts;

namespace CSharpSourceEmitter {
  public partial class SourceEmitter : CodeTraverser, ICSharpSourceEmitter {
    public override void TraverseChildren(IMethodDefinition methodDefinition) {
      if (!this.printCompilerGeneratedMembers) {
        if (methodDefinition.IsConstructor && methodDefinition.ParameterCount == 0 && 
        AttributeHelper.Contains(methodDefinition.Attributes, methodDefinition.Type.PlatformType.SystemRuntimeCompilerServicesCompilerGeneratedAttribute))
          return;

        // Skip if this is a method generated for use by a property or event
        foreach (var p in methodDefinition.ContainingTypeDefinition.Properties)
          if ((p.Getter != null && p.Getter.ResolvedMethod == methodDefinition) ||
          (p.Setter != null && p.Setter.ResolvedMethod == methodDefinition))
            return;
        foreach (var e in methodDefinition.ContainingTypeDefinition.Events)
          if ((e.Adder != null && e.Adder.ResolvedMethod == methodDefinition) ||
          (e.Remover != null && e.Remover.ResolvedMethod == methodDefinition))
            return;

        if (AttributeHelper.Contains(methodDefinition.Attributes, methodDefinition.Type.PlatformType.SystemRuntimeCompilerServicesCompilerGeneratedAttribute))
          return; // eg. an iterator helper - may have invalid identifier name
      }

      // Cctors should probably be outputted in some cases
      if (methodDefinition.IsStaticConstructor) return;

      foreach (var ma in SortAttributes(methodDefinition.Attributes))
        if (Utils.GetAttributeType(ma) != SpecialAttribute.Extension)
          PrintAttribute(methodDefinition, ma, true, null);

      foreach (var ra in SortAttributes(methodDefinition.ReturnValueAttributes))
        PrintAttribute(methodDefinition, ra, true, "return");

      PrintToken(CSharpToken.Indent);

      PrintMethodDefinitionVisibility(methodDefinition);
      PrintMethodDefinitionModifiers(methodDefinition);

      bool conversion = IsConversionOperator(methodDefinition);
      if (!conversion) {
        PrintMethodDefinitionReturnType(methodDefinition);
        if (!methodDefinition.IsConstructor && !IsDestructor(methodDefinition))
          PrintToken(CSharpToken.Space);
      }
      PrintMethodDefinitionName(methodDefinition);
      if (conversion)
        PrintMethodDefinitionReturnType(methodDefinition);

      if (methodDefinition.IsGeneric) {
        Traverse(methodDefinition.GenericParameters);
      }
      Traverse(methodDefinition.Parameters);
      if (methodDefinition.IsGeneric)
        PrintConstraints(methodDefinition.GenericParameters);
      if (!methodDefinition.IsAbstract && !methodDefinition.IsExternal)
        Traverse(methodDefinition.Body);
      else
        PrintToken(CSharpToken.Semicolon);
    }

    private void PrintConstraints(IEnumerable<IGenericMethodParameter> genericParameters) {
      Contract.Requires(genericParameters != null);

      this.sourceEmitterOutput.IncreaseIndent();
      foreach (var genpar in genericParameters) {
        bool first = true;
        if (genpar.MustBeReferenceType) {
          this.sourceEmitterOutput.WriteLine("");
          this.sourceEmitterOutput.Write("where " + genpar.Name.Value + ": class", true);
          first = false;
        }
        if (genpar.MustBeValueType) {
          this.sourceEmitterOutput.WriteLine("");
          this.sourceEmitterOutput.Write("where " + genpar.Name.Value + ": struct", true);
          first = false;
        }
        foreach (var c in genpar.Constraints) {
          if (TypeHelper.TypesAreEquivalent(c, c.PlatformType.SystemValueType)) continue;
          if (first) {
            this.sourceEmitterOutput.WriteLine("");
            this.sourceEmitterOutput.Write("where " + genpar.Name.Value + ": ", true);
            first = false;
          } else {
            this.sourceEmitterOutput.Write(", ");
          }
          this.PrintTypeReference(c);
        }
        if (genpar.MustHaveDefaultConstructor && !genpar.MustBeValueType) {
          if (first) {
            this.sourceEmitterOutput.WriteLine("");
            this.sourceEmitterOutput.Write("where " + genpar.Name.Value + ": new ()", true);
          } else {
            this.sourceEmitterOutput.Write(", new ()");
          }
        }
      }
      this.sourceEmitterOutput.DecreaseIndent();
    }

    private void foo<T, U>()
      where T : class
      where U : struct {
    }

    public virtual void PrintMethodDefinitionVisibility(IMethodDefinition methodDefinition) {
      Contract.Requires(methodDefinition != null);

      if (!IsDestructor(methodDefinition) &&
        !methodDefinition.ContainingTypeDefinition.IsInterface &&
        IteratorHelper.EnumerableIsEmpty(MemberHelper.GetExplicitlyOverriddenMethods(methodDefinition)))
        PrintTypeMemberVisibility(methodDefinition.Visibility);
    }

    public virtual bool IsMethodUnsafe(IMethodDefinition/*?*/ methodDefinition) {
      if (methodDefinition == null) return false;
      foreach (var p in methodDefinition.Parameters) {
        if (p.Type.TypeCode == PrimitiveTypeCode.Pointer)
          return true;
      }
      if (methodDefinition.Type.TypeCode == PrimitiveTypeCode.Pointer)
        return true;
      return false;
    }

    public virtual bool IsOperator(IMethodDefinition methodDefinition) {
      Contract.Requires(methodDefinition != null);

      return (methodDefinition.IsSpecialName && methodDefinition.Name.Value.StartsWith("op_"));
    }

    public virtual bool IsConversionOperator(IMethodDefinition methodDefinition) {
      Contract.Requires(methodDefinition != null);

      return (methodDefinition.IsSpecialName && (
        methodDefinition.Name.Value == "op_Explicit" || methodDefinition.Name.Value == "op_Implicit"));
    }

    public virtual bool IsDestructor(IMethodDefinition methodDefinition) {
      Contract.Requires(methodDefinition != null);

      if (methodDefinition.ContainingTypeDefinition.IsValueType) return false; //only classes can have destructors
      if (methodDefinition.ParameterCount == 0 &&   methodDefinition.IsVirtual && !methodDefinition.IsNewSlot && 
        methodDefinition.Visibility == TypeMemberVisibility.Family && methodDefinition.Name.Value == "Finalize") {
        // Try to make sure that this Finalize method overrides the protected System.Object.Finalize
        return this.IsDestructor(methodDefinition, methodDefinition.ContainingTypeDefinition);
      }
      return false;
    }

    private bool IsDestructor(IMethodDefinition methodDefinition, ITypeReference baseClassReference) {
      Contract.Requires(baseClassReference != null);

      var baseClass = baseClassReference.ResolvedType;
      if (baseClass is Dummy) return true; //It might not be true, but it LOOKS true and we can't tell for sure. So give up and pretend it is true.
      var baseFinalize = TypeHelper.GetMethod(baseClass.GetMembersNamed(methodDefinition.Name, false), methodDefinition);
      if (!(baseFinalize is Dummy) && baseFinalize.IsNewSlot) return TypeHelper.TypesAreEquivalent(baseClass, baseClass.PlatformType.SystemObject);
      foreach (var bbcRef in baseClass.BaseClasses)
        return IsDestructor(methodDefinition, bbcRef);
      return true; //Did not find Finalize in System.Object, which means that we're clueless anyway.
    }

    public virtual void PrintMethodDefinitionModifiers(IMethodDefinition methodDefinition) {
      Contract.Requires(methodDefinition != null);

      // This algorithm is probably not exactly right yet.
      // TODO: Compare to FrameworkDesignStudio rules (see CCIModifiers project, and AssemblyDocumentWriter.WriteMemberStart)

      if (IsMethodUnsafe(methodDefinition))
        PrintKeywordUnsafe();

      if (!(Utils.GetHiddenBaseClassMethod(methodDefinition) is Dummy))
        PrintKeywordNew();

      if (methodDefinition.ContainingTypeDefinition.IsInterface) {
        // Defining an interface method - 'unsafe' and 'new' are the only valid modifier
        return;
      }

      if (!methodDefinition.IsAbstract && methodDefinition.IsExternal)
        PrintKeywordExtern();

      if (IsDestructor(methodDefinition))
        return;

      if (methodDefinition.IsStatic) {
        PrintKeywordStatic();
      } else if (methodDefinition.IsVirtual) {
        if (methodDefinition.IsNewSlot && 
          (IteratorHelper.EnumerableIsNotEmpty(MemberHelper.GetImplicitlyImplementedInterfaceMethods(methodDefinition)) ||
            IteratorHelper.EnumerableIsNotEmpty(MemberHelper.GetExplicitlyOverriddenMethods(methodDefinition)))) {
          // Implementing a method defined on an interface: implicitly virtual and sealed
          if (methodDefinition.IsAbstract)
            PrintKeywordAbstract();
          else if (!methodDefinition.IsSealed)
            PrintKeywordVirtual();
        } else {
          // Instance method on a class
          if (methodDefinition.IsAbstract)
            PrintKeywordAbstract();

          if (methodDefinition.IsNewSlot) {
            // Only overrides (or interface impls) can be sealed in C#.  If this is
            // a new sealed virtual then just emit as non-virtual which is a similar thing.
            // We get these in reference assemblies for methods which were implementations of private (and so removed)
            // interfaces.
            if (!methodDefinition.IsSealed && !methodDefinition.IsAbstract)
              PrintKeywordVirtual();
          } else {
            PrintKeywordOverride();
            if (methodDefinition.IsSealed)
              PrintKeywordSealed();
          }
        }
      }
    }

    public virtual void PrintMethodDefinitionReturnType(IMethodDefinition methodDefinition) {
      if (methodDefinition == null)
        this.sourceEmitterOutput.Write("unknown");
      else if (!methodDefinition.IsConstructor && !IsDestructor(methodDefinition) /*&& !IsOperator(methodDefinition)*/)
        PrintTypeReference(methodDefinition.Type);
    }

    public virtual void PrintMethodDefinitionName(IMethodDefinition methodDefinition) {
      Contract.Requires(methodDefinition != null);

      bool isDestructor = IsDestructor(methodDefinition);
      if (isDestructor)
        PrintToken(CSharpToken.Tilde);
      if (methodDefinition.IsConstructor || isDestructor)
        PrintTypeDefinitionName(methodDefinition.ContainingTypeDefinition);
      else if (IsOperator(methodDefinition)) {
        sourceEmitterOutput.Write(MapOperatorNameToCSharp(methodDefinition));
      } else
        PrintIdentifier(methodDefinition.Name);
    }

    public virtual string MapOperatorNameToCSharp(IMethodDefinition methodDefinition) {
      Contract.Requires(methodDefinition != null);
      // ^ requires IsOperator(methodDefinition)
      switch (methodDefinition.Name.Value) {
        case "op_Decrement": return "operator --";
        case "op_Increment": return "operator ++";
        case "op_UnaryNegation": return "operator -";
        case "op_UnaryPlus": return "operator +";
        case "op_LogicalNot": return "operator !";
        case "op_OnesComplement": return "operator ~";
        case "op_True": return "operator true";
        case "op_False": return "operator false";
        case "op_Addition": return "operator +";
        case "op_Subtraction": return "operator -";
        case "op_Multiply": return "operator *";
        case "op_Division": return "operator /";
        case "op_Modulus": return "operator %";
        case "op_ExclusiveOr": return "operator ^";
        case "op_BitwiseAnd": return "operator &";
        case "op_BitwiseOr": return "operator |";
        case "op_LeftShift": return "operator <<";
        case "op_RightShift": return "operator >>";
        case "op_Equality": return "operator ==";
        case "op_GreaterThan": return "operator >";
        case "op_LessThan": return "operator <";
        case "op_Inequality": return "operator !=";
        case "op_GreaterThanOrEqual": return "operator >=";
        case "op_LessThanOrEqual": return "operator <=";
        case "op_Explicit": return "explicit operator ";
        case "op_Implicit": return "implicit operator ";
        default: return methodDefinition.Name.Value; // other unsupported by C# directly
      }
    }
    public virtual void PrintMethodReferenceName(IMethodReference methodReference, NameFormattingOptions options) {
      Contract.Requires(methodReference != null);

      string signature = MemberHelper.GetMethodSignature(methodReference, options|NameFormattingOptions.ContractNullable|NameFormattingOptions.UseTypeKeywords);
      if (signature.EndsWith(".get") || signature.EndsWith(".set"))
        signature = signature.Substring(0, signature.Length-4);
      if (methodReference.Name.Value == ".ctor")
        PrintTypeReferenceName(methodReference.ContainingType);
      else
        sourceEmitterOutput.Write(signature);
    }

    public override void Traverse(IMethodBody methodBody) {
      PrintToken(CSharpToken.LeftCurly);

      var sourceBody = methodBody as ISourceMethodBody;
      if (sourceBody != null)
        base.Traverse(sourceBody);

      PrintToken(CSharpToken.RightCurly);
    }

  }
}
