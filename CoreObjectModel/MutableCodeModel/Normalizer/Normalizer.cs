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
using Microsoft.Cci.MutableCodeModel;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Cci.Contracts;
using Microsoft.Cci.MutableContracts;

namespace Microsoft.Cci.MutableCodeModel {

  /// <summary>
  /// A class providing functionality to rewrite high level constructs such as anonymous delegates and yield statements
  /// into helper classes and methods, thus making it easier to generate IL from the CodeModel.
  /// </summary>
  public class MethodBodyNormalizer {

    /// <summary>
    /// A class providing functionality to rewrite high level constructs such as anonymous delegates and yield statements
    /// into helper classes and methods, thus making it easier to generate IL from the CodeModel.
    /// </summary>
    /// <param name="host">An object representing the application that is hosting the converter. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.</param>
    /// <param name="sourceLocationProvider">An object that can map the ILocation objects found in a block of statements to IPrimarySourceLocation objects. May be null.</param>
    public MethodBodyNormalizer(IMetadataHost host, ISourceLocationProvider/*?*/ sourceLocationProvider) {
      this.host = host;
      this.sourceLocationProvider = sourceLocationProvider;
    }

    /// <summary>
    /// An object representing the application that is hosting the converter. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.
    /// </summary>
    IMetadataHost host;

    /// <summary>
    /// An object that can map the ILocation objects found in a block of statements to IPrimarySourceLocation objects. May be null.
    /// </summary>
    ISourceLocationProvider/*?*/ sourceLocationProvider;

    /// <summary>
    /// Given a method definition and a block of statements that represents the Block property of the body of the method,
    /// returns a SourceMethod with a body that no longer has any yield statements or anonymous delegate expressions.
    /// The given block of statements is mutated in place.
    /// </summary>
    public SourceMethodBody GetNormalizedSourceMethodBodyFor(IMethodDefinition method, IBlockStatement body) {

      var finder = new ClosureFinder(method, this.host);
      finder.Traverse(body);

      var privateHelperTypes = new List<ITypeDefinition>();
      if (finder.foundYield) {
        this.isIteratorBody = true;
        body = this.GetNormalizedIteratorBody(body, method, privateHelperTypes);
      }

      SourceMethodBody result = new SourceMethodBody(this.host, this.sourceLocationProvider);
      result.Block = body;
      result.MethodDefinition = method;
      result.IsNormalized = true;
      result.LocalsAreZeroed = true;
      result.PrivateHelperTypes = privateHelperTypes;

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
    /// GetEnumerator, Current getter, and DisposeMethod. (GetEnumerator is needed only if the iterator's type is IEnumerable and not IEnumerator.)
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
    /// <param name="privateHelperTypes">List of helper types generated when compiling <paramref name="method">method</paramref>/></param>
    /// <returns></returns>
    private BlockStatement GetNormalizedIteratorBody(IBlockStatement body, IMethodDefinition method, List<ITypeDefinition> privateHelperTypes) {
      IteratorClosureGenerator iteratorClosureGenerator = new IteratorClosureGenerator(method, privateHelperTypes, this.host, this.sourceLocationProvider);
      return iteratorClosureGenerator.CompileIterator(body);
    }

    /// <summary>
    /// Returns true if the last call to GetNormalizedSourceMethodBodyFor visited a body that contains a yield statement.
    /// </summary>
    public bool IsIteratorBody {
      get { return this.isIteratorBody; }
    }
    bool isIteratorBody;

  }

  /// <summary>
  /// A traverser that checks for the presense of yield statements and anonymous delegates.
  /// </summary>
  internal class NormalizationChecker : CodeTraverser {

    /// <summary>
    /// The traversal encountered an anonymous delegate.
    /// </summary>
    internal bool foundAnonymousDelegate;

    /// <summary>
    /// The traversal encountered a yield statement.
    /// </summary>
    internal bool foundYield;

    /// <summary>
    /// The traversal encountered a foreach statement.
    /// </summary>
    internal bool foundForEach;

    public override void TraverseChildren(IAnonymousDelegate anonymousDelegate) {
      this.foundAnonymousDelegate = true;
      if (this.foundYield && this.foundForEach)
        this.StopTraversal = true;
      else
        base.TraverseChildren(anonymousDelegate);
    }

    public override void TraverseChildren(IForEachStatement forEachStatement) {
      this.foundForEach = true;
      if (this.foundAnonymousDelegate && this.foundYield)
        this.StopTraversal = true;
      else
        base.TraverseChildren(forEachStatement);
    }

