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
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Cci;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Linq;
using System.Diagnostics.Contracts;

namespace CSharpSourceEmitter {
  public partial class SourceEmitter : CodeTraverser, ICSharpSourceEmitter {


    private uint currentPrecedence;

    private bool LowerPrecedenceThanParentExpression(IExpression expression) {
      var newPriority = Precedence(expression);
      return newPriority < this.currentPrecedence;
    }

    /// <summary>
    /// Higher precedence means more tightly binding.
    /// </summary>
    private uint Precedence(IExpression/*?*/ expression) {
      if (expression == null) return 0;

      var prefix = false;
      var postfix = false;
      var binaryExpression = expression as IBinaryOperation;
      if (binaryExpression != null) {
        prefix = IsPrefix(binaryExpression);
        postfix = IsPostfix(binaryExpression);
      }

      var assign = expression as IAssignment;
      if (assign != null) return 1;
      var conditional = expression as IConditional;
      if (conditional != null) {
        if (IsLogicalOr(conditional)) return 3;
        if (IsLogicalAnd(conditional)) return 4;
        return 2;
      }
      var bitwiseOr = expression as IBitwiseOr;
      if (bitwiseOr != null) return 5;
      var xor = expression as IExclusiveOr;
      if (xor != null) return 6;
      var bitwiseAnd = expression as IBitwiseAnd;
      if (bitwiseAnd != null) return 7;
      var equality = expression as IEquality;
      if (equality != null) return 8;
      var notEqual = expression as INotEquality;
      if (notEqual != null) return 8;
      var lessThan = expression as ILessThan;
      if (lessThan != null) return 9;
      var greaterThan = expression as IGreaterThan;
      if (greaterThan != null) return 9;
      var lessThanOrEqual = expression as ILessThanOrEqual;
      if (lessThanOrEqual != null) return 9;
      var greaterThanOrEqual = expression as IGreaterThanOrEqual;
      if (greaterThanOrEqual != null) return 9;
      var isTest = expression as ICheckIfInstance;
      if (isTest != null) return 9;
      var asTest = expression as ICastIfPossible;
      if (asTest != null) return 9;
      var leftShift = expression as ILeftShift;
      if (leftShift != null) return 10;
      var rightShift = expression as IRightShift;
      if (rightShift != null) return 10;
      var add = expression as IAddition;
      if (add != null && !prefix && !postfix) return 11;
      var sub = expression as ISubtraction;
      if (sub != null && !prefix && !postfix) return 11;
      var mult = expression as IMultiplication;
      if (mult != null) return 12;
      var div = expression as IDivision;
      if (div != null) return 12;
      var mod = expression as IModulus;
      if (mod != null) return 12;
      var unaryPlus = expression as IUnaryPlus;
      if (unaryPlus != null) return 13;
      var unaryMinus = expression as IUnaryNegation;
      if (unaryMinus != null) return 13;
      var logicalNot = expression as ILogicalNot;
      if (logicalNot != null) return 13;
      var bitwiseNegation = expression as IOnesComplement;
      if (bitwiseNegation != null) return 13;
      if (binaryExpression != null && prefix) return 13;
      var cast = expression as IConversion;
      if (cast != null) return 13;
      // field dereference == 14
      // function application == 14
      var arrayIndexer = expression as IArrayIndexer;
      if (arrayIndexer != null) return 14;
      if (binaryExpression != null && postfix) return 14;
      var newExpression = expression as ICreateObjectInstance;
      if (newExpression != null) return 14;
      throw new InvalidOperationException();
    }

    private bool IsLogicalOr(IConditional conditional) {
      Contract.Requires(conditional != null);

      return conditional.Type.TypeCode == PrimitiveTypeCode.Boolean && ExpressionHelper.IsIntegralOne(conditional.ResultIfTrue);
    }
    private bool IsLogicalAnd(IConditional conditional) {
      Contract.Requires(conditional != null);

      if (conditional.Type.TypeCode == PrimitiveTypeCode.Boolean) {
        if (ExpressionHelper.IsIntegralZero(conditional.ResultIfFalse)) return true; // A ? B : false is code-model for conjunction
        if (ExpressionHelper.IsIntegralZero(conditional.ResultIfTrue)) return true; // A ? false : B is handled as !(A) && B in the traverser for conditionals
      }
      return false;
    }
    private bool IsPrefix(IBinaryOperation binaryOperation) {
      Contract.Requires(binaryOperation != null);

      return binaryOperation.LeftOperand is ITargetExpression && ExpressionHelper.IsIntegralOne(binaryOperation.RightOperand) && !binaryOperation.ResultIsUnmodifiedLeftOperand;
    }
    private bool IsPostfix(IBinaryOperation binaryOperation) {
      Contract.Requires(binaryOperation != null);

      return binaryOperation.LeftOperand is ITargetExpression && ExpressionHelper.IsIntegralOne(binaryOperation.RightOperand) && binaryOperation.ResultIsUnmodifiedLeftOperand;
    }

    public override void TraverseChildren(IAddition addition) {

      var needsParen = LowerPrecedenceThanParentExpression(addition);
      var savedCurrentPrecedence = this.currentPrecedence;
      this.currentPrecedence = this.Precedence(addition);

      if (needsParen)
        this.sourceEmitterOutput.Write("(");

      if (addition.LeftOperand is ITargetExpression && ExpressionHelper.IsIntegralOne(addition.RightOperand)) {
        if (addition.ResultIsUnmodifiedLeftOperand) {
          this.Traverse(addition.LeftOperand);
          this.sourceEmitterOutput.Write("++");
        } else {
          this.sourceEmitterOutput.Write("++");
          this.Traverse(addition.LeftOperand);
        }
        goto Ret;
      }

      this.Traverse(addition.LeftOperand);
      if (addition.LeftOperand is ITargetExpression)
        this.sourceEmitterOutput.Write(" += ");
      else
        this.sourceEmitterOutput.Write(" + ");
      this.Traverse(addition.RightOperand);

    Ret:
      if (needsParen)
        this.sourceEmitterOutput.Write(")");

      this.currentPrecedence = savedCurrentPrecedence;
    }

    public override void TraverseChildren(IAddressableExpression addressableExpression) {
      ILocalDefinition/*?*/ local = addressableExpression.Definition as ILocalDefinition;
      if (local != null) {
        this.PrintLocalName(local);
        return;
      }
      IParameterDefinition/*?*/ param = addressableExpression.Definition as IParameterDefinition;
      if (param != null) {
        this.PrintParameterDefinitionName(param);
        return;
      }
      IArrayIndexer/*?*/ arrayIndexer = addressableExpression.Definition as IArrayIndexer;
      if (arrayIndexer != null) {
        this.Traverse(arrayIndexer);
        return;
      }
      IAddressDereference/*?*/ addressDereference = addressableExpression.Definition as IAddressDereference;
      if (addressDereference != null) {
        this.Traverse(addressDereference);
        return;
      }
      if (addressableExpression.Instance != null) {
        var addrOf = addressableExpression.Instance as IAddressOf;
        if (addrOf != null && addrOf.Expression.Type.IsValueType)
          this.Traverse(addrOf.Expression);
        else
          this.Traverse(addressableExpression.Instance);
        this.sourceEmitterOutput.Write(".");
      }
      IFieldReference/*?*/ field = addressableExpression.Definition as IFieldReference;
      if (field != null) {
        if (addressableExpression.Instance == null) {
          this.PrintTypeReferenceName(field.ContainingType);
          this.sourceEmitterOutput.Write(".");
        }
        this.sourceEmitterOutput.Write(field.Name.Value);
        return;
      }
      IMethodReference/*?*/ method = addressableExpression.Definition as IMethodReference;
      if (method != null) {
        this.sourceEmitterOutput.Write(MemberHelper.GetMethodSignature(method, NameFormattingOptions.Signature));
        return;
      }
      Contract.Assume(addressableExpression.Definition is IExpression);
      this.Traverse((IExpression)addressableExpression.Definition);
    }

    public override bool Equals(object obj) {
      return base.Equals(obj);
    }

    public override int GetHashCode() {
      return base.GetHashCode();
    }

    public override string ToString() {
      return base.ToString();
    }

