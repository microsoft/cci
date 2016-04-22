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
using System.IO;
using System.Diagnostics.Contracts;

namespace Microsoft.Cci {
  /// <summary>
  /// A wrapper for a TextWriter instance that adds various methods for emitting source code in a nicely formatted way.
  /// The formatting conventions can be specified via options.
  /// </summary>
  public class SourceEmitter {

    /// <summary>
    /// A wrapper for a TextWriter instance that adds various methods for emitting source code in a nicely formatted way.
    /// The formatting conventions can be specified via options.
    /// </summary>
    /// <param name="textWriter">The TextWriter instance to which the formatted source code is written.</param>
    public SourceEmitter(TextWriter textWriter) {
      Contract.Requires(textWriter != null);

      this.textWriter = textWriter;
      this.atTheStartOfANewLine = true;
    }

    readonly TextWriter textWriter;
    bool atTheStartOfANewLine;

    [ContractInvariantMethod]
    private void ObjectInvariant() {
      Contract.Invariant(this.textWriter != null);
    }

    /// <summary>
    /// If true the indentation level is incremented after the opening delimiter for a block has been emitted
    /// and decreased again after the closing delimiter has been emitted.
    /// </summary>
    public bool IndentBlockContents {
      get { return this.indentBlockContents; }
      set { this.indentBlockContents = value; }
    }
    private bool indentBlockContents = true;

    /// <summary>
    /// If true then EmitBlockOpeningDelimiter increments the indentation level before writing out the delimiter
    /// and EmitBlockClosingDelimiter decrements the indentation level after writing out the delimiter.
    /// </summary>
    public bool IndentBlockDelimiters {
      get { return this.indentBlockDelimiters; }
      set { this.indentBlockDelimiters = value; }
    }
    private bool indentBlockDelimiters;

    /// <summary>
    /// If true the indentation level is incremented after the opening delimiter for a switch case has been emitted
    /// and decreased again after the closing delimiter has been emitted.
    /// </summary>
    public bool IndentCaseContents {
      get { return this.indentCaseContents; }
      set { this.indentCaseContents = value; }
    }
    private bool indentCaseContents = true;

    /// <summary>
    /// If true then EmitCaseOpeningDelimiter increments the indentation level before writing out the delimiter
    /// and EmitCaseClosingDelimiter decrements the indentation level after writing out the delimiter.
    /// </summary>
    public bool IndentCaseDelimiters {
      get { return this.indentCaseDelimiters; }
      set { this.indentCaseDelimiters = value; }
    }
    private bool indentCaseDelimiters = true;

    /// <summary>
    /// If true, namespace members (for example: types) are separated by blank lines.
    /// </summary>
    public bool LeaveBlankLinesBetweenNamespaceMembers {
      get { return this.leaveBlankLinesBetweenNamespaceMembers; }
      set { this.leaveBlankLinesBetweenNamespaceMembers = value; }
    }
    private bool leaveBlankLinesBetweenNamespaceMembers = true;

    /// <summary>
    /// If true, type members (for example: fields) are separated by blank lines.
    /// </summary>
    public bool LeaveBlankLinesBetweenTypeMembers {
      get { return this.leaveBlankLinesBetweenTypeMembers; }
      set { this.leaveBlankLinesBetweenTypeMembers = value; }
    }
    private bool leaveBlankLinesBetweenTypeMembers = true;

    /// <summary>
    /// If true then EmitAnonymousMethodBodyOpeningDelimiter starts a new line before calling EmitBlockOpeningDelimiter.
    /// </summary>
    public bool PlaceAnonymousMethodBodyOpeningDelimitersOnNewLine {
      get { return this.placeAnonymousMethodBodyOpeningDelimitersOnNewLine; }
      set { this.placeAnonymousMethodBodyOpeningDelimitersOnNewLine = value; }
    }
    private bool placeAnonymousMethodBodyOpeningDelimitersOnNewLine;

