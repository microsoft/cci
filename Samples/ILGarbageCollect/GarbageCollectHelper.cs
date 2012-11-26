using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using Microsoft.Cci;

namespace ILGarbageCollect {
  public class GarbageCollectHelper {

    /// <summary>
    /// Utility function to iterate over the direct base classes (including interfaces) of a type
    /// definition.
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    [Pure]
    internal static IEnumerable<ITypeDefinition> BaseClasses(ITypeDefinition t) {
      Contract.Requires(t != null);
      Contract.Requires(!(t is Dummy));

      foreach (var b in t.ResolvedType.BaseClasses) yield return b.ResolvedType;
      foreach (var b in t.ResolvedType.Interfaces) yield return b.ResolvedType;
    }

    /// <summary>
    /// Utility function to iterate over all super types (including interfaces) of a type
    /// definition. Note: we don't count a type as a super type of itself.
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    internal static ISet<ITypeDefinition> AllSuperTypes(ITypeDefinition t) {
      Contract.Requires(t != null);
      Contract.Requires(!(t is Dummy));

      ISet<ITypeDefinition> collectedSuperTypes = new HashSet<ITypeDefinition>();

      CollectAllSuperTypes(collectedSuperTypes, t);

      return collectedSuperTypes;
    }

    private static void CollectAllSuperTypes(ISet<ITypeDefinition> collectedTypes, ITypeDefinition t) {
      foreach (ITypeDefinition directSuperType in BaseClasses(t)) {
        collectedTypes.Add(directSuperType);
        CollectAllSuperTypes(collectedTypes, directSuperType);
      }
    }

    /// <summary>
    /// Returns all super classes, in ascending order (i.e. System.Object is last) of a given class.
    /// Note: we don't count a type as a super class of itself
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    internal static IList<ITypeDefinition> AllSuperClasses(ITypeDefinition t) {
      List<ITypeDefinition> superClasses = new List<ITypeDefinition>();

      CollectAllSuperClasses(superClasses, t);

      return superClasses;
    }

    internal static ITypeDefinition InstantiatedTypeIfPossible(ITypeDefinition type) {
      ITypeReference fullyInstantiatedSpecializedTypeReference;

      if (TypeHelper.TryGetFullyInstantiatedSpecializedTypeReference(type, out fullyInstantiatedSpecializedTypeReference)) {
        return fullyInstantiatedSpecializedTypeReference.ResolvedType;
      }
      else {
        return type;
      }
    }

    private static void CollectAllSuperClasses(List<ITypeDefinition> collectedTypes, ITypeDefinition t) {
      Contract.Requires(t.BaseClasses.Count() <= 1);

      if (t.BaseClasses.Count() > 0) {
        ITypeDefinition superClassTypeDefinition = t.BaseClasses.First().ResolvedType;
        collectedTypes.Add(superClassTypeDefinition);
        CollectAllSuperClasses(collectedTypes, superClassTypeDefinition);
      }
    }


