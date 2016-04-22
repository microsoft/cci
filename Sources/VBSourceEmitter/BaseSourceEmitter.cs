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
using System.Diagnostics.Contracts;
using System.Text;
using Microsoft.Cci;
using Ast = Microsoft.Cci.Ast;
using System.Globalization;

namespace VBSourceEmitter {
  public enum VBToken {
    Abstract,
    Add,
    Assign,
    Boolean,
    Byte,
    Char,
    Class,
    Colon,
    Comma,
    Delegate,
    Dot,
    Double,
    End,
    Enum,
    Extern,
    Event,
    False,
    Fixed,
    Function,
    Get,
    In,
    Indent,
    Int,
    Interface,
    Internal,
    LeftAngleBracket,
    LeftCurly,
    LeftParenthesis,
    LeftSquareBracket,
    Long,
    Base,
    Namespace,
    New,
    NewLine,
    Null,
    Object,
    Out,
    Override,
    Private,
    Protected,
    Public,
    ReadOnly,
    Ref,
    Remove,
    Return,
    RightAngleBracket,
    RightCurly,
    RightParenthesis,
    RightSquareBracket,
    Sealed,
    Semicolon,
    Set,
    Short,
    Space,
    Static,
    String,
    Struct,
    Tilde,
    This,
    Throw,
    True,
    Try,
    TypeOf,
    UInt,
    ULong,
    Unsafe,
    UShort,
    Virtual,
    YieldBreak,
    YieldReturn,
    As,
    Sub,
  }

  /// <summary>
  /// Prints out VB source corresponding to CCI nodes as they are visited.
  /// </summary>
  /// <remarks>
  /// Extenders can modify the output by overriding Traverse or Print* methods.
  /// This is a rather ugly and somewhat inflexible model.  A better approach would be to transform
  /// the CCI object model into a VB AST (parse tree), then let extenders mutate that model before 
  /// running a very simple visitor that prints it out as text.
  /// </remarks>
  public partial class SourceEmitter : IVBSourceEmitter {
    protected ISourceEmitterOutput sourceEmitterOutput;
    protected bool printCompilerGeneratedMembers;
    protected IMetadataHost host;
    private VBHelper helper;

    public SourceEmitter(IMetadataHost hostEnvironment, ISourceEmitterOutput sourceEmitterOutput) {
      this.host = hostEnvironment;
      this.sourceEmitterOutput = sourceEmitterOutput;
      this.LeftCurlyOnNewLine = true;
      this.helper = new VBHelper();
    }

    public bool LeftCurlyOnNewLine { get; set; }

    public virtual void PrintString(string str) {
      this.sourceEmitterOutput.Write(QuoteString(str));
    }

    public static string QuoteString(string str) {
      StringBuilder sb = new StringBuilder(str.Length + 4);
      sb.Append("\"");
      foreach (char ch in str) {
        sb.Append(EscapeChar(ch, true));
      }
      sb.Append("\"");
      return sb.ToString();
    }

    public static string EscapeChar(char c, bool inString) {
      switch (c)
      {
        case '\r': return @"\r";
        case '\n': return @"\n";
        case '\f': return @"\f";
        case '\t': return @"\t";
        case '\v': return @"\v";
        case '\0': return @"\0";
        case '\a': return @"\a";
        case '\b': return @"\b";
        case '\\': return @"\\";
        case '\'': return inString ? "'" : @"\'";
        case '"': return inString ? "\\\"" : "\"";
      }
      var cat = Char.GetUnicodeCategory(c);
      if (cat == UnicodeCategory.Control ||
        cat == UnicodeCategory.LineSeparator ||
        cat == UnicodeCategory.Format || 
        cat == UnicodeCategory.Surrogate || 
        cat == UnicodeCategory.PrivateUse || 
        cat == UnicodeCategory.OtherNotAssigned)
        return String.Format("\\u{0:X4}", (int)c);
      return c.ToString();
    }


