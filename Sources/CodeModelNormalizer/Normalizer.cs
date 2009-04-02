//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using Microsoft.Cci.MutableCodeModel;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Cci.Contracts;

namespace Microsoft.Cci {

  /// <summary>
  /// This visitor takes a method body and rewrites it so that high level constructs such as anonymous delegates and yield statements
  /// are turned into helper classes and methods, thus making it easier to generate IL from the CodeModel.
  /// </summary>
  public class CodeModelNormalizer : CodeAndContractMutator {

    public CodeModelNormalizer(IMetadataHost host, SourceMethodBodyProvider ilToSourceProvider, SourceToILConverterProvider sourceToILProvider,
      ISourceLocationProvider/*?*/ sourceLocationProvider, ContractProvider/*?*/ contractProvider)
      : base(host, ilToSourceProvider, sourceToILProvider, sourceLocationProvider, contractProvider) {
    }

    public CodeModelNormalizer(IMetadataHost host, bool copyOnlyIfNotAlreadyMutable, SourceMethodBodyProvider ilToSourceProvider, SourceToILConverterProvider sourceToILProvider,
      ISourceLocationProvider/*?*/ sourceLocationProvider, ContractProvider/*?*/ contractProvider)
      : base(host, copyOnlyIfNotAlreadyMutable, ilToSourceProvider, sourceToILProvider, sourceLocationProvider, contractProvider) {
    }

    public CodeModelNormalizer(CodeAndContractMutator mutator) 
      : base(mutator) {
    }

    Dictionary<IParameterDefinition, bool> capturedParameters = new Dictionary<IParameterDefinition, bool>();
    List<ILocalDefinition> closureLocals = new List<ILocalDefinition>();
    NestedTypeDefinition currentClosureClass = new NestedTypeDefinition();
    Dictionary<object, BoundField> fieldForCapturedLocalOrParameter = new Dictionary<object, BoundField>();
    IMethodDefinition method = Dummy.Method;
    List<IFieldDefinition> outerClosures = new List<IFieldDefinition>();
    List<ITypeDefinition> privateHelperTypes = new List<ITypeDefinition>();
    Dictionary<IMethodDefinition, bool> isAlreadyNormalized = new Dictionary<IMethodDefinition, bool>();


    public ISourceMethodBody GetNormalizedSourceMethodBodyFor(IMethodDefinition method, IBlockStatement body) {
      this.closureLocals = new List<ILocalDefinition>();
      this.currentClosureClass = new NestedTypeDefinition();
      this.fieldForCapturedLocalOrParameter = new Dictionary<object, BoundField>();
      this.method = method;
      this.outerClosures = new List<IFieldDefinition>();
      this.privateHelperTypes = new List<ITypeDefinition>();
      this.path.Push(method.ContainingTypeDefinition);
      this.path.Push(method);

      ClosureFinder finder = new ClosureFinder(this.fieldForCapturedLocalOrParameter, this.host.NameTable, this.contractProvider);
      finder.Visit(body);
      IMethodContract/*?*/ methodContract = null;
      if (this.contractProvider != null)
        methodContract = this.contractProvider.GetMethodContractFor(method);
      if (methodContract != null)
        finder.Visit(methodContract);
      if (this.fieldForCapturedLocalOrParameter.Count == 0 && (finder.foundAnonymousDelegate || finder.foundYield))
        body = this.CreateAndInitializeClosureTemp(body);
      body = this.Visit(body, method, methodContract);
      SourceMethodBody result = new SourceMethodBody(this.sourceToILProvider, this.host, this.sourceLocationProvider, this.contractProvider);
      result.Block = body;
      result.MethodDefinition = method;
      result.LocalsAreZeroed = true;
      result.PrivateHelperTypes = this.privateHelperTypes;

      this.path.Pop();
      this.path.Pop();
      return result;
    }

    private IBlockStatement CreateAndInitializeClosureTemp(IBlockStatement body) {
      BlockStatement mutableBlockStatement = new BlockStatement(body);
      NestedTypeDefinition closureClass = this.CreateClosureClass();
      ILocalDefinition closureTemp = this.CreateClosureTemp(closureClass);
      mutableBlockStatement.Statements.Insert(0, this.ConstructClosureInstance(closureTemp, closureClass));
      return mutableBlockStatement;
    }

