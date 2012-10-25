// ==++==
// 
//   
//    Copyright (c) 2012 Microsoft Corporation.  All rights reserved.
//   
//    The use and distribution terms for this software are contained in the file
//    named license.txt, which can be found in the root of this distribution.
//    By using this software in any fashion, you are agreeing to be bound by the
//    terms of this license.
//   
//    You must not remove this notice, or any other, from this software.
//   
// 
// ==--==
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Microsoft.Cci;
using Microsoft.Cci.Analysis;
using Microsoft.Cci.UtilityDataStructures;
using System.IO;

namespace ILtoC {
  partial class Translator {

    internal Translator(IMetadataHost host, IModule module, IAssembly mscorlib, SourceEmitter source, SourceEmitter header, string location, PdbReader/*?*/ pdbReader) {
      Contract.Requires(host != null);
      Contract.Requires(module != null);
      Contract.Requires(mscorlib != null);
      Contract.Requires(source != null);
      Contract.Requires(header != null);
      Contract.Requires(location != null);

      this.host = host;
      this.module = module;
      this.pdbReader = pdbReader;
      this.currentBody = Dummy.MethodBody;
      this.source = source;
      this.header = header;
      this.location = location;
      this.sourceEmitter = header;
      this.arithmeticExceptionType = UnitHelper.FindType(host.NameTable, mscorlib, "System.ArithmeticException");
      this.contextBoundObject = UnitHelper.FindType(host.NameTable, mscorlib, "System.ContextBoundObject");
      this.cRuntimeAttribute = UnitHelper.FindType(host.NameTable, mscorlib, "System.CRuntimeAttribute");
      this.divideByZeroExceptionType = UnitHelper.FindType(host.NameTable, mscorlib, "System.DivideByZeroException");
      this.doNotMangleAttribute = UnitHelper.FindType(host.NameTable, mscorlib, "System.Runtime.CompilerServices.DoNotMangleAttribute");
      this.invalidCastExceptionType = UnitHelper.FindType(host.NameTable, mscorlib, "System.InvalidCastException");
      this.marshalByRefObject = UnitHelper.FindType(host.NameTable, mscorlib, "System.MarshalByRefObject");
      this.overflowExceptionType = UnitHelper.FindType(host.NameTable, mscorlib, "System.OverflowException");
      this.outOfMemoryExceptionType = UnitHelper.FindType(host.NameTable, mscorlib, "System.OutOfMemoryException");
      this.runtimeHelpers = UnitHelper.FindType(host.NameTable, mscorlib, "System.Runtime.CompilerServices.RuntimeHelpers");
      this.runtimeType = UnitHelper.FindType(host.NameTable, mscorlib, "System.RuntimeType");
      this.stackOverflowExceptionType = UnitHelper.FindType(host.NameTable, mscorlib, "System.StackOverflowException");
      this.nullReferenceExceptionType = UnitHelper.FindType(host.NameTable, mscorlib, "System.NullReferenceException");
      this.threadStaticAttribute = UnitHelper.FindType(host.NameTable, mscorlib, "System.ThreadStaticAttribute");
      this.vaListType = UnitHelper.FindType(host.NameTable, mscorlib, "System.CRuntime.va_list");
      this.callFunctionPointer = host.NameTable.GetNameFor("CallFunctionPointer");
      this.callFunctionPointer2 = host.NameTable.GetNameFor("CallFunctionPointer2");
      this.getAs = host.NameTable.GetNameFor("GetAs");
      this.getAsPointer = host.NameTable.GetNameFor("GetAsPointer");
      this.getOffsetToStringData = host.NameTable.GetNameFor("get_OffsetToStringData");
      this.baseClass0 = TypeHelper.GetField(host.PlatformType.SystemType.ResolvedType, host.NameTable.GetNameFor("baseClass0"));
      this.baseClass1 = TypeHelper.GetField(host.PlatformType.SystemType.ResolvedType, host.NameTable.GetNameFor("baseClass1"));
      this.baseClass2 = TypeHelper.GetField(host.PlatformType.SystemType.ResolvedType, host.NameTable.GetNameFor("baseClass2"));
      this.baseClass3 = TypeHelper.GetField(host.PlatformType.SystemType.ResolvedType, host.NameTable.GetNameFor("baseClass3"));
      this.baseClass4 = TypeHelper.GetField(host.PlatformType.SystemType.ResolvedType, host.NameTable.GetNameFor("baseClass4"));
      this.baseClass5 = TypeHelper.GetField(host.PlatformType.SystemType.ResolvedType, host.NameTable.GetNameFor("baseClass5"));
      this.baseClasses6andBeyond = TypeHelper.GetField(host.PlatformType.SystemType.ResolvedType, host.NameTable.GetNameFor("baseClasses6andBeyond"));
      this.directInterfacesField = TypeHelper.GetField(host.PlatformType.SystemType.ResolvedType, host.NameTable.GetNameFor("directlyImplementedInterfaces"));
      this.implementedInterfaceMapField = TypeHelper.GetField(host.PlatformType.SystemType.ResolvedType, host.NameTable.GetNameFor("implementedInterfaceMap"));
      this.genericArgumentsField = TypeHelper.GetField(host.PlatformType.SystemType.ResolvedType, host.NameTable.GetNameFor("genericArguments"));
      this.defaultConstructorField = TypeHelper.GetField(host.PlatformType.SystemType.ResolvedType, host.NameTable.GetNameFor("defaultConstructor"));
      this.interfaceIndexField = TypeHelper.GetField(host.PlatformType.SystemType.ResolvedType, host.NameTable.GetNameFor("interfaceIndex"));
      this.stringEmptyField = TypeHelper.GetField(host.PlatformType.SystemString.ResolvedType, host.NameTable.GetNameFor("Empty"));
      this.typeField = TypeHelper.GetField(host.PlatformType.SystemObject.ResolvedType, host.NameTable.GetNameFor("type"));
      this.delegateTargetField = TypeHelper.GetField(host.PlatformType.SystemDelegate.ResolvedType, host.NameTable.GetNameFor("_target"));
      this.delegateIsStaticField = TypeHelper.GetField(host.PlatformType.SystemDelegate.ResolvedType, host.NameTable.GetNameFor("_isStatic"));
      this.delegateMethodPtrField = TypeHelper.GetField(host.PlatformType.SystemDelegate.ResolvedType, host.NameTable.GetNameFor("_methodPtr"));
    }

