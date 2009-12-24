//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics.Contracts;
using Microsoft.Cci;
using Microsoft.Cci.MutableCodeModel;
using System.IO;
using CciSharp.Framework;

namespace CciSharp
{
    class CcsEngine
    {
        readonly CcsHost host;

        public CcsEngine()
        {
            this.host = new CcsHost();
        }

        public int Mutate(string assembly, IEnumerable<string> mutatorAssemblies)
        {
            Contract.Requires(!String.IsNullOrEmpty(assembly));

            var assemblyPath = Path.GetFullPath(assembly);
            // load mutators
            var mutators = LoadMutators(mutatorAssemblies);
            PdbReader pdbReader;
            var module = this.host.LoadModuleFrom(assemblyPath, out pdbReader);
            var copier = new CodeAndContractMutator(host, false);
            var moduleCopy = copier.Visit(module);

            foreach (var mutator in mutators)
            {
                if (this.host.ErrorCount > 0) break;
                mutator.Visit(moduleCopy);
            }

            if (this.host.ErrorCount == 0)
                this.WriteModule(assemblyPath, moduleCopy, pdbReader);

            return this.host.ErrorCount == 0 ? CcsExitCodes.Success : CcsExitCodes.Errors;
      }

        private void WriteModule(string assemblyPath,  Module module, PdbReader _pdbReader)
        {
            Contract.Requires(!String.IsNullOrEmpty(assemblyPath));
            Contract.Requires(module != null);

            // write module to disk
            var newAssemblyPath = Path.ChangeExtension(assemblyPath, ".ccs") + Path.GetExtension(assemblyPath);
            var pdbPath = Path.ChangeExtension(assemblyPath, ".pdb");
            var newPdbPath = Path.ChangeExtension(newAssemblyPath, ".pdb");
            this.host.Event(CcsEventLevel.Message, "rewriting {0} -> {1}", assemblyPath, newAssemblyPath);
            using (var peStream = File.Create(newAssemblyPath))
            {
                if (_pdbReader == null)
                    PeWriter.WritePeToStream(module, this.host, peStream);
                else
                {
                    Contract.Assert(_pdbReader != null);
                    this.host.Event(CcsEventLevel.Message, "rewriting {0} -> {1}", pdbPath, newPdbPath);
                    using (var pdbWriter = new PdbWriter(newPdbPath, _pdbReader))
                        PeWriter.WritePeToStream(module, this.host, peStream, _pdbReader, _pdbReader, pdbWriter);
                }
            }
        }

        private List<CcsMutatorBase> LoadMutators(IEnumerable<string> mutatorAssemblyFiles)
        {
            Contract.Requires(mutatorAssemblyFiles != null);
            var mutators = new List<CcsMutatorBase>();
            foreach (var file in mutatorAssemblyFiles)
            {
                this.host.Event(CcsEventLevel.Message, "loading mutators from {0}", file);
                var mutatorAssembly = System.Reflection.Assembly.LoadFrom(file);
                foreach (var type in mutatorAssembly.GetExportedTypes())
                {
                    if (typeof(CcsMutatorBase).IsAssignableFrom(type))
                    {
                        var mutator = (CcsMutatorBase)Activator.CreateInstance(type, this.host);
                        mutators.Add(mutator);
                    }
                }
            }

            mutators.Sort((l, r) => l.Priority.CompareTo(r.Priority));
            return mutators;
        }
    }
}
