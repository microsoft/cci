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
using System.Text;
using Microsoft.Cci;
using System.Diagnostics;
using System.IO;

#if !COMPACTFX
using Xunit;
#endif

namespace ModuleReaderTests {

  public class ModuleReaderTestException : System.Exception {
    public ModuleReaderTestException(string testName)
      : base(testName + " Failed!") {
    }
  }

  internal class HostEnvironment : MetadataReaderHost {
    PeReader peReader;
    internal HostEnvironment()
      : base(new NameTable(), new InternFactory(), 0, null, false) {
      this.peReader = new PeReader(this);
    }

    public override IUnit LoadUnitFrom(string location) {
      IUnit result = this.peReader.OpenModule(BinaryDocument.GetBinaryDocumentForFile(location, this));
      this.RegisterAsLatest(result);
      return result;
    }

    /// <summary>
    /// Open the binary document as a memory block in host dependent fashion.
    /// </summary>
    /// <param name="sourceDocument">The binary document that is to be opened.</param>
    /// <returns>The unmanaged memory block corresponding to the source document.</returns>
    public override IBinaryDocumentMemoryBlock/*?*/ OpenBinaryDocument(IBinaryDocument sourceDocument) {
      try {
        IBinaryDocumentMemoryBlock binDocMemoryBlock = UnmanagedBinaryMemoryBlock.CreateUnmanagedBinaryMemoryBlock(sourceDocument.Location, sourceDocument);
        this.disposableObjectAllocatedByThisHost.Add((IDisposable)binDocMemoryBlock);
        return binDocMemoryBlock;
      } catch (IOException) {
        return null;
      }
    }

  }

  internal class Helper {
    internal static bool CompareStringContents(string orignialString, string[] sampleStrings, int[] sampleIndicies) {
      Debug.Assert(sampleStrings.Length == sampleIndicies.Length);
      for (int i = 0; i < sampleIndicies.Length;++i ) {
        int index = orignialString.IndexOf(sampleStrings[i]);
        if (index != sampleIndicies[i])
          return false;
      }
      return true;
    }

    internal static IModule GetModuleForType(ITypeDefinition typeDefinition) {
      INestedTypeDefinition ntd = typeDefinition as INestedTypeDefinition;
      while (ntd != null) {
        typeDefinition = ntd.ContainingTypeDefinition;
        ntd = typeDefinition as INestedTypeDefinition;
      }
      INamespaceTypeDefinition nstd = typeDefinition as INamespaceTypeDefinition;
      if (nstd != null) {
        return nstd.ContainingUnitNamespace.Unit as IModule;
      }
      return null;
    }

    internal static IUnitNamespace GetNamespace(IUnit unit, params IName[] nameList) {
      IUnitNamespace unitNamespace = unit.UnitNamespaceRoot;
      for (int i = 0; i < nameList.Length; ++i) {
        foreach (INamespaceMember nsm in unitNamespace.GetMembersNamed(nameList[i], false)) {
          unitNamespace = nsm as IUnitNamespace;
          if (unitNamespace == null)
            break;
        }
      }
      return unitNamespace;
    }

    internal static IGlobalFieldDefinition GetGlobalField(IUnitNamespace unitNamespace, IName fieldName) {
      foreach (INamespaceMember nm in unitNamespace.GetMembersNamed(fieldName, false)) {
        if (nm is IGlobalFieldDefinition)
          return nm as IGlobalFieldDefinition;
      }
      return null;
    }

    internal static IGlobalMethodDefinition GetGlobalMethod(IUnitNamespace unitNamespace, IName methodName) {
      foreach (INamespaceMember nm in unitNamespace.GetMembersNamed(methodName, false)) {
        if (nm is IGlobalMethodDefinition)
          return nm as IGlobalMethodDefinition;
      }
      return null;
    }

    internal static INamespaceTypeDefinition GetNamespaceType(IUnitNamespace unitNamespace, IName typeName) {
      foreach (INamespaceMember nm in unitNamespace.GetMembersNamed(typeName, false)) {
        if (nm is INamespaceTypeDefinition)
          return nm as INamespaceTypeDefinition;
      }
      return null;
    }