    /// <summary>
    /// If true then EmitAnonymousTypeBodyOpeningDelimiter starts a new line before calling EmitBlockOpeningDelimiter.
    /// </summary>
    public bool PlaceAnonymousTypeBodyOpeningDelimitersOnNewLine {
      get { return this.placeAnonymousTypeBodyOpeningDelimitersOnNewLine; }
      set { this.placeAnonymousTypeBodyOpeningDelimitersOnNewLine = value; }
    }
    private bool placeAnonymousTypeBodyOpeningDelimitersOnNewLine;

    /// <summary>
    /// If true then EmitControlBlockOpeningDelimiter starts a new line before calling EmitBlockOpeningDelimiter.
    /// A control block is the body of an loop or the true/false part of an if statement, for example.
    /// </summary>
    public bool PlaceControlBlockOpeningDelimitersOnNewLine {
      get { return this.placeControlBlockOpeningDelimitersOnNewLine; }
      set { this.placeControlBlockOpeningDelimitersOnNewLine = value; }
    }
    private bool placeControlBlockOpeningDelimitersOnNewLine;

    /// <summary>
    /// If true then EmitCatch starts a new line before emitting the catch delimiter.
    /// </summary>
    public bool PlaceCatchOnNewLine {
      get { return this.placeCatchOnNewLine; }
      set { this.placeCatchOnNewLine = value; }
    }
    private bool placeCatchOnNewLine;

    /// <summary>
    /// If true then EmitElse starts a new line before emitting the else delimiter.
    /// </summary>
    public bool PlaceElseOnNewLine {
      get { return this.placeElseOnNewLine; }
      set { this.placeElseOnNewLine = value; }
    }
    private bool placeElseOnNewLine;

    /// <summary>
    /// If true then EmitFinally starts a new line before emitting the finally delimiter.
    /// </summary>
    public bool PlaceFinallyOnNewLine {
      get { return this.placeFinallyOnNewLine; }
      set { this.placeFinallyOnNewLine = value; }
    }
    private bool placeFinallyOnNewLine;

    /// <summary>
    /// If true then EmitLambdaBodyOpeningDelimiter starts a new line before calling EmitBlockOpeningDelimiter.
    /// </summary>
    public bool PlaceLambdaBodyOpeningDelimitersOnNewLine {
      get { return this.placeLambdaBodyOpeningDelimitersOnNewLine; }
      set { this.placeLambdaBodyOpeningDelimitersOnNewLine = value; }
    }
    private bool placeLambdaBodyOpeningDelimitersOnNewLine;

    /// <summary>
    /// If true then EmitMethodBodyOpeningDelimiter starts a new line before calling EmitBlockOpeningDelimiter.
    /// </summary>
    public bool PlaceMethodBodyOpeningDelimitersOnNewLine {
      get { return this.placeMethodBodyOpeningDelimitersOnNewLine; }
      set { this.placeMethodBodyOpeningDelimitersOnNewLine = value; }
    }
    private bool placeMethodBodyOpeningDelimitersOnNewLine;

    /// <summary>
    /// If true then EmitObjectInitializerBodyOpeningDelimiter starts a new line before calling EmitBlockOpeningDelimiter.
    /// </summary>
    public bool PlaceObjectInitializerBodyOpeningDelimitersOnNewLine {
      get { return this.placeObjectInitializerBodyOpeningDelimitersOnNewLine; }
      set { this.placeObjectInitializerBodyOpeningDelimitersOnNewLine = value; }
    }
    private bool placeObjectInitializerBodyOpeningDelimitersOnNewLine;

    /// <summary>
    /// If true then EmitTypeBodyOpeningDelimiter starts a new line before calling EmitBlockOpeningDelimiter.
    /// </summary>
    public bool PlaceTypeBodyOpeningDelimitersOnNewLine {
      get { return this.placeTypeBodyOpeningDelimitersOnNewLine; }
      set { this.placeTypeBodyOpeningDelimitersOnNewLine = value; }
    }
    private bool placeTypeBodyOpeningDelimitersOnNewLine;