    public virtual void PrintIdentifier(IName name) {
      sourceEmitterOutput.Write(EscapeIdentifier(name.Value));
    }

    public static string EscapeIdentifier(string identifier) {
      // Check to see if this is a keyword, and if so escape it with '@' prefix
      switch (identifier) {
        case "abstract":   case "as":          case "base":      case "bool":      case "break":
        case "byte":       case "case":        case "catch":     case "char":      case "checked":     case "class":
        case "const":      case "continue":    case "decimal":   case "default":   case "delegate":    case "do":
        case "double":     case "else":        case "enum":      case "event":     case "explicit":    case "extern":
        case "false":      case "finally":     case "fixed":     case "float":     case "for":         case "foreach":
        case "goto":       case "if":          case "implicit":  case "in":        case "int":         case "interface":
        case "internal":   case "is":          case "lock":      case "long":      case "namespace":   case "new":
        case "null":       case "object":      case "operator":  case "out":       case "override":    case "params":
        case "private":    case "protected":   case "public":    case "readonly":  case "ref":         case "return":
        case "sbyte":      case "sealed":      case "short":     case "sizeof":    case "stackalloc":  case "static":
        case "string":     case "struct":      case "switch":    case "this":      case "throw":       case "true":
        case "try":        case "typeof":      case "uint":      case "ulong":     case "unchecked":   case "unsafe":
        case "ushort":     case "using":       case "virtual":   case "void":      case "volatile":    case "while":
          return "@" + identifier;
      }

      // Not a keyword, just return it
      // It may still have characters that are invalid for an identifier, but C# doesn't provide any way to
      // escape those (even unicode escapes must conform to the required character classes)
      return identifier;
    }

