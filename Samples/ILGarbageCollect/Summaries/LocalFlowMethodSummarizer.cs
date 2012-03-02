using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Diagnostics.Contracts;

using Microsoft.Cci;
using Microsoft.Cci.Analysis;

using ILGarbageCollect.Mark;

using ILGarbageCollect.LocalFlow;

namespace ILGarbageCollect.Summaries {
  public abstract class LocalFlowMethodSummarizer : IMethodSummarizer {


    public bool CanSummarizeMethod(IMethodDefinition methodDefinition) {
      if (methodDefinition.IsAbstract || methodDefinition.IsExternal) {
        return false;
      }

      // We don't support analyzing a method if it contains ANY exception/finally blocks
      // This is a simplifying assumption -- exceptions can be addressed later.
      if (methodDefinition.Body.OperationExceptionInformation.Count() > 0) {
        return false;
      }

      if (MethodContainsUnhandledInstructions(methodDefinition)) {
        return false;
      }

      /*
      if (!IsLocalFlowSummaryWarranted(methodDefinition)) {
        return false;
      }
       */

      return true;
    }


    private bool IsLocalFlowSummaryWarranted(IMethodDefinition definition) {

      bool containsCallvirt = false;

      foreach (IOperation op in definition.Body.Operations) {
        switch (op.OperationCode) {
          case OperationCode.Callvirt:
            containsCallvirt = true;
            break;
          default:
            break;
        }
      }

      return  containsCallvirt;
    }

    private bool MethodContainsUnhandledInstructions(IMethodDefinition definition) {
      // There is a bug (I think) in the control flow creator for switch statements
      // so we don't allow methods with switch.

      foreach (IOperation op in definition.Body.Operations) {
        switch (op.OperationCode) {
          case OperationCode.Switch:
            return true;
          default:
            break;
        }
      }

      // t-devinc: Check for references, out parameters etc. and punt
      // May want to punt on callvirt to interface, too, we'll see.

      return false;
    }

    abstract public ReachabilitySummary SummarizeMethod(IMethodDefinition methodDefinition, WholeProgram wholeProgram);

  }

  class DebuggableBasicBlock<T> : BasicBlock<T> where T : Microsoft.Cci.Analysis.Instruction {
    public override string ToString() {
      return "Blam!";
    }
  }


  /// <summary>
  /// A really stupid local flow summarizer that uses 1-bit reachability to figure out the reachable basic blocks
  /// and then defers to the SimpleBytecodeMethodSummarizer for each instruction in those reachable blocks.
  /// 
  /// We use this as a sanity check -- in the absence of exception handlers the results from this summary should be the
  /// the same as those from SimpleBytecodeMethodSummarizer.
  /// 
  /// </summary>
  internal class ReachabilityBasedLocalFlowMethodSummarizer : LocalFlowMethodSummarizer {

    public override ReachabilitySummary SummarizeMethod(IMethodDefinition methodDefinition, WholeProgram wholeProgram) {

      // Console.WriteLine("Running ReachabilityBasedLocalFlowMethodSummarizer on {0}", methodDefinition);

      ReachabilityStateAbstraction stateAbstraction = new ReachabilityStateAbstraction();
      ReachabilityStateInterpreter stateInterpreter = new ReachabilityStateInterpreter(stateAbstraction);
     

      var algorithm = new WorkListAlgorithm<ReachabilityStateInterpreter, ReachabilityStateAbstraction, bool>(stateInterpreter);

      algorithm.RunOnMethod(methodDefinition, wholeProgram.Host());

      SimpleBytecodeMethodSummarizer.BytecodeVisitor visitor = new SimpleBytecodeMethodSummarizer.BytecodeVisitor();


      foreach (BasicBlock<Instruction> block in algorithm.ControlFlowGraph.AllBlocks) {
        bool blockIsReachable = algorithm.GetBlockPostState(block);

        if (blockIsReachable) {

          foreach (Instruction instruction in block.Instructions) {
            visitor.Visit(instruction.Operation);
          }

        }
      }
      return visitor.GetSummary();
    }
  }

  /// <summary>
  /// A Types-based local flow summarizer. It tracks the (declared) types of objects as they flow.
  /// 
  /// 
  /// </summary>
  internal class TypesLocalFlowMethodSummarizer : LocalFlowMethodSummarizer {

