//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.IO;
using Microsoft.Cci;
using Microsoft.Cci.MutableCodeModel;
using System.Collections.Generic;
using Microsoft.Cci.Contracts;

namespace Microsoft.Cci.ILToCodeModel {

  /// <summary>
  /// Helper class for performing common tasks on mutable contracts
  /// </summary>
  public class ContractHelper {
    /// <summary>
    /// Accumulates all elements from <paramref name="sourceContract"/> into <paramref name="targetContract"/>
    /// </summary>
    /// <param name="targetContract">Contract which is target of accumulator</param>
    /// <param name="sourceContract">Contract which is source of accumulator</param>
    public static void AddMethodContract(MethodContract targetContract, IMethodContract sourceContract) {
      targetContract.Preconditions.AddRange(sourceContract.Preconditions);
      targetContract.Postconditions.AddRange(sourceContract.Postconditions);
      targetContract.ThrownExceptions.AddRange(sourceContract.ThrownExceptions);
      return;
    }
    /// <summary>
    /// Accumulates all elements from <paramref name="sourceContract"/> into <paramref name="targetContract"/>
    /// </summary>
    /// <param name="targetContract">Contract which is target of accumulator</param>
    /// <param name="sourceContract">Contract which is source of accumulator</param>
    public static void AddTypeContract(TypeContract targetContract, ITypeContract sourceContract) {
      targetContract.ContractFields.AddRange(sourceContract.ContractFields);
      targetContract.ContractMethods.AddRange(sourceContract.ContractMethods);
      targetContract.Invariants.AddRange(sourceContract.Invariants);
      return;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="method"></param>
    /// <returns></returns>
    public static IMethodReference UninstantiateAndUnspecialize(IMethodReference method) {
      IMethodReference result = method;
      IGenericMethodInstanceReference gmir = result as IGenericMethodInstanceReference;
      if (gmir != null) {
        result = gmir.GenericMethod;
      }
      ISpecializedMethodReference smr = result as ISpecializedMethodReference;
      if (smr != null) {
        result = smr.UnspecializedVersion;
      }
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static ITypeReference Unspecialized(ITypeReference type) {
      var instance = type as IGenericTypeInstanceReference;
      if (instance != null) {
        return instance.GenericType;
      }
      return type;
    }

    /// <summary>
    /// Given an interface method, J.M, see if the interface is marked with the
    /// [ContractClass(typeof(T))] attribute. If so, then return T.J.M, else null.
    /// That is, T must explicitly implement J.M, not implicitly!!!
    /// </summary>
    /// <param name="methodDefinition"></param>
    /// <returns></returns>
    public static IMethodDefinition/*?*/ GetMethodFromContractClass(IMethodDefinition methodDefinition) {
      ITypeDefinition iface = methodDefinition.ContainingTypeDefinition;
      foreach (ICustomAttribute attribute in iface.Attributes) {
        if (TypeHelper.GetTypeName(attribute.Type) != "System.Diagnostics.Contracts.ContractClassAttribute")
          continue;
        List<IMetadataExpression> args = new List<IMetadataExpression>(attribute.Arguments);
        IMetadataTypeOf typeHoldingContractMD = args[0] as IMetadataTypeOf;
        ITypeReference typeHoldingContractReference = Unspecialized(typeHoldingContractMD.TypeToGet);
        ITypeDefinition typeHoldingContractDefinition = typeHoldingContractReference.ResolvedType;
        foreach (IMethodImplementation methodImplementation in typeHoldingContractDefinition.ExplicitImplementationOverrides) {
          var implementedInterfaceMethod = UninstantiateAndUnspecialize(methodImplementation.ImplementedMethod);
          if (methodDefinition.InternedKey == implementedInterfaceMethod.InternedKey)
            return methodImplementation.ImplementingMethod.ResolvedMethod;
        }
      }
      return null;
    }

    /// <summary>
    /// Returns the first method found in <paramref name="typeDefinition"/> containing an instance of 
    /// an attribute with the name "ContractInvariantMethodAttribute", if it exists.
    /// </summary>
    /// <param name="typeDefinition">The type whose members will be searched</param>
    /// <returns>May return null if not found</returns>
    public static IMethodDefinition/*?*/ GetInvariantMethod(ITypeDefinition typeDefinition) {
      foreach (IMethodDefinition methodDef in typeDefinition.Methods)
        foreach (var attr in methodDef.Attributes) {
          INamespaceTypeReference ntr = attr.Type as INamespaceTypeReference;
          if (ntr != null && ntr.Name.Value == "ContractInvariantMethodAttribute")
            return methodDef;
        }
      return null;
    }


  }

}