    /// <summary>
    /// Find possible implementations of m for derived, upto (and including) 'upto').
    /// 
    /// If m is an unspecialized generic interface, they may be multiple possible implementations; at this
    /// point we can't tell which one would be called since we've removed all specialization from m.
    /// 
    /// We require 'derived' to be unspecialized, but note that its super types may be specialized.
    /// We require 'm' to be unspecialized.
    /// 
    /// </summary>
    internal static ICollection<IMethodDefinition> Implements(ITypeDefinition derived, ITypeDefinition upto, IMethodDefinition m) {
      Contract.Requires(derived != null);
      Contract.Requires(!(derived is Dummy));
      Contract.Requires(upto != null);
      Contract.Requires(!(upto is Dummy));
      Contract.Requires(m != null);
      Contract.Requires(!(m is Dummy));
      Contract.Requires(GarbageCollectHelper.TypeDefinitionIsUnspecialized(derived));

      Contract.Requires(GarbageCollectHelper.MethodDefinitionIsUnspecialized(m));

      Contract.Requires(!derived.IsInterface);

      Contract.Ensures(Contract.ForAll(Contract.Result<ICollection<IMethodDefinition>>(), resultM => GarbageCollectHelper.MethodDefinitionIsUnspecialized(resultM)));
      Contract.Ensures(Contract.ForAll(Contract.Result<ICollection<IMethodDefinition>>(), resultM =>
        resultM != null && !(resultM is Dummy))
      );

      ISet<IMethodDefinition> foundImplementations = new HashSet<IMethodDefinition>();

      // If derived implements an interface multiple times, there may be multiple specialized versions for derived
      IEnumerable<IMethodDefinition> versionsOfMSpecializedForDerived = SearchSpecializedHierarchyForVersionOfMethod(derived, m);

      var classHierarchyChain = new ITypeDefinition[] { derived }.Concat(GarbageCollectHelper.AllSuperClasses(derived));

      foreach (IMethodDefinition mSpecializedForDerived in versionsOfMSpecializedForDerived) {

        IMethodDefinition foundImplementation = null;

        // If this is a method defined on an inteface, we must first search the hierarchy for explicit implementations,
        // since an explicit implementation on a base type supercedes an implicit implementation on a derived type

        if (m.ContainingTypeDefinition.IsInterface) {
          foreach (var classInHierarchy in classHierarchyChain) {
            foreach (IMethodImplementation methodImplementation in classInHierarchy.ExplicitImplementationOverrides) {
              if (methodImplementation.ImplementedMethod.InternedKey == mSpecializedForDerived.InternedKey) {
                foundImplementation = methodImplementation.ImplementingMethod.ResolvedMethod;
                break;
              }
            }
            if (foundImplementation != null) break;
            if (TypeHelper.TypesAreEquivalent(GarbageCollectHelper.UnspecializeAndResolveTypeReference(classInHierarchy), upto)) break;
          }
        }

        // If we found an explicit implementation, don't seach for an implicit one

        if (foundImplementation == null) {
          foreach (var classInHierarchy in classHierarchyChain) {
            foundImplementation = ImplementationForMethodInClass(mSpecializedForDerived, classInHierarchy);
            if (foundImplementation != null) break;
            if (TypeHelper.TypesAreEquivalent(GarbageCollectHelper.UnspecializeAndResolveTypeReference(classInHierarchy), upto)) break;
          }
        }

        // Do we really expect to find an implementation for EACH mSpecializedForDerived; or do we expect to find at least one overall all?
        Contract.Assert(foundImplementation != null);

        foundImplementations.Add(GarbageCollectHelper.UnspecializeAndResolveMethodReference(foundImplementation));

      }

      return foundImplementations;
    }


    /// <summary>
    /// A version of Implements that works over instantiated types and methods.
    /// 
    /// t-devinc: Not entirely convinced this is correct.
    /// </summary>
    /// <param name="derived"></param>
    /// <param name="upto"></param>
    /// <param name="m"></param>
    /// <returns></returns>
    internal static IMethodDefinition ImplementsInstantiated(ITypeDefinition derived, IMethodDefinition m) {
      Contract.Requires(derived != null);
      Contract.Requires(!(derived is Dummy));
      Contract.Requires(m != null);
      Contract.Requires(!(m is Dummy));

      Contract.Requires(TypeHelper.Type1DerivesFromOrIsTheSameAsType2(derived, m.ContainingTypeDefinition));

      Contract.Requires(!derived.IsInterface);

      Contract.Ensures(Contract.Result<IMethodDefinition>() != null);
      Contract.Ensures(!(Contract.Result<IMethodDefinition>() is Dummy));

      var classHierarchyChain = new ITypeDefinition[] { derived }.Concat(GarbageCollectHelper.AllSuperClasses(derived));

      foreach (var classInHierarchy in classHierarchyChain) {

        IMethodDefinition specializedImplementation = ImplementationForMethodInClass(m, classInHierarchy);

        if (specializedImplementation != null) {
          return specializedImplementation;
        }
      }

      // We shouldn't get here
      return null;
    }


