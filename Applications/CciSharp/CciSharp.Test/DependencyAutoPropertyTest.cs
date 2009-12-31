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
        public class DependencyPropertyAttribute : Attribute {
            public DependencyPropertyAttribute() { }
            public object DefaultValue { get; set; }
            public bool Validate { get; set; }
        }

        class Foo : DependencyObject
        {
            [DependencyProperty]
            public int Value { get; set; }
            [DependencyProperty(DefaultValue = 10)]
            public int ValueWithDefault { get; set; }
            [DependencyProperty(Validate = true)]
            public int ValueWithValidation { get; set; }
            static bool ValidateValueWithValidation(int value)
            {
                return true;
            }
        }

        [Fact(Skip = "requires WPF message pump")]
        public void ValueTest()
        {
            var foo = new Foo();
            foo.Value = 100;
            var value = foo.Value;
            Assert.True(100 == value);
        }
    }
}
