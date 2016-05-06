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

  internal class AnonymousDelegateInserter : CodeRewriter {

    internal AnonymousDelegateInserter(SourceMethodBody sourceMethodBody)
      : base(sourceMethodBody.host) {
      Contract.Requires(sourceMethodBody != null);
      Contract.Requires(sourceMethodBody.ilMethodBody != null);

      this.sourceMethodBody = sourceMethodBody;
      this.numberOfAssignmentsToLocal = sourceMethodBody.numberOfAssignmentsToLocal; Contract.Assume(this.numberOfAssignmentsToLocal != null);
      this.numberOfReferencesToLocal = sourceMethodBody.numberOfReferencesToLocal; Contract.Assume(this.numberOfReferencesToLocal != null);
      this.containingMethod = sourceMethodBody.ilMethodBody.MethodDefinition;
      var containingType = sourceMethodBody.ilMethodBody.MethodDefinition.ContainingTypeDefinition;
      if (containingType.IsGeneric)
        this.containingType = containingType.InstanceType;
      else
        this.containingType = containingType;
    }

    SourceMethodBody sourceMethodBody;
    ITypeReference containingType;
    IMethodDefinition containingMethod;
    Hashtable<IAnonymousDelegate>/*?*/ delegatesCachedInFields;
    Hashtable<LocalDefinition, AnonymousDelegate>/*?*/ delegatesCachedInLocals;
    Hashtable<object>/*?*/ closures;
    HashtableForUintValues<object> numberOfAssignmentsToLocal;
    HashtableForUintValues<object> numberOfReferencesToLocal;
    ClosureFieldMapper/*?*/ closureFieldMapper;

    [ContractInvariantMethod]
    private void ObjectInvariant() {
      Contract.Invariant(this.sourceMethodBody != null);
      Contract.Invariant(this.containingMethod != null);
      Contract.Invariant(this.numberOfAssignmentsToLocal != null);
      Contract.Invariant(this.numberOfReferencesToLocal != null);
    }

    public IBlockStatement InsertAnonymousDelegates(IBlockStatement block, out bool didNothing) {
      Contract.Requires(block != null);
      Contract.Ensures(Contract.Result<IBlockStatement>() != null);

      Hashtable<Expression>/*?*/ closureFieldToLocalOrParameterMap = null;
      didNothing = true;
      var closureFinder = new ClosureFinder(this.host);
      closureFinder.Traverse(block);
      if (!closureFinder.sawAnonymousDelegate) return block;
      didNothing = false;
      var closures = this.closures = closureFinder.closures;
      var closuresThatCannotBeDeleted = closureFinder.closuresThatCannotBeDeleted;
      closureFinder = null;
      if (closures != null) {
        if (closuresThatCannotBeDeleted != null) {
          foreach (var keptClosure in closuresThatCannotBeDeleted.Values) {
            var td = keptClosure as ITypeDefinition;
            if (td == null) continue;
            closures[td.InternedKey] = null;
          }
        }
        closureFieldToLocalOrParameterMap = new Hashtable<Expression>();
        this.closureFieldMapper = new ClosureFieldMapper(this.host, this.containingMethod, closures, closureFieldToLocalOrParameterMap);
        this.closureFieldMapper.Traverse(block);
      }
      var result = this.Rewrite(block);
      if (this.delegatesCachedInFields != null || this.delegatesCachedInLocals != null)
        result = new AnonymousDelegateCachingRemover(this.host, this.delegatesCachedInFields, this.delegatesCachedInLocals).Rewrite(result);
      if (closures != null) {
        Contract.Assert(closureFieldToLocalOrParameterMap != null);
        if (this.sourceMethodBody.privateHelperTypesToRemove == null) 
          this.sourceMethodBody.privateHelperTypesToRemove = new List<ITypeDefinition>();
        foreach (ITypeDefinition closure in closures.Values) {
          Contract.Assume(closure != null);
          this.sourceMethodBody.privateHelperTypesToRemove.Add(TypeHelper.UninstantiateAndUnspecialize(closure).ResolvedType);
        }
        var declBlockFinder = new ClosureFieldDeclaringBlockFinder(closureFieldToLocalOrParameterMap);
        declBlockFinder.Traverse(result);
        Contract.Assume(declBlockFinder.declaringBlockMap != null);
        new CapturedLocalDeclarationInserter(closureFieldToLocalOrParameterMap, declBlockFinder.declaringBlockMap).Traverse(result);
        result = new ClosureRemover(this.sourceMethodBody, closures, closureFieldToLocalOrParameterMap).Rewrite(result);
      }
      if (this.delegatesCachedInFields != null || this.delegatesCachedInLocals != null || closures != null) {
        Contract.Assume(result is BlockStatement);
        new PatternReplacer(this.sourceMethodBody, (BlockStatement)result).Traverse(result);
      }
      return result;
    }

    public override void RewriteChildren(Assignment assignment) {
      base.RewriteChildren(assignment);
      var anonDel = assignment.Source as IAnonymousDelegate;
      if (anonDel != null) {
        var targetField = assignment.Target.Definition as IFieldReference;
        if (targetField != null && targetField.ResolvedField.IsStatic && 
        (AttributeHelper.Contains(targetField.ResolvedField.Attributes, this.host.PlatformType.SystemRuntimeCompilerServicesCompilerGeneratedAttribute) ||
         AttributeHelper.Contains(targetField.ResolvedField.ContainingTypeDefinition.Attributes, this.host.PlatformType.SystemRuntimeCompilerServicesCompilerGeneratedAttribute))) {
          var specializedTargetField = targetField as ISpecializedFieldReference;
          if (this.delegatesCachedInFields == null) this.delegatesCachedInFields = new Hashtable<IAnonymousDelegate>();
          this.delegatesCachedInFields[targetField.InternedKey] = anonDel;
          if (this.sourceMethodBody.privateHelperFieldsToRemove == null) this.sourceMethodBody.privateHelperFieldsToRemove = new List<IFieldDefinition>();
          if (specializedTargetField != null) targetField = specializedTargetField.UnspecializedVersion;
          this.sourceMethodBody.privateHelperFieldsToRemove.Add(targetField.ResolvedField);
        }
        return;
      }
      if (this.closures != null && this.closures.Find(assignment.Source.Type.InternedKey) != null) {
        var targetField = assignment.Target.Definition as IFieldReference;
        if (targetField != null) {
          var specializedTargetField = targetField as ISpecializedFieldReference;
          if (this.sourceMethodBody.privateHelperFieldsToRemove == null) this.sourceMethodBody.privateHelperFieldsToRemove = new List<IFieldDefinition>();
          if (specializedTargetField != null) targetField = specializedTargetField.UnspecializedVersion;
          this.sourceMethodBody.privateHelperFieldsToRemove.Add(targetField.ResolvedField);
        }
      }
    }

    public override void RewriteChildren(ConditionalStatement conditionalStatement) {
      base.RewriteChildren(conditionalStatement);
      var trueBlock = conditionalStatement.TrueBranch as BlockStatement;
      if (trueBlock == null || trueBlock.Statements.Count != 1) return;
      if (!(conditionalStatement.FalseBranch is IEmptyStatement)) return;
      var equals = conditionalStatement.Condition as IEquality;
      if (equals == null || !(equals.RightOperand is IDefaultValue)) return;
      var binding = equals.LeftOperand as IBoundExpression;
      if (binding == null) return;
      var exprStat = trueBlock.Statements[0] as IExpressionStatement;
      if (exprStat == null) return;
      var assignment = exprStat.Expression as IAssignment;
      if (assignment == null) return;
      var anonDel = assignment.Source as AnonymousDelegate;
      if (anonDel == null) return;
      if (!TypeHelper.TypesAreEquivalent(assignment.Type, binding.Type)) return;
      var local = binding.Definition as LocalDefinition;
      if (local != null) {
        //Unfortunately the C# compiler does not mark such locals as being compiler generated.
        //So we'll use a heuristic to try and prevent us from deleting user written code.
        //The local should be (optionally) initialized to null and then assign the anonymous delegate
        uint numAssigns; 
        if (!this.numberOfAssignmentsToLocal.TryGetValue(local, out numAssigns) || numAssigns > 2) return;
        //The local should be tested for null and then used one more time to get the non-null value.
        uint numRefs;
        if (!this.numberOfReferencesToLocal.TryGetValue(local, out numRefs) || numRefs != 2) return;
        if (this.delegatesCachedInLocals == null) this.delegatesCachedInLocals = new Hashtable<LocalDefinition,AnonymousDelegate>();
        this.delegatesCachedInLocals.Add(local, anonDel);
      }
    }

    [ContractVerification(false)]
    public override IExpression Rewrite(ICreateDelegateInstance createDelegateInstance) {
      IMethodDefinition delegateMethodDefinition = createDelegateInstance.MethodToCallViaDelegate.ResolvedMethod;
      if (this.closures != null && this.closures.Find(delegateMethodDefinition.ContainingTypeDefinition.InternedKey) != null) {
        Contract.Assume(!(delegateMethodDefinition is Dummy));
        return this.ConvertToAnonymousDelegate(delegateMethodDefinition, createDelegateInstance.Type);
      }
      if (delegateMethodDefinition.Visibility == TypeMemberVisibility.Private && 
        AttributeHelper.Contains(delegateMethodDefinition.Attributes, this.host.PlatformType.SystemRuntimeCompilerServicesCompilerGeneratedAttribute)) {
        //The method that is enclosed in the delegate is private and it has been generated by a compiler. 
        //We assume that means that this delegate is an anonymous delegate and we'll rewrite the expression as such.
        //The anonymous delegate will not have captured anything other than the this parameter, otherwise it would be an instance method on a closure class.
        if (this.containingMethod.IsGeneric) {
          var genInst = delegateMethodDefinition as IGenericMethodInstance;
          if (genInst == null) return createDelegateInstance; //Anonymous delegates have to be generic in this case.
          if (this.GenericArgumentsDoNotMatchGenericParameters(genInst.GenericArguments)) return createDelegateInstance;
        }
        this.AddToPrivateHelperMethodsToRemove(MemberHelper.UninstantiateAndUnspecialize(delegateMethodDefinition));
        Contract.Assume(!(delegateMethodDefinition is Dummy));
        return this.ConvertToAnonymousDelegate(delegateMethodDefinition, createDelegateInstance.Type);
      }
      return createDelegateInstance;
    }

    public override ITypeReference Rewrite(ITypeReference typeReference) {
      return typeReference;
    }

    /// <summary>
    /// A compiler method that was generated from the body of an anonymous delegate inside a generic method will itself be generic and will be instantiated with the
    /// generic parameters of the generic method. Return false if this is not the case, so that the caller knows that it is not dealing with an anonymous method.
    /// </summary>
    private bool GenericArgumentsDoNotMatchGenericParameters(IEnumerable<ITypeReference> genericArguments) {
      Contract.Requires(genericArguments != null);

      var leftEnum = this.containingMethod.GenericParameters.GetEnumerator();
      var rightEnum = genericArguments.GetEnumerator();
      while (leftEnum.MoveNext()) {
        if (!rightEnum.MoveNext()) return true;
        if (!TypeHelper.TypesAreEquivalent(leftEnum.Current, rightEnum.Current)) return true;
      }
      return rightEnum.MoveNext();
    }

    private IExpression ConvertToAnonymousDelegate(IMethodDefinition closureMethod, ITypeReference delegateType) {
      Contract.Requires(closureMethod != null);
      Contract.Requires(!(closureMethod is Dummy));
      Contract.Requires(delegateType != null);

      AnonymousDelegate anonDel = new AnonymousDelegate();
      anonDel.CallingConvention = closureMethod.CallingConvention;
      anonDel.Parameters = new List<IParameterDefinition>(closureMethod.Parameters);
      var body = this.GetCopyOfBody(closureMethod);
      if (this.closureFieldMapper != null)
        this.closureFieldMapper.Traverse(body);
      anonDel.Body = this.Rewrite(body);
      anonDel.ReturnValueIsByRef = closureMethod.ReturnValueIsByRef;
      if (closureMethod.ReturnValueIsModified)
        anonDel.ReturnValueCustomModifiers = new List<ICustomModifier>(closureMethod.ReturnValueCustomModifiers);
      anonDel.ReturnType = closureMethod.Type;
      anonDel.Type = delegateType;
      new LocalReadAndWriteCounter(this.numberOfAssignmentsToLocal, this.numberOfReferencesToLocal).Traverse(anonDel.Body);
      return new ReparentAnonymousDelegateParametersAndLocals(this.host, this.containingMethod, closureMethod, anonDel).Rewrite(anonDel);
    }

    private IBlockStatement GetCopyOfBody(IMethodDefinition delegateMethod) {
      Contract.Requires(delegateMethod != null);
      Contract.Ensures(Contract.Result<IBlockStatement>() != null);

      var genericInstance = delegateMethod as IGenericMethodInstance;
      if (genericInstance != null) {
        //We want to avoid compiling the generic template to IL, specializing that IL to get IL for the generic method instance and then decompiling
        //the specialized IL to get a specialized block, and then copying the specialized block to get a body for the anonymous delegate.
        //So, we just get a copy of the template's already decompiled block and then specialize it.
        var block = this.GetCopyOfBody(genericInstance.GenericMethod.ResolvedMethod);
        return new MapGenericMethodParameters(this.host, genericInstance).Rewrite(block);
      }

      var specializedMethod = delegateMethod as ISpecializedMethodDefinition;
      if (specializedMethod != null) {
        //We are referring to a method of a closure class for a method that is generic (either by having its own generic parameters
        //or by being a member of a generic type (including non generic nested types of generic containing types).
        //We want avoid compiling specialized methods to IL and then decompiling those, just to get a specialized copy of the 
        //Hence we first get a copy of the compiled block of the unspecialized method.
        var block = this.GetCopyOfBody(specializedMethod.UnspecializedVersion);
        return new MapGenericTypeParameters(this.host, specializedMethod.ContainingTypeDefinition).Rewrite(block);
      }

      Contract.Assume(!delegateMethod.IsAbstract && !delegateMethod.IsExternal);
      IMethodBody methodBody = delegateMethod.Body;
      var alreadyDecompiledBody = methodBody as SourceMethodBody;
      if (alreadyDecompiledBody == null) {
        var alreadyDecompiledBody2 = methodBody as Microsoft.Cci.MutableCodeModel.SourceMethodBody;
        if (alreadyDecompiledBody2 == null) {
          //Getting here is a bit of a surprise, but only if we decompile an entire module.
          //If we are decompiling a single method at a time, the anonymous delegate method might not have a source body.
          return new SourceMethodBody(methodBody, this.host,
            this.sourceMethodBody.sourceLocationProvider, this.sourceMethodBody.localScopeProvider, this.sourceMethodBody.options).Block;
        } else {
          //On the whole, we don't expect to get here, but it could happen if the decompiler's client is decompiling code that been
          //decompiled and then copied.
          return new CodeDeepCopier(this.host, alreadyDecompiledBody2.SourceLocationProvider).Copy(alreadyDecompiledBody2.Block);
        }
      } else {
          return new CodeDeepCopier(this.host, alreadyDecompiledBody.sourceLocationProvider).Copy(alreadyDecompiledBody.Block);
      }
    }

    private void AddToPrivateHelperMethodsToRemove(IMethodDefinition methodToRemove) {
      Contract.Requires(methodToRemove != null);

      if (this.sourceMethodBody.privateHelperMethodsToRemove == null)
        this.sourceMethodBody.privateHelperMethodsToRemove = new Dictionary<uint, IMethodDefinition>();
      this.sourceMethodBody.privateHelperMethodsToRemove[methodToRemove.InternedKey] = methodToRemove;
    }

  }

  internal class ReparentAnonymousDelegateParametersAndLocals : CodeRewriter {

    internal ReparentAnonymousDelegateParametersAndLocals(IMetadataHost host, IMethodDefinition method, IMethodDefinition closureMethod, AnonymousDelegate anonymousDelegate)
      : base(host) {
      Contract.Requires(host != null);
      Contract.Requires(method != null);
      Contract.Requires(closureMethod != null);
      Contract.Requires(anonymousDelegate != null);

      this.method = method;
      this.closureMethod = closureMethod;
      this.anonymousDelegate = anonymousDelegate;
    }

    IMethodDefinition method;
    IMethodDefinition closureMethod;
    AnonymousDelegate anonymousDelegate;
    Hashtable<IParameterDefinition> parameterMap = new Hashtable<IParameterDefinition>();

    [ContractInvariantMethod]
    private void ObjectInvariant() {
      Contract.Invariant(this.method != null);
      Contract.Invariant(this.closureMethod != null);
      Contract.Invariant(this.anonymousDelegate != null);
      Contract.Invariant(this.parameterMap != null);
    }

    public override IParameterDefinition Rewrite(IParameterDefinition parameterDefinition) {
      //we need to copy the parameters because they still come from the closure method.
      var copy = new ParameterDefinition();
      copy.Copy(parameterDefinition, this.host.InternFactory);
      copy.ContainingSignature = this.anonymousDelegate;
      this.parameterMap.Add((uint)parameterDefinition.Name.UniqueKey, copy);
      return copy;
    }

    public override object RewriteReference(IParameterDefinition parameterDefinition) {
      var mappedParameter = this.parameterMap.Find((uint)parameterDefinition.Name.UniqueKey);
      Contract.Assume(mappedParameter != null);
      return mappedParameter;
    }

    public override void RewriteChildren(LocalDefinition localDefinition) {
      //The locals have already been copied. They just have to get fixed up.
      localDefinition.MethodDefinition = this.method;
    }

    public override void RewriteChildren(ThisReference thisReference) {
      thisReference.Type = this.closureMethod.ContainingTypeDefinition;
    }

  }

  internal class MapGenericMethodParameters : CodeRewriter {

    internal MapGenericMethodParameters(IMetadataHost host, IGenericMethodInstance genericMethodInstance)
      : base(host, copyAndRewriteImmutableReferences: true) {
      Contract.Requires(host != null);
      Contract.Requires(genericMethodInstance != null);

      this.genericArguments = IteratorHelper.GetAsArray(genericMethodInstance.GenericArguments);
    }

    ITypeReference[] genericArguments;

    [ContractInvariantMethod]
    private void ObjectInvariant() {
      Contract.Invariant(this.genericArguments != null);
    }

    public override ILocalDefinition Rewrite(ILocalDefinition localDefinition) {
      var capturedDef = localDefinition as CapturedLocalDefinition;
      if (capturedDef != null) {
        Contract.Assume(capturedDef.capturingField != null);
        capturedDef.capturingField = this.Rewrite(capturedDef.capturingField);
      }
      return base.Rewrite(localDefinition);
    }

    public override ITypeReference Rewrite(IGenericMethodParameterReference genericMethodParameterReference) {
      Contract.Assume(genericMethodParameterReference.Index < this.genericArguments.Length);
      var genArg = this.genericArguments[genericMethodParameterReference.Index];
      Contract.Assume(genArg != null);
      return genArg;
    }

    public override IMethodReference Rewrite(IMethodReference methodReference) {
      //Method references contain generic parameter references that should not be mapped.
      return methodReference;
    }

  }

  internal class MapGenericTypeParameters : CodeRewriter {

    internal MapGenericTypeParameters(IMetadataHost host, ITypeDefinition type)
      : base(host, copyAndRewriteImmutableReferences: true) {
      Contract.Requires(host != null);
      Contract.Requires(type != null);

      this.type = type;
    }

    ITypeDefinition type;
    Hashtable<ITypeReference> genericArgumentsMap = new Hashtable<ITypeReference>();

    [ContractInvariantMethod]
    private void ObjectInvariant() {
      Contract.Invariant(this.type != null);
      Contract.Invariant(this.genericArgumentsMap != null);
    }

    public override IBlockStatement Rewrite(IBlockStatement block) {
      while (true) {
        var specializedNestedType = this.type as ISpecializedNestedTypeDefinition;
        if (specializedNestedType != null) {
          this.type = specializedNestedType.ContainingTypeDefinition;
          //Carry on. At some point, the container must be a generic type instance.
        } else {
          var genInstance = this.type as IGenericTypeInstance;
          if (genInstance != null) {
            var argEnum = genInstance.GenericArguments.GetEnumerator();
            var parEnum = genInstance.GenericType.ResolvedType.GenericParameters.GetEnumerator();
            while (argEnum.MoveNext() && parEnum.MoveNext())
              this.genericArgumentsMap.Add(parEnum.Current.InternedKey, argEnum.Current);
            return base.Rewrite(block);
          } else {
            Contract.Assume(false); //specialized methods should come either from specialized nested types, or from generic types intances.
            return block;
          }
        }
      }
    }

    public override ITypeReference Rewrite(IGenericTypeParameterReference genericTypeParameterReference) {
      return this.genericArgumentsMap.Find(genericTypeParameterReference.InternedKey)??genericTypeParameterReference;
    }

    public override ILocalDefinition Rewrite(ILocalDefinition localDefinition) {
      var capturedDef = localDefinition as CapturedLocalDefinition;
      if (capturedDef != null) {
        Contract.Assume(capturedDef.capturingField != null);
        capturedDef.capturingField = this.Rewrite(capturedDef.capturingField);
      }
      return base.Rewrite(localDefinition);
    }

  }

  internal class LocalReadAndWriteCounter : CodeTraverser {

    internal LocalReadAndWriteCounter(HashtableForUintValues<object> numberOfAssignmentsToLocal, HashtableForUintValues<object> numberOfReferencesToLocal) {
      Contract.Requires(numberOfAssignmentsToLocal != null);
      Contract.Requires(numberOfReferencesToLocal != null);

      this.numberOfAssignmentsToLocal = numberOfAssignmentsToLocal;
      this.numberOfReferencesToLocal = numberOfReferencesToLocal;
    }

    HashtableForUintValues<object> numberOfAssignmentsToLocal;
    HashtableForUintValues<object> numberOfReferencesToLocal;

    [ContractInvariantMethod]
    private void ObjectInvariant() {
      Contract.Invariant(this.numberOfAssignmentsToLocal != null);
      Contract.Invariant(this.numberOfReferencesToLocal != null);
    }

    public override void TraverseChildren(IAddressableExpression addressableExpression) {
      base.TraverseChildren(addressableExpression);
      var local = addressableExpression.Definition as ILocalDefinition;
      if (local == null) return;
      this.numberOfAssignmentsToLocal[local]++;
    }

    public override void TraverseChildren(ILocalDeclarationStatement localDeclarationStatement) {
      base.TraverseChildren(localDeclarationStatement);
      if (localDeclarationStatement.InitialValue == null) return;
      this.numberOfAssignmentsToLocal[localDeclarationStatement.LocalVariable]++;
    }

    public override void TraverseChildren(IBoundExpression boundExpression) {
      base.TraverseChildren(boundExpression);
      var local = boundExpression.Definition as ILocalDefinition;
      if (local == null) return;
      this.numberOfReferencesToLocal[local]++;
    }

    public override void TraverseChildren(ITargetExpression targetExpression) {
      base.TraverseChildren(targetExpression);
      var local = targetExpression.Definition as ILocalDefinition;
      if (local == null) return;
      this.numberOfAssignmentsToLocal[local]++;
    }

  }
}