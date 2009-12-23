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

        public void Mutate(string assembly)
        {
            Contract.Requires(!String.IsNullOrEmpty(assembly));

            var assemblyPath = Path.GetFullPath(assembly);
            // load mutators
            var mutators = LoadMutators(assemblyPath);
            PdbReader pdbReader;
            var module = this.host.LoadModuleFrom(assemblyPath, out pdbReader);
            var copier = new CodeAndContractMutator(host, false);
            var moduleCopy = copier.Visit(module);

            foreach (var mutator in mutators)
                mutator.Visit(moduleCopy);

            this.WriteModule(assemblyPath, moduleCopy, pdbReader);
      }

        private void WriteModule(string assemblyPath,  Module module, PdbReader _pdbReader)
        {
            Contract.Requires(!String.IsNullOrEmpty(assemblyPath));
            Contract.Requires(module != null);

            // write module to disk
            var newAssemblyPath = Path.ChangeExtension(assemblyPath, ".ccs" + Path.GetExtension(assemblyPath));
            Console.WriteLine("writing {0}", newAssemblyPath);
            using (var peStream = File.Create(newAssemblyPath))
            {
                if (_pdbReader == null)
                    PeWriter.WritePeToStream(module, this.host, peStream);
                else
                {
                    Contract.Assert(_pdbReader != null);
                    using (var pdbWriter = new PdbWriter(Path.ChangeExtension(newAssemblyPath, ".pdb"), _pdbReader))
                        PeWriter.WritePeToStream(module, this.host, peStream, _pdbReader, _pdbReader, pdbWriter);
                }
            }
        }

        private List<CcsMutatorBase> LoadMutators(string assemblyPath)
        {
            Contract.Requires(!String.IsNullOrEmpty(assemblyPath));

            var mutators = new List<CcsMutatorBase>();
            var directory = Path.GetDirectoryName(assemblyPath);
            foreach (var file in Directory.GetFiles(directory, "CciSharp.*.dll"))
            {
                var mutatorAssembly = System.Reflection.Assembly.LoadFrom(file);
                foreach (var type in mutatorAssembly.GetExportedTypes())
                {
                    var mutator = (CcsMutatorBase)Activator.CreateInstance(type, this.host);
                    mutators.Add(mutator);
                }
            }

            mutators.Sort((l, r) => l.Priority.CompareTo(r.Priority));
            return mutators;
        }
    }
}