    public override void TraverseChildren(IYieldBreakStatement yieldBreakStatement) {
      this.foundYield = true;
      if (this.foundAnonymousDelegate && this.foundForEach)
        this.StopTraversal = true;
      else
        base.TraverseChildren(yieldBreakStatement);
    }

    public override void TraverseChildren(IYieldReturnStatement yieldReturnStatement) {
      this.foundYield = true;
      if (this.foundAnonymousDelegate && this.foundForEach)
        this.StopTraversal = true;
      else
        base.TraverseChildren(yieldReturnStatement);
    }
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
  internal class IteratorClosureGenerator {

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
    internal IteratorClosureGenerator(IMethodDefinition method, List<ITypeDefinition> privateHelperTypes, IMetadataHost host,
      ISourceLocationProvider/*?*/ sourceLocationProvider) {
      this.privateHelperTypes = privateHelperTypes;
      this.method = method;
      this.fieldForCapturedLocalOrParameter = new Dictionary<object, BoundField>();
      this.iteratorLocalCount = new Dictionary<IBlockStatement, uint>();
      this.host = host; this.sourceLocationProvider = sourceLocationProvider;

      var methodType = method.Type;
      var genericMethodType = methodType as IGenericTypeInstanceReference;
      if (genericMethodType != null) methodType = genericMethodType.GenericType;
      this.isEnumerable =
        TypeHelper.TypesAreEquivalent(methodType, host.PlatformType.SystemCollectionsGenericIEnumerable)
        || TypeHelper.TypesAreEquivalent(methodType, host.PlatformType.SystemCollectionsIEnumerable);
    }

    IMetadataHost host;
    ISourceLocationProvider/*?*/ sourceLocationProvider;

    /// <summary>
    /// A map that indicates how many iterator locals are present in a given block. Only useful for generated MoveNext methods. May be null.
    /// </summary>>
    Dictionary<IBlockStatement, uint> iteratorLocalCount;

    /// <summary>
    /// Iterator method.
    /// </summary>
    private IMethodDefinition method = Dummy.MethodDefinition;

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
    /// Mapping between (the interned key of) method type parameters (of the iterator method) and generic type parameters (of the closure class).
    /// </summary>
    Dictionary<uint, IGenericTypeParameter> genericTypeParameterMapping = new Dictionary<uint, IGenericTypeParameter>();

    /// <summary>
    /// Mapping between parameters and locals to the fields of the closure class. 
    /// </summary>
    internal Dictionary<object, BoundField> FieldForCapturedLocalOrParameter {
      get { return fieldForCapturedLocalOrParameter; }
    }

    Dictionary<object, BoundField> fieldForCapturedLocalOrParameter;

    bool isEnumerable; // true if return type of method is IEnumerable, false if return type of method is IEnumerator

