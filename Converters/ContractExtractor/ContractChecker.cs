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
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using Microsoft.Cci;
using Microsoft.Cci.MutableContracts;

namespace Microsoft.Cci.Contracts {

  internal class Visibility : CodeTraverser {

    private IMetadataHost host;
    private TypeMemberVisibility currentVisibility = TypeMemberVisibility.Public;

    private Visibility(IMetadataHost host) {
      this.host = host;
    }

    /// <summary>
    /// Returns the most restrictive visibility of any member mentioned within the expression.
    /// I.e., if a private member is referenced within the expression, then TypeMemberVisibility.Private
    /// is returned. If TypeMemberVisibility.Public is returned, then all referenced members are public.
    /// </summary>
    public static TypeMemberVisibility MostRestrictiveVisibility(IMetadataHost host, IExpression expression) {
      var v = new Visibility(host);
      v.Traverse(expression);
      return v.currentVisibility;
    }

    public override void TraverseChildren(IBoundExpression boundExpression) {
      var tm = boundExpression.Definition as ITypeMemberReference;
      if (tm != null) {
        var resolvedMember = tm.ResolvedTypeDefinitionMember;
        string propertyName = ContractHelper.GetStringArgumentFromAttribute(resolvedMember.Attributes, "System.Diagnostics.Contracts.ContractPublicPropertyNameAttribute");
        // we don't care what it is, it just means it has a public property that represents it
        // so if it is null, then it is *not* a field that has a [ContractPublicPropertyName] marking
        // and so its visibility counts. If it *is* such a field, then it is considered to be public.
        // (TODO: checker should make sure that the property it names is public.)
        if (propertyName == null) {
          this.currentVisibility = TypeHelper.VisibilityIntersection(this.currentVisibility, resolvedMember.Visibility);
        }
      }
      base.TraverseChildren(boundExpression);
    }

  }

  /// <summary>
  /// 
  /// </summary>
  public class ContractChecker {

    private ContractChecker() { }

    /// <summary>
    /// Mutates the <paramref name="methodContract"/> by removing any contracts that violate any of the rules
    /// about contracts, e.g., preconditions mentioning a member that is more restrictive than the method containing
    /// the precondition.
    /// TODO: Return a list of errors.
    /// </summary>
    public static void CheckMethodContract(IMetadataHost host, IMethodDefinition method, MethodContract methodContract) {
      var reqs = methodContract.Preconditions;
      var newReqs = new List<IPrecondition>(methodContract.Preconditions.Count);
      foreach (var p in reqs) {
        var contractExpression = p.Condition;
        var v = Visibility.MostRestrictiveVisibility(host, contractExpression);
        var currentVisibility = method.Visibility;
        var intersection = TypeHelper.VisibilityIntersection(v, currentVisibility);
        if (intersection == currentVisibility) {
          newReqs.Add(p);
        } else {
          // TODO!! BUGBUG!! Need to signal an error, not just silently not add the precondition!
        }
      }
      methodContract.Preconditions = newReqs;
    }
  }

}