    /// <summary>
    /// The number of times this.IndentSize spaces are added to the start of a new line before writing source to it.
    /// </summary>
    public byte IndentationLevel {
      get { return this.indentationLevel; }
      set { this.indentationLevel = value; }
    }
    private byte indentationLevel;

    /// <summary>
    /// The number of spaces, for each level of indentation, to add to the start of a new line before writing source code to it.
    /// </summary>
    public byte IndentSize {
      get { return indentSize; }
      set { indentSize = value; }
    }
    private byte indentSize = 2;

    /// <summary>
    /// Choices for how labels are to be indented when they start a new line.
    /// </summary>
    public enum LabelIndentationKind {
      /// <summary>
      /// Nothing is to precede the label when it is the first thing on a new line.
      /// </summary>
      PlaceLabelsInLeftmostColumn,
      /// <summary>
      /// The label is to be indented one level less than the current level of indentation when it is the first thing on a new line.
      /// </summary>
      PlaceLabelsOneIndentationLessThanCurrentLevel,
      /// <summary>
      /// The label is to be indented at the current level of indentation when it is the first thing on a new line.
      /// </summary>
      PlaceLabelsAtCurrentIndentation,
    }

    /// <summary>
    /// The kind of indentation that is to precede a label when it is the first thing on a new line.
    /// </summary>
    public LabelIndentationKind LabelIndentation {
      get { return this.labelIndentation; }
      set { this.labelIndentation = value; }
    }
    private LabelIndentationKind labelIndentation = LabelIndentationKind.PlaceLabelsOneIndentationLessThanCurrentLevel;

    /// <summary>
    /// Write out the given string and note that it is the closing delimiter of a block.
    /// If this.IdentBlockContents is true then this.IndentationLevel will be incremented before writing out the delimiter.
    /// If this.IndentBlockDelimiters is true then this.IndentationLevel will be decremented after writing out the delimiter.
    /// </summary>
    /// <param name="delimiter">A string representing the start of a block.</param>
    public void EmitBlockClosingDelimiter(string delimiter) {
      Contract.Requires(delimiter != null);

      if (this.IndentBlockContents) this.IndentationLevel--;
      this.EmitString(delimiter);
      if (this.IndentBlockDelimiters) this.IndentationLevel--;
    }

    /// <summary>
    /// Write out the given string and note that it is the opening delimiter of a block.
    /// If this.IndentBlockDelimiters is true then this.IndentationLevel will be incremented before writing out the delimiter.
    /// If this.IdentBlockContents is true then this.IndentationLevel will be incremented after writing out the delimiter.
    /// </summary>
    /// <param name="delimiter">A string representing the start of a block.</param>
    public void EmitBlockOpeningDelimiter(string delimiter) {
      Contract.Requires(delimiter != null);

      if (this.IndentBlockDelimiters) this.IndentationLevel++;
      this.EmitString(delimiter);
      if (this.IndentBlockContents) this.IndentationLevel++;
    }

    /// <summary>
    /// Write out the given string and note that it is the closing delimiter of a switch case.
    /// If this.IndentCaseContents is true then this.IndentationLevel will be decremented before writing out the delimiter.
    /// </summary>
    /// <param name="delimiter">A string representing the start of a block.</param>
    public void EmitCaseClosingDelimiter(string delimiter) {
      Contract.Requires(delimiter != null);

      if (this.IndentCaseContents) this.IndentationLevel--;
      this.EmitString(delimiter);
      if (this.IndentCaseDelimiters) this.IndentationLevel--;
    }

    /// <summary>
    /// Write out the given string and note that it is the opening delimiter of a switch case.
    /// If this.IndentCaseContents is true then this.IndentationLevel will be incremented after writing out the delimiter.
    /// </summary>
    /// <param name="delimiter">A string representing the start of a block.</param>
    public void EmitCaseOpeningDelimiter(string delimiter) {
      Contract.Requires(delimiter != null);

      if (this.IndentCaseDelimiters) this.IndentationLevel++;
      this.EmitString(delimiter);
      if (this.IndentCaseContents) this.IndentationLevel++;
    }

