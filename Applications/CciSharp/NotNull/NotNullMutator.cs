//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using CciSharp.Framework;
using Microsoft.Cci.MutableCodeModel;
using Microsoft.Cci;
using System.Diagnostics.Contracts;
using Microsoft.Cci.Contracts;
using Microsoft.Cci.MutableContracts;

namespace CciSharp.Mutators {
  /// <summary>
  /// A mutator that injects null check preconditions and post-conditions.
  /// This mutator must be executed *before* the runtime rewritter.
  /// </summary>
  public sealed class NotNullMutator
      : CcsMutatorBase {
    public NotNullMutator(ICcsHost host)
      : base(host, "Not Null", 10, typeof(NotNullResources)) { }

    public override bool Visit() {
      var assembly = this.Host.MutatedAssembly;
      PdbReader _pdbReader;
      if (!this.Host.TryGetMutatedPdbReader(out _pdbReader))
        _pdbReader = null;
      var contracts = this.Host.MutatedContracts;

      var mutator = new Mutator(this, _pdbReader, contracts);
      mutator.RewriteChildren(assembly);
      ContractHelper.InjectContractCalls(this.Host, this.Host.MutatedAssembly, contracts, _pdbReader);
      return mutator.MutationCount > 0;
    }

    class Mutator
        : CcsCodeMutatorBase<NotNullMutator> {
      readonly Stack<bool> notNullsContext = new Stack<bool>();
      readonly INamespaceTypeReference booleanType;
      readonly INamespaceTypeReference voidType;
      //readonly IMethodReference contractRequiresMethod;

      public Mutator(
          NotNullMutator owner,
          ISourceLocationProvider reader,
          ContractProvider contracts)
        : base(owner, reader, contracts) {
        this.notNullsContext.Push(false);
        this.booleanType = this.Host.PlatformType.SystemBoolean;
        this.voidType = this.Host.PlatformType.SystemBoolean;
      }

      public int MutationCount { get; private set; }


      public override void RewriteChildren(Assembly assembly) {
        ICustomAttribute attribute;
        bool hasAttribute = CcsHelper.TryGetAttributeByName(assembly.Attributes, "NotNullAttribute", out attribute);
        bool hasMaybeNullAttribute = CcsHelper.TryGetAttributeByName(assembly.Attributes, "MaybeNullAttribute", out attribute);
        this.notNullsContext.Push(hasAttribute || (this.notNullsContext.Peek() && !hasMaybeNullAttribute));
        base.RewriteChildren(assembly);
        this.notNullsContext.Pop();
        return;
      }

      public override void RewriteChildren(Module module) {
        ICustomAttribute attribute;
        bool hasAttribute = CcsHelper.TryGetAttributeByName(module.Attributes, "NotNullAttribute", out attribute);
        bool hasMaybeNullAttribute = CcsHelper.TryGetAttributeByName(module.Attributes, "MaybeNullAttribute", out attribute);
        this.notNullsContext.Push(hasAttribute || (this.notNullsContext.Peek() && !hasMaybeNullAttribute));
        base.RewriteChildren(module);
        this.notNullsContext.Pop();
        return;
      }

      public override void RewriteChildren(NamespaceTypeDefinition namespaceTypeDefinition) {
        ICustomAttribute attribute;
        bool hasAttribute = CcsHelper.TryGetAttributeByName(namespaceTypeDefinition.Attributes, "NotNullAttribute", out attribute);
        bool hasMaybeNullAttribute = CcsHelper.TryGetAttributeByName(namespaceTypeDefinition.Attributes, "MaybeNullAttribute", out attribute);
        this.notNullsContext.Push(hasAttribute || (this.notNullsContext.Peek() && !hasMaybeNullAttribute));
        base.RewriteChildren(namespaceTypeDefinition);
        this.notNullsContext.Pop();
        return;
      }

      public override void RewriteChildren(NestedTypeDefinition nestedTypeDefinition) {
        ICustomAttribute attribute;
        bool hasAttribute = CcsHelper.TryGetAttributeByName(nestedTypeDefinition.Attributes, "NotNullAttribute", out attribute);
        bool hasMaybeNullAttribute = CcsHelper.TryGetAttributeByName(nestedTypeDefinition.Attributes, "MaybeNullAttribute", out attribute);
        this.notNullsContext.Push(hasAttribute || (this.notNullsContext.Peek() && !hasMaybeNullAttribute));
        base.RewriteChildren(nestedTypeDefinition);
        this.notNullsContext.Pop();
        return;
      }

      public override void RewriteChildren(MethodDefinition methodDefinition) {
        if (methodDefinition.IsAbstract) {
          // not supported yet
          return;
        }

        var initialMutationCount = this.MutationCount;
        bool hasAttribute;
        ICustomAttribute attribute;
        var preconditions = new List<IPrecondition>();

        // do not add requires to overloaded methods
        bool isOverride = methodDefinition.IsVirtual &&
          MemberHelper.GetImplicitlyOverriddenBaseClassMethod(methodDefinition) != Dummy.Method;

        if (methodDefinition.ParameterCount > 0) {
          foreach (var parameter in methodDefinition.Parameters) {
            if (CcsHelper.TryGetAttributeByName(parameter.Attributes, "MaybeNullAttribute", out attribute))
              continue;
            var type = parameter.Type;
            hasAttribute = CcsHelper.TryGetAttributeByName(parameter.Attributes, "NotNullAttribute", out attribute);
            // validate
            if (hasAttribute) {
              if (type.IsValueType) {
                this.Host.Event(CcsEventLevel.Error, "[NotNull] may only be applied to reference types");
                continue;
              }
              if (isOverride) {
                this.Host.Event(CcsEventLevel.Error, "[NotNull] may not be applied on parameters in method overrides");
                continue;
              }
            }
            if (hasAttribute || this.notNullsContext.Peek()) {
              // skip
              if (type.IsValueType)
                continue;
              preconditions.Add(
                new Precondition {
                  Condition = new NotEquality {
                    LeftOperand = new BoundExpression { Definition = parameter },
                    RightOperand = new CompileTimeConstant { Value = null, Type = parameter.Type },
                    Type = this.booleanType,
                  },
                  // description
                  OriginalSource = parameter.Name.Value + " != null",
                });
              this.MutationCount++;
            }
          }
        }
        

        var postconditions = new List<IPostcondition>();
        if (!CcsHelper.TryGetAttributeByName(methodDefinition.ReturnValueAttributes, "MaybeNullAttribute", out attribute)) {
          var returnType = methodDefinition.Type;
          hasAttribute = CcsHelper.TryGetAttributeByName(methodDefinition.ReturnValueAttributes, "NotNullAttribute", out attribute);
          if (hasAttribute && returnType.IsValueType) {
            this.Host.Event(CcsEventLevel.Error, "[NotNull] may only be applied to reference types");
          } else {
            if (hasAttribute || this.notNullsContext.Peek()) {
              if (!returnType.IsValueType) {
                postconditions.Add(
                  new Postcondition {
                    Condition = new NotEquality {
                      LeftOperand = new ReturnValue { Type = returnType, },
                      RightOperand = new CompileTimeConstant { Value = null, Type = returnType },
                      Type = this.booleanType,
                    },
                    OriginalSource = "result != null",
                  });
                this.MutationCount++;
              }
            }
          }
        }

        if (initialMutationCount < this.MutationCount) {
          var newContract = new MethodContract {
            Preconditions = preconditions,
            Postconditions = postconditions,
          };

          // merge existing contracts, if any
          var contract = this.contractProvider.GetMethodContractFor(methodDefinition);
          if (contract != null) {
            ContractHelper.AddMethodContract(newContract, contract);
          }
          // store new contracts
          this.contractProvider.AssociateMethodWithContract(methodDefinition, newContract);
        }

        // Visit the parameters so any attributes are removed
        methodDefinition.Parameters = this.Rewrite(methodDefinition.Parameters);

        return;
      }

      public override List<ICustomAttribute> Rewrite(List<ICustomAttribute> customAttributes) {
        if (customAttributes == null) return customAttributes;
        customAttributes.RemoveAll(a => 
          CcsHelper.AttributeMatchesByName(a, "NotNullAttribute")
          ||
          CcsHelper.AttributeMatchesByName(a, "MaybeNullAttribute")
          );
        return customAttributes;
      }

    }
  }
}
