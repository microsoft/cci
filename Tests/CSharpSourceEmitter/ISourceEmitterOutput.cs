//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;

namespace CSharpSourceEmitter {
  public interface ISourceEmitterOutput {
    void WriteLine(string str, bool fIndent);
    void WriteLine(string str);
    void Write(string str, bool fIndent);
    void Write(string str);
    void IncreaseIndent();
    void DecreaseIndent();
  }

}
