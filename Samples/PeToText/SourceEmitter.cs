
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
using System.IO;
using CSharpSourceEmitter;
using Microsoft.Cci;
using Microsoft.Cci.MetadataReader;
using Microsoft.Cci.ILToCodeModel;
using Microsoft.Cci.Contracts;

namespace PeToText {
  public class SourceEmitter : CSharpSourceEmitter.SourceEmitter {

    public SourceEmitter(ISourceEmitterOutput sourceEmitterOutput, IMetadataHost host, PdbReader/*?*/ pdbReader, bool noIL, bool printCompilerGeneratedMembers, bool noStack)
      : base(sourceEmitterOutput) {
      this.host = host;
      this.pdbReader = pdbReader;
      this.noIL = noIL;
      this.printCompilerGeneratedMembers = printCompilerGeneratedMembers;
      this.noStack = noStack;
    }

    IMetadataHost host;
    PdbReader/*?*/ pdbReader;
    bool noIL;
    bool noStack;

    public override void Traverse(IMethodBody methodBody) {
      PrintToken(CSharpToken.LeftCurly);

      ISourceMethodBody/*?*/ sourceMethodBody = methodBody as ISourceMethodBody;
      if (sourceMethodBody == null) {
        var options = DecompilerOptions.Loops;
        if (!this.printCompilerGeneratedMembers)
          options |= (DecompilerOptions.AnonymousDelegates | DecompilerOptions.Iterators);
        if (this.noStack) options |= DecompilerOptions.Unstack;
        sourceMethodBody = new SourceMethodBody(methodBody, this.host, this.pdbReader, this.pdbReader, options);
      }
      if (this.noIL)
        this.Traverse(sourceMethodBody.Block.Statements);
      else {
        this.Traverse(sourceMethodBody.Block);
        PrintToken(CSharpToken.NewLine);

        if (this.pdbReader != null)
          PrintScopes(methodBody);
        else
          PrintLocals(methodBody.LocalVariables);

        int currentIndex = -1; // a number no index matches
        foreach (IOperation operation in methodBody.Operations) {
          if (this.pdbReader != null) {
            foreach (IPrimarySourceLocation psloc in this.pdbReader.GetPrimarySourceLocationsFor(operation.Location)) {
              if (psloc.StartIndex != currentIndex) {
                PrintSourceLocation(psloc);
                currentIndex = psloc.StartIndex;
              }
            }
          }
          PrintOperation(operation);
        }
      }

      PrintToken(CSharpToken.RightCurly);
    }

    private void PrintScopes(IMethodBody methodBody) {
      foreach (ILocalScope scope in this.pdbReader.GetLocalScopes(methodBody))
        PrintScopes(scope);
    }

    private void PrintScopes(ILocalScope scope) {
      sourceEmitterOutput.Write(string.Format("IL_{0} ... IL_{1} ", scope.Offset.ToString("x4"), (scope.Offset+scope.Length).ToString("x4")), true);
      sourceEmitterOutput.WriteLine("{");
      sourceEmitterOutput.IncreaseIndent();
      PrintConstants(this.pdbReader.GetConstantsInScope(scope));
      PrintLocals(this.pdbReader.GetVariablesInScope(scope));
      sourceEmitterOutput.DecreaseIndent();
      sourceEmitterOutput.WriteLine("}", true);
    }

    private void PrintConstants(IEnumerable<ILocalDefinition> locals) {
      foreach (ILocalDefinition local in locals) {
        sourceEmitterOutput.Write("const ", true);
        PrintTypeReference(local.Type);
        sourceEmitterOutput.WriteLine(" "+this.GetLocalName(local));
      }
    }

    private void PrintLocals(IEnumerable<ILocalDefinition> locals) {
      foreach (ILocalDefinition local in locals) {
        sourceEmitterOutput.Write("", true);
        PrintTypeReference(local.Type);
        sourceEmitterOutput.WriteLine(" "+this.GetLocalName(local));
      }
    }

    public override void PrintLocalName(ILocalDefinition local) {
      this.sourceEmitterOutput.Write(this.GetLocalName(local));
    }

    private void PrintOperation(IOperation operation) {
      sourceEmitterOutput.Write("IL_" + operation.Offset.ToString("x4") + ": ", true);
      sourceEmitterOutput.Write(operation.OperationCode.ToString());
      ILocalDefinition/*?*/ local = operation.Value as ILocalDefinition;
      if (local != null)
        sourceEmitterOutput.Write(" "+this.GetLocalName(local));
      else if (operation.Value is string)
        sourceEmitterOutput.Write(" \""+operation.Value+"\"");
      else if (operation.Value != null) {
        if (OperationCode.Br_S <= operation.OperationCode && operation.OperationCode <= OperationCode.Blt_Un)
          sourceEmitterOutput.Write(" IL_"+((uint)operation.Value).ToString("x4"));
        else if (operation.OperationCode == OperationCode.Switch) {
          foreach (uint i in (uint[])operation.Value)
            sourceEmitterOutput.Write(" IL_"+i.ToString("x4"));
        } else
          sourceEmitterOutput.Write(" "+operation.Value);
      }
      sourceEmitterOutput.WriteLine("", false);
    }

    protected virtual string GetLocalName(ILocalDefinition local) {
      string localName = local.Name.Value;
      if (this.pdbReader != null) {
        foreach (IPrimarySourceLocation psloc in this.pdbReader.GetPrimarySourceLocationsForDefinitionOf(local)) {
          if (psloc.Source.Length > 0) {
            localName = psloc.Source;
            break;
          }
        }
      }
      return localName;
    }

    private void PrintSourceLocation(IPrimarySourceLocation psloc) {
      sourceEmitterOutput.WriteLine("");
      sourceEmitterOutput.Write(psloc.Document.Name.Value+"("+psloc.StartLine+":"+psloc.StartColumn+")-("+psloc.EndLine+":"+psloc.EndColumn+"): ", true);
      sourceEmitterOutput.WriteLine(psloc.Source);
    }
  }

}