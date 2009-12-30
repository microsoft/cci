//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using CciSharp.Framework;
using Microsoft.Cci.MutableCodeModel;
using Microsoft.Cci;

namespace CciSharp.Mutators
{
    /// <summary>
    /// Assigns the value in [DefaultValue(...)] in all constructors for auto-properties
    /// </summary>
    public sealed class DefaultAutoPropertyValue
        : CcsMutatorBase
    {
        public DefaultAutoPropertyValue(ICcsHost host)
            : base(host, "DefaultValue", 5, typeof(DefaultAutoPropertyValueResources))
        { }

        public override bool Visit(Assembly assembly, PdbReader pdbReader)
        {
            return false;
        }
    }
}
