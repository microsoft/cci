using System;
using System.Collections.Generic;
using System.Text;
//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using Microsoft.Cci;
using System.IO;
using CciSharp.Framework;
using Microsoft.Cci.ILToCodeModel;
using Microsoft.Cci.MutableCodeModel;
using System.Diagnostics.Contracts;
using Microsoft.Cci.Contracts;
using Microsoft.Cci.MutableContracts;

namespace CciSharp
{
    sealed class CcsHost
       : CodeContractAwareHostEnvironment
       , ICcsHost
       , IContractAwareHost
    {
        readonly PeReader peReader;
        readonly object syncLock = new object();
        readonly TextWriter @out = Console.Out;
        int errorCount;
        int warningCount; 

        #region protected by syncLock
        readonly Dictionary<string, PdbReader> pdbReaders;
        #endregion

        public int ErrorCount
        {
            get { return this.errorCount; }
        }

        public int WarningCount
        {
            get { return this.warningCount; }
        }

        public CcsHost()
        {
            this.peReader = new PeReader(this);
            this.pdbReaders = new Dictionary<string, PdbReader>(StringComparer.OrdinalIgnoreCase);
        }

        public override IUnit LoadUnitFrom(string location)
        {
            IUnit result = this.peReader.OpenAssembly(BinaryDocument.GetBinaryDocumentForFile(location, this));
            this.RegisterAsLatest(result);
            return result;
        }

        public Assembly LoadAssemblyFrom(string location, out PdbReader pdbReader)
        {
            var assembly = (IAssembly)this.LoadUnitFrom(location);
            if (!this.TryGetPdbReader(assembly, out pdbReader))
                pdbReader = null;
            
            var decompiled = Decompiler.GetCodeModelFromMetadataModel(this, assembly, pdbReader);
            return new CodeDeepCopier(this, pdbReader).Copy(decompiled);
        }
           
        public bool TryGetPdbReader(IAssembly assembly, out PdbReader reader)
        {
            lock (this.syncLock)
            {
                string pdbFile = Path.ChangeExtension(assembly.Location, ".pdb");
                if (!this.pdbReaders.TryGetValue(pdbFile, out reader))
                    this.pdbReaders[pdbFile] = reader =
                        File.Exists(pdbFile)
                        ? new PdbReader(File.OpenRead(pdbFile), this)
                        : null;

                return reader != null;
            }
        }

        #region ICcsHost Members
        static string LevelToString(CcsEventLevel level)
        {
            switch (level)
            {
                case CcsEventLevel.Error: return "error: ";
                case CcsEventLevel.Warning:
                    return "warning: ";
                default: return "";
            }
        }
        public void Event(CcsEventLevel level, string message)
        {
            this.CountErrorsAndWarnings(level);
            this.@out.WriteLine("CciSharp: {0}{1}", LevelToString(level), message);
        }

        private void CountErrorsAndWarnings(CcsEventLevel level)
        {
            switch (level)
            {
                case CcsEventLevel.Error: this.errorCount++; break;
                case CcsEventLevel.Warning: this.warningCount++; break;
            }
        }

        public void Event(CcsEventLevel level, string format, params object[] args)
        {
            //Contract.Requires(!String.IsNullOrEmpty(format));
            this.Event(level, String.Format(format, args));
        }

        public void Event(CcsEventLevel level, IPrimarySourceLocation location,  string message)
        {
            this.CountErrorsAndWarnings(level);
            this.@out.WriteLine("CciSharp: {0}({1}): {2}{3}", location.SourceDocument.Location, location.StartLine, LevelToString(level), message);
        }

        public void Event(CcsEventLevel level, IPrimarySourceLocation location, string format, params object[] args)
        {
            //Contract.Requires(location != null);
            //Contract.Requires(!String.IsNullOrEmpty(format));

            this.Event(level, location, String.Format(format, args));
        }
        #endregion

        public Assembly MutatedAssembly { get; private set; }
        PdbReader _mutatedPdbReader;
        public bool TryGetMutatedPdbReader(out PdbReader pdbReader)
        {
            pdbReader = this._mutatedPdbReader;
            return pdbReader != null;
        }
        public ContractProvider MutatedContracts { get; private set; }

        public void LoadMutatedAssembly(string assemblyFullPath)
        {
            Contract.Requires(!String.IsNullOrEmpty(assemblyFullPath));

            var assembly = this.LoadAssemblyFrom(assemblyFullPath, out this._mutatedPdbReader);
            // Extract contracts (side effect: removes them from the method bodies)
            var contractProvider = Microsoft.Cci.MutableContracts.ContractHelper.ExtractContracts(this, assembly, this._mutatedPdbReader, this._mutatedPdbReader);

            this.MutatedAssembly = assembly;
            this.MutatedContracts = contractProvider;
        }
    }
}
