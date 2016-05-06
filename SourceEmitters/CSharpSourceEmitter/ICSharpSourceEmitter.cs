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
using Microsoft.Cci;

namespace CSharpSourceEmitter {
  /// <summary>
  /// This interface is a placeholder only (for now).
  /// We would like to derive this interface automatically from an annotated grammar file (X.language).
  /// This interface will reflect closely the grammar productions so that the code emitter stays
  /// in sync with the parser. For example, it will define methods signatures like these:
  /// 
  ///     ...
  ///     void PrintMethodDefinitionModifiers(IMethodDefinition methodDefinition);
  ///     void PrintMethodDefinitionVisibility(IMethodDefinition methodDefinition);
  ///     void PrintMethodDefinitionName(IMethodDefinition methodDefinition);
  ///     void PrintMethodDefinitionParameters(IMethodDefinition methodDefinition);
  ///     ...
  /// 
  /// </summary>
  interface ICSharpSourceEmitter {
  }
}
