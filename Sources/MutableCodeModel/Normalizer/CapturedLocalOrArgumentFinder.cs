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
using Microsoft.Cci.Contracts;

namespace Microsoft.Cci.MutableCodeModel {

  internal class ClosureFinder : BaseCodeTraverser {

    /*\
     * 
     * Closure Finder state
     * 1. Map: Lambda to Method. So closure finder can create the method and have its containing type set correctly.
     *    The normalizer then can retrieve the method from the map when it needs to generate a body for the method.
     * 2. Map: Defs to fields. This can just be one global table where the fields may be in different closure classes
     *    because of nested closures. Can always retrieve the closure class from the field, if it is needed.
     * 3. Map: Local definitions/parameter definitions to closure class that would contain a field corresponding to
     *    that local/parameter *if* any occurrence of the local/parameter is captured within a lambda. Note that the
     *    closure class must be the outermost one relative to the scope of the local or the parameter.
     * 4. Set: Local/parameter definitions that are discovered. Add the a local when a local definition is
     *    encountered. Add the parameters of the enclosing method (the one being normalized) when the first
     *    lambda is entered. Add the parameters of each lambda when that lambda is entered.
     *    When a lambda is encountered, add all elements of the set to map (3) mapping them to the closure class
     *    corresponding to the lambda. Then clear the set.
     * 5. List: The list of classes used by the closure classes. I.e., the first element is the containing class of
     *    the original method that contains the lambdas. The rest are the nested closure classes created by this
     *    visitor as it walks the original method body. Due to the order of the visit, the list is meant to be in
     *    the order from outermost class to innermost.
     *    I.e., (forall i : 0 < i < closureClassList.Length : closureClassList[i].ContainingTypeDefinition == closureClassList[i-1])
     *    The list is used by the Normalizer for access to all of the classes.
     * 6. List: The list of generated fields used by the generated closure classes to point to their enclosing generated closure
     *    class. (The outermost closure class has one only if it captures the "this" reference from the original method,
     *    otherwise it doesn't need the field.)
     *    
    \*/

    internal Dictionary<IAnonymousDelegate, MethodDefinition> lambda2method = new Dictionary<IAnonymousDelegate, MethodDefinition>();
    internal Dictionary<object, BoundField> fieldForCapturedLocalOrParameter;
    private Dictionary<INamedEntity, NestedTypeDefinition> localOrParameter2ClosureClass = new Dictionary<INamedEntity, NestedTypeDefinition>();
    private Dictionary<INamedEntity, bool> localsOrParametersInScope = new Dictionary<INamedEntity, bool>();
    internal List<ITypeDefinition> classList = new List<ITypeDefinition>();
    internal List<FieldDefinition> outerClosures = new List<FieldDefinition>();


    /// <summary>
    /// The enclosing method that is being normalized, i.e., the one whose body will end up containing the lambdas.
    /// </summary>
    IMethodDefinition method;
    IAnonymousDelegate/*?*/ currentAnonymousDelegate;
    IMetadataHost host;
    INameTable nameTable;
    internal bool foundAnonymousDelegate;
    internal bool foundYield;
    internal NestedTypeDefinition/*?*/ generatedclosureClass;
    internal Dictionary<uint, IGenericTypeParameter> genericTypeParameterMapping = new Dictionary<uint, IGenericTypeParameter>();
    CopyTypeFromIteratorToClosure copyTypeToClosure;
    int counter;
    /// <summary>
    /// The depth in the current tree of lambdas. Level 0 is the original method.
    /// invariant: 0 &lt;= nestingDepth &lt;= classList.Count
    /// </summary>
    int nestingDepth;

    internal ClosureFinder(
      IMethodDefinition method,
      Dictionary<object, BoundField> fieldForCapturedLocalOrParameter,
      IMetadataHost host,
      int counter) {
      this.method = method;
      this.fieldForCapturedLocalOrParameter = fieldForCapturedLocalOrParameter;
      this.host = host;
      this.nameTable = host.NameTable;
      this.counter = counter;
      this.classList.Add(method.ContainingTypeDefinition);
      this.nestingDepth = 1;
    }

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
    /// If a definition should be captured, capture it. Otherwise noop. 
    /// 
    /// The act of capturing means mapping the definition (or its type's interned id if the definition is a reference to THIS) to
    /// a new BoundField object that represents a field in the closure class. 
    /// </summary>
    /// <param name="definition"></param>
    private void CaptureDefinition(object definition) {
      IThisReference/*?*/ thisRef = definition as IThisReference;
      if (thisRef != null) {
        definition = thisRef.Type.ResolvedType.InternedKey;        
      }
      if (this.fieldForCapturedLocalOrParameter.ContainsKey(definition)) return;

      IName/*?*/ name = null;
      ITypeReference/*?*/ type = null;
      ILocalDefinition/*?*/ local = definition as ILocalDefinition;
      var containingClass = this.generatedclosureClass;
      if (local != null) {
        if (!this.localOrParameter2ClosureClass.TryGetValue(local, out containingClass)) return;
        if (false && containingClass == this.generatedclosureClass) {
          // A use of a local is captured only if it is found in a *nested* closure,
          // not the closure where the local is defined.
          return;
        }
        name = local.Name;
        type = local.Type;
      } else {
        IParameterDefinition/*?*/ par = definition as IParameterDefinition;
        if (par != null) {
          if (!this.localOrParameter2ClosureClass.TryGetValue(par, out containingClass)) return;
          if (par.ContainingSignature == this.currentAnonymousDelegate) {
            // A use of a parameter is captured only if it is found in a *nested* closure,
            // not the closure to which the parameter belongs.
            return;
          }
          name = par.Name;
          type = par.Type;
        } else {
          if (definition is uint) {
            type = thisRef.Type;
            name = this.nameTable.GetNameFor("__this value");
          } else return;
        }
      }
      if (name == null) return;

      FieldDefinition field = new FieldDefinition();
      field.ContainingTypeDefinition = containingClass;
      field.Name = name;
      field.Type = this.copyTypeToClosure.Visit(type);
      field.Visibility = TypeMemberVisibility.Public;
      containingClass.Fields.Add(field);
      BoundField be = new BoundField(field, field.Type);
      this.fieldForCapturedLocalOrParameter.Add(definition, be);
    }

