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
using System.Collections.Generic;
using Microsoft.Cci.MutableCodeModel;

namespace Microsoft.Cci.ILToCodeModel {

  /// <summary>
  /// Replaces certain compiler-generated patterns with their source-level equivalents.
  ///   1. Closures: anonymous delegates are restored from closure classes and methods.
  ///   2. Return values: boolean methods have integer return values replaced by boolean values.
  /// </summary>
  internal class CompilationArtifactRemover : MethodBodyCodeMutator {

    internal CompilationArtifactRemover(SourceMethodBody sourceMethodBody, bool restoreAnonymousDelegates)
      : base(sourceMethodBody.host, true) {
      this.containingType = sourceMethodBody.ilMethodBody.MethodDefinition.ContainingTypeDefinition;
      this.sourceMethodBody = sourceMethodBody;
      this.restoreAnonymousDelegates = restoreAnonymousDelegates;
    }

    internal Dictionary<uint, IBoundExpression> capturedBinding = new Dictionary<uint, IBoundExpression>();
    ITypeDefinition containingType;
    Dictionary<ILocalDefinition, bool> currentClosureLocals = new Dictionary<ILocalDefinition, bool>();
    SourceMethodBody sourceMethodBody;
    Dictionary<ILocalDefinition, IExpression> expressionToSubstituteForCompilerGeneratedSingleAssignmentLocal = new Dictionary<ILocalDefinition, IExpression>();
    Dictionary<IParameterDefinition, IParameterDefinition> parameterMap = new Dictionary<IParameterDefinition, IParameterDefinition>();
    internal Dictionary<int, bool>/*?*/ referencedLabels;
    GenericMethodParameterMapper/*?*/ genericParameterMapper = null;
    private bool restoreAnonymousDelegates = false;

    public IBlockStatement RemoveCompilationArtifacts(BlockStatement blockStatement) {
      if (this.referencedLabels == null) {
        LabelReferenceFinder finder = new LabelReferenceFinder();
        finder.Traverse(blockStatement.Statements);
        this.referencedLabels = finder.referencedLabels;
      }
      if (this.restoreAnonymousDelegates) {
        this.RecursivelyFindCapturedLocals(blockStatement);
      }
      IBlockStatement result = this.Visit(blockStatement);
      if (this.restoreAnonymousDelegates) {
        result = CachedDelegateRemover.RemoveCachedDelegates(this.host, result, sourceMethodBody);
      }
      return result;
    }

    public override IAddressableExpression Visit(AddressableExpression addressableExpression) {
      var closureField = addressableExpression.Definition as IFieldReference;
      if (closureField != null) {
        var unspecializedClosureField = UnspecializedMethods.UnspecializedFieldReference(closureField);
        IBoundExpression binding = null;
        if (this.capturedBinding.TryGetValue(unspecializedClosureField.InternedKey, out binding)) {
          addressableExpression.Definition = binding.Definition;
          addressableExpression.Instance = binding.Instance;
          return addressableExpression;
        }
      }
      var parameter = addressableExpression.Definition as IParameterDefinition;
      if (parameter != null) {
        IParameterDefinition parToSubstitute;
        if (this.parameterMap.TryGetValue(parameter, out parToSubstitute)) {
          addressableExpression.Definition = parToSubstitute;
          return addressableExpression;
        }
      }
      return base.Visit(addressableExpression);
    }

    public override IExpression Visit(Assignment assignment) {
      var ld = assignment.Target.Definition as ILocalDefinition;
      if (ld != null && this.currentClosureLocals.ContainsKey(ld))
        return CodeDummy.Expression;
      return base.Visit(assignment);
    }

    public override IExpression Visit(Conversion conversion) {
      conversion.ValueToConvert = this.Visit(conversion.ValueToConvert);
      if (TypeHelper.TypesAreEquivalent(conversion.TypeAfterConversion, conversion.ValueToConvert.Type) &&
        // converting a floating point number to the same floating point number is not a nop: it might result in precision loss.
        !(conversion.TypeAfterConversion.TypeCode == PrimitiveTypeCode.Float32 || conversion.TypeAfterConversion.TypeCode == PrimitiveTypeCode.Float64)
        )
        return conversion.ValueToConvert;
      else {
        var cc = conversion.ValueToConvert as CompileTimeConstant;
        if (cc != null) {
          if (cc.Value == null) {
            cc.Type = conversion.TypeAfterConversion;
            return cc;
          }
          if (conversion.TypeAfterConversion.TypeCode == PrimitiveTypeCode.Boolean && conversion.ValueToConvert.Type.TypeCode == PrimitiveTypeCode.Int32 && cc.Value is int) {
            var bcc = new CompileTimeConstant();
            bcc.Value = ((int)cc.Value) != 0;
            bcc.Type = conversion.TypeAfterConversion;
            return bcc;
          }
          if (conversion.TypeAfterConversion.TypeCode == PrimitiveTypeCode.Char && conversion.ValueToConvert.Type.TypeCode == PrimitiveTypeCode.Int32 && cc.Value is int) {
            var bcc = new CompileTimeConstant();
            bcc.Value = (char)(int)cc.Value;
            bcc.Type = conversion.TypeAfterConversion;
            return bcc;
          }
        } else if (conversion.TypeAfterConversion.TypeCode == PrimitiveTypeCode.Boolean) {
          var conditional = conversion.ValueToConvert as Conditional;
          if (conditional != null) {
            conditional.ResultIfFalse = this.ConvertToBoolean(conditional.ResultIfFalse);
            conditional.ResultIfTrue = this.ConvertToBoolean(conditional.ResultIfTrue);
            conditional.Type = conversion.TypeAfterConversion;
            return conditional;
          }
        }
        return conversion;
      }
    }

    public override IExpression Visit(IExpression expression) {
      var convertToUnsigned = expression as ConvertToUnsigned;
      if (convertToUnsigned != null) expression = new Conversion(convertToUnsigned);
      return base.Visit(expression);
    }

    private IExpression ConvertToBoolean(IExpression expression) {
      if (expression.Type.TypeCode == PrimitiveTypeCode.Boolean) return expression;
      var cc = expression as CompileTimeConstant;
      if (cc != null && cc.Value is int) {
        cc.Value = ((int)cc.Value) != 0;
        cc.Type = expression.Type.PlatformType.SystemBoolean;
        return cc;
      }
      return new Conversion() {
        ValueToConvert = expression,
        TypeAfterConversion = expression.Type.PlatformType.SystemBoolean,
        Type = expression.Type.PlatformType.SystemBoolean
      };
    }

