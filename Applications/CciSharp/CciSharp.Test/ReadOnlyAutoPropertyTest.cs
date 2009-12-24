using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace CciSharp.Test
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class ReadOnlyAttribute : Attribute
    {
    }

    public partial class ReadOnlyAutoPropertyTest
    {
        [Fact(Skip = "not ready yet")]
        public void PropertyIsReadonly()
        {
            Assert.False(!typeof(CProperty).GetProperty("Value").CanWrite);
            Assert.False(!typeof(SProperty).GetProperty("Value").CanWrite);
        }

        class CProperty
        {
            public CProperty(object value)
            {
                this.Value = value;
            }
            [ReadOnly]
            public object Value { get; private set; }
        }
        class SProperty
        {
            public SProperty(int value)
            {
                this.Value = value;
            }
            [ReadOnly]
            public int Value { get; private set; }
        }
    }
}
