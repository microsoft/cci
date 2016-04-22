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
using System.Diagnostics.Contracts;
using Microsoft.Cci.UtilityDataStructures;
using System;
using System.IO;
using System.Text;

namespace Microsoft.Cci.ILToCodeModel {

  internal class PatternReplacer : CodeTraverser {

    internal PatternReplacer(SourceMethodBody sourceMethodBody, BlockStatement block) {
      Contract.Requires(sourceMethodBody != null);
      Contract.Requires(block != null);
      this.host = sourceMethodBody.host; Contract.Assume(this.host != null);
      this.nameTable = this.host.NameTable;
      this.gotosThatTarget = sourceMethodBody.gotosThatTarget; Contract.Assume(this.gotosThatTarget != null);
      this.numberOfAssignmentsToLocal = sourceMethodBody.numberOfAssignmentsToLocal; Contract.Assume(this.numberOfAssignmentsToLocal != null);
      this.numberOfReferencesToLocal = sourceMethodBody.numberOfReferencesToLocal; Contract.Assume(this.numberOfReferencesToLocal != null);
      this.bindingsThatMakeALastUseOfALocalVersion = sourceMethodBody.bindingsThatMakeALastUseOfALocalVersion; Contract.Assume(this.bindingsThatMakeALastUseOfALocalVersion != null);
      this.singleAssignmentReferenceFinder = new SingleAssignmentSingleReferenceFinder(this.bindingsThatMakeALastUseOfALocalVersion, this.numberOfReferencesToLocal);
      this.singleAssignmentLocalReplacer = new SingleAssignmentLocalReplacer(this.host, this.bindingsThatMakeALastUseOfALocalVersion, this.numberOfAssignmentsToLocal);
      this.sourceLocationProvider = sourceMethodBody.sourceLocationProvider;
      this.singleUseExpressionChecker = new SingleUseExpressionChecker();
      Contract.Assume(sourceMethodBody.ilMethodBody != null);
      this.isVoidMethod = sourceMethodBody.ilMethodBody.MethodDefinition.Type.TypeCode == PrimitiveTypeCode.Void;
      while (block != null) {
        var n = block.Statements.Count;
        if (n > 0) {
          var nestedBlock = block.Statements[n-1] as BlockStatement;
          if (nestedBlock != null) { block = nestedBlock; continue; }
        }
        if (n >= 2) { // && sourceMethodBody.ilMethodBody.MethodDefinition.Type.TypeCode != PrimitiveTypeCode.Void) {
          var labeledStatement = block.Statements[n-2] as LabeledStatement;
          if (labeledStatement == null && n >= 3 && block.Statements[n-2] is EmptyStatement) {
            labeledStatement = block.Statements[n-3] as LabeledStatement;
          }
          var returnStatement = block.Statements[n-1] as ReturnStatement;
          if (labeledStatement != null && returnStatement != null) {
            if (sourceMethodBody.ilMethodBody.MethodDefinition.Type.TypeCode != PrimitiveTypeCode.Void) {
              var boundExpression = returnStatement.Expression as BoundExpression;
              if (boundExpression != null) {
                this.returnValueTemp = boundExpression.Definition as ILocalDefinition;
                if (this.returnValueTemp != null) {
                  bool isCompilerGenerated = false;
                  if (this.sourceLocationProvider != null)
                    this.sourceLocationProvider.GetSourceNameFor(this.returnValueTemp, out isCompilerGenerated);
                  if (isCompilerGenerated) {
                    this.labelOfFinalReturn = labeledStatement.Label;
                    this.finalBlock = block;
                  } else {
                    this.returnValueTemp = null;
                  }
                }
              }
            } else {
              this.labelOfFinalReturn = labeledStatement.Label;
            }
          }
        }
        block = null; //TODO: putting a return here causes Clousot to conclude that the object invariant is never reached.
      }
    }

    IMetadataHost host;
    INameTable nameTable;
    Hashtable<List<IGotoStatement>> gotosThatTarget;
    HashtableForUintValues<object> numberOfAssignmentsToLocal;
    HashtableForUintValues<object> numberOfReferencesToLocal;
    SetOfObjects bindingsThatMakeALastUseOfALocalVersion;
    ILocalDefinition/*?*/ returnValueTemp;
    IName/*?*/ labelOfFinalReturn;
    SingleAssignmentSingleReferenceFinder singleAssignmentReferenceFinder;
    SingleAssignmentLocalReplacer singleAssignmentLocalReplacer;
    SingleUseExpressionChecker singleUseExpressionChecker;
    ISourceLocationProvider/*?*/ sourceLocationProvider;
    LabeledStatement labelImmediatelyFollowingCurrentBlock;
    IBlockStatement finalBlock;
    bool isVoidMethod;

    [ContractInvariantMethod]
    private void ObjectInvariant() {
      Contract.Invariant(this.host != null);
      Contract.Invariant(this.nameTable != null);
      Contract.Invariant(this.gotosThatTarget != null);
      Contract.Invariant(this.numberOfAssignmentsToLocal != null);
      Contract.Invariant(this.numberOfReferencesToLocal != null);
      Contract.Invariant(this.bindingsThatMakeALastUseOfALocalVersion != null);
      Contract.Invariant(this.singleAssignmentReferenceFinder != null);
      Contract.Invariant(this.singleAssignmentLocalReplacer != null);
      Contract.Invariant(this.singleUseExpressionChecker != null);
    }

    [ContractVerification(false)]
    public override void TraverseChildren(IBlockStatement block) {
      Contract.Assume(block is BlockStatement);
      var b = (BlockStatement)block;
      var savedLabelImmediatelyFollowingCurrentBlock = this.labelImmediatelyFollowingCurrentBlock;
      for (int i = 0, n = b.Statements.Count; i < n; i++) {
        var statement = b.Statements[i];
        Contract.Assume(statement != null);
        if (statement is BlockStatement) {
          if (i < n-1) {
            this.labelImmediatelyFollowingCurrentBlock = b.Statements[i+1] as LabeledStatement;
            if (this.labelImmediatelyFollowingCurrentBlock == null) {
              var blk = b.Statements[i+1] as BlockStatement;
              if (blk != null && blk.Statements.Count > 0)
                this.labelImmediatelyFollowingCurrentBlock = blk.Statements[0] as LabeledStatement;
            }
          } else {
            this.labelImmediatelyFollowingCurrentBlock = savedLabelImmediatelyFollowingCurrentBlock;
          }
        }
        this.Traverse(statement);
      }
      this.labelImmediatelyFollowingCurrentBlock = savedLabelImmediatelyFollowingCurrentBlock;

      while (this.ReplaceArrayInitializerPattern(b) ||
      this.ReplaceArrayInitializerPattern2(b) ||
      this.ReplaceConditionalExpressionPattern(b) ||
      this.ReplacePushPushDupPopPopPattern(b) ||
      this.ReplacePushDupPopPattern(b) ||
      this.ReplacePushDupPushPopPattern(b) ||
      ReplacePushPopPattern(b, this.host) ||
      this.ReplaceDupPopPattern(b) ||
      this.ReplacePostBinopPattern(b) ||
      this.ReplacePropertyBinopPattern(b) ||
      this.ReplaceReturnViaGoto(b) ||
      this.ReplaceReturnViaGotoInVoidMethods(b) ||
      this.ReplaceSelfAssignment(b) ||
      this.ReplaceShortCircuitAnd(b) ||
      this.ReplaceShortCircuitAnd2(b) ||
      this.ReplaceShortCircuitAnd3(b) ||
      this.ReplaceShortCircuitAnd4(b) ||
      this.ReplaceShortCircuitAnd5(b) ||
      this.ReplaceShortCircuitAnd6(b) ||
      this.ReplacedCompoundAssignmentViaTempPattern(b) ||
      this.ReplaceSingleUseCompilerGeneratedLocalPattern(b) ||
      this.ReplaceCompilerGeneratedLocalUsedForInitializersPattern(b) ||
      this.ReplacePlusAssignForStringPattern(b)
      ) {
      }

      //Look for a final return that returns a temp that is never assigned to.
      //Assuming the compiler did not allow unitialized variables, this return should be an unreachable compiler artifact.
      if (b == this.finalBlock) {
        var n = b.Statements.Count;
        Contract.Assume(n >= 2 && this.returnValueTemp != null);
        int statementsToRemove = 2;
        var labeledStatement = b.Statements[n-2] as LabeledStatement;
        if (labeledStatement == null && n >= 3 && b.Statements[n-2] is EmptyStatement) {
          labeledStatement = b.Statements[n-3] as LabeledStatement;
          statementsToRemove = 3;
        }
        var returnStatement = b.Statements[n-1] as ReturnStatement;
        Contract.Assume(labeledStatement != null && returnStatement != null);
        var boundExpression = returnStatement.Expression as BoundExpression;
        Contract.Assume(boundExpression != null);
        Contract.Assume(this.returnValueTemp == boundExpression.Definition);
        if (this.numberOfAssignmentsToLocal[this.returnValueTemp] == 0) {
          //Contract.Assume(this.numberOfReferencesToLocal[this.returnValueTemp] == 1);
          this.labelOfFinalReturn = labeledStatement.Label;
          for (int i = 0; i < n; i++) {
            var localDeclarationStatement = b.Statements[i] as LocalDeclarationStatement;
            if (localDeclarationStatement == null) return;
            if (localDeclarationStatement.LocalVariable != this.returnValueTemp) continue;
            b.Statements.RemoveAt(i);
            n--;
            break;
          }
          Contract.Assume(n >= statementsToRemove);
          b.Statements.RemoveRange(n-statementsToRemove, statementsToRemove);
        }
      }
    }