    readonly IModule module;
    readonly PdbReader/*?*/ pdbReader;
    IMethodBody currentBody;
    Instruction previousInstruction;
    SourceEmitter sourceEmitter;
    readonly SourceEmitter source;
    readonly SourceEmitter header;
    readonly IMetadataHost host;
    readonly string location;
    readonly INamedTypeDefinition arithmeticExceptionType;
    readonly INamedTypeDefinition contextBoundObject;
    readonly INamedTypeDefinition cRuntimeAttribute;
    readonly INamedTypeDefinition divideByZeroExceptionType;
    readonly INamedTypeDefinition doNotMangleAttribute;
    readonly INamedTypeDefinition invalidCastExceptionType;
    readonly INamedTypeDefinition marshalByRefObject;
    readonly INamedTypeDefinition nullReferenceExceptionType;
    readonly INamedTypeDefinition overflowExceptionType;
    readonly INamedTypeDefinition outOfMemoryExceptionType;
    readonly INamedTypeDefinition runtimeHelpers;
    readonly INamedTypeDefinition runtimeType;
    readonly INamedTypeDefinition stackOverflowExceptionType;
    readonly INamedTypeDefinition threadStaticAttribute;
    readonly INamedTypeDefinition vaListType;
    readonly IName callFunctionPointer;
    readonly IName callFunctionPointer2;
    readonly IName getAs;
    readonly IName getAsPointer;
    readonly IName getOffsetToStringData;
    readonly IFieldDefinition baseClass0;
    readonly IFieldDefinition baseClass1;
    readonly IFieldDefinition baseClass2;
    readonly IFieldDefinition baseClass3;
    readonly IFieldDefinition baseClass4;
    readonly IFieldDefinition baseClass5;
    readonly IFieldDefinition baseClasses6andBeyond;
    readonly IFieldDefinition directInterfacesField;
    readonly IFieldDefinition implementedInterfaceMapField;
    readonly IFieldDefinition genericArgumentsField;
    readonly IFieldDefinition defaultConstructorField;
    readonly IFieldDefinition interfaceIndexField;
    readonly IFieldDefinition stringEmptyField;
    readonly IFieldDefinition typeField;
    readonly IFieldDefinition delegateTargetField;
    readonly IFieldDefinition delegateIsStaticField;
    readonly IFieldDefinition delegateMethodPtrField;

    class Temp { internal string name; internal ITypeReference type; }