    /// <summary>
    /// Compile the method body, represented by <paramref name="block"/>. It creates the closure class and all its members
    /// and creates a new body for the iterator method. 
    /// </summary>
    internal BlockStatement CompileIterator(IBlockStatement block) {
      var copier = new CodeDeepCopier(this.host, this.sourceLocationProvider);
      var copiedBlock = copier.Copy(block);
      var localCollector = new LocalCollector();
      new CodeTraverser() { PreorderVisitor = localCollector }.Traverse(copiedBlock);
      this.allLocals = localCollector.allLocals;
      IteratorClosureInformation iteratorClosure = this.CreateIteratorClosure(copiedBlock);
      var result = this.CreateNewIteratorMethodBody(iteratorClosure);
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
        ns = new Immutable.NestedUnitNamespaceReference(ns, this.host.NameTable.GetNameFor("Diagnostics"));
        var debuggerHiddenClass = new Immutable.NamespaceTypeReference(this.host, ns, this.host.NameTable.GetNameFor("DebuggerHiddenAttribute"), 0, false, false, true, PrimitiveTypeCode.Reference);
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
    /// iteratorClosureLocal = new Closure(0);
    /// iteratorClosureLocal.field = parameter; // for each parameter including this. 
    /// return iteratorClosureLocal;
    /// </remarks>
    private BlockStatement CreateNewIteratorMethodBody(IteratorClosureInformation iteratorClosure) {
      BlockStatement result = new BlockStatement();
      // iteratorClosureLocal = new IteratorClosure(0);
      LocalDefinition localDefinition = new LocalDefinition() {
        Name = this.host.NameTable.GetNameFor("iteratorClosureLocal"),
        Type = GetClosureTypeReferenceFromIterator(iteratorClosure),
      };
      CreateObjectInstance createObjectInstance = new CreateObjectInstance() {
        MethodToCall = GetMethodReference(iteratorClosure, iteratorClosure.Constructor),
        Type = localDefinition.Type
      };
      // the start state depends on whether the iterator is an IEnumerable or an IEnumerator. For the former,
      // it must be created in state -2. Then it is the GetEnumerator method that puts it into its
      // "start" state, i.e., state 0.
      var startState = this.isEnumerable ? -2 : 0;
      createObjectInstance.Arguments.Add(new CompileTimeConstant() { Value = startState, Type = this.host.PlatformType.SystemInt32 });
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
          var thisR = new ThisReference();
          IExpression thisValue = thisR;
          if (!this.method.ContainingTypeDefinition.IsClass) {
            thisValue = new AddressDereference() {
              Address = thisR,
              Type = this.method.ContainingTypeDefinition.IsGeneric ? (ITypeReference)this.method.ContainingTypeDefinition.InstanceType : (ITypeReference)this.method.ContainingTypeDefinition
            };
          }
          assignment = new Assignment {
            Source = thisValue,
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
                IsVolatile = false
              }
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
      return Dummy.TypeReference;
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
          return new Immutable.GenericTypeInstanceReference(genericTypeInstanceRef.GenericType, args, this.host.InternFactory);
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
          InternFactory = this.host.InternFactory,
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
          Parameters = methodDefinition.ParameterCount == 0 ? null : new List<IParameterTypeInformation>(((IMethodReference)methodDefinition).Parameters),
          ExtraParameters = null,
          ReturnValueIsByRef = methodDefinition.ReturnValueIsByRef,
          ReturnValueIsModified = methodDefinition.ReturnValueIsModified,
          Attributes = null,
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
      NestedTypeDefinition iteratorClosureType = new NestedTypeDefinition() {
        ExplicitImplementationOverrides = new List<IMethodImplementation>(),
        Fields = new List<IFieldDefinition>(),
        GenericParameters = new List<IGenericTypeParameter>(),
        Interfaces = new List<ITypeReference>(),
        Methods = new List<IMethodDefinition>(),
        NestedTypes = new List<INestedTypeDefinition>(),
      };
      this.privateHelperTypes.Add(iteratorClosureType);
      result.ClosureDefinition = iteratorClosureType;
      List<IGenericMethodParameter> genericMethodParameters = new List<IGenericMethodParameter>();
      ushort count = 0;
      foreach (var genericMethodParameter in method.GenericParameters) {
        genericMethodParameters.Add(genericMethodParameter);
        GenericTypeParameter newTypeParam = new GenericTypeParameter() {
          Name = this.host.NameTable.GetNameFor(genericMethodParameter.Name.Value + "_"),
          InternFactory = this.host.InternFactory,
          PlatformType = this.host.PlatformType,
          Index = (count++),
        };
        this.genericTypeParameterMapping[genericMethodParameter.InternedKey] = newTypeParam;
        newTypeParam.DefiningType = iteratorClosureType;
        iteratorClosureType.GenericParameters.Add(newTypeParam);
      }

      this.copyTypeToClosure = new CopyTypeFromIteratorToClosure(this.host, this.genericTypeParameterMapping);

      // Duplicate Constraints
      foreach (var genericMethodParameter in genericMethodParameters) {
        GenericTypeParameter correspondingTypeParameter = (GenericTypeParameter)this.genericTypeParameterMapping[genericMethodParameter.InternedKey];
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
      iteratorClosureType.Attributes = new List<ICustomAttribute>(1) { compilerGeneratedAttribute };
      iteratorClosureType.BaseClasses = new List<ITypeReference>(1) { this.host.PlatformType.SystemObject };
      iteratorClosureType.ContainingTypeDefinition = this.method.ContainingTypeDefinition;
      iteratorClosureType.ExplicitImplementationOverrides = new List<IMethodImplementation>(7);
      iteratorClosureType.InternFactory = this.host.InternFactory;
      iteratorClosureType.IsBeforeFieldInit = true;
      iteratorClosureType.IsClass = true;
      iteratorClosureType.IsSealed = true;
      iteratorClosureType.Layout = LayoutKind.Auto;
      iteratorClosureType.StringFormat = StringFormatKind.Ansi;
      iteratorClosureType.Visibility = TypeMemberVisibility.Private;

      /* Interfaces. */
      result.InitializeInterfaces(result.ElementType, this.isEnumerable);

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
      MethodDefinition constructor = new MethodDefinition() {
        InternFactory = this.host.InternFactory,
        Parameters = new List<IParameterDefinition>(1),
      };
      // Parameter
      ParameterDefinition stateParameter = new ParameterDefinition() {
        ContainingSignature = constructor,
        Index = 0,
        Name = this.host.NameTable.GetNameFor("state"),
        Type = this.host.PlatformType.SystemInt32
      };
      constructor.Parameters.Add(stateParameter);
      // Statements
      MethodCall baseConstructorCall = new MethodCall() { ThisArgument = new ThisReference(), MethodToCall = this.ObjectCtor, Type = this.host.PlatformType.SystemVoid };
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
      SourceMethodBody body = new SourceMethodBody(this.host, this.sourceLocationProvider);
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
      if (this.isEnumerable) {
        CreateGetEnumeratorMethodGeneric(iteratorClosure);
        CreateGetEnumeratorMethodNonGeneric(iteratorClosure);
      }
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
        InternFactory = this.host.InternFactory,
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

      SourceMethodBody body = new SourceMethodBody(this.host, this.sourceLocationProvider, null, this.iteratorLocalCount);
      IBlockStatement block = TranslateIteratorMethodBodyToMoveNextBody(iteratorClosure, blockStatement);
      moveNext.Body = body;
      body.IsNormalized = true;
      body.LocalsAreZeroed = true;
      body.Block = block;
      body.MethodDefinition = moveNext;
    }

    /// <summary>
    /// Create method body of the MoveNext from a copy of the body of the iterator method.
    /// 
    /// First we substitute the locals/parameters with closure fields, and generic method type parameter of the iterator
    /// method with generic type parameters of the closure class (if any). 
    /// Then, we build the state machine. 
    /// </summary>
    private IBlockStatement TranslateIteratorMethodBodyToMoveNextBody(IteratorClosureInformation iteratorClosure, BlockStatement blockStatement) {
      var rewriter = new RewriteAsMoveNext(this.FieldForCapturedLocalOrParameter, this.iteratorLocalCount, this.genericTypeParameterMapping, iteratorClosure, this.host);
      rewriter.RewriteChildren(blockStatement);
      // State machine.
      Dictionary<int, ILabeledStatement> StateEntries = new YieldReturnYieldBreakReplacer(iteratorClosure, this.host).GetStateEntries(blockStatement);
      return BuildStateMachine(iteratorClosure, blockStatement, StateEntries);
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
      var returnFalse = new ReturnStatement() { Expression = new CompileTimeConstant() { Value = false, Type = this.host.PlatformType.SystemBoolean } };
      var returnFalseLabel = new LabeledStatement() { Label = this.host.NameTable.GetNameFor("return false"), Statement = returnFalse };
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
      defaultCase.Body.Add(new GotoStatement() { TargetStatement = returnFalseLabel });
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
        Attributes = new List<ICustomAttribute>(1),
        InternFactory = this.host.InternFactory,
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
      SourceMethodBody body = new SourceMethodBody(this.host, this.sourceLocationProvider);
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
        Attributes = new List<ICustomAttribute>(1),
        InternFactory = this.host.InternFactory,
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
      SourceMethodBody body = new SourceMethodBody(this.host, this.sourceLocationProvider);
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
        var assemblyReference = new Immutable.AssemblyReference(this.host, this.host.CoreAssemblySymbolicIdentity);
        IUnitNamespaceReference ns = new Immutable.RootUnitNamespaceReference(assemblyReference);
        ns = new Immutable.NestedUnitNamespaceReference(ns, this.host.NameTable.GetNameFor("System"));
        var SystemDotThreading = new Immutable.NestedUnitNamespaceReference(ns, this.host.NameTable.GetNameFor("Threading"));
        ITypeReference ThreadingDotThread = new Immutable.NamespaceTypeReference(this.host, SystemDotThreading, this.host.NameTable.GetNameFor("Thread"), 0, false, false, true, PrimitiveTypeCode.Reference);
        foreach (ITypeMemberReference memref in ThreadingDotThread.ResolvedType.GetMembersNamed(this.host.NameTable.GetNameFor("ManagedThreadId"), false)) {
          IPropertyDefinition propertyDef = memref as IPropertyDefinition;
          if (propertyDef != null) {
            return propertyDef;
          }
        }
        return Dummy.PropertyDefinition;
      }
    }

    /// <summary>
    /// An Expression that represents Thread.CurrentThread
    /// </summary>
    MethodCall ThreadDotCurrentThread {
      get {
        var assemblyReference = new Immutable.AssemblyReference(this.host, this.host.CoreAssemblySymbolicIdentity);
        IUnitNamespaceReference ns = new Immutable.RootUnitNamespaceReference(assemblyReference);
        ns = new Immutable.NestedUnitNamespaceReference(ns, this.host.NameTable.GetNameFor("System"));
        var SystemDotThreading = new Immutable.NestedUnitNamespaceReference(ns, this.host.NameTable.GetNameFor("Threading"));
        var ThreadingDotThread = new Immutable.NamespaceTypeReference(this.host, SystemDotThreading, this.host.NameTable.GetNameFor("Thread"), 0, false, false, true, PrimitiveTypeCode.Reference);
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
        Attributes = new List<ICustomAttribute>(1),
        InternFactory = this.host.InternFactory,
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
      var body = new SourceMethodBody(this.host, this.sourceLocationProvider);
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
        Attributes = new List<ICustomAttribute>(1),
        InternFactory = this.host.InternFactory,
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
      SourceMethodBody body1 = new SourceMethodBody(this.host, this.sourceLocationProvider);
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
        Attributes = new List<ICustomAttribute>(1),
        InternFactory = this.host.InternFactory,
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
      if (!iteratorClosure.ElementType.IsValueType && TypeHelper.TypesAreAssignmentCompatible(iteratorClosure.ElementType.ResolvedType, this.host.PlatformType.SystemObject.ResolvedType)) {
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
      SourceMethodBody body = new SourceMethodBody(this.host, this.sourceLocationProvider);
      body.IsNormalized = true;
      body.LocalsAreZeroed = true;
      body.Block = block;
      body.MethodDefinition = getterNonGenericCurrent;
      getterNonGenericCurrent.Body = body;

      // Create generic version of get_Current, the body of which is simply returning this.current.
      MethodDefinition getterGenericCurrent = new MethodDefinition() {
        Attributes = new List<ICustomAttribute>(1),
        InternFactory = this.host.InternFactory,
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
      body = new SourceMethodBody(this.host, this.sourceLocationProvider);
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
        field.InternFactory = this.host.InternFactory;
        field.Name = this.host.NameTable.GetNameFor("<>__" + "this");
        //ITypeReference typeRef;
        //if (TypeHelper.TryGetFullyInstantiatedSpecializedTypeReference(method.ContainingTypeDefinition, out typeRef))
        //  field.Type = typeRef;
        //else
        //  field.Type = method.ContainingTypeDefinition;
        field.Type = NamedTypeDefinition.SelfInstance((INamedTypeDefinition)method.ContainingTypeDefinition, this.host.InternFactory);
        field.Visibility = TypeMemberVisibility.Public;
        field.ContainingTypeDefinition = iteratorClosure.ClosureDefinition;
        iteratorClosure.ThisField = field;
        BoundField boundField = new BoundField(field, iteratorClosure.ThisFieldReference.Type);
        this.FieldForCapturedLocalOrParameter.Add(new ThisReference(), boundField);
      }
      foreach (IParameterDefinition parameter in this.method.Parameters) {
        FieldDefinition field = new FieldDefinition();
        field.InternFactory = this.host.InternFactory;
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
        field.InternFactory = this.host.InternFactory;
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
      current.InternFactory = this.host.InternFactory;
      current.Name = this.host.NameTable.GetNameFor("<>__" + "current");
      current.Type = iteratorClosure.ElementType;
      current.Visibility = TypeMemberVisibility.Private;
      current.ContainingTypeDefinition = iteratorClosure.ClosureDefinition;
      iteratorClosure.CurrentField = current;

      FieldDefinition state = new FieldDefinition();
      state.InternFactory = this.host.InternFactory;
      state.Name = this.host.NameTable.GetNameFor("<>__" + "state");
      state.Type = this.host.PlatformType.SystemInt32;
      state.Visibility = TypeMemberVisibility.Private;
      state.ContainingTypeDefinition = iteratorClosure.ClosureDefinition;
      iteratorClosure.StateField = state;

      FieldDefinition initialThreadId = new FieldDefinition();
      initialThreadId.InternFactory = this.host.InternFactory;
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
      if (genericTypeInstance != null && 
        (TypeHelper.TypesAreEquivalent(genericTypeInstance.GenericType, methodTypeReference.PlatformType.SystemCollectionsGenericIEnumerable)
        || TypeHelper.TypesAreEquivalent(genericTypeInstance.GenericType, methodTypeReference.PlatformType.SystemCollectionsGenericIEnumerator))) {
        return genericTypeInstance.GenericArguments;
      }
      var result = new List<ITypeReference>();
      result.Add(this.host.PlatformType.SystemObject);
      return result;
    }

    class LocalCollector : CodeVisitor {

      internal List<ILocalDefinition> allLocals = new List<ILocalDefinition>();

      /// <summary>
      /// Collect locals declared in the body. 
      /// </summary>
      public override void Visit(ILocalDeclarationStatement localDeclarationStatement) {
        if (!this.allLocals.Contains(localDeclarationStatement.LocalVariable))
          this.allLocals.Add(localDeclarationStatement.LocalVariable);
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
      }

      public override void Visit(IBoundExpression boundExpression) {
        ILocalDefinition localDefinition = boundExpression.Definition as ILocalDefinition;
        if (localDefinition != null) {
          if (!this.allLocals.Contains(localDefinition)) {
            this.allLocals.Add(localDefinition);
          }
        }
      }

      public override void Visit(IAddressableExpression addressableExpression) {
        ILocalDefinition localDefinition = addressableExpression.Definition as ILocalDefinition;
        if (localDefinition != null) {
          if (!this.allLocals.Contains(localDefinition)) {
            this.allLocals.Add(localDefinition);
          }
        }
      }
    }
  }

  internal class YieldReturnYieldBreakReplacer : CodeRewriter {
    /// <summary>
    /// Used in the tranformation of an iterator method body into a MoveNext method body, this class replaces
    /// yield returns and yield breaks with approppriate assignments to this dot current and return statements. 
    /// In addition, it inserts a new label statement after each yield return, and associates a unique state 
    /// number with the label. Such a mapping can be aquired from calling the GetStateEntries method. It is not
    /// suggested to call the Visit methods directly. 
    /// </summary>
    IteratorClosureInformation iteratorClosure;
    internal YieldReturnYieldBreakReplacer(IteratorClosureInformation iteratorClosure, IMetadataHost host) :
      base(host) {
      this.iteratorClosure = iteratorClosure;
    }
    private int stateNumber;
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
        Label = this.host.NameTable.GetNameFor("Label"+ this.stateNumber++), Statement = new EmptyStatement()
      };
      // O(n), but happen only once. 
      blockStatement.Statements.Insert(0, initialLabel);
      stateEntries.Add(0, initialLabel);
      this.stateNumber = 1;
      this.RewriteChildren(blockStatement);
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
    public override IStatement Rewrite(IYieldReturnStatement yieldReturnStatement) {
      BlockStatement blockStatement = new BlockStatement();
      int state = this.stateNumber++;
      ExpressionStatement thisDotStateEqState = new ExpressionStatement() {
        Expression = new Assignment() {
          Source = new CompileTimeConstant() { Value = state, Type = this.host.PlatformType.SystemInt32 },
          Target = new TargetExpression() { Definition = this.iteratorClosure.StateFieldReference, Instance = new ThisReference(), Type = this.host.PlatformType.SystemInt32 },
          Type = this.host.PlatformType.SystemInt32,
        },
        Locations = IteratorHelper.EnumerableIsEmpty(yieldReturnStatement.Locations) ? null : new List<ILocation>(yieldReturnStatement.Locations)
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
    public override IStatement Rewrite(IYieldBreakStatement yieldBreakStatement) {
      BlockStatement blockStatement = new BlockStatement();
      ExpressionStatement thisDotStateEqMinus2 = new ExpressionStatement() {
        Expression = new Assignment() {
          Source = new CompileTimeConstant() { Value = -2, Type = this.host.PlatformType.SystemInt32 },
          Target = new TargetExpression() { Definition = iteratorClosure.StateFieldReference, Type = this.host.PlatformType.SystemInt32, Instance = new ThisReference() },
          Type = this.host.PlatformType.SystemInt32,
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
