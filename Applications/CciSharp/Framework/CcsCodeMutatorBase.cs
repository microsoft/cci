//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Cci.MutableCodeModel;
using Microsoft.Cci;
using System.Diagnostics.Contracts;
using System.Diagnostics;
using CciSharp;
using Microsoft.Cci.Contracts;
using Microsoft.Cci.MutableContracts;
using Microsoft.Cci.MutableCodeModel.Contracts;

namespace CciSharp.Framework
{
    /// <summary>
    /// Abstract base class for mutator that will be run as a post build step
    /// </summary>
    public abstract class CcsCodeMutatorBase<TMutator>
        : CodeAndContractMutatingVisitor
        where TMutator : CcsMutatorBase
    {
        /// <summary>
        /// Initializes a new instance of the mutator
        /// </summary>
        /// <param name="owner">the owner mutator</param>
        /// <param name="sourceLocationProvider">the pdb reader</param>
        /// <param name="contractProvider"></param>
        protected CcsCodeMutatorBase(
            TMutator owner, 
            ISourceLocationProvider sourceLocationProvider,
            ContractProvider contractProvider)
            : base(owner.Host, sourceLocationProvider, contractProvider)
        {
            Contract.Requires(owner != null);
            Contract.Requires(contractProvider != null);
            this.Owner = owner;
        }

        [ContractInvariantMethod]
        void ObjectInvariant()
        {
            Contract.Invariant(this.Owner != null);
            Contract.Invariant(this.Host != null);
        }

        /// <summary>
        /// Gets the owner
        /// </summary>
        [ReadOnly]
        public TMutator Owner { get; private set; }

        /// <summary>
        /// Gets the host
        /// </summary>
        public ICcsHost Host
        {
            get { return this.Owner.Host; }
        }
    }
}
