//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System.Collections.Generic;
using Microsoft.Cci.MutableCodeModel;

namespace Microsoft.Cci.ILToCodeModel {

  internal class CompilationArtifactRemover : CodeMutator {

    internal CompilationArtifactRemover(SourceMethodBody sourceMethodBody)
      : base(sourceMethodBody.host, true)
    {
      this.containingType = sourceMethodBody.ilMethodBody.MethodDefinition.ContainingTypeDefinition;
      this.sourceMethodBody = sourceMethodBody;
    }

    internal Dictionary<IFieldReference, object> capturedLocalOrParameter = new Dictionary<IFieldReference, object>();
    ITypeDefinition containingType;
    ILocalDefinition currentClosureLocal = Dummy.LocalVariable;
    SourceMethodBody sourceMethodBody;
    Dictionary<ILocalDefinition, IExpression> expressionToSubstituteForCompilerGeneratedSingleAssignmentLocal = new Dictionary<ILocalDefinition, IExpression>();
    Dictionary<IParameterDefinition, IParameterDefinition> parameterMap = new Dictionary<IParameterDefinition, IParameterDefinition>();
    internal Dictionary<int, bool>/*?*/ referencedLabels;

    public override IAddressableExpression Visit(AddressableExpression addressableExpression) {
      var field = addressableExpression.Definition as IFieldReference;
      if (field != null) {
        object localOrParameter = null;
        if (this.capturedLocalOrParameter.TryGetValue(field, out localOrParameter)) {
          addressableExpression.Definition = localOrParameter;
          addressableExpression.Instance = null;
          return addressableExpression;
        }
      }
      return base.Visit(addressableExpression);
    }

    public override ITargetExpression Visit(TargetExpression targetExpression) {
      var field = targetExpression.Definition as IFieldReference;
      if (field != null) {
        object localOrParameter = null;
        if (this.capturedLocalOrParameter.TryGetValue(field, out localOrParameter)) {
          targetExpression.Definition = localOrParameter;
          targetExpression.Instance = null;
          return targetExpression;
        }
      }
      return base.Visit(targetExpression);
    }

    public override IBlockStatement Visit(BlockStatement blockStatement) {
      if (this.referencedLabels == null) {
        LabelReferenceFinder finder = new LabelReferenceFinder();
        finder.Visit(blockStatement.Statements);
        this.referencedLabels = finder.referencedLabels;
      }
      ILocalDefinition savedClosureLocal = this.currentClosureLocal;
      this.FindCapturedLocals(blockStatement.Statements);
      IBlockStatement result = base.Visit(blockStatement);
      this.currentClosureLocal = savedClosureLocal;
      return result;
    }

    private void FindCapturedLocals(List<IStatement> statements) {
      LocalDeclarationStatement/*?*/ locDecl = null;
      INestedTypeReference/*?*/ closureType = null;
      int i = 0;
      while (i < statements.Count && closureType == null) {
        locDecl = statements[i++] as LocalDeclarationStatement;
        if (locDecl == null || !(locDecl.InitialValue is ICreateObjectInstance)) continue;
        closureType = locDecl.LocalVariable.Type as INestedTypeReference;
      }
      if (closureType == null) return;
      if (closureType.ContainingType.ResolvedType != this.containingType) return;
      if (!AttributeHelper.Contains(closureType.ResolvedType.Attributes, this.containingType.PlatformType.SystemRuntimeCompilerServicesCompilerGeneratedAttribute))
        return;
      this.currentClosureLocal = locDecl.LocalVariable;
      statements.RemoveAt(i-1);
      for (int j = i-1; j < statements.Count; j++) {
        IExpressionStatement/*?*/ es = statements[j] as IExpressionStatement;
        if (es == null) break;
        IAssignment/*?*/ assignment = es.Expression as IAssignment;
        if (assignment == null) break;
        IFieldReference/*?*/ closureField = assignment.Target.Definition as IFieldReference;
        if (closureField == null || !TypeHelper.TypesAreEquivalent(closureField.ContainingType, closureType)) break;
        IBoundExpression/*?*/ binding = assignment.Source as IBoundExpression;
        if (binding == null || !(binding.Definition is ILocalDefinition || binding.Definition is IParameterDefinition)) break;
        this.capturedLocalOrParameter.Add(closureField, binding.Definition);
        statements.RemoveAt(j--);
      }
      foreach (var field in closureType.ResolvedType.Fields) {
        if (this.capturedLocalOrParameter.ContainsKey(field)) continue;
        var newLocal = new LocalDefinition() { Name = field.Name, Type = field.Type };
        var newLocalDecl = new LocalDeclarationStatement() { LocalVariable = newLocal };
        statements.Insert(i, newLocalDecl);
        this.capturedLocalOrParameter.Add(field, newLocal);
      }
    }