    public override ITargetExpression Visit(TargetExpression targetExpression) {
      var closureField = targetExpression.Definition as IFieldReference;
      if (closureField != null) {
        var unspecializedClosureField = UnspecializedMethods.UnspecializedFieldReference(closureField);
        IBoundExpression binding = null;
        if (this.capturedBinding.TryGetValue(unspecializedClosureField.InternedKey, out binding)) {
          targetExpression.Definition = binding.Definition;
          targetExpression.Instance = binding.Instance;
          return targetExpression;
        }
      }
      var parameter = targetExpression.Definition as IParameterDefinition;
      if (parameter != null) {
        IParameterDefinition parToSubstitute;
        if (this.parameterMap.TryGetValue(parameter, out parToSubstitute)) {
          targetExpression.Definition = parToSubstitute;
          return targetExpression;
        }
      }
      return base.Visit(targetExpression);
    }

    class CapturedLocalsFinder : CodeTraverser {

      CompilationArtifactRemover remover;
      ILocalDefinition/*?*/ exceptionContainer;

      internal CapturedLocalsFinder(CompilationArtifactRemover remover) {
        this.remover = remover;
      }

      public override void TraverseChildren(IBlockStatement block) {
        var blockStatement = block as BlockStatement;
        if (blockStatement != null)
          this.FindCapturedLocals(blockStatement.Statements);
        base.TraverseChildren(block);
      }

      public override void TraverseChildren(ICatchClause catchClause) {
        var savedExceptionContainer = this.exceptionContainer;
        this.exceptionContainer = catchClause.ExceptionContainer;
        base.TraverseChildren(catchClause);
        this.exceptionContainer = savedExceptionContainer;
      }

