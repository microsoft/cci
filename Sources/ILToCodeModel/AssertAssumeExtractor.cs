//
// Copyright (c) Microsoft Corporation.  All Rights Reserved.
// This code is licensed under the Microsoft Public License.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//-----------------------------------------------------------------------------
using System.Collections.Generic;
using Microsoft.Cci.MutableCodeModel;
using Microsoft.Cci.Contracts;

namespace Microsoft.Cci.ILToCodeModel {
  internal class AssertAssumeExtractor : MethodBodyCodeMutator {

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

  }
}
