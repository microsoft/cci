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
using System.Diagnostics.Contracts;
using System;

namespace Microsoft.Cci.MutableCodeModel {

  /// <summary>
  /// A rewriter for CodeModel method bodies, which changes any anynomous delegate expressions found in the body into delegates over
  /// methods of either the containing type of the method, or of a nested type of that type.
  /// </summary>
  public class AnonymousDelegateRemover : CodeRewriter {

    /// <summary>
    /// A rewriter for CodeModel method bodies, which changes any anynomous delegate expressions found in the body into delegates over
    /// methods of either the containing type of the method, or of a nested type of that type.
    /// </summary>
    /// <param name="host">An object representing the application that is hosting the converter. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.</param>
    /// <param name="sourceLocationProvider">An object that can map the ILocation objects found in a block of statements to IPrimarySourceLocation objects. May be null.</param>
    public AnonymousDelegateRemover(IMetadataHost host, ISourceLocationProvider/*?*/ sourceLocationProvider)
      : base(host) {
      this.copier = new MetadataDeepCopier(host);
      this.sourceLocationProvider = sourceLocationProvider;
      var compilerGeneratedCtor = this.GetReferenceToDefaultConstructor(host.PlatformType.SystemRuntimeCompilerServicesCompilerGeneratedAttribute);
      this.compilerGenerated = new CustomAttribute() { Constructor = compilerGeneratedCtor };
      this.objectCtor = this.GetReferenceToDefaultConstructor(this.host.PlatformType.SystemObject);
    }

    /// <summary>
    /// A copier to use for making copies of type references that need to get specialized for use inside anonymous methods.
    /// </summary>
    MetadataDeepCopier copier;

    /// <summary>
    /// A list of all of the closure (display) classes generated as a result of removing the anonymous delegates.
    /// These classes contain copies of any parameters and locals captured by anonymous delegates.
    /// </summary>
    internal List<ITypeDefinition> closureClasses;

    /// <summary>
    /// Fields that cache anonymous delegates that capture no state are added here. The list comes from the type that contains 
    /// the method whose body is rewritten by the rewriter. (If that type is immutable, closure classes are generated instead.)
    /// </summary>
    List<ITypeDefinitionMember> helperMembers;

    /// <summary>
    /// A list of scopes that declare locals that have been captured. When entering such a scope, a closure must be constructed.
    /// </summary>
    Dictionary<object, bool> scopesWithCapturedLocals = new Dictionary<object, bool>();

    /// <summary>
    /// A map from parameters and locals to closure field references, specialized (if necessary) for use inside of anonymous methods.
    /// </summary>
    Dictionary<object, IFieldReference> fieldReferencesForUseInsideAnonymousMethods;

    /// <summary>
    /// A map from parameters and locals to closure field references, specialized (if necessary) for use inside the method being rewritten.
    /// </summary>
    Dictionary<object, IFieldReference> fieldReferencesForUseInsideThisMethod;

    /// <summary>
    /// A map from the generic parameters (if any) of the method being rewritten, to the corresponding generic parameters of the delegate method (if a peer)
    /// or the type parameters of the closure class (if the method captured state).
    /// </summary>
    Dictionary<ushort, IGenericParameterReference> genericMethodParameterMap;

    /// <summary>
    /// A list of the anonymous delegates that capture the "this" argument of the containing method. They should at the very
    /// least be instance peer methods, unlike delegates that capture nothing and the end up being static peer methods.
    /// </summary>
    Dictionary<IAnonymousDelegate, bool> anonymousDelegatesThatCaptureThis = new Dictionary<IAnonymousDelegate, bool>();

    /// <summary>
    /// A list of anonymous delegates that capture one or more of the arguments (other than this) or the local definitions
    /// of the method being rewritten. The method for these delegates must be instance methods of the classes in which the
    /// state will be captured.
    /// </summary>
    Dictionary<IAnonymousDelegate, bool> anonymousDelegatesThatCaptureLocalsOrParameters = new Dictionary<IAnonymousDelegate, bool>();

    /// <summary>
    /// An object that can map the ILocation objects found in a block of statements to IPrimarySourceLocation objects. May be null.
    /// </summary>
    ISourceLocationProvider/*?*/ sourceLocationProvider;

    /// <summary>
    /// The closure (display) class that holds the captured state of the current block. If the method is generic
    /// the closure class will be generic and this will be the template, not an instantiation.
    /// </summary>
    NamedTypeDefinition currentClosureClass;

    /// <summary>
    /// Either the same as this.currentClosure, or this.currentClosure.InstanceType if the closure is generic.
    /// Use this instance inside the body of an anonymous delegate.
    /// </summary>
    ITypeReference currentClosureSelfInstance;

    /// <summary>
    /// Either the same as this.currentClosureClass, or an instance of it using the generic type parameters of the method being
    /// rewritten as the generic arguments. Use this instance inside the body of the method being rewritten.
    /// </summary>
    ITypeReference currentClosureInstance;

    /// <summary>
    /// A bound expression for accessing the local that contains the current closure object instance.
    /// </summary>
    IExpression currentClosureObject;

    /// <summary>
    /// The local variable that holds the instance of the current closure class. This.currentClosureInstance is an expression that binds to this local.
    /// </summary>
    ILocalDefinition currentClosureLocal;

    /// <summary>
    /// A list expressions (local bindings) for all of the closure object instances. Only used in the method being rewritten. Delegate methods
    /// will start with their "this" argument and then traverse the outer closure fields of that object.
    /// </summary>
    List<IExpression> closureLocalInstances;

    /// <summary>
    /// A custom attribute that indicates that the attributed definition was generated by the compiler. Used to annotate closure classes,
    /// cache fields and delegate methods (if they are peers).
    /// </summary>
    ICustomAttribute compilerGenerated;

    /// <summary>
    /// A number that is incremented every time a method is generated for an anonymous delegate, and that helps to make the method names unique.
    /// </summary>
    int anonymousDelegateCounter;

    /// <summary>
    /// A flag that indicates if the rewriter is inside the method whose anonymous delegates are being removed, or inside the body
    /// of generated delegate method.
    /// </summary>
    bool isInsideAnonymousMethod;

    /// <summary>
    /// The method containing the block that is being rewritten.
    /// </summary>
    IMethodDefinition method;

    /// <summary>
    /// A reference to the default constructor of System.Object. Used during the generation of closure class constructors.
    /// </summary>
    IMethodReference objectCtor;

