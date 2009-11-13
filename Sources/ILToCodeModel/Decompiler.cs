//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System.Collections.Generic;
using Microsoft.Cci.MutableCodeModel;
using Microsoft.Cci.Contracts;
using System.IO;
using System.Diagnostics;

namespace Microsoft.Cci.ILToCodeModel {

  /// <summary>
  /// Provides methods that convert a given Metadata Model into an equivalent Code Model. 
  /// </summary>
  public static class Decompiler {

    /// <summary>
    /// Returns a mutable Code Model assembly that is equivalent to the given Metadata Model assembly,
    /// except that in the new assembly method bodies also implement ISourceMethodBody.
    /// </summary>
    /// <param name="host">An object representing the application that is hosting this decompiler. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.</param>
    /// <param name="assembly">The root of the Metadata Model to be converted to a Code Model.</param>
    /// <param name="pdbReader">An object that can map offsets in an IL stream to source locations and block scopes. May be null.</param>
    public static Assembly GetCodeModelFromMetadataModel(IMetadataHost host, IAssembly assembly, PdbReader/*?*/ pdbReader) {
      return (Assembly)GetCodeAndContractModelFromMetadataModelHelper(host, assembly, pdbReader, pdbReader, null);
    }

    /// <summary>
    /// Returns a mutable Code Model assembly that is equivalent to the given Metadata Model assembly,
    /// except that in the new assembly method bodies also implement ISourceMethodBody.
    /// </summary>
    /// <param name="host">An object representing the application that is hosting this decompiler. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.</param>
    /// <param name="assembly">The root of the Metadata Model to be converted to a Code Model.</param>
    /// <param name="pdbReader">An object that can map offsets in an IL stream to source locations and block scopes. May be null.</param>
    /// <param name="contractProvider">A mutable object that implements IContractProvider. Any code contracts discovered during decompilation are added to this object.</param>
    public static Assembly GetCodeAndContractModelFromMetadataModel(IMetadataHost host, IAssembly assembly, PdbReader/*?*/ pdbReader, ContractProvider contractProvider) {
      return (Assembly)GetCodeAndContractModelFromMetadataModelHelper(host, assembly, pdbReader, pdbReader, contractProvider);
    }

    /// <summary>
    /// Returns a mutable Code Model module that is equivalent to the given Metadata Model module,
    /// except that in the new module method bodies also implement ISourceMethodBody.
    /// </summary>
    /// <param name="host">An object representing the application that is hosting this decompiler. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.</param>
    /// <param name="module">The root of the Metadata Model to be converted to a Code Model.</param>
    /// <param name="pdbReader">An object that can map offsets in an IL stream to source locations and block scopes. May be null.</param>
    public static Module GetCodeModelFromMetadataModel(IMetadataHost host, IModule module, PdbReader/*?*/ pdbReader) {
      return GetCodeAndContractModelFromMetadataModelHelper(host, module, pdbReader, pdbReader, null);
    }

    /// <summary>
    /// Returns a mutable Code Model module that is equivalent to the given Metadata Model module,
    /// except that in the new module method bodies also implement ISourceMethodBody.
    /// </summary>
    /// <param name="host">An object representing the application that is hosting this decompiler. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.</param>
    /// <param name="module">The root of the Metadata Model to be converted to a Code Model.</param>
    /// <param name="pdbReader">An object that can map offsets in an IL stream to source locations and block scopes. May be null.</param>
    /// <param name="contractProvider">A mutable object that implements IContractProvider. Any code contracts discovered during decompilation are added to this object.</param>
    public static Module GetCodeAndContractModelFromMetadataModel(IMetadataHost host, IModule module, PdbReader/*?*/ pdbReader, ContractProvider contractProvider) {
      return GetCodeAndContractModelFromMetadataModelHelper(host, module, pdbReader, pdbReader, contractProvider);
    }

