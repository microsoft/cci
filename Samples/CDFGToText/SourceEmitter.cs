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
using Microsoft.Cci.Analysis;

namespace CdfgToText {
  public class SourceEmitter : MetadataTraverser {

    public SourceEmitter(TextWriter sourceEmitterOutput, IMetadataHost host, PdbReader/*?*/ pdbReader, FileStream/*?*/ profileReader) {
      this.sourceEmitterOutput = sourceEmitterOutput;
      this.host = host;
      this.pdbReader = pdbReader;
      this.profileReader = profileReader;
    }

    IMetadataHost host;
    PdbReader/*?*/ pdbReader;
    FileStream/*?*/ profileReader;
    TextWriter sourceEmitterOutput;

    ControlAndDataFlowGraph<BasicBlock<Instruction>, Instruction> cdfg;

    public override void TraverseChildren(IAssembly assembly) {
      foreach (var t in assembly.GetAllTypes()) this.Traverse(t);
    }

    public override void TraverseChildren(ITypeDefinition typeDefinition) {
      if (this.PrintDefinitionSourceLocations(typeDefinition))
        this.sourceEmitterOutput.WriteLine(TypeHelper.GetTypeName(typeDefinition));
      base.TraverseChildren(typeDefinition);
    }

    public override void TraverseChildren(ITypeDefinitionMember typeMember) {
      if (this.PrintDefinitionSourceLocations(typeMember))
        this.sourceEmitterOutput.WriteLine(MemberHelper.GetMemberSignature(typeMember, NameFormattingOptions.DocumentationId));
      base.TraverseChildren(typeMember);
    }

    private bool PrintDefinitionSourceLocations(IDefinition definition) {
      bool result = false;
      if (this.pdbReader != null) {
        foreach (var psLoc in this.pdbReader.GetPrimarySourceLocationsFor(definition.Locations)) {
          this.PrintSourceLocation(psLoc);
          result = true;
        }
      }
      return result;
    }

    public override void Traverse(IMethodBody methodBody) {
      sourceEmitterOutput.WriteLine("");
      if (this.pdbReader != null)
        PrintScopes(methodBody);
      else
        PrintLocals(methodBody.LocalVariables);

      this.cdfg = ControlAndDataFlowGraph<BasicBlock<Instruction>, Instruction>.GetControlAndDataFlowGraphFor(host, methodBody, this.pdbReader);
      var numberOfBlocks = this.cdfg.BlockFor.Count;

      foreach (var block in this.cdfg.AllBlocks) {
        this.PrintBlock(block);
      }

      sourceEmitterOutput.WriteLine("**************************************************************");
      sourceEmitterOutput.WriteLine();
    }

    private void PrintBlock(BasicBlock<Instruction> block) {
      sourceEmitterOutput.WriteLine("start of basic block "+block.Instructions[0].Operation.Offset.ToString("x4"));
      sourceEmitterOutput.WriteLine("  Initial stack:");
      foreach (var instruction in block.OperandStack)
        sourceEmitterOutput.WriteLine("    "+TypeHelper.GetTypeName(instruction.Type));
      sourceEmitterOutput.WriteLine();

      sourceEmitterOutput.WriteLine("  Instructions: offset, opcode, type, instructions that flow into this");
      foreach (var instruction in block.Instructions)
        this.PrintInstruction(instruction);

      foreach (var successor in this.cdfg.SuccessorsFor(block)) {
        sourceEmitterOutput.Write("  successor block "+successor.Instructions[0].Operation.Offset.ToString("x4"));
        if (this.profileReader != null) {
          uint count = this.ReadCountFromProfile();
          sourceEmitterOutput.Write(" traversed "+count+" times");
        }
        sourceEmitterOutput.WriteLine();
      }
      sourceEmitterOutput.WriteLine("end of basic block");
      sourceEmitterOutput.WriteLine();
    }

    private uint ReadCountFromProfile() {
      this.profileReader.Read(this.buffer, 0, 4);
      return (uint)((this.buffer[3] << 24) | (this.buffer[2] << 16) | (this.buffer[1] << 8) | this.buffer[0]);
    }
    byte[] buffer = new byte[4];

    private void PrintInstruction(Instruction instruction) {
      if (this.pdbReader != null) {
        foreach (IPrimarySourceLocation psloc in this.pdbReader.GetPrimarySourceLocationsFor(instruction.Operation.Location)) {
          PrintSourceLocation(psloc);
          break;
        }
      }

      sourceEmitterOutput.Write("    ");
      sourceEmitterOutput.Write(instruction.Operation.Offset.ToString("x4"));
      sourceEmitterOutput.Write(", ");
      sourceEmitterOutput.Write(instruction.Operation.OperationCode.ToString());
      if (instruction.Operation.Value is uint)
        sourceEmitterOutput.Write(" "+((uint)instruction.Operation.Value).ToString("x4"));
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
      if (instruction.Operation is Dummy)
        sourceEmitterOutput.Write("stack");
      else
        sourceEmitterOutput.Write(instruction.Operation.Offset.ToString("x4"));
    }

    private void PrintScopes(IMethodBody methodBody) {
      foreach (ILocalScope scope in this.pdbReader.GetLocalScopes(methodBody))
        PrintScopes(scope);
    }

    private void PrintScopes(ILocalScope scope) {
      sourceEmitterOutput.Write(string.Format("IL_{0} ... IL_{1} ", scope.Offset.ToString("x4"), (scope.Offset+scope.Length).ToString("x4")));
      sourceEmitterOutput.WriteLine("{");
      PrintConstants(this.pdbReader.GetConstantsInScope(scope));
      PrintLocals(this.pdbReader.GetVariablesInScope(scope));
      sourceEmitterOutput.WriteLine("}");
    }

    private void PrintConstants(IEnumerable<ILocalDefinition> locals) {
      foreach (ILocalDefinition local in locals) {
        sourceEmitterOutput.Write("  const ");
        sourceEmitterOutput.Write(TypeHelper.GetTypeName(local.Type));
        sourceEmitterOutput.WriteLine(" "+this.GetLocalName(local));
      }
    }

    private void PrintLocals(IEnumerable<ILocalDefinition> locals) {
      foreach (ILocalDefinition local in locals) {
        sourceEmitterOutput.Write("  ");
        sourceEmitterOutput.Write(TypeHelper.GetTypeName(local.Type));
        sourceEmitterOutput.WriteLine(" "+this.GetLocalName(local));
      }
    }

    private void PrintOperation(IOperation operation) {
      sourceEmitterOutput.Write("IL_" + operation.Offset.ToString("x4") + ": ");
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
      sourceEmitterOutput.Write(psloc.Document.Name.Value+"("+psloc.StartLine+":"+psloc.StartColumn+")-("+psloc.EndLine+":"+psloc.EndColumn+"): ");
      var source = psloc.Source;
      var newLinePos = source.IndexOf('\n');
      if (newLinePos > 0) source = source.Substring(0, newLinePos);
      sourceEmitterOutput.WriteLine(source);
    }
  }

}
