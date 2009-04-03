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

    protected Expression(IExpression expression) {
      this.locations = new List<ILocation>(expression.Locations);
      this.type = expression.Type;
    }

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

    public bool IsPure {
      get { return false; }
    }

    public List<ILocation> Locations {
      get { return this.locations; }
      set { this.locations = value; }
    }
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

    public override void ConvertToIL(IMethodDefinition method, IBlockStatement body) {
      MethodBodyNormalizer normalizer = new MethodBodyNormalizer(this.host, null, ProvideSourceToILConverter,
        this.sourceLocationProvider, (ContractProvider)this.contractProvider);
      ISourceMethodBody normalizedBody = normalizer.GetNormalizedSourceMethodBodyFor(method, body);
      this.privateHelperTypes = normalizedBody.PrivateHelperTypes;
      base.Visit(normalizedBody);
    }

    public override IEnumerable<ITypeDefinition> GetPrivateHelperTypes() {
      return this.privateHelperTypes;
    }

    IEnumerable<ITypeDefinition> privateHelperTypes = IteratorHelper.GetEmptyEnumerable<ITypeDefinition>();

    static ISourceToILConverter ProvideSourceToILConverter(IMetadataHost host, ISourceLocationProvider/*?*/ sourceLocationProvider, IContractProvider/*?*/ contractProvider) {
      return new CodeModelToILConverter(host, sourceLocationProvider, contractProvider);
    }

  }
}