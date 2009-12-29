//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics.Contracts;
using Microsoft.Cci;

namespace CciSharp.Framework
{
    /// <summary>
    /// Various useful methods when building mutators
    /// </summary>
    public static class CcsHelper
    {
        /// <summary>
        /// Tries to find an attribue by name
        /// </summary>
        /// <param name="attributes"></param>
        /// <param name="attributeName"></param>
        /// <param name="lazyAttribute"></param>
        /// <returns></returns>
        public static bool TryGetAttributeByName(
            IEnumerable<ICustomAttribute> attributes, 
            string attributeName,
            out ICustomAttribute lazyAttribute)
        {
            Contract.Requires(attributes != null);
            Contract.Requires(!String.IsNullOrEmpty(attributeName));
            foreach (var attribute in attributes)
            {
                var type = attribute.Type as INamedEntity;
                if (type != null &&
                    String.Equals(type.Name.Value, attributeName, StringComparison.Ordinal))
                {
                    lazyAttribute = attribute;
                    return true;
                }
            }
            lazyAttribute = null;
            return false;
        }
    }
}
