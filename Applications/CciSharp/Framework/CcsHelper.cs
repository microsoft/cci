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
using Microsoft.Cci.MutableCodeModel;

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
        /// <param name="attribute"></param>
        /// <returns></returns>
        public static bool TryGetAttributeByName(
            IEnumerable<ICustomAttribute> attributes,
            string attributeName,
            out ICustomAttribute attribute)
        {
            Contract.Requires(attributes != null);
            Contract.Requires(!String.IsNullOrEmpty(attributeName));
            Contract.Ensures(!Contract.Result<bool>() || Contract.ValueAtReturn(out attribute) != null);

            foreach (var a in attributes)
            {
                var type = a.Type as INamedEntity;
                if (type != null &&
                    String.Equals(type.Name.Value, attributeName, StringComparison.Ordinal))
                {
                    attribute = a;
                    return true;
                }
            }
            attribute = null;
            return false;
        }

        /// <summary>
        /// Tries to get the first field reference in the method body
        /// </summary>
        /// <param name="body"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public static bool TryGetFirstFieldReference(IMethodBody body, out IFieldReference field)
        {
            Contract.Requires(body != null);
            Contract.Ensures(!Contract.Result<bool>() || Contract.ValueAtReturn(out field) != null);

            foreach (var operation in body.Operations)
            {
                if (operation.OperationCode == OperationCode.Stfld ||
                    operation.OperationCode == OperationCode.Ldfld)
                {
                    field = (IFieldReference)operation.Value;
                    return field != null;
                }
            }

            field = null;
            return false;
        }

        /// <summary>
        /// Tries to extarct the value of a named argument and make it an expression
        /// </summary>
        /// <param name="attribute"></param>
        /// <param name="argumentName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static bool TryGetNamedArgumentValue(
            ICustomAttribute attribute,
            string argumentName,
            out CompileTimeConstant defaultValue)
        {
                Contract.Requires(attribute != null);
                Contract.Requires(!String.IsNullOrEmpty(argumentName));

                foreach(var namedArgument in attribute.NamedArguments)
                {
                    if (String.Equals(namedArgument.ArgumentName.Value, argumentName, StringComparison.Ordinal))
                    {
                        var valueExpression = namedArgument.ArgumentValue;
                        var value = valueExpression as IMetadataConstant;
                        if (value != null)
                        {
                            defaultValue = new CompileTimeConstant { Type = value.Type, Value = value.Value };
                            // good candidate
                            return true;
                        }
                    }
                }

                defaultValue = null;
                return false;
        }
    }
}
