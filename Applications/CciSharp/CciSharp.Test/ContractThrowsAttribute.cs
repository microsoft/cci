using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics.Contracts;
using Xunit;
using Xunit.Sdk;

namespace CciSharp.Test
{
    public sealed class ContractThrowsAttribute
        : Xunit.BeforeAfterTestAttribute
    {
        public override void Before(MethodInfo methodUnderTest)
        {
            Contract.ContractFailed += Contract_ContractFailed;
        }
        public override void After(MethodInfo methodUnderTest)
        {
            Contract.ContractFailed -= Contract_ContractFailed;
        }

        void Contract_ContractFailed(object sender, ContractFailedEventArgs e)
        {
            e.SetHandled();
            throw new AssertException(
                String.Format("{0}: {1}, {2}", e.FailureKind, e.Condition, e.Message)
                );
        }
    }
}
