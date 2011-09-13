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
using System.Diagnostics.Contracts;
using System.IO;
using Microsoft.Cci.ILToCodeModel;
using Microsoft.Cci.MutableCodeModel;
using Microsoft.Cci.MutableCodeModel.Contracts;
using Microsoft.Cci.MutableContracts;

namespace Microsoft.Cci.Contracts {

  /// <summary>
  /// A contract provider that layers on top of an existing contract provider and which
  /// takes into account the way contracts for abstract methods are represented
  /// when IL uses the Code Contracts library. Namely, the containing type of an abstract method has an
  /// attribute that points to a class of proxy methods which hold the contracts for the corresponding
  /// abstract method.
  /// This provider wraps an existing non-code-contracts-aware provider and caches to avoid recomputing
  /// whether a contract exists or not.
  /// </summary>
  public class CodeContractsContractExtractor : IContractExtractor {

    /// <summary>
    /// needed to be able to map the contracts from a contract class proxy method to an abstract method
    /// </summary>
    IMetadataHost host;
    /// <summary>
    /// The (non-aware) provider that was used to extract the contracts from the IL.
    /// </summary>
    IContractExtractor underlyingContractProvider;
    /// <summary>
    /// Used just to cache results to that the underlyingContractProvider doesn't have to get asked
    /// more than once.
    /// </summary>
    ContractProvider contractProviderCache;

    /// <summary>
    /// Creates a contract provider which is aware of how abstract methods have their contracts encoded.
    /// </summary>
    /// <param name="host">
    /// The host that was used to load the unit for which the <paramref name="underlyingContractProvider"/>
    /// is a provider for.
    /// </param>
    /// <param name="underlyingContractProvider">
    /// The (non-aware) provider that was used to extract the contracts from the IL.
    /// </param>
    public CodeContractsContractExtractor(IMetadataHost host, IContractExtractor underlyingContractProvider) {
      this.host = host;
      this.underlyingContractProvider = underlyingContractProvider;
      this.contractProviderCache = new ContractProvider(underlyingContractProvider.ContractMethods, underlyingContractProvider.Unit);
    }

    #region IContractProvider Members

    /// <summary>
    /// Returns the loop contract, if any, that has been associated with the given object. Returns null if no association exits.
    /// </summary>
    /// <param name="loop">An object that might have been associated with a loop contract. This can be any kind of object.</param>
    /// <returns></returns>
    public ILoopContract/*?*/ GetLoopContractFor(object loop) {
      return this.underlyingContractProvider.GetLoopContractFor(loop);
    }