    private void CreateClosureClassIfNecessary(BlockStatement blockStatement, IMethodContract/*?*/ methodContract) {
      bool captureThis = false;
      List<IStatement> statements = new List<IStatement>();
      if (this.fieldForCapturedLocalOrParameter.Count > 0) {
        NestedTypeDefinition/*?*/ closureClass = null;
        ILocalDefinition/*?*/ closureTemp = null;
        IStatement/*?*/ captureOuterClosure = null;
        if (this.closureLocals.Count == 0) {
          foreach (object capturedLocalOrParameter in this.fieldForCapturedLocalOrParameter.Keys) {
            ITypeDefinition/*?*/ typeDef = capturedLocalOrParameter as ITypeDefinition;
            if (typeDef != null) {
              captureThis = true;
              if (closureClass == null) {
                closureClass = this.CreateClosureClass();
                closureTemp = this.CreateClosureTemp(closureClass);
                if (this.closureLocals.Count > 1)
                  captureOuterClosure = this.CaptureOuterClosure(closureClass);
              }
            }
            IParameterDefinition/*?*/ parameter = capturedLocalOrParameter as IParameterDefinition;
            if (parameter == null) continue;
            this.AddToClosureIfCaptured(ref closureClass, ref closureTemp, ref captureOuterClosure, parameter);
          }
        }
        foreach (IStatement statement in blockStatement.Statements) {
          ILocalDeclarationStatement/*?*/ locDecl = statement as ILocalDeclarationStatement;
          if (locDecl == null) continue;
          this.AddToClosureIfCaptured(ref closureClass, ref closureTemp, ref captureOuterClosure, locDecl.LocalVariable);
        }
        if (closureTemp != null) {
          statements.Add(this.ConstructClosureInstance(closureTemp, closureClass));
          if (captureOuterClosure != null)
            statements.Add(captureOuterClosure);
          else if (captureThis)
            statements.Add(this.CaptureThisArgument(closureClass));
          foreach (object capturedLocalOrParameter in this.fieldForCapturedLocalOrParameter.Keys) {
            IParameterDefinition/*?*/ parameter = capturedLocalOrParameter as IParameterDefinition;
            if (parameter == null) continue;
            BoundField bf = this.fieldForCapturedLocalOrParameter[parameter];
            statements.Add(this.InitializeBoundFieldFromParameter(bf, parameter));
          }
        }
      }
      //if (methodContract != null) {
      //  MethodCall callStartContract = new MethodCall();
      //  callStartContract.IsStaticCall = true;
      //  callStartContract.MethodToCall = this.contractProvider.ContractMethods.StartContract;
      //  callStartContract.Type = this.host.PlatformType.SystemVoid;
      //  ExpressionStatement startContract = new ExpressionStatement();
      //  startContract.Expression = callStartContract;
      //  statements.Add(startContract);
      //}
      if (statements.Count > 0) {
        statements.AddRange(blockStatement.Statements);
        blockStatement.Statements = statements;
      }      
    }

    private IStatement InitializeBoundFieldFromParameter(BoundField boundField, IParameterDefinition parameter) {
      var currentClosureLocal = this.closureLocals[0];
      var currentClosureLocalBinding = new BoundExpression() { Definition = currentClosureLocal, Type = currentClosureLocal.Type };
      var target = new TargetExpression() { Instance = currentClosureLocalBinding, Definition = boundField.Field, Type = parameter.Type };
      var boundParameter = new BoundExpression() { Definition = parameter, Type = parameter.Type };
      var assignment = new Assignment() { Target = target, Source = boundParameter, Type = parameter.Type };
      return new ExpressionStatement() { Expression = assignment };
    }

