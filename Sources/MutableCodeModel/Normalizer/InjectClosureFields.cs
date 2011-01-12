//---------------------------------------------------------
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


namespace Microsoft.Cci.MutableCodeModel {
  /// <summary>
  /// A mutator that substitutes the appropriate closure fields (i.e., fields defined
  /// in private nested types that implement a closure) for occurrences of parameters,
  /// and locals. It is used on anonymous delegate bodies as well as the body of the
  /// original method that contained the anonymous delegate(s) since it also needs to
  /// use the closure fields.
  /// NOTE: It must be instantiated for each body it visits, either an anonymous
  /// delegate or the original method body.
  /// </summary>  
  internal class InjectClosureFields : CodeMutator {

    #region Fields

    /// <summary>
    /// The method whose body this mutator is visiting.
    /// </summary>
    IMethodDefinition method;
    Dictionary<object, BoundField>/*!*/ fieldForCapturedLocalOrParameter;
    /// <summary>
    /// When this is being used on the body of an anonymous delegate, this
    /// is the closure class the method implementing the anonymous delegate
    /// is contained in.
    /// When this is being used on the original method body, this is the
    /// containing type definition of the original method.
    /// </summary>
    ITypeDefinition currentClass;
    /// <summary>
    /// Non-null when currentClass has a nested closure class defined
    /// within it. Of course in that case, this *is* that nested closure class.
    /// I.e., it is null only for the innermost nested closure class.
    /// </summary>
    NestedTypeDefinition/*?*/ nestedClosure;
    /// <summary>
    /// Non-empty when this is being used on the body of a nested anonymous
    /// delegate (i.e., not on an outermost anonymous delegate or the original
    /// method body).
    /// </summary>
    List<FieldDefinition> outerClosures;
    Dictionary<uint, IGenericTypeParameter> genericTypeParameterMapping;
    /// <summary>
    /// When non-null a local that is used to reference fields in the next closure class "down"
    /// from the method that is currently being mutated.
    /// </summary>
    LocalDefinition/*?*/ closureLocal;

    /// <summary>
    /// The map used to get from an anonymous delegate encountered during mutation to the method
    /// in the closure class that implements the anonymous delegate.
    /// </summary>
    readonly Dictionary<IAnonymousDelegate, MethodDefinition> lambda2method;
    /// <summary>
    /// A map to hold generated constructors.
    /// </summary>
    readonly Dictionary<NestedTypeDefinition, MethodDefinition> constructorForClosureClass;

    /// <summary>
    /// The list of classes used by the closure classes. I.e., the first element is the containing class of
    /// the original method that contains the lambdas. The rest are the nested closure classes created by this
    /// visitor as it walks the original method body. Due to the order of the visit, the list is meant to be in
    /// the order from outermost class to innermost.
    /// I.e., (forall i : 0 &lt; i &lt; closureClassList.Length : closureClassList[i].ContainingTypeDefinition == closureClassList[i-1])
    /// </summary>
    List<ITypeDefinition> classList;
    int nestingDepth;

    #endregion

