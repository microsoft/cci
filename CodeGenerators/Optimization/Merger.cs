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
using System.Diagnostics.Contracts;
using Microsoft.Cci.UtilityDataStructures;
using Microsoft.Cci.MutableCodeModel;

namespace Microsoft.Cci.Optimization {

  /// <summary>
  /// 
  /// </summary>
  public class AssemblyMerger : ModuleMerger {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="host"></param>
    /// <param name="result"></param>
    /// <param name="modulesToMerge"></param>
    public AssemblyMerger(IMetadataHost host, Assembly result, params Module[] modulesToMerge) 
      : base(host, result, modulesToMerge) {
      Contract.Requires(host != null);
      Contract.Requires(result != null);
      Contract.Requires(modulesToMerge != null);

      this.result = result;
    }

    Assembly result;

    [ContractInvariantMethod]
    private void ObjectInvariant() {
      Contract.Invariant(this.result != null);
    }

    /// <summary>
    /// 
    /// </summary>
    public override void Merge() {
      base.Merge();
      this.result.AssemblyAttributes = this.Concatenate((a) => ((a as Assembly)??Dummy.Assembly).AssemblyAttributes);
      this.result.ExportedTypes = this.Concatenate((a) => ((a as Assembly)??Dummy.Assembly).ExportedTypes);
      this.result.Files = this.Concatenate((a) => ((a as Assembly)??Dummy.Assembly).Files);
      this.result.MemberModules = this.Concatenate((a) => ((a as Assembly)??Dummy.Assembly).MemberModules);
      this.result.Resources = this.Concatenate((a) => ((a as Assembly)??Dummy.Assembly).Resources);
      this.result.SecurityAttributes = this.Concatenate((a) => ((a as Assembly)??Dummy.Assembly).SecurityAttributes);

      this.MergeAttributes(this.result.Attributes);
      this.ReparentFilesAndDealWithDuplicates();
      this.ReparentMemberModulesAndDealWithDuplicates();
      this.ReparentResourcesAndDealWithDuplicates();
      this.MergeSecurityAttributes();
    }

    private void ReparentFilesAndDealWithDuplicates() {
      var files = this.result.Files;
      if (files == null) return;
      var fileRefFor = new Hashtable<FileReference>();
      for (int i = 0; i < files.Count; i++) {
        var fileRef = files[i] as FileReference;
        if (fileRef == null) { files.RemoveAt(i); continue; } //TODO: error 
        var key = (uint)fileRef.FileName.UniqueKey;
        if (fileRefFor[key] != null) { files.RemoveAt(i); continue; }; //TODO: check that hash values match
        fileRef.ContainingAssembly = this.result;
        fileRefFor[key] = fileRef;
      }
      files.TrimExcess();
    }

    private void ReparentMemberModulesAndDealWithDuplicates() {
      var memberModules = this.result.MemberModules;
      if (memberModules == null) return;
      var memberModuleFor = new Dictionary<ModuleIdentity, Module>();
      for (int i = 0; i < memberModules.Count; i++) {
        var memberModule = memberModules[i] as Module;
        if (memberModule == null) { memberModules.RemoveAt(i); continue; } //TODO: error
        if (memberModuleFor.ContainsKey(memberModule.ModuleIdentity)) { memberModules.RemoveAt(i); continue; }
        memberModule.ContainingAssembly = this.result;
        memberModuleFor[memberModule.ModuleIdentity] = memberModule;
      }
      memberModules.TrimExcess();
    }

    private void ReparentResourcesAndDealWithDuplicates() {
      var resources = this.result.Resources;
      if (resources == null) return;
      var resourceFor = new Hashtable<Resource>();
      for (int i = 0; i < resources.Count; i++) {
        var resource = resources[i] as Resource;
        if (resource == null) { resources.RemoveAt(i); continue; } //TODO: error
        var key = (uint)resource.Name.UniqueKey;
        if (resourceFor[key] != null) { resources.RemoveAt(i); continue; }
        resource.DefiningAssembly = this.result;
        resourceFor[key] = resource;
      }
      resources.TrimExcess();
    }

