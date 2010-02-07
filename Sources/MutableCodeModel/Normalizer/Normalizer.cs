//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using Microsoft.Cci.MutableCodeModel;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Cci.Contracts;

namespace Microsoft.Cci.MutableCodeModel {

  /// <summary>
  /// This visitor takes a method body and rewrites it so that high level constructs such as anonymous delegates and yield statements
  /// are turned into helper classes and methods, thus making it easier to generate IL from the CodeModel.
  /// </summary>
  public class MethodBodyNormalizer : MethodBodyCodeAndContractMutator {

    /// <summary>
    /// Initializes a visitor that takes a method body and rewrites it so that high level constructs such as anonymous delegates and yield statements
    /// are turned into helper classes and methods, thus making it easier to generate IL from the CodeModel.
    /// </summary>
    /// <param name="host">An object representing the application that is hosting the converter. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.</param>
    /// <param name="sourceLocationProvider">An object that can map the ILocation objects found in a block of statements to IPrimarySourceLocation objects. May be null.</param>
    /// <param name="contractProvider">An object that associates contracts, such as preconditions and postconditions, with methods, types and loops.
    /// IL to check this contracts will be generated along with IL to evaluate the block of statements. May be null.</param>
    public MethodBodyNormalizer(IMetadataHost host, ISourceLocationProvider/*?*/ sourceLocationProvider, ContractProvider/*?*/ contractProvider)
      : base(host, sourceLocationProvider, contractProvider) {
    }

    Dictionary<IParameterDefinition, bool> capturedParameters = new Dictionary<IParameterDefinition, bool>();
    Dictionary<IMethodDefinition, bool> isAlreadyNormalized = new Dictionary<IMethodDefinition, bool>();
    Dictionary<object, BoundField> FieldForCapturedLocalOrParameter = new Dictionary<object, BoundField>();
    NestedTypeDefinition CurrentClosureClass = new NestedTypeDefinition();
    IMethodDefinition method = Dummy.Method;
    List<ILocalDefinition> closureLocals = new List<ILocalDefinition>();
    List<IFieldDefinition> outerClosures = new List<IFieldDefinition>();
    List<ITypeDefinition> privateHelperTypes = new List<ITypeDefinition>();
    Dictionary<IBlockStatement, uint>/*?*/ iteratorLocalCount;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="method"></param>
    /// <param name="body"></param>
    /// <returns></returns>
    public ISourceMethodBody GetNormalizedSourceMethodBodyFor(IMethodDefinition method, IBlockStatement body) {
      this.CurrentClosureClass = new NestedTypeDefinition();
      this.FieldForCapturedLocalOrParameter = new Dictionary<object, BoundField>();
      this.closureLocals = new List<ILocalDefinition>();
      this.method = method;
      this.outerClosures = new List<IFieldDefinition>();
      this.privateHelperTypes = new List<ITypeDefinition>();
      this.path.Push(method.ContainingTypeDefinition);
      this.path.Push(method);

      ClosureFinder finder = new ClosureFinder(this.FieldForCapturedLocalOrParameter, this.host.NameTable, this.contractProvider);
      finder.Visit(body);
      IMethodContract/*?*/ methodContract = null;
      if (this.contractProvider != null)
        methodContract = this.contractProvider.GetMethodContractFor(method);
      if (methodContract != null)
        finder.Visit(methodContract);
      if (finder.foundAnonymousDelegate && this.FieldForCapturedLocalOrParameter.Count == 0) {
        body = this.CreateAndInitializeClosureTemp(body);
      }
      body = this.Visit(body, method, methodContract);
      if (finder.foundYield) {
        this.isIteratorBody = true;
        this.FieldForCapturedLocalOrParameter = new Dictionary<object, BoundField>();
        body = this.GetNormalizedIteratorBody(body, method, methodContract, privateHelperTypes);
      }
      SourceMethodBody result = new SourceMethodBody(this.host, this.sourceLocationProvider, this.contractProvider);
      result.Block = body;
      result.MethodDefinition = method;
      result.IsNormalized = true;
      result.LocalsAreZeroed = true;
      result.PrivateHelperTypes = privateHelperTypes;

      this.path.Pop();
      this.path.Pop();
      return result;
    }

    /// <summary>
    /// Given the body of an iterator method <paramref name="body"/>, this method try to compile its body.
    /// 
    /// Specifically, this method:
    /// 1) creates a closure class that implements: IEnumerator, generic and nongeneric versions,
    /// IEnumerable, generic and nongeneric versions, and IDisposable. The generic versions of IEnumerator
    /// and IEnumerator is instantiated by a type T that is used to instantiate the return type of
    /// the iterator method. The members of the closure class include:
    /// 1.1) fields corresponding to every parameter and local variables.
    /// 1.2) fields that manages the state machine: __current and __state, and a currentThreadId field.
    /// 1.3) a constructor that takes one int argument.  
    /// 1.4) methods that are required by the interfaces of the closure class: MoveNext, Reset,
    /// GetEnumerator, Current getter, and DisposeMethod. 
    /// 2) creates the new body of the iterator method: which returns a local variable that holds a new object of the closure class, with the fields of 
    /// the closure class that correspond to the parameters (including the self parameter if applicable)
    /// initialized. 
    /// 3) transforms the body, which should now not contain any annonymous delegates, into the body of the 
    /// MoveNext method of the closure class. This includes:
    /// 3.1) every local/parameter reference -> this.field reference
    /// 3.2) every yield return or yield break -> assignment to current and return true or false, respectively.
    /// 3.3) a switch statement for the state machine, with state values corresponds to each yield return/yield break
    /// 3.4) a try block if an iterator is created in the body. 
    /// 4) If the iterator method has a type parameter, so will the closure class. Make sure in the closure class only
    /// the right type parameter is referenced. 
    /// </summary>
    /// <param name="body">The method body to be normalized</param>
    /// <param name="method">Method definition that owns the body</param>
    /// <param name="methodContract">The contract of this method</param>
    /// <param name="privateHelperTypes">List of helper types generated when compiling <paramref name="method">method</paramref>/></param>
    /// <returns></returns>
    private IBlockStatement GetNormalizedIteratorBody(IBlockStatement body, IMethodDefinition method, IMethodContract methodContract, List<ITypeDefinition> privateHelperTypes) {
      this.iteratorLocalCount = new Dictionary<IBlockStatement, uint>();
      IteratorClosureGenerator iteratorClosureGenerator = new IteratorClosureGenerator(this.FieldForCapturedLocalOrParameter, this.iteratorLocalCount,
        method, privateHelperTypes, this.host, this.sourceLocationProvider, this.contractProvider);
      return iteratorClosureGenerator.CompileIterator(body);
    }

    /// <summary>
    /// Returns true if the last call to GetNormalizedSourceMethodBodyFor visited a body that contains a yield statement.
    /// </summary>
    public bool IsIteratorBody {
      get { return this.isIteratorBody; }
    }
    bool isIteratorBody;

    private IMethodReference CompilerGeneratedCtor {
      get {
        if (this.compilerGeneratedCtor == null)
          this.compilerGeneratedCtor = new Microsoft.Cci.MethodReference(this.host, this.host.PlatformType.SystemRuntimeCompilerServicesCompilerGeneratedAttribute,
             CallingConvention.HasThis, this.host.PlatformType.SystemVoid, this.host.NameTable.Ctor, 0);
        return this.compilerGeneratedCtor;
      }
    }
    private IMethodReference/*?*/ compilerGeneratedCtor;

    private IMethodReference ObjectCtor {
      get {
        if (this.objectCtor == null)
          this.objectCtor = new Microsoft.Cci.MethodReference(this.host, this.host.PlatformType.SystemObject, CallingConvention.HasThis,
             this.host.PlatformType.SystemVoid, this.host.NameTable.Ctor, 0);
        return this.objectCtor;
      }
    }
    private IMethodReference/*?*/ objectCtor;


    private IBlockStatement CreateAndInitializeClosureTemp(IBlockStatement body) {
      BlockStatement mutableBlockStatement = new BlockStatement(body);
      NestedTypeDefinition closureClass = this.CreateClosureClass();
      ILocalDefinition closureTemp = this.CreateClosureTemp(closureClass);
      mutableBlockStatement.Statements.Insert(0, this.ConstructClosureInstance(closureTemp, closureClass));
      return mutableBlockStatement;
    }

    private ILocalDefinition CreateClosureTemp(NestedTypeDefinition closureClass) {
      LocalDefinition result = new LocalDefinition();
      this.cache.Add(result, result);
      this.closureLocals.Insert(0, result);
      var boundExpression = new BoundExpression();
      boundExpression.Definition = result;
      this.closureInstanceFor.Add(closureClass, boundExpression);
      result.Name = this.host.NameTable.GetNameFor("__closure " + this.privateHelperTypes.Count);
      result.Type = closureClass;
      return result;
    }

    private IStatement ConstructClosureInstance(ILocalDefinition closureTemp, NestedTypeDefinition closureClass) {
      var target = new TargetExpression() { Definition = closureTemp, Type = closureTemp.Type };
      var constructor = this.CreateDefaultConstructorFor(closureClass);
      var construct = new CreateObjectInstance() { MethodToCall = constructor, Type = closureClass };
      var assignment = new Assignment() { Target = target, Source = construct, Type = closureClass };
      return new ExpressionStatement() { Expression = assignment };
    }

    private NestedTypeDefinition CreateClosureClass() {
      CustomAttribute compilerGeneratedAttribute = new CustomAttribute();
      compilerGeneratedAttribute.Constructor = this.CompilerGeneratedCtor;

      NestedTypeDefinition result = new NestedTypeDefinition();
      this.cache.Add(result, result);
      this.CurrentClosureClass = result;
      this.privateHelperTypes.Add(result);
      string signature = MemberHelper.GetMethodSignature(this.method, NameFormattingOptions.Signature | NameFormattingOptions.ReturnType | NameFormattingOptions.TypeParameters);
      result.Name = this.host.NameTable.GetNameFor(signature + " closure " + this.privateHelperTypes.Count);
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
      if (this.closureLocals.Count == 0 && this.FieldForCapturedLocalOrParameter.TryGetValue(this.method.ContainingTypeDefinition, out capturedThis)) {
        result.Fields.Add(capturedThis.Field);
        capturedThis.Field.ContainingTypeDefinition = result;
        capturedThis.Field.Type = this.Visit(capturedThis.Field.Type);
      }
      return result;
    }

    private void AddToClosureIfCaptured(ref NestedTypeDefinition closureClass, ref ILocalDefinition closureTemp, ref IStatement captureOuterClosure, object localOrParameter) {
      BoundField/*?*/ bf = null;
      if (this.FieldForCapturedLocalOrParameter.TryGetValue(localOrParameter, out bf)) {
        if (closureClass == null) {
          closureClass = this.CreateClosureClass();
          closureTemp = this.CreateClosureTemp(closureClass);
          if (this.closureLocals.Count > 1)
            captureOuterClosure = this.CaptureOuterClosure(closureClass);
        }
        FieldDefinition correspondingField = bf.Field;
        correspondingField.ContainingTypeDefinition = closureClass;
        closureClass.Fields.Add(correspondingField);
        //this.cache.Add(correspondingField, correspondingField);
      }
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
      outerClosureField.ContainingTypeDefinition = closureClass;
      outerClosureField.Name = this.host.NameTable.GetNameFor("__outerClosure " + (this.privateHelperTypes.Count - 1));
      outerClosureField.Type = outerClosureLocal.Type;
      outerClosureField.Visibility = TypeMemberVisibility.Public;

      var currentClosureLocalBinding = new BoundExpression() { Definition = currentClosureLocal, Type = currentClosureLocal.Type };
      var target = new TargetExpression() { Instance = currentClosureLocalBinding, Definition = outerClosureField, Type = outerClosureField.Type };
      var source = new BoundExpression() { Definition = outerClosureLocal, Type = outerClosureLocal.Type };
      var assignment = new Assignment() { Target = target, Source = source, Type = outerClosureField.Type };
      return new ExpressionStatement() { Expression = assignment };
    }

