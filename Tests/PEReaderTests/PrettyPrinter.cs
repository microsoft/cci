//-----------------------------------------------------------------------------
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the Microsoft Public License.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Microsoft.Cci;

//^ using Microsoft.Contracts;

namespace ModuleReaderTests {
  /// <summary>
  /// Interface for representing the file/string stream/Avalon view on which the ildasm'ed information will be printed.
  /// </summary>
  internal interface IILDasmPaper {
    /// <summary>
    /// Print ILDasm directive on the paper. It is the keywords that start with '.'. For example .assembly.
    /// </summary>
    /// <param name="directive"></param>
    void Directive(string directive);
    /// <summary>
    /// Print ILDasm keyword on the paper. For example: public.
    /// </summary>
    /// <param name="keyword"></param>
    void Keyword(string keyword);
    /// <summary>
    /// Print an identifier on the paper.
    /// </summary>
    /// <param name="identifierName"></param>
    void Identifier(string identifierName);
    /// <summary>
    /// Print a symbol on the paper.
    /// </summary>
    /// <param name="symbol"></param>
    void Symbol(string symbol);
    /// <summary>
    /// Print a constant value.
    /// </summary>
    /// <param name="value"></param>
    void Constant(object/*?*/ value);
    /// <summary>
    /// Print an int in hex format.
    /// </summary>
    /// <param name="value"></param>
    void HexUInt(uint value);
    /// <summary>
    /// Print an int in decimal format.
    /// </summary>
    /// <param name="value"></param>
    void Int(int value);
    /// <summary>
    /// Print an uint in decimal format.
    /// </summary>
    /// <param name="value"></param>
    void UInt(uint value);
    /// <summary>
    /// Print a byte stream
    /// </summary>
    /// <param name="byteStream"></param>
    void ByteStream(IEnumerable<byte> byteStream);
    /// <summary>
    /// Print the string as is.
    /// </summary>
    /// <param name="dataString"></param>
    void DataString(string dataString);
    /// <summary>
    /// Print the comment.
    /// </summary>
    /// <param name="comment"></param>
    void Comment(string comment);
    /// <summary>
    /// Opens a new block. Prints { and indents.
    /// </summary>
    void OpenBlock();
    /// <summary>
    /// Closes a block. Prints } and unindents.
    /// </summary>
    void CloseBlock();
    /// <summary>
    /// Indent up one level.
    /// </summary>
    void Indent();
    /// <summary>
    /// Indent down one level.
    /// </summary>
    void Unindent();
    /// <summary>
    /// Goto a new line.
    /// </summary>
    void NewLine();
  }

  /// <summary>
  /// String based ILDasm paper. When finally done, call Content to get the result.
  /// </summary>
  internal sealed class StringILDasmPaper : IILDasmPaper {
    readonly StringBuilder StringBuilder;
    readonly int IndentSize;
    int CurrentIndent;
    int LastNewLineIndex;
    bool NewLine;
    bool Symbol;

    /// <summary>
    /// Constructor for the String based ILDasm paper
    /// </summary>
    public StringILDasmPaper(int indentSize) {
      StringBuilder = new StringBuilder();
      this.IndentSize = indentSize;
      this.CurrentIndent = 0;
      this.NewLine = true;
      this.Symbol = false;
    }

    /// <summary>
    /// Gives the current content of the string builder.
    /// </summary>
    public string Content {
      get {
        return this.StringBuilder.ToString();
      }
    }

    void EnsureIndentAndSpace() {
      if (this.NewLine) {
        this.LastNewLineIndex = this.StringBuilder.Length;
        this.StringBuilder.Append(new string(' ', this.IndentSize * this.CurrentIndent));
      } else if (!this.Symbol) {
        this.StringBuilder.Append(' ');
      }
    }

    #region IILDasmPaper Members

    void IILDasmPaper.Directive(string directive) {
      this.EnsureIndentAndSpace();
      this.StringBuilder.Append(directive);
      this.NewLine = false;
      this.Symbol = false;
    }

    void IILDasmPaper.Keyword(string keyword) {
      this.EnsureIndentAndSpace();
      this.StringBuilder.Append(keyword);
      this.NewLine = false;
      this.Symbol = false;
    }

    void IILDasmPaper.Identifier(string identifierName) {
      this.EnsureIndentAndSpace();
      this.StringBuilder.Append(identifierName);
      this.NewLine = false;
      this.Symbol = false;
    }

    void IILDasmPaper.Symbol(string symbol) {
      this.Symbol = true;
      this.EnsureIndentAndSpace();
      this.StringBuilder.Append(symbol);
      this.NewLine = false;
    }

    void IILDasmPaper.Constant(object/*?*/ value) {
      this.EnsureIndentAndSpace();
      if (value == null) {
        this.StringBuilder.Append("null");
      } else if (value is char) {
        this.StringBuilder.Append("'" + value + "'");
      } else if (value is string) {
        string str = value as string;
        str.Replace("\r", "\\r");
        str.Replace("\n", "\\n");
        this.StringBuilder.Append("\"" + value + "\"");
      } else {
        this.StringBuilder.Append(value);
      }
      this.NewLine = false;
      this.Symbol = false;
    }

    void IILDasmPaper.HexUInt(uint value) {
      this.EnsureIndentAndSpace();
      this.StringBuilder.AppendFormat("0x{0}", value.ToString("X8"));
      this.NewLine = false;
      this.Symbol = false;
    }

    void IILDasmPaper.Int(int value) {
      this.EnsureIndentAndSpace();
      this.StringBuilder.Append(value);
      this.NewLine = false;
      this.Symbol = false;
    }

    void IILDasmPaper.UInt(uint value) {
      this.EnsureIndentAndSpace();
      this.StringBuilder.Append(value);
      this.NewLine = false;
      this.Symbol = false;
    }

    void IILDasmPaper.ByteStream(IEnumerable<byte> byteStream) {
      this.EnsureIndentAndSpace();
      int indentCount = this.StringBuilder.Length - this.LastNewLineIndex;
      this.StringBuilder.Append('(');
      indentCount++;
      string indent = new string(' ', indentCount);
      int count = 0;
      StringBuilder asciiSb = new StringBuilder(" // ");
      bool isPrintable = false;
      foreach (byte b in byteStream) {
        if ((count & 0x0000000F) == 0x00000000 && count != 0) {
          if (isPrintable) {
            this.StringBuilder.Append(' ');
            this.StringBuilder.AppendLine(asciiSb.ToString());
            isPrintable = false;
          } else {
            this.StringBuilder.AppendLine();
          }
          this.StringBuilder.Append(indent);
          asciiSb = new StringBuilder(" // ");
        }
        this.StringBuilder.Append(b.ToString("X2"));
        this.StringBuilder.Append(' ');
        if (b >= 32 && b < 127) {
          isPrintable = true;
          asciiSb.Append((char)b);
        } else {
          asciiSb.Append('.');
        }
        count++;
      }
      this.StringBuilder.Append(')');
      if (isPrintable) {
        while ((count & 0x0000000F) != 0x00000000) {
          count++;
          this.StringBuilder.Append("   ");
        }
        this.StringBuilder.Append(asciiSb);
      }
      this.NewLine = false;
      this.Symbol = false;
    }

    void IILDasmPaper.DataString(string dataString) {
      this.EnsureIndentAndSpace();
      this.StringBuilder.Append(dataString);
      this.NewLine = false;
      this.Symbol = false;
    }

    void IILDasmPaper.Comment(string comment) {
      this.EnsureIndentAndSpace();
      this.StringBuilder.Append(comment);
      this.NewLine = false;
      this.Symbol = false;
    }

    void IILDasmPaper.OpenBlock() {
      this.EnsureIndentAndSpace();
      this.StringBuilder.AppendLine("{");
      this.CurrentIndent++;
      this.NewLine = true;
      this.Symbol = false;
    }

    void IILDasmPaper.CloseBlock() {
      this.CurrentIndent--;
      Debug.Assert(this.CurrentIndent >= 0);
      this.EnsureIndentAndSpace();
      this.StringBuilder.AppendLine("}");
      this.NewLine = true;
      this.Symbol = false;
    }

    void IILDasmPaper.Indent() {
      this.CurrentIndent++;
    }

    void IILDasmPaper.Unindent() {
      this.CurrentIndent--;
      Debug.Assert(this.CurrentIndent >= 0);
    }

    void IILDasmPaper.NewLine() {
      this.StringBuilder.AppendLine();
      this.NewLine = true;
      this.Symbol = false;
    }

    #endregion
  }

  internal class ILDasmPrettyPrinter {
    readonly IILDasmPaper ILDasmPaper;
    readonly IModule CurrentModule;

    public ILDasmPrettyPrinter(
      IILDasmPaper ildasmPaper,
      IModule currentModule
    ) {
      this.ILDasmPaper = ildasmPaper;
      this.CurrentModule = currentModule;
    }

    public void Version(Version ver) {
      this.ILDasmPaper.Directive(".ver");
      this.ILDasmPaper.Int(ver.Major);
      this.ILDasmPaper.Symbol(":");
      this.ILDasmPaper.Int(ver.Minor);
      this.ILDasmPaper.Symbol(":");
      this.ILDasmPaper.Int(ver.Revision);
      this.ILDasmPaper.Symbol(":");
      this.ILDasmPaper.Int(ver.Build);
    }

