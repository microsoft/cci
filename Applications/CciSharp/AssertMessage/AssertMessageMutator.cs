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
    /// <remarks>
    /// Also works for 'False', module decompiler bugs.
    /// </remarks>
    public sealed class AssertMessageMutator
        : CcsMutatorBase
    {
        Dictionary<uint, IMethodReference> assertMethods;

        public AssertMessageMutator(ICcsHost host)
            : base(host, "AssertMessage", 5)
        {
        }

        public override bool Visit(Module module)
        {
            var testTypes = new TestType(this.Host);
            testTypes.Visit(module);
            this.assertMethods = testTypes.Methods;
            if (this.assertMethods.Count == 0)
                return false; // nothing todo here.

            var mutator = new Mutator(this);
            mutator.Visit(module);
            return mutator.MutationCount > 0;
        }

        class Mutator 
            : CcsCodeMutatorBase<AssertMessageMutator>
        {
            readonly Microsoft.Cci.MethodReference stringFormatStringObjectArray;
            public Mutator(AssertMessageMutator owner)
                : base(owner)
            {
                this.stringFormatStringObjectArray =
                    new Microsoft.Cci.MethodReference(
                        this.Host,
                        this.Host.PlatformType.SystemString,
                        CallingConvention.Default,
                        host.PlatformType.SystemVoid,
                        this.Host.NameTable.GetNameFor("Format"), 0,
                        this.Host.PlatformType.SystemString,
                        Vector.GetVector(this.Host.PlatformType.SystemObject, this.Host.InternFactory)
                        );
            }

            public int MutationCount = 0;

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
                if (this.Owner.assertMethods.TryGetValue(methodCall.MethodToCall.InternedKey, out assertStringMethod))
                {
                    // we've got a winner here.
                    var condition = methodCall.Arguments[0];
                    var message = ExtractSourceFromPdb(condition);
                    message = PretifyMessage(assertStringMethod, message);
                    IExpression messageExpression = new CompileTimeConstant { Value = message };

                    // collect the values of locals and parameters
                    var variables = CollectSubExpressions(condition);
                    var variableNames = new Dictionary<string, object>();
                    if (variables.Count > 0)
                    {
                        var args = new CreateArray
                        {
                            ElementType = this.Host.PlatformType.SystemObject,
                            Rank = 1
                        };
                        var sb = new StringBuilder(message);
                        sb.Append(" where ");
                        int k = 0;
                        for (int i = 0; i < variables.Count; i++)
                        {
                            var variable = variables[i];
                            var name = this.GetVariableName(variable);
                            if (!variableNames.ContainsKey(name))
                            {
                                variableNames.Add(name, null);
                                if (k > 0)
                                    sb.Append(", ");
                                this.AppendVariable(sb, k, name);
                                args.Initializers.Add(variable);
                                k++;
                            }
                        }

                        message = sb.ToString();
                        messageExpression = new MethodCall
                        {
                            MethodToCall = this.stringFormatStringObjectArray,
                            Arguments = new List<IExpression>(new IExpression[] { 
                                new CompileTimeConstant { Value = message }, 
                                args })
                        };
                    }

                    var newCall = new MethodCall
                    {
                        MethodToCall = assertStringMethod,
                        Arguments = new List<IExpression>(
                            new IExpression[] { 
                                condition, 
                                messageExpression
                            }), 
                        IsStaticCall = true, 
                        Locations = methodCall.Locations
                    };
                    this.MutationCount++;
                    return newCall;
                }

                return base.Visit(methodCall);
            }

            private void AppendVariable(StringBuilder sb, int i, string name)
            {
                Contract.Requires(sb != null);
                Contract.Requires(!String.IsNullOrEmpty(name));

                sb.AppendFormat("{0} = ", name);
                sb.Append('{');
                sb.Append(i);
                sb.Append('}');
            }

            private string GetVariableName(IBoundExpression variable)
            {
                string name;
                var local = variable.Definition as ILocalDefinition;
                if (local != null)
                {
                    bool compilerGenerated;
                    name = this.pdbReader.GetSourceNameFor(local, out compilerGenerated);
                }
                else
                    name = ((INamedEntity)variable.Definition).Name.Value;
                return name;
            }

            private List<IBoundExpression> CollectSubExpressions(IExpression condition)
            {
                Contract.Requires(condition != null);

                var vis = new SubExpressionTraverser();
                vis.Visit(condition);
                return vis.Temps;
            }

            private static string PretifyMessage(IMethodReference assertStringMethod, string message)
            {
                Contract.Requires(assertStringMethod != null);
                Contract.Requires(message != null);
                Contract.Ensures(!String.IsNullOrEmpty(Contract.Result<string>()));

                var methodName = assertStringMethod.Name.Value + "(";
                // HACK: trim away Assert.True(
                int index;
                if ((index = message.IndexOf(methodName)) > -1)
                    message = message.Substring(index + methodName.Length);
                // HACK: trim away the parenthesis
                if (message.EndsWith(");"))
                    message = message.Substring(0, message.Length - 2);
                return message;
            }

            private string ExtractSourceFromPdb(IExpression condition)
            {
                Contract.Requires(condition != null);
                Contract.Ensures(!String.IsNullOrEmpty(Contract.Result<string>()));

                var sb = new StringBuilder();
                Contract.Assert(this.pdbReader != null);
                foreach (var location in condition.Locations)
                    foreach (var primarySourceLocation in this.pdbReader.GetPrimarySourceLocationsFor(location))
                    {
                        var source = primarySourceLocation.Source;
                        for (int i = 0; i < source.Length; i++)
                        {
                            char c = source[i];
                            switch (c)
                            {
                                case '{': sb.Append("{{"); break;
                                case '}': sb.Append("}}"); break;
                                default: sb.Append(c); break;
                            }
                        }
                    }

                var message = sb.ToString();
                return message;
            }
        }

        class SubExpressionTraverser
            : BaseCodeTraverser
        {
            public readonly List<IBoundExpression> Temps =
                new List<IBoundExpression>();

            public override void Visit(IBoundExpression boundExpression)
            {
                this.Temps.Add(boundExpression);
                base.Visit(boundExpression);
            }
        }

        // TODO: why do I need a platform type here?
        class TestType : PlatformType
        {
            public readonly Dictionary<uint, IMethodReference> Methods;
            readonly IMetadataHost host;
            public TestType(IMetadataHost host)
                : base(host)
            {
                this.host = host;
                this.Methods = new Dictionary<uint, IMethodReference>();
            }

            public void Visit(IModule module)
            {
                foreach(var assembly in module.AssemblyReferences)
                {
                    switch (assembly.Name.Value)
                    {
                        case "System":
                            this.AddAssertMethod(assembly, "System.Diagnostics.Debug", "Assert");
                            break;
                        case "MbUnit.Framework":
                            this.AddAssertMethod(assembly, "MbUnit.Framework.Assert", "IsTrue");
                            this.AddAssertMethod(assembly, "MbUnit.Framework.Assert", "IsFalse");
                            break;
                        case "nunit.framework":
                            this.AddAssertMethod(assembly, "NUnit.Framework.Assert", "IsTrue");
                            this.AddAssertMethod(assembly, "NUnit.Framework.Assert", "IsFalse");
                            break;
                        case "xunit": 
                            this.AddAssertMethod(assembly, "Xunit.Assert", "True");
                            this.AddAssertMethod(assembly, "Xunit.Assert", "False");
                            break;
                        case "Microsoft.Pex.Framework":
                            this.AddAssertMethod(assembly, "Microsoft.Pex.Framework.PexAssert", "IsTrue");
                            this.AddAssertMethod(assembly, "Microsoft.Pex.Framework.PexAssert", "IsFalse");
                            break;
                        case "Microsoft.VisualStudio.QualityTools.UnitTestFramework":
                            this.AddAssertMethod(assembly, "Microsoft.VisualStudio.TestTools.UnitTesting.Assert", "IsTrue");
                            this.AddAssertMethod(assembly, "Microsoft.VisualStudio.TestTools.UnitTesting.Assert", "IsFalse");
                            break;
                    }
                }
            }

            private void AddAssertMethod(
                IAssemblyReference assembly,
                string assertTypeFullName,
                string methodName)
            {
                Contract.Requires(host != null);
                Contract.Requires(assembly != null);

                var assertType = this.CreateReference(assembly, assertTypeFullName.Split('.'));
                var name = host.NameTable.GetNameFor(methodName);
                var platformType = host.PlatformType;
                var booleanType = platformType.SystemBoolean;
                var stringType = platformType.SystemString;

                this.Methods.Add(
                    new Microsoft.Cci.MethodReference(host, assertType, CallingConvention.Default, host.PlatformType.SystemVoid, name, 0, booleanType).InternedKey,
                    new Microsoft.Cci.MethodReference(host, assertType, CallingConvention.Default, host.PlatformType.SystemVoid, name, 0, booleanType, stringType)
                    );
            }
        }
    }
}
