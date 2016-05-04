// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Globalization;
using System.Diagnostics.Contracts;

namespace Microsoft.Cci {

    /// <summary>
    /// InternFactory + Caching for reusing
    /// </summary>
    public class CachingInternFactory : IInternFactory, ICachingFactory
    {
        IInternFactory m_factory;
        Dictionary<uint, Object> m_objects;

        public CachingInternFactory()
        {
            m_factory = new InternFactory();
            m_objects = new Dictionary<uint, object>();
        }

        public uint GetAssemblyInternedKey(AssemblyIdentity assemblyIdentity)
        {
            return m_factory.GetAssemblyInternedKey(assemblyIdentity);
        }

        public uint GetFieldInternedKey(IFieldReference fieldReference)
        {
            return m_factory.GetFieldInternedKey(fieldReference);
        }

        public uint GetFunctionPointerTypeReferenceInternedKey(CallingConvention callingConvention, IEnumerable<IParameterTypeInformation> parameters, IEnumerable<IParameterTypeInformation> extraArgumentTypes, IEnumerable<ICustomModifier> returnValueCustomModifiers, bool returnValueIsByRef, ITypeReference returnType)
        {
            return m_factory.GetFunctionPointerTypeReferenceInternedKey(callingConvention, parameters, extraArgumentTypes, returnValueCustomModifiers, returnValueIsByRef, returnType);
        }

        public uint GetGenericMethodParameterReferenceInternedKey(IMethodReference defininingMethodReference, int index)
        {
            return m_factory.GetGenericMethodParameterReferenceInternedKey(defininingMethodReference, index);
        }

        public uint GetGenericTypeInstanceReferenceInternedKey(ITypeReference genericTypeReference, IEnumerable<ITypeReference> genericArguments)
        {
            return m_factory.GetGenericTypeInstanceReferenceInternedKey(genericTypeReference, genericArguments);
        }

        public uint GetGenericTypeParameterReferenceInternedKey(ITypeReference definingTypeReference, int index)
        {
            return m_factory.GetGenericTypeParameterReferenceInternedKey(definingTypeReference, index);
        }

        public uint GetManagedPointerTypeReferenceInternedKey(ITypeReference targetTypeReferece)
        {
            return m_factory.GetManagedPointerTypeReferenceInternedKey(targetTypeReferece);
        }

        public uint GetMatrixTypeReferenceInternedKey(ITypeReference elementTypeReference, int rank, IEnumerable<ulong> sizes, IEnumerable<int> lowerBounds)
        {
            return m_factory.GetMatrixTypeReferenceInternedKey(elementTypeReference, rank, sizes, lowerBounds);
        }

        public uint GetMethodInternedKey(IMethodReference methodReference)
        {
            return m_factory.GetMethodInternedKey(methodReference);
        }

        public uint GetModifiedTypeReferenceInternedKey(ITypeReference typeReference, IEnumerable<ICustomModifier> customModifiers)
        {
            return m_factory.GetModifiedTypeReferenceInternedKey(typeReference, customModifiers);
        }

        public uint GetModuleInternedKey(ModuleIdentity moduleIdentity)
        {
            return m_factory.GetModuleInternedKey(moduleIdentity);
        }

        public uint GetNamespaceTypeReferenceInternedKey(IUnitNamespaceReference containingUnitNamespace, IName typeName, uint genericParameterCount)
        {
            return m_factory.GetNamespaceTypeReferenceInternedKey(containingUnitNamespace, typeName, genericParameterCount);
        }

        public uint GetNestedTypeReferenceInternedKey(ITypeReference containingTypeReference, IName typeName, uint genericParameterCount)
        {
            return m_factory.GetNestedTypeReferenceInternedKey(containingTypeReference, typeName, genericParameterCount);
        }

        public uint GetPointerTypeReferenceInternedKey(ITypeReference targetTypeReference)
        {
            return m_factory.GetPointerTypeReferenceInternedKey(targetTypeReference);
        }

        public uint GetTypeReferenceInternedKey(ITypeReference typeReference)
        {
            return m_factory.GetTypeReferenceInternedKey(typeReference);
        }

        public uint GetVectorTypeReferenceInternedKey(ITypeReference elementTypeReference)
        {
            return m_factory.GetVectorTypeReferenceInternedKey(elementTypeReference);
        }


        /// <summary>
        /// Caching GenericTypeInstanceRefernece objects based on interned key, avoiding GenericTypeInstance.InitializeIfNecessary expense
        /// </summary>
        public IGenericTypeInstanceReference GetOrMakeGenericTypeInstanceReference(INamedTypeReference genericTypeReference, IEnumerable<ITypeReference> genericArguments)
        {
            InternFactory factory = m_factory as InternFactory;
            if (factory != null && !factory.InternKeysAreReliablyUnique)
            {
                return new Microsoft.Cci.Immutable.GenericTypeInstanceReference(genericTypeReference, genericArguments, this, true);
            }

            uint key = m_factory.GetGenericTypeInstanceReferenceInternedKey(genericTypeReference, genericArguments);

            IGenericTypeInstanceReference type = null;

            object value;

            if (m_objects.TryGetValue(key, out value))
            {
                type = value as IGenericTypeInstanceReference;

                if (type != null && !SequenceEquals(genericArguments, type.GenericArguments))
                {
                    // We can currently get problematic cache hits here for different objects representing the same type.
                    // e.g. SignatureGenericTypeParameter from ref signature can substitute for GenericTypeParameter
                    // from def signature. This breaks assumptions that were there prior to the sharing of generic
                    // type instances that was introduced by the caching intern factory. In that particular case, it breaks
                    // the subsequent specialization of the type parameter.
                    //
                    // We should investigate how to share more in these cases, but in the meantime, we conservatively
                    // only use an existing instantiation if the genericArguments are identical object references and 
                    // otherwise force a cache miss here.
                    type = null;
                }
            }

            if (type == null)
            {
                type = new Microsoft.Cci.Immutable.GenericTypeInstanceReference(genericTypeReference, genericArguments, this, true);
                m_objects[key] = type;
            }

            return type;
        }

        private static bool SequenceEquals(IEnumerable<ITypeReference> leftSequence, IEnumerable<ITypeReference> rightSequence)
        {
            // NOTE: Adapter() is used to prevent temporary allocation for common IReadOnlyList<T> case.
            var leftEnumerator = leftSequence.Adapter().GetEnumerator();
            var rightEnumerator = rightSequence.Adapter().GetEnumerator();

            while (leftEnumerator.MoveNext())
            {
                if (!rightEnumerator.MoveNext())
                {
                    Debug.Assert(false, "Count mismatch not expected.");
                    return false;
                }

                ITypeReference left = leftEnumerator.Current;
                ITypeReference right = rightEnumerator.Current;
                Debug.Assert(left.InternedKey == right.InternedKey);

                if (left != right)
                {
                    return false;
                }
            }

            if (rightEnumerator.MoveNext())
            {
                Debug.Assert(false, "Count mismatch not expected.");
                return false;
            }

            return true;
        }

        public void FlushCache()
        {
            m_objects = new Dictionary<uint, object>();
        }

        public void Cleanup()
        {
            m_factory = null;
            m_objects = null;
        }
    }
}
