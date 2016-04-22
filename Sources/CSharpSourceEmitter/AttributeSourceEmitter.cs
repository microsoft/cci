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
using System.Diagnostics.Contracts;

namespace CSharpSourceEmitter {
  public partial class SourceEmitter : CodeTraverser, ICSharpSourceEmitter {

    public virtual void PrintAttributes(IReference target) {
      Contract.Requires(target != null);

      foreach (var attribute in SortAttributes(target.Attributes)) {
        this.PrintAttribute(target, attribute, true, null);
      }
    }

    public static IEnumerable<ICustomAttribute> SortAttributes(IEnumerable<ICustomAttribute> attributes) {
      List<ICustomAttribute> attrs = new List<ICustomAttribute>(attributes);
      //Attribute tokens are not stable under repeated compilation of the same source, so we'll sort by attribute type name.
      //This is not stable when multiple attributes of the same type are present, but it should be good enough for repeatable tests.
      attrs.Sort(new Comparison<ICustomAttribute>((a1, a2) => {
        return string.CompareOrdinal(TypeHelper.GetTypeName(a1.Type), TypeHelper.GetTypeName(a2.Type));
      }));

      return attrs;
    }

    public virtual void PrintAttribute(IReference target, ICustomAttribute attribute, bool newLine, string targetType) {
      Contract.Requires(attribute != null);

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
