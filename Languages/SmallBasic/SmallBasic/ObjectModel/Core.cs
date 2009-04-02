//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System.Collections.Generic;
using System.Resources;
using Microsoft.Cci.Ast;
using System.Collections.Specialized;
using System.Diagnostics.SymbolStore;
using System;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci.SmallBasic {

  /// <summary>
  /// An object that represents a source document, such as file, which is parsed by a SmallBasic compiler to produce the SmallBasic specific object model
  /// from which the language agnostic object model can be obtained.
  /// </summary>
  public interface ISmallBasicSourceDocument : ISourceDocument {
    /// <summary>
    /// The SmallBasic compilation part that corresponds to this SmallBasic source document.
    /// </summary>
    SmallBasicCompilationPart SmallBasicCompilationPart {
      get;
      // ^ ensures result.SourceLocation.SourceDocument == this;
    }
  }

  public sealed class SmallBasicCompilation : Compilation {

    /// <summary>
    /// Do not use this constructor unless you are implementing the Compilation property of the Module class.
    /// I.e. to construct a Compilation instance, construct a Module instance and use its Compilation property. 
    /// </summary>
    internal SmallBasicCompilation(ISourceEditHost hostEnvironment, Unit result, IEnumerable<CompilationPart> parts)
      : base(hostEnvironment, result, new FrameworkOptions()) 
      //^ requires result is Module || result is Assembly;
    {
      this.parts = parts;
    }

    protected override List<CompilationPart> GetPartList() {
      return new List<CompilationPart>(this.parts);
    }
    readonly IEnumerable<CompilationPart> parts;

    readonly IDictionary<string,string> options = new Dictionary<string,string>();

    public override Compilation UpdateCompilationParts(IEnumerable<CompilationPart> parts) {
      SmallBasicAssembly/*?*/ oldAssembly = this.Result as SmallBasicAssembly;
      if (oldAssembly != null) {
        SmallBasicAssembly newAssembly = new SmallBasicAssembly(oldAssembly.Name, oldAssembly.Location, this.HostEnvironment, this.options, oldAssembly.AssemblyReferences, oldAssembly.ModuleReferences, parts);
        return (Compilation)newAssembly.Compilation;
      }
      //^ assume this.Result is Module; //follows from constructor precondition and immutability.
      SmallBasicModule oldModule = (SmallBasicModule)this.Result;
      SmallBasicModule newModule = new SmallBasicModule(oldModule.Name, oldModule.Location, this.HostEnvironment, this.options, Dummy.Assembly, oldModule.AssemblyReferences, oldModule.ModuleReferences, parts);
      return newModule.Compilation;
    }
  }

  public sealed class SmallBasicCompilationPart : CompilationPart {

    internal SmallBasicCompilationPart(SmallBasicCompilationHelper helper, ISourceLocation sourceLocation)
      : base(helper, sourceLocation) 
      // ^ requires sourceLocation.SourceDocument is SpecSharpDocument;
    {
    }

    internal SmallBasicCompilationPart(SmallBasicCompilationHelper helper, SmallBasicCompilationPart template)
      : base(helper, template)
    {
    }

    /// <summary>
    /// Makes a shallow copy of this compilation part that can be added to the compilation parts list of the given target compilation.
    /// The shallow copy may share child objects with this instance, but should never expose such child objects except through
    /// wrappers (or shallow copies made on demand). If this instance is already a part of the target compilation it
    /// returns itself.
    /// </summary>
    //^ [MustOverride]
    public override CompilationPart MakeShallowCopyFor(Compilation targetCompilation)
      //^^ ensures result.GetType() == this.GetType();
    {
      if (this.Compilation == targetCompilation) return this;
      LanguageSpecificCompilationHelper helperCopy = this.Helper.MakeShallowCopyFor(targetCompilation);
      //^ assume helperCopy is SmallBasicCompilationHelper; //The constructor ensures that this.Helper is an instance of SmallBasicCompilationHelper
      //The post condition of MakeShallowCopyFor ensures that the copy is also an instance of SmallBasicCompilationHelper.
      return new SmallBasicCompilationPart((SmallBasicCompilationHelper)helperCopy, this.SourceLocation);
    }

    internal Parser Parser {
      get {
        if (this.parser == null)
          this.parser = new Parser(this.Compilation.NameTable, this.SourceLocation, new SmallBasicCompilerOptions(), this.ScannerAndParserErrors); //TODO: get options from Compilation
        return parser;
      }
    }
    Parser/*?*/ parser;

    public override RootNamespaceDeclaration RootNamespace {
      get {
        if (this.rootNamespace == null)
          this.rootNamespace = new SmallBasicRootNamespaceDeclaration(this, this.SourceLocation);
        return this.rootNamespace;
      }
    }

    internal readonly List<IErrorMessage> ScannerAndParserErrors = new List<IErrorMessage>();

    public override CompilationPart UpdateRootNamespace(RootNamespaceDeclaration rootNamespace)
      //^^ requires this.RootNamespace.GetType() == rootNamespace().GetType();
    {
      List<CompilationPart> newParts = new List<CompilationPart>(this.Compilation.Parts);
      Compilation newCompilation = this.Compilation.UpdateCompilationParts(newParts);
      SmallBasicCompilationHelper helper = (SmallBasicCompilationHelper)this.Helper.MakeShallowCopyFor(newCompilation);
      SmallBasicCompilationPart result = new SmallBasicCompilationPart(helper, rootNamespace.SourceLocation);
      result.rootNamespace = rootNamespace;
      for (int i = 0, n = newParts.Count; i < n; i++) {
        if (newParts[i] == this) { newParts[i] = result; break; }
      }
      return result;
    }

    public override CompilationPart UpdateWith(AstSourceDocumentEdit edit, IList<CompilationPart> updatedParts, out EditEventArgs editEventArgs, out EditEventArgs/*?*/ symbolTableEditEventArgs)
      //^^ requires this.SourceLocation.SourceDocument == edit.SourceLocationBeforeEdit.SourceDocument;
      //^^ requires edit.SourceLocationBeforeEdit.SourceDocument.GetType() == edit.SourceDocumentAfterEdit.GetType();
      //^^ requires this.RootNamespace.SourceLocation.Contains(edit.SourceLocationBeforeEdit);
      //^^ ensures result.GetType() == this.GetType();
    {
      SmallBasicCompilationPart result = (SmallBasicCompilationPart)base.UpdateWith(edit, updatedParts, out editEventArgs, out symbolTableEditEventArgs);
      ISourceDocument docAfterEdit = edit.SourceDocumentAfterEdit;
      //^ assume docAfterEdit is SmallBasicDocument; //since it has the same type as this.SourceLocation.SourceDocument and the constructor
      //guarantees that this.SourceLocation.SourceDocument is an instance of SmallBasicDocument.
      SmallBasicDocument doc = (SmallBasicDocument)docAfterEdit;
      doc.smallBasicCompilationPart = result;
      return result;
    }
  }

  public sealed class SmallBasicCompilationHelper : LanguageSpecificCompilationHelper {

    public SmallBasicCompilationHelper(Compilation compilation)
      : base(compilation, "SmallBasic") {
    }

    private SmallBasicCompilationHelper(Compilation targetCompilation, SmallBasicCompilationHelper template) 
      : base(targetCompilation, template) {
    }

    private Expression GetRootClassInstance(CompileTimeConstant labelIndex, RootClassDeclaration rootClass) {
      List<Expression> arguments = new List<Expression>(1);
      arguments.Add(labelIndex);
      foreach (IMethodDefinition constructor in rootClass.TypeDefinition.GetMembersNamed(this.Compilation.NameTable.Ctor, false)) {
        return new CreateObjectInstanceForResolvedConstructor(constructor, arguments, SourceDummy.SourceLocation);
      }
      //^ assume false;
      return labelIndex;
    }

    public override Expression ImplicitConversion(Expression expression, ITypeDefinition targetType) {
      if (targetType.IsDelegate && expression.Type == Dummy.Type) {
        SmallBasicSimpleName/*?*/ labelName = expression as SmallBasicSimpleName;
        if (labelName != null && labelName.rootClass != null) {
          int labelIndex = labelName.rootClass.GetLabelIndex(labelName.Name);
          if (labelIndex > 0) return this.ConvertToDelegate(expression, targetType, labelIndex, labelName.rootClass);
        }
      } else if ((TypeHelper.TypesAreEquivalent(expression.Type, targetType.PlatformType.SystemDecimal) || expression.Type.TypeCode != PrimitiveTypeCode.NotPrimitive) && targetType.TypeCode != PrimitiveTypeCode.NotPrimitive)
        return base.ExplicitConversion(expression, targetType);
      else if (expression.Type.TypeCode != PrimitiveTypeCode.NotPrimitive && (TypeHelper.TypesAreEquivalent(targetType, targetType.PlatformType.SystemDecimal)))
        return base.ExplicitConversion(expression, targetType);
      return base.ImplicitConversion(expression, targetType);
    }

    private Expression ConvertToDelegate(Expression expression, ITypeDefinition targetType, int labelIndex, RootClassDeclaration rootClass) {
      IMethodDefinition invokeMethod = this.GetInvokeMethod(targetType);
      if (invokeMethod != Dummy.Method && invokeMethod.Type.TypeCode == PrimitiveTypeCode.Void && IteratorHelper.EnumerableIsEmpty(invokeMethod.Parameters)) {
        IMethodDefinition matchingMethod = rootClass.MainMethod.MethodDefinition;
        CompileTimeConstant constant = new CompileTimeConstant(labelIndex, SourceDummy.SourceLocation);
        constant.SetContainingExpression(expression);
        Expression instance = this.GetRootClassInstance(constant, rootClass);
        return new CreateDelegateInstance(instance, targetType, matchingMethod, expression.SourceLocation);
      }
      return base.ImplicitConversion(expression, targetType);
    }

    public override LanguageSpecificCompilationHelper MakeShallowCopyFor(Compilation compilation)
      //^^ ensures result.GetType() == this.GetType();
    {
      if (this.Compilation == compilation) return this;
      return new SmallBasicCompilationHelper(compilation, this);
    }

  }

  public sealed class SmallBasicDocument : PrimarySourceDocument, ISmallBasicSourceDocument {

    public SmallBasicDocument(SmallBasicCompilationHelper helper, IName name, string location, System.IO.StreamReader streamReader)
      : base(name, location, streamReader) {
      this.helper = helper;
    }

    public SmallBasicDocument(SmallBasicCompilationHelper helper, IName name, string location, string text)
      : base(name, location, text) {
      this.helper = helper;
    }

    private SmallBasicDocument(SmallBasicCompilationHelper helper, string text, SourceDocument previousVersion, int position, int oldLength, int newLength)
      : base(text, previousVersion, position, oldLength, newLength) {
      this.helper = helper;
    }

    readonly SmallBasicCompilationHelper helper;

    //public override CompilationPart CompilationPart {
    //  get
    //    //^^ ensures result.SourceLocation.SourceDocument == this;
    //  {
    //    return this.SmallBasicCompilationPart;
    //  }
    //}

    public SmallBasicCompilationPart SmallBasicCompilationPart {
      get {
        if (this.smallBasicCompilationPart == null)
          this.smallBasicCompilationPart = new SmallBasicCompilationPart(helper, this.SourceLocation);
        return this.smallBasicCompilationPart;
      }
    }
    internal SmallBasicCompilationPart/*?*/ smallBasicCompilationPart;

    /// <summary>
    /// Obtains a source location instance that corresponds to the substring of the document specified by the given start position and length.
    /// </summary>
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

    //public ISourceDocumentEdit GetDocumentEdit(int position, int length, string updatedText)
    //  //^^ requires 0 <= position && position < this.Length;
    //  //^^ requires 0 <= length && length <= this.Length;
    //  //^^ requires 0 <= position+length && position+length <= this.Length;
    //  //^^ ensures result.SourceLocationBeforeEdit.SourceDocument.IsUpdatedVersionOf(this);
    //{
    //  string oldText = this.GetText();
    //  if (position > oldText.Length) 
    //    position = oldText.Length; //This should only happen if the source document got clobbered after the precondition was established.
    //  //^ assume 0 <= position; //Follows from the precondition and the previous statement
    //  if (position+length > oldText.Length)
    //    length = oldText.Length-position;
    //  //^ assume 0 <= position+length; //established by the precondition and not changed by the previous two statements.
    //  string newText = oldText.Substring(0, position)+updatedText+oldText.Substring(position+length);
    //  SmallBasicDocument newDocument = new SmallBasicDocument(this.helper, this.Name, newText, this, position, length, updatedText.Length);
    //  return new SourceDocumentEdit(this.GetSourceLocation(position, length), newDocument);
    //}

    public override string SourceLanguage {
      get { return "SmallBasic"; }
    }

    public override Guid DocumentType {
      get { return SymDocumentType.Text; }
    }

    public override Guid Language {
      get { return SymLanguageType.Basic; }
    }

    public override Guid LanguageVendor {
      get { return SymLanguageVendor.Microsoft; }
    }
  }

  internal sealed class SmallBasicErrorMessage : ErrorMessage {

    public SmallBasicErrorMessage(ISourceItem sourceItem, long code, string messageKey, params string[] messageArguments)
      : base(sourceItem.SourceLocation, code, messageKey, messageArguments) {
    }

    public SmallBasicErrorMessage(ISourceLocation sourceLocation, long code, string messageKey, params string[] messageArguments)
      : base(sourceLocation, code, messageKey, messageArguments) {
    }

    public override object ErrorReporter {
      get { return Microsoft.Cci.SmallBasic.ErrorReporter.Instance; }
    }

    public override string ErrorReporterIdentifier {
      get { return "SB"; }
    }

    public override ISourceErrorMessage MakeShallowCopy(ISourceDocument targetDocument) {
      if (this.SourceLocation.SourceDocument == targetDocument) return this;
      return new SmallBasicErrorMessage(this.SourceLocation, this.Code, this.MessageKey, this.MessageArguments());
    }

    public override string Message {
      get {
        ResourceManager resourceManager = new ResourceManager("SmallBasic.ErrorMessages", this.GetType().Assembly);
        return base.GetMessage(resourceManager);
      }
    }
  }

}