    public override IExpression Visit(BoundExpression boundExpression) {
      ILocalDefinition/*?*/ local = boundExpression.Definition as ILocalDefinition;
      if (local != null) {
        IExpression substitute = boundExpression;
        if (this.expressionToSubstituteForCompilerGeneratedSingleAssignmentLocal.TryGetValue(local, out substitute))
          return substitute;
      }
      IFieldReference/*?*/ closureField = boundExpression.Definition as IFieldReference;
      if (closureField != null) {
        object/*?*/ localOrParameter = null;
        this.capturedLocalOrParameter.TryGetValue(closureField.ResolvedField, out localOrParameter);
        if (localOrParameter != null) {
          boundExpression.Definition = localOrParameter;
          boundExpression.Instance = null;
          return boundExpression;
        }
      }
      IParameterDefinition/*?*/ parameter = boundExpression.Definition as IParameterDefinition;
      if (parameter != null) {
        IParameterDefinition parToSubstitute;
        if (this.parameterMap.TryGetValue(parameter, out parToSubstitute)){
          boundExpression.Definition = parToSubstitute;
          return boundExpression;
        }
      }
      return base.Visit(boundExpression);
    }

    public override IExpression Visit(CreateDelegateInstance createDelegateInstance) {
      BoundExpression/*?*/ bexpr = createDelegateInstance.Instance as BoundExpression;
      if (bexpr != null && bexpr.Definition == this.currentClosureLocal)
        return ConvertToAnonymousDelegate(createDelegateInstance);
      CompileTimeConstant/*?*/ cc = createDelegateInstance.Instance as CompileTimeConstant;
      if (cc != null && cc.Value == null && 
        TypeHelper.TypesAreEquivalent(createDelegateInstance.MethodToCallViaDelegate.ContainingType, this.containingType) &&
        AttributeHelper.Contains(createDelegateInstance.MethodToCallViaDelegate.ResolvedMethod.Attributes, 
          this.containingType.PlatformType.SystemRuntimeCompilerServicesCompilerGeneratedAttribute))
        return ConvertToAnonymousDelegate(createDelegateInstance);
      return base.Visit(createDelegateInstance);
    }

    private IExpression ConvertToAnonymousDelegate(CreateDelegateInstance createDelegateInstance) {
      IMethodDefinition closureMethod = createDelegateInstance.MethodToCallViaDelegate.ResolvedMethod;
      AnonymousDelegate anonDel = new AnonymousDelegate();
      anonDel.CallingConvention = closureMethod.CallingConvention;
      anonDel.Parameters = new List<IParameterDefinition>(closureMethod.Parameters);
      for (int i = 0, n = anonDel.Parameters.Count; i < n; i++) {
        IParameterDefinition closureMethodPar = anonDel.Parameters[i];
        ParameterDefinition par = new ParameterDefinition();
        this.parameterMap.Add(closureMethodPar, par);
        par.Copy(closureMethodPar, this.host.InternFactory);
        par.ContainingSignature = anonDel;
        anonDel.Parameters[i] = par;
      }
      anonDel.Body = new SourceMethodBody(closureMethod.Body, this.sourceMethodBody.host, this.sourceMethodBody.contractProvider, this.sourceMethodBody.pdbReader).Block;
      anonDel.ReturnValueIsByRef = closureMethod.ReturnValueIsByRef;
      if (closureMethod.ReturnValueIsModified)
        anonDel.ReturnValueCustomModifiers = new List<ICustomModifier>(closureMethod.ReturnValueCustomModifiers);
      anonDel.ReturnType = closureMethod.Type;
      anonDel.Type = createDelegateInstance.Type;
      return this.Visit(anonDel);
    }

