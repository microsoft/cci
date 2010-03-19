//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using Microsoft.Cci;
using System.Diagnostics;
using Microsoft.Cci.MutableCodeModel;

//^ using Microsoft.Contracts;

  public enum ErrorCode {
    GenericMethodInstanceResolution = 100,
    MethodResolution,
    SpecializedMethodResolution,

    FunctionPointerTypeResolution = 200,
    GenericTypeInstanceResolution,
    GenericMethodParameterResolution,
    GenericTypeParameterResolution,
    NamespaceTypeResolution, 
    NestedTypeResolution,
    PointerTypeResolution,
    SpecializedNestedTypeResolution,

    FieldResolution = 300,
    SpecializedFieldResolution, 

    ContainingTypeDefinitionMismatch = 400,
  }

  public struct ErrorNode {
    ErrorCode code;
    public ErrorCode Code {
      set { this.code = value; }
      get { return this.code; }
    }
    IObjectWithLocations node;
    public IObjectWithLocations Node {
      get { return this.node; }
      set { this.node = value; }
    }
  }
  /// <summary>
  /// A checker that checks the following over a mutable model.
  /// 1) That every reference is resolved to a proper definition.
  /// 2) That generic applications are type-correct.
  /// 3) Property accessors are the same objects as the setter or getter.
  /// 4) Event accessors are the same objects as the adder or remover.
  /// 5) AllTypes in a module are the same objects as the definitions.
  /// 6) For source method body, Method calls are type-correct.
  /// 7) For source method body, local def, parameter def and property def that appear in an expression is the same object
  /// as the definition.
  /// 8) ContainingTypeDefinition in method definitions are the same objects as the containing type definition. 
  /// </summary>
  public class Checker : CodeMutatingVisitor {
    /// <summary>
    /// A checker that checks the consistency of a code model. 
    /// </summary>
    /// <param name="host"></param>
    public Checker(IMetadataHost host)
      : base(host) {
    }

    private Dictionary<IObjectWithLocations, ErrorNode> errors = new Dictionary<IObjectWithLocations, ErrorNode>();

    /// <summary>
    /// Get the errors from the checker. The keys are the offensive nodes and error node are error information. 
    /// </summary>
    public Dictionary<IObjectWithLocations, ErrorNode> Errors {
      get {
        return errors;
      }
    }

    private void EmitError(IObjectWithLocations node, ErrorCode code) {
      if (this.errors.ContainsKey(node)) return;
      this.errors.Add(node, new ErrorNode() { Node = node, Code = code });
    }

    public override IGenericMethodInstanceReference Visit(IGenericMethodInstanceReference genericMethodInstanceReference) {
      var methodDef = genericMethodInstanceReference.ResolvedMethod;
      if (methodDef == Dummy.Method) {
        this.EmitError(genericMethodInstanceReference, ErrorCode.GenericMethodInstanceResolution);
      }
      return base.Visit(genericMethodInstanceReference);
    }

    public override IFunctionPointerTypeReference Visit(IFunctionPointerTypeReference functionPointerTypeReference) {
      var typ = functionPointerTypeReference.ResolvedType;
      if (typ == Dummy.Type) {
        this.EmitError(functionPointerTypeReference, ErrorCode.FunctionPointerTypeResolution);
      }
      return base.Visit(functionPointerTypeReference);
    }

    public override ISpecializedNestedTypeReference Visit(ISpecializedNestedTypeReference specializedNestedTypeReference) {
      var typ = specializedNestedTypeReference.ResolvedType;
      if (typ == Dummy.Type) {
        this.EmitError(specializedNestedTypeReference, ErrorCode.SpecializedNestedTypeResolution);
      }
      return base.Visit(specializedNestedTypeReference);
    }
    public override IPointerTypeReference Visit(IPointerTypeReference pointerTypeReference) {
      var typ = pointerTypeReference.ResolvedType;
      if (typ == Dummy.Type) {
        this.EmitError(pointerTypeReference, ErrorCode.PointerTypeResolution);
      }
      return base.Visit(pointerTypeReference);
    }
    public override INestedTypeReference Visit(INestedTypeReference nestedTypeReference) {
      var typ = nestedTypeReference.ResolvedType;
      if (typ == Dummy.Type) {
        this.EmitError(nestedTypeReference, ErrorCode.NestedTypeResolution);
      }
      return base.Visit(nestedTypeReference);
    }
    public override ISpecializedMethodReference Visit(ISpecializedMethodReference specializedMethodReference) {
      var method = specializedMethodReference.ResolvedMethod;
      if (method == Dummy.Method) {
        this.EmitError(specializedMethodReference, ErrorCode.SpecializedMethodResolution);
      }
      return base.Visit(specializedMethodReference);
    }
    public override ISpecializedFieldReference Visit(ISpecializedFieldReference specializedFieldReference) {
      var field = specializedFieldReference.ResolvedField;
      if (field == Dummy.Field) {
        this.EmitError(specializedFieldReference, ErrorCode.SpecializedFieldResolution);
      }
      return base.Visit(specializedFieldReference);
    }
    public override INamespaceTypeReference Visit(INamespaceTypeReference namespaceTypeReference) {
      var typ = namespaceTypeReference.ResolvedType;
      if (typ == Dummy.Type) {
        this.EmitError(namespaceTypeReference, ErrorCode.NamespaceTypeResolution);
      }
      return base.Visit(namespaceTypeReference);
    }
    public override IArrayTypeReference Visit(IArrayTypeReference arrayTypeReference) {
      var typ = arrayTypeReference.ResolvedType;
      if (typ == Dummy.Type) {
        this.EmitError(arrayTypeReference, ErrorCode.NamespaceTypeResolution);
      }
      return base.Visit(arrayTypeReference);
    }
    public override IGenericMethodParameterReference Visit(IGenericMethodParameterReference genericMethodParameterReference) {
      var typ = genericMethodParameterReference.ResolvedType;
      if (typ == Dummy.Type) {
        this.EmitError(genericMethodParameterReference, ErrorCode.GenericMethodParameterResolution);
      } else if ((typ.Name != genericMethodParameterReference.Name && !genericMethodParameterReference.Name.Value.StartsWith("!")) || typ.Index != genericMethodParameterReference.Index) {
        this.EmitError(genericMethodParameterReference, ErrorCode.GenericMethodParameterResolution);
      }
      return base.Visit(genericMethodParameterReference);
    }
    public override IGenericTypeInstanceReference Visit(IGenericTypeInstanceReference genericTypeInstanceReference) {
      var typ = genericTypeInstanceReference.ResolvedType;
      if (typ == Dummy.Type) {
        this.EmitError(genericTypeInstanceReference, ErrorCode.GenericTypeInstanceResolution);
      }
      return base.Visit(genericTypeInstanceReference);
    }
    public override IGenericTypeParameterReference Visit(IGenericTypeParameterReference genericTypeParameterReference) {
      var typ = genericTypeParameterReference.ResolvedType;
      if (typ == Dummy.GenericTypeParameter) {
        this.EmitError(genericTypeParameterReference, ErrorCode.GenericTypeParameterResolution);
      } else if ((typ.Name != genericTypeParameterReference.Name && !genericTypeParameterReference.Name.Value.StartsWith("!")) || typ.Index != genericTypeParameterReference.Index) {
        this.EmitError(genericTypeParameterReference, ErrorCode.GenericTypeParameterResolution);
      }
      return base.Visit(genericTypeParameterReference);
    }
    public override FieldReference Mutate(FieldReference fieldReference) {
      var field = fieldReference.ResolvedField;
      if (field == Dummy.Field) {
        this.EmitError(fieldReference, ErrorCode.FieldResolution);
      }
      return base.Mutate(fieldReference);
    }
    public override Microsoft.Cci.MutableCodeModel.MethodReference Mutate(Microsoft.Cci.MutableCodeModel.MethodReference methodReference) {
      var method = methodReference.ResolvedMethod;
      if (method == Dummy.Method) {
        this.EmitError(methodReference, ErrorCode.MethodResolution);
      }
      return base.Mutate(methodReference);
    }

    public override INamespaceTypeDefinition Visit(INamespaceTypeDefinition namespaceTypeDefinition) {
      return base.Visit(namespaceTypeDefinition);
    }
    public override MethodDefinition Mutate(MethodDefinition methodDefinition) {
      if (methodDefinition.ContainingTypeDefinition != this.GetCurrentType()) {
        this.EmitError(methodDefinition, ErrorCode.ContainingTypeDefinitionMismatch);
      }
      return base.Mutate(methodDefinition);
    }

    public override IExpression Visit(AnonymousDelegate anonymousDelegate) {
      return base.Visit(anonymousDelegate);
    }
  }