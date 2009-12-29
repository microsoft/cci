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
    public partial class LazyAutoPropertyTest
    {
        [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
        public class LazyAttribute : Attribute { }

        public class Foo
        {
            int value;
            [Lazy]
            public int Value
            {
                get { return this.value++; }
            }
        }

        [Fact]
        public void LazyProperty()
        {
            var foo = new Foo();

            var value = foo.Value;
            var nextValue = foo.Value;
            Assert.Equal(value, nextValue);
        }
    }
}