    static private IMethodDefinition ImplementationForMethodInClass(IMethodDefinition method, ITypeDefinition classDefinition) {

      // get generic version of instantiated method, if needed (i.e. go from Foo<String>.M<List>() to Foo<String>.M<T>()
      // This may not deal with specialized methods properly.

      if (method is IGenericMethodInstance) {
        method = ((IGenericMethodInstance)method).GenericMethod.ResolvedMethod;
      }

      // Searches the explicit overrides and implicit implementations in the class for one that implements specializedMethod

      foreach (IMethodImplementation methodImplementation in classDefinition.ExplicitImplementationOverrides) {
        if (methodImplementation.ImplementedMethod.InternedKey == method.InternedKey) {
          return methodImplementation.ImplementingMethod.ResolvedMethod;
        }
      }

      // t-devinc: We should probably use MemberHelper.GetImplicitlyOverridingDerivedClassMethod here
      // cteidt: ... but it returns null if the implicitly overriding method is declared newSlot
      // (which can still override an interface method), so the code is duplicated here, without
      // the newSlot check

      foreach (ITypeDefinitionMember derivedMember in classDefinition.GetMembersNamed(method.Name, false)) {
        IMethodDefinition/*?*/ derivedMethod = derivedMember as IMethodDefinition;
        if (derivedMethod == null || !derivedMethod.IsVirtual) continue;
        if (MemberHelper.MethodsAreEquivalent(method, derivedMethod)) {
          return derivedMethod;
        }
      }

      return null;
    }


    /// <summary>
    /// Search the hierarchy of type for a method whose unspecialized version is the given unspecializedMethod.
    /// 
    /// Although type must not be specialized, its super types may be specialized; if so, the returned method is
    /// specialized, otherwise the returned method will be exactly unspecializedMethod.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="unspecializedMethod"></param>
    /// <returns></returns>

    /* Example:
     * class AbstractList<T> {
     *   void Contains(T t) {}
     * }
     * 
     * class LinkedList<T> : AbstractList<T> {
     *   void Contains(T t) {}
     * }
     * 
     * class StringList : LinkedList<String> {
     *   void Contains(String s) {}
     * }
     * 
     * Here SearchSpecializedHierarchyForVersionOfMethod(StringList, AbstractList`1::Contains) will return AbstractList<String>::Contains.
     */

    /*  t-devinc: There might be more than one:
     * 
     *  interface HasM<T> {
        void M(T t);
      }

      class FooWithM : HasM<string>, HasM<int> {
        void M(string s) { }

        void M(int i) { }
      }
     */

    internal static IEnumerable<IMethodDefinition> SearchSpecializedHierarchyForVersionOfMethod(ITypeDefinition rootType, IMethodDefinition unspecializedMethod) {
      Contract.Requires(MethodDefinitionIsUnspecialized(unspecializedMethod));
      Contract.Requires(TypeDefinitionIsUnspecialized(rootType));

      Contract.Ensures(Contract.ForAll<IMethodDefinition>(Contract.Result<IEnumerable<IMethodDefinition>>(), m => UnspecializeAndResolveMethodReference(m) == unspecializedMethod));

      // NB: unspecializedMethod can be an interface method

      // The super types may be specialized
      IEnumerable<ITypeDefinition> ancestorTypesToSearch = new ITypeDefinition[] { rootType }.Concat(AllSuperTypes(rootType));

      ISet<IMethodDefinition> specializedVersions = new HashSet<IMethodDefinition>(new MethodDefinitionEqualityComparer());

      foreach (ITypeDefinition specializedAncestor in ancestorTypesToSearch) {
        foreach (IMethodDefinition method in specializedAncestor.Methods) {
          if (UnspecializeAndResolveMethodReference(method) == unspecializedMethod) {
            specializedVersions.Add(method);
          }
        }
      }

      return specializedVersions;
    }

    [Pure]
    public static bool MethodDefinitionIsUnspecialized(IMethodDefinition definition) {
      return (UnspecializeAndResolveMethodReference(definition) == definition);
    }

    [Pure]
    public static bool FieldDefinitionIsUnspecialized(IFieldDefinition definition) {
      return (UnspecializeAndResolveFieldReference(definition) == definition);
    }

