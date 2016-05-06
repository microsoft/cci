//-----------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.  All Rights Reserved.
// This code is licensed under the Microsoft Public License.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//-----------------------------------------------------------------------------
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Cci.Immutable;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci.Ast {

  /// <summary>
  /// A simple name that serves as an alias for an expression supposed to be the name of a namespace or of a type.
  /// In C# this corresponds to the "using aliasName = namespaceOrTypeName;" construct.
  /// </summary>
  public class AliasDeclaration : NamespaceDeclarationMember {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="referencedNamespaceOrType"></param>
    /// <param name="sourceLocation"></param>
    public AliasDeclaration(NameDeclaration name, Expression referencedNamespaceOrType, ISourceLocation sourceLocation)
      : base(name, sourceLocation) {
      this.referencedNamespaceOrType = referencedNamespaceOrType;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="containingNamespaceDeclaration"></param>
    /// <param name="template"></param>
    protected AliasDeclaration(NamespaceDeclaration containingNamespaceDeclaration, AliasDeclaration template)
      : base(containingNamespaceDeclaration, template)
      //^ ensures containingNamespaceDeclaration == this.containingNamespaceDeclaration;
    {
      this.referencedNamespaceOrType = template.referencedNamespaceOrType.MakeCopyFor(containingNamespaceDeclaration.DummyBlock);
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the alias or a constituent part of the alias.
    /// Do not call this method directly, but evaluate the HasErrors property. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return false;
    }

    /// <summary>
    /// Calls the visitor.Visit(AliasDeclaration) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    //^ [MustOverride]
    /// <summary>
    /// Returns a shallow copy of this namespace declarations member with the given namespace declaration as the containing namespace of the copy.
    /// </summary>
    /// <param name="targetNamespaceDeclaration">The namespace declaration that is to be the containing namespace of the copy.</param>
    /// <returns></returns>
    public override NamespaceDeclarationMember MakeShallowCopyFor(NamespaceDeclaration targetNamespaceDeclaration) {
      if (targetNamespaceDeclaration == this.ContainingNamespaceDeclaration) return this;
      return new AliasDeclaration(targetNamespaceDeclaration, this);
    }

    /// <summary>
    /// An expression that, if correct, resolves to an INamespace instance or an ITypeDefinition instance.
    /// </summary>
    public Expression ReferencedNamespaceOrType {
      get {
        return this.referencedNamespaceOrType;
      }
    }
    readonly Expression referencedNamespaceOrType;

    /// <summary>
    /// Resolves the ReferencedNamespaceOrType expression. If the expression fails to resolve an error is generated and a dummy namespace is returned.
    /// </summary>
    public virtual object ResolvedNamespaceOrType {
      get
        //^ ensures result is INamespaceDefinition || result is ITypeDefinition;
      {
        object/*?*/ result;
        if ((result = this.resolvedNamespaceOrType) == null) {
          lock (GlobalLock.LockingObject) {
            if ((result = this.resolvedNamespaceOrType) == null) {
              this.resolvedNamespaceOrType = result = this.Resolve();
              //^ assert result is INamespaceDefinition || result is ITypeDefinition;
            } else {
              //^ assume result is INamespaceDefinition || result is ITypeDefinition; //follows from the invariant
            }
          }
          //^ assert result is INamespaceDefinition || result is ITypeDefinition;
        } else {
          //^ assume result is INamespaceDefinition || result is ITypeDefinition; //follows from the invariant
        }
        //^ assert result != null;
        return result;
      }
    }
    private object/*?*/ resolvedNamespaceOrType;
    //^ invariant resolvedNamespaceOrType == null || resolvedNamespaceOrType is INamespaceDefinition || resolvedNamespaceOrType is ITypeDefinition;

    private object Resolve()
      //^ ensures result is INamespaceDefinition || result is ITypeDefinition;
    {
      this.ContainingNamespaceDeclaration.BusyResolvingAnAliasOrImport = true;
      object/*?*/ result = null;
      SimpleName/*?*/ simpleName = this.ReferencedNamespaceOrType as SimpleName;
      if (simpleName != null) {
        result = simpleName.ResolveAsNamespaceOrType();
        if (!(result is ITypeDefinition || result is INamespaceDefinition))
          result = null;
      }
      if (result == null) {
        QualifiedName/*?*/ qualifiedName = this.ReferencedNamespaceOrType as QualifiedName;
        if (qualifiedName != null) {
          result = qualifiedName.ResolveAsNamespaceOrTypeGroup();
          ITypeGroup typeGroup = result as ITypeGroup;
          if (typeGroup != null) {
            foreach (ITypeDefinition type in typeGroup.GetTypes(0)) {
              result = type;
              break;
            }
          }
          if (!(result is ITypeDefinition || result is INamespaceDefinition))
            result = null;
        }
      }
      if (result == null) {
        AliasQualifiedName/*?*/ aliasQualifiedName = this.ReferencedNamespaceOrType as AliasQualifiedName;
        if (aliasQualifiedName != null) {
          result = aliasQualifiedName.ResolveAsNamespaceOrType();
          if (!(result is ITypeDefinition || result is INamespaceDefinition))
            result = null;
        }
      }
      if (result == null) {
        //TODO: error
        result = Dummy.Type;
      }
      this.ContainingNamespaceDeclaration.BusyResolvingAnAliasOrImport = false;
      return result;
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a namespace member before constructing the namespace.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    /// <param name="containingNamespaceDeclaration">The containing namespace declaration.</param>
    /// <param name="recurse">True if the method should be called recursively on members of nested namespace declarations.</param>
    public override void SetContainingNamespaceDeclaration(NamespaceDeclaration containingNamespaceDeclaration, bool recurse) {
      base.SetContainingNamespaceDeclaration(containingNamespaceDeclaration, recurse);
      if (!recurse) return;
      DummyExpression containingExpression = new DummyExpression(containingNamespaceDeclaration.DummyBlock, SourceDummy.SourceLocation);
      this.referencedNamespaceOrType.SetContainingExpression(containingExpression);
    }

  }

  /// <summary>
  /// 
  /// </summary>
  public interface IAggregatableNamespaceDeclarationMember : INamespaceDeclarationMember, IContainerMember<NamespaceDeclaration> {
    /// <summary>
    /// 
    /// </summary>
    INamespaceMember AggregatedMember { get; }
  }

  /// <summary>
  /// Represents a namespace construct as found in the source code.
  /// </summary>
  public abstract class NamespaceDeclaration : SourceItemWithAttributes, IContainer<IAggregatableNamespaceDeclarationMember>, INamespaceScope, IErrorCheckable {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sourceLocation"></param>
    protected NamespaceDeclaration(ISourceLocation sourceLocation)
      : base(sourceLocation) {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="template"></param>
    protected NamespaceDeclaration(NamespaceDeclaration template)
      : base(template.SourceLocation) {
      this.members = new List<INamespaceDeclarationMember>(template.Members);
      this.sourceAttributes = new List<SourceCustomAttribute>(template.SourceAttributes);
    }

    /// <summary>
    /// A collection of the members, if any, of this namespace that are namespace or type alias declarations.
    /// These correspond to the using id = ... clauses that can follow a C# namespace declaration.
    /// </summary>
    public IEnumerable<AliasDeclaration> Aliases {
      get {
        if (this.aliases == null)
          this.aliases = this.ComputeAliases();
        return this.aliases;
      }
    }
    IEnumerable<AliasDeclaration> aliases;

    private Microsoft.Cci.UtilityDataStructures.Hashtable<AliasDeclaration> CaseInsensitiveAliasTable {
      get {
        if (this.caseInsensitiveAliasTable == null) {
          this.caseInsensitiveAliasTable = this.ComputeAliasTable(true);
        }
        return this.caseInsensitiveAliasTable;
      }
    }
    Microsoft.Cci.UtilityDataStructures.Hashtable<AliasDeclaration> caseInsensitiveAliasTable;

    private Microsoft.Cci.UtilityDataStructures.Hashtable<AliasDeclaration> CaseSensitiveAliasTable {
      get {
        if (this.caseSensitiveAliasTable == null) {
          this.caseSensitiveAliasTable = this.ComputeAliasTable(false);
        }
        return this.caseSensitiveAliasTable;
      }
    }
    Microsoft.Cci.UtilityDataStructures.Hashtable<AliasDeclaration> caseSensitiveAliasTable;

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the member or a constituent part of the member.
    /// Do not call this method directly, but evaluate the HasErrors property. The latter will cache the return value.
    /// </summary>
    protected virtual bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      bool result = false;
      foreach (var alias in this.Aliases)
        result |= alias.HasErrors;
      foreach (var import in this.Imports)
        result |= import.HasErrors;
      foreach (var member in this.Members)
        result |= member.HasErrors;
      foreach (var attribute in this.SourceAttributes)
        result |= attribute.HasErrors;
      foreach (var unitSetAlias in this.UnitSetAliases)
        result |= unitSetAlias.HasErrors;
      return result;
    }

    private Microsoft.Cci.UtilityDataStructures.Hashtable<AliasDeclaration> ComputeAliasTable(bool ignoreCase) {
      var result = new Microsoft.Cci.UtilityDataStructures.Hashtable<AliasDeclaration>();
      foreach (AliasDeclaration alias in this.Aliases) {
        uint key = ignoreCase ? (uint)alias.Name.UniqueKeyIgnoringCase : (uint)alias.Name.UniqueKey;
        result.Add(key, alias);
      }
      return result;
    }

    private IEnumerable<AliasDeclaration> ComputeAliases() {
      var aliasList = new List<AliasDeclaration>();
      foreach (var member in this.Members) {
        var alias = member as AliasDeclaration;
        if (alias == null) continue;
        aliasList.Add(alias);
      }
      aliasList.TrimExcess();
      return aliasList.AsReadOnly();
    }

    /// <summary>
    /// Returns a list of custom attributes that describes this type declaration member.
    /// Typically, these will be derived from this.SourceAttributes. However, some source attributes
    /// might instead be persisted as metadata bits and other custom attributes may be synthesized
    /// from information not provided in the form of source custom attributes.
    /// The list is not trimmed to size, since an override of this method may call the base method
    /// and then add more attributes.
    /// </summary>
    protected override List<ICustomAttribute> GetAttributes() {
      var result = new List<ICustomAttribute>();
      foreach (var sourceAttribute in this.SourceAttributes) {
        if (sourceAttribute.HasErrors) continue;
        result.Add(new CustomAttribute(sourceAttribute));
      }
      //TODO: suppress pseudo attributes and add in synthesized ones.
      return result;
    }

    /// <summary>
    /// If this is true, the alias declarations and namespace imports of this namespace declaration must be ignored when resolving
    /// simple names.
    /// </summary>
    internal bool BusyResolvingAnAliasOrImport;

    /// <summary>
    /// The compilation to which this namespace declaration belongs.
    /// </summary>
    public Compilation Compilation {
      get {
        return this.CompilationPart.Compilation;
      }
    }

    /// <summary>
    /// The compilation part that contains this namespace declaration.
    /// </summary>
    public abstract CompilationPart CompilationPart { get; }

    /// <summary>
    /// Calls the visitor.Visit(NamespaceDeclaration) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// A block statement that serves as the declaring block of any expressions that form part of the the namespace declaration
    /// but that do not appear inside a method body.
    /// </summary>
    public abstract BlockStatement DummyBlock { get; }

    /// <summary>
    /// Returns the IAliasDeclaration or IUnitSetAliasDeclaration, if any, with the given name, ignoring the case of the name if so instructed.
    /// </summary>
    public void GetAliasNamed(IName name, bool ignoreCase, ref AliasDeclaration/*?*/ aliasDeclaration, ref UnitSetAliasDeclaration/*?*/ unitSetAliasDeclaration) {
      aliasDeclaration = null;
      unitSetAliasDeclaration = null;
      int key = ignoreCase ? name.UniqueKeyIgnoringCase : name.UniqueKey;
      var table = ignoreCase ? this.CaseInsensitiveAliasTable : this.CaseSensitiveAliasTable;
      AliasDeclaration alias = table.Find((uint)key);
      if (alias != null) {
        aliasDeclaration = alias;
        return;
      }
      foreach (var unitSetAliasDecl in this.UnitSetAliases) {
        int mkey = ignoreCase ? unitSetAliasDecl.Name.UniqueKeyIgnoringCase : unitSetAliasDecl.Name.UniqueKey;
        if (key != mkey) continue;
        unitSetAliasDeclaration = unitSetAliasDecl;
        return;
      }
    }

    /// <summary>
    /// Find a method group that is accessible and applicable for the given
    /// simple name and argument list. If no methods found go to enclosing namespaces.
    /// </summary>
    /// <param name="result"></param>
    /// <param name="simpleName"></param>
    /// <param name="arguments"></param>
    public void GetApplicableExtensionMethods(List<IMethodDefinition> result, SimpleName simpleName, IEnumerable<Expression> arguments) {
      // First look for methods in directly enclosed types.
      this.GetExtensionMethodsFromDirectlyEnclosedTypes(result, simpleName, arguments);
      if (result.Count > 0) return;
      // Failing that, look for methods in namespaces in using declarations.
      this.GetExtensionMethodsViaUsingDirectives(result, simpleName, arguments);
      if (result.Count > 0) return;
      // If nothing found recurse to enclosing namespace, if any.
      NestedNamespaceDeclaration nested = this as NestedNamespaceDeclaration;
      if (nested != null)
        nested.ContainingNamespaceDeclaration.GetApplicableExtensionMethods(result, simpleName, arguments);
    }

    class ExtensionMethodScope : Scope<IMethodDefinition> {
      internal void InsertInExtensionScope(IMethodDefinition method) {
        this.AddMemberToCache(method);
      }
    }

    private ExtensionMethodScope extensionMethodsFromEnclosedScope;
    private ExtensionMethodScope extensionMethodsFromUsingDirectives;

    /// <summary>
    /// All the applicable methods found in ALL directly enclosed
    /// classes are aggregated into result method group.
    /// </summary>
    /// <param name="result"></param>
    /// <param name="simpleName"></param>
    /// <param name="arguments"></param>
    private void GetExtensionMethodsFromDirectlyEnclosedTypes(List<IMethodDefinition> result, SimpleName simpleName, IEnumerable<Expression> arguments) {
      IEnumerable<IMethodDefinition> candidates;
      if (extensionMethodsFromEnclosedScope == null)
        this.PopulateExtensionMethodsFromEnclosedScope();
      // Get all the methods with the right simple name
      candidates = this.extensionMethodsFromEnclosedScope.GetMembersNamed(simpleName.Name, false);
      if (candidates != null)
        // Now filter them for applicability with the given argument list
        foreach (IMethodDefinition method in candidates)
          if (this.Helper.MethodIsEligible(method, arguments))
            result.Add(method);      
    }

    private void PopulateExtensionMethodsFromEnclosedScope() {
      extensionMethodsFromEnclosedScope = new ExtensionMethodScope();
      // We look for TypeDeclarations with TypeDefinitions that declare extension methods.
      // This might include methods declared in *other* partials of a directly enclosed class.
      foreach (INamespaceDeclarationMember member in this.Members) {
        TypeDeclaration typeDeclaration = member as TypeDeclaration;
        if (typeDeclaration != null && typeDeclaration.TypeDefinition.HasExtensionMethod) {
          foreach (ITypeDefinitionMember typeMember in typeDeclaration.TypeDefinition.Members) {
            var method = typeMember as MethodDefinition;
            if (method != null && method.IsExtensionMethod)
              extensionMethodsFromEnclosedScope.InsertInExtensionScope(method);
          }
        }
      }
    }

    /// <summary>
    /// All the applicable methods found in ALL directly enclosed
    /// classes in ALL of the included namespaces are aggregated 
    /// into the result method group.
    /// </summary>
    /// <param name="result"></param>
    /// <param name="simpleName"></param>
    /// <param name="arguments"></param>
    private void GetExtensionMethodsViaUsingDirectives(List<IMethodDefinition> result, SimpleName simpleName, IEnumerable<Expression> arguments) {
      IEnumerable<IMethodDefinition> candidates;
      if (extensionMethodsFromUsingDirectives == null)
        this.PopulateExtensionMethodsViaUsingDirectives();
      // Get all the extension methods with the right name.
      candidates = this.extensionMethodsFromEnclosedScope.GetMembersNamed(simpleName.Name, false);
      if (candidates != null)
        // Now filter the methods for applicability.
        foreach (IMethodDefinition method in candidates)
          if (this.Helper.MethodIsEligible(method, arguments))
            result.Add(method);
    }

    private void PopulateExtensionMethodsViaUsingDirectives() {
      extensionMethodsFromUsingDirectives = new ExtensionMethodScope();
      IEnumerable<NamespaceImportDeclaration> imports = this.Imports;
      foreach (NamespaceImportDeclaration import in imports) {
        NamespaceReferenceExpression nsImport = import.ImportedNamespace;
        INamespaceDefinition nsDefinition = nsImport.Resolve();
        if (!(nsDefinition is Dummy)) {
          foreach (INamespaceMember member in nsDefinition.Members) {
            ITypeDefinition typeDefinition = member as ITypeDefinition;
            if (typeDefinition != null && this.HasExtensionMethod(typeDefinition)) {
              foreach (ITypeDefinitionMember typeMember in typeDefinition.Members) {
                IMethodDefinition method = typeMember as IMethodDefinition;
                if (method != null && this.IsExtensionMethod(method))
                  extensionMethodsFromEnclosedScope.InsertInExtensionScope(method);
              }
            }
          }
        }
      }
    }

    private bool IsExtensionMethod(IMethodDefinition method) {
      MethodDefinition methodDef = method as MethodDefinition;
      if (methodDef != null)
        return methodDef.IsExtensionMethod;
      else
        return AttributeHelper.Contains(method.Attributes, this.Helper.PlatformType.SystemRuntimeCompilerServicesExtensionAttribute);
    }

    private bool HasExtensionMethod(ITypeDefinition typeDefinition) {
      NamedTypeDefinition typeDef = typeDefinition as NamedTypeDefinition;
      if (typeDef != null)
        return typeDef.HasExtensionMethod;
      else
        return AttributeHelper.Contains(typeDefinition.Attributes, this.Helper.PlatformType.SystemRuntimeCompilerServicesExtensionAttribute);
    }

    private object/*?*/ GetCachedMethodExtensionGroup(IName methodName) {
      //TODO: implement this
      if (methodName == null) return null;
      return null;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="methodName"></param>
    /// <param name="ignoreCase"></param>
    /// <param name="namespaceDeclaration"></param>
    /// <returns></returns>
    public static object/*?*/ GetMethodExtensionGroup(IName methodName, bool ignoreCase, NamespaceDeclaration namespaceDeclaration) {
      object/*?*/ result = null;
      NamespaceDeclaration/*?*/ nsDecl = namespaceDeclaration as NamespaceDeclaration;
      if (nsDecl != null) {
        result = nsDecl.GetCachedMethodExtensionGroup(methodName);
        if (result != null) return result;
      }
      Function<INamespaceMember, bool> IsStatic = NamespaceDeclaration.IsStatic;
      List<IMethodDefinition>/*?*/ matchingMethods = null;
      foreach (INamespaceMember nsMember in namespaceDeclaration.Scope.GetMatchingMembers(IsStatic)) {
        //^ assume nsMember is ITypeDefinition;
        ITypeDefinition typeDef = (ITypeDefinition)nsMember;
        foreach (ITypeDefinitionMember tMember in typeDef.GetMembersNamed(methodName, ignoreCase)) {
          IMethodDefinition/*?*/ method = tMember as IMethodDefinition;
          if (method == null /*|| !method.IsExtensionMethod*/) continue;
          if (matchingMethods == null) matchingMethods = new List<IMethodDefinition>();
          matchingMethods.Add(method);
        }
      }
      if (matchingMethods == null) return null;
      return null;
      //TODO: construct a method group object
    }

    /// <summary>
    /// Checks the namespace for errors and returns true if any were found.
    /// </summary>
    public bool HasErrors {
      get {
        if (this.hasErrors == null)
          this.hasErrors = this.CheckForErrorsAndReturnTrueIfAnyAreFound();
        return this.hasErrors.Value;
      }
    }
    bool? hasErrors;

    /// <summary>
    /// An instance of a language specific class containing methods that are of general utility. 
    /// </summary>
    public LanguageSpecificCompilationHelper Helper {
      get { return this.CompilationPart.Helper; }
    }

    /// <summary>
    /// A collection of the members, if any, of this namespace that are namespace import declarations.
    /// These correspond to the using clauses that can follow a C# namespace declaration.
    /// </summary>
    public IEnumerable<NamespaceImportDeclaration> Imports {
      get {
        if (this.imports == null)
          this.imports = this.ComputeImportList();
        return this.imports;
      }
    }
    IEnumerable<NamespaceImportDeclaration> imports;

    private IEnumerable<NamespaceImportDeclaration> ComputeImportList() {
      var importList = new List<NamespaceImportDeclaration>();
      foreach (var member in this.Members) {
        NamespaceImportDeclaration/*?*/ namespaceImport = member as NamespaceImportDeclaration;
        if (namespaceImport == null) continue;
        importList.Add(namespaceImport);
      }
      importList.TrimExcess();
      return importList.AsReadOnly();
    }

    /// <summary>
    /// If this namespace declaration has not yet been initialized, parse the source, set the containing nodes of the result
    /// and report any scanning and parsing errors via the host environment. This method is called whenever 
    /// </summary>
    /// <remarks>Not called in incremental scenarios</remarks>
    protected abstract void InitializeIfNecessary();
    //^ ensures this.members != null;

    private static bool IsStatic(INamespaceMember member) {
      ITypeDefinition/*?*/ typeDef = member as ITypeDefinition;
      if (typeDef == null) return false;
      return typeDef.IsStatic;
    }

    /// <summary>
    /// The members of this namespace declaration. Members can include things such as types, nested namespaces, alias declarations, and so on.
    /// </summary>
    public IEnumerable<INamespaceDeclarationMember> Members {
      get {
        if (this.cachedMembers == null)
          this.cachedMembers = this.ComputeCachedMemberList();
        return this.cachedMembers;
      }
    }
    private IEnumerable<INamespaceDeclarationMember>/*?*/ cachedMembers;

    /// <summary>
    /// A list of members that are either supplied as an argument during construction or later filled
    /// in via a method in the derived class. If InitializeIfNecessary is called, it must leave this
    /// field non null.
    /// </summary>
    protected List<INamespaceDeclarationMember>/*?*/ members;

    private IEnumerable<INamespaceDeclarationMember> ComputeCachedMemberList() {
      this.InitializeIfNecessary();
      //^ assume this.members != null;
      var members = this.members;
      this.members = null;
      for (int i = 0, n = members.Count; i < n; i++) {
        //^ assume members[i].ContainingNamespaceDeclaration.GetType() == this.GetType(); //by construction
        members[i] = members[i].MakeShallowCopyFor(this);
        var typeDecl = members[i] as TypeDeclaration;
        //if (typeDecl != null && typeDecl.
      }
      if (this == this.CompilationPart.RootNamespace) {
        members = new List<INamespaceDeclarationMember>(members);
        var globalMembers = (List<ITypeDeclarationMember>)this.CompilationPart.GlobalDeclarationContainer.GlobalMembers;
        for (int i = 0, n = globalMembers.Count; i < n; i++) {
          var gnsMember = globalMembers[i] as INamespaceDeclarationMember;
          if (gnsMember != null) members.Add(gnsMember);
        }
        members.TrimExcess();
      }
      return members.AsReadOnly();
    }

    /// <summary>
    /// Only one method body per namespace declaration needs to write out PDB information about the used namespaces.
    /// This field tracks which body takes on that role. Initially, it is null.
    /// </summary>
    internal MethodBody/*?*/ methodBodyThatWillProvideNonEmptyNamespaceScopes;

    /// <summary>
    /// Completes the two part construction of the namespace declaration by setting the containing nodes
    /// of all of the nodes contained directly or indirectly inside this namespace declaration.
    /// </summary>
    protected virtual void SetContainingNodes()
      //^ requires this.members != null;
    {
      foreach (INamespaceDeclarationMember imember in this.members) {
        NamespaceDeclarationMember/*?*/ member = imember as NamespaceDeclarationMember;
        if (member != null) {
          member.SetContainingNamespaceDeclaration(this, true);
          continue;
        }
        NamespaceTypeDeclaration/*?*/ type = imember as NamespaceTypeDeclaration;
        if (type != null) {
          type.SetContainingNamespaceDeclaration(this, true);
          continue;
        }
        NestedNamespaceDeclaration/*?*/ nestedNamespace = imember as NestedNamespaceDeclaration;
        if (nestedNamespace != null) {
          nestedNamespace.SetContainingNamespaceDeclaration(this, true);
          continue;
        }
      }
      if (this.sourceAttributes != null) {
        DummyExpression containingExpression = new DummyExpression(this.DummyBlock, SourceDummy.SourceLocation);
        foreach (SourceCustomAttribute attribute in this.sourceAttributes)
          attribute.SetContainingExpression(containingExpression);
      }
    }

    /// <summary>
    /// A scope containing all of the members in the associated namespace as well as all of the members of any namespaces imported by the namespace declaration.
    /// </summary>
    public NamespaceScope Scope {
      get {
        if (this.scope == null) {
          lock (GlobalLock.LockingObject) {
            if (this.scope == null)
              this.scope = new NamespaceScope(this);
          }
        }
        return this.scope;
      }
    }
    NamespaceScope/*?*/ scope;

    /// <summary>
    /// 
    /// </summary>
    public IEnumerable<SourceCustomAttribute> SourceAttributes {
      get {
        if (this.cachedSourceAttributes == null)
          this.cachedSourceAttributes = this.ComputeCachedSourceAttributes();
        return this.cachedSourceAttributes;
      }
    }
    /// <summary>
    /// 
    /// </summary>
    protected IEnumerable<SourceCustomAttribute>/*?*/ cachedSourceAttributes;

    /// <summary>
    /// 
    /// </summary>
    protected List<SourceCustomAttribute>/*?*/ sourceAttributes;

    private IEnumerable<SourceCustomAttribute> ComputeCachedSourceAttributes() {
      this.InitializeIfNecessary();
      var sourceAttributes = this.sourceAttributes;
      this.sourceAttributes = null;
      if (sourceAttributes == null) return Enumerable<SourceCustomAttribute>.Empty;
      for (int i = 0, n = sourceAttributes.Count; i < n; i++)
        sourceAttributes[i] = sourceAttributes[i].MakeShallowCopyFor(this.DummyBlock);
      return sourceAttributes.AsReadOnly();
    }

    /// <summary>
    /// The corresponding symbol table object. Effectively a namespace that unifies all of the namespace declarations with the same name for a particular unit.
    /// </summary>
    public abstract IUnitNamespace UnitNamespace {
      get;
      //^ ensures result is RootUnitNamespace || result is NestedUnitNamespace;
    }

    /// <summary>
    /// A collection of the members, if any, of this namespace that are unit set aliases.
    /// These correspond to the "extern alias name;" syntax in C#.
    /// </summary>
    public IEnumerable<UnitSetAliasDeclaration> UnitSetAliases {
      get {
        if (this.unitSetAliases == null)
          this.unitSetAliases = this.ComputeUnitSetAliases();
        return this.unitSetAliases;
      }
    }
    IEnumerable<UnitSetAliasDeclaration> unitSetAliases;

    private IEnumerable<UnitSetAliasDeclaration> ComputeUnitSetAliases() {
      var unitSetAliasList = new List<UnitSetAliasDeclaration>();
      foreach (var member in this.Members) {
        var unitSetAlias = member as UnitSetAliasDeclaration;
        if (unitSetAlias == null) continue;
        unitSetAliasList.Add(unitSetAlias);
      }
      unitSetAliasList.TrimExcess();
      return unitSetAliasList.AsReadOnly();
    }

    /// <summary>
    /// 
    /// </summary>
    public abstract IUnitSetNamespace UnitSetNamespace { get; }

    /// <summary>
    /// This method is called when one of the members of this namespace declaration has been updated because of an edit.
    /// It returns a new namespace declaration object that is the same as this declaration, except that one of its members
    /// is different. Since all of the members have a ContainingNamespaceDeclaration property that should point to the new
    /// namespace declaration object, each of the members in the list (except the new one) will be shallow copied and reparented
    /// as soon as they are accessed.
    /// </summary>
    /// <remarks>The current implementation makes shallow copies of all members as soon as any one of them is accessed
    /// because all ways of accessing a particular member involves evaluating the Members property, which returns all members and
    /// hence has no choice but to make the copies up front. This not too great a disaster since the copies are shallow.</remarks>
    /// <param name="members">A new list of members, where all of the elements are the same as the elements of this.members, except for the
    /// member that has been updated, which appears in the list as an updated member.</param>
    /// <param name="edit">The edit that caused all of the trouble. This is used to update the source location of the resulting
    /// namespace declaration.</param>
    public abstract NamespaceDeclaration UpdateMembers(List<INamespaceDeclarationMember> members, ISourceDocumentEdit edit);
    //^ requires edit.SourceDocumentAfterEdit.IsUpdatedVersionOf(this.SourceLocation.SourceDocument);
    //^ ensures result.GetType() == this.GetType();

    /// <summary>
    /// Zero or more used namespaces. These correspond to using clauses in C#.
    /// </summary>
    public IEnumerable<IUsedNamespace> UsedNamespaces {
      get {
        foreach (var alias in this.Aliases) {
          var ns = alias.ResolvedNamespaceOrType as INamespaceDefinition;
          if (ns == null) continue;
          yield return new UsedNamespace(alias.Name.Name, ns.Name);
        }
        foreach (var import in this.Imports) {
          var ns = import.ImportedNamespace.Resolve();
          if (ns is Dummy) continue;
          yield return new UsedNamespace(ns.Name);
        }
      }
    }

    #region IContainer<IAggregatableNamespaceDeclarationMember> Members

    IEnumerable<IAggregatableNamespaceDeclarationMember> IContainer<IAggregatableNamespaceDeclarationMember>.Members {
      get {
        return IteratorHelper.GetFilterEnumerable<INamespaceDeclarationMember, IAggregatableNamespaceDeclarationMember>(this.Members);
      }
    }

    #endregion

  }

  /// <summary>
  ///  A namespace that is used (imported) inside a namespace scope.
  /// </summary>
  internal class UsedNamespace : IUsedNamespace {

    /// <summary>
    /// Allocates a namespace that is used (imported) inside a namespace scope.
    /// </summary>
    /// <param name="alias">The name of a namespace that has been aliased. For example the "y.z" of "using x = y.z;" or "using y.z" in C#. </param>
    /// <param name="namespaceName">The name of a namepace that has been aliased.  For example the "y.z" of "using x = y.z;" or "using y.z" in C#.</param>
    internal UsedNamespace(IName alias, IName namespaceName) {
      this.alias = alias;
      this.namespaceName = namespaceName;
    }

    /// <summary>
    /// Allocates a namespace that is used (imported) inside a namespace scope.
    /// </summary>
    /// <param name="namespaceName">The name of a namepace that has been aliased.  For example the "y.z" of "using x = y.z;" or "using y.z" in C#.</param>
    internal UsedNamespace(IName namespaceName) {
      this.alias = Dummy.Name;
      this.namespaceName = namespaceName;
    }

    /// <summary>
    /// An alias for a namespace. For example the "x" of "using x = y.z;" in C#. Empty if no alias is present.
    /// </summary>
    /// <value></value>
    public IName Alias {
      get { return this.alias; }
    }
    readonly IName alias;

    /// <summary>
    /// The name of a namepace that has been aliased.  For example the "y.z" of "using x = y.z;" or "using y.z" in C#.
    /// </summary>
    /// <value></value>
    public IName NamespaceName {
      get { return this.namespaceName; }
    }
    readonly IName namespaceName;

  }


  /// <summary>
  /// 
  /// </summary>
  public abstract class NamespaceDeclarationMember : SourceItem, INamespaceDeclarationMember {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="sourceLocation"></param>
    protected NamespaceDeclarationMember(NameDeclaration name, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.name = name;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="containingNamespaceDeclaration"></param>
    /// <param name="template"></param>
    protected NamespaceDeclarationMember(NamespaceDeclaration containingNamespaceDeclaration, NamespaceDeclarationMember template)
      : base(template.SourceLocation)
      //^ ensures containingNamespaceDeclaration == this.containingNamespaceDeclaration;
    {
      ISourceDocument containingSourceDoccument = containingNamespaceDeclaration.SourceLocation.SourceDocument;
      ISourceLocation templateLocation = template.SourceLocation;
      if (containingSourceDoccument.IsUpdatedVersionOf(templateLocation.SourceDocument))
        this.sourceLocation = containingSourceDoccument.GetCorrespondingSourceLocation(templateLocation);
      this.name = template.Name.MakeCopyFor(containingNamespaceDeclaration.CompilationPart.Compilation);
      this.containingNamespaceDeclaration = containingNamespaceDeclaration;
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the member or a constituent part of the member.
    /// Do not call this method directly, but evaluate the HasErrors property. The latter will cache the return value.
    /// </summary>
    protected abstract bool CheckForErrorsAndReturnTrueIfAnyAreFound();

    /// <summary>
    /// The namespace declaration in which this nested namespace declaration is nested.
    /// </summary>
    /// <value></value>
    public NamespaceDeclaration ContainingNamespaceDeclaration {
      get
        //^ ensures result == this.containingNamespaceDeclaration;
      {
        //^ assume this.containingNamespaceDeclaration != null;
        return this.containingNamespaceDeclaration;
      }
    }
    /// <summary>
    /// The namespace declaration in which this nested namespace declaration is nested.
    /// </summary>
    protected NamespaceDeclaration/*?*/ containingNamespaceDeclaration;

    /// <summary>
    /// Checks the member for errors and returns true if any were found.
    /// </summary>
    public bool HasErrors {
      get {
        if (this.hasErrors == null)
          this.hasErrors = this.CheckForErrorsAndReturnTrueIfAnyAreFound();
        return this.hasErrors.Value;
      }
    }
    bool? hasErrors;

    /// <summary>
    /// Returns a shallow copy of this namespace declarations member with the given namespace declaration as the containing namespace of the copy.
    /// </summary>
    /// <param name="targetNamespaceDeclaration">The namespace declaration that is to be the containing namespace of the copy.</param>
    public abstract NamespaceDeclarationMember MakeShallowCopyFor(NamespaceDeclaration targetNamespaceDeclaration);
    //^ ensures result.GetType() == this.GetType();
    //^ ensures result.ContainingNamespaceDeclaration == targetNamespaceDeclaration;

    /// <summary>
    /// The name of the member. For example the alias of an alias declaration, the name of nested namespace and so on.
    /// Can be the empty name, for example if the construct is a namespace import.
    /// </summary>
    /// <value></value>
    public virtual NameDeclaration Name {
      get {
        return this.name;
      }
    }
    readonly NameDeclaration name;

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a namespace member before constructing the namespace.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    /// <param name="containingNamespaceDeclaration">The containing namespace declaration.</param>
    /// <param name="recurse">True if the method should be called recursively on members of nested namespace declarations.</param>
    public virtual void SetContainingNamespaceDeclaration(NamespaceDeclaration containingNamespaceDeclaration, bool recurse) {
      this.containingNamespaceDeclaration = containingNamespaceDeclaration;
    }

    #region INamespaceDeclarationMember Members

    NamespaceDeclaration INamespaceDeclarationMember.ContainingNamespaceDeclaration {
      get
        //^ ensures result == this.containingNamespaceDeclaration;
      {
        //^ assume this.containingNamespaceDeclaration != null;
        return this.containingNamespaceDeclaration;
      }
    }

    INamespaceDeclarationMember INamespaceDeclarationMember.MakeShallowCopyFor(NamespaceDeclaration targetNamespaceDeclaration)
      //^^ requires targetNamespaceDeclaration.GetType() == this.ContainingNamespaceDeclaration.GetType();
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingNamespaceDeclaration == targetNamespaceDeclaration;
    {
      //^ assume this.ContainingNamespaceDeclaration == ((INamespaceDeclarationMember)this).ContainingNamespaceDeclaration;
      NamespaceDeclarationMember result = this.MakeShallowCopyFor((NamespaceDeclaration)targetNamespaceDeclaration);
      //^ assume result.ContainingNamespaceDeclaration == ((INamespaceDeclarationMember)result).ContainingNamespaceDeclaration;
      return result;
    }

    #endregion

    #region INamedEntity Members

    IName INamedEntity.Name {
      get {
        return this.Name;
      }
    }

    #endregion

    #region IContainerMember<NamespaceDeclaration> Members

    NamespaceDeclaration IContainerMember<NamespaceDeclaration>.Container {
      get {
        return this.ContainingNamespaceDeclaration;
      }
    }

    IName IContainerMember<NamespaceDeclaration>.Name {
      get {
        return this.Name;
      }
    }

    #endregion

  }

  /// <summary>
  /// Corresponds to "using Namespace;" in C#.
  /// </summary>
  public class NamespaceImportDeclaration : NamespaceDeclarationMember {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="importedNamespace"></param>
    /// <param name="sourceLocation"></param>
    public NamespaceImportDeclaration(NameDeclaration name, NamespaceReferenceExpression importedNamespace, ISourceLocation sourceLocation)
      : base(name, sourceLocation) {
      this.importedNamespace = importedNamespace;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="containingNamespaceDeclaration"></param>
    /// <param name="template"></param>
    protected NamespaceImportDeclaration(NamespaceDeclaration containingNamespaceDeclaration, NamespaceImportDeclaration template)
      : base(containingNamespaceDeclaration, template)
      //^ ensures this.containingNamespaceDeclaration == containingNamespaceDeclaration;
    {
      this.importedNamespace = (NamespaceReferenceExpression)template.importedNamespace.MakeCopyFor(containingNamespaceDeclaration.DummyBlock);
      //^ assume this.containingNamespaceDeclaration == containingNamespaceDeclaration;
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a namespace member before constructing the namespace.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    /// <param name="containingNamespaceDeclaration"></param>
    /// <param name="recurse"></param>
    public override void SetContainingNamespaceDeclaration(NamespaceDeclaration containingNamespaceDeclaration, bool recurse) {
      base.SetContainingNamespaceDeclaration(containingNamespaceDeclaration, recurse);
      if (!recurse) return;
      DummyExpression containingExpression = new DummyExpression(containingNamespaceDeclaration.DummyBlock, SourceDummy.SourceLocation);
      this.importedNamespace.SetContainingExpression(containingExpression);
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the namespace import.
    /// Do not call this method directly, but evaluate the HasErrors property. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return false;
    }

    /// <summary>
    /// Calls the visitor.Visit(NamespaceImportDeclaration) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// A reference to the namespace being imported. Includes the source expression.
    /// </summary>
    public NamespaceReferenceExpression ImportedNamespace {
      get {
        return this.importedNamespace;
      }
    }
    readonly NamespaceReferenceExpression importedNamespace;

    //^ [MustOverride]
    /// <summary>
    /// Returns a shallow copy of this namespace declarations member with the given namespace declaration as the containing namespace of the copy.
    /// </summary>
    /// <param name="targetNamespaceDeclaration">The namespace declaration that is to be the containing namespace of the copy.</param>
    /// <returns></returns>
    public override NamespaceDeclarationMember MakeShallowCopyFor(NamespaceDeclaration targetNamespaceDeclaration) {
      if (targetNamespaceDeclaration == this.ContainingNamespaceDeclaration) return this;
      return new NamespaceImportDeclaration(targetNamespaceDeclaration, this);
    }

  }

  /// <summary>
  /// A scope whose members consist of all of the members of the UnitSetNamespace of a NamespaceDeclaration, as well as the members of any imported namespaces.
  /// The scope members do not include the AliasDeclaration members of the declaration. However, these can be found via the GetAliasNamed method.
  /// </summary>
  public class NamespaceScope : IScope<INamespaceMember> {

    /// <summary>
    /// Allocates a scope whose members consist of all of the members of the UnitSetNamespace of a NamespaceDeclaration, as well as the members of any imported namespaces.
    /// The scope members do not include the AliasDeclaration members of the declaration. However, these can be found via the GetAliasNamed method.
    /// </summary>
    public NamespaceScope(NamespaceDeclaration namespaceDeclaration) {
      this.namespaceDeclaration = namespaceDeclaration;
    }

    /// <summary>
    /// Return true if the given member instance is a member of this scope.
    /// </summary>
    //^ [Pure]
    public bool Contains(INamespaceMember member)
      // ^ ensures result == exists{INamespaceMember mem in this.Members; mem == member};
    {
      foreach (INamespaceMember mem in this.GetMembersNamed(member.Name, false))
        if (mem == member) return true;
      return false;
    }

    /// <summary>
    /// Returns the list of members with the given name that also satisfies the given predicate.
    /// </summary>
    //^ [Pure]
    public IEnumerable<INamespaceMember> GetMatchingMembersNamed(IName name, bool ignoreCase, Function<INamespaceMember, bool> predicate) {
      if (this.namespaceDeclaration.BusyResolvingAnAliasOrImport) yield break;
      foreach (INamespaceMember member in this.namespaceDeclaration.UnitSetNamespace.GetMatchingMembersNamed(name, ignoreCase, predicate))
        yield return member;
      foreach (var namespaceImport in this.namespaceDeclaration.Imports) {
        foreach (INamespaceMember importedMember in this.Resolve(namespaceImport.ImportedNamespace).GetMatchingMembersNamed(name, ignoreCase, predicate))
          yield return importedMember;
      }
    }

    /// <summary>
    /// Returns the list of members with the given name that also satisfies the given predicate.
    /// </summary>
    //^ [Pure]
    public IEnumerable<INamespaceMember> GetMatchingMembers(Function<INamespaceMember, bool> predicate)
      //^^ ensures forall{INamespaceMember member in result; member.Name.UniqueKey == name.UniqueKey && predicate(member) && this.Contains(member)};
      //^^ ensures forall{INamespaceMember member in this.Members; member.Name.UniqueKey == name.UniqueKey && predicate(member) ==> 
      //^^                                                            exists{INamespaceMember mem in result; mem == member}};
    {
      if (this.namespaceDeclaration.BusyResolvingAnAliasOrImport) yield break;
      foreach (INamespaceMember member in this.namespaceDeclaration.UnitSetNamespace.GetMatchingMembers(predicate))
        yield return member;
      foreach (var namespaceImport in this.namespaceDeclaration.Imports) {
        foreach (INamespaceMember importedMember in this.Resolve(namespaceImport.ImportedNamespace).GetMatchingMembers(predicate))
          yield return importedMember;
      }
    }

    /// <summary>
    /// Returns the list of members with the given name.
    /// </summary>
    //^ [Pure]
    public IEnumerable<INamespaceMember> GetMembersNamed(IName name, bool ignoreCase)
      //^^ ensures forall{INamespaceMember member in result; member.Name.UniqueKey == name.UniqueKey && this.Contains(member)};
      //^^ ensures forall{INamespaceMember member in this.Members; member.Name.UniqueKey == name.UniqueKey ==> 
      //^^                                                            exists{INamespaceMember mem in result; mem == member}};
    {
      if (this.namespaceDeclaration.BusyResolvingAnAliasOrImport) yield break;
      foreach (INamespaceMember member in this.namespaceDeclaration.UnitSetNamespace.GetMembersNamed(name, ignoreCase))
        yield return member;

      foreach (var namespaceImport in this.namespaceDeclaration.Imports) {
        foreach (INamespaceMember importedMember in this.Resolve(namespaceImport.ImportedNamespace).GetMembersNamed(name, ignoreCase))
          yield return importedMember;
      }
    }

    /// <summary>
    /// The collection of member instances that are members of this scope.
    /// </summary>
    public IEnumerable<INamespaceMember> Members {
      get {
        if (this.namespaceDeclaration.BusyResolvingAnAliasOrImport) yield break;
        foreach (INamespaceMember member in this.namespaceDeclaration.UnitSetNamespace.Members)
          yield return member;
        foreach (var namespaceImport in this.namespaceDeclaration.Imports) {
          foreach (INamespaceMember importedMember in this.Resolve(namespaceImport.ImportedNamespace).Members)
            yield return importedMember;
        }
      }
    }

    /// <summary>
    /// The namespace declaration for which this is the scope.
    /// </summary>
    readonly NamespaceDeclaration namespaceDeclaration;

    /// <summary>
    /// Call expression.Resolve(), but first set the BusyResolvingAnAliasOrImport flag to indicate that this scope should be skipped when resolving.
    /// </summary>
    private INamespaceDefinition Resolve(NamespaceReferenceExpression expression) {
      this.namespaceDeclaration.BusyResolvingAnAliasOrImport = true;
      INamespaceDefinition result = expression.Resolve();
      this.namespaceDeclaration.BusyResolvingAnAliasOrImport = false;
      return result;
    }

  }

  /// <summary>
  /// A namespace that is nested inside another namespace.
  /// </summary>
  public class NestedNamespaceDeclaration : NamespaceDeclaration, IAggregatableNamespaceDeclarationMember {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="members"></param>
    /// <param name="sourceAttributes"></param>
    /// <param name="sourceLocation"></param>
    public NestedNamespaceDeclaration(NameDeclaration name, List<INamespaceDeclarationMember> members, List<SourceCustomAttribute>/*?*/ sourceAttributes, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.members = members;
      this.name = name;
      this.sourceAttributes = sourceAttributes;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="containingNamespaceDeclaration"></param>
    /// <param name="template"></param>
    protected NestedNamespaceDeclaration(NamespaceDeclaration containingNamespaceDeclaration, NestedNamespaceDeclaration template)
      : base(template)
      //^ ensures this.containingNamespaceDeclaration == containingNamespaceDeclaration;
    {
      this.name = template.Name.MakeCopyFor(containingNamespaceDeclaration.CompilationPart.Compilation);
      this.containingNamespaceDeclaration = containingNamespaceDeclaration;
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the namespace or a constituent part of the namespace.
    /// Do not call this method directly, but evaluate the HasErrors property. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      bool result = base.CheckForErrorsAndReturnTrueIfAnyAreFound();
      //TODO: any checks that are specific to nested namespaces
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    public NamespaceDeclaration ContainingNamespaceDeclaration {
      get
        //^ ensures result == this.containingNamespaceDeclaration;
      {
        //^ assume this.containingNamespaceDeclaration != null;
        return this.containingNamespaceDeclaration;
      }
    }
    /// <summary>
    /// 
    /// </summary>
    protected NamespaceDeclaration/*?*/ containingNamespaceDeclaration;

    /// <summary>
    /// Gets the compilation part.
    /// </summary>
    /// <value>The compilation part.</value>
    public override CompilationPart CompilationPart {
      get { return this.ContainingNamespaceDeclaration.CompilationPart; }
    }

    /// <summary>
    /// Creates the nested namespace.
    /// </summary>
    /// <returns></returns>
    protected virtual NestedUnitNamespace CreateNestedNamespace() {
      return new NestedUnitNamespace(this.ContainingNamespaceDeclaration.UnitNamespace, this.Name, this.Compilation.Result);
    }

    /// <summary>
    /// 
    /// </summary>
    public override BlockStatement DummyBlock {
      get {
        if (this.dummyBlock == null) {
          BlockStatement dummyBlock = BlockStatement.CreateDummyFor(this.SourceLocation);
          dummyBlock.SetContainers(this.ContainingNamespaceDeclaration.DummyBlock, this);
          lock (this) {
            if (this.dummyBlock == null) {
              this.dummyBlock = dummyBlock;
            }
          }
        }
        return this.dummyBlock;
      }
    }
    //^ [Once]
    private BlockStatement/*?*/ dummyBlock;

    private NestedUnitNamespace GetOrCreateNestedUnitNamepace()
      //^ ensures this.nestedUnitNamespace != null;
    {
      foreach (INamespaceMember member in this.ContainingNamespaceDeclaration.UnitNamespace.GetMembersNamed(this.Name, false)) {
        NestedUnitNamespace/*?*/ nuns = member as NestedUnitNamespace;
        if (nuns != null) {
          this.nestedUnitNamespace = nuns;
          nuns.AddNamespaceDeclaration(this);
          return nuns;
        }
      }
      NestedUnitNamespace result = this.nestedUnitNamespace = this.CreateNestedNamespace();
      result.AddNamespaceDeclaration(this);
      return result;
    }

    /// <summary>
    /// If this namespace declaration has not yet been initialized, parse the source, set the containing nodes of the result
    /// and report any scanning and parsing errors via the host environment. This method is called whenever 
    /// </summary>
    /// <remarks>Not called in incremental scenarios</remarks>
    protected override void InitializeIfNecessary()
      //^^ ensures this.members != null;
    {
      //^ assume this.members != null; //the constructor ensures this
    }

    //^ [MustOverride]
    /// <summary>
    /// Returns a shallow copy of this instance with the given namespace as the new containing namespace.
    /// </summary>
    /// <param name="targetNamespaceDeclaration">The containing namespace of the result.</param>
    public virtual NestedNamespaceDeclaration MakeShallowCopyFor(NamespaceDeclaration targetNamespaceDeclaration)
      //^ ensures result.GetType() == this.GetType();
      //^ ensures result.ContainingNamespaceDeclaration == targetNamespaceDeclaration;
    {
      if (this.ContainingNamespaceDeclaration == targetNamespaceDeclaration) return this;
      return new NestedNamespaceDeclaration(targetNamespaceDeclaration, this);
    }

    /// <summary>
    /// The name of the member. For example the alias of an alias declaration, the name of nested namespace and so on.
    /// Can be the empty name, for example if the construct is a namespace import.
    /// </summary>
    /// <value></value>
    public NameDeclaration Name {
      get {
        return this.name;
      }
    }
    readonly NameDeclaration name;

    /// <summary>
    /// The corresponding symbol table object.
    /// </summary>
    public NestedUnitNamespace NestedUnitNamespace {
      get {
        if (this.nestedUnitNamespace == null) {
          this.GetOrCreateNestedUnitNamepace();
          //^ assert this.nestedUnitNamespace != null;
        }
        return this.nestedUnitNamespace;
      }
    }
    NestedUnitNamespace/*?*/ nestedUnitNamespace;

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a namespace member before constructing the namespace.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public virtual void SetContainingNamespaceDeclaration(NamespaceDeclaration containingNamespaceDeclaration, bool recurse) {
      this.containingNamespaceDeclaration = containingNamespaceDeclaration;
      if (!recurse) return;
      this.SetContainingNodes();
    }

    /// <summary>
    /// The corresponding symbol table object. Effectively a namespace that unifies all of the namespace declarations with the same name for a particular unit.
    /// </summary>
    /// <value></value>
    public override IUnitNamespace UnitNamespace {
      get {
        return this.NestedUnitNamespace;
      }
    }

    /// <summary>
    /// This method is called when one of the members of this namespace declaration has been updated because of an edit.
    /// It returns a new namespace declaration object that is the same as this declaration, except that one of its members
    /// is different. Since all of the members have a ContainingNamespaceDeclaration property that should point to the new
    /// namespace declaration object, each of the members in the list (except the new one) will be shallow copied and reparented
    /// as soon as they are accessed.
    /// </summary>
    /// <remarks>The current implementation makes shallow copies of all members as soon as any one of them is accessed
    /// because all ways of accessing a particular member involves evaluating the Members property, which returns all members and
    /// hence has no choice but to make the copies up front. This not too great a disaster since the copies are shallow.</remarks>
    /// <param name="members">A new list of members, where all of the elements are the same as the elements of this.members, except for the
    /// member that has been updated, which appears in the list as an updated member.</param>
    /// <param name="edit">The edit that caused all of the trouble. This is used to update the source location of the resulting
    /// namespace declaration.</param>
    public override NamespaceDeclaration UpdateMembers(List<INamespaceDeclarationMember> members, ISourceDocumentEdit edit)
      //^^ requires edit.SourceDocumentAfterEdit.IsUpdatedVersionOf(this.SourceLocation.SourceDocument);
      //^^ ensures result.GetType() == this.GetType();
    {
      List<INamespaceDeclarationMember> newParentMembers = new List<INamespaceDeclarationMember>(this.ContainingNamespaceDeclaration.Members);
      //^ assume edit.SourceDocumentAfterEdit.IsUpdatedVersionOf(this.ContainingNamespaceDeclaration.SourceLocation.SourceDocument);
      NamespaceDeclaration newParent = this.ContainingNamespaceDeclaration.UpdateMembers(newParentMembers, edit);
      ISourceDocument afterEdit = edit.SourceDocumentAfterEdit;
      ISourceLocation locationBeforeEdit = this.SourceLocation;
      //^ assume afterEdit.IsUpdatedVersionOf(locationBeforeEdit.SourceDocument);
      ISourceLocation locationAfterEdit = afterEdit.GetCorrespondingSourceLocation(locationBeforeEdit);
      NestedNamespaceDeclaration result = new NestedNamespaceDeclaration(name, members, this.sourceAttributes, locationAfterEdit);
      result.containingNamespaceDeclaration = newParent;
      for (int i = 0, n = newParentMembers.Count; i < n; i++) {
        if (newParentMembers[i] == this) { newParentMembers[i] = result; break; }
      }
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    public override IUnitSetNamespace UnitSetNamespace {
      get {
        if (this.unitSetNamespace == null) {
          lock (GlobalLock.LockingObject) {
            if (this.unitSetNamespace == null) {
              IUnitSetNamespace/*?*/ result = null;
              foreach (INamespaceMember member in this.ContainingNamespaceDeclaration.UnitSetNamespace.GetMembersNamed(this.Name, false)) {
                result = member as IUnitSetNamespace;
                if (result != null) break;
              }
              if (result == null) result = Dummy.RootUnitSetNamespace;
              this.unitSetNamespace = result;
            }
          }
        }
        return this.unitSetNamespace;
      }
    }
    IUnitSetNamespace/*?*/ unitSetNamespace;

    #region INamespaceDeclarationMember Members

    NamespaceDeclaration INamespaceDeclarationMember.ContainingNamespaceDeclaration {
      get
        //^ ensures result == this.containingNamespaceDeclaration;
      {
        return this.ContainingNamespaceDeclaration;
      }
    }

    INamespaceDeclarationMember INamespaceDeclarationMember.MakeShallowCopyFor(NamespaceDeclaration targetNamespaceDeclaration)
      //^^ requires targetNamespaceDeclaration.GetType() == this.ContainingNamespaceDeclaration.GetType();
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingNamespaceDeclaration == targetNamespaceDeclaration;
    {
      //^ assume targetNamespaceDeclaration.GetType() == this.ContainingNamespaceDeclaration.GetType(); //follows from the precondition
      NestedNamespaceDeclaration result = this.MakeShallowCopyFor((NamespaceDeclaration)targetNamespaceDeclaration);
      //^ assume result.ContainingNamespaceDeclaration == ((INamespaceDeclarationMember)result).ContainingNamespaceDeclaration;
      return result;
    }

    #endregion

    #region IContainerMember<NamespaceDeclaration> Members

    NamespaceDeclaration IContainerMember<NamespaceDeclaration>.Container {
      get {
        return this.ContainingNamespaceDeclaration;
      }
    }

    IName IContainerMember<NamespaceDeclaration>.Name {
      get {
        return this.Name;
      }
    }

    #endregion

    #region IAggregatableNamespaceDeclarationMember Members

    INamespaceMember IAggregatableNamespaceDeclarationMember.AggregatedMember {
      get { return this.NestedUnitNamespace; }
    }

    #endregion

    #region INamedEntity Members

    IName INamedEntity.Name {
      get { return this.Name; }
    }

    #endregion

  }

  /// <summary>
  /// Models the VB Option Explicit, Option Strict and Option Compare directives
  /// </summary>
  public class OptionDeclaration : NamespaceDeclarationMember {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="dummyName"></param>
    /// <param name="compareStringsAsBinary"></param>
    /// <param name="explicit"></param>
    /// <param name="strict"></param>
    /// <param name="sourceLocation"></param>
    public OptionDeclaration(NameDeclaration dummyName, bool compareStringsAsBinary, bool @explicit, bool strict, ISourceLocation sourceLocation)
      : base(dummyName, sourceLocation) {
      uint flags = 0;
      if (compareStringsAsBinary) flags |= 1;
      if (@explicit) flags |= 2;
      if (strict) flags |= 4;
      this.flags = flags;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="containingNamespaceDeclaration"></param>
    /// <param name="template"></param>
    protected OptionDeclaration(NamespaceDeclaration containingNamespaceDeclaration, OptionDeclaration template)
      : base(containingNamespaceDeclaration, template)
      //^ ensures containingNamespaceDeclaration == this.containingNamespaceDeclaration;
    {
      uint flags = 0;
      if (template.CompareStringsAsBinary) flags |= 1;
      if (template.Explicit) flags |= 2;
      if (template.Strict) flags |= 4;
      this.flags = flags;
    }

    private uint flags;

    /// <summary>
    /// If true, strings are compared by doing binary comparisons between the character elements. 
    /// </summary>
    public bool CompareStringsAsBinary {
      get { return (this.flags & 1) != 0; }
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the option declaration.
    /// Do not call this method directly, but evaluate the HasErrors property. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return false;
    }

    /// <summary>
    /// Calls the visitor.Visit(OptionDeclaration) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// If true, all local variables must be explicitly declared.
    /// </summary>
    public bool Explicit {
      get { return (this.flags & 2) != 0; }
    }

    //^ [MustOverride]
    /// <summary>
    /// 
    /// </summary>
    /// <param name="targetNamespaceDeclaration"></param>
    /// <returns></returns>
    public override NamespaceDeclarationMember MakeShallowCopyFor(NamespaceDeclaration targetNamespaceDeclaration) {
      if (this.ContainingNamespaceDeclaration == targetNamespaceDeclaration) return this;
      return new OptionDeclaration(targetNamespaceDeclaration, this);
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a namespace member before constructing the namespace.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    /// <param name="containingNamespaceDeclaration"></param>
    /// <param name="recurse"></param>
    public override void SetContainingNamespaceDeclaration(NamespaceDeclaration containingNamespaceDeclaration, bool recurse) {
      base.SetContainingNamespaceDeclaration(containingNamespaceDeclaration, recurse);
      if (!recurse) return;
      RootNamespaceDeclaration/*?*/ rootNamespace = containingNamespaceDeclaration as RootNamespaceDeclaration;
      if (rootNamespace == null) {
        //^ assume false; //These declarations should only be nested inside root namespaces
        return;
      }
      //TODO: check if flags is already set. If so, complain.
      rootNamespace.flags |= this.flags;
    }

    /// <summary>
    /// If true, type annotations must be present and all operations must resolve at compile time.
    /// </summary>
    public bool Strict {
      get { return (this.flags & 4) != 0; }
    }

  }

  /// <summary>
  /// An AST node for the anonymous (outermost) namespace of a compilation part.
  /// This declaration object is aggregrated with the root namespaces of each of the other
  /// compilation parts to jointly project onto a single IRootUnitNamespace object.
  /// The latter object is the one that gets persisted into the resulting assembly
  /// and that constitutes the symbol table that is consulted when analyzing the AST nodes.
  /// The aggregated namespace is available via the UnitNamespace property.
  /// </summary>
  public class RootNamespaceDeclaration : NamespaceDeclaration {

    /// <summary>
    /// Allocates a new anonymous (outermost) namespace declaration.
    /// </summary>
    /// <param name="compilationPart">The compilation part for which the resulting object is the root namespace declaration.</param>
    /// <param name="attributes">Custom attributes that appear explicitly in the source code.</param>
    /// <param name="members">The members of this namespace declaration. Members can include things such as types, nested namespaces, alias declarations, and so on.</param>
    /// <param name="sourceLocation">The location in the source that corresponds to this declaration object.</param>
    /// <remarks>Called when the compilation part already exists. This happens when a source document is parsed for the first time,
    /// as the result of a top down traversal of a compilation.</remarks>
    public RootNamespaceDeclaration(CompilationPart compilationPart, List<SourceCustomAttribute>/*?*/ attributes, List<INamespaceDeclarationMember> members, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.compilationPart = compilationPart;
      this.sourceAttributes = attributes;
      this.members = members;
    }

    /// <summary>
    /// Allocates a new anonymous (outermost) namespace declaration.
    /// Note that the members field of the resulting object should either be initialized before the first call to InitializeIfNecessary, or
    /// that method should be overridden with an implementation that initializes it.
    /// </summary>
    /// <param name="compilationPart"></param>
    /// <param name="sourceLocation"></param>
    protected RootNamespaceDeclaration(CompilationPart/*?*/ compilationPart, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.compilationPart = compilationPart;
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the namespace or a constituent part of the namespace.
    /// Do not call this method directly, but evaluate the HasErrors property. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      bool result = base.CheckForErrorsAndReturnTrueIfAnyAreFound();
      //TODO: any checks that are specific to root namespaces
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    public bool CompareStringsAsBinary {
      get { return (this.flags & 1) != 0; }
    }

    /// <summary>
    /// The compilation part for which this is the root namespace declaration.
    /// </summary>
    public override CompilationPart CompilationPart {
      get {
        //^ assume this.compilationPart != null;
        return this.compilationPart;
      }
    }
    /// <summary>
    /// The compilation part for which this is the root namespace declaration.
    /// </summary>
    protected CompilationPart/*?*/ compilationPart;

    /// <summary>
    /// A block statement that provides an evaluation environment for any expressions that form
    /// part of members of the namespace declaration. For example, the initial values of global
    /// field declarations.
    /// </summary>
    public override BlockStatement DummyBlock {
      get {
        if (this.dummyBlock == null) {
          BlockStatement dummyBlock = BlockStatement.CreateDummyFor(this.SourceLocation);
          dummyBlock.SetContainers(dummyBlock, this);
          lock (this) {
            if (this.dummyBlock == null) {
              this.dummyBlock = dummyBlock;
            }
          }
        }
        return this.dummyBlock;
      }
    }
    //^ [Once]
    private BlockStatement/*?*/ dummyBlock;

    /// <summary>
    /// Members of the namespace, such as the Option directives in VB may set these flags during
    /// second phase initialization. (I.e. from inside their SetContainingNamespaceDeclaration methods.)
    /// </summary>
    internal uint flags;

    /// <summary>
    /// If this namespace declaration has not yet been initialized, parse the source, set the containing nodes of the result
    /// and report any scanning and parsing errors via the host environment. This method is called whenever 
    /// </summary>
    /// <remarks>Not called in incremental scenarios</remarks>
    protected override void InitializeIfNecessary()
      //^^ ensures this.members != null;
    {
      //^ assume this.members != null; //The public constructor ensures this. The protected constructor relies on its caller to ensure it, to otherwise to override this method.
      if ((this.flags & 0x80000000) != 0) return;
      lock (GlobalLock.LockingObject) {
        if ((this.flags & 0x80000000) != 0) return;
        this.flags |= 0x80000000;
        //^ assume this.members != null;
        this.SetContainingNodes();
      }
      //^ assume this.members != null;
    }

    /// <summary>
    /// If true, all variables used inside this namespace should be declared explicitly. This
    /// corresponds to the Option explicit directive in VB.
    /// </summary>
    public bool Explicit {
      get { return (this.flags & 2) != 0; }
    }

    /// <summary>
    /// Makes this namespace declaration the parent of the nested namespaces and namespace types that
    /// are contained within this namespace declaration.
    /// </summary>
    protected override void SetContainingNodes() {
      this.CompilationPart.GlobalDeclarationContainer.SetContainingNamespaceDeclaration(this, true);
      base.SetContainingNodes();
    }

    /// <summary>
    /// If true, type annotations must be present and all operations must resolve at compile time
    /// inside this namespace declaration.
    /// </summary>
    public bool Strict {
      get { return (this.flags & 4) != 0; }
    }

    /// <summary>
    /// The single namespace definition that aggregates all of the members of the root namespace declarations of
    /// all of the compilation parts of this compilation. In effect this is the root of the symbol table for
    /// this compilation.
    /// </summary>
    public override IUnitNamespace UnitNamespace {
      get {
        return this.Compilation.Result.UnitNamespaceRoot;
      }
    }

    /// <summary>
    /// The root namepace of the unit set associated with the compilation that contains this namespace declaration.
    /// In effect this is the root of a symbol table that includes all of the symbols imported from external libraries (assemblies).
    /// </summary>
    public override IUnitSetNamespace UnitSetNamespace {
      get {
        return this.Compilation.UnitSet.UnitSetNamespaceRoot;
      }
    }

    /// <summary>
    /// This method is called when one of the members of this namespace declaration has been updated because of an edit.
    /// It returns a new namespace declaration object that is the same as this declaration, except that one of its members
    /// is different. Since all of the members have a ContainingNamespaceDeclaration property that should point to the new
    /// namespace declaration object, each of the members in the list (except the new one) will be shallow copied and reparented
    /// as soon as they are accessed.
    /// </summary>
    /// <remarks>The current implementation makes shallow copies of all members as soon as any one of them is accessed
    /// because all ways of accessing a particular member involves evaluating the Members property, which returns all members and
    /// hence has no choice but to make the copies up front. This not too great a disaster since the copies are shallow.</remarks>
    /// <param name="members">A new list of members, where all of the elements are the same as the elements of this.members, except for the
    /// member that has been updated, which appears in the list as an updated member.</param>
    /// <param name="edit">The edit that caused all of the trouble. This is used to update the source location of the resulting
    /// namespace declaration.</param>
    public override NamespaceDeclaration UpdateMembers(List<INamespaceDeclarationMember> members, ISourceDocumentEdit edit)
      //^^ requires edit.SourceDocumentAfterEdit.IsUpdatedVersionOf(this.SourceLocation.SourceDocument);
      //^^ ensures result.GetType() == this.GetType();
    {
      ISourceDocument afterEdit = edit.SourceDocumentAfterEdit;
      ISourceLocation locationBeforeEdit = this.SourceLocation;
      //^ assume afterEdit.IsUpdatedVersionOf(locationBeforeEdit.SourceDocument);
      ISourceLocation locationAfterEdit = afterEdit.GetCorrespondingSourceLocation(locationBeforeEdit);
      RootNamespaceDeclaration result = new RootNamespaceDeclaration(null, locationAfterEdit);
      result.members = members;
      result.compilationPart = this.CompilationPart.UpdateRootNamespace(result);
      return result;
    }

  }

  /// <summary>
  /// A simple name that serves as an alias for an expression known to be the name of a unit set.
  /// In C# this corresponds to the "extern alias name;" syntax.
  /// </summary>
  public class UnitSetAliasDeclaration : NamespaceDeclarationMember {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="sourceLocation"></param>
    public UnitSetAliasDeclaration(NameDeclaration name, ISourceLocation sourceLocation)
      : base(name, sourceLocation) {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="containingNamespaceDeclaration"></param>
    /// <param name="template"></param>
    protected UnitSetAliasDeclaration(NamespaceDeclaration containingNamespaceDeclaration, UnitSetAliasDeclaration template)
      : base(containingNamespaceDeclaration, template)
      //^ ensures containingNamespaceDeclaration == this.containingNamespaceDeclaration;
    {
      Compilation targetCompilation = containingNamespaceDeclaration.CompilationPart.Compilation;
      this.unitSet = targetCompilation.GetUnitSetFor(template.Name.MakeCopyFor(targetCompilation));
      //^ assume containingNamespaceDeclaration == this.containingNamespaceDeclaration;
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the unit set alias declaration.
    /// Do not call this method directly, but evaluate the HasErrors property. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return false;
    }

    /// <summary>
    /// Calls the visitor.Visit(UnitSetAliasDeclaration) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    //^ [MustOverride]
    /// <summary>
    /// 
    /// </summary>
    /// <param name="targetNamespaceDeclaration"></param>
    /// <returns></returns>
    public override NamespaceDeclarationMember MakeShallowCopyFor(NamespaceDeclaration targetNamespaceDeclaration) {
      if (targetNamespaceDeclaration == this.ContainingNamespaceDeclaration) return this;
      return new UnitSetAliasDeclaration(targetNamespaceDeclaration, this);
    }

    /// <summary>
    /// The unit set referenced by the alias.
    /// </summary>
    public IUnitSet UnitSet {
      get {
        IUnitSet/*?*/ result;
        if ((result = this.unitSet) == null) {
          lock (GlobalLock.LockingObject) {
            if ((result = this.unitSet) == null) {
              this.unitSet = result = this.ContainingNamespaceDeclaration.Compilation.GetUnitSetFor(this.Name);
              if (result is Dummy) {
                this.ContainingNamespaceDeclaration.Helper.ReportError(new AstErrorMessage(this.Name, Error.BadExternAlias, this.Name.Value));
              }
            }
          }
        }
        return result;
      }
    }
    IUnitSet/*?*/ unitSet;

  }

}
