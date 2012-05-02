//-----------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.  All Rights Reserved.
// This code is licensed under the Microsoft Public License.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Cci;
using Microsoft.Cci.MetadataReader;

namespace PeToText {
  public class SourceEmitter : MetadataTraverser {

    public SourceEmitter(TextWriter writer, IMetadataHost host, PdbReader/*?*/ pdbReader) {
      this.writer = writer;
      this.host = host;
      this.pdbReader = pdbReader;
      this.TraverseIntoMethodBodies = true;
    }

    TextWriter writer;
    IMetadataHost host;
    PdbReader/*?*/ pdbReader;

    public override void TraverseChildren(INamespaceDefinition namespaceDefinition) {
      var members = new List<INamespaceMember>(namespaceDefinition.Members);
      members.Sort((x, y) => string.CompareOrdinal(x.Name.Value, y.Name.Value));
      foreach (var member in members)
        this.Traverse(member);
    }

    public override void TraverseChildren(IMethodDefinition method) {
      this.writer.WriteLine(MemberHelper.GetMethodSignature(method, NameFormattingOptions.Signature));
      this.writer.WriteLine();
      if (!method.IsAbstract && !method.IsExternal)
        this.Traverse(method.Body);
      this.writer.WriteLine();
      this.writer.WriteLine("*******************************************************************************");
      this.writer.WriteLine();
    }

    public override void TraverseChildren(IMethodBody methodBody) {
      if (this.pdbReader != null)
        this.PrintScopes(methodBody);
      else
        this.PrintLocals(methodBody.LocalVariables);

      int currentLine = -1; // a number no index matches
      int currentCol = -1;
      foreach (IOperation operation in methodBody.Operations) {
        if (this.pdbReader != null) {
          foreach (IPrimarySourceLocation psloc in this.pdbReader.GetPrimarySourceLocationsFor(operation.Location)) {
            if (psloc.StartLine != currentLine || psloc.StartColumn != currentCol) {
              this.PrintSourceLocation(psloc);
              currentLine = psloc.StartIndex;
              currentCol = psloc.StartColumn;
            }
          }
        }
        this.PrintOperation(operation);
      }
    }

    private void PrintScopes(IMethodBody methodBody) {
      foreach (ILocalScope scope in this.pdbReader.GetLocalScopes(methodBody))
        this.PrintScopes(scope);
    }

    private void PrintScopes(ILocalScope scope) {
      this.writer.Write(string.Format("IL_{0} ... IL_{1} ", scope.Offset.ToString("x4"), (scope.Offset+scope.Length).ToString("x4")), true);
      this.writer.WriteLine("{");
      this.PrintConstants(this.pdbReader.GetConstantsInScope(scope));
      this.PrintLocals(this.pdbReader.GetVariablesInScope(scope));
      this.writer.WriteLine("}");
    }

    private void PrintConstants(IEnumerable<ILocalDefinition> locals) {
      foreach (ILocalDefinition local in locals) {
        this.writer.Write("  const ", true);
        this.PrintTypeReference(local.Type);
        this.writer.WriteLine(" "+this.GetLocalName(local)+";");
      }
    }

    private void PrintTypeReference(ITypeReference typeReference) {
      this.writer.Write(TypeHelper.GetTypeName(typeReference));
    }

    private void PrintLocals(IEnumerable<ILocalDefinition> locals) {
      foreach (ILocalDefinition local in locals) {
        this.writer.Write("  ");
        this.PrintTypeReference(local.Type);
        this.writer.WriteLine(" "+this.GetLocalName(local)+";");
      }
    }

    private void PrintLocalName(ILocalDefinition local) {
      this.writer.Write(this.GetLocalName(local));
    }

    private void PrintOperation(IOperation operation) {
      this.writer.Write("IL_" + operation.Offset.ToString("x4") + ": ", true);
      this.writer.Write(operation.OperationCode.ToString());
      ILocalDefinition/*?*/ local = operation.Value as ILocalDefinition;
      if (local != null)
        this.writer.Write(" "+this.GetLocalName(local));
      else if (operation.Value is string)
        this.writer.Write(" \""+operation.Value+"\"");
      else if (operation.Value != null) {
        if (OperationCode.Br_S <= operation.OperationCode && operation.OperationCode <= OperationCode.Blt_Un)
          this.writer.Write(" IL_"+((uint)operation.Value).ToString("x4"));
        else if (operation.OperationCode == OperationCode.Switch) {
          foreach (uint i in (uint[])operation.Value)
            this.writer.Write(" IL_"+i.ToString("x4"));
        } else
          this.writer.Write(" "+operation.Value);
      }
      this.writer.WriteLine("", false);
    }

    protected virtual string GetLocalName(ILocalDefinition local) {
      string localName = local.Name.Value;
      if (this.pdbReader != null) {
        bool isCompilerGenerated;
        localName = this.pdbReader.GetSourceNameFor(local, out isCompilerGenerated);
        if (object.ReferenceEquals(localName, local.Name.Value)) {
          foreach (IPrimarySourceLocation psloc in this.pdbReader.GetPrimarySourceLocationsForDefinitionOf(local)) {
            if (psloc.Source.Length > 0) {
              localName = psloc.Source;
              break;
            }
          }
        }
      }
      return localName;
    }

    private void PrintSourceLocation(IPrimarySourceLocation psloc) {
      this.writer.WriteLine("");
      this.writer.Write(psloc.Document.Name.Value+"("+psloc.StartLine+":"+psloc.StartColumn+")-("+psloc.EndLine+":"+psloc.EndColumn+"): ", true);
      this.writer.WriteLine(psloc.Source);
    }
  }

}