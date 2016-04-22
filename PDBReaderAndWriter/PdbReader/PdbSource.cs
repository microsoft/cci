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

namespace Microsoft.Cci.Pdb {
  internal class PdbSource {
    //internal uint index;
    internal string name;
    internal Guid doctype;
    internal Guid language;
    internal Guid vendor;
    internal Guid checksumAlgorithm;
    internal byte[] checksum;

    internal PdbSource(/*uint index, */string name, Guid doctype, Guid language, Guid vendor, Guid checksumAlgorithm, byte[] checksum) {
      //this.index = index;
      this.name = name;
      this.doctype = doctype;
      this.language = language;
      this.vendor = vendor;
      this.checksumAlgorithm = checksumAlgorithm;
      this.checksum = checksum;
    }
  }
}