    /// <summary>
    /// When given a method definition and a block of statements that represents the Block property of the body of the method
    /// this method returns a semantically equivalent SourceMethod with a body that no longer has any anonymous delegate expressions.
    /// The given block of statements is mutated in place.
    /// Any types that get defined in order to implement the body semantics are returned (null if no such types are defined).
    /// </summary>
    /// <param name="method">The method containing the block that is to be rewritten.</param>
    /// <param name="body">The block to be rewritten. 
    /// The entire tree rooted at the block must be mutable and the nodes must not be shared with anything else.</param>
    public ICollection<ITypeDefinition>/*?*/ RemoveAnonymousDelegates(IMethodDefinition method, BlockStatement body) {
      this.method = method;
      var finder = new CapturedParameterAndLocalFinder();
      finder.TraverseChildren(body);
      this.fieldReferencesForUseInsideAnonymousMethods = finder.captures;
      this.anonymousDelegatesThatCaptureLocalsOrParameters = finder.anonymousDelegatesThatCaptureLocalsOrParameters;
      this.anonymousDelegatesThatCaptureThis = finder.anonymousDelegatesThatCaptureThis;
      finder = null;
      var blockFinder = new ScopesWithCapturedLocalsFinder(this.fieldReferencesForUseInsideAnonymousMethods);
      blockFinder.TraverseChildren(body);
      this.scopesWithCapturedLocals = blockFinder.scopesWithCapturedLocals;
      blockFinder = null;
      this.fieldReferencesForUseInsideThisMethod = new Dictionary<object, IFieldReference>(this.fieldReferencesForUseInsideAnonymousMethods);

      this.GenerateTopLevelClosure();
      if (this.currentClosureClass != null) {
        //declare a local to keep the parameter closure class
        var closureLocal = new LocalDefinition() { Type = this.currentClosureInstance, Name = this.host.NameTable.GetNameFor("CS$<>8__locals"+this.closureClasses.Count) };
        this.currentClosureLocal = closureLocal;
        this.currentClosureObject = new BoundExpression() { Definition = closureLocal, Type = closureLocal.Type };
        this.closureLocalInstances = new List<IExpression>();
        this.closureLocalInstances.Add(this.currentClosureObject);
        this.scopesWithCapturedLocals.Remove(body); //otherwise it will introduce its own closure class if any of its locals were captured.
        this.RewriteChildren(body);
        //do this after rewriting so that parameter references are not rewritten into closure field references.
        this.InsertStatementsToAllocateAndInitializeTopLevelClosure(body);
      } else {
        this.RewriteChildren(body);
      }
      return this.closureClasses == null ? null : this.closureClasses.AsReadOnly();
    }

    /// <summary>
    /// Inserts statements into block that will create an instance of the top level closure and initialize it 
    /// with captured parameter values and the "this" value (if captured).
    /// </summary>
    private void InsertStatementsToAllocateAndInitializeTopLevelClosure(BlockStatement body) {
      List<IStatement> statements = new List<IStatement>(this.method.ParameterCount+2+body.Statements.Count);
      IFieldReference fieldReference;
      //initialize local with an instance of the closure class
      statements.Add(
        new LocalDeclarationStatement() {
          LocalVariable = this.currentClosureLocal,
          InitialValue = new CreateObjectInstance() {
            MethodToCall = this.GetReferenceToDefaultConstructor(this.currentClosureInstance),
            Type = this.currentClosureInstance
          }
        });
      //initialize the fields of the closure class with the initial parameter values
      foreach (var parameter in this.method.Parameters) {
        if (!this.fieldReferencesForUseInsideThisMethod.TryGetValue(parameter, out fieldReference)) continue;
        statements.Add(
          new ExpressionStatement() {
            Expression = new Assignment() {
              Target = new TargetExpression() { Instance = this.currentClosureObject, Definition = fieldReference, Type = fieldReference.Type },
              Source = new BoundExpression() { Definition = parameter, Type = parameter.Type },
              Type = parameter.Type
            }
          });
      }
      if (this.method.IsConstructor) {
        var offset = this.GetOffsetOfFirstStatementAfterBaseOrThisConstructorCall(body.Statements);
        statements.AddRange(body.Statements.GetRange(0, offset));
        body.Statements.RemoveRange(0, offset);
      }
      //intialize the this argument field if that exists
      if (this.fieldReferencesForUseInsideThisMethod.TryGetValue(this.method, out fieldReference)) {
        statements.Add(
          new ExpressionStatement() {
            Expression = new Assignment() {
              Target = new TargetExpression() { Instance = this.currentClosureObject, Definition = fieldReference, Type = fieldReference.Type },
              Source = new ThisReference() { Type = fieldReference.Type },
              Type = fieldReference.Type
            }
          });
      }
      statements.AddRange(body.Statements);
      body.Statements = statements;
    }

    private int GetOffsetOfFirstStatementAfterBaseOrThisConstructorCall(List<IStatement> list) {
      Contract.Requires(list != null);

      for (int i = 0, count = list.Count; i < count; i++) {
        var exprStat = list[i] as ExpressionStatement;
        if (exprStat == null) continue;
        var mcall = exprStat.Expression as MethodCall;
        if (mcall == null) continue;
        var method = mcall.MethodToCall.ResolvedMethod;
        if (!method.IsConstructor) continue;
        if (TypeHelper.TypesAreEquivalent(this.method.ContainingTypeDefinition, method.ContainingTypeDefinition))
          return i + 1;
        foreach (var baseClass in this.method.ContainingTypeDefinition.BaseClasses) {
          if (TypeHelper.TypesAreEquivalent(baseClass, method.ContainingTypeDefinition))
            return i+1;
        }
      }
      return 0;
    }

    /// <summary>
    /// If any parameters have been captured, or if "this" has been captured along with some locals, generate a closure class
    /// for the root block and populate it with fields to hold the captured parameter values and the "this" argument (if captured).
    /// </summary>
    private void GenerateTopLevelClosure() {
      var mutableContainingType = this.method.ContainingTypeDefinition as NamedTypeDefinition;
      if (mutableContainingType != null) {
        this.helperMembers = mutableContainingType.PrivateHelperMembers; //methods for anonymous delegates that capture nothing go here
      } else if (this.fieldReferencesForUseInsideThisMethod.Count == 0) {
        //None of the anonymous delegates have captured anything, except perhaps "this", so they can all become peer methods if method.ContainingType is mutable.
        //However, we better create a closure class since method.ContainingType is not mutable.
        //TODO: introduce an interface that can be implemented by the AST classes as well, so that NamedTypeDefinition is not the only way to be mutable.
        this.CreateClosureClass();
      }
      //Add fields for captured parameters
      foreach (var parameter in this.method.Parameters) {
        if (!this.fieldReferencesForUseInsideThisMethod.ContainsKey(parameter)) continue;
        if (this.currentClosureClass == null) this.CreateClosureClass();
        this.isInsideAnonymousMethod = true;
        var fieldType = this.Rewrite(this.copier.Copy(parameter.Type)); //get the type as the anonymous delegate will see it
        this.isInsideAnonymousMethod = false;
        this.CreateClosureField(parameter, fieldType, parameter.Type, parameter.Name.Value);
      }
      //Add field for captured "this" if needed
      if (this.anonymousDelegatesThatCaptureThis.Count > 0) {
        if (this.currentClosureClass == null) this.CreateClosureClass();
        ITypeReference thisTypeReference = NamedTypeDefinition.SelfInstance((INamedTypeDefinition)this.method.ContainingTypeDefinition, this.host.InternFactory);
        this.isInsideAnonymousMethod = true;
        var fieldType = this.Rewrite(this.copier.Copy(thisTypeReference)); //get the type as the anonymous delegate will see it
        this.isInsideAnonymousMethod = false;
        this.CreateClosureField(this.method, fieldType, thisTypeReference, "<>__this");
      }
    }

