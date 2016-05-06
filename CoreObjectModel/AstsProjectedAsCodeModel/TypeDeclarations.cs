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
using System;
using System.Collections.Generic;
using Microsoft.Cci.Contracts;
using System.Diagnostics;
using Microsoft.Cci.Immutable;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci.Ast {

  /// <summary>
  /// 
  /// </summary>
  public class GlobalDeclarationContainerClass : NamespaceClassDeclaration {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="compilationHost"></param>
    public GlobalDeclarationContainerClass(IMetadataHost compilationHost)
      : this(compilationHost, new List<ITypeDeclarationMember>()) {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="compilationHost"></param>
    /// <param name="globalMembers"></param>
    private GlobalDeclarationContainerClass(IMetadataHost compilationHost, List<ITypeDeclarationMember> globalMembers)
      : base(null, Flags.None, new NameDeclaration(compilationHost.NameTable.GetNameFor("__Globals__"), SourceDummy.SourceLocation),
      new List<GenericTypeParameterDeclaration>(0), new List<TypeExpression>(0), globalMembers, SourceDummy.SourceLocation) {
      this.globalMembers = globalMembers;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="containingNamespaceDeclaration"></param>
    /// <param name="template"></param>
    protected GlobalDeclarationContainerClass(NamespaceDeclaration containingNamespaceDeclaration, GlobalDeclarationContainerClass template)
      : this(containingNamespaceDeclaration, template, template.globalMembers)
      //^ ensures this.containingNamespaceDeclaration == containingNamespaceDeclaration;
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="containingNamespaceDeclaration"></param>
    /// <param name="template"></param>
    /// <param name="members"></param>
    protected GlobalDeclarationContainerClass(NamespaceDeclaration containingNamespaceDeclaration, GlobalDeclarationContainerClass template, List<ITypeDeclarationMember> members)
      : base(containingNamespaceDeclaration, template, members)
      //^ ensures this.containingNamespaceDeclaration == containingNamespaceDeclaration;
    {
      this.globalMembers = members;
    }


    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the member or a constituent part of the member.
    /// Do not call this method directly, but evaluate the HasErrors property. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      bool result = base.CheckForErrorsAndReturnTrueIfAnyAreFound();
      foreach (var globalMember in this.GlobalMembers) {
        result |= globalMember.HasErrors;
      }
      return result;
    }

    /// <summary>
    /// Creates a new type definition to correspond to this type declaration.
    /// </summary>
    /// <returns></returns>
    protected internal override NamespaceTypeDefinition CreateType() {
      NamespaceTypeDefinition result = this.Compilation.GlobalsClass;
      result.AddTypeDeclaration(this);
      ITypeContract/*?*/ typeContract = this.Compilation.ContractProvider.GetTypeContractFor(this);
      if (typeContract != null)
        this.Compilation.ContractProvider.AssociateTypeWithContract(result, typeContract);
      return result;
    }

    /// <summary>
    /// A list of type declaration members that correspond to global variables and functions.
    /// </summary>
    public IEnumerable<ITypeDeclarationMember> GlobalMembers { //TODO: 
      get
        //^ ensures result is List<ITypeDeclarationMember>; //The return type is different so that a downcast is required before the members can be modified.
        //TODO: make the post condition valid only while the class has not yet been fully initialized.
      {
        return this.globalMembers;
      }
    }
    List<ITypeDeclarationMember> globalMembers;

    /// <summary>
    /// A scope containing global variables and functions.
    /// </summary>
    public IScope<ITypeDeclarationMember> GlobalScope {
      get {
        if (this.globalScope == null)
          this.globalScope = new GlobalDeclarationScope(this.GlobalMembers);
        return this.globalScope;
      }
    }
    //^ [Once]
    IScope<ITypeDeclarationMember>/*?*/ globalScope;

    //^ [MustOverride]
    /// <summary>
    /// Returns a shallow copy of this instance with the given namespace as the new containing namespace.
    /// </summary>
    /// <param name="targetNamespaceDeclaration">The containing namespace of the result.</param>
    /// <returns></returns>
    public override INamespaceDeclarationMember MakeShallowCopyFor(NamespaceDeclaration targetNamespaceDeclaration)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingNamespaceDeclaration == targetNamespaceDeclaration;
    {
      if (targetNamespaceDeclaration == this.ContainingNamespaceDeclaration) return this;
      return new GlobalDeclarationContainerClass(targetNamespaceDeclaration, this);
    }

    //^ [MustOverride]
    /// <summary>
    /// Returns a shallow copy of this namespace type with the given list of members replacing the template members.
    /// The containing namespace declaration should be set by means of a later call to SetContainingNamespaceDeclaration.
    /// </summary>
    /// <param name="members">The member list of the new type declaration.</param>
    /// <returns></returns>
    protected override NamespaceTypeDeclaration MakeShallowCopy(List<ITypeDeclarationMember> members)
      //^^ ensures result.GetType() == this.GetType();
    {
      return new GlobalDeclarationContainerClass(this.ContainingNamespaceDeclaration, this, members);
    }

  }

  /// <summary>
  /// 
  /// </summary>
  internal class GlobalDeclarationScope : Scope<ITypeDeclarationMember> {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="members"></param>
    internal GlobalDeclarationScope(IEnumerable<ITypeDeclarationMember> members) {
      this.members = members;
    }

    /// <summary>
    /// 
    /// </summary>
    protected override void InitializeIfNecessary() {
      if (this.initialized) return;
      lock (GlobalLock.LockingObject) {
        if (this.initialized) return;
        foreach (ITypeDeclarationMember member in members)
          this.AddMemberToCache(member);
        this.initialized = true;
      }
    }
    bool initialized;

    /// <summary>
    /// The collection of member instances that are members of this scope.
    /// </summary>
    /// <value></value>
    public override IEnumerable<ITypeDeclarationMember> Members {
      get { return this.Members; }
    }
    IEnumerable<ITypeDeclarationMember> members;
  }

  /// <summary>
  /// Corresponds to a source construct that declares a type parameter for a generic method or type.
  /// </summary>
  public abstract class GenericParameterDeclaration : SourceItemWithAttributes, IDeclaration, INamedEntity, IParameterListEntry {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sourceAttributes"></param>
    /// <param name="name"></param>
    /// <param name="index"></param>
    /// <param name="constraints"></param>
    /// <param name="variance"></param>
    /// <param name="mustBeReferenceType"></param>
    /// <param name="mustBeValueType"></param>
    /// <param name="mustHaveDefaultConstructor"></param>
    /// <param name="sourceLocation"></param>
    protected GenericParameterDeclaration(List<SourceCustomAttribute>/*?*/ sourceAttributes, NameDeclaration name, ushort index, List<TypeExpression> constraints,
      TypeParameterVariance variance, bool mustBeReferenceType, bool mustBeValueType, bool mustHaveDefaultConstructor, ISourceLocation sourceLocation)
      : base(sourceLocation)
      //^ requires !mustBeReferenceType || !mustBeValueType;
    {
      this.constraints = constraints;
      this.flags = (int)variance;
      if (mustBeReferenceType) this.flags |= 0x40000000;
      if (mustBeValueType) this.flags |= 0x20000000;
      if (mustHaveDefaultConstructor) this.flags |= 0x10000000;
      this.index = index;
      this.name = name;
      this.sourceAttributes = sourceAttributes;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected GenericParameterDeclaration(BlockStatement containingBlock, GenericParameterDeclaration template)
      : base(template.SourceLocation) {
      this.containingBlock = containingBlock;
      this.constraints = new List<TypeExpression>(template.constraints);
      this.flags = template.flags;
      this.index = template.index;
      this.name = template.name;
      if (template.sourceAttributes != null)
        this.sourceAttributes = new List<SourceCustomAttribute>(template.sourceAttributes);
    }

    /// <summary>
    /// Returns a list of custom attributes that describes this type declaration member.
    /// Typically, these will be derived from this.SourceAttributes. However, some source attributes
    /// might instead be persisted as metadata bits and other custom attributes may be synthesized
    /// from information not provided in the form of source custom attributes.
    /// The list is not trimmed to size, since an override of this method may call the base method
    /// and then add more attributes.
    /// </summary>
    protected override List<ICustomAttribute> GetAttributes() {
      List<ICustomAttribute> result = new List<ICustomAttribute>();
      foreach (var attribute in this.SourceAttributes) {
        if (attribute.HasErrors) continue;
        //TODO: ignore pseudo attributes
        result.Add(new CustomAttribute(attribute));
      }
      return result;
    }

    /// <summary>
    /// Add a constraint to this generic parameter declaration. This is done after construction of this declaration because
    /// it may contain a reference to this declaration. It should not be called after the second phase of construction has
    /// been completed.
    /// </summary>
    /// <param name="constraint">A type expression that resolves to a type that constrains this parameter.</param>
    protected void AddConstraint(TypeExpression constraint) {
      this.constraints.Add(constraint);
    }

    /// <summary>
    /// A list of classes or interfaces. All type arguments matching this parameter must be derived from all of the classes and implement all of the interfaces.
    /// </summary>
    public IEnumerable<TypeExpression> Constraints {
      get {
        for (int i = 0, n = this.constraints.Count; i < n; i++)
          yield return this.constraints[i] = (TypeExpression)this.constraints[i].MakeCopyFor(this.ContainingBlock);
      }
    }
    readonly List<TypeExpression> constraints;

    /// <summary>
    /// The compilation that contains this statement.
    /// </summary>
    public Compilation Compilation {
      get { return this.ContainingBlock.Compilation; }
    }

    /// <summary>
    /// The compilation part that this declaration forms a part of.
    /// </summary>
    /// <value></value>
    public CompilationPart CompilationPart {
      get { return this.ContainingBlock.CompilationPart; }
    }

    /// <summary>
    /// The block that contains this declaration.
    /// </summary>
    public BlockStatement ContainingBlock {
      get {
        //^ assume this.containingBlock != null;
        return this.containingBlock;
      }
    }
    /// <summary>
    /// The block that contains this declaration.
    /// </summary>
    protected BlockStatement/*?*/ containingBlock;

    private int flags;

    /// <summary>
    /// The position in the parameter list where this instance can be found.
    /// </summary>
    /// <value></value>
    public ushort Index {
      get { return this.index; }
    }
    readonly ushort index;

    /// <summary>
    /// True if all type arguments matching this parameter are constrained to be reference types.
    /// </summary>
    public bool MustBeReferenceType {
      get
        //^ ensures result ==> !this.MustBeValueType;
        //^ ensures result == ((this.flags & 0x60000000) == 0x40000000);
      {
        bool result = (this.flags & 0x60000000) == 0x40000000;
        //^ assume result ==> !this.MustBeValueType;
        return result;
      }
      protected set {
        this.flags &= ~0x60000000;
        if (value) this.flags |= 0x40000000;
      }
    }

    /// <summary>
    /// True if all type arguments matching this parameter are constrained to be value types.
    /// </summary>
    public bool MustBeValueType {
      get
        //^ ensures result == ((this.flags & 0x60000000) == 0x20000000);
      {
        bool result = (this.flags & 0x60000000) == 0x20000000;
        //^ assume result ==> !this.MustBeReferenceType;
        return result;
      }
      protected set {
        this.flags &= ~0x60000000;
        if (value) this.flags |= 0x20000000;
      }
    }

    /// <summary>
    /// True if all type arguments matching this parameter are constrained to be value types or concrete classes with visible default constructors.
    /// </summary>
    public bool MustHaveDefaultConstructor {
      get { return (this.flags & 0x10000000) != 0; }
      protected set { if (value) this.flags |= 0x10000000; else this.flags &= ~0x10000000; }
    }

    /// <summary>
    /// The name of the entity.
    /// </summary>
    /// <value></value>
    public NameDeclaration Name {
      get { return this.name; }
    }
    readonly NameDeclaration name;

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a generic parameter before constructing the declaring type or method.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public virtual void SetContainingExpression(Expression containingExpression) {
      this.containingBlock = containingExpression.ContainingBlock;
      foreach (TypeExpression constraint in this.constraints) constraint.SetContainingExpression(containingExpression);
      if (this.sourceAttributes != null)
        foreach (SourceCustomAttribute attribute in this.sourceAttributes) attribute.SetContainingExpression(containingExpression);
    }

    /// <summary>
    /// Custom attributes that are explicitly specified in source. Some of these may not end up in persisted metadata.
    /// </summary>
    /// <value></value>
    public IEnumerable<SourceCustomAttribute> SourceAttributes {
      get {
        List<SourceCustomAttribute> sourceAttributes;
        if (this.sourceAttributes == null)
          yield break;
        else
          sourceAttributes = this.sourceAttributes;
        for (int i = 0, n = sourceAttributes.Count; i < n; i++) {
          yield return sourceAttributes[i] = sourceAttributes[i].MakeShallowCopyFor(this.ContainingBlock);
        }
      }
    }
    readonly List<SourceCustomAttribute>/*?*/ sourceAttributes;
    //TODO: rather than use may be null fields to store parsely populated properties, use a dictionary 

    /// <summary>
    /// Indicates if the generic type or method with this type parameter is co-, contra-, or non variant with respect to this type parameter.
    /// </summary>
    public TypeParameterVariance Variance {
      get { return ((TypeParameterVariance)this.flags) & TypeParameterVariance.Mask; }
      protected set { this.flags |= (int)value; }
    }

    #region INamedEntity Members

    IName INamedEntity.Name {
      get { return this.Name; }
    }

    #endregion

  }

  /// <summary>
  /// Corresponds to a source construct that declares a type parameter for a generic type.
  /// </summary>
  public class GenericTypeParameterDeclaration : GenericParameterDeclaration {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sourceAttributes"></param>
    /// <param name="name"></param>
    /// <param name="index"></param>
    /// <param name="constraints"></param>
    /// <param name="variance"></param>
    /// <param name="mustBeReferenceType"></param>
    /// <param name="mustBeValueType"></param>
    /// <param name="mustHaveDefaultConstructor"></param>
    /// <param name="sourceLocation"></param>
    public GenericTypeParameterDeclaration(List<SourceCustomAttribute>/*?*/ sourceAttributes, NameDeclaration name,
      ushort index, List<TypeExpression> constraints, TypeParameterVariance variance, bool mustBeReferenceType, bool mustBeValueType, bool mustHaveDefaultConstructor, ISourceLocation sourceLocation)
      : base(sourceAttributes, name, index, constraints, variance, mustBeReferenceType, mustBeValueType, mustHaveDefaultConstructor, sourceLocation)
      //^ requires !mustBeReferenceType || !mustBeValueType;
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="declaringType"></param>
    /// <param name="template"></param>
    protected GenericTypeParameterDeclaration(TypeDeclaration declaringType, GenericTypeParameterDeclaration template)
      : base(declaringType.OuterDummyBlock, template) {
      this.declaringType = declaringType;
    }

    /// <summary>
    /// Makes a copy of this generic type parameter declaration, changing the target type to the given type.
    /// </summary>
    public virtual GenericTypeParameterDeclaration MakeCopyFor(TypeDeclaration targetDeclaringType) {
      if (this.declaringType == targetDeclaringType) return this;
      return new GenericTypeParameterDeclaration(targetDeclaringType, this);
    }

    /// <summary>
    /// Calls the visitor.Visit(GenericTypeParameterDeclaration) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The generic type that declares this type parameter.
    /// </summary>
    public TypeDeclaration DeclaringType {
      get {
        //^ assume this.declaringType != null;
        return this.declaringType;
      }
    }
    //^ [SpecPublic]
    TypeDeclaration/*?*/ declaringType;

    /// <summary>
    /// The symbol table entity that corresponds to this source construct.
    /// </summary>
    public IGenericTypeParameter GenericTypeParameterDefinition {
      get {
        foreach (GenericTypeParameter genericTypeParameter in this.DeclaringType.TypeDefinition.GenericParameters)
          if (genericTypeParameter.Index == this.Index) return genericTypeParameter;
        //^ assume false; //It is not OK to create a GenericTypeParameterDeclaration whose declaring type does not know about it.
        return Dummy.GenericTypeParameter;
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="declaringType"></param>
    public virtual void SetDeclaringType(TypeDeclaration declaringType)
      //^ requires this.declaringType == null;
    {
      DummyExpression containingExpression = new DummyExpression(declaringType.OuterDummyBlock, SourceDummy.SourceLocation);
      this.SetContainingExpression(containingExpression);
      this.declaringType = declaringType;
    }

  }

  /// <summary>
  /// Corresponds to a source construct that declares a class nested directly inside a namespace.
  /// </summary>
  public class NamespaceClassDeclaration : NamespaceTypeDeclaration, IClassDeclaration, INamespaceDeclarationMember {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sourceAttributes"></param>
    /// <param name="flags"></param>
    /// <param name="name"></param>
    /// <param name="genericParameters"></param>
    /// <param name="baseTypes"></param>
    /// <param name="members"></param>
    /// <param name="sourceLocation"></param>
    public NamespaceClassDeclaration(List<SourceCustomAttribute>/*?*/ sourceAttributes, Flags flags, NameDeclaration name,
      List<GenericTypeParameterDeclaration>/*?*/ genericParameters, List<TypeExpression> baseTypes, List<ITypeDeclarationMember> members, ISourceLocation sourceLocation)
      : base(sourceAttributes, flags, name, genericParameters, baseTypes, members, sourceLocation) {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="containingNamespaceDeclaration"></param>
    /// <param name="template"></param>
    protected NamespaceClassDeclaration(NamespaceDeclaration containingNamespaceDeclaration, NamespaceClassDeclaration template)
      : base(containingNamespaceDeclaration, template)
      //^ ensures this.containingNamespaceDeclaration == containingNamespaceDeclaration;
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="containingNamespaceDeclaration"></param>
    /// <param name="template"></param>
    /// <param name="members"></param>
    protected NamespaceClassDeclaration(NamespaceDeclaration containingNamespaceDeclaration, NamespaceClassDeclaration template, List<ITypeDeclarationMember> members)
      : base(containingNamespaceDeclaration, template, members)
      //^ ensures this.containingNamespaceDeclaration == containingNamespaceDeclaration;
    {
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the member or a constituent part of the member.
    /// Do not call this method directly, but evaluate the HasErrors property. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      bool result = base.CheckForErrorsAndReturnTrueIfAnyAreFound();
      return result;
    }

    /// <summary>
    /// Calls the visitor.Visit(NamespaceClassDeclaration) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    //^ [MustOverride]
    /// <summary>
    /// Returns a shallow copy of this namespace type with the given list of members replacing the template members.
    /// The containing namespace declaration should be set by means of a later call to SetContainingNamespaceDeclaration.
    /// </summary>
    /// <param name="members">The member list of the new type declaration.</param>
    /// <returns></returns>
    protected override NamespaceTypeDeclaration MakeShallowCopy(List<ITypeDeclarationMember> members)
      //^^ ensures result.GetType() == this.GetType();
    {
      return new NamespaceClassDeclaration(this.ContainingNamespaceDeclaration, this, members);
    }

    //^ [MustOverride]
    /// <summary>
    /// Returns a shallow copy of this instance with the given namespace as the new containing namespace.
    /// </summary>
    /// <param name="targetNamespaceDeclaration">The containing namespace of the result.</param>
    /// <returns></returns>
    public override INamespaceDeclarationMember MakeShallowCopyFor(NamespaceDeclaration targetNamespaceDeclaration)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingNamespaceDeclaration == targetNamespaceDeclaration;
    {
      if (targetNamespaceDeclaration == this.ContainingNamespaceDeclaration) return this;
      return new NamespaceClassDeclaration(targetNamespaceDeclaration, this);
    }

  }

  /// <summary>
  /// Corresponds to a source construct that declares a delegate nested directly inside a namespace.
  /// </summary>
  public class NamespaceDelegateDeclaration : NamespaceTypeDeclaration, IDelegateDeclaration, INamespaceDeclarationMember {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sourceAttributes"></param>
    /// <param name="flags"></param>
    /// <param name="name"></param>
    /// <param name="genericParameters"></param>
    /// <param name="signature"></param>
    /// <param name="sourceLocation"></param>
    public NamespaceDelegateDeclaration(List<SourceCustomAttribute>/*?*/ sourceAttributes,
      Flags flags, NameDeclaration name, List<GenericTypeParameterDeclaration> genericParameters, SignatureDeclaration signature, ISourceLocation sourceLocation)
      : base(sourceAttributes, flags, name, genericParameters, new List<TypeExpression>(0), new List<ITypeDeclarationMember>(0), sourceLocation) {
      this.signature = signature;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="containingNamespaceDeclaration"></param>
    /// <param name="template"></param>
    protected NamespaceDelegateDeclaration(NamespaceDeclaration containingNamespaceDeclaration, NamespaceDelegateDeclaration template)
      : base(containingNamespaceDeclaration, template)
      //^ ensures this.containingNamespaceDeclaration == containingNamespaceDeclaration;
    {
      this.signature = template.signature.MakeShallowCopyFor(containingNamespaceDeclaration.DummyBlock);
      //^ assume this.containingNamespaceDeclaration == containingNamespaceDeclaration;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="containingNamespaceDeclaration"></param>
    /// <param name="template"></param>
    /// <param name="members"></param>
    protected NamespaceDelegateDeclaration(NamespaceDeclaration containingNamespaceDeclaration, NamespaceDelegateDeclaration template, List<ITypeDeclarationMember> members)
      : base(containingNamespaceDeclaration, template, members) {
      this.signature = template.signature.MakeShallowCopyFor(containingNamespaceDeclaration.DummyBlock);
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the member or a constituent part of the member.
    /// Do not call this method directly, but evaluate the HasErrors property. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return false;
    }

    /// <summary>
    /// Creates a new type definition to correspond to this type declaration.
    /// </summary>
    /// <returns></returns>
    protected internal override NamespaceTypeDefinition CreateType() {
      return base.CreateType();
      //TODO: create a derived type specific to delegates
    }

    /// <summary>
    /// Calls the visitor.Visit(NamespaceDelegateDeclaration) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    //^ [MustOverride]
    /// <summary>
    /// Returns a shallow copy of this namespace type with the given list of members replacing the template members.
    /// The containing namespace declaration should be set by means of a later call to SetContainingNamespaceDeclaration.
    /// </summary>
    /// <param name="members">The member list of the new type declaration.</param>
    /// <returns></returns>
    protected override NamespaceTypeDeclaration MakeShallowCopy(List<ITypeDeclarationMember> members)
      //^^ ensures result.GetType() == this.GetType();
    {
      return new NamespaceDelegateDeclaration(this.ContainingNamespaceDeclaration, this, members);
    }

    //^ [MustOverride]
    /// <summary>
    /// Returns a shallow copy of this instance with the given namespace as the new containing namespace.
    /// </summary>
    /// <param name="targetNamespaceDeclaration">The containing namespace of the result.</param>
    /// <returns></returns>
    public override INamespaceDeclarationMember MakeShallowCopyFor(NamespaceDeclaration targetNamespaceDeclaration)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingNamespaceDeclaration == targetNamespaceDeclaration;
    {
      if (targetNamespaceDeclaration == this.ContainingNamespaceDeclaration) return this;
      return new NamespaceDelegateDeclaration(targetNamespaceDeclaration, this);
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a namespace type before constructing the namespace.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingNamespaceDeclaration(NamespaceDeclaration containingNamespaceDeclaration, bool recurse) {
      base.SetContainingNamespaceDeclaration(containingNamespaceDeclaration, recurse);
      this.Signature.SetContainingBlock(this.DummyBlock);
    }

    /// <summary>
    /// The signature of the Invoke method.
    /// </summary>
    /// <value></value>
    public SignatureDeclaration Signature {
      get {
        return this.signature;
      }
    }
    readonly SignatureDeclaration signature;

    #region IDelegateDeclaration Members

    ISignatureDeclaration IDelegateDeclaration.Signature {
      get {
        return this.Signature;
      }
    }

    #endregion

  }

  /// <summary>
  /// Corresponds to a source construct that declares an enumerated scalar type nested directly inside a namespace.
  /// </summary>
  public class NamespaceEnumDeclaration : NamespaceTypeDeclaration, IEnumDeclaration, INamespaceDeclarationMember {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sourceAttributes"></param>
    /// <param name="flags"></param>
    /// <param name="name"></param>
    /// <param name="underlyingType"></param>
    /// <param name="members"></param>
    /// <param name="sourceLocation"></param>
    public NamespaceEnumDeclaration(List<SourceCustomAttribute>/*?*/ sourceAttributes, Flags flags, NameDeclaration name,
       TypeExpression/*?*/ underlyingType, List<ITypeDeclarationMember> members, ISourceLocation sourceLocation)
      : base(sourceAttributes, flags|TypeDeclaration.Flags.Sealed, name, new List<GenericTypeParameterDeclaration>(0), new List<TypeExpression>(0), members, sourceLocation) {
      this.underlyingType = underlyingType;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="containingNamespaceDeclaration"></param>
    /// <param name="template"></param>
    protected NamespaceEnumDeclaration(NamespaceDeclaration containingNamespaceDeclaration, NamespaceEnumDeclaration template)
      : base(containingNamespaceDeclaration, template) {
      if (template.underlyingType != null)
        this.underlyingType = (TypeExpression)template.underlyingType.MakeCopyFor(containingNamespaceDeclaration.DummyBlock);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="containingNamespaceDeclaration"></param>
    /// <param name="template"></param>
    /// <param name="members"></param>
    protected NamespaceEnumDeclaration(NamespaceDeclaration containingNamespaceDeclaration, NamespaceEnumDeclaration template, List<ITypeDeclarationMember> members)
      : base(containingNamespaceDeclaration, template, members) {
      if (template.underlyingType != null)
        this.underlyingType = (TypeExpression)template.underlyingType.MakeCopyFor(containingNamespaceDeclaration.DummyBlock);
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the member or a constituent part of the member.
    /// Do not call this method directly, but evaluate the HasErrors property. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return false;
    }

    /// <summary>
    /// Calls the visitor.Visit(NamespaceEnumDeclaration) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    //^ [MustOverride]
    /// <summary>
    /// Returns a shallow copy of this namespace type with the given list of members replacing the template members.
    /// The containing namespace declaration should be set by means of a later call to SetContainingNamespaceDeclaration.
    /// </summary>
    /// <param name="members">The member list of the new type declaration.</param>
    /// <returns></returns>
    protected override NamespaceTypeDeclaration MakeShallowCopy(List<ITypeDeclarationMember> members)
      //^^ ensures result.GetType() == this.GetType();
    {
      return new NamespaceEnumDeclaration(this.ContainingNamespaceDeclaration, this, members);
    }

    //^ [MustOverride]
    /// <summary>
    /// Returns a shallow copy of this instance with the given namespace as the new containing namespace.
    /// </summary>
    /// <param name="targetNamespaceDeclaration">The containing namespace of the result.</param>
    /// <returns></returns>
    public override INamespaceDeclarationMember MakeShallowCopyFor(NamespaceDeclaration targetNamespaceDeclaration)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingNamespaceDeclaration == targetNamespaceDeclaration;
    {
      if (targetNamespaceDeclaration == this.ContainingNamespaceDeclaration) return this;
      return new NamespaceEnumDeclaration(targetNamespaceDeclaration, this);
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a namespace type before constructing the namespace.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    /// <param name="containingNamespaceDeclaration"></param>
    /// <param name="recurse"></param>
    public override void SetContainingNamespaceDeclaration(NamespaceDeclaration containingNamespaceDeclaration, bool recurse) {
      TypeExpression/*?*/ thisType = this.UnderlyingType;
      if (thisType == null) thisType = TypeExpression.For(containingNamespaceDeclaration.Helper.PlatformType.SystemInt32.ResolvedType);
      NameDeclaration value__ = new NameDeclaration(containingNamespaceDeclaration.Helper.NameTable.GetNameFor("value__"), SourceDummy.SourceLocation);
      FieldDeclaration field = new FieldDeclaration(null, FieldDeclaration.Flags.RuntimeSpecial|FieldDeclaration.Flags.SpecialName,
        TypeMemberVisibility.Public, thisType, value__, null, SourceDummy.SourceLocation);
      this.AddHelperMember(field);
      base.SetContainingNamespaceDeclaration(containingNamespaceDeclaration, recurse);
      field.SetContainingTypeDeclaration(this, true);
      if (this.UnderlyingType != null)
        this.UnderlyingType.SetContainingExpression(new DummyExpression(this.DummyBlock, SourceDummy.SourceLocation));
    }

    /// <summary>
    /// The primitive integral type that will be used to represent the values of enumeration. May be null.
    /// </summary>
    /// <value></value>
    public TypeExpression/*?*/ UnderlyingType {
      get {
        return this.underlyingType;
      }
    }
    readonly TypeExpression/*?*/ underlyingType;

    #region IEnumDeclaration Members

    TypeExpression/*?*/ IEnumDeclaration.UnderlyingType {
      get {
        return this.UnderlyingType;
      }
    }

    #endregion

  }

  /// <summary>
  /// Corresponds to a source construct that declares an interface nested directly inside a namespace.
  /// </summary>
  public class NamespaceInterfaceDeclaration : NamespaceTypeDeclaration, IInterfaceDeclaration, INamespaceDeclarationMember {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sourceAttributes"></param>
    /// <param name="flags"></param>
    /// <param name="name"></param>
    /// <param name="genericParameters"></param>
    /// <param name="baseTypes"></param>
    /// <param name="members"></param>
    /// <param name="sourceLocation"></param>
    public NamespaceInterfaceDeclaration(List<SourceCustomAttribute>/*?*/ sourceAttributes, Flags flags, NameDeclaration name,
      List<GenericTypeParameterDeclaration>/*?*/ genericParameters, List<TypeExpression> baseTypes, List<ITypeDeclarationMember> members, ISourceLocation sourceLocation)
      : base(sourceAttributes, flags, name, genericParameters, baseTypes, members, sourceLocation) {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="containingNamespaceDeclaration"></param>
    /// <param name="template"></param>
    protected NamespaceInterfaceDeclaration(NamespaceDeclaration containingNamespaceDeclaration, NamespaceInterfaceDeclaration template)
      : base(containingNamespaceDeclaration, template) {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="containingNamespaceDeclaration"></param>
    /// <param name="template"></param>
    /// <param name="members"></param>
    protected NamespaceInterfaceDeclaration(NamespaceDeclaration containingNamespaceDeclaration, NamespaceInterfaceDeclaration template, List<ITypeDeclarationMember> members)
      : base(containingNamespaceDeclaration, template, members) {
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the member or a constituent part of the member.
    /// Do not call this method directly, but evaluate the HasErrors property. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return false;
    }

    /// <summary>
    /// Calls the visitor.Visit(NamespaceInterfaceDeclaration) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    //^ [MustOverride]
    /// <summary>
    /// Returns a shallow copy of this namespace type with the given list of members replacing the template members.
    /// The containing namespace declaration should be set by means of a later call to SetContainingNamespaceDeclaration.
    /// </summary>
    /// <param name="members">The member list of the new type declaration.</param>
    /// <returns></returns>
    protected override NamespaceTypeDeclaration MakeShallowCopy(List<ITypeDeclarationMember> members)
      //^^ ensures result.GetType() == this.GetType();
    {
      return new NamespaceInterfaceDeclaration(this.ContainingNamespaceDeclaration, this, members);
    }

    //^ [MustOverride]
    /// <summary>
    /// Returns a shallow copy of this instance with the given namespace as the new containing namespace.
    /// </summary>
    /// <param name="targetNamespaceDeclaration">The containing namespace of the result.</param>
    /// <returns></returns>
    public override INamespaceDeclarationMember MakeShallowCopyFor(NamespaceDeclaration targetNamespaceDeclaration)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingNamespaceDeclaration == targetNamespaceDeclaration;
    {
      if (targetNamespaceDeclaration == this.ContainingNamespaceDeclaration) return this;
      return new NamespaceInterfaceDeclaration(targetNamespaceDeclaration, this);
    }

  }

  /// <summary>
  /// Corresponds to a source construct that declares a value type (struct) nested directly inside a namespace.
  /// </summary>
  public class NamespaceStructDeclaration : NamespaceTypeDeclaration, IStructDeclaration, INamespaceDeclarationMember {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sourceAttributes"></param>
    /// <param name="flags"></param>
    /// <param name="name"></param>
    /// <param name="genericParameters"></param>
    /// <param name="baseTypes"></param>
    /// <param name="members"></param>
    /// <param name="sourceLocation"></param>
    public NamespaceStructDeclaration(List<SourceCustomAttribute>/*?*/ sourceAttributes, Flags flags, NameDeclaration name,
      List<GenericTypeParameterDeclaration> genericParameters, List<TypeExpression> baseTypes, List<ITypeDeclarationMember> members, ISourceLocation sourceLocation)
      : base(sourceAttributes, flags|TypeDeclaration.Flags.Sealed, name, genericParameters, baseTypes, members, sourceLocation) {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="containingNamespaceDeclaration"></param>
    /// <param name="template"></param>
    protected NamespaceStructDeclaration(NamespaceDeclaration containingNamespaceDeclaration, NamespaceStructDeclaration template)
      : base(containingNamespaceDeclaration, template) {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="containingNamespaceDeclaration"></param>
    /// <param name="template"></param>
    /// <param name="members"></param>
    protected NamespaceStructDeclaration(NamespaceDeclaration containingNamespaceDeclaration, NamespaceStructDeclaration template, List<ITypeDeclarationMember> members)
      : base(containingNamespaceDeclaration, template, members) {
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the member or a constituent part of the member.
    /// Do not call this method directly, but evaluate the HasErrors property. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return false;
    }

    /// <summary>
    /// Calls the visitor.Visit(NamespaceStructDeclaration) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    //^ [MustOverride]
    /// <summary>
    /// Returns a shallow copy of this namespace type with the given list of members replacing the template members.
    /// The containing namespace declaration should be set by means of a later call to SetContainingNamespaceDeclaration.
    /// </summary>
    /// <param name="members">The member list of the new type declaration.</param>
    /// <returns></returns>
    protected override NamespaceTypeDeclaration MakeShallowCopy(List<ITypeDeclarationMember> members)
      //^^ ensures result.GetType() == this.GetType();
    {
      return new NamespaceStructDeclaration(this.ContainingNamespaceDeclaration, this, members);
    }

    /// <summary>
    /// Layout of the type declaration.
    /// </summary>
    public override LayoutKind Layout {
      get {
        return LayoutKind.Sequential; //TODO: get this from a custom attribute
      }
    }

    //^ [MustOverride]
    /// <summary>
    /// Returns a shallow copy of this instance with the given namespace as the new containing namespace.
    /// </summary>
    /// <param name="targetNamespaceDeclaration">The containing namespace of the result.</param>
    /// <returns></returns>
    public override INamespaceDeclarationMember MakeShallowCopyFor(NamespaceDeclaration targetNamespaceDeclaration)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingNamespaceDeclaration == targetNamespaceDeclaration;
    {
      if (targetNamespaceDeclaration == this.ContainingNamespaceDeclaration) return this;
      return new NamespaceStructDeclaration(targetNamespaceDeclaration, this);
    }

  }

  /// <summary>
  /// Corresponds to a source language type declaration (at the top level or nested in a namespace), such as a C# partial class. 
  /// One of more of these make up a type definition. 
  /// Each contains a collection of <see cref="NamespaceTypeDeclaration"/> instances in its <see cref="TypeDeclaration.TypeDeclarationMembers"/> property.
  /// The union of the collections make up the Members property of the type definition.
  /// </summary>
  public abstract class NamespaceTypeDeclaration : TypeDeclaration, INamespaceDeclarationMember, IAggregatableNamespaceDeclarationMember {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sourceAttributes"></param>
    /// <param name="flags"></param>
    /// <param name="name"></param>
    /// <param name="genericParameters"></param>
    /// <param name="baseTypes"></param>
    /// <param name="members"></param>
    /// <param name="sourceLocation"></param>
    protected NamespaceTypeDeclaration(List<SourceCustomAttribute>/*?*/ sourceAttributes, Flags flags, NameDeclaration name,
      List<GenericTypeParameterDeclaration>/*?*/ genericParameters, List<TypeExpression> baseTypes, List<ITypeDeclarationMember> members, ISourceLocation sourceLocation)
      : base(sourceAttributes, flags, name, genericParameters, baseTypes, members, sourceLocation) {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="containingNamespaceDeclaration"></param>
    /// <param name="template"></param>
    protected NamespaceTypeDeclaration(NamespaceDeclaration containingNamespaceDeclaration, NamespaceTypeDeclaration template)
      : base(containingNamespaceDeclaration.DummyBlock, template)
      //^ ensures this.containingNamespaceDeclaration == containingNamespaceDeclaration;
    {
      this.containingNamespaceDeclaration = containingNamespaceDeclaration;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="containingNamespaceDeclaration"></param>
    /// <param name="template"></param>
    /// <param name="members"></param>
    protected NamespaceTypeDeclaration(NamespaceDeclaration containingNamespaceDeclaration, NamespaceTypeDeclaration template, List<ITypeDeclarationMember> members)
      : base(containingNamespaceDeclaration.DummyBlock, template, members)
      //^ ensures this.containingNamespaceDeclaration == containingNamespaceDeclaration;
    {
      this.containingNamespaceDeclaration = containingNamespaceDeclaration;
    }

    /// <summary>
    /// The namespace that contains this member.
    /// </summary>
    public NamespaceDeclaration ContainingNamespaceDeclaration {
      get
        //^ ensures result == this.containingNamespaceDeclaration;
      {
        //^ assume this.containingNamespaceDeclaration != null;
        return this.containingNamespaceDeclaration;
      }
    }
    /// <summary>
    /// 
    /// </summary>
    protected NamespaceDeclaration/*?*/ containingNamespaceDeclaration;

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the member or a constituent part of the member.
    /// Do not call this method directly, but evaluate the HasErrors property. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      bool result = base.CheckForErrorsAndReturnTrueIfAnyAreFound();
      //TODO: any namespace type specific error checks. For example, is this type a duplicate of another?
      return result;
    }

    /// <summary>
    /// A block that is the containing block for any expressions contained inside the type declaration
    /// but not inside of a method.
    /// </summary>
    /// <value></value>
    public override BlockStatement DummyBlock {
      get {
        if (this.dummyBlock == null) {
          BlockStatement dummyBlock = BlockStatement.CreateDummyFor(this.SourceLocation);
          dummyBlock.SetContainers(this.ContainingNamespaceDeclaration.DummyBlock, this);
          lock (this) {
            if (this.dummyBlock == null) {
              this.dummyBlock = dummyBlock;
            }
          }
        }
        return this.dummyBlock;
      }
    }
    //^ [Once]
    private BlockStatement/*?*/ dummyBlock;

    private NamespaceTypeDefinition GetOrCreateType() {
      foreach (INamespaceMember member in this.ContainingNamespaceDeclaration.UnitNamespace.GetMembersNamed(this.Name, false)) {
        NamespaceTypeDefinition/*?*/ nt = member as NamespaceTypeDefinition;
        if (nt != null && nt.GenericParameterCount == this.GenericParameterCount) {
          if (this.namespaceTypeDefinition == nt) return nt;
          nt.AddTypeDeclaration(this);
          return nt;
        }
      }
      return this.CreateType();
    }

    /// <summary>
    /// Creates a new type definition to correspond to this type declaration.
    /// </summary>
    protected internal virtual NamespaceTypeDefinition CreateType() {
      NamespaceTypeDefinition result = new NamespaceTypeDefinition(this.ContainingNamespaceDeclaration.UnitNamespace, this.Name, this.Compilation.HostEnvironment.InternFactory);
      this.namespaceTypeDefinition = result;
      result.AddTypeDeclaration(this);
      ITypeContract/*?*/ typeContract = this.Compilation.ContractProvider.GetTypeContractFor(this);
      if (typeContract != null)
        this.Compilation.ContractProvider.AssociateTypeWithContract(result, typeContract);
      return result;
    }

    /// <summary>
    /// If true, this type is accessible outside of the unit that contains it.
    /// </summary>
    public virtual bool IsPublic {
      get {
        return (((TypeMemberVisibility)this.flags) & TypeMemberVisibility.Mask) == TypeMemberVisibility.Public;
      }
    }

    /// <summary>
    /// Returns a shallow copy of this namespace type with the given list of members replacing the template members. 
    /// The containing namespace declaration should be set by means of a later call to SetContainingNamespaceDeclaration.
    /// </summary>
    /// <param name="members">The member list of the new type declaration.</param>
    protected abstract NamespaceTypeDeclaration MakeShallowCopy(List<ITypeDeclarationMember> members);
    //^ ensures result.GetType() == this.GetType();

    /// <summary>
    /// Returns a shallow copy of this instance with the given namespace as the new containing namespace.
    /// </summary>
    /// <param name="targetNamespaceDeclaration">The containing namespace of the result.</param>
    public abstract INamespaceDeclarationMember MakeShallowCopyFor(NamespaceDeclaration targetNamespaceDeclaration);
    //^ ensures result.GetType() == this.GetType();
    //^ ensures result.ContainingNamespaceDeclaration == targetNamespaceDeclaration;

    /// <summary>
    /// The symbol table entity that corresponds to this source construct.
    /// </summary>
    public NamespaceTypeDefinition NamespaceTypeDefinition {
      get {
        if (this.namespaceTypeDefinition == null)
          this.namespaceTypeDefinition = this.GetOrCreateType();
        return this.namespaceTypeDefinition;
      }
    }
    NamespaceTypeDefinition/*?*/ namespaceTypeDefinition;

    /// <summary>
    /// A block that serves as the container for expressions that are not contained inside this type declaration, but that are part of the
    /// signature of this type declaration (for example a base class reference). The block scope includes the type parameters of this type
    /// declaration.
    /// </summary>
    /// <value></value>
    public override BlockStatement OuterDummyBlock {
      get {
        BlockStatement/*?*/ outerDummyBlock = this.outerDummyBlock;
        if (outerDummyBlock == null) {
          lock (GlobalLock.LockingObject) {
            if (this.outerDummyBlock == null) {
              this.outerDummyBlock = outerDummyBlock = BlockStatement.CreateDummyFor(this.SourceLocation);
              outerDummyBlock.SetContainers(this.ContainingNamespaceDeclaration.DummyBlock, this);
            }
          }
        }
        return outerDummyBlock;
      }
    }
    BlockStatement/*?*/ outerDummyBlock;

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a namespace type before constructing the namespace.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public virtual void SetContainingNamespaceDeclaration(NamespaceDeclaration containingNamespaceDeclaration, bool recurse) {
      this.containingNamespaceDeclaration = containingNamespaceDeclaration;
      this.OuterDummyBlock.SetContainers(containingNamespaceDeclaration.DummyBlock, this);
      this.SetCompilationPart(containingNamespaceDeclaration.CompilationPart, recurse);
    }

    /// <summary>
    /// The symbol table type definition that corresponds to this type declaration. If this type declaration is a partial type, the symbol table type
    /// will be an aggregate of multiple type declarations.
    /// </summary>
    /// <value></value>
    public override NamedTypeDefinition TypeDefinition {
      get { return this.NamespaceTypeDefinition; }
    }

    /// <summary>
    /// Returns a shallow copy of this type declaration that has the specified list of members as the value of its Members property.
    /// </summary>
    /// <param name="members">The members of the new type declaration.</param>
    /// <param name="edit">The edit that resulted in this update.</param>
    /// <returns></returns>
    public override TypeDeclaration UpdateMembers(List<ITypeDeclarationMember> members, ISourceDocumentEdit edit)
      //^^ requires edit.SourceDocumentAfterEdit.IsUpdatedVersionOf(this.SourceLocation.SourceDcoument);
      //^^ ensures result.GetType() == this.GetType();
    {
      NamespaceTypeDeclaration result = this.MakeShallowCopy(members);
      ISourceDocument afterEdit = edit.SourceDocumentAfterEdit;
      ISourceLocation locationBeforeEdit = this.SourceLocation;
      //^ assume afterEdit.IsUpdatedVersionOf(locationBeforeEdit.SourceDocument);
      result.sourceLocation = afterEdit.GetCorrespondingSourceLocation(locationBeforeEdit);
      List<INamespaceDeclarationMember> newParentMembers = new List<INamespaceDeclarationMember>(this.ContainingNamespaceDeclaration.Members);
      NamespaceDeclaration newParent = this.ContainingNamespaceDeclaration.UpdateMembers(newParentMembers, edit);
      result.containingNamespaceDeclaration = newParent;
      for (int i = 0, n = newParentMembers.Count; i < n; i++) {
        if (newParentMembers[i] == this) { newParentMembers[i] = result; break; }
      }
      return result;
    }

    #region INamespaceDeclarationMember Members

    NamespaceDeclaration INamespaceDeclarationMember.ContainingNamespaceDeclaration {
      get { return this.ContainingNamespaceDeclaration; }
    }

    INamespaceDeclarationMember INamespaceDeclarationMember.MakeShallowCopyFor(NamespaceDeclaration targetNamespaceDeclaration)
      //^^ requires targetNamespaceDeclaration.GetType() == this.ContainingNamespaceDeclaration.GetType();
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingNamespaceDeclaration == targetNamespaceDeclaration;
    {
      //^ assume targetNamespaceDeclaration is NamespaceDeclaration; //Follows from the precondition
      return this.MakeShallowCopyFor((NamespaceDeclaration)targetNamespaceDeclaration);
    }

    #endregion

    #region IContainerMember<NamespaceDeclaration> Members

    NamespaceDeclaration IContainerMember<NamespaceDeclaration>.Container {
      get { return this.ContainingNamespaceDeclaration; }
    }

    IName IContainerMember<NamespaceDeclaration>.Name {
      get { return this.Name; }
    }

    #endregion

    #region IAggregatableNamespaceDeclarationMember Members

    INamespaceMember IAggregatableNamespaceDeclarationMember.AggregatedMember {
      get { return this.NamespaceTypeDefinition; }
    }

    #endregion

  }

  /// <summary>
  /// Corresponds to a source construct that declares a class nested inside another type.
  /// </summary>
  public class NestedClassDeclaration : NestedTypeDeclaration, IClassDeclaration {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sourceAttributes"></param>
    /// <param name="flags"></param>
    /// <param name="name"></param>
    /// <param name="genericParameters"></param>
    /// <param name="baseTypes"></param>
    /// <param name="members"></param>
    /// <param name="sourceLocation"></param>
    public NestedClassDeclaration(List<SourceCustomAttribute>/*?*/ sourceAttributes, Flags flags, NameDeclaration name,
      List<GenericTypeParameterDeclaration> genericParameters, List<TypeExpression> baseTypes, List<ITypeDeclarationMember> members, ISourceLocation sourceLocation)
      : base(sourceAttributes, flags, name, genericParameters, baseTypes, members, sourceLocation) {
    }

    /// <summary>
    /// Create a nested class declaration from a non nested one 
    /// </summary>
    /// <param name="sourceClassDeclaration"></param>
    public NestedClassDeclaration(NamespaceClassDeclaration sourceClassDeclaration)
      : base(sourceClassDeclaration) {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="containingTypeDeclaration"></param>
    /// <param name="template"></param>
    protected NestedClassDeclaration(TypeDeclaration containingTypeDeclaration, NestedClassDeclaration template)
      : base(containingTypeDeclaration, template) {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="containingTypeDeclaration"></param>
    /// <param name="template"></param>
    /// <param name="members"></param>
    protected NestedClassDeclaration(TypeDeclaration containingTypeDeclaration, NestedClassDeclaration template, List<ITypeDeclarationMember> members)
      : base(containingTypeDeclaration, template, members) {
    }

    /// <summary>
    /// Calls the visitor.Visit(NestedClassDeclaration) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    //^ [MustOverride]
    /// <summary>
    /// Returns a shallow copy of this nested type with the given list of members replacing the template members.
    /// The containing type declaration should be set by means of a later call to SetContainingTypeDeclaration.
    /// </summary>
    /// <param name="members">The member list of the new type declaration.</param>
    /// <returns></returns>
    protected override NestedTypeDeclaration MakeShallowCopy(List<ITypeDeclarationMember> members)
      //^^ ensures result.GetType() == this.GetType();
    {
      return new NestedClassDeclaration(this.ContainingTypeDeclaration, this, members);
    }

    //^ [MustOverride]
    /// <summary>
    /// Returns a shallow copy of this nested type declaration with the given type declaration as the containing
    /// type declaration of the copy.
    /// </summary>
    /// <param name="targetTypeDeclaration">The type declaration that will contain the result.</param>
    /// <returns></returns>
    public override ITypeDeclarationMember MakeShallowCopyFor(TypeDeclaration targetTypeDeclaration) {
      if (targetTypeDeclaration == this.ContainingTypeDeclaration) return this;
      return new NestedClassDeclaration(targetTypeDeclaration, this);
    }

  }

  /// <summary>
  /// Corresponds to a source construct that declares a delegate nested inside another type.
  /// </summary>
  public class NestedDelegateDeclaration : NestedTypeDeclaration, IDelegateDeclaration {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sourceAttributes"></param>
    /// <param name="flags"></param>
    /// <param name="name"></param>
    /// <param name="genericParameters"></param>
    /// <param name="signature"></param>
    /// <param name="sourceLocation"></param>
    public NestedDelegateDeclaration(List<SourceCustomAttribute>/*?*/ sourceAttributes, Flags flags, NameDeclaration name,
      List<GenericTypeParameterDeclaration> genericParameters, SignatureDeclaration signature, ISourceLocation sourceLocation)
      : base(sourceAttributes, flags, name,
      genericParameters, new List<TypeExpression>(0), new List<ITypeDeclarationMember>(0), sourceLocation) {
      this.signature = signature;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="containingTypeDeclaration"></param>
    /// <param name="template"></param>
    protected NestedDelegateDeclaration(TypeDeclaration containingTypeDeclaration, NestedDelegateDeclaration template)
      : base(containingTypeDeclaration, template) {
      this.signature = template.signature; //TODO: copy the signature
    }

    /// <summary>
    /// Calls the visitor.Visit(NestedDelegateDeclaration) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    //^ [MustOverride]
    /// <summary>
    /// Returns a shallow copy of this nested type with the given list of members replacing the template members.
    /// The containing type declaration should be set by means of a later call to SetContainingTypeDeclaration.
    /// </summary>
    /// <param name="members">The member list of the new type declaration.</param>
    /// <returns></returns>
    protected override NestedTypeDeclaration MakeShallowCopy(List<ITypeDeclarationMember> members)
      //^^ ensures result.GetType() == this.GetType();
    {
      return new NestedDelegateDeclaration(this.ContainingTypeDeclaration, this);
    }

    //^ [MustOverride]
    /// <summary>
    /// Returns a shallow copy of this nested type declaration with the given type declaration as the containing
    /// type declaration of the copy.
    /// </summary>
    /// <param name="targetTypeDeclaration">The type declaration that will contain the result.</param>
    /// <returns></returns>
    public override ITypeDeclarationMember MakeShallowCopyFor(TypeDeclaration targetTypeDeclaration) {
      if (targetTypeDeclaration == this.ContainingTypeDeclaration) return this;
      return new NestedDelegateDeclaration(targetTypeDeclaration, this);
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a type member before constructing the containing type declaration.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    /// <param name="containingTypeDeclaration">The type declaration that contains this nested type declaration.</param>
    /// <param name="recurse">True if the construction of the children of this node need to be completed as well.</param>
    public override void SetContainingTypeDeclaration(TypeDeclaration containingTypeDeclaration, bool recurse) {
      base.SetContainingTypeDeclaration(containingTypeDeclaration, recurse);
      this.Signature.SetContainingBlock(this.DummyBlock);
    }

    /// <summary>
    /// The signature of the Invoke method.
    /// </summary>
    /// <value></value>
    public SignatureDeclaration Signature {
      get {
        return this.signature;
      }
    }
    readonly SignatureDeclaration signature;

    #region IDelegateDeclaration Members

    ISignatureDeclaration IDelegateDeclaration.Signature {
      get {
        return this.signature;
      }
    }

    #endregion

  }

  /// <summary>
  /// Corresponds to a source construct that declares an enumerated scalar type nested inside another type.
  /// </summary>
  public class NestedEnumDeclaration : NestedTypeDeclaration, IEnumDeclaration {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sourceAttributes"></param>
    /// <param name="flags"></param>
    /// <param name="name"></param>
    /// <param name="underlyingType"></param>
    /// <param name="members"></param>
    /// <param name="sourceLocation"></param>
    public NestedEnumDeclaration(List<SourceCustomAttribute>/*?*/ sourceAttributes, Flags flags, NameDeclaration name,
      TypeExpression/*?*/ underlyingType, List<ITypeDeclarationMember> members, ISourceLocation sourceLocation)
      : base(sourceAttributes, flags|TypeDeclaration.Flags.Sealed, name, new List<GenericTypeParameterDeclaration>(0), new List<TypeExpression>(0), members, sourceLocation) {
      this.underlyingType = underlyingType;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="containingTypeDeclaration"></param>
    /// <param name="template"></param>
    protected NestedEnumDeclaration(TypeDeclaration containingTypeDeclaration, NestedEnumDeclaration template)
      : base(containingTypeDeclaration, template) {
      if (template.UnderlyingType != null)
        this.underlyingType = (TypeExpression)template.UnderlyingType.MakeCopyFor(containingTypeDeclaration.OuterDummyBlock);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="containingTypeDeclaration"></param>
    /// <param name="template"></param>
    /// <param name="members"></param>
    protected NestedEnumDeclaration(TypeDeclaration containingTypeDeclaration, NestedEnumDeclaration template, List<ITypeDeclarationMember> members)
      : base(containingTypeDeclaration, template, members) {
      if (template.UnderlyingType != null)
        this.underlyingType = (TypeExpression)template.UnderlyingType.MakeCopyFor(containingTypeDeclaration.OuterDummyBlock);
    }

    /// <summary>
    /// Calls the visitor.Visit(NestedEnumDeclaration) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    //^ [MustOverride]
    /// <summary>
    /// Returns a shallow copy of this nested type with the given list of members replacing the template members.
    /// The containing type declaration should be set by means of a later call to SetContainingTypeDeclaration.
    /// </summary>
    /// <param name="members">The member list of the new type declaration.</param>
    /// <returns></returns>
    protected override NestedTypeDeclaration MakeShallowCopy(List<ITypeDeclarationMember> members)
      //^^ ensures result.GetType() == this.GetType();
    {
      return new NestedEnumDeclaration(this.ContainingTypeDeclaration, this, members);
    }

    //^ [MustOverride]
    /// <summary>
    /// Returns a shallow copy of this nested type declaration with the given type declaration as the containing
    /// type declaration of the copy.
    /// </summary>
    /// <param name="targetTypeDeclaration">The type declaration that will contain the result.</param>
    /// <returns></returns>
    public override ITypeDeclarationMember MakeShallowCopyFor(TypeDeclaration targetTypeDeclaration) {
      if (targetTypeDeclaration == this.ContainingTypeDeclaration) return this;
      return new NestedEnumDeclaration(targetTypeDeclaration, this);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="compilationPart"></param>
    /// <param name="recurse"></param>
    public override void SetCompilationPart(CompilationPart compilationPart, bool recurse) {
      TypeExpression/*?*/ thisType = this.UnderlyingType;
      if (thisType == null) thisType = TypeExpression.For(compilationPart.Helper.PlatformType.SystemInt32.ResolvedType);
      NameDeclaration value__ = new NameDeclaration(compilationPart.Helper.NameTable.GetNameFor("value__"), SourceDummy.SourceLocation);
      FieldDeclaration field = new FieldDeclaration(null, FieldDeclaration.Flags.RuntimeSpecial|FieldDeclaration.Flags.SpecialName,
        TypeMemberVisibility.Public, thisType, value__, null, SourceDummy.SourceLocation);
      this.AddHelperMember(field);
      base.SetCompilationPart(compilationPart, recurse);
      field.SetContainingTypeDeclaration(this, true);
      if (!recurse) return;
      BlockStatement dummyBlock = this.OuterDummyBlock;
      DummyExpression containingExpression = new DummyExpression(dummyBlock, SourceDummy.SourceLocation);
      if (this.underlyingType != null)
        this.underlyingType.SetContainingExpression(containingExpression);
    }

    /// <summary>
    /// The primitive integral type that will be used to represent the values of enumeration. May be null.
    /// </summary>
    /// <value></value>
    public TypeExpression/*?*/ UnderlyingType {
      get {
        return this.underlyingType;
      }
    }
    readonly TypeExpression/*?*/ underlyingType;

    #region IEnumDeclaration Members

    TypeExpression/*?*/ IEnumDeclaration.UnderlyingType {
      get {
        return this.UnderlyingType;
      }
    }

    #endregion
  }

  /// <summary>
  /// Corresponds to a source construct that declares an interface nested inside another type.
  /// </summary>
  public class NestedInterfaceDeclaration : NestedTypeDeclaration, IInterfaceDeclaration {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sourceAttributes"></param>
    /// <param name="flags"></param>
    /// <param name="name"></param>
    /// <param name="genericParameters"></param>
    /// <param name="baseTypes"></param>
    /// <param name="members"></param>
    /// <param name="sourceLocation"></param>
    public NestedInterfaceDeclaration(List<SourceCustomAttribute>/*?*/ sourceAttributes, Flags flags, NameDeclaration name,
      List<GenericTypeParameterDeclaration> genericParameters, List<TypeExpression> baseTypes, List<ITypeDeclarationMember> members, ISourceLocation sourceLocation)
      : base(sourceAttributes, flags, name, genericParameters, baseTypes, members, sourceLocation) {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="containingTypeDeclaration"></param>
    /// <param name="template"></param>
    protected NestedInterfaceDeclaration(TypeDeclaration containingTypeDeclaration, NestedInterfaceDeclaration template)
      : base(containingTypeDeclaration, template) {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="containingTypeDeclaration"></param>
    /// <param name="template"></param>
    /// <param name="members"></param>
    protected NestedInterfaceDeclaration(TypeDeclaration containingTypeDeclaration, NestedInterfaceDeclaration template, List<ITypeDeclarationMember> members)
      : base(containingTypeDeclaration, template, members) {
    }

    /// <summary>
    /// Calls the visitor.Visit(NestedInterfaceDeclaration) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    //^ [MustOverride]
    /// <summary>
    /// Returns a shallow copy of this nested type with the given list of members replacing the template members.
    /// The containing type declaration should be set by means of a later call to SetContainingTypeDeclaration.
    /// </summary>
    /// <param name="members">The member list of the new type declaration.</param>
    /// <returns></returns>
    protected override NestedTypeDeclaration MakeShallowCopy(List<ITypeDeclarationMember> members)
      //^^ ensures result.GetType() == this.GetType();
    {
      return new NestedInterfaceDeclaration(this.ContainingTypeDeclaration, this, members);
    }

    //^ [MustOverride]
    /// <summary>
    /// Returns a shallow copy of this nested type declaration with the given type declaration as the containing
    /// type declaration of the copy.
    /// </summary>
    /// <param name="targetTypeDeclaration">The type declaration that will contain the result.</param>
    /// <returns></returns>
    public override ITypeDeclarationMember MakeShallowCopyFor(TypeDeclaration targetTypeDeclaration) {
      if (targetTypeDeclaration == this.ContainingTypeDeclaration) return this;
      return new NestedInterfaceDeclaration(targetTypeDeclaration, this);
    }

  }

  /// <summary>
  /// Corresponds to a source construct that declares a value type (struct) nested inside another type.
  /// </summary>
  public class NestedStructDeclaration : NestedTypeDeclaration, IStructDeclaration {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sourceAttributes"></param>
    /// <param name="flags"></param>
    /// <param name="name"></param>
    /// <param name="genericParameters"></param>
    /// <param name="baseTypes"></param>
    /// <param name="members"></param>
    /// <param name="sourceLocation"></param>
    public NestedStructDeclaration(List<SourceCustomAttribute>/*?*/ sourceAttributes, Flags flags, NameDeclaration name,
      List<GenericTypeParameterDeclaration> genericParameters, List<TypeExpression> baseTypes, List<ITypeDeclarationMember> members, ISourceLocation sourceLocation)
      : base(sourceAttributes, flags|TypeDeclaration.Flags.Sealed, name, genericParameters, baseTypes, members, sourceLocation) {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="containingTypeDeclaration"></param>
    /// <param name="template"></param>
    protected NestedStructDeclaration(TypeDeclaration containingTypeDeclaration, NestedStructDeclaration template)
      : base(containingTypeDeclaration, template)
      // ^ ensures this.ContainingTypeDeclaration == containingTypeDeclaration; //Spec# problem with delayed receiver
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="containingTypeDeclaration"></param>
    /// <param name="template"></param>
    /// <param name="members"></param>
    protected NestedStructDeclaration(TypeDeclaration containingTypeDeclaration, NestedStructDeclaration template, List<ITypeDeclarationMember> members)
      : base(containingTypeDeclaration, template, members)
      // ^ ensures this.ContainingTypeDeclaration == containingTypeDeclaration; //Spec# problem with delayed receiver
    {
    }

    /// <summary>
    /// Calls the visitor.Visit(NestedStructDeclaration) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Layout of the type declaration.
    /// </summary>
    public override LayoutKind Layout {
      get {
        return LayoutKind.Sequential; //TODO: get this from a custom attribute
      }
    }

    //^ [MustOverride]
    /// <summary>
    /// Returns a shallow copy of this nested type with the given list of members replacing the template members.
    /// The containing type declaration should be set by means of a later call to SetContainingTypeDeclaration.
    /// </summary>
    /// <param name="members">The member list of the new type declaration.</param>
    /// <returns></returns>
    protected override NestedTypeDeclaration MakeShallowCopy(List<ITypeDeclarationMember> members)
      //^^ ensures result.GetType() == this.GetType();
    {
      return new NestedStructDeclaration(this.ContainingTypeDeclaration, this, members);
    }

    //^ [MustOverride]
    /// <summary>
    /// Returns a shallow copy of this nested type declaration with the given type declaration as the containing
    /// type declaration of the copy.
    /// </summary>
    /// <param name="targetTypeDeclaration">The type declaration that will contain the result.</param>
    /// <returns></returns>
    public override ITypeDeclarationMember MakeShallowCopyFor(TypeDeclaration targetTypeDeclaration) {
      if (targetTypeDeclaration == this.ContainingTypeDeclaration) return this;
      return new NestedStructDeclaration(targetTypeDeclaration, this);
    }
  }

  /// <summary>
  /// Corresponds to a source language type declaration nested inside another type declaration, such as a C# partial class. 
  /// One of more of these make up a type definition. 
  /// Each contains a collection of <see cref="NestedTypeDeclaration"/> instances in its <see cref="TypeDeclaration.TypeDeclarationMembers"/> property.
  /// The union of the collections make up the Members property of the type definition.
  /// </summary>
  public abstract class NestedTypeDeclaration : TypeDeclaration, ITypeDeclarationMember, IAggregatableTypeDeclarationMember {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sourceAttributes"></param>
    /// <param name="flags"></param>
    /// <param name="name"></param>
    /// <param name="genericParameters"></param>
    /// <param name="baseTypes"></param>
    /// <param name="members"></param>
    /// <param name="sourceLocation"></param>
    protected NestedTypeDeclaration(List<SourceCustomAttribute>/*?*/ sourceAttributes, Flags flags, NameDeclaration name,
      List<GenericTypeParameterDeclaration> genericParameters, List<TypeExpression> baseTypes, List<ITypeDeclarationMember> members, ISourceLocation sourceLocation)
      : base(sourceAttributes, flags, name, genericParameters, baseTypes, members, sourceLocation) {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sourceTypeDeclaration"></param>
    protected NestedTypeDeclaration(NamespaceTypeDeclaration sourceTypeDeclaration)
      : base(sourceTypeDeclaration.CompilationPart, sourceTypeDeclaration) {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="containingTypeDeclaration"></param>
    /// <param name="template"></param>
    protected NestedTypeDeclaration(TypeDeclaration containingTypeDeclaration, NestedTypeDeclaration template)
      : base(containingTypeDeclaration.DummyBlock, template)
      // ^ ensures this.ContainingTypeDeclaration == containingTypeDeclaration; //Spec# problem with delayed receiver
    {
      this.containingTypeDeclaration = containingTypeDeclaration;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="containingTypeDeclaration"></param>
    /// <param name="template"></param>
    /// <param name="members"></param>
    protected NestedTypeDeclaration(TypeDeclaration containingTypeDeclaration, NestedTypeDeclaration template, List<ITypeDeclarationMember> members)
      : base(containingTypeDeclaration.DummyBlock, template, members)
      // ^ ensures this.ContainingTypeDeclaration == containingTypeDeclaration; //Spec# problem with delayed receiver
    {
      this.containingTypeDeclaration = containingTypeDeclaration;
    }

    /// <summary>
    /// True if the given type member may be accessed from the scope defined by this block. For example, if the member is private and this
    /// block is defined inside a method of the containing type of the member, the result is true.
    /// </summary>
    /// <param name="member">The type member to check.</param>
    public override bool CanAccess(ITypeDefinitionMember member) {
      if (base.CanAccess(member)) return true;
      return this.ContainingTypeDeclaration.CanAccess(member);
    }

    /// <summary>
    /// True if the given type may be accessed from the scope defined by this block. For example, if the type is private nested type and this
    /// block is defined inside a method of the containing type of the member, the result is true.
    /// </summary>
    /// <param name="typeDefinition">The type to check.</param>
    /// <returns></returns>
    public override bool CanAccess(ITypeDefinition typeDefinition) {
      if (base.CanAccess(typeDefinition)) return true;
      return this.ContainingTypeDeclaration.CanAccess(typeDefinition);
    }

    /// <summary>
    /// The type declaration that contains this member.
    /// </summary>
    /// <value></value>
    public TypeDeclaration ContainingTypeDeclaration {
      get {
        //^ assume this.containingTypeDeclaration != null;
        return this.containingTypeDeclaration;
      }
    }
    //^ [SpecPublic]
    TypeDeclaration/*?*/ containingTypeDeclaration;

    /// <summary>
    /// A block that is the containing block for any expressions contained inside the type declaration
    /// but not inside of a method.
    /// </summary>
    /// <value></value>
    public override BlockStatement DummyBlock {
      get {
        if (this.dummyBlock == null) {
          BlockStatement dummyBlock = BlockStatement.CreateDummyFor(this.SourceLocation);
          dummyBlock.SetContainers(this.ContainingTypeDeclaration.DummyBlock, this);
          lock (this) {
            if (this.dummyBlock == null) {
              this.dummyBlock = dummyBlock;
            }
          }
        }
        return this.dummyBlock;
      }
    }
    //^ [Once]
    private BlockStatement/*?*/ dummyBlock;

    private NestedTypeDefinition CreateNestedTypeAndUpdateBackingField() {
      var containingTypeDef = this.ContainingTypeDeclaration.TypeDefinition;
      if (this.nestedTypeDefinition != null) {
        //Could have happened as a side-effect of contructing the containing type definition.
        //Typically this only happens if the nested type declaration is asked about its type definition 
        //before the containing type declaration is asked about its type definition.
        return this.nestedTypeDefinition;
      }
      foreach (TypeDeclaration containingTypeDeclaration in containingTypeDef.TypeDeclarations) {
        foreach (ITypeDeclarationMember member in containingTypeDeclaration.TypeDeclarationMembers) {
          if (member == this) continue;
          NestedTypeDeclaration/*?*/ nt = member as NestedTypeDeclaration;
          if (nt != null && nt.Name.UniqueKey == this.Name.UniqueKey && nt.GenericParameterCount == this.GenericParameterCount) {
            nt.TypeDefinition.AddTypeDeclaration(this);
            return nt.NestedTypeDefinition;
          }
        }
      }
      if (this.nestedTypeDefinition != null) 
        return this.nestedTypeDefinition; //TODO: this might be dead code. For now, rather be safe than sorry.
      else
        return this.CreateNestedType();
    }

    /// <summary>
    /// Creates a type definition to correspond to this type declaration.
    /// </summary>
    /// <returns></returns>
    protected internal virtual NestedTypeDefinition CreateNestedType() {
      NestedTypeDefinition result = new NestedTypeDefinition(this.ContainingTypeDeclaration.TypeDefinition, this.Name, this.Compilation.HostEnvironment.InternFactory);
      this.nestedTypeDefinition = result;
      result.AddTypeDeclaration(this);
      ITypeContract/*?*/ typeContract = this.Compilation.ContractProvider.GetTypeContractFor(this);
      if (typeContract != null)
        this.Compilation.ContractProvider.AssociateTypeWithContract(result, typeContract);
      return result;
    }

    /// <summary>
    /// Indicates that this member is intended to hide the name of an inherited member.
    /// </summary>
    /// <value></value>
    public virtual bool IsNew {
      get {
        return (this.flags & Flags.New) != 0;
      }
    }

    /// <summary>
    /// Returns a shallow copy of this nested type with the given list of members replacing the template members. 
    /// The containing type declaration should be set by means of a later call to SetContainingTypeDeclaration.
    /// </summary>
    /// <param name="members">The member list of the new type declaration.</param>
    protected abstract NestedTypeDeclaration MakeShallowCopy(List<ITypeDeclarationMember> members);
    //^ ensures result.GetType() == this.GetType();

    /// <summary>
    /// Returns a shallow copy of this nested type declaration with the given type declaration as the containing
    /// type declaration of the copy.
    /// </summary>
    /// <param name="targetTypeDeclaration">The type declaration that will contain the result.</param>
    /// <returns></returns>
    public abstract ITypeDeclarationMember MakeShallowCopyFor(TypeDeclaration targetTypeDeclaration);
    //^ ensures result.GetType() == this.GetType();
    //^ ensures result.ContainingTypeDeclaration == targetTypeDeclaration;

    /// <summary>
    /// The symbol table entity that corresponds to this source construct.
    /// </summary>
    public NestedTypeDefinition NestedTypeDefinition {
      get {
        if (this.nestedTypeDefinition == null) {
          lock (GlobalLock.LockingObject) {
            if (this.nestedTypeDefinition == null)
              this.CreateNestedTypeAndUpdateBackingField(); //the backing field is updated before returning, in order to short-circuit recursive call backs.
          }
        }
        return this.nestedTypeDefinition;
      }
    }
    NestedTypeDefinition/*?*/ nestedTypeDefinition;

    /// <summary>
    /// A block that serves as the container for expressions that are not contained inside this type declaration, but that are part of the
    /// signature of this type declaration (for example a base class reference). The block scope includes the type parameters of this type
    /// declaration.
    /// </summary>
    /// <value></value>
    public override BlockStatement OuterDummyBlock {
      get {
        if (this.outerDummyBlock == null) {
          lock (GlobalLock.LockingObject) {
            if (this.outerDummyBlock == null) {
              this.outerDummyBlock = BlockStatement.CreateDummyFor(this.SourceLocation);
              this.outerDummyBlock.SetContainers(this.ContainingTypeDeclaration.DummyBlock, this);
            }
          }
        }
        return this.outerDummyBlock;
      }
    }
    BlockStatement/*?*/ outerDummyBlock;

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a type member before constructing the containing type declaration.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    /// <param name="containingTypeDeclaration">The type declaration that contains this nested type declaration.</param>
    /// <param name="recurse">True if the construction of the children of this node need to be completed as well.</param>
    public virtual void SetContainingTypeDeclaration(TypeDeclaration containingTypeDeclaration, bool recurse) {
      this.containingTypeDeclaration = containingTypeDeclaration;
      this.OuterDummyBlock.SetContainers(containingTypeDeclaration.DummyBlock, this);
      this.SetCompilationPart(containingTypeDeclaration.CompilationPart, recurse);
    }

    /// <summary>
    /// The symbol table type definition that corresponds to this type declaration. If this type declaration is a partial type, the symbol table type
    /// will be an aggregate of multiple type declarations.
    /// </summary>
    /// <value></value>
    public override NamedTypeDefinition TypeDefinition {
      get { return this.NestedTypeDefinition; }
    }

    /// <summary>
    /// Returns a shallow copy of this type declaration that has the specified list of members as the value of its Members property.
    /// </summary>
    /// <param name="members">The members of the new type declaration.</param>
    /// <param name="edit">The edit that resulted in this update.</param>
    /// <returns></returns>
    public override TypeDeclaration UpdateMembers(List<ITypeDeclarationMember> members, ISourceDocumentEdit edit)
      //^^ requires edit.SourceDocumentAfterEdit.IsUpdatedVersionOf(this.SourceLocation.SourceDcoument);
      //^^ ensures result.GetType() == this.GetType();
    {
      NestedTypeDeclaration result = this.MakeShallowCopy(members);
      ISourceDocument afterEdit = edit.SourceDocumentAfterEdit;
      ISourceLocation locationBeforeEdit = this.SourceLocation;
      //^ assume afterEdit.IsUpdatedVersionOf(locationBeforeEdit.SourceDocument);
      result.sourceLocation = afterEdit.GetCorrespondingSourceLocation(locationBeforeEdit);
      List<ITypeDeclarationMember> newParentMembers = new List<ITypeDeclarationMember>(this.ContainingTypeDeclaration.TypeDeclarationMembers);
      TypeDeclaration containingTypeDeclaration = this.ContainingTypeDeclaration;
      //^ assume edit.SourceDocumentAfterEdit.IsUpdatedVersionOf(containingTypeDeclaration.SourceLocation.SourceDocument);
      TypeDeclaration newParent = containingTypeDeclaration.UpdateMembers(newParentMembers, edit);
      result.containingTypeDeclaration = newParent;
      for (int i = 0, n = newParentMembers.Count; i < n; i++) {
        if (newParentMembers[i] == this) { newParentMembers[i] = result; break; }
      }
      return result;
    }

    /// <summary>
    /// Indicates if the member is public or confined to its containing type, derived types and/or declaring assembly.
    /// </summary>
    /// <value></value>
    public TypeMemberVisibility Visibility {
      get {
        return ((TypeMemberVisibility)this.flags) & TypeMemberVisibility.Mask;
      }
    }

    #region ITypeDeclarationMember Members

    TypeDeclaration ITypeDeclarationMember.ContainingTypeDeclaration {
      get { return this.ContainingTypeDeclaration; }
    }

    ITypeDeclarationMember ITypeDeclarationMember.MakeShallowCopyFor(TypeDeclaration targetTypeDeclaration)
      //^^ requires targetTypeDeclaration.GetType() == this.ContainingTypeDeclaration.GetType();
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingTypeDeclaration == targetTypeDeclaration;
    {
      //^ assume targetTypeDeclaration is TypeDeclaration; //follows from the precondition
      return this.MakeShallowCopyFor((TypeDeclaration)targetTypeDeclaration);
    }

    ITypeDefinitionMember/*?*/ ITypeDeclarationMember.TypeDefinitionMember {
      get { return this.NestedTypeDefinition; }
    }

    #endregion

    #region IContainerMember<TypeDeclaration> Members

    TypeDeclaration IContainerMember<TypeDeclaration>.Container {
      get { return this.ContainingTypeDeclaration; }
    }

    IName IContainerMember<TypeDeclaration>.Name {
      get { return this.Name; }
    }

    #endregion

    #region IAggregatableMember Members

    ITypeDefinitionMember IAggregatableTypeDeclarationMember.AggregatedMember {
      get { return this.NestedTypeDefinition; }
    }

    #endregion

  }

  /// <summary>
  /// Corresponds to a source language type declaration, such as a C# partial class. One of more of these make up a type definition. 
  /// </summary>
  public abstract class TypeDeclaration : SourceItemWithAttributes, IContainer<IAggregatableTypeDeclarationMember>, IContainer<ITypeDeclarationMember>, IDeclaration, INamedEntity, IErrorCheckable {
    /// <summary>
    /// A collection of flags that correspond to modifiers such as public and abstract.
    /// </summary>
    [Flags]
    public enum Flags {
      /// <summary>
      /// No source code construct was found that specifies one of the other flag values. Use defaults.
      /// </summary>
      None=0x00000000,

      /// <summary>
      /// The declared type is abstract.
      /// </summary>
      Abstract=0x40000000,

      /// <summary>
      /// The declared type is a nested type that hides a nested type declared a base class.
      /// </summary>
      New=0x20000000,

      /// <summary>
      /// The declared type may be specified by more than one syntatic construct.
      /// </summary>
      Partial=0x10000000,

      /// <summary>
      /// The declared type may not be used as a base class.
      /// </summary>
      Sealed=0x08000000,

      /// <summary>
      /// The declared type has a private constructor and no instance state or instance methods.
      /// </summary>
      Static=0x04000000,

      /// <summary>
      /// The declared type may contain constructs that are not verifiably type safe.
      /// </summary>
      Unsafe=0x02000000,
    }

    /// <summary>
    /// 
    /// </summary>
    protected readonly Flags flags;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sourceAttributes"></param>
    /// <param name="flags"></param>
    /// <param name="name"></param>
    /// <param name="genericParameters"></param>
    /// <param name="baseTypes"></param>
    /// <param name="members"></param>
    /// <param name="sourceLocation"></param>
    protected TypeDeclaration(List<SourceCustomAttribute>/*?*/ sourceAttributes, Flags flags, NameDeclaration name,
      List<GenericTypeParameterDeclaration>/*?*/ genericParameters, List<TypeExpression> baseTypes, List<ITypeDeclarationMember> members, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.sourceAttributes = sourceAttributes;
      this.flags = flags;
      this.name = name;
      this.genericParameters = genericParameters;
      this.baseTypes = baseTypes;
      this.typeDeclarationMembers = members;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="compilationPart"></param>
    /// <param name="template"></param>
    protected TypeDeclaration(CompilationPart compilationPart, TypeDeclaration template)
      : base(template.SourceLocation) {
      this.sourceAttributes = new List<SourceCustomAttribute>(template.SourceAttributes);
      this.flags = template.flags;
      this.compilationPart = compilationPart;
      this.name = template.Name.MakeCopyFor(compilationPart.Compilation);
      this.genericParameters = new List<GenericTypeParameterDeclaration>(template.GenericParameters);
      this.baseTypes = new List<TypeExpression>(template.BaseTypes);
      this.typeDeclarationMembers = new List<ITypeDeclarationMember>(template.typeDeclarationMembers);
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected TypeDeclaration(BlockStatement containingBlock, TypeDeclaration template)
      : this(containingBlock, template, new List<ITypeDeclarationMember>(template.typeDeclarationMembers)) {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="containingBlock"></param>
    /// <param name="template"></param>
    /// <param name="members"></param>
    protected TypeDeclaration(BlockStatement containingBlock, TypeDeclaration template, List<ITypeDeclarationMember> members)
      : base(template.SourceLocation) {
      this.sourceAttributes = new List<SourceCustomAttribute>(template.SourceAttributes);
      this.flags = template.flags;
      this.compilationPart = containingBlock.CompilationPart;
      this.name = template.Name.MakeCopyFor(containingBlock.Compilation);
      this.genericParameters = new List<GenericTypeParameterDeclaration>(template.GenericParameters);
      this.baseTypes = new List<TypeExpression>(template.BaseTypes);
      this.typeDeclarationMembers = members;
    }

    /// <summary>
    /// Compute the offset of its field. For example, a struct in C and a union will have
    /// different say about its field's offset. 
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public virtual uint GetFieldOffset(object item) {
      return 0;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="typeDeclarationMember"></param>
    public void AddHelperMember(ITypeDeclarationMember typeDeclarationMember) {
      lock (this) {
        if (this.helperMembers == null)
          this.helperMembers = new List<ITypeDeclarationMember>();
        this.helperMembers.Add(typeDeclarationMember);
      }
    }

    /// <summary>
    /// The byte alignment that values of the given type ought to have. Must be a power of 2. If zero, the alignment is decided at runtime.
    /// </summary>
    public virtual ushort Alignment {
      get { return 0; } //TODO: provide a default implementation that extracts this from a custom attribute
    }

    /// <summary>
    /// Returns a list of custom attributes that describes this type declaration member.
    /// Typically, these will be derived from this.SourceAttributes. However, some source attributes
    /// might instead be persisted as metadata bits and other custom attributes may be synthesized
    /// from information not provided in the form of source custom attributes.
    /// The list is not trimmed to size, since an override of this method may call the base method
    /// and then add more attributes.
    /// </summary>
    protected override List<ICustomAttribute> GetAttributes() {
      List<ICustomAttribute> result = new List<ICustomAttribute>();
      foreach (var attribute in this.SourceAttributes) {
        if (attribute.HasErrors) continue;
        //TODO: filter out pseudo custom attributes
        result.Add(new CustomAttribute(attribute));
      }
      return result;
    }

    /// <summary>
    /// A collection of expressions that refer to the base types (classes and interfaces) of this type.
    /// </summary>
    public IEnumerable<TypeExpression> BaseTypes {
      get {
        for (int i = 0, n = this.baseTypes.Count; i < n; i++)
          yield return this.baseTypes[i] = (TypeExpression)this.baseTypes[i].MakeCopyFor(this.OuterDummyBlock);
      }
    }
    readonly List<TypeExpression> baseTypes;

    /// <summary>
    /// True if the given type member may be accessed by this type declaration. For example, if the member is private and this
    /// is that same as the containing type of the member, or is a nested type of the containing type of the member, the result is true.
    /// </summary>
    /// <param name="member">The type member to check.</param>
    public virtual bool CanAccess(ITypeDefinitionMember member) {
      if (this.TypeDefinition == member.ContainingTypeDefinition) return true;
      if (this.TypeDefinition.IsGeneric && TypeHelper.TypesAreEquivalent(this.TypeDefinition.InstanceType, member.ContainingTypeDefinition))
        return true;
      var geninst = member.ContainingTypeDefinition as IGenericTypeInstance;
      if (geninst != null && this.TypeDefinition == geninst.GenericType.ResolvedType) return true;
      if (!this.CanAccess(member.ContainingTypeDefinition)) return false;
      switch (member.Visibility) {
        case TypeMemberVisibility.Assembly:
          return this.Compilation.Result == TypeHelper.GetDefiningUnit(member.ContainingTypeDefinition);
        //TODO: friend assemblies
        case TypeMemberVisibility.Family:
          return TypeHelper.Type1DerivesFromOrIsTheSameAsType2(this.TypeDefinition, member.ContainingTypeDefinition);
        case TypeMemberVisibility.FamilyAndAssembly:
          return this.Compilation.Result == TypeHelper.GetDefiningUnit(member.ContainingTypeDefinition) &&
            TypeHelper.Type1DerivesFromOrIsTheSameAsType2(this.TypeDefinition, member.ContainingTypeDefinition);
        //TODO: friend assemblies
        case TypeMemberVisibility.FamilyOrAssembly:
          return this.Compilation.Result == TypeHelper.GetDefiningUnit(member.ContainingTypeDefinition) ||
            TypeHelper.Type1DerivesFromOrIsTheSameAsType2(this.TypeDefinition, member.ContainingTypeDefinition);
        //TODO: friend assemblies
        case TypeMemberVisibility.Public:
          return true;
        default:
          return false;
      }
    }

    /// <summary>
    /// True if the given type may be accessed from the scope defined by this block. For example, if the type is private nested type and this
    /// block is defined inside a method of the containing type of the member, the result is true.
    /// </summary>
    /// <param name="typeDefinition">The type to check.</param>
    public virtual bool CanAccess(ITypeDefinition typeDefinition) {
      if (this.TypeDefinition == typeDefinition) return true;
      if (this.TypeDefinition.IsGeneric && TypeHelper.TypesAreEquivalent(this.TypeDefinition.InstanceType, typeDefinition))
        return true;
      var nsTypeDef = typeDefinition as INamespaceTypeDefinition;
      if (nsTypeDef != null) {
        if (nsTypeDef.IsPublic) return true;
        return nsTypeDef.ContainingNamespace.RootOwner == this.Compilation.Result; //TODO: worry about /addmodule
      }
      INestedTypeDefinition/*?*/ nestedTypeDef = typeDefinition as INestedTypeDefinition;
      if (nestedTypeDef != null) return this.CanAccess((ITypeDefinitionMember)nestedTypeDef);
      IManagedPointerType/*?*/ managedPointerType = typeDefinition as IManagedPointerType;
      if (managedPointerType != null) return this.CanAccess(managedPointerType.TargetType.ResolvedType);
      IPointerType/*?*/ pointerType = typeDefinition as IPointerType;
      if (pointerType != null) return this.CanAccess(pointerType.TargetType.ResolvedType);
      IArrayType/*?*/ arrayType = typeDefinition as IArrayType;
      if (arrayType != null) return this.CanAccess(arrayType.ElementType.ResolvedType);
      IGenericTypeInstance/*?*/ genericTypeInstance = typeDefinition as IGenericTypeInstance;
      if (genericTypeInstance != null) {
        if (!this.CanAccess(genericTypeInstance.GenericType.ResolvedType)) return false;
        foreach (var typeRef in genericTypeInstance.GenericArguments) {
          if (!this.CanAccess(typeRef.ResolvedType)) return false;
        }
        return true;
      }
      IGenericTypeParameter/*?*/ genericParameter = typeDefinition as IGenericTypeParameter;
      if (genericParameter != null) return genericParameter.DefiningType == this.TypeDefinition;
      return false;
    }

    /// <summary>
    /// True if the given namespace, or one of its descendant namespaces, contains a type that can be accessed from the scope defined by this block.
    /// </summary>
    public virtual bool CanAccess(INamespaceDefinition nestedNamespaceDefinition) {
      foreach (var member in nestedNamespaceDefinition.Members) {
        var typeDef = member as ITypeDefinition;
        if (typeDef != null && this.CanAccess(typeDef)) return true;
        var nestedNs = member as INamespaceDefinition;
        if (nestedNs != null && this.CanAccess(nestedNs)) return true;
      }
      return false;
    }

    /// <summary>
    /// A map from names to resolved metadata items. Use this table for case insensitive lookup.
    /// Do not use this dictionary unless you are implementing SimpleName.ResolveUsing(TypeDeclaration typeDeclaration). 
    /// </summary>
    internal Dictionary<int, object/*?*/> caseInsensitiveCache = new Dictionary<int, object/*?*/>();
    /// <summary>
    /// A map from names to resolved metadata items. Use this table for case sensitive lookup.
    /// Do not use this dictionary unless you are implementing SimpleName.ResolveUsing(TypeDeclaration typeDeclaration). 
    /// </summary>
    internal Dictionary<int, object/*?*/> caseSensitiveCache = new Dictionary<int, object/*?*/>();

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the member or a constituent part of the member.
    /// Do not call this method directly, but evaluate the HasErrors property. The latter will cache the return value.
    /// </summary>
    protected virtual bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      bool result = false;
      foreach (var baseType in this.BaseTypes)
        result |= baseType.HasErrors;
      foreach (var member in this.TypeDeclarationMembers)
        result |= member.HasErrors;
      foreach (var attribute in this.SourceAttributes)
        result |= attribute.HasErrors;
      return result;
    }

    /// <summary>
    /// The compilation to which this type declaration belongs.
    /// </summary>
    public Compilation Compilation {
      get { return this.CompilationPart.Compilation; }
    }

    /// <summary>
    /// The compilation part to which this type declaration belongs.
    /// </summary>
    public CompilationPart CompilationPart {
      get {
        //^ assume this.compilationPart != null;
        return this.compilationPart;
      }
    }
    //^ [SpecPublic]
    CompilationPart/*?*/ compilationPart;

    /// <summary>
    /// A block that is the containing block for any expressions contained inside the type declaration
    /// but not inside of a method.
    /// </summary>
    public abstract BlockStatement DummyBlock { get; }

    /// <summary>
    /// The type parameters, if any, of this type.
    /// </summary>
    public IEnumerable<GenericTypeParameterDeclaration> GenericParameters {
      get {
        List<GenericTypeParameterDeclaration> genericParameters;
        if (this.genericParameters == null)
          yield break;
        else
          genericParameters = this.genericParameters;
        for (int i = 0, n = genericParameters.Count; i < n; i++)
          yield return genericParameters[i] = genericParameters[i].MakeCopyFor(this);
      }
    }
    readonly List<GenericTypeParameterDeclaration>/*?*/ genericParameters;

    /// <summary>
    /// The number of generic parameters. Zero if the type is not generic.
    /// </summary>
    public virtual ushort GenericParameterCount {
      get {
        return (ushort)(this.genericParameters == null ? 0 : this.genericParameters.Count);
      }
    }

    /// <summary>
    /// The visibility of type members that do not explicitly specify their visibility
    /// (their ITypeMember.Visibility values is TypeMemberVisibility.Default).
    /// </summary>
    public virtual TypeMemberVisibility GetDefaultVisibility() {
      return TypeMemberVisibility.Private;
    }

    /// <summary>
    /// Checks the class for errors and returns true if any were found.
    /// </summary>
    public bool HasErrors {
      get {
        if (this.hasErrors == null)
          this.hasErrors = this.CheckForErrorsAndReturnTrueIfAnyAreFound();
        return this.hasErrors.Value;
      }
    }
    bool? hasErrors;

    /// <summary>
    /// An instance of a language specific class containing methods that are of general utility. 
    /// </summary>
    public LanguageSpecificCompilationHelper Helper {
      get { return this.CompilationPart.Helper; }
    }

    /// <summary>
    /// If true, instances of this type will all be instances of some subtype of this type.
    /// </summary>
    public virtual bool IsAbstract {
      get {
        return (this.flags & Flags.Abstract) != 0;
      }
    }

    /// <summary>
    /// If true, this type declaration may be aggregated with other type declarations into a single type definition.
    /// </summary>
    public virtual bool IsPartial {
      get {
        return (this.flags & Flags.Partial) != 0;
      }
    }

    /// <summary>
    /// If true, this type has no subtypes.
    /// </summary>
    public virtual bool IsSealed {
      get {
        return (this.flags & Flags.Sealed) != 0;
      }
    }

    /// <summary>
    /// A static class can not have instance members. A static class is sealed.
    /// </summary>
    public virtual bool IsStatic {
      get {
        return (this.flags & Flags.Static) != 0;
      }
    }

    /// <summary>
    /// If true, this type can contain "unsafe" constructs such as pointers.
    /// </summary>
    public virtual bool IsUnsafe {
      get {
        return (this.flags & Flags.Unsafe) != 0;
      }
    }

    /// <summary>
    /// Layout of the type declaration.
    /// </summary>
    public virtual LayoutKind Layout {
      get {
        return LayoutKind.Auto; //TODO: get this from a custom attribute
      }
    }

    /// <summary>
    /// The name of the type.
    /// </summary>
    public NameDeclaration Name {
      get {
        return this.name;
      }
    }
    readonly NameDeclaration name;

    /// <summary>
    /// A block that serves as the container for expressions that are not contained inside this type declaration, but that are part of the
    /// signature of this type declaration (for example a base class reference). The block scope includes the type parameters of this type
    /// declaration.
    /// </summary>
    public abstract BlockStatement OuterDummyBlock { get; }

    /// <summary>
    /// A possibly empty collection of type members that are added by the compiler to help with the implementation of language features.
    /// </summary>
    public virtual IEnumerable<ITypeDefinitionMember> PrivateHelperMembers {
      get {
        if (this.helperMembers == null)
          yield break;
        else {
          List<ITypeDeclarationMember> helperMembers = this.helperMembers;
          // iterate with index and not foreach because the list may change while we iterate over it
          for (int i = 0; i < helperMembers.Count; i++) {
            ITypeDefinitionMember/*?*/ hmemDef = helperMembers[i].TypeDefinitionMember;
            if (hmemDef != null) yield return hmemDef;
          }
        }
      }
    }
    List<ITypeDeclarationMember>/*?*/ helperMembers;

    /// <summary>
    /// A collection of metadata declarative security attributes that are associated with this type.
    /// </summary>
    public IEnumerable<ISecurityAttribute> SecurityAttributes {
      get { return Enumerable<ISecurityAttribute>.Empty; } //TODO: extract these from the source attributes
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="compilationPart"></param>
    /// <param name="recurse"></param>
    public virtual void SetCompilationPart(CompilationPart compilationPart, bool recurse) {
      this.compilationPart = compilationPart;
      if (!recurse) return;
      BlockStatement dummyBlock = this.OuterDummyBlock;
      DummyExpression containingExpression = new DummyExpression(dummyBlock, SourceDummy.SourceLocation);
      if (this.sourceAttributes != null)
        foreach (SourceCustomAttribute attribute in this.sourceAttributes) attribute.SetContainingExpression(containingExpression);
      if (this.genericParameters != null)
        foreach (GenericTypeParameterDeclaration genericParameter in this.genericParameters) genericParameter.SetContainingExpression(containingExpression);
      foreach (TypeExpression baseType in this.baseTypes) baseType.SetContainingExpression(containingExpression);
      foreach (ITypeDeclarationMember member in this.typeDeclarationMembers) this.SetMemberContainingTypeDeclaration(member);
      TypeContract/*?*/ typeContract = this.Compilation.ContractProvider.GetTypeContractFor(this) as TypeContract;
      if (typeContract != null) typeContract.SetContainingType(this);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="member"></param>
    public virtual void SetMemberContainingTypeDeclaration(ITypeDeclarationMember member) {
      TypeDeclarationMember/*?*/ tmem = member as TypeDeclarationMember;
      if (tmem != null) {
        tmem.SetContainingTypeDeclaration(this, true);
        return;
      }
      NestedTypeDeclaration/*?*/ ntdecl = member as NestedTypeDeclaration;
      if (ntdecl != null) {
        ntdecl.SetContainingTypeDeclaration(this, true);
        return;
      }
    }

    /// <summary>
    /// Size of an object of this type. In bytes. If zero, the size is unspecified and will be determined at runtime.
    /// </summary>
    public virtual uint SizeOf {
      get {
        //TODO: run through the attributes and see if one of them specifies the size of the type.
        return 0;
      }
    }

    /// <summary>
    /// Custom attributes that are explicitly specified in source. Some of these may not end up in persisted metadata.
    /// </summary>
    /// <value></value>
    public IEnumerable<SourceCustomAttribute> SourceAttributes {
      get {
        List<SourceCustomAttribute> sourceAttributes;
        if (this.sourceAttributes == null)
          yield break;
        else
          sourceAttributes = this.sourceAttributes;
        for (int i = 0, n = sourceAttributes.Count; i < n; i++)
          yield return sourceAttributes[i] = sourceAttributes[i].MakeShallowCopyFor(this.OuterDummyBlock);
      }
    }
    readonly List<SourceCustomAttribute>/*?*/ sourceAttributes;

    //^ [Confined]
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public override string ToString() {
      return this.Helper.GetTypeName(this.TypeDefinition);
    }

    /// <summary>
    /// The collection of things that are considered members of this type. For example: events, fields, method, properties and nested types.
    /// </summary>
    public IEnumerable<ITypeDeclarationMember> TypeDeclarationMembers {
      get {
        for (int i = 0, n = this.typeDeclarationMembers.Count; i < n; i++)
          yield return this.typeDeclarationMembers[i] = this.typeDeclarationMembers[i].MakeShallowCopyFor(this);
      }
    }
    readonly List<ITypeDeclarationMember> typeDeclarationMembers;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="uniqueKey"></param>
    /// <returns></returns>
    public IEnumerable<ITypeDeclarationMember> GetTypeDeclarationMembersNamed(int uniqueKey) {
      this.InitializeIfNecessary();
      List<ITypeDeclarationMember> members;
      if (this.caseSensitiveMemberNameToMemberListMap.TryGetValue(uniqueKey, out members)) {
        List<ITypeDeclarationMember> result = new List<ITypeDeclarationMember>(members.Count);
        foreach (var member in members)
          result.Add(member.MakeShallowCopyFor(this));
        return result;
      } else {
        return emptyMemberList;
      }
    }

    private static readonly List<ITypeDeclarationMember> emptyMemberList = new List<ITypeDeclarationMember>(0);

    private void InitializeIfNecessary() {
      if (this.caseSensitiveMemberNameToMemberListMap == null) {
        this.caseSensitiveMemberNameToMemberListMap = new Dictionary<int, List<ITypeDeclarationMember>>();
        foreach (var member in this.typeDeclarationMembers) {
          List<ITypeDeclarationMember> membersNamed;
          if (!this.caseSensitiveMemberNameToMemberListMap.TryGetValue(member.Name.UniqueKey, out membersNamed)) {
            membersNamed = new List<ITypeDeclarationMember>();
            this.caseSensitiveMemberNameToMemberListMap[member.Name.UniqueKey] = membersNamed;
          }
          membersNamed.Add(member);
        }
      }
    }

    private Dictionary<int, List<ITypeDeclarationMember>> caseSensitiveMemberNameToMemberListMap = null;

    /// <summary>
    /// The symbol table type definition that corresponds to this type declaration. If this type declaration is a partial type, the symbol table type
    /// will be an aggregate of multiple type declarations.
    /// </summary>
    public abstract NamedTypeDefinition TypeDefinition {
      get;
    }

    /// <summary>
    /// Returns a shallow copy of this type declaration that has the specified list of members as the value of its Members property.
    /// </summary>
    /// <param name="members">The members of the new type declaration.</param>
    /// <param name="edit">The edit that resulted in this update.</param>
    public abstract TypeDeclaration UpdateMembers(List<ITypeDeclarationMember> members, ISourceDocumentEdit edit);
    //^ requires edit.SourceDocumentAfterEdit.IsUpdatedVersionOf(this.SourceLocation.SourceDocument);
    //^ ensures result.GetType() == this.GetType();

    #region IContainer<IAggregatableTypeDeclarationMember> Members

    IEnumerable<IAggregatableTypeDeclarationMember> IContainer<IAggregatableTypeDeclarationMember>.Members {
      get {
        return IteratorHelper.GetFilterEnumerable<ITypeDeclarationMember, IAggregatableTypeDeclarationMember>(this.TypeDeclarationMembers);
      }
    }

    #endregion

    #region IContainer<ITypeDeclarationMember> Members

    IEnumerable<ITypeDeclarationMember> IContainer<ITypeDeclarationMember>.Members {
      get {
        return this.TypeDeclarationMembers;
      }
    }

    #endregion

    #region INamedEntity Members

    IName INamedEntity.Name {
      get { return this.Name; }
    }

    #endregion
  }

}
