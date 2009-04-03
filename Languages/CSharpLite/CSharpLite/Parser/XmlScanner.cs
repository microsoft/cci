//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.Cci.Ast;
//^ using Microsoft.Contracts;

namespace Microsoft.Cci.SpecSharp {

  public sealed class XmlScanner {
    //////////////////////////////////////////////////////////////////////
    //State that should be set appropriately when restarting the scanner.
    /////////////////////////////////////////////////////////////////////

    //TODO: make all these private. Make the internal ones readable via read-only accessors. Make them settable via a small number of restart methods.

    /// <summary>The comment delimiter (/// or /**) that initiated the documentation comment currently being scanned (as XML).</summary>
    internal Token docCommentStart;

    /// <summary>One more than the last column that contains a character making up the token. Set this to the starting position when restarting the scanner.</summary>
    public int endPos;

    /// <summary>The index of sourceChars[0] in this.sourceLocation.Document. Add this to srartPos to arrive at the true starting position of the current token.</summary>
    public int offset;

    /// <summary>One more than the last column that contains a source character.</summary>
    public int maxPos;

    /// <summary>The characters to scan for tokens.
    private char[] sourceChars;

    /// <summary>The state governs the behavior of GetNextToken when scanning XML literals. It also allows the scanner to restart inside of a token.</summary>
    internal ScannerState state = ScannerState.Code;

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Other state. Expected to be the same every time the scanner is restarted or only meaningful immediately after a token has been scanned.
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>The character value of the last scanned character literal token.</summary>
    internal char charLiteralValue;

    /// <summary>Keeps track of the document in which the current token originates. Affected by the #line directive.</summary>
    private ISourceLocation sourceLocation; //accessed by parser via CurrentSourceLocation

    /// <summary>When this is true the scanner will not recognize Spec# only keywords.</summary>
    private bool inCompatibilityMode;

    /// <summary>
    /// Used to build the unescaped contents of an identifier when the identifier contains escape sequences. An instance variable because multiple methods are involved.
    /// </summary>
    private readonly StringBuilder identifier = new StringBuilder(128); //TODO: rename to identifierBuilder

    /// <summary>Records the extent of the identifier source that has already been appended to the identifier builder.</summary>
    private int idLastPosOnBuilder;

    /// <summary>True if the scanner should not return tokens corresponding to comments.</summary>
    private bool ignoreComments = true;

    /// <summary>True if the last XML element content string scanned consists entirely of whitespace. Governs the behavior of GetStringLiteral.</summary>
    private bool isWhitespace;

    /// <summary>Keeps track of the end position of the last malformed token. Used to avoid repeating lexical error messages when the parser backtracks.</summary>
    private int lastReportedErrorPos;

    /// <summary>True if scanning the last token has changed the state with which the scanner should be restarted when rescanning subsequent tokens.</summary>
    public bool RestartStateHasChanged;

    /// <summary>The position of the first character forming part of the last scanned token.</summary>
    public int startPos;

    /// <summary>True when the scanner is in single line mode and has reached a line break before completing scanning of the current token.</summary>
    internal bool stillInsideToken;

    /// <summary>True if the last token scanned was separated from the preceding token by whitespace that includes a line break.</summary>
    internal bool TokenIsFirstAfterLineBreak;

    /// <summary>The contents of the last string literal scanned, with escape sequences already replaced with their corresponding characters.</summary>
    private string/*?*/ unescapedString;

    /// <summary>A list to which any scanner errors should be appended if it is not null.</summary>
    private readonly List<IErrorMessage>/*?*/ scannerErrors;

    private static readonly Keyword[] Keywords = Keyword.InitKeywords();
    // ^ invariant Keywords.Length == 26;

    private static readonly Keyword ExtendedKeywords = Keyword.InitExtendedKeywords();

    internal XmlScanner(SpecSharpCompilerOptions options, List<IErrorMessage>/*?*/ scannerErrors) {
      this.scannerErrors = scannerErrors;
      this.inCompatibilityMode = options.Compatibility;
      this.sourceChars = new char[1];
      this.sourceLocation = Dummy.SourceLocation;
    }
    public void SetSourceText(ISourceLocation sourceLocation) {
      this.sourceLocation = sourceLocation;
      char[] chars = this.sourceChars = new char[sourceLocation.Length+1];
      sourceLocation.SourceDocument.CopyTo(sourceLocation.StartIndex, chars, 0, sourceLocation.Length);
      this.endPos = this.startPos = 0;
      this.maxPos = sourceLocation.Length;
      this.offset = sourceLocation.StartIndex;
    }

