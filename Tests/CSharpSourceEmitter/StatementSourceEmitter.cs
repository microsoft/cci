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

namespace CSharpSourceEmitter {
  public partial class SourceEmitter : BaseCodeTraverser, ICSharpSourceEmitter {

    public override void Visit(IBlockStatement block) {
      PrintToken(CSharpToken.LeftCurly);
      base.Visit(block);
      PrintToken(CSharpToken.RightCurly);
    }

    public override void Visit(IAssertStatement assertStatement) {
      this.PrintToken(CSharpToken.Indent);
      sourceEmitterOutput.Write("CodeContract.Assert(");
      this.Visit(assertStatement.Condition);
      sourceEmitterOutput.WriteLine(");");
    }

    public override void Visit(IAssumeStatement assumeStatement) {
      this.PrintToken(CSharpToken.Indent);
      sourceEmitterOutput.Write("CodeContract.Assume(");
      this.Visit(assumeStatement.Condition);
      sourceEmitterOutput.WriteLine(");");
    }

    public override void Visit(IBreakStatement breakStatement) {
      this.PrintToken(CSharpToken.Indent);
      sourceEmitterOutput.WriteLine("break;");
    }

    public override void Visit(IConditionalStatement conditionalStatement) {
      sourceEmitterOutput.Write("if (", true);
      this.Visit(conditionalStatement.Condition);
      sourceEmitterOutput.Write(")");
      if (conditionalStatement.TrueBranch is IBlockStatement)
        this.Visit(conditionalStatement.TrueBranch);
      else {
        PrintToken(CSharpToken.NewLine);
        sourceEmitterOutput.IncreaseIndent();
        this.Visit(conditionalStatement.TrueBranch);
        sourceEmitterOutput.DecreaseIndent();
      }
      if (!(conditionalStatement.FalseBranch is IEmptyStatement)) {
        this.sourceEmitterOutput.Write("else", true);
        this.Visit(conditionalStatement.FalseBranch);
      }
    }

    public override void Visit(IContinueStatement continueStatement) {
      sourceEmitterOutput.WriteLine("continue;");
    }

    public override void Visit(ICatchClause catchClause) {
      base.Visit(catchClause);
    }

    public override void Visit(IDebuggerBreakStatement debuggerBreakStatement) {
      this.PrintToken(CSharpToken.Indent);
      sourceEmitterOutput.WriteLine("Debugger.Break();");
    }

    public override void Visit(IDoUntilStatement doUntilStatement) {
      base.Visit(doUntilStatement);
    }

    public override void Visit(IEmptyStatement emptyStatement) {
      base.Visit(emptyStatement);
    }

    public override void Visit(IExpressionStatement expressionStatement) {
      this.PrintToken(CSharpToken.Indent);
      this.Visit(expressionStatement.Expression);
      this.PrintToken(CSharpToken.Semicolon);
    }

    public override void Visit(IFieldReference fieldReference) {
      this.sourceEmitterOutput.Write(MemberHelper.GetMemberSignature(fieldReference, NameFormattingOptions.None));
    }

    public override void Visit(IFileReference fileReference) {
      base.Visit(fileReference);
    }

    public override void Visit(IForEachStatement forEachStatement) {
      base.Visit(forEachStatement);
    }

    public override void Visit(IForStatement forStatement) {
      base.Visit(forStatement);
    }

    public override void Visit(IFunctionPointerTypeReference functionPointerTypeReference) {
      base.Visit(functionPointerTypeReference);
    }

    public override void Visit(IGenericMethodInstanceReference genericMethodInstanceReference) {
      base.Visit(genericMethodInstanceReference);
    }

    public override void Visit(IGenericMethodParameterReference genericMethodParameterReference) {
      base.Visit(genericMethodParameterReference);
    }

    public override void Visit(IGenericParameter genericParameter) {
      base.Visit(genericParameter);
    }

    public override void Visit(IGenericTypeInstanceReference genericTypeInstanceReference) {
      base.Visit(genericTypeInstanceReference);
    }

    public override void Visit(IGenericTypeParameterReference genericTypeParameterReference) {
      base.Visit(genericTypeParameterReference);
    }

    public override void Visit(IGotoStatement gotoStatement) {
      this.sourceEmitterOutput.Write("goto ", true);
      this.sourceEmitterOutput.Write(gotoStatement.TargetStatement.Label.Value);
      this.sourceEmitterOutput.WriteLine(";");
    }

    public override void Visit(IGotoSwitchCaseStatement gotoSwitchCaseStatement) {
      base.Visit(gotoSwitchCaseStatement);
    }

    public override void Visit(ILabeledStatement labeledStatement) {
      this.sourceEmitterOutput.DecreaseIndent();
      this.sourceEmitterOutput.Write(labeledStatement.Label.Value, true);
      this.sourceEmitterOutput.WriteLine(":");
      this.sourceEmitterOutput.IncreaseIndent();
      this.Visit(labeledStatement.Statement);
    }

    public override void Visit(ILocalDefinition localDefinition) {
      base.Visit(localDefinition);
    }

