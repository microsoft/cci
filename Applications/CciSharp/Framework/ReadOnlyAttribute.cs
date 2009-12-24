//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;

namespace CciSharp.Framework
{
    /// <summary>
    /// Attribute to specify that an auto property is readonly
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class ReadOnlyAttribute : Attribute
    {
    }
}