      private void FindCapturedLocals(List<IStatement> statements) {
        ILocalDefinition/*?*/ locDef = null;
        IFieldReference/*?*/ fieldRef = null;
        INestedTypeReference/*?*/ closureType = null;
        int i = 0;
        while (i < statements.Count && closureType == null) {
          var statement = statements[i++];
          var locDecl = statement as LocalDeclarationStatement;
          if (locDecl == null) {
            var exprStatement = statement as ExpressionStatement;
            if (exprStatement == null) continue;
            var assignment = exprStatement.Expression as Assignment;
            if (assignment == null) continue;
            if (!(assignment.Source is ICreateObjectInstance)) continue;
            locDef = assignment.Target.Definition as ILocalDefinition;
            if (locDef != null)
              closureType = UnspecializedMethods.AsUnspecializedNestedTypeReference(locDef.Type);
            else {
              fieldRef = assignment.Target.Definition as IFieldReference;
              if (fieldRef == null || !(assignment.Target.Instance is IThisReference)) continue;
              closureType = UnspecializedMethods.AsUnspecializedNestedTypeReference(fieldRef.Type);
            }
          } else {
            if (!(locDecl.InitialValue is ICreateObjectInstance)) continue;
            locDef = locDecl.LocalVariable;
            closureType = UnspecializedMethods.AsUnspecializedNestedTypeReference(locDef.Type);
          }
        }
        if (closureType == null) return;
        //REVIEW: need to avoid resolving types that are not defined in the module we are analyzing.
        ITypeReference t1 = UnspecializedMethods.AsUnspecializedTypeReference(closureType.ContainingType.ResolvedType);
        ITypeReference t2 = UnspecializedMethods.AsUnspecializedTypeReference(this.remover.containingType);
        if (!TypeHelper.TypesAreEquivalent(t1, t2)) {
          var nt2 = t2 as INestedTypeReference;
          if (nt2 == null || !TypeHelper.TypesAreEquivalent(t1, nt2.ContainingType)) return;
        }
        var resolvedClosureType = closureType.ResolvedType;
        if (!UnspecializedMethods.IsCompilerGenerated(resolvedClosureType)) return;
        //Check if this is an iterator creating its state class, rather than just a method creating a closure class
        foreach (var iface in resolvedClosureType.Interfaces) {
          if (TypeHelper.TypesAreEquivalent(iface, resolvedClosureType.PlatformType.SystemCollectionsIEnumerator)) return;
        }
        if (this.remover.sourceMethodBody.privateHelperTypesToRemove == null)
          this.remover.sourceMethodBody.privateHelperTypesToRemove = new List<ITypeDefinition>();
        this.remover.sourceMethodBody.privateHelperTypesToRemove.Add(resolvedClosureType);
        if (locDef != null)
          this.remover.currentClosureLocals.Add(locDef, true);
        else {
          if (this.remover.sourceMethodBody.privateHelperFieldsToRemove == null)
            this.remover.sourceMethodBody.privateHelperFieldsToRemove = new Dictionary<IFieldDefinition, IFieldDefinition>();
          var field = UnspecializedMethods.UnspecializedFieldDefinition(fieldRef.ResolvedField);
          this.remover.sourceMethodBody.privateHelperFieldsToRemove.Add(field, field);
        }

        if (resolvedClosureType.IsGeneric && this.remover.sourceMethodBody.MethodDefinition.IsGeneric)
          this.remover.genericParameterMapper = new GenericMethodParameterMapper(this.remover.host, this.remover.sourceMethodBody.MethodDefinition, resolvedClosureType);

        statements.RemoveAt(i - 1);

        // Walk the rest of the statements in the block (but *without* recursively
        // descending into them) looking for assignments that save local state into
        // fields in the closure. Such assignments do not belong in the method body.
        // 
        // They were introduced by the compiler because the closure reads their value.
        // That is, such assignments are of the form:
        //  closureLocal.f := e
        // where "e" is either "this", a local, a parameter, (corner case) a value not held in a local of the original program,
        // or another closureLocal (because sometimes the compiler generates code so
        // that one closure class has access to another one).
        // When the RHS expression is a local/parameter, then rely on a naming
        // convention that the field f has the same name as the local/parameter.
        // If it does not follow the naming convention, then the statement corresponds to
        // a real statement that was in the original method body.
        //
        // For each such assignment statement, delete it from the list of statements and
        // add "e" to the remover's table as the expression to replace all occurrences of
        // "closureLocal.f" throughout the method body.
        //
        // [Note on corner case: this seems to arise when a value occurs in an anonymous delegate that
        // isn't used outside of the anonymous delegate.
        // For instance: { ... var x = new Object(); M((args for lambda) => ... body of lambda contains a reference to x ...) ... }
        // where there are no occurrences of x in the rest of the method body. The compiler plays it safe and still treats x as a
        // captured local.]
        //
        for (int j = i - 1; j < statements.Count; j++) {
          if (statements[j] is IEmptyStatement) continue;
          IExpressionStatement/*?*/ es = statements[j] as IExpressionStatement;
          if (es == null) continue;
          IAssignment/*?*/ assignment = es.Expression as IAssignment;
          if (assignment == null) continue;
          IFieldReference/*?*/ closureField = assignment.Target.Definition as IFieldReference;
          if (closureField == null) {
            // check to see if it is of the form "loc := closureLocal".
            // I.e., a local has been introduced that is an alias for a local containing a closure instance.
            var targetLoc = this.TargetExpressionAsLocal(assignment.Target);
            var sourceLoc = this.ExpressionAsLocal(assignment.Source);
            if (targetLoc != null && sourceLoc != null && this.remover.currentClosureLocals.ContainsKey(sourceLoc)) {
              this.remover.currentClosureLocals.Add(targetLoc, true);
              statements.RemoveAt(j--);
            }
            continue;
          }
          var unspecializedClosureField = UnspecializedMethods.UnspecializedFieldReference(closureField);
          var closureFieldContainingType = UnspecializedMethods.AsUnspecializedNestedTypeReference(closureField.ContainingType);
          if (closureFieldContainingType == null) continue;
          if (!TypeHelper.TypesAreEquivalent(closureFieldContainingType, closureType)) continue;
          if (this.remover.capturedBinding.ContainsKey(unspecializedClosureField.InternedKey)) continue;
          var thisReference = assignment.Source as IThisReference;
          if (thisReference == null) {
            var/*?*/ binding = assignment.Source as IBoundExpression;
            //if (binding == null) {
            //  //The closure is capturing a local that is defined in the block being closed over. Need to introduce the local.
            //  var newLocal = new LocalDefinition() {
            //    Name = closureField.Name,
            //    Type = closureField.Type,
            //  };
            //  var newLocalDecl = new LocalDeclarationStatement() { LocalVariable = newLocal, InitialValue = assignment.Source };
            //  statements[j] = newLocalDecl;
            //  if (this.remover.sourceMethodBody.privateHelperFieldsToRemove == null)
            //    this.remover.sourceMethodBody.privateHelperFieldsToRemove = new Dictionary<IFieldDefinition, IFieldDefinition>();
            //  this.remover.sourceMethodBody.privateHelperFieldsToRemove[unspecializedClosureField.ResolvedField] = unspecializedClosureField.ResolvedField;
            //  this.remover.capturedBinding.Add(unspecializedClosureField.InternedKey, new BoundExpression() { Definition = newLocal, Type = newLocal.Type });
            //  continue;
            //}
            if (binding != null && (binding.Definition is IParameterDefinition || binding.Definition is ILocalDefinition)) {
              var p = binding.Definition as IParameterDefinition;
              if (p != null) {
                if (closureField.Name != p.Name) {
                  continue;
                } else {
                  this.remover.capturedBinding[unspecializedClosureField.InternedKey] = binding;
                }
              } else {
                // must be a local
                var l = binding.Definition as ILocalDefinition;
                if (closureField.Name != l.Name) {
                  // Check to see if it is closureLocal.f := other_closure_local
                  // If so, delete it.
                  var sourceLoc = ExpressionAsLocal(assignment.Source);
                  if (sourceLoc != null && (this.remover.currentClosureLocals.ContainsKey(sourceLoc) || this.exceptionContainer == sourceLoc)) {
                    statements.RemoveAt(j--);
                    if (this.exceptionContainer == sourceLoc)
                      this.remover.capturedBinding[unspecializedClosureField.InternedKey] = binding;
                  }
                  continue;
                } else {
                  this.remover.capturedBinding[unspecializedClosureField.InternedKey] = binding;
                }
              }
            } else if (binding != null && fieldRef != null) {
              //In this case the closure is inside an iterator and its closure fields get their values from iterator state class fields or expressions.
              //In the former case, arrange for all references to the closure field to become references to the corresponding iterator state field.
              //In the latter case, the loop below will introduce a local to hold the value of the expression and arrange for references to the closure
              //field to become a reference to the local.
              IFieldReference iteratorField = binding.Definition as IFieldReference;
              if (iteratorField != null && binding.Instance is IThisReference) {
                this.remover.capturedBinding[unspecializedClosureField.InternedKey] = binding;
              } else
                continue;
            } else if (binding != null) {
              //In this case the closure is inside another closure and the closure fields get their values from the fields of the outer closure.
              IFieldReference outerClosureField = binding.Definition as IFieldReference;
              if (outerClosureField != null && binding.Instance is IThisReference) {
                this.remover.capturedBinding[unspecializedClosureField.InternedKey] = binding;
              } else
                continue;
            } else {
              // Corner case: see note above
              LocalDefinition localDefinition = new LocalDefinition() {
                Name = closureField.ResolvedField.Name,
                Type = this.remover.genericParameterMapper == null ? closureField.Type : this.remover.genericParameterMapper.Visit(closureField.Type),
              };
              LocalDeclarationStatement localDeclStatement = new LocalDeclarationStatement() {
                LocalVariable = localDefinition,
                InitialValue = assignment.Source,
              };
              statements.Insert(j, localDeclStatement); j++;
              this.remover.capturedBinding[unspecializedClosureField.InternedKey] = new BoundExpression() { Definition = localDefinition };
              if (this.remover.sourceMethodBody.privateHelperFieldsToRemove == null)
                this.remover.sourceMethodBody.privateHelperFieldsToRemove = new Dictionary<IFieldDefinition, IFieldDefinition>();
              this.remover.sourceMethodBody.privateHelperFieldsToRemove[closureField.ResolvedField] = closureField.ResolvedField;
            }
          } else {
            this.remover.capturedBinding[unspecializedClosureField.InternedKey] = new BoundExpression() { Instance = thisReference };
          }
          statements.RemoveAt(j--);
        }

        foreach (var field in closureType.ResolvedType.Fields) {
          if (this.remover.capturedBinding.ContainsKey(field.InternedKey)) continue;
          var newLocal = new LocalDefinition() {
            Name = field.Name,
            Type = this.remover.genericParameterMapper == null ? field.Type : this.remover.genericParameterMapper.Visit(field.Type),
          };
          var newLocalDecl = new LocalDeclarationStatement() { LocalVariable = newLocal };
          statements.Insert(i - 1, newLocalDecl);
          if (this.remover.sourceMethodBody.privateHelperFieldsToRemove == null)
            this.remover.sourceMethodBody.privateHelperFieldsToRemove = new Dictionary<IFieldDefinition, IFieldDefinition>();
          this.remover.sourceMethodBody.privateHelperFieldsToRemove[field] = field;
          this.remover.capturedBinding.Add(field.InternedKey, new BoundExpression() { Definition = newLocal, Type = newLocal.Type });
        }
      }