    internal static INestedTypeDefinition GetNestedType(ITypeDefinition typeDefinition, IName typeName) {
      foreach (ITypeDefinitionMember tm in typeDefinition.GetMembersNamed(typeName, false)) {
        if (tm is INestedTypeDefinition)
          return tm as INestedTypeDefinition;
      }
      return null;
    }

    internal static IFieldDefinition GetFieldNamed(ITypeDefinition typeDefinition, IName fieldName) {
      foreach (ITypeDefinitionMember tm in typeDefinition.GetMembersNamed(fieldName, false)) {
        if (tm is IFieldDefinition)
          return tm as IFieldDefinition;
      }
      return null;
    }

    internal static IMethodDefinition GetMethodNamed(ITypeDefinition typeDefinition, IName methodName) {
      foreach (ITypeDefinitionMember tm in typeDefinition.GetMembersNamed(methodName, false)) {
        if (tm is IMethodDefinition)
          return tm as IMethodDefinition;
      }
      return null;
    }

    internal static IEventDefinition GetEventNamed(ITypeDefinition typeDefinition, IName eventName) {
      foreach (ITypeDefinitionMember tm in typeDefinition.GetMembersNamed(eventName, false)) {
        if (tm is IEventDefinition)
          return tm as IEventDefinition;
      }
      return null;
    }

    internal static IPropertyDefinition GetPropertyNamed(ITypeDefinition typeDefinition, IName fieldName) {
      foreach (ITypeDefinitionMember tm in typeDefinition.GetMembersNamed(fieldName, false)) {
        if (tm is IPropertyDefinition)
          return tm as IPropertyDefinition;
      }
      return null;
    }

    internal static string SignatureDefinition(IUnit currentUnit, ISignature signatureDefinition) {
      StringBuilder sb = new StringBuilder();
      sb.Append(Helper.TypeDefinition(currentUnit, signatureDefinition.Type.ResolvedType));
      sb.Append("(");
      bool isNotFirst = false;
      foreach (IParameterDefinition paramDef in signatureDefinition.Parameters) {
        if (isNotFirst) {
          sb.Append(",");
        }
        isNotFirst = true;
        sb.Append(Helper.TypeDefinition(currentUnit, paramDef.Type.ResolvedType));
        if (paramDef.IsByReference)
          sb.Append("&");
      }
      sb.Append(")");
      return sb.ToString();
    }

    internal static string TypeDefinitionMember(IUnit currentUnit, ITypeDefinitionMember typeDefinitionMember) {
      INestedTypeDefinition nestedTypeDefinition = typeDefinitionMember as INestedTypeDefinition;
      if (nestedTypeDefinition != null) {
        StringBuilder sb = new StringBuilder(".class ");
        sb.Append(nestedTypeDefinition.Name.Value);
        return sb.ToString();
      }
      IMethodDefinition methodDefinition = typeDefinitionMember as IMethodDefinition;
      if (methodDefinition != null) {
        StringBuilder sb = new StringBuilder(".method ");
        sb.AppendFormat("{0} : {1}", methodDefinition.Name.Value, Helper.SignatureDefinition(currentUnit, methodDefinition));
        return sb.ToString();
      }
      IFieldDefinition fieldDefinition = typeDefinitionMember as IFieldDefinition;
      if (fieldDefinition != null) {
        StringBuilder sb = new StringBuilder(".field ");
        sb.AppendFormat("{0} : {1}", fieldDefinition.Name.Value, Helper.TypeDefinition(currentUnit, fieldDefinition.Type.ResolvedType));
        return sb.ToString();
      }
      IEventDefinition eventDefinition = typeDefinitionMember as IEventDefinition;
      if (eventDefinition != null) {
        StringBuilder sb = new StringBuilder(".event ");
        sb.AppendFormat("{0} : {1}", eventDefinition.Name.Value, Helper.TypeDefinition(currentUnit, eventDefinition.Type.ResolvedType));
        return sb.ToString();
      }
      IPropertyDefinition propertyDefinition = typeDefinitionMember as IPropertyDefinition;
      if (propertyDefinition != null) {
        StringBuilder sb = new StringBuilder(".property ");
        sb.AppendFormat("{0} : {1}", propertyDefinition.Name.Value, Helper.SignatureDefinition(currentUnit, propertyDefinition));
        return sb.ToString();
      }
      return "!?!Error-TypeMember!?!";
    }

