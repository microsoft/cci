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

namespace CSharpSourceEmitter {
  public class SourceEmitterOutputTextWriter : ISourceEmitterOutput {

    public SourceEmitterOutputTextWriter(TextWriter outputWriter, int indentSize) {
      this.outputWriter = outputWriter;
      this.indentLevel = indentSize;
      this.strIndent = "";
      this.CurrentLineEmpty = true;
    }

    public SourceEmitterOutputTextWriter(TextWriter outputWriter) : this(outputWriter, 4)
    {
    }

    public virtual void WriteLine(string str, bool fIndent) {
      OutputBegin(fIndent);
      outputWriter.WriteLine(str);
      this.CurrentLineEmpty = true;
    }

    public virtual void WriteLine(string str) {
      WriteLine(str, false);
    }

    public virtual void Write(string str, bool fIndent) {
      OutputBegin(fIndent);
      outputWriter.Write(str);
      this.CurrentLineEmpty = false;
    }

    public virtual void Write(string str) {
      Write(str, false);
    }

    public virtual void IncreaseIndent() {
      int newIndent = strIndent.Length + indentLevel;
      strIndent = new String(' ', newIndent);
    }

    public virtual void DecreaseIndent() {
      int newIndent = strIndent.Length - indentLevel;
      strIndent = new String(' ', newIndent);
    }

    public bool CurrentLineEmpty {
      get;
      private set;
    }

    public event Action<ISourceEmitterOutput> LineStart;

    protected virtual void OutputBegin(bool fIndent)
    {
      if (fIndent) {
        if (LineStart != null)
          LineStart(this);
        outputWriter.Write(strIndent);
      }
    }

    protected TextWriter outputWriter;
    protected string strIndent;
    protected readonly int indentLevel;
  }

  public class SourceEmitterOutputConsole : SourceEmitterOutputTextWriter {
    public SourceEmitterOutputConsole(int indentSize) :
      base(System.Console.Out, indentSize)
    {
    }

    public SourceEmitterOutputConsole() : 
      base(System.Console.Out)
    {
    }
  }
}
