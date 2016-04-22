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
using Microsoft.Cci.MutableCodeModel;
using System.Diagnostics.Contracts;
using Microsoft.Cci.UtilityDataStructures;
using System;

namespace Microsoft.Cci.ILToCodeModel {

  internal class LockReplacer : CodeTraverser {

    IMetadataHost host;
    ISourceLocationProvider/*?*/ sourceLocationProvider;
    HashtableForUintValues<object> numberOfReferencesToLocal = new HashtableForUintValues<object>();
    HashtableForUintValues<object> numberOfAssignmentsToLocal = new HashtableForUintValues<object>();
    SetOfObjects bindingsThatMakeALastUseOfALocalVersion = new SetOfObjects();
    MethodReference monitorEnter;
    MethodReference monitorExit;

    internal LockReplacer(SourceMethodBody sourceMethodBody) {
      Contract.Requires(sourceMethodBody != null);
      this.host = sourceMethodBody.host; Contract.Assume(sourceMethodBody.host != null);
      this.sourceLocationProvider = sourceMethodBody.sourceLocationProvider;
      this.numberOfReferencesToLocal = sourceMethodBody.numberOfReferencesToLocal; Contract.Assume(sourceMethodBody.numberOfReferencesToLocal != null);
      this.numberOfAssignmentsToLocal = sourceMethodBody.numberOfAssignmentsToLocal; Contract.Assume(sourceMethodBody.numberOfAssignmentsToLocal != null);
      this.bindingsThatMakeALastUseOfALocalVersion = sourceMethodBody.bindingsThatMakeALastUseOfALocalVersion; Contract.Assume(sourceMethodBody.bindingsThatMakeALastUseOfALocalVersion != null);
      var systemThreading = new Immutable.NestedUnitNamespaceReference(this.host.PlatformType.SystemObject.ContainingUnitNamespace,
        this.host.NameTable.GetNameFor("Threading"));
      var systemThreadingMonitor = new Immutable.NamespaceTypeReference(this.host, systemThreading, this.host.NameTable.GetNameFor("Monitor"), 0,
        isEnum: false, isValueType: false, typeCode: PrimitiveTypeCode.NotPrimitive);
      var parameters = new IParameterTypeInformation[2];
      this.monitorEnter = new MethodReference(this.host, systemThreadingMonitor, CallingConvention.Default, this.host.PlatformType.SystemVoid,
        this.host.NameTable.GetNameFor("Enter"), 0, parameters);
      parameters[0] = new SimpleParameterTypeInformation(monitorEnter, 0, this.host.PlatformType.SystemObject);
      parameters[1] = new SimpleParameterTypeInformation(monitorEnter, 1, this.host.PlatformType.SystemBoolean, isByReference: true);
      this.monitorExit = new MethodReference(this.host, systemThreadingMonitor, CallingConvention.Default, this.host.PlatformType.SystemVoid,
        this.host.NameTable.GetNameFor("Exit"), 0, this.host.PlatformType.SystemObject);

    }

    [ContractInvariantMethod]
    private void ObjectInvariant() {
      Contract.Invariant(this.host != null);
      Contract.Invariant(this.numberOfReferencesToLocal != null);
      Contract.Invariant(this.numberOfAssignmentsToLocal != null);
      Contract.Invariant(this.bindingsThatMakeALastUseOfALocalVersion != null);
      Contract.Invariant(this.monitorEnter != null);
      Contract.Invariant(this.monitorExit != null);
    }