      private ILocalDefinition/*?*/ TargetExpressionAsLocal(ITargetExpression targetExpression) {
        if (targetExpression.Instance != null) return null;
        return targetExpression.Definition as ILocalDefinition;
      }

      private ILocalDefinition/*?*/ ExpressionAsLocal(IExpression expression) {
        var boundExpression = expression as IBoundExpression;
        if (boundExpression == null) return null;
        if (boundExpression.Instance != null) return null;
        return boundExpression.Definition as ILocalDefinition;
      }

    }

    private void RecursivelyFindCapturedLocals(BlockStatement blockStatement) {
      new CapturedLocalsFinder(this).Traverse(blockStatement);
    }

    public override IExpression Visit(BoundExpression boundExpression) {
      ILocalDefinition/*?*/ local = boundExpression.Definition as ILocalDefinition;
      if (local != null) {
        IExpression substitute = boundExpression;
        if (this.expressionToSubstituteForCompilerGeneratedSingleAssignmentLocal.TryGetValue(local, out substitute)) {
          this.sourceMethodBody.numberOfReferences[local]--;
          return substitute;
        }
      }
      IFieldReference/*?*/ closureField = boundExpression.Definition as IFieldReference;
      if (closureField != null) {
        var unspecializedClosureField = UnspecializedMethods.UnspecializedFieldReference(closureField);
        IBoundExpression/*?*/ binding = null;
        if (this.capturedBinding.TryGetValue(unspecializedClosureField.InternedKey, out binding)) {
          IThisReference thisReference = binding.Instance as IThisReference;
          if (thisReference != null && binding.Definition is Dummy) return thisReference;
          boundExpression.Definition = binding.Definition;
          boundExpression.Instance = binding.Instance;
          return boundExpression;
        }
      }
      IParameterDefinition/*?*/ parameter = boundExpression.Definition as IParameterDefinition;
      if (parameter != null) {
        IParameterDefinition parToSubstitute;
        if (this.parameterMap.TryGetValue(parameter, out parToSubstitute)) {
          boundExpression.Definition = parToSubstitute;
          return boundExpression;
        }
      }
      return base.Visit(boundExpression);
    }

    public override IExpression Visit(CreateDelegateInstance createDelegateInstance) {
      IMethodDefinition delegateMethodDefinition = createDelegateInstance.MethodToCallViaDelegate.ResolvedMethod;
      delegateMethodDefinition = UnspecializedMethods.UnspecializedMethodDefinition(delegateMethodDefinition);
      ITypeReference delegateContainingType = createDelegateInstance.MethodToCallViaDelegate.ContainingType;
      delegateContainingType = UnspecializedMethods.AsUnspecializedTypeReference(delegateContainingType);
      INestedTypeDefinition/*?*/ dctnt = delegateContainingType.ResolvedType as INestedTypeDefinition;
      ITypeDefinition/*?*/ dctct = dctnt == null ? null : dctnt.ContainingTypeDefinition;
      if (this.restoreAnonymousDelegates &&
        (TypeHelper.TypesAreEquivalent(delegateContainingType.ResolvedType, this.containingType) || TypeHelper.TypesAreEquivalent(dctct, this.containingType)) &&
        UnspecializedMethods.IsCompilerGenerated(delegateMethodDefinition))
        return ConvertToAnonymousDelegate(createDelegateInstance, iteratorsHaveNotBeenDecompiled: false);
      // REVIEW: This is needed only when iterators are *not* decompiled. When they are, then that happens before this phase and the create delegate instances
      // have been moved into the iterator method (from the MoveNext method) so the above pattern catches everything.
      if (this.restoreAnonymousDelegates && UnspecializedMethods.IsCompilerGenerated(delegateMethodDefinition)) {
        var containingTypeAsNestedType = this.containingType as INestedTypeDefinition;
        if (containingTypeAsNestedType != null && dctnt != null && TypeHelper.TypesAreEquivalent(dctnt.ContainingType, containingTypeAsNestedType.ContainingType)) {
          return ConvertToAnonymousDelegate(createDelegateInstance, iteratorsHaveNotBeenDecompiled: true);
        }
      }
      return base.Visit(createDelegateInstance);
    }