    [Pure]
    public static bool TypeDefinitionIsUnspecialized(ITypeDefinition definition) {
      return (UnspecializeAndResolveTypeReference(definition) == definition);
    }

    [Pure]
    public static bool TypeIsConstructable(ITypeDefinition type) {
      if (!type.IsAbstract && (type.IsClass || type.IsStruct || type.IsDelegate)) {

        // Exclude funky VB type with no finalizer
        if (!(type is INamedTypeDefinition && ((INamedTypeDefinition)type).Name.Value == "<Module>")) {
          return true;
        }
      }

      return false;
    }


    internal static IMethodDefinition GetStaticConstructor(INameTable nametable, ITypeDefinition t) {
      Contract.Requires(nametable != null);
      Contract.Requires(t != null);
      Contract.Requires(!(t is Dummy));
      Contract.Ensures(Contract.Result<IMethodDefinition>() != null);

      // return the "first" we find.  will be eithe r0 or 1 static constructor.
      foreach (var cctor in t.ResolvedType.GetMembersNamed(nametable.Cctor, false))
        return cctor as IMethodDefinition;
      return Dummy.Method;
    }


    internal static ISet<IAssembly> CloseAndResolveOverReferencedAssemblies(IAssembly rootAssembly) {
      return CloseAndResolveOverReferencedAssemblies(new IAssembly[] { rootAssembly });
    }

    internal static ISet<IAssembly> CloseAndResolveOverReferencedAssemblies(IEnumerable<IAssembly> rootAssemblies) {

      HashSet<IAssembly> collectedAssemblies = new HashSet<IAssembly>();

      foreach (IAssembly rootAssembly in rootAssemblies) {
        CloseAndResolveOverReferencedAssembliesHelper(collectedAssemblies, rootAssembly);
      }

      return collectedAssemblies;
    }

    private static void CloseAndResolveOverReferencedAssembliesHelper(ISet<IAssembly> collectedAssemblies, IAssembly assembly) {
      Contract.Requires(!(assembly is Dummy));
      Contract.Ensures(collectedAssemblies.Contains(assembly));

      if (collectedAssemblies.Contains(assembly)) {
        return; // Base Case
      }
      else {
        collectedAssemblies.Add(assembly);

        foreach (IAssemblyReference referencedAssemblyReference in assembly.AssemblyReferences) {
          IAssembly referencedAssembly = referencedAssemblyReference.ResolvedAssembly;

          if (!(referencedAssembly is Dummy)) {
            CloseAndResolveOverReferencedAssembliesHelper(collectedAssemblies, referencedAssembly);
          }
          else {
            throw new Exception("Couldn't resolve assembly " + referencedAssemblyReference + " referenced in " + assembly);
          }
        }
      }

      return; // Recursive Case
    }

    [Pure]
    internal static IMethodDefinition UnspecializeAndResolveMethodReference(IMethodReference methodReference) {
      Contract.Requires(!(methodReference is Dummy));
      Contract.Ensures(!(Contract.Result<IMethodDefinition>() is Dummy));

      IMethodReference unspecializedReference;

      if (methodReference is ISpecializedMethodReference) {
        unspecializedReference = ((ISpecializedMethodReference)methodReference).UnspecializedVersion;

        Contract.Assert(!(unspecializedReference is ISpecializedMethodReference));
      }
      else {
        unspecializedReference = methodReference;
      }

      IMethodDefinition resolvedDefinition = unspecializedReference.ResolvedMethod;

      IMethodDefinition unspecializedDefinition;

      if (resolvedDefinition is IGenericMethodInstance) {
        unspecializedDefinition = UnspecializeAndResolveMethodReference(((IGenericMethodInstance)resolvedDefinition).GenericMethod);
      }
      else {
        unspecializedDefinition = resolvedDefinition;
      }

      if (unspecializedDefinition is Dummy) {
        unspecializedDefinition = null;
      }

      return unspecializedDefinition;
    }

