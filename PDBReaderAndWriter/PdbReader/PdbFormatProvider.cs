// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;

namespace Microsoft.Cci.Pdb {
  /// <summary>
  /// Portable PDB provider.
  /// </summary>
  internal static class PdbFormatProvider {

    /// <summary>
    /// Detect whether a given stream contains portable or Windows PDB format data
    /// and deserialize CCI PDB information from the given format.
    /// </summary>
    /// <param name="module">IL module (needed only for portable PDB's)</param>
    /// <param name="pdbInputStream">PDB stream to read</param>
    /// <param name="tokenToSourceMapping">Output mapping from function tokens to line numbers</param>
    /// <param name="sourceServerData">Source server data (currently supported for Windows PDB only)</param>
    /// <param name="debugInformationVersion">Debug info version (currently supported for Windows PDB only)</param>
    public static IEnumerable<PdbFunction> LoadFunctions(
      IModule module,
      Stream pdbInputStream,
      out Dictionary<uint, PdbTokenLine> tokenToSourceMapping,
      out string sourceServerData,
      out string debugInformationVersion)
    {
      int age;
      Guid guid;

      // Load CCI data from Windows PDB
      IEnumerable<PdbFunction> result = PdbFile.LoadFunctions(
        pdbInputStream,
        out tokenToSourceMapping,
        out sourceServerData,
        out age,
        out guid);

      var guidHex = guid.ToString("N");
      string ageHex = age.ToString("X");

      debugInformationVersion = guidHex + ageHex;

      return result;
    }
  }
}