    /// <summary>
    /// Creates a field in the current closure class with the given type and name.
    /// If the current closure class is generic, the returned value is a reference
    /// to the corresponding field in the InstanceType of the current closure class.
    /// </summary>
    private void CreateClosureField(object capturedDefinition, ITypeReference fieldType, ITypeReference typeToUseInThisMethod, string name) {
      FieldDefinition field = new FieldDefinition() {
        ContainingTypeDefinition = this.currentClosureClass,
        InternFactory = this.host.InternFactory,
        Name = this.host.NameTable.GetNameFor(name),
        Type = fieldType,
        Visibility = TypeMemberVisibility.Public
      };
      this.currentClosureClass.Fields.Add(field);
      if (this.currentClosureClass == this.currentClosureInstance) {
        //no generics
        this.fieldReferencesForUseInsideAnonymousMethods[capturedDefinition] = field;
        this.fieldReferencesForUseInsideThisMethod[capturedDefinition] = field;
        return;
      }
      var fieldRef = new SpecializedFieldReference() {
        ContainingType = this.currentClosureSelfInstance, //The type the closure uses to refer to itself
        InternFactory = this.host.InternFactory,
        Name = field.Name,
        Type = field.Type,
        UnspecializedVersion = field,
      };
      this.fieldReferencesForUseInsideAnonymousMethods[capturedDefinition] = fieldRef;
      fieldRef = new SpecializedFieldReference() {
        ContainingType = this.currentClosureInstance, //The type this.method uses to refer to the closure.
        InternFactory = this.host.InternFactory,
        Name = field.Name,
        Type = typeToUseInThisMethod,
        UnspecializedVersion = field,
      };
      this.fieldReferencesForUseInsideThisMethod[capturedDefinition] = fieldRef;
    }

    /// <summary>
    /// Creates a new nested type definition with a default constructor and no other members and adds it to this.closureClasses.
    /// If this.method is generic, then the closure class is generic as well, with the same
    /// number of type parameters (constrained in the same way) as the generic method.
    /// Initializes this.currentClosure, this.currentClosureInstance and this.currentClosureSelfInstance.
    /// </summary>
    private void CreateClosureClass() {
      if (this.closureClasses == null) this.closureClasses = new List<ITypeDefinition>();
      NestedTypeDefinition closure = new NestedTypeDefinition();
      var containingType = this.method.ContainingTypeDefinition;
      closure.Name = this.host.NameTable.GetNameFor("<"+this.method.Name+">c__DisplayClass"+closure.GetHashCode());
      closure.Attributes = new List<ICustomAttribute>(1) { this.compilerGenerated };
      closure.BaseClasses = new List<ITypeReference>(1) { this.host.PlatformType.SystemObject };
      closure.ContainingTypeDefinition = containingType;
      closure.Fields = new List<IFieldDefinition>();
      closure.InternFactory = this.host.InternFactory;
      closure.IsBeforeFieldInit = true;
      closure.IsClass = true;
      closure.IsSealed = true;
      closure.Layout = LayoutKind.Auto;
      closure.Methods = new List<IMethodDefinition>();
      closure.StringFormat = StringFormatKind.Ansi;
      closure.Visibility = TypeMemberVisibility.Private;
      this.closureClasses.Add(closure);
      this.currentClosureClass = closure;

      //generics
      if (this.method.IsGeneric) {
        Dictionary<ushort, IGenericParameterReference> genericMethodParameterMap = new Dictionary<ushort, IGenericParameterReference>();
        this.genericMethodParameterMap = genericMethodParameterMap;
        bool foundConstraints = false;
        var genericTypeParameters = new List<IGenericTypeParameter>(this.method.GenericParameterCount);
        closure.GenericParameters = genericTypeParameters;
        foreach (var genericMethodParameter in this.method.GenericParameters) {
          var copyOfGenericMethodParameter = this.copier.Copy(genericMethodParameter); //so that we have mutable constraints to rewrite
          var genericTypeParameter = new GenericTypeParameter();
          genericTypeParameter.Copy(copyOfGenericMethodParameter, this.host.InternFactory);
          genericTypeParameter.DefiningType = closure;
          if (genericTypeParameter.Constraints != null && genericTypeParameter.Constraints.Count > 0) foundConstraints = true;
          genericTypeParameters.Add(genericTypeParameter);
          genericMethodParameterMap.Add(copyOfGenericMethodParameter.Index, genericTypeParameter);
        }
        if (foundConstraints) {
          //Fix up any self references that might lurk inside constraints.
          closure.GenericParameters = new GenericParameterRewriter(this.host, genericMethodParameterMap).Rewrite(genericTypeParameters);
        }
        var instanceType = closure.InstanceType;
        var genericArguments = IteratorHelper.GetConversionEnumerable<IGenericMethodParameter, ITypeReference>(this.method.GenericParameters);
        this.currentClosureInstance = new Immutable.GenericTypeInstanceReference(instanceType.GenericType, genericArguments, this.host.InternFactory);
        this.currentClosureSelfInstance = instanceType;
      } else {
        //if any of the containing types are generic, we need an instance or a specialized nested type.
        this.currentClosureInstance = NestedTypeDefinition.SelfInstance(closure, this.host.InternFactory);
        this.currentClosureSelfInstance = this.currentClosureInstance;
      }

      //default constructor
      var block = new BlockStatement();
      block.Statements.Add(
        new ExpressionStatement() {
          Expression = new MethodCall() {
            ThisArgument = new ThisReference() { Type = this.currentClosureSelfInstance },
            MethodToCall = this.objectCtor,
            Type = this.host.PlatformType.SystemVoid
          }
        }
      );

      var constructorBody = new SourceMethodBody(this.host, this.sourceLocationProvider) {
        LocalsAreZeroed = true,
        IsNormalized = true,
        Block = block
      };

      var defaultConstructor = new MethodDefinition() {
        Body = constructorBody,
        ContainingTypeDefinition = closure,
        CallingConvention = CallingConvention.HasThis,
        InternFactory = this.host.InternFactory,
        IsCil = true,
        IsHiddenBySignature = true,
        IsRuntimeSpecial = true,
        IsSpecialName = true,
        Name = this.host.NameTable.Ctor,
        Type = this.host.PlatformType.SystemVoid,
        Visibility = TypeMemberVisibility.Public,
      };
      constructorBody.MethodDefinition = defaultConstructor;
      closure.Methods.Add(defaultConstructor);

    }

