//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci.MutableCodeModel {

  public sealed class NestedUnitNamespace : UnitNamespace, INestedUnitNamespace, ICopyFrom<INestedUnitNamespace> {

    public NestedUnitNamespace() {
      this.containingUnitNamespace = Dummy.RootUnitNamespace;
    }

    public void Copy(INestedUnitNamespace nestedUnitNamespace, IInternFactory internFactory) {
      ((ICopyFrom<IUnitNamespace>)this).Copy(nestedUnitNamespace, internFactory);
      this.containingUnitNamespace = nestedUnitNamespace.ContainingUnitNamespace;
    }

    public IUnitNamespace ContainingUnitNamespace {
      get { return this.containingUnitNamespace; }
      set { this.containingUnitNamespace = value; }
    }
    IUnitNamespace containingUnitNamespace;

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
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


    #region INestedUnitNamespaceReference Members

    IUnitNamespaceReference INestedUnitNamespaceReference.ContainingUnitNamespace {
      get { return this.ContainingUnitNamespace; }
    }

    INestedUnitNamespace INestedUnitNamespaceReference.ResolvedNestedUnitNamespace {
      get { return this; }
    }

    #endregion
  }

  public sealed class NestedUnitNamespaceReference : UnitNamespaceReference, INestedUnitNamespaceReference, ICopyFrom<INestedUnitNamespaceReference> {

    public NestedUnitNamespaceReference() {
      this.containingUnitNamespace = Dummy.RootUnitNamespace;
      this.name = Dummy.Name;
    }

    public void Copy(INestedUnitNamespaceReference nestedUnitNamespaceReference, IInternFactory internFactory) {
      ((ICopyFrom<IUnitNamespaceReference>)this).Copy(nestedUnitNamespaceReference, internFactory);
      this.containingUnitNamespace = nestedUnitNamespaceReference.ContainingUnitNamespace;
      this.name = nestedUnitNamespaceReference.Name;
    }

    public IUnitNamespaceReference ContainingUnitNamespace {
      get { return this.containingUnitNamespace; }
      set { this.containingUnitNamespace = value; this.resolvedNestedUnitNamespace = null; }
    }
    IUnitNamespaceReference containingUnitNamespace;

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    internal override IUnitReference GetUnit() {
      return this.Unit;
    }

    public IName Name {
      get { return this.name; }
      set { this.name = value; this.resolvedNestedUnitNamespace = null; }
    }
    IName name;

    private INestedUnitNamespace Resolve() {
      foreach (INamespaceMember member in this.containingUnitNamespace.ResolvedUnitNamespace.GetMembersNamed(this.Name, false)) {
        INestedUnitNamespace/*?*/ ns = member as INestedUnitNamespace;
        if (ns != null) return ns;
      }
      return Dummy.NestedUnitNamespace;
    }

    public INestedUnitNamespace ResolvedNestedUnitNamespace {
      get {
        if (this.resolvedNestedUnitNamespace == null)
          this.resolvedNestedUnitNamespace = this.Resolve();
        return this.resolvedNestedUnitNamespace;
      }
    }
    INestedUnitNamespace/*?*/ resolvedNestedUnitNamespace;

    public override IUnitNamespace ResolvedUnitNamespace {
      get { return this.ResolvedNestedUnitNamespace; }
    }

    public IUnitReference Unit {
      get { return this.containingUnitNamespace.Unit; }
    }


  }

  public sealed class RootUnitNamespace : UnitNamespace, IRootUnitNamespace, ICopyFrom<IRootUnitNamespace> {

    public RootUnitNamespace() {
    }

    public void Copy(IRootUnitNamespace rootUnitNamespace, IInternFactory internFactory) {
      ((ICopyFrom<IUnitNamespace>)this).Copy(rootUnitNamespace, internFactory);
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

  }

  public sealed class RootUnitNamespaceReference : UnitNamespaceReference, IRootUnitNamespaceReference, ICopyFrom<IRootUnitNamespaceReference> {

    public RootUnitNamespaceReference() {
      this.unit = Dummy.Unit;
    }

    public void Copy(IRootUnitNamespaceReference rootUnitNamespaceReference, IInternFactory internFactory) {
      ((ICopyFrom<IUnitNamespaceReference>)this).Copy(rootUnitNamespaceReference, internFactory);
      this.unit = rootUnitNamespaceReference.Unit;
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    internal override IUnitReference GetUnit() {
      return this.Unit;
    }

    public override IUnitNamespace ResolvedUnitNamespace {
      get { return this.Unit.ResolvedUnit.UnitNamespaceRoot; }
    }

    public IUnitReference Unit {
      get { return this.unit; }
      set { this.unit = value; }
    }
    IUnitReference unit;

  }

  public abstract class UnitNamespace : IUnitNamespace, ICopyFrom<IUnitNamespace> {

    internal UnitNamespace() {
      this.attributes = new List<ICustomAttribute>();
      this.locations = new List<ILocation>(1);
      this.members = new List<INamespaceMember>();
      this.name = Dummy.Name;
      this.unit = Dummy.Unit;
    }

    public virtual void Copy(IUnitNamespace unitNamespace, IInternFactory internFactory) {
      this.attributes = new List<ICustomAttribute>(unitNamespace.Attributes);
      this.locations = new List<ILocation>(unitNamespace.Locations);
      this.members = new List<INamespaceMember>(unitNamespace.Members);
      this.name = unitNamespace.Name;
      this.unit = unitNamespace.Unit;
    }

    public List<ICustomAttribute> Attributes {
      get { return this.attributes; }
      set { this.attributes = value; }
    }
    List<ICustomAttribute> attributes;

    //^ [Pure]
    public bool Contains(INamespaceMember member) {
      foreach (INamespaceMember nsmem in this.Members)
        if (member == nsmem) return true;
      return false;
    }

    public abstract void Dispatch(IMetadataVisitor visitor);

    //^ [Pure]
    public IEnumerable<INamespaceMember> GetMatchingMembersNamed(IName name, bool ignoreCase, Function<INamespaceMember, bool> predicate) {
      foreach (INamespaceMember nsmem in this.Members) {
        if (nsmem.Name.UniqueKey == name.UniqueKey || ignoreCase && (name.UniqueKeyIgnoringCase == nsmem.Name.UniqueKeyIgnoringCase)) {
          if (predicate(nsmem)) yield return nsmem;
        }
      }
    }

    //^ [Pure]
    public IEnumerable<INamespaceMember> GetMatchingMembers(Function<INamespaceMember, bool> predicate) {
      foreach (INamespaceMember nsmem in this.Members) {
        if (predicate(nsmem)) yield return nsmem;
      }
    }

    //^ [Pure]
    public IEnumerable<INamespaceMember> GetMembersNamed(IName name, bool ignoreCase) {
      foreach (INamespaceMember nsmem in this.Members) {
        if (nsmem.Name.UniqueKey == name.UniqueKey || ignoreCase && (name.UniqueKeyIgnoringCase == nsmem.Name.UniqueKeyIgnoringCase)) {
          yield return nsmem;
        }
      }
    }

    public List<ILocation> Locations {
      get { return this.locations; }
      set { this.locations = value; }
    }
    List<ILocation> locations;

    public List<INamespaceMember> Members {
      get { return this.members; }
      set { this.members = value; }
    }
    List<INamespaceMember> members;

    public IName Name {
      get { return this.name; }
      set { this.name = value; }
    }
    IName name;

    public IUnit Unit {
      get { return this.unit; }
      set { this.unit = value; }
    }
    IUnit unit;

    #region INamespaceDefinition Members

    INamespaceRootOwner INamespaceDefinition.RootOwner {
      get { return this.Unit; }
    }

    #endregion

    #region INamespaceDefinition Members


    IEnumerable<INamespaceMember> INamespaceDefinition.Members {
      get { return this.members.AsReadOnly(); }
    }

    #endregion

    #region IContainer<INamespaceMember> Members

    IEnumerable<INamespaceMember> IContainer<INamespaceMember>.Members {
      get { return this.members.AsReadOnly(); }
    }

    #endregion

    #region IReference Members

    IEnumerable<ICustomAttribute> IReference.Attributes {
      get { return this.attributes.AsReadOnly(); }
    }

    IEnumerable<ILocation> IObjectWithLocations.Locations {
      get { return this.locations.AsReadOnly(); }
    }

    #endregion

    #region IScope<INamespaceMember> Members

    IEnumerable<INamespaceMember> IScope<INamespaceMember>.Members {
      get { return this.members.AsReadOnly(); }
    }

    #endregion

    #region IUnitNamespaceReference Members

    IUnitReference IUnitNamespaceReference.Unit {
      get { return this.Unit; }
    }

    IUnitNamespace IUnitNamespaceReference.ResolvedUnitNamespace {
      get { return this; }
    }

    #endregion
  }

  public abstract class UnitNamespaceReference : IUnitNamespaceReference, ICopyFrom<IUnitNamespaceReference> {

    internal UnitNamespaceReference() {
      this.attributes = new List<ICustomAttribute>();
      this.locations = new List<ILocation>();
    }

    public virtual void Copy(IUnitNamespaceReference unitNamespaceReference, IInternFactory internFactory) {
      this.attributes = new List<ICustomAttribute>(unitNamespaceReference.Attributes);
      this.locations = new List<ILocation>(unitNamespaceReference.Locations);
    }

    public List<ICustomAttribute> Attributes {
      get { return this.attributes; }
      set { this.attributes = value; }
    }
    List<ICustomAttribute> attributes;

    public abstract void Dispatch(IMetadataVisitor visitor);

    public List<ILocation> Locations {
      get { return this.locations; }
      set { this.locations = value; }
    }
    List<ILocation> locations;

    public abstract IUnitNamespace ResolvedUnitNamespace {
      get;
    }

    internal abstract IUnitReference GetUnit();

    IUnitReference IUnitNamespaceReference.Unit {
      get { return this.GetUnit(); }
    }

    #region IReference Members

    IEnumerable<ICustomAttribute> IReference.Attributes {
      get { return this.attributes.AsReadOnly(); }
    }

    IEnumerable<ILocation> IObjectWithLocations.Locations {
      get { return this.locations.AsReadOnly(); }
    }

    #endregion
  }

}
