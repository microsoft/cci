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
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Microsoft.Cci.UtilityDataStructures;

namespace Microsoft.Cci.Analysis {

  /// <summary>
  /// Provides a static method that modifies a suitable control and data flow graph into a form where every local is assigned to in a single location.
  /// That is, the graph is put into Static Single Assignment (SSA) form. (The "Static" is an attempt to make it clear that a local can be assigned
  /// to many times dynamically (during execution) even though there is a single assignment instruction for it in the graph.)
  /// </summary>
  /// <typeparam name="BasicBlock">A type that is a subtype of Microsoft.Cci.Analysis.SSABasicBlock.</typeparam>
  /// <typeparam name="Instruction">A type that is a subtype of Microsoft.Cci.Analysis.Instruction and that has a default constructor.</typeparam>
  public class SingleAssigner<BasicBlock, Instruction>
    where BasicBlock : SSABasicBlock<Instruction>, new()
    where Instruction : Microsoft.Cci.Analysis.Instruction, new() {

    /// <summary>
    /// Initializes an instance of SingleAssigner.
    /// </summary>
    /// <param name="cdfg">
    /// A set of basic blocks, each of which has a list of successor blocks and some other information.
    /// Each block consists of a list of instructions, each of which can point to previous instructions that compute the operands it consumes.
    /// </param>
    /// <param name="nameTable">
    /// An extensible collection of IName instances that represent names that are commonly used during compilation.
    /// </param>
    /// <param name="cfgQueries"></param>
    /// <param name="sourceLocationProvider">An object that can map some kinds of ILocation objects to IPrimarySourceLocation objects. May be null.</param>
    private SingleAssigner(INameTable nameTable, ControlAndDataFlowGraph<BasicBlock, Instruction> cdfg,
      ControlGraphQueries<BasicBlock, Instruction> cfgQueries, ISourceLocationProvider sourceLocationProvider) {
      Contract.Requires(nameTable != null);
      Contract.Requires(cdfg != null);
      Contract.Requires(cfgQueries != null);

      this.nameTable = nameTable;
      this.cdfg = cdfg;
      this.cfgQueries = cfgQueries;
      this.sourceLocationProvider = sourceLocationProvider;
    }

    /// <summary>
    /// An extensible collection of IName instances that represent names that are commonly used during compilation.
    /// </summary>
    INameTable nameTable;
    /// <summary>
    /// An object that can map some kinds of ILocation objects to IPrimarySourceLocation objects. May be null.
    /// </summary>
    ISourceLocationProvider sourceLocationProvider;
    /// <summary>
    /// Used to make up unique names for the new locals introduced to make all assignments unique.
    /// </summary>
    uint localCounter;
    /// <summary>
    /// A set of basic blocks, each of which has a list of successor blocks and some other information.
    /// Each block consists of a list of instructions, each of which can point to previous instructions that compute the operands it consumes.
    /// </summary>
    ControlAndDataFlowGraph<BasicBlock, Instruction> cdfg;
    /// <summary>
    /// Presents information derived from a simple control flow graph. For example, traversal orders, predecessors, dominators and dominance frontiers.
    /// </summary>
    ControlGraphQueries<BasicBlock, Instruction> cfgQueries;
    /// <summary>
    /// A list of all of the reads (join points or phi nodes) in this.cdfg. Used to avoid allocating new List objects for every block.
    /// </summary>
    List<Join> allReads = new List<Join>();
    /// <summary>
    /// Keeps track of all of the blocks that have already been visited. Used to break cycles during visitation.
    /// </summary>
    SetOfObjects blocksAlreadyVisited = new SetOfObjects();
    /// <summary>
    /// A map from the local (or parameter) used in the original IL to the new local written by the most recent assignment to the original local.
    /// </summary>
    Hashtable<object, object> ssaVariableFor = new Hashtable<object, object>();

    [ContractInvariantMethod]
    private void ObjectInvariant() {
      Contract.Invariant(this.nameTable != null);
      Contract.Invariant(this.cdfg != null);
      Contract.Invariant(this.cfgQueries != null);
      Contract.Invariant(this.allReads != null);
      Contract.Invariant(this.blocksAlreadyVisited != null);
      Contract.Invariant(this.ssaVariableFor != null);
    }

    /// <summary>
    /// Rewrites the blocks in the given cdfg so that every assignment to a local or parameter is to a new local (and thus each local is just
    /// assigned to in exactly one place in the graph). The new names introduced by the writes are connected to the reads in successor blocks
    /// by means of join points (a.k.a. Phi nodes) that are found in the Reads property of an SSABasicBlock.
    /// </summary>
    /// <param name="cdfg">
    /// A set of basic blocks, each of which has a list of successor blocks and some other information.
    /// Each block consists of a list of instructions, each of which can point to previous instructions that compute the operands it consumes.
    /// </param>
    /// <param name="cfgQueries">
    /// Presents information derived from a simple control flow graph. For example, traversal orders, predecessors, dominators and dominance frontiers.
    /// </param>
    /// <param name="nameTable">
    /// An extensible collection of IName instances that represent names that are commonly used during compilation.
    /// </param>
    /// <param name="sourceLocationProvider"></param>
    public static void GetInSingleAssignmentForm(INameTable nameTable, ControlAndDataFlowGraph<BasicBlock, Instruction> cdfg,
      ControlGraphQueries<BasicBlock, Instruction> cfgQueries, ISourceLocationProvider sourceLocationProvider) {
      Contract.Requires(nameTable != null);
      Contract.Requires(cdfg != null);
      Contract.Requires(cfgQueries != null);

      var singleAssigner = new SingleAssigner<BasicBlock, Instruction>(nameTable, cdfg, cfgQueries, sourceLocationProvider);
      singleAssigner.GetInSingleAssignmentForm();
    }

    /// <summary>
    /// Rewrites the blocks in the given cdfg so that every assignment to a local or parameter is to a new local (and thus each local is just
    /// assigned to in exactly one place in the graph). The new names introduced by the writes are connected to the reads in successor blocks
    /// by means of join points (a.k.a. Phi nodes) that are found in the Reads property of an SSABasicBlock.
    /// </summary>
    private void GetInSingleAssignmentForm() {
      foreach (var block in this.cdfg.AllBlocks) {
        Contract.Assume(block != null);
        this.CreateSSAVariablesAndJoinInformation(block);
      }
      foreach (var block in this.cdfg.RootBlocks) {
        Contract.Assume(block != null);
        this.ssaVariableFor.Clear();
        this.ReplaceLocalsWithSSALocals(block, this.ssaVariableFor);
      }
    }

    private void ReplaceLocalsWithSSALocals(BasicBlock block, Hashtable<object, object> ssaVariableFor) {
      Contract.Requires(block != null);
      Contract.Requires(ssaVariableFor != null);

      if (block.Joins != null) {
        foreach (var join in block.Joins) {
          Contract.Assume(join.OriginalLocal != null);
          ssaVariableFor[join.OriginalLocal] = join.NewLocal;
        }
      }

      foreach (var instruction in block.Instructions) {
        Contract.Assume(instruction != null);
        var operation = instruction.Operation;
        switch (operation.OperationCode) {
          case OperationCode.Ldarga:
          case OperationCode.Ldarga_S:
          case OperationCode.Starg:
          case OperationCode.Starg_S:
            Contract.Assume(operation.Value is SSAParameterDefinition);
            var ssaParam = (SSAParameterDefinition)operation.Value;
            ssaVariableFor[ssaParam.OriginalParameter] = ssaParam;
            break;

          case OperationCode.Ldloca:
          case OperationCode.Ldloca_S:
          case OperationCode.Stloc:
          case OperationCode.Stloc_0:
          case OperationCode.Stloc_1:
          case OperationCode.Stloc_2:
          case OperationCode.Stloc_3:
          case OperationCode.Stloc_S:
            Contract.Assume(operation.Value is SSALocalDefinition);
            var ssaLocal = (SSALocalDefinition)operation.Value;
            ssaVariableFor[ssaLocal.OriginalLocal] = ssaLocal;
            break;

          case OperationCode.Ldarg:
          case OperationCode.Ldarg_0:
          case OperationCode.Ldarg_1:
          case OperationCode.Ldarg_2:
          case OperationCode.Ldarg_3:
          case OperationCode.Ldarg_S:
          case OperationCode.Ldloc:
          case OperationCode.Ldloc_0:
          case OperationCode.Ldloc_1:
          case OperationCode.Ldloc_2:
          case OperationCode.Ldloc_3:
          case OperationCode.Ldloc_S:
            var ssaVar = ssaVariableFor[operation.Value??Dummy.ParameterDefinition];
            if (ssaVar != null)
              instruction.Operation = new SSAOperation(operation, ssaVar);
            break;
        }
      }

      var successors = this.cdfg.SuccessorsFor(block);
      var n = successors.Count;
      if (n == 0) return;
      for (var i = 0; i < n; i++) {
        var succ = successors[i];
        if (this.cfgQueries.ImmediateDominator(succ) == block) {
          //Add join information, if necessary
          foreach (var pair in this.ssaVariableFor) {
            if (succ.Joins != null) {
              foreach (var join in succ.Joins) {
                if (join.OriginalLocal == pair.key) {
                  if (join.Join2 == null) {
                    join.Join2 = (INamedEntity)pair.value;
                    join.Block2 = block;
                  } else {
                    var otherJoins = join.OtherJoins;
                    if (otherJoins == null) join.OtherJoins = otherJoins = new List<INamedEntity>();
                    otherJoins.Add((INamedEntity)pair.value);
                    var otherBlocks = join.OtherBlocks;
                    if (otherBlocks == null) join.OtherBlocks = otherBlocks = new List<object>();
                    otherBlocks.Add(block);
                  }
                  break;
                }
              }
            }
          }
        }
      }
      for (var i = 0; i < n-1; i++) {
        var succ = successors[i];
        if (!this.blocksAlreadyVisited.Add(succ)) continue;
        var copyOfssaVariableFor = new Hashtable<object, object>(ssaVariableFor);
        this.ReplaceLocalsWithSSALocals(succ, copyOfssaVariableFor);
      }
      var lastSuccessor = successors[n-1];
      //Contract.Assume(lastSuccessor != null);
      if (this.blocksAlreadyVisited.Add(lastSuccessor))
        this.ReplaceLocalsWithSSALocals(lastSuccessor, ssaVariableFor);
    }

    /// <summary>
    /// Runs through the instructions of the given block and updates any instruction that references a local or parameter
    /// to instead reference a SSA local or parameter.
    /// </summary>
    private void CreateSSAVariablesAndJoinInformation(BasicBlock block) {
      Contract.Requires(block != null);

      this.ssaVariableFor.Clear();
      foreach (var instruction in block.Instructions) {
        Contract.Assume(instruction != null);
        var operation = instruction.Operation;
        switch (operation.OperationCode) {
          case OperationCode.Ldarga:
          case OperationCode.Ldarga_S:
          case OperationCode.Ldloca:
          case OperationCode.Ldloca_S:
            var ssaVariable = this.ssaVariableFor[operation.Value??Dummy.ParameterDefinition];
            if (ssaVariable != null)
              //The variable has already been defined in this block, use its new identity.
              instruction.Operation = new SSAOperation(operation, ssaVariable);
            else
              //Create a new identity for this variable.
              this.ReplaceWithNewSSAValue(instruction);
            //Now replace the value one more time because this instruction represents both a read (the above replacement) and a write (the replacement below).
            this.ReplaceWithNewSSAValue(instruction);
            break;

          case OperationCode.Starg:
          case OperationCode.Starg_S:
          case OperationCode.Stloc:
          case OperationCode.Stloc_0:
          case OperationCode.Stloc_1:
          case OperationCode.Stloc_2:
          case OperationCode.Stloc_3:
          case OperationCode.Stloc_S:
            //Assign to new variable
            this.ReplaceWithNewSSAValue(instruction);
            break;

          //case OperationCode.Stind_I:
          //case OperationCode.Stind_I1:
          //case OperationCode.Stind_I2:
          //case OperationCode.Stind_I4:
          //case OperationCode.Stind_I8:
          //case OperationCode.Stind_R4:
          //case OperationCode.Stind_R8:
          //case OperationCode.Stind_Ref:
          //These could potentially write to locals. In that case the SSA constructed by this algorithm is inaccurate.
          //Such coding patterns are very rare and fixing the inaccuracy at this level is very expensive.
          //Consequently we leave it to the client to either fix the inaccuracy itself, or to detect situations where
          //a stind could write to a local and to report them as errors.

        }
      }

      foreach (var successor in this.cfgQueries.DominanceFrontierFor(block)) {
        Contract.Assume(successor != null);
        foreach (var pair in this.ssaVariableFor) {
          bool joinedWriteWithRead = false;
          if (successor.Joins != null) {
            foreach (var join in successor.Joins) {
              if (join.OriginalLocal == pair.key) {
                if (join.Join2 == null) {
                  join.Join2 = (INamedEntity)pair.value;
                  join.Block2 = block;
                } else {
                  var otherJoins = join.OtherJoins;
                  if (otherJoins == null) join.OtherJoins = otherJoins = new List<INamedEntity>();
                  otherJoins.Add((INamedEntity)pair.value);
                  var otherBlocks = join.OtherBlocks;
                  if (otherBlocks == null) join.OtherBlocks = otherBlocks = new List<object>();
                  otherBlocks.Add(block);
                }
                joinedWriteWithRead = true;
                break;
              }
            }
          }
          if (!joinedWriteWithRead) {
            successor.Joins = new Join() {
              OriginalLocal = pair.key, NewLocal = this.GetNewLocal(pair.key),
              Join1 = (INamedEntity)pair.value, Block1 = block, Next = successor.Joins
            };
          }
        }
      }
    }

    /// <summary>
    /// Makes up a new LocalDefinition or ParameterDefinition corresponding to the one in instruction.Operation.Value and 
    /// then updates instruction.Operation.Value with the new definition. The new definition will be an instance of
    /// SSALocalDefinition or SSAParameterDefinition, both of which retain a reference to the original definition
    /// so that the original IL can be recovered from the control flow graph even after it has been put into SSA form.
    /// </summary>
    /// <param name="instruction">The instruction to update.</param>
    private void ReplaceWithNewSSAValue(Instruction instruction) {
      Contract.Requires(instruction != null);

      var operation = instruction.Operation;
      var local = operation.Value as ILocalDefinition;
      if (local != null) {
        var newLocal = this.GetNewLocal(local);
        this.ssaVariableFor[local] = newLocal;
        instruction.Operation = new SSAOperation(operation, newLocal);
      } else {
        var par = operation.Value as IParameterDefinition;
        var newPar = this.GetNewLocal(par);
        if (par == null) par = Dummy.ParameterDefinition;
        this.ssaVariableFor[par] = newPar;
        instruction.Operation = new SSAOperation(operation, newPar);
      }
    }

    /// <summary>
    /// Returns a new SSALocalDefinition if the argument is a local or a new SSAParameterDefinition if the argument is a parameter.
    /// </summary>
    private object GetNewLocal(object localOrParameter) {
      Contract.Ensures(Contract.Result<object>() != null);

      var local = localOrParameter as ILocalDefinition;
      if (local != null) {
        var localName = this.GetLocalName(local);
        IName newName = this.GetNewName(localName);
        return new SSALocalDefinition(local, newName);
      } else {
        var par = (localOrParameter as IParameterDefinition)??Dummy.ParameterDefinition;
        IName newName = this.GetNewName(par.Name.Value);
        return new SSAParameterDefinition(par, newName, this.cdfg.MethodBody.MethodDefinition.ContainingTypeDefinition);
      }
    }

    /// <summary>
    /// Looks up a source provided name for the local using this.sourceLocationProvider, if there is one.
    /// </summary>
    /// <param name="local"></param>
    /// <returns></returns>
    private string GetLocalName(ILocalDefinition local) {
      Contract.Requires(local != null);
      Contract.Ensures(Contract.Result<string>() != null);

      if (this.sourceLocationProvider != null) {
        bool isCompilerGenerated;
        var result = this.sourceLocationProvider.GetSourceNameFor(local, out isCompilerGenerated);
        if (!string.IsNullOrEmpty(result)) return result;
      }
      return local.Name.Value;
    }

    /// <summary>
    /// Makes up a new name that is derived from the given name, but distinct from all other local names in this graph.
    /// </summary>
    /// <remarks>Since all local names are going to get rewritten like this, they should remain unique if they started out that way.</remarks>
    private IName GetNewName(string name) {
      Contract.Requires(name != null);
      Contract.Ensures(Contract.Result<IName>() != null);

      if (name.Length == 0) name = "this";
      return this.nameTable.GetNameFor(name+"_"+this.localCounter++);
    }

  }