    /// <summary>
    /// Returns a reference to the closure method. If the method is generic, the reference is to an instantiation, 
    /// using the generic parameters of the current class as arguments.
    /// </summary>
    private IMethodReference CreateClosureMethod(AnonymousDelegate anonymousDelegate) {
      bool isPeerMethod = this.helperMembers != null && !this.anonymousDelegatesThatCaptureLocalsOrParameters.ContainsKey(anonymousDelegate);
      bool isStaticMethod = isPeerMethod && !this.anonymousDelegatesThatCaptureThis.ContainsKey(anonymousDelegate);
      var body = new SourceMethodBody(this.host, this.sourceLocationProvider) {
        Block = anonymousDelegate.Body,
        LocalsAreZeroed = true
      };
      var counter = isPeerMethod ? this.helperMembers.Count : this.anonymousDelegateCounter++;
      var prefix = "<"+this.method.Name.Value;
      prefix += isPeerMethod ? ">p__" : ">b__";
      var method = new MethodDefinition() {
        ContainingTypeDefinition = isPeerMethod ? this.method.ContainingTypeDefinition : this.currentClosureClass,
        Name = this.host.NameTable.GetNameFor(prefix+counter),
        Visibility = isPeerMethod ? TypeMemberVisibility.Private : TypeMemberVisibility.Public,
        Body = body,
        CallingConvention = isStaticMethod ? CallingConvention.Default : CallingConvention.HasThis,
        InternFactory = this.host.InternFactory,
        Parameters = anonymousDelegate.Parameters,
        Type = anonymousDelegate.ReturnType,
        IsCil = true,
        IsStatic = isStaticMethod,
        IsHiddenBySignature = true,
      };
      body.MethodDefinition = method;
      if (method.Parameters != null) {
        foreach (ParameterDefinition parameterDefinition in method.Parameters)
          parameterDefinition.ContainingSignature = method;
      }

      if (isPeerMethod) {
        this.helperMembers.Add(method);
        if (this.method.IsGeneric) this.MakeDelegateMethodGeneric(method);
        method.Attributes = new List<ICustomAttribute>(1);
        method.Attributes.Add(this.compilerGenerated);
      } else {
        this.currentClosureClass.Methods.Add(method);
        this.isInsideAnonymousMethod = true;
        this.RewriteChildren(method);
        this.isInsideAnonymousMethod = false;
      }

      IMethodReference methodReference = method;
      ITypeReference containingTypeDefinitionInstance = method.ContainingTypeDefinition;
      if (isPeerMethod)
        containingTypeDefinitionInstance = NamedTypeDefinition.SelfInstance((INamedTypeDefinition)method.ContainingTypeDefinition, this.host.InternFactory);
      if ((isPeerMethod && method.ContainingTypeDefinition != containingTypeDefinitionInstance) || 
          (!isPeerMethod && this.currentClosureClass != this.currentClosureInstance)) {
        methodReference = new MethodReference() {
          CallingConvention = method.CallingConvention,
          ContainingType = isPeerMethod ? containingTypeDefinitionInstance : this.currentClosureInstance,
          GenericParameterCount = method.GenericParameterCount,
          InternFactory = this.host.InternFactory,
          Name = method.Name,
          Parameters = methodReference.ParameterCount == 0 ? null : new List<IParameterTypeInformation>(methodReference.Parameters),
          Type = method.Type,
        };
      }

      if (!method.IsGeneric) return methodReference;
      return new GenericMethodInstanceReference() {
        CallingConvention = method.CallingConvention,
        ContainingType = method.ContainingTypeDefinition,
        GenericArguments = new List<ITypeReference>(IteratorHelper.GetConversionEnumerable<IGenericMethodParameter, ITypeReference>(method.GenericParameters)),
        GenericMethod = methodReference,
        InternFactory = this.host.InternFactory,
        Name = method.Name,
        Parameters = methodReference.ParameterCount == 0 ? null : new List<IParameterTypeInformation>(methodReference.Parameters),
        Type = method.Type,
      };
    }

    /// <summary>
    /// Returns an expression that results in a closure object instance that contains the given field.
    /// If the expression will be evaluated in the body of this.method, the result is a bound expression
    /// that references the local that contains the object. Otherwise it is the "this" argument of the 
    /// anonymous delegate method, possibly with a number of field accesses to chase down the outer closure chain.
    /// </summary>
    /// <param name="closureField">A reference to a field from the "self instance" of a closure class.</param>
    private IExpression GetClosureObjectInstanceContaining(IFieldReference closureField) {
      if (this.isInsideAnonymousMethod) {
        IExpression result = new ThisReference() { Type = this.currentClosureSelfInstance };
        while (result.Type != closureField.ContainingType) {
          var outerClosureField = this.fieldReferencesForUseInsideAnonymousMethods[result.Type];
          result = new BoundExpression() { Instance = result, Definition = outerClosureField, Type = outerClosureField.Type };
        }
        return result;
      } else {
        foreach (var instance in this.closureLocalInstances) {
          if (instance.Type == closureField.ContainingType) return instance;
        }
        return this.currentClosureObject;
      }
    }

    /// <summary>
    /// Copies the generic parameters from this.method to the given delegate method and fixes up 
    /// the delegate method signature to refer to its own type parameters, rather than those of this.method.
    /// </summary>
    private void MakeDelegateMethodGeneric(MethodDefinition delegateMethod) {
      delegateMethod.CallingConvention |= CallingConvention.Generic;
      var savedGenericMethodParameterMap = this.genericMethodParameterMap;
      this.genericMethodParameterMap = new Dictionary<ushort, IGenericParameterReference>();
      delegateMethod.GenericParameters = new List<IGenericMethodParameter>(this.method.GenericParameterCount);
      foreach (var genericParameter in this.method.GenericParameters) {
        var delPar = new GenericMethodParameter();
        delPar.Copy(genericParameter, this.host.InternFactory);
        delPar.DefiningMethod = delegateMethod;
        delegateMethod.GenericParameters.Add(delPar);
        this.genericMethodParameterMap.Add(genericParameter.Index, delPar);
      }
      new GenericParameterRewriter(this.host, this.genericMethodParameterMap).RewriteChildren(delegateMethod);
      this.genericMethodParameterMap = savedGenericMethodParameterMap;
    }