    /// <summary>
    /// Returns a mutable Code Model module that is equivalent to the given Metadata Model module,
    /// except that in the new module method bodies also implement ISourceMethodBody.
    /// </summary>
    /// <param name="host">An object representing the application that is hosting this decompiler. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.</param>
    /// <param name="module">The root of the Metadata Model to be converted to a Code Model.</param>
    /// <param name="sourceLocationProvider">An object that can map some kinds of ILocation objects to IPrimarySourceLocation objects. May be null.</param>
    /// <param name="localScopeProvider">An object that can provide information about the local scopes of a method. May be null.</param>
    /// <param name="contractProvider">A mutable object that implements IContractProvider. Any code contracts discovered during decompilation are added to this object. May be null.</param>
    private static Module GetCodeAndContractModelFromMetadataModelHelper(IMetadataHost host, IModule module,
      ISourceLocationProvider/*?*/ sourceLocationProvider, ILocalScopeProvider/*?*/ localScopeProvider, ContractProvider/*?*/ contractProvider) {
      var replacer = new ReplaceMetadataMethodBodiesWithDecompiledMethodBodies(host, module, sourceLocationProvider, localScopeProvider, contractProvider);
      var result = replacer.Visit(module); //Makes a mutable copy of module and simultaneously replaces the method bodies with bodies that can decompile the IL
      var finder = new HelperTypeFinder();
      finder.Visit(result);
      var remover = new RemoveUnnecessaryTypes(finder.helperTypes);
      remover.Visit(result);

      return result;
    }
  }

  /// <summary>
  /// A mutator that copies metadata models into mutable code models by using the base MetadataMutator class to make a mutable copy
  /// of a given metadata model and also replaces any method bodies with instances of SourceMethodBody, which implements the ISourceMethodBody.Block property
  /// by decompiling the metadata model information provided by the properties of IMethodBody.
  /// </summary>
  internal class ReplaceMetadataMethodBodiesWithDecompiledMethodBodies : MetadataMutator {

    /// <summary>
    /// A mutable object that implements IContractProvider. Any code contracts discovered during decompilation are added to this object. May be null.
    /// </summary>
    ContractProvider/*?*/ contractProvider;

    /// <summary>
    /// An object that can provide information about the local scopes of a method. May be null. 
    /// </summary>
    ILocalScopeProvider/*?*/ localScopeProvider;

    /// <summary>
    /// An object that can map offsets in an IL stream to source locations and block scopes. May be null.
    /// </summary>
    ISourceLocationProvider/*?*/ sourceLocationProvider;

    /// <summary>
    /// Allocates a mutator that copies metadata models into mutable code models by using the base MetadataMutator class to make a mutable copy
    /// of a given metadata model and also replaces any method bodies with instances of SourceMethodBody, which implements the ISourceMethodBody.Block property
    /// by decompiling the metadata model information provided by the properties of IMethodBody.
    /// </summary>
    /// <param name="host">An object representing the application that is hosting this mutator. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.</param>
    /// <param name="unit">The unit of metadata that will be mutated.</param>
    /// <param name="sourceLocationProvider">An object that can map some kinds of ILocation objects to IPrimarySourceLocation objects. May be null.</param>
    /// <param name="localScopeProvider">An object that can provide information about the local scopes of a method. May be null.</param>
    /// <param name="contractProvider">A mutable object that implements IContractProvider. Any code contracts discovered during decompilation are added to this object. May be null.</param>
    internal ReplaceMetadataMethodBodiesWithDecompiledMethodBodies(IMetadataHost host, IUnit unit,
      ISourceLocationProvider/*?*/ sourceLocationProvider, ILocalScopeProvider/*?*/ localScopeProvider, ContractProvider/*?*/ contractProvider)
      : base(host) {
      this.contractProvider = contractProvider;
      this.localScopeProvider = localScopeProvider;
      this.sourceLocationProvider = sourceLocationProvider;
    }

    /// <summary>
    /// Replaces the given method body with an equivalent instance of SourceMethod body, which in addition also implements ISourceMethodBody,
    /// which has the additional property, Block, which represents the corresponding Code Model for the method body.
    /// </summary>
    /// <param name="methodBody">The method body to visit.</param>
    public override IMethodBody Visit(IMethodBody methodBody) {
      var ilBody = base.Visit(methodBody); //Visit the body to fix up all the references to point to the copy.
      var result = new SourceMethodBody(ilBody, this.host, this.sourceLocationProvider, this.localScopeProvider, this.contractProvider);
      return result;
    }

  }