    /// <summary>
    /// Returns the method contract, if any, that has been associated with the given object. Returns null if no association exits.
    /// </summary>
    /// <param name="method">An object that might have been associated with a method contract. This can be any kind of object.</param>
    /// <returns></returns>
    public IMethodContract/*?*/ GetMethodContractFor(object method) {

      IMethodContract contract = this.contractProviderCache.GetMethodContractFor(method);
      if (contract != null) return contract == ContractDummy.MethodContract ? null : contract;

      IMethodReference methodReference = method as IMethodReference;
      if (methodReference == null) {
        this.contractProviderCache.AssociateMethodWithContract(method, ContractDummy.MethodContract);
        return null;
      }
      IMethodDefinition methodDefinition = methodReference.ResolvedMethod;
      if (methodDefinition == Dummy.Method) {
        this.contractProviderCache.AssociateMethodWithContract(method, ContractDummy.MethodContract);
        return null;
      }
      if (!methodDefinition.IsAbstract) {
        contract = this.underlyingContractProvider.GetMethodContractFor(method);
        if (contract != null) {
          return contract;
        } else {
          this.contractProviderCache.AssociateMethodWithContract(method, ContractDummy.MethodContract);
          return null;
        }
      }

      // But if it is an abstract method, then check to see if its containing type points to a class holding the contract
      IMethodDefinition/*?*/ proxyMethod = ContractHelper.GetMethodFromContractClass(this.host, methodDefinition);
      if (proxyMethod == null) {
        this.contractProviderCache.AssociateMethodWithContract(method, ContractDummy.MethodContract);
        return null;
      }
      ITypeDefinition contractClass;
      var specializedProxyMethod = proxyMethod as ISpecializedMethodDefinition;
      IMethodDefinition unspec = null;
      if (specializedProxyMethod != null) {
        unspec = ContractHelper.UninstantiateAndUnspecialize(specializedProxyMethod).ResolvedMethod;
        contract = this.underlyingContractProvider.GetMethodContractFor(unspec);
        contractClass = unspec.ContainingTypeDefinition;
      } else {
        contract = this.underlyingContractProvider.GetMethodContractFor(proxyMethod);
        contractClass = proxyMethod.ContainingTypeDefinition;
      }
      if (contract == null) {
        this.contractProviderCache.AssociateMethodWithContract(method, ContractDummy.MethodContract);
        return null;
      }
      var cccc = new ConvertContractClassContract(this.host, contractClass, methodDefinition.ContainingTypeDefinition);
      cccc.Traverse(contract);
      contract = ContractHelper.CopyContract(this.host, contract, methodDefinition, specializedProxyMethod != null ? unspec : proxyMethod);
      if (specializedProxyMethod != null || proxyMethod.IsGeneric || proxyMethod.ContainingTypeDefinition.IsGeneric) {
        var stdm = specializedProxyMethod as Immutable.SpecializedTypeDefinitionMember<IMethodDefinition>;
        var sourceTypeReferences = stdm == null ? 
          (proxyMethod.ContainingTypeDefinition.IsGeneric ? 
            IteratorHelper.GetConversionEnumerable<IGenericTypeParameter, ITypeReference>(methodDefinition.ContainingTypeDefinition.GenericParameters)
            : Enumerable<ITypeReference>.Empty)
          : stdm.ContainingGenericTypeInstance.GenericArguments;
        contract = ContractHelper.SpecializeMethodContract(this.host, contract, unspec == null ? proxyMethod.ContainingTypeDefinition : unspec.ContainingTypeDefinition, unspec == null ? proxyMethod.ContainingTypeDefinition.GenericParameters : unspec.ContainingTypeDefinition.GenericParameters, sourceTypeReferences,
          unspec == null ? proxyMethod.GenericParameters : unspec.GenericParameters, methodDefinition.GenericParameters);
      }
      this.contractProviderCache.AssociateMethodWithContract(methodDefinition, contract);
      return contract;
    }


    /// <summary>
    /// Returns the triggers, if any, that have been associated with the given object. Returns null if no association exits.
    /// </summary>
    /// <param name="quantifier">An object that might have been associated with triggers. This can be any kind of object.</param>
    /// <returns></returns>
    public IEnumerable<IEnumerable<IExpression>>/*?*/ GetTriggersFor(object quantifier) {
      return this.underlyingContractProvider.GetTriggersFor(quantifier);
    }

    /// <summary>
    /// Returns the type contract, if any, that has been associated with the given object. Returns null if no association exits.
    /// </summary>
    /// <param name="type">An object that might have been associated with a type contract. This can be any kind of object.</param>
    /// <returns></returns>
    public ITypeContract/*?*/ GetTypeContractFor(object type) {
      return this.underlyingContractProvider.GetTypeContractFor(type);
    }

    /// <summary>
    /// A collection of methods that can be called in a way that provides tools with information about contracts.
    /// </summary>
    /// <value></value>
    public IContractMethods/*?*/ ContractMethods {
      get { return this.underlyingContractProvider.ContractMethods; }
    }

    /// <summary>
    /// The unit that this is a contract provider for. Intentional design:
    /// no provider works on more than one unit.
    /// </summary>
    /// <value></value>
    public IUnit/*?*/ Unit {
      get { return this.underlyingContractProvider.Unit; }
    }

    #endregion

    #region IContractExtractor Members

    /// <summary>
    /// Delegate callback to underlying contract extractor.
    /// </summary>
    public void RegisterContractProviderCallback(IContractProviderCallback contractProviderCallback) {
      this.underlyingContractProvider.RegisterContractProviderCallback(contractProviderCallback);
    }

