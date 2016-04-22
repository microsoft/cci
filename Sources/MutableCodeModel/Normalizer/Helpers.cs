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
using System.Diagnostics;
using Microsoft.Cci.MutableCodeModel;
using Microsoft.Cci.Contracts;

namespace Microsoft.Cci.MutableCodeModel {

  /// <summary>
  /// Information needed for and during the creation of the closure class for an iterator method. Such information includes:
  /// 1) the closure class,
  /// 2) its members, and
  /// 3) references to the generic instances of the class and its members, as used by methods in the closure class.
  /// </summary>
  internal class IteratorClosureInformation {
    /// <summary>
    /// Information needed for and during the creation of the closure class for an iterator method. Such information includes:
    /// 1) the closure class,
    /// 2) its members, and
    /// 3) references to the generic instances of the class and its members
    /// </summary>
    internal IteratorClosureInformation(IMetadataHost host) {
      this.host = host;
    }

    private IMetadataHost host;

    /// <summary>
    /// Closure class definition.
    /// </summary>
    internal NestedTypeDefinition ClosureDefinition;

    /// <summary>
    /// The fully instantiated/specialized version of the closure class, where all the instantiations use the type parameters themselves. 
    /// </summary>
    /// <remarks>
    /// The specialized version of the closure class does not exist yet. We have to specialize it ourselves. 
    /// </remarks>
    internal ITypeReference ClosureDefinitionReference {
      get {
        if (this.closureDefinitionReference == null) {
          this.closureDefinitionReference = NamedTypeDefinition.SelfInstance(this.ClosureDefinition, this.host.InternFactory);
        }
        return closureDefinitionReference;
      }
    }
    ITypeReference/*?*/ closureDefinitionReference = null;

    /// <summary>
    /// Given a field definition in the closure class, get its reference as will be used by the methods in the closure class. 
    /// </summary>
    internal IFieldReference GetReferenceOfFieldUsedByPeers(IFieldDefinition fieldDef) {

      IFieldReference fieldReference = null;
      ITypeReference typeReference = this.ClosureDefinitionReference;
      ISpecializedNestedTypeReference nestedTypeRef = typeReference as ISpecializedNestedTypeReference;
      IGenericTypeInstanceReference genericTypeInstanceRef = typeReference as IGenericTypeInstanceReference;
      if (nestedTypeRef != null || genericTypeInstanceRef != null) {
        fieldReference = new SpecializedFieldReference() {
          ContainingType = typeReference,
          InternFactory = this.host.InternFactory,
          Name = fieldDef.Name,
          UnspecializedVersion = fieldDef,
          Type = fieldDef.Type
        };
      } else fieldReference = fieldDef;
      return fieldReference;
    }

    /// <summary>
    /// The "current" field of the iterator closure class. Should not be set more than once. The setter also add the member to the member list of the closure class.
    /// </summary>
    internal IFieldDefinition CurrentField {
      get { return this.currentField; }
      set {
        if (this.currentField == null) {
          this.currentField = value;
          this.ClosureDefinition.Fields.Add(value);
        } else Debug.Assert(false);
      }
    }
    private IFieldDefinition currentField;

    /// <summary>
    /// The reference to this.current as used by methods in the closure class.
    /// </summary>
    internal IFieldReference CurrentFieldReference {
      get {
        if (this.currentFieldReference == null) {
          this.currentFieldReference = this.GetReferenceOfFieldUsedByPeers(this.currentField);
        }
        return this.currentFieldReference;
      }
    }
    private IFieldReference currentFieldReference;

    /// <summary>
    /// The "state" field of the iterator closure. Should not be set more than once. The setter also add the member to the member list of the closure class.
    /// </summary>
    internal IFieldDefinition StateField {
      get { return this.stateField; }
      set {
        if (this.stateField == null) {
          this.stateField = value;
          this.ClosureDefinition.Fields.Add(value);
        } else Debug.Assert(false);
      }
    }
    private IFieldDefinition stateField;

    /// <summary>
    /// The reference to this.state as used by methods in the closure class.
    /// </summary>
    internal IFieldReference StateFieldReference {
      get {
        if (this.stateFieldReference == null) {
          this.stateFieldReference = this.GetReferenceOfFieldUsedByPeers(this.stateField);
        }
        return this.stateFieldReference;
      }
    }
    private IFieldReference stateFieldReference;