    /// <summary>
    /// Allocates a mutator that substitutes the appropriate closure fields (i.e., fields defined
    /// in private nested types that implement a closure) for occurrences of parameters,
    /// and locals. It is used on anonymous delegate bodies as well as the body of the
    /// original method that contained the anonymous delegate(s) since it also needs to
    /// use the closure fields.
    /// NOTE: It must be instantiated for each body it visits, either an anonymous
    /// delegate or the original method body.
    /// </summary>
    /// <param name="method">The method whose body is to be mutated into the body of an anonymous delegate.</param>
    /// <param name="fieldForCapturedLocalOrParameter">A map from captured locals and parameters to the closure class fields that hold their state for the method
    /// corresponding to the anonymous delegate.</param>
    /// <param name="classList">
    /// The list of classes used by the closure classes. I.e., the first element is the containing class of
    /// the original method that contains the lambdas. The rest are the nested closure classes created by this
    /// visitor as it walks the original method body. Due to the order of the visit, the list is meant to be in
    /// the order from outermost class to innermost.
    /// I.e., (forall i : 0 &lt; i &lt; closureClassList.Length : closureClassList[i].ContainingTypeDefinition == closureClassList[i-1])
    /// </param>
    /// <param name="index">
    /// The index into <paramref name="classList"/> of the containing type of the method whose body is going to be mutated
    /// by this muatator.
    /// </param>
    /// <param name="outerClosures">A potentially empty list of closures that for any anonymous delegates that enclose the anonymous delegate that will be
    /// traversed by this mutator.</param>
    /// <param name="host">An object representing the application that is hosting this mutator. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.</param>
    /// <param name="sourceLocationProvider">An object that can map the ILocation objects found in a block of statements to IPrimarySourceLocation objects. May be null.</param>
    /// <param name="genericTypeParameterMapping"></param>
    /// <param name="lambda2method">The map used to get from an anonymous delegate encountered during mutation to the method
    /// in the closure class that implements the anonymous delegate.</param>
    /// <param name="constructorForClosureClass">
    /// A map shared by all instances of this class that provides the constructor of the closure class so that method calls
    /// to it can be generated in methods in the enclosing class. The map starts out empty at the outermost call to this class
    /// and each time a ctor is generated, it is added to the map.
    /// </param>
    private InjectClosureFields(
      IMethodDefinition method,
      Dictionary<object, BoundField> fieldForCapturedLocalOrParameter,
      List<ITypeDefinition> classList, int index,
      List<FieldDefinition> outerClosures, IMetadataHost host, ISourceLocationProvider/*?*/ sourceLocationProvider,
      Dictionary<uint, IGenericTypeParameter> genericTypeParameterMapping,
      Dictionary<IAnonymousDelegate, MethodDefinition> lambda2method,
      Dictionary<NestedTypeDefinition, MethodDefinition> constructorForClosureClass
      )
      : base(host, true, sourceLocationProvider) {
      this.method = method;
      this.classList = classList;
      this.nestingDepth = index;
      this.fieldForCapturedLocalOrParameter = fieldForCapturedLocalOrParameter;
      this.currentClass = classList[index];
      if (index < classList.Count - 1) {
        this.nestedClosure = (NestedTypeDefinition)classList[index + 1];
        this.closureLocal = this.CreateClosureLocal(method, this.nestedClosure);
      }
      this.outerClosures = outerClosures;
      this.genericTypeParameterMapping = genericTypeParameterMapping;
      this.lambda2method = lambda2method;
      this.constructorForClosureClass = constructorForClosureClass;
    }

    #region Helper Methods

    /// <summary>
    /// Given a closure type that we created, return an expression that is of the form this[.f]*. The [.f] repeats 
    /// more than zero times if the closure is embedded. 
    /// </summary>
    /// <param name="closure">The closure class</param>
    /// <returns></returns>
    private IExpression ClosureInstanceFor(NestedTypeDefinition closure) {
      // Case 1. A reference to "this", i.e., the current class
      ThisReference thisRef = new ThisReference();
      thisRef.Type = this.GetSelfReferenceForPrivateHelperTypes(closure);
      if (closure.InternedKey == this.currentClass.InternedKey) {
        return thisRef;
      }
      // Case 2. A reference "down" to the next closure class in the chain.
      // The reference is always through the local variable that each method
      // has holding an instance of the next class in the chain.
      NestedTypeDefinition nestedClosure = closure as NestedTypeDefinition;
      if (nestedClosure != null && nestedClosure.ContainingTypeDefinition == this.currentClass) {
        return new BoundExpression() {
          Definition = this.closureLocal,
          Instance = null,
          Type = this.closureLocal.Type,
        };
      }
      // Case 3. A reference "up" to some class "closure" is nested within.
      // Return a chain of this.f.g.h where each field points to the next
      // outer class.
      var boundExpression = new BoundExpression();
      boundExpression.Instance = thisRef;
      // Need to possibly instantiate "closure" because outer closure fields
      // will be generic *if* (and only if) outermost closure class is generic.
      var k = this.GetSelfReferenceForPrivateHelperTypes(closure).InternedKey;
      for (int i = this.outerClosures.Count - 1; 0 <= i; i--) {
        var closureField = this.outerClosures[i];
        boundExpression.Definition = this.GetSelfReference(closureField);
        if (closureField.Type.InternedKey == k) {
          boundExpression.Type = this.GetSelfReferenceForPrivateHelperTypes(closure);
          return boundExpression;
        }
        var be = new BoundExpression();
        be.Instance = boundExpression;
        boundExpression = be;
      }
      Debug.Assert(false);
      return CodeDummy.Expression;
    }

