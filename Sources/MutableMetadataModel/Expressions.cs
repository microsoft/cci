//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci.MutableCodeModel {

  public sealed class MetadataConstant : MetadataExpression, IMetadataConstant, ICopyFrom<IMetadataConstant> {

    public MetadataConstant() {
      this.value = null;
    }

    public void Copy(IMetadataConstant metadataConstant, IInternFactory internFactory) {
      ((ICopyFrom<IMetadataExpression>)this).Copy(metadataConstant, internFactory);
      this.value = metadataConstant.Value;
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public object Value {
      get { return this.value; }
      set { this.value = value; }
    }
    object value;


  }

  public sealed class MetadataCreateArray : MetadataExpression, IMetadataCreateArray, ICopyFrom<IMetadataCreateArray> {

    public MetadataCreateArray() {
      this.elementType = Dummy.TypeReference;
      this.initializers = new List<IMetadataExpression>();
      this.lowerBounds = new List<int>();
      this.rank = 0;
      this.sizes = new List<ulong>();
    }

    public void Copy(IMetadataCreateArray createArray, IInternFactory internFactory) {
      ((ICopyFrom<IMetadataExpression>)this).Copy(createArray, internFactory);
      this.elementType = createArray.ElementType;
      this.initializers = new List<IMetadataExpression>(createArray.Initializers);
      this.lowerBounds = new List<int>(createArray.LowerBounds);
      this.rank = createArray.Rank;
      this.sizes = new List<ulong>(createArray.Sizes);
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public ITypeReference ElementType {
      get { return this.elementType; }
      set { this.elementType = value; }
    }
    ITypeReference elementType;

    public List<IMetadataExpression> Initializers {
      get { return this.initializers; }
      set { this.initializers = value; }
    }
    List<IMetadataExpression> initializers;

    public List<int> LowerBounds {
      get { return this.lowerBounds; }
      set { this.lowerBounds = value; }
    }
    List<int> lowerBounds;

    public uint Rank {
      get { return this.rank; }
      set { this.rank = value; }
    }
    uint rank;

    public List<ulong> Sizes {
      get { return this.sizes; }
      set { this.sizes = value; }
    }
    List<ulong> sizes;


    #region IMetadataCreateArray Members


    IEnumerable<IMetadataExpression> IMetadataCreateArray.Initializers {
      get { return this.initializers.AsReadOnly(); }
    }

    IEnumerable<int> IMetadataCreateArray.LowerBounds {
      get { return this.lowerBounds.AsReadOnly(); }
    }

    IEnumerable<ulong> IMetadataCreateArray.Sizes {
      get { return this.sizes.AsReadOnly(); }
    }

    #endregion
  }

  public abstract class MetadataExpression : IMetadataExpression, ICopyFrom<IMetadataExpression> {

    internal MetadataExpression() {
      this.locations = new List<ILocation>();
      this.type = Dummy.TypeReference;
    }

    public void Copy(IMetadataExpression metadataExpression, IInternFactory internFactory) {
      this.locations = new List<ILocation>(metadataExpression.Locations);
      this.type = metadataExpression.Type;
    }


    public abstract void Dispatch(IMetadataVisitor visitor);

    public bool HasErrors() { 
      return false; 
    }

    public List<ILocation> Locations {
      get { return this.locations; }
      set { this.locations = value; }
    }
    List<ILocation> locations;

    public ITypeReference Type {
      get { return this.type; }
      set { this.type = value; }
    }
    ITypeReference type;

    #region IMetadataExpression Members


    IEnumerable<ILocation> IMetadataExpression.Locations {
      get { return this.locations.AsReadOnly(); }
    }

    #endregion
  }

  public sealed class MetadataNamedArgument : MetadataExpression, IMetadataNamedArgument, ICopyFrom<IMetadataNamedArgument> {

    public MetadataNamedArgument() {
      this.argumentName = Dummy.Name;
      this.argumentValue = Dummy.Expression;
      this.isField = false;
      this.resolvedDefinition = null;
    }

    public void Copy(IMetadataNamedArgument namedArgument, IInternFactory internFactory) {
      ((ICopyFrom<IMetadataExpression>)this).Copy(namedArgument, internFactory);
      this.argumentName = namedArgument.ArgumentName;
      this.argumentValue = namedArgument.ArgumentValue;
      this.isField = namedArgument.IsField;
      this.resolvedDefinition = namedArgument.ResolvedDefinition;
    }

    public IName ArgumentName {
      get { return this.argumentName; }
      set { this.argumentName = value; }
    }
    IName argumentName;

    public IMetadataExpression ArgumentValue {
      get { return this.argumentValue; }
      set { this.argumentValue = value; }
    }
    IMetadataExpression argumentValue;

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public bool IsField {
      get { return this.isField; }
      set { this.isField = value; }
    }
    bool isField;

    public object/*?*/ ResolvedDefinition {
      get { return this.resolvedDefinition; }
      set { this.resolvedDefinition = value; }
    }
    object/*?*/ resolvedDefinition;

  }

  public sealed class MetadataTypeOf : MetadataExpression, IMetadataTypeOf, ICopyFrom<IMetadataTypeOf> {

    public MetadataTypeOf() {
      this.typeToGet = Dummy.TypeReference;
    }

    public void Copy(IMetadataTypeOf typeOf, IInternFactory internFactory) {
      ((ICopyFrom<IMetadataExpression>)this).Copy(typeOf, internFactory);
      this.typeToGet = typeOf.TypeToGet;
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public ITypeReference TypeToGet {
      get { return this.typeToGet; }
      set { this.typeToGet = value; }
    }
    ITypeReference typeToGet;

  }

}