    public override ReachabilitySummary SummarizeMethod(IMethodDefinition methodDefinition, WholeProgram wholeProgram) {

      //Console.WriteLine("Running AlwaysTopLocalFlowMethodSummarizer on {0}", methodDefinition);



      // hack to analyze on Foo<T> rather than Foo
      // since we need this for data flow

      // Unfortunately, this makes the CFG barf. Need to revisit later.

      /*
      ITypeReference fullyInstantiatedSpecializedTypeReference;

      if (TypeHelper.TryGetFullyInstantiatedSpecializedTypeReference(methodDefinition.ContainingTypeDefinition, out fullyInstantiatedSpecializedTypeReference)) {
        foreach (IMethodDefinition instantiatedMethod in fullyInstantiatedSpecializedTypeReference.ResolvedType.Methods) {
          if (GarbageCollectHelper.UnspecializeAndResolveMethodReference(instantiatedMethod).InternedKey == methodDefinition.InternedKey) {
            methodDefinition = instantiatedMethod;
            break;
          }
        }
      }
      */

      var valueAbstraction = new TypesValueAbstraction(wholeProgram.Host());


      var stateInterpreter = new VirtualCallRecordingStateInterpreter<TypesValueAbstraction, ITypeDefinition>(valueAbstraction);

      

      
     


      

      var algorithm = new WorkListAlgorithm<VirtualCallRecordingStateInterpreter<TypesValueAbstraction, ITypeDefinition>,
                                            LocalsAndOperandsStateAbstraction<TypesValueAbstraction, ITypeDefinition>,
                                            LocalsAndOperands<ITypeDefinition>>(stateInterpreter);

      try {
        algorithm.RunOnMethod(methodDefinition, wholeProgram.Host());
      }
      catch (Exception e) {
        if (e.GetType().FullName.Contains("ContractException")) {
          // Very hokey, but it is what it is.
          // Sometimes we fail because building the CFG fails, and occasionally
          // we fail because of something in the analysis. In either event,
          // we pick ourselves up and fall back on the regular summarization

          // Need to record statistics on how often this happens.

          Console.WriteLine("Got contract exception running local flow for " + methodDefinition);

          return new SimpleBytecodeMethodSummarizer().SummarizeMethod(methodDefinition, wholeProgram);
        }
        else {
          throw;
        }
      }
      


      LocalFlowEnhancedBytecodeVisitor bytecodeVisitor = new LocalFlowEnhancedBytecodeVisitor();

      foreach (BasicBlock<Instruction> block in algorithm.ControlFlowGraph.AllBlocks) {
        foreach (Instruction instruction in block.Instructions) {
          if (instruction.Operation.OperationCode == OperationCode.Callvirt) {
            ITypeDefinition valueAtReceiver = algorithm.StateInterpreter.ReceiverValueForVirtualCallInstruction(instruction.Operation);

            Contract.Assert(valueAtReceiver != null);

            //t-devinc: handle not resolving method gracefully.
            IMethodDefinition compileTimeTargetMethod = (instruction.Operation.Value as IMethodReference).ResolvedMethod;

            // The only way we allow the analysis to return a type that is a supertype of the type being dispatched upon is
            // if we've gone to top (System.Object).
            //
            // This is really just a sanity check.
            ITypeDefinition compileTimeType = compileTimeTargetMethod.ContainingTypeDefinition;

            Contract.Assert(valueAtReceiver == wholeProgram.Host().PlatformType.SystemObject.ResolvedType 
                            || TypeHelper.Type1DerivesFromOrIsTheSameAsType2(valueAtReceiver, compileTimeType)
                            || (compileTimeType.IsInterface && TypeHelper.Type1ImplementsType2(valueAtReceiver, compileTimeType))
                            || (valueAtReceiver.IsInterface && compileTimeType == wholeProgram.Host().PlatformType.SystemObject.ResolvedType)
                            );


            bytecodeVisitor.NoteReceiverTypeForCallVirtual(valueAtReceiver, instruction.Operation);            
          }
        }
      }

      

      foreach (IOperation operation in methodDefinition.Body.Operations) {
        bytecodeVisitor.Visit(operation);
      }

      return bytecodeVisitor.GetSummary();
    }
  }

  internal class LocalFlowEnhancedBytecodeVisitor : SimpleBytecodeMethodSummarizer.BytecodeVisitor {

    IDictionary<IOperation, ITypeDefinition> receiverTypesByCallvirtOperation = new Dictionary<IOperation, ITypeDefinition>();

    public void NoteReceiverTypeForCallVirtual(ITypeDefinition receiverType, IOperation operation) {
      receiverTypesByCallvirtOperation[operation] = receiverType;
    }

    internal override void Visit(IOperation op) {
      if (op.OperationCode == OperationCode.Callvirt) {
        IMethodDefinition compileTimeTargetMethod = ResolveMethodReference(op.Value as IMethodReference);

        if (compileTimeTargetMethod != null && compileTimeTargetMethod.IsVirtual) {
          // we have a callvirt to a virtual method
          // so lets use the local flow information

          //summary.VirtuallyCalledMethods.Add(target);

          ITypeDefinition tightenedReceiverType;

          if (receiverTypesByCallvirtOperation.TryGetValue(op, out tightenedReceiverType)) {

            if (!tightenedReceiverType.IsInterface && TypeHelper.Type1DerivesFromOrIsTheSameAsType2(tightenedReceiverType, compileTimeTargetMethod.ContainingType)) {
              //Console.WriteLine("Value at receiver is {0}", valueAtReceiver);

              //Console.WriteLine("Will look up {0} in {1}", compileTimeTargetMethod, tightenedReceiverType);

              IMethodDefinition tightenedMethod = GarbageCollectHelper.ImplementsInstantiated(tightenedReceiverType, compileTimeTargetMethod);

              

              if (tightenedMethod != null) {

                if (tightenedMethod != compileTimeTargetMethod) {
                  //Console.WriteLine("Tightened method is {0} (from {1})", tightenedMethod, compileTimeTargetMethod);
                }
                

                // Yay, all the flow analysis paid off!
                GetSummary().VirtuallyCalledMethods.Add(tightenedMethod);

                //GetSummary().VirtuallyCalledMethods.Add(compileTimeTargetMethod);
                return;
              }
            }
          }
        }
      }

      // If we've gotten here, we fall back to normal handling
      base.Visit(op);
    }
  }

  


  

  
}