    internal NestedTypeDefinition CreateClosureClass(bool makeGeneric) {
      CustomAttribute compilerGeneratedAttribute = new CustomAttribute();
      compilerGeneratedAttribute.Constructor = this.CompilerGeneratedCtor;

      NestedTypeDefinition result = new NestedTypeDefinition();
      string signature = MemberHelper.GetMethodSignature(this.method, NameFormattingOptions.Signature | NameFormattingOptions.ReturnType | NameFormattingOptions.TypeParameters);
      //result.Name = this.host.NameTable.GetNameFor("closureclass");
      result.Name = this.host.NameTable.GetNameFor(signature + " closure " + this.counter);
      this.counter++;
      result.Attributes.Add(compilerGeneratedAttribute);
      result.BaseClasses.Add(this.host.PlatformType.SystemObject);
      result.ContainingTypeDefinition = this.generatedclosureClass != null ? this.generatedclosureClass : method.ContainingTypeDefinition;
      result.InternFactory = this.host.InternFactory;
      result.IsBeforeFieldInit = true;
      result.IsClass = true;
      result.IsSealed = true;
      result.Layout = LayoutKind.Auto;
      result.StringFormat = StringFormatKind.Ansi;
      result.Visibility = TypeMemberVisibility.Private;

      //BoundField/*?*/ capturedThis;
      //var thisTypeReference = TypeDefinition.SelfInstance(this.method.ContainingTypeDefinition, this.host.InternFactory);
      //if (this.closureLocals.Count == 0 && this.FieldForCapturedLocalOrParameter.TryGetValue(thisTypeReference.InternedKey, out capturedThis)) {
      //  result.Fields.Add(capturedThis.Field);
      //  capturedThis.Field.ContainingTypeDefinition = result;
      //  capturedThis.Field.Type = this.Visit(capturedThis.Field.Type);
      //}

      if (makeGeneric) {
        List<IGenericMethodParameter> genericMethodParameters = new List<IGenericMethodParameter>();
        ushort count = 0;
        foreach (var genericMethodParameter in this.method.GenericParameters) {
          genericMethodParameters.Add(genericMethodParameter);
          GenericTypeParameter newTypeParam = new GenericTypeParameter() {
            Name = this.host.NameTable.GetNameFor(genericMethodParameter.Name.Value + "_"),
            Index = (count++),
          };
          this.genericTypeParameterMapping[genericMethodParameter.InternedKey] = newTypeParam;
          newTypeParam.DefiningType = result;
          result.GenericParameters.Add(newTypeParam);
        }
        this.copyTypeToClosure = new CopyTypeFromIteratorToClosure(this.host, genericTypeParameterMapping);
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
      }

      this.generatedclosureClass = result;
      classList.Add(result);
      return result;
    }


