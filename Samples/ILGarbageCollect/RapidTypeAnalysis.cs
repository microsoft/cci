using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Cci;
using System.IO;
using Microsoft.Cci.MutableCodeModel;
using System.Diagnostics.Contracts;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using ILGarbageCollect.Summaries;
using ILGarbageCollect.Mark;
using ILGarbageCollect.Reasons;


[assembly: InternalsVisibleTo("TestILGarbageCollector")]

namespace ILGarbageCollect {

  public class RapidTypeAnalysis {
    private readonly HashSet<ITypeDefinition> types;
    private readonly HashSet<IMethodDefinition> methods;
    
    private readonly VirtualDispatchDemand virtualCallsInDemand;
    private readonly HashSet<IMethodDefinition> nonvirtualDispatches;

    private readonly HashSet<IFieldDefinition> fields;
    private readonly HashSet<ITypeDefinition> constructed;
    private readonly HashSet<IGenericParameter> constructedGenericParameters;

    private readonly HashSet<IMethodDefinition> worklist;

    private readonly WholeProgram wholeProgram;

    private HashSet<IMethodSummarizer> reflectionSummarizers;

    private readonly SimpleBytecodeMethodSummarizer simpleBytecodeSummarizer;
    private readonly ReachabilityBasedLocalFlowMethodSummarizer reachabilityFlowBytecodeSummarizer;

    //private int countUsedFlowBasedSummarizer;

    private readonly ITypeDefinition systemObjectType;
    private readonly IMethodDefinition systemObjectFinalizeMethod;

    private readonly ISet<IMethodDefinition> methodsRequiringReflectionSummary;

    private readonly ISet<IReference> unresolvedReferences;


    // How to deal with new T() where T is a type variable.
    private readonly TypeVariableCreateInstanceStrategy createInstanceStrategy = TypeVariableCreateInstanceStrategy.ConstructAllConcreteParameters;

    private readonly HashSet<ITypeDefinition> unspecializedTypesPassedAsTypeVariables;

    private readonly AnalysisReasons analysisReasons = new AnalysisReasons();
    private bool finishedAnalysis;

    public RapidTypeAnalysis(WholeProgram wholeProgram, TargetProfile profile) {
      Contract.Ensures(!this.FinishedAnalysis);

      this.types = new HashSet<ITypeDefinition>(new TypeDefinitionEqualityComparer());
      this.methods = new HashSet<IMethodDefinition>(new MethodDefinitionEqualityComparer());
      this.virtualCallsInDemand = new VirtualDispatchDemand();

      this.nonvirtualDispatches = new HashSet<IMethodDefinition>(new MethodDefinitionEqualityComparer());

      this.fields = new HashSet<IFieldDefinition>(new FieldDefinitionEqualityComparer());
      this.constructed = new HashSet<ITypeDefinition>(new TypeDefinitionEqualityComparer());

      this.constructedGenericParameters = new HashSet<IGenericParameter>(new TypeDefinitionEqualityComparer());

      // Note: we use the interned key as the hashcode, so this set should be deterministic
      this.worklist = new HashSet<IMethodDefinition>(new MethodDefinitionEqualityComparer());

      this.wholeProgram = wholeProgram;

      this.reflectionSummarizers = new HashSet<IMethodSummarizer>();
     
      this.simpleBytecodeSummarizer = new SimpleBytecodeMethodSummarizer();
      this.reachabilityFlowBytecodeSummarizer = new ReachabilityBasedLocalFlowMethodSummarizer();

      //systemObjectType = wholeProgram.Host().PlatformType.SystemObject.ResolvedType;

      // Weak heuristic -- should provide custom host?

      IAssembly coreAssembly = wholeProgram.HeuristicFindCoreAssemblyForProfile(profile);
      Contract.Assert(coreAssembly != null);

      systemObjectType = GarbageCollectHelper.CreateTypeReference(wholeProgram.Host(), coreAssembly, "System.Object").ResolvedType;
      Contract.Assert(!(systemObjectType is Dummy));

      systemObjectFinalizeMethod = TypeHelper.GetMethod(systemObjectType, wholeProgram.Host().NameTable.GetNameFor("Finalize"));
      Contract.Assert(!(systemObjectFinalizeMethod is Dummy));

      methodsRequiringReflectionSummary = new HashSet<IMethodDefinition>(new MethodDefinitionEqualityComparer());

      unresolvedReferences = new HashSet<IReference>(new ReferenceEqualityComparer());

      unspecializedTypesPassedAsTypeVariables = new HashSet<ITypeDefinition>(new TypeDefinitionEqualityComparer());

    }


