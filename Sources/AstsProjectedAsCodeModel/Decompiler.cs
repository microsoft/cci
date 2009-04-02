using System;
using System.Collections.Generic;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci.Framework {

  public class DecompiledMethodBody : BlockStatement {

    public DecompiledMethodBody(MethodDeclaration containingMethodDeclaration, ISourceLocation sourceLocation)
      : base(new DecompiledStatementList(containingMethodDeclaration), sourceLocation) {
    }

  }

  /// <summary>
  /// A lazy statement list that is meant be part of a decompiled method body.
  /// Use this class ONLY if you are writing the constructor of the DecompiledMethodBody class.
  /// </summary>
  internal class DecompiledStatementList : IEnumerable<Statement> {

    readonly MethodDeclaration containingMethodDeclaration;

    /// <summary>
    /// A lazy statement list that is meant be part of a decompiled method body.
    /// Use this class ONLY if you are writing the constructor of the DecompiledMethodBody class.
    /// </summary>
    /// <param name="containingMethodDeclaration">The definition associated with this method must return the DecompiledMethodBody instance of which this list is a part.</param>
    internal DecompiledStatementList(MethodDeclaration containingMethodDeclaration) {
      this.containingMethodDeclaration = containingMethodDeclaration;
    }

    private IEnumerable<Statement> Statements {
      get {
        if (this.statements == null) {
          IMethodDefinition method = this.containingMethodDeclaration.MethodDefinition;
          IBlockStatement body = method.Body.Block;
          //^ assume body is DecompiledMethodBody; 
          Decompiler decompiler = new Decompiler(method, (DecompiledMethodBody)body, this.containingMethodDeclaration.Helper);
          this.statements = decompiler.DecompileInstructionsToStatementList();
        }
        return this.statements;
      }
    }
    IEnumerable<Statement>/*?*/ statements;

    #region IEnumerable<Statement> Members

    public IEnumerator<Statement> GetEnumerator() {
      return this.Statements.GetEnumerator();
    }

    #endregion

    #region IEnumerable Members

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
      return this.GetEnumerator();
    }

    #endregion
  }

  internal class StatementGroup {

    internal readonly List<Statement> Statements = new List<Statement>();

  }

  internal class Decompiler {

    //TODO: Spec# these fields are representational and can do with invariants defined on the objects they point to.
    //However, the collections contain objects that are peers to this object.

    struct BranchTargetInfo {
      internal int targetInstructionIndex;
      internal IName targetLabel;
    }

    readonly Dictionary<int, BranchTargetInfo> branchTargetOffsetToBranchTargetInfoMap = new Dictionary<int, BranchTargetInfo>();
    readonly Dictionary<int, StatementGroup> instructionOffsetToStatementGroupMap;
    readonly Dictionary<int, int> tryIndexToHandlerIndexMap = new Dictionary<int, int>();
    readonly Stack<Expression> operandStack = new Stack<Expression>();
    readonly Stack<int> tryIndexStack = new Stack<int>();

    ISourceLocation currentSourceLocation;
    StatementGroup currentStatementGroup;
    BlockStatement containingBlock;
    List<Statement> containingBlockStatements;

    readonly IMethodDefinition method;
    readonly LanguageSpecificCompilationHelper helper;
    readonly List<ICilInstruction> instructions;

    internal Decompiler(IMethodDefinition method, DecompiledMethodBody body, LanguageSpecificCompilationHelper helper) {
      this.method = method;
      this.helper = helper;
      this.containingBlock = body;
      this.containingBlockStatements = new List<Statement>();
      this.currentSourceLocation = body.SourceLocation;
      StatementGroup currentStatementGroup = new StatementGroup();
      this.currentStatementGroup = currentStatementGroup;
      Dictionary<int, StatementGroup> instructionOffsetToStatementGroupMap = new Dictionary<int, StatementGroup>();
      this.instructionOffsetToStatementGroupMap = instructionOffsetToStatementGroupMap;
      instructionOffsetToStatementGroupMap.Add(0, currentStatementGroup);
      this.instructions = new List<ICilInstruction>(method.Body.Instructions);
    }

    private void AddStatement(Statement statement) {
      this.containingBlockStatements.Add(statement);
      this.currentStatementGroup.Statements.Add(statement);
      if (this.method == null) return; //Dummy use to shut up FxCop
    }

    private void ConstructAdd(bool checkOverflow, bool operandsAreUnsigned) 
      //^ requires operandsAreUnsigned ==> checkOverflow;
    {
      if (this.operandStack.Count < 2) return; //badly formed IL. TODO: error message
      Expression rightOperand = this.operandStack.Pop();
      Expression leftOperand = this.operandStack.Pop();
      SourceLocationBuilder slb = new SourceLocationBuilder(leftOperand.SourceLocation);
      slb.UpdateToSpan(rightOperand.SourceLocation);
      this.operandStack.Push(new Addition(leftOperand, rightOperand, checkOverflow, operandsAreUnsigned, slb.GetSourceLocation()));
    }

    private int ConstructLabeledStatementGroups(int instructionIndex)
      //^ requires 0 <= instructionIndex && instructionIndex < this.instructions.Count;
    {
      for (; instructionIndex < this.instructions.Count; instructionIndex++) {
        ICilInstruction instruction = this.instructions[instructionIndex];
        if (this.instructionOffsetToStatementGroupMap.ContainsKey(instruction.Offset)) {
          //Have been to this instruction before.
          return instructionIndex;
        }
        this.currentSourceLocation = instruction.SourceLocation;
        if (this.branchTargetOffsetToBranchTargetInfoMap.ContainsKey(instruction.Offset)) 
          this.StartNewStatementGroup(instruction);
        if (instruction.IsBranch)
          return this.ConstructBranch(instructionIndex, instruction);
        switch (instruction.OpCode) {
          case CilOpCode.Add:
            this.ConstructAdd(false, false);
            break;
          case CilOpCode.Add_Ovf:
            this.ConstructAdd(true, false);
            break;
          case CilOpCode.Add_Ovf_Un:
            this.ConstructAdd(true, true);
            break;
          case CilOpCode.EndOfFilter:
          case CilOpCode.EndOfHandler:
            return instructionIndex;
          case CilOpCode.Pop:
            this.ConstructExpressionStatement();
            break;
          case CilOpCode.Try:
            instructionIndex = this.ConstructTryStatement(instructionIndex);
            break;
          default:
            break;
        }
      }
      return 0;
    }

    private int ConstructBranch(int instructionIndex, ICilInstruction instruction)
      //^ requires 0 <= instructionIndex && instructionIndex < this.instructions.Count;
      //^ requires this.instructions[instructionIndex] == instruction;
      //^ requires instruction.IsBranch;
    {
      int targetOffset = (int)instruction.Value;
      //FindBranchTargetsAndHandlers should have created a branchTargetInfo entry for every branch instruction, therefore:
      //^ assume this.branchTargetOffsetToBranchTargetInfoMap.ContainsKey(targetOffset); 
      BranchTargetInfo branchTargetInfo = this.branchTargetOffsetToBranchTargetInfoMap[targetOffset];
      SimpleName label = new SimpleName(branchTargetInfo.targetLabel, this.currentSourceLocation, false);
      GotoStatement gotoStatement = new GotoStatement(label, this.currentSourceLocation);
      Expression/*?*/ condition = this.ConstructBranchCondition(instruction);
      if (condition == null) {
        this.AddStatement(gotoStatement);
        return this.ConstructLabeledStatementGroups(branchTargetInfo.targetInstructionIndex);
      }
      EmptyStatement emptyStatement = new EmptyStatement(this.currentSourceLocation);
      ConditionalStatement ifStatement = new ConditionalStatement(condition, gotoStatement, emptyStatement, this.currentSourceLocation);
      this.AddStatement(ifStatement);
      this.ConstructLabeledStatementGroups(branchTargetInfo.targetInstructionIndex);
      return instructionIndex+1;
    }

    private Expression/*?*/ ConstructBranchCondition(ICilInstruction instruction)
      //^ requires instruction.IsBranch;
    {
      int stackSize = this.operandStack.Count;
      if (stackSize < 1) return null; //TODO: error message
      if (stackSize < 2) {
        Expression operand = this.operandStack.Pop();
        switch (instruction.OpCode) {
          case CilOpCode.Brfalse:
          case CilOpCode.Brfalse_S:
            return null; //TODO: op == null or op == 0 as the case may be
          case CilOpCode.Brtrue:
          case CilOpCode.Brtrue_S:
            return operand; //TODO: op != null or op != 0 as the case may be
        }
      }
      Expression operand2 = this.operandStack.Pop();
      Expression operand1 = this.operandStack.Pop();
      switch (instruction.OpCode) {
        case CilOpCode.Beq:
        case CilOpCode.Beq_S:
        case CilOpCode.Bge:
        case CilOpCode.Bge_S:
        case CilOpCode.Bge_Un:
        case CilOpCode.Bge_Un_S:
        case CilOpCode.Bgt:
        case CilOpCode.Bgt_S:
        case CilOpCode.Bgt_Un:
        case CilOpCode.Bgt_Un_S:
        case CilOpCode.Ble:
        case CilOpCode.Ble_S:
        case CilOpCode.Ble_Un:
        case CilOpCode.Ble_Un_S:
        case CilOpCode.Blt:
        case CilOpCode.Blt_S:
        case CilOpCode.Blt_Un:
        case CilOpCode.Blt_Un_S:
        case CilOpCode.Bne_Un:
        case CilOpCode.Bne_Un_S:
          return null;
        case CilOpCode.Brfalse:
        case CilOpCode.Brfalse_S:
        case CilOpCode.Brtrue:
        case CilOpCode.Brtrue_S:
          this.operandStack.Push(operand1);
          return operand2;
        default:
          return null;
      }
    }

    private void ConstructExpressionStatement() {
      if (this.operandStack.Count < 1) return; //TODO: error message
      Expression operand = this.operandStack.Pop();
      ExpressionStatement statement = new ExpressionStatement(operand);
      this.AddStatement(statement);
    }

    /// <summary>
    /// Constructs a new TryCatch, TryFilter or TryFinally statement from the instructions starting at tryIndex and
    /// returns the index that the caller should increment to continue processing of the instruction stream following the
    /// last instruction incorporated into the try statement. The constructed statement is added to the current block and
    /// the current statement group.
    /// </summary>
    private int ConstructTryStatement(int tryIndex)
      //^ requires 0 <= tryIndex && tryIndex < this.instructions.Count;
      //^ ensures 0 <= result && result < this.instructions.Count;
    {
      int indexOfLastTryBodyInstruction;
      BlockStatement tryBody = this.ConstructTryBody(tryIndex, out indexOfLastTryBodyInstruction);
      ISourceLocation tryStatementSourceLocation = tryBody.SourceLocation; //TODO: use a builder

      int handlerIndex;
      if (!this.tryIndexToHandlerIndexMap.TryGetValue(tryIndex, out handlerIndex)) {
        //^ assume 0 <= indexOfLastTryBodyInstruction && indexOfLastTryBodyInstruction < this.instructions.Count; //TODO: out of band modifies clause for TryGetValue
        return indexOfLastTryBodyInstruction; //The error should have been reported already.
      }

      //^ assume 0 < handlerIndex && handlerIndex < this.instructions.Count; //TODO: it would be nice if an invariant guaranteed this.
      ICilInstruction handlerInstruction = this.instructions[handlerIndex];
      int indexOfLastHandlerInstruction;
      BlockStatement handlerBody;
      Statement tryStatement;
      handlerBody = this.ConstructHandlerBody(handlerIndex, out indexOfLastHandlerInstruction);
      //TODO: update tryStatement source location

      switch (handlerInstruction.OpCode) {
        case CilOpCode.Catch:
          tryStatement = new TryCatchFinallyStatement(tryBody, this.GetCatchClauses(handlerBody), null, tryStatementSourceLocation);
          break;
        case CilOpCode.Filter:
          tryStatement = new TryCatchFinallyStatement(tryBody, new List<CatchClause>(0).AsReadOnly(), null, tryStatementSourceLocation);
          break;
        case CilOpCode.Finally:
          tryStatement = new TryCatchFinallyStatement(tryBody, new List<CatchClause>(0).AsReadOnly(), handlerBody, tryStatementSourceLocation);
          break;
        default:
          //^ assume false; //TODO: report error
          return indexOfLastHandlerInstruction;
      }

      this.AddStatement(tryStatement);
      //^ assume 0 <= indexOfLastHandlerInstruction && indexOfLastHandlerInstruction < this.instructions.Count;
      return indexOfLastHandlerInstruction;
    }

    private IEnumerable<CatchClause> GetCatchClauses(BlockStatement handlerBody){
      // ^ assume handlerBody.Statements is List<Statement>;
      //List<Statement> handlerStatements = (List<Statement>)handlerBody.Statements;
      TypeExpression/*?*/ exceptionType = null;
      NameDeclaration/*?*/ name = null;
      //^ assume exceptionType != null; //TODO: get an actual value
      //^ assume name != null; //TODO: get an actual value
      //TODO: filters
      List<CatchClause> result = new List<CatchClause>(1);
      result.Add(new CatchClause(exceptionType, null, name, handlerBody, handlerBody.SourceLocation));
      return result.AsReadOnly();
    }

    private BlockStatement ConstructHandlerBody(int handlerIndex, out int indexOfLastHandlerInstruction)
      //^ requires 0 < handlerIndex && handlerIndex < this.instructions.Count;
      //^ ensures 0 <= indexOfLastHandlerInstruction && indexOfLastHandlerInstruction < this.instructions.Count;
    {
      ICilInstruction handlerInstruction = this.instructions[handlerIndex];
      ISourceLocation handlerLocation = handlerInstruction.SourceLocation; //TODO: use a location builder
      StatementGroup savedCurrentStatementGroup = this.currentStatementGroup;
      this.StartNewStatementGroup(handlerInstruction);
      List<Statement> savedContainingBlockStatements = this.containingBlockStatements;
      this.containingBlockStatements = new List<Statement>();
      BlockStatement savedContainingBlock = this.containingBlock;
      BlockStatement handlerBody = this.containingBlock = new BlockStatement(this.containingBlockStatements.AsReadOnly(), handlerLocation);
      if (handlerIndex+1 < this.instructions.Count) {
        this.operandStack.Clear(); //TODO: error message if stack is not empty
        indexOfLastHandlerInstruction = this.ConstructLabeledStatementGroups(handlerIndex+1); //If the IL is well formed, this should confine itself to the handler body
      } else
        indexOfLastHandlerInstruction = handlerIndex;
      //TODO: record association between handlerBody and its first statement group
      this.containingBlock = savedContainingBlock;
      this.containingBlockStatements = savedContainingBlockStatements;
      this.currentStatementGroup = savedCurrentStatementGroup;
      return handlerBody;
    }

    private BlockStatement ConstructTryBody(int tryIndex, out int indexOfLastTryBodyInstruction)
      //^ requires 0 <= tryIndex && tryIndex < this.instructions.Count;
      //^ ensures 0 <= indexOfLastTryBodyInstruction && indexOfLastTryBodyInstruction < this.instructions.Count;
    {
      ICilInstruction tryInstruction = this.instructions[tryIndex];
      ISourceLocation tryLocation = tryInstruction.SourceLocation; //TODO: use a location builder
      StatementGroup savedCurrentStatementGroup = this.currentStatementGroup;
      this.StartNewStatementGroup(tryInstruction);
      List<Statement> savedContainingBlockStatements = this.containingBlockStatements;
      this.containingBlockStatements = new List<Statement>();
      BlockStatement savedContainingBlock = this.containingBlock;
      BlockStatement tryBody = this.containingBlock = new BlockStatement(this.containingBlockStatements.AsReadOnly(), tryLocation);
      if (tryIndex+1 < this.instructions.Count) {
        this.operandStack.Clear(); //TODO: error message if stack is not empty
        indexOfLastTryBodyInstruction = this.ConstructLabeledStatementGroups(tryIndex+1); //If the IL is well formed, this should confine itself to the try body
      } else {
        indexOfLastTryBodyInstruction = tryIndex;
      }
      this.containingBlock = savedContainingBlock;
      this.containingBlockStatements = savedContainingBlockStatements;
      this.currentStatementGroup = savedCurrentStatementGroup;
      return tryBody;
    }

    internal IEnumerable<Statement> DecompileInstructionsToStatementList() {
      if (this.instructions.Count > 0) {
        this.FindBranchTargetsAndHandlers();
        //^ assume this.instructions.Count > 0; //TODO: add a modifies clause to FindBranchTargetsAndHandlers so that the verifier can work this out for itself
        this.ConstructLabeledStatementGroups(0);
        this.DecompileStatementGroups();
      }
      return this.containingBlockStatements.AsReadOnly();
    }

    private void DecompileStatementGroups() {
      //At this stage, every reachable instruction is in a statement group
      //and all handlers have been folded into try statements reachable from the first instruction.
      throw new Exception("The method or operation is not implemented.");
    }

    private void FindBranchTargetsAndHandlers() {
      //Find branches and create entries for their targets
      foreach (ICilInstruction instruction in this.instructions){
        if (instruction.IsBranch) 
          this.branchTargetOffsetToBranchTargetInfoMap.Add((int)instruction.Value, new BranchTargetInfo());
      }
      //Fill in the instruction part of the branch target offset to instruction counter map
      int instructionCounter = 0;
      foreach (ICilInstruction instruction in this.instructions) {
        BranchTargetInfo info;
        if (this.branchTargetOffsetToBranchTargetInfoMap.TryGetValue(instruction.Offset, out info)) {
          info.targetInstructionIndex = instructionCounter;
          info.targetLabel = this.helper.Compilation.NameTable.GetNameFor("IL_"+instruction.Offset);
          this.branchTargetOffsetToBranchTargetInfoMap[instruction.Offset] = info;
        }
        if (instruction.OpCode == CilOpCode.Try)
          this.tryIndexStack.Push(instructionCounter);
        else if (instruction.IsStartOfHandler) {
          if (this.tryIndexStack.Count > 0) {
            this.tryIndexToHandlerIndexMap[tryIndexStack.Pop()] = instructionCounter;
          }//TODO: else give an error
        }
        instructionCounter++;
      }
      //TODO: if the tryStack is not empty give an error
    }

    private void StartNewStatementGroup(ICilInstruction instruction){
      this.currentStatementGroup = new StatementGroup();
      this.instructionOffsetToStatementGroupMap.Add(instruction.Offset, this.currentStatementGroup);
      BranchTargetInfo info;
      if (this.branchTargetOffsetToBranchTargetInfoMap.TryGetValue(instruction.Offset, out info)){
        INameDeclaration labelName = new NameDeclaration(info.targetLabel, this.currentSourceLocation);
        EmptyStatement emptyStatement = new EmptyStatement(this.currentSourceLocation);
        this.AddStatement(new LabeledStatement(labelName, emptyStatement, this.currentSourceLocation));
      }
    }
  }

}