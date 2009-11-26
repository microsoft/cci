//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Cci.MutableCodeModel;
using Microsoft.Cci.Contracts;

namespace Microsoft.Cci.MutableCodeModel {

  /// <summary>
  /// A closure created by the compiler  for an iterator
  /// </summary>
  internal class IteratorClosure {
    public NestedTypeDefinition ClosureDefinition;

    public ITypeReference ClosureDefinitionReference {
      get {
        ITypeReference result = null;
        // GetFullyInstantiatedSpecializedTypeReference not yet working on ClosureDefinition, has 
        // to get the specialized version from its containing type.
        if (closureDefinitionReference == null) {
          var containingTypeRef = GetFullyInstantiatedSpecializedTypeReference(ClosureDefinition.ContainingTypeDefinition);
          foreach (var t in containingTypeRef.ResolvedType.NestedTypes) {
            if (t.Name == ClosureDefinition.Name) {
              result = t; break;
            }
          }
          if (result == null) {
            GenericTypeInstance genericInstance = containingTypeRef.ResolvedType as GenericTypeInstance;
            if (genericInstance != null) {
              result = (ITypeReference)genericInstance.SpecializeMember(ClosureDefinition, this.ClosureDefinition.InternFactory);
            } else {
              SpecializedNestedTypeDefinition specializedNestedType = containingTypeRef.ResolvedType as SpecializedNestedTypeDefinition;
              if (specializedNestedType != null) {
                result = (ITypeReference)specializedNestedType.SpecializeMember(ClosureDefinition, this.ClosureDefinition.InternFactory);
              }
            }
          }
          if (result == null) {
            result = ClosureDefinition;
          }
          if (ClosureDefinition.IsGeneric) closureDefinitionReference = result.ResolvedType.InstanceType;
          else closureDefinitionReference = result;
        }
        return closureDefinitionReference;
      }
    }
    ITypeReference/*?*/ closureDefinitionReference = null;

    bool IsTypeGeneric(ITypeDefinition typeDef) {
      if (typeDef.IsGeneric) return true;
      INestedTypeDefinition nestedType = typeDef as INestedTypeDefinition;
      if (nestedType != null) {
        if (IsTypeGeneric(nestedType.ContainingTypeDefinition)) return true;
      }
      return false;
    }

    ITypeReference GetFullyInstantiatedSpecializedTypeReference(ITypeDefinition typeDefinition) {
      if (typeDefinition.IsGeneric) return typeDefinition.InstanceType;
      INestedTypeDefinition nestedType = typeDefinition as INestedTypeDefinition;
      if (nestedType != null) {
        ITypeReference containingTypeReference = GetFullyInstantiatedSpecializedTypeReference(nestedType.ContainingTypeDefinition);
        foreach (var t in containingTypeReference.ResolvedType.NestedTypes) {
          if (t.Name == nestedType.Name && t.GenericParameterCount == nestedType.GenericParameterCount) {
            return t;
          }
        }
        //GenericTypeInstance genericTypeInstance = containingTypeReference as GenericTypeInstance;
        //if (genericTypeInstance != null) {
        //  //var specialiedTypeDef = new SpecializedNestedTypeDefinition(nestedType, nestedType, containingTypeReference.ResolvedType, genericTypeInstance, this.ClosureDefinition.InternFactory);
        //  var specializedTypeDef = genericTypeInstance.SpecializeMember(nestedType, this.ClosureDefinition.InternFactory);
        //  return (ITypeReference)specializedTypeDef; // cannot be a generic type, no instantiation is needed. 
        //} else {
        //  SpecializedNestedTypeDefinition specializedNestedType = containingTypeReference.ResolvedType as SpecializedNestedTypeDefinition;
        //  if (specializedNestedType != null) {
        //    var specializedTypeDef = specializedNestedType.SpecializeMember(nestedType, this.ClosureDefinition.InternFactory);
        //    return (ITypeReference)specializedTypeDef;
        //  }
        //}
      }
      return typeDefinition;
    }

    Dictionary<IFieldDefinition, IFieldReference> fieldReferences = new Dictionary<IFieldDefinition, IFieldReference>();

    public IFieldReference GetFieldReference(IFieldDefinition fieldDef) {
      if (!fieldReferences.ContainsKey(fieldDef)) {
        IFieldReference fieldReference = null;
        ITypeReference typeReference = ClosureDefinitionReference;
        foreach (var f in typeReference.ResolvedType.Fields) {
          if (f.Name == fieldDef.Name) { fieldReference = f; break; }
        }
        if (fieldReference == null) {
          GenericTypeInstance genericInstance = typeReference as GenericTypeInstance;
          if (genericInstance != null) {
            fieldReference = (IFieldReference)genericInstance.SpecializeMember(fieldDef, this.ClosureDefinition.InternFactory);
          } else {
            SpecializedNestedTypeDefinition specializedNestedType = typeReference as SpecializedNestedTypeDefinition;
            if (specializedNestedType != null)
              fieldReference = (IFieldReference)specializedNestedType.SpecializeMember(fieldDef, this.ClosureDefinition.InternFactory);
          }
        }
        if (fieldReference == null) {
          fieldReference = fieldDef;
        }
        //fieldReference = new SpecializedFieldDefinition(fieldDef, fieldDef, ClosureDefinitionReference.ResolvedType, (GenericTypeInstance)ClosureDefinitionReference);
        fieldReferences[fieldDef] = fieldReference;
      }
      return fieldReferences[fieldDef];
    }

