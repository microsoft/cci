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
using System.Collections.Generic;
using System.Diagnostics;
using System;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci.Ast {
  /// <summary>
  /// Error information relating to a portion of a source document. This class is used for errors reported by the AST base classes.
  /// </summary>
  public sealed class AstErrorMessage : ErrorMessage {

    /// <summary>
    /// Allocates an object providing error information relating to a portion of a source document.
    /// </summary>
    /// <param name="sourceItem">The source item to which the error applies.</param>
    /// <param name="error">An enumeration code that corresponds to this error. The enumeration identifier is used as the message key and the enumeration value is used as the code.</param>
    /// <param name="messageArguments">Zero or more strings that are to be substituted for "{i}" sequences in the message string return by GetMessage.</param>
    public AstErrorMessage(ISourceItem sourceItem, Error error, params string[] messageArguments)
      : base(sourceItem.SourceLocation, (long)error, error.ToString(), messageArguments) {
    }

    /// <summary>
    /// Allocates an object providing error information relating to a portion of a source document.
    /// </summary>
    /// <param name="sourceItem">The source item to which the error applies.</param>
    /// <param name="error">An enumeration code that corresponds to this error. The enumeration identifier is used as the message key and the enumeration value is used as the code.</param>
    /// <param name="relatedLocations">Zero ore more locations that are related to this error.</param>
    /// <param name="messageArguments">Zero or more strings that are to be substituted for "{i}" sequences in the message string return by GetMessage.</param>
    public AstErrorMessage(ISourceItem sourceItem, Error error, IEnumerable<ILocation> relatedLocations, params string[] messageArguments)
      : base(sourceItem.SourceLocation, (long)error, error.ToString(), relatedLocations, messageArguments) {
    }

    /// <summary>
    /// Allocates an object providing error information relating to a portion of a source document.
    /// </summary>
    /// <param name="sourceLocation">The location of the error in the source document.</param>
    /// <param name="errorCode">A code that corresponds to this error. This code is the same for all cultures.</param>
    /// <param name="messageKey">A string that is used as the key when looking for the localized error message using a resource manager.</param>
    /// <param name="relatedLocations">Zero ore more locations that are related to this error.</param>
    /// <param name="messageArguments">Zero or more strings that are to be substituted for "{i}" sequences in the message string return by GetMessage.</param>
    private AstErrorMessage(ISourceLocation sourceLocation, long errorCode, string messageKey, IEnumerable<ILocation> relatedLocations, string[] messageArguments)
      : base(sourceLocation, errorCode, messageKey, relatedLocations, messageArguments) {
    }

    /// <summary>
    /// The object reporting the error. This can be used to filter out errors coming from non interesting sources.
    /// </summary>
    public override object ErrorReporter {
      get { return SemanticErrorReporter.Instance; }
    }

    /// <summary>
    /// A short identifier for the reporter of the error, suitable for use in human interfaces. For example "CS" in the case of a C# language error.
    /// </summary>
    public override string ErrorReporterIdentifier {
      get { return "CciAst"; }
    }

    /// <summary>
    /// True if the error message should be treated as an informational warning rather than as an indication that the associated
    /// compilation has failed and no useful executable output has been generated. The value of this property does
    /// not depend solely on this.Code but can be influenced by compiler options such as the csc /warnaserror option.
    /// </summary>
    public override bool IsWarning {
      get {
        switch ((Error)this.Code) {
          case Error.BadReferenceCompareLeft:
          case Error.BadReferenceCompareRight:
          case Error.ConstInReadsOrWritesClause:
          case Error.PotentialUnintendRangeComparison:
          case Error.ExpressionStatementHasNoSideEffect:
            return true;
        }
        return false;
      }
    }

    /// <summary>
    /// Makes a copy of this error message, changing only Location and SourceLocation to come from the
    /// given source document. Returns the same instance if the given source document is the same
    /// as this.SourceLocation.SourceDocument.
    /// </summary>
    /// <param name="targetDocument">The document to which the resulting error message must refer.</param>
    public override ISourceErrorMessage MakeShallowCopy(ISourceDocument targetDocument)
      //^^ requires targetDocument == this.SourceLocation.SourceDocument || targetDocument.IsUpdatedVersionOf(this.SourceLocation.SourceDocument);
      //^^ ensures targetDocument == this.SourceLocation.SourceDocument ==> result == this;
    {
      if (this.SourceLocation.SourceDocument == targetDocument) return this;
      return new AstErrorMessage(targetDocument.GetCorrespondingSourceLocation(this.SourceLocation), this.Code, this.MessageKey, this.RelatedLocations, this.MessageArguments());
    }

    /// <summary>
    /// A description of the error suitable for user interaction. Localized to the current culture.
    /// </summary>
    public override string Message {
      get {
        System.Resources.ResourceManager rmgr = new System.Resources.ResourceManager("Microsoft.Cci.Ast.ErrorMessages", typeof(AstErrorMessage).Assembly);
        return this.GetMessage(rmgr);
      }
    }
  }

  /// <summary>
  /// An enumeration of errors that are reported by the AST base classes.
  /// </summary>
  public enum Error {

    /// <summary>
    /// A placeholder to indicate no error.
    /// </summary>
    NotAnError = 0,

    /// <summary>
    /// Alias '{0}' not found.
    /// </summary>
    AliasNotFound,

    /// <summary>
    /// The call is ambiguous between the following methods or properties: '{0}' and '{1}'.
    /// </summary>
    AmbiguousCall,

    /// <summary>
    /// The left-hand side of an assignment must be a variable, property or indexer.
    /// </summary>
    AssignmentLeftHandValueExpected,

    /// <summary>
    /// The best overloaded method match for '{0}' has some invalid arguments.
    /// </summary>
    BadArgumentTypes,

    /// <summary>
    /// Argument '{0}': cannot convert from '{1}' to '{2}'.
    /// </summary>
    BadArgumentType,

    /// <summary>
    /// Operator '{0}' cannot be applied to operands of type '{1}' and '{2}'.
    /// </summary>
    BadBinaryOperation,

    /// <summary>
    /// The extern alias '{0}' was not specified in a /reference option.
    /// </summary>
    BadExternAlias,

    /// <summary>
    /// No overload for method '{0}' takes '{1}' arguments.
    /// </summary>
    BadNumberOfArguments,

    /// <summary>
    /// Possible unintended reference comparison; to get a value comparison, cast the left hand side to type '{0}'.
    /// </summary>
    BadReferenceCompareLeft,

    /// <summary>
    /// Possible unintended reference comparison; to get a value comparison, cast the right hand side to type '{0}'.
    /// </summary>
    BadReferenceCompareRight,

    /// <summary>
    /// '{0}' has the wrong return type.
    /// </summary>
    BadReturnType,

    /// <summary>
    /// Operator '{0}' cannot be applied to operand of type '{1}'.
    /// </summary>
    BadUnaryOperation,

    /// <summary>
    /// '{0}' is a '{1}' but is used like a '{2}'.
    /// </summary>
    BadUseOfSymbol,

    /// <summary>
    /// Could not read option batch file '{0}'. {1}
    /// </summary>
    BatchFileNotRead,

    /// <summary>
    /// '{0}' is not a method and cannot be called.
    /// </summary>
    CannotCallNonMethod,

    /// <summary>
    /// The type arguments for method '{0}' cannot be inferred from the usage. Try specifying the type arguments explicitly.
    /// </summary>
    CantInferMethTypeArgs,

    /// <summary>
    /// Constant value '{0}' in reads/writes clause is meaningless; this is probably not what you wanted.
    /// </summary>
    ConstInReadsOrWritesClause,

    /// <summary>
    /// Constant value '{0}' cannot be converted to a '{1}'.
    /// </summary>
    ConstOutOfRange,

    /// <summary>
    /// Constant value '{0}' cannot be converted to a '{1}' (use 'unchecked' syntax to override).
    /// </summary>
    ConstOutOfRangeChecked,

    /// <summary>
    /// Response file '{0}' included multiple times.
    /// </summary>
    DuplicateResponseFile,

    /// <summary>
    /// Explicit array size {0} does not match array initializer dimension {1}.
    /// </summary>
    ExplicitSizeDoesNotMatchInitializer,

    /// <summary>
    /// Evaluating this expression has the side effect of modifying memory, which is not permitted in this context.
    /// </summary>
    ExpressionHasSideEffect,

    /// <summary>
    /// The expression '{0}' has no side effect; expected operation with side effect.
    /// </summary>
    ExpressionStatementHasNoSideEffect,

    /// <summary>
    /// Extension methods can only be defined on static, non-nested classes
    /// </summary>
    ExtensionMethodsOnlyInStaticClass,

    /// <summary>
    /// Extension methods can only be defined on static, non-generic classes
    /// </summary>
    ExtensionMethodsOnlyInNonGenericClass,

    /// <summary>
    /// '{0}' is inaccessible due to its protection level.
    /// </summary>
    InaccessibleTypeMember,

    /// <summary>
    /// This initializer element count {1} is different from count {0} in first element.
    /// </summary>
    InitializerCountInconsistent,
  
    /// <summary>
    /// Code page '{0}' is invalid or not installed.
    /// </summary>
    InvalidCodePage,

    /// <summary>
    /// Invalid option: '{0}'.
    /// </summary>
    InvalidCompilerOption,

    /// <summary>
    /// Error while accessing file or path '{0}' - {1}
    /// </summary>
    InvalidFileOrPath,

    /// <summary>
    /// '{0}' is a binary file instead of a source code file.
    /// </summary>
    IsBinaryFile,

    /// <summary>
    /// Label '{0}' can not be found within the scope of the goto statement.
    /// </summary>
    LabelNotFound,

    /// <summary>
    /// Array sizes must be int32 constants if an initializer is supplied.
    /// </summary>
    MustBeConstInt,

    /// <summary>
    /// The name '{0}' does not exist in the current context.
    /// </summary>
    NameNotInContext,

    /// <summary>
    /// Cannot convert type '{0}' to '{1}'.
    /// </summary>
    NoExplicitConversion,

    /// <summary>
    /// Cannot implicitly convert type '{0}' to '{1}'. An explicit conversion exists (are you missing a cast?)
    /// </summary>
    NoImplicitConvCast,

    /// <summary>
    /// Cannot implicitly convert type '{0}' to '{1}'.
    /// </summary>
    NoImplicitConversion,

    /// <summary>
    /// Cannot implicitly convert value '{0}' to type '{1}'.
    /// </summary>
    NoImplicitConversionForValue,

    /// <summary>
    /// No overload for '{1}' matches delegate '{0}'.
    /// </summary>
    NoMatchingOverload,

    /// <summary>
    /// No source files to compile.
    /// </summary>
    NoSourceFiles,

    /// <summary>
    /// File '{0}' does not exist.
    /// </summary>
    NoSuchFile,

    /// <summary>
    /// '{0}' does not contain a definition for '{1}'.
    /// </summary>
    NoSuchMember,

    /// <summary>
    /// Static member '{0}' cannot be accessed with an instance reference; qualify it with a type name instead.
    /// </summary>
    ObjectProhibited,

    /// <summary>
    /// An object reference is required for the nonstatic field, method, or property '{0}'.
    /// </summary>
    ObjectRequired,

    /// <summary>
    /// Reference to out parameter '{0}' not allowed in this context.
    /// </summary>
    OutParameterReferenceNotAllowedHere,

    /// <summary>
    /// The * or -&gt; operator must be applied to a pointer.
    /// </summary>
    PointerExpected,

    /// <summary>
    /// '{0}' probably does not express what you intended; use two conjoined conditions to express an interval or parenthesize the {1} comparison.
    /// </summary>
    PotentialUnintendRangeComparison,

    /// <summary>
    /// The type or namespace name '{0}' could not be found (are you missing a using directive or an assembly reference?)
    /// </summary>
    SingleTypeNameNotFound,

    /// <summary>
    /// Source file '{0}' could not be read. {1}.
    /// </summary>
    SourceFileNotRead,

    /// <summary>
    /// Source file '{0}' is too large to be compiled.
    /// </summary>
    SourceFileTooLarge,

    /// <summary>
    /// Cannot take the address of the given expression.
    /// </summary>
    CannotTakeAddress,

    /// <summary>
    /// Type of conditional expression cannot be determined.
    /// </summary>
    CannotInferTypeOfConditional,

    /// <summary>
    /// Type of conditional expression cannot be determined because there are implicit conversions between '{0}' and '{1}'; try adding an explicit cast to one of the arguments.
    /// </summary>
    CannotInferTypeOfConditionalDueToAmbiguity,

    /// <summary>
    /// The operation in question is undefined on void pointers.
    /// </summary>
    UndefinedOperationOnVoidPointers,

    /// <summary>
    /// {0} does not contain a constructor with {1} arguments.
    /// </summary>
    WrongNumberOfArgumentsInConstructorCall,

    /// <summary>
    /// '{0}' : illegal use of type '{1}'.	
    /// </summary>
    IllegalUseOfType,

    /// <summary>
    /// The type or namespace name '{1}' does not exist in the namespace '{0}' (are you missing an assembly reference?)
    /// </summary>
    TypeNameNotFound,

    /// <summary>
    /// Not an actual error message, but a convenient place holder during development.
    /// </summary>
    ToBeDefined
  }

}