    private void MergeSecurityAttributes() {
      var secAttrs = this.result.SecurityAttributes;
      if (secAttrs == null) return;
      for (int i = 0; i < secAttrs.Count; i++) {
        var attr = secAttrs[i];
        Contract.Assume(attr != null);
        var mutableSecAttr = attr as SecurityAttribute;
        for (int j = i+1; j < secAttrs.Count; j++) {
          var laterAttr = secAttrs[j];
          Contract.Assume(laterAttr != null);
          if (attr.Action == laterAttr.Action) {
            if (mutableSecAttr == null) {
              mutableSecAttr = new SecurityAttribute() { Action = attr.Action, Attributes = new List<ICustomAttribute>(attr.Attributes) };
              secAttrs[i] = mutableSecAttr;
            }
            Contract.Assume(mutableSecAttr.Attributes != null);
            if (laterAttr.Attributes != null)
              mutableSecAttr.Attributes.AddRange(laterAttr.Attributes);
            secAttrs.RemoveAt(j);
          }
        }
        if (mutableSecAttr != null)
          this.MergeAttributes(mutableSecAttr.Attributes);
      }
    }

  }

  /// <summary>
  /// 
  /// </summary>
  public class ModuleMerger {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="host"></param>
    /// <param name="result"></param>
    /// <param name="modulesToMerge"></param>
    public ModuleMerger(IMetadataHost host, Module result, params Module[] modulesToMerge) {
      Contract.Requires(host != null);
      Contract.Requires(result != null);
      Contract.Requires(modulesToMerge != null);

      this.result = result;
      this.resultModuleType = new NamespaceTypeDefinition();
      this.rootNamespace = new RootUnitNamespace() { Unit = result };
      this.modulesToMerge = modulesToMerge;
      this.assemblyReferenceFor = new Dictionary<AssemblyIdentity, IAssemblyReference>();
      this.host = host;
    }

    Module result;
    NamespaceTypeDefinition resultModuleType; //result.AllTypes[0]
    RootUnitNamespace rootNamespace;
    Module[] modulesToMerge;
    Dictionary<AssemblyIdentity, IAssemblyReference> assemblyReferenceFor;
    IMetadataHost host;

    [ContractInvariantMethod]
    private void ObjectInvariant() {
      Contract.Invariant(this.result != null);
      Contract.Invariant(this.resultModuleType != null);
      Contract.Invariant(this.rootNamespace != null);
      Contract.Invariant(this.modulesToMerge != null);
      Contract.Invariant(this.host != null);
      Contract.Invariant(this.assemblyReferenceFor != null);
    }

    /// <summary>
    /// 
    /// </summary>
    public virtual void Merge() {
      this.MergeModuleTypes();
      this.ConcatenateOtherTypes();
      this.result.ModuleAttributes = this.Concatenate((a) => a.ModuleAttributes);
      this.result.ModuleReferences = this.Concatenate((a) => a.ModuleReferences);
      this.result.UninterpretedSections = this.Concatenate((a) => a.UninterpretedSections);

      this.MergeUnitNamespaceRoots();
      this.MergeAssemblyReferences();
      this.ReparentMembersAndDealWithDuplicates(this.rootNamespace);
      this.MergeAttributes(this.result.ModuleAttributes);
      this.ReparentModuleReferencesAndDealWithDuplicates();
      this.DealWithDuplicateUninterpretedSections();
    }

    private void MergeModuleTypes() {
      this.resultModuleType.Name = host.NameTable.GetNameFor("<Module>");
      this.resultModuleType.ContainingUnitNamespace = this.result.UnitNamespaceRoot;
      this.resultModuleType.InternFactory = this.host.InternFactory;
      this.result.AllTypes.Add(this.resultModuleType);
      this.resultModuleType.Fields = new List<IFieldDefinition>();
      this.resultModuleType.Methods = new List<IMethodDefinition>();
      for (int i = 0, n = this.modulesToMerge.Length; i < n; i++) {
        var assemToMerge = this.modulesToMerge[i];
        Contract.Assume(assemToMerge != null);
        if (assemToMerge.AllTypes.Count == 0) continue;
        var moduleTypeToMerge = assemToMerge.AllTypes[0];
        Contract.Assume(moduleTypeToMerge != null);
        Contract.Assume(this.resultModuleType.Fields != null);
        if (moduleTypeToMerge.Fields != null)
          this.resultModuleType.Fields.AddRange(moduleTypeToMerge.Fields);
        Contract.Assume(this.resultModuleType.Methods != null);
        if (moduleTypeToMerge.Methods != null)
          this.resultModuleType.Methods.AddRange(moduleTypeToMerge.Methods);
      }
      Contract.Assume(this.resultModuleType.Fields != null);
      if (this.resultModuleType.Fields.Count == 0)
        this.resultModuleType.Fields = null;
      else
        this.resultModuleType.Fields.TrimExcess();
      Contract.Assume(this.resultModuleType.Methods != null);
      if (this.resultModuleType.Methods.Count == 0)
        this.resultModuleType.Methods = null;
      else
        this.resultModuleType.Methods.TrimExcess();
    }