    private bool ReplaceArrayInitializerPattern(BlockStatement b) {
      Contract.Requires(b != null);
      bool replacedPattern = false;
      var statements = b.Statements;
      for (int i = 1; i < b.Statements.Count-1; i++) {
        var expressionStatement = statements[i] as ExpressionStatement;
        if (expressionStatement == null) continue;
        var call = expressionStatement.Expression as MethodCall;
        if (call == null) continue;
        if (call.MethodToCall.Name != this.InitializeArray) continue;
        var arguments = call.Arguments;
        if (arguments.Count != 2) continue;
        var arrayArg = arguments[0];
        if (!(arrayArg is IDupValue)) continue;
        var token = arguments[1] as TokenOf;
        if (token == null) continue;
        var field = token.Definition as IFieldDefinition;
        if (field == null || !field.IsMapped) continue;
        var pushArray = statements[i-1] as PushStatement;
        if (pushArray == null) continue;
        var createArray = pushArray.ValueToPush as CreateArray;
        if (createArray == null) continue;
        AddSizesAndArrayInitializers(createArray, field);
        var assignStatement = statements[i+1] as ExpressionStatement;
        if (assignStatement != null) {
          var assign = assignStatement.Expression as Assignment;
          if (assign != null && assign.Source is PopValue) {
            assign.Source = createArray;
            statements.RemoveRange(i-1, 2);
            continue;
          }
        }
        statements.RemoveAt(i);
        replacedPattern = true;
      }
      return replacedPattern;
    }

    static void AddSizesAndArrayInitializers(CreateArray createArray, IFieldDefinition initialValueField) {
      Contract.Requires(createArray != null);
      Contract.Requires(initialValueField != null);
      Contract.Requires(initialValueField.IsMapped);

      List<ulong> sizes = new List<ulong>();
      foreach (IExpression expr in createArray.Sizes) {
        IMetadataConstant mdc = expr as IMetadataConstant;
        if (mdc == null) return;
        sizes.Add(ConvertToUlong(mdc));
      }
      ITypeReference elemType = createArray.ElementType;
      MemoryStream memoryStream = new MemoryStream(new List<byte>(initialValueField.FieldMapping.Data).ToArray());
      BinaryReader reader = new BinaryReader(memoryStream, Encoding.Unicode);
      ulong flatSize = 1;
      foreach (ulong dimensionSize in sizes) flatSize *= dimensionSize;
      while (flatSize-- > 0) {
        CompileTimeConstant cc = new CompileTimeConstant();
        cc.Value = ReadValue(elemType.TypeCode, reader);
        cc.Type = elemType;
        createArray.Initializers.Add(cc);
      }
    }

    private static ulong ConvertToUlong(IMetadataConstant c) {
      Contract.Requires(c != null);
      IConvertible/*?*/ ic = c.Value as IConvertible;
      if (ic == null) return 0; //TODO: error
      switch (ic.GetTypeCode()) {
        case TypeCode.SByte:
        case TypeCode.Int16:
        case TypeCode.Int32:
        case TypeCode.Int64:
          return (ulong)ic.ToInt64(null); //TODO: error if < 0
        case TypeCode.Byte:
        case TypeCode.UInt16:
        case TypeCode.UInt32:
        case TypeCode.UInt64:
          return ic.ToUInt64(null);
      }
      return 0; //TODO: error
    }

    private static object ReadValue(PrimitiveTypeCode primitiveTypeCode, BinaryReader reader) {
      Contract.Requires(reader != null);
      switch (primitiveTypeCode) {
        case PrimitiveTypeCode.Boolean: return reader.ReadBoolean();
        case PrimitiveTypeCode.Char: return (char)reader.ReadUInt16();
        case PrimitiveTypeCode.Float32: return reader.ReadSingle();
        case PrimitiveTypeCode.Float64: return reader.ReadDouble();
        case PrimitiveTypeCode.Int16: return reader.ReadInt16();
        case PrimitiveTypeCode.Int32: return reader.ReadInt32();
        case PrimitiveTypeCode.Int64: return reader.ReadInt64();
        case PrimitiveTypeCode.Int8: return reader.ReadSByte();
        case PrimitiveTypeCode.UInt16: return reader.ReadUInt16();
        case PrimitiveTypeCode.UInt32: return reader.ReadUInt32();
        case PrimitiveTypeCode.UInt64: return reader.ReadUInt64();
        case PrimitiveTypeCode.UInt8: return reader.ReadByte();
        default:
          Contract.Assume(false);
          break;
      }
      return null;
    }

    IName InitializeArray {
      get {
        if (this.initializeArray == null)
          this.initializeArray = this.nameTable.GetNameFor("InitializeArray");
        return this.initializeArray;
      }
    }
    IName/*?*/ initializeArray;

    private bool ReplaceArrayInitializerPattern2(BlockStatement b) {
      Contract.Requires(b != null);
      bool replacedPattern = false;
      var statements = b.Statements;
      for (int i = 1; i < b.Statements.Count-1; i++) {
        var expressionStatement = statements[i] as ExpressionStatement;
        if (expressionStatement == null) continue;
        var assignment = expressionStatement.Expression as Assignment;
        if (assignment == null) continue;
        var targetLocal = assignment.Target.Definition as ILocalDefinition;
        if (targetLocal == null) continue;
        var createArray = assignment.Source as CreateArray;
        if (createArray == null || createArray.Initializers.Count > 0) continue;
        int j = 0;
        for (; j+i+1 < b.Statements.Count; j++) {
          var initializer = b.Statements[j+i+1] as ExpressionStatement;
          if (initializer == null) break;
          var initializingAssignment = initializer.Expression as Assignment;
          if (initializingAssignment == null) break;
          var target = initializingAssignment.Target;
          var targetBinding = target.Instance as BoundExpression;
          if (targetBinding != null) {
            if (targetBinding.Definition != targetLocal) break;
            var targetArrayIndex = target.Definition as ArrayIndexer;
            if (targetArrayIndex == null) break;
            if (j != ComputeFlatIndex(targetArrayIndex, createArray)) break;
          } else {
            var addressDeref = target.Definition as AddressDereference;
            if (addressDeref == null) break;
            var addressOf = addressDeref.Address as AddressOf;
            if (addressOf == null) break;
            var addressableExpression = addressOf.Expression;
            targetBinding = addressableExpression.Instance as BoundExpression;
            if (targetBinding == null) break;
            if (targetBinding.Definition != targetLocal) break;
            var targetArrayIndex = addressableExpression.Definition as ArrayIndexer;
            if (targetArrayIndex == null) break;
            if (j != ComputeFlatIndex(targetArrayIndex, createArray)) break;
          }
          createArray.Initializers.Add(initializingAssignment.Source);
        }
        if (j > 0) {
          this.numberOfReferencesToLocal[targetLocal] = (uint)(this.numberOfReferencesToLocal[targetLocal] - j);
          b.Statements.RemoveRange(i+1, j);
          replacedPattern = true;
        }
      }
      return replacedPattern;
    }

    private int ComputeFlatIndex(ArrayIndexer arrayIndexer, CreateArray createArray) {
      Contract.Requires(arrayIndexer != null);
      Contract.Requires(createArray != null);

      var n = arrayIndexer.Indices.Count;
      if (n == 1) {
        var indexValue = arrayIndexer.Indices[0] as CompileTimeConstant;
        if (indexValue == null || !(indexValue.Value is int)) return -1;
        return (int)indexValue.Value;
      }
      var result = 0;
      for (int i = 0; i < n; i++) {
        var targetIndexValue = arrayIndexer.Indices[i] as CompileTimeConstant;
        if (targetIndexValue == null || !(targetIndexValue.Value is int)) return -1;
        if (i > 0) {
          if (i >= createArray.Sizes.Count) return -1;
          var sizeConst = createArray.Sizes[i] as CompileTimeConstant;
          if (sizeConst == null || !(sizeConst.Value is int)) return -1;
          result *= (int)sizeConst.Value;
        }
        if (i < createArray.LowerBounds.Count)
          result -= createArray.LowerBounds[i];
        result += (int)targetIndexValue.Value;
      }
      return result;
    }

    private bool ReplaceConditionalExpressionPattern(BlockStatement b) {
      Contract.Requires(b != null);
      bool replacedPattern = false;
      var statements = b.Statements;
      for (int i = 0; i < statements.Count-6; i++) {
        var ifStatement = statements[i] as ConditionalStatement;
        if (ifStatement == null) continue;
        var gotoTrueCase = ifStatement.TrueBranch as GotoStatement;
        if (gotoTrueCase == null) continue;
        Contract.Assume(ifStatement.FalseBranch is EmptyStatement);
        var labeledStatement = statements[i+1] as LabeledStatement;
        if (labeledStatement == null) continue;
        var gotos = this.gotosThatTarget[(uint)labeledStatement.Label.UniqueKey];
        if (gotos != null && gotos.Count > 0) continue;
        var pushFalseCase = statements[i+2] as PushStatement;
        if (pushFalseCase == null) continue;
        var gotoEnd = statements[i+3] as GotoStatement;
        if (gotoEnd == null) continue;
        var labeledStatement2 = statements[i+4] as LabeledStatement;
        if (labeledStatement2 == null || labeledStatement2 != gotoTrueCase.TargetStatement) continue;
        gotos = this.gotosThatTarget[(uint)labeledStatement2.Label.UniqueKey];
        Contract.Assume(gotos != null && gotos.Count > 0);
        if (gotos.Count > 1) continue;
        var pushTrueCase = statements[i+5] as PushStatement;
        if (pushTrueCase == null) continue;
        var endLabel = statements[i+6] as LabeledStatement;
        if (endLabel == null || gotoEnd.TargetStatement != endLabel) continue;
        var conditional = new Conditional() { Condition = ifStatement.Condition, ResultIfTrue = pushTrueCase.ValueToPush, ResultIfFalse = pushFalseCase.ValueToPush };
        pushTrueCase.ValueToPush = TypeInferencer.FixUpType(conditional);
        pushTrueCase.Locations.AddRange(ifStatement.Locations);
        gotos = this.gotosThatTarget[(uint)endLabel.Label.UniqueKey];
        Contract.Assume(gotos != null && gotos.Count > 0);
        gotos.Remove(gotoEnd);
        if (gotos.Count == 0) statements.RemoveAt(i+6);
        statements.RemoveRange(i, 5);
        replacedPattern = true;
      }
      return replacedPattern;
    }

    private bool ReplaceDupPopPattern(BlockStatement b) {
      Contract.Requires(b != null);
      bool replacedPattern = false;
      var statements = b.Statements;
      for (int i = 0; i < statements.Count-1; i++) {
        var exprS1 = statements[i] as ExpressionStatement;
        if (exprS1 == null) continue;
        var assign1 = exprS1.Expression as Assignment;
        if (assign1 == null) continue;
        if (!(assign1.Source is IDupValue)) continue;
        var exprS2 = statements[i+1] as ExpressionStatement;
        if (exprS2 == null) continue;
        var assign2 = exprS2.Expression as Assignment;
        if (assign2 == null) continue;
        if (!(assign2.Source is IPopValue)) continue;
        assign1.Source = assign2.Source;
        assign2.Source = assign1;
        statements.RemoveAt(i);
        replacedPattern = true;
      }
      return replacedPattern;
    }