    /// <summary>
    /// For a client (e.g., the decompiler) that has a source method body and wants to have its
    /// contract extracted and added to the contract provider.
    /// </summary>
    public MethodContractAndMethodBody SplitMethodBodyIntoContractAndCode(ISourceMethodBody sourceMethodBody) {
      return this.underlyingContractProvider.SplitMethodBodyIntoContractAndCode(sourceMethodBody);
    }

    #endregion

    private class ConvertContractClassContract : CodeAndContractTraverser {

      private ITypeDefinition contractClass;
      private ITypeDefinition abstractType;
      private uint contractClassInternedKey;
      IMetadataHost host;

      private Dictionary<uint, IMethodReference> correspondingAbstractMember = new Dictionary<uint, IMethodReference>();

      public ConvertContractClassContract(IMetadataHost host, ITypeDefinition contractClass, ITypeDefinition abstractType)
        : base(null) {
        Contract.Requires(contractClass.IsGeneric == abstractType.IsGeneric);
        this.contractClass = contractClass;
        this.contractClassInternedKey = this.contractClass.InternedKey;
        this.abstractType = abstractType;
        this.host = host;
      }

      public override void TraverseChildren(IMethodCall methodCall) {
        var mtc = methodCall.MethodToCall;
        var ct = ContractHelper.UninstantiateAndUnspecialize(mtc).ContainingType;
        if (ct.InternedKey != this.contractClass.InternedKey) return;
        foreach (IMethodDefinition ifaceMethod in ContractHelper.GetAllImplicitlyImplementedInterfaceMethods(mtc.ResolvedMethod)) {
          var mutableMethodCall = (MethodCall)methodCall;
          mutableMethodCall.MethodToCall = ifaceMethod;
          mutableMethodCall.IsVirtualCall = true;
          base.TraverseChildren(methodCall);
          return;
        }
        return;
      }

    }

  }

  /// <summary>
  /// A contract provider that can be used to get contracts from a unit by querying in
  /// a random-access manner. That is, the unit is *not* traversed eagerly.
  /// </summary>
  public class LazyContractExtractor : IContractExtractor, IDisposable {

    /// <summary>
    /// Needed because the decompiler requires the concrete class ContractProvider
    /// </summary>
    ContractProvider underlyingContractProvider;
    /// <summary>
    /// needed to pass to decompiler
    /// </summary>
    IContractAwareHost host;
    /// <summary>
    /// needed to pass to decompiler
    /// </summary>
    private PdbReader/*?*/ pdbReader;
    /// <summary>
    /// Objects interested in getting the method body after extraction.
    /// </summary>
    List<IContractProviderCallback> callbacks = new List<IContractProviderCallback>();

    private IUnit unit; // the module this is a lazy provider for

    /// <summary>
    /// Allocates an object that can be used to query for contracts by asking questions about specific methods/types, etc.
    /// </summary>
    /// <param name="host">The host that loaded the unit for which this is to be a contract provider.</param>
    /// <param name="unit">The unit to retrieve the contracts from.</param>
    /// <param name="contractMethods">A collection of methods that can be called in a way that provides tools with information about contracts.</param>
    /// <param name="usePdb">Whether to use the PDB file (and possibly the source files if available) during extraction.</param>
    public LazyContractExtractor(IContractAwareHost host, IUnit unit, IContractMethods contractMethods, bool usePdb) {
      this.host = host;
      this.underlyingContractProvider = new ContractProvider(contractMethods, unit);
      if (usePdb) {
        string pdbFile = Path.ChangeExtension(unit.Location, "pdb");
        if (File.Exists(pdbFile)) {
          using (var pdbStream = File.OpenRead(pdbFile)) {
            this.pdbReader = new PdbReader(pdbStream, host);
          }
        }
      }
      this.unit = unit;
    }

