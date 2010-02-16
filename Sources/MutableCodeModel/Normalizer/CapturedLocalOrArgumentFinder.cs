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
using Microsoft.Cci.MutableCodeModel;
using System.Collections.Generic;
using Microsoft.Cci.Contracts;

namespace Microsoft.Cci.MutableCodeModel {

  internal class ClosureFinder : BaseCodeAndContractTraverser {

    internal Dictionary<object, BoundField> fieldForCapturedLocalOrParameter;
    Dictionary<ILocalDefinition, bool> localsToCapture = new Dictionary<ILocalDefinition, bool>();
    IAnonymousDelegate/*?*/ currentAnonymousDelegate;
    INameTable nameTable;
    internal bool foundAnonymousDelegate;
    internal bool foundYield;

    internal ClosureFinder(Dictionary<object, BoundField> fieldForCapturedLocalOrParameter, INameTable nameTable, IContractProvider/*?*/ contractProvider)
      : base(contractProvider) {
      this.fieldForCapturedLocalOrParameter = fieldForCapturedLocalOrParameter;
      this.nameTable = nameTable;
    }

    private void CaptureDefinition(object definition) {
      IThisReference/*?*/ thisRef = definition as IThisReference;
      if (thisRef != null)
        definition = thisRef.Type.ResolvedType;
      if (this.fieldForCapturedLocalOrParameter.ContainsKey(definition)) return;
      IName/*?*/ name = null;
      ITypeReference/*?*/ type = null;
      ILocalDefinition/*?*/ local = definition as ILocalDefinition;
      if (local != null) {
        if (!this.localsToCapture.ContainsKey(local)) return;
        name = local.Name;
        type = local.Type;
      } else {
        IParameterDefinition/*?*/ par = definition as IParameterDefinition;
        if (par != null) {
          if (par.ContainingSignature == this.currentAnonymousDelegate) return;
          name = par.Name;
          type = par.Type;
        } else {
          type = definition as ITypeDefinition;
          if (type == null) return;
          name = this.nameTable.GetNameFor("__this value");
        }
      }
      if (name == null) return;
      FieldDefinition field = new FieldDefinition();
      field.Name = name;
      field.Type = type;
      field.Visibility = TypeMemberVisibility.Public;
      BoundField be = new BoundField(field, type);
      this.fieldForCapturedLocalOrParameter.Add(definition, be);
    }

    public override void Visit(IAnonymousDelegate anonymousDelegate) {
      this.foundAnonymousDelegate = true;
      IAnonymousDelegate/*?*/ savedCurrentAnonymousDelegate = this.currentAnonymousDelegate;
      this.currentAnonymousDelegate = anonymousDelegate;
      base.Visit(anonymousDelegate);
      this.currentAnonymousDelegate = savedCurrentAnonymousDelegate;
    }

    public override void Visit(IAddressableExpression addressableExpression) {
      base.Visit(addressableExpression);
      if (this.currentAnonymousDelegate != null)
        this.CaptureDefinition(addressableExpression.Definition);
    }

    public override void Visit(IBaseClassReference baseClassReference) {
      base.Visit(baseClassReference);
    }

    public override void Visit(IBoundExpression boundExpression) {
      base.Visit(boundExpression);
      if (this.currentAnonymousDelegate != null) {
        if (boundExpression.Instance != null)
          this.CaptureDefinition(boundExpression.Instance);
        this.CaptureDefinition(boundExpression.Definition);
      }
    }

    public override void Visit(ILocalDeclarationStatement localDeclarationStatement) {
      if (this.currentAnonymousDelegate == null)
        this.localsToCapture[localDeclarationStatement.LocalVariable] = true;
      base.Visit(localDeclarationStatement);
    }

    public override void Visit(ITargetExpression targetExpression) {
      base.Visit(targetExpression);
      if (this.currentAnonymousDelegate != null)
        this.CaptureDefinition(targetExpression.Definition);
    }

    public override void Visit(IThisReference thisReference) {
      base.Visit(thisReference);
      if (this.currentAnonymousDelegate != null)
        this.CaptureDefinition(thisReference);
    }

    public override void Visit(IYieldBreakStatement yieldBreakStatement) {
      this.foundYield = true;
      base.Visit(yieldBreakStatement);
    }

    public override void Visit(IYieldReturnStatement yieldReturnStatement) {
      this.foundYield = true;
      base.Visit(yieldReturnStatement);
    }

  }
}