    readonly Hashtable<string> mangledFieldName = new Hashtable<string>();

    readonly Hashtable<string> mangledMethodName = new Hashtable<string>();

    readonly Hashtable<string> mangledTypeName = new Hashtable<string>();

    readonly Hashtable<string> sanitizedName = new Hashtable<string>();

    /// <summary>
    /// The keys are the stack depth and type interned key.
    /// </summary>
    readonly DoubleHashtable<Temp> tempForStackSlot = new DoubleHashtable<Temp>();

    class Instruction : Microsoft.Cci.Analysis.Instruction {

      /// <summary>
      /// The temporary to which the result of the instruction must be assigned.
      /// </summary>
      internal Temp result;

      /// <summary>
      /// The instruction that results in the first operand of the operation, if an operand is required.
      /// </summary>
      public new Instruction/*?*/ Operand1 {
        get {
          return base.Operand1 as Instruction;
        }
      }

    }

    readonly List<Temp> temps = new List<Temp>();

    readonly Stack<Temp> operandStack = new Stack<Temp>();

    readonly MultiHashtable<string> exceptionSwitchTables = new MultiHashtable<string>();

    readonly MultiHashtable<IOperationExceptionInformation> finallyHandlersForCatch = new MultiHashtable<IOperationExceptionInformation>();

    bool generateOverflowCheckTemp;

    bool mayThrowException;

    bool needsTempForArrayElementAddress;

    bool hasCallVirt;

    SetOfUints catchHandlerOffsets = new SetOfUints();

    uint imtsize = 40;

    readonly Hashtable depthMap = new Hashtable();

    // We use both a list and a hashtable to hold the vmts. The list is ued to guarantee the order while the HashTable is used for fast lookup.
    readonly Hashtable<List<IMethodReference>> vmts = new Hashtable<List<IMethodReference>>();

    readonly Hashtable<Hashtable> vmtHashTable = new Hashtable<Hashtable>();

    readonly Hashtable<ITypeDefinition> typesWithStaticConstructors = new Hashtable<ITypeDefinition>();

    [ContractInvariantMethod]
    private void ObjectInvariant() {
      Contract.Invariant(this.module != null);
      Contract.Invariant(this.currentBody != null);
      Contract.Invariant(this.sourceEmitter != null);
      Contract.Invariant(this.header != null);
      Contract.Invariant(this.source != null);
      Contract.Invariant(this.host != null);

      Contract.Invariant(this.mangledFieldName != null);
      Contract.Invariant(this.mangledMethodName != null);
      Contract.Invariant(this.mangledTypeName != null);
      Contract.Invariant(this.sanitizedName != null);
      Contract.Invariant(this.tempForStackSlot != null);
      Contract.Invariant(this.temps != null);
      Contract.Invariant(this.operandStack != null);

      Contract.Invariant(this.contextBoundObject != null);
      Contract.Invariant(this.cRuntimeAttribute != null);
      Contract.Invariant(this.doNotMangleAttribute != null);
      Contract.Invariant(this.invalidCastExceptionType != null);
      Contract.Invariant(this.marshalByRefObject != null);
      Contract.Invariant(this.runtimeHelpers != null);
      Contract.Invariant(this.runtimeType != null);
      Contract.Invariant(this.overflowExceptionType != null);
      Contract.Invariant(this.threadStaticAttribute != null);
      Contract.Invariant(this.vaListType != null);
      Contract.Invariant(this.exceptionSwitchTables != null);
      Contract.Invariant(this.finallyHandlersForCatch != null);
      Contract.Invariant(this.location != null);
      Contract.Invariant(this.getAs != null);
      Contract.Invariant(this.getAsPointer != null);
      Contract.Invariant(this.getOffsetToStringData != null);
      Contract.Invariant(this.depthMap != null);
      Contract.Invariant(this.baseClass0 != null);
      Contract.Invariant(this.baseClass1 != null);
      Contract.Invariant(this.baseClass2 != null);
      Contract.Invariant(this.baseClass3 != null);
      Contract.Invariant(this.baseClass4 != null);
      Contract.Invariant(this.baseClass5 != null);
      Contract.Invariant(this.baseClasses6andBeyond != null);
      Contract.Invariant(this.directInterfacesField != null);
      Contract.Invariant(this.implementedInterfaceMapField != null);
      Contract.Invariant(this.interfaceIndexField != null);
      Contract.Invariant(this.genericArgumentsField != null);
      Contract.Invariant(this.defaultConstructorField != null);
      Contract.Invariant(this.stringEmptyField != null);
      Contract.Invariant(this.typeField != null);
      Contract.Invariant(this.delegateTargetField != null);
      Contract.Invariant(this.delegateIsStaticField != null);
      Contract.Invariant(this.delegateMethodPtrField != null);
      Contract.Invariant(this.vmts != null);
      Contract.Invariant(this.vmtHashTable != null);
      Contract.Invariant(this.typesWithStaticConstructors != null);
      Contract.Invariant(this.catchHandlerOffsets != null);
    }