    public void Assembly(IAssembly assembly) {
      this.ILDasmPaper.Directive(".assembly");
      this.ILDasmPaper.Identifier(assembly.Name.Value);
      this.ILDasmPaper.NewLine();
      this.ILDasmPaper.OpenBlock();
      this.CustomAttributes(assembly.AssemblyAttributes);
      {
        if (assembly.PublicKey.GetEnumerator().MoveNext()) {
          this.ILDasmPaper.Directive(".publickey");
          this.ILDasmPaper.Symbol(" = ");
          this.ILDasmPaper.ByteStream(assembly.PublicKey);
          this.ILDasmPaper.NewLine();
        }
      }
      {
        this.ILDasmPaper.Directive(".hash algorithm");
        this.ILDasmPaper.HexUInt(0x00008004);
        this.ILDasmPaper.NewLine();
      }
      {
        this.Version(assembly.Version);
        this.ILDasmPaper.NewLine();
      }
      {
        string culture = assembly.Culture;
        if (culture.Length != 0) {
          this.ILDasmPaper.Directive(".culture");
          this.ILDasmPaper.Constant(culture);
          this.ILDasmPaper.NewLine();
        }
      }
      {
        this.ILDasmPaper.Directive(".flags");
        this.ILDasmPaper.HexUInt(assembly.Flags);
        this.ILDasmPaper.NewLine();
      }
      {
        foreach (ISecurityAttribute sa in assembly.SecurityAttributes) {
          this.SecurityAttribute(sa);
        }
      }
      this.ILDasmPaper.CloseBlock();
    }

    public void Module(IModule module) {
      this.ILDasmPaper.Directive(".module");
      this.ILDasmPaper.Identifier(module.Name.Value);
      this.ILDasmPaper.OpenBlock();
      this.CustomAttributes(module.ModuleAttributes);
      {
        this.ILDasmPaper.Directive(".flags");
        if (module.ILOnly) {
          this.ILDasmPaper.Keyword("ilonly");
        }
        if (module.Requires32bits) {
          this.ILDasmPaper.Keyword("bit32");
        }
        if (module.Requires64bits) {
          this.ILDasmPaper.Keyword("bit64");
        }
        if (module.RequiresAmdInstructionSet) {
          this.ILDasmPaper.Keyword("amd");
        }
        if (module.TrackDebugData) {
          this.ILDasmPaper.Keyword("trackdebugdata");
        }
        switch (module.Kind) {
          case ModuleKind.ConsoleApplication:
            this.ILDasmPaper.Keyword("exe");
            break;
          case ModuleKind.WindowsApplication:
            this.ILDasmPaper.Keyword("winexe");
            break;
          case ModuleKind.DynamicallyLinkedLibrary:
            this.ILDasmPaper.Keyword("dll");
            break;
          case ModuleKind.ManifestResourceFile:
            this.ILDasmPaper.Keyword("resource");
            break;
          case ModuleKind.UnmanagedDynamicallyLinkedLibrary:
            this.ILDasmPaper.Keyword("nativedll");
            break;
        }
        this.ILDasmPaper.NewLine();
      }
      {
        this.ILDasmPaper.Directive(".mdversion");
        this.ILDasmPaper.Int(module.MetadataFormatMajorVersion);
        this.ILDasmPaper.Symbol(":");
        this.ILDasmPaper.Int(module.MetadataFormatMinorVersion);
        this.ILDasmPaper.NewLine();
      }
      {
        this.ILDasmPaper.Directive(".guid");
        this.ILDasmPaper.DataString(module.PersistentIdentifier.ToString());
        this.ILDasmPaper.NewLine();
      }
      {
        this.ILDasmPaper.Directive(".runtime");
        this.ILDasmPaper.DataString(module.TargetRuntimeVersion);
        this.ILDasmPaper.NewLine();
      }
      {
        if (module.ContainingAssembly != null) {
          this.ILDasmPaper.Directive(".assembly");
          this.ModuleAsReference(module.ContainingAssembly);
          this.ILDasmPaper.NewLine();
        }
      }
      {
        IMethodReference method = module.EntryPoint;
        if (method != Dummy.MethodReference) {
          this.ILDasmPaper.Directive(".entrypoint");
          this.MethodReference(method);
          this.ILDasmPaper.NewLine();
        }
      }
      this.ILDasmPaper.CloseBlock();
    }

    public void ModuleReference(IModuleReference moduleReference) {
      this.ILDasmPaper.Directive(".module extern");
      this.ILDasmPaper.DataString(moduleReference.Name.Value);
      this.ILDasmPaper.NewLine();
    }

    public void ModuleReferences(IModule module) {
      foreach (IModuleReference moduleReference in module.ModuleReferences) {
        this.ModuleReference(moduleReference);
      }
    }

    public void AssemblyReference(IAssemblyReference assemblyReference) {
      this.ILDasmPaper.Directive(".assembly extern");
      this.ILDasmPaper.Identifier(assemblyReference.Name.Value);
      this.ILDasmPaper.NewLine();
      this.ILDasmPaper.OpenBlock();
      {
        if (assemblyReference.PublicKeyToken.GetEnumerator().MoveNext()) {
          this.ILDasmPaper.Directive(".publickeytoken");
          this.ILDasmPaper.Symbol(" = ");
          this.ILDasmPaper.ByteStream(assemblyReference.PublicKeyToken);
          this.ILDasmPaper.NewLine();
        }
      }
      {
        this.Version(assemblyReference.Version);
        this.ILDasmPaper.NewLine();
      }
      {
        string culture = assemblyReference.Culture;
        if (culture.Length != 0) {
          this.ILDasmPaper.Directive(".culture");
          this.ILDasmPaper.Constant(culture);
          this.ILDasmPaper.NewLine();
        }
      }
      this.ILDasmPaper.CloseBlock();
    }

    public void AssemblyReferences(IModule module) {
      foreach (IAssemblyReference assemblyReference in module.AssemblyReferences) {
        this.AssemblyReference(assemblyReference);
      }
    }

    public void ResourceReference(IResourceReference resourceReference) {
      this.ILDasmPaper.Directive(".mresource");
      IResource resource = resourceReference.Resource;
      if (resource.IsPublic) {
        this.ILDasmPaper.Keyword("public");
      } else {
        this.ILDasmPaper.Keyword("private");
      }
      this.ILDasmPaper.Identifier(resource.Name.Value);
      this.ILDasmPaper.NewLine();
      this.ILDasmPaper.OpenBlock();
      {
        if (resourceReference.DefiningAssembly.Equals(this.CurrentModule.ContainingAssembly)) {
          if (resource.IsInExternalFile) {
            this.ILDasmPaper.Directive(".file");
            this.ILDasmPaper.Identifier(resource.ExternalFile.FileName.Value);
            this.ILDasmPaper.Keyword("at");
            this.ILDasmPaper.HexUInt(0);
            this.ILDasmPaper.NewLine();
          }
        } else {
          this.ILDasmPaper.Directive(".assembly extern");
          this.ILDasmPaper.Identifier(resourceReference.DefiningAssembly.Name.Value);
          this.ILDasmPaper.NewLine();
        }
        this.ILDasmPaper.ByteStream(resource.Data);
        this.ILDasmPaper.NewLine();
      }
      this.ILDasmPaper.CloseBlock();
    }

    public void FileReference(IFileReference fileReference) {
      this.ILDasmPaper.Directive(".file");
      if (!fileReference.HasMetadata) {
        this.ILDasmPaper.Keyword("nometadata");
      }
      this.ILDasmPaper.Identifier(fileReference.FileName.Value);
      this.ILDasmPaper.NewLine();
      this.ILDasmPaper.Indent();
      this.ILDasmPaper.Indent();
      {
        this.ILDasmPaper.Directive(".hash");
        this.ILDasmPaper.Symbol(" = ");
        this.ILDasmPaper.ByteStream(fileReference.HashValue);
      }
      this.ILDasmPaper.Unindent();
      this.ILDasmPaper.Unindent();
      this.ILDasmPaper.NewLine();
    }

    public void FileReferences(IEnumerable<IFileReference> fileReferences) {
      foreach (IFileReference fileReference in fileReferences) {
        this.FileReference(fileReference);
      }
    }

    public void Win32Resource(IWin32Resource win32Resource) {
      this.ILDasmPaper.Directive(".win32resource");
      string nameOrId = win32Resource.Id < 0 ? win32Resource.Name : win32Resource.Id.ToString("X8");
      this.ILDasmPaper.Identifier(nameOrId);
      this.ILDasmPaper.NewLine();
      this.ILDasmPaper.OpenBlock();
      string typeNameOrId = win32Resource.TypeId < 0 ? win32Resource.TypeName : win32Resource.TypeId.ToString("X8");
      this.ILDasmPaper.Directive(".type");
      this.ILDasmPaper.Identifier(typeNameOrId);
      this.ILDasmPaper.NewLine();
      this.ILDasmPaper.Directive(".language");
      this.ILDasmPaper.HexUInt(win32Resource.LanguageId);
      this.ILDasmPaper.NewLine();
      this.ILDasmPaper.Directive(".codepage");
      this.ILDasmPaper.HexUInt(win32Resource.CodePage);
      this.ILDasmPaper.NewLine();
      this.ILDasmPaper.Directive(".data");
      this.ILDasmPaper.Symbol(" = ");
      this.ILDasmPaper.ByteStream(win32Resource.Data);
      this.ILDasmPaper.NewLine();
      this.ILDasmPaper.CloseBlock();
    }

    void TypeMemberAccess(TypeMemberVisibility typeMemberVisibility) {
      switch (typeMemberVisibility) {
        case TypeMemberVisibility.Assembly:
          this.ILDasmPaper.Keyword("assembly");
          break;
        case TypeMemberVisibility.Family:
          this.ILDasmPaper.Keyword("family");
          break;
        case TypeMemberVisibility.FamilyAndAssembly:
          this.ILDasmPaper.Keyword("famandassem");
          break;
        case TypeMemberVisibility.FamilyOrAssembly:
          this.ILDasmPaper.Keyword("famorassem");
          break;
        case TypeMemberVisibility.Other:
          this.ILDasmPaper.Keyword("compilercontrolled");
          break;
        case TypeMemberVisibility.Private:
          this.ILDasmPaper.Keyword("private");
          break;
        case TypeMemberVisibility.Public:
          this.ILDasmPaper.Keyword("public");
          break;
        case TypeMemberVisibility.Default:
        default:
          break;
      }
    }

