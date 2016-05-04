// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Microsoft.Cci;
using Microsoft.Cci.MetadataReader;
using Microsoft.Cci.MutableCodeModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization; // needed for defining exception .ctors
using System.Text;
using System.Diagnostics.Contracts;

namespace AsmMeta {
  // For deciding whether to keep a member, there are several interesting axes:
  //  Visibility: Keep everything, or only externally visible items, or external + friends (w/ FriendAccessAllowedAttribute)
  //  Security: Keep all methods, or only methods not marked with the SecurityCriticalAttribute.  May add an HPA filter.
  //  Obsolete methods: Keep items marked with ObsoleteAttribute, or only if the ObsoleteAttribute's IsError flag is set, or none.
  //  Inclusion or exclusion list: Did the user tell us via a file to explicitly include or exclude this member?
  // 
  // Secondary overriding consideration: For a value type appearing in the reference assembly, all of its fields
  //   must also appear in the reference assembly so that managed C++ can get precise size information for the value 
  //   type.  This may mean including private members that would otherwise be excluded.
  enum KeepOptions { All, ExtVis, NonPrivate };
  enum SecurityKeepOptions { All, OnlyNonCritical, /* ExcludeMethodsWithStrictHPAs */ };

  internal class DeleteThings : MetadataRewriter {
    private KeepOptions WhatToKeep = KeepOptions.All;
    private SecurityKeepOptions SecurityWhatToKeep = SecurityKeepOptions.All;
    private bool KeepAttributes = true;
    private Dictionary<string, bool> ExemptAttributes = new Dictionary<string, bool>();
    private IMethodReference/*?*/ entryPoint = null;
    private bool entryPointKept;
    /// <summary>
    /// Behave just like the original AsmMeta
    /// </summary>
    private bool backwardCompat;

    /// <summary>
    /// Tables for keeping track of everything that has been whacked. Used for a second pass to fix up references
    /// to things that have been deleted.
    /// </summary>
    internal Dictionary<uint, bool> WhackedTypes = new Dictionary<uint, bool>();
    internal Dictionary<uint, bool> WhackedMethods = new Dictionary<uint, bool>();

    private AsmMetaHostEnvironment asmMetaHostEnvironment;

    /// <summary>
    /// Use this constructor when you don't want some things emitted into the output.
    /// For instance, if <paramref name="e"/> is <code>EmitOptions.ExtVis</code>
    /// then all methods, types, etc., that are not visible outside of the assembly are not emitted into the output.
    /// </summary>
    /// <param name="e">
    /// Indicates what to emit and what to leave out.
    /// For instance, if it is <code>EmitOptions.ExtVis</code>
    /// then all methods, types, etc., that are not visible outside of the assembly
    /// are not emitted into the output.
    /// </param>
    /// <param name="keepAttributes">
    /// Specify whether to keep custom attributes on types, methods, assembly, etc.
    /// </param>
    /// <param name="exemptAttributes">
    /// A list of attribute names that are exempt from the polarity of <paramref name="keepAttributes"/>.
    /// For instance, if <paramref name="keepAttributes"/> is true, then if an attribute is in the list, that
    /// means to not emit it. Conversely, if <paramref name="keepAttributes"/> is false, then if it is in the
    /// list, it means to emit it.
    /// </param>
    /// <param name="backwardCompatibility">
    /// When true, then this behaves just like the original AsmMeta. That means, among other things, that the argument values
    /// of <paramref name="e"/> and <paramref name="keepAttributes"/> are ignored and the values KeepOptions.ExtVis and
    /// false, respectively, are used instead.
    /// </param>
    public DeleteThings(AsmMetaHostEnvironment host, KeepOptions e, SecurityKeepOptions transparency, bool keepAttributes, string[] exemptAttributes, bool backwardCompatibility)
      : base(host) {
      this.asmMetaHostEnvironment = host;
      if (backwardCompatibility) {
        this.WhatToKeep = KeepOptions.ExtVis;
        this.KeepAttributes = false;
        this.backwardCompat = true;
        this.SecurityWhatToKeep = SecurityKeepOptions.All;
      } else {
        this.WhatToKeep = e;
        this.KeepAttributes = keepAttributes;
        this.SecurityWhatToKeep = transparency;
      }
      for (int i = 0, n = exemptAttributes == null ? 0 : exemptAttributes.Length; i < n; i++) {
        this.ExemptAttributes[exemptAttributes[i]] = true;
      }
    }

