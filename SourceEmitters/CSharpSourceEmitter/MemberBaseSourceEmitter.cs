// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Cci;

namespace CSharpSourceEmitter {
  public partial class SourceEmitter : CodeTraverser, ICSharpSourceEmitter {
    public virtual void PrintTypeMemberVisibility(TypeMemberVisibility visibility) {
      switch (visibility) {
        case TypeMemberVisibility.Public:
          PrintKeywordPublic();
          break;

        case TypeMemberVisibility.Private:
          PrintKeywordPrivate();
          break;

        case TypeMemberVisibility.Assembly:
          PrintKeywordInternal();
          break;

        case TypeMemberVisibility.Family:
          PrintKeywordProtected();
          break;

        case TypeMemberVisibility.FamilyOrAssembly:
          PrintKeywordProtectedInternal();
          break;

        case TypeMemberVisibility.FamilyAndAssembly:
        default:
          sourceEmitterOutput.Write("Invalid-visibility ");
          break;
      }
    }

  }
}