    private BoundExpression/*?*/ closureInstanceFor;
    private LocalDefinition CreateClosureLocal(IMethodDefinition methodDefinition, NestedTypeDefinition closureClass) {
      LocalDefinition result = new LocalDefinition();
      var boundExpression = new BoundExpression();
      boundExpression.Definition = result;
      this.closureInstanceFor = boundExpression;
      result.Name = this.host.NameTable.GetNameFor("__closure");
      result.MethodDefinition = methodDefinition;
      result.Type = this.GetSelfReferenceForPrivateHelperTypes(nestedClosure);
      return result;
    }

    /// <summary>
    /// Given a private helper type, that is, a closure type generated for compiling anonymous delegate or an iterator
    /// method, generate a type reference that can be used whether it is generic or not.
    /// </summary>
    private ITypeReference GetSelfReferenceForPrivateHelperTypes(INestedTypeDefinition privateHelperType) {
      //^ requires privateHelperType is INestedTypeDefinition;
      var result = TypeDefinition.SelfInstance(privateHelperType, this.host.InternFactory);
      if (this.method.IsGeneric) {
        var t = new SpecializedNestedTypeReference() {
          ContainingType = TypeDefinition.SelfInstance(privateHelperType.ContainingTypeDefinition, this.host.InternFactory),
          GenericParameterCount = privateHelperType.GenericParameterCount,
          MangleName = true,
          Name = privateHelperType.Name,
          UnspecializedVersion = privateHelperType,
        };
        var gas = IteratorHelper.GetConversionEnumerable<IGenericMethodParameter, ITypeReference>(this.method.GenericParameters);
        result = GenericTypeInstance.GetGenericTypeInstance(t, gas, this.host.InternFactory);
      }
      return result;
    }

    /// <summary>
    /// Get a reference to a field of a private helper type of which resolution is possible. 
    /// </summary>
    /// <param name="fieldDefinition"></param>
    /// <returns></returns>
    private IFieldReference GetSelfReference(IFieldDefinition fieldDefinition) {
      var ntd = fieldDefinition.ContainingTypeDefinition as NestedTypeDefinition;
      if (ntd == null)
        return fieldDefinition;
      var containingType = this.GetSelfReferenceForPrivateHelperTypes(ntd);
      if (containingType is IGenericTypeInstanceReference || containingType is ISpecializedNestedTypeReference) {
        var sFieldReference = new SpecializedFieldReference();
        ((FieldReference)sFieldReference).Copy(fieldDefinition, this.host.InternFactory);
        sFieldReference.ContainingType = containingType;
        sFieldReference.UnspecializedVersion = fieldDefinition;
        return sFieldReference;
      } else {
        return fieldDefinition;
      }
    }

    /// <summary>
    /// Get a method reference to a method definition. Either the definition is in a private helper type
    /// (and is either the constructor or else the implementation of an anonymous delegate), or else
    /// it is a static method defined in the same type as the method that contains the anonymous delegate.
    /// The returned reference is to be used in the body of the method containing the anonymous delegate.
    /// </summary>
    /// <param name="methodDefinition"></param>
    /// <returns></returns>
    private IMethodReference GetSelfReference(IMethodDefinition methodDefinition) {
      ITypeReference containingType;
      INamespaceTypeDefinition ntd = methodDefinition.ContainingType as INamespaceTypeDefinition;
      if (ntd != null)
        containingType = TypeDefinition.SelfInstance(ntd, this.host.InternFactory);
      else
        containingType = this.GetSelfReferenceForPrivateHelperTypes((INestedTypeDefinition)methodDefinition.ContainingTypeDefinition);
      if (methodDefinition.IsGeneric || containingType is IGenericTypeInstanceReference || containingType is ISpecializedNestedTypeReference) {
        var result = new SpecializedMethodReference();
        ((MethodReference)result).Copy(methodDefinition, this.host.InternFactory);
        result.UnspecializedVersion = methodDefinition;
        result.ContainingType = containingType;
        return result;
      }
      var result1 = new MethodReference();
      result1.Copy(methodDefinition, this.host.InternFactory);
      result1.ContainingType = containingType;
      return result1;
    }

