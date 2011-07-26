//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using CciSharp.Framework;
using Microsoft.Cci.MutableCodeModel;
using Microsoft.Cci;
using System.IO;
using System.Diagnostics.Contracts;
using Microsoft.Cci.Contracts;
using Microsoft.Cci.MutableContracts;

namespace CciSharp.Mutators
{
    /// <summary>
    /// Implements the INotifyPropertyChanged interface behavior
    /// for auto-properties
    /// </summary>
    public sealed class NotifyAutoPropertyChanged
        : CcsMutatorBase
    {
        TestType testTypes;
        public NotifyAutoPropertyChanged(ICcsHost host)
            : base(host, "NotifyAutoPropertyChanged", 10, typeof(NotifyAutoPropertyResources))
        {
            this.testTypes = new TestType(host);
        }

        public override bool Visit()
        {
            var assembly = this.Host.MutatedAssembly;
            PdbReader _pdbReader;
            if (!this.Host.TryGetMutatedPdbReader(out _pdbReader))
                _pdbReader = null; 
            var contracts = this.Host.MutatedContracts;
            var mutator = new Mutator(this, _pdbReader, contracts);
            mutator.Visit(assembly);
            return mutator.MutationCount > 0;
        }

        class Mutator
            : CcsCodeMutatorBase<NotifyAutoPropertyChanged>
        {
            public Mutator(
                NotifyAutoPropertyChanged owner, 
                ISourceLocationProvider sourceLocationProvider,
                ContractProvider contracts)
                : base(owner, sourceLocationProvider, contracts)
            { }

            public int MutationCount { get; private set; }

            public override NamespaceTypeDefinition Mutate(NamespaceTypeDefinition namespaceTypeDefinition) 
            {
                this.FindOrCreateOnNotifyPropertyChangedMethod(namespaceTypeDefinition);
                return base.Mutate(namespaceTypeDefinition);
            }
            public override NestedTypeDefinition Mutate(NestedTypeDefinition nestedTypeDefinition)
            {
                this.FindOrCreateOnNotifyPropertyChangedMethod(nestedTypeDefinition);
                return base.Mutate(nestedTypeDefinition);
            }
            void FindOrCreateOnNotifyPropertyChangedMethod(ITypeDefinition typeDefinition)
            {
                if (!typeDefinition.IsInterface &&
                    TypeHelper.Type1ImplementsType2(
                    typeDefinition, 
                    this.Owner.testTypes.INotifyPropertyChanged))
                {
                    // try find the method that raises the event,
                    // or create it if necessary
                    IMethodDefinition onNotifyPropertyChangedMethod;
                    if (!this.TryFindOrCreateOnNotifyPropertyChangedMethod(typeDefinition, out onNotifyPropertyChangedMethod))
                    {
                        this.Host.Event(CcsEventLevel.Error, "could not find or create OnNotifyPropertyChanged method");
                        return;
                    }
                }
            }

            private bool TryFindOrCreateOnNotifyPropertyChangedMethod(
                ITypeDefinition typeDefinition, 
                out IMethodDefinition onNotifyPropertyChangedMethod)
            {
                for (ITypeDefinition t = typeDefinition; t != null; t = TypeHelper.BaseClass(t))
                {
                    foreach (var method in t.Methods)
                    {
                        // todo visibility
                    }
                }
                onNotifyPropertyChangedMethod = null;
                return false;
            }

            private bool TryGetEvent(NamedTypeDefinition typeDefinition, out IEventDefinition @event, out IFieldReference field)
            {
                Contract.Requires(typeDefinition != null);

                var name = this.Host.NameTable.GetNameFor("PropertyChanged");
                for (ITypeDefinition t = typeDefinition; t != null; t = TypeHelper.BaseClass(t))
                {
                    foreach (var e in typeDefinition.Events)
                    {
                        if (e.Name == name)
                        {
                            @event = e;
                            MethodDefinition adder;
                            if ((adder = e.Adder as MethodDefinition) != null &&
                                CcsHelper.TryGetFirstFieldReference(adder.Body, out field))
                                return true;

                            goto notFound;
                        }
                    }                    
                }

            notFound:
                @event = null;
                field = null;
                return false;
            }
        }

        class TestType
            : Microsoft.Cci.Immutable.PlatformType
        {
            public TestType(IMetadataHost host)
                : base(host)
            {
            }

            IAssemblyReference _systemAssemblyRef;
            /// <summary>
            /// Returns an identity that is the same as CoreAssemblyIdentity, except that the name is "System.Core" and the version is at least 3.5.
            /// </summary>
            public IAssemblyReference SystemAssemblyRef
            {
                get
                {
                    if (this._systemAssemblyRef == null)
                    {
                        var core = this.CoreAssemblyRef.AssemblyIdentity;
                        var name = this.host.NameTable.GetNameFor("System");
                        var location = core.Location;
                        if (location != null)
                            location = Path.Combine(Path.GetDirectoryName(location), "System.dll");
                        var version = core.Version;
                        this._systemAssemblyRef = new Microsoft.Cci.Immutable.AssemblyReference(this.host, new AssemblyIdentity(name, core.Culture, version, core.PublicKeyToken, location));
                    }
                    return this._systemAssemblyRef;
                }
            }

            ITypeReference _iNotifyPropertyChanged;
            public ITypeReference INotifyPropertyChanged
            {
                get
                {
                    if (this._iNotifyPropertyChanged == null)
                        this._iNotifyPropertyChanged = this.CreateReference(
                            this.SystemAssemblyRef,
                            "System", "ComponentModel", "INotifyPropertyChanged");
                    return this._iNotifyPropertyChanged;
                }
            }

            ITypeReference _propertyChangedEventHandler;
            public ITypeReference PropertyChangedEventHandler
            {
                get
                {
                    if (this._propertyChangedEventHandler == null)
                        this._propertyChangedEventHandler = this.CreateReference(
                            this.SystemAssemblyRef,
                            "System", "ComponentModel", "PropertyChangedEventHandler");
                    return this._propertyChangedEventHandler;
                }
            }
        }
    }
}