    class GenericParameterRewriter : CodeRewriter {

      /// <summary>
      /// A rewriter that substitutes the keys of the genericMethodParameterMap with their corresponding values.
      /// </summary>
      internal GenericParameterRewriter(IMetadataHost host, Dictionary<ushort, IGenericParameterReference> genericMethodParameterMap)
        : base(host) {
        this.genericMethodParameterMap = genericMethodParameterMap;
      }

      Dictionary<ushort, IGenericParameterReference> genericMethodParameterMap;

      /// <summary>
      /// Rewrites the children of the given generic method instance reference.
      /// </summary>
      public override void RewriteChildren(GenericMethodInstanceReference genericMethodInstanceReference) {
        this.RewriteChildren((MethodReference)genericMethodInstanceReference);
        genericMethodInstanceReference.GenericArguments = this.Rewrite(genericMethodInstanceReference.GenericArguments);
        //do not rewrite the generic method reference, it does not contain any references to generic method type parameters of this.method
        //but it might have referenes to generic method type parameters, which will confuse the code below.
      }

      public override ITypeReference Rewrite(IGenericMethodParameterReference genericMethodParameterReference) {
        IGenericParameterReference referenceToSubstitute;
        if (this.genericMethodParameterMap.TryGetValue(genericMethodParameterReference.Index, out referenceToSubstitute))
          return referenceToSubstitute;
        Contract.Assume(false); //An anonymous delegate body should not be able to reference generic method type parameters that are not defined by the containing method.
        return base.Rewrite(genericMethodParameterReference);
      }
    }

    /// <summary>
    /// Rewrites the given anonymous delegate expression.
    /// </summary>
    public override IExpression Rewrite(IAnonymousDelegate anonymousDelegate) {
      if (this.isInsideAnonymousMethod) return base.Rewrite(anonymousDelegate);
      IMethodReference method = this.CreateClosureMethod((AnonymousDelegate)anonymousDelegate);
      var createDelegate = new CreateDelegateInstance() {
        MethodToCallViaDelegate = method,
        Type = anonymousDelegate.Type
      };
      if (!method.IsStatic) {
        //TODO: if there is reason to believe the delegate will be constructed in a loop, but its closure is constructed before the loop, then cache the delegate in a local
        //that is in the same scope as the closure instance
        if (method.ContainingType == this.currentClosureInstance)
          createDelegate.Instance = this.currentClosureObject;
        else //non static peer method
          createDelegate.Instance = new ThisReference() {
            Type = NamedTypeDefinition.SelfInstance((INamedTypeDefinition)this.method.ContainingTypeDefinition, this.host.InternFactory)
          };
      } else if ((method.CallingConvention & CallingConvention.Generic) == 0) {
        //cache the delegate in a static field (we can only do this if method is not generic, i.e. when at most one instance will be created).
        var cache = this.CreateStaticCacheField(anonymousDelegate.Type);
        var boundField = new BoundExpression() { Definition = cache, Type = cache.Type };
        var statements = new List<IStatement>(1);
        var conditional = new ConditionalStatement() {
          Condition = new Equality() {
            LeftOperand = boundField,
            RightOperand = new CompileTimeConstant() { Value = null, Type = cache.Type },
            Type = this.host.PlatformType.SystemBoolean
          },
          TrueBranch = new ExpressionStatement() {
            Expression = new Assignment() {
              Target = new TargetExpression() { Definition = cache, Type = cache.Type },
              Source = createDelegate,
              Type = cache.Type
            }
          }
        };
        statements.Add(conditional);
        return new BlockExpression() {
          BlockStatement = new BlockStatement() { Statements = statements },
          Expression = boundField
        };
      }
      return createDelegate;
    }

    /// <summary>
    /// Creates and returns a static field of the given type and adds it to the helper members of the containing type of this.method.
    /// </summary>
    private IFieldReference CreateStaticCacheField(ITypeReference fieldType) {
      FieldDefinition field = new FieldDefinition() {
        ContainingTypeDefinition = this.method.ContainingTypeDefinition,
        InternFactory = this.host.InternFactory,
        Name = this.host.NameTable.GetNameFor("CS$<>__CachedAnonymousMethodDelegate"+this.helperMembers.Count),
        Type = fieldType,
        Visibility = TypeMemberVisibility.Private,
        IsStatic = true,
        Attributes = new List<ICustomAttribute>(1)
      };
      field.Attributes.Add(this.compilerGenerated);
      this.helperMembers.Add(field);
      var containingType = field.ContainingTypeDefinition;
      if (!TypeHelper.HasOwnOrInheritedTypeParameters(containingType)) return field;
      ITypeReference tr = containingType;
      if (containingType.IsGeneric)
        tr = containingType.InstanceType;
      else {
        Contract.Assume(containingType is INestedTypeDefinition);
        tr = NestedTypeDefinition.SelfInstance((INestedTypeDefinition)containingType, this.host.InternFactory);
      }
      return new SpecializedFieldReference() {
        ContainingType = tr,
        InternFactory = this.host.InternFactory,
        Name = field.Name,
        UnspecializedVersion = field,
        Type = field.Type,
      };
    }

    /// <summary>
    /// Rewrites the children of the given addressable expression.
    /// </summary>
    public override void RewriteChildren(AddressableExpression addressableExpression) {
      IFieldReference closureField;
      var map = this.isInsideAnonymousMethod ? this.fieldReferencesForUseInsideAnonymousMethods : this.fieldReferencesForUseInsideThisMethod;
      if (map.TryGetValue(addressableExpression.Definition, out closureField)) {
        addressableExpression.Instance = this.GetClosureObjectInstanceContaining(closureField);
        addressableExpression.Definition = closureField;
        addressableExpression.Type = closureField.Type;
        return;
      }
      base.RewriteChildren(addressableExpression);
    }