  /// <summary>
  /// A basic block in a control flow graph, enhanced with information to help create and represent a Static Single Assignment (SSA) form
  /// of the control flow graph.
  /// </summary>
  /// <typeparam name="Instruction"></typeparam>
  public class SSABasicBlock<Instruction> : EnhancedBasicBlock<Instruction> where Instruction : Microsoft.Cci.Analysis.Instruction {
    /// <summary>
    /// A potentially null (empty) list of locals 
    /// </summary>
    public Join/*?*/ Joins;

  }

  /// <summary>
  /// Records information about a local (or parameter) whose value can come from more than one SSA variable defined in ancestor blocks.
  /// Corresponds to a "Phi node" in SSA literature.
  /// </summary>
  public class Join {
    /// <summary>
    /// The block from which control flowed to create Join1.
    /// </summary>
    public object Block1;
    /// <summary>
    /// The block from which control flowed to create Join2.
    /// </summary>
    public object Block2;
    /// <summary>
    /// A potentially null (empty) list of blocks from which control flowed to create OtherJoins. The order is the same, so OtherBlocks[i] will be the block that provided OtherJoins[i].
    /// </summary>
    public List<object>/*?*/ OtherBlocks;
    /// <summary>
    /// The local (or parameter) that appears in the original IL.
    /// </summary>
    public object OriginalLocal;
    /// <summary>
    /// The "SSA" local (or parameter) that is "written" by this join point (a.k.a. "Phi node")
    /// </summary>
    public object NewLocal;
    /// <summary>
    /// A local written by an ancestor block that flows into this join point without an intervening write to OriginalLocal.
    /// </summary>
    public INamedEntity Join1;
    /// <summary>
    /// A local written by another ancestor block that flows into this join point without an intervening write to OriginalLocal.
    /// </summary>
    public INamedEntity/*?*/ Join2;
    /// <summary>
    /// A potentially null (empty) set of locals written by other ancestor blocks that flows into this join point without an intervening write to OriginalLocal.
    /// </summary>
    public List<INamedEntity>/*?*/ OtherJoins;
    /// <summary>
    /// The type of the local (or parameter).
    /// </summary>
    public ITypeReference Type {
      get {
        Contract.Ensures(Contract.Result<ITypeReference>() != null);
        var local = this.NewLocal as ILocalDefinition;
        if (local != null) return local.Type;
        var par = this.NewLocal as IParameterDefinition;
        Contract.Assume(par != null);
        return par.Type;
      }
    }
    /// <summary>
    /// The next local.
    /// </summary>
    internal Join/*?*/ Next;

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public ReadLocalEnumerator GetEnumerator() {
      return new ReadLocalEnumerator(this);
    }

