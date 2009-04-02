//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;

namespace CSharpSourceEmitter {
  public class SourceEmitterOutputString : ISourceEmitterOutput {
    protected SourceEmitterContext sourceEmitterContext;
    protected StringBuilder strData;

    public SourceEmitterOutputString(SourceEmitterContext sourceEmitterContext) {
      this.sourceEmitterContext = sourceEmitterContext;
      strData = new StringBuilder();
    }

    public virtual void WriteLine(string str, bool fIndent) {
      if (fIndent)
        strData.Append(sourceEmitterContext.strIndent);

      strData.AppendLine(str);
    }

    public virtual void WriteLine(string str) {
      WriteLine(str, false);
    }

    public virtual void Write(string str, bool fIndent) {
      if (fIndent)
        strData.Append(sourceEmitterContext.strIndent);

      strData.Append(str);
    }

    public virtual void Write(string str) {
      Write(str, false);
    }

    public virtual void IncreaseIndent() {
      sourceEmitterContext.IncreaseIndent();
    }

    public virtual void DecreaseIndent() {
      sourceEmitterContext.DecreaseIndent();
    }

    public string Data {
      get { return strData.ToString(); }
    }

    public void ClearData() {
      strData = new StringBuilder();
    }

  }
}
