using System.Diagnostics.Contracts;
using Microsoft.Cci;
using Microsoft.Cci.Analysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Cci.UtilityDataStructures;

namespace CDFG
{

    public class FindReachingMayDefinitions
    {
        private HashSet<Instruction> reached = new HashSet<Instruction>();
        private Queue<Instruction> todo = new Queue<Instruction>();
        private Action<Instruction> onReach;

        public FindReachingMayDefinitions(Action<Instruction> onReach)
        {
            this.onReach = onReach;
        }

        public void StartFrom(Instruction from)
        {
            if (from != null)
            {
                todo.Enqueue(from);
            }
        }

        public void IterateFlowPaths()
        {
            while (todo.Count > 0)
            {
                // follow special phi instructions which have null Operations
                // and transitive flow instructions like cast and isinst

                var instruction = todo.Dequeue();

                if (reached.Contains(instruction)) continue;
                reached.Add(instruction);
                if (this.onReach != null) { onReach(instruction); }

                if (!instruction.IsMergeNode)
                {
                    switch (instruction.Operation.OperationCode)
                    {
                        // propagate
                        case OperationCode.Isinst:
                        case OperationCode.Castclass:
                            var prev = instruction.Operand1;
                            if (prev != null)
                            {
                                todo.Enqueue(prev);
                            }
                            break;

                        case OperationCode.Ldarg:
                        case OperationCode.Ldarg_0:
                        case OperationCode.Ldarg_1:
                        case OperationCode.Ldarg_2:
                        case OperationCode.Ldarg_3:
                        case OperationCode.Ldarg_S:
                        case OperationCode.Ldloc:
                        case OperationCode.Ldloc_0:
                        case OperationCode.Ldloc_1:
                        case OperationCode.Ldloc_2:
                        case OperationCode.Ldloc_3:
                        case OperationCode.Ldloc_S:
                        case OperationCode.Ldind_I:
                        case OperationCode.Ldind_I1:
                        case OperationCode.Ldind_I2:
                        case OperationCode.Ldind_I4:
                        case OperationCode.Ldind_I8:
                        case OperationCode.Ldind_R4:
                        case OperationCode.Ldind_R8:
                        case OperationCode.Ldind_Ref:
                        case OperationCode.Ldind_U1:
                        case OperationCode.Ldind_U2:
                        case OperationCode.Ldind_U4:
                            // follow the result value backwards
                            var result = instruction.Aux;
                            if (result != null)
                            {
                                todo.Enqueue(result);
                            }
                            break;

                        case OperationCode.Dup:
                            todo.Enqueue(instruction.Operand1);
                            break;
                    }
                }
                else
                {
                    // follow pseudo instruction (phi node) possibly inputs
                    foreach (var inflow in instruction.InFlows())
                    {
                        todo.Enqueue(inflow);
                    }
                }
            }
        }

        public bool IsReachable(Instruction target)
        {
            return this.reached.Contains(target);
        }
    }
}