    private IFieldDefinition currentField;
    public IFieldDefinition CurrentField {
      get { return currentField; }
      set {
        currentField = value;
        this.ClosureDefinition.Fields.Add(value);
        currentFieldReference = GetFieldReference(value);
      }
    }
    public IFieldReference CurrentFieldReference {
      get {
        if (currentFieldReference == null) {
          currentFieldReference = GetFieldReference(currentField);
        }
        return currentFieldReference;
      }
    }
    IFieldReference currentFieldReference;

    private IFieldDefinition stateField;

    public IFieldReference StateFieldReference {
      get {
        if (stateFieldReference == null) {
          stateFieldReference = GetFieldReference(stateField);
        }
        return stateFieldReference;
      }
    }
    IFieldReference stateFieldReference;

    public IFieldDefinition StateField {
      get { return stateField; }
      set {
        stateField = value;
        this.ClosureDefinition.Fields.Add(value);
      }
    }
    private IFieldDefinition thisField = null;

    public IFieldDefinition ThisField {
      get { return thisField; }
      set { thisField = value; this.ClosureDefinition.Fields.Add(value); }
    }

    public IFieldReference ThisFieldReference {
      get {
        if (thisFieldReference == null) {
          thisFieldReference = GetFieldReference(thisField);
        }
        return thisFieldReference;
      }
    }
    IFieldReference thisFieldReference;

    private IFieldDefinition initialThreadId;

    public IFieldDefinition InitialThreadId {
      get { return initialThreadId; }
      set { initialThreadId = value; this.ClosureDefinition.Fields.Add(value); }
    }

    public IFieldReference InitThreadIdFieldReference {
      get {
        if (threadIdFieldReference == null) {
          threadIdFieldReference = GetFieldReference(initialThreadId);
        }
        return threadIdFieldReference;
      }
    }
    IFieldReference threadIdFieldReference;

    private IMethodDefinition constructor;

    public IMethodDefinition Constructor {
      get { return constructor; }
      set {
        constructor = value;
        this.ClosureDefinition.Methods.Add(value);
      }
    }

    public IMethodReference ConstructorReference {
      get {
        if (constructorReference == null) {
          constructorReference = GetSpecializedMethodReference(constructor);
        }
        return constructorReference;
      }
    }
    IMethodReference constructorReference;

    private IMethodDefinition moveNext;

    public IMethodDefinition MoveNext {
      get { return moveNext; }
      set { moveNext = value; this.ClosureDefinition.Methods.Add(value); }
    }
    private IMethodDefinition genericGetEnumerator;

    public IMethodReference MoveNextReference {
      get {
        if (moveNextReference == null) {

          moveNextReference = GetSpecializedMethodReference(moveNext);

        }
        return moveNextReference;
      }
    }
    IMethodReference moveNextReference;

    IMethodReference GetSpecializedMethodReference(IMethodDefinition method) {
      IMethodReference methodReference = null;
      ITypeReference typeReference = ClosureDefinitionReference;
      foreach (var f in typeReference.ResolvedType.Methods) {
        if (f.Name == method.Name) { methodReference = f; break; }
      }
      if (methodReference == null) {
        GenericTypeInstance genericInstance = typeReference as GenericTypeInstance;
        if (genericInstance != null) {
          methodReference = (IMethodReference)genericInstance.SpecializeMember(method, this.ClosureDefinition.InternFactory);
        } else {
          SpecializedNestedTypeDefinition specializedNestedType = typeReference as SpecializedNestedTypeDefinition;
          if (specializedNestedType != null)
            methodReference = (IMethodReference)specializedNestedType.SpecializeMember(method, this.ClosureDefinition.InternFactory);
        }
      }
      // methodReference = new SpecializedMethodDefinition(method, method, ClosureDefinitionReference.ResolvedType, (GenericTypeInstance)closureDefinitionReference);
      return methodReference;
    }


    public IMethodDefinition GenericGetEnumerator {
      get { return genericGetEnumerator; }
      set { genericGetEnumerator = value; this.ClosureDefinition.Methods.Add(value); }
    }
    private IMethodDefinition genericGetCurrent;

    public IMethodReference GenericGetEnumeratorReference {
      get {
        if (genericGetEnumeratorReference == null) {
          genericGetEnumeratorReference = GetSpecializedMethodReference(genericGetEnumerator);
        }
        return genericGetEnumeratorReference;
      }
    }
    IMethodReference genericGetEnumeratorReference;