    /// <summary>
    /// 
    /// </summary>
    public struct ReadLocalEnumerator {

      internal ReadLocalEnumerator(Join head) {
        Contract.Requires(head != null);
        this.current = head;
        this.next = head;
      }

      /// <summary>
      /// 
      /// </summary>
      public Join Current {
        get {
          Contract.Ensures(Contract.Result<Join>() != null);
          Contract.Assume(this.current != null);
          return this.current;
        }
      }
      Join/*?*/ current;

      Join/*?*/ next;

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      public bool MoveNext() {
        if (this.next == null) return false;
        this.current = this.next;
        this.next = this.current.Next;
        return true;
      }

    }
  }

  /// <summary>
  /// A local definition that is just a new name (and object identity) for an existing local. The existing local
  /// can be recovered via the OriginalLocal property.
  /// </summary>
  public class SSALocalDefinition : ILocalDefinition {

    /// <summary>
    /// A local definition that is just a new name (and object identity) for an existing local. The existing local
    /// can be recovered via the OriginalLocal property.
    /// </summary>
    /// <param name="originalLocal">The local for which the new local provides a new name and new object identity.</param>
    /// <param name="name">The name of the new local.</param>
    public SSALocalDefinition(ILocalDefinition originalLocal, IName name) {
      Contract.Requires(originalLocal != null);
      Contract.Requires(name != null);
      this.originalLocal = originalLocal;
      this.name = name;
    }