    /// <summary>
    /// The "this" field of the iterator closure, that is, the one that captures the this parameter
    /// of the orginal iterator method, if any. Should not be set more than once. The setter also add the member to the member list of the closure class.
    /// </summary>
    internal IFieldDefinition ThisField {
      get { return this.thisField; }
      set {
        if (this.thisField == null) {
          this.thisField = value;
          this.ClosureDefinition.Fields.Add(value);
        } else Debug.Assert(false);
      }
    }
    private IFieldDefinition thisField = null;

    /// <summary>
    /// The reference to the this field of the iterator closure, as used by the methods in the closure class.
    /// </summary>
    internal IFieldReference ThisFieldReference {
      get {
        if (this.thisFieldReference == null) {
          this.thisFieldReference = this.GetReferenceOfFieldUsedByPeers(this.thisField);
        }
        return this.thisFieldReference;
      }
    }
    IFieldReference thisFieldReference;

    /// <summary>
    /// The "l_initialThreadId" field of the closure. Should not be set more than once. The setter also add the member to the member list of the closure class.
    /// </summary>
    internal IFieldDefinition InitialThreadId {
      get { return this.initialThreadId; }
      set {
        if (this.initialThreadId == null) {
          this.initialThreadId = value;
          this.ClosureDefinition.Fields.Add(value);
        } else Debug.Assert(false);
      }
    }
    private IFieldDefinition initialThreadId;

    /// <summary>
    /// The reference to the l_initialThreadId as used by methods in the closure class.
    /// </summary>
    internal IFieldReference InitThreadIdFieldReference {
      get {
        if (this.threadIdFieldReference == null) {
          this.threadIdFieldReference = this.GetReferenceOfFieldUsedByPeers(this.initialThreadId);
        }
        return this.threadIdFieldReference;
      }
    }
    private IFieldReference threadIdFieldReference;

    /// <summary>
    /// Constructor of the iterator closure. Should not be set more than once. The setter also add the member to the member list of the closure class.
    /// </summary>
    internal IMethodDefinition Constructor {
      get { return this.constructor; }
      set {
        if (this.constructor == null) {
          this.constructor = value;
          this.ClosureDefinition.Methods.Add(value);
        } else Debug.Assert(false);
      }
    }
    private IMethodDefinition constructor;

    /// <summary>
    /// The reference to the constructor as used by the methods (for example, GetEnumerator) in the closure class. 
    /// </summary>
    internal IMethodReference ConstructorReference {
      get {
        if (this.constructorReference == null) {
          this.constructorReference = this.GetReferenceOfMethodUsedByPeers(this.constructor);
        }
        return this.constructorReference;
      }
    }
    private IMethodReference constructorReference;

    /// <summary>
    /// MoveNext method of the iterator class. Should not be set more than once. The setter also add the member to the member list of the closure class.
    /// 
    /// MoveNext is not used by other methods in the closure class.
    /// </summary>
    internal IMethodDefinition MoveNext {
      get { return this.moveNext; }
      set {
        if (this.moveNext == null) {
          this.moveNext = value;
          this.ClosureDefinition.Methods.Add(value);
        } else Debug.Assert(false);
      }
    }
    private IMethodDefinition moveNext;

    /// <summary>
    /// Get the reference of a method in the closure class as used by other methods in the same class. 
    /// </summary>
    private IMethodReference GetReferenceOfMethodUsedByPeers(IMethodDefinition method) {
      IMethodReference methodReference = null;
      ITypeReference typeReference = this.ClosureDefinitionReference;
      ISpecializedNestedTypeReference specializedNestedTypeRef = typeReference as ISpecializedNestedTypeReference;
      IGenericTypeInstanceReference genericInstanceRef = typeReference as IGenericTypeInstanceReference;
      if (specializedNestedTypeRef != null || genericInstanceRef != null) {
        methodReference = new SpecializedMethodReference() {
          ContainingType = typeReference,
          GenericParameterCount = method.GenericParameterCount,
          InternFactory = this.host.InternFactory,
          UnspecializedVersion = method,
          Type = method.Type,
          Name = method.Name,
          CallingConvention = method.CallingConvention,
          Parameters = method.ParameterCount == 0 ? null : new List<IParameterTypeInformation>(((IMethodReference)method).Parameters),
          ExtraParameters = null,
          ReturnValueIsByRef = method.ReturnValueIsByRef,
          ReturnValueIsModified = method.ReturnValueIsModified,
          Attributes = null,
        };
      } else methodReference = method;
      return methodReference;
    }

