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
      if (methodDefinition.IsConstructor && IteratorHelper.EnumerableIsEmpty(methodDefinition.Parameters)) return;
      if (methodDefinition.IsStaticConstructor) return;
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
        this.sourceEmitterOutput.WriteLine(";");
    }

    public virtual void PrintMethodDefinitionVisibility(IMethodDefinition methodDefinition) {
      PrintTypeMemberVisibility(methodDefinition.Visibility);
    }

    public virtual void PrintMethodDefinitionModifiers(IMethodDefinition methodDefinition) {
      if (methodDefinition.IsStatic)
        PrintKeywordStatic();

      if (methodDefinition.IsAbstract)
        PrintKeywordAbstract();

      if (methodDefinition.IsNewSlot)
        PrintKeywordNew();

      if (methodDefinition.IsSealed)
        PrintKeywordSealed();

      if (methodDefinition.IsVirtual && !methodDefinition.IsAbstract)
        PrintKeywordVirtual();
    }

    public virtual void PrintMethodDefinitionReturnType(IMethodDefinition methodDefinition) {
      if (methodDefinition.IsConstructor) return;
      PrintTypeReference(methodDefinition.Type);
    }

    public virtual void PrintMethodDefinitionName(IMethodDefinition methodDefinition) {
      if (methodDefinition.IsConstructor)
        PrintTypeDefinitionName(methodDefinition.ContainingTypeDefinition);
      else
        sourceEmitterOutput.Write(methodDefinition.Name.Value);
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