    private bool ReplacePostBinopPattern(BlockStatement b) {
      Contract.Requires(b != null);
      bool replacedPattern = false;
      var statements = b.Statements;
      for (int i = 0; i < statements.Count-2; i++) {
        var push1 = statements[i] as PushStatement;
        if (push1 == null) continue;
        var bound1 = push1.ValueToPush as BoundExpression;
        if (bound1 == null) continue;
        var exprS1 = statements[i+1] as ExpressionStatement;
        if (exprS1 == null) continue;
        var assign1 = exprS1.Expression as Assignment;
        if (assign1 == null) continue;
        if (bound1.Definition != assign1.Target.Definition) continue;
        if (bound1.Instance != assign1.Target.Instance) continue;
        var binop1 = assign1.Source as BinaryOperation;
        if (binop1 == null) continue;
        if (!(binop1.LeftOperand is IDupValue)) continue;
        binop1.LeftOperand = assign1.Target;
        binop1.ResultIsUnmodifiedLeftOperand = true;
        push1.ValueToPush = binop1;
        statements.RemoveAt(i+1);
        replacedPattern = true;
      }
      return replacedPattern;
    }

    private bool ReplacePropertyBinopPattern(BlockStatement b) {
      Contract.Requires(b != null);
      bool replacedPattern = false;
      var statements = b.Statements;
      for (int i = 0; i < statements.Count-1; i++) {
        var es1 = statements[i] as ExpressionStatement;
        if (es1 == null) continue;
        var setterCall = es1.Expression as MethodCall;
        if (setterCall == null || /*setterCall.IsStaticCall ||*/ setterCall.IsJumpCall || setterCall.Arguments.Count != 1) continue;
        var binaryOp = setterCall.Arguments[0] as BinaryOperation;
        if (binaryOp == null) continue;
        var getterCall = binaryOp.LeftOperand as MethodCall;
        if (getterCall == null || /*getterCall.IsStaticCall ||*/ getterCall.IsJumpCall || getterCall.Arguments.Count != 0) continue;
        if (!(setterCall.IsStaticCall == getterCall.IsStaticCall)) continue;
        if (!setterCall.IsStaticCall && !(getterCall.ThisArgument is IDupValue)) continue;
        var setterName = setterCall.MethodToCall.Name.Value;
        var getterName = getterCall.MethodToCall.Name.Value;
        if (setterName.Length != getterName.Length || setterName.Length <= 4) continue;
        if (string.CompareOrdinal(setterName, 4, getterName, 4, setterName.Length-4) != 0) continue;
        if (!setterName.StartsWith("set_") || !getterName.StartsWith("get_")) continue;
        var propertyNameStr = setterCall.MethodToCall.Name.Value.Substring(4);
        var propertyName = this.host.NameTable.GetNameFor(propertyNameStr);
        var property = new PropertyDefinition() {
          CallingConvention = setterCall.IsStaticCall ? CallingConvention.Default : CallingConvention.HasThis,
          ContainingTypeDefinition = getterCall.MethodToCall.ContainingType.ResolvedType,
          Name = propertyName, Getter = getterCall.MethodToCall, Setter = setterCall.MethodToCall, Type = getterCall.Type
        };
        binaryOp.LeftOperand = new TargetExpression() {
          Definition = property, Instance = setterCall.IsStaticCall ? null : setterCall.ThisArgument,
          GetterIsVirtual = getterCall.IsVirtualCall, SetterIsVirtual = setterCall.IsVirtualCall,
          Type = getterCall.Type
        };
        es1.Expression = binaryOp;
        replacedPattern = true;
      }
      return replacedPattern;
    }

    private bool ReplacedCompoundAssignmentViaTempPattern(BlockStatement b) {
      Contract.Requires(b != null);
      bool replacedPattern = false;
      var statements = b.Statements;
      for (int i = 0; i < statements.Count-1; i++) {
        var exprS1 = statements[i] as ExpressionStatement;
        if (exprS1 == null) continue;
        var assign1 = exprS1.Expression as Assignment;
        if (assign1 == null) continue;
        var assign2 = assign1;
        Assignment assign3 = null;
        ILocalDefinition temp = null;
        while (assign2 != null && assign3 == null) {
          assign3 = assign2.Source as Assignment;
          if (assign3 == null) break;
          temp = assign3.Target.Definition as ILocalDefinition;
          if (temp == null || assign3.Source is IPopValue) assign2 = null;
        }
        if (assign2 == null || assign3 == null) continue;
        var exprS2 = statements[i+1] as ExpressionStatement;
        if (exprS2 == null) continue;
        var assign4 = exprS2.Expression as Assignment;
        if (assign4 == null) continue;
        var boundExpr = assign4.Source as BoundExpression;
        if (boundExpr == null || boundExpr.Definition != temp) continue;
        if (this.bindingsThatMakeALastUseOfALocalVersion.Contains(boundExpr)) {
          assign2.Source = assign3.Source;
          this.numberOfAssignmentsToLocal[temp]--;
        }
        this.numberOfReferencesToLocal[temp]--;
        assign4.Source = assign1;
        statements.RemoveAt(i);
        replacedPattern = true;
      }
      return replacedPattern;
    }

    [ContractVerification(false)]
    private bool ReplacePushPushDupPopPopPattern(BlockStatement b) {
      Contract.Requires(b != null);
      bool replacedPattern = false;
      var statements = b.Statements;
      for (int i = 0; i < statements.Count-3; i++) {
        var push1 = statements[i] as PushStatement;
        if (push1 == null) continue;
        var push2 = statements[i+1] as PushStatement;
        if (push2 == null) continue;
        var exprS1 = statements[i+2] as ExpressionStatement;
        if (exprS1 == null) continue;
        var assign1 = exprS1.Expression as Assignment;
        if (assign1 == null) continue;
        if (!(assign1.Source is IDupValue)) continue;
        var exprS2 = statements[i+3] as ExpressionStatement;
        if (exprS2 == null) continue;
        var assign2 = exprS2.Expression as Assignment;
        if (assign2 == null) {
          var setterCall = exprS2.Expression as MethodCall;
          if (setterCall == null || setterCall.IsStaticCall || setterCall.IsJumpCall || setterCall.Arguments.Count != 1) continue;
          if (!(setterCall.ThisArgument is IPopValue) || !(setterCall.Arguments[0] is IPopValue)) continue;
          var binaryOp = push2.ValueToPush as BinaryOperation;
          if (binaryOp == null) continue;
          var getterCall = binaryOp.LeftOperand as MethodCall;
          if (getterCall == null || getterCall.IsStaticCall || getterCall.IsJumpCall || getterCall.Arguments.Count != 0) continue;
          if (!(getterCall.ThisArgument is IDupValue)) continue;
          var setterName = setterCall.MethodToCall.Name.Value;
          var getterName = getterCall.MethodToCall.Name.Value;
          if (setterName.Length != getterName.Length || setterName.Length <= 4) continue;
          if (string.CompareOrdinal(setterName, 4, getterName, 4, setterName.Length-4) != 0) continue;
          if (!setterName.StartsWith("set_") || !getterName.StartsWith("get_")) continue;
          var propertyNameStr = setterCall.MethodToCall.Name.Value.Substring(4);
          var propertyName = this.host.NameTable.GetNameFor(propertyNameStr);
          var property = new PropertyDefinition() {
            CallingConvention = Cci.CallingConvention.HasThis,
            Name = propertyName, Getter = getterCall.MethodToCall, Setter = setterCall.MethodToCall, Type = getterCall.Type
          };
          binaryOp.LeftOperand = new TargetExpression() { Definition = property, Instance = push1.ValueToPush, 
            GetterIsVirtual = getterCall.IsVirtualCall, SetterIsVirtual = setterCall.IsVirtualCall, Type = getterCall.Type };          
          assign1.Source = binaryOp;
          exprS1.Expression = assign1;
        } else {
          if (!(assign2.Source is IPopValue)) {
            if (!(assign2.Target.Instance is IPopValue)) {
              var addrDeref = assign2.Target.Definition as AddressDereference;
              if (addrDeref == null || !(addrDeref.Address is IPopValue)) continue;
              var binOp = assign2.Source as BinaryOperation;
              if (binOp == null) continue;
              if (!(binOp.LeftOperand is IPopValue)) continue;
              var constVal = binOp.RightOperand as ICompileTimeConstant;
              if (constVal == null) continue;
              var addrDeref2 = push2.ValueToPush as IAddressDereference;
              if (addrDeref2 == null) continue;
              if (!(addrDeref2.Address is IDupValue)) continue;
              var target2 = assign2.Target as TargetExpression;
              if (target2 == null) continue;
              addrDeref.Address = push1.ValueToPush;
              binOp.LeftOperand = target2;
              binOp.ResultIsUnmodifiedLeftOperand = true;
              assign1.Source = binOp;
              exprS1.Expression = assign1;
            } else {
              var binOp = assign2.Source as BinaryOperation;
              if (binOp == null) continue;
              if (!(binOp.LeftOperand is IPopValue)) continue;
              var constVal = binOp.RightOperand as ICompileTimeConstant;
              if (constVal == null) continue;
              var boundExpr = push2.ValueToPush as BoundExpression;
              if (boundExpr == null) continue;
              if (!(boundExpr.Instance is IDupValue)) continue;
              var target2 = assign2.Target as TargetExpression;
              if (target2 == null) continue;
              if (target2.Definition != boundExpr.Definition) continue;
              target2.Instance = push1.ValueToPush;
              binOp.LeftOperand = target2;
              binOp.ResultIsUnmodifiedLeftOperand = true;
              assign1.Source = binOp;
              exprS1.Expression = assign1;
            }
          } else {
            if (!(assign2.Target.Instance is IPopValue)) continue;
            if (assign2.Target.Definition is IArrayIndexer) continue;
            assign1.Source = assign2;
            assign2.Source = TypeInferencer.Convert(push2.ValueToPush, assign2.Target.Type);
            ((TargetExpression)assign2.Target).Instance = push1.ValueToPush;
            exprS1.Expression = this.CollapseOpAssign(assign1);
          }
        }
        statements[i] = exprS1;
        statements.RemoveRange(i+1, 3);
        replacedPattern = true;
      }
      return replacedPattern;
    }