    public IEnumerable<IMethodSummarizer> ReflectionSummarizers {
      get { return this.reflectionSummarizers; }
      set { this.reflectionSummarizers = new HashSet<IMethodSummarizer>(value); }
    }


    [Pure]
    private bool VirtualMethodIsInDemand(IMethodDefinition virtualMethod) {
      Contract.Requires(GarbageCollectHelper.MethodDefinitionIsUnspecialized(virtualMethod));
      Contract.Requires(virtualMethod.IsVirtual);

      return virtualCallsInDemand.VirtualMethodIsInDemand(virtualMethod);
    }

    private void MarkVirtualMethodAsInDemand(IMethodDefinition virtualMethod) {
      Contract.Requires(GarbageCollectHelper.MethodDefinitionIsUnspecialized(virtualMethod));
      Contract.Requires(virtualMethod.IsVirtual);
      virtualCallsInDemand.NoteDispatchIsInDemand(virtualMethod);
    }

    [Pure]
    public bool MethodIsReachable(IMethodDefinition method) {
      Contract.Requires(!method.IsAbstract);
      Contract.Assert(GarbageCollectHelper.MethodDefinitionIsUnspecialized(method));

      return methods.Contains(method);
    }

    private void MarkMethodAsReachable(IMethodDefinition method) {
      Contract.Requires(GarbageCollectHelper.MethodDefinitionIsUnspecialized(method));
      Contract.Requires(!method.IsAbstract);

      Contract.Ensures(methods.Contains(method));

      methods.Add(method);
    }

    private void TypeUseFound(ITypeDefinition t) {
      Contract.Requires(t != null);
      Contract.Requires(!(t is Dummy));

      Contract.Requires(GarbageCollectHelper.UnspecializeAndResolveTypeReference(t) == t);
     Contract.Ensures(this.types.Contains(t));

      if (this.types.Contains(t)) return;

      // add all base classes of this class
      foreach (var baseclass in GarbageCollectHelper.BaseClasses(t)) {
        this.TypeUseFound(GarbageCollectHelper.UnspecializeAndResolveTypeReference(baseclass));
      }

      this.types.Add(t);

      // add static constructor to worklist
      var cctor = GarbageCollectHelper.GetStaticConstructor(this.wholeProgram.Host().NameTable, t);
      if (!(cctor is Dummy)) {
        this.AddToWorklist(GarbageCollectHelper.UnspecializeAndResolveMethodReference(cctor));
      }
    }


    private void ConstructionFoundWithReason(ITypeDefinition t, TypeConstructedReason reason) {
      analysisReasons.NoteTypeConstructedForReason(t, reason);

      ConstructionFound(t);
    }

    private void ConstructionFound(ITypeDefinition t) {
      Contract.Requires(t != null);
      Contract.Requires(!(t is Dummy));
      Contract.Requires(GarbageCollectHelper.TypeDefinitionIsUnspecialized(t));
      Contract.Requires(GarbageCollectHelper.TypeIsConstructable(t));

      Contract.Ensures(this.constructed.Contains(t));

      //Console.WriteLine("Found construction of {0}", t);

      if (this.constructed.Contains(t)) return;
      this.constructed.Add(t);

      // t-devinc: should change AllSuperTypes, etc. to include t
      foreach (var baseclass in new ITypeDefinition[] {t}.Concat(GarbageCollectHelper.AllSuperTypes(t))) {
        ITypeDefinition unspecializedBaseClass = GarbageCollectHelper.UnspecializeAndResolveTypeReference(baseclass);
        foreach (var m in unspecializedBaseClass.Methods) {

          if (m.IsVirtual && VirtualMethodIsInDemand(m)) {
            ICollection<IMethodDefinition> implementationsOfMForT = GarbageCollectHelper.Implements(t, unspecializedBaseClass, m);

            Contract.Assert(implementationsOfMForT.Count() > 0);

            foreach (IMethodDefinition mprime in implementationsOfMForT) {
              NoteDispatch(m, GarbageCollectHelper.UnspecializeAndResolveMethodReference(mprime), t);
            }                     
          }         
        }
      }

      // If a type is constructed then its Finalize method may be called

      ICollection<IMethodDefinition> implementationsOfFinalizeForT = GarbageCollectHelper.Implements(t, systemObjectType, systemObjectFinalizeMethod);
      Contract.Assert(implementationsOfFinalizeForT.Count() == 1);

      // t-devinc: Need to to add reason for this
      this.AddToWorklist(GarbageCollectHelper.UnspecializeAndResolveMethodReference(implementationsOfFinalizeForT.First()));
    }

