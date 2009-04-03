//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using Microsoft.Cci.Ast;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci.SpecSharp {
  class ErrorReporter : ISymbolSyntaxErrorsReporter {
    private ErrorReporter() { }
    internal static readonly ErrorReporter Instance = new ErrorReporter();
  }
}