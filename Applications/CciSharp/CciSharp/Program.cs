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
                args.Length != 2)
            {
                Console.WriteLine("invalid arguments");
                Console.WriteLine("ccs <assembly path> <semi column separated list of mutator assemblies>");
                return CcsExitCodes.InvalidArguments;
            }

            try
            {
                var engine = new CcsEngine();
                return engine.Mutate(args[0], args[1].Split(';'));
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