    /// <summary>
    /// Disposes the PdbReader object, if any, that is used to obtain the source text locations corresponding to contracts.
    /// </summary>
    public virtual void Dispose() {
      this.Close();
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the PdbReader object, if any, that is used to obtain the source text locations corresponding to contracts.
    /// </summary>
    ~LazyContractExtractor() {
      this.Close();
    }

    private void Close() {
      if (this.pdbReader != null)
        this.pdbReader.Dispose();
    }

    /// <summary>
    /// The unit that this is a contract provider for. Intentional design:
    /// no provider works on more than one unit.
    /// </summary>
    /// <value></value>
    public IUnit Unit { get { return this.unit; } }

    /// <summary>
    /// Gets the host.
    /// </summary>
    /// <value>The host.</value>
    public IMetadataHost Host { get { return this.host; } }

    #region IContractProvider Members

    /// <summary>
    /// Returns the loop contract, if any, that has been associated with the given object. Returns null if no association exits.
    /// </summary>
    /// <param name="loop">An object that might have been associated with a loop contract. This can be any kind of object.</param>
    /// <returns></returns>
    public ILoopContract/*?*/ GetLoopContractFor(object loop) {
      throw new NotImplementedException();
    }

    /// <summary>
    /// Returns the method contract, if any, that has been associated with the given object. Returns null if no association exits.
    /// </summary>
    /// <param name="method">An object that might have been associated with a method contract. This can be any kind of object.</param>
    /// <returns></returns>
    public IMethodContract/*?*/ GetMethodContractFor(object method) {

      IMethodContract contract = this.underlyingContractProvider.GetMethodContractFor(method);
      if (contract != null) return contract == ContractDummy.MethodContract ? null : contract;

      IMethodReference methodReference = method as IMethodReference;
      if (methodReference == null) {
        this.underlyingContractProvider.AssociateMethodWithContract(method, ContractDummy.MethodContract);
        return null;
      }

      IMethodDefinition methodDefinition = methodReference.ResolvedMethod;
      if (methodDefinition == Dummy.Method) {
        this.underlyingContractProvider.AssociateMethodWithContract(method, ContractDummy.MethodContract);
        return null;
      }

      if (methodDefinition.IsAbstract || methodDefinition.IsExternal) { // precondition of Body getter
        this.underlyingContractProvider.AssociateMethodWithContract(method, ContractDummy.MethodContract);
        return null;
      }

      IMethodBody methodBody = methodDefinition.Body;

      if (methodBody is Dummy) {
        this.underlyingContractProvider.AssociateMethodWithContract(method, ContractDummy.MethodContract);
        return null;
      }

      ISourceMethodBody/*?*/ sourceMethodBody = methodBody as ISourceMethodBody;
      if (sourceMethodBody == null) {
        //sourceMethodBody = new Microsoft.Cci.ILToCodeModel.SourceMethodBody(methodBody, this.host, this.pdbReader, this.pdbReader);
        sourceMethodBody = Decompiler.GetCodeModelFromMetadataModel(this.host, methodBody, this.pdbReader, DecompilerOptions.AnonymousDelegates);
      }

      MethodContractAndMethodBody result = this.SplitMethodBodyIntoContractAndCode(sourceMethodBody);
      var methodContract = result.MethodContract;

      // If the method was generated for an auto-property, then need to see if a
      // contract can be derived by mining the invariant.
      if (ContractHelper.IsAutoPropertyMember(this.host, methodDefinition)) {

        var tc = GetTypeContractFor(methodDefinition.ContainingTypeDefinition);
        if (tc != null) {
          var derivedPreconditions = new List<IPrecondition>();
          var locations = new List<ILocation>();
          foreach (var i in tc.Invariants) {
            locations.AddRange(i.Locations);
            derivedPreconditions.Add(
              new Precondition() {
                Condition = i.Condition,
                Description = i.Description,
                OriginalSource = i.OriginalSource,
                Locations = new List<ILocation>(i.Locations),
              });
          }
          if (0 < derivedPreconditions.Count) {
            var derivedMethodContract = new MethodContract() {
              Locations = locations,
              Preconditions = derivedPreconditions,
            };
            if (methodContract != null) {
              ContractHelper.AddMethodContract(derivedMethodContract, methodContract);
            }
            methodContract = derivedMethodContract;
          }
        }
      }

      if (methodContract == null) {
        this.underlyingContractProvider.AssociateMethodWithContract(method, ContractDummy.MethodContract); // so we don't try to extract more than once
      } else {
        this.underlyingContractProvider.AssociateMethodWithContract(method, methodContract);
      }

      // Notify all interested parties
      foreach (var c in this.callbacks) {
        c.ProvideResidualMethodBody(methodDefinition, result.BlockStatement);
      }

      return methodContract;
    }

    /// <summary>
    /// Returns the triggers, if any, that have been associated with the given object. Returns null if no association exits.
    /// </summary>
    /// <param name="quantifier">An object that might have been associated with triggers. This can be any kind of object.</param>
    /// <returns></returns>
    public IEnumerable<IEnumerable<IExpression>>/*?*/ GetTriggersFor(object quantifier) {
      throw new NotImplementedException();
    }

    /// <summary>
    /// Returns the type contract, if any, that has been associated with the given object. Returns null if no association exits.
    /// </summary>
    /// <param name="type">An object that might have been associated with a type contract. This can be any kind of object.</param>
    /// <returns></returns>
    public ITypeContract/*?*/ GetTypeContractFor(object type) {

      ITypeContract/*?*/ typeContract = this.underlyingContractProvider.GetTypeContractFor(type);
      if (typeContract != null) return typeContract == ContractDummy.TypeContract ? null : typeContract;

      ITypeReference/*?*/ typeReference = type as ITypeReference;
      if (typeReference == null) {
        this.underlyingContractProvider.AssociateTypeWithContract(type, ContractDummy.TypeContract);
        return null;
      }

      ITypeDefinition/*?*/ typeDefinition = typeReference.ResolvedType;
      if (typeDefinition == null) {
        this.underlyingContractProvider.AssociateTypeWithContract(type, ContractDummy.TypeContract);
        return null;
      }

      var contract = Microsoft.Cci.MutableContracts.ContractExtractor.GetObjectInvariant(this.host, typeDefinition, this.pdbReader, this.pdbReader);
      if (contract == null) {
        this.underlyingContractProvider.AssociateTypeWithContract(type, ContractDummy.TypeContract); // so we don't try to extract more than once
        return null;
      } else {
        this.underlyingContractProvider.AssociateTypeWithContract(type, contract);
        return contract;
      }
    }

    /// <summary>
    /// A collection of methods that can be called in a way that provides tools with information about contracts.
    /// </summary>
    /// <value></value>
    public IContractMethods ContractMethods {
      get { return this.underlyingContractProvider.ContractMethods; }
    }

    #endregion

    #region IContractExtractor Members

    /// <summary>
    /// After the callback has been registered, when a contract is extracted
    /// from a method, the callback will be notified.
    /// </summary>
    public void RegisterContractProviderCallback(IContractProviderCallback contractProviderCallback) {
      this.callbacks.Add(contractProviderCallback);
    }

    /// <summary>
    /// For a client (e.g., the decompiler) that has a source method body and wants to have its
    /// contract extracted and added to the contract provider.
    /// </summary>
    public MethodContractAndMethodBody SplitMethodBodyIntoContractAndCode(ISourceMethodBody sourceMethodBody) {
      return Microsoft.Cci.MutableContracts.ContractExtractor.SplitMethodBodyIntoContractAndCode(this.host, sourceMethodBody, this.pdbReader);
    }

    #endregion
  }

