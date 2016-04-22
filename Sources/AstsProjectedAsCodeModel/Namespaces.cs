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
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Cci.Immutable;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci.Ast {

  /// <summary>
  /// 
  /// </summary>
  public class NestedUnitNamespace : UnitNamespace, INestedUnitNamespace {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="containingUnitNamespace"></param>
    /// <param name="name"></param>
    /// <param name="unit"></param>
    public NestedUnitNamespace(IUnitNamespace containingUnitNamespace, IName name, IUnit unit)
      : base(name, unit)
      //^ requires containingUnitNamespace is RootUnitNamespace || containingUnitNamespace is NestedUnitNamespace;
    {
      this.containingUnitNamespace = containingUnitNamespace;
    }

    /// <summary>
    /// The unit namespace that contains this member.
    /// </summary>
    /// <value></value>
    public IUnitNamespace ContainingUnitNamespace {
      get
        //^ ensures result is RootUnitNamespace || result is NestedUnitNamespace;
      {
        return this.containingUnitNamespace;
      }
    }
    readonly IUnitNamespace containingUnitNamespace;
    //^ invariant containingUnitNamespace is RootUnitNamespace || containingUnitNamespace is NestedUnitNamespace;

    /// <summary>
    /// Calls the visitor.Visit(INestedUnitNamespace) method.
    /// </summary>
    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls visitor.Visit(INestedUnitNamespaceReference).
    /// </summary>
    public override void DispatchAsReference(IMetadataVisitor visitor) {
      visitor.Visit((INestedUnitNamespaceReference)this);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public override string ToString() {
      if (this.ContainingUnitNamespace is IRootUnitNamespace) return this.Name.Value;
      return this.ContainingUnitNamespace.ToString() + "." + this.Name.Value;
    }

    #region INamespaceMember Members

    INamespaceDefinition INamespaceMember.ContainingNamespace {
      get { return this.ContainingUnitNamespace; }
    }

    #endregion

    #region IScopeMember<IScope<INamespaceMember>> Members

    /// <summary>
    /// The scope instance with a Members collection that includes this instance.
    /// </summary>
    /// <value></value>
    public IScope<INamespaceMember> ContainingScope {
      get { return this.ContainingUnitNamespace; }
    }

    #endregion

    #region IContainerMember<INamespace> Members

    /// <summary>
    /// The container instance with a Members collection that includes this instance.
    /// </summary>
    /// <value></value>
    public INamespaceDefinition Container {
      get { return this.ContainingUnitNamespace; }
    }

    #endregion

    #region IUnitNamespaceReference Members

    IUnitReference IUnitNamespaceReference.Unit {
      get { return this.Unit; }
    }

    #endregion

    #region INestedUnitNamespaceReference Members

    IUnitNamespaceReference INestedUnitNamespaceReference.ContainingUnitNamespace {
      get { return this.ContainingUnitNamespace; }
    }

    /// <summary>
    /// The namespace definition being referred to.
    /// </summary>
    /// <value></value>
    public INestedUnitNamespace ResolvedNestedUnitNamespace {
      get { return this; }
    }

    #endregion
  }

  /// <summary>
  /// A named collection of namespace members, with routines to search and maintain the collection. All the members belong to an associated
  /// IUnit instance.
  /// </summary>
  public abstract class UnitNamespace : AggregatedNamespace<NamespaceDeclaration, IAggregatableNamespaceDeclarationMember>, IUnitNamespace {

    /// <summary>
    /// Allocates a named collection of namespace members, with routines to search and maintain the collection. All the members belong to an associated
    /// IUnit instance.
    /// </summary>
    /// <param name="name">The name of the namespace.</param>
    /// <param name="unit">The IUnit instance associated with this namespace.</param>
    protected UnitNamespace(IName name, IUnit unit)
      : base(name) {
      this.unit = unit;
    }

    /// <summary>
    /// The members of this namespace definition are the aggregation of the members of zero or more namespace declarations. This
    /// method adds another namespace declaration of the list of declarations that supply the members of this namespace definition.
    /// </summary>
    /// <param name="declaration">The namespace declaration to add to the list of declarations that together define this namespace definition.</param>
    protected internal void AddNamespaceDeclaration(NamespaceDeclaration declaration) {
      this.namespaceDeclarations.Add(declaration);
      this.AddContainer(declaration);
    }

    /// <summary>
    /// A collection of metadata custom attributes that are associated with this definition.
    /// </summary>
    public sealed override IEnumerable<ICustomAttribute> Attributes {
      get {
        if (this.attributes == null) {
          var attributes = this.GetAttributes();
          attributes.TrimExcess();
          this.attributes = attributes.AsReadOnly();
        }
        return this.attributes;
      }
    }
    IEnumerable<ICustomAttribute>/*?*/ attributes;

    /// <summary>
    /// The list of namespace declarations that together define this namespace definition.
    /// </summary>
    public IEnumerable<NamespaceDeclaration> NamespaceDeclarations {
      get {
        return this.namespaceDeclarations.AsReadOnly();
      }
    }
    List<NamespaceDeclaration> namespaceDeclarations = new List<NamespaceDeclaration>();

    /// <summary>
    /// Populate the given list with all of the assembly attributes in this namespace and its nested namespaces.
    /// If sawTypeExtensions is false, also look for any types in this namespace and its nested namespaces that
    /// have the HasExtensionMethod property set to true and set sawTypeExtensions to true if such a type is found.
    /// </summary>
    /// <param name="attributes">A list to fill in with the assembly attributes in this namespace.</param>
    /// <param name="sawTypeWithExtensions">If sawTypeExtensions is false, also look for any types in this namespace and its nested namespaces that
    /// have the HasExtensionMethod property set to true and set sawTypeExtensions to true if such a type is found.</param>
    public void FillInWithAssemblyAttributes(List<ICustomAttribute> attributes, ref bool sawTypeWithExtensions) {
      foreach (var nsDecl in this.namespaceDeclarations) {
        foreach (var attribute in nsDecl.SourceAttributes) {
          if (attribute.HasErrors) continue;
          if ((attribute.Targets & System.AttributeTargets.Assembly) != System.AttributeTargets.Assembly) continue;
          attributes.Add(new CustomAttribute(attribute));
        }
      }
      foreach (var member in this.Members) {
        var unitNamespace = member as UnitNamespace;
        if (unitNamespace != null)
          unitNamespace.FillInWithAssemblyAttributes(attributes, ref sawTypeWithExtensions);
        else if (!sawTypeWithExtensions) {
          var type = member as NamespaceTypeDefinition;
          if (type != null && type.HasExtensionMethod) {
            sawTypeWithExtensions = true;
            break;
          }
        }
      }
    }

    /// <summary>
    /// Populate the given list with all of the module attributes in this namespace and its nested namespaces.
    /// </summary>
    /// <param name="attributes">A list to fill in with the module attributes in this namespace.</param>
    public void FillInWithModuleAttributes(List<ICustomAttribute> attributes) {
      foreach (var nsDecl in this.namespaceDeclarations) {
        foreach (var attribute in nsDecl.SourceAttributes) {
          if (attribute.HasErrors) continue;
          if ((attribute.Targets & System.AttributeTargets.Module) != System.AttributeTargets.Module) continue;
          attributes.Add(new CustomAttribute(attribute));
        }
      }
      foreach (var member in this.Members) {
        var unitNamespace = member as UnitNamespace;
        if (unitNamespace == null) continue;
        unitNamespace.FillInWithModuleAttributes(attributes);
      }
    }

    /// <summary>
    /// Appends all of the type members of the given namespace (as well as those of its nested namespaces and types) to
    /// the given list of types.
    /// </summary>
    /// <param name="nameSpace">The namespace to (recursively) traverse to find nested types.</param>
    /// <param name="typeList">A mutable list of types to which any types found inside the given namespace will be appended.</param>
    internal static void FillInWithTypes(INamespaceDefinition nameSpace, List<INamedTypeDefinition> typeList) {
      foreach (INamespaceMember member in nameSpace.Members) {
        INamedTypeDefinition/*?*/ type = member as INamedTypeDefinition;
        if (type != null)
          FillInWithNestedTypes(type, typeList);
        else {
          INamespaceDefinition/*?*/ ns = member as INamespaceDefinition;
          if (ns != null)
            FillInWithTypes(ns, typeList);
        }
      }
    }

    /// <summary>
    /// Appends all of the type members of the given type definition (as well as those of its nested types) to the given list of types.
    /// </summary>
    /// <param name="typeDefinition">The type definition to (recursively) traverse to find nested types.</param>
    /// <param name="typeList">A mutable list of types to which any types found inside the given namespace will be appended.</param>
    private static void FillInWithNestedTypes(INamedTypeDefinition typeDefinition, List<INamedTypeDefinition> typeList) {
      typeList.Add(typeDefinition);
      foreach (ITypeDefinitionMember member in typeDefinition.Members) {
        INamedTypeDefinition/*?*/ nestedType = member as INamedTypeDefinition;
        if (nestedType != null)
          FillInWithNestedTypes(nestedType, typeList);
      }
    }

    /// <summary>
    /// Finds or creates an aggregated member instance corresponding to the given member. Usually this should result in the given member being added to the declarations
    /// collection of the aggregated member.
    /// </summary>
    /// <param name="member">The member to aggregate.</param>
    protected override INamespaceMember GetAggregatedMember(IAggregatableNamespaceDeclarationMember member) {
      return member.AggregatedMember;
    }

    /// <summary>
    /// Returns a list of custom attributes that describes this type declaration member.
    /// Typically, these will be derived from this.SourceAttributes. However, some source attributes
    /// might instead be persisted as metadata bits and other custom attributes may be synthesized
    /// from information not provided in the form of source custom attributes.
    /// The list is not trimmed to size, since an override of this method may call the base method
    /// and then add more attributes.
    /// </summary>
    protected virtual List<ICustomAttribute> GetAttributes() {
      List<ICustomAttribute> result = new List<ICustomAttribute>();
      foreach (var nsDecl in this.namespaceDeclarations)
        result.AddRange(nsDecl.Attributes);
      return result;
    }

    #region IUnitNamespace Members

    /// <summary>
    /// The IUnit instance associated with this namespace.
    /// </summary>
    public IUnit Unit {
      get { return this.unit; }
    }
    IUnit unit;

    #endregion

    #region INamespace Members

    /// <summary>
    /// The object associated with the namespace. For example an IUnit or IUnitSet instance. This namespace is either the root namespace of that object
    /// or it is a nested namespace that is directly of indirectly nested in the root namespace.
    /// </summary>
    public sealed override INamespaceRootOwner RootOwner {
      get { return this.unit; }
    }

    #endregion

    #region IDefinition Members

    /// <summary>
    /// The source locations of the zero or more namespace declarations that together define this namespace definition.
    /// </summary>
    public sealed override IEnumerable<ILocation> Locations {
      get {
        foreach (NamespaceDeclaration declaration in this.namespaceDeclarations)
          yield return declaration.SourceLocation;
      }
    }

    #endregion

    #region IUnitNamespaceReference Members

    IUnitReference IUnitNamespaceReference.Unit {
      get { return this.unit; }
    }

    /// <summary>
    /// The namespace definition being referred to.
    /// </summary>
    /// <value></value>
    public IUnitNamespace ResolvedUnitNamespace {
      get { return this; }
    }

    #endregion
  }

  /// <summary>
  /// A unit namespace that is not nested inside another namespace.
  /// </summary>
  public class RootUnitNamespace : UnitNamespace, IRootUnitNamespace {

    /// <summary>
    /// Allocates a unit namespace that is not nested inside another namespace.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="unit"></param>
    public RootUnitNamespace(IName name, IUnit unit)
      : base(name, unit) {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="container"></param>
    protected override void AddContainer(NamespaceDeclaration container) {
      base.AddContainer(container);
    }

    /// <summary>
    /// Calls visitor.Visit(IRootUnitNamespace).
    /// </summary>
    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls visitor.Visit(IRootUnitNamespaceReference).
    /// </summary>
    public override void DispatchAsReference(IMetadataVisitor visitor) {
      visitor.Visit((IRootUnitNamespaceReference)this);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public override string ToString() {
      return "global::";
    }

  }

}