  /// <summary>
  /// A traverser that visits every method body and collects together all of the private helper types of these bodies.
  /// </summary>
  internal sealed class HelperTypeFinder : BaseMetadataTraverser {

    /// <summary>
    /// Contains an entry for every type that has been introduced by the compiler to hold the state of an anonymous delegate or of an iterator.
    /// Since decompilation re-introduces the anonymous delegates and iterators, these types should be removed from member lists.
    /// They stick around as PrivateHelperTypes of the methods containing the iterators and anonymous delegates.
    /// </summary>
    internal Dictionary<uint, ITypeDefinition> helperTypes = new Dictionary<uint, ITypeDefinition>();

    /// <summary>
    /// Traverses only the namespace root of the given assembly, removing any type from the model that have the same
    /// interned key as one of the entries of this.typesToRemove.
    /// </summary>
    public override void Visit(IModule module) {
      this.Visit(module.NamespaceRoot);
    }

    /// <summary>
    /// Visits the specified type definition, traversing only the nested types and methods and
    /// collecting together all of the private helper types that are introduced by the compiler
    /// when methods that contain closures or iterators are compiled.
    /// </summary>
    public override void Visit(ITypeDefinition typeDefinition) {
      var mutableTypeDefinition = (TypeDefinition)typeDefinition;
      foreach (ITypeDefinition nestedType in mutableTypeDefinition.NestedTypes)
        this.Visit(nestedType);
      foreach (IMethodDefinition method in mutableTypeDefinition.Methods)
        this.Visit(method);
    }

    /// <summary>
    /// Visits only the (possibly missing) body of the method.
    /// </summary>
    /// <param name="method"></param>
    public override void Visit(IMethodDefinition method) {
      if (method.IsAbstract || method.IsExternal) return;
      this.Visit(method.Body);
    }

    /// <summary>
    /// Records all of the helper types of the method body into this.helperTypes.
    /// </summary>
    /// <param name="methodBody"></param>
    public override void Visit(IMethodBody methodBody) {
      var mutableBody = (SourceMethodBody)methodBody;
      if (mutableBody.privateHelperTypesToRemove == null) return;
      foreach (var helperType in mutableBody.privateHelperTypesToRemove)
        this.helperTypes.Add(helperType.InternedKey, helperType);
    }

  }

  /// <summary>
  /// A traverser for a mutable code model that removes a specified set of types from the model.
  /// </summary>
  internal class RemoveUnnecessaryTypes : BaseMetadataTraverser {

    /// <summary>
    /// Contains an entry for every type that has been introduced by the compiler to hold the state of an anonymous delegate or of an iterator.
    /// Since decompilation re-introduces the anonymous delegates and iterators, these types should be removed from member lists.
    /// They stick around as PrivateHelperTypes of the methods containing the iterators and anonymous delegates.
    /// </summary>
    Dictionary<uint, ITypeDefinition> helperTypes;

    /// <summary>
    /// Allocatates a traverser for a mutable code model that removes a specified set of types from the model.
    /// </summary>
    /// <param name="helperTypes">Dictionary whose keys are the interned keys of the types to remove from member lists.</param>
    internal RemoveUnnecessaryTypes(Dictionary<uint, ITypeDefinition> helperTypes) {
      this.helperTypes = helperTypes;
    }

    /// <summary>
    /// Traverses only the namespace root of the given assembly, removing any type from the model that have the same
    /// interned key as one of the entries of this.typesToRemove.
    /// </summary>
    public override void Visit(IModule module) {
      this.Visit(module.NamespaceRoot);
    }

    /// <summary>
    /// Visits the specified type definition, removing any nested types that are compiler introduced private helper types
    /// for maintaining the state of closures and anonymous delegates.
    /// </summary>
    public override void Visit(ITypeDefinition typeDefinition) {
      var mutableTypeDefinition = (TypeDefinition)typeDefinition;
      for (int i = 0; i < mutableTypeDefinition.NestedTypes.Count; i++) {
        var nestedType = mutableTypeDefinition.NestedTypes[i];
        if (this.helperTypes.ContainsKey(nestedType.InternedKey)) {
          mutableTypeDefinition.NestedTypes.RemoveAt(i);
          i--;
        } else
          this.Visit(nestedType);
      }
    }

  }

}