    private IExpression ConvertToAnonymousDelegate(CreateDelegateInstance createDelegateInstance, bool iteratorsHaveNotBeenDecompiled) {
      IMethodDefinition closureMethod = createDelegateInstance.MethodToCallViaDelegate.ResolvedMethod;
      if (this.sourceMethodBody.privateHelperMethodsToRemove == null) this.sourceMethodBody.privateHelperMethodsToRemove = new Dictionary<uint, IMethodDefinition>();
      IMethodBody closureMethodBody = UnspecializedMethods.GetMethodBodyFromUnspecializedVersion(closureMethod);
      AnonymousDelegate anonDel = new AnonymousDelegate();
      anonDel.CallingConvention = closureMethod.CallingConvention;
      var unspecializedClosureMethod = UnspecializedMethods.UnspecializedMethodDefinition(closureMethod);
      this.sourceMethodBody.privateHelperMethodsToRemove[unspecializedClosureMethod.InternedKey] = unspecializedClosureMethod;
      anonDel.Parameters = new List<IParameterDefinition>(unspecializedClosureMethod.Parameters);
      for (int i = 0, n = anonDel.Parameters.Count; i < n; i++) {
        IParameterDefinition closureMethodPar = UnspecializedParameterDefinition(anonDel.Parameters[i]);
        ParameterDefinition par = new ParameterDefinition();
        this.parameterMap.Add(closureMethodPar, par);
        par.Copy(closureMethodPar, this.host.InternFactory);
        par.ContainingSignature = anonDel;
        anonDel.Parameters[i] = par;
      }
      var alreadyDecompiledBody = closureMethodBody as SourceMethodBody;
      ISourceMethodBody anonDelSourceMethodBody = alreadyDecompiledBody;
      if (alreadyDecompiledBody == null) {
        var alreadyDecompiledBody2 = closureMethodBody as Microsoft.Cci.MutableCodeModel.SourceMethodBody;
        if (alreadyDecompiledBody2 == null) {
          var smb = new SourceMethodBody(closureMethodBody, this.sourceMethodBody.host,
            this.sourceMethodBody.sourceLocationProvider, this.sourceMethodBody.localScopeProvider, this.sourceMethodBody.options);
          anonDelSourceMethodBody = smb;
          anonDel.Body = smb.Block;
        } else {
          anonDel.Body = alreadyDecompiledBody2.Block;
          anonDelSourceMethodBody = alreadyDecompiledBody2;
        }
      } else {
        anonDel.Body = alreadyDecompiledBody.Block;
      }

      anonDel.ReturnValueIsByRef = closureMethod.ReturnValueIsByRef;
      if (closureMethod.ReturnValueIsModified)
        anonDel.ReturnValueCustomModifiers = new List<ICustomModifier>(closureMethod.ReturnValueCustomModifiers);
      anonDel.ReturnType = closureMethod.Type;
      anonDel.Type = createDelegateInstance.Type;

      if (iteratorsHaveNotBeenDecompiled && unspecializedClosureMethod.ContainingTypeDefinition.IsGeneric &&
        unspecializedClosureMethod.ContainingTypeDefinition.GenericParameterCount ==
        this.sourceMethodBody.MethodDefinition.ContainingTypeDefinition.GenericParameterCount) {
        var mapper = new GenericTypeParameterMapper(this.host, this.sourceMethodBody.MethodDefinition.ContainingTypeDefinition,
          unspecializedClosureMethod.ContainingTypeDefinition);
        mapper.Rewrite(anonDel);
      }

      BlockStatement bs = anonDel.Body as BlockStatement;
      if (bs != null) {
        var savedReferencedLabels = this.referencedLabels;
        this.referencedLabels = null;
        anonDel.Body = this.RemoveCompilationArtifacts(bs);
        this.referencedLabels = savedReferencedLabels;
      }
      IExpression result = anonDel;

      if (this.sourceMethodBody.MethodDefinition.IsGeneric) {
        if (unspecializedClosureMethod.IsGeneric)
          this.genericParameterMapper = new GenericMethodParameterMapper(this.host, this.sourceMethodBody.MethodDefinition, unspecializedClosureMethod);
        // If the closure method was not generic, then its containing type is generic
        // and the generic parameter mapper was created when the closure instance creation
        // was discovered at the beginning of this visitor.
        if (this.genericParameterMapper != null) {
          result = this.genericParameterMapper.Visit(result);
          foreach (var v in this.capturedBinding.Values) {
            // Do NOT visit any of the parameters in the table because that
            // will cause them to (possibly) have their types changed. But
            // they already have the correct type because they are parameters
            // of the enclosing method.
            // But the locals are ones that were created by this visitor so
            // they need their types updated.
            LocalDefinition ld = v.Definition as LocalDefinition;
            if (ld != null) {
              ld.Type = this.genericParameterMapper.Visit(ld.Type);
              ld.MethodDefinition = this.sourceMethodBody.MethodDefinition;
            }
          }
        }
      }

      return result;
    }

    /// <summary>
    /// Given a parameter definition, if it is a specialized parameter definition, get the unspecialized version, or
    /// otherwise return the parameter itself. 
    /// </summary>
    /// <param name="parameter"></param>
    /// <returns></returns>
    internal static IParameterDefinition UnspecializedParameterDefinition(IParameterDefinition parameter) {
      var specializedParameter = parameter as Immutable.SpecializedParameterDefinition;
      if (specializedParameter != null) {
        return UnspecializedParameterDefinition(specializedParameter.PartiallySpecializedParameter);
      }
      return parameter;
    }

    public override IExpression Visit(CreateObjectInstance createObjectInstance) {
      if (createObjectInstance.Arguments.Count == 2) {
        AddressOf/*?*/ aexpr = createObjectInstance.Arguments[1] as AddressOf;
        if (aexpr != null && aexpr.Expression.Definition is IMethodReference) {
          CreateDelegateInstance createDel = new CreateDelegateInstance();
          createDel.Instance = createObjectInstance.Arguments[0];
          createDel.IsVirtualDelegate = aexpr.Expression.Instance != null;
          createDel.MethodToCallViaDelegate = (IMethodReference)aexpr.Expression.Definition;
          createDel.Locations = createObjectInstance.Locations;
          createDel.Type = createObjectInstance.Type;
          return this.Visit(createDel);
        }
      }
      return base.Visit(createObjectInstance);
    }

    public override IExpression Visit(Equality equality) {
      if (equality.LeftOperand.Type.TypeCode == PrimitiveTypeCode.Boolean) {
        if (ExpressionHelper.IsIntegralZero(equality.RightOperand))
          return PatternDecompiler.InvertCondition(this.Visit(equality.LeftOperand));
      }
      return base.Visit(equality);
    }

    public override IExpression Visit(GreaterThan greaterThan) {
      var castIfPossible = greaterThan.LeftOperand as ICastIfPossible;
      if (castIfPossible != null) {
        var compileTimeConstant = greaterThan.RightOperand as ICompileTimeConstant;
        if (compileTimeConstant != null && compileTimeConstant.Value == null) {
          return this.Visit(new CheckIfInstance() {
            Operand = castIfPossible.ValueToCast,
            TypeToCheck = castIfPossible.TargetType,
            Type = greaterThan.Type,
            Locations = greaterThan.Locations
          });
        }
      }
      castIfPossible = greaterThan.RightOperand as ICastIfPossible;
      if (castIfPossible != null) {
        var compileTimeConstant = greaterThan.LeftOperand as ICompileTimeConstant;
        if (compileTimeConstant != null && compileTimeConstant.Value == null) {
          return this.Visit(new CheckIfInstance() {
            Operand = castIfPossible.ValueToCast,
            TypeToCheck = castIfPossible.TargetType,
            Type = greaterThan.Type,
            Locations = greaterThan.Locations
          });
        }
      }
      return base.Visit(greaterThan);
    }

    public override IStatement Visit(LabeledStatement labeledStatement) {
      if (!this.referencedLabels.ContainsKey(labeledStatement.Label.UniqueKey))
        return CodeDummy.Block;
      return base.Visit(labeledStatement);
    }

