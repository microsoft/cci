// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Cci.WriterUtilities {
  public sealed class MemoryStream {

    public MemoryStream() {
      this.Buffer = new byte[64];
    }

    public MemoryStream(uint initialSize)
      //^ requires initialSize > 0;
    {
      this.Buffer = new byte[initialSize];
    }

    public byte[] Buffer;
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

    public uint Length;

    public uint Position {
      get {
        return this.position;
      }
      set {
        byte[] myBuffer = this.Buffer;
#if COMPACTFX // || COREFX_SUBSET
        uint n = (uint)myBuffer.Length;
#else
        uint n = (uint)myBuffer.LongLength;
#endif
        if (value >= n) this.Grow(myBuffer, n, value);
        if (value > this.Length) this.Length = value;
        this.position = value;
      }
    }
    private uint position;
    private bool reusable = true;

    public byte[] ToArray() {
      uint n = this.Length;
      byte[] source = this.Buffer;
      if (source.Length == n)
      {
        reusable = false; // buffer returned to caller, can't reuse the stream anymore
        return this.Buffer;
      }
      byte[] result = new byte[n];
      for (int i = 0; i < n; i++)
        result[i] = source[i];
      return result;
    }

    public void Write(byte[] buffer, uint index, uint count) {
      uint p = this.position;
      this.Position = p + count;
      byte[] myBuffer = this.Buffer;
      for (uint i = 0, j = p, k = index; i < count; i++)
        myBuffer[j++] = buffer[k++];
    }

    public void WriteTo(MemoryStream stream) {
      stream.Write(this.Buffer, 0, this.Length);
    }

    public void WriteTo(System.IO.Stream stream) {
      stream.Write(this.Buffer, 0, (int)this.Length);
    }

    internal bool ClearForReuse()
    {
        this.position = 0;
        this.Length = 0;

        return this.reusable;
    }
  }

  internal static class BinaryWriterCache
  {
      [ThreadStatic]
      private static BinaryWriter ts_CachedWriter1;

      [ThreadStatic]
      private static BinaryWriter ts_CachedWriter2;

      private static uint s_Capacity = 1024;

      /// <summary>
      /// Acquire a BinaryWriter
      /// </summary>
      public static BinaryWriter Acquire(bool unicode = false)
      {
          BinaryWriter writer = ts_CachedWriter1;

          if (writer != null)
          {
              ts_CachedWriter1 = null;
              writer.SetUnicode(unicode);

              return writer;
          }

          writer = ts_CachedWriter2;

          if (writer != null)
          {
              ts_CachedWriter2 = null;
              writer.SetUnicode(unicode);

              return writer;
          }

          // Create new one
          return new BinaryWriter(new MemoryStream(s_Capacity), unicode);
      }

      /// <summary>
      /// Release BinaryWriter to cache
      /// </summary>
      public static void ReleaseToCache(this BinaryWriter writer)
      {
          if (writer.BaseStream.ClearForReuse())
          {
              if (ts_CachedWriter1 == null)
                ts_CachedWriter1 = writer;
              else
                ts_CachedWriter2 = writer; 
          }
      }

      /// <summary>
      /// Convert to array and release BinaryWriter to cache
      /// </summary>
      public static byte[] ToArrayAndRelease(this BinaryWriter writer)
      {
          byte[] result = writer.BaseStream.ToArray();

          writer.ReleaseToCache();

          return result;
      }

  }

  /// <summary>
  /// List of MemoryStream, avoiding growing huge MemoryStream in LOH, breaking it into parts
  /// </summary>
  internal class MemoryStreamList
  {
      const int BlockSize = 64 * 1024;

      MemoryStream m_stream;
      BinaryWriter m_writer;

      List<MemoryStream> m_flushedStreams;
      uint m_flushedLength;

      public MemoryStreamList()
      {
          m_stream = new MemoryStream(BlockSize);
          m_writer = new BinaryWriter(m_stream);
      }

      public BinaryWriter Writer
      {
          get { return m_writer; }
      }

      public uint Length
      {
          get { return m_flushedLength + m_stream.Length; }
      }

      public uint Position
      {
          get { return m_flushedLength + m_stream.Position; }
      }

      /// <summary>
      /// If stream is 32-bit aligned and having less than 10% unused space, store it and start a new stream
      /// </summary>
      public void CheckFlush()
      {
          uint len = m_stream.Length;

          if (((len & 3) == 0) && (len >= (m_stream.Buffer.Length * 9 / 10)))
          {
              if (m_flushedStreams == null)
              {
                  m_flushedStreams = new List<MemoryStream>();
              }

              // Put into flushed stream list
              m_flushedStreams.Add(m_stream);
              m_flushedLength += m_stream.Length;

              // Create a new stream for writing
              m_stream = new MemoryStream(BlockSize);

              m_writer.BaseStream = m_stream;
          }
      }

      public void WriteTo(System.IO.Stream s)
      {
          if (m_flushedStreams != null)
          {
              for (int i = 0; i < m_flushedStreams.Count; i++)
              {
                  m_flushedStreams[i].WriteTo(s);
              }
          }

          m_stream.WriteTo(s);
      }
  }

    
}