    internal static string Unit(IUnit unit) {
      StringBuilder sb = new StringBuilder();
      IAssembly assembly = unit as IAssembly;
      IModule module = unit as IModule;
      if (assembly != null) {
        sb.AppendFormat("[{0}]", assembly.Name.Value);
      } else if (module != null) {
        sb.AppendFormat("[.module {0}]", module.Name.Value);
      }
      return sb.ToString();
    }

    internal static string ModuleQualifiedUnitNamespace(IUnit currentUnit, IUnitNamespace unitNamespace, out bool wasRoot) {
      INestedUnitNamespace nestedUnitNamespace = unitNamespace as INestedUnitNamespace;
      if (nestedUnitNamespace != null) {
        StringBuilder sb = new StringBuilder(Helper.ModuleQualifiedUnitNamespace(currentUnit, (IUnitNamespace)nestedUnitNamespace.ContainingNamespace, out wasRoot));
        if (!wasRoot) {
          sb.Append(".");
        }
        sb.Append(nestedUnitNamespace.Name.Value);
        wasRoot = false;
        return sb.ToString();
      } else {
        wasRoot = true;
        if (!unitNamespace.Unit.Equals(currentUnit))
          return Helper.Unit(unitNamespace.Unit);
      }
      return string.Empty;
    }