    void CustomModifier(ICustomModifier customModifier) {
      if (customModifier.IsOptional) {
        this.ILDasmPaper.Keyword("modopt");
      } else {
        this.ILDasmPaper.Keyword("modreq");
      }
      this.ILDasmPaper.Symbol("(");
      this.TypeReference(customModifier.Modifier);
      this.ILDasmPaper.Symbol(")");
    }

    void CustomModifiers(IEnumerable<ICustomModifier> customModifiers) {
      foreach (ICustomModifier cm in customModifiers) {
        this.CustomModifier(cm);
      }
    }

    public void TypeDefinitionAsReference(ITypeReference typeReference) {
      if (typeReference == null || typeReference == Dummy.TypeReference) {
        this.ILDasmPaper.Identifier("###DummyType###");
      }
      PrimitiveTypeCode ptc = typeReference.TypeCode;
      switch (ptc) {
        case PrimitiveTypeCode.Boolean:
          this.ILDasmPaper.Keyword("bool");
          return;
        case PrimitiveTypeCode.Char:
          this.ILDasmPaper.Keyword("char");
          return;
        case PrimitiveTypeCode.Int8:
          this.ILDasmPaper.Keyword("int8");
          return;
        case PrimitiveTypeCode.Float32:
          this.ILDasmPaper.Keyword("float32");
          return;
        case PrimitiveTypeCode.Float64:
          this.ILDasmPaper.Keyword("float64");
          return;
        case PrimitiveTypeCode.Int16:
          this.ILDasmPaper.Keyword("int16");
          return;
        case PrimitiveTypeCode.Int32:
          this.ILDasmPaper.Keyword("int32");
          return;
        case PrimitiveTypeCode.Int64:
          this.ILDasmPaper.Keyword("int64");
          return;
        case PrimitiveTypeCode.IntPtr:
          this.ILDasmPaper.Keyword("native int");
          return;
        case PrimitiveTypeCode.UInt8:
          this.ILDasmPaper.Keyword("unsigned int8");
          return;
        case PrimitiveTypeCode.UInt16:
          this.ILDasmPaper.Keyword("unsigned int16");
          return;
        case PrimitiveTypeCode.UInt32:
          this.ILDasmPaper.Keyword("unsigned int32");
          return;
        case PrimitiveTypeCode.UInt64:
          this.ILDasmPaper.Keyword("unsigned int64");
          return;
        case PrimitiveTypeCode.UIntPtr:
          this.ILDasmPaper.Keyword("native unsigned int");
          return;
        case PrimitiveTypeCode.Void:
          this.ILDasmPaper.Keyword("void");
          return;
      }
      INamespaceTypeReference namespaceType = typeReference as INamespaceTypeReference;
      if (namespaceType != null) {
        bool wasRoot;
        this.ModuleQualifiedUnitNamespace(namespaceType.ContainingUnitNamespace, out wasRoot);
        if (!wasRoot) {
          this.ILDasmPaper.Symbol(".");
        }
        string name = namespaceType.Name.Value;
        if (namespaceType.GenericParameterCount != 0) {
          name += "`" + namespaceType.GenericParameterCount;
        }
        this.ILDasmPaper.Identifier(name);
        return;
      }
      INestedTypeReference nestedTypeDefinition = typeReference as INestedTypeReference;
      if (nestedTypeDefinition != null) {
        this.TypeDefinitionAsReference(nestedTypeDefinition.ContainingType);
        this.ILDasmPaper.Symbol("/");
        string name = nestedTypeDefinition.Name.Value;
        if (nestedTypeDefinition.GenericParameterCount != 0) {
          name += "`" + nestedTypeDefinition.GenericParameterCount;
        }
        this.ILDasmPaper.Identifier(name);
        return;
      }
      IGenericTypeInstanceReference genericTypeInstance = typeReference as IGenericTypeInstanceReference;
      if (genericTypeInstance != null) {
        this.TypeReference(genericTypeInstance.GenericType);
        this.ILDasmPaper.Symbol("<");
        bool isNotFirst = false;
        foreach (ITypeReference genericArgument in genericTypeInstance.GenericArguments) {
          if (isNotFirst) {
            this.ILDasmPaper.Symbol(",");
          }
          isNotFirst = true;
          this.TypeReference(genericArgument);
        }
        this.ILDasmPaper.Symbol(">");
        return;
      }
      IPointerTypeReference pointerType = typeReference as IPointerTypeReference;
      if (pointerType != null) {
        this.TypeReference(pointerType.TargetType);
        this.ILDasmPaper.Symbol("*");
        return;
      }
      IManagedPointerTypeReference mgdPointerType = typeReference as IManagedPointerTypeReference;
      if (mgdPointerType != null) {
        this.TypeReference(mgdPointerType.TargetType);
        this.ILDasmPaper.Symbol("&");
        return;
      }
      IArrayTypeReference arrayType = typeReference as IArrayTypeReference;
      if (arrayType != null) {
        this.TypeReference(arrayType.ElementType);
        this.ILDasmPaper.Symbol("[");
        if (!arrayType.IsVector) {
          if (arrayType.Rank == 1) {
            this.ILDasmPaper.Symbol("*");
          } else {
            for (int i = 1; i < arrayType.Rank; ++i) {
              this.ILDasmPaper.Symbol(",");
            }
          }
        }
        this.ILDasmPaper.Symbol("]");
        return;
      }
      IGenericTypeParameter genericTypeParameter = typeReference as IGenericTypeParameter;
      if (genericTypeParameter != null) {
        this.ILDasmPaper.Symbol("!");
        this.ILDasmPaper.Int((int)genericTypeParameter.Index);
        return;
      }
      IGenericMethodParameter genericMethodParameter = typeReference as IGenericMethodParameter;
      if (genericMethodParameter != null) {
        this.ILDasmPaper.Symbol("!!");
        this.ILDasmPaper.Int((int)genericMethodParameter.Index);
        return;
      }
      IFunctionPointerTypeReference functionPointerType = typeReference as IFunctionPointerTypeReference;
      if (functionPointerType != null) {
        this.TypeReference(functionPointerType.Type);
        this.ILDasmPaper.Symbol("(");
        bool isNotFirst = false;
        foreach (IParameterTypeInformation paramInfo in functionPointerType.Parameters) {
          if (isNotFirst) {
            this.ILDasmPaper.Symbol(",");
          }
          isNotFirst = true;
          this.TypeReference(paramInfo.Type);
          if (paramInfo.IsByReference)
            this.ILDasmPaper.Symbol("&");
        }
        if (functionPointerType.ExtraArgumentTypes.GetEnumerator().MoveNext()) {
          this.ILDasmPaper.Symbol("...");
          foreach (IParameterTypeInformation paramInfo in functionPointerType.ExtraArgumentTypes) {
            if (isNotFirst) {
              this.ILDasmPaper.Symbol(",");
            }
            isNotFirst = true;
            this.TypeReference(paramInfo.Type);
            if (paramInfo.IsByReference)
              this.ILDasmPaper.Symbol("&");
          }
        }
        this.ILDasmPaper.Symbol(")");
      }
    }

    public void TypeReference(ITypeReference typeReference) {
      if (typeReference == null || typeReference == Dummy.TypeReference) {
        this.ILDasmPaper.Identifier("###DummyType###");
      }
      var typeDef = typeReference.ResolvedType;
      if (typeDef != Dummy.Type)
        this.TypeDefinitionAsReference(typeDef);
      else
        this.TypeDefinitionAsReference(typeReference);
      IModifiedTypeReference/*?*/ modifiedReference = typeReference as IModifiedTypeReference;
      if (modifiedReference != null) {
        this.CustomModifiers(modifiedReference.CustomModifiers);
      }
    }

    void ModuleAsReference(IModuleReference module) {
      IAssemblyReference assembly = module as IAssemblyReference;
      if (assembly != null) {
        this.ILDasmPaper.Symbol("[");
        this.ILDasmPaper.Identifier(assembly.Name.Value);
        this.ILDasmPaper.Symbol("]");
        return;
      }
      this.ILDasmPaper.Symbol("[");
      this.ILDasmPaper.Directive(".module");
      this.ILDasmPaper.Identifier(module.Name.Value);
      this.ILDasmPaper.Symbol("]");
      return;
    }

    void ModuleQualifiedUnitNamespace(IUnitNamespaceReference unitNamespace, out bool wasRoot) {
      INestedUnitNamespaceReference nestedUnitNamespace = unitNamespace as INestedUnitNamespaceReference;
      if (nestedUnitNamespace != null) {
        this.ModuleQualifiedUnitNamespace(nestedUnitNamespace.ContainingUnitNamespace, out wasRoot);
        if (!wasRoot) {
          this.ILDasmPaper.Symbol(".");
        }
        this.ILDasmPaper.Identifier(nestedUnitNamespace.Name.Value);
        wasRoot = false;
      } else {
        IModuleReference module = (IModuleReference)unitNamespace.Unit;
        if (module.ContainingAssembly != null)
          module = module.ContainingAssembly;
        if (!module.Equals(this.CurrentModule))
          this.ModuleAsReference(module);
        wasRoot = true;
      }
    }

    void QualifiedUnitNamespace(IUnitNamespace unitNamespace, out bool wasRoot) {
      INestedUnitNamespace nestedUnitNamespace = unitNamespace as INestedUnitNamespace;
      if (nestedUnitNamespace != null) {
        this.QualifiedUnitNamespace((IUnitNamespace)nestedUnitNamespace.ContainingNamespace, out wasRoot);
        if (!wasRoot) {
          this.ILDasmPaper.Symbol(".");
        }
        this.ILDasmPaper.Identifier(nestedUnitNamespace.Name.Value);
        wasRoot = false;
        return;
      }
      wasRoot = true;
      return;
    }