  /// <summary>
  /// A mutator that changes all references defined in one unit into being
  /// references defined in another unit.
  /// It does so by substituting the target unit's identity for the source
  /// unit's identity whenever it rewrites a unit reference.
  /// </summary>
  internal class MappingMutator {

    private IMetadataHost host;
    private UnitIdentity sourceUnitIdentity;
    private IUnit targetUnit = null;

    public MappingMutator(IMetadataHost host, IUnit targetUnit, IUnit sourceUnit) {
      this.host = host;
      this.sourceUnitIdentity = sourceUnit.UnitIdentity;
      this.targetUnit = targetUnit;
    }

    public IMethodReference Map(IMethodReference methodReference) {
      var result = new MetadataDeepCopier(host).Copy(methodReference);
      var rewriter = new ActualMutator(host, targetUnit, sourceUnitIdentity);
      return rewriter.Rewrite(result);
    }
    public ITypeReference Map(ITypeReference typeReference) {
      var result = new MetadataDeepCopier(host).Copy(typeReference);
      var rewriter = new ActualMutator(host, targetUnit, sourceUnitIdentity);
      return rewriter.Rewrite(result);
    }
    public IMethodContract Map(IMethodContract methodContract) {
      var result = new CodeAndContractDeepCopier(host).Copy(methodContract);
      var rewriter = new ActualMutator(host, targetUnit, sourceUnitIdentity);
      return rewriter.Rewrite(result);
    }
    public ITypeContract Map(ITypeContract typeContract) {
      var result = new CodeAndContractDeepCopier(host).Copy(typeContract);
      var rewriter = new ActualMutator(host, targetUnit, sourceUnitIdentity);
      return rewriter.Rewrite(result);
    }

