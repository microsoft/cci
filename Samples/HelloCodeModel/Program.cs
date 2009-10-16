using System;
using Microsoft.Cci;
using Microsoft.Cci.MutableCodeModel;
using Microsoft.Cci.Contracts;
using System.IO;

namespace HelloCodeModel {
  class Program {
    static void Main(string[] args) {
      var nameTable = new NameTable();
      var host = new PeReader.DefaultHost(nameTable);
      var coreAssembly = host.LoadAssembly(host.CoreAssemblySymbolicIdentity);

      var assembly = new Assembly() {
        Name = nameTable.GetNameFor("hello"),
        ModuleName = nameTable.GetNameFor("hello.exe"),
        Kind = ModuleKind.ConsoleApplication,
        TargetRuntimeVersion = coreAssembly.TargetRuntimeVersion,          
      };
      assembly.AssemblyReferences.Add(coreAssembly);

      var rootUnitNamespace = new RootUnitNamespace();
      assembly.UnitNamespaceRoot = rootUnitNamespace;
      rootUnitNamespace.Unit = assembly;

      var moduleClass = new NamespaceTypeDefinition() {
        ContainingUnitNamespace = rootUnitNamespace,
        InternFactory = host.InternFactory,
        IsClass = true,
        Name = nameTable.GetNameFor("<Module>"),
      };
      assembly.AllTypes.Add(moduleClass);

      var testClass = new NamespaceTypeDefinition() {
        ContainingUnitNamespace = rootUnitNamespace,
        InternFactory = host.InternFactory,
        IsClass = true, 
        IsPublic = true,  
        Name = nameTable.GetNameFor("Test"),
      };
      rootUnitNamespace.Members.Add(testClass);
      assembly.AllTypes.Add(testClass);
      testClass.BaseClasses.Add(host.PlatformType.SystemObject);

      var mainMethod = new MethodDefinition() {
        ContainingType = testClass,
        InternFactory = host.InternFactory,
        IsCil = true,
        IsStatic = true,
        Name = nameTable.GetNameFor("Main"),
        Type = host.PlatformType.SystemVoid,
        Visibility = TypeMemberVisibility.Public,
      };
      assembly.EntryPoint = mainMethod;
      testClass.Methods.Add(mainMethod);

      SourceToILConverterProvider converterProvider = delegate(IMetadataHost mhost, ISourceLocationProvider/*?*/ sourceLocationProvider, IContractProvider/*?*/ contractProvider) {
        return new CodeModelToILConverter(host, sourceLocationProvider, contractProvider);
      };

      var body = new SourceMethodBody(converterProvider, host, null, null) { 
        MethodDefinition = mainMethod,
        LocalsAreZeroed = true 
      };
      mainMethod.Body = body;

      var block = new BlockStatement();
      body.Block = block;

      var systemConsole = UnitHelper.FindType(nameTable, coreAssembly, "System.Console");
      var writeLine = TypeHelper.GetMethod(systemConsole, nameTable.GetNameFor("WriteLine"), host.PlatformType.SystemString);

      var call = new MethodCall() { IsStaticCall = true, MethodToCall = writeLine, Type = host.PlatformType.SystemVoid };
      call.Arguments.Add(new CompileTimeConstant() { Type = host.PlatformType.SystemString, Value = "hello" });
      block.Statements.Add(new ExpressionStatement() { Expression = call });
      block.Statements.Add(new ReturnStatement());

      Stream peStream = File.Create("hello.exe");
      PeWriter.WritePeToStream(assembly, host, peStream);      

    }


  }
}
