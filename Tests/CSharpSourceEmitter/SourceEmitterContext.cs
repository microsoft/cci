//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;

namespace CSharpSourceEmitter {
  /// <summary>
  /// This class will contain formatting options for emitting code.
  /// </summary>
  public class SourceEmitterContext {
    public int nIndent = 0;
    public string strIndent = "";

    public virtual void IncreaseIndent() {
      nIndent += 4;

      strIndent = "";
      for (int i = 0; i < nIndent; i++)
        strIndent += " ";
    }

    public virtual void DecreaseIndent() {
      nIndent -= 4;

      strIndent = "";
      for (int i = 0; i < nIndent; i++)
        strIndent += " ";
    }

  }

}