    public override void TraverseChildren(IAddressDereference addressDereference) {
      if (addressDereference.Address.Type is IPointerTypeReference || addressDereference.Address.Type.TypeCode == PrimitiveTypeCode.IntPtr ||
        addressDereference.Address.Type.TypeCode == PrimitiveTypeCode.UIntPtr ||
        addressDereference.Address is IDupValue || addressDereference.Address is IPopValue) {
        this.sourceEmitterOutput.Write("*");
        var conv = addressDereference.Address as IConversion;
        if (conv != null && (conv.TypeAfterConversion.TypeCode == PrimitiveTypeCode.IntPtr || conv.TypeAfterConversion.TypeCode == PrimitiveTypeCode.UIntPtr)) {
          string type = TypeHelper.GetTypeName(addressDereference.Type, NameFormattingOptions.ContractNullable|NameFormattingOptions.UseTypeKeywords);
          this.sourceEmitterOutput.Write("("+type+"*)");
          this.Traverse(conv.ValueToConvert);
          return;
        }
      } else {
        var addrOf = addressDereference.Address as IAddressOf;
        if (addrOf != null) {
          this.Traverse(addrOf.Expression);
          return;
        }
      }
      this.Traverse(addressDereference.Address);
    }

    public override void TraverseChildren(IAddressOf addressOf) {
      this.sourceEmitterOutput.Write("&");
      this.Traverse(addressOf.Expression);
    }

    public override void TraverseChildren(IAliasForType aliasForType) {
      // Already outputted these at the top for IAssembly
    }

    public override void TraverseChildren(IAnonymousDelegate anonymousDelegate) {
      var nonEmptyStatementCount = 0;
      IStatement nonEmptyStatement = null;
      foreach (var statement in anonymousDelegate.Body.Statements) {
        if (!(statement is IEmptyStatement)) {
          nonEmptyStatementCount++;
          nonEmptyStatement = statement;
        }
      }
      if (nonEmptyStatementCount == 1) {
        var returnStatement = nonEmptyStatement as IReturnStatement;
        if (returnStatement != null && returnStatement.Expression != null) {
          this.Traverse(anonymousDelegate.Parameters);
          this.sourceEmitterOutput.Write(" => ");
          this.Traverse(returnStatement.Expression);
          return;
        }
        var expressionStatement = nonEmptyStatement as IExpressionStatement;
        if (expressionStatement != null && anonymousDelegate.ReturnType.TypeCode == PrimitiveTypeCode.Void) {
          this.Traverse(anonymousDelegate.Parameters);
          this.sourceEmitterOutput.Write(" => ");
          this.Traverse(expressionStatement.Expression);
          return;
        }
      }
      this.sourceEmitterOutput.Write("delegate ");
      this.Traverse(anonymousDelegate.Parameters);
      this.sourceEmitterOutput.WriteLine(" {");
      this.sourceEmitterOutput.IncreaseIndent();
      this.Traverse(anonymousDelegate.Body.Statements);
      this.sourceEmitterOutput.DecreaseIndent();
      this.sourceEmitterOutput.Write("}", true);
    }

    public override void TraverseChildren(IArrayIndexer arrayIndexer) {
      this.Traverse(arrayIndexer.IndexedObject);
      this.sourceEmitterOutput.Write("[");
      this.Traverse(arrayIndexer.Indices);
      this.sourceEmitterOutput.Write("]");
    }

    public override void TraverseChildren(IArrayTypeReference arrayTypeReference) {
      base.TraverseChildren(arrayTypeReference);
    }

    public override void TraverseChildren(IAssembly assembly) {
      foreach (var attr in SortAttributes(assembly.Attributes)) {
        var at = Utils.GetAttributeType(attr);
        if (at == SpecialAttribute.Extension ||
          at == SpecialAttribute.AssemblyDelaySign ||
          at == SpecialAttribute.AssemblyKeyFile)
          continue;
        PrintAttribute(assembly, attr, true, "assembly");
      }

      // Assembly-level pseudo-custom attributes
      foreach (var alias in assembly.ExportedTypes) {
        if (!(alias.AliasedType is INestedTypeReference)) { // Nested type aliases seem redundant and there appears to be no way to generate them via an attribute
          sourceEmitterOutput.Write("[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(");
          PrintTypeReference(alias.AliasedType);
          sourceEmitterOutput.WriteLine("))]");
        }
      }
      PrintToken(CSharpToken.NewLine);
      this.Traverse(assembly.SecurityAttributes);
      this.TraverseChildren((IModule)assembly);
    }

    public override void TraverseChildren(IAssemblyReference assemblyReference) {
      base.TraverseChildren(assemblyReference);
    }

    public override void TraverseChildren(IAssignment assignment) {
      var needsParen = LowerPrecedenceThanParentExpression(assignment);
      var savedCurrentPrecedence = this.currentPrecedence;
      this.currentPrecedence = this.Precedence(assignment);

      if (needsParen) this.sourceEmitterOutput.Write("(");

      var binOp = assignment.Source as IBinaryOperation;
      if (binOp != null && assignment.Target.Instance == null) {
        var leftBinding = binOp.LeftOperand as IBoundExpression;
        if (leftBinding != null && leftBinding.Instance == null && leftBinding.Definition == assignment.Target.Definition) {
          if (binOp is IAddition) {
            if (ExpressionHelper.IsIntegralOne(binOp.RightOperand)) { //TODO: pointer incr can have size == target type size.
              if (binOp.ResultIsUnmodifiedLeftOperand) {
                this.Traverse(assignment.Target);
                this.sourceEmitterOutput.Write("++");
              } else {
                this.sourceEmitterOutput.Write("++");
                this.Traverse(assignment.Target);
              }
            } else {
              this.Traverse(assignment.Target);
              this.sourceEmitterOutput.Write(" += ");
              this.Traverse(binOp.RightOperand);
            }
            goto Ret;
          }
          if (binOp is ISubtraction) {
            if (ExpressionHelper.IsIntegralOne(binOp.RightOperand)) { //TODO: pointer incr can have size == target type size.
              if (binOp.ResultIsUnmodifiedLeftOperand) {
                this.Traverse(assignment.Target);
                this.sourceEmitterOutput.Write("--");
              } else {
                this.sourceEmitterOutput.Write("--");
                this.Traverse(assignment.Target);
              }
            } else {
              this.Traverse(assignment.Target);
              this.sourceEmitterOutput.Write(" -= ");
              this.Traverse(binOp.RightOperand);
            }
            goto Ret;
          }
          this.Traverse(assignment.Target);
          if (binOp is IBitwiseAnd) {
            this.sourceEmitterOutput.Write(" &= ");
            this.Traverse(binOp.RightOperand);
            goto Ret;
          }
          if (binOp is IBitwiseOr) {
            this.sourceEmitterOutput.Write(" |= ");
            this.Traverse(binOp.RightOperand);
            goto Ret;
          }
          if (binOp is IDivision) {
            this.sourceEmitterOutput.Write(" /= ");
            this.Traverse(binOp.RightOperand);
            goto Ret;
          }
          if (binOp is IExclusiveOr) {
            this.sourceEmitterOutput.Write(" ^= ");
            this.Traverse(binOp.RightOperand);
            goto Ret;
          }
          if (binOp is ILeftShift) {
            this.sourceEmitterOutput.Write(" <<= ");
            this.Traverse(binOp.RightOperand);
            goto Ret;
          }
          if (binOp is IModulus) {
            this.sourceEmitterOutput.Write(" %= ");
            this.Traverse(binOp.RightOperand);
            goto Ret;
          }
          if (binOp is IMultiplication) {
            this.sourceEmitterOutput.Write(" *= ");
            this.Traverse(binOp.RightOperand);
            goto Ret;
          }
          if (binOp is IRightShift) {
            this.sourceEmitterOutput.Write(" >>= ");
            this.Traverse(binOp.RightOperand);
            goto Ret;
          }
        }
      }

      this.Traverse(assignment.Target);
      this.PrintToken(CSharpToken.Space);
      this.PrintToken(CSharpToken.Assign);
      this.PrintToken(CSharpToken.Space);
      this.Traverse(assignment.Source);

    Ret:
      if (needsParen) this.sourceEmitterOutput.Write(")");
      this.currentPrecedence = savedCurrentPrecedence;

    }

    public override void TraverseChildren(IBitwiseAnd bitwiseAnd) {

      var needsParen = LowerPrecedenceThanParentExpression(bitwiseAnd);
      var savedCurrentPrecedence = this.currentPrecedence;
      this.currentPrecedence = this.Precedence(bitwiseAnd);

      if (needsParen) this.sourceEmitterOutput.Write("(");
      this.Traverse(bitwiseAnd.LeftOperand);
      if (bitwiseAnd.LeftOperand is ITargetExpression)
        this.sourceEmitterOutput.Write(" &= ");
      else
        this.sourceEmitterOutput.Write(" & ");
      this.Traverse(bitwiseAnd.RightOperand);
      if (needsParen) this.sourceEmitterOutput.Write(")");

      this.currentPrecedence = savedCurrentPrecedence;
    }

