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
using Microsoft.Cci.Immutable;

namespace FindThrowPlatformType {
  class Program {
    static void Main(string[] args) {
      if (args == null || args.Length == 0) {
        Console.WriteLine("usage: FindThrowArgumentNull [path]fileName.ext");
        return;
      }

      using (var host = new PeReader.DefaultHost()) {
        var module = host.LoadUnitFrom(args[0]) as IModule;

        if (module == null) {
          Console.WriteLine(args[0] + " is not a PE file containing a CLR module or assembly.");
          return;
        }

        var platformType = new MyPlatformType(host);
        INamespaceTypeReference systemArgumentNullException = platformType.SystemArgumentNullException;
        IName ctor = host.NameTable.Ctor;

        //write out the signature of every method that contains the IL equivalent of "throw new System.ArgumentNullException();"
        foreach (var type in module.GetAllTypes()) {
          foreach (var methodDefinition in type.Methods) {
            var lastInstructionWasNewObjSystemArgumentNull = false;
            foreach (var operation in methodDefinition.Body.Operations) {
              if (operation.OperationCode == OperationCode.Newobj) {
                var consRef = operation.Value as IMethodReference;
                if (consRef != null && consRef.Name == ctor &&
                  TypeHelper.TypesAreEquivalent(consRef.ContainingType, systemArgumentNullException)) {
                  lastInstructionWasNewObjSystemArgumentNull = true;
                }
              } else if (lastInstructionWasNewObjSystemArgumentNull && operation.OperationCode == OperationCode.Throw) {
                Console.WriteLine(MemberHelper.GetMethodSignature(methodDefinition,
                  NameFormattingOptions.ReturnType|NameFormattingOptions.TypeParameters|NameFormattingOptions.Signature));
                break;
              } else {
                lastInstructionWasNewObjSystemArgumentNull = false;
              }
            }
          }
        }
      }
    }
  }

  /// <summary>
  /// A collection of references to types from the core platform, such as System.Object and System.SystemArgumentNullException.
  /// </summary>
  internal class MyPlatformType : PlatformType {

    /// <summary>
    /// Allocates an object that is a collection of references to types from the core platform, such as System.Object and System.SystemArgumentNullException.
    /// </summary>
    /// <param name="host">
    /// An object that provides a standard abstraction over the applications that host components that provide or consume objects from the metadata model.
    /// </param>
    internal MyPlatformType(IMetadataHost host)
      : base(host) {
    }

    /// <summary>
    /// A reference to the System.ArgumentNullException from the core system assembly.
    /// </summary>
    internal INamespaceTypeReference SystemArgumentNullException {
      get {
        if (this.systemArgumentNullException == null) {
          this.systemArgumentNullException = this.CreateReference(
            this.CoreAssemblyRef, "System", "ArgumentNullException");
        }
        return this.systemArgumentNullException;
      }
    }
    INamespaceTypeReference systemArgumentNullException;
  }

}
