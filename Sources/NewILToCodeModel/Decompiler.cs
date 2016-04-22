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
using System.Diagnostics.Contracts;
using System;

namespace Microsoft.Cci.ILToCodeModel {

  /// <summary>
  /// Options that are used to control how much decompilation happens.
  /// </summary>
  [Flags]
  public enum DecompilerOptions {
    /// <summary>
    /// Default value: all flags are false.
    /// </summary>
    None = 0,
    /// <summary>
    /// True if display classes should be decompiled into anonymous delegates.
    /// </summary>
    AnonymousDelegates = 1,
    /// <summary>
    /// True if iterator classes should be decompiled into iterator methods.
    /// </summary>
    Iterators = AnonymousDelegates << 1,
    /// <summary>
    /// True if loop structures should be decompiled into high-level loops (for-statements, while-statements, etc.)
    /// </summary>
    Loops = Iterators << 1,
    /// <summary>
    /// True if all explicit mention of the stack should be decompiled into assignments/uses of locals.
    /// </summary>
    Unstack = Loops << 1,
    /// <summary>
    /// The final methods are not going to be modified, so provide original IL instructions and handler data.
    /// </summary>
    ReadOnly = Unstack << 1,
  }

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
    /// <param name="options">Set of options that control decompilation.</param>
    [ContractVerification(false)]
    public static Assembly GetCodeModelFromMetadataModel(IMetadataHost host, IAssembly assembly, PdbReader/*?*/ pdbReader, DecompilerOptions options = DecompilerOptions.None) {
      Contract.Requires(host != null);
      Contract.Requires(assembly != null);
      Contract.Requires(!(assembly is Dummy));
      Contract.Ensures(Contract.Result<Assembly>() != null);

      return (Assembly)GetCodeModelFromMetadataModelHelper(host, assembly, pdbReader, pdbReader, options);
    }

    /// <summary>
    /// Returns a mutable Code Model module that is equivalent to the given Metadata Model module,
    /// except that in the new module method bodies also implement ISourceMethodBody.
    /// </summary>
    /// <param name="host">An object representing the application that is hosting this decompiler. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.</param>
    /// <param name="module">The root of the Metadata Model to be converted to a Code Model.</param>
    /// <param name="pdbReader">An object that can map offsets in an IL stream to source locations and block scopes. May be null.</param>
    /// <param name="options">Set of options that control decompilation.</param>
    [ContractVerification(false)]
    public static Module GetCodeModelFromMetadataModel(IMetadataHost host, IModule module, PdbReader/*?*/ pdbReader, DecompilerOptions options = DecompilerOptions.None) {
      Contract.Requires(host != null);
      Contract.Requires(module != null);
      Contract.Requires(!(module is Dummy));
      Contract.Ensures(Contract.Result<Module>() != null);

      return GetCodeModelFromMetadataModelHelper(host, module, pdbReader, pdbReader, options);
    }