    /// <summary>
    /// The generic version of the GetEnumerator method. Should not be set more than once. The setter also add the member to the member list of the closure class.
    /// </summary>
    internal IMethodDefinition GenericGetEnumerator {
      get { return this.genericGetEnumerator; }
      set {
        if (this.genericGetEnumerator == null) {
          genericGetEnumerator = value;
          this.ClosureDefinition.Methods.Add(value);
        } else Debug.Assert(false);
      }
    }
    private IMethodDefinition genericGetEnumerator;

    /// <summary>
    /// The reference to the generic version of GetEnumerator as used by other methods (for example, the non-generic
    /// version of GetEnumerator) in the same closure class. 
    /// </summary>
    internal IMethodReference GenericGetEnumeratorReference {
      get {
        if (this.genericGetEnumeratorReference == null) {
          this.genericGetEnumeratorReference = this.GetReferenceOfMethodUsedByPeers(this.genericGetEnumerator);
        }
        return this.genericGetEnumeratorReference;
      }
    }
    private IMethodReference genericGetEnumeratorReference;

    /// <summary>
    /// The generic version of the get_Current method. Should not be set more than once. The setter also add the member to the member list of the closure class.
    /// </summary>
    internal IMethodDefinition GenericGetCurrent {
      get { return this.genericGetCurrent; }
      set {
        if (this.genericGetCurrent == null) {
          this.genericGetCurrent = value;
          this.ClosureDefinition.Methods.Add(value);
        } else Debug.Assert(false);
      }
    }
    private IMethodDefinition genericGetCurrent;

    /// <summary>
    /// The reference to the get_Current method, as used by other methods (for example, the non-generic version of get_Current)
    /// in the same closure class. 
    /// </summary>
    internal IMethodReference GenericGetCurrentReference {
      get {
        if (this.genericGetCurrentReference == null) {
          this.genericGetCurrentReference = this.GetReferenceOfMethodUsedByPeers(this.genericGetCurrent);
        }
        return this.genericGetCurrentReference;
      }
    }
    private IMethodReference genericGetCurrentReference;

    /// <summary>
    /// The dispose Method of the closure class. Should not be set more than once. The setter also add the member to the member list of the closure class.
    /// </summary>
    internal IMethodDefinition DisposeMethod {
      get { return this.disposeMethod; }
      set {
        if (this.disposeMethod == null) {
          this.disposeMethod = value;
          this.ClosureDefinition.Methods.Add(value);
        } else Debug.Assert(false);
      }
    }
    private IMethodDefinition disposeMethod;

    /// <summary>
    /// The reset method of the closure class. Should not be set more than once. The setter also add the member to the member list of the closure class.
    /// </summary>
    internal IMethodDefinition Reset {
      get { return this.reset; }
      set {
        if (this.reset == null) {
          this.reset = value;
          this.ClosureDefinition.Methods.Add(value);
        } else Debug.Assert(false);
      }
    }
    private IMethodDefinition reset;

    /// <summary>
    /// The non-generic get_Current method of the closure class. Should not be set more than once. The setter also add the member to the member list of the closure class.
    /// 
    /// This method is not used by other methods in the closure class. 
    /// </summary>
    internal IMethodDefinition NonGenericGetCurrent {
      get { return this.nonGenericGetCurrent; }
      set {
        if (this.nonGenericGetCurrent == null) {
          this.nonGenericGetCurrent = value; this.ClosureDefinition.Methods.Add(value);
        } else Debug.Assert(false);
      }
    }
    private IMethodDefinition nonGenericGetCurrent;

    /// <summary>
    /// The non-generic GetEnumerator method of the closure class. Should not be set more than once. The setter also add the member to the member list of the closure class.
    /// 
    /// This method is not used by other methods in the closure class. 
    /// </summary>
    internal IMethodDefinition NonGenericGetEnumerator {
      get { return this.nonGenericGetEnumerator; }
      set {
        if (this.nonGenericGetEnumerator == null) {
          this.nonGenericGetEnumerator = value;
          this.ClosureDefinition.Methods.Add(value);
        } else Debug.Assert(false);
      }
    }
    private IMethodDefinition nonGenericGetEnumerator;

    /// <summary>
    /// Add field definition to the clousre class.
    /// </summary>
    /// <param name="field"></param>
    internal void AddField(IFieldDefinition field) {
      ClosureDefinition.Fields.Add(field);
    }

