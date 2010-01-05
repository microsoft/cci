//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Cci;
using System.Diagnostics.Contracts;
using Microsoft.Cci.MutableCodeModel;
using Microsoft.Cci.Contracts;

namespace CciSharp.Framework
{
    /// <summary>
    /// The CciSharp metadata host
    /// </summary>
    [ContractClass(typeof(ICcsHostContract))]
    public interface ICcsHost
        : IMetadataHost
    {
        /// <summary>
        /// Gets the mutated assembly
        /// </summary>
        Assembly MutatedAssembly { get; }

        /// <summary>
        /// Tries to get the mutated pdb reader
        /// </summary>
        bool TryGetMutatedPdbReader(out PdbReader pdbReader);

        /// <summary>
        /// Gets the mutated contracts
        /// </summary>
        ContractProvider MutatedContracts { get; }

        /// <summary>
        /// Tries to get a pdb reader for the given module
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="reader"></param>
        /// <returns></returns>
        bool TryGetPdbReader(IAssembly assembly, out PdbReader reader);

        /// <summary>
        /// Logs an event
        /// </summary>
        /// <param name="level"></param>
        /// <param name="message"></param>
        void Event(CcsEventLevel level, string message);

        /// <summary>
        /// Logs an event
        /// </summary>
        /// <param name="level"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        void Event(CcsEventLevel level, string format, params object[] args);

        /// <summary>
        /// Logs an event
        /// </summary>
        /// <param name="level"></param>
        /// <param name="location"></param>
        /// <param name="message"></param>
        void Event(CcsEventLevel level, IPrimarySourceLocation location, string message);

        /// <summary>
        /// Logs an event
        /// </summary>
        /// <param name="level"></param>
        /// <param name="location"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        void Event(CcsEventLevel level, IPrimarySourceLocation location, string format, params object[] args);
    }

    /// <summary>
    /// Level of logging
    /// </summary>
    public enum CcsEventLevel 
    {
        /// <summary>
        /// Error message
        /// </summary>
        Error,
        /// <summary>
        /// Warning
        /// </summary>
        Warning,
        /// <summary>
        /// Message
        /// </summary>
        Message
    }

    [ContractClassFor(typeof(ICcsHost))]
    class ICcsHostContract : ICcsHost
    {
        #region ICcsHost Members
        bool ICcsHost.TryGetPdbReader(IAssembly assembly, out PdbReader reader)
        {
            Contract.Requires(assembly != null);
            Contract.Ensures(!Contract.Result<bool>() || Contract.ValueAtReturn(out reader) != null);

            reader = default(PdbReader);
            return false;
        }
        void ICcsHost.Event(CcsEventLevel level, string message)
        {
            Contract.Requires(!String.IsNullOrEmpty(message));
            throw new NotImplementedException();
        }

        void ICcsHost.Event(CcsEventLevel level, string format, params object[] args)
        {
            Contract.Requires(!String.IsNullOrEmpty(format));
            Contract.Requires(args != null);
            throw new NotImplementedException();
        }

        void ICcsHost.Event(CcsEventLevel level, IPrimarySourceLocation location, string message)
        {
            Contract.Requires(location != null);
            Contract.Requires(!String.IsNullOrEmpty(message));
            throw new NotImplementedException();
        }

        void ICcsHost.Event(CcsEventLevel level, IPrimarySourceLocation location, string format, params object[] args)
        {
            Contract.Requires(location != null);
            Contract.Requires(!String.IsNullOrEmpty(format));
            Contract.Requires(args != null);
            throw new NotImplementedException();
        }
        #endregion

        #region IMetadataHost Members

        event EventHandler<ErrorEventArgs> IMetadataHost.Errors
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        AssemblyIdentity IMetadataHost.ContractAssemblySymbolicIdentity
        {
            get { throw new NotImplementedException(); }
        }

        AssemblyIdentity IMetadataHost.CoreAssemblySymbolicIdentity
        {
            get { throw new NotImplementedException(); }
        }

        AssemblyIdentity IMetadataHost.SystemCoreAssemblySymbolicIdentity
        {
            get { throw new NotImplementedException(); }
        }

        IAssembly IMetadataHost.FindAssembly(AssemblyIdentity assemblyIdentity)
        {
            throw new NotImplementedException();
        }

        IModule IMetadataHost.FindModule(ModuleIdentity moduleIdentity)
        {
            throw new NotImplementedException();
        }

        IUnit IMetadataHost.FindUnit(UnitIdentity unitIdentity)
        {
            throw new NotImplementedException();
        }

        IInternFactory IMetadataHost.InternFactory
        {
            get { throw new NotImplementedException(); }
        }

        IPlatformType IMetadataHost.PlatformType
        {
            get { throw new NotImplementedException(); }
        }

        IAssembly IMetadataHost.LoadAssembly(AssemblyIdentity assemblyIdentity)
        {
            throw new NotImplementedException();
        }

        IModule IMetadataHost.LoadModule(ModuleIdentity moduleIdentity)
        {
            throw new NotImplementedException();
        }

        IUnit IMetadataHost.LoadUnit(UnitIdentity unitIdentity)
        {
            throw new NotImplementedException();
        }

        IUnit IMetadataHost.LoadUnitFrom(string location)
        {
            throw new NotImplementedException();
        }

        IEnumerable<IUnit> IMetadataHost.LoadedUnits
        {
            get { throw new NotImplementedException(); }
        }

        INameTable IMetadataHost.NameTable
        {
            get { throw new NotImplementedException(); }
        }

        byte IMetadataHost.PointerSize
        {
            get { throw new NotImplementedException(); }
        }

        void IMetadataHost.ReportErrors(ErrorEventArgs errorEventArguments)
        {
            throw new NotImplementedException();
        }

        void IMetadataHost.ReportError(IErrorMessage error)
        {
            throw new NotImplementedException();
        }

        AssemblyIdentity IMetadataHost.ProbeAssemblyReference(IUnit referringUnit, AssemblyIdentity referencedAssembly)
        {
            throw new NotImplementedException();
        }

        ModuleIdentity IMetadataHost.ProbeModuleReference(IUnit referringUnit, ModuleIdentity referencedModule)
        {
            throw new NotImplementedException();
        }

        AssemblyIdentity IMetadataHost.UnifyAssembly(AssemblyIdentity assemblyIdentity)
        {
            throw new NotImplementedException();
        }

        #endregion

        Assembly ICcsHost.MutatedAssembly
        {
            get {
                Contract.Ensures(Contract.Result<Assembly>() != null);
                return null;
            }
        }

        bool ICcsHost.TryGetMutatedPdbReader(out PdbReader pdbReader)
        {
            Contract.Ensures(!Contract.Result<bool>() || Contract.ValueAtReturn(out pdbReader) != null);
            pdbReader = null;
            return false;
        }

        ContractProvider ICcsHost.MutatedContracts
        {
            get
            {
                Contract.Ensures(Contract.Result<ContractProvider>() != null);
                return null;
            }
        }
    }
}
