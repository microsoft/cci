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
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Microsoft.Cci.Immutable;
using Microsoft.Cci.UtilityDataStructures;

namespace Microsoft.Cci.Optimization {

  /// <summary>
  /// A rewriter for method bodies that inlines calls to methods identified by the rewriter client via a call back.
  /// </summary>
  public class Inliner : ILRewriter {

    /// <summary>
    /// A rewriter for method bodies that inlines calls to methods identified by the rewriter client via a call back.
    /// </summary>
    /// <param name="host">An object representing the application that is hosting this mutator. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.</param>
    /// <param name="inlineSelector">
    /// Returns zero or more method definitions that should be inlined at a given call site. Zero methods means no inlining. For non virtual calls, one method means that the call
    /// should be inlined. For virtual calls, one or methods means that the call site should do call site type tests to avoid virtual calls for the returned methods.
    /// A subsequent call to ShouldInline, using one of the method definitions as the methodBeingCalled parameter can be used to determine if the call following the type test
    /// should be inline or not.
    /// </param>
    /// <param name="sourceLocationProvider">An object that can map some kinds of ILocation objects to IPrimarySourceLocation objects. May be null.</param>
    /// <param name="localScopeProvider">An object that can provide information about the local scopes of a method. May be null.</param>
    public Inliner(IMetadataHost host, ShouldInline inlineSelector, ILocalScopeProvider/*?*/ localScopeProvider, ISourceLocationProvider/*?*/ sourceLocationProvider) 
      : base(host, localScopeProvider, sourceLocationProvider) {
      Contract.Requires(host != null);
      Contract.Requires(inlineSelector != null);

      this.inlineSelector = inlineSelector;
      this.method = Dummy.MethodDefinition;
    }

    [ContractInvariantMethod]
    private void ObjectInvariant() {
      Contract.Invariant(inlineSelector != null);
      Contract.Invariant(this.method != null);
      Contract.Invariant(this.localFor != null);
    }

    ShouldInline inlineSelector;
    IMethodDefinition method;
    Hashtable<IParameterDefinition, GeneratorLocal> localFor = new Hashtable<IParameterDefinition, GeneratorLocal>();
    GeneratorLocal/*?*/ localForThis;
    ILGeneratorLabel/*?*/ returnLabel;
    ushort combinedMaxStack;

    /// <summary>
    /// </summary>
    /// <param name="methodBody"></param>
    /// <returns></returns>
    public override IMethodBody Rewrite(IMethodBody methodBody) {
      this.combinedMaxStack = methodBody.MaxStack;
      this.method = methodBody.MethodDefinition;
      return base.Rewrite(methodBody);
    }

    /// <summary>
    /// Emits the given operation at the current position of the new IL stream. Also tracks any referenced local definitions,
    /// so that this.localVariables will contain the exact list of locals used in the new method body.
    /// </summary>
    protected override void EmitOperation(IOperation operation) {
      switch (operation.OperationCode) {
        case OperationCode.Call:
          Contract.Assume(operation.Value is IMethodReference);
          var methodsToInline = this.inlineSelector(this.method, operation.Offset, (IMethodReference)operation.Value);
          Contract.Assume(methodsToInline != null);
          if (methodsToInline.Count == 1) {
            var methodToInline = methodsToInline[0];
            Contract.Assume(methodToInline != null && !methodToInline.IsAbstract && !methodToInline.IsExternal);
            this.Inline(methodToInline.Body);
            return;
          }
          break;

        //TODO: virtual calls

        case OperationCode.Ldarg:
        case OperationCode.Ldarg_0:
        case OperationCode.Ldarg_1:
        case OperationCode.Ldarg_2:
        case OperationCode.Ldarg_3:
        case OperationCode.Ldarg_S:
          if (this.returnLabel == null) break;
          this.EmitMappedLocalInsteadOfArgument(OperationCode.Ldloc, operation.Value);
          return;
        case OperationCode.Ldarga:
        case OperationCode.Ldarga_S:
          if (this.returnLabel == null) break;
          this.EmitMappedLocalInsteadOfArgument(OperationCode.Ldloca, operation.Value);
          return;
        case OperationCode.Starg:
        case OperationCode.Starg_S:
          if (this.returnLabel == null) break;
          this.EmitMappedLocalInsteadOfArgument(OperationCode.Stloc, operation.Value);
          return;

        case OperationCode.Ret:
          if (this.returnLabel != null) {
            this.Generator.Emit(OperationCode.Br, this.returnLabel);
            return;
          }
          break;
      }

      base.EmitOperation(operation);
    }