    public virtual bool PrintToken(VBToken token) {
      switch (token) {
        case VBToken.Assign:
          sourceEmitterOutput.Write("=");
          break;
        case VBToken.NewLine:
          sourceEmitterOutput.WriteLine("");
          break;
        case VBToken.Indent:
          sourceEmitterOutput.Write("", true);
          break;
        case VBToken.Space:
          sourceEmitterOutput.Write(" ");
          break;
        case VBToken.Dot:
          sourceEmitterOutput.Write(".");
          break;
        case VBToken.LeftCurly:
          if (this.LeftCurlyOnNewLine) {
            if (!this.sourceEmitterOutput.CurrentLineEmpty)
              PrintToken(VBToken.NewLine);
          } else {
            PrintToken(VBToken.Space);
          }
          sourceEmitterOutput.WriteLine("", this.LeftCurlyOnNewLine);
          sourceEmitterOutput.IncreaseIndent();
          break;
        case VBToken.RightCurly:
          sourceEmitterOutput.DecreaseIndent();
          sourceEmitterOutput.WriteLine("", true);
          break;
        case VBToken.LeftParenthesis:
          sourceEmitterOutput.Write("(");
          break;
        case VBToken.RightParenthesis:
          sourceEmitterOutput.Write(")");
          break;
        case VBToken.LeftAngleBracket:
          sourceEmitterOutput.Write("<");
          break;
        case VBToken.RightAngleBracket:
          sourceEmitterOutput.Write(">");
          break;
        case VBToken.LeftSquareBracket:
          sourceEmitterOutput.Write("[");
          break;
        case VBToken.RightSquareBracket:
          sourceEmitterOutput.Write("]");
          break;
        case VBToken.Semicolon:
          sourceEmitterOutput.WriteLine(";");
          break;
        case VBToken.Colon:
          sourceEmitterOutput.Write(":");
          break;
        case VBToken.Comma:
          sourceEmitterOutput.Write(",");
          break;
        case VBToken.Tilde:
          sourceEmitterOutput.Write("~");
          break;
        case VBToken.Public:
          sourceEmitterOutput.Write("Public ");
          break;
        case VBToken.Private:
          sourceEmitterOutput.Write("Private ");
          break;
        case VBToken.Internal:
          sourceEmitterOutput.Write("Internal ");
          break;
        case VBToken.Protected:
          sourceEmitterOutput.Write("Protected ");
          break;
        case VBToken.Static:
          sourceEmitterOutput.Write("Static ");
          break;
        case VBToken.Abstract:
          sourceEmitterOutput.Write("Abstract ");
          break;
        case VBToken.Extern:
          sourceEmitterOutput.Write("Extern ");
          break;
        case VBToken.Unsafe:
          sourceEmitterOutput.Write("Unsafe ");
          break;
        case VBToken.ReadOnly:
          sourceEmitterOutput.Write("Readonly ");
          break;
        case VBToken.Fixed:
          sourceEmitterOutput.Write("Fixed ");
          break;
        case VBToken.New:
          sourceEmitterOutput.Write("New ");
          break;
        case VBToken.Sealed:
          sourceEmitterOutput.Write("Sealed ");
          break;
        case VBToken.Virtual:
          sourceEmitterOutput.Write("Virtual ");
          break;
        case VBToken.Override:
          sourceEmitterOutput.Write("Override ");
          break;
        case VBToken.Class:
          sourceEmitterOutput.Write("Class ");
          break;
        case VBToken.Interface:
          sourceEmitterOutput.Write("Interface ");
          break;
        case VBToken.Struct:
          sourceEmitterOutput.Write("Structure ");
          break;
        case VBToken.Enum:
          sourceEmitterOutput.Write("Enum ");
          break;
        case VBToken.Delegate:
          sourceEmitterOutput.Write("Delegate ");
          break;
        case VBToken.Event:
          sourceEmitterOutput.Write("Event ");
          break;
        case VBToken.Namespace:
          sourceEmitterOutput.Write("Namespace ");
          break;
        case VBToken.Null:
          sourceEmitterOutput.Write("Nothing");
          break;
        case VBToken.In:
          sourceEmitterOutput.Write("In ");
          break;
        case VBToken.Out:
          sourceEmitterOutput.Write("Out ");
          break;
        case VBToken.Ref:
          sourceEmitterOutput.Write("Ref ");
          break;
        case VBToken.Boolean:
          sourceEmitterOutput.Write("Boolean ");
          break;
        case VBToken.Byte:
          sourceEmitterOutput.Write("Byte ");
          break;
        case VBToken.Char:
          sourceEmitterOutput.Write("Char ");
          break;
        case VBToken.Double:
          sourceEmitterOutput.Write("Double ");
          break;
        case VBToken.Short:
          sourceEmitterOutput.Write("Short ");
          break;
        case VBToken.Int:
          sourceEmitterOutput.Write("Integer ");
          break;
        case VBToken.Long:
          sourceEmitterOutput.Write("Long ");
          break;
        case VBToken.Object:
          sourceEmitterOutput.Write("Object ");
          break;
        case VBToken.String:
          sourceEmitterOutput.Write("String");
          break;
        case VBToken.UShort:
          sourceEmitterOutput.Write("UShort ");
          break;
        case VBToken.UInt:
          sourceEmitterOutput.Write("UInteger ");
          break;
        case VBToken.ULong:
          sourceEmitterOutput.Write("ULong ");
          break;
        case VBToken.Get:
          sourceEmitterOutput.Write("Get");
          break;
        case VBToken.Set:
          sourceEmitterOutput.Write("Set");
          break;
        case VBToken.Add:
          sourceEmitterOutput.Write("Add");
          break;
        case VBToken.Remove:
          sourceEmitterOutput.Write("Remove");
          break;
        case VBToken.Return:
          sourceEmitterOutput.Write("Return");
          break;
        case VBToken.This:
          sourceEmitterOutput.Write("Me");
          break;
        case VBToken.Throw:
          sourceEmitterOutput.Write("Throw");
          break;
        case VBToken.Try:
          sourceEmitterOutput.Write("Try");
          break;
        case VBToken.YieldReturn:
          sourceEmitterOutput.Write("yield return");
          break;
        case VBToken.YieldBreak:
          sourceEmitterOutput.Write("yield break");
          break;
        case VBToken.True:
          sourceEmitterOutput.Write("True");
          break;
        case VBToken.False:
          sourceEmitterOutput.Write("False");
          break;
        case VBToken.TypeOf:
          sourceEmitterOutput.Write("Typeof");
          break;
        case VBToken.End:
          sourceEmitterOutput.Write("End");
          break;
        case VBToken.As:
          sourceEmitterOutput.Write("As");
          break;
        case VBToken.Sub:
          sourceEmitterOutput.Write("Sub");
          break;
        case VBToken.Base:
          sourceEmitterOutput.Write("MyBase");
          break;
        case VBToken.Function:
          sourceEmitterOutput.Write("Function");
          break;
        default:
          sourceEmitterOutput.Write("Unknown-token");
          break;
      }

      return true;
    }