    private void AddToClosureIfCaptured(ref NestedTypeDefinition closureClass, ref ILocalDefinition closureTemp, ref IStatement captureOuterClosure, object localOrParameter) {
      BoundField/*?*/ bf = null;
      if (this.fieldForCapturedLocalOrParameter.TryGetValue(localOrParameter, out bf)) {
        if (closureClass == null) {
          closureClass = this.CreateClosureClass();
          closureTemp = this.CreateClosureTemp(closureClass);
          if (this.closureLocals.Count > 1)
            captureOuterClosure = this.CaptureOuterClosure(closureClass);
        }
        FieldDefinition correspondingField = bf.Field;
        correspondingField.ContainingType = closureClass;
        closureClass.Fields.Add(correspondingField);
        this.cache.Add(correspondingField, correspondingField);
      }
    }

    private IStatement ConstructClosureInstance(ILocalDefinition closureTemp, NestedTypeDefinition closureClass) {
      var target = new TargetExpression() { Definition = closureTemp, Type = closureTemp.Type };
      var constructor = this.CreateDefaultConstructorFor(closureClass);
      var construct = new CreateObjectInstance() { MethodToCall = constructor, Type = closureClass };
      var assignment = new Assignment() { Target = target, Source = construct, Type = closureClass };
      return new ExpressionStatement() { Expression = assignment };
    }

    private IStatement CaptureThisArgument(NestedTypeDefinition closureClass) {
      var currentClosureLocal = this.closureLocals[0];
      var currentClosureLocalBinding = new BoundExpression() { Definition = currentClosureLocal, Type = currentClosureLocal.Type };
      var thisArgumentField = closureClass.Fields[0];
      var target = new TargetExpression() { Instance = currentClosureLocalBinding, Definition = thisArgumentField, Type = thisArgumentField.Type };
      var assignment = new Assignment() { Target = target, Source = new ThisReference(), Type = thisArgumentField.Type };
      return new ExpressionStatement() { Expression = assignment };
    }

    private IStatement CaptureOuterClosure(NestedTypeDefinition closureClass) {
      var currentClosureLocal = this.closureLocals[0];
      var outerClosureLocal = this.closureLocals[1];
      var outerClosureField = new FieldDefinition();
      closureClass.Fields.Add(outerClosureField);
      this.outerClosures.Insert(0, outerClosureField);
      outerClosureField.ContainingType = closureClass;
      outerClosureField.Name = this.host.NameTable.GetNameFor("__outerClosure "+(this.privateHelperTypes.Count-1));
      outerClosureField.Type = outerClosureLocal.Type;
      outerClosureField.Visibility = TypeMemberVisibility.Public;

      var currentClosureLocalBinding = new BoundExpression() { Definition = currentClosureLocal, Type = currentClosureLocal.Type };
      var target = new TargetExpression() { Instance = currentClosureLocalBinding, Definition = outerClosureField, Type = outerClosureField.Type };
      var source = new BoundExpression() { Definition = outerClosureLocal, Type = outerClosureLocal.Type };
      var assignment = new Assignment() { Target = target, Source = source, Type = outerClosureField.Type };
      return new ExpressionStatement() { Expression = assignment };
    }

    private IExpression ClosureInstanceFor(ITypeDefinition closure) {
      IExpression/*?*/ result = CodeDummy.Expression;
      if (!this.closureInstanceFor.TryGetValue(closure, out result)) {
        Debug.Assert(false);
      }
      return result;
    }
    Dictionary<ITypeDefinition, IExpression> closureInstanceFor = new Dictionary<ITypeDefinition, IExpression>();

