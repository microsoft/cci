using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Diagnostics;
using System.Diagnostics.Contracts;

//using QuickGraph;
//using QuickGraph.Algorithms;
//using QuickGraph.Algorithms.ShortestPath;
//using QuickGraph.Algorithms.Search;

using Microsoft.Cci;

using ILGarbageCollect;


namespace ILGarbageCollect.Reasons {


  public abstract class AnalysisReason /*:  IEdge<AnalysisFact>, IEquatable<AnalysisReason> */ {

    protected readonly AnalysisReasons analysisReasons;

    internal AnalysisReason(AnalysisReasons analysisReasons) {
      this.analysisReasons = analysisReasons;

      analysisReasons.NoteAnalysisReasonCreated(this); // escaping this in a non-sealed class. Bad bad bad.
    }

    internal AnalysisFact Result { get; set; }

    internal abstract AnalysisFact MainCause { get; }

    internal virtual IEnumerable<AnalysisFact> OtherCauses { get { yield break; } }

#if QUICKGRAPH
    bool IEquatable<AnalysisReason>.Equals(AnalysisReason other) {
      return this == other;
    }

    AnalysisFact IEdge<AnalysisFact>.Source { get { return Result; } }

    AnalysisFact IEdge<AnalysisFact>.Target { get { return MainCause; } }

#endif
  }


  // This is a useless abstract class -- should get rid of it.
  public abstract class MethodReachedReason : AnalysisReason {

    internal MethodReachedReason(AnalysisReasons analysisReasons)
      : base(analysisReasons) {

    }

  }

  /* Under our system there are two reasons a method M' can be reachable:
      1) A reachable method P calls M' non-virtually.
      2) A reachable method P performs a dynamic dispatch on some method which M' overrides and a type T which defines or inherits M' has been constructed.
      3) It is a designated entry point.
      . . .
   *  To Add: finalizers upon type construction found, class constructors upon type use found
   * 
   */

  public class MethodReachedBecauseEntryPointReason : MethodReachedReason {
    public MethodReachedBecauseEntryPointReason(AnalysisReasons analysisReasons)
      : base(analysisReasons) {
    }

    internal override AnalysisFact MainCause {
      get {
        return analysisReasons.GetEntryPointReachedFact();
      }
    }

    public override string ToString() {
      return "Because program was launched.";
    }
  }

  public class MethodReachedBecauseDispatchedVirtuallyReason : MethodReachedReason {

    private VirtualDispatchFact virtualDispatchFact;

    private TypeConstructedFact typeConstructedFact;

    public MethodReachedBecauseDispatchedVirtuallyReason(AnalysisReasons analysisReasons, IMethodDefinition methodDispatchedUpon, ITypeDefinition typeConstructed)
      : base(analysisReasons) {

      virtualDispatchFact = analysisReasons.GetVirtualDispatchFact(methodDispatchedUpon);
      typeConstructedFact = analysisReasons.GetTypeConstructedFact(typeConstructed);
    }

    public IMethodDefinition DispatchAgainstMethod { get { return virtualDispatchFact.DispatchMethod; } }

    public ITypeDefinition AndTypeWasConstructed { get { return typeConstructedFact.TypeConstructed; } }

    public override string ToString() {
      return "Because of virtual dispatch against " + DispatchAgainstMethod + " with " + AndTypeWasConstructed + " constructed";
    }


    internal override AnalysisFact MainCause { get { return virtualDispatchFact; } }
    internal override IEnumerable<AnalysisFact> OtherCauses { get { yield return typeConstructedFact; } } // Implement Me!

  }

  public class MethodReachedBecauseDispatchedNonVirtuallyReason : MethodReachedReason {

    private NonVirtualDispatchFact nonVirtualDispatchFact;

    public MethodReachedBecauseDispatchedNonVirtuallyReason(AnalysisReasons analysisReasons, IMethodDefinition methodDispatchedUpon)
      : base(analysisReasons) {

      nonVirtualDispatchFact = analysisReasons.GetNonVirtualDispatchFact(methodDispatchedUpon);
    }

    public IMethodDefinition DispatchAgainstMethod { get { return nonVirtualDispatchFact.DispatchMethod; } }

    internal override AnalysisFact MainCause { get { return nonVirtualDispatchFact; } }

    public override string ToString() {
      return "Because of nonvirtual dispatch against " + DispatchAgainstMethod;
    }

  }

