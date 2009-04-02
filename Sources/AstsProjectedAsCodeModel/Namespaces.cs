//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System.Collections.Generic;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci.Ast {

  public class NestedUnitNamespace : UnitNamespace, INestedUnitNamespace {

    public NestedUnitNamespace(IUnitNamespace containingUnitNamespace, IName name, IUnit unit)
      : base(name, unit)
      //^ requires containingUnitNamespace is RootUnitNamespace || containingUnitNamespace is NestedUnitNamespace;
    {
      this.containingUnitNamespace = containingUnitNamespace;
    }

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

    public IScope<INamespaceMember> ContainingScope {
      get { return this.ContainingUnitNamespace; }
    }

    #endregion

    #region IContainerMember<INamespace> Members

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

    public INestedUnitNamespace ResolvedNestedUnitNamespace {
      get { return this; }
    }

    #endregion
  }

  public abstract class UnitNamespace : AggregatedNamespace<NamespaceDeclaration, IAggregatableNamespaceDeclarationMember>, IUnitNamespace {

    protected UnitNamespace(IName name, IUnit unit)
      : base(name) {
      this.unit = unit;
    }

    protected internal void AddNamespaceDeclaration(NamespaceDeclaration declaration) {
      this.namespaceDeclarations.Add(declaration);
      this.AddContainer(declaration);
    }

    /// <summary>
    /// A collection of metadata custom attributes that are associated with this definition.
    /// </summary>
    public override IEnumerable<ICustomAttribute> Attributes {
      get {
        foreach (NamespaceDeclaration nsDecl in this.namespaceDeclarations) {
          foreach (SourceCustomAttribute attr in nsDecl.SourceAttributes)
            yield return new CustomAttribute(attr);
        }
      }
    }

    public IEnumerable<NamespaceDeclaration> NamespaceDeclarations {
      get {
        return this.namespaceDeclarations.AsReadOnly();
      }
    }
    List<NamespaceDeclaration> namespaceDeclarations = new List<NamespaceDeclaration>();

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

    private static void FillInWithNestedTypes(INamedTypeDefinition typeDefinition, List<INamedTypeDefinition> typeList) {
      typeList.Add(typeDefinition);
      foreach (ITypeDefinitionMember member in typeDefinition.Members) {
        INamedTypeDefinition/*?*/ nestedType = member as INamedTypeDefinition;
        if (nestedType != null)
          FillInWithNestedTypes(nestedType, typeList);
      }
    }

    protected override INamespaceMember GetAggregatedMember(IAggregatableNamespaceDeclarationMember member) {
      return member.AggregatedMember;
    }

    #region IUnitNamespace Members

    public IUnit Unit {
      get { return this.unit; }
    }
    IUnit unit;

    #endregion

    #region INamespace Members

    public sealed override INamespaceRootOwner RootOwner {
      get { return this.unit; }
    }

    #endregion

    #region IDefinition Members

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

    public IUnitNamespace ResolvedUnitNamespace {
      get { return this; }
    }

    #endregion
  }

  public class RootUnitNamespace : UnitNamespace, IRootUnitNamespace {

    public RootUnitNamespace(IName name, IUnit unit)
      : base(name, unit) {
    }

    protected override void AddContainer(NamespaceDeclaration container) {
      base.AddContainer(container);
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public override string ToString() {
      return "global::";
    }

  }

}