    private NestedTypeDefinition CreateClosureClass() {
      MethodReference compilerGeneratedCtor = new MethodReference(this.host, this.host.PlatformType.SystemRuntimeCompilerServicesCompilerGeneratedAttribute,
        CallingConvention.HasThis, this.host.PlatformType.SystemVoid, this.host.NameTable.Ctor, 0);
      CustomAttribute compilerGeneratedAttribute = new CustomAttribute();
      compilerGeneratedAttribute.Constructor = compilerGeneratedCtor;

      NestedTypeDefinition result = new NestedTypeDefinition();
      this.cache.Add(result, result);
      this.currentClosureClass = result;
      this.privateHelperTypes.Add(result);
      string signature = MemberHelper.GetMethodSignature(this.method, NameFormattingOptions.Signature|NameFormattingOptions.ReturnType|NameFormattingOptions.TypeParameters);
      result.Name = this.host.NameTable.GetNameFor(signature+ " closure "+this.privateHelperTypes.Count);
      result.Attributes.Add(compilerGeneratedAttribute);
      result.BaseClasses.Add(this.host.PlatformType.SystemObject);
      result.ContainingTypeDefinition = this.GetCurrentType();
      result.InternFactory = this.host.InternFactory;
      result.IsBeforeFieldInit = true;
      result.IsClass = true;
      result.IsSealed = true;
      result.Layout = LayoutKind.Auto;
      result.StringFormat = StringFormatKind.Ansi;
      result.Visibility = TypeMemberVisibility.Private;

      BoundField/*?*/ capturedThis;
      if (this.closureLocals.Count == 0 && this.fieldForCapturedLocalOrParameter.TryGetValue(this.method.ContainingTypeDefinition, out capturedThis)) {
        result.Fields.Add(capturedThis.Field);
        capturedThis.Field.ContainingType = result;
        capturedThis.Field.Type = this.Visit(capturedThis.Field.Type);
      }
      return result;
    }

    private ILocalDefinition CreateClosureTemp(NestedTypeDefinition closureClass) {
      LocalDefinition result = new LocalDefinition();
      this.cache.Add(result, result);
      this.closureLocals.Insert(0, result);
      var boundExpression = new BoundExpression();
      boundExpression.Definition = result;
      this.closureInstanceFor.Add(closureClass, boundExpression);
      result.Name = this.host.NameTable.GetNameFor("__closure "+this.privateHelperTypes.Count);
      result.Type = closureClass;
      return result;
    }

    private MethodDefinition CreateDefaultConstructorFor(NestedTypeDefinition closureClass) {
      IMethodReference objectCtor = new MethodReference(this.host, this.host.PlatformType.SystemObject, CallingConvention.HasThis, 
        this.host.PlatformType.SystemVoid, this.host.NameTable.Ctor, 0);
      MethodCall baseConstructorCall = new MethodCall() { ThisArgument = new BaseClassReference(), MethodToCall = objectCtor, Type = this.host.PlatformType.SystemVoid };
      ExpressionStatement baseConstructorCallStatement = new ExpressionStatement() { Expression = baseConstructorCall };
      List<IStatement> statements = new List<IStatement>();
      statements.Add(baseConstructorCallStatement);
      BlockStatement block = new BlockStatement() { Statements = statements };
      SourceMethodBody body = new SourceMethodBody(this.sourceToILProvider, this.host, this.sourceLocationProvider, this.contractProvider);
      body.LocalsAreZeroed = true;
      body.Block = block;

      MethodDefinition result = new MethodDefinition();
      closureClass.Methods.Add(result);
      this.cache.Add(result, result);
      this.isAlreadyNormalized.Add(result, true);
      result.Body = body;
      body.MethodDefinition = result;
      result.CallingConvention = CallingConvention.HasThis;
      result.ContainingType = closureClass;
      result.IsCil = true;
      result.IsHiddenBySignature = true;
      result.IsRuntimeSpecial = true;
      result.IsSpecialName = true;
      result.Name = this.host.NameTable.Ctor;
      result.Type = this.host.PlatformType.SystemVoid;
      result.Visibility = TypeMemberVisibility.Public;
      return result;
    }

    private IMethodBody GetMethodBodyFrom(IBlockStatement block, IMethodDefinition method) {
      var bodyFixer = new FixAnonymousDelegateBodyToUseClosure(this.fieldForCapturedLocalOrParameter, this.cache, this.currentClosureClass, this.outerClosures,
        this.host, this.ilToSourceProvider, this.sourceToILProvider, this.sourceLocationProvider);
      block = bodyFixer.Visit(block);
      var result = new SourceMethodBody(ProvideSourceToILConverter, this.host, this.sourceLocationProvider, this.contractProvider);
      result.Block = block;
      result.LocalsAreZeroed = true;
      result.MethodDefinition = method;
      return result;
    }