  public class DispatchReachedReason : AnalysisReason {

    private readonly MethodReachedFact methodReachedFact;

    internal DispatchReachedReason(AnalysisReasons analysisReasons, IMethodDefinition reachedMethod)
      : base(analysisReasons) {

      methodReachedFact = analysisReasons.GetMethodReachedFact(reachedMethod);
    }

    public IMethodDefinition MethodWasReached { get { return methodReachedFact.ReachedMethod; } }

    internal override AnalysisFact MainCause {
      get {
        return methodReachedFact;
      }
    }

    public override string ToString() {
      return "Because " + MethodWasReached + " was called.";
    }
  }

  /* There are two ways a type can be constructed:
    * 1) It is a struct, and thus considered automatically construcuted
    * 2) A method M' calls newobj to allocate it
    * 3) One of its constructors is a designated entry point.
    * 
    * We only record 2) and 3) since 1) can be extracted directly from the type
    * 
    * (What about dealing with new T() ?)
    */


  // Consider getting read of this useless class.
  public abstract class TypeConstructedReason : AnalysisReason {
    internal TypeConstructedReason(AnalysisReasons analysisReasons)
      : base(analysisReasons) {

    }
  }

  internal class TypeConstructedBecauseEntryPointReason : TypeConstructedReason {

    internal TypeConstructedBecauseEntryPointReason(AnalysisReasons analysisReasons)
      : base(analysisReasons) {
    }

    internal override AnalysisFact MainCause {
      get {
        return analysisReasons.GetEntryPointReachedFact();
      }
    }

    public override string ToString() {
      return "Constructor for type marked as marked as entry";
    }
  }

  internal class TypeConstructedBecauseAllocatingMethodReachedReason : TypeConstructedReason {

    private readonly MethodReachedFact allocatorReachedFact;

    internal TypeConstructedBecauseAllocatingMethodReachedReason(AnalysisReasons analysisReasons, IMethodDefinition allocatingMethod)
      : base(analysisReasons) {

      allocatorReachedFact = analysisReasons.GetMethodReachedFact(allocatingMethod);
    }

    public IMethodDefinition AllocatorWasReached { get { return allocatorReachedFact.ReachedMethod; } }

    internal override AnalysisFact MainCause {
      get {
        return allocatorReachedFact;
      }
    }

    public override string ToString() {
      return "Because allocated in reached method " + AllocatorWasReached;
    }
  }

  public abstract class AnalysisFact {
    protected readonly AnalysisReasons analysisReasons;

    internal AnalysisFact(AnalysisReasons analysisReasons) {
      this.analysisReasons = analysisReasons;

      analysisReasons.NoteAnalysisFactCreated(this); // escaping this. Bad bad bad.
    }

    public abstract IEnumerable<AnalysisReason> GetReasons();
  }

  internal class TypeConstructedFact : AnalysisFact {

    internal TypeConstructedFact(AnalysisReasons analysisReasons)
      : base(analysisReasons) {
    }

    internal ITypeDefinition TypeConstructed { get; set; }

    public override IEnumerable<AnalysisReason> GetReasons() {
      return analysisReasons.GetReasonsTypeWasConstructed(TypeConstructed);
    }
  }

  internal class NonVirtualDispatchFact : AnalysisFact {

    internal NonVirtualDispatchFact(AnalysisReasons analysisReasons)
      : base(analysisReasons) {
    }

    internal IMethodDefinition DispatchMethod { get; set; }

    public override IEnumerable<AnalysisReason> GetReasons() {
      return analysisReasons.GetReasonsNonVirtualDispatchWasReached(DispatchMethod);
    }

    public override string ToString() {
      return "Non-virtual dispatch on " + DispatchMethod + " was reached.";
    }
  }

  internal class VirtualDispatchFact : AnalysisFact {

    internal VirtualDispatchFact(AnalysisReasons analysisReasons)
      : base(analysisReasons) {
    }

    internal IMethodDefinition DispatchMethod { get; set; }

    public override IEnumerable<AnalysisReason> GetReasons() {
      return analysisReasons.GetReasonsVirtualDispatchWasReached(DispatchMethod);
    }

    public override string ToString() {
      return "Dynamic dispatch on " + DispatchMethod + " was reached.";
    }
  }

  internal class MethodReachedFact : AnalysisFact {

    internal MethodReachedFact(AnalysisReasons analysisReasons)
      : base(analysisReasons) {
    }

