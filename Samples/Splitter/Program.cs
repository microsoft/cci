//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Cci;
using Microsoft.Cci.MutableCodeModel;

namespace Splitter {
  class Program {
    static void Main (string[] args) {
      if (args == null || args.Length == 0) {
        Console.WriteLine("usage: splitter [path]fileName.ext");
        return;
      }
      HostEnvironment host = new HostEnvironment();
      IModule/*?*/ module = host.LoadUnitFrom(args[0]) as IModule;
      if (module == null || module == Dummy.Module || module == Dummy.Assembly) {
        Console.WriteLine(args[0] + " is not a PE file containing a CLR module or assembly, or an error occurred when loading it.");
        return;
      }
      MetadataMutator mutator = new DeleteAttributes(host);
      IAssembly/*?*/ assembly = module as IAssembly;
      if (assembly != null)
        module = mutator.Visit(mutator.GetMutableCopy(assembly));
      else
        module = mutator.Visit(mutator.GetMutableCopy(module));
      PeWriter.WritePeToStream(module, host, File.Create(args[0] + ".pe"));
    }
  }

  internal class HostEnvironment : MetadataReaderHost {
    PeReader peReader;
    internal HostEnvironment () {
      this.peReader = new PeReader(this);
    }

    public override IUnit LoadUnitFrom (string location) {
      IUnit result = this.peReader.OpenModule(BinaryDocument.GetBinaryDocumentForFile(location, this));
      this.RegisterAsLatest(result);
      return result;
    }
  }

  public class DeleteAttributes : MetadataMutator {
    public DeleteAttributes (IMetadataHost host) : base(host) { }
    public override System.Collections.Generic.List<INamespaceMember> Visit (System.Collections.Generic.List<INamespaceMember> namespaceMembers) {
      if (this.stopTraversal) return namespaceMembers;
      List<INamespaceMember> newList = new List<INamespaceMember>();
      for (int i = 0, n = namespaceMembers.Count; i < n; i++) {
        ITypeDefinition td = namespaceMembers[i] as ITypeDefinition;
        if (td != null && AttributeHelper.IsAttributeType(td)) continue;
        INamespaceMember mem = this.Visit(namespaceMembers[i]);
        newList.Add(mem);
      }
      return newList;
    }
    public override System.Collections.Generic.List<INestedTypeDefinition> Visit (System.Collections.Generic.List<INestedTypeDefinition> nestedTypeDefinitions) {
      if (this.stopTraversal) return nestedTypeDefinitions;
      List<INestedTypeDefinition> newList = new List<INestedTypeDefinition>();
      for (int i = 0, n = nestedTypeDefinitions.Count; i < n; i++) {
        ITypeDefinition td = nestedTypeDefinitions[i] as ITypeDefinition;
        if (td != null && AttributeHelper.IsAttributeType(td)) continue;
        INestedTypeDefinition ntd = this.Visit(this.GetMutableCopy(nestedTypeDefinitions[i]));
        newList.Add(ntd);
      }
      return newList;
    }
    public override List<ICustomAttribute> Visit (List<ICustomAttribute> customAttributes) {
      if (this.stopTraversal) return customAttributes;
      List<ICustomAttribute> newList = new List<ICustomAttribute>();
      return newList;
    }
  }

}