    static ISourceToILConverter ProvideSourceToILConverter(IMetadataHost host, ISourceLocationProvider/*?*/ sourceLocationProvider, IContractProvider/*?*/ contractProvider) {
      return new PreNormalizedCodeModelToILConverter(host, sourceLocationProvider, contractProvider);
    }

    public override IExpression Visit(AnonymousDelegate anonymousDelegate) {
      var method = new MethodDefinition();
      this.currentClosureClass.Methods.Add(method);
      method.CallingConvention = anonymousDelegate.CallingConvention;
      method.ContainingType = this.currentClosureClass;
      method.IsCil = true;
      method.IsHiddenBySignature = true;
      if ((anonymousDelegate.CallingConvention & CallingConvention.HasThis) == 0)
        method.IsStatic = true;
      method.Name = this.host.NameTable.GetNameFor("__anonymous_method "+this.currentClosureClass.Methods.Count);
      method.Parameters = new List<IParameterDefinition>(anonymousDelegate.Parameters);
      if (anonymousDelegate.ReturnValueIsModified)
        method.ReturnValueCustomModifiers = new List<ICustomModifier>(anonymousDelegate.ReturnValueCustomModifiers);
      method.ReturnValueIsByRef = anonymousDelegate.ReturnValueIsByRef;
      method.Type = anonymousDelegate.ReturnType;
      method.Visibility = TypeMemberVisibility.Public;
      method.Body = this.GetMethodBodyFrom(anonymousDelegate.Body, method);

      var boundCurrentClosureLocal = new BoundExpression();
      if (this.closureLocals.Count == 0)
        boundCurrentClosureLocal.Definition = this.CreateClosureTemp(this.currentClosureClass);
      else
        boundCurrentClosureLocal.Definition = this.closureLocals[0];

      var createDelegateInstance = new CreateDelegateInstance();
      if ((anonymousDelegate.CallingConvention & CallingConvention.HasThis) != 0)
        createDelegateInstance.Instance = boundCurrentClosureLocal;
      createDelegateInstance.MethodToCallViaDelegate = method;
      createDelegateInstance.Type = this.Visit(anonymousDelegate.Type);

      return createDelegateInstance;
    }

    public override IAddressableExpression Visit(AddressableExpression addressableExpression) {
      BoundField/*?*/ boundField;
      if (this.fieldForCapturedLocalOrParameter.TryGetValue(addressableExpression.Definition, out boundField)) {
        addressableExpression.Instance = this.ClosureInstanceFor(boundField.Field.ContainingTypeDefinition);
        addressableExpression.Definition = boundField.Field;
        addressableExpression.Type = this.Visit(addressableExpression.Type);
        return addressableExpression;
      }
      return base.Visit(addressableExpression);
    }

    private IBlockStatement Visit(IBlockStatement blockStatement, IMethodDefinition method, IMethodContract/*?*/ methodContract)
      //^ requires methodContract != null ==> this.contractProvider != null;
    {
      BlockStatement mutableBlockStatement = new BlockStatement(blockStatement);
      return Visit(mutableBlockStatement, method, methodContract);
    }

    private IBlockStatement Visit(BlockStatement blockStatement, IMethodDefinition/*?*/ method, IMethodContract/*?*/ methodContract) 
      //^ requires method == null ==> methodContract == null;
      //^ requires methodContract != null ==> this.contractProvider != null;
    {
      NestedTypeDefinition savedCurrentClosureClass = this.currentClosureClass;
      this.CreateClosureClassIfNecessary(blockStatement, methodContract);
      blockStatement.Statements = this.Visit(blockStatement.Statements);
      if (methodContract != null) 
        this.contractProvider.AssociateMethodWithContract(method, this.Visit(methodContract));
      if (savedCurrentClosureClass != this.currentClosureClass) {
        this.currentClosureClass = savedCurrentClosureClass;
        this.closureLocals.RemoveAt(0);
        if (this.outerClosures.Count > 0)
          this.outerClosures.RemoveAt(0);
      }
      return blockStatement;
    }