    public override void TraverseChildren(IBitwiseOr bitwiseOr) {

      var needsParen = LowerPrecedenceThanParentExpression(bitwiseOr);
      var savedCurrentPrecedence = this.currentPrecedence;
      this.currentPrecedence = this.Precedence(bitwiseOr);

      if (needsParen) this.sourceEmitterOutput.Write("(");
      this.Traverse(bitwiseOr.LeftOperand);
      if (bitwiseOr.LeftOperand is ITargetExpression)
        this.sourceEmitterOutput.Write(" |= ");
      else
        this.sourceEmitterOutput.Write(" | ");
      this.Traverse(bitwiseOr.RightOperand);
      if (needsParen) this.sourceEmitterOutput.Write(")");

      this.currentPrecedence = savedCurrentPrecedence;
    }

    /// <summary>
    /// Special case for the source expression "new C(){ f1 = e1, f2 = e2, ... }" (where the f's can be fields
    /// or properties). See comment in the decompiler.
    /// </summary>
    public override void TraverseChildren(IBlockExpression blockExpression) {
      var boundExpression = blockExpression.Expression as IBoundExpression;
      if (boundExpression != null) {
        var localDefinition = boundExpression.Definition as ILocalDefinition;
        if (localDefinition != null) {
          var specialCase = true;
          var i = 0;
          foreach (var s in blockExpression.BlockStatement.Statements) {
            if (i == 0) {
              if (!(s is ILocalDeclarationStatement)) {
                specialCase = false;
                break;
              }
            } else {
              var expressionStatement = s as IExpressionStatement;
              var expr = expressionStatement.Expression;
              if (!((expr is IAssignment || expr is IMethodCall))) {
                specialCase = false;
                break;
              }
            }
            i++;
          }
          if (specialCase) {
            PrintNewExpressionWithInitializers(blockExpression);
            return;
          }
        }
      }
      this.sourceEmitterOutput.WriteLine("(() => {");
      this.sourceEmitterOutput.IncreaseIndent();
      this.Traverse(blockExpression.BlockStatement);
      this.sourceEmitterOutput.Write("return ", true);
      this.Traverse(blockExpression.Expression);
      this.sourceEmitterOutput.Write("; })()");
      this.sourceEmitterOutput.DecreaseIndent();
    }

    private void PrintNewExpressionWithInitializers(IBlockExpression blockExpression) {
      Contract.Requires(blockExpression != null);

      var i = 0;
      foreach (var s in blockExpression.BlockStatement.Statements) {
        if (i++ == 0) {
          var lds = s as ILocalDeclarationStatement;
          this.Traverse(lds.InitialValue);
          this.sourceEmitterOutput.WriteLine("{");
          this.sourceEmitterOutput.IncreaseIndent();
          continue;
        }
        var expressionStatement = (IExpressionStatement)s;
        this.sourceEmitterOutput.Write("", true);
        var assign = expressionStatement.Expression as IAssignment;
        if (assign != null) {
          var def = assign.Target.Definition;
          PrintBoundExpressionDefinition(null, def, false);
          this.sourceEmitterOutput.Write(" = ");
          this.Traverse(assign.Source);
          this.sourceEmitterOutput.WriteLine(", ");
          continue;
        }

        var methodCall = expressionStatement.Expression as IMethodCall;
        if (methodCall != null) {
          this.PrintMethodReferenceName(methodCall.MethodToCall, NameFormattingOptions.OmitContainingNamespace | NameFormattingOptions.OmitContainingType);
          this.sourceEmitterOutput.Write(" = ");
          foreach (var a in methodCall.Arguments) {
            this.Traverse(a);
            break;
          }
          this.sourceEmitterOutput.WriteLine(", ");
          continue;
        }

        Contract.Assume(false);
      }
      this.sourceEmitterOutput.DecreaseIndent();
      this.sourceEmitterOutput.Write("}", true);
    }

    public override void TraverseChildren(IBoundExpression boundExpression) {
      if (boundExpression.Instance != null) {
        IAddressOf/*?*/ addressOf = boundExpression.Instance as IAddressOf;
        if (addressOf != null && addressOf.Expression.Type.IsValueType) {
          this.Traverse(addressOf.Expression);
        } else {
          this.Traverse(boundExpression.Instance);
        }
        this.PrintToken(CSharpToken.Dot);
      }
      PrintBoundExpressionDefinition(boundExpression.Instance, boundExpression.Definition);
    }

    private void PrintBoundExpressionDefinition(IExpression/*?*/ instance, object definition, bool printStaticFieldType = true) {
      ILocalDefinition/*?*/ local = definition as ILocalDefinition;
      if (local != null)
        this.PrintLocalName(local);
      else {
        IFieldReference/*?*/ fr = definition as IFieldReference;
        if (fr != null) {
          if (printStaticFieldType && instance == null) {
            this.PrintTypeReferenceName(fr.ContainingType);
            this.sourceEmitterOutput.Write(".");
          }
          this.sourceEmitterOutput.Write(fr.Name.Value);
        } else {
          INamedEntity/*?*/ ne = definition as INamedEntity;
          if (ne != null)
            this.sourceEmitterOutput.Write(ne.Name.Value);
        }
      }
    }

    public override void TraverseChildren(ICastIfPossible castIfPossible) {

      var needsParen = LowerPrecedenceThanParentExpression(castIfPossible);
      var savedCurrentPrecedence = this.currentPrecedence;
      this.currentPrecedence = this.Precedence(castIfPossible);

      if (needsParen) this.sourceEmitterOutput.Write("(");
      this.Traverse(castIfPossible.ValueToCast);
      this.sourceEmitterOutput.Write(" as ");
      this.PrintTypeReference(castIfPossible.TargetType);
      if (needsParen) this.sourceEmitterOutput.Write(")");

      this.currentPrecedence = savedCurrentPrecedence;
    }

    public override void TraverseChildren(ICheckIfInstance checkIfInstance) {

      var needsParen = LowerPrecedenceThanParentExpression(checkIfInstance);
      var savedCurrentPrecedence = this.currentPrecedence;
      this.currentPrecedence = this.Precedence(checkIfInstance);

      if (needsParen) this.sourceEmitterOutput.Write("(");
      this.Traverse(checkIfInstance.Operand);
      this.sourceEmitterOutput.Write(" is ");
      this.PrintTypeReference(checkIfInstance.TypeToCheck);
      if (needsParen) this.sourceEmitterOutput.Write(")");

      this.currentPrecedence = savedCurrentPrecedence;
    }

    public override void TraverseChildren(ICompileTimeConstant constant) {
      var val = constant.Value;
      if (val == null)
        this.PrintToken(CSharpToken.Null);
      else if (val is bool)
        this.sourceEmitterOutput.Write(((bool)val) ? "true" : "false");
      else if (val is string)
        this.PrintString((string)val);
      else if (val is char)
        PrintCharacter((char)val);
      else if (val is int)
        PrintInt((int)val);
      else if (val is uint)
        PrintUint((uint)val);
      else if (val is long)
        PrintLong((long)val);
      else if (val is ulong)
        PrintUlong((ulong)val);
      else if (val is float)
        PrintFloat((float)val);
      else if (val is double)
        PrintDouble((double)val);
      else
        this.sourceEmitterOutput.Write(val.ToString());
    }

