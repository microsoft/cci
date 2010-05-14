//-----------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.  All Rights Reserved.
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

namespace GenericResolutionTests {
  class Program {
    static int Main(string[] args) {
      var newArgs = new List<string>();
      newArgs.Add(typeof(Program).Assembly.Location);
      if (args != null)
        newArgs.AddRange(args);
      return Xunit.ConsoleClient.Program.Main(newArgs.ToArray());
    }
  }
}
