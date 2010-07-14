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
    public partial class NotNullTest
    {
        [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple =false, Inherited = false)]
        public sealed class NotNullAttribute : Attribute{}

        static void ParameterNotNull([NotNull]object o)
        {
        }

        [Fact(Skip = "not ready yet")]
        public static void ParameterNotNullTest()
        {
            try
            {
                ParameterNotNull(null);
                Assert.True(false, "expected to fail");
            }
            catch
            {
            }
        }

        [Fact(Skip = "not ready yet")]
        public static void TypeParameterNotNullTest()
        {
            try
            {
                new FooNotNull().ParameterNotNull(null);
                Assert.True(false, "expected to fail");
            }
            catch
            {
            }
        }

        [return: NotNull]
        static object ResultNotNull()
        {
            return null;
        }


        [Fact(Skip = "not ready yet")]
        public static void ResultNotNullTest()
        {
            try
            {
                var value = ResultNotNull();
                Assert.True(value != null);
            }
            catch
            {
            }
        }

        [Fact(Skip = "not ready yet")]
        public static void TypeResultNotNullTest()
        {
            try
            {
                var value = new FooNotNull().ResultNotNull();
                Assert.True(value != null);
            }
            catch
            {
            }
        }

        [NotNull]
        class FooNotNull
        {
            public void ParameterNotNull(object o)
            { }
            public object ResultNotNull()
            {
                return null;
            }
        }
    }
}