    internal static string TypeDefinition(IUnit currentUnit, ITypeDefinition typeDefinition) {
      if (typeDefinition == Dummy.Type) {
        return "###DummyType###";
      }
      PrimitiveTypeCode ptc = typeDefinition.TypeCode;
      switch (ptc) {
        case PrimitiveTypeCode.Boolean:
          return "bool";
        case PrimitiveTypeCode.Char:
          return "char";
        case PrimitiveTypeCode.Int8:
          return "int8";
        case PrimitiveTypeCode.Float32:
          return "float32";
        case PrimitiveTypeCode.Float64:
          return "float64";
        case PrimitiveTypeCode.Int16:
          return "int16";
        case PrimitiveTypeCode.Int32:
          return "int32";
        case PrimitiveTypeCode.Int64:
          return "int64";
        case PrimitiveTypeCode.IntPtr:
          return "native int";
        case PrimitiveTypeCode.UInt8:
          return "unsigned int8";
        case PrimitiveTypeCode.UInt16:
          return "unsigned int16";
        case PrimitiveTypeCode.UInt32:
          return "unsigned int32";
        case PrimitiveTypeCode.UInt64:
          return "unsigned int64";
        case PrimitiveTypeCode.UIntPtr:
          return "native unsigned int";
        case PrimitiveTypeCode.Void:
          return "void";
      }
      INamespaceTypeDefinition namespaceType = typeDefinition as INamespaceTypeDefinition;
      if (namespaceType != null) {
        bool wasRoot;
        StringBuilder sb = new StringBuilder(Helper.ModuleQualifiedUnitNamespace(currentUnit, (IUnitNamespace)namespaceType.ContainingNamespace, out wasRoot));
        if (!wasRoot) {
          sb.Append(".");
        }
        sb.Append(namespaceType.Name.Value);
        if (namespaceType.GenericParameterCount != 0) {
          sb.Append("`");
          sb.Append(namespaceType.GenericParameterCount);
        }
        return sb.ToString();
      }
      INestedTypeDefinition nestedTypeDefinition = typeDefinition as INestedTypeDefinition;
      if (nestedTypeDefinition != null) {
        StringBuilder sb = new StringBuilder(Helper.TypeDefinition(currentUnit, nestedTypeDefinition.ContainingTypeDefinition));
        sb.Append("/");
        sb.Append(nestedTypeDefinition.Name.Value);
        if (nestedTypeDefinition.GenericParameterCount != 0) {
          sb.Append("`");
          sb.Append(nestedTypeDefinition.GenericParameterCount);
        }
        return sb.ToString();
      }
      IGenericTypeInstanceReference genericTypeInstance = typeDefinition as IGenericTypeInstanceReference;
      if (genericTypeInstance != null) {
        StringBuilder sb = new StringBuilder(Helper.TypeDefinition(currentUnit, genericTypeInstance.GenericType.ResolvedType));
        sb.Append("<");
        bool isNotFirst = false;
        foreach (ITypeReference typeReference in genericTypeInstance.GenericArguments) {
          if (isNotFirst) {
            sb.Append(",");
          }
          isNotFirst = true;
          sb.Append(Helper.TypeDefinition(currentUnit, typeReference.ResolvedType));
        }
        sb.Append(">");
        return sb.ToString();
      }
      IPointerTypeReference pointerType = typeDefinition as IPointerTypeReference;
      if (pointerType != null) {
        StringBuilder sb = new StringBuilder(Helper.TypeDefinition(currentUnit, pointerType.TargetType.ResolvedType));
        sb.Append("*");
        return sb.ToString();
      }
      IArrayTypeReference arrayType = typeDefinition as IArrayTypeReference;
      if (arrayType != null) {
        StringBuilder sb = new StringBuilder(Helper.TypeDefinition(currentUnit, arrayType.ElementType.ResolvedType));
        sb.Append("[");
        if (!arrayType.IsVector) {
          if (arrayType.Rank == 1) {
            sb.Append("*");
          } else {
            for (int i = 1; i < arrayType.Rank; ++i) {
              sb.Append(",");
            }
          }
        }
        sb.Append("]");
        return sb.ToString();
      }
      IGenericTypeParameter genericTypeParameter = typeDefinition as IGenericTypeParameter;
      if (genericTypeParameter != null) {
        return "!" + genericTypeParameter.Index;
      }
      IGenericMethodParameter genericMethodParameter = typeDefinition as IGenericMethodParameter;
      if (genericMethodParameter != null) {
        return "!!" + genericMethodParameter.Index;
      }
      return "!?!ErrorType!?!";
    }

    internal static IOperation GetOperation(IMethodDefinition method, int operationNumber) {
      IMethodBody mb = method.Body;
      foreach (IOperation op in mb.Operations) {
        if (operationNumber == 0)
          return op;
        operationNumber--;
      }
      return null;
    }
  }

  public class ModuleReaderTestClass {
    internal HostEnvironment HostEnv;
    internal INameTable NameTable;
    internal IAssembly MscorlibAssembly;
    internal IAssembly SystemAssembly;
    internal IAssembly VjslibAssembly;
    internal IAssembly AssemblyAssembly;
    internal IAssembly CppAssembly;
    internal IAssembly TestAssembly;
    internal IModule Module1;
    internal IModule Module2;
    internal IAssembly ILAsmAssembly;
    internal IAssembly PhxArchMsil;

