//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Cci;

namespace CSharpSourceEmitter {
  public partial class SourceEmitter : BaseCodeTraverser, ICSharpSourceEmitter {
    public override void Visit(IMethodDefinition methodDefinition) {

      // Skip if this is a method generated for use by a property or event
      foreach (var p in methodDefinition.ContainingTypeDefinition.Properties)
        if ((p.Getter != null && p.Getter.ResolvedMethod == methodDefinition) ||
          (p.Setter != null && p.Setter.ResolvedMethod == methodDefinition))
          return;
      foreach (var e in methodDefinition.ContainingTypeDefinition.Events)
        if ((e.Adder != null && e.Adder.ResolvedMethod == methodDefinition) ||
          (e.Remover != null && e.Remover.ResolvedMethod == methodDefinition))
          return;

      // Cctors should probably be outputted in some cases
      if (methodDefinition.IsStaticConstructor) return;

      PrintAttributes(methodDefinition.Attributes);
      foreach (var ra in methodDefinition.ReturnValueAttributes)
        PrintAttribute(ra, true, "return");

      PrintToken(CSharpToken.Indent);

      PrintMethodDefinitionVisibility(methodDefinition);
      PrintMethodDefinitionModifiers(methodDefinition);
      PrintMethodDefinitionReturnType(methodDefinition);
      PrintToken(CSharpToken.Space);
      PrintMethodDefinitionName(methodDefinition);
      if (methodDefinition.IsGeneric) {
        Visit(methodDefinition.GenericParameters);
      }
      Visit(methodDefinition.Parameters);
      if (!methodDefinition.IsAbstract && !methodDefinition.IsExternal)
        Visit(methodDefinition.Body);
      else
        PrintToken(CSharpToken.Semicolon);
    }

    public virtual void PrintMethodDefinitionVisibility(IMethodDefinition methodDefinition) {
      if (!IsDestructor(methodDefinition) &&
        !methodDefinition.ContainingTypeDefinition.IsInterface &&
        IteratorHelper.EnumerableIsEmpty(MemberHelper.GetExplicitlyOverriddenMethods(methodDefinition)))
        PrintTypeMemberVisibility(methodDefinition.Visibility);
    }

    public virtual bool IsMethodUnsafe(IMethodDefinition methodDefinition) {
      foreach (var p in methodDefinition.Parameters) {
        if (p.Type.TypeCode == PrimitiveTypeCode.Pointer)
          return true;
      }
      if (methodDefinition.Type.TypeCode == PrimitiveTypeCode.Pointer)
        return true;
      return false;
    }

    public virtual bool IsDestructor(IMethodDefinition methodDefinition) {

      if (methodDefinition.Name.Value == "Finalize" && methodDefinition.ParameterCount == 0)  // quick check
      {
        // Verify that this Finalize method override the public System.Object.Finalize
        var typeDef = methodDefinition.ContainingTypeDefinition;
        var objType = typeDef.PlatformType.SystemObject.ResolvedType;
        var finMethod = (IMethodDefinition)IteratorHelper.Single(
          objType.GetMatchingMembersNamed(methodDefinition.Name, false, m => m.Visibility == TypeMemberVisibility.Family));
        if (MemberHelper.GetImplicitlyOverridingDerivedClassMethod(finMethod, typeDef) != Dummy.Method)
          return true;
      }
      return false;
    }

    public virtual void PrintMethodDefinitionModifiers(IMethodDefinition methodDefinition) {

      // This algorithm is probably not exactly right yet.
      // TODO: Compare to FrameworkDesignStudio rules (see CCIModifiers project, and AssemblyDocumentWriter.WriteMemberStart)

      if (IsMethodUnsafe(methodDefinition))
        PrintKeywordUnsafe();

      if (Utils.GetHiddenBaseClassMethod(methodDefinition) != Dummy.Method)
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
          else if (methodDefinition.IsNewSlot)
            PrintKeywordVirtual();
          else {
            PrintKeywordOverride();
            if (methodDefinition.IsSealed)
              PrintKeywordSealed();
          }
        }
      }
    }

    public virtual void PrintMethodDefinitionReturnType(IMethodDefinition methodDefinition) {
      if (!methodDefinition.IsConstructor && !IsDestructor(methodDefinition))
        PrintTypeReference(methodDefinition.Type);
    }

    public virtual void PrintMethodDefinitionName(IMethodDefinition methodDefinition) {
      bool isDestructor = IsDestructor(methodDefinition);
      if (isDestructor)
        PrintToken(CSharpToken.Tilde);
      if (methodDefinition.IsConstructor || isDestructor)
        PrintTypeDefinitionName(methodDefinition.ContainingTypeDefinition);
      else
        PrintIdentifier(methodDefinition.Name);
    }

    public virtual void PrintMethodReferenceName(IMethodReference methodReference, NameFormattingOptions options) {
      string signature = MemberHelper.GetMethodSignature(methodReference, options|NameFormattingOptions.ContractNullable|NameFormattingOptions.UseTypeKeywords);
      if (methodReference.Name.Value == ".ctor")
        PrintTypeReferenceName(methodReference.ContainingType);
      else
        sourceEmitterOutput.Write(signature);
    }

    public override void Visit(IMethodBody methodBody) {
      PrintToken(CSharpToken.NewLine);
      PrintToken(CSharpToken.LeftCurly);

      //base.Visit(methodBody);

      PrintToken(CSharpToken.RightCurly);
      PrintToken(CSharpToken.NewLine);
    }

  }
}