    private IExpression CollapseOpAssign(Assignment assignment) {
      Contract.Requires(assignment != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      var sourceAssignment = assignment.Source as Assignment;
      if (sourceAssignment != null) {
        assignment.Source = this.CollapseOpAssign(sourceAssignment);
        return assignment;
      }
      var binOp = assignment.Source as BinaryOperation;
      if (binOp != null) {
        var addressDeref = binOp.LeftOperand as IAddressDereference;
        if (addressDeref != null) {
          var dupValue = addressDeref.Address as IDupValue;
          if (dupValue != null) {
            if (binOp is IAddition || binOp is IBitwiseAnd || binOp is IBitwiseOr || binOp is IDivision || binOp is IExclusiveOr ||
            binOp is ILeftShift || binOp is IModulus || binOp is IMultiplication || binOp is IRightShift || binOp is ISubtraction) {
              binOp.LeftOperand = assignment.Target;
              return binOp;
            }
          }
        } else {
          var boundExpr = binOp.LeftOperand as IBoundExpression;
          if (boundExpr != null && boundExpr.Definition == assignment.Target.Definition && boundExpr.Instance is IDupValue) {
            if (binOp is IAddition || binOp is IBitwiseAnd || binOp is IBitwiseOr || binOp is IDivision || binOp is IExclusiveOr ||
            binOp is ILeftShift || binOp is IModulus || binOp is IMultiplication || binOp is IRightShift || binOp is ISubtraction) {
              binOp.LeftOperand = assignment.Target;
              return binOp;
            }
          }
        }
      }
      return assignment;
    }

    private bool ReplacePushDupPopPattern(BlockStatement b) {
      Contract.Requires(b != null);
      bool replacedPattern = false;
      var statements = b.Statements;
      for (int i = 0; i < statements.Count-3; i++) {
        var push1 = statements[i] as PushStatement;
        if (push1 == null) continue;
        var binop1 = push1.ValueToPush as BinaryOperation;
        if (binop1 == null) continue;
        var bound1 = binop1.LeftOperand as BoundExpression;
        if (bound1 == null) continue;
        var exprS1 = statements[i+1] as ExpressionStatement;
        if (exprS1 == null) continue;
        var assign1 = exprS1.Expression as Assignment;
        if (assign1 == null) continue;
        if (assign1.Target.Definition != bound1.Definition) continue;
        if (assign1.Target.Instance != bound1.Instance) {
          if (!(assign1.Target.Instance is IPopValue && bound1.Instance is IDupValue)) continue;
        }
        if (!(assign1.Source is DupValue)) continue;
        var statement = statements[i+2] as Statement;
        if (statement == null) continue;
        var popCounter = new PopCounter();
        popCounter.Traverse(statement);
        if (popCounter.count != 1) continue;
        binop1.LeftOperand = assign1.Target;
        var popReplacer = new SinglePopReplacer(this.host, binop1);
        popReplacer.Rewrite(statement);
        statements.RemoveRange(i, 2);
        replacedPattern = true;
      }
      return replacedPattern;
    }

    private bool ReplacePushDupPushPopPattern(BlockStatement b) {
      Contract.Requires(b != null);
      bool replacedPattern = false;
      var statements = b.Statements;
      for (int i = 0; i < statements.Count-4; i++) {
        var push1 = statements[i] as PushStatement;
        if (push1 == null) continue;
        var exprS1 = statements[i+1] as ExpressionStatement;
        if (exprS1 == null) continue;
        var assign1 = exprS1.Expression as Assignment;
        if (assign1 == null) continue;
        if (!(assign1.Source is DupValue) || assign1.Target.Instance != null) continue;
        var push2 = statements[i+2] as PushStatement;
        if (push2 == null) continue;
        var popCounter = new PopCounter();
        popCounter.Traverse(push2);
        if (popCounter.count != 1) continue;
        if (!this.singleUseExpressionChecker.ExpressionCanBeMovedAndDoesNotReference(push1.ValueToPush, assign1.Target.Definition)) continue;
        assign1.Source = push1.ValueToPush;
        var popReplacer = new SinglePopReplacer(this.host, assign1);
        popReplacer.Rewrite(push2.ValueToPush);
        statements.RemoveRange(i, 2);
        replacedPattern = true;
      }
      return replacedPattern;
    }

    class SinglePopReplacer : CodeRewriter {

      internal SinglePopReplacer(IMetadataHost host, Expression previouslyPushedExpression)
        : base(host) {
        Contract.Requires(previouslyPushedExpression != null);
        this.previouslyPushedExpression = previouslyPushedExpression;
      }

      Expression previouslyPushedExpression;

      [ContractInvariantMethod]
      private void ObjectInvariant() {
        Contract.Invariant(this.previouslyPushedExpression != null);
      }

      public override IExpression Rewrite(IPopValue popValue) {
        return this.previouslyPushedExpression;
      }

    }

    [ContractVerification(false)]
    internal static bool ReplacePushPopPattern(BlockStatement b, IMetadataHost host) {
      Contract.Requires(b != null);
      Contract.Requires(host != null);

      bool replacedPattern = false;
      var statements = b.Statements;
      for (int i = 0; i < statements.Count-1; i++) {
        //First identify count consecutive push statements
        int count = 0;
        while (i+count < statements.Count-1 && statements[i+count] is PushStatement) count++;
        if (count == 0) continue;
        Contract.Assert(i+count < statements.Count);
        Contract.Assert(count < statements.Count);
        for (int j = 0; j < count; j++) {
          Contract.Assume(statements[i+j] is PushStatement);
          if (((PushStatement)statements[i+j]).ValueToPush is DupValue) goto nextIteration;
        }
        Contract.Assert(i >= 0);
        Contract.Assert(i < statements.Count-1);
        //If any of the push statements (other than the first one) contains pop expressions, replace them with the corresponding push values and remove the pushes.
        int pushcount = 1; //the number of push statements that are eligble for removal at this point.
        for (int j = i + 1; j < i + count; j++) {
          Contract.Assume(j < statements.Count); //because i+count < statements.Count for the initial value of count and count just decreases
          Contract.Assume(j >= 0); //because i >= 0 and j never decreases to less than i
          Contract.Assume(statements[j] is PushStatement); //because i < j < i+count and i..i+count-1 are all push statements
          PushStatement st = (PushStatement)statements[j];
          PopCounter pcc = new PopCounter();
          pcc.Traverse(st.ValueToPush);
          int numberOfPushesToRemove = pushcount;
          if (pcc.count > 0) {
            if (pcc.count < numberOfPushesToRemove) numberOfPushesToRemove = pcc.count;
            int firstPushToRemove = j - numberOfPushesToRemove;
            PopReplacer prr = new PopReplacer(host, statements, firstPushToRemove, pcc.count-numberOfPushesToRemove);
            st.ValueToPush = prr.Rewrite(st.ValueToPush);
            statements.RemoveRange(firstPushToRemove, numberOfPushesToRemove);
            if (pcc.count > numberOfPushesToRemove) return true; //We've consumed all of the pushes and we're done.
            count -= numberOfPushesToRemove; //Fewer pushes now remain
            pushcount -= numberOfPushesToRemove; //Likewise fewer eligible pushes remain
            //Start over again at firstPushToRemove, which now indexes the first statement not yet considered.
            j = firstPushToRemove-1;
            continue;
          }
          pushcount++;
        }
        Contract.Assume(count >= 0);
        Contract.Assume(i+count < statements.Count);
        var nextStatement = statements[i + count];
        Contract.Assume(nextStatement != null);
        if (!(nextStatement is IExpressionStatement || nextStatement is IPushStatement || nextStatement is IReturnStatement || nextStatement is IThrowStatement)) continue;
        PopCounter pc = new PopCounter();
        pc.Traverse(nextStatement);
        if (pc.count == 0) continue;
        if (pc.count < count) {
          i += count-pc.count; //adjust i to point to the first push statement to remove because of subsequent pops.
          count = pc.count;
        }
        Contract.Assume(i < statements.Count);
        PopReplacer pr = new PopReplacer(host, statements, i, pc.count-count);
        pr.Rewrite(nextStatement);
        var s = nextStatement as Statement;
        if (s != null)
          s.Locations.AddRange(statements[i].Locations);
        Contract.Assume(count >= 0);
        Contract.Assert(i+count < statements.Count);
        statements.RemoveRange(i, count);
        replacedPattern = true;
      nextIteration: ;
      }
      return replacedPattern;
    }

    private bool ReplaceShortCircuitAnd(BlockStatement b) {
      Contract.Requires(b != null);
      bool replacedPattern = false;
      var statements = b.Statements;
      for (int i = 0; i < statements.Count-6; i++) {
        var ifStatement = statements[i] as ConditionalStatement;
        if (ifStatement == null) continue;
        var gotoFalseCase = ifStatement.FalseBranch as GotoStatement;
        if (gotoFalseCase == null) continue;
        Contract.Assume(ifStatement.TrueBranch is EmptyStatement);
        var labeledStatement = statements[i+1] as LabeledStatement;
        if (labeledStatement == null) continue;
        var gotos = this.gotosThatTarget[(uint)labeledStatement.Label.UniqueKey];
        if (gotos != null && gotos.Count > 0) continue;
        var pushTrueCase = statements[i+2] as PushStatement;
        if (pushTrueCase == null) continue;
        if (pushTrueCase.ValueToPush.Type.TypeCode != PrimitiveTypeCode.Boolean) continue;
        var gotoEnd = statements[i+3] as GotoStatement;
        if (gotoEnd == null) continue;
        var labeledStatement2 = statements[i+4] as LabeledStatement;
        if (labeledStatement2 == null || labeledStatement2 != gotoFalseCase.TargetStatement) continue;
        gotos = this.gotosThatTarget[(uint)labeledStatement2.Label.UniqueKey];
        Contract.Assume(gotos != null && gotos.Count > 0);
        if (gotos.Count > 1) continue;
        var pushFalseCase = statements[i+5] as PushStatement;
        if (pushFalseCase == null) continue;
        var falseCaseVal = pushFalseCase.ValueToPush as CompileTimeConstant;
        if (falseCaseVal == null || !(falseCaseVal.Value is int)) continue;
        var endLabel = statements[i+6] as LabeledStatement;
        if (endLabel == null || gotoEnd.TargetStatement != endLabel) continue;
        Conditional conditional;
        if (((int)falseCaseVal.Value) != 0) {
          var trueConst = new CompileTimeConstant() { Value = true, Type = this.host.PlatformType.SystemBoolean };
          var cond = new LogicalNot() { Operand = ifStatement.Condition, Type = this.host.PlatformType.SystemBoolean };
          conditional = new Conditional() { Condition = cond, ResultIfTrue = trueConst, ResultIfFalse = pushTrueCase.ValueToPush, Type = this.host.PlatformType.SystemBoolean };
        } else {
          var falseConst = new CompileTimeConstant() { Value = false, Type = this.host.PlatformType.SystemBoolean };
          conditional = new Conditional() { Condition = ifStatement.Condition, ResultIfTrue = pushTrueCase.ValueToPush, ResultIfFalse = falseConst, Type = this.host.PlatformType.SystemBoolean };
        }
        pushFalseCase.ValueToPush = conditional;
        statements.RemoveAt(i+6);
        statements.RemoveRange(i, 5);
        replacedPattern = true;
      }
      return replacedPattern;
    }

    private bool ReplaceShortCircuitAnd2(BlockStatement b) {
      Contract.Requires(b != null);
      bool replacedPattern = false;
      var statements = b.Statements;
      for (int i = 0; i < statements.Count-3; i++) {
        var ifStatement = statements[i] as ConditionalStatement;
        if (ifStatement == null) continue;
        var gotoFalseCase = ifStatement.FalseBranch as GotoStatement;
        if (gotoFalseCase == null) continue;
        Contract.Assume(ifStatement.TrueBranch is EmptyStatement);
        var labeledStatement = statements[i+1] as LabeledStatement;
        if (labeledStatement == null) continue;
        var gotos = this.gotosThatTarget[(uint)labeledStatement.Label.UniqueKey];
        if (gotos != null && gotos.Count > 0) continue;
        var ifStatement2 = statements[i+2] as ConditionalStatement;
        if (ifStatement2 == null) continue;
        var gotoTrueCase = ifStatement2.TrueBranch as GotoStatement;
        if (gotoTrueCase == null) continue;
        Contract.Assume(ifStatement2.FalseBranch is EmptyStatement);
        var labeledStatement2 = statements[i+3] as LabeledStatement;
        if (labeledStatement2 == null) continue;
        if (labeledStatement2 == gotoFalseCase.TargetStatement) {
          gotos = this.gotosThatTarget[(uint)labeledStatement2.Label.UniqueKey];
          Contract.Assume(gotos != null && gotos.Count > 0);
          if (gotos.Count > 1) continue;
          var falseConst = new CompileTimeConstant() { Value = false, Type = this.host.PlatformType.SystemBoolean };
          var conditional = new Conditional() { Condition = ifStatement.Condition, ResultIfTrue = ifStatement2.Condition, ResultIfFalse = falseConst, Type = this.host.PlatformType.SystemBoolean };
          ifStatement2.Condition = conditional;
          statements.RemoveRange(i, 2);
          gotos.Remove(gotoFalseCase);
          replacedPattern = true;
        } else {
          if (gotoFalseCase.TargetStatement != gotoTrueCase.TargetStatement) continue;
          //actually have a short circuit or here
          gotos = this.gotosThatTarget[(uint)gotoFalseCase.TargetStatement.Label.UniqueKey];
          Contract.Assume(gotos != null && gotos.Count > 0);
          var trueConst = new CompileTimeConstant() { Value = true, Type = this.host.PlatformType.SystemBoolean };
          var invertedCond = new LogicalNot() { Operand = ifStatement.Condition, Type = this.host.PlatformType.SystemBoolean };
          var conditional = new Conditional() { Condition = invertedCond, ResultIfTrue = trueConst, ResultIfFalse = ifStatement2.Condition, Type = this.host.PlatformType.SystemBoolean };
          ifStatement2.Condition = conditional;
          statements.RemoveRange(i, 2);
          gotos.Remove(gotoFalseCase);
          replacedPattern = true;
        }
      }
      return replacedPattern;
    }

    private bool ReplaceShortCircuitAnd3(BlockStatement b) {
      Contract.Requires(b != null);
      bool replacedPattern = false;
      var statements = b.Statements;
      for (int i = 0; i < statements.Count-2; i++) {
        var ifStatement1 = statements[i] as ConditionalStatement;
        if (ifStatement1 == null || !(ifStatement1.FalseBranch is EmptyStatement)) continue;
        var goto1 = ifStatement1.TrueBranch as GotoStatement;
        if (goto1 == null) continue;
        var labStat = statements[i+1] as LabeledStatement;
        if (labStat == null) continue;
        var gotos1 =  this.gotosThatTarget[(uint)labStat.Label.UniqueKey];
        if (gotos1 != null && gotos1.Count > 0) continue;
        var ifStatement2 = statements[i+2] as ConditionalStatement;
        if (ifStatement2 == null || !(ifStatement2.FalseBranch is EmptyStatement)) continue;
        var labeledStatement = i < statements.Count-3 ? statements[i+3] as LabeledStatement : this.labelImmediatelyFollowingCurrentBlock;
        if (goto1.TargetStatement != labeledStatement) continue;
        var gotos = this.gotosThatTarget[(uint)labeledStatement.Label.UniqueKey];
        if (gotos == null || gotos.Count == 0) continue;
        var falseConst = new CompileTimeConstant() { Value = false, Type = this.host.PlatformType.SystemBoolean };
        var invertedCond = new LogicalNot() { Operand = ifStatement1.Condition, Type = this.host.PlatformType.SystemBoolean };
        var conditional = new Conditional() { Condition = invertedCond, ResultIfTrue = ifStatement2.Condition, ResultIfFalse = falseConst, Type = this.host.PlatformType.SystemBoolean };
        ifStatement2.Condition = conditional;
        statements.RemoveRange(i, 2);
        gotos.Remove(goto1);
        replacedPattern = true;
      }
      return replacedPattern;
    }

    private bool ReplaceShortCircuitAnd4(BlockStatement b) {
      Contract.Requires(b != null);
      bool replacedPattern = false;
      var statements = b.Statements;
      for (int i = 0; i < statements.Count-2; i++) {
        var ifStatement1 = statements[i] as ConditionalStatement;
        if (ifStatement1 == null || !(ifStatement1.FalseBranch is EmptyStatement)) continue;
        var goto1 = ifStatement1.TrueBranch as GotoStatement;
        if (goto1 == null) continue;
        var labStat = statements[i+1] as LabeledStatement;
        if (labStat == null) continue;
        var gotos1 =  this.gotosThatTarget[(uint)labStat.Label.UniqueKey];
        if (gotos1 != null && gotos1.Count > 0) continue;
        var ifStatement2 = statements[i+2] as ConditionalStatement;
        if (ifStatement2 == null) continue;
        if (ifStatement2.TrueBranch is EmptyStatement) {
          var goto2 = ifStatement2.FalseBranch as GotoStatement;
          if (goto2 == null) continue;
          if (goto1.TargetStatement != goto2.TargetStatement) continue;
          var gotos = this.gotosThatTarget[(uint)goto1.TargetStatement.Label.UniqueKey];
          if (gotos == null || gotos.Count == 0) continue;
          var falseConst = new CompileTimeConstant() { Value = false, Type = this.host.PlatformType.SystemBoolean };
          var notCond1 = new LogicalNot() { Operand = ifStatement1.Condition, Type = this.host.PlatformType.SystemBoolean };
          var conditional = new Conditional() { Condition = notCond1, ResultIfTrue = ifStatement2.Condition, ResultIfFalse = falseConst, Type = this.host.PlatformType.SystemBoolean };
          ifStatement2.Condition = conditional;
          statements.RemoveRange(i, 2);
          gotos.Remove(goto1);
          replacedPattern = true;
        } else if (ifStatement2.FalseBranch is EmptyStatement) {
          var goto2 = ifStatement2.TrueBranch as GotoStatement;
          if (goto2 == null) continue;
          if (goto1.TargetStatement != goto2.TargetStatement) continue;
          var gotos = this.gotosThatTarget[(uint)goto1.TargetStatement.Label.UniqueKey];
          if (gotos == null || gotos.Count == 0) continue;
          var trueConst = new CompileTimeConstant() { Value = true, Type = this.host.PlatformType.SystemBoolean };
          var cond1 = ifStatement1.Condition;
          var conditional = new Conditional() { Condition = cond1, ResultIfTrue = trueConst, ResultIfFalse = ifStatement2.Condition, Type = this.host.PlatformType.SystemBoolean };
          ifStatement2.Condition = conditional;
          statements.RemoveRange(i, 2);
          gotos.Remove(goto1);
          replacedPattern = true;
        }
      }
      return replacedPattern;
    }

    private bool ReplaceShortCircuitAnd5(BlockStatement b) {
      Contract.Requires(b != null);
      bool replacedPattern = false;
      var statements = b.Statements;
      for (int i = 0; i < statements.Count; i++) {
        var ifStatement = statements[i] as ConditionalStatement;
        if (ifStatement == null) continue;
        var pushTrue = ifStatement.TrueBranch as IPushStatement;
        if (pushTrue == null) {
          var trueBlock = ifStatement.TrueBranch as BlockStatement;
          if (trueBlock != null && trueBlock.Statements.Count == 1)
            pushTrue = trueBlock.Statements[0] as IPushStatement;
        }
        if (pushTrue == null || pushTrue.ValueToPush.Type.TypeCode != PrimitiveTypeCode.Boolean) continue;
        var pushFalse = ifStatement.FalseBranch as IPushStatement;
        if (pushFalse == null) {
          var falseBlock = ifStatement.FalseBranch as BlockStatement;
          if (falseBlock != null && falseBlock.Statements.Count == 1)
            pushFalse = falseBlock.Statements[0] as IPushStatement;
        }
        if (pushFalse == null) continue;
        var falseCaseVal = pushFalse.ValueToPush as CompileTimeConstant;
        if (falseCaseVal == null || !(falseCaseVal.Value is int)) continue;
        if (0 != (int)falseCaseVal.Value) continue;
        var falseConst = new CompileTimeConstant() { Value = false, Type = this.host.PlatformType.SystemBoolean };
        statements[i] = new PushStatement() {
          ValueToPush = new Conditional() {
            Condition = ifStatement.Condition, ResultIfTrue = pushTrue.ValueToPush,
            ResultIfFalse = falseConst, Type = pushTrue.ValueToPush.Type
          }
        };
        replacedPattern = true;
      }
      return replacedPattern;
    }

    [ContractVerification(false)]
    private bool ReplaceShortCircuitAnd6(BlockStatement b) {
      Contract.Requires(b != null);
      bool replacedPattern = false;
      var statements = b.Statements;
      for (int i = 0; i < statements.Count-8; i++) {
        var ifStatement = statements[i] as ConditionalStatement;
        if (ifStatement == null) continue;
        var gotoFalseCase = ifStatement.FalseBranch as GotoStatement;
        if (gotoFalseCase == null) continue;
        Contract.Assume(ifStatement.TrueBranch is EmptyStatement);
        var labeledStatement = statements[i+1] as LabeledStatement;
        if (labeledStatement == null) continue;
        var gotos = this.gotosThatTarget[(uint)labeledStatement.Label.UniqueKey];
        if (gotos != null && gotos.Count > 0) continue;
        var ifStatement2 = statements[i+2] as ConditionalStatement;
        if (ifStatement2 == null) continue;
        var gotoTrueCase = ifStatement2.FalseBranch as GotoStatement;
        if (gotoTrueCase == null) continue;
        if (!(ifStatement2.TrueBranch is EmptyStatement)) continue;
        var labeledStatement2 = statements[i+3] as LabeledStatement;
        if (labeledStatement2 == null || labeledStatement2 != gotoFalseCase.TargetStatement) continue;
        gotos = this.gotosThatTarget[(uint)labeledStatement2.Label.UniqueKey];
        Contract.Assume(gotos != null && gotos.Count > 0);
        if (gotos.Count > 1) continue;
        Contract.Assume(gotos[0] == gotoFalseCase);
        var pushFalseCase = statements[i+4] as PushStatement;
        if (pushFalseCase == null) continue;
        if (pushFalseCase.ValueToPush.Type.TypeCode != PrimitiveTypeCode.Boolean) continue;
        var gotoEnd = statements[i+5] as GotoStatement;
        if (gotoEnd == null) continue;
        var labeledStatement3 = statements[i+6] as LabeledStatement;
        if (labeledStatement3 == null || labeledStatement3 != gotoTrueCase.TargetStatement) continue;
        gotos = this.gotosThatTarget[(uint)labeledStatement3.Label.UniqueKey];
        Contract.Assume(gotos != null && gotos.Count > 0);
        if (gotos.Count > 1) continue;
        Contract.Assume(gotos[0] == gotoTrueCase);
        var pushTrueCase = statements[i+7] as PushStatement;
        if (pushTrueCase == null) continue;
        if (pushTrueCase.ValueToPush.Type.TypeCode != PrimitiveTypeCode.Int32) continue;
        var pushTrueVal = pushTrueCase.ValueToPush as CompileTimeConstant;
        if (pushTrueVal == null || !(pushTrueVal.Value is int)) continue;
        if (1 != (int)pushTrueVal.Value) continue;
        var endLabel = statements[i+8] as LabeledStatement;
        if (endLabel == null || gotoEnd.TargetStatement != endLabel) continue;
        gotos = this.gotosThatTarget[(uint)endLabel.Label.UniqueKey];
        Contract.Assume(gotos != null && gotos.Count > 0);
        if (gotos.Count > 1) continue;
        Contract.Assume(gotos[0] == gotoEnd);

        var falseConst = new CompileTimeConstant() { Value = false, Type = this.host.PlatformType.SystemBoolean };
        var invertedCond1 = IfThenElseReplacer.InvertCondition(ifStatement2.Condition);
        var conditional1 = new Conditional() { Condition = ifStatement.Condition, ResultIfTrue = invertedCond1, ResultIfFalse = falseConst, Type = this.host.PlatformType.SystemBoolean };
        var trueConst = new CompileTimeConstant() { Value = true, Type = this.host.PlatformType.SystemBoolean };
        var invertedCond2 = IfThenElseReplacer.InvertCondition(pushFalseCase.ValueToPush);
        var conditional2 = new Conditional() { Condition = conditional1, ResultIfTrue = trueConst, ResultIfFalse = invertedCond2, Type = this.host.PlatformType.SystemBoolean };
        pushTrueCase.ValueToPush = conditional2;
        statements.RemoveAt(i+8);
        statements.RemoveRange(i, 7);
        replacedPattern = true;
      }
      return replacedPattern;
    }

    private bool ReplaceReturnViaGoto(BlockStatement b) {
      Contract.Requires(b != null);
      if (this.returnValueTemp == null) return false;
      bool replacedPattern = false;
      var statements = b.Statements;
      for (int i = 1; i < b.Statements.Count-1; i++) {
        var expressionStatement = statements[i] as ExpressionStatement;
        if (expressionStatement == null) continue;
        var assign = expressionStatement.Expression as Assignment;
        if (assign == null) continue;
        if (assign.Target.Definition != this.returnValueTemp) continue;
        var gotoStatement = statements[i+1] as GotoStatement;
        if (gotoStatement == null) continue;
        if (gotoStatement.TargetStatement.Label != this.labelOfFinalReturn) continue;
        var gotos = this.gotosThatTarget[(uint)gotoStatement.TargetStatement.Label.UniqueKey];
        if (gotos != null) gotos.Remove(gotoStatement);
        statements[i] = new ReturnStatement() { Expression = assign.Source, Locations = expressionStatement.Locations };
        this.numberOfAssignmentsToLocal[this.returnValueTemp]--;
        statements.RemoveAt(i+1);
        replacedPattern = true;
      }
      return replacedPattern;
    }

    private bool ReplaceReturnViaGotoInVoidMethods(BlockStatement b) {
      Contract.Requires(b != null);
      if (!this.isVoidMethod) return false;
      if (this.labelOfFinalReturn == null) return false;
      bool replacedPattern = false;
      var statements = b.Statements;
      for (int i = 1; i < b.Statements.Count - 1; i++) {
        var gotoStatement = statements[i] as GotoStatement;
        if (gotoStatement == null) continue;
        if (gotoStatement.TargetStatement.Label != this.labelOfFinalReturn) continue;
        var gotos = this.gotosThatTarget[(uint)gotoStatement.TargetStatement.Label.UniqueKey];
        if (gotos != null) gotos.Remove(gotoStatement);
        statements[i] = new ReturnStatement() { Locations = gotoStatement.Locations };
        replacedPattern = true;
      }
      return replacedPattern;
    }

    private bool ReplaceSelfAssignment(BlockStatement b) {
      Contract.Requires(b != null);
      if (this.returnValueTemp == null) return false;
      bool replacedPattern = false;
      var statements = b.Statements;
      for (int i = 1; i < b.Statements.Count; i++) {
        var expressionStatement = statements[i] as ExpressionStatement;
        if (expressionStatement == null) continue;
        var assign = expressionStatement.Expression as Assignment;
        if (assign == null) continue;
        var local = assign.Target.Definition as LocalDefinition;
        if (local == null) continue;
        var boundExpr = assign.Source as BoundExpression;
        if (boundExpr == null || boundExpr.Definition != local) continue;
        this.numberOfAssignmentsToLocal[local]--;
        this.numberOfReferencesToLocal[local]--;
        statements.RemoveAt(i);
        replacedPattern = true;
      }
      return replacedPattern;
    }

    private bool ReplaceSingleUseCompilerGeneratedLocalPattern(BlockStatement b) {
      Contract.Requires(b != null);
      bool replacedPattern = false;
      var statements = b.Statements;
      for (int i = 0; i < statements.Count-1; i++) {
        uint referencesToRemove = 1;
        var expressionStatement = statements[i] as ExpressionStatement;
        if (expressionStatement == null) continue;
        var assignment = expressionStatement.Expression as Assignment;
        if (assignment == null) continue;
        var local = assignment.Target.Definition as ILocalDefinition;
        if (local == null) {
          var adr = assignment.Target.Definition as IAddressDereference;
          if (adr != null) {
            var addrOf = adr.Address as IAddressOf;
            if (addrOf != null) {
              local = addrOf.Expression.Definition as ILocalDefinition;
              referencesToRemove = 2;
            }
          }
        }
        if (local == null || local is CapturedLocalDefinition) continue;
        if (this.sourceLocationProvider != null) {
          bool isCompilerGenerated;
          var sourceName = this.sourceLocationProvider.GetSourceNameFor(local, out isCompilerGenerated);
          if (!isCompilerGenerated) continue;
        }
        if (!this.singleUseExpressionChecker.ExpressionCanBeMovedAndDoesNotReference(assignment.Source, local)) continue;
        var j = 1;
        while (i+j < statements.Count-2 && statements[i+j] is IEmptyStatement) j++;
        Contract.Assume(i+j < statements.Count); //i < statements.Count-1 and (j == 1 or the loop above established i+j < statements.Count-1)
        Contract.Assume(statements[i+j] != null);
        if (this.singleAssignmentReferenceFinder.LocalCanBeReplacedIn(statements[i+j], local)) {
          if (this.singleAssignmentLocalReplacer.Replace(assignment.Source, local, statements[i+j])) {
            ((Expression)assignment.Source).Locations = expressionStatement.Locations;
            this.numberOfAssignmentsToLocal[local]--;
            this.numberOfReferencesToLocal[local] -= referencesToRemove;
            var s = statements[i + 1] as Statement;
            if (s != null)
              s.Locations.AddRange(statements[i].Locations);
            statements.RemoveRange(i, j);
            replacedPattern = true;
          }
        }
      }
      return replacedPattern;
    }

    /// <summary>
    /// The source expression "new C(){ f1 = e1, f2 = e2, ... }" (where the f's can be fields
    /// or properties) turns into "cgl = new C(); cgl.f1 = e1; cg1.f2 = e2; ...".
    /// ("cgl" means "compiler-generated local".)
    /// Turn it into a block expression whose Statements are the statements above (but where
    /// the first one becomes a local declaration statement), and with an Expression that is
    /// just the local, cgl', where cgl' is a freshly created local.
    /// </summary>
    private bool ReplaceCompilerGeneratedLocalUsedForInitializersPattern(BlockStatement b) {
      Contract.Requires(b != null);
      bool replacedPattern = false;
      var statements = b.Statements;
      for (int i = 0; i < statements.Count - 1; i++) {
        var expressionStatement = statements[i] as ExpressionStatement;
        if (expressionStatement == null) continue;
        var assignment = expressionStatement.Expression as Assignment;
        if (assignment == null) continue;
        var local = assignment.Target.Definition as ILocalDefinition;
        if (local == null || local is CapturedLocalDefinition) continue;
        if (this.numberOfAssignmentsToLocal[local] != 1) continue;
        if (this.sourceLocationProvider != null) {
          bool isCompilerGenerated;
          var sourceName = this.sourceLocationProvider.GetSourceNameFor(local, out isCompilerGenerated);
          if (!isCompilerGenerated) continue;
        }
        var createObject = assignment.Source as ICreateObjectInstance;
        if (createObject == null) continue;
        if (!this.singleUseExpressionChecker.ExpressionCanBeMovedAndDoesNotReference(assignment.Source, local)) continue;
        var j = 1;
        while (i + j < statements.Count - 1 && IsAssignmentToFieldOrProperty(local, statements[i + j])) j++;
        if (j == 1) continue;
        if (this.numberOfReferencesToLocal[local] != (uint)j) continue;
        Contract.Assume(i + j < statements.Count); //i < statements.Count-1 and (j == 1 or the loop above established i+j < statements.Count-1)
        Contract.Assume(statements[i + j] != null);
        if (LocalFinder.LocalOccursIn(statements[i+j], local) && this.singleAssignmentReferenceFinder.LocalCanBeReplacedIn(statements[i + j], local)) {
          var newLocal = new LocalDefinition() {
            Name = this.host.NameTable.GetNameFor(local.Name.Value + "_prime"),
            MethodDefinition = local.MethodDefinition,
            Type = local.Type,
          };
          var lds = new LocalDeclarationStatement() {
            InitialValue = assignment.Source,
            LocalVariable = newLocal,
          };
          var stmts = new List<IStatement>(j) { lds, };
          var boundExpression = new BoundExpression() { Definition = newLocal, Instance = null, Type = newLocal.Type, };
          foreach (var s in statements.GetRange(i + 1, j - 1)) {
            Contract.Assume(s != null);
            this.singleAssignmentLocalReplacer.Replace(boundExpression, local, s);
            stmts.Add(s);
          }
          var blockExpression = new BlockExpression() {
            BlockStatement = new BlockStatement() {
              Statements = stmts,
            },
            Expression = new BoundExpression() { Definition = newLocal, Instance = null, Type = newLocal.Type, },
            Type = newLocal.Type,
          };
          if (this.singleAssignmentLocalReplacer.Replace(blockExpression, local, statements[i + j])) {
            this.numberOfAssignmentsToLocal[newLocal] = 1;
            this.numberOfReferencesToLocal[newLocal] = (uint)j;
            this.numberOfAssignmentsToLocal[local]--;
            this.numberOfReferencesToLocal[local] = 0;
            statements.RemoveRange(i, j);
            replacedPattern = true;
          } else
            Contract.Assume(false); // replacement should succeed since the combination of LocalOccursIn and LocalCanBeReplacedIn returned true
        }
      }
      return replacedPattern;
    }

    private bool IsAssignmentToFieldOrProperty(ILocalDefinition local, IStatement statement) {
      var expressionStatement = statement as ExpressionStatement;
      if (expressionStatement == null) return false;
      IBoundExpression boundExpression = null;
      var assignment = expressionStatement.Expression as Assignment;
      if (assignment != null)
        boundExpression = assignment.Target.Instance as IBoundExpression;
      else {
        var methodCall = expressionStatement.Expression as IMethodCall;
        if (methodCall == null) return false;
        if (!IsSetter(methodCall.MethodToCall)) return false;
        if (methodCall.IsStaticCall) return false;
        if (methodCall.IsJumpCall) return false;
        boundExpression = methodCall.ThisArgument as IBoundExpression;
      }
      if (boundExpression == null) return false;
      if (boundExpression.Instance != null) return false;
      return boundExpression.Definition == local;

    }

    private static bool IsSetter(IMethodReference methodReference) {
      Contract.Requires(methodReference != null);
      // can't check IsSpecialName unless we resolve, which we don't want to do
      return methodReference.Name.Value.StartsWith("set_")
        && methodReference.Type.TypeCode == PrimitiveTypeCode.Void;
    }

    /// <summary>
    /// For a string field, s, the source expression e.s += ""
    /// turns into a specific pattern.
    /// That pattern here looks like:
    /// i:   push e
    /// i+1: push dup.s
    /// i+2: (!= dup (default_value string)) ? goto L2 : empty
    /// i+3: L1
    /// i+4: pop
    /// i+5: push ""
    /// i+6: L2
    /// i+7: pop.s = pop
    /// </summary>
    private bool ReplacePlusAssignForStringPattern(BlockStatement b) {
      Contract.Requires(b != null);
      bool replacedPattern = false;
      var statements = b.Statements;
      for (int i = 0; i < statements.Count - 7; i++) {
        var push1 = statements[i] as PushStatement;
        if (push1 == null) continue;
        var push2 = statements[i + 1] as PushStatement;
        if (push2 == null) continue;
        var boundExpression = push2.ValueToPush as IBoundExpression;
        if (boundExpression == null) continue;
        var dupValue = boundExpression.Instance as IDupValue;
        if (dupValue == null) continue;
        var field = boundExpression.Definition as IFieldReference;
        if (field == null) continue;
        var conditionalStatement = statements[i + 2] as IConditionalStatement;
        if (conditionalStatement == null) continue;
        var notEquality = conditionalStatement.Condition as INotEquality;
        if (notEquality == null) continue;
        var gotoStatement = conditionalStatement.TrueBranch as IGotoStatement;
        if (gotoStatement == null) continue;
        var branchTarget = gotoStatement.TargetStatement;
        var emptyStatement = conditionalStatement.FalseBranch as IEmptyStatement;
        if (emptyStatement == null) continue;
        var labeledStatement = statements[i + 3] as ILabeledStatement;
        if (labeledStatement == null) continue;
        var popStatement = statements[i + 4] as IExpressionStatement;
        if (popStatement == null) continue;
        if (!(popStatement.Expression is IPopValue)) continue;
        var pushEmptyString = statements[i + 5] as IPushStatement;
        if (pushEmptyString == null) continue;
        var emptyString = pushEmptyString.ValueToPush as ICompileTimeConstant;
        if (emptyString == null) continue;
        if (emptyString.Type.TypeCode != PrimitiveTypeCode.String) continue;
        if ((string)emptyString.Value != "") continue;
        labeledStatement = statements[i + 6] as ILabeledStatement;
        if (labeledStatement == null) continue;
        if (labeledStatement.Label != branchTarget.Label) continue;
        var assignStatement = statements[i + 7] as IExpressionStatement;
        if (assignStatement == null) continue;
        var assignment = assignStatement.Expression as IAssignment;
        if (assignment == null) continue;
        if (!(assignment.Source is IPopValue)) continue;
        if (!(assignment.Target.Instance is IPopValue)) continue;
        // REVIEW: should the definition of the target be checked to be the same as "field"? If so, how?

        var plusEqual = new Addition() {
          LeftOperand = new TargetExpression() {
            Definition = assignment.Target.Definition,
            Instance = push1.ValueToPush,
          },
          RightOperand = emptyString,
          ResultIsUnmodifiedLeftOperand = false,
          Type = assignment.Type,
        };

        statements[i] = new ExpressionStatement() {
          Expression = plusEqual,
          Locations = new List<ILocation>(push1.Locations),
        };
        statements.RemoveRange(i + 1, 7);
        replacedPattern = true;
      }
      return replacedPattern;
    }
  }