    public override IBlockStatement Visit(BlockStatement blockStatement) {
      return this.Visit(blockStatement, null, null);
    }

    public override IExpression Visit(BoundExpression boundExpression) {
      BoundField/*?*/ boundField;
      if (this.fieldForCapturedLocalOrParameter.TryGetValue(boundExpression.Definition, out boundField)) {
        IParameterDefinition/*?*/ boundParameter = boundExpression.Definition as IParameterDefinition;
        if (boundParameter != null && !this.capturedParameters.ContainsKey(boundParameter)) {
          this.capturedParameters.Add(boundParameter, true);
        } else {
          boundExpression.Instance = this.ClosureInstanceFor(boundField.Field.ContainingTypeDefinition);
          boundExpression.Definition = boundField.Field;
          boundExpression.Type = this.Visit(boundExpression.Type);
          return boundExpression;
        }
      }
      return base.Visit(boundExpression);
    }

    public override IGlobalMethodDefinition Visit(IGlobalMethodDefinition globalMethodDefinition) {
      var result = base.Visit(globalMethodDefinition);
      this.isAlreadyNormalized.Add(globalMethodDefinition, true);
      return result;
    }

    public override IStatement Visit(LocalDeclarationStatement localDeclarationStatement) {
      localDeclarationStatement.LocalVariable = this.Visit(this.GetMutableCopy(localDeclarationStatement.LocalVariable));
      if (localDeclarationStatement.InitialValue != null) {
        var source = this.Visit(localDeclarationStatement.InitialValue);
        BoundField/*?*/ boundField;
        if (this.fieldForCapturedLocalOrParameter.TryGetValue(localDeclarationStatement.LocalVariable, out boundField)) {
          var currentClosureLocal = this.closureLocals[0];
          var currentClosureLocalBinding = new BoundExpression() { Definition = currentClosureLocal, Type = currentClosureLocal.Type };
          var target = new TargetExpression() { Instance = currentClosureLocalBinding, Definition = boundField.Field, Type = boundField.Type };
          var assignment = new Assignment() { Target = target, Source = source, Type = boundField.Type };
          return new ExpressionStatement() { Expression = assignment };
        } else {
          var target = new TargetExpression() { Definition = localDeclarationStatement.LocalVariable, Type = localDeclarationStatement.LocalVariable.Type };
          var assignment = new Assignment() { Target = target, Source = source, Type = target.Type };
          return new ExpressionStatement() { Expression = assignment };
        }
      }
      return localDeclarationStatement;
    }

    public override IMethodBody Visit(IMethodBody methodBody) {
      ISourceMethodBody/*?*/ sourceMethodBody = methodBody as ISourceMethodBody;
      if (sourceMethodBody != null)
        return this.GetNormalizedSourceMethodBodyFor(this.GetCurrentMethod(), sourceMethodBody.Block);
      return base.Visit(methodBody);
    }

    public override IMethodDefinition Visit(IMethodDefinition methodDefinition) {
      if (this.isAlreadyNormalized.ContainsKey(methodDefinition)) {
        object result = methodDefinition;
        this.cache.TryGetValue(methodDefinition, out result);
        return (IMethodDefinition)result;
      }
      return base.Visit(methodDefinition);
    }

    public override IMethodContract Visit(IMethodContract methodContract) {
      object/*?*/ result = null;
      if (this.cache.TryGetValue(methodContract, out result)) return (IMethodContract)result;
      result = base.Visit(methodContract);
      this.cache.Add(methodContract, result);
      return (IMethodContract)result;
    }