    private void ConcatenateOtherTypes() {
      var resultTypes = this.result.AllTypes;
      for (int i = 0, n = this.modulesToMerge.Length; i < n; i++) {
        var assemToMerge = this.modulesToMerge[i];
        Contract.Assume(assemToMerge != null);
        if (assemToMerge.AllTypes.Count < 1) continue;
        for (int j = 0, m = assemToMerge.AllTypes.Count; j < m; j++) {
          var typeToMerge = assemToMerge.AllTypes[j];
          Contract.Assume(typeToMerge != null);
          resultTypes.Add(typeToMerge);
        }
      }
      resultTypes.TrimExcess();
    }

    internal delegate IEnumerable<ElementType> GetCollection<ElementType>(IModule module);

    internal List<ElementType>/*?*/ Concatenate<ElementType>(GetCollection<ElementType> getCollectionFrom) {
      Contract.Requires(getCollectionFrom != null);

      var result = new List<ElementType>();
      for (int i = 0, n = this.modulesToMerge.Length; i < n; i++) {
        var moduleToMerge = this.modulesToMerge[i];
        Contract.Assume(moduleToMerge != null);
        var collection = getCollectionFrom(moduleToMerge);
        Contract.Assume(collection != null);
        result.AddRange(collection);
      }
      if (result.Count == 0) return null;
      result.TrimExcess();
      return result;
    }

    private void MergeUnitNamespaceRoots() {
      this.result.UnitNamespaceRoot = this.rootNamespace;
      for (int i = 0; i < this.modulesToMerge.Length; i++) {
        var assemblyToMerge = this.modulesToMerge[i];
        Contract.Assume(assemblyToMerge != null);
        this.MergeNamespaces(this.rootNamespace, assemblyToMerge.UnitNamespaceRoot);
      }
    }

    private void MergeNamespaces(UnitNamespace result, IUnitNamespace namespaceToMerge) {
      Contract.Requires(result != null);
      Contract.Requires(namespaceToMerge != null);

      foreach (var member in namespaceToMerge.Members) {
        var nestedNs = member as NestedUnitNamespace;
        if (nestedNs != null) {
          UnitNamespace nestedResult = null;
          foreach (var rmember in result.Members) {
            Contract.Assume(rmember != null);
            if (rmember.Name != nestedNs.Name) continue;
            nestedResult = rmember as UnitNamespace;
            if (nestedResult != null) break;
          }
          if (nestedResult == null)
            nestedResult = new NestedUnitNamespace() { ContainingUnitNamespace = result, Name = nestedNs.Name };
          this.MergeNamespaces(nestedResult, nestedNs);
          continue;
        }
        result.Members.Add(member);
      }
    }

    /// <summary>
    /// Fixes up all references in this.result that would resolve to definitions in one of the assemblies to merge
    /// so that instead they will resolve to the (reparented) definition in the merged assembly. Consolidates
    /// the other assembly references so that there is a single instance for each referenced assembly.
    /// Also replaces all references to definitions in the merged assembly, with the actual definitions.
    /// </summary>
    private void MergeAssemblyReferences() {
      for (int i = 0, n = this.modulesToMerge.Length; i < n; i++) {
        var moduleToMerge = this.modulesToMerge[i];
        Contract.Assume(moduleToMerge != null);
      }
      var referenceMerger = new AssemblyReferenceMerger(this.host, this.result, this.assemblyReferenceFor);
      referenceMerger.Rewrite(this.result);
      var nonSelfReferences = new List<IAssemblyReference>(this.assemblyReferenceFor.Count-1);
      this.result.AssemblyReferences = nonSelfReferences;
      foreach (var assemRef in this.assemblyReferenceFor.Values) {
        if (assemRef == this.result) continue;
        nonSelfReferences.Add(assemRef);
      }
    }