  internal class LocalFinder : CodeTraverser {
    bool found = false;
    private ILocalDefinition local;
    private LocalFinder(ILocalDefinition local) {
      this.local = local;
    }
    public static bool LocalOccursIn(IStatement s, ILocalDefinition local) {
      Contract.Requires(s != null);
      Contract.Requires(local != null);
      var lf = new LocalFinder(local);
      lf.Traverse(s);
      return lf.found;
    }
    public override void TraverseChildren(ILocalDefinition localDefinition) {
      if (localDefinition == this.local) this.found = true;
    }
  }

  internal class SingleAssignmentSingleReferenceFinder : CodeTraverser {

    internal SingleAssignmentSingleReferenceFinder(SetOfObjects bindingsThatMakeALastUseOfALocalVersion, HashtableForUintValues<object> numberOfReferencesToLocal) {
      Contract.Requires(bindingsThatMakeALastUseOfALocalVersion != null);
      Contract.Requires(numberOfReferencesToLocal != null);

      this.bindingsThatMakeALastUseOfALocalVersion = bindingsThatMakeALastUseOfALocalVersion;
      this.numberOfReferencesToLocal = numberOfReferencesToLocal;
      this.local = Dummy.LocalVariable;
    }

    SetOfObjects bindingsThatMakeALastUseOfALocalVersion;
    HashtableForUintValues<object> numberOfReferencesToLocal;
    ILocalDefinition local;
    bool localCanBeReplaced;

