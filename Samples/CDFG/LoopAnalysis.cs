using Microsoft.Cci;
using Microsoft.Cci.Analysis;
using Microsoft.Cci.MetadataReader;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace CDFG
{
    using CFG = ControlAndDataFlowGraph<EnhancedBasicBlock<Instruction>, Instruction>;

    internal class LoopFinderVisitor : MetadataVisitor
    {
        private IMetadataHost host;
        private PdbReader pdbReader;

        public LoopFinderVisitor(IMetadataHost host, PdbReader pdbReader)
        {
            this.host = host;
            this.pdbReader = pdbReader;
        }

        public override void Visit(IMethodBody methodBody)
        {
            Console.WriteLine();
            Console.WriteLine("==========================");
            Console.WriteLine("{0}", MemberHelper.GetMemberSignature(methodBody.MethodDefinition, NameFormattingOptions.DocumentationId));

            var cdfg = ControlAndDataFlowGraph<EnhancedBasicBlock<Instruction>, Instruction>.GetControlAndDataFlowGraphFor(this.host, methodBody, this.pdbReader);
            var cfgQueries = new ControlGraphQueries<EnhancedBasicBlock<Instruction>, Instruction>(cdfg);

            var numberOfBlocks = cdfg.BlockFor.Count;
            Console.WriteLine("# blocks: {0}", numberOfBlocks);
            Console.WriteLine("CFG");
            foreach (var block in cdfg.AllBlocks)
            {
                Console.WriteLine("{0:X}, Successors: {1}", block.Offset, Offsets(cdfg.SuccessorsFor(block)));
            }

            Dictionary<EnhancedBasicBlock<Instruction>, List<EnhancedBasicBlock<Instruction>>> dominators = new Dictionary<EnhancedBasicBlock<Instruction>,List<EnhancedBasicBlock<Instruction>>>();
            foreach (var b in cdfg.AllBlocks)
            {
                var dom = new List<EnhancedBasicBlock<Instruction>>();
                foreach (var c in cdfg.AllBlocks)
                {
                    if (cfgQueries.Dominates(c, b))
                        dom.Add(c);
                }
                dominators.Add(b, dom);
            }

            var surroundingLoops = LoopFinder.GetLoopInformation(cdfg, cfgQueries, methodBody);

            Console.WriteLine("\nLoop information");
            foreach (var b in cdfg.AllBlocks)
            {
                List<EnhancedBasicBlock<Instruction>> loops;
                if (surroundingLoops.TryGetValue(b, out loops))
                {
                    Console.WriteLine("{0:X}: ({1} loop{3}) {2}",
                        b.Offset,
                        loops.Count(),
                        String.Join(",", loops.Select(l => String.Format("{0:X}", l.Offset))),
                        loops.Count() > 1 ? "s" : ""
                        );
                }
                else
                {
                    //Console.WriteLine("{0:X} is not contained in a loop", b.Offset);
                }
            }

            return;
        }
        private static string Offsets(Microsoft.Cci.UtilityDataStructures.Sublist<EnhancedBasicBlock<Instruction>> blocks)
        {
            var s = "[";
            var firstTime = true;
            foreach (var b in blocks)
            {
                if (!firstTime)
                    s += ", ";
                s += String.Format("{0:X}", b.Offset);
                firstTime = false;
            }
            s += "]";
            return s;
        }

    }

    internal class NotReducibleFlowGraph : Exception
    {

    }

    internal class LoopFinder
    {
        /// <summary>
        /// 1. We first identify a set of loop-head blocks. These are the targets of back-edges in a depth-first traversal from the start block
        /// 2. For each loop head, we start a depth first visit from all the back-edge sources backwards in the control-flow,
        ///     but stop when we reach the loop-head. (Here we assume that the loop head dominates all loop blocks)
        /// 3. For each such visited block, we add the current loop head to the list of its surrounding loops
        /// </summary>
        /// <param name="cdfg"></param>
        /// <param name="cfgQueries"></param>
        /// <param name="methodBody"></param>
        /// <returns></returns>
        public static Dictionary<EnhancedBasicBlock<Instruction>, List<EnhancedBasicBlock<Instruction>>>
            GetLoopInformation(
            CFG cdfg,
            ControlGraphQueries<EnhancedBasicBlock<Instruction>, Instruction> cfgQueries,
            IMethodBody methodBody)
        {
            var surroundingLoops = new Dictionary<EnhancedBasicBlock<Instruction>, List<EnhancedBasicBlock<Instruction>>>();
            try
            {
                ForwardDFS(cdfg, cfgQueries, new HashSet<EnhancedBasicBlock<Instruction>>(), new HashSet<EnhancedBasicBlock<Instruction>>(), surroundingLoops, cdfg.RootBlocks.First());
            }
            catch (NotReducibleFlowGraph)
            {
                return new Dictionary<EnhancedBasicBlock<Instruction>, List<EnhancedBasicBlock<Instruction>>>();
            }
            return surroundingLoops;
        }

        private static void ForwardDFS(CFG cdfg,
            ControlGraphQueries<EnhancedBasicBlock<Instruction>, Instruction> cfgQueries,
            HashSet<EnhancedBasicBlock<Instruction>> currentPath,
            HashSet<EnhancedBasicBlock<Instruction>> visitedBlocks,
            Dictionary<EnhancedBasicBlock<Instruction>, List<EnhancedBasicBlock<Instruction>>> surroundingLoops,
            EnhancedBasicBlock<Instruction> block)
        {
            if (visitedBlocks.Contains(block)) return;
            visitedBlocks.Add(block);
            currentPath.Add(block);
            var successors = cdfg.SuccessorsFor(block);
            //Console.WriteLine("{0:X}, Successors: {1}", block.Offset, Offsets(successors));
            foreach (var s in successors)
            {
                if (currentPath.Contains(s))
                {
                    // backedge from block to s
                    //WriteBlockOffset(s, 1);
                    if (!cfgQueries.Dominates(s, block))
                    {
                        throw new NotReducibleFlowGraph();
                    }
                    BackwardDFS(cdfg, cfgQueries, surroundingLoops, new HashSet<EnhancedBasicBlock<Instruction>>(), block, s);
                }
                else
                {
                    ForwardDFS(cdfg, cfgQueries, currentPath, visitedBlocks, surroundingLoops, s);
                }
            }
            currentPath.Remove(block);
        }

        private static void BackwardDFS(CFG cdfg,
            ControlGraphQueries<EnhancedBasicBlock<Instruction>, Instruction> cfgQueries,
            Dictionary<EnhancedBasicBlock<Instruction>, List<EnhancedBasicBlock<Instruction>>> surroundingLoops,
            HashSet<EnhancedBasicBlock<Instruction>> visitedBlocks,
            EnhancedBasicBlock<Instruction> block,
            EnhancedBasicBlock<Instruction> loopHead)
        {
            if (visitedBlocks.Contains(block)) return;
            visitedBlocks.Add(block);
            List<EnhancedBasicBlock<Instruction>> loops;
            if (!surroundingLoops.TryGetValue(block, out loops))
            {
                loops = new List<EnhancedBasicBlock<Instruction>>();
                surroundingLoops.Add(block, loops);
            }
            surroundingLoops[block].Add(loopHead);
            if (block == loopHead) return;

            var predecessors = cfgQueries.PredeccessorsFor(block);
            //Console.WriteLine("{0:X}, Predecessors: {1}", block.Offset, Offsets(predecessors));
            foreach (var p in predecessors)
            {
                BackwardDFS(cdfg, cfgQueries, surroundingLoops, visitedBlocks, p, loopHead);
            }
        }
    }

}