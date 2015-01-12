using Microsoft.Cci;
using Microsoft.Cci.Analysis;
using Microsoft.Cci.MetadataReader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace CDFG
{
    using System.Diagnostics.Contracts;
    using CFG = ControlAndDataFlowGraph<EnhancedBasicBlock<Instruction>, Instruction>;

    class Program
    {
        static void Main(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                Console.WriteLine("usage: CDFG [path]fileName.ext");
                return;
            }

            using (var host = new DefaultWindowsRuntimeHost())
            {
                //Read the Metadata Model from the PE file
                var module = host.LoadUnitFrom(args[0]) as IModule;
                if (module == null || module is Dummy)
                {
                    Console.WriteLine(args[0] + " is not a PE file containing a CLR module or assembly.");
                    return;
                }

                //Get a PDB reader if there is a PDB file.
                PdbReader/*?*/ pdbReader = null;
                string pdbFile = module.DebugInformationLocation;
                if (string.IsNullOrEmpty(pdbFile) || !File.Exists(pdbFile))
                    pdbFile = Path.ChangeExtension(module.Location, "pdb");
                if (File.Exists(pdbFile))
                {
                    using (var pdbStream = File.OpenRead(pdbFile))
                    {
                        pdbReader = new PdbReader(pdbStream, host);
                    }
                }
                using (pdbReader)
                {
                    ISourceLocationProvider sourceLocationProvider = pdbReader;
                    ILocalScopeProvider localScopeProvider = pdbReader;

                    var cdfgVisitor = new LoopFinderVisitor(host, pdbReader);
                    var traverser = new MetadataTraverser();
                    traverser.TraverseIntoMethodBodies = true;
                    traverser.PreorderVisitor = cdfgVisitor;
                    traverser.Traverse(module);

                }
            }
        }
    }

 }