    public override void Visit(ILocalDeclarationStatement localDeclarationStatement) {
      string type = TypeHelper.GetTypeName(localDeclarationStatement.LocalVariable.Type, NameFormattingOptions.ContractNullable|NameFormattingOptions.UseTypeKeywords);
      this.sourceEmitterOutput.Write(type, true);
      this.sourceEmitterOutput.Write(" ");
      this.PrintLocalName(localDeclarationStatement.LocalVariable);
      if (localDeclarationStatement.InitialValue != null) {
        this.sourceEmitterOutput.Write(" = ");
        this.Visit(localDeclarationStatement.InitialValue);
      }
      this.sourceEmitterOutput.WriteLine(";");
    }

    public override void Visit(ILockStatement lockStatement) {
      base.Visit(lockStatement);
    }

    public override void Visit(IResourceUseStatement resourceUseStatement) {
      base.Visit(resourceUseStatement);
    }

    public override void Visit(IRethrowStatement rethrowStatement) {
      this.sourceEmitterOutput.WriteLine("throw;", true);
    }

    public override void Visit(IReturnStatement returnStatement) {
      this.PrintToken(CSharpToken.Indent);
      this.PrintToken(CSharpToken.Return);
      if (returnStatement.Expression != null) {
        this.PrintToken(CSharpToken.Space);
        this.Visit(returnStatement.Expression);
      }
      this.PrintToken(CSharpToken.Semicolon);
      this.PrintToken(CSharpToken.NewLine);
    }

    public override void Visit(IStatement statement) {
      base.Visit(statement);
    }

    public override void Visit(ISwitchCase switchCase) {
      if (switchCase.IsDefault)
        this.sourceEmitterOutput.WriteLine("default:", true);
      else {
        this.sourceEmitterOutput.Write("case ", true);
        this.Visit(switchCase.Expression);
        this.sourceEmitterOutput.WriteLine(":");
      }
      this.sourceEmitterOutput.IncreaseIndent();
      this.Visit(switchCase.Body);
      this.sourceEmitterOutput.DecreaseIndent();
    }

    public override void Visit(ISwitchStatement switchStatement) {
      this.sourceEmitterOutput.Write("switch(", true);
      this.Visit(switchStatement.Expression);
      this.sourceEmitterOutput.WriteLine("){");
      this.sourceEmitterOutput.IncreaseIndent();
      this.Visit(switchStatement.Cases);
      this.sourceEmitterOutput.DecreaseIndent();
      
      this.sourceEmitterOutput.WriteLine("}", true);
    }

    public override void Visit(IThrowStatement throwStatement) {
      this.PrintToken(CSharpToken.Indent);
      this.PrintToken(CSharpToken.Throw);
      if (throwStatement.Exception != null) {
        this.PrintToken(CSharpToken.Space);
        this.Visit(throwStatement.Exception);
      }
      this.PrintToken(CSharpToken.Semicolon);
    }

    public override void Visit(ITryCatchFinallyStatement tryCatchFilterFinallyStatement) {
      this.PrintToken(CSharpToken.Indent);
      this.PrintToken(CSharpToken.Try);
      this.Visit(tryCatchFilterFinallyStatement.TryBody);
      foreach (ICatchClause clause in tryCatchFilterFinallyStatement.CatchClauses) {
        this.sourceEmitterOutput.Write("catch", true);
        if (clause.ExceptionType != Dummy.TypeReference) {
          this.sourceEmitterOutput.Write("(");
          this.PrintTypeReference(clause.ExceptionType);
          this.sourceEmitterOutput.Write(" ");
          this.PrintLocalName(clause.ExceptionContainer);
          this.sourceEmitterOutput.Write(")");
        }
        this.Visit(clause.Body);
      }
      if (tryCatchFilterFinallyStatement.FinallyBody != null) {
        this.sourceEmitterOutput.Write("finally", true);
        this.Visit(tryCatchFilterFinallyStatement.FinallyBody);
      }
    }

    public override void Visit(IWhileDoStatement whileDoStatement) {
      base.Visit(whileDoStatement);
    }

    public override void Visit(IWin32Resource win32Resource) {
      base.Visit(win32Resource);
    }

    public override void Visit(IYieldBreakStatement yieldBreakStatement) {
      this.PrintToken(CSharpToken.Indent);
      this.PrintToken(CSharpToken.YieldBreak);
      this.PrintToken(CSharpToken.Semicolon);
      this.PrintToken(CSharpToken.NewLine);
    }

    public override void Visit(IYieldReturnStatement yieldReturnStatement) {
      this.PrintToken(CSharpToken.Indent);
      this.PrintToken(CSharpToken.YieldReturn);
      if (yieldReturnStatement.Expression != null) {
        this.PrintToken(CSharpToken.Space);
        this.Visit(yieldReturnStatement.Expression);
      }
      this.PrintToken(CSharpToken.Semicolon);
      this.PrintToken(CSharpToken.NewLine);
    }

    public override void VisitMethodReturnAttributes(IEnumerable<ICustomAttribute> customAttributes) {
      base.VisitMethodReturnAttributes(customAttributes);
    }

  }
}