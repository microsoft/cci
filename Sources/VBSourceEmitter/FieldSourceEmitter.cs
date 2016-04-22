//-----------------------------------------------------------------------------
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the Microsoft Public License.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Cci;

namespace VBSourceEmitter {
  public partial class SourceEmitter : CodeTraverser, IVBSourceEmitter {
    public override void TraverseChildren(IFieldDefinition fieldDefinition) {
      if (fieldDefinition.ContainingType.IsEnum && fieldDefinition.IsRuntimeSpecial && fieldDefinition.IsSpecialName)
        return; // implicit value field of an enum

      if (!this.printCompilerGeneratedMembers &&
        AttributeHelper.Contains(fieldDefinition.Attributes, fieldDefinition.Type.PlatformType.SystemRuntimeCompilerServicesCompilerGeneratedAttribute))
        return; // eg. a cached anonymous delegate - may have invalid symbols

      foreach (var e in fieldDefinition.ContainingTypeDefinition.Events) {
        if (e.Name == fieldDefinition.Name)
          return;   // field is probably the implicit delegate backing the event
      }

      // Figure out if this is a special fixed buffer field
      ICustomAttribute fixedBufferAttr = Utils.FindAttribute(fieldDefinition.Attributes, SpecialAttribute.FixedBuffer);

      if (fixedBufferAttr == null)
        PrintAttributes(fieldDefinition);

      if (fieldDefinition.ContainingTypeDefinition.Layout == LayoutKind.Explicit)
        PrintPseudoCustomAttribute(fieldDefinition, "System.Runtime.InteropServices.FieldOffset", fieldDefinition.Offset.ToString(), true, null);

      PrintToken(VBToken.Indent);

      if (fieldDefinition.IsCompileTimeConstant && fieldDefinition.ContainingType.IsEnum) {
        PrintFieldDefinitionEnumValue(fieldDefinition);
      } else {
        PrintFieldDefinitionVisibility(fieldDefinition);
        PrintFieldDefinitionModifiers(fieldDefinition);

        if (fixedBufferAttr == null) {
          PrintToken(VBToken.Space);
          PrintFieldDefinitionName(fieldDefinition);
          if (fieldDefinition.IsCompileTimeConstant) {
            sourceEmitterOutput.Write(" = ");
            PrintFieldDefinitionValue(fieldDefinition);
          }
        } else {
          PrintFieldDefinitionFixedBuffer(fieldDefinition, fixedBufferAttr);
        }
        PrintToken(VBToken.Space);
        PrintToken(VBToken.As);
        PrintToken(VBToken.Space);
        PrintFieldDefinitionType(fieldDefinition);
        //PrintToken(VBToken.Semicolon);
      }
    }

    public virtual void PrintFieldDefinitionValue(IFieldDefinition fieldDefinition) {
      // We've got context here about the field that can be used to provide a better value.
      // For enums, the IMetadataConstant is just the primitive value
      var fieldType = fieldDefinition.Type.ResolvedType;
      if (fieldType.IsEnum) {
        PrintEnumValue(fieldType, fieldDefinition.CompileTimeValue.Value);
      } else if (TypeHelper.TypesAreEquivalent(fieldDefinition.ContainingTypeDefinition, fieldType.PlatformType.SystemFloat32) && 
                 fieldType.TypeCode == PrimitiveTypeCode.Float32) {
        // Defining System.Single, can't reference the symbolic names, use constant hacks instead
        float val = (float)fieldDefinition.CompileTimeValue.Value;
        if (float.IsNegativeInfinity(val))
          sourceEmitterOutput.Write("-1.0f / 0.0f");
        else if (float.IsPositiveInfinity(val))
          sourceEmitterOutput.Write("1.0f / 0.0f");
        else if (float.IsNaN(val))
          sourceEmitterOutput.Write("0.0f / 0.0f");
        else
          sourceEmitterOutput.Write(val.ToString("R") + "f");
      } else if (TypeHelper.TypesAreEquivalent(fieldDefinition.ContainingTypeDefinition, fieldType.PlatformType.SystemFloat64) &&
                 fieldType.TypeCode == PrimitiveTypeCode.Float64) {
        // Defining System.Double, can't reference the symbolic names, use constant hacks instead
        double val = (double)fieldDefinition.CompileTimeValue.Value;
        if (double.IsNegativeInfinity(val))
          sourceEmitterOutput.Write("-1.0 / 0.0");
        else if (double.IsPositiveInfinity(val))
          sourceEmitterOutput.Write("1.0 / 0.0");
        else if (double.IsNaN(val))
          sourceEmitterOutput.Write("0.0 / 0.0");
        else
          sourceEmitterOutput.Write(val.ToString("R"));
      } else if (TypeHelper.TypesAreEquivalent(fieldDefinition.ContainingTypeDefinition, fieldType) && 
        (fieldType.TypeCode == PrimitiveTypeCode.Int32 || fieldType.TypeCode == PrimitiveTypeCode.UInt32 ||
         fieldType.TypeCode == PrimitiveTypeCode.Int64 || fieldType.TypeCode == PrimitiveTypeCode.UInt64)) {
        // Defining a core integral system type, can't reference the symbolic names, use constants
        sourceEmitterOutput.Write(fieldDefinition.CompileTimeValue.Value.ToString());
      } else {
        this.Traverse(fieldDefinition.CompileTimeValue);
      }
    }