    [Pure]
    internal static IFieldDefinition UnspecializeAndResolveFieldReference(IFieldReference fieldReference) {
      Contract.Requires(!(fieldReference is Dummy));
      Contract.Ensures(!(Contract.Result<IFieldDefinition>() is Dummy));

      IFieldReference unspecializedReference;

      if (fieldReference is ISpecializedFieldReference) {
        unspecializedReference = ((ISpecializedFieldReference)fieldReference).UnspecializedVersion;
      }
      else {
        unspecializedReference = fieldReference;
      }

      IFieldDefinition resolvedDefinition = unspecializedReference.ResolvedField;


      if (resolvedDefinition is Dummy) {
        resolvedDefinition = null;
      }

      return resolvedDefinition;
    }

    [Pure]
    internal static ITypeDefinition UnspecializeAndResolveTypeReference(ITypeReference typeReference) {
      Contract.Requires(!(typeReference is Dummy));
      Contract.Ensures(!(Contract.Result<ITypeDefinition>() is Dummy));


      // t-devinc: We don't currently unspecialize. Need to determine if that is needed.
      ITypeDefinition resolvedTypeDefinition = typeReference.ResolvedType;

      if (!(resolvedTypeDefinition is Dummy)) {

        if (resolvedTypeDefinition is IGenericTypeInstance) {
          return UnspecializeAndResolveTypeReference(((IGenericTypeInstance)resolvedTypeDefinition).GenericType);
        }
        else if (resolvedTypeDefinition is ISpecializedNestedTypeDefinition) {
          return UnspecializeAndResolveTypeReference(((ISpecializedNestedTypeDefinition)resolvedTypeDefinition).UnspecializedVersion);
        }
        else {
          return resolvedTypeDefinition;
        }


      }
      else {
        Console.WriteLine("Warning: Couldn't resolve type reference {0} (UNSOUND).", typeReference);
        // Not quite sure how to to deal with this kind of error.
        // If we've gotten here it means either:
        // 1) Somehow the user has screwed up and we don't have access to the dlls we need to resolve references, OR
        // 2) We've screwed up and have tried to resolve something that should be resolved.
        return null;
      }

    }

    // For debugging purposes only
    internal static bool MethodDefinitionHasName(IMethodDefinition methodDefinition, string className, string methodName) {
      IMethodDefinition unspecializedMethodDefinition = GarbageCollectHelper.UnspecializeAndResolveMethodReference(methodDefinition);

      if (unspecializedMethodDefinition.Name.Value.Equals(methodName)) {
        INamedTypeDefinition containingType = unspecializedMethodDefinition.ContainingTypeDefinition as INamedTypeDefinition;

        if (containingType != null && containingType.Name.Value.Equals(className)) {
          return true;
        }
      }
      return false;
    }


    public static string GetIDStringForReference(IReference reference) {
      if (reference is ITypeReference) {
        return TypeHelper.GetTypeName((ITypeReference)reference, NameFormattingOptions.DocumentationId);
      }
      else if (reference is IMethodReference) {
        return MemberHelper.GetMethodSignature((IMethodReference)reference, NameFormattingOptions.DocumentationId);
      }
      else if (reference is IFieldDefinition) {
        return MemberHelper.GetMemberSignature((IFieldReference)reference, NameFormattingOptions.DocumentationId);
      }
      else {
        throw new ArgumentException("Un-supported reference type: " + reference.GetType());
      }
    }

    internal static INamespaceTypeReference CreateTypeReference(IMetadataHost host, IAssemblyReference assemblyReference, string typeName) {
      return CreateTypeReference(host, assemblyReference, typeName, 0);
    }

    internal static INamespaceTypeReference CreateTypeReference(IMetadataHost host, IAssemblyReference assemblyReference, string typeName, ushort genericParameterCount) {
      IUnitNamespaceReference ns = new Microsoft.Cci.Immutable.RootUnitNamespaceReference(assemblyReference);
      string[] names = typeName.Split('.');
      for (int i = 0, n = names.Length - 1; i < n; i++)
        ns = new Microsoft.Cci.Immutable.NestedUnitNamespaceReference(ns, host.NameTable.GetNameFor(names[i]));
      return new Microsoft.Cci.Immutable.NamespaceTypeReference(host, ns, host.NameTable.GetNameFor(names[names.Length - 1]), genericParameterCount, false, false, true, PrimitiveTypeCode.NotPrimitive);
    }

