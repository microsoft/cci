//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System.Collections.Generic;
using Microsoft.Cci.MutableCodeModel;
using Microsoft.Cci.Contracts;

namespace Microsoft.Cci.ILToCodeModel {
  internal class AssertAssumeExtractor : CodeMutator {

    SourceMethodBody sourceMethodBody;

    internal AssertAssumeExtractor(SourceMethodBody sourceMethodBody)
      : base(sourceMethodBody.host, true) {
      this.sourceMethodBody = sourceMethodBody;
    }

    public override IStatement Visit(ExpressionStatement expressionStatement) {
      IMethodCall/*?*/ methodCall = expressionStatement.Expression as IMethodCall;
      if (methodCall == null) goto JustVisit;
      IMethodReference methodToCall = methodCall.MethodToCall;
      if (!TypeHelper.TypesAreEquivalent(methodToCall.ContainingType, this.sourceMethodBody.platformType.SystemDiagnosticsContractsContract)) goto JustVisit;
      string mname = methodToCall.Name.Value;
      List<IExpression> arguments = new List<IExpression>(methodCall.Arguments);
      List<ILocation> locations = new List<ILocation>(methodCall.Locations);
      if (arguments.Count != 1) goto JustVisit;
      if (mname == "Assert") {
        AssertStatement assertStatement = new AssertStatement() {
          Condition = this.Visit(arguments[0]),
          Locations = locations
        };
        return assertStatement;
      }
      if (mname == "Assume") {
        AssumeStatement assumeStatement = new AssumeStatement() {
          Condition = this.Visit(arguments[0]),
          Locations = locations
        };
        return assumeStatement;
      }
    JustVisit:
      return base.Visit(expressionStatement);
    }

    public override IFieldReference Visit(IFieldReference fieldReference) {
      return fieldReference;
    }

    public override IMethodReference Visit(IMethodReference methodReference) {
      return methodReference;
    }

    public override ITypeReference Visit(ITypeReference typeReference) {
      return typeReference;
    }

  }
}
