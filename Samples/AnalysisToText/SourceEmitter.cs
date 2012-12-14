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
using Microsoft.Cci.Analysis;
using Microsoft.Cci.MetadataReader;
using Microsoft.Cci.UtilityDataStructures;

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

    ControlAndDataFlowGraph<AiBasicBlock<Instruction>, Instruction> cdfg;
    ControlGraphQueries<AiBasicBlock<Instruction>, Instruction> cfgQueries;
    ValueMappings<Instruction> valueMappings;

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
      this.sourceEmitterOutput.WriteLine(MemberHelper.GetMethodSignature(methodBody.MethodDefinition,
        NameFormattingOptions.Signature|NameFormattingOptions.ReturnType|NameFormattingOptions.ParameterModifiers|NameFormattingOptions.ParameterName));
      sourceEmitterOutput.WriteLine("");
      if (this.pdbReader != null)
        PrintScopes(methodBody);
      else
        PrintLocals(methodBody.LocalVariables);

      this.cdfg = ControlAndDataFlowGraph<AiBasicBlock<Instruction>, Instruction>.GetControlAndDataFlowGraphFor(host, methodBody, this.pdbReader);
      this.cfgQueries = new ControlGraphQueries<AiBasicBlock<Instruction>, Instruction>(this.cdfg);
      SingleAssigner<AiBasicBlock<Instruction>, Instruction>.GetInSingleAssignmentForm(host.NameTable, this.cdfg, this.cfgQueries, this.pdbReader);
      this.valueMappings = new ValueMappings<Instruction>(this.host.PlatformType, new Z3Wrapper.Wrapper(host.PlatformType));
      AbstractInterpreter<AiBasicBlock<Instruction>, Instruction>.InterpretUsingAbstractValues(this.cdfg, this.cfgQueries, this.valueMappings);

      var numberOfBlocks = this.cdfg.BlockFor.Count;

      foreach (var block in this.cdfg.AllBlocks) {
        this.PrintBlock(block);
      }

      sourceEmitterOutput.WriteLine("**************************************************************");
      sourceEmitterOutput.WriteLine();
    }

    private void PrintBlock(AiBasicBlock<Instruction> block) {
      sourceEmitterOutput.WriteLine("start of basic block "+block.Offset.ToString("x4"));
      if (this.cfgQueries != null) {
        var immediateDominator = this.cfgQueries.ImmediateDominator(block);
        sourceEmitterOutput.WriteLine("  immediate dominator "+immediateDominator.Offset.ToString("x4"));
        foreach (var predecessor in this.cfgQueries.PredeccessorsFor(block)) {
          sourceEmitterOutput.WriteLine("  predecessor block "+predecessor.Offset.ToString("x4"));
        }
      }
      if (block.Joins != null) {
        sourceEmitterOutput.WriteLine("  joins:");
        foreach (var join in block.Joins) {
          sourceEmitterOutput.Write("    "+join.NewLocal+" = " + this.GetLocalOrParameterName(join.OriginalLocal)+", via join(");
          sourceEmitterOutput.Write(join.Join1.Name.Value);
          if (join.Join2 != null) sourceEmitterOutput.Write(", "+join.Join2.Name.Value);
          if (join.OtherJoins != null)
            foreach (var otherJoin in join.OtherJoins) sourceEmitterOutput.Write(", "+otherJoin.Name.Value);
          sourceEmitterOutput.WriteLine(")");
        }
      }
      sourceEmitterOutput.WriteLine("  Initial stack:");
      foreach (var instruction in block.OperandStack) {
        sourceEmitterOutput.Write("    "+TypeHelper.GetTypeName(instruction.Type));
        sourceEmitterOutput.Write(' ');
        this.PrintPhiNode(instruction);
        sourceEmitterOutput.WriteLine();
      }
      sourceEmitterOutput.WriteLine();

      if (block.ConstraintsAtEntry.Count > 0) {
        sourceEmitterOutput.WriteLine("  Initial constraints:");
        bool noConstraints = true;
        foreach (var constraintList in block.ConstraintsAtEntry) {
          if (constraintList == null) continue;
          foreach (var constraint in constraintList) {
            if (constraint == null) continue;
            noConstraints = false;
            break;
          }
        }
        if (noConstraints) {
          sourceEmitterOutput.WriteLine();
        } else {
          bool firstList = true;
          foreach (var constraintList in block.ConstraintsAtEntry) {
            if (firstList) firstList = false; else sourceEmitterOutput.WriteLine("    or");
            var firstConstraint = true;
            if (constraintList != null) {
              foreach (var constraint in constraintList) {
                if (constraint == null) continue;
                if (firstConstraint) firstConstraint = false; else sourceEmitterOutput.WriteLine("      and");
                sourceEmitterOutput.Write("        ");
                this.PrintExpression(constraint);
                sourceEmitterOutput.WriteLine();
              }
            }
            if (firstConstraint) sourceEmitterOutput.WriteLine("        none");
          }
        }
      }

      sourceEmitterOutput.WriteLine("  Instructions: offset, opcode, type, instructions that flow into this");
      foreach (var instruction in block.Instructions)
        this.PrintInstruction(instruction, block);

      int i = 0;
      foreach (var successor in this.cdfg.SuccessorsFor(block)) {
        sourceEmitterOutput.Write("  successor block "+successor.Offset.ToString("x4"));
        if (this.profileReader != null) {
          uint count = this.ReadCountFromProfile();
          sourceEmitterOutput.Write(" traversed "+count+" times");
        }
        sourceEmitterOutput.WriteLine();
        if (block.ConstraintsAtExit.Count > i) {
          sourceEmitterOutput.WriteLine("  constraints at successor edge");
          sourceEmitterOutput.Write("    ");
          var constraintList = block.ConstraintsAtExit[i];
          bool first = true;
          foreach (var constraint in constraintList) {
            if (constraint == null) continue;
            if (first) first = false; else sourceEmitterOutput.Write(" and ");
            this.PrintExpression(constraint);
          }
          sourceEmitterOutput.WriteLine();
        }
        i++;
      }
      sourceEmitterOutput.WriteLine("end of basic block");
      sourceEmitterOutput.WriteLine();
    }

    private string GetLocalOrParameterName(object locOrPar) {
      if (locOrPar == null) return "this";
      var local = locOrPar as ILocalDefinition;
      if (local != null)
        return this.GetLocalName(local);
      else if (locOrPar == Dummy.ParameterDefinition)
        return "this";
      else
        return ((IParameterDefinition)locOrPar).Name.Value;
    }

    private uint ReadCountFromProfile() {
      this.profileReader.Read(this.buffer, 0, 4);
      return (uint)((this.buffer[3] << 24) | (this.buffer[2] << 16) | (this.buffer[1] << 8) | this.buffer[0]);
    }
    byte[] buffer = new byte[4];

    private void PrintInstruction(Instruction instruction, AiBasicBlock<Instruction> block) {
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
      if (instruction.Operation.Value != null) {
        sourceEmitterOutput.Write(", ");
        sourceEmitterOutput.Write(instruction.Operation.Value.ToString());
      }
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
      if (this.valueMappings != null) {
        var constVal = this.valueMappings.GetCompileTimeConstantValueFor(instruction, block);
        if (constVal != null) {
          if (constVal is Dummy)
            sourceEmitterOutput.WriteLine("      operation always fails to produce a value");
          else {
            sourceEmitterOutput.Write("      pushes constant: ");
            sourceEmitterOutput.WriteLine((constVal.Value??"null").ToString());
          }
        } else {
          var canonicalExpr = this.valueMappings.GetCanonicalExpressionFor(instruction);
          if (canonicalExpr != null) {
            sourceEmitterOutput.Write("      canonical expression: ");
            this.PrintExpression(canonicalExpr);
            sourceEmitterOutput.WriteLine();
            var interval = this.valueMappings.GetIntervalFor(canonicalExpr, block);
            if (interval != null) {
              sourceEmitterOutput.Write("      interval: ");
              this.PrintInterval(interval);
              sourceEmitterOutput.WriteLine();
            }
            if (instruction.Type.TypeCode == PrimitiveTypeCode.Boolean) {
              var isTrue = this.valueMappings.CheckIfExpressionIsTrue(canonicalExpr, block);
              if (isTrue.HasValue) {
                sourceEmitterOutput.WriteLine("      is always "+isTrue.Value);
              }
            }
          }
        }
      }
    }

    private void PrintFlowFrom(Instruction instruction) {
      if (instruction.Operation.OperationCode == OperationCode.Nop && instruction.Operation.Value is INamedEntity)
        sourceEmitterOutput.Write("stack slot "+instruction.Operation.Offset);
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
      if (this.pdbReader != null) {
        bool isCompilerGenerated;
        return this.pdbReader.GetSourceNameFor(local, out isCompilerGenerated);
      }
      return local.Name.Value;
    }

    private void PrintSourceLocation(IPrimarySourceLocation psloc) {
      sourceEmitterOutput.WriteLine("");
      sourceEmitterOutput.Write(psloc.Document.Name.Value+"("+psloc.StartLine+":"+psloc.StartColumn+")-("+psloc.EndLine+":"+psloc.EndColumn+"): ");
      var source = psloc.Source;
      var newLinePos = source.IndexOf('\n');
      if (newLinePos > 0) source = source.Substring(0, newLinePos);
      sourceEmitterOutput.WriteLine(source);
    }

    private void PrintExpression(Instruction canonicalExpr) {
      if (canonicalExpr.Operation.OperationCode == OperationCode.Nop && canonicalExpr.Operation.Value is INamedEntity) {
        this.PrintPhiNode(canonicalExpr);
        return;
      }
      var operands2ToN = canonicalExpr.Operand2 as Instruction[];
      if (operands2ToN != null) {
        sourceEmitterOutput.Write('(');
        this.PrintOperator(canonicalExpr.Operation);
        sourceEmitterOutput.Write(' ');
        this.PrintExpression(canonicalExpr.Operand1);
        foreach (var operand in operands2ToN) {
          sourceEmitterOutput.Write(", ");
          this.PrintExpression(operand);
        }
        sourceEmitterOutput.Write(')');
        return;
      }
      var operand2 = canonicalExpr.Operand2 as Instruction;
      if (operand2 != null) {
        sourceEmitterOutput.Write('(');
        this.PrintExpression(canonicalExpr.Operand1);
        sourceEmitterOutput.Write(' ');
        this.PrintOperator(canonicalExpr.Operation);
        sourceEmitterOutput.Write(' ');
        this.PrintExpression(operand2);
        sourceEmitterOutput.Write(')');
      } else if (canonicalExpr.Operand1 != null) {
        this.PrintOperator(canonicalExpr.Operation);
        sourceEmitterOutput.Write(' ');
        this.PrintExpression(canonicalExpr.Operand1);
      } else {
        this.PrintOperator(canonicalExpr.Operation);
      }
    }

    private void PrintPhiNode(Instruction canonicalExpr) {
      var namedEntity = (INamedEntity)canonicalExpr.Operation.Value;
      if (namedEntity.Name is Dummy)
        sourceEmitterOutput.Write("stack slot "+canonicalExpr.Operation.Offset);
      else
        sourceEmitterOutput.Write(namedEntity.Name.Value);
      sourceEmitterOutput.Write(" = phi(");
      if (canonicalExpr.Operand1 != null) {
        this.PrintExpression(canonicalExpr.Operand1);
        var operand2 = canonicalExpr.Operand2 as Instruction;
        if (operand2 != null) {
          sourceEmitterOutput.Write(", ");
          this.PrintExpression(operand2);
        } else {
          var operands2ToN = canonicalExpr.Operand2 as Instruction[];
          if (operands2ToN != null) {
            foreach (var operand in operands2ToN) {
              sourceEmitterOutput.Write(", ");
              this.PrintExpression(operand);
            }
          }
        }
      }
      sourceEmitterOutput.Write(")");
    }

    private void PrintOperator(IOperation operation) {
      sourceEmitterOutput.Write(operation.OperationCode.ToString());
      if (operation.Value != null) {
        sourceEmitterOutput.Write(" ");
        sourceEmitterOutput.Write(operation.Value.ToString());
      }
    }

    private void PrintInterval(Interval interval) {
      if (interval.LowerBound is Dummy)
        sourceEmitterOutput.Write("-infinity");
      else
        this.sourceEmitterOutput.Write(interval.LowerBound.Value.ToString());
      sourceEmitterOutput.Write(" .. ");
      if (interval.UpperBound is Dummy)
        sourceEmitterOutput.Write("infinity");
      else
        this.sourceEmitterOutput.Write(interval.UpperBound.Value.ToString());
      if (interval.IncludesDivisionByZero)
        sourceEmitterOutput.Write(", expression might divide by zero");
      if (interval.IncludesOverflow)
        sourceEmitterOutput.Write(", expression might overflow");
      if (interval.IncludesUnderflow)
        sourceEmitterOutput.Write(", expression might underflow");
      if (interval.ExcludesZero && !Evaluator.IsPositive(interval.LowerBound) && !Evaluator.IsNegative(interval.UpperBound)) {
        sourceEmitterOutput.Write(", expression never results in zero");
      }
    }



  }

}
