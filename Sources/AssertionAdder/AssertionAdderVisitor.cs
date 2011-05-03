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
using System.Text;
using Microsoft.Cci;
using Microsoft.Cci.MutableCodeModel;

namespace Microsoft.Cci {
  /// <summary>
  /// A visitor that traverses a code model and generates explicit assert and assume statements based on implicit checks and assumptions
  /// that are present in the object model. For example, any array index expression implicitly asserts that the array index is within bounds.
  /// The purpose of this visitor is to produce an object model that can be checked by a static checker, without requiring the static checker to
  /// have special cases for all of the implicit assertions and assumes in the code.
  /// </summary>
  /// <remarks>This class is basically obsolete. It should be replaced by a mutator. It is also mostly incomplete.</remarks>
  public class AssertAssumeAdderVisitor : BaseCodeTraverser {

    /// <summary>
    /// Allocates a visitor that traverses a code model and generates explicit assert and assume statements based on implicit checks and assumptions
    /// that are present in the object model. For example, any array index expression implicitly asserts that the array index is within bounds.
    /// The purpose of this visitor is to produce an object model that can be checked by a static checker, without requiring the static checker to
    /// have special cases for all of the implicit assertions and assumes in the code.
    /// </summary>
    /// <param name="nameTable">A collection of IName instances that represent names that are commonly used during compilation.
    /// This is a provided as a parameter to the host environment in order to allow more than one host
    /// environment to co-exist while agreeing on how to map strings to IName instances.</param>
    /// <param name="insertAssumeFalseAtLine"></param>
    public AssertAssumeAdderVisitor(INameTable nameTable, uint? insertAssumeFalseAtLine)
      : base() {
      this.nameTable = nameTable;
      this.insertAssumeFalseAtLine = insertAssumeFalseAtLine;
    }

    /// <summary>
    /// A map from statements to collections of statements that should precede them. The preceding statements will be assert/assume statements
    /// that make explicit the checks that are implicit in the statement they precede.
    /// </summary>
    public IDictionary<IStatement, ICollection<IStatement>> AddedStmts {
      get { return this.addedStmts; }
    }
    readonly IDictionary<IStatement, ICollection<IStatement>> addedStmts = new Dictionary<IStatement, ICollection<IStatement>>();

    private IStatement currentStatement = CodeDummy.Block;
    readonly INameTable nameTable;
    private IUnitSet unitSet = Dummy.UnitSet;
    private readonly uint? insertAssumeFalseAtLine;

    private ICollection<IStatement> GetOrCreateStmtListForStmt(IStatement stmt) {
      ICollection<IStatement>/*?*/ stmts;
      if (!this.addedStmts.TryGetValue(this.currentStatement, out stmts))
        this.addedStmts[stmt] = stmts = new List<IStatement>();
      return stmts;
    }

    private void AddAssertion(IExpression condition) {
      AssertStatement assertStatement = new AssertStatement();
      assertStatement.Condition = condition;
      this.GetOrCreateStmtListForStmt(this.currentStatement).Add(assertStatement);
    }

    private IMethodDefinition GetPointerValidator() { //TODO: rename __vcValid to __pointerIsValid
      IMethodDefinition result = Dummy.Method;
      foreach (INamespaceMember member in this.unitSet.UnitSetNamespaceRoot.GetMembersNamed(this.nameTable.GetNameFor("__vcValid"), false)) {
        IGlobalMethodDefinition/*?*/ glob = member as IGlobalMethodDefinition;
        if (glob == null) continue;
        result = glob;
        break;
      }
      return result;
    }

    private MethodCall GetPointerValidationCall(IExpression pointer) {
      CompileTimeConstant pointerSize = new CompileTimeConstant();
      pointerSize.Type = pointer.Type.PlatformType.SystemInt32.ResolvedType;
      pointerSize.Value = pointer.Type.PlatformType.PointerSize;
      MethodCall mcall = new MethodCall();
      mcall.Arguments.Add(pointer);
      mcall.Arguments.Add(pointerSize);
      mcall.Locations.Add(PointerIsValidationLocation.For(pointerSize, pointer.Locations));
      mcall.MethodToCall = this.PointerValidator;
      mcall.Type = this.PointerValidator.Type;
      return mcall;
    }

    private IMethodDefinition PointerValidator {
      get {
        if (this.pointerValidator == null)
          this.pointerValidator = this.GetPointerValidator();
        return this.pointerValidator;
      }
    }
    IMethodDefinition/*?*/ pointerValidator;

