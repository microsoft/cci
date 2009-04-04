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

namespace Microsoft.Cci.CSharp {
  internal class Parser {
    Compilation compilation;
    INameTable nameTable;
    List<IErrorMessage> scannerAndParserErrors;
    List<IErrorMessage> originalScannerAndParserErrors;
    Token currentToken;
    bool insideBlock;
    bool insideType;
    private Scanner scanner;

    internal Parser(Compilation compilation, ISourceLocation sourceLocation, List<IErrorMessage> scannerAndParserErrors) {
      this.compilation = compilation;
      this.nameTable = compilation.NameTable;
      this.scannerAndParserErrors = this.originalScannerAndParserErrors = scannerAndParserErrors;
      this.scanner = new Scanner(scannerAndParserErrors, sourceLocation, true);
      this.insideBlock = false;
      this.insideType = false;
    }

    private NameDeclaration GetNameDeclarationFor(string name, ISourceLocation sourceLocation) 
      //^ ensures result.Value == name;
    {
      IName iname = this.nameTable.GetNameFor(name);
      NameDeclaration result = new NameDeclaration(iname, sourceLocation);
      //^ assume result.Value == name;
      return result;
    }

    private IName GetNameFor(string name)
      //^ ensures result.Value == name;
    {
      return this.nameTable.GetNameFor(name);
    }

    private void GetNextToken()
      //^ requires this.currentToken != Token.EndOfFile;
    {
      this.currentToken = this.scanner.GetNextToken();
    }

    //^ [Pure]
    private Token PeekNextToken()
    {
      if (this.currentToken == Token.EndOfFile) return Token.EndOfFile;
      int position = this.scanner.CurrentDocumentPosition();
      //^ assume this.currentToken != Token.EndOfFile;
      this.GetNextToken();
      Token tk = this.currentToken;
      this.scanner.RestoreDocumentPosition(position);
      this.currentToken = Token.None;
      this.GetNextToken();
      return tk;
    }

    private void HandleError(Error error, params string[] messageParameters) 
      // ^ modifies this.scannerAndParserErrors;
      //^ ensures this.currentToken == old(this.currentToken);
    {
      this.HandleError(this.scanner.SourceLocationOfLastScannedToken, error, messageParameters);
    }

    private void HandleError(ISourceLocation errorLocation, Error error, params string[] messageParameters)
      // ^ modifies this.scannerAndParserErrors;
      //^ ensures this.currentToken == old(this.currentToken);
    {
      //^ Token oldToken = this.currentToken;
      if (this.originalScannerAndParserErrors == this.scannerAndParserErrors) {
      }
      this.scannerAndParserErrors.Add(new CSharpErrorMessage(errorLocation, (long)error, error.ToString(), messageParameters));
      //^ assume this.currentToken == oldToken;
    }

    /// <summary>
    /// Call this method only on a freshly allocated Parser instance and call it only once.
    /// </summary>
    internal void ParseNamespaceBody(List<INamespaceDeclarationMember> members, List<Ast.SourceCustomAttribute> sourceAttributes) 
    {
      //^ assume this.currentToken != Token.EndOfFile; //assume this method is called directly after construction and then never again.
      this.GetNextToken(); //Get first token from scanner
      this.ParseNamespaceBody(members, sourceAttributes, Parser.EndOfFile);
    }

    private void ParseNamespaceBody(List<INamespaceDeclarationMember> members, List<Ast.SourceCustomAttribute>/*?*/ sourceAttributes, TokenSet followers) {
    tryAgain:
      this.ParseExternalAliasDirectives(members, followers|Parser.AttributeOrNamespaceOrTypeDeclarationStart|Token.Using|Token.EndOfFile);
      this.ParseUsingDirectives(members, followers|Parser.AttributeOrNamespaceOrTypeDeclarationStart|Token.Extern|Token.EndOfFile);
      if (this.currentToken == Token.Extern) {
        this.HandleError(Error.ExternAfterElements);
        goto tryAgain;
      }
      if (sourceAttributes != null)
        this.ParseAttributes(ref sourceAttributes, true, followers|Parser.NamespaceOrTypeDeclarationStart);
      if (this.currentToken != Token.EndOfFile)
        this.ParseNamespaceMemberDeclarations(members, followers);
      members.TrimExcess();
    }

    private void ParseExternalAliasDirectives(List<INamespaceDeclarationMember> members, TokenSet followers)
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      while (this.currentToken == Token.Extern)
        this.ParseExternalAliasDirective(members, followers|Token.Extern);
      this.SkipTo(followers);
    }