    public virtual bool PrintKeywordNamespace() {
      return PrintToken(VBToken.Namespace);
    }

    public virtual bool PrintKeywordAbstract() {
      PrintToken(VBToken.Abstract);
      return true;
    }

    public virtual bool PrintKeywordExtern() {
      PrintToken(VBToken.Extern);
      return true;
    }

    public virtual bool PrintKeywordSealed() {
      PrintToken(VBToken.Sealed);
      return true;
    }

    public virtual bool PrintKeywordStatic() {
      PrintToken(VBToken.Static);
      return true;
    }

    public virtual bool PrintKeywordFixed() {
      PrintToken(VBToken.Fixed);
      return true;
    }

    public virtual bool PrintKeywordUnsafe() {
      PrintToken(VBToken.Unsafe);
      return true;
    }

    public virtual bool PrintKeywordPublic() {
      PrintToken(VBToken.Public);
      return true;
    }

    public virtual bool PrintKeywordPrivate() {
      PrintToken(VBToken.Private);
      return true;
    }

    public virtual bool PrintKeywordInternal() {
      PrintToken(VBToken.Internal);
      return true;
    }

    public virtual bool PrintKeywordProtected() {
      PrintToken(VBToken.Protected);
      return true;
    }

    public virtual bool PrintKeywordProtectedInternal() {
      PrintToken(VBToken.Protected);
      PrintToken(VBToken.Internal);
      return true;
    }

    public virtual bool PrintKeywordReadOnly() {
      PrintToken(VBToken.ReadOnly);
      return true;
    }

    public virtual bool PrintKeywordNew() {
      PrintToken(VBToken.New);
      return true;
    }

    public virtual bool PrintKeywordVirtual() {
      PrintToken(VBToken.Virtual);
      return true;
    }

    public virtual bool PrintKeywordOverride() {
      PrintToken(VBToken.Override);
      return true;
    }

    public virtual bool PrintKeywordIn() {
      PrintToken(VBToken.In);
      return true;
    }

    public virtual bool PrintKeywordOut() {
      PrintToken(VBToken.Out);
      return true;
    }

    public virtual bool PrintKeywordRef() {
      PrintToken(VBToken.Ref);
      return true;
    }

    public virtual bool PrintPrimitive(System.TypeCode typeCode) {
      switch (typeCode) {
        case System.TypeCode.Boolean:
          PrintToken(VBToken.Boolean);
          break;
        case System.TypeCode.Byte:
          PrintToken(VBToken.Byte);
          break;
        case System.TypeCode.Char:
          PrintToken(VBToken.Char);
          break;
        case System.TypeCode.Decimal:
          PrintToken(VBToken.Double);
          break;
        case System.TypeCode.Int16:
          PrintToken(VBToken.Short);
          break;
        case System.TypeCode.Int32:
          PrintToken(VBToken.Int);
          break;
        case System.TypeCode.Int64:
          PrintToken(VBToken.Long);
          break;
        case System.TypeCode.Object:
          PrintToken(VBToken.Object);
          break;
        case System.TypeCode.String:
          PrintToken(VBToken.String);
          break;
        case System.TypeCode.UInt16:
          PrintToken(VBToken.UShort);
          break;
        case System.TypeCode.UInt32:
          PrintToken(VBToken.UInt);
          break;
        case System.TypeCode.UInt64:
          PrintToken(VBToken.ULong);
          break;
        default:
          // This is not a primitive type.
          return false;
      }

      return true;
    }