    public static bool AssemblyMayBeSystemOrFramework(IAssembly assembly) {
      // Heuristic and likely to be wrong

      string assemblyLocation = assembly.Location;
      string windowsRootPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

      //Console.WriteLine("assembly location is {0}", assemblyLocation);

      //string dotNetFrameworkPath = Path.Combine(windowsRootPath, "Microsoft.NET", "Framework");

      if (assemblyLocation.StartsWith(windowsRootPath)) {
        return true;
      }

      return false;
    }
  }

  // Should dump the definition equality comparers and just
  // use reference equality comparers, if possible.

  public class MethodDefinitionEqualityComparer : IEqualityComparer<IMethodDefinition> {

    public bool Equals(IMethodDefinition x, IMethodDefinition y) {
      return x.InternedKey == y.InternedKey;
    }

    public int GetHashCode(IMethodDefinition obj) {
      return (int)obj.InternedKey;
    }
  }

  public class TypeDefinitionEqualityComparer : IEqualityComparer<ITypeDefinition> {
    public bool Equals(ITypeDefinition x, ITypeDefinition y) {
      return x.InternedKey == y.InternedKey;
    }

    public int GetHashCode(ITypeDefinition obj) {
      return (int)obj.InternedKey;
    }
  }

  public class FieldDefinitionEqualityComparer : IEqualityComparer<IFieldDefinition> {
    public bool Equals(IFieldDefinition x, IFieldDefinition y) {
      return x.InternedKey == y.InternedKey;
    }

    public int GetHashCode(IFieldDefinition obj) {
      return (int)obj.InternedKey;
    }
  }

  public class DefinitionEqualityComparer : IEqualityComparer<IDefinition> {
    public bool Equals(IDefinition x, IDefinition y) {
      if (x is IMethodDefinition && y is IMethodDefinition) {
        return ((IMethodDefinition)x).InternedKey == ((IMethodDefinition)y).InternedKey;
      }
      else if (x is ITypeDefinition && y is ITypeDefinition) {
        return ((ITypeDefinition)x).InternedKey == ((ITypeDefinition)y).InternedKey;
      }
      else if (x is IFieldDefinition && y is IFieldDefinition) {
        return ((IFieldDefinition)x).InternedKey == ((IFieldDefinition)y).InternedKey;
      }
      else {
        return x == y;
      }
    }

    public int GetHashCode(IDefinition obj) {
      if (obj is IMethodDefinition) {
        return (int)((IMethodDefinition)obj).InternedKey;
      }
      else if (obj is ITypeDefinition) {
        return (int)((ITypeDefinition)obj).InternedKey;
      }
      else if (obj is IFieldDefinition) {
        return (int)((IFieldDefinition)obj).InternedKey;
      }
      else {
        return obj.GetHashCode();
      }
    }
  }

  public class ReferenceEqualityComparer : IEqualityComparer<IReference> {
    public bool Equals(IReference x, IReference y) {
      if (x is IMethodReference && y is IMethodReference) {
        return ((IMethodReference)x).InternedKey == ((IMethodReference)y).InternedKey;
      }
      else if (x is ITypeReference && y is ITypeReference) {
        return ((ITypeReference)x).InternedKey == ((ITypeReference)y).InternedKey;
      }
      else if (x is IFieldReference && y is IFieldReference) {
        return ((IFieldReference)x).InternedKey == ((IFieldReference)y).InternedKey;
      }
      else {
        return x == y;
      }
    }

    public int GetHashCode(IReference obj) {
      if (obj is IMethodReference) {
        return (int)((IMethodReference)obj).InternedKey;
      }
      else if (obj is ITypeReference) {
        return (int)((ITypeReference)obj).InternedKey;
      }
      else if (obj is IFieldReference) {
        return (int)((IFieldReference)obj).InternedKey;
      }
      else {
        return obj.GetHashCode();
      }
    }
  }
}
