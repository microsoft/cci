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

    NamedTypeDefinition currentClosure;

    IExpression currentClosureInstance;

    List<IExpression> closureLocalInstances;

    ILocalDefinition currentClosureLocal;

    IFieldDefinition fieldForThis;

    ICustomAttribute compilerGenerated;

    int idCounter;

    bool isInsideAnonymousMethod;

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
        var closureLocal = new LocalDefinition() { Type = closureType, Name = this.host.NameTable.GetNameFor("CS$<>8__locals"+this.closureClasses.Count) };
        this.currentClosureLocal = closureLocal;
        this.currentClosureInstance = new BoundExpression() { Definition = closureLocal, Type = closureType };
        this.closureLocalInstances = new List<IExpression>();
        this.closureLocalInstances.Add(this.currentClosureInstance);
        this.RewriteChildren(body);
        //do this after rewriting so that parameter references are not rewritten into closure field references.
        this.InsertStatementsToAllocateAndInitializeTopLevelClosure(method, body, closureLocal);
      } else {
        this.RewriteChildren(body);
      }
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
      if (this.closureClasses == null) this.closureClasses = new List<ITypeDefinition>();
      NestedTypeDefinition closure = new NestedTypeDefinition();
      var containingType = method.ContainingTypeDefinition;
      closure.Name = this.host.NameTable.GetNameFor("<>c__DisplayClass"+this.closureClasses.Count);
      closure.Attributes.Add(this.compilerGenerated);
      closure.BaseClasses.Add(this.host.PlatformType.SystemObject);
      closure.ContainingTypeDefinition = containingType;
      closure.InternFactory = this.host.InternFactory;
      closure.IsBeforeFieldInit = true;
      closure.IsClass = true;
      closure.IsSealed = true;
      closure.Layout = LayoutKind.Auto;
      closure.StringFormat = StringFormatKind.Ansi;
      closure.Visibility = TypeMemberVisibility.Private;

      this.InjectDefaultConstructor(closure);

      this.closureClasses.Add(closure);
      return closure;
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
        if (this.method.IsGeneric) this.MakeDelegateMethodGeneric(method);
        method.Attributes = new List<ICustomAttribute>(1);
        method.Attributes.Add(this.compilerGenerated);
      } else {
        this.currentClosure.Methods.Add(method);
        this.isInsideAnonymousMethod = true;
        method.Body = this.Rewrite(method.Body);
        this.isInsideAnonymousMethod = false;
      }

      if (method.IsGeneric) {
        return new GenericMethodInstanceReference() {
          CallingConvention = method.CallingConvention,
          ContainingType = method.ContainingTypeDefinition,
          GenericArguments = new List<ITypeReference>(IteratorHelper.GetConversionEnumerable<IGenericMethodParameter, ITypeReference>(method.GenericParameters)),
          GenericMethod = method,
          InternFactory = this.host.InternFactory,
          Name = method.Name,
          Parameters = new List<IParameterTypeInformation>(((IMethodReference)method).Parameters),
          Type = method.Type,
        };
      }

      //need to return an instantiation (if generic)

      return method;
    }

    private IExpression GetClosureInstanceContaining(IFieldDefinition closureField) {
      if (this.isInsideAnonymousMethod) {
        IExpression result = new ThisReference() { Type = this.currentClosureLocal.Type };
        while (!TypeHelper.TypesAreEquivalent(result.Type, closureField.ContainingType)) {
          var outerClosureField = this.captures[result.Type];
          result = new BoundExpression() { Instance = result, Definition = outerClosureField, Type = outerClosureField.Type };
        }
        return result;
      } else {
        foreach (var instance in this.closureLocalInstances) {
          if (TypeHelper.TypesAreEquivalent(instance.Type, closureField.Type)) return instance;
        }
        return this.currentClosureInstance;
      }
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

    private void MakeDelegateMethodGeneric(MethodDefinition delegateMethod) {
      delegateMethod.CallingConvention |= CallingConvention.Generic;
      Dictionary<ushort, IGenericParameterReference> parameterMap = new Dictionary<ushort, IGenericParameterReference>();
      delegateMethod.GenericParameters = new List<IGenericMethodParameter>(this.method.GenericParameterCount);
      foreach (var genericParameter in this.method.GenericParameters) {
        var delPar = new GenericMethodParameter();
        delPar.Copy(genericParameter, this.host.InternFactory);
        delPar.DefiningMethod = delegateMethod;
        delegateMethod.GenericParameters.Add(delPar);
        parameterMap.Add(genericParameter.Index, delPar);
      }
      new GenericParameterRewriter(this.host, parameterMap).RewriteChildren(delegateMethod);
    }

    class GenericParameterRewriter : CodeRewriter {
      internal GenericParameterRewriter(IMetadataHost host, Dictionary<ushort, IGenericParameterReference> parameterMap)
        : base(host) {
        this.parameterMap = parameterMap;
      }

      Dictionary<ushort, IGenericParameterReference> parameterMap;

      public override ITypeReference Rewrite(IGenericMethodParameterReference genericMethodParameterReference) {
        IGenericParameterReference referenceToSubstitute;
        if (this.parameterMap.TryGetValue(genericMethodParameterReference.Index, out referenceToSubstitute))
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
      if ((method.CallingConvention & CallingConvention.HasThis) != 0 || (method.CallingConvention & CallingConvention.Generic) != 0)
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
        addressableExpression.Instance = this.GetClosureInstanceContaining(closureField);
        addressableExpression.Definition = closureField;
        addressableExpression.Type = this.Rewrite(addressableExpression.Type);
        return;
      }
      base.RewriteChildren(addressableExpression);
    }

    /// <summary>
    /// Rewrites the children of the given statement block.
    /// </summary>
    public override void RewriteChildren(BlockStatement block) {
      if (this.captures.ContainsKey(block)) {
        var savedCurrentClosure = this.currentClosure;
        var savedCurrentClosureInstance = this.currentClosureInstance;
        var savedCurrentClosureLocal = this.currentClosureLocal;
        this.currentClosure = this.CreateClosureClass(this.method);
        IFieldDefinition outerClosure = null;
        if (savedCurrentClosureLocal != null) {
          outerClosure = this.CreateClosureField((NestedTypeDefinition)this.currentClosure, savedCurrentClosureLocal.Type, savedCurrentClosureLocal.Name.Value);
          this.captures[this.currentClosure] = outerClosure;
        }

        var closureType = NamedTypeDefinition.SelfInstance(this.currentClosure, this.host.InternFactory);
        var closureLocal = new LocalDefinition() { Type = closureType, Name = this.host.NameTable.GetNameFor("CS$<>8__locals"+this.closureClasses.Count) };
        this.currentClosureInstance = new BoundExpression() { Definition = closureLocal, Type = closureType };
        this.currentClosureLocal = closureLocal;
        if (this.closureLocalInstances == null) this.closureLocalInstances = new List<IExpression>();
        this.closureLocalInstances.Add(this.currentClosureInstance);
        base.RewriteChildren(block);
        block.Statements.Insert(0, new ExpressionStatement() {
          Expression = new Assignment() {
            Target = new TargetExpression() { Definition = closureLocal, Type = closureLocal.Type },
            Source = new CreateObjectInstance() {
              MethodToCall = TypeHelper.GetMethod(closureType, this.host.NameTable.Ctor),
              Type = closureType
            }
          }
        });
        if (savedCurrentClosureLocal != null) {
          block.Statements.Insert(1, new ExpressionStatement() {
            Expression = new Assignment() {
              Target = new TargetExpression() { Instance = new BoundExpression() { Definition = closureLocal }, Definition = outerClosure },
              Source = new BoundExpression() { Definition = savedCurrentClosureLocal }
            }
          });
        }
        this.currentClosure = savedCurrentClosure;
        this.currentClosureInstance = savedCurrentClosureInstance;
        this.currentClosureLocal = savedCurrentClosureLocal;
      } else
        base.RewriteChildren(block);
    }

    /// <summary>
    /// Rewrites the children of the given bound expression.
    /// </summary>
    public override void RewriteChildren(BoundExpression boundExpression) {
      IFieldDefinition closureField;
      if (this.captures.TryGetValue(boundExpression.Definition, out closureField)) {
        boundExpression.Instance = this.GetClosureInstanceContaining(closureField);
        boundExpression.Definition = closureField;
        boundExpression.Type = this.Rewrite(boundExpression.Type);
        return;
      }
      base.RewriteChildren(boundExpression);
    }

    /// <summary>
    /// Rewrites the children of the given local declaration statement.
    /// </summary>
    public override IStatement Rewrite(ILocalDeclarationStatement localDeclarationStatement) {
      var local = localDeclarationStatement.LocalVariable;
      if (this.captures.ContainsKey(local)) {
        var field = this.CreateClosureField((NestedTypeDefinition)this.currentClosure, local.Type, localDeclarationStatement.LocalVariable.Name.Value);
        this.captures[local] = field;
        if (localDeclarationStatement.InitialValue == null) return new EmptyStatement();
        return new ExpressionStatement() {
          Expression = new Assignment() {
            Target = new TargetExpression() { Instance = this.currentClosureInstance, Definition = field, Type = local.Type },
            Source = this.Rewrite(localDeclarationStatement.InitialValue)
          }
        };
      }
      return base.Rewrite(localDeclarationStatement);
    }

    /// <summary>
    /// Rewrites the children of the given target expression.
    /// </summary>
    public override void RewriteChildren(TargetExpression targetExpression) {
      IFieldDefinition closureField;
      if (this.captures.TryGetValue(targetExpression.Definition, out closureField)) {
        targetExpression.Instance = this.GetClosureInstanceContaining(closureField);
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
    IBlockStatement/*?*/ currentBlock;
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

    public override void TraverseChildren(IBlockStatement block) {
      var saved = this.currentBlock;
      this.currentBlock = block;
      base.TraverseChildren(block);
      this.currentBlock = saved;
    }

    public override void TraverseChildren(IBoundExpression boundExpression) {
      this.LookForCapturedDefinition(boundExpression.Definition);
      base.TraverseChildren(boundExpression);
    }

    public override void TraverseChildren(ILocalDeclarationStatement localDeclarationStatement) {
      if (this.currentAnonymousDelegate != null)
        this.definitionsToIgnore[localDeclarationStatement.LocalVariable] = true;
      else
        this.captures[this.currentBlock] = Dummy.Field;
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