    public override MethodDefinition Visit(MethodDefinition methodDefinition) {
      if (this.stopTraversal) return methodDefinition;
      if (methodDefinition == Dummy.Method) return methodDefinition;
      this.Visit((TypeDefinitionMember)methodDefinition);
      this.path.Push(methodDefinition);
      if (methodDefinition.IsGeneric)
        methodDefinition.GenericParameters = this.Visit(methodDefinition.GenericParameters, methodDefinition);
      methodDefinition.Parameters = this.Visit(methodDefinition.Parameters);
      if (methodDefinition.IsPlatformInvoke)
        methodDefinition.PlatformInvokeData = this.Visit(this.GetMutableCopy(methodDefinition.PlatformInvokeData));
      methodDefinition.ReturnValueAttributes = this.VisitMethodReturnValueAttributes(methodDefinition.ReturnValueAttributes);
      if (methodDefinition.ReturnValueIsModified)
        methodDefinition.ReturnValueCustomModifiers = this.VisitMethodReturnValueCustomModifiers(methodDefinition.ReturnValueCustomModifiers);
      if (methodDefinition.ReturnValueIsMarshalledExplicitly)
        methodDefinition.ReturnValueMarshallingInformation = this.VisitMethodReturnValueMarshallingInformation(this.GetMutableCopy(methodDefinition.ReturnValueMarshallingInformation));
      if (methodDefinition.HasDeclarativeSecurity)
        methodDefinition.SecurityAttributes = this.Visit(methodDefinition.SecurityAttributes);
      methodDefinition.Type = this.Visit(methodDefinition.Type);
      if (!methodDefinition.IsAbstract && !methodDefinition.IsExternal)
        methodDefinition.Body = this.Visit(methodDefinition.Body);
      else {
        if (this.contractProvider != null) {
          IMethodContract/*?*/ methodContract = this.contractProvider.GetMethodContractFor(methodDefinition);
          if (methodContract != null)
            this.contractProvider.AssociateMethodWithContract(methodDefinition, this.Visit(methodContract));
        }
      }
      this.path.Pop();
      return methodDefinition;
    }

    public override ITargetExpression Visit(TargetExpression targetExpression) {
      BoundField/*?*/ boundField;
      if (this.fieldForCapturedLocalOrParameter.TryGetValue(targetExpression.Definition, out boundField)) {
        targetExpression.Instance = this.ClosureInstanceFor(boundField.Field.ContainingTypeDefinition);
        targetExpression.Definition = boundField.Field;
        targetExpression.Type = this.Visit(targetExpression.Type);
        return targetExpression;
      }
      return base.Visit(targetExpression);
    }

    public override List<IMethodDefinition> Visit(List<IMethodDefinition> methodDefinitions) {
      int n = methodDefinitions.Count;
      List<IMethodDefinition> result = new List<IMethodDefinition>(n);
      for (int i = 0; i < n; i++) {
        IMethodDefinition meth = methodDefinitions[i];
        if (AttributeHelper.Contains(meth.Attributes, this.host.PlatformType.SystemRuntimeCompilerServicesCompilerGeneratedAttribute)) {
          if (meth.Name.Value.Contains(">b__"))
            continue;
        }
        result.Add(this.Visit(this.GetMutableCopy(meth)));
      }
      return result;
    }

    public override List<INestedTypeDefinition> Visit(List<INestedTypeDefinition> nestedTypeDefinitions) {
      int n = nestedTypeDefinitions.Count;
      List<INestedTypeDefinition> result = new List<INestedTypeDefinition>(n);
      for (int i = 0; i < n; i++) {
        INestedTypeDefinition nt = nestedTypeDefinitions[i];
        if (AttributeHelper.Contains(nt.Attributes, this.host.PlatformType.SystemRuntimeCompilerServicesCompilerGeneratedAttribute)) {
          if (!nt.Name.Value.Contains(">c__Display") && !nt.Name.Value.Contains(">d__") && !nt.Name.Value.Contains(">e__"))
            continue;
        }
        result.Add(this.Visit(this.GetMutableCopy(nt)));
      }
      return result;
    }

  }

  public class MethodBodyNormalizer : CodeModelNormalizer {

    public MethodBodyNormalizer(IMetadataHost host, SourceMethodBodyProvider ilToSourceProvider, SourceToILConverterProvider sourceToILProvider,
      ISourceLocationProvider/*?*/ sourceLocationProvider, ContractProvider/*?*/ contractProvider)
      : base(host, ilToSourceProvider, sourceToILProvider, sourceLocationProvider, contractProvider) {
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


  }
}