    /// <summary>
    /// Returns a (mutable) Code Model SourceMethod body that is equivalent to the given Metadata Model method body.
    /// It does *not* delete any helper types.
    /// </summary>
    /// <param name="host">An object representing the application that is hosting this decompiler. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.</param>
    /// <param name="methodBody">The Metadata Model method body that is to be decompiled.</param>
    /// <param name="pdbReader">An object that can map offsets in an IL stream to source locations and block scopes. May be null.</param>
    /// <param name="options">Set of options that control decompilation.</param>
    public static ISourceMethodBody GetCodeModelFromMetadataModel(IMetadataHost host, IMethodBody methodBody, PdbReader/*?*/ pdbReader, DecompilerOptions options = DecompilerOptions.None) {
      Contract.Requires(host != null);
      Contract.Requires(methodBody != null);

      return new Microsoft.Cci.ILToCodeModel.SourceMethodBody(methodBody, host, pdbReader, pdbReader, options);
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
    /// <param name="options">Set of options that control decompilation.</param>
    [ContractVerification(false)]
    private static Module GetCodeModelFromMetadataModelHelper(IMetadataHost host, IModule module,
      ISourceLocationProvider/*?*/ sourceLocationProvider, ILocalScopeProvider/*?*/ localScopeProvider, DecompilerOptions options) {
      Contract.Requires(host != null);
      Contract.Requires(module != null);
      Contract.Requires(!(module is Dummy));
      Contract.Ensures(Contract.Result<Module>() != null);

      var result = new MetadataDeepCopier(host).Copy(module);
      var replacer = new ReplaceMetadataMethodBodiesWithDecompiledMethodBodies(host, sourceLocationProvider, localScopeProvider, options);
      replacer.Traverse(result);
      var finder = new HelperTypeFinder(host, sourceLocationProvider);
      finder.Traverse(result);
      Contract.Assume(finder.helperTypes != null);
      Contract.Assume(finder.helperMethods != null);
      Contract.Assume(finder.helperFields != null);
      var remover = new RemoveUnnecessaryTypes(finder.helperTypes, finder.helperMethods, finder.helperFields);
      remover.Traverse(result);
      result.AllTypes.RemoveAll(td => finder.helperTypes.ContainsKey(td.InternedKey)); // depends on RemoveAll preserving order
      return result;
    }

    /// <summary>
    /// An object that can provide information about the local scopes of a method.
    /// </summary>
    public class LocalScopeProvider : ILocalScopeProvider {

      /// <summary>
      /// An object that can provide information about the local scopes of a method.
      /// </summary>
      /// <param name="originalLocalScopeProvider">The local scope provider to use for methods that have not been decompiled.</param>
      public LocalScopeProvider(ILocalScopeProvider originalLocalScopeProvider) {
        Contract.Requires(originalLocalScopeProvider != null);
        this.originalLocalScopeProvider = originalLocalScopeProvider;
      }

      ILocalScopeProvider originalLocalScopeProvider;

      [ContractInvariantMethod]
      private void ObjectInvariant() {
        Contract.Invariant(this.originalLocalScopeProvider != null);
      }


      #region ILocalScopeProvider Members

      /// <summary>
      /// Returns zero or more local (block) scopes, each defining an IL range in which an iterator local is defined.
      /// The scopes are returned by the MoveNext method of the object returned by the iterator method.
      /// The index of the scope corresponds to the index of the local. Specifically local scope i corresponds
      /// to the local stored in field &lt;localName&gt;x_i of the class used to store the local values in between
      /// calls to MoveNext.
      /// </summary>
      /// <param name="methodBody"></param>
      /// <returns></returns>
      public IEnumerable<ILocalScope> GetIteratorScopes(IMethodBody methodBody) {
        var sourceMethodBody = methodBody as Microsoft.Cci.MutableCodeModel.SourceMethodBody;
        if (sourceMethodBody == null) return this.originalLocalScopeProvider.GetIteratorScopes(methodBody);
        return sourceMethodBody.IteratorScopes??Enumerable<ILocalScope>.Empty;
      }

      /// <summary>
      /// Returns zero or more local (block) scopes into which the CLR IL operations in the given method body is organized.
      /// </summary>
      /// <param name="methodBody"></param>
      /// <returns></returns>
      public IEnumerable<ILocalScope> GetLocalScopes(IMethodBody methodBody) {
        var sourceMethodBody = methodBody as Microsoft.Cci.MutableCodeModel.SourceMethodBody;
        if (sourceMethodBody == null) return this.originalLocalScopeProvider.GetLocalScopes(methodBody);
        return sourceMethodBody.LocalScopes??Enumerable<ILocalScope>.Empty;
      }

      /// <summary>
      /// Returns zero or more namespace scopes into which the namespace type containing the given method body has been nested.
      /// These scopes determine how simple names are looked up inside the method body. There is a separate scope for each dotted
      /// component in the namespace type name. For istance namespace type x.y.z will have two namespace scopes, the first is for the x and the second
      /// is for the y.
      /// </summary>
      /// <param name="methodBody"></param>
      /// <returns></returns>
      public IEnumerable<INamespaceScope> GetNamespaceScopes(IMethodBody methodBody) {
        var sourceMethodBody = methodBody as Microsoft.Cci.MutableCodeModel.SourceMethodBody;
        if (sourceMethodBody == null) return this.originalLocalScopeProvider.GetNamespaceScopes(methodBody);
        return sourceMethodBody.NamespaceScopes??Enumerable<INamespaceScope>.Empty;
      }

      /// <summary>
      /// Returns zero or more local constant definitions that are local to the given scope.
      /// </summary>
      /// <param name="scope"></param>
      /// <returns></returns>
      public IEnumerable<ILocalDefinition> GetConstantsInScope(ILocalScope scope) {
        var generatorScope = scope as ILGeneratorScope;
        if (generatorScope == null) return this.originalLocalScopeProvider.GetConstantsInScope(scope);
        return generatorScope.Constants??Enumerable<ILocalDefinition>.Empty;
      }

      /// <summary>
      /// Returns zero or more local variable definitions that are local to the given scope.
      /// </summary>
      /// <param name="scope"></param>
      /// <returns></returns>
      public IEnumerable<ILocalDefinition> GetVariablesInScope(ILocalScope scope) {
        var generatorScope = scope as ILGeneratorScope;
        if (generatorScope == null) return this.originalLocalScopeProvider.GetVariablesInScope(scope);
        return generatorScope.Locals??Enumerable<ILocalDefinition>.Empty;
      }

      /// <summary>
      /// Returns true if the method body is an iterator.
      /// </summary>
      /// <param name="methodBody"></param>
      /// <returns></returns>
      public bool IsIterator(IMethodBody methodBody) {
        var sourceMethodBody = methodBody as Microsoft.Cci.MutableCodeModel.SourceMethodBody;
        if (sourceMethodBody == null) return this.originalLocalScopeProvider.IsIterator(methodBody);
        return sourceMethodBody.IsIterator;
      }

      /// <summary>
      /// If the given method body is the "MoveNext" method of the state class of an asynchronous method, the returned
      /// object describes where synchronization points occur in the IL operations of the "MoveNext" method. Otherwise
      /// the result is null.
      /// </summary>
      public ISynchronizationInformation/*?*/ GetSynchronizationInformation(IMethodBody methodBody) {
        var sourceMethodBody = methodBody as Microsoft.Cci.MutableCodeModel.SourceMethodBody;
        if (sourceMethodBody == null) return this.originalLocalScopeProvider.GetSynchronizationInformation(methodBody);
        return sourceMethodBody.SynchronizationInformation;
      }

      #endregion
    }

  }