    public IMethodDefinition GenericGetCurrent {
      get { return genericGetCurrent; }
      set { genericGetCurrent = value; this.ClosureDefinition.Methods.Add(value); }
    }

    public IMethodReference GenericGetCurrentReference {
      get {
        if (genericGetCurrentReference == null) {
          genericGetCurrentReference = GetSpecializedMethodReference(genericGetCurrent);
        }
        return genericGetCurrentReference;
      }
    }
    IMethodReference genericGetCurrentReference;

    private IMethodDefinition dispose;

    public IMethodDefinition Dispose {
      get { return dispose; }
      set { dispose = value; this.ClosureDefinition.Methods.Add(value); }
    }

    public IMethodReference DisposeReference {
      get {
        if (disposeReference == null) {
          disposeReference = GetSpecializedMethodReference(Dispose);
        }
        return disposeReference;
      }
    }
    IMethodReference disposeReference;

    private IMethodDefinition reset;

    public IMethodDefinition Reset {
      get { return reset; }
      set { reset = value; this.ClosureDefinition.Methods.Add(value); }
    }

    public IMethodReference ResetReference {
      get {
        if (resetReference == null) {
          resetReference = GetSpecializedMethodReference(Reset);
        }
        return resetReference;
      }
    }
    IMethodReference resetReference;

    private IMethodDefinition nonGenericGetCurrent;

    public IMethodDefinition NonGenericGetCurrent {
      get { return nonGenericGetCurrent; }
      set { nonGenericGetCurrent = value; this.ClosureDefinition.Methods.Add(value); }
    }
    private IMethodDefinition nonGenericGetEnumerator;

    public IMethodDefinition NonGenericGetEnumerator {
      get { return nonGenericGetEnumerator; }
      set { nonGenericGetEnumerator = value; this.ClosureDefinition.Methods.Add(value); }
    }

    public IteratorClosure() {
      currentField = stateField = thisField = initialThreadId = Dummy.Field;
      constructor = moveNext = genericGetCurrent = genericGetEnumerator = dispose = reset = nonGenericGetCurrent = nonGenericGetEnumerator = Dummy.Method;
    }

    public void AddField(IFieldDefinition field) {
      ClosureDefinition.Fields.Add(field);
    }

    private ITypeReference nonGenericIEnumeratorInterface;

    public ITypeReference NonGenericIEnumeratorInterface {
      get { return nonGenericIEnumeratorInterface; }
      set {
        nonGenericIEnumeratorInterface = value;
        if (!this.ClosureDefinition.Interfaces.Contains(value)) {
          this.ClosureDefinition.Interfaces.Add(value);
        }
      }
    }

    private ITypeReference genericIEnumeratorInterface;

    public ITypeReference GenericIEnumeratorInterface {
      get { return genericIEnumeratorInterface; }
      set {
        genericIEnumeratorInterface = value;
        if (!this.ClosureDefinition.Interfaces.Contains(value))
          this.ClosureDefinition.Interfaces.Add(value);
      }
    }
    private ITypeReference nonGenericIEnumerableInterface;

    public ITypeReference NonGenericIEnumerableInterface {
      get { return nonGenericIEnumerableInterface; }
      set {
        nonGenericIEnumerableInterface = value;
        if (!this.ClosureDefinition.Interfaces.Contains(value))
          this.ClosureDefinition.Interfaces.Add(value);
      }
    }
    private ITypeReference genericIEnumerableInterface;

    public ITypeReference GenericIEnumerableInterface {
      get { return genericIEnumerableInterface; }
      set {
        genericIEnumerableInterface = value;
        if (!this.ClosureDefinition.Interfaces.Contains(value))
          this.ClosureDefinition.Interfaces.Add(value);
      }
    }
    private ITypeReference disposableInterface;

    public ITypeReference DisposableInterface {
      get { return disposableInterface; }
      set {
        disposableInterface = value;
        if (!this.ClosureDefinition.Interfaces.Contains(value))
          this.ClosureDefinition.Interfaces.Add(value);
      }
    }

    ITypeReference elementType;

    public ITypeReference ElementType {
      get { return elementType; }
      set { elementType = value; }
    }

  }

  internal sealed class BoundField : Expression, IBoundExpression {

    public BoundField(FieldDefinition field, ITypeReference type) {
      this.field = field;
      this.Type = type;
    }

    public byte Alignment {
      get { return 0; }
    }

    public object Definition {
      get { return this.Field; }
    }

    public FieldDefinition Field {
      get { return this.field; }
    }
    FieldDefinition field;

    public IExpression/*?*/ Instance {
      get { return null; }
    }

    public bool IsUnaligned {
      get { return false; }
    }

    public bool IsVolatile {
      get { return false; }
    }

    public override void Dispatch(ICodeVisitor visitor) {
      throw new NotImplementedException();
    }
  }


}