//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
namespace Microsoft.Cci.SmallBasic {
  using System;
  using System.Collections.Generic;
  using System.Diagnostics;
  using System.Text;

  /// <summary>
  /// The Small Basic Line Scanner
  /// </summary>
  public class Scanner {
    #region Private Members

    int currentIndex;
    int lineLength;
    string lineText;
    ISourceLocation sourceLocation; //accessed by parser via CurrentSourceLocation
    TokenInfo tokenInfo = new TokenInfo();

    #endregion // Private Members

    public Scanner() {
      this.sourceLocation = SourceDummy.SourceLocation;
      this.lineText = "";
    }

    internal Scanner(ISourceLocation sourceLocation) {
      this.sourceLocation = sourceLocation;
      this.lineText = sourceLocation.Source;
      this.lineLength = sourceLocation.Source.Length;
    }

    /// <summary>
    /// Scans the given text and returns a list of tokens
    /// </summary>
    /// <param name="lineText">
    /// The text to scan
    /// </param>
    /// <returns>
    /// A list of tokens
    /// </returns>
    public List<TokenInfo> GetTokenList(string lineText) {
      this.lineText = lineText;
      this.lineLength = this.lineText.Length;
      this.currentIndex = 0;

      List<TokenInfo> tokenInfoList = new List<TokenInfo>();
      TokenInfo tokenInfo;
      while (ScanNextToken(out tokenInfo))
        tokenInfoList.Add(tokenInfo);
      return tokenInfoList;
    }

    /// <summary>
    /// Scans and populates the token info for the next token
    /// </summary>
    /// <returns>
    /// true if a token was found, else false.
    /// </returns>
    internal bool ScanNextToken(out TokenInfo tokenInfo) {
      // Go to the next non-whitespace character
      EatSpaces();

      this.tokenInfo = tokenInfo = new TokenInfo();
      tokenInfo.Start = this.currentIndex;

      // Get the next character and analyze it
      char c = GetNextChar();
      if (c == (char)0)
        return false;

      switch (c) {
        case '+':
          tokenInfo.Token = Token.Addition;
          tokenInfo.Length = 1;
          break;

        case '-':
          tokenInfo.Token = Token.Subtraction;
          tokenInfo.Length = 1;
          break;

        case '/':
          tokenInfo.Token = Token.Division;
          tokenInfo.Length = 1;
          break;

        case '*':
          tokenInfo.Token = Token.Multiplication;
          tokenInfo.Length = 1;
          break;

        case '^':
          tokenInfo.Token = Token.Raise;
          tokenInfo.Length = 1;
          break;

        case '(':
          tokenInfo.Token = Token.LeftParens;
          tokenInfo.Length = 1;
          break;

        case ')':
          tokenInfo.Token = Token.RightParens;
          tokenInfo.Length = 1;
          break;

        case '[':
          tokenInfo.Token = Token.LeftBracket;
          tokenInfo.Length = 1;
          break;

        case ']':
          tokenInfo.Token = Token.RightBracket;
          tokenInfo.Length = 1;
          break;

        case ':':
          tokenInfo.Token = Token.Colon;
          tokenInfo.Length = 1;
          break;

        case ',':
          tokenInfo.Token = Token.Comma;
          tokenInfo.Length = 1;
          break;

        case '.':
          tokenInfo.Token = Token.Dot;
          tokenInfo.Length = 1;
          break;

        case '<': {
            c = GetNextChar();
            if (c == '=') {
              tokenInfo.Token = Token.LessThanEqualTo;
              tokenInfo.Length = 2;
            } else if (c == '>') {
              tokenInfo.Token = Token.NotEqualTo;
              tokenInfo.Length = 2;
            } else {
              this.currentIndex--;
              tokenInfo.Token = Token.LessThan;
              tokenInfo.Length = 1;
            }
            break;
          }

        case '>': {
            c = GetNextChar();
            if (c == '=') {
              tokenInfo.Token = Token.GreaterThanEqualTo;
              tokenInfo.Length = 2;
            } else {
              this.currentIndex--;
              tokenInfo.Token = Token.GreaterThan;
              tokenInfo.Length = 1;
            }
            break;
          }


        case '=':
          tokenInfo.Token = Token.Equals;
          tokenInfo.Length = 1;
          break;

        case '\'': {
            this.currentIndex--;
            tokenInfo.Token = Token.Comment;
            string comment = ReadComment();
            tokenInfo.Length = comment.Length;
            break;
          }

        case '"': {
            this.currentIndex--;
            tokenInfo.Token = Token.StringLiteral;
            string literal = ReadStringLiteral();
            tokenInfo.Length = literal.Length;
            break;
          }

        // line terminators
        case '\r':
          tokenInfo.Token = Token.EndOfLine;
          tokenInfo.Length = 1;
          if (this.GetNextChar() == '\n')
            tokenInfo.Length++;
          else
            this.currentIndex--;
          break;

        case '\n':
        case (char)0x85:
        case (char)0x2028:
        case (char)0x2029:
          tokenInfo.Token = Token.EndOfLine;
          tokenInfo.Length = 1;
          break;

        default: {
            if (char.IsLetter(c) || c == '_') {
              this.currentIndex--;
              string nextToken = ReadKeywordOrIdentifier();
              tokenInfo.Token = MatchToken(nextToken);
              tokenInfo.Length = nextToken.Length;
            } else if (char.IsDigit(c)) {
              this.currentIndex--;
              string nextToken = ReadNumericLiteral();
              tokenInfo.Token = Token.NumericLiteral;
              tokenInfo.Length = nextToken.Length;
            } else {
              tokenInfo.Token = Token.Illegal;
              tokenInfo.Length = 1;
            }
            break;
          }
      }

      tokenInfo.TokenType = GetTokenType(tokenInfo.Token);
      return true;
    }

