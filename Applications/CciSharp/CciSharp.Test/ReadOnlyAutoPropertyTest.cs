using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using System.ComponentModel;

namespace CciSharp.Test
{
    public partial class ReadOnlyAutoPropertyTest
    {
        [Fact]
        public void CPropertyIsReadonly()
        {
            Assert.False(typeof(CProperty).GetProperty("Value").CanWrite,"setter should be removed");
        }

        [Fact]
        public void SPropertyIsReadonly()
        {
            Assert.False(typeof(SProperty).GetProperty("Value").CanWrite,"setter should be removed");
        }

        class CProperty
        {
            public CProperty(object value)
            {
                this.Value = value;
            }
            [ReadOnly(true)]
            public object Value
            {
                get;
                private set;
            }
        }
        class SProperty
        {
            public SProperty(int value)
            {
                this.Value = value;
            }
            [ReadOnly(true)]
            public int Value
            {
                get;
                private set;
            }
        }
    }
}
