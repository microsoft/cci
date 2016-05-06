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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Cci.Contracts;
using Microsoft.Cci.UtilityDataStructures;
using Microsoft.Cci.Immutable;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci.Ast {

  /// <summary>
  /// An object that describes an edit to a source file.
  /// </summary>
  public abstract class AstSourceDocumentEdit : SourceDocumentEdit {

    /// <summary>
    /// Allocates an object that describes an edit to a source file.
    /// </summary>
    protected AstSourceDocumentEdit(ISourceLocation sourceLocationBeforeEdit, ISourceDocument sourceDocumentAfterEdit)
      : base(sourceLocationBeforeEdit, sourceDocumentAfterEdit)
      //^ requires sourceDocumentAfterEdit.IsUpdatedVersionOf(sourceLocationBeforeEdit.SourceDocument);
    {
    }

    /// <summary>
    /// The compilation part that is the result of applying this edit.
    /// </summary>
    public abstract CompilationPart CompilationPartAfterEdit {
      get;
    }

  }

  /// <summary>
  /// The root node and global context for a compilation. Every node in the AST has a path back to this node. 
  /// Compilation nodes and all of their descendants are immutable once initialized. Initialization happens in two phases.
  /// Calling the constructor is phase one. Setting the parent node is phase two (and is delayed in order to allow for bottom up AST construction).
  /// A compilation does not have a second phase initialization method since it has no parent.
  /// </summary>
  public abstract class Compilation : ICompilation, IErrorCheckable {

    /// <summary>
    /// Use this constructor to construct an entirely new Compilation. A compilation is a list of source files, compiler options and references to other compilations.
    /// In command line terms, it corresponds to a single invocation of the command line compiler. In IDE terms it corresponds to what Visual Studio calls a project.
    /// Compilation nodes and all of their descendants are immutable once initialized. Initialization happens in two phases.
    /// Calling the constructor is phase one. Setting the parent node is phase two (and is delayed in order to allow for bottom up AST construction).
    /// A compilation does not have a second phase initialization method since it has no parent.
    /// </summary>
    /// <param name="hostEnvironment">An object that represents the application that hosts the compilation and that provides things such as a shared name table.</param>
    /// <param name="result">A "unit of compilation" that holds the result of this compilation. Once the Compilation has been constructed, result can be navigated causing
    /// on demand compilation to occur.</param>
    /// <param name="options">Compilation options, for example whether or not checked arithmetic is the default.</param>
    protected Compilation(ISourceEditHost hostEnvironment, Unit result, FrameworkOptions options) {
      this.hostEnvironment = hostEnvironment;
      this.result = result;
      this.options = options;
    }

    /// <summary>
    /// Use this constructor to construct a Compilation that is an incremental update of another Compilation.
    /// </summary>
    /// <param name="hostEnvironment">An object that represents the application that hosts the compilation and that provides things such as a shared name table.</param>
    /// <param name="result">A "unit of compilation" that holds the result of this compilation. Once the Compilation has been constructed, result can be navigated causing
    /// on demand compilation to occur.</param>
    /// <param name="parts">A parts list that is incrementally different from the parts of previous version of this compilation.</param>
    /// <param name="options">Compilation options, for example whether or not checked arithmetic is the default.</param>
    protected Compilation(ISourceEditHost hostEnvironment, Unit result, FrameworkOptions options, IEnumerable<CompilationPart> parts) {
      this.hostEnvironment = hostEnvironment;
      this.result = result;
      this.options = options;
      this.parts = new List<CompilationPart>(parts);
    }

    /// <summary>
    /// A collection of methods that correspond to operations that are built in to the target platform.
    /// These methods exist only to allow standard overload resolution logic to be used to determine which
    /// conversions to apply to the arguments.
    /// </summary>
    public BuiltinMethods BuiltinMethods {
      get {
        if (this.builtinMethods == null) {
          lock (GlobalLock.LockingObject) {
            if (this.builtinMethods == null)
              this.builtinMethods = new BuiltinMethods(this);
          }
        }
        return this.builtinMethods;
      }
    }
    //^ [Once]
    BuiltinMethods/*?*/ builtinMethods;

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the compilation.
    /// Do not call this method directly, but evaluate the HasErrors property. The latter will cache the return value.
    /// </summary>
    protected virtual bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      bool result = false;
      foreach (var compilationPart in this.Parts)
        result |= compilationPart.HasErrors;
      return result;
    }

    /// <summary>
    /// Returns true if the given source document forms a part of the compilation.
    /// </summary>
    public bool Contains(ISourceDocument sourceDocument) {
      foreach (CompilationPart compilationPart in this.Parts) {
        if (compilationPart.SourceLocation.SourceDocument == sourceDocument) return true;
      }
      return false;
    }

    /// <summary>
    /// Returns an object that can provide information about the local scopes of a method.
    /// </summary>
    protected virtual ILocalScopeProvider GetLocalScopeProvider() {
      return new LocalScopeProvider();
    }

    /// <summary>
    /// Constructs a compilation parts list for the first time.
    /// This method is combined with the constructor that does not take a parts list as parameter and requires the parts list to be null.
    /// It is implemented by derived types and is expected to create compilation parts that know how to construct themselves by parsing their corresponding source files.
    /// </summary>
    protected abstract List<CompilationPart> GetPartList();
    //^ requires this.parts == null;


    /// <summary>
    /// Returns an object that can map some kinds of ILocation objects to IPrimarySourceLocation objects.
    /// In this case, it is primarily needed to map IDerivedSourceLocations and IIncludedSourceLocations to IPrimarySourceLocations.
    /// </summary>
    protected virtual ISourceLocationProvider GetSourceLocationProvider() {
      return new SourceLocationProvider();
    }

    /// <summary>
    /// Gets a unit set defined by the given name as specified by the compilation options. For example, the name could be an external alias
    /// in C# and the compilation options will specify which referenced assemblies correspond to the external alias.
    /// Returns a dummy unit set if none of the units in this compilation correspond to the given unit set name.
    /// </summary>
    public IUnitSet GetUnitSetFor(IName unitSetName) {
      List<IUnit> units = new List<IUnit>();
      foreach (IUnitReference unitReference in this.Result.UnitReferences)
        if (unitReference.ResolvedUnit.Name.UniqueKey == unitSetName.UniqueKey)
          units.Add(unitReference.ResolvedUnit);
      if (units.Count == 0) return Dummy.UnitSet;
      return new UnitSet(units);
    }

    /// <summary>
    /// The class that contains any global variables and funcions.
    /// </summary>
    public GlobalsClass GlobalsClass {
      get {
        if (this.globalsClass == null)
          this.globalsClass = new GlobalsClass(this);
        return this.globalsClass;
      }
    }
    GlobalsClass globalsClass;

    /// <summary>
    /// Checks the compilation for errors and returns true if any were found.
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
    /// An object that represents the (mutable) environment that hosts the compiler that produced this Compilation instance.
    /// </summary>
    public ISourceEditHost HostEnvironment {
      get { return this.hostEnvironment; }
    }
    readonly ISourceEditHost hostEnvironment;

    /// <summary>
    /// A reference to the default constructor of the System.Runtime.CompilerServices.CompilerGenerated attribute.
    /// </summary>
    public IMethodReference CompilerGeneratedCtor {
      get {
        if (this.compilerGeneratedCtor == null)
          this.compilerGeneratedCtor = new MethodReference(this.HostEnvironment,
            this.PlatformType.SystemRuntimeCompilerServicesCompilerGeneratedAttribute,
             CallingConvention.HasThis, this.PlatformType.SystemVoid, this.NameTable.Ctor, 0);
        return this.compilerGeneratedCtor;
      }
    }
    private IMethodReference/*?*/ compilerGeneratedCtor;

    /// <summary>
    /// An object that associates contracts, such as preconditions and postconditions, with methods, types and loops. 
    /// </summary>
    public SourceContractProvider ContractProvider {
      get {
        if (this.contractProvider == null) {
          lock (this) {
            if (this.contractProvider == null)
              this.contractProvider = new SourceContractProvider(new ContractMethods(this.HostEnvironment), this.Result);
          }
        }
        return this.contractProvider;
      }
    }
    //^ [Once]
    SourceContractProvider/*?*/ contractProvider;

    /// <summary>
    /// A reference to the default constructor of the System.Runtime.CompilerServices.CompilerGenerated attribute.
    /// </summary>
    public IMethodReference ExtensionAttributeCtor {
      get {
        if (this.extensionAttributeCtor == null)
          this.extensionAttributeCtor = new MethodReference(this.HostEnvironment,
            this.PlatformType.SystemRuntimeCompilerServicesExtensionAttribute,
             CallingConvention.HasThis, this.PlatformType.SystemVoid, this.NameTable.Ctor, 0);
        return this.extensionAttributeCtor;
      }
    }
    private IMethodReference/*?*/ extensionAttributeCtor;

    /// <summary>
    /// An object that can provide information about the local scopes of a method.
    /// </summary>
    public ILocalScopeProvider LocalScopeProvider {
      get {
        if (this.localScopeProvider == null)
          this.localScopeProvider = this.GetLocalScopeProvider();
        return this.localScopeProvider;
      }
    }
    ILocalScopeProvider/*?*/ localScopeProvider;

    /// <summary>
    /// 
    /// </summary>
    public ModuleClass ModuleClass {
      get {
        if (this.moduleClass == null)
          this.moduleClass = new ModuleClass(this);
        return this.moduleClass;
      }
    }
    ModuleClass moduleClass;

    /// <summary>
    /// A table used to intern strings used as names. This table is obtained from the host environment.
    /// It is mutuable, in as much as it is possible to add new names to the table.
    /// </summary>
    public INameTable NameTable {
      get {
        return this.HostEnvironment.NameTable;
      }
    }

    /// <summary>
    /// Compilation options, for example whether or not checked arithmetic is the default.
    /// </summary>
    public FrameworkOptions Options {
      get { return this.options; }
    }
    readonly FrameworkOptions options;

    /// <summary>
    /// A collection of ASTs, each of which corresponds to source input to the compilation.
    /// </summary>
    public IEnumerable<CompilationPart> Parts {
      get {
        if (this.parts == null) {
          lock (GlobalLock.LockingObject) {
            if (this.parts == null)
              this.parts = this.GetPartList();
          }
        }
        List<CompilationPart> parts = this.parts;
        for (int i = 0, n = parts.Count; i < n; i++) {
          CompilationPart part = parts[i];
          if (part.Compilation != this)
            parts[i] = part = part.MakeShallowCopyFor(this);
          yield return part;
        }
      }
    }
    //^ [SpecPublic]
    List<CompilationPart>/*?*/ parts;

    /// <summary>
    /// A collection of well known types that must be part of every target platform and that are fundamental to modeling compiled code.
    /// The types are obtained by querying the unit set of the compilation and thus can include types that are defined by the compilation itself.
    /// </summary>
    public PlatformType PlatformType {
      get {
        PlatformType/*?*/ platformType = this.platformType;
        if (platformType == null) {
          platformType = new PlatformType(this.HostEnvironment);
          lock (this) {
            if (this.platformType == null)
              this.platformType = platformType;
          }
        }
        return this.platformType;
      }
    }
    //^ [Once]
    PlatformType/*?*/ platformType;

    /// <summary>
    /// The root of an object model that represents the output of a compilation. This includes a symbol table that corresponds to the
    /// metadata in a PE file, as well as compiled method bodies that corresponds to the instructions streams in a PE file.
    /// </summary>
    public Unit Result {
      get { return this.result; }
    }
    readonly Unit result;

    /// <summary>
    /// An object that can map some kinds of ILocation objects to IPrimarySourceLocation objects.
    /// In this case, it is primarily needed to map IDerivedSourceLocations and IIncludedSourceLocations to IPrimarySourceLocations.
    /// </summary>
    public ISourceLocationProvider SourceLocationProvider {
      get {
        if (this.sourceLocationProvider == null)
          this.sourceLocationProvider = this.GetSourceLocationProvider();
        return this.sourceLocationProvider;
      }
    }
    ISourceLocationProvider/*?*/ sourceLocationProvider;

    /// <summary>
    /// Returns a new Compilation instance that is the same as this instance except that the given collection of compilation parts replaces the collection from this instance.
    /// </summary>
    /// <param name="parts">A list of compilation parts that may either belong to this compilation or may be phase one
    /// initialized compilation parts that were derived from compilation parts belonging to this compilation.</param>
    /// <remarks>
    /// After a source edit, typical behavior is to construct a sub tree corresponding to the smallest enclosing syntactic declaration construct
    /// that encloses the edited source region. Then the parent of the corresponding construct in the old AST is updated with
    /// the newly constructed sub tree. The update method of the parent will in turn call the update method of its parent and so on
    /// until this method is reached. The buck stops here. The resulting compilation node is a mixture of old compilation parts and new compilation parts
    /// kept in the this this.parts field. If a compilation part is actually visited (by means of a traversal of the enumeration returned by this.Parts,
    /// then each returned compilation part will be a shallow (reparented) copy of the old part, so that the mixture is never observable,
    /// but deep copies are only made when absolutely necessary.
    /// </remarks>
    public abstract Compilation UpdateCompilationParts(IEnumerable<CompilationPart> parts);

    /// <summary>
    /// A set of units comprised by the result of the compilation along with all of the units referenced by this compilation.
    /// </summary>
    public UnitSet UnitSet {
      get {
        if (this.unitSet == null) {
          lock (GlobalLock.LockingObject) {
            if (this.unitSet == null)
              this.unitSet = this.CreateUnitSet();
          }
        }
        return this.unitSet;
      }
    }
    //^ [Once]
    UnitSet/*?*/ unitSet;

    //TODO: think about excluding the result of the compilation. It is not needed and precludes reuse of unit sets among different versions of the compilation.
    private UnitSet CreateUnitSet() {
      List<IUnit> units = new List<IUnit>();
      units.Add(this.Result);
      foreach (IUnitReference unitReference in this.Result.UnitReferences)
        units.Add(unitReference.ResolvedUnit);
      units.TrimExcess();
      return new UnitSet(units.AsReadOnly());
    }


    #region ICompilation Members

    IPlatformType ICompilation.PlatformType {
      get { return this.PlatformType; }
    }

    IUnit ICompilation.Result {
      get {
        return this.Result;
      }
    }

    IUnitSet ICompilation.UnitSet {
      get { return this.UnitSet; }
    }

    #endregion

  }

  /// <summary>
  /// A part of a compilation that has been derived from a single source document. 
  /// </summary>
  public abstract class CompilationPart : CheckableSourceItem, IErrorCheckable {

    /// <summary>
    /// Initializes a part of a compilation that has been derived from a single source document. 
    /// </summary>
    /// <param name="helper">An instance of a language specific class containing methods that are of general utility during semantic analysis.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated compilation part.</param>
    protected CompilationPart(LanguageSpecificCompilationHelper helper, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.helper = helper;
      this.globalDeclarationContainer = new GlobalDeclarationContainerClass(helper.Compilation.HostEnvironment);
    }

    /// <summary>
    /// Initializes a part of a compilation that has been derived from a single source document. 
    /// </summary>
    /// <param name="helper">An instance of a language specific class containing methods that are of general utility.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated compiltation part.</param>
    /// <param name="globalDeclarationContainer">A class that contains global variables and methods as its members.</param>
    protected CompilationPart(LanguageSpecificCompilationHelper helper, ISourceLocation sourceLocation, GlobalDeclarationContainerClass globalDeclarationContainer)
      : base(sourceLocation) {
      this.helper = helper;
      this.globalDeclarationContainer = globalDeclarationContainer;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its helper object.
    /// </summary>
    /// <param name="helper">The helper object the new copy. This should be different from the helper object the template part.</param>
    /// <param name="template">The compilation part to copy.</param>
    protected CompilationPart(LanguageSpecificCompilationHelper helper, CompilationPart template)
      : base(template.SourceLocation)
      //^ requires helper != template.Helper;
    {
      this.helper = helper;
      this.globalDeclarationContainer = template.GlobalDeclarationContainer; //TODO: make a copy
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the compilation part.
    /// Do not call this method directly, but evaluate the HasErrors property. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      bool result = false;
      result |= this.GlobalDeclarationContainer.HasErrors;
      result |= this.RootNamespace.HasErrors;
      return result;
    }

    /// <summary>
    /// The compilation to which this part belongs.
    /// </summary>
    public Compilation Compilation {
      get { return this.Helper.Compilation; }
    }

    /// <summary>
    /// Calls the visitor.Visit(CompilationPart) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// A class that contains global variables and methods as its members.
    /// </summary>
    public GlobalDeclarationContainerClass GlobalDeclarationContainer {
      get { return this.globalDeclarationContainer; }
    }
    readonly GlobalDeclarationContainerClass globalDeclarationContainer;

    /// <summary>
    /// An instance of a language specific class containing methods that are of general utility. 
    /// </summary>
    public LanguageSpecificCompilationHelper Helper {
      get { return this.helper; }
    }
    readonly LanguageSpecificCompilationHelper helper;

    /// <summary>
    /// Makes a shallow copy of this compilation part that can be added to the compilation parts list of the given target compilation.
    /// The shallow copy may share child objects with this instance, but should never expose such child objects except through
    /// wrappers (or shallow copies made on demand). If this instance is already a part of the target compilation it
    /// returns itself.
    /// </summary>
    /// <remarks>Do not call this method on compilation parts that already return the target compilation as the value of their
    /// compilation property. (Compilers that do not support incremental compilation might throw an exception if this method is called.)</remarks>
    /// <param name="targetCompilation">The compilation is to be the parent compilation of the new compilation part.</param>
    //^ [MustOverride]
    public abstract CompilationPart MakeShallowCopyFor(Compilation targetCompilation);
    //^ ensures result.GetType() == this.GetType();

    /// <summary>
    /// An anonymous namespace that contains all of the top level types and namespaces found in this compilation part.
    /// </summary>
    public virtual RootNamespaceDeclaration RootNamespace {
      //^ [MustOverride] //TODO Boogie: perhaps MustOverride should also mean that the override may not call back? If not, an additional attribute is needed.
      get {
        //^ assume this.rootNamespace != null; //This assumes that the derived class will not call back here
        return this.rootNamespace;
      }
    }
    /// <summary>
    /// 
    /// </summary>
    protected RootNamespaceDeclaration/*?*/ rootNamespace;

    /// <summary>
    /// Returns a new CompilationPart instance that is the same as this instance except that the root namespace has been replaced with the given namespace.
    /// </summary>
    public abstract CompilationPart UpdateRootNamespace(RootNamespaceDeclaration rootNamespace);
    //^ requires this.RootNamespace.GetType() == rootNamespace.GetType();

    /// <summary>
    /// Returns a compilation part that is based on the source document that is the result of the given edit and that computed as an incremental change to this compilation part.
    /// Also updates the given list of compilation parts so that it includes the resulting part rather than this part.
    /// </summary>
    /// <param name="edit">A text edit that was applied to the source document on which this compilation part is based.</param>
    /// <param name="updatedParts">The list of parts of the updated version of this.Compilation. The entry of the list that contains this part is updated with the resulting part.</param>
    /// <param name="editEventArgs">An EditEventArgs object that describes the incremental change is assigned to this out parameter.</param>
    /// <param name="symbolTableEditEventArgs">An EditEventArgs object that describes only the changes that are not confined to method bodies is assigned to this out parameter. 
    /// Null if the edit did not result in such changes.</param>
    public virtual CompilationPart UpdateWith(AstSourceDocumentEdit edit, IList<CompilationPart> updatedParts, out EditEventArgs editEventArgs, out EditEventArgs/*?*/ symbolTableEditEventArgs)
      //^ requires this.SourceLocation.SourceDocument == edit.SourceLocationBeforeEdit.SourceDocument;
      //^ requires edit.SourceLocationBeforeEdit.SourceDocument.GetType() == edit.SourceDocumentAfterEdit.GetType();
      //^ requires this.SourceLocation.Contains(edit.SourceLocationBeforeEdit);
      //^ requires this.RootNamespace.SourceLocation.Contains(edit.SourceLocationBeforeEdit);
      //^ ensures result.GetType() == this.GetType();
    {
      RootNamespaceDeclaration rootNamespace;
      MemberFinder memberFinder = new MemberFinder(edit.SourceLocationBeforeEdit);
      memberFinder.Visit(this.RootNamespace);
      if (memberFinder.MostNestedContainingTypeDeclarationMember != null)
        rootNamespace = this.GetUpdatedRootNamespace(memberFinder.MostNestedContainingTypeDeclarationMember, edit, updatedParts, out editEventArgs, out symbolTableEditEventArgs);
      else if (memberFinder.MostNestedContainingNamespaceDeclarationMember != null)
        rootNamespace = this.GetUpdatedRootNamespace(memberFinder.MostNestedContainingNamespaceDeclarationMember, edit, updatedParts, out editEventArgs, out symbolTableEditEventArgs);
      else
        rootNamespace = this.GetUpdatedRootNamespace(edit, updatedParts, out editEventArgs, out symbolTableEditEventArgs);
      return rootNamespace.CompilationPart;
    }

    /// <summary>
    /// Returns a root namespace that is based on the source document that is the result of the given edit and that computed as an incremental change to this compilation part.
    /// Also updates the given list of compilation parts so that it includes the compilation part of the resulting root namespace rather than this part.
    /// </summary>
    /// <param name="oldMember">A namespace member that entirely encloses the given edit.</param>
    /// <param name="edit">A text edit that was applied to the source document on which this compilation part is based.</param>
    /// <param name="updatedParts">The list of parts of the updated version of this.Compilation. 
    /// The entry of the list that contains this part is updated with result.CompilationPart.</param>
    /// <param name="editEventArgs">An EditEventArgs object that describes the incremental change is assigned to this out parameter.</param>
    /// <param name="symbolTableEditEventArgs">An EditEventArgs object that describes only the changes that are not confined to method bodies is assigned to this out parameter. 
    /// Null if the edit did not result in such changes.</param>
    public virtual RootNamespaceDeclaration GetUpdatedRootNamespace(INamespaceDeclarationMember oldMember,
      AstSourceDocumentEdit edit, IList<CompilationPart> updatedParts, out EditEventArgs editEventArgs, out EditEventArgs/*?*/ symbolTableEditEventArgs)
      //^ requires this.SourceLocation.SourceDocument == edit.SourceLocationBeforeEdit.SourceDocument;
      //^ requires oldMember.SourceLocation.Contains(edit.SourceLocationBeforeEdit);
      //^ requires this.SourceLocation.SourceDocument == oldMember.SourceLocation.SourceDocument;
      //^ requires oldMember.ContainingNamespaceDeclaration.CompilationPart == this;
      //^ ensures result.CompilationPart.GetType() == this.GetType();
    {
      INamespaceDeclarationMember/*?*/ newMember = this.ParseAsNamespaceDeclarationMember(oldMember.SourceLocation, edit);
      if (newMember == null || newMember.GetType() != oldMember.GetType()) {
        //This can happen when the edit splits the containing namespace declaration member into two or more members, or if the edit results in the deletion of the member.
        INamespaceDeclarationMember/*?*/ outerMember = oldMember.ContainingNamespaceDeclaration as INamespaceDeclarationMember;
        if (outerMember != null)
          return this.GetUpdatedRootNamespace(outerMember, edit, updatedParts, out editEventArgs, out symbolTableEditEventArgs);
        return this.GetUpdatedRootNamespace(edit, updatedParts, out editEventArgs, out symbolTableEditEventArgs);
      }
      RootNamespaceDeclaration result = this.GetUpdatedRootNamespace(oldMember, newMember, edit, true); //requires oldMember.ContainingNamespaceDeclaration.CompilationPart == this;
      this.CompareOldAndNew(oldMember, newMember, edit, out editEventArgs, out symbolTableEditEventArgs);
      return result;
    }

    /// <summary>
    /// Returns a root namespace that is based on the source document that is the result of the given edit and that computed as an incremental change to this compilation part.
    /// Also updates the given list of compilation parts so that it includes the compilation part of the resulting root namespace rather than this part.
    /// </summary>
    /// <param name="oldMember">A namespace member that entirely encloses the given edit.</param>
    /// <param name="newMember">A namespace member that is the result of applying the given edit to oldMember.</param>
    /// <param name="edit">A text edit that was applied to the source document on which this compilation part is based.</param>
    /// <param name="editEventArgs">An EditEventArgs object that describes the incremental change is assigned to this out parameter.</param>
    /// <param name="symbolTableEditEventArgs">An EditEventArgs object that describes only the changes that are not confined to method bodies is assigned to this out parameter. 
    /// Null if the edit did not result in such changes.</param>
    private void CompareOldAndNew(INamespaceDeclarationMember oldMember, INamespaceDeclarationMember newMember,
      ISourceDocumentEdit edit, out EditEventArgs editEventArgs, out EditEventArgs/*?*/ symbolTableEditEventArgs)
      //^ requires oldMember.GetType() == newMember.GetType();
      //^ requires newMember.ContainingNamespaceDeclaration.CompilationPart.SourceLocation.SourceDocument.IsUpdatedVersionOf(this.SourceLocation.SourceDocument);
    {
      //TODO; need to compare the members of old and new with each other. 
      //Try to figure out deletions vs insertions vs modifications.
      //Need one case for nested namespace, another for namespace type
      IAggregatableNamespaceDeclarationMember/*?*/ aggMember = newMember as IAggregatableNamespaceDeclarationMember;
      List<IEditDescriptor> edits = new List<IEditDescriptor>(1);
      if (aggMember != null)
        edits.Add(new EditDescriptor(EditEventKind.Modification, aggMember.AggregatedMember,
          aggMember.ContainingNamespaceDeclaration.UnitNamespace, oldMember.ContainingNamespaceDeclaration.UnitNamespace,
          aggMember.ContainingNamespaceDeclaration.CompilationPart.SourceLocation.SourceDocument, this.SourceLocation.SourceDocument));
      else {
        INamespaceDefinition oldParent = oldMember.ContainingNamespaceDeclaration.UnitNamespace;
        INestedUnitNamespace/*?*/ oldNestedParent = oldParent as INestedUnitNamespace;
        if (oldNestedParent != null) oldParent = oldNestedParent.ContainingNamespace;
        INamespaceDefinition newParent = newMember.ContainingNamespaceDeclaration.UnitNamespace;
        INestedUnitNamespace/*?*/ newNestedParent = newParent as INestedUnitNamespace;
        if (newNestedParent != null) newParent = newNestedParent.ContainingNamespace;
        edits.Add(new EditDescriptor(EditEventKind.Modification, newMember.ContainingNamespaceDeclaration.UnitNamespace,
          newParent, oldParent, newMember.ContainingNamespaceDeclaration.CompilationPart.SourceLocation.SourceDocument, this.SourceLocation.SourceDocument));
      }
      editEventArgs = new EditEventArgs(edits);
      symbolTableEditEventArgs = editEventArgs;
      if (edit == null || oldMember == null) return; //Dummy use of edit until TODO is done.
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="oldMember"></param>
    /// <param name="newMember"></param>
    /// <param name="edit"></param>
    /// <param name="recurse"></param>
    /// <returns></returns>
    public virtual RootNamespaceDeclaration GetUpdatedRootNamespace(INamespaceDeclarationMember oldMember, INamespaceDeclarationMember newMember,
      ISourceDocumentEdit edit, bool recurse)
      //^ requires newMember is NamespaceDeclarationMember || newMember is NamespaceTypeDeclaration || newMember is NestedNamespaceDeclaration;
      //^ requires oldMember.ContainingNamespaceDeclaration.CompilationPart == this;
      //^ requires edit.SourceDocumentAfterEdit.IsUpdatedVersionOf(this.SourceLocation.SourceDocument);
      //^ ensures result.CompilationPart.GetType() == this.GetType();
    {
      while (true)
      //^ invariant newMember is NamespaceDeclarationMember || newMember is NamespaceTypeDeclaration || newMember is NestedNamespaceDeclaration;
      // ^ invariant oldMember.ContainingNamespaceDeclaration.CompilationPart == this;
      // ^ invariant edit.SourceDocumentAfterEdit.IsUpdatedVersionOf(this.SourceLocation.SourceDocument);
      {
        //^ assume oldMember.ContainingNamespaceDeclaration.CompilationPart == this;
        List<INamespaceDeclarationMember> newMembers = new List<INamespaceDeclarationMember>(oldMember.ContainingNamespaceDeclaration.Members);
        for (int i = 0, n = newMembers.Count; i < n; i++) {
          if (newMembers[i] == oldMember) { newMembers[i] = newMember; break; }
        }
        //^ assume oldMember.ContainingNamespaceDeclaration.CompilationPart == this;
        //^ assume edit.SourceDocumentAfterEdit.IsUpdatedVersionOf(this.SourceLocation.SourceDocument);
        RootNamespaceDeclaration/*?*/ oldRootNs = oldMember.ContainingNamespaceDeclaration as RootNamespaceDeclaration;
        if (oldRootNs != null) {
          //^ assume oldRootNs.CompilationPart == this;
          //This is the expected way to terminate the loop
          RootNamespaceDeclaration result = (RootNamespaceDeclaration)oldRootNs.UpdateMembers(newMembers, edit);
          //^ assume result.CompilationPart.GetType() == this.GetType();
          this.SetContainingNamespace(newMember, result, recurse);
          return result;
        }
        NestedNamespaceDeclaration/*?*/ oldNestedNs = oldMember.ContainingNamespaceDeclaration as NestedNamespaceDeclaration;
        if (oldNestedNs == null) {
          //^ assume false; //To get here, oldMember.ContainingNamespaceDeclaration has to be neither a RootNamespaceDeclaration, nor an NestedNamespaceDeclaration.
          return this.RootNamespace;
        }
        NestedNamespaceDeclaration newNestedNs = (NestedNamespaceDeclaration)oldNestedNs.UpdateMembers(newMembers, edit);
        this.SetContainingNamespace(newMember, newNestedNs, recurse);
        recurse = false;
        newMember = newNestedNs;
        //^ assume oldNestedNs.ContainingNamespaceDeclaration.CompilationPart == this;
        oldMember = oldNestedNs;
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="newMember"></param>
    /// <param name="result"></param>
    /// <param name="recurse"></param>
    public virtual void SetContainingNamespace(INamespaceDeclarationMember newMember, NamespaceDeclaration result, bool recurse)
      //^ requires newMember is NamespaceDeclarationMember || newMember is NamespaceTypeDeclaration || newMember is NestedNamespaceDeclaration;
    {
      NamespaceDeclarationMember/*?*/ nsdMember = newMember as NamespaceDeclarationMember;
      if (nsdMember != null)
        nsdMember.SetContainingNamespaceDeclaration(result, recurse);
      else {
        NamespaceTypeDeclaration/*?*/ nstd = newMember as NamespaceTypeDeclaration;
        if (nstd != null) {
          nstd.SetContainingNamespaceDeclaration(result, recurse);
        } else {
          NestedNamespaceDeclaration nnsd = (NestedNamespaceDeclaration)newMember;
          nnsd.SetContainingNamespaceDeclaration(result, recurse);
        }
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="edit"></param>
    /// <param name="updatedParts"></param>
    /// <param name="editEventArgs"></param>
    /// <param name="symbolTableEditEventArgs"></param>
    /// <returns></returns>
    public virtual RootNamespaceDeclaration GetUpdatedRootNamespace(AstSourceDocumentEdit edit, IList<CompilationPart> updatedParts, out EditEventArgs editEventArgs, out EditEventArgs/*?*/ symbolTableEditEventArgs)
      //^ requires this.SourceLocation.SourceDocument == edit.SourceLocationBeforeEdit.SourceDocument;
      //^ requires this.SourceLocation.Contains(edit.SourceLocationBeforeEdit);
      //^ ensures result.CompilationPart.GetType() == this.GetType();
    {
      RootNamespaceDeclaration result = edit.CompilationPartAfterEdit.ParseAsRootNamespace();
      for (int i = 0, n = updatedParts.Count; i < n; i++) {
        if (updatedParts[i] == this) { updatedParts[i] = edit.CompilationPartAfterEdit; break; }
      }
      List<IEditDescriptor> edits = new List<IEditDescriptor>(1);
      edits.Add(new EditDescriptor(EditEventKind.Modification, result.UnitNamespace,
        result.UnitNamespace, this.RootNamespace.UnitNamespace, result.CompilationPart.SourceLocation.SourceDocument, this.SourceLocation.SourceDocument));
      editEventArgs = new EditEventArgs(edits.AsReadOnly());
      symbolTableEditEventArgs = editEventArgs;
      //^ assume result.CompilationPart.GetType() == this.GetType();
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="oldMember"></param>
    /// <param name="edit"></param>
    /// <param name="updatedParts"></param>
    /// <param name="editEventArgs"></param>
    /// <param name="symbolTableEditEventArgs"></param>
    /// <returns></returns>
    public virtual RootNamespaceDeclaration GetUpdatedRootNamespace(ITypeDeclarationMember oldMember,
      AstSourceDocumentEdit edit, IList<CompilationPart> updatedParts, out EditEventArgs editEventArgs, out EditEventArgs/*?*/ symbolTableEditEventArgs)
      //^ requires this.SourceLocation.SourceDocument == edit.SourceLocationBeforeEdit.SourceDocument;
      //^ requires oldMember.SourceLocation.Contains(edit.SourceLocationBeforeEdit);
      //^ ensures result.CompilationPart.GetType() == this.GetType();
    {
      ITypeDeclarationMember/*?*/ outerMember = oldMember.ContainingTypeDeclaration as ITypeDeclarationMember;
      ITypeDeclarationMember/*?*/ newMember = this.ParseAsTypeDeclarationMember(oldMember.SourceLocation, edit, oldMember.ContainingTypeDeclaration.Name);
      if (newMember == null || newMember.GetType() != oldMember.GetType() || newMember.TypeDefinitionMember == null) {
        //This can happen when the edit splits the containing type declaration member into two or more members, or if the edit results in the deletion of the member.
        if (outerMember != null) {
          //^ assume false; //unsatisfied precondition: requires this.SourceLocation.SourceDocument == edit.SourceLocationBeforeEdit.SourceDocument;
          return this.GetUpdatedRootNamespace(outerMember, edit, updatedParts, out editEventArgs, out symbolTableEditEventArgs);
        }
        INamespaceDeclarationMember/*?*/ outerNsMember = oldMember.ContainingTypeDeclaration as INamespaceDeclarationMember;
        if (outerNsMember != null) {
          //^ assume false; //unsatisfied precondition: requires this.SourceLocation.SourceDocument == edit.SourceLocationBeforeEdit.SourceDocument;
          return this.GetUpdatedRootNamespace(outerNsMember, edit, updatedParts, out editEventArgs, out symbolTableEditEventArgs);
        }
        //Get here when splitting or deleting a type nested in the root namespace.
        //^ assume false; //unsatisfied precondition: requires this.SourceLocation.SourceDocument == edit.SourceLocationBeforeEdit.SourceDocument;
        return this.GetUpdatedRootNamespace(edit, updatedParts, out editEventArgs, out symbolTableEditEventArgs);
      }
      //^ assume false; //unsatisfied precondition: requires this.SourceLocation.SourceDocument == edit.SourceLocationBeforeEdit.SourceDocument;
      RootNamespaceDeclaration result = this.GetUpdatedRootNamespace(oldMember, newMember, edit, true);
      this.CompareOldAndNew(oldMember, newMember, edit, out editEventArgs, out symbolTableEditEventArgs);
      return result;
    }

    private void CompareOldAndNew(ITypeDeclarationMember oldMember, ITypeDeclarationMember newMember,
      ISourceDocumentEdit edit, out EditEventArgs editEventArgs, out EditEventArgs/*?*/ symbolTableEditEventArgs)
      //^ requires oldMember.TypeDefinitionMember != null && newMember.TypeDefinitionMember != null;
      //^ requires this.SourceLocation.SourceDocument == edit.SourceLocationBeforeEdit.SourceDocument;
      //^ requires oldMember.SourceLocation.Contains(edit.SourceLocationBeforeEdit);
      //^ requires newMember.CompilationPart.SourceLocation.SourceDocument.IsUpdatedVersionOf(this.SourceLocation.SourceDocument);
    {
      //TODO: actual comparison
      List<IEditDescriptor> edits = new List<IEditDescriptor>(1);
      edits.Add(new EditDescriptor(EditEventKind.Modification, newMember.TypeDefinitionMember,
        newMember.TypeDefinitionMember.ContainingTypeDefinition, oldMember.TypeDefinitionMember.ContainingTypeDefinition, newMember.CompilationPart.SourceLocation.SourceDocument, this.SourceLocation.SourceDocument));
      editEventArgs = new EditEventArgs(edits);
      symbolTableEditEventArgs = editEventArgs;
      MethodDeclaration/*?*/ oldMethod = oldMember as MethodDeclaration;
      if (oldMethod != null && !oldMethod.IsAbstract && !oldMethod.IsExternal && newMember is MethodDeclaration) {
        foreach (ILocation location in oldMethod.Body.Locations) {
          ISourceLocation/*?*/ sourceLocation = location as ISourceLocation;
          if (sourceLocation != null && sourceLocation.Contains(edit.SourceLocationBeforeEdit)) {
            symbolTableEditEventArgs = null;
          }
        }
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="oldMember"></param>
    /// <param name="newMember"></param>
    /// <param name="edit"></param>
    /// <param name="recurse"></param>
    /// <returns></returns>
    public virtual RootNamespaceDeclaration GetUpdatedRootNamespace(ITypeDeclarationMember oldMember, ITypeDeclarationMember newMember,
      ISourceDocumentEdit edit, bool recurse)
      //^ requires newMember is TypeDeclarationMember || newMember is NestedTypeDeclaration;
      //^ requires this.SourceLocation.SourceDocument == edit.SourceLocationBeforeEdit.SourceDocument;
      //^ requires oldMember.SourceLocation.Contains(edit.SourceLocationBeforeEdit);
      //^ requires newMember.CompilationPart.SourceLocation.SourceDocument.IsUpdatedVersionOf(this.SourceLocation.SourceDocument);
      //^ ensures result.CompilationPart.GetType() == this.GetType();
    {
      while (true)
      //^ invariant newMember is TypeDeclarationMember || newMember is NestedTypeDeclaration;
      {
        List<ITypeDeclarationMember> newMembers = new List<ITypeDeclarationMember>(oldMember.ContainingTypeDeclaration.TypeDeclarationMembers);
        for (int i = 0, n = newMembers.Count; i < n; i++) {
          if (newMembers[i] == oldMember) { newMembers[i] = newMember; break; }
        }
        NamespaceTypeDeclaration/*?*/ oldNsType = oldMember.ContainingTypeDeclaration as NamespaceTypeDeclaration;
        if (oldNsType != null) {
          //This is the expected way to terminate the loop
          //^ assume false; //unsatisfied precondition: requires edit.SourceDocumentAfterEdit.IsUpdatedVersionOf(this.SourceLocation.SourceDocument);
          NamespaceTypeDeclaration newNsType = (NamespaceTypeDeclaration)oldNsType.UpdateMembers(newMembers, edit);
          this.SetContainingTypeDeclaration(newMember, newNsType, recurse);
          return this.GetUpdatedRootNamespace(oldNsType, newNsType, edit, false);
        }
        NestedTypeDeclaration/*?*/ oldNestedType = oldMember.ContainingTypeDeclaration as NestedTypeDeclaration;
        if (oldNestedType == null) {
          //^ assume false; //To get here, oldMember.ContainingTypeDeclaration has to be neither an NamespaceTypeDeclaration, nor an NestedTypeDeclaration.
          return this.RootNamespace;
        }
        //^ assume false; //unsatisfied precondition: requires edit.SourceDocumentAfterEdit.IsUpdatedVersionOf(this.SourceLocation.SourceDocument);
        NestedTypeDeclaration newNestedType = (NestedTypeDeclaration)oldNestedType.UpdateMembers(newMembers, edit);
        this.SetContainingTypeDeclaration(newMember, newNestedType, recurse);
        recurse = false;
        newMember = newNestedType;
        oldMember = oldNestedType;
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="newMember"></param>
    /// <param name="newNsType"></param>
    /// <param name="recurse"></param>
    public virtual void SetContainingTypeDeclaration(ITypeDeclarationMember newMember, TypeDeclaration newNsType, bool recurse)
      //^ requires newMember is TypeDeclarationMember || newMember is NestedTypeDeclaration;
    {
      TypeDeclarationMember/*?*/ tdMember = newMember as TypeDeclarationMember;
      if (tdMember != null)
        tdMember.SetContainingTypeDeclaration(newNsType, recurse);
      else {
        NestedTypeDeclaration ntd = (NestedTypeDeclaration)newMember;
        ntd.SetContainingTypeDeclaration(newNsType, recurse);
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sourceLocationBeforeEdit"></param>
    /// <param name="edit"></param>
    /// <returns></returns>
    public virtual INamespaceDeclarationMember/*?*/ ParseAsNamespaceDeclarationMember(ISourceLocation sourceLocationBeforeEdit, ISourceDocumentEdit edit)
      // ^ requires this.SourceLocation.SourceDocument == sourceLocationBeforeEdit.SourceDocument;
      // ^ requires this.SourceLocation.SourceDocument == edit.SourceLocationBeforeEdit.SourceDocument;
      // ^ requires sourceLocationBeforeEdit.Contains(edit.SourceLocationBeforeEdit);
      // ^ requires edit.SourceDocumentAfterEdit.IsUpdatedVersionOf(sourceLocationBeforeEdit.SourceDocument);
      //^ ensures result == null || result is NamespaceDeclarationMember || result is NamespaceTypeDeclaration || result is NestedNamespaceDeclaration;
    {
      //^ assume false; //Not all languages have namespace members. Such languages should see to it that this method never gets called. 
      throw new NotSupportedException();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public virtual RootNamespaceDeclaration ParseAsRootNamespace()
      //^ ensures result.CompilationPart.GetType() == this.GetType();
      //^ ensures result.CompilationPart.SourceLocation.SourceDocument == this.SourceLocation.SourceDocument;
    {
      //^ assume false; //Not all languages have namespace members. Such languages should see to it that this method never gets called. 
      throw new NotSupportedException();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sourceLocationBeforeEdit"></param>
    /// <param name="edit"></param>
    /// <param name="typeName"></param>
    /// <returns></returns>
    public virtual ITypeDeclarationMember/*?*/ ParseAsTypeDeclarationMember(ISourceLocation sourceLocationBeforeEdit, ISourceDocumentEdit edit, IName typeName)
      // ^ requires this.SourceLocation.SourceDocument == sourceLocationBeforeEdit.SourceDocument;
      // ^ requires this.SourceLocation.SourceDocument == edit.SourceLocationBeforeEdit.SourceDocument;
      // ^ requires sourceLocationBeforeEdit.Contains(edit.SourceLocationBeforeEdit);
      // ^ requires edit.SourceDocumentAfterEdit.IsUpdatedVersionOf(sourceLocationBeforeEdit.SourceDocument);
      //^ ensures result == null || result is TypeDeclarationMember || result is NestedTypeDeclaration;
    {
      //^ assume false; //Not all languages have namespace members. Such languages should see to it that this method never gets called. 
      throw new NotSupportedException();
    }

    /// <summary>
    /// Call this only when holding a lock on GlobalLock.LockingObject. Use the result only while holding the lock.
    /// </summary>
    internal Dictionary<MethodDeclaration, IPlatformInvokeInformation> PlatformInvokeInformationTable {
      get {
        if (this.platformInvokeInformationTable == null)
          this.platformInvokeInformationTable = new Dictionary<MethodDeclaration, IPlatformInvokeInformation>();
        return this.platformInvokeInformationTable;
      }
    }
    internal Dictionary<MethodDeclaration, IPlatformInvokeInformation>/*?*/ platformInvokeInformationTable;

  }

  /// <summary>
  /// An AST node derived from a single region of source code and which represents a symbol declaration.
  /// </summary>
  public interface IDeclaration : ISourceItem {

    /// <summary>
    /// Custom attributes that are to be persisted in the metadata.
    /// </summary>
    IEnumerable<ICustomAttribute> Attributes { get; }

    /// <summary>
    /// The compilation part that this declaration forms a part of.
    /// </summary>
    CompilationPart CompilationPart { get; }

    /// <summary>
    /// Custom attributes that are explicitly specified in source. Some of these may not end up in persisted metadata.
    /// </summary>
    IEnumerable<SourceCustomAttribute> SourceAttributes { get; }

  }

  /// <summary>
  /// An object that has been derived from a portion of a source document.
  /// </summary>
  public interface ISourceItem {

    /// <summary>
    /// The location in the source document that has been parsed to construct this item. 
    /// </summary>
    ISourceLocation SourceLocation { get; }

  }

  /// <summary>
  /// A source traverser that finds the most nested namespace or type member that spans a given source location.
  /// </summary>
  public class MemberFinder : SourceTraverser {

    /// <summary>
    /// Allocates a source traverser that finds the most nested namespace or type member that spans a given source location.
    /// </summary>
    /// <param name="locationToContain">The source location that should be contained by namespace or type member, if any, that this instance is going to try and find.</param>
    public MemberFinder(ISourceLocation locationToContain) {
      this.locationToContain = locationToContain;
    }

    /// <summary>
    /// The source location that should be contained by namespace or type member, if any, that this instance is going to try and find.
    /// </summary>
    protected ISourceLocation LocationToContain {
      get { return this.locationToContain; }
    }
    private ISourceLocation locationToContain;

    /// <summary>
    /// The most nested namespace declaration member that fully contains the location that was supplied when this instance of MemberFinder was constructed.
    /// May be null, in which case either no such member exists, or this visitor has not yet been dispatched on a member that contains the location.
    /// </summary>
    public INamespaceDeclarationMember/*?*/ MostNestedContainingNamespaceDeclarationMember {
      get
        //^ ensures result == null || result.SourceLocation.Contains(this.LocationToContain);
      {
        //^ assume this.mostNestedContainingNamespaceDeclarationMember == null || this.mostNestedContainingNamespaceDeclarationMember.SourceLocation.Contains(this.LocationToContain); //follows from invariant that Boogie chokes on
        return this.mostNestedContainingNamespaceDeclarationMember;
      }
    }
    INamespaceDeclarationMember/*?*/ mostNestedContainingNamespaceDeclarationMember;
    // ^ invariant mostNestedContainingNamespaceDeclarationMember == null || mostNestedContainingNamespaceDeclarationMember.SourceLocation.Contains(this.LocationToContain);

    /// <summary>
    /// The most nested type declaration member that fully contains the location that was supplied when this instance of MemberFinder was constructed.
    /// May be null, in which case either no such member exists, or this visitor has not yet been dispatched on a member that contains the location.
    /// </summary>
    public ITypeDeclarationMember/*?*/ MostNestedContainingTypeDeclarationMember {
      get
        //^ ensures result == null || result.SourceLocation.Contains(this.LocationToContain);
      {
        //^ assume this.mostNestedContainingTypeDeclarationMember == null || this.mostNestedContainingTypeDeclarationMember.SourceLocation.Contains(this.LocationToContain); //follows from invariant that Boogie chokes on
        return this.mostNestedContainingTypeDeclarationMember;
      }
    }
    ITypeDeclarationMember/*?*/ mostNestedContainingTypeDeclarationMember;
    // ^ invariant mostNestedContainingTypeDeclarationMember == null || mostNestedContainingTypeDeclarationMember.SourceLocation.Contains(this.LocationToContain);
    //TODO: Boogie can't handle this invariant:

    /// <summary>
    /// If the given namespace declaration member contains the location to find, remember it in this.MostNestedContainingNamespaceDeclarationMember.
    /// </summary>
    public override void VisitNamespaceDeclarationMember(INamespaceDeclarationMember namespaceDeclarationMember)
      //^^ ensures this.path.Count == old(this.path.Count);
    {
      //^ int oldCount = this.path.Count;
      if (namespaceDeclarationMember.SourceLocation.Contains(this.LocationToContain)) {
        this.mostNestedContainingNamespaceDeclarationMember = namespaceDeclarationMember;
        base.VisitNamespaceDeclarationMember(namespaceDeclarationMember);
      }
      //TODO: stop the traversal once it proceeds beyond this.LocationToContain.
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
    }

    /// <summary>
    /// If the given type declaration member contains the location to find, remember it in this.MostNestedContainingTypeDeclarationMember.
    /// </summary>
    public override void VisitTypeDeclarationMember(ITypeDeclarationMember typeDeclarationMember)
      //^^ ensures this.path.Count == old(this.path.Count);
    {
      //^ int oldCount = this.path.Count;
      if (typeDeclarationMember.SourceLocation.Contains(this.LocationToContain)) {
        this.mostNestedContainingTypeDeclarationMember = typeDeclarationMember;
        base.VisitTypeDeclarationMember(typeDeclarationMember);
      }
      //TODO: stop the traversal once it proceeds beyond this.LocationToContain.
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
    }
  }

  /// <summary>
  /// A class containing methods that are of general utility during semantic analysis. Some of these methods are language specific and are expected to be
  /// overridden in a language specific derived class. This class defaults to C# behavior for such methods.
  /// Unlike CompilationHelper, instances of this class are specific to a particular compilation.
  /// </summary>
  public class LanguageSpecificCompilationHelper {

    /// <summary>
    /// Allocates an instance of a class containing methods that are of general utility during semantic analysis. Some of these methods are language specific and are expected to be
    /// overridden in a language specific derived class. This class defaults to C# behavior for such methods.
    /// Unlike CompilationHelper, instances of this class are specific to a particular compilation.
    /// </summary>
    /// <param name="compilation">The compilation with which the resulting helper object is associated.</param>
    /// <param name="languageName">The language for which the methods of this helper object implement this appropriate semantics.</param>
    public LanguageSpecificCompilationHelper(Compilation compilation, string languageName) {
      this.compilation = compilation;
      this.languageName = languageName;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its associated compilation.
    /// </summary>
    /// <param name="targetCompilation">A new value for the associated compilation. This replaces template.Compilation in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected LanguageSpecificCompilationHelper(Compilation targetCompilation, LanguageSpecificCompilationHelper template) {
      this.compilation = targetCompilation;
      this.languageName = template.languageName;
    }

    /// <summary>
    /// Returns an expression that adds a nullable wrapper to the value produced by the given expression, if needed.
    /// </summary>
    /// <param name="expression">The expression that results in the value to convert.</param>
    /// <param name="unwrappedType">The element type of of the nullable wrapper. Must be equal to this.RemoveNullableWrapper(nullableType).</param>
    /// <param name="type">The type of the nullable wrapper. Must be derived from the value of the type parameter.</param>
    //^ [Pure]
    protected virtual Expression AddNullableWrapperIfNeeded(Expression expression, ITypeDefinition unwrappedType, ITypeDefinition type)
      //^ requires unwrappedType == type || unwrappedType == this.RemoveNullableWrapper(type);
      //^ ensures result == expression || result is MethodCall;
    {
      if (unwrappedType == type) return expression;
      //TODO: C# v2 does not call the conversion, but invokes the constructor. Find out if this matters.
      Expression result = this.UserDefinedConversion(expression, unwrappedType, type, false);
      if (result is MethodCall) return result;
      //^ assume false;
      return expression;
    }

    /// <summary>
    /// The compilation for which this helper exists. In general, a single compilation instance can have multiple language specific compilation helpers.
    /// </summary>
    public Compilation Compilation {
      get { return this.compilation; }
    }
    readonly Compilation compilation;

    /// <summary>
    /// Returns an expression that will convert the value of the given expression to a value of the given type.
    /// If conversion is not possible an instance of DummyExpression is returned.
    /// </summary>
    //^ [Pure]
    protected virtual Expression Conversion(Expression expression, ITypeDefinition targetType, bool isExplicitConversion, ISourceLocation sourceLocation) {
      if (TypeHelper.TypesAreEquivalent(expression.Type, targetType)) return expression;
      CompileTimeConstant/*?*/ cconst = expression as CompileTimeConstant;
      if (cconst != null) {
        NullLiteral nullLiteral;
        ITypeDefinition tType = this.RemoveNullableWrapper(targetType);
        bool targetIsNullable = tType != targetType;
        if (tType.IsEnum && ExpressionHelper.IsIntegralZero(cconst)) {
          if (!targetIsNullable) return cconst.ConvertToTargetTypeIfIntegerInRangeOf(tType.UnderlyingType.ResolvedType, true);
          return this.AddNullableWrapperIfNeeded(expression, tType, targetType);
        } else if ((nullLiteral = cconst as NullLiteral) != null) {
          if (tType.IsReferenceType || targetIsNullable || tType is IPointerTypeReference)
            return new DefaultValue(nullLiteral, targetType);
        } else {
          ITypeDefinition tt = tType;
          if (tType.IsEnum && isExplicitConversion) {
            tt = tType.UnderlyingType.ResolvedType;
            if (TypeHelper.TypesAreEquivalent(expression.Type, tt)) return this.ConversionExpression(expression, targetType);
          }
          CompileTimeConstant convertedConst = cconst.ConvertToTargetTypeIfIntegerInRangeOf(tt, isExplicitConversion);
          if (convertedConst != cconst) {
            if (cconst.ValueIsPolymorphicCompileTimeConstant || this.ImplicitConversionExists(cconst.Type, targetType) || cconst.IntegerConversionIsLossless(targetType)) {
              if (tType.IsEnum) return this.ConversionExpression(expression, targetType, sourceLocation);
              //^ assume tType == this.RemoveNullableWrapper(targetType);
              if (!targetIsNullable) return convertedConst;
              return this.AddNullableWrapperIfNeeded(convertedConst, tType, targetType);
            }
          }
          //The constant cannot be converted to the target type without a loss of information
          if (expression.ContainingBlock.UseCheckedArithmetic && TypeHelper.IsPrimitiveInteger(tt))
            //TODO: this should be !expression.ContainingBlock.UseUncheckedArithmetic, the latter being true only if an explicit unchecked expression is encountered
            return new DummyExpression(expression.SourceLocation);
        }
      } else {
        object/*?*/ val = expression.Value;
        if (val != null) {
          if (!isExplicitConversion && targetType.IsEnum && TypeHelper.TypesAreEquivalent(expression.Type, targetType))
            isExplicitConversion = true;
          cconst = expression.GetAsConstant();
          return this.Conversion(cconst, targetType, isExplicitConversion, sourceLocation);
        }
      }
      if (expression.Type is Dummy) {
        if (targetType.IsDelegate) {
          AnonymousMethod/*?*/ anonMethod = expression as AnonymousMethod;
          if (anonMethod != null)
            return this.ConversionFromAnonymousMethodToDelegate(anonMethod, targetType);
          return this.ConversionFromMethodGroupToDelegate(expression, targetType);
        }
        IFunctionPointerTypeReference/*?*/ functionPointer = targetType as IFunctionPointerTypeReference;
        if (functionPointer != null) return this.ConversionFromMethodGroupToFunctionPointer(expression, functionPointer);
        return new DummyExpression(expression.SourceLocation);
      }
      return this.Conversion(expression, targetType, true, isExplicitConversion);
    }

    /// <summary>
    /// Returns an expression that will convert the value of the given expression to a value of the given type.
    /// If no implicit conversion exists an instance of DummyExpression is returned.
    /// </summary>
    /// <param name="expression">The expression that results in the value to convert.</param>
    /// <param name="targetType">The type to which the expression must be converted.</param>
    /// <param name="allowUserDefinedConversion">True if a user defined conversion may be used to do the conversion.</param>
    /// <param name="isExplicitConversion">True if the conversion has an explicit source representation.</param>
    //^ [Pure]
    protected virtual Expression Conversion(Expression expression, ITypeDefinition targetType, bool allowUserDefinedConversion, bool isExplicitConversion) {
      ITypeDefinition systemEnum = this.PlatformType.SystemEnum.ResolvedType;
      ITypeDefinition systemObject = this.PlatformType.SystemObject.ResolvedType;
      ITypeDefinition systemValueType = this.PlatformType.SystemValueType.ResolvedType;
      ITypeDefinition sourceType = expression.Type;
      if (TypeHelper.TypesAreEquivalent(sourceType, targetType)) return expression;
      ITypeDefinition sType = allowUserDefinedConversion ? this.RemoveNullableWrapper(sourceType) : sourceType;
      ITypeDefinition tType = allowUserDefinedConversion ? this.RemoveNullableWrapper(targetType) : targetType;
      bool sourceIsNullable = sType != sourceType;
      bool targetIsNullable = tType != targetType;
      if (sourceIsNullable) {
        //^ assume sType == this.RemoveNullableWrapper(sourceType);
        if (targetIsNullable) return this.LiftedConversionExpression(expression, sType, sourceType, tType, targetType, isExplicitConversion);
        if (sType.IsEnum && TypeHelper.TypesAreEquivalent(targetType, systemEnum))
          return this.ConversionExpression(this.ConversionExpression(expression, systemObject), tType);
        if (TypeHelper.TypesAreEquivalent(tType, systemValueType) || TypeHelper.TypesAreEquivalent(tType, systemObject) || (tType.IsInterface && TypeHelper.Type1ImplementsType2(sType, tType)))
          return this.ConversionExpression(this.ConversionExpression(expression, systemObject), tType);
        if (isExplicitConversion)
          return this.Conversion(this.UnboxedNullable(expression, sType, sourceType), tType, allowUserDefinedConversion, isExplicitConversion);
        return new DummyExpression(expression.SourceLocation); //Can't implicitly convert from a nullable type to anything but a base type or implemented interface
      } else if (sType.IsValueType) {
        if (sType.IsEnum && TypeHelper.TypesAreEquivalent(targetType, systemEnum)) return this.ConversionExpression(expression, tType);
        if (TypeHelper.TypesAreEquivalent(tType, systemValueType) || TypeHelper.TypesAreEquivalent(tType, systemObject) || (tType.IsInterface && TypeHelper.Type1ImplementsType2(sType, tType)))
          return this.ConversionExpression(expression, tType);
      }
      if (TypeHelper.Type1DerivesFromOrIsTheSameAsType2(sType, tType) || (targetType.IsInterface && TypeHelper.Type1ImplementsType2(sType, tType) || TypeHelper.Type1IsCovariantWithType2(sType, tType)))
        return this.AddNullableWrapperIfNeeded(expression, tType, targetType);
      if (this.ImplicitTypeParameterConversionExists(sType, tType) || (isExplicitConversion && this.ReferenceConversionMightBePossibleAtRuntime(sType, tType))) {
        expression = this.ConversionExpression(expression, tType); //This boxes the expression and (unless tType is known to be a reference type) unboxes it again as a value of type tType.
        //In the above case, the unboxing will be a no-op at runtime except when tType has been instantiated with the same concrete value type as sType.
        //^ assume tType == targetType || tType == this.RemoveNullableWrapper(targetType);
        return this.AddNullableWrapperIfNeeded(expression, tType, targetType);
      }
      if (TypeHelper.TypesAreEquivalent(targetType, systemObject)) return expression;
      if (this.NumericConversionWasPossible(ref expression, sType, tType, isExplicitConversion))
        return this.AddNullableWrapperIfNeeded(expression, tType, targetType);
      if ((sType.TypeCode == PrimitiveTypeCode.NotPrimitive || tType.TypeCode == PrimitiveTypeCode.NotPrimitive) && allowUserDefinedConversion) {
        Expression convertedExpression = this.UserDefinedConversion(expression, sType, tType, isExplicitConversion);
        //^ assume tType == targetType || tType == this.RemoveNullableWrapper(targetType);
        if (!(convertedExpression is DummyExpression)) {
          convertedExpression = this.Conversion(convertedExpression, tType, false, isExplicitConversion);
          //^ assume tType == targetType || tType == this.RemoveNullableWrapper(targetType);
          return this.AddNullableWrapperIfNeeded(convertedExpression, tType, targetType);
        }
      }
      IPointerTypeReference/*?*/ spType = sType as IPointerTypeReference;
      IPointerTypeReference/*?*/ tpType = tType as IPointerTypeReference;
      if (spType != null && tpType != null) {
        if (isExplicitConversion) return this.ConversionExpression(expression, tpType.ResolvedType);
        if (TypeHelper.TypesAreEquivalent(tpType.TargetType.ResolvedType, tpType.PlatformType.SystemVoid))
          return this.ConversionExpression(expression, tpType.ResolvedType);
      } else if (sType is IFunctionPointerTypeReference && tpType != null && TypeHelper.TypesAreEquivalent(tpType.TargetType.ResolvedType, tpType.PlatformType.SystemVoid)) {
        return this.ConversionExpression(expression, tpType.ResolvedType);
      } else if (spType != null && isExplicitConversion && TypeHelper.IsPrimitiveInteger(tType)) {
        return this.ConversionExpression(expression, tType);
      } else if (tpType != null && isExplicitConversion && TypeHelper.IsPrimitiveInteger(sType))
        return this.ConversionExpression(expression, tType);
      if (isExplicitConversion && TypeHelper.Type1DerivesFromOrIsTheSameAsType2(targetType, sourceType))
        return this.ConversionExpression(expression, targetType);
      return new DummyExpression(expression.SourceLocation);
    }

    /// <summary>
    /// Returns an instance of Conversion that converts the given expression to the given target type.
    /// This method should only be called if expression results in a type of value for which a well known built-in conversion
    /// exists. For example, if the expression results in a value of a reference type, then the resulting expression
    /// will get translated to the castclass IL instruction, whereas an expression that results in a value whose compile time type is
    /// a type parameter, or a value type, will get translated to a box IL instruction.
    /// </summary>
    //^ [Pure]
    protected Expression ConversionExpression(Expression expression, ITypeDefinition targetType) {
      return ConversionExpression(expression, targetType, expression.SourceLocation);
    }

    /// <summary>
    /// Returns an instance of Conversion that converts the given expression to the given target type.
    /// This method should only be called if expression results in a type of value for which a well known built-in conversion
    /// exists. For example, if the expression results in a value of a reference type, then the resulting expression
    /// will get translated to the castclass IL instruction, whereas an expression that results in a value whose compile time type is
    /// a type parameter, or a value type, will get translated to a box IL instruction.
    /// </summary>
    //^ [Pure]
    protected virtual Expression ConversionExpression(Expression expression, ITypeDefinition targetType, ISourceLocation sourceLocation) {
      return new Conversion(expression, targetType, sourceLocation);    
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="anonMethod"></param>
    /// <param name="targetType"></param>
    /// <returns></returns>
    protected virtual Expression ConversionFromAnonymousMethodToDelegate(AnonymousMethod anonMethod, ITypeDefinition targetType)
      //^ requires targetType.IsDelegate;
    {
      //TODO: check that signature matches.
      AnonymousDelegate result = new AnonymousDelegate(anonMethod, targetType);
      result.SetContainingExpression(anonMethod);
      return result;
    }

    /// <summary>
    /// Returns an expression that will result in a delegate of the given target type, using a method from the method group represented by the given expression.
    /// If the given expression does not resolve to a method group, or if the method group does not include a method that matches the delegate, then an
    /// instance of DummyExpression is returned.
    /// </summary>
    //^ [Pure]
    protected virtual Expression ConversionFromMethodGroupToDelegate(Expression expression, ITypeDefinition targetType)
      //^ requires targetType.IsDelegate;
    {
      bool applicableButNotConsistent;
      QualifiedName qualifiedName;
      IMethodDefinition matchingMethod = this.GetMatchingMethodFromMethodGroup(expression, targetType, true, out applicableButNotConsistent);
      Expression/*?*/ instance = null;
      if (!(matchingMethod is Dummy)) {
        if (!matchingMethod.IsStatic) {
          SimpleName/*?*/ simpleName = expression as SimpleName;
          if (simpleName != null)
            instance = new ThisReference(simpleName.ContainingBlock, simpleName.SourceLocation);
          else {
            qualifiedName = expression as QualifiedName;
            if (qualifiedName != null)
              instance = qualifiedName.Qualifier;
          }
        }
      }
      else if (!applicableButNotConsistent && (qualifiedName = expression as QualifiedName) != null) { // Look for extension method.
        List<IMethodDefinition> result = new List<IMethodDefinition>();
        SimpleName simpleName = qualifiedName.SimpleName;
        IMethodDefinition invokeMethod = this.GetInvokeMethod(targetType);
        IEnumerable<Expression> delegateArgs = 
          MakeExtensionArgumentList(qualifiedName, MakeFakeArgumentList(expression, invokeMethod));

        NamespaceDeclaration enclosingNamespace = expression.ContainingBlock.ContainingNamespaceDeclaration;
        enclosingNamespace.GetApplicableExtensionMethods(result, simpleName, delegateArgs);
        if (result.Count > 0)
          matchingMethod = this.ResolveOverload(result, delegateArgs, false);
        instance = qualifiedName.Qualifier;
      }
      if (matchingMethod is Dummy)
        return new DummyExpression(expression.SourceLocation);
      return new CreateDelegateInstance(instance, targetType, matchingMethod, expression.SourceLocation);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="expression"></param>
    /// <param name="functionPointer"></param>
    /// <returns></returns>
    protected virtual Expression ConversionFromMethodGroupToFunctionPointer(Expression expression, IFunctionPointerTypeReference functionPointer) { //TODO: pass in the source context of the conversion
      AddressOf/*?*/ addressOfExpression = expression as AddressOf;
      if (addressOfExpression != null) {
        IMethodDefinition/*?*/ method = addressOfExpression.Address.Definition as IMethodDefinition;
        if (method != null) { //TODO: check for match to functionPointer
          return new Conversion(addressOfExpression, functionPointer.ResolvedType, expression.SourceLocation);
        }
      }
      return new DummyExpression(expression.SourceLocation);
    }

    /// <summary>
    /// Returns a list of expressions that convert the given argument expressions to match the types of the given list of parameters.
    /// </summary>
    /// <param name="containingExpression">The expression that contains the argument list to be converted. Used when calling SetContainingExpression
    /// on expressions that are created for the purpose of the conversion.</param>
    /// <param name="arguments">A list of expressions that match parameters.</param>
    /// <param name="parameters">A list of parameters.</param>
    //^ [Pure]
    public List<Expression> ConvertArguments(Expression containingExpression, IEnumerable<Expression> arguments, IEnumerable<IParameterDefinition> parameters) {
      return this.ConvertArguments(containingExpression, arguments, parameters, false);
    }

    /// <summary>
    /// Returns a list of expressions that convert the given argument expressions to match the types of the given list of parameters.
    /// </summary>
    /// <param name="containingExpression">The expression that contains the argument list to be converted. Used when calling SetContainingExpression
    /// on expressions that are created for the purpose of the conversion.</param>
    /// <param name="arguments">A list of expressions that match parameters.</param>
    /// <param name="parameters">A list of parameters.</param>
    /// <param name="allowExtraArguments">If this is true, any extra arguments that do not match parameters are appended to the result without conversion.
    /// This is needed when the parameters belong to a method that accepts extra arguments.</param>
    //^ [Pure]
    public virtual List<Expression> ConvertArguments(Expression containingExpression, IEnumerable<Expression> arguments, IEnumerable<IParameterDefinition> parameters, bool allowExtraArguments) {
      List<Expression> convertedArgs = new List<Expression>();
      IEnumerator<Expression> args = arguments.GetEnumerator();
      IEnumerator<IParameterDefinition> pars = parameters.GetEnumerator();
      if (pars.MoveNext()) {
        for (; ; ) {
          Expression convertedArg;
          IParameterDefinition par = pars.Current;
          bool lastParameter = !pars.MoveNext();
          if (args.MoveNext()) {
            ITypeDefinition parType = par.Type.ResolvedType;
            Expression arg = args.Current;
            if (lastParameter && par.IsParameterArray) {
              if (!this.ImplicitConversionExists(arg, parType)) {
                convertedArg = this.GetParamArray(containingExpression, par, args); //an expression that collects the remaining arguments into a parameter array.
                convertedArgs.Add(convertedArg);
                break;
              }
            }
            convertedArg = this.ImplicitConversionInAssignmentContext(arg, parType);
            if (convertedArg is DummyExpression)
              this.ReportFailedArgumentConversion(arg, parType, par.Index);
          } else {
            //Still have parameters, but no more arguments have been specified.
            if (lastParameter && par.IsParameterArray) {
              convertedArg = this.GetParamArray(containingExpression, par, null); //an expression that results in an empty parameter array.
            } else
              convertedArg = this.GetDefaultValueFor(containingExpression, par);
          }
          convertedArgs.Add(convertedArg);
          if (lastParameter) {
            // if last parameter has been processed, then we need to see
            // if this method accepts additional arguments. If it is the case,
            // push all the arguments into the converted arguments list without
            // conversion.
            if (allowExtraArguments) {
              while (true) {
                if (args.MoveNext()) {
                  convertedArgs.Add(args.Current);
                } else {
                  break;
                }
              }
            }
            break;
          }
        }
      }
      convertedArgs.TrimExcess();
      return convertedArgs;
    }

    /// <summary>
    /// Creates an instance of a language specific object that formats type member signatures according to the syntax of the language.
    /// </summary>
    protected virtual SignatureFormatter CreateSignatureFormatter() {
      return new SignatureFormatter(this.TypeNameFormatter);
    }

    /// <summary>
    /// Creates an instance of a language specific object that formats type names according to the syntax of the language.
    /// </summary>
    protected virtual TypeNameFormatter CreateTypeNameFormatter() {
      return new TypeNameFormatter();
    }

    /// <summary>
    /// Returns an expression that will convert the value of the given expression to a value of the given type.
    /// If no explicit conversion exists, a DummyExpression is returned.
    /// </summary>
    //^ [Pure]
    public Expression ExplicitConversion(Expression expression, ITypeDefinition targetType) {
      return this.ExplicitConversion(expression, targetType, expression.SourceLocation);
    }

    /// <summary>
    /// Returns an expression that will convert the value of the given expression to a value of the given type.
    /// If no explicit conversion exists, a DummyExpression is returned.
    /// </summary>
    //^ [Pure]
    public virtual Expression ExplicitConversion(Expression expression, ITypeDefinition targetType, ISourceLocation sourceLocation) {
      return this.Conversion(expression, targetType, true, sourceLocation);
    }

    /// <summary>
    /// If the type specifies a default member name, return a list of members with that name. If not, return an empty list.
    /// </summary>
    /// <param name="type">The type whose default members are wanted.</param>
    //^ [Pure]
    public virtual IEnumerable<ITypeDefinitionMember> GetDefaultMembers(ITypeDefinition type) {
      //TODO: implement this for real
      IName memberName = Dummy.Name;
      if (TypeHelper.TypesAreEquivalent(type, this.PlatformType.SystemString)) memberName = this.NameTable.GetNameFor("Chars");
      // default to Item
      else if (TypeHelper.TypesAreEquivalent(type, this.PlatformType.SystemCollectionsGenericDictionary) || true) memberName = this.NameTable.GetNameFor("Item");
      var result = new List<ITypeDefinitionMember>();
      this.PopulateWithDefaultMembersIncludingInheritedMembers(type, memberName, result);
      return result;
    }

    /// <summary>
    /// Add all members named memberName to the given list, starting with the local members of the given type and then recursively
    /// doing the same with the base classes. Do not add a member if there already is one with the same signature.
    /// </summary>
    private void PopulateWithDefaultMembersIncludingInheritedMembers(ITypeDefinition type, IName memberName, List<ITypeDefinitionMember> result) {
      foreach (var member in type.GetMembersNamed(memberName, false)) {
        if (result.Exists((x) => MembersMatch(x, member))) continue;
        result.Add(member);
      }
      foreach (var baseClass in type.BaseClasses)
        this.PopulateWithDefaultMembersIncludingInheritedMembers(baseClass.ResolvedType, memberName, result);
    }

    /// <summary>
    /// Returns true if x and y are both properties with the same signature.
    /// </summary>
    private static bool MembersMatch(ITypeDefinitionMember x, ITypeDefinitionMember y) {
      IPropertyDefinition xp = x as IPropertyDefinition;
      IPropertyDefinition yp = y as IPropertyDefinition;
      if (xp != null && yp != null) return MemberHelper.SignaturesAreEqual(xp, yp);
      //TODO: more stuff
      return false;
    }

    /// <summary>
    /// Create an expression that corresponds to the address of the parameter
    /// </summary>
    public virtual AddressOf GetAddressOf(Expression expr, ISourceLocation sourceLocation) {
      return new AddressOf(new AddressableExpression(expr), sourceLocation);
    }

    /// <summary>
    /// If the type specifies a default member name, return a list of indexed properties with that name. If not, return an empty list.
    /// </summary>
    /// <param name="type">The type whose default indexed properties are wanted.</param>
    //^ [Pure]
    public virtual IEnumerable<IPropertyDefinition> GetDefaultIndexedProperties(ITypeDefinition type) {
      foreach (ITypeDefinitionMember member in this.GetDefaultMembers(type)) {
        IPropertyDefinition/*?*/ property = member as IPropertyDefinition;
        if (property != null && IteratorHelper.EnumerableIsNotEmpty(property.Parameters)) yield return property;
      }
    }

    /// <summary>
    /// If the type specifies a default member name, return a list of getter methods of any indexed properties with the default name. If not, return an empty list.
    /// </summary>
    /// <param name="type">The type whose default indexed property getters are wanted.</param>
    //^ [Pure]
    public virtual IEnumerable<IMethodDefinition> GetDefaultIndexedPropertyGetters(ITypeDefinition type) {
      foreach (IPropertyDefinition defaultIndexProperty in this.GetDefaultIndexedProperties(type)) {
        if (defaultIndexProperty.Getter != null) yield return defaultIndexProperty.Getter.ResolvedMethod;
      }
    }

    /// <summary>
    /// Returns a default value to match with the given parameter. If the parameter specifies a default value, use that.
    /// If not, return the default value for the type of the parameter.
    /// </summary>
    /// <param name="containingExpression">The expression that contains the argument list to be converted. Used when calling SetContainingExpression
    /// on expressions that are created for the purpose of the conversion.</param>
    /// <param name="par">The parameter for which a matching default value is desired.</param>
    //^ [Pure]
    public virtual Expression GetDefaultValueFor(Expression containingExpression, IParameterDefinition par) {
      Expression result;
      if (par.HasDefaultValue)
        result = new CompileTimeConstant(par.DefaultValue.Value, SourceDummy.SourceLocation);
      else
        result = new DefaultValue(TypeExpression.For(par.Type), SourceDummy.SourceLocation);
      result.SetContainingExpression(containingExpression);
      return result;
    }

    /// <summary>
    /// Returns any members of the given name that may be declared in another type, but that are to be found in the given
    /// type during lookup. For example, extension methods in C#.
    /// </summary>
    public virtual IEnumerable<ITypeDefinitionMember> GetExtensionMembers(ITypeDefinition type, IName memberName, bool ignoreCase) {
      ITypeContract/*?*/ contract = this.Compilation.ContractProvider.GetTypeContractFor(type);
      if (contract != null) {
        foreach (IFieldDefinition contractField in contract.ContractFields) {
          if (ignoreCase) {
            if (contractField.Name.UniqueKeyIgnoringCase == memberName.UniqueKeyIgnoringCase) yield return contractField;
          } else {
            if (contractField.Name.UniqueKey == memberName.UniqueKey) yield return contractField;
          }
        }
      }
    }

    /// <summary>
    /// Gets the Invoke method from the delegate. Returns Dummy.Method if the delegate type is malformed.
    /// </summary>
    //^ [Pure]
    public virtual IMethodDefinition GetInvokeMethod(ITypeDefinition delegateType)
      //^ requires delegateType.IsDelegate;
    {
      foreach (ITypeDefinitionMember member in delegateType.GetMembersNamed(this.NameTable.Invoke, false)) {
        IMethodDefinition/*?*/ method = member as IMethodDefinition;
        if (method != null) return method;
      }
      return Dummy.Method; //Should get here only when the delegate type is obtained from a malformed or malicious referenced assembly.
    }

    /// <summary>
    /// Returns the method from the method group represented by the given expression, which is compatible the given delegate type.
    /// Returns Dummy.Method if not such method can be found.
    /// </summary>
    //^ [Pure]
    private IMethodDefinition GetMatchingMethodFromMethodGroup(Expression expression, ITypeDefinition targetType, bool requireConsistency)
    {
      bool dummy;
      return this.GetMatchingMethodFromMethodGroup(expression, targetType, requireConsistency, out dummy);
    }

    private IMethodDefinition GetMatchingMethodFromMethodGroup
      (Expression expression, ITypeDefinition targetType, bool requireConsistency, out bool applicableButInconsistent)
    {
      applicableButInconsistent = false;
      GenericInstanceExpression/*?*/ genericInstance = expression as GenericInstanceExpression;
      IMethodDefinition/*?*/ methodGroupRepresentative = this.ResolveIfName(expression) as IMethodDefinition;
      if (methodGroupRepresentative == null) {
        //Check if expression binds to a group of generic methods
        if (genericInstance != null)
          methodGroupRepresentative = this.ResolveIfName(genericInstance.GenericTypeOrMethod) as IMethodDefinition;
      }
      if (methodGroupRepresentative == null) return Dummy.Method;
      //^ assume targetType.IsDelegate;
      IMethodDefinition invokeMethod = this.GetInvokeMethod(targetType);
      List<Expression> fakeArguments = MakeFakeArgumentList(expression, invokeMethod);
      IEnumerable<IMethodDefinition> candidates;
      if (genericInstance == null)
        candidates = this.GetMethodGroupMethods(methodGroupRepresentative, IteratorHelper.EnumerableCount(invokeMethod.Parameters), false, null);
      else
        candidates = this.GetMethodGroupMethods(methodGroupRepresentative, IteratorHelper.EnumerableCount(invokeMethod.Parameters), false, genericInstance.GetArgumentTypeReferences());
      IMethodDefinition matchingMethod = this.ResolveOverload(candidates, fakeArguments);
      // There is an applicable method, but is not consistent with delegate invocation semantics.
      // The caller needs to know this, since no extension method is searched for in this case.
      if (!(matchingMethod is Dummy) && requireConsistency && !this.SignaturesAreConsistent(invokeMethod, matchingMethod)) {
        matchingMethod = Dummy.Method;
        applicableButInconsistent = true;
      }
      return matchingMethod;
    }

    private static List<Expression> MakeFakeArgumentList(Expression expression, IMethodDefinition invokeMethod) {
      List<Expression> fakeArguments = new List<Expression>();
      foreach (IParameterDefinition delegateParam in invokeMethod.Parameters)
        fakeArguments.Add(new DummyExpression(expression.ContainingBlock, SourceDummy.SourceLocation, delegateParam.Type.ResolvedType));
      return fakeArguments;
    }

    /// <summary>
    /// Returns dummy argument list for a static call of an extension
    /// method equivalent to the receiver + original argument list in
    /// the "dispatched-call" method call syntax.
    /// </summary>
    internal static IEnumerable<Expression> MakeExtensionArgumentList(QualifiedName callExpression, IEnumerable<Expression> originalArguments) {
      yield return callExpression.Qualifier;
      foreach (Expression argument in originalArguments)
        yield return argument;
    }


    /// <summary>
    /// Returns the collection of methods with the same name as the given method and declared by the same type as the given method (or by a base type)
    /// and that might be called with the given number of arguments.
    /// </summary>
    //TODO: pass in an expression to provide a context for determining visibility
    //^ [Pure]
    public virtual IEnumerable<IMethodDefinition> GetMethodGroupMethods(IMethodDefinition methodGroupRepresentative, uint argumentCount, bool argumentListIsIncomplete) {
      return this.GetMethodGroupMethods(methodGroupRepresentative, argumentCount, argumentListIsIncomplete, null);
    }

    /// <summary>
    /// Returns the collection of methods with the same name as the given method and declared by the same type as the given method (or by a base type)
    /// and that might be called with the given number of arguments.
    /// </summary>
    //TODO: pass in an expression to provide a context for determining visibility
    //^ [Pure]
    public virtual IEnumerable<IMethodDefinition> GetMethodGroupMethods(IMethodDefinition methodGroupRepresentative, uint argumentCount, bool argumentListIsIncomplete, IEnumerable<ITypeReference>/*?*/ genericArguments) {
      //TODO: need to instantiate generic methods
      ITypeDefinition type = methodGroupRepresentative.ContainingTypeDefinition;
      IName methodName = methodGroupRepresentative.Name;
      return this.GetMethodGroupMethods(type, methodName, argumentCount, argumentListIsIncomplete, genericArguments);
    }

    /// <summary>
    /// Returns the collection of methods with the given name and declared by the given type (or by a base type)
    /// and that might be called with the given number of arguments.
    /// </summary>
    //^ [Pure]
    private IEnumerable<IMethodDefinition> GetMethodGroupMethods(ITypeDefinition type, IName methodName, uint argumentCount, bool argumentListIsIncomplete, IEnumerable<ITypeReference>/*?*/ genericArguments) {
      uint genericArgumentCount = IteratorHelper.EnumerableCount(genericArguments);
      foreach (ITypeDefinitionMember member in type.GetMembersNamed(methodName, false)) {
        IMethodDefinition/*?*/ method = member as IMethodDefinition;
        if (method == null) continue; //Cannot happen with classes defined in C#
        uint methodParameterCount = IteratorHelper.EnumerableCount(method.Parameters);
        if (methodParameterCount != argumentCount && (methodParameterCount < argumentCount || !argumentListIsIncomplete) &&
          !this.MethodQualifiesEvenIfArgumentNumberMismatches(method, methodParameterCount, argumentCount)) continue;
        //TODO: ignore method if it is not visible (needs an extra parameter)
        if (genericArgumentCount == 0)
          yield return method;
        else if (method.GenericParameterCount != genericArgumentCount)
          continue;
        else
          yield return new GenericMethodInstance(method, genericArguments, this.Compilation.HostEnvironment.InternFactory);
      }
      foreach (ITypeReference baseTypeRef in type.BaseClasses) {
        foreach (IMethodDefinition baseClassMethod in this.GetMethodGroupMethods(baseTypeRef.ResolvedType, methodName, argumentCount, argumentListIsIncomplete, genericArguments))
          yield return baseClassMethod;
      }
    }

    /// <summary>
    /// Returns a language specific string that corresponds to the given method definition and that conforms to the specified formatting options.
    /// </summary>
    //^ [Pure]
    public virtual string GetMethodSignature(IMethodReference method, NameFormattingOptions formattingOptions) {
      return this.SignatureFormatter.GetMethodSignature(method, formattingOptions);
    }

    /// <summary>
    /// Returns an expression that allocates a parameter array instance to match given parameter and that contains the given arguments as elements.
    /// </summary>
    /// <param name="containingExpression">The expression that contains the argument list to be converted. Used when calling SetContainingExpression
    /// on expressions that are created for the purpose of the conversion.</param>
    /// <param name="par">The parameter for which a matching parameter array instance is desired. parameter.IsParameterArray must be true.</param>
    /// <param name="args">The argument values, if any, that are to be the elements of the paraemter array instance. May be null.</param>
    //^ [Pure]
    public virtual Expression GetParamArray(Expression containingExpression, IParameterDefinition par, IEnumerator<Expression>/*?*/ args)
      //^ requires par.IsParameterArray;
    {
      SourceLocationBuilder slb;
      List<Expression> initializers = new List<Expression>();
      if (args != null) {
        slb = new SourceLocationBuilder(args.Current.SourceLocation);
        do {
          initializers.Add(this.ImplicitConversionInAssignmentContext(args.Current, par.ParamArrayElementType.ResolvedType));
          slb.UpdateToSpan(args.Current.SourceLocation);
        } while (args.MoveNext());
      } else {
        slb = new SourceLocationBuilder(SourceDummy.SourceLocation);
      }
      //^ assume par.IsParameterArray; //see precondition
      var result = new CreateArray(par.ParamArrayElementType, initializers, slb);
      result.SetContainingExpression(containingExpression);
      return result;
    }

    /// <summary>
    /// Return the type that the pointer type points to
    /// </summary>
    public virtual ITypeReference GetPointerTargetType(ITypeDefinition type) {
      IPointerTypeReference ptrType = type as IPointerTypeReference;
      if (ptrType != null)
        return ptrType.TargetType;
      else return null;
    }

    /// <summary>
    /// Returns a singleton collection of locations that either provides the source location of the given
    /// type definition member, or provides information about the name and assembly of the containing type
    /// of the given member.
    /// </summary>
    /// <param name="member">A type member that is the subject of an error or warning.</param>
    public virtual IEnumerable<ILocation> GetRelatedLocations(ITypeDefinitionMember member) {
      var tdmem = member as TypeDefinitionMember;
      if (tdmem != null) {
        return IteratorHelper.GetSingletonEnumerable<ILocation>(tdmem.Declaration.Name.SourceLocation);
      }
      //TODO: return a location that refers to the assembly name of the referenced member.
      return Enumerable<ILocation>.Empty; ;
    }

    /// <summary>
    /// Returns a language specific string that corresponds to a source expression that would bind to the given type definition when appearing in an appropriate context.
    /// </summary>
    //^ [Pure]
    public virtual string GetTypeName(ITypeDefinition typeDefinition) {
      return this.GetTypeName(typeDefinition, NameFormattingOptions.ContractNullable|NameFormattingOptions.UseTypeKeywords);
    }

    /// <summary>
    /// Returns a language specific string that corresponds to a source expression that would evaluate to the given type definition when appearing in an appropriate context.
    /// </summary>
    //^ [Pure]
    public virtual string GetTypeName(ITypeDefinition typeDefinition, NameFormattingOptions formattingOptions) {
      return this.TypeNameFormatter.GetTypeName(typeDefinition, formattingOptions);
    }

    /// <summary>
    /// Returns an expression that will convert the value of the given expression to a value of the given type.
    /// If conversion is not possible or has to be explicit an instance of DummyExpression is returned.
    /// </summary>
    //^ [Pure]
    public virtual Expression ImplicitConversion(Expression expression, ITypeDefinition targetType) {
      return this.Conversion(expression, targetType, false, expression.SourceLocation);
    }

    /// <summary>
    /// Returns an expression that will convert the value of the given expression to a value of the given type
    /// when the conversion happens when assigning the expression to a variable of the target type. This is different
    /// than the general ImplicitConversion, which can also happen when testing method matching, which
    /// affect the selection of operators. 
    /// </summary>
    /// <returns></returns>
    public virtual Expression ImplicitConversionInAssignmentContext(Expression expression, ITypeDefinition targetType) {
      return this.ImplicitConversion(expression, targetType);
    }

    /// <summary>
    /// Returns true if an implicit conversion is available to convert the value of the given expression to a corresponding value of the given target type.
    /// </summary>
    //^ [Pure]
    public virtual bool ImplicitConversionExists(Expression expression, ITypeDefinition targetType) {
      CompileTimeConstant/*?*/ cconst = expression as CompileTimeConstant;
      if (cconst != null) {
        ITypeDefinition tType = this.RemoveNullableWrapper(targetType);
        bool targetIsNullable = tType != targetType;
        if (tType.IsEnum && ExpressionHelper.IsIntegralZero(cconst)) return true;
        if (cconst.ValueIsPolymorphicCompileTimeConstant) {
          if (ExpressionHelper.IsIntegerInRangeOf(cconst, tType)) return true;
        }
        if (cconst.CouldBeInterpretedAsNegativeSignedInteger && !TypeHelper.IsUnsignedPrimitiveInteger(targetType)) {
          if (cconst.ConvertToTargetTypeIfIntegerInRangeOf(targetType, false) != cconst) return true;
        } else if (cconst.CouldBeInterpretedAsUnsignedInteger && TypeHelper.IsUnsignedPrimitiveInteger(targetType)) {
          if (cconst.ConvertToTargetTypeIfIntegerInRangeOf(targetType, true) != cconst) return true;
        }
        if (cconst.Value == null && (tType.IsReferenceType || targetIsNullable || tType is IPointerTypeReference)) return true;
      } else {
        Parenthesis/*?*/ paren = expression as Parenthesis;
        if (paren != null) return this.ImplicitConversionExists(paren.ParenthesizedExpression, targetType);
        object/*?*/ val = expression.Value;
        if (val != null) {
          if (targetType.IsEnum && TypeHelper.TypesAreEquivalent(expression.Type, targetType)) return true;
          if (expression.Type.IsEnum && !this.ImplicitEnumToIntegerConversionIsAllowed) return false;
          cconst = expression.GetAsConstant();
          return this.ImplicitConversionExists(cconst, targetType);
        }
      }
      if (targetType.IsDelegate) {
        AnonymousMethod/*?*/ anonMeth = expression as AnonymousMethod;
        if (anonMeth != null) return this.ImplicitConversionFromAnonymousMethodExists(anonMeth, targetType);
      }
      //TODO: lambdas
      ITypeDefinition expressionType = expression.Type;
      if (expressionType is Dummy) {
        if (targetType.IsDelegate) return this.ImplicitConversionFromMethodGroupExists(expression, targetType);
        return false;
      }
      return this.ImplicitConversionExists(expressionType, targetType);
    }

    /// <summary>
    /// Returns true if a value of an enumeration type can be implicitly converted to an integer type.
    /// </summary>
    protected virtual bool ImplicitEnumToIntegerConversionIsAllowed {
      get { return false; }
    }

    /// <summary>
    /// Returns true if the given anonymous method expression can be implicitly converted to the given target type.
    /// </summary>
    public virtual bool ImplicitConversionFromAnonymousMethodExists(AnonymousMethod anonMeth, ITypeDefinition targetType)
      //^ requires targetType.IsDelegate;
    {
      return true; //TODO: implement this for real
    }

    /// <summary>
    /// Returns true if an implicit conversion is available to convert a value of the given source type to a corresponding value of the given target type.
    /// </summary>
    //^ [Pure]
    public virtual bool ImplicitConversionExists(ITypeDefinition sourceType, ITypeDefinition targetType) {
      if (TypeHelper.TypesAreEquivalent(targetType, this.PlatformType.SystemObject)) return true;
      bool result = false;
      ITypeDefinition sType = this.RemoveNullableWrapper(sourceType);
      ITypeDefinition tType = this.RemoveNullableWrapper(targetType);
      bool sourceIsNullable = sType != sourceType;
      bool targetIsNullable = tType != targetType;
      if (sourceIsNullable && !targetIsNullable) {
        if (sourceType.IsEnum && TypeHelper.TypesAreEquivalent(targetType, this.PlatformType.SystemEnum))
          result = true;
        else
          result = TypeHelper.TypesAreEquivalent(targetType, this.PlatformType.SystemValueType);
      } else {
        if (this.ImplicitStandardConversionExists(sType, tType))
          result = true;
        else
          if (sType.TypeCode == PrimitiveTypeCode.NotPrimitive || tType.TypeCode == PrimitiveTypeCode.NotPrimitive) {
            if (this.ImplicitUserDefinedConversionExists(sType, tType)) result = true;
          }
      }
      return result;
    }

    /// <summary>
    /// Returns true if argument has a better implicit conversion to par1type than it has to par2Type.
    /// </summary>
    //^ [Pure]
    protected virtual bool ImplicitConversionFromArgumentToType1isBetterThanImplicitConversionToType2(Expression argument, ITypeDefinition par1Type, ITypeDefinition par2Type) {
      if (TypeHelper.TypesAreEquivalent(par1Type, par2Type)) return false;
      if (!this.ImplicitConversionExists(argument, par1Type)) return false;
      if (!this.ImplicitConversionExists(argument, par2Type)) return true;
      bool t1tot2 = this.ImplicitConversionExists(par1Type, par2Type);
      bool t2tot1 = this.ImplicitConversionExists(par2Type, par1Type);
      if (t1tot2 && !t2tot1) return true;
      if (!t1tot2 && t2tot1) return false;
      PrimitiveTypeCode t1code = par1Type.TypeCode;
      PrimitiveTypeCode t2code = par2Type.TypeCode;
      if (t1code == PrimitiveTypeCode.Int8 && (t2code == PrimitiveTypeCode.UInt8 || t2code == PrimitiveTypeCode.UInt16 || t2code == PrimitiveTypeCode.UInt32 || t2code == PrimitiveTypeCode.UInt64))
        return true;
      if (t2code == PrimitiveTypeCode.Int8 && (t1code == PrimitiveTypeCode.UInt8 || t1code == PrimitiveTypeCode.UInt16 || t1code == PrimitiveTypeCode.UInt32 || t1code == PrimitiveTypeCode.UInt64))
        return false;
      if (t1code == PrimitiveTypeCode.Int16 && (t2code == PrimitiveTypeCode.UInt16 || t2code == PrimitiveTypeCode.UInt32 || t2code == PrimitiveTypeCode.UInt64))
        return true;
      if (t2code == PrimitiveTypeCode.Int16 && (t1code == PrimitiveTypeCode.UInt16 || t1code == PrimitiveTypeCode.UInt32 || t1code == PrimitiveTypeCode.UInt64))
        return false;
      if (t1code == PrimitiveTypeCode.Int32 && (t2code == PrimitiveTypeCode.UInt32 || t2code == PrimitiveTypeCode.UInt64))
        return true;
      if (t2code == PrimitiveTypeCode.Int32 && (t1code == PrimitiveTypeCode.UInt32 || t1code == PrimitiveTypeCode.UInt64))
        return false;
      if (t1code == PrimitiveTypeCode.Int64 && (t2code == PrimitiveTypeCode.UInt64))
        return true;
      if (t1code == PrimitiveTypeCode.Pointer)
        return argument.Type is IPointerTypeReference;
      if (t2code == PrimitiveTypeCode.Pointer)
        return !(argument.Type is IPointerTypeReference);
      return false;
    }

    /// <summary>
    /// Returns true if the given expression represents a method group that contains a method that is compatible with the given delegate type.
    /// </summary>
    //^ [Pure]
    private bool ImplicitConversionFromMethodGroupExists(Expression expression, ITypeDefinition targetType)
      //^ requires targetType.IsDelegate;
    {
      return !(this.GetMatchingMethodFromMethodGroup(expression, targetType, false) is Dummy);
    }

    /// <summary>
    /// Returns true if an implicit numeric conversion from the given source type to the given target type.
    /// </summary>
    //^ [Pure]
    protected virtual bool ImplicitNumericConversionExists(ITypeDefinition sourceType, ITypeDefinition targetType) {
      switch (sourceType.TypeCode) {
        case PrimitiveTypeCode.Float32:
          return targetType.TypeCode == PrimitiveTypeCode.Float64 || targetType.TypeCode == PrimitiveTypeCode.Float32;
        case PrimitiveTypeCode.Float64:
          return targetType.TypeCode == PrimitiveTypeCode.Float64;
        case PrimitiveTypeCode.Int16:
          // From short to int, long, float, double, or decimal.
          switch (targetType.TypeCode) {
            case PrimitiveTypeCode.Int16:
            case PrimitiveTypeCode.Int32:
            case PrimitiveTypeCode.Int64:
            case PrimitiveTypeCode.Float32:
            case PrimitiveTypeCode.Float64: return true;
            default: return TypeHelper.TypesAreEquivalent(targetType, this.PlatformType.SystemDecimal);
          }
        case PrimitiveTypeCode.Int32:
          // From int to long, float, double, or decimal.
          switch (targetType.TypeCode) {
            case PrimitiveTypeCode.Int32:
            case PrimitiveTypeCode.Int64:
            case PrimitiveTypeCode.Float32:
            case PrimitiveTypeCode.Float64: return true;
            default: return TypeHelper.TypesAreEquivalent(targetType, this.PlatformType.SystemDecimal);
          }
        case PrimitiveTypeCode.Int64:
          // From long to float, double, or decimal.
          switch (targetType.TypeCode) {
            case PrimitiveTypeCode.Int64:
            case PrimitiveTypeCode.Float32:
            case PrimitiveTypeCode.Float64: return true;
            default: return TypeHelper.TypesAreEquivalent(targetType, this.PlatformType.SystemDecimal);
          }
        case PrimitiveTypeCode.Int8:
          // From sbyte to short, int, long, float, double, or decimal.
          switch (targetType.TypeCode) {
            case PrimitiveTypeCode.Int8:
            case PrimitiveTypeCode.Int16:
            case PrimitiveTypeCode.Int32:
            case PrimitiveTypeCode.Int64:
            case PrimitiveTypeCode.Float32:
            case PrimitiveTypeCode.Float64: return true;
            default: return TypeHelper.TypesAreEquivalent(targetType, this.PlatformType.SystemDecimal);
          }
        case PrimitiveTypeCode.UInt16:
          // From ushort to int, uint, long, ulong, float, double, or decimal.
          switch (targetType.TypeCode) {
            case PrimitiveTypeCode.UInt16:
            case PrimitiveTypeCode.Int32:
            case PrimitiveTypeCode.UInt32:
            case PrimitiveTypeCode.Int64:
            case PrimitiveTypeCode.UInt64:
            case PrimitiveTypeCode.Float32:
            case PrimitiveTypeCode.Float64: return true;
            default: return TypeHelper.TypesAreEquivalent(targetType, this.PlatformType.SystemDecimal);
          }
        case PrimitiveTypeCode.UInt32:
          // From uint to long, ulong, float, double, or decimal.
          switch (targetType.TypeCode) {
            case PrimitiveTypeCode.UInt32:
            case PrimitiveTypeCode.Int64:
            case PrimitiveTypeCode.UInt64:
            case PrimitiveTypeCode.Float32:
            case PrimitiveTypeCode.Float64: return true;
            default: return TypeHelper.TypesAreEquivalent(targetType, this.PlatformType.SystemDecimal);
          }
        case PrimitiveTypeCode.UInt64:
          // From ulong to float, double, or decimal.
          switch (targetType.TypeCode) {
            case PrimitiveTypeCode.UInt64:
            case PrimitiveTypeCode.Float32:
            case PrimitiveTypeCode.Float64: return true;
            default: return TypeHelper.TypesAreEquivalent(targetType, this.PlatformType.SystemDecimal);
          }
        case PrimitiveTypeCode.UInt8:
          // From byte to short, ushort, int, uint, long, ulong, float, double, or decimal.
          switch (targetType.TypeCode) {
            case PrimitiveTypeCode.UInt8:
            case PrimitiveTypeCode.Int16:
            case PrimitiveTypeCode.UInt16:
            case PrimitiveTypeCode.Int32:
            case PrimitiveTypeCode.UInt32:
            case PrimitiveTypeCode.Int64:
            case PrimitiveTypeCode.UInt64:
            case PrimitiveTypeCode.Float32:
            case PrimitiveTypeCode.Float64: return true;
            default: return TypeHelper.TypesAreEquivalent(targetType, this.PlatformType.SystemDecimal);
          }
        default:
          if (TypeHelper.TypesAreEquivalent(sourceType, this.PlatformType.SystemChar)) goto case PrimitiveTypeCode.UInt16;
          return false;
      }
    }

    /// <summary>
    /// Returns true if a standard (mostly CLR supplied) implicit conversion is available to convert a value of the given source type to a corresponding value of the given target type.
    /// </summary>
    //^ [Pure]
    protected virtual bool ImplicitStandardConversionExists(ITypeDefinition sourceType, ITypeDefinition targetType) {
      if (TypeHelper.TypesAreEquivalent(sourceType, targetType)) return true;
      if (this.ImplicitNumericConversionExists(sourceType, targetType)) return true;
      return this.TypesAreClrAssignmentCompatible(sourceType, targetType);
    }

    /// <summary>
    /// Returns true if the source type is a generic parameter with a constraint that requires its runtime value to be of a
    /// type that derives from the given target type or implements the given target type.
    /// </summary>
    //^ [Pure]
    protected virtual bool ImplicitTypeParameterConversionExists(ITypeDefinition sourceType, ITypeDefinition targetType) {
      IGenericParameter/*?*/ genericParameter = sourceType as IGenericParameter;
      if (genericParameter != null) {
        if (genericParameter == targetType) return true;
        foreach (ITypeReference constraint in genericParameter.Constraints) {
          ITypeDefinition constraintType = constraint.ResolvedType;
          if (constraintType is IGenericParameter) {
            if (this.ImplicitTypeParameterConversionExists(constraintType, targetType)) return true;
          } else {
            if (TypeHelper.Type1DerivesFromOrIsTheSameAsType2(constraintType, targetType)) return true;
            if (targetType.IsInterface && TypeHelper.Type1ImplementsType2(constraintType, targetType)) return true;
          }
        }
      }
      return false;
    }

    /// <summary>
    /// Returns true if the source type or target type defines a conversion operator that can be used to convert a value
    /// of the source type to a value of the target type.
    /// </summary>
    //^ [Pure]
    protected virtual bool ImplicitUserDefinedConversionExists(ITypeDefinition sourceType, ITypeDefinition targetType) {
      return this.ImplicitUserDefinedConversion(sourceType, targetType) != null;
    }

    /// <summary>
    /// Returns the most specific user defined implicit conversion operator that can be used to convert a value
    /// of the source type to a value of the target type.
    /// </summary>
    //^ [Pure]
    protected virtual IMethodDefinition/*?*/ ImplicitUserDefinedConversion(ITypeDefinition sourceType, ITypeDefinition targetType) {
      return this.UserDefinedConversion(sourceType, targetType, this.NameTable.OpImplicit);
    }

    /// <summary>
    /// Try to unify the given argument type with the given parameter type by replacing any occurrences of type parameters in parameterType with corresponding type
    /// arguments obtained from inferredTypeArgumentsFor. Returns true if unification fails. Updates inferredTypeArgumentsFor with any new inferences made during
    /// a successful unification.
    /// </summary>
    //^ [Pure]
    public virtual bool InferTypesAndReturnTrueIfInferenceFails(Dictionary<IGenericMethodParameter, ITypeDefinition> inferredTypeArgumentFor, ITypeDefinition argumentType, ITypeDefinition parameterType) {
      if (argumentType is Dummy)
        return true; //Error situation: the argument is not valid, or else it would have a real type.

      if (parameterType is IGenericMethodParameter) {
        ITypeDefinition/*?*/ previouslyInferredType = null;
        if (inferredTypeArgumentFor.TryGetValue((IGenericMethodParameter)parameterType, out previouslyInferredType)) {
          //^ assume previouslyInferredType != null;
          if (!TypeHelper.TypesAreEquivalent(previouslyInferredType, argumentType)) return true;
        } else {
          inferredTypeArgumentFor[(IGenericMethodParameter)parameterType] = argumentType;
        }
        return false;
      }

      IArrayTypeReference/*?*/ paType = parameterType as IArrayTypeReference;
      if (paType != null) {
        IArrayTypeReference/*?*/ aaType = argumentType as IArrayTypeReference;
        if (aaType == null) return true; //Type inference fails
        if (paType.Rank != aaType.Rank) {
          return true; //Type inference fails
        } else {
          return this.InferTypesAndReturnTrueIfInferenceFails(inferredTypeArgumentFor, aaType.ElementType.ResolvedType, paType.ElementType.ResolvedType);
        }
      }

      IGenericTypeInstanceReference/*?*/ piType = parameterType as IGenericTypeInstanceReference;
      if (piType != null) {
        IGenericTypeInstanceReference/*?*/ aiType = argumentType as IGenericTypeInstanceReference;
        if (aiType == null) {
          IArrayTypeReference/*?*/ aaType = argumentType as IArrayTypeReference;
          if (aaType != null) {
            foreach (ITypeReference interfaceRef in argumentType.Interfaces) {
              //check if the interface matches parameterType and if any references to generic method parameters occuring in parameterType can be inferred from the interface structure.
              if (!this.InferTypesAndReturnTrueIfInferenceFails(inferredTypeArgumentFor, interfaceRef.ResolvedType, parameterType)) return false;  //Type inference succeeds
            }
          }
          return true; //Type inference fails
        }
        if (TypeHelper.TypesAreEquivalent(aiType.GenericType.ResolvedType, piType.GenericType.ResolvedType)) {
          IEnumerator<ITypeReference> aiArgs = aiType.GenericArguments.GetEnumerator();
          IEnumerator<ITypeReference> piArgs = piType.GenericArguments.GetEnumerator();
          while (aiArgs.MoveNext() && piArgs.MoveNext()) {
            if (this.InferTypesAndReturnTrueIfInferenceFails(inferredTypeArgumentFor, aiArgs.Current.ResolvedType, piArgs.Current.ResolvedType)) return true; //Type inference fails
          }
          return false; //Type inference succeeds
        } else {
          foreach (ITypeReference interfaceRef in argumentType.Interfaces) {
            //check if the interface matches parameterType and if any references to generic method parameters occuring in parameterType can be inferred from the interface structure.
            if (!this.InferTypesAndReturnTrueIfInferenceFails(inferredTypeArgumentFor, interfaceRef.ResolvedType, parameterType)) return false;  //Type inference succeeds
          }
          foreach (ITypeReference baseClassRef in argumentType.BaseClasses) {
            return this.InferTypesAndReturnTrueIfInferenceFails(inferredTypeArgumentFor, baseClassRef.ResolvedType, parameterType);
          }
          return true; //Type inference fails
        }
      }
      return true; //Type inference fails
    }

    /// <summary>
    /// Returns true if type1 is "more specific" than type2. More specific means that type1 is less parameterized. Specifically,
    /// a concrete type is more specific than a type parameter. A constructed type is more specific than another constructed type
    /// if its type arguments are all at least as specific as the type arguments of the other type and one of the arguments is more specific.
    /// </summary>
    //^ [Pure]
    protected virtual bool IsMoreSpecific(ITypeDefinition type1, ITypeDefinition type2) {
      if (type2 is IGenericParameter) return !(type1 is IGenericParameter);
      IArrayTypeReference/*?*/ arrayType1 = type1 as IArrayTypeReference;
      if (arrayType1 != null) {
        IArrayTypeReference/*?*/ arrayType2 = type2 as IArrayTypeReference;
        if (arrayType2 != null && arrayType1.Rank == arrayType2.Rank)
          return this.IsMoreSpecific(arrayType1.ElementType.ResolvedType, arrayType2.ElementType.ResolvedType);
        return false;
      }
      IGenericTypeInstanceReference/*?*/ genericInstance1 = type1 as IGenericTypeInstanceReference;
      if (genericInstance1 != null) {
        IGenericTypeInstanceReference/*?*/ genericInstance2 = type2 as IGenericTypeInstanceReference;
        if (genericInstance2 == null) return false;
        if (genericInstance1.ResolvedType.GenericParameterCount != genericInstance2.ResolvedType.GenericParameterCount) return false;
        bool result = false;
        IEnumerator<ITypeReference> genericArguments2enumerator = genericInstance2.GenericArguments.GetEnumerator();
        foreach (ITypeReference genericArgument1 in genericInstance1.GenericArguments) {
          if (!genericArguments2enumerator.MoveNext()) {
            //^ assume false; //Should only get here if GenericParameterCount lied.
            return false;
          }
          ITypeReference genericArgument2 = genericArguments2enumerator.Current;
          if (this.IsMoreSpecific(genericArgument2.ResolvedType, genericArgument1.ResolvedType)) return false;
          if (this.IsMoreSpecific(genericArgument1.ResolvedType, genericArgument2.ResolvedType))
            result = true;
        }
        return result;
      }
      return false;
    }

    /// <summary>
    /// Returns true if the given type is to be treated as a pointer type
    /// </summary>
    public virtual bool IsPointerType(ITypeDefinition type) {
      return type is IPointerTypeReference;
    }

    /// <summary>
    /// Returns true if the given type is one of the following: sbyte, byte, short, ushort, int, uint, long, ulong, char, string or an enum type
    /// </summary>
    //^ [Pure]
    public virtual bool IsSwitchableType(ITypeDefinition type) {
      switch (type.TypeCode) {
        case PrimitiveTypeCode.Char:
        case PrimitiveTypeCode.Int16:
        case PrimitiveTypeCode.Int32:
        case PrimitiveTypeCode.Int64:
        case PrimitiveTypeCode.Int8:
        case PrimitiveTypeCode.UInt16:
        case PrimitiveTypeCode.UInt32:
        case PrimitiveTypeCode.UInt64:
        case PrimitiveTypeCode.UInt8:
          return true;
        case PrimitiveTypeCode.NotPrimitive:
          return type.IsEnum || TypeHelper.TypesAreEquivalent(type, type.PlatformType.SystemString);
      }
      return false;
    }

    /// <summary>
    /// A string identifying the specific language for which this is a compilation helper.
    /// </summary>
    public string LanguageName {
      get { return this.languageName; }
    }
    readonly string languageName;

    /// <summary>
    /// Returns an expression that converts a nullable value of one type into a nullable value of another type. Does not unbox the source value if it is null.
    /// </summary>
    //^ [Pure]
    private Expression LiftedConversionExpression(Expression expression, ITypeDefinition unwrappedSourceType, ITypeDefinition sourceType, ITypeDefinition unwrappedTargetType, ITypeDefinition targetType, bool isExplicitConversion)
      //^ requires unwrappedSourceType != sourceType && unwrappedSourceType == this.RemoveNullableWrapper(sourceType);
      //^ requires unwrappedTargetType != targetType && unwrappedTargetType == this.RemoveNullableWrapper(targetType);
    {
      Expression unboxedNullable = this.UnboxedNullable(expression, unwrappedSourceType, sourceType);
      Expression unliftedConversion = this.Conversion(unboxedNullable, unwrappedTargetType, isExplicitConversion, unboxedNullable.SourceLocation);
      if (unliftedConversion is DummyExpression) return unliftedConversion;
      IMethodDefinition/*?*/ userDefinedUnliftedConversion = null;
      MethodCall/*?*/ mcall = unliftedConversion as MethodCall;
      if (mcall != null && unliftedConversion != unboxedNullable) userDefinedUnliftedConversion = mcall.ResolvedMethod;
      Expression result = new LiftedConversion(expression, userDefinedUnliftedConversion, targetType, expression.SourceLocation);
      result.SetContainingExpression(expression);
      return result;
    }

    /// <summary>
    /// Makes a shallow copy of this helper instance, if necessary.
    /// The shallow copy may share child objects with this instance, but should never expose such child objects except through
    /// wrappers (or shallow copies made on demand). If this instance is already associated with the given compilation this
    /// method just returns the instance.
    /// </summary>
    //^ [MustOverride]
    //^ [Pure]
    public virtual LanguageSpecificCompilationHelper MakeShallowCopyFor(Compilation targetCompilation)
      //^ ensures result.GetType() == this.GetType();
    {
      return new LanguageSpecificCompilationHelper(compilation, this);
    }

    /// <summary>
    /// Returns true if method1 has at least one parameter with a type that is more specific (see IsMoreSpecific) than the
    /// type of the correponding parameter in method2. Method1 and Method2 must have the same number of parameters.
    /// </summary>
    //^ [Pure]
    protected virtual bool Method1HasMoreSpecificParameterTypesThanMethod2(IMethodDefinition method1, IMethodDefinition method2)
      // ^ requires EnumerationHelper.EnumerableCount(method1.Parameters) == EnumerationHelper.EnumerableCount(method2.Parameters);
    {
      bool result = false;
      IEnumerator<IParameterDefinition> m2ParameterEnumerator = method2.Parameters.GetEnumerator();
      foreach (IParameterDefinition m1Parameter in method1.Parameters) {
        if (!m2ParameterEnumerator.MoveNext()) {
          //^ assume false; //the precondition should exclude this
          return result;
        }
        IParameterDefinition m2Parameter = m2ParameterEnumerator.Current;
        if (this.IsMoreSpecific(m1Parameter.Type.ResolvedType, m2Parameter.Type.ResolvedType))
          result = true;
      }
      return result;
    }

    /// <summary>
    /// Returns true if the given method can be called with the given arguments, if 
    /// need be after applying implicit conversions and constructing a parameter array, 
    /// or a runtimeargumenthandle.
    /// </summary>
    //^ [Pure]
    public virtual bool MethodIsEligible(IMethodDefinition method, IEnumerable<Expression> arguments) {
      return this.MethodIsEligible(method, arguments, false);
    }

    /// <summary>
    /// Returns true if the given method can be called with the given arguments, if 
    /// need be after applying implicit conversions and constructing a parameter array, 
    /// or a runtimeargumenthandle. If allowTypeMismatch is true then only the number of parameters are considered
    /// when deciding if method is eligible.
    /// </summary>
    //^ [Pure]
    protected virtual bool MethodIsEligible(IMethodDefinition method, IEnumerable<Expression> arguments, bool allowTypeMismatch) {
      IEnumerator<IParameterDefinition> methodParameterEnumerator = method.Parameters.GetEnumerator();
      ITypeDefinition methodParamArrayElementType = Dummy.Type;
      foreach (Expression argument in arguments) {
        ITypeDefinition parType = methodParamArrayElementType;
        if (methodParameterEnumerator.MoveNext()) {
          IParameterDefinition mParam = methodParameterEnumerator.Current;
          if (!allowTypeMismatch) {
            if (mParam.IsOut) {
              if (!(argument is OutArgument)) return false;
            } else if (mParam.IsByReference) {
              if (!(argument is RefArgument)) return false;
            } else {
              if (argument is RefArgument || argument is OutArgument) return false;
            }
          }
          parType = mParam.Type.ResolvedType;
          if (mParam.IsParameterArray) {
            methodParamArrayElementType = mParam.ParamArrayElementType.ResolvedType;
            if (!this.ImplicitConversionExists(argument, parType)) {
              parType = methodParamArrayElementType;
            } else
              methodParamArrayElementType = Dummy.Type;
          }
        } else {
          // if the number of the parameter is less than the number of arguments
          // we need to check to see if the method accepts extra arguments, in which
          // case we do not check the arguments' types.
          if (method.AcceptsExtraArguments) {
            return true;
          }
        }
        if (!this.ImplicitConversionExists(argument, parType)) {
          if (!allowTypeMismatch) return false;
        }
      }
      if (methodParameterEnumerator.MoveNext()) return methodParameterEnumerator.Current.IsParameterArray;
      return true;
    }

    /// <summary>
    /// Returns true if method1 matches the given arguments better than method2 does. "Better" implies that method1 is as good a match as method 2 for every argument
    /// and a better match for at least one argument.
    /// </summary>
    //^ [Pure]
    protected virtual bool Method1MatchesArgumentsBetterThanMethod2(IMethodDefinition method1, IMethodDefinition method2, IEnumerable<Expression> arguments) {
      IEnumerator<IParameterDefinition> method1ParameterEnumerator = method1.Parameters.GetEnumerator();
      IEnumerator<IParameterDefinition> method2ParameterEnumerator = method2.Parameters.GetEnumerator();
      ITypeDefinition method1ParamArrayElementType = Dummy.Type;
      ITypeDefinition method2ParamArrayElementType = Dummy.Type;
      bool method1MustBeExpanded = false;
      bool method2MustBeExpanded = false;
      bool method1IsBetterForSomeArgument = false;
      foreach (Expression argument in arguments) {
        //TODO: worry about lambdas
        ITypeDefinition par1Type = method1ParamArrayElementType;
        if (method1ParameterEnumerator.MoveNext()) {
          IParameterDefinition m1Param = method1ParameterEnumerator.Current;
          if (m1Param.IsOut) {
            if (!(argument is OutArgument)) return false;
          } else if (m1Param.IsByReference) {
            if (!(argument is RefArgument)) return false;
          } else {
            if (argument is RefArgument || argument is OutArgument) return false;
          }
          par1Type = m1Param.Type.ResolvedType;
          if (m1Param.IsParameterArray) {
            method1ParamArrayElementType = m1Param.ParamArrayElementType.ResolvedType;
            if (!this.ImplicitConversionExists(argument, par1Type)) {
              par1Type = method1ParamArrayElementType;
              method1MustBeExpanded = true;
            } else
              method1ParamArrayElementType = Dummy.Type;
          }
        } else
          method1MustBeExpanded = true;
        ITypeDefinition par2Type = method2ParamArrayElementType;
        if (method2ParameterEnumerator.MoveNext()) {
          IParameterDefinition m2Param = method2ParameterEnumerator.Current;
          if (m2Param.IsOut) {
            if (!(argument is OutArgument)) method1IsBetterForSomeArgument = true;
          } else if (m2Param.IsByReference) {
            if (!(argument is RefArgument)) method1IsBetterForSomeArgument = true;
          } else {
            if (argument is RefArgument || argument is OutArgument) method1IsBetterForSomeArgument = true;
          }
          par2Type = m2Param.Type.ResolvedType;
          if (m2Param.IsParameterArray) {
            method2ParamArrayElementType = m2Param.ParamArrayElementType.ResolvedType;
            if (!this.ImplicitConversionExists(argument, par2Type)) {
              par2Type = method2ParamArrayElementType;
              method2MustBeExpanded = true;
            } else
              method2ParamArrayElementType = Dummy.Type;
          }
        } else
          method2MustBeExpanded = true;
        //If method1 is a worse match than method2 for this argument it cannot be better than method2 for all argumeents.
        if (this.ImplicitConversionFromArgumentToType1isBetterThanImplicitConversionToType2(argument, par2Type, par1Type)) return false;
        if (!method1IsBetterForSomeArgument)
          method1IsBetterForSomeArgument = this.ImplicitConversionFromArgumentToType1isBetterThanImplicitConversionToType2(argument, par1Type, par2Type);
      }
      //If method1 has parameters for which there are no matching arguments, return false unless the unmatched parameter is a parameter array
      if (method1ParameterEnumerator.MoveNext()) {
        if (!method1ParameterEnumerator.Current.IsParameterArray) return false;
        if (!method2ParameterEnumerator.MoveNext()) return false; //method1 has an unmatched parameter array, method2 does not
      }
      //At this point method1 is not worse than method2 for any argument
      if (method1IsBetterForSomeArgument) return true;
      //At this point there is a tie.
      method1 = this.UninstantiateAndUnspecialize(method1);
      method2 = this.UninstantiateAndUnspecialize(method2);
      if (!method1.IsGeneric && method2.IsGeneric) return true;
      if (method1.IsGeneric && !method2.IsGeneric) return false;
      if (!method1MustBeExpanded && method2MustBeExpanded) return true;
      if (method1MustBeExpanded && !method2MustBeExpanded) return false;
      uint method1NumberOfParameters = IteratorHelper.EnumerableCount(method1.Parameters);
      uint method2NumberOfParameters = IteratorHelper.EnumerableCount(method2.Parameters);
      if (method1MustBeExpanded) {
        //^ assert method2MustBeExpanded;
        if (method1NumberOfParameters > method2NumberOfParameters) return true; //less expanded than method2
        if (method2NumberOfParameters > method1NumberOfParameters) return false; //more expanded than method2
      } else {
        if (method1NumberOfParameters > method2NumberOfParameters) return false; //method1 has a parameter array that has been "expanded" zero times
        if (method2NumberOfParameters > method1NumberOfParameters) return true; //method2 has a parameter array that has been "expanded" zero times
      }
      //^ assert method1NumberOfParameters == method2NumberOfParameters;
      if (this.Method1HasMoreSpecificParameterTypesThanMethod2(method1, method2)) return true;
      if (this.Method1HasMoreSpecificParameterTypesThanMethod2(method2, method1)) return false;
      if (TypeHelper.Type1DerivesFromType2(method1.ContainingTypeDefinition, method2.ContainingTypeDefinition) && this.ParametersMatch(method1, method2)) return true;
      return false;
    }

    /// <summary>
    /// Returns true if the given method can be called with the given number of arguments 
    /// either because the method's last parameter is a parameter array 
    /// or because the method accepts extra parameter
    /// </summary>
    //^ [Pure]
    private bool MethodQualifiesEvenIfArgumentNumberMismatches(IMethodDefinition method, uint methodParameterCount, uint argumentCount)
      // ^ requires EnumerationHelper.EnumerableCount(method.Parameters) == methodParameterCount;
    {
      if (methodParameterCount < 1 || argumentCount < methodParameterCount - 1) return false;
      if (method.AcceptsExtraArguments && argumentCount > methodParameterCount) {
        return true;
      }
      IEnumerator<IParameterDefinition> parameterEnumerator = method.Parameters.GetEnumerator();
      while (parameterEnumerator.MoveNext()) {
        if (--methodParameterCount == 0) {
          IParameterDefinition lastParameter = parameterEnumerator.Current;
          if (lastParameter.IsParameterArray) return true;
        }
      }
      return false;
    }

    /// <summary>
    /// A table used to intern strings used as names. This table is obtained from the host environment.
    /// It is mutuable, in as much as it is possible to add new names to the table.
    /// </summary>
    public INameTable NameTable {
      get { return this.Compilation.NameTable; }
    }

    /// <summary>
    /// Returns true if sourceType and targetType are numeric types and an conversion from sourceType to targetType exists.
    /// If the method returns true, it also updates expression with an expression that will perform the numeric conversion at runtime.
    /// </summary>
    protected virtual bool NumericConversionWasPossible(ref Expression expression, ITypeDefinition sourceType, ITypeDefinition targetType, bool isExplicitConversion) {
      if (isExplicitConversion) {
        if (sourceType.IsEnum) {
          sourceType = sourceType.UnderlyingType.ResolvedType;
          if (!targetType.IsEnum) {
            if (this.NumericConversionWasPossible(ref expression, sourceType, targetType, true)) {
              expression = this.ConversionExpression(expression, targetType);
              return true;
            }
          }
        }
        if (targetType.IsEnum) {
          if (this.NumericConversionWasPossible(ref expression, sourceType, targetType.UnderlyingType.ResolvedType, true)) {
            expression = this.ConversionExpression(expression, targetType);
            return true;
          }
        }
      }
      if (TypeHelper.TypesAreEquivalent(sourceType, targetType)) return true;
      switch (sourceType.TypeCode) {
        case PrimitiveTypeCode.Float32:
          //implicit: From float to double
          //explicit: From float to sbyte, byte, short, ushort, int, uint, long, ulong, char, or decimal.
          switch (targetType.TypeCode) {
            case PrimitiveTypeCode.Float64:
              expression = this.ConversionExpression(expression, targetType);
              return true;
            case PrimitiveTypeCode.Int8:
            case PrimitiveTypeCode.UInt8:
            case PrimitiveTypeCode.Int16:
            case PrimitiveTypeCode.UInt16:
            case PrimitiveTypeCode.Int32:
            case PrimitiveTypeCode.UInt32:
            case PrimitiveTypeCode.Int64:
            case PrimitiveTypeCode.UInt64:
              if (isExplicitConversion) goto case PrimitiveTypeCode.Float64;
              return false;
            default:
              if (isExplicitConversion) {
                if (TypeHelper.TypesAreEquivalent(targetType, this.PlatformType.SystemChar)) goto case PrimitiveTypeCode.Float64;
                if (TypeHelper.TypesAreEquivalent(targetType, this.PlatformType.SystemDecimal)) {
                  expression = this.UserDefinedConversion(expression, sourceType, targetType, true);
                  return true;
                }
              }
              return false;
          }
        case PrimitiveTypeCode.Float64:
          //implicit: none
          //explicit: From double to sbyte, byte, short, ushort, int, uint, long, ulong, char, float, or decimal.
          switch (targetType.TypeCode) {
            case PrimitiveTypeCode.Int8:
            case PrimitiveTypeCode.UInt8:
            case PrimitiveTypeCode.Int16:
            case PrimitiveTypeCode.UInt16:
            case PrimitiveTypeCode.Int32:
            case PrimitiveTypeCode.UInt32:
            case PrimitiveTypeCode.Int64:
            case PrimitiveTypeCode.UInt64:
            case PrimitiveTypeCode.Float32:
              if (isExplicitConversion) {
                expression = this.ConversionExpression(expression, targetType);
                return true;
              }
              return false;
            default:
              if (isExplicitConversion) {
                if (TypeHelper.TypesAreEquivalent(targetType, this.PlatformType.SystemChar)) goto case PrimitiveTypeCode.Float32;
                if (TypeHelper.TypesAreEquivalent(targetType, this.PlatformType.SystemDecimal)) {
                  expression = this.UserDefinedConversion(expression, sourceType, targetType, true);
                  return true;
                }
              }
              return false;
          }
        case PrimitiveTypeCode.Int16:
          //implicit: From short to int, long, float, double, or decimal.
          //explicit: From short to sbyte, byte, ushort, uint, ulong, or char.
          switch (targetType.TypeCode) {
            case PrimitiveTypeCode.Int32:
              return true;
            case PrimitiveTypeCode.Int64:
            case PrimitiveTypeCode.Float32:
            case PrimitiveTypeCode.Float64:
              expression = this.ConversionExpression(expression, targetType);
              return true;
            case PrimitiveTypeCode.Int8:
            case PrimitiveTypeCode.UInt8:
            case PrimitiveTypeCode.UInt16:
            case PrimitiveTypeCode.UInt32:
            case PrimitiveTypeCode.UInt64:
            case PrimitiveTypeCode.IntPtr:
            case PrimitiveTypeCode.UIntPtr:
              if (isExplicitConversion) goto case PrimitiveTypeCode.Int64;
              return false;
            default:
              if (TypeHelper.TypesAreEquivalent(targetType, this.PlatformType.SystemDecimal)) {
                expression = this.UserDefinedConversion(expression, sourceType, targetType, false);
                return true;
              }
              if (isExplicitConversion) {
                if (TypeHelper.TypesAreEquivalent(targetType, this.PlatformType.SystemChar)) goto case PrimitiveTypeCode.Int64;
              }
              return false;
          }
        case PrimitiveTypeCode.Int32:
          //implicit: From int to long, float, double, or decimal.
          //explicit: From int to sbyte, byte, short, ushort, uint, ulong, or char.
          switch (targetType.TypeCode) {
            case PrimitiveTypeCode.Int64:
            case PrimitiveTypeCode.Float32:
            case PrimitiveTypeCode.Float64:
            case PrimitiveTypeCode.IntPtr:
            case PrimitiveTypeCode.UIntPtr:
              expression = this.ConversionExpression(expression, targetType);
              return true;
            case PrimitiveTypeCode.Int8:
            case PrimitiveTypeCode.UInt8:
            case PrimitiveTypeCode.Int16:
            case PrimitiveTypeCode.UInt16:
            case PrimitiveTypeCode.UInt32:
            case PrimitiveTypeCode.UInt64:
              if (isExplicitConversion) goto case PrimitiveTypeCode.Int64;
              return false;
            default:
              if (TypeHelper.TypesAreEquivalent(targetType, this.PlatformType.SystemDecimal)) {
                expression = this.UserDefinedConversion(expression, sourceType, targetType, false);
                return true;
              }
              if (isExplicitConversion) {
                if (TypeHelper.TypesAreEquivalent(targetType, this.PlatformType.SystemChar)) goto case PrimitiveTypeCode.Int64;
              }
              return false;
          }
        case PrimitiveTypeCode.Int64:
        //implicit: From long to float, double, or decimal.
        //explicit:	From long to sbyte, byte, short, ushort, int, uint, ulong, or char.
        case PrimitiveTypeCode.UInt64:
          //implicit: From ulong to float, double, or decimal.
          //explicit: From ulong to sbyte, byte, short, ushort, int, uint, long, or char.
          switch (targetType.TypeCode) {
            case PrimitiveTypeCode.Float32:
            case PrimitiveTypeCode.Float64:
              expression = this.ConversionExpression(expression, targetType);
              return true;
            case PrimitiveTypeCode.Int8:
            case PrimitiveTypeCode.UInt8:
            case PrimitiveTypeCode.Int16:
            case PrimitiveTypeCode.UInt16:
            case PrimitiveTypeCode.Int32:
            case PrimitiveTypeCode.UInt32:
            case PrimitiveTypeCode.Int64:
            case PrimitiveTypeCode.UInt64:
            case PrimitiveTypeCode.IntPtr:
            case PrimitiveTypeCode.UIntPtr:
              if (isExplicitConversion) goto case PrimitiveTypeCode.Float32;
              return false;
            default:
              if (TypeHelper.TypesAreEquivalent(targetType, this.PlatformType.SystemDecimal)) {
                expression = this.UserDefinedConversion(expression, sourceType, targetType, false);
                return true;
              }
              if (isExplicitConversion) {
                if (TypeHelper.TypesAreEquivalent(targetType, this.PlatformType.SystemChar)) goto case PrimitiveTypeCode.Float32;
              }
              return false;
          }
        case PrimitiveTypeCode.IntPtr:
          if (!isExplicitConversion) goto default;
          switch (targetType.TypeCode) {
            case PrimitiveTypeCode.Int32:
            case PrimitiveTypeCode.Int64:
              expression = this.UserDefinedConversion(expression, sourceType, targetType, true);
              return true;
            case PrimitiveTypeCode.Float32:
            case PrimitiveTypeCode.Int8:
            case PrimitiveTypeCode.UInt8:
            case PrimitiveTypeCode.Int16:
            case PrimitiveTypeCode.UInt16:
            case PrimitiveTypeCode.UInt32:
              expression = this.UserDefinedConversion(expression, sourceType, this.PlatformType.SystemInt32.ResolvedType, true);
              expression = this.ConversionExpression(expression, targetType);
              return true;
            case PrimitiveTypeCode.Float64:
            case PrimitiveTypeCode.UInt64:
              expression = this.UserDefinedConversion(expression, sourceType, this.PlatformType.SystemInt64.ResolvedType, true);
              expression = this.ConversionExpression(expression, targetType);
              return true;
            default:
              if (TypeHelper.TypesAreEquivalent(targetType, this.PlatformType.SystemDecimal)) {
                expression = this.UserDefinedConversion(expression, sourceType, this.PlatformType.SystemInt64.ResolvedType, true);
                expression = this.UserDefinedConversion(expression, this.PlatformType.SystemInt64.ResolvedType, targetType, true);
                return true;
              }
              if (isExplicitConversion && targetType is IPointerTypeReference) {
                expression = this.UserDefinedConversion(expression, sourceType, this.PlatformType.SystemVoidPtr.ResolvedType, true);
                expression = this.ConversionExpression(expression, targetType);
                return true;
              }
              return false;
          }
        case PrimitiveTypeCode.Int8:
          //implicit: From sbyte to short, int, long, float, double, or decimal.
          //explicit: From sbyte to byte, ushort, uint, ulong, or char.
          switch (targetType.TypeCode) {
            case PrimitiveTypeCode.Int16:
            case PrimitiveTypeCode.Int32:
              return true;
            case PrimitiveTypeCode.Int64:
            case PrimitiveTypeCode.Float32:
            case PrimitiveTypeCode.Float64:
              expression = this.ConversionExpression(expression, targetType);
              return true;
            case PrimitiveTypeCode.UInt8:
            case PrimitiveTypeCode.UInt16:
            case PrimitiveTypeCode.UInt32:
            case PrimitiveTypeCode.UInt64:
            case PrimitiveTypeCode.IntPtr:
            case PrimitiveTypeCode.UIntPtr:
              if (isExplicitConversion) goto case PrimitiveTypeCode.Int64;
              return false;
            default:
              if (TypeHelper.TypesAreEquivalent(targetType, this.PlatformType.SystemDecimal)) {
                expression = this.UserDefinedConversion(expression, sourceType, targetType, false);
                return true;
              }
              if (isExplicitConversion) {
                if (TypeHelper.TypesAreEquivalent(targetType, this.PlatformType.SystemChar)) goto case PrimitiveTypeCode.Int64;
              }
              return false;
          }
        case PrimitiveTypeCode.UInt16:
          //implicit: From ushort to int, uint, long, ulong, float, double, or decimal.
          //explicit: From ushort to sbyte, byte, short, or char.
          switch (targetType.TypeCode) {
            case PrimitiveTypeCode.UInt16:
            case PrimitiveTypeCode.Int32:
            case PrimitiveTypeCode.UInt32:
              return true;
            case PrimitiveTypeCode.Int64:
            case PrimitiveTypeCode.UInt64:
            case PrimitiveTypeCode.Float32:
            case PrimitiveTypeCode.Float64:
              expression = this.ConversionExpression(expression, targetType);
              return true;
            case PrimitiveTypeCode.Int8:
            case PrimitiveTypeCode.UInt8:
            case PrimitiveTypeCode.Int16:
            case PrimitiveTypeCode.IntPtr:
            case PrimitiveTypeCode.UIntPtr:
              if (isExplicitConversion) goto case PrimitiveTypeCode.Int64;
              return false;
            default:
              if (TypeHelper.TypesAreEquivalent(targetType, this.PlatformType.SystemDecimal)) {
                expression = this.UserDefinedConversion(expression, sourceType, targetType, false);
                return true;
              }
              if (isExplicitConversion) {
                if (TypeHelper.TypesAreEquivalent(targetType, this.PlatformType.SystemChar)) goto case PrimitiveTypeCode.Int64;
              }
              return false;
          }
        case PrimitiveTypeCode.UInt32:
          //implicit: From uint to long, ulong, float, double, or decimal.
          //explicit: From uint to sbyte, byte, short, ushort, int, or char.
          switch (targetType.TypeCode) {
            case PrimitiveTypeCode.Int64:
            case PrimitiveTypeCode.UInt64:
            case PrimitiveTypeCode.Float32:
            case PrimitiveTypeCode.Float64:
              expression = this.ConversionExpression(expression, targetType);
              return true;
            case PrimitiveTypeCode.Int8:
            case PrimitiveTypeCode.UInt8:
            case PrimitiveTypeCode.Int16:
            case PrimitiveTypeCode.UInt16:
            case PrimitiveTypeCode.Int32:
            case PrimitiveTypeCode.IntPtr:
            case PrimitiveTypeCode.UIntPtr:
              if (isExplicitConversion) goto case PrimitiveTypeCode.Int64;
              return false;
            default:
              if (TypeHelper.TypesAreEquivalent(targetType, this.PlatformType.SystemDecimal)) {
                expression = this.UserDefinedConversion(expression, sourceType, targetType, false);
                return true;
              }
              if (isExplicitConversion) {
                if (TypeHelper.TypesAreEquivalent(targetType, this.PlatformType.SystemChar)) goto case PrimitiveTypeCode.Int64;
              }
              return false;
          }
        case PrimitiveTypeCode.UIntPtr:
          if (!isExplicitConversion) goto default;
          switch (targetType.TypeCode) {
            case PrimitiveTypeCode.UInt32:
            case PrimitiveTypeCode.UInt64:
              expression = this.UserDefinedConversion(expression, sourceType, targetType, true);
              return true;
            case PrimitiveTypeCode.Float32:
            case PrimitiveTypeCode.Int8:
            case PrimitiveTypeCode.UInt8:
            case PrimitiveTypeCode.Int16:
            case PrimitiveTypeCode.UInt16:
            case PrimitiveTypeCode.Int32:
              expression = this.UserDefinedConversion(expression, sourceType, this.PlatformType.SystemUInt32.ResolvedType, true);
              expression = this.ConversionExpression(expression, targetType);
              return true;
            case PrimitiveTypeCode.Float64:
            case PrimitiveTypeCode.Int64:
              expression = this.UserDefinedConversion(expression, sourceType, this.PlatformType.SystemUInt64.ResolvedType, true);
              expression = this.ConversionExpression(expression, targetType);
              return true;
            default:
              if (TypeHelper.TypesAreEquivalent(targetType, this.PlatformType.SystemDecimal)) {
                expression = this.UserDefinedConversion(expression, sourceType, this.PlatformType.SystemUInt64.ResolvedType, true);
                expression = this.UserDefinedConversion(expression, this.PlatformType.SystemUInt64.ResolvedType, targetType, true);
                return true;
              }
              if (isExplicitConversion && targetType is IPointerTypeReference) {
                expression = this.UserDefinedConversion(expression, sourceType, this.PlatformType.SystemVoidPtr.ResolvedType, true);
                expression = this.ConversionExpression(expression, targetType);
                return true;
              }
              return false;
          }
        case PrimitiveTypeCode.UInt8:
          //implicit: From byte to short, ushort, int, uint, long, ulong, float, double, or decimal.
          //explicit: From byte to sbyte or char.
          switch (targetType.TypeCode) {
            case PrimitiveTypeCode.Int16:
            case PrimitiveTypeCode.UInt16:
            case PrimitiveTypeCode.Int32:
            case PrimitiveTypeCode.UInt32:
              return true;
            case PrimitiveTypeCode.Int64:
            case PrimitiveTypeCode.UInt64:
            case PrimitiveTypeCode.Float32:
            case PrimitiveTypeCode.Float64:
              expression = this.ConversionExpression(expression, targetType);
              return true;
            case PrimitiveTypeCode.Int8:
            case PrimitiveTypeCode.IntPtr:
            case PrimitiveTypeCode.UIntPtr:
              if (isExplicitConversion) goto case PrimitiveTypeCode.Int64;
              return false;
            default:
              if (TypeHelper.TypesAreEquivalent(targetType, this.PlatformType.SystemDecimal)) {
                expression = this.UserDefinedConversion(expression, sourceType, targetType, false);
                return true;
              }
              if (isExplicitConversion) {
                if (TypeHelper.TypesAreEquivalent(targetType, this.PlatformType.SystemChar)) goto case PrimitiveTypeCode.Int64;
              }
              return false;
          }
        default:
          if (TypeHelper.TypesAreEquivalent(sourceType, this.PlatformType.SystemChar)) goto case PrimitiveTypeCode.UInt16;
          if (isExplicitConversion && TypeHelper.TypesAreEquivalent(sourceType, this.PlatformType.SystemDecimal)) {
            //explicit: From decimal to sbyte, byte, short, ushort, int, uint, long, ulong, char, float, or double.
            switch (targetType.TypeCode) {
              case PrimitiveTypeCode.Int8:
              case PrimitiveTypeCode.UInt8:
              case PrimitiveTypeCode.Int16:
              case PrimitiveTypeCode.UInt16:
              case PrimitiveTypeCode.Int32:
              case PrimitiveTypeCode.UInt32:
              case PrimitiveTypeCode.Int64:
              case PrimitiveTypeCode.UInt64:
              case PrimitiveTypeCode.Float32:
              case PrimitiveTypeCode.Float64:
                expression = this.UserDefinedConversion(expression, sourceType, targetType, true);
                return true;
              default:
                if (TypeHelper.TypesAreEquivalent(targetType, this.PlatformType.SystemChar)) goto case PrimitiveTypeCode.Int8;
                return false;
            }
          }
          if (isExplicitConversion && sourceType is IPointerTypeReference) {
            switch (targetType.TypeCode) {
              case PrimitiveTypeCode.Int8:
              case PrimitiveTypeCode.UInt8:
              case PrimitiveTypeCode.Int16:
              case PrimitiveTypeCode.UInt16:
              case PrimitiveTypeCode.Int32:
              case PrimitiveTypeCode.UInt32:
              case PrimitiveTypeCode.Int64:
              case PrimitiveTypeCode.UInt64:
                expression = this.ConversionExpression(expression, targetType);
                return true;
              case PrimitiveTypeCode.IntPtr:
              case PrimitiveTypeCode.UIntPtr:
                expression = this.ConversionExpression(expression, this.PlatformType.SystemVoidPtr.ResolvedType);
                expression = this.UserDefinedConversion(expression, expression.Type, targetType, true);
                return true;
              default:
                return false;
            }
          }
          return false;
      }
    }

    /// <summary>
    /// Returns true if delegateSignature and methodSignature are contra variant in their parameter types.
    /// That is, each of the delegateSignature parameter types can be converted to the corresponding parameter types of methodSignature. 
    /// For the purpose of this method, a type is regarded as convertible to another type if there is a standard (CLR supplied) reference conversion.
    /// </summary>
    //^ [Pure]
    private bool ParametersAreConsistent(ISignature delegateSignature, ISignature methodSignature) {
      IEnumerator<IParameterTypeInformation> delPars = delegateSignature.Parameters.GetEnumerator();
      IEnumerator<IParameterTypeInformation> methPars = methodSignature.Parameters.GetEnumerator();
      while (delPars.MoveNext() && methPars.MoveNext()) {
        IParameterTypeInformation delPar = delPars.Current;
        IParameterTypeInformation methPar = methPars.Current;
        if (!TypeHelper.TypesAreEquivalent(delPar.Type.ResolvedType, methPar.Type.ResolvedType)) {
          if (this.TypesAreClrAssignmentCompatible(delPar.Type.ResolvedType, methPar.Type.ResolvedType))
            return !delPar.IsByReference && !methPar.IsByReference;
          return false;
        }
        if (delPar.IsByReference != methPar.IsByReference) return false;
      }
      if (delPars.MoveNext() || methPars.MoveNext()) return false;
      return true;
    }

    /// <summary>
    /// Returns true if signature1 and signature2 have the same number of parameters and for each parameter pair the types and mode (by ref/by value) match.
    /// </summary>
    //^ [Pure]
    private bool ParametersMatch(ISignature signature1, ISignature signature2) {
      IEnumerator<IParameterTypeInformation> sig1Pars = signature1.Parameters.GetEnumerator();
      IEnumerator<IParameterTypeInformation> sig2Pars = signature2.Parameters.GetEnumerator();
      while (sig1Pars.MoveNext() && sig2Pars.MoveNext()) {
        IParameterTypeInformation par1 = sig1Pars.Current;
        IParameterTypeInformation par2 = sig2Pars.Current;
        if (!TypeHelper.TypesAreEquivalent(par1.Type.ResolvedType, par2.Type.ResolvedType)) return false;
        if (par1.IsByReference != par2.IsByReference) return false;
      }
      if (sig1Pars.MoveNext() || sig2Pars.MoveNext()) return false;
      return true;
    }

    /// <summary>
    /// A collection of well known types that must be part of every target platform and that are fundamental to modeling compiled code.
    /// The types are obtained by querying the unit set of the compilation and thus can include types that are defined by the compilation itself.
    /// </summary>
    public PlatformType PlatformType {
      get { return this.Compilation.PlatformType; }
    }

    /// <summary>
    /// Selects the member of the given collection of methods that best matches the given arguments.
    /// This method is expected to have language specific behavior when invoked via an instance of a subclass of LanguageSpecificCompilationHelper.
    /// When invoked via an instance of a standard framework class (such as CompilationHelper), C# rules apply.
    /// If no the method collection is empty or if there is no single best match, Dummy.Method is returned.
    /// </summary>
    //^ [Pure]
    public IMethodDefinition ResolveOverload(IEnumerable<IMethodDefinition> candidateMethods, params Expression[] arguments) {
      return this.ResolveOverload(candidateMethods, (IEnumerable<Expression>)arguments, false);
    }

    /// <summary>
    /// Selects the member of the given collection of methods that best matches the given arguments.
    /// If the method collection is empty or none of its elements can be called with the given arguments, or if there is no single 
    /// best match, Dummy.Method is returned.
    /// </summary>
    //^ [Pure]
    public IMethodDefinition ResolveOverload(IEnumerable<IMethodDefinition> candidateMethods, IEnumerable<Expression> arguments) {
      return this.ResolveOverload(candidateMethods, arguments, false);
    }

    /// <summary>
    /// Selects the member of the given collection of methods that best matches the given arguments.
    /// If the method collection is empty or none of its elements can be called with the given arguments, or if there is no single 
    /// best match, Dummy.Method is returned. If allowTypeMismatches is true then a method can be returned even if the arguments
    /// do not match its parameters, provided that the correct number of arguments are passed and there is only one such method.
    /// </summary>
    //^ [Pure]
    public virtual IMethodDefinition ResolveOverload(IEnumerable<IMethodDefinition> candidateMethods, IEnumerable<Expression> arguments, bool allowTypeMismatches) {
      IMethodDefinition bestSoFar = Dummy.Method;
      List<IMethodDefinition>/*?*/ ambiguousMatches = null;
      foreach (IMethodDefinition candidate in candidateMethods) {
        if (bestSoFar is Dummy) {
          if (this.MethodIsEligible(candidate, arguments)) bestSoFar = candidate;
          continue;
        }
        if (!this.MethodIsEligible(candidate, arguments)) {
          continue;
        }
        if (this.Method1MatchesArgumentsBetterThanMethod2(bestSoFar, candidate, arguments)) continue;
        if (this.Method1MatchesArgumentsBetterThanMethod2(candidate, bestSoFar, arguments)) {
          //candidate is better than bestSoFar, so replace it with candidate
          bestSoFar = candidate;
          if (ambiguousMatches == null) continue; //candidate is the clear winner.
          //All of the members of ambiguousMatches were neither better nor worse than the old value of bestSoFar.
          //Throw out any members for which this is no longer the case.
          List<IMethodDefinition> remainingAmbiguousMatches = new List<IMethodDefinition>(ambiguousMatches.Count);
          foreach (IMethodDefinition ameth in ambiguousMatches) {
            //ameth cannot be better than candidate, since the latter is better than the old value of bestSoFar and ameth is known not to better than that.
            if (this.Method1MatchesArgumentsBetterThanMethod2(candidate, ameth, arguments)) continue; //ameth loses and drops out of the race
            remainingAmbiguousMatches.Add(ameth); //ameth is neither better nor worse than candidate
          }
          if (remainingAmbiguousMatches.Count == 0)
            ambiguousMatches = null;
          else
            ambiguousMatches = remainingAmbiguousMatches;
          continue;
        }
        //Neither candidate nor bestSoFar is better than the other. Stick with bestSoFar, but remember the candidate.
        if (ambiguousMatches == null) ambiguousMatches = new List<IMethodDefinition>();
        ambiguousMatches.Add(candidate);
      }
      if (ambiguousMatches != null) return Dummy.Method;
      if (bestSoFar is Dummy && allowTypeMismatches)
        return this.SingleCandidateWithTheGivenNumberOfArguments(candidateMethods, arguments);
      else
        return bestSoFar;
    }

    /// <summary>
    /// Returns the candidate method with a parameter list that could match the given argument list, if there were not type mismatches, provided 
    /// that there is only one such candidate. If not, returns Dummy.Method.
    /// </summary>
    private IMethodDefinition SingleCandidateWithTheGivenNumberOfArguments(IEnumerable<IMethodDefinition> candidateMethods, IEnumerable<Expression> arguments) {
      IMethodDefinition likelyMatch = Dummy.Method;
      foreach (IMethodDefinition candidate in candidateMethods) {
        if (this.MethodIsEligible(candidate, arguments, true)) {
          if (!(likelyMatch is Dummy)) return Dummy.Method;
          likelyMatch = candidate;
        }
      }
      return likelyMatch;
    }

    /// <summary>
    /// If the given type is an instance if System.Nullable&lt;T&gt;, return the value of T. Otherwise return the given type.
    /// </summary>
    //^ [Pure]
    public ITypeDefinition RemoveNullableWrapper(ITypeDefinition type) {
      IGenericTypeInstanceReference/*?*/ instance = type as IGenericTypeInstanceReference;
      if (instance == null) return type;
      if (!TypeHelper.TypesAreEquivalent(instance.GenericType.ResolvedType, this.PlatformType.SystemNullable)) return type;
      foreach (ITypeReference arg in instance.GenericArguments) return arg.ResolvedType;
      //^ assume false; //An instance of PlatformType.SystemNullable should always have exactly one argument, but getting the verifier to work that out for itself is a asking a bit much.
      return type;
    }

    /// <summary>
    /// Raises the CompilationErrors event with the given error wrapped up in an error event arguments object.
    /// </summary>
    /// <param name="error">The error to report.</param>
    public void ReportError(IErrorMessage error) {
      this.Compilation.HostEnvironment.ReportError(error);
    }

    /// <summary>
    /// Reports an error stating that the type of the given expression cannot be converted to the given parameter type.
    /// </summary>
    /// <param name="expression">The expression that results in the argument value.</param>
    /// <param name="parameterType">The type of the parameter.</param>
    /// <param name="parameterIndex">The zero based index of the parameter.</param>
    public virtual void ReportFailedArgumentConversion(Expression expression, ITypeDefinition parameterType, int parameterIndex) {
      if (expression.HasErrors) return;
      if (expression.Type is Dummy)
        this.ReportFailedImplicitConversion(expression, parameterType);
      else
        this.ReportError(new AstErrorMessage(expression, Error.BadArgumentType, (parameterIndex+1).ToString(), 
          this.GetTypeName(expression.Type), this.GetTypeName(parameterType)));
    }

    /// <summary>
    /// Reports an error to the effect that the given expression cannot be implicitly converted to the given type.
    /// Expressions that are constants as well as expressions that could have been explicitly converted are treated
    /// as special cases.
    /// </summary>
    /// <param name="expression">The expression that could not be converted.</param>
    /// <param name="targetType">The type to which the expression could not be implicitly converted.</param>
    //^ [Pure]
    public virtual void ReportFailedImplicitConversion(Expression expression, ITypeDefinition targetType) {
      if (expression.HasErrors || targetType is Dummy) return;
      if (expression.Type is Dummy) {
        if (targetType.IsDelegate)
          this.ReportFailedMethodGroupToDelegateConversion(expression, targetType);
        else
          this.ReportError(new AstErrorMessage(expression, Error.NoImplicitConversionForValue, expression.SourceLocation.Source, this.GetTypeName(targetType)));
        return;
      }
      string sourceTypeName = this.GetTypeName(expression.Type);
      string targetTypeName = this.GetTypeName(targetType);
      if (this.ExpressionIsNumericLiteral(expression)) {
        //^ assert expression.Value != null;
        this.ReportError(new AstErrorMessage(expression, Error.ConstOutOfRange, String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}", expression.Value), targetTypeName));
      } else if (!this.ReportAreYouMissingACast || this.ExplicitConversion(expression, targetType) is DummyExpression)
        this.ReportError(new AstErrorMessage(expression, Error.NoImplicitConversion, sourceTypeName, targetTypeName));
      else
        this.ReportError(new AstErrorMessage(expression, Error.NoImplicitConvCast, sourceTypeName, targetTypeName));
    }

    /// <summary>
    /// 
    /// </summary>
    protected virtual bool ReportAreYouMissingACast {
      get { return true; }
    }

    /// <summary>
    /// Reports an error to the effect that the given method group expression cannot be implicitly converted to the given delegate type.
    /// </summary>
    /// <param name="expression">The expression that could not be converted. The expression should have no errors and should not have a type.</param>
    /// <param name="targetType">The delegate type to which the expression could not be implicitly converted.</param>
    //^ [Pure]
    public virtual void ReportFailedMethodGroupToDelegateConversion(Expression expression, ITypeDefinition targetType)
      //^ requires !expression.HasErrors;
      //^ requires targetType.IsDelegate;
    {
      IMethodDefinition/*?*/ methodGroupRepresentative = this.ResolveIfName(expression) as IMethodDefinition;
      if (methodGroupRepresentative == null) {
        //Check if expression binds to a group of generic methods
        GenericInstanceExpression/*?*/ genericInstance = expression as GenericInstanceExpression;
        if (genericInstance != null)
          methodGroupRepresentative = this.ResolveIfName(genericInstance.GenericTypeOrMethod) as IMethodDefinition;
      }
      if (methodGroupRepresentative == null) {
        //TODO: error about expression not being a method
        return;
      }
      bool sawOnlyGenericMethods = true;
      IMethodDefinition invokeMethod = this.GetInvokeMethod(targetType);
      IMethodDefinition/*?*/ methodToComplainAbout = null;
      foreach (IMethodDefinition method in this.GetMethodGroupMethods(methodGroupRepresentative, IteratorHelper.EnumerableCount(invokeMethod.Parameters), false)) {
        if (!method.IsGeneric) sawOnlyGenericMethods = false;
        if (methodToComplainAbout == null) methodToComplainAbout = method;
      }
      if (sawOnlyGenericMethods && methodToComplainAbout != null) {
        this.ReportError(new AstErrorMessage(expression, Error.CantInferMethTypeArgs,
          this.GetMethodSignature(methodToComplainAbout, NameFormattingOptions.Signature|NameFormattingOptions.TypeParameters|NameFormattingOptions.UseTypeKeywords)));
        return;
      }
      if (methodToComplainAbout != null && this.ParametersAreConsistent(invokeMethod, methodToComplainAbout)) {
        this.ReportError(new AstErrorMessage(expression, Error.BadReturnType,
          this.GetMethodSignature(methodToComplainAbout,
            NameFormattingOptions.ReturnType|NameFormattingOptions.Signature|NameFormattingOptions.TypeParameters|NameFormattingOptions.UseTypeKeywords)));
        //TODO: related error locations
        return;
      }
      this.ReportError(new AstErrorMessage(expression, Error.NoMatchingOverload, this.GetTypeName(targetType),
          this.GetMethodSignature(methodToComplainAbout,
            NameFormattingOptions.Signature|NameFormattingOptions.TypeParameters|NameFormattingOptions.UseTypeKeywords)));
      //TODO: related error locations
    }

    /// <summary>
    /// Returns true if expression represents a compile time constant that can be regarded as a single numeric literal, such as -1.
    /// </summary>
    public virtual bool ExpressionIsNumericLiteral(Expression expression)
      //^ ensures result ==> expression.Value != null;
    {
      UnaryOperation/*?*/ unary = expression as UnaryOperation;
      if (unary != null) expression = unary.Operand;
      CompileTimeConstant/*?*/ cc = expression as CompileTimeConstant;
      return cc != null && cc.IsNumericConstant && cc.Value != null;
    }

    /// <summary>
    /// Returns true if a value of type sourceType might also be a value of type targetType.
    /// </summary>
    //^ [Pure]
    private bool ReferenceConversionMightBePossibleAtRuntime(ITypeDefinition sourceType, ITypeDefinition targetType) {
      if (TypeHelper.TypesAreEquivalent(sourceType, sourceType.PlatformType.SystemObject)) return targetType.IsReferenceType;
      if (TypeHelper.Type1DerivesFromType2(targetType, sourceType)) return true;
      if (sourceType.IsInterface && (!targetType.IsSealed || TypeHelper.Type1ImplementsType2(targetType, sourceType))) return true;
      if (targetType.IsInterface && !sourceType.IsSealed) return true;
      if (TypeHelper.Type1IsCovariantWithType2(targetType, sourceType)) return true;
      if (this.ImplicitTypeParameterConversionExists(targetType, sourceType)) return true;
      return false;
    }

    /// <summary>
    /// Resolves the given expression and returns the result.
    /// Always returns null if the expression is not a simple name or a qualified name.
    /// </summary>
    //^ [Pure]
    private object/*?*/ ResolveIfName(Expression methodExpression) {
      SimpleName/*?*/ simpleName = methodExpression as SimpleName;
      if (simpleName != null) return simpleName.Resolve();
      QualifiedName/*?*/ qualifiedName = methodExpression as QualifiedName;
      if (qualifiedName != null) return qualifiedName.Resolve(false);
      return null;
    }

    /// <summary>
    /// Returns true if delegateSignature and methodSignature are covariant in their return types and contra variant in their parameter types.
    /// That is, the return type of methodSignature can be converted to the return type of delegateSignature and each of the delegateSignature
    /// parameter types can be converted to the corresponding parameter types of methodSignature. For the purpose of this method, a type is
    /// regarded as convertible to another type if there is a standard (CLR supplied) reference conversion.
    /// </summary>
    //^ [Pure]
    private bool SignaturesAreConsistent(ISignature delegateSignature, ISignature methodSignature) {
      if (!TypeHelper.TypesAreEquivalent(delegateSignature.Type.ResolvedType, methodSignature.Type.ResolvedType))
        return this.TypesAreClrAssignmentCompatible(methodSignature.Type.ResolvedType, delegateSignature.Type.ResolvedType);
      return this.ParametersAreConsistent(delegateSignature, methodSignature);
    }

    /// <summary>
    /// Checks if selecting this overload would cause something undesirable to happen. For example "int op uint" may become "long op long" which
    /// is desirable for C# but undesirable for C.
    /// </summary>
    public virtual bool StandardBinaryOperatorOverloadIsUndesirable(IMethodDefinition standardBinaryOperatorMethod, Expression leftOperand, Expression rightOperand) {
      return false;
    }

    /// <summary>
    /// A language specific object that formats type member signatures according to syntax of the language.
    /// </summary>
    SignatureFormatter SignatureFormatter {
      get {
        if (this.signatureFormatter == null)
          this.signatureFormatter = this.CreateSignatureFormatter();
        return this.signatureFormatter;
      }
    }
    //^ [Once]
    SignatureFormatter/*?*/ signatureFormatter;

    /// <summary>
    /// Returns true if if the given value is a primitive integer with a 1 value in its most significant bit.
    /// </summary>
    public bool SignBitIsSet(object value) {
      IConvertible/*?*/ ic = value as IConvertible;
      if (ic == null) return false;
      switch (ic.GetTypeCode()) {
        case TypeCode.Boolean: return false;
        case TypeCode.Byte: return ic.ToByte(null) > sbyte.MaxValue;
        case TypeCode.Char: return ic.ToChar(null) > short.MaxValue;
        case TypeCode.Int16: return ic.ToInt16(null) < 0;
        case TypeCode.Int32: return ic.ToInt32(null) < 0;
        case TypeCode.Int64: return ic.ToInt64(null) < 0;
        case TypeCode.SByte: return ic.ToSByte(null) < 0;
        case TypeCode.UInt16: return ic.ToUInt16(null) > short.MaxValue;
        case TypeCode.UInt32: return ic.ToUInt32(null) > int.MaxValue;
        case TypeCode.UInt64: return ic.ToUInt64(null) > long.MaxValue;
        default: return false;
      }
    }

    /// <summary>
    /// A language specific object that formats type names according to syntax of the language.
    /// </summary>
    public TypeNameFormatter TypeNameFormatter {
      get {
        if (this.typeNameFormatter == null)
          this.typeNameFormatter = this.CreateTypeNameFormatter();
        return this.typeNameFormatter;
      }
    }
    //^ [Once]
    TypeNameFormatter/*?*/ typeNameFormatter;

    /// <summary>
    /// Returns true if a CLR supplied implicit reference conversion is available to convert a value of the given source type to a corresponding value of the given target type.
    /// </summary>
    //^ [Pure]
    protected bool TypesAreClrAssignmentCompatible(ITypeDefinition sourceType, ITypeDefinition targetType) {
      if (TypeHelper.Type1DerivesFromOrIsTheSameAsType2(sourceType, targetType)) return true;
      if (TypeHelper.TypesAreEquivalent(targetType, this.PlatformType.SystemObject) && sourceType.IsInterface) return true;
      if (TypeHelper.Type1IsCovariantWithType2(sourceType, targetType)) return true;
      if (targetType.IsInterface && TypeHelper.Type1ImplementsType2(sourceType, targetType)) return true;
      if (this.ImplicitTypeParameterConversionExists(sourceType, targetType)) return true;
      if ((sourceType is IPointerTypeReference || sourceType is IFunctionPointerTypeReference) && targetType is IPointerTypeReference && 
        TypeHelper.TypesAreEquivalent(((IPointerTypeReference)targetType).TargetType.ResolvedType, targetType.PlatformType.SystemVoid)) return true;
      return false;
    }

    /// <summary>
    /// Returns an expression that extracts the value from a nullable wrapper at runtime. The expression fails at runtime if the wrapper wraps a null value.
    /// </summary>
    //^ [Pure]
    private Expression UnboxedNullable(Expression expression, ITypeDefinition unwrappedType, ITypeDefinition type)
      //^ requires unwrappedType != type;
      //^ requires unwrappedType == this.RemoveNullableWrapper(type);
    {
      //TODO: C# v2 does not call the conversion, but invokes get_Value. Find out if this matters.
      return this.UserDefinedConversion(expression, type, unwrappedType, true);
    }

    /// <summary>
    /// Returns the original method that has been specialized (perhaps more than once) and possibly instantiated
    /// to create the given method. Just returns the given method if no specialization or instantiation has occurred.
    /// </summary>
    //^ [Pure]
    protected virtual IMethodDefinition UninstantiateAndUnspecialize(IMethodDefinition method) {
      IGenericMethodInstance/*?*/ genericMethod = method as IGenericMethodInstance;
      if (genericMethod != null) method = genericMethod.GenericMethod.ResolvedMethod;
      ISpecializedMethodDefinition/*?*/ specializedMethod = method as ISpecializedMethodDefinition;
      if (specializedMethod != null) method = specializedMethod.UnspecializedVersion;
      return method;
    }

    /// <summary>
    /// Returns true if the field has (or should have) a compile time value that should be used in expressions whenever the field is referenced.
    /// For example, if field.IsCompileTimeConstant is true then the CLR mandates that the value should be used since the field will have no runtime memory associated with it.
    /// </summary>
    public virtual bool UseCompileTimeValueOfField(IFieldDefinition field) {
      return field.IsCompileTimeConstant;
    }

    /// <summary>
    /// Returns an expression that invokes a user defined conversion (possibly followed by a standard conversion) to convert
    /// the expression from sourceType to targetType. If no user defined conversion exists, it returns an instance of DummyExpression.
    /// </summary>
    //^ [Pure]
    protected Expression UserDefinedConversion(Expression expression, ITypeDefinition sourceType, ITypeDefinition targetType, bool isExplicitConversion) {
      Expression result;
      IMethodDefinition/*?*/ userConversion = this.UserDefinedConversion(sourceType, targetType, this.NameTable.OpImplicit);
      if (userConversion == null && isExplicitConversion)
        userConversion = this.UserDefinedConversion(sourceType, targetType, this.NameTable.OpExplicit);
      if (userConversion == null) {
        result = new DummyExpression(expression.SourceLocation);
        result.SetContainingExpression(expression);
      } else {
        List<Expression> arguments = new List<Expression>(1);
        arguments.Add(expression);
        Expression call = new MethodCall(expression, arguments.AsReadOnly(), expression.SourceLocation, userConversion);
        call.SetContainingExpression(expression);
        result = this.Conversion(call, targetType, false, false);
        if (call != result) result.SetContainingExpression(expression);
      }
      return result;
    }

    /// <summary>
    /// Returns the most specific user defined conversion operator that can be used to convert a value
    /// of the source type to a value of the target type. The given conversionName determines whether an explicit or implicit conversion is returned.
    /// </summary>
    //^ [Pure]
    protected virtual IMethodDefinition/*?*/ UserDefinedConversion(ITypeDefinition sourceType, ITypeDefinition targetType, IName conversionName) {
      ITypeDefinition/*?*/ mostSpecificSourceType = null;
      ITypeDefinition/*?*/ mostSpecificTargetType = null;
      IMethodDefinition/*?*/ mostSpecificConversion = null;
      ITypeDefinition sourceTypeOrBase = sourceType;
      while (!(sourceTypeOrBase is Dummy))
      //^ invariant mostSpecificSourceType == null <==> mostSpecificTargetType == null;
      {
        foreach (ITypeDefinitionMember member in sourceTypeOrBase.GetMembersNamed(conversionName, false))
        // ^ invariant mostSpecificSourceType == null <==> mostSpecificTargetType == null;
        {
          //^ assume mostSpecificSourceType == null <==> mostSpecificTargetType == null;
          IMethodDefinition/*?*/ conversion = member as IMethodDefinition;
          if (conversion == null || !IteratorHelper.EnumerableHasLength(conversion.Parameters, 1)) continue;
          ITypeDefinition conversionTargetType = conversion.Type.ResolvedType;
          if (TypeHelper.TypesAreEquivalent(conversionTargetType, targetType)) {
            mostSpecificSourceType = sourceTypeOrBase;
            mostSpecificTargetType = conversionTargetType;
            mostSpecificConversion = conversion;
            sourceTypeOrBase = Dummy.Type; //No base class of sourceTypeOrBase can define a visible conversion that is a better match with the target type
            break;
          }
          if (!this.ImplicitStandardConversionExists(conversionTargetType, targetType)) continue;
          if (mostSpecificSourceType == null) {
            mostSpecificSourceType = sourceTypeOrBase;
            mostSpecificTargetType = conversionTargetType;
            mostSpecificConversion = conversion;
            continue;
          }
          //^ assert mostSpecificTargetType != null;
          if (TypeHelper.Type1DerivesFromType2(conversionTargetType, mostSpecificTargetType)) {
            mostSpecificTargetType = conversionTargetType;
            if (mostSpecificSourceType != sourceTypeOrBase) {
              mostSpecificConversion = null; //A base type conversion gets closer to targetType than a conversion from a derived type. 
              //That eliminates all conversions from sourceType and its base classes since none of them can match both the most specific source and target types.
              sourceTypeOrBase = Dummy.Type;
              break;
            } else
              mostSpecificConversion = conversion;
          }
        }
        ITypeDefinition type = sourceTypeOrBase;
        sourceTypeOrBase = Dummy.Type;
        foreach (ITypeReference baseClassReference in type.BaseClasses) {
          sourceTypeOrBase = baseClassReference.ResolvedType;
          break;
        }
        //^ assume mostSpecificSourceType == null <==> mostSpecificTargetType == null;
      }
      foreach (ITypeDefinitionMember member in targetType.GetMembersNamed(conversionName, false))
      // ^ invariant mostSpecificSourceType == null <==> mostSpecificTargetType == null;
      {
        //^ assume mostSpecificSourceType == null <==> mostSpecificTargetType == null;
        IMethodDefinition/*?*/ conversion = member as IMethodDefinition;
        if (conversion == null) continue;
        ITypeDefinition/*?*/ parameterType = null;
        foreach (IParameterDefinition parameter in conversion.Parameters) {
          if (parameterType != null) { parameterType = null; break; } //Not a valid conversion
          parameterType = parameter.Type.ResolvedType;
        }
        if (parameterType == null || parameterType is Dummy) continue; //Not a valid conversion
        if (!this.ImplicitStandardConversionExists(sourceType, parameterType)) continue;
        if (mostSpecificSourceType == null || 
            TypeHelper.Type1DerivesFromType2(parameterType, mostSpecificSourceType) ||
            (this.ImplicitStandardConversionExists(parameterType, mostSpecificSourceType) && !this.ImplicitStandardConversionExists(mostSpecificSourceType, parameterType))) {
          mostSpecificSourceType = parameterType;
          mostSpecificTargetType = targetType;
          mostSpecificConversion = conversion;
          continue;
        }
        if (TypeHelper.Type1DerivesFromType2(mostSpecificSourceType, parameterType)) {
          //conversion cannot be selected, but it can still eliminate mostSpecificConversion.
          if (mostSpecificTargetType != targetType) {
            //mostSpecificConversion (if not null) comes from sourceType or one of its base types. 
            //mostSpecificConversion is closer to source type than conversion, but not as close to target type. Neither it nor conversion is best, so eliminate both.
            mostSpecificConversion = null;
            mostSpecificTargetType = targetType; //Make sure that future conversions are as good as both of the eliminated conversions
            continue;
          }
        } else {
          // parameterType and mostSpecificSourceType is equally specific
          if (mostSpecificTargetType == targetType) {
            if (this.ImplicitConversionExists(mostSpecificSourceType, parameterType) && !this.ImplicitConversionExists(parameterType, mostSpecificSourceType)) continue;
            // conversion and mostSpecificConversion are equally specific. That makes them ambiguous.
            mostSpecificConversion = null;
            continue;
          } else {
            mostSpecificConversion = conversion;
          }
        }
      }
      return mostSpecificConversion;
    }

  }

  /// <summary>
  /// An object that can provide information about the local scopes of a method.
  /// </summary>
  internal sealed class LocalScopeProvider : ILocalScopeProvider {

    internal static IEnumerable<ILocalScope> emptyLocalScopes = Enumerable<ILocalScope>.Empty;
    internal static IEnumerable<INamespaceScope> emptyNamespaceScopes = Enumerable<INamespaceScope>.Empty;
    internal static IEnumerable<ILocalDefinition> emptyLocals = Enumerable<ILocalDefinition>.Empty;

    /// <summary>
    /// Returns zero or more local (block) scopes, each defining an IL range in which an iterator local is defined.
    /// The scopes are returned by the MoveNext method of the object returned by the iterator method.
    /// The index of the scope corresponds to the index of the local. Specifically local scope i corresponds
    /// to the local stored in field &lt;localName&gt;x_i of the class used to store the local values in between
    /// calls to MoveNext.
    /// </summary>
    public IEnumerable<ILocalScope> GetIteratorScopes(IMethodBody methodBody) {
      var mbody = methodBody as Microsoft.Cci.MutableCodeModel.SourceMethodBody;
      if (mbody == null) return emptyLocalScopes;
      return mbody.IteratorScopes;
    }

    /// <summary>
    /// Returns zero or more local (block) scopes into which the CLR IL operations in the given method body is organized.
    /// </summary>
    public IEnumerable<ILocalScope> GetLocalScopes(IMethodBody methodBody) {
      var mbody = methodBody as MethodBody;
      if (mbody == null) return emptyLocalScopes;
      return mbody.GetLocalScopes();
    }

    /// <summary>
    /// Returns zero or more namespace scopes into which the namespace type containing the given method body has been nested.
    /// These scopes determine how simple names are looked up inside the method body. There is a separate scope for each dotted
    /// component in the namespace type name. For istance namespace type x.y.z will have two namespace scopes, the first is for the x and the second
    /// is for the y.
    /// </summary>
    public IEnumerable<INamespaceScope> GetNamespaceScopes(IMethodBody methodBody) {
      var mbody = methodBody as MethodBody;
      if (mbody == null) return emptyNamespaceScopes;
      return mbody.GetNamespaceScopes();
    }

    /// <summary>
    /// Returns zero or more local constant definitions that are local to the given scope.
    /// </summary>
    public IEnumerable<ILocalDefinition> GetConstantsInScope(ILocalScope scope) {
      var genScope = scope as ILGeneratorScope;
      if (genScope == null) return emptyLocals;
      return genScope.Constants;
    }

    /// <summary>
    /// Returns zero or more local variable definitions that are local to the given scope.
    /// </summary>
    public IEnumerable<ILocalDefinition> GetVariablesInScope(ILocalScope scope) {
      var genScope = scope as ILGeneratorScope;
      if (genScope == null) return emptyLocals;
      return genScope.Locals;
    }

    /// <summary>
    /// Returns true if the method body is an iterator.
    /// </summary>
    public bool IsIterator(IMethodBody methodBody) {
      var mbody = methodBody as MethodBody;
      if (mbody == null) return false;
      return mbody.IsIteratorBody;
    }

    /// <summary>
    /// If the given method body is the "MoveNext" method of the state class of an asynchronous method, the returned
    /// object describes where synchronization points occur in the IL operations of the "MoveNext" method. Otherwise
    /// the result is null.
    /// </summary>
    public ISynchronizationInformation/*?*/ GetSynchronizationInformation(IMethodBody methodBody) {
      return null;
    }
  }

  /// <summary>
  /// A collection of named members, with routines to search and maintain the collection. The search routines have linear complexity.
  /// Use this class when memory is at a premium and the expectation is that the scope will have a small number of members. 
  /// For example, a statement block scope should use this base class.
  /// </summary>
  /// <typeparam name="MemberType">The type of the members of this scope.</typeparam>
  public abstract class NonCachingScope<MemberType> : IScope<MemberType>
    where MemberType : class, IScopeMember<IScope<MemberType>> {

    /// <summary>
    /// Allocates an object that is a collection of named members, with routines to search the collection. The search routines have linear complexity.
    /// Use this class when memory is at a premium and the expectation is that the scope will have a small number of members. 
    /// For example, a statement block scope should use this base class.
    /// </summary>
    /// <param name="scopeMembers">To do get rid of this parameter.</param>
    protected NonCachingScope(IEnumerable<MemberType> scopeMembers) { //TODO: need to provide for lazy initialization
      this.members = scopeMembers;
    }

    /// <summary>
    /// Return true if the given member instance is a member of this scope.
    /// </summary>
    //^ [Pure]
    public bool Contains(MemberType/*!*/ member) {
      foreach (MemberType mem in this.members)
        if (member == mem) return true;
      return false;
    }

    /// <summary>
    /// Returns the list of members with the given name that also satisfy the given predicate.
    /// </summary>
    //^ [Pure]
    public IEnumerable<MemberType> GetMatchingMembersNamed(IName name, bool ignoreCase, Function<MemberType, bool> predicate) {
      int key = ignoreCase ? name.UniqueKeyIgnoringCase : name.UniqueKey;
      foreach (MemberType member in this.members) {
        IName mname = member.Name;
        int mkey = ignoreCase ? mname.UniqueKeyIgnoringCase : mname.UniqueKey;
        if (key == mkey && predicate(member)) yield return member;
      }
    }

    /// <summary>
    /// Returns the list of members with the given name that also satisfies the given predicate.
    /// </summary>
    //^ [Pure]
    public IEnumerable<MemberType> GetMatchingMembers(Function<MemberType, bool> predicate) {
      foreach (MemberType member in this.members)
        if (predicate(member)) yield return member;
    }

    /// <summary>
    /// Returns the list of members with the given name.
    /// </summary>
    //^ [Pure]
    public IEnumerable<MemberType> GetMembersNamed(IName name, bool ignoreCase) {
      int key = ignoreCase ? name.UniqueKeyIgnoringCase : name.UniqueKey;
      foreach (MemberType member in this.members) {
        IName mname = member.Name;
        int mkey = ignoreCase ? mname.UniqueKeyIgnoringCase : mname.UniqueKey;
        if (key == mkey) yield return member;
      }
    }

    /// <summary>
    /// The collection of member instances that are members of this scope.
    /// </summary>
    public IEnumerable<MemberType> Members {
      get { return this.members; }
    }
    IEnumerable<MemberType> members;

  }

  /// <summary>
  /// An object that has been derived from a portion of a source document.
  /// </summary>
  public abstract class SourceItem : ISourceItem, IObjectWithLocations {

    /// <summary>
    /// Initializes an object that has been derived from a portion of a source document.
    /// </summary>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated source item.</param>
    protected SourceItem(ISourceLocation sourceLocation) {
      this.sourceLocation = sourceLocation;
    }

    /// <summary>
    /// Calls the visitor.Visit(T) method where T is the most derived object model node interface type implemented by the concrete type
    /// of the object implementing IStatement. The dispatch method does not invoke Dispatch on any child objects. If child traversal
    /// is desired, the implementations of the Visit methods should do the subsequent dispatching.
    /// </summary>
    public virtual void Dispatch(ICodeVisitor visitor) {
    }

    /// <summary>
    /// Calls the visitor.Visit(T) method where T is the most derived subtype of this base type.
    /// The dispatch method does not invoke Dispatch on any child objects. If child traversal
    /// is desired, the implementations of the Visit methods should do the subsequent dispatching.
    /// </summary>
    public virtual void Dispatch(SourceVisitor visitor) {
    }

    /// <summary>
    /// A collection with exactly one element, namely this.SourceLocation.
    /// </summary>
    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetSingletonEnumerable<ILocation>(this.SourceLocation); }
    }

    /// <summary>
    /// The location in the source document that has been parsed to construct this item. 
    /// </summary>
    //^ [Pure]
    public ISourceLocation SourceLocation {
      get {
        SourceLocationBuilder/*?*/ bldr = this.sourceLocation as SourceLocationBuilder;
        if (bldr != null)
          this.sourceLocation = bldr.GetSourceLocation();
        return this.sourceLocation;
      }
    }
    /// <summary>
    /// The location in the source document that has been parsed to construct this item.
    /// </summary>
    protected ISourceLocation sourceLocation;

  }

  /// <summary>
  /// An object that can be checked for errors.
  /// </summary>
  public interface IErrorCheckable
  {

    /// <summary>
    /// Checks the object for errors and returns true if any have been found.
    /// </summary>
    bool HasErrors { get; }
  }

  /// <summary>
  /// An object that has been derived from a portion of a source document and can be checked for semantic errors.
  /// </summary>
  public abstract class CheckableSourceItem : SourceItem, IErrorCheckable {

    /// <summary>
    /// Initializes an object that has been derived from a portion of a source document and can be checked for semantic errors.
    /// </summary>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated source item.</param>
    public CheckableSourceItem(ISourceLocation sourceLocation)
      : base(sourceLocation) {
    }

    /// <summary>
    /// Checks the source item for errors and returns true if any were found.
    /// </summary>
    public bool HasErrors {
      get {
        if (this.hasErrors == null)
          this.hasErrors = this.CheckForErrorsAndReturnTrueIfAnyAreFound();
        return this.hasErrors.Value;
      }
    }
    /// <summary>
    /// Non null and true if this item has errors. Visible to derived classes so that it can be set during construction.
    /// When non null, the item has been checked and need not be checked again.
    /// </summary>
    protected bool? hasErrors;

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the item or a constituent part of the item.
    /// </summary>
    protected abstract bool CheckForErrorsAndReturnTrueIfAnyAreFound();
  }


  /// <summary>
  /// An object that has been derived from a portion of a source document and that can have custom attributes
  /// associated with it.
  /// </summary>
  public abstract class SourceItemWithAttributes : SourceItem {

    /// <summary>
    /// Initializes an object that has been derived from a portion of a source document and that can have custom attributes
    /// associated with it.
    /// </summary>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated source item.</param>
    protected SourceItemWithAttributes(ISourceLocation sourceLocation) 
      : base(sourceLocation) {
    }

    /// <summary>
    /// Custom attributes that are to be persisted in the metadata.
    /// </summary>
    public IEnumerable<ICustomAttribute> Attributes {
      get {
        if (this.attributes == null) {
          List<ICustomAttribute> attrs = this.GetAttributes();
          attrs.TrimExcess();
          this.attributes = attrs.AsReadOnly();
        }
        return this.attributes;
      }
    }
    IEnumerable<ICustomAttribute>/*?*/ attributes;

    /// <summary>
    /// Returns a list of custom attributes that describes this type declaration member.
    /// Typically, these will be derived from this.SourceAttributes. However, some source attributes
    /// might instead be persisted as metadata bits and other custom attributes may be synthesized
    /// from information not provided in the form of source custom attributes.
    /// The list is not trimmed to size, since an override of this method may call the base method
    /// and then add more attributes.
    /// </summary>
    protected abstract List<ICustomAttribute> GetAttributes();

  }

  /// <summary>
  /// An object that can map some kinds of ILocation objects to IPrimarySourceLocation objects.
  /// In this case, it primarily maps IDerivedSourceLocations and IIncludedSourceLocations to IPrimarySourceLocations.
  /// </summary>
  internal sealed class SourceLocationProvider : ISourceLocationProvider {

    public IEnumerable<IPrimarySourceLocation> GetPrimarySourceLocationsFor(IEnumerable<ILocation> locations) {
      foreach (ILocation location in locations) {
        foreach (IPrimarySourceLocation psloc in this.GetPrimarySourceLocationsFor(location))
          yield return psloc;
      }
    }

    public IEnumerable<IPrimarySourceLocation> GetPrimarySourceLocationsFor(ILocation location) {
      IPrimarySourceLocation/*?*/ psloc = location as IPrimarySourceLocation;
      if (psloc != null) {
        IIncludedSourceLocation /*?*/ iloc = psloc as IncludedSourceLocation;
        if (iloc != null)
          yield return new OriginalSourceLocation(iloc);
        else
          yield return psloc;
      } else {
        IDerivedSourceLocation/*?*/ dsloc = location as IDerivedSourceLocation;
        if (dsloc != null) {
          foreach (IPrimarySourceLocation psl in dsloc.PrimarySourceLocations) {
            IIncludedSourceLocation /*?*/ iloc = psl as IncludedSourceLocation;
            if (iloc != null)
              yield return new OriginalSourceLocation(iloc);
            else
              yield return psl;
          }
        }
      }
    }

    public IEnumerable<IPrimarySourceLocation> GetPrimarySourceLocationsForDefinitionOf(ILocalDefinition localDefinition) {
      LocalDefinition locDef = localDefinition as LocalDefinition;
      if (locDef != null)
        return this.GetPrimarySourceLocationsFor(locDef.LocalDeclaration.Name.SourceLocation);
      return Enumerable<IPrimarySourceLocation>.Empty;
    }

    public string GetSourceNameFor(ILocalDefinition localDefinition, out bool isCompilerGenerated) {
      isCompilerGenerated = localDefinition.Name is Dummy;
      return localDefinition.Name.Value;
    }

  }

  /// <summary>
  /// Implemented by classes that visit nodes of object graphs via a double dispatch mechanism, usually performing some computation of a subset of the nodes in the graph.
  /// Contains a specialized Visit routine for each standard type of object defined in this object model. 
  /// </summary>
  public abstract class SourceVisitor {
    /// <summary>
    /// Performs some computation with the given addition.
    /// </summary>
    public abstract void Visit(Addition addition);
    /// <summary>
    /// Performs some computation with the given addition assignment.
    /// </summary>
    public abstract void Visit(AdditionAssignment additionAssignment);
    /// <summary>
    /// Performs some computation with the given addressable expression.
    /// </summary>
    public abstract void Visit(AddressableExpression addressableExpression);
    /// <summary>
    /// Performs some computation with the given address dereference expression.
    /// </summary>
    public abstract void Visit(AddressDereference addressDerefence);
    /// <summary>
    /// Performs some computation with the given AddressOf expression.
    /// </summary>
    public abstract void Visit(AddressOf addressOf);
    /// <summary>
    /// Performs some computation with the given alias declaration.
    /// </summary>
    public abstract void Visit(AliasDeclaration aliasDeclaration);
    /// <summary>
    /// Performs some computation with the given alias qualified name.
    /// </summary>
    public abstract void Visit(AliasQualifiedName aliasQualifiedName);
    /// <summary>
    /// Performs some computation with the given anonymous method expression.
    /// </summary>
    public abstract void Visit(AnonymousMethod anonymousMethod);
    /// <summary>
    /// Performs some computation with the given attach event handler statement.
    /// </summary>
    public abstract void Visit(AttachEventHandlerStatement attachEventHandlerStatement);
    /// <summary>
    /// Performs some computation with the given attribute type expression.
    /// </summary>
    public abstract void Visit(AttributeTypeExpression attributeTypeExpression);
    /// <summary>
    /// Performs some computation with the given array type expression.
    /// </summary>
    public abstract void Visit(ArrayTypeExpression arrayTypeExpression);
    /// <summary>
    /// Performs some computation with the given assert statement.
    /// </summary>
    public abstract void Visit(AssertStatement assertStatement);
    /// <summary>
    /// Performs some computation with the given assignment expression.
    /// </summary>
    public abstract void Visit(Assignment assignment);
    /// <summary>
    /// Performs some computation with the given assume statement.
    /// </summary>
    public abstract void Visit(AssumeStatement assumeStatement);
    /// <summary>
    /// Performs some computation with the given base class reference expression.
    /// </summary>
    public abstract void Visit(BaseClassReference baseClassReference);
    /// <summary>
    /// Performs some computation with the given bitwise and expression.
    /// </summary>
    public abstract void Visit(BitwiseAnd bitwiseAnd);
    /// <summary>
    /// Performs some computation with the given bitwise and assignment.
    /// </summary>
    public abstract void Visit(BitwiseAndAssignment bitwiseAndAssignment);
    /// <summary>
    /// Performs some computation with the given bitwise or expression.
    /// </summary>
    public abstract void Visit(BitwiseOr bitwiseOr);
    /// <summary>
    /// Performs some computation with the given bitwise or assignment.
    /// </summary>
    public abstract void Visit(BitwiseOrAssignment bitwiseOrAssignment);
    /// <summary>
    /// Performs some computation with the given block expression.
    /// </summary>
    public abstract void Visit(BlockExpression blockExpression);
    /// <summary>
    /// Performs some computation with the given statement block.
    /// </summary>
    public abstract void Visit(BlockStatement block);
    /// <summary>
    /// Performs some computation with the cast-if-possible expression.
    /// </summary>
    public abstract void Visit(Cast cast);
    /// <summary>
    /// Performs some computation with the cast-if-possible expression.
    /// </summary>
    public abstract void Visit(CastIfPossible castIfPossible);
    /// <summary>
    /// Performs some computation with the given catch clause.
    /// </summary>
    public abstract void Visit(CatchClause catchClause);
    /// <summary>
    /// Performs some computation with the given checked expression.
    /// </summary>
    public abstract void Visit(CheckedExpression checkedExpression);
    /// <summary>
    /// Performs some computation with the given check-if-instance expression.
    /// </summary>
    public abstract void Visit(CheckIfInstance checkIfInstance);
    /// <summary>
    /// Performs some computation with the given clear last error statement(VB: On Error Goto -1).
    /// </summary>
    public abstract void Visit(ClearLastErrorStatement clearLastErrorStatement);
    /// <summary>
    /// Performs some computation with the given conversion expression.
    /// </summary>
    public abstract void Visit(Conversion conversion);
    /// <summary>
    /// Performs some computation with the given comma expression.
    /// </summary>
    public abstract void Visit(Comma comma);
    /// <summary>
    /// Performs some computation with the given conditional expression.
    /// </summary>
    public abstract void Visit(Conditional conditional);
    /// <summary>
    /// Performs some computation with the given compilation.
    /// </summary>
    public abstract void Visit(Compilation compilation);
    /// <summary>
    /// Performs some computation with the given compilation part.
    /// </summary>
    public abstract void Visit(CompilationPart compilationPart);
    /// <summary>
    /// Performs some computation with the given conditional statement.
    /// </summary>
    public abstract void Visit(ConditionalStatement conditionalStatement);
    /// <summary>
    /// Performs some computation with the given compile time constant.
    /// </summary>
    public abstract void Visit(CompileTimeConstant constant);
    /// <summary>
    /// Performs some computation with the given continue statement.
    /// </summary>
    public abstract void Visit(ContinueStatement continueStatement);
    /// <summary>
    /// Performs some computation with the anonymous object creation expression.
    /// </summary>
    public abstract void Visit(CreateAnonymousObject createAnonymousObject);
    /// <summary>
    /// Performs some computation with the given array creation expression.
    /// </summary>
    public abstract void Visit(CreateArray createArray);
    /// <summary>
    /// Performs some computation with the anonymous object creation expression.
    /// </summary>
    public abstract void Visit(CreateDelegateInstance createDelegateInstance);
    /// <summary>
    /// Performs some computation with the given implicitly typed array creation expression.
    /// </summary>
    public abstract void Visit(CreateImplicitlyTypedArray implicitlyTypedArrayCreate);
    /// <summary>
    /// Performs some computation with the given constructor call expression.
    /// </summary>
    public abstract void Visit(CreateObjectInstance createObjectInstance);
    /// <summary>
    /// Performs some computation with the given create stack array expression.
    /// </summary>
    public abstract void Visit(CreateStackArray stackArrayCreate);
    /// <summary>
    /// Performs some computation with the given indexer expression.
    /// </summary>
    public abstract void Visit(Indexer indexer);
    /// <summary>
    /// Performs some computation with the given defalut value expression.
    /// </summary>
    public abstract void Visit(DefaultValue defaultValue);
    /// <summary>
    /// Performs some computation with the given disable on error handler statement (VB: On Error Goto 0).
    /// </summary>
    public abstract void Visit(DisableOnErrorHandler disableOnErrorHandler);
    /// <summary>
    /// Performs some computation with the given division expression.
    /// </summary>
    public abstract void Visit(Division division);
    /// <summary>
    /// Performs some computation with the given division assignment.
    /// </summary>
    public abstract void Visit(DivisionAssignment divisionAssignment);
    /// <summary>
    /// Performs some computation with the given do while statement.
    /// </summary>
    public abstract void Visit(DoWhileStatement doWhileStatement);
    /// <summary>
    /// Performs some computation with the given documentation node.
    /// </summary>
    public abstract void Visit(Documentation documentation);
    /// <summary>
    /// Performs some computation with the given documenation attribute.
    /// </summary>
    public abstract void Visit(DocumentationAttribute documentationAttribute);
    /// <summary>
    /// Performs some computation with the given documenation element.
    /// </summary>
    public abstract void Visit(DocumentationElement documentationElement);
    /// <summary>
    /// Performs some computation with the given do until statement.
    /// </summary>
    public abstract void Visit(DoUntilStatement doUntilStatement);
    /// <summary>
    /// Performs some computation with the given empty statement.
    /// </summary>
    public abstract void Visit(EmptyStatement emptyStatement);
    /// <summary>
    /// Performs some computation with the given empty type expression.
    /// </summary>
    public abstract void Visit(EmptyTypeExpression emptyTypeExpression);
    /// <summary>
    /// Performs some computation with the given end statement.
    /// </summary>
    public abstract void Visit(EndStatement endStatement);
    /// <summary>
    /// Performs some computation with the given equality expression.
    /// </summary>
    public abstract void Visit(Equality equality);
    /// <summary>
    /// Performs some computation with the given erase statement.
    /// </summary>
    public abstract void Visit(EraseStatement eraseStatement);
    /// <summary>
    /// Performs some computation with the given error statement.
    /// </summary>
    public abstract void Visit(ErrorStatement errorStatement);
    /// <summary>
    /// Performs some computation with the given event declaration.
    /// </summary>
    public abstract void Visit(EventDeclaration eventDeclaration);
    /// <summary>
    /// Performs some computation with the given exclusive or expression.
    /// </summary>
    public abstract void Visit(ExclusiveOr exclusiveOr);
    /// <summary>
    /// Performs some computation with the given exclusive or assignment.
    /// </summary>
    public abstract void Visit(ExclusiveOrAssignment exclusiveOrAssignment);
    /// <summary>
    /// Performs some computation with the given break statement.
    /// </summary>
    public abstract void Visit(BreakStatement breakStatement);
    /// <summary>
    /// Performs some computation with the given exists expression.
    /// </summary>
    public abstract void Visit(Exists exists);
    /// <summary>
    /// Performs some computation with the given exponentiation expression.
    /// </summary>
    public abstract void Visit(Exponentiation exponentiation);
    /// <summary>
    /// Performs some computation with the given expression statement.
    /// </summary>
    public abstract void Visit(ExpressionStatement expressionStatement);
    /// <summary>
    /// Performs some computation with the given field declaration.
    /// </summary>
    public abstract void Visit(FieldDeclaration fieldDeclaration);
    /// <summary>
    /// Performs some computation with the given fixed statement.
    /// </summary>
    public abstract void Visit(FixedStatement fixedStatement);
    /// <summary>
    /// Performs some computation with the given forall expression.
    /// </summary>
    public abstract void Visit(Forall forall);
    /// <summary>
    /// Performs some computation with the given foreach statement.
    /// </summary>
    public abstract void Visit(ForEachStatement forEachStatement);
    /// <summary>
    /// Performs some computation with the given "for i in range" statement.
    /// </summary>
    public abstract void Visit(ForRangeStatement forRangeStatement);
    /// <summary>
    /// Performs some computation with the given for statement.
    /// </summary>
    public abstract void Visit(ForStatement forStatement);
    /// <summary>
    /// Performs some computation with the given generic instance expression.
    /// </summary>
    public abstract void Visit(GenericInstanceExpression genericInstanceExpression);
    /// <summary>
    /// Performs some computation with the given generic method parameter declaration.
    /// </summary>
    public abstract void Visit(GenericMethodParameterDeclaration genericMethodParameterDeclaration);
    /// <summary>
    /// Performs some computation with the given generic type instance expression.
    /// </summary>
    public abstract void Visit(GenericTypeInstanceExpression genericTypeInstanceExpression);
    /// <summary>
    /// Performs some computation with the given goto statement.
    /// </summary>
    public abstract void Visit(GotoStatement gotoStatement);
    /// <summary>
    /// Performs some computation with the given goto switch case statement.
    /// </summary>
    public abstract void Visit(GotoSwitchCaseStatement gotoSwitchCaseStatement);
    /// <summary>
    /// Performs some computation with the given generic instance.
    /// </summary>
    public abstract void Visit(GenericTypeInstance genericTypeInstance);
    /// <summary>
    /// Performs some computation with the given generic parameter declaration.
    /// </summary>
    public abstract void Visit(GenericTypeParameterDeclaration genericTypeParameterDeclaration);
    /// <summary>
    /// Performs some computation with the given get type of typed reference expression.
    /// </summary>
    public abstract void Visit(GetTypeOfTypedReference getTypeOfTypedReference);
    /// <summary>
    /// Performs some computation with the given get value of typed reference expression.
    /// </summary>
    public abstract void Visit(GetValueOfTypedReference getValueOfTypedReference);
    /// <summary>
    /// Performs some computation with the given greater-than expression.
    /// </summary>
    public abstract void Visit(GreaterThan greaterThan);
    /// <summary>
    /// Performs some computation with the given greater-than-or-equal expression.
    /// </summary>
    public abstract void Visit(GreaterThanOrEqual greaterThanOrEqual);
    /// <summary>
    /// Performs some computation with the given implicit qualifier expression.
    /// </summary>
    public abstract void Visit(ImplicitQualifier implicitQualifier);
    /// <summary>
    /// Performs some computation with the given implies expression.
    /// </summary>
    public abstract void Visit(Implies implies);
    /// <summary>
    /// Performs some computation with the given object initialization expression.
    /// </summary>
    public abstract void Visit(InitializeObject initializeObject);
    /// <summary>
    /// Performs some computation with the given integer division expression.
    /// </summary>
    public abstract void Visit(IntegerDivision integerDivision);
    /// <summary>
    /// Performs some computation with the given IsFalse expression.
    /// </summary>
    public abstract void Visit(IsFalse isFalse);
    /// <summary>
    /// Performs some computation with the given IsTrue expression.
    /// </summary>
    public abstract void Visit(IsTrue isTrue);
    /// <summary>
    /// Performs some computation with the given labeled statement.
    /// </summary>
    public abstract void Visit(LabeledStatement labeledStatement);
    /// <summary>
    /// Performs some computation with the given lambda expression.
    /// </summary>
    public abstract void Visit(Lambda lambda);
    /// <summary>
    /// Performs some computation with the given lambda parameter.
    /// </summary>
    public abstract void Visit(LambdaParameter lambdaParameter);
    /// <summary>
    /// Performs some computation with the given left shift expression.
    /// </summary>
    public abstract void Visit(LeftShift leftShift);
    /// <summary>
    /// Performs some computation with the given left shift assignment.
    /// </summary>
    public abstract void Visit(LeftShiftAssignment leftShiftAssignment);
    /// <summary>
    /// Performs some computation with the given less-than expression.
    /// </summary>
    public abstract void Visit(LessThan lessThan);
    /// <summary>
    /// Performs some computation with the given less-than-or-equal expression.
    /// </summary>
    public abstract void Visit(LessThanOrEqual lessThanOrEqual);
    /// <summary>
    /// Performs some computation with the given lifted conversion expression.
    /// </summary>
    public abstract void Visit(LiftedConversion liftedConversion);
    /// <summary>
    /// Performs some computation with the given like expression.
    /// </summary>
    public abstract void Visit(Like like);
    /// <summary>
    /// Performs some computation with the given local declaration.
    /// </summary>
    public abstract void Visit(LocalDeclaration localDeclaration);
    /// <summary>
    /// Performs some computation with the given local declarations statement.
    /// </summary>
    public abstract void Visit(LocalDeclarationsStatement localDeclarationsStatement);
    /// <summary>
    /// Performs some computation with the given lock statement.
    /// </summary>
    public abstract void Visit(LockStatement lockStatement);
    /// <summary>
    /// Performs some computation with the given logical and expression.
    /// </summary>
    public abstract void Visit(LogicalAnd logicalAnd);
    /// <summary>
    /// Performs some computation with the given logical not expression.
    /// </summary>
    public abstract void Visit(LogicalNot logicalNot);
    /// <summary>
    /// Performs some computation with the given logical or expression.
    /// </summary>
    public abstract void Visit(LogicalOr logicalOr);
    /// <summary>
    /// Performs some computation with the given loop invariant.
    /// </summary>
    public abstract void Visit(LoopInvariant loopInvariant);
    /// <summary>
    /// Performs some computation with the given make typed reference expression.
    /// </summary>
    public abstract void Visit(MakeTypedReference makeTypedReference);
    /// <summary>
    /// Performs some computation with the given managed pointer type.
    /// </summary>
    public abstract void Visit(ManagedPointerType managedPointerType);
    /// <summary>
    /// Performs some computation with the given managed pointer type.
    /// </summary>
    public abstract void Visit(ManagedPointerTypeExpression managedPointerTypeExpression);
    /// <summary>
    /// Performs some computation with the given method body.
    /// </summary>
    public abstract void Visit(MethodBody methodBody);
    /// <summary>
    /// Performs some computation with the given method call.
    /// </summary>
    public abstract void Visit(MethodCall methodCall);
    /// <summary>
    /// Performs some computation with the given method declaration.
    /// </summary>
    public abstract void Visit(MethodDeclaration methodDeclaration);
    /// <summary>
    /// Performs some computation with the given method group.
    /// </summary>
    public abstract void Visit(MethodGroup methodGroup);
    /// <summary>
    /// Performs some computation with the given modulus expression.
    /// </summary>
    public abstract void Visit(Modulus modulus);
    /// <summary>
    /// Performs some computation with the given modulus assignment.
    /// </summary>
    public abstract void Visit(ModulusAssignment modulusAssignment);
    /// <summary>
    /// Performs some computation with the given multiplication expression.
    /// </summary>
    public abstract void Visit(Multiplication multiplication);
    /// <summary>
    /// Performs some computation with the given multiplication assignment.
    /// </summary>
    public abstract void Visit(MultiplicationAssignment multiplicationAssignment);
    /// <summary>
    /// Performs some computation with the given named argument expression.
    /// </summary>
    public abstract void Visit(NamedArgument namedArgument);
    /// <summary>
    /// Performs some computation with the given name declaration.
    /// </summary>
    public abstract void Visit(NameDeclaration nameDeclaration);
    /// <summary>
    /// Performs some computation with the given named type expression.
    /// </summary>
    public abstract void Visit(NamedTypeExpression namedTypeExpression);
    /// <summary>
    /// Performs some computation with the given class declaration.
    /// </summary>
    public abstract void Visit(NamespaceClassDeclaration namespaceClassDeclaration);
    /// <summary>
    /// Performs some computation with the given namespace declaration.
    /// </summary>
    public abstract void Visit(NamespaceDeclaration unitNamespaceDeclaration);
    /// <summary>
    /// Performs some computation with the given delegate declaration.
    /// </summary>
    public abstract void Visit(NamespaceDelegateDeclaration delegateDeclaration);
    /// <summary>
    /// Performs some computation with the given enum declaration.
    /// </summary>
    public abstract void Visit(NamespaceEnumDeclaration enumDeclaration);
    /// <summary>
    /// Performs some computation with the given namespace import declaration.
    /// </summary>
    public abstract void Visit(NamespaceImportDeclaration namespaceImportDeclaration);
    /// <summary>
    /// Performs some computation with the given interface declaration.
    /// </summary>
    public abstract void Visit(NamespaceInterfaceDeclaration interfaceDeclaration);
    /// <summary>
    /// Performs some computation with the given namespace reference.
    /// </summary>
    public abstract void Visit(NamespaceReferenceExpression namespaceReferenceExpression);
    /// <summary>
    /// Performs some computation with the given struct declaration.
    /// </summary>
    public abstract void Visit(NamespaceStructDeclaration structDeclaration);
    /// <summary>
    /// Performs some computation with the given class declaration.
    /// </summary>
    public abstract void Visit(NestedClassDeclaration classDeclaration);
    /// <summary>
    /// Performs some computation with the given namespace declaration.
    /// </summary>
    public abstract void Visit(NestedNamespaceDeclaration nestedNamespaceDeclaration);
    /// <summary>
    /// Performs some computation with the given delegate declaration.
    /// </summary>
    public abstract void Visit(NestedDelegateDeclaration delegateDeclaration);
    /// <summary>
    /// Performs some computation with the given enum declaration.
    /// </summary>
    public abstract void Visit(NestedEnumDeclaration enumDeclaration);
    /// <summary>
    /// Performs some computation with the given interface declaration.
    /// </summary>
    public abstract void Visit(NestedInterfaceDeclaration interfaceDeclaration);
    /// <summary>
    /// Performs some computation with the given struct declaration.
    /// </summary>
    public abstract void Visit(NestedStructDeclaration structDeclaration);
    /// <summary>
    /// Performs some computation with the given non null type expression.
    /// </summary>
    public abstract void Visit(NonNullTypeExpression nonNullTypeExpression);
    /// <summary>
    /// Performs some computation with the given not equality expression.
    /// </summary>
    public abstract void Visit(NotEquality notEquality);
    /// <summary>
    /// Performs some computation with the given nullable type expression.
    /// </summary>
    public abstract void Visit(NullableTypeExpression nullableTypeExpression);
    /// <summary>
    /// Performs some computation with the given null coalescing expression.
    /// </summary>
    public abstract void Visit(NullCoalescing nullCoalescing);
    /// <summary>
    /// Performs some computation with the given On Error Goto statement.
    /// </summary>
    public abstract void Visit(OnErrorGotoStatement onErrorGotoStatement);
    /// <summary>
    /// Performs some computation with the given On Error Resume Next statement.
    /// </summary>
    public abstract void Visit(OnErrorResumeNextStatement onErrorResumeNextStatement);
    /// <summary>
    /// Performs some computation with the given one's complement expression.
    /// </summary>
    public abstract void Visit(OnesComplement onesComplement);
    /// <summary>
    /// Performs some computation with the given option declaration.
    /// </summary>
    public abstract void Visit(OptionDeclaration optionDeclaration);
    /// <summary>
    /// Performs some computation with the given out argument expression.
    /// </summary>
    public abstract void Visit(OutArgument outArgument);
    /// <summary>
    /// Performs some computation with the given parameter declaration.
    /// </summary>
    public abstract void Visit(ParameterDeclaration parameterDeclaration);
    /// <summary>
    /// Performs some computation with the given parenthesis expression.
    /// </summary>
    public abstract void Visit(Parenthesis parenthesis);
    /// <summary>
    /// Performs some computation with the given pointer type expression.
    /// </summary>
    public abstract void Visit(PointerTypeExpression pointerTypeExpression);
    /// <summary>
    /// Performs some computation with the given collection population expression.
    /// </summary>
    public abstract void Visit(PopulateCollection populateCollection);
    /// <summary>
    /// Performs some computation with the given postcondition.
    /// </summary>
    public abstract void Visit(Postcondition postcondition);
    /// <summary>
    /// Performs some computation with the given postfix decrement expression.
    /// </summary>
    public abstract void Visit(PostfixDecrement postfixDecrement);
    /// <summary>
    /// Performs some computation with the given postfix increment expression.
    /// </summary>
    public abstract void Visit(PostfixIncrement postfixIncrement);
    /// <summary>
    /// Performs some computation with the given precondition.
    /// </summary>
    public abstract void Visit(Precondition precondition);
    /// <summary>
    /// Performs some computation with the given prefix decrement expression.
    /// </summary>
    public abstract void Visit(PrefixDecrement prefixDecrement);
    /// <summary>
    /// Performs some computation with the given prefix increment expression.
    /// </summary>
    public abstract void Visit(PrefixIncrement prefixIncrement);
    /// <summary>
    /// Performs some computation with the given property declaration.
    /// </summary>
    public abstract void Visit(PropertyDeclaration propertyDeclaration);
    /// <summary>
    /// Performs some computation with the given property setter value expression.
    /// </summary>
    public abstract void Visit(PropertySetterValue propertySetterValue);
    /// <summary>
    /// Performs some computation with the given qualified name expression.
    /// </summary>
    public abstract void Visit(QualifiedName qualifiedName);
    /// <summary>
    /// Performs some computation with the given raise event statement.
    /// </summary>
    public abstract void Visit(RaiseEventStatement raiseEventStatement);
    /// <summary>
    /// Performs some computation with the given range expression.
    /// </summary>
    public abstract void Visit(Range range);
    /// <summary>
    /// Performs some computation with the given redimension clause.
    /// </summary>
    public abstract void Visit(RedimensionClause redimensionClause);
    /// <summary>
    /// Performs some computation with the given redimension statement.
    /// </summary>
    public abstract void Visit(RedimensionStatement redimensionStatement);
    /// <summary>
    /// Performs some computation with the given ref argument expression.
    /// </summary>
    public abstract void Visit(RefArgument refArgument);
    /// <summary>
    /// Performs some computation with the given reference equality expression.
    /// </summary>
    public abstract void Visit(ReferenceEquality referenceEquality);
    /// <summary>
    /// Performs some computation with the given reference inequality expression.
    /// </summary>
    public abstract void Visit(ReferenceInequality referenceInequality);
    /// <summary>
    /// Performs some computation with the given remove event handler statement.
    /// </summary>
    public abstract void Visit(RemoveEventHandlerStatement removeEventHandlerStatement);
    /// <summary>
    /// Performs some computation with the given resume labeled statement.
    /// </summary>
    public abstract void Visit(ResumeLabeledStatement resumeLabeledStatement);
    /// <summary>
    /// Performs some computation with the given resume next statement.
    /// </summary>
    public abstract void Visit(ResumeNextStatement resumeNextStatement);
    /// <summary>
    /// Performs some computation with the given resume statement.
    /// </summary>
    public abstract void Visit(ResumeStatement resumeStatement);
    /// <summary>
    /// Performs some computation with the given resource use statement.
    /// This statement corresponds to the using(IDisposable ob = ...){...} in C#.
    /// </summary>
    public abstract void Visit(ResourceUseStatement resourceUseStatement);
    /// <summary>
    /// Performs some computation with the rethrow statement.
    /// </summary>
    public abstract void Visit(RethrowStatement rethrowStatement);
    /// <summary>
    /// Performs some computation with the return statement.
    /// </summary>
    public abstract void Visit(ReturnStatement returnStatement);
    /// <summary>
    /// Performs some computation with the given return value expression.
    /// </summary>
    public abstract void Visit(ReturnValue returnValue);
    /// <summary>
    /// Performs some computation with the given right shift expression.
    /// </summary>
    public abstract void Visit(RightShift rightShift);
    /// <summary>
    /// Performs some computation with the given right shift assignment.
    /// </summary>
    public abstract void Visit(RightShiftAssignment rightShiftAssignment);
    /// <summary>
    /// Performs some computation with the given unsigned right shift expression.
    /// </summary>
    public abstract void Visit(UnsignedRightShift unsignedRightShift);
    /// <summary>
    /// Performs some computation with the given root namespace expression.
    /// </summary>
    public abstract void Visit(RootNamespaceExpression rootNamespaceExpression);
    /// <summary>
    /// Performs some computation with the given runtime argument handle expression.
    /// </summary>
    public abstract void Visit(RuntimeArgumentHandleExpression runtimeArgumentHandleExpression);
    /// <summary>
    /// Performs some computation with the given signature declaration.
    /// </summary>
    public abstract void Visit(SignatureDeclaration signatureDeclaration);
    /// <summary>
    /// Performs some computation with the given simple name.
    /// </summary>
    public abstract void Visit(SimpleName simpleName);
    /// <summary>
    /// Performs some computation with the given sizeof() expression.
    /// </summary>
    public abstract void Visit(SizeOf sizeOf);
    /// <summary>
    /// Performs some computation with the given slice expression.
    /// </summary>
    public abstract void Visit(Slice slice);
    /// <summary>
    /// Performs some computation with the given source custom attribute.
    /// </summary>
    public abstract void Visit(SourceCustomAttribute sourceCustomAttribute);
    /// <summary>
    /// Performs some computation with the given stop statement.
    /// </summary>
    public abstract void Visit(StopStatement stopStatement);
    /// <summary>
    /// Performs some computation with the given string concatenation expression.
    /// </summary>
    public abstract void Visit(StringConcatenation stringConcatenation);
    /// <summary>
    /// Performs some computation with the given subtraction expression.
    /// </summary>
    public abstract void Visit(Subtraction subtraction);
    /// <summary>
    /// Performs some computation with the given subtraction assignment.
    /// </summary>
    public abstract void Visit(SubtractionAssignment subtractionAssignment);
    /// <summary>
    /// Performs some computation with the given switch case.
    /// </summary>
    public abstract void Visit(SwitchCase switchCase);
    /// <summary>
    /// Performs some computation with the given switch statement.
    /// </summary>
    public abstract void Visit(SwitchStatement switchStatement);
    /// <summary>
    /// Performs some computation with the given target expression.
    /// </summary>
    public abstract void Visit(TargetExpression targetExpression);
    /// <summary>
    /// Performs some computation with the given this reference expression.
    /// </summary>
    public abstract void Visit(ThisReference thisReference);
    /// <summary>
    /// Performs some computation with the thrown exception.
    /// </summary>
    public abstract void Visit(ThrownException thrownException);
    /// <summary>
    /// Performs some computation with the throw statement.
    /// </summary>
    public abstract void Visit(ThrowStatement throwStatement);
    /// <summary>
    /// Performs some computation with the try-catch-filter-finally statement.
    /// </summary>
    public abstract void Visit(TryCatchFinallyStatement tryCatchFilterFinallyStatement);
    /// <summary>
    /// Performs some computation with the given type declaration.
    /// </summary>
    public abstract void Visit(TypeDeclaration typeDeclaration);
    /// <summary>
    /// Performs some computation with the given type invariant.
    /// </summary>
    public abstract void Visit(TypeInvariant typeInvariant);
    /// <summary>
    /// Performs some computation with the given typeof() expression.
    /// </summary>
    public abstract void Visit(TypeOf typeOf);
    /// <summary>
    /// Performs some computation with the given unary negation expression.
    /// </summary>
    public abstract void Visit(UnaryNegation unaryNegation);
    /// <summary>
    /// Performs some computation with the given unary plus expression.
    /// </summary>
    public abstract void Visit(UnaryPlus unaryPlus);
    /// <summary>
    /// Performs some computation with the given unchecked expression.
    /// </summary>
    public abstract void Visit(UncheckedExpression uncheckedExpression);
    /// <summary>
    /// Performs some computation with the given unit set alias.
    /// </summary>
    public abstract void Visit(UnitSetAliasDeclaration unitSetAliasDeclaration);
    /// <summary>
    /// Performs some computation with the given until do statement.
    /// </summary>
    public abstract void Visit(UntilDoStatement untilDoStatement);
    /// <summary>
    /// Performs some computation with the given while do statement.
    /// </summary>
    public abstract void Visit(WhileDoStatement whileDoStatement);
    /// <summary>
    /// Performs some computation with the given with statement.
    /// </summary>
    public abstract void Visit(WithStatement withStatement);
    /// <summary>
    /// Performs some computation with the given yield break statement.
    /// </summary>
    public abstract void Visit(YieldBreakStatement yieldBreakStatement);
    /// <summary>
    /// Performs some computation with the given yield return statement.
    /// </summary>
    public abstract void Visit(YieldReturnStatement yieldReturnStatement);
  }

  /// <summary>
  /// A class that contains a specialized Visit routine for each standard type of object defined in this object model. 
  /// Each of these visit routines in turn dispatches the visitor on each of the child objects of the corresponding node.
  /// </summary>
  public class SourceTraverser : SourceVisitor {

    /// <summary>
    /// Allocates an object with methods that contains a specialized Visit routine for each standard type of object defined in this object model. 
    /// Each of these visit routines in turn dispatches the visitor on each of the child objects of the corresponding node.
    /// </summary>
    public SourceTraverser() {
    }

    /// <summary>
    /// The path from the graph root to the current node being visited.
    /// </summary>
    //^ [SpecPublic]
    protected System.Collections.Stack path = new System.Collections.Stack();

    /// <summary>
    /// If true, the object of the traversal has been achieved and the traversal should terminate as quickly as possible.
    /// </summary>
    protected bool stopTraversal;

    /// <summary>
    /// If true, the traversal should include method bodies.
    /// </summary>
    protected bool visitMethodBodies;

    /// <summary>
    /// Performs some computation with the given addition expression.
    /// </summary>
    public override void Visit(Addition addition)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(addition);
      this.VisitExpression(addition.LeftOperand);
      this.VisitExpression(addition.RightOperand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given addition assignment expression.
    /// </summary>
    public override void Visit(AdditionAssignment additionAssignment)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(additionAssignment);
      this.Visit(additionAssignment.LeftOperand);
      this.VisitExpression(additionAssignment.RightOperand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given addressable expression.
    /// </summary>
    public override void Visit(AddressableExpression addressableExpression)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(addressableExpression);
      this.VisitExpression(addressableExpression.Expression);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given address derefence expression.
    /// </summary>
    public override void Visit(AddressDereference addressDerefence)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(addressDerefence);
      this.VisitExpression(addressDerefence.Address);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given address of expression.
    /// </summary>
    public override void Visit(AddressOf addressOf)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(addressOf);
      this.Visit(addressOf.Address);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given alias declaration.
    /// </summary>
    public override void Visit(AliasDeclaration aliasDeclaration)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(aliasDeclaration);
      this.VisitExpression(aliasDeclaration.ReferencedNamespaceOrType);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given alias qualified Name.
    /// </summary>
    public override void Visit(AliasQualifiedName aliasQualifiedName)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(aliasQualifiedName);
      this.VisitExpression(aliasQualifiedName.Alias);
      this.VisitExpression(aliasQualifiedName.SimpleName);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given anonymous method.
    /// </summary>
    public override void Visit(AnonymousMethod anonymousMethod)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    /// <summary>
    /// Performs some computation with the given create array expression.
    /// </summary>
    public override void Visit(CreateArray createArray)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(createArray);
      this.VisitTypeExpression(createArray.ElementTypeExpression);
      this.Visit(createArray.LowerBounds);
      this.Visit(createArray.Sizes);
      this.Visit(createArray.Initializers);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given array type expression.
    /// </summary>
    public override void Visit(ArrayTypeExpression arrayTypeExpression)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(arrayTypeExpression);
      this.VisitTypeExpression(arrayTypeExpression.ElementType);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given assert statement.
    /// </summary>
    public override void Visit(AssertStatement assertStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(assertStatement);
      this.VisitExpression(assertStatement.Condition);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given assignment expression.
    /// </summary>
    public override void Visit(Assignment assignment)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(assignment);
      this.Visit(assignment.Target);
      this.VisitExpression(assignment.Source);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given assume statement.
    /// </summary>
    public override void Visit(AssumeStatement assumeStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(assumeStatement);
      this.VisitExpression(assumeStatement.Condition);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given attribute type expression.
    /// </summary>
    /// <param name="attributeTypeExpression"></param>
    public override void Visit(AttributeTypeExpression attributeTypeExpression)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(attributeTypeExpression);
      this.VisitExpression(attributeTypeExpression.Expression);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given attach event handler statement.
    /// </summary>
    public override void Visit(AttachEventHandlerStatement attachEventHandlerStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(attachEventHandlerStatement);
      this.VisitExpression(attachEventHandlerStatement.Event);
      this.VisitExpression(attachEventHandlerStatement.Handler);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given base class reference expression.
    /// </summary>
    public override void Visit(BaseClassReference baseClassReference)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    /// <summary>
    /// Performs some computation with the given bitwise and expression.
    /// </summary>
    public override void Visit(BitwiseAnd bitwiseAnd)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(bitwiseAnd);
      this.VisitExpression(bitwiseAnd.LeftOperand);
      this.VisitExpression(bitwiseAnd.RightOperand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given bitwise and assignment expression.
    /// </summary>
    public override void Visit(BitwiseAndAssignment bitwiseAndAssignment)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(bitwiseAndAssignment);
      this.Visit(bitwiseAndAssignment.LeftOperand);
      this.VisitExpression(bitwiseAndAssignment.RightOperand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given bitwise or expression.
    /// </summary>
    public override void Visit(BitwiseOr bitwiseOr)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(bitwiseOr);
      this.VisitExpression(bitwiseOr.LeftOperand);
      this.VisitExpression(bitwiseOr.RightOperand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given bitwise or assignment expression.
    /// </summary>
    public override void Visit(BitwiseOrAssignment bitwiseOrAssignment)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(bitwiseOrAssignment);
      this.Visit(bitwiseOrAssignment.LeftOperand);
      this.VisitExpression(bitwiseOrAssignment.RightOperand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given block expression.
    /// </summary>
    public override void Visit(BlockExpression blockExpression)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(blockExpression);
      this.VisitStatement(blockExpression.BlockStatement);
      this.VisitExpression(blockExpression.Expression);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given block.
    /// </summary>
    public override void Visit(BlockStatement block)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(block);
      this.Visit(block.Statements);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given cast expression.
    /// </summary>
    public override void Visit(Cast cast)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(cast);
      this.VisitExpression(cast.TargetType);
      this.VisitExpression(cast.ValueToCast);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given cast if possible expression.
    /// </summary>
    public override void Visit(CastIfPossible castIfPossible)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(castIfPossible);
      this.VisitExpression(castIfPossible.TargetType);
      this.VisitExpression(castIfPossible.ValueToCast);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given catch clauses.
    /// </summary>
    public virtual void Visit(IEnumerable<CatchClause> catchClauses)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      foreach (CatchClause catchClause in catchClauses)
        this.Visit(catchClause);
    }

    /// <summary>
    /// Performs some computation with the given catch clause.
    /// </summary>
    public override void Visit(CatchClause catchClause)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(catchClause);
      if (catchClause.FilterCondition != null)
        this.VisitExpression(catchClause.FilterCondition);
      this.VisitStatement(catchClause.Body);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given checked expression.
    /// </summary>
    public override void Visit(CheckedExpression checkedExpression) {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(checkedExpression);
      this.VisitExpression(checkedExpression.Operand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given check if instance expression.
    /// </summary>
    public override void Visit(CheckIfInstance checkIfInstance)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(checkIfInstance);
      this.VisitExpression(checkIfInstance.Operand);
      this.VisitExpression(checkIfInstance.TypeToCheck);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given clear last error statement.
    /// </summary>
    public override void Visit(ClearLastErrorStatement clearLastErrorStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(clearLastErrorStatement);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given conversion.
    /// </summary>
    public override void Visit(Conversion conversion)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(conversion);
      this.VisitExpression(conversion.ValueToConvert);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given comma expression.
    /// </summary>
    public override void Visit(Comma comma)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(comma);
      this.VisitExpression(comma.LeftOperand);
      this.VisitExpression(comma.RightOperand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given compilation.
    /// </summary>
    public override void Visit(Compilation compilation)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(compilation);
      this.Visit(compilation.Parts);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given compilation parts.
    /// </summary>
    public virtual void Visit(IEnumerable<CompilationPart> compilationParts)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      foreach (CompilationPart compilationPart in compilationParts)
        this.Visit(compilationPart);
    }

    /// <summary>
    /// Performs some computation with the given compilation part.
    /// </summary>
    public override void Visit(CompilationPart compilationPart)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(compilationPart);
      this.Visit(compilationPart.RootNamespace);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given constant.
    /// </summary>
    public override void Visit(CompileTimeConstant constant)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    /// <summary>
    /// Performs some computation with the given conditional expression.
    /// </summary>
    public override void Visit(Conditional conditional)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(conditional);
      this.VisitExpression(conditional.Condition);
      this.VisitExpression(conditional.ResultIfTrue);
      this.VisitExpression(conditional.ResultIfFalse);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given conditional statement.
    /// </summary>
    public override void Visit(ConditionalStatement conditionalStatement)
      //^ ensures this.path.Count == old(this.path.Count);
   {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(conditionalStatement);
      this.VisitExpression(conditionalStatement.Condition);
      this.VisitStatement(conditionalStatement.TrueBranch);
      this.VisitStatement(conditionalStatement.FalseBranch);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given create object instance expression.
    /// </summary>
    public override void Visit(CreateObjectInstance createObjectInstance)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(createObjectInstance);
      this.VisitExpression(createObjectInstance.ObjectType);
      this.Visit(createObjectInstance.ConvertedArguments);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given continue statement.
    /// </summary>
    public override void Visit(ContinueStatement continueStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(continueStatement);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given create anonymous object expression.
    /// </summary>
    public override void Visit(CreateAnonymousObject createAnonymousObject)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(createAnonymousObject);
      this.Visit(createAnonymousObject.Initializers);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given create delegate instance expression.
    /// </summary>
    public override void Visit(CreateDelegateInstance createDelegateInstance)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(createDelegateInstance);
      if (createDelegateInstance.Instance != null)
        this.VisitExpression(createDelegateInstance.Instance);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given implicitly typed array create expression.
    /// </summary>
    public override void Visit(CreateImplicitlyTypedArray implicitlyTypedArrayCreate)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(implicitlyTypedArrayCreate);
      this.Visit(implicitlyTypedArrayCreate.Initializers);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given documentation.
    /// </summary>
    public override void Visit(Documentation documentation)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(documentation);
      this.Visit(documentation.Elements);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given documentation attributes.
    /// </summary>
    public virtual void Visit(IEnumerable<DocumentationAttribute> documentationAttributes)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      foreach (DocumentationAttribute documentationAttribute in documentationAttributes)
        this.Visit(documentationAttribute);
    }

    /// <summary>
    /// Performs some computation with the given documentation attribute.
    /// </summary>
    public override void Visit(DocumentationAttribute documentationAttribute) {
    }

    /// <summary>
    /// Performs some computation with the given documentation elements.
    /// </summary>
    public virtual void Visit(IEnumerable<DocumentationElement> documentationElements)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      foreach (DocumentationElement documentationElement in documentationElements)
        this.Visit(documentationElement);
    }

    /// <summary>
    /// Performs some computation with the given documentation element.
    /// </summary>
    public override void Visit(DocumentationElement documentationElement) {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(documentationElement);
      this.Visit(documentationElement.Attributes);
      this.Visit(documentationElement.Children);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given default value expression.
    /// </summary>
    public override void Visit(DefaultValue defaultValue)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(defaultValue);
      this.VisitExpression(defaultValue.DefaultValueType);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given disable on error handler expression.
    /// </summary>
    public override void Visit(DisableOnErrorHandler disableOnErrorHandler)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(disableOnErrorHandler);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given division expression.
    /// </summary>
    public override void Visit(Division division)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(division);
      this.VisitExpression(division.LeftOperand);
      this.VisitExpression(division.RightOperand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given division assignment expression.
    /// </summary>
    public override void Visit(DivisionAssignment divisionAssignment)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(divisionAssignment);
      this.Visit(divisionAssignment.LeftOperand);
      this.VisitExpression(divisionAssignment.RightOperand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given do until statement.
    /// </summary>
    public override void Visit(DoUntilStatement doUntilStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(doUntilStatement);
      this.VisitStatement(doUntilStatement.Body);
      this.VisitExpression(doUntilStatement.Condition);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given do while statement.
    /// </summary>
    public override void Visit(DoWhileStatement doWhileStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(doWhileStatement);
      this.VisitStatement(doWhileStatement.Body);
      this.VisitExpression(doWhileStatement.Condition);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given empty statement.
    /// </summary>
    public override void Visit(EmptyStatement emptyStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(emptyStatement);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given empty type expression expression.
    /// </summary>
    public override void Visit(EmptyTypeExpression emptyTypeExpression)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    /// <summary>
    /// Performs some computation with the given end statement.
    /// </summary>
    public override void Visit(EndStatement endStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(endStatement);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given equality expression.
    /// </summary>
    public override void Visit(Equality equality)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(equality);
      this.VisitExpression(equality.LeftOperand);
      this.VisitExpression(equality.RightOperand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given erase statement.
    /// </summary>
    public override void Visit(EraseStatement eraseStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(eraseStatement);
      this.Visit(eraseStatement.Targets);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given error statement.
    /// </summary>
    public override void Visit(ErrorStatement errorStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(errorStatement);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given event declaration.
    /// </summary>
    public override void Visit(EventDeclaration eventDeclaration)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(eventDeclaration);
      this.Visit(eventDeclaration.SourceAttributes);
      this.Visit(eventDeclaration.AdderAttributes);
      if (eventDeclaration.AdderBody != null)
        this.Visit(eventDeclaration.AdderBody);
      if (eventDeclaration.Caller != null)
        this.VisitTypeDeclarationMember(eventDeclaration.Caller);
      this.Visit(eventDeclaration.RemoverAttributes);
      if (eventDeclaration.RemoverBody != null)
        this.Visit(eventDeclaration.RemoverBody);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given exclusive or expression.
    /// </summary>
    public override void Visit(ExclusiveOr exclusiveOr)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(exclusiveOr);
      this.VisitExpression(exclusiveOr.LeftOperand);
      this.VisitExpression(exclusiveOr.RightOperand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given exclusive or assignment expression.
    /// </summary>
    public override void Visit(ExclusiveOrAssignment exclusiveOrAssignment)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(exclusiveOrAssignment);
      this.Visit(exclusiveOrAssignment.LeftOperand);
      this.VisitExpression(exclusiveOrAssignment.RightOperand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given break statement.
    /// </summary>
    public override void Visit(BreakStatement breakStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(breakStatement);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given exists expression.
    /// </summary>
    public override void Visit(Exists exists)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(exists);
      this.Visit(exists.BoundVariables);
      this.VisitExpression(exists.Condition);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given exponentiation expression.
    /// </summary>
    public override void Visit(Exponentiation exponentiation)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(exponentiation);
      this.VisitExpression(exponentiation.LeftOperand);
      this.VisitExpression(exponentiation.RightOperand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given expressions.
    /// </summary>
    public virtual void Visit(IEnumerable<Expression> expressions)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      foreach (Expression expression in expressions)
        this.VisitExpression(expression);
    }

    /// <summary>
    /// Performs some computation with the given expression.
    /// </summary>
    public virtual void VisitExpression(Expression expression) {
      if (this.stopTraversal) return;
      expression.Dispatch(this);
    }

    /// <summary>
    /// Performs some computation with the given expression statement.
    /// </summary>
    public override void Visit(ExpressionStatement expressionStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(expressionStatement);
      this.VisitExpression(expressionStatement.Expression);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    //public override void Visit(ExtensionMethod extensionMethod)
    //  //^ ensures this.path.Count == old(this.path.Count);
    //{
    //  if (this.stopTraversal) return;
    //  //^ int oldCount = this.path.Count;
    //  this.path.Push(extensionMethod);
    //  this.Visit(extensionMethod.Method);
    //  //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
    //  this.path.Pop();
    //}

    /// <summary>
    /// Performs some computation with the given field declaration.
    /// </summary>
    public override void Visit(FieldDeclaration fieldDeclaration)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(fieldDeclaration);
      this.Visit(fieldDeclaration.SourceAttributes);
      if (fieldDeclaration.Initializer != null)
        this.VisitExpression(fieldDeclaration.Initializer);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given fixed statement.
    /// </summary>
    public override void Visit(FixedStatement fixedStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(fixedStatement);
      this.VisitStatement(fixedStatement.FixedPointerDeclarators);
      this.VisitStatement(fixedStatement.Body);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given forall expression.
    /// </summary>
    public override void Visit(Forall forall)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(forall);
      this.Visit(forall.BoundVariables);
      this.VisitExpression(forall.Condition);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given for each statement.
    /// </summary>
    public override void Visit(ForEachStatement forEachStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(forEachStatement);
      this.VisitExpression(forEachStatement.VariableType);
      this.Visit(forEachStatement.VariableName);
      this.VisitExpression(forEachStatement.Collection);
      this.VisitStatement(forEachStatement.Body);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given for range statement.
    /// </summary>
    public override void Visit(ForRangeStatement forRangeStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(forRangeStatement);
      this.VisitExpression(forRangeStatement.VariableName);
      this.VisitExpression(forRangeStatement.Range);
      if (forRangeStatement.Step != null)
        this.VisitExpression(forRangeStatement.Step);
      this.VisitStatement(forRangeStatement.Body);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given for statement.
    /// </summary>
    public override void Visit(ForStatement forStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(forStatement);
      this.Visit(forStatement.InitStatements);
      this.VisitExpression(forStatement.Condition);
      this.Visit(forStatement.IncrementStatements);
      this.VisitStatement(forStatement.Body);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    //public override void Visit(FunctionPointer functionPointer)
    //  //^ ensures this.path.Count == old(this.path.Count);
    //{
    //}

    /// <summary>
    /// Performs some computation with the given generic instance expression.
    /// </summary>
    public override void Visit(GenericInstanceExpression genericInstanceExpression)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(genericInstanceExpression);
      this.VisitExpression(genericInstanceExpression.GenericTypeOrMethod);
      this.Visit(genericInstanceExpression.ArgumentTypes);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given generic method parameter declarations.
    /// </summary>
    public virtual void Visit(IEnumerable<GenericMethodParameterDeclaration> genericMethodParameterDeclarations)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      foreach (GenericMethodParameterDeclaration genericMethodParameterDeclaration in genericMethodParameterDeclarations)
        this.Visit(genericMethodParameterDeclaration);
    }

    /// <summary>
    /// Performs some computation with the given generic method parameter declaration.
    /// </summary>
    public override void Visit(GenericMethodParameterDeclaration genericMethodParameterDeclaration)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(genericMethodParameterDeclaration);
      this.Visit(genericMethodParameterDeclaration.Constraints);
      this.Visit(genericMethodParameterDeclaration.SourceAttributes);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given generic type instance expression.
    /// </summary>
    public override void Visit(GenericTypeInstanceExpression genericTypeInstanceExpression)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(genericTypeInstanceExpression);
      this.VisitExpression(genericTypeInstanceExpression.GenericType);
      this.Visit(genericTypeInstanceExpression.ArgumentTypes);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given generic type instance.
    /// </summary>
    public override void Visit(GenericTypeInstance genericTypeInstance)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    /// <summary>
    /// Performs some computation with the given generic type parameter declarations.
    /// </summary>
    public virtual void Visit(IEnumerable<GenericTypeParameterDeclaration> genericTypeParameterDeclarations)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      foreach (GenericTypeParameterDeclaration genericTypeParameterDeclaration in genericTypeParameterDeclarations)
        this.Visit(genericTypeParameterDeclaration);
    }

    /// <summary>
    /// Performs some computation with the given generic type parameter declaration.
    /// </summary>
    public override void Visit(GenericTypeParameterDeclaration genericTypeParameterDeclaration)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(genericTypeParameterDeclaration);
      this.Visit(genericTypeParameterDeclaration.Constraints);
      this.Visit(genericTypeParameterDeclaration.SourceAttributes);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given get type of typed reference expression.
    /// </summary>
    public override void Visit(GetTypeOfTypedReference getTypeOfTypedReference)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(getTypeOfTypedReference);
      this.VisitExpression(getTypeOfTypedReference.TypedReference);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given get value of typed reference expression.
    /// </summary>
    public override void Visit(GetValueOfTypedReference getValueOfTypedReference)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(getValueOfTypedReference);
      this.VisitExpression(getValueOfTypedReference.TypedReference);
      this.VisitExpression(getValueOfTypedReference.TargetType);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given goto statement.
    /// </summary>
    public override void Visit(GotoStatement gotoStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(gotoStatement);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given goto switch case statement.
    /// </summary>
    public override void Visit(GotoSwitchCaseStatement gotoSwitchCaseStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    /// <summary>
    /// Performs some computation with the given greater than expression.
    /// </summary>
    public override void Visit(GreaterThan greaterThan)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(greaterThan);
      this.VisitExpression(greaterThan.LeftOperand);
      this.VisitExpression(greaterThan.RightOperand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given greater than or equal expression.
    /// </summary>
    public override void Visit(GreaterThanOrEqual greaterThanOrEqual)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(greaterThanOrEqual);
      this.VisitExpression(greaterThanOrEqual.LeftOperand);
      this.VisitExpression(greaterThanOrEqual.RightOperand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given implicit qualifier expression.
    /// </summary>
    public override void Visit(ImplicitQualifier implicitQualifier)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    /// <summary>
    /// Performs some computation with the given implies expression.
    /// </summary>
    public override void Visit(Implies implies)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(implies);
      this.VisitExpression(implies.LeftOperand);
      this.VisitExpression(implies.RightOperand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given indexer.
    /// </summary>
    public override void Visit(Indexer indexer)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(indexer);
      this.VisitExpression(indexer.IndexedObject);
      this.Visit(indexer.ConvertedArguments);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given initialize object expression.
    /// </summary>
    public override void Visit(InitializeObject initializeObject)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(initializeObject);
      this.VisitExpression(initializeObject.ObjectToInitialize);
      this.Visit(initializeObject.NamedArguments);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.or operation is not implemented.");
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given integer division expression.
    /// </summary>
    public override void Visit(IntegerDivision integerDivision)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(integerDivision);
      this.VisitExpression(integerDivision.LeftOperand);
      this.VisitExpression(integerDivision.RightOperand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given is false expression.
    /// </summary>
    public override void Visit(IsFalse isFalse)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(isFalse);
      this.VisitExpression(isFalse.Operand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given is true expression.
    /// </summary>
    public override void Visit(IsTrue isTrue)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(isTrue);
      this.VisitExpression(isTrue.Operand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given labeled statement.
    /// </summary>
    public override void Visit(LabeledStatement labeledStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(labeledStatement);
      this.VisitStatement(labeledStatement.Statement);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given lambda.
    /// </summary>
    public override void Visit(Lambda lambda)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(lambda);
      this.Visit(lambda.Parameters);
      if (lambda.Expression == null) {
        //^ assert lambda.Body != null;
        this.VisitStatement(lambda.Body);
      } else {
        this.VisitExpression(lambda.Expression);
      }
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given lambda parameters.
    /// </summary>
    public virtual void Visit(IEnumerable<LambdaParameter> lambdaParameters)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      foreach (LambdaParameter lambdaParameter in lambdaParameters)
        this.Visit(lambdaParameter);
    }

    /// <summary>
    /// Performs some computation with the given lambda parameter.
    /// </summary>
    public override void Visit(LambdaParameter lambdaParameter)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(lambdaParameter);
      if (lambdaParameter.ParameterType != null)
        this.VisitExpression(lambdaParameter.ParameterType);
      this.Visit(lambdaParameter.ParameterName);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given left shift expression.
    /// </summary>
    public override void Visit(LeftShift leftShift)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(leftShift);
      this.VisitExpression(leftShift.LeftOperand);
      this.VisitExpression(leftShift.RightOperand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given left shift assignment expression.
    /// </summary>
    public override void Visit(LeftShiftAssignment leftShiftAssignment)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(leftShiftAssignment);
      this.Visit(leftShiftAssignment.LeftOperand);
      this.VisitExpression(leftShiftAssignment.RightOperand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given less than expression.
    /// </summary>
    public override void Visit(LessThan lessThan)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(lessThan);
      this.VisitExpression(lessThan.LeftOperand);
      this.VisitExpression(lessThan.RightOperand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given less than or equal expression.
    /// </summary>
    public override void Visit(LessThanOrEqual lessThanOrEqual)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(lessThanOrEqual);
      this.VisitExpression(lessThanOrEqual.LeftOperand);
      this.VisitExpression(lessThanOrEqual.RightOperand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given lifted conversion expression.
    /// </summary>
    public override void Visit(LiftedConversion liftedConversion)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(liftedConversion);
      this.VisitExpression(liftedConversion.ValueToConvert);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given like expression.
    /// </summary>
    public override void Visit(Like like)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(like);
      this.VisitExpression(like.LeftOperand);
      this.VisitExpression(like.RightOperand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given local declarations.
    /// </summary>
    public virtual void Visit(IEnumerable<LocalDeclaration> localDeclarations)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      foreach (LocalDeclaration localDeclaration in localDeclarations)
        this.Visit(localDeclaration);
    }

    /// <summary>
    /// Performs some computation with the given local declaration.
    /// </summary>
    public override void Visit(LocalDeclaration localDeclaration)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(localDeclaration);
      if (localDeclaration.InitialValue != null)
        this.VisitExpression(localDeclaration.InitialValue);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given local declarations statements.
    /// </summary>
    public virtual void Visit(IEnumerable<LocalDeclarationsStatement> localDeclarationsStatements)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      foreach (LocalDeclarationsStatement localDeclarationsStatement in localDeclarationsStatements)
        this.VisitStatement(localDeclarationsStatement);
    }

    /// <summary>
    /// Performs some computation with the given local declarations statement.
    /// </summary>
    public override void Visit(LocalDeclarationsStatement localDeclarationsStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(localDeclarationsStatement);
      if (localDeclarationsStatement.TypeExpression != null)
        this.VisitExpression(localDeclarationsStatement.TypeExpression);
      this.Visit(localDeclarationsStatement.Declarations);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given lock statement.
    /// </summary>
    public override void Visit(LockStatement lockStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(lockStatement);
      this.VisitExpression(lockStatement.Guard);
      this.VisitStatement(lockStatement.Body);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given logical and expression.
    /// </summary>
    public override void Visit(LogicalAnd logicalAnd)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(logicalAnd);
      this.VisitExpression(logicalAnd.LeftOperand);
      this.VisitExpression(logicalAnd.RightOperand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given logical not expression.
    /// </summary>
    public override void Visit(LogicalNot logicalNot)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(logicalNot);
      this.VisitExpression(logicalNot.Operand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.hod or operation is not implemented.");
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given logical or expression.
    /// </summary>
    public override void Visit(LogicalOr logicalOr)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(logicalOr);
      this.VisitExpression(logicalOr.LeftOperand);
      this.VisitExpression(logicalOr.RightOperand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given loop invariant.
    /// </summary>
    public override void Visit(LoopInvariant loopInvariant)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(loopInvariant);
      this.VisitExpression(loopInvariant.Condition);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given make typed reference expression.
    /// </summary>
    public override void Visit(MakeTypedReference makeTypedReference)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(makeTypedReference);
      this.VisitExpression(makeTypedReference.Operand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given managed pointer type.
    /// </summary>
    public override void Visit(ManagedPointerType managedPointerType)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    /// <summary>
    /// Performs some computation with the given managed pointer type expression.
    /// </summary>
    public override void Visit(ManagedPointerTypeExpression managedPointerTypeExpression)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(managedPointerTypeExpression);
      this.VisitExpression(managedPointerTypeExpression.TargetType);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given method body.
    /// </summary>
    public override void Visit(MethodBody methodBody)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(methodBody);
      this.VisitStatement(methodBody.Block);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given method call.
    /// </summary>
    public override void Visit(MethodCall methodCall)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(methodCall);
      this.Visit(methodCall.OriginalArguments);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given method declaration.
    /// </summary>
    public override void Visit(MethodDeclaration methodDeclaration)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(methodDeclaration);
      this.Visit(methodDeclaration.SourceAttributes);
      this.VisitTypeExpression(methodDeclaration.Type);
      this.Visit(methodDeclaration.Parameters);
      if (this.visitMethodBodies && !methodDeclaration.IsAbstract && !methodDeclaration.IsExternal)
        this.Visit(methodDeclaration.Body);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given method group expression.
    /// </summary>
    public override void Visit(MethodGroup methodGroup)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(methodGroup);
      this.VisitExpression(methodGroup.Selector);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }
    
    /// <summary>
    /// Performs some computation with the given modulus expression.
    /// </summary>
    public override void Visit(Modulus modulus)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(modulus);
      this.VisitExpression(modulus.LeftOperand);
      this.VisitExpression(modulus.RightOperand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given modulus assignment expression.
    /// </summary>
    public override void Visit(ModulusAssignment modulusAssignment)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(modulusAssignment);
      this.Visit(modulusAssignment.LeftOperand);
      this.VisitExpression(modulusAssignment.RightOperand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given multiplication expression.
    /// </summary>
    public override void Visit(Multiplication multiplication)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(multiplication);
      this.VisitExpression(multiplication.LeftOperand);
      this.VisitExpression(multiplication.RightOperand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.or operation is not implemented.");
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given multiplication assignment expression.
    /// </summary>
    public override void Visit(MultiplicationAssignment multiplicationAssignment)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(multiplicationAssignment);
      this.Visit(multiplicationAssignment.LeftOperand);
      this.VisitExpression(multiplicationAssignment.RightOperand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.or operation is not implemented.");
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given named arguments.
    /// </summary>
    public virtual void Visit(IEnumerable<NamedArgument> namedArguments)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      foreach (NamedArgument namedArgument in namedArguments)
        this.VisitExpression(namedArgument);
    }

    /// <summary>
    /// Performs some computation with the given named argument.
    /// </summary>
    public override void Visit(NamedArgument namedArgument)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(namedArgument);
      this.VisitExpression(namedArgument.ArgumentName);
      this.VisitExpression(namedArgument.ArgumentValue);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given name declaration.
    /// </summary>
    public override void Visit(NameDeclaration nameDeclaration)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    /// <summary>
    /// Performs some computation with the given named type expression.
    /// </summary>
    public override void Visit(NamedTypeExpression namedTypeExpression)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(namedTypeExpression);
      this.VisitExpression(namedTypeExpression.Expression);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given namespace class declaration.
    /// </summary>
    public override void Visit(NamespaceClassDeclaration namespaceClassDeclaration)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(namespaceClassDeclaration);
      this.Visit(namespaceClassDeclaration.SourceAttributes);
      foreach (TypeExpression baseType in namespaceClassDeclaration.BaseTypes)
        this.VisitTypeExpression(baseType);
      this.Visit(namespaceClassDeclaration.TypeDeclarationMembers);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given unit namespace declaration.
    /// </summary>
    public override void Visit(NamespaceDeclaration unitNamespaceDeclaration)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(unitNamespaceDeclaration);
      this.Visit(unitNamespaceDeclaration.SourceAttributes);
      this.Visit(unitNamespaceDeclaration.Members);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given namespace declaration member.
    /// </summary>
    public virtual void VisitNamespaceDeclarationMember(INamespaceDeclarationMember namespaceDeclarationMember)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      NestedNamespaceDeclaration/*?*/ nestedNamespace = namespaceDeclarationMember as NestedNamespaceDeclaration;
      if (nestedNamespace != null)
        this.Visit(nestedNamespace);
      else {
        TypeDeclaration/*?*/ typeDeclaration = namespaceDeclarationMember as TypeDeclaration;
        if (typeDeclaration != null)
          this.Visit(typeDeclaration);
        else {
          SourceItem/*?*/ sourceItem = namespaceDeclarationMember as SourceItem;
          if (sourceItem != null) sourceItem.Dispatch(this);
        }
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    /// <summary>
    /// Performs some computation with the given namespace members.
    /// </summary>
    public virtual void Visit(IEnumerable<INamespaceDeclarationMember> namespaceMembers)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      foreach (INamespaceDeclarationMember namespaceDeclarationMember in namespaceMembers)
        this.VisitNamespaceDeclarationMember(namespaceDeclarationMember);
    }

    /// <summary>
    /// Performs some computation with the given delegate declaration.
    /// </summary>
    public override void Visit(NamespaceDelegateDeclaration delegateDeclaration)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(delegateDeclaration);
      this.Visit(delegateDeclaration.SourceAttributes);
      this.Visit(delegateDeclaration.Signature);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given enum declaration.
    /// </summary>
    public override void Visit(NamespaceEnumDeclaration enumDeclaration)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(enumDeclaration);
      this.Visit(enumDeclaration.SourceAttributes);
      foreach (TypeExpression baseType in enumDeclaration.BaseTypes)
        this.VisitTypeExpression(baseType);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given namespace import declaration.
    /// </summary>
    public override void Visit(NamespaceImportDeclaration namespaceImportDeclaration)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(namespaceImportDeclaration);
      this.VisitExpression(namespaceImportDeclaration.ImportedNamespace);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given interface declaration.
    /// </summary>
    public override void Visit(NamespaceInterfaceDeclaration interfaceDeclaration)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(interfaceDeclaration);
      this.Visit(interfaceDeclaration.SourceAttributes);
      foreach (TypeExpression baseType in interfaceDeclaration.BaseTypes)
        this.VisitTypeExpression(baseType);
      this.Visit(interfaceDeclaration.TypeDeclarationMembers);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given namespace reference expression.
    /// </summary>
    public override void Visit(NamespaceReferenceExpression namespaceReferenceExpression)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(namespaceReferenceExpression);
      this.VisitExpression(namespaceReferenceExpression.Expression);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given struct declaration.
    /// </summary>
    public override void Visit(NamespaceStructDeclaration structDeclaration)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(structDeclaration);
      this.Visit(structDeclaration.SourceAttributes);
      foreach (TypeExpression baseType in structDeclaration.BaseTypes)
        this.VisitTypeExpression(baseType);
      this.Visit(structDeclaration.TypeDeclarationMembers);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given class declaration.
    /// </summary>
    public override void Visit(NestedClassDeclaration classDeclaration)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(classDeclaration);
      this.Visit(classDeclaration.SourceAttributes);
      foreach (TypeExpression baseType in classDeclaration.BaseTypes)
        this.VisitTypeExpression(baseType);
      this.Visit(classDeclaration.GenericParameters);
      this.Visit(classDeclaration.TypeDeclarationMembers);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given delegate declaration.
    /// </summary>
    public override void Visit(NestedDelegateDeclaration delegateDeclaration)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(delegateDeclaration);
      this.Visit(delegateDeclaration.SourceAttributes);
      this.Visit(delegateDeclaration.Signature);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given enum declaration.
    /// </summary>
    public override void Visit(NestedEnumDeclaration enumDeclaration)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(enumDeclaration);
      this.Visit(enumDeclaration.SourceAttributes);
      foreach (TypeExpression baseType in enumDeclaration.BaseTypes)
        this.VisitTypeExpression(baseType);
      this.Visit(enumDeclaration.TypeDeclarationMembers);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given interface declaration.
    /// </summary>
    public override void Visit(NestedInterfaceDeclaration interfaceDeclaration)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(interfaceDeclaration);
      this.Visit(interfaceDeclaration.SourceAttributes);
      foreach (TypeExpression baseType in interfaceDeclaration.BaseTypes)
        this.VisitTypeExpression(baseType);
      this.Visit(interfaceDeclaration.TypeDeclarationMembers);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given nested namespace declaration.
    /// </summary>
    public override void Visit(NestedNamespaceDeclaration nestedNamespaceDeclaration)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(nestedNamespaceDeclaration);
      this.Visit(nestedNamespaceDeclaration.SourceAttributes);
      this.Visit(nestedNamespaceDeclaration.Members);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given struct declaration.
    /// </summary>
    public override void Visit(NestedStructDeclaration structDeclaration)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(structDeclaration);
      this.Visit(structDeclaration.SourceAttributes);
      foreach (TypeExpression baseType in structDeclaration.BaseTypes)
        this.VisitTypeExpression(baseType);
      this.Visit(structDeclaration.TypeDeclarationMembers);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given non null type expression.
    /// </summary>
    public override void Visit(NonNullTypeExpression nonNullTypeExpression)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(nonNullTypeExpression);
      this.VisitExpression(nonNullTypeExpression.ElementType);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given not equality expression.
    /// </summary>
    public override void Visit(NotEquality notEquality)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(notEquality);
      this.VisitExpression(notEquality.LeftOperand);
      this.VisitExpression(notEquality.RightOperand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given nullable type expression.
    /// </summary>
    public override void Visit(NullableTypeExpression nullableTypeExpression)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(nullableTypeExpression);
      this.VisitExpression(nullableTypeExpression.ElementType);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given null coalescing expression.
    /// </summary>
    public override void Visit(NullCoalescing nullCoalescing)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(nullCoalescing);
      this.VisitExpression(nullCoalescing.LeftOperand);
      this.VisitExpression(nullCoalescing.RightOperand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given on error goto statement.
    /// </summary>
    public override void Visit(OnErrorGotoStatement onErrorGotoStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(onErrorGotoStatement);
      this.VisitStatement(onErrorGotoStatement.Goto);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given on error resume next statement.
    /// </summary>
    public override void Visit(OnErrorResumeNextStatement onErrorResumeNextStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(onErrorResumeNextStatement);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given ones complement expression.
    /// </summary>
    public override void Visit(OnesComplement onesComplement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(onesComplement);
      this.VisitExpression(onesComplement.Operand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given option declaration.
    /// </summary>
    public override void Visit(OptionDeclaration optionDeclaration)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    /// <summary>
    /// Performs some computation with the given out argument.
    /// </summary>
    public override void Visit(OutArgument outArgument)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(outArgument);
      this.Visit(outArgument.Expression);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given parameters.
    /// </summary>
    public virtual void Visit(IEnumerable<ParameterDeclaration> parameters)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      foreach (ParameterDeclaration parameter in parameters)
        this.Visit(parameter);
    }

    /// <summary>
    /// Performs some computation with the given parameter declaration.
    /// </summary>
    public override void Visit(ParameterDeclaration parameterDeclaration)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(parameterDeclaration);
      this.Visit(parameterDeclaration.SourceAttributes);
      this.VisitTypeExpression(parameterDeclaration.Type);
      if (parameterDeclaration.HasDefaultValue)
        this.VisitExpression(parameterDeclaration.DefaultValue);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given parenthesis expression.
    /// </summary>
    public override void Visit(Parenthesis parenthesis)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(parenthesis);
      this.VisitExpression(parenthesis.ParenthesizedExpression);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given populate collection expression.
    /// </summary>
    public override void Visit(PopulateCollection populateCollection)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(populateCollection);
      this.VisitExpression(populateCollection.CollectionToPopulate);
      this.Visit(populateCollection.ElementValues);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.or operation is not implemented.");
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given pointer type expression.
    /// </summary>
    public override void Visit(PointerTypeExpression pointerTypeExpression)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(pointerTypeExpression);
      this.VisitExpression(pointerTypeExpression.ElementType);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given postconditions.
    /// </summary>
    public virtual void Visit(IEnumerable<Postcondition> postconditions)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      foreach (Postcondition postcondition in postconditions)
        this.Visit(postcondition);
    }

    /// <summary>
    /// Performs some computation with the given postcondition.
    /// </summary>
    public override void Visit(Postcondition postcondition)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(postcondition);
      this.VisitExpression(postcondition.Condition);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given postfix decrement expression.
    /// </summary>
    public override void Visit(PostfixDecrement postfixDecrement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(postfixDecrement);
      this.Visit(postfixDecrement.Target);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given postfix increment expression.
    /// </summary>
    public override void Visit(PostfixIncrement postfixIncrement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(postfixIncrement);
      this.Visit(postfixIncrement.Target);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given postcondition.
    /// </summary>
    public override void Visit(Precondition precondition)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(precondition);
      this.VisitExpression(precondition.Condition);
      if (precondition.ExceptionToThrow != null)
        this.VisitExpression(precondition.ExceptionToThrow);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given prefix decrement expression.
    /// </summary>
    public override void Visit(PrefixDecrement prefixDecrement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(prefixDecrement);
      this.Visit(prefixDecrement.Target);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given prefix increment expression.
    /// </summary>
    public override void Visit(PrefixIncrement prefixIncrement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(prefixIncrement);
      this.Visit(prefixIncrement.Target);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given property declaration.
    /// </summary>
    public override void Visit(PropertyDeclaration propertyDeclaration)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(propertyDeclaration);
      this.Visit(propertyDeclaration.SourceAttributes);
      this.VisitTypeExpression(propertyDeclaration.Type);
      this.Visit(propertyDeclaration.Parameters);
      if (propertyDeclaration.GetterBody != null)
        this.VisitStatement(propertyDeclaration.GetterBody);
      if (propertyDeclaration.SetterBody != null)
        this.VisitStatement(propertyDeclaration.SetterBody);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given property setter value expression.
    /// </summary>
    public override void Visit(PropertySetterValue propertySetterValue)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    /// <summary>
    /// Performs some computation with the given qualified name expression.
    /// </summary>
    public override void Visit(QualifiedName qualifiedName)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(qualifiedName);
      this.VisitExpression(qualifiedName.Qualifier);
      this.VisitExpression(qualifiedName.SimpleName);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given raise event statement.
    /// </summary>
    public override void Visit(RaiseEventStatement raiseEventStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(raiseEventStatement);
      this.VisitExpression(raiseEventStatement.EventToRaise);
      this.Visit(raiseEventStatement.Arguments);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given range expression.
    /// </summary>
    public override void Visit(Range range)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(range);
      this.VisitExpression(range.StartValue);
      this.VisitExpression(range.EndValue);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given redimension clauses.
    /// </summary>
    public virtual void Visit(IEnumerable<RedimensionClause> redimensionClauses)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      foreach (RedimensionClause redimensionClause in redimensionClauses)
        this.Visit(redimensionClause);
    }

    /// <summary>
    /// Performs some computation with the given redimension clause.
    /// </summary>
    public override void Visit(RedimensionClause redimensionClause)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(redimensionClause);
      this.Visit(redimensionClause.Target);
      this.VisitExpression(redimensionClause.Value);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given redimension statement.
    /// </summary>
    public override void Visit(RedimensionStatement redimensionStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(redimensionStatement);
      this.Visit(redimensionStatement.Arrays);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given ref argument.
    /// </summary>
    public override void Visit(RefArgument refArgument)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(refArgument);
      this.Visit(refArgument.Expression);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given reference equality expression.
    /// </summary>
    public override void Visit(ReferenceEquality referenceEquality)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(referenceEquality);
      this.VisitExpression(referenceEquality.LeftOperand);
      this.VisitExpression(referenceEquality.RightOperand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given reference inequality expression.
    /// </summary>
    public override void Visit(ReferenceInequality referenceInequality)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(referenceInequality);
      this.VisitExpression(referenceInequality.LeftOperand);
      this.VisitExpression(referenceInequality.RightOperand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given remove event handler statement.
    /// </summary>
    public override void Visit(RemoveEventHandlerStatement removeEventHandlerStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(removeEventHandlerStatement);
      this.VisitExpression(removeEventHandlerStatement.Event);
      this.VisitExpression(removeEventHandlerStatement.Handler);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given resource use statement.
    /// </summary>
    public override void Visit(ResourceUseStatement resourceUseStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(resourceUseStatement);
      this.VisitStatement(resourceUseStatement.ResourceAcquisitions);
      this.VisitStatement(resourceUseStatement.Body);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given resume labeled statement.
    /// </summary>
    public override void Visit(ResumeLabeledStatement resumeLabeledStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(resumeLabeledStatement);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given resume next statement.
    /// </summary>
    public override void Visit(ResumeNextStatement resumeNextStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(resumeNextStatement);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given resume statement.
    /// </summary>
    public override void Visit(ResumeStatement resumeStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(resumeStatement);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given rethrow statement.
    /// </summary>
    public override void Visit(RethrowStatement rethrowStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(rethrowStatement);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given return statement.
    /// </summary>
    public override void Visit(ReturnStatement returnStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(returnStatement);
      if (returnStatement.Expression != null)
        this.VisitExpression(returnStatement.Expression);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given return value expression.
    /// </summary>
    public override void Visit(ReturnValue returnValue)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    /// <summary>
    /// Performs some computation with the given right shift expression.
    /// </summary>
    public override void Visit(RightShift rightShift)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(rightShift);
      this.VisitExpression(rightShift.LeftOperand);
      this.VisitExpression(rightShift.RightOperand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given right shift assignment expression.
    /// </summary>
    public override void Visit(RightShiftAssignment rightShiftAssignment)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(rightShiftAssignment);
      this.Visit(rightShiftAssignment.LeftOperand);
      this.VisitExpression(rightShiftAssignment.RightOperand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given stack array create expression.
    /// </summary>
    public override void Visit(CreateStackArray stackArrayCreate)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(stackArrayCreate);
      this.VisitExpression(stackArrayCreate.ElementType);
      this.VisitExpression(stackArrayCreate.Size);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given stop statement.
    /// </summary>
    public override void Visit(StopStatement stopStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(stopStatement);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given unsigned right shift expression.
    /// </summary>
    public override void Visit(UnsignedRightShift unsignedRightShift)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(unsignedRightShift);
      this.VisitExpression(unsignedRightShift.LeftOperand);
      this.VisitExpression(unsignedRightShift.RightOperand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given root namespace expression.
    /// </summary>
    public override void Visit(RootNamespaceExpression rootNamespaceExpression)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    /// <summary>
    /// Performs some computation with the given runtime argument handle expression.
    /// </summary>
    public override void Visit(RuntimeArgumentHandleExpression runtimeArgumentHandleExpression)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    /// <summary>
    /// Performs some computation with the given signature declaration.
    /// </summary>
    public override void Visit(SignatureDeclaration signatureDeclaration)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(signatureDeclaration);
      this.Visit(signatureDeclaration.Parameters);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given simple name.
    /// </summary>
    public override void Visit(SimpleName simpleName)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    /// <summary>
    /// Performs some computation with the given size of expression.
    /// </summary>
    public override void Visit(SizeOf sizeOf)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(sizeOf);
      this.VisitExpression(sizeOf.Expression);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given slice expression.
    /// </summary>
    public override void Visit(Slice slice)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(slice);
      this.VisitExpression(slice.Collection);
      this.VisitExpression(slice.StartIndex);
      this.VisitExpression(slice.Length);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given source custom attributes.
    /// </summary>
    public virtual void Visit(IEnumerable<SourceCustomAttribute> sourceCustomAttributes)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      foreach (SourceCustomAttribute sourceCustomAttribute in sourceCustomAttributes)
        this.Visit(sourceCustomAttribute);
    }

    /// <summary>
    /// Performs some computation with the given source custom attribute.
    /// </summary>
    public override void Visit(SourceCustomAttribute sourceCustomAttribute)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(sourceCustomAttribute);
      this.VisitExpression(sourceCustomAttribute.Type);
      this.Visit(sourceCustomAttribute.Arguments);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given statements.
    /// </summary>
    public virtual void Visit(IEnumerable<Statement> statements)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      foreach (Statement statement in statements)
        this.VisitStatement(statement);
    }

    /// <summary>
    /// Performs some computation with the given statement.
    /// </summary>
    public virtual void VisitStatement(Statement statement) {
      if (this.stopTraversal) return;
      statement.Dispatch(this);
    }

    /// <summary>
    /// Performs some computation with the given string concatenation expression.
    /// </summary>
    public override void Visit(StringConcatenation stringConcatenation)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(stringConcatenation);
      this.VisitExpression(stringConcatenation.LeftOperand);
      this.VisitExpression(stringConcatenation.RightOperand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given subtraction expression.
    /// </summary>
    public override void Visit(Subtraction subtraction)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(subtraction);
      this.VisitExpression(subtraction.LeftOperand);
      this.VisitExpression(subtraction.RightOperand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given subtraction assignment expression.
    /// </summary>
    public override void Visit(SubtractionAssignment subtractionAssignment)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(subtractionAssignment);
      this.Visit(subtractionAssignment.LeftOperand);
      this.VisitExpression(subtractionAssignment.RightOperand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given switch cases.
    /// </summary>
    public virtual void Visit(IEnumerable<SwitchCase> switchCases)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      foreach (SwitchCase switchCase in switchCases)
        this.Visit(switchCase);
    }

    /// <summary>
    /// Performs some computation with the given switch case.
    /// </summary>
    public override void Visit(SwitchCase switchCase)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(switchCase);
      if (!switchCase.IsDefault)
        this.VisitExpression(switchCase.Expression);
      this.Visit(switchCase.Body);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given switch statement.
    /// </summary>
    public override void Visit(SwitchStatement switchStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(switchStatement);
      this.VisitExpression(switchStatement.Expression);
      this.Visit(switchStatement.Cases);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given target expressions.
    /// </summary>
    public virtual void Visit(IEnumerable<AddressableExpression> targetExpressions)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      foreach (AddressableExpression targetExpression in targetExpressions)
        this.Visit(targetExpression);
    }

    /// <summary>
    /// Performs some computation with the given target expression.
    /// </summary>
    public override void Visit(TargetExpression targetExpression)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(targetExpression);
      this.VisitExpression(targetExpression.Expression);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given this reference.
    /// </summary>
    public override void Visit(ThisReference thisReference)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    /// <summary>
    /// Performs some computation with the thrown exception.
    /// </summary>
    public override void Visit(ThrownException thrownException)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(thrownException);
      if (thrownException.ExceptionType != null)
        this.VisitTypeExpression(thrownException.ExceptionType);
      this.Visit(thrownException.Postcondition);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given throw statement.
    /// </summary>
    public override void Visit(ThrowStatement throwStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(throwStatement);
      if (throwStatement.Exception != null)
        this.VisitExpression(throwStatement.Exception);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given try catch filter finally statement.
    /// </summary>
    public override void Visit(TryCatchFinallyStatement tryCatchFilterFinallyStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(tryCatchFilterFinallyStatement);
      this.VisitStatement(tryCatchFilterFinallyStatement.TryBody);
      this.Visit(tryCatchFilterFinallyStatement.CatchClauses);
      if (tryCatchFilterFinallyStatement.FinallyBody != null)
        this.VisitStatement(tryCatchFilterFinallyStatement.FinallyBody);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given type declaration.
    /// </summary>
    public override void Visit(TypeDeclaration typeDeclaration)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      //^ int oldCount = this.path.Count;
      typeDeclaration.Dispatch(this);
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    /// <summary>
    /// Performs some computation with the given type members.
    /// </summary>
    public virtual void Visit(IEnumerable<ITypeDeclarationMember> typeMembers)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      foreach (ITypeDeclarationMember typeMember in typeMembers)
        this.VisitTypeDeclarationMember(typeMember);
    }

    /// <summary>
    /// Performs some computation with the given type declaration member.
    /// </summary>
    public virtual void VisitTypeDeclarationMember(ITypeDeclarationMember typeDeclarationMember)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      TypeDeclaration/*?*/ typeDeclaration = typeDeclarationMember as TypeDeclaration;
      if (typeDeclaration != null)
        this.Visit(typeDeclaration);
      else {
        SourceItem/*?*/ sourceItem = typeDeclarationMember as SourceItem;
        if (sourceItem != null) sourceItem.Dispatch(this);
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    /// <summary>
    /// Performs some computation with the given type expressions.
    /// </summary>
    public virtual void Visit(IEnumerable<TypeExpression> typeExpressions)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      foreach (TypeExpression typeExpression in typeExpressions)
        this.VisitExpression(typeExpression);
    }

    /// <summary>
    /// Performs some computation with the given loop invariant.
    /// </summary>
    public override void Visit(TypeInvariant typeInvariant)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(typeInvariant);
      this.VisitExpression(typeInvariant.Condition);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given type of expression.
    /// </summary>
    public override void Visit(TypeOf typeOf)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(typeOf);
      this.VisitExpression(typeOf.Expression);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given unary negation expression.
    /// </summary>
    public override void Visit(UnaryNegation unaryNegation)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(unaryNegation);
      this.VisitExpression(unaryNegation.Operand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given unary plus expression.
    /// </summary>
    public override void Visit(UnaryPlus unaryPlus)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(unaryPlus);
      this.VisitExpression(unaryPlus.Operand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given unchecked expression.
    /// </summary>
    public override void Visit(UncheckedExpression uncheckedExpression) {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(uncheckedExpression);
      this.VisitExpression(uncheckedExpression.Operand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given unit set alias declaration.
    /// </summary>
    public override void Visit(UnitSetAliasDeclaration unitSetAliasDeclaration)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(unitSetAliasDeclaration);
      this.Visit(unitSetAliasDeclaration.Name);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given until do statement.
    /// </summary>
    public override void Visit(UntilDoStatement untilDoStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(untilDoStatement);
      this.VisitExpression(untilDoStatement.Condition);
      this.VisitStatement(untilDoStatement.Body);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given while do statement.
    /// </summary>
    public override void Visit(WhileDoStatement whileDoStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(whileDoStatement);
      this.VisitExpression(whileDoStatement.Condition);
      this.VisitStatement(whileDoStatement.Body);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given with statement.
    /// </summary>
    public override void Visit(WithStatement withStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(withStatement);
      this.VisitExpression(withStatement.ImplicitQualifier);
      this.VisitStatement(withStatement.Body);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given yield break statement.
    /// </summary>
    public override void Visit(YieldBreakStatement yieldBreakStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    /// <summary>
    /// Performs some computation with the given yield return statement.
    /// </summary>
    public override void Visit(YieldReturnStatement yieldReturnStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(yieldReturnStatement);
      this.VisitExpression(yieldReturnStatement.Expression);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given type expression.
    /// </summary>
    public virtual void VisitTypeExpression(TypeExpression typeExpression)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      typeExpression.Dispatch(this);
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

  }

  /// <summary>
  /// 
  /// </summary>
  public class StatementFinder : MemberFinder {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="locationToContain"></param>
    public StatementFinder(ISourceLocation locationToContain)
      : base(locationToContain) {
    }

    /// <summary>
    /// 
    /// </summary>
    public Statement/*?*/ MostNestedStatement {
      get
        //^ ensures result == null || result.SourceLocation.Contains(this.LocationToContain);
      {
        //^ assume this.mostNestedStatement == null || this.mostNestedStatement.SourceLocation.Contains(this.LocationToContain); //follows from invariant that Boogie chokes on
        return this.mostNestedStatement;
      }
    }
    Statement/*?*/ mostNestedStatement;
    // ^ invariant mostNestedStatement == null || mostNestedStatement.SourceLocation.Contains(this.LocationToContain);

    /// <summary>
    /// Performs some computation with the given method declaration.
    /// </summary>
    /// <param name="methodDeclaration"></param>
    public override void Visit(MethodDeclaration methodDeclaration)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      //^ int oldCount = this.path.Count;
      this.path.Push(methodDeclaration);
      this.Visit(methodDeclaration.SourceAttributes);
      this.Visit(methodDeclaration.Parameters);
      if (!methodDeclaration.IsAbstract && !methodDeclaration.IsExternal)
        this.Visit(methodDeclaration.Body);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given statement.
    /// </summary>
    /// <param name="statement"></param>
    public override void VisitStatement(Statement statement) {
      if (statement.SourceLocation.Contains(this.LocationToContain)) {
        this.mostNestedStatement = statement;
        base.VisitStatement(statement);
      }
    }

  }

  /// <summary>
  /// 
  /// </summary>
  public class StatementScope : Scope<LocalDeclaration> { //TODO: use non caching scope

    /// <summary>
    /// 
    /// </summary>
    /// <param name="statement"></param>
    public StatementScope(Statement statement) {
      this.statement = statement;
    }

    Statement statement;
    Dictionary<int, LabeledStatement>/*?*/ statementForLabel;
    Dictionary<int, LabeledStatement>/*?*/ statementForLabelIgnoringCase;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="label"></param>
    /// <param name="ignoreCase"></param>
    /// <returns></returns>
    public LabeledStatement/*?*/ GetStatementLabeled(IName label, bool ignoreCase) {
      LabeledStatement/*?*/ result = null;
      this.InitializeIfNecessary();
      if (ignoreCase) {
        if (this.statementForLabelIgnoringCase != null)
          this.statementForLabelIgnoringCase.TryGetValue(label.UniqueKeyIgnoringCase, out result);
      } else {
        if (this.statementForLabel != null)
          this.statementForLabel.TryGetValue(label.UniqueKey, out result);
      }
      return result;
    }

    /// <summary>
    /// Provides a derived class with an opportunity to lazily initialize the scope's data structures via calls to AddMemberToCache.
    /// </summary>
    protected override void InitializeIfNecessary() {
      if (this.isInitialized) return;
      lock (GlobalLock.LockingObject) {
        if (this.isInitialized) return;
        this.statement.Dispatch(new LocalsFinder(this));
        this.isInitialized = true;
      }
    }
    private bool isInitialized;

    class LocalsFinder : SourceTraverser {

      internal LocalsFinder(StatementScope scope) {
        this.scope = scope;
      }

      StatementScope scope;

      public override void Visit(LocalDeclaration localDeclaration) {
        this.scope.AddMemberToCache(localDeclaration);
      }

      public override void Visit(LocalDeclarationsStatement localDeclarationsStatement) {
        this.Visit(localDeclarationsStatement.Declarations);
      }

      public override void VisitStatement(Statement statement) {
        LocalDeclarationsStatement/*?*/ locDecls = statement as LocalDeclarationsStatement;
        if (locDecls != null)
          this.Visit(locDecls);
        else {
          SwitchStatement/*?*/ switchStatement = statement as SwitchStatement;
          if (switchStatement != null) {
            foreach (SwitchCase swCase in switchStatement.Cases)
              this.Visit(swCase.Body);
            return;
          }
          LabeledStatement/*?*/ labeledStatement = statement as LabeledStatement;
          while (labeledStatement != null) {
            if (this.scope.statementForLabel == null) this.scope.statementForLabel = new Dictionary<int, LabeledStatement>();
            if (this.scope.statementForLabelIgnoringCase == null) this.scope.statementForLabelIgnoringCase = new Dictionary<int, LabeledStatement>();
            this.scope.statementForLabel.Add(labeledStatement.Label.UniqueKey, labeledStatement);
            this.scope.statementForLabelIgnoringCase.Add(labeledStatement.Label.UniqueKeyIgnoringCase, labeledStatement);
            labeledStatement = labeledStatement.Statement as LabeledStatement;
          }
        }
      }

    }

  }
}
