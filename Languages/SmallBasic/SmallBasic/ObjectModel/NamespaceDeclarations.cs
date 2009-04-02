//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using Microsoft.Cci.Ast;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci.SmallBasic {

  internal sealed class SmallBasicRootNamespaceDeclaration : RootNamespaceDeclaration {

    internal SmallBasicRootNamespaceDeclaration(SmallBasicCompilationPart compilationPart, ISourceLocation sourceLocation)
      : base(compilationPart, sourceLocation) {
    }

    internal SmallBasicRootNamespaceDeclaration(ISourceLocation sourceLocation)
      : base(null, sourceLocation) {
    }

    /// <summary>
    /// If this namespace declaration has not yet been initialized, parse the source, set the containing nodes of the result
    /// and report any scanning and parsing errors via the host environment. This method is called whenever 
    /// </summary>
    /// <remarks>Not called in incremental scenarios</remarks>
    protected override void InitializeIfNecessary() {
      if (this.isInitialized) return;
      lock (GlobalLock.LockingObject) {
        if (this.isInitialized) return;
        //^ assume this.CompilationPart is CompilationPart; //The constructor ensures this
        SmallBasicCompilationPart cp = (SmallBasicCompilationPart)this.CompilationPart;
        Parser parser = new Parser(this.Compilation.NameTable, this.SourceLocation, new SmallBasicCompilerOptions(), cp.ScannerAndParserErrors); //TODO: get options from Compilation
        this.Parse(parser);
        this.SetContainingNodes();
        ErrorEventArgs errorEventArguments = new ErrorEventArgs(ErrorReporter.Instance, this.SourceLocation, cp.ScannerAndParserErrors.AsReadOnly());
        this.Compilation.HostEnvironment.ReportErrors(errorEventArguments);
        this.isInitialized = true;
      }
    }
    bool isInitialized;

    internal void Parse(Parser parser) {
      this.sourceAttributes = new List<SourceCustomAttribute>(0);
      List<INamespaceDeclarationMember> members = this.members = new List<INamespaceDeclarationMember>(2);
      members.Add(this.CreateNamespaceImport());
      List<ITypeDeclarationMember> typeMembers = new List<ITypeDeclarationMember>();
      RootClassDeclaration rootClass = new RootClassDeclaration(new NameDeclaration(this.Compilation.NameTable.GetNameFor("RootClass"), this.SourceLocation), typeMembers, this.SourceLocation);
      members.Add(rootClass);
      List<Statement> statements = new List<Statement>();
      rootClass.AddStandardMembers(this.Compilation, statements);
      parser.ParseStatements(statements, rootClass);
    }

    private NamespaceImportDeclaration CreateNamespaceImport() {
      NameDeclaration dummyName = new NameDeclaration(Dummy.Name, SourceDummy.SourceLocation);
      SimpleName microsoft = new SimpleName(this.Compilation.NameTable.GetNameFor("Microsoft"), SourceDummy.SourceLocation, false);
      SimpleName smallBasic = new SimpleName(this.Compilation.NameTable.GetNameFor("SmallBasic"), SourceDummy.SourceLocation, false);
      SimpleName library = new SimpleName(this.Compilation.NameTable.GetNameFor("Library"), SourceDummy.SourceLocation, false);
      QualifiedName microsoftSmallBasic = new QualifiedName(microsoft, smallBasic, SourceDummy.SourceLocation);
      QualifiedName microsoftSmallBasicLibrary = new QualifiedName(microsoftSmallBasic, library, SourceDummy.SourceLocation);
      NamespaceReferenceExpression smallBasicLibrary = new NamespaceReferenceExpression(microsoftSmallBasicLibrary, SourceDummy.SourceLocation);
      return new NamespaceImportDeclaration(dummyName, smallBasicLibrary, SourceDummy.SourceLocation);
    }

    internal void Parse(Parser parser, SmallBasicCompilationPart compilationPart) {
      this.Parse(parser);
      this.compilationPart = compilationPart;
      this.SetContainingNodes();
      this.isInitialized = true;
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
    public override NamespaceDeclaration UpdateMembers(List<INamespaceDeclarationMember> members, ISourceDocumentEdit edit) {
      SmallBasicRootNamespaceDeclaration result = new SmallBasicRootNamespaceDeclaration(edit.SourceDocumentAfterEdit.GetCorrespondingSourceLocation(this.SourceLocation));
      result.members = members;
      result.isInitialized = true;
      result.compilationPart = this.CompilationPart.UpdateRootNamespace(result);
      return result;
    }

  }

}