    void SimpleUnitNamespace(IUnitNamespace unitNamespace, out bool wasRoot) {
      INestedUnitNamespace nestedUnitNamespace = unitNamespace as INestedUnitNamespace;
      if (nestedUnitNamespace != null) {
        this.ILDasmPaper.Identifier(nestedUnitNamespace.Name.Value);
        wasRoot = false;
        return;
      }
      wasRoot = true;
      return;
    }

    void QualifiedAliasForType(IAliasForType aliasForType) {
      INamespaceAliasForType nsAliasForType = aliasForType as INamespaceAliasForType;
      if (nsAliasForType != null) {
        bool wasRoot;
        this.QualifiedUnitNamespace((IUnitNamespace)nsAliasForType.ContainingNamespace, out wasRoot);
        if (!wasRoot) {
          this.ILDasmPaper.Symbol(".");
        }
        this.ILDasmPaper.Identifier(nsAliasForType.Name.Value);
        return;
      }
      INestedAliasForType nestedAliasForType = aliasForType as INestedAliasForType;
      if (nestedAliasForType != null) {
        this.SimpleAliasForType(nestedAliasForType.ContainingAlias);
        this.ILDasmPaper.Symbol(".");
        this.ILDasmPaper.Identifier(nestedAliasForType.Name.Value);
        return;
      }
      return;
    }

    void SimpleAliasForType(IAliasForType aliasForType) {
      INamespaceAliasForType nsAliasForType = aliasForType as INamespaceAliasForType;
      if (nsAliasForType != null) {
        this.ILDasmPaper.Identifier(nsAliasForType.Name.Value);
        return;
      }
      INestedAliasForType nstAliasForType = aliasForType as INestedAliasForType;
      if (nstAliasForType != null) {
        this.ILDasmPaper.Identifier(nstAliasForType.Name.Value);
        return;
      }
    }

    public void ExportedType(IAliasForType exportedType) {
      INamespaceAliasForType nsAlias = exportedType as INamespaceAliasForType;
      INestedAliasForType nestedAlias = exportedType as INestedAliasForType;
      this.ILDasmPaper.Directive(".class extern");
      if (nsAlias != null) {
        if (nsAlias.IsPublic) {
          this.ILDasmPaper.Keyword("public");
        }
        this.QualifiedAliasForType(exportedType);
        this.ILDasmPaper.NewLine();
      } else if (nestedAlias != null) {
        this.ILDasmPaper.Keyword("nested");
        this.TypeMemberAccess(nestedAlias.Visibility);
        this.ILDasmPaper.Identifier(nestedAlias.Name.Value);
        this.ILDasmPaper.NewLine();
      } else {
        this.ILDasmPaper.NewLine();
        return;
      }
      this.ILDasmPaper.OpenBlock();
      if (nsAlias != null) {
        this.ILDasmPaper.Directive(".file");
        this.ILDasmPaper.Identifier(Helper.GetModuleForType(nsAlias.AliasedType.ResolvedType).Name.Value);
      } else {
        this.ILDasmPaper.Directive(".class extern");
        this.QualifiedAliasForType(nestedAlias.ContainingAlias);
      }
      this.ILDasmPaper.NewLine();
      this.ILDasmPaper.CloseBlock();
    }

    public void ExportedTypes(IEnumerable<IAliasForType> exportedTypes) {
      foreach (IAliasForType aft in exportedTypes) {
        this.ExportedType(aft);
      }
    }

    void GenericParameter(IGenericParameter genericParameter) {
      switch (genericParameter.Variance) {
        case TypeParameterVariance.Covariant:
          this.ILDasmPaper.Symbol("+");
          break;
        case TypeParameterVariance.Contravariant:
          this.ILDasmPaper.Symbol("-");
          break;
        case TypeParameterVariance.NonVariant:
        case TypeParameterVariance.Mask:
        default:
          break;
      }
      if (genericParameter.MustBeReferenceType)
        this.ILDasmPaper.Keyword("class");
      if (genericParameter.MustBeValueType)
        this.ILDasmPaper.Keyword("valuetype");
      if (genericParameter.MustHaveDefaultConstructor)
        this.ILDasmPaper.Keyword(".ctor");
      if (genericParameter.Constraints.GetEnumerator().MoveNext()) {
        this.ILDasmPaper.Symbol("(");
        bool isNotFirst = false;
        foreach (ITypeReference tr in genericParameter.Constraints) {
          if (isNotFirst) {
            this.ILDasmPaper.Symbol(", ");
          }
          isNotFirst = true;
          this.TypeReference(tr);
        }
        this.ILDasmPaper.Symbol(") ");
      }
      this.ILDasmPaper.Identifier(genericParameter.Name.Value);
    }

    public void TypeDefinition(ITypeDefinition typeDefinition) {
      this.ILDasmPaper.Directive(".class");
      INamespaceTypeDefinition namespaceTypeDefinition = typeDefinition as INamespaceTypeDefinition;
      INestedTypeDefinition nestedTypeDefinition = typeDefinition as INestedTypeDefinition;
      if (typeDefinition.IsInterface)
        this.ILDasmPaper.Keyword("interface");
      if (namespaceTypeDefinition != null) {
        if (namespaceTypeDefinition.IsPublic) {
          this.ILDasmPaper.Keyword("public");
        } else {
          this.ILDasmPaper.Keyword("private");
        }
      }
      if (typeDefinition.IsAbstract)
        this.ILDasmPaper.Keyword("abstract");
      switch (typeDefinition.Layout) {
        case LayoutKind.Auto:
          this.ILDasmPaper.Keyword("auto");
          break;
        case LayoutKind.Sequential:
          this.ILDasmPaper.Keyword("sequential");
          break;
        case LayoutKind.Explicit:
          this.ILDasmPaper.Keyword("explicit");
          break;
      }
      switch (typeDefinition.StringFormat) {
        case StringFormatKind.Unspecified:
          break;
        case StringFormatKind.Ansi:
          this.ILDasmPaper.Keyword("ansi");
          break;
        case StringFormatKind.Unicode:
          this.ILDasmPaper.Keyword("unicode");
          break;
        case StringFormatKind.AutoChar:
          this.ILDasmPaper.Keyword("autochar");
          break;
      }
      if (typeDefinition.IsComObject)
        this.ILDasmPaper.Keyword("import");
      if (typeDefinition.IsSerializable)
        this.ILDasmPaper.Keyword("serializable");
      if (typeDefinition.IsSealed)
        this.ILDasmPaper.Keyword("sealed");
      if (nestedTypeDefinition != null) {
        this.ILDasmPaper.Keyword("nested");
        this.TypeMemberAccess(nestedTypeDefinition.Visibility);
      }
      if (typeDefinition.IsBeforeFieldInit)
        this.ILDasmPaper.Keyword("beforefieldinit");
      if (typeDefinition.IsSpecialName)
        this.ILDasmPaper.Keyword("specialname");
      if (typeDefinition.IsRuntimeSpecial)
        this.ILDasmPaper.Keyword("rtspecialname");
      this.TypeDefinitionAsReference(typeDefinition);
      if (typeDefinition.GenericParameterCount > 0) {
        this.ILDasmPaper.Symbol("<");
        bool isNotFirst = false;
        foreach (IGenericParameter gp in typeDefinition.GenericParameters) {
          if (isNotFirst) {
            this.ILDasmPaper.Symbol(", ");
          }
          isNotFirst = true;
          this.GenericParameter(gp);
        }
        this.ILDasmPaper.Symbol(">");
      }
      this.ILDasmPaper.Indent();
      this.ILDasmPaper.NewLine();
      {
        if (typeDefinition.BaseClasses.GetEnumerator().MoveNext()) {
          this.ILDasmPaper.Keyword("extends ");
          foreach (ITypeReference tr in typeDefinition.BaseClasses) {
            this.TypeReference(tr);
          }
          this.ILDasmPaper.NewLine();
        }
      }
      {
        if (typeDefinition.Interfaces.GetEnumerator().MoveNext()) {
          this.ILDasmPaper.Keyword("implements ");
          bool isNotFirst = false;
          foreach (ITypeReference tr in typeDefinition.Interfaces) {
            if (isNotFirst) {
              this.ILDasmPaper.Symbol(",");
              this.ILDasmPaper.NewLine();
              this.ILDasmPaper.DataString("           ");
            }
            isNotFirst = true;
            this.TypeReference(tr);
          }
          this.ILDasmPaper.NewLine();
        }
      }
      this.ILDasmPaper.Unindent();
      this.ILDasmPaper.OpenBlock();
      this.CustomAttributes(typeDefinition.Attributes);
      List<string> strList = new List<string>();
      foreach (ITypeDefinitionMember tdm in typeDefinition.Members) {
        strList.Add(Helper.TypeDefinitionMember(this.CurrentModule, tdm));
      }
      strList.Sort();
      foreach (string str in strList) {
        this.ILDasmPaper.DataString(str);
        this.ILDasmPaper.NewLine();
      }
      this.ILDasmPaper.Directive(".flags");
      if (typeDefinition.IsClass)
        this.ILDasmPaper.Keyword("class");
      if (typeDefinition.IsDelegate)
        this.ILDasmPaper.Keyword("delegate");
      if (typeDefinition.IsEnum)
        this.ILDasmPaper.Keyword("enum");
      if (typeDefinition.IsReferenceType)
        this.ILDasmPaper.Keyword("reftype");
      if (typeDefinition.IsStatic)
        this.ILDasmPaper.Keyword("static");
      if (typeDefinition.IsValueType)
        this.ILDasmPaper.Keyword("valuetype");
      if (typeDefinition.IsStruct)
        this.ILDasmPaper.Keyword("struct");
      this.ILDasmPaper.NewLine();
      this.ILDasmPaper.Directive(".pack");
      this.ILDasmPaper.Int(typeDefinition.Alignment);
      this.ILDasmPaper.NewLine();
      this.ILDasmPaper.Directive(".size");
      this.ILDasmPaper.UInt(typeDefinition.SizeOf);
      this.ILDasmPaper.NewLine();
      foreach (IMethodImplementation methodImpl in typeDefinition.ExplicitImplementationOverrides) {
        this.ILDasmPaper.Directive(".override");
        this.MethodReference(methodImpl.ImplementedMethod);
        this.ILDasmPaper.Keyword(" with");
        this.MethodReference(methodImpl.ImplementingMethod);
        this.ILDasmPaper.NewLine();
      }
      this.ILDasmPaper.CloseBlock();
    }