    public virtual bool PrintPrimitive(PrimitiveTypeCode typeCode) {
      switch (typeCode) {
        case PrimitiveTypeCode.Boolean:
          PrintToken(VBToken.Boolean);
          break;
        case PrimitiveTypeCode.Int8:
          PrintToken(VBToken.Byte);
          break;
        case PrimitiveTypeCode.Char:
          PrintToken(VBToken.Char);
          break;
        case PrimitiveTypeCode.Int16:
          PrintToken(VBToken.Short);
          break;
        case PrimitiveTypeCode.Int32:
          PrintToken(VBToken.Int);
          break;
        case PrimitiveTypeCode.Int64:
          PrintToken(VBToken.Long);
          break;
        case PrimitiveTypeCode.String:
          PrintToken(VBToken.String);
          break;
        case PrimitiveTypeCode.UInt16:
          PrintToken(VBToken.UShort);
          break;
        case PrimitiveTypeCode.UInt32:
          PrintToken(VBToken.UInt);
          break;
        case PrimitiveTypeCode.UInt64:
          PrintToken(VBToken.ULong);
          break;
        default:
          // This is not a primitive type.
          return false;
      }

      return true;
    }

  }

  public class VBHelper : Ast.LanguageSpecificCompilationHelper {
    public VBHelper()
      : base(null, "VB") {
    }

    /// <summary>
    /// Creates an instance of a language specific object that formats type names according to the syntax of the language.
    /// </summary>
    protected override TypeNameFormatter CreateTypeNameFormatter() {
      return new VBTypeNameFormatter();
    }

    protected override SignatureFormatter CreateSignatureFormatter() {
      return new VBSignatureFormatter(this.TypeNameFormatter);
    }

  }

  public class VBTypeNameFormatter : TypeNameFormatter {