    static private bool IsFamilyOrIsFamilyORAssembly(ITypeDefinitionMember typeDefinitionMember) {
      return typeDefinitionMember.Visibility == TypeMemberVisibility.Family || typeDefinitionMember.Visibility == TypeMemberVisibility.FamilyOrAssembly;
    }
    static private bool IsPublic(ITypeDefinition typeDefinition) {
      INamespaceTypeDefinition namespaceTypeDefinition = typeDefinition as INamespaceTypeDefinition;
      if (namespaceTypeDefinition != null) return namespaceTypeDefinition.IsPublic;
      INestedTypeDefinition nestedTypeDefinition = typeDefinition as INestedTypeDefinition;
      if (nestedTypeDefinition != null) return nestedTypeDefinition.Visibility == TypeMemberVisibility.Public;
      return false;
    }

    #region ShouldWhack
    private bool ShouldWhack(ICustomAttribute a) {
      string name = TypeHelper.GetTypeName(a.Type);
      return this.KeepAttributes == this.ExemptAttributes.ContainsKey(name);
    }
    private bool ShouldWhack(ITypeDefinition typeDefinition) {
      if (SecurityWhatToKeep == SecurityKeepOptions.OnlyNonCritical) {
        if (IsSecurityCritical(typeDefinition))
          return true;
      }
      switch (this.WhatToKeep) {
        case KeepOptions.All:
          return false;
        case KeepOptions.ExtVis:
          if (typeDefinition is INamespaceTypeDefinition || typeDefinition is INestedTypeDefinition)
            return !TypeHelper.IsVisibleOutsideAssembly(typeDefinition);
          return false; // REVIEW: what is the right thing to do here?
        case KeepOptions.NonPrivate:
          INamespaceTypeDefinition namespaceTypeDefinition = typeDefinition as INamespaceTypeDefinition;
          if (namespaceTypeDefinition != null) return !namespaceTypeDefinition.IsPublic;
          INestedTypeDefinition nestedTypeDefinition = typeDefinition as INestedTypeDefinition;
          if (nestedTypeDefinition != null) return nestedTypeDefinition.Visibility == TypeMemberVisibility.Private;
          return false;
        default:
          return false;
      }
    }
    private bool ShouldWhack(ITypeDefinitionMember mem) {
      if (SecurityWhatToKeep == SecurityKeepOptions.OnlyNonCritical) {
        if (IsSecurityCritical(mem))
          return true;
      }
      return (
        ((this.WhatToKeep == KeepOptions.ExtVis) && !MemberHelper.IsVisibleOutsideAssembly(mem))
        ||
        (this.WhatToKeep == KeepOptions.NonPrivate && mem.Visibility == TypeMemberVisibility.Private)
        );
    }
    // Does NOT include methods marked with SecuritySafeCriticalAttribute.
    private bool IsSecurityCritical(ITypeDefinition type) {
      return AttributeHelper.Contains(type.Attributes, this.asmMetaHostEnvironment.PlatformType.SystemSecuritySecurityCriticalAttribute);
    }
    // Does NOT include methods marked with SecuritySafeCriticalAttribute.
    private bool IsSecurityCritical(ITypeDefinitionMember member) {
      return AttributeHelper.Contains(member.Attributes, this.asmMetaHostEnvironment.PlatformType.SystemSecuritySecurityCriticalAttribute);
    }
    #endregion ShouldWhack

    public override void RewriteChildren(Assembly assembly) {
      base.RewriteChildren(assembly);
      if (this.backwardCompat) {
        //modifiedAssembly.AssemblyReferences = new List<IAssemblyReference>();
        assembly.SecurityAttributes = new List<ISecurityAttribute>();
      }
      return;
    }

    public override void RewriteChildren(Module module) {
      if (module.EntryPoint is Dummy)
        this.entryPoint = module.EntryPoint;
      else
        this.entryPoint = null;

      base.RewriteChildren(module);

      if (this.entryPoint != null && !this.entryPointKept) module.EntryPoint = Dummy.MethodReference;
      if (this.backwardCompat) module.TrackDebugData = false; // not preserved by the original AsmMeta
      return;
    }

    public override List<INamespaceMember> Rewrite(List<INamespaceMember> namespaceMembers) {
      List<INamespaceMember> newList = new List<INamespaceMember>();
      foreach (var namespaceMember in namespaceMembers) {
        INamespaceTypeDefinition namespaceTypeDefinition = namespaceMember as INamespaceTypeDefinition;
        if (namespaceTypeDefinition != null) {
          if (this.ShouldWhack(namespaceTypeDefinition)) {
            if (!this.WhackedTypes.ContainsKey(namespaceTypeDefinition.InternedKey))
              this.WhackedTypes.Add(namespaceTypeDefinition.InternedKey, true);
            continue;
          }
          var mutableNamespaceTypeDefinition = (NamespaceTypeDefinition)this.Rewrite(namespaceTypeDefinition);
          if (this.backwardCompat)
            mutableNamespaceTypeDefinition.IsBeforeFieldInit = false;
          newList.Add(mutableNamespaceTypeDefinition);
        } else {
          newList.Add(base.Rewrite(namespaceMember));
        }
      }
      return newList;
    }

