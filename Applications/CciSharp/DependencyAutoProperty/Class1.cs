using System;
using System.Collections.Generic;
using CciSharp.Framework;
using Microsoft.Cci.MutableCodeModel;
using Microsoft.Cci;

namespace CciSharp.Mutators
{
    /// <summary>
    /// A mutator that instruments an autoproperty to implement the DependencyProperty pattern.
    /// </summary>
    public sealed class DependencyAutoProperty
        : CcsMutatorBase
    {
        public DependencyAutoProperty(ICcsHost host)
            :base(host, "Dependency Auto Property", 20, typeof(DependencyAutoPropertyResources))
        {
        }

        public override bool Visit(Assembly assembly, PdbReader pdbReader)
        {
            return false;
        }
    }
}