    private IMethodBody GetMethodBodyFrom(IBlockStatement block, IMethodDefinition method, Dictionary<uint, IGenericTypeParameter> genericTypeParameterMapping) {
      var closureClass = (NestedTypeDefinition)method.ContainingTypeDefinition; // cast succeeds because method is *always* defined in a closure class
      var nestingDepth = this.classList.IndexOf(closureClass);
      Debug.Assert(0 <= nestingDepth);

      var bodyFixer = new InjectClosureFields(method, this.fieldForCapturedLocalOrParameter,
        this.classList, nestingDepth,
        this.outerClosures,
        this.host, this.sourceLocationProvider, genericTypeParameterMapping,
        this.lambda2method,
        this.constructorForClosureClass
        );
      block = bodyFixer.Visit(block);

      // For all methods except for the one in the innermost closure class,
      // inject a preamble that creates an instance of the next inner closure
      // class and stores the instance in a local variable. The preamble also
      // stores any parameters of the method that have been captured in that instance:
      //   local := new NestedClosure();
      //   local.p := p; // for each parameter p that is captured
      // NOTE: Can't make this the override for Visit(IBlockStatement) because
      // if the decompiler isn't perfect --- and who is? --- then there may be
      // nested blocks, but this should be done only for the top-level block.
      if (bodyFixer.nestedClosure != null) {
        var result2 = block as BlockStatement;
        if (result2 != null) { // result2 is just an alias for result
          var inits = new List<IStatement>();
          var s = bodyFixer.ConstructClosureInstance(bodyFixer.closureLocal, bodyFixer.nestedClosure);
          inits.Add(s);
          BoundField boundField;

          if (!method.IsStatic) {
            inits.Add(InitializeOuterClosurePointer(method, bodyFixer.closureLocal, bodyFixer.outerClosures[nestingDepth], closureClass));
          }

          // if a parameter was captured, assign its value to the corresponding field
          // in the closure class
          foreach (var p in method.Parameters) {
            if (fieldForCapturedLocalOrParameter.TryGetValue(p, out boundField)) {
              s = bodyFixer.InitializeBoundFieldFromParameter(boundField, p);
              inits.Add(s);
            }
          }
          result2.Statements.InsertRange(0, inits);
        }
      }

      var result = new SourceMethodBody(this.host, this.sourceLocationProvider, null);
      result.Block = block;
      result.IsNormalized = true;
      result.LocalsAreZeroed = true;
      result.MethodDefinition = method;
      return result;
    }

    private IStatement ConstructClosureInstance(ILocalDefinition closureTemp, NestedTypeDefinition closureClass) {

      ITypeReference instanceOfClosureClass = closureClass;

      instanceOfClosureClass = GenericTypeInstance.GetGenericTypeInstance(closureClass,
        IteratorHelper.GetConversionEnumerable<IGenericMethodParameter, ITypeReference>(this.method.GenericParameters),
        host.InternFactory);

      var target = new TargetExpression() {
        Definition = closureTemp,
        Type = closureTemp.Type,
      };
      var constructor = this.GetOrCreateDefaultConstructorFor(closureClass);
      var constructorReference = this.GetSelfReference(constructor);

      var construct = new CreateObjectInstance() {
        MethodToCall = constructorReference,
        Type = instanceOfClosureClass,
      };
      var assignment = new Assignment() {
        Target = target,
        Source = construct,
        Type = this.GetSelfReferenceForPrivateHelperTypes(closureClass)
      };
      return new ExpressionStatement() { Expression = assignment };
    }

    private MethodDefinition GetOrCreateDefaultConstructorFor(NestedTypeDefinition closureClass) {
      MethodDefinition md = null;
      if (this.constructorForClosureClass.TryGetValue(closureClass, out md)) return md;
      MethodCall baseConstructorCall = new MethodCall() { ThisArgument = new ThisReference(), MethodToCall = this.ObjectCtor, Type = this.host.PlatformType.SystemVoid };
      ExpressionStatement baseConstructorCallStatement = new ExpressionStatement() { Expression = baseConstructorCall };
      List<IStatement> statements = new List<IStatement>();
      statements.Add(baseConstructorCallStatement);
      BlockStatement block = new BlockStatement() { Statements = statements };
      SourceMethodBody body = new SourceMethodBody(this.host, this.sourceLocationProvider);
      body.IsNormalized = true;
      body.LocalsAreZeroed = true;
      body.Block = block;

      MethodDefinition result = new MethodDefinition();
      closureClass.Methods.Add(result);
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
      this.constructorForClosureClass.Add(closureClass, result);
      return result;
    }

    private IMethodReference ObjectCtor {
      get {
        if (this.objectCtor == null)
          this.objectCtor = new Microsoft.Cci.MethodReference(this.host, this.host.PlatformType.SystemObject, CallingConvention.HasThis,
             this.host.PlatformType.SystemVoid, this.host.NameTable.Ctor, 0);
        return this.objectCtor;
      }
    }
    private IMethodReference/*?*/ objectCtor;