    [ContractInvariantMethod]
    private void ObjectInvariant() {
      Contract.Invariant(this.bindingsThatMakeALastUseOfALocalVersion != null);
      Contract.Invariant(this.numberOfReferencesToLocal != null);
      Contract.Invariant(this.local != null);
    }

    internal bool LocalCanBeReplacedIn(IStatement statement, ILocalDefinition local) {
      Contract.Requires(statement != null);
      Contract.Requires(local != null);
      this.local = local;
      this.StopTraversal = false;
      this.localCanBeReplaced = true;
      this.Traverse(statement);
      return this.localCanBeReplaced;
    }

    public override void TraverseChildren(IAddressableExpression addressableExpression) {
      if (addressableExpression.Definition == this.local) {
        //We are willing to replace the local with an expression because the CodeModel to IL converter will re-introduce the local.
        if (this.numberOfReferencesToLocal[local] == 1)
          this.localCanBeReplaced = true;
        else
          this.localCanBeReplaced = this.bindingsThatMakeALastUseOfALocalVersion.Contains(addressableExpression);
        this.StopTraversal = true;
        return;
      }
      base.TraverseChildren(addressableExpression);
    }

    public override void TraverseChildren(IBoundExpression boundExpression) {
      if (boundExpression.Definition == this.local) {
        if (this.numberOfReferencesToLocal[local] == 1)
          this.localCanBeReplaced = true;
        else
          this.localCanBeReplaced = this.bindingsThatMakeALastUseOfALocalVersion.Contains(boundExpression);
        this.StopTraversal = true;
        return;
      }
      base.TraverseChildren(boundExpression);
    }

