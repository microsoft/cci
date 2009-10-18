using System;
using Microsoft.Cci;
using Microsoft.Cci.Ast;
using Microsoft.Cci.Contracts;
using System.IO;
using System.Collections.Generic;

namespace HelloAST {
  class Program {
    static void Main(string[] args) {
      var nameTable = new NameTable();
      var host = new HelloHost();
      var coreAssembly = host.LoadAssembly(host.CoreAssemblySymbolicIdentity);

      var aname = nameTable.GetNameFor("hello");
      var mname = nameTable.GetNameFor("hello.exe");
      var arefs = IteratorHelper.GetSingletonEnumerable<IAssemblyReference>(coreAssembly);
      var source = new HelloSourceDocument(aname);
      var sources = IteratorHelper.GetSingletonEnumerable<HelloSourceDocument>(source);

      var helloAssembly = new HelloAssembly(aname, "nowhere", host, mname, arefs, sources);

      Stream peStream = File.Create("hello.exe");
      PeWriter.WritePeToStream(helloAssembly, host, peStream);      

    }

    class HelloAssembly : Assembly {

      ISourceEditHost host;
      IEnumerable<HelloSourceDocument> programSources;

      internal HelloAssembly(IName name, string location, ISourceEditHost host, IName moduleName, 
        IEnumerable<IAssemblyReference> assemblyReferences, IEnumerable<HelloSourceDocument> programSources)
        : base(name, location, moduleName, assemblyReferences, IteratorHelper.GetEmptyEnumerable<IModuleReference>(), 
        IteratorHelper.GetEmptyEnumerable<IResourceReference>(), IteratorHelper.GetEmptyEnumerable<IFileReference>()) {
        this.host = host;
        this.programSources = programSources;
      }

      protected override RootUnitNamespace CreateRootNamespace() {
        return new RootUnitNamespace(this.Compilation.NameTable.EmptyName, this);
      }

      public override IMethodReference EntryPoint {
        get {
          var test = UnitHelper.FindType(this.host.NameTable, this, "Test");
          return TypeHelper.GetMethod(test, this.host.NameTable.GetNameFor("Main"));
        }
      }

      public override Compilation Compilation {
        get {
          if (this.compilation == null) {
            this.compilation = new HelloCompilation(this.host, this, this.programSources);
          }
          return this.compilation;
        }
      }
      Compilation compilation;

      public override void Dispatch(IMetadataVisitor visitor) {
        visitor.Visit(this);
      }
    }

    class HelloCompilation : Compilation {

      internal HelloCompilation(ISourceEditHost hostEnvironment, Unit result, IEnumerable<HelloSourceDocument> sources)
        : base(hostEnvironment, result, new FrameworkOptions()) {
        var helper = new LanguageSpecificCompilationHelper(this, "hello");
        foreach (var source in sources)
          this.partList.Add(new HelloCompilationPart(helper, source.SourceLocation));
      }

      protected override List<CompilationPart> GetPartList() {
        return this.partList;
      }
      List<CompilationPart> partList = new List<CompilationPart>();

      public override Compilation UpdateCompilationParts(IEnumerable<CompilationPart> parts) {
        throw new NotImplementedException();
      }
    }

    class HelloCompilationPart : CompilationPart {

      internal HelloCompilationPart(LanguageSpecificCompilationHelper helper, ISourceLocation sourceLocation)
        : base(helper, sourceLocation) {
      }