    internal ISourceLocation CurrentSourceLocation {
      get {
        return this.GetSourceLocation(this.tokenInfo.Start, this.currentIndex - this.tokenInfo.Start);
      }
    }

    internal string GetTokenSource() {
      return this.lineText.Substring(this.tokenInfo.Start, this.tokenInfo.Length);
    }

    #region Private Helpers

    /// <summary>
    /// Gets the next character from the text line
    /// </summary>
    char GetNextChar() {
      if (this.currentIndex < this.lineLength)
        return this.lineText[this.currentIndex++];
      else {
        this.currentIndex++;
        return (char)0;
      }
    }

    //^ [Pure]
    ISourceLocation GetSourceLocation(int position, int length)
      //^^ requires 0 <= position && (position < this.Length || position == 0);
      //^^ requires 0 <= length;
      //^^ requires length <= this.Length;
      //^^ requires position+length <= this.Length;
      //^^ ensures result.SourceDocument == this;
      //^^ ensures result.StartIndex == position;
      //^^ ensures result.Length == length;
    {
      return this.sourceLocation.SourceDocument.GetSourceLocation(
        this.sourceLocation.StartIndex + position, length);
    }

    /// <summary>
    /// Reads a keyword or an identifier starting from the current index
    /// </summary>
    string ReadKeywordOrIdentifier() {
      StringBuilder sb = new StringBuilder();
      char c = GetNextChar();
      Debug.Assert(char.IsLetter(c));

      while (char.IsLetterOrDigit(c) || c == '_') {
        sb.Append(c);
        c = GetNextChar();
      }

      // Move the position back one step
      this.currentIndex--;
      return sb.ToString();
    }

    /// <summary>
    /// Reads a numeric literal starting from the current index
    /// </summary>
    string ReadNumericLiteral() {
      StringBuilder sb = new StringBuilder();
      char c = GetNextChar();
      Debug.Assert(char.IsDigit(c));

      bool decimalEncountered = false;

      while (true) {
        if (c == '.' && !decimalEncountered)
          decimalEncountered = true;
        else if (!char.IsDigit(c))
          break;

        sb.Append(c);
        c = GetNextChar();
      }

      // Move the position one step back
      this.currentIndex--;
      return sb.ToString();
    }