    /// <summary>
    /// Returns a C#-like string that corresponds to the given type definition and that conforms to the specified formatting options.
    /// </summary>
    [Pure]
    public override string GetTypeName(ITypeReference type, NameFormattingOptions formattingOptions) {
      Contract.Requires(type != null);
      Contract.Ensures(Contract.Result<string>() != null);

      if (type is Dummy) return "Microsoft.Cci.DummyTypeReference";
      if ((formattingOptions & NameFormattingOptions.UseTypeKeywords) != 0) {
        switch (type.TypeCode) {
          case PrimitiveTypeCode.Boolean: return "Boolean";
          case PrimitiveTypeCode.Char: return "Char";
          case PrimitiveTypeCode.Float32: return "Float";
          case PrimitiveTypeCode.Float64: return "Double";
          case PrimitiveTypeCode.Int16: return "Short";
          case PrimitiveTypeCode.Int32: return "Integer";
          case PrimitiveTypeCode.Int64: return "Long";
          case PrimitiveTypeCode.Int8: return "SByte";
          case PrimitiveTypeCode.String: return "String";
          case PrimitiveTypeCode.UInt16: return "UShort";
          case PrimitiveTypeCode.UInt32: return "UInteger";
          case PrimitiveTypeCode.UInt64: return "ULong";
          case PrimitiveTypeCode.UInt8: return "Byte";
          case PrimitiveTypeCode.Void: { Contract.Assert(false); throw new InvalidOperationException(); }
          case PrimitiveTypeCode.NotPrimitive:
            if (TypeHelper.TypesAreEquivalent(type, type.PlatformType.SystemDecimal)) return "Decimal";
            if (TypeHelper.TypesAreEquivalent(type, type.PlatformType.SystemObject)) return "Object";
            break;
        }
      }
      IArrayTypeReference/*?*/ arrayType = type as IArrayTypeReference;
      if (arrayType != null) return this.GetArrayTypeName(arrayType, formattingOptions);
      IFunctionPointerTypeReference/*?*/ functionPointerType = type as IFunctionPointerTypeReference;
      if (functionPointerType != null) return this.GetFunctionPointerTypeName(functionPointerType, formattingOptions);
      IGenericTypeParameterReference/*?*/ genericTypeParam = type as IGenericTypeParameterReference;
      if (genericTypeParam != null) return this.GetGenericTypeParameterName(genericTypeParam, formattingOptions);
      IGenericMethodParameterReference/*?*/ genericMethodParam = type as IGenericMethodParameterReference;
      if (genericMethodParam != null) return this.GetGenericMethodParameterName(genericMethodParam, formattingOptions);
      IGenericTypeInstanceReference/*?*/ genericInstance = type as IGenericTypeInstanceReference;
      if (genericInstance != null) return this.GetGenericTypeInstanceName(genericInstance, formattingOptions);
      INestedTypeReference/*?*/ ntTypeDef = type as INestedTypeReference;
      if (ntTypeDef != null) return this.GetNestedTypeName(ntTypeDef, formattingOptions);
      INamespaceTypeReference/*?*/ nsTypeDef = type as INamespaceTypeReference;
      if (nsTypeDef != null) return this.GetNamespaceTypeName(nsTypeDef, formattingOptions);
      IPointerTypeReference/*?*/ pointerType = type as IPointerTypeReference;
      if (pointerType != null) return this.GetPointerTypeName(pointerType, formattingOptions);
      IManagedPointerTypeReference/*?*/ managedPointerType = type as IManagedPointerTypeReference;
      if (managedPointerType != null) return this.GetManagedPointerTypeName(managedPointerType, formattingOptions);
      IModifiedTypeReference/*?*/ modifiedType = type as IModifiedTypeReference;
      if (modifiedType != null) return this.GetModifiedTypeName(modifiedType, formattingOptions);
      if (type.ResolvedType != type && !(type.ResolvedType is Dummy)) return this.GetTypeName(type.ResolvedType, formattingOptions);
      return "unknown type: " + type.GetType().ToString();
    }


    /// <summary>
    /// Returns a VB-like string that corresponds to a source expression that would bind to the given generic type instance when appearing in an appropriate context.
    /// </summary>
    [Pure]
    protected override string GetGenericTypeInstanceName(IGenericTypeInstanceReference genericTypeInstance, NameFormattingOptions formattingOptions) {
      Contract.Requires(genericTypeInstance != null);
      Contract.Ensures(Contract.Result<string>() != null);

      ITypeReference genericType = genericTypeInstance.GenericType;
      if ((formattingOptions & NameFormattingOptions.ContractNullable) != 0) {
        if (TypeHelper.TypesAreEquivalent(genericType, genericTypeInstance.PlatformType.SystemNullable)) {
          foreach (ITypeReference tref in genericTypeInstance.GenericArguments) {
            return this.GetTypeName(tref, formattingOptions) + "?";
          }
        }
      }
      if ((formattingOptions & NameFormattingOptions.OmitTypeArguments) == 0) {
        // Don't include the type parameters if we are to include the type arguments
        // If formatting for a documentation id, don't use generic type name suffixes.
        StringBuilder sb = new StringBuilder(this.GetTypeName(genericType, formattingOptions & ~(NameFormattingOptions.TypeParameters | ((formattingOptions & NameFormattingOptions.FormattingForDocumentationId) != 0 ? NameFormattingOptions.UseGenericTypeNameSuffix : NameFormattingOptions.None))));
        if ((formattingOptions & NameFormattingOptions.FormattingForDocumentationId) != 0) sb.Append("{"); else sb.Append("(Of ");
        bool first = true;
        string delim = ((formattingOptions & NameFormattingOptions.OmitWhiteSpaceAfterListDelimiter) == 0) ? ", " : ",";
        foreach (ITypeReference argument in genericTypeInstance.GenericArguments) {
          if (first) first = false; else sb.Append(delim);
          sb.Append(this.GetTypeName(argument, formattingOptions & ~(NameFormattingOptions.MemberKind | NameFormattingOptions.DocumentationIdMemberKind)));
        }
        if ((formattingOptions & NameFormattingOptions.FormattingForDocumentationId) != 0) sb.Append("}"); else sb.Append(")");
        return sb.ToString();
      }
      //If type arguments are not wanted, then type parameters are not going to be welcome either.
      return this.GetTypeName(genericType, formattingOptions & ~NameFormattingOptions.TypeParameters);
    }

