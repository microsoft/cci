using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Cci;
using System.Diagnostics.Contracts;
using System.Resources;

namespace CciSharp.Framework
{
    /// <summary>
    /// An error message
    /// </summary>
    public sealed class CcsErrorMessage
        : ErrorMessage
    {
        readonly Type resourceType;

        /// <summary>
        /// Initializes a new instance of the error message
        /// </summary>
        /// <param name="resourceType"></param>
        /// <param name="sourceLocation"></param>
        /// <param name="errorCode"></param>
        /// <param name="messageKey"></param>
        /// <param name="relatedLocations"></param>
        /// <param name="messageArguments"></param>
        public CcsErrorMessage(
            Type resourceType,
            ISourceLocation sourceLocation,
            long errorCode,
            string messageKey,
            IEnumerable<ILocation> relatedLocations,
            string[] messageArguments)
            :base(sourceLocation, errorCode, messageKey, relatedLocations, messageArguments)
        {
            Contract.Requires(resourceType != null);
            this.resourceType = resourceType;
        }
  
        [ContractInvariantMethod]
        void ObjectInvariant()
        {
            Contract.Invariant(this.resourceType != null);
        }

        /// <summary>
        /// Gets a value indicating if the error is a warning
        /// </summary>
        public override bool IsWarning
        {
            get
            {
                return base.IsWarning;
            }
        }

        /// <summary>
        /// Gets the error reporter
        /// </summary>
        public override object ErrorReporter
        {
            get { return CcsErrorReporter.Instance; }
        }

        /// <summary>
        /// Gets the error reporter identifier
        /// </summary>
        public override string ErrorReporterIdentifier
        {
            get { return "CciSharp"; }
        }

        /// <summary>
        /// Makes a shallow copy of the source document
        /// </summary>
        /// <param name="targetDocument"></param>
        /// <returns></returns>
        public override ISourceErrorMessage MakeShallowCopy(ISourceDocument targetDocument)
        {
          if (base.SourceLocation.SourceDocument == targetDocument)
            {
                return this;
            }
            return new CcsErrorMessage(
                this.resourceType,
                targetDocument.GetCorrespondingSourceLocation(base.SourceLocation), 
                base.Code, 
                base.MessageKey, 
                base.RelatedLocations, 
                base.MessageArguments());
        }

        /// <summary>
        /// Gets the error message
        /// </summary>
        public override string Message
        {
            get 
            {
                var resourceManager = new ResourceManager(this.resourceType);
                return base.GetMessage(resourceManager);
            }
        }
    }
}