    public override IStatement Visit(LocalDeclarationStatement localDeclarationStatement) {
      ILocalDefinition local = localDeclarationStatement.LocalVariable;
      if (0 < this.currentClosureLocals.Count) {
        //if this temp was introduced to hold a copy of the closure local, then delete it
        var deleteIt = false;
        foreach (var currentClosureLocal in this.currentClosureLocals.Keys) {
          if (TypeHelper.TypesAreEquivalent(currentClosureLocal.Type, local.Type)) {
            deleteIt = true;
            break;
          }
          // Or it might be that we are visiting the method that is being turned back into a lambda
          // and it might be a temp introduced to hold "this" because of decompilation inadequacies.
          // (E.g., if there was "f++" operator in the lambda for a captured local/parameter, then
          // it becomes "ldarg0; dup" in the IL in the closure class and that decompiles to
          // "t1 := this; t2 := t1; t1.f := t2.f + 1".
          ITypeReference t1 = UnspecializedMethods.AsUnspecializedTypeReference(currentClosureLocal.Type);
          ITypeReference t2 = UnspecializedMethods.AsUnspecializedTypeReference(local.Type);
          if (t1 == t2) {
            deleteIt = true;
            break;
          }
        }
        if (deleteIt) {
          if (localDeclarationStatement.InitialValue == null) {
            // then need to delete all assignments to this local because they are the
            // real initialization.
            this.currentClosureLocals[local] = true;
          }
          return CodeDummy.Block;
        }

      }
      base.Visit(localDeclarationStatement);
      int numberOfAssignments = 0;
      if (!this.sourceMethodBody.numberOfAssignments.TryGetValue(local, out numberOfAssignments) || numberOfAssignments > 1)
        return localDeclarationStatement;
      int numReferences = 0;
      this.sourceMethodBody.numberOfReferences.TryGetValue(local, out numReferences);
      if (this.sourceMethodBody.sourceLocationProvider != null) {
        bool isCompilerGenerated = false;
        this.sourceMethodBody.sourceLocationProvider.GetSourceNameFor(local, out isCompilerGenerated);
        if (!isCompilerGenerated) return localDeclarationStatement;
      }
      var val = localDeclarationStatement.InitialValue;
      if (val is CompileTimeConstant || val is TypeOf || val is ThisReference) {
        this.expressionToSubstituteForCompilerGeneratedSingleAssignmentLocal.Add(local, val);
        return CodeDummy.Block; //Causes the caller to omit this statement from the containing statement list.
      }
      if (numReferences == 0 && val == null) return CodeDummy.Block; //unused declaration
      return localDeclarationStatement;
    }

    public override IExpression Visit(LogicalNot logicalNot) {
      if (logicalNot.Type == Dummy.TypeReference)
        return PatternDecompiler.InvertCondition(this.Visit(logicalNot.Operand));
      else if (logicalNot.Operand.Type.TypeCode == PrimitiveTypeCode.Int32)
        return new Equality() {
          LeftOperand = this.Visit(logicalNot.Operand),
          RightOperand = new CompileTimeConstant() { Value = 0, Type = this.host.PlatformType.SystemInt32 },
          Type = this.host.PlatformType.SystemBoolean,
          Locations = logicalNot.Locations
        };
      else {
        var castIfPossible = logicalNot.Operand as CastIfPossible;
        if (castIfPossible != null) {
          var operand = new CheckIfInstance() {
            Locations = castIfPossible.Locations,
            Operand = castIfPossible.ValueToCast,
            Type = this.host.PlatformType.SystemBoolean,
            TypeToCheck = castIfPossible.TargetType,
          };
          logicalNot.Operand = operand;
          return logicalNot;
        }
        return base.Visit(logicalNot);
      }
    }

    public override IExpression Visit(MethodCall methodCall) {
      if (methodCall.Arguments.Count == 1) {
        var tokenOf = methodCall.Arguments[0] as TokenOf;
        if (tokenOf != null) {
          var typeRef = tokenOf.Definition as ITypeReference;
          if (typeRef != null && methodCall.MethodToCall.InternedKey == this.GetTypeFromHandle.InternedKey) {
            return new TypeOf() { Locations = methodCall.Locations, Type = methodCall.Type, TypeToGet = typeRef };
          }
        }
      }
      return base.Visit(methodCall);
    }

    public override IExpression Visit(NotEquality notEquality) {
      base.Visit(notEquality);
      var cc1 = notEquality.LeftOperand as CompileTimeConstant;
      var cc2 = notEquality.RightOperand as CompileTimeConstant;
      if (cc1 != null && cc2 != null) {
        if (cc1.Type.TypeCode == PrimitiveTypeCode.Int32 && cc2.Type.TypeCode == PrimitiveTypeCode.Int32)
          return new CompileTimeConstant() { Value = ((int)cc1.Value) != ((int)cc2.Value), Type = notEquality.Type };
      }
      return notEquality;
    }

    /// <summary>
    /// A reference to System.Type.GetTypeFromHandle(System.Runtime.TypeHandle).
    /// </summary>
    IMethodReference GetTypeFromHandle {
      get {
        if (this.getTypeFromHandle == null) {
          this.getTypeFromHandle = new MethodReference(this.host, this.host.PlatformType.SystemType, CallingConvention.Default, this.host.PlatformType.SystemType,
          this.host.NameTable.GetNameFor("GetTypeFromHandle"), 0, this.host.PlatformType.SystemRuntimeTypeHandle);
        }
        return this.getTypeFromHandle;
      }
    }
    IMethodReference/*?*/ getTypeFromHandle;

    public override IStatement Visit(ReturnStatement returnStatement) {
      if (returnStatement.Expression != null) {
        returnStatement.Expression = this.Visit(returnStatement.Expression);
      }
      if (this.sourceMethodBody.MethodDefinition.Type.TypeCode == PrimitiveTypeCode.Boolean) {
        CompileTimeConstant/*?*/ cc = returnStatement.Expression as CompileTimeConstant;
        if (cc != null) {
          if (ExpressionHelper.IsIntegralZero(cc))
            cc.Value = false;
          else
            cc.Value = true;
          cc.Type = this.containingType.PlatformType.SystemBoolean;
        }
      }
      return returnStatement;
    }

  }

  internal class CachedDelegateRemover : MethodBodyCodeMutator {

    public static IBlockStatement RemoveCachedDelegates(IMetadataHost host, IBlockStatement blockStatement, SourceMethodBody sourceMethodBody) {
      var finder = new FindAssignmentToCachedDelegateStaticFieldOrLocal(sourceMethodBody);
      finder.Traverse(blockStatement);
      if (finder.cachedDelegateFieldsOrLocals.Count == 0) return blockStatement;
      var mutator = new CachedDelegateRemover(host, finder.cachedDelegateFieldsOrLocals);
      return mutator.Visit(blockStatement);
    }