    public void MethodReference(IMethodReference methodReference) {
      IGenericMethodInstanceReference genericMethodInst = methodReference as IGenericMethodInstanceReference;
      if (genericMethodInst != null) {
        methodReference = genericMethodInst.GenericMethod;
      }
      if (methodReference.IsStatic) {
        this.ILDasmPaper.Keyword("static");
      } else {
        this.ILDasmPaper.Keyword("instance");
      }
      this.TypeReference(methodReference.Type);
      this.TypeReference(methodReference.ContainingType);
      this.ILDasmPaper.Symbol("::");
      this.ILDasmPaper.Identifier(methodReference.Name.Value);
      bool isNotFirst = false;
      if (genericMethodInst != null) {
        this.ILDasmPaper.Symbol("<");
        foreach (ITypeReference typeRef in genericMethodInst.GenericArguments) {
          if (isNotFirst) {
            this.ILDasmPaper.Symbol(",");
          }
          isNotFirst = true;
          this.TypeReference(typeRef);
        }
        this.ILDasmPaper.Symbol(">");
      }
      this.ILDasmPaper.Symbol("(");
      isNotFirst = false;
      foreach (IParameterTypeInformation paramType in methodReference.Parameters) {
        if (isNotFirst) {
          this.ILDasmPaper.Symbol(",");
        }
        isNotFirst = true;
        this.TypeReference(paramType.Type);
        if (paramType.IsByReference)
          this.ILDasmPaper.Symbol("&");
      }
      this.ILDasmPaper.Symbol(")");
    }

    public void FieldDefinitionAsRefernece(IFieldDefinition fieldDefinition) {
      this.TypeReference(fieldDefinition.Type);
      this.TypeDefinitionAsReference(fieldDefinition.ContainingTypeDefinition);
      this.ILDasmPaper.Symbol("::");
      this.ILDasmPaper.Identifier(fieldDefinition.Name.Value);
    }

    public void PlatformInvokeInformation(IPlatformInvokeInformation pInvokeInfo) {
      this.ILDasmPaper.Keyword("pinvokeimpl");
      this.ILDasmPaper.Symbol("(");
      this.ILDasmPaper.Identifier(pInvokeInfo.ImportModule.Name.Value);
      this.ILDasmPaper.Keyword("as");
      this.ILDasmPaper.Identifier(pInvokeInfo.ImportName.Value);
      if (pInvokeInfo.NoMangle)
        this.ILDasmPaper.Keyword("nomangle");
      switch (pInvokeInfo.StringFormat) {
        case StringFormatKind.Unspecified:
          break;
        case StringFormatKind.Ansi:
          this.ILDasmPaper.Keyword("ansi");
          break;
        case StringFormatKind.Unicode:
          this.ILDasmPaper.Keyword("unicode");
          break;
        case StringFormatKind.AutoChar:
          this.ILDasmPaper.Keyword("autochar");
          break;
      }
      switch (pInvokeInfo.PInvokeCallingConvention) {
        case PInvokeCallingConvention.WinApi:
          this.ILDasmPaper.Keyword("winapi");
          break;
        case PInvokeCallingConvention.CDecl:
          this.ILDasmPaper.Keyword("cdecl");
          break;
        case PInvokeCallingConvention.StdCall:
          this.ILDasmPaper.Keyword("stdcall");
          break;
        case PInvokeCallingConvention.ThisCall:
          this.ILDasmPaper.Keyword("thiscall");
          break;
        case PInvokeCallingConvention.FastCall:
          this.ILDasmPaper.Keyword("fastcall");
          break;
        default:
          break;
      }
      if (pInvokeInfo.SupportsLastError)
        this.ILDasmPaper.Keyword("lasterr");
      this.ILDasmPaper.Symbol(")");
    }

    public void Parameters(IEnumerable<IParameterDefinition> parameters) {
      this.ILDasmPaper.Symbol("(");
      if (parameters.GetEnumerator().MoveNext()) {
        this.ILDasmPaper.NewLine();
        this.ILDasmPaper.Indent();
        {
          bool isNotFirst = false;
          foreach (IParameterDefinition paramDef in parameters) {
            if (isNotFirst) {
              this.ILDasmPaper.Symbol(",");
              this.ILDasmPaper.NewLine();
            }
            isNotFirst = true;
            if (paramDef.IsIn) {
              this.ILDasmPaper.Symbol("[");
              this.ILDasmPaper.Keyword("in");
              this.ILDasmPaper.Symbol("]");
            }
            if (paramDef.IsOptional) {
              this.ILDasmPaper.Symbol("[");
              this.ILDasmPaper.Keyword("opt");
              this.ILDasmPaper.Symbol("]");
            }
            if (paramDef.IsOut) {
              this.ILDasmPaper.Symbol("[");
              this.ILDasmPaper.Keyword("out");
              this.ILDasmPaper.Symbol("]");
            }
            this.TypeReference(paramDef.Type);
            if (paramDef.IsByReference)
              this.ILDasmPaper.Symbol("&");
            if (paramDef.IsModified)
              this.CustomModifiers(paramDef.CustomModifiers);
            if (paramDef.IsMarshalledExplicitly) {
              this.MarshallingInformation(paramDef.MarshallingInformation);
            }
            if (paramDef.Name != Dummy.Name) {
              this.ILDasmPaper.DataString("");
              this.ILDasmPaper.Identifier(paramDef.Name.Value);
            }
          }
        }
        this.ILDasmPaper.Unindent();
        this.ILDasmPaper.NewLine();
      }
      this.ILDasmPaper.Symbol(")");
    }

    void Operation(
      Dictionary<ILocalDefinition, int> localVariableOrdinals,
      Dictionary<IParameterDefinition, int> parameterOrdinals,
      IOperation operation
    ) {
      this.ILDasmPaper.Identifier("IL_" + operation.Offset.ToString("x4") + ": ");
      string op = operation.OperationCode.ToString();
      op = op.ToLowerInvariant().Replace('_', '.');
      this.ILDasmPaper.Keyword(op);
      switch (operation.OperationCode) {
        case OperationCode.Ldarg:
        case OperationCode.Ldarga:
        case OperationCode.Starg:
        case OperationCode.Starg_S:
        case OperationCode.Ldarg_S:
        case OperationCode.Ldarga_S: {
            IParameterDefinition param = operation.Value as IParameterDefinition;
            string name;
            if (param != null && param.Name != Dummy.Name) {
              name = param.Name.Value;
            } else {
              int num = 0;
              if (param != null)
                num = parameterOrdinals[param];
              name = "param_" + num;
            }
            this.ILDasmPaper.Identifier(name);
          }
          break;
        case OperationCode.Ldloc:
        case OperationCode.Ldloca:
        case OperationCode.Stloc:
        case OperationCode.Ldloc_S:
        case OperationCode.Ldloca_S:
        case OperationCode.Stloc_S: {
            ILocalDefinition localVarDef = operation.Value as ILocalDefinition;
            string name = "V_";
            if (localVarDef != null)
              name += localVariableOrdinals[localVarDef];
            this.ILDasmPaper.Identifier(name);
          }
          break;
        case OperationCode.Ldc_I4_S:
        case OperationCode.Ldc_I4:
        case OperationCode.Ldc_I8:
        case OperationCode.Ldc_R4:
        case OperationCode.Ldc_R8: {
            this.ILDasmPaper.Constant(operation.Value);
          }
          break;
        case OperationCode.Ldftn:
        case OperationCode.Ldvirtftn:
        case OperationCode.Newobj:
        case OperationCode.Jmp:
        case OperationCode.Call:
        case OperationCode.Callvirt: {
            IMethodReference methodRef = operation.Value as IMethodReference;
            if (methodRef == null)
              break;
            this.MethodReference(methodRef);
          }
          break;
        case OperationCode.Calli: {
            this.TypeDefinitionAsReference(operation.Value as IFunctionPointerTypeReference);
          }
          break;
        case OperationCode.Leave:
        case OperationCode.Leave_S:
        case OperationCode.Br_S:
        case OperationCode.Brfalse_S:
        case OperationCode.Brtrue_S:
        case OperationCode.Beq_S:
        case OperationCode.Bge_S:
        case OperationCode.Bgt_S:
        case OperationCode.Ble_S:
        case OperationCode.Blt_S:
        case OperationCode.Bne_Un_S:
        case OperationCode.Bge_Un_S:
        case OperationCode.Bgt_Un_S:
        case OperationCode.Ble_Un_S:
        case OperationCode.Blt_Un_S:
        case OperationCode.Br:
        case OperationCode.Brfalse:
        case OperationCode.Brtrue:
        case OperationCode.Beq:
        case OperationCode.Bge:
        case OperationCode.Bgt:
        case OperationCode.Ble:
        case OperationCode.Blt:
        case OperationCode.Bne_Un:
        case OperationCode.Bge_Un:
        case OperationCode.Bgt_Un:
        case OperationCode.Ble_Un:
        case OperationCode.Blt_Un: {
            uint offset = (uint)operation.Value;
            this.ILDasmPaper.Identifier("IL_" + offset.ToString("x4"));
          }
          break;
        case OperationCode.Switch: {
            uint[] offsets = (uint[])operation.Value;
            bool isNotFirst = false;
            this.ILDasmPaper.Symbol("(");
            foreach (uint offset in offsets) {
              if (isNotFirst) {
                this.ILDasmPaper.Symbol(",");
              }
              this.ILDasmPaper.Identifier("IL_" + offset.ToString("x4"));
            }
            this.ILDasmPaper.Symbol(")");
          }
          break;
        case OperationCode.Sizeof:
        case OperationCode.Constrained_:
        case OperationCode.Initobj:
        case OperationCode.Mkrefany:
        case OperationCode.Refanyval:
        case OperationCode.Ldelem:
        case OperationCode.Stelem:
        case OperationCode.Unbox_Any:
        case OperationCode.Ldelema:
        case OperationCode.Newarr:
        case OperationCode.Box:
        case OperationCode.Stobj:
        case OperationCode.Unbox:
        case OperationCode.Isinst:
        case OperationCode.Castclass:
        case OperationCode.Ldobj:
        case OperationCode.Cpobj: {
            ITypeReference typeReference = operation.Value as ITypeReference;
            if (typeReference != null) {
              this.TypeReference(typeReference);
            }
          }
          break;
        case OperationCode.Ldstr: {
            this.ILDasmPaper.Constant(operation.Value);
          }
          break;
        case OperationCode.Ldfld:
        case OperationCode.Ldflda:
        case OperationCode.Stfld:
        case OperationCode.Ldsfld:
        case OperationCode.Ldsflda:
        case OperationCode.Stsfld: {
            IFieldReference fieldRef = operation.Value as IFieldReference;
            if (fieldRef == null)
              break;
            IFieldDefinition fieldDef = fieldRef.ResolvedField;
            if (fieldDef == null)
              break;
            this.FieldDefinitionAsRefernece(fieldDef);
          }
          break;
        case OperationCode.No_:
        case OperationCode.Unaligned_: {
            this.ILDasmPaper.Constant(operation.Value);
          }
          break;
        default:
          break;
      }
    }

