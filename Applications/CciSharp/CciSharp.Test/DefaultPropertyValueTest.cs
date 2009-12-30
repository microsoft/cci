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
using System.ComponentModel;

namespace CciSharp.Test
{
    public partial class DefaultPropertyValueTest
    {
        public class Foo
        {
            [DefaultValue(10)]
            public int ValueGetPrivateSet { get; private set; }
            [DefaultValue(10)]
            public int ValueGetSet { get; set; }
        }

        [Fact]
        public void ValueGetPrivateSet()
        {
            Assert.True(new Foo().ValueGetPrivateSet == 10);
        }

        [Fact]
        public void ValueGetSet()
        {
            Assert.True(new Foo().ValueGetSet == 10);
        }
    }
}
