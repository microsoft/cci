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
        if (!containingTypeDefinition.IsValueType) { //If it is a value type, is is probably the state class of an async method.
          if (this.closures == null) this.closures = new Hashtable<object>();
          this.closures.Add(containingTypeDefinition.InternedKey, containingTypeDefinition);
          this.AddOuterClosures(containingTypeDefinition);
          this.sawAnonymousDelegate = true;
        }
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
      var local = new CapturedLocalDefinition() { MethodDefinition = this.method, Name = field.Name, Type = field.Type };
      this.closureFieldToLocalOrParameterMap.Add(field.InternedKey, new BoundExpression() { Definition = local, Type = field.Type });
    }

    public override void TraverseChildren(IBlockStatement block) {
      Contract.Assume(block is BlockStatement);
      var mutableBlock = (BlockStatement)block;
      this.Traverse(mutableBlock.Statements);
    }

  }

  internal class ClosureFieldDeclaringBlockFinder : CodeTraverser {

    internal ClosureFieldDeclaringBlockFinder(Hashtable<Expression> closureFieldToLocalOrParameterMap) {
      Contract.Requires(closureFieldToLocalOrParameterMap != null);

      this.closureFieldToLocalOrParameterMap = closureFieldToLocalOrParameterMap;
      this.declaringBlockMap = new Hashtable<BlockStatement>();
      this.containingBlockMap = new Hashtable<BlockStatement, BlockStatement>();
    }

    BlockStatement/*?*/ currentBlock;
    Hashtable<Expression> closureFieldToLocalOrParameterMap;
    internal readonly Hashtable<BlockStatement> declaringBlockMap;
    Hashtable<BlockStatement, BlockStatement> containingBlockMap;

    [ContractInvariantMethod]
    private void ObjectInvariant() {
      Contract.Invariant(this.closureFieldToLocalOrParameterMap != null);
      Contract.Invariant(this.declaringBlockMap != null);
      Contract.Invariant(this.containingBlockMap != null);
    }

    public override void TraverseChildren(IBlockStatement block) {
      Contract.Assume(block is BlockStatement);
      var mutableBlock = (BlockStatement)block;
      if (this.currentBlock != null) this.containingBlockMap.Add(mutableBlock, this.currentBlock);
      var savedCurrentBlock = this.currentBlock;
      this.currentBlock = mutableBlock;
      this.Traverse(mutableBlock.Statements);
      this.currentBlock = savedCurrentBlock;
    }

    //TODO: endow ForStatements with a block that contains its init statements.

    public override void TraverseChildren(IAddressableExpression addressableExpression) {
      base.TraverseChildren(addressableExpression);
      this.UpdateDeclaringBlock(addressableExpression.Definition as IFieldReference);
    }

    public override void TraverseChildren(IBoundExpression boundExpression) {
      base.TraverseChildren(boundExpression);
      this.UpdateDeclaringBlock(boundExpression.Definition as IFieldReference);
    }

    public override void TraverseChildren(ITargetExpression targetExpression) {
      base.TraverseChildren(targetExpression);
      this.UpdateDeclaringBlock(targetExpression.Definition as IFieldReference);
    }

    private void UpdateDeclaringBlock(IFieldReference/*?*/ fieldReference) {
      if (fieldReference == null) return;
      var boundExpr = this.closureFieldToLocalOrParameterMap[fieldReference.InternedKey] as BoundExpression;
      if (boundExpr == null || !(boundExpr.Definition is ILocalDefinition)) return;
      var declaringBlock = this.declaringBlockMap[fieldReference.InternedKey];
      if (declaringBlock == null) {
        this.declaringBlockMap[fieldReference.InternedKey] = this.currentBlock;
        return;
      }
      var dblock = declaringBlock;
      while (dblock != null) {
        if (dblock == this.currentBlock) return;
        Contract.Assume(this.currentBlock != null);
        var cblock = this.containingBlockMap[this.currentBlock];
        while (cblock != null) {
          if (cblock == dblock) {
            this.declaringBlockMap[fieldReference.InternedKey] = dblock;
            return;
          }
          cblock = this.containingBlockMap[cblock];
        }
        dblock = this.containingBlockMap[dblock];
      }
      Contract.Assume(false);
    }

  }

  internal class CapturedLocalDeclarationInserter : CodeTraverser {

    internal CapturedLocalDeclarationInserter(Hashtable<Expression> closureFieldToLocalOrParameterMap, Hashtable<BlockStatement> declaringBlockMap) {
      Contract.Requires(declaringBlockMap != null);
      Contract.Requires(closureFieldToLocalOrParameterMap != null);

      this.closureFieldToLocalOrParameterMap = closureFieldToLocalOrParameterMap;
      this.declaringBlockMap = declaringBlockMap;
    }

    Hashtable<Expression> closureFieldToLocalOrParameterMap;
    Hashtable<BlockStatement> declaringBlockMap;

    [ContractInvariantMethod]
    private void ObjectInvariant() {
      Contract.Invariant(this.declaringBlockMap != null);
      Contract.Invariant(this.closureFieldToLocalOrParameterMap != null);
    }

    public override void TraverseChildren(IBlockStatement block) {
      Contract.Assume(block is BlockStatement);
      var mutableBlock = (BlockStatement)block;
      this.Traverse(mutableBlock.Statements);
      foreach (var pair in this.declaringBlockMap) {
        if (pair.Value != mutableBlock) continue;
        var boundExpr = this.closureFieldToLocalOrParameterMap[pair.Key] as BoundExpression;
        if (boundExpr == null) { Contract.Assume(false); continue; }
        var local = boundExpr.Definition as ILocalDefinition;
        if (local == null) { Contract.Assume(false); continue; }
        var localDecl = new LocalDeclarationStatement() { LocalVariable = local };
        mutableBlock.Statements.Insert(0, localDecl);
      }
    }
  }

  internal class CapturedLocalDefinition : LocalDefinition {
  }
}