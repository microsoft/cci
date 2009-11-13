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
  /// This class takes a code model and rewrites it so that high level constructs such as anonymous delegates and yield statements
  /// are turned into helper classes and methods, thus making it easier to generate IL from the CodeModel.
  /// </summary>
  public class CodeModelNormalizer : CodeAndContractMutator {

    /// <summary>
    /// Initializes a visitor that takes a code model and rewrites it so that high level constructs such as anonymous delegates and yield statements
    /// are turned into helper classes and methods, thus making it easier to generate IL from the CodeModel. This constructor is used when
    /// the code model was obtained by reading in a compiled unit of metadata via something like the PE reader. 
    /// </summary>
    /// <param name="host">An object representing the application that is hosting the converter. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.</param>
    /// <param name="sourceToILProvider">A delegate that returns an ISourceToILConverter object initialized with the given host, source location provider and contract provider.
    /// The returned object is in turn used to convert blocks of statements into lists of IL operations.</param>
    /// <param name="sourceLocationProvider">An object that can map the ILocation objects found in a block of statements to IPrimarySourceLocation objects. May be null.</param>
    /// <param name="contractProvider">An object that associates contracts, such as preconditions and postconditions, with methods, types and loops.
    /// IL to check this contracts will be generated along with IL to evaluate the block of statements. May be null.</param>
    public CodeModelNormalizer(IMetadataHost host, SourceToILConverterProvider sourceToILProvider,
      ISourceLocationProvider/*?*/ sourceLocationProvider, ContractProvider/*?*/ contractProvider)
      : base(host, sourceToILProvider, sourceLocationProvider, contractProvider) {
    }

    /// <summary>
    /// Initializes a visitor that takes a method body and rewrites it so that high level constructs such as anonymous delegates and yield statements
    /// are turned into helper classes and methods, thus making it easier to generate IL from the CodeModel.
    /// </summary>
    /// <param name="host">An object representing the application that is hosting the converter. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.</param>
    /// <param name="copyOnlyIfNotAlreadyMutable"></param>
    /// <param name="sourceToILProvider">A delegate that returns an ISourceToILConverter object initialized with the given host, source location provider and contract provider.
    /// The returned object is in turn used to convert blocks of statements into lists of IL operations.</param>
    /// <param name="sourceLocationProvider">An object that can map the ILocation objects found in a block of statements to IPrimarySourceLocation objects. May be null.</param>
    /// <param name="contractProvider">An object that associates contracts, such as preconditions and postconditions, with methods, types and loops.
    /// IL to check this contracts will be generated along with IL to evaluate the block of statements. May be null.</param>
    public CodeModelNormalizer(IMetadataHost host, bool copyOnlyIfNotAlreadyMutable, SourceToILConverterProvider sourceToILProvider,
      ISourceLocationProvider/*?*/ sourceLocationProvider, ContractProvider/*?*/ contractProvider)
      : base(host, copyOnlyIfNotAlreadyMutable, sourceToILProvider, sourceLocationProvider, contractProvider) {
    }

    /// <summary>
    /// Initializes a visitor that takes a method body and rewrites it so that high level constructs such as anonymous delegates and yield statements
    /// are turned into helper classes and methods, thus making it easier to generate IL from the CodeModel.
    /// </summary>
    /// <param name="mutator"></param>
    public CodeModelNormalizer(CodeAndContractMutator mutator)
      : base(mutator) {
    }

    Dictionary<IParameterDefinition, bool> capturedParameters = new Dictionary<IParameterDefinition, bool>();
    Dictionary<IMethodDefinition, bool> isAlreadyNormalized = new Dictionary<IMethodDefinition, bool>();
    Dictionary<object, BoundField> fieldForCapturedLocalOrParameter = new Dictionary<object, BoundField>();
    NestedTypeDefinition currentClosureClass = new NestedTypeDefinition();

    //List<ILocalDefinition> allLocals = new List<ILocalDefinition>();
    IMethodDefinition method= Dummy.Method;
    List<ILocalDefinition> closureLocals = new List<ILocalDefinition>();
    List<IFieldDefinition> outerClosures = new List<IFieldDefinition>();
    List<ITypeDefinition> privateHelperTypes = new List<ITypeDefinition>();

    private Dictionary<IParameterDefinition, bool> CapturedParameters {
      get { return capturedParameters; }
      set { capturedParameters = value; }
    }

    private Dictionary<IMethodDefinition, bool> IsAlreadyNormalized {
      get { return isAlreadyNormalized; }
    }

    private Dictionary<object, BoundField> FieldForCapturedLocalOrParameter {
      get { return fieldForCapturedLocalOrParameter; }
      set { fieldForCapturedLocalOrParameter = value; }
    }

    private NestedTypeDefinition CurrentClosureClass {
      get { return currentClosureClass; }
      set { currentClosureClass = value; }
    }

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

      ClosureFinder finder = new ClosureFinder(FieldForCapturedLocalOrParameter, this.host.NameTable, this.contractProvider);
      finder.Visit(body);
      IMethodContract/*?*/ methodContract = null;
      if (this.contractProvider != null)
        methodContract = this.contractProvider.GetMethodContractFor(method);
      if (methodContract != null)
        finder.Visit(methodContract);
      if (finder.foundAnonymousDelegate && this.FieldForCapturedLocalOrParameter.Count ==0) {
        body = this.CreateAndInitializeClosureTemp(body);
      }
      body = this.Visit(body, method, methodContract);
      if (finder.foundYield) {
        this.fieldForCapturedLocalOrParameter = new Dictionary<object, BoundField>();
        body = GetNormalizedIteratorBody(body, method, methodContract, privateHelperTypes);
      }
      SourceMethodBody result = new SourceMethodBody(this.sourceToILProvider, this.host, this.sourceLocationProvider, this.contractProvider);
      result.Block = body;
      result.MethodDefinition = method;
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
    /// GetEnumerator, Current getter, and Dispose. 
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

      IteratorClosureGenerator iteratorClosureGenerator = new IteratorClosureGenerator(this, this.FieldForCapturedLocalOrParameter, method, privateHelperTypes, this.host, this.sourceToILProvider, this.sourceLocationProvider, this.contractProvider);
      return iteratorClosureGenerator.CompileIterator(body, method, methodContract);
    }

    private MethodReference CompilerGeneratedCtor {
      get {
        if (this.compilerGeneratedCtor == null)
          this.compilerGeneratedCtor = new MethodReference(this.host, this.host.PlatformType.SystemRuntimeCompilerServicesCompilerGeneratedAttribute,
             CallingConvention.HasThis, this.host.PlatformType.SystemVoid, this.host.NameTable.Ctor, 0);
        return this.compilerGeneratedCtor;
      }
    }
    private MethodReference/*?*/ compilerGeneratedCtor;

    private MethodReference ObjectCtor {
      get {
        if (this.objectCtor == null)
          this.objectCtor = new MethodReference(this.host, this.host.PlatformType.SystemObject, CallingConvention.HasThis,
             this.host.PlatformType.SystemVoid, this.host.NameTable.Ctor, 0);
        return this.objectCtor;
      }
    }
    private MethodReference/*?*/ objectCtor;


    static ISourceToILConverter ProvideSourceToILConverter(IMetadataHost host, ISourceLocationProvider/*?*/ sourceLocationProvider, IContractProvider/*?*/ contractProvider) {
      return new PreNormalizedCodeModelToILConverter(host, sourceLocationProvider, contractProvider);
    }

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
        capturedThis.Field.ContainingType = result;
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
        correspondingField.ContainingType = closureClass;
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
      outerClosureField.ContainingType = closureClass;
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
      SourceMethodBody body = new SourceMethodBody(this.sourceToILProvider, this.host, this.sourceLocationProvider, this.contractProvider);
      body.LocalsAreZeroed = true;
      body.Block = block;

      MethodDefinition result = new MethodDefinition();
      closureClass.Methods.Add(result);
      this.cache.Add(result, result);
      this.IsAlreadyNormalized.Add(result, true);
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
      return Visit(mutableBlockStatement, method, methodContract);
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
      this.IsAlreadyNormalized.Add(globalMethodDefinition, true);
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
      if (this.IsAlreadyNormalized.ContainsKey(methodDefinition)) {
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
      method.ContainingType = this.CurrentClosureClass;
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
      localDeclarationStatement.LocalVariable = this.Visit(this.GetMutableCopy(localDeclarationStatement.LocalVariable));
      if (localDeclarationStatement.InitialValue != null) {
        var source = this.Visit(localDeclarationStatement.InitialValue);
        BoundField/*?*/ boundField;
        if (this.FieldForCapturedLocalOrParameter.TryGetValue(localDeclarationStatement.LocalVariable, out boundField)) {
          var currentClosureLocal = this.closureLocals[0];
          var currentClosureLocalBinding = new BoundExpression() { Definition = currentClosureLocal, Type = currentClosureLocal.Type };
          var target = new TargetExpression() { Instance = currentClosureLocalBinding, Definition = boundField.Field, Type = boundField.Type };
          var assignment = new Assignment() { Target = target, Source = source, Type = boundField.Type };
          return new ExpressionStatement() { Expression = assignment, Locations = localDeclarationStatement.Locations };
        } else {
          var target = new TargetExpression() { Definition = localDeclarationStatement.LocalVariable, Type = localDeclarationStatement.LocalVariable.Type };
          var assignment = new Assignment() { Target = target, Source = source, Type = target.Type };
          return new ExpressionStatement() { Expression = assignment, Locations = localDeclarationStatement.Locations };
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
        if (boundParameter != null && !this.CapturedParameters.ContainsKey(boundParameter)) {
          this.CapturedParameters.Add(boundParameter, true);
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
        this.host, this.sourceToILProvider, this.sourceLocationProvider);
      block = bodyFixer.Visit(block);
      var result = new SourceMethodBody(ProvideSourceToILConverter, this.host, this.sourceLocationProvider, this.contractProvider);
      result.Block = block;
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

  class IteratorClosureGenerator : MethodBodyCodeAndContractMutator {
    IMethodDefinition method = Dummy.Method;
    List<ITypeDefinition> privateHelperTypes;
    CodeModelNormalizer codeModelNormalizer;
    List<ILocalDefinition> allLocals;
    Dictionary<ITypeReference, ITypeReference> genericTypeParameterMapping = new Dictionary<ITypeReference, ITypeReference>();
    Dictionary<object, BoundField>/*passed in from constructor*/ fieldForCapturedLocalOrParameter;
    public IteratorClosureGenerator(CodeModelNormalizer codeModelNormalizer, Dictionary<object, BoundField> fieldForCapturedLocalOrParameter, IMethodDefinition method, List<ITypeDefinition> privateHelperTypes, IMetadataHost host, SourceToILConverterProvider sourceToILProvider,
      ISourceLocationProvider/*?*/ sourceLocationProvider, ContractProvider/*?*/ contractProvider)
      : base(host, sourceToILProvider, sourceLocationProvider, contractProvider) {
      this.privateHelperTypes = privateHelperTypes;
      this.method = method;
      this.codeModelNormalizer = codeModelNormalizer;
      this.fieldForCapturedLocalOrParameter = fieldForCapturedLocalOrParameter;
    }

    public IBlockStatement CompileIterator(IBlockStatement block, IMethodDefinition method, IMethodContract methodContract) {
      this.allLocals = new List<ILocalDefinition>();
      block = this.Visit(block);
      BlockStatement mutableBlockStatement = new BlockStatement(block);
      IteratorClosure iteratorClosure = this.CreateIteratorClosure(mutableBlockStatement);
      IBlockStatement result = this.CreateNewIteratorMethodBody(mutableBlockStatement, iteratorClosure);
      return result;
    }

    internal Dictionary<object, BoundField> FieldForCapturedLocalOrParameter {
      get { return fieldForCapturedLocalOrParameter; }
      set { fieldForCapturedLocalOrParameter = value; }
    }

    private MethodReference CompilerGeneratedCtor {
      get {
        if (this.compilerGeneratedCtor == null)
          this.compilerGeneratedCtor = new MethodReference(this.host, this.host.PlatformType.SystemRuntimeCompilerServicesCompilerGeneratedAttribute,
             CallingConvention.HasThis, this.host.PlatformType.SystemVoid, this.host.NameTable.Ctor, 0);
        return this.compilerGeneratedCtor;
      }
    }
    private MethodReference/*?*/ compilerGeneratedCtor;

    private MethodReference ObjectCtor {
      get {
        if (this.objectCtor == null)
          this.objectCtor = new MethodReference(this.host, this.host.PlatformType.SystemObject, CallingConvention.HasThis,
             this.host.PlatformType.SystemVoid, this.host.NameTable.Ctor, 0);
        return this.objectCtor;
      }
    }
    private MethodReference/*?*/ objectCtor;

    private MethodReference/*?*/ debuggerHiddenCtor;

    private MethodReference DebuggerHiddenCtor {
      get {
        IUnitNamespaceReference ns = this.host.PlatformType.SystemObject.ContainingUnitNamespace;
        ns = new NestedUnitNamespaceReference(ns, this.host.NameTable.GetNameFor("Diagnostics"));
        var debuggerHiddenClass = new NamespaceTypeReference(this.host, ns, this.host.NameTable.GetNameFor("DebuggerHiddenAttribute"), 0, false, false, PrimitiveTypeCode.Reference);
        if (this.debuggerHiddenCtor == null) {
          this.debuggerHiddenCtor = new MethodReference(this.host, debuggerHiddenClass, CallingConvention.HasThis, this.host.PlatformType.SystemVoid, this.host.NameTable.Ctor, 0);
        }
        return this.debuggerHiddenCtor;
      }
    }

    private IBlockStatement CreateNewIteratorMethodBody(BlockStatement block, IteratorClosure iteratorClosure) {
      BlockStatement result = new BlockStatement();
      LocalDefinition localDefinition = new LocalDefinition() {
        Name = this.host.NameTable.GetNameFor("iteratorClosureLocal"),
        Type = GetClosureTypeReferenceFromIterator(iteratorClosure),
        Locations = block.Locations
      };
      CreateObjectInstance createObjectInstance = new CreateObjectInstance() {
        MethodToCall = GetMethodReference(iteratorClosure, iteratorClosure.Constructor), Locations = block.Locations, Type = localDefinition.Type
      };
      createObjectInstance.Arguments.Add(new CompileTimeConstant() { Value = -2, Type = this.host.PlatformType.SystemInt32 });

      LocalDeclarationStatement localDeclarationStatement = new LocalDeclarationStatement() { InitialValue = createObjectInstance, LocalVariable = localDefinition, Locations = block.Locations };
      result.Statements.Add(localDeclarationStatement);
      foreach (object capturedLocalOrParameter in FieldForCapturedLocalOrParameter.Keys) {
        BoundField boundField = FieldForCapturedLocalOrParameter[capturedLocalOrParameter];
        Assignment assignment;
        ITypeReference localOrParameterType = GetLocalOrParameterType(capturedLocalOrParameter);
        if (capturedLocalOrParameter is IThisReference) {
          assignment = new Assignment {
            Source = new ThisReference(),
            Type = boundField.Type,
            Target = new TargetExpression() {
              Definition = GetFieldReference(iteratorClosure, boundField.Field),
              Type = boundField.Type,
              Instance = new BoundExpression() { Type = localDefinition.Type, Instance = null, Definition = localDefinition, Locations = block.Locations, IsVolatile = false }
            },
            Locations = block.Locations
          };
        } else {
          assignment = new Assignment {
            Source = new BoundExpression() { Definition = capturedLocalOrParameter, Instance = null, IsVolatile = false, Locations = block.Locations, Type = boundField.Type },
            Type = boundField.Type,
            Target = new TargetExpression() { Definition = GetFieldReference(iteratorClosure, boundField.Field), Type = localOrParameterType, Instance = new BoundExpression() { Type = localDefinition.Type, Instance = null, Definition = localDefinition, Locations = block.Locations, IsVolatile = false } },
            Locations = block.Locations
          };
        }
        ExpressionStatement expressionStatement = new ExpressionStatement() { Expression = assignment, Locations = block.Locations };
        result.Statements.Add(expressionStatement);
      }
      result.Statements.Add(new ReturnStatement() {
        Expression = new BoundExpression() { Definition = localDeclarationStatement.LocalVariable, Instance = null, Type = localDeclarationStatement.LocalVariable.Type }
      });
      return result;
    }

    /// <summary>
    /// return the type of the parameter (excluding this) or local variable represented by <paramref name="obj"/>
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    ITypeReference GetLocalOrParameterType(object obj) {
      IParameterDefinition parameterDefinition = obj as IParameterDefinition;
      if (parameterDefinition != null) {
        return parameterDefinition.Type;
      }
      ILocalDefinition localDefinition = obj as ILocalDefinition;
      if (localDefinition != null) return localDefinition.Type;
      return Dummy.Type;
    }

    bool IsTypeGeneric(ITypeDefinition typeDef) {
      if (typeDef.IsGeneric) return true;
      INestedTypeDefinition nestedType = typeDef as INestedTypeDefinition;
      if (nestedType != null) {
        if (IsTypeGeneric(nestedType.ContainingTypeDefinition)) return true;
      }
      return false;
    }

    ITypeReference GetFullyInstantiatedSpecializedTypeReference(ITypeDefinition typeDefinition) {
      if (typeDefinition.IsGeneric) return typeDefinition.InstanceType;
      INestedTypeDefinition nestedType = typeDefinition as INestedTypeDefinition;
      if (nestedType != null) {
        ITypeReference containingTypeReference = GetFullyInstantiatedSpecializedTypeReference(nestedType.ContainingTypeDefinition);
        foreach (var t in containingTypeReference.ResolvedType.NestedTypes) {
          if (t.Name == nestedType.Name && t.GenericParameterCount == nestedType.GenericParameterCount) {
            return t;
          }
        }
      }
      return typeDefinition;
    }

    ITypeReference GetClosureTypeReferenceFromIterator(IteratorClosure iteratorClosure) {
      ITypeReference closureReference = iteratorClosure.ClosureDefinitionReference;
      if (method.IsGeneric) {
        IGenericTypeInstanceReference genericTypeInstanceRef = closureReference as IGenericTypeInstanceReference;
        Debug.Assert(genericTypeInstanceRef != null);
        if (genericTypeInstanceRef != null) {
          List<ITypeReference> args = new List<ITypeReference>();
          foreach (var t in method.GenericParameters) args.Add(t);
          return GenericTypeInstance.GetGenericTypeInstance(genericTypeInstanceRef.GenericType.ResolvedType, args, this.host.InternFactory);
        }
      }
      return closureReference;
    }

    IFieldReference GetFieldReference(IteratorClosure iteratorClosure, /*unspecialized*/IFieldDefinition fieldDefinition) {
      ITypeReference typeReference = GetClosureTypeReferenceFromIterator(iteratorClosure);
      foreach (var f in typeReference.ResolvedType.Fields) {
        if (f.Name == fieldDefinition.Name) return f;
      }
      IFieldReference fieldReference = fieldDefinition;
      GenericTypeInstance genericInstance = typeReference as GenericTypeInstance;
      if (genericInstance != null) {
        fieldReference = (IFieldReference)genericInstance.SpecializeMember(fieldDefinition, this.host.InternFactory);
      } else {
        SpecializedNestedTypeDefinition specializedNestedType = typeReference as SpecializedNestedTypeDefinition;
        if (specializedNestedType != null)
          fieldReference = (IFieldReference)specializedNestedType.SpecializeMember(fieldDefinition, this.host.InternFactory);
      }
      return fieldReference;
    }

    IMethodReference GetMethodReference(IteratorClosure iteratorClosure, IMethodDefinition methodDefinition) {
      ITypeReference typeReference = GetClosureTypeReferenceFromIterator(iteratorClosure);
      foreach (var m in typeReference.ResolvedType.Methods) {
        if (m.Name == methodDefinition.Name) return m;
      }
      IMethodReference methodReference = methodDefinition;
      GenericTypeInstance genericInstance = typeReference as GenericTypeInstance;
      if (genericInstance != null) {
        methodReference = (IMethodReference)genericInstance.SpecializeMember(methodDefinition, this.host.InternFactory);
      } else {
        SpecializedNestedTypeDefinition specializedNestedType = typeReference as SpecializedNestedTypeDefinition;
        if (specializedNestedType != null)
          methodReference = (IMethodReference)specializedNestedType.SpecializeMember(methodDefinition, this.host.InternFactory);
      }
      return methodReference;
    }

    ITypeReference PlatformIDisposable {
      get {
        if (this.platformIDisposable == null) {
          this.platformIDisposable = new NamespaceTypeReference(this.host, this.host.PlatformType.SystemObject.ContainingUnitNamespace,
            this.host.NameTable.GetNameFor("IDisposable"), 0, false, false, PrimitiveTypeCode.Reference);
        }
        return this.platformIDisposable;
      }
    }
    ITypeReference platformIDisposable = null;

    private IteratorClosure CreateIteratorClosure(BlockStatement blockStatement) {
      IteratorClosure result = new IteratorClosure();
      CustomAttribute compilerGeneratedAttribute = new CustomAttribute();
      compilerGeneratedAttribute.Constructor = this.CompilerGeneratedCtor;

      NestedTypeDefinition iteratorClosureType = new NestedTypeDefinition();
      result.ClosureDefinition = iteratorClosureType;
      List<ITypeReference> typeParameters = new List<ITypeReference>();
      ushort count =0;
      foreach (var typeParameter in method.GenericParameters) {
        typeParameters.Add(typeParameter);
        GenericTypeParameter newTypeParam = new GenericTypeParameter() {
          Name = this.host.NameTable.GetNameFor(typeParameter.Name.Value + "_"),
          Index = (count++)
        };
        this.genericTypeParameterMapping[typeParameter] = newTypeParam;
        this.cache.Add(typeParameter, newTypeParam);
        this.cache.Add(newTypeParam, newTypeParam);
        newTypeParam.DefiningType = iteratorClosureType;
        iteratorClosureType.GenericParameters.Add(newTypeParam);
      }
      // Duplicate Constraints
      foreach (var typeParameter in typeParameters) {
        IGenericParameter originalTypeParameter = (IGenericParameter)typeParameter;

        GenericTypeParameter correspondingTypeParameter = (GenericTypeParameter)this.genericTypeParameterMapping[originalTypeParameter];
        if (originalTypeParameter.Constraints != null) {
          correspondingTypeParameter.Constraints = new List<ITypeReference>();
          foreach (ITypeReference t in originalTypeParameter.Constraints) {
            correspondingTypeParameter.Constraints.Add(this.Visit(t));
          }
        }
      }
      IEnumerable<ITypeReference> methodTypeArguments = GetClosureEnumeratorTypeArguments(method.Type);
      ITypeReference genericEnumeratorType = this.Visit(GenericTypeInstance.GetGenericTypeInstance(this.host.PlatformType.SystemCollectionsGenericIEnumerator, methodTypeArguments, this.host.InternFactory));
      ITypeReference genericEnumerableType = this.Visit(GenericTypeInstance.GetGenericTypeInstance(this.host.PlatformType.SystemCollectionsGenericIEnumerable, methodTypeArguments, this.host.InternFactory));
      ITypeReference nongenericEnumeratorType = this.host.PlatformType.SystemCollectionsIEnumerator;
      ITypeReference nongenericEnumerableType = this.host.PlatformType.SystemCollectionsIEnumerable;
      ITypeReference iDisposable = this.PlatformIDisposable;

      this.cache.Add(iteratorClosureType, iteratorClosureType);
      this.privateHelperTypes.Add(iteratorClosureType);
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

      ITypeReference elementType = null;
      foreach (ITypeReference t in methodTypeArguments) {
        elementType = t; break;
      }

      /* Interfaces. */
      result.ElementType = this.Visit(elementType);
      result.NonGenericIEnumerableInterface = nongenericEnumerableType;
      result.NonGenericIEnumeratorInterface = nongenericEnumeratorType;
      result.GenericIEnumerableInterface = genericEnumerableType;
      result.GenericIEnumeratorInterface = genericEnumeratorType;
      result.DisposableInterface = iDisposable;

      /* Fields, Methods, and Properties. */
      CreateIteratorClosureFields(result);
      CreateIteratorClosureConstructor(result);
      CreateIteratorClosureMethods(result, blockStatement);
      CreateIteratorClosureProperties(result);
      return result;
    }

    private void CreateIteratorClosureConstructor(IteratorClosure iteratorClosure) {
      MethodDefinition constructor = new MethodDefinition();
      this.cache.Add(constructor, constructor);
      ParameterDefinition stateParameter = new ParameterDefinition() {
        ContainingSignature = constructor,
        Index = 0,
        Name = this.host.NameTable.GetNameFor("state"),
        Type = this.host.PlatformType.SystemInt32
      };
      constructor.Parameters.Add(stateParameter);

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
            MethodToCall = ThreadDotManagedThreadId.Getter,
            ThisArgument = ThreadDotCurrentThread,
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
      SourceMethodBody body = new SourceMethodBody(this.sourceToILProvider, this.host, this.sourceLocationProvider, this.contractProvider);
      body.LocalsAreZeroed = true;
      body.Block = block;
      constructor.Body = body;
      body.MethodDefinition = constructor;
      constructor.CallingConvention = CallingConvention.HasThis;
      constructor.ContainingType = iteratorClosure.ClosureDefinition;
      constructor.IsCil = true;
      constructor.IsHiddenBySignature = true;
      constructor.IsRuntimeSpecial = true;
      constructor.IsSpecialName = true;
      constructor.Name = this.host.NameTable.Ctor;
      constructor.Type = this.host.PlatformType.SystemVoid;
      constructor.Visibility = TypeMemberVisibility.Public;
      iteratorClosure.Constructor = constructor;
    }

    private void CreateIteratorClosureMethods(IteratorClosure iteratorClosure, BlockStatement blockStatement) {

      // Dispose: Currently do nothing. A more serious implementation should probably dispose helper objects
      // since we dont have a specification at the moment, we will decide what to do after studying output
      // from the C# compiler. 
      CreateResetMethod(iteratorClosure);
      CreateDisposeMethod(iteratorClosure);
      CreateGetEnumeratorMethodGeneric(iteratorClosure);
      CreateGetEnumeratorMethodNonGeneric(iteratorClosure);
      CreateMoveNextMethod(iteratorClosure, blockStatement);
    }

    private void CreateMoveNextMethod(IteratorClosure /*!*/ iteratorClosure, BlockStatement blockStatement) {
      MethodDefinition moveNext = new MethodDefinition() {
        Name = this.host.NameTable.GetNameFor("MoveNext")
      };
      this.cache.Add(moveNext, moveNext);
      moveNext.ContainingType = iteratorClosure.ClosureDefinition;
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
      MethodImplementation moveNextImp = new MethodImplementation() {
        ContainingType = iteratorClosure.ClosureDefinition,
        ImplementingMethod = moveNext,
        ImplementedMethod = moveNextOriginal
      };
      iteratorClosure.ClosureDefinition.ExplicitImplementationOverrides.Add(moveNextImp);

      SourceMethodBody body = new SourceMethodBody(this.sourceToILProvider, this.host, this.sourceLocationProvider, this.contractProvider);
      IBlockStatement block = TranslateIteratorMethodBodyToMoveNextBody(iteratorClosure, blockStatement);
      moveNext.Body = body;
      body.Block = block;
      body.MethodDefinition = moveNext;
    }

    private IBlockStatement TranslateIteratorMethodBodyToMoveNextBody(IteratorClosure iteratorClosure, BlockStatement blockStatement) {

      FixIteratorBodyToUseClosure copier = new FixIteratorBodyToUseClosure(this.FieldForCapturedLocalOrParameter,
        this.cache, iteratorClosure, this.host, this.sourceToILProvider, this.sourceLocationProvider);
      IBlockStatement result = copier.Visit(blockStatement);
      Dictionary<int, ILabeledStatement> StateEntries = new YieldReturnYieldBreakReplacer(iteratorClosure, this.host).GetStateEntries(blockStatement);
      result = BuildStateMachine(iteratorClosure, (BlockStatement)result, StateEntries);
      return result;
    }

    BlockStatement BuildStateMachine(IteratorClosure iteratorClosure, BlockStatement oldBody, Dictionary<int, ILabeledStatement> stateEntries) {
      int max = 0; foreach (int i in stateEntries.Keys) { if (max < i) max = i; }
      if (max > 16) return oldBody;
      BlockStatement result = new BlockStatement();
      List<ISwitchCase> cases = new List<ISwitchCase>();
      foreach (int i in stateEntries.Keys) {
        SwitchCase c = new SwitchCase() {
          Expression = new CompileTimeConstant() { Type = this.host.PlatformType.SystemInt32, Value = i },
          Body = new List<IStatement>(),
        };
        c.Body.Add(new GotoStatement() { TargetStatement = stateEntries[i] });
        cases.Add(c);
      }
      SwitchCase defaultCase = new SwitchCase() {
      };
      defaultCase.Body.Add(new ReturnStatement() { Expression = new CompileTimeConstant() { Value = false, Type = this.host.PlatformType.SystemBoolean } });
      SwitchStatement switchStatement = new SwitchStatement() {
        Cases = cases,
        Expression = new BoundExpression() { Type = this.host.PlatformType.SystemInt32, Instance = new ThisReference(), Definition = iteratorClosure.StateFieldReference }
      };
      cases.Add(defaultCase);
      result.Statements.Add(switchStatement);
      result.Statements.Add(oldBody);
      result.Statements.Add(new ReturnStatement() { Expression = new CompileTimeConstant() { Value = false, Type = this.host.PlatformType.SystemBoolean } });
      return result;
    }

    private void CreateResetMethod(IteratorClosure/*!*/ iteratorClosure) {
      // System.Collections.IEnumerator.Reset: Simply throws an exception
      MethodDefinition reset = new MethodDefinition() {
        Name = this.host.NameTable.GetNameFor("Reset")
      };
      this.cache.Add(reset, reset);
      CustomAttribute debuggerHiddenAttribute = new CustomAttribute() { Constructor = this.DebuggerHiddenCtor };
      reset.Attributes.Add(debuggerHiddenAttribute);
      reset.CallingConvention |= CallingConvention.HasThis;
      reset.Visibility = TypeMemberVisibility.Private;
      reset.ContainingType = iteratorClosure.ClosureDefinition;
      reset.Type = this.host.PlatformType.SystemVoid;
      reset.IsVirtual = true; reset.IsNewSlot = true; reset.IsHiddenBySignature = true; reset.IsSealed = true;
      iteratorClosure.Reset = reset;
      IMethodReference resetImplemented = Dummy.MethodReference;
      foreach (var memref in iteratorClosure.NonGenericIEnumeratorInterface.ResolvedType.GetMembersNamed(this.host.NameTable.GetNameFor("Reset"), false)) {
        IMethodReference mref = memref as IMethodReference;
        if (mref != null) { resetImplemented = mref; break; }
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
      SourceMethodBody body = new SourceMethodBody(this.sourceToILProvider, this.host, this.sourceLocationProvider, this.contractProvider);
      body.LocalsAreZeroed = true;
      body.Block = block;
      body.MethodDefinition = reset;
      reset.Body = body;
    }

    private void CreateDisposeMethod(IteratorClosure/*!*/ iteratorClosure) {
      MethodDefinition disposeMethod = new MethodDefinition() {
        Name = this.host.NameTable.GetNameFor("Dispose")
      };
      this.cache.Add(disposeMethod, disposeMethod);
      disposeMethod.Attributes.Add(new CustomAttribute() { Constructor = this.debuggerHiddenCtor });
      disposeMethod.CallingConvention |= CallingConvention.HasThis;
      disposeMethod.Visibility = TypeMemberVisibility.Public;
      disposeMethod.ContainingType = iteratorClosure.ClosureDefinition;
      disposeMethod.Type = this.host.PlatformType.SystemVoid;
      disposeMethod.IsVirtual = true; disposeMethod.IsNewSlot = true; disposeMethod.IsHiddenBySignature = true; disposeMethod.IsSealed = true;
      iteratorClosure.Dispose = disposeMethod;
      IMethodReference disposeImplemented = Dummy.MethodReference;
      foreach (var memref in iteratorClosure.DisposableInterface.ResolvedType.GetMembersNamed(this.host.NameTable.GetNameFor("Dispose"), false)) {
        IMethodReference mref = memref as IMethodReference;
        if (mref != null) { disposeImplemented = mref; break; }
      }
      MethodImplementation disposeImp = new MethodImplementation() {
        ContainingType = iteratorClosure.ClosureDefinition,
        ImplementedMethod = disposeImplemented,
        ImplementingMethod = disposeMethod
      };
      iteratorClosure.ClosureDefinition.ExplicitImplementationOverrides.Add(disposeImp);
      BlockStatement block = new BlockStatement();
      block.Statements.Add(new ReturnStatement() {
        Expression = null,
        Locations = iteratorClosure.ClosureDefinition.Locations
      });
      SourceMethodBody body = new SourceMethodBody(this.sourceToILProvider, this.host, this.sourceLocationProvider, this.contractProvider);
      body.LocalsAreZeroed = true;
      body.Block = block;
      body.MethodDefinition = disposeMethod;
      disposeMethod.Body = body;
    }

    IPropertyDefinition/*?*/ ThreadDotManagedThreadId {
      get {
        AssemblyReference assemblyReference = new AssemblyReference(this.host, this.host.CoreAssemblySymbolicIdentity);
        IUnitNamespaceReference ns = new RootUnitNamespaceReference(assemblyReference);
        ns = new NestedUnitNamespaceReference(ns, this.host.NameTable.GetNameFor("System"));
        NestedUnitNamespaceReference SystemDotThreading = new NestedUnitNamespaceReference(ns, this.host.NameTable.GetNameFor("Threading"));
        ITypeReference ThreadingDotThread = new NamespaceTypeReference(this.host, SystemDotThreading, this.host.NameTable.GetNameFor("Thread"), 0, false, false, PrimitiveTypeCode.Reference);
        foreach (ITypeMemberReference memref in ThreadingDotThread.ResolvedType.GetMembersNamed(this.host.NameTable.GetNameFor("ManagedThreadId"), false)) {
          IPropertyDefinition propertyDef = memref as IPropertyDefinition;
          if (propertyDef != null) {
            return propertyDef;
          }
        }
        return Dummy.Property;
      }
    }

    MethodCall ThreadDotCurrentThread {
      get {
        AssemblyReference assemblyReference = new AssemblyReference(this.host, this.host.CoreAssemblySymbolicIdentity);
        IUnitNamespaceReference ns = new RootUnitNamespaceReference(assemblyReference);
        ns = new NestedUnitNamespaceReference(ns, this.host.NameTable.GetNameFor("System"));
        NestedUnitNamespaceReference SystemDotThreading = new NestedUnitNamespaceReference(ns, this.host.NameTable.GetNameFor("Threading"));
        ITypeReference ThreadingDotThread = new NamespaceTypeReference(this.host, SystemDotThreading, this.host.NameTable.GetNameFor("Thread"), 0, false, false, PrimitiveTypeCode.Reference);
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

    private void CreateGetEnumeratorMethodGeneric(IteratorClosure iteratorClosure) {
      // GetEnumerator generic version: it will be called by the non-generic version
      // pseudo code: if (this.state == -2 && this.threadID == Thread.CurrentThread.ManagedThreadId) {
      //                 this.state = 0; return this;
      //              }
      //              else { _d = new thisclosureclass(0); return _d; }
      MethodDefinition genericGetEnumerator = new MethodDefinition() {
        Name = this.host.NameTable.GetNameFor("System.Collections.Generic.IEnumerable<" + iteratorClosure.ElementType.ToString()+">.GetEnumerator")
      };
      this.cache.Add(genericGetEnumerator, genericGetEnumerator);
      CustomAttribute debuggerHiddenAttribute = new CustomAttribute() { Constructor = this.DebuggerHiddenCtor };
      genericGetEnumerator.Attributes.Add(debuggerHiddenAttribute);
      genericGetEnumerator.CallingConvention |= CallingConvention.HasThis;
      genericGetEnumerator.ContainingType = iteratorClosure.ClosureDefinition;
      genericGetEnumerator.Visibility = TypeMemberVisibility.Public;
      genericGetEnumerator.Type = iteratorClosure.GenericIEnumeratorInterface;
      genericGetEnumerator.IsVirtual = true;
      genericGetEnumerator.IsNewSlot = true;
      genericGetEnumerator.IsHiddenBySignature = true;
      genericGetEnumerator.IsSealed = true;
      iteratorClosure.GenericGetEnumerator = genericGetEnumerator;
      IMethodReference genericGetEnumeratorOriginal = Dummy.MethodReference;
      foreach (var memref in iteratorClosure.GenericIEnumerableInterface.ResolvedType.GetMembersNamed(this.host.NameTable.GetNameFor("GetEnumerator"), false)) {
        IMethodReference mref = memref as IMethodReference;
        if (mref != null) { genericGetEnumeratorOriginal = mref; break; }
      }
      MethodImplementation genericGetEnumeratorImp = new MethodImplementation() {
        ContainingType = iteratorClosure.ClosureDefinition,
        ImplementingMethod = genericGetEnumerator,
        ImplementedMethod = genericGetEnumeratorOriginal
      };
      iteratorClosure.ClosureDefinition.ExplicitImplementationOverrides.Add(genericGetEnumeratorImp);

      #region body of the genericgetenumerator
      BoundExpression thisDotState = new BoundExpression() {
        Definition = iteratorClosure.StateFieldReference,
        Instance = new ThisReference(),
        Type = this.host.PlatformType.SystemInt32
      };
      BoundExpression thisDotThreadId = new BoundExpression() {
        Definition = iteratorClosure.InitThreadIdFieldReference,
        Instance = new ThisReference(),
        Type = this.host.PlatformType.SystemInt32
      };
      MethodCall currentThreadId = new MethodCall() {
        MethodToCall = ThreadDotManagedThreadId.Getter,
        ThisArgument = ThreadDotCurrentThread,
        Type = this.host.PlatformType.SystemInt32
      };
      Equality stateEqMinus2 = new Equality() { LeftOperand = thisDotState, RightOperand = new CompileTimeConstant() { Type = this.host.PlatformType.SystemInt32, Value = -2 }, Type = this.host.PlatformType.SystemBoolean };
      Equality threadIdEqCurrentThreadId = new Equality { LeftOperand = thisDotThreadId, RightOperand = currentThreadId, Type = this.host.PlatformType.SystemBoolean };

      BlockStatement returnThis = new BlockStatement();
      ExpressionStatement thisDotStateEq0 = new ExpressionStatement() {
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
      returnThis.Statements.Add(thisDotStateEq0);
      returnThis.Statements.Add(new ReturnStatement() { Expression = new ThisReference() });

      BlockStatement returnNew = new BlockStatement();
      List<IExpression> args = new List<IExpression>(); args.Add(new CompileTimeConstant() { Value = 0, Type = this.host.PlatformType.SystemInt32 });
      LocalDeclarationStatement closureInstanceLocalDecl = new LocalDeclarationStatement() {
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

      ReturnStatement returnNewClosureInstance = new ReturnStatement() {
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
      #endregion

      BlockStatement block = new BlockStatement();
      block.Statements.Add(returnThisOrNew
        //new ReturnStatement() {
        //  Expression = new CompileTimeConstant() { Value= null, Type = genericGetEnumerator.Type}
        //}
        );
      SourceMethodBody body = new SourceMethodBody(this.sourceToILProvider, this.host, this.sourceLocationProvider, this.contractProvider);
      body.LocalsAreZeroed = true;
      body.Block = block;
      body.MethodDefinition = genericGetEnumerator;
      genericGetEnumerator.Body = body;
    }

    void CreateGetEnumeratorMethodNonGeneric(IteratorClosure iteratorClosure) {
      // GetEnumerator non-generic version
      MethodDefinition nongenericGetEnumerator = new MethodDefinition() {
        Name = this.host.NameTable.GetNameFor("System.Collections.IEnumerable.GetEnumerator")
      };
      this.cache.Add(nongenericGetEnumerator, nongenericGetEnumerator);
      nongenericGetEnumerator.Attributes.Add(new CustomAttribute() { Constructor = this.DebuggerHiddenCtor });
      nongenericGetEnumerator.CallingConvention |= CallingConvention.HasThis;
      nongenericGetEnumerator.ContainingType = iteratorClosure.ClosureDefinition;
      nongenericGetEnumerator.Visibility = TypeMemberVisibility.Public;
      nongenericGetEnumerator.Type = iteratorClosure.NonGenericIEnumeratorInterface;
      nongenericGetEnumerator.IsVirtual = true;
      nongenericGetEnumerator.IsNewSlot = true;
      nongenericGetEnumerator.IsHiddenBySignature = true;
      nongenericGetEnumerator.IsSealed = true;
      iteratorClosure.NonGenericGetEnumerator = nongenericGetEnumerator;
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

      BlockStatement block1 = new BlockStatement();
      block1.Statements.Add(new ReturnStatement() {
        Expression = /*new CompileTimeConstant() { Value = null, Type = nongenericGetEnumerator.Type }*/
         new MethodCall() {
           IsStaticCall = false,
           MethodToCall = iteratorClosure.GenericGetEnumeratorReference,
           ThisArgument = new ThisReference(),
           Type = iteratorClosure.NonGenericIEnumeratorInterface
         }
      });
      SourceMethodBody body1 = new SourceMethodBody(this.sourceToILProvider, this.host, this.sourceLocationProvider, this.contractProvider);
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
    private void CreateIteratorClosureProperties(IteratorClosure iteratorClosure) {
      // PropertyDefinition nongenericCurrent = new PropertyDefinition();
      MethodDefinition getterNonGenericCurrent = new MethodDefinition() {
        Name = this.host.NameTable.GetNameFor("System.Collections.IEnumerator.get_Current")
      };
      this.cache.Add(getterNonGenericCurrent, getterNonGenericCurrent);

      CustomAttribute debuggerHiddenAttribute = new CustomAttribute();
      debuggerHiddenAttribute.Constructor = this.DebuggerHiddenCtor;
      getterNonGenericCurrent.Attributes.Add(debuggerHiddenAttribute);

      getterNonGenericCurrent.CallingConvention |= CallingConvention.HasThis;
      getterNonGenericCurrent.Visibility |= TypeMemberVisibility.Public;
      getterNonGenericCurrent.ContainingType = iteratorClosure.ClosureDefinition;
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
      SourceMethodBody body = new SourceMethodBody(this.sourceToILProvider, this.host, this.sourceLocationProvider, this.contractProvider);
      body.LocalsAreZeroed = true;
      body.Block = block;
      body.MethodDefinition = getterNonGenericCurrent;
      getterNonGenericCurrent.Body = body;

      // Create generic version of get_Current
      MethodDefinition getterGenericCurrent = new MethodDefinition() {
        Name = this.host.NameTable.GetNameFor("System.Collections.Generic.IEnumerator<" + iteratorClosure.ElementType.ToString() +">.get_Current")
      };
      this.cache.Add(getterGenericCurrent, getterGenericCurrent);
      getterGenericCurrent.Attributes.Add(debuggerHiddenAttribute);

      getterGenericCurrent.CallingConvention |= CallingConvention.HasThis;
      getterGenericCurrent.Visibility |= TypeMemberVisibility.Public;
      getterGenericCurrent.ContainingType = iteratorClosure.ClosureDefinition;
      getterGenericCurrent.Type = iteratorClosure.ElementType;
      getterGenericCurrent.IsSpecialName = true;
      getterGenericCurrent.IsVirtual = true;
      getterGenericCurrent.IsNewSlot = true;
      getterGenericCurrent.IsHiddenBySignature = true;
      getterGenericCurrent.IsSealed = true;
      iteratorClosure.NonGenericGetCurrent = getterGenericCurrent;
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
      body = new SourceMethodBody(this.sourceToILProvider, this.host, this.sourceLocationProvider, this.contractProvider);
      body.LocalsAreZeroed = true;
      body.Block = block;
      body.MethodDefinition = getterGenericCurrent;
      getterGenericCurrent.Body = body;
    }

    IEnumerable<ILocalDefinition> AllLocals {
      get { return allLocals; }
    }

    private void CreateIteratorClosureFields(IteratorClosure iteratorClosure) {
      // Create fields of the closure class: parameters and this
      if (!method.IsStatic) {
        FieldDefinition field = new FieldDefinition();
        field.Name = this.host.NameTable.GetNameFor("<>__" + "this");
        field.Type = GetFullyInstantiatedSpecializedTypeReference(method.ContainingTypeDefinition);
        field.Visibility = TypeMemberVisibility.Public;
        field.ContainingType = iteratorClosure.ClosureDefinition;
        iteratorClosure.ThisField = field;
        BoundField boundField = new BoundField(field, iteratorClosure.ThisFieldReference.Type);
        this.FieldForCapturedLocalOrParameter.Add(new ThisReference(), boundField);
      }
      foreach (IParameterDefinition parameter in method.Parameters) {
        //this.CapturedParameters.Add(parameter, true);
        FieldDefinition field = new FieldDefinition();
        field.Name = parameter.Name;
        field.Type = this.Visit(parameter.Type);
        field.ContainingType = iteratorClosure.ClosureDefinition;
        field.Visibility = TypeMemberVisibility.Public;
        iteratorClosure.AddField(field);
        BoundField boundField = new BoundField(field, parameter.Type);
        this.FieldForCapturedLocalOrParameter.Add(parameter, boundField);
      }
      // Create fields of the closure class: Locals
      foreach (ILocalDefinition local in AllLocals) {
        FieldDefinition field = new FieldDefinition();
        field.Name = this.host.NameTable.GetNameFor("<>__" + local.Name.Value + this.privateHelperTypes.Count);
        field.Type = this.Visit(local.Type);
        field.Visibility = TypeMemberVisibility.Public;
        field.ContainingType = iteratorClosure.ClosureDefinition;
        iteratorClosure.AddField(field);
        BoundField boundField = new BoundField(field, field.Type);
        this.FieldForCapturedLocalOrParameter.Add(local, boundField);
      }
      // Create fields of the fields that manages
      FieldDefinition current = new FieldDefinition();
      current.Name = this.host.NameTable.GetNameFor("<>__" + "current");
      current.Type = iteratorClosure.ElementType;
      current.Visibility = TypeMemberVisibility.Private;
      current.ContainingType = iteratorClosure.ClosureDefinition;
      iteratorClosure.CurrentField = current;

      FieldDefinition state = new FieldDefinition();
      state.Name = this.host.NameTable.GetNameFor("<>__" + "state");
      state.Type = this.host.PlatformType.SystemInt32;
      state.Visibility = TypeMemberVisibility.Private;
      state.ContainingType = iteratorClosure.ClosureDefinition;
      iteratorClosure.StateField = state;

      FieldDefinition initialThreadId = new FieldDefinition();
      initialThreadId.Name = this.host.NameTable.GetNameFor("<>__" + "l_initialThreadId");
      initialThreadId.Type = this.host.PlatformType.SystemInt32;
      initialThreadId.Visibility = TypeMemberVisibility.Private;
      initialThreadId.ContainingType = iteratorClosure.ClosureDefinition;
      iteratorClosure.InitialThreadId = initialThreadId;
    }

    /// <summary>
    /// Find the type arguments of the IEnumerable generic type implemented by a <paramref name="methodTypeReference"/>.
    /// </summary>
    /// <param name="methodTypeReference">A type that must implement IEnumerable. </param>
    /// <returns>An enumeration of ITypeReference of length 1. </returns>
    private IEnumerable<ITypeReference> GetClosureEnumeratorTypeArguments(ITypeReference /*!*/ methodTypeReference) {
      IGenericTypeInstanceReference genericTypeInstance = methodTypeReference as IGenericTypeInstanceReference;
      if (genericTypeInstance != null && TypeHelper.TypesAreEquivalent(genericTypeInstance.GenericType, methodTypeReference.PlatformType.SystemCollectionsGenericIEnumerable)) {
        return genericTypeInstance.GenericArguments;
      }
      // should be unreachable
      return new List<ITypeReference>();
    }

    public override IStatement Visit(LocalDeclarationStatement localDeclarationStatement) {
      localDeclarationStatement.LocalVariable = this.Visit(this.GetMutableCopy(localDeclarationStatement.LocalVariable));
      if (!this.allLocals.Contains(localDeclarationStatement.LocalVariable))
        this.allLocals.Add(localDeclarationStatement.LocalVariable);
      return base.Visit(localDeclarationStatement);
    }

    public override ITargetExpression Visit(TargetExpression targetExpression) {
      ILocalDefinition localDefinition = targetExpression.Definition as ILocalDefinition;
      if (localDefinition != null) {
        if (!this.allLocals.Contains(localDefinition)) {
          this.allLocals.Add(localDefinition);
        }
      }
      return base.Visit(targetExpression);
    }

    public override ITypeReference Visit(ITypeReference typeReference) {
      if (this.genericTypeParameterMapping.ContainsKey(typeReference)) return this.genericTypeParameterMapping[typeReference];
      IGenericTypeInstanceReference genericTypeReference = typeReference as IGenericTypeInstanceReference;
      if (genericTypeReference != null)
        return this.Visit(genericTypeReference);
      return base.Visit(typeReference);
    }
  }

  /// <summary>
  /// This visitor takes a method body and rewrites it so that high level constructs such as anonymous delegates and yield statements
  /// are turned into helper classes and methods, thus making it easier to generate IL from the CodeModel.
  /// </summary>
  public class MethodBodyNormalizer : CodeModelNormalizer {
    /// <summary>
    /// Initializes a visitor that takes a method body and rewrites it so that high level constructs such as anonymous delegates and yield statements
    /// are turned into helper classes and methods, thus making it easier to generate IL from the CodeModel.
    /// </summary>
    /// <param name="host">An object representing the application that is hosting the converter. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.</param>
    /// <param name="sourceToILProvider">A delegate that returns an ISourceToILConverter object initialized with the given host, source location provider and contract provider.
    /// The returned object is in turn used to convert blocks of statements into lists of IL operations.</param>
    /// <param name="sourceLocationProvider">An object that can map the ILocation objects found in a block of statements to IPrimarySourceLocation objects. May be null.</param>
    /// <param name="contractProvider">An object that associates contracts, such as preconditions and postconditions, with methods, types and loops.
    /// IL to check this contracts will be generated along with IL to evaluate the block of statements. May be null.</param>
    public MethodBodyNormalizer(IMetadataHost host, SourceToILConverterProvider sourceToILProvider,
      ISourceLocationProvider/*?*/ sourceLocationProvider, ContractProvider/*?*/ contractProvider)
      : base(host, sourceToILProvider, sourceLocationProvider, contractProvider) {
    }

    /// <summary>
    /// Visits the specified field reference.
    /// </summary>
    /// <param name="fieldReference">The field reference.</param>
    public override IFieldReference Visit(IFieldReference fieldReference) {
      //Just return the reference as is. The base visitor will make a copy of the reference, which
      //is inappropriate here because only the body of a method is being mutated (and hence all
      //fields definitions will remain unchanged).
      return fieldReference;
    }

    /// <summary>
    /// Visits the specified method reference.
    /// </summary>
    /// <param name="methodReference">The method reference.</param>
    public override IMethodReference Visit(IMethodReference methodReference) {
      //Just return the reference as is. The base visitor will make a copy of the reference, which
      //is inappropriate here because only the body of a method is being mutated (and hence all
      //method definitions will remain unchanged).
      return methodReference;
    }

    /// <summary>
    /// Visits the specified type reference.
    /// </summary>
    /// <param name="typeReference">The type reference.</param>
    public override ITypeReference Visit(ITypeReference typeReference) {
      //Just return the reference as is. The base visitor will make a copy of the reference, which
      //is inappropriate here because only the body of a method is being mutated (and hence all
      //type definitions will remain unchanged).
      return typeReference;
    }


  }

  internal class YieldReturnYieldBreakReplacer : MethodBodyCodeMutator {
    IteratorClosure iteratorClosure;
    internal YieldReturnYieldBreakReplacer(IteratorClosure iteratorClosure, IMetadataHost host) :
      base(host) {
      this.iteratorClosure = iteratorClosure;
    }

    int count = 0;
    int Count {
      get {
        return count++;
      }
    }

    Dictionary<int, ILabeledStatement> stateEntries;
    internal Dictionary<int, ILabeledStatement> GetStateEntries(BlockStatement body) {
      BlockStatement blockStatement = body;
      stateEntries = new Dictionary<int, ILabeledStatement>();
      LabeledStatement initialLabel = new LabeledStatement() {
        Label = this.host.NameTable.GetNameFor("Label"+ Count), Statement = new EmptyStatement()
      };
      blockStatement.Statements.Insert(0, initialLabel);
      stateEntries.Add(0, initialLabel);
      base.Visit(blockStatement);
      Dictionary<int, ILabeledStatement> result = stateEntries;
      stateEntries = null;
      return result;
    }

    public override IStatement Visit(YieldReturnStatement yieldReturnStatement) {
      BlockStatement blockStatement = new BlockStatement();
      int state = Count;
      ExpressionStatement thisDotStateEqState = new ExpressionStatement() {
        Expression = new Assignment() {
          Source = new CompileTimeConstant() { Value = state, Type = this.host.PlatformType.SystemInt32 },
          Target = new TargetExpression() { Definition = this.iteratorClosure.StateFieldReference, Instance = new ThisReference(), Type = this.host.PlatformType.SystemInt32 },
          Type = this.host.PlatformType.SystemInt32
        },
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

    public override IStatement Visit(YieldBreakStatement yieldBreakStatement) {
      BlockStatement blockStatement = new BlockStatement();
      ExpressionStatement thisDotStateEqMinus1 = new ExpressionStatement() {
        Expression = new Assignment() {
          Source = new CompileTimeConstant() { Value = -1, Type = this.host.PlatformType.SystemInt32 },
          Target = new TargetExpression() { Definition = iteratorClosure.StateFieldReference, Type = this.host.PlatformType.SystemInt32, Instance = new ThisReference() }
        }
      };
      blockStatement.Statements.Add(thisDotStateEqMinus1);
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