      private RootNamespaceDeclaration ConstructRootNamespace() {
        var nameTable = this.Helper.Compilation.NameTable;

        var namespaceMembers = new List<INamespaceDeclarationMember>(1);
        var result = new RootNamespaceDeclaration(this, null, namespaceMembers, this.sourceLocation);

        var typeName = nameTable.GetNameFor("Test");
        var typeNameDeclaration = new NameDeclaration(typeName, this.sourceLocation);
        var baseTypes = new List<TypeExpression>(0);
        var typeMembers = new List<ITypeDeclarationMember>(1);
        var classDeclaraton = new NamespaceClassDeclaration(null, TypeDeclaration.Flags.None, typeNameDeclaration, null,
          baseTypes, typeMembers, this.sourceLocation);
        namespaceMembers.Add(classDeclaraton);

        var voidExpr = TypeExpression.For(this.Helper.Compilation.PlatformType.SystemVoid);
        var methodName = nameTable.GetNameFor("Main");
        var methodNameDeclaration = new NameDeclaration(methodName, this.sourceLocation);
        var statements = new List<Statement>(1);
        var body = new BlockStatement(statements, this.sourceLocation);
        var methodDeclaration = new MethodDeclaration(null, MethodDeclaration.Flags.Static, TypeMemberVisibility.Public, voidExpr, null,
          methodNameDeclaration, null, null, null, body, this.sourceLocation);
        typeMembers.Add(methodDeclaration);

        var system = new SimpleName(nameTable.GetNameFor("System"), this.sourceLocation, false);
        var console = new SimpleName(nameTable.GetNameFor("Console"), this.sourceLocation, false);
        var systemConsole = new QualifiedName(system, console, this.sourceLocation);
        var writeLine = new SimpleName(nameTable.GetNameFor("WriteLine"), this.sourceLocation, false);
        var methodToCall = new QualifiedName(systemConsole, writeLine, this.sourceLocation);
        var arguments = new List<Expression>(1);
        arguments.Add(new CompileTimeConstant("hello", this.sourceLocation));
        var methodCall = new MethodCall(methodToCall, arguments, this.sourceLocation);
        var callStatement = new ExpressionStatement(methodCall);
        statements.Add(callStatement);

        return result;
      }

      public override CompilationPart MakeShallowCopyFor(Compilation targetCompilation) {
        throw new NotImplementedException();
      }

      public override RootNamespaceDeclaration RootNamespace {
        get {
          if (this.rootNamespace == null)
            this.rootNamespace = this.ConstructRootNamespace();
          return this.rootNamespace;
        }
      }

      public override CompilationPart UpdateRootNamespace(RootNamespaceDeclaration rootNamespace) {
        throw new NotImplementedException();
      }
    }

    class HelloSourceDocument : SourceDocument {

      internal HelloSourceDocument(IName name)
        : base(name) {
      }

      public override ISourceLocation GetSourceLocation(int position, int length) {
        return SourceDummy.SourceLocation;
      }

      public override int Length {
        get { return 0; }
      }

      public override string Location {
        get { return ""; }
      }

      public override string SourceLanguage {
        get { return "hello"; }
      }

      public override SourceLocation SourceLocation {
        get { return new HelloSourceLocation(this, 0, 0); }
      }

      public override int CopyTo(int position, char[] destination, int destinationOffset, int length) {
        return 0;
      }

      public override string GetText() {
        return "";
      }

      public override void ToLineColumn(int position, out int line, out int column) {
        line = 0;
        column = 0;
      }
    }

    class HelloSourceLocation : SourceLocation {

      internal HelloSourceLocation(HelloSourceDocument sourceDocument, int startIndex, int length)
        : base(startIndex, length) {
        this.sourceDocument = sourceDocument;
      }

      public override ISourceDocument SourceDocument {
        get { return this.sourceDocument; }
      }
      HelloSourceDocument sourceDocument;

    }

    class HelloHost : SourceEditHostEnvironment {
      PeReader peReader;

      internal HelloHost() {
        this.peReader = new PeReader(this);
      }

      public override IUnit LoadUnitFrom(string location) {
        if (!File.Exists(location)) return Dummy.Unit;
        IModule result = this.peReader.OpenModule(BinaryDocument.GetBinaryDocumentForFile(location, this));
        if (result == Dummy.Module) return Dummy.Unit;
        this.RegisterAsLatest(result);
        return result;
      }
    }
  }
}