    private class ActualMutator : CodeAndContractRewriter {

      private UnitIdentity sourceUnitIdentity;
      private IUnit targetUnit = null;

      /// <summary>
      /// A mutator that, when it visits anything, converts any references defined in the <paramref name="sourceUnit"/>
      /// into references defined in the <paramref name="targetUnit"/>
      /// </summary>
      /// <param name="host">
      /// The host that loaded the <paramref name="targetUnit"/>
      /// </param>
      /// <param name="targetUnit">
      /// The unit to which all references in the <paramref name="sourceUnit"/>
      /// will mapped.
      /// </param>
      /// <param name="sourceUnit">
      /// The unit from which references will be mapped into references from the <paramref name="targetUnit"/>
      /// </param>
      public ActualMutator(IMetadataHost host, IUnit targetUnit, UnitIdentity sourceUnitIdentity)
        : base(host) {
        this.sourceUnitIdentity = sourceUnitIdentity;
        this.targetUnit = targetUnit;
      }

      public override IModuleReference Rewrite(IModuleReference moduleReference) {
        if (moduleReference.UnitIdentity.Equals(this.sourceUnitIdentity)) {
          return (IModuleReference)this.targetUnit;
        }
        return base.Rewrite(moduleReference);
      }
      public override IAssemblyReference Rewrite(IAssemblyReference assemblyReference) {
        if (assemblyReference.UnitIdentity.Equals(this.sourceUnitIdentity)) {
          return (IAssemblyReference)this.targetUnit;
        }
        return base.Rewrite(assemblyReference);
      }
    }
  }

  /// <summary>
  /// A contract extractor that serves up the union of the contracts found from a set of contract extractors.
  /// One extractor is the primary extractor: all contracts retrieved from this contract extractor are expressed
  /// in terms of the types/members as defined by that extractor's unit. Optionally, a set of secondary extractors
  /// are used to query for contracts on equivalent methods/types: any contracts found are transformed into
  /// being contracts expressed over the types/members as defined by the primary provider and additively
  /// merged into the contracts from the primary extractor.
  /// </summary>
  public class AggregatingContractExtractor : IContractExtractor, IDisposable {

    private IUnit unit;
    private IContractExtractor primaryExtractor;
    private List<IContractProvider> oobExtractors;
    ContractProvider underlyingContractProvider; // used just because it provides a store so this provider can cache its results
    IMetadataHost host;
    private Dictionary<IContractProvider, MappingMutator> mapperForOobToPrimary = new Dictionary<IContractProvider, MappingMutator>();
    private Dictionary<IContractProvider, MappingMutator> mapperForPrimaryToOob = new Dictionary<IContractProvider, MappingMutator>();

    private List<object> methodsBeingExtracted = new List<object>();

