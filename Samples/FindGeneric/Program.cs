//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.IO;
using Microsoft.Cci;

namespace FindGeneric
{
  class Program
  {
    #region snippet FindGeneric-Program-Main
    static int Main(string[] args)
    {
      #region remove
      if (args.Length < 1)
      {
        Console.WriteLine("Must specify at least one input file.");
        return 1;
      }
      #endregion
      #region snippet FindGeneric-Program-Main-HostEnvironment
      HostEnvironment host = new HostEnvironment();
      #endregion

      foreach (string assemblyName in args)
      {
        #region snippet FindGeneric-Program-Main-LoadUnitFrom
        IAssembly/*?*/ assembly = host.LoadUnitFrom(assemblyName)
          as IAssembly;
        #endregion
        if (assembly == null || assembly == Dummy.Assembly)
        {
          #region remove
          Console.WriteLine("The file '" + assemblyName + "' is not a PE file" +
            " containing a CLR assembly, or an error occurred when loading it.");
          #endregion
          continue;
        }
        else
        {
          Console.WriteLine("Generic Methods in generic types from '"
            + assembly.Name.Value + "':");
          #region remove
          Console.WriteLine("=====================================");
          #endregion
        }

        #region snippet FindGeneric-Program-Main-ForLoop
        foreach (INamedTypeDefinition type in assembly.GetAllTypes())
        {
          if (type.IsGeneric)
          {
            foreach (IMethodDefinition methodDefinition in type.Methods)
            {
              if (methodDefinition.IsGeneric)
              {
                Console.WriteLine(MemberHelper.GetMemberSignature(
                  methodDefinition,
                  NameFormattingOptions.Signature |
                  NameFormattingOptions.TypeParameters |
                  NameFormattingOptions.TypeConstraints |
                  NameFormattingOptions.ParameterName
                  ));
              }
            }
          }
        }
        #endregion
      }
      return 0;
    }
    #endregion
  }

  #region snippet FindGeneric-HostEnvironment
  internal class HostEnvironment : MetadataReaderHost
  {
    PeReader peReader;
    internal HostEnvironment()
      : base(new NameTable(), 4)
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
  #endregion
}
