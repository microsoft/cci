//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Cci;
using Microsoft.Cci.MutableCodeModel;
using System.Diagnostics.Contracts;
using CciSharp.Framework;

namespace CciSharp.AssertMessage
{
    public sealed class AssertMessageMutator
        : CcsMutatorBase
    {
        readonly IMethodReference isTrueBooleanMethod;
        readonly IMethodReference isTrueBooleanStringMethod;
        public AssertMessageMutator(ICcsHost host)
            : base(host, "AssertMessage", 5)
        {
            var testTypes = new TestType(host);
            this.isTrueBooleanMethod = testTypes.IsTrueBooleanMethod;
            this.isTrueBooleanStringMethod = testTypes.IsTrueBooleanStringMethod;
        }

        [ContractInvariantMethod]
        void ObjectInvariant()
        {
            Contract.Invariant(this.isTrueBooleanMethod != null);
            Contract.Invariant(this.isTrueBooleanStringMethod != null);
        }

        PdbReader pdbReader;
        public override Module Visit(Module module)
        {
            if (!this.Host.TryGetPdbReader(module, out this.pdbReader))
            {
                this.Host.Event(CcsEventLevel.Error, "missing symbols for {0}", module.Location);
                return module;
            }

            return base.Visit(module);
        }

        const string AssertTrueString = "Assert.True(";
        public override IExpression Visit(MethodCall methodCall)
        {
            if (methodCall.MethodToCall.InternedKey == this.isTrueBooleanMethod.InternedKey)
            {
                // we've got a winner here.
                var condition = methodCall.Arguments[0];

                var sb = new StringBuilder();
                Contract.Assert(this.pdbReader != null);
                foreach (var location in condition.Locations)
                    foreach (var primarySourceLocation in this.pdbReader.GetPrimarySourceLocationsFor(location))
                        sb.Append(primarySourceLocation.Source);

                var message = sb.ToString();
                // trim away Assert.True(
                if (message.StartsWith(AssertTrueString))
                    message = message.Substring(AssertTrueString.Length);
                if (message.EndsWith(");"))
                    message = message.Substring(0, message.Length - 2);
                var newCall = new MethodCall
                {
                    MethodToCall = this.isTrueBooleanStringMethod,
                    Arguments = new List<IExpression>(new IExpression[] { condition, new CompileTimeConstant { Value = message } })
                };
                return newCall;
            }

            return methodCall;
        }

        class TestType : PlatformType
        {
            readonly INamespaceTypeReference assertType;
            readonly IMethodReference isTrueBooleanMethod;
            readonly IMethodReference isTrueBooleanStringMethod;

            public TestType(IMetadataHost host)
                : base(host)
            {
                var unitTestModule = (IModule)host.LoadUnitFrom(typeof(Xunit.Assert).Assembly.Location);
                var unitTestIdentity = unitTestModule.ContainingAssembly;
                this.assertType = this.CreateReference(unitTestIdentity, "Xunit", "Assert");
                var platformType = host.PlatformType;
                var isTrueName = host.NameTable.GetNameFor("True");
                var booleanType = platformType.SystemBoolean;
                var stringType = platformType.SystemString;
                this.isTrueBooleanMethod = new Microsoft.Cci.MethodReference(host, this.assertType, CallingConvention.Default, host.PlatformType.SystemVoid, isTrueName, 0, booleanType);
                this.isTrueBooleanStringMethod = new Microsoft.Cci.MethodReference(host, this.assertType, CallingConvention.Default, host.PlatformType.SystemVoid, isTrueName, 0, booleanType, stringType);
            }

            public IMethodReference IsTrueBooleanMethod
            {
                get { return this.isTrueBooleanMethod; }
            }

            public IMethodReference IsTrueBooleanStringMethod
            {
                get { return this.isTrueBooleanStringMethod; }
            }
        }
    }
}