    /// <summary>
    /// The constructor for creating an aggregating extractor.
    /// </summary>
    /// <param name="host">This is the host that loaded the unit for which the <paramref name="primaryExtractor"/> is
    /// the extractor for.
    /// </param>
    /// <param name="primaryExtractor">
    /// The extractor that will be used to define the types/members of things referred to in contracts.
    /// </param>
    /// <param name="oobExtractorsAndHosts">
    /// These are optional. If non-null, then it must be a finite sequence of pairs: each pair is a contract extractor
    /// and the host that loaded the unit for which it is a extractor.
    /// </param>
    public AggregatingContractExtractor(IMetadataHost host, IContractExtractor primaryExtractor, IEnumerable<KeyValuePair<IContractProvider, IMetadataHost>>/*?*/ oobExtractorsAndHosts) {
      var primaryUnit = primaryExtractor.Unit;
      this.unit = primaryUnit;
      this.primaryExtractor = primaryExtractor;

      this.underlyingContractProvider = new ContractProvider(primaryExtractor.ContractMethods, primaryUnit);
      this.host = host;

      if (oobExtractorsAndHosts != null) {
        this.oobExtractors = new List<IContractProvider>();
        foreach (var oobProviderAndHost in oobExtractorsAndHosts) {
          var oobProvider = oobProviderAndHost.Key;
          var oobHost = oobProviderAndHost.Value;
          this.oobExtractors.Add(oobProvider);
          IUnit oobUnit = oobProvider.Unit;
          this.mapperForOobToPrimary.Add(oobProvider, new MappingMutator(host, primaryUnit, oobUnit));
          this.mapperForPrimaryToOob.Add(oobProvider, new MappingMutator(oobHost, oobUnit, primaryUnit));
        }
      }
    }

