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
            return false;
        }

        class Mutator
            : CcsCodeMutatorBase<NotifyAutoPropertyChanged>
        {
            public Mutator(NotifyAutoPropertyChanged owner, ISourceLocationProvider sourceLocationProvider)
                : base(owner, sourceLocationProvider)
            { }

            protected override void Visit(TypeDefinition typeDefinition)
            {
                if (TypeHelper.Type1ImplementsType2(
                    typeDefinition, 
                    this.Owner.testTypes.INotifyPropertyChanged))
                {
                    // find event that implements interface method
                }
                base.Visit(typeDefinition);
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