    public override List<INestedTypeDefinition> Rewrite(List<INestedTypeDefinition> nestedTypes) {
      List<INestedTypeDefinition> newList = new List<INestedTypeDefinition>();
      for (int i = 0, n = nestedTypes.Count; i < n; i++) {
        var nestedTypeDefinition = (NestedTypeDefinition)nestedTypes[i];
        if (nestedTypeDefinition != null && this.ShouldWhack((ITypeDefinition)nestedTypeDefinition)) {
          if (!this.WhackedTypes.ContainsKey(nestedTypeDefinition.InternedKey))
            this.WhackedTypes.Add(nestedTypeDefinition.InternedKey, true);
          continue;
        }
        this.Rewrite(nestedTypeDefinition);
        if (this.backwardCompat)
          nestedTypeDefinition.IsBeforeFieldInit = false;
        newList.Add(nestedTypeDefinition);
      }
      return newList;
    }

    public override List<ICustomAttribute> Rewrite(List<ICustomAttribute> customAttributes) {
      List<ICustomAttribute> newList = new List<ICustomAttribute>();

      foreach (CustomAttribute customAttribute in customAttributes) {
        bool keep = true;
        if (ShouldWhack(customAttribute)) {
          keep = false; // priority goes to KeepAttribute setting
        } else {
          if (this.WhatToKeep != KeepOptions.All) {
            if (ShouldWhack(customAttribute.Type.ResolvedType)) {
              keep = false;
            } else if (customAttribute.Arguments != null) {
              // need to make sure that if there are any arguments that are types, those
              // types are public. Otherwise whack the attribute
              foreach (IMetadataExpression argument in customAttribute.Arguments) {
                ITypeDefinition typeDefinition = argument.Type as ITypeDefinition;
                if (typeDefinition != null) {
                  IMetadataConstant ct = argument as IMetadataConstant;
                  if (ct != null) {
                    ITypeDefinition constantValue = ct as ITypeDefinition;
                    if (ShouldWhack(constantValue)) {
                      keep = false;
                      break; // only need to find one to decide to whack attribute
                    }
                  }
                }
              }
            }
          }
        }
        if (keep) {
          newList.Add(customAttribute);
        }
      }
      return newList;
    }

    public override List<IEventDefinition> Rewrite(List<IEventDefinition> events) {
      List<IEventDefinition> newList = new List<IEventDefinition>();
      foreach (EventDefinition eventDefinition in events) {
        if (!ShouldWhack(eventDefinition)) {
          newList.Add(this.Rewrite(eventDefinition));
        }
      }
      return newList;
    }

    public override List<IFieldDefinition> Rewrite(List<IFieldDefinition> fields) {
      List<IFieldDefinition> newList = new List<IFieldDefinition>();
      foreach (FieldDefinition fieldDefinition in fields) {
        if (!ShouldWhack(fieldDefinition)) {
          newList.Add(this.Rewrite(fieldDefinition));
        }
      }
      return newList;
    }

    public override List<IMethodDefinition> Rewrite(List<IMethodDefinition> methods) {
      List<IMethodDefinition> newList = new List<IMethodDefinition>();
      foreach (MethodDefinition methodDefinition in methods) {
        bool keep;
        #region Decide whether to keep method or not
        if (this.WhatToKeep != KeepOptions.All || this.SecurityWhatToKeep != SecurityKeepOptions.All) {
          if (false && this.entryPoint != null && this.entryPoint == methodDefinition) { // I just checked the original AsmMeta's behavior and it deletes private Main methods!
            keep = true; // need entry point even if it is not visible
            //} else if (method.DeclaringMember != null && method.DeclaringMember.IsVisibleOutsideAssembly) {
            //  keep = true; // believe it or not, one accessor might not be visible, but the other one might be!
          } else if (IsFamilyOrIsFamilyORAssembly(methodDefinition)
            && IsPublic(methodDefinition.ContainingTypeDefinition)
            && methodDefinition.ContainingTypeDefinition.IsSealed
            ) {
            keep = true; // compatibility with AsmMeta's rules...
          } else {
            keep = !ShouldWhack(methodDefinition);
          }
        } else {
          keep = true;
        }
        #endregion
        if (keep) { // still need to delete its body
          if (this.entryPoint == methodDefinition) this.entryPointKept = true;
          newList.Add(this.Rewrite(methodDefinition));
        } else {
          if (!this.WhackedMethods.ContainsKey(methodDefinition.InternedKey))
            this.WhackedMethods.Add(methodDefinition.InternedKey, true);
        }
      }
      return newList;
    }

