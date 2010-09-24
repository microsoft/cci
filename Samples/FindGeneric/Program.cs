//-----------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.  All Rights Reserved.
// This code is licensed under the Microsoft Public License.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//-----------------------------------------------------------------------------
using System;
using Microsoft.Cci;

namespace FindGeneric {
  class Program {
    static void Main(string[] args) {
      if (args.Length < 1) {
        Console.WriteLine("Must specify at least one input file.");
        return;
      }
      using (var host = new PeReader.DefaultHost()) {
        foreach (string assemblyName in args) {
          var assembly = host.LoadUnitFrom(assemblyName) as IAssembly;
          if (assembly == null || assembly == Dummy.Assembly) {
            Console.WriteLine("The file '" + assemblyName + "' is not a PE file" +
            " containing a CLR assembly, or an error occurred when loading it.");
            continue;
          }

          Console.WriteLine("Generic Methods in generic types from '"+assembly.Name.Value+"':");
          foreach (INamedTypeDefinition type in assembly.GetAllTypes()) {
            if (!type.IsGeneric) continue;
            foreach (IMethodDefinition methodDefinition in type.Methods) {
              if (methodDefinition.IsGeneric) {
                Console.WriteLine(MemberHelper.GetMemberSignature(methodDefinition,
                  NameFormattingOptions.Signature | NameFormattingOptions.TypeParameters | NameFormattingOptions.TypeConstraints | NameFormattingOptions.ParameterName));
              }
            }
          }
        }
      }
    }
  }
}
