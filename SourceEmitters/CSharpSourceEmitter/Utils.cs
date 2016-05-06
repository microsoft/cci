using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Cci;
using System.Diagnostics.Contracts;

namespace CSharpSourceEmitter {

  // Note that we currently require only .NET 2.0, so some of these are things that are available
  // in later versions of .NET
  static class Utils {
    /// <summary>
    /// True if the specified type is defined in mscorlib and has the specified name
    /// </summary>
    public static bool IsMscorlibTypeNamed(ITypeReference type, string name) {
      Contract.Requires(type != null);

      return (TypeHelper.GetTypeName(type) == name &&
          UnitHelper.UnitsAreEquivalent(TypeHelper.GetDefiningUnitReference(type), TypeHelper.GetDefiningUnitReference(type.PlatformType.SystemObject)));
    }

    /// <summary>
    /// Returns the method from the closest base class that is hidden by the given method according to C# rules.
    /// If the method is an interface method definition, then look at it's base interfaces
    /// If no such method exists, Dummy.MethodDefinition is returned.
    /// </summary>
    public static IMethodDefinition GetHiddenBaseClassMethod(IMethodDefinition derivedClassMethod) {
      Contract.Requires(derivedClassMethod != null);

      if (derivedClassMethod.IsConstructor) return Dummy.MethodDefinition;
      if (derivedClassMethod.IsVirtual && !derivedClassMethod.IsNewSlot) return Dummy.MethodDefinition;   // an override
      var typeDef = derivedClassMethod.ContainingTypeDefinition;
      var bases = typeDef.IsInterface ? typeDef.Interfaces : typeDef.BaseClasses;
      foreach (ITypeReference baseClassReference in bases) {
        IMethodDefinition overriddenMethod = GetHiddenBaseClassMethod(derivedClassMethod, baseClassReference.ResolvedType);
        if (!(overriddenMethod is Dummy)) return overriddenMethod;
      }
      return Dummy.MethodDefinition;
    }
    private static IMethodDefinition GetHiddenBaseClassMethod(IMethodDefinition derivedClassMethod, ITypeDefinition baseClass) {
      Contract.Requires(derivedClassMethod != null);
      Contract.Requires(baseClass != null);

      foreach (ITypeDefinitionMember baseMember in baseClass.GetMembersNamed(derivedClassMethod.Name, false)) {
        IMethodDefinition/*?*/ baseMethod = baseMember as IMethodDefinition;
        if (baseMethod == null) continue;
        if (baseMethod.Visibility == TypeMemberVisibility.Private) continue;
        if ((baseMethod.Visibility == TypeMemberVisibility.Assembly || baseMethod.Visibility == TypeMemberVisibility.FamilyAndAssembly) &&
          !UnitHelper.UnitsAreEquivalent(TypeHelper.GetDefiningUnit(derivedClassMethod.ContainingTypeDefinition), TypeHelper.GetDefiningUnit(baseClass)))
          continue;
        if (!derivedClassMethod.IsHiddenBySignature) return baseMethod;
        if (derivedClassMethod.IsGeneric || baseMethod.IsGeneric) {
          if (derivedClassMethod.GenericParameterCount == baseMethod.GenericParameterCount &&
            IteratorHelper.EnumerablesAreEqual(((ISignature)derivedClassMethod).Parameters, ((ISignature)baseMethod).Parameters, MemberHelper.GenericMethodParameterEqualityComparer))
            return baseMethod;
        } else if (IteratorHelper.EnumerablesAreEqual(((ISignature)derivedClassMethod).Parameters, ((ISignature)baseMethod).Parameters, MemberHelper.ParameterInformationComparer))
          return baseMethod;
      }
      var bases = baseClass.IsInterface ? baseClass.Interfaces : baseClass.BaseClasses;
      foreach (ITypeReference baseClassReference in bases) {
        IMethodDefinition overriddenMethod = GetHiddenBaseClassMethod(derivedClassMethod, baseClassReference.ResolvedType);
        if (!(overriddenMethod is Dummy)) return overriddenMethod;
      }
      return Dummy.MethodDefinition;
    }

