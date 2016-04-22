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
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci.Ast {

  /// <summary>
  /// Corresponds to a metadata custom attribute.
  /// </summary>
  public class CustomAttribute : ICustomAttribute {

    /// <summary>
    /// Constructs a metadata custom attribute that is a projection of a corresponding source custom attribute.
    /// </summary>
    /// <param name="sourceAttribute">The source custom attribute to project onto the symbol table attribute being constructed.</param>
    public CustomAttribute(SourceCustomAttribute sourceAttribute) {
      this.sourceAttribute = sourceAttribute;
    }

    /// <summary>
    /// The source attribute that is being projected onto this symbol table attribute.
    /// </summary>
    SourceCustomAttribute sourceAttribute;

    /// <summary>
    /// Specifies whether more than one instance of this type of attribute is allowed on same element. This information is obtained from an attribute on the attribute type definition.
    /// </summary>
    public bool AllowMultiple {
      get { return this.sourceAttribute.AllowMultiple; }
    }

    /// <summary>
    /// A list zero or more positional arguments for the attribute constructor, followed by zero or more named arguments that specify values for fields and properties of the attribute.
    /// </summary>
    public IEnumerable<Expression> Arguments {
      get { return this.sourceAttribute.Arguments; }
    }

    /// <summary>
    /// A reference to the constructor that will be used to instantiate this custom attribute during execution (if the attribute is inspected via Reflection).
    /// </summary>
    public IMethodReference Constructor {
      get { return this.sourceAttribute.Constructor; }
    }

    /// <summary>
    /// Calls the visitor.Visit(ICustomAttribute) method.
    /// </summary>
    public virtual void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Specifies whether this attribute applies to derived types and/or overridden methods. This information is obtained from an attribute on the attribute type definition.
    /// </summary>
    public bool Inherited {
      get { return this.sourceAttribute.Inherited; }
    }

    /// <summary>
    /// The type of the attribute. For example System.AttributeUsageAttribute.
    /// </summary>
    public ITypeReference Type {
      get { return this.sourceAttribute.Type.ResolvedType; }
    }

    /// <summary>
    /// Specifies the symbol table elements on which it is valid to apply this attribute. This information is obtained from an attribute on the attribute type definition.
    /// </summary>
    public AttributeTargets ValidOn {
      get { return this.sourceAttribute.ValidOn; }
    }

    #region ICustomAttribute Members

    IEnumerable<IMetadataExpression> ICustomAttribute.Arguments {
      get {
        foreach (Expression expression in this.Arguments) {
          if (expression is NamedArgument) break;
          yield return expression.ProjectAsIMetadataExpression();
        }
      }
    }

    IMethodReference ICustomAttribute.Constructor {
      get {
        return this.Constructor;
      }
    }

    IEnumerable<IMetadataNamedArgument> ICustomAttribute.NamedArguments {
      get {
        foreach (Expression expression in this.Arguments) {
          NamedArgument/*?*/ namedArgument = expression as NamedArgument;
          if (namedArgument == null) continue;
          yield return namedArgument;
        }
      }
    }

    ushort ICustomAttribute.NumberOfNamedArguments {
      get {
        ushort result = 0;
        foreach (Expression expression in this.Arguments) {
          NamedArgument/*?*/ namedArgument = expression as NamedArgument;
          if (namedArgument == null) continue;
          result++;
        }
        return result;
      }
    }

    #endregion
  }

  /// <summary>
  /// A structured comment, such as this one, attached to a declaration.
  /// </summary>
  public class Documentation {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="elements"></param>
    public Documentation(IEnumerable<DocumentationElement> elements) {
      this.elements = elements;
    }

    /// <summary>
    /// A collection of tagged (and attributed) elements that define the structured documentation.
    /// </summary>
    public IEnumerable<DocumentationElement> Elements {
      get { return this.elements; }
    }
    readonly IEnumerable<DocumentationElement> elements;

    /// <summary>
    /// The contents of the comment in plain text form.
    /// </summary>
    public virtual string Text {
      get {
        if (this.text == null) {
          this.text = string.Empty;
          foreach (DocumentationElement element in this.Elements) {
            if (element.Name.Value == "summary") {
              this.text = element.Text;
              break;
            }
          }
        }
        return this.text;
      }
    }
    string/*?*/ text;

  }

  /// <summary>
  /// A (name, string) pair that helps to define a documentation element. For example cref="IDocumentationAttribute" or type="bullet".
  /// </summary>
  public class DocumentationAttribute : SourceItem {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <param name="sourceLocation"></param>
    public DocumentationAttribute(IName name, string value, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.name = name;
      this.value = value;
    }

    /// <summary>
    /// The name of the attribute.
    /// </summary>
    public IName Name {
      get { return this.name; }
    }
    readonly IName name;

    /// <summary>
    /// The value of the attribute.
    /// </summary>
    public string Value {
      get { return this.value; }
    }
    readonly string value;

  }

  /// <summary>
  /// A part of a structured comment enclosed by a start tag (such as &lt;summary&gt;) and an end tag (such as &lt;/summary&gt;).
  /// </summary>
  public class DocumentationElement : SourceItem {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="attributes"></param>
    /// <param name="children"></param>
    /// <param name="sourceLocation"></param>
    public DocumentationElement(IName name, IEnumerable<DocumentationAttribute> attributes, IEnumerable<DocumentationElement> children, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.attributes = attributes;
      this.children = children;
      this.name = name;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="text"></param>
    /// <param name="sourceLocation"></param>
    public DocumentationElement(string text, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.name = Dummy.Name;
      this.attributes = EmptyAttributeCollection;
      this.children = EmptyElementCollection;
      this.text = text;
    }

    readonly static IEnumerable<DocumentationElement> EmptyElementCollection = new List<DocumentationElement>(0).AsReadOnly();
    readonly static IEnumerable<DocumentationAttribute> EmptyAttributeCollection = new List<DocumentationAttribute>(0).AsReadOnly();

    internal void AppendText(StringBuilder builder) {
      if (this.text != null) {
        builder.Append(this.text); return;
      }
      foreach (DocumentationElement child in this.Children)
        child.AppendText(builder);
    }

    /// <summary>
    /// The attributes defined for the element. These are included in the start tag.
    /// </summary>
    public IEnumerable<DocumentationAttribute> Attributes {
      get { return this.attributes; }
    }
    readonly IEnumerable<DocumentationAttribute> attributes;

    /// <summary>
    /// A list of tagged (and untagged text) elements that collectively make up the contents of the parent element.
    /// </summary>
    public IEnumerable<DocumentationElement> Children {
      get { return this.children; }
    }
    readonly IEnumerable<DocumentationElement> children;

    /// <summary>
    /// The (tag) name of the element (for example "summary"). May be Dummy.Name, in which case the element is really just text and the Attributes and Children collections will be empty.
    /// </summary>
    public IName Name {
      get { return this.name; }
    }
    readonly IName name;

    /// <summary>
    /// The contents of the element in plain text form.
    /// </summary>
    public string Text {
      get {
        if (this.text == null) {
          lock (GlobalLock.LockingObject) {
            if (this.text == null) {
              StringBuilder sb = new StringBuilder();
              this.AppendText(sb);
              this.text = sb.ToString();
            }
          }
        }
        return this.text;
      }
    }
    string/*?*/ text;

  }

  /// <summary>
  /// When managed code calls unmanaged methods or exposes managed fields to unmanaged code, it is sometimes necessary to provide specific information about how the 
  /// managed types should be marshalled to and from the unmanaged types.
  /// </summary>
  public class MarshallingInformation : IMarshallingInformation {

    /// <summary>
    /// When managed code calls unmanaged methods or exposes managed fields to unmanaged code, it is sometimes necessary to provide specific information about how the 
    /// managed types should be marshalled to and from the unmanaged types.
    /// </summary>
    /// <param name="unmanagedType">
    /// The unmanaged type to which the managed type will be marshalled. This can be be UnmanagedType.CustomMarshaler, in which case the umanaged type
    /// is decided at runtime.
    /// </param>
    /// The unmanged element type of the unmanaged array.
    /// <param name="elementType">
    /// </param>
    /// <param name="elementSize">
    /// The size of an element of the fixed sized umanaged array.
    /// </param>
    /// <param name="elementSizeMultiplier">
    /// A multiplier that must be applied to the value of the parameter specified by ParamIndex in order to work out the total size of the unmanaged array.
    /// </param>
    /// <param name="iidParameterIndex">Specifies the index of the parameter that contains the value of the Inteface Identifier (IID) of the marshalled object.</param>
    /// <param name="numberOfElements">
    /// The number of elements in the fixed size portion of the unmanaged array.
    /// </param>
    /// <param name="paramIndex">
    /// The zero based index of the parameter in the unmanaged method that contains the number of elements in the variable portion of unmanaged array.
    /// If the index is null, the variable portion is of size zero, or the caller conveys the size of the variable portion of the array to the unmanaged method in some other way.
    /// </param>
    /// <param name="safeArrayElementSubtype">
    /// The type to which the variant values of all elements of the safe array must belong. See also SafeArrayElementUserDefinedSubType.
    /// (The element type of a safe array is VARIANT. The "sub type" specifies the value of all of the tag fields (vt) of the element values. )
    /// </param>
    /// <param name="safeArrayElementUserDefinedSubtype">
    /// A reference to the user defined type to which the variant values of all elements of the safe array must belong.
    /// (The element type of a safe array is VARIANT. The tag fields will all be either VT_DISPATCH or VT_UNKNOWN or VT_RECORD.
    /// The "user defined sub type" specifies the type of value the ppdispVal/ppunkVal/pvRecord fields of the element values may point to.)
    /// May be null if the above does not apply.
    /// </param>
    /// <param name="customMarshaller">
    /// A reference to the type implementing the custom marshaller. Must not be be null if unmanagedType == System.Runtime.InteropServices.UnmanagedType.CustomMarshaler.
    /// </param>
    /// <param name="customMarshallerRuntimeArgument">
    /// An argument string (cookie) passed to the custom marshaller at run time. Must not be be null if unmanagedType == System.Runtime.InteropServices.UnmanagedType.CustomMarshaler.
    /// </param>
    public MarshallingInformation(System.Runtime.InteropServices.UnmanagedType unmanagedType,
      System.Runtime.InteropServices.UnmanagedType elementType, uint elementSize, uint elementSizeMultiplier, uint iidParameterIndex, uint numberOfElements, uint? paramIndex,
      System.Runtime.InteropServices.VarEnum safeArrayElementSubtype, ITypeReference/*?*/ safeArrayElementUserDefinedSubtype,
      ITypeReference/*?*/ customMarshaller, string/*?*/ customMarshallerRuntimeArgument)
      //^ requires unmanagedType == System.Runtime.InteropServices.UnmanagedType.CustomMarshaler ==> 
      //^   customMarshaller != null && customMarshallerRuntimeArgument != null;
      //^ requires iidParameterIndex > 0 ==> unmanagedType == System.Runtime.InteropServices.UnmanagedType.Interface;
      //^ requires unmanagedType == System.Runtime.InteropServices.UnmanagedType.SafeArray &&
      //^  (safeArrayElementSubType == System.Runtime.InteropServices.VarEnum.VT_DISPATCH ||
      //^   safeArrayElementSubType == System.Runtime.InteropServices.VarEnum.VT_UNKNOWN ||
      //^   safeArrayElementSubType == System.Runtime.InteropServices.VarEnum.VT_RECORD) ==>
      //^      safeArrayElementUserDefinedSubType != null;
    {
      this.customMarshaller = customMarshaller;
      this.customMarshallerRuntimeArgument = customMarshallerRuntimeArgument;
      this.elementSize = elementSize;
      this.elementSizeMultiplier = elementSizeMultiplier;
      this.elementType = elementType;
      this.iidParameterIndex = iidParameterIndex;
      this.numberOfElements = numberOfElements;
      this.paramIndex = paramIndex;
      this.safeArrayElementSubtype = safeArrayElementSubtype;
      this.safeArrayElementUserDefinedSubtype = safeArrayElementUserDefinedSubtype;
      this.unmanagedType = unmanagedType;
    }

    /// <summary>
    /// A reference to the type implementing the custom marshaller.
    /// </summary>
    public ITypeReference CustomMarshaller {
      get {
        //^ assume this.customMarshaller != null; //follows from the precondition
        return this.customMarshaller;
      }
    }
    readonly ITypeReference/*?*/ customMarshaller;

    /// <summary>
    /// An argument string (cookie) passed to the custom marshaller at run time.
    /// </summary>
    public string CustomMarshallerRuntimeArgument {
      get {
        //^ assume this.customMarshallerRuntimeArgument != null; //follows from the precondition
        return this.customMarshallerRuntimeArgument;
      }
    }
    readonly string/*?*/ customMarshallerRuntimeArgument;

    /// <summary>
    /// The size of an element of the fixed sized umanaged array.
    /// </summary>
    public uint ElementSize {
      get { return this.elementSize; }
    }
    readonly uint elementSize;

    /// <summary>
    /// The unmanged element type of the unmanaged array.
    /// </summary>
    public System.Runtime.InteropServices.UnmanagedType ElementType {
      get { return this.elementType; }
    }
    readonly System.Runtime.InteropServices.UnmanagedType elementType;

    /// <summary>
    /// Specifies the index of the parameter that contains the value of the Inteface Identifier (IID) of the marshalled object.
    /// </summary>
    public uint IidParameterIndex {
      get { return this.iidParameterIndex; }
    }
    readonly uint iidParameterIndex;

    /// <summary>
    /// The unmanaged type to which the managed type will be marshalled. This can be be UnmanagedType.CustomMarshaler, in which case the umanaged type
    /// is decided at runtime.
    /// </summary>
    public System.Runtime.InteropServices.UnmanagedType UnmanagedType {
      get { return this.unmanagedType; }
    }
    readonly System.Runtime.InteropServices.UnmanagedType unmanagedType;

    /// <summary>
    /// The number of elements in the fixed size portion of the unmanaged array.
    /// </summary>
    public uint NumberOfElements {
      get { return this.numberOfElements; }
    }
    readonly uint numberOfElements;

    /// <summary>
    /// The zero based index of the parameter in the unmanaged method that contains the number of elements in the variable portion of unmanaged array.
    /// If the index is null, the variable portion is of size zero, or the caller conveys the size of the variable portion of the array to the unmanaged method in some other way.
    /// </summary>
    public uint? ParamIndex {
      get { return this.paramIndex; }
    }
    readonly uint? paramIndex;

    /// <summary>
    /// The type to which the variant values of all elements of the safe array must belong. See also SafeArrayElementUserDefinedSubType.
    /// (The element type of a safe array is VARIANT. The "sub type" specifies the value of all of the tag fields (vt) of the element values. )
    /// </summary>
    public System.Runtime.InteropServices.VarEnum SafeArrayElementSubtype {
      get { return this.safeArrayElementSubtype; }
    }
    readonly System.Runtime.InteropServices.VarEnum safeArrayElementSubtype;

    /// <summary>
    /// A reference to the user defined type to which the variant values of all elements of the safe array must belong.
    /// (The element type of a safe array is VARIANT. The tag fields will all be either VT_DISPATCH or VT_UNKNOWN or VT_RECORD.
    /// The "user defined sub type" specifies the type of value the ppdispVal/ppunkVal/pvRecord fields of the element values may point to.)
    /// </summary>
    public ITypeReference SafeArrayElementUserDefinedSubtype {
      get {
        //^ assume safeArrayElementUserDefinedSubtype != null; //follows from the precondition
        return this.safeArrayElementUserDefinedSubtype;
      }
    }
    readonly ITypeReference/*?*/ safeArrayElementUserDefinedSubtype;

    /// <summary>
    /// A multiplier that must be applied to the value of the parameter specified by ParamIndex in order to work out the total size of the unmanaged array.
    /// </summary>
    public uint ElementSizeMultiplier {
      get { return this.elementSizeMultiplier; }
    }
    readonly uint elementSizeMultiplier;

  }

  /// <summary>
  /// A source code item corresponding to a name.
  /// </summary>
  public class NameDeclaration : IName, ISourceItem {

    /// <summary>
    /// Allocates a source code item corresponding to a name.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="sourceLocation">The location of the corresponding source code item.</param>
    public NameDeclaration(IName name, ISourceLocation sourceLocation) {
      this.sourceLocation = sourceLocation;
      this.name = name;
    }

    /// <summary>
    /// Allocates a copy of the given name declaration, but using the name table of the given
    /// target compilation to obtain the corresponding IName value.
    /// </summary>
    /// <param name="targetCompilation">The compilation that is the root node of the AST of which the new name declaration will be a part.</param>
    /// <param name="template">A name declaration from another declaration, whose source location and value is to be copied by the new name declaration.</param>
    protected NameDeclaration(Compilation targetCompilation, NameDeclaration template) {
      this.sourceLocation = template.SourceLocation;
      this.name = targetCompilation.NameTable.GetNameFor(template.Value);
    }

    /// <summary>
    /// Calls visitor.Visit((SourceDeclaration)this).
    /// </summary>
    public virtual void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The name being declared.
    /// </summary>
    public IName Name {
      get { return this.name; }
    }
    readonly IName name;

    /// <summary>
    /// Makes a copy of this name declaration using the name table from the given target declaration.
    /// </summary>
    public virtual NameDeclaration MakeCopyFor(Compilation targetCompilation) {
      return new NameDeclaration(targetCompilation, this);
    }

    /// <summary>
    /// The location in the source document that has been parsed to construct this item.
    /// </summary>
    /// <value></value>
    public ISourceLocation SourceLocation {
      get { return this.sourceLocation; }
    }
    readonly ISourceLocation sourceLocation;

    //^ [Confined]
    /// <summary>
    /// Returns a string that represents this instance.
    /// </summary>
    public override string ToString() {
      return "NameDeclaration{Name="+this.name.Value+"}";
    }

    /// <summary>
    /// An integer that is unique within the pool from which the name instance has been allocated. Useful as a hashtable key.
    /// </summary>
    /// <value></value>
    public int UniqueKey {
      get { return this.name.UniqueKey; }
    }

    /// <summary>
    /// An integer that is unique within the pool from which the name instance has been allocated. Useful as a hashtable key.
    /// All name instances in the pool that have the same string value when ignoring the case of the characters in the string
    /// will have the same key value.
    /// </summary>
    /// <value></value>
    public int UniqueKeyIgnoringCase {
      get { return this.name.UniqueKeyIgnoringCase; }
    }

    /// <summary>
    /// The string value corresponding to this name.
    /// </summary>
    /// <value></value>
    public string Value {
      get { return this.name.Value; }
    }

  }

  internal sealed class PlatformInvokeInformation : IPlatformInvokeInformation {

    internal PlatformInvokeInformation(ITypeDeclarationMember declaringMember, SourceCustomAttribute dllImportAttribute) {
      this.importModule = Dummy.ModuleReference;
      this.importName = declaringMember.Name.Name;
      this.noMangle = false;
      this.pinvokeCallingConvention = PInvokeCallingConvention.WinApi;
      this.stringFormat = StringFormatKind.Unspecified;
      this.useBestFit = null;
      this.throwExceptionForUnmappableChar = null;

      INameTable nameTable = dllImportAttribute.ContainingBlock.NameTable;
      int bestFitMappingKey = nameTable.GetNameFor("BestFitMapping").UniqueKey;
      int callingConventionKey = nameTable.GetNameFor("CallingConvention").UniqueKey;
      int charSetKey = nameTable.GetNameFor("CharSet").UniqueKey;
      int entryPointKey = nameTable.GetNameFor("EntryPoint").UniqueKey;
      int exactSpellingKey = nameTable.GetNameFor("ExactSpelling").UniqueKey;
      int setLastErrorKey = nameTable.GetNameFor("SetLastError").UniqueKey;
      int throwOnUnmappableCharKey = nameTable.GetNameFor("ThrowOnUnmappableChar").UniqueKey;

      foreach (Expression expr in dllImportAttribute.Arguments) {
        CompileTimeConstant cc = expr as CompileTimeConstant;
        if (cc != null && cc.Value is string) {
          IName moduleName = expr.NameTable.GetNameFor((string)cc.Value);
          this.importModule = new Immutable.ModuleReference(dllImportAttribute.ContainingBlock.Compilation.HostEnvironment, new ModuleIdentity(moduleName, string.Empty));
          continue;
        }
        NamedArgument narg = expr as NamedArgument;
        if (narg == null) continue;
        int key = narg.ArgumentName.Name.UniqueKey;
        if (key == bestFitMappingKey) {
          if (narg.ArgumentValue.Value is bool)
            this.useBestFit = (bool)narg.ArgumentValue.Value;
          continue;
        }
        if (key == callingConventionKey) {
          if (narg.ArgumentValue.Value is int) {
            switch ((CallingConvention)(int)narg.ArgumentValue.Value) {
              case CallingConvention.C: this.pinvokeCallingConvention = PInvokeCallingConvention.CDecl; break;
              case CallingConvention.Standard: this.pinvokeCallingConvention = PInvokeCallingConvention.StdCall; break;
              case CallingConvention.ExplicitThis: this.pinvokeCallingConvention = PInvokeCallingConvention.ThisCall; break;
              case CallingConvention.FastCall: this.pinvokeCallingConvention = PInvokeCallingConvention.FastCall; break;
            }
          }
          continue;
        }
        if (key == charSetKey) {
          if (narg.ArgumentValue.Value is int) {
            switch ((CharSet)(int)narg.ArgumentValue.Value) {
              case CharSet.Ansi: this.stringFormat = StringFormatKind.Ansi; break;
              case CharSet.Auto: this.stringFormat = StringFormatKind.AutoChar; break;
              case CharSet.Unicode: this.stringFormat = StringFormatKind.Unicode; break;
            }
          }
          continue;
        }
        if (key == entryPointKey) {
          string/*?*/ importName = narg.ArgumentValue.Value as string;
          if (importName != null) {
            this.importName = nameTable.GetNameFor(importName);
          }
          continue;
        }
        if (key == exactSpellingKey) {
          if (narg.ArgumentValue.Value is bool)
            this.noMangle = (bool)narg.ArgumentValue.Value;
          continue;
        }
        if (key == setLastErrorKey) {
          if (narg.ArgumentValue.Value is bool)
            this.supportsLastError = (bool)narg.ArgumentValue.Value;
          continue;
        }
        if (key == throwOnUnmappableCharKey) {
          if (narg.ArgumentValue.Value is bool)
            this.throwExceptionForUnmappableChar = (bool)narg.ArgumentValue.Value;
          continue;
        }
      }
    }

    public IModuleReference ImportModule {
      get { return this.importModule; }
    }
    IModuleReference importModule;

    public IName ImportName {
      get { return this.importName; }
    }
    IName importName;

    public bool NoMangle {
      get { return this.noMangle; }
    }
    bool noMangle;

    public StringFormatKind StringFormat {
      get { return this.stringFormat; }
    }
    StringFormatKind stringFormat;

    public PInvokeCallingConvention PInvokeCallingConvention {
      get { return this.pinvokeCallingConvention; }
    }
    PInvokeCallingConvention pinvokeCallingConvention;

    public bool SupportsLastError {
      get { return this.supportsLastError; }
    }
    bool supportsLastError;

    public bool? UseBestFit {
      get { return this.useBestFit; }
    }
    bool? useBestFit;

    public bool? ThrowExceptionForUnmappableChar {
      get { return this.throwExceptionForUnmappableChar; }
    }
    bool? throwExceptionForUnmappableChar;

  }

  /// <summary>
  /// A custom attribute as it appears in source code. This may have special meaning to the compiler and might not be translated to a metadata custom attribute.
  /// </summary>
  public class SourceCustomAttribute : CheckableSourceItem {

    /// <summary>
    /// Allocates a custom attribute as it appears in source code. This may have special meaning to the compiler and might not be translated to a metadata custom attribute.
    /// </summary>
    /// <param name="targets">The kinds of symbols that are the targetted by this attribute. Usually a single target will be specified.</param>
    /// <param name="type"></param>
    /// <param name="arguments"></param>
    /// <param name="sourceLocation"></param>
    public SourceCustomAttribute(AttributeTargets targets, AttributeTypeExpression type, List<Expression> arguments, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.arguments = arguments;
      this.targets = targets;
      this.type = type;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected SourceCustomAttribute(BlockStatement containingBlock, SourceCustomAttribute template)
      : base(template.SourceLocation)
      //^ ensures this.containingBlock == containingBlock;
    {
      this.arguments = new List<Expression>(template.arguments);
      this.targets = template.targets;
      this.type = (AttributeTypeExpression)template.type.MakeCopyFor(containingBlock);
      this.containingBlock = containingBlock;
    }

    /// <summary>
    /// Specifies whether more than one instance of this type of attribute is allowed on same element. This information is obtained from an attribute on the attribute type definition.
    /// </summary>
    public bool AllowMultiple {
      get {
        if (this.flags == 0) this.GetUsageInformation();
        return (this.flags & 0x20000000) != 0;
      }
    }

    /// <summary>
    /// A list zero or more postional arguments for the attribute constructor, followed by zero or more named arguments that specify values for fields and properties of the attribute.
    /// </summary>
    public IEnumerable<Expression> Arguments {
      get {
        for (int i = 0, n = this.arguments.Count; i < n; i++)
          yield return this.arguments[i] = this.arguments[i].MakeCopyFor(this.ContainingBlock);
      }
    }
    readonly List<Expression> arguments;

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the member or a constituent part of the member.
    /// Do not call this method directly, but evaluate the HasErrors property. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      bool result = this.Type.HasErrors;
      foreach (var argument in this.Arguments)
        result |= argument.HasErrors;
      if (!result) {
        //TODO: check that constructor resolves
      }
      return result;
    }

    /// <summary>
    /// A reference to the constructor of the custom attribute.
    /// </summary>
    public IMethodReference Constructor {
      get {
        if (this.constructor == null)
          this.constructor = this.GetConstructor();
        return this.constructor;
      }
    }
    //^ [Once]
    IMethodReference/*?*/ constructor;

    /// <summary>
    /// A list of zero of more positional arguments for the attribute constructor.
    /// </summary>
    public IEnumerable<Expression> ConstructorArguments {
      get {
        for (int i = 0, n = this.arguments.Count; i < n; i++) {
          if (this.arguments[i] is NamedArgument) yield break;
          yield return this.arguments[i] = this.arguments[i].MakeCopyFor(this.ContainingBlock);
        }
      }
    }

    /// <summary>
    /// The (dummy) block used to provide a scope chain for the type expression and the argument expressions.
    /// </summary>
    public BlockStatement ContainingBlock {
      get
        //^ ensures result == this.containingBlock;
      {
        //^ assume this.containingBlock != null;
        return this.containingBlock;
      }
    }
    /// <summary>
    /// The (dummy) block used to provide a scope chain for the type expression and the argument expressions.
    /// Writeable by derived classes to allow for delayed initialization.
    /// </summary>
    protected BlockStatement/*?*/ containingBlock;

    /// <summary>
    /// Calls the visitor.Visit(SourceCustomAttribute) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// 
    /// </summary>
    protected virtual IMethodReference GetConstructor() {
      IEnumerable<IMethodDefinition> constructors = this.GetConstructors();
      return this.ContainingBlock.Helper.ResolveOverload(constructors, this.ConstructorArguments);
      //TODO: error if resolution fails
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    protected virtual IEnumerable<IMethodDefinition> GetConstructors() {
      foreach (ITypeDefinitionMember member in this.Type.ResolvedType.GetMembersNamed(this.ContainingBlock.NameTable.Ctor, false)) {
        IMethodDefinition/*?*/ meth = member as IMethodDefinition;
        if (meth != null && meth.IsSpecialName) yield return meth;
      }
    }

    /// <summary>
    /// Obtains the values of AllowMultiple, Inherited and ValidOn by looking at the arguments of the AttributeUsage custom attribute that is supposed to be
    /// attached to the type of this custom attribute.
    /// </summary>
    private void GetUsageInformation() {
      this.flags = 0x40000000; //Records that the attribute usage has already been determined.
      INameTable nameTable = this.ContainingBlock.Compilation.NameTable;
      ITypeDefinition attributeUsageAttribute = this.ContainingBlock.Compilation.PlatformType.SystemAttributeUsageAttribute.ResolvedType;
      foreach (ICustomAttribute attribute in this.Type.ResolvedType.Attributes) {
        if (attribute.Type.ResolvedType != attributeUsageAttribute) continue;
        bool firstArgument = true;
        foreach (IExpression expression in attribute.Arguments) {
          ICompileTimeConstant/*?*/ cc = expression as ICompileTimeConstant;
          if (firstArgument) {
            if (cc != null) {
              if (cc.Value is int) {
                //^ assume false; //not really, but we need to shut up a bogus message: Unboxing cast might fail
                this.flags |= (int)cc.Value;
              }//else TODO: error?
            }//else TODO: error?
          } else {
            INamedArgument/*?*/ namedArgument = expression as INamedArgument;
            if (namedArgument != null) {
              if (namedArgument.ArgumentName.UniqueKey == nameTable.AllowMultiple.UniqueKey) {
                cc = namedArgument.ArgumentValue as ICompileTimeConstant;
                if (cc != null && cc.Value is bool) {
                  if (((bool)cc.Value)) this.flags |= 0x20000000;
                }//else TODO: error?
              } else if (namedArgument.ArgumentName.UniqueKey == nameTable.Inherited.UniqueKey) {
                cc = namedArgument.ArgumentValue as ICompileTimeConstant;
                if (cc != null && cc.Value is bool) {
                  if (((bool)cc.Value)) this.flags |= 0x10000000;
                }//else TODO: error?
              }
            }//else TODO: error?
          }
        }
        break;
      }
      //TODO: what if attribute does not have usage attribute? Error?
    }

    /// <summary>
    /// Holds a bit (0x4000000) that indicates that the attribute usage attribute has already been inspected, a bit (0x20000000) that indicates that
    /// AllowMultiple is true, a bit (0x10000000) that indicates that Inherited is true and stores the value of ValidOn in the lower order bits.
    /// </summary>
    int flags;

    /// <summary>
    /// Specifies whether this attribute applies to derived types and/or overridden methods. This information is obtained from an attribute on the attribute type definition.
    /// </summary>
    public bool Inherited {
      get {
        if (this.flags == 0) this.GetUsageInformation();
        return (this.flags & 0x10000000) != 0;
      }
    }

    /// <summary>
    /// Specifies the symbol table elements on which it is valid to apply this attribute. This information is obtained from an attribute on the attribute type definition.
    /// </summary>
    public AttributeTargets ValidOn {
      get {
        if (this.flags == 0) this.GetUsageInformation();
        return ((AttributeTargets)this.flags) & AttributeTargets.All;
      }
    }
    /// <summary>
    /// Makes a copy of this source custom attribute, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public virtual SourceCustomAttribute MakeShallowCopyFor(BlockStatement containingBlock)
      //^ requires this.ContainingBlock.GetType() == containingBlock.GetType();
      //^ ensures result.GetType() == this.GetType();
      //^ ensures result.ContainingBlock == containingBlock;
    {
      if (containingBlock == this.ContainingBlock) return this;
      return new SourceCustomAttribute(containingBlock, this);
    }

    /// <summary>
    /// The kind of metadata object targeted by this attribute. Used when the source context is ambiguous. For example a return type attribute in C#.
    /// Zero if not specified in source. Usually at most one target is specified.
    /// </summary>
    public AttributeTargets Targets {
      get { return this.targets; }
    }
    readonly AttributeTargets targets;

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a custom attribute before constructing the container for the attribute.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public virtual void SetContainingExpression(Expression containingExpression) {
      this.containingBlock = containingExpression.ContainingBlock;
      foreach (Expression arg in this.arguments) arg.SetContainingExpression(containingExpression);
      this.type.SetContainingExpression(containingExpression);
    }

    /// <summary>
    /// The type of the attribute. For example System.AttributeUsageAttribute.
    /// </summary>
    public AttributeTypeExpression Type {
      get { return this.type; }
    }
    readonly AttributeTypeExpression type;

  }

}