    /// <summary>
    /// Visits the specified assembly.
    /// </summary>
    /// <param name="assembly">The assembly.</param>
    public override void Visit(IAssembly assembly) {
      List<IUnit> units = new List<IUnit>();
      foreach (IUnitReference uref in assembly.UnitReferences) units.Add(uref.ResolvedUnit);
      units.Add(assembly);
      this.unitSet = new Immutable.UnitSet(units.AsReadOnly());
      base.Visit(assembly);
    }

    // currently, the following functionality resides in the ConvertFelt2Boogie visitor

    //public override void Visit(IConversion conversion) {
    //  base.Visit(conversion);
    //  if (!conversion.CheckNumericRange) return;
    //  IExpression valueToConvert = conversion.ValueToConvert;
    //  ITypeDefinition sourceType = valueToConvert.Type;
    //  ITypeDefinition targetType = conversion.TypeAfterConversion.ResolvedType;
    //  ITypeDefinition systemBoolean = targetType.PlatformType.SystemBoolean;
    //  if (!TypeHelper.IsPrimitiveInteger(sourceType) || !TypeHelper.IsPrimitiveInteger(targetType)) return;
    //  CompileTimeConstant lowerBound = new CompileTimeConstant();
    //  CompileTimeConstant upperBound = new CompileTimeConstant();
    //  lowerBound.Type = targetType;
    //  upperBound.Type = targetType;
    //  switch (targetType.TypeCode) {
    //    case PrimitiveTypeCode.UInt8:
    //      lowerBound.Value = byte.MinValue;
    //      upperBound.Value = byte.MaxValue;
    //      break;
    //    case PrimitiveTypeCode.Int8:
    //      lowerBound.Value = sbyte.MinValue;
    //      upperBound.Value = sbyte.MaxValue;
    //      break;
    //    case PrimitiveTypeCode.Char:
    //    case PrimitiveTypeCode.UInt16:
    //      lowerBound.Value = ushort.MinValue;
    //      upperBound.Value = ushort.MaxValue;
    //      break;
    //    case PrimitiveTypeCode.Int16:
    //      lowerBound.Value = short.MinValue;
    //      upperBound.Value = short.MaxValue;
    //      break;
    //    case PrimitiveTypeCode.UInt32:
    //      lowerBound.Value = uint.MinValue;
    //      if (sourceType.TypeCode == PrimitiveTypeCode.Int32)
    //        upperBound.Value = int.MaxValue;
    //      else
    //        upperBound.Value = uint.MaxValue;
    //      break;
    //    case PrimitiveTypeCode.Int32:
    //      if (sourceType.TypeCode == PrimitiveTypeCode.UInt32)
    //        lowerBound.Value = uint.MinValue;
    //      else
    //        lowerBound.Value = int.MinValue;
    //      upperBound.Value = int.MaxValue;
    //      break;
    //    case PrimitiveTypeCode.UInt64:
    //      if (sourceType.TypeCode != PrimitiveTypeCode.Int64) return;
    //      lowerBound.Value = ulong.MinValue;
    //      upperBound.Value = long.MaxValue;
    //      break;
    //    case PrimitiveTypeCode.Int64:
    //      if (sourceType.TypeCode != PrimitiveTypeCode.UInt64) return;
    //      lowerBound.Value = long.MinValue;
    //      upperBound.Value = long.MaxValue;
    //      break;
    //    default:
    //      return;
    //  }

    //  Conversion convertedLowerBound = new Conversion();
    //  convertedLowerBound.ValueToConvert = lowerBound;
    //  convertedLowerBound.Type = sourceType;
    //  convertedLowerBound.TypeAfterConversion = sourceType;

    //  LessThanOrEqual le = new LessThanOrEqual();
    //  le.Locations.Add(LowerboundAssertionLocation.For(lowerBound, conversion.Locations));
    //  le.LeftOperand = convertedLowerBound;
    //  le.RightOperand = valueToConvert;
    //  le.Type = systemBoolean;

    //  this.AddAssertion(le);

    //  Conversion convertedUpperBound = new Conversion();
    //  convertedUpperBound.ValueToConvert = upperBound;
    //  convertedUpperBound.Type = sourceType;
    //  convertedUpperBound.TypeAfterConversion = sourceType;

    //  GreaterThanOrEqual ge = new GreaterThanOrEqual();
    //  ge.Locations.Add(UpperboundAssertionLocation.For(upperBound, conversion.Locations));
    //  ge.LeftOperand = convertedUpperBound;
    //  ge.RightOperand = valueToConvert;
    //  ge.Type = systemBoolean;

    //  this.AddAssertion(ge);
    //}

    /// <summary>
    /// Visits the specified method.
    /// </summary>
    /// <param name="method">The method.</param>
    public override void Visit(IMethodDefinition method) {
      //TODO: if the method is the main method of the assembly, then add assumptions that the global variables have been initialized and not yet modified.
      base.Visit(method);
      if (!method.IsAbstract && !method.IsExternal)
        this.Visit(method.Body);
    }

