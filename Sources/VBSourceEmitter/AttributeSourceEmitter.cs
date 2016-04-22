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

namespace VBSourceEmitter {
  public partial class SourceEmitter : CodeTraverser, IVBSourceEmitter {

    public virtual void PrintAttributes(IReference target) {
      foreach (var attribute in target.Attributes) {
        this.PrintAttribute(target, attribute, true, null);
      }
    }

    public virtual void PrintAttribute(IReference target, ICustomAttribute attribute, bool newLine, string targetType) {
      this.sourceEmitterOutput.Write("[", newLine);
      if (targetType != null) {
        this.sourceEmitterOutput.Write(targetType);
        this.sourceEmitterOutput.Write(": ");
      }
      this.PrintTypeReferenceName(attribute.Constructor.ContainingType);
      if (attribute.NumberOfNamedArguments > 0 || IteratorHelper.EnumerableIsNotEmpty(attribute.Arguments)) {
        this.sourceEmitterOutput.Write("(");
        bool first = true;
        foreach (var argument in attribute.Arguments) {
          if (first)
            first = false;
          else
            this.sourceEmitterOutput.Write(", ");
          this.Traverse(argument);
        }
        foreach (var namedArgument in attribute.NamedArguments) {
          if (first)
            first = false;
          else
            this.sourceEmitterOutput.Write(", ");
          this.Traverse(namedArgument);
        }
        this.sourceEmitterOutput.Write(")");
      }
      this.sourceEmitterOutput.Write("]");
      if (newLine) this.sourceEmitterOutput.WriteLine("");
    }

    /// <summary>
    /// Prints out a C# attribute which doesn't actually exist as an attribute in the metadata.
    /// </summary>
    /// <remarks>
    /// Perhaps callers should instead construct an ICustomAttribute instance (using some helpers), then they could just
    /// call PrintAttribute.
    /// </remarks>
    public virtual void PrintPseudoCustomAttribute(IReference target, string typeName, string arguments, bool newLine, string targetType) {
      this.sourceEmitterOutput.Write("[", newLine);
      if (targetType != null) {
        this.sourceEmitterOutput.Write(targetType);
        this.sourceEmitterOutput.Write(": ");
      }
      this.sourceEmitterOutput.Write(typeName);
      if (arguments != null) {
        this.sourceEmitterOutput.Write("(");
        this.sourceEmitterOutput.Write(arguments);
        this.sourceEmitterOutput.Write(")");
      }
      this.sourceEmitterOutput.Write("]");
      if (newLine) this.sourceEmitterOutput.WriteLine("");      
    }
  }
}
