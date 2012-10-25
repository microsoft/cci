// ==++==
// 
//   
//    Copyright (c) 2012 Microsoft Corporation.  All rights reserved.
//   
//    The use and distribution terms for this software are contained in the file
//    named license.txt, which can be found in the root of this distribution.
//    By using this software in any fashion, you are agreeing to be bound by the
//    terms of this license.
//   
//    You must not remove this notice, or any other, from this software.
//   
// 
// ==--==
using Microsoft.Cci;
using System.Diagnostics.Contracts;

namespace ILtoC {
  partial class Translator {

    private void EmitMain() {
      if (this.module.EntryPoint is Dummy) return;
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("int main(int argc, char *argv[])");
      this.sourceEmitter.EmitMethodBodyOpeningDelimiter("{");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("uintptr_t arguments_array;");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("int result = 0;");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("uintptr_t ret;");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("allocateStatics();");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString(this.TypeLoaderName);
      this.sourceEmitter.EmitString("();");
      this.sourceEmitter.EmitNewLine();

      var argArrayName = this.EmitMainArguments();
      this.sourceEmitter.EmitString("ret = ");
      this.sourceEmitter.EmitString(this.GetMangledMethodName(this.module.EntryPoint));
      this.sourceEmitter.EmitString("(");
      if (argArrayName != null) {
        this.sourceEmitter.EmitString(argArrayName);
        if (this.module.EntryPoint.Type.TypeCode != PrimitiveTypeCode.Void)
          this.sourceEmitter.EmitString(", (uintptr_t)&result");
      } else {
        if (this.module.EntryPoint.Type.TypeCode != PrimitiveTypeCode.Void)
          this.sourceEmitter.EmitString("(uintptr_t)&result");
      }
      this.sourceEmitter.EmitString(");");
      this.sourceEmitter.EmitNewLine();

      this.sourceEmitter.EmitString("if ((void *) ret != NULL)");
      this.sourceEmitter.EmitBlockOpeningDelimiter("{");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("exit(123456);");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitBlockClosingDelimiter("}");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("return result;");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitBlockClosingDelimiter("}");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitNewLine();
    }

    private string/*?*/ EmitMainArguments() {
      foreach (var par in this.module.EntryPoint.Parameters) {
        var arrayType = par.Type as IArrayTypeReference;
        if (arrayType == null || arrayType.ElementType.TypeCode != PrimitiveTypeCode.String) break;
        this.sourceEmitter.EmitString("arguments_array = ");
        var systemArray = this.GetMangledTypeName(this.host.PlatformType.SystemArray);
        this.sourceEmitter.EmitString("(uintptr_t)calloc(1, sizeof(struct ");
        this.sourceEmitter.EmitString(systemArray);
        this.sourceEmitter.EmitString(") + sizeof(");
        this.EmitTypeReference(arrayType.ElementType, storageLocation: true);
        this.sourceEmitter.EmitString(") * argc);");
        this.sourceEmitter.EmitNewLine();
        this.EmitAdjustPointerToDataFromHeader("arguments_array");
        this.sourceEmitter.EmitString("InitializeArrayHeader(arguments_array, argc, ");
        this.EmitTypeObjectReference(arrayType);
        this.sourceEmitter.EmitString(", ");
        var elemType = arrayType.ElementType;
        this.EmitTypeObjectReference(elemType);
        this.sourceEmitter.EmitString(");");
        this.sourceEmitter.EmitNewLine();
        //TODO: add strings to array
        return "arguments_array";
      }
      return null;
    }

  }
}
