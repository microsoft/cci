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
using System.Diagnostics.Contracts;
using Xunit.Sdk;

namespace CciSharp.Test
{
    public partial class LazyAutoPropertyTest
    {
        [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
        public class LazyAttribute : Attribute { }

        public class Foo
        {
            [Lazy]
            public int Value
            {
                get { return Environment.TickCount; }
            }

            [Lazy]
            public object ValueWithContracts
            {
                get
                {
                    Contract.Ensures(Contract.Result<object>() != null);
                    return null; // should always fail
                }
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

        [Fact(Skip = "decompiler bug")]
        [ContractThrows]
        public void LazyWithContractsPropertyBug()
        {
            var foo = new Foo();

            try
            {
                var value = foo.ValueWithContracts;
                Assert.True(false, "contract ensures should have triggered");
            }
            catch { }
        }

        [Fact]
        [ContractThrows]
        public void LazyWithContractsProperty()
        {
            var foo = new Foo();

            try
            {
                var value = foo.ValueWithContracts;
                throw new InvalidOperationException();
            }
            catch (Exception ex) {
                var assertException = ex.InnerException as AssertException;
                if (assertException == null)
                    throw;
                Console.WriteLine(ex.ToString()); }
        }
    }
}