    /// <summary>
    /// The name of this local.
    /// </summary>
    IName name;
    /// <summary>
    /// The local for which this local provides a new name and new object identity.
    /// </summary>
    ILocalDefinition originalLocal;

    [ContractInvariantMethod]
    private void ObjectInvariant() {
      Contract.Invariant(this.originalLocal != null);
      Contract.Invariant(this.name != null);
    }

    /// <summary>
    /// The local for which this local provides a new name and new object identity. 
    /// </summary>
    public ILocalDefinition OriginalLocal {
      get {
        Contract.Ensures(Contract.Result<ILocalDefinition>() != null);
        return this.originalLocal;
      }
    }

    /// <summary>
    /// Return the name of the local.
    /// </summary>
    public override string ToString() {
      return this.name.Value;
    }

    #region ILocalDefinition Members

    /// <summary>
    /// The compile time value of the definition, if it is a local constant.
    /// </summary>
    public IMetadataConstant CompileTimeValue {
      get {
        Contract.Assume(this.originalLocal.IsConstant);
        return this.originalLocal.CompileTimeValue;
      }
    }

    /// <summary>
    /// Custom modifiers associated with local variable definition.
    /// </summary>
    public IEnumerable<ICustomModifier> CustomModifiers {
      get {
        Contract.Assume(this.originalLocal.IsModified);
        return this.originalLocal.CustomModifiers;
      }
    }

