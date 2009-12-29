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

        public override bool Visit(Module module, PdbReader _pdbReader)
        {
            var mutator = new Mutator(this, _pdbReader);
            mutator.Visit(module);
            return mutator.MutationCount > 0;
        }

        class Mutator
            : CcsCodeMutatorBase<NotifyAutoPropertyChanged>
        {
            public Mutator(NotifyAutoPropertyChanged owner, ISourceLocationProvider sourceLocationProvider)
                : base(owner, sourceLocationProvider)
            { }

            public int MutationCount { get; private set; }

            protected override void Visit(TypeDefinition typeDefinition)
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
                base.Visit(typeDefinition);
            }

            private bool TryFindOrCreateOnNotifyPropertyChangedMethod(
                TypeDefinition typeDefinition, 
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

            private bool TryGetEvent(TypeDefinition typeDefinition, out IEventDefinition @event, out IFieldReference field)
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
            : PlatformType
        {
            readonly IMetadataHost host;
            public TestType(IMetadataHost host)
                : base(host)
            {
                this.host = host;
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
                        this._systemAssemblyRef = new Microsoft.Cci.AssemblyReference(this.host, new AssemblyIdentity(name, core.Culture, version, core.PublicKeyToken, location));
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