    private IStatement InitializeBoundFieldFromParameter(BoundField boundField, IParameterDefinition parameter) {
      var currentClosureLocal = this.closureLocals[0];
      var currentClosureLocalBinding = new BoundExpression() { Definition = currentClosureLocal, Type = currentClosureLocal.Type };
      var target = new TargetExpression() { Instance = currentClosureLocalBinding, Definition = boundField.Field, Type = parameter.Type };
      var boundParameter = new BoundExpression() { Definition = parameter, Type = parameter.Type };
      var assignment = new Assignment() { Target = target, Source = boundParameter, Type = parameter.Type };
      return new ExpressionStatement() { Expression = assignment };
    }

    private MethodDefinition CreateDefaultConstructorFor(NestedTypeDefinition closureClass) {
      MethodCall baseConstructorCall = new MethodCall() { ThisArgument = new BaseClassReference(), MethodToCall = this.ObjectCtor, Type = this.host.PlatformType.SystemVoid };
      ExpressionStatement baseConstructorCallStatement = new ExpressionStatement() { Expression = baseConstructorCall };
      List<IStatement> statements = new List<IStatement>();
      statements.Add(baseConstructorCallStatement);
      BlockStatement block = new BlockStatement() { Statements = statements };
      SourceMethodBody body = new SourceMethodBody(this.host, this.sourceLocationProvider, this.contractProvider);
      body.IsNormalized = true;
      body.LocalsAreZeroed = true;
      body.Block = block;

      MethodDefinition result = new MethodDefinition();
      closureClass.Methods.Add(result);
      this.cache.Add(result, result);
      this.isAlreadyNormalized.Add(result, true);
      result.Body = body;
      body.MethodDefinition = result;
      result.CallingConvention = CallingConvention.HasThis;
      result.ContainingTypeDefinition = closureClass;
      result.IsCil = true;
      result.IsHiddenBySignature = true;
      result.IsRuntimeSpecial = true;
      result.IsSpecialName = true;
      result.Name = this.host.NameTable.Ctor;
      result.Type = this.host.PlatformType.SystemVoid;
      result.Visibility = TypeMemberVisibility.Public;
      return result;
    }

