using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.Cci;
using CSharpSourceEmitter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.CodeDom.Compiler;
using System.IO;
using Microsoft.CSharp;

namespace CSharpSourceEmitterUnitTests {
  /// <summary>
  /// These tests take an existing assembly, decompile it to C# declarations, and then verify that the
  /// C# can be recompiled succesfully without any unexpected errors or warnings.
  /// Ideally we'd also validate that the binaries match semantically, but we're not there yet.
  /// </summary>
  [TestClass]
  public class CSharpBinaryRoundTrip {

    [TestMethod]
    public void DecompileMscorlib() {
      DecompileCompile("mscorlib.dll");
    }

    private void DecompileCompile(string asmName) {
      
      // Extract and load assembly (seems like there should be a way to load directly from a stream,
      // or we should just use the test deployment infrastructure to deploy this binary directly).
      string asmPath = Path.GetFullPath(asmName);
      ExtractResource("CSharpSourceEmitterUnitTests.EmbeddedResources." + asmName, asmPath);

      using (var host = new HostEnvironment()) {
        IAssembly/*?*/ assembly = host.LoadUnitFrom(asmPath) as IAssembly;
        Assert.IsNotNull(assembly);
        Assert.AreNotEqual(Dummy.Assembly, assembly);

        // Now decompile to a file
        string sourcePath = Path.ChangeExtension(asmPath, ".cs");
        using (var sourceWriter = new StreamWriter(sourcePath)) {
          var sourceEmitterOutput = new SourceEmitterOutputTextWriter(sourceWriter);
          SourceEmitter csSourceEmitter = new CSDeclExternSourceEmitter(sourceEmitterOutput, host);
          csSourceEmitter.Traverse(assembly);
        }

        // Now verify that we can recompile it without errors
        string assemblyName = Path.ChangeExtension(sourcePath, ".recompiled.dll");
        CompilerParameters parameters = new CompilerParameters();
        parameters.GenerateExecutable = false;
        parameters.IncludeDebugInformation = true;
        parameters.OutputAssembly = assemblyName;
        parameters.TreatWarningsAsErrors = true;
        parameters.WarningLevel = 4;
        parameters.CompilerOptions = "/unsafe+ /nowarn:0626,0824,0169,0649,0618,0436,3019,0809 /nostdlib+";

        CompilerResults results;
        using (CodeDomProvider icc = new CSharpCodeProvider(new Dictionary<string, string>() { { "CompilerVersion", "v3.5" } })) {
          results = icc.CompileAssemblyFromFile(parameters, sourcePath);
        }

        StringBuilder msg = new StringBuilder(); ;
        if (results.Errors.Count > 0) {
          msg.AppendLine(String.Format("Failed to compile {0}:", sourcePath));
          foreach (var e in results.Errors)
            msg.AppendLine(e.ToString());
        }
        Assert.AreEqual(0, results.Errors.Count, msg.ToString());
        Assert.IsTrue(File.Exists(assemblyName));

        // Eventually we'll want to do some checking on the contents of the binary too
      }
    }

    static void ExtractResource(string resource, string targetFile) {
        System.Reflection.Assembly a = System.Reflection.Assembly.GetExecutingAssembly();
        using (Stream srcStream = a.GetManifestResourceStream(resource)) {
          Assert.IsNotNull(srcStream, "Failed to find resource: " + resource);
          byte[] bytes = new byte[srcStream.Length];
          srcStream.Read(bytes, 0, bytes.Length);
          File.WriteAllBytes(targetFile, bytes);
        }
    }

    public class CSDeclExternSourceEmitter : CSharpSourceEmitter.SourceEmitter {
      public CSDeclExternSourceEmitter(ISourceEmitterOutput sourceEmitterOutput, IMetadataHost host)
        : base(sourceEmitterOutput, host) {
      }

      public override void PrintMethodDefinitionModifiers(IMethodDefinition methodDefinition) {
        // Adds extern to all non-abstract methods, except properties on structs
        base.PrintMethodDefinitionModifiers(methodDefinition);
        if (!methodDefinition.IsExternal && !methodDefinition.IsAbstract && !IsPropertyOnStruct(methodDefinition))
          PrintKeywordExtern();
      }

      public override void Traverse(IMethodBody methodBody) {
        if (IsPropertyOnStruct(methodBody.MethodDefinition))
          sourceEmitterOutput.WriteLine(" {throw new System.NotImplementedException();}");
        else
          PrintToken(CSharpToken.Semicolon);
      }

      private bool IsPropertyOnStruct(IMethodDefinition methodDefinition) {
        if (methodDefinition.ContainingType.IsValueType && methodDefinition.IsSpecialName) {
          string name = methodDefinition.Name.Value;
          if (name.StartsWith("get_") || name.StartsWith("set_"))
            return true;
        }
        return false;
      }

    }

  }
}