    private void ReparentMembersAndDealWithDuplicates(UnitNamespace unitNamespace) {
      Contract.Requires(unitNamespace != null);

      var members = unitNamespace.Members;
      for (int i = 0, n = members.Count; i < n; i++) {
        var member = members[i];
        var nsType = member as NamespaceTypeDefinition;
        if (nsType != null) {
          nsType.ContainingUnitNamespace = unitNamespace;
          for (int j = i+1; j < n; j++) {
            var laterNsType = members[j] as NamespaceTypeDefinition;
            if (laterNsType == null || laterNsType.Name != nsType.Name) continue;
            if (nsType.MangleName != laterNsType.MangleName) continue;
            if (nsType.MangleName && nsType.GenericParameterCount != laterNsType.GenericParameterCount) continue;
            this.ReportNameCollisionAndRename(nsType, laterNsType, j);
          }
          continue;
        }
        var globalField = member as GlobalFieldDefinition;
        if (globalField != null) {
          globalField.ContainingTypeDefinition = this.resultModuleType;
          for (int j = i+1; j < n; j++) {
            var laterField = members[j] as GlobalFieldDefinition;
            if (laterField == null || laterField.Name != globalField.Name) continue;
            if (!TypeHelper.TypesAreEquivalent(globalField.Type, laterField.Type)) continue;
            //TODO: check custom modifiers
            this.ReportNameCollisionAndRename(globalField, laterField, j);
          }
          continue;
        }
        var globalMethod = member as GlobalMethodDefinition;
        if (globalMethod != null) {
          globalMethod.ContainingTypeDefinition = this.resultModuleType;
          for (int j = i+1; j < n; j++) {
            var laterMethod = members[j] as GlobalMethodDefinition;
            if (laterMethod == null || laterMethod.Name != globalMethod.Name) continue;
            if (!MemberHelper.SignaturesAreEqual(globalMethod, laterMethod, resolveTypes: false)) continue;
            this.ReportNameCollisionAndRename(globalMethod, laterMethod, j);
          }
          continue;
        }
        var alias = member as NamespaceAliasForType;
        if (alias != null) {
          alias.ContainingNamespace = unitNamespace;
          for (int j = i+1; j < n; j++) {
            var laterAlias = members[j] as NamespaceAliasForType;
            if (laterAlias == null || laterAlias.Name != alias.Name || laterAlias.GenericParameterCount != alias.GenericParameterCount) continue; //Note: these names are not unmangled
            this.ReportNameCollision(alias, laterAlias, j);
          }
          continue;
        }
      }
    }

    private void ReportNameCollision(NamespaceAliasForType alias, NamespaceAliasForType laterAlias, int position) {
      Contract.Requires(alias != null);
      Contract.Requires(laterAlias != null);

      this.host.ReportError(new ErrorMessage() {
        Code = 1, ErrorReporter = this, Error = MergeError.DuplicateAlias,
        MessageParameter = TypeHelper.GetTypeName(alias.AliasedType)
      });
    }

    private void ReportNameCollisionAndRename(GlobalFieldDefinition globalField, GlobalFieldDefinition laterField, int position) {
      Contract.Requires(globalField != null);
      Contract.Requires(laterField != null);

      laterField.Name = this.host.NameTable.GetNameFor(laterField.Name.Value+"``"+position);
      this.host.ReportError(new ErrorMessage() {
        Code = 2, ErrorReporter = this, Error = MergeError.DuplicateGlobalField,
        MessageParameter = MemberHelper.GetMemberSignature(globalField, NameFormattingOptions.Signature),
      });
    }

    private void ReportNameCollisionAndRename(GlobalMethodDefinition globalMethod, GlobalMethodDefinition laterMethod, int position) {
      Contract.Requires(globalMethod != null);
      Contract.Requires(laterMethod != null);

      laterMethod.Name = this.host.NameTable.GetNameFor(laterMethod.Name.Value+"``"+position);
      this.host.ReportError(new ErrorMessage() {
        Code = 3, ErrorReporter = this, Error = MergeError.DuplicateGlobalMethod,
        MessageParameter = MemberHelper.GetMethodSignature(globalMethod, NameFormattingOptions.ReturnType|NameFormattingOptions.Signature),
      });
    }