    public override void TraverseChildren(IBlockStatement block) {
      base.TraverseChildren(block);
      Contract.Assume(block is BlockStatement);
      var decompiledBlock = (BlockStatement)block;
      var statements = decompiledBlock.Statements;
      for (int i = 0; i < statements.Count-1; i++) {
        //TODO: need to deal with patterns where the decl and the assignment are separated.
        var loclDecl = statements[i] as LocalDeclarationStatement;
        if (loclDecl == null) continue;
        var local = loclDecl.LocalVariable;
        if (local.Type.TypeCode != PrimitiveTypeCode.Boolean) continue;
        //if (this.sourceLocationProvider != null) {
        //  bool isCompilerGenerated;
        //  var sourceName = this.sourceLocationProvider.GetSourceNameFor(local, out isCompilerGenerated);
        //  if (!isCompilerGenerated) continue;
        //}
        var tryFinallyStatement = statements[i+1] as TryCatchFinallyStatement;
        if (tryFinallyStatement == null) continue;
        if (tryFinallyStatement.FinallyBody == null || tryFinallyStatement.CatchClauses.Count > 0 || tryFinallyStatement.FaultBody != null) continue;
        ILocalDefinition monitorVar;
        var monitorObject = this.GetMonitor(tryFinallyStatement.TryBody, local, out monitorVar);
        if (monitorObject == null) continue;
        if (!this.FinallyBodyCallsMonitorExit(tryFinallyStatement.FinallyBody, local, monitorVar)) continue;
        this.numberOfAssignmentsToLocal[local]-=2;
        this.numberOfReferencesToLocal[local]-=2;
        this.numberOfAssignmentsToLocal[monitorVar]--;
        this.numberOfReferencesToLocal[monitorVar]--;
        var tryStatements = ((BlockStatement)tryFinallyStatement.TryBody).Statements;
        tryStatements.RemoveRange(0, 3);
        var body = new BlockStatement() { Statements = tryStatements };
        var lockStatement = new LockStatement() { Guard = monitorObject, Body = body, Locations = tryFinallyStatement.Locations };
        statements[i] = lockStatement;
        statements.RemoveAt(i+1);
        if (this.numberOfAssignmentsToLocal[monitorVar] == 0) {
          for (int j = 0; j < statements.Count; j++) {
            var ldecl = statements[j] as LocalDeclarationStatement;
            if (ldecl == null) continue;
            if (ldecl.LocalVariable != monitorVar) continue;
            statements.RemoveAt(j);
            break;
          }
        }
      }
    }

    private IExpression/*?*/ GetMonitor(IBlockStatement block, ILocalDefinition local, out ILocalDefinition monitorVar) {
      Contract.Ensures(Contract.Result<IExpression>() == null || Contract.ValueAtReturn<ILocalDefinition>(out monitorVar) != null);
      monitorVar = null;
      Contract.Assume(block is BlockStatement);
      var decompiledBlock = (BlockStatement)block;
      var statements = decompiledBlock.Statements;
      var n = statements.Count;
      if (n < 3) return null;
      var pushStatement = statements[0] as PushStatement;
      if (pushStatement == null) return null;
      var exprStatement1 = statements[1] as ExpressionStatement;
      if (exprStatement1 == null) return null;
      var assignment1 = exprStatement1.Expression as Assignment;
      if (assignment1 == null || !(assignment1.Source is IDupValue)) return null;
      monitorVar = assignment1.Target.Definition as ILocalDefinition;
      if (monitorVar == null) return null;
      var exprStatement2 = statements[2] as ExpressionStatement;
      if (exprStatement2 == null) return null;
      var methodCall = exprStatement2.Expression as MethodCall;
      if (methodCall == null) return null;
      if (methodCall.Arguments.Count != 2 || !(methodCall.Arguments[0] is IPopValue)) return null;
      if (methodCall.MethodToCall.InternedKey != this.monitorEnter.InternedKey) return null;
      return pushStatement.ValueToPush;
    }

    private bool FinallyBodyCallsMonitorExit(IBlockStatement block, ILocalDefinition monitorTakenLocal, ILocalDefinition monitorVar) {
      Contract.Assume(block is BlockStatement);
      var decompiledBlock = (BlockStatement)block;
      var statements = decompiledBlock.Statements;
      if (statements.Count < 2) return false;
      var cond = statements[0] as ConditionalStatement;
      if (cond == null) return false;
      if (!(cond.FalseBranch is EmptyStatement)) return false;
      var boundExpr = cond.Condition as BoundExpression;
      if (boundExpr == null) return false;
      if (boundExpr.Definition != monitorTakenLocal) return false;
      if (!this.bindingsThatMakeALastUseOfALocalVersion.Contains(boundExpr)) return false;
      var trueBlock = cond.TrueBranch as BlockStatement;
      if (trueBlock == null) return false;
      if (trueBlock.Statements.Count < 1) return false;
      var exprStat = trueBlock.Statements[0] as ExpressionStatement;
      if (exprStat == null) return false;
      var methodCall = exprStat.Expression as MethodCall;
      if (methodCall == null) return false;
      if (methodCall.MethodToCall.InternedKey != this.monitorExit.InternedKey) return false;
      if (methodCall.Arguments.Count != 1) return false;
      var boundExpr2 = methodCall.Arguments[0] as BoundExpression;
      if (boundExpr2 == null) return false;
      if (boundExpr2.Definition != monitorVar) return false;
      if (!this.bindingsThatMakeALastUseOfALocalVersion.Contains(boundExpr2)) return false;
      return true;
    }
  }
}