    /// <summary>
    /// Disposes any constituent contract providers that implement the IDisposable interface.
    /// </summary>
    public virtual void Dispose() {
      this.Close();
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes any constituent contract providers that implement the IDisposable interface. 
    /// </summary>
    ~AggregatingContractExtractor() {
      this.Close();
    }

    private void Close() {
      var primaryDisposable = this.primaryExtractor as IDisposable;
      if (primaryDisposable != null) primaryDisposable.Dispose();
      foreach (var oobProvider in this.oobExtractors) {
        var oobDisposable = oobProvider as IDisposable;
        if (oobDisposable != null)
          oobDisposable.Dispose();
      }
    }

    #region IContractProvider Members

    /// <summary>
    /// Returns the loop contract, if any, that has been associated with the given object. Returns null if no association exits.
    /// </summary>
    /// <param name="loop">An object that might have been associated with a loop contract. This can be any kind of object.</param>
    /// <returns></returns>
    public ILoopContract/*?*/ GetLoopContractFor(object loop) {
      throw new NotImplementedException();
    }

    /// <summary>
    /// Returns the method contract, if any, that has been associated with the given object. Returns null if no association exits.
    /// </summary>
    /// <param name="method">An object that might have been associated with a method contract. This can be any kind of object.</param>
    /// <returns></returns>
    public IMethodContract/*?*/ GetMethodContractFor(object method) {

      if (this.methodsBeingExtracted.Contains(method)) {
        // hit a cycle while chasing validators/abbreviators
        // TODO: signal error
        return null;
      } else {
        this.methodsBeingExtracted.Add(method);
      }

      try {

        IMethodContract contract = this.underlyingContractProvider.GetMethodContractFor(method);
        if (contract != null) return contract == ContractDummy.MethodContract ? null : contract;

        MethodContract result = new MethodContract();
        IMethodContract primaryContract = this.primaryExtractor.GetMethodContractFor(method);
        bool found = false;
        if (primaryContract != null) {
          found = true;
          Microsoft.Cci.MutableContracts.ContractHelper.AddMethodContract(result, primaryContract);
        }
        if (this.oobExtractors != null) {
          foreach (var oobProvider in this.oobExtractors) {

            IMethodReference methodReference = method as IMethodReference;
            if (methodReference == null) continue; // REVIEW: Is there anything else it could be and still find a contract for it?

            MappingMutator primaryToOobMapper = this.mapperForPrimaryToOob[oobProvider];
            var oobMethod = primaryToOobMapper.Map(methodReference);

            if (oobMethod == null) continue;

            var oobContract = oobProvider.GetMethodContractFor(oobMethod);

            if (oobContract == null) continue;

            MappingMutator oobToPrimaryMapper = this.mapperForOobToPrimary[oobProvider];
            oobContract = oobToPrimaryMapper.Map(oobContract);

            var sps = new Microsoft.Cci.MutableContracts.SubstituteParameters(this.host, oobMethod.ResolvedMethod, methodReference.ResolvedMethod);
            oobContract = sps.Visit(oobContract);

            Microsoft.Cci.MutableContracts.ContractHelper.AddMethodContract(result, oobContract);
            found = true;

          }
        }

        // always cache so we don't try to extract more than once
        if (found) {
          this.underlyingContractProvider.AssociateMethodWithContract(method, result);
        } else {
          this.underlyingContractProvider.AssociateMethodWithContract(method, ContractDummy.MethodContract);
          result = null;
        }
        return result;
      } finally {
        this.methodsBeingExtracted.RemoveAt(this.methodsBeingExtracted.Count - 1);
      }
    }

    /// <summary>
    /// Returns the triggers, if any, that have been associated with the given object. Returns null if no association exits.
    /// </summary>
    /// <param name="quantifier">An object that might have been associated with triggers. This can be any kind of object.</param>
    /// <returns></returns>
    public IEnumerable<IEnumerable<IExpression>>/*?*/ GetTriggersFor(object quantifier) {
      throw new NotImplementedException();
    }

    /// <summary>
    /// Returns the type contract, if any, that has been associated with the given object. Returns null if no association exits.
    /// </summary>
    /// <param name="type">An object that might have been associated with a type contract. This can be any kind of object.</param>
    /// <returns></returns>
    public ITypeContract/*?*/ GetTypeContractFor(object type) {

      ITypeContract contract = this.underlyingContractProvider.GetTypeContractFor(type);
      if (contract != null) return contract == ContractDummy.TypeContract ? null : contract;

      TypeContract result = new TypeContract();
      ITypeContract primaryContract = this.primaryExtractor.GetTypeContractFor(type);
      bool found = false;
      if (primaryContract != null) {
        found = true;
        ContractHelper.AddTypeContract(result, primaryContract);
      }
      if (this.oobExtractors != null) {
        foreach (var oobProvider in this.oobExtractors) {
          var oobUnit = oobProvider.Unit;

          ITypeReference typeReference = type as ITypeReference;
          if (typeReference == null || typeReference is Dummy) continue; // REVIEW: Is there anything else it could be and still find a contract for it?

          MappingMutator primaryToOobMapper = this.mapperForPrimaryToOob[oobProvider];
          var oobType = primaryToOobMapper.Map(typeReference);

          if (oobType == null) continue;

          var oobContract = oobProvider.GetTypeContractFor(oobType);

          if (oobContract == null) continue;

          MappingMutator oobToPrimaryMapper = this.mapperForOobToPrimary[oobProvider];
          oobContract = oobToPrimaryMapper.Map(oobContract);
          ContractHelper.AddTypeContract(result, oobContract);
          found = true;

        }
      }

      // always cache so we don't try to extract more than once
      if (found) {
        this.underlyingContractProvider.AssociateTypeWithContract(type, result);
        return result;
      } else {
        this.underlyingContractProvider.AssociateTypeWithContract(type, ContractDummy.TypeContract);
        return null;
      }

    }

    /// <summary>
    /// A collection of methods that can be called in a way that provides tools with information about contracts.
    /// </summary>
    /// <value></value>
    public IContractMethods/*?*/ ContractMethods {
      get { return this.underlyingContractProvider.ContractMethods; }
    }

    /// <summary>
    /// The unit that this is a contract provider for. Intentional design:
    /// no provider works on more than one unit.
    /// </summary>
    /// <value></value>
    public IUnit/*?*/ Unit {
      get { return this.unit; }
    }

    #endregion

    #region IContractExtractor Members

    /// <summary>
    /// Delegate to the primary provider
    /// </summary>
    /// <param name="contractProviderCallback"></param>
    public void RegisterContractProviderCallback(IContractProviderCallback contractProviderCallback) {
      this.primaryExtractor.RegisterContractProviderCallback(contractProviderCallback);
    }

    /// <summary>
    /// For a client (e.g., the decompiler) that has a source method body and wants to have its
    /// contract extracted and added to the contract provider.
    /// </summary>
    public MethodContractAndMethodBody SplitMethodBodyIntoContractAndCode(ISourceMethodBody sourceMethodBody) {
      return this.primaryExtractor.SplitMethodBodyIntoContractAndCode(sourceMethodBody);
    }

    #endregion
  }

}