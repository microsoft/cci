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
using Microsoft.Cci;
using System.Globalization;
using System.Diagnostics.Contracts;

namespace CSharpSourceEmitter {
  public enum CSharpToken {
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
    Enum,
    Extern,
    Event,
    False,
    Fixed,
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
  }

  /// <summary>
  /// Prints out C# source corresponding to CCI nodes as they are visited.
  /// </summary>
  /// <remarks>
  /// Extenders can modify the output by overriding Traverse or Print* methods.
  /// This is a rather ugly and somewhat inflexible model.  A better approach would be to transform
  /// the CCI object model into a C# AST (parse tree), then let extenders mutate that model before 
  /// running a very simple visitor that prints it out as text.
  /// </remarks>
  public partial class SourceEmitter : ICSharpSourceEmitter {
    readonly protected ISourceEmitterOutput sourceEmitterOutput;
    protected bool printCompilerGeneratedMembers;

    [ContractInvariantMethod]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
    private void ObjectInvariant()
    {
      Contract.Invariant(this.sourceEmitterOutput != null);
    }

    public SourceEmitter(ISourceEmitterOutput sourceEmitterOutput, IMetadataHost hostEnvironment) {
      Contract.Requires(sourceEmitterOutput != null);

      this.sourceEmitterOutput = sourceEmitterOutput;
      this.LeftCurlyOnNewLine = true;
    }

    public SourceEmitter(ISourceEmitterOutput sourceEmitterOutput) : this(sourceEmitterOutput, null) {
      Contract.Requires(sourceEmitterOutput != null);
    }

    public bool LeftCurlyOnNewLine { get; set; }

    public virtual void PrintString(string str) {
      Contract.Requires(str != null);

      this.sourceEmitterOutput.Write(QuoteString(str));
    }

