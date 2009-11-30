//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Cci;

namespace CSharpSourceEmitter {
  public partial class SourceEmitter : BaseCodeTraverser, ICSharpSourceEmitter {
    public virtual void PrintTypeDefinition(ITypeDefinition typeDefinition) {
      if (typeDefinition.IsDelegate) {
        PrintDelegateDefinition(typeDefinition);
        return;
      }

      if (((INamedEntity)typeDefinition).Name.Value.Contains("PrivateImplementationDetails")) return;

      PrintTypeDefinitionAttributes(typeDefinition);
      PrintToken(CSharpToken.Indent);
      PrintTypeDefinitionVisibility(typeDefinition);
      PrintTypeDefinitionModifiers(typeDefinition);
      PrintTypeDefinitionKeywordType(typeDefinition);
      PrintTypeDefinitionName(typeDefinition);
      if (typeDefinition.IsGeneric) {
        this.Visit(typeDefinition.GenericParameters);
      }
      PrintTypeDefinitionBaseTypesAndInterfaces(typeDefinition);

      PrintToken(CSharpToken.NewLine);
      PrintTypeDefinitionLeftCurly(typeDefinition);

      var methods = new List<IMethodDefinition>(typeDefinition.Methods);
      var events = new List<IEventDefinition>(typeDefinition.Events);
      var properties = new List<IPropertyDefinition>(typeDefinition.Properties);
      var fields = new List<IFieldDefinition>(typeDefinition.Fields);
      var nestedTypes = new List<INestedTypeDefinition>(typeDefinition.NestedTypes);

      int methodsCount = 0;
      foreach (var method in methods) {
        if (method.IsConstructor && method.ParameterCount == 0) continue;
        if (method.IsStaticConstructor) continue;
        if (method.IsSpecialName && !method.IsConstructor) continue;
        methodsCount++;
      }

      int nestedTypesCount = 0;
      foreach (var nestedType in nestedTypes) {
        if (AttributeHelper.Contains(nestedType.Attributes, nestedType.PlatformType.SystemRuntimeCompilerServicesCompilerGeneratedAttribute)) continue;
        nestedTypesCount++;
      }

      // TODO: Method order can be important too - eg. for COMImport types
      Comparison<IMethodDefinition> mcomparison = (x, y) => string.Compare(x.Name.Value, y.Name.Value);
      methods.Sort(mcomparison);
      Visit(methods);
      if (methodsCount > 0 && (events.Count + properties.Count + fields.Count + nestedTypesCount) > 0) sourceEmitterOutput.WriteLine("");

      Comparison<IEventDefinition> ecomparison = (x, y) => string.Compare(x.Name.Value, y.Name.Value);
      events.Sort(ecomparison);
      Visit(events);
      if (events.Count > 0 && (properties.Count + fields.Count + nestedTypesCount) > 0) sourceEmitterOutput.WriteLine("");

      Comparison<IPropertyDefinition> pcomparison = (x, y) => string.Compare(x.Name.Value, y.Name.Value);
      properties.Sort(pcomparison);
      Visit(properties);
      if (properties.Count > 0 && (fields.Count+nestedTypesCount) > 0) sourceEmitterOutput.WriteLine("");

      // Order of fields can be important (eg. in sequential layout structs, and nice even just for enums, etc.)
      // So use existing order
      Visit(fields);
      if (fields.Count > 0 && nestedTypesCount > 0) sourceEmitterOutput.WriteLine("");

      Comparison<INestedTypeDefinition> tcomparison = (x, y) =>
        x.Name.UniqueKey == y.Name.UniqueKey ? x.GenericParameterCount - y.GenericParameterCount : string.Compare(x.Name.Value, y.Name.Value);
      nestedTypes.Sort(tcomparison);
      Visit(nestedTypes);

      PrintTypeDefinitionRightCurly(typeDefinition);
    }

    public virtual void PrintDelegateDefinition(ITypeDefinition delegateDefinition) {
      PrintTypeDefinitionAttributes(delegateDefinition);
      PrintToken(CSharpToken.Indent);

      IMethodDefinition invokeMethod = null;
      foreach (var invokeMember in delegateDefinition.GetMatchingMembers(m => m.Name.Value == "Invoke")) {
        IMethodDefinition idef = invokeMember as IMethodDefinition;
        if (idef != null) { invokeMethod = idef; break; }
      }

      PrintTypeDefinitionVisibility(delegateDefinition);
      if (IsMethodUnsafe(invokeMethod))
        PrintKeywordUnsafe();

      PrintKeywordDelegate();
      PrintMethodDefinitionReturnType(invokeMethod);
      PrintToken(CSharpToken.Space);
      PrintTypeDefinitionName(delegateDefinition);
      if (delegateDefinition.GenericParameterCount > 0) {
        this.Visit(delegateDefinition.GenericParameters);
      }
      Visit(invokeMethod.Parameters);

      PrintToken(CSharpToken.Semicolon);
      PrintToken(CSharpToken.NewLine);
    }