    /// <summary>
    /// Rewrites the children of the given block expression.
    /// </summary>
    /// <param name="blockExpression"></param>
    public override void RewriteChildren(BlockExpression blockExpression) {
      var block = blockExpression.BlockStatement as BlockStatement;
      Contract.Assume(block != null);
      if (this.scopesWithCapturedLocals.ContainsKey(block)) {
        this.AllocateClosureFor(block, block.Statements, () => {
          base.RewriteChildren(block);
          blockExpression.Expression = this.Rewrite(blockExpression.Expression);
        });
      } else
        base.RewriteChildren(blockExpression);
    }

    /// <summary>
    /// Rewrites the children of the given statement block.
    /// </summary>
    public override void RewriteChildren(BlockStatement block) {
      if (this.scopesWithCapturedLocals.ContainsKey(block)) {
        this.AllocateClosureFor(block, block.Statements, () => base.RewriteChildren(block));
      } else
        base.RewriteChildren(block);
    }

    delegate void Action(); //not defined in CLR v2.

    /// <summary>
    /// Saves the current closure fields. Allocates a new closure and updates the fields. Then calls the given delegate and
    /// restores the earlier state.
    /// </summary>
    private void AllocateClosureFor(object scope, List<IStatement> statements, Action rewriteScope) {
      Contract.Assume(!this.isInsideAnonymousMethod);
      var savedCurrentClosure = this.currentClosureClass;
      var savedCurrentClosureSelfInstance = this.currentClosureSelfInstance;
      var savedCurrentClosureInstance = this.currentClosureInstance;
      var savedCurrentClosureObject = this.currentClosureObject;
      var savedCurrentClosureLocal = this.currentClosureLocal;
      this.CreateClosureClass();
      IFieldReference outerClosure = null;
      if (savedCurrentClosureLocal != null) {
        this.CreateClosureField(this.currentClosureSelfInstance, savedCurrentClosureSelfInstance, savedCurrentClosureInstance, savedCurrentClosureLocal.Name.Value);
        outerClosure = this.fieldReferencesForUseInsideThisMethod[this.currentClosureSelfInstance];
      }

      var closureLocal = new LocalDefinition() { Type = this.currentClosureInstance, Name = this.host.NameTable.GetNameFor("CS$<>__locals"+this.closureClasses.Count) };
      this.currentClosureObject = new BoundExpression() { Definition = closureLocal, Type = this.currentClosureInstance };
      this.currentClosureLocal = closureLocal;
      if (this.closureLocalInstances == null) this.closureLocalInstances = new List<IExpression>();
      this.closureLocalInstances.Add(this.currentClosureObject);
      rewriteScope();
      Statement createClosure = new ExpressionStatement() {
        Expression = new Assignment() {
          Target = new TargetExpression() { Definition = closureLocal, Type = closureLocal.Type },
          Source = new CreateObjectInstance() {
            MethodToCall = this.GetReferenceToDefaultConstructor(this.currentClosureInstance),
            Type = currentClosureSelfInstance,
          }
        }
      };
      ILabeledStatement labeledStatement = null;
      for (int i = 0, n = statements.Count; i < n; i++) {
        labeledStatement = statements[i] as ILabeledStatement;
        if (labeledStatement != null) {
          createClosure = new LabeledStatement() { Label = labeledStatement.Label, Statement = createClosure };
          createClosure.Locations.AddRange(labeledStatement.Locations);
          statements[i] = labeledStatement.Statement;
          break;
        } else if (statements[i] is IEmptyStatement) {
          continue;
        } else {
          var declSt = statements[i] as ILocalDeclarationStatement;
          if (declSt != null && declSt.InitialValue == null) continue;
          break;
        }
      }
      statements.Insert(0, createClosure);
      if (outerClosure != null) {
        statements.Insert(1, new ExpressionStatement() {
          Expression = new Assignment() {
            Target = new TargetExpression() { Instance = new BoundExpression() { Definition = closureLocal, Type = closureLocal.Type }, Definition = outerClosure, Type = closureLocal.Type },
            Source = new BoundExpression() { Definition = savedCurrentClosureLocal, Type = savedCurrentClosureLocal.Type }, 
            Type = closureLocal.Type,
          }
        });
      }
      this.currentClosureClass = savedCurrentClosure;
      this.currentClosureSelfInstance = savedCurrentClosureSelfInstance;
      this.currentClosureInstance = savedCurrentClosureInstance;
      this.currentClosureObject = savedCurrentClosureObject;
      this.currentClosureLocal = savedCurrentClosureLocal;
    }

    /// <summary>
    /// Returns a reference to the default constructor method of the given type.
    /// </summary>
    /// <param name="containingType">The type whose default constructor is wanted. If the type is generic this
    /// must be a generic type instance where the generic parameters are used as the generic arguments. (I.e. the "self" instance.)</param>
    private IMethodReference GetReferenceToDefaultConstructor(ITypeReference containingType) {
      return new MethodReference() {
        CallingConvention = CallingConvention.HasThis,
        ContainingType = containingType,
        InternFactory = this.host.InternFactory,
        Name = this.host.NameTable.Ctor,
        Type = this.host.PlatformType.SystemVoid
      };
    }

    /// <summary>
    /// Rewrites the given assignment expression.
    /// </summary>
    public override IExpression Rewrite(IAssignment assignment) {
      var targetInstance = assignment.Target.Instance;
      var result = base.Rewrite(assignment);
      if (targetInstance == null && assignment.Target.Instance != null) {
        //The target now pushes something onto the stack that was not there before the rewrite.
        //It the right hand side uses the stack, then it will not see the stack it expected.
        //If so, we need to evaluate the right handside and squirrel it away in a temp before executing
        //the actual assignment.
        var popFinder = new PopFinder();
        popFinder.Traverse(assignment.Source);
        if (popFinder.foundAPop) {
          var temp = new LocalDefinition() { Name = this.host.NameTable.GetNameFor("PopTemp"+this.popTempCounter++), Type = assignment.Source.Type };
          var localDeclarationStatement = new LocalDeclarationStatement() { LocalVariable = temp, InitialValue = assignment.Source };
          var blockStatement = new BlockStatement();
          blockStatement.Statements.Add(localDeclarationStatement);
          Contract.Assume(assignment is Assignment);
          ((Assignment)assignment).Source = new BoundExpression() { Definition = temp, Type = temp.Type };
          return new BlockExpression() { BlockStatement = blockStatement, Expression = assignment, Type = assignment.Type };
        }        
      }
      return result;
    }

    int popTempCounter;

    class PopFinder : CodeTraverser {
      internal bool foundAPop;

      public override void TraverseChildren(IPopValue popValue) {
        this.foundAPop = true;
        this.StopTraversal = true;
      }
    }

