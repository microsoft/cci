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

namespace SpecSharpTests {
  /// <summary>
  /// A unit test that invokes the Spec# command line host with a test suite
  /// </summary>
  [TestClass]
  public class UnitTest {
    [TestMethod]
    public void RunSuite() {
      StringBuilder testOuput = new StringBuilder();
      Console.SetOut(new StringWriter(testOuput));
      Stream resource = this.GetType().Assembly.GetManifestResourceStream("SpecSharpTests.suite");
      SpecSharpCommandLineHost.RunTestSuite("SpecSharpTests.suite", new StreamReader(resource));
      if (!string.Equals(testOuput.ToString(), "SpecSharpTests.suite passed\r\n")) {
        StreamWriter writer = File.CreateText("..\\..\\Failures.txt");
        writer.Write(testOuput);
        writer.Flush();
        Assert.Fail("See Failures.txt");
      }
    }

  }
}
