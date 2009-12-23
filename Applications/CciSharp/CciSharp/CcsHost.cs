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

namespace CciSharp
{
    sealed class CcsHost
       : MetadataReaderHost
        , ICcsHost
    {
        readonly PeReader peReader;
        readonly object syncLock = new object();
        #region protected by syncLock
        readonly Dictionary<string, PdbReader> pdbReaders;
        #endregion

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
    }
}
