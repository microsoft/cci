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
using System.Diagnostics;
using Microsoft.Cci.Contracts;
using Microsoft.Cci.Immutable;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci.Ast {

  /// <summary>
  /// A bit field is a member that represents a bit aligned variable associated with an object or class.
  /// </summary>
  public class BitFieldDeclaration : FieldDeclaration {

    /// <summary>
    /// Allocates a member that represents a variable associated with an object or class.
    /// </summary>
    /// <param name="sourceAttributes">Custom attributes that are explicitly specified in source. May be null.
    /// Some of these may not end up in persisted metadata.
    /// For example in C# a custom attribute is used to specify IFieldDefinition.IsNotSerialized. Such a custom attribute is deleted by the compiler.</param>
    /// <param name="bitLengthExpression">An expression that is expected to result in the number of bits that form part of the value of the field.</param>
    /// <param name="flags">A set of flags that specify the value of boolean properties of the field, such as IsStatic.</param>
    /// <param name="visibility">Indicates if the member is public or confined to its containing type, derived types and/or declaring assembly.</param>
    /// <param name="type">An expression that denote the type of value that is stored in this field.</param>
    /// <param name="name">The name of the member. </param>
    /// <param name="initializer">An expression that evaluates to the initial value of this field. May be null.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public BitFieldDeclaration(List<SourceCustomAttribute>/*?*/ sourceAttributes, Expression bitLengthExpression,
      FieldDeclaration.Flags flags, TypeMemberVisibility visibility,
      TypeExpression type, NameDeclaration name, Expression/*?*/ initializer, ISourceLocation sourceLocation)
      : base(sourceAttributes, flags, visibility, type, name, initializer, sourceLocation) {
      this.bitLengthExpression = bitLengthExpression;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing type.
    /// </summary>
    /// <param name="containingTypeDeclaration">The containing type of the copied member. This should be different from the containing type of the template member.</param>
    /// <param name="template">The type member to copy.</param>
    protected BitFieldDeclaration(TypeDeclaration containingTypeDeclaration, BitFieldDeclaration template)
      : base(containingTypeDeclaration, template)
      //^ ensures this.containingTypeDeclaration == containingTypeDeclaration;
    {
      this.bitLengthExpression = template.BitLengthExpression.MakeCopyFor(containingTypeDeclaration.DummyBlock);
    }

    /// <summary>
    /// The number of bits that form part of the value of the field. 
    /// </summary>
    public override uint BitLength {
      get {
        if (this.bitLength == null) {
          uint blen = 0;
          Expression blexpr = this.Helper.ImplicitConversion(this.BitLengthExpression, this.PlatformType.SystemUInt32.ResolvedType);
          object/*?*/ val = blexpr.Value;
          if (val is uint) {
            blen = (uint)val;
            //TODO: complain if len > Sizeof(this.Type.ResolvedType)*8
          }
          this.bitLength = blen;
        }
        return (uint)this.bitLength;
      }
    }
    uint? bitLength;

    /// <summary>
    /// An expression that is expected to result in a positive integer compile time constant.
    /// </summary>
    public Expression BitLengthExpression {
      get { return this.bitLengthExpression; }
    }
    Expression bitLengthExpression;

    /// <summary>
    /// The field is aligned on a bit boundary and uses only the BitLength number of least significant bits of the representation of a Type value.
    /// </summary>
    public override bool IsBitField {
      get {
        return true;
      }
    }

    /// <summary>
    /// Makes a shallow copy of this member that can be added to the member list of the given target type declaration.
    /// The shallow copy may share child objects with this instance, but should never expose such child objects except through
    /// wrappers (or shallow copies made on demand). If this instance is already a member of the target type declaration it
    /// returns itself.
    /// </summary>
    //^ [MustOverride, Pure]
    public override TypeDeclarationMember MakeShallowCopyFor(TypeDeclaration targetTypeDeclaration) {
      if (targetTypeDeclaration == this.ContainingTypeDeclaration) return this;
      return new BitFieldDeclaration(targetTypeDeclaration, this);
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a type member before constructing the containing type declaration.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingTypeDeclaration(TypeDeclaration containingTypeDeclaration, bool recurse) {
      base.SetContainingTypeDeclaration(containingTypeDeclaration, recurse);
      if (recurse) {
        DummyExpression dummyExpression = new DummyExpression(containingTypeDeclaration.DummyBlock, SourceDummy.SourceLocation);
        this.BitLengthExpression.SetContainingExpression(dummyExpression);
      }
    }

  }

  /// <summary>
  /// Implemented by type declaration members (such as nested types) that can be aggegrated with other declarations into a single definition.
  /// </summary>
  public interface IAggregatableTypeDeclarationMember : ITypeDeclarationMember, IContainerMember<TypeDeclaration> {
    /// <summary>
    /// The single definition that is associated with this declaration and possibly other declarations too.
    /// </summary>
    ITypeDefinitionMember AggregatedMember { get; }
  }

  /// <summary>
  /// A constant field that represents an enumeration values.
  /// </summary>
  public class EnumMember : FieldDeclaration {

    /// <summary>
    /// Allocates a constant field that represents an enumeration values.
    /// </summary>
    /// <param name="sourceAttributes">Custom attributes that are explicitly specified in source. May be null. Some of these may not end up in persisted metadata.
    /// For example in C# a custom attribute is used to specify IFieldDefinition.IsNotSerialized. Such a custom attribute is deleted by the compiler.</param>
    /// <param name="type">An expression that denote the type of value that is stored in this field.</param>
    /// <param name="name">The name of the value. </param>
    /// <param name="initializer">An expression that evaluates to the initial value of this field. May be null.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public EnumMember(List<SourceCustomAttribute>/*?*/ sourceAttributes, TypeExpression type, NameDeclaration name,
      Expression/*?*/ initializer, ISourceLocation sourceLocation)
      : base(sourceAttributes, FieldDeclaration.Flags.Constant|FieldDeclaration.Flags.Static, TypeMemberVisibility.Public, type, name, initializer, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing type.
    /// </summary>
    /// <param name="containingTypeDeclaration">The containing type of the copied member. This should be different from the containing type of the template member.</param>
    /// <param name="template">The type member to copy.</param>
    protected EnumMember(TypeDeclaration containingTypeDeclaration, EnumMember template)
      : base(containingTypeDeclaration, template)
      //^ ensures this.containingTypeDeclaration == containingTypeDeclaration;
    {
    }

    /// <summary>
    /// An expression that converts the value of this.Initializer to this.Type.ResolvedType using an implicit conversion.
    /// If this.Intializer is null, this.ConvertedInitializer is also null.
    /// </summary>
    public override Expression/*?*/ ConvertedInitializer {
      get
        //^ ensures result == null <==> this.Initializer == null;
      {
        if (this.Initializer == null) return null;
        return this.Helper.ExplicitConversion(this.Initializer, this.Type.ResolvedType);
      }
    }

    /// <summary>
    /// An expression that evaluates to the value of this enumeration member.
    /// </summary>
    public override Expression/*?*/ Initializer {
      get
        //^ ensures result != null;
      {
        if (this.initializer == null) {
          EnumMember/*?*/ previousMember = this.PreviousMember;
          if (previousMember == null) {
            this.initializer = new CompileTimeConstant((byte)0, this.SourceLocation);
            //TODO: Boogie it seems that both boolean and byte are abbreviated to b, which causes the above to try and load byte 0 into a boolean local
          } else {
            //^ assert previousMember.Initializer != null;
            this.initializer = new Addition(previousMember.Initializer, new CompileTimeConstant((byte)1, this.SourceLocation), this.SourceLocation);
          }
          this.initializer.SetContainingExpression(new DummyExpression(this.ContainingTypeDeclaration.DummyBlock, SourceDummy.SourceLocation));
        }
        return this.initializer;
      }
    }

    /// <summary>
    /// Makes a shallow copy of this member that can be added to the member list of the given target type declaration.
    /// The shallow copy may share child objects with this instance, but should never expose such child objects except through
    /// wrappers (or shallow copies made on demand). If this instance is already a member of the target type declaration it
    /// returns itself.
    /// </summary>
    //^ [MustOverride, Pure]
    public override TypeDeclarationMember MakeShallowCopyFor(TypeDeclaration targetTypeDeclaration) {
      if (targetTypeDeclaration == this.ContainingTypeDeclaration) return this;
      return new EnumMember(targetTypeDeclaration, this);
    }

    /// <summary>
    /// The previous enumeration member. Null if this member is the first in the enumeration.
    /// Used to work out the default value for the member.
    /// </summary>
    EnumMember/*?*/ PreviousMember {
      get {
        EnumMember/*?*/ previousMember = null;
        foreach (ITypeDeclarationMember member in this.ContainingTypeDeclaration.TypeDeclarationMembers) {
          if (member == this) break;
          if (!(member is EnumMember)) continue;
          previousMember = (EnumMember)member;
        }
        return previousMember;
      }
    }

  }

  /// <summary>
  /// An event is a member that enables an object or class to provide notifications. Clients can attach executable code for events by supplying event handlers.
  /// </summary>
  public class EventDeclaration : TypeDeclarationMember {

    /// <summary>
    /// Allocates an event member that enables an object or class to provide notifications. Clients can attach executable code for events by supplying event handlers.
    /// This overload provides for events that have explcitly specified methods for adding and removing handlers.
    /// </summary>
    /// <param name="sourceAttributes">Custom attributes that are explicitly specified in source. May be null.
    /// Some of these may not end up in persisted metadata.</param>
    /// <param name="flags">A set of flags that specify the value of boolean properties of the member, such as IsNew.</param>
    /// <param name="visibility">Indicates if the event is public or confined to its containing type, derived types and/or declaring assembly.</param>
    /// <param name="type">An expression that denote the (delegate) type of the handlers that will handle the event.</param>
    /// <param name="implementedInterfaces">A list of interfaces whose corresponding abstract events are implemented by this event.</param>
    /// <param name="name">The name of the member. </param>
    /// <param name="adderAttributes">Custom attributes specified directly on the adder method.</param>
    /// <param name="adderBody">The body of the adder method. May be null for field like events.</param>
    /// <param name="removerAttributes">Custom attributes specified directly on the remover method.</param>
    /// <param name="removerBody">The body of the remover method. May be null for field like events.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public EventDeclaration(List<SourceCustomAttribute>/*?*/ sourceAttributes,
      Flags flags, TypeMemberVisibility visibility, TypeExpression type, List<TypeExpression>/*?*/ implementedInterfaces, NameDeclaration name,
      List<SourceCustomAttribute>/*?*/ adderAttributes, BlockStatement/*?*/ adderBody, List<SourceCustomAttribute>/*?*/ removerAttributes, BlockStatement/*?*/ removerBody, ISourceLocation sourceLocation)
      : base(sourceAttributes, (TypeDeclarationMember.Flags)flags, visibility, name, sourceLocation) {
      //^ assume this.caller == null;
      this.adderAttributes = adderAttributes;
      this.adderBody = adderBody;
      this.implementedInterfaces = implementedInterfaces;
      this.removerAttributes = removerAttributes;
      this.removerBody = removerBody;
      this.type = type;
    }

    /// <summary>
    /// Allocates an event member that enables an object or class to provide notifications. Clients can attach executable code for events by supplying event handlers.
    /// This overload provides for field like events.
    /// </summary>
    /// <param name="sourceAttributes">Custom attributes that are explicitly specified in source. May be null.
    /// Some of these may not end up in persisted metadata.</param>
    /// <param name="flags">A set of flags that specify the value of boolean properties of the member, such as IsNew.</param>
    /// <param name="visibility">Indicates if the event is public or confined to its containing type, derived types and/or declaring assembly.</param>
    /// <param name="type">An expression that denote the (delegate) type of the handlers that will handle the event.</param>
    /// <param name="name">The name of the member. </param>
    /// <param name="initializer">An expression that evaluates to the initial handler for this event.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public EventDeclaration(List<SourceCustomAttribute>/*?*/ sourceAttributes,
      Flags flags, TypeMemberVisibility visibility, TypeExpression type, NameDeclaration name, Expression/*?*/ initializer, ISourceLocation sourceLocation)
      : base(sourceAttributes, (TypeDeclarationMember.Flags)flags, visibility, name, sourceLocation) {
      //^ assume this.adder == null;
      this.initializer = initializer;
      this.type = type;
    }

    /// <summary>
    /// 
    /// </summary>
    [Flags]
    public new enum Flags { //Must remain same as PropertyDeclaration.Flags and be a prefix of MethodDeclaration.Flags
      /// <summary>
      /// 
      /// </summary>
      New=int.MinValue,
      /// <summary>
      /// 
      /// </summary>
      Unsafe=(New>>1)&int.MaxValue,

      /// <summary>
      /// 
      /// </summary>
      Abstract=Unsafe>>1,
      /// <summary>
      /// 
      /// </summary>
      External=Abstract>>1,
      /// <summary>
      /// 
      /// </summary>
      Override=External>>1,
      /// <summary>
      /// 
      /// </summary>
      Sealed=Override>>1,
      /// <summary>
      /// 
      /// </summary>
      Static=Sealed>>1,
      /// <summary>
      /// 
      /// </summary>
      Virtual=Static>>1
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing type.
    /// </summary>
    /// <param name="containingTypeDeclaration">The containing type of the copied member. This should be different from the containing type of the template member.</param>
    /// <param name="template">The type member to copy.</param>
    protected EventDeclaration(TypeDeclaration containingTypeDeclaration, EventDeclaration template)
      : base(containingTypeDeclaration, template)
      //^ requires containingTypeDeclaration.GetType() == template.ContainingTypeDeclaration.GetType();
      //^ ensures this.containingTypeDeclaration == containingTypeDeclaration;
    {
      if (template.AdderBody != null) {
        this.adderBody = (BlockStatement)template.AdderBody.MakeCopyFor(containingTypeDeclaration.DummyBlock);
        if (template.AdderAttributes != null)
          this.adderAttributes = new List<SourceCustomAttribute>(template.AdderAttributes);
      } else if (template.Adder != null)
        this.adder = (MethodDeclaration)template.Adder.MakeShallowCopyFor(containingTypeDeclaration);
      if (template.Caller != null)
        this.caller = (MethodDeclaration)template.Caller.MakeShallowCopyFor(containingTypeDeclaration);
      if (template.ImplementedInterfaces != null)
        this.implementedInterfaces = new List<TypeExpression>(template.ImplementedInterfaces);
      if (template.RemoverBody != null) {
        this.removerBody = (BlockStatement)template.RemoverBody.MakeCopyFor(containingTypeDeclaration.DummyBlock);
        if (template.RemoverAttributes != null)
          this.removerAttributes = new List<SourceCustomAttribute>(template.RemoverAttributes);
      } else if (template.Remover != null)
        this.remover = (MethodDeclaration)template.Remover.MakeShallowCopyFor(containingTypeDeclaration);
      this.type = (TypeExpression)template.Type.MakeCopyFor(containingTypeDeclaration.DummyBlock);
    }

    /// <summary>
    /// Calls the visitor.Visit(EventDeclaration) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Custom attributes specified directly on the adder method.
    /// </summary>
    public virtual IEnumerable<SourceCustomAttribute> AdderAttributes {
      get {
        List<SourceCustomAttribute> adderAttributes;
        if (this.adderAttributes == null)
          yield break;
        else
          adderAttributes = this.adderAttributes;
        for (int i = 0, n = adderAttributes.Count; i < n; i++) {
          yield return adderAttributes[i] = adderAttributes[i].MakeShallowCopyFor(this.ContainingTypeDeclaration.DummyBlock);
        }
      }
    }
    readonly List<SourceCustomAttribute>/*?*/ adderAttributes;

    /// <summary>
    /// The method that is used to attach handlers to this event.
    /// </summary>
    public MethodDeclaration Adder {
      get
        //^ ensures result.ContainingTypeDeclaration == this.ContainingTypeDeclaration;
      {
        if (this.adder != null) return this.adder;
        lock (GlobalLock.LockingObject) {
          if (this.adder != null) return this.adder;
          ISourceLocation sourceLocation = this.Name.SourceLocation;
          TypeExpression voidExpr = TypeExpression.For(this.PlatformType.SystemVoid.ResolvedType);
          NameDeclaration name = new NameDeclaration(this.NameTable.GetNameFor("add_"+this.Name.Value), sourceLocation);
          List<ParameterDeclaration> parameters = new List<ParameterDeclaration>(1);
          NameDeclaration pname = new NameDeclaration(this.NameTable.value, sourceLocation);
          parameters.Add(new ParameterDeclaration(null, this.Type, pname, null, 0, false, false, false, false, sourceLocation));
          BlockStatement/*?*/ body = this.AdderBody;
          if (body == null && !this.IsAbstract && !this.IsExternal) body = this.DefaultAdderBody();
          //TODO: worry about attributes with method target defined on event
          MethodDeclaration.Flags flags = (MethodDeclaration.Flags)(this.flags&(int)~TypeMemberVisibility.Mask);
          flags |= MethodDeclaration.Flags.SpecialName|MethodDeclaration.Flags.Synchronized;
          this.adder = new MethodDeclaration(this.adderAttributes, flags, this.Visibility, voidExpr, this.implementedInterfaces, name, null, parameters, null, body, this.SourceLocation);
          this.adder.SetContainingTypeDeclaration(this.ContainingTypeDeclaration, true);
        }
        return this.adder;
      }
    }
    MethodDeclaration/*?*/ adder;
    //^ invariant this.adder == null || this.adder.ContainingTypeDeclaration == this.ContainingTypeDeclaration;

    /// <summary>
    /// The body of the adder method. May be null for field like events.
    /// </summary>
    public BlockStatement/*?*/ AdderBody {
      get { return this.adderBody; }
    }
    BlockStatement/*?*/ adderBody;

    /// <summary>
    /// The method that is used to call the event handlers when the event occurs. May be null.
    /// </summary>
    public MethodDeclaration/*?*/ Caller {
      get
        //^ ensures result == null || result.ContainingTypeDeclaration == this.ContainingTypeDeclaration;
      {
        return this.caller;
      }
    }
    MethodDeclaration/*?*/ caller;
    //^ invariant caller == null || caller.ContainingTypeDeclaration == this.ContainingTypeDeclaration;

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the event or a constituent part of the event.
    /// Do not call this method directly, but evaluate the HasErrors property. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return false;
    }

    /// <summary>
    /// A body for use by the adder method of a field like event.
    /// </summary>
    private BlockStatement DefaultAdderBody() {
      List<Statement> statements = new List<Statement>();
      //TODO: add code to implement adder
      BlockStatement body = new BlockStatement(statements, this.Name.SourceLocation);
      return body;
    }

    /// <summary>
    /// A body for use by the remover method of a field like event.
    /// </summary>
    private BlockStatement DefaultRemoverBody() {
      List<Statement> statements = new List<Statement>();
      //TODO: add code to implement remover
      BlockStatement body = new BlockStatement(statements, this.Name.SourceLocation);
      return body;
    }

    /// <summary>
    /// The symbol table object that represents the metadata for this event.
    /// </summary>
    public EventDefinition EventDefinition {
      get {
        if (this.eventDefinition == null)
          this.eventDefinition = new EventDefinition(this);
        return this.eventDefinition;
      }
    }
    EventDefinition/*?*/ eventDefinition;

    /// <summary>
    /// A list of interfaces whose corresponding abstract events are implemented by this event.
    /// </summary>
    public IEnumerable<TypeExpression> ImplementedInterfaces {
      get {
        List<TypeExpression> implementedInterfaces;
        if (this.implementedInterfaces == null)
          yield break;
        else
          implementedInterfaces = this.implementedInterfaces;
        for (int i = 0, n = implementedInterfaces.Count; i < n; i++) {
          yield return implementedInterfaces[i] = (TypeExpression)implementedInterfaces[i].MakeCopyFor(this.ContainingTypeDeclaration.DummyBlock);
        }
      }
    }
    readonly List<TypeExpression>/*?*/ implementedInterfaces;

    /// <summary>
    /// An expression that evaluates to the initial handler for this event.
    /// </summary>
    public Expression/*?*/ Initializer {
      get { return this.initializer; }
    }
    readonly Expression/*?*/ initializer;

    /// <summary>
    /// True if the methods to add or remove handlers to or from the event are abstract.
    /// </summary>
    public bool IsAbstract {
      get { return (this.flags & (int)Flags.Abstract) != 0; }
    }

    /// <summary>
    /// True if the methods to add or remove handlers to or from the event are defined externally.
    /// </summary>
    public bool IsExternal {
      get { return (this.flags & (int)Flags.External) != 0; }
    }

    /// <summary>
    /// True if the methods to add or remove handlers to or from the event are overriding base class methods.
    /// </summary>
    public bool IsOverride {
      get { return (this.flags & (int)Flags.Override) != 0; }
    }

    /// <summary>
    /// True if the event gets special treatment from the runtime.
    /// </summary>
    public virtual bool IsRuntimeSpecial {
      get { return false; }
    }

    /// <summary>
    /// True if the methods to add or remove handlers to or from the event are sealed.
    /// </summary>
    public bool IsSealed {
      get { return (this.flags & (int)Flags.Sealed) != 0; }
    }

    /// <summary>
    /// This event is special in some way, as specified by the name.
    /// </summary>
    public virtual bool IsSpecialName {
      get { return false; }
    }

    /// <summary>
    /// True if the methods to add or remove handlers to or from the event are static.
    /// </summary>
    public bool IsStatic {
      get { return (this.flags & (int)Flags.Static) != 0; }
    }

    /// <summary>
    /// True if the methods to add or remove handlers to or from the event are virtual.
    /// </summary>
    public bool IsVirtual {
      get { return (this.flags & (int)Flags.Virtual) != 0; }
    }

    /// <summary>
    /// Makes a shallow copy of this member that can be added to the member list of the given target type declaration.
    /// The shallow copy may share child objects with this instance, but should never expose such child objects except through
    /// wrappers (or shallow copies made on demand). If this instance is already a member of the target type declaration it
    /// returns itself.
    /// </summary>
    //^ [MustOverride, Pure]
    public override TypeDeclarationMember MakeShallowCopyFor(TypeDeclaration targetTypeDeclaration) {
      if (this.ContainingTypeDeclaration == targetTypeDeclaration) return this;
      return new EventDeclaration(targetTypeDeclaration, this);
    }

    /// <summary>
    /// A list of methods that are associated with the event.
    /// </summary>
    public virtual IEnumerable<MethodDeclaration> Accessors {
      get {
        MethodDeclaration/*?*/ adder = this.Adder;
        if (adder != null) yield return adder;
        MethodDeclaration/*?*/ remover = this.Remover;
        if (remover != null) yield return remover;
        MethodDeclaration/*?*/ caller = this.Caller;
        if (caller != null) yield return caller;
      }
    }

    /// <summary>
    /// Custom attributes specified directly on the remover method.
    /// </summary>
    public virtual IEnumerable<SourceCustomAttribute> RemoverAttributes {
      get {
        List<SourceCustomAttribute> removerAttributes;
        if (this.removerAttributes == null)
          yield break;
        else
          removerAttributes = this.removerAttributes;
        for (int i = 0, n = removerAttributes.Count; i < n; i++) {
          yield return removerAttributes[i] = removerAttributes[i].MakeShallowCopyFor(this.ContainingTypeDeclaration.DummyBlock);
        }
      }
    }
    readonly List<SourceCustomAttribute>/*?*/ removerAttributes;

    /// <summary>
    /// The method that is used to detach handlers from this event.
    /// </summary>
    protected internal MethodDeclaration Remover {
      get
        //^ ensures result.ContainingTypeDeclaration == this.ContainingTypeDeclaration;
      {
        if (this.remover != null) return this.remover;
        lock (GlobalLock.LockingObject) {
          if (this.remover != null) return this.remover;
          ISourceLocation sourceLocation = this.Name.SourceLocation;
          TypeExpression voidExpr = TypeExpression.For(this.PlatformType.SystemVoid.ResolvedType);
          NameDeclaration name = new NameDeclaration(this.NameTable.GetNameFor("remove_"+this.Name.Value), sourceLocation);
          List<ParameterDeclaration> parameters = new List<ParameterDeclaration>(1);
          NameDeclaration pname = new NameDeclaration(this.NameTable.value, sourceLocation);
          parameters.Add(new ParameterDeclaration(null, this.Type, pname, null, 0, false, false, false, false, sourceLocation));
          BlockStatement/*?*/ body = this.RemoverBody;
          if (body == null && !this.IsAbstract && !this.IsExternal) body = this.DefaultRemoverBody();
          //TODO: worry about attributes with method target defined on event
          MethodDeclaration.Flags flags = (MethodDeclaration.Flags)(this.flags&(int)~TypeMemberVisibility.Mask);
          flags |= MethodDeclaration.Flags.SpecialName|MethodDeclaration.Flags.Synchronized;
          this.remover = new MethodDeclaration(this.removerAttributes, flags, this.Visibility, voidExpr, this.implementedInterfaces, name,
            null, parameters, null, body, this.SourceLocation);
          this.remover.SetContainingTypeDeclaration(this.ContainingTypeDeclaration, true);
        }
        return this.remover;
      }
    }
    MethodDeclaration/*?*/ remover;
    //^ invariant this.remover == null || this.remover.ContainingTypeDeclaration == this.ContainingTypeDeclaration;

    /// <summary>
    /// The body of the remover method. May be null for field like events.
    /// </summary>
    public BlockStatement/*?*/ RemoverBody {
      get {
        return this.removerBody;
      }
    }
    BlockStatement/*?*/ removerBody;

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a type member before constructing the containing type declaration.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingTypeDeclaration(TypeDeclaration containingTypeDeclaration, bool recurse)
      //^^ modifies this.*;
    {
      base.SetContainingTypeDeclaration(containingTypeDeclaration, recurse);
      //^ assert this.ContainingTypeDeclaration == containingTypeDeclaration;
      if (!recurse) return;
      BlockStatement containingBlock = containingTypeDeclaration.DummyBlock;
      DummyExpression containingExpression = new DummyExpression(containingBlock, SourceDummy.SourceLocation);
      if (this.adderAttributes != null)
        foreach (SourceCustomAttribute attribute in this.adderAttributes) attribute.SetContainingExpression(containingExpression);
      if (this.adderBody != null)
        this.adderBody.SetContainingBlock(containingBlock);
      if (this.implementedInterfaces != null)
        foreach (TypeExpression implementedInterface in this.implementedInterfaces) implementedInterface.SetContainingExpression(containingExpression);
      if (this.removerAttributes != null)
        foreach (SourceCustomAttribute attribute in this.removerAttributes) attribute.SetContainingExpression(containingExpression);
      if (this.removerBody != null)
        this.removerBody.SetContainingBlock(containingBlock);
      this.type.SetContainingExpression(containingExpression);
      //^ assume this.ContainingTypeDeclaration == containingTypeDeclaration;
    }

    /// <summary>
    /// The (delegate) type of the handlers that will handle the event.
    /// </summary>
    public TypeExpression Type {
      get { return this.type; }
    }
    readonly TypeExpression type;

    /// <summary>
    /// The symbol table object that represents the metadata for this member.
    /// </summary>
    public override TypeDefinitionMember TypeDefinitionMember {
      get { return this.EventDefinition; }
    }

  }

  /// <summary>
  /// A field is a member that represents a variable associated with an object or class.
  /// </summary>
  public class FieldDeclaration : TypeDeclarationMember {

    /// <summary>
    /// Allocates a member that represents a variable associated with an object or class.
    /// </summary>
    /// <param name="sourceAttributes">Custom attributes that are explicitly specified in source. May be null.
    /// Some of these may not end up in persisted metadata.
    /// For example in C# a custom attribute is used to specify IFieldDefinition.IsNotSerialized. Such a custom attribute is deleted by the compiler.</param>
    /// <param name="flags">A set of flags that specify the value of boolean properties of the field, such as IsStatic.</param>
    /// <param name="visibility">Indicates if the member is public or confined to its containing type, derived types and/or declaring assembly.</param>
    /// <param name="type">An expression that denote the type of value that is stored in this field.</param>
    /// <param name="name">The name of the member. </param>
    /// <param name="initializer">An expression that evaluates to the initial value of this field. May be null.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public FieldDeclaration(List<SourceCustomAttribute>/*?*/ sourceAttributes,
      FieldDeclaration.Flags flags, TypeMemberVisibility visibility,
      TypeExpression type, NameDeclaration name, Expression/*?*/ initializer, ISourceLocation sourceLocation)
      : base(sourceAttributes, (TypeDeclarationMember.Flags)flags, visibility, name, sourceLocation) {
      this.initializer = initializer;
      this.type = type;
      this.flags |= (int)flags;
    }

    /// <summary>
    /// 
    /// </summary>
    [Flags]
    public new enum Flags {
      /// <summary>
      /// 
      /// </summary>
      New=int.MinValue,
      /// <summary>
      /// 
      /// </summary>
      Unsafe=(New>>1)&int.MaxValue,

      /// <summary>
      /// 
      /// </summary>
      AutomaticEventHookup=Unsafe>>1,
      /// <summary>
      /// 
      /// </summary>
      Constant=AutomaticEventHookup>>1,
      /// <summary>
      /// 
      /// </summary>
      ReadOnly=Constant>>1,
      /// <summary>
      /// 
      /// </summary>
      RuntimeSpecial=ReadOnly>>1,
      /// <summary>
      /// 
      /// </summary>
      SpecialName=RuntimeSpecial>>1,
      /// <summary>
      /// 
      /// </summary>
      Static=SpecialName>>1,
      /// <summary>
      /// 
      /// </summary>
      Volatile=Static>>1
    }

    [Flags]
    private enum ExtendedFlags {
      CustomAttributesNotYetProcessed=Flags.Volatile >> 1,
      Mapped=CustomAttributesNotYetProcessed>>1,
      MarshalledExplicitly=Mapped>>1,
      NotSerialized=MarshalledExplicitly>>1
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing type.
    /// </summary>
    /// <param name="containingTypeDeclaration">The containing type of the copied member. This should be different from the containing type of the template member.</param>
    /// <param name="template">The type member to copy.</param>
    protected FieldDeclaration(TypeDeclaration containingTypeDeclaration, FieldDeclaration template)
      : base(containingTypeDeclaration, template)
      //^ ensures this.containingTypeDeclaration == containingTypeDeclaration;
    {
      if (template.Initializer != null)
        this.initializer = template.Initializer.MakeCopyFor(containingTypeDeclaration.DummyBlock);
      this.type = (TypeExpression)template.Type.MakeCopyFor(containingTypeDeclaration.DummyBlock);
    }

    /// <summary>
    /// Adds zero or more assignments statements to the giving collection. Executing these statements will initialize the field.
    /// </summary>
    internal protected virtual void AddInitializingAssignmentsTo(ICollection<Statement> statements) {
      if (this.Initializer == null || this.IsMapped) return;
      TargetExpression target = new TargetExpression(new BoundExpression(new DummyExpression(SourceDummy.SourceLocation), this.FieldDefinition));
      Assignment initializeField = new Assignment(target, this.Initializer, this.Initializer.SourceLocation);
      statements.Add(new ExpressionStatement(initializeField));
    }

    /// <summary>
    /// True if one or more of the events exposed by the type object stored in this field will be automatically handled by methods declared
    /// in this parent type of this field. In VB this corresponds to the WithEvents modifier.
    /// </summary>
    public bool AutomaticEventHookup {
      get { return (this.flags & (int)Flags.AutomaticEventHookup) != 0; }
    }

    /// <summary>
    /// The number of least significant bits that form part of the value of the field.
    /// </summary>
    public virtual uint BitLength {
      get { return TypeHelper.SizeOfType(this.Type.ResolvedType)*8; }
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the field or a constituent part of the field.
    /// Do not call this method directly, but evaluate the HasErrors property. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      bool result = false;
      if (this.Initializer != null)
        if (this.Initializer.HasErrors)
          result = true;
        else if (this.ConvertedInitializer != null && this.ConvertedInitializer.HasErrors) {
          this.Helper.ReportFailedImplicitConversion(this.Initializer, this.Type.ResolvedType);
          result = true;
        }
      // TODO ... more?
      return result;
    }

    /// <summary>
    /// An expression that converts the value of this.Initializer to this.Type.ResolvedType using an implicit conversion.
    /// If this.Intializer is null, this.ConvertedInitializer is also null.
    /// </summary>
    public virtual Expression/*?*/ ConvertedInitializer {
      get
        //^ ensures result == null <==> this.Initializer == null;
      {
        if (this.convertedInitializer == null)
          this.convertedInitializer = this.ConvertInitializer();
        return this.convertedInitializer;
      }
    }
    Expression/*?*/ convertedInitializer;

    /// <summary>
    /// Converts this.Initializer to this.Type.ResolvedType.
    /// </summary>
    protected virtual Expression/*?*/ ConvertInitializer()
      //^ ensures result == null <==> this.Initializer == null;
    {
      if (this.Initializer == null) return null;
      return this.Helper.ImplicitConversionInAssignmentContext(this.Initializer, this.Type.ResolvedType);
    }

    /// <summary>
    /// 
    /// </summary>
    protected internal virtual IEnumerable<ICustomModifier> CustomModifiers {
      get { return Enumerable<ICustomModifier>.Empty; }
    }

    /// <summary>
    /// The symbol table object that represents the metadata for this event.
    /// </summary>
    public FieldDefinition FieldDefinition {
      get {
        if (this.fieldDefinition == null) {
          FieldDefinition field = new FieldDefinition(this);
          lock (this) {
            if (this.fieldDefinition == null) this.fieldDefinition = field;
          }
        }
        return this.fieldDefinition;
      }
    }
    FieldDefinition/*?*/ fieldDefinition;

    /// <summary>
    /// The compile time value of the field. This value should be used directly in IL, rather than a reference to the field.
    /// If the field does not have a valid compile time value, Dummy.Constant is returned.
    /// </summary>
    public CompileTimeConstant CompileTimeValue {
      get {
        if (this.compileTimeValue == null)
          this.compileTimeValue = this.GetCompileTimeValue();
        return this.compileTimeValue;
      }
    }
    //^ [Once]
    CompileTimeConstant/*?*/ compileTimeValue;

    /// <summary>
    /// Calls the visitor.Visit(FieldDeclaration) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Information of the location where this field is mapped to
    /// </summary>
    public ISectionBlock FieldMapping {
      get
        //^^ requires this.IsMapped;
      {
        if (this.Initializer == null) return Dummy.SectionBlock;
        byte[]/*?*/ value = this.GetMappedData();
        if (value == null) return Dummy.SectionBlock;
        return new StaticDataSectionBlock(0, value); //TODO: get offset from compilation
      }
    }

    /// <summary>
    /// Returns the value of the intializer expression, provided that the expression
    /// has been provided, can be converted to the type of the field and has a value at compile time.
    /// Otherwise returns an instance of DummyConstant.
    /// </summary>
    protected virtual CompileTimeConstant GetCompileTimeValue() {
      CompileTimeConstant/*?*/ result = null;
      Expression/*?*/ convertedInitializer = this.ConvertedInitializer;
      if (convertedInitializer != null) {
        result = convertedInitializer as CompileTimeConstant;
        if (result == null) {
          object/*?*/ value = convertedInitializer.Value;
          if (value != null) {
            CompileTimeConstant ctc = new CompileTimeConstant(value, convertedInitializer.SourceLocation);
            ctc.UnfoldedExpression = convertedInitializer;
            ctc.SetContainingExpression(convertedInitializer);
            result = ctc;
          }
        }
      }
      if (result == null) {
        ISourceLocation sourceLocation = SourceDummy.SourceLocation;
        if (this.Initializer != null) sourceLocation = this.Initializer.SourceLocation;
        result = new DummyConstant(sourceLocation);
      }
      return result;
    }

    /// <summary>
    /// Returns a byte array representing the part of the process image to which this field will be mapped. Can be null.
    /// </summary>
    protected virtual byte[]/*?*/ GetMappedData()
      //^ requires this.IsMapped;
    {
      if (this.ConvertedInitializer == null) return null;
      return this.ConvertedInitializer.Value as byte[];
    }

    /// <summary>
    /// An expression that evaluates to the initial value of this field. May be null.
    /// </summary>
    public virtual Expression/*?*/ Initializer {
      get { return this.initializer; }
    }
    /// <summary>
    /// An expression that evaluates to the initial value of this field. May be null.
    /// </summary>
    protected Expression/*?*/ initializer;

    /// <summary>
    /// The field is aligned on a bit boundary and uses only the BitLength number of least significant bits of the representation of a Type value.
    /// </summary>
    public virtual bool IsBitField {
      get { return false; }
    }

    /// <summary>
    /// This field is a compile-time constant. The field has no runtime location and cannot be directly addressed from IL.
    /// </summary>
    public virtual bool IsCompileTimeConstant {
      get {
        return (this.flags & (int)Flags.Constant) != 0;
      }
    }

    /// <summary>
    /// This field is mapped to an explicitly initialized (static) memory location.
    /// </summary>
    public virtual bool IsMapped {
      get {
        if (!this.IsStatic) return false; //TODO: Boogie: this if statement should establish the post condition in all cases
        if ((this.flags & (int)ExtendedFlags.CustomAttributesNotYetProcessed) == 0)
          return (this.flags & (int)ExtendedFlags.Mapped) != 0;
        this.flags &= ~(int)ExtendedFlags.CustomAttributesNotYetProcessed;
        //TODO: look through custom attributes of field definition
        return false;
      }
    }

    /// <summary>
    /// This field has associated field marshalling information.
    /// </summary>
    public bool IsMarshalledExplicitly {
      get {
        if ((this.flags & (int)ExtendedFlags.CustomAttributesNotYetProcessed) == 0)
          return (this.flags & (int)ExtendedFlags.MarshalledExplicitly) != 0;
        this.flags &= ~(int)ExtendedFlags.CustomAttributesNotYetProcessed;
        //TODO: look through custom attributes of field definition
        this.marshallingInformation = null;
        return false;
      }
    }

    /// <summary>
    /// This field has custom modifiers.
    /// </summary>
    internal protected virtual bool IsModified {
      get { return false; } //TODO: compute this. For example volatile fields have modifiers.
    }

    /// <summary>
    /// The field does not have to be serialized when its containing instance is serialized.
    /// </summary>
    public bool IsNotSerialized {
      get {
        if ((this.flags & (int)ExtendedFlags.CustomAttributesNotYetProcessed) == 0)
          return (this.flags & (int)ExtendedFlags.NotSerialized) != 0;
        this.flags &= ~(int)ExtendedFlags.CustomAttributesNotYetProcessed;
        //TODO: look through custom attributes of field definition
        this.marshallingInformation = null;
        return false;
      }
    }

    /// <summary>
    /// This field is read only. It can only be assigned to in a constructor.
    /// </summary>
    public bool IsReadOnly {
      get { return (this.flags & (int)Flags.ReadOnly) != 0; }
    }

    /// <summary>
    /// True if the field gets special treatment from the runtime.
    /// </summary>
    public virtual bool IsRuntimeSpecial {
      get { return (this.flags & (int)Flags.RuntimeSpecial) != 0; }
    }

    /// <summary>
    /// This field is special in some way, as specified by the name.
    /// </summary>
    public virtual bool IsSpecialName {
      get { return (this.flags & (int)Flags.SpecialName) != 0; }
    }

    /// <summary>
    /// This field is static (shared by all instances of its declaring type).
    /// </summary>
    public bool IsStatic {
      get { return (this.flags & (int)Flags.Static) != 0; }
    }

    /// <summary>
    /// The field value is never cached, but written and read directly from memory.
    /// </summary>
    public bool IsVolatile {
      get { return (this.flags & (int)Flags.Volatile) != 0; }
    }

    /// <summary>
    /// Makes a shallow copy of this member that can be added to the member list of the given target type declaration.
    /// The shallow copy may share child objects with this instance, but should never expose such child objects except through
    /// wrappers (or shallow copies made on demand). If this instance is already a member of the target type declaration it
    /// returns itself.
    /// </summary>
    //^ [MustOverride, Pure]
    public override TypeDeclarationMember MakeShallowCopyFor(TypeDeclaration targetTypeDeclaration) {
      if (targetTypeDeclaration == this.ContainingTypeDeclaration) return this;
      return new FieldDeclaration(targetTypeDeclaration, this);
    }

    /// <summary>
    /// Specifies how this field is marshalled when it is accessed from unmanaged code.
    /// </summary>
    public MarshallingInformation MarshallingInformation {
      get {
        //^ assume this.marshallingInformation != null;
        return this.marshallingInformation;
      }
    }
    MarshallingInformation/*?*/ marshallingInformation;

    /// <summary>
    /// Offset of the field.
    /// </summary>
    public virtual uint Offset {
      get {
        TypeDeclaration td = this.containingTypeDeclaration as TypeDeclaration;
        if (td != null) return td.GetFieldOffset(this);
        return 0; //TODO: look for custom attribute that specifies offset
      }
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a type member before constructing the containing type declaration.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingTypeDeclaration(TypeDeclaration containingTypeDeclaration, bool recurse)
      //^^ modifies this.*;
    {
      base.SetContainingTypeDeclaration(containingTypeDeclaration, recurse);
      //^ assert this.ContainingTypeDeclaration == containingTypeDeclaration;
      if (!recurse) return;
      DummyExpression containingExpression = new DummyExpression(containingTypeDeclaration.DummyBlock, SourceDummy.SourceLocation);
      this.type.SetContainingExpression(containingExpression);
      if (this.initializer != null) this.initializer.SetContainingExpression(containingExpression);
      //^ assume this.ContainingTypeDeclaration == containingTypeDeclaration;
    }

    /// <summary>
    /// An expression that denote the type of value that is stored in this field.
    /// </summary>
    public TypeExpression Type {
      get { return this.type; }
    }
    readonly TypeExpression type;

    /// <summary>
    /// The symbol table object that represents the metadata for this member.
    /// </summary>
    public override TypeDefinitionMember TypeDefinitionMember {
      get { return this.FieldDefinition; }
    }

  }

  /// <summary>
  /// Corresponds to a source construct that declares a type parameter for a generic method.
  /// </summary>
  public class GenericMethodParameterDeclaration : GenericParameterDeclaration {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sourceAttributes"></param>
    /// <param name="name"></param>
    /// <param name="index"></param>
    /// <param name="constraints"></param>
    /// <param name="variance"></param>
    /// <param name="mustBeReferenceType"></param>
    /// <param name="mustBeValueType"></param>
    /// <param name="mustHaveDefaultConstructor"></param>
    /// <param name="sourceLocation"></param>
    public GenericMethodParameterDeclaration(List<SourceCustomAttribute>/*?*/ sourceAttributes, NameDeclaration name,
      ushort index, List<TypeExpression> constraints, TypeParameterVariance variance, bool mustBeReferenceType, bool mustBeValueType, bool mustHaveDefaultConstructor, ISourceLocation sourceLocation)
      : base(sourceAttributes, name, index, constraints, variance, mustBeReferenceType, mustBeValueType, mustHaveDefaultConstructor, sourceLocation)
      //^ requires !mustBeReferenceType || !mustBeValueType;
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="declaringMethod"></param>
    /// <param name="template"></param>
    protected GenericMethodParameterDeclaration(MethodDeclaration declaringMethod, GenericMethodParameterDeclaration template)
      : base(declaringMethod.DummyBlock, template)
      //^ requires declaringMethod.IsGeneric;
    {
      this.declaringMethod = declaringMethod;
    }

    /// <summary>
    /// Calls the visitor.Visit(GenericMethodParameterDeclaration) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The generic method that declares this type parameter.
    /// </summary>
    public MethodDeclaration DeclaringMethod {
      get
        //^ ensures result.IsGeneric;
      {
        //^ assume this.declaringMethod != null;
        return this.declaringMethod;
      }
    }
    //^ [SpecPublic]
    MethodDeclaration/*?*/ declaringMethod;
    //^ invariant this.declaringMethod == null || this.declaringMethod.IsGeneric;

    /// <summary>
    /// The symbol table entity that corresponds to this source construct.
    /// </summary>
    public GenericMethodParameter GenericMethodParameterDefinition {
      get {
        if (this.genericMethodParameterDefinition == null) {
          MethodDefinition declaringMethod = this.DeclaringMethod.MethodDefinition;
          //^ assume declaringMethod.IsGeneric;
          this.genericMethodParameterDefinition = new GenericMethodParameter(declaringMethod, this);
        }
        return this.genericMethodParameterDefinition;
      }
    }
    //^ [Once]
    GenericMethodParameter/*?*/ genericMethodParameterDefinition;

    /// <summary>
    /// Makes a shallow copy of this generic parameter that can be added to the generic parameter list of the given method declaration.
    /// The shallow copy may share child objects with this instance, but should never expose such child objects except through
    /// wrappers (or shallow copies made on demand). If this instance is already a parameter of the given method declaration it
    /// returns itself.
    /// </summary>
    public virtual GenericMethodParameterDeclaration MakeShallowCopyFor(MethodDeclaration declaringMethod)
      //^ requires declaringMethod.IsGeneric;
    {
      if (declaringMethod == this.DeclaringMethod) return this;
      return new GenericMethodParameterDeclaration(declaringMethod, this);
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a generic method parameter before constructing the declaring method declaration.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    /// <param name="declaringMethod"></param>
    public virtual void SetDeclaringMethod(MethodDeclaration declaringMethod)
      //^ requires declaringMethod.IsGeneric;
      //^ modifies this.*;
    {
      this.declaringMethod = declaringMethod;
      // ^ assume this.containingBlock == null;
      DummyExpression containingExpression = new DummyExpression(declaringMethod.DummyBlock, SourceDummy.SourceLocation);
      base.SetContainingExpression(containingExpression);
    }

  }

  /// <summary>
  /// Represents a global variable.
  /// </summary>
  public class GlobalFieldDeclaration : FieldDeclaration, INamespaceDeclarationMember, IAggregatableNamespaceDeclarationMember, IAggregatableTypeDeclarationMember {

    /// <summary>
    /// Allocates a member that represents a variable associated with an object or class.
    /// </summary>
    /// <param name="sourceAttributes">Custom attributes that are explicitly specified in source. May be null.
    /// Some of these may not end up in persisted metadata.
    /// For example in C# a custom attribute is used to specify IFieldDefinition.IsNotSerialized. Such a custom attribute is deleted by the compiler.</param>
    /// <param name="flags">A set of flags that specify the value of boolean properties of the field, such as IsStatic.</param>
    /// <param name="visibility">Indicates if the member is public or confined to its containing type, derived types and/or declaring assembly.</param>
    /// <param name="type">An expression that denote the type of value that is stored in this field.</param>
    /// <param name="name">The name of the member. </param>
    /// <param name="initializer">An expression that evaluates to the initial value of this field. May be null.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public GlobalFieldDeclaration(List<SourceCustomAttribute>/*?*/ sourceAttributes,
      FieldDeclaration.Flags flags, TypeMemberVisibility visibility,
      TypeExpression type, NameDeclaration name, Expression/*?*/ initializer, ISourceLocation sourceLocation)
      : base(sourceAttributes, flags, visibility, type, name, initializer, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing type.
    /// </summary>
    /// <param name="containingTypeDeclaration">The containing type of the copied member. This should be different from the containing type of the template member.</param>
    /// <param name="template">The type member to copy.</param>
    protected GlobalFieldDeclaration(TypeDeclaration containingTypeDeclaration, GlobalFieldDeclaration template)
      : base(containingTypeDeclaration, template)
      //^ ensures this.containingTypeDeclaration == containingTypeDeclaration;
    {
    }

    /// <summary>
    /// The namespace declaration in which this nested namespace declaration is nested.
    /// </summary>
    public NamespaceDeclaration ContainingNamespaceDeclaration {
      get
        // ^ ensures exists{INamespaceDeclarationMember member in result.Members; member == this};
      {
        return this.CompilationPart.RootNamespace;
      }
    }

    /// <summary>
    /// True if this field is visible outside of the unit in which it is defined.
    /// </summary>
    public bool IsPublic {
      get { return this.Visibility == TypeMemberVisibility.Public; }
    }

    /// <summary>
    /// Makes a shallow copy of this member that can be added to the member list of the given target type declaration.
    /// The shallow copy may share child objects with this instance, but should never expose such child objects except through
    /// wrappers (or shallow copies made on demand). If this instance is already a member of the target type declaration it
    /// returns itself.
    /// </summary>
    //^ [MustOverride, Pure]
    public override TypeDeclarationMember MakeShallowCopyFor(TypeDeclaration targetTypeDeclaration) {
      if (targetTypeDeclaration == this.ContainingTypeDeclaration) return this;
      return new GlobalFieldDeclaration(targetTypeDeclaration, this);
    }

    /// <summary>
    /// The symbol table entity corresponding to this global variable declaration
    /// </summary>
    public IGlobalFieldDefinition GlobalFieldDefinition {
      get {
        if (this.globalFieldDefinition == null)
          this.globalFieldDefinition = this.GetGlobalFieldDefinition();
        return this.globalFieldDefinition;
      }
    }
    IGlobalFieldDefinition/*?*/ globalFieldDefinition;

    /// <summary>
    /// Allocates or finds the global definition that corresponds to this declaration.
    /// </summary>
    protected virtual IGlobalFieldDefinition GetGlobalFieldDefinition() {
      foreach (ITypeDefinitionMember member in this.GlobalDefinitionsContainerType.GetMembersNamed(this.Name.Name, false)) {
        GlobalFieldDefinition/*?*/ globalField = member as GlobalFieldDefinition;
        if (globalField != null) {
          globalField.AddGlobalFieldDeclaration(this);
          return globalField;
        }
      }
      return new GlobalFieldDefinition(this);
    }

    /// <summary>
    /// A special type that is always a member of the root namespace of a unit and that contains all global variables and functions as its members.
    /// </summary>
    public ITypeDefinition GlobalDefinitionsContainerType {
      get { return this.ContainingTypeDeclaration.TypeDefinition; }
    }

    #region INamespaceDeclarationMember Members

    NamespaceDeclaration INamespaceDeclarationMember.ContainingNamespaceDeclaration {
      get { return this.CompilationPart.RootNamespace; }
    }

    /// <summary>
    /// Makes a shallow copy of this member that can be added to the member list of the given target namespace declaration.
    /// The shallow copy may share child objects with this instance, but should never expose such child objects except through
    /// wrappers (or shallow copies made on demand). If this instance is already a member of the target namespace declaration it
    /// returns itself. 
    /// </summary>
    INamespaceDeclarationMember INamespaceDeclarationMember.MakeShallowCopyFor(NamespaceDeclaration targetNamespaceDeclaration)
      //^^ requires targetNamespaceDeclaration.GetType() == this.ContainingNamespaceDeclaration.GetType();
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingNamespaceDeclaration == targetNamespaceDeclaration;
    {
      if (targetNamespaceDeclaration == this.CompilationPart.RootNamespace) return this;
      return (INamespaceDeclarationMember)this.MakeShallowCopyFor(((CompilationPart)targetNamespaceDeclaration.CompilationPart).GlobalDeclarationContainer);
    }

    #endregion

    #region IContainerMember<NamespaceDeclaration> Members

    NamespaceDeclaration IContainerMember<NamespaceDeclaration>.Container {
      get { return this.CompilationPart.RootNamespace; }
    }

    IName IContainerMember<NamespaceDeclaration>.Name {
      get { return this.Name; }
    }

    #endregion

    #region IAggregatableNamespaceDeclarationMember Members

    /// <summary>
    /// The single definition that is associated with this declaration and possibly other declarations too.
    /// </summary>
    /// <value></value>
    public INamespaceMember AggregatedMember {
      get { return this.GlobalFieldDefinition; }
    }

    #endregion

    #region IAggregatableTypeDeclarationMember Members

    ITypeDefinitionMember IAggregatableTypeDeclarationMember.AggregatedMember {
      get { return this.GlobalFieldDefinition; }
    }

    #endregion

  }

  /// <summary>
  /// Represents a global method.
  /// </summary>
  public class GlobalMethodDeclaration : MethodDeclaration, INamespaceDeclarationMember, IAggregatableNamespaceDeclarationMember {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sourceAttributes"></param>
    /// <param name="flags"></param>
    /// <param name="visibility"></param>
    /// <param name="type"></param>
    /// <param name="name"></param>
    /// <param name="genericParameters"></param>
    /// <param name="parameters"></param>
    /// <param name="body"></param>
    /// <param name="sourceLocation"></param>
    public GlobalMethodDeclaration(List<SourceCustomAttribute>/*?*/ sourceAttributes, Flags flags,
      TypeMemberVisibility visibility, TypeExpression type, NameDeclaration name, List<GenericMethodParameterDeclaration>/*?*/ genericParameters, List<ParameterDeclaration>/*?*/ parameters, BlockStatement/*?*/ body, ISourceLocation sourceLocation)
      : base(sourceAttributes, flags|Flags.Static, visibility, type, null,
      name, genericParameters, parameters, null, body, sourceLocation)
      //^ requires body == null ==> isExternal;
    {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing type.
    /// </summary>
    /// <param name="containingTypeDeclaration">The containing type of the copied member. This should be different from the containing type of the template member.</param>
    /// <param name="template">The type member to copy.</param>
    //^ [NotDelayed]
    protected GlobalMethodDeclaration(TypeDeclaration containingTypeDeclaration, GlobalMethodDeclaration template)
      : base(containingTypeDeclaration, template)
      //^ ensures this.containingTypeDeclaration == containingTypeDeclaration;
    {
      //^ base;
    }

    /// <summary>
    /// The namespace declaration in which this nested namespace declaration is nested.
    /// </summary>
    public NamespaceDeclaration ContainingNamespaceDeclaration {
      get
        // ^ ensures exists{INamespaceDeclarationMember member in result.Members; member == this};
      {
        return this.CompilationPart.RootNamespace;
      }
    }

    /// <summary>
    /// The symbol table entity corresponding to this global method declaration
    /// </summary>
    public GlobalMethodDefinition GlobalMethodDefinition {
      get {
        if (this.globalMethodDefinition == null)
          this.globalMethodDefinition = this.CreateGlobalMethodDefinition();
        return this.globalMethodDefinition;
      }
    }
    //^ [Once]
    GlobalMethodDefinition/*?*/ globalMethodDefinition;

    /// <summary>
    /// Allocates the global definition that corresponds to this declaration.
    /// </summary>
    protected virtual GlobalMethodDefinition CreateGlobalMethodDefinition() {
      GlobalMethodDefinition globalMethodDefinition = new GlobalMethodDefinition(this);
      MethodContract/*?*/ contract = this.Compilation.ContractProvider.GetMethodContractFor(this) as MethodContract;
      if (contract != null)
        this.Compilation.ContractProvider.AssociateMethodWithContract(globalMethodDefinition, contract);
      return globalMethodDefinition;
    }

    /// <summary>
    /// Allocates or finds the method definition that corresponds to this declaration.
    /// </summary>
    protected override MethodDefinition CreateMethodDefinition() {
      return this.GlobalMethodDefinition;
    }

    /// <summary>
    /// Makes a shallow copy of this member that can be added to the member list of the given target type declaration.
    /// The shallow copy may share child objects with this instance, but should never expose such child objects except through
    /// wrappers (or shallow copies made on demand). If this instance is already a member of the target type declaration it
    /// returns itself.
    /// </summary>
    //^ [MustOverride, Pure]
    public override TypeDeclarationMember MakeShallowCopyFor(TypeDeclaration targetTypeDeclaration) {
      if (targetTypeDeclaration == this.ContainingTypeDeclaration) return this;
      return new GlobalMethodDeclaration(targetTypeDeclaration, this);
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a type member before constructing the containing type declaration.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingTypeDeclaration(TypeDeclaration containingTypeDeclaration, bool recurse) {
      base.SetContainingTypeDeclaration(containingTypeDeclaration, recurse);
      if (!recurse) return;
      MethodContract/*?*/ contract = this.Compilation.ContractProvider.GetMethodContractFor(this) as MethodContract;
      if (contract != null)
        contract.SetContainingBlock(this.DummyBlock);
    }

    #region IAggregatableNamespaceDeclarationMember Members

    INamespaceMember IAggregatableNamespaceDeclarationMember.AggregatedMember {
      get { return this.GlobalMethodDefinition; }
    }

    #endregion

    #region INamespaceDeclarationMember Members

    NamespaceDeclaration INamespaceDeclarationMember.ContainingNamespaceDeclaration {
      get { return this.CompilationPart.RootNamespace; }
    }

    /// <summary>
    /// Makes a shallow copy of this member that can be added to the member list of the given target namespace declaration.
    /// The shallow copy may share child objects with this instance, but should never expose such child objects except through
    /// wrappers (or shallow copies made on demand). If this instance is already a member of the target namespace declaration it
    /// returns itself. 
    /// </summary>
    INamespaceDeclarationMember INamespaceDeclarationMember.MakeShallowCopyFor(NamespaceDeclaration targetNamespaceDeclaration)
      //^^ requires targetNamespaceDeclaration.GetType() == this.ContainingNamespaceDeclaration.GetType();
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingNamespaceDeclaration == targetNamespaceDeclaration;
    {
      if (targetNamespaceDeclaration == this.CompilationPart.RootNamespace) return this;
      return (INamespaceDeclarationMember)this.MakeShallowCopyFor(((CompilationPart)targetNamespaceDeclaration.CompilationPart).GlobalDeclarationContainer);
    }

    #endregion

    #region IContainerMember<NamespaceDeclaration> Members

    NamespaceDeclaration IContainerMember<NamespaceDeclaration>.Container {
      get { return this.CompilationPart.RootNamespace; }
    }

    IName IContainerMember<NamespaceDeclaration>.Name {
      get { return this.Name; }
    }

    #endregion

  }

  /// <summary>
  /// This class models the source representation of a method.
  /// </summary>
  public class MethodDeclaration : TypeDeclarationMember, ISignatureDeclaration {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sourceAttributes">May be null.</param>
    /// <param name="flags"></param>
    /// <param name="visibility"></param>
    /// <param name="type"></param>
    /// <param name="implementedInterfaces">May be null.</param>
    /// <param name="name"></param>
    /// <param name="genericParameters">May be null.</param>
    /// <param name="parameters">May be null.</param>
    /// <param name="handledEvents">May be null.</param>
    /// <param name="body">May be null.</param>
    /// <param name="sourceLocation"></param>
    public MethodDeclaration(List<SourceCustomAttribute>/*?*/ sourceAttributes,
      Flags flags, TypeMemberVisibility visibility, TypeExpression type, List<TypeExpression>/*?*/ implementedInterfaces, NameDeclaration name,
      List<GenericMethodParameterDeclaration>/*?*/ genericParameters, List<ParameterDeclaration>/*?*/ parameters, List<QualifiedName>/*?*/ handledEvents,
      BlockStatement/*?*/ body, ISourceLocation sourceLocation)
      : base(sourceAttributes, (TypeDeclarationMember.Flags)flags, visibility, name, sourceLocation) {
      this.type = type;
      this.genericParameters = genericParameters;
      this.parameters = parameters;
      this.handledEvents = handledEvents;
      this.body = body;
      this.flags |= (int)(ExtendedFlags.CustomAttributesNotYetProcessed|ExtendedFlags.ImplicitInterfaceImplementationNotYetChecked);
      this.implementedInterfaces = implementedInterfaces;
    }

    /// <summary>
    /// 
    /// </summary>
    [Flags]
    public new enum Flags {
      /// <summary>
      /// 
      /// </summary>
      New=int.MinValue,
      /// <summary>
      /// 
      /// </summary>
      Unsafe=(New>>1)&int.MaxValue,

      /// <summary>
      /// 
      /// </summary>
      Abstract=Unsafe>>1,
      /// <summary>
      /// 
      /// </summary>
      External=Abstract>>1,
      /// <summary>
      /// 
      /// </summary>
      Override=External>>1,
      /// <summary>
      /// 
      /// </summary>
      Sealed=Override>>1,
      /// <summary>
      /// 
      /// </summary>
      Static=Sealed>>1,
      /// <summary>
      /// 
      /// </summary>
      Virtual=Static>>1,

      //Up to here is must remain the same as EventDeclaration.Flags and PropertyDeclaration.Flags

      /// <summary>
      /// 
      /// </summary>
      AcceptsExtraArguments=Virtual>>1,
      /// <summary>
      /// 
      /// </summary>
      IsCompilerGenerated=AcceptsExtraArguments>>1,
      /// <summary>
      /// 
      /// </summary>
      ExtensionMethod=IsCompilerGenerated>>1,
      /// <summary>
      /// 
      /// </summary>
      SpecialName=ExtensionMethod>>1,
      /// <summary>
      /// 
      /// </summary>
      Synchronized=SpecialName>>1,
    }

    [Flags]
    private enum ExtendedFlags {
      CustomAttributesNotYetProcessed=Flags.Synchronized >> 1,
      ImplicitInterfaceImplementationNotYetChecked=CustomAttributesNotYetProcessed>>1,

      Cil=ImplicitInterfaceImplementationNotYetChecked>>1,
      DeclarativeSecurity=Cil>>1,
      ExplicitThisParameter=DeclarativeSecurity>>1,
      NativeCode=ExplicitThisParameter>>1,
      NeverInlined=NativeCode>>1,
      NeverOptimized=NeverInlined>>1,
      PlatformInvoke=NeverOptimized>>1,
      PreserveSignature=PlatformInvoke>>1,
      RequiresSecurityObject=PreserveSignature>>1,
      RuntimeImplemented=RequiresSecurityObject>>1,
      Unmanaged=RuntimeImplemented>>1,
    }

    //^ [NotDelayed]
    /// <summary>
    /// 
    /// </summary>
    /// <param name="containingTypeDeclaration"></param>
    /// <param name="template"></param>
    protected MethodDeclaration(TypeDeclaration containingTypeDeclaration, MethodDeclaration template)
      : base(containingTypeDeclaration, template)
      //^ ensures this.containingTypeDeclaration == containingTypeDeclaration;
    {
      if (template.body != null)
        this.body = (BlockStatement)template.body.MakeCopyFor(containingTypeDeclaration.DummyBlock);
      //TODO: provide a way to provide body on demand by reparsing it.
      if (template.genericParameters != null && template.genericParameters.Count > 0)
        this.genericParameters = new List<GenericMethodParameterDeclaration>(template.genericParameters);
      if (template.handledEvents != null)
        this.handledEvents = new List<QualifiedName>(template.handledEvents);
      if (template.implementedInterfaces != null)
        this.implementedInterfaces = new List<TypeExpression>(template.implementedInterfaces);
      if (template.parameters != null)
        this.parameters = new List<ParameterDeclaration>(template.parameters);
      this.type = (TypeExpression)template.type.MakeCopyFor(containingTypeDeclaration.DummyBlock);
      //^ base;
      //^ assume this.genericParameters == null || this.IsGeneric;
    }

    /// <summary>
    /// True if the method is a "vararg" method. That is, if it has a calling convention that allows extra arguments to be passed on the stack.
    /// In C++ such methods specify ... at the end of their parameter lists. In C#, the __arglist keyword is used.
    /// </summary>
    public bool AcceptsExtraArguments {
      get { return (this.flags & (int)Flags.AcceptsExtraArguments) != 0; }
    }

    /// <summary>
    /// Get custom attributes from the corresponding type definition, and run through them setting the flag bits for the various computed Boolean properties.
    /// </summary>
    private void AnalyzeAttributesToExtractComputedFlags() {
      if ((this.flags & (int)ExtendedFlags.CustomAttributesNotYetProcessed) == 0) return;
      this.flags &= ~(int)ExtendedFlags.CustomAttributesNotYetProcessed;
      foreach (SourceCustomAttribute attribute in this.SourceAttributes) {
        if (TypeHelper.TypesAreEquivalent(attribute.Type.ResolvedType, this.Helper.PlatformType.SystemRuntimeInteropServicesDllImportAttribute))
          this.AnalyzeDllImportAttribute(attribute);
        //TODO: more attributes.
      }
    }

    private void AnalyzeDllImportAttribute(SourceCustomAttribute attribute) {
      this.flags |= (int)ExtendedFlags.PlatformInvoke;
      foreach (Expression expr in attribute.Arguments) {
        NamedArgument/*?*/ narg = expr as NamedArgument;
        if (narg == null) continue;
        if (narg.ArgumentName.Name.Value == "PreserveSig" && narg.ArgumentValue.Value is bool && ((bool)narg.ArgumentValue.Value)) {
          this.flags |= (int)ExtendedFlags.PreserveSignature;
          break;
        }
      }
    }

    /// <summary>
    /// The body of this method.
    /// </summary>
    public BlockStatement Body {
      get
        //^^ requires !this.IsAbstract && !this.IsExternal;
      {
        //^ assume this.body != null;
        return this.body;
      }
    }
    readonly BlockStatement/*?*/ body;

    /// <summary>
    /// Calling convention of the method.
    /// </summary>
    public virtual CallingConvention CallingConvention {
      get {
        CallingConvention result = CallingConvention.Default;
        if (this.IsGeneric) result |= CallingConvention.Generic;
        if (!this.IsStatic) result |= CallingConvention.HasThis;
        return result;
      } //TODO: extract from custom attributes
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the method or a constituent part of the method.
    /// Do not call this method directly, but evaluate the HasErrors property. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      bool result = false;
      // Check extension method validity
      if (this.IsExtensionMethod) {
        NamedTypeDefinition surroundingType = this.ContainingTypeDeclaration.TypeDefinition;
        // Extension methods can only be declared on types that are:
        //   classes, declared static, non-nested, not generic.
        Error error = Error.NotAnError;
        if (!surroundingType.IsClass
          || !surroundingType.IsStatic
          || surroundingType is NestedTypeDefinition)
          error = Error.ExtensionMethodsOnlyInStaticClass;
        else if (surroundingType.GenericParameterCount != 0)
          error = Error.ExtensionMethodsOnlyInNonGenericClass;
        // TODO: Need to check that first parameter is not of pointer type.
        if (error != Error.NotAnError) {
          this.Helper.ReportError(new AstErrorMessage(this, error));
          result = true;
        }
      }
      //TODO: lots more

      // Finally, recurse to body ...
      if (this.body != null)
        result |= this.body.HasErrors;
      return result;
    }

    //private ITypeDefinition TypeOfFirstParameter() {
    //  ITypeDefinition firstParameter = Dummy.Type;
    //  IEnumerator<ParameterDeclaration> parameters = this.Parameters.GetEnumerator();
    //  if (parameters.MoveNext())
    //    firstParameter = parameters.Current.Type.Type;
    //  return firstParameter;
    //}



    private void CheckIfNonVirtualThatImplicitlyImplementsInterfaceMethod() {
      if ((this.flags & (int)ExtendedFlags.ImplicitInterfaceImplementationNotYetChecked) == 0) return;
      this.flags &= ~(int)ExtendedFlags.ImplicitInterfaceImplementationNotYetChecked;
      if ((this.flags & (int)Flags.Virtual) != 0) return;
      if (this.Visibility != TypeMemberVisibility.Public) return;
      if (!this.ContainingTypeDeclaration.TypeDefinition.UsesToImplementAnInterfaceMethod(this.MethodDefinition)) return;
      this.flags |= (int)Flags.Sealed;
      this.flags |= (int)Flags.Virtual;
    }

    /// <summary>
    /// Calls the visitor.Visit(MethodDeclaration) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// A block statement that serves as the declaring block of any expressions that form part of the the method declaration
    /// but that do not appear inside a method body.
    /// </summary>
    public BlockStatement DummyBlock {
      get {
        if (this.dummyBlock == null) {
          BlockStatement dummyBlock = BlockStatement.CreateDummyFor(this.SourceLocation);
          dummyBlock.SetContainers(this.ContainingTypeDeclaration.DummyBlock, this);
          lock (this) {
            if (this.dummyBlock == null) {
              this.dummyBlock = dummyBlock;
            }
          }
        }
        return this.dummyBlock;
      }
    }
    //^ [Once]
    private BlockStatement/*?*/ dummyBlock;

    /// <summary>
    /// If the method is generic then this list contains the type parameters.
    /// </summary>
    public IEnumerable<GenericMethodParameterDeclaration> GenericParameters {
      get {
        List<GenericMethodParameterDeclaration> genericParameters;
        if (this.genericParameters == null)
          yield break;
        else
          genericParameters = this.genericParameters;
        for (int i = 0, n = genericParameters.Count; i < n; i++)
          yield return genericParameters[i] = genericParameters[i].MakeShallowCopyFor(this);
      }
    }
    //^ [SpecPublic]
    List<GenericMethodParameterDeclaration>/*?*/ genericParameters;
    //^ invariant genericParameters == null || this.IsGeneric;

    /// <summary>
    /// The number of generic parameters. Zero if the method is not generic.
    /// </summary>
    public ushort GenericParameterCount
      //^^ ensures !this.IsGeneric ==> result == 0;
      //^^ ensures this.IsGeneric ==> result > 0;
    {
      get {
        ushort result = (ushort)(this.genericParameters == null ? 0 : this.genericParameters.Count);
        //^ assume this.IsGeneric <==> (this.genericParameters != null && this.genericParameters.Count > 0);
        return result;
      }
    }

    /// <summary>
    /// Returns a list of custom attributes that describes this type declaration member.
    /// Typically, these will be derived from this.SourceAttributes. However, some source attributes
    /// might instead be persisted as metadata bits and other custom attributes may be synthesized
    /// from information not provided in the form of source custom attributes.
    /// The list is not trimmed to size, since an override of this method may call the base method
    /// and then add more attributes.
    /// </summary>
    protected override List<ICustomAttribute> GetAttributes() {
      var result = base.GetAttributes();
      if ((this.flags & (int)MethodDeclaration.Flags.IsCompilerGenerated) != 0) {
        var cgattr = new Microsoft.Cci.MutableCodeModel.CustomAttribute();
        cgattr.Constructor = this.Compilation.CompilerGeneratedCtor;
        result.Add(cgattr);
      }
      if ((this.flags & (int)MethodDeclaration.Flags.ExtensionMethod) != 0) {
        var eattr = new Microsoft.Cci.MutableCodeModel.CustomAttribute();
        eattr.Constructor = this.Compilation.ExtensionAttributeCtor;
        result.Add(eattr);
      }
      return result;
    }

    /// <summary>
    /// A list of events that are declaratively handled by this method. Whenever an object is assigned to an event source in the list,
    /// the method is unhooked from the event of the previous value and hooked up to the event of the new value. In VB this corresponds
    /// to a the Handles clause of a method.
    /// </summary>
    public IEnumerable<QualifiedName> HandledEvents {
      get {
        List<QualifiedName> handledEvents;
        if (this.handledEvents == null)
          yield break;
        else
          handledEvents = this.handledEvents;
        for (int i = 0, n = handledEvents.Count; i < n; i++)
          yield return handledEvents[i] = (QualifiedName)handledEvents[i].MakeCopyFor(this.DummyBlock);
      }
    }
    readonly List<QualifiedName>/*?*/ handledEvents;

    /// <summary>
    /// True if this method has a non empty collection of SecurityAttributes or the System.Security.SuppressUnmanagedCodeSecurityAttribute.
    /// </summary>
    public virtual bool HasDeclarativeSecurity {
      get {
        this.AnalyzeAttributesToExtractComputedFlags();
        return (this.flags & (int)ExtendedFlags.DeclarativeSecurity) != 0;
      }
    }

    /// <summary>
    /// True if this is an instance method that explicitly declares the type and name of its first parameter (the instance).
    /// </summary>
    public virtual bool HasExplicitThisParameter {
      get {
        this.AnalyzeAttributesToExtractComputedFlags();
        return (this.flags & (int)ExtendedFlags.ExplicitThisParameter) != 0;
      }
    }

    /// <summary>
    /// A list of interfaces whose corresponding abstract methods are implemented by this method.
    /// </summary>
    public IEnumerable<TypeExpression> ImplementedInterfaces {
      get {
        List<TypeExpression> implementedInterfaces;
        if (this.implementedInterfaces == null)
          yield break;
        else
          implementedInterfaces = this.implementedInterfaces;
        for (int i = 0, n = implementedInterfaces.Count; i < n; i++) {
          yield return implementedInterfaces[i] = (TypeExpression)implementedInterfaces[i].MakeCopyFor(this.ContainingTypeDeclaration.DummyBlock);
        }
      }
    }
    internal readonly List<TypeExpression>/*?*/ implementedInterfaces;

    /// <summary>
    /// True if the method does not provide an implementation (there is no body and evaluating the Body property is an error).
    /// </summary>
    public bool IsAbstract {
      get { return (this.flags & (int)Flags.Abstract) != 0; }
    }

    /// <summary>
    /// True if the method can only be overridden when it is also accessible. 
    /// </summary>
    public virtual bool IsAccessCheckedOnOverride {
      get { return false; }
    }

    /// <summary>
    /// True if the method is implemented in the CLI Common Intermediate Language.
    /// </summary>
    public virtual bool IsCil {
      get {
        this.AnalyzeAttributesToExtractComputedFlags();
        return (this.flags & (int)ExtendedFlags.Cil) == 0;
      }
    }

    /// <summary>
    /// True if this method is a static method that can be called as an instance method on another class because it has an explicit this parameter.
    /// In other words, the class defining this static method is effectively extending another class, but doing so without subclassing it and
    /// without requiring client code to instantiate the subclass.
    /// </summary>
    public bool IsExtensionMethod {
      get { return (this.flags & (int)Flags.ExtensionMethod) != 0; }
    }

    /// <summary>
    /// True if the method has an external implementation (i.e. not supplied by this declaration).
    /// </summary>
    public bool IsExternal {
      get {
        if (this.containingTypeDeclaration is IDelegateDeclaration) return true;
        return (this.flags & (int)Flags.External) != 0;
      }
    }

    /// <summary>
    /// True if the method implementation is defined by another method definition (to be supplied at a later time).
    /// Only for use in 
    /// </summary>
    public bool IsForwardReference {
      get {
        return this.body == null && this.IsExternal;
      }
    }

    /// <summary>
    /// True if the method has generic parameters;
    /// </summary>
    public bool IsGeneric {
      get
        //^ ensures result <==> (this.genericParameters != null && this.genericParameters.Count > 0);
      {
        if (this.genericParameters != null) {
          if (this.genericParameters.Count > 0) return true;
          this.genericParameters = null;
        }
        return false;
      }
    }

    /// <summary>
    /// True if this method is hidden if a derived type declares a method with the same name and signature. 
    /// If false, any method with the same name hides this method. This flag is ignored by the runtime and is only used by compilers.
    /// </summary>
    public virtual bool IsHiddenBySignature {
      get {
        return true;
      }
    }

    /// <summary>
    /// True if the method is implemented in native (platform-specific) code.
    /// </summary>
    public virtual bool IsNativeCode {
      get {
        this.AnalyzeAttributesToExtractComputedFlags();
        return (this.flags & (int)ExtendedFlags.NativeCode) != 0;
      }
    }

    /// <summary>
    /// True if the the runtime is not allowed to inline this method.
    /// </summary>
    public virtual bool IsNeverInlined {
      get {
        this.AnalyzeAttributesToExtractComputedFlags();
        return (this.flags & (int)ExtendedFlags.NeverInlined) != 0;
      }
    }

    /// <summary>
    /// True if the the runtime is not allowed to inline this method.
    /// </summary>
    public virtual bool IsNeverOptimized {
      get {
        this.AnalyzeAttributesToExtractComputedFlags();
        return (this.flags & (int)ExtendedFlags.NeverOptimized) != 0;
      }
    }

    /// <summary>
    /// True if the method is implemented via the invocation of an underlying platform method.
    /// </summary>
    public virtual bool IsPlatformInvoke {
      get {
        this.AnalyzeAttributesToExtractComputedFlags();
        return (this.flags & (int)ExtendedFlags.PlatformInvoke) != 0;
      }
    }

    /// <summary>
    /// True if this method overrides a base class method.
    /// </summary>
    public bool IsOverride {
      get { return (this.flags & (int)Flags.Override) != 0; }
    }

    /// <summary>
    /// True if the implementation of this method is supplied by the runtime.
    /// </summary>
    public virtual bool IsRuntimeImplemented {
      get {
        if (this.containingTypeDeclaration is IDelegateDeclaration) return true;
        this.AnalyzeAttributesToExtractComputedFlags();
        return (this.flags & (int)ExtendedFlags.RuntimeImplemented) != 0;
      }
    }

    /// <summary>
    /// True if the method is an internal part of the runtime and must be called in a special way.
    /// </summary>
    public virtual bool IsRuntimeInternal {
      get { return false; }
    }

    /// <summary>
    /// True if the method gets special treatment from the runtime. For example, it might be a constructor.
    /// </summary>
    public virtual bool IsRuntimeSpecial {
      get { return this.IsSpecialName && (this.Name.UniqueKey == this.NameTable.Ctor.UniqueKey || this.Name.UniqueKey == this.NameTable.Cctor.UniqueKey); }
    }

    /// <summary>
    /// True if this method may not be overridden by a derived class method.
    /// </summary>
    public bool IsSealed {
      get {
        this.CheckIfNonVirtualThatImplicitlyImplementsInterfaceMethod();
        return (this.flags & (int)Flags.Sealed) != 0;
      }
    }

    /// <summary>
    /// True if the method is special in some way for tools. For example, it might be a property getter or setter.
    /// </summary>
    public virtual bool IsSpecialName {
      get { return (this.flags & (int)Flags.SpecialName) != 0; }
    }

    /// <summary>
    /// True if the method does not require an instance of its declaring type as its first argument.
    /// </summary>
    public bool IsStatic {
      get { return (this.flags & (int)Flags.Static) != 0; }
    }

    /// <summary>
    /// True if only one thread at a time may execute this method.
    /// </summary>
    public bool IsSynchronized {
      get { return (this.flags & (int)Flags.Synchronized) != 0; }
    }

    /// <summary>
    /// True if the implementation of this method is not managed by the runtime.
    /// </summary>
    public virtual bool IsUnmanaged {
      get {
        this.AnalyzeAttributesToExtractComputedFlags();
        return (this.flags & (int)ExtendedFlags.Unmanaged) != 0;
      }
    }

    /// <summary>
    /// True if the method may be overridden (or if it is an override).
    /// </summary>
    public bool IsVirtual {
      get {
        this.CheckIfNonVirtualThatImplicitlyImplementsInterfaceMethod();
        return (this.flags & (int)Flags.Virtual) != 0;
      }
    }

    /// <summary>
    /// Makes a shallow copy of this member that can be added to the member list of the given target type declaration.
    /// The shallow copy may share child objects with this instance, but should never expose such child objects except through
    /// wrappers (or shallow copies made on demand). If this instance is already a member of the target type declaration it
    /// returns itself.
    /// </summary>
    //^ [MustOverride, Pure]
    public override TypeDeclarationMember MakeShallowCopyFor(TypeDeclaration targetTypeDeclaration) {
      if (this.ContainingTypeDeclaration == targetTypeDeclaration) return this;
      return new MethodDeclaration(targetTypeDeclaration, this);
    }

    /// <summary>
    /// The symbol table object that represents the metadata for this method.
    /// </summary>
    public MethodDefinition MethodDefinition {
      get {
        if (this.methodDefinition == null) {
          lock (GlobalLock.LockingObject) {
            if (this.methodDefinition == null)
              this.methodDefinition = this.CreateMethodDefinition();
          }
        }
        return this.methodDefinition;
      }
    }
    //^ [Once]
    MethodDefinition/*?*/ methodDefinition;

    /// <summary>
    /// Allocates or the method definition that corresponds to this declaration.
    /// </summary>
    protected virtual MethodDefinition CreateMethodDefinition() {
      MethodDefinition methodDefinition = new MethodDefinition(this);
      SourceContractProvider provider = this.Compilation.ContractProvider;
      IMethodContract/*?*/ contract = provider.GetMethodContractFor(this);
      if (contract != null)
        provider.AssociateMethodWithContract(methodDefinition, contract);
      return methodDefinition;
    }

    /// <summary>
    /// The parameters of this method.
    /// </summary>
    public IEnumerable<ParameterDeclaration> Parameters {
      get {
        List<ParameterDeclaration> parameters;
        if (this.parameters == null)
          yield break;
        else
          parameters = this.parameters;
        for (int i = 0, n = parameters.Count; i < n; i++)
          yield return parameters[i] = parameters[i].MakeShallowCopyFor(this, this.DummyBlock);
      }
    }
    internal readonly List<ParameterDeclaration>/*?*/ parameters;

    /// <summary>
    /// Detailed information about the PInvoke stub. Identifies which method to call, which module has the method and the calling convention among other things.
    /// </summary>
    public IPlatformInvokeInformation PlatformInvokeData {
      get
        //^ requires this.IsPlatformInvoke;
      {
        IPlatformInvokeInformation result;
        lock (GlobalLock.LockingObject) {
          if (this.CompilationPart.PlatformInvokeInformationTable.TryGetValue(this, out result)) return result;
        }
        result = this.ComputePlatformInvokeInformation();
        lock (GlobalLock.LockingObject) {
          this.CompilationPart.PlatformInvokeInformationTable.Add(this, result);
        }
        return result;
      }
    }

    private IPlatformInvokeInformation ComputePlatformInvokeInformation() {
      foreach (SourceCustomAttribute attribute in this.SourceAttributes) {
        if (TypeHelper.TypesAreEquivalent(attribute.Type.ResolvedType, this.Helper.PlatformType.SystemRuntimeInteropServicesDllImportAttribute)) {
          return new PlatformInvokeInformation(this, attribute);
        }
      }
      return Dummy.PlatformInvokeInformation;
    }

    /// <summary>
    /// True if the method signature must not be mangled during the interoperation with COM code.
    /// </summary>
    public virtual bool PreserveSignature {
      get {
        this.AnalyzeAttributesToExtractComputedFlags();
        return (this.flags & (int)ExtendedFlags.PreserveSignature) != 0;
      }
    }

    /// <summary>
    /// The name of this method, qualified with the name of an interface, if this method is the private implementation
    /// of an interface method.
    /// </summary>
    internal protected virtual IName QualifiedName {
      get {
        if (this.qualifiedName == null) {
          if (this.implementedInterfaces == null || this.implementedInterfaces.Count == 0)
            this.qualifiedName = this.Name.Name;
          else
            this.qualifiedName = this.Helper.NameTable.GetNameFor(this.implementedInterfaces[0].SourceLocation.Source+"."+this.Name.Value);
        }
        return this.qualifiedName;
      }
    }
    /// <summary>
    /// 
    /// </summary>
    protected IName/*?*/ qualifiedName;

    /// <summary>
    /// True if the method calls another method containing security code. If this flag is set, the method
    /// should have System.Security.DynamicSecurityMethodAttribute present in its list of custom attributes.
    /// </summary>
    public virtual bool RequiresSecurityObject {
      get {
        this.AnalyzeAttributesToExtractComputedFlags();
        return (this.flags & (int)ExtendedFlags.RequiresSecurityObject) != 0;
      }
    }

    /// <summary>
    /// Custom attributes associated with the method's return value.
    /// </summary>
    public virtual IEnumerable<ICustomAttribute> ReturnValueAttributes {
      get { return Enumerable<ICustomAttribute>.Empty; } //TODO: compute this
    }

    /// <summary>
    /// Returns the list of custom modifiers, if any, associated with the retuned value. Evaluate this property only if ReturnValueIsModified is true.
    /// </summary>
    public virtual IEnumerable<ICustomModifier> ReturnValueCustomModifiers {
      get { return Enumerable<ICustomModifier>.Empty; } //TODO: compute this
    }

    /// <summary>
    /// True if the return value is passed by reference (using a managed pointer).
    /// </summary>
    public virtual bool ReturnValueIsByRef {
      get { return false; }
    }

    /// <summary>
    /// True if the return value has one or more custom modifiers associated with it.
    /// </summary>
    public virtual bool ReturnValueIsModified {
      get { return false; } //TODO: compute this
    }

    /// <summary>
    /// The return value has associated marshalling information.
    /// </summary>
    public bool ReturnValueIsMarshalledExplicitly {
      get { return false; } //TODO: compute this
    }

    /// <summary>
    /// Specifies how the return value is marshalled when the method is called from unmanaged code.
    /// </summary>
    public IMarshallingInformation ReturnValueMarshallingInformation {
      get { return Dummy.MarshallingInformation; } //TODO: compute this
    }

    /// <summary>
    /// Declarative security actions for this method.
    /// </summary>
    public virtual IEnumerable<ISecurityAttribute> SecurityAttributes {
      get { return Enumerable<ISecurityAttribute>.Empty; } //TODO: compute this
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a type member before constructing the containing type declaration.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingTypeDeclaration(TypeDeclaration containingTypeDeclaration, bool recurse)
      //^ ensures this.ContainingTypeDeclaration == containingTypeDeclaration;
    {
      base.SetContainingTypeDeclaration(containingTypeDeclaration, recurse);
      //^ assert this.ContainingTypeDeclaration == containingTypeDeclaration;
      if (!recurse) return;
      if (containingTypeDeclaration is IInterfaceDeclaration)
        this.flags |= (int)Flags.Abstract;
      DummyExpression containingExpression = new DummyExpression(this.DummyBlock, SourceDummy.SourceLocation);
      if (this.body != null)
        this.body.SetContainingBlock(this.DummyBlock);
      if (this.genericParameters != null) {
        foreach (GenericMethodParameterDeclaration genericParameter in this.genericParameters) {
          //^ assume this.IsGeneric; //TODO: this should not be necessary
          genericParameter.SetDeclaringMethod(this);
        }
      }
      if (this.implementedInterfaces != null)
        foreach (TypeExpression implementedInterface in this.implementedInterfaces) implementedInterface.SetContainingExpression(containingExpression);
      if (this.parameters != null)
        foreach (ParameterDeclaration parameter in this.parameters) parameter.SetContainingSignatureAndExpression(this, containingExpression);
      this.type.SetContainingExpression(containingExpression);
      //^ assume this.ContainingTypeDeclaration == containingTypeDeclaration;
    }

    /// <summary>
    /// The symbol table object that represents the metadata for this method.
    /// </summary>
    public ISignature SignatureDefinition {
      get { return this.MethodDefinition; }
    }

    /// <summary>
    /// The symbol table object that represents the metadata for this member.
    /// </summary>
    public override TypeDefinitionMember TypeDefinitionMember {
      get { return this.MethodDefinition; }
    }

    /// <summary>
    /// An expression that denotes the return type of the method.
    /// </summary>
    public TypeExpression Type {
      get { return this.type; }
    }
    readonly TypeExpression type;

  }

  /// <summary>
  /// 
  /// </summary>
  public class ParameterDeclaration : SourceItemWithAttributes, IDeclaration, INamedEntity, IParameterListEntry {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sourceAttributes"></param>
    /// <param name="type"></param>
    /// <param name="name"></param>
    /// <param name="defaultValue"></param>
    /// <param name="index"></param>
    /// <param name="isOptional"></param>
    /// <param name="isOut"></param>
    /// <param name="isParameterArray"></param>
    /// <param name="isRef"></param>
    /// <param name="isThis"></param>
    /// <param name="sourceLocation"></param>
    public ParameterDeclaration(List<SourceCustomAttribute>/*?*/ sourceAttributes,
      TypeExpression type, NameDeclaration name, Expression/*?*/ defaultValue, ushort index,
      bool isOptional, bool isOut, bool isParameterArray, bool isRef, bool isThis, ISourceLocation sourceLocation)
      : base(sourceLocation)
      //^ requires isParameterArray ==> type is ArrayTypeExpression;
    {
      this.sourceAttributes = sourceAttributes;
      this.type = type;
      this.name = name;
      this.defaultValue = defaultValue;
      this.index = index;
      int flags = 0;
      //Reserve four bits for IsIn and IsMarshalledExplicitly. These are computed from custom attributes, then cached.
      if (isOptional) flags |= 16;
      if (isOut) flags |= 32;
      if (isParameterArray) flags |= 64;
      if (isRef) flags |= 128;
      if (isThis) flags |= 256;
      this.flags = flags;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sourceAttributes"></param>
    /// <param name="type"></param>
    /// <param name="name"></param>
    /// <param name="defaultValue"></param>
    /// <param name="index"></param>
    /// <param name="isOptional"></param>
    /// <param name="isOut"></param>
    /// <param name="isParameterArray"></param>
    /// <param name="isRef"></param>
    /// <param name="sourceLocation"></param>
    public ParameterDeclaration(List<SourceCustomAttribute>/*?*/ sourceAttributes,
      TypeExpression type, NameDeclaration name, Expression/*?*/ defaultValue, ushort index,
      bool isOptional, bool isOut, bool isParameterArray, bool isRef, ISourceLocation sourceLocation)
      : this(sourceAttributes, type, name, defaultValue, index, isOptional, isOut, isParameterArray, isRef, false, sourceLocation) {
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="containingSignature"></param>
    /// <param name="containingBlock"></param>
    /// <param name="template"></param>
    protected ParameterDeclaration(ISignatureDeclaration containingSignature, BlockStatement containingBlock, ParameterDeclaration template)
      : base(template.SourceLocation) {
      this.containingSignature = containingSignature;
      this.sourceAttributes = new List<SourceCustomAttribute>(template.SourceAttributes);
      this.type = (TypeExpression)template.type.MakeCopyFor(containingBlock);
      this.name = template.Name.MakeCopyFor(containingBlock.Compilation);
      if (template.HasDefaultValue)
        this.defaultValue = template.DefaultValue.MakeCopyFor(containingBlock);
      this.index = template.index;
      this.flags = template.flags;
    }

    /// <summary>
    /// Returns a list of custom attributes that describes this type declaration member.
    /// Typically, these will be derived from this.SourceAttributes. However, some source attributes
    /// might instead be persisted as metadata bits and other custom attributes may be synthesized
    /// from information not provided in the form of source custom attributes.
    /// The list is not trimmed to size, since an override of this method may call the base method
    /// and then add more attributes.
    /// </summary>
    protected override List<ICustomAttribute> GetAttributes() {
      var result = new List<ICustomAttribute>();
      foreach (var sourceAttribute in this.SourceAttributes) {
        if (sourceAttribute.HasErrors) continue;
        result.Add(new CustomAttribute(sourceAttribute));
      }
      //TODO: suppress pseudo attributes and add in synthesized ones, such as the param array attribute
      return result;
    }

    /// <summary>
    /// The compilation that contains this parameter declaration.
    /// </summary>
    public Compilation Compilation {
      get { return this.Type.ContainingBlock.Compilation; }
    }

    /// <summary>
    /// The compilation part that contains this parameter declaration.
    /// </summary>
    public CompilationPart CompilationPart {
      get { return this.Type.ContainingBlock.CompilationPart; }
    }

    /// <summary>
    /// The method or property that declares this parameter.
    /// </summary>
    public ISignatureDeclaration ContainingSignature {
      get {
        //^ assume this.containingSignature != null; 
        return this.containingSignature;
      }
    }
    //^ [SpecPublic]
    ISignatureDeclaration/*?*/ containingSignature;

    /// <summary>
    /// 
    /// </summary>
    protected internal virtual IEnumerable<ICustomModifier> CustomModifiers {
      get { return Enumerable<ICustomModifier>.Empty; }
    }

    /// <summary>
    /// A value that should be supplied as the corresponding argument value by callers that do not explicitly specify an argument value for this parameter.
    /// This expression is only correct if it evaluates to a compile time constant.
    /// </summary>
    public Expression DefaultValue {
      get
        //^^ requires this.HasDefaultValue;
        //^ ensures result == this.defaultValue;
      {
        Expression/*?*/ defaultValue = this.defaultValue;
        //^ assume defaultValue != null; //follows from precondition
        return defaultValue;
      }
    }
    //^ [SpecPublic]
    readonly Expression/*?*/ defaultValue;

    /// <summary>
    /// Calls the visitor.Visit(ParameterDeclaration) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    //^ [SpecPublic]
    private readonly int flags;

    /// <summary>
    /// True if the parameter has a default value that should be supplied as the argument value by a caller for which the argument value has not been explicitly specified.
    /// </summary>
    public bool HasDefaultValue {
      get
        //^ ensures this.DefaultValue is ICompileTimeConstant;
      {
        return this.defaultValue is ICompileTimeConstant;
      }
      //TODO: evaluate the expression and return false if the result is not a compile time constant
    }

    /// <summary>
    /// The position in the parameter list where this instance can be found.
    /// </summary>
    /// <value></value>
    public ushort Index {
      get { return this.index; }
    }
    readonly ushort index;

    /// <summary>
    /// True if the argument value must be included in the marshalled arguments passed to a remote callee.
    /// </summary>
    public bool IsIn {
      get { return (this.flags & 1) != 0; }
      //TODO: compute this from the custom attributes
    }

    /// <summary>
    /// This parameter has associated marshalling information.
    /// </summary>
    public bool IsMarshalledExplicitly {
      get { return (this.flags & 4) != 0; }
      //TODO: compute this from the custom attributes
    }

    /// <summary>
    /// The parameter has custom modifiers.
    /// </summary>
    internal protected virtual bool IsModified {
      get { return false; } //TODO: compute this. For example volatile fields have modifiers.
    }

    /// <summary>
    /// True if the argument value must be included in the marshalled arguments passed to a remote callee only if it is different from the default value (if there is one).
    /// </summary>
    public bool IsOptional {
      get { return (this.flags & 16) != 0; }
    }

    /// <summary>
    /// True if the parameter is passed by reference and is always assigned to by the declaring method before it is referenced.
    /// Corresponds to the out modifier in C#.
    /// </summary>
    public bool IsOut {
      get { return (this.flags & 32) != 0; }
    }

    /// <summary>
    /// True if the parameter has the ParamArrayAttribute custom attribute.
    /// </summary>
    public bool IsParameterArray {
      get
        //^ ensures result == ((this.flags & 64) != 0);
      {
        return (this.flags & 64) != 0;
      }
    }

    /// <summary>
    /// True if the parameter is passed by reference. Corresponds to the ref modifier in C#.
    /// </summary>
    public bool IsRef {
      get { return (this.flags & 128) != 0; }
    }

    /// <summary>
    /// True if this parameter is the "this" of an extension method.
    /// </summary>
    public bool IsThis {
      get { return (this.flags & 256) != 0; }
    }

    // ^ [MustOverride]
    /// <summary>
    /// 
    /// </summary>
    /// <param name="containingSignature"></param>
    /// <param name="containingBlock"></param>
    /// <returns></returns>
    public virtual ParameterDeclaration MakeShallowCopyFor(ISignatureDeclaration containingSignature, BlockStatement containingBlock)
      //^ ensures result.GetType() == this.GetType();
    {
      if (this.ContainingSignature == containingSignature) return this;
      return new ParameterDeclaration(containingSignature, containingBlock, this);
    }

    /// <summary>
    /// The name of the entity.
    /// </summary>
    /// <value></value>
    public NameDeclaration Name {
      get { return this.name; }
    }
    readonly NameDeclaration name;

    /// <summary>
    /// The element type of the parameter array.
    /// </summary>
    public ITypeDefinition ParamArrayElementType {
      get
        //^^ requires this.IsParameterArray;
      {
        //^ assume this.type is ArrayTypeExpression; //follows from the invariant
        ArrayTypeExpression type = (ArrayTypeExpression)this.type;
        return type.ElementType.ResolvedType;
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    protected virtual ParameterDefinition CreateParameterDefinition() {
      return new ParameterDefinition(this);
    }

    /// <summary>
    /// 
    /// </summary>
    public ParameterDefinition ParameterDefinition {
      get {
        if (this.parameterDefinition == null) {
          lock (GlobalLock.LockingObject) {
            if (this.parameterDefinition == null)
              this.parameterDefinition = CreateParameterDefinition();
          }
        }
        return this.parameterDefinition;
      }
    }
    ParameterDefinition/*?*/ parameterDefinition;

    /// <summary>
    /// Custom attributes that are explicitly specified in source. Some of these may not end up in persisted metadata.
    /// </summary>
    /// <value></value>
    public IEnumerable<SourceCustomAttribute> SourceAttributes {
      get {
        List<SourceCustomAttribute> sourceAttributes;
        if (this.sourceAttributes == null)
          yield break;
        else
          sourceAttributes = this.sourceAttributes;
        for (int i = 0, n = sourceAttributes.Count; i < n; i++) {
          //yield return sourceAttributes[i] = sourceAttributes[i].MakeShallowCopyFor(this.ContainingSignature.DummyBlock);
          yield return sourceAttributes[i];
        }
      }
    }
    readonly List<SourceCustomAttribute>/*?*/ sourceAttributes;

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a parameter before constructing the containing method or delegate.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public virtual void SetContainingSignatureAndExpression(ISignatureDeclaration containingSignature, Expression containingExpression) {
      this.containingSignature = containingSignature;
      if (this.sourceAttributes != null)
        foreach (SourceCustomAttribute attribute in this.sourceAttributes) attribute.SetContainingExpression(containingExpression);
      this.type.SetContainingExpression(containingExpression);
      if (this.defaultValue != null) this.defaultValue.SetContainingExpression(containingExpression);
    }

    /// <summary>
    /// The type of argument value that corresponds to this parameter.
    /// </summary>
    public TypeExpression Type {
      get { return this.type; }
    }
    readonly TypeExpression type;
    //^ invariant this.IsParameterArray ==> type is ArrayTypeExpression;

    #region INamedEntity Members

    IName INamedEntity.Name {
      get { return this.Name; }
    }

    #endregion

  }

  /// <summary>
  /// A property is a member that provides access to an attribute of an object or a class.
  /// This interface models the source representation of a property.
  /// </summary>
  public class PropertyDeclaration : TypeDeclarationMember, ISignatureDeclaration {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sourceAttributes"></param>
    /// <param name="flags"></param>
    /// <param name="visibility"></param>
    /// <param name="type"></param>
    /// <param name="implementedInterfaces"></param>
    /// <param name="name"></param>
    /// <param name="parameters"></param>
    /// <param name="getterAttributes"></param>
    /// <param name="getterBody"></param>
    /// <param name="getterVisibility"></param>
    /// <param name="setterAttributes"></param>
    /// <param name="setterBody"></param>
    /// <param name="setterVisibility"></param>
    /// <param name="sourceLocation"></param>
    public PropertyDeclaration(List<SourceCustomAttribute>/*?*/ sourceAttributes,
      Flags flags, TypeMemberVisibility visibility, TypeExpression type, List<TypeExpression>/*?*/ implementedInterfaces, NameDeclaration name, List<ParameterDeclaration>/*?*/ parameters,
      List<SourceCustomAttribute>/*?*/ getterAttributes, BlockStatement/*?*/ getterBody, TypeMemberVisibility getterVisibility,
      List<SourceCustomAttribute>/*?*/ setterAttributes, BlockStatement/*?*/ setterBody, TypeMemberVisibility setterVisibility, ISourceLocation sourceLocation)
      : base(sourceAttributes, (TypeDeclarationMember.Flags)flags, visibility, name, sourceLocation) {
      this.getterAttributes = getterAttributes;
      this.getterBody = getterBody;
      this.flags |= (int)(getterVisibility & TypeMemberVisibility.Mask) << 4;
      this.flags |= (int)(setterVisibility & TypeMemberVisibility.Mask) << 8;
      this.implementedInterfaces = implementedInterfaces;
      this.parameters = parameters;
      this.setterAttributes = setterAttributes;
      this.setterBody = setterBody;
      this.type = type;
    }

    /// <summary>
    /// 
    /// </summary>
    [Flags]
    public new enum Flags { //Must remain same as EventDeclaration.Flags and be a prefix of MethodDeclaration.Flags
      /// <summary>
      /// 
      /// </summary>
      New=int.MinValue,
      /// <summary>
      /// 
      /// </summary>
      Unsafe=(New>>1)&int.MaxValue,

      /// <summary>
      /// 
      /// </summary>
      Abstract=Unsafe>>1,
      /// <summary>
      /// 
      /// </summary>
      External=Abstract>>1,
      /// <summary>
      /// 
      /// </summary>
      Override=External>>1,
      /// <summary>
      /// 
      /// </summary>
      Sealed=Override>>1,
      /// <summary>
      /// 
      /// </summary>
      Static=Sealed>>1,
      /// <summary>
      /// 
      /// </summary>
      Virtual=Static>>1

    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="containingTypeDeclaration"></param>
    /// <param name="template"></param>
    protected PropertyDeclaration(TypeDeclaration containingTypeDeclaration, PropertyDeclaration template)
      : base(containingTypeDeclaration, template)
      //^ ensures this.containingTypeDeclaration == containingTypeDeclaration;
    {
      //^ assert this.containingTypeDeclaration == containingTypeDeclaration;
      if (template.getterAttributes != null)
        this.getterAttributes = new List<SourceCustomAttribute>(template.getterAttributes);
      if (template.getterBody != null)
        this.getterBody = (BlockStatement)template.getterBody.MakeCopyFor(containingTypeDeclaration.DummyBlock);
      if (template.implementedInterfaces != null)
        this.implementedInterfaces = new List<TypeExpression>(template.implementedInterfaces);
      if (template.parameters != null)
        this.parameters = new List<ParameterDeclaration>(template.parameters);
      if (template.setterAttributes != null)
        this.setterAttributes = new List<SourceCustomAttribute>(template.setterAttributes);
      if (template.setterBody != null)
        this.setterBody = (BlockStatement)template.setterBody.MakeCopyFor(containingTypeDeclaration.DummyBlock);
      this.type = (TypeExpression)template.type.MakeCopyFor(containingTypeDeclaration.DummyBlock);
      //^ assume this.containingTypeDeclaration == containingTypeDeclaration;
      //^ assume this.getter == null;
      //^ assume this.setter == null;
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the property or a constituent part of the property.
    /// Do not call this method directly, but evaluate the HasErrors property. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return false;
    }

    /// <summary>
    /// A compile time constant value that provides the default value for the property. (Who uses this and why?)
    /// </summary>
    public Expression DefaultValue {
      get {
        //^ assume false; //this.HasDefaultValue must return false unless this routine is overridden.
        Expression/*?*/ defaultValue = null;
        //^ assume defaultValue != null; //follows from assumption above.
        return defaultValue;
      }
    }

    /// <summary>
    /// Calls the visitor.Visit(PropertyDeclaration) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The method used to get the value of this property. May be absent (null).
    /// </summary>
    public MethodDeclaration/*?*/ Getter {
      get {
        BlockStatement/*?*/ getterBody = this.GetterBody;
        if (getterBody == null) return null;
        if (this.getter != null) return this.getter;
        lock (GlobalLock.LockingObject) {
          if (this.getter != null) return this.getter;
          ISourceLocation sourceLocation = this.Name.SourceLocation;
          TypeExpression typeExpr = (TypeExpression)this.Type.MakeCopyFor(this.ContainingTypeDeclaration.DummyBlock);
          NameDeclaration name = new NameDeclaration(this.NameTable.GetNameFor("get_"+this.Name.Value), sourceLocation);
          List<ParameterDeclaration>/*?*/ parameters = null;
          if (this.parameters != null) parameters = new List<ParameterDeclaration>(this.Parameters);
          //TODO: worry about attributes defined on the property and targeting methods
          MethodDeclaration.Flags flags = (MethodDeclaration.Flags)(this.flags&0xFFFFF000);
          flags |= MethodDeclaration.Flags.SpecialName;
          this.getter = new MethodDeclaration(this.getterAttributes, flags, this.Visibility, typeExpr, this.implementedInterfaces, name, null, parameters, null, getterBody, this.SourceLocation);
          this.getter.SetContainingTypeDeclaration(this.ContainingTypeDeclaration, false);
        }
        return this.getter;
      }
    }
    MethodDeclaration/*?*/ getter;
    //^ invariant this.getter == null || this.getter.ContainingTypeDeclaration == this.ContainingTypeDeclaration;

    /// <summary>
    /// Custom attributes that apply to the method that is used to get the value of the property.
    /// </summary>
    public IEnumerable<SourceCustomAttribute> GetterAttributes {
      get {
        List<SourceCustomAttribute> getterAttributes;
        if (this.getterAttributes == null)
          yield break;
        else
          getterAttributes = this.getterAttributes;
        for (int i = 0, n = getterAttributes.Count; i < n; i++) {
          yield return getterAttributes[i] = getterAttributes[i].MakeShallowCopyFor(this.ContainingTypeDeclaration.DummyBlock);
        }
      }
    }
    readonly List<SourceCustomAttribute>/*?*/ getterAttributes;

    /// <summary>
    /// The body of the method used to get the value of this property.
    /// </summary>
    public BlockStatement/*?*/ GetterBody {
      get { return this.getterBody; }
    }
    readonly BlockStatement/*?*/ getterBody;

    /// <summary>
    /// Indicates if the getter is public or confined to its containing type, derived types and/or declaring assembly. May be different from the property.
    /// </summary>
    public TypeMemberVisibility GetterVisibility {
      get { return (TypeMemberVisibility)(this.flags >> 4) & TypeMemberVisibility.Mask; }
    }

    /// <summary>
    /// True if this property has a compile time constant associated with it that serves as a default value for the property. (Who uses this and why?)
    /// </summary>
    public virtual bool HasDefaultValue {
      get { return false; }
    }

    /// <summary>
    /// A list of interfaces whose corresponding abstract properties are implemented by this property.
    /// </summary>
    public IEnumerable<TypeExpression> ImplementedInterfaces {
      get {
        List<TypeExpression> implementedInterfaces;
        if (this.implementedInterfaces == null)
          yield break;
        else
          implementedInterfaces = this.implementedInterfaces;
        for (int i = 0, n = implementedInterfaces.Count; i < n; i++) {
          yield return implementedInterfaces[i] = (TypeExpression)implementedInterfaces[i].MakeCopyFor(this.ContainingTypeDeclaration.DummyBlock);
        }
      }
    }
    readonly List<TypeExpression>/*?*/ implementedInterfaces;

    /// <summary>
    /// True if the methods to get and set the value of this property are abstract.
    /// </summary>
    public bool IsAbstract {
      get { return (this.flags & (int)Flags.Abstract) != 0; }
    }

    /// <summary>
    /// True if the methods to get and set the value of this property are defined externally.
    /// </summary>
    public bool IsExternal {
      get { return (this.flags & (int)Flags.External) != 0; }
    }

    /// <summary>
    /// True if the methods to get and set the value of this property are overriding base class methods.
    /// </summary>
    public bool IsOverride {
      get { return (this.flags & (int)Flags.Override) != 0; }
    }

    /// <summary>
    /// True if the property gets special treatment from the runtime.
    /// </summary>
    public virtual bool IsRuntimeSpecial {
      get { return false; }
    }

    /// <summary>
    /// True if this property is special in some way, as specified by the name.
    /// </summary>
    public virtual bool IsSpecialName {
      get { return false; }
    }

    /// <summary>
    /// True if the methods to get and set the value of this property are sealed.
    /// </summary>
    public bool IsSealed {
      get { return (this.flags & (int)Flags.Sealed) != 0; }
    }

    /// <summary>
    /// True if the methods to get and set the value of this property are static.
    /// </summary>
    public bool IsStatic {
      get { return (this.flags & (int)Flags.Static) != 0; }
    }

    /// <summary>
    /// True if the methods to get and set the value of this property are virtual.
    /// </summary>
    public bool IsVirtual {
      get { return (this.flags & (int)Flags.Virtual) != 0; }
    }

    /// <summary>
    /// Makes a shallow copy of this member that can be added to the member list of the given target type declaration.
    /// The shallow copy may share child objects with this instance, but should never expose such child objects except through
    /// wrappers (or shallow copies made on demand). If this instance is already a member of the target type declaration it
    /// returns itself.
    /// </summary>
    //^ [MustOverride, Pure]
    public override TypeDeclarationMember MakeShallowCopyFor(TypeDeclaration targetTypeDeclaration) {
      if (targetTypeDeclaration == this.ContainingTypeDeclaration) return this;
      return new PropertyDeclaration(targetTypeDeclaration, this);
    }

    /// <summary>
    /// A list of methods that are associated with the property.
    /// </summary>
    public virtual IEnumerable<MethodDeclaration> Accessors {
      get {
        MethodDeclaration/*?*/ getter = this.Getter;
        if (getter != null) yield return getter;
        MethodDeclaration/*?*/ setter = this.Setter;
        if (setter != null) yield return setter;
      }
    }

    /// <summary>
    /// The parameters forming part of this signature.
    /// </summary>
    /// <value></value>
    public IEnumerable<ParameterDeclaration> Parameters {
      get {
        List<ParameterDeclaration> parameters;
        if (this.parameters == null)
          yield break;
        else
          parameters = this.parameters;
        for (int i = 0, n = parameters.Count; i < n; i++)
          yield return parameters[i] = parameters[i].MakeShallowCopyFor(this, this.ContainingTypeDeclaration.DummyBlock);
      }
    }
    readonly List<ParameterDeclaration>/*?*/ parameters;

    /// <summary>
    /// The symbol table object that represents the metadata for this property.
    /// </summary>
    public PropertyDefinition PropertyDefinition {
      get {
        if (this.propertyDefinition == null) {
          lock (GlobalLock.LockingObject) {
            if (this.propertyDefinition == null)
              this.propertyDefinition = new PropertyDefinition(this);
          }
        }
        return this.propertyDefinition;
      }
    }
    PropertyDefinition/*?*/ propertyDefinition;

    /// <summary>
    /// The name of this property, qualified with the name of an interface, if this property is the private implementation
    /// of an interface property.
    /// </summary>
    internal protected virtual IName QualifiedName {
      get {
        if (this.qualifiedName == null) {
          if (this.implementedInterfaces == null || this.implementedInterfaces.Count == 0)
            this.qualifiedName = this.Name.Name;
          else
            this.qualifiedName = this.Helper.NameTable.GetNameFor(this.implementedInterfaces[0].SourceLocation.Source+"."+this.Name.Value);
        }
        return this.qualifiedName;
      }
    }
    /// <summary>
    /// 
    /// </summary>
    protected IName/*?*/ qualifiedName;

    /// <summary>
    /// Custom attributes associated with the property's return value.
    /// </summary>
    public virtual IEnumerable<ICustomAttribute> ReturnValueAttributes {
      get { return Enumerable<ICustomAttribute>.Empty; } //TODO: implement this
    }

    /// <summary>
    /// Returns the list of custom modifiers, if any, associated with the returned value. Evaluate this property only if ReturnValueIsModified is true.
    /// </summary>
    public virtual IEnumerable<ICustomModifier> ReturnValueCustomModifiers {
      get { return Enumerable<ICustomModifier>.Empty; }
    }

    /// <summary>
    /// True if the getter return value is passed by reference (using a managed pointer).
    /// </summary>
    public virtual bool ReturnValueIsByRef {
      get { return false; }
    }

    /// <summary>
    /// True if the getter return value has one or more custom modifiers associated with it.
    /// </summary>
    public virtual bool ReturnValueIsModified {
      get { return false; }
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a type member before constructing the containing type declaration.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingTypeDeclaration(TypeDeclaration containingTypeDeclaration, bool recurse) {
      base.SetContainingTypeDeclaration(containingTypeDeclaration, recurse);
      //^ assert this.ContainingTypeDeclaration == containingTypeDeclaration;
      if (!recurse) return;
      BlockStatement containingBlock = containingTypeDeclaration.DummyBlock;
      DummyExpression containingExpression = new DummyExpression(containingBlock, SourceDummy.SourceLocation);
      if (this.getterAttributes != null)
        foreach (SourceCustomAttribute attribute in this.getterAttributes) attribute.SetContainingExpression(containingExpression);
      if (this.getterBody != null)
        this.getterBody.SetContainers(containingBlock, this);
      if (this.implementedInterfaces != null)
        foreach (TypeExpression implementedInterface in this.implementedInterfaces) implementedInterface.SetContainingExpression(containingExpression);
      if (this.parameters != null)
        foreach (ParameterDeclaration parameter in this.parameters) parameter.SetContainingSignatureAndExpression(this, containingExpression);
      if (this.setterAttributes != null)
        foreach (SourceCustomAttribute attribute in this.setterAttributes) attribute.SetContainingExpression(containingExpression);
      if (this.setterBody != null)
        this.setterBody.SetContainers(containingBlock, this);
      this.type.SetContainingExpression(containingExpression);
      //^ assume this.ContainingTypeDeclaration == containingTypeDeclaration;
    }

    /// <summary>
    /// The method used to set the value of this property. May be absent (null).
    /// </summary>
    public MethodDeclaration/*?*/ Setter {
      get {
        BlockStatement/*?*/ setterBody = this.SetterBody;
        if (setterBody == null) return null;
        if (this.setter != null) return this.setter;
        lock (GlobalLock.LockingObject) {
          if (this.setter != null) return this.setter;
          ISourceLocation sourceLocation = this.SetterBody.SourceLocation;
          TypeExpression voidExpr = TypeExpression.For(this.PlatformType.SystemVoid.ResolvedType);
          NameDeclaration name = new NameDeclaration(this.NameTable.GetNameFor("set_"+this.Name.Value), sourceLocation);
          List<ParameterDeclaration> parameters;
          if (this.parameters != null) {
            parameters = new List<ParameterDeclaration>(this.parameters.Count+1);
            foreach (ParameterDeclaration indexerParameter in this.Parameters) parameters.Add(indexerParameter);
          } else
            parameters = new List<ParameterDeclaration>(1);
          NameDeclaration pname = new NameDeclaration(this.NameTable.value, sourceLocation);
          parameters.Add(new ParameterDeclaration(null, this.Type, pname, null, (ushort)parameters.Count, false, false, false, false, this.Type.SourceLocation));
          //TODO: worry about setter attributes defined on the property itself
          MethodDeclaration.Flags flags = (MethodDeclaration.Flags)(this.flags&0xFFFFF000);
          flags |= MethodDeclaration.Flags.SpecialName;
          this.setter = new MethodDeclaration(this.setterAttributes, flags, this.Visibility, voidExpr, this.implementedInterfaces, name,
            null, parameters, null, setterBody, sourceLocation);
          this.setter.SetContainingTypeDeclaration(this.ContainingTypeDeclaration, false);
          setterBody.SetContainingBlock(this.setter.DummyBlock);
        }
        return this.setter;
      }
    }
    MethodDeclaration/*?*/ setter;
    //^ invariant this.setter == null || this.setter.ContainingTypeDeclaration == this.ContainingTypeDeclaration;

    /// <summary>
    /// Custom attributes that apply to the method that is used to set the value of the property.
    /// </summary>
    public IEnumerable<SourceCustomAttribute> SetterAttributes {
      get {
        List<SourceCustomAttribute> setterAttributes;
        if (this.setterAttributes == null)
          yield break;
        else
          setterAttributes = this.setterAttributes;
        for (int i = 0, n = setterAttributes.Count; i < n; i++) {
          yield return setterAttributes[i] = setterAttributes[i].MakeShallowCopyFor(this.ContainingTypeDeclaration.DummyBlock);
        }
      }
    }
    readonly List<SourceCustomAttribute>/*?*/ setterAttributes;

    /// <summary>
    /// The body of the method used to set the value of this property.
    /// </summary>
    public BlockStatement/*?*/ SetterBody {
      get { return this.setterBody; }
    }
    readonly BlockStatement/*?*/ setterBody;

    /// <summary>
    /// Indicates if the getter is public or confined to its containing type, derived types and/or declaring assembly. May be different from the property.
    /// </summary>
    public TypeMemberVisibility SetterVisibility {
      get { return (TypeMemberVisibility)(this.flags >> 8) & TypeMemberVisibility.Mask; }
    }

    /// <summary>
    /// The symbol table object that represents the metadata for this property.
    /// </summary>
    public ISignature SignatureDefinition {
      get { return this.PropertyDefinition; }
    }

    /// <summary>
    /// An expression that denotes the type of the property.
    /// </summary>
    public TypeExpression Type {
      get { return this.type; }
    }
    readonly TypeExpression type;

    /// <summary>
    /// The symbol table object that represents the metadata for this member.
    /// </summary>
    public override TypeDefinitionMember TypeDefinitionMember {
      get { return this.PropertyDefinition; }
    }

  }

  /// <summary>
  /// Represents a block of section.
  /// </summary>
  internal unsafe sealed class StaticDataSectionBlock : ISectionBlock {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="offset"></param>
    /// <param name="data"></param>
    internal StaticDataSectionBlock(uint offset, byte[] data) {
      this.offset = offset;
      this.data = data;
    }

    /// <summary>
    /// Section where the block resides.
    /// </summary>
    /// <value></value>
    public PESectionKind PESectionKind {
      get { return PESectionKind.StaticData; }
    }

    /// <summary>
    /// Offset into section where the block resides.
    /// </summary>
    /// <value></value>
    public uint Offset {
      get { return this.offset; }
    }
    readonly uint offset;

    /// <summary>
    /// Size of the block.
    /// </summary>
    /// <value></value>
    public uint Size {
      get { return (uint)this.data.Length; }
    }

    /// <summary>
    /// Byte information stored in the block.
    /// </summary>
    /// <value></value>
    public IEnumerable<byte> Data {
      get { return this.data; }
    }
    readonly byte[] data;

  }

  /// <summary>
  /// 
  /// </summary>
  public class SignatureDeclaration : SourceItem, ISignatureDeclaration {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="type"></param>
    /// <param name="parameters"></param>
    /// <param name="sourceLocation"></param>
    public SignatureDeclaration(TypeExpression type, List<ParameterDeclaration> parameters, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.type = type;
      this.parameters = parameters;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected SignatureDeclaration(BlockStatement containingBlock, SignatureDeclaration template)
      : base(template.SourceLocation) {
      this.type = (TypeExpression)template.Type.MakeCopyFor(containingBlock);
      this.parameters = new List<ParameterDeclaration>(template.Parameters);
    }

    /// <summary>
    /// Calls the visitor.Visit(xxxxx) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
    }

    /// <summary>
    /// Makes a shallow copy of this signature.
    /// </summary>
    public SignatureDeclaration MakeShallowCopyFor(BlockStatement containingBlock) {
      if (this.type.ContainingBlock == containingBlock) return this;
      return new SignatureDeclaration(containingBlock, this);
    }

    /// <summary>
    /// The parameters forming part of this signature.
    /// </summary>
    /// <value></value>
    public IEnumerable<ParameterDeclaration> Parameters {
      get { return this.parameters.AsReadOnly(); }
    }
    readonly List<ParameterDeclaration> parameters;

    /// <summary>
    /// 
    /// </summary>
    protected internal virtual IEnumerable<CustomAttribute> ReturnValueAttributes {
      get { return Enumerable<CustomAttribute>.Empty; } //TODO: compute this
    }

    /// <summary>
    /// 
    /// </summary>
    protected internal virtual IEnumerable<ICustomModifier> ReturnValueCustomModifiers {
      get { return Enumerable<ICustomModifier>.Empty; } //TODO: compute this
    }

    /// <summary>
    /// 
    /// </summary>
    protected internal virtual bool ReturnValueIsByRef {
      get { return false; }
    }

    /// <summary>
    /// 
    /// </summary>
    protected internal virtual bool ReturnValueIsModified {
      get { return false; } //TODO: compute this
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="containingBlock"></param>
    public virtual void SetContainingBlock(BlockStatement containingBlock) {
      DummyExpression containingExpression = new DummyExpression(containingBlock, SourceDummy.SourceLocation);
      this.Type.SetContainingExpression(containingExpression);
      foreach (ParameterDeclaration parameter in this.parameters)
        parameter.SetContainingSignatureAndExpression(this, containingExpression);
    }

    /// <summary>
    /// 
    /// </summary>
    public SignatureDefinition SignatureDefinition {
      get {
        if (this.signatureDefinition == null) {
          lock (GlobalLock.LockingObject) {
            if (this.signatureDefinition == null)
              this.signatureDefinition = new SignatureDefinition(this);
          }
        }
        return this.signatureDefinition;
      }
    }
    SignatureDefinition/*?*/ signatureDefinition;

    /// <summary>
    /// 
    /// </summary>
    public TypeExpression Type {
      get { return this.type; }
    }
    readonly TypeExpression type;

    #region ISignatureDeclaration Members

    ISignature ISignatureDeclaration.SignatureDefinition {
      get { return this.SignatureDefinition; }
    }

    #endregion
  }

  /// <summary>
  /// A member of a type declaration, such as a field or a method.
  /// </summary>
  public abstract class TypeDeclarationMember : SourceItemWithAttributes, IAggregatableTypeDeclarationMember, ITypeDeclarationMember {

    /// <summary>
    /// Initializes a member of a type declaration, such as a field or a method.
    /// </summary>
    /// <param name="sourceAttributes">Custom attributes that are explicitly specified in source. Some of these may not end up in persisted metadata.
    /// For example in C# a custom attribute is used to specify IFieldDefinition.IsNotSerialized. This custom attribute is deleted by the compiler.</param>
    /// <param name="isNew">Indicates that this member is intended to hide the name of an inherited member.</param>
    /// <param name="isUnsafe">True if the member exposes an unsafe type, such as a pointer.</param>
    /// <param name="visibility">Indicates if the member is public or confined to its containing type, derived types and/or declaring assembly.</param>
    /// <param name="name">The name of the member. </param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    protected TypeDeclarationMember(List<SourceCustomAttribute>/*?*/ sourceAttributes, bool isNew, bool isUnsafe, TypeMemberVisibility visibility,
      NameDeclaration name, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.sourceAttributes = sourceAttributes;
      this.name = name;
      int flags = (int)visibility;
      if (isNew) flags |= (int)Flags.New;
      if (isUnsafe) flags |= (int)Flags.Unsafe;
      this.flags = flags;
    }

    /// <summary>
    /// Initializes a member of a type declaration, such as a field or a method.
    /// </summary>
    /// <param name="sourceAttributes">Custom attributes that are explicitly specified in source. Some of these may not end up in persisted metadata.
    /// For example in C# a custom attribute is used to specify IFieldDefinition.IsNotSerialized. This custom attribute is deleted by the compiler.</param>
    /// <param name="flags">A set of flags that specify the value of boolean properties of the member, such as IsNew.</param>
    /// <param name="visibility">Indicates if the member is public or confined to its containing type, derived types and/or declaring assembly.</param>
    /// <param name="name">The name of the member. </param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    protected TypeDeclarationMember(List<SourceCustomAttribute>/*?*/ sourceAttributes, Flags flags, TypeMemberVisibility visibility,
      NameDeclaration name, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.sourceAttributes = sourceAttributes;
      this.name = name;
      this.flags = ((int)visibility)|(int)flags;
    }

    /// <summary>
    /// 
    /// </summary>
    [Flags]
    protected enum Flags {
      /// <summary>
      /// 
      /// </summary>
      New=int.MinValue,
      /// <summary>
      /// 
      /// </summary>
      Unsafe=(New>>1)&int.MaxValue,
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing type.
    /// </summary>
    /// <param name="containingTypeDeclaration">The containing type of the copied member. This should be different from the containing type of the template member.</param>
    /// <param name="template">The type member to copy.</param>
    protected TypeDeclarationMember(TypeDeclaration containingTypeDeclaration, TypeDeclarationMember template)
      : base(template.SourceLocation)
      //^ ensures this.containingTypeDeclaration == containingTypeDeclaration;
    {
      ISourceDocument containingDocument = containingTypeDeclaration.SourceLocation.SourceDocument;
      ISourceLocation templateLocation = template.SourceLocation;
      if (containingDocument.IsUpdatedVersionOf(templateLocation.SourceDocument))
        this.sourceLocation = containingDocument.GetCorrespondingSourceLocation(templateLocation);
      this.containingTypeDeclaration = containingTypeDeclaration;
      if (template.sourceAttributes != null)
        this.sourceAttributes = new List<SourceCustomAttribute>(template.sourceAttributes);
      this.name = template.Name.MakeCopyFor(containingTypeDeclaration.Compilation);
      this.flags = template.flags;
      //^ assume this.containingTypeDeclaration == containingTypeDeclaration;
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the member or a constituent part of the member.
    /// Do not call this method directly, but evaluate the HasErrors property. The latter will cache the return value.
    /// </summary>
    protected abstract bool CheckForErrorsAndReturnTrueIfAnyAreFound();

    /// <summary>
    /// The compilation that this declaration forms a part of.
    /// </summary>
    public Compilation Compilation {
      get { return this.ContainingTypeDeclaration.Compilation; }
    }

    /// <summary>
    /// The compilation part that this declaration forms a part of.
    /// </summary>
    public CompilationPart CompilationPart {
      get { return this.ContainingTypeDeclaration.CompilationPart; }
    }

    /// <summary>
    /// The type declaration that contains this member.
    /// </summary>
    public TypeDeclaration ContainingTypeDeclaration {
      get
        //^ ensures result == this.containingTypeDeclaration;
      {
        //^ assume this.containingTypeDeclaration != null;
        return this.containingTypeDeclaration;
      }
    }
    //^ [SpecPublic]
    /// <summary>
    /// The type declaration that contains this member.
    /// </summary>
    protected TypeDeclaration/*?*/ containingTypeDeclaration;

    /// <summary>
    /// 
    /// </summary>
    protected int flags;

    /// <summary>
    /// Returns a list of custom attributes that describes this type declaration member.
    /// Typically, these will be derived from this.SourceAttributes. However, some source attributes
    /// might instead be persisted as metadata bits and other custom attributes may be synthesized
    /// from information not provided in the form of source custom attributes.
    /// The list is not trimmed to size, since an override of this method may call the base method
    /// and then add more attributes.
    /// </summary>
    protected override List<ICustomAttribute> GetAttributes() {
      var result = new List<ICustomAttribute>();
      foreach (var sourceAttribute in this.SourceAttributes) {
        if (sourceAttribute.HasErrors) continue;
        if (TypeHelper.TypesAreEquivalent(sourceAttribute.Type.ResolvedType, this.Helper.PlatformType.SystemRuntimeInteropServicesDllImportAttribute))
          continue;
        //TODO: ignore source attribute if it is not meant to be persisted.
        result.Add(new CustomAttribute(sourceAttribute));
      }
      return result;
    }

    /// <summary>
    /// Returns the visibility that applies by default to this member if no visibility was supplied in the source code.
    /// </summary>
    //^ [Pure]
    public virtual TypeMemberVisibility GetDefaultVisibility() {
      if (this.ContainingTypeDeclaration is IInterfaceDeclaration) return TypeMemberVisibility.Public;
      return TypeMemberVisibility.Private;
    }

    /// <summary>
    /// If no source attributes were supplied to the constructor, this method will get called to supply them when
    /// the SourceAttributes property is evaluated. If this method returns null, it will be called every time
    /// the property is evaluated. If this is too expensive, be sure to return an empty list rather than null.
    /// </summary>
    protected virtual List<SourceCustomAttribute>/*?*/ GetSourceAttributes() {
      return null;
    }

    /// <summary>
    /// Checks the member for errors and returns true if any were found.
    /// </summary>
    public bool HasErrors {
      get {
        if (this.hasErrors == null)
          this.hasErrors = this.CheckForErrorsAndReturnTrueIfAnyAreFound();
        return this.hasErrors.Value;
      }
    }
    bool? hasErrors;

    /// <summary>
    /// An instance of a language specific class containing methods that are of general utility. 
    /// </summary>
    public LanguageSpecificCompilationHelper Helper {
      get { return this.ContainingTypeDeclaration.Helper; }
    }

    /// <summary>
    /// Indicates that this member is intended to hide the name of an inherited member.
    /// </summary>
    public bool IsNew {
      get { return (this.flags & (int)Flags.New) != 0; }
    }

    /// <summary>
    /// True if the member exposes an unsafe type, such as a pointer.
    /// </summary>
    public bool IsUnsafe {
      get { return (this.flags & (int)Flags.Unsafe) != 0; }
    }

    /// <summary>
    /// Makes a shallow copy of this member that can be added to the member list of the given target type declaration.
    /// The shallow copy may share child objects with this instance, but should never expose such child objects except through
    /// wrappers (or shallow copies made on demand). If this instance is already a member of the target type declaration it
    /// returns itself.
    /// </summary>
    //^ [Pure]
    public abstract TypeDeclarationMember MakeShallowCopyFor(TypeDeclaration targetTypeDeclaration);
    //^ requires targetTypeDeclaration.GetType() == this.ContainingTypeDeclaration.GetType();
    //^ ensures result.GetType() == this.GetType();
    //^ ensures result.ContainingTypeDeclaration == targetTypeDeclaration;

    /// <summary>
    /// The name of the member. 
    /// </summary>
    public NameDeclaration Name {
      get { return this.name; }
    }
    readonly NameDeclaration name;

    /// <summary>
    /// A table used to intern strings used as names. This table is obtained from the host environment.
    /// It is mutuable, in as much as it is possible to add new names to the table.
    /// </summary>
    public INameTable NameTable {
      get { return this.Compilation.NameTable; }
    }

    /// <summary>
    /// A collection of well known types that must be part of every target platform and that are fundamental to modeling compiled code.
    /// The types are obtained by querying the unit set of the compilation and thus can include types that are defined by the compilation itself.
    /// </summary>
    public PlatformType PlatformType {
      get { return this.Compilation.PlatformType; }
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a type member before constructing the containing type declaration.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public virtual void SetContainingTypeDeclaration(TypeDeclaration containingTypeDeclaration, bool recurse)
      //^ ensures this.ContainingTypeDeclaration == containingTypeDeclaration;
      //^ modifies this.*;
    {
      this.containingTypeDeclaration = containingTypeDeclaration;
      if (!recurse) return;
      DummyExpression containingExpression = new DummyExpression(containingTypeDeclaration.DummyBlock, SourceDummy.SourceLocation);
      if (this.sourceAttributes != null)
        foreach (SourceCustomAttribute attribute in this.sourceAttributes) attribute.SetContainingExpression(containingExpression);
    }

    /// <summary>
    /// Custom attributes that are explicitly specified in source. Some of these may not end up in persisted metadata.
    /// For example in C# a custom attribute is used to specify IFieldDefinition.IsNotSerialized. This custom attribute is deleted by the compiler.
    /// </summary>
    public IEnumerable<SourceCustomAttribute> SourceAttributes {
      get {
        if (this.sourceAttributes == null)
          this.sourceAttributes = this.GetSourceAttributes();
        if (this.sourceAttributes != null) {
          for (int i = 0, n = this.sourceAttributes.Count; i < n; i++) {
            //^ assume this.sourceAttributes != null;
            yield return this.sourceAttributes[i] = this.sourceAttributes[i].MakeShallowCopyFor(this.ContainingTypeDeclaration.DummyBlock);
          }
        }
      }
    }
    List<SourceCustomAttribute>/*?*/ sourceAttributes;

    /// <summary>
    /// The symbol table object that represents the metadata for this member.
    /// </summary>
    public abstract TypeDefinitionMember TypeDefinitionMember {
      get;
    }

    /// <summary>
    /// Indicates if the member is public or confined to its containing type, derived types and/or declaring assembly.
    /// </summary>
    public TypeMemberVisibility Visibility {
      get { return (TypeMemberVisibility)(this.flags & (int)TypeMemberVisibility.Mask); }
    }

    #region ITypeDeclarationMember Members

    TypeDeclaration ITypeDeclarationMember.ContainingTypeDeclaration {
      get { return this.ContainingTypeDeclaration; }
    }

    ITypeDeclarationMember ITypeDeclarationMember.MakeShallowCopyFor(TypeDeclaration targetTypeDeclaration)
      //^^ requires targetTypeDeclaration.GetType() == this.ContainingTypeDeclaration.GetType();
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingTypeDeclaration == targetTypeDeclaration;
    {
      //^ assume targetTypeDeclaration.GetType() == this.ContainingTypeDeclaration.GetType(); //follows from the precondition
      TypeDeclarationMember result = this.MakeShallowCopyFor((TypeDeclaration)targetTypeDeclaration);
      //^ assume result.ContainingTypeDeclaration == ((ITypeDeclarationMember)result).ContainingTypeDeclaration;
      return result;
    }

    ITypeDefinitionMember/*?*/ ITypeDeclarationMember.TypeDefinitionMember {
      get { return this.TypeDefinitionMember; }
    }

    #endregion

    #region IContainerMember<TypeDeclaration> Members

    IName IContainerMember<TypeDeclaration>.Name {
      get { return this.Name; }
    }

    #endregion

    #region IAggregatableTypeDeclarationMember Members

    ITypeDefinitionMember IAggregatableTypeDeclarationMember.AggregatedMember {
      get { return this.TypeDefinitionMember; }
    }

    #endregion

    #region IContainerMember<TypeDeclaration> Members

    TypeDeclaration IContainerMember<TypeDeclaration>.Container {
      get { return this.ContainingTypeDeclaration; }
    }

    #endregion

    #region INamedEntity Members

    IName INamedEntity.Name {
      get { return this.Name; }
    }

    #endregion

  }

}