  /// <summary>
  /// A mutator that copies metadata models into mutable code models by using the base MetadataMutator class to make a mutable copy
  /// of a given metadata model and also replaces any method bodies with instances of SourceMethodBody, which implements the ISourceMethodBody.Block property
  /// by decompiling the metadata model information provided by the properties of IMethodBody.
  /// </summary>
  internal class ReplaceMetadataMethodBodiesWithDecompiledMethodBodies : MetadataTraverser {

    /// <summary>
    /// An object that can provide information about the local scopes of a method. May be null. 
    /// </summary>
    ILocalScopeProvider/*?*/ localScopeProvider;

    /// <summary>
    /// An object that can map offsets in an IL stream to source locations and block scopes. May be null.
    /// </summary>
    ISourceLocationProvider/*?*/ sourceLocationProvider;

    /// <summary>
    /// An object representing the application that is hosting this mutator. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.
    /// </summary>
    IMetadataHost host;

    /// <summary>
    /// Decompiler options needed at the point that new source method bodies are created for each method definition.
    /// </summary>
    DecompilerOptions options;

    /// <summary>
    /// Allocates a mutator that copies metadata models into mutable code models by using the base MetadataMutator class to make a mutable copy
    /// of a given metadata model and also replaces any method bodies with instances of SourceMethodBody, which implements the ISourceMethodBody.Block property
    /// by decompiling the metadata model information provided by the properties of IMethodBody.
    /// </summary>
    /// <param name="host">An object representing the application that is hosting this mutator. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.</param>
    /// <param name="sourceLocationProvider">An object that can map some kinds of ILocation objects to IPrimarySourceLocation objects. May be null.</param>
    /// <param name="localScopeProvider">An object that can provide information about the local scopes of a method. May be null.</param>
    /// <param name="options">Set of options that control decompilation.</param>
    internal ReplaceMetadataMethodBodiesWithDecompiledMethodBodies(IMetadataHost host,
      ISourceLocationProvider/*?*/ sourceLocationProvider, ILocalScopeProvider/*?*/ localScopeProvider, DecompilerOptions options) {
      Contract.Requires(host != null);

      this.localScopeProvider = localScopeProvider;
      this.sourceLocationProvider = sourceLocationProvider;
      this.host = host;
      this.options = options;
    }

