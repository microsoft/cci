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
            [DefaultValue(100)]
            public int ValueGetSet { get; set; }
            [DefaultValue(1000), ReadOnly(true)]
            public int ReadonlyValueGetPrivateSet { get; private set; }
        }

        [Fact]
        public void ValueGetPrivateSet()
        {
            Assert.True(new Foo().ValueGetPrivateSet == 10);
        }

        [Fact]
        public void ValueGetSet()
        {
            Assert.True(new Foo().ValueGetSet == 100);
        }

        [Fact]
        public void ReadonlyValueGetPrivateSet()
        {
            Assert.True(new Foo().ReadonlyValueGetPrivateSet == 1000);
        }
    }
}