    public override IExpression Visit(CreateObjectInstance createObjectInstance) {
      if (createObjectInstance.Arguments.Count == 2) {
        AddressOf/*?*/ aexpr = createObjectInstance.Arguments[1] as AddressOf;
        if (aexpr != null && aexpr.Expression.Definition is IMethodReference) {
          CreateDelegateInstance createDel = new CreateDelegateInstance();
          createDel.Instance = createObjectInstance.Arguments[0];
          createDel.MethodToCallViaDelegate = (IMethodReference)aexpr.Expression.Definition;
          createDel.Locations = createObjectInstance.Locations;
          createDel.Type = createObjectInstance.Type;
          return this.Visit(createDel);
        }
      }
      return base.Visit(createObjectInstance);
    }

    public override IFieldReference Visit(IFieldReference fieldReference) {
      return fieldReference;
    }

    public override IMethodReference Visit(IMethodReference methodReference) {
      return methodReference;
    }

    public override ITypeReference Visit(ITypeReference typeReference) {
      return typeReference;
    }

    public override IStatement Visit(LabeledStatement labeledStatement) {
      if (!this.referencedLabels.ContainsKey(labeledStatement.Label.UniqueKey))
        return CodeDummy.Block;
      return base.Visit(labeledStatement);
    }

    public override IStatement Visit(LocalDeclarationStatement localDeclarationStatement) {
      ILocalDefinition local = localDeclarationStatement.LocalVariable;
      base.Visit(localDeclarationStatement);
      int numberOfAssignments = 0;
      if (!this.sourceMethodBody.numberOfAssignments.TryGetValue(local, out numberOfAssignments) || numberOfAssignments > 1)
        return localDeclarationStatement;
      int numReferences = 0;
      if (!this.sourceMethodBody.numberOfReferences.TryGetValue(local, out numReferences) || numReferences > 1) 
        return localDeclarationStatement;
      if (this.sourceMethodBody.pdbReader != null) {
        bool isCompilerGenerated = false;
        this.sourceMethodBody.pdbReader.GetSourceNameFor(local, out isCompilerGenerated);
        if (!isCompilerGenerated) return localDeclarationStatement;
      }
      if (localDeclarationStatement.InitialValue != null)
        this.expressionToSubstituteForCompilerGeneratedSingleAssignmentLocal.Add(local, localDeclarationStatement.InitialValue);
      return CodeDummy.Block; //Causes the caller to omit this statement from the containing statement list.
    }

    public override IExpression Visit(LogicalNot logicalNot) {
      if (logicalNot.Type == Dummy.TypeReference)
        return InvertCondition(this.Visit(logicalNot.Operand));
      else if (logicalNot.Operand.Type.TypeCode == PrimitiveTypeCode.Int32)
        return new Equality() { 
          LeftOperand = logicalNot.Operand, 
          RightOperand = new CompileTimeConstant() { Value = 0, Type = this.host.PlatformType.SystemInt32 }, 
          Type = this.host.PlatformType.SystemBoolean 
        };
      else
        return base.Visit(logicalNot);
    }

    private static IExpression InvertCondition(IExpression condition) {
      IEquality/*?*/ equality = condition as IEquality;
      if (equality != null) {
        if (equality.LeftOperand.Type.TypeCode == PrimitiveTypeCode.Boolean) {
          if (ExpressionHelper.IsIntegralZero(equality.RightOperand))
            return equality.LeftOperand;
        }
      }
      LogicalNot logicalNot = new LogicalNot();
      logicalNot.Operand = condition;
      logicalNot.Type = condition.Type.PlatformType.SystemBoolean;
      return logicalNot;
    }

    public override IStatement Visit(ReturnStatement returnStatement) {
      if (returnStatement.Expression != null)
        returnStatement.Expression = this.Visit(returnStatement.Expression);
      if (this.sourceMethodBody.MethodDefinition.Type.TypeCode == PrimitiveTypeCode.Boolean) {
        CompileTimeConstant/*?*/ cc = returnStatement.Expression as CompileTimeConstant;
        if (cc != null) {
          if (ExpressionHelper.IsIntegralZero(cc))
            cc.Value = false;
          else
            cc.Value = true;
          cc.Type =  this.containingType.PlatformType.SystemBoolean;
        }
      }
      return returnStatement;
    }

  }

  internal class LabelReferenceFinder : BaseCodeTraverser {

    internal Dictionary<int, bool> referencedLabels = new Dictionary<int, bool>();

    public override void Visit(IGotoStatement gotoStatement) {
      this.referencedLabels[gotoStatement.TargetStatement.Label.UniqueKey] = true;
    }

  }

}