    /// <summary>
    /// Appends a C#-like specific string of the dimensions of the given array type reference to the given StringBuilder.
    /// <example>For example, this appends the "[][,]" part of an array like "int[][,]".</example>
    /// </summary>
    protected override void AppendArrayDimensions(IArrayTypeReference arrayType, StringBuilder sb, NameFormattingOptions formattingOptions) {
      Contract.Requires(arrayType != null);
      Contract.Requires(sb != null);

      IArrayTypeReference/*?*/ elementArrayType = arrayType.ElementType as IArrayTypeReference;
      bool formattingForDocumentationId = (formattingOptions & NameFormattingOptions.FormattingForDocumentationId) != 0;
      if (formattingForDocumentationId && elementArrayType != null) { //Append the outer dimensions of the array first
        this.AppendArrayDimensions(elementArrayType, sb, formattingOptions);
      }
      sb.Append("(");
      if (!arrayType.IsVector) {
        if (formattingForDocumentationId) {
          bool first = true;
          IEnumerator<int> lowerBounds = arrayType.LowerBounds.GetEnumerator();
          IEnumerator<ulong> sizes = arrayType.Sizes.GetEnumerator();
          for (int i = 0; i < arrayType.Rank; i++) {
            if (!first) sb.Append(","); first = false;
            if (lowerBounds.MoveNext()) {
              sb.Append(lowerBounds.Current);
              sb.Append(":");
              if (sizes.MoveNext()) sb.Append(sizes.Current);
            } else {
              if (sizes.MoveNext()) sb.Append("0:" + sizes.Current);
            }
          }
        } else {
          sb.Append(',', (int)arrayType.Rank - 1);
        }
      }
      sb.Append(")");
      if (!formattingForDocumentationId && elementArrayType != null) { //Append the inner dimensions of the array first
        this.AppendArrayDimensions(elementArrayType, sb, formattingOptions);
      }
    }

  }

  public class VBSignatureFormatter : SignatureFormatter {

    public VBSignatureFormatter(TypeNameFormatter typeNameFormatter)
      : base(typeNameFormatter) {
    }

    /// <summary>
    /// Appends a formatted string of type arguments. Enclosed in angle brackets and comma-delimited.
    /// </summary>
    protected override void AppendGenericArguments(IGenericMethodInstanceReference method, NameFormattingOptions formattingOptions, StringBuilder sb) {
      Contract.Requires(method != null);
      Contract.Requires(sb != null);

      if ((formattingOptions & NameFormattingOptions.OmitTypeArguments) != 0) return;
      sb.Append("(Of ");
      bool first = true;
      string delim = ((formattingOptions & NameFormattingOptions.OmitWhiteSpaceAfterListDelimiter) == 0) ? ", " : ",";
      foreach (ITypeReference argument in method.GenericArguments) {
        if (first) first = false; else sb.Append(delim);
        sb.Append(this.typeNameFormatter.GetTypeName(argument, formattingOptions));
      }
      sb.Append(")");
    }

  }


}
