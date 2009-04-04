//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System.Collections.Generic;
using System.Resources;
using Microsoft.Cci.Ast;
using Microsoft.Cci.CSharp.Preprocessing;
using System;
using System.Diagnostics.SymbolStore;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci.CSharp {

  /// <summary>
  /// An object that represents a source document, such as file, which is parsed by a C# compiler to produce the C# specific object model
  /// from which the language agnostic object model can be obtained.
  /// </summary>
  public interface ICSharpSourceDocument : ISourceDocument {
    /// <summary>
    /// The C# compilation part that corresponds to this C# source document.
    /// </summary>
    CSharpCompilationPart CSharpCompilationPart {
      get;
      // ^ ensures result.SourceLocation.SourceDocument == this;
    }
  }

  public sealed class CSharpCompilation : Compilation {

    /// <summary>
    /// Do not use this constructor unless you are implementing the Compilation property of the Module class.
    /// I.e. to construct a Compilation instance, construct a Module instance and use its Compilation property. 
    /// </summary>
    internal CSharpCompilation(ISourceEditHost hostEnvironment, Unit result, CSharpOptions options, IEnumerable<CompilationPart> parts)
      : base(hostEnvironment, result, options)
      //^ requires result is CSharpModule || result is CSharpAssembly;
    {
      this.parts = parts;
    }

    protected override List<CompilationPart> GetPartList() {
      return new List<CompilationPart>(this.parts);
    }
    readonly IEnumerable<CompilationPart> parts;

    internal readonly CSharpOptions options = new CSharpOptions();

    public override Compilation UpdateCompilationParts(IEnumerable<CompilationPart> parts) {
      CSharpAssembly/*?*/ oldAssembly = this.Result as CSharpAssembly;
      if (oldAssembly != null) {
        CSharpAssembly newAssembly = new CSharpAssembly(oldAssembly.Name, oldAssembly.Location, this.HostEnvironment, this.options, oldAssembly.AssemblyReferences, oldAssembly.ModuleReferences, parts);
        return newAssembly.Compilation;
      }
      //^ assume this.Result is CSharpModule; //follows from constructor precondition and immutability.
      CSharpModule oldModule = (CSharpModule)this.Result;
      CSharpModule newModule = new CSharpModule(oldModule.Name, oldModule.Location, this.HostEnvironment, this.options, Dummy.Assembly, oldModule.AssemblyReferences, oldModule.ModuleReferences, parts);
      return newModule.Compilation;
    }
  }

  public sealed class CSharpCompilationPart : CompilationPart {

    public CSharpCompilationPart(CSharpCompilationHelper helper, ISourceLocation sourceLocation)
      : base(helper, sourceLocation)
      //^ requires sourceLocation.SourceDocument is CSharpCompositeDocument;
    {
    }

    //^ [MustOverride]
    public override CompilationPart MakeShallowCopyFor(Compilation targetCompilation)
      //^^ ensures result.GetType() == this.GetType();
    {
      if (this.Compilation == targetCompilation) return this;
      ISourceLocation sloc = this.SourceLocation;
      CSharpCompositeDocument/*?*/ oldDocument = sloc.SourceDocument as CSharpCompositeDocument;
      //^ assume oldDocument != null; //follows from constructor precondition and immutability of sloc
      CompilationPart result = oldDocument.MakeShallowCopyFor(targetCompilation).CSharpCompilationPart;
      //^ assume result.GetType() == this.GetType();
      return result;
    }

    public override INamespaceDeclarationMember/*?*/ ParseAsNamespaceDeclarationMember(ISourceLocation sourceLocationBeforeEdit, ISourceDocumentEdit edit)
      //^^ requires this.SourceLocation.SourceDocument == sourceLocationBeforeEdit.SourceDocument;
      //^^ requires this.SourceLocation.SourceDocument == edit.SourceLocation.SourceDocument;
      //^^ requires sourceLocationBeforeEdit.Contains(edit.SourceLocation);
      //^^ requires edit.SourceDocumentAfterEdit.IsUpdatedVersionOf(sourceLocationBeforeEdit.SourceDocument);
      //^^ ensures result == null || result is NamespaceDeclarationMember || result is NamespaceTypeDeclaration || result is NestedNamespaceDeclaration;
    {
      CSharpCompositeDocument/*?*/ updatedDoc = edit.SourceDocumentAfterEdit as CSharpCompositeDocument;
      //^ assume updatedDoc != null; //follows from constructor precondition and immutability of this.SourceLocation
      //^ assume updatedDoc.IsUpdatedVersionOf(sourceLocationBeforeEdit.SourceDocument); //follows from precondition
      ISourceLocation updatedSourceLocation = updatedDoc.GetCorrespondingSourceLocation(sourceLocationBeforeEdit);
      List<IErrorMessage> scannerAndParserErrors = updatedDoc.ScannerAndParserErrors;
      Parser parser = new Parser(this.Compilation, updatedSourceLocation, scannerAndParserErrors); //TODO: get options from Compilation
      INamespaceDeclarationMember/*?*/ result = parser.ParseNamespaceDeclarationMember();
      if (result != null) {
        ErrorEventArgs errorEventArguments = new ErrorEventArgs(ErrorReporter.Instance, updatedDoc.UnpreprocessedDocument.SourceLocation, updatedDoc.PreprocessorErrors.AsReadOnly());
        this.Compilation.HostEnvironment.ReportErrors(errorEventArguments);
        errorEventArguments = new ErrorEventArgs(ErrorReporter.Instance, updatedSourceLocation, scannerAndParserErrors.AsReadOnly());
        this.Compilation.HostEnvironment.ReportErrors(errorEventArguments);
      }
      return result;
    }

    public override RootNamespaceDeclaration ParseAsRootNamespace() {
      ISourceLocation sloc = this.SourceLocation;
      //^ assume sloc.SourceDocument is CSharpCompositeDocument;  //follows from constructor precondition and immutability of sloc
      CSharpRootNamespaceDeclaration result = new CSharpRootNamespaceDeclaration(this, sloc);
      this.rootNamespace = result;
      List<IErrorMessage> scannerAndParserErrors = ((CSharpCompositeDocument)this.SourceLocation.SourceDocument).ScannerAndParserErrors;
      scannerAndParserErrors.Clear();
      Parser parser = new Parser(this.Compilation, this.SourceLocation, scannerAndParserErrors); //TODO: get options from Compilation
      result.Parse(parser, this);
      ErrorEventArgs errorEventArguments = new ErrorEventArgs(ErrorReporter.Instance, this.SourceLocation, scannerAndParserErrors.AsReadOnly());
      this.Compilation.HostEnvironment.ReportErrors(errorEventArguments);
      errorEventArguments = new ErrorEventArgs(ErrorReporter.Instance, this.UnpreprocessedDocument.SourceLocation, this.PreprocessorErrors);
      this.Compilation.HostEnvironment.ReportErrors(errorEventArguments);
      return result;
    }

    public override ITypeDeclarationMember/*?*/ ParseAsTypeDeclarationMember(ISourceLocation sourceLocationBeforeEdit, ISourceDocumentEdit edit, IName typeName)
      //^^ requires this.SourceLocation.SourceDocument == sourceLocationBeforeEdit.SourceDocument;
      //^^ requires this.SourceLocation.SourceDocument == edit.SourceLocation.SourceDocument;
      //^^ requires sourceLocationBeforeEdit.Contains(edit.SourceLocation);
      //^^ requires edit.SourceDocumentAfterEdit.IsUpdatedVersionOf(sourceLocationBeforeEdit.SourceDocument);
      //^^ ensures result == null || result is TypeDeclarationMember || result is NestedTypeDeclaration;
    {
      ISourceLocation updatedSourceLocation = edit.SourceDocumentAfterEdit.GetCorrespondingSourceLocation(sourceLocationBeforeEdit); //unsatisfied precondition: requires this.IsUpdatedVersionOf(sourceLocationInPreviousVersionOfDocument.SourceDocument);
      List<IErrorMessage> scannerAndParserErrors = ((CSharpCompositeDocument)edit.SourceDocumentAfterEdit).ScannerAndParserErrors;
      Parser parser = new Parser(this.Compilation, updatedSourceLocation, scannerAndParserErrors); //TODO: get options from Compilation
      ITypeDeclarationMember/*?*/ result = parser.ParseTypeDeclarationMember(typeName);
      if (result != null) {
        CSharpCompositeDocument sdoc = (CSharpCompositeDocument)edit.SourceDocumentAfterEdit;
        ErrorEventArgs errorEventArguments = new ErrorEventArgs(ErrorReporter.Instance, sdoc.UnpreprocessedDocument.SourceLocation, sdoc.PreprocessorErrors.AsReadOnly());
        this.Compilation.HostEnvironment.ReportErrors(errorEventArguments);
        errorEventArguments = new ErrorEventArgs(ErrorReporter.Instance, updatedSourceLocation, scannerAndParserErrors.AsReadOnly());
        this.Compilation.HostEnvironment.ReportErrors(errorEventArguments);
      }
      return result;
    }

    internal List<IErrorMessage> PreprocessorErrors {
      get {
        ISourceLocation sloc = this.SourceLocation;
        CSharpCompositeDocument/*?*/ sdoc = sloc.SourceDocument as CSharpCompositeDocument;
        //^ assume sdoc != null; //follows from constructor precondition and immutability of sloc
        return sdoc.PreprocessorErrors;
      }
    }

    public override RootNamespaceDeclaration RootNamespace {
      get
        //^ ensures result is CSharpRootNamespaceDeclaration;
      {
        if (this.rootNamespace == null) {
          lock (GlobalLock.LockingObject) {
            if (this.rootNamespace == null) {
              ISourceLocation sloc = this.SourceLocation;
              //^ assume sloc.SourceDocument is CSharpCompositeDocument;  //follows from constructor precondition and immutability of sloc
              this.rootNamespace = new CSharpRootNamespaceDeclaration(this, sloc);
            }
          }
        }
        //^ assume this.rootNamespace is CSharpRootNamespaceDeclaration; //The above assignment is the sole initialization of this.rootNamespace
        return this.rootNamespace;
      }
    }

    internal List<IErrorMessage> ScannerAndParserErrors {
      get {
        ISourceLocation sloc = this.SourceLocation;
        CSharpCompositeDocument/*?*/ sdoc = sloc.SourceDocument as CSharpCompositeDocument;
        //^ assume sdoc != null; //follows from constructor precondition and immutability of sloc
        return sdoc.ScannerAndParserErrors; 
      }
    }

    public override CompilationPart UpdateRootNamespace(RootNamespaceDeclaration rootNamespace)
      //^^ requires this.RootNamespace.GetType() == rootNamespace().GetType();
    {
      List<CompilationPart> newParts = new List<CompilationPart>(this.Compilation.Parts);
      Compilation newCompilation = this.Compilation.UpdateCompilationParts(newParts);
      //^ assume this.Helper is CSharpCompilationHelper; //The constructor's type signature ensures this.
      CSharpCompilationHelper helper = (CSharpCompilationHelper)this.Helper.MakeShallowCopyFor(newCompilation);
      //^ assume rootNamespace is CSharpRootNamespaceDeclaration; //follows from the precondition and the post condition of this.RootNamespace.
      ISourceLocation sloc = rootNamespace.SourceLocation;
      //^ assume sloc.SourceDocument is CSharpCompositeDocument; //follows from the precondition of the constructors of CSharpRootNamespaceDeclaration.
      CSharpCompilationPart result = new CSharpCompilationPart(helper, sloc);
      result.rootNamespace = rootNamespace;
      for (int i = 0, n = newParts.Count; i < n; i++) {
        if (newParts[i] == this) { newParts[i] = result; break; }
      }
      return result;
    }

    internal ISourceDocument UnpreprocessedDocument {
      get {
        ISourceLocation sloc = this.SourceLocation;
        CSharpCompositeDocument/*?*/ sdoc = sloc.SourceDocument as CSharpCompositeDocument;
        //^ assume sdoc != null; //follows from constructor precondition and immutability of sloc
        return sdoc.UnpreprocessedDocument;
      }
    }

  }

  public sealed class CSharpCompilationHelper : LanguageSpecificCompilationHelper {

    public CSharpCompilationHelper(Compilation compilation)
      : base(compilation, "C#") {
    }

    private CSharpCompilationHelper(Compilation targetCompilation, CSharpCompilationHelper template) 
      : base(targetCompilation, template) {
    }

    //^ [Pure]
    public override LanguageSpecificCompilationHelper MakeShallowCopyFor(Compilation targetCompilation)
      //^^ ensures result.GetType() == this.GetType();
    {
      if (this.Compilation == targetCompilation) return this;
      return new CSharpCompilationHelper(targetCompilation, this);
    }

  }

  public sealed class CSharpUnpreprocessedSourceDocument : PrimarySourceDocument {

    public CSharpUnpreprocessedSourceDocument(CSharpCompilationHelper helper, IName name, string location, System.IO.StreamReader streamReader)
      : base(name, location, streamReader) {
      this.helper = helper;
    }

    public CSharpUnpreprocessedSourceDocument(CSharpCompilationHelper helper, IName name, string location, string text)
      : base(name, location, text) {
      this.helper = helper;
    }

    public CSharpUnpreprocessedSourceDocument(CSharpCompilationHelper helper, string text, SourceDocument previousVersion, int position, int oldLength, int newLength)
      : base(text, previousVersion, position, oldLength, newLength) {
      this.helper = helper;
    }

    readonly CSharpCompilationHelper helper;

    //public override CompilationPart CompilationPart {
    //  get
    //    //^^ ensures result.SourceLocation.SourceDocument == this;
    //  {
    //    //^ assume false; //unpreprocessed source documents should be fed to the preprocessor only and should never be asked to parse themselves.
    //    throw new System.InvalidOperationException();
    //  }
    //}

    //^ [Pure]
    public override ISourceLocation GetSourceLocation(int position, int length)
      //^^ requires 0 <= position && (position < this.Length || position == 0);
      //^^ requires 0 <= length;
      //^^ requires length <= this.Length;
      //^^ requires position+length <= this.Length;
      //^^ ensures result.SourceDocument == this;
      //^^ ensures result.StartIndex == position;
      //^^ ensures result.Length == length;
    {
      return new PrimarySourceLocation(this, position, length);
    }

    public CSharpUnpreprocessedSourceDocument GetUpdatedDocument(int position, int length, string updatedText)
      //^ requires 0 <= position && position < this.Length;
      //^ requires 0 <= length && length <= this.Length;
      //^ requires 0 <= position+length && position+length <= this.Length;
    {
      string oldText = this.GetText();
      if (position > oldText.Length) 
        position = oldText.Length; //This should only happen if the source document got clobbered after the precondition was established.
      //^ assume 0 <= position; //Follows from the precondition and the previous statement
      if (position+length > oldText.Length)
        length = oldText.Length-position;
      //^ assume 0 <= position+length; //established by the precondition and not changed by the previous two statements.
      string newText = oldText.Substring(0, position)+updatedText+oldText.Substring(position+length);
      return new CSharpUnpreprocessedSourceDocument(this.helper, newText, this, position, length, updatedText.Length);
    }

    public override string SourceLanguage {
      get { return "C#"; }
    }

    public override Guid DocumentType {
      get { return SymDocumentType.Text; }
    }

    public override Guid Language {
      get { return SymLanguageType.CSharp; }
    }

    public override Guid LanguageVendor {
      get { return SymLanguageVendor.Microsoft; }
    }
  }

  public abstract class CSharpCompositeDocument : CompositeSourceDocument {

    protected CSharpCompositeDocument(IName name)
      : base(name) {
    }

    protected CSharpCompositeDocument(SourceDocument previousVersion, int position, int oldLength, int newLength)
      : base(previousVersion, position, oldLength, newLength) {
    }

    /// <summary>
    /// Makes a shallow copy of this source document (creating a new
    /// </summary>
    //^^ [MustOverride]
    public abstract CSharpCompositeDocument MakeShallowCopyFor(Compilation targetCompilation);

    internal abstract List<IErrorMessage> PreprocessorErrors { get; }

    internal abstract List<IErrorMessage> ScannerAndParserErrors { get; }

    public abstract CSharpCompilationPart CSharpCompilationPart {
      get;
        //^^ ensures result.SourceLocation.SourceDocument == this;
        //^ ensures result is CompilationPart;
    }

    internal abstract ISourceDocument UnpreprocessedDocument { get; }

  }

  public abstract class CSharpCompositeDocument<PrimaryDocumentType, VersionType> : CSharpCompositeDocument, ICSharpSourceDocument 
    where PrimaryDocumentType : class, IPrimarySourceDocument
  {

    protected CSharpCompositeDocument(CSharpCompilationHelper helper, PrimaryDocumentType/*!*/ documentToPreprocess)
      : base(documentToPreprocess.Name)
    {
      this.helper = helper;
      this.documentToPreprocess = documentToPreprocess;
    }

    protected CSharpCompositeDocument(CSharpCompilationHelper helper, PrimaryDocumentType/*!*/ documentToPreprocess, PreprocessorInformation/*?*/ preprocessorInformation, 
      CSharpCompositeDocument<PrimaryDocumentType, VersionType> previousVersion, int position, int oldLength, int newLength)
      : base(previousVersion, position, oldLength, newLength)
    {
      this.helper = helper;
      this.documentToPreprocess = documentToPreprocess;
      this.preprocessorInformation = preprocessorInformation;
    }

    //public override CompilationPart CompilationPart {
    //  get { return this.CSharpCompilationPart; }
    //}

    public override CSharpCompilationPart CSharpCompilationPart {
      get
        //^^ ensures result.SourceLocation.SourceDocument == this;
        //^ ensures result is CompilationPart;
      {
        if (this.specSharpCompilationPart == null) {
          lock (GlobalLock.LockingObject) {
            if (this.specSharpCompilationPart == null) {
              ISourceLocation sloc = this.SourceLocation;
              //^ assume sloc.SourceDocument is CSharpCompositeDocument;
              this.specSharpCompilationPart = new CSharpCompilationPart(this.helper, sloc);
            }
          }
        }
        return this.specSharpCompilationPart;
      }
    }
    private CSharpCompilationPart/*?*/ specSharpCompilationPart;

    public PrimaryDocumentType/*!*/ DocumentToPreprocess {
      //^ [Pure]
      get { return this.documentToPreprocess; }
      protected set { this.documentToPreprocess = value; }
    }
    private PrimaryDocumentType/*!*/ documentToPreprocess;

    /// <summary>
    /// Returns a source document that represents the replacement of the text of this.DocumentToPreprocess from position to position+length with updatedText.
    /// I.e. the given position and length corresponds to the text of the document before preprocessing, but the resulting
    /// edit is an edit to the document after preprocessing (i.e. this document, not the one that preprocessor consumes).
    /// The compilation containing the compilation part that corresponds to the result of this call is registered with the
    /// host environment as being the latest version of the compilation.
    /// </summary>
    /// <param name="position">The position in this.DocumentToPreprocess, of the first character to be replaced by this edit.</param>
    /// <param name="length">The number of characters in this.DocumentToPreprocess that will be replaced by this edit.</param>
    /// <param name="updatedText">The replacement string.</param>
    protected ICSharpSourceDocument GetUpdatedDocument(int position, int length, string updatedText, VersionType version)
      //^ requires 0 <= position && position < this.DocumentToPreprocess.Length;
      //^ requires 0 <= length && length <= this.DocumentToPreprocess.Length;
      //^ requires 0 <= position+length && position+length <= this.DocumentToPreprocess.Length;
      //^ ensures result.IsUpdatedVersionOf(this);
      //^ ensures result.GetType() == this.GetType();
    {
      CSharpCompositeDocument<PrimaryDocumentType, VersionType> result;
      List<CompilationPart> nextParts = new List<CompilationPart>(this.CSharpCompilationPart.Compilation.Parts);
      Compilation nextCompilation = this.CSharpCompilationPart.Compilation.UpdateCompilationParts(nextParts);
      CSharpCompilationHelper nextHelper = (CSharpCompilationHelper)this.Helper.MakeShallowCopyFor(nextCompilation);
      PreprocessorInformation ppInfo = this.PreprocessorInformation;
      foreach (ISourceLocation includedLocation in ppInfo.IncludedLocations) {
        if (includedLocation.StartIndex <= position && position+length <= includedLocation.StartIndex+includedLocation.Length) {
          //Included section spans the edit.
          PrimaryDocumentType nextVersionOfDocumentToPreprocess = this.GetNextVersionOfDocumentToPreprocess(position, length, updatedText, version);
          PreprocessorInformation nextVersionOfPreprocessorInformation = new PreprocessorInformation(nextVersionOfDocumentToPreprocess, ppInfo);
          result = this.GetNextVersion(nextHelper, nextVersionOfDocumentToPreprocess, nextVersionOfPreprocessorInformation, position, length, updatedText.Length);
          goto updateCompilationPart;
        }
      }
      foreach (ISourceLocation excludedLocation in ppInfo.excludedLocations) {
        if (excludedLocation.StartIndex <= position && position+length <= excludedLocation.StartIndex+excludedLocation.Length) {
          //Excluded section spans the edit.
          PrimaryDocumentType nextVersionOfDocumentToPreprocess = this.GetNextVersionOfDocumentToPreprocess(position, length, updatedText, version);
          PreprocessorInformation nextVersionOfPreprocessorInformation = new PreprocessorInformation(nextVersionOfDocumentToPreprocess, ppInfo);
          result = this.GetNextVersion(nextHelper, nextVersionOfDocumentToPreprocess, nextVersionOfPreprocessorInformation, 0, 0, 0);
          goto updateCompilationPart;
        }
      }
      {
        //Not a common case and could be an edit that results in a different list of included locations.
        //Re-preprocess and produce an edit that replaces the entire resulting document (and thus forces the entire CompilationUnit to be rebuilt).
        PrimaryDocumentType nextVersionOfDocumentToPreprocess = this.GetNextVersionOfDocumentToPreprocess(position, length, updatedText, version);
        result = this.GetNewVersion(nextHelper, nextVersionOfDocumentToPreprocess);
        result = this.GetNextVersion(nextHelper, nextVersionOfDocumentToPreprocess, result.PreprocessorInformation, 0, this.Length, result.Length);
        goto updateCompilationPart;
      }
    updateCompilationPart:
      EditEventArgs editEventArgs;
      EditEventArgs/*?*/ symbolTableEditEventArgs;
      ISourceLocation oldLocationBeforePreprocessing = this.DocumentToPreprocess.GetSourceLocation(position, length);
      ISourceLocation oldLocationAfterPreprocessing = this.GetLocationAfterPreprocessing(oldLocationBeforePreprocessing);
      CSharpSourceDocumentEdit edit = new CSharpSourceDocumentEdit(oldLocationAfterPreprocessing, result);
      edit.compilationPartAfterEdit = result.specSharpCompilationPart = (CSharpCompilationPart)this.CSharpCompilationPart.UpdateWith(edit, nextParts, out editEventArgs, out symbolTableEditEventArgs);
      this.Helper.Compilation.HostEnvironment.RegisterAsLatest(result.CSharpCompilationPart.Compilation);
      this.Helper.Compilation.HostEnvironment.ReportEdits(editEventArgs);
      if (symbolTableEditEventArgs != null)
        this.Helper.Compilation.HostEnvironment.ReportSymbolTableEdits(symbolTableEditEventArgs);
      return result;
    }

    internal sealed class CSharpSourceDocumentEdit : AstSourceDocumentEdit {
      /// <summary>
      /// Allocates an object that describes an edit to a source file.
      /// </summary>
      internal CSharpSourceDocumentEdit(ISourceLocation sourceLocationBeforeEdit, ISourceDocument sourceDocumentAfterEdit)
        : base(sourceLocationBeforeEdit, sourceDocumentAfterEdit)
        //^ requires sourceDocumentAfterEdit.IsUpdatedVersionOf(sourceLocationBeforeEdit.SourceDocument);
      {
      }

      /// <summary>
      /// The compilation part that is the result of applying this edit.
      /// </summary>
      public override CompilationPart CompilationPartAfterEdit {
        get {
          //^ assume this.compilationPartAfterEdit != null;
          return this.compilationPartAfterEdit; 
        }
      }
      internal CompilationPart/*?*/ compilationPartAfterEdit;
    }

    /// <summary>
    /// Returns a location in the preprocessed document that corresponds to the given location from the unpreprocessed document.
    /// </summary>
    /// <param name="sourceLocation">A locotion in a source document that forms part of this composite document.</param>
    public ISourceLocation GetLocationAfterPreprocessing(ISourceLocation sourceLocation)
      //^ requires sourceLocation.SourceDocument == this.DocumentToPreprocess;
    {
      int start = 0;
      int length = 0;
      PreprocessorInformation ppInfo = this.PreprocessorInformation;
      foreach (ISourceLocation includedLocation in ppInfo.IncludedLocations) {
        if (includedLocation.StartIndex+includedLocation.Length <= sourceLocation.StartIndex) {
          //The included location is before the source location. Just add its length to start.
          start += includedLocation.Length;
        } else if (includedLocation.StartIndex <= sourceLocation.StartIndex) {
          if (includedLocation.StartIndex+includedLocation.Length >= sourceLocation.StartIndex+sourceLocation.Length) {
            //The included location overlaps the entire source location
            start += sourceLocation.StartIndex-includedLocation.StartIndex;
            length = sourceLocation.Length;
            break;
          } else {
            //The included location overlaps a prefix of the source location
            start += sourceLocation.StartIndex-includedLocation.StartIndex;
            length += includedLocation.Length-(sourceLocation.StartIndex-includedLocation.StartIndex);
          }
        } else if (includedLocation.StartIndex >= sourceLocation.StartIndex+sourceLocation.Length) {
          //The included location occurs after the end of the source location.
          break;
        } else {
          if (includedLocation.StartIndex+includedLocation.Length < sourceLocation.StartIndex+sourceLocation.Length) {
            //The included location is contained within the source location.
            length += includedLocation.Length;
          } else {
            //The included location overlaps a suffix of the source location
            length += includedLocation.StartIndex-sourceLocation.StartIndex;
            break;
          }
        }
      }
      return this.GetSourceLocation(start, length);
    }

    /// <summary>
    /// Returns a new preprocessed document of the same type as this document, but using the given helper and underlying document to preprocess.
    /// </summary>
    /// <param name="helper">A C# specific helper object that is used to provide the value of the CompilationPart property.</param>
    /// <param name="documentToPreprocess">The unpreprocessed document on which the newly allocated document should be based.</param>
    protected abstract CSharpCompositeDocument<PrimaryDocumentType, VersionType> GetNewVersion(CSharpCompilationHelper helper, PrimaryDocumentType documentToPreprocess);
    // ^ ensures result.GetType() == this.GetType(); //TODO: this crashes the non null analyzer

    /// <summary>
    /// Returns a new version of this.DocumentToPreprocess where the substring designated by position and length has been replaced by the given replacement text.
    /// </summary>
    /// <param name="position">The index of the first character to replace.</param>
    /// <param name="length">The number of characters to replace.</param>
    /// <param name="updatedText">The replacement string.</param>
    /// <param name="version">A version object (may be null) to associate with the result.</param>
    protected abstract PrimaryDocumentType/*!*/ GetNextVersionOfDocumentToPreprocess(int position, int length, string updatedText, VersionType version);
    // ^ requires 0 <= position && position < this.DocumentToPreprocess.Length;
    // ^ requires 0 <= length && length <= this.DocumentToPreprocess.Length;
    // ^ requires 0 <= position+length && position+length <= this.DocumentToPreprocess.Length;

    /// <summary>
    /// Returns a new version of this document where the substring designated by position and length has been replaced by the given replacement text.
    /// </summary>
    /// <param name="helper">A C# specific helper object that is used to provide the value of the CompilationPart property.</param>
    /// <param name="nextVersionOfDocumentToPreprocess">The unpreprocessed document on which the newly allocated document should be based.</param>
    /// <param name="nextVersionOfPreprocessorInformation">A preprocessing information object that was incrementally derived from it previous version.</param>
    /// <param name="position">The first character in the previous version of the new document that will be changed in the new document.</param>
    /// <param name="oldLength">The number of characters in the previous verion of the new document that will be changed in the new document.</param>
    /// <param name="newLength">The number of replacement characters in the new document. 
    /// (The length of the string that replaces the substring from position to position+length in the previous version of the new document.)</param>
    protected abstract CSharpCompositeDocument<PrimaryDocumentType, VersionType> GetNextVersion(CSharpCompilationHelper helper,
      PrimaryDocumentType nextVersionOfDocumentToPreprocess, PreprocessorInformation/*?*/ nextVersionOfPreprocessorInformation, int position, int oldLength, int newLength);
    // ^ ensures result.GetType() == this.GetType(); //TODO: this crashes the non null analyzer

    protected override IEnumerable<ISourceLocation> GetFragments() {
      if (this.preprocessorInformation != null)
        return this.preprocessorInformation.IncludedLocations; //should get here when constructing a PDB
      if (this.preprocessor == null) {
        //Fast case for constructing symbol table in VS
        Preprocessor preprocessor = this.GetAndCacheNewPreprocessor(); //avoid calling this.PreprocessorInformation as it will process the entire document before returning.
        return preprocessor.GetIncludedSections();
      }
      //Should only get here when getting source location information about an error.
      return this.PreprocessorInformation.IncludedLocations;
    }

    private Preprocessor GetAndCacheNewPreprocessor()
      //^ ensures this.preprocessor == result;
    {
      return this.preprocessor = new Preprocessor(this.DocumentToPreprocess, (CSharpOptions)this.CSharpCompilationPart.Compilation.Options);
    }

    protected CSharpCompilationHelper Helper {
      get { return this.helper; }
    }
    readonly CSharpCompilationHelper helper;

    private Preprocessor Preprocessor {
      get
        //^ ensures result.PreprocessingIsComplete;
      {
        if (this.preprocessor == null || !this.preprocessor.PreprocessingIsComplete) {
          lock (GlobalLock.LockingObject) {
            if (this.preprocessor == null || !this.preprocessor.PreprocessingIsComplete) {
              Preprocessor preprocessor = this.GetAndCacheNewPreprocessor();
              //^ assert this.preprocessor != null;
              for (IEnumerator<ISourceLocation> includedSectionEnumerator = preprocessor.GetIncludedSections().GetEnumerator(); includedSectionEnumerator.MoveNext(); ) ;
              //^ assume this.preprocessor.PreprocessingIsComplete;
            }
          }
        }
        return this.preprocessor; 
      }
    }
    private Preprocessor/*?*/ preprocessor;

    internal override List<IErrorMessage> PreprocessorErrors {
      get {
        if (this.preprocessor == null) {
          lock (GlobalLock.LockingObject) {
            if (this.preprocessor == null) {
              if (this.preprocessorInformation == null)
                return new List<IErrorMessage>(0);
              return this.preprocessorInformation.errors;
            }
          }
        }
        return this.preprocessor.errors;
      }
    }

    public PreprocessorInformation PreprocessorInformation {
      get {
        if (this.preprocessorInformation == null) {
          lock (GlobalLock.LockingObject) {
            if (this.preprocessorInformation == null)
              this.preprocessorInformation = this.Preprocessor.PreprocessorInformation;
          }
        }
        return this.preprocessorInformation;
      }
    }
    private PreprocessorInformation/*?*/ preprocessorInformation;

    internal override List<IErrorMessage> ScannerAndParserErrors {
      get { return this.scannerAndParserErrors; }
    }
    private readonly List<IErrorMessage> scannerAndParserErrors = new List<IErrorMessage>();

    public override string SourceLanguage {
      get { return "C#"; }
    }

    internal override ISourceDocument UnpreprocessedDocument {
      get { return this.documentToPreprocess; }
    }

  }

  public sealed class CSharpSourceDocument : CSharpCompositeDocument<CSharpUnpreprocessedSourceDocument, object> {

    public CSharpSourceDocument(CSharpCompilationHelper helper, IName name, string location, System.IO.StreamReader streamReader)
      : base(helper, new CSharpUnpreprocessedSourceDocument(helper, name, location, streamReader)) {
    }

    public CSharpSourceDocument(CSharpCompilationHelper helper, IName name, string location, string text)
      : base(helper, new CSharpUnpreprocessedSourceDocument(helper, name, location, text)) {
    }

    private CSharpSourceDocument(CSharpCompilationHelper helper, CSharpUnpreprocessedSourceDocument documentToPreprocess)
      : base(helper, documentToPreprocess) {
    }

    private CSharpSourceDocument(CSharpCompilationHelper helper, CSharpUnpreprocessedSourceDocument nextVersionOfDocumentToPreprocess, PreprocessorInformation/*?*/ nextVersionOfPreprocessorInformation,
      CSharpSourceDocument previousVersion, int position, int oldLength, int newLength)
      : base(helper, nextVersionOfDocumentToPreprocess, nextVersionOfPreprocessorInformation, previousVersion, position, oldLength, newLength) {
    }

    /// <summary>
    /// Returns a source document edit that represents the replacement of the text of this.DocumentToPreprocess from position to position+length with updatedText.
    /// I.e. the given position and length corresponds to the text of the document before preprocessing, but the resulting
    /// edit is an edit to the document after preprocessing (i.e. this document, not the one that preprocessor consumes).
    /// </summary>
    /// <param name="position">The position in this.DocumentToPreprocess, of the first character to be replaced by this edit.</param>
    /// <param name="length">The number of characters in this.DocumentToPreprocess that will be replaced by this edit.</param>
    /// <param name="updatedText">The replacement string.</param>
    public ICSharpSourceDocument GetUpdatedDocument(int position, int length, string updatedText)
      //^ requires 0 <= position && position < this.DocumentToPreprocess.Length;
      //^ requires 0 <= length && length <= this.DocumentToPreprocess.Length;
      //^ requires 0 <= position+length && position+length <= this.DocumentToPreprocess.Length;
      //^ ensures result.IsUpdatedVersionOf(this);
    {
      return base.GetUpdatedDocument(position, length, updatedText, this);
    }

    /// <summary>
    /// Returns a new preprocessed document of the same type as this document, but using the given helper and underlying document to preprocess.
    /// </summary>
    /// <param name="helper">A C# specific helper object that is used to provide the value of the CompilationPart property.</param>
    /// <param name="documentToPreprocess">The unpreprocessed document on which the newly allocated document should be based.</param>
    protected override CSharpCompositeDocument<CSharpUnpreprocessedSourceDocument, object> GetNewVersion(CSharpCompilationHelper helper, CSharpUnpreprocessedSourceDocument documentToPreprocess) {
      return new CSharpSourceDocument(helper, documentToPreprocess);
    }

    protected override CSharpUnpreprocessedSourceDocument GetNextVersionOfDocumentToPreprocess(int position, int length, string updatedText, object version) 
      //^^ requires 0 <= position && position < this.DocumentToPreprocess.Length;
      //^^ requires 0 <= length && length <= this.DocumentToPreprocess.Length;
      //^^ requires 0 <= position+length && position+length <= this.DocumentToPreprocess.Length;
    {
      CSharpUnpreprocessedSourceDocument documentToPreprocess = this.DocumentToPreprocess;
      //^ assume 0 <= position && position < documentToPreprocess.Length; //follows from precondition
      //^ assume 0 <= length && length <= this.DocumentToPreprocess.Length; //follows from precondition
      //^ assume 0 <= position+length && position+length <= this.DocumentToPreprocess.Length;
      return documentToPreprocess.GetUpdatedDocument(position, length, updatedText);
    }

    protected override CSharpCompositeDocument<CSharpUnpreprocessedSourceDocument, object> GetNextVersion(CSharpCompilationHelper helper,
      CSharpUnpreprocessedSourceDocument nextVersionOfDocumentToPreprocess, PreprocessorInformation/*?*/ nextVersionOfPreprocessorInformation, int position, int oldLength, int newLength)
      //^^ ensures result.GetType() == this.GetType();
    {
      return new CSharpSourceDocument(helper, nextVersionOfDocumentToPreprocess, nextVersionOfPreprocessorInformation, this, position, oldLength, newLength); 
    }

    /// <summary>
    /// Makes a shallow copy of this source document (creating a new
    /// </summary>
    public override CSharpCompositeDocument MakeShallowCopyFor(Compilation targetCompilation) {
      CSharpCompilationHelper helperCopy = (CSharpCompilationHelper)this.Helper.MakeShallowCopyFor(targetCompilation);
      PreprocessorInformation/*?*/ nextVersionOfPreprocessorInformation = new PreprocessorInformation(this.DocumentToPreprocess, this.PreprocessorInformation);
      return new CSharpSourceDocument(helperCopy, this.DocumentToPreprocess, nextVersionOfPreprocessorInformation, this, 0, 0, 0);
    }

    internal override ISourceDocument UnpreprocessedDocument {
      get { return this.DocumentToPreprocess; }
    }
  }

  public sealed class DummyCSharpCompilation : Compilation {

    public DummyCSharpCompilation(ICompilation compilation, IMetadataHost compilationHost)
      : base(new DummyEditHostEnvironment(compilationHost), new DummyUnit(compilation, compilationHost), new CSharpOptions()) {
    }

    protected override List<CompilationPart> GetPartList() {
      return new List<CompilationPart>(0);
    }

    public override Compilation UpdateCompilationParts(IEnumerable<CompilationPart> parts) {
      return this;
    }
  }

  internal sealed class DummyEditHostEnvironment : SourceEditHostEnvironment {

    internal DummyEditHostEnvironment(IMetadataHost compilationHost)
      : base(compilationHost.NameTable, 4) {
      this.compilationHost = compilationHost;
    }

    readonly IMetadataHost compilationHost;

    public override IUnit LoadUnitFrom(string location) {
      return this.compilationHost.LoadUnitFrom(location);
    }
  }

  internal sealed class CSharpErrorMessage : ErrorMessage {

    public CSharpErrorMessage(ISourceLocation sourceLocation, long code, string messageKey, params string[] messageArguments)
      : base(sourceLocation, code, messageKey, messageArguments) {
    }

    public override object ErrorReporter {
      get { return Microsoft.Cci.CSharp.ErrorReporter.Instance; }
    }

    public override string ErrorReporterIdentifier {
      get { return "CS"; }
    }

    public override bool IsWarning {
      get {
        return this.Severity != 0; //TODO: check options
      }
    }

    public override ISourceErrorMessage MakeShallowCopy(ISourceDocument targetDocument)
      //^^ requires targetDocument == this.SourceLocation.SourceDocument || targetDocument.IsUpdatedVersionOf(this.SourceLocation.SourceDocument);
      //^^ ensures targetDocument == this.SourceLocation.SourceDocument ==> result == this;
    {
      if (this.SourceLocation.SourceDocument == targetDocument) return this;
      ISourceLocation sloc = this.SourceLocation;
      //^ assume targetDocument.IsUpdatedVersionOf(sloc.SourceDocument); //follows from precondition
      return new CSharpErrorMessage(targetDocument.GetCorrespondingSourceLocation(sloc), this.Code, this.MessageKey, this.MessageArguments());
    }

    public override string Message {
      get {
        ResourceManager resourceManager = new ResourceManager("Microsoft.Cci.CSharp.ErrorMessages", this.GetType().Assembly);
        return base.GetMessage(resourceManager);
      }
    }

    public int Severity {
      get {
        switch ((Error)this.Code) {
          case Error.AlwaysNull: return 2;
          case Error.AttributeLocationOnBadDeclaration: return 1;
          case Error.BadBox: return 1;
          case Error.BadNonEmptyStream: return 1;
          case Error.BadNonNull: return 1;
          case Error.BadNonNullOnStream: return 1;
          case Error.BadRefCompareLeft: return 2;
          case Error.BadRefCompareRight: return 2;
          case Error.BadStream: return 1;
          case Error.BadStreamOnNonNullStream: return 1;
          case Error.BitwiseOrSignExtend: return 3;
          case Error.CLSNotOnModules: return 1;
          case Error.DeprecatedSymbol: return 2;
          case Error.DeprecatedSymbolStr: return 2;
          case Error.DuplicateUsing: return 3;
          case Error.EmptySwitch: return 1;
          case Error.EqualityOpWithoutEquals: return 3;
          case Error.EqualityOpWithoutGetHashCode: return 3;
          case Error.ExpressionIsAlreadyOfThisType: return 4;
          case Error.ExternMethodNoImplementation: return 1;
          case Error.InvalidAttributeLocation: return 1;
          case Error.InvalidMainSig: return 4;
          case Error.IsAlwaysFalse: return 1;
          case Error.IsAlwaysTrue: return 1;
          case Error.LowercaseEllSuffix: return 4;
          case Error.MainCantBeGeneric: return 4;
          case Error.MultipleTypeDefs: return 1;
          case Error.NewOrOverrideExpected: return 2;
          case Error.NewNotRequired: return 4;
          case Error.NewRequired: return 1;
          case Error.NonObsoleteOverridingObsolete: return 1;
          case Error.PossibleMistakenNullStatement: return 3;
          case Error.ProtectedInSealed: return 4;
          case Error.RedundantBox: return 1;
          case Error.RedundantNonNull: return 1;
          case Error.RedundantStream: return 1;
          case Error.RelatedErrorLocation: return -1;
          case Error.RelatedErrorModule: return -1;
          case Error.RelatedWarningLocation: return -1;
          case Error.RelatedWarningModule: return -1;
          case Error.SealedTypeIsAlreadyInvariant: return 1;
          case Error.UnknownEntity: return 2;
          case Error.UnreachableCode: return 2;
          case Error.UnreferencedLabel: return 2;
          case Error.UnreferencedVarAssg: return 2;
          case Error.UseDefViolationField: return 1;
          case Error.UseSwitchInsteadOfAttribute: return 1;
          case Error.VacuousIntegralComp: return 2;
          case Error.ValueTypeIsAlreadyInvariant: return 1;
          case Error.ValueTypeIsAlreadyNonNull: return 1;
          case Error.VolatileByRef: return 1;
          case Error.WarningDirective: return 2;

          case Error.CoercionToNonNullTypeMightFail: return 2;
          case Error.CannotCoerceNullToNonNullType: return 1;
          case Error.ReceiverMightBeNull: return 2;
          case Error.ReceiverCannotBeNull: return 1;
          case Error.UseOfNullPointer: return 1;
          case Error.UseOfPossiblyNullPointer: return 2;

          case Error.CannotLoadShadowedAssembly: return 2;
          case Error.TypeMissingInShadowedAssembly: return 2;
          case Error.MethodMissingInShadowedAssembly: return 2;

          case Error.NonNullFieldNotInitializedBeforeConstructorCall: return 2;
          case Error.AccessThroughDelayedReference: return 2;
          case Error.StoreIntoLessDelayedLocation: return 2;
          case Error.NonNullFieldNotInitializedAtEndOfDelayedConstructor: return 2;
          case Error.BaseNotInitialized: return 2;
          case Error.BaseMultipleInitialization: return 2;
          case Error.ActualCannotBeDelayed: return 2;
          case Error.DelayedReferenceByReference: return 2;
          case Error.DelayedRefParameter: return 2;
          case Error.DelayedStructConstructor: return 2;
          case Error.ActualMustBeDelayed: return 2;
          case Error.ReceiverCannotBeDelayed: return 2;
          case Error.ReceiverMustBeDelayed: return 2;
          case Error.NonNullFieldNotInitializedByDefaultConstructor: return 2;
          case Error.AccessThroughDelayedThisInConstructor: return 2;

          case Error.GenericWarning: return 4;

        }
        return 0;
      }
    }

  }

}
