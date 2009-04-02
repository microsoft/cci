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

      var fields = new List<IFieldDefinition>(typeDefinition.Fields);
      var properties = new List<IPropertyDefinition>(typeDefinition.Properties);
      var methods = new List<IMethodDefinition>(typeDefinition.Methods);
      var events = new List<IEventDefinition>(typeDefinition.Events);
      var nestedTypes = new List<INestedTypeDefinition>(typeDefinition.NestedTypes);

      int methodsCount = 0;
      foreach (var method in methods) {
        if (method.IsConstructor && method.ParameterCount == 0) continue;
        if (method.IsStaticConstructor) continue;
        methodsCount++;
      }

      int nestedTypesCount = 0;
      foreach (var nestedType in nestedTypes) {
        if (AttributeHelper.Contains(nestedType.Attributes, nestedType.PlatformType.SystemRuntimeCompilerServicesCompilerGeneratedAttribute)) continue;
        nestedTypesCount++;
      }

      Comparison<IFieldDefinition> fcomparison = (x, y) => 
        x.IsCompileTimeConstant == y.IsCompileTimeConstant ? string.Compare(x.Name.Value, y.Name.Value) :
        (x.IsCompileTimeConstant?0:1) - (y.IsCompileTimeConstant?0:1);
      fields.Sort(fcomparison);
      Visit(fields);
      if (fields.Count > 0 && (properties.Count+methodsCount+events.Count+nestedTypesCount) > 0) sourceEmitterOutput.WriteLine("");

      Comparison<IPropertyDefinition> pcomparison = (x, y) => string.Compare(x.Name.Value, y.Name.Value);
      properties.Sort(pcomparison);
      Visit(properties);
      if (properties.Count > 0 && (methodsCount+events.Count+nestedTypesCount) > 0) sourceEmitterOutput.WriteLine("");

      Comparison<IMethodDefinition> mcomparison = (x, y) => string.Compare(x.Name.Value, y.Name.Value);
      methods.Sort(mcomparison);
      Visit(methods);
      if (methodsCount > 0 && (events.Count+nestedTypesCount) > 0) sourceEmitterOutput.WriteLine("");

      Comparison<IEventDefinition> ecomparison = (x, y) => string.Compare(x.Name.Value, y.Name.Value);
      events.Sort(ecomparison);
      Visit(events);
      if (events.Count > 0 && nestedTypesCount > 0) sourceEmitterOutput.WriteLine("");

      Comparison<INestedTypeDefinition> tcomparison = (x, y) => 
        x.Name.UniqueKey == y.Name.UniqueKey ? x.GenericParameterCount - y.GenericParameterCount : string.Compare(x.Name.Value, y.Name.Value);
      nestedTypes.Sort(tcomparison);
      Visit(nestedTypes);

      PrintTypeDefinitionRightCurly(typeDefinition);
    }

    public virtual void PrintTypeDefinitionAttributes(ITypeDefinition typeDefinition) {
      PrintAttributes(typeDefinition.Attributes);
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
      if (typeDefinition.IsAbstract)
        PrintKeywordAbstract();

      if (typeDefinition.IsSealed)
        PrintKeywordSealed();

      if (typeDefinition.IsStatic)
        PrintKeywordStatic();
    }

    public virtual void PrintTypeDefinitionKeywordType(ITypeDefinition typeDefinition) {
      if (typeDefinition.IsInterface)
        PrintKeywordInterface();
      else
        PrintKeywordClass();
    }

    public virtual void PrintKeywordClass() {
      PrintToken(CSharpToken.Class);
    }

    public virtual void PrintKeywordInterface() {
      PrintToken(CSharpToken.Interface);
    }

    public virtual void PrintTypeDefinitionName(ITypeDefinition typeDefinition) {
      INamespaceTypeDefinition namespaceTypeDefinition = typeDefinition as INamespaceTypeDefinition;
      if (namespaceTypeDefinition != null) {
        sourceEmitterOutput.Write(namespaceTypeDefinition.Name.Value);
        return;
      }

      INestedTypeDefinition nestedTypeDefinition = typeDefinition as INestedTypeDefinition;
      if (nestedTypeDefinition != null) {
        sourceEmitterOutput.Write(nestedTypeDefinition.Name.Value);
        return;
      }

      INamedEntity namedEntity = typeDefinition as INamedEntity;
      if (namedEntity != null) {
        sourceEmitterOutput.Write(namedEntity.Name.Value);
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
