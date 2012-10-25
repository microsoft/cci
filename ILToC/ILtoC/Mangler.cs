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
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Microsoft.Cci;
using Microsoft.Cci.Analysis;
using Microsoft.Cci.UtilityDataStructures;
using System.Text;

namespace ILtoC {
  partial class Mangler : MetadataVisitor {

    int hash = 5381;

    internal string Mangle(IFieldReference field) {
      Contract.Requires(field != null);

      field.ResolvedField.Dispatch(this); //compute the hash
      var sb = new StringBuilder();
      sb.Append('_');
      sb.Append((uint)this.hash);
      sb.Append('_');
      this.AppendSanitizedName(sb, field.Name.Value);
      return sb.ToString();
    }

    internal string Mangle(IMethodReference method) {
      Contract.Requires(method != null);

      method.ResolvedMethod.Dispatch(this); //compute the hash
      var sb = new StringBuilder();
      sb.Append('_');
      sb.Append((uint)this.hash);
      sb.Append('_');
      this.AppendSanitizedName(sb, TypeHelper.GetTypeName(method.ContainingType));
      sb.Append('_');
      this.AppendSanitizedName(sb, method.Name.Value);
      foreach (var par in method.Parameters) {
        sb.Append('_');
        this.AppendSanitizedName(sb, TypeHelper.GetTypeName(par.Type, NameFormattingOptions.OmitContainingType));
      }
      return sb.ToString();
    }

    internal string Mangle(IModule module) {
      Contract.Requires(module != null);

      module.Dispatch(this);
      var sb = new StringBuilder();
      sb.Append('_');
      sb.Append((uint)this.hash);
      sb.Append('_');
      this.AppendSanitizedName(sb, module.Name.Value);
      return sb.ToString();
    }

    internal string Mangle(string value) {
      Contract.Requires(value != null);

      this.Visit(value); //compute the hash
      return "mangled_string_"+(uint)this.hash;
    }

    internal string Mangle(ITypeReference type) {
      Contract.Requires(type != null);

      type.ResolvedType.Dispatch(this); //compute the hash
      var sb = new StringBuilder();
      sb.Append('_');
      sb.Append((uint)this.hash);
      sb.Append('_');
      this.AppendSanitizedName(sb, TypeHelper.GetTypeName(type));
      return sb.ToString();
    }

    private void AppendSanitizedName(StringBuilder sb, string name) {
      Contract.Requires(sb != null);
      Contract.Requires(name != null);

      if (name.Length < 1) return;
      var firstChar = name[0];
      if ('0' <= firstChar && firstChar <= '9') sb.Append('_');
      foreach (var ch in name) {
        if (('a' <= ch && ch <= 'z') || ('0' <= ch && ch <= '9') || ('A' <= ch && ch <= 'Z') || ch == '_')
          sb.Append(ch);
        else if (char.IsLetterOrDigit(ch))
          sb.Append(ch);
        else
          sb.Append('_');
      }
    }

    public override void Visit(IArrayTypeReference arrayTypeReference) {
      arrayTypeReference.ElementType.Dispatch(this);
      int h = this.hash;
      h = (h << 5 + h) ^ ((int)arrayTypeReference.Rank+4);
      this.hash = h;
    }

    public override void Visit(IAssembly assembly) {
      this.Visit(assembly.Name.Value);
      int h = this.hash;
      if (assembly.Version.Major != 0)
        h = (h << 5 + h) ^ (int)assembly.Version.Major;
      if (assembly.Version.Minor != 0)
        h = (h << 5 + h) ^ (int)assembly.Version.Minor;
      if (assembly.Version.Revision != 0)
        h = (h << 5 + h) ^ (int)assembly.Version.Revision;
      if (assembly.Version.Build != 0)
        h = (h << 5 + h) ^ (int)assembly.Version.Build;
      foreach (var b in assembly.PublicKeyToken)
        h = (h << 5 + h) ^ b;
      this.Visit(assembly.Culture);
      this.hash = h;
    }

    public override void Visit(ICustomModifier customModifier) {
      customModifier.Modifier.Dispatch(this);
      if (customModifier.IsOptional) {
        int h = this.hash;
        h = (h << 5 + h) ^ 1;
        this.hash = h;
      }
    }

    private void Visit(IEnumerable<ICustomModifier> customModifiers) {
      Contract.Requires(customModifiers != null);
      foreach (var customModifier in customModifiers) {
        Contract.Assume(customModifier != null);
        this.Visit(customModifier);
      }
    }

    public override void Visit(IFieldDefinition field) {
      field.ContainingTypeDefinition.Dispatch(this);
      field.Type.Dispatch(this);
      this.Visit(field.Name.Value);
      if (field.IsModified) this.Visit(field.CustomModifiers);
    }

    public override void Visit(IFunctionPointerTypeReference functionPointerTypeReference) {
      this.Visit((ISignature)functionPointerTypeReference);
      foreach (var extraArgument in functionPointerTypeReference.ExtraArgumentTypes) {
        Contract.Assume(extraArgument != null);
        this.Visit(extraArgument);
      }
    }