    /// <summary>
    /// The non-generic version of the IEnumerator implemented by the closure class. 
    /// </summary>
    internal ITypeReference NonGenericIEnumeratorInterface {
      get { return nonGenericIEnumeratorInterface; }
      set {
        nonGenericIEnumeratorInterface = value;
        if (!this.ClosureDefinition.Interfaces.Contains(value)) {
          this.ClosureDefinition.Interfaces.Add(value);
        }
      }
    }
    private ITypeReference nonGenericIEnumeratorInterface;

    /// <summary>
    /// The generic version of the IEnumerator interface implemented by the closure class.
    /// </summary>
    internal ITypeReference GenericIEnumeratorInterface {
      get { return genericIEnumeratorInterface; }
      set {
        genericIEnumeratorInterface = value;
        if (!this.ClosureDefinition.Interfaces.Contains(value))
          this.ClosureDefinition.Interfaces.Add(value);
      }
    }
    private ITypeReference genericIEnumeratorInterface;

    /// <summary>
    /// The non-generic version of IEnumerable implemented by the closure class.
    /// </summary>
    internal ITypeReference NonGenericIEnumerableInterface {
      get { return nonGenericIEnumerableInterface; }
      set {
        nonGenericIEnumerableInterface = value;
        if (!this.ClosureDefinition.Interfaces.Contains(value))
          this.ClosureDefinition.Interfaces.Add(value);
      }
    }
    private ITypeReference nonGenericIEnumerableInterface;

    /// <summary>
    /// The generic verision of IEnumerable implemented by the closure class. It is an instance of
    /// IEnumerable[T] with T instantiated to the elementType of the iterator closure. 
    /// </summary>
    internal ITypeReference GenericIEnumerableInterface {
      get { return genericIEnumerableInterface; }
      set {
        genericIEnumerableInterface = value;
        if (!this.ClosureDefinition.Interfaces.Contains(value))
          this.ClosureDefinition.Interfaces.Add(value);
      }
    }
    private ITypeReference genericIEnumerableInterface;

    /// <summary>
    /// The IDisposable interface. 
    /// </summary>
    internal ITypeReference DisposableInterface {
      get { return this.disposableInterface; }
      set {
        this.disposableInterface = value;
        if (!this.ClosureDefinition.Interfaces.Contains(value))
          this.ClosureDefinition.Interfaces.Add(value);
      }
    }
    private ITypeReference disposableInterface;

    /// <summary>
    /// The element type of the IEnumerable implemented by the closure class. If the iterator method's return type is not 
    /// generic, this is System.Object. 
    /// </summary>
    internal ITypeReference ElementType {
      get { return this.elementType; }
      set { this.elementType = value; }
    }
    ITypeReference elementType;

    internal void InitializeInterfaces(ITypeReference elementType, bool isEnumerable) {
      var methodTypeArguments = new List<ITypeReference>();
      methodTypeArguments.Add(elementType);
      ITypeReference genericEnumeratorType = new Immutable.GenericTypeInstanceReference(this.host.PlatformType.SystemCollectionsGenericIEnumerator, methodTypeArguments, this.host.InternFactory);
      ITypeReference genericEnumerableType = new Immutable.GenericTypeInstanceReference(this.host.PlatformType.SystemCollectionsGenericIEnumerable, methodTypeArguments, this.host.InternFactory);
      ITypeReference nongenericEnumeratorType = this.host.PlatformType.SystemCollectionsIEnumerator;
      ITypeReference nongenericEnumerableType = this.host.PlatformType.SystemCollectionsIEnumerable;
      ITypeReference iDisposable = this.PlatformIDisposable;

      if (isEnumerable)
        this.NonGenericIEnumerableInterface = nongenericEnumerableType;
      this.NonGenericIEnumeratorInterface = nongenericEnumeratorType;
      if (isEnumerable)
        this.GenericIEnumerableInterface = genericEnumerableType;
      this.GenericIEnumeratorInterface = genericEnumeratorType;
      this.DisposableInterface = iDisposable;
    }

    /// <summary>
    /// IDisposable interface. 
    /// </summary>
    private ITypeReference PlatformIDisposable {
      get {
        if (this.platformIDisposable == null) {
          this.platformIDisposable = new Immutable.NamespaceTypeReference(this.host, this.host.PlatformType.SystemObject.ContainingUnitNamespace,
            this.host.NameTable.GetNameFor("IDisposable"), 0, false, false, true, PrimitiveTypeCode.Reference);
        }
        return this.platformIDisposable;
      }
    }
    private ITypeReference platformIDisposable = null;
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