    private CachedDelegateRemover(IMetadataHost host, Dictionary<string, AnonymousDelegate> cachedDelegateFieldsOrLocals)
      : base(host, true) {
      this.cachedDelegateFieldsOrLocals = cachedDelegateFieldsOrLocals;
    }

    Dictionary<string, AnonymousDelegate> cachedDelegateFieldsOrLocals;
    static string CachedDelegateId = "CachedAnonymousMethodDelegate";

    public override IExpression Visit(AnonymousDelegate anonymousDelegate) {
      return anonymousDelegate;
    }

    public override IExpression Visit(BoundExpression boundExpression) {
      var fieldReference = boundExpression.Definition as IFieldReference;
      if (fieldReference != null) {
        if (this.cachedDelegateFieldsOrLocals.ContainsKey(fieldReference.Name.Value))
          return this.cachedDelegateFieldsOrLocals[fieldReference.Name.Value];
        else
          return boundExpression;
      }
      var localDefinition = boundExpression.Definition as ILocalDefinition;
      if (localDefinition != null) {
        if (this.cachedDelegateFieldsOrLocals.ContainsKey(localDefinition.Name.Value))
          return this.cachedDelegateFieldsOrLocals[localDefinition.Name.Value];
        else
          return boundExpression;
      }
      return base.Visit(boundExpression);
    }

    public override IStatement Visit(ConditionalStatement conditionalStatement) {
      var condition = conditionalStatement.Condition;
      var logicalNot = condition as ILogicalNot;
      if (logicalNot != null) condition = logicalNot.Operand;
      var equal = condition as IEquality;
      if (equal != null && equal.RightOperand is IDefaultValue) condition = equal.LeftOperand;
      var boundExpression = condition as IBoundExpression;
      if (boundExpression != null) {
        var locations = conditionalStatement.Locations;
        var fieldReference = boundExpression.Definition as IFieldReference;
        if (fieldReference != null) {
          if (this.cachedDelegateFieldsOrLocals.ContainsKey(fieldReference.Name.Value)) {
            if (locations.Count == 0)
              return CodeDummy.Block;
            else
              return new EmptyStatement() { Locations = locations, };
          }
        }
        var localDefinition = boundExpression.Definition as ILocalDefinition;
        if (localDefinition != null) {
          if (this.cachedDelegateFieldsOrLocals.ContainsKey(localDefinition.Name.Value)) {
            if (locations.Count == 0)
              return CodeDummy.Block;
            else
              return new EmptyStatement() { Locations = locations, };
          }
        }
      }
      return base.Visit(conditionalStatement);
    }

    public override IStatement Visit(ExpressionStatement expressionStatement) {
      var assignment = expressionStatement.Expression as IAssignment;
      if (assignment == null) return base.Visit(expressionStatement); // need to look for method calls
      var lambda = assignment.Source as AnonymousDelegate;
      if (lambda == null) return base.Visit(expressionStatement);
      // but otherwise, we know no sub-expression of this assignment are interesting for this visitor
      // so don't do the base call.
      var fieldReference = assignment.Target.Definition as IFieldReference;
      if (fieldReference != null) {
        if (this.cachedDelegateFieldsOrLocals.ContainsKey(fieldReference.Name.Value))
          return CodeDummy.Block;
        else
          return expressionStatement;
      }
      var localDefinition = assignment.Target.Definition as ILocalDefinition;
      if (localDefinition == null) return expressionStatement;
      if (this.cachedDelegateFieldsOrLocals.ContainsKey(localDefinition.Name.Value))
        return CodeDummy.Block;
      else
        return expressionStatement;
    }

    public override IStatement Visit(LocalDeclarationStatement localDeclarationStatement) {
      var localDefinition = localDeclarationStatement.LocalVariable;
      if (this.cachedDelegateFieldsOrLocals.ContainsKey(localDefinition.Name.Value))
        return CodeDummy.Block;
      else
        return base.Visit(localDeclarationStatement);
    }

    class FindAssignmentToCachedDelegateStaticFieldOrLocal : CodeTraverser {

      internal FindAssignmentToCachedDelegateStaticFieldOrLocal(SourceMethodBody sourceMethodBody) {
        this.sourceMethodBody = sourceMethodBody;
      }

      SourceMethodBody sourceMethodBody;

      public Dictionary<string, AnonymousDelegate> cachedDelegateFieldsOrLocals = new Dictionary<string, AnonymousDelegate>();

      /// <summary>
      /// Need to look for the pattern "if (!loc) then {loc = lambda;} else nop;" (or field instead of "loc")
      /// instead of looking for just assignments of lambdas to locals (or fields). The latter leads to the
      /// mis-identification of user-written code that assigns labmdas to locals (or fields).
      /// </summary>
      public override void TraverseChildren(IConditionalStatement conditionalStatement) {
        if (!(conditionalStatement.FalseBranch is IEmptyStatement)) goto JustTraverse;
        var b = conditionalStatement.TrueBranch as BlockStatement;
        if (b == null) goto JustTraverse;
        if (b.Statements.Count != 1) goto JustTraverse;
        var s = b.Statements[0] as IExpressionStatement;
        if (s == null) goto JustTraverse;
        var assignment = s.Expression as IAssignment;
        if (assignment == null) goto JustTraverse;
        AnonymousDelegate lambda = assignment.Source as AnonymousDelegate;
        if (lambda == null) goto JustTraverse;
        IFieldReference/*?*/ fieldReference = assignment.Target.Definition as IFieldReference;
        if (fieldReference != null) {
          if (UnspecializedMethods.IsCompilerGenerated(fieldReference)
            && fieldReference.Name.Value.Contains(CachedDelegateId)) {
            this.cachedDelegateFieldsOrLocals[fieldReference.Name.Value] = lambda;
            if (this.sourceMethodBody.privateHelperFieldsToRemove == null)
              this.sourceMethodBody.privateHelperFieldsToRemove = new Dictionary<IFieldDefinition, IFieldDefinition>();
            this.sourceMethodBody.privateHelperFieldsToRemove.Add(fieldReference.ResolvedField, fieldReference.ResolvedField);
          }
          return;
        }
        ILocalDefinition/*?*/ localDefinition = assignment.Target.Definition as ILocalDefinition;
        if (localDefinition != null) {
          this.cachedDelegateFieldsOrLocals[localDefinition.Name.Value] = lambda;
          return;
        }
      JustTraverse:
        base.TraverseChildren(conditionalStatement);

      }
    }
  }

