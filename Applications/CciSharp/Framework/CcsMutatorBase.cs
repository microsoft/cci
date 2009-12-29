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

namespace CciSharp.Framework
{
    /// <summary>
    /// Abstract base class for mutator that will be run as a post build step
    /// </summary>
    [DebuggerDisplay("{Name}")]
    public abstract class CcsMutatorBase
    {
        readonly Type resourceType;

        /// <summary>
        /// Initializes a new instance of the mutator
        /// </summary>
        /// <param name="host"></param>
        /// <param name="name"></param>
        /// <param name="priority"></param>
        /// <param name="resourceType"></param>
        protected CcsMutatorBase(ICcsHost host, string name, int priority, Type resourceType)
        {
            Contract.Requires(host != null);
            Contract.Requires(!String.IsNullOrEmpty(name));
            Contract.Requires(priority >= 0);

            this.Host = host;
            this.Name = name;
            this.Priority = priority;
            this.resourceType = resourceType;
        }

        /// <summary>
        /// Visits and mutates the module in-place.
        /// </summary>
        /// <param name="module"></param>
        /// <param name="pdbReader">the pdb reader</param>
        public abstract bool Visit(Module module, PdbReader pdbReader);

        /// <summary>
        /// Gets the host
        /// </summary>
        [ReadOnly]
        public ICcsHost Host { get; private set; }

        /// <summary>
        /// Gets the mutator name
        /// </summary>
        [ReadOnly]
        public string Name { get; private set; }

        /// <summary>
        /// Gets the mutator priority
        /// </summary>
        [ReadOnly]
        public int Priority { get; private set; }

        /// <summary>
        /// Gets a string representation of the mutator
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.Name;
        }
    }
}
