// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Cci.MetadataReader.ObjectModelImplementation;
using System;

namespace Microsoft.Cci.MetadataReader.Extensions
{
  public static class AliasForTypeExtensions
  {
    /// <summary>
    /// Helper to step through one indirection of IAliasForType.
    /// The method gets the type reference on the target of the alias
    /// and if that target is another alias it also returns its IAliasForType.
    /// This can be used to step over aliases one by one, as opposed to IAliasForType.AliasedType
    /// which sometimes resolves all the way through to the type def.
    /// It also provides a way to get to the target's IAliasForType which is otherwise
    /// really hard to get to (a simple type ref pointing to it is not enough, it will still report itself as non-alias).
    /// </summary>
    /// <param name="aliasForType">The alias to step through.</param>
    /// <param name="referencedAliasForType">If the target points to another alias this will contain IAliasForType for it.</param>
    /// <returns>The type ref for the target of the source alias. The type ref can be resolved, but its IsAlias property can't be trusted
    /// as it will always return false.</returns>
    public static INamedTypeReference GetUnresolvedAliasedTypeReference(this IAliasForType aliasForType, out IAliasForType referencedAliasForType)
    {
      referencedAliasForType = null;

      var exportedAliasForType = aliasForType as ExportedTypeAliasBase;
      if (exportedAliasForType == null)
        throw new NotSupportedException("An IAliasForType created using PEReader is required.");

      IMetadataReaderNamedTypeReference typeReference = exportedAliasForType.UnresolvedAliasedTypeReference as IMetadataReaderNamedTypeReference;
      if (typeReference == null)
        throw new NotSupportedException("A type reference created using PEReader is required.");

      var internalAssembly = typeReference.ModuleReference.ResolvedUnit as Assembly;
      if (internalAssembly != null)
      {
        // Try if the other assembly has this as yet another type alias, since there's no good way of getting from a normal type reference
        // to the alias. This is basically the only way to do that.
        referencedAliasForType = internalAssembly.PEFileToObjectModel.TryToResolveAsNamespaceTypeAlias(typeReference.NamespaceFullName, typeReference.MangledTypeName);
      }

      return typeReference;
    }
  }
}
