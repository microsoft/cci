//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Cci {
  internal sealed class MemoryStream {

    internal MemoryStream() {
      this.Buffer = new byte[64];
      this.Length = 0;
      this.position = 0;
    }

    internal MemoryStream(uint initialSize) 
      //^ requires initialSize > 0;
    {
      this.Buffer = new byte[initialSize];
      this.Length = 0;
      this.position = 0;
    }

    internal byte[] Buffer;
    //^ invariant Buffer.LongLength > 0;

    private void Grow(byte[] myBuffer, uint n, uint m)
      //^ requires n > 0;
    {
      ulong n2 = n*2;
      if (n2 == 0) n2 = 16;
      while (m >= n2) n2 = n2*2;
      byte[] newBuffer = this.Buffer = new byte[n2];
      for (int i = 0; i < n; i++)
        newBuffer[i] = myBuffer[i];
    }

    internal uint Length;

    internal uint Position {
      get { 
        return this.position; 
      }
      set {
        byte[] myBuffer = this.Buffer;
        uint n = (uint)myBuffer.LongLength;
        if (value >= n) this.Grow(myBuffer, n, value);
        if (value > this.Length) this.Length = value;
        this.position = value;
      }
    }
    private uint position;

    internal byte[] ToArray() {
      uint n = this.Length;
      byte[] source = this.Buffer;
      if (source.Length == n) return this.Buffer;
      byte[] result = new byte[n];
      for (int i = 0; i < n; i++)
        result[i] = source[i];
      return result;
    }

    internal void Write(byte[] buffer, uint index, uint count)  {
      uint p = this.position;
      this.Position = p + count;
      byte[] myBuffer = this.Buffer;
      for (uint i = 0, j = p, k = index; i < count; i++)
        myBuffer[j++] = buffer[k++];
    }

    internal void WriteTo(MemoryStream stream) {
      stream.Write(this.Buffer, 0, this.Length);
    }

    internal void WriteTo(System.IO.Stream stream) {
      stream.Write(this.Buffer, 0, (int)this.Length);
    }
  }
}
