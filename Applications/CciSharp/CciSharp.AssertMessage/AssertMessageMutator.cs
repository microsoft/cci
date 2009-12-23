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

namespace CciSharp.Mutators
{
    /// <summary>
    /// This mutator takes Xunit' Assert.True(condition) method calls and turns them
    /// into Assert.True(condition, "condition").
    /// </summary>
    public sealed class AssertMessageMutator
        : CcsMutatorBase
    {
        readonly Dictionary<uint, IMethodReference> assertMethods;

        public AssertMessageMutator(ICcsHost host)
            : base(host, "AssertMessage", 5)
        {
            var testTypes = new TestType(host);
            this.assertMethods = testTypes.Methods;
        }

        [ContractInvariantMethod]
        void ObjectInvariant()
        {
            Contract.Invariant(this.assertMethods != null);
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

        public override IExpression Visit(MethodCall methodCall)
        {
            IMethodReference assertStringMethod;
            if (this.assertMethods.TryGetValue(methodCall.MethodToCall.InternedKey, out assertStringMethod))
            {
                // we've got a winner here.
                var condition = methodCall.Arguments[0];

                var sb = new StringBuilder();
                Contract.Assert(this.pdbReader != null);
                foreach (var location in condition.Locations)
                    foreach (var primarySourceLocation in this.pdbReader.GetPrimarySourceLocationsFor(location))
                        sb.Append(primarySourceLocation.Source);

                var message = sb.ToString();
                var methodName = assertStringMethod.Name.Value + "(";
                // HACK: trim away Assert.True(
                int index;
                if ((index = message.IndexOf(methodName)) > -1)
                    message = message.Substring(methodName.Length);
                // HACK: trim away the parenthesis
                if (message.EndsWith(");"))
                    message = message.Substring(0, message.Length - 2);

                var newCall = new MethodCall
                {
                    MethodToCall = assertStringMethod,
                    Arguments = new List<IExpression>(new IExpression[] { condition, new CompileTimeConstant { Value = message } })
                };
                return newCall;
            }

            return methodCall;
        }

        // TODO: why do I need a platform type here?
        class TestType : PlatformType
        {
            public readonly Dictionary<uint, IMethodReference> Methods;
            
            public TestType(IMetadataHost host)
                : base(host)
            {
                var unitTestModule = (IModule)host.LoadUnitFrom(typeof(Xunit.Assert).Assembly.Location);
                var unitTestIdentity = unitTestModule.ContainingAssembly;
                var assertType = this.CreateReference(unitTestIdentity, "Xunit", "Assert");
                var platformType = host.PlatformType;
                var booleanType = platformType.SystemBoolean;
                var stringType = platformType.SystemString;

                this.Methods = new Dictionary<uint, IMethodReference>();

                var isTrueName = host.NameTable.GetNameFor("True");
                this.Methods.Add(
                    new Microsoft.Cci.MethodReference(host, assertType, CallingConvention.Default, host.PlatformType.SystemVoid, isTrueName, 0, booleanType).InternedKey,
                    new Microsoft.Cci.MethodReference(host, assertType, CallingConvention.Default, host.PlatformType.SystemVoid, isTrueName, 0, booleanType, stringType)
                    );

                var isFalseName = host.NameTable.GetNameFor("False");
                this.Methods.Add(
                    new Microsoft.Cci.MethodReference(host, assertType, CallingConvention.Default, host.PlatformType.SystemVoid, isFalseName, 0, booleanType).InternedKey,
                    new Microsoft.Cci.MethodReference(host, assertType, CallingConvention.Default, host.PlatformType.SystemVoid, isFalseName, 0, booleanType, stringType)
                    );
            }
        }
    }
}
