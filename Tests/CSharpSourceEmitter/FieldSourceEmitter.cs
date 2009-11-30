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
    public override void Visit(IFieldDefinition fieldDefinition) {
      if (fieldDefinition.ContainingType.IsEnum && fieldDefinition.IsRuntimeSpecial && fieldDefinition.IsSpecialName)
        return; // implicit value field of an enum

      // Figure out if this is a special fixed buffer field
      ICustomAttribute fixedBufferAttr = Utils.FindAttribute(fieldDefinition.Attributes, SpecialAttribute.FixedBuffer);

      if (fixedBufferAttr == null)
        PrintAttributes(fieldDefinition.Attributes);

      if (fieldDefinition.ContainingTypeDefinition.Layout == LayoutKind.Explicit)
        sourceEmitterOutput.WriteLine(String.Format("[System.Runtime.InteropServices.FieldOffset({0})]", fieldDefinition.Offset), true);

      PrintToken(CSharpToken.Indent);

      if (fieldDefinition.IsCompileTimeConstant && fieldDefinition.ContainingType.IsEnum) {
        PrintFieldDefinitionEnumValue(fieldDefinition);
      } else {
        PrintFieldDefinitionVisibility(fieldDefinition);
        PrintFieldDefinitionModifiers(fieldDefinition);

        if (fixedBufferAttr == null) {
          PrintFieldDefinitionType(fieldDefinition);
          PrintToken(CSharpToken.Space);
          PrintFieldDefinitionName(fieldDefinition);
          if (fieldDefinition.IsCompileTimeConstant) {
            sourceEmitterOutput.Write(" = ");
            // For enums, the IMetadataConstant is just the primitive value
            if (fieldDefinition.Type.ResolvedType.IsEnum)
              PrintEnumValue(fieldDefinition.Type.ResolvedType, fieldDefinition.CompileTimeValue.Value);
            else
              this.Visit(fieldDefinition.CompileTimeValue);
          }
        } else {
          PrintFieldDefinitionFixedBuffer(fieldDefinition, fixedBufferAttr);
        }
        PrintToken(CSharpToken.Semicolon);
      }
    }

    public virtual void PrintFieldDefinitionVisibility(IFieldDefinition fieldDefinition) {
      PrintTypeMemberVisibility(fieldDefinition.Visibility);
    }

    public virtual void PrintFieldDefinitionModifiers(IFieldDefinition fieldDefinition) {

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
      PrintToken(CSharpToken.Space);
      PrintFieldDefinitionName(fieldDefinition);
      PrintToken(CSharpToken.LeftSquareBracket);
      int len = (int)(((IMetadataConstant)args[1]).Value);
      this.sourceEmitterOutput.Write(len.ToString());
      PrintToken(CSharpToken.RightSquareBracket);
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
        long lv = Convert.ToInt64(fieldDefinition.CompileTimeValue.Value);
        if (TypeHelper.IsSignedPrimitiveInteger(type) && lv < 0) {
          castNeeded = true;
          sourceEmitterOutput.Write("unchecked((");
          Visit(type);
          PrintToken(CSharpToken.RightParenthesis);
        }
      }
      // Output flags values in hex, non-flags in decimal 
      this.sourceEmitterOutput.Write(String.Format(isFlags ? "0x{0:X}" : "{0}", val));
      if (castNeeded)
        PrintToken(CSharpToken.RightParenthesis);
      PrintToken(CSharpToken.Comma);
      PrintToken(CSharpToken.NewLine);
    }
  }
}
