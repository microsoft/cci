// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
