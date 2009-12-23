//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace CciSharp.Test
{
    public class AssertMessageTest
    {
        private void AssertTrue(bool value)
        {
        }

        [Fact]
        public void AddMessageToTrue()
        {
            int x = 3;
            try
            {
                Assert.True(x != 5);
            }
            catch (Exception ex)
            {
                Assert.True(ex.Message.Contains("x != 5"), ex.Message + "does not contain the expression");
            }
        }
    }
}