    public override void TraverseChildren(IConditional conditional) {

      var needsParen = LowerPrecedenceThanParentExpression(conditional);
      var savedCurrentPrecedence = this.currentPrecedence;
      this.currentPrecedence = this.Precedence(conditional);

      if (needsParen) this.sourceEmitterOutput.Write("(");

      if (conditional.Type.TypeCode == PrimitiveTypeCode.Boolean) {
        if (ExpressionHelper.IsIntegralOne(conditional.ResultIfTrue)) {
          this.Traverse(conditional.Condition);
          this.sourceEmitterOutput.Write(" || ");
          this.Traverse(conditional.ResultIfFalse);
          goto Ret;
        }
        if (ExpressionHelper.IsIntegralZero(conditional.ResultIfFalse)) {
          this.Traverse(conditional.Condition);
          this.sourceEmitterOutput.Write(" && ");
          this.Traverse(conditional.ResultIfTrue);
          goto Ret;
        }
        if (ExpressionHelper.IsIntegralOne(conditional.ResultIfFalse)) {
          var ln = conditional.Condition as ILogicalNot;
          if (ln != null)
          {
            this.Traverse(ln.Operand);
          }
          else
          {
            this.sourceEmitterOutput.Write("!");
            var x = this.currentPrecedence;
            this.currentPrecedence = 13; // precedence of unary negation
            this.Traverse(conditional.Condition);
            this.currentPrecedence = x;
          }
          this.sourceEmitterOutput.Write(" || ");
          this.Traverse(conditional.ResultIfTrue);
          goto Ret;
        }
        if (ExpressionHelper.IsIntegralZero(conditional.ResultIfTrue)) {
          var ln = conditional.Condition as ILogicalNot;
          if (ln != null) {
            this.Traverse(ln.Operand);
          } else {
            this.sourceEmitterOutput.Write("!");
            var x = this.currentPrecedence;
            this.currentPrecedence = 13; // precedence of unary negation
            this.Traverse(conditional.Condition);
            this.currentPrecedence = x;
          }
          this.sourceEmitterOutput.Write(" && ");
          this.Traverse(conditional.ResultIfFalse);
          goto Ret;
        }
      }
      this.Traverse(conditional.Condition);
      this.sourceEmitterOutput.Write(" ? ");
      this.Traverse(conditional.ResultIfTrue);
      this.sourceEmitterOutput.Write(" : ");
      this.Traverse(conditional.ResultIfFalse);

      Ret:
      if (needsParen) this.sourceEmitterOutput.Write(")");
      this.currentPrecedence = savedCurrentPrecedence;
    }

    public override void TraverseChildren(IConversion conversion) {
      if (conversion.ValueToConvert is IVectorLength) {
        this.Traverse(((IVectorLength)conversion.ValueToConvert).Vector);
        if (conversion.TypeAfterConversion.TypeCode == PrimitiveTypeCode.Int64 || conversion.TypeAfterConversion.TypeCode == PrimitiveTypeCode.UInt64)
          this.sourceEmitterOutput.Write(".LongLength");
        else
          this.sourceEmitterOutput.Write(".Length");
        return;
      }

      var needsParen = LowerPrecedenceThanParentExpression(conversion);
      var savedCurrentPrecedence = this.currentPrecedence;
      this.currentPrecedence = this.Precedence(conversion);

      if (conversion.CheckNumericRange)
        this.sourceEmitterOutput.Write("checked");
      if (needsParen)
        this.sourceEmitterOutput.Write("(");
      this.sourceEmitterOutput.Write("(");
      this.PrintTypeReferenceName(conversion.TypeAfterConversion);
      this.sourceEmitterOutput.Write(")");
      this.Traverse(conversion.ValueToConvert);
      if (needsParen)
        this.sourceEmitterOutput.Write(")");

      this.currentPrecedence = savedCurrentPrecedence;
    }

    public override void TraverseChildren(ICreateArray createArray) {
      this.sourceEmitterOutput.Write("new ");
      this.PrintTypeReference(createArray.ElementType);
      this.sourceEmitterOutput.Write("[");
      this.Traverse(createArray.Sizes);
      this.sourceEmitterOutput.Write("]");
      if (IteratorHelper.EnumerableIsNotEmpty(createArray.Initializers)) {
        this.sourceEmitterOutput.Write(" {");
        this.Traverse(createArray.Initializers);
        this.sourceEmitterOutput.Write("}");
      }
    }

    private void TraverseChildren(IEnumerable<ulong> sizes) {
      Contract.Requires(sizes != null);

      bool emitComma = false;
      foreach (ulong size in sizes) {
        if (emitComma) this.sourceEmitterOutput.Write(", ");
        this.sourceEmitterOutput.Write(size.ToString());
        emitComma = true;
      }
    }

    public override void TraverseChildren(ICreateDelegateInstance/*!*/ createDelegateInstance) {
      if (createDelegateInstance.Instance != null) {
        ICompileTimeConstant constant = createDelegateInstance.Instance as ICompileTimeConstant;
        if (constant == null || constant.Value != null) {
          this.Traverse(createDelegateInstance.Instance);
          this.PrintToken(CSharpToken.Dot);
        }
      }
      this.PrintMethodDefinitionName(createDelegateInstance.MethodToCallViaDelegate.ResolvedMethod);
      //base.TraverseChildren(createDelegateInstance);
    }

    public override void TraverseChildren(ICreateObjectInstance createObjectInstance) {
      this.PrintToken(CSharpToken.New);
      this.PrintTypeReferenceName(createObjectInstance.MethodToCall.ContainingType);
      this.PrintArgumentList(createObjectInstance.Arguments, createObjectInstance.MethodToCall.Parameters);
    }

    public override void TraverseChildren(ICustomAttribute customAttribute) {
      // Different uses of custom attributes must print them directly based on context
      //base.TraverseChildren(customAttribute);
    }

    public override void TraverseChildren(ICustomModifier customModifier) {
      base.TraverseChildren(customModifier);
    }

    public override void TraverseChildren(IDefaultValue defaultValue) {
      this.sourceEmitterOutput.Write("default(");
      this.PrintTypeReference(defaultValue.DefaultValueType);
      this.sourceEmitterOutput.Write(")");
    }

    public override void TraverseChildren(IDivision division) {

      var needsParen = LowerPrecedenceThanParentExpression(division);
      var savedCurrentPrecedence = this.currentPrecedence;
      this.currentPrecedence = this.Precedence(division);

      if (needsParen) this.sourceEmitterOutput.Write("(");
      this.Traverse(division.LeftOperand);
      if (division.LeftOperand is ITargetExpression)
        this.sourceEmitterOutput.Write(" /= ");
      else
        this.sourceEmitterOutput.Write(" / ");
      this.Traverse(division.RightOperand);
      if (needsParen) this.sourceEmitterOutput.Write(")");

      this.currentPrecedence = savedCurrentPrecedence;
    }

    public override void TraverseChildren(IDupValue dupValue) {
      this.sourceEmitterOutput.Write("dup");
    }

    public override void TraverseChildren(IEquality equality) {

      var needsParen = LowerPrecedenceThanParentExpression(equality);
      var savedCurrentPrecedence = this.currentPrecedence;
      this.currentPrecedence = this.Precedence(equality);

      if (needsParen) this.sourceEmitterOutput.Write("(");
      this.Traverse(equality.LeftOperand);
      this.sourceEmitterOutput.Write(" == ");
      this.Traverse(equality.RightOperand);
      if (needsParen) this.sourceEmitterOutput.Write(")");

      this.currentPrecedence = savedCurrentPrecedence;
    }

    public override void TraverseChildren(IExclusiveOr exclusiveOr) {

      var needsParen = LowerPrecedenceThanParentExpression(exclusiveOr);
      var savedCurrentPrecedence = this.currentPrecedence;
      this.currentPrecedence = this.Precedence(exclusiveOr);

      if (needsParen) this.sourceEmitterOutput.Write("(");
      this.Traverse(exclusiveOr.LeftOperand);
      if (exclusiveOr.LeftOperand is ITargetExpression)
        this.sourceEmitterOutput.Write(" ^= ");
      else
        this.sourceEmitterOutput.Write(" ^ ");
      this.Traverse(exclusiveOr.RightOperand);
      if (needsParen) this.sourceEmitterOutput.Write(")");

      this.currentPrecedence = savedCurrentPrecedence;
    }

    public new void Traverse(IEnumerable<IExpression> arguments) {
      Contract.Requires(arguments != null);

      bool needComma = false;
      foreach (IExpression argument in arguments) {
        if (needComma) {
          this.PrintToken(CSharpToken.Comma);
          this.PrintToken(CSharpToken.Space);
        }
        this.Traverse(argument);
        needComma = true;
      }
    }

    public override void TraverseChildren(IExpression expression) {
      base.TraverseChildren(expression);
    }

    public override void TraverseChildren(IGetTypeOfTypedReference getTypeOfTypedReference) {
      this.sourceEmitterOutput.Write("__reftype(");
      this.Traverse(getTypeOfTypedReference.TypedReference);
      this.sourceEmitterOutput.Write(")");
    }

    public override void TraverseChildren(IGetValueOfTypedReference getValueOfTypedReference) {
      this.sourceEmitterOutput.Write("__refvalue(");
      this.Traverse(getValueOfTypedReference.TypedReference);
      this.sourceEmitterOutput.Write(", ");
      this.PrintTypeReferenceName(getValueOfTypedReference.TargetType);
      this.sourceEmitterOutput.Write(")");
    }