    internal void TranslateToC() {
      this.ExtractResources();
      Hashtable<IGenericMethodInstanceReference> closedGenericMethodInstances;
      var closedStructuralTypeInstances = this.GetAllClosedStructuralTypeInstanceReferencesInThisModule(out closedGenericMethodInstances); //EmitBody depends on its side-effects
      this.EmitBody(closedStructuralTypeInstances);
      this.EmitHeader(closedStructuralTypeInstances, closedGenericMethodInstances);
    }

    private void ExtractResources() {
      ExtractResource("ILtoC.Resources.OverflowChecker.h", Path.Combine(this.location, "OverflowChecker.h"));
      ExtractResource("ILtoC.Resources.OverflowChecker.c", Path.Combine(this.location, "OverflowChecker.c"));
      ExtractResource("ILtoC.Resources.Platform.h", Path.Combine(this.location, "Platform.h"));
      ExtractResource("ILtoC.Resources.Platform.c", Path.Combine(this.location, "Platform.c"));
      ExtractResource("ILtoC.Resources.Platform_msvc.h", Path.Combine(this.location, "Platform_msvc.h"));
      ExtractResource("ILtoC.Resources.Platform_msvc.c", Path.Combine(this.location, "Platform_msvc.c"));
    }

    static void ExtractResource(string resource, string targetFile) {
      System.Reflection.Assembly a = System.Reflection.Assembly.GetExecutingAssembly();
      using (Stream srcStream = a.GetManifestResourceStream(resource)) {
        Contract.Assume(srcStream != null);
        byte[] bytes = new byte[srcStream.Length];
        srcStream.Read(bytes, 0, bytes.Length);
        File.WriteAllBytes(targetFile, bytes);
      }
    }

    private void EmitHeader(Hashtable<ITypeReference> closedStructuralTypeInstances, Hashtable<IGenericMethodInstanceReference> closedGenericMethodInstances) {
      Contract.Requires(closedStructuralTypeInstances != null);
      this.sourceEmitter = this.header;

      var literalStrings = this.module.GetStrings();
       
      this.EmitReferences();
      this.EmitStructs(closedStructuralTypeInstances);
      this.EmitStaticVariables(closedStructuralTypeInstances, literalStrings, doingHeader: true);
      this.EmitMethodSignatures(closedStructuralTypeInstances, closedGenericMethodInstances);
    }

    private void EmitReferences() {
      this.sourceEmitter.EmitString("#include <memory.h>");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("#include <stdlib.h>");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("#include <stdint.h>");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("#include <math.h>");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("#include \"Platform.h\"");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("#include \"OverflowChecker.h\"");
      this.sourceEmitter.EmitNewLine();
      var arefs = new SetOfObjects();
      foreach (var referencedAssembly in this.module.AssemblyReferences) {
        Contract.Assume(referencedAssembly != null);
        var unifiedIdentity = referencedAssembly.UnifiedAssemblyIdentity;
        if (!arefs.Add(unifiedIdentity)) continue;
        var hfile = Path.ChangeExtension(unifiedIdentity.Location, ".h");
        this.sourceEmitter.EmitString("#include \"");
        this.sourceEmitter.EmitString(hfile);
        this.sourceEmitter.EmitString("\"");
        this.sourceEmitter.EmitNewLine();
      }
      foreach (var referencedModule in this.module.ModuleReferences) {
        Contract.Assume(referencedModule != null);
        var hfile = Path.ChangeExtension(referencedModule.ModuleIdentity.Location, ".h");
        this.sourceEmitter.EmitString("#include \"");
        this.sourceEmitter.EmitString(hfile);
        this.sourceEmitter.EmitString("\"");
        this.sourceEmitter.EmitNewLine();
      }
      this.sourceEmitter.EmitString("#define IMTSIZE " + this.imtsize);
      this.sourceEmitter.EmitNewLine();
    }