    private void NoteVirtualDispatch(IMethodDefinition methodDispatchedUpon) {
      Contract.Requires(methodDispatchedUpon != null);
      Contract.Requires(!(methodDispatchedUpon is Dummy));
      Contract.Requires(GarbageCollectHelper.MethodDefinitionIsUnspecialized(methodDispatchedUpon));
      Contract.Requires(methodDispatchedUpon.IsVirtual);

      Contract.Ensures(VirtualMethodIsInDemand(methodDispatchedUpon));

      //Console.WriteLine("VirtCallFound on {0} of type {1}", methodDefinition, methodDefinition.GetType());

      bool virtualMethodIsAlreadyKnown = VirtualMethodIsInDemand(methodDispatchedUpon);

      MarkVirtualMethodAsInDemand(methodDispatchedUpon);

      if (virtualMethodIsAlreadyKnown) return;

      // The code below this point is called only the first time we learn that
      // someone has dispatched against this method.
              
      //Console.WriteLine("{0} has {1} derived methods", methodDefinition, this.callgraph.GetAllDerivedMethods(methodDefinition).ToArray().Length);


      ITypeDefinition typeDefiningM = methodDispatchedUpon.ContainingTypeDefinition;

      this.TypeUseFound(typeDefiningM);
     
      // t-devinc: clean this up
      foreach (ITypeDefinition subType in new ITypeDefinition[] {typeDefiningM}.Concat(wholeProgram.ClassHierarchy().AllSubClassesOfClass(typeDefiningM))) {
        if (GarbageCollectHelper.TypeIsConstructable(subType) && ((subType.IsStruct || constructed.Contains(subType)))) {

          ICollection<IMethodDefinition> implementationsOfMethodDefinitionForSubType = GarbageCollectHelper.Implements(subType, typeDefiningM, methodDispatchedUpon);

          Contract.Assert(implementationsOfMethodDefinitionForSubType.Count() > 0);

          foreach (IMethodDefinition implementationOfM in implementationsOfMethodDefinitionForSubType) {
            NoteDispatch(methodDispatchedUpon, GarbageCollectHelper.UnspecializeAndResolveMethodReference(implementationOfM), subType);                 
          }
        }
      }
    }

    private void NoteDispatch(IMethodDefinition compileTimeMethod, IMethodDefinition runtimeMethod, ITypeDefinition runtimeType) {
      Contract.Requires(GarbageCollectHelper.MethodDefinitionIsUnspecialized(compileTimeMethod));
      Contract.Requires(GarbageCollectHelper.MethodDefinitionIsUnspecialized(runtimeMethod));
      Contract.Requires(GarbageCollectHelper.TypeDefinitionIsUnspecialized(runtimeType));

      // Note: runtimeType may not be type containing runtimeMethod, but it will be a subtype of it.
      // Might want a contract for this.

      if (virtualCallsInDemand.NoteVirtualMethodMayDispatchToMethod(compileTimeMethod, runtimeMethod)) {
        AddToWorklist(runtimeMethod);

        analysisReasons.NoteMethodReachableForReason(runtimeMethod, analysisReasons.MethodReachedByDispatchAgainstVirtualMethodWithTypeConstructed(compileTimeMethod, runtimeType));
      }
    }

    private void NotePotentialNonVirtualMethodReachedForReason(IMethodDefinition targetMethodDefinition, MethodReachedReason reason) {
      Contract.Requires(GarbageCollectHelper.MethodDefinitionIsUnspecialized(targetMethodDefinition));

      this.TypeUseFound(GarbageCollectHelper.UnspecializeAndResolveTypeReference(targetMethodDefinition.ContainingTypeDefinition));

      this.AddToWorklist(GarbageCollectHelper.UnspecializeAndResolveMethodReference(targetMethodDefinition));

      // Really should a precondition requiring reason to not be null,
      // but for now there are some situations where we still don't 
      // create reasons, so they pass null as a cop out.
      if (reason != null) {
        analysisReasons.NoteMethodReachableForReason(targetMethodDefinition, reason);
      }
      
    }

