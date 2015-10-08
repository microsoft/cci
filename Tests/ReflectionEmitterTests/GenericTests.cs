using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Cci;
using Microsoft.Cci.ReflectionEmitter;
using Microsoft.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReflectionEmitterTests
{
    [TestClass]
    public class GenericTests
    {
        [TestMethod]
        public void TestGenericForeach()
        {
            Run("GenericForEach");
        }

        [TestMethod]
        public void TestCustomGenericWithSystemParameter()
        {
            Run("CustomGenericWithSystemParameter");
        }

        [TestMethod]
        public void TestSystemGenericWithCustomParameter()
        {
            Run("SystemGenericWithCustomParameter");
        }

        [TestMethod]
        public void TestAnonymousTypes()
        {
            Run("AnonymousTypes");
        }

        static void Run(string sourceName)
        {
            var path = ExtractAndCompile(sourceName);
            try
            {
                Assembly assembly;

                using (var host = new PeReader.DefaultHost())
                using (var pdbStream = File.OpenRead(Path.ChangeExtension(path, ".pdb")))
                {
                    var pdbreader = new PdbReader(pdbStream, host);
                    var loaded = host.LoadAssembly(new AssemblyIdentity(host.NameTable.GetNameFor(Path.GetFileNameWithoutExtension(path)), null, new Version(), null, path));
                    var dynamicLoader = new DynamicLoader(pdbreader, pdbreader);
                    assembly = dynamicLoader.Load(loaded);
                }

                var type = assembly.GetTypes().Single(t => t.Name == sourceName && Attribute.GetCustomAttribute(t, typeof(CompilerGeneratedAttribute)) == null);
                var foo = Activator.CreateInstance(type);
                var method = type.GetMethod("Run");
                var action = (Action)Delegate.CreateDelegate(typeof(Action), foo, method);
                action();
            }
            finally
            {
                File.Delete(path);
            }
        }

        static void ExtractResource(string resource, string targetFile)
        {
            var asm = Assembly.GetExecutingAssembly();
            using (Stream srcStream = asm.GetManifestResourceStream(resource))
            {
                byte[] bytes = new byte[srcStream.Length];
                srcStream.Read(bytes, 0, bytes.Length);
                File.WriteAllBytes(targetFile, bytes);
            }
        }

        static string ExtractAndCompile(string sourceName)
        {
            var sourceFile = Path.GetRandomFileName();
            var asmFile = Path.ChangeExtension(Path.GetRandomFileName(), ".dll");
            try
            {
                ExtractResource("ReflectionEmitterTests.TestData." + sourceName + ".cs", sourceFile);

                var parameters = new CompilerParameters();
                parameters.GenerateExecutable = false;
                parameters.IncludeDebugInformation = true;
                parameters.OutputAssembly = asmFile;
                parameters.ReferencedAssemblies.Add(typeof (Enumerable).Assembly.Location);
                //parameters.CompilerOptions = " /out:" + GetAssemblyPath();

                using (CodeDomProvider icc = new CSharpCodeProvider())
                {
                    icc.CompileAssemblyFromFile(parameters, sourceFile);
                    return asmFile;
                }
            }
            finally
            {
                if (File.Exists(sourceFile))
                {
                    File.Delete(sourceFile);
                }
            }
        }
    }
}
