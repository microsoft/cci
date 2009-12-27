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

namespace CciSharp
{
    sealed class CcsHost
       : MetadataReaderHost
        , ICcsHost
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
            IUnit result = this.peReader.OpenModule(BinaryDocument.GetBinaryDocumentForFile(location, this));
            this.RegisterAsLatest(result);
            return result;
        }

        public Module LoadModuleFrom(string location, out PdbReader pdbReader)
        {
            var module = (IModule)this.LoadUnitFrom(location);
            if (!this.TryGetPdbReader(module, out pdbReader))
                pdbReader = null;
            
            return Decompiler.GetCodeAndContractModelFromMetadataModel(this, module, pdbReader, null);
        }
           
        public bool TryGetPdbReader(IModule module, out PdbReader reader)
        {
            lock (this.syncLock)
            {
                string pdbFile = Path.ChangeExtension(module.Location, ".pdb");
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
            this.@out.WriteLine("{0}{1}", LevelToString(level), message);
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
            Contract.Requires(!String.IsNullOrEmpty(format));
            this.Event(level, String.Format(format, args));
        }

        public void Event(CcsEventLevel level, IPrimarySourceLocation location,  string message)
        {
            this.CountErrorsAndWarnings(level);
            this.@out.WriteLine("{0}({1}): {2}{3}", location.SourceDocument.Location, location.StartLine, LevelToString(level), message);
        }

        public void Event(CcsEventLevel level, IPrimarySourceLocation location, string format, params object[] args)
        {
            Contract.Requires(location != null);
            Contract.Requires(!String.IsNullOrEmpty(format));

            this.Event(level, location, String.Format(format, args));
        }
        #endregion
    }
}