    private void AddToWorklist(IMethodDefinition m) {
      Contract.Requires(m != null);
      Contract.Requires(!(m is Dummy));
      Contract.Requires(!(m is IGenericMethodInstance));
      Contract.Requires(!(m is ISpecializedMethodDefinition));
      Contract.Ensures(this.worklist.Contains(m) || this.methods.Contains(m));

      if (MethodIsReachable(m)) return;

      MarkMethodAsReachable(m);

      if (this.worklist.Contains(m)) return;

      this.worklist.Add(m);
    }

      public void Run(IEnumerable<IMethodReference> roots) {
        Contract.Requires(!this.FinishedAnalysis);
        Contract.Ensures(this.FinishedAnalysis);

      // add the rootset
      foreach (var rootReference in roots) {
        IMethodDefinition rootDefinition = GarbageCollectHelper.UnspecializeAndResolveMethodReference(rootReference);

        NotePotentialNonVirtualMethodReachedForReason(rootDefinition, analysisReasons.MethodReachedBecauseEntryPoint());

        // If a constructor for a type is an entry point, we consider that type to be constructed.
        if (rootDefinition.IsConstructor) {
          ITypeDefinition constructedType = rootDefinition.ContainingTypeDefinition;

          ConstructionFoundWithReason(constructedType, analysisReasons.TypeConstructedBecauseConstructorIsEntryPoint());
        }
      }

      // walk over worklist
      do {
        IMethodDefinition m = this.worklist.First();
        this.worklist.Remove(m);
        //Console.WriteLine("Pulled method {0} off of worklist", m);

        

        if (!m.IsExternal) {

          

          IMethodSummarizer bestSummarizer = GetBytecodeSummarizerForMethod(m);

          ReachabilitySummary bytecodeSummary = bestSummarizer.SummarizeMethod(m, wholeProgram);

          if (bytecodeSummary != null) {
            ProcessSummary(bytecodeSummary, m);

            ISet<ReachabilitySummary> reflectionSummaries = RunReflectionSummarizers(m, wholeProgram);

            foreach (ReachabilitySummary summary in reflectionSummaries) {
              ProcessSummary(summary, m);
            }

            bool gotReflectionSummary = reflectionSummaries.Count() > 0;

            if (!gotReflectionSummary && IsReflectionSummaryProbablyNeeded(bytecodeSummary, m)) {
              //Console.WriteLine("{0} calls reflection but doesn't have a summary. (UNSOUND?).", m);

              methodsRequiringReflectionSummary.Add(m);
            }
          }
        }
        else {
          if (!m.IsAbstract) {
            // Boy, there are a lot of these
            // Console.WriteLine("Note: cannot process external method {0}. (UNSOUND)", m);
          }
        }       

      } while (this.worklist.Count > 0);


      //Console.WriteLine("Used flow-based summarizer for {0}/{1} methods", countUsedFlowBasedSummarizer, ReachableMethods().Count());
      this.finishedAnalysis = true;

    }