    [ContractInvariantMethod]
    private void ObjectInvariant() {
      Contract.Invariant(this.host != null);
    }


    /// <summary>
    /// Replaces the body of the given method with an equivalent instance of SourceMethod body, which in addition also implements ISourceMethodBody,
    /// which has the additional property, Block, which represents the corresponding Code Model for the method body.
    /// </summary>
    public override void TraverseChildren(IMethodDefinition method) {
      var methodDef = (MethodDefinition)method;
      if (methodDef.IsExternal || methodDef.IsAbstract) return;
      methodDef.Body = new SourceMethodBody(method.Body, this.host, this.sourceLocationProvider, this.localScopeProvider, this.options);
    }

  }

  /// <summary>
  /// A traverser that visits every method body and collects together all of the private helper types of these bodies.
  /// </summary>
  internal sealed class HelperTypeFinder : MetadataTraverser {

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
    /// An object representing the application that is hosting this decompiler. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.
    /// </summary>
    IMetadataHost host;

    /// <summary>
    /// An object that can map some kinds of ILocation objects to IPrimarySourceLocation objects. May be null.
    /// </summary>
    ISourceLocationProvider/*?*/ sourceLocationProvider;

    /// <summary>
    /// A traverser that visits every method body and collects together all of the private helper types of these bodies.
    /// </summary>
    /// <param name="host">An object representing the application that is hosting this decompiler. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.</param>
    /// <param name="sourceLocationProvider">An object that can map some kinds of ILocation objects to IPrimarySourceLocation objects. May be null.</param>
    internal HelperTypeFinder(IMetadataHost host, ISourceLocationProvider/*?*/ sourceLocationProvider) {
      Contract.Requires(host != null);

      this.host = host;
      this.sourceLocationProvider = sourceLocationProvider;
      this.TraverseIntoMethodBodies = true;
    }

    [ContractInvariantMethod]
    private void ObjectInvariant() {
      Contract.Invariant(this.helperTypes != null);
      Contract.Invariant(this.helperMethods != null);
      Contract.Invariant(this.helperFields != null);
      Contract.Invariant(this.host != null);
    }


    /// <summary>
    /// Traverses only the namespace root of the given assembly, removing any type from the model that have the same
    /// interned key as one of the entries of this.typesToRemove.
    /// </summary>
    public override void TraverseChildren(IModule module) {
      this.Traverse(module.UnitNamespaceRoot);
    }

    /// <summary>
    /// Traverses only the nested types and methods and collects together all of the private helper types that are introduced by the compiler
    /// when methods that contain closures or iterators are compiled.
    /// </summary>
    public override void TraverseChildren(INamedTypeDefinition typeDefinition) {
      foreach (ITypeDefinition nestedType in typeDefinition.NestedTypes) {
        Contract.Assume(nestedType != null);
        this.Traverse(nestedType);
      }
      foreach (IMethodDefinition method in typeDefinition.Methods) {
        Contract.Assume(method != null);
        this.Traverse(method);
      }
    }

    /// <summary>
    /// Traverses only the (possibly missing) body of the method.
    /// </summary>
    /// <param name="method"></param>
    public override void TraverseChildren(IMethodDefinition method) {
      if (method.IsAbstract || method.IsExternal) return;
      this.Traverse(method.Body);
    }