    internal IMethodDefinition ReachedMethod { get; set; }

    public override IEnumerable<AnalysisReason> GetReasons() {
      return analysisReasons.GetReasonsMethodWasReached(ReachedMethod);
    }

    public override string ToString() {
      return "Method " + ReachedMethod + " was called.";
    }
  }


  internal class EntryPointReachedFact : AnalysisFact {

    internal EntryPointReachedFact(AnalysisReasons analysisReasons)
      : base(analysisReasons) {
    }

    public override IEnumerable<AnalysisReason> GetReasons() {
      yield break;
    }

    public override string ToString() {
      return "Program was started.";
    }
  }



  public sealed class AnalysisReasons /* : DelegateIncidenceGraph<AnalysisFact, AnalysisReason>,
                                    IVertexAndEdgeListGraph<AnalysisFact, AnalysisReason> */{

    // Map from IDefinition-land to fact-land
    // We can't use IDefinitions as facts directly because some definitions
    // are used in multiple ways: e.g., an IMethodDefinition can be both a dispatch fact
    // (i.e. we had a virtual call to that compile-time method) and a reachable fact
    // (i.e. some dispatch resolved to that method at run-time, so it was reachable).
    //
    // We have a similar situation for types (type constructed vs. type use found).

    private readonly IDictionary<ITypeDefinition, TypeConstructedFact> typeConstructionFactsByType
        = new Dictionary<ITypeDefinition, TypeConstructedFact>(new TypeDefinitionEqualityComparer());

    private readonly IDictionary<IMethodDefinition, NonVirtualDispatchFact> nonVirtualDispatchFactsByMethod
        = new Dictionary<IMethodDefinition, NonVirtualDispatchFact>(new MethodDefinitionEqualityComparer());

    private readonly IDictionary<IMethodDefinition, VirtualDispatchFact> virtualDispatchFactsByMethod
        = new Dictionary<IMethodDefinition, VirtualDispatchFact>(new MethodDefinitionEqualityComparer());

    private readonly IDictionary<IMethodDefinition, MethodReachedFact> methodReachedFactsByMethod
        = new Dictionary<IMethodDefinition, MethodReachedFact>(new MethodDefinitionEqualityComparer());


    private readonly AnalysisFact entryPointReachedFact;

    // Map from IDefinition land to reason-land

    private readonly IDictionary<IMethodDefinition, HashSet<MethodReachedReason>> reasonSetsByMethodReached
        = new Dictionary<IMethodDefinition, HashSet<MethodReachedReason>>(new MethodDefinitionEqualityComparer());

    private readonly IDictionary<ITypeDefinition, HashSet<TypeConstructedReason>> reasonSetsByTypeConstructed
        = new Dictionary<ITypeDefinition, HashSet<TypeConstructedReason>>(new TypeDefinitionEqualityComparer());

    private readonly IDictionary<IMethodDefinition, HashSet<DispatchReachedReason>> reasonSetsByNonVirtualDispatchReached
        = new Dictionary<IMethodDefinition, HashSet<DispatchReachedReason>>(new MethodDefinitionEqualityComparer());

    private readonly IDictionary<IMethodDefinition, HashSet<DispatchReachedReason>> reasonSetsByVirtualDispatchReached
        = new Dictionary<IMethodDefinition, HashSet<DispatchReachedReason>>(new MethodDefinitionEqualityComparer());


    // Keep track of all facts
    // This is wasteful of memory, but simplifies implementing IVertexSet and IEdgeSet

    private readonly HashSet<AnalysisFact> allFacts = new HashSet<AnalysisFact>();
    private readonly HashSet<AnalysisReason> allReasons = new HashSet<AnalysisReason>();


    public AnalysisReasons()
#if QUICKGRAPH
      : base(AnalysisReasons.TryReasonsForFact) 
#endif
    {

      entryPointReachedFact = new EntryPointReachedFact(this); // escaping this; have to be careful if ever subclass
      allFacts.Add(entryPointReachedFact);
    }

    public void NoteMethodReachableForReason(IMethodDefinition methodReached, MethodReachedReason reason) {
      HashSet<MethodReachedReason> reasons;

      if (!reasonSetsByMethodReached.TryGetValue(methodReached, out reasons)) {
        reasons = new HashSet<MethodReachedReason>();

        reasonSetsByMethodReached[methodReached] = reasons;
      }

      reasons.Add(reason);
      reason.Result = GetMethodReachedFact(methodReached);
    }

    public void NoteNonVirtualDispatchReachableForReason(IMethodDefinition methodDispatchedAgainst, DispatchReachedReason reason) {
      HashSet<DispatchReachedReason> reasons;

      if (!reasonSetsByNonVirtualDispatchReached.TryGetValue(methodDispatchedAgainst, out reasons)) {
        reasons = new HashSet<DispatchReachedReason>();

        reasonSetsByNonVirtualDispatchReached[methodDispatchedAgainst] = reasons;
      }

      reasons.Add(reason);
      reason.Result = GetNonVirtualDispatchFact(methodDispatchedAgainst);
    }

    public void NoteVirtualDispatchReachableForReason(IMethodDefinition methodDispatchedAgainst, DispatchReachedReason reason) {
      HashSet<DispatchReachedReason> reasons;

      if (!reasonSetsByVirtualDispatchReached.TryGetValue(methodDispatchedAgainst, out reasons)) {
        reasons = new HashSet<DispatchReachedReason>();

        reasonSetsByVirtualDispatchReached[methodDispatchedAgainst] = reasons;
      }

      reasons.Add(reason);
      reason.Result = GetVirtualDispatchFact(methodDispatchedAgainst);
    }

    public void NoteTypeConstructedForReason(ITypeDefinition constructedType, TypeConstructedReason reason) {
      HashSet<TypeConstructedReason> reasons;

      if (!reasonSetsByTypeConstructed.TryGetValue(constructedType, out reasons)) {
        reasons = new HashSet<TypeConstructedReason>();

        reasonSetsByTypeConstructed[constructedType] = reasons;
      }

      reasons.Add(reason);
      reason.Result = GetTypeConstructedFact(constructedType);
    }

    public HashSet<TypeConstructedReason> GetReasonsTypeWasConstructed(ITypeDefinition typeDefinition) {
      HashSet<TypeConstructedReason> reasons = null;

      if (reasonSetsByTypeConstructed.TryGetValue(typeDefinition, out reasons)) {
        return reasons;
      }

      return new HashSet<TypeConstructedReason>();
    }

    public HashSet<MethodReachedReason> GetReasonsMethodWasReached(IMethodDefinition methodDefinition) {
      HashSet<MethodReachedReason> reasons = null;

      if (reasonSetsByMethodReached.TryGetValue(methodDefinition, out reasons)) {
        return reasons;
      }

      return new HashSet<MethodReachedReason>();
    }

    public HashSet<DispatchReachedReason> GetReasonsNonVirtualDispatchWasReached(IMethodDefinition methodDispatchedAgainst) {
      HashSet<DispatchReachedReason> reasons = null;

      if (reasonSetsByNonVirtualDispatchReached.TryGetValue(methodDispatchedAgainst, out reasons)) {
        return reasons;
      }

      return new HashSet<DispatchReachedReason>();
    }

    public HashSet<DispatchReachedReason> GetReasonsVirtualDispatchWasReached(IMethodDefinition methodDispatchedAgainst) {
      HashSet<DispatchReachedReason> reasons = null;

      if (reasonSetsByVirtualDispatchReached.TryGetValue(methodDispatchedAgainst, out reasons)) {
        return reasons;
      }

      return new HashSet<DispatchReachedReason>();
    }

    public HashSet<IMethodDefinition> AllMethodsVirtuallyDispatchedAgainst() {
      return new HashSet<IMethodDefinition>(reasonSetsByVirtualDispatchReached.Keys, new MethodDefinitionEqualityComparer());
    }

    public HashSet<IMethodDefinition> AllMethodsNonVirtuallyDispatchedAgainst() {
      return new HashSet<IMethodDefinition>(reasonSetsByNonVirtualDispatchReached.Keys, new MethodDefinitionEqualityComparer());
    }

#if QUICKGRAPH
    public void CalculateBestReasons() {
      Contract.Assert(Contract.ForAll<AnalysisReason>(allReasons, (reason) => reason.Result != null));
      Contract.Assert(Contract.ForAll<AnalysisReason>(allReasons, (reason) => reason.MainCause != null));

      Console.WriteLine("There are {0} facts and {1} reasons.", allFacts.Count(), allReasons.Count());




      // Choose an arbitrary fact to print a reason for.

      AnalysisFact sourceFact = methodReachedFactsByMethod.Values.ElementAt(100);

      AnalysisFact targetFact = entryPointReachedFact;

      Console.WriteLine("SourceFact is {0} TargetFact is {1}", sourceFact, targetFact);

      Console.WriteLine("Calculating distance from {0} to {1}", sourceFact, targetFact);

      PrintShortestPath(sourceFact, targetFact);

      PrintSomePath(sourceFact, targetFact);

      //PrintShortestPathFromAllShortestPaths(sourceFact, targetFact);

    }

    private void PrintShortestPath(AnalysisFact sourceFact, AnalysisFact targetFact) {

      Func<AnalysisReason, double> weights = (reason) => 1.0;

      Stopwatch stopwatch = new Stopwatch();
      stopwatch.Start();

      TryFunc<AnalysisFact, IEnumerable<AnalysisReason>> tryGetPath = this.ShortestPathsDijkstra(weights, sourceFact);

      IEnumerable<AnalysisReason> path;
      if (tryGetPath(targetFact, out path)) {
        Console.WriteLine("Got shortest path of length {0}", path.Count());

        PrintPath(path);
      }
      else {
        Console.WriteLine("Couldn't get path!");
      }

      stopwatch.Stop();

      Console.WriteLine("Finding shortest path took {0} seconds", stopwatch.Elapsed);
    }

    private void PrintSomePath(AnalysisFact sourceFact, AnalysisFact targetFact) {

      Stopwatch stopwatch = new Stopwatch();
      stopwatch.Start();

      var dfs = new DepthFirstSearchAlgorithm<AnalysisFact, AnalysisReason>(this);



      Dictionary<AnalysisFact, AnalysisReason> vertexPredecessors = new Dictionary<AnalysisFact, AnalysisReason>();

      dfs.TreeEdge += (AnalysisReason reason) => {


        vertexPredecessors.Add(reason.MainCause, reason);

        if (reason.MainCause == targetFact) {
          dfs.Services.CancelManager.Cancel();

        }
      };


      dfs.SetRootVertex(sourceFact);

      //do the search
      dfs.Compute();

      // reconstruct the path

      IList<AnalysisReason> path = new List<AnalysisReason>();

      AnalysisFact iterator = targetFact;

      while (iterator != sourceFact) {
        AnalysisReason reason;
        if (vertexPredecessors.TryGetValue(iterator, out reason)) {
          path.Add(reason);
          iterator = reason.Result;
        }
        else {
          throw new Exception("Couldn't get predecessor reason for iterator " + iterator);
        }


      }

      Console.WriteLine("Got some path of length {0}", path.Count());

      //PrintPath(path.Reverse());

      Console.WriteLine("Finding some path took {0} seconds", stopwatch.Elapsed);
    }


    private void PrintShortestPathFromAllShortestPaths(AnalysisFact sourceFact, AnalysisFact targetFact) {

      Func<AnalysisReason, double> weights = (reason) => 1.0;

      Stopwatch stopwatch = new Stopwatch();
      stopwatch.Start();

      var fw = new QuickGraph.Algorithms.ShortestPath.FloydWarshallAllShortestPathAlgorithm<AnalysisFact, AnalysisReason>(this, weights);

      // compute
      fw.Compute();


      IEnumerable<AnalysisReason> path;
      if (fw.TryGetPath(sourceFact, targetFact, out path)) {
        Console.WriteLine("Got shortest path of length {0}", path.Count());

        PrintPath(path);
      }
      else {
        Console.WriteLine("Couldn't get path!");
      }

      stopwatch.Stop();

      Console.WriteLine("Finding shortest path via ALL shortest paths took {0} seconds", stopwatch.Elapsed);
    }

    void PrintPath(IEnumerable<AnalysisReason> path) {
      foreach (var e in path) {
        //Console.WriteLine("Fact: {0}", e.Result);

        if (e.MainCause is NonVirtualDispatchFact) {
          // skip
        }
        else {
          Console.WriteLine("\t{0}", e);
        }
        
      }
    }
#endif

    // Fact getter/creators
    //
    // There is a lot of boiler plate here; a sign of bad design on my part.

    internal TypeConstructedFact GetTypeConstructedFact(ITypeDefinition constructedType) {
      TypeConstructedFact result;

      if (!typeConstructionFactsByType.TryGetValue(constructedType, out result)) {
        result = new TypeConstructedFact(this) { TypeConstructed = constructedType };

        typeConstructionFactsByType[constructedType] = result;
      }

      return result;
    }

    internal MethodReachedFact GetMethodReachedFact(IMethodDefinition methodReached) {
      MethodReachedFact result;

      if (!methodReachedFactsByMethod.TryGetValue(methodReached, out result)) {
        result = new MethodReachedFact(this) { ReachedMethod = methodReached };

        methodReachedFactsByMethod[methodReached] = result;
      }

      return result;
    }

    internal VirtualDispatchFact GetVirtualDispatchFact(IMethodDefinition methodDispatchedUpon) {
      VirtualDispatchFact result;

      if (!virtualDispatchFactsByMethod.TryGetValue(methodDispatchedUpon, out result)) {
        result = new VirtualDispatchFact(this) { DispatchMethod = methodDispatchedUpon };

        virtualDispatchFactsByMethod[methodDispatchedUpon] = result;
      }

      return result;
    }


    internal AnalysisFact GetEntryPointReachedFact() {
      return entryPointReachedFact;
    }

    internal NonVirtualDispatchFact GetNonVirtualDispatchFact(IMethodDefinition methodDispatchedUpon) {
      NonVirtualDispatchFact result;

      if (!nonVirtualDispatchFactsByMethod.TryGetValue(methodDispatchedUpon, out result)) {
        result = new NonVirtualDispatchFact(this) { DispatchMethod = methodDispatchedUpon };

        nonVirtualDispatchFactsByMethod[methodDispatchedUpon] = result;
      }

      return result;
    }

    // Reason factories

    public MethodReachedReason MethodReachedByDispatchAgainstNonVirtualMethod(IMethodDefinition dispatchAgainstMethod) {
      return new MethodReachedBecauseDispatchedNonVirtuallyReason(this, dispatchAgainstMethod);
    }

    public MethodReachedReason MethodReachedBecauseEntryPoint() {
      return new MethodReachedBecauseEntryPointReason(this);
    }

    public MethodReachedReason MethodReachedByDispatchAgainstVirtualMethodWithTypeConstructed(IMethodDefinition dispatchAgainstMethod, ITypeDefinition constructedType) {
      return new MethodReachedBecauseDispatchedVirtuallyReason(this, dispatchAgainstMethod, constructedType);
    }



    public DispatchReachedReason DispatchReachedBecauseContainingMethodWasReached(IMethodDefinition containingMethod) {
      return new DispatchReachedReason(this, containingMethod);
    }


    public TypeConstructedReason TypeConstructedBecauseAllocatingMethodReached(IMethodDefinition allocatingMethod) {
      return new TypeConstructedBecauseAllocatingMethodReachedReason(this, allocatingMethod);
    }

    public TypeConstructedReason TypeConstructedBecauseConstructorIsEntryPoint() {
      return new TypeConstructedBecauseEntryPointReason(this);
    }


    // Construction Notification (this is absurd)

    internal void NoteAnalysisFactCreated(AnalysisFact fact) {
      allFacts.Add(fact);
    }

    internal void NoteAnalysisReasonCreated(AnalysisReason fact) {
      allReasons.Add(fact);
    }

#if QUICKGRAPH
    // IVertexSet<IAnalysisFact> implementations

    bool IVertexSet<AnalysisFact>.IsVerticesEmpty { get { return allFacts.Count() > 0; } }

    int IVertexSet<AnalysisFact>.VertexCount { get { return allFacts.Count(); } }

    IEnumerable<AnalysisFact> IVertexSet<AnalysisFact>.Vertices { get { return allFacts; } }

    // IEdgeSet<IAnalysisFact, AnalysisReason> implementations

    int IEdgeSet<AnalysisFact, AnalysisReason>.EdgeCount {
      get {
        return allReasons.Count();
      }
    }

    IEnumerable<AnalysisReason> IEdgeSet<AnalysisFact, AnalysisReason>.Edges {
      get {
        return allReasons;
      }
    }

    bool IEdgeSet<AnalysisFact, AnalysisReason>.IsEdgesEmpty {
      get {
        return false; // There must always be an edge from the entry point fact to some entry method
      }
    }

    bool IEdgeSet<AnalysisFact, AnalysisReason>.ContainsEdge(AnalysisReason edge) {
      return true; // any reason created must be an edge
    }

#endif

    // Delegates

    public static bool TryReasonsForFact(AnalysisFact fact, out IEnumerable<AnalysisReason> result) {
      result = fact.GetReasons();

      return true;
    }
  }


}