    public override void TraverseChildren(IGreaterThan greaterThan) {

      if (greaterThan.IsUnsignedOrUnordered && !TypeHelper.IsPrimitiveInteger(greaterThan.LeftOperand.Type)) {
        this.sourceEmitterOutput.Write("!(");
        this.Traverse(greaterThan.LeftOperand);
        this.sourceEmitterOutput.Write(" <= ");
        this.Traverse(greaterThan.RightOperand);
        this.sourceEmitterOutput.Write(")");
        return;
      }

      var needsParen = LowerPrecedenceThanParentExpression(greaterThan);
      var savedCurrentPrecedence = this.currentPrecedence;
      this.currentPrecedence = this.Precedence(greaterThan);

      if (needsParen) this.sourceEmitterOutput.Write("(");
      if (greaterThan.IsUnsignedOrUnordered && TypeHelper.IsPrimitiveInteger(greaterThan.LeftOperand.Type) && 
        greaterThan.LeftOperand.Type != TypeHelper.UnsignedEquivalent(greaterThan.LeftOperand.Type)) {
        if (needsParen) this.sourceEmitterOutput.Write("(");
        this.PrintTypeReferenceName(TypeHelper.UnsignedEquivalent(greaterThan.LeftOperand.Type));
        if (needsParen) this.sourceEmitterOutput.Write(")");
      }
      this.Traverse(greaterThan.LeftOperand);
      this.sourceEmitterOutput.Write(" > ");
      if (greaterThan.IsUnsignedOrUnordered && TypeHelper.IsPrimitiveInteger(greaterThan.RightOperand.Type) && 
        greaterThan.RightOperand.Type != TypeHelper.UnsignedEquivalent(greaterThan.RightOperand.Type)) {
        if (needsParen) this.sourceEmitterOutput.Write("(");
        this.PrintTypeReferenceName(TypeHelper.UnsignedEquivalent(greaterThan.RightOperand.Type));
        if (needsParen) this.sourceEmitterOutput.Write(")");
      }
      this.Traverse(greaterThan.RightOperand);
      if (needsParen) this.sourceEmitterOutput.Write(")");

      this.currentPrecedence = savedCurrentPrecedence;
    }

    public override void TraverseChildren(IGreaterThanOrEqual greaterThanOrEqual) {

      if (greaterThanOrEqual.IsUnsignedOrUnordered && !TypeHelper.IsPrimitiveInteger(greaterThanOrEqual.LeftOperand.Type)) {
        this.sourceEmitterOutput.Write("!(");
        this.Traverse(greaterThanOrEqual.LeftOperand);
        this.sourceEmitterOutput.Write(" < ");
        this.Traverse(greaterThanOrEqual.RightOperand);
        this.sourceEmitterOutput.Write(")");
        return;
      }

      var needsParen = LowerPrecedenceThanParentExpression(greaterThanOrEqual);
      var savedCurrentPrecedence = this.currentPrecedence;
      this.currentPrecedence = this.Precedence(greaterThanOrEqual);

      if (needsParen) this.sourceEmitterOutput.Write("(");
      if (greaterThanOrEqual.IsUnsignedOrUnordered && TypeHelper.IsPrimitiveInteger(greaterThanOrEqual.LeftOperand.Type) && 
        greaterThanOrEqual.LeftOperand.Type != TypeHelper.UnsignedEquivalent(greaterThanOrEqual.LeftOperand.Type)) {
        if (needsParen) this.sourceEmitterOutput.Write("(");
        this.PrintTypeReferenceName(TypeHelper.UnsignedEquivalent(greaterThanOrEqual.LeftOperand.Type));
        if (needsParen) this.sourceEmitterOutput.Write(")");
      }
      this.Traverse(greaterThanOrEqual.LeftOperand);
      this.sourceEmitterOutput.Write(" >= ");
      if (greaterThanOrEqual.IsUnsignedOrUnordered && TypeHelper.IsPrimitiveInteger(greaterThanOrEqual.RightOperand.Type) && 
        greaterThanOrEqual.RightOperand.Type != TypeHelper.UnsignedEquivalent(greaterThanOrEqual.RightOperand.Type)) {
        if (needsParen) this.sourceEmitterOutput.Write("(");
        this.PrintTypeReferenceName(TypeHelper.UnsignedEquivalent(greaterThanOrEqual.RightOperand.Type));
        if (needsParen) this.sourceEmitterOutput.Write(")");
      }
      this.Traverse(greaterThanOrEqual.RightOperand);
      if (needsParen) this.sourceEmitterOutput.Write(")");

      this.currentPrecedence = savedCurrentPrecedence;
    }

    public override void TraverseChildren(ILeftShift leftShift) {

      var needsParen = LowerPrecedenceThanParentExpression(leftShift);
      var savedCurrentPrecedence = this.currentPrecedence;
      this.currentPrecedence = this.Precedence(leftShift);

      if (needsParen) this.sourceEmitterOutput.Write("(");
      this.Traverse(leftShift.LeftOperand);
      if (leftShift.LeftOperand is ITargetExpression)
        this.sourceEmitterOutput.Write(" <<= ");
      else
        this.sourceEmitterOutput.Write(" << ");
      this.Traverse(leftShift.RightOperand);
      if (needsParen) this.sourceEmitterOutput.Write(")");

      this.currentPrecedence = savedCurrentPrecedence;
    }

    public override void TraverseChildren(ILessThan lessThan) {

      if (lessThan.IsUnsignedOrUnordered && !TypeHelper.IsPrimitiveInteger(lessThan.LeftOperand.Type)) {
        this.sourceEmitterOutput.Write("!(");
        this.Traverse(lessThan.LeftOperand);
        this.sourceEmitterOutput.Write(" >= ");
        this.Traverse(lessThan.RightOperand);
        this.sourceEmitterOutput.Write(")");
        return;
      }

      var needsParen = LowerPrecedenceThanParentExpression(lessThan);
      var savedCurrentPrecedence = this.currentPrecedence;
      this.currentPrecedence = this.Precedence(lessThan);

      if (needsParen) this.sourceEmitterOutput.Write("(");
      if (lessThan.IsUnsignedOrUnordered && TypeHelper.IsPrimitiveInteger(lessThan.LeftOperand.Type) && 
        lessThan.LeftOperand.Type != TypeHelper.UnsignedEquivalent(lessThan.LeftOperand.Type)) {
        if (needsParen) this.sourceEmitterOutput.Write("(");
        this.PrintTypeReferenceName(TypeHelper.UnsignedEquivalent(lessThan.LeftOperand.Type));
        if (needsParen) this.sourceEmitterOutput.Write(")");
      }
      this.Traverse(lessThan.LeftOperand);
      this.sourceEmitterOutput.Write(" < ");
      if (lessThan.IsUnsignedOrUnordered && TypeHelper.IsPrimitiveInteger(lessThan.RightOperand.Type) && 
        lessThan.RightOperand.Type != TypeHelper.UnsignedEquivalent(lessThan.RightOperand.Type)) {
        if (needsParen) this.sourceEmitterOutput.Write("(");
        this.PrintTypeReferenceName(TypeHelper.UnsignedEquivalent(lessThan.RightOperand.Type));
        if (needsParen) this.sourceEmitterOutput.Write(")");
      }
      this.Traverse(lessThan.RightOperand);
      if (needsParen) this.sourceEmitterOutput.Write(")");

      this.currentPrecedence = savedCurrentPrecedence;
    }

    public override void TraverseChildren(ILessThanOrEqual lessThanOrEqual) {
      if (lessThanOrEqual.IsUnsignedOrUnordered && !TypeHelper.IsPrimitiveInteger(lessThanOrEqual.LeftOperand.Type)) {
        this.sourceEmitterOutput.Write("!(");
        this.Traverse(lessThanOrEqual.LeftOperand);
        this.sourceEmitterOutput.Write(" > ");
        this.Traverse(lessThanOrEqual.RightOperand);
        this.sourceEmitterOutput.Write(")");
        return;
      }

      var needsParen = LowerPrecedenceThanParentExpression(lessThanOrEqual);
      var savedCurrentPrecedence = this.currentPrecedence;
      this.currentPrecedence = this.Precedence(lessThanOrEqual);

      if (needsParen) this.sourceEmitterOutput.Write("(");
      if (lessThanOrEqual.IsUnsignedOrUnordered && TypeHelper.IsPrimitiveInteger(lessThanOrEqual.LeftOperand.Type) && 
        lessThanOrEqual.LeftOperand.Type != TypeHelper.UnsignedEquivalent(lessThanOrEqual.LeftOperand.Type)) {
        if (needsParen) this.sourceEmitterOutput.Write("(");
        this.PrintTypeReferenceName(TypeHelper.UnsignedEquivalent(lessThanOrEqual.LeftOperand.Type));
        if (needsParen) this.sourceEmitterOutput.Write(")");
      }
      this.Traverse(lessThanOrEqual.LeftOperand);
      this.sourceEmitterOutput.Write(" <= ");
      if (lessThanOrEqual.IsUnsignedOrUnordered && TypeHelper.IsPrimitiveInteger(lessThanOrEqual.RightOperand.Type) && 
        lessThanOrEqual.RightOperand.Type != TypeHelper.UnsignedEquivalent(lessThanOrEqual.RightOperand.Type)) {
        if (needsParen) this.sourceEmitterOutput.Write("(");
        this.PrintTypeReferenceName(TypeHelper.UnsignedEquivalent(lessThanOrEqual.RightOperand.Type));
        if (needsParen) this.sourceEmitterOutput.Write(")");
      }
      this.Traverse(lessThanOrEqual.RightOperand);
      if (needsParen) this.sourceEmitterOutput.Write(")");

      this.currentPrecedence = savedCurrentPrecedence;
    }