    private IStatement InitializeBoundFieldFromParameter(BoundField boundField, IParameterDefinition parameter) {
      var currentClosureLocal = this.closureLocal;
      var currentClosureLocalBinding = new BoundExpression() { Definition = currentClosureLocal, Type = currentClosureLocal.Type };
      var fieldDef = boundField.Field;
      var fieldRef = GetSelfReference(fieldDef);
      var target = new TargetExpression() { Instance = currentClosureLocalBinding, Definition = fieldRef, Type = parameter.Type };
      var boundParameter = new BoundExpression() { Definition = parameter, Type = parameter.Type };
      var assignment = new Assignment() { Target = target, Source = boundParameter, Type = parameter.Type };
      return new ExpressionStatement() { Expression = assignment };
    }

    private IStatement InitializeOuterClosurePointer(IMethodDefinition method, LocalDefinition closureLocal, FieldDefinition outerClosure, INestedTypeDefinition currentClass) {
      var currentClosureLocalBinding = new BoundExpression() { Definition = closureLocal, Type = closureLocal.Type };
      var tmp = this.method;
      this.method = method;

      var fieldRef = GetSelfReference(outerClosure);
      var target = new TargetExpression() { Instance = currentClosureLocalBinding, Definition = fieldRef, Type = currentClass };
      var thisRef = new ThisReference() {
        Type = this.GetSelfReferenceForPrivateHelperTypes(currentClass),
      };
      var assignment = new Assignment() { Target = target, Source = thisRef, Type = currentClass };
      this.method = tmp;
      return new ExpressionStatement() { Expression = assignment };
    }

    #endregion

    #region Entry Point

    /// <summary>
    /// Given a method and its body, along with a bunch of information
    /// computed by a previous visit to the body, rewrite the body in
    /// the following ways:
    /// 1. Any references to captured locals or parameters become
    /// references to the corresponding fields in the closure classes.
    /// 2. Create the definitions for the methods in the
    /// closure classes that implement the anonymous delegates.
    /// 3. Replace occurrences of anonymous delegates by create-delegate
    /// expressions.
    /// </summary>
    internal static IBlockStatement GetBodyAfterInjectingClosureFields(
      IMethodDefinition method,
      Dictionary<object, BoundField> fieldForCapturedLocalOrParameter,
      List<ITypeDefinition> classList,
      List<FieldDefinition> outerClosures,
      IMetadataHost host,
      ISourceLocationProvider/*?*/ sourceLocationProvider,
      Dictionary<uint, IGenericTypeParameter> genericTypeParameterMapping,
      Dictionary<IAnonymousDelegate, MethodDefinition> lambda2method,
      IBlockStatement body) {

      var instance = new InjectClosureFields(
        method, fieldForCapturedLocalOrParameter,
          classList, 0,
          outerClosures,
          host, sourceLocationProvider, genericTypeParameterMapping,
          lambda2method,
          new Dictionary<NestedTypeDefinition, MethodDefinition>());
      var result = instance.Visit(body);

      // For all methods except for the one in the innermost closure class,
      // inject a preamble that creates an instance of the next inner closure
      // class and stores the instance in a local variable. The preamble also
      // stores any parameters of the method that have been captured in that instance:
      //   local := new NestedClosure();
      //   local.p := p; // for each parameter p that is captured
      // NOTE: Can't make this the override for Visit(IBlockStatement) because
      // if the decompiler isn't perfect --- and who is? --- then there may be
      // nested blocks, but this should be done only for the top-level block.
      if (instance.nestedClosure != null) {
        var result2 = result as BlockStatement;
        if (result2 != null) { // result2 is just an alias for result
          var inits = new List<IStatement>();
          var s = instance.ConstructClosureInstance(instance.closureLocal, instance.nestedClosure);
          inits.Add(s);
          BoundField boundField;

          if (!method.IsStatic && !method.ContainingTypeDefinition.IsStruct) {
            inits.Add(instance.InitializeOuterClosurePointer(method, instance.closureLocal, instance.outerClosures[0], instance.nestedClosure));
          }

          // if a parameter was captured, assign its value to the corresponding field
          // in the closure class
          foreach (var p in method.Parameters) {
            if (fieldForCapturedLocalOrParameter.TryGetValue(p, out boundField)) {
              s = instance.InitializeBoundFieldFromParameter(boundField, p);
              inits.Add(s);
            }
          }
          result2.Statements.InsertRange(0, inits);
        }
      }

      return result;
    }

    #endregion

    #region Overrides

