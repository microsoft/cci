// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