    public override void TraverseChildren(ILogicalNot logicalNot) {
      var needsParen = LowerPrecedenceThanParentExpression(logicalNot);
      var savedCurrentPrecedence = this.currentPrecedence;
      this.currentPrecedence = this.Precedence(logicalNot);

      if (needsParen) this.sourceEmitterOutput.Write("(");
      this.sourceEmitterOutput.Write("!");
      this.Traverse(logicalNot.Operand);
      if (needsParen) this.sourceEmitterOutput.Write(")");

      this.currentPrecedence = savedCurrentPrecedence;
    }

    public override void TraverseChildren(IMakeTypedReference makeTypedReference) {
      this.sourceEmitterOutput.Write("__makeref(");
      this.Traverse(makeTypedReference.Operand);
      this.sourceEmitterOutput.Write(")");
    }

    public override void TraverseChildren(IManagedPointerTypeReference managedPointerTypeReference) {
      base.TraverseChildren(managedPointerTypeReference);
    }

    public override void TraverseChildren(IMarshallingInformation marshallingInformation) {
      base.TraverseChildren(marshallingInformation);
    }

    public override void TraverseChildren(IMetadataConstant constant) {
      var val = constant.Value;
      if (val == null)
        this.PrintToken(CSharpToken.Null);
      else if (constant.Type.ResolvedType.IsEnum)
        PrintEnumValue(constant.Type.ResolvedType, val);
      else if (val is string)
        PrintString((string)val);
      else if (val is bool)
        PrintToken((bool)val ? CSharpToken.True : CSharpToken.False);
      else if (val is char)
        this.sourceEmitterOutput.Write(String.Format("'{0}'", EscapeChar((char)val, false)));
      else if (val is float)
        PrintFloat((float)val);
      else if (val is double)
        PrintDouble((double)val);
      else if (val is int)
        PrintInt((int)val);
      else if (val is uint)
        PrintUint((uint)val);
      else if (val is long)
        PrintLong((long)val);
      else if (val is ulong)
        PrintUlong((ulong)val);
      else if (val is float)
        PrintFloat((float)val);
      else if (val is double)
        PrintDouble((double)val);
      else
        this.sourceEmitterOutput.Write(constant.Value.ToString());
    }

    public virtual void PrintFloat(float value) {
      // Use symbolic names for common constants
      if (float.IsNaN(value))
        sourceEmitterOutput.Write("float.NaN");
      else if (float.IsPositiveInfinity(value))
        sourceEmitterOutput.Write("float.PositiveInfinity");
      else if (float.IsNegativeInfinity(value))
        sourceEmitterOutput.Write("float.NegativeInfinity");
      else if (value == float.Epsilon)
        sourceEmitterOutput.Write("float.Epsilon");
      else if (value == float.MaxValue)
        sourceEmitterOutput.Write("float.MaxValue");
      else if (value == float.MinValue)
        sourceEmitterOutput.Write("float.MinValue");
      else
        sourceEmitterOutput.Write(value.ToString("R") + "f"); // round-trip format 
    }

    public virtual void PrintDouble(double value) {
      // Use symbolic names for common constants
      if (double.IsNaN(value))
        sourceEmitterOutput.Write("double.NaN");
      else if (double.IsPositiveInfinity(value))
        sourceEmitterOutput.Write("double.PositiveInfinity");
      else if (double.IsNegativeInfinity(value))
        sourceEmitterOutput.Write("double.NegativeInfinity");
      else if (value == double.Epsilon)
        sourceEmitterOutput.Write("double.Epsilon");
      else if (value == double.MaxValue)
        sourceEmitterOutput.Write("double.MaxValue");
      else if (value == double.MinValue)
        sourceEmitterOutput.Write("double.MinValue");
      else {
        var str = value.ToString("R");
        sourceEmitterOutput.Write(str);
        if (str.IndexOfAny(new char[] { '.', 'e', 'E' }) < 0)
          sourceEmitterOutput.Write(".0");
      }
    }

    public virtual void PrintLong(long value) {
      if (value == long.MaxValue)
        sourceEmitterOutput.Write("long.MaxValue");
      else if (value == long.MinValue)
        sourceEmitterOutput.Write("long.MinValue");
      else
        sourceEmitterOutput.Write(value.ToString());
    }

    public virtual void PrintUlong(ulong value) {
      if (value == ulong.MaxValue)
        sourceEmitterOutput.Write("ulong.MaxValue");
      else
        sourceEmitterOutput.Write(value.ToString());
    }

    public virtual void PrintInt(int value) {
      if (value == int.MaxValue)
        sourceEmitterOutput.Write("int.MaxValue");
      else if (value == int.MinValue)
        sourceEmitterOutput.Write("int.MinValue");
      else
        sourceEmitterOutput.Write(value.ToString());
    }

    public virtual void PrintUint(uint value) {
      if (value == uint.MaxValue)
        sourceEmitterOutput.Write("uint.MaxValue");
      else
        sourceEmitterOutput.Write(value.ToString());
    }

    public virtual void PrintCharacter(char c) {
      sourceEmitterOutput.Write("'");
      sourceEmitterOutput.Write(EscapeChar(c, true));
      sourceEmitterOutput.Write("'");
    }

    public virtual void PrintEnumValue(ITypeDefinition enumType, object valObj) {
      Contract.Requires(enumType != null);
      Contract.Requires(valObj != null);

      bool flags = (Utils.FindAttribute(enumType.Attributes, SpecialAttribute.Flags) != null);

      // Loop through all the enum constants looking for a match
      ulong value = UnboxToULong(valObj);
      bool success = false;
      List<IFieldDefinition> constants = new List<IFieldDefinition>();
      foreach (var f in enumType.Fields) {
        if (f.IsCompileTimeConstant && TypeHelper.TypesAreEquivalent(f.Type, enumType))
          constants.Add(f);
      }
      // Sort by order in type - hopefully this gives the minimum set of flags
      constants.Sort((f1, f2) => {
        if (f1 is IMetadataObjectWithToken && f2 is IMetadataObjectWithToken)
          return ((IMetadataObjectWithToken)f1).TokenValue.CompareTo(((IMetadataObjectWithToken)f2).TokenValue);
        return UnboxToULong(f1.CompileTimeValue.Value).CompareTo(UnboxToULong(f2.CompileTimeValue.Value));
      });

      // If the value has the high bit set, and no constant does, then it's probably best represented as a negation
      bool negate = false;
      int nBits = Marshal.SizeOf(valObj)*8;
      ulong highBit = 1ul << (nBits - 1);
      if (flags && (value & highBit) == highBit && constants.Count > 0 && (UnboxToULong(constants[0].CompileTimeValue.Value) & highBit) == 0) {
        value = (~value) & ((1UL << nBits) - 1);
        negate = true;
        sourceEmitterOutput.Write("~(");
      }
      ulong valLeft = value;
      foreach (var c in constants) {
        ulong fv = UnboxToULong(c.CompileTimeValue.Value);
        if (valLeft == fv || (flags && (fv != 0) && ((valLeft & fv) == fv))) {
          if (valLeft != value)
            sourceEmitterOutput.Write(" | ");
          TraverseChildren((IFieldReference)c);
          valLeft -= fv;
          if (valLeft == 0) {
            success = true;
            break;
          }
        }
      }
      // No match, output cast
      if (!success) {
        if (valLeft != value)
          sourceEmitterOutput.Write(" | ");
        sourceEmitterOutput.Write("unchecked((");
        TraverseChildren((ITypeReference)enumType);
        sourceEmitterOutput.Write(")0x" + valLeft.ToString("X") + ")");
      }
      if (negate)
        sourceEmitterOutput.Write(")");
    }