    public ModuleReaderTestClass() {
      this.HostEnv = new HostEnvironment();
      this.NameTable = this.HostEnv.NameTable;
      string location = Directory.GetCurrentDirectory();
      string cppAssemblyPath = Path.Combine(location, "MRW_CppAssembly.dll");
      string ilAsmAssemblyPath = Path.Combine(location, "MRW_ILAsmAssembly.dll");
      string module1Path = Path.Combine(location, "MRW_Module1.netmodule");
      string module2Path = Path.Combine(location, "MRW_Module2.netmodule");
      string phxArchMsilPath = Path.Combine(location, "arch-msil.dll");
      string testAssemblyPath = Path.Combine(location, "MRW_TestAssembly.dll");
      string assemblyPath = Path.Combine(location, "MRW_Assembly.dll");
      string vjslibPath = Path.Combine(location, "vjslib.dll");

      ExtractResource("PEReaderTests.TestModules.MRW_CppAssembly.dll", cppAssemblyPath);
      ExtractResource("PEReaderTests.TestModules.MRW_ILAsmAssembly.dll", ilAsmAssemblyPath);
      ExtractResource("PEReaderTests.TestModules.MRW_Module1.netmodule", module1Path);
      ExtractResource("PEReaderTests.TestModules.MRW_Module2.netmodule", module2Path);
      ExtractResource("PEReaderTests.TestModules.arch-msil.dll", phxArchMsilPath);
      ExtractResource("PEReaderTests.TestModules.MRW_TestAssembly.dll", testAssemblyPath);
      ExtractResource("PEReaderTests.TestModules.MRW_Assembly.dll", assemblyPath);
      ExtractResource("PEReaderTests.TestModules.vjslib.dll", vjslibPath);

      DirectoryInfo dirInfo = new DirectoryInfo(Path.GetDirectoryName(typeof(object).Assembly.Location));
      string clrLocation = dirInfo.Parent.FullName + "\\" + "v2.0.50727\\";

      this.MscorlibAssembly = (IAssembly)this.HostEnv.LoadUnitFrom(clrLocation + "mscorlib.dll");
      this.SystemAssembly = (IAssembly)this.HostEnv.LoadUnitFrom(clrLocation + "System.dll");
      this.VjslibAssembly = (IAssembly)this.HostEnv.LoadUnitFrom(vjslibPath);
      this.AssemblyAssembly = (IAssembly)this.HostEnv.LoadUnitFrom(assemblyPath);
      this.CppAssembly = (IAssembly)this.HostEnv.LoadUnitFrom(cppAssemblyPath);
      this.TestAssembly = (IAssembly)this.HostEnv.LoadUnitFrom(testAssemblyPath);
      this.Module1 = (IModule)this.HostEnv.LoadUnitFrom(module1Path);
      this.Module2 = (IModule)this.HostEnv.LoadUnitFrom(module2Path);
      this.ILAsmAssembly = (IAssembly)this.HostEnv.LoadUnitFrom(ilAsmAssemblyPath);
      this.PhxArchMsil = (IAssembly)this.HostEnv.LoadUnitFrom(phxArchMsilPath);
    }

    static void ExtractResource(string resource, string targetFile) {
      System.Reflection.Assembly a = System.Reflection.Assembly.GetExecutingAssembly();
      using (Stream srcStream = a.GetManifestResourceStream(resource)) {
        byte[] bytes = new byte[srcStream.Length];
        srcStream.Read(bytes, 0, bytes.Length);
        File.WriteAllBytes(targetFile, bytes);
      }
    }

    private static void LogAssemblyFile(string name, string path)
    {
        if (!File.Exists(path))
          Console.WriteLine("{0}: {1} - not found", name, path);
    }

    public void MRW_AssemblyTests() {
      AssemblyModuleTests amTests = new AssemblyModuleTests(this);
      Assert.True(amTests.RunAssemblyTests());
    }

    public void MRW_TypeTests() {
      TypeTests typeTests = new TypeTests(this);
      Assert.True(typeTests.RunTypeTests());
    }

    public void MRW_TypeMemberTests() {
      TypeMemberTests typeMemberTests = new TypeMemberTests(this);
      Assert.True(typeMemberTests.RunTypeMemberTests());
    }

    public void MRW_MethodBodyTests() {
      MethodBodyTests methodBodyTests = new MethodBodyTests(this);
      Assert.True(methodBodyTests.RunMethodBodyTests());
    }

    [Fact]
    public void MetadataReaderTests() {
      System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
      this.MRW_AssemblyTests();
      this.MRW_TypeTests();
      this.MRW_TypeMemberTests();
      this.MRW_MethodBodyTests();
      this.HostEnv.Dispose();
    }

  }
}
