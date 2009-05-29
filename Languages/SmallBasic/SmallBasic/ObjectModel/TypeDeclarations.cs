//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using Microsoft.Cci.Ast;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci.SmallBasic {
  internal sealed class RootClassDeclaration : NamespaceClassDeclaration {

    internal RootClassDeclaration(NameDeclaration name, List<ITypeDeclarationMember> members, ISourceLocation sourceLocation)
      : base(null, Flags.Static, name,  new List<GenericTypeParameterDeclaration>(0), new List<TypeExpression>(0), members, sourceLocation) {
      this.members = members;
    }

    private void AddConstructor(Compilation compilation) {
      IName entryPoint = compilation.NameTable.GetNameFor("entryPoint");
      List<Statement> body = new List<Statement>(2);
      body.Add(new ExpressionStatement(new BaseClassConstructorCall(Expression.EmptyCollection, SourceDummy.SourceLocation)));
      body.Add(new ExpressionStatement(
        new Assignment(new TargetExpression(new SimpleName(compilation.NameTable.GetNameFor("<<first statement>>"), SourceDummy.SourceLocation, false)),
        new SimpleName(entryPoint, SourceDummy.SourceLocation, false), SourceDummy.SourceLocation)));
      List<ParameterDeclaration> parameters = new List<ParameterDeclaration>(1);
      parameters.Add(new ParameterDeclaration(null, TypeExpression.For(compilation.PlatformType.SystemInt32.ResolvedType),
        new NameDeclaration(entryPoint, SourceDummy.SourceLocation), null, 0, false, false, false, false, SourceDummy.SourceLocation));
      MethodDeclaration constructor = new MethodDeclaration(null, MethodDeclaration.Flags.SpecialName, TypeMemberVisibility.Public,
        TypeExpression.For(compilation.PlatformType.SystemVoid.ResolvedType), null, new NameDeclaration(compilation.NameTable.Ctor, SourceDummy.SourceLocation), null, 
        parameters, null, new BlockStatement(body, this.SourceLocation), this.SourceLocation);
      this.members.Add(constructor);
    }

    private void AddEntryPointMethod(Compilation compilation) {
      List<Expression> args = new List<Expression>(1);
      args.Add(new CompileTimeConstant(0, SourceDummy.SourceLocation));
      Expression createInstance = new CreateObjectInstance(new NamedTypeExpression(new SimpleName(this.Name, SourceDummy.SourceLocation, false)), args, SourceDummy.SourceLocation);
      Expression callMain = new MethodCall(
        new QualifiedName(createInstance, new SimpleName(compilation.NameTable.GetNameFor("Main"), SourceDummy.SourceLocation, false), SourceDummy.SourceLocation),
        Expression.EmptyCollection, SourceDummy.SourceLocation);
      List<Statement> body = new List<Statement>(1);
      body.Add(new ExpressionStatement(callMain));
      MethodDeclaration entryPointMethod = new MethodDeclaration(null, MethodDeclaration.Flags.Static, TypeMemberVisibility.Public,
        TypeExpression.For(compilation.PlatformType.SystemVoid.ResolvedType), null, new NameDeclaration(compilation.NameTable.GetNameFor("EntryPoint"), SourceDummy.SourceLocation), null, 
        null, null, new BlockStatement(body, this.SourceLocation), this.SourceLocation);
      this.members.Add(entryPointMethod);
    }

    internal FieldDeclaration AddFieldForLocal(SimpleName localName, Expression initialValue, ISourceLocation sourceLocation) {
      FieldDeclaration field = new FieldDeclaration(null, FieldDeclaration.Flags.Static, TypeMemberVisibility.Private, 
        TypeExpression.For(initialValue.Type), new NameDeclaration(localName.Name, localName.SourceLocation), null, sourceLocation);
      field.SetContainingTypeDeclaration(this, false);
      this.members.Add(field);
      this.localFieldFor.Add(localName.Name.UniqueKeyIgnoringCase, field.FieldDefinition);
      return field;
    }

    internal void AddLabel(SimpleName label) {
      this.labelIndex[label.Name.UniqueKey] = this.cases.Count;
      GotoStatement gotoStatement = new GotoStatement(label, SourceDummy.SourceLocation);
      List<Statement> statements = new List<Statement>(1);
      statements.Add(gotoStatement);
      this.cases.Add(new SwitchCase(new CompileTimeConstant(this.cases.Count, label.SourceLocation), statements, SourceDummy.SourceLocation));
    }

    private void AddMainMethod(Compilation compilation, List<Statement> bodyOfMainRoutine, FieldDeclaration labelField) {
      MethodDeclaration mainMethod = new MethodDeclaration(null, 0, TypeMemberVisibility.Public,
        TypeExpression.For(compilation.PlatformType.SystemVoid.ResolvedType), null, new NameDeclaration(compilation.NameTable.GetNameFor("Main"), SourceDummy.SourceLocation), null, 
        null, null, new BlockStatement(bodyOfMainRoutine, this.SourceLocation), this.SourceLocation);
      bodyOfMainRoutine.Add(new SwitchStatement(new SimpleName(labelField.Name, SourceDummy.SourceLocation, false), this.cases, SourceDummy.SourceLocation));
      bodyOfMainRoutine.Add(new LabeledStatement(new NameDeclaration(labelField.Name, SourceDummy.SourceLocation), new EmptyStatement(false, SourceDummy.SourceLocation), SourceDummy.SourceLocation));
      this.AddLabel(new SimpleName(labelField.Name, SourceDummy.SourceLocation, false));
      this.members.Add(mainMethod);
      this.mainMethod = mainMethod;
    }

    internal void AddStandardMembers(Compilation compilation, List<Statement> bodyOfMainRoutine) {
      NameDeclaration labelOfFirstStatement = new NameDeclaration(compilation.NameTable.GetNameFor("<<first statement>>"), SourceDummy.SourceLocation);
      FieldDeclaration labelField = new FieldDeclaration(null, 0, TypeMemberVisibility.Private,
        TypeExpression.For(compilation.PlatformType.SystemInt32.ResolvedType), labelOfFirstStatement, null, SourceDummy.SourceLocation);
      this.members.Add(labelField);
      this.AddMainMethod(compilation, bodyOfMainRoutine, labelField);
      this.AddEntryPointMethod(compilation);
      this.AddConstructor(compilation);
    }

    List<SwitchCase> cases = new List<SwitchCase>();

    internal int GetLabelIndex(IName labelName) {
      int result = 0;
      this.labelIndex.TryGetValue(labelName.UniqueKey, out result);
      return result;
    }

    internal Dictionary<int, FieldDefinition> localFieldFor = new Dictionary<int, FieldDefinition>();

    public MethodDeclaration MainMethod {
      get { 
        //^ assume this.mainMethod != null;
        return this.mainMethod; 
      }
    }
    MethodDeclaration/*?*/ mainMethod;

    public override INamespaceDeclarationMember MakeShallowCopyFor(NamespaceDeclaration targetNamespaceDeclaration) {
      if (this.ContainingNamespaceDeclaration == targetNamespaceDeclaration) return this;
      List<ITypeDeclarationMember> newMembers = new List<ITypeDeclarationMember>(this.members);
      return new RootClassDeclaration(this.Name, newMembers, targetNamespaceDeclaration.SourceLocation.SourceDocument.GetCorrespondingSourceLocation(this.SourceLocation));
    }

    protected override NamespaceTypeDeclaration MakeShallowCopy(List<ITypeDeclarationMember> members) {
      return new RootClassDeclaration(this.Name, members, this.sourceLocation);
    }

    Dictionary<int, int> labelIndex = new Dictionary<int, int>();

    List<ITypeDeclarationMember> members;

    public override IEnumerable<ITypeDefinitionMember> PrivateHelperMembers {
      get {
        return IteratorHelper.GetConversionEnumerable<FieldDefinition, ITypeDefinitionMember>(this.localFieldFor.Values);
      }
    }

  }
}