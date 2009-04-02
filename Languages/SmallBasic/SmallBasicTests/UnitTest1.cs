//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Cci.SmallBasic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SmallBasicTests {
  /// <summary>
  /// A unit test that invokes the SmallBasic command line host with a test suite
  /// </summary>
  [TestClass]
  public class UnitTest {
    [TestMethod]
    public void RunSmallBasicSuite() {
      StringBuilder testOuput = new StringBuilder();
      Console.SetOut(new StringWriter(testOuput));
      Stream resource = this.GetType().Assembly.GetManifestResourceStream("SmallBasicTests.suite");
      SmallBasicCommandLineHost.RunSuite("SmallBasicTests.suite", new StreamReader(resource));
      if (!string.Equals(testOuput.ToString(), "SmallBasicTests.suite passed\r\n")) {
        StreamWriter writer = File.CreateText("..\\..\\Failures.txt");
        writer.Write(testOuput);
        writer.Flush();
        Assert.Fail("See Failures.txt");
      }
    }

  }
}
