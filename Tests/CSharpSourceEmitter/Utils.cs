using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Cci;

namespace CSharpSourceEmitter {

  // Note that we currently require only .NET 2.0, so some of these are things that are available
  // in later versions of .NET
  static class Utils {
    /// <summary>
    /// True if the specified type is defined in mscorlib and has the specified name
    /// </summary>
    public static bool IsMscorlibTypeNamed(ITypeReference type, string name) {
      return (TypeHelper.GetTypeName(type) == name &&
          UnitHelper.UnitsAreEquivalent(TypeHelper.GetDefiningUnitReference(type), TypeHelper.GetDefiningUnitReference(type.PlatformType.SystemObject)));
    }

    /// <summary>
    /// Returns the method from the closest base class that is hidden by the given method according to C# rules.
    /// If no such method exists, Dummy.Method is returned.
    /// </summary>
    public static IMethodDefinition GetHiddenBaseClassMethod(IMethodDefinition derivedClassMethod) {
      if (derivedClassMethod.IsConstructor) return Dummy.Method;
      if (derivedClassMethod.IsVirtual && !derivedClassMethod.IsNewSlot) return Dummy.Method;   // an override
      foreach (ITypeReference baseClassReference in derivedClassMethod.ContainingTypeDefinition.BaseClasses) {
        IMethodDefinition overriddenMethod = GetHiddenBaseClassMethod(derivedClassMethod, baseClassReference.ResolvedType);
        if (overriddenMethod != Dummy.Method) return overriddenMethod;
      }
      return Dummy.Method;
    }
    private static IMethodDefinition GetHiddenBaseClassMethod(IMethodDefinition derivedClassMethod, ITypeDefinition baseClass) {
      foreach (ITypeDefinitionMember baseMember in baseClass.GetMembersNamed(derivedClassMethod.Name, false)) {
        IMethodDefinition/*?*/ baseMethod = baseMember as IMethodDefinition;
        if (baseMethod == null) continue;
        if (baseMethod.Visibility == TypeMemberVisibility.Private) continue;
        if (!derivedClassMethod.IsHiddenBySignature) return baseMethod;
        if (derivedClassMethod.IsGeneric || baseMethod.IsGeneric) {
          if (derivedClassMethod.GenericParameterCount == baseMethod.GenericParameterCount &&
            IteratorHelper.EnumerablesAreEqual(derivedClassMethod.Parameters, baseMethod.Parameters, MemberHelper.GenericMethodParameterEqualityComparer))
            return baseMethod;
        } else if (IteratorHelper.EnumerablesAreEqual(((ISignature)derivedClassMethod).Parameters, ((ISignature)baseMethod).Parameters, MemberHelper.ParameterInformationComparer))
          return baseMethod;
      }
      foreach (ITypeReference baseClassReference in baseClass.BaseClasses) {
        IMethodDefinition overriddenMethod = GetHiddenBaseClassMethod(derivedClassMethod, baseClassReference.ResolvedType);
        if (overriddenMethod != Dummy.Method) return overriddenMethod;
      }
      return Dummy.Method;
    }

    /// <summary>
    /// Determine if the specified attribute is of a special type, and if so return a code representing it.
    /// This is usefull for types not alread in IPlatformType
    /// </summary>
    public static SpecialAttribute GetAttributeType(ICustomAttribute attr) {
      // This seems like a big hack
      // Perhaps I should add this as a PlatformType and use AttributeHelper.Contains instead?
      // There's got to be a cleaner way to do this?

      var type = attr.Type;
      var typeName = TypeHelper.GetTypeName(type);
      var attrUnit = TypeHelper.GetDefiningUnitReference(type);

      // mscorlib
      if (UnitHelper.UnitsAreEquivalent(TypeHelper.GetDefiningUnitReference(type), TypeHelper.GetDefiningUnitReference(type.PlatformType.SystemObject))) {
        switch (typeName) {
          case "System.FlagsAttribute": return SpecialAttribute.Flags;
          case "System.Runtime.CompilerServices.FixedBufferAttribute": return SpecialAttribute.FixedBuffer;
          case "System.ParamArrayAttribute": return SpecialAttribute.ParamArray;
          case "System.Reflection.DefaultMemberAttribute": return SpecialAttribute.DefaultMemberAttribute;
          case "System.Reflection.AssemblyKeyFileAttribute": return SpecialAttribute.AssemblyKeyFile;
          case "System.Reflection.AssemblyDelaySignAttribute": return SpecialAttribute.AssemblyDelaySign;
        }
      } else if (attrUnit.Name.Value == "System.Core") {
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