    /// <summary>
    /// Visits the specified module.
    /// </summary>
    /// <param name="module">The module.</param>
    public override void Visit(IModule module) {
      List<IUnit> units = new List<IUnit>();
      foreach (IUnitReference uref in module.UnitReferences) units.Add(uref.ResolvedUnit);
      units.Add(module);
      this.unitSet = new Immutable.UnitSet(units.AsReadOnly());
      base.Visit(module);
    }

    /// <summary>
    /// Visits the specified pointer call.
    /// </summary>
    /// <param name="pointerCall">The pointer call.</param>
    public override void Visit(IPointerCall pointerCall) {
      base.Visit(pointerCall);
      this.AddAssertion(this.GetPointerValidationCall(pointerCall.Pointer));
    }

    /// <summary>
    /// Visits the specified statement.
    /// </summary>
    /// <param name="statement">The statement.</param>
    public override void Visit(IStatement statement) {
      IStatement oldCurrentStatement = this.currentStatement;
      this.currentStatement = statement;
      base.Visit(statement);
      this.currentStatement = oldCurrentStatement;
    }

    static void GetLocationLineSpan(IPrimarySourceLocation loc, out uint startLine, out uint endLine) {
      IIncludedSourceLocation/*?*/ iloc = loc as IIncludedSourceLocation;
      if (iloc != null) {
        startLine = (uint)iloc.OriginalStartLine;
        endLine = (uint)iloc.OriginalEndLine;
      } else {
        startLine = (uint)loc.StartLine;
        endLine = (uint)loc.EndLine;
      }
    }

    /// <summary>
    /// Visits the specified block.
    /// </summary>
    /// <param name="block">The block.</param>
    public override void Visit(IBlockStatement block) {
      if (this.insertAssumeFalseAtLine != null) {
        uint startLine;
        uint endLine;
        GetLocationLineSpan(GetPrimarySourceLocationFrom(block.Locations), out startLine, out endLine);
        if (startLine <= this.insertAssumeFalseAtLine.Value &&
           this.insertAssumeFalseAtLine.Value < endLine) {
          foreach (IStatement stmt in block.Statements) {
            GetLocationLineSpan(GetPrimarySourceLocationFrom(stmt.Locations), out startLine, out endLine);
            GetPrimarySourceLocationFrom(stmt.Locations);
            if (this.insertAssumeFalseAtLine.Value < endLine) {
              AssumeStatement assumeFalse = new AssumeStatement();
              CompileTimeConstant constFalse = new CompileTimeConstant();
              constFalse.Value = false;
              assumeFalse.Condition = constFalse;
              this.GetOrCreateStmtListForStmt(stmt).Add(assumeFalse);
              break;
            }
          }
        }
      }
      base.Visit(block);
    }

    /// <summary>
    /// Visits the specified unit.
    /// </summary>
    /// <param name="unit">The unit.</param>
    public override void Visit(IUnit unit) {
      List<IUnit> units = new List<IUnit>();
      foreach (IUnitReference uref in unit.UnitReferences) units.Add(uref.ResolvedUnit);
      units.Add(unit);
      this.unitSet = new Immutable.UnitSet(units.AsReadOnly());
      base.Visit(unit);
    }

    internal static IPrimarySourceLocation GetPrimarySourceLocationFrom(IEnumerable<ILocation> locations) {
      IPrimarySourceLocation/*?*/ ploc = null;
      foreach (ILocation location in locations) {
        ploc = location as IPrimarySourceLocation;
        if (ploc != null) break;
        IDerivedSourceLocation/*?*/ dloc = location as IDerivedSourceLocation;
        if (dloc != null) {
          foreach (IPrimarySourceLocation dploc in dloc.PrimarySourceLocations) {
            ploc = dploc;
            break;
          }
        }
      }
      if (ploc == null) ploc = SourceDummy.PrimarySourceLocation;
      return ploc;
    }
  }

  internal class AssertionAdderSourceLocationWrapper : IIncludedSourceLocation {

    protected AssertionAdderSourceLocationWrapper(IPrimarySourceLocation wrappedSourceLocation) {
      this.wrappedSourceLocation = wrappedSourceLocation;
    }

    IPrimarySourceLocation wrappedSourceLocation;

    public bool Contains(ISourceLocation location) {
      return this.wrappedSourceLocation.Contains(location);
    }

    public int CopyTo(int offset, char[] destination, int destinationOffset, int length) {
      return this.wrappedSourceLocation.CopyTo(offset, destination, destinationOffset, length);
    }

