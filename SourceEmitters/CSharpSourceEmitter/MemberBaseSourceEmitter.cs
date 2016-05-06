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
