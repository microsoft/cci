using System;
using System.IO;
using Microsoft.Cci;
using Microsoft.Cci.MutableCodeModel;

namespace HelloCodeModel {
  class Program {
    static void Main(string[] args) {
      var host = new PeReader.DefaultHost();
      var nameTable = host.NameTable;

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
        ContainingTypeDefinition = testClass,
        InternFactory = host.InternFactory,
        IsCil = true,
        IsStatic = true,
        Name = nameTable.GetNameFor("Main"),
        Type = host.PlatformType.SystemVoid,
        Visibility = TypeMemberVisibility.Public,
      };
      assembly.EntryPoint = mainMethod;
      testClass.Methods.Add(mainMethod);

      var ilGenerator = new ILGenerator(host);
      var body = new ILGeneratorMethodBody(ilGenerator, true, 1);
      body.MethodDefinition = mainMethod;
      mainMethod.Body = body;

      var systemConsole = UnitHelper.FindType(nameTable, coreAssembly, "System.Console");
      var writeLine = TypeHelper.GetMethod(systemConsole, nameTable.GetNameFor("WriteLine"), host.PlatformType.SystemString);

      ilGenerator.Emit(OperationCode.Ldstr, "hello");
      ilGenerator.Emit(OperationCode.Call, writeLine);
      ilGenerator.Emit(OperationCode.Ret);

      Stream peStream = File.Create("hello.exe");
      PeWriter.WritePeToStream(assembly, host, peStream);      

    }


  }
}
