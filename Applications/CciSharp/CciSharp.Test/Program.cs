//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CciSharp.Test
{
    class Program
    {
        public static int Main(string[] args)
        {
            var newArgs = new List<string>();
            newArgs.Add(typeof(Program).Assembly.Location);
            if (args != null)
                newArgs.AddRange(args);
            return Xunit.ConsoleClient.Program.Main(newArgs.ToArray());
        }
    }
}