    /// <summary>
    /// True if this local definition is readonly and initialized with a compile time constant value.
    /// </summary>
    public bool IsConstant {
      get { return this.originalLocal.IsConstant; }
    }

    /// <summary>
    /// The local variable has custom modifiers.
    /// </summary>
    public bool IsModified {
      get { return this.originalLocal.IsModified; }
    }

    /// <summary>
    /// True if the value referenced by the local must not be moved by the actions of the garbage collector.
    /// </summary>
    public bool IsPinned {
      get { return this.originalLocal.IsPinned; }
    }

    /// <summary>
    /// True if the local contains a managed pointer (for example a reference to a local variable or a reference to a field of an object).
    /// </summary>
    public bool IsReference {
      get { return this.originalLocal.IsReference; }
    }

    /// <summary>
    /// The definition of the method in which this local is defined.
    /// </summary>
    public IMethodDefinition MethodDefinition {
      get { return this.originalLocal.MethodDefinition; }
    }

    /// <summary>
    /// The type of the local.
    /// </summary>
    public ITypeReference Type {
      get { return this.originalLocal.Type; }
    }

    #endregion

    #region INamedEntity Members

    /// <summary>
    /// The name of the entity.
    /// </summary>
    public IName Name {
      get { return this.name; }
    }

    #endregion