    private void ParseExternalAliasDirective(List<INamespaceDeclarationMember> members, TokenSet followers)
      //^ requires this.currentToken == Token.Extern;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      SourceLocationBuilder sctx = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
      this.GetNextToken();
      if (this.currentToken == Token.Alias)
        this.GetNextToken();
      else {
        if (!Parser.IdentifierOrNonReservedKeyword[this.currentToken]) {
          this.SkipTo(followers, Error.SyntaxError, "alias");
          return;
        }
        this.HandleError(Error.SyntaxError, "alias");
      }
      if (!Parser.IdentifierOrNonReservedKeyword[this.currentToken]) {
        this.SkipTo(followers, Error.ExpectedIdentifier);
        return;
      }
      NameDeclaration name = this.ParseNameDeclaration();
      sctx.UpdateToSpan(this.scanner.SourceLocationOfLastScannedToken);
      members.Add(new UnitSetAliasDeclaration(name, sctx.GetSourceLocation()));
      this.SkipSemiColon(followers);
    }

    private NameDeclaration ParseNameDeclaration() {
      IName name;
      ISourceLocation sourceLocation = this.scanner.SourceLocationOfLastScannedToken;
      if (Parser.IdentifierOrNonReservedKeyword[this.currentToken]) {
        name = this.GetNameFor(this.scanner.GetIdentifierString());
        //^ assume this.currentToken != Token.EndOfFile; //assume Parser.IdentifierOrNonReservedKeyword is constructed correctly
        this.GetNextToken();
      } else {
        name = Dummy.Name;
      }
      return new NameDeclaration(name, sourceLocation);
    }

    private void ParseUsingDirectives(List<INamespaceDeclarationMember> members, TokenSet followers)
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      while (this.currentToken == Token.Using)
        this.ParseUsingDirective(members, followers|Token.Using);
      this.SkipTo(followers);
    }

    private void ParseUsingDirective(List<INamespaceDeclarationMember> members, TokenSet followers)
      //^ requires this.currentToken == Token.Using;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      SourceLocationBuilder sctx = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
      this.GetNextToken();
      if (!Parser.IdentifierOrNonReservedKeyword[this.currentToken]) {
        this.SkipTo(followers, Error.ExpectedIdentifier);
        return;
      }
      NameDeclaration name = this.ParseNameDeclaration();
      TokenSet followersOrSemicolon = followers|Token.Semicolon;
      if (this.currentToken == Token.Assign) {
        this.GetNextToken();
        Expression referencedNamespaceOrType = this.ParseNamespaceOrTypeName(false, followersOrSemicolon);
        sctx.UpdateToSpan(referencedNamespaceOrType.SourceLocation);
        members.Add(new AliasDeclaration(name, referencedNamespaceOrType, sctx.GetSourceLocation()));
      } else {
        NamespaceReferenceExpression namespaceName = this.ParseImportedNamespaceName(name, followersOrSemicolon);
        sctx.UpdateToSpan(namespaceName.SourceLocation);
        NameDeclaration dummyName = new NameDeclaration(Dummy.Name, this.scanner.SourceLocationOfLastScannedToken);
        members.Add(new NamespaceImportDeclaration(dummyName, namespaceName, sctx.GetSourceLocation()));
      }
      this.SkipSemiColon(followers);
    }

    private NamespaceReferenceExpression ParseImportedNamespaceName(NameDeclaration name, TokenSet followers)
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      Expression expression = new SimpleName(name, name.SourceLocation, false);
      SourceLocationBuilder sctx = new SourceLocationBuilder(expression.SourceLocation);
      if (this.currentToken == Token.DoubleColon) {
        this.GetNextToken();
        SimpleName simpleName = this.ParseSimpleName(followers|Token.Dot);
        sctx.UpdateToSpan(simpleName.SourceLocation);
        expression = new AliasQualifiedName(expression, simpleName, sctx.GetSourceLocation());
      }
      while (this.currentToken == Token.Dot)
        //^ invariant expression is SimpleName || expression is QualifiedName || expression is AliasQualifiedName;
      {
        this.GetNextToken();
        SimpleName simpleName = this.ParseSimpleName(followers|Token.Dot);
        sctx.UpdateToSpan(simpleName.SourceLocation);
        expression = new QualifiedName(expression, simpleName, sctx.GetSourceLocation());
      }
      NamespaceReferenceExpression result = new NamespaceReferenceExpression(expression, sctx.GetSourceLocation());
      this.SkipTo(followers);
      return result;
    }

    private SimpleName ParseSimpleName(TokenSet followers)
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      IName name;
      ISourceLocation sourceLocation = this.scanner.SourceLocationOfLastScannedToken;
      if (Parser.IdentifierOrNonReservedKeyword[this.currentToken]) {
        name = this.GetNameFor(this.scanner.GetIdentifierString());
        //^ assume this.currentToken != Token.EndOfFile;
        this.GetNextToken();
      } else {
        name = Dummy.Name;
        this.HandleError(Error.ExpectedIdentifier);
      }
      SimpleName result = new SimpleName(name, sourceLocation, false);
      this.SkipTo(followers);
      return result;
    }

    private Expression ParseNamespaceOrTypeName(bool allowEmptyArguments, TokenSet followers)
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
      //^ ensures result is SimpleName || result is AliasQualifiedName || result is QualifiedName || result is GenericTypeInstanceExpression;
    {
      SimpleName rootName = this.ParseSimpleName(followers|Token.Dot|Token.DoubleColon|Token.LessThan);
      Expression expression = rootName;
      if (rootName.Name.UniqueKey == this.nameTable.global.UniqueKey && this.currentToken == Token.DoubleColon)
        expression = new RootNamespaceExpression(rootName.SourceLocation);
      SourceLocationBuilder sctx = new SourceLocationBuilder(expression.SourceLocation);
      if (this.currentToken == Token.DoubleColon) {
        this.GetNextToken();
        SimpleName simpleName = this.ParseSimpleName(followers|Token.Dot|Token.LessThan);
        sctx.UpdateToSpan(simpleName.SourceLocation);
        expression = new AliasQualifiedName(expression, simpleName, sctx.GetSourceLocation());
      }
      //^ assume expression is SimpleName || expression is AliasQualifiedName; //RootNamespace will always disappear into AliasQualifiedName
    moreDots:
      while (this.currentToken == Token.Dot)
        //^ invariant expression is SimpleName || expression is AliasQualifiedName || expression is QualifiedName || expression is GenericTypeInstanceExpression;
      {
        this.GetNextToken();
        SimpleName simpleName = this.ParseSimpleName(followers|Token.Dot|Token.LessThan);
        sctx.UpdateToSpan(simpleName.SourceLocation);
        expression = new QualifiedName(expression, simpleName, sctx.GetSourceLocation());
      }
      if (this.currentToken == Token.LessThan) {
        //^ assume expression is SimpleName || expression is AliasQualifiedName || expression is QualifiedName; //Can only get back here if generic instance was followed by dot.
        TypeExpression genericType = new NamedTypeExpression(expression);
        while (this.currentToken == Token.LessThan)
          //^ invariant expression is SimpleName || expression is AliasQualifiedName || expression is QualifiedName || expression is GenericTypeInstanceExpression;
        {
          List<TypeExpression> arguments = this.ParseTypeArguments(sctx, allowEmptyArguments, followers|Token.Dot);
          expression = new GenericTypeInstanceExpression(genericType, arguments.AsReadOnly(), sctx.GetSourceLocation());
        }
        if (this.currentToken == Token.Dot) goto moreDots;
      }
      if (this.insideType && !this.insideBlock && !followers[this.currentToken])
        this.SkipTo(followers, Error.InvalidMemberDecl, this.scanner.GetTokenSource());
      else
        this.SkipTo(followers);
      return expression;
    }

    private List<TypeExpression> ParseTypeArguments(SourceLocationBuilder sctx, bool allowEmptyArguments, TokenSet followers)
      //^ requires this.currentToken == Token.LessThan;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      this.GetNextToken();
      List<TypeExpression> result = new List<TypeExpression>();
      bool sawEmptyArgument = false;
      ISourceLocation commaContext = this.scanner.SourceLocationOfLastScannedToken;
      while (this.currentToken != Token.EndOfFile) {
        if ((this.currentToken == Token.GreaterThan || this.currentToken == Token.RightShift) && result.Count > 0 && !sawEmptyArgument) break;
        if (this.currentToken == Token.Comma || this.currentToken == Token.GreaterThan) {
          result.Add(new EmptyTypeExpression(this.scanner.SourceLocationOfLastScannedToken));
          if (allowEmptyArguments) {
            sawEmptyArgument = true;
            commaContext = this.scanner.SourceLocationOfLastScannedToken;
          }else
            this.HandleError(commaContext, Error.TypeExpected);
          if (this.currentToken == Token.GreaterThan) break;
          //^ assume this.currentToken == Token.Comma;
          this.GetNextToken();
          continue;
        }
        if (sawEmptyArgument) this.HandleError(Error.TypeExpected);
        TypeExpression t = this.ParseTypeExpression(false, allowEmptyArguments, followers|Token.Comma|Token.GreaterThan|Token.RightShift);
        result.Add(t);
        if (this.currentToken != Token.Comma) break;
        this.GetNextToken();
      }
      sctx.UpdateToSpan(this.scanner.SourceLocationOfLastScannedToken);
      if (this.currentToken == Token.RightShift) {
        this.currentToken = Token.GreaterThan;
        //^ assume followers[Token.GreaterThan];
      } else
        this.SkipOverTo(Token.GreaterThan, followers);
      result.TrimExcess();
      //^ assume followers[this.currentToken];
      return result;
    }

    private TypeExpression ParseTypeExpression(bool formsPartOfBooleanExpression, bool allowEmptyArguments, TokenSet followers)
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      if (this.currentToken == Token.EndOfFile) {
        this.HandleError(Error.TypeExpected);
        return this.GetTypeExpressionFor(Token.Object, SourceDummy.SourceLocation);
      }
      TokenSet followersOrTypeOperator = followers|Parser.TypeOperator;
      TypeExpression type = this.ParseBaseTypeExpression(allowEmptyArguments, followersOrTypeOperator);
      SourceLocationBuilder sctx = new SourceLocationBuilder(type.SourceLocation);
      for (; ; ) {
        sctx.UpdateToSpan(this.scanner.SourceLocationOfLastScannedToken);
        switch (this.currentToken) {
          case Token.BitwiseAnd:
            this.HandleError(Error.ExpectedIdentifier); //TODO: this matches C#, but a better error would be nice
            this.GetNextToken();
            break;
          case Token.LeftBracket:
            uint rank = this.ParseRankSpecifier(sctx, followersOrTypeOperator);
            type = this.ParseArrayType(rank, type, sctx,  followersOrTypeOperator);
            break;
          case Token.Multiply: {
            this.GetNextToken();
            type = new PointerTypeExpression(type, sctx.GetSourceLocation());
            break;
          }
          case Token.LogicalNot: {
            this.GetNextToken();
            type = new NonNullTypeExpression(type, sctx.GetSourceLocation());
            break;
          }
          case Token.Conditional: {
            if (formsPartOfBooleanExpression && Parser.NullableTypeNonFollower[this.PeekNextToken()]) goto done;
            this.GetNextToken();
            type = new NullableTypeExpression(type, sctx.GetSourceLocation());
            break;
          }
          default:
            goto done;
        }
      }
    done:
      this.SkipTo(followers);
      return type;
    }

    private TypeExpression ParseArrayType(uint rank, TypeExpression elementType, SourceLocationBuilder sctx, TokenSet followers)
      //^ requires rank > 0;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      List<uint> rankList = new List<uint>();
      for (;;) 
        // ^ invariant forall{int i in (0:rankList.Count); rankList[i] > 0}; //TODO: find out why this does not parse
      {
        rankList.Add(rank); //TODO: find away to tell Boogie that this does not modify this.currentToken
        if (this.currentToken != Token.LeftBracket) break;
        rank = this.ParseRankSpecifier(sctx, followers|Token.LeftBracket);
      }
      for (int i = rankList.Count; i > 0; i--)
        // ^ invariant forall{int i in (0:rankList.Count); rankList[i] > 0};
      {
        rank = rankList[i-1];
        //^ assume rank > 0; 
        elementType = new ArrayTypeExpression(elementType, rank, sctx.GetSourceLocation()); //TODO: find away to tell Boogie that this does not modify this.currentToken
      }
      //^ assume followers[this.currentToken] || this.currentToken == Token.EndOfFile;
      return elementType;
    }

    private uint ParseRankSpecifier(SourceLocationBuilder sctx, TokenSet followers)
      //^ requires this.currentToken == Token.LeftBracket;
      //^ ensures result > 0;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      this.GetNextToken();
      uint rank = 1;
      while (this.currentToken == Token.Comma) {
        rank++;
        this.GetNextToken();
      }
      ISourceLocation tokLoc = this.scanner.SourceLocationOfLastScannedToken;
      //^ assume tokLoc.SourceDocument == sctx.SourceDocument;
      sctx.UpdateToSpan(tokLoc);
      this.SkipOverTo(Token.RightBracket, followers);
      return rank;
    }

    private TypeExpression ParseBaseTypeExpression(bool allowEmptyArguments, TokenSet followers) 
      //^ requires this.currentToken != Token.EndOfFile;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      TypeExpression result;
      switch(this.currentToken){
        case Token.Bool:
        case Token.Decimal:
        case Token.Sbyte:
        case Token.Byte:
        case Token.Short:
        case Token.Ushort:
        case Token.Int:
        case Token.Uint:
        case Token.Long:
        case Token.Ulong:
        case Token.Char:
        case Token.Float:
        case Token.Double:
        case Token.Object:
        case Token.String:
        case Token.Void:
          result = this.GetTypeExpressionFor(this.currentToken, this.scanner.SourceLocationOfLastScannedToken);
          this.GetNextToken();
          this.SkipTo(followers);
          break;
        default:
          Expression expr = this.ParseNamespaceOrTypeName(allowEmptyArguments, followers);
          GenericTypeInstanceExpression/*?*/ gtexpr = expr as GenericTypeInstanceExpression;
          if (gtexpr != null)
            result = gtexpr;
          else
            result = new NamedTypeExpression(expr);
          break;
      }
      return result;
    }

    /// <summary>
    /// Call this method only on a freshly allocated Parser instance and call it only once.
    /// </summary>
    internal INamespaceDeclarationMember/*?*/ ParseNamespaceDeclarationMember()
      //^ ensures result == null || result is NamespaceDeclarationMember || result is NamespaceTypeDeclaration || result is NestedNamespaceDeclaration;
    {
      //^ assume this.currentToken != Token.EndOfFile; //assume this method is called directly after construction and then never again.
      List<INamespaceDeclarationMember> members = new List<INamespaceDeclarationMember>(1);
      this.GetNextToken();
      this.ParseNamespaceMemberDeclarations(members, Parser.EndOfFile|Parser.AttributeOrNamespaceOrTypeDeclarationStart);
      if (members.Count != 1 || this.currentToken != Token.EndOfFile) return null;
      INamespaceDeclarationMember result = members[0];
      //^ assume result is NamespaceDeclarationMember || result is NamespaceTypeDeclaration || result is NestedNamespaceDeclaration; //TODO: get this from a post condition
      return result;
    }

    private void ParseNamespaceMemberDeclarations(List<INamespaceDeclarationMember> members, TokenSet followers)
      //^ requires this.currentToken != Token.EndOfFile;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      TokenSet followersOrAttributeOrNamespaceOrTypeDeclarationStart = followers|Parser.AttributeOrNamespaceOrTypeDeclarationStart;
      while (followersOrAttributeOrNamespaceOrTypeDeclarationStart[this.currentToken] && this.currentToken != Token.EndOfFile && this.currentToken != Token.RightBrace)
        this.ParseNamespaceOrTypeDeclarations(members, followersOrAttributeOrNamespaceOrTypeDeclarationStart);
      this.SkipTo(followers);
    }

    private void ParseNamespaceOrTypeDeclarations(List<INamespaceDeclarationMember> members, TokenSet followers)
      //^ requires followers[this.currentToken];
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      if (this.currentToken == Token.Private || this.currentToken == Token.Protected) {
        this.HandleError(Error.PrivateOrProtectedNamespaceElement);
        this.GetNextToken();
      }
      if (this.currentToken == Token.Namespace)
        this.ParseNestedNamespaceDeclaration(members, followers);
      else
        this.ParseTypeDeclaration(members, followers);
    }

    private void ParseNestedNamespaceDeclaration(List<INamespaceDeclarationMember> parentMembers, TokenSet followers)
      //^ requires this.currentToken == Token.Namespace;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      SourceLocationBuilder nsCtx = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
      this.GetNextToken();
      if (!Parser.IdentifierOrNonReservedKeyword[this.currentToken]) 
        this.HandleError(Error.ExpectedIdentifier);
      NameDeclaration nsName = this.ParseNameDeclaration();
      nsCtx.UpdateToSpan(this.scanner.SourceLocationOfLastScannedToken);
      List<INamespaceDeclarationMember> nestedMembers = new List<INamespaceDeclarationMember>();
      List<Ast.SourceCustomAttribute> nestedSourceAttributes = new List<Ast.SourceCustomAttribute>();
      NestedNamespaceDeclaration nestedNamespace = new NestedNamespaceDeclaration(nsName, nestedMembers, nestedSourceAttributes, nsCtx);
      parentMembers.Add(nestedNamespace);
      while (this.currentToken == Token.Dot) {
        this.GetNextToken();
        if (!Parser.IdentifierOrNonReservedKeyword[this.currentToken])
          this.HandleError(Error.ExpectedIdentifier);
        nsName = this.ParseNameDeclaration();
        parentMembers = nestedMembers;
        nestedMembers = new List<INamespaceDeclarationMember>();
        nestedSourceAttributes = new List<SourceCustomAttribute>();
        nestedNamespace = new NestedNamespaceDeclaration(nsName, nestedMembers, nestedSourceAttributes, nsCtx);
        parentMembers.Add(nestedNamespace);
      }
      this.Skip(Token.LeftBrace);
      this.ParseNamespaceBody(nestedMembers, null, followers|Token.RightBrace);
      nsCtx.UpdateToSpan(this.scanner.SourceLocationOfLastScannedToken);
      this.SkipOverTo(Token.RightBrace, followers);
    }

    private void ParseTypeDeclaration(List<INamespaceDeclarationMember> namespaceMembers, TokenSet followers)
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      SourceLocationBuilder sctx = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
      List<SourceCustomAttribute>/*?*/ attributes = this.ParseAttributes(followers|Parser.AttributeOrTypeDeclarationStart);
      List<ModifierToken> modifiers = this.ParseModifiers();
      TypeDeclaration.Flags flags = this.ConvertToTypeDeclarationFlags(modifiers);
      switch (this.currentToken) {
        case Token.Class:
          this.ParseNamespaceClassDeclaration(namespaceMembers, attributes, flags, sctx, followers); 
          break;
        case Token.Interface:
          this.ParseNamespaceInterfaceDeclaration(namespaceMembers, attributes, flags, sctx, followers); 
          break;
        case Token.Struct:
          this.ParseNamespaceStructDeclaration(namespaceMembers, attributes, flags, sctx, followers); 
          break;
        case Token.Delegate:
          this.ParseNamespaceDelegateDeclaration(namespaceMembers, attributes, flags, sctx, followers);
          break;
        case Token.Enum:
          this.ParseNamespaceEnumDeclaration(namespaceMembers, attributes, flags, sctx, followers);
          break;
        default:
          if (modifiers.Count > 0 || !followers[this.currentToken])
            this.SkipTo(followers, Error.BadTokenInType);
          return;
      }
    }

    private void ParseAttributes(ref List<SourceCustomAttribute>/*?*/ sourceAttributes, bool globalAttributes, TokenSet followers) {
      while (this.currentToken == Token.LeftBracket) {
        int position = this.scanner.CurrentDocumentPosition();
        this.GetNextToken();
        AttributeTargets target = this.ParseAttributeTarget();
        if (globalAttributes) {
          if (target != AttributeTargets.Assembly && target != AttributeTargets.Module) {
            this.scanner.RestoreDocumentPosition(position);
            this.currentToken = Token.None;
            this.GetNextToken();
            return;
          }
        }
        while (true) {
          Expression expr = this.ParseExpression(followers|Token.Comma|Token.RightBracket);
          MethodCall/*?*/ mcall = expr as MethodCall;
          if (mcall != null && (mcall.MethodExpression is SimpleName || mcall.MethodExpression is QualifiedName || mcall.MethodExpression is AliasQualifiedName)) {
            AttributeTypeExpression type = new AttributeTypeExpression(mcall.MethodExpression);
            List<Expression> arguments = new List<Expression>(mcall.OriginalArguments);
            bool seenNamedArgument = false;
            for (int i = 0, n = arguments.Count; i < n; i++) {
              Assignment/*?*/ assignment = arguments[i] as Assignment;
              if (assignment == null) {
                if (seenNamedArgument)
                  this.HandleError(arguments[i].SourceLocation, Error.NamedArgumentExpected);
                continue;
              }
              SimpleName/*?*/ name = assignment.Target.Expression as SimpleName;
              if (name == null) {
                this.HandleError(assignment.Target.SourceLocation, Error.ExpectedIdentifier);
                name = new SimpleName(Dummy.Name, assignment.Target.SourceLocation, false);
              }
              seenNamedArgument = true;
              arguments[i] = new NamedArgument(name, assignment.Source, assignment.SourceLocation);
            }
            if (sourceAttributes == null) sourceAttributes = new List<SourceCustomAttribute>(1);
            sourceAttributes.Add(new SourceCustomAttribute(target, type, arguments, mcall.SourceLocation));
          } else if (expr is SimpleName || expr is QualifiedName || expr is AliasQualifiedName) {
            AttributeTypeExpression type = new AttributeTypeExpression(expr);
            if (sourceAttributes == null) sourceAttributes = new List<SourceCustomAttribute>(1);
            sourceAttributes.Add(new SourceCustomAttribute(target, type, new List<Expression>(0), expr.SourceLocation));
          } else {
            this.HandleError(expr.SourceLocation, Error.ExpectedIdentifier);
          }
          if (this.currentToken != Token.Comma) break;
          this.GetNextToken();
        }
        this.Skip(Token.RightBracket);
      }
      if (sourceAttributes != null) sourceAttributes.TrimExcess();
    }

    private List<SourceCustomAttribute>/*?*/ ParseAttributes(TokenSet followers) {
      List<SourceCustomAttribute>/*?*/ result = null;
      this.ParseAttributes(ref result, false, followers);
      this.SkipTo(followers);
      return result;
    }

    private AttributeTargets ParseAttributeTarget() {
      AttributeTargets result = (AttributeTargets)0;
      switch (this.currentToken) {
        case Token.Event:
        case Token.Identifier:
        case Token.Return:
          if (this.PeekNextToken() == Token.Colon) {
            string id = this.scanner.GetIdentifierString();
            switch (id) {
              case "assembly": result = AttributeTargets.Assembly; break;
              case "event": result = AttributeTargets.Event; break;
              case "field": result = AttributeTargets.Field; break;
              case "method": result = AttributeTargets.Method|AttributeTargets.Constructor; break;
              case "module": result = AttributeTargets.Module; break;
              case "parameter": result = AttributeTargets.Parameter; break;
              case "property": result = AttributeTargets.Property; break;
              case "return": result = AttributeTargets.ReturnValue; break;
              case "type": result = AttributeTargets.Class|AttributeTargets.Delegate|AttributeTargets.Enum|AttributeTargets.Interface|AttributeTargets.Struct; break;
              default:
                this.HandleError(Error.InvalidAttributeLocation, id);
                break;
            }
            this.GetNextToken(); //Skip the id
            this.GetNextToken(); //Skip the colon
          }
          break;
        default:
          break;
      }
      return result;
    }

    private void ParseNamespaceClassDeclaration(List<INamespaceDeclarationMember> namespaceMembers, List<SourceCustomAttribute>/*?*/ attributes, TypeDeclaration.Flags flags, SourceLocationBuilder sctx, TokenSet followers)
      //^ requires this.currentToken == Token.Class;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      this.GetNextToken();
      if (!Parser.IdentifierOrNonReservedKeyword[this.currentToken])
        this.HandleError(Error.ExpectedIdentifier);
      NameDeclaration name = this.ParseNameDeclaration();
      List<Ast.GenericTypeParameterDeclaration> genericParameters = new List<Ast.GenericTypeParameterDeclaration>(); 
      List<TypeExpression> baseTypes = new List<TypeExpression>();
      List<ITypeDeclarationMember> members = new List<ITypeDeclarationMember>();
      NamespaceClassDeclaration type = new NamespaceClassDeclaration(attributes, flags, name, genericParameters, baseTypes, members, sctx);
      namespaceMembers.Add(type);
      this.ParseRestOfTypeDeclaration(sctx, type, genericParameters, baseTypes, members, followers);
    }

    private void ParseNestedClassDeclaration(List<ITypeDeclarationMember> typeMembers, List<SourceCustomAttribute>/*?*/ attributes, TypeDeclaration.Flags flags, SourceLocationBuilder sctx, TokenSet followers)
      //^ requires this.currentToken == Token.Class;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      this.GetNextToken();
      if (!Parser.IdentifierOrNonReservedKeyword[this.currentToken])
        this.HandleError(Error.ExpectedIdentifier);
      NameDeclaration name = this.ParseNameDeclaration();
      List<Ast.GenericTypeParameterDeclaration> genericParameters = new List<Ast.GenericTypeParameterDeclaration>();
      List<TypeExpression> baseTypes = new List<TypeExpression>();
      List<ITypeDeclarationMember> members = new List<ITypeDeclarationMember>();
      NestedClassDeclaration type = new NestedClassDeclaration(attributes, flags, name, genericParameters, baseTypes, members, sctx);
      typeMembers.Add(type);
      this.ParseRestOfTypeDeclaration(sctx, type, genericParameters, baseTypes, members, followers);
    }

    private void ParseNamespaceInterfaceDeclaration(List<INamespaceDeclarationMember> namespaceMembers, List<SourceCustomAttribute>/*?*/ attributes, TypeDeclaration.Flags flags, SourceLocationBuilder sctx, TokenSet followers)
      //^ requires this.currentToken == Token.Interface;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      this.GetNextToken();
      if (!Parser.IdentifierOrNonReservedKeyword[this.currentToken])
        this.HandleError(Error.ExpectedIdentifier);
      NameDeclaration name = this.ParseNameDeclaration();
      List<Ast.GenericTypeParameterDeclaration> genericParameters = new List<Ast.GenericTypeParameterDeclaration>();
      List<TypeExpression> baseTypes = new List<TypeExpression>();
      List<ITypeDeclarationMember> members = new List<ITypeDeclarationMember>();
      NamespaceInterfaceDeclaration type = new NamespaceInterfaceDeclaration(attributes, flags, name, genericParameters, baseTypes, members, sctx);
      namespaceMembers.Add(type);
      this.ParseRestOfTypeDeclaration(sctx, type, genericParameters, baseTypes, members, followers);
    }

    private void ParseNestedInterfaceDeclaration(List<ITypeDeclarationMember> typeMembers, List<SourceCustomAttribute>/*?*/ attributes, TypeDeclaration.Flags flags, SourceLocationBuilder sctx, TokenSet followers)
      //^ requires this.currentToken == Token.Interface;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      this.GetNextToken();
      if (!Parser.IdentifierOrNonReservedKeyword[this.currentToken])
        this.HandleError(Error.ExpectedIdentifier);
      NameDeclaration name = this.ParseNameDeclaration();
      List<Ast.GenericTypeParameterDeclaration> genericParameters = new List<Ast.GenericTypeParameterDeclaration>();
      List<TypeExpression> baseTypes = new List<TypeExpression>();
      List<ITypeDeclarationMember> members = new List<ITypeDeclarationMember>();
      NestedInterfaceDeclaration type = new NestedInterfaceDeclaration(attributes, flags, name, genericParameters, baseTypes, members, sctx);
      typeMembers.Add(type);
      this.ParseRestOfTypeDeclaration(sctx, type, genericParameters, baseTypes, members, followers);
    }

    private void ParseNamespaceStructDeclaration(List<INamespaceDeclarationMember> namespaceMembers, List<SourceCustomAttribute>/*?*/ attributes, TypeDeclaration.Flags flags, SourceLocationBuilder sctx, TokenSet followers)
      //^ requires this.currentToken == Token.Struct;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      this.GetNextToken();
      if (!Parser.IdentifierOrNonReservedKeyword[this.currentToken])
        this.HandleError(Error.ExpectedIdentifier);
      NameDeclaration name = this.ParseNameDeclaration();
      List<Ast.GenericTypeParameterDeclaration> genericParameters = new List<Ast.GenericTypeParameterDeclaration>();
      List<TypeExpression> baseTypes = new List<TypeExpression>();
      List<ITypeDeclarationMember> members = new List<ITypeDeclarationMember>();
      NamespaceStructDeclaration type = new NamespaceStructDeclaration(attributes, flags, name, genericParameters, baseTypes, members, sctx);
      namespaceMembers.Add(type);
      this.ParseRestOfTypeDeclaration(sctx, type, genericParameters, baseTypes, members, followers);
    }

    private void ParseNestedStructDeclaration(List<ITypeDeclarationMember> typeMembers, List<SourceCustomAttribute>/*?*/ attributes, TypeDeclaration.Flags flags, SourceLocationBuilder sctx, TokenSet followers)
      //^ requires this.currentToken == Token.Struct;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      this.GetNextToken();
      if (!Parser.IdentifierOrNonReservedKeyword[this.currentToken])
        this.HandleError(Error.ExpectedIdentifier);
      NameDeclaration name = this.ParseNameDeclaration();
      List<Ast.GenericTypeParameterDeclaration> genericParameters = new List<Ast.GenericTypeParameterDeclaration>();
      List<TypeExpression> baseTypes = new List<TypeExpression>();
      List<ITypeDeclarationMember> members = new List<ITypeDeclarationMember>();
      NestedStructDeclaration type = new NestedStructDeclaration(attributes, flags, name, genericParameters, baseTypes, members, sctx);
      typeMembers.Add(type);
      this.ParseRestOfTypeDeclaration(sctx, type, genericParameters, baseTypes, members, followers);
    }

    private void ParseNamespaceDelegateDeclaration(List<INamespaceDeclarationMember> namespaceMembers, List<SourceCustomAttribute>/*?*/ attributes, TypeDeclaration.Flags flags, SourceLocationBuilder sctx, TokenSet followers)
      //^ requires this.currentToken == Token.Delegate;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      this.GetNextToken();
      TypeExpression returnType = this.ParseTypeExpression(false, false, followers|Token.LeftParenthesis|Token.Semicolon|Parser.IdentifierOrNonReservedKeyword);
      if (!Parser.IdentifierOrNonReservedKeyword[this.currentToken])
        this.HandleError(Error.ExpectedIdentifier);
      NameDeclaration name = this.ParseNameDeclaration();
      List<Ast.GenericTypeParameterDeclaration> genericParameters = new List<Ast.GenericTypeParameterDeclaration>();
      List<Ast.ParameterDeclaration> parameters = new List<Ast.ParameterDeclaration>();
      SignatureDeclaration signature = new SignatureDeclaration(returnType, parameters, sctx);
      NamespaceDelegateDeclaration type = new NamespaceDelegateDeclaration(attributes, flags, name, genericParameters, signature, sctx);
      namespaceMembers.Add(type);
      this.ParseGenericTypeParameters(genericParameters, followers|Token.LeftParenthesis|Token.Where|Token.Semicolon);
      this.ParseParameters(parameters, Token.RightParenthesis, followers|Token.Where|Token.Semicolon, sctx);
      this.ParseGenericTypeParameterConstraintsClauses(genericParameters, followers|Token.Semicolon);
      if (this.currentToken == Token.Semicolon)
        this.GetNextToken();
      this.SkipTo(followers);
    }

    private void ParseNestedDelegateDeclaration(List<ITypeDeclarationMember> typeMembers, List<SourceCustomAttribute>/*?*/ attributes, TypeDeclaration.Flags flags, SourceLocationBuilder sctx, TokenSet followers)
      //^ requires this.currentToken == Token.Delegate;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      this.GetNextToken();
      TypeExpression returnType = this.ParseTypeExpression(false, false, followers|Token.LeftParenthesis|Token.Semicolon|Parser.IdentifierOrNonReservedKeyword);
      if (!Parser.IdentifierOrNonReservedKeyword[this.currentToken])
        this.HandleError(Error.ExpectedIdentifier);
      NameDeclaration name = this.ParseNameDeclaration();
      List<Ast.GenericTypeParameterDeclaration> genericParameters = new List<Ast.GenericTypeParameterDeclaration>();
      List<Ast.ParameterDeclaration> parameters = new List<Ast.ParameterDeclaration>();
      SignatureDeclaration signature = new SignatureDeclaration(returnType, parameters, sctx);
      NestedDelegateDeclaration type = new NestedDelegateDeclaration(attributes, flags, name, genericParameters, signature, sctx);
      typeMembers.Add(type);
      this.ParseGenericTypeParameters(genericParameters, followers|Token.LeftParenthesis|Token.Where|Token.Semicolon);
      this.ParseParameters(parameters, Token.RightParenthesis, followers|Token.Where|Token.Semicolon, sctx);
      this.ParseGenericTypeParameterConstraintsClauses(genericParameters, followers|Token.Semicolon);
      sctx.UpdateToSpan(this.scanner.SourceLocationOfLastScannedToken);
      if (this.currentToken == Token.Semicolon)
        this.GetNextToken();
      this.SkipTo(followers);
    }

    private void ParseNamespaceEnumDeclaration(List<INamespaceDeclarationMember> namespaceMembers, List<SourceCustomAttribute>/*?*/ attributes, TypeDeclaration.Flags flags, SourceLocationBuilder sctx, TokenSet followers)
      //^ requires this.currentToken == Token.Enum;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      this.GetNextToken();
      if (!Parser.IdentifierOrNonReservedKeyword[this.currentToken])
        this.HandleError(Error.ExpectedIdentifier);
      NameDeclaration name = this.ParseNameDeclaration();
      TypeExpression/*?*/ underlyingType = null;
      if (this.currentToken == Token.Colon) {
        this.GetNextToken();
        if (this.currentToken != Token.EndOfFile)
          underlyingType = this.ParseTypeExpression(false, false, followers|Token.LeftBrace);
      }
      List<ITypeDeclarationMember> members = new List<ITypeDeclarationMember>();
      NamespaceEnumDeclaration type = new NamespaceEnumDeclaration(attributes, flags, name, underlyingType, members, sctx);
      namespaceMembers.Add(type);
      this.ParseRestOfEnum(sctx, type, members, followers);
    }

    private void ParseNestedEnumDeclaration(List<ITypeDeclarationMember> namespaceMembers, List<SourceCustomAttribute>/*?*/ attributes, TypeDeclaration.Flags flags, SourceLocationBuilder sctx, TokenSet followers)
      //^ requires this.currentToken == Token.Enum;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      this.GetNextToken();
      if (!Parser.IdentifierOrNonReservedKeyword[this.currentToken])
        this.HandleError(Error.ExpectedIdentifier);
      NameDeclaration name = this.ParseNameDeclaration();
      TypeExpression/*?*/ underlyingType = null;
      if (this.currentToken == Token.Colon) {
        this.GetNextToken();
        if (this.currentToken != Token.EndOfFile)
          underlyingType = this.ParseTypeExpression(false, false, followers|Token.LeftBrace);
      }
      List<ITypeDeclarationMember> members = new List<ITypeDeclarationMember>();
      NestedEnumDeclaration type = new NestedEnumDeclaration(attributes, flags, name, underlyingType, members, sctx);
      namespaceMembers.Add(type);
      this.ParseRestOfEnum(sctx, type, members, followers);
    }

    private void ParseGenericTypeParameters(List<Ast.GenericTypeParameterDeclaration> genericParameters, TokenSet followers) {
      if (this.currentToken != Token.LessThan) return;
      this.GetNextToken();
      while (this.currentToken != Token.GreaterThan && this.currentToken != Token.Colon && this.currentToken != Token.LeftBrace && this.currentToken != Token.EndOfFile) {
        List<SourceCustomAttribute>/*?*/ attributes = this.ParseAttributes(followers|Parser.IdentifierOrNonReservedKeyword);
        if (!Parser.IdentifierOrNonReservedKeyword[this.currentToken])
          this.HandleError(Error.ExpectedIdentifier);
        NameDeclaration name = this.ParseNameDeclaration();
        genericParameters.Add(new CSharpGenericTypeParameterDeclaration(attributes, name, (ushort)genericParameters.Count));
        if (this.currentToken != Token.Comma) break;
        this.GetNextToken();
      }
      if (this.currentToken == Token.RightShift)
        this.currentToken = Token.GreaterThan;
      else
        this.SkipOverTo(Token.GreaterThan, followers);
    }

    private void ParseBaseTypes(List<TypeExpression> baseTypes, TokenSet followers)
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      if (this.currentToken == Token.Colon) {
        this.GetNextToken();
        TokenSet baseTypeStart = Parser.IdentifierOrNonReservedKeyword|Parser.Predefined;
        while (baseTypeStart[this.currentToken]) {
          //^ assume this.currentToken != Token.EndOfFile;
          baseTypes.Add(this.ParseTypeExpression(false, false, followers|Token.Comma));
          if (this.currentToken != Token.Comma) break;
          this.GetNextToken();
        }
      }
      this.SkipTo(followers);
    }

    private void ParseGenericTypeParameterConstraintsClauses(List<Ast.GenericTypeParameterDeclaration> genericTypeParameters, TokenSet followers)
      // ^ requires forall{Ast.GenericTypeParameterDeclaration genericTypeParameter in genericTypeParameters; genericTypeParameter is CSharpGenericTypeParameterDeclaration};
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      while (this.currentToken == Token.Where) {
        this.GetNextToken();
        if (!Parser.IdentifierOrNonReservedKeyword[this.currentToken])
          this.HandleError(Error.ExpectedIdentifier);
        NameDeclaration name = this.ParseNameDeclaration();
        CSharpGenericTypeParameterDeclaration/*?*/ applicableParameter = null;
        foreach (Ast.GenericTypeParameterDeclaration genericTypeParameter in genericTypeParameters)
          if (genericTypeParameter.Name.UniqueKey == name.UniqueKey) {
            //^ assume genericTypeParameter is CSharpGenericTypeParameterDeclaration; //follows from precondition
            applicableParameter = (CSharpGenericTypeParameterDeclaration)genericTypeParameter; 
            break; 
          }
        if (applicableParameter == null) {
          this.HandleError(name.SourceLocation, Error.TyVarNotFoundInConstraint);
          applicableParameter = new CSharpGenericTypeParameterDeclaration(null, name, (ushort)genericTypeParameters.Count);
        }
        this.Skip(Token.Colon);
        this.ParseGenericTypeParameterConstraints(applicableParameter, followers|Token.Where);
      }
      this.SkipTo(followers);
    }

    private void ParseGenericTypeParameterConstraints(CSharpGenericTypeParameterDeclaration applicableParameter, TokenSet followers)
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      TokenSet constraintStart = Parser.IdentifierOrNonReservedKeyword|Token.Struct|Token.Class|Token.New;
      if (!constraintStart[this.currentToken]) {
        this.HandleError(Error.TypeExpected);
        this.SkipTo(followers);
        return;
      }
      if (this.currentToken == Token.Class) {
        this.GetNextToken();
        applicableParameter.MustBeReferenceType = true;
      } else if (this.currentToken == Token.Struct) {
        this.GetNextToken();
        applicableParameter.MustBeValueType = true;
      }
      while (constraintStart[this.currentToken]) {
        if (this.currentToken == Token.Class || this.currentToken == Token.Struct) {
          this.HandleError(Error.RefValBoundMustBeFirst);
          this.GetNextToken(); continue;
        } else if (this.currentToken == Token.New) {
          this.GetNextToken();
          this.Skip(Token.LeftParenthesis);
          this.Skip(Token.RightParenthesis);
          applicableParameter.MustHaveDefaultConstructor = true;
          if (this.currentToken == Token.Comma) {
            this.HandleError(Error.NewBoundMustBeLast);
          }
          break;
        }
        //^ assume this.currentToken != Token.EndOfFile;
        applicableParameter.AddConstraint(this.ParseTypeExpression(false, false, constraintStart|Token.Comma|followers));
        if (this.currentToken != Token.Comma) break;
        this.GetNextToken();
      }
      this.SkipTo(followers);
    }

    private void ParseRestOfTypeDeclaration(SourceLocationBuilder sctx, TypeDeclaration type, List<Ast.GenericTypeParameterDeclaration> genericParameters,
      List<TypeExpression> baseTypes, List<ITypeDeclarationMember> members, TokenSet followers)
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      this.ParseGenericTypeParameters(genericParameters, followers|Token.Colon|Token.LeftBrace|Token.Where);
      this.ParseBaseTypes(baseTypes, followers|Token.LeftBrace|Token.Where);
      this.ParseGenericTypeParameterConstraintsClauses(genericParameters, followers|Token.LeftBrace);
      this.Skip(Token.LeftBrace);
      this.ParseTypeMembers(sctx, type.Name, members, followers|Token.RightBrace);
      ISourceLocation tokLoc = this.scanner.SourceLocationOfLastScannedToken;
      //^ assume tokLoc.SourceDocument == sctx.SourceDocument;
      sctx.UpdateToSpan(tokLoc);
      this.Skip(Token.RightBrace);
      if (this.currentToken == Token.Semicolon)
        this.GetNextToken();
      this.SkipTo(followers);
    }

    private void ParseRestOfEnum(SourceLocationBuilder sctx, TypeDeclaration type, List<ITypeDeclarationMember> members, TokenSet followers)
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      TypeExpression texpr = new NamedTypeExpression(new SimpleName(type.Name, type.Name.SourceLocation, false));
      this.Skip(Token.LeftBrace);
      while (this.currentToken == Token.LeftBracket || Parser.IdentifierOrNonReservedKeyword[this.currentToken]){
        this.ParseEnumMember(texpr, members, followers|Token.Comma|Token.RightBrace);
        if (this.currentToken == Token.RightBrace) break;
        this.Skip(Token.Comma);
        if (this.currentToken == Token.RightBrace) break;
      }
      sctx.UpdateToSpan(this.scanner.SourceLocationOfLastScannedToken);
      this.Skip(Token.RightBrace);
      if (this.currentToken == Token.Semicolon)
        this.GetNextToken();
      this.SkipTo(followers);
    }

    private void ParseEnumMember(TypeExpression typeExpression, List<ITypeDeclarationMember> members, TokenSet followers)
      //^ requires this.currentToken == Token.LeftBracket || Parser.IdentifierOrNonReservedKeyword[this.currentToken];
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      SourceLocationBuilder sctx = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
      List<SourceCustomAttribute>/*?*/ attributes = this.ParseAttributes(followers|Parser.AttributeOrTypeDeclarationStart|Parser.IdentifierOrNonReservedKeyword);
      if (!Parser.IdentifierOrNonReservedKeyword[this.currentToken])
        this.HandleError(Error.ExpectedIdentifier);
      NameDeclaration name = this.ParseNameDeclaration();
      Expression/*?*/ initializer = null;
      if (this.currentToken == Token.Assign) {
        this.GetNextToken();
        initializer = this.ParseExpression(followers);
      }
      EnumMember member = new EnumMember(attributes, typeExpression, name, initializer, sctx);
      members.Add(member);
      this.SkipTo(followers);
    }

    internal ITypeDeclarationMember/*?*/ ParseTypeDeclarationMember(IName typeName)
      //^ ensures result == null || result is TypeDeclarationMember || result is NestedTypeDeclaration;
    {
      //^ assume this.currentToken != Token.EndOfFile; //assume this method is called directly after construction and then never again.
      //TODO: special treatment for enum members
      this.GetNextToken();
      SourceLocationBuilder slb = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
      List<ITypeDeclarationMember> members = new List<ITypeDeclarationMember>(1);
      this.ParseTypeMembers(slb, typeName, members, Parser.EndOfFile|Parser.TypeMemberStart|Token.RightBrace|Parser.AttributeOrNamespaceOrTypeDeclarationStart);
      if (members.Count != 1 || this.currentToken != Token.EndOfFile) return null;
      ITypeDeclarationMember result = members[0];
      //^ assume result is TypeDeclarationMember || result is NestedTypeDeclaration; //TODO: obtain this from post condition of ParseTypeMembers
      return result;
    }

    private void ParseTypeMembers(SourceLocationBuilder sctx, IName typeName, List<ITypeDeclarationMember> members, TokenSet followers)
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      TokenSet followersOrTypeMemberStart = followers|Parser.TypeMemberStart;
      for (; ; ) {
        SourceLocationBuilder tctx = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
        List<SourceCustomAttribute>/*?*/ attributes = this.ParseAttributes(followersOrTypeMemberStart);
        List<ModifierToken> modifiers = this.ParseModifiers();
        switch (this.currentToken) {
          case Token.Class:
            this.ParseNestedClassDeclaration(members, attributes, this.ConvertToTypeDeclarationFlags(modifiers), tctx, followersOrTypeMemberStart);
            break;
          case Token.Interface:
            this.ParseNestedInterfaceDeclaration(members, attributes, this.ConvertToTypeDeclarationFlags(modifiers), tctx, followersOrTypeMemberStart);
            break;
          case Token.Struct:
            this.ParseNestedStructDeclaration(members, attributes, this.ConvertToTypeDeclarationFlags(modifiers), tctx, followersOrTypeMemberStart);
            break;
          case Token.Enum:
            this.ParseNestedEnumDeclaration(members, attributes, this.ConvertToTypeDeclarationFlags(modifiers), tctx, followersOrTypeMemberStart);
            break;
          case Token.Delegate:
            this.ParseNestedDelegateDeclaration(members, attributes, this.ConvertToTypeDeclarationFlags(modifiers), tctx, followersOrTypeMemberStart);
            break;
          case Token.Const:
            this.ParseConst(members, attributes, modifiers, tctx, followersOrTypeMemberStart);
            break;
          case Token.Invariant:
            goto default;
            //this.ParseInvariant(attributes, modifierTokens, modifierContexts, tctx, followersOrTypeMemberStart);
            //break;
          case Token.Bool:
          case Token.Decimal:
          case Token.Sbyte:
          case Token.Byte:
          case Token.Short:
          case Token.Ushort:
          case Token.Int:
          case Token.Uint:
          case Token.Long:
          case Token.Ulong:
          case Token.Char:
          case Token.Float:
          case Token.Double:
          case Token.Object:
          case Token.String:
          case Token.Void:
          case Token.Identifier:
            this.ParseConstructorOrFieldOrMethodOrPropertyOrStaticInitializer(typeName, members, attributes, modifiers, tctx, followersOrTypeMemberStart);
            break;
          case Token.Event:
            this.ParseEvent(members, attributes, modifiers, tctx, followersOrTypeMemberStart); 
            break;
          case Token.Operator:
          case Token.Explicit:
          case Token.Implicit:
            this.ParseOperator(members, attributes, modifiers, null, tctx, followersOrTypeMemberStart); 
            break;
          case Token.BitwiseNot:
            this.ParseDestructor(typeName, members, attributes, modifiers, tctx, followersOrTypeMemberStart); 
            break;
          default:
            if (Parser.IdentifierOrNonReservedKeyword[this.currentToken]) goto case Token.Identifier;
            sctx.UpdateToSpan(this.scanner.SourceLocationOfLastScannedToken);
            this.SkipTo(followers);
            return;
        }
      }
    }

    private void ParseEvent(List<ITypeDeclarationMember> members, 
      List<SourceCustomAttribute>/*?*/ attributes, List<ModifierToken> modifiers, SourceLocationBuilder sctx, TokenSet followers)
      //^ requires this.currentToken == Token.Event;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      this.GetNextToken();
      EventDeclaration.Flags flags = 0;
      TypeMemberVisibility visibility = this.ConvertToTypeMemberVisibility(modifiers);
      if (this.LookForModifier(modifiers, Token.Abstract, Error.None)) flags |= EventDeclaration.Flags.Abstract;
      if (this.LookForModifier(modifiers, Token.New, Error.None)) flags |= EventDeclaration.Flags.New;
      if (this.LookForModifier(modifiers, Token.Extern, Error.None)) flags |= EventDeclaration.Flags.External;
      if (this.LookForModifier(modifiers, Token.Override, Error.None)) flags |= EventDeclaration.Flags.Override;
      this.LookForModifier(modifiers, Token.Readonly, Error.InvalidModifier);
      if (this.LookForModifier(modifiers, Token.Sealed, Error.None)) flags |= EventDeclaration.Flags.Sealed;
      if (this.LookForModifier(modifiers, Token.Static, Error.None)) flags |= EventDeclaration.Flags.Static;
      if (this.LookForModifier(modifiers, Token.Unsafe, Error.None)) flags |= EventDeclaration.Flags.Unsafe;
      if (this.LookForModifier(modifiers, Token.Virtual, Error.None)) flags |= EventDeclaration.Flags.Virtual;
      this.LookForModifier(modifiers, Token.Volatile, Error.InvalidModifier);


      TokenSet followersOrCommaOrSemiColon = followers|Token.Comma|Token.Semicolon;
      TypeExpression type = this.ParseTypeExpression(false, false, followersOrCommaOrSemiColon|Parser.IdentifierOrNonReservedKeyword|Token.Assign);

      for (bool firstTime = true; ; firstTime = false){
        if (!Parser.IdentifierOrNonReservedKeyword[this.currentToken])
          this.SkipTo(followers|Parser.IdentifierOrNonReservedKeyword|Token.LeftBrace|Token.Dot|Token.LessThan|Token.Assign|Token.Comma, Error.ExpectedIdentifier);
        NameDeclaration name = this.ParseNameDeclaration();
        if (firstTime && (this.currentToken == Token.LeftBrace || this.currentToken == Token.Dot || this.currentToken == Token.LessThan)) {
          this.ParseEventWithAccessors(members, attributes, modifiers, type, name, sctx, followers);
          return;
        }
        
        Expression/*?*/ initializer = null;
        if (this.currentToken == Token.Assign){
          this.GetNextToken();
          initializer = this.ParseExpression(followersOrCommaOrSemiColon);
        }
        SourceLocationBuilder ctx = new SourceLocationBuilder(sctx.GetSourceLocation());
        if (initializer != null)
          ctx.UpdateToSpan(initializer.SourceLocation);
        else
          ctx.UpdateToSpan(name.SourceLocation);
        EventDeclaration e = new EventDeclaration(attributes, flags, visibility, type, name, initializer, ctx);
        members.Add(e);
        if (this.currentToken != Token.Comma) break;
        this.GetNextToken();
      }
      this.SkipSemiColon(followers);
      this.SkipTo(followers);
    }

    private void ParseEventWithAccessors(List<ITypeDeclarationMember> members, 
      List<SourceCustomAttribute>/*?*/ attributes, List<ModifierToken> modifiers, TypeExpression type, NameDeclaration name, SourceLocationBuilder sctx, TokenSet followers)
      //^ requires this.currentToken == Token.Dot || this.currentToken == Token.LessThan || this.currentToken == Token.LeftBrace;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      EventDeclaration.Flags flags = 0;
      TypeMemberVisibility visibility = this.ConvertToTypeMemberVisibility(modifiers);
      if (this.LookForModifier(modifiers, Token.Abstract, Error.None)) flags |= EventDeclaration.Flags.Abstract;
      if (this.LookForModifier(modifiers, Token.New, Error.None)) flags |= EventDeclaration.Flags.New;
      if (this.LookForModifier(modifiers, Token.Extern, Error.None)) flags |= EventDeclaration.Flags.External;
      if (this.LookForModifier(modifiers, Token.Override, Error.None)) flags |= EventDeclaration.Flags.Override;
      this.LookForModifier(modifiers, Token.Readonly, Error.InvalidModifier);
      if (this.LookForModifier(modifiers, Token.Sealed, Error.None)) flags |= EventDeclaration.Flags.Sealed;
      if (this.LookForModifier(modifiers, Token.Static, Error.None)) flags |= EventDeclaration.Flags.Static;
      if (this.LookForModifier(modifiers, Token.Unsafe, Error.None)) flags |= EventDeclaration.Flags.Unsafe;
      if (this.LookForModifier(modifiers, Token.Virtual, Error.None)) flags |= EventDeclaration.Flags.Virtual;
      this.LookForModifier(modifiers, Token.Volatile, Error.InvalidModifier);

      List<TypeExpression>/*?*/ implementedInterfaces = null;
      Expression implementedInterface = this.ParseImplementedInterfacePlusName(ref name, false, followers);
      QualifiedName/*?*/ qual = implementedInterface as QualifiedName;
      if (qual != null) {
        implementedInterfaces = new List<TypeExpression>(1);
        GenericTypeInstanceExpression/*?*/ gte = qual.Qualifier as GenericTypeInstanceExpression;
        if (gte != null)
          implementedInterfaces.Add(gte);
        else {
          //^ assume qual.Qualifier is SimpleName || qual.Qualifier is QualifiedName; //follows from post condition of ParseImplementedInterfacePlusName
          implementedInterfaces.Add(new NamedTypeExpression(qual.Qualifier));
        }
      }

      if (this.currentToken == Token.LeftBrace) 
        this.GetNextToken();
      else{
        Error e = Error.ExpectedLeftBrace;
        if (implementedInterfaces != null) e = Error.ExplicitEventFieldImpl;
        this.SkipTo(followers|Token.LeftBracket|Token.Add|Token.Remove|Token.RightBrace, e);
      }

      TokenSet followersOrRightBrace = followers|Token.RightBrace;
      List<SourceCustomAttribute>/*?*/ adderAttributes = null;
      BlockStatement/*?*/ adderBody = null;
      List<SourceCustomAttribute>/*?*/ removerAttributes = null;
      BlockStatement/*?*/ removerBody = null;
      bool alreadyComplainedAboutModifier = false;
      for (; ; ) {
        List<SourceCustomAttribute>/*?*/ accessorAttrs = this.ParseAttributes(followers|Parser.AddOrRemoveOrModifier|Token.LeftBrace);
        switch (this.currentToken) {
          case Token.Add:
            adderAttributes = accessorAttrs;
            if (adderBody != null)
              this.HandleError(Error.DuplicateAccessor);
            this.GetNextToken();
            if (this.currentToken != Token.LeftBrace) {
              this.SkipTo(followersOrRightBrace|Token.Remove, Error.AddRemoveMustHaveBody);
            } else
              adderBody = this.ParseBody(followersOrRightBrace|Token.Remove);
            continue;
          case Token.Remove:
            removerAttributes = accessorAttrs;
            if (removerBody != null)
              this.HandleError(Error.DuplicateAccessor);
            this.GetNextToken();
            if (this.currentToken != Token.LeftBrace) {
              this.SkipTo(followersOrRightBrace|Token.Remove, Error.AddRemoveMustHaveBody);
              removerBody = null;
            } else
              removerBody = this.ParseBody(followersOrRightBrace|Token.Add);
            continue;
          case Token.New:
          case Token.Public:
          case Token.Protected:
          case Token.Internal:
          case Token.Private:
          case Token.Abstract:
          case Token.Sealed:
          case Token.Static:
          case Token.Readonly:
          case Token.Volatile:
          case Token.Virtual:
          case Token.Override:
          case Token.Extern:
          case Token.Unsafe:
            if (!alreadyComplainedAboutModifier)
              this.HandleError(Error.NoModifiersOnAccessor);
            this.GetNextToken();
            alreadyComplainedAboutModifier = true;
            continue;
          default:
            this.HandleError(Error.AddOrRemoveExpected);
            break;
        }
        break;
      }
      if (adderBody == null)
        adderBody = null;
      if (removerBody == null)
        removerBody = null;
      EventDeclaration evnt = new EventDeclaration(attributes, flags, visibility, type, implementedInterfaces, name, 
        adderAttributes, adderBody, removerAttributes, removerBody, sctx.GetSourceLocation());
      members.Add(evnt);
      this.SkipOverTo(Token.RightBrace, followers);
    }

    private Expression ParseImplementedInterfacePlusName(ref NameDeclaration name, bool allowThis, TokenSet followers)
      //^ ensures result is SimpleName || result is QualifiedName;
      //^ ensures result is QualifiedName ==> (((QualifiedName)result).Qualifier is SimpleName || 
      //^ ((QualifiedName)result).Qualifier is QualifiedName || ((QualifiedName)result).Qualifier is GenericTypeInstanceExpression);
    {
      Expression implementedInterface = new SimpleName(name, name.SourceLocation, false);
      while (this.currentToken == Token.Dot || this.currentToken == Token.LessThan) 
        //^ invariant implementedInterface is SimpleName || implementedInterface is QualifiedName;

        //The following invariant does not hold, it seems. Fix it.
        //^ invariant implementedInterface is QualifiedName ==> (((QualifiedName)implementedInterface).Qualifier is SimpleName || 
        //^ ((QualifiedName)implementedInterface).Qualifier is QualifiedName || ((QualifiedName)implementedInterface).Qualifier is GenericTypeInstanceExpression);
      {
        if (this.currentToken == Token.LessThan) {
          //^ assume implementedInterface is SimpleName || implementedInterface is QualifiedName;
          TypeExpression genericType = new NamedTypeExpression(implementedInterface);
          SourceLocationBuilder ctx = new SourceLocationBuilder(implementedInterface.SourceLocation);
          List<TypeExpression> typeArguments = this.ParseTypeArguments(ctx, false, followers|Token.Dot|Token.LeftBrace);
          implementedInterface = new GenericTypeInstanceExpression(genericType, typeArguments, ctx);
        }
        this.Skip(Token.Dot);
        if (this.currentToken == Token.This && allowThis) {
          name = this.GetNameDeclarationFor("Item", this.scanner.SourceLocationOfLastScannedToken);
        } else {
          if (!Parser.IdentifierOrNonReservedKeyword[this.currentToken])
            this.SkipTo(followers|Parser.IdentifierOrNonReservedKeyword|Token.LeftBrace, Error.ExpectedIdentifier);
          name = this.ParseNameDeclaration();
        }
        SourceLocationBuilder ctx1 = new SourceLocationBuilder(implementedInterface.SourceLocation);
        ctx1.UpdateToSpan(name.SourceLocation);
        implementedInterface = new QualifiedName(implementedInterface, new SimpleName(name, name.SourceLocation, false), ctx1);
      }
      return implementedInterface;
    }

    private void ParseConst(List<ITypeDeclarationMember> members, 
      List<SourceCustomAttribute>/*?*/ attributes, List<ModifierToken> modifiers, SourceLocationBuilder sctx, TokenSet followers) 
      //^ requires this.currentToken == Token.Const;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      this.GetNextToken();
      TypeMemberVisibility visibility = this.ConvertToTypeMemberVisibility(modifiers);
      this.LookForModifier(modifiers, Token.Abstract, Error.InvalidModifier);
      this.LookForModifier(modifiers, Token.Extern, Error.InvalidModifier);
      bool isNew = this.LookForModifier(modifiers, Token.New, Error.None);
      this.LookForModifier(modifiers, Token.Override, Error.InvalidModifier);
      this.LookForModifier(modifiers, Token.Readonly, Error.InvalidModifier);
      this.LookForModifier(modifiers, Token.Sealed, Error.InvalidModifier);
      this.LookForModifier(modifiers, Token.Static, Error.StaticConstant);
      this.LookForModifier(modifiers, Token.Unsafe, Error.InvalidModifier);
      this.LookForModifier(modifiers, Token.Virtual, Error.InvalidModifier);
      this.LookForModifier(modifiers, Token.Volatile, Error.InvalidModifier);

      TokenSet followersOrCommaOrSemiColon = followers|Token.Comma|Token.Semicolon;
      TypeExpression type = this.ParseTypeExpression(false, false, followersOrCommaOrSemiColon|Parser.IdentifierOrNonReservedKeyword|Token.Assign);

      for (;;){
        if (!Parser.IdentifierOrNonReservedKeyword[this.currentToken])
          this.SkipTo(followers|Parser.IdentifierOrNonReservedKeyword|Token.Assign|Token.Comma|Token.Semicolon, Error.ExpectedIdentifier);
        NameDeclaration name = this.ParseNameDeclaration();
        if (this.currentToken == Token.Assign)
          this.GetNextToken();
        else if (this.currentToken == Token.LeftBrace && type is IInterfaceDeclaration) {
          //might be a mistaken attempt to define a readonly property
          this.HandleError(Error.ConstValueRequired); //TODO: this is as per the C# compiler, but a better message would be nice.
          this.ParseProperty(members, attributes, modifiers, type, null, name, sctx, followers);
          this.SkipOverTo(Token.LeftBrace, followers);
          return;
        } else {
          this.SkipTo(Parser.UnaryStart|followersOrCommaOrSemiColon, Error.ConstValueRequired);
          if (this.currentToken == Token.Comma) {
            this.GetNextToken();
            continue; //Try to parse the next constant declarator
          }
          if (!Parser.UnaryStart[this.currentToken]) {
            this.SkipTo(followers, Error.None);
            return;
          }
        }
        Expression initializer = this.ParseExpression(followersOrCommaOrSemiColon);
        SourceLocationBuilder ctx = new SourceLocationBuilder(sctx.GetSourceLocation());
        ctx.UpdateToSpan(initializer.SourceLocation);
        FieldDeclaration.Flags flags = FieldDeclaration.Flags.Constant|FieldDeclaration.Flags.Static;
        if (isNew) flags |= FieldDeclaration.Flags.New;
        FieldDeclaration f = new FieldDeclaration(attributes, flags, visibility, type, name, initializer, ctx.GetSourceLocation());
        members.Add(f);
        if (this.currentToken != Token.Comma) break;
        this.GetNextToken();
      }
      this.SkipSemiColon(followers);
      this.SkipTo(followers);
    }

    private void ParseDestructor(IName parentTypeName, List<ITypeDeclarationMember> members, 
      List<SourceCustomAttribute>/*?*/ attributes, List<ModifierToken> modifiers, SourceLocationBuilder sctx, TokenSet followers)
      //^ requires this.currentToken == Token.BitwiseNot;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      this.GetNextToken();
      MethodDeclaration.Flags flags = MethodDeclaration.Flags.SpecialName;
      this.LookForModifier(modifiers, Token.Abstract, Error.InvalidModifier);
      this.LookForModifier(modifiers, Token.New, Error.InvalidModifier);
      if (this.LookForModifier(modifiers, Token.Extern, Error.None)) flags |= MethodDeclaration.Flags.External;
      this.LookForModifier(modifiers, Token.Override, Error.InvalidModifier);
      this.LookForModifier(modifiers, Token.Readonly, Error.InvalidModifier);
      this.LookForModifier(modifiers, Token.Sealed, Error.InvalidModifier);
      this.LookForModifier(modifiers, Token.Static, Error.InvalidModifier);
      this.LookForModifier(modifiers, Token.Unsafe, Error.InvalidModifier);
      this.LookForModifier(modifiers, Token.Virtual, Error.InvalidModifier);
      this.LookForModifier(modifiers, Token.Volatile, Error.InvalidModifier);


      if (!Parser.IdentifierOrNonReservedKeyword[this.currentToken])
        this.HandleError(Error.ExpectedIdentifier);
      NameDeclaration name = this.ParseNameDeclaration();
      if (name.UniqueKey != parentTypeName.UniqueKey)
        this.HandleError(Error.WrongNameForDestructor);
      name = new NameDeclaration(this.GetNameFor("Finalize"), name.SourceLocation);
      List<Ast.ParameterDeclaration> parameters = new List<Ast.ParameterDeclaration>(0);
      this.ParseParameters(parameters, Token.RightParenthesis, followers|Token.LeftBrace);
      if (parameters.Count > 0) {
        this.HandleError(parameters[0].SourceLocation, Error.ExpectedRightParenthesis);
        parameters.Clear();
      }
      BlockStatement body = this.ParseBody(followers);
      //^ assert followers[this.currentToken] || this.currentToken == Token.EndOfFile;
      sctx.UpdateToSpan(body.SourceLocation);
      MethodDeclaration method = new MethodDeclaration(attributes, flags, TypeMemberVisibility.Private, 
        this.GetTypeExpressionFor(Token.Void, name.SourceLocation), null, name, null, parameters, null, body, sctx.GetSourceLocation());
      members.Add(method);
      //^ assume followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    }

    private NamedTypeExpression GetTypeExpressionFor(Token tok, ISourceLocation sourceLocation)
      //^ requires tok == Token.Bool || tok == Token.Decimal || tok == Token.Sbyte ||
      //^   tok == Token.Byte || tok == Token.Short || tok == Token.Ushort ||
      //^   tok == Token.Int || tok == Token.Uint || tok == Token.Long ||
      //^   tok == Token.Ulong || tok == Token.Char || tok == Token.Float ||
      //^   tok == Token.Double || tok == Token.Object || tok == Token.String ||
      //^   tok == Token.Void;
    {
      return new NamedTypeExpression(this.RootQualifiedNameFor(tok, sourceLocation));
    }

    private void ParseConstructorOrFieldOrMethodOrPropertyOrStaticInitializer(IName typeName, List<ITypeDeclarationMember> members, 
      List<SourceCustomAttribute>/*?*/ attributes, List<ModifierToken> modifiers, SourceLocationBuilder sctx, TokenSet followers)
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      if (Parser.IdentifierOrNonReservedKeyword[this.currentToken] && this.scanner.GetIdentifierString() == typeName.Value && this.PeekNextToken() == Token.LeftParenthesis) {
        //^ assume Parser.IdentifierOrNonReservedKeyword[this.currentToken]; //TODO: Boogie bug. The above condition ought to do it.
        this.ParseConstructor(members, attributes, modifiers, sctx, followers|Token.Semicolon);
        if (this.currentToken == Token.Semicolon) this.GetNextToken();
        this.SkipTo(followers);
        return;
      }

      SourceLocationBuilder idCtx = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
      TypeExpression type = this.ParseTypeExpression(false, false, followers|Parser.IdentifierOrNonReservedKeyword|Token.Explicit|Token.Implicit);
      bool badModifier = false;
    tryAgain:
      switch (this.currentToken) {
        case Token.This:
          NameDeclaration itemId = this.GetNameDeclarationFor("Item", this.scanner.SourceLocationOfLastScannedToken);
          this.ParseProperty(members, attributes, modifiers, type, null, itemId, sctx, followers);
          return;
        case Token.Explicit:
        case Token.Implicit:
        case Token.Operator:
          this.ParseOperator(members, attributes, modifiers, type, sctx, followers);
          return;
        case Token.New:
        case Token.Public:
        case Token.Protected:
        case Token.Internal:
        case Token.Private:
        case Token.Abstract:
        case Token.Sealed:
        case Token.Static:
        case Token.Readonly:
        case Token.Volatile:
        case Token.Virtual:
        case Token.Override:
        case Token.Extern:
        case Token.Unsafe:
          if (this.scanner.TokenIsFirstAfterLineBreak) break;
          if (!badModifier) {
            this.HandleError(Error.BadModifierLocation, this.scanner.GetTokenSource());
            badModifier = true;
          }
          this.GetNextToken();
          goto tryAgain;
        case Token.LeftParenthesis:
        case Token.LessThan:
          if (type is NamedTypeExpression && ((NamedTypeExpression)type).Expression is SimpleName) {
            this.HandleError(type.SourceLocation, Error.MemberNeedsType);
            NameDeclaration methName = new NameDeclaration(((SimpleName)((NamedTypeExpression)type).Expression).Name, type.SourceLocation);
            this.ParseMethod(members, attributes, modifiers, this.GetTypeExpressionFor(Token.Void, methName.SourceLocation), null, methName, sctx, followers);
            return;
          }
          break;
        default:
          if (!Parser.IdentifierOrNonReservedKeyword[this.currentToken]) {
            if (followers[this.currentToken])
              this.HandleError(Error.ExpectedIdentifier);
            else
              this.SkipTo(followers);
            this.ParseField(members, attributes, modifiers, type, this.GetNameDeclarationFor("", this.scanner.SourceLocationOfLastScannedToken), sctx, followers);
            return;
          }
          break;
      }
      NameDeclaration name = this.ParseNameDeclaration();
      List<TypeExpression>/*?*/ implementedInterfaces = null;
      if (this.currentToken != Token.LessThan || !this.AtMethodTypeParameterList()) {
        Expression implementedInterface = this.ParseImplementedInterfacePlusName(ref name, true, followers|Token.LeftBrace|Token.LeftBracket|Token.LeftParenthesis|Token.LessThan);
        QualifiedName/*?*/ qual = implementedInterface as QualifiedName;
        if (qual != null) {
          implementedInterfaces = new List<TypeExpression>(1);
          GenericTypeInstanceExpression/*?*/ genInst = qual.Qualifier as GenericTypeInstanceExpression;
          if (genInst != null)
            implementedInterfaces.Add(genInst);
          else
            implementedInterfaces.Add(new NamedTypeExpression(qual.Qualifier));
        }
      }
      //if (badModifier) name.SourceContext.Document = null; //suppress any further errors involving this member
      switch (this.currentToken) {
        case Token.LeftBrace:
        case Token.This:
          this.ParseProperty(members, attributes, modifiers, type, implementedInterfaces, name, sctx, followers);
          return;
        case Token.LeftParenthesis:
        case Token.LessThan:
          this.ParseMethod(members, attributes, modifiers, type, implementedInterfaces, name, sctx, followers);
          return;
        default:
          if (implementedInterfaces != null)
            this.ParseMethod(members, attributes, modifiers, type, implementedInterfaces, name, sctx, followers);
          else
            this.ParseField(members, attributes, modifiers, type, name, sctx, followers);
          return;
      }
    }

    private void ParseConstructor(List<ITypeDeclarationMember> members, List<SourceCustomAttribute>/*?*/ attributes, List<ModifierToken> modifiers, SourceLocationBuilder sctx, TokenSet followers) 
      //^ requires Parser.IdentifierOrNonReservedKeyword[this.currentToken];
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      MethodDeclaration.Flags flags = MethodDeclaration.Flags.SpecialName;
      TypeMemberVisibility visibility = this.ConvertToTypeMemberVisibility(modifiers);
      this.LookForModifier(modifiers, Token.Abstract, Error.InvalidModifier);
      this.LookForModifier(modifiers, Token.New, Error.InvalidModifier);
      this.LookForModifier(modifiers, Token.Extern, Error.InvalidModifier);
      this.LookForModifier(modifiers, Token.Override, Error.InvalidModifier);
      this.LookForModifier(modifiers, Token.Readonly, Error.InvalidModifier);
      this.LookForModifier(modifiers, Token.Sealed, Error.InvalidModifier);
      this.LookForModifier(modifiers, Token.Static, Error.InvalidModifier);
      if (this.LookForModifier(modifiers, Token.Unsafe, Error.None)) flags |= MethodDeclaration.Flags.Unsafe;
      this.LookForModifier(modifiers, Token.Virtual, Error.InvalidModifier);
      this.LookForModifier(modifiers, Token.Volatile, Error.InvalidModifier);

      NameDeclaration name = new NameDeclaration(this.nameTable.Ctor, this.scanner.SourceLocationOfLastScannedToken);
      //^ assume this.currentToken != Token.EndOfFile; //follows from the precondition
      this.GetNextToken();

      List<Ast.ParameterDeclaration> parameters = new List<Ast.ParameterDeclaration>();
      List<Statement> statements = new List<Statement>();
      SourceLocationBuilder bodyCtx = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
      BlockStatement body = new BlockStatement(statements, bodyCtx);
      MethodDeclaration constructor = new MethodDeclaration(attributes, flags, 
        visibility, new NamedTypeExpression(this.RootQualifiedNameFor(Token.Void)), null, name, null, parameters, null, body, sctx);
      members.Add(constructor);
      this.ParseParameters(parameters, Token.RightParenthesis, followers|Token.Where|Token.LeftBrace|Token.Semicolon|Token.Colon);
      SourceLocationBuilder callCtx = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
      Token kind = Token.Base;
      IEnumerable<Expression> arguments = Expression.EmptyCollection;
      if (this.currentToken == Token.Colon) {
        this.GetNextToken();
        callCtx = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
        if (this.currentToken == Token.This) {
          kind = Token.This;
          this.GetNextToken();
          if (this.currentToken == Token.LeftParenthesis)
            arguments = this.ParseArgumentList(callCtx, followers).AsReadOnly();
          else {
            callCtx.UpdateToSpan(this.scanner.SourceLocationOfLastScannedToken);
            this.SkipTo(followers|Token.LeftBrace, Error.ExpectedLeftParenthesis);
          }
        } else if (this.currentToken == Token.Base) {
          this.GetNextToken();
          if (this.currentToken == Token.LeftParenthesis)
            arguments = this.ParseArgumentList(callCtx, followers).AsReadOnly();
          else {
            callCtx.UpdateToSpan(this.scanner.SourceLocationOfLastScannedToken);
            this.SkipTo(followers|Token.LeftBrace, Error.ExpectedLeftParenthesis);
          }
        } else {
          callCtx.UpdateToSpan(this.scanner.SourceLocationOfLastScannedToken);
          this.SkipTo(followers|Token.LeftBrace, Error.ThisOrBaseExpected);
        }
      }
      if (kind == Token.Base) {
        statements.Add(new FieldInitializerStatement());
        statements.Add(new ExpressionStatement(new BaseClassConstructorCall(arguments, callCtx)));
      } else
        statements.Add(new ExpressionStatement(new ChainedConstructorCall(arguments, callCtx)));
      this.ParseBody(statements, bodyCtx, followers);
      //^ assert followers[this.currentToken] || this.currentToken == Token.EndOfFile;
      sctx.UpdateToSpan(bodyCtx.GetSourceLocation());
      //^ assume followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
    private bool AtMethodTypeParameterList() {
      return true;
      //TODO: save back up point, then try to parse type parameter list
    }

    private void ParseField(List<ITypeDeclarationMember> members, 
      List<SourceCustomAttribute>/*?*/ attributes, List<ModifierToken> modifiers, TypeExpression type, NameDeclaration name, SourceLocationBuilder sctx, TokenSet followers)
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      TypeMemberVisibility visibility = this.ConvertToTypeMemberVisibility(modifiers);
      FieldDeclaration.Flags flags = 0;
      this.LookForModifier(modifiers, Token.Abstract, Error.InvalidModifier);
      if (this.LookForModifier(modifiers, Token.New, Error.None)) flags |= FieldDeclaration.Flags.New;
      this.LookForModifier(modifiers, Token.Extern, Error.InvalidModifier);
      this.LookForModifier(modifiers, Token.Override, Error.InvalidModifier);
      if (this.LookForModifier(modifiers, Token.Readonly, Error.None)) flags |= FieldDeclaration.Flags.ReadOnly;
      this.LookForModifier(modifiers, Token.Sealed, Error.InvalidModifier);
      if (this.LookForModifier(modifiers, Token.Static, Error.None)) flags |= FieldDeclaration.Flags.Static;
      if (this.LookForModifier(modifiers, Token.Unsafe, Error.None)) flags |= FieldDeclaration.Flags.Unsafe;
      this.LookForModifier(modifiers, Token.Virtual, Error.InvalidModifier);
      if (this.LookForModifier(modifiers, Token.Volatile, Error.None)) flags |= FieldDeclaration.Flags.Volatile;

      TokenSet followersOrCommaOrSemicolon = followers|Token.Comma|Token.Semicolon;
      for (; ; ) {
        Expression/*?*/ initializer = null;
        if (this.currentToken == Token.Assign) {
          //bool savedParsingStatement = this.parsingStatement;
          //this.parsingStatement = true;
          this.GetNextToken();
          if (this.currentToken == Token.LeftBrace) {
            //initializer = this.ParseArrayInitializer(type, followersOrCommaOrSemicolon);
          } else
            initializer = this.ParseExpression(followersOrCommaOrSemicolon);
          //if (this.currentToken != Token.EndOfFile) this.parsingStatement = savedParsingStatement;
        }
        sctx.UpdateToSpan(this.scanner.SourceLocationOfLastScannedToken);
        FieldDeclaration f = new FieldDeclaration(attributes, flags, visibility, type, name, initializer, sctx);
        members.Add(f);
        if (this.currentToken != Token.Comma) break;
        this.GetNextToken();
        name = this.ParseNameDeclaration();
        sctx = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
      }
      this.SkipSemiColon(followers);
    }

    private void ParseMethod(List<ITypeDeclarationMember> members, 
      List<SourceCustomAttribute>/*?*/ attributes, List<ModifierToken> modifiers, TypeExpression type, 
      List<TypeExpression>/*?*/ implementedInterfaces, NameDeclaration name, SourceLocationBuilder sctx, TokenSet followers)
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      MethodDeclaration.Flags flags = 0;
      TypeMemberVisibility visibility = this.ConvertToTypeMemberVisibility(modifiers);
      if (this.LookForModifier(modifiers, Token.Abstract, Error.None)) flags |= MethodDeclaration.Flags.Abstract;
      if (this.LookForModifier(modifiers, Token.New, Error.None)) flags |= MethodDeclaration.Flags.New;
      if (this.LookForModifier(modifiers, Token.Extern, Error.None)) flags |= MethodDeclaration.Flags.External;
      if (this.LookForModifier(modifiers, Token.Override, Error.None)) flags |= MethodDeclaration.Flags.Override;
      this.LookForModifier(modifiers, Token.Readonly, Error.InvalidModifier);
      if (this.LookForModifier(modifiers, Token.Sealed, Error.None)) flags |= MethodDeclaration.Flags.Sealed;
      if (this.LookForModifier(modifiers, Token.Static, Error.None)) flags |= MethodDeclaration.Flags.Static;
      if (this.LookForModifier(modifiers, Token.Unsafe, Error.None)) flags |= MethodDeclaration.Flags.Unsafe;
      if (this.LookForModifier(modifiers, Token.Virtual, Error.None)) flags |= MethodDeclaration.Flags.Virtual;
      this.LookForModifier(modifiers, Token.Volatile, Error.InvalidModifier);

      List<Ast.GenericMethodParameterDeclaration> genericParameters = new List<Ast.GenericMethodParameterDeclaration>();
      List<Ast.ParameterDeclaration> parameters = new List<Ast.ParameterDeclaration>();
      List<Statement> statements = new List<Statement>();
      SourceLocationBuilder bodyCtx = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
      BlockStatement body = new BlockStatement(statements, bodyCtx);
      MethodDeclaration method = new MethodDeclaration(attributes, flags, visibility, 
        type, implementedInterfaces, name, genericParameters, parameters, null, body, sctx);
      members.Add(method);

      this.ParseGenericMethodParameters(genericParameters, followers|Token.LeftParenthesis|Token.Where|Token.LeftBrace|Token.Semicolon);
      this.ParseParameters(parameters, Token.RightParenthesis, followers|Token.Where|Token.LeftBrace|Token.Semicolon);
      this.ParseGenericMethodParameterConstraintsClauses(genericParameters, followers|Token.LeftBrace|Token.Semicolon);
      //bool swallowedSemicolonAlready = false;
      //this.ParseMethodContract(oper, followers|Token.LeftBrace|Token.Semicolon, ref swallowedSemicolonAlready);
      this.ParseBody(statements, bodyCtx, followers);
      //^ assert followers[this.currentToken] || this.currentToken == Token.EndOfFile;
      sctx.UpdateToSpan(bodyCtx.GetSourceLocation());
      //^ assume followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    }

    private void ParseGenericMethodParameters(List<Ast.GenericMethodParameterDeclaration> genericParameters, TokenSet followers) {
      if (this.currentToken != Token.LessThan) return;
      this.GetNextToken();
      while (this.currentToken != Token.GreaterThan && this.currentToken != Token.Colon && this.currentToken != Token.LeftBrace && this.currentToken != Token.EndOfFile) {
        List<SourceCustomAttribute>/*?*/ attributes = this.ParseAttributes(followers|Parser.IdentifierOrNonReservedKeyword);
        if (!Parser.IdentifierOrNonReservedKeyword[this.currentToken])
          this.HandleError(Error.ExpectedIdentifier);
        NameDeclaration name = this.ParseNameDeclaration();
        genericParameters.Add(new CSharpGenericMethodParameterDeclaration(attributes, name, (ushort)genericParameters.Count));
        if (this.currentToken != Token.Comma) break;
        this.GetNextToken();
      }
      if (this.currentToken == Token.RightShift)
        this.currentToken = Token.GreaterThan;
      else
        this.SkipOverTo(Token.GreaterThan, followers);
    }

    private void ParseGenericMethodParameterConstraintsClauses(List<Ast.GenericMethodParameterDeclaration> genericMethodParameters, TokenSet followers)
      // ^ requires forall{Ast.GenericMethodParameterDeclaration genericMethodParameter in genericMethodParameters; genericMethodParameter is CSharpGenericMethodParameterDeclaration};
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      while (this.currentToken == Token.Where) {
        this.GetNextToken();
        if (!Parser.IdentifierOrNonReservedKeyword[this.currentToken])
          this.HandleError(Error.ExpectedIdentifier);
        NameDeclaration name = this.ParseNameDeclaration();
        CSharpGenericMethodParameterDeclaration/*?*/ applicableParameter = null;
        foreach (Ast.GenericMethodParameterDeclaration genericMethodParameter in genericMethodParameters)
          if (genericMethodParameter.Name.UniqueKey == name.UniqueKey) {
            //^ assume genericMethodParameter is CSharpGenericMethodParameterDeclaration; //follows from precondition
            applicableParameter = (CSharpGenericMethodParameterDeclaration)genericMethodParameter; 
            break; 
          }
        if (applicableParameter == null) {
          this.HandleError(name.SourceLocation, Error.TyVarNotFoundInConstraint);
          applicableParameter = new CSharpGenericMethodParameterDeclaration(null, name, (ushort)genericMethodParameters.Count);
        }
        this.Skip(Token.Colon);
        this.ParseGenericMethodParameterConstraints(applicableParameter, followers|Token.Where);
      }
      this.SkipTo(followers);
    }

    private void ParseGenericMethodParameterConstraints(CSharpGenericMethodParameterDeclaration applicableParameter, TokenSet followers)
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      TokenSet constraintStart = Parser.IdentifierOrNonReservedKeyword|Token.Struct|Token.Class|Token.New;
      if (!constraintStart[this.currentToken]) {
        this.HandleError(Error.TypeExpected);
        this.SkipTo(followers);
        return;
      }
      if (this.currentToken == Token.Class) {
        this.GetNextToken();
        applicableParameter.MustBeReferenceType = true;
      } else if (this.currentToken == Token.Struct) {
        this.GetNextToken();
        applicableParameter.MustBeValueType = true;
      }
      while (constraintStart[this.currentToken]) {
        //^ assume this.currentToken != Token.EndOfFile;
        if (this.currentToken == Token.Class || this.currentToken == Token.Struct) {
          this.HandleError(Error.RefValBoundMustBeFirst);
          this.GetNextToken(); continue;
        } else if (this.currentToken == Token.New) {
          this.GetNextToken();
          this.Skip(Token.LeftParenthesis);
          this.Skip(Token.RightParenthesis);
          applicableParameter.MustHaveDefaultConstructor = true;
          if (this.currentToken == Token.Comma) {
            this.HandleError(Error.NewBoundMustBeLast);
          }
          break;
        }
        applicableParameter.AddConstraint(this.ParseTypeExpression(false, false, constraintStart|Token.Comma|followers));
        if (this.currentToken != Token.Comma) break;
        this.GetNextToken();
      }
      this.SkipTo(followers);
    }

    private void ParseOperator(List<ITypeDeclarationMember> members, 
      List<SourceCustomAttribute>/*?*/ attributes, List<ModifierToken> modifiers, TypeExpression/*?*/ resultType, SourceLocationBuilder sctx, TokenSet followers)
      //^ requires this.currentToken == Token.Explicit || this.currentToken == Token.Implicit || this.currentToken == Token.Operator;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      MethodDeclaration.Flags flags = MethodDeclaration.Flags.SpecialName;
      TypeMemberVisibility visibility = this.ConvertToTypeMemberVisibility(modifiers);
      this.LookForModifier(modifiers, Token.Abstract, Error.InvalidModifier);
      if (this.LookForModifier(modifiers, Token.Extern, Error.None)) flags |= MethodDeclaration.Flags.External;
      this.LookForModifier(modifiers, Token.New, Error.InvalidModifier);
      this.LookForModifier(modifiers, Token.Override, Error.InvalidModifier);
      this.LookForModifier(modifiers, Token.Readonly, Error.InvalidModifier);
      this.LookForModifier(modifiers, Token.Sealed, Error.InvalidModifier);
      if (this.LookForModifier(modifiers, Token.Static, Error.None)) flags |= MethodDeclaration.Flags.Static;
      if (this.LookForModifier(modifiers, Token.Unsafe, Error.None)) flags |= MethodDeclaration.Flags.Unsafe;
      this.LookForModifier(modifiers, Token.Virtual, Error.InvalidModifier);
      this.LookForModifier(modifiers, Token.Volatile, Error.InvalidModifier);


      NameDeclaration opName = new NameDeclaration(this.nameTable.EmptyName, this.scanner.SourceLocationOfLastScannedToken);
      SourceLocationBuilder ctx = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
      ISourceLocation symCtx = this.scanner.SourceLocationOfLastScannedToken;
      bool canBeBinary = false;
      bool canBeUnary = false;
      switch (this.currentToken) {
        case Token.Explicit:
          this.GetNextToken();
          ctx.UpdateToSpan(this.scanner.SourceLocationOfLastScannedToken);
          opName = this.GetNameDeclarationFor("op_Explicit", ctx.GetSourceLocation());
          this.Skip(Token.Operator);
          if (resultType != null && this.currentToken == Token.LeftParenthesis)
            this.HandleError(opName.SourceLocation, Error.BadOperatorSyntax, "explicit");
          else
            resultType = this.ParseTypeExpression(false, false, followers|Token.LeftParenthesis);
          canBeUnary = true;
          break;
        case Token.Implicit:
          this.GetNextToken();
          ctx.UpdateToSpan(this.scanner.SourceLocationOfLastScannedToken);
          opName = this.GetNameDeclarationFor("op_Implicit", ctx.GetSourceLocation());
          this.Skip(Token.Operator);
          if (resultType != null && this.currentToken == Token.LeftParenthesis)
            this.HandleError(opName.SourceLocation, Error.BadOperatorSyntax, "implicit");
          else
            resultType = this.ParseTypeExpression(false, false, followers|Token.LeftParenthesis);
          canBeUnary = true;
          break;
        case Token.Operator: 
          this.GetNextToken();
          symCtx = this.scanner.SourceLocationOfLastScannedToken;
          ctx.UpdateToSpan(symCtx);
          this.GetOperatorName(ref opName, ref canBeBinary, ref canBeUnary, ctx.GetSourceLocation());
          if (this.currentToken != Token.EndOfFile) this.GetNextToken();
          if (resultType == null) {
            this.HandleError(Error.BadOperatorSyntax2, ctx.GetSourceLocation().Source);
            if (this.currentToken != Token.LeftParenthesis)
              resultType = this.ParseTypeExpression(false, false, followers|Token.LeftParenthesis);
            else
              resultType = new NamedTypeExpression(this.RootQualifiedNameFor(Token.Void));
          }
          break;
        default:
          //^ assert false;
          break;
      }
      //Parse the parameter list
      List<Ast.ParameterDeclaration> parameters = new List<Ast.ParameterDeclaration>();
      this.ParseParameters(parameters, Token.RightParenthesis, followers|Token.LeftBrace|Token.Semicolon|Token.Requires|Token.Modifies|Token.Ensures|Token.Where|Token.Throws);
      switch (parameters.Count) {
        case 1:
          if (!canBeUnary)
            this.HandleError(symCtx, Error.OvlUnaryOperatorExpected);
          if (canBeBinary && opName != null) {
            if (opName.Value == "op_Addition") opName = this.GetNameDeclarationFor("op_UnaryPlus", opName.SourceLocation);
            else if (opName.Value == "op_Subtraction") opName = this.GetNameDeclarationFor("op_UnaryNegation", opName.SourceLocation);
          }
          break;
        case 2:
          if (!canBeBinary)
            if (canBeUnary)
              this.HandleError(symCtx, Error.WrongParsForUnaryOp, opName.SourceLocation.Source);
            else
              this.HandleError(symCtx, Error.OvlBinaryOperatorExpected);
          break;
        default:
          if (canBeBinary)
            this.HandleError(symCtx, Error.WrongParsForBinOp, opName.SourceLocation.Source);
          else if (canBeUnary)
            this.HandleError(symCtx, Error.WrongParsForUnaryOp, opName.SourceLocation.Source);
          else
            this.HandleError(symCtx, Error.OvlBinaryOperatorExpected);
          break;
      }
      ctx.UpdateToSpan(this.scanner.SourceLocationOfLastScannedToken);

      //bool swallowedSemicolonAlready = false;
      //this.ParseMethodContract(oper, followers|Token.LeftBrace|Token.Semicolon, ref swallowedSemicolonAlready);
      BlockStatement body = this.ParseBody(followers);
      //^ assert followers[this.currentToken] || this.currentToken == Token.EndOfFile;
      sctx.UpdateToSpan(body.SourceLocation);

      MethodDeclaration method = new MethodDeclaration(attributes, flags, visibility, 
        resultType, null, opName, null, parameters, null, body, sctx.GetSourceLocation());
      members.Add(method);
      //^ assume followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    }

    private void GetOperatorName(ref NameDeclaration opName, ref bool canBeBinary, ref bool canBeUnary, ISourceLocation opCtxt) {
      switch (this.currentToken) {
        case Token.Plus:
          canBeBinary = true;
          canBeUnary = true;
          opName = this.GetNameDeclarationFor("op_Addition", opCtxt);
          break;
        case Token.Subtract:
          canBeBinary = true;
          canBeUnary = true;
          opName = this.GetNameDeclarationFor("op_Subtraction", opCtxt);
          break;
        case Token.Multiply:
          canBeBinary = true;
          opName = this.GetNameDeclarationFor("op_Multiply", opCtxt);
          break;
        case Token.Divide:
          canBeBinary = true;
          opName = this.GetNameDeclarationFor("op_Division", opCtxt);
          break;
        case Token.Remainder:
          canBeBinary = true;
          opName = this.GetNameDeclarationFor("op_Modulus", opCtxt);
          break;
        case Token.BitwiseAnd:
          canBeBinary = true;
          opName = this.GetNameDeclarationFor("op_BitwiseAnd", opCtxt);
          break;
        case Token.BitwiseOr:
          canBeBinary = true;
          opName = this.GetNameDeclarationFor("op_BitwiseOr", opCtxt);
          break;
        case Token.BitwiseXor:
          canBeBinary = true;
          opName = this.GetNameDeclarationFor("op_ExclusiveOr", opCtxt);
          break;
        case Token.LeftShift:
          canBeBinary = true;
          opName = this.GetNameDeclarationFor("op_LeftShift", opCtxt);
          break;
        case Token.RightShift:
          canBeBinary = true;
          opName = this.GetNameDeclarationFor("op_RightShift", opCtxt);
          break;
        case Token.Equal:
          canBeBinary = true;
          opName = this.GetNameDeclarationFor("op_Equality", opCtxt);
          break;
        case Token.NotEqual:
          canBeBinary = true;
          opName = this.GetNameDeclarationFor("op_Inequality", opCtxt);
          break;
        case Token.GreaterThan:
          canBeBinary = true;
          opName = this.GetNameDeclarationFor("op_GreaterThan", opCtxt);
          break;
        case Token.LessThan:
          canBeBinary = true;
          opName = this.GetNameDeclarationFor("op_LessThan", opCtxt);
          break;
        case Token.GreaterThanOrEqual:
          canBeBinary = true;
          opName = this.GetNameDeclarationFor("op_GreaterThanOrEqual", opCtxt);
          break;
        case Token.LessThanOrEqual:
          canBeBinary = true;
          opName = this.GetNameDeclarationFor("op_LessThanOrEqual", opCtxt);
          break;
        case Token.LogicalNot:
          canBeUnary = true;
          opName = this.GetNameDeclarationFor("op_LogicalNot", opCtxt);
          break;
        case Token.BitwiseNot:
          canBeUnary = true;
          opName = this.GetNameDeclarationFor("op_OnesComplement", opCtxt);
          break;
        case Token.AddOne:
          canBeUnary = true;
          opName = this.GetNameDeclarationFor("op_Increment", opCtxt);
          break;
        case Token.SubtractOne:
          canBeUnary = true;
          opName = this.GetNameDeclarationFor("op_Decrement", opCtxt);
          break;
        case Token.True:
          canBeUnary = true;
          opName = this.GetNameDeclarationFor("op_True", opCtxt);
          break;
        case Token.False:
          canBeUnary = true;
          opName = this.GetNameDeclarationFor("op_False", opCtxt);
          break;
        case Token.Implicit:
          canBeUnary = true;
          opName = this.GetNameDeclarationFor("op_Implicit", opCtxt);
          this.HandleError(opName.SourceLocation, Error.BadOperatorSyntax, "implicit");
          break;
        case Token.Explicit:
          canBeUnary = true;
          opName = this.GetNameDeclarationFor("op_Explicit", opCtxt);
          this.HandleError(opName.SourceLocation, Error.BadOperatorSyntax, "explicit");
          break;
      }
    }

    private void ParseProperty(List<ITypeDeclarationMember> members, 
      List<SourceCustomAttribute>/*?*/ attributes, List<ModifierToken> modifiers, TypeExpression type, List<TypeExpression>/*?*/ implementedInterfaces, 
      NameDeclaration name, SourceLocationBuilder sctx, TokenSet followers) 
      //^ requires (this.currentToken == Token.This && name.Value == "Item") || this.currentToken == Token.LeftBrace;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      PropertyDeclaration.Flags flags = 0;
      TypeMemberVisibility visibility = this.ConvertToTypeMemberVisibility(modifiers);
      if (this.LookForModifier(modifiers, Token.Abstract, Error.None)) flags |= PropertyDeclaration.Flags.Abstract;
      if (this.LookForModifier(modifiers, Token.New, Error.None)) flags |= PropertyDeclaration.Flags.New;
      if (this.LookForModifier(modifiers, Token.Extern, Error.None)) flags |= PropertyDeclaration.Flags.External;
      if (this.LookForModifier(modifiers, Token.Override, Error.None)) flags |= PropertyDeclaration.Flags.Override;
      this.LookForModifier(modifiers, Token.Readonly, Error.InvalidModifier);
      if (this.LookForModifier(modifiers, Token.Sealed, Error.None)) flags |= PropertyDeclaration.Flags.Sealed;
      if (this.LookForModifier(modifiers, Token.Static, Error.None)) flags |= PropertyDeclaration.Flags.Static;
      if (this.LookForModifier(modifiers, Token.Unsafe, Error.None)) flags |= PropertyDeclaration.Flags.Unsafe;
      if (this.LookForModifier(modifiers, Token.Virtual, Error.None)) flags |= PropertyDeclaration.Flags.Virtual;
      this.LookForModifier(modifiers, Token.Volatile, Error.InvalidModifier);

      bool isIndexer = this.currentToken == Token.This;
      this.GetNextToken();
      List<Ast.ParameterDeclaration>/*?*/ parameters = null;
      if (isIndexer) {
        parameters = new List<Ast.ParameterDeclaration>();
        this.ParseParameters(parameters, Token.RightBracket, followers|Token.LeftBrace);
        this.Skip(Token.LeftBrace);
      }

      //if (this.currentToken == Token.LeftBrace)
      //  this.GetNextToken();
      //else {
      //  Error e = Error.ExpectedLeftBrace;
      //  if (implementedInterfaces != null) e = Error.ExplicitEventFieldImpl;
      //  this.SkipTo(followers|Token.LeftBracket|Token.Add|Token.Remove|Token.RightBrace, e);
      //}

      TokenSet followersOrRightBrace = followers|Token.RightBrace;
      List<SourceCustomAttribute>/*?*/ getterAttributes = null;
      TypeMemberVisibility getterVisibility = visibility;
      BlockStatement/*?*/ getterBody = null;
      List<SourceCustomAttribute>/*?*/ setterAttributes = null;
      BlockStatement/*?*/ setterBody = null;
      TypeMemberVisibility setterVisibility = visibility;
      bool alreadyComplainedAboutModifier = false;
      List<ModifierToken>/*?*/ accessorModifiers = null;
      SourceLocationBuilder/*?*/ bodyCtx = null;
      for (; ; ) {
        if (bodyCtx == null) bodyCtx = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
        List<SourceCustomAttribute>/*?*/ accessorAttrs = this.ParseAttributes(followers | Parser.GetOrLeftBracketOrSetOrModifier | Token.LeftBrace);
        switch (this.currentToken) {
          case Token.Get:
            if (accessorModifiers != null) {
              getterVisibility = this.ConvertToTypeMemberVisibility(accessorModifiers);
              accessorModifiers = null;
            }
            getterAttributes = accessorAttrs;
            if (getterBody != null)
              this.HandleError(Error.DuplicateAccessor);
            this.GetNextToken();
            bool bodyAllowed = true;
            if (this.currentToken == Token.Semicolon) {
              this.GetNextToken();
              bodyAllowed = false;
            }
            //this.ParseMethodContract(m, followers|Token.LeftBrace|Token.Semicolon, ref swallowedSemicolonAlready);
            if (bodyAllowed)
              getterBody = this.ParseBody(bodyCtx, followersOrRightBrace|Token.Set);
            bodyCtx = null;
            continue;
          case Token.Set:
            if (accessorModifiers != null) {
              setterVisibility = this.ConvertToTypeMemberVisibility(accessorModifiers);
              accessorModifiers = null;
            }
            setterAttributes = accessorAttrs;
            if (setterBody != null)
              this.HandleError(Error.DuplicateAccessor);
            this.GetNextToken();
            bodyAllowed = true;
            if (this.currentToken == Token.Semicolon) {
              this.GetNextToken();
              bodyAllowed = false;
            }
            //this.ParseMethodContract(m, followers|Token.LeftBrace|Token.Semicolon, ref swallowedSemicolonAlready);
            if (bodyAllowed)
              setterBody = this.ParseBody(bodyCtx, followersOrRightBrace|Token.Get);
            bodyCtx = null;
            continue;
          case Token.Protected:
          case Token.Internal:
          case Token.Private:
            if (accessorModifiers != null) goto case Token.Public;
            accessorModifiers = this.ParseModifiers();
            continue;
          case Token.Public:
          case Token.New:
          case Token.Abstract:
          case Token.Sealed:
          case Token.Static:
          case Token.Readonly:
          case Token.Volatile:
          case Token.Virtual:
          case Token.Override:
          case Token.Extern:
          case Token.Unsafe:
            if (!alreadyComplainedAboutModifier)
              this.HandleError(Error.NoModifiersOnAccessor);
            this.GetNextToken();
            alreadyComplainedAboutModifier = true;
            continue;
          case Token.RightBrace:
            break;
          default:
            this.HandleError(Error.GetOrSetExpected);
            break;
        }
        break;
      }
      sctx.UpdateToSpan(bodyCtx.GetSourceLocation());
      PropertyDeclaration prop = new PropertyDeclaration(attributes, flags, visibility, type, implementedInterfaces, name, parameters, 
        getterAttributes, getterBody, getterVisibility, setterAttributes, setterBody, setterVisibility, sctx.GetSourceLocation());
      members.Add(prop);
      this.SkipOverTo(Token.RightBrace, followers);
    }

    private void ParseParameters(List<Ast.ParameterDeclaration> parameters, Token closingToken, TokenSet followers) {
      this.ParseParameters(parameters, closingToken, followers, new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken));
    }

    private void ParseParameters(List<Ast.ParameterDeclaration> parameters, Token closingToken, TokenSet followers, SourceLocationBuilder ctx) 
      //^ requires closingToken == Token.RightBracket || closingToken == Token.RightParenthesis;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      ctx.UpdateToSpan(this.scanner.SourceLocationOfLastScannedToken);
      if (closingToken == Token.RightBracket)
        this.Skip(Token.LeftBracket);
      else
        this.Skip(Token.LeftParenthesis);
      while (this.currentToken != closingToken && this.currentToken != Token.EndOfFile){
        CSharpParameterDeclaration parameter = this.ParseParameter((ushort)parameters.Count, closingToken == Token.RightParenthesis, followers|Token.Comma|closingToken);
        parameters.Add(parameter);
        if (this.currentToken != Token.Comma) break;
        this.GetNextToken();
      }
      ctx.UpdateToSpan(this.scanner.SourceLocationOfLastScannedToken);
      parameters.TrimExcess();
      this.SkipOverTo(closingToken, followers);
    }

    private CSharpParameterDeclaration ParseParameter(ushort index, bool allowRefParameters, TokenSet followers)
      //^ requires this.currentToken != Token.EndOfFile;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      SourceLocationBuilder sctx = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
      List<SourceCustomAttribute>/*?*/ attributes = this.ParseAttributes(followers|Parser.ParameterTypeStart);
      bool isParamArray = false;
      bool isOut = false;
      bool isRef = false;
      if (this.currentToken == Token.Params) {
        isParamArray = true;
        this.GetNextToken();
      } else if (this.currentToken == Token.Ref) {
        isRef = allowRefParameters;
        if (!allowRefParameters)
          this.HandleError(Error.IndexerWithRefParam);
        this.GetNextToken();
      } else if (this.currentToken == Token.Out) {
        isOut = allowRefParameters;
        if (!allowRefParameters)
          this.HandleError(Error.IndexerWithRefParam);
        this.GetNextToken();
      }
      TypeExpression type = this.ParseTypeExpression(false, false, followers|Parser.IdentifierOrNonReservedKeyword);
      if (isParamArray && !(type is ArrayTypeExpression)) {
        //TODO: error message
        isParamArray = false;
      }
      NameDeclaration name = this.ParseNameDeclaration();
      if (this.currentToken == Token.LeftBracket) {
        this.HandleError(Error.BadArraySyntax);
        uint rank = this.ParseRankSpecifier(sctx, followers|Token.LeftBracket);
        type = this.ParseArrayType(rank, type, sctx, followers);
      } else if (this.currentToken == Token.Assign) {
        this.HandleError(Error.NoDefaultArgs);
        this.GetNextToken();
        if (Parser.UnaryStart[this.currentToken]) {
          this.ParseExpression(followers);
        }
      }
      sctx.UpdateToSpan(name.SourceLocation);
      CSharpParameterDeclaration result = new CSharpParameterDeclaration(attributes, type, name, null, index, false, isOut, isParamArray, isRef, sctx);
      this.SkipTo(followers);
      return result;
    }

    private BlockStatement ParseBody(TokenSet followers)
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      SourceLocationBuilder bodyCtx = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
      return this.ParseBody(bodyCtx, followers);
    }

    private BlockStatement ParseBody(SourceLocationBuilder bodyCtx, TokenSet followers)
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      List<Statement> statements = new List<Statement>();
      BlockStatement block = new BlockStatement(statements,
        this.compilation.Options.CheckedArithmetic ? BlockStatement.Options.UseCheckedArithmetic : BlockStatement.Options.UseUncheckedArithmetic, bodyCtx);
      this.ParseBody(statements, bodyCtx, followers);
      //TODO: throw the body away and replace it with a stub that will reparse when needed.
      return block;
    }

    //TODO: get rid of this method (Need to rewrite ParseMethod first).
    private void ParseBody(List<Statement> statements, SourceLocationBuilder bodyCtx, TokenSet followers)
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      bodyCtx.UpdateToSpan(this.scanner.SourceLocationOfLastScannedToken);
      if (this.currentToken == Token.Semicolon) {
        this.GetNextToken();
        //TODO: error if method is not abstract
      }else if (this.currentToken == Token.LeftBrace){
        this.GetNextToken();
        //Temporary hack to skip over method bodies so that symbol tables can be parsed from source before statements are parsable.
        //for (int braceCount = 1; braceCount > 0; this.GetNextToken()){
        //  if (this.currentToken == Token.LeftBrace) braceCount++;
        //  else if (this.currentToken == Token.RightBrace) braceCount--;
        //}
        this.ParseStatements(statements, followers|Token.RightBrace);
        //statements.Add(new EmptyStatement(true, this.scanner.SourceLocationOfLastScannedToken));
        bodyCtx.UpdateToSpan(this.scanner.SourceLocationOfLastScannedToken);
        this.Skip(Token.RightBrace);
      }
      this.SkipTo(followers);
    }

    private BlockStatement ParseBlock(TokenSet followers)
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      SourceLocationBuilder slb = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
      return this.ParseBlock(BlockStatement.Options.Default, slb, followers);
    }

    private BlockStatement ParseBlock(BlockStatement.Options options, SourceLocationBuilder slb, TokenSet followers)
      //^ requires options == BlockStatement.Options.Default || options == BlockStatement.Options.AllowUnsafeCode || 
      //^  options == BlockStatement.Options.UseCheckedArithmetic || options == BlockStatement.Options.UseUncheckedArithmetic;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      this.Skip(Token.LeftBrace);
      List<Statement> statements = new List<Statement>();
      this.ParseStatements(statements, followers|Token.RightBrace);
      slb.UpdateToSpan(this.scanner.SourceLocationOfLastScannedToken);
      BlockStatement result = new BlockStatement(statements, options, slb);
      this.SkipOverTo(Token.RightBrace, followers);
      return result;
    }

    private void ParseStatements(List<Statement> statements, TokenSet followers)
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      TokenSet statementFollowers = followers|Parser.StatementStart;
      if (!statementFollowers[this.currentToken])
        this.SkipTo(statementFollowers, Error.InvalidExprTerm, this.scanner.GetTokenSource());
      while (Parser.StatementStart[this.currentToken]) {
        Token tok = this.currentToken;
        Statement s = this.ParseStatement(statementFollowers);
        if (s is EmptyStatement && (tok == Token.Get || tok == Token.Set || 
          (this.unmatchedTry && (this.currentToken == Token.Catch || this.currentToken == Token.Finally))))
          break;
        statements.Add(s);
      }
      this.SkipTo(followers);
    }

    private Statement ParseStatement(TokenSet followers)
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      switch (this.currentToken) {
        case Token.LeftBrace: return this.ParseBlock(followers);
        case Token.Semicolon: return this.ParseEmptyStatement(followers);
        //case Token.Acquire: return this.ParseAcquire(followers);
        //case Token.Assert: return this.ParseAssertion(followers);
        //case Token.Assume: return this.ParseAssumption(followers);
        case Token.If: return this.ParseIf(followers);
        case Token.Switch: return this.ParseSwitch(followers);
        case Token.While: return this.ParseWhile(followers);
        case Token.Do: return this.ParseDoWhile(followers);
        case Token.For: return this.ParseFor(followers);
        case Token.Foreach: return this.ParseForEach(followers);
        case Token.Break: return this.ParseBreak(followers);
        case Token.Continue: return this.ParseContinue(followers);
        case Token.Goto: return this.ParseGoto(followers);
        case Token.Return: return this.ParseReturn(followers);
        case Token.Throw: return this.ParseThrow(followers);
        case Token.Yield: return this.ParseYield(followers);
        case Token.Try:
        case Token.Catch:
        case Token.Finally:
          return this.ParseTry(followers);
        case Token.Checked: return this.ParseChecked(followers);
        case Token.Unchecked: return this.ParseUnchecked(followers);
        //case Token.Read: return this.ParseExpose(followers, NodeType.Read);
        //case Token.Write: return this.ParseExpose(followers, NodeType.Write);
        //case Token.Expose: return this.ParseExpose(followers, NodeType.Write);
        case Token.Fixed: return this.ParseFixed(followers);
        case Token.Lock: return this.ParseLock(followers);
        case Token.Using: return this.ParseUsing(followers);
        case Token.Unsafe: return this.ParseUnsafe(followers);
        case Token.Const: return this.ParseLocalConst(followers);
        case Token.New: return this.ParseNewStatement(followers);
        case Token.Get:
        case Token.Set:
          if (followers[this.currentToken]) {
            //Inside getter or setter. Do not allow get or set to be followed by a {.
            Token nextToken = this.PeekNextToken();
            if (nextToken == Token.LeftBrace) {
              return new EmptyStatement(false, this.scanner.SourceLocationOfLastScannedToken); //The caller looks for this to detect this situation
            }
          }
          goto default;
        default:
          return this.ParseExpressionStatementOrDeclaration(false, true, followers, true);
      }
    }

    private Statement ParseExpressionStatementOrDeclaration(bool acceptComma, bool acceptLabel, TokenSet followers, bool skipSemicolon)
      //^ requires acceptComma ==> followers[Token.Comma];
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
      //^ ensures result is ExpressionStatement || result is LocalDeclarationsStatement || (acceptLabel && result is LabeledStatement);
    {
      int position = this.scanner.CurrentDocumentPosition();
      Token nextToken = this.PeekNextToken();
      TypeExpression/*?*/ te = null;
      if (nextToken == Token.Conditional || nextToken == Token.LessThan || nextToken == Token.Multiply ||
      !(nextToken == Token.LeftParenthesis || nextToken == Token.Semicolon || Parser.InfixOperators[nextToken])) {
        List<IErrorMessage> savedErrors = this.scannerAndParserErrors;
        this.scannerAndParserErrors = new List<IErrorMessage>();
        te = this.ParseTypeExpression(false, false, followers|Parser.IdentifierOrNonReservedKeyword);
        if (this.scannerAndParserErrors.Count != 0) te = null;
        this.scannerAndParserErrors = savedErrors;
      }
      if (te == null || !Parser.IdentifierOrNonReservedKeyword[this.currentToken]) {
        //Tried to parse a type expression and failed, or clearly not dealing with a declaration.
        //Restore prior state and reparse as expression
        this.scanner.RestoreDocumentPosition(position);
        this.currentToken = Token.None;
        this.GetNextToken();
        TokenSet followersOrCommaOrColon = followers|Token.Comma|Token.Colon;
        Expression e = this.ParseExpression(followersOrCommaOrColon);
        SourceLocationBuilder slb = new SourceLocationBuilder(e.SourceLocation);
        ExpressionStatement eStat = new ExpressionStatement(e, slb);
        SimpleName/*?*/ id = null;
        if (this.currentToken == Token.Colon && acceptLabel && (id = e as SimpleName) != null)
          return this.ParseLabeledStatement(id, followers);
        if (!(e is Assignment || e is BinaryOperationAssignment || e is CreateObjectInstance || e is MethodCall || e is UnaryOperationAssignment || followers[Token.RightParenthesis]))
          this.HandleError(e.SourceLocation, Error.IllegalStatement);
        if (!acceptComma || this.currentToken != Token.Comma) {
          if (this.currentToken == Token.Semicolon) {
            slb.UpdateToSpan(this.scanner.SourceLocationOfLastScannedToken);
            //^ assume this.currentToken == Token.Semicolon;
            this.GetNextToken();
          } else if (skipSemicolon)
            this.SkipSemiColon(followers);
          this.SkipTo(followers);
        }
        //^ assume followers[this.currentToken] || this.currentToken == Token.EndOfFile;
        return eStat;
      }
      return this.ParseLocalDeclarations(new SourceLocationBuilder(te.SourceLocation), te, false, false, skipSemicolon, followers);
    }

    private LabeledStatement ParseLabeledStatement(SimpleName label, TokenSet followers) 
      //^ requires this.currentToken == Token.Colon;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      SourceLocationBuilder slb = new SourceLocationBuilder(label.SourceLocation);
      this.GetNextToken();
      Statement statement;
      if (Parser.StatementStart[this.currentToken]) {
        statement = this.ParseStatement(followers);
      } else {
        statement = new EmptyStatement(false, this.scanner.SourceLocationOfLastScannedToken);
        this.SkipTo(followers, Error.ExpectedSemicolon);
      }
      //^ assert followers[this.currentToken] || this.currentToken == Token.EndOfFile;
      slb.UpdateToSpan(statement.SourceLocation);
      LabeledStatement result = new LabeledStatement(new NameDeclaration(label.Name, label.SourceLocation), statement, slb);
      //^ assume followers[this.currentToken] || this.currentToken == Token.EndOfFile;
      return result;
    }

    //private ParseFixedPointerDeclarations

    private LocalDeclarationsStatement ParseFixedPointerDeclarations(SourceLocationBuilder slb, TypeExpression typeExpression, TokenSet followers)
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      List<LocalDeclaration> declarations = new List<LocalDeclaration>();
      for (; ; ) {
        NameDeclaration locName = this.ParseNameDeclaration();
        SourceLocationBuilder locSctx = new SourceLocationBuilder(locName.SourceLocation);
        this.Skip(Token.Assign);
        Expression/*?*/ locInitialValue = this.ParseExpression(followers|Token.Semicolon|Token.Comma);
        locSctx.UpdateToSpan(this.scanner.SourceLocationOfLastScannedToken);
        slb.UpdateToSpan(this.scanner.SourceLocationOfLastScannedToken);
        declarations.Add(new FixedPointerDeclaration(locName, locInitialValue, locSctx));
        if (this.currentToken != Token.Comma) break;
        this.GetNextToken();
      }
      declarations.TrimExcess();
      LocalDeclarationsStatement result = new LocalDeclarationsStatement(false, true, false, typeExpression, declarations, slb);
      this.SkipTo(followers);
      return result;
    }

    private LocalDeclarationsStatement ParseLocalDeclarations(SourceLocationBuilder slb, TypeExpression typeExpression, bool constant, bool initOnly, bool skipSemicolon, TokenSet followers)
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      List<LocalDeclaration> declarations = new List<LocalDeclaration>();
      for (; ; ) {
        NameDeclaration locName = this.ParseNameDeclaration();
        SourceLocationBuilder locSctx = new SourceLocationBuilder(locName.SourceLocation);
        //if (this.currentToken == Token.LeftBracket) {
        //  this.HandleError(Error.CStyleArray);
        //  int endPos = this.scanner.endPos;
        //  int rank = this.ParseRankSpecifier(true, followers|Token.RightBracket|Parser.IdentifierOrNonReservedKeyword|Token.Assign|Token.Semicolon|Token.Comma);
        //  if (rank > 0)
        //    t = result.Type = result.TypeExpression =
        //      this.ParseArrayType(rank, t, followers|Token.RightBracket|Parser.IdentifierOrNonReservedKeyword|Token.Assign|Token.Semicolon|Token.Comma);
        //  else {
        //    this.currentToken = Token.LeftBracket;
        //    this.scanner.endPos = endPos;
        //    this.GetNextToken();
        //    while (!this.scanner.TokenIsFirstAfterLineBreak && 
        //      this.currentToken != Token.RightBracket && this.currentToken != Token.Assign && this.currentToken != Token.Semicolon)
        //      this.GetNextToken();
        //    if (this.currentToken == Token.RightBracket) this.GetNextToken();
        //  }
        //}
        //if (this.currentToken == Token.LeftParenthesis) {
        //  this.HandleError(Error.BadVarDecl);
        //  int dummy;
        //  SourceContext lpCtx = this.scanner.CurrentSourceContext;
        //  this.GetNextToken();
        //  this.ParseArgumentList(followers|Token.LeftBrace|Token.Semicolon|Token.Comma, lpCtx, out dummy);
        //} else 
        Expression/*?*/ locInitialValue = null;
        if (this.currentToken == Token.Assign || constant) {
          this.Skip(Token.Assign);
          ArrayTypeExpression/*?*/ arrayTypeExpression = typeExpression as ArrayTypeExpression;
          if (this.currentToken == Token.LeftBrace && arrayTypeExpression != null)
            locInitialValue = this.ParseArrayInitializer(arrayTypeExpression, followers|Token.Semicolon|Token.Comma);
          else
            locInitialValue = this.ParseExpression(followers|Token.Semicolon|Token.Comma);
        }
        locSctx.UpdateToSpan(this.scanner.SourceLocationOfLastScannedToken);
        slb.UpdateToSpan(this.scanner.SourceLocationOfLastScannedToken);
        declarations.Add(new LocalDeclaration(false, false, locName, locInitialValue, locSctx));

        if (this.currentToken != Token.Comma) break;
        this.GetNextToken();

        //SourceContext sctx = this.scanner.CurrentSourceContext;
        //ScannerState ss = this.scanner.state;
        //TypeNode ty = this.ParseTypeExpression(null, followers|Token.Identifier|Token.Comma|Token.Semicolon, true);
        //if (ty == null || this.currentToken != Token.Identifier) {
        //  this.scanner.endPos = sctx.StartPos;
        //  this.scanner.state = ss;
        //  this.currentToken = Token.None;
        //  this.GetNextToken();
        //} else
        //  this.HandleError(sctx, Error.MultiTypeInDeclaration);
      }
      if (skipSemicolon) this.SkipSemiColon(followers);
      declarations.TrimExcess();
      LocalDeclarationsStatement result = new LocalDeclarationsStatement(constant, initOnly, !constant, typeExpression, declarations, slb);
      this.SkipTo(followers);
      return result;
    }

    private Statement ParseNewStatement(TokenSet followers)
      //^ requires this.currentToken == Token.New;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      SourceLocationBuilder slb = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
      Statement s = new ExpressionStatement(this.ParseNew(followers), slb);
      slb.UpdateToSpan(this.scanner.SourceLocationOfLastScannedToken);
      this.SkipSemiColon(followers);
      return s;
    }

    private Statement ParseLocalConst(TokenSet followers)
      //^ requires this.currentToken == Token.Const;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      SourceLocationBuilder slb = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
      this.GetNextToken();
      TypeExpression te = this.ParseBaseTypeExpression(false, followers|Token.Identifier|Token.Assign|Token.Comma);
      slb.UpdateToSpan(te.SourceLocation);
      return this.ParseLocalDeclarations(slb, te, true, false, true, followers);
    }

    private Statement ParseUnsafe(TokenSet followers)
      //^ requires this.currentToken == Token.Unsafe;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      SourceLocationBuilder slb = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
      this.GetNextToken();
      return this.ParseBlock(BlockStatement.Options.AllowUnsafeCode, slb, followers);
    }

    private Statement ParseUsing(TokenSet followers)
      //^ requires this.currentToken == Token.Using;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      SourceLocationBuilder slb = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
      this.GetNextToken();
      if (Parser.IdentifierOrNonReservedKeyword[this.currentToken]) {
        this.HandleError(Error.SyntaxError, "(");
        this.GetNextToken();
        if (this.currentToken == Token.Semicolon)
          this.GetNextToken();
        slb.UpdateToSpan(this.scanner.SourceLocationOfLastScannedToken);
        EmptyStatement es = new EmptyStatement(false, slb);
        this.SkipTo(followers);
        return es;
      }
      this.Skip(Token.LeftParenthesis);
      Statement resourceAcquisition = this.ParseExpressionStatementOrDeclaration(false, false, followers|Token.RightParenthesis|Parser.StatementStart, false);
      //^ assert resourceAcquisition is ExpressionStatement || resourceAcquisition is LocalDeclarationsStatement;
      this.Skip(Token.RightParenthesis);
      Statement body = this.ParseStatement(followers);
      slb.UpdateToSpan(body.SourceLocation);
      Statement result = new ResourceUseStatement(resourceAcquisition, body, slb);
      //^ assume followers[this.currentToken] || this.currentToken == Token.EndOfFile;
      return result;
    }

    private Statement ParseLock(TokenSet followers)
      //^ requires this.currentToken == Token.Lock;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      SourceLocationBuilder slb = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
      this.GetNextToken();
      Expression guard = this.ParseParenthesizedExpression(false, followers|Parser.StatementStart);
      Statement body = this.ParseStatement(followers);
      if (body is EmptyStatement)
        this.HandleError(body.SourceLocation, Error.PossibleMistakenNullStatement);
      slb.UpdateToSpan(body.SourceLocation);
      Statement result = new LockStatement(guard, body, slb);
      //^ assume followers[this.currentToken] || this.currentToken == Token.EndOfFile;
      return result;
    }

    private Statement ParseFixed(TokenSet followers)
      //^ requires this.currentToken == Token.Fixed;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      SourceLocationBuilder slb = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
      this.GetNextToken();
      this.Skip(Token.LeftParenthesis);
      TypeExpression te = this.ParseTypeExpression(false, false, followers|Parser.IdentifierOrNonReservedKeyword);
      LocalDeclarationsStatement declarators = this.ParseFixedPointerDeclarations(slb, te, followers|Token.RightParenthesis|Parser.StatementStart);
      this.Skip(Token.RightParenthesis);
      Statement body = this.ParseStatement(followers);
      //^ assert followers[this.currentToken] || this.currentToken == Token.EndOfFile;
      slb.UpdateToSpan(body.SourceLocation);
      FixedStatement result = new FixedStatement(declarators, body, slb);
      //^ assume followers[this.currentToken] || this.currentToken == Token.EndOfFile;
      return result;
    }

    private Statement ParseUnchecked(TokenSet followers)      
      //^ requires this.currentToken == Token.Unchecked;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      SourceLocationBuilder slb = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
      this.GetNextToken();
      return this.ParseBlock(BlockStatement.Options.UseUncheckedArithmetic, slb, followers);
    }

    private Statement ParseChecked(TokenSet followers)
      //^ requires this.currentToken == Token.Checked;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      SourceLocationBuilder slb = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
      this.GetNextToken();
      return this.ParseBlock(BlockStatement.Options.UseCheckedArithmetic, slb, followers);
    }

    private bool unmatchedTry;

    private Statement ParseTry(TokenSet followers)
      //^ requires this.currentToken == Token.Try || this.currentToken == Token.Catch || this.currentToken == Token.Finally;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      bool savedUnmatchedTry = this.unmatchedTry;
      SourceLocationBuilder tryCatchFinallyContext = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
      TokenSet tryBlockFollowers = followers|Parser.CatchOrFinally;
      BlockStatement tryBody;
      if (this.currentToken == Token.Try) {
        this.unmatchedTry = true;
        this.GetNextToken();
        if (this.currentToken == Token.LeftBrace)
          tryBody = this.ParseBlock(tryBlockFollowers);
        else {
          List<Statement> tryBlockStatements = new List<Statement>();
          SourceLocationBuilder slb = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
          this.HandleError(Error.ExpectedLeftBrace);
          if (Parser.StatementStart[this.currentToken]) {
            this.ParseStatements(tryBlockStatements, tryBlockFollowers|Token.RightBrace);
            slb.UpdateToSpan(this.scanner.SourceLocationOfLastScannedToken);
            this.Skip(Token.RightBrace);
          }
          tryBody = new BlockStatement(tryBlockStatements, slb);
        }
        tryCatchFinallyContext.UpdateToSpan(tryBody.SourceLocation);
      } else {
        if (savedUnmatchedTry && ((this.currentToken == Token.Catch && followers[Token.Catch]) || (this.currentToken == Token.Finally && followers[Token.Finally])))
          return new EmptyStatement(false, tryCatchFinallyContext); //Busy parsing the body of a try. Return an empty statement to signal that the body has come to an end. Leave the catch or finally in place.
        //Complain about missing try and insert a dummy try block before the catch or finally.
        this.HandleError(Error.SyntaxError, "try");
        tryBody = new BlockStatement(new List<Statement>(0), this.scanner.SourceLocationOfLastScannedToken);
      }
      List<CatchClause> catchClauses = new List<CatchClause>();
      bool seenEmptyCatch = false;
      while (this.currentToken == Token.Catch) {
        CatchClause c = this.ParseCatchClause(tryBlockFollowers, ref seenEmptyCatch);
        catchClauses.Add(c);
        tryCatchFinallyContext.UpdateToSpan(c.SourceLocation);
      }
      catchClauses.TrimExcess();
      BlockStatement/*?*/ finallyBody = null;
      if (this.currentToken == Token.Finally) {
        this.GetNextToken();
        finallyBody = this.ParseBlock(followers);
        tryCatchFinallyContext.UpdateToSpan(finallyBody.SourceLocation);
      } else if (catchClauses.Count == 0) {
        this.SkipTo(followers, Error.ExpectedEndTry);
      }
      this.unmatchedTry = savedUnmatchedTry;
      TryCatchFinallyStatement result = new TryCatchFinallyStatement(tryBody, catchClauses, finallyBody, tryCatchFinallyContext);
      //^ assume followers[this.currentToken] || this.currentToken == Token.EndOfFile;
      return result;
    }

    private CatchClause ParseCatchClause(TokenSet followers, ref bool seenEmptyCatch)
      //^ requires this.currentToken == Token.Catch;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      if (seenEmptyCatch) this.HandleError(Error.TooManyCatches);
      SourceLocationBuilder slb = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
      this.GetNextToken();
      TypeExpression exeptionType;
      NameDeclaration/*?*/ name = null;
      if (this.currentToken == Token.LeftParenthesis) {
        this.Skip(Token.LeftParenthesis);
        exeptionType = this.ParseTypeExpression(false, false, followers|Token.Identifier|Token.RightParenthesis);
        if (Parser.IdentifierOrNonReservedKeyword[this.currentToken]) 
          name = this.ParseNameDeclaration();
        this.Skip(Token.RightParenthesis);
      } else {
        exeptionType = this.GetTypeExpressionFor(Token.Object, slb.GetSourceLocation());
      }
      BlockStatement body = this.ParseBlock(followers);
      slb.UpdateToSpan(body.SourceLocation);
      CatchClause result = new CatchClause(exeptionType, null, name, body, slb);
      //^ assume followers[this.currentToken] || this.currentToken == Token.EndOfFile;
      return result;
    }

    private Statement ParseYield(TokenSet followers)
      //^ requires this.currentToken == Token.Yield;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      Token nextToken = this.PeekNextToken();
      if (nextToken == Token.Break){
        this.GetNextToken();
        ISourceLocation sctx = this.scanner.SourceLocationOfLastScannedToken;
        Statement result = new YieldBreakStatement(sctx);
        this.SkipOverTo(Token.Break, followers);
        return result;
      } 
      if (nextToken == Token.Return) {
        this.GetNextToken();
        //^ assume this.currentToken == Token.Return;
        this.GetNextToken();
        Expression val = this.ParseExpression(followers);
        ISourceLocation sctx = this.scanner.SourceLocationOfLastScannedToken;
        Statement result = new YieldReturnStatement(val, sctx);
        //^ assume followers[this.currentToken] || this.currentToken == Token.EndOfFile;
        return result;
      }
      return this.ParseExpressionStatementOrDeclaration(false, true, followers, true);
    }

    private Statement ParseThrow(TokenSet followers) 
      //^ requires this.currentToken == Token.Throw;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      SourceLocationBuilder slb = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
      this.GetNextToken();
      Expression/*?*/ expr = null;
      if (this.currentToken != Token.Semicolon) {
        expr = this.ParseExpression(followers|Token.Semicolon);
        slb.UpdateToSpan(expr.SourceLocation);
      }
      Statement result;
      if (expr == null)
        result = new RethrowStatement(slb);
      else
        result = new ThrowStatement(expr, slb);
      this.SkipSemiColon(followers);
      return result;
    }

    private Statement ParseReturn(TokenSet followers)       
      //^ requires this.currentToken == Token.Return;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      SourceLocationBuilder slb = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
      this.GetNextToken();
      Expression/*?*/ expr = null;
      if (this.currentToken != Token.Semicolon) {
        expr = this.ParseExpression(followers|Token.Semicolon);
        slb.UpdateToSpan(expr.SourceLocation);
      }
      Statement result = new ReturnStatement(expr, slb);
      this.SkipSemiColon(followers);
      return result;
    }

    private Statement ParseGoto(TokenSet followers)
      //^ requires this.currentToken == Token.Goto;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      SourceLocationBuilder slb = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
      this.GetNextToken();
      Statement result;
      switch (this.currentToken) {
        case Token.Case:
          this.GetNextToken();
          Expression caseLabel = this.ParseExpression(followers|Token.Semicolon);
          slb.UpdateToSpan(caseLabel.SourceLocation);
          result = new GotoSwitchCaseStatement(caseLabel, slb);
          break;
        case Token.Default:
          slb.UpdateToSpan(this.scanner.SourceLocationOfLastScannedToken);
          result = new GotoSwitchCaseStatement(null, slb);
          this.GetNextToken();
          break;
        default:
          result = new GotoStatement(this.ParseSimpleName(followers), slb);
          break;
      }
      this.SkipSemiColon(followers);
      return result;
    }

    private Statement ParseContinue(TokenSet followers)
      //^ requires this.currentToken == Token.Continue;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      ISourceLocation sourceLocation = this.scanner.SourceLocationOfLastScannedToken;
      this.GetNextToken();
      Statement result = new ContinueStatement(sourceLocation);
      this.SkipSemiColon(followers);
      return result;
    }

    private Statement ParseBreak(TokenSet followers)       
      //^ requires this.currentToken == Token.Break;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      ISourceLocation sourceLocation = this.scanner.SourceLocationOfLastScannedToken;
      this.GetNextToken();
      Statement result = new BreakStatement(sourceLocation);
      this.SkipSemiColon(followers);
      return result;
    }

    private Statement ParseForEach(TokenSet followers)
      //^ requires this.currentToken == Token.Foreach;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      SourceLocationBuilder slb = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
      this.GetNextToken();
      this.Skip(Token.LeftParenthesis);
      TypeExpression variableType = this.ParseTypeExpression(false, false, followers|Parser.IdentifierOrNonReservedKeyword|Token.In|Token.RightParenthesis);
      if (this.currentToken == Token.In)
        this.HandleError(Error.BadForeachDecl);
      NameDeclaration variableName = this.ParseNameDeclaration();
      this.Skip(Token.In);
      Expression collection = this.ParseExpression(followers|Token.RightParenthesis);
      this.Skip(Token.RightParenthesis);
      Statement body = this.ParseStatement(followers);
      slb.UpdateToSpan(body.SourceLocation);
      ForEachStatement result = new ForEachStatement(variableType, variableName, collection, body, slb);
      //^ assume followers[this.currentToken] || this.currentToken == Token.EndOfFile;
      return result;
    }

    private Statement ParseFor(TokenSet followers)
      //^ requires this.currentToken == Token.For;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      SourceLocationBuilder slb = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
      this.GetNextToken();
      this.Skip(Token.LeftParenthesis);
      TokenSet followersOrRightParenthesisOrSemicolon = followers|Parser.RightParenthesisOrSemicolon;
      List<Statement> initStatements = this.ParseForInitializer(followersOrRightParenthesisOrSemicolon);
      Expression condition = this.ParseExpression(followersOrRightParenthesisOrSemicolon);
      this.Skip(Token.Semicolon);
      List<Statement> incrementStatements = this.ParseForIncrementer(followers | Token.RightParenthesis);
      this.Skip(Token.RightParenthesis);
      Statement body = this.ParseStatement(followers);
      slb.UpdateToSpan(body.SourceLocation);
      ForStatement result = new ForStatement(initStatements, condition, incrementStatements, body, slb);
      //^ assume followers[this.currentToken] || this.currentToken == Token.EndOfFile;
      return result;
    }

    private List<Statement> ParseForInitializer(TokenSet followers)
      //^ ensures followers[this.currentToken] || this.currentToken == Token.Semicolon || this.currentToken == Token.RightParenthesis || this.currentToken == Token.EndOfFile;
    {
      List<Statement> statements = new List<Statement>(1);
      if (this.currentToken == Token.Semicolon) {
        this.GetNextToken();
        statements.TrimExcess();
        //^ assume this.currentToken == Token.Semicolon;
        return statements;
      }
      if (this.currentToken == Token.RightParenthesis) {
        this.Skip(Token.Semicolon);
        statements.TrimExcess();
        //^ assume this.currentToken == Token.RightParenthesis;
        return statements;
      }
      TokenSet followerOrComma = followers|Token.Comma;
      for (; ; ) {
        //^ assume followerOrComma[Token.Comma];
        Statement s = this.ParseExpressionStatementOrDeclaration(true, false, followerOrComma, true);
        statements.Add(s);
        if (s is LocalDeclarationsStatement) {
          if (statements.Count > 1)
            this.HandleError(s.SourceLocation, Error.ExpectedExpression);
        } else {
          ExpressionStatement es = (ExpressionStatement)s;
          Expression e = es.Expression;
          if (!(e is Assignment || e is BinaryOperationAssignment || e is MethodCall || e is UnaryOperationAssignment || e is CreateObjectInstance))
            this.HandleError(e.SourceLocation, Error.IllegalStatement);
        }
        //^ assume followers[this.currentToken] || this.currentToken == Token.Comma || this.currentToken == Token.EndOfFile;
        if (this.currentToken != Token.Comma) break;
        this.GetNextToken();
      }
      //^ assert followers[this.currentToken] || this.currentToken == Token.EndOfFile;
      statements.TrimExcess();
      //^ assume followers[this.currentToken] || this.currentToken == Token.EndOfFile;
      return statements;
    }

    private List<Statement> ParseForIncrementer(TokenSet followers)
      //^ ensures followers[this.currentToken] || this.currentToken == Token.RightParenthesis || this.currentToken == Token.EndOfFile;
    {
      List<Statement> statements = new List<Statement>(1);
      if (this.currentToken == Token.RightParenthesis) {
        statements.TrimExcess();
        //^ assume this.currentToken == Token.RightParenthesis;
        return statements;
      }
      TokenSet followerOrComma = followers|Token.Comma;
      for (; ; ) {
        Expression e = this.ParseExpression(followerOrComma);
        if (!(e is Assignment || e is BinaryOperationAssignment || e is MethodCall || e is UnaryOperationAssignment || e is CreateObjectInstance))
          this.HandleError(e.SourceLocation, Error.IllegalStatement);
        statements.Add(new ExpressionStatement(e));
        //^ assume followers[this.currentToken] || this.currentToken == Token.Comma || this.currentToken == Token.EndOfFile;
        if (this.currentToken != Token.Comma) break;
        this.GetNextToken();
      }
      //^ assert followers[this.currentToken] || this.currentToken == Token.EndOfFile;
      statements.TrimExcess();
      //^ assume followers[this.currentToken] || this.currentToken == Token.EndOfFile;
      return statements;
    }

    private Statement ParseDoWhile(TokenSet followers)
      //^ requires this.currentToken == Token.Do;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      SourceLocationBuilder slb = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
      this.GetNextToken();
      Statement body = this.ParseStatement(followers|Token.While);
      if (body is EmptyStatement)
        this.HandleError(body.SourceLocation, Error.PossibleMistakenNullStatement);
      this.Skip(Token.While);
      Expression condition = this.ParseParenthesizedExpression(false, followers|Token.Semicolon);
      DoWhileStatement result = new DoWhileStatement(body, condition, slb);
      this.SkipSemiColon(followers);
      return result;
    }

    private Statement ParseWhile(TokenSet followers) 
      //^ requires this.currentToken == Token.While;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      SourceLocationBuilder slb = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
      this.GetNextToken();
      Expression condition = this.ParseParenthesizedExpression(false, followers);
      Statement body = this.ParseStatement(followers);
      slb.UpdateToSpan(body.SourceLocation);
      WhileDoStatement result = new WhileDoStatement(condition, body, slb);
      this.SkipTo(followers);
      return result;
    }

    private Statement ParseSwitch(TokenSet followers)
      //^ requires this.currentToken == Token.Switch;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      SourceLocationBuilder slb = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
      this.GetNextToken();
      Expression expression = this.ParseParenthesizedExpression(false, followers|Token.LeftBrace);
      List<SwitchCase> cases = new List<SwitchCase>();
      this.Skip(Token.LeftBrace);
      TokenSet followersOrCaseOrColonOrDefaultOrRightBrace = followers|Parser.CaseOrColonOrDefaultOrRightBrace;
      TokenSet followersOrCaseOrDefaultOrRightBrace = followers|Parser.CaseOrDefaultOrRightBrace;
      for (; ; ) {
        SourceLocationBuilder scCtx = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
        Expression/*?*/ scExpression = null;
        switch (this.currentToken) {
          case Token.Case:
            this.GetNextToken();
            if (this.currentToken == Token.Colon)
              this.HandleError(Error.ConstantExpected);
            else {
              scExpression = this.ParseExpression(followersOrCaseOrColonOrDefaultOrRightBrace);
              scCtx.UpdateToSpan(scExpression.SourceLocation);
            }
            break;
          case Token.Default: //Parse these as many times as they occur. Checker will report the error.
            this.GetNextToken();
            break;
          default:
            if (Parser.StatementStart[this.currentToken]) {
              this.HandleError(Error.StmtNotInCase);
              this.ParseStatement(followersOrCaseOrColonOrDefaultOrRightBrace);
              continue;
            }
            goto done;
        }
        this.Skip(Token.Colon);
        IEnumerable<Statement> scBody;
        if (Parser.StatementStart[this.currentToken])
          scBody = this.ParseSwitchCaseStatementBlock(scCtx, followersOrCaseOrDefaultOrRightBrace);
        else
          scBody = IteratorHelper.GetEmptyEnumerable<Statement>();
        cases.Add(new SwitchCase(scExpression, scBody, scCtx));
      }
    done:
      if (cases.Count == 0) {
        this.HandleError(Error.EmptySwitch);
      } else {
        // add SwitchCaseBottom to last case if it happened to have no statements.
        SwitchCase lastCase = cases[cases.Count-1];
        if (lastCase != null && !lastCase.Body.GetEnumerator().MoveNext()) {
          List<Statement> body = new List<Statement>(1);
          body.Add(new EmptyStatement(true, lastCase.SourceLocation));
          cases[cases.Count-1] = new SwitchCase(lastCase.IsDefault ? null : lastCase.Expression, body.AsReadOnly(), lastCase.SourceLocation);
        }
      }

      cases.TrimExcess();
      slb.UpdateToSpan(this.scanner.SourceLocationOfLastScannedToken);
      SwitchStatement result = new SwitchStatement(expression, cases, slb);
      this.SkipOverTo(Token.RightBrace, followers);
      return result;
    }

    private IEnumerable<Statement> ParseSwitchCaseStatementBlock(SourceLocationBuilder switchCaseContext, TokenSet followers)
      //^ requires Parser.StatementStart[this.currentToken];
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      List<Statement> statements = new List<Statement>();
      while (Parser.StatementStart[this.currentToken]) {
        if (this.currentToken == Token.Default) {
          if (this.PeekNextToken() != Token.LeftParenthesis) break;
        }
        statements.Add(this.ParseStatement(followers));
      }
      if (statements.Count > 0) {
        ISourceLocation sctx = statements[statements.Count-1].SourceLocation;
        switchCaseContext.UpdateToSpan(sctx);
        statements.Add(new EmptyStatement(true, sctx));
      }
      statements.TrimExcess();
      IEnumerable<Statement> result = statements.AsReadOnly();
      //^ assume followers[this.currentToken] || this.currentToken == Token.EndOfFile;
      return result;
    }

    private Statement ParseIf(TokenSet followers)       
      //^ requires this.currentToken == Token.If;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      SourceLocationBuilder slb = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
      this.GetNextToken();
      Expression ifCondition = this.ParseParenthesizedExpression(false, followers|Parser.StatementStart);
      Statement ifTrue = this.ParseStatement(followers|Token.Else);
      if (ifTrue is EmptyStatement)
        this.HandleError(ifTrue.SourceLocation, Error.PossibleMistakenNullStatement);
      Statement ifFalse;
      if (this.currentToken == Token.Else) {
        this.GetNextToken();
        ifFalse = this.ParseStatement(followers);
        if (ifFalse is EmptyStatement)
          this.HandleError(ifFalse.SourceLocation, Error.PossibleMistakenNullStatement);
      } else {
        ifFalse = new EmptyStatement(false, ifTrue.SourceLocation);
      }
      slb.UpdateToSpan(ifFalse.SourceLocation);
      Statement result = new ConditionalStatement(ifCondition, ifTrue, ifFalse, slb);
      this.SkipTo(followers);
      return result;
    }

    private Statement ParseEmptyStatement(TokenSet followers)
      //^ requires this.currentToken == Token.Semicolon;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      EmptyStatement result = new EmptyStatement(false, this.scanner.SourceLocationOfLastScannedToken);
      this.GetNextToken();
      this.SkipTo(followers);
      return result;
    }

    private Expression ParseExpression(TokenSet followers)
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      TokenSet followersOrInfixOperators = followers|Parser.InfixOperators;
      Expression operand1 = this.ParseUnaryExpression(followersOrInfixOperators);
      if (!Parser.InfixOperators[this.currentToken]) {
        this.SkipTo(followers);
        return operand1;
      }
      if (this.currentToken == Token.Conditional)
        return this.ParseConditional(operand1, followers);
      else
        return this.ParseAssignmentExpression(operand1, followers);
    }

    private Expression ParseAssignmentExpression(Expression operand1, TokenSet followers) 
      //^ requires Parser.InfixOperators[this.currentToken];
      //^ requires this.currentToken != Token.Conditional;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      switch (this.currentToken) {
        case Token.PlusAssign:
        case Token.Assign:
        case Token.BitwiseAndAssign:
        case Token.BitwiseOrAssign:
        case Token.BitwiseXorAssign:
        case Token.DivideAssign:
        case Token.LeftShiftAssign:
        case Token.MultiplyAssign:
        case Token.RemainderAssign:
        case Token.RightShiftAssign:
        case Token.SubtractAssign:
          SourceLocationBuilder slb = new SourceLocationBuilder(operand1.SourceLocation);
          Token operatorToken = this.currentToken;
          this.GetNextToken();
          TargetExpression target = new TargetExpression(operand1);
          Expression operand2 = this.ParseExpression(followers);
          slb.UpdateToSpan(operand2.SourceLocation);
          //^ assume followers[this.currentToken] || this.currentToken == Token.EndOfFile;
          switch (operatorToken) {
            case Token.PlusAssign: return new AdditionAssignment(target, operand2, slb);
            case Token.BitwiseAndAssign: return new BitwiseAndAssignment(target, operand2, slb);
            case Token.BitwiseOrAssign: return new BitwiseOrAssignment(target, operand2, slb);
            case Token.BitwiseXorAssign: return new ExclusiveOrAssignment(target, operand2, slb);
            case Token.DivideAssign: return new DivisionAssignment(target, operand2, slb);
            case Token.LeftShiftAssign: return new LeftShiftAssignment(target, operand2, slb);
            case Token.MultiplyAssign: return new MultiplicationAssignment(target, operand2, slb);
            case Token.RemainderAssign: return new ModulusAssignment(target, operand2, slb);
            case Token.RightShiftAssign: return new RightShiftAssignment(target, operand2, slb);
            case Token.SubtractAssign: return new SubtractionAssignment(target, operand2, slb);
            default: return new Assignment(target, operand2, slb);
          }
        default:
          operand1 = this.ParseBinaryExpression(operand1, followers|Token.Conditional);
          if (this.currentToken == Token.Conditional)
            return this.ParseConditional(operand1, followers);
          //^ assume followers[this.currentToken] || this.currentToken == Token.EndOfFile;
          return operand1;
      }
    }

    private Expression ParseBinaryExpression(Expression operand1, TokenSet followers)
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      TokenSet unaryFollowers = followers|Parser.InfixOperators;
      Expression expression;
      switch (this.currentToken) {
        case Token.Plus:
        case Token.As:
        case Token.BitwiseAnd:
        case Token.BitwiseOr:
        case Token.BitwiseXor:
        case Token.Divide:
        case Token.Equal:
        case Token.GreaterThan:
        case Token.GreaterThanOrEqual:
        //case Token.Iff:
        //case Token.In:
        //case Token.Implies:
        case Token.Is:
        case Token.LeftShift:
        case Token.LessThan:
        case Token.LessThanOrEqual:
        case Token.LogicalAnd:
        case Token.LogicalOr:
        //case Token.Maplet:
        case Token.Multiply:
        case Token.NotEqual:
        case Token.NullCoalescing:
        //case Token.Range:
        case Token.Remainder:
        case Token.RightShift:
        case Token.Subtract:
          Token operator1 = this.currentToken;
          this.GetNextToken();
          Expression operand2;
          if (operator1 == Token.Is || operator1 == Token.As) {
            operand2 = this.ParseTypeExpression(operator1 == Token.Is, false, followers);
            expression = AllocateBinaryExpression(operand1, operand2, operator1);
            //^ assume followers[this.currentToken] || this.currentToken == Token.EndOfFile;
            return expression;
          }
          operand2 = this.ParseUnaryExpression(unaryFollowers);
          switch (this.currentToken) {
            case Token.Plus:
            case Token.As:
            case Token.BitwiseAnd:
            case Token.BitwiseOr:
            case Token.BitwiseXor:
            case Token.Divide:
            case Token.Equal:
            case Token.GreaterThan:
            case Token.GreaterThanOrEqual:
            //case Token.Iff:
            //case Token.Implies:
            //case Token.In:
            case Token.Is:
            case Token.LeftShift:
            case Token.LessThan:
            case Token.LessThanOrEqual:
            case Token.LogicalAnd:
            case Token.LogicalOr:
            //case Token.Maplet:
            case Token.Multiply:
            case Token.NotEqual:
            case Token.NullCoalescing:
            //case Token.Range:
            case Token.Remainder:
            case Token.RightShift:
            case Token.Subtract:
              expression = this.ParseComplexExpression(Token.None, operand1, operator1, operand2, unaryFollowers);
              break;
            default:
              expression = AllocateBinaryExpression(operand1, operand2, operator1);
              break;
          }
          break;
        default:
          expression = operand1;
          break;
      }
      this.SkipTo(followers);
      return expression;
    }

    private static Expression AllocateBinaryExpression(Expression operand1, Expression operand2, Token operatorToken) 
      //^ requires (operatorToken == Token.As || operatorToken == Token.Is) ==> operand2 is TypeExpression;
    {
      SourceLocationBuilder slb = new SourceLocationBuilder(operand1.SourceLocation);
      slb.UpdateToSpan(operand2.SourceLocation);
      switch (operatorToken) {
        case Token.Plus: return new Addition(operand1, operand2, slb);
        case Token.As: return new CastIfPossible(operand1, (TypeExpression)operand2, slb);
        case Token.BitwiseAnd: return new BitwiseAnd(operand1, operand2, slb);
        case Token.BitwiseOr: return new BitwiseOr(operand1, operand2, slb);
        case Token.BitwiseXor: return new ExclusiveOr(operand1, operand2, slb);
        case Token.Divide: return new Division(operand1, operand2, slb);
        case Token.Equal: return new Equality(operand1, operand2, slb);
        case Token.GreaterThan: return new GreaterThan(operand1, operand2, slb);
        case Token.GreaterThanOrEqual: return new GreaterThanOrEqual(operand1, operand2, slb);
        //case Token.Iff: return new Addition(operand1, operand2, slb);
        //case Token.In: return new Addition(operand1, operand2, slb);
        //case Token.Implies: return new Addition(operand1, operand2, slb);
        case Token.Is: return new CheckIfInstance(operand1, (TypeExpression)operand2, slb);
        case Token.LeftShift: return new LeftShift(operand1, operand2, slb);
        case Token.LessThan: return new LessThan(operand1, operand2, slb);
        case Token.LessThanOrEqual: return new LessThanOrEqual(operand1, operand2, slb);
        case Token.LogicalAnd: return new LogicalAnd(operand1, operand2, slb);
        case Token.LogicalOr: return new LogicalOr(operand1, operand2, slb);
        //case Token.Maplet: return new Addition(operand1, operand2, slb);
        case Token.Multiply: return new Multiplication(operand1, operand2, slb);
        case Token.NotEqual: return new NotEquality(operand1, operand2, slb);
        case Token.NullCoalescing: return new NullCoalescing(operand1, operand2, slb);
        //case Token.Range: return new Addition(operand1, operand2, slb);
        case Token.Remainder: return new Modulus(operand1, operand2, slb);
        case Token.RightShift: return new RightShift(operand1, operand2, slb);
        case Token.Subtract: return new Subtraction(operand1, operand2, slb);
        default:
          //^ assume false;
          goto case Token.Plus;
      }
    }

    private Expression ParseComplexExpression(Token operator0, Expression operand1, Token operator1, Expression operand2, TokenSet followers)
      //^ requires this.currentToken != Token.EndOfFile;
      //^ requires operator1 != Token.As && operator1 != Token.Is;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
    restart:
      //^ assume this.currentToken != Token.EndOfFile; //OK because of precondition and state at point where control comes back here
      Token operator2 = this.currentToken;
      this.GetNextToken();
      Expression operand3;
      if (operator2 == Token.Is || operator2 == Token.As) 
        operand3 = this.ParseTypeExpression(operator2 == Token.Is, false, followers);
      else
        operand3 = this.ParseUnaryExpression(followers);
      if (Parser.LowerPriority(operator1, operator2)) {
        switch (this.currentToken) {
          case Token.Plus:
          case Token.As:
          case Token.BitwiseAnd:
          case Token.BitwiseOr:
          case Token.BitwiseXor:
          case Token.Divide:
          case Token.Equal:
          case Token.GreaterThan:
          case Token.GreaterThanOrEqual:
          //case Token.Iff:
          //case Token.Implies:
          //case Token.In:
          case Token.Is:
          case Token.LeftShift:
          case Token.LessThan:
          case Token.LessThanOrEqual:
          case Token.LogicalAnd:
          case Token.LogicalOr:
          //case Token.Maplet:
          case Token.Multiply:
          case Token.NotEqual:
          case Token.NullCoalescing:
          case Token.Range:
          case Token.Remainder:
          case Token.RightShift:
          case Token.Subtract:
            if (Parser.LowerPriority(operator2, this.currentToken) && operator2 != Token.As && operator2 != Token.Is) {
              //Can't reduce just operand2 op2 operand3 because there is an op3 with priority over op2
              //^ assume this.currentToken != Token.EndOfFile; //follows from the switch logic
              operand2 = this.ParseComplexExpression(operator1, operand2, operator2, operand3, followers); //reduce complex expression
              //Now either at the end of the entire expression, or at an operator that is at the same or lower priority than op1
              //Either way, operand2 op2 operand3 op3 ... has been reduced to just operand2 and the code below will
              //either restart this procedure to parse the remaining expression or reduce operand1 op1 operand2 and return to the caller
            } else
              goto default;
            break;
          default:
            //Reduce operand2 op2 operand3. There either is no further binary operator, or it does not take priority over op2.
            //^ assert (operator2 == Token.As || operator2 == Token.Is) ==> operand3 is TypeExpression; 
            operand2 = AllocateBinaryExpression(operand2, operand3, operator2);
            //The code following this will reduce operand1 op1 operand2 and return to the caller
            break;
        }
      } else {
        //^ assert (operator2 == Token.As || operator2 == Token.Is) ==> operand3 is TypeExpression; 
        //^ assume operator1 != Token.As && operator1 != Token.Is; //follows from precondition
        operand1 = AllocateBinaryExpression(operand1, operand2, operator1);
        operand2 = operand3;
        operator1 = operator2;
        //^ assert (operator1 == Token.As || operator1 == Token.Is) ==> operand2 is TypeExpression; 
      }
      //At this point either operand1 op1 operand2 has been reduced, or operand2 op2 operand3 .... has been reduced, so back to just two operands
      switch (this.currentToken) {
        case Token.Plus:
        case Token.As:
        case Token.BitwiseAnd:
        case Token.BitwiseOr:
        case Token.BitwiseXor:
        case Token.Divide:
        case Token.Equal:
        case Token.GreaterThan:
        case Token.GreaterThanOrEqual:
        //case Token.Iff:
        //case Token.Implies:
        //case Token.In:
        case Token.Is:
        case Token.LeftShift:
        case Token.LessThan:
        case Token.LessThanOrEqual:
        case Token.LogicalAnd:
        case Token.LogicalOr:
        //case Token.Maplet:
        case Token.Multiply:
        case Token.NotEqual:
        case Token.NullCoalescing:
        case Token.Range:
        case Token.Remainder:
        case Token.RightShift:
        case Token.Subtract:
          if (operator0 == Token.None || Parser.LowerPriority(operator0, this.currentToken))
            //The caller is not prepared to deal with the current token, go back to the start of this routine and consume some more tokens
            goto restart;
          else
            goto default; //Let the caller deal with the current token
        default:
          //reduce operand1 op1 operand2 and return to caller
          //^ assume (operator1 == Token.As || operator1 == Token.Is) ==> operand2 is TypeExpression; 
          return AllocateBinaryExpression(operand1, operand2, operator1);
      }
    }

    /// <summary>
    /// returns true if opnd1 operator1 opnd2 operator2 opnd3 implicitly brackets as opnd1 operator1 (opnd2 operator2 opnd3)
    /// </summary>
    private static bool LowerPriority(Token operator1, Token operator2) {
      switch (operator1) {
        case Token.Divide:
        case Token.Multiply:
        case Token.Remainder:
          switch (operator2) {
            default:
              return false;
          }
        case Token.Plus:
        case Token.Subtract:
          switch (operator2) {
            case Token.Divide:
            case Token.Multiply:
            case Token.Remainder:
              return true;
            default:
              return false;
          }
        case Token.LeftShift:
        case Token.RightShift:
          switch (operator2) {
            case Token.Divide:
            case Token.Multiply:
            case Token.Remainder:
            case Token.Plus:
            case Token.Subtract:
              return true;
            default:
              return false;
          }
        case Token.As:
        case Token.GreaterThan:
        case Token.GreaterThanOrEqual:
        case Token.Is:
        case Token.LessThan:
        case Token.LessThanOrEqual:
          switch (operator2) {
            case Token.Divide:
            case Token.Multiply:
            case Token.Remainder:
            case Token.Plus:
            case Token.Subtract:
            case Token.LeftShift:
            case Token.RightShift:
              return true;
            default:
              return false;
          }
        case Token.Equal:
        case Token.NotEqual:
        case Token.Maplet:
        case Token.Range:
          switch (operator2) {
            case Token.Divide:
            case Token.Multiply:
            case Token.Remainder:
            case Token.Plus:
            case Token.Subtract:
            case Token.LeftShift:
            case Token.RightShift:
            case Token.As:
            case Token.GreaterThan:
            case Token.GreaterThanOrEqual:
            case Token.Is:
            case Token.LessThan:
            case Token.LessThanOrEqual:
              return true;
            default:
              return false;
          }
        case Token.BitwiseAnd:
          switch (operator2) {
            case Token.Divide:
            case Token.Multiply:
            case Token.Remainder:
            case Token.Plus:
            case Token.Subtract:
            case Token.LeftShift:
            case Token.RightShift:
            case Token.As:
            case Token.GreaterThan:
            case Token.GreaterThanOrEqual:
            case Token.Is:
            case Token.LessThan:
            case Token.LessThanOrEqual:
            case Token.Maplet:
            case Token.Range:
            case Token.Equal:
            case Token.NotEqual:
              return true;
            default:
              return false;
          }
        case Token.BitwiseXor:
          switch (operator2) {
            case Token.Divide:
            case Token.Multiply:
            case Token.Remainder:
            case Token.Plus:
            case Token.Subtract:
            case Token.LeftShift:
            case Token.RightShift:
            case Token.As:
            case Token.GreaterThan:
            case Token.GreaterThanOrEqual:
            case Token.Is:
            case Token.LessThan:
            case Token.LessThanOrEqual:
            case Token.Maplet:
            case Token.Range:
            case Token.Equal:
            case Token.NotEqual:
            case Token.BitwiseAnd:
              return true;
            default:
              return false;
          }
        case Token.BitwiseOr:
          switch (operator2) {
            case Token.Divide:
            case Token.Multiply:
            case Token.Remainder:
            case Token.Plus:
            case Token.Subtract:
            case Token.LeftShift:
            case Token.RightShift:
            case Token.As:
            case Token.GreaterThan:
            case Token.GreaterThanOrEqual:
            case Token.Is:
            case Token.LessThan:
            case Token.LessThanOrEqual:
            case Token.Range:
            case Token.Maplet:
            case Token.Equal:
            case Token.NotEqual:
            case Token.BitwiseAnd:
            case Token.BitwiseXor:
              return true;
            default:
              return false;
          }
        case Token.LogicalAnd:
          switch (operator2) {
            case Token.Divide:
            case Token.Multiply:
            case Token.Remainder:
            case Token.Plus:
            case Token.Subtract:
            case Token.LeftShift:
            case Token.RightShift:
            case Token.As:
            case Token.GreaterThan:
            case Token.GreaterThanOrEqual:
            case Token.Is:
            case Token.LessThan:
            case Token.LessThanOrEqual:
            case Token.Maplet:
            case Token.Equal:
            case Token.NotEqual:
            case Token.BitwiseAnd:
            case Token.BitwiseXor:
            case Token.BitwiseOr:
              return true;
            default:
              return false;
          }
        case Token.LogicalOr:
          switch (operator2) {
            case Token.Divide:
            case Token.Multiply:
            case Token.Remainder:
            case Token.Plus:
            case Token.Subtract:
            case Token.LeftShift:
            case Token.RightShift:
            case Token.As:
            case Token.GreaterThan:
            case Token.GreaterThanOrEqual:
            case Token.Is:
            case Token.LessThan:
            case Token.LessThanOrEqual:
            case Token.Maplet:
            case Token.Range:
            case Token.Equal:
            case Token.NotEqual:
            case Token.BitwiseAnd:
            case Token.BitwiseXor:
            case Token.BitwiseOr:
            case Token.LogicalAnd:
              return true;
            default:
              return false;
          }
        case Token.NullCoalescing:
          switch (operator2) {
            case Token.Divide:
            case Token.Multiply:
            case Token.Remainder:
            case Token.Plus:
            case Token.Subtract:
            case Token.LeftShift:
            case Token.RightShift:
            case Token.As:
            case Token.GreaterThan:
            case Token.GreaterThanOrEqual:
            case Token.Is:
            case Token.LessThan:
            case Token.LessThanOrEqual:
            case Token.Maplet:
            case Token.Range:
            case Token.Equal:
            case Token.NotEqual:
            case Token.BitwiseAnd:
            case Token.BitwiseXor:
            case Token.BitwiseOr:
            case Token.LogicalAnd:
            case Token.LogicalOr:
            case Token.NullCoalescing:
              return true;
            default:
              return false;
          }
        case Token.Implies:
          switch (operator2) {
            case Token.Iff:
              return false;
            default:
              return true;
          }
        case Token.Iff:
          return true;
      }
      //^ assume false;
      return false;
    }

    private Expression ParseConditional(Expression condition, TokenSet followers) 
      //^ requires this.currentToken == Token.Conditional;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      this.GetNextToken();
      SourceLocationBuilder slb = new SourceLocationBuilder(condition.SourceLocation);
      Expression resultIfTrue = this.ParseExpression(followers|Token.Colon);
      Expression resultIfFalse;
      if (this.currentToken == Token.Colon) {
        this.GetNextToken();
        resultIfFalse = this.ParseExpression(followers);
      } else {
        this.Skip(Token.Colon); //gives appropriate error message
        if (!followers[this.currentToken])
          //Assume that only the : is missing. Go ahead as if it were specified.
          resultIfFalse = this.ParseExpression(followers);
        else
          resultIfFalse = this.ParseDummyExpression();
      }
      slb.UpdateToSpan(resultIfFalse.SourceLocation);
      Expression result = new Conditional(condition, resultIfTrue, resultIfFalse, slb);
      this.SkipTo(followers);
      return result;
    }

    private Expression ParseDummyExpression() {
      ISourceLocation currentLocation = this.scanner.SourceLocationOfLastScannedToken;
      return new CompileTimeConstant(null, currentLocation.SourceDocument.GetSourceLocation(currentLocation.StartIndex, 0));
    }

    private Expression ParseUnaryExpression(TokenSet followers)
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      switch (this.currentToken) {
        case Token.AddOne:
        case Token.BitwiseAnd:
        case Token.BitwiseNot:
        case Token.LogicalNot:
        case Token.Multiply:
        case Token.Plus:
        case Token.Subtract:
        case Token.SubtractOne:
          SourceLocationBuilder slb = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
          Token operatorToken = this.currentToken;
          this.GetNextToken();
          Expression operand = this.ParseUnaryExpression(followers);
          slb.UpdateToSpan(operand.SourceLocation);
          Expression result = AllocateUnaryExpression(operatorToken, operand, slb);
          //^ assume followers[this.currentToken] || this.currentToken == Token.EndOfFile;
          return result;
        //case Token.LeftParenthesis:
        //  return this.ParseCastExpression(followers);
        default:
          return this.ParsePrimaryExpression(followers);
      }
    }

    private static Expression AllocateUnaryExpression(Token operatorToken, Expression operand, SourceLocationBuilder slb)
      //^ requires operatorToken == Token.AddOne || operatorToken == Token.BitwiseAnd || operatorToken == Token.BitwiseNot || operatorToken == Token.LogicalNot || 
      //^ operatorToken == Token.Multiply || operatorToken == Token.Plus || operatorToken == Token.Subtract || operatorToken == Token.SubtractOne;
    {
      switch (operatorToken) {
        case Token.AddOne: return new PrefixIncrement(new TargetExpression(operand), slb);
        case Token.BitwiseAnd: return new AddressOf(new AddressableExpression(operand), slb);
        case Token.BitwiseNot: return new OnesComplement(operand, slb);
        case Token.LogicalNot: return new LogicalNot(operand, slb);
        case Token.Multiply: return new AddressDereference(operand, slb);
        case Token.Plus: return new UnaryPlus(operand, slb);
        case Token.Subtract: return new UnaryNegation(operand, slb);
        case Token.SubtractOne: return new PrefixDecrement(new TargetExpression(operand), slb);
        default:
          //^ assume false;
          goto case Token.AddOne;
      }
    }

    private Expression ParsePrimaryExpression(TokenSet followers)
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      ISourceLocation sctx = this.scanner.SourceLocationOfLastScannedToken;
      Expression expression = new DummyExpression(sctx);
      switch (this.currentToken) {
        case Token.ArgList:
          this.GetNextToken();
          expression = new RuntimeArgumentHandleExpression(sctx);
          break;
        case Token.Delegate:
          expression = this.ParseAnonymousMethod(followers);
          break;
        case Token.New:
          expression = this.ParseNew(followers|Token.Dot|Token.LeftBracket|Token.Arrow);
          break;
        case Token.Identifier:
          expression = this.ParseSimpleName(followers|Token.Dot|Token.DoubleColon|Token.Lambda|Token.LeftParenthesis);
          if (this.currentToken == Token.DoubleColon) {
            if (((SimpleName)expression).Name == this.nameTable.global)
              expression = new RootNamespaceExpression(expression.SourceLocation);
            expression = this.ParseQualifiedName(expression, followers|Token.Dot|Token.LessThan|Token.LeftParenthesis|Token.AddOne|Token.SubtractOne);
          } else if (this.currentToken == Token.Lambda) {
            expression = this.ParseLambda((SimpleName)expression, followers);
          }
          break;
        case Token.Null:
          expression = new NullLiteral(sctx);
          this.GetNextToken();
          break;
        case Token.True:
          expression = new CompileTimeConstant(true, false, sctx);
          this.GetNextToken();
          break;
        case Token.False:
          expression = new CompileTimeConstant(false, false, sctx);
          this.GetNextToken();
          break;
        case Token.CharLiteral:
          expression = new CompileTimeConstant(this.scanner.charLiteralValue, false, sctx);
          this.GetNextToken();
          break;
        case Token.HexLiteral:
          expression = this.ParseHexLiteral();
          break;
        case Token.IntegerLiteral:
          expression = this.ParseIntegerLiteral();
          break;
        case Token.RealLiteral:
          expression = this.ParseRealLiteral();
          break;
        case Token.StringLiteral:
          expression = new CompileTimeConstant(this.scanner.GetString(), false, sctx);
          this.GetNextToken();
          break;
        case Token.This:
          expression = new ThisReference(sctx);
          this.GetNextToken();
          break;
        case Token.Base:
          expression = new BaseClassReference(sctx);
          this.GetNextToken();
          break;
        case Token.Typeof:
        case Token.Sizeof:
        case Token.Default: 
          expression = this.ParseTypeofSizeofOrDefault(followers);
          break;
        case Token.Stackalloc:
          return this.ParseStackalloc(followers);
        case Token.Checked:
        case Token.MakeRef:
        case Token.RefType:
        case Token.Unchecked:
          expression = this.ParseCheckedOrMakeRefOrRefTypeOrUnchecked(followers);
          break;
        case Token.RefValue:
          expression = this.ParseGetValueOfTypedReference(followers);
          break;
        case Token.Bool:
        case Token.Decimal:
        case Token.Sbyte:
        case Token.Byte:
        case Token.Short:
        case Token.Ushort:
        case Token.Int:
        case Token.Uint:
        case Token.Long:
        case Token.Ulong:
        case Token.Char:
        case Token.Float:
        case Token.Double:
        case Token.Object:
        case Token.String:
          expression = this.RootQualifiedNameFor(this.currentToken, sctx);
          this.GetNextToken();
          break;
        case Token.LeftParenthesis:
          expression = this.ParseCastExpression(followers|Token.Dot|Token.LeftBracket|Token.Arrow);
          break;
        default:
          if (Parser.IdentifierOrNonReservedKeyword[this.currentToken]) goto case Token.Identifier;
          if (Parser.InfixOperators[this.currentToken]) {
            this.HandleError(Error.InvalidExprTerm, this.scanner.GetTokenSource());
            //^ assume this.currentToken != Token.EndOfFile; //should not be a member of InfixOperators
            this.GetNextToken();
          } else
            this.SkipTo(followers|Parser.PrimaryStart, Error.InvalidExprTerm, this.scanner.GetTokenSource());
          if (Parser.PrimaryStart[this.currentToken]) return this.ParsePrimaryExpression(followers);
          goto done;
      }

      expression = this.ParseIndexerCallOrSelector(expression, followers|Token.AddOne|Token.SubtractOne);
      for (; ; ) {
        switch (this.currentToken) {
          case Token.AddOne:
            SourceLocationBuilder slb = new SourceLocationBuilder(expression.SourceLocation);
            slb.UpdateToSpan(this.scanner.SourceLocationOfLastScannedToken);
            this.GetNextToken();
            expression = new PostfixIncrement(new TargetExpression(expression), slb);
            break;
          case Token.SubtractOne:
            slb = new SourceLocationBuilder(expression.SourceLocation);
            slb.UpdateToSpan(this.scanner.SourceLocationOfLastScannedToken);
            this.GetNextToken();
            expression = new PostfixDecrement(new TargetExpression(expression), slb);
            break;
          case Token.Arrow:
          case Token.Dot:
          case Token.LeftBracket:
            expression = this.ParseIndexerCallOrSelector(expression, followers|Token.AddOne|Token.SubtractOne);
            break;
          default:
            goto done;
        }
      }
    done:
      this.SkipTo(followers);
      return expression;
    }

    private Expression ParseLambda(TokenSet followers)
      //^ requires this.currentToken == Token.LeftParenthesis;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      SourceLocationBuilder slb = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
      List<LambdaParameter> parameters = this.ParseLambdaParameters(followers|Token.Lambda);
      //^ assume this.currentToken == Token.Lambda; //The caller is expected to guarantee this, but the precondition is awkward to write
      return this.ParseLambda(parameters, slb, followers);
    }

    private List<LambdaParameter> ParseLambdaParameters(TokenSet followers)
      //^ requires this.currentToken == Token.LeftParenthesis;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      List<LambdaParameter> result = new List<LambdaParameter>();
      this.GetNextToken();
      if (this.currentToken != Token.RightParenthesis) {
        LambdaParameter parameter = this.ParseLambdaParameter(true, followers|Token.Comma|Token.RightParenthesis);
        bool parametersAreTyped = parameter.ParameterType != null;
        result.Add(parameter);
        while (this.currentToken == Token.Comma) {
          this.GetNextToken();
          parameter = this.ParseLambdaParameter(parametersAreTyped, followers|Token.Comma|Token.RightParenthesis);
          if (parametersAreTyped && parameter.ParameterType == null)
            this.HandleError(parameter.ParameterName.SourceLocation, Error.TypeExpected);
          result.Add(parameter);
        }
      }
      result.TrimExcess();
      this.SkipOverTo(Token.RightParenthesis, followers);
      return result;
    }

    private LambdaParameter ParseLambdaParameter(bool allowType, TokenSet followers)
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      SourceLocationBuilder slb = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
      bool isOut = false;
      bool isRef = false;
      Token firstToken = this.currentToken;
      NameDeclaration parameterName = new NameDeclaration(this.GetNameFor(this.scanner.GetIdentifierString()), this.scanner.SourceLocationOfLastScannedToken);
      TypeExpression/*?*/ parameterType = null;
      if (allowType) {
        if (this.currentToken == Token.Out) { isOut = true; this.GetNextToken(); } 
        else if (this.currentToken == Token.Ref) { isRef = true; this.GetNextToken(); }
        parameterType = this.ParseTypeExpression(false, false, followers);
        if ((this.currentToken == Token.Comma || this.currentToken == Token.RightParenthesis) && Parser.IdentifierOrNonReservedKeyword[firstToken]) {
          parameterType = null;
        } else {
          parameterName = new NameDeclaration(this.GetNameFor(this.scanner.GetIdentifierString()), this.scanner.SourceLocationOfLastScannedToken);
        }
      }
      slb.UpdateToSpan(this.scanner.SourceLocationOfLastScannedToken);
      LambdaParameter result = new LambdaParameter(isOut, isRef, parameterType, parameterName, slb);
      if (!Parser.IdentifierOrNonReservedKeyword[this.currentToken])
        this.HandleError(Error.ExpectedIdentifier);
      else {
        //^ assume this.currentToken != Token.EndOfFile; //follows from definition of Parser.IdentifierOrNonReservedKeyword
        this.GetNextToken();
      }
      this.SkipTo(followers);
      return result;
    }

    private Expression ParseLambda(SimpleName parameterName, TokenSet followers)
      //^ requires this.currentToken == Token.Lambda;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      NameDeclaration paramName = new NameDeclaration(parameterName.Name, parameterName.SourceLocation);
      SourceLocationBuilder slb = new SourceLocationBuilder(parameterName.SourceLocation);
      LambdaParameter parameter = new LambdaParameter(false, false, null, paramName, parameterName.SourceLocation);
      List<LambdaParameter> parameters = new List<LambdaParameter>(1);
      parameters.Add(parameter);
      //^ assume this.currentToken == Token.Lambda; //follows from the precondition
      return this.ParseLambda(parameters, slb, followers);
    }

    private Expression ParseLambda(List<LambdaParameter> parameters, SourceLocationBuilder slb, TokenSet followers) 
      //^ requires this.currentToken == Token.Lambda;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      BlockStatement/*?*/ body = null;
      Expression/*?*/ expression = null;
      this.GetNextToken();
      if (this.currentToken == Token.LeftBrace)
        body = this.ParseBody(followers);
      else
        expression = this.ParseExpression(followers);
      return new Lambda(parameters, expression, body, slb);
    }

    private Expression ParseGetValueOfTypedReference(TokenSet followers)
      //^ requires this.currentToken == Token.RefValue;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      SourceLocationBuilder slb = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
      this.GetNextToken();
      this.Skip(Token.LeftParenthesis);
      Expression e = this.ParseExpression(followers|Token.Comma);
      this.Skip(Token.Comma);
      TypeExpression te = this.ParseTypeExpression(false, false, followers|Token.RightParenthesis);
      slb.UpdateToSpan(this.scanner.SourceLocationOfLastScannedToken);
      Expression result = new GetValueOfTypedReference(e, te, slb);
      this.SkipOverTo(Token.RightParenthesis, followers);
      return result;
    }

    private Expression ParseCheckedOrMakeRefOrRefTypeOrUnchecked(TokenSet followers)
      //^ requires this.currentToken == Token.Checked || this.currentToken == Token.MakeRef || this.currentToken == Token.RefType || this.currentToken == Token.Unchecked;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      SourceLocationBuilder slb = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
      Token tok = this.currentToken;
      this.GetNextToken();
      this.Skip(Token.LeftParenthesis);
      Expression operand = this.ParseExpression(followers|Token.RightParenthesis);
      Expression result;
      switch (tok) {
        case Token.Checked: result = new CheckedExpression(operand, slb); break;
        case Token.MakeRef: result = new MakeTypedReference(operand, slb); break;
        case Token.RefType: result = new GetTypeOfTypedReference(operand, slb); break;
        case Token.Unchecked: result = new UncheckedExpression(operand, slb); break;
        default:
          //^ assert false;
          goto case Token.Checked;
      }
      this.SkipOverTo(Token.RightParenthesis, followers);
      return result;
    }

    private Expression ParseStackalloc(TokenSet followers)
      //^ requires this.currentToken == Token.Stackalloc;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      SourceLocationBuilder slb = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
      this.GetNextToken();
      TypeExpression elementType = this.ParseBaseTypeExpression(false, followers|Token.LeftBracket);
      Token openingDelimiter = this.currentToken;
      if (this.currentToken != Token.LeftBracket) {
        this.HandleError(Error.BadStackAllocExpr);
        if (this.currentToken == Token.LeftParenthesis) this.GetNextToken();
      } else
        this.GetNextToken();
      Expression size = this.ParseExpression(followers|Token.RightBracket|Token.RightParenthesis);
      slb.UpdateToSpan(this.scanner.SourceLocationOfLastScannedToken);
      if (this.currentToken == Token.RightParenthesis && openingDelimiter == Token.LeftParenthesis)
        this.GetNextToken();
      else
        this.Skip(Token.RightBracket);
      Expression result = new CreateStackArray(elementType, size, slb);
      this.SkipTo(followers);
      return result;
    }

    private Expression ParseAnonymousMethod(TokenSet followers)
      //^ requires this.currentToken == Token.Delegate;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      SourceLocationBuilder slb = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
      this.GetNextToken();
      List<Ast.ParameterDeclaration> parameters = new List<Ast.ParameterDeclaration>();
      if (this.currentToken == Token.LeftParenthesis)
        this.ParseParameters(parameters, Token.RightParenthesis, followers);
      BlockStatement body = this.ParseBody(followers); //TODO: just parse a block
      //^ assert followers[this.currentToken] || this.currentToken == Token.EndOfFile;
      slb.UpdateToSpan(body.SourceLocation);
      Expression result = new AnonymousMethod(parameters, body, slb);
      //^ assume followers[this.currentToken] || this.currentToken == Token.EndOfFile;
      return result;
    }

    private Expression ParseNew(TokenSet followers) 
      //^ requires this.currentToken == Token.New;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      SourceLocationBuilder ctx = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
      this.GetNextToken();
      if (this.currentToken == Token.LeftBracket)
        return this.ParseNewImplicitlyTypedArray(ctx, followers);
      if (this.currentToken == Token.LeftBrace)
        return this.ParseNewAnonymousTypeInstance(ctx, followers);
      TypeExpression t = this.ParseBaseTypeExpression(false, followers|Parser.InfixOperators|Token.LeftBracket|Token.LeftParenthesis|Token.RightParenthesis);
      if (this.currentToken == Token.Conditional) {
        SourceLocationBuilder slb = new SourceLocationBuilder(t.SourceLocation);
        slb.UpdateToSpan(this.scanner.SourceLocationOfLastScannedToken);
        //^ assume this.currentToken == Token.Conditional; //no side effects from the methods can touch this.currentToken
        this.GetNextToken();
        t = new NullableTypeExpression(t, slb);
      //} else if (this.currentToken == Token.LogicalNot) {
      //  TypeExpression type = t;
      //  t = new NonNullableTypeExpression(type);
      //  t.SourceContext = type.SourceContext;
      //  t.SourceContext.EndPos = this.scanner.endPos;
      //  this.GetNextToken();
      } else if (this.currentToken == Token.Multiply) {
        SourceLocationBuilder slb = new SourceLocationBuilder(t.SourceLocation);
        slb.UpdateToSpan(this.scanner.SourceLocationOfLastScannedToken);
        //^ assume this.currentToken == Token.Multiply; //no side effects from the methods can touch this.currentToken
        this.GetNextToken();
        t = new PointerTypeExpression(t, slb);
      }
      ctx.UpdateToSpan(t.SourceLocation);
      TypeExpression et = t;
      uint rank = 0;
      while (this.currentToken == Token.LeftBracket) {
        Token nextTok = this.PeekNextToken();
        if (nextTok != Token.Comma && nextTok != Token.RightBracket) break; //not a rank specifier, but a size specifier
        rank = this.ParseRankSpecifier(ctx, followers|Token.LeftBrace|Token.LeftBracket|Token.LeftParenthesis|Token.RightParenthesis);
        et = t;
        t = new ArrayTypeExpression(et, rank, ctx);
      }
      if (rank > 0) {
        //new T[] {...} or new T[,] {{..} {...}...}, etc where T can also be an array type
        List<Expression> initializers;
        if (this.currentToken == Token.LeftBrace)
          initializers = this.ParseArrayInitializers(rank, et, followers, false, ctx);
        else {
          initializers = new List<Expression>(0);
          if (Parser.UnaryStart[this.currentToken])
            this.HandleError(Error.ExpectedLeftBrace);
          else
            this.HandleError(Error.MissingArraySize);
          while (Parser.UnaryStart[this.currentToken]) {
            this.ParseExpression(followers|Token.Comma|Token.RightBrace);
            if (this.currentToken != Token.Comma) break;
            this.GetNextToken();
          }
          ctx.UpdateToSpan(this.scanner.SourceLocationOfLastScannedToken);
          this.SkipOverTo(Token.RightBrace, followers);
        }
        return new CreateArray(et, initializers.AsReadOnly(), new List<Expression>(0).AsReadOnly(), rank, new List<Expression>(0).AsReadOnly(), ctx);
      }
      if (this.currentToken == Token.LeftBracket) {
        //new T[x] or new T[x,y] etc. possibly followed by an initializer or element type rank specifier
        this.GetNextToken();
        List<Expression> sizes = this.ParseExpressionList(ctx, followers|Token.LeftBrace|Token.LeftBracket);
        rank = (uint)sizes.Count;
        List<Expression> initializers;
        if (this.currentToken == Token.LeftBrace)
          initializers = this.ParseArrayInitializers(rank, t, followers, false, ctx);
        else {
          uint elementRank = 0;
        tryAgain:
          while (this.currentToken == Token.LeftBracket) {
            Token nextTok = this.PeekNextToken();
            if (nextTok != Token.Comma && nextTok != Token.RightBracket) break; //not a rank specifier, but a size specifier
            elementRank = this.ParseRankSpecifier(ctx, followers|Token.LeftBrace|Token.LeftBracket|Token.LeftParenthesis|Token.RightParenthesis);
            t = new ArrayTypeExpression(t, elementRank, ctx);
          }
          if (this.currentToken == Token.LeftBrace)
            initializers = this.ParseArrayInitializers(rank, t, followers, false, ctx);
          else {
            if (this.currentToken == Token.LeftBracket) { //new T[x][y] or something like that
              this.GetNextToken();
              this.HandleError(Error.InvalidArray);
              elementRank = (uint)this.ParseExpressionList(ctx, followers).Count;
              goto tryAgain;
            } else {
              initializers = new List<Expression>(0);
              ctx.UpdateToSpan(this.scanner.SourceLocationOfLastScannedToken);
              this.SkipTo(followers);
            }
          }
        }
        return new CreateArray(t, initializers.AsReadOnly(), new List<Expression>(0).AsReadOnly(), rank, sizes.AsReadOnly(), ctx);
      }
      //new T(...)
      IEnumerable<Expression> arguments = Expression.EmptyCollection;
      IEnumerable<Expression> elementValues = Expression.EmptyCollection;
      IEnumerable<NamedArgument> namedArguments = NamedArgument.EmptyCollection;
      if (this.currentToken == Token.LeftParenthesis) {
        //if (t is NonNullableTypeExpression) {
        //  this.SkipTo(followers, Error.BadNewExpr);
        //  return null;
        //}
        arguments = this.ParseArgumentList(ctx, followers|Token.LeftBrace).AsReadOnly();
      } else if (this.currentToken != Token.LeftBrace) {
        this.SkipTo(followers, Error.BadNewExpr);
      }
      Expression result = new CreateObjectInstance(t, arguments, ctx.GetSourceLocation());
      if (this.currentToken == Token.LeftBrace) {
        this.ParseElementValuesOrNamedArguments(ref elementValues, ref namedArguments, ctx, followers);
        if (elementValues != Expression.EmptyCollection)
          return new PopulateCollection(result, elementValues, ctx);
        else if (namedArguments != NamedArgument.EmptyCollection)
          return new InitializeObject(result, namedArguments, ctx);
        else {
          this.HandleError(Error.SyntaxError); //TODO: better error
        }
      }
      return result;
    }

    private void ParseElementValuesOrNamedArguments(ref IEnumerable<Expression> elementValues, ref IEnumerable<NamedArgument> namedArguments, SourceLocationBuilder slb, TokenSet followers) 
      //^ requires this.currentToken == Token.LeftBrace;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      this.GetNextToken();
      bool namedArgumentList = Parser.IdentifierOrNonReservedKeyword[this.currentToken] && this.PeekNextToken() == Token.Assign;
      if (namedArgumentList) 
        namedArguments = this.ParseNamedArgumentList(slb, followers|Token.RightBrace);
      else
        elementValues = this.ParseCollectionElementValueList(slb, followers|Token.RightBrace);
      this.SkipOverTo(Token.RightBrace, followers);
    }

    private IEnumerable<NamedArgument> ParseNamedArgumentList(SourceLocationBuilder slb, TokenSet followers)
      //^ requires Parser.IdentifierOrNonReservedKeyword[this.currentToken];
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      List<NamedArgument> namedArguments = new List<NamedArgument>();
      while (Parser.IdentifierOrNonReservedKeyword[this.currentToken]) {
        SimpleName argumentName = this.ParseSimpleName(followers|Token.Comma);
        this.Skip(Token.Assign);
        Expression argumentValue;
        if (this.currentToken == Token.LeftBrace) {
          IEnumerable<Expression> nestedElementValues = Expression.EmptyCollection;
          IEnumerable<NamedArgument> nestedNamedArguments = NamedArgument.EmptyCollection;
          //^ assume this.currentToken == Token.LeftBrace;
          this.ParseElementValuesOrNamedArguments(ref nestedElementValues, ref nestedNamedArguments, slb, followers);
          if (nestedElementValues != Expression.EmptyCollection)
            argumentValue = new PopulateCollection(null, nestedElementValues, slb);
          else if (nestedNamedArguments != NamedArgument.EmptyCollection)
            argumentValue = new InitializeObject(null, nestedNamedArguments, slb);
          else {
            this.HandleError(Error.SyntaxError); //TODO: better error
            continue;
          }
        } else
          argumentValue = this.ParseExpression(followers|Token.Comma);
        namedArguments.Add(new NamedArgument(argumentName, argumentValue, slb));
        if (this.currentToken != Token.Comma) break;
        this.GetNextToken();
      }
      namedArguments.TrimExcess();
      IEnumerable<NamedArgument> result = namedArguments.AsReadOnly();
      this.SkipTo(followers);
      return result;
    }

    private IEnumerable<Expression> ParseCollectionElementValueList(SourceLocationBuilder slb, TokenSet followers)
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      List<Expression> elementValues = new List<Expression>();
      while (this.currentToken != Token.RightBrace && this.currentToken != Token.EndOfFile) {
        Expression elementValue;
        if (this.currentToken == Token.LeftBrace) {
          IEnumerable<Expression> nestedElementValues = Expression.EmptyCollection;
          IEnumerable<NamedArgument> nestedNamedArguments = NamedArgument.EmptyCollection;
          //^ assume this.currentToken == Token.LeftBrace;
          this.ParseElementValuesOrNamedArguments(ref nestedElementValues, ref nestedNamedArguments, slb, followers);
          if (nestedElementValues != Expression.EmptyCollection)
            elementValue = new PopulateCollection(null, nestedElementValues, slb);
          else if (nestedNamedArguments != NamedArgument.EmptyCollection)
            elementValue = new InitializeObject(null, nestedNamedArguments, slb);
          else {
            this.HandleError(Error.SyntaxError); //TODO: better error
            continue;
          }
        } else
          elementValue = this.ParseExpression(followers|Token.Comma);
        elementValues.Add(elementValue);
        if (this.currentToken != Token.Comma) break;
        this.GetNextToken();
      }
      elementValues.TrimExcess();
      IEnumerable<Expression> result = elementValues.AsReadOnly();
      this.SkipTo(followers);
      return result;
    }

    private Expression ParseNewAnonymousTypeInstance(SourceLocationBuilder slb, TokenSet followers)
      //^ requires this.currentToken == Token.LeftBrace;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      this.GetNextToken();
      TokenSet followersOrCommaOrRightBrace = followers|Token.Comma|Token.RightBrace;
      List<Expression> initializers = new List<Expression>();
      while (Parser.UnaryStart[this.currentToken]) 
        // ^ invariant forall{IExpression initializer in initializers; initializer is NamedArgument || initializer is SimpleName || initializer is QualifiedName};
      {
        SourceLocationBuilder eslb = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
        Expression e = this.ParseUnaryExpression(followersOrCommaOrRightBrace|Parser.InfixOperators);
        SimpleName/*?*/ id = e as SimpleName;
        if (this.currentToken == Token.Assign) {
          this.GetNextToken();
          if (id == null) this.HandleError(e.SourceLocation, Error.ExpectedIdentifier);
          e = this.ParseExpression(followersOrCommaOrRightBrace);
          eslb.UpdateToSpan(e.SourceLocation);
          if (id != null) initializers.Add(new NamedArgument(id, e, eslb));
        } else {
          if (id != null)
            initializers.Add(id);
          else {
            QualifiedName/*?*/ qualId = e as QualifiedName;
            if (qualId != null)
              initializers.Add(qualId);
            else {
              this.HandleError(e.SourceLocation, Error.SyntaxError);
            }
          }
        }
        if (this.currentToken != Token.Comma) break;
        this.GetNextToken();
      }
      slb.UpdateToSpan(this.scanner.SourceLocationOfLastScannedToken);
      Expression result = new CreateAnonymousObject(initializers, slb);
      this.SkipOverTo(Token.RightBracket, followers);
      return result;
    }

    private Expression ParseNewImplicitlyTypedArray(SourceLocationBuilder slb, TokenSet followers)
      //^ requires this.currentToken == Token.LeftBracket;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      this.GetNextToken();
      this.Skip(Token.RightBracket);
      List<Expression> initializers;
      if (this.currentToken == Token.LeftBrace)
        initializers = this.ParseExpressionList(slb, followers);
      else {
        initializers = new List<Expression>(1);
        initializers.Add(this.ParseDummyExpression());
        this.Skip(Token.LeftBrace);
      }
      Expression result = new CreateImplicitlyTypedArray(initializers, slb);
      this.SkipTo(followers);
      return result;
    }

    private List<Expression> ParseExpressionList(SourceLocationBuilder slb, TokenSet followers)
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      TokenSet followersOrCommaOrRightBracket = followers|Token.Comma|Token.RightBracket;
      List<Expression> result = new List<Expression>();
      if (this.currentToken != Token.RightBracket) {
        Expression expression = this.ParseExpression(followersOrCommaOrRightBracket);
        result.Add(expression);
        while (this.currentToken == Token.Comma) {
          this.GetNextToken();
          expression = this.ParseExpression(followersOrCommaOrRightBracket);
          result.Add(expression);
        }
      }
      slb.UpdateToSpan(this.scanner.SourceLocationOfLastScannedToken);
      this.Skip(Token.RightBracket);
      this.SkipTo(followers);
      return result;
    }

    private Expression ParseArrayInitializer(ArrayTypeExpression arrayTypeExpression, TokenSet followers)
      //^ requires this.currentToken == Token.LeftBrace;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      uint rank = arrayTypeExpression.Rank;
      SourceLocationBuilder slb = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
      List<Expression> initializers = this.ParseArrayInitializers(rank, arrayTypeExpression.ElementType, followers, false, slb);
      //^ assert followers[this.currentToken] || this.currentToken == Token.EndOfFile;
      List<Expression> lowerBounds = new List<Expression>(0);
      List<Expression> sizes = new List<Expression>(0);
      Expression result = new CreateArray(arrayTypeExpression.ElementType, initializers, lowerBounds, rank, sizes, slb);
      //^ assume followers[this.currentToken] || this.currentToken == Token.EndOfFile;
      return result;
    }

    private List<Expression> ParseArrayInitializers(uint rank, TypeExpression elementType, TokenSet followers, bool doNotSkipClosingBrace, SourceLocationBuilder ctx)
      //^ requires this.currentToken == Token.LeftBrace;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      this.GetNextToken();
      List<Expression> initialValues = new List<Expression>();
      if (this.currentToken == Token.RightBrace) {
        ctx.UpdateToSpan(this.scanner.SourceLocationOfLastScannedToken);
        this.GetNextToken();
        initialValues.TrimExcess();
        return initialValues;
      }
      while (true) {
        if (rank > 1) {
          List<Expression> elemArrayInitializers;
          SourceLocationBuilder ectx = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
          if (this.currentToken == Token.LeftBrace) {
            elemArrayInitializers = this.ParseArrayInitializers(rank-1, elementType, followers|Token.Comma|Token.LeftBrace, false, ectx);
          } else {
            elemArrayInitializers = new List<Expression>(0);
            this.SkipTo(followers|Token.Comma|Token.LeftBrace, Error.ExpectedLeftBrace);
          }
          CreateArray elemArr = new CreateArray(elementType, elemArrayInitializers.AsReadOnly(), new List<Expression>(0).AsReadOnly(), rank-1, new List<Expression>(0).AsReadOnly(), ectx);
          initialValues.Add(elemArr);
        } else {
          if (this.currentToken == Token.LeftBrace) {
            this.HandleError(Error.ArrayInitInBadPlace);
            SourceLocationBuilder ectx = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
            //^ assume this.currentToken == Token.LeftBrace;
            List<Expression> elemArrayInitializers = this.ParseArrayInitializers(1, elementType, followers|Token.Comma|Token.LeftBrace, false, ectx);
            CreateArray elemArr = new CreateArray(elementType, elemArrayInitializers.AsReadOnly(), new List<Expression>(0).AsReadOnly(), 1, new List<Expression>(0).AsReadOnly(), ectx);
            initialValues.Add(elemArr);
          } else
            initialValues.Add(this.ParseExpression(followers|Token.Comma|Token.RightBrace));
        }
        if (this.currentToken != Token.Comma) break;
        this.GetNextToken();
        if (this.currentToken == Token.RightBrace) break;
      }
      ctx.UpdateToSpan(this.scanner.SourceLocationOfLastScannedToken);
      if (!doNotSkipClosingBrace) {
        this.Skip(Token.RightBrace);
        this.SkipTo(followers);
      }
      initialValues.TrimExcess();
      return initialValues;
    }

    private Expression ParseCastExpression(TokenSet followers)
      //^ requires this.currentToken == Token.LeftParenthesis;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      SourceLocationBuilder slb = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
      int position = this.scanner.CurrentDocumentPosition();
      this.GetNextToken();
      List<IErrorMessage> savedErrors = this.scannerAndParserErrors;
      this.scannerAndParserErrors = new List<IErrorMessage>(0);
      TypeExpression targetType = this.ParseTypeExpression(false, false, followers|Token.RightParenthesis);
      bool isCast = false;
      bool isLambda = false;
      if (this.currentToken == Token.RightParenthesis && this.scannerAndParserErrors.Count == 0) {
        if (targetType is NamedTypeExpression) {
          Token nextTok = this.PeekNextToken();
          isCast = Parser.CastFollower[nextTok];
          isLambda = nextTok == Token.Lambda;
        } else
          //Parsed a type expression that cannot also be a value expression.
          isCast = true;
      }
      this.scannerAndParserErrors = savedErrors;
      Expression expression;
      if (!isCast) {
        //Encountered an error while trying to parse (type expr) and there is some reason to be believe that this might not be a type argument list at all.
        //Back up the scanner and let the caller carry on as if it knew that < is the less than operator
        this.scanner.RestoreDocumentPosition(position);
        this.currentToken = Token.None;
        this.GetNextToken();
        if (isLambda)
          expression = this.ParseLambda(followers);
        else
          expression = this.ParseParenthesizedExpression(true, followers);
      } else {
        this.Skip(Token.RightParenthesis);
        Expression valueToCast = this.ParseUnaryExpression(followers);
        slb.UpdateToSpan(valueToCast.SourceLocation);
        expression = new Cast(valueToCast, targetType, slb);
      }
      for (; ; ) {
        switch (this.currentToken) {
          case Token.Arrow:
          case Token.Dot:
          case Token.LeftBracket:
            expression = this.ParseIndexerCallOrSelector(expression, followers);
            break;
          default:
            goto done;
        }
      }
    done:
      this.SkipTo(followers);
      return expression;
    }

    private Expression ParseQualifiedName(Expression qualifier, TokenSet followers)
      //^ requires this.currentToken == Token.Arrow || this.currentToken == Token.Dot || this.currentToken == Token.DoubleColon;
      //^ requires this.currentToken == Token.DoubleColon ==> qualifier is SimpleName || qualifier is RootNamespaceExpression;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      Token tok = this.currentToken;
      SourceLocationBuilder slb = new SourceLocationBuilder(qualifier.SourceLocation);
      this.GetNextToken();
      SimpleName name = this.ParseSimpleName(followers);
      slb.UpdateToSpan(name.SourceLocation);
      Expression result;
      if (tok == Token.Arrow) 
        result = new PointerQualifiedName(qualifier, name, slb);
      else if (tok == Token.DoubleColon) 
        result = new AliasQualifiedName(qualifier, name, slb);
      else {
        //^ assert tok == Token.Dot;
        result = new QualifiedName(qualifier, name, slb);
      }
      //^ assume followers[this.currentToken] || this.currentToken == Token.EndOfFile;
      return result;
    }

    private Expression ParseTypeofSizeofOrDefault(TokenSet followers)
      //^ requires this.currentToken == Token.Typeof || this.currentToken == Token.Sizeof || this.currentToken == Token.Default;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      Token tok = this.currentToken;
      SourceLocationBuilder slb = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
      this.GetNextToken();
      this.Skip(Token.LeftParenthesis);
      TypeExpression type = this.ParseTypeExpression(false, true, followers|Token.RightParenthesis);
      slb.UpdateToSpan(this.scanner.SourceLocationOfLastScannedToken);
      this.SkipOverTo(Token.RightParenthesis, followers);
      Expression result;
      if (tok == Token.Typeof) 
        result = new TypeOf(type, slb);
      else if (tok == Token.Sizeof)
        result = new SizeOf(type, slb);
      else {
        //^ assert tok == Token.Default;
        result = new DefaultValue(type, slb);
      }
      //^ assume followers[this.currentToken] || this.currentToken == Token.EndOfFile;
      return result;
    }

    private Expression ParseIndexerCallOrSelector(Expression expression, TokenSet followers)
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      while (true) {
        switch (this.currentToken) {
          case Token.Arrow:
          case Token.Dot:
            expression = this.ParseQualifiedName(expression, followers|Token.Arrow|Token.Dot|Token.LeftBracket|Token.LeftParenthesis|Token.LessThan);
            break;
          case Token.LeftBracket:
            expression = this.ParseIndexer(expression, followers|Token.Arrow|Token.Dot|Token.LeftBracket|Token.LeftParenthesis|Token.LessThan);
            break;
          case Token.LeftParenthesis:
            expression = this.ParseMethodCall(expression, followers|Token.Arrow|Token.Dot|Token.LeftBracket|Token.LeftParenthesis|Token.LessThan);
            break;
          case Token.LessThan:
            Expression gi = this.ParseGenericInstance(expression, followers|Token.Arrow|Token.Dot|Token.LeftBracket|Token.LeftParenthesis|Token.LessThan);
            if (gi == expression) goto default;
            expression = gi;
            break;
          default: 
            this.SkipTo(followers);
            return expression;
        }
      }
    }

    private Expression ParseGenericInstance(Expression expression, TokenSet followers)
      //^ requires this.currentToken == Token.LessThan;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      int position = this.scanner.CurrentDocumentPosition();
      SourceLocationBuilder slb = new SourceLocationBuilder(expression.SourceLocation);
      List<IErrorMessage> savedErrors = this.scannerAndParserErrors;
      this.scannerAndParserErrors = new List<IErrorMessage>(0);
      List<TypeExpression> argumentTypes = this.ParseTypeArguments(slb, false, followers);
      if (this.scannerAndParserErrors.Count > 0 && (argumentTypes.Count <= 1 || Parser.TypeArgumentListNonFollower[this.currentToken])) {
        //Encountered an error while trying to parse <...type args...> and there is some reason to be believe that this might not be a type argument list at all.
        //Back up the scanner and let the caller carry on as if it knew that < is the less than operator
        this.scannerAndParserErrors = savedErrors;
        this.scanner.RestoreDocumentPosition(position);
        this.currentToken = Token.None;
        this.GetNextToken();
        //^ assume this.currentToken == Token.LessThan;
        return expression;
      }
      savedErrors.AddRange(this.scannerAndParserErrors);
      this.scannerAndParserErrors = savedErrors;
      Expression result = new GenericInstanceExpression(expression, argumentTypes, slb);
      //^ assume followers[this.currentToken] || this.currentToken == Token.EndOfFile;
      return result;
    }

    private MethodCall ParseMethodCall(Expression method, TokenSet followers)
      //^ requires this.currentToken == Token.LeftParenthesis;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      SourceLocationBuilder slb = new SourceLocationBuilder(method.SourceLocation);
      return new MethodCall(method, this.ParseArgumentList(slb, followers).AsReadOnly(), slb);
    }

    private List<Expression> ParseArgumentList(SourceLocationBuilder slb, TokenSet followers)
      //^ requires this.currentToken == Token.LeftParenthesis;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      this.GetNextToken();
      TokenSet followersOrCommaOrRightParenthesis = followers|Token.Comma|Token.RightParenthesis;
      List<Expression> arguments = new List<Expression>();
      if (this.currentToken != Token.RightParenthesis) {
        Expression argument = this.ParseArgument(followersOrCommaOrRightParenthesis);
        arguments.Add(argument);
        while (this.currentToken == Token.Comma) {
          this.GetNextToken();
          argument = this.ParseArgument(followersOrCommaOrRightParenthesis);
          arguments.Add(argument);
        }
      }
      arguments.TrimExcess();
      slb.UpdateToSpan(this.scanner.SourceLocationOfLastScannedToken);
      this.SkipOverTo(Token.RightParenthesis, followers);
      return arguments;
    }

    private Expression ParseArgument(TokenSet followers)
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      switch (this.currentToken) {
        case Token.Ref:
          SourceLocationBuilder slb = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
          this.GetNextToken();
          Expression expr = this.ParseExpression(followers);
          slb.UpdateToSpan(expr.SourceLocation);
          Expression refArg = new RefArgument(new AddressableExpression(expr), slb);
          //^ assume followers[this.currentToken] || this.currentToken == Token.EndOfFile;
          return refArg;
        case Token.Out:
          slb = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
          this.GetNextToken();
          expr = this.ParseExpression(followers);
          slb.UpdateToSpan(expr.SourceLocation);
          Expression outArg = new OutArgument(new TargetExpression(expr), slb);
          //^ assume followers[this.currentToken] || this.currentToken == Token.EndOfFile;
          return outArg;
        //case Token.ArgList:
        //  slb = new SourceLocationBuilder(this.scanner.CurrentSourceContext);
        //  this.GetNextToken();
        //  if (this.currentToken == Token.LeftParenthesis) {
        //    ExpressionList el = this.ParseExpressionList(followers, ref sctx);
        //    return new ArglistArgumentExpression(el, sctx);
        //  }
        //  return new ArglistExpression(sctx);
        default:
          return this.ParseExpression(followers);
      }
    }

    private Indexer ParseIndexer(Expression indexedObject, TokenSet followers)
      //^ requires this.currentToken == Token.LeftBracket;
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      SourceLocationBuilder slb = new SourceLocationBuilder(indexedObject.SourceLocation);
      this.GetNextToken();
      List<Expression> indices = new List<Expression>();
      while (this.currentToken != Token.RightBracket) {
        Expression index = this.ParseExpression(followers|Token.Comma|Token.RightBracket);
        indices.Add(index);
        if (this.currentToken != Token.Comma) break;
        this.GetNextToken();
      }
      indices.TrimExcess();
      slb.UpdateToSpan(this.scanner.SourceLocationOfLastScannedToken);
      Indexer result = new Indexer(indexedObject, indices.AsReadOnly(), slb);
      this.SkipOverTo(Token.RightBracket, followers);
      return result;
    }

    private Expression ParseParenthesizedExpression(bool keepParentheses, TokenSet followers)
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      SourceLocationBuilder sctx = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
      if (this.currentToken == Token.LeftBrace) {
        Expression dummy = new DummyExpression(sctx);
        this.SkipTo(followers, Error.SyntaxError, "(");
        return dummy;
      }
      this.Skip(Token.LeftParenthesis);
      Expression result = this.ParseExpression(followers|Token.RightParenthesis|Token.Colon);
      if (keepParentheses) {
        sctx.UpdateToSpan(this.scanner.SourceLocationOfLastScannedToken);
        result = new Parenthesis(result, sctx);
      }
      this.SkipOverTo(Token.RightParenthesis, followers);
      return result;
    }

    private CompileTimeConstant ParseHexLiteral()
      //^ requires this.currentToken == Token.HexLiteral;
    {
      string tokStr = this.scanner.GetTokenSource();
      //^ assume tokStr.StartsWith("0x") || tokStr.StartsWith("0X"); //The scanner should not return a Token.HexLiteral when this is not the case.
      SourceLocationBuilder ctx = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
      TypeCode tc = this.scanner.ScanNumberSuffix();
      ctx.UpdateToSpan(this.scanner.SourceLocationOfLastScannedToken);
      CompileTimeConstant result;
      switch (tc) {
        case TypeCode.Single:
        case TypeCode.Double:
        case TypeCode.Decimal:
          this.HandleError(Error.ExpectedSemicolon);
          goto default;
        default:
          ulong ul;
          //^ assume tokStr.Length >= 2;
          if (!UInt64.TryParse(tokStr.Substring(2), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out ul)) {
            this.HandleError(ctx, Error.IntOverflow);
            ul = 0;
          }
          result = GetConstantOfSmallestIntegerTypeThatIncludes(ul, tc, ctx);
          break;
      }
      //^ assume this.currentToken == Token.HexLiteral; //follows from the precondition
      this.GetNextToken();
      return result;
    }

    private CompileTimeConstant ParseIntegerLiteral() 
      //^ requires this.currentToken == Token.IntegerLiteral;
    {
      string tokStr = this.scanner.GetTokenSource();
      SourceLocationBuilder ctx = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
      TypeCode tc = this.scanner.ScanNumberSuffix();
      ctx.UpdateToSpan(this.scanner.SourceLocationOfLastScannedToken);
      CompileTimeConstant result;
      switch (tc) {
        case TypeCode.Single:
          float f;
          if (!Single.TryParse(tokStr, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out f)) {
            this.HandleError(ctx, Error.FloatOverflow);
            f = float.NaN;
          }
          result = new CompileTimeConstant(f, false, ctx);
          break;
        case TypeCode.Double:
          double d;
          if (!Double.TryParse(tokStr, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out d)) {
            this.HandleError(ctx, Error.FloatOverflow);
            d = double.NaN;
          }
          result = new CompileTimeConstant(d, false, ctx);
          break;
        case TypeCode.Decimal:
          decimal m;
          if (!decimal.TryParse(tokStr, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out m)) {
            this.HandleError(ctx, Error.IntOverflow);
            m = decimal.Zero;
          }
          result = new CompileTimeConstant(m, false, ctx);
          break;
        default:
          ulong ul;
          if (!UInt64.TryParse(tokStr, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out ul)) {
            this.HandleError(ctx, Error.IntOverflow);
            ul = 0;
          }
          result = GetConstantOfSmallestIntegerTypeThatIncludes(ul, tc, ctx);
          break;
      }
      //^ assume this.currentToken == Token.IntegerLiteral; //follows from the precondition
      this.GetNextToken();
      return result;
    }

    private static CompileTimeConstant GetConstantOfSmallestIntegerTypeThatIncludes(ulong ul, TypeCode tc, SourceLocationBuilder ctx) {
      CompileTimeConstant result;
      if (ul <= int.MaxValue && tc == TypeCode.Empty)
        result = new CompileTimeConstant((int)ul, tc == TypeCode.Empty, ctx);
      else if (ul <= uint.MaxValue && (tc == TypeCode.Empty || tc == TypeCode.UInt32))
        result = new CompileTimeConstant((uint)ul, tc == TypeCode.Empty, ctx);
      else if (ul <= long.MaxValue && (tc == TypeCode.Empty || tc == TypeCode.Int64))
        result = new CompileTimeConstant((long)ul, tc == TypeCode.Empty, ctx);
      else
        result = new CompileTimeConstant(ul, tc == TypeCode.Empty, ctx);
      return result;
    }

    private static char[] nonZeroDigits = { '1', '2', '3', '4', '5', '6', '7', '8', '9' };

    private CompileTimeConstant ParseRealLiteral()
      //^ requires this.currentToken == Token.RealLiteral;
    {
      string tokStr = this.scanner.GetTokenSource();
      SourceLocationBuilder ctx = new SourceLocationBuilder(this.scanner.SourceLocationOfLastScannedToken);
      TypeCode tc = this.scanner.ScanNumberSuffix();
      ctx.UpdateToSpan(this.scanner.SourceLocationOfLastScannedToken);
      CompileTimeConstant result;
      string/*?*/ typeName = null;
      switch (tc) {
        case TypeCode.Single:
          typeName = "float";
          float fVal;
          if (!Single.TryParse(tokStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out fVal))
            this.HandleError(ctx, Error.FloatOverflow, typeName);
          else if (fVal == 0f && tokStr.IndexOfAny(nonZeroDigits) >= 0)
            this.HandleError(ctx, Error.FloatOverflow, typeName);
          result = new CompileTimeConstant(fVal, false, ctx);
          break;
        case TypeCode.Empty:
        case TypeCode.Double:
          typeName = "double";
          double dVal;
          if (!Double.TryParse(tokStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out dVal))
            this.HandleError(ctx, Error.FloatOverflow, typeName);
          else if (dVal == 0d && tokStr.IndexOfAny(nonZeroDigits) >= 0)
            this.HandleError(ctx, Error.FloatOverflow, typeName);
          result = new CompileTimeConstant(dVal, tc == TypeCode.Empty, ctx);
          break;
        case TypeCode.Decimal:
          typeName = "decimal";
          decimal decVal;
          if (!Decimal.TryParse(tokStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out decVal))
            this.HandleError(ctx, Error.FloatOverflow, typeName);
          result = new CompileTimeConstant(decVal, false, ctx);
          break;
        default:
          this.HandleError(Error.ExpectedSemicolon);
          goto case TypeCode.Empty;
      }
      //^ assume this.currentToken == Token.RealLiteral; //follows from the precondition
      this.GetNextToken();
      return result;
    }

    private bool LookForModifier(List<ModifierToken> modifiers, Token token, Error error) {
      bool result = false;
      for (int i = 0, n = modifiers.Count; i < n; i++) {
        if (modifiers[i].Token != token) continue;
        if (error != Error.None) {
          ISourceLocation sctx = modifiers[i].SourceLocation;
          //^ assume sctx != null; //This is really a precondition, but a tedious one to make formal.
          this.HandleError(sctx, error, sctx.Source);
          return false;
        }
        if (result) {
          ISourceLocation sctx = modifiers[i].SourceLocation;
          //^ assume sctx != null; //It follows from the way the modifier lists are constructed (i.e. not from uninitialized arrays).
          this.HandleError(sctx, Error.DuplicateModifier, sctx.Source);
        }
        result = true;
      }
      return result;
    }

    private TypeDeclaration.Flags ConvertToTypeDeclarationFlags(List<ModifierToken> modifiers) {
      TypeDeclaration.Flags result = TypeDeclaration.Flags.None;
      for (int i = 0, n = modifiers.Count; i < n; i++) {
        ISourceLocation sctx = modifiers[i].SourceLocation;
        //^ assume sctx != null; //It follows from the way the modifier lists are constructed (i.e. not from uninitialized arrays).
        switch (modifiers[i].Token) {
          case Token.Abstract:
            if ((result & TypeDeclaration.Flags.Static) != 0)
              this.HandleError(sctx, Error.AbstractSealedStatic);
            else if ((result & TypeDeclaration.Flags.Abstract) != 0)
              this.HandleError(sctx, Error.DuplicateModifier, "abstract");
            result |= TypeDeclaration.Flags.Abstract;
            break;
          case Token.Partial:
            result |= TypeDeclaration.Flags.Partial;
            return result;
          case Token.Private:
            if ((((TypeMemberVisibility)result) & TypeMemberVisibility.Mask) == TypeMemberVisibility.Private)
              this.HandleError(sctx, Error.DuplicateModifier, "private");
            else if ((((TypeMemberVisibility)result) & TypeMemberVisibility.Mask) != TypeMemberVisibility.Default)
              this.HandleError(sctx, Error.ConflictingProtectionModifier);
            result &= ~(TypeDeclaration.Flags)TypeMemberVisibility.Mask;
            result |= (TypeDeclaration.Flags)TypeMemberVisibility.Private;
            break;
          case Token.Protected:
            TypeMemberVisibility p = TypeMemberVisibility.Family;
            if ((((TypeMemberVisibility)result) & TypeMemberVisibility.Mask) == TypeMemberVisibility.Family)
              this.HandleError(sctx, Error.DuplicateModifier, "protected");
            else if ((((TypeMemberVisibility)result) & TypeMemberVisibility.Mask) != TypeMemberVisibility.Default) {
              if ((((TypeMemberVisibility)result) & TypeMemberVisibility.Mask) == TypeMemberVisibility.Assembly)
                p = TypeMemberVisibility.FamilyOrAssembly;
              else
                this.HandleError(sctx, Error.ConflictingProtectionModifier);
            }
            result &= ~(TypeDeclaration.Flags)TypeMemberVisibility.Mask;
            result |= (TypeDeclaration.Flags)p;
            break;
          case Token.Public:
            if ((((TypeMemberVisibility)result) & TypeMemberVisibility.Mask) == TypeMemberVisibility.Public)
              this.HandleError(sctx, Error.DuplicateModifier, "public");
            else if ((((TypeMemberVisibility)result) & TypeMemberVisibility.Mask) != TypeMemberVisibility.Default)
              this.HandleError(sctx, Error.ConflictingProtectionModifier);
            result &= ~(TypeDeclaration.Flags)TypeMemberVisibility.Mask;
            result |= (TypeDeclaration.Flags)TypeMemberVisibility.Public;
            break;
          case Token.Internal:
            TypeMemberVisibility a = TypeMemberVisibility.Assembly;
            if ((((TypeMemberVisibility)result) & TypeMemberVisibility.Mask) == TypeMemberVisibility.Assembly)
              this.HandleError(sctx, Error.DuplicateModifier, "internal");
            else if ((((TypeMemberVisibility)result) & TypeMemberVisibility.Mask) != TypeMemberVisibility.Default) {
              if ((((TypeMemberVisibility)result) & TypeMemberVisibility.Mask) == TypeMemberVisibility.Family)
                a = TypeMemberVisibility.FamilyOrAssembly;
              else
                this.HandleError(sctx, Error.ConflictingProtectionModifier);
            }
            result &= ~(TypeDeclaration.Flags)TypeMemberVisibility.Mask;
            result |= (TypeDeclaration.Flags)a;
            break;
          case Token.Sealed:
            if ((result & TypeDeclaration.Flags.Static) != 0)
              this.HandleError(sctx, Error.SealedStaticClass);
            else if ((result & TypeDeclaration.Flags.Sealed) != 0)
              this.HandleError(sctx, Error.DuplicateModifier, "sealed");
            result |= TypeDeclaration.Flags.Sealed;
            break;
          case Token.Static:
            if ((result & TypeDeclaration.Flags.Static) != 0)
              this.HandleError(sctx, Error.DuplicateModifier, "static");
            else if ((result & TypeDeclaration.Flags.Abstract) != 0)
              this.HandleError(sctx, Error.AbstractSealedStatic);
            else if ((result & TypeDeclaration.Flags.Sealed) != 0)
              this.HandleError(sctx, Error.SealedStaticClass);
            result |= TypeDeclaration.Flags.Static|TypeDeclaration.Flags.Sealed|TypeDeclaration.Flags.Abstract;
            break;
          case Token.Unsafe:
            if ((result & TypeDeclaration.Flags.Unsafe) != 0)
              this.HandleError(sctx, Error.DuplicateModifier, "unsafe");
            result |= TypeDeclaration.Flags.Unsafe;
            break;
          default:
            this.HandleError(sctx, Error.InvalidModifier, sctx.Source);
            break;
        }
      }
      return result;
    }

    private TypeMemberVisibility ConvertToTypeMemberVisibility(List<ModifierToken> modifiers) {
      TypeMemberVisibility result = (TypeMemberVisibility)0;
      for (int i = 0, n = modifiers.Count; i < n; i++) {
        ISourceLocation sctx = modifiers[i].SourceLocation;
        switch (modifiers[i].Token) {
          case Token.Private:
            if ((result & TypeMemberVisibility.Mask) == TypeMemberVisibility.Private)
              this.HandleError(sctx, Error.DuplicateModifier, "private");
            else if ((result & TypeMemberVisibility.Mask) != TypeMemberVisibility.Default)
              this.HandleError(sctx, Error.ConflictingProtectionModifier);
            result &= ~TypeMemberVisibility.Mask;
            result |= TypeMemberVisibility.Private;
            break;
          case Token.Protected:
            TypeMemberVisibility p = TypeMemberVisibility.Family;
            if ((result & TypeMemberVisibility.Mask) == TypeMemberVisibility.Family)
              this.HandleError(sctx, Error.DuplicateModifier, "protected");
            else if ((result & TypeMemberVisibility.Mask) != TypeMemberVisibility.Default) {
              if ((result & TypeMemberVisibility.Mask) == TypeMemberVisibility.Assembly)
                p = TypeMemberVisibility.FamilyOrAssembly;
              else
                this.HandleError(sctx, Error.ConflictingProtectionModifier);
            }
            result &= ~TypeMemberVisibility.Mask;
            result |= p;
            break;
          case Token.Public:
            if ((result & TypeMemberVisibility.Mask) == TypeMemberVisibility.Public)
              this.HandleError(sctx, Error.DuplicateModifier, "public");
            else if ((result & TypeMemberVisibility.Mask) != TypeMemberVisibility.Default)
              this.HandleError(sctx, Error.ConflictingProtectionModifier);
            result &= ~TypeMemberVisibility.Mask;
            result |= TypeMemberVisibility.Public;
            break;
          case Token.Internal:
            TypeMemberVisibility a = TypeMemberVisibility.Assembly;
            if ((result & TypeMemberVisibility.Mask) == TypeMemberVisibility.Assembly)
              this.HandleError(sctx, Error.DuplicateModifier, "internal");
            else if ((result & TypeMemberVisibility.Mask) != TypeMemberVisibility.Default) {
              if ((result & TypeMemberVisibility.Mask) == TypeMemberVisibility.Family)
                a = TypeMemberVisibility.FamilyOrAssembly;
              else
                this.HandleError(sctx, Error.ConflictingProtectionModifier);
            }
            result &= ~TypeMemberVisibility.Mask;
            result |= a;
            break;
          default:
            break;
        }
      }
      return result;
    }

    struct ModifierToken { 
      internal Token Token; 
      internal ISourceLocation SourceLocation;

      internal ModifierToken(Token token, ISourceLocation sourceLocation) {
        this.Token = token;
        this.SourceLocation = sourceLocation;
      }
    }

    private List<ModifierToken> ParseModifiers() {
      List<ModifierToken> result = new List<ModifierToken>();
      ModifierToken tok = new ModifierToken(this.currentToken, this.scanner.SourceLocationOfLastScannedToken);
      for (; ; ) {
        switch (this.currentToken) {
          case Token.Abstract:
          case Token.Extern:
          case Token.Internal:
          case Token.New:
          case Token.Override:
          case Token.Protected:
          case Token.Private:
          case Token.Public:
          case Token.Readonly:
          case Token.Sealed:
          case Token.Static:
          case Token.Volatile:
          case Token.Virtual:
          case Token.Unsafe:
            result.Add(tok);
            break;
          case Token.Partial:
            Token nextToken = this.PeekNextToken();
            if (nextToken == Token.Class || nextToken == Token.Struct || nextToken == Token.Interface)
              result.Add(tok);
            else if (!Parser.IdentifierOrNonReservedKeyword[nextToken])
              this.HandleError(Error.PartialMisplaced);
            this.GetNextToken();
            return result;
          default:
            return result;
        }
        this.GetNextToken();
        tok = new ModifierToken(this.currentToken, this.scanner.SourceLocationOfLastScannedToken);
      }
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
        switch (token) {
          case Token.Alias: this.HandleError(Error.SyntaxError, "alias"); break;
          case Token.Colon: this.HandleError(Error.SyntaxError, ":"); break;
          case Token.Identifier: this.HandleError(Error.ExpectedIdentifier); break;
          case Token.In: this.HandleError(Error.InExpected); break;
          case Token.LeftBrace: this.HandleError(Error.ExpectedLeftBrace); break;
          case Token.LeftParenthesis: this.HandleError(Error.SyntaxError, "("); break;
          case Token.RightBrace: this.HandleError(Error.ExpectedRightBrace); break;
          case Token.RightBracket: this.HandleError(Error.ExpectedRightBracket); break;
          case Token.RightParenthesis: this.HandleError(Error.ExpectedRightParenthesis); break;
          case Token.Semicolon: this.HandleError(Error.ExpectedSemicolon); break;
          default: this.HandleError(Error.UnexpectedToken, this.scanner.GetTokenSource()); break;
        }
      }
    }

    private void SkipSemiColon(TokenSet followers)
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      if (this.currentToken == Token.Semicolon) {
        this.GetNextToken();
        this.SkipTo(followers);
      } else {
        this.Skip(Token.Semicolon);
        while (!this.scanner.TokenIsFirstAfterLineBreak && this.currentToken != Token.Semicolon && this.currentToken != Token.RightBrace && this.currentToken != Token.EndOfFile
          && (this.currentToken != Token.LeftBrace || !followers[Token.LeftBrace]))
          this.GetNextToken();
        if (this.currentToken == Token.Semicolon) 
          this.GetNextToken();
        this.SkipTo(followers);
      }
    }

    private void SkipTo(TokenSet followers)
      //^ ensures followers[this.currentToken] || this.currentToken == Token.EndOfFile;
    {
      if (followers[this.currentToken]) return;
      Error error = Error.InvalidExprTerm;
      if (this.currentToken == Token.Using)
        error = Error.UsingAfterElements;
      else if (!this.insideBlock)
        error = Error.InvalidMemberDecl;
      this.HandleError(error, this.scanner.GetTokenSource());
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
      //^ requires tok == Token.Bool || tok == Token.Decimal || tok == Token.Sbyte ||
      //^   tok == Token.Byte || tok == Token.Short || tok == Token.Ushort ||
      //^   tok == Token.Int || tok == Token.Uint || tok == Token.Long ||
      //^   tok == Token.Ulong || tok == Token.Char || tok == Token.Float ||
      //^   tok == Token.Double || tok == Token.Object || tok == Token.String ||
      //^   tok == Token.Void;
    {
      return this.RootQualifiedNameFor(tok, this.scanner.SourceLocationOfLastScannedToken);
    }

    private QualifiedName RootQualifiedNameFor(Token tok, ISourceLocation sctx) 
      //^ requires tok == Token.Bool || tok == Token.Decimal || tok == Token.Sbyte ||
      //^   tok == Token.Byte || tok == Token.Short || tok == Token.Ushort ||
      //^   tok == Token.Int || tok == Token.Uint || tok == Token.Long ||
      //^   tok == Token.Ulong || tok == Token.Char || tok == Token.Float ||
      //^   tok == Token.Double || tok == Token.Object || tok == Token.String ||
      //^   tok == Token.Void;
    {
      RootNamespaceExpression rootNs = new RootNamespaceExpression(sctx);
      AliasQualifiedName systemNs = new AliasQualifiedName(rootNs, this.GetSimpleNameFor("System"), sctx);
      switch (tok) {
        case Token.Bool: return new QualifiedName(systemNs, this.GetSimpleNameFor("Boolean"), sctx);
        case Token.Decimal: return new QualifiedName(systemNs, this.GetSimpleNameFor("Decimal"), sctx);
        case Token.Sbyte: return new QualifiedName(systemNs, this.GetSimpleNameFor("SByte"), sctx);
        case Token.Byte: return new QualifiedName(systemNs, this.GetSimpleNameFor("Byte"), sctx);
        case Token.Short: return new QualifiedName(systemNs, this.GetSimpleNameFor("Int16"), sctx);
        case Token.Ushort: return new QualifiedName(systemNs, this.GetSimpleNameFor("UInt16"), sctx);
        case Token.Int: return new QualifiedName(systemNs, this.GetSimpleNameFor("Int32"), sctx);
        case Token.Uint: return new QualifiedName(systemNs, this.GetSimpleNameFor("UInt32"), sctx);
        case Token.Long: return new QualifiedName(systemNs, this.GetSimpleNameFor("Int64"), sctx);
        case Token.Ulong: return new QualifiedName(systemNs, this.GetSimpleNameFor("UInt64"), sctx);
        case Token.Char: return new QualifiedName(systemNs, this.GetSimpleNameFor("Char"), sctx);
        case Token.Float: return new QualifiedName(systemNs, this.GetSimpleNameFor("Single"), sctx);
        case Token.Double: return new QualifiedName(systemNs, this.GetSimpleNameFor("Double"), sctx);
        case Token.Object: return new QualifiedName(systemNs, this.GetSimpleNameFor("Object"), sctx);
        case Token.String: return new QualifiedName(systemNs, this.GetSimpleNameFor("String"), sctx);
        default:
          //^ assert tok == Token.Void;
          return new QualifiedName(systemNs, this.GetSimpleNameFor("Void"), sctx);
      }
    }

    private SimpleName GetSimpleNameFor(string nameString) {
      IName name = this.GetNameFor(nameString);
      return new SimpleName(name, this.scanner.SourceLocationOfLastScannedToken, false);
    }

    private static readonly TokenSet AddOneOrSubtractOne;
    private static readonly TokenSet AddOrRemoveOrModifier;
    private static readonly TokenSet AssignmentOperators;
    private static readonly TokenSet AttributeOrNamespaceOrTypeDeclarationStart;
    private static readonly TokenSet AttributeOrTypeDeclarationStart;
    private static readonly TokenSet CaseOrDefaultOrRightBrace;
    private static readonly TokenSet CaseOrColonOrDefaultOrRightBrace;
    private static readonly TokenSet CastFollower;
    private static readonly TokenSet CatchOrFinally;
    private static readonly TokenSet EndOfFile;
    private static readonly TokenSet GetOrLeftBracketOrSetOrModifier;
    private static readonly TokenSet IdentifierOrNonReservedKeyword;
    private static readonly TokenSet InfixOperators;
    private static readonly TokenSet ParameterTypeStart;
    private static readonly TokenSet PrimaryStart;
    private static readonly TokenSet ProtectionModifier;
    private static readonly TokenSet NamespaceOrTypeDeclarationStart;
    private static readonly TokenSet RightParenthesisOrSemicolon;
    private static readonly TokenSet StatementStart;
    private static readonly TokenSet TypeArgumentListNonFollower;
    private static readonly TokenSet TypeMemberStart;
    private static readonly TokenSet ContractStart;
    private static readonly TokenSet TypeStart;
    private static readonly TokenSet TypeOperator;
    private static readonly TokenSet UnaryStart;
    private static readonly TokenSet Term; //  Token belongs to first set for term-or-unary-operator (follows casts), but is not a predefined type.
    private static readonly TokenSet Predefined; // Token is a predefined type
    private static readonly TokenSet UnaryOperator; //  Token belongs to unary operator
    private static readonly TokenSet NullableTypeNonFollower;
    
    static Parser(){
      AddOneOrSubtractOne = new TokenSet();
      AddOneOrSubtractOne |= Token.AddOne;
      AddOneOrSubtractOne |= Token.SubtractOne;

      AddOrRemoveOrModifier = new TokenSet();
      AddOrRemoveOrModifier |= Token.Add;
      AddOrRemoveOrModifier |= Token.Remove;
      AddOrRemoveOrModifier |= Token.New;
      AddOrRemoveOrModifier |= Token.Public;
      AddOrRemoveOrModifier |= Token.Protected;
      AddOrRemoveOrModifier |= Token.Internal;
      AddOrRemoveOrModifier |= Token.Private;
      AddOrRemoveOrModifier |= Token.Abstract;
      AddOrRemoveOrModifier |= Token.Sealed;
      AddOrRemoveOrModifier |= Token.Static;
      AddOrRemoveOrModifier |= Token.Readonly;
      AddOrRemoveOrModifier |= Token.Volatile;
      AddOrRemoveOrModifier |= Token.Virtual;
      AddOrRemoveOrModifier |= Token.Override;
      AddOrRemoveOrModifier |= Token.Extern;
      AddOrRemoveOrModifier |= Token.Unsafe;

      AssignmentOperators = new TokenSet();      
      AssignmentOperators |= Token.PlusAssign;
      AssignmentOperators |= Token.Assign;
      AssignmentOperators |= Token.BitwiseAndAssign;
      AssignmentOperators |= Token.BitwiseOrAssign;
      AssignmentOperators |= Token.BitwiseXorAssign;
      AssignmentOperators |= Token.DivideAssign;
      AssignmentOperators |= Token.LeftShiftAssign;
      AssignmentOperators |= Token.MultiplyAssign;
      AssignmentOperators |= Token.RemainderAssign;
      AssignmentOperators |= Token.RightShiftAssign;
      AssignmentOperators |= Token.SubtractAssign;

      AttributeOrTypeDeclarationStart = new TokenSet();
      AttributeOrTypeDeclarationStart |= Token.LeftBracket;
      AttributeOrTypeDeclarationStart |= Token.New;
      AttributeOrTypeDeclarationStart |= Token.Partial;
      AttributeOrTypeDeclarationStart |= Token.Unsafe;
      AttributeOrTypeDeclarationStart |= Token.Public;
      AttributeOrTypeDeclarationStart |= Token.Internal;
      AttributeOrTypeDeclarationStart |= Token.Abstract;
      AttributeOrTypeDeclarationStart |= Token.Sealed;
      AttributeOrTypeDeclarationStart |= Token.Static;
      AttributeOrTypeDeclarationStart |= Token.Class;
      AttributeOrTypeDeclarationStart |= Token.Delegate;
      AttributeOrTypeDeclarationStart |= Token.Enum;
      AttributeOrTypeDeclarationStart |= Token.Interface;
      AttributeOrTypeDeclarationStart |= Token.Struct;

      CaseOrDefaultOrRightBrace = new TokenSet();
      CaseOrDefaultOrRightBrace |= Token.Case;
      CaseOrDefaultOrRightBrace |= Token.Default;
      CaseOrDefaultOrRightBrace |= Token.RightBrace;

      CaseOrColonOrDefaultOrRightBrace = CaseOrDefaultOrRightBrace;
      CaseOrColonOrDefaultOrRightBrace |= Token.Colon;

      CatchOrFinally = new TokenSet();
      CatchOrFinally |= Token.Catch;
      CatchOrFinally |= Token.Finally;

      ContractStart = new TokenSet();
      ContractStart |= Token.Requires;
      ContractStart |= Token.Modifies;
      ContractStart |= Token.Ensures;
      ContractStart |= Token.Throws;

      EndOfFile = new TokenSet();
      EndOfFile |= Token.EndOfFile;

      GetOrLeftBracketOrSetOrModifier = new TokenSet();
      GetOrLeftBracketOrSetOrModifier |= Token.Get;
      GetOrLeftBracketOrSetOrModifier |= Token.LeftBracket;
      GetOrLeftBracketOrSetOrModifier |= Token.Set;
      GetOrLeftBracketOrSetOrModifier |= Token.New;
      GetOrLeftBracketOrSetOrModifier |= Token.Public;
      GetOrLeftBracketOrSetOrModifier |= Token.Protected;
      GetOrLeftBracketOrSetOrModifier |= Token.Internal;
      GetOrLeftBracketOrSetOrModifier |= Token.Private;
      GetOrLeftBracketOrSetOrModifier |= Token.Abstract;
      GetOrLeftBracketOrSetOrModifier |= Token.Sealed;
      GetOrLeftBracketOrSetOrModifier |= Token.Static;
      GetOrLeftBracketOrSetOrModifier |= Token.Readonly;
      GetOrLeftBracketOrSetOrModifier |= Token.Volatile;
      GetOrLeftBracketOrSetOrModifier |= Token.Virtual;
      GetOrLeftBracketOrSetOrModifier |= Token.Override;
      GetOrLeftBracketOrSetOrModifier |= Token.Extern;
      GetOrLeftBracketOrSetOrModifier |= Token.Unsafe;
      
      IdentifierOrNonReservedKeyword = new TokenSet();
      IdentifierOrNonReservedKeyword |= Token.Identifier;
      IdentifierOrNonReservedKeyword |= Token.Acquire;
      IdentifierOrNonReservedKeyword |= Token.Add;
      IdentifierOrNonReservedKeyword |= Token.Alias;
      IdentifierOrNonReservedKeyword |= Token.Assert;
      IdentifierOrNonReservedKeyword |= Token.Assume;
      IdentifierOrNonReservedKeyword |= Token.Count;
      IdentifierOrNonReservedKeyword |= Token.Ensures;
      IdentifierOrNonReservedKeyword |= Token.Exists;
      IdentifierOrNonReservedKeyword |= Token.Expose;
      IdentifierOrNonReservedKeyword |= Token.Forall;
      IdentifierOrNonReservedKeyword |= Token.Get;
      IdentifierOrNonReservedKeyword |= Token.Modifies;
      IdentifierOrNonReservedKeyword |= Token.Old;
      IdentifierOrNonReservedKeyword |= Token.Otherwise;
      IdentifierOrNonReservedKeyword |= Token.Partial;
      IdentifierOrNonReservedKeyword |= Token.Read;
      IdentifierOrNonReservedKeyword |= Token.Remove;
      IdentifierOrNonReservedKeyword |= Token.Requires;
      IdentifierOrNonReservedKeyword |= Token.Set;
      IdentifierOrNonReservedKeyword |= Token.Throws;
      IdentifierOrNonReservedKeyword |= Token.Unique;
      IdentifierOrNonReservedKeyword |= Token.Value;
      IdentifierOrNonReservedKeyword |= Token.Var;
      IdentifierOrNonReservedKeyword |= Token.Write;
      IdentifierOrNonReservedKeyword |= Token.Yield;
      IdentifierOrNonReservedKeyword |= Token.Where;      

      InfixOperators = new TokenSet();      
      InfixOperators |= Token.PlusAssign;
      InfixOperators |= Token.As;
      InfixOperators |= Token.Assign;
      InfixOperators |= Token.BitwiseAnd;
      InfixOperators |= Token.BitwiseAndAssign;
      InfixOperators |= Token.BitwiseOr;
      InfixOperators |= Token.BitwiseOrAssign;
      InfixOperators |= Token.BitwiseXor;
      InfixOperators |= Token.BitwiseXorAssign;
      InfixOperators |= Token.Conditional;
      InfixOperators |= Token.Divide;
      InfixOperators |= Token.DivideAssign;
      InfixOperators |= Token.Equal;
      InfixOperators |= Token.GreaterThan;
      InfixOperators |= Token.GreaterThanOrEqual;
      InfixOperators |= Token.Is;
      //InfixOperators |= Token.Iff;
      //InfixOperators |= Token.Implies;
      InfixOperators |= Token.LeftShift;
      InfixOperators |= Token.LeftShiftAssign;
      InfixOperators |= Token.LessThan;
      InfixOperators |= Token.LessThanOrEqual;
      InfixOperators |= Token.LogicalAnd;
      InfixOperators |= Token.LogicalOr;
      //InfixOperators |= Token.Maplet; 
      InfixOperators |= Token.Multiply;
      InfixOperators |= Token.MultiplyAssign;
      InfixOperators |= Token.NotEqual;
      InfixOperators |= Token.NullCoalescing;
      InfixOperators |= Token.Plus;
      //InfixOperators |= Token.Range; 
      InfixOperators |= Token.Remainder;
      InfixOperators |= Token.RemainderAssign;
      InfixOperators |= Token.RightShift;
      InfixOperators |= Token.RightShiftAssign;
      InfixOperators |= Token.Subtract;
      InfixOperators |= Token.SubtractAssign;
      InfixOperators |= Token.Arrow;

      TypeStart = new TokenSet();
      TypeStart |= Parser.IdentifierOrNonReservedKeyword;
      TypeStart |= Token.Bool;
      TypeStart |= Token.Decimal;
      TypeStart |= Token.Sbyte;
      TypeStart |= Token.Byte;
      TypeStart |= Token.Short;
      TypeStart |= Token.Ushort;
      TypeStart |= Token.Int;
      TypeStart |= Token.Uint;
      TypeStart |= Token.Long;
      TypeStart |= Token.Ulong;
      TypeStart |= Token.Char;
      TypeStart |= Token.Float;
      TypeStart |= Token.Double;
      TypeStart |= Token.Object;
      TypeStart |= Token.String;
      TypeStart |= Token.LeftBracket;
      TypeStart |= Token.LeftParenthesis;

      ParameterTypeStart = new TokenSet();
      ParameterTypeStart |= Parser.TypeStart;
      ParameterTypeStart |= Token.Ref;
      ParameterTypeStart |= Token.Out;
      ParameterTypeStart |= Token.Params;

      PrimaryStart = new TokenSet();
      PrimaryStart |= Parser.IdentifierOrNonReservedKeyword;
      PrimaryStart |= Token.This;
      PrimaryStart |= Token.Base;
      PrimaryStart |= Token.Value;
      PrimaryStart |= Token.New;
      PrimaryStart |= Token.Typeof;
      PrimaryStart |= Token.Sizeof;
      PrimaryStart |= Token.Stackalloc;
      PrimaryStart |= Token.Checked;
      PrimaryStart |= Token.Unchecked;
      PrimaryStart |= Token.HexLiteral;
      PrimaryStart |= Token.IntegerLiteral;
      PrimaryStart |= Token.StringLiteral;
      PrimaryStart |= Token.CharLiteral;
      PrimaryStart |= Token.RealLiteral;
      PrimaryStart |= Token.Null;
      PrimaryStart |= Token.False;
      PrimaryStart |= Token.True;
      PrimaryStart |= Token.Bool;
      PrimaryStart |= Token.Decimal;
      PrimaryStart |= Token.Sbyte;
      PrimaryStart |= Token.Byte;
      PrimaryStart |= Token.Short;
      PrimaryStart |= Token.Ushort;
      PrimaryStart |= Token.Int;
      PrimaryStart |= Token.Uint;
      PrimaryStart |= Token.Long;
      PrimaryStart |= Token.Ulong;
      PrimaryStart |= Token.Char;
      PrimaryStart |= Token.Float;
      PrimaryStart |= Token.Double;
      PrimaryStart |= Token.Object;
      PrimaryStart |= Token.String;
      PrimaryStart |= Token.LeftParenthesis;

      ProtectionModifier = new TokenSet();
      ProtectionModifier |= Token.Public;
      ProtectionModifier |= Token.Protected;
      ProtectionModifier |= Token.Internal;
      ProtectionModifier |= Token.Private;

      NamespaceOrTypeDeclarationStart = new TokenSet();
      NamespaceOrTypeDeclarationStart |= Token.Partial;
      NamespaceOrTypeDeclarationStart |= Token.Unsafe;
      NamespaceOrTypeDeclarationStart |= Token.Public;
      NamespaceOrTypeDeclarationStart |= Token.Internal;
      NamespaceOrTypeDeclarationStart |= Token.Abstract;
      NamespaceOrTypeDeclarationStart |= Token.Sealed;
      NamespaceOrTypeDeclarationStart |= Token.Static;
      NamespaceOrTypeDeclarationStart |= Token.Namespace;
      NamespaceOrTypeDeclarationStart |= Token.Class;
      NamespaceOrTypeDeclarationStart |= Token.Delegate;
      NamespaceOrTypeDeclarationStart |= Token.Enum;
      NamespaceOrTypeDeclarationStart |= Token.Interface;
      NamespaceOrTypeDeclarationStart |= Token.Struct;

      AttributeOrNamespaceOrTypeDeclarationStart = AttributeOrTypeDeclarationStart;
      AttributeOrNamespaceOrTypeDeclarationStart |= Token.Namespace;
      AttributeOrNamespaceOrTypeDeclarationStart |= Token.Private; //For error recovery
      AttributeOrNamespaceOrTypeDeclarationStart |= Token.Protected; //For error recovery

      RightParenthesisOrSemicolon = new TokenSet();
      RightParenthesisOrSemicolon |= Token.RightParenthesis;
      RightParenthesisOrSemicolon |= Token.Semicolon;

      TypeMemberStart = new TokenSet();
      TypeMemberStart |= Token.LeftBracket;
      TypeMemberStart |= Token.LeftParenthesis;
      TypeMemberStart |= Token.LeftBrace;
      TypeMemberStart |= Token.New;
      TypeMemberStart |= Token.Partial;
      TypeMemberStart |= Token.Public;
      TypeMemberStart |= Token.Protected;
      TypeMemberStart |= Token.Internal;
      TypeMemberStart |= Token.Private;
      TypeMemberStart |= Token.Abstract;
      TypeMemberStart |= Token.Sealed;
      TypeMemberStart |= Token.Static;
      TypeMemberStart |= Token.Readonly;
      TypeMemberStart |= Token.Volatile;
      TypeMemberStart |= Token.Virtual;
      TypeMemberStart |= Token.Override;
      TypeMemberStart |= Token.Extern;
      TypeMemberStart |= Token.Unsafe;
      TypeMemberStart |= Token.Const;
      TypeMemberStart |= Parser.IdentifierOrNonReservedKeyword;
      TypeMemberStart |= Token.Event;
      TypeMemberStart |= Token.This;
      TypeMemberStart |= Token.Operator;
      TypeMemberStart |= Token.BitwiseNot;
      TypeMemberStart |= Token.Static;
      TypeMemberStart |= Token.Class;
      TypeMemberStart |= Token.Delegate;
      TypeMemberStart |= Token.Enum;
      TypeMemberStart |= Token.Interface;
      TypeMemberStart |= Token.Struct;
      TypeMemberStart |= Token.Bool;
      TypeMemberStart |= Token.Decimal;
      TypeMemberStart |= Token.Sbyte;
      TypeMemberStart |= Token.Byte;
      TypeMemberStart |= Token.Short;
      TypeMemberStart |= Token.Ushort;
      TypeMemberStart |= Token.Int;
      TypeMemberStart |= Token.Uint;
      TypeMemberStart |= Token.Long;
      TypeMemberStart |= Token.Ulong;
      TypeMemberStart |= Token.Char;
      TypeMemberStart |= Token.Float;
      TypeMemberStart |= Token.Double;
      TypeMemberStart |= Token.Object;
      TypeMemberStart |= Token.String;
      TypeMemberStart |= Token.Void;
      TypeMemberStart |= Token.Invariant;

      TypeOperator = new TokenSet();
      TypeOperator |= Token.LeftBracket;
      TypeOperator |= Token.Multiply;
      TypeOperator |= Token.Plus;
      TypeOperator |= Token.Conditional;
      TypeOperator |= Token.LogicalNot;
      TypeOperator |= Token.BitwiseAnd;

      UnaryStart = new TokenSet();
      UnaryStart |= Parser.IdentifierOrNonReservedKeyword;
      UnaryStart |= Token.LeftParenthesis;
      UnaryStart |= Token.LeftBracket;
      UnaryStart |= Token.This;
      UnaryStart |= Token.Base;
      UnaryStart |= Token.Value;
      UnaryStart |= Token.AddOne;
      UnaryStart |= Token.SubtractOne;
      UnaryStart |= Token.New;
      UnaryStart |= Token.Default;
      UnaryStart |= Token.Typeof;
      UnaryStart |= Token.Sizeof;
      UnaryStart |= Token.Stackalloc;
      UnaryStart |= Token.Delegate;
      UnaryStart |= Token.Checked;
      UnaryStart |= Token.Unchecked;
      UnaryStart |= Token.HexLiteral;
      UnaryStart |= Token.IntegerLiteral;
      UnaryStart |= Token.StringLiteral;
      UnaryStart |= Token.CharLiteral;
      UnaryStart |= Token.RealLiteral;
      UnaryStart |= Token.Null;
      UnaryStart |= Token.False;
      UnaryStart |= Token.True;
      UnaryStart |= Token.Bool;
      UnaryStart |= Token.Decimal;
      UnaryStart |= Token.Sbyte;
      UnaryStart |= Token.Byte;
      UnaryStart |= Token.Short;
      UnaryStart |= Token.Ushort;
      UnaryStart |= Token.Int;
      UnaryStart |= Token.Uint;
      UnaryStart |= Token.Long;
      UnaryStart |= Token.Ulong;
      UnaryStart |= Token.Char;
      UnaryStart |= Token.Float;
      UnaryStart |= Token.Double;
      UnaryStart |= Token.Object;
      UnaryStart |= Token.String;
      UnaryStart |= Token.Plus;
      UnaryStart |= Token.BitwiseNot;
      UnaryStart |= Token.LogicalNot;
      UnaryStart |= Token.Multiply;
      UnaryStart |= Token.Subtract;
      UnaryStart |= Token.AddOne;
      UnaryStart |= Token.SubtractOne;
      UnaryStart |= Token.Multiply;
      UnaryStart |= Token.BitwiseAnd;

      StatementStart = new TokenSet();
      StatementStart |= Parser.UnaryStart;
      StatementStart |= Token.LeftBrace;
      StatementStart |= Token.Semicolon;
      StatementStart |= Token.Acquire;
      StatementStart |= Token.Assert;
      StatementStart |= Token.Assume;
      StatementStart |= Token.If;
      StatementStart |= Token.Switch;
      StatementStart |= Token.While;
      StatementStart |= Token.Do;
      StatementStart |= Token.For;
      StatementStart |= Token.Foreach;
      StatementStart |= Token.While;
      StatementStart |= Token.Break;
      StatementStart |= Token.Continue;
      StatementStart |= Token.Goto;
      StatementStart |= Token.Return;
      StatementStart |= Token.Throw;
      StatementStart |= Token.Yield;
      StatementStart |= Token.Try;
      StatementStart |= Token.Catch; //Not really, but helps error recovery
      StatementStart |= Token.Finally; //Not really, but helps error recovery
      StatementStart |= Token.Checked;
      StatementStart |= Token.Unchecked;
      StatementStart |= Token.Read;
      StatementStart |= Token.Write;
      StatementStart |= Token.Expose;
      StatementStart |= Token.Fixed;
      StatementStart |= Token.Lock;
      StatementStart |= Token.Unsafe;
      StatementStart |= Token.Using;
      StatementStart |= Token.Const;
      StatementStart |= Token.Delegate;
      StatementStart |= Token.Void;

      Term = new TokenSet();
      Term |= Token.ArgList;
      Term |= Token.MakeRef;
      Term |= Token.RefType;
      Term |= Token.RefValue;
      Term |= Token.Base;
      Term |= Token.Checked;
      Term |= Token.Default;
      Term |= Token.Delegate;
      Term |= Token.False;
      Term |= Token.New;
      Term |= Token.Null;
      Term |= Token.Sizeof;
      Term |= Token.This;
      Term |= Token.True;
      Term |= Token.Typeof;
      Term |= Token.Unchecked;
      Term |= Token.Identifier;
      Term |= Token.IntegerLiteral;
      Term |= Token.RealLiteral;
      Term |= Token.StringLiteral;
      Term |= Token.CharLiteral;
      Term |= Token.LeftParenthesis;

      Predefined = new TokenSet();
      Predefined |= Token.Bool;
      Predefined |= Token.Decimal;
      Predefined |= Token.Sbyte;
      Predefined |= Token.Byte;
      Predefined |= Token.Short;
      Predefined |= Token.Ushort;
      Predefined |= Token.Int;
      Predefined |= Token.Uint;
      Predefined |= Token.Long;
      Predefined |= Token.Ulong;
      Predefined |= Token.Char;
      Predefined |= Token.Float;
      Predefined |= Token.Double;
      Predefined |= Token.Object;
      Predefined |= Token.String;
      Predefined |= Token.Void;

      UnaryOperator = new TokenSet();
      UnaryOperator |= Token.Base;
      UnaryOperator |= Token.Default;
      UnaryOperator |= Token.Sizeof;
      UnaryOperator |= Token.This;
      UnaryOperator |= Token.Typeof;
      UnaryOperator |= Token.BitwiseAnd;
      UnaryOperator |= Token.Plus;
      UnaryOperator |= Token.Subtract;
      UnaryOperator |= Token.Multiply;
      UnaryOperator |= Token.BitwiseNot;
      UnaryOperator |= Token.LogicalNot;
      UnaryOperator |= Token.AddOne;
      UnaryOperator |= Token.SubtractOne;

      NullableTypeNonFollower = Term | Predefined | UnaryOperator;

      TypeArgumentListNonFollower = NullableTypeNonFollower;
      //TypeArgumentListNonFollower[Token.LeftParenthesis] = false;

      CastFollower = IdentifierOrNonReservedKeyword;
      CastFollower |= Token.LeftParenthesis;
      CastFollower |= Token.This;
      CastFollower |= Token.Base;
      CastFollower |= Token.Value;
      CastFollower |= Token.AddOne;
      CastFollower |= Token.SubtractOne;
      CastFollower |= Token.New;
      CastFollower |= Token.Default;
      CastFollower |= Token.Typeof;
      CastFollower |= Token.Sizeof;
      CastFollower |= Token.Stackalloc;
      CastFollower |= Token.Delegate;
      CastFollower |= Token.Checked;
      CastFollower |= Token.Unchecked;
      CastFollower |= Token.HexLiteral;
      CastFollower |= Token.IntegerLiteral;
      CastFollower |= Token.StringLiteral;
      CastFollower |= Token.CharLiteral;
      CastFollower |= Token.RealLiteral;
      CastFollower |= Token.Null;
      CastFollower |= Token.False;
      CastFollower |= Token.True;
      CastFollower |= Token.Bool;
      CastFollower |= Token.Decimal;
      CastFollower |= Token.Sbyte;
      CastFollower |= Token.Byte;
      CastFollower |= Token.Short;
      CastFollower |= Token.Ushort;
      CastFollower |= Token.Int;
      CastFollower |= Token.Uint;
      CastFollower |= Token.Long;
      CastFollower |= Token.Ulong;
      CastFollower |= Token.Char;
      CastFollower |= Token.Float;
      CastFollower |= Token.Double;
      CastFollower |= Token.Object;
      CastFollower |= Token.String;
      CastFollower |= Token.BitwiseNot;
      CastFollower |= Token.LogicalNot;
      CastFollower |= Token.Delegate;
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
        //set{
        //  int i = (int)t;
        //  if (i < 64)
        //    if (value)
        //      this.bits0 |= (1ul << i);
        //    else
        //      this.bits0 &= ~(1ul << i);
        //  else if (i < 128)
        //    if (value)
        //      this.bits1 |= (1ul << (i-64));
        //    else
        //      this.bits1 &= ~(1ul << (i-64));
        //  else if (i < 192)
        //    if (value)
        //      this.bits2 |= (1ul << (i-128));
        //    else
        //      this.bits2 &= ~(1ul << (i-128));
        //  else
        //    if (value)
        //      this.bits3 |= (1ul << (i-192));
        //    else
        //      this.bits3 &= ~(1ul << (i-192));
        //}
      }

      //^ static TokenSet(){
        //^ int i = (int)Token.EndOfFile;
        //^ assert 0 <= i && i <= 255;
      //^ }
    }

  }
}
