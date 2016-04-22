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
    internal Hashtable<object>/*?*/ closuresThatCannotBeDeleted;
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

    public override void TraverseChildren(ITokenOf tokenOf) {
      base.TraverseChildren(tokenOf);
      var typeRef = tokenOf.Definition as ITypeReference;
      if (typeRef == null) {
        var typeMemberRef = tokenOf.Definition as ITypeMemberReference;
        if (typeMemberRef != null)
          typeRef = typeMemberRef.ContainingType;
      }
      if (typeRef != null) {
        var typeDef = typeRef.ResolvedType;
        if (TypeHelper.IsCompilerGenerated(typeDef)) {
          if (this.closuresThatCannotBeDeleted == null)
            this.closuresThatCannotBeDeleted = new Hashtable<object>();
          while (typeDef != null) {
            this.closuresThatCannotBeDeleted[typeDef.InternedKey] = typeDef;
            ITypeDefinition baseType = null;
            foreach (var baseTypeRef in typeDef.BaseClasses) {
              baseType = baseTypeRef as ITypeDefinition;
              if (baseType != null) break;
            }
            typeDef = baseType;
          }         
        }         
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
        if (TypeHelper.IsEmbeddedInteropType(ft)) continue;
        if (this.closures.Find(ft.InternedKey) != null) continue;
        this.closures.Add(ft.InternedKey, ft);
        this.AddOuterClosures(ft);
      }
    }

  }

  /// <summary>
  /// The purpose of this traverser is to discover the mapping between fields in a closure state class and the original locals and parameters that were
  /// captured into the closure state class, so that we can substitute field accesses with local and parameter accesses during decompilation of anonymous
  /// delegates. Things are complicated by having to deal with a variety of compilers that potentially use different name mangling schemes
  /// and moreover we might not have a PDB file available and so might not know the name of a local or parameter. The bottom line is that we
  /// cannot rely on naming conventions. Generally, we rely on the source operand of the first assignment to a state field as providing the local
  /// or parameter that is being captured. However, if an anonymous delegate uses a local that is not used outside of it (or other anonymous delegates)
  /// then a compiler (such as, alas, the C# compiler) might provide a state field for the local while not actually defining a real local of inserting
  /// an assignment to capture the value of the local in the state class before constructing a closure. We therefore recurse into anonymous delegate 
  /// bodies to find assignments to state fields, assume those are captured locals, and then dummy up locals for use in the decompiled method.
  /// </summary>
  internal class ClosureFieldMapper : CodeTraverser {

    internal ClosureFieldMapper(IMetadataHost host, IMethodDefinition method, Hashtable<object> closures, Hashtable<Expression> closureFieldToLocalOrParameterMap) {
      Contract.Requires(host != null);
      Contract.Requires(method != null);
      Contract.Requires(closures != null);
      Contract.Requires(closureFieldToLocalOrParameterMap != null);

      this.host = host;
      this.method = method;
      this.closures = closures;
      this.closureFieldToLocalOrParameterMap = closureFieldToLocalOrParameterMap;
    }

    IMetadataHost host;
    IMethodDefinition method;
    Hashtable<object> closures;
    Hashtable<Expression> closureFieldToLocalOrParameterMap;

    [ContractInvariantMethod]
    private void ObjectInvariant() {
      Contract.Invariant(this.host != null);
      Contract.Invariant(this.method != null);
      Contract.Invariant(this.closures != null);
      Contract.Invariant(this.closureFieldToLocalOrParameterMap != null);
    }

    public override void TraverseChildren(IAssignment assignment) {
      base.TraverseChildren(assignment);
      var definition = assignment.Target.Definition;
      var instance = assignment.Target.Instance;
      if (instance == null) {
        var addressDeref = assignment.Target.Definition as IAddressDereference;
        if (addressDeref != null) {
          var addressOf = addressDeref.Address as IAddressOf;
          if (addressOf != null) {
            instance = addressOf.Expression.Instance;
            definition = addressOf.Expression.Definition;
          }
        }
      }
      if (instance == null) return;
      var field = definition as IFieldReference;
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
      var local = new CapturedLocalDefinition() { capturingField = field, MethodDefinition = this.method, Name = field.Name, Type = field.Type };
      this.closureFieldToLocalOrParameterMap.Add(field.InternedKey, new BoundExpression() { Definition = local, Type = field.Type });
    }

    public override void TraverseChildren(IAddressableExpression addressableExpression) {
      var definition = addressableExpression.Definition;
      var instance = addressableExpression.Instance;
      if (instance == null) {
        var addressDeref = definition as IAddressDereference;
        if (addressDeref != null) {
          var addressOf = addressDeref.Address as IAddressOf;
          if (addressOf != null) {
            instance = addressOf.Expression.Instance;
            definition = addressOf.Expression.Definition;
          }
        }
      }
      if (instance == null) return;
      var field = definition as IFieldReference;
      if (field == null) return;
      if (this.closures.Find(instance.Type.InternedKey) == null) return;
      if (this.closureFieldToLocalOrParameterMap[field.InternedKey] != null) return;
      var local = new CapturedLocalDefinition() { capturingField = field, MethodDefinition = this.method, Name = field.Name, Type = field.Type };
      this.closureFieldToLocalOrParameterMap.Add(field.InternedKey, new BoundExpression() { Definition = local, Type = field.Type });
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
    internal IFieldReference capturingField;

    public override LocalDefinition Clone() {
      var clone = new CapturedLocalDefinition();
      clone.Copy(this, Dummy.InternFactory);
      clone.capturingField = this.capturingField;
      return clone;
    }
  }
}