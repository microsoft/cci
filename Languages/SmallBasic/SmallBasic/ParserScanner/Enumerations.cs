//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
namespace Microsoft.Cci.SmallBasic {
  public enum TokenType {
    Illegal,
    Identifier,
    StringLiteral,
    NumericLiteral,
    Comment,
    Keyword,
    Operator,
    Delimiter,
  }

  public enum Token {
    Illegal,
    EndOfFile,
    EndOfLine,
    Colon,
    Comma,
    Comment,
    Identifier,
    LeftBracket,
    LeftParens,
    NumericLiteral,
    RightBracket,
    RightParens,
    StringLiteral,

    // Keywords
    Do,
    Each,
    Else,
    EndIf,
    False,
    For,
    Gosub,
    Goto,
    If,
    In,
    Loop,
    Next,
    Return,
    Step,
    To,
    True,
    While,

    // Operators
    And,
    Addition,
    Division,
    Dot,
    Equals,
    GreaterThan,
    GreaterThanEqualTo,
    LessThan,
    LessThanEqualTo,
    Multiplication,
    Not,
    NotEqualTo,
    Or,
    Raise,
    Subtraction,

  }
}