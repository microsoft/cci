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
public class Checker : CodeTraverser {
  /// <summary>
  /// A checker that checks the consistency of a code model. 
  /// </summary>
  /// <param name="host"></param>
  public Checker(IMetadataHost host) {
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

  public override void TraverseChildren(IGenericMethodInstanceReference genericMethodInstanceReference) {
    var methodDef = genericMethodInstanceReference.ResolvedMethod;
    if (methodDef == Dummy.Method) {
      this.EmitError(genericMethodInstanceReference, ErrorCode.GenericMethodInstanceResolution);
    }
    base.TraverseChildren(genericMethodInstanceReference);
  }

  public override void TraverseChildren(IMethodReference methodReference) {
    var methodDef = methodReference.ResolvedMethod;
    if (methodDef == Dummy.Method) {
      this.EmitError(methodReference, ErrorCode.MethodResolution);
    }
    base.TraverseChildren(methodReference);
  }

  public override void TraverseChildren(IFunctionPointerTypeReference functionPointerTypeReference) {
    var typ = functionPointerTypeReference.ResolvedType;
    if (typ == Dummy.Type) {
      this.EmitError(functionPointerTypeReference, ErrorCode.FunctionPointerTypeResolution);
    }
    base.TraverseChildren(functionPointerTypeReference);
  }

  public override void TraverseChildren(IPointerTypeReference pointerTypeReference) {
    var typ = pointerTypeReference.ResolvedType;
    if (typ == Dummy.Type) {
      this.EmitError(pointerTypeReference, ErrorCode.PointerTypeResolution);
    }
    base.TraverseChildren(pointerTypeReference);
  }

  public override void TraverseChildren(INestedTypeReference nestedTypeReference) {
    var typ = nestedTypeReference.ResolvedType;
    if (typ == Dummy.Type) {
      this.EmitError(nestedTypeReference, ErrorCode.NestedTypeResolution);
    }
    base.TraverseChildren(nestedTypeReference);
  }

  public override void TraverseChildren(INamespaceTypeReference namespaceTypeReference) {
    var typ = namespaceTypeReference.ResolvedType;
    if (typ == Dummy.Type) {
      this.EmitError(namespaceTypeReference, ErrorCode.NamespaceTypeResolution);
    }
    base.TraverseChildren(namespaceTypeReference);
  }

  public override void TraverseChildren(IArrayTypeReference arrayTypeReference) {
    var typ = arrayTypeReference.ResolvedType;
    if (typ == Dummy.Type) {
      this.EmitError(arrayTypeReference, ErrorCode.NamespaceTypeResolution);
    }
    base.TraverseChildren(arrayTypeReference);
  }

  public override void TraverseChildren(IGenericMethodParameterReference genericMethodParameterReference) {
    var typ = genericMethodParameterReference.ResolvedType;
    if (typ == Dummy.Type) {
      this.EmitError(genericMethodParameterReference, ErrorCode.GenericMethodParameterResolution);
    } else if ((typ.Name != genericMethodParameterReference.Name && !genericMethodParameterReference.Name.Value.StartsWith("!")) || typ.Index != genericMethodParameterReference.Index) {
      this.EmitError(genericMethodParameterReference, ErrorCode.GenericMethodParameterResolution);
    }
    base.TraverseChildren(genericMethodParameterReference);
  }

  public override void TraverseChildren(IGenericTypeInstanceReference genericTypeInstanceReference) {
    var typ = genericTypeInstanceReference.ResolvedType;
    if (!(typ is IGenericTypeInstance) || (((IGenericTypeInstance)typ).GenericType is Dummy)) {
      this.EmitError(genericTypeInstanceReference, ErrorCode.GenericTypeInstanceResolution);
    }
    base.TraverseChildren(genericTypeInstanceReference);
  }

  public override void TraverseChildren(IGenericTypeParameterReference genericTypeParameterReference) {
    var typ = genericTypeParameterReference.ResolvedType;
    if (typ == Dummy.GenericTypeParameter) {
      this.EmitError(genericTypeParameterReference, ErrorCode.GenericTypeParameterResolution);
    } else if ((typ.Name != genericTypeParameterReference.Name && !genericTypeParameterReference.Name.Value.StartsWith("!")) || typ.Index != genericTypeParameterReference.Index) {
      this.EmitError(genericTypeParameterReference, ErrorCode.GenericTypeParameterResolution);
    }
    base.TraverseChildren(genericTypeParameterReference);
  }

}