    private void ReportNameCollisionAndRename(NamespaceTypeDefinition nsType, NamespaceTypeDefinition laterNsType, int position) {
      Contract.Requires(nsType != null);
      Contract.Requires(laterNsType != null);
      laterNsType.Name = this.host.NameTable.GetNameFor(laterNsType.Name.Value+"``"+position);
      this.host.ReportError(new ErrorMessage() {
        Code = 4, ErrorReporter = this, Error = MergeError.DuplicateGlobalField,
        MessageParameter = TypeHelper.GetTypeName(nsType),
      });
    }

    internal void MergeAttributes(List<ICustomAttribute> attrs) {
      if (attrs == null) return;
      for (int i = 0; i < attrs.Count; i++) {
        var attr = attrs[i];
        Contract.Assume(attr != null);
        for (int j = i+1; j < attrs.Count; j++) {
          var laterAttr = attrs[j];
          Contract.Assume(laterAttr != null);
          if (TypeHelper.TypesAreEquivalent(attr.Type, laterAttr.Type))
            attrs.RemoveAt(j);
        }
      }
    }

    [ContractVerification(false)] //moduleRef.ModuleIdentity = .... reports requires unproven: !this.IsFrozen
    private void ReparentModuleReferencesAndDealWithDuplicates() {
      var moduleReferences = this.result.ModuleReferences;
      if (moduleReferences == null) return;
      var moduleRefFor = new Hashtable<ModuleReference>();
      for (int i = 0; i < moduleReferences.Count; i++) {
        var moduleRef = moduleReferences[i] as ModuleReference;
        if (moduleRef == null || moduleRef.IsFrozen) { moduleReferences.RemoveAt(i); continue; }; //TODO: error
        var modId = moduleRef.ModuleIdentity;
        var key = (uint)modId.Name.UniqueKey;
        if (moduleRefFor[key] != null) { moduleReferences.RemoveAt(i); continue; };
        moduleRef.ContainingAssembly = this.result as IAssembly;
        moduleRef.ModuleIdentity = new ModuleIdentity(modId.Name, modId.Location, this.result.ModuleIdentity as AssemblyIdentity);
        moduleRefFor[key] = moduleRef;
      }
      moduleReferences.TrimExcess();
    }

    private void DealWithDuplicateUninterpretedSections() {
      var uninterpretedSections = this.result.UninterpretedSections;
      if (uninterpretedSections == null) return;
      var uninterpretedSectionFor = new Hashtable<PESection>();
      for (int i = 0; i < uninterpretedSections.Count; i++) {
        var uninterpretedSection = uninterpretedSections[i] as PESection;
        if (uninterpretedSection == null) { uninterpretedSections.RemoveAt(i); continue; } //TODO: error
        var key = (uint)uninterpretedSection.SectionName.UniqueKey;
        if (uninterpretedSectionFor[key] != null) { uninterpretedSections.RemoveAt(i); continue; } //TODO: error
        uninterpretedSectionFor[key] = uninterpretedSection;
      }
    }

  }

  internal class AssemblyReferenceMerger : MetadataRewriter {

    internal AssemblyReferenceMerger(IMetadataHost host, Module mergedModule, Dictionary<AssemblyIdentity, IAssemblyReference> assemblyReferenceFor)
      : base(host) {
      Contract.Requires(host != null);
      Contract.Requires(mergedModule != null);
      Contract.Requires(assemblyReferenceFor != null);

      this.mergedModule = mergedModule;
      this.assemblyReferenceFor = assemblyReferenceFor;
    }

    Module mergedModule;
    Dictionary<AssemblyIdentity, IAssemblyReference> assemblyReferenceFor;

    [ContractInvariantMethod]
    private void ObjectInvariant() {
      Contract.Invariant(this.mergedModule != null);
      Contract.Invariant(this.assemblyReferenceFor != null);
    }

    public override IAssemblyReference Rewrite(IAssemblyReference assemblyReference) {
      IAssemblyReference result;
      if (!this.assemblyReferenceFor.TryGetValue(assemblyReference.AssemblyIdentity, out result))
        this.assemblyReferenceFor[assemblyReference.AssemblyIdentity] = result = assemblyReference;
      Contract.Assume(result != null);
      return result;
    }