    private string Substring(int start, int length)
      //^ requires this.sourceChars != null;
    {
      return new string(this.sourceChars, start, length);
    }
    private Token GetNextXmlToken() {
      int maxPos = this.maxPos;
      if (this.state == ScannerState.XML) {
        this.startPos = this.endPos;
        this.ScanXmlText();
        char ch = this.GetChar(this.endPos);
        if (this.startPos < this.endPos) {
          if (this.state == ScannerState.Code && (this.docCommentStart == Token.SingleLineDocCommentStart ||
          (this.docCommentStart == Token.MultiLineDocCommentStart && ch == '*')))
            return Token.LiteralContentString;
          if (ch == '<') this.state = ScannerState.Tag;
          else this.state = ScannerState.Text;
          Debug.Assert(this.state == ScannerState.Text && this.endPos >= maxPos
            || this.state == ScannerState.Tag || this.state == ScannerState.Code);
          return Token.LiteralContentString;
        }
      }
    nextToken:
      char c = this.SkipBlanks();
      this.startPos = this.endPos - 1;
      switch (c) {
        case (char)0:
          this.startPos = this.endPos;
          this.TokenIsFirstAfterLineBreak = true;
          return Token.EndOfFile;
        case '\r':
          if (this.GetChar(this.endPos) == '\n') this.endPos++;
          goto nextToken;
        case '\n':
        case (char)0x2028:
        case (char)0x2029:
          goto nextToken;
        case '>':
          this.RestartStateHasChanged = true;
          return Token.EndOfTag;
        case '=':
          return Token.Assign;
        case ':':
          return Token.Colon;
        case '"':
        case '\'':
          state = (c == '"') ? ScannerState.XmlAttr1 : ScannerState.XmlAttr2;
          this.ScanXmlString(c);
          if (this.stillInsideToken)
            this.stillInsideToken = false;
          else
            state = ScannerState.Tag;
          return Token.StringLiteral;
        case '/':
          c = this.GetChar(this.endPos);
          if (c == '>') {
            this.endPos++;
            this.RestartStateHasChanged = true;
            this.state = ScannerState.Text;
            return Token.EndOfSimpleTag;
          }
          return Token.Divide;
        case '<':
          c = this.GetChar(this.endPos);
          if (c == '/') {
            this.RestartStateHasChanged = true;
            this.endPos++;
            return Token.StartOfClosingTag;
          } else if (c == '?') {
            this.endPos++;
            this.ScanXmlProcessingInstructionsTag();
            return Token.ProcessingInstructions;
          } else if (c == '!') {
            c = this.GetChar(++this.endPos);
            if (c == '-') {
              if (this.GetChar(++this.endPos) == '-') {
                this.endPos++;
                this.ScanXmlComment();
                return Token.LiteralComment;
              }
              this.endPos--;
            } else if (c == '[') {
              if (this.GetChar(++this.endPos) == 'C' &&
                this.GetChar(++this.endPos) == 'D' &&
                this.GetChar(++this.endPos) == 'A' &&
                this.GetChar(++this.endPos) == 'T' &&
                this.GetChar(++this.endPos) == 'A' &&
                this.GetChar(++this.endPos) == '[') {
                this.endPos++;
                this.ScanXmlCharacterData();
                return Token.CharacterData;
              }
            }
            this.endPos--;
          }
          this.RestartStateHasChanged = true;
          return Token.StartOfTag;
        default:
          if (this.IsIdentifierStartChar(c)) {
            this.ScanIdentifier();
            return Token.Identifier;
          } else if (Scanner.IsDigit(c))
            return this.ScanNumber(c);
          return Token.IllegalCharacter;
      }
    }
    private char GetChar(int index) {
      return this.sourceChars[index];
    }
    internal string GetIdentifierString() {
      if (this.identifier.Length > 0) return this.identifier.ToString();
      int start = this.startPos;
      if (this.GetChar(start) == '@') start++;
      int end = this.endPos;
      if (end > this.maxPos) end = this.maxPos;
      return this.Substring(start, end - start);
    }
    internal string/*?*/ GetString() {
      return this.unescapedString;
    }
    //TODO: internal Literal GetStringLiteral() {
    //TODO:   if (this.isWhitespace)
    //TODO:     return new WhitespaceLiteral(this.unescapedString, SystemTypes.String, this.CurrentSourceContext);
    //TODO:   else
    //TODO:     return new Literal(this.unescapedString, SystemTypes.String, this.CurrentSourceContext);
    //TODO: }
    internal string GetTokenSource() {
      int endPos = this.endPos;
      if (endPos > this.maxPos) endPos = this.maxPos;
      if (this.startPos == endPos && endPos < this.maxPos && this.state == ScannerState.XML) {
        return this.Substring(this.startPos, 1);
      }
      return this.Substring(this.startPos, endPos - this.startPos);
    }
    private void ScanCharacter() {
      this.ScanString('\'');
      int n;
      if (this.unescapedString == null || (n = this.unescapedString.Length) == 0) {
        if (this.GetChar(this.endPos) == '\'') {
          this.charLiteralValue = '\'';
          this.endPos++;
          this.HandleError(Error.UnescapedSingleQuote);
        } else {
          this.charLiteralValue = (char)0;
          this.HandleError(Error.EmptyCharConst);
        }
        return;
      } else {
        this.charLiteralValue = this.unescapedString[0];
        if (n == 1) return;
        this.HandleError(Error.TooManyCharsInConst);
      }
    }
    private void ScanEscapedChar(StringBuilder sb) {
      char ch = this.GetChar(this.endPos);
      if (ch != 'U') {
        sb.Append(this.ScanEscapedChar());
        return;
      }
      //Scan 32-bit Unicode character. 
      uint escVal = 0;
      this.endPos++;
      for (int i = 0; i < 8; i++) {
        ch = this.GetChar(this.endPos++);
        escVal <<= 4;
        if (Scanner.IsHexDigit(ch))
          escVal |= (uint)Scanner.GetHexValue(ch);
        else {
          this.HandleError(Error.IllegalEscape);
          this.endPos--;
          escVal >>= 4;
          break;
        }
      }
      if (escVal < 0x10000)
        sb.Append((char)escVal);
      else if (escVal <= 0x10FFFF) {
        //Append as surrogate pair of 16-bit characters.
        char ch1 = (char)((escVal - 0x10000) / 0x400 + 0xD800);
        char ch2 = (char)((escVal - 0x10000) % 0x400 + 0xDC00);
        sb.Append(ch1);
        sb.Append(ch2);
      } else {
        sb.Append((char)escVal);
        this.HandleError(Error.IllegalEscape);
      }
    }
    private char ScanEscapedChar() {
      int maxPos = this.maxPos;
      int escVal = 0;
      bool requireFourDigits = false;
      int savedStartPos = this.startPos;
      int errorStartPos = this.endPos - 1;
      char ch = this.GetChar(this.endPos++);
      switch (ch) {
        default:
          this.startPos = errorStartPos;
          if (this.endPos > maxPos) this.endPos = maxPos;
          this.HandleError(Error.IllegalEscape);
          this.startPos = savedStartPos;
          if (ch == 'X') goto case 'x';
          return (char)0;
        // Single char escape sequences \b etc
        case 'a': return (char)7;
        case 'b': return (char)8;
        case 't': return (char)9;
        case 'n': return (char)10;
        case 'v': return (char)11;
        case 'f': return (char)12;
        case 'r': return (char)13;
        case '"': return '"';
        case '\'': return '\'';
        case '\\': return '\\';
        case '0':
          if (this.endPos >= maxPos) goto default;
          return (char)0;
        // unicode escape sequence \uHHHH
        case 'u':
          requireFourDigits = true;
          goto case 'x';
        // hexadecimal escape sequence \xH or \xHH or \xHHH or \xHHHH
        case 'x':
          for (int i = 0; i < 4; i++) {
            ch = this.GetChar(this.endPos++);
            escVal <<= 4;
            if (Scanner.IsHexDigit(ch))
              escVal |= Scanner.GetHexValue(ch);
            else {
              if (i == 0 || requireFourDigits) {
                this.startPos = errorStartPos;
                this.HandleError(Error.IllegalEscape);
                this.startPos = savedStartPos;
              }
              this.endPos--;
              return (char)(escVal >> 4);
            }
          }
          return (char)escVal;
      }
    }
    private void ScanIdentifier() {
      int endPos = this.endPos;
      for (; ; ) {
        char c = this.GetChar(endPos);
        if ('a' <= c && c <= 'z' || 'A' <= c && c <= 'Z' || '0' <= c && c <= '9' || c == '_' || c == '$') {
          endPos++;
          continue;
        }
        this.endPos = endPos;
        if (c == '\\') {
          if (!this.IsIdentifierPartChar(c)) {
            break;
          }
        } else if (c < 128 || !this.IsIdentifierPartChar(c)) {
          break;
        }
        endPos = ++this.endPos;
      }
      this.endPos = endPos;
      if (this.idLastPosOnBuilder > 0) {
        this.identifier.Append(this.Substring(this.idLastPosOnBuilder, endPos - this.idLastPosOnBuilder));
        this.idLastPosOnBuilder = 0;
        if (this.identifier.Length == 0)
          this.HandleError(Error.UnexpectedToken);
      }
    }
    private Token ScanNumber(char leadChar) {
      Token token = leadChar == '.' ? Token.RealLiteral : Token.IntegerLiteral;
      char c;
      if (leadChar == '0') {
        c = this.GetChar(this.endPos);
        if (c == 'x' || c == 'X') {
          if (!Scanner.IsHexDigit(this.GetChar(this.endPos + 1)))
            return token; //return the 0 as a separate token
          token = Token.HexLiteral;
          while (Scanner.IsHexDigit(this.GetChar(++this.endPos))) ;
          return token;
        }
      }
      bool alreadyFoundPoint = leadChar == '.';
      bool alreadyFoundExponent = false;
      for (; ; ) {
        c = this.GetChar(this.endPos);
        if (!Scanner.IsDigit(c)) {
          if (c == '.') {
            if (alreadyFoundPoint) break;
            alreadyFoundPoint = true;
            token = Token.RealLiteral;
          } else if (c == 'e' || c == 'E') {
            if (alreadyFoundExponent) break;
            alreadyFoundExponent = true;
            alreadyFoundPoint = true;
            token = Token.RealLiteral;
          } else if (c == '+' || c == '-') {
            char e = this.GetChar(this.endPos - 1);
            if (e != 'e' && e != 'E') break;
          } else
            break;
        }
        this.endPos++;
      }
      c = this.GetChar(this.endPos - 1);
      if (c == '.') {
        this.endPos--;
        c = this.GetChar(this.endPos - 1);
        return Token.IntegerLiteral;
      }
      if (c == '+' || c == '-') {
        this.endPos--;
        c = this.GetChar(this.endPos - 1);
      }
      if (c == 'e' || c == 'E')
        this.endPos--;
      return token;
    }
    internal TypeCode ScanNumberSuffix() {
      this.startPos = this.endPos;
      char ch = this.GetChar(this.endPos++);
      if (ch == 'u' || ch == 'U') {
        char ch2 = this.GetChar(this.endPos++);
        if (ch2 == 'l' || ch2 == 'L') return TypeCode.UInt64;
        this.endPos--;
        return TypeCode.UInt32;
      } else if (ch == 'l' || ch == 'L') {
        if (ch == 'l') this.HandleError(Error.LowercaseEllSuffix);
        char ch2 = this.GetChar(this.endPos++);
        if (ch2 == 'u' || ch2 == 'U') return TypeCode.UInt64;
        this.endPos--;
        return TypeCode.Int64;
      } else if (ch == 'f' || ch == 'F')
        return TypeCode.Single;
      else if (ch == 'd' || ch == 'D')
        return TypeCode.Double;
      else if (ch == 'm' || ch == 'M')
        return TypeCode.Decimal;
      this.endPos--;
      return TypeCode.Empty;
    }

