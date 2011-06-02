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
using Microsoft.Cci;
using Microsoft.Cci.MetadataReader;
using Microsoft.Cci.UtilityDataStructures;

namespace CdfgToText {
  public class SourceEmitter : MetadataTraverser {

    public SourceEmitter(TextWriter sourceEmitterOutput, IMetadataHost host, PdbReader/*?*/ pdbReader) {
      this.sourceEmitterOutput = sourceEmitterOutput;
      this.host = host;
      this.pdbReader = pdbReader;
    }

    IMetadataHost host;
    PdbReader/*?*/ pdbReader;
    TextWriter sourceEmitterOutput;

    Queue<BasicBlock<Instruction>> blocksToVisit;
    SetOfObjects blocksAlreadyVisited;

    public override void Traverse(IMethodBody methodBody) {
      sourceEmitterOutput.WriteLine(MemberHelper.GetMethodSignature(methodBody.MethodDefinition, NameFormattingOptions.Signature));
      sourceEmitterOutput.WriteLine();

      if (this.pdbReader != null)
        PrintScopes(methodBody);
      else
        PrintLocals(methodBody.LocalVariables);

      var cfg = ControlAndDataFlowGraph<BasicBlock<Instruction>, Instruction>.GetControlAndDataFlowGraphFor(host, methodBody);
      var numberOfBlocks = cfg.BlockFor.Count;
      this.blocksToVisit = new Queue<BasicBlock<Instruction>>((int)numberOfBlocks);
      this.blocksAlreadyVisited = new SetOfObjects(numberOfBlocks);

      foreach (var root in cfg.RootBlocks) {
        this.blocksToVisit.Enqueue(root);
        while (this.blocksToVisit.Count != 0)
          this.PrintBlock();
      }

      sourceEmitterOutput.WriteLine("**************************************************************");
      sourceEmitterOutput.WriteLine();
    }

    private void PrintBlock() {
      var block = this.blocksToVisit.Dequeue();
      this.blocksAlreadyVisited.Add(block);

      sourceEmitterOutput.WriteLine("start of basic block", true);
      sourceEmitterOutput.WriteLine("  Initial stack:", true);
      foreach (var instruction in block.OperandStack)
        sourceEmitterOutput.WriteLine("    "+TypeHelper.GetTypeName(instruction.Type));
      sourceEmitterOutput.WriteLine("");

      sourceEmitterOutput.WriteLine("  Instructions: offset, opcode, type, instructions that flow into this");
      foreach (var instruction in block.Instructions)
        this.PrintInstruction(instruction);

      sourceEmitterOutput.WriteLine("end of basic block", true);
      sourceEmitterOutput.WriteLine("");

      foreach (var successor in block.Successors) {
        if (blocksAlreadyVisited.Contains(successor)) continue;
        blocksToVisit.Enqueue(successor);
      }
    }

    private void PrintInstruction(Instruction instruction) {
      //TODO: print out source line information if available

      sourceEmitterOutput.Write("    ");
      sourceEmitterOutput.Write(instruction.Operation.Offset.ToString("x4"), true);
      sourceEmitterOutput.Write(", ");
      sourceEmitterOutput.Write(instruction.Operation.OperationCode.ToString());
      sourceEmitterOutput.Write(", ");
      sourceEmitterOutput.Write(TypeHelper.GetTypeName(instruction.Type));
      if (instruction.Operand1 != null) {
        sourceEmitterOutput.Write(", ");
        this.PrintFlowFrom(instruction.Operand1);
      }
      var i2 = instruction.Operand2 as Instruction;
      if (i2 != null) {
        sourceEmitterOutput.Write(", ");
        this.PrintFlowFrom(i2);
      } else {
        var i2a = instruction.Operand2 as Instruction[];
        if (i2a != null) {
          foreach (var i2e in i2a) {
            sourceEmitterOutput.Write(", ");
            this.PrintFlowFrom(i2e);
          }
        }
      }
      sourceEmitterOutput.WriteLine("");
    }

    private void PrintFlowFrom(Instruction instruction) {
      if (instruction.Operation == Dummy.Operation)
        sourceEmitterOutput.Write("stack");
      else
        sourceEmitterOutput.Write(instruction.Operation.Offset.ToString("x4"));
    }

    private void PrintScopes(IMethodBody methodBody) {
      foreach (ILocalScope scope in this.pdbReader.GetLocalScopes(methodBody))
        PrintScopes(scope);
    }

    private void PrintScopes(ILocalScope scope) {
      sourceEmitterOutput.Write(string.Format("IL_{0} ... IL_{1} ", scope.Offset.ToString("x4"), scope.Length.ToString("x4")), true);
      sourceEmitterOutput.WriteLine("{");
      PrintConstants(this.pdbReader.GetConstantsInScope(scope));
      PrintLocals(this.pdbReader.GetVariablesInScope(scope));
      sourceEmitterOutput.WriteLine("}", true);
    }

    private void PrintConstants(IEnumerable<ILocalDefinition> locals) {
      foreach (ILocalDefinition local in locals) {
        sourceEmitterOutput.Write("  ");
        sourceEmitterOutput.Write("const ", true);
        sourceEmitterOutput.Write(TypeHelper.GetTypeName(local.Type));
        sourceEmitterOutput.WriteLine(" "+this.GetLocalName(local));
      }
    }

    private void PrintLocals(IEnumerable<ILocalDefinition> locals) {
      foreach (ILocalDefinition local in locals) {
        sourceEmitterOutput.Write("  ");
        sourceEmitterOutput.Write("", true);
        sourceEmitterOutput.Write(TypeHelper.GetTypeName(local.Type));
        sourceEmitterOutput.WriteLine(" "+this.GetLocalName(local));
      }
    }

    public void PrintLocalName(ILocalDefinition local) {
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