    public void ProcessSummary(ReachabilitySummary summary, IMethodDefinition summarizedMethod) {
      Contract.Requires(GarbageCollectHelper.MethodDefinitionIsUnspecialized(summarizedMethod));

      foreach (IMethodDefinition nonvirtuallyCalledMethod in summary.NonvirtuallyCalledMethods) {
        NoteGenericParameterFlowForMethod(nonvirtuallyCalledMethod);

        IMethodDefinition unspecializedMethod = GarbageCollectHelper.UnspecializeAndResolveMethodReference(nonvirtuallyCalledMethod);

        analysisReasons.NoteNonVirtualDispatchReachableForReason(nonvirtuallyCalledMethod, analysisReasons.DispatchReachedBecauseContainingMethodWasReached(summarizedMethod));

        if (nonvirtualDispatches.Add(unspecializedMethod)) {
          MethodReachedReason reason = analysisReasons.MethodReachedByDispatchAgainstNonVirtualMethod(unspecializedMethod);

          NotePotentialNonVirtualMethodReachedForReason(unspecializedMethod, reason);
        }     
      }

      foreach (IMethodDefinition virtuallyCalledMethod in summary.VirtuallyCalledMethods) {
        NoteGenericParameterFlowForMethod(virtuallyCalledMethod);

        IMethodDefinition unspecializedMethod = GarbageCollectHelper.UnspecializeAndResolveMethodReference(virtuallyCalledMethod);

        analysisReasons.NoteVirtualDispatchReachableForReason(unspecializedMethod, analysisReasons.DispatchReachedBecauseContainingMethodWasReached(summarizedMethod));

        NoteVirtualDispatch(unspecializedMethod);
      }

      foreach (ITypeDefinition reachableType in summary.ReachableTypes) {
        TypeUseFound(GarbageCollectHelper.UnspecializeAndResolveTypeReference(reachableType));
      }

      foreach (ITypeDefinition constructedType in summary.ConstructedTypes) {
        ITypeDefinition unspecializedConstructedType = GarbageCollectHelper.UnspecializeAndResolveTypeReference(constructedType);

        ConstructionFoundWithReason(unspecializedConstructedType, analysisReasons.TypeConstructedBecauseAllocatingMethodReached(summarizedMethod));
      }

      foreach (IFieldDefinition reachableField in summary.ReachableFields) {
        fields.Add(GarbageCollectHelper.UnspecializeAndResolveFieldReference(reachableField));
      }

      foreach (IGenericParameter genericParameter in summary.ConstructedTypeParameters) {
        NoteTypeVariableConstructed(genericParameter);
      }

      unresolvedReferences.UnionWith(summary.UnresolvedReferences);
    }

   

    private void NoteTypeVariableConstructed(IGenericParameter typeVariable) {

      if (constructedGenericParameters.Count() == 0) {
        NoteFirstTypeVariableConstructed();
      }
      
      constructedGenericParameters.Add(typeVariable);
    }

    // Called when the first new T() is detected.
    // This method is where we add worst-case-scenario
    // constructed types for those stategies.

    // t-devinc: Now that we don't do construct reachable types
    // this maybe doesn't make sense.
    private void NoteFirstTypeVariableConstructed() {
      IEnumerable<ITypeDefinition> potentialUniverse;

      switch (createInstanceStrategy) {
        case TypeVariableCreateInstanceStrategy.ConstructAll:
          potentialUniverse = wholeProgram.AllDefinedTypes();
          break;

        default:
          // do nothing
          potentialUniverse = new HashSet<ITypeDefinition>();
          break;
      }

      foreach (ITypeDefinition t in potentialUniverse) {
        if (GarbageCollectHelper.TypeIsConstructable(t)) {
          ConstructionFound(t);
          // t-devinc: We should associate a reason with this!
        }
      }
    }


    private void NoteGenericParameterFlowForMethod(IMethodDefinition calledMethod) {
      // There are three ways generic parameters can flow
      //
      // 1) generic type parameters via a newobject constructor
      // 2) generic type parameters via a static call
      // 3) generic method parameters via a (non-constructor)method call

      // t-devinc: 
      // I don't think this handles specialized types properly (i.e. a type some of whose parameters are instantiated
      // and some of whom are generic). This can happen with inner classes.
     

      // handle Foo<T>.DoSomething(); or new Foo<T>()
      if (calledMethod.ContainingTypeDefinition is IGenericTypeInstance && (calledMethod.IsStatic || calledMethod.IsConstructor)) {
        IGenericTypeInstance instantiatedType = calledMethod.ContainingTypeDefinition as IGenericTypeInstance;
       
        ITypeDefinition genericType = instantiatedType.GenericType.ResolvedType;

        IEnumerable<ITypeReference> actuals = instantiatedType.GenericArguments;

        IEnumerable<IGenericParameter> formals = genericType.GenericParameters;

        NoteGenericParametersFlow(actuals, formals);        
      }

      // handle Bar.DoSomething<T>();
      if (calledMethod is IGenericMethodInstance) {
        IGenericMethodInstance instantiatedMethod = calledMethod as IGenericMethodInstance;

        IMethodDefinition genericMethod = instantiatedMethod.GenericMethod.ResolvedMethod;

        IEnumerable<ITypeReference> actuals = instantiatedMethod.GenericArguments;

        IEnumerable<IGenericParameter> formals = genericMethod.GenericParameters;

        NoteGenericParametersFlow(actuals, formals);
      }    
    }