    private void CreateClosureClassIfNecessary(BlockStatement blockStatement, IMethodContract/*?*/ methodContract) {
      bool captureThis = false;
      List<IStatement> statements = new List<IStatement>();
      if (this.FieldForCapturedLocalOrParameter.Count > 0) {
        NestedTypeDefinition/*?*/ closureClass = null;
        ILocalDefinition/*?*/ closureTemp = null;
        IStatement/*?*/ captureOuterClosure = null;
        if (this.closureLocals.Count == 0) {
          foreach (object capturedLocalOrParameter in this.FieldForCapturedLocalOrParameter.Keys) {
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
          foreach (object capturedLocalOrParameter in this.FieldForCapturedLocalOrParameter.Keys) {
            IParameterDefinition/*?*/ parameter = capturedLocalOrParameter as IParameterDefinition;
            if (parameter == null) continue;
            BoundField bf = this.FieldForCapturedLocalOrParameter[parameter];
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

    private IBlockStatement Visit(IBlockStatement blockStatement, IMethodDefinition method, IMethodContract/*?*/ methodContract)
      //^ requires methodContract != null ==> this.contractProvider != null;
    {
      BlockStatement mutableBlockStatement = new BlockStatement(blockStatement);
      return this.Visit(mutableBlockStatement, method, methodContract);
    }

    private IBlockStatement Visit(BlockStatement blockStatement, IMethodDefinition/*?*/ method, IMethodContract/*?*/ methodContract)
      //^ requires method == null ==> methodContract == null;
      //^ requires methodContract != null ==> this.contractProvider != null;
    {
      NestedTypeDefinition savedCurrentClosureClass = this.CurrentClosureClass;
      this.CreateClosureClassIfNecessary(blockStatement, methodContract);
      blockStatement.Statements = this.Visit(blockStatement.Statements);
      if (methodContract != null)
        this.contractProvider.AssociateMethodWithContract(method, this.Visit(methodContract));
      if (savedCurrentClosureClass != this.CurrentClosureClass) {
        this.CurrentClosureClass = savedCurrentClosureClass;
        this.closureLocals.RemoveAt(0);
        if (this.outerClosures.Count > 0)
          this.outerClosures.RemoveAt(0);
      }
      return blockStatement;
    }

    /// <summary>
    /// Visits the specified global method definition.
    /// </summary>
    /// <param name="globalMethodDefinition">The global method definition.</param>
    public override IGlobalMethodDefinition Visit(IGlobalMethodDefinition globalMethodDefinition) {
      var result = base.Visit(globalMethodDefinition);
      this.isAlreadyNormalized.Add(globalMethodDefinition, true);
      return result;
    }

    /// <summary>
    /// Visits the specified method definitions.
    /// </summary>
    /// <param name="methodDefinitions">The method definitions.</param>
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

    /// <summary>
    /// Visits the specified nested type definitions.
    /// </summary>
    /// <param name="nestedTypeDefinitions">The nested type definitions.</param>
    public override List<INestedTypeDefinition> Visit(List<INestedTypeDefinition> nestedTypeDefinitions) {
      int n = nestedTypeDefinitions.Count;
      List<INestedTypeDefinition> result = new List<INestedTypeDefinition>(n);
      for (int i = 0; i < n; i++) {
        INestedTypeDefinition nt = nestedTypeDefinitions[i];
        if (AttributeHelper.Contains(nt.Attributes, this.host.PlatformType.SystemRuntimeCompilerServicesCompilerGeneratedAttribute)) {
          continue;
        }
        result.Add(this.Visit(this.GetMutableCopy(nt)));
      }
      return result;
    }

    /// <summary>
    /// Visits the specified method body.
    /// </summary>
    /// <param name="methodBody">The method body.</param>
    public override IMethodBody Visit(IMethodBody methodBody) {
      ISourceMethodBody/*?*/ sourceMethodBody = methodBody as ISourceMethodBody;
      if (sourceMethodBody != null)
        return this.GetNormalizedSourceMethodBodyFor(this.GetCurrentMethod(), sourceMethodBody.Block);
      return base.Visit(methodBody);
    }

    /// <summary>
    /// Visits the specified block statement.
    /// </summary>
    /// <param name="blockStatement">The block statement.</param>
    public override IBlockStatement Visit(BlockStatement blockStatement) {
      return this.Visit(blockStatement, null, null);
    }

    /// <summary>
    /// Visits the specified method definition.
    /// </summary>
    /// <param name="methodDefinition">The method definition.</param>
    /// <returns></returns>
    public override IMethodDefinition Visit(IMethodDefinition methodDefinition) {
      if (this.isAlreadyNormalized.ContainsKey(methodDefinition)) {
        object result = methodDefinition;
        this.cache.TryGetValue(methodDefinition, out result);
        return (IMethodDefinition)result;
      }
      return base.Visit(methodDefinition);
    }

    /// <summary>
    /// Visits the specified method contract.
    /// </summary>
    /// <param name="methodContract">The method contract.</param>
    public override IMethodContract Visit(IMethodContract methodContract) {
      object/*?*/ result = null;
      if (this.cache.TryGetValue(methodContract, out result)) return (IMethodContract)result;
      result = base.Visit(methodContract);
      this.cache.Add(methodContract, result);
      return (IMethodContract)result;
    }

    /// <summary>
    /// Visits the specified method definition.
    /// </summary>
    /// <param name="methodDefinition">The method definition.</param>
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

    /// <summary>
    /// Visits the specified anonymous delegate.
    /// </summary>
    /// <param name="anonymousDelegate">The anonymous delegate.</param>
    public override IExpression Visit(AnonymousDelegate anonymousDelegate) {
      var method = new MethodDefinition();
      this.CurrentClosureClass.Methods.Add(method);
      method.CallingConvention = anonymousDelegate.CallingConvention;
      method.ContainingTypeDefinition = this.CurrentClosureClass;
      method.IsCil = true;
      method.IsHiddenBySignature = true;
      if ((anonymousDelegate.CallingConvention & CallingConvention.HasThis) == 0)
        method.IsStatic = true;
      method.Name = this.host.NameTable.GetNameFor("__anonymous_method " + this.CurrentClosureClass.Methods.Count);
      method.Parameters = new List<IParameterDefinition>(anonymousDelegate.Parameters);
      if (anonymousDelegate.ReturnValueIsModified)
        method.ReturnValueCustomModifiers = new List<ICustomModifier>(anonymousDelegate.ReturnValueCustomModifiers);
      method.ReturnValueIsByRef = anonymousDelegate.ReturnValueIsByRef;
      method.Type = anonymousDelegate.ReturnType;
      method.Visibility = TypeMemberVisibility.Public;
      method.Body = this.GetMethodBodyFrom(anonymousDelegate.Body, method);

      var boundCurrentClosureLocal = new BoundExpression();
      if (this.closureLocals.Count == 0)
        boundCurrentClosureLocal.Definition = this.CreateClosureTemp(this.CurrentClosureClass);
      else
        boundCurrentClosureLocal.Definition = this.closureLocals[0];

      var createDelegateInstance = new CreateDelegateInstance();
      if ((anonymousDelegate.CallingConvention & CallingConvention.HasThis) != 0)
        createDelegateInstance.Instance = boundCurrentClosureLocal;
      createDelegateInstance.MethodToCallViaDelegate = method;
      createDelegateInstance.Type = this.Visit(anonymousDelegate.Type);

      return createDelegateInstance;
    }

    /// <summary>
    /// Visits the specified local declaration statement.
    /// </summary>
    /// <param name="localDeclarationStatement">The local declaration statement.</param>
    public override IStatement Visit(LocalDeclarationStatement localDeclarationStatement) {
      var originalLocalVariable = localDeclarationStatement.LocalVariable;
      localDeclarationStatement.LocalVariable = this.Visit(this.GetMutableCopy(originalLocalVariable));
      if (localDeclarationStatement.InitialValue != null) {
        var source = this.Visit(localDeclarationStatement.InitialValue);
        BoundField/*?*/ boundField;
        if (this.FieldForCapturedLocalOrParameter.TryGetValue(originalLocalVariable, out boundField)) {
          var currentClosureLocal = this.closureLocals[0];
          var currentClosureLocalBinding = new BoundExpression() { Definition = currentClosureLocal, Type = currentClosureLocal.Type };
          var target = new TargetExpression() { Instance = currentClosureLocalBinding, Definition = boundField.Field, Type = boundField.Type };
          var assignment = new Assignment() { Target = target, Source = source, Type = boundField.Type };
          return new ExpressionStatement() { Expression = assignment, Locations = localDeclarationStatement.Locations };
        } else {
          return base.Visit(localDeclarationStatement);
        }
      }
      return localDeclarationStatement;
    }

    /// <summary>
    /// Visits the specified target expression.
    /// </summary>
    /// <param name="targetExpression">The target expression.</param>
    public override ITargetExpression Visit(TargetExpression targetExpression) {
      BoundField/*?*/ boundField;
      if (this.FieldForCapturedLocalOrParameter.TryGetValue(targetExpression.Definition, out boundField)) {
        targetExpression.Instance = this.ClosureInstanceFor(boundField.Field.ContainingTypeDefinition);
        targetExpression.Definition = boundField.Field;
        targetExpression.Type = this.Visit(targetExpression.Type);
        return targetExpression;
      }
      return base.Visit(targetExpression);
    }


    /// <summary>
    /// Visits the specified addressable expression.
    /// </summary>
    /// <param name="addressableExpression">The addressable expression.</param>
    public override IAddressableExpression Visit(AddressableExpression addressableExpression) {
      BoundField/*?*/ boundField;
      if (this.FieldForCapturedLocalOrParameter.TryGetValue(addressableExpression.Definition, out boundField)) {
        addressableExpression.Instance = this.ClosureInstanceFor(boundField.Field.ContainingTypeDefinition);
        addressableExpression.Definition = boundField.Field;
        addressableExpression.Type = this.Visit(addressableExpression.Type);
        return addressableExpression;
      }
      return base.Visit(addressableExpression);
    }

    /// <summary>
    /// Visits the specified bound expression.
    /// </summary>
    /// <param name="boundExpression">The bound expression.</param>
    public override IExpression Visit(BoundExpression boundExpression) {
      BoundField/*?*/ boundField;
      if (this.FieldForCapturedLocalOrParameter.TryGetValue(boundExpression.Definition, out boundField)) {
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


    private IMethodBody GetMethodBodyFrom(IBlockStatement block, IMethodDefinition method) {
      var bodyFixer = new FixAnonymousDelegateBodyToUseClosure(this.FieldForCapturedLocalOrParameter, this.cache, this.CurrentClosureClass, this.outerClosures,
        this.host, this.sourceLocationProvider);
      block = bodyFixer.Visit(block);
      var result = new SourceMethodBody(this.host, this.sourceLocationProvider, this.contractProvider);
      result.Block = block;
      result.IsNormalized = true;
      result.LocalsAreZeroed = true;
      result.MethodDefinition = method;
      return result;
    }

    private IExpression ClosureInstanceFor(ITypeDefinition closure) {
      IExpression/*?*/ result = CodeDummy.Expression;
      if (!this.closureInstanceFor.TryGetValue(closure, out result)) {
        Debug.Assert(false);
      }
      return result;
    }
    Dictionary<ITypeDefinition, IExpression> closureInstanceFor = new Dictionary<ITypeDefinition, IExpression>();
  }

  /// <summary>
  /// Create closure class, including all its members for an iterator method and rewrite the body of the iterator method.
  /// 
  /// Specifically, we:
  /// 1) creates a closure class that implements: IEnumerator, generic and nongeneric versions,
  /// IEnumerable, generic and nongeneric versions, and IDisposable. The generic versions of IEnumerator
  /// and IEnumerator is instantiated by a type T that is used to instantiate the return type of
  /// the iterator method. The members of the closure class include:
  /// 1.1) fields corresponding to every parameter and local variables.
  /// 1.2) fields that manages the state machine: __current and __state, and a currentThreadId field.
  /// 1.3) a constructor that takes one int argument.  
  /// 1.4) methods that are required by the interfaces of the closure class: MoveNext, Reset,
  /// GetEnumerator, Current getter, and DisposeMethod. 
  /// 2) creates the new body of the iterator method: which returns a local variable that holds a new object of the closure class, with the fields of 
  /// the closure class that correspond to the parameters (including the self parameter if applicable)
  /// initialized. 
  /// 3) transforms the body, which should now not contain any annonymous delegates, into the body of the 
  /// MoveNext method of the closure class. This includes:
  /// 3.1) every local/parameter reference -> this.field reference
  /// 3.2) every yield return or yield break -> assignment to current and return true or false, respectively.
  /// 3.3) a switch statement for the state machine, with state values corresponds to each yield return/yield break.
  /// 3.4) try statement for foreach if the iterator method body uses one. 
  /// 4) If the iterator method has a type parameter, so will the closure class. Make sure in the closure class only
  /// the right type parameter is referenced. 
  /// </summary>
  internal class IteratorClosureGenerator : BaseCodeTraverser {

    /// <summary>
    /// Create closure class, including all its members for an iterator method and rewrite the body of the iterator method.
    /// 
    /// Specifically, we
    /// 1) creates a closure class that implements: IEnumerator, generic and nongeneric versions,
    /// IEnumerable, generic and nongeneric versions, and IDisposable. The generic versions of IEnumerator
    /// and IEnumerator is instantiated by a type T that is used to instantiate the return type of
    /// the iterator method. The members of the closure class include:
    /// 1.1) fields corresponding to every parameter and local variables.
    /// 1.2) fields that manages the state machine: __current and __state, and a currentThreadId field.
    /// 1.3) a constructor that takes one int argument.  
    /// 1.4) methods that are required by the interfaces of the closure class: MoveNext, Reset,
    /// GetEnumerator, Current getter, and DisposeMethod. 
    /// 2) creates the new body of the iterator method: which returns a local variable that holds a new object of the closure class, with the fields of 
    /// the closure class that correspond to the parameters (including the self parameter if applicable)
    /// initialized. 
    /// 3) transforms the body, which should now not contain any annonymous delegates, into the body of the 
    /// MoveNext method of the closure class. This includes:
    /// 3.1) every local/parameter reference -> this.field reference
    /// 3.2) every yield return or yield break -> assignment to current and return true or false, respectively.
    /// 3.3) a switch statement for the state machine, with state values corresponds to each yield return/yield break.
    /// 3.4) try statement for foreach if the iterator method body uses one. 
    /// 4) If the iterator method has a type parameter, so will the closure class. Make sure in the closure class only
    /// the right type parameter is referenced. 
    /// </summary>
    internal IteratorClosureGenerator(Dictionary<object, BoundField> fieldForCapturedLocalOrParameter, Dictionary<IBlockStatement, uint> iteratorLocalCount,
        IMethodDefinition method, List<ITypeDefinition> privateHelperTypes, IMetadataHost host,
      ISourceLocationProvider/*?*/ sourceLocationProvider, ContractProvider/*?*/ contractProvider)
    {
      this.privateHelperTypes = privateHelperTypes;
      this.method = method;
      this.fieldForCapturedLocalOrParameter = fieldForCapturedLocalOrParameter;
      this.iteratorLocalCount = iteratorLocalCount;
      this.host = host; this.contractProvider = contractProvider; this.sourceLocationProvider = sourceLocationProvider;
    }

    IMetadataHost host;
    ISourceLocationProvider/*?*/ sourceLocationProvider; 
    ContractProvider/*?*/ contractProvider;

    Dictionary<IBlockStatement, uint> iteratorLocalCount;
    /// <summary>
    /// Iterator method.
    /// </summary>
    private IMethodDefinition method = Dummy.Method;
    /// <summary>
    /// List of helper types generated during compilation. We shall add the iterator closure to the list. 
    /// </summary>
    private List<ITypeDefinition> privateHelperTypes;
    /// <summary>
    /// List of all locals in the body of iterator method.
    /// </summary>
    private List<ILocalDefinition> allLocals;

    CopyTypeFromIteratorToClosure copyTypeToClosure;
    /// <summary>
    /// Mapping between method type parameters (of the iterator method) and generic type parameters (of the closure class).
    /// </summary>
    Dictionary<IGenericMethodParameter, IGenericTypeParameter> genericTypeParameterMapping = new Dictionary<IGenericMethodParameter, IGenericTypeParameter>();
    /// <summary>
    /// Mapping between parameters and locals to the fields of the closure class. 
    /// </summary>
    internal Dictionary<object, BoundField> FieldForCapturedLocalOrParameter {
      get { return fieldForCapturedLocalOrParameter; }
    }

    Dictionary<object, BoundField> fieldForCapturedLocalOrParameter;
   
    /// <summary>
    /// Compile the method body, represented by <paramref name="block"/>. It creates the closure class and all its members
    /// and creates a new body for the iterator method. 
    /// </summary>
    internal IBlockStatement CompileIterator(IBlockStatement block) {
      this.allLocals = new List<ILocalDefinition>();
      this.Visit(block);
      BlockStatement mutableBlockStatement = new BlockStatement(block);
      IteratorClosureInformation iteratorClosure = this.CreateIteratorClosure(mutableBlockStatement);
      IBlockStatement result = this.CreateNewIteratorMethodBody(mutableBlockStatement, iteratorClosure);
      return result;
    }

    /// <summary>
    /// Constructor of the CompilerGeneratedAttribute.
    /// </summary>
    private IMethodReference CompilerGeneratedCtor {
      get {
        if (this.compilerGeneratedCtor == null)
          this.compilerGeneratedCtor = new Microsoft.Cci.MethodReference(this.host, this.host.PlatformType.SystemRuntimeCompilerServicesCompilerGeneratedAttribute,
             CallingConvention.HasThis, this.host.PlatformType.SystemVoid, this.host.NameTable.Ctor, 0);
        return this.compilerGeneratedCtor;
      }
    }
    private IMethodReference/*?*/ compilerGeneratedCtor;

    /// <summary>
    /// Constructor of the constructor of the Object class. 
    /// </summary>
    private IMethodReference ObjectCtor {
      get {
        if (this.objectCtor == null)
          this.objectCtor = new Microsoft.Cci.MethodReference(this.host, this.host.PlatformType.SystemObject, CallingConvention.HasThis,
             this.host.PlatformType.SystemVoid, this.host.NameTable.Ctor, 0);
        return this.objectCtor;
      }
    }
    private IMethodReference/*?*/ objectCtor;

    /// <summary>
    /// Constructor of the DebuggerHiddenAttribute.
    /// </summary>
    private IMethodReference/*?*/ debuggerHiddenCtor;
    private IMethodReference DebuggerHiddenCtor {
      get {
        IUnitNamespaceReference ns = this.host.PlatformType.SystemObject.ContainingUnitNamespace;
        ns = new Microsoft.Cci.NestedUnitNamespaceReference(ns, this.host.NameTable.GetNameFor("Diagnostics"));
        var debuggerHiddenClass = new Microsoft.Cci.NamespaceTypeReference(this.host, ns, this.host.NameTable.GetNameFor("DebuggerHiddenAttribute"), 0, false, false, PrimitiveTypeCode.Reference);
        if (this.debuggerHiddenCtor == null) {
          this.debuggerHiddenCtor = new Microsoft.Cci.MethodReference(this.host, debuggerHiddenClass, CallingConvention.HasThis, this.host.PlatformType.SystemVoid, this.host.NameTable.Ctor, 0);
        }
        return this.debuggerHiddenCtor;
      }
    }

    /// <summary>
    /// Create the new body of the iterator method. 
    /// </summary>
    /// <remarks>
    /// Pseudo code:
    /// iteratorClosureLocal = new Closure(-2);
    /// iteratorClosureLocal.field = parameter; // for each parameter including this. 
    /// return iteratorClosureLocal;
    /// </remarks>
    private IBlockStatement CreateNewIteratorMethodBody(BlockStatement block, IteratorClosureInformation iteratorClosure) {
      BlockStatement result = new BlockStatement();
      // iteratorClosureLocal = new IteratorClosure(-2);
      LocalDefinition localDefinition = new LocalDefinition() {
        Name = this.host.NameTable.GetNameFor("iteratorClosureLocal"),
        Type = GetClosureTypeReferenceFromIterator(iteratorClosure),
        Locations = block.Locations
      };
      CreateObjectInstance createObjectInstance = new CreateObjectInstance() {
        MethodToCall = GetMethodReference(iteratorClosure, iteratorClosure.Constructor), 
        Locations = block.Locations, 
        Type = localDefinition.Type
      };
      createObjectInstance.Arguments.Add(new CompileTimeConstant() { Value = -2, Type = this.host.PlatformType.SystemInt32 });
      LocalDeclarationStatement localDeclarationStatement = new LocalDeclarationStatement() { 
        InitialValue = createObjectInstance, 
        LocalVariable = localDefinition
      };
      result.Statements.Add(localDeclarationStatement);
      // Generate assignments to closure instance's fields for each of the parameters captured by the closure. 
      foreach (object capturedLocalOrParameter in FieldForCapturedLocalOrParameter.Keys) {
        BoundField boundField = FieldForCapturedLocalOrParameter[capturedLocalOrParameter];
        Assignment assignment;
        ITypeReference localOrParameterType = GetLocalOrParameterType(capturedLocalOrParameter);
        if (capturedLocalOrParameter is ILocalDefinition) continue;
        if (capturedLocalOrParameter is IThisReference) {
          assignment = new Assignment {
            Source = new ThisReference(),
            Type = this.method.ContainingType,
            Target = new TargetExpression() {
              Definition = GetFieldReference(iteratorClosure, boundField.Field),
              Type = this.method.ContainingType,
              Instance = new BoundExpression() { 
                Type = localDefinition.Type, 
                Instance = null, 
                Definition = localDefinition, 
                IsVolatile = false 
              }
            },
          };
        } else {
          assignment = new Assignment {
            Source = new BoundExpression() { 
              Definition = capturedLocalOrParameter, 
              Instance = null, 
              IsVolatile = false, 
              Type = localOrParameterType 
            },
            Type = localOrParameterType,
            Target = new TargetExpression() { 
              Definition = GetFieldReference(iteratorClosure, boundField.Field), 
              Type = localOrParameterType, 
              Instance = new BoundExpression() { 
                Type = localDefinition.Type, 
                Instance = null, 
                Definition = localDefinition, 
                IsVolatile = false } 
            },
          };
        }
        ExpressionStatement expressionStatement = new ExpressionStatement() { Expression = assignment };
        result.Statements.Add(expressionStatement);
      }
      // Generate: return iteratorClosureLocal;
      result.Statements.Add(new ReturnStatement() {
        Expression = new BoundExpression() { Definition = localDeclarationStatement.LocalVariable, Instance = null, Type = localDeclarationStatement.LocalVariable.Type }
      });
      return result;
    }

    /// <summary>
    /// Return the type of the parameter (excluding this) or local variable represented by <paramref name="obj"/>.
    /// </summary>
    private ITypeReference GetLocalOrParameterType(object obj) {
      IParameterDefinition parameterDefinition = obj as IParameterDefinition;
      if (parameterDefinition != null) {
        return parameterDefinition.Type;
      }
      ILocalDefinition localDefinition = obj as ILocalDefinition;
      if (localDefinition != null) return localDefinition.Type;
      return Dummy.Type;
    }

    /// <summary>
    /// Instantiate the closure class using the generic method parameters of the iterator method, if any. 
    /// </summary>
    ITypeReference GetClosureTypeReferenceFromIterator(IteratorClosureInformation iteratorClosure) {
      ITypeReference closureReference = iteratorClosure.ClosureDefinitionReference;
      if (this.method.IsGeneric) {
        IGenericTypeInstanceReference genericTypeInstanceRef = closureReference as IGenericTypeInstanceReference;
        Debug.Assert(genericTypeInstanceRef != null);
        if (genericTypeInstanceRef != null) {
          List<ITypeReference> args = new List<ITypeReference>();
          foreach (var t in method.GenericParameters) args.Add(t);
          return GenericTypeInstance.GetGenericTypeInstance(genericTypeInstanceRef.GenericType, args, this.host.InternFactory);
        }
      }
      return closureReference;
    }

    /// <summary>
    /// Instantiate a closure class field using the generic method parameters of the iterator method, if any. 
    /// Code Review: cache the result of GetClosureTypeReferenceFromIterator.
    /// </summary>
    /// <param name="iteratorClosure"></param>
    /// <param name="fieldDefinition"></param>
    /// <returns></returns>
    IFieldReference GetFieldReference(IteratorClosureInformation iteratorClosure, /*unspecialized*/IFieldDefinition fieldDefinition) {
      ITypeReference typeReference = GetClosureTypeReferenceFromIterator(iteratorClosure);
      IGenericTypeInstanceReference genericInstanceRef = typeReference as IGenericTypeInstanceReference;
      ISpecializedNestedTypeReference specializedNestedTypeRef = typeReference as ISpecializedNestedTypeReference;
      IFieldReference fieldReference = fieldDefinition;
      if (genericInstanceRef != null || specializedNestedTypeRef != null) {
        fieldReference = new SpecializedFieldReference() {
          ContainingType = typeReference,
          Name = fieldDefinition.Name,
          UnspecializedVersion = fieldDefinition,
          Type = fieldDefinition.Type
        };
      }
      return fieldReference;
    }

    /// <summary>
    /// Instantiate a closure class method using the generic method parameters of the iterator method, if any. 
    /// </summary>
    IMethodReference GetMethodReference(IteratorClosureInformation iteratorClosure, IMethodDefinition methodDefinition) {
      ITypeReference typeReference = GetClosureTypeReferenceFromIterator(iteratorClosure);
      //foreach (var m in typeReference.ResolvedType.Methods) {
      //  if (m.Name == methodDefinition.Name) return m;
      //}
      IMethodReference methodReference = methodDefinition;
      ISpecializedNestedTypeReference specializedNestedTypeRef = typeReference as ISpecializedNestedTypeReference;
      IGenericTypeInstanceReference genericInstanceRef = typeReference as IGenericTypeInstanceReference;
      if (specializedNestedTypeRef != null || genericInstanceRef != null) {
        methodReference = new SpecializedMethodReference() {
          ContainingType = typeReference,
          GenericParameterCount = methodDefinition.GenericParameterCount,
          InternFactory = this.host.InternFactory,
          UnspecializedVersion = methodDefinition,
          Type = methodDefinition.Type,
          Name = methodDefinition.Name,
          CallingConvention = methodDefinition.CallingConvention,
          IsGeneric = methodDefinition.IsGeneric,
          Parameters = new List<IParameterTypeInformation>(((IMethodReference)methodDefinition).Parameters),
          ExtraParameters = new List<IParameterTypeInformation>(((IMethodReference)methodDefinition).ExtraParameters),
          ReturnValueIsByRef = methodDefinition.ReturnValueIsByRef,
          ReturnValueIsModified = methodDefinition.ReturnValueIsModified,
          Attributes = new List<ICustomAttribute>(methodDefinition.Attributes)
        };
      }
      return methodReference;
    }

    /// <summary>
    /// Create the iterator closure class and add it to the private helper types list. 
    /// </summary>
    private IteratorClosureInformation CreateIteratorClosure(BlockStatement blockStatement) {
      IteratorClosureInformation result = new IteratorClosureInformation(this.host);
      CustomAttribute compilerGeneratedAttribute = new CustomAttribute();
      compilerGeneratedAttribute.Constructor = this.CompilerGeneratedCtor;

      // Create the closure class with CompilerGeneratedAttribute, the list of generic type parameters isomorphic to 
      // those of the iterator method. 
      NestedTypeDefinition iteratorClosureType = new NestedTypeDefinition();
      this.privateHelperTypes.Add(iteratorClosureType);
      result.ClosureDefinition = iteratorClosureType;
      List<IGenericMethodParameter> genericMethodParameters = new List<IGenericMethodParameter>();
      ushort count =0;
      foreach (var genericMethodParameter in method.GenericParameters) {
        genericMethodParameters.Add(genericMethodParameter);
        GenericTypeParameter newTypeParam = new GenericTypeParameter() {
          Name = this.host.NameTable.GetNameFor(genericMethodParameter.Name.Value + "_"),
          Index = (count++)
        };
        this.genericTypeParameterMapping[genericMethodParameter] = newTypeParam;
        newTypeParam.DefiningType = iteratorClosureType;
        iteratorClosureType.GenericParameters.Add(newTypeParam);
      }

      this.copyTypeToClosure = new CopyTypeFromIteratorToClosure(this.host, this.genericTypeParameterMapping);

      // Duplicate Constraints
      foreach (var genericMethodParameter in genericMethodParameters) {
        GenericTypeParameter correspondingTypeParameter = (GenericTypeParameter)this.genericTypeParameterMapping[genericMethodParameter];
        if (genericMethodParameter.Constraints != null) {
          correspondingTypeParameter.Constraints = new List<ITypeReference>();
          foreach (ITypeReference t in genericMethodParameter.Constraints) {
            correspondingTypeParameter.Constraints.Add(copyTypeToClosure.Visit(t));
          }
        }
      }
      // elementTypes contains only one element, the argument type of the return type (the T in IEnumerable<T>) of the iterator method, or simply System.Object if the
      // iterator is not generic. 
      IEnumerable<ITypeReference> elementTypes = GetClosureEnumeratorTypeArguments(method.Type);
      // elementType of the IEnumerable. 
      ITypeReference elementType = null;
      foreach (ITypeReference t in elementTypes) {
        elementType = t; break;
      }
      result.ElementType = this.copyTypeToClosure.Visit(elementType);

      // Set up the iterator closure class. 
      // TODO: name generation to follow the convention of csc. 
      iteratorClosureType.Name = this.host.NameTable.GetNameFor("<" + this.method.Name.Value + ">" + "ic__" + this.privateHelperTypes.Count);
      iteratorClosureType.Attributes.Add(compilerGeneratedAttribute);
      iteratorClosureType.BaseClasses.Add(this.host.PlatformType.SystemObject);
      iteratorClosureType.ContainingTypeDefinition = this.method.ContainingTypeDefinition;
      iteratorClosureType.InternFactory = this.host.InternFactory;
      iteratorClosureType.IsBeforeFieldInit = true;
      iteratorClosureType.IsClass = true;
      iteratorClosureType.IsSealed = true;
      iteratorClosureType.Layout = LayoutKind.Auto;
      iteratorClosureType.StringFormat = StringFormatKind.Ansi;
      iteratorClosureType.Visibility = TypeMemberVisibility.Private;

      /* Interfaces. */
      result.InitializeInterfaces(result.ElementType);
     
      /* Fields, Methods, and Properties. */
      CreateIteratorClosureFields(result);
      CreateIteratorClosureConstructor(result);
      CreateIteratorClosureMethods(result, blockStatement);
      CreateIteratorClosureProperties(result);
      return result;
    }

    /// <summary>
    /// Create the constuctor of the iterator class. The pseudo-code is: 
    /// 
    /// Ctor(int state) {
    ///   object.Ctor();
    ///   this.state = state;
    ///   this.threadid = Thread.CurrentThread.ManagedThreadId;
    /// }
    /// </summary>
    private void CreateIteratorClosureConstructor(IteratorClosureInformation iteratorClosure) {
      MethodDefinition constructor = new MethodDefinition();
      // Parameter
      ParameterDefinition stateParameter = new ParameterDefinition() {
        ContainingSignature = constructor,
        Index = 0,
        Name = this.host.NameTable.GetNameFor("state"),
        Type = this.host.PlatformType.SystemInt32
      };
      constructor.Parameters.Add(stateParameter);
      // Statements
      MethodCall baseConstructorCall = new MethodCall() { ThisArgument = new BaseClassReference(), MethodToCall = this.ObjectCtor, Type = this.host.PlatformType.SystemVoid };
      ExpressionStatement baseConstructorCallStatement = new ExpressionStatement() { Expression = baseConstructorCall };
      List<IStatement> statements = new List<IStatement>();
      ExpressionStatement thisDotStateEqState = new ExpressionStatement() {
        Expression = new Assignment() {
          Source = new BoundExpression() { Definition = stateParameter, Instance = null, Type = this.host.PlatformType.SystemInt32 },
          Target = new TargetExpression() { Instance = new ThisReference(), Type = this.host.PlatformType.SystemInt32, Definition = iteratorClosure.StateFieldReference },
          Type = this.host.PlatformType.SystemInt32
        }
      };
      ExpressionStatement thisThreadIdEqCurrentThreadId = new ExpressionStatement() {
        Expression = new Assignment() {
          Source = new MethodCall() {
            MethodToCall = this.ThreadDotManagedThreadId.Getter,
            ThisArgument = this.ThreadDotCurrentThread,
            Type = this.host.PlatformType.SystemInt32
          },
          Target = new TargetExpression() { Instance = new ThisReference(), Type = this.host.PlatformType.SystemInt32, Definition = iteratorClosure.InitThreadIdFieldReference },
          Type = this.host.PlatformType.SystemInt32
        }
      };
      statements.Add(baseConstructorCallStatement);
      statements.Add(thisDotStateEqState);
      statements.Add(thisThreadIdEqCurrentThreadId);
      BlockStatement block = new BlockStatement() { Statements = statements };
      SourceMethodBody body = new SourceMethodBody(this.host, this.sourceLocationProvider, this.contractProvider);
      body.LocalsAreZeroed = true;
      body.IsNormalized = true;
      body.Block = block;
      constructor.Body = body;
      body.MethodDefinition = constructor;
      // Metadata of the constructor
      constructor.CallingConvention = CallingConvention.HasThis;
      constructor.ContainingTypeDefinition = iteratorClosure.ClosureDefinition;
      constructor.IsCil = true;
      constructor.IsHiddenBySignature = true;
      constructor.IsRuntimeSpecial = true;
      constructor.IsSpecialName = true;
      constructor.Name = this.host.NameTable.Ctor;
      constructor.Type = this.host.PlatformType.SystemVoid;
      constructor.Visibility = TypeMemberVisibility.Public;
      iteratorClosure.Constructor = constructor;
    }
    /// <summary>
    /// Create the methods of the iterator closure. 
    /// </summary>
    /// <param name="iteratorClosure"></param>
    /// <param name="blockStatement"></param>
    private void CreateIteratorClosureMethods(IteratorClosureInformation iteratorClosure, BlockStatement blockStatement) {
      // Reset and DisposeMethod currently do nothing. 
      CreateResetMethod(iteratorClosure);
      CreateDisposeMethod(iteratorClosure);
      // Two versions of GetEnumerator for generic and non-generic interfaces.
      CreateGetEnumeratorMethodGeneric(iteratorClosure);
      CreateGetEnumeratorMethodNonGeneric(iteratorClosure);
      // MoveNext
      CreateMoveNextMethod(iteratorClosure, blockStatement);
    }

    /// <summary>
    /// Create the MoveNext method. This method sets up metadata and calls TranslateIteratorMethodBodyToMoveNextBody
    /// to compile the body. 
    /// </summary>
    private void CreateMoveNextMethod(IteratorClosureInformation /*!*/ iteratorClosure, BlockStatement blockStatement) {
      // Method definition and metadata.
      MethodDefinition moveNext = new MethodDefinition() {
        Name = this.host.NameTable.GetNameFor("MoveNext")
      };
      moveNext.ContainingTypeDefinition = iteratorClosure.ClosureDefinition;
      moveNext.Visibility = TypeMemberVisibility.Private;
      moveNext.CallingConvention |= CallingConvention.HasThis;
      moveNext.Type = this.host.PlatformType.SystemBoolean;
      moveNext.InternFactory = this.host.InternFactory;
      moveNext.IsSealed = true;
      moveNext.IsVirtual = true;
      moveNext.IsHiddenBySignature = true;
      moveNext.IsNewSlot = true;
      iteratorClosure.MoveNext = moveNext;
      IMethodReference moveNextOriginal = Dummy.MethodReference;
      foreach (ITypeMemberReference tmref in iteratorClosure.NonGenericIEnumeratorInterface.ResolvedType.GetMembersNamed(this.host.NameTable.GetNameFor("MoveNext"), false)) {
        moveNextOriginal = tmref as IMethodReference;
        if (moveNextOriginal != null) break;
      }
      // Explicit method implementation
      MethodImplementation moveNextImp = new MethodImplementation() {
        ContainingType = iteratorClosure.ClosureDefinition,
        ImplementingMethod = moveNext,
        ImplementedMethod = moveNextOriginal
      };
      iteratorClosure.ClosureDefinition.ExplicitImplementationOverrides.Add(moveNextImp);

      SourceMethodBody body = new SourceMethodBody(this.host, this.sourceLocationProvider, this.contractProvider, this.iteratorLocalCount);
      IBlockStatement block = TranslateIteratorMethodBodyToMoveNextBody(iteratorClosure, blockStatement);
      moveNext.Body = body;
      body.IsNormalized = true;
      body.LocalsAreZeroed = true;
      body.Block = block;
      body.MethodDefinition = moveNext;
    }

    /// <summary>
    /// Create method body of the MoveNext from the body of the iterator method.
    /// 
    /// First we substitute the locals/parameters with closure fields, and generic method type parameter of the iterator
    /// method with generic type parameters of the closure class (if any). 
    /// Then, we build the state machine. 
    /// </summary>
    private IBlockStatement TranslateIteratorMethodBodyToMoveNextBody(IteratorClosureInformation iteratorClosure, BlockStatement blockStatement) {
      // Copy and substitution.
      CopyToIteratorClosure copier = new CopyToIteratorClosure(this.FieldForCapturedLocalOrParameter, this.iteratorLocalCount, this.genericTypeParameterMapping, iteratorClosure, this.host);
      IBlockStatement result = copier.Visit(blockStatement);
      // State machine.
      Dictionary<int, ILabeledStatement> StateEntries = new YieldReturnYieldBreakReplacer(iteratorClosure, this.host).GetStateEntries(blockStatement);
      result = BuildStateMachine(iteratorClosure, (BlockStatement)result, StateEntries);
      return result;
    }

    /// <summary>
    /// Build the state machine. 
    /// 
    /// We start from state 0. For each yield return, we assign a unique state, which we call continueing state. For a yield return 
    /// assigned with state x, we move the state machine from the previous state to x. Whenever we see a yield break, we transit
    /// the state to -1. 
    /// 
    /// When we return from state x, we jump to a label that is inserted right after the previous yield return (that is assigned with state x). 
    /// </summary>
    private BlockStatement BuildStateMachine(IteratorClosureInformation iteratorClosure, BlockStatement oldBody, Dictionary<int, ILabeledStatement> stateEntries) {
      // Switch on cases. StateEntries, which have been computed previously, map a state number (for initial and continuing states) to a label that has been inserted 
      // right after the associated yield return. 
      BlockStatement result = new BlockStatement();
      var returnFalse = new ReturnStatement() { Expression = new CompileTimeConstant() { Value = false, Type = this.host.PlatformType.SystemBoolean} };
      var returnFalseLabel = new LabeledStatement(){ Label = this.host.NameTable.GetNameFor("return false"), Statement = returnFalse};
      List<ISwitchCase> cases = new List<ISwitchCase>();
      foreach (int i in stateEntries.Keys) {
        SwitchCase c = new SwitchCase() {
          Expression = new CompileTimeConstant() { Type = this.host.PlatformType.SystemInt32, Value = i },
          Body = new List<IStatement>(),
        };
        c.Body.Add(new GotoStatement() { TargetStatement = stateEntries[i] });
        cases.Add(c);
      }
      // Default case.
      SwitchCase defaultCase = new SwitchCase();
      defaultCase.Body.Add(new GotoStatement(){ TargetStatement = returnFalseLabel });
      cases.Add(defaultCase);
      SwitchStatement switchStatement = new SwitchStatement() {
        Cases = cases,
        Expression = new BoundExpression() { Type = this.host.PlatformType.SystemInt32, Instance = new ThisReference(), Definition = iteratorClosure.StateFieldReference }
      };
      result.Statements.Add(switchStatement);
      result.Statements.Add(oldBody);
      result.Statements.Add(returnFalseLabel);
      return result;
    }

    /// <summary>
    /// Create the Reset method. Like in CSC, this method contains nothing. 
    /// </summary>
    private void CreateResetMethod(IteratorClosureInformation iteratorClosure) {
      // System.Collections.IEnumerator.Reset: Simply throws an exception
      MethodDefinition reset = new MethodDefinition() {
        Name = this.host.NameTable.GetNameFor("Reset")
      };
      CustomAttribute debuggerHiddenAttribute = new CustomAttribute() { Constructor = this.DebuggerHiddenCtor };
      reset.Attributes.Add(debuggerHiddenAttribute);
      reset.CallingConvention |= CallingConvention.HasThis;
      reset.Visibility = TypeMemberVisibility.Private;
      reset.ContainingTypeDefinition = iteratorClosure.ClosureDefinition;
      reset.Type = this.host.PlatformType.SystemVoid;
      reset.IsVirtual = true; 
      reset.IsNewSlot = true; 
      reset.IsHiddenBySignature = true; 
      reset.IsSealed = true;
      iteratorClosure.Reset = reset;
      // explicitly state that this reset method implements IEnumerator's reset method. 
      IMethodReference resetImplemented = Dummy.MethodReference;
      foreach (var memref in iteratorClosure.NonGenericIEnumeratorInterface.ResolvedType.GetMembersNamed(this.host.NameTable.GetNameFor("Reset"), false)) {
        IMethodReference mref = memref as IMethodReference;
        if (mref != null) { 
          resetImplemented = mref; 
          break; 
        }
      }
      MethodImplementation resetImp = new MethodImplementation() {
        ContainingType = iteratorClosure.ClosureDefinition,
        ImplementedMethod = resetImplemented,
        ImplementingMethod = reset
      };
      iteratorClosure.ClosureDefinition.ExplicitImplementationOverrides.Add(resetImp);
      List<IStatement> statements = new List<IStatement>();
      ReturnStatement returnCurrent = new ReturnStatement() {
        Expression = null,
        Locations = iteratorClosure.ClosureDefinition.Locations
      };
      statements.Add(returnCurrent);
      BlockStatement block = new BlockStatement() { Statements = statements };
      SourceMethodBody body = new SourceMethodBody(this.host, this.sourceLocationProvider, this.contractProvider);
      body.LocalsAreZeroed = true;
      body.IsNormalized = true;
      body.Block = block;
      body.MethodDefinition = reset;
      reset.Body = body;
    }

    /// <summary>
    /// DisposeMethod method. Currently the method body does nothing. 
    /// </summary>
    private void CreateDisposeMethod(IteratorClosureInformation iteratorClosure) {
      MethodDefinition disposeMethod = new MethodDefinition() {
        Name = this.host.NameTable.GetNameFor("Dispose")
      };
      disposeMethod.Attributes.Add(new CustomAttribute() { Constructor = this.debuggerHiddenCtor });
      disposeMethod.CallingConvention |= CallingConvention.HasThis;
      disposeMethod.Visibility = TypeMemberVisibility.Public;
      disposeMethod.ContainingTypeDefinition = iteratorClosure.ClosureDefinition;
      disposeMethod.Type = this.host.PlatformType.SystemVoid;
      disposeMethod.IsVirtual = true; 
      disposeMethod.IsNewSlot = true; 
      disposeMethod.IsHiddenBySignature = true; 
      disposeMethod.IsSealed = true;
      // Add disposeMethod to parent's member list. 
      iteratorClosure.DisposeMethod = disposeMethod;
      // Explicitly implements IDisposable's dispose. 
      IMethodReference disposeImplemented = Dummy.MethodReference;
      foreach (var memref in iteratorClosure.DisposableInterface.ResolvedType.GetMembersNamed(this.host.NameTable.GetNameFor("Dispose"), false)) {
        IMethodReference mref = memref as IMethodReference;
        if (mref != null) { 
          disposeImplemented = mref; 
          break; 
        }
      }
      MethodImplementation disposeImp = new MethodImplementation() {
        ContainingType = iteratorClosure.ClosureDefinition,
        ImplementedMethod = disposeImplemented,
        ImplementingMethod = disposeMethod
      };
      iteratorClosure.ClosureDefinition.ExplicitImplementationOverrides.Add(disposeImp);
      // Body is a sole return. 
      BlockStatement block = new BlockStatement();
      block.Statements.Add(new ReturnStatement() {
        Expression = null,
        Locations = iteratorClosure.ClosureDefinition.Locations
      });
      SourceMethodBody body = new SourceMethodBody(this.host, this.sourceLocationProvider, this.contractProvider);
      body.LocalsAreZeroed = true;
      body.IsNormalized = true;
      body.Block = block;
      body.MethodDefinition = disposeMethod;
      disposeMethod.Body = body;
    }

    /// <summary>
    /// Property definition of Thread.ManagedThreadId
    /// </summary>
    IPropertyDefinition/*?*/ ThreadDotManagedThreadId {
      get {
        var assemblyReference = new Microsoft.Cci.AssemblyReference(this.host, this.host.CoreAssemblySymbolicIdentity);
        IUnitNamespaceReference ns = new Microsoft.Cci.RootUnitNamespaceReference(assemblyReference);
        ns = new Microsoft.Cci.NestedUnitNamespaceReference(ns, this.host.NameTable.GetNameFor("System"));
        var SystemDotThreading = new Microsoft.Cci.NestedUnitNamespaceReference(ns, this.host.NameTable.GetNameFor("Threading"));
        ITypeReference ThreadingDotThread = new Microsoft.Cci.NamespaceTypeReference(this.host, SystemDotThreading, this.host.NameTable.GetNameFor("Thread"), 0, false, false, PrimitiveTypeCode.Reference);
        foreach (ITypeMemberReference memref in ThreadingDotThread.ResolvedType.GetMembersNamed(this.host.NameTable.GetNameFor("ManagedThreadId"), false)) {
          IPropertyDefinition propertyDef = memref as IPropertyDefinition;
          if (propertyDef != null) {
            return propertyDef;
          }
        }
        return Dummy.Property;
      }
    }

    /// <summary>
    /// An Expression that represents Thread.CurrentThread
    /// </summary>
    MethodCall ThreadDotCurrentThread {
      get {
        var assemblyReference = new Microsoft.Cci.AssemblyReference(this.host, this.host.CoreAssemblySymbolicIdentity);
        IUnitNamespaceReference ns = new Microsoft.Cci.RootUnitNamespaceReference(assemblyReference);
        ns = new Microsoft.Cci.NestedUnitNamespaceReference(ns, this.host.NameTable.GetNameFor("System"));
        var SystemDotThreading = new Microsoft.Cci.NestedUnitNamespaceReference(ns, this.host.NameTable.GetNameFor("Threading"));
        var ThreadingDotThread = new Microsoft.Cci.NamespaceTypeReference(this.host, SystemDotThreading, this.host.NameTable.GetNameFor("Thread"), 0, false, false, PrimitiveTypeCode.Reference);
        IMethodReference/*? !after search*/ CurrentThreadPropertyGetter = null;
        foreach (ITypeMemberReference memref in ThreadingDotThread.ResolvedType.GetMembersNamed(this.host.NameTable.GetNameFor("CurrentThread"), false)) {
          IPropertyDefinition property = memref as IPropertyDefinition;
          if (property != null) {
            CurrentThreadPropertyGetter = property.Getter; break;
          }
        }
        MethodCall result = new MethodCall {
          Type = ThreadingDotThread.ResolvedType,
          ThisArgument = null,
          MethodToCall = CurrentThreadPropertyGetter,
          IsStaticCall = true
        };
        return result;
      }
    }

    /// <summary>
    /// Create the generic version of the GetEnumerator for the iterator closure class. 
    /// </summary>
    /// <param name="iteratorClosure"></param>
    private void CreateGetEnumeratorMethodGeneric(IteratorClosureInformation iteratorClosure) {
      // Metadata
      MethodDefinition genericGetEnumerator = new MethodDefinition() {
        Name = this.host.NameTable.GetNameFor("System.Collections.Generic.IEnumerable<" + iteratorClosure.ElementType.ToString()+">.GetEnumerator")
      };
      CustomAttribute debuggerHiddenAttribute = new CustomAttribute() { Constructor = this.DebuggerHiddenCtor };
      genericGetEnumerator.Attributes.Add(debuggerHiddenAttribute);
      genericGetEnumerator.CallingConvention |= CallingConvention.HasThis;
      genericGetEnumerator.ContainingTypeDefinition = iteratorClosure.ClosureDefinition;
      genericGetEnumerator.Visibility = TypeMemberVisibility.Public;
      genericGetEnumerator.Type = iteratorClosure.GenericIEnumeratorInterface;
      genericGetEnumerator.IsVirtual = true;
      genericGetEnumerator.IsNewSlot = true;
      genericGetEnumerator.IsHiddenBySignature = true;
      genericGetEnumerator.IsSealed = true;
      // Membership 
      iteratorClosure.GenericGetEnumerator = genericGetEnumerator;
      IMethodReference genericGetEnumeratorOriginal = Dummy.MethodReference;
      // Explicit implementation of IEnumerable<T>.GetEnumerator
      foreach (var memref in iteratorClosure.GenericIEnumerableInterface.ResolvedType.GetMembersNamed(this.host.NameTable.GetNameFor("GetEnumerator"), false)) {
        IMethodReference mref = memref as IMethodReference;
        if (mref != null) { genericGetEnumeratorOriginal = mref; break; }
      }
      var genericGetEnumeratorImp = new MethodImplementation() {
        ContainingType = iteratorClosure.ClosureDefinition,
        ImplementingMethod = genericGetEnumerator,
        ImplementedMethod = genericGetEnumeratorOriginal
      };
      iteratorClosure.ClosureDefinition.ExplicitImplementationOverrides.Add(genericGetEnumeratorImp);
      // Body
      var block = GetBodyOfGenericGetEnumerator(iteratorClosure);
      var body = new SourceMethodBody(this.host, this.sourceLocationProvider, this.contractProvider);
      body.LocalsAreZeroed = true;
      body.IsNormalized = true;
      body.Block = block;
      body.MethodDefinition = genericGetEnumerator;
      genericGetEnumerator.Body = body;
    }

    /// <summary>
    /// Create the body of the generic version of GetEnumerator for the iterator closure class.
    /// 
    /// The body's pseudo code. 
    /// {
    ///   if (Thread.CurrentThread.ManagedThreadId == this.l_initialThreadId AND this.state == -2) {
    ///     this.state = 0;
    ///     return this;
    ///   }
    ///   else {
    ///     return a new copy of the iterator instance with state being zero.
    ///   }
    /// }
    /// </summary>
    private BlockStatement GetBodyOfGenericGetEnumerator(IteratorClosureInformation iteratorClosure) {
      var thisDotState = new BoundExpression() {
        Definition = iteratorClosure.StateFieldReference,
        Instance = new ThisReference(),
        Type = this.host.PlatformType.SystemInt32
      };
      var thisDotThreadId = new BoundExpression() {
        Definition = iteratorClosure.InitThreadIdFieldReference,
        Instance = new ThisReference(),
        Type = this.host.PlatformType.SystemInt32
      };
      var currentThreadId = new MethodCall() {
        MethodToCall = ThreadDotManagedThreadId.Getter,
        ThisArgument = ThreadDotCurrentThread,
        Type = this.host.PlatformType.SystemInt32
      };
      var stateEqMinus2 = new Equality() { LeftOperand = thisDotState, RightOperand = new CompileTimeConstant() { Type = this.host.PlatformType.SystemInt32, Value = -2 }, Type = this.host.PlatformType.SystemBoolean };
      var threadIdEqCurrentThreadId = new Equality { LeftOperand = thisDotThreadId, RightOperand = currentThreadId, Type = this.host.PlatformType.SystemBoolean };

      var thisDotStateEq0 = new ExpressionStatement() {
        Expression = new Assignment() {
          Source = new CompileTimeConstant() { Type = this.host.PlatformType.SystemInt32, Value = 0 },
          Target = new TargetExpression() {
            Definition = iteratorClosure.StateFieldReference,
            Instance = new ThisReference(),
            Type = this.host.PlatformType.SystemInt32
          },
          Type = this.host.PlatformType.SystemInt32
        },
      };
      var returnThis = new BlockStatement();
      returnThis.Statements.Add(thisDotStateEq0);
      returnThis.Statements.Add(new ReturnStatement() { Expression = new ThisReference() });
      var returnNew = new BlockStatement();
      var args = new List<IExpression>(); 
      args.Add(new CompileTimeConstant() { Value = 0, Type = this.host.PlatformType.SystemInt32 });
      var closureInstanceLocalDecl = new LocalDeclarationStatement() {
        LocalVariable = new LocalDefinition() {
          Name = this.host.NameTable.GetNameFor("local0"),
          Type = iteratorClosure.ClosureDefinitionReference
        },
        InitialValue = new CreateObjectInstance() {
          MethodToCall = iteratorClosure.ConstructorReference,
          Arguments = args,
          Type = iteratorClosure.ClosureDefinitionReference
        }
      };
      var returnNewClosureInstance = new ReturnStatement() {
        Expression = new BoundExpression() {
          Instance = null,
          Type = iteratorClosure.ClosureDefinitionReference,
          Definition = closureInstanceLocalDecl.LocalVariable
        }
      };
      returnNew.Statements.Add(closureInstanceLocalDecl);
      if (!method.IsStatic) {
        ExpressionStatement assignThisDotThisToNewClosureDotThis = new ExpressionStatement() {
          Expression = new Assignment() {
            Source = new BoundExpression() {
              Definition = iteratorClosure.ThisFieldReference,
              Instance = new ThisReference(),
              Type = iteratorClosure.ClosureDefinitionReference
            },
            Type = iteratorClosure.ClosureDefinition,
            Target = new TargetExpression() {
              Instance = new BoundExpression() {
                Instance = null,
                Definition = closureInstanceLocalDecl.LocalVariable,
                Type = iteratorClosure.ClosureDefinitionReference
              },
              Definition = iteratorClosure.ThisFieldReference,
              Type = iteratorClosure.ClosureDefinitionReference
            }
          }
        };
        returnNew.Statements.Add(assignThisDotThisToNewClosureDotThis);
      }
      returnNew.Statements.Add(returnNewClosureInstance);

      ConditionalStatement returnThisOrNew = new ConditionalStatement() {
        Condition = new Conditional() {
          Condition = stateEqMinus2,
          ResultIfTrue = threadIdEqCurrentThreadId,
          ResultIfFalse = new CompileTimeConstant() { Type = this.host.PlatformType.SystemBoolean, Value = false },
          Type = this.host.PlatformType.SystemBoolean
        },
        TrueBranch = returnThis,
        FalseBranch = returnNew
      };
      BlockStatement block = new BlockStatement();
      block.Statements.Add(returnThisOrNew);
      return block;
    }

    /// <summary>
    /// Create the non-generic version of GetEnumerator and add it to the member list of iterator closure class. 
    /// </summary>
    private void CreateGetEnumeratorMethodNonGeneric(IteratorClosureInformation iteratorClosure) {
      // GetEnumerator non-generic version, which delegates to the generic version. 
      // Metadata
      MethodDefinition nongenericGetEnumerator = new MethodDefinition() {
        Name = this.host.NameTable.GetNameFor("System.Collections.IEnumerable.GetEnumerator")
      };
      nongenericGetEnumerator.Attributes.Add(
        new CustomAttribute() { Constructor = this.DebuggerHiddenCtor }
        );
      nongenericGetEnumerator.CallingConvention |= CallingConvention.HasThis;
      nongenericGetEnumerator.ContainingTypeDefinition = iteratorClosure.ClosureDefinition;
      nongenericGetEnumerator.Visibility = TypeMemberVisibility.Public;
      nongenericGetEnumerator.Type = iteratorClosure.NonGenericIEnumeratorInterface;
      nongenericGetEnumerator.IsVirtual = true;
      nongenericGetEnumerator.IsNewSlot = true;
      nongenericGetEnumerator.IsHiddenBySignature = true;
      nongenericGetEnumerator.IsSealed = true;
      iteratorClosure.NonGenericGetEnumerator = nongenericGetEnumerator;
      // Explicitly implements IEnumerable.GetEnumerator();
      IMethodReference nongenericGetEnumeratorOriginal = Dummy.MethodReference;
      foreach (var memref in iteratorClosure.NonGenericIEnumerableInterface.ResolvedType.GetMembersNamed(this.host.NameTable.GetNameFor("GetEnumerator"), false)) {
        IMethodReference mref = memref as IMethodReference;
        if (mref != null) { nongenericGetEnumeratorOriginal = mref; break; }
      }
      MethodImplementation nonGenericGetEnumeratorImp = new MethodImplementation() {
        ContainingType = iteratorClosure.ClosureDefinition,
        ImplementedMethod = nongenericGetEnumeratorOriginal,
        ImplementingMethod = nongenericGetEnumerator
      };
      iteratorClosure.ClosureDefinition.ExplicitImplementationOverrides.Add(nonGenericGetEnumeratorImp);
      // Body: call this.GetEnumerator (the generic version).
      BlockStatement block1 = new BlockStatement();
      block1.Statements.Add(new ReturnStatement() {
        Expression = new MethodCall() {
           IsStaticCall = false,
           MethodToCall = iteratorClosure.GenericGetEnumeratorReference,
           ThisArgument = new ThisReference(),
           Type = iteratorClosure.NonGenericIEnumeratorInterface
         }
      });
      SourceMethodBody body1 = new SourceMethodBody(this.host, this.sourceLocationProvider, this.contractProvider);
      body1.IsNormalized = true;
      body1.LocalsAreZeroed = true;
      body1.Block = block1;
      body1.MethodDefinition = nongenericGetEnumerator;
      nongenericGetEnumerator.Body = body1;
    }

    /// <summary>
    /// Create two properties: object Current and T Current as the closure class implements both the 
    /// generic and non-generic version of ienumerator. 
    /// 
    /// Current Implementation generates getters, but not the property.
    /// </summary>
    /// <param name="iteratorClosure">Information about the closure created when compiling the current iterator method</param>
    private void CreateIteratorClosureProperties(IteratorClosureInformation iteratorClosure) {
      // Non-generic version of the get_Current, which returns the generic version of get_Current. 
      MethodDefinition getterNonGenericCurrent = new MethodDefinition() {
        Name = this.host.NameTable.GetNameFor("System.Collections.IEnumerator.get_Current")
      };
      CustomAttribute debuggerHiddenAttribute = new CustomAttribute();
      debuggerHiddenAttribute.Constructor = this.DebuggerHiddenCtor;
      getterNonGenericCurrent.Attributes.Add(debuggerHiddenAttribute);
      getterNonGenericCurrent.CallingConvention |= CallingConvention.HasThis;
      getterNonGenericCurrent.Visibility |= TypeMemberVisibility.Public;
      getterNonGenericCurrent.ContainingTypeDefinition = iteratorClosure.ClosureDefinition;
      getterNonGenericCurrent.Type = this.host.PlatformType.SystemObject;
      getterNonGenericCurrent.IsSpecialName = true;
      getterNonGenericCurrent.IsVirtual = true;
      getterNonGenericCurrent.IsNewSlot = true;
      getterNonGenericCurrent.IsHiddenBySignature = true;
      getterNonGenericCurrent.IsSealed = true;
      iteratorClosure.NonGenericGetCurrent = getterNonGenericCurrent;
      IMethodReference originalMethod = Dummy.MethodReference;
      foreach (ITypeMemberReference tref in iteratorClosure.NonGenericIEnumeratorInterface.ResolvedType.GetMembersNamed(this.host.NameTable.GetNameFor("get_Current"), false)) {
        originalMethod = tref as IMethodReference; if (originalMethod != null) break;
      }
      // assert originalMethod != Dummy
      MethodImplementation getterImplementation = new MethodImplementation() {
        ContainingType = iteratorClosure.ClosureDefinition,
        ImplementingMethod = getterNonGenericCurrent,
        ImplementedMethod = originalMethod
      };
      iteratorClosure.ClosureDefinition.ExplicitImplementationOverrides.Add(getterImplementation);

      List<IStatement> statements = new List<IStatement>();
      IFieldReference currentField = iteratorClosure.CurrentFieldReference;
      BoundExpression thisDotCurr = new BoundExpression() {
        Definition = currentField,
        Instance = new ThisReference(),
        Locations = iteratorClosure.ClosureDefinition.Locations,
        Type = currentField.Type
      };
      IExpression returnExpression;
      if (TypeHelper.TypesAreAssignmentCompatible(iteratorClosure.ElementType.ResolvedType, this.host.PlatformType.SystemObject.ResolvedType)) {
        returnExpression = thisDotCurr;
      } else {
        Conversion convertion = new Conversion() {
          CheckNumericRange = false,
          Type = this.host.PlatformType.SystemObject,
          TypeAfterConversion = getterNonGenericCurrent.Type,
          ValueToConvert = thisDotCurr
        };
        returnExpression = convertion;
      }
      ReturnStatement returnCurrent = new ReturnStatement() {
        Expression = returnExpression,
        Locations = iteratorClosure.ClosureDefinition.Locations
      };
      statements.Add(returnCurrent);
      BlockStatement block = new BlockStatement() { Statements = statements };
      SourceMethodBody body = new SourceMethodBody(this.host, this.sourceLocationProvider, this.contractProvider);
      body.IsNormalized = true;
      body.LocalsAreZeroed = true;
      body.Block = block;
      body.MethodDefinition = getterNonGenericCurrent;
      getterNonGenericCurrent.Body = body;

      // Create generic version of get_Current, the body of which is simply returning this.current.
      MethodDefinition getterGenericCurrent = new MethodDefinition() {
        Name = this.host.NameTable.GetNameFor("System.Collections.Generic.IEnumerator<" + iteratorClosure.ElementType.ToString() +">.get_Current")
      };
      getterGenericCurrent.Attributes.Add(debuggerHiddenAttribute);

      getterGenericCurrent.CallingConvention |= CallingConvention.HasThis;
      getterGenericCurrent.Visibility |= TypeMemberVisibility.Public;
      getterGenericCurrent.ContainingTypeDefinition = iteratorClosure.ClosureDefinition;
      getterGenericCurrent.Type = iteratorClosure.ElementType;
      getterGenericCurrent.IsSpecialName = true;
      getterGenericCurrent.IsVirtual = true;
      getterGenericCurrent.IsNewSlot = true;
      getterGenericCurrent.IsHiddenBySignature = true;
      getterGenericCurrent.IsSealed = true;
      iteratorClosure.GenericGetCurrent = getterGenericCurrent;
      originalMethod = Dummy.MethodReference;
      foreach (ITypeMemberReference tref in iteratorClosure.GenericIEnumeratorInterface.ResolvedType.GetMembersNamed(this.host.NameTable.GetNameFor("get_Current"), false)) {
        originalMethod = tref as IMethodReference; if (originalMethod != null) break;
      }
      MethodImplementation getterImplementation2 = new MethodImplementation() {
        ContainingType = iteratorClosure.ClosureDefinition,
        ImplementingMethod = getterGenericCurrent,
        ImplementedMethod = originalMethod
      };
      iteratorClosure.ClosureDefinition.ExplicitImplementationOverrides.Add(getterImplementation2);

      statements = new List<IStatement>();
      currentField = iteratorClosure.CurrentFieldReference;
      BoundExpression thisDotCurrent = new BoundExpression() {
        Definition = currentField, Instance = new ThisReference(), Locations = iteratorClosure.ClosureDefinition.Locations, Type = currentField.Type
      };
      returnCurrent = new ReturnStatement() {
        Expression = thisDotCurrent,
        Locations = iteratorClosure.ClosureDefinition.Locations
      };
      statements.Add(returnCurrent);
      block = new BlockStatement() { Statements = statements };
      body = new SourceMethodBody(this.host, this.sourceLocationProvider, this.contractProvider);
      body.LocalsAreZeroed = true;
      body.Block = block;
      body.MethodDefinition = getterGenericCurrent;
      getterGenericCurrent.Body = body;
    }

    /// <summary>
    /// Create fields for the closure class, which include fields for captured variables and fields for maintaining the state machine.
    /// </summary>
    private void CreateIteratorClosureFields(IteratorClosureInformation iteratorClosure) 
      //^ requires (iteratorClosure.ElementType != null);
    {
      // Create fields of the closure class: parameters and this
      if (!this.method.IsStatic) {
        FieldDefinition field = new FieldDefinition();
        // TODO: naming convention should use csc's.
        field.Name = this.host.NameTable.GetNameFor("<>__" + "this");
        //ITypeReference typeRef;
        //if (TypeHelper.TryGetFullyInstantiatedSpecializedTypeReference(method.ContainingTypeDefinition, out typeRef))
        //  field.Type = typeRef;
        //else
        //  field.Type = method.ContainingTypeDefinition;
        field.Type = TypeDefinition.SelfInstance(method.ContainingTypeDefinition, this.host.InternFactory);
        field.Visibility = TypeMemberVisibility.Public;
        field.ContainingTypeDefinition = iteratorClosure.ClosureDefinition;
        iteratorClosure.ThisField = field;
        BoundField boundField = new BoundField(field, iteratorClosure.ThisFieldReference.Type);
        this.FieldForCapturedLocalOrParameter.Add(new ThisReference(), boundField);
      }
      foreach (IParameterDefinition parameter in this.method.Parameters) {
        FieldDefinition field = new FieldDefinition();
        field.Name = parameter.Name;
        field.Type = this.copyTypeToClosure.Visit(parameter.Type);
        field.ContainingTypeDefinition = iteratorClosure.ClosureDefinition;
        field.Visibility = TypeMemberVisibility.Public;
        iteratorClosure.AddField(field);
        BoundField boundField = new BoundField(field, field.Type);
        this.FieldForCapturedLocalOrParameter.Add(parameter, boundField);
      }
      // Create fields of the closure class: Locals
      foreach (ILocalDefinition local in this.allLocals) {
        FieldDefinition field = new FieldDefinition();
        field.Name = this.host.NameTable.GetNameFor("<>__" + local.Name.Value + this.privateHelperTypes.Count);
        field.Type = this.copyTypeToClosure.Visit(local.Type);
        field.Visibility = TypeMemberVisibility.Public;
        field.ContainingTypeDefinition = iteratorClosure.ClosureDefinition;
        iteratorClosure.AddField(field);
        BoundField boundField = new BoundField(field, field.Type);
        this.FieldForCapturedLocalOrParameter.Add(local, boundField);
      }
      // Create fields: current, state, and l_initialThreadId
      FieldDefinition current = new FieldDefinition();
      current.Name = this.host.NameTable.GetNameFor("<>__" + "current");
      current.Type = iteratorClosure.ElementType;
      current.Visibility = TypeMemberVisibility.Private;
      current.ContainingTypeDefinition = iteratorClosure.ClosureDefinition;
      iteratorClosure.CurrentField = current;

      FieldDefinition state = new FieldDefinition();
      state.Name = this.host.NameTable.GetNameFor("<>__" + "state");
      state.Type = this.host.PlatformType.SystemInt32;
      state.Visibility = TypeMemberVisibility.Private;
      state.ContainingTypeDefinition = iteratorClosure.ClosureDefinition;
      iteratorClosure.StateField = state;

      FieldDefinition initialThreadId = new FieldDefinition();
      initialThreadId.Name = this.host.NameTable.GetNameFor("<>__" + "l_initialThreadId");
      initialThreadId.Type = this.host.PlatformType.SystemInt32;
      initialThreadId.Visibility = TypeMemberVisibility.Private;
      initialThreadId.ContainingTypeDefinition = iteratorClosure.ClosureDefinition;
      iteratorClosure.InitialThreadId = initialThreadId;
    }

    /// <summary>
    /// Find the type argument of the IEnumerable generic type implemented by a <paramref name="methodTypeReference"/>, or 
    /// System.Object if <paramref name="methodTypeReference"/> implements the non-generic IEnumerable.
    /// </summary>
    /// <param name="methodTypeReference">A type that must implement IEnumerable, or IEnumerable[T]. </param>
    /// <returns>An enumeration of ITypeReference of length 1. </returns>
    private IEnumerable<ITypeReference> GetClosureEnumeratorTypeArguments(ITypeReference /*!*/ methodTypeReference) {
      IGenericTypeInstanceReference genericTypeInstance = methodTypeReference as IGenericTypeInstanceReference;
      if (genericTypeInstance != null && TypeHelper.TypesAreEquivalent(genericTypeInstance.GenericType, methodTypeReference.PlatformType.SystemCollectionsGenericIEnumerable)) {
        return genericTypeInstance.GenericArguments;
      }
      var result = new List<ITypeReference>();
      result.Add(this.host.PlatformType.SystemObject);
      return result;
    }

    /// <summary>
    /// Collect locals declared in the body. 
    /// </summary>
    public override void Visit(ILocalDeclarationStatement localDeclarationStatement) {
      //localDeclarationStatement.LocalVariable = this.Visit(this.GetMutableCopy(localDeclarationStatement.LocalVariable));
      if (!this.allLocals.Contains(localDeclarationStatement.LocalVariable))
        this.allLocals.Add(localDeclarationStatement.LocalVariable);
      base.Visit(localDeclarationStatement);
    }

    /// <summary>
    /// Collect locals in TargetExpression
    /// </summary>
    public override void Visit(ITargetExpression targetExpression) {
      ILocalDefinition localDefinition = targetExpression.Definition as ILocalDefinition;
      if (localDefinition != null) {
        if (!this.allLocals.Contains(localDefinition)) {
          this.allLocals.Add(localDefinition);
        }
      }
      base.Visit(targetExpression);
    }

    public override void Visit(IBoundExpression boundExpression) {
      ILocalDefinition localDefinition = boundExpression.Definition as ILocalDefinition;
      if (localDefinition != null) {
        if (!this.allLocals.Contains(localDefinition)) {
          this.allLocals.Add(localDefinition);
        }
      }
      base.Visit(boundExpression);
    }

    public override void Visit(IAddressableExpression addressableExpression) {
      ILocalDefinition localDefinition = addressableExpression.Definition as ILocalDefinition;
      if (localDefinition != null) {
        if (!this.allLocals.Contains(localDefinition)) {
          this.allLocals.Add(localDefinition);
        }
      }
      base.Visit(addressableExpression);
    }
  }

  internal class YieldReturnYieldBreakReplacer: MethodBodyCodeMutator {
  /// <summary>
  /// Used in the tranformation of an iterator method body into a MoveNext method body, this class replaces
  /// yield returns and yield breaks with approppriate assignments to this dot current and return statements. 
  /// In addition, it inserts a new label statement after each yield return, and associates a unique state 
  /// number with the label. Such a mapping can be aquired from calling the GetStateEntries method. It is not
  /// suggested to call the Visit methods directly. 
  /// </summary>
    IteratorClosureInformation iteratorClosure;
    internal YieldReturnYieldBreakReplacer(IteratorClosureInformation iteratorClosure, IMetadataHost host) :
      base(host, true) {
      this.iteratorClosure = iteratorClosure;
    }
    /// <summary>
    /// State generator
    /// </summary>
    private int stateNumber;
    private int FreshState {
      get {
        return stateNumber++;
      }
    }
    /// <summary>
    /// Mapping between state machine state and its target label.
    /// </summary>
    Dictionary<int, ILabeledStatement> stateEntries;

    /// <summary>
    /// Compute the mapping between every (starting and continuing) state and their unique entry points. It does so
    /// by inserting a unique label at the entry points and associate the state with the label. 
    /// </summary>
    internal Dictionary<int, ILabeledStatement> GetStateEntries(BlockStatement body) {
      BlockStatement blockStatement = body;
      stateEntries = new Dictionary<int, ILabeledStatement>();
      LabeledStatement initialLabel = new LabeledStatement() {
        Label = this.host.NameTable.GetNameFor("Label"+ FreshState), Statement = new EmptyStatement()
      };
      // O(n), but happen only once. 
      blockStatement.Statements.Insert(0, initialLabel);
      stateEntries.Add(0, initialLabel);
      this.stateNumber = 1;
      base.Visit(blockStatement);
      this.stateNumber = 0; 
      Dictionary<int, ILabeledStatement> result = stateEntries;
      stateEntries = null;
      return result;
    }

    /// <summary>
    /// Replace a (yield return exp)with a new block of the form:
    /// {
    ///   Fresh_Label:;
    ///   this.current = exp;
    ///   state = Fresh_state;
    ///   return true;
    /// }
    /// and associate the newly generated Fresh_state with its entry point: Fresh_label.
    /// </summary>
    public override IStatement Visit(YieldReturnStatement yieldReturnStatement) {
      BlockStatement blockStatement = new BlockStatement();
      int state = FreshState;
      ExpressionStatement thisDotStateEqState = new ExpressionStatement() {
        Expression = new Assignment() {
          Source = new CompileTimeConstant() { Value = state, Type = this.host.PlatformType.SystemInt32 },
          Target = new TargetExpression() { Definition = this.iteratorClosure.StateFieldReference, Instance = new ThisReference(), Type = this.host.PlatformType.SystemInt32 },
          Type = this.host.PlatformType.SystemInt32,
        },
        Locations = yieldReturnStatement.Locations
      };
      blockStatement.Statements.Add(thisDotStateEqState);
      ExpressionStatement thisDotCurrentEqReturnExp = new ExpressionStatement() {
        Expression = new Assignment() {
          Source = yieldReturnStatement.Expression,
          Target = new TargetExpression() { Definition = this.iteratorClosure.CurrentFieldReference, Instance = new ThisReference(), Type = this.iteratorClosure.CurrentFieldReference.Type },
          Type = this.iteratorClosure.CurrentFieldReference.Type
        }
      };
      blockStatement.Statements.Add(thisDotCurrentEqReturnExp);
      ReturnStatement returnTrue = new ReturnStatement() {
        Expression = new CompileTimeConstant() {
          Value = true, Type = this.host.PlatformType.SystemBoolean
        }
      };
      blockStatement.Statements.Add(returnTrue);
      LabeledStatement labeledStatement = new LabeledStatement() {
        Label = this.host.NameTable.GetNameFor("Label"+state), Statement = new EmptyStatement()
      };
      blockStatement.Statements.Add(labeledStatement);
      this.stateEntries.Add(state, labeledStatement);
      return blockStatement;
    }

    /// <summary>
    /// Replace a yield break with:
    /// {
    ///   this.state = -2;
    ///   return;
    /// }
    /// </summary>
    /// <param name="yieldBreakStatement"></param>
    /// <returns></returns>
    public override IStatement Visit(YieldBreakStatement yieldBreakStatement) {
      BlockStatement blockStatement = new BlockStatement();
      ExpressionStatement thisDotStateEqMinus2 = new ExpressionStatement() {
        Expression = new Assignment() {
          Source = new CompileTimeConstant() { Value = -2, Type = this.host.PlatformType.SystemInt32 },
          Target = new TargetExpression() { Definition = iteratorClosure.StateFieldReference, Type = this.host.PlatformType.SystemInt32, Instance = new ThisReference() }
        }
      };
      blockStatement.Statements.Add(thisDotStateEqMinus2);
      ReturnStatement returnFalse = new ReturnStatement() {
        Expression = new CompileTimeConstant() {
          Value = false,
          Type = this.host.PlatformType.SystemBoolean
        }
      };
      blockStatement.Statements.Add(returnFalse);
      return blockStatement;
    }
  }
}
