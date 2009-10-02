//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Cci;

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
    Dot,
    Double,
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
    This,
    Throw,
    Try,
    UInt,
    ULong,
    UShort,
    Virtual,
    YieldBreak,
    YieldReturn,
  }

  public partial class SourceEmitter : ICSharpSourceEmitter {
    protected ISourceEmitterOutput sourceEmitterOutput;

    public SourceEmitter(ISourceEmitterOutput sourceEmitterOutput, IMetadataHost hostEnvironment) {
      this.sourceEmitterOutput = sourceEmitterOutput;
    }

    public SourceEmitter(ISourceEmitterOutput sourceEmitterOutput) {
      this.sourceEmitterOutput = sourceEmitterOutput;
    }

    public virtual void PrintAttributes(IEnumerable<ICustomAttribute> attributes) {
      foreach (var attribute in attributes) {
        this.PrintAttribute(attribute, true, null);
      }
    }

    public virtual void PrintAttribute(ICustomAttribute attribute, bool notInline, string target) {
      this.sourceEmitterOutput.Write("[", notInline);
      if (target != null) {
        this.sourceEmitterOutput.Write(target);
        this.sourceEmitterOutput.Write(": ");
      }
      this.PrintTypeReferenceName(attribute.Constructor.ContainingType);
      if (attribute.NumberOfNamedArguments > 0 || IteratorHelper.EnumerableIsNotEmpty(attribute.Arguments)) {
        this.sourceEmitterOutput.Write("(");
        bool first = true;
        foreach (var argument in attribute.Arguments) {
          if (first)
            first = false;
          else
            this.sourceEmitterOutput.Write(", ");
          this.Visit(argument);
        }
        foreach (var namedArgument in attribute.NamedArguments) {
          if (first)
            first = false;
          else
            this.sourceEmitterOutput.Write(", ");
          this.Visit(namedArgument);
        }
        this.sourceEmitterOutput.Write(")");
      }
      this.sourceEmitterOutput.Write("]");
      if (notInline) this.sourceEmitterOutput.WriteLine("");
    }

    public virtual void PrintString(string str) {
      this.sourceEmitterOutput.Write("\"");
      foreach (char ch in str) {
        if (ch == '\\')
          this.sourceEmitterOutput.Write("\\\\");
        else if (ch == '"')
          this.sourceEmitterOutput.Write("\\\"");
        else
          this.sourceEmitterOutput.Write(ch.ToString());
      }
      this.sourceEmitterOutput.Write("\"");
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
          sourceEmitterOutput.WriteLine("{", true);
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
        case CSharpToken.ReadOnly:
          sourceEmitterOutput.Write("readonly ");
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
        case CSharpToken.Class:
          sourceEmitterOutput.Write("class ");
          break;
        case CSharpToken.Interface:
          sourceEmitterOutput.Write("interface ");
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

    public virtual bool PrintKeywordSealed() {
      PrintToken(CSharpToken.Sealed);
      return true;
    }

    public virtual bool PrintKeywordStatic() {
      PrintToken(CSharpToken.Static);
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
