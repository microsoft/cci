using System;
using System.Collections.Generic;
using System.Text;
using CciSharp.Framework;
using Microsoft.Cci.MutableCodeModel;

namespace CciSharp.ReadOnlyAutoProperty
{
    /// <summary>
    /// Turns an auto property annotated with a [ReadOnly] attribute
    /// and whose setter is private, into a property with a getter
    /// and a readonly backing field.
    /// </summary>
    /// <remarks>
    /// The [ReadOnly] attribute namespace and assembly does not matter.
    /// </remarks>
    public sealed class ReadOnlyAutoPropertyMutator
        : CcsMutatorBase
    {
        public ReadOnlyAutoPropertyMutator(ICcsHost host)
            : base(host, "ReadOnly Auto Property", 1)
        { }

        public override void Visit(Module module)
        {
            // pass1: collect properties to mutate and field references,
            // pass2: mutate properties and update field references
        }
    }
}
