// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Cci;
using Microsoft.Cci.MetadataReader.ObjectModelImplementation;
using System;
using System.Collections.Generic;
namespace Microsoft.Cci.MetadataReader.Extensions
{
    public static class ModuleExtensions
    {
        public static IEnumerable<ITypeMemberReference> GetConstructedTypeInstanceMembers(this IModule module)
        {
            if (!(module is Module))
                throw new NotSupportedException("An IModule created using PEReader is required.");

            var readerModule = module as Module;
            return readerModule.GetConstructedTypeInstanceMembers();
        }
    }
}