  internal class LabelReferenceFinder : CodeTraverser {

    internal Dictionary<int, bool> referencedLabels = new Dictionary<int, bool>();

    public override void TraverseChildren(IGotoStatement gotoStatement) {
      this.referencedLabels[gotoStatement.TargetStatement.Label.UniqueKey] = true;
    }

  }

  /// <summary>
  /// If the original method that contained the anonymous delegate is generic, then
  /// the code generated by the compiler, the "closure method", is also generic.
  /// If the anonymous delegate didn't capture any locals or parameters, then a
  /// (generic) static method was generated to implement the lambda.
  /// If it did capture things, then the closure method is a non-generic instance
  /// method in a generic class.
  /// In either case, any references to those generic parameters need to be mapped back
  /// to become references to the original method's generic parameters.
  /// Create an instance of this class for each anonymous delegate using the appropriate
  /// constructor. This is known from whether the closure method is (static and generic)
  /// or (instance and not-generic, but whose containing type is generic).
  /// Those are the only two patterns created by the compiler.
  /// </summary>
  internal class GenericMethodParameterMapper : CodeMutatingVisitor {

    /// <summary>
    /// The original generic method in which the anonymous delegate is being re-created.
    /// </summary>
    readonly IMethodDefinition targetMethod;
    ///// <summary>
    ///// The constructed reference to the targetMethod that is used within any
    ///// generated generic method parameter reference.
    ///// </summary>
    //readonly Microsoft.Cci.MutableCodeModel.MethodReference targetMethodReference;
    /// <summary>
    /// Just a short-cut to the generic parameters so the list can be created once
    /// and then the individual parameters can be accessed with an indexer.
    /// </summary>
    readonly List<IGenericMethodParameter> targetMethodGenericParameters;
    /// <summary>
    /// Used only when mapping from a generic method (i.e., a static closure method) to
    /// the original generic method.
    /// </summary>
    readonly IMethodDefinition/*?*/ sourceMethod;
    /// <summary>
    /// Used only when mapping from a method in a generic class (i.e., a closure class)
    /// to the original generic method.
    /// </summary>
    readonly INestedTypeReference/*?*/ sourceType;

    //^ Contract.Invariant((this.sourceMethod == null) != (this.sourceType == null));

    /// <summary>
    /// Use this constructor when the anonymous delegate did not capture any locals or parameters
    /// and so was implemented as a static, generic closure method.
    /// </summary>
    public GenericMethodParameterMapper(IMetadataHost host, IMethodDefinition targetMethod, IMethodDefinition sourceMethod)
      : this(host, targetMethod) {
      this.sourceMethod = sourceMethod;
    }
    /// <summary>
    /// Use this constructor when the anonymous delegate did capture a local or parameter
    /// and so was implemented as an instance, non-generic closure method within a generic
    /// class.
    /// </summary>
    public GenericMethodParameterMapper(IMetadataHost host, IMethodDefinition targetMethod, INestedTypeReference sourceType)
      : this(host, targetMethod) {
      this.sourceType = sourceType;
    }

    private GenericMethodParameterMapper(IMetadataHost host, IMethodDefinition targetMethod)
      : base(host) {
      this.targetMethod = targetMethod;
      this.targetMethodGenericParameters = new List<IGenericMethodParameter>(targetMethod.GenericParameters);
      //this.targetMethodReference = new Microsoft.Cci.MutableCodeModel.MethodReference();
      //this.targetMethodReference.Copy(this.targetMethod, this.host.InternFactory);
    }

    public override ITypeReference Visit(ITypeReference typeReference) {
      // No sense in doing this more than once
      ITypeReference result = null;
      IReference mappedTo = null;
      if (this.referenceCache.TryGetValue(typeReference, out mappedTo)) {
        result = (ITypeReference)mappedTo;
        return result;
      }
      if (this.sourceMethod != null) {
        IGenericMethodParameterReference gmpr = typeReference as IGenericMethodParameterReference;
        if (gmpr != null && gmpr.DefiningMethod == this.sourceMethod) {
          result = this.targetMethodGenericParameters[gmpr.Index];
        }
      } else { // this.sourceType != null
        IGenericTypeParameterReference gtpr = typeReference as IGenericTypeParameterReference;
        if (gtpr != null && gtpr.DefiningType == this.sourceType) {
          result = this.targetMethodGenericParameters[gtpr.Index];
        }
      }
      if (result == null)
        result = base.Visit(typeReference);
      // The base visit might have resulted in a copy (if typeReference is an immutable object)
      // in which case the base visit will already have put it into the cache (associated with
      // result). So need to check before adding it.
      if (!this.referenceCache.ContainsKey(typeReference))
        this.referenceCache.Add(typeReference, result);
      return result;
    }
  }

  /// <summary>
  /// If the original method that contained the anonymous delegate is comes from a generic type, then
  /// the code generated by the compiler, the "closure method", is also generic.
  /// The references to generic type parameters of the closure method must be mapped back
  /// to references to the generic type parameters of the containing type of the method containing
  /// the anonymous delegate.
  /// </summary>
  internal class GenericTypeParameterMapper : CodeRewriter {

    /// <summary>
    /// The original generic type in which the anonymous delegate is being re-created.
    /// </summary>
    readonly ITypeDefinition targetType;
    /// <summary>
    /// Just a short-cut to the generic parameters so the list can be created once
    /// and then the individual parameters can be accessed with an indexer.
    /// </summary>
    readonly List<IGenericTypeParameter> targetTypeGenericParameters;
    /// <summary>
    /// The generic closure class that contains the method being mapped to an anonymous delegate.
    /// </summary>
    readonly ITypeDefinition sourceType;

    /// <summary>
    /// Use this constructor when the anonymous delegate did not capture any locals or parameters
    /// and so was implemented as a static, generic closure method.
    /// </summary>
    public GenericTypeParameterMapper(IMetadataHost host, ITypeDefinition targetType, ITypeDefinition sourceType)
      : base(host) {
      this.targetType = targetType;
      this.sourceType = sourceType;
      this.targetTypeGenericParameters = new List<IGenericTypeParameter>(targetType.GenericParameters);
    }

    public override ITypeReference Rewrite(IGenericTypeParameterReference genericTypeParameterReference) {
      if (genericTypeParameterReference.DefiningType.ResolvedType == this.sourceType)
        return this.targetTypeGenericParameters[genericTypeParameterReference.Index];
      return base.Rewrite(genericTypeParameterReference);
    }

  }

}
