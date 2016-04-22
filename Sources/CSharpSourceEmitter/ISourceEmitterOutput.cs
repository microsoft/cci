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

namespace CSharpSourceEmitter {
  public interface ISourceEmitterOutput {
    void WriteLine(string str, bool fIndent);
    void WriteLine(string str);
    void Write(string str, bool fIndent);
    void Write(string str);
    void IncreaseIndent();
    void DecreaseIndent();

    /// <summary>
    /// Indicates whether anything has been written to the current line yet
    /// </summary>
    bool CurrentLineEmpty { get; }

    /// <summary>
    /// Invoked at the start of a new non-empty line, just before writing the indent
    /// </summary>
    event Action<ISourceEmitterOutput> LineStart;
  }

}
