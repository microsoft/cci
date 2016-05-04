// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Diagnostics;

// Set of helper extension methods and types to unify the full .NET and Core .NET surface areas

namespace System.Runtime.InteropServices
{
    // UnmanagedType.CustomMarshaler got removed from Core FX - define the old value here
    static class UnmanagedTypeEx
    {
        public const UnmanagedType CustomMarshaler = (UnmanagedType)44;
    }
}

namespace Microsoft.Cci
{
    static class ConvertExtensions
    {
        // Convert.GetTypeCode was removed from CoreFX - have an extension method
        public static TypeCode ConvertGetTypeCode(this object value)
        {
            if (value == null) return TypeCode.Empty;
            IConvertible temp = value as IConvertible;
            if (temp != null)
            {
                return temp.GetTypeCode();
            }
            return TypeCode.Object;
        }
    }
}

#if COREFX_CONTRACTS

// These are constants/attributes that got removed from Core FX, but the CCI code refers to them

namespace System.Collections.Generic
{
    static class MyExtensions
    {
        public static IReadOnlyList<T> AsReadOnly<T>(this List<T> This) { return new System.Collections.ObjectModel.ReadOnlyCollection<T>(This); }
    }
}

namespace System.Security
{
    class SuppressUnmanagedCodeSecurityAttribute : Attribute { }
}

#endif
