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
        : CodeAndContractMutator
    {
        /// <summary>
        /// Initializes a new instance of the mutator
        /// </summary>
        /// <param name="host"></param>
        /// <param name="name"></param>
        /// <param name="priority"></param>
        protected CcsMutatorBase(ICcsHost host, string name, int priority)
            : base(host, true)
        {
            Contract.Requires(!String.IsNullOrEmpty(name));
            Contract.Requires(priority >= 0);
            this.Name = name;
            this.Priority = priority;
        }

        /// <summary>
        /// Gets the host
        /// </summary>
        public ICcsHost Host
        {
            get { return (ICcsHost)this.host; }
        }

        /// <summary>
        /// Gets the mutator name
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the mutator priority
        /// </summary>
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