    private static ulong UnboxToULong(object obj) {
      Contract.Requires(obj != null);

      // Can't just cast - must unbox to specific type.
      // Can't use Convert.ToUInt64 - it'll throw for negative numbers
      switch (Convert.GetTypeCode(obj)) {
        case TypeCode.Byte:
          return (ulong)(Byte)obj;
        case TypeCode.SByte:
          return (ulong)(Byte)(SByte)obj;
        case TypeCode.UInt16:
          return (ulong)(UInt16)obj;
        case TypeCode.Int16:
          return (ulong)(UInt16)(Int16)obj;
        case TypeCode.UInt32:
          return (ulong)(UInt32)obj;
        case TypeCode.Int32:
          return (ulong)(UInt32)(Int32)obj;
        case TypeCode.UInt64:
          return (ulong)obj;
        case TypeCode.Int64:
          return (ulong)(Int64)obj;
      }
      // Argument must be of integral type (not in message becaseu we don't want english strings in CCI)
      throw new ArgumentException();
    }

    public override void TraverseChildren(IMetadataCreateArray createArray) {
      base.TraverseChildren(createArray);
    }

    public override void TraverseChildren(IMetadataNamedArgument namedArgument) {
      this.sourceEmitterOutput.Write(namedArgument.ArgumentName.Value+" = ");
      this.Traverse(namedArgument.ArgumentValue);
    }

    public override void TraverseChildren(IMetadataTypeOf typeOf) {
      PrintToken(CSharpToken.TypeOf);
      PrintToken(CSharpToken.LeftParenthesis);
      this.PrintTypeReference(typeOf.TypeToGet);
      PrintToken(CSharpToken.RightParenthesis);
    }

    public override void TraverseChildren(IMethodCall methodCall) {
      NameFormattingOptions options = NameFormattingOptions.None;
      bool delegateInvocation = false;
      if (!methodCall.IsStaticCall && !methodCall.IsJumpCall) {
        IAddressOf/*?*/ addressOf = methodCall.ThisArgument as IAddressOf;
        if (addressOf != null) {
          this.Traverse(addressOf.Expression);
        } else {
          this.Traverse(methodCall.ThisArgument);
          var methodReference = methodCall.MethodToCall;
          delegateInvocation = methodReference.Name.Value == "Invoke" && methodReference.ContainingType.ResolvedType.IsDelegate;
        }
        if (!delegateInvocation) this.PrintToken(CSharpToken.Dot);
        options |= NameFormattingOptions.OmitContainingNamespace | NameFormattingOptions.OmitContainingType;
      } else {
        // it is a static call, so see if it is an operator
        var methodDefinition = methodCall.MethodToCall.ResolvedMethod;
        if (IsOperator(methodDefinition) && !IsConversionOperator(methodDefinition)) {
          var opName = MapOperatorNameToCSharp(methodDefinition);
          if (opName.Length >= 9) {
            opName = opName.Substring(9);
          }
          if (methodDefinition.ParameterCount == 1) {
            this.sourceEmitterOutput.Write(opName);
            this.Traverse(methodCall.Arguments.ElementAt(0));
          } else {
            this.Traverse(methodCall.Arguments.ElementAt(0));
            this.sourceEmitterOutput.Write(" ");
            this.sourceEmitterOutput.Write(opName);
            this.sourceEmitterOutput.Write(" ");
            this.Traverse(methodCall.Arguments.ElementAt(1));
          }
          return;
        }
      }
      if (!delegateInvocation)
        this.PrintMethodReferenceName(methodCall.MethodToCall, options);
      if (methodCall.MethodToCall.Name.Value.StartsWith("get_")) {
        if (methodCall.MethodToCall.ParameterCount > 0) {
          this.sourceEmitterOutput.Write("[");
          this.Traverse(methodCall.Arguments);
          this.sourceEmitterOutput.Write("]");
        }
      } else if (methodCall.MethodToCall.Name.Value.StartsWith("set_") && methodCall.MethodToCall.ParameterCount > 0) {
        var argList = new List<IExpression>(methodCall.Arguments);
        if (argList.Count > 1) {
          this.sourceEmitterOutput.Write("[");
          for (int i = 0, n = argList.Count-1; i < n; i++) {
            if (i > 0) this.sourceEmitterOutput.Write(", ");
            this.Traverse(argList[i]);
          }
          this.sourceEmitterOutput.Write("]");
        }
        this.sourceEmitterOutput.Write(" = ");
        this.Traverse(argList[argList.Count-1]);
      } else 
        this.PrintArgumentList(methodCall.Arguments, methodCall.MethodToCall.Parameters);
    }

    private void PrintArgumentList(IEnumerable<IExpression> arguments, IEnumerable<IParameterTypeInformation> parameters) {
      Contract.Requires(arguments != null);
      Contract.Requires(parameters != null);

      this.sourceEmitterOutput.Write("(");
      var paramEnum = parameters.GetEnumerator();
      bool paramIsValid = true;
      bool needComma = false;
      foreach (IExpression argument in arguments) {
        paramIsValid = paramIsValid && paramEnum.MoveNext();
        if (needComma) {
          this.PrintToken(CSharpToken.Comma);
          this.PrintToken(CSharpToken.Space);
        }
        var addressOf = argument as IAddressOf;
        if (addressOf != null) {
          string modifier = "ref ";
          if (paramIsValid) {
            var pardef = paramEnum.Current as IParameterDefinition;
            if (pardef != null && pardef.IsByReference && pardef.IsOut && !pardef.IsIn)
              modifier = "out ";
          }
          this.sourceEmitterOutput.Write(modifier);
          this.Traverse(addressOf.Expression);
        } else
          this.Traverse(argument);
        needComma = true;
      }
      this.sourceEmitterOutput.Write(")");
    }

    public override void TraverseChildren(IMethodImplementation methodImplementation) {
      base.TraverseChildren(methodImplementation);
    }

    public override void TraverseChildren(IMethodReference methodReference) {
      base.TraverseChildren(methodReference);
    }

    public override void TraverseChildren(IModifiedTypeReference modifiedTypeReference) {
      base.TraverseChildren(modifiedTypeReference);
    }

    public override void TraverseChildren(IModule module) {
      foreach (var attr in SortAttributes(module.ModuleAttributes)) {
        PrintAttribute(module, attr, true, "module");
      }
      this.Traverse(module.UnitNamespaceRoot);
    }

    public override void TraverseChildren(IModuleReference moduleReference) {
      base.TraverseChildren(moduleReference);
    }

    public override void TraverseChildren(IModulus modulus) {

      var needsParen = LowerPrecedenceThanParentExpression(modulus);
      var savedCurrentPrecedence = this.currentPrecedence;
      this.currentPrecedence = this.Precedence(modulus);

      if (needsParen) this.sourceEmitterOutput.Write("(");
      this.Traverse(modulus.LeftOperand);
      if (modulus.LeftOperand is ITargetExpression)
        this.sourceEmitterOutput.Write(" %= ");
      else
        this.sourceEmitterOutput.Write(" % ");
      this.Traverse(modulus.RightOperand);
      if (needsParen) this.sourceEmitterOutput.Write(")");

      this.currentPrecedence = savedCurrentPrecedence;
    }

    public override void TraverseChildren(IMultiplication multiplication) {

      var needsParen = LowerPrecedenceThanParentExpression(multiplication);
      var savedCurrentPrecedence = this.currentPrecedence;
      this.currentPrecedence = this.Precedence(multiplication);

      if (needsParen) this.sourceEmitterOutput.Write("(");
      this.Traverse(multiplication.LeftOperand);
      if (multiplication.LeftOperand is ITargetExpression)
        this.sourceEmitterOutput.Write(" *= ");
      else
        this.sourceEmitterOutput.Write(" * ");
      this.Traverse(multiplication.RightOperand);
      if (needsParen) this.sourceEmitterOutput.Write(")");

      this.currentPrecedence = savedCurrentPrecedence;
    }

    public override void TraverseChildren(INamedArgument namedArgument) {
      base.TraverseChildren(namedArgument);
    }

    public override void TraverseChildren(INamespaceAliasForType namespaceAliasForType) {
      base.TraverseChildren(namespaceAliasForType);
    }

    public override void TraverseChildren(INamespaceTypeReference namespaceTypeReference) {
      base.TraverseChildren(namespaceTypeReference);
    }

    public override void TraverseChildren(INestedTypeReference nestedTypeReference) {
      base.TraverseChildren(nestedTypeReference);
    }

