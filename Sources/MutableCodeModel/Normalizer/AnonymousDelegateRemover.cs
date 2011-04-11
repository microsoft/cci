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
  public class AnonymousDelegateRemover : CodeRewriter {

    /// <summary>
    /// A class providing functionality to rewrite high level constructs such as anonymous delegates and yield statements
    /// into helper classes and methods, thus making it easier to generate IL from the CodeModel.
    /// </summary>
    /// <param name="host">An object representing the application that is hosting the converter. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.</param>
    /// <param name="sourceLocationProvider">An object that can map the ILocation objects found in a block of statements to IPrimarySourceLocation objects. May be null.</param>
    public AnonymousDelegateRemover(IMetadataHost host, ISourceLocationProvider/*?*/ sourceLocationProvider)
      : base(host) {
      this.sourceLocationProvider = sourceLocationProvider;
      var compilerGeneratedCtor = new Microsoft.Cci.MethodReference(host, host.PlatformType.SystemRuntimeCompilerServicesCompilerGeneratedAttribute,
         CallingConvention.HasThis, host.PlatformType.SystemVoid, host.NameTable.Ctor, 0);
      this.compilerGenerated = new CustomAttribute() { Constructor = compilerGeneratedCtor };
      this.objectCtor = new Microsoft.Cci.MethodReference(this.host, this.host.PlatformType.SystemObject, CallingConvention.HasThis,
         this.host.PlatformType.SystemVoid, this.host.NameTable.Ctor, 0);
    }

    internal List<ITypeDefinition> closureClasses;

    List<ITypeDefinitionMember> helperMembers;

    Dictionary<object, IFieldDefinition> captures;

    Dictionary<IAnonymousDelegate, bool> anonymousDelegatesThatCaptureThis = new Dictionary<IAnonymousDelegate, bool>();

    Dictionary<IAnonymousDelegate, bool> anonymousDelegatesThatCaptureLocalsOrParameters = new Dictionary<IAnonymousDelegate, bool>();

    /// <summary>
    /// An object that can map the ILocation objects found in a block of statements to IPrimarySourceLocation objects. May be null.
    /// </summary>
    ISourceLocationProvider/*?*/ sourceLocationProvider;

    //Dictionary<BlockStatement, NestedTypeDefinition> closureFor;

    NamedTypeDefinition currentClosure;

    IExpression currentClosureInstance;

    IFieldDefinition fieldForThis;

    ICustomAttribute compilerGenerated;

    int idCounter;

    //bool insideAnonymousDelegate;

    IMethodDefinition method;

    IMethodReference objectCtor;

    /// <summary>
    /// Given a method definition and a block of statements that represents the Block property of the body of the method,
    /// returns a SourceMethod with a body that no longer has any yield statements or anonymous delegate expressions.
    /// The given block of statements is mutated in place.
    /// </summary>
    /// <param name="method"></param>
    /// <param name="body">The tree rooted at body must be fully mutable and the nodes must not be shared with anything else.</param>
    public void RemoveAnonymousDelegates(IMethodDefinition method, BlockStatement body) {
      this.method = method;
      //find all of the parameters and locals that are captured by the anonymous delegates
      var finder = new CapturedParameterAndLocalFinder();
      finder.TraverseChildren(body);
      this.captures = finder.captures;
      this.anonymousDelegatesThatCaptureLocalsOrParameters = finder.anonymousDelegatesThatCaptureLocalsOrParameters;
      this.anonymousDelegatesThatCaptureThis = finder.anonymousDelegatesThatCaptureThis;
      finder = null;

      //if any parameters (including this) have been captured, generate a closure class and keep a map from param to closure field
      var topLevelClosureClass = this.GenerateTopLevelClosure(method);
      if (topLevelClosureClass != null) {
        this.currentClosure = topLevelClosureClass;
        //declare a local to keep the parameter closure class
        var closureType = NamedTypeDefinition.SelfInstance(this.currentClosure, this.host.InternFactory);
        var closureLocal = new LocalDefinition() { Type = closureType };
        this.currentClosureInstance = new BoundExpression() { Definition = closureLocal, Type = closureType };
        this.RewriteChildren(body); 
        //do this after rewriting so that parameter references are not rewritten into closure field references.
        this.InsertStatementsToAllocateAndInitializeTopLevelClosure(method, body, closureLocal);
      } else {
        this.RewriteChildren(body);
      }

      //for each block declaring a local that has been captured, generate a closure class and keep a map from local to closure field
      //for each closure class create a local that keeps the closure
      //when entering a scope with a closure, initialize the local
      //turn all reads/writes to captured locals/parameters to field reads/writes
      //move the bodies of anonymous delegates into methods of the most nested closure classes (these methods will not be normalized)
      //and turn anonymous delegate expressions into delegate creation expressions
    }

    private void InsertStatementsToAllocateAndInitializeTopLevelClosure(IMethodDefinition method, BlockStatement body, LocalDefinition closureLocal) {
      List<IStatement> initializerStatements = new List<IStatement>();
      var closureType = closureLocal.Type.ResolvedType;
      //initialize local with an instance of the closure class
      initializerStatements.Add(
        new LocalDeclarationStatement() {
          LocalVariable = closureLocal,
          InitialValue = new CreateObjectInstance() {
            MethodToCall = TypeHelper.GetMethod(closureType, this.host.NameTable.Ctor),
            Type = closureType
          }
        });
      //initialize the fields of the closure class with the initial parameter values
      foreach (var parameter in method.Parameters) {
        IFieldDefinition field;
        if (!this.captures.TryGetValue(parameter, out field)) continue;
        initializerStatements.Add(
          new ExpressionStatement() {
            Expression = new Assignment() {
              Target = new TargetExpression() { Instance = this.currentClosureInstance, Definition = field, Type = field.Type },
              Source = new BoundExpression() { Definition = parameter, Type = parameter.Type },
              Type = parameter.Type
            }
          });
      }
      //intialize the this argument field if that exists
      if (this.fieldForThis != null) {
        initializerStatements.Add(
          new ExpressionStatement() {
            Expression = new Assignment() {
              Target = new TargetExpression() { Instance = this.currentClosureInstance, Definition = this.fieldForThis, Type = this.fieldForThis.Type },
              Source = new ThisReference(),
              Type = this.fieldForThis.Type
            }
          });
      }
      body.Statements.InsertRange(0, initializerStatements);
    }

    private NestedTypeDefinition/*?*/ GenerateTopLevelClosure(IMethodDefinition method) {
      NestedTypeDefinition result = null;
      foreach (var parameter in method.Parameters) {
        if (!this.captures.ContainsKey(parameter)) continue;
        if (result == null) result = this.CreateClosureClass(method);
        var field = this.CreateClosureField(result, parameter.Type, parameter.Name.Value);
        this.captures[parameter] = field;
      }
      //TODO: run through parameters and look for captures
      if (this.anonymousDelegatesThatCaptureThis.Count > 0) {
        if (this.captures.Count == 0) {
          //The anonymous delegates have captured only "this", so they can become peer methods if method.ContainingType is mutable.
          var mutableContainingType = method.ContainingTypeDefinition as NamedTypeDefinition;
          if (mutableContainingType != null) { //TODO: introduce an interface that can be implemented by the AST classes as well.
            this.helperMembers = mutableContainingType.PrivateHelperMembers;
            this.currentClosure = mutableContainingType;
            this.currentClosureInstance = new ThisReference() { Type = NamedTypeDefinition.SelfInstance(mutableContainingType, this.host.InternFactory) };
            return null;
          }
        }
        if (result == null) result = this.CreateClosureClass(method);
        var thisTypeReference = NamedTypeDefinition.SelfInstance((INamedTypeDefinition)method.ContainingType, this.host.InternFactory);
        this.fieldForThis = this.CreateClosureField(result, thisTypeReference, "<>"+this.idCounter+++"__this");
      } else {
        if (this.captures.Count == 0) {
          //None of the anonymous delegates have captured anything. They become static peer methods if method.ContainingType is mutable.
          var mutableContainingType = method.ContainingTypeDefinition as NamedTypeDefinition;
          if (mutableContainingType != null) { //TODO: introduce an interface that can be implemented by the AST classes as well.
            this.helperMembers = mutableContainingType.PrivateHelperMembers;
            this.currentClosure = mutableContainingType;
            return null;
          }
        }
      }
      return result;
    }

    private FieldDefinition CreateClosureField(NestedTypeDefinition closure, ITypeReference fieldType, string name) {
      FieldDefinition field = new FieldDefinition() {
        ContainingTypeDefinition = closure,
        InternFactory = this.host.InternFactory,
        Name = this.host.NameTable.GetNameFor(name),
        Type = fieldType,
        Visibility = TypeMemberVisibility.Public
      };
      closure.Fields.Add(field);
      return field;
    }

    private NestedTypeDefinition CreateClosureClass(IMethodDefinition method) {
      NestedTypeDefinition result = new NestedTypeDefinition();
      var containingType = method.ContainingTypeDefinition; //TODO: worry about closure chains
      if (this.closureClasses == null) this.closureClasses = new List<ITypeDefinition>();
      this.closureClasses.Add(result);
      result.Name = this.host.NameTable.GetNameFor("<>c__DisplayClass"+this.closureClasses.Count);
      result.Attributes.Add(this.compilerGenerated);
      result.BaseClasses.Add(this.host.PlatformType.SystemObject);
      result.ContainingTypeDefinition = containingType;
      result.InternFactory = this.host.InternFactory;
      result.IsBeforeFieldInit = true;
      result.IsClass = true;
      result.IsSealed = true;
      result.Layout = LayoutKind.Auto;
      result.StringFormat = StringFormatKind.Ansi;
      result.Visibility = TypeMemberVisibility.Private;

      this.InjectDefaultConstructor(result);


      
    //  string signature = MemberHelper.GetMethodSignature(this.method, NameFormattingOptions.Signature | NameFormattingOptions.ReturnType | NameFormattingOptions.TypeParameters);
    //  result.Attributes.Add(compilerGeneratedAttribute);

    //  //BoundField/*?*/ capturedThis;
    //  //var thisTypeReference = TypeDefinition.SelfInstance(this.method.ContainingTypeDefinition, this.host.InternFactory);
    //  //if (this.closureLocals.Count == 0 && this.FieldForCapturedLocalOrParameter.TryGetValue(thisTypeReference.InternedKey, out capturedThis)) {
    //  //  result.Fields.Add(capturedThis.Field);
    //  //  capturedThis.Field.ContainingTypeDefinition = result;
    //  //  capturedThis.Field.Type = this.Visit(capturedThis.Field.Type);
    //  //}

    //  if (makeGeneric) {
    //    List<IGenericMethodParameter> genericMethodParameters = new List<IGenericMethodParameter>();
    //    ushort count = 0;
    //    if (this.method.IsGeneric) {
    //      foreach (var genericMethodParameter in this.method.GenericParameters) {
    //        genericMethodParameters.Add(genericMethodParameter);
    //        GenericTypeParameter newTypeParam = new GenericTypeParameter() {
    //          Name = this.host.NameTable.GetNameFor(genericMethodParameter.Name.Value + "_"),
    //          Index = (count++),
    //          InternFactory = this.host.InternFactory,
    //          PlatformType = this.host.PlatformType,
    //        };
    //        this.genericTypeParameterMapping[genericMethodParameter.InternedKey] = newTypeParam;
    //        newTypeParam.DefiningType = result;
    //        result.GenericParameters.Add(newTypeParam);
    //      }
    //    }
    //    this.copyTypeToClosure = new CopyTypeFromIteratorToClosure(this.host, genericTypeParameterMapping);
    //    if (this.method.IsGeneric) {
    //      // Duplicate Constraints
    //      foreach (var genericMethodParameter in genericMethodParameters) {
    //        GenericTypeParameter correspondingTypeParameter = (GenericTypeParameter)this.genericTypeParameterMapping[genericMethodParameter.InternedKey];
    //        if (genericMethodParameter.Constraints != null) {
    //          correspondingTypeParameter.Constraints = new List<ITypeReference>();
    //          foreach (ITypeReference t in genericMethodParameter.Constraints) {
    //            correspondingTypeParameter.Constraints.Add(copyTypeToClosure.Visit(t));
    //          }
    //        }
    //      }
    //    }
    //  }

    //  this.generatedclosureClass = result;
    //  classList.Add(result);
      return result;
    }

    private IFieldDefinition CreateStaticCacheField(ITypeReference fieldType) {
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
      return field;
    }

    /// <summary>
    /// Returns a reference to the closure method. If the method is generic, the reference is to an instantiation, using
    /// the generic parameters of the current class as arguments.
    /// </summary>
    /// <param name="anonymousDelegate"></param>
    /// <returns></returns>
    private IMethodReference CreateClosureMethod(AnonymousDelegate anonymousDelegate) {
      bool isPeerMethod = !this.anonymousDelegatesThatCaptureLocalsOrParameters.ContainsKey(anonymousDelegate);
      bool isStaticMethod = isPeerMethod && !this.anonymousDelegatesThatCaptureThis.ContainsKey(anonymousDelegate);
      var body = new SourceMethodBody(this.host, this.sourceLocationProvider) {
        Block = anonymousDelegate.Body,
        LocalsAreZeroed = true
      };
      var method = new MethodDefinition() {
        ContainingTypeDefinition = this.currentClosure,
        Name = this.host.NameTable.GetNameFor("<"+this.method.Name.Value+">b__"+this.idCounter++),
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
      foreach (ParameterDefinition parameterDefinition in method.Parameters)
        parameterDefinition.ContainingSignature = method;

      if (isPeerMethod) {
        this.helperMembers.Add(method);
        if (isStaticMethod) {
          method.Attributes = new List<ICustomAttribute>(1);
          method.Attributes.Add(this.compilerGenerated);
        }
      } else {
        this.currentClosure.Methods.Add(method);
        //need to rewrite body (if not a peer)
      }

      //need to return an instantiation (if generic)

      return method;
    }

    private void InjectDefaultConstructor(NestedTypeDefinition closureClass) {
      var block = new BlockStatement();
      block.Statements.Add(
        new ExpressionStatement() {
          Expression = new MethodCall() {
            ThisArgument = new ThisReference(),
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
        ContainingTypeDefinition = closureClass,
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

      closureClass.Methods.Add(defaultConstructor);

    }

    /// <summary>
    /// Rewrites the given anonymous delegate expression.
    /// </summary>
    public override IExpression Rewrite(IAnonymousDelegate anonymousDelegate) {
      IMethodReference method = this.CreateClosureMethod((AnonymousDelegate)anonymousDelegate);
      var createDelegate = new CreateDelegateInstance() {
        MethodToCallViaDelegate = method,
        Type = anonymousDelegate.Type
      };
      if ((method.CallingConvention & CallingConvention.HasThis) != 0)
        createDelegate.Instance = this.currentClosureInstance;
      else {
        //cache the delegate in a static field
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
    /// Rewrites the children of the given addressable expression.
    /// </summary>
    public override void RewriteChildren(AddressableExpression addressableExpression) {
      IFieldDefinition closureField;
      if (this.captures.TryGetValue(addressableExpression.Definition, out closureField)) {
        addressableExpression.Instance = this.currentClosureInstance; //TODO: closure chains
        addressableExpression.Definition = closureField;
        addressableExpression.Type = this.Rewrite(addressableExpression.Type);
        return;
      }
      base.RewriteChildren(addressableExpression);
    }

    /// <summary>
    /// Rewrites the children of the given bound expression.
    /// </summary>
    public override void RewriteChildren(BoundExpression boundExpression) {
      IFieldDefinition closureField;
      if (this.captures.TryGetValue(boundExpression.Definition, out closureField)) {
        boundExpression.Instance = this.currentClosureInstance; //TODO: closure chains
        boundExpression.Definition = closureField;
        boundExpression.Type = this.Rewrite(boundExpression.Type);
        return;
      }
      base.RewriteChildren(boundExpression);
    }

    /// <summary>
    /// Rewrites the children of the given target expression.
    /// </summary>
    public override void RewriteChildren(TargetExpression targetExpression) {
      IFieldDefinition closureField;
      if (this.captures.TryGetValue(targetExpression.Definition, out closureField)) {
        targetExpression.Instance = this.currentClosureInstance; //TODO: closure chains
        targetExpression.Definition = closureField;
        targetExpression.Type = this.Rewrite(targetExpression.Type);
        return;
      }
      base.RewriteChildren(targetExpression);
    }

    /// <summary>
    /// Rewrites the given this reference expression.
    /// </summary>
    public override IExpression Rewrite(IThisReference thisReference) {
      if (this.fieldForThis != null) {
        return new BoundExpression() {
          Instance = this.currentClosureInstance,
          Definition = this.fieldForThis,
          Type = this.fieldForThis.Type
        };
      }
      return base.Rewrite(thisReference);
    }

  }

  internal class CapturedParameterAndLocalFinder : CodeTraverser {
    internal Dictionary<object, IFieldDefinition> captures = new Dictionary<object, IFieldDefinition>();
    internal Dictionary<IAnonymousDelegate, bool> anonymousDelegatesThatCaptureThis = new Dictionary<IAnonymousDelegate, bool>();
    internal Dictionary<IAnonymousDelegate, bool> anonymousDelegatesThatCaptureLocalsOrParameters = new Dictionary<IAnonymousDelegate, bool>();

    IAnonymousDelegate/*?*/ currentAnonymousDelegate;
    Dictionary<object, bool> definitionsToIgnore = new Dictionary<object, bool>();

    public override void TraverseChildren(IAnonymousDelegate anonymousDelegate) {
      var saved = this.currentAnonymousDelegate;
      this.currentAnonymousDelegate = anonymousDelegate;
      base.TraverseChildren(anonymousDelegate);
      this.currentAnonymousDelegate = saved;
    }

    public override void TraverseChildren(IAddressableExpression addressableExpression) {
      this.LookForCapturedDefinition(addressableExpression.Definition);
      base.TraverseChildren(addressableExpression);
    }

    public override void TraverseChildren(IBoundExpression boundExpression) {
      this.LookForCapturedDefinition(boundExpression.Definition);
      base.TraverseChildren(boundExpression);
    }

    public override void TraverseChildren(ILocalDeclarationStatement localDeclarationStatement) {
      if (this.currentAnonymousDelegate != null)
        this.definitionsToIgnore[localDeclarationStatement.LocalVariable] = true;
      base.TraverseChildren(localDeclarationStatement);
    }

    public override void TraverseChildren(IParameterDefinition parameterDefinition) {
      if (this.currentAnonymousDelegate != null)
        this.definitionsToIgnore[parameterDefinition] = true;
      base.TraverseChildren(parameterDefinition);
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
        this.captures[definition] = Dummy.Field;
        this.anonymousDelegatesThatCaptureLocalsOrParameters[this.currentAnonymousDelegate] = true;
      }
    }

  }

}