    public override void TraverseChildren(IMethodCall methodCall) {
      base.TraverseChildren(methodCall);
      if (!this.StopTraversal) {
        //If the local has not been replaced by the time the method is called, we pessimistically assume that
        //the method being called can mutate state that will be used to compute the value of the local.
        //We therefore keep the local and force it to be initialized before the call takes place.
        this.localCanBeReplaced = false;
        this.StopTraversal = true;
      }
    }

    public override void TraverseChildren(ITargetExpression targetExpression) {
      if (targetExpression.Definition == this.local) {
        this.localCanBeReplaced = false;
        this.StopTraversal = true;
        return;
      }
      base.TraverseChildren(targetExpression);
    }

  }

  internal class SingleAssignmentLocalReplacer : CodeRewriter {

    internal SingleAssignmentLocalReplacer(IMetadataHost host, SetOfObjects bindingsThatMakeALastUseOfALocalVersion, HashtableForUintValues<object> numberOfAssignmentsToLocal)
      : base(host) {
      Contract.Requires(host != null);
      Contract.Requires(bindingsThatMakeALastUseOfALocalVersion != null);
      Contract.Requires(numberOfAssignmentsToLocal != null);

      this.bindingsThatMakeALastUseOfALocalVersion = bindingsThatMakeALastUseOfALocalVersion;
      this.numberOfAssignmentsToLocal = numberOfAssignmentsToLocal;
      this.local = Dummy.LocalVariable;
      this.expressionToSubstituteForLocal = CodeDummy.Expression;
    }