    private void ScanString(char closingQuote) {
      char ch;
      int start = this.endPos;
      int maxPos = this.maxPos;
      this.unescapedString = null;
      StringBuilder/*?*/ unescapedSB = null;
      this.isWhitespace = false;
      do {
        ch = this.GetChar(this.endPos++);
        if (ch == '\\') {
          // Got an escape of some sort. Have to use the StringBuilder
          if (unescapedSB == null) unescapedSB = new StringBuilder(128);
          // start points to the first position that has not been written to the StringBuilder.
          // The first time we get in here that position is the beginning of the string, after that
          // it is the character immediately following the escape sequence
          int len = this.endPos - start - 1;
          if (len > 0) // append all the non escaped chars to the string builder
            unescapedSB.Append(this.sourceChars, start, len);
          //int savedEndPos = this.endPos - 1;
          this.ScanEscapedChar(unescapedSB); //might be a 32-bit unicode character
          //          if (closingQuote == (char)0 && unescapedSB.Length > 0 && unescapedSB[unescapedSB.Length-1] == (char)0){
          //            unescapedSB.Length -= 1;
          //            this.endPos = savedEndPos;
          //            start = this.endPos;
          //            break;
          //          }
          start = this.endPos;
        } else {
          // This is the common non escaped case
          if (this.IsLineTerminator(ch, 0) || (ch == 0 && this.endPos >= maxPos)) {
            this.FindGoodRecoveryPoint(closingQuote);
            break;
          }
        }
      } while (ch != closingQuote);
      // update this.unescapedString using the StringBuilder
      if (unescapedSB != null && closingQuote != (char)0) {
        int len = this.endPos - start - 1;
        if (len > 0) {
          // append all the non escape chars to the string builder
          unescapedSB.Append(this.sourceChars, start, len);
        }
        this.unescapedString = unescapedSB.ToString();
      } else {
        if (closingQuote == (char)0)
          this.unescapedString = this.Substring(this.startPos, this.endPos - this.startPos);
        else if (closingQuote == '\'' && this.startPos < maxPos - 1 && (this.startPos == this.endPos - 1 || this.GetChar(this.endPos - 1) != '\''))
          this.unescapedString = this.Substring(this.startPos + 1, 1); //suppress further errors
        else if (this.endPos <= this.startPos + 2)
          this.unescapedString = "";
        else
          this.unescapedString = this.Substring(this.startPos + 1, this.endPos - this.startPos - 2);
      }
    }
    private void FindGoodRecoveryPoint(char closingQuote) {
      int maxPos = this.maxPos;
      if (closingQuote == (char)0) {
        //Scan backwards to last char before new line or EOF
        if (this.endPos >= maxPos) {
          this.endPos = maxPos; return;
        }
        char ch = this.GetChar(this.endPos - 1);
        while (Scanner.IsEndOfLine(ch)) {
          this.endPos--;
          ch = this.GetChar(this.endPos - 1);
        }
        return;
      }
      int endPos = this.endPos;
      int i;
      if (endPos < maxPos) {
        //scan forward in next line looking for suitable matching quote
        for (i = endPos; i < maxPos; i++) {
          char ch = this.GetChar(i);
          if (ch == closingQuote) {
            //Give an error, but go on as if new line is allowed
            this.endPos--;
            if (this.GetChar(this.endPos - 1) == (char)0x0d) this.endPos--;
            this.HandleError(Error.NewlineInConst);
            this.endPos = i + 1;
            return;
          }
          switch (ch) {
            case ';':
            case '}':
            case ')':
            case ']':
            case '(':
            case '[':
            case '+':
            case '-':
            case '*':
            case '/':
            case '%':
            case '!':
            case '=':
            case '<':
            case '>':
            case '|':
            case '&':
            case '^':
            case '~':
            case '@':
            case ':':
            case '?':
            case ',':
            case '"':
            case '\'':
              i = maxPos; break;
          }
        }
      } else
        this.endPos = endPos = maxPos;
      int lastSemicolon = endPos;
      int lastNonBlank = this.startPos;
      for (i = this.startPos; i < endPos; i++) {
        char ch = this.GetChar(i);
        if (this.ignoreComments) {
          if (ch == ';') { lastSemicolon = i; lastNonBlank = i; }
          if (ch == '/' && i < endPos - 1) {
            char ch2 = this.GetChar(++i);
            if (ch2 == '/' || ch2 == '*') {
              i -= 2; break;
            }
          }
        }
        if (Scanner.IsEndOfLine(ch)) break;
        if (!Scanner.IsBlankSpace(ch)) lastNonBlank = i;
      }
      if (lastSemicolon == lastNonBlank)
        this.endPos = lastSemicolon;
      else
        this.endPos = i;
      int savedStartPos = this.startPos;
      this.startPos = this.endPos;
      this.endPos++;
      if (closingQuote == '"')
        this.HandleError(Error.ExpectedDoubleQuote);
      else
        this.HandleError(Error.ExpectedSingleQuote);
      this.startPos = savedStartPos;
      if (this.endPos > this.startPos + 1) this.endPos--;
    }
    internal void ScanVerbatimString() {
      char ch;
      int start = this.endPos;
      int maxPos = this.maxPos;
      this.unescapedString = null;
      StringBuilder/*?*/ unescapedSB = null;
      for (; ; ) {
        ch = this.GetChar(this.endPos++);
        if (ch == '"') {
          ch = this.GetChar(this.endPos);
          if (ch != '"') break; //Reached the end of the string
          this.endPos++;
          if (unescapedSB == null) unescapedSB = new StringBuilder(128);
          // start points to the first position that has not been written to the StringBuilder.
          // The first time we get in here that position is the beginning of the string, after that
          // it is the character immediately following the "" pair
          int len = this.endPos - start;
          if (len > 0) // append all the non escaped chars to the string builder
            unescapedSB.Append(this.sourceChars, start, len);
          start = this.endPos;
        } else if (this.IsLineTerminator(ch, 1)) {
          ch = this.GetChar(++this.endPos);
        } else if (ch == (char)0 && this.endPos >= maxPos) {
          //Reached EOF
          this.stillInsideToken = true;
          this.endPos = maxPos;
          this.HandleError(Error.NewlineInConst);
          break;
        }
      }
      // update this.unescapedString using the StringBuilder
      if (unescapedSB != null) {
        int len = this.endPos - start - 1;
        if (len > 0) {
          // append all the non escape chars to the string builder
          unescapedSB.Append(this.sourceChars, start, len);
        }
        this.unescapedString = unescapedSB.ToString();
      } else {
        if (this.endPos <= this.startPos + 3)
          this.unescapedString = "";
        else
          this.unescapedString = this.Substring(this.startPos + 2, this.endPos - this.startPos - 3);
      }
    }
    internal void ScanXmlString(char closingQuote) {
      char ch;
      int start = this.endPos;
      int maxPos = this.maxPos;
      this.unescapedString = null;
      StringBuilder/*?*/ unescapedSB = null;
      do {
        ch = this.GetChar(this.endPos++);
        if (ch == '&') {
          // Got an escape of some sort. Have to use the StringBuilder
          if (unescapedSB == null) unescapedSB = new StringBuilder(128);
          // start points to the first position that has not been written to the StringBuilder.
          // The first time we get in here that position is the beginning of the string, after that
          // it is the character immediately following the escape sequence
          int len = this.endPos - start - 1;
          if (len > 0) // append all the non escaped chars to the string builder
            unescapedSB.Append(this.sourceChars, start, len);
          unescapedSB.Append(this.ScanXmlEscapedChar());
          start = this.endPos;
        } else if (this.IsLineTerminator(ch, 1)) {
          ch = this.GetChar(++this.endPos);
        } else if (ch == 0 && this.endPos >= maxPos) {
          this.stillInsideToken = true;
          this.endPos--;
          this.HandleError(Error.NewlineInConst);
          break;
        }
      } while (ch != closingQuote);
      // update this.unescapedString using the StringBuilder
      if (unescapedSB != null) {
        int len = this.endPos - start - 1;
        if (len > 0) {
          // append all the non escape chars to the string builder
          unescapedSB.Append(this.sourceChars, start, len);
        }
        this.unescapedString = unescapedSB.ToString();
      } else {
        if (this.endPos <= this.startPos + 2)
          this.unescapedString = "";
        else
          this.unescapedString = this.Substring(this.startPos + 1, this.endPos - this.startPos - 2);
      }
    }
    internal void ScanXmlCharacterData() {
      int start = this.endPos;
      int maxPos = this.maxPos;
      for (; ; ) {
        char c = this.GetChar(this.endPos);
        while (c == ']') {
          c = this.GetChar(++this.endPos);
          if (c == ']') {
            c = this.GetChar(++this.endPos);
            if (c == '>') {
              this.endPos++;
              this.unescapedString = this.Substring(start, this.endPos - start - 3);
              return;
            } else if (c == (char)0 && this.endPos >= maxPos)
              return;
          } else if (c == (char)0 && this.endPos >= maxPos)
            return;
          else if (this.IsLineTerminator(c, 1)) {
            c = this.GetChar(++this.endPos);
          }
        }
        if (c == (char)0 && this.endPos >= maxPos) return;
        ++this.endPos;
      }
    }
    internal void ScanXmlComment() {
      int start = this.endPos;
      int maxPos = this.maxPos;
      for (; ; ) {
        char c = this.GetChar(this.endPos);
        while (c == '-') {
          c = this.GetChar(++this.endPos);
          if (c == '-') {
            c = this.GetChar(++this.endPos);
            if (c == '>') {
              this.endPos++;
              this.unescapedString = this.Substring(start, this.endPos - start - 3);
              return;
            } else if (c == (char)0 && this.endPos >= maxPos)
              return;
          } else if (c == (char)0 && this.endPos >= maxPos)
            return;
          else if (this.IsLineTerminator(c, 1)) {
            c = this.GetChar(++this.endPos);
          }
        }
        if (c == (char)0 && this.endPos >= maxPos) return;
        ++this.endPos;
      }
    }
    private char ScanXmlEscapedChar()
      // ^ requires this.GetChar(this.endPos - 1, maxPos) == '&';
    {
      char ch = this.GetChar(this.endPos);
      if (ch == '#') {
        return ExpandCharEntity();
      } else {
        int start = endPos;
        // must be built in named entity, amp, lt, gt, quot or apos.
        for (int i = 4; ch != 0 && ch != ';' && --i >= 0; ch = this.GetChar(++this.endPos)) ;
        if (ch == ';') {
          string name = this.Substring(start, this.endPos - start);
          switch (name) {
            case "amp": ch = '&'; break;
            case "lt": ch = '<'; break;
            case "gt": ch = '>'; break;
            case "quot": ch = '"'; break;
            case "apos": ch = '\''; break;
            default:
              int savedStartPos = this.startPos;
              this.startPos = start - 1;
              this.endPos++;
              this.HandleError(Error.UnknownEntity, this.Substring(start - 1, this.endPos - start + 1));
              this.endPos = start;
              this.startPos = savedStartPos;
              return '&';
          }
          this.endPos++; // consume ';'
        } else {
          int savedStartPos = this.startPos;
          this.startPos = start - 1;
          this.endPos = start;
          this.HandleError(Error.IllegalEscape);
          this.startPos = savedStartPos;
          return '&';
        }
        return ch;
      }
    }
    public char ExpandCharEntity() {
      int start = this.endPos;
      char ch = this.GetChar(++this.endPos);
      int v = 0;
      if (ch == 'x') {
        ch = this.GetChar(++this.endPos);
        for (; ch != 0 && ch != ';'; ch = this.GetChar(++this.endPos)) {
          int p = 0;
          if (ch >= '0' && ch <= '9') {
            p = (int)(ch - '0');
          } else if (ch >= 'a' && ch <= 'f') {
            p = (int)(ch - 'a') + 10;
          } else if (ch >= 'A' && ch <= 'F') {
            p = (int)(ch - 'A') + 10;
          } else {
            this.HandleError(Error.BadHexDigit, this.Substring(this.endPos, 1));
            break; // not a hex digit
          }
          if (v > ((Char.MaxValue - p) / 16)) {
            this.HandleError(Error.EntityOverflow, this.Substring(start, this.endPos - start));
            break; // overflow
          }
          v = (v * 16) + p;
        }
      } else {
        for (; ch != 0 && ch != ';'; ch = this.GetChar(++this.endPos)) {
          if (ch >= '0' && ch <= '9') {
            int p = (int)(ch - '0');
            if (v > ((Char.MaxValue - p) / 10)) {
              this.HandleError(Error.EntityOverflow, this.Substring(start, this.endPos - start));
              break; // overflow
            }
            v = (v * 10) + p;
          } else {
            this.HandleError(Error.BadDecimalDigit, this.Substring(this.endPos, 1));
            break; // char out of range
          }
        }
      }
      if (ch == 0) {
        this.HandleError(Error.IllegalEscape);
      } else {
        this.endPos++; // consume ';'
      }
      return Convert.ToChar(v);
    }