    public override void Visit(IAnonymousDelegate anonymousDelegate) {
      this.foundAnonymousDelegate = true;
      IAnonymousDelegate/*?*/ savedCurrentAnonymousDelegate = this.currentAnonymousDelegate;
      this.currentAnonymousDelegate = anonymousDelegate;
      NestedTypeDefinition/*?*/ savedCurrentClosureClass = this.generatedclosureClass;

      // If this is a new level, then create a closure class to implement the lambda.
      // (Only the outermost closure class can be a generic class and then only if the method
      // containing the closures is generic.)
      // Otherwise any lambdas at this level will become methods in this level's closure class
      NestedTypeDefinition closureClass;
      if (nestingDepth < this.classList.Count) {
        closureClass = (NestedTypeDefinition)this.classList[nestingDepth];
      } else {
        closureClass = this.CreateClosureClass(savedCurrentClosureClass == null);
        // If this is not the outermost closure class, then create a field
        // that methods in this closure class can use to access fields in
        // enclosing closure classes.
        if (savedCurrentClosureClass != null) {
          var outerClosureField = new FieldDefinition() {
            ContainingTypeDefinition = closureClass,
            Name = this.host.NameTable.GetNameFor("__outerClosure"),
            Type = TypeDefinition.SelfInstance(savedCurrentClosureClass,this.host.InternFactory),
            Visibility = TypeMemberVisibility.Public,
          };
          closureClass.Fields.Add(outerClosureField);
          this.outerClosures.Add(outerClosureField);
        }
      }

      this.generatedclosureClass = closureClass;

      // Keep a local list of the parameters and locals that, if captured,
      // would become fields of the closure class for this lambda. An
      // alternative would be to keep a stack of scopes instead of the single
      // table "localOrParameter2ClosureClass". Then when leaving a lambda,
      // the local table for that closure class could be popped from the
      // stack. But then when potentially capturing a use of a local/parameter
      // each table in the stack would have to be searched. Using one table
      // makes that more efficient, but then requires the use of this local
      // list so that the shared table can be purged after visiting the current
      // lambda.
      var localOrParametersInScopeForThisClosure = new List<INamedEntity>();

      // Map all locals in scope to the closure class, in case they are captured.
      foreach (var l in this.localsOrParametersInScope.Keys) {
        this.localOrParameter2ClosureClass.Add(l, closureClass);
        localOrParametersInScopeForThisClosure.Add(l);
      }
      // Add parameters of enclosing method, if this is the outermost lambda
      if (savedCurrentAnonymousDelegate == null) {
        foreach (var p in this.method.Parameters) {
          this.localOrParameter2ClosureClass.Add(p, closureClass);
          localOrParametersInScopeForThisClosure.Add(p);
        }
      }

      // Clear set of locals in scope, start new scope within lambda
      var savedLocalsOrParametersInScope = this.localsOrParametersInScope;
      this.localsOrParametersInScope = new Dictionary<INamedEntity, bool>();

      // The parameters of the lambda are available for capturing.
      // This constitutes their definition point.
      foreach (var p in anonymousDelegate.Parameters) {
        this.localsOrParametersInScope[p] = true;
      }

      // Create a (partial) method so its containing type is correctly set.
      // Then add it to map so that Normalizer can retrieve it and fill in
      // the rest of the definition.
      var L = new MethodDefinition();
      L.ContainingTypeDefinition = closureClass;
      L.Name = this.host.NameTable.GetNameFor("__anonymous_method " + IteratorHelper.EnumerableCount(closureClass.Methods));
      closureClass.Methods.Add(L);
      this.lambda2method.Add(anonymousDelegate, L);

      this.nestingDepth++;
      base.Visit(anonymousDelegate);
      this.nestingDepth--;

      // If they aren't removed, then uses outside of an inner lambda
      // (but inside of an outer lambda!) will get mistakenly captured.
      foreach (var ne in localOrParametersInScopeForThisClosure) {
        this.localOrParameter2ClosureClass.Remove(ne);
      }

      this.currentAnonymousDelegate = savedCurrentAnonymousDelegate;
      this.localsOrParametersInScope = savedLocalsOrParametersInScope;
      if (savedCurrentClosureClass != null) {
        // leave the outermost one so that Normalizer can retrieve it
        this.generatedclosureClass = savedCurrentClosureClass;
      }
    }

    public override void Visit(IAddressableExpression addressableExpression) {
      base.Visit(addressableExpression);
      if (this.currentAnonymousDelegate != null)
        this.CaptureDefinition(addressableExpression.Definition);
    }

    public override void Visit(IBaseClassReference baseClassReference) {
      base.Visit(baseClassReference);
    }

    public override void Visit(IBoundExpression boundExpression) {
      base.Visit(boundExpression);
      if (this.currentAnonymousDelegate != null) {
        if (boundExpression.Instance != null)
          this.CaptureDefinition(boundExpression.Instance);
        this.CaptureDefinition(boundExpression.Definition);
      }
    }

    public override void Visit(ILocalDeclarationStatement localDeclarationStatement) {
      this.localsOrParametersInScope[localDeclarationStatement.LocalVariable] = true;
      base.Visit(localDeclarationStatement);
    }

    public override void Visit(ITargetExpression targetExpression) {
      base.Visit(targetExpression);
      if (this.currentAnonymousDelegate != null)
        this.CaptureDefinition(targetExpression.Definition);
    }

    public override void Visit(IThisReference thisReference) {
      base.Visit(thisReference);
      if (this.currentAnonymousDelegate != null)
        this.CaptureDefinition(thisReference);
    }

    public override void Visit(IYieldBreakStatement yieldBreakStatement) {
      this.foundYield = true;
      base.Visit(yieldBreakStatement);
    }

    public override void Visit(IYieldReturnStatement yieldReturnStatement) {
      this.foundYield = true;
      base.Visit(yieldReturnStatement);
    }

  }
}
