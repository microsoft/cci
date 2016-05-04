// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Cci;

namespace VBSourceEmitter {
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
  interface IVBSourceEmitter {
  }
}