    public override void Visit(IGenericMethodInstanceReference genericMethodInstanceReference) {
      genericMethodInstanceReference.GenericMethod.ResolvedMethod.Dispatch(this);
      foreach (var genArg in genericMethodInstanceReference.GenericArguments) {
        Contract.Assume(genArg != null);
        genArg.ResolvedType.Dispatch(this);
      }
    }

    public override void Visit(IGenericTypeInstanceReference genericTypeInstanceReference) {
      genericTypeInstanceReference.GenericType.ResolvedType.Dispatch(this);
      foreach (var genArg in genericTypeInstanceReference.GenericArguments) {
        Contract.Assume(genArg != null);
        genArg.ResolvedType.Dispatch(this);
      }
    }

    public override void Visit(IManagedPointerTypeReference managedPointerTypeReference) {
      managedPointerTypeReference.TargetType.ResolvedType.Dispatch(this);
      int h = this.hash;
      h = (h << 5 + h) ^ 1;
      this.hash = h;
    }

    public override void Visit(IMethodDefinition method) {
      method.ContainingTypeDefinition.Dispatch(this);
      this.Visit(method.Name.Value);
      this.Visit((ISignature)method);
    }

    public override void Visit(IModifiedTypeReference modifiedTypeReference) {
      modifiedTypeReference.UnmodifiedType.ResolvedType.Dispatch(this);
      this.Visit(modifiedTypeReference.CustomModifiers);
    }

    public override void Visit(IModule module) {
      this.Visit(module.ModuleName.Value);
    }

    public override void Visit(INamespaceTypeDefinition namespaceTypeDefinition) {
      namespaceTypeDefinition.ContainingUnitNamespace.Dispatch(this);
      this.Visit(namespaceTypeDefinition.Name.Value);
      if (namespaceTypeDefinition.GenericParameterCount > 0) {
        int h = this.hash;
        h = (h << 5 + h) ^ (int)namespaceTypeDefinition.GenericParameterCount;
        this.hash = h;
      }
    }

    public override void Visit(INestedTypeDefinition nestedTypeDefinition) {
      nestedTypeDefinition.ContainingTypeDefinition.Dispatch(this);
      this.Visit(nestedTypeDefinition.Name.Value);
      if (nestedTypeDefinition.GenericParameterCount > 0) {
        int h = this.hash;
        h = (h << 5 + h) ^ (int)nestedTypeDefinition.GenericParameterCount;
        this.hash = h;
      }
    }

    public override void Visit(INestedUnitNamespace nestedUnitNamespace) {
      nestedUnitNamespace.ContainingUnitNamespace.Dispatch(this);
      this.Visit(nestedUnitNamespace.Name.Value);
    }

    public override void Visit(IParameterTypeInformation parameterTypeInformation) {
      parameterTypeInformation.Type.ResolvedType.Dispatch(this);
      if (parameterTypeInformation.IsModified) this.Visit(parameterTypeInformation.CustomModifiers);
      if (parameterTypeInformation.IsByReference) {
        int h = this.hash;
        h = (h << 5 + h) ^ 2;
        this.hash = h;
      }
    }

    public override void Visit(IPointerTypeReference pointerTypeReference) {
      pointerTypeReference.TargetType.ResolvedType.Dispatch(this);
      int h = this.hash;
      h = (h << 5 + h) ^ 3;
      this.hash = h;
    }

    public override void Visit(IRootUnitNamespace rootUnitNamespace) {
      rootUnitNamespace.Unit.Dispatch(this);
    }

    private void Visit(ISignature signature) {
      Contract.Requires(signature != null);

      signature.Type.ResolvedType.Dispatch(this);
      if (signature.ReturnValueIsModified) this.Visit(signature.ReturnValueCustomModifiers);
      foreach (var par in signature.Parameters) {
        Contract.Assume(par != null);
        this.Visit(par);
      }
      int h = this.hash;
      h = (h << 5 + h) ^ (int)signature.CallingConvention;
      if (signature.ReturnValueIsByRef) h = (h << 5 + h) ^ 2;
      this.hash = h;
    }

    private void Visit(string str) {
      Contract.Requires(str != null);
      unsafe {
        fixed (char* src = str) {
          int h = this.hash;
          char* s = src;
          int c;
          while ((c = *s++) != 0)
            h = ((h << 5) + h) ^ c;
          this.hash = h;
        }
      }
    }

    public override void Visit(ITypeMemberReference typeMember) {
      typeMember.ResolvedTypeDefinitionMember.Dispatch(this);
    }

    public override void Visit(ITypeReference typeReference) {
      typeReference.ResolvedType.Dispatch(this);
    }

    public override void Visit(IUnitNamespaceReference unitNamespaceReference) {
      unitNamespaceReference.ResolvedUnitNamespace.Dispatch(this);
    }

    public override void Visit(IUnitReference unitReference) {
      unitReference.ResolvedUnit.Dispatch(this);
    }

  }
}
