//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.IO;
using Microsoft.Cci;

namespace FindThrowPlatformType
{
  class Program
  {
    static void Main(string[] args)
    {
      if (args == null || args.Length == 0)
      {
        Console.WriteLine("usage: FindThrowPlatformType [path]fileName.ext");
        return;
      }

      HostEnvironment host = new HostEnvironment();
      IModule/*?*/ assembly = host.LoadUnitFrom(args[0]) as IModule;

      if (assembly == null || assembly == Dummy.Module || assembly == Dummy.Assembly)
      {
        Console.WriteLine(args[0] + " is not a PE file containing a CLR module or assembly.");
        return;
      }

      MyPlatformType platformType = new MyPlatformType(host);
      // create a reference to System.ArgumentNullException
      INamespaceTypeReference systemArgumentNullException = platformType.SystemArgumentNullException;

      foreach (INamedTypeDefinition type in assembly.GetAllTypes())
      {
        foreach (IMethodDefinition methodDefinition in type.Methods)
        {
          foreach (IOperation operation in methodDefinition.Body.Operations)
          {
            // before a System.ArgumentNullException is thrown, 
            // an object of that type has to be constructed with newobj instruction
            if (operation.OperationCode == OperationCode.Newobj)
            {
              // identify a call to System.ArgumentNullException's constructor
              IMethodReference consRef = operation.Value as IMethodReference;
              if (consRef != null && consRef.Name.Value.Equals(".ctor") &&
                  consRef.ContainingType.InternedKey == systemArgumentNullException.InternedKey)
              {
                Console.WriteLine(methodDefinition);
              }
            }
          }
        }
      }
    }
  }

  internal class MyPlatformType : PlatformType
  {
    public MyPlatformType(IMetadataHost host)
      : base(host)
    {
    }

    public INamespaceTypeReference SystemArgumentNullException
    {
      get
      {
        if (this.systemArgumentNullException == null)
        {
          this.systemArgumentNullException = this.CreateReference(
            this.CoreAssemblyRef, "System", "ArgumentNullException");
        }
        return this.systemArgumentNullException;
      }
    }
    INamespaceTypeReference systemArgumentNullException;
  }

  internal class HostEnvironment : MetadataReaderHost
  {
    PeReader peReader;

    internal HostEnvironment()
    {
      this.peReader = new PeReader(this);
    }
    
    public override IUnit LoadUnitFrom(string location)
    {
      IUnit result = this.peReader.OpenModule(
        BinaryDocument.GetBinaryDocumentForFile(location, this));
      this.RegisterAsLatest(result);
      return result;
    }
  }
}