    void MethodBody(IMethodBody methodBody) {
      this.ILDasmPaper.Directive(".maxstack");
      this.ILDasmPaper.Int(methodBody.MaxStack);
      this.ILDasmPaper.NewLine();
      this.ILDasmPaper.Directive(".locals");
      if (methodBody.LocalsAreZeroed)
        this.ILDasmPaper.Keyword("init");
      Dictionary<ILocalDefinition, int> localVariableOrdinals = new Dictionary<ILocalDefinition, int>();
      int count = 0;
      this.ILDasmPaper.Symbol("(");
      if (methodBody.LocalVariables.GetEnumerator().MoveNext()) {
        this.ILDasmPaper.NewLine();
        this.ILDasmPaper.Indent();
        bool isNotFirst = false;
        foreach (ILocalDefinition localVariable in methodBody.LocalVariables) {
          if (isNotFirst) {
            this.ILDasmPaper.Symbol(",");
            this.ILDasmPaper.NewLine();
          }
          isNotFirst = true;
          this.TypeReference(localVariable.Type);
          if (localVariable.IsReference)
            this.ILDasmPaper.Symbol("&");
          if (localVariable.IsModified)
            this.CustomModifiers(localVariable.CustomModifiers);
          localVariableOrdinals.Add(localVariable, count);
          this.ILDasmPaper.DataString("");
          this.ILDasmPaper.Identifier("V_" + count);
          count++;
        }
        this.ILDasmPaper.Unindent();
        this.ILDasmPaper.NewLine();
      }
      this.ILDasmPaper.Symbol(")");
      this.ILDasmPaper.NewLine();
      Dictionary<IParameterDefinition, int> parameterOrdinals = new Dictionary<IParameterDefinition, int>();
      count = 0;
      if (!methodBody.MethodDefinition.IsStatic)
        count = 1;
      foreach (IParameterDefinition param in methodBody.MethodDefinition.Parameters) {
        parameterOrdinals.Add(param, count);
        count++;
      }
      foreach (IOperation operation in methodBody.Operations) {
        this.Operation(localVariableOrdinals, parameterOrdinals, operation);
        this.ILDasmPaper.NewLine();
      }
      foreach (IOperationExceptionInformation excepInfo in methodBody.OperationExceptionInformation) {
        this.ILDasmPaper.Directive(".try");
        this.ILDasmPaper.Identifier("IL_" + excepInfo.TryStartOffset.ToString("x4"));
        this.ILDasmPaper.Keyword("to");
        this.ILDasmPaper.Identifier("IL_" + excepInfo.TryEndOffset.ToString("x4"));
        switch (excepInfo.HandlerKind) {
          case HandlerKind.Catch:
            this.ILDasmPaper.Keyword("catch");
            this.TypeReference(excepInfo.ExceptionType);
            break;
          case HandlerKind.Filter:
            this.ILDasmPaper.Keyword("filter");
            this.ILDasmPaper.Identifier("IL_" + excepInfo.FilterDecisionStartOffset.ToString("x4"));
            break;
          case HandlerKind.Finally:
            this.ILDasmPaper.Keyword("finally");
            break;
          case HandlerKind.Fault:
            this.ILDasmPaper.Keyword("fault");
            break;
          case HandlerKind.Illegal:
          default:
            this.ILDasmPaper.Keyword("illegal");
            break;
        }
        this.ILDasmPaper.Keyword("handler");
        this.ILDasmPaper.Identifier("IL_" + excepInfo.HandlerStartOffset.ToString("x4"));
        this.ILDasmPaper.Keyword("to");
        this.ILDasmPaper.Identifier("IL_" + excepInfo.HandlerEndOffset.ToString("x4"));
        this.ILDasmPaper.NewLine();
      }
    }

    public void MethodDefinition(IMethodDefinition methodDefinition) {
      IGenericMethodInstance genericMethodInst = methodDefinition as IGenericMethodInstance;
      this.ILDasmPaper.Directive(".method");
      this.TypeMemberAccess(methodDefinition.Visibility);
      if (methodDefinition.IsHiddenBySignature)
        this.ILDasmPaper.Keyword("hidebysig");
      if (methodDefinition.IsNewSlot)
        this.ILDasmPaper.Keyword("newslot");
      if (methodDefinition.IsSpecialName)
        this.ILDasmPaper.Keyword("specialname");
      if (methodDefinition.IsRuntimeSpecial)
        this.ILDasmPaper.Keyword("rtspecialname");
      if (methodDefinition.IsStatic)
        this.ILDasmPaper.Keyword("static");
      if (methodDefinition.IsAbstract)
        this.ILDasmPaper.Keyword("abstract");
      if (methodDefinition.IsAccessCheckedOnOverride)
        this.ILDasmPaper.Keyword("strict");
      if (methodDefinition.IsVirtual)
        this.ILDasmPaper.Keyword("virtual");
      if (methodDefinition.IsSealed)
        this.ILDasmPaper.Keyword("final");
      if (methodDefinition.RequiresSecurityObject)
        this.ILDasmPaper.Keyword("reqsecobj");
      if (methodDefinition.IsPlatformInvoke) {
        this.PlatformInvokeInformation(methodDefinition.PlatformInvokeData);
      }
      if (!methodDefinition.IsStatic)
        this.ILDasmPaper.Keyword("instance");
      if (!methodDefinition.HasExplicitThisParameter)
        this.ILDasmPaper.Keyword("explicit");
      switch (methodDefinition.CallingConvention & (CallingConvention)0x0F) {
        case CallingConvention.C:
          this.ILDasmPaper.Keyword("unmanaged cdecl");
          break;
        case CallingConvention.Default:
          this.ILDasmPaper.Keyword("default");
          break;
        case CallingConvention.ExtraArguments:
          this.ILDasmPaper.Keyword("vararg");
          break;
        case CallingConvention.FastCall:
          this.ILDasmPaper.Keyword("unmanaged fastcall");
          break;
        case CallingConvention.Standard:
          this.ILDasmPaper.Keyword("unmanaged stdcall");
          break;
        case CallingConvention.ThisCall:
          this.ILDasmPaper.Keyword("unmanaged thiscall");
          break;
      }
      this.TypeReference(methodDefinition.Type);
      if (methodDefinition.ReturnValueIsByRef)
        this.ILDasmPaper.Symbol("&");
      if (methodDefinition.ReturnValueIsModified)
        this.CustomModifiers(methodDefinition.ReturnValueCustomModifiers);
      this.ILDasmPaper.Identifier(methodDefinition.Name.Value);
      if (genericMethodInst != null) {
        bool isNotFirst = false;
        this.ILDasmPaper.Symbol("<");
        foreach (ITypeReference typeRef in genericMethodInst.GenericArguments) {
          if (isNotFirst) {
            this.ILDasmPaper.Symbol(",");
          }
          isNotFirst = true;
          this.TypeReference(typeRef);
        }
        this.ILDasmPaper.Symbol(">");
      } else if (methodDefinition.GenericParameterCount > 0) {
        this.ILDasmPaper.Symbol("<");
        bool isNotFirst = false;
        foreach (IGenericParameter gp in methodDefinition.GenericParameters) {
          if (isNotFirst) {
            this.ILDasmPaper.Symbol(", ");
          }
          isNotFirst = true;
          this.GenericParameter(gp);
        }
        this.ILDasmPaper.Symbol(">");
      }
      this.Parameters(methodDefinition.Parameters);
      if (methodDefinition.IsNativeCode)
        this.ILDasmPaper.Keyword("native");
      if (methodDefinition.IsCil)
        this.ILDasmPaper.Keyword("cil");
      if (methodDefinition.IsRuntimeImplemented)
        this.ILDasmPaper.Keyword("runtime");
      if (methodDefinition.IsUnmanaged)
        this.ILDasmPaper.Keyword("unmanaged");
      else
        this.ILDasmPaper.Keyword("managed");
      if (methodDefinition.IsForwardReference)
        this.ILDasmPaper.Keyword("forwardref");
      if (methodDefinition.PreserveSignature)
        this.ILDasmPaper.Keyword("preservesig");
      if (methodDefinition.IsRuntimeInternal)
        this.ILDasmPaper.Keyword("internalcall");
      if (methodDefinition.IsSynchronized)
        this.ILDasmPaper.Keyword("synchronized");
      if (methodDefinition.IsNeverInlined)
        this.ILDasmPaper.Keyword("noinlining");
      if (methodDefinition.IsNeverInlined)
        this.ILDasmPaper.Keyword("nooptimization");
      this.ILDasmPaper.NewLine();
      this.ILDasmPaper.OpenBlock();
      this.CustomAttributes(methodDefinition.Attributes);
      if (!methodDefinition.IsAbstract && !methodDefinition.IsExternal && methodDefinition.Body != Dummy.MethodBody) {
        this.MethodBody(methodDefinition.Body);
      }
      this.ILDasmPaper.CloseBlock();
    }