    /// <summary>
    /// Returns the field from the closest base class that is hidden by the given field according to C# rules.
    /// </summary>
    public static IFieldDefinition GetHiddenField(IFieldDefinition derivedClassField) {
      Contract.Requires(derivedClassField != null);

      var typeDef = derivedClassField.ContainingTypeDefinition;
      foreach (ITypeReference baseClassReference in typeDef.BaseClasses) {
        IFieldDefinition hiddenField = GetHiddenField(derivedClassField, baseClassReference.ResolvedType);
        if (!(hiddenField is Dummy)) return hiddenField;
      }
      return Dummy.FieldDefinition;
    }
    private static IFieldDefinition GetHiddenField(IFieldDefinition derivedClassField, ITypeDefinition baseClass) {
      Contract.Requires(baseClass != null);
      Contract.Requires(derivedClassField != null);

      foreach (ITypeDefinitionMember baseMember in baseClass.GetMembersNamed(derivedClassField.Name, false)) {
        IFieldDefinition/*?*/ baseField = baseMember as IFieldDefinition;
        if (baseField == null) continue;
        if (baseField.Visibility == TypeMemberVisibility.Private) continue;
        return baseField;
      }
      var bases = baseClass.IsInterface ? baseClass.Interfaces : baseClass.BaseClasses;
      foreach (ITypeReference baseClassReference in bases) {
        IFieldDefinition hiddenField = GetHiddenField(derivedClassField, baseClassReference.ResolvedType);
        if (!(hiddenField is Dummy)) return hiddenField;
      }
      return Dummy.FieldDefinition;
    }

    /// <summary>
    /// Determine if the specified attribute is of a special type, and if so return a code representing it.
    /// This is usefull for types not alread in IPlatformType
    /// </summary>
    public static SpecialAttribute GetAttributeType(ICustomAttribute attr) {
      Contract.Requires(attr != null);

      // This seems like a big hack
      // Perhaps I should add this as a PlatformType and use AttributeHelper.Contains instead?
      // There's got to be a cleaner way to do this?

      var type = attr.Type;
      var typeName = TypeHelper.GetTypeName(type);
      var attrUnit = TypeHelper.GetDefiningUnitReference(type);
      var mscorlibUnit = TypeHelper.GetDefiningUnitReference(type.PlatformType.SystemObject);

      // mscorlib
      if (UnitHelper.UnitsAreEquivalent(attrUnit, mscorlibUnit) || (attrUnit != null && mscorlibUnit != null && attrUnit.ResolvedUnit == mscorlibUnit.ResolvedUnit)) {
        switch (typeName) {
          case "System.FlagsAttribute": return SpecialAttribute.Flags;
          case "System.Runtime.CompilerServices.FixedBufferAttribute": return SpecialAttribute.FixedBuffer;
          case "System.ParamArrayAttribute": return SpecialAttribute.ParamArray;
          case "System.Reflection.DefaultMemberAttribute": return SpecialAttribute.DefaultMemberAttribute;
          case "System.Reflection.AssemblyKeyFileAttribute": return SpecialAttribute.AssemblyKeyFile;
          case "System.Reflection.AssemblyDelaySignAttribute": return SpecialAttribute.AssemblyDelaySign;
        }
      } else if (attrUnit != null && attrUnit.Name.Value == "System.Core") {
        switch (typeName) {
          case "System.Runtime.CompilerServices.ExtensionAttribute": return SpecialAttribute.Extension;
        }
      }
      return SpecialAttribute.None;
    }

    /// <summary>
    /// IF an attribute of the specified special type exists in the sequence, return it.  Otherwise return null.
    /// </summary>
    public static ICustomAttribute FindAttribute(IEnumerable<ICustomAttribute> attrs, SpecialAttribute sa) {
      Contract.Requires(attrs != null);

      foreach (var a in attrs)
        if (GetAttributeType(a) == sa)
          return a;
      return null;
    }
  }

  /// <summary>
  /// Identifiers for some common attribute types
  /// </summary>
  public enum SpecialAttribute {
    None,
    Flags,
    Extension,
    FixedBuffer,
    ParamArray,
    DefaultMemberAttribute,
    AssemblyKeyFile,
    AssemblyDelaySign,
  }
}