    private void NoteGenericParametersFlow(IEnumerable<ITypeReference> actualsEnumerable, IEnumerable<IGenericParameter> formalsEnumerable) {
      ITypeDefinition[] actuals = (from a in actualsEnumerable select a.ResolvedType).ToArray();

      IGenericParameter[] formals = formalsEnumerable.ToArray();

      if (actuals.Count() == formals.Count()) {
        for (int i = 0; i < actuals.Count(); i++) {
          ITypeDefinition actual = actuals[i];

          IGenericParameter formal = formals[i];

          NoteGenericParameterFlow(actual, formal);
        }
      }
      else {
        throw new Exception("Oh-Uh: |actuals| != |formals|");
      }
    }

    private void NoteGenericParameterFlow(ITypeDefinition actual, IGenericParameter formal) {

      if (createInstanceStrategy == TypeVariableCreateInstanceStrategy.ConstructAllConcreteParameters) {
        if (!(actual is IGenericParameter)) {
          // actual is concrete

          ITypeDefinition unspecializedConcreteType = GarbageCollectHelper.UnspecializeAndResolveTypeReference(actual);

          unspecializedTypesPassedAsTypeVariables.Add(unspecializedConcreteType);

          if (GarbageCollectHelper.TypeIsConstructable(unspecializedConcreteType)) {
            // t-devinc: We should associate a reason with this construction found
            ConstructionFound(unspecializedConcreteType);
            
            IMethodDefinition defaultConstructor = TypeHelper.GetMethod(unspecializedConcreteType, wholeProgram.Host().NameTable.GetNameFor(".ctor"));

            if (!(defaultConstructor is Dummy)) {
              // t-devinc: Add reason for this
              NotePotentialNonVirtualMethodReachedForReason(defaultConstructor, null);
            }
          }      
        }
      }      
    }

    
    private ISet<ReachabilitySummary> RunReflectionSummarizers(IMethodDefinition method, WholeProgram wholeProgram) {
      ISet<ReachabilitySummary> summaries = new HashSet<ReachabilitySummary>();
     

      foreach (IMethodSummarizer reflectionSummarizer in reflectionSummarizers) {
        ReachabilitySummary summary = reflectionSummarizer.SummarizeMethod(method, wholeProgram);

        if (summary != null) {
          summaries.Add(summary);
        }
      }

      return summaries;
    }
      

    private bool IsReflectionSummaryProbablyNeeded(ReachabilitySummary bytecodeSummary, IMethodDefinition containingMethod) {
      // Note we only get the compile-time targets for virtually called methods here.
      // This is not right; but we're really just using it as a heuristic to help debugging.

      // If a non-system class calls a reflection class, heuristically we probably need a summary
      // t-devinc: ToString(): gross
      if (!containingMethod.ContainingType.ToString().Contains("System")) {
        foreach (IMethodDefinition calledMethod in bytecodeSummary.NonvirtuallyCalledMethods.Concat(bytecodeSummary.VirtuallyCalledMethods)) {
          if (   calledMethod.ContainingType.ToString().Contains("System.Reflection") 
              || calledMethod.ContainingType.ToString().Contains("System.Activator")
              || calledMethod.ContainingType.ToString().Contains("System.Xml.Serialization.XmlSerializer")) {
            //Console.WriteLine("{0} calls reflection method {1}", containingMethod, calledMethod);
            return true;
          }
        }
      }

      return false;
    }


    private IMethodSummarizer GetBytecodeSummarizerForMethod(IMethodDefinition methodDefinition) {
      return simpleBytecodeSummarizer;

      /*
      TypesLocalFlowMethodSummarizer localFlowSummarizer = new TypesLocalFlowMethodSummarizer();

      if (localFlowSummarizer.CanSummarizeMethod(methodDefinition)) {
        countUsedFlowBasedSummarizer++;
        return localFlowSummarizer;
      }
      else {
        return simpleBytecodeSummarizer;
      }
       * */
    }

    public override string ToString() {
      var s = "";
      foreach (var m in this.methods) {
        s += " " + m;
      }
      return s;
    }

    public ICollection<IMethodDefinition> ReachableMethods() {
      return this.methods;
    }

    public ICollection<IFieldDefinition> ReachableFields() {
      return this.fields;
    }