    internal void ScanXmlProcessingInstructionsTag() {
      int maxPos = this.maxPos;
      for (; ; ) {
        char c = this.GetChar(this.endPos);
        while (c == '?') {
          c = this.GetChar(++this.endPos);
          if (c == '>') {
            this.endPos++;
            return;
          } else if (c == (char)0 && this.endPos >= maxPos)
            return;
          else if (this.IsLineTerminator(c, 1)) {
            c = this.GetChar(++this.endPos);
          }
        }
        if (c == (char)0 && this.endPos >= maxPos) return;
        ++this.endPos;
      }
    }
    internal void ScanXmlText() {
      char c;
      int start = this.endPos;
      int maxPos = this.maxPos;
      this.unescapedString = null;
      this.isWhitespace = true;
      StringBuilder/*?*/ unescapedSB = null;
      for (; ; ) {
        c = this.GetChar(this.endPos++);
        if (c == '&') {
          isWhitespace = false;
          // Got an escape of some sort. Have to use the StringBuilder
          if (unescapedSB == null) unescapedSB = new StringBuilder(128);
          // start points to the first position that has not been written to the StringBuilder.
          // The first time we get in here that position is the beginning of the string, after that
          // it is the character immediately following the escape sequence
          int len = this.endPos - start - 1;
          if (len > 0) // append all the non escaped chars to the string builder
            unescapedSB.Append(this.sourceChars, start, len);
          unescapedSB.Append(this.ScanXmlEscapedChar());
          start = this.endPos;
        } else {
          if (c == (char)0 && this.endPos >= maxPos) break;
          if (this.IsLineTerminator(c, 0)) {
            if (this.docCommentStart != Token.None) {
              if (unescapedSB == null) unescapedSB = new StringBuilder(128);
              int len = this.endPos - start;
              if (len > 0) // append all the non escaped chars to the string builder
                unescapedSB.Append(this.sourceChars, start, len);
              start = this.endPos;
              c = this.SkipBlanks();
              if (c == '/' && this.GetChar(this.endPos) == '/' && this.GetChar(this.endPos + 1) == '/') {
                if (this.docCommentStart == Token.MultiLineDocCommentStart) {
                  bool lastCharWasSlash = false;
                  for (int j = unescapedSB.Length - 1; j > 0; j--) {
                    char ch = unescapedSB[j];
                    if (ch == '/')
                      lastCharWasSlash = true;
                    else if (ch == '*' && lastCharWasSlash) {
                      unescapedSB.Length = j;
                      break;
                    }
                  }
                  this.docCommentStart = Token.SingleLineDocCommentStart;
                  this.RestartStateHasChanged = true;
                }
                this.endPos += 2;
              } else if (this.docCommentStart == Token.SingleLineDocCommentStart) {
                if (c == '/' && this.GetChar(this.endPos) == '*' && this.GetChar(this.endPos + 1) == '*') {
                  this.docCommentStart = Token.MultiLineDocCommentStart;
                  this.RestartStateHasChanged = true;
                  this.endPos += 2;
                } else {
                  start = --this.endPos;
                  this.state = ScannerState.Code;
                  this.RestartStateHasChanged = true;
                  break;
                }
              } else {
                len = this.endPos - start - 1;
                if (len > 0) // append all the non escaped chars to the string builder
                  unescapedSB.Append(this.sourceChars, start, len);
                unescapedSB.Append(c);
              }
              start = this.endPos;
            }
          }
          if (c == '<') break;
          if (!this.ignoreComments && c == '*' && this.docCommentStart == Token.MultiLineDocCommentStart && this.GetChar(this.endPos) == '/') {
            start = --this.endPos;
            this.state = ScannerState.Code;
            this.RestartStateHasChanged = true;
            break;
          }
          if (isWhitespace && !XmlScanner.IsXmlWhitespace(c)) {
            isWhitespace = false;
          }
        }
      }
      // update this.unescapedString using the StringBuilder
      if (unescapedSB != null) {
        int len = this.endPos - start - 1;
        if (len > 0) {
          // append all the non escaped chars to the string builder
          unescapedSB.Append(this.sourceChars, start, len);
        }
        this.unescapedString = unescapedSB.ToString();
      } else {
        int len = this.endPos - start - 1;
        if (len <= 0)
          this.unescapedString = "";
        else
          this.unescapedString = this.Substring(this.startPos, len);
      }
      if (c == '<' || c == (char)0) this.endPos--;
    }
    private void SkipSingleLineComment() {
      int maxPos = this.maxPos;
      while (!this.IsEndLineOrEOF(this.GetChar(this.endPos++), 0)) ;
      if (this.endPos > maxPos) this.endPos = maxPos;
    }
    internal void SkipMultiLineComment() {
      int maxPos = this.maxPos;
      for (; ; ) {
        char c = this.GetChar(this.endPos);
        while (c == '*') {
          c = this.GetChar(++this.endPos);
          if (c == '/') {
            this.endPos++;
            return;
          } else if (c == (char)0 && this.endPos >= maxPos) {
            this.stillInsideToken = true;
            return;
          } else if (this.IsLineTerminator(c, 1)) {
            c = this.GetChar(++this.endPos);
          }
        }
        if (c == (char)0 && this.endPos >= maxPos) {
          this.stillInsideToken = true;
          this.endPos = maxPos;
          return;
        }
        ++this.endPos;
      }
    }
    private char SkipBlanks() {
      int maxPos = this.maxPos;
      char c = this.GetChar(this.endPos);
      while (Scanner.IsBlankSpace(c) ||
        (c == (char)0 && this.endPos < maxPos)) { // silently skip over nulls
        c = this.GetChar(++this.endPos);
      }
      if (c != '\0') this.endPos++;
      return c;
    }
    public static bool IsBlankSpace(char c) {
      switch (c) {
        case (char)0x09:
        case (char)0x0B:
        case (char)0x0C:
        case (char)0x1A:
        case (char)0x20:
          return true;
        default:
          if (c >= 128)
            return Char.GetUnicodeCategory(c) == UnicodeCategory.SpaceSeparator;
          else
            return false;
      }
    }
    public static bool IsEndOfLine(char c) {
      switch (c) {
        case (char)0x0D:
        case (char)0x0A:
        case (char)0x85:
        case (char)0x2028:
        case (char)0x2029:
          return true;
        default:
          return false;
      }
    }
    private bool IsLineTerminator(char c, int increment) {
      switch (c) {
        case (char)0x0D:
          // treat 0x0D0x0A as a single character
          if (this.GetChar(this.endPos + increment) == 0x0A)
            this.endPos++;
          return true;
        case (char)0x0A:
        case (char)0x85:
        case (char)0x2028:
        case (char)0x2029:
          return true;
        default:
          return false;
      }
    }
    private static bool IsXmlWhitespace(char c) {
      switch (c) {
        case (char)0x0D:
          // treat 0x0D0x0A as a single character
          return true;
        case (char)0x0A:
          return true;
        case (char)0x2028: // bugbug: should these be here?
          return true;
        case (char)0x2029:
          return true;
        case (char)0x20:
          return true;
        case (char)0x9:
          return true;
        default:
          return false;
      }
    }
    private bool IsEndLineOrEOF(char c, int increment) {
      return this.IsLineTerminator(c, increment) || c == (char)0 && this.endPos >= this.maxPos;
    }
    internal bool IsIdentifierPartChar(char c) {
      if (this.IsIdentifierStartCharHelper(c, true))
        return true;
      if ('0' <= c && c <= '9')
        return true;
      if (this.state != ScannerState.Code && (c == '-' || c == '.'))
        return true;
      if (c == '\\') {
        this.endPos++;
        this.ScanEscapedChar();
        this.endPos--;
        return true; //It is not actually true, or IsIdentifierStartCharHelper would have caught it, but this makes for better error recovery
      }
      return false;
    }
    internal bool IsIdentifierStartChar(char c) {
      return this.IsIdentifierStartCharHelper(c, false);
    }
    private bool IsIdentifierStartCharHelper(char c, bool expandedUnicode) {
      bool isEscapeChar = false;
      int escapeLength = 0;
      UnicodeCategory ccat = 0;
      if (c == '\\') {
        isEscapeChar = true;
        char cc = this.GetChar(this.endPos + 1);
        switch (cc) {
          case '-':
            c = '-';
            goto isIdentifierChar;
          case 'u':
            escapeLength = 4;
            break;
          case 'U':
            escapeLength = 8;
            break;
          default:
            return false;
        }
        int escVal = 0;
        for (int i = 0; i < escapeLength; i++) {
          char ch = this.GetChar(this.endPos + 2 + i);
          escVal <<= 4;
          if (Scanner.IsHexDigit(ch))
            escVal |= Scanner.GetHexValue(ch);
          else {
            escVal >>= 4;
            break;
          }
        }
        if (escVal > 0xFFFF) return false; //REVIEW: can a 32-bit Unicode char ever be legal? If so, how does one categorize it?
        c = (char)escVal;
      }
      if ('a' <= c && c <= 'z' || 'A' <= c && c <= 'Z' || c == '_' || c == '$')
        goto isIdentifierChar;
      if (c < 128)
        return false;
      ccat = Char.GetUnicodeCategory(c);
      switch (ccat) {
        case UnicodeCategory.UppercaseLetter:
        case UnicodeCategory.LowercaseLetter:
        case UnicodeCategory.TitlecaseLetter:
        case UnicodeCategory.ModifierLetter:
        case UnicodeCategory.OtherLetter:
        case UnicodeCategory.LetterNumber:
          goto isIdentifierChar;
        case UnicodeCategory.NonSpacingMark:
        case UnicodeCategory.SpacingCombiningMark:
        case UnicodeCategory.DecimalDigitNumber:
        case UnicodeCategory.ConnectorPunctuation:
          if (expandedUnicode) goto isIdentifierChar;
          return false;
        case UnicodeCategory.Format:
          if (expandedUnicode) {
            if (!isEscapeChar) {
              isEscapeChar = true;
              escapeLength = -1;
            }
            goto isIdentifierChar;
          }
          return false;
        default:
          return false;
      }
    isIdentifierChar:
      if (isEscapeChar) {
        int startPos = this.idLastPosOnBuilder;
        if (startPos == 0) startPos = this.startPos;
        if (this.endPos > startPos)
          this.identifier.Append(this.Substring(startPos, this.endPos - startPos));
        if (ccat != UnicodeCategory.Format)
          this.identifier.Append(c);
        this.endPos += escapeLength + 1;
        this.idLastPosOnBuilder = this.endPos + 1;
      }
      return true;
    }
    /// <summary>
    /// Returns true if '0' &lt;= c &amp;&amp; c &lt;= '9'.
    /// </summary>
    //^ [Pure]
    internal static bool IsDigit(char c) {
      return '0' <= c && c <= '9';
    }
    internal static bool IsHexDigit(char c) {
      return Scanner.IsDigit(c) || 'A' <= c && c <= 'F' || 'a' <= c && c <= 'f';
    }
    internal static bool IsAsciiLetter(char c) {
      return 'A' <= c && c <= 'Z' || 'a' <= c && c <= 'z';
    }
    internal static bool IsUnicodeLetter(char c) {
      return c >= 128 && Char.IsLetter(c);
    }
    private void HandleError(Error error, params string[] messageParameters) {
      if (this.endPos <= this.lastReportedErrorPos) return;
      if (this.scannerErrors == null) return;
      this.lastReportedErrorPos = this.endPos;
      ISourceLocation errorLocation;
      if (error == Error.BadHexDigit)
        errorLocation = this.GetSourceLocation(this.endPos - 1, 1);
      else
        errorLocation = this.GetSourceLocation(this.startPos, this.endPos - this.startPos);
      this.scannerErrors.Add(new SpecSharpErrorMessage(errorLocation, (long)error, error.ToString(), messageParameters));
    }
    internal static int GetHexValue(char hex) {
      int hexValue;
      if ('0' <= hex && hex <= '9')
        hexValue = hex - '0';
      else if ('a' <= hex && hex <= 'f')
        hexValue = hex - 'a' + 10;
      else
        hexValue = hex - 'A' + 10;
      return hexValue;
    }
    internal ISourceLocation CurrentSourceContext {
      get {
        return this.GetSourceLocation(this.startPos+this.offset, this.endPos - this.startPos);
      }
    }
    //^ [Pure]
    private ISourceLocation GetSourceLocation(int position, int length)
      //^ requires 0 <= position && (position < this.Length || position == 0);
      //^ requires 0 <= length;
      //^ requires length <= this.Length;
      //^ requires position+length <= this.Length;
    {
      return this.sourceLocation.SourceDocument.GetSourceLocation(
        this.sourceLocation.StartIndex + position, length);
    }
  }