    /// <summary>
    /// Records all of the helper types of the method body into this.helperTypes.
    /// </summary>
    /// <param name="methodBody"></param>
    public override void TraverseChildren(IMethodBody methodBody) {
      var mutableBody = (SourceMethodBody)methodBody;
      var block = mutableBody.Block; //force decompilation
      bool denormalize = false;
      if (mutableBody.privateHelperTypesToRemove != null) {
        denormalize = true;
        foreach (var helperType in mutableBody.privateHelperTypesToRemove) {
          Contract.Assume(helperType != null);
          this.helperTypes[helperType.InternedKey] = helperType;
        }
      }
      if (mutableBody.privateHelperMethodsToRemove != null) {
        denormalize = true;
        foreach (var helperMethod in mutableBody.privateHelperMethodsToRemove.Values) {
          Contract.Assume(helperMethod != null);
          this.helperMethods[helperMethod.InternedKey] = helperMethod;
        }
      }
      if (mutableBody.privateHelperFieldsToRemove != null) {
        denormalize = true;
        foreach (var helperField in mutableBody.privateHelperFieldsToRemove)
          this.helperFields[helperField] = helperField;
      }
      if (denormalize) {
        var mutableMethod = (MethodDefinition)mutableBody.MethodDefinition;
        var denormalizedBody = new Microsoft.Cci.MutableCodeModel.SourceMethodBody(this.host, this.sourceLocationProvider);
        denormalizedBody.LocalsAreZeroed = mutableBody.LocalsAreZeroed;
        denormalizedBody.IsNormalized = false;
        denormalizedBody.Block = block;
        denormalizedBody.MethodDefinition = mutableMethod;
        mutableMethod.Body = denormalizedBody;
      }
    }

  }

  /// <summary>
  /// A traverser for a mutable code model that removes a specified set of types from the model.
  /// </summary>
  internal class RemoveUnnecessaryTypes : MetadataTraverser {

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
    /// Allocates a traverser for a mutable code model that removes a specified set of types from the model.
    /// </summary>
    /// <param name="helperTypes">A dictionary whose keys are the interned keys of the types to remove from member lists.</param>
    /// <param name="helperMethods">A dictionary whose keys are the interned keys of the methods to remove from member lists.</param>
    /// <param name="helperFields">A dictionary whose keys are the interned keys of the methods to remove from member lists.</param>
    internal RemoveUnnecessaryTypes(Dictionary<uint, ITypeDefinition> helperTypes, Dictionary<uint, IMethodDefinition> helperMethods,
      Dictionary<IFieldDefinition, IFieldDefinition> helperFields) {
      Contract.Requires(helperTypes != null);
      Contract.Requires(helperMethods != null);
      Contract.Requires(helperFields != null);

      this.helperTypes = helperTypes;
      this.helperMethods = helperMethods;
      this.helperFields = helperFields;
    }

    [ContractInvariantMethod]
    private void ObjectInvariant() {
      Contract.Invariant(this.helperFields != null);
      Contract.Invariant(this.helperMethods != null);
      Contract.Invariant(this.helperTypes != null);
    }


    /// <summary>
    /// Traverses only the namespace root of the given assembly, removing any type from the model that have the same
    /// interned key as one of the entries of this.typesToRemove.
    /// </summary>
    public override void TraverseChildren(IModule module) {
      this.Traverse(module.UnitNamespaceRoot);
    }

    /// <summary>
    /// Traverses the specified type definition, removing any nested types that are compiler introduced private helper types
    /// for maintaining the state of closures and anonymous delegates.
    /// </summary>
    public override void TraverseChildren(INamedTypeDefinition typeDefinition) {
      var mutableTypeDefinition = (NamedTypeDefinition)typeDefinition;
      if (mutableTypeDefinition.NestedTypes != null) {
        for (int i = 0; i < mutableTypeDefinition.NestedTypes.Count; i++) {
          var nestedType = mutableTypeDefinition.NestedTypes[i];
          Contract.Assume(nestedType != null);
          if (this.helperTypes.ContainsKey(nestedType.InternedKey)) {
            mutableTypeDefinition.NestedTypes.RemoveAt(i);
            i--;
          } else
            this.Traverse(nestedType);
        }
      }
      if (mutableTypeDefinition.Methods != null) {
        for (int i = 0; i < mutableTypeDefinition.Methods.Count; i++) {
          var helperMethod = mutableTypeDefinition.Methods[i];
          Contract.Assume(helperMethod != null);
          if (this.helperMethods.ContainsKey(helperMethod.InternedKey)) {
            mutableTypeDefinition.Methods.RemoveAt(i);
            i--;
          }
        }
      }
      if (mutableTypeDefinition.Fields != null) {
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

}