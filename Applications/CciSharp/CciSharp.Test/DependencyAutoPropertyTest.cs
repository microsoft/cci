//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.ComponentModel;
using Xunit;

namespace CciSharp.Test
{
    public partial class DependencyAutoPropertyTest
    {
        [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
        public class DependencyPropertyAttribute : Attribute { }

        class Foo : DependencyObject
        {
            [DependencyProperty]
            public int Value { get; set; }
            [DependencyProperty, DefaultValue(10)]
            public int ValueWithDefault { get; set; }
        }

        [Fact(Skip = "not ready")]
        public void ValueTest()
        {
            var foo = new Foo();
        }
    }
}
