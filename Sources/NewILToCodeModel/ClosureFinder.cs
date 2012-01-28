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
using System.Diagnostics.Contracts;
using Microsoft.Cci.MutableCodeModel;
using System.Collections.Generic;
using Microsoft.Cci.UtilityDataStructures;

namespace Microsoft.Cci.ILToCodeModel {
  internal class ClosureFinder : CodeTraverser {

    internal ClosureFinder(IMetadataHost host) {
      Contract.Requires(host != null);
      this.host = host;
    }

    IMetadataHost host;
    internal Hashtable<object>/*?*/ closures;
    internal bool sawAnonymousDelegate;

    [ContractInvariantMethod]
    private void ObjectInvariant() {
      Contract.Invariant(this.host != null);
    }

    public override void TraverseChildren(ICreateDelegateInstance createDelegateInstance) {
      base.TraverseChildren(createDelegateInstance);
      var delegateMethodDefinition = createDelegateInstance.MethodToCallViaDelegate.ResolvedMethod;
      var containingTypeDefinition = delegateMethodDefinition.ContainingTypeDefinition;
      if (TypeHelper.IsCompilerGenerated(containingTypeDefinition)) {
        if (this.closures == null) this.closures = new Hashtable<object>();
        this.closures.Add(containingTypeDefinition.InternedKey, containingTypeDefinition);
        this.AddOuterClosures(containingTypeDefinition);
        this.sawAnonymousDelegate = true;
      } else {
        this.sawAnonymousDelegate |= AttributeHelper.Contains(delegateMethodDefinition.Attributes, this.host.PlatformType.SystemRuntimeCompilerServicesCompilerGeneratedAttribute);
      }
    }

    private void AddOuterClosures(ITypeDefinition typeDefinition) {
      Contract.Requires(typeDefinition != null);
      Contract.Requires(this.closures != null);
      Contract.Ensures(this.closures != null);

      foreach (var field in typeDefinition.Fields) {
        Contract.Assume(field != null);
        var ft = field.Type.ResolvedType;
        if (!TypeHelper.IsCompilerGenerated(ft)) continue;
        this.closures.Add(ft.InternedKey, ft);
        this.AddOuterClosures(ft);
      }
    }

  }

  internal class ClosureFieldMapper : CodeTraverser {

    internal ClosureFieldMapper(IMetadataHost host, IMethodDefinition method, Hashtable<object> closures) {
      Contract.Requires(host != null);
      Contract.Requires(method != null);
      Contract.Requires(closures != null);

      this.host = host;
      this.method = method;
      this.closures = closures;
      this.closureFieldToLocalOrParameterMap = new Hashtable<Expression>();
    }

    IMetadataHost host;
    IMethodDefinition method;
    Hashtable<object> closures;
    internal Hashtable<Expression> closureFieldToLocalOrParameterMap;
    LocalDeclarationStatement/*?*/ localDeclarationToSubstituteForAssignment;

    [ContractInvariantMethod]
    private void ObjectInvariant() {
      Contract.Invariant(this.host != null);
      Contract.Invariant(this.method != null);
      Contract.Invariant(this.closures != null);
      Contract.Invariant(this.closureFieldToLocalOrParameterMap != null);
    }

    public override void TraverseChildren(IAssignment assignment) {
      base.TraverseChildren(assignment);
      var instance = assignment.Target.Instance;
      if (instance == null) return;
      var field = assignment.Target.Definition as IFieldReference;
      if (field == null) return;
      if (this.closures.Find(instance.Type.InternedKey) == null) return;
      if (this.closureFieldToLocalOrParameterMap[field.InternedKey] != null) return;
      var thisRef = assignment.Source as ThisReference;
      if (thisRef != null) {
        this.closureFieldToLocalOrParameterMap.Add(field.InternedKey, thisRef);
        return;
      }
      var binding = assignment.Source as BoundExpression;
      if (binding != null) {
        var par = binding.Definition as IParameterDefinition;
        if (par != null && par.Name == field.Name) {
          this.closureFieldToLocalOrParameterMap.Add(field.InternedKey, binding);
          return;
        }
      }
      var local = new LocalDefinition() { MethodDefinition = this.method, Name = field.Name, Type = field.Type };
      this.closureFieldToLocalOrParameterMap.Add(field.InternedKey, new BoundExpression() { Definition = local, Type = field.Type });
      this.localDeclarationToSubstituteForAssignment = new LocalDeclarationStatement() { LocalVariable = local, InitialValue = assignment.Source };
    }

    public override void TraverseChildren(IBlockStatement block) {
      Contract.Assume(block is BlockStatement);
      var mutableBlock = (BlockStatement)block;
      this.Traverse(mutableBlock.Statements);
    }

    public override void TraverseChildren(IForStatement forStatement) {
      Contract.Assume(forStatement is ForStatement);
      var mutableForStatement = (ForStatement)forStatement;
      this.TraverseChildren((IStatement)mutableForStatement);
      this.Traverse(mutableForStatement.InitStatements);
      this.Traverse(mutableForStatement.Condition);
      this.Traverse(forStatement.IncrementStatements);
      this.Traverse(mutableForStatement.Body);
    }

    private void Traverse(List<IStatement> statements) {
      Contract.Requires(statements != null);

      for (int i = 0, n = statements.Count; i < n; i++) {
        Contract.Assume(statements[i] != null);
        this.Traverse(statements[i]);
        if (this.localDeclarationToSubstituteForAssignment != null) {
          statements[i] = this.localDeclarationToSubstituteForAssignment;
          this.localDeclarationToSubstituteForAssignment = null;
        }
      }
    }


  }


}