    #region IObjectWithLocations Members

    /// <summary>
    /// A potentially empty collection of locations that correspond to this instance.
    /// </summary>
    public IEnumerable<ILocation> Locations {
      get { return this.originalLocal.Locations; }
    }

    #endregion
  }

  /// <summary>
  /// A parameter definition that is just a new name (and object identity) for an existing parameter. The existing parameter
  /// can be recovered via the OriginalParameter property.
  /// </summary>
  public class SSAParameterDefinition : IParameterDefinition {

    /// <summary>
    /// A parameter definition that is just a new name (and object identity) for an existing parameter. The existing parameter
    /// can be recovered via the OriginalParameter property.
    /// </summary>
    /// <param name="originalParameter">The parameter for which the new parameter provides a new name and new object identity.</param>
    /// <param name="name"></param>
    /// <param name="containingType"></param>
    public SSAParameterDefinition(IParameterDefinition originalParameter, IName name, ITypeDefinition containingType) {
      Contract.Requires(originalParameter != null);
      Contract.Requires(name != null);
      Contract.Requires(containingType != null);

      this.originalParameter = originalParameter;
      this.name = name;
      if (originalParameter.Type is Dummy)
        this.type = containingType; //should only happen if the parameter is the this parameter.
      else
        this.type = originalParameter.Type;
    }