  /// <summary>
  /// States of the scanner. Chiefly used to decide how to scan XML literals inside of documentation comments.
  /// </summary>
  [Flags]
  public enum ScannerState {
    /// <summary>Scanning normal code. Not inside a documentation comment.</summary>
    Code,
    /// <summary>Scanning a documentation comment. Not inside a tag or the body of an element.</summary>
    XML,
    /// <summary>Scanning a tag of an XML element.</summary>
    Tag,
    /// <summary>Scanning stopped at the end of a line before a multi-line comment was completed. Carry on scanning the comment when restarting at the next line.</summary>
    MLComment,
    /// <summary>Scanning stopped at the end of a line before a multi-line string was completed. Carry on scanning the string when restarting at the next line.</summary>
    MLString,
    /// <summary>Scanning the body of a CDATA tag.</summary>
    CData,
    /// <summary>Scanning the body of an XML PI tag.</summary>
    PI,
    /// <summary>Scanning the body of an XML element.</summary>
    Text,
    /// <summary>Scanning an XML comment inside an XML literal.</summary>
    LiteralComment,
    ///<summary>Scanning a single quoted multi-line xml attribute value.</summary>
    XmlAttr1,
    ///<summary>Scanning a double quoted multi-line xml attribute value.</summary>
    XmlAttr2,
    /// <summary>The last token was a numeric literal. Used to prevent . from triggering member selection.</summary>
    LastTokenDisablesMemberSelection,
    /// <summary>Masks out bits that can vary independently of the state in the lower order bits.</summary>
    StateMask=0xF,
    /// <summary>Inside a specification comment. Recognize Spec# keywords even if compiling in C# mode.</summary>
    ExplicitlyInSpecSharp=0x10,
    /// <summary>True if inside a multi-line specification comment. If true, do not set explicitlyInSpecSharp to false when reaching a line break.</summary>
    InSpecSharpMultilineComment=0x20
  };
}
