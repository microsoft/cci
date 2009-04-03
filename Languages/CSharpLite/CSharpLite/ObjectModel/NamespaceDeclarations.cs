//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using Microsoft.Cci.Ast;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci.SpecSharp {

  internal sealed class SpecSharpRootNamespaceDeclaration : RootNamespaceDeclaration {

    internal SpecSharpRootNamespaceDeclaration(SpecSharpCompilationPart compilationPart, ISourceLocation sourceLocation)
      : base(compilationPart, sourceLocation) 
      //^ requires sourceLocation.SourceDocument is SpecSharpCompositeDocument;
    {
    }

    internal SpecSharpRootNamespaceDeclaration(ISourceLocation sourceLocation)
      : base(null, sourceLocation)
      //^ requires sourceLocation.SourceDocument is SpecSharpCompositeDocument;
    {
    }

    protected override void InitializeIfNecessary()
      //^^ ensures this.members != null;
    {
      if (this.isInitialized) return;
      lock (GlobalLock.LockingObject) {
        if (this.isInitialized) return;
        //^ assume this.CompilationPart is SpecSharpCompilationPart; //The constructor ensures this
        SpecSharpCompilationPart cp = (SpecSharpCompilationPart)this.CompilationPart;
        Parser parser = new Parser(this.Compilation, this.SourceLocation, cp.ScannerAndParserErrors); //TODO: get options from Compilation
        this.Parse(parser);
        this.SetContainingNodes();
        ErrorEventArgs errorEventArguments = new ErrorEventArgs(ErrorReporter.Instance, this.SourceLocation, cp.ScannerAndParserErrors.AsReadOnly());
        this.Compilation.HostEnvironment.ReportErrors(errorEventArguments);
        errorEventArguments = new ErrorEventArgs(ErrorReporter.Instance, cp.UnpreprocessedDocument.SourceLocation, cp.PreprocessorErrors);
        this.Compilation.HostEnvironment.ReportErrors(errorEventArguments);
        this.isInitialized = true;
      }
    }
    bool isInitialized;
    //^ invariant isInitialized ==> this.members != null;

    private void Parse(Parser parser)
      //^ ensures this.members != null;
    {
      List<INamespaceDeclarationMember> members = this.members = new List<INamespaceDeclarationMember>();
      List<SourceCustomAttribute> sourceAttributes = this.sourceAttributes = new List<SourceCustomAttribute>();
      parser.ParseNamespaceBody(members, sourceAttributes);
      members.TrimExcess();
      sourceAttributes.TrimExcess();
      //^ assume this.members != null;
    }

    internal void Parse(Parser parser, SpecSharpCompilationPart compilationPart) {
      this.Parse(parser);
      this.compilationPart = compilationPart;
      this.SetContainingNodes();
      this.isInitialized = true;
    }

    public override NamespaceDeclaration UpdateMembers(List<INamespaceDeclarationMember> members, ISourceDocumentEdit edit)
      //^^ requires edit.SourceDocumentAfterEdit.IsUpdatedVersionOf(this.SourceLocation.SourceDocument);
      //^^ ensures result.GetType() == this.GetType();
    {
      SpecSharpRootNamespaceDeclaration result = new SpecSharpRootNamespaceDeclaration(edit.SourceDocumentAfterEdit.GetCorrespondingSourceLocation(this.SourceLocation));
      result.members = members;
      result.isInitialized = true;
      result.compilationPart = this.CompilationPart.UpdateRootNamespace(result);
      return result;
    }

  }

}