    public override INamespaceTypeReference Rewrite(INamespaceTypeReference namespaceTypeReference) {
      namespaceTypeReference = base.Rewrite(namespaceTypeReference);
      var unit = TypeHelper.GetDefiningUnitReference(namespaceTypeReference);
      if (unit == this.mergedModule) {
        var resolvedType = namespaceTypeReference.ResolvedType;
        Contract.Assume(!(resolvedType is Dummy));
        return resolvedType;
      }
      return namespaceTypeReference;
    }

    public override INestedTypeReference Rewrite(INestedTypeReference nestedTypeReference) {
      nestedTypeReference = base.Rewrite(nestedTypeReference);
      var unit = TypeHelper.GetDefiningUnitReference(nestedTypeReference);
      if (unit == this.mergedModule) {
        var resolvedType = nestedTypeReference.ResolvedType;
        Contract.Assume(!(resolvedType is Dummy));
        return resolvedType;
      }
      return nestedTypeReference;
    }

  }

  /// <summary>
  /// Information about an error that occurred during an assembly merge operation.
  /// </summary>
  public sealed class ErrorMessage : IErrorMessage {

    /// <summary>
    /// The object reporting the error. This can be used to filter out errors coming from non interesting sources.
    /// </summary>
    public object ErrorReporter { get; internal set; }

    /// <summary>
    /// A short identifier for the reporter of the error, suitable for use in human interfaces. For example "CS" in the case of a C# language error.
    /// </summary>
    public string ErrorReporterIdentifier {
      get { return "Merger"; }
    }

    /// <summary>
    /// The error this message pertains to.
    /// </summary>
    public MergeError Error { get; internal set; }

    /// <summary>
    /// A code that corresponds to this error. This code is the same for all cultures.
    /// </summary>
    public long Code { get; internal set; }

    /// <summary>
    /// True if the error message should be treated as an informational warning rather than as an indication that the associated
    /// merge has failed and no useful executable output has been generated.
    /// </summary>
    public bool IsWarning {
      get { return true; }
    }

    /// <summary>
    /// A description of the error suitable for user interaction. Localized to the current culture.
    /// </summary>
    public string Message {
      get {
        System.Resources.ResourceManager resourceManager = new System.Resources.ResourceManager("Microsoft.Cci.Optimization.AssemblyMerger.ErrorMessages", typeof(ErrorMessage).Assembly);
        string messageKey = this.Error.ToString();
        string/*?*/ localizedString = null;
        try {
          localizedString = resourceManager.GetString(messageKey);
        } catch (System.Resources.MissingManifestResourceException) {
        }
        try {
          if (localizedString == null) {
            localizedString = resourceManager.GetString(messageKey, System.Globalization.CultureInfo.InvariantCulture);
          }
        } catch (System.Resources.MissingManifestResourceException) {
        }
        if (localizedString == null)
          localizedString = messageKey;
        else if (this.MessageParameter != null)
          localizedString = string.Format(localizedString, this.MessageParameter);
        return localizedString;
      }
    }

    /// <summary>
    /// If not null, this strings parameterizes the error message.
    /// </summary>
    public string/*?*/ MessageParameter { get; internal set; }

    /// <summary>
    /// The location of the error.
    /// </summary>
    public ILocation Location { get { return Dummy.Location; } }

    /// <summary>
    /// Zero ore more locations that are related to this error.
    /// </summary>
    public IEnumerable<ILocation> RelatedLocations { get { return Enumerable<ILocation>.Empty; } }
  }

  /// <summary>
  /// An enumeration of errors that can occur during assembly merging.
  /// </summary>
  public enum MergeError {
    /// <summary>
    /// Type {0} is exported from more than one of the merged assemblies. A rename was done to prevent the merged assembly from being invalid.
    /// </summary>
    DuplicateAlias,
    /// <summary>
    /// Global method {0} is exported from more than one of the merged assemblies. A rename was done to prevent the merged assembly from being invalid.
    /// </summary>
    DuplicateGlobalMethod,
    /// <summary>
    /// Global field {0} is exported from more than one of the merged assemblies. A rename was done to prevent the merged assembly from being invalid.
    /// </summary>
    DuplicateGlobalField,
    /// <summary>
    /// Namespace type {0} is exported from more than one of the merged assemblies. A rename was done to prevent the merged assembly from being invalid.
    /// </summary>
    DuplicateType,
  }

}