    /// <summary>
    /// If this.PlaceCatchOnNewLine is true then a new line will be emitted, if necessary, 
    /// before calling this.EmitString with the given catch delimiter string.
    /// </summary>
    /// <param name="catchDelimiter">A string representing the start of the else part of an if statement.</param>
    public void EmitCatch(string catchDelimiter) {
      Contract.Requires(catchDelimiter != null);

      if (this.PlaceCatchOnNewLine && !this.atTheStartOfANewLine) this.EmitNewLine();
      this.EmitString(catchDelimiter);
    }

    /// <summary>
    /// If this.PlaceElseOnNewLine is true then a new line will be emitted, if necessary, 
    /// before calling this.EmitString with the given else delimiter string.
    /// </summary>
    /// <param name="elseDelimiter">A string representing the start of the else part of an if statement.</param>
    public void EmitElse(string elseDelimiter) {
      Contract.Requires(elseDelimiter != null);

      if (this.PlaceElseOnNewLine && !this.atTheStartOfANewLine) this.EmitNewLine();
      this.EmitString(elseDelimiter);
    }

    /// <summary>
    /// If this.PlaceFinallyOnNewLine is true then a new line will be emitted, if necessary, 
    /// before calling this.EmitString with the given finally delimiter string.
    /// </summary>
    /// <param name="finallyDelimiter">A string representing the start of the else part of an if statement.</param>
    public void EmitFinally(string finallyDelimiter) {
      Contract.Requires(finallyDelimiter != null);

      if (this.PlaceFinallyOnNewLine && !this.atTheStartOfANewLine) this.EmitNewLine();
      this.EmitString(finallyDelimiter);
    }

    /// <summary>
    /// Emits the given string, after applying the indentation rules applicable to labels.
    /// </summary>
    /// <param name="label">The string to emit as a label.</param>
    public void EmitLabel(string label) {
      Contract.Requires(label != null);

      if (this.atTheStartOfANewLine && this.LabelIndentation != LabelIndentationKind.PlaceLabelsInLeftmostColumn) {
        int indentationLevel;
        if (this.LabelIndentation == LabelIndentationKind.PlaceLabelsOneIndentationLessThanCurrentLevel)
          indentationLevel = this.IndentationLevel-1;
        else
          indentationLevel = this.IndentationLevel;
        for (int i = 0, n = indentationLevel*this.IndentSize; i < n; i++)
          this.textWriter.Write(' ');
        this.atTheStartOfANewLine = false;         
      }
      this.textWriter.Write(label);
    }

    /// <summary>
    /// Emits a new line character, thus starting a new source line.
    /// </summary>
    public void EmitNewLine() {
      this.textWriter.WriteLine();
      this.atTheStartOfANewLine = true;
    }

    /// <summary>
    /// Emits the given string to the current source line.
    /// If this is the first string on the line, indentation will be emitted before the string.
    /// Note, however, that if the string is empty nothing is emitted, not even indentation.
    /// </summary>
    /// <param name="str">The string to emit.</param>
    public void EmitString(string str) {
      Contract.Requires(str != null);

      if (str.Length == 0) return;
      this.IndentIfAtStartOfNewLine();
      this.textWriter.Write(str);
    }

    /// <summary>
    /// If this.PlaceAnonymousMethodBodyOpeningDelimitersOnNewLine is true a new line is emitted, if necessary,
    /// before calling this.EmitBlockOpeningDelimiter with the given delimiter.
    /// </summary>
    /// <param name="delimiter">A string representing the start of an anonymous method body block.</param>
    public void EmitAnonymousMethodBodyOpeningDelimiter(string delimiter) {
      Contract.Requires(delimiter != null);

      if (this.PlaceAnonymousMethodBodyOpeningDelimitersOnNewLine && !this.atTheStartOfANewLine)
        this.EmitNewLine();
      this.EmitBlockOpeningDelimiter(delimiter);
    }