    public override List<IPropertyDefinition> Rewrite(List<IPropertyDefinition> properties) {
      List<IPropertyDefinition> newList = new List<IPropertyDefinition>();
      foreach (PropertyDefinition propertyDefinition in properties) {
        if (!ShouldWhack(propertyDefinition)) {
          newList.Add(this.Rewrite(propertyDefinition));
        }
      }
      return newList;
    }
  }

  internal class FixUpReferences : MetadataRewriter {

    private Dictionary<uint, bool> whackedMethods;
    private Dictionary<uint, bool> whackedTypes;

    public FixUpReferences(IMetadataHost host, Dictionary<uint, bool> WhackedMethods, Dictionary<uint, bool> WhackedTypes)
      : base(host) {
      this.whackedMethods = WhackedMethods;
      this.whackedTypes = WhackedTypes;
    }

    //TODO: what about events?

    public override void RewriteChildren(PropertyDefinition propertyDefinition) {
      base.RewriteChildren(propertyDefinition);
      if (propertyDefinition.Accessors != null && 0 < propertyDefinition.Accessors.Count) {
        var accessors = new List<IMethodReference>(propertyDefinition.Accessors.Count);
        foreach (var methodReference in propertyDefinition.Accessors)
          if (!this.whackedMethods.ContainsKey(methodReference.InternedKey))
            accessors.Add(methodReference);
        propertyDefinition.Accessors = accessors;
      }
      //TODO: what about the getter and setter?
      return;
    }

    public override void RewriteChildren(NamespaceTypeDefinition namespaceTypeDefinition) {
      this.PruneInterfacesAndExplicitImplementationOverrides(namespaceTypeDefinition);
      base.RewriteChildren(namespaceTypeDefinition);
    }

    public override void RewriteChildren(NestedTypeDefinition nestedTypeDefinition) {
      this.PruneInterfacesAndExplicitImplementationOverrides(nestedTypeDefinition);
      base.RewriteChildren(nestedTypeDefinition);
    }

    private void PruneInterfacesAndExplicitImplementationOverrides(NamedTypeDefinition typeDefinition) {
      #region Prune the list of interfaces this type implements (if necessary)
      if (typeDefinition.Interfaces != null && 0 < typeDefinition.Interfaces.Count) {
        var newInterfaceList = new List<ITypeReference>();
        foreach (var iface in typeDefinition.Interfaces) {
          if (!this.whackedTypes.ContainsKey(iface.InternedKey))
            newInterfaceList.Add(iface);
        }
        typeDefinition.Interfaces = newInterfaceList;
      }
      #endregion Prune the list of interfaces this type implements (if necessary)
      #region Prune the list of explicit implementation overrides (as necessary)
      if (typeDefinition.ExplicitImplementationOverrides != null && 0 < typeDefinition.ExplicitImplementationOverrides.Count) {
        var newExplicitImplementationOverrides = new List<IMethodImplementation>();
        foreach (IMethodImplementation methodImpl in typeDefinition.ExplicitImplementationOverrides) {
          if (!this.whackedMethods.ContainsKey(methodImpl.ImplementingMethod.InternedKey)) {
            newExplicitImplementationOverrides.Add(methodImpl);
          }
        }
        typeDefinition.ExplicitImplementationOverrides = newExplicitImplementationOverrides;
      }
      #endregion Prune the list of explicit implementation overrides (as necessary)
    }
  }

  internal class RenameAssembly : MetadataRewriter {

    private RenameAssembly(IMetadataHost host)
      : base(host) {
    }

    private AssemblyIdentity originalAssemblyIdentity = null;
    private IAssemblyReference replacementAssemblyReference = null;

    public static IUnit ReparentAssemblyIdentity(IMetadataHost host, AssemblyIdentity targetAssemblyIdentity, AssemblyIdentity sourceAssemblyIdentity, IUnit unit) {
      Contract.Requires(targetAssemblyIdentity != null);
      Contract.Requires(sourceAssemblyIdentity != null);
      var rar = new RenameAssembly(host);
      rar.originalAssemblyIdentity = targetAssemblyIdentity;
      rar.replacementAssemblyReference = new Microsoft.Cci.Immutable.AssemblyReference(host, sourceAssemblyIdentity);
      return rar.Rewrite(unit);
    }

    /// <summary>
    /// The object model does not guarantee that the assembly references are shared, so
    /// since the assembly (which itself can be a reference) is being updated, this method is
    /// needed in order to guarantee that all references see the update.
    /// </summary>
    public override IAssemblyReference Rewrite(IAssemblyReference assemblyReference) {
      return (assemblyReference.AssemblyIdentity.Equals(originalAssemblyIdentity))
        ?
        replacementAssemblyReference
        :
        base.Rewrite(assemblyReference);
    }
  }

}
