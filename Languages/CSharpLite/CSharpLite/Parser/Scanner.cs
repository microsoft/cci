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

  public sealed class Scanner {
    /// <summary>The character value of the last scanned character literal token.</summary>
    internal char charLiteralValue;

    /// <summary>The index of the first character beyond the last scanned token.</summary>
    private int endPos; //^ invariant startPos <= endPos && endPos <= charsInBuffer;

    /// <summary>The index of sourceChars[0] in this.sourceLocation. Add this to startPos to arrive at the true starting position of the current token.</summary>
    private int offset; //^ invariant 0 <= offset && 0 <= offset+charsInBuffer && offset+charsInBuffer <= sourceLocation.Length; 

    /// <summary>The number of characters in the current document buffer being scanned. this.buffer[this.charsInBuffer] always == 0.</summary>
    private int charsInBuffer; //^ invariant 0 <= charsInBuffer && charsInBuffer < buffer.Length && buffer[charsInBuffer] == (char)0;

    /// <summary>True if the last token scanned was separated from the preceding token by whitespace that includes a line break.</summary>
    public bool TokenIsFirstAfterLineBreak { 
      get { return this.tokenIsFirstAfterLineBreak; } 
    }
    private bool tokenIsFirstAfterLineBreak;

    /// <summary>A linked list of keywords that start with "__".</summary>
    private static readonly Keyword ExtendedKeywords = Keyword.InitExtendedKeywords();

    /// <summary>
    /// Used to build the unescaped contents of an identifier when the identifier contains escape sequences. An instance variable because multiple methods are involved.
    /// </summary>
    private readonly StringBuilder identifierBuilder = new StringBuilder(128);

    /// <summary>Records the extent of the identifier source that has already been appended to the identifier builder.</summary>
    private int idLastPosOnBuilder; //^ invariant 0 <= idLastPosOnBuilder && idLastPosOnBuilder <= this.endPos;

    /// <summary>True if the scanner should not return tokens corresponding to comments.</summary>
    private bool ignoreComments = true;

    /// <summary>True if inside a multi-line specification comment.</summary>
    private bool inSpecSharpMultilineComment;

    /// <summary>An array of linked lists of keywords, to be indexed with the first character of the keyword.</summary>
    private static readonly Keyword/*?*/[] Keywords = Keyword.InitKeywords();
    // ^ invariant Keywords.Length == 26; //TODO: Boogie crashes on this invariant

    /// <summary>Keeps track of the end position of the last malformed token. Used to avoid repeating lexical error messages when the parser backtracks.</summary>
    private int lastReportedErrorPos;

    /// <summary>A list to which any scanner errors should be appended if it is not null.</summary>
    private readonly List<IErrorMessage>/*?*/ scannerErrors;

    /// <summary>The characters to scan for tokens.</summary>
    private char[] buffer; 
    //^ invariant 0 < buffer.LongLength+this.offset;
    //^ invariant buffer.LongLength+this.offset <= int.MaxValue;
    //TODO: switch to using char* and an unmanaged memory block. This will get rid of the (unnecessary) index out range check. (First verify code.)

    /// <summary>Keeps track of the source location being scanned.</summary>
    private ISourceLocation sourceLocation;

    /// <summary>The position of the first character forming part of the last scanned token.</summary>
    private int startPos; //^ invariant 0 <= startPos && startPos <= charsInBuffer;

    /// <summary>The contents of the last string literal scanned, with escape sequences already replaced with their corresponding characters.</summary>
    private string/*?*/ unescapedString;

    public Scanner(List<IErrorMessage>/*?*/ scannerErrors, ISourceLocation sourceLocation, bool ignoreComments) {
      this.scannerErrors = scannerErrors;
      this.sourceLocation = sourceLocation;
      char[] buffer = new char[16];
      this.charsInBuffer = sourceLocation.CopyTo(0, buffer, 0, buffer.Length-1);
      this.buffer = buffer;
      this.endPos = this.startPos = 0;
      this.offset = 0;
      this.ignoreComments = ignoreComments;
    }

    //^ [Pure]
    internal int CurrentDocumentPosition()
      //^ ensures 0 <= result;
    {      
      return this.offset+this.startPos;
    }

    private char GetCurrentChar()
      //^ requires 0 <= this.endPos;
      //^ requires this.endPos <= this.charsInBuffer;
      //^ ensures result != (char)0 ==> this.endPos < this.charsInBuffer;
      //^ ensures old(this.endPos)-old(this.startPos) == this.endPos-this.startPos;
    {
      char c = this.buffer[this.endPos];
      if (c == (char)0 && this.endPos == this.charsInBuffer && this.charsInBuffer > 0) {
        this.GetNextFragment();
        c = this.buffer[this.endPos];
      }
      return c;
    }

    private char GetCurrentCharAndIncrementEndPos() 
      //^ requires 0 <= this.endPos;
      //^ requires this.endPos < this.charsInBuffer;
      //^ ensures result != (char)0 ==> this.endPos <= this.charsInBuffer;
    {
      char c = this.buffer[this.endPos++];
      if (c == (char)0 && this.endPos == this.charsInBuffer) {
        this.GetNextFragment();
        c = this.buffer[this.endPos];
      }
      return c;
    }

    private char GetChar(int index)
      //^ requires 0 <= index;
      //^ requires index <= this.charsInBuffer;
    {
      return this.buffer[index];
    }

    private char PeekAheadOneCharacter()
      //^ requires this.endPos+1 <= this.charsInBuffer;
      //^ requires this.charsInBuffer > 0;
      //^ ensures result != (char)0 ==> this.endPos+1 < this.charsInBuffer;
      //^ ensures result == this.buffer[this.endPos+1];
    {
      if (this.endPos+1 == this.charsInBuffer)
        this.GetNextFragment();
      return this.buffer[this.endPos+1];
    }

    private char GetNextCharAndIncrementEndPos()
      //^ requires this.endPos < this.charsInBuffer;
      //^ ensures result != (char)0 ==> this.endPos < this.charsInBuffer;
    {
      char c =  this.buffer[++this.endPos];
      if (c == (char)0 && this.endPos == this.charsInBuffer) {
        this.GetNextFragment();
        c = this.buffer[this.endPos];
      }
      return c;
    }

    /// <summary>
    /// Gets another fragment of characters from the source document and updates this.buffer, this.endPos and this.startPos accordingly.
    /// The new fragment will start with the first character of the current token (this.startPos). If the old fragment started at the same character (in other words
    /// if the old fragment did not contain more than one complete token), the size of the buffer is doubled so that the new fragment is 
    /// bigger than the old fragment and thus scanning will not get stuck. (This assumes that all token scanning code will tread EOF as a token terminator.)
    /// </summary>
    private void GetNextFragment()
      //^ requires this.charsInBuffer > 0 || (this.charsInBuffer == 0 && this.startPos == 0 && this.endPos == 0);
      //^ ensures this.buffer[this.charsInBuffer] == 0;
      //^ ensures this.endPos == old(this.endPos) - old(this.startPos);
      //^ ensures this.startPos == 0;
    {
      this.offset += this.startPos;
      if (this.startPos == 0 && this.charsInBuffer > 0) {
        //ran out of characters in the buffer before hitting a new token. Have to increase the size of the buffer in order not to get stuck.
        long newBufferLength = this.buffer.Length*2L;
        if (newBufferLength+this.offset > int.MaxValue) 
          newBufferLength = int.MaxValue-this.offset;
        int bufLen = (int)newBufferLength;
        //^ assume 0 < bufLen; //no overlow is assured by previous if statement
        this.buffer = new char[bufLen];
      }
      this.charsInBuffer = this.sourceLocation.CopyTo(this.offset, this.buffer, 0, this.buffer.Length-1);
      this.buffer[this.charsInBuffer] = (char)0;
      this.endPos -= this.startPos;
      this.startPos = 0;
    }

    private char GetPreviousChar() 
      //^ requires this.endPos > 0;
    {
      return this.buffer[this.endPos-1];
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

    /// <summary>
    /// Returns a string that corresponds to the last token that was scanned.
    /// If the last token was an identifier that includes escape sequences then
    /// the returned string is the unescaped string (the escape sequences have been
    /// replaced by the characters they represent). If the last token was an identifier
    /// that was prefixed by the @ character, that character is omitted from the result.
    /// </summary>
    //^ [Pure]
    internal string GetIdentifierString() {
      if (this.identifierBuilder.Length > 0) return this.identifierBuilder.ToString();
      int start = this.startPos;
      if (this.GetChar(start) == '@' && start < this.endPos) start++;
      return this.Substring(start, this.endPos-start);
    }

    public Token GetNextToken() {
      Token token = Token.None;
      this.tokenIsFirstAfterLineBreak = false;
    nextToken:
      this.identifierBuilder.Length = 0;
      char c = this.SkipBlanks();
      if (this.endPos > 0) this.startPos = this.endPos - 1;
      switch (c) {
        case (char)0:
          token = Token.EndOfFile; //Null char is a signal from SkipBlanks that end of source has been reached
          this.tokenIsFirstAfterLineBreak = true;
          break;
        case '{':
          token = Token.LeftBrace;
          break;
        case '}':
          token = Token.RightBrace;
          break;
        case '[':
          token = Token.LeftBracket;
          break;
        case ']':
          token = Token.RightBracket;
          break;
        case '(':
          token = Token.LeftParenthesis;
          break;
        case ')':
          token = Token.RightParenthesis;
          break;
        case '.':
          token = Token.Dot;
          c = this.GetCurrentChar();
          if (Scanner.IsDigit(c)) {
            token = this.ScanNumber('.');
          } else if (c == '.') {
            token = Token.Range;
            this.endPos++;
          }
          break;
        case ',':
          token = Token.Comma;
          break;
        case ':':
          token = Token.Colon;
          c = this.GetCurrentChar();
          if (c == ':') {
            token = Token.DoubleColon;
            this.endPos++;
          }
          break;
        case ';':
          token = Token.Semicolon;
          break;
        case '+':
          token = Token.Plus;
          c = this.GetCurrentChar();
          if (c == '=') {
            token = Token.PlusAssign; this.endPos++;
          } else if (c == '+') {
            token = Token.AddOne; this.endPos++;
          }
          break;
        case '-':
          token = Token.Subtract;
          c = this.GetCurrentChar();
          if (c == '=') {
            token = Token.SubtractAssign; this.endPos++;
          } else if (c == '-') {
            token = Token.SubtractOne; this.endPos++;
          } else if (c == '>') {
            token = Token.Arrow; this.endPos++;
          }
          break;
        case '*':
          token = Token.Multiply;
          c = this.GetCurrentChar();
          if (c == '=') {
            token = Token.MultiplyAssign; this.endPos++;
          }
          break;
        case '/':
          token = Token.Divide;
          c = this.GetCurrentChar();
          switch (c) {
            case '=':
              token = Token.DivideAssign; this.endPos++;
              break;
            case '/':
              c = this.PeekAheadOneCharacter();
              //if (c == '^' && this.PeekAheadBy(2) != '^') { // Spec#-lite comment
              //  // The check on endPos+1 is so that comments that look like //^^^^^^^ ...
              //  // don't get mistakenly identified as Spec#-lite comments (since no Spec#
              //  // construct begins with a caret we think this is safe to do).
              //  // //^ construct, just swallow it and pretend it wasn't there
              //  this.endPos += 2;
              //  if (this.ignoreComments) goto nextToken;
              //  token = Token.SingleLineComment;
              //  break;
              //} else {
                this.SkipSingleLineComment();
                if (this.ignoreComments) {
                  if (this.endPos >= this.charsInBuffer) {
                    token = Token.EndOfFile;
                    this.tokenIsFirstAfterLineBreak = true;
                    break; // just break out and return
                  }
                  goto nextToken; // read another token this last one was a comment
                } else {
                  token = Token.SingleLineComment;
                  break;
                }
              //}
            case '*':
              this.endPos++;
              c = this.GetCurrentChar();
              //^ assert 0 < this.endPos;
              //if (c == '^' && this.PeekAheadBy(1) != '^') { // Spec#-lite comment
              //  // The check on endPos+1 is so that comments that look like /*^^^^^^^ ...
              //  // don't get mistakenly identified as Spec#-lite comments (since no Spec#
              //  // construct begins with a caret we think this is safe to do).
              //  // begin /*^ ... ^*/ construct
              //  this.endPos += 1;
              //  goto nextToken;
              //}
              //if (c == '!' && this.PeekAheadBy(1) == '*' && this.PeekAheadBy(2) == '/') { // Spec#-lite comment
              //  // special comment convention for non-null types, "/*!*/" is short for "/*^ ! ^*/"
              //  token = Token.LogicalNot;
              //  this.endPos += 3;
              //  break;
              //}
              //if (c == '?' && this.PeekAheadBy(1) == '*' && this.PeekAheadBy(2) == '/') { // Spec#-lite comment
              //  // special comment convention for non-null types, "/*?*/" is short for "/*^ ? ^*/"
              //  token = Token.Conditional;
              //  this.endPos += 3;
              //  break;
              //}
              //^ assume 0 < this.endPos; //follows from previous assert
              if (this.ignoreComments) {
                int savedEndPos = this.endPos;
                this.SkipMultiLineComment();
                if (this.endPos == this.charsInBuffer && this.GetPreviousChar() != '/') {
                  this.endPos = savedEndPos;
                  this.HandleError(Error.NoCommentEnd);
                  this.tokenIsFirstAfterLineBreak = true;
                  token = Token.EndOfFile;
                  this.endPos = this.charsInBuffer;
                  break;
                }
                goto nextToken; // read another token this last one was a comment
              } else {
                this.SkipMultiLineComment();
                token = Token.MultiLineComment;
                break;
              }
          }
          break;
        case '%':
          token = Token.Remainder;
          c = this.GetCurrentChar();
          if (c == '=') {
            token = Token.RemainderAssign; this.endPos++;
          }
          break;
        case '&':
          token = Token.BitwiseAnd;
          c = this.GetCurrentChar();
          if (c == '=') {
            token = Token.BitwiseAndAssign; this.endPos++;
          } else if (c == '&') {
            token = Token.LogicalAnd; this.endPos++;
          }
          break;
        case '|':
          token = Token.BitwiseOr;
          c = this.GetCurrentChar();
          if (c == '=') {
            token = Token.BitwiseOrAssign; this.endPos++;
          } else if (c == '|') {
            token = Token.LogicalOr; this.endPos++;
          }
          break;
        case '^':
          if (this.inSpecSharpMultilineComment && this.GetCurrentChar() == '*' && this.PeekAheadOneCharacter() == '/') {
            // end /*^ ... ^*/ construct
            this.endPos += 2;
            this.inSpecSharpMultilineComment = false;
            goto nextToken;
          }
          token = Token.BitwiseXor;
          c = this.GetCurrentChar();
          if (c == '=') {
            token = Token.BitwiseXorAssign; this.endPos++;
          }
          break;
        case '!':
          token = Token.LogicalNot;
          c = this.GetCurrentChar();
          if (c == '=') {
            token = Token.NotEqual; this.endPos++;
          }
          break;
        case '~':
          token = Token.BitwiseNot;
          c = this.GetCurrentChar();
          if (c == '>') {
            token = Token.Maplet; this.endPos++;
          }
          break;
        case '=':
          token = Token.Assign;
          c = this.GetCurrentChar();
          if (c == '=') {
            token = Token.Equal;
            c = this.GetNextCharAndIncrementEndPos();
            if (c == '>') {
              token = Token.Implies; this.endPos++;
            }
          } else if (c == '>') {
            token = Token.Lambda; this.endPos++;
          }
          break;
        case '<':
          token = Token.LessThan;
          c = this.GetCurrentChar();
          if (c == '=') {
            token = Token.LessThanOrEqual;
            c = this.GetNextCharAndIncrementEndPos();
            if (c == '=') {
              c = this.GetNextCharAndIncrementEndPos();
              if (c == '>') {
                token = Token.Iff; this.endPos++;
              } else {
                this.endPos--;
              }
            }
          } else if (c == '<') {
            token = Token.LeftShift;
            c = this.GetNextCharAndIncrementEndPos();
            if (c == '=') {
              token = Token.LeftShiftAssign; this.endPos++;
            }
          }
          break;
        case '>':
          token = Token.GreaterThan;
          c = this.GetCurrentChar();
          if (c == '=') {
            token = Token.GreaterThanOrEqual; this.endPos++;
          } else if (c == '>') {
            token = Token.RightShift;
            c = this.GetNextCharAndIncrementEndPos();
            if (c == '=') {
              token = Token.RightShiftAssign; this.endPos++;
            }
          }
          break;
        case '?':
          token = Token.Conditional;
          c = this.GetCurrentChar();
          if (c == '?') {
            token = Token.NullCoalescing; this.endPos++;
          }
          break;
        case '\'':
          token = Token.CharLiteral;
          this.ScanCharacter();
          break;
        case '"':
          token = Token.StringLiteral;
          this.ScanString(c);
          break;
        case '@':
          token = Token.IllegalCharacter;
          c = this.GetCurrentChar();
          if (c == (char)0) break;
          this.endPos++;
          //^ assert 0 < this.endPos;
          //^ assert this.endPos <= this.charsInBuffer;
          if (c == '"') {
            token = Token.StringLiteral;
            this.ScanVerbatimString();
            break;
          }
          if (c == '\\') goto case '\\';
          if ('a' <= c && c <= 'z' || 'A' <= c && c <= 'Z' || c == '_' || Scanner.IsUnicodeLetter(c)) {
            //^ assume 0 < this.endPos; //follows from assert above
            //^ assume this.endPos <= this.charsInBuffer; //follows from assert above
            token = Token.Identifier;
            this.ScanIdentifier();
          }
          break;
        case '\\':
          this.endPos--;
          if (this.IsIdentifierStartChar(c)) {
            token = Token.Identifier;
            this.endPos++;
            this.ScanIdentifier();
            break;
          }
          this.endPos++;
          //^ assume this.endPos < this.charsInBuffer; //otherwise c would be (char)0
          this.ScanEscapedChar();
          token = Token.IllegalCharacter;
          break;
        // line terminators
        case '\r':
          this.tokenIsFirstAfterLineBreak = true;
          if (this.GetCurrentChar() == '\n') this.endPos++;
          goto nextToken;
        case '\n':
        case (char)0x85:
        case (char)0x2028:
        case (char)0x2029:
          this.tokenIsFirstAfterLineBreak = true;
          goto nextToken;
        default:
          if ('a' <= c && c <= 'z') {
            token = this.ScanKeyword(c);
          } else if (c == '_' && this.GetCurrentChar() == '_') {
            this.endPos++;
            token = this.ScanExtendedKeyword();
          } else if ('A' <= c && c <= 'Z' || c == '_') {
            token = Token.Identifier;
            //^ assume 0 < this.endPos && this.endPos < this.charsInBuffer; //otherwise c would be (char)0
            this.ScanIdentifier();
          } else if (Scanner.IsDigit(c)) {
            token = this.ScanNumber(c);
          } else if (Scanner.IsUnicodeLetter(c)) {
            token = Token.Identifier;
            //^ assume 0 < this.endPos && this.endPos < this.charsInBuffer; //otherwise c would be (char)0
            this.ScanIdentifier();
          } else
            token = Token.IllegalCharacter;
          break;
      }
      return token;
    }

    //^ [Pure]
    private ISourceLocation GetSourceLocation(int position, int length)
      //^ requires position >= 0;
      //^ requires 0 <= this.sourceLocation.StartIndex + position;
      //^ requires this.sourceLocation.StartIndex + position <= this.sourceLocation.Length;
      //^ requires this.sourceLocation.StartIndex+position+length <= this.sourceLocation.Length;
      //^ requires 0 <= length && length <= this.sourceLocation.Length;
    {
      int start = this.sourceLocation.StartIndex+position;
      ISourceDocument sdoc = this.sourceLocation.SourceDocument;
      //^ assume start < sdoc.Length; //follows from the precondition
      //^ assume start+length <= sdoc.Length; //follows from the precondition
      return sdoc.GetSourceLocation(start, length);
    } 

    internal string/*?*/ GetString() {
      return this.unescapedString;
    }

    internal string GetTokenSource() {
      int endPos = this.endPos;
      return this.Substring(this.startPos, endPos - this.startPos);
    }

    private void HandleError(Error error, params string[] messageParameters) {
      if (this.endPos <= this.lastReportedErrorPos) return;
      if (this.scannerErrors == null) return;
      this.lastReportedErrorPos = this.endPos;
      ISourceLocation errorLocation;
      if (error == Error.BadHexDigit) {
        //^ assume 0 <= this.offset+this.endPos-1; //no overflow
        //^ assume 0 <= this.sourceLocation.StartIndex+this.offset+this.endPos-1; //no overflow
        //^ assume this.sourceLocation.StartIndex+this.offset+this.endPos <= this.sourceLocation.Length; //from invariants
        errorLocation = this.GetSourceLocation(this.offset+this.endPos-1, 1);
      } else {
        //^ assume 0 <= this.offset+this.startPos; //no overflow
        //^ assume 0 <= this.sourceLocation.StartIndex+this.offset+this.startPos; //no overflow
        //^ assume this.sourceLocation.StartIndex+this.offset+this.endPos <= this.sourceLocation.Length; //from invariants
        errorLocation = this.GetSourceLocation(this.offset+this.startPos, this.endPos-this.startPos);
      }
      this.scannerErrors.Add(new SpecSharpErrorMessage(errorLocation, (long)error, error.ToString(), messageParameters));
    }

    //^ [Pure]
    public static bool IsBlankSpace(char c) {
      if (c == (char)0x20) return true;
      if (c <= 128)
        return c == (char)0x09 || c == (char)0x0C || c == (char)0x1A;
      else
        return IsUnicodeBlankSpace(c);
    }

    //^ [Pure]
    private static bool IsUnicodeBlankSpace(char c) {
      return Char.GetUnicodeCategory(c) == UnicodeCategory.SpaceSeparator;
    }

    //^ [Pure]
    public static bool IsBlankSpaceOrNull(char c)
      //^ ensures c == (char)0 ==> result;
    {
      if (c == (char)0x20) return true;
      if (c <= 128)
        return c == (char)0x09 || c == (char)0x0C || c == (char)0x1A || c == (char)0;
      else
        return IsUnicodeBlankSpace(c);
    }

    //^ [Pure]
    public static bool IsEndOfLine(char c)
      //^ ensures result <==> c == (char)0x0D || c == (char)0x0A || c == (char)0x85 || c == (char)0x2028 || c == (char)0x2029;
    {
      if (c == (char)0x0D || c == (char)0x0A) return true;
      return c == (char)0x85 || c == (char)0x2028 || c == (char)0x2029;
    }

    private bool IsIdentifierPartChar(char c) 
      //^ requires this.charsInBuffer > 0;
      //^ requires this.endPos < this.charsInBuffer;
    {
      if (this.IsIdentifierStartCharHelper(c, true))
        return true;
      if ('0' <= c && c <= '9')
        return true;
      if (c == '\\' && this.endPos < this.charsInBuffer-1) {
        this.endPos++;
        this.ScanEscapedChar();
        this.endPos--;
        return true; //It is not actually true, or IsIdentifierStartCharHelper would have caught it, but this makes for better error recovery
      }
      return false;
    }

    private bool IsIdentifierStartChar(char c)
      //^ requires this.charsInBuffer > 0;
      //^ requires this.endPos < this.charsInBuffer;
    {
      return this.IsIdentifierStartCharHelper(c, false);
    }

    private bool IsIdentifierStartCharHelper(char c, bool expandedUnicode)
      //^ requires this.charsInBuffer > 0;
      //^ requires this.endPos < this.charsInBuffer;
    {
      int escapeLength = 0;
      UnicodeCategory ccat = 0;
      if (c == '\\') {
        this.endPos++;
        char cc = this.GetCurrentChar();
        switch (cc) {
          case 'u':
            escapeLength = 4;
            break;
          case 'U':
            escapeLength = 8;
            break;
          default:
            this.endPos--;
            return false;
        }
        int escVal = 0;
        for (int i = 0; i < escapeLength; i++)
          //^ invariant this.charsInBuffer > 0;
        {
          this.endPos++;
          char ch = this.GetCurrentChar();
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
        //TODO: error if c < 0xA0 (except '$', '@' and '`') or if 0xD800 <= c <= 0xDFFF;
      }
      if ('a' <= c && c <= 'z' || 'A' <= c && c <= 'Z' || c == '_' || c == '$')
        goto isIdentifierChar;
      if (c < 128) {
        if (escapeLength > 0) this.endPos -= escapeLength + 1;
        return false;
      }
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
          if (escapeLength > 0) this.endPos -= escapeLength + 1;
          return false;
        case UnicodeCategory.Format:
          if (expandedUnicode) goto isIdentifierChar;
          if (escapeLength > 0) this.endPos -= escapeLength + 1;
          return false;
        default:
          if (escapeLength > 0) this.endPos -= escapeLength + 1;
          return false;
      }
    isIdentifierChar:
      if (escapeLength > 0) {
        int escapePos = this.endPos-escapeLength-1;
        int startPos = this.idLastPosOnBuilder;
        if (startPos == 0) startPos = this.startPos;
        if (escapePos > startPos)
          this.identifierBuilder.Append(this.Substring(startPos, escapePos - startPos));
        if (ccat != UnicodeCategory.Format)
          this.identifierBuilder.Append(c);
        this.idLastPosOnBuilder = this.endPos;
      } else if (ccat == UnicodeCategory.Format) {
        int startPos = this.idLastPosOnBuilder;
        if (startPos == 0) startPos = this.startPos;
        if (this.endPos > startPos)
          this.identifierBuilder.Append(this.Substring(startPos, this.endPos - startPos));
        this.idLastPosOnBuilder = this.endPos;
      }
      return true;
    }

    /// <summary>
    /// Returns true if '0' &lt;= c &amp;&amp; c &lt;= '9'.
    /// </summary>
    //^ [Pure]
    internal static bool IsDigit(char c)
      //^ ensures result <==> '0' <= c && c <= '9';
    {
      return '0' <= c && c <= '9';
    }

    internal static bool IsHexDigit(char c)
      //^ ensures result <==> Scanner.IsDigit(c) || 'A' <= c && c <= 'F' || 'a' <= c && c <= 'f';
    {
      return Scanner.IsDigit(c) || 'A' <= c && c <= 'F' || 'a' <= c && c <= 'f';
    }

    internal static bool IsAsciiLetter(char c)
      //^ ensures result <==> 'A' <= c && c <= 'Z' || 'a' <= c && c <= 'z';
    {
      return 'A' <= c && c <= 'Z' || 'a' <= c && c <= 'z';
    }

    internal static bool IsUnicodeLetter(char c)
      // ^ ensures result <==> c >= 128 && Char.IsLetter(c);
    {
      return c >= 128 && Char.IsLetter(c);
    }

    internal void RestoreDocumentPosition(int position) 
      //^ requires 0 <= position;
    {
      this.offset = position;
      this.startPos = 0;
      this.endPos = 0;
      this.charsInBuffer = 0;
      this.GetNextFragment();
    }

    private void ScanCharacter()
      //^ requires this.endPos <= this.charsInBuffer;
      //^ requires this.charsInBuffer > 0;
    {
      this.ScanString('\'');
      //^ assert this.unescapedString != null;
      int n = this.unescapedString.Length;
      if (n == 0) {
        if (this.GetCurrentChar() == '\'') {
          //this happens when ''' is encountered. Scan it as if it were legal, but give an error.
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

    private void ScanEscapedChar(StringBuilder sb) 
      //^ requires this.endPos <= this.charsInBuffer;
    {
      char ch = this.GetCurrentChar();
      if (ch != 'U') {
        sb.Append(this.ScanEscapedChar());
        return;
      }
      //Scan 32-bit Unicode character. 
      uint escVal = 0;
      this.endPos++;
      for (int i = 0; i < 8; i++)
        //^ invariant this.endPos <= this.charsInBuffer;
      {
        ch = this.GetCurrentChar();
        escVal <<= 4;
        if (Scanner.IsHexDigit(ch))
          escVal |= (uint)Scanner.GetHexValue(ch);
        else {
          this.HandleError(Error.IllegalEscape);
          escVal >>= 4;
          break;
        }
        this.endPos++;
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

    private char ScanEscapedChar()
      //^ requires this.endPos <= this.charsInBuffer;
    {
      int escVal = 0;
      bool requireFourDigits = false;
      int savedStartPos = this.startPos;
      int errorStartPos = this.endPos - 1;
      if (this.endPos == this.charsInBuffer) {
        this.startPos = errorStartPos;
        this.HandleError(Error.IllegalEscape);
        this.startPos = savedStartPos;
        return (char)0;
      }
      char ch = this.GetCurrentCharAndIncrementEndPos();
      switch (ch) {
        default:
          this.startPos = errorStartPos;
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
          return (char)0;
        // unicode escape sequence \uHHHH
        case 'u':
          requireFourDigits = true;
          goto case 'x';
        // hexadecimal escape sequence \xH or \xHH or \xHHH or \xHHHH
        case 'x':
          for (int i = 0; i < 4; i++)
            //^ invariant this.endPos <= this.charsInBuffer;
          {
            ch = this.GetCurrentChar();
            escVal <<= 4;
            if (Scanner.IsHexDigit(ch))
              escVal |= Scanner.GetHexValue(ch);
            else {
              if (this.endPos < this.charsInBuffer) this.endPos++;
              if (i == 0 || requireFourDigits) {
                this.startPos = errorStartPos;
                this.HandleError(Error.IllegalEscape);
                this.startPos = savedStartPos;
              }
              return (char)(escVal >> 4);
            }
            this.endPos++;
          }
          return (char)escVal;
      }
    }

    /// <summary>
    /// We've already seen __
    /// </summary>
    /// <returns>Extended keyword token or identifier.</returns>
    private Token ScanExtendedKeyword() 
      //^ requires this.charsInBuffer > 0;
      //^ requires this.startPos < this.endPos;
    {
      for (; ; ) 
        //^ invariant this.charsInBuffer > 0;
        //^ invariant this.startPos < this.endPos;
      {
        char c = this.GetCurrentChar();
        if ('a' <= c && c <= 'z' || c == '_') {
          this.endPos++;
          continue;
        } else {
          if (this.endPos == this.charsInBuffer)
            return Token.Identifier;
          if (this.IsIdentifierPartChar(c)) {
            this.endPos++;
            this.ScanIdentifier();
            return Token.Identifier;
          }
          break;
        }
      }
      Keyword extendedKeyword = Scanner.ExtendedKeywords;
      return extendedKeyword.GetKeyword(this.buffer, this.startPos, this.endPos, false);
    }

    private void ScanIdentifier()
      //^ requires this.endPos > 0;
    {
      int i = this.endPos;
      char[] buffer = this.buffer;
      for (; ; )
        //^ invariant this.charsInBuffer > 0;
        //^ invariant 0 <= i && i <= this.charsInBuffer && buffer == this.buffer;
        //^ invariant i >= this.endPos;
      {
        char c = buffer[i];
        if ('a' <= c && c <= 'z' || 'A' <= c && c <= 'Z' || '0' <= c && c <= '9' || c == '_' || c == '$') {
          i++;
          continue;
        }
        this.endPos = i;
        if (c == (char)0 && this.endPos >= this.charsInBuffer) {
          this.GetNextFragment();
          i = this.endPos;
          buffer = this.buffer;
          if (i >= this.charsInBuffer) break;
          continue;
        }
        if (c == '\\') {
          if (!this.IsIdentifierPartChar(c)) {
            break;
          }
        }else if (c < 128 || !this.IsIdentifierPartChar(c)) {
          break;
        }
        i = ++this.endPos;
      }
      this.endPos = i;
      if (this.idLastPosOnBuilder > 0) {
        this.identifierBuilder.Append(this.Substring(this.idLastPosOnBuilder, i - this.idLastPosOnBuilder));
        this.idLastPosOnBuilder = 0;
        if (this.identifierBuilder.Length == 0)
          this.HandleError(Error.UnexpectedToken);
      }
    }

    private Token ScanKeyword(char ch)
      //^ requires 'a' <= ch && ch <= 'z';
      //^ requires this.startPos < this.endPos;
    {
      int i = this.endPos;
      char[] buffer = this.buffer;
      for (; ; )
        //^ invariant this.charsInBuffer > 0;
        //^ invariant 0 <= i && i <= this.charsInBuffer && buffer == this.buffer;
        //^ invariant this.startPos < this.endPos;
        //^ invariant this.endPos <= i;
      {
        char c = buffer[i];
        if ('a' <= c && c <= 'z' || c == '_') {
          i++;
          continue;
        }
        this.endPos = i;
        if (c == (char)0 && this.endPos == this.charsInBuffer) {
          this.GetNextFragment();
          i = this.endPos;
          buffer = this.buffer;
          if (i >= this.charsInBuffer) break;
          continue;
        }
        if (this.IsIdentifierPartChar(c)) {
          this.endPos++;
          this.ScanIdentifier();
          return Token.Identifier;
        }
        break;
      }
      this.endPos = i;
      //^ assume Scanner.Keywords.Length == 26; //There should be an invariant to this effect, but Boogie chokes on it.
      Keyword/*?*/ keyword = Scanner.Keywords[ch - 'a'];
      if (keyword == null) return Token.Identifier;
      return keyword.GetKeyword(this.buffer, this.startPos, this.endPos, false);
    }

    private Token ScanNumber(char leadChar)
      //^ requires this.charsInBuffer > 0;
      //^ requires this.endPos > 0;
    {
      Token token = leadChar == '.' ? Token.RealLiteral : Token.IntegerLiteral;
      char c;
      if (leadChar == '0') {
        c = this.GetCurrentChar();
        if (c == 'x' || c == 'X') {
          if (!Scanner.IsHexDigit(this.PeekAheadOneCharacter()))
            return token; //return the 0 as a separate token
          this.endPos++;
          token = Token.HexLiteral;
          do 
            //^ invariant this.endPos <= this.charsInBuffer;
            //^ invariant this.endPos > 0;
          {
            c = this.GetCurrentChar();
            if (!Scanner.IsHexDigit(c)) break;
            this.endPos++;
          } while (true);
          return token;
        }
      }
      bool alreadyFoundPoint = leadChar == '.';
      bool alreadyFoundExponent = false;
      int positionOfFirstPoint = -1;      
      for (; ; )
        //^ invariant this.endPos <= this.charsInBuffer;
        //^ invariant this.endPos > 0;
      {
        c = this.GetCurrentChar();
        if (!Scanner.IsDigit(c)) {
          if (c == '.') {
            if (alreadyFoundPoint) break;
            alreadyFoundPoint = true;
            positionOfFirstPoint = this.endPos;
            token = Token.RealLiteral;
          } else if (c == 'e' || c == 'E') {
            if (alreadyFoundExponent) break;
            alreadyFoundExponent = true;
            alreadyFoundPoint = true;
            token = Token.RealLiteral;
          } else if (c == '+' || c == '-') {
            char e = this.GetPreviousChar();
            if (e != 'e' && e != 'E') break;
          } else
            break;
        }
        this.endPos++;
      }
      c = this.GetPreviousChar();
      if (c == '.') {
        this.endPos--;
        return Token.IntegerLiteral;
      }
      if (c == '+' || c == '-') {
        this.endPos--;
        c = this.GetPreviousChar();
      }
      if (c == 'e' || c == 'E') {
        this.endPos--;
        if (positionOfFirstPoint == -1) return Token.IntegerLiteral;
      }
      return token;
    }

    public TypeCode ScanNumberSuffix() {
      this.startPos = this.endPos;
      char ch = this.GetCurrentChar();
      if (ch == 'u' || ch == 'U') {
        this.endPos++;
        char ch2 = this.GetCurrentChar();
        if (ch2 == 'l' || ch2 == 'L') {
          this.endPos++;
          return TypeCode.UInt64;
        }
        return TypeCode.UInt32;
      } else if (ch == 'l' || ch == 'L') {
        this.endPos++;
        if (ch == 'l') this.HandleError(Error.LowercaseEllSuffix);
        char ch2 = this.GetCurrentChar();
        if (ch2 == 'u' || ch2 == 'U') {
          this.endPos++;
          return TypeCode.UInt64;
        }
        return TypeCode.Int64;
      } else if (ch == 'f' || ch == 'F') {
        this.endPos++;
        return TypeCode.Single;
      } else if (ch == 'd' || ch == 'D') {
        this.endPos++;
        return TypeCode.Double;
      } else if (ch == 'm' || ch == 'M') {
        this.endPos++;
        return TypeCode.Decimal;
      }
      return TypeCode.Empty;
    }

    private void ScanString(char closingQuote) 
      //^ requires closingQuote == '"' || closingQuote == '\'';
      //^ requires this.endPos <= this.charsInBuffer;
      //^ requires this.charsInBuffer > 0;
      //^ ensures this.unescapedString != null;
    {
      char ch;
      char[] buffer = this.buffer;
      int start = this.endPos;
      int i = start;
      this.unescapedString = null;
      StringBuilder/*?*/ unescapedSB = null;
      do
        //^ invariant 0 <= start;
        //^ invariant start <= i && i <= this.charsInBuffer && buffer == this.buffer;
        //^ invariant 0 < this.charsInBuffer && this.charsInBuffer <= buffer.Length;
      {
        ch = buffer[i++];
        if (ch == (char)0 && i == this.charsInBuffer+1) {
          //Reached the end of a fragment
          this.endPos = --i;
          this.GetNextFragment();
          start -= i-this.endPos;
          i = this.endPos;
          buffer = this.buffer;
          if (i == this.charsInBuffer) {
            //Reached the end of the document
            this.endPos = this.charsInBuffer-1;
            this.FindGoodRecoveryPointAndComplainAboutMissingClosingQuote(closingQuote);
            i = this.endPos;
            break;
          }
          ch = buffer[i++];
        }
        if (ch == '\\') {
          // Got an escape of some sort. Have to use the StringBuilder (but avoid calling Append for every character).
          if (unescapedSB == null) unescapedSB = new StringBuilder(256);
          // start points to the first position that has not been written to the StringBuilder.
          // The first time we get in here that position is the beginning of the string, after that
          // it is the character immediately following the escape sequence
          int len = i - start - 1;
          if (len > 0) // append all the non escaped chars to the string builder
            unescapedSB.Append(buffer, start, len);
          this.endPos = i;
          this.ScanEscapedChar(unescapedSB);
          buffer = this.buffer;
          start = i = this.endPos;
        } else if (Scanner.IsEndOfLine(ch)) {
          this.endPos = i-1;
          this.FindGoodRecoveryPointAndComplainAboutMissingClosingQuote(closingQuote);
          i = this.endPos;
          break;
        }
      } while (ch != closingQuote);

      // update this.unescapedString using the StringBuilder
      if (unescapedSB != null) {
        int len = i - start - 1;
        if (len > 0) // append any trailing non escaped chars to the string builder
          unescapedSB.Append(buffer, start, len);
        this.unescapedString = unescapedSB.ToString();
      } else {
        if (closingQuote == '\'' && (this.startPos >= i-1 || buffer[i-1] != '\'')) {
          //Get here if the closing character quote is missing. An error has already been reported and this.endPos has been positioned at an appropriate recovery point.
          if (this.startPos+1 < this.charsInBuffer)
            this.unescapedString = this.Substring(this.startPos+1, 1);
          else
            this.unescapedString = " ";
        } else {
          if (i <= this.startPos+2)
            this.unescapedString = "";
          else
            this.unescapedString = this.Substring(this.startPos+1, i-this.startPos-2);
        }
      }

      this.endPos = i;
    }

    /// <summary>
    /// If an end of line sequence is encountered before the end of a string or character literal has been encountered,
    /// then this routine will look for a position where either the desired closing quote can be found (becuase the programmer does not know that new lines terminate strings)
    /// or it seems likely that the closing quote has actually been forgotten. 
    /// The routine does not scan beyond the end of the current buffer.
    /// </summary>
    private void FindGoodRecoveryPointAndComplainAboutMissingClosingQuote(char closingQuote) 
      //^ requires this.endPos < this.charsInBuffer;
    {
      int maxPos = this.charsInBuffer;
      int endPos = this.endPos;
      int i;
      if (endPos == maxPos) {
        //Reached the end of the file before reaching the end of the line containing the start of the unterminated string or character literal.
        this.endPos = endPos = maxPos;
      } else {
        //^ assert 0 <= endPos && endPos < maxPos;
        //peek ahead in the buffer looking for a matching quote that occurs before any character that is probably not part of the literal.
        for (i = endPos; i < maxPos; i++)
          //^ invariant maxPos == this.charsInBuffer;
          //^ invariant endPos == this.endPos;
          // ^ invariant 0 <= i && i < maxPos;
        {
          //^ assume 0 <= i;
          //^ assume i < maxPos;
          char ch = this.GetChar(i);
          if (ch == closingQuote) {
            //Found a matching quote before running into a character that is probably not part of the literal.
            //Give an error, but go on as if a new line is actually allowed inside the string or character literal.
            if (this.endPos > 0) {
              //Trim the span of the error to coincide with the last character of the line in which the literal starts.
              this.endPos--;
              if (this.endPos > 0 && this.GetPreviousChar() == (char)0x0d) this.endPos--;
            }
            this.HandleError(Error.NewlineInConst);
            this.endPos = i + 1; //Now update this.endPos to point to the first character beyond the closing quote
            return;
          }
          //^ assert 0 <= i && i < maxPos;
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
              //Found a character that is probably not meant to be part of the string or character literal.
              i = maxPos; //Terminate the for loop.
              break;
          }
          //^ assert 0 <= i && i <= maxPos;
        }
      }

      //At this point the assumption is that the closing quote has been omitted by mistake.
      //Look for a likely point where the ommission occurred.
      int lastSemicolon = endPos;
      int lastNonBlank = this.startPos;
      for (i = this.startPos+1; i < endPos; i++)
        //^ invariant endPos == this.endPos;
        // ^ invariant 0 <= i && i < endPos;
      {
        //^ assume 0 <= i && i < endPos;
        char ch = this.GetChar(i);
        if (ch == ';') { lastSemicolon = i; lastNonBlank = i; }
        if (ch == '/' && i < endPos - 1) {
          char ch2 = this.GetChar(++i);
          if (ch2 == '/' || ch2 == '*') {
            i -= 2; break;
          }
        }
        if (Scanner.IsEndOfLine(ch)) break;
        if (!Scanner.IsBlankSpace(ch)) lastNonBlank = i;
      }
      if (lastSemicolon == lastNonBlank)
        this.endPos = lastSemicolon; //The last non blank character before the end of the line (or start of a comment) is a semicolon. Likely, the missing quote should precede it.
      else
        this.endPos = i; //i is the position of the end of line, or of the start of a comment.
      int savedStartPos = this.startPos; //Save the start of the current token
      //Constrain the span of the error to the character before which the missing quote should be inserted.
      this.startPos = this.endPos;
      this.endPos++; //increment endPos to provide a non empty span for the error
      if (closingQuote == '"')
        this.HandleError(Error.ExpectedDoubleQuote);
      else
        this.HandleError(Error.ExpectedSingleQuote);
      //Restore the start of the current token
      this.startPos = savedStartPos;
      //Undo the increment of this.endPos
      this.endPos--;
    }

    private void ScanVerbatimString()
      //^ requires this.endPos <= this.charsInBuffer;
      //^ requires this.charsInBuffer > 0;
    {
      this.unescapedString = null;
      StringBuilder/*?*/ unescapedSB = null;
      int start = this.endPos; //Position of first character of actual string that has not yet been written to unescapedSB
      int i = start;
      char[] buffer = this.buffer;
      char ch;
      //^ assert start <= i && i <= this.charsInBuffer && buffer == this.buffer;
      for (; ; )
        //^ invariant 0 <= start;
        //^ invariant start <= i && i <= this.charsInBuffer && buffer == this.buffer;
        //^ invariant 0 < this.charsInBuffer && this.charsInBuffer <= buffer.Length;
      {
        ch = buffer[i++];
        if (ch == '"') {
          ch = buffer[i];
          if (ch == (char)0 && i >= this.charsInBuffer) {
            //^ assert start <= i && i <= this.charsInBuffer && buffer == this.buffer;
            //Reached the end of a fragment
            this.endPos = i;
            this.GetNextFragment();
            start -= i-this.endPos;
            i = this.endPos;
            buffer = this.buffer;
            if (i >= this.charsInBuffer) {
              i = this.charsInBuffer;
              this.HandleError(Error.NewlineInConst);
              break;
            }
            //^ assume start <= i && i <= this.charsInBuffer && buffer == this.buffer;
            ch = buffer[i];
          }
          if (ch != '"') break; //Reached the end of the string
          //Found a "" sequence. This is an escape sequence for an embedded ", hence need to resort to using a StringBuilder.
          i++;
          if (unescapedSB == null) unescapedSB = new StringBuilder(1024);
          // start points to the first position that has not been written to the StringBuilder.
          // The first time we get in here that position is the beginning of the string (excluding the starting quote).
          // Subsequently it is the character immediately following the "" pair
          int len = i - start - 1;
          if (len > 0) // append all the non escaped chars to the string builder
            unescapedSB.Append(buffer, start, len);
          start = i;
        } else if (ch == (char)0 && i >= this.charsInBuffer) {
          //Reached the end of a fragment
          this.endPos = i;
          this.GetNextFragment();
          start -= i-this.endPos;
          i = this.endPos;
          buffer = this.buffer;
          if (i >= this.charsInBuffer) {
            i = this.charsInBuffer;
            this.HandleError(Error.NewlineInConst);
            break;
          }
        }
        //^ assume start <= i && i <= this.charsInBuffer && buffer == this.buffer;
      }
      this.endPos = i;
      // update this.unescapedString using the StringBuilder
      if (unescapedSB != null) {
        int len = this.endPos - start - 1;
        if (len > 0) {
          // append all the non escape chars to the string builder
          unescapedSB.Append(this.buffer, start, len);
        }
        this.unescapedString = unescapedSB.ToString();
      } else {
        if (this.endPos <= this.startPos + 3)
          this.unescapedString = "";
        else
          this.unescapedString = this.Substring(this.startPos + 2, this.endPos - this.startPos - 3);
      }
    }

    private char SkipBlanks() 
      //^ ensures result == (char)0 ==> this.startPos == 0 && this.endPos == 0 && this.charsInBuffer == 0;
      //^ ensures result != (char)0 ==> result == this.buffer[this.startPos] && this.endPos == this.startPos+1;
    {
      char[] buffer = this.buffer;
      int i = this.endPos;
      char c = buffer[i];
      while (Scanner.IsBlankSpaceOrNull(c))
        //^ invariant 0 <= i && i <= this.charsInBuffer && buffer == this.buffer;
        //^ invariant c == buffer[i];
      {
        if (i == this.charsInBuffer){
          //Reached the end of a fragment
          this.startPos = this.endPos = i;
          this.GetNextFragment();
          if (this.charsInBuffer == 0) return (char)0; //Reached the end of the document
          i = this.endPos-1;
          buffer = this.buffer;
        }
        c = buffer[++i];
      }
      if (c != (char)0) {
        this.startPos = i;
        this.endPos = ++i;
      }
      return c;
    }

    private void SkipMultiLineComment() 
      //^ requires this.endPos > 0;
    {
      int i = this.endPos;
      char[] buffer = this.buffer;
      bool previousCharWasAsterisk = false;
      for (; ; )
        //^ invariant 0 <= i && i <= this.charsInBuffer && buffer == this.buffer;
      {
        char c = buffer[i++];
        if (c == '/' && previousCharWasAsterisk) {
          this.endPos = i;
          return;
        }
        if (i > this.charsInBuffer) {
          //Reached the end of a fragment
          this.endPos = --i;
          this.GetNextFragment();
          i = this.endPos;
          buffer = this.buffer;
          if (i >= this.charsInBuffer) return; //Reached the end of the document
          continue;
        }
        previousCharWasAsterisk = c == '*';        
      }
    }

    private void SkipSingleLineComment() {
      char[] buffer = this.buffer;
      int i = this.endPos;
      char c = buffer[i];
      while (!Scanner.IsEndOfLine(c))
        //^ invariant 0 <= i && i <= this.charsInBuffer && buffer == this.buffer;
        //^ invariant i == this.charsInBuffer ==> c == (char)0;
      {
        if (i == this.charsInBuffer) {
          //Reached the end of a fragment
          this.endPos = i;
          this.GetNextFragment();
          i = this.endPos;
          buffer = this.buffer;
          if (i >= this.charsInBuffer) return; //Reached the end of the document
          c = buffer[i];
          continue;
        }
        c = buffer[++i];
      }
      this.endPos = i;
      if (c == (char)0x0D && this.PeekAheadOneCharacter() == (char)0x0A)
        this.endPos++;
    }

    public ISourceLocation SourceLocationOfLastScannedToken {
      get {
        //^ assume 0 <= this.sourceLocation.StartIndex+this.offset+this.startPos; //no overflow
        //^ assume this.sourceLocation.StartIndex+this.offset+this.endPos <= this.sourceLocation.Length; //invariant
        return this.GetSourceLocation(this.offset+this.startPos, this.endPos-this.startPos);
      }
    }

    private string Substring(int start, int length)
      //^ requires 0 <= start;
      //^ requires start < this.buffer.Length;
      //^ requires 0 <= length;
      //^ requires 0 <= start + length;
      //^ requires start + length <= this.charsInBuffer;
    {
      return new string(this.buffer, start, length);
    }

  }

  public enum Token : int {
    /// <summary>
    /// default(Token). Not a real token.
    /// </summary>
    None,

    /// <summary>
    /// abstract
    /// </summary>
    Abstract,
    /// <summary>
    /// acquire
    /// </summary>
    Acquire,
    /// <summary>
    /// Add
    /// </summary>
    Add,
    /// <summary>
    /// ++
    /// </summary>
    AddOne,
    /// <summary>
    /// alias
    /// </summary>
    Alias,
    /// <summary>
    /// __arglist
    /// </summary>
    ArgList,
    /// <summary>
    /// ->
    /// </summary>
    Arrow,
    /// <summary>
    /// as
    /// </summary>
    As,
    /// <summary>
    /// assert
    /// </summary>
    Assert,
    /// <summary>
    /// =
    /// </summary>
    Assign,
    /// <summary>
    /// assume
    /// </summary>
    Assume,
    /// <summary>
    /// base
    /// </summary>
    Base,
    /// <summary>
    /// &
    /// </summary>
    BitwiseAnd,
    /// <summary>
    /// &=
    /// </summary>
    BitwiseAndAssign,
    /// <summary>
    /// ~
    /// </summary>
    BitwiseNot,
    /// <summary>
    /// |
    /// </summary>
    BitwiseOr,
    /// <summary>
    /// |=
    /// </summary>
    BitwiseOrAssign,
    /// <summary>
    /// ^
    /// </summary>
    BitwiseXor,
    /// <summary>
    /// ^=
    /// </summary>
    BitwiseXorAssign,
    /// <summary>
    /// bool
    /// </summary>
    Bool,
    /// <summary>
    /// break
    /// </summary>
    Break,
    /// <summary>
    /// byte
    /// </summary>
    Byte,
    /// <summary>
    /// case
    /// </summary>
    Case,
    /// <summary>
    /// catch
    /// </summary>
    Catch,
    /// <summary>
    /// char
    /// </summary>
    Char,
    /// <summary>
    /// 'x'
    /// </summary>
    CharLiteral,
    /// <summary>
    /// checked
    /// </summary>
    Checked,
    /// <summary>
    /// class
    /// </summary>
    Class,
    /// <summary>
    /// ?
    /// </summary>
    Conditional,
    /// <summary>
    /// :
    /// </summary>
    Colon,
    /// <summary>
    /// ,
    /// </summary>
    Comma,
    /// <summary>
    /// const
    /// </summary>
    Const,
    /// <summary>
    /// continue
    /// </summary>
    Continue,
    /// <summary>
    /// count
    /// </summary>
    Count,
    /// <summary>
    /// decimal
    /// </summary>
    Decimal,
    /// <summary>
    /// default
    /// </summary>
    Default,
    /// <summary>
    /// delegate
    /// </summary>
    Delegate,
    /// <summary>
    /// /
    /// </summary>
    Divide,
    /// <summary>
    /// /=
    /// </summary>
    DivideAssign,
    /// <summary>
    /// do
    /// </summary>
    Do,
    /// <summary>
    /// .
    /// </summary>
    Dot,
    /// <summary>
    /// double
    /// </summary>
    Double,
    /// <summary>
    /// ::
    /// </summary>
    DoubleColon,
    /// <summary>
    /// elements_seen
    /// </summary>
    ElementsSeen,
    /// <summary>
    /// else
    /// </summary>
    Else,
    /// <summary>
    /// ensures
    /// </summary>
    Ensures,
    /// <summary>
    /// enum
    /// </summary>
    Enum,
    /// <summary>
    /// ==
    /// </summary>
    Equal,
    /// <summary>
    /// event
    /// </summary>
    Event,
    /// <summary>
    /// exists
    /// </summary>
    Exists,
    /// <summary>
    /// explicit
    /// </summary>
    Explicit,
    /// <summary>
    /// expose
    /// </summary>
    Expose,
    /// <summary>
    /// extern
    /// </summary>
    Extern,
    /// <summary>
    /// false
    /// </summary>
    False,
    /// <summary>
    /// finally
    /// </summary>
    Finally,
    /// <summary>
    /// fixed
    /// </summary>
    Fixed,
    /// <summary>
    /// float
    /// </summary>
    Float,
    /// <summary>
    /// for
    /// </summary>
    For,
    /// <summary>
    /// rorall
    /// </summary>
    Forall,
    /// <summary>
    /// roreach
    /// </summary>
    Foreach,
    /// <summary>
    /// get
    /// </summary>
    Get,
    /// <summary>
    /// goto
    /// </summary>
    Goto,
    /// <summary>
    /// >
    /// </summary>
    GreaterThan,
    /// <summary>
    /// >=
    /// </summary>
    GreaterThanOrEqual,
    /// <summary>
    /// 01234567890ABCDEF
    /// </summary>
    HexLiteral,
    /// <summary>
    /// a-zA-Z0-9_
    /// </summary>
    Identifier,
    /// <summary>
    /// if
    /// </summary>
    If,
    /// <summary>
    /// #
    /// </summary>
    IllegalCharacter,
    /// <summary>
    /// ==>
    /// </summary>
    Implies,
    /// <summary>
    /// <==>
    /// </summary>
    Iff,
    /// <summary>
    /// implicit
    /// </summary>
    Implicit,
    /// <summary>
    /// in
    /// </summary>
    In,
    /// <summary>
    /// invariant
    /// </summary>
    Invariant,
    /// <summary>
    /// int
    /// </summary>
    Int,
    /// <summary>
    /// 0-9
    /// </summary>
    IntegerLiteral,
    /// <summary>
    /// interface
    /// </summary>
    Interface,
    /// <summary>
    /// internal
    /// </summary>
    Internal,
    /// <summary>
    /// is
    /// </summary>
    Is,
    /// <summary>
    /// it
    /// </summary>
    It,
    /// <summary>
    /// =>
    /// </summary>
    Lambda,
    /// <summary>
    /// {
    /// </summary>
    LeftBrace,
    /// <summary>
    /// [
    /// </summary>
    LeftBracket,
    /// <summary>
    /// (
    /// </summary>
    LeftParenthesis,
    /// <summary>
    /// &lt;&lt;
    /// </summary>
    LeftShift,
    /// <summary>
    /// &lt;&lt;=
    /// </summary>
    LeftShiftAssign,
    /// <summary>
    /// &lt;
    /// </summary>
    LessThan,
    /// <summary>
    /// &lt;=
    /// </summary>
    LessThanOrEqual,
    /// <summary>
    /// lock
    /// </summary>
    Lock,
    /// <summary>
    /// &amp;&amp;
    /// </summary>
    LogicalAnd,
    /// <summary>
    /// !
    /// </summary>
    LogicalNot,
    /// <summary>
    /// ||
    /// </summary>
    LogicalOr,
    /// <summary>
    /// long
    /// </summary>
    Long,
    /// <summary>
    /// __makeref
    /// </summary>
    MakeRef,
    /// <summary>
    /// ~&gt;
    /// </summary>
    Maplet,
    /// <summary>
    /// modifies
    /// </summary>
    Modifies,
    /// <summary>
    /// /*....*/
    /// </summary>
    MultiLineComment,
    /// <summary>
    /// *
    /// </summary>
    Multiply,
    /// <summary>
    /// *=
    /// </summary>
    MultiplyAssign,
    /// <summary>
    /// namespace
    /// </summary>
    Namespace,
    /// <summary>
    /// new
    /// </summary>
    New,
    /// <summary>
    /// null
    /// </summary>
    Null,
    /// <summary>
    /// ??
    /// </summary>
    NullCoalescing,
    /// <summary>
    /// !=
    /// </summary>
    NotEqual,
    /// <summary>
    /// object
    /// </summary>
    Object,
    /// <summary>
    /// operator
    /// </summary>
    Operator,
    /// <summary>
    /// old
    /// </summary>
    Old,
    /// <summary>
    /// out
    /// </summary>
    Out,
    /// <summary>
    /// otherwise
    /// </summary>
    Otherwise,
    /// <summary>
    /// override
    /// </summary>
    Override,
    /// <summary>
    /// params
    /// </summary>
    Params,
    /// <summary>
    /// partial
    /// </summary>
    Partial,
    /// <summary>
    /// +
    /// </summary>
    Plus,
    /// <summary>
    /// +=
    /// </summary>
    PlusAssign,
    /// <summary>
    /// private
    /// </summary>
    Private,
    /// <summary>
    /// protected
    /// </summary>
    Protected,
    /// <summary>
    /// public
    /// </summary>
    Public,
    /// <summary>
    /// ..
    /// </summary>
    Range,
    /// <summary>
    /// read
    /// </summary>
    Read,
    /// <summary>
    /// 0-9.0-9e+-0-9
    /// </summary>
    RealLiteral,
    /// <summary>
    /// readonly
    /// </summary>
    Readonly,
    /// <summary>
    /// ref
    /// </summary>
    Ref,
    /// <summary>
    /// __reftype
    /// </summary>
    RefType,
    /// <summary>
    /// __refvalue
    /// </summary>
    RefValue,
    /// <summary>
    /// requires
    /// </summary>
    Requires,
    /// <summary>
    /// %
    /// </summary>
    Remainder,
    /// <summary>
    /// %=
    /// </summary>
    RemainderAssign,
    /// <summary>
    /// remove
    /// </summary>
    Remove,
    /// <summary>
    /// return
    /// </summary>
    Return,
    /// <summary>
    /// }
    /// </summary>
    RightBrace,
    /// <summary>
    /// ]
    /// </summary>
    RightBracket,
    /// <summary>
    /// )
    /// </summary>
    RightParenthesis,
    /// <summary>
    /// &gt;&gt;
    /// </summary>
    RightShift,
    /// <summary>
    /// &gt;&gt;=
    /// </summary>
    RightShiftAssign,
    /// <summary>
    /// sbyte
    /// </summary>
    Sbyte,
    /// <summary>
    /// set
    /// </summary>
    Set,
    /// <summary>
    /// sealed
    /// </summary>
    Sealed,
    /// <summary>
    /// ;
    /// </summary>
    Semicolon,
    /// <summary>
    /// //.....
    /// </summary>
    SingleLineComment,
    /// <summary>
    /// short
    /// </summary>
    Short,
    /// <summary>
    /// sizeof
    /// </summary>
    Sizeof,
    /// <summary>
    /// stackalloc
    /// </summary>
    Stackalloc,
    /// <summary>
    /// &lt;/
    /// </summary>
    StartOfClosingTag,
    /// <summary>
    /// &lt;
    /// </summary>
    StartOfTag,
    /// <summary>
    /// static
    /// </summary>
    Static,
    /// <summary>
    /// string
    /// </summary>
    String,
    /// <summary>
    /// " ... "
    /// </summary>
    StringLiteral,
    /// <summary>
    /// struct
    /// </summary>
    Struct,
    /// <summary>
    /// -
    /// </summary>
    Subtract,
    /// <summary>
    /// -=
    /// </summary>
    SubtractAssign,
    /// <summary>
    /// --
    /// </summary>
    SubtractOne,
    /// <summary>
    /// switch
    /// </summary>
    Switch,
    /// <summary>
    /// this
    /// </summary>
    This,
    /// <summary>
    /// throw
    /// </summary>
    Throw,
    /// <summary>
    /// throws
    /// </summary>
    Throws,
    /// <summary>
    /// true
    /// </summary>
    True,
    /// <summary>
    /// try
    /// </summary>
    Try,
    /// <summary>
    /// typeof
    /// </summary>
    Typeof,
    /// <summary>
    /// uint
    /// </summary>
    Uint,
    /// <summary>
    /// ulong
    /// </summary>
    Ulong,
    /// <summary>
    /// unchecked
    /// </summary>
    Unchecked,
    /// <summary>
    /// unique
    /// </summary>
    Unique,
    /// <summary>
    /// unsafe
    /// </summary>
    Unsafe,
    /// <summary>
    /// upto
    /// </summary>
    Upto,
    /// <summary>
    /// ushort
    /// </summary>
    Ushort,
    /// <summary>
    /// using
    /// </summary>
    Using,
    /// <summary>
    /// value
    /// </summary>
    Value,
    /// <summary>
    /// var
    /// </summary>
    Var,
    /// <summary>
    /// virtual
    /// </summary>
    Virtual,
    /// <summary>
    /// void
    /// </summary>
    Void,
    /// <summary>
    /// volatile
    /// </summary>
    Volatile,
    /// <summary>
    /// where
    /// </summary>
    Where,
    /// <summary>
    /// while
    /// </summary>
    While,
    /// <summary>
    /// write
    /// </summary>
    Write,
    /// <summary>
    /// <!-- .... -->
    /// </summary>
    LiteralComment,
    /// <summary>
    /// text and more text
    /// </summary>
    LiteralContentString,
    /// <summary>
    /// yield
    /// </summary>
    Yield,
    /// <summary>
    /// A dummy token produced when the end of the file is reached.
    /// </summary>
    EndOfFile,
  }

  internal sealed class Keyword {
    private Keyword/*?*/ next;
    private Token token;
    private string name;
    private int length; //^ invariant length >= 0;
    private bool specSharp;

    private Keyword(Token token, string name)
      //^ requires name.Length > 0;
    {
      this.name = name;
      this.token = token;
      this.length = name.Length;
    }

    private Keyword(Token token, string name, Keyword next)
      //^ requires name.Length > 0;
    {
      this.name = name;
      this.next = next;
      this.token = token;
      this.length = name.Length;
    }

    private Keyword(Token token, string name, bool specSharp)
      //^ requires name.Length > 0;
    {
      this.name = name;
      this.next = null;
      this.token = token;
      this.length = name.Length;
      this.specSharp = specSharp;
    }

    private Keyword(Token token, string name, bool specSharp, Keyword next)
      //^ requires name.Length > 0;
    {
      this.name = name;
      this.next = next;
      this.token = token;
      this.length = name.Length;
      this.specSharp = specSharp;
    }

    internal Token GetKeyword(char[] source, int startPos, int endPos, bool csharpOnly)
      //^ requires 0 <= startPos && startPos < endPos;
      //^ requires endPos < source.Length;
    {
      int length = endPos - startPos;
      Keyword/*?*/ keyword = this;
    nextToken:
      while (keyword != null) 
        //^ invariant 0 <= startPos && 0 < startPos+length && startPos+length < source.Length;
      {
        if (length == keyword.length) {
          // we know the first char has to match
          string name = keyword.name;
          for (int i = 1, j = startPos + 1; i < length; i++, j++) 
            //^ invariant i == j - startPos;
          {
            char ch1 = name[i];
            char ch2 = source[j];
            if (ch1 == ch2)
              continue;
            else if (ch2 < ch1)
              return Token.Identifier;
            else {
              keyword = keyword.next;
              goto nextToken;
            }
          }
          if (csharpOnly && keyword.specSharp) return Token.Identifier;
          return keyword.token;
        } else if (length < keyword.length)
          return Token.Identifier;

        keyword = keyword.next;
      }
      return Token.Identifier;
    }

    internal static Keyword/*?*/[] InitKeywords() {
      // There is a linked list for each letter.
      // In each list, the keywords are sorted first by length, and then lexicographically.
      // So the constructor invocations must occur in the opposite order.
      Keyword/*?*/[] keywords = new Keyword/*?*/[26];
      Keyword keyword;
      // a
      keyword = new Keyword(Token.Abstract, "abstract");
      keyword = new Keyword(Token.Acquire, "acquire", true, keyword);
      keyword = new Keyword(Token.Assume, "assume", true, keyword);
      keyword = new Keyword(Token.Assert, "assert", true, keyword);
      keyword = new Keyword(Token.Alias, "alias", keyword);
      keyword = new Keyword(Token.Add, "add", keyword);
      keyword = new Keyword(Token.As, "as", keyword);
      keywords['a' - 'a'] = keyword;
      // b
      keyword = new Keyword(Token.Break, "break");
      keyword = new Keyword(Token.Byte, "byte", keyword);
      keyword = new Keyword(Token.Bool, "bool", keyword);
      keyword = new Keyword(Token.Base, "base", keyword);
      keywords['b' - 'a'] = keyword;
      // c
      keyword = new Keyword(Token.Continue, "continue");
      keyword = new Keyword(Token.Checked, "checked", keyword);
      keyword = new Keyword(Token.Count, "count", true, keyword);
      keyword = new Keyword(Token.Const, "const", keyword);
      keyword = new Keyword(Token.Class, "class", keyword);
      keyword = new Keyword(Token.Catch, "catch", keyword);
      keyword = new Keyword(Token.Char, "char", keyword);
      keyword = new Keyword(Token.Case, "case", keyword);
      keywords['c' - 'a'] = keyword;
      // d      
      keyword = new Keyword(Token.Delegate, "delegate");
      keyword = new Keyword(Token.Default, "default", keyword);
      keyword = new Keyword(Token.Decimal, "decimal", keyword);
      keyword = new Keyword(Token.Double, "double", keyword);
      keyword = new Keyword(Token.Do, "do", keyword);
      keywords['d' - 'a'] = keyword;
      // e
      keyword = new Keyword(Token.ElementsSeen, "elements_seen", true);
      keyword = new Keyword(Token.Explicit, "explicit", keyword);
      keyword = new Keyword(Token.Ensures, "ensures", true, keyword);
      keyword = new Keyword(Token.Extern, "extern", keyword);
      keyword = new Keyword(Token.Expose, "expose", true, keyword);
      keyword = new Keyword(Token.Exists, "exists", true, keyword);
      keyword = new Keyword(Token.Event, "event", keyword);
      keyword = new Keyword(Token.Enum, "enum", keyword);
      keyword = new Keyword(Token.Else, "else", keyword);
      keywords['e' - 'a'] = keyword;
      // f
      keyword = new Keyword(Token.Foreach, "foreach");
      keyword = new Keyword(Token.Finally, "finally", keyword);
      keyword = new Keyword(Token.Forall, "forall", true, keyword);
      keyword = new Keyword(Token.Float, "float", keyword);
      keyword = new Keyword(Token.Fixed, "fixed", keyword);
      keyword = new Keyword(Token.False, "false", keyword);
      keyword = new Keyword(Token.For, "for", keyword);
      keywords['f' - 'a'] = keyword;
      // g
      keyword = new Keyword(Token.Goto, "goto");
      keyword = new Keyword(Token.Get, "get", keyword);
      keywords['g' - 'a'] = keyword;
      // i
      keyword = new Keyword(Token.Invariant, "invariant", true);
      keyword = new Keyword(Token.Interface, "interface", keyword);
      keyword = new Keyword(Token.Internal, "internal", keyword);
      keyword = new Keyword(Token.Implicit, "implicit", keyword);
      keyword = new Keyword(Token.Int, "int", keyword);
      keyword = new Keyword(Token.Is, "is", keyword);
      keyword = new Keyword(Token.In, "in", keyword);
      keyword = new Keyword(Token.If, "if", keyword);
      keywords['i' - 'a'] = keyword;
      //l
      keyword = new Keyword(Token.Long, "long");
      keyword = new Keyword(Token.Lock, "lock", keyword);
      keywords['l' - 'a'] = keyword;
      // n
      keyword = new Keyword(Token.Namespace, "namespace");
      keyword = new Keyword(Token.Null, "null", keyword);
      keyword = new Keyword(Token.New, "new", keyword);
      keywords['n' - 'a'] = keyword;
      // m
      keyword = new Keyword(Token.Modifies, "modifies", true);
      keywords['m' - 'a'] = keyword;
      // o
      keyword = new Keyword(Token.Otherwise, "otherwise");
      keyword = new Keyword(Token.Override, "override", keyword);
      keyword = new Keyword(Token.Operator, "operator", keyword);
      keyword = new Keyword(Token.Object, "object", keyword);
      keyword = new Keyword(Token.Out, "out", keyword);
      keyword = new Keyword(Token.Old, "old", true, keyword);
      keywords['o' - 'a'] = keyword;
      // p
      keyword = new Keyword(Token.Protected, "protected");
      keyword = new Keyword(Token.Private, "private", keyword);
      keyword = new Keyword(Token.Partial, "partial", keyword);
      keyword = new Keyword(Token.Public, "public", keyword);
      keyword = new Keyword(Token.Params, "params", keyword);
      keywords['p' - 'a'] = keyword;
      // r
      keyword = new Keyword(Token.Requires, "requires", true);
      keyword = new Keyword(Token.Readonly, "readonly", keyword);
      keyword = new Keyword(Token.Return, "return", keyword);
      keyword = new Keyword(Token.Remove, "remove", keyword);
      keyword = new Keyword(Token.Read, "read", true, keyword);
      keyword = new Keyword(Token.Ref, "ref", keyword);
      keywords['r' - 'a'] = keyword;
      // s
      keyword = new Keyword(Token.Stackalloc, "stackalloc");
      keyword = new Keyword(Token.Switch, "switch", keyword);
      keyword = new Keyword(Token.Struct, "struct", keyword);
      keyword = new Keyword(Token.String, "string", keyword);
      keyword = new Keyword(Token.Static, "static", keyword);
      keyword = new Keyword(Token.Sizeof, "sizeof", keyword);
      keyword = new Keyword(Token.Sealed, "sealed", keyword);
      keyword = new Keyword(Token.Short, "short", keyword);
      keyword = new Keyword(Token.Sbyte, "sbyte", keyword);
      keyword = new Keyword(Token.Set, "set", keyword);
      keywords['s' - 'a'] = keyword;
      // t
      keyword = new Keyword(Token.Typeof, "typeof");
      keyword = new Keyword(Token.Throws, "throws", true, keyword);
      keyword = new Keyword(Token.Throw, "throw", keyword);
      keyword = new Keyword(Token.True, "true", keyword);
      keyword = new Keyword(Token.This, "this", keyword);
      keyword = new Keyword(Token.Try, "try", keyword);
      keywords['t' - 'a'] = keyword;
      // u
      keyword = new Keyword(Token.Unchecked, "unchecked");
      keyword = new Keyword(Token.Ushort, "ushort", keyword);
      keyword = new Keyword(Token.Unsafe, "unsafe", keyword);
      keyword = new Keyword(Token.Unique, "unique", keyword);
      keyword = new Keyword(Token.Using, "using", keyword);
      keyword = new Keyword(Token.Ulong, "ulong", keyword);
      keyword = new Keyword(Token.Uint, "uint", keyword);
      keywords['u' - 'a'] = keyword;
      // v
      keyword = new Keyword(Token.Volatile, "volatile");
      keyword = new Keyword(Token.Virtual, "virtual", keyword);
      keyword = new Keyword(Token.Value, "value", keyword);
      keyword = new Keyword(Token.Void, "void", keyword);
      keyword = new Keyword(Token.Var, "var", true, keyword);
      keywords['v' - 'a'] = keyword;
      // w
      keyword = new Keyword(Token.Write, "write");
      keyword = new Keyword(Token.While, "while", keyword);
      keyword = new Keyword(Token.Where, "where", keyword);
      keywords['w' - 'a'] = keyword;
      // y
      keyword = new Keyword(Token.Yield, "yield");
      keywords['y' - 'a'] = keyword;

      return keywords;
    }
    public static Keyword InitExtendedKeywords() {
      // This is a linked list of keywords starting with __
      // In the list, the keywords are sorted first by length, and then lexicographically.
      // So the constructor invocations must occur in the opposite order.
      Keyword keyword;
      // __
      keyword = new Keyword(Token.RefValue, "__refvalue");
      keyword = new Keyword(Token.RefType, "__reftype", keyword);
      keyword = new Keyword(Token.MakeRef, "__makeref", keyword);
      keyword = new Keyword(Token.ArgList, "__arglist", keyword);

      return keyword;
    }
  }
}

