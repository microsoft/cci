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
using System.Collections.Generic;
using Microsoft.Cci.MutableCodeModel;
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
      return (Assembly)GetCodeModelFromMetadataModelHelper(host, assembly, pdbReader, pdbReader);
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
      return GetCodeModelFromMetadataModelHelper(host, module, pdbReader, pdbReader);
    }

    /// <summary>
    /// Returns a (mutable) Code Model SourceMethod body that is equivalent to the given Metadata Model method body.
    /// It does *not* delete any helper types.
    /// </summary>
    /// <param name="host">An object representing the application that is hosting this decompiler. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.</param>
    /// <param name="methodBody">The Metadata Model method body that is to be decompiled.</param>
    /// <param name="pdbReader">An object that can map offsets in an IL stream to source locations and block scopes. May be null.</param>
    public static ISourceMethodBody GetCodeModelFromMetadataModel(IMetadataHost host, IMethodBody methodBody, PdbReader/*?*/ pdbReader) {
      return new Microsoft.Cci.ILToCodeModel.SourceMethodBody(methodBody, host, pdbReader, pdbReader);
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
    private static Module GetCodeModelFromMetadataModelHelper(IMetadataHost host, IModule module,
      ISourceLocationProvider/*?*/ sourceLocationProvider, ILocalScopeProvider/*?*/ localScopeProvider) {
      var replacer = new ReplaceMetadataMethodBodiesWithDecompiledMethodBodies(host, module, sourceLocationProvider, localScopeProvider);
      var result = replacer.Visit(module); //Makes a mutable copy of module and simultaneously replaces the method bodies with bodies that can decompile the IL
      var finder = new HelperTypeFinder();
      finder.Visit(result);
      var remover = new RemoveUnnecessaryTypes(finder.helperTypes, finder.helperMethods, finder.helperFields);
      remover.Visit(result);
      result.AllTypes.RemoveAll(td => finder.helperTypes.ContainsKey(td.InternedKey)); // depends on RemoveAll preserving order
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
    internal ReplaceMetadataMethodBodiesWithDecompiledMethodBodies(IMetadataHost host, IUnit unit,
      ISourceLocationProvider/*?*/ sourceLocationProvider, ILocalScopeProvider/*?*/ localScopeProvider)
      : base(host) {
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
      return new SourceMethodBody(ilBody, this.host, this.sourceLocationProvider, this.localScopeProvider);
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
    /// Contains an entry for every method that has been introduced by the compiler in order to implement anonymous delegates.
    /// Since decompilation re-introduces the anonymous delegates and iterators, these members should be removed from member lists.
    /// They stick around as PrivateHelperMembers of the methods containing the anonymous delegates.
    /// </summary>
    internal Dictionary<uint, IMethodDefinition> helperMethods = new Dictionary<uint, IMethodDefinition>();

    /// <summary>
    /// Contains an entry for every field that has been introduced by the compiler in order to implement anonymous delegates.
    /// Since decompilation re-introduces the anonymous delegates and iterators, these members should be removed from member lists.
    /// They stick around as PrivateHelperMembers of the methods containing the anonymous delegates.
    /// </summary>
    internal Dictionary<IFieldDefinition, IFieldDefinition> helperFields = new Dictionary<IFieldDefinition, IFieldDefinition>();

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
      var block = mutableBody.Block; //force decompilation
      if (mutableBody.privateHelperTypesToRemove != null) {
        foreach (var helperType in mutableBody.privateHelperTypesToRemove)
          this.helperTypes.Add(helperType.InternedKey, helperType);
      }
      if (mutableBody.privateHelperMethodsToRemove != null) {
        foreach (var helperMethod in mutableBody.privateHelperMethodsToRemove.Values)
          this.helperMethods.Add(helperMethod.InternedKey, helperMethod);
      }
      if (mutableBody.privateHelperFieldsToRemove != null) {
        foreach (var helperField in mutableBody.privateHelperFieldsToRemove.Values)
          this.helperFields.Add(helperField, helperField);
      }
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
    /// Contains an entry for every method that has been introduced by the compiler in order to implement anonymous delegates.
    /// Since decompilation re-introduces the anonymous delegates and iterators, these members should be removed from member lists.
    /// They stick around as PrivateHelperMembers of the methods containing the anonymous delegates.
    /// </summary>
    Dictionary<uint, IMethodDefinition> helperMethods;

    /// <summary>
    /// Contains an entry for every field that has been introduced by the compiler in order to implement anonymous delegates.
    /// Since decompilation re-introduces the anonymous delegates and iterators, these members should be removed from member lists.
    /// They stick around as PrivateHelperMembers of the methods containing the anonymous delegates.
    /// </summary>
    Dictionary<IFieldDefinition, IFieldDefinition> helperFields;

    /// <summary>
    /// Allocatates a traverser for a mutable code model that removes a specified set of types from the model.
    /// </summary>
    /// <param name="helperTypes">A dictionary whose keys are the interned keys of the types to remove from member lists.</param>
    /// <param name="helperMethods">A dictionary whose keys are the interned keys of the methods to remove from member lists.</param>
    /// <param name="helperFields">A dictionary whose keys are the interned keys of the methods to remove from member lists.</param>
    internal RemoveUnnecessaryTypes(Dictionary<uint, ITypeDefinition> helperTypes, Dictionary<uint, IMethodDefinition> helperMethods,
      Dictionary<IFieldDefinition, IFieldDefinition> helperFields) {
      this.helperTypes = helperTypes;
      this.helperMethods = helperMethods;
      this.helperFields = helperFields;
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
          this.Visit((ITypeDefinition)nestedType);
      }
      for (int i = 0; i < mutableTypeDefinition.Methods.Count; i++) {
        var helperMethod = mutableTypeDefinition.Methods[i];
        if (this.helperMethods.ContainsKey(helperMethod.InternedKey)) {
          mutableTypeDefinition.Methods.RemoveAt(i);
          i--;
        }
      }
      for (int i = 0; i < mutableTypeDefinition.Fields.Count; i++) {
        var helperField = mutableTypeDefinition.Fields[i];
        if (this.helperFields.ContainsKey(helperField)) {
          mutableTypeDefinition.Fields.RemoveAt(i);
          i--;
        }
      }
    }

  }

}