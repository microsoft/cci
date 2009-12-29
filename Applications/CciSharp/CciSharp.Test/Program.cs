//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace CciSharp.Test
{
    class Program
    {
        public static int Main(string[] args)
        {
            var newArgs = new List<string>(args.Length + 1);
            newArgs.Add(typeof(Program).Assembly.Location);
            if (args != null)
                newArgs.AddRange(args);
            try
            {
                return Xunit.ConsoleClient.Program.Main(newArgs.ToArray());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return -1000;
            }
        }
    }
}