    /// <summary>
    /// Rewrites the children of the given bound expression.
    /// </summary>
    public override void RewriteChildren(BoundExpression boundExpression) {
      IFieldReference closureField;
      var map = this.isInsideAnonymousMethod ? this.fieldReferencesForUseInsideAnonymousMethods : this.fieldReferencesForUseInsideThisMethod;
      if (map.TryGetValue(boundExpression.Definition, out closureField)) {
        boundExpression.Instance = this.GetClosureObjectInstanceContaining(closureField);
        boundExpression.Definition = closureField;
        boundExpression.Type = closureField.Type;
        return;
      }
      base.RewriteChildren(boundExpression);
    }

    /// <summary>
    /// Rewrites the children of the given catch clause.
    /// </summary>
    /// <param name="catchClause"></param>
    public override void RewriteChildren(CatchClause catchClause) {
      if (this.scopesWithCapturedLocals.ContainsKey(catchClause)) {
        var statements = ((BlockStatement)catchClause.Body).Statements;
        this.AllocateClosureFor(catchClause, statements,
          delegate() {
            this.isInsideAnonymousMethod = true;
            var local = catchClause.ExceptionContainer;
            var fieldType = this.Rewrite(this.copier.Copy(local.Type)); //get the type as the anon delegate will see it
            this.isInsideAnonymousMethod = false;
            this.CreateClosureField(local, fieldType, local.Type, local.Name.Value);
            var field = this.fieldReferencesForUseInsideThisMethod[local];
            statements.Insert(0, new ExpressionStatement() {
              Expression = new Assignment() {
                Target = new TargetExpression() { Instance = this.currentClosureObject, Definition = field, Type = field.Type },
                Source = new BoundExpression() { Definition = local, Type = local.Type },
                Type = local.Type,
              }
            });
            base.RewriteChildren(catchClause);
          });
      } else
        base.RewriteChildren(catchClause);
    }

    /// <summary>
    /// Rewrites the children of the given foreach statement.
    /// </summary>
    public override void RewriteChildren(ForEachStatement forEachStatement) {
      if (this.scopesWithCapturedLocals.ContainsKey(forEachStatement)) {
        var statements = new List<IStatement>();
        forEachStatement.Collection = this.Rewrite(forEachStatement.Collection);
        this.AllocateClosureFor(forEachStatement, statements,
          delegate() {
            this.isInsideAnonymousMethod = true;
            var local = forEachStatement.Variable;
            var fieldType = this.Rewrite(this.copier.Copy(local.Type)); //get the type as the anon delegate will see it
            this.isInsideAnonymousMethod = false;
            this.CreateClosureField(local, fieldType, local.Type, local.Name.Value);
            var field = this.fieldReferencesForUseInsideThisMethod[local];
            statements.Add(new ExpressionStatement() {
              Expression = new Assignment() {
                Target = new TargetExpression() { Instance = this.currentClosureObject, Definition = field, Type = field.Type },
                Source = new BoundExpression() { Definition = local, Type = local.Type },
                Type = fieldType,
              }
            });
            statements.Add(this.Rewrite(forEachStatement.Body));
            forEachStatement.Body = new BlockStatement() { Statements = statements };
          });
      } else
        base.RewriteChildren(forEachStatement);
    }

    /// <summary>
    /// Rewrites the children of the given generic method instance reference.
    /// </summary>
    public override void RewriteChildren(GenericMethodInstanceReference genericMethodInstanceReference) {
      this.RewriteChildren((MethodReference)genericMethodInstanceReference);
      genericMethodInstanceReference.GenericArguments = this.Rewrite(genericMethodInstanceReference.GenericArguments);
      //do not rewrite the generic method reference, it does not contain any references to generic method type parameters of this.method
      //but it might have referenes to generic method type parameters, which will confuse the code below.
    }

    /// <summary>
    /// Rewrites the given generic method parameter reference.
    /// </summary>
    public override ITypeReference Rewrite(IGenericMethodParameterReference genericMethodParameterReference) {
      if (this.genericMethodParameterMap != null && this.isInsideAnonymousMethod) {
        IGenericParameterReference referenceToSubstitute;
        if (this.genericMethodParameterMap.TryGetValue(genericMethodParameterReference.Index, out referenceToSubstitute))
          return referenceToSubstitute;
        Contract.Assume(false);
      }
      return base.Rewrite(genericMethodParameterReference);
    }

    /// <summary>
    /// Rewrites the children of the given local declaration statement.
    /// </summary>
    public override IStatement Rewrite(ILocalDeclarationStatement localDeclarationStatement) {
      var local = localDeclarationStatement.LocalVariable;
      if (this.fieldReferencesForUseInsideAnonymousMethods.ContainsKey(local)) {
        Contract.Assume(!this.isInsideAnonymousMethod); //only locals outside of delegates get captured as far as this rewriter is concerned.
        this.isInsideAnonymousMethod = true;
        var fieldType = this.Rewrite(this.copier.Copy(local.Type)); //get the type as the anon delegate will see it
        this.isInsideAnonymousMethod = false;
        this.CreateClosureField(local, fieldType, local.Type, localDeclarationStatement.LocalVariable.Name.Value);
        if (localDeclarationStatement.InitialValue == null) return new EmptyStatement();
        var field = this.fieldReferencesForUseInsideThisMethod[local];
        return new ExpressionStatement() {
          Expression = new Assignment() {
            Target = new TargetExpression() { Instance = this.currentClosureObject, Definition = field, Type = field.Type },
            Source = this.Rewrite(localDeclarationStatement.InitialValue),
            Type = fieldType,
          }
        };
      }
      return base.Rewrite(localDeclarationStatement);
    }

    /// <summary>
    /// Rewrites the children of the given target expression.
    /// </summary>
    public override void RewriteChildren(TargetExpression targetExpression) {
      IFieldReference closureField;
      var map = this.isInsideAnonymousMethod ? this.fieldReferencesForUseInsideAnonymousMethods : this.fieldReferencesForUseInsideThisMethod;
      if (map.TryGetValue(targetExpression.Definition, out closureField)) {
        targetExpression.Instance = this.GetClosureObjectInstanceContaining(closureField);
        targetExpression.Definition = closureField;
        targetExpression.Type = closureField.Type;
        return;
      }
      base.RewriteChildren(targetExpression);
    }

    /// <summary>
    /// Rewrites the given this reference expression.
    /// </summary>
    public override IExpression Rewrite(IThisReference thisReference) {
      if (this.isInsideAnonymousMethod) {
        IFieldReference thisField;
        if (this.fieldReferencesForUseInsideAnonymousMethods.TryGetValue(this.method, out thisField)) {
          return new BoundExpression() {
            Instance = this.GetClosureObjectInstanceContaining(thisField),
            Definition = thisField,
            Type = thisField.Type
          };
        }
      }
      return base.Rewrite(thisReference);
    }

  }