    private void EmitMappedLocalInsteadOfArgument(OperationCode opCodeToUse, object parameter) {
      var parDef = parameter as IParameterDefinition;
      var local = parDef == null ? this.localForThis : this.localFor[parDef];
      Contract.Assume(local != null);
      this.Generator.Emit(opCodeToUse, local);
    }

    [ContractVerification(false)] //Times out
    private void Inline(IMethodBody methodBody) {
      Contract.Requires(methodBody != null);

      var savedCombinedMaxStack = this.combinedMaxStack;
      this.combinedMaxStack += methodBody.MaxStack;
      if (this.combinedMaxStack > this.maxStack) this.maxStack = this.combinedMaxStack;

      var savedLocalForThis = this.localForThis;
      var nametable = this.Host.NameTable;
      var method = methodBody.MethodDefinition;
      var n = method.ParameterCount;
      if (!method.IsStatic) n++;
      var temps = new GeneratorLocal[n];
      for (ushort i = 0; i < n; i++) temps[i] = new GeneratorLocal() { MethodDefinition = method };
      var j = 0;
      if (!method.IsStatic) {
        var temp0 = temps[0];
        Contract.Assume(temp0 != null);
        temp0.Name = nametable.GetNameFor("this");
        temp0.Type = method.ContainingTypeDefinition;
        if (method.ContainingTypeDefinition.IsValueType)
          temp0.Type = ManagedPointerType.GetManagedPointerType(temp0.Type, this.Host.InternFactory);
        j = 1;
        this.localForThis = temp0;
      }
      foreach (var par in methodBody.MethodDefinition.Parameters) {
        Contract.Assume(par != null);
        Contract.Assume(j < n);
        var tempj = temps[j++];
        Contract.Assume(tempj != null);
        this.localFor[par] = tempj;
        tempj.Name = par.Name;
        tempj.Type = par.Type;
        if (par.IsByReference)
          tempj.Type = ManagedPointerType.GetManagedPointerType(tempj.Type, this.Host.InternFactory);
      }
      this.Generator.BeginScope();
      for (int i = n-1; i >= 0; i--) {
        var temp = temps[i];
        Contract.Assume(temp != null);
        this.Generator.Emit(OperationCode.Stloc, temp);
        this.TrackLocal(temp);
        this.Generator.AddVariableToCurrentScope(temp);
      }

      var savedReturnLabel = this.returnLabel;
      var returnLabel = this.returnLabel = new ILGeneratorLabel();
      this.EmitMethodBody(methodBody);
      this.Generator.MarkLabel(returnLabel);
      this.returnLabel = savedReturnLabel;
      this.localForThis = savedLocalForThis;
      this.Generator.EndScope();
      this.combinedMaxStack = savedCombinedMaxStack;

    }

  }

  /// <summary>
  /// Returns zero or more method definitions that should be inlined at the given call site. Zero methods means no inlining. For non virtual calls, one method means that the call
  /// should be inlined. For virtual calls, one or methods means that the call site should do call site type tests to avoid virtual calls for the returned methods.
  /// A subsequent call to ShouldInline, using one of the method definitions as the methodBeingCalled parameter can be used to determine if the call following the type test
  /// should be inline or not.
  /// </summary>
  /// <param name="callingMethod">The method into which the called method should be inlined, if so desired.</param>
  /// <param name="offsetOfCall">The offset in the calling method where the inlining should take place.</param>
  /// <param name="methodBeingCalled">The method being called.</param>
  public delegate IList<IMethodDefinition> ShouldInline(IMethodDefinition callingMethod, uint offsetOfCall, IMethodReference methodBeingCalled);


}