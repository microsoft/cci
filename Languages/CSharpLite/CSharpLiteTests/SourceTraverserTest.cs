//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Cci.SpecSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Cci.Ast;
using System.Diagnostics;

namespace SpecSharpTests
{
    /// <summary>
    /// A unit test that invokes the Spec# command line host with a test suite
    /// </summary>
    [TestClass]
    public class SourceTraverserTest
    {
        private class MySourceTraverser : SourceTraverser
        {
            public MySourceTraverser()
            {
                visitMethodBodies = true;
            }
        }

        [TestMethod]
        [DeploymentItem("system.more")]
        public void TraverseSystemDotMore()
        {
            StringBuilder testOuput = new StringBuilder();
            Console.SetOut(new StringWriter(testOuput));

            File.Move("system.more", "system.exe");
            Process.Start("system").WaitForExit();
            var compiler = new MSBuildCompiler();
            var assembly = compiler.CompileProject(@"System\System.More.csproj");
            var visitor = new MySourceTraverser();
            visitor.Visit(assembly.Compilation);
        }

    }
}