    /// <summary>
    /// If this is the declaration (i.e., definition) of a local that was captured, then
    /// transform it into an assignment to the field in the closure class.
    /// </summary>
    /// <param name="localDeclarationStatement">The local declaration statement.</param>
    public override IStatement Visit(LocalDeclarationStatement localDeclarationStatement) {
      var originalLocalVariable = localDeclarationStatement.LocalVariable;
      localDeclarationStatement.LocalVariable = this.VisitReferenceTo(originalLocalVariable);
      if (localDeclarationStatement.InitialValue != null) {
        var source = this.Visit(localDeclarationStatement.InitialValue);
        BoundField/*?*/ boundField;
        if (this.fieldForCapturedLocalOrParameter.TryGetValue(originalLocalVariable, out boundField)) {
          var currentClosureLocal = this.closureLocal;
          var currentClosureLocalBinding = new BoundExpression() { Definition = currentClosureLocal, Type = currentClosureLocal.Type };
          var fieldDef = boundField.Field;
          var fieldRef = GetSelfReference(fieldDef);
          var target = new TargetExpression() { Instance = currentClosureLocalBinding, Definition = fieldRef, Type = boundField.Type };
          var assignment = new Assignment() { Target = target, Source = source, Type = boundField.Type };
          return new ExpressionStatement() { Expression = assignment, Locations = localDeclarationStatement.Locations };
        } else {
          //return base.Visit(localDeclarationStatement);
          localDeclarationStatement.InitialValue = source;
        }
      }
      // Decompilation might have left a local declaration without an initial value. If so, it isn't needed
      // anymore (if that local was captured), since the assignment to it will become the assignment to the
      // field.
      if (this.fieldForCapturedLocalOrParameter.ContainsKey(originalLocalVariable))
        return CodeDummy.GotoStatement;
      return localDeclarationStatement;
    }

    /// <summary>
    /// Replaces all references to captured locals/parameters with a reference to the corresponding closure class field.
    /// </summary>
    public override IAddressableExpression Visit(AddressableExpression addressableExpression) {
      BoundField/*?*/ boundField;
      if (this.fieldForCapturedLocalOrParameter.TryGetValue(addressableExpression.Definition, out boundField)) {
        var selfReference = boundField.Field.ContainingTypeDefinition as NestedTypeDefinition;
        if (selfReference == null) return addressableExpression; // really "assert false" since no bound field belongs to a non-nested type
        addressableExpression.Instance = this.ClosureInstanceFor(selfReference);
        addressableExpression.Definition = this.GetSelfReference(boundField.Field);
        addressableExpression.Type = this.Visit(boundField.Type);
        return addressableExpression;
      }
      return base.Visit(addressableExpression);
    }

    /// <summary>
    /// Replaces all references to captured locals/parameters with a reference to the corresponding closure class field.
    /// </summary>
    public override IExpression Visit(BoundExpression boundExpression) {
      BoundField/*?*/ boundField;
      if (this.fieldForCapturedLocalOrParameter.TryGetValue(boundExpression.Definition, out boundField)) {
        var selfReference = boundField.Field.ContainingTypeDefinition as NestedTypeDefinition;
        if (selfReference == null) return boundExpression; // really "assert false" since no bound field belongs to a non-nested type
        boundExpression.Instance = this.ClosureInstanceFor(selfReference);
        boundExpression.Definition = this.GetSelfReference(boundField.Field);
        boundExpression.Type = this.Visit(boundField.Type);
        return boundExpression;
      }
      return base.Visit(boundExpression);
    }

    /// <summary>
    /// Replaces all references to captured locals/parameters with a reference to the corresponding closure class field.
    /// </summary>
    public override ITargetExpression Visit(TargetExpression targetExpression) {
      BoundField/*?*/ boundField;
      if (this.fieldForCapturedLocalOrParameter.TryGetValue(targetExpression.Definition, out boundField)) {
        var selfReference = boundField.Field.ContainingTypeDefinition as NestedTypeDefinition;
        if (selfReference == null) return targetExpression; // really "assert false" since no bound field belongs to a non-nested type
        targetExpression.Instance = this.ClosureInstanceFor(selfReference);
        targetExpression.Definition = this.GetSelfReference(boundField.Field);
        return targetExpression;
      }
      return base.Visit(targetExpression);
    }

