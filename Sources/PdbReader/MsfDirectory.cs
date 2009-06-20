//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;

namespace Microsoft.Cci.Pdb {
  internal class MsfDirectory {
    internal MsfDirectory(PdbReader reader, PdbFileHeader head, BitAccess bits) {
      int pages = reader.PagesFromSize(head.directorySize);

      // 0..n in page of directory pages.
      bits.MinCapacity(head.directorySize);
      int directoryRootPages = head.directoryRoot.Length;
      int pagesPerPage = head.pageSize / 4;
      for (int i = 0; i < directoryRootPages; i++) {
        int pagesInThisPage = (i == directoryRootPages - 1) ? pages % pagesPerPage : pagesPerPage;
        reader.Seek(head.directoryRoot[i], 0);
        bits.Append(reader.reader, pagesInThisPage * 4);
      }
      bits.Position = 0;

      DataStream stream = new DataStream(head.directorySize, bits, pages);
      bits.MinCapacity(head.directorySize);
      stream.Read(reader, bits);

      // 0..3 in directory pages
      int count;
      bits.ReadInt32(out count);

      // 4..n
      int[] sizes = new int[count];
      bits.ReadInt32(sizes);

      // n..m
      streams = new DataStream[count];
      for (int i = 0; i < count; i++) {
        if (sizes[i] <= 0) {
          streams[i] = new DataStream();
        } else {
          streams[i] = new DataStream(sizes[i], bits,
                                      reader.PagesFromSize(sizes[i]));
        }
      }
    }

    internal DataStream[] streams;
  }

}