  /// <summary>
  /// A traverser that records all of the scopes that declare locals that have been captured by anonymous delegates.
  /// This runs as a second pass, after all of the captured locals have been found.
  /// </summary>
  internal class ScopesWithCapturedLocalsFinder : CodeTraverser {

    /// <summary>
    /// A traverser that records all of the blocks that declare locals that have been captured by anonymous delegates.
    /// This runs as a second pass, after all of the captured locals have been found.
    /// </summary>
    internal ScopesWithCapturedLocalsFinder(Dictionary<object, IFieldReference> captures) {
      Contract.Requires(captures != null);

      this.captures = captures;
    }

    internal Dictionary<object, bool> scopesWithCapturedLocals = new Dictionary<object, bool>();
    Dictionary<object, IFieldReference> captures;
    IBlockStatement/*?*/ currentBlock;

    [ContractInvariantMethod]
    private void ObjectInvariant() {
      Contract.Invariant(this.captures != null);
    }

    public override void TraverseChildren(IBlockExpression blockExpression) {
      var saved = this.currentBlock;
      this.currentBlock = blockExpression.BlockStatement;
      base.TraverseChildren(blockExpression.BlockStatement);
      this.Traverse(blockExpression.Expression);
      this.currentBlock = saved;
    }

    public override void TraverseChildren(IBlockStatement block) {
      var saved = this.currentBlock;
      this.currentBlock = block;
      base.TraverseChildren(block);
      this.currentBlock = saved;
    }

    public override void TraverseChildren(ICatchClause catchClause) {
      if (this.captures.ContainsKey(catchClause.ExceptionContainer))
        this.scopesWithCapturedLocals[catchClause] = true;
      base.TraverseChildren(catchClause);
    }

    public override void TraverseChildren(IForEachStatement forEachStatement) {
      if (this.captures.ContainsKey(forEachStatement.Variable))
        this.scopesWithCapturedLocals[forEachStatement] = true;
      base.TraverseChildren(forEachStatement);
    }

    public override void TraverseChildren(ILocalDeclarationStatement localDeclarationStatement) {
      if (this.captures.ContainsKey(localDeclarationStatement.LocalVariable))
        this.scopesWithCapturedLocals[this.currentBlock] = true;
      base.TraverseChildren(localDeclarationStatement);
    }

  }

  /// <summary>
  /// A traverser that records all of the parameters and locals that are captured by anoymous delegates and also
  /// records the anonymous delegates that do the capturing. Delegates that only capture the "this" argument
  /// will have an entry in anonymousDelegatesThatCaptureThis but no entry in anonymousDelegatesThatCaptureLocalsOrParameters.
  /// </summary>
  internal class CapturedParameterAndLocalFinder : CodeTraverser {

    /// <summary>
    /// A traverser that records all of the parameters and locals that are captured by anoymous delegates and also
    /// records the anonymous delegates that do the capturing. Delegates that only capture the "this" argument
    /// will have an entry in anonymousDelegatesThatCaptureThis but no entry in anonymousDelegatesThatCaptureLocalsOrParameters.
    /// </summary>
    internal CapturedParameterAndLocalFinder() { }

    internal Dictionary<object, IFieldReference> captures = new Dictionary<object, IFieldReference>();
    internal Dictionary<IAnonymousDelegate, bool> anonymousDelegatesThatCaptureThis = new Dictionary<IAnonymousDelegate, bool>();
    internal Dictionary<IAnonymousDelegate, bool> anonymousDelegatesThatCaptureLocalsOrParameters = new Dictionary<IAnonymousDelegate, bool>();

    IAnonymousDelegate/*?*/ currentAnonymousDelegate;
    Dictionary<object, bool> definitionsToIgnore = new Dictionary<object, bool>();

    public override void TraverseChildren(IAnonymousDelegate anonymousDelegate) {
      var saved = this.currentAnonymousDelegate;
      this.currentAnonymousDelegate = anonymousDelegate;
      foreach (var parameter in anonymousDelegate.Parameters)
        this.definitionsToIgnore[parameter] = true;
      base.TraverseChildren(anonymousDelegate);
      this.currentAnonymousDelegate = saved;
      if (saved != null) {
        if (this.anonymousDelegatesThatCaptureThis.ContainsKey(anonymousDelegate))
          this.anonymousDelegatesThatCaptureThis[saved] = true;
        if (this.anonymousDelegatesThatCaptureLocalsOrParameters.ContainsKey(anonymousDelegate))
          this.anonymousDelegatesThatCaptureLocalsOrParameters[saved] = true;
      }
    }

    public override void TraverseChildren(IAddressableExpression addressableExpression) {
      this.LookForCapturedDefinition(addressableExpression.Definition);
      base.TraverseChildren(addressableExpression);
    }

    public override void TraverseChildren(IBoundExpression boundExpression) {
      this.LookForCapturedDefinition(boundExpression.Definition);
      base.TraverseChildren(boundExpression);
    }

    public override void TraverseChildren(ICatchClause catchClause) {
      if (this.currentAnonymousDelegate != null)
        this.definitionsToIgnore[catchClause.ExceptionContainer] = true;
      base.TraverseChildren(catchClause);
    }

    public override void TraverseChildren(IForEachStatement forEachStatement) {
      if (this.currentAnonymousDelegate != null)
        this.definitionsToIgnore[forEachStatement.Variable] = true;
      base.TraverseChildren(forEachStatement);
    }

    public override void TraverseChildren(ILocalDeclarationStatement localDeclarationStatement) {
      if (this.currentAnonymousDelegate != null)
        this.definitionsToIgnore[localDeclarationStatement.LocalVariable] = true;
      base.TraverseChildren(localDeclarationStatement);
    }

    public override void TraverseChildren(ITargetExpression targetExpression) {
      this.LookForCapturedDefinition(targetExpression.Definition);
      base.TraverseChildren(targetExpression);
    }

    public override void TraverseChildren(IThisReference thisReference) {
      if (this.currentAnonymousDelegate != null)
        this.anonymousDelegatesThatCaptureThis[this.currentAnonymousDelegate] = true;
    }

    private void LookForCapturedDefinition(object definition) {
      if (this.currentAnonymousDelegate == null) return;
      if (this.definitionsToIgnore.ContainsKey(definition)) return;
      if (definition is IParameterDefinition || definition is ILocalDefinition) {
        this.captures[definition] = Dummy.FieldReference;
        this.anonymousDelegatesThatCaptureLocalsOrParameters[this.currentAnonymousDelegate] = true;
      }
    }

  }

}