    public ICollection<ITypeDefinition> ReachableTypes() {
      return this.types;
    }

    public ICollection<ITypeDefinition> ConstructedTypes() {
      return this.constructed;
    }

    public ICollection<IGenericParameter> ConstructedGenericParameters() {
      return this.constructedGenericParameters;
    }

    public AnalysisReasons GetAnalysisReasons() {
      return this.analysisReasons;
    }

    public WholeProgram WholeProgram() {
      return wholeProgram;
    }

    public ISet<IMethodDefinition> MethodsRequiringReflectionSummary() {
      return methodsRequiringReflectionSummary;
    }

    public ISet<IReference> UnresolvedReferences() {
      return unresolvedReferences;
    }

    // Filter all methods in methodref by those that ILGC says are reachable.
    public IEnumerable<IMethodReference> GetMethodCallees(IMethodReference methodref) {
        Contract.Requires(methodref != null);
        Contract.Requires(!(methodref is Dummy));
        Contract.Requires(this.FinishedAnalysis);

        // use a set to remove potential duplicates.
        ISet<IMethodDefinition> calls = new HashSet<IMethodDefinition>(new MethodDefinitionEqualityComparer());

        // get unspecialized definition of this method reference
        IMethodDefinition methoddef = GarbageCollectHelper.UnspecializeAndResolveMethodReference(methodref);        

        IMethodSummarizer bestSummarizer = new CompleteBytecodeMethodSummarizer();
        ReachabilitySummary summary = bestSummarizer.SummarizeMethod(methoddef, wholeProgram);


        if (summary != null) {
            foreach (var method in summary.NonvirtuallyCalledMethods) {
                if (this.ReachableMethods().Contains(GarbageCollectHelper.UnspecializeAndResolveMethodReference(method as IMethodReference))) // && this.ReachableTypes().Contains(method.ContainingTypeDefinition))
                    calls.Add(method);
            }

            foreach (var method in summary.VirtuallyCalledMethods)
                if (this.ReachableMethods().Contains(GarbageCollectHelper.UnspecializeAndResolveMethodReference(method as IMethodReference)))// && this.ReachableTypes().Contains(method.ContainingTypeDefinition))
                    calls.Add(method);
        }

        return calls;
    }

    public bool FinishedAnalysis { get { return this.finishedAnalysis; } }
  }
  

  internal class VirtualDispatchDemand {
    private readonly IDictionary<IMethodDefinition, HashSet<IMethodDefinition>> runtimeTargetsByDispatch = new Dictionary<IMethodDefinition, HashSet<IMethodDefinition>>(new MethodDefinitionEqualityComparer());

    [Pure]
    public bool VirtualMethodIsInDemand(IMethodDefinition methodDefinition) {
      Contract.Requires(GarbageCollectHelper.MethodDefinitionIsUnspecialized(methodDefinition));
      Contract.Requires(methodDefinition.IsVirtual);

      return runtimeTargetsByDispatch.ContainsKey(methodDefinition);
    }

    public void NoteDispatchIsInDemand(IMethodDefinition compileTimeMethod) {
      Contract.Requires(GarbageCollectHelper.MethodDefinitionIsUnspecialized(compileTimeMethod));
      Contract.Requires(compileTimeMethod.IsVirtual);
      Contract.Ensures(VirtualMethodIsInDemand(compileTimeMethod));
      Contract.Ensures(runtimeTargetsByDispatch[compileTimeMethod] != null);

      HashSet<IMethodDefinition> calls = null;
      if (!runtimeTargetsByDispatch.TryGetValue(compileTimeMethod, out calls)) {
        calls = new HashSet<IMethodDefinition>(new MethodDefinitionEqualityComparer());
        runtimeTargetsByDispatch[compileTimeMethod] = calls;
      }
    }

    public bool NoteVirtualMethodMayDispatchToMethod(IMethodDefinition compileTimeMethod, IMethodDefinition runtimeTarget) {
      Contract.Requires(GarbageCollectHelper.MethodDefinitionIsUnspecialized(compileTimeMethod));
      Contract.Requires(compileTimeMethod.IsVirtual);

      NoteDispatchIsInDemand(compileTimeMethod);

      return runtimeTargetsByDispatch[compileTimeMethod].Add(runtimeTarget);
    }
  }


  namespace Reasons {

  }
}