    /// <summary>
    /// If this.PlaceAnonymousTypeBodyOpeningDelimitersOnNewLine is true a new line is emitted, if necessary,
    /// before calling this.EmitBlockOpeningDelimiter with the given delimiter.
    /// </summary>
    /// <param name="delimiter">A string representing the start of a anonymous type body block.</param>
    public void EmitAnonymousTypeBodyOpeningDelimiter(string delimiter) {
      Contract.Requires(delimiter != null);

      if (this.PlaceAnonymousTypeBodyOpeningDelimitersOnNewLine && !this.atTheStartOfANewLine)
        this.EmitNewLine();
      this.EmitBlockOpeningDelimiter(delimiter);
    }

    /// <summary>
    /// If this.PlaceControlBlockOpeningDelimitersOnNewLine is true a new line is emitted, if necessary,
    /// before calling this.EmitBlockOpeningDelimiter with the given delimiter.
    /// A control block is the body of an loop or the true/false part of an if statement, for example.
    /// </summary>
    /// <param name="delimiter">A string representing the start of a control block.</param>
    public void EmitControlBlockOpeningDelimiter(string delimiter) {
      Contract.Requires(delimiter != null);

      if (this.PlaceControlBlockOpeningDelimitersOnNewLine && !this.atTheStartOfANewLine)
        this.EmitNewLine();
      this.EmitBlockOpeningDelimiter(delimiter);
    }

    /// <summary>
    /// If this.PlaceLambdaBodyOpeningDelimitersOnNewLine is true a new line is emitted, if necessary,
    /// before calling this.EmitBlockOpeningDelimiter with the given delimiter.
    /// </summary>
    /// <param name="delimiter">A string representing the start of a lambda body block.</param>
    public void EmitLambdaBodyOpeningDelimiter(string delimiter) {
      Contract.Requires(delimiter != null);

      if (this.PlaceLambdaBodyOpeningDelimitersOnNewLine && !this.atTheStartOfANewLine)
        this.EmitNewLine();
      this.EmitBlockOpeningDelimiter(delimiter);
    }

    /// <summary>
    /// If this.PlaceMethodBodyOpeningDelimitersOnNewLine is true a new line is emitted, if necessary,
    /// before calling this.EmitBlockOpeningDelimiter with the given delimiter.
    /// </summary>
    /// <param name="delimiter">A string representing the start of a method body block.</param>
    public void EmitMethodBodyOpeningDelimiter(string delimiter) {
      Contract.Requires(delimiter != null);

      if (this.PlaceMethodBodyOpeningDelimitersOnNewLine && !this.atTheStartOfANewLine)
        this.EmitNewLine();
      else
        this.EmitString(" ");
      this.EmitBlockOpeningDelimiter(delimiter);
    }

    /// <summary>
    /// If this.PlaceTypeBodyOpeningDelimitersOnNewLine is true a new line is emitted, if necessary,
    /// before calling this.EmitBlockOpeningDelimiter with the given delimiter.
    /// </summary>
    /// <param name="delimiter">A string representing the start of a type body block.</param>
    public void EmitTypeBodyOpeningDelimiter(string delimiter) {
      Contract.Requires(delimiter != null);

      if (this.PlaceTypeBodyOpeningDelimitersOnNewLine && !this.atTheStartOfANewLine) 
        this.EmitNewLine();
      this.EmitBlockOpeningDelimiter(delimiter);
    }

    /// <summary>
    /// If nothing has yet been written to the current line, write out this.IndentationLevel*this.IndentSize number of spaces.
    /// (Calling this again before first calling EmitNewLine, will have no effect.)
    /// </summary>
    private void IndentIfAtStartOfNewLine() {
      if (this.atTheStartOfANewLine) {
        for (int i = 0, n = this.IndentationLevel*this.IndentSize; i < n; i++)
          this.textWriter.Write(' ');
        this.atTheStartOfANewLine = false;
      }
    }

  }

}