    public IDocument Document {
      get { return this.wrappedSourceLocation.PrimarySourceDocument; }
    }

    public int EndColumn {
      get { return this.wrappedSourceLocation.EndColumn; }
    }

    public int EndIndex {
      get { return this.wrappedSourceLocation.EndIndex; }
    }

    public int EndLine {
      get { return this.wrappedSourceLocation.EndLine; }
    }

    public int Length {
      get { return this.wrappedSourceLocation.Length; }
    }

    public string OriginalSourceDocumentName {
      get {
        IIncludedSourceLocation/*?*/ includedLoc = this.wrappedSourceLocation as IIncludedSourceLocation;
        if (includedLoc != null)
          return includedLoc.OriginalSourceDocumentName;
        else
          return this.wrappedSourceLocation.SourceDocument.Name.Value;
      }
    }

    public int OriginalEndLine {
      get {
        IIncludedSourceLocation/*?*/ includedLoc = this.wrappedSourceLocation as IIncludedSourceLocation;
        if (includedLoc != null)
          return includedLoc.OriginalEndLine;
        else
          return this.wrappedSourceLocation.EndLine;
      }
    }

    public int OriginalStartLine {
      get {
        IIncludedSourceLocation/*?*/ includedLoc = this.wrappedSourceLocation as IIncludedSourceLocation;
        if (includedLoc != null)
          return includedLoc.OriginalStartLine;
        else
          return this.wrappedSourceLocation.StartLine;
      }
    }

    public IPrimarySourceDocument PrimarySourceDocument {
      get { return this.wrappedSourceLocation.PrimarySourceDocument; }
    }

    public ISourceDocument SourceDocument {
      get { return this.wrappedSourceLocation.PrimarySourceDocument; }
    }

    public virtual string Source {
      get { return this.wrappedSourceLocation.Source; }
    }

    public int StartColumn {
      get { return this.wrappedSourceLocation.StartColumn; }
    }

    public int StartIndex {
      get { return this.wrappedSourceLocation.StartIndex; }
    }

    public int StartLine {
      get { return this.wrappedSourceLocation.StartLine; }
    }

  }

  //internal sealed class LowerboundAssertionLocation : AssertionAdderSourceLocationWrapper {

  //  private LowerboundAssertionLocation(CompileTimeConstant lowerbound, IPrimarySourceLocation expressionLocation)
  //    : base(expressionLocation) {
  //    this.lowerbound = lowerbound;
  //  }

  //  internal static LowerboundAssertionLocation For(CompileTimeConstant lowerbound, IEnumerable<ILocation> locations) {
  //    IPrimarySourceLocation ploc = AssertAssumeAdderVisitor.GetPrimarySourceLocationFrom(locations);
  //    return new LowerboundAssertionLocation(lowerbound, ploc);
  //  }

  //  CompileTimeConstant lowerbound;

  //  public override string Source {
  //    get { 
  //      object/*?*/ lb = this.lowerbound.Value;
  //      return lb + " <= " + base.Source;
  //    }
  //  }
  //}

  internal sealed class PointerIsValidationLocation : AssertionAdderSourceLocationWrapper {
    private PointerIsValidationLocation(CompileTimeConstant pointerSize, IPrimarySourceLocation expressionLocation)
      : base(expressionLocation) {
      this.pointerSize = pointerSize;
    }

    CompileTimeConstant pointerSize;

    internal static PointerIsValidationLocation For(CompileTimeConstant pointerSize, IEnumerable<ILocation> locations) {
      IPrimarySourceLocation ploc = AssertAssumeAdderVisitor.GetPrimarySourceLocationFrom(locations);
      return new PointerIsValidationLocation(pointerSize, ploc);
    }

    public override string Source {
      get {
        object/*?*/ ub = this.pointerSize.Value;
        return "valid("+base.Source+", "+ub+")";
      }
    }
  }

  //internal sealed class UpperboundAssertionLocation : AssertionAdderSourceLocationWrapper {

  //  private UpperboundAssertionLocation(CompileTimeConstant upperbound, IPrimarySourceLocation expressionLocation)
  //    : base(expressionLocation) {
  //    this.upperbound = upperbound;
  //  }

  //  internal static UpperboundAssertionLocation For(CompileTimeConstant upperbound, IEnumerable<ILocation> locations) {
  //    IPrimarySourceLocation ploc = AssertAssumeAdderVisitor.GetPrimarySourceLocationFrom(locations);
  //    return new UpperboundAssertionLocation(upperbound, ploc);
  //  }

  //  CompileTimeConstant upperbound;

  //  public override string Source {
  //    get {
  //      object/*?*/ ub = this.upperbound.Value;
  //      return ub + " >= " + base.Source;
  //    }
  //  }
  //}

}