    /// <summary>
    /// Reads a string literal starting from the current index
    /// </summary>
    string ReadStringLiteral() {
      StringBuilder sb = new StringBuilder();
      char c = GetNextChar();
      Debug.Assert(c == '"');
      sb.Append(c);
      c = GetNextChar();

      while (c != (char)0) {
        sb.Append(c);
        if (c == '"') {
          c = GetNextChar();
          if (c != '"')
            break;
          sb.Append(c);
        }

        c = GetNextChar();
      }

      // Move the position one step back
      this.currentIndex--;
      return sb.ToString();
    }

    /// <summary>
    /// Reads a line comment
    /// </summary>
    string ReadComment() {
      StringBuilder sb = new StringBuilder();
      char c = GetNextChar();
      Debug.Assert(c == '\'');
      sb.Append(c);

      sb.Append(this.lineText.Substring(this.currentIndex));
      this.currentIndex = this.lineLength;
      return sb.ToString();
    }

    /// <summary>
    /// Advances the index to a non-space character
    /// </summary>
    void EatSpaces() {
      while (IsWhiteSpace(GetNextChar())) ;
      this.currentIndex--;
    }

    /// <summary>
    /// Gets whether or not the given character is a whitespace character
    /// </summary>
    bool IsWhiteSpace(char c) {
      switch (c) {
        case (char)0x09:
        case (char)0x0B:
        case (char)0x0C:
        case (char)0x1A:
        case (char)0x20:
          return true;
        default:
          if (c >= 128)
            return Char.GetUnicodeCategory(c) == System.Globalization.UnicodeCategory.SpaceSeparator;
          else
            return false;
      }
    }

    /// <summary>
    /// Matches the given token text with a token from the enumerations
    /// </summary>
    Token MatchToken(string tokenText) {
      switch (tokenText.ToLowerInvariant()) {
        case "and":
          return Token.And;

        case "do":
          return Token.Do;

        case "each":
          return Token.Each;

        case "else":
          return Token.Else;

        case "endif":
          return Token.EndIf;

        case "false":
          return Token.False;

        case "for":
          return Token.For;

        case "gosub":
          return Token.Gosub;

        case "goto":
          return Token.Goto;

        case "if":
          return Token.If;

        case "in":
          return Token.In;

        case "loop":
          return Token.Loop;

        case "next":
          return Token.Next;

        case "not":
          return Token.Not;

        case "or":
          return Token.Or;

        case "return":
          return Token.Return;

        case "step":
          return Token.Step;

        case "to":
          return Token.To;

        case "true":
          return Token.True;

        case "while":
          return Token.While;

        default:
          return Token.Identifier;
      }
    }

    /// <summary>
    /// Given the token, returns the token type
    /// </summary>
    TokenType GetTokenType(Token token) {
      switch (token) {
        case Token.Illegal:
          return TokenType.Illegal;

        case Token.Comment:
          return TokenType.Comment;

        case Token.StringLiteral:
          return TokenType.StringLiteral;

        case Token.NumericLiteral:
          return TokenType.NumericLiteral;

        case Token.Identifier:
          return TokenType.Identifier;

        case Token.Do:
        case Token.Each:
        case Token.Else:
        case Token.EndIf:
        case Token.False:
        case Token.For:
        case Token.Gosub:
        case Token.Goto:
        case Token.If:
        case Token.In:
        case Token.Loop:
        case Token.Next:
        case Token.Return:
        case Token.Step:
        case Token.To:
        case Token.True:
        case Token.While:
          return TokenType.Keyword;

        case Token.And:
        case Token.Equals:
        case Token.Not:
        case Token.Or:
        case Token.Dot:
        case Token.Addition:
        case Token.Subtraction:
        case Token.Division:
        case Token.Multiplication:
        case Token.LeftParens:
        case Token.Raise:
        case Token.RightParens:
        case Token.LeftBracket:
        case Token.RightBracket:
        case Token.Comma:
        case Token.LessThan:
        case Token.LessThanEqualTo:
        case Token.GreaterThan:
        case Token.GreaterThanEqualTo:
        case Token.NotEqualTo:
          return TokenType.Operator;

        default:
          return TokenType.Illegal;
      }
    }

    #endregion // Private Helpers

  }
}