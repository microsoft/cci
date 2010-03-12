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
using Microsoft.Cci;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci {
  /// <summary>
  /// A provider that aggregates a set of providers in order to
  /// map offsets in an IL stream to source locations.
  /// </summary>
  public sealed class AggregatingSourceLocationProvider : ISourceLocationProvider, IDisposable
    {

    Dictionary<IUnit, ISourceLocationProvider> unit2Provider = new Dictionary<IUnit, ISourceLocationProvider>();

    /// <summary>
    /// Copies the contents of the table
    /// </summary>
    public AggregatingSourceLocationProvider(Dictionary<IUnit, ISourceLocationProvider> unit2ProviderMap) {
      foreach (var keyValuePair in unit2ProviderMap) {
        this.unit2Provider.Add(keyValuePair.Key, keyValuePair.Value);
      }
    }

    #region ISourceLocationProvider Members

    public IEnumerable<IPrimarySourceLocation> GetPrimarySourceLocationsFor(IEnumerable<ILocation> locations) {
      foreach (ILocation location in locations) {
        foreach (var psloc in this.MapLocationToSourceLocations(location))
          yield return psloc;
      }
    }

    public IEnumerable<IPrimarySourceLocation> GetPrimarySourceLocationsFor(ILocation location) {
      var psloc = location as IPrimarySourceLocation;
      if (psloc != null)
        return IteratorHelper.GetSingletonEnumerable(psloc);
      else {
        return this.MapLocationToSourceLocations(location);
      }
    }

    public IEnumerable<IPrimarySourceLocation> GetPrimarySourceLocationsForDefinitionOf(ILocalDefinition localDefinition) {
      ISourceLocationProvider/*?*/ provider = this.GetProvider(localDefinition);
      if (provider == null)
        return IteratorHelper.GetEmptyEnumerable<IPrimarySourceLocation>();
      else
        return provider.GetPrimarySourceLocationsForDefinitionOf(localDefinition);
    }

    public string GetSourceNameFor(ILocalDefinition localDefinition, out bool isCompilerGenerated) {
      ISourceLocationProvider/*?*/ provider = this.GetProvider(localDefinition);
      if (provider == null) {
        isCompilerGenerated = false;
        return "";
      } else {
        return provider.GetSourceNameFor(localDefinition, out isCompilerGenerated);
      }
    }

    #endregion

    #region IDisposable Members

    public void Dispose() {
      this.Close();
      GC.SuppressFinalize(this);
    }

    #endregion

    ~AggregatingSourceLocationProvider() {
      this.Close();
    }

    private void Close() {
      foreach (var p in this.unit2Provider.Values) {
        IDisposable d = p as IDisposable;
        if (d != null)
          d.Dispose();
      }
    }

    #region Helper methods

    private IMethodDefinition lastUsedMethod = Dummy.Method;
    private ISourceLocationProvider lastUsedProvider = default(ISourceLocationProvider);
    private ISourceLocationProvider/*?*/ GetProvider(IMethodDefinition methodDefinition) {
      if (methodDefinition == lastUsedMethod) return lastUsedProvider;
      ISourceLocationProvider provider = null;
      var definingUnit = TypeHelper.GetDefiningUnit(methodDefinition.ResolvedMethod.ContainingTypeDefinition);
      this.unit2Provider.TryGetValue(definingUnit, out provider);
      if (provider != null) {
        this.lastUsedMethod = methodDefinition;
        this.lastUsedProvider = provider;
      }
      return provider;
    }

    private ISourceLocationProvider/*?*/ GetProvider(IILLocation/*?*/ mbLocation) {
      if (mbLocation == null) return null;
      return this.GetProvider(mbLocation.MethodDefinition);
    }

    private ISourceLocationProvider/*?*/ GetProvider(ILocalDefinition localDefinition) {
      return this.GetProvider(localDefinition.MethodDefinition);
    }

    private IEnumerable<IPrimarySourceLocation/*?*/> MapLocationToSourceLocations(ILocation location) {
      IILLocation/*?*/ mbLocation = location as IILLocation;
      ISourceLocationProvider provider = this.GetProvider(mbLocation);
      if (provider != null)
        foreach (var psloc in provider.GetPrimarySourceLocationsFor(location))
          yield return psloc;
    }

    #endregion

  }

  /// <summary>
  /// A provider that aggregates a set of providers in order to
  /// map offsets in an IL stream to block scopes.
  /// </summary>
  public sealed class AggregatingLocalScopeProvider : ILocalScopeProvider, IDisposable {

    Dictionary<IUnit, ILocalScopeProvider> unit2Provider = new Dictionary<IUnit, ILocalScopeProvider>();

    /// <summary>
    /// Copies the contents of the table
    /// </summary>
    public AggregatingLocalScopeProvider(Dictionary<IUnit, ILocalScopeProvider> unit2ProviderMap) {
      foreach (var keyValuePair in unit2ProviderMap) {
        this.unit2Provider.Add(keyValuePair.Key, keyValuePair.Value);
      }
    }

    #region ILocalScopeProvider Members

    public IEnumerable<ILocalScope> GetIteratorScopes(IMethodBody methodBody) {
      ILocalScopeProvider/*?*/ provider = this.GetProvider(methodBody.MethodDefinition);
      if (provider == null) {
        return IteratorHelper.GetEmptyEnumerable<ILocalScope>();
      } else {
        return provider.GetIteratorScopes(methodBody);
      }
    }

    public IEnumerable<ILocalScope> GetLocalScopes(IMethodBody methodBody) {
      ILocalScopeProvider/*?*/ provider = this.GetProvider(methodBody.MethodDefinition);
      if (provider == null) {
        return IteratorHelper.GetEmptyEnumerable<ILocalScope>();
      } else {
        return provider.GetLocalScopes(methodBody);
      }
    }

    public IEnumerable<INamespaceScope> GetNamespaceScopes(IMethodBody methodBody) {
      ILocalScopeProvider/*?*/ provider = this.GetProvider(methodBody.MethodDefinition);
      if (provider == null) {
        return IteratorHelper.GetEmptyEnumerable<INamespaceScope>();
      } else {
        return provider.GetNamespaceScopes(methodBody);
      }
    }

    public IEnumerable<ILocalDefinition> GetConstantsInScope(ILocalScope scope) {
      ILocalScopeProvider/*?*/ provider = this.GetProvider(scope.MethodDefinition);
      if (provider == null) {
        return IteratorHelper.GetEmptyEnumerable<ILocalDefinition>();
      } else {
        return provider.GetConstantsInScope(scope);
      }
    }

    public IEnumerable<ILocalDefinition> GetVariablesInScope(ILocalScope scope) {
      ILocalScopeProvider/*?*/ provider = this.GetProvider(scope.MethodDefinition);
      if (provider == null) {
        return IteratorHelper.GetEmptyEnumerable<ILocalDefinition>();
      } else {
        return provider.GetVariablesInScope(scope);
      }
    }

    public bool IsIterator(IMethodBody methodBody) {
      var provider = this.GetProvider(methodBody.MethodDefinition);
      if (provider == null) return false;
      return provider.IsIterator(methodBody);
    }

    #endregion

    #region IDisposable Members

    public void Dispose() {
      this.Close();
      GC.SuppressFinalize(this);
    }

    #endregion

    ~AggregatingLocalScopeProvider() {
      this.Close();
    }

    private void Close() {
      foreach (var p in this.unit2Provider.Values) {
        IDisposable d = p as IDisposable;
        if (d != null)
          d.Dispose();
      }
    }

    #region Helper methods

    private IMethodDefinition lastUsedMethod = Dummy.Method;
    private ILocalScopeProvider lastUsedProvider = null;
    private ILocalScopeProvider/*?*/ GetProvider(IMethodDefinition methodDefinition) {
      if (methodDefinition == lastUsedMethod) return lastUsedProvider;
      ILocalScopeProvider provider = null;
      var definingUnit = TypeHelper.GetDefiningUnit(methodDefinition.ResolvedMethod.ContainingTypeDefinition);
      this.unit2Provider.TryGetValue(definingUnit, out provider);
      if (provider != null) {
        this.lastUsedMethod = methodDefinition;
        this.lastUsedProvider = provider;
      }
      return provider;
    }

    private ILocalScopeProvider/*?*/ GetProvider(IILLocation/*?*/ mbLocation) {
      if (mbLocation == null) return null;
      return this.GetProvider(mbLocation.MethodDefinition);
    }

    private ILocalScopeProvider/*?*/ GetProvider(ILocalDefinition localDefinition) {
      return this.GetProvider(localDefinition.MethodDefinition);
    }

    #endregion

  }
}