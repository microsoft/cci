//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;

namespace CSharpSourceEmitter {
  public class SourceEmitterOutputConsole : ISourceEmitterOutput {
    protected SourceEmitterContext sourceEmitterContext;

    public SourceEmitterOutputConsole(SourceEmitterContext sourceEmitterContext) {
      this.sourceEmitterContext = sourceEmitterContext;
    }

    public virtual void WriteLine(string str, bool fIndent) {
      if (fIndent)
        Console.Write(sourceEmitterContext.strIndent);

      Console.WriteLine(str);
    }

    public virtual void WriteLine(string str) {
      WriteLine(str, false);
    }

    public virtual void Write(string str, bool fIndent) {
      if (fIndent)
        Console.Write(sourceEmitterContext.strIndent);

      Console.Write(str);
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

  }
}