    public virtual void PrintFieldDefinitionVisibility(IFieldDefinition fieldDefinition) {
      PrintTypeMemberVisibility(fieldDefinition.Visibility);
    }

    public virtual void PrintFieldDefinitionModifiers(IFieldDefinition fieldDefinition) {

      if (!(Utils.GetHiddenField(fieldDefinition) is Dummy))
        PrintKeywordNew();

      if (fieldDefinition.Type.TypeCode == PrimitiveTypeCode.Pointer) {
        PrintKeywordUnsafe();
      }

      if (fieldDefinition.IsCompileTimeConstant) {
        sourceEmitterOutput.Write("const ");
        return;
      }

      if (fieldDefinition.IsStatic)
        PrintKeywordStatic();

      if (fieldDefinition.IsReadOnly)
        PrintKeywordReadOnly();

      if (MemberHelper.IsVolatile(fieldDefinition))
        sourceEmitterOutput.Write("volatile ");
    }

    public virtual void PrintFieldDefinitionType(IFieldDefinition fieldDefinition) {
      PrintTypeReference(fieldDefinition.Type);
    }

    public virtual void PrintFieldDefinitionName(IFieldDefinition fieldDefinition) {
      PrintIdentifier(fieldDefinition.Name);
    }

    public virtual void PrintFieldDefinitionFixedBuffer(IFieldDefinition fieldDefinition, ICustomAttribute fixedBufferAttribute) {
      PrintKeywordUnsafe();
      PrintKeywordFixed();
      var args = new List<IMetadataExpression>(fixedBufferAttribute.Arguments);
      PrintTypeReference(((IMetadataTypeOf)args[0]).TypeToGet.ResolvedType);
      PrintToken(VBToken.Space);
      PrintFieldDefinitionName(fieldDefinition);
      PrintToken(VBToken.LeftSquareBracket);
      int len = (int)(((IMetadataConstant)args[1]).Value);
      this.sourceEmitterOutput.Write(len.ToString());
      PrintToken(VBToken.RightSquareBracket);
    }

    public virtual void PrintFieldDefinitionEnumValue(IFieldDefinition fieldDefinition) {
      PrintFieldDefinitionName(fieldDefinition);
      sourceEmitterOutput.Write(" = ");
      var val = fieldDefinition.CompileTimeValue.Value;
      bool isFlags = (Utils.FindAttribute(fieldDefinition.ContainingTypeDefinition.Attributes, SpecialAttribute.Flags) != null);
      bool castNeeded = false;
      if (isFlags) {
        // Add cast if necessary
        var type = fieldDefinition.CompileTimeValue.Type;
        if (TypeHelper.IsSignedPrimitiveInteger(type) && Convert.ToInt64(val) < 0) {
          castNeeded = true;
          sourceEmitterOutput.Write("unchecked((");
          PrintTypeReference(type);
          PrintToken(VBToken.RightParenthesis);
        }
      }
      // Output flags values in hex, non-flags in decimal
      if (isFlags)
        this.sourceEmitterOutput.Write(String.Format("0x{0:X}", val));
      else
        Traverse(fieldDefinition.CompileTimeValue);
      if (castNeeded)
        PrintToken(VBToken.RightParenthesis);
      PrintToken(VBToken.Comma);
      PrintToken(VBToken.NewLine);
    }
  }
}
