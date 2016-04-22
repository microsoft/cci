//-----------------------------------------------------------------------------
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the Microsoft Public License.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//-----------------------------------------------------------------------------

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