    public void FieldDefinition(IFieldDefinition fieldDefinition) {
      this.ILDasmPaper.Directive(".field");
      if (fieldDefinition.ContainingTypeDefinition.Layout == LayoutKind.Explicit) {
        this.ILDasmPaper.Symbol("[");
        this.ILDasmPaper.UInt(fieldDefinition.Offset);
        this.ILDasmPaper.Symbol("]");
      }
      this.TypeMemberAccess(fieldDefinition.Visibility);
      if (fieldDefinition.IsStatic)
        this.ILDasmPaper.Keyword("static");
      if (fieldDefinition.IsReadOnly)
        this.ILDasmPaper.Keyword("initonly");
      if (fieldDefinition.IsCompileTimeConstant)
        this.ILDasmPaper.Keyword("literal");
      if (fieldDefinition.IsNotSerialized)
        this.ILDasmPaper.Keyword("notserialized");
      if (fieldDefinition.IsSpecialName)
        this.ILDasmPaper.Keyword("specialname");
      if (fieldDefinition.IsRuntimeSpecial)
        this.ILDasmPaper.Keyword("rtspecialname");
      if (fieldDefinition.IsMarshalledExplicitly) {
        this.MarshallingInformation(fieldDefinition.MarshallingInformation);
      }
      this.TypeReference(fieldDefinition.Type);
      this.ILDasmPaper.Identifier(fieldDefinition.Name.Value);
      if (fieldDefinition.IsCompileTimeConstant) {
        this.ILDasmPaper.Symbol("=");
        this.Expression(fieldDefinition.CompileTimeValue);
      }
      if (fieldDefinition.IsMapped) {
        this.ILDasmPaper.Keyword("at");
        ISectionBlock mappingSection = fieldDefinition.FieldMapping;
        switch (mappingSection.PESectionKind) {
          case PESectionKind.Text:
            this.ILDasmPaper.Keyword("text");
            break;
          case PESectionKind.StaticData:
            this.ILDasmPaper.Keyword("data");
            break;
          case PESectionKind.ThreadLocalStorage:
            this.ILDasmPaper.Keyword("tls");
            break;
        }
        this.ILDasmPaper.Symbol("{");
        this.ILDasmPaper.HexUInt(mappingSection.Offset);
        this.ILDasmPaper.Symbol(",");
        this.ILDasmPaper.HexUInt(mappingSection.Size);
        this.ILDasmPaper.Symbol("}");
        this.ILDasmPaper.Directive(".data");
        this.ILDasmPaper.Symbol("=");
        this.ILDasmPaper.ByteStream(mappingSection.Data);
      }
      this.ILDasmPaper.NewLine();
      this.ILDasmPaper.OpenBlock();
      this.CustomAttributes(fieldDefinition.Attributes);
      this.ILDasmPaper.CloseBlock();
    }

    public void EventDefinition(IEventDefinition eventDefinition) {
      this.ILDasmPaper.Directive(".event");
      this.TypeMemberAccess(eventDefinition.Visibility);
      if (eventDefinition.IsSpecialName)
        this.ILDasmPaper.Keyword("specialname");
      if (eventDefinition.IsRuntimeSpecial)
        this.ILDasmPaper.Keyword("rtspecialname");
      this.TypeReference(eventDefinition.Type);
      this.ILDasmPaper.Identifier(eventDefinition.Name.Value);
      this.ILDasmPaper.NewLine();
      this.ILDasmPaper.OpenBlock();
      this.CustomAttributes(eventDefinition.Attributes);
      this.ILDasmPaper.Directive(".addon");
      this.MethodReference(eventDefinition.Adder);
      this.ILDasmPaper.NewLine();
      this.ILDasmPaper.Directive(".removeon");
      this.MethodReference(eventDefinition.Remover);
      this.ILDasmPaper.NewLine();
      if (eventDefinition.Caller != null) {
        this.ILDasmPaper.Directive(".fire");
        this.MethodReference(eventDefinition.Caller);
        this.ILDasmPaper.NewLine();
      }
      foreach (IMethodDefinition methodDef in eventDefinition.Accessors) {
        if (methodDef == eventDefinition.Adder) continue;
        if (methodDef == eventDefinition.Remover) continue;
        if (methodDef == eventDefinition.Caller) continue;
        this.ILDasmPaper.Directive(".other");
        this.MethodReference(methodDef);
        this.ILDasmPaper.NewLine();
      }
      this.ILDasmPaper.CloseBlock();
    }

    public void PropertyDefinition(IPropertyDefinition propertyDefinition) {
      this.ILDasmPaper.Directive(".property");
      this.TypeMemberAccess(propertyDefinition.Visibility);
      if (propertyDefinition.IsSpecialName)
        this.ILDasmPaper.Keyword("specialname");
      if (propertyDefinition.IsRuntimeSpecial)
        this.ILDasmPaper.Keyword("rtspecialname");
      if (!propertyDefinition.IsStatic)
        this.ILDasmPaper.Keyword("instance");
      if ((propertyDefinition.CallingConvention & CallingConvention.ExplicitThis) == CallingConvention.ExplicitThis)
        this.ILDasmPaper.Keyword("explicit");
      switch (propertyDefinition.CallingConvention & (CallingConvention)0x0F) {
        case CallingConvention.C:
          this.ILDasmPaper.Keyword("unmanaged cdecl");
          break;
        case CallingConvention.Default:
          this.ILDasmPaper.Keyword("default");
          break;
        case CallingConvention.ExtraArguments:
          this.ILDasmPaper.Keyword("vararg");
          break;
        case CallingConvention.FastCall:
          this.ILDasmPaper.Keyword("unmanaged fastcall");
          break;
        case CallingConvention.Standard:
          this.ILDasmPaper.Keyword("unmanaged stdcall");
          break;
        case CallingConvention.ThisCall:
          this.ILDasmPaper.Keyword("unmanaged thiscall");
          break;
      }
      this.TypeReference(propertyDefinition.Type);
      this.ILDasmPaper.Identifier(propertyDefinition.Name.Value);
      this.Parameters(propertyDefinition.Parameters);
      this.ILDasmPaper.NewLine();
      this.ILDasmPaper.OpenBlock();
      this.CustomAttributes(propertyDefinition.Attributes);
      if (propertyDefinition.Getter != null) {
        this.ILDasmPaper.Directive(".get");
        this.MethodReference(propertyDefinition.Getter);
        this.ILDasmPaper.NewLine();
      }
      if (propertyDefinition.Setter != null) {
        this.ILDasmPaper.Directive(".set");
        this.MethodReference(propertyDefinition.Setter);
        this.ILDasmPaper.NewLine();
      }
      foreach (IMethodReference methodRef in propertyDefinition.Accessors) {
        if (methodRef == propertyDefinition.Getter) continue;
        if (methodRef == propertyDefinition.Setter) continue;
        this.ILDasmPaper.Directive(".other");
        this.MethodReference(methodRef);
        this.ILDasmPaper.NewLine();
      }
      this.ILDasmPaper.CloseBlock();
    }

    public void Expression(IMetadataExpression expression) {
      IMetadataNamedArgument namedArg = expression as IMetadataNamedArgument;
      if (namedArg != null) {
        ITypeDefinitionMember tdm = namedArg.ResolvedDefinition as ITypeDefinitionMember;
        string tdmStr = Helper.TypeDefinitionMember(this.CurrentModule, tdm);
        this.ILDasmPaper.DataString(tdmStr);
        this.ILDasmPaper.Symbol("=");
        this.Expression(namedArg.ArgumentValue);
        return;
      }
      IMetadataConstant ct = expression as IMetadataConstant;
      if (ct != null) {
        this.ILDasmPaper.Keyword("const");
        this.ILDasmPaper.Symbol("(");
        this.ILDasmPaper.Constant(ct.Value);
        this.ILDasmPaper.Symbol(",");
        this.TypeReference(ct.Type);
        this.ILDasmPaper.Symbol(")");
        return;
      }
      IMetadataTypeOf te = expression as IMetadataTypeOf;
      if (te != null) {
        this.ILDasmPaper.Keyword("typeof");
        this.ILDasmPaper.Symbol("(");
        this.TypeDefinitionAsReference(te.TypeToGet);
        this.ILDasmPaper.Symbol(")");
        return;
      }
      IMetadataCreateArray arrc = expression as IMetadataCreateArray;
      if (arrc != null) {
        this.ILDasmPaper.Keyword("array");
        this.ILDasmPaper.Symbol("(");
        this.TypeReference(arrc.Type);
        this.ILDasmPaper.Symbol(")");
        this.ILDasmPaper.Symbol("{");
        bool isFirst = true;
        foreach (IMetadataExpression e in arrc.Initializers) {
          if (!isFirst)
            this.ILDasmPaper.Symbol(", ");
          this.Expression(e);
          isFirst = false;
        }
        this.ILDasmPaper.Symbol("}");
      }
    }

