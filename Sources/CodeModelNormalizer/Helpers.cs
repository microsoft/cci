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

namespace Microsoft.Cci {

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
      set { currentField = value;
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
      set { stateField = value;
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
      set { constructor = value;
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
      set { nonGenericIEnumeratorInterface = value;
      if (!this.ClosureDefinition.Interfaces.Contains(value)) {
        this.ClosureDefinition.Interfaces.Add(value);
      }
      }
    }

    private ITypeReference genericIEnumeratorInterface;

    public ITypeReference GenericIEnumeratorInterface {
      get { return genericIEnumeratorInterface; }
      set { genericIEnumeratorInterface = value;
      if (!this.ClosureDefinition.Interfaces.Contains(value))
        this.ClosureDefinition.Interfaces.Add(value);
      }
    }
    private ITypeReference nonGenericIEnumerableInterface;

    public ITypeReference NonGenericIEnumerableInterface {
      get { return nonGenericIEnumerableInterface; }
      set { nonGenericIEnumerableInterface = value;
      if (!this.ClosureDefinition.Interfaces.Contains(value))
        this.ClosureDefinition.Interfaces.Add(value);
      }
    }
    private ITypeReference genericIEnumerableInterface;

    public ITypeReference GenericIEnumerableInterface {
      get { return genericIEnumerableInterface; }
      set { genericIEnumerableInterface = value;
      if (!this.ClosureDefinition.Interfaces.Contains(value))
        this.ClosureDefinition.Interfaces.Add(value);
      }
    }
    private ITypeReference disposableInterface;

    public ITypeReference DisposableInterface {
      get { return disposableInterface; }
      set { disposableInterface = value;
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
  }

  /// <summary>
  /// An expression results in a value of some type.
  /// </summary>
  internal abstract class Expression : IExpression {

    protected Expression() {
      this.locations = new List<ILocation>();
      this.type = Dummy.TypeReference;
    }

    //protected Expression(IExpression expression) {
    //  this.locations = new List<ILocation>(expression.Locations);
    //  this.type = expression.Type;
    //}

    /// <summary>
    /// Calls the visitor.Visit(T) method where T is the most derived object model node interface type implemented by the concrete type
    /// of the object implementing IDoubleDispatcher. The dispatch method does not invoke Dispatch on any child objects. If child traversal
    /// is desired, the implementations of the Visit methods should do the subsequent dispatching.
    /// </summary>
    public void Dispatch(ICodeVisitor visitor) {
    }

    /// <summary>
    /// Checks the expression for errors and returns true if any were found.
    /// </summary>
    public bool HasErrors() {
      return false;
    }

    /// <summary>
    /// True if the expression has no observable side effects.
    /// </summary>
    /// <value></value>
    public bool IsPure {
      get { return false; }
    }

    //public List<ILocation> Locations {
    //  get { return this.locations; }
    //  set { this.locations = value; }
    //}
    List<ILocation> locations;

    /// <summary>
    /// The type of value the expression will evaluate to, as determined at compile time.
    /// </summary>
    public ITypeReference Type {
      get { return this.type; }
      set { this.type = value; }
    }
    ITypeReference type;

    #region IExpression Members

    IEnumerable<ILocation> IObjectWithLocations.Locations {
      get { return this.locations.AsReadOnly(); }
    }

    #endregion
  }

  internal class PreNormalizedCodeModelToILConverter : CodeModelToILConverter {

    public PreNormalizedCodeModelToILConverter(IMetadataHost host, ISourceLocationProvider/*?*/ sourceLocationProvider, IContractProvider/*?*/ contractProvider)
      : base(host, sourceLocationProvider, contractProvider) {
      this.host = host;
    }

    /// <summary>
    /// Traverses the given block of statements in the context of the given method to produce a list of
    /// IL operations, exception information blocks (the locations of handlers, filters and finallies) and any private helper
    /// types (for example closure classes) that represent the semantics of the given block of statements.
    /// The results of the traversal can be retrieved via the GetOperations, GetOperationExceptionInformation
    /// and GetPrivateHelperTypes methods.
    /// </summary>
    /// <param name="method">A method that provides the context for a block of statments that are to be converted to IL.</param>
    /// <param name="body">A block of statements that are to be converted to IL.</param>
    public override void ConvertToIL(IMethodDefinition method, IBlockStatement body) {
      MethodBodyNormalizer normalizer = new MethodBodyNormalizer(this.host, null, ProvideSourceToILConverter,
        this.sourceLocationProvider, (ContractProvider)this.contractProvider);
      ISourceMethodBody normalizedBody = normalizer.GetNormalizedSourceMethodBodyFor(method, body);
      this.privateHelperTypes = normalizedBody.PrivateHelperTypes;
      base.Visit(normalizedBody);
    }

    /// <summary>
    /// Returns zero or more types that are used to keep track of information needed to implement
    /// the statements that have been converted to IL by this converter. For example, any closure classes
    /// needed to compile anonymous delegate expressions (lambdas) will be returned by this method.
    /// </summary>
    public override IEnumerable<ITypeDefinition> GetPrivateHelperTypes() {
      return this.privateHelperTypes;
    }

    IEnumerable<ITypeDefinition> privateHelperTypes = IteratorHelper.GetEmptyEnumerable<ITypeDefinition>();

    static ISourceToILConverter ProvideSourceToILConverter(IMetadataHost host, ISourceLocationProvider/*?*/ sourceLocationProvider, IContractProvider/*?*/ contractProvider) {
      return new CodeModelToILConverter(host, sourceLocationProvider, contractProvider);
    }

  }
}