    /// <summary>
    /// The name of this parameter.
    /// </summary>
    IName name;
    /// <summary>
    /// The parameter for which this object provides a new name and new object identity.
    /// </summary>
    IParameterDefinition originalParameter;

    [ContractInvariantMethod]
    private void ObjectInvariant() {
      Contract.Invariant(this.originalParameter != null);
      Contract.Invariant(this.name != null);
      Contract.Invariant(this.type != null);
    }

    /// <summary>
    /// The parameter for which this object provides a new name and new object identity.
    /// </summary>
    public IParameterDefinition OriginalParameter {
      get {
        Contract.Ensures(Contract.Result<IParameterDefinition>() != null);
        return this.originalParameter;
      }
    }

    /// <summary>
    /// Returns the name of the parameter.
    /// </summary>
    public override string ToString() {
      return this.name.Value;
    }

    #region IParameterDefinition Members

    /// <summary>
    /// A compile time constant value that should be supplied as the corresponding argument value by callers that do not explicitly specify an argument value for this parameter.
    /// </summary>
    public IMetadataConstant DefaultValue {
      get { return this.originalParameter.DefaultValue; }
    }

    /// <summary>
    /// True if the parameter has a default value that should be supplied as the argument value by a caller for which the argument value has not been explicitly specified.
    /// </summary>
    public bool HasDefaultValue {
      get { return this.originalParameter.HasDefaultValue; }
    }

    /// <summary>
    /// True if the argument value must be included in the marshalled arguments passed to a remote callee.
    /// </summary>
    public bool IsIn {
      get { return this.originalParameter.IsIn; }
    }

    /// <summary>
    /// This parameter has associated marshalling information.
    /// </summary>
    public bool IsMarshalledExplicitly {
      get { return this.originalParameter.IsMarshalledExplicitly; }
    }

    /// <summary>
    /// True if the argument value must be included in the marshalled arguments passed to a remote callee only if it is different from the default value (if there is one).
    /// </summary>
    public bool IsOptional {
      get { return this.originalParameter.IsOptional; }
    }

    /// <summary>
    /// True if the final value assigned to the parameter will be marshalled with the return values passed back from a remote callee.
    /// </summary>
    public bool IsOut {
      get { return this.originalParameter.IsOut; }
    }

    /// <summary>
    /// True if the parameter has the ParamArrayAttribute custom attribute.
    /// </summary>
    public bool IsParameterArray {
      get { return this.originalParameter.IsParameterArray; }
    }

    /// <summary>
    /// Specifies how this parameter is marshalled when it is accessed from unmanaged code.
    /// </summary>
    public IMarshallingInformation MarshallingInformation {
      get { return this.originalParameter.MarshallingInformation; }
    }

    /// <summary>
    /// The element type of the parameter array.
    /// </summary>
    public ITypeReference ParamArrayElementType {
      get { return this.originalParameter.ParamArrayElementType; }
    }

    #endregion

    #region IReference Members

    /// <summary>
    /// A collection of metadata custom attributes that are associated with this definition.
    /// </summary>
    public IEnumerable<ICustomAttribute> Attributes {
      get { return this.originalParameter.Attributes; }
    }

    /// <summary>
    /// Calls visitor.Visit(IParameterDefinition).
    /// </summary>
    public void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls visitor.VisitReference(IParameterDefinition).
    /// </summary>
    public void DispatchAsReference(IMetadataVisitor visitor) {
      visitor.VisitReference(this);
    }

    #endregion

    #region IObjectWithLocations Members

    /// <summary>
    /// A potentially empty collection of locations that correspond to this instance.
    /// </summary>
    public IEnumerable<ILocation> Locations {
      get { return this.originalParameter.Locations; }
    }