    public void CustomAttribute(ICustomAttribute customAttribute) {
      this.ILDasmPaper.Directive(".custom");
      this.MethodReference(customAttribute.Constructor);
      this.ILDasmPaper.NewLine();
      this.ILDasmPaper.OpenBlock();
      foreach (IMetadataExpression argument in customAttribute.Arguments) {
        this.ILDasmPaper.Directive(".argument");
        this.Expression(argument);
        this.ILDasmPaper.NewLine();
      }
      foreach (IMetadataExpression argument in customAttribute.NamedArguments) {
        this.ILDasmPaper.Directive(".argument");
        this.Expression(argument);
        this.ILDasmPaper.NewLine();
      }
      this.ILDasmPaper.CloseBlock();
    }

    public void CustomAttributes(IEnumerable<ICustomAttribute> customAttributes) {
      var custAttrs = new List<ICustomAttribute>(customAttributes);
      custAttrs.Sort(CompareTypeNames);
      foreach (ICustomAttribute ca in custAttrs) {
        this.CustomAttribute(ca);
      }
    }

    private static int CompareTypeNames(ICustomAttribute x, ICustomAttribute y) {
      return string.CompareOrdinal(TypeHelper.GetTypeName(x.Type), TypeHelper.GetTypeName(y.Type));
    }

    public void SecurityAttribute(ISecurityAttribute securityAttribute) {
      this.ILDasmPaper.Directive(".permissionset");
      switch (securityAttribute.Action) {
        case SecurityAction.Assert:
          this.ILDasmPaper.Keyword("assert");
          break;
        case SecurityAction.Demand:
          this.ILDasmPaper.Keyword("demand");
          break;
        case SecurityAction.Deny:
          this.ILDasmPaper.Keyword("deny");
          break;
        case SecurityAction.InheritanceDemand:
          this.ILDasmPaper.Keyword("inheritcheck");
          break;
        case SecurityAction.LinkDemand:
          this.ILDasmPaper.Keyword("linkcheck");
          break;
        case SecurityAction.PermitOnly:
          this.ILDasmPaper.Keyword("permitonly");
          break;
        case SecurityAction.RequestMinimum:
          this.ILDasmPaper.Keyword("reqmin");
          break;
        case SecurityAction.RequestOptional:
          this.ILDasmPaper.Keyword("reqopt");
          break;
        case SecurityAction.RequestRefuse:
          this.ILDasmPaper.Keyword("reqrefuse");
          break;
      }
      this.ILDasmPaper.NewLine();
      this.ILDasmPaper.OpenBlock();
      foreach (ICustomAttribute ca in securityAttribute.Attributes) {
        this.CustomAttribute(ca);
      }
      this.ILDasmPaper.CloseBlock();
    }

    public void SecurityAttributes(IEnumerable<ISecurityAttribute> securityAttributes) {
      foreach (ISecurityAttribute sa in securityAttributes) {
        this.SecurityAttribute(sa);
      }
    }

    void UnmanagedType(System.Runtime.InteropServices.UnmanagedType unmanagedType) {
      switch (unmanagedType) {
        case System.Runtime.InteropServices.UnmanagedType.AnsiBStr:
          this.ILDasmPaper.Keyword("ansi bstr");
          break;
        case System.Runtime.InteropServices.UnmanagedType.AsAny:
          this.ILDasmPaper.Keyword("as any");
          break;
        case System.Runtime.InteropServices.UnmanagedType.BStr:
          this.ILDasmPaper.Keyword("bstr");
          break;
        case System.Runtime.InteropServices.UnmanagedType.Bool:
          this.ILDasmPaper.Keyword("bool");
          break;
        case System.Runtime.InteropServices.UnmanagedType.ByValArray:
          this.ILDasmPaper.Keyword("XXX");
          break;
        case System.Runtime.InteropServices.UnmanagedType.ByValTStr:
          this.ILDasmPaper.Keyword("XXX");
          break;
        case System.Runtime.InteropServices.UnmanagedType.Currency:
          this.ILDasmPaper.Keyword("currency");
          break;
        case System.Runtime.InteropServices.UnmanagedType.CustomMarshaler:
          this.ILDasmPaper.Keyword("XXX");
          break;
        case System.Runtime.InteropServices.UnmanagedType.Error:
          this.ILDasmPaper.Keyword("error");
          break;
        case System.Runtime.InteropServices.UnmanagedType.FunctionPtr:
          this.ILDasmPaper.Keyword("method");
          break;
        case System.Runtime.InteropServices.UnmanagedType.I1:
          this.ILDasmPaper.Keyword("int8");
          break;
        case System.Runtime.InteropServices.UnmanagedType.I2:
          this.ILDasmPaper.Keyword("int16");
          break;
        case System.Runtime.InteropServices.UnmanagedType.I4:
          this.ILDasmPaper.Keyword("int32");
          break;
        case System.Runtime.InteropServices.UnmanagedType.I8:
          this.ILDasmPaper.Keyword("int64");
          break;
        case System.Runtime.InteropServices.UnmanagedType.IDispatch:
          this.ILDasmPaper.Keyword("idispatch");
          break;
        case System.Runtime.InteropServices.UnmanagedType.IUnknown:
          this.ILDasmPaper.Keyword("iunknown");
          break;
        case System.Runtime.InteropServices.UnmanagedType.Interface:
          this.ILDasmPaper.Keyword("interface");
          break;
        case System.Runtime.InteropServices.UnmanagedType.LPArray:
          this.ILDasmPaper.Keyword("XXX");
          break;
        case System.Runtime.InteropServices.UnmanagedType.LPStr:
          this.ILDasmPaper.Keyword("lpstr");
          break;
        case System.Runtime.InteropServices.UnmanagedType.LPStruct:
          this.ILDasmPaper.Keyword("lpstruct");
          break;
        case System.Runtime.InteropServices.UnmanagedType.LPTStr:
          this.ILDasmPaper.Keyword("lptstr");
          break;
        case System.Runtime.InteropServices.UnmanagedType.LPWStr:
          this.ILDasmPaper.Keyword("lpwstr");
          break;
        case System.Runtime.InteropServices.UnmanagedType.R4:
          this.ILDasmPaper.Keyword("float32");
          break;
        case System.Runtime.InteropServices.UnmanagedType.R8:
          this.ILDasmPaper.Keyword("float64");
          break;
        case System.Runtime.InteropServices.UnmanagedType.SafeArray:
          this.ILDasmPaper.Keyword("XXX");
          break;
        case System.Runtime.InteropServices.UnmanagedType.Struct:
          this.ILDasmPaper.Keyword("struct");
          break;
        case System.Runtime.InteropServices.UnmanagedType.SysInt:
          this.ILDasmPaper.Keyword("XXX");
          break;
        case System.Runtime.InteropServices.UnmanagedType.SysUInt:
          this.ILDasmPaper.Keyword("XXX");
          break;
        case System.Runtime.InteropServices.UnmanagedType.TBStr:
          this.ILDasmPaper.Keyword("tbstr");
          break;
        case System.Runtime.InteropServices.UnmanagedType.U1:
          this.ILDasmPaper.Keyword("unsigned int8");
          break;
        case System.Runtime.InteropServices.UnmanagedType.U2:
          this.ILDasmPaper.Keyword("unsigned int16");
          break;
        case System.Runtime.InteropServices.UnmanagedType.U4:
          this.ILDasmPaper.Keyword("unsigned int32");
          break;
        case System.Runtime.InteropServices.UnmanagedType.U8:
          this.ILDasmPaper.Keyword("unsigned int64");
          break;
        case System.Runtime.InteropServices.UnmanagedType.VBByRefStr:
          this.ILDasmPaper.Keyword("XXX");
          break;
        case System.Runtime.InteropServices.UnmanagedType.VariantBool:
          this.ILDasmPaper.Keyword("variant bool");
          break;
        default:
          break;
      }
    }

    public void MarshallingInformation(IMarshallingInformation marshallInfo) {
      this.ILDasmPaper.Keyword("marshal");
      this.ILDasmPaper.Symbol("(");
      if (marshallInfo.UnmanagedType == System.Runtime.InteropServices.UnmanagedType.CustomMarshaler) {
        this.ILDasmPaper.Keyword("custom");
        this.ILDasmPaper.Symbol("(");
        this.ILDasmPaper.Identifier(TypeHelper.GetTypeName(marshallInfo.CustomMarshaller));
        this.ILDasmPaper.Symbol(",");
        this.ILDasmPaper.Identifier(marshallInfo.CustomMarshallerRuntimeArgument);
        this.ILDasmPaper.Symbol(")");
      } else if (marshallInfo.UnmanagedType == System.Runtime.InteropServices.UnmanagedType.LPArray) {
        this.UnmanagedType(marshallInfo.ElementType);
        this.ILDasmPaper.Symbol(",");
        this.ILDasmPaper.Int((int)marshallInfo.NumberOfElements);
        this.ILDasmPaper.Symbol(",");
        this.ILDasmPaper.Int(marshallInfo.ParamIndex == null ? -1 : (int)marshallInfo.ParamIndex.Value);
      } else if (marshallInfo.UnmanagedType == System.Runtime.InteropServices.UnmanagedType.SafeArray) {
        this.ILDasmPaper.Identifier(marshallInfo.SafeArrayElementSubtype.ToString());
        this.ILDasmPaper.Symbol(",");
        this.ILDasmPaper.Identifier(TypeHelper.GetTypeName(marshallInfo.SafeArrayElementUserDefinedSubtype));
      } else {
        this.UnmanagedType(marshallInfo.UnmanagedType);
      }
      this.ILDasmPaper.Symbol(")");
    }
  }
}
