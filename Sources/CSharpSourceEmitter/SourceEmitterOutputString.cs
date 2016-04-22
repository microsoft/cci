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
