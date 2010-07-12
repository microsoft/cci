using System;
using System.Collections.Generic;
using System.Text;
using CciSharp.Framework;
using Microsoft.Cci;
using Microsoft.Cci.Contracts;
using Microsoft.Cci.MutableCodeModel;
using Microsoft.Cci.MutableContracts;

namespace CciSharp.Mutators
{
  public class EnsuresNotNullMutator : CcsMutatorBase 
  {
    public EnsuresNotNullMutator(ICcsHost host)
      : base(host, "Ensures Not Null", 0, typeof(EnsuresNotNullMutator))
        {
        }


    public override bool Visit() {
      var assembly = this.Host.MutatedAssembly;
      PdbReader _pdbReader;
      if (!this.Host.TryGetMutatedPdbReader(out _pdbReader))
        _pdbReader = null;
      var contracts = this.Host.MutatedContracts;

      new NonNullInjector(this.Host, contracts).Visit(this.Host.MutatedAssembly);
      ContractHelper.InjectContractCalls(this.Host, this.Host.MutatedAssembly, contracts, _pdbReader);

      return true;
    }
  }
  class NonNullInjector : BaseCodeTraverser {

    IMetadataHost host;
    Microsoft.Cci.MutableContracts.ContractProvider contractProvider;

    public NonNullInjector(
      IMetadataHost host,
      Microsoft.Cci.MutableContracts.ContractProvider contractProvider) {
      this.host = host;
      this.contractProvider = contractProvider;
    }

    public override void Visit(IMethodDefinition method) {
      if (!MemberHelper.IsVisibleOutsideAssembly(method)) return;
      var returnType = method.Type;
      if (returnType == this.host.PlatformType.SystemVoid
        || returnType.IsEnum
        || returnType.IsValueType
        ) return;

      var newContract = new Microsoft.Cci.MutableContracts.MethodContract();
      var post = new List<IPostcondition>();
      var p = new Microsoft.Cci.MutableContracts.PostCondition() {
        Condition = new NotEquality() {
          LeftOperand = new ReturnValue() { Type = returnType, },
          RightOperand = new CompileTimeConstant() {
            Type = returnType,
            Value = null,
          },
          Type = this.host.PlatformType.SystemBoolean,
        },
        OriginalSource = "result != null",
      };
      post.Add(p);
      newContract.Postconditions = post;

      var contract = this.contractProvider.GetMethodContractFor(method);
      if (contract != null) {
        Microsoft.Cci.MutableContracts.ContractHelper.AddMethodContract(newContract, contract);
      }
      this.contractProvider.AssociateMethodWithContract(method, newContract);

      base.Visit(method);
    }
  }


}
