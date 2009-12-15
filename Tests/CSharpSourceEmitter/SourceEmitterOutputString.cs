//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace CSharpSourceEmitter {
  public class SourceEmitterOutputString : SourceEmitterOutputTextWriter {
    public SourceEmitterOutputString()
      : base(new StringWriter()) {
    }

    public SourceEmitterOutputString(int indentLevel)
      : base(new StringWriter(), indentLevel) {
    }

    public string Data {
      get {
        return ((StringWriter)this.outputWriter).ToString();
      }
    }

    public void ClearData() {
      var sw = (StringWriter)this.outputWriter;
      sw.Flush();
      sw.GetStringBuilder().Length = 0;
    }
  }
}