    SetOfObjects bindingsThatMakeALastUseOfALocalVersion;
    HashtableForUintValues<object> numberOfAssignmentsToLocal;
    ILocalDefinition local;
    IExpression expressionToSubstituteForLocal;
    bool replacementHappened;
    internal bool replacedLastUseOfLocalVersion;

    [ContractInvariantMethod]
    private void ObjectInvariant() {
      Contract.Invariant(this.bindingsThatMakeALastUseOfALocalVersion != null);
      Contract.Invariant(this.numberOfAssignmentsToLocal != null);
      Contract.Invariant(this.local != null);
      Contract.Invariant(this.expressionToSubstituteForLocal != null);
    }


    internal bool Replace(IExpression expressionToSubstituteForLocal, ILocalDefinition local, IStatement statement) {
      Contract.Requires(expressionToSubstituteForLocal != null);
      Contract.Requires(local != null);
      Contract.Requires(statement != null);

      this.expressionToSubstituteForLocal = TypeInferencer.Convert(expressionToSubstituteForLocal, local.Type);
      this.local = local;
      this.replacementHappened = false;
      this.replacedLastUseOfLocalVersion = false;
      this.Rewrite(statement);
      return this.replacementHappened;
    }

    public override IExpression Rewrite(IBoundExpression boundExpression) {
      if (boundExpression.Definition == this.local) {
        this.replacementHappened = true;
        this.replacedLastUseOfLocalVersion = this.bindingsThatMakeALastUseOfALocalVersion.Contains(boundExpression);
        return this.expressionToSubstituteForLocal;
      }
      return base.Rewrite(boundExpression);
    }

    public override void RewriteChildren(AddressableExpression addressableExpression) {
      if (addressableExpression.Definition == this.local) {
        addressableExpression.Definition = this.expressionToSubstituteForLocal;
        this.numberOfAssignmentsToLocal[this.local]--;
        this.replacementHappened = true;
        return;
      }
      base.RewriteChildren(addressableExpression);
    }

  }

  internal class SingleUseExpressionChecker : CodeTraverser {

    bool foundProblem;
    object definition = Dummy.LocalVariable;

    internal bool ExpressionCanBeMovedAndDoesNotReference(IExpression expression, object definition) {
      Contract.Requires(expression != null);
      Contract.Requires(definition != null);
      this.foundProblem = false;
      this.definition = definition;
      this.StopTraversal = false;
      Contract.Assume(this.objectsThatHaveAlreadyBeenTraversed != null);
      this.objectsThatHaveAlreadyBeenTraversed.Clear();
      this.Traverse(expression);
      return !this.foundProblem;
    }

    //public override void TraverseChildren(IAddressableExpression addressableExpression) {
    //  this.foundProblem = true;
    //  this.StopTraversal = true;
    //}

    public override void TraverseChildren(IDupValue dupValue) {
      this.foundProblem = true;
      this.StopTraversal = true;
    }

    public override void TraverseChildren(ILocalDefinition localDefinition) {
      if (localDefinition == this.definition) {
        this.foundProblem = true;
        this.StopTraversal = true;
      }
    }

    //public override void TraverseChildren(IMethodCall methodCall) {
    //  this.foundProblem = true;
    //  this.StopTraversal = true;
    //}

    public override void TraverseChildren(IParameterDefinition parameterDefinition) {
      if (parameterDefinition == this.definition) {
        this.foundProblem = true;
        this.StopTraversal = true;
      }
    }

    public override void TraverseChildren(IPopValue popValue) {
      this.foundProblem = true;
      this.StopTraversal = true;
    }

    //public override void TraverseChildren(ITargetExpression targetExpression) {
    //  this.foundProblem = true;
    //  this.StopTraversal = true;
    //}
  }

  internal class PopCounter : CodeTraverser {
    internal int count;

    public override void TraverseChildren(IExpression expression) {
      if (expression is IDupValue) { this.StopTraversal = true; return; }
      if (expression is PopValue) this.count++;
      base.TraverseChildren(expression);
    }

    /// <summary>
    /// Do not count pops in lambdas: they must not be confused with pops that
    /// are not within them.
    /// </summary>
    public override void TraverseChildren(IAnonymousDelegate anonymousDelegate) {
    }

  }

  internal class PopReplacer : CodeRewriter {
    List<IStatement> statements;
    int i;
    int numberOfPopsToIgnore;

    internal PopReplacer(IMetadataHost host, List<IStatement> statements, int i, int numberOfPopsToIgnore)
      : base(host) {
      Contract.Requires(host != null);
      Contract.Requires(statements != null);
      Contract.Requires(i >= 0);
      Contract.Requires(i < statements.Count);
      Contract.Requires(numberOfPopsToIgnore >= 0);

      this.statements = statements;
      this.i = i;
      this.numberOfPopsToIgnore = numberOfPopsToIgnore;
    }

    [ContractInvariantMethod]
    private void ObjectInvariant() {
      Contract.Invariant(this.statements != null);
      Contract.Invariant(this.i >= 0);
      Contract.Invariant(this.i < this.statements.Count);
    }


    public override IExpression Rewrite(IExpression expression) {
      var pop = expression as PopValue;
      if (pop != null) {
        if (this.numberOfPopsToIgnore-- > 0) return expression;
        Contract.Assume(this.statements[this.i] is PushStatement);
        Contract.Assume(this.i+1 < statements.Count);
        PushStatement push = (PushStatement)this.statements[this.i++];
        return TypeInferencer.Convert(push.ValueToPush, pop.Type);
      }
      if (expression is IDupValue) {
        this.numberOfPopsToIgnore = int.MaxValue;
        return expression;
      }
      return base.Rewrite(expression);
    }

    /// <summary>
    /// Do not replace pops in lambdas: they must not be confused with pops that
    /// are not within them.
    /// </summary>
    public override IExpression Rewrite(IAnonymousDelegate anonymousDelegate) {
      return anonymousDelegate;
    }

  }

  internal class UnreferencedLabelRemover : CodeTraverser {

    internal UnreferencedLabelRemover(SourceMethodBody sourceMethodBody) {
      Contract.Requires(sourceMethodBody != null);
      this.gotosThatTarget = sourceMethodBody.gotosThatTarget; Contract.Assume(this.gotosThatTarget != null);
      this.numberOfAssignmentsToLocal = sourceMethodBody.numberOfAssignmentsToLocal; Contract.Assume(this.numberOfAssignmentsToLocal != null);
      this.numberOfReferencesToLocal = sourceMethodBody.numberOfReferencesToLocal; Contract.Assume(this.numberOfReferencesToLocal != null);
    }

    Hashtable<List<IGotoStatement>> gotosThatTarget;
    HashtableForUintValues<object> numberOfAssignmentsToLocal;
    HashtableForUintValues<object> numberOfReferencesToLocal;

    [ContractInvariantMethod]
    private void ObjectInvariant() {
      Contract.Invariant(this.gotosThatTarget != null);
      Contract.Invariant(this.numberOfAssignmentsToLocal != null);
      Contract.Invariant(this.numberOfReferencesToLocal != null);
    }


    public override void TraverseChildren(IBlockStatement block) {
      base.TraverseChildren(block);
      Contract.Assume(block is BlockStatement);
      var dblock = (BlockStatement)block;
      for (int i = 0; i < dblock.Statements.Count; i++) {
        var statement = dblock.Statements[i];
        var label = statement as ILabeledStatement;
        if (label != null) {
          var gotos = this.gotosThatTarget[(uint)label.Label.UniqueKey];
          if (gotos == null || gotos.Count == 0) {
            dblock.Statements.RemoveAt(i);
            i--;
          }
        } else {
          var localDeclaration = statement as ILocalDeclarationStatement;
          if (localDeclaration == null) continue;
          if (this.numberOfAssignmentsToLocal[localDeclaration.LocalVariable] > 0) continue;
          if (this.numberOfReferencesToLocal[localDeclaration.LocalVariable] > 0) continue;
          dblock.Statements.RemoveAt(i);
          i--;
        }
      }
    }
  }

}