    private void EmitBody(Hashtable<ITypeReference> closedStructuralTypeInstancesUsedInThisModule) {
      Contract.Requires(closedStructuralTypeInstancesUsedInThisModule != null);

      this.sourceEmitter = this.source;
      IEnumerable<string> literalStrings = null;
      Hashtable<ITypeReference> allClosedStructuralTypeInstances = null;
      Hashtable<IGenericMethodInstanceReference> allClosedGenericMethodInstances = null;
      if (this.module.Kind == ModuleKind.ConsoleApplication || this.module.Kind == ModuleKind.WindowsApplication) {
        //We need one definition of these per executable.

        this.sourceEmitter.EmitString("// Uncomment the following line to enable __debugbreak() in the event of an exception been thrown");
        this.sourceEmitter.EmitNewLine();
        this.sourceEmitter.EmitString("// #define ENABLE_DEBUG_BREAK");
        this.sourceEmitter.EmitNewLine();

        this.sourceEmitter.EmitString("uint32_t appdomain_static_block_size;");
        this.sourceEmitter.EmitNewLine();
        this.sourceEmitter.EmitString("tls_type appdomain_static_block_tlsIndex;");
        this.sourceEmitter.EmitNewLine();
        this.sourceEmitter.EmitString("uint32_t interfaceMethodIDCounter;");
        this.sourceEmitter.EmitNewLine();
        this.sourceEmitter.EmitString("uint8_t* statics;");
        this.sourceEmitter.EmitNewLine();
        this.sourceEmitter.EmitString("uint32_t thread_static_block_size;");
        this.sourceEmitter.EmitNewLine();
        this.sourceEmitter.EmitString("tls_type thread_static_block_tlsIndex;");
        this.sourceEmitter.EmitNewLine();
  
        var allModules = this.GetThisAndAllReferencedModules();
        var allNominalTypes = this.GetAllNominalTypes(allModules);
        allClosedStructuralTypeInstances = this.GetAllClosedStructuralTypeInstanceReferencesIn(allModules, out allClosedGenericMethodInstances);
        literalStrings = this.GetAllLiteralStringsIn(allModules);
        this.EmitAllocatorForStaticVariables(allNominalTypes, allClosedStructuralTypeInstances);
        this.EmitMain();
      } else {
        //Since this module is not the executable, we wont emit static variable definitions for things that are
        //defined by structure (and thus do not live in a particular module). All such things are defined, once,
        //in the the executable. The header for the current module will contain external references to these definitions.
      }

      this.EmitStaticVariables(allClosedStructuralTypeInstances, literalStrings, doingHeader: false);
      this.EmitTypeLoader(closedStructuralTypeInstancesUsedInThisModule, literalStrings);
      this.EmitMethods(allClosedStructuralTypeInstances, allClosedGenericMethodInstances);
    }

    private SetOfObjects GetThisAndAllReferencedModules() {
      SetOfObjects referencedModules = new SetOfObjects();
      referencedModules.Add(this.module);
      this.GetModulesReferenced(referencedModules, this.module);
      return referencedModules;
    }

    private void GetModulesReferenced(SetOfObjects referencedModules, IModule module) {
      Contract.Requires(referencedModules != null);
      Contract.Requires(module != null);

      foreach (var assembly in module.AssemblyReferences) {
        Contract.Assume(assembly != null);
        if (referencedModules.Add(assembly.ResolvedModule))
          this.GetModulesReferenced(referencedModules, assembly.ResolvedAssembly);
      }

      foreach (var referencedModule in module.ModuleReferences) {
        Contract.Assume(referencedModule != null);
        if (referencedModules.Add(referencedModule.ResolvedModule))
          this.GetModulesReferenced(referencedModules, referencedModule.ResolvedModule);
      }
    }

    private IEnumerable<string> GetAllLiteralStringsIn(SetOfObjects modules) {
      Contract.Requires(modules != null);

      var stringSet = new HashSet<string>();
      foreach (IModule module in modules.Values) {
        Contract.Assume(module != null);
        foreach (var str in module.GetStrings())
          stringSet.Add(str);
      }
      return stringSet;
    }

    private IEnumerable<INamedTypeDefinition> GetAllNominalTypes(SetOfObjects modules) {
      Contract.Requires(modules != null);
      foreach (IModule module in modules.Values) {
        Contract.Assume(module != null);
        foreach (var type in module.GetAllTypes())
          yield return type;
      }
    }

  }
}