    /// <summary>
    /// Replaces an occurrence of "this" (appearing in the original method or within a lambda)
    /// which refers to an instance of the class containing the original method
    /// with a bound expression that evaluates to the same instance. If the context is the
    /// original method, then just return the occurrence. If the context is a method in a (nested)
    /// closure class, then return an expression of the form "this.outer.outer ..." where "outer"
    /// is the field in each closure class that holds an instance of the class for the
    /// surrounding context (which is either the containing class of the original method for
    /// the outermost lambda or the closure class corresponding to the next outer lambda for
    /// any nested lambdas).
    /// 
    /// </summary>
    public override IExpression Visit(ThisReference thisReference) {

      if (this.nestingDepth == 0) {
        // then the occurrence of "this" is in the original method
        // and doesn't need to be modified at all!
        return thisReference;
      }

      // Otherwise we must be in the context of a nested closure.
      NestedTypeDefinition closure = (NestedTypeDefinition)this.currentClass;

      Debug.Assert(this.fieldForCapturedLocalOrParameter.ContainsKey(thisReference.Type.InternedKey));
      BoundField f = this.fieldForCapturedLocalOrParameter[thisReference.Type.InternedKey];
      ThisReference thisRef = new ThisReference();
      thisRef.Type = this.GetSelfReferenceForPrivateHelperTypes(closure);
      var boundExpression = new BoundExpression();
      boundExpression.Instance = thisRef;
      if (this.nestingDepth > 1) {
        for (int i = this.nestingDepth - 1; 0 < i; i--) {
          boundExpression.Definition = this.GetSelfReference(this.outerClosures[i]);
          var be = new BoundExpression();
          be.Instance = boundExpression;
          boundExpression = be;
        }
      }
      boundExpression.Definition = this.GetSelfReference(this.outerClosures[0]);
      return boundExpression;
    }

    public override ITypeReference Visit(ITypeReference typeReference) {

      // In the body of the original method, do *not* replace references to the method's
      // generic parameters with references to the type parameters of the closure class.
      if (this.nestingDepth == 0) return typeReference;

      IGenericMethodParameter gmp = typeReference as IGenericMethodParameter;
      if (gmp != null) {
        IGenericTypeParameter targetType;
        if (this.genericTypeParameterMapping.TryGetValue(gmp.InternedKey, out targetType))
          return targetType;
      }
      IGenericMethodParameterReference gmpr = typeReference as IGenericMethodParameterReference;
      if (gmpr != null) {
        IGenericTypeParameter targetType;
        if (this.genericTypeParameterMapping.TryGetValue(gmpr.ResolvedType.InternedKey, out targetType))
          return targetType;
      }
      return base.Visit(typeReference);
    }