    public override void TraverseChildren(INotEquality notEquality) {

      var needsParen = LowerPrecedenceThanParentExpression(notEquality);
      var savedCurrentPrecedence = this.currentPrecedence;
      this.currentPrecedence = this.Precedence(notEquality);

      if (needsParen) this.sourceEmitterOutput.Write("(");
      this.Traverse(notEquality.LeftOperand);
      this.sourceEmitterOutput.Write(" != ");
      this.Traverse(notEquality.RightOperand);
      if (needsParen) this.sourceEmitterOutput.Write(")");

      this.currentPrecedence = savedCurrentPrecedence;
    }

    public override void TraverseChildren(IOldValue oldValue) {
      this.sourceEmitterOutput.Write("Contract.Old(");
      base.Traverse(oldValue.Expression);
      this.sourceEmitterOutput.Write(")");
    }

    public override void TraverseChildren(IOnesComplement onesComplement) {
      base.TraverseChildren(onesComplement);
    }

    public override void TraverseChildren(IOperation operation) {
      base.TraverseChildren(operation);
    }

    public override void TraverseChildren(IOperationExceptionInformation operationExceptionInformation) {
      base.TraverseChildren(operationExceptionInformation);
    }

    public override void TraverseChildren(IOutArgument outArgument) {
      base.TraverseChildren(outArgument);
    }

    public override void TraverseChildren(IParameterTypeInformation parameterTypeInformation) {
      base.TraverseChildren(parameterTypeInformation);
    }

    public override void TraverseChildren(IPlatformInvokeInformation platformInvokeInformation) {
      base.TraverseChildren(platformInvokeInformation);
    }

    public override void TraverseChildren(IPointerCall pointerCall) {
      base.TraverseChildren(pointerCall);
    }

    public override void TraverseChildren(IPointerTypeReference pointerTypeReference) {
      base.TraverseChildren(pointerTypeReference);
    }

    public override void TraverseChildren(IPopValue popValue) {
      this.sourceEmitterOutput.Write("pop");
    }

    public override void TraverseChildren(IRefArgument refArgument) {
      base.TraverseChildren(refArgument);
    }

    public override void TraverseChildren(IResourceReference resourceReference) {
      base.TraverseChildren(resourceReference);
    }

    public override void TraverseChildren(IReturnValue returnValue) {
      //this.sourceEmitterOutput.Write("result");
      this.sourceEmitterOutput.Write("Contract.Result<");
      this.PrintTypeReference(returnValue.Type);
      this.sourceEmitterOutput.Write(">()");
    }

    public override void TraverseChildren(IRightShift rightShift) {

      var needsParen = LowerPrecedenceThanParentExpression(rightShift);
      var savedCurrentPrecedence = this.currentPrecedence;
      this.currentPrecedence = this.Precedence(rightShift);

      if (needsParen) this.sourceEmitterOutput.Write("(");
      this.Traverse(rightShift.LeftOperand);
      if (rightShift.LeftOperand is ITargetExpression)
        this.sourceEmitterOutput.Write(" >>= ");
      else
        this.sourceEmitterOutput.Write(" >> ");
      this.Traverse(rightShift.RightOperand);
      if (needsParen) this.sourceEmitterOutput.Write(")");

      this.currentPrecedence = savedCurrentPrecedence;
    }

    public override void TraverseChildren(IRuntimeArgumentHandleExpression runtimeArgumentHandleExpression) {
      base.TraverseChildren(runtimeArgumentHandleExpression);
    }

    public override void TraverseChildren(ISecurityAttribute securityAttribute) {
      base.TraverseChildren(securityAttribute);
    }

    public override void TraverseChildren(ISizeOf sizeOf) {
      this.sourceEmitterOutput.Write("sizeOf(");
      this.PrintTypeReference(sizeOf.TypeToSize);
      this.sourceEmitterOutput.Write(")");
    }

    public override void TraverseChildren(ISourceMethodBody methodBody) {
      base.TraverseChildren(methodBody);
    }

    public override void TraverseChildren(IStackArrayCreate stackArrayCreate) {
      base.TraverseChildren(stackArrayCreate);
    }

    public override void TraverseChildren(ISubtraction subtraction) {
      if (subtraction.LeftOperand is ITargetExpression && ExpressionHelper.IsIntegralOne(subtraction.RightOperand)) {
        if (subtraction.ResultIsUnmodifiedLeftOperand) {
          this.Traverse(subtraction.LeftOperand);
          this.sourceEmitterOutput.Write("--");
        } else {
          this.sourceEmitterOutput.Write("--");
          this.Traverse(subtraction.LeftOperand);
        }
        return;
      }

      var needsParen = LowerPrecedenceThanParentExpression(subtraction);
      var savedCurrentPrecedence = this.currentPrecedence;
      this.currentPrecedence = this.Precedence(subtraction);

      if (needsParen) this.sourceEmitterOutput.Write("(");
      this.Traverse(subtraction.LeftOperand);
      if (subtraction.LeftOperand is ITargetExpression)
        this.sourceEmitterOutput.Write(" -= ");
      else
        this.sourceEmitterOutput.Write(" - ");
      this.Traverse(subtraction.RightOperand);
      if (needsParen) this.sourceEmitterOutput.Write(")");

      this.currentPrecedence = savedCurrentPrecedence;
    }

    public override void TraverseChildren(ITargetExpression targetExpression) {
      IArrayIndexer/*?*/ indexer = targetExpression.Definition as IArrayIndexer;
      if (indexer != null) {
        this.Traverse(indexer);
        return;
      }
      IAddressDereference/*?*/ deref = targetExpression.Definition as IAddressDereference;
      if (deref != null) {
        IAddressOf/*?*/ addressOf = deref.Address as IAddressOf;
        if (addressOf != null) {
          this.Traverse(addressOf.Expression);
          return;
        }
        if (targetExpression.Instance != null) {
          this.Traverse(targetExpression.Instance);
          this.sourceEmitterOutput.Write("->");
        } else if (deref.Address.Type is IPointerTypeReference || deref.Address.Type.TypeCode == PrimitiveTypeCode.IntPtr)
          this.sourceEmitterOutput.Write("*");
        this.Traverse(deref.Address);
        return;
      } else {
        if (targetExpression.Instance != null) {
          IAddressOf/*?*/ addressOf = targetExpression.Instance as IAddressOf;
          if (addressOf != null)
            this.Traverse(addressOf.Expression);
          else
            this.Traverse(targetExpression.Instance);
          this.sourceEmitterOutput.Write(".");
        }
      }
      ILocalDefinition/*?*/ local = targetExpression.Definition as ILocalDefinition;
      if (local != null)
        this.PrintLocalName(local);
      else {
        IFieldReference/*?*/ field = targetExpression.Definition as IFieldReference;
        if (field != null) {
          if (field.IsStatic) {
            this.PrintTypeReference(field.ContainingType);
            this.sourceEmitterOutput.Write(".");
          }
          this.sourceEmitterOutput.Write(field.Name.Value);
        } else {
          INamedEntity/*?*/ ne = targetExpression.Definition as INamedEntity;
          if (ne != null)
            this.sourceEmitterOutput.Write(ne.Name.Value);
        }
      }
    }

    public virtual void PrintLocalName(ILocalDefinition local) {
      Contract.Requires(local != null);

      this.sourceEmitterOutput.Write(local.Name.Value);
    }

    public override void TraverseChildren(IThisReference thisReference) {
      this.PrintToken(CSharpToken.This);
    }

    public override void TraverseChildren(ITypeDefinitionMember typeMember) {
      base.TraverseChildren(typeMember);
    }

    public override void TraverseChildren(ITokenOf tokenOf) {
      this.sourceEmitterOutput.Write("tokenof(");
      base.TraverseChildren(tokenOf);
      this.sourceEmitterOutput.Write(")");
    }

    public override void TraverseChildren(ITypeOf typeOf) {
      this.sourceEmitterOutput.Write("typeof(");
      this.PrintTypeReference(typeOf.TypeToGet);
      this.sourceEmitterOutput.Write(")");
    }

    public override void TraverseChildren(ITypeReference typeReference) {
      this.PrintTypeReference(typeReference);
    }

    public override void TraverseChildren(IUnaryNegation unaryNegation) {
      base.TraverseChildren(unaryNegation);
    }

    public override void TraverseChildren(IUnaryPlus unaryPlus) {
      base.TraverseChildren(unaryPlus);
    }

    public override void TraverseChildren(IUnitNamespaceReference unitNamespaceReference) {
      base.TraverseChildren(unitNamespaceReference);
    }

    public override void TraverseChildren(IVectorLength vectorLength) {
      this.Traverse(vectorLength.Vector);
      this.sourceEmitterOutput.Write(".Length");
    }

  }
}
