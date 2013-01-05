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
using System.IO;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CSharp;
using Xunit;

using Microsoft.Cci;
using Microsoft.Cci.MutableCodeModel;
using Microsoft.Cci.Contracts;
using Microsoft.Cci.ILToCodeModel;
using System.Text;

namespace ResolutionTests {
  public class Program {
    [Fact]
    public void ResolutionTest() {
      ReadMutateAndResolve("Source.cs");
    }

    static void ExtractResource(string resource, string targetFile) {
      System.Reflection.Assembly a = System.Reflection.Assembly.GetExecutingAssembly();
      using (Stream srcStream = a.GetManifestResourceStream(resource)) {
        byte[] bytes = new byte[srcStream.Length];
        srcStream.Read(bytes, 0, bytes.Length);
        File.WriteAllBytes(targetFile, bytes);
      }
    }

    static void ExtractAndCompile(string sourceFile) {
      string assemblyName = Path.ChangeExtension(sourceFile, ".dll");

      string tempFile = Path.GetRandomFileName();
      ExtractResource("GenericResolutionTests.TestData." + sourceFile, tempFile);

      CompilerParameters parameters = new CompilerParameters();
      parameters.GenerateExecutable = Path.GetExtension(assemblyName) == ".exe";
      parameters.IncludeDebugInformation = true;
      parameters.OutputAssembly = assemblyName;

      CompilerResults results;
      using (CodeDomProvider icc = new CSharpCodeProvider()) {
        results = icc.CompileAssemblyFromFile(parameters, tempFile);
      }

      File.Delete(tempFile);

      foreach (var s in results.Errors) {
        Debug.WriteLine(s);
      }

      Assert.Equal(0, results.Errors.Count);
      Assert.True(File.Exists(assemblyName), string.Format("Failed to compile {0} from {1}", assemblyName, sourceFile));
    }

    static void ReadMutateAndResolve(string sourceLocation) {

      ExtractAndCompile(sourceLocation);
      string dllLocation = Path.ChangeExtension(sourceLocation, ".dll");

      using (var host = new PeReader.DefaultHost()) {
        //Read the Metadata Model from the PE file
        var module = host.LoadUnitFrom(dllLocation) as IModule;
        if (module == null || module == Dummy.Module || module == Dummy.Assembly) {
          Console.WriteLine(dllLocation + " is not a PE file containing a CLR module or assembly.");
          return;
        }

        //Get a PDB reader if there is a PDB file.
        PdbReader/*?*/ pdbReader = null;
        string pdbFile = Path.ChangeExtension(module.Location, "pdb");
        if (File.Exists(pdbFile)) {
          Stream pdbStream = File.OpenRead(pdbFile);
          pdbReader = new PdbReader(pdbStream, host);
        }

        module = MetadataCopier.DeepCopy(host, module);
        module = new MetadataRewriter(host).Rewrite(module);
        TestResolution resolver = new TestResolution(host);
        resolver.Visit(module);

        string result = resolver.Output.ToString();
        Assert.True(!result.Contains("Dummy"));
      }
    }

    public class TestResolution : CodeVisitor {

      public StringBuilder Output = new StringBuilder();

      public TestResolution(IMetadataHost host) {
      }

      public override void Visit(IMethodReference methodReference) {
        if (methodReference.Name.Value.StartsWith("bar")) {
          Output.AppendFormat(".... method {0}: {1} of {2}.", methodReference.ResolvedMethod.Name, methodReference, methodReference.ContainingType);
          var itor1 = methodReference.ResolvedMethod.Parameters.GetEnumerator();
          var itor2 = methodReference.Parameters.GetEnumerator();
          while (itor1.MoveNext() && itor2.MoveNext()) {
            string typeString = "Dummy";
            var type1 = itor1.Current.Type;
            var type2 = itor2.Current.Type;
            if (type1.InternedKey == type2.InternedKey) {
              typeString = itor1.Current.Type.ToString();
            }
            Output.AppendFormat("     - parameter: {0} of {1}.", itor1.Current, typeString);
          }
          return;
        }
        base.Visit(methodReference);
      }

      public override void Visit(IFieldReference fieldReference) {
        if (fieldReference.Name.Value.StartsWith("f")) {
          Output.AppendFormat(".... field {0}: {1} of {2}.", fieldReference.ResolvedField.Name, fieldReference, fieldReference.ContainingType);
          Output.AppendFormat("     - resolved field type: {0}", fieldReference.ResolvedField.Type.ResolvedType);
          return;
        }
        base.Visit(fieldReference);
      }
    }
  }

  internal class MyTraceListener : TraceListener {

    public override void Fail(string message, string detailMessage) {
      Console.WriteLine("Fail:");
      Console.WriteLine(message);
      Console.WriteLine();
      Console.WriteLine(detailMessage);

      Assert.True(false, message);
    }

    public override void Fail(string message) {
      Console.WriteLine("Fail:");
      Console.WriteLine(message);

      Assert.True(false, message);
    }

    public override void Write(string message) {
      Console.Write(message);
    }

    public override void WriteLine(string message) {
      Console.WriteLine(message);
    }
  }

}