    /// <summary>
    /// Generates the implementation for the <paramref name="anonymousDelegate"/>.
    /// Notation:
    ///   The <paramref name="anonymousDelegate"/> is lambda.
    ///   The method whose body contains the lambda is M.
    ///   The class containing M is C.
    ///   The generated method implementing the lambda is L.
    ///   
    /// If the lambda captures any state:
    ///   then L is a static method defined in C.
    ///   then L is an instance method in a generated (nested private)
    ///   class, "CC", that is contained in C.
    /// </summary>
    /// <param name="anonymousDelegate">The anonymous delegate.</param>
    public override IExpression Visit(AnonymousDelegate anonymousDelegate) {
      var M = this.method;

      var L = this.lambda2method[anonymousDelegate]; //new MethodDefinition();
      L.CallingConvention = anonymousDelegate.CallingConvention;
      if ((anonymousDelegate.CallingConvention & CallingConvention.HasThis) == 0) {
        // Keep method static
        L.IsStatic = true;
        // But if it was generic, turn that off because the method being generic turns
        // into the closure class being generic and not the method.
        // REVIEW: this might need to change if we stop putting the static methods into
        // a nested closure class.
        L.CallingConvention = L.CallingConvention & ~CallingConvention.Generic;
      }
      L.CallingConvention = L.CallingConvention & ~CallingConvention.Generic;

      L.IsCil = true;
      L.IsHiddenBySignature = true;

      if (anonymousDelegate.ReturnValueIsModified)
        L.ReturnValueCustomModifiers = new List<ICustomModifier>(anonymousDelegate.ReturnValueCustomModifiers);
      L.ReturnValueIsByRef = anonymousDelegate.ReturnValueIsByRef;
      L.Visibility = TypeMemberVisibility.Public;

      var newParams = new List<IParameterDefinition>(anonymousDelegate.Parameters.Count);
      // hack? increment the nesting depth so that the visit of the parameter types
      // happens in the context of the anonymous delegate and not the containing method.
      // If this isn't done, then generic method parameters referred to in the parameter
      // types don't get transformed into being references to the generic type parameters
      // of the closure class.
      // Also, need to do this for the types of any local variables, as well as the return
      // type of the closure method itself.
      this.nestingDepth++;
      foreach (var p in anonymousDelegate.Parameters) {
        var newP = new ParameterDefinition();
        newP.Copy(p, this.host.InternFactory);
        newP.ContainingSignature = L;
        newP.Type = this.Visit(newP.Type);
        newParams.Add(newP);
        BoundField/*?*/ bf;
        if (this.fieldForCapturedLocalOrParameter.TryGetValue(p, out bf)) {
          this.fieldForCapturedLocalOrParameter.Add(newP, bf);
        }
      }
      L.Parameters = newParams;

      L.Type = this.Visit(anonymousDelegate.ReturnType);

      if ((anonymousDelegate.CallingConvention & CallingConvention.HasThis) == 0) {
        SubstituteParameters sps = new SubstituteParameters(host, anonymousDelegate, L);
        anonymousDelegate.Body = sps.Visit(anonymousDelegate.Body);
      }

      L.Body = this.GetMethodBodyFrom(anonymousDelegate.Body, L, this.genericTypeParameterMapping);
      // Can't ask for the local variables until after the body has been set

      // NB!!! Side effect of evaluating the LocalVariables is to turn the CodeModel into IL!!
      // So this has to be done *after* the body is constructed, but don't expect to do any 
      // further modifications to the CodeModel of the Body after this is done!
      foreach (var v in L.Body.LocalVariables) {
        LocalDefinition ld = v as LocalDefinition;
        if (ld != null) {
          ld.Type = this.Visit(ld.Type);
          ld.MethodDefinition = L;
        }
      }

      this.nestingDepth--;

      var boundCurrentClosureLocal = new BoundExpression();
      boundCurrentClosureLocal.Definition = this.closureLocal;

      var createDelegateInstance = new CreateDelegateInstance();
      if ((anonymousDelegate.CallingConvention & CallingConvention.HasThis) != 0)
        createDelegateInstance.Instance = boundCurrentClosureLocal;

      var mref = this.GetSelfReference(L);
      if (L.IsGeneric) {
        var gas = new List<ITypeReference>();
        foreach (var gmp in M.GenericParameters)
          gas.Add(gmp);
        mref = new GenericMethodInstanceReference() {
          CallingConvention = L.CallingConvention,
          ContainingType = TypeDefinition.SelfInstance(L.ContainingTypeDefinition, this.host.InternFactory),
          GenericArguments = gas,
          GenericMethod = mref,
          InternFactory = this.host.InternFactory,
          Name = mref.Name,
          Parameters = new List<IParameterTypeInformation>(mref.Parameters),
          Type = mref.Type,
        };
      }
      createDelegateInstance.MethodToCallViaDelegate = mref;
      createDelegateInstance.Type = this.Visit(anonymousDelegate.Type);

      return createDelegateInstance;
    }

    #endregion

  }
  /// <summary>
  /// A mutator that substitutes parameters defined in one signature with those from another signature.
  /// </summary>
  public sealed class SubstituteParameters : MethodBodyCodeAndContractMutator {
    private ISignature targetSignature;
    private ISignature sourceSignature;
    List<IParameterTypeInformation> parameters;

    /// <summary>
    /// Creates a mutator that replaces all occurrences of parameters from the target signature with those from the source signature.
    /// </summary>
    public SubstituteParameters(IMetadataHost host, ISignature targetSignature, ISignature sourceSignature)
      : base(host, true /*false*/) { // NB: Important to pass "false": this mutator needs to make a copy of the entire contract!
      this.targetSignature = targetSignature;
      this.sourceSignature = sourceSignature;
      this.parameters = new List<IParameterTypeInformation>(sourceSignature.Parameters);
    }

    /// <summary>
    /// If the <paramref name="boundExpression"/> represents a parameter of the source signature,
    /// it is replaced with the equivalent parameter of the target signature.
    /// </summary>
    /// <param name="boundExpression">The bound expression.</param>
    public override IExpression Visit(BoundExpression boundExpression) {
      ParameterDefinition/*?*/ par = boundExpression.Definition as ParameterDefinition;
      if (par != null && par.ContainingSignature == this.targetSignature) {
        boundExpression.Definition = this.parameters[par.Index];
        return boundExpression;
      } else {
        return base.Visit(boundExpression);
      }
    }

  }

}
