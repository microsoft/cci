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
using System;
using System.IO;
using Microsoft.Cci;
using Microsoft.Cci.MutableCodeModel;
using System.Collections.Generic;
using Microsoft.Cci.Contracts;

namespace Microsoft.Cci.Contracts {

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
      targetContract.IsPure |= sourceContract.IsPure; // need the disjunction
      return;
    }

    /// <summary>
    /// Returns a method contract containing the 'effective' contract for the given
    /// method definition. The effective contract contains all contracts for the method:
    /// any that it has on its own, as well as all those inherited from any methods
    /// that it overrides or interface methods that it implements (either implicitly
    /// or explicitly).
    /// All parameters in inherited contracts are substituted for by
    /// the method's own parameters.
    /// If there are no contracts, then it returns null.
    /// </summary>
    public static MethodContract/*?*/ GetMethodContractForIncludingInheritedContracts(IContractAwareHost host, IMethodDefinition methodDefinition) {
      MethodContract cumulativeContract = new MethodContract();
      bool atLeastOneContract = false;
      IMethodContract/*?*/ mc = GetMethodContractFor(host, methodDefinition);
      if (mc != null) {
        ContractHelper.AddMethodContract(cumulativeContract, mc);
        atLeastOneContract = true;
      }
      #region Overrides of base class methods
      if (!methodDefinition.IsNewSlot) { // REVIEW: Is there a better test?
        IMethodDefinition overriddenMethod = MemberHelper.GetImplicitlyOverriddenBaseClassMethod(methodDefinition) as IMethodDefinition;
        while (overriddenMethod != null && overriddenMethod != Dummy.Method) {
          IMethodContract/*?*/ overriddenContract = GetMethodContractFor(host, overriddenMethod);
          if (overriddenContract != null) {
            SubstituteParameters sps = new SubstituteParameters(host, methodDefinition, overriddenMethod);
            MethodContract newContract = sps.Visit(overriddenContract) as MethodContract;
            ContractHelper.AddMethodContract(cumulativeContract, newContract);
            atLeastOneContract = true;
          }
          overriddenMethod = MemberHelper.GetImplicitlyOverriddenBaseClassMethod(overriddenMethod) as IMethodDefinition;
        }
      }
      #endregion Overrides of base class methods
      #region Implicit interface implementations
      foreach (IMethodDefinition ifaceMethod in MemberHelper.GetImplicitlyImplementedInterfaceMethods(methodDefinition)) {
        IMethodContract/*?*/ ifaceContract = GetMethodContractFor(host, ifaceMethod);
        if (ifaceContract == null) continue;
        SubstituteParameters sps = new SubstituteParameters(host, methodDefinition, ifaceMethod);
        MethodContract newContract = sps.Visit(ifaceContract) as MethodContract;
        ContractHelper.AddMethodContract(cumulativeContract, newContract);
        atLeastOneContract = true;
      }
      #endregion Implicit interface implementations
      #region Explicit interface implementations and explicit method overrides
      foreach (IMethodReference ifaceMethodRef in MemberHelper.GetExplicitlyOverriddenMethods(methodDefinition)) {
        IMethodDefinition/*?*/ ifaceMethod = ifaceMethodRef.ResolvedMethod;
        if (ifaceMethod == null) continue;
        IMethodContract/*?*/ ifaceContract = GetMethodContractFor(host, ifaceMethod);
        if (ifaceContract == null) continue;
        SubstituteParameters sps = new SubstituteParameters(host, methodDefinition, ifaceMethod);
        MethodContract newContract = sps.Visit(ifaceContract) as MethodContract;
        ContractHelper.AddMethodContract(cumulativeContract, newContract);
        atLeastOneContract = true;
      }
      #endregion Explicit interface implementations and explicit method overrides
      return atLeastOneContract ? cumulativeContract : null;
    }

    /// <summary>
    /// Returns a (possibly-null) method contract relative to a contract-aware host.
    /// If you already know which unit the method is defined in and/or already have
    /// the contract provider for the unit in which the method is defined, then you
    /// would do just as well to directly query that contract provider.
    /// </summary>
    public static IMethodContract/*?*/ GetMethodContractFor(IContractAwareHost host, IMethodDefinition methodDefinition) {
      IUnit/*?*/ unit = TypeHelper.GetDefiningUnit(methodDefinition.ContainingType.ResolvedType);
      if (unit == null) return null;
      IContractProvider/*?*/ cp = host.GetContractProvider(unit.UnitIdentity);
      if (cp == null) return null;
      return cp.GetMethodContractFor(methodDefinition);
    }
  }

  /// <summary>
  /// A mutator that substitutes parameters defined in one method with those from another method.
  /// </summary>
  public sealed class SubstituteParameters : MethodBodyCodeAndContractMutator {
    private IMethodDefinition targetMethod;
    private IMethodDefinition sourceMethod;
    /// <summary>
    /// Creates a mutator that replaces all occurrences of parameters from the target method with those from the source method.
    /// </summary>
    public SubstituteParameters(IMetadataHost host, IMethodDefinition targetMethodDefinition, IMethodDefinition sourceMethodDefinition)
      : base(host, false) { // NB: Important to pass "false": this mutator needs to make a copy of the entire contract!
      this.targetMethod = targetMethodDefinition;
      this.sourceMethod = sourceMethodDefinition;
    }

    /// <summary>
    /// Visits the specified bound expression.
    /// </summary>
    /// <param name="boundExpression">The bound expression.</param>
    /// <returns></returns>
    public override IExpression Visit(BoundExpression boundExpression) {
      ParameterDefinition/*?*/ par = boundExpression.Definition as ParameterDefinition;
      if (par != null && par.ContainingSignature == this.sourceMethod) {
        List<IParameterDefinition> parameters = new List<IParameterDefinition>(targetMethod.Parameters);
        boundExpression.Definition = parameters[par.Index];
        return boundExpression;
      } else {
        return base.Visit(boundExpression);
      }
    }
  }

}