    public static string QuoteString(string str) {
      Contract.Requires(str != null);

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
      Contract.Requires(name != null);

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

    public virtual bool PrintToken(CSharpToken token) {
      switch (token) {
        case CSharpToken.Assign:
          sourceEmitterOutput.Write("=");
          break;
        case CSharpToken.NewLine:
          sourceEmitterOutput.WriteLine("");
          break;
        case CSharpToken.Indent:
          sourceEmitterOutput.Write("", true);
          break;
        case CSharpToken.Space:
          sourceEmitterOutput.Write(" ");
          break;
        case CSharpToken.Dot:
          sourceEmitterOutput.Write(".");
          break;
        case CSharpToken.LeftCurly:
          if (this.LeftCurlyOnNewLine) {
            if (!this.sourceEmitterOutput.CurrentLineEmpty)
              PrintToken(CSharpToken.NewLine);
          } else {
            PrintToken(CSharpToken.Space);
          }
          sourceEmitterOutput.WriteLine("{", this.LeftCurlyOnNewLine);
          sourceEmitterOutput.IncreaseIndent();
          break;
        case CSharpToken.RightCurly:
          sourceEmitterOutput.DecreaseIndent();
          sourceEmitterOutput.WriteLine("}", true);
          break;
        case CSharpToken.LeftParenthesis:
          sourceEmitterOutput.Write("(");
          break;
        case CSharpToken.RightParenthesis:
          sourceEmitterOutput.Write(")");
          break;
        case CSharpToken.LeftAngleBracket:
          sourceEmitterOutput.Write("<");
          break;
        case CSharpToken.RightAngleBracket:
          sourceEmitterOutput.Write(">");
          break;
        case CSharpToken.LeftSquareBracket:
          sourceEmitterOutput.Write("[");
          break;
        case CSharpToken.RightSquareBracket:
          sourceEmitterOutput.Write("]");
          break;
        case CSharpToken.Semicolon:
          sourceEmitterOutput.WriteLine(";");
          break;
        case CSharpToken.Colon:
          sourceEmitterOutput.Write(":");
          break;
        case CSharpToken.Comma:
          sourceEmitterOutput.Write(",");
          break;
        case CSharpToken.Tilde:
          sourceEmitterOutput.Write("~");
          break;
        case CSharpToken.Public:
          sourceEmitterOutput.Write("public ");
          break;
        case CSharpToken.Private:
          sourceEmitterOutput.Write("private ");
          break;
        case CSharpToken.Internal:
          sourceEmitterOutput.Write("internal ");
          break;
        case CSharpToken.Protected:
          sourceEmitterOutput.Write("protected ");
          break;
        case CSharpToken.Static:
          sourceEmitterOutput.Write("static ");
          break;
        case CSharpToken.Abstract:
          sourceEmitterOutput.Write("abstract ");
          break;
        case CSharpToken.Extern:
          sourceEmitterOutput.Write("extern ");
          break;
        case CSharpToken.Unsafe:
          sourceEmitterOutput.Write("unsafe ");
          break;
        case CSharpToken.ReadOnly:
          sourceEmitterOutput.Write("readonly ");
          break;
        case CSharpToken.Fixed:
          sourceEmitterOutput.Write("fixed ");
          break;
        case CSharpToken.New:
          sourceEmitterOutput.Write("new ");
          break;
        case CSharpToken.Sealed:
          sourceEmitterOutput.Write("sealed ");
          break;
        case CSharpToken.Virtual:
          sourceEmitterOutput.Write("virtual ");
          break;
        case CSharpToken.Override:
          sourceEmitterOutput.Write("override ");
          break;
        case CSharpToken.Class:
          sourceEmitterOutput.Write("class ");
          break;
        case CSharpToken.Interface:
          sourceEmitterOutput.Write("interface ");
          break;
        case CSharpToken.Struct:
          sourceEmitterOutput.Write("struct ");
          break;
        case CSharpToken.Enum:
          sourceEmitterOutput.Write("enum ");
          break;
        case CSharpToken.Delegate:
          sourceEmitterOutput.Write("delegate ");
          break;
        case CSharpToken.Event:
          sourceEmitterOutput.Write("event ");
          break;
        case CSharpToken.Namespace:
          sourceEmitterOutput.Write("namespace ");
          break;
        case CSharpToken.Null:
          sourceEmitterOutput.Write("null");
          break;
        case CSharpToken.In:
          sourceEmitterOutput.Write("in ");
          break;
        case CSharpToken.Out:
          sourceEmitterOutput.Write("out ");
          break;
        case CSharpToken.Ref:
          sourceEmitterOutput.Write("ref ");
          break;
        case CSharpToken.Boolean:
          sourceEmitterOutput.Write("boolean ");
          break;
        case CSharpToken.Byte:
          sourceEmitterOutput.Write("byte ");
          break;
        case CSharpToken.Char:
          sourceEmitterOutput.Write("char ");
          break;
        case CSharpToken.Double:
          sourceEmitterOutput.Write("double ");
          break;
        case CSharpToken.Short:
          sourceEmitterOutput.Write("short ");
          break;
        case CSharpToken.Int:
          sourceEmitterOutput.Write("int ");
          break;
        case CSharpToken.Long:
          sourceEmitterOutput.Write("long ");
          break;
        case CSharpToken.Object:
          sourceEmitterOutput.Write("object ");
          break;
        case CSharpToken.String:
          sourceEmitterOutput.Write("string ");
          break;
        case CSharpToken.UShort:
          sourceEmitterOutput.Write("ushort ");
          break;
        case CSharpToken.UInt:
          sourceEmitterOutput.Write("uint ");
          break;
        case CSharpToken.ULong:
          sourceEmitterOutput.Write("ulong ");
          break;
        case CSharpToken.Get:
          sourceEmitterOutput.Write("get");
          break;
        case CSharpToken.Set:
          sourceEmitterOutput.Write("set");
          break;
        case CSharpToken.Add:
          sourceEmitterOutput.Write("add");
          break;
        case CSharpToken.Remove:
          sourceEmitterOutput.Write("remove");
          break;
        case CSharpToken.Return:
          sourceEmitterOutput.Write("return");
          break;
        case CSharpToken.This:
          sourceEmitterOutput.Write("this");
          break;
        case CSharpToken.Throw:
          sourceEmitterOutput.Write("throw");
          break;
        case CSharpToken.Try:
          sourceEmitterOutput.Write("try");
          break;
        case CSharpToken.YieldReturn:
          sourceEmitterOutput.Write("yield return");
          break;
        case CSharpToken.YieldBreak:
          sourceEmitterOutput.Write("yield break");
          break;
        case CSharpToken.True:
          sourceEmitterOutput.Write("true");
          break;
        case CSharpToken.False:
          sourceEmitterOutput.Write("false");
          break;
        case CSharpToken.TypeOf:
          sourceEmitterOutput.Write("typeof");
          break;
        default:
          sourceEmitterOutput.Write("Unknown-token");
          break;
      }

      return true;
    }

    public virtual bool PrintKeywordNamespace() {
      return PrintToken(CSharpToken.Namespace);
    }

    public virtual bool PrintKeywordAbstract() {
      PrintToken(CSharpToken.Abstract);
      return true;
    }

    public virtual bool PrintKeywordExtern() {
      PrintToken(CSharpToken.Extern);
      return true;
    }

    public virtual bool PrintKeywordSealed() {
      PrintToken(CSharpToken.Sealed);
      return true;
    }

    public virtual bool PrintKeywordStatic() {
      PrintToken(CSharpToken.Static);
      return true;
    }

    public virtual bool PrintKeywordFixed() {
      PrintToken(CSharpToken.Fixed);
      return true;
    }

    public virtual bool PrintKeywordUnsafe() {
      PrintToken(CSharpToken.Unsafe);
      return true;
    }

    public virtual bool PrintKeywordPublic() {
      PrintToken(CSharpToken.Public);
      return true;
    }

    public virtual bool PrintKeywordPrivate() {
      PrintToken(CSharpToken.Private);
      return true;
    }

    public virtual bool PrintKeywordInternal() {
      PrintToken(CSharpToken.Internal);
      return true;
    }

    public virtual bool PrintKeywordProtected() {
      PrintToken(CSharpToken.Protected);
      return true;
    }

    public virtual bool PrintKeywordProtectedInternal() {
      PrintToken(CSharpToken.Protected);
      PrintToken(CSharpToken.Internal);
      return true;
    }

    public virtual bool PrintKeywordReadOnly() {
      PrintToken(CSharpToken.ReadOnly);
      return true;
    }

    public virtual bool PrintKeywordNew() {
      PrintToken(CSharpToken.New);
      return true;
    }

    public virtual bool PrintKeywordVirtual() {
      PrintToken(CSharpToken.Virtual);
      return true;
    }

    public virtual bool PrintKeywordOverride() {
      PrintToken(CSharpToken.Override);
      return true;
    }

    public virtual bool PrintKeywordIn() {
      PrintToken(CSharpToken.In);
      return true;
    }

    public virtual bool PrintKeywordOut() {
      PrintToken(CSharpToken.Out);
      return true;
    }

    public virtual bool PrintKeywordRef() {
      PrintToken(CSharpToken.Ref);
      return true;
    }

    public virtual bool PrintPrimitive(System.TypeCode typeCode) {
      switch (typeCode) {
        case System.TypeCode.Boolean:
          PrintToken(CSharpToken.Boolean);
          break;
        case System.TypeCode.Byte:
          PrintToken(CSharpToken.Byte);
          break;
        case System.TypeCode.Char:
          PrintToken(CSharpToken.Char);
          break;
        case System.TypeCode.Decimal:
          PrintToken(CSharpToken.Double);
          break;
        case System.TypeCode.Int16:
          PrintToken(CSharpToken.Short);
          break;
        case System.TypeCode.Int32:
          PrintToken(CSharpToken.Int);
          break;
        case System.TypeCode.Int64:
          PrintToken(CSharpToken.Long);
          break;
        case System.TypeCode.Object:
          PrintToken(CSharpToken.Object);
          break;
        case System.TypeCode.String:
          PrintToken(CSharpToken.String);
          break;
        case System.TypeCode.UInt16:
          PrintToken(CSharpToken.UShort);
          break;
        case System.TypeCode.UInt32:
          PrintToken(CSharpToken.UInt);
          break;
        case System.TypeCode.UInt64:
          PrintToken(CSharpToken.ULong);
          break;
        default:
          // This is not a primitive type.
          return false;
      }

      return true;
    }

  }
}