    public virtual void PrintTypeDefinitionAttributes(ITypeDefinition typeDefinition) {

      foreach (var attribute in typeDefinition.Attributes) {
        // Skip DefaultMemeberAttribute on a class that has an indexer
        var at = Utils.GetAttributeType(attribute);
        if (at == SpecialAttribute.DefaultMemberAttribute &&
          IteratorHelper.Any(typeDefinition.Properties, p => IteratorHelper.EnumerableIsNotEmpty(p.Parameters)))
          continue;
        // Skip ExtensionAttribute
        if (at == SpecialAttribute.Extension)
          continue;

        PrintAttribute(attribute, true, null);
      }

      if (typeDefinition.Layout != LayoutKind.Auto) {
        sourceEmitterOutput.WriteLine(String.Format("[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.{0})]",
          typeDefinition.Layout.ToString()),
          true);
      }
    }

    public virtual void PrintTypeDefinitionVisibility(ITypeDefinition typeDefinition) {
      if (typeDefinition is INamespaceTypeDefinition) {
        INamespaceTypeDefinition namespaceTypeDefinition = typeDefinition as INamespaceTypeDefinition;
        if (namespaceTypeDefinition.IsPublic)
          PrintKeywordPublic();
      } else if (typeDefinition is INestedTypeDefinition) {
        INestedTypeDefinition nestedTypeDefinition = typeDefinition as INestedTypeDefinition;
        PrintTypeMemberVisibility(nestedTypeDefinition.Visibility);
      }
    }

    public virtual void PrintTypeDefinitionModifiers(ITypeDefinition typeDefinition) {
      // If it's abstract and sealed and has no ctors, then consider it to be a static class
      // Perhaps we should check for the presense of CompilerServices.ExtensionAttribute instead
      if (typeDefinition.IsStatic) {
        PrintKeywordStatic();
      } else {
        if (typeDefinition.IsAbstract && !typeDefinition.IsInterface)
          PrintKeywordAbstract();

        if (typeDefinition.IsSealed && !typeDefinition.IsValueType)
          PrintKeywordSealed();
      }
    }

    public virtual void PrintTypeDefinitionKeywordType(ITypeDefinition typeDefinition) {
      if (typeDefinition.IsInterface)
        PrintKeywordInterface();
      else if (typeDefinition.IsEnum)
        PrintKeywordEnum();
      else if (typeDefinition.IsValueType)
        PrintKeywordStruct();
      else
        PrintKeywordClass();
    }

    public virtual void PrintKeywordClass() {
      PrintToken(CSharpToken.Class);
    }

    public virtual void PrintKeywordInterface() {
      PrintToken(CSharpToken.Interface);
    }

    public virtual void PrintKeywordStruct() {
      PrintToken(CSharpToken.Struct);
    }

    public virtual void PrintKeywordEnum() {
      PrintToken(CSharpToken.Enum);
    }

    public virtual void PrintKeywordDelegate() {
      PrintToken(CSharpToken.Delegate);
    }

    public virtual void PrintTypeDefinitionName(ITypeDefinition typeDefinition) {
      INamespaceTypeDefinition namespaceTypeDefinition = typeDefinition as INamespaceTypeDefinition;
      if (namespaceTypeDefinition != null) {
        PrintIdentifier(namespaceTypeDefinition.Name);
        return;
      }

      INestedTypeDefinition nestedTypeDefinition = typeDefinition as INestedTypeDefinition;
      if (nestedTypeDefinition != null) {
        PrintIdentifier(nestedTypeDefinition.Name);
        return;
      }

      INamedEntity namedEntity = typeDefinition as INamedEntity;
      if (namedEntity != null) {
        PrintIdentifier(namedEntity.Name);
      } else {
        sourceEmitterOutput.Write(typeDefinition.ToString());
      }
    }

    public virtual void PrintTypeDefinitionBaseTypesAndInterfaces(ITypeDefinition typeDefinition) {
      PrintBaseTypesAndInterfacesList(typeDefinition);
    }

    public virtual void PrintTypeDefinitionLeftCurly(ITypeDefinition typeDefinition) {
      PrintToken(CSharpToken.LeftCurly);
    }

    public virtual void PrintTypeDefinitionRightCurly(ITypeDefinition typeDefinition) {
      PrintToken(CSharpToken.RightCurly);
      PrintToken(CSharpToken.NewLine);
    }

  }
}
