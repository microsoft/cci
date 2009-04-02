//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using Microsoft.Cci;
using Microsoft.Cci.Ast;
using System.Collections.Generic;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci.SmallBasic {
  internal class Parser {
    INameTable nameTable;
    List<IErrorMessage> scannerAndParserErrors;
    Token currentToken;
    readonly SmallBasicCompilerOptions options;
    Scanner scanner;

    RootClassDeclaration/*?*/ rootClass;

    internal Parser(INameTable nameTable, ISourceLocation sourceLocation, SmallBasicCompilerOptions options, List<IErrorMessage> scannerAndParserErrors) {
      this.nameTable = nameTable;
      this.scannerAndParserErrors = scannerAndParserErrors;
      this.options = options;
      this.scanner = new Scanner(sourceLocation);
    }

    internal void ParseStatements(List<Statement> statements, RootClassDeclaration rootClass) {
      this.rootClass = rootClass;
      this.GetNextToken();
      TokenSet statementStartOrEof = Parser.StatementStart|Parser.EndOfFile;
      this.ParseStatements(statements, statementStartOrEof);
    }

    private void ParseStatements(List<Statement> statements, TokenSet followers)
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      while (Parser.StatementStart[this.currentToken]) {
        switch (this.currentToken) {
          case Token.Do: statements.Add(this.ParseDo(followers)); break;
          case Token.For: statements.Add(this.ParseFor(followers)); break;
          case Token.Gosub: statements.Add(this.ParseGosub(followers)); break;
          case Token.Goto: statements.Add(this.ParseGoto(followers)); break;
          case Token.Identifier: statements.Add(this.ParseAssignmentOrCallOrLabel(followers)); break;
          case Token.If: statements.Add(this.ParseIf(followers)); break;
          case Token.Return: statements.Add(this.ParseReturn(followers)); break;
          case Token.EndOfLine: this.GetNextToken(); break;
          default: this.SkipTo(followers); break;
        }
      }
    }

    private BlockStatement ParseStatementBlock(TokenSet followers) {
      SourceLocationBuilder slb = new SourceLocationBuilder(this.scanner.CurrentSourceLocation);
      List<Statement> statements = new List<Statement>();
      this.ParseStatements(statements, followers);
      slb.UpdateToSpan(this.scanner.CurrentSourceLocation);
      return new BlockStatement(statements, slb);
    }

    private Statement ParseDo(TokenSet followers)
      //^ requires this.currentToken == Token.Do;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      SourceLocationBuilder slb = new SourceLocationBuilder(this.scanner.CurrentSourceLocation);
      this.GetNextToken();
      this.Skip(Token.While);
      Expression condition = this.ParseExpression(followers|Token.EndOfLine);
      this.Skip(Token.EndOfLine);
      BlockStatement body = this.ParseStatementBlock(followers|Token.Loop);
      slb.UpdateToSpan(this.scanner.CurrentSourceLocation);
      WhileDoStatement result = new WhileDoStatement(condition, body, slb);
      this.SkipClosingKeyword(Token.Loop, followers);
      return result;
    }


    private Statement ParseFor(TokenSet followers)
      //^ requires this.currentToken == Token.For;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      SourceLocationBuilder slb = new SourceLocationBuilder(this.scanner.CurrentSourceLocation);
      this.GetNextToken();
      if (this.currentToken == Token.Each) return this.ParseForEach(slb, followers);
      SmallBasicSimpleName variableName = this.ParseSimpleName(followers|Parser.ExpressionStart|Token.Equals|Token.To|Token.Step|Token.EndOfLine);
      variableName.rootClass = this.rootClass;
      this.Skip(Token.Equals);
      Expression startValue = this.ParseExpression(followers|Parser.ExpressionStart|Token.To|Token.Step|Token.EndOfLine);
      variableName.expressionToInferTargetTypeFrom = startValue;
      SourceLocationBuilder rslb = new SourceLocationBuilder(startValue.SourceLocation);
      this.Skip(Token.To);
      Expression endValue = this.ParseExpression(followers|Parser.ExpressionStart|Token.Step|Token.EndOfLine);
      rslb.UpdateToSpan(endValue.SourceLocation);
      Expression/*?*/ stepValue = null;
      if (this.currentToken == Token.Step) {
        this.GetNextToken();
        stepValue = this.ParseExpression(followers|Token.EndOfLine);
      }
      this.Skip(Token.EndOfLine);
      BlockStatement body = this.ParseStatementBlock(followers|Token.Next);
      slb.UpdateToSpan(body.SourceLocation);
      ForRangeStatement result = new ForRangeStatement(null, variableName, new Range(startValue, endValue, rslb), stepValue, body, slb); //TODO Spec#: Range should not be ambiguous
      this.SkipClosingKeyword(Token.Next, followers);
      return result;
    }

    private Statement ParseForEach(SourceLocationBuilder slb, TokenSet followers)
      //^ requires this.currentToken == Token.Each;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      this.GetNextToken();
      NameDeclaration variableName = this.ParseNameDeclaration();
      this.Skip(Token.In);
      Expression collection = this.ParseExpression(followers|Token.EndOfLine);
      this.Skip(Token.EndOfLine);
      BlockStatement body = this.ParseStatementBlock(followers|Token.Next);
      slb.UpdateToSpan(body.SourceLocation);
      TypeExpression variableType = new NamedTypeExpression(new SimpleName(this.nameTable.GetNameFor("var"), SourceDummy.SourceLocation, false));
      ForEachStatement result = new ForEachStatement(variableType, variableName, collection, body, slb);
      this.SkipClosingKeyword(Token.Next, followers);
      return result;
    }

    private NameDeclaration ParseNameDeclaration() {
      IName name;
      ISourceLocation sourceLocation = this.scanner.CurrentSourceLocation;
      if (this.currentToken == Token.Identifier) {
        name = this.GetNameFor(this.scanner.GetTokenSource());
        this.GetNextToken();
      } else {
        name = Dummy.Name;
      }
      return new NameDeclaration(name, sourceLocation);
    }

    private Statement ParseGosub(TokenSet followers)
      //^ requires this.currentToken == Token.Gosub;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      SourceLocationBuilder slb = new SourceLocationBuilder(this.scanner.CurrentSourceLocation);
      this.GetNextToken();
      //^ assume this.rootClass != null;
      GosubStatement result = new GosubStatement(this.ParseSimpleName(followers|Token.EndOfLine), slb, this.rootClass);
      slb.UpdateToSpan(result.TargetLabel.SourceLocation);
      this.SkipOverTo(Token.EndOfLine, followers);
      return result;
    }

    private Statement ParseGoto(TokenSet followers)
      //^ requires this.currentToken == Token.Goto;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      SourceLocationBuilder slb = new SourceLocationBuilder(this.scanner.CurrentSourceLocation);
      this.GetNextToken();
      SimpleName targetLabel = this.ParseSimpleName(followers|Token.EndOfLine|Token.If);
      Statement result = new GotoStatement(targetLabel, slb);
      slb.UpdateToSpan(targetLabel.SourceLocation);
      if (this.currentToken == Token.If) {
        this.GetNextToken();
        slb = new SourceLocationBuilder(slb.GetSourceLocation());
        Expression condition = this.ParseExpression(followers|Token.EndOfLine);
        slb.UpdateToSpan(condition.SourceLocation);
        result = new ConditionalStatement(condition, result, new EmptyStatement(false, this.scanner.CurrentSourceLocation), slb);
      }
      this.SkipOverTo(Token.EndOfLine, followers);
      return result;
    }

    private Statement ParseIf(TokenSet followers)
      //^ requires this.currentToken == Token.If;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      SourceLocationBuilder slb = new SourceLocationBuilder(this.scanner.CurrentSourceLocation);
      this.GetNextToken();
      Expression ifCondition = this.ParseExpression(followers|Token.Else|Token.EndIf|Parser.StatementStart|Token.EndOfLine);
      this.Skip(Token.EndOfLine);
      BlockStatement ifTrue = this.ParseStatementBlock(followers|Token.Else|Token.EndIf);
      Statement ifFalse;
      if (this.currentToken == Token.Else) {
        this.GetNextToken();
        this.Skip(Token.EndOfLine);
        ifFalse = this.ParseStatementBlock(followers|Token.EndIf);
      } else {
        ifFalse = new EmptyStatement(false, this.scanner.CurrentSourceLocation);
      }
      slb.UpdateToSpan(this.scanner.CurrentSourceLocation);
      Statement result = new ConditionalStatement(ifCondition, ifTrue, ifFalse, slb);
      this.SkipClosingKeyword(Token.EndIf, followers);
      return result;
    }

    private Statement ParseReturn(TokenSet followers)
      //^ requires this.currentToken == Token.Return;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      ISourceLocation sourceLocation = this.scanner.CurrentSourceLocation;
      this.GetNextToken();
      Statement result = new ReturnStatement(null, sourceLocation);
      this.SkipOverTo(Token.EndOfLine, followers);
      return result;
    }

    private Statement ParseAssignmentOrCallOrLabel(TokenSet followers)
      //^ requires this.currentToken == Token.Identifier;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      SimpleName rootName = this.ParseSimpleName(followers|Token.Colon|Token.Dot|Token.Equals|Token.LeftBracket|Token.LeftParens|Token.EndOfLine);
      switch (this.currentToken) {
        case Token.Colon:
          return this.ParseLabel(rootName, followers);
        case Token.Dot: 
        case Token.Equals:
        case Token.LeftBracket:
        case Token.LeftParens:
          return this.ParseAssignmentOrCall(rootName, followers);
        case Token.Identifier:
        //TODO: error
        default: 
          return new EmptyStatement(false, rootName.SourceLocation);
      }
    }

    private LabeledStatement ParseLabel(SimpleName rootName, TokenSet followers)
      //^ requires this.currentToken == Token.Colon;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      NameDeclaration labelName = new NameDeclaration(rootName.Name, rootName.SourceLocation);
      //^ assume this.rootClass != null;
      this.rootClass.AddLabel(rootName);
      SourceLocationBuilder slb = new SourceLocationBuilder(rootName.SourceLocation);
      slb.UpdateToSpan(this.scanner.CurrentSourceLocation);
      this.GetNextToken();
      LabeledStatement result = new LabeledStatement(labelName, new EmptyStatement(false, SourceDummy.SourceLocation), slb);
      this.SkipOverTo(Token.EndOfLine, followers);
      return result;
    }

    private Statement ParseAssignmentOrCall(SimpleName rootName, TokenSet followers)
      //^ requires this.currentToken == Token.Dot || this.currentToken == Token.Equals || this.currentToken == Token.LeftBracket || this.currentToken == Token.LeftParens;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      Expression expression = rootName;
      SourceLocationBuilder slb = new SourceLocationBuilder(rootName.SourceLocation);
      while (true) {
        switch (this.currentToken) {
          case Token.Dot: {
              this.GetNextToken();
              SimpleName simpleName = this.ParseSimpleName(followers|Token.Dot|Token.Equals|Token.LeftBracket|Token.LeftParens|Token.EndOfLine);
              slb.UpdateToSpan(simpleName.SourceLocation);
              expression = new QualifiedName(expression, simpleName, slb);
              continue;
            }
          case Token.LeftBracket: {
              this.GetNextToken();
              IEnumerable<Expression> indices = this.ParseExpressions(slb, Token.RightBracket, followers|Token.Dot|Token.Equals|Token.LeftBracket|Token.LeftParens|Token.EndOfLine);
              expression = new Indexer(expression, indices, slb);
              continue;
            }
          case Token.LeftParens: {
              this.GetNextToken();
              IEnumerable<Expression> indices = this.ParseExpressions(slb, Token.RightParens, followers|Token.Dot|Token.Equals|Token.LeftBracket|Token.LeftParens|Token.EndOfLine);
              expression = new MethodCall(expression, indices, slb); //TODO: change this to VBMethodCall
              continue;
            }
        }
        break;
      }
      if (this.currentToken == Token.Equals) {
        this.GetNextToken();
        Expression source = this.ParseExpression(followers|Token.EndOfLine);
        SmallBasicSimpleName/*?*/ target = expression as SmallBasicSimpleName;
        if (target != null) target.expressionToInferTargetTypeFrom = source;
        slb.UpdateToSpan(source.SourceLocation);
        expression = new SmallBasicAssignment(new TargetExpression(expression), source, slb);
      }
      this.SkipOverTo(Token.EndOfLine, followers);
      return new ExpressionStatement(expression);
    }

    private Expression ParseExpression(TokenSet followers)
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      if (!Parser.ExpressionStart[this.currentToken]) {
        //TODO: error
        return new DummyExpression(this.scanner.CurrentSourceLocation);
      }
      Expression result = this.ParseOrExpression(followers);
      this.SkipTo(followers);
      return result;
    }

    private Expression ParseOrExpression(TokenSet followers)
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      TokenSet followerOrOr = followers|Token.Or;
      Expression result = this.ParseAndExpression(followerOrOr);
      while (this.currentToken == Token.Or) {
        SourceLocationBuilder slb = new SourceLocationBuilder(result.SourceLocation);
        this.GetNextToken();
        Expression operand2 = this.ParseAndExpression(followerOrOr);
        slb.UpdateToSpan(operand2.SourceLocation);
        result = new LogicalOr(result, operand2, slb);
      }
      //^ assume followers[this.currentToken] || this.currentToken == Token.EndOfFile;
      return result;
    }

    private Expression ParseAndExpression(TokenSet followers)
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      TokenSet followerOrAnd = followers|Token.And;
      Expression result = this.ParseRelationalExpression(followerOrAnd);
      while (this.currentToken == Token.And) {
        SourceLocationBuilder slb = new SourceLocationBuilder(result.SourceLocation);
        this.GetNextToken();
        Expression operand2 = this.ParseRelationalExpression(followerOrAnd);
        slb.UpdateToSpan(operand2.SourceLocation);
        result = new LogicalAnd(result, operand2, slb);
      }
      //^ assume followers[this.currentToken] || this.currentToken == Token.EndOfFile;
      return result;
    }

    private Expression ParseRelationalExpression(TokenSet followers) {
      TokenSet followerOrRelational = followers|Parser.RelationalOperators;
      Expression result = this.ParseAdditiveExpression(followerOrRelational);
      while (Parser.RelationalOperators[this.currentToken]) {
        SourceLocationBuilder slb = new SourceLocationBuilder(result.SourceLocation);
        Token operatorToken = this.currentToken;
        this.GetNextToken();
        Expression operand2 = this.ParseAdditiveExpression(followerOrRelational);
        slb.UpdateToSpan(operand2.SourceLocation);
        switch (operatorToken){
          case Token.Equals: result = new Equality(result, operand2, slb); break;
          case Token.GreaterThan: result = new GreaterThan(result, operand2, slb); break;
          case Token.GreaterThanEqualTo: result = new GreaterThanOrEqual(result, operand2, slb); break;
          case Token.LessThan: result = new LessThan(result, operand2, slb); break;
          case Token.LessThanEqualTo: result = new LessThanOrEqual(result, operand2, slb); break;
          case Token.NotEqualTo: result = new NotEquality(result, operand2, slb); break;
        }
      }
      //^ assume followers[this.currentToken] || this.currentToken == Token.EndOfFile;
      return result;
    }

    private Expression ParseAdditiveExpression(TokenSet followers) {
      TokenSet followerOrAdditive = followers|Token.Addition|Token.Subtraction;
      Expression result = this.ParseMultiplicativeExpression(followerOrAdditive);
      while (this.currentToken == Token.Addition || this.currentToken == Token.Subtraction) {
        SourceLocationBuilder slb = new SourceLocationBuilder(result.SourceLocation);
        Token operatorToken = this.currentToken;
        this.GetNextToken();
        Expression operand2 = this.ParseMultiplicativeExpression(followerOrAdditive);
        slb.UpdateToSpan(operand2.SourceLocation);
        switch (operatorToken) {
          case Token.Addition: result = new Addition(result, operand2, slb); break;
          case Token.Subtraction: result = new Subtraction(result, operand2, slb); break;
        }
      }
      //^ assume followers[this.currentToken] || this.currentToken == Token.EndOfFile;
      return result;
    }

    private Expression ParseMultiplicativeExpression(TokenSet followers) {
      TokenSet followerOrMultiplicative = followers|Token.Division|Token.Multiplication;
      Expression result = this.ParseRaiseExpression(followerOrMultiplicative);
      while (this.currentToken == Token.Division || this.currentToken == Token.Multiplication) {
        SourceLocationBuilder slb = new SourceLocationBuilder(result.SourceLocation);
        Token operatorToken = this.currentToken;
        this.GetNextToken();
        Expression operand2 = this.ParseRaiseExpression(followerOrMultiplicative);
        slb.UpdateToSpan(operand2.SourceLocation);
        switch (operatorToken) {
          case Token.Division: result = new Division(result, operand2, slb); break;
          case Token.Multiplication: result = new Multiplication(result, operand2, slb); break;
        }
      }
      //^ assume followers[this.currentToken] || this.currentToken == Token.EndOfFile;
      return result;
    }

    private Expression ParseRaiseExpression(TokenSet followers)
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      TokenSet followerOrRaise = followers|Token.Raise;
      Expression result = this.ParseUnaryExpression(followerOrRaise);
      if (this.currentToken == Token.Raise) {
        SourceLocationBuilder slb = new SourceLocationBuilder(result.SourceLocation);
        this.GetNextToken();
        Expression operand2 = this.ParseRaiseExpression(followerOrRaise);
        slb.UpdateToSpan(operand2.SourceLocation);
        result = new Exponentiation(result, operand2, slb);
      }
      //^ assume followers[this.currentToken] || this.currentToken == Token.EndOfFile;
      return result;
    }

    private Expression ParseUnaryExpression(TokenSet followers) {
      Expression result;
      switch (this.currentToken) {
        case Token.Addition: {
            SourceLocationBuilder slb = new SourceLocationBuilder(this.scanner.CurrentSourceLocation);
            this.GetNextToken();
            result = new UnaryPlus(this.ParseSimpleExpression(followers), slb);
            break;
          }

        case Token.Not: {
            SourceLocationBuilder slb = new SourceLocationBuilder(this.scanner.CurrentSourceLocation);
            this.GetNextToken();
            result = new LogicalNot(this.ParseRelationalExpression(followers), slb);
            break;
          }

        case Token.Subtraction: {
            SourceLocationBuilder slb = new SourceLocationBuilder(this.scanner.CurrentSourceLocation);
            this.GetNextToken();
            result = new UnaryPlus(this.ParseSimpleExpression(followers), slb);
            break;
          }

        default:
          result = this.ParseSimpleExpression(followers);
          break;
      }
      return result;
    }

    private Expression ParseSimpleExpression(TokenSet followers) {
      Expression result;
      TokenSet followersOrDotOrLeftParensOrLeftBracket = followers|Token.Dot|Token.LeftParens|Token.LeftBracket;
      switch (this.currentToken) {
        case Token.False:
          result = new CompileTimeConstant(false, this.scanner.CurrentSourceLocation);
          this.GetNextToken();
          break;
        case Token.Identifier:
          result = this.ParseSimpleName(followersOrDotOrLeftParensOrLeftBracket);
          break;
        case Token.NumericLiteral:
          result = new CompileTimeConstant(decimal.Parse(this.scanner.GetTokenSource(), System.Globalization.CultureInfo.InvariantCulture), this.scanner.CurrentSourceLocation);
          this.GetNextToken();
          break;
        case Token.StringLiteral:
          result = new CompileTimeConstant(this.scanner.GetTokenSource().Trim('"'), this.scanner.CurrentSourceLocation);
          this.GetNextToken();
          break;
        case Token.True:
          result = new CompileTimeConstant(true, this.scanner.CurrentSourceLocation);
          this.GetNextToken();
          break;
        case Token.LeftParens:
          result = this.ParseParenthesizedExpression(followersOrDotOrLeftParensOrLeftBracket);
          break;
        default:
          //TODO: error
          result = new DummyExpression(this.scanner.CurrentSourceLocation);
          break;
      }
      SourceLocationBuilder slb = new SourceLocationBuilder(result.SourceLocation);
      while (true) {
        switch (this.currentToken) {
          case Token.Dot: {
              this.GetNextToken();
              SmallBasicSimpleName simpleName = this.ParseSimpleName(followers|Token.Dot|Token.Equals|Token.LeftBracket|Token.LeftParens|Token.EndOfLine);
              slb.UpdateToSpan(simpleName.SourceLocation);
              result = new QualifiedName(result, simpleName, slb);
              continue;
            }
          case Token.LeftBracket: {
              this.GetNextToken();
              IEnumerable<Expression> indices = this.ParseExpressions(slb, Token.RightBracket, followers|Token.Dot|Token.Equals|Token.LeftBracket|Token.LeftParens|Token.EndOfLine);
              result = new SmallBasicIndexer(result, indices, slb);
              continue;
            }
          case Token.LeftParens: {
              this.GetNextToken();
              IEnumerable<Expression> indices = this.ParseExpressions(slb, Token.RightParens, followers|Token.Dot|Token.Equals|Token.LeftBracket|Token.LeftParens|Token.EndOfLine);
              result = new MethodCall(result, indices, slb); //TODO: change this to VBMethodCall
              continue;
            }
        }
        break;
      }
      this.SkipTo(followers);
      return result;
    }

    private Expression ParseParenthesizedExpression(TokenSet followers)
      //^ requires this.currentToken == Token.LeftParens;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      SourceLocationBuilder sctx = new SourceLocationBuilder(this.scanner.CurrentSourceLocation);
      this.GetNextToken();
      Expression result = this.ParseExpression(followers|Token.RightParens|Token.EndOfLine);
      sctx.UpdateToSpan(this.scanner.CurrentSourceLocation);
      result = new Parenthesis(result, sctx);
      this.SkipOverTo(Token.RightParens, followers|Token.EndOfLine);
      return result;
    }

    private IEnumerable<Expression> ParseExpressions(SourceLocationBuilder slb, Token terminator, TokenSet followers) {
      List<Expression> result = new List<Expression>();
      while (Parser.ExpressionStart[this.currentToken]) {
        Expression expression = this.ParseExpression(followers|Parser.ExpressionStart|Token.Comma|terminator);
        result.Add(expression);
        if (this.currentToken != Token.Comma) break;
        this.GetNextToken();
      }
      slb.UpdateToSpan(this.scanner.CurrentSourceLocation);
      this.SkipOverTo(terminator, followers);
      result.TrimExcess();
      return result.AsReadOnly();
    }

    private SmallBasicSimpleName ParseSimpleName(TokenSet followers)
      //^ requires this.currentToken == Token.Identifier;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      ISourceLocation sourceLocation = this.scanner.CurrentSourceLocation;
      IName name = this.GetNameFor(sourceLocation.Source);
      SmallBasicSimpleName result = new SmallBasicSimpleName(name, sourceLocation);
      result.rootClass = this.rootClass;
      this.GetNextToken();
      //TODO: if current token is end of line, then give an error about unexpected end of line
      this.SkipTo(followers);
      return result;
    }

    private NameDeclaration GetNameDeclarationFor(string name, ISourceLocation sourceLocation) {
      IName iname = this.nameTable.GetNameFor(name);
      return new NameDeclaration(iname, sourceLocation);
    }

    private IName GetNameFor(string name) {
      return this.nameTable.GetNameFor(name);
    }

    private void GetNextToken()
      //^ requires this.currentToken != Token.EndOfFile;
    {
      TokenInfo tokInfo;
      do {
        if (!this.scanner.ScanNextToken(out tokInfo))
          this.currentToken = Token.EndOfFile;
        else
          this.currentToken = tokInfo.Token;
      } while (this.currentToken == Token.Comment);
    }


    private void HandleError(Error error, params string[] messageParameters) 
      // ^ modifies this.scannerAndParserErrors;
    {
      this.HandleError(this.scanner.CurrentSourceLocation, error, messageParameters);
    }

    private void HandleError(ISourceLocation errorLocation, Error error, params string[] messageParameters)
      // ^ modifies this.scannerAndParserErrors;
    {
      this.scannerAndParserErrors.Add(new SmallBasicErrorMessage(errorLocation, (long)error, error.ToString(), messageParameters));
    }

    private void SkipClosingKeyword(Token token, TokenSet followers)
      //^ requires token != Token.EndOfFile;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      if (this.currentToken == token) {
        this.GetNextToken();
        this.Skip(Token.EndOfLine);
      } else {
        this.HandleError(Error.UnexpectedToken, this.scanner.GetTokenSource());
      }
      this.SkipTo(followers, Error.None);
    }

    private void SkipOverTo(Token token, TokenSet followers)
      //^ requires token != Token.EndOfFile;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      this.Skip(token);
      this.SkipTo(followers, Error.None);
    }

    private void Skip(Token token) 
      //^ requires token != Token.EndOfFile;
    {
      if (this.currentToken == token)
        this.GetNextToken();
      else {
        if (token != Token.EndOfLine || this.currentToken != Token.EndOfFile)
          this.HandleError(Error.UnexpectedToken, this.scanner.GetTokenSource()); 
      }
    }

    private void SkipTo(TokenSet followers)
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      if (followers[this.currentToken]) return;
      this.HandleError(Error.InvalidExprTerm, this.scanner.GetTokenSource());
      while (!followers[this.currentToken] && this.currentToken != Token.EndOfFile)
        this.GetNextToken();
    }

    private void SkipTo(TokenSet followers, Error error, params string[] messages)
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      if (error != Error.None)
        this.HandleError(error, messages);
      while (!followers[this.currentToken] && this.currentToken != Token.EndOfFile)
        this.GetNextToken();
    }

    //^ [Pure]
    private QualifiedName RootQualifiedNameFor(Token tok)
      ////^ requires tok == Token.Bool || tok == Token.Decimal || tok == Token.Sbyte ||
      ////^   tok == Token.Byte || tok == Token.Short || tok == Token.Ushort ||
      ////^   tok == Token.Int || tok == Token.Uint || tok == Token.Long ||
      ////^   tok == Token.Ulong || tok == Token.Char || tok == Token.Float ||
      ////^   tok == Token.Double || tok == Token.Object || tok == Token.String ||
      ////^   tok == Token.Void;
    {
      return this.RootQualifiedNameFor(tok, this.scanner.CurrentSourceLocation);
    }

    private QualifiedName RootQualifiedNameFor(Token tok, ISourceLocation sctx) 
      ////^ requires tok == Token.Bool || tok == Token.Decimal || tok == Token.Sbyte ||
      ////^   tok == Token.Byte || tok == Token.Short || tok == Token.Ushort ||
      ////^   tok == Token.Int || tok == Token.Uint || tok == Token.Long ||
      ////^   tok == Token.Ulong || tok == Token.Char || tok == Token.Float ||
      ////^   tok == Token.Double || tok == Token.Object || tok == Token.String ||
      ////^   tok == Token.Void;
    {
      RootNamespaceExpression rootNs = new RootNamespaceExpression(sctx);
      AliasQualifiedName systemNs = new AliasQualifiedName(rootNs, this.GetSimpleNameFor("System"), sctx);
      switch (tok) {
        //case Token.Bool: return new QualifiedName(systemNs, this.GetSimpleNameFor("Boolean"), sctx);
        //case Token.Decimal: return new QualifiedName(systemNs, this.GetSimpleNameFor("Decimal"), sctx);
        //case Token.Sbyte: return new QualifiedName(systemNs, this.GetSimpleNameFor("SByte"), sctx);
        //case Token.Byte: return new QualifiedName(systemNs, this.GetSimpleNameFor("Byte"), sctx);
        //case Token.Short: return new QualifiedName(systemNs, this.GetSimpleNameFor("Int16"), sctx);
        //case Token.Ushort: return new QualifiedName(systemNs, this.GetSimpleNameFor("UInt16"), sctx);
        //case Token.Int: return new QualifiedName(systemNs, this.GetSimpleNameFor("Int32"), sctx);
        //case Token.Uint: return new QualifiedName(systemNs, this.GetSimpleNameFor("UInt32"), sctx);
        //case Token.Long: return new QualifiedName(systemNs, this.GetSimpleNameFor("Int64"), sctx);
        //case Token.Ulong: return new QualifiedName(systemNs, this.GetSimpleNameFor("UInt64"), sctx);
        //case Token.Char: return new QualifiedName(systemNs, this.GetSimpleNameFor("Char"), sctx);
        //case Token.Float: return new QualifiedName(systemNs, this.GetSimpleNameFor("Single"), sctx);
        //case Token.Double: return new QualifiedName(systemNs, this.GetSimpleNameFor("Double"), sctx);
        //case Token.Object: return new QualifiedName(systemNs, this.GetSimpleNameFor("Object"), sctx);
        //case Token.String: return new QualifiedName(systemNs, this.GetSimpleNameFor("String"), sctx);
        default:
          ////^ assert tok == Token.Void;
          return new QualifiedName(systemNs, this.GetSimpleNameFor("Void"), sctx);
      }
    }

    private SimpleName GetSimpleNameFor(string nameString) {
      IName name = this.GetNameFor(nameString);
      return new SimpleName(name, this.scanner.CurrentSourceLocation, false);
    }

    private static readonly TokenSet BinaryOperators;
    private static readonly TokenSet EndOfFile;
    private static readonly TokenSet ExpressionStart;
    private static readonly TokenSet RelationalOperators;
    private static readonly TokenSet StatementStart;
    
    static Parser(){
      BinaryOperators = new TokenSet();
      BinaryOperators |= Token.Addition;
      BinaryOperators |= Token.And;
      BinaryOperators |= Token.Division;
      BinaryOperators |= Token.Equals;
      BinaryOperators |= Token.GreaterThan;
      BinaryOperators |= Token.GreaterThanEqualTo;
      BinaryOperators |= Token.LessThan;
      BinaryOperators |= Token.LessThanEqualTo;
      BinaryOperators |= Token.Multiplication;
      BinaryOperators |= Token.NotEqualTo;
      BinaryOperators |= Token.Or;
      BinaryOperators |= Token.Raise;
      BinaryOperators |= Token.Subtraction;

      EndOfFile = new TokenSet();
      EndOfFile |= Token.EndOfFile;

      ExpressionStart = new TokenSet();
      ExpressionStart |= Token.Addition;
      ExpressionStart |= Token.False;
      ExpressionStart |= Token.Identifier;
      ExpressionStart |= Token.LeftParens;
      ExpressionStart |= Token.Not;
      ExpressionStart |= Token.NumericLiteral;
      ExpressionStart |= Token.StringLiteral;
      ExpressionStart |= Token.Subtraction;
      ExpressionStart |= Token.True;

      RelationalOperators = new TokenSet();
      RelationalOperators |= Token.Equals;
      RelationalOperators |= Token.GreaterThan;
      RelationalOperators |= Token.GreaterThanEqualTo;
      RelationalOperators |= Token.LessThan;
      RelationalOperators |= Token.LessThanEqualTo;
      RelationalOperators |= Token.NotEqualTo;

      StatementStart = new TokenSet();
      StatementStart |= Token.Do;
      StatementStart |= Token.For;
      StatementStart |= Token.Gosub;
      StatementStart |= Token.Goto;
      StatementStart |= Token.Identifier;
      StatementStart |= Token.If;
      StatementStart |= Token.Return;

    }

    private struct TokenSet {
      private ulong bits0, bits1, bits2, bits3;

      //^ [Pure]
      public static TokenSet operator |(TokenSet ts, Token t){
        TokenSet result = new TokenSet();
        int i = (int)t;
        if (i < 64){
          result.bits0 = ts.bits0 | (1ul << i);
          result.bits1 = ts.bits1;
          result.bits2 = ts.bits2;
          result.bits3 = ts.bits3;
        }else if (i < 128){
          result.bits0 = ts.bits0;
          result.bits1 = ts.bits1 | (1ul << (i-64));
          result.bits2 = ts.bits2;
          result.bits3 = ts.bits3;
        }else if (i < 192){
          result.bits0 = ts.bits0;
          result.bits1 = ts.bits1;
          result.bits2 = ts.bits2 | (1ul << (i-128));
          result.bits3 = ts.bits3;
        }else{
          result.bits0 = ts.bits0;
          result.bits1 = ts.bits1;
          result.bits2 = ts.bits2;
          result.bits3 = ts.bits3 | (1ul << (i-192));
        }
        return result;
      }

      //^ [Pure]
      public static TokenSet operator|(TokenSet ts1, TokenSet ts2) {
        TokenSet result = new TokenSet();
        result.bits0 = ts1.bits0 | ts2.bits0;
        result.bits1 = ts1.bits1 | ts2.bits1;
        result.bits2 = ts1.bits2 | ts2.bits2;
        result.bits3 = ts1.bits3 | ts2.bits3;
        return result;
      }

      internal bool this[Token t]{
        get {
          int i = (int)t;
          if (i < 64)
            return (this.bits0 & (1ul << i)) != 0;
          else if (i < 128)
            return (this.bits1 & (1ul << (i-64))) != 0;
          else if (i < 192)
            return (this.bits2 & (1ul << (i-128))) != 0;
          else
            return (this.bits3 & (1ul << (i-192))) != 0;
        }
      }

      static TokenSet(){
        //^ int i = (int)Token.EndOfFile;
        //^ assert 0 <= i && i <= 255;
      }
    }


  }
}
