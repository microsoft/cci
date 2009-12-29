using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using System.ComponentModel;

namespace CciSharp.Test
{
    public partial class NotifyPropertyChangedTest
    {
        [Fact(Skip = "not ready")]
        public void Changed()
        {
            var target = new Target();
            bool called= false;
            target.PropertyChanged += (sender, p) =>
                {
                    called = true;
                    Assert.Equal(p.PropertyName, "Value");
                };
            target.Value = 10;
            Assert.True(called, "should have been called");
        }

        [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
        public class NotifyChangedAttribute : Attribute { }

        public class Target : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            [NotifyChanged]
            public int Value { get; set; }

            public void Goal(int value)
            {
                bool changed = Object.Equals(this.Value, value);
                this.Value = value;
                if (changed)
                {
                    var eh = this.PropertyChanged;
                    if (eh != null)
                        eh(this, new PropertyChangedEventArgs("Value"));
                }
            }
        }

        public class ExplicitTarget : INotifyPropertyChanged
        {
            event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
            {
                add { }
                remove { }
            }

            [NotifyChanged]
            public int Value { get; set; }
        }
    }
}
