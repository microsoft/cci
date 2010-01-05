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
    public partial class WeakLazyAutoPropertyTest
    {
        [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
        public class WeakLazyAttribute : Attribute { }

        public class Foo
        {
            [WeakLazy]
            public string Value
            {
                get { return Environment.TickCount.ToString(); }
            }
        }

        [Fact]
        public void WeakLazyProperty()
        {
            var foo = new Foo();

            var value = foo.Value;
            var nextValue = foo.Value;
            Assert.Equal(value, nextValue);
        }
    }
}