    #endregion

    #region INamedEntity Members

    /// <summary>
    /// The name of the entity.
    /// </summary>
    public IName Name {
      get { return this.name; }
    }

    #endregion

    #region IParameterTypeInformation Members

    /// <summary>
    /// The method or property that defines this parameter.
    /// </summary>
    public ISignature ContainingSignature {
      get { return this.originalParameter.ContainingSignature; }
    }

    /// <summary>
    /// The list of custom modifiers, if any, associated with the parameter. Evaluate this property only if IsModified is true.
    /// </summary>
    public IEnumerable<ICustomModifier> CustomModifiers {
      get {
        Contract.Assume(this.originalParameter.IsModified);
        return this.originalParameter.CustomModifiers;
      }
    }

    /// <summary>
    /// True if the parameter is passed by reference (using a managed pointer).
    /// </summary>
    public bool IsByReference {
      get { return this.originalParameter.IsByReference; }
    }

    /// <summary>
    /// This parameter has one or more custom modifiers associated with it.
    /// </summary>
    public bool IsModified {
      get { return this.originalParameter.IsModified; }
    }

    /// <summary>
    /// The type of argument value that corresponds to this parameter.
    /// </summary>
    public ITypeReference Type {
      get { return this.type; }
    }
    ITypeReference type;

    #endregion

    #region IParameterListEntry Members

    /// <summary>
    /// The position in the parameter list where this instance can be found.
    /// </summary>
    public ushort Index {
      get { return this.originalParameter.Index; }
    }

    #endregion

    #region IMetadataConstantContainer Members

    /// <summary>
    /// The constant value associated with this metadata object. For example, the default value of a parameter.
    /// </summary>
    public IMetadataConstant Constant {
      get { return this.originalParameter.Constant; }
    }

    #endregion
  }

  /// <summary>
  /// A copy of an existing operation, whose Value property has been updated to reference a SSALocalDefinition or SSAParameterDefinition.
  /// The original operation can be recovered via the OriginalOperation property.
  /// </summary>
  internal class SSAOperation : IOperation {

    /// <summary>
    /// A copy of an existing operation, whose Value property has been updated to reference a SSALocalDefinition or SSAParameterDefinition.
    /// The original operation can be recovered via the OriginalOperation property.
    /// </summary>
    /// <param name="originalOperation">The operation to copy into the new operation.</param>
    /// <param name="value">The object to use as the Value property of the new operation.</param>
    internal SSAOperation(IOperation originalOperation, object value) {
      Contract.Requires(originalOperation != null);

      this.originalOperation = originalOperation;
      this.value = value;
    }

    [ContractInvariantMethod]
    private void ObjectInvariant() {
      Contract.Invariant(this.originalOperation != null);
    }

    /// <summary>
    /// The operation that was replaced by this object. It is the same as this object except for this.Value.
    /// </summary>
    IOperation originalOperation;
    /// <summary>
    /// The object to use as the Value property of this object. Usually an SSALocalDefinition or an SSAParameterDefinition
    /// but could be a Dummy.ParameterDefinition of the operation references the this value of an instance method.
    /// </summary>
    object value;

    /// <summary>
    /// The operation that was replaced by this object. It is the same as this object except for this.Value.
    /// </summary>
    public IOperation OriginalOperation {
      get {
        return this.originalOperation;
      }
    }

    #region IOperation Members

    /// <summary>
    /// The actual value of the operation code
    /// </summary>
    public OperationCode OperationCode {
      get { return this.originalOperation.OperationCode; }
    }

    /// <summary>
    /// The offset from the start of the operation stream of a method
    /// </summary>
    public uint Offset {
      get { return this.originalOperation.Offset; }
    }

    /// <summary>
    /// The location that corresponds to this instruction.
    /// </summary>
    public ILocation Location {
      get { return this.originalOperation.Location; }
    }

    /// <summary>
    /// Immediate data such as a string, the address of a branch target, or a metadata reference, such as a Field
    /// </summary>
    public object Value {
      get { return this.value; }
    }

    #endregion
  }
}