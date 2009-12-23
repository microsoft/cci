//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;

namespace CciSharp
{
    class Program
    {
        static int Main(string[] args)
        {
            Console.WriteLine("CCISharp: A pluggable post compiler for .NET");

            if (args == null ||
                args.Length != 1)
            {
                Console.WriteLine("invalid arguments");
                return CcsExitCodes.InvalidArguments;
            }

            try
            {
                var engine = new CcsEngine();
                return engine.Mutate(args[0]);
            }
            catch (Exception ex)
            {
                Console.WriteLine("unexpected error: {0}", ex.Message);
                Console.WriteLine(ex);
                return CcsExitCodes.UnexpectedException;
            }
        }
    }
}
