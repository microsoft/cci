using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Cci;
using Microsoft.Cci.MutableCodeModel;
using System.Diagnostics.Contracts;
using System.IO;

namespace ILGarbageCollect.Instrumentation {
  /*
   * 
   * To use this instrumentation we require the following code to be compiled
   * save this code as BlockCounter.cs
   * run:
   *    csc /t:library /out:Zombie.dll BlockCounter.cs 
   * in the directory where you will run your instrumented code.
   
   
using System;
using System.Collections.Generic;


class Comparer : IEqualityComparer<Trace> {
  public bool Equals(Trace x, Trace y) {
    if (x.Count != y.Count) return false;
    for (int i = 0; i < x.Count; i++)
      if (x[i] != y[i]) return false;
    return true;
  }

  public int GetHashCode(Trace obj) {
    int c = obj[0].GetHashCode() + obj[obj.Count - 1].GetHashCode();
    return c;
  }
}

class InternedComparer : IEqualityComparer<string>, IComparer<string> {
  public static InternedComparer instance = new InternedComparer();

  public bool Equals(string x, string y) {
    return x == y;
  }

  public int GetHashCode(string obj) {
    return obj.GetHashCode();
  }

  public int Compare(string x, string y) {
    return x.CompareTo(y);
  }
}

class Trace : List<string> {
  public void AddString(string op) {
    base.Add(op);
  }

  public bool ContainsString(string op) {
    return base.Contains(op);
  }
}

public class BlockCounter {
  // global instance.
  private readonly static BlockCounter Instance = new BlockCounter();
  private readonly Dictionary<Trace, long> map = new Dictionary<Trace, long>(new Comparer());
  private Trace trace = new Trace();

  ~BlockCounter() {
    foreach (var pair in Instance.map) {
      Console.Write(pair.Value + " ");
      foreach (var s in pair.Key)
        Console.Write("\t" + s);
      Console.WriteLine();
    }
    
    // get the last trace if there is one.
    if (Instance.trace != null) {
      long count;
      if (!Instance.map.TryGetValue(Instance.trace, out count))
        count = 0;
      Console.Write(count + " ");
      foreach(var s in Instance.trace)
        Console.Write("\t" + s);
      Console.WriteLine();        
    }
  }

  public static void VisitInstruction(string operation) {
    Instance.trace.AddString(operation);  
  }

  public static void VisitMaybeAnchorInstruction(string operation) {
    if (Instance.trace.ContainsString(operation)) { // new trace as we are backwards branching to this
      long count = 0;

      // see if we need to create a new trace: if we have not seen this one before.
      if (!Instance.map.TryGetValue(Instance.trace, out count)) {
        Instance.map[Instance.trace] = 0;
      }
      // update the count  
      Instance.map[Instance.trace] = count + 1;

      // new trace, so start fresh.
      //Instance.trace.Clean();
      Instance.trace = new Trace();
    }
    VisitInstruction(operation);
  }
}
   * */

  public class TraceInstrumentor {
    internal class DumbBlockMethodVisitor : ILMethodBodyRewriter {

      private readonly string methodName;
      private readonly IDictionary<uint, uint> anchors;
      private IOperation specialOp = null;
      private string specialOpString = null;

      private readonly IMethodReference blockCounterDotVisitMaybeAnchorInstruction;
      private readonly IMethodReference blockCounterDotVisitInstruction;


      public DumbBlockMethodVisitor(IMetadataHost host, MethodBody method, string methodName)
        : base(host, method) {

        Console.WriteLine(MemberHelper.GetMemberSignature(method.MethodDefinition, NameFormattingOptions.None));

        this.anchors = new Dictionary<uint, uint>();
        this.methodName = //methodName;// method.MethodDefinition.ToString(); 
          MemberHelper.GetMethodSignature(method.MethodDefinition, NameFormattingOptions.SmartTypeName);

        var zombieAssemblyIdentity = new AssemblyIdentity(host.NameTable.GetNameFor("Zombie"), "", new Version(1, 0, 0), new byte[0], "Zombie.dll");
        var zombieAssemblyReference = new AssemblyReference() {
          Host = host,
          AssemblyIdentity = zombieAssemblyIdentity
        };
        var counterClassNamespaceReference = ILMethodBodyRewriter.CreateTypeReference(host, zombieAssemblyReference, "BlockCounter");

        this.blockCounterDotVisitMaybeAnchorInstruction = new Microsoft.Cci.MethodReference(
          this.host, counterClassNamespaceReference,
          CallingConvention.Default,
          host.PlatformType.SystemVoid,
          host.NameTable.GetNameFor("VisitMaybeAnchorInstruction"),
          0,
          host.PlatformType.SystemString);

        this.blockCounterDotVisitInstruction = new Microsoft.Cci.MethodReference(
          this.host, counterClassNamespaceReference,
          CallingConvention.Default,
          host.PlatformType.SystemVoid,
          host.NameTable.GetNameFor("VisitInstruction"),
          0,
          host.PlatformType.SystemString);
      }

      protected override void RewriteMaxStack(ushort maxstack) {
        ushort x = 2;
        x += maxstack;
        base.RewriteMaxStack(x);
      }

      private bool IsAnchor(IOperation op) {
        // only terminate trace on backwards branch
        uint jumpedFrom = 0;
        if (this.anchors.TryGetValue(op.Offset, out jumpedFrom))
          return jumpedFrom > op.Offset; //backward branch
        return false;
      }

      protected override void RewriteAny(IOperation op) {
        var operationAsString = "op: " + methodName + " " + op.OperationCode.ToString() + ";" + string.Format("{0:X}", op.Offset);

        if (op.OperationCode == OperationCode.Constrained_ || op.OperationCode == OperationCode.Ldftn) {
          this.specialOp = op;
          this.specialOpString = operationAsString;
          return;
        }


        if (this.specialOp != null) {
          if (this.IsAnchor(specialOp)) {
            generator.Emit(OperationCode.Ldstr, this.specialOpString);
            generator.Emit(OperationCode.Call, this.blockCounterDotVisitMaybeAnchorInstruction);
          } else {
            generator.Emit(OperationCode.Ldstr, this.specialOpString);
            generator.Emit(OperationCode.Call, this.blockCounterDotVisitInstruction);
          }

          if (this.IsAnchor(op)) {
            generator.Emit(OperationCode.Ldstr, operationAsString);
            generator.Emit(OperationCode.Call, this.blockCounterDotVisitMaybeAnchorInstruction);
          } else {
            generator.Emit(OperationCode.Ldstr, operationAsString);
            generator.Emit(OperationCode.Call, this.blockCounterDotVisitInstruction);
          }
          base.RewriteAny(this.specialOp);
          base.RewriteAny(op);
          this.specialOp = null;
          this.specialOpString = null;
          return;
        }

        // only terminate trace on backwards branch
        if (this.IsAnchor(op)) {
          generator.Emit(OperationCode.Ldstr, operationAsString);
          generator.Emit(OperationCode.Call, this.blockCounterDotVisitMaybeAnchorInstruction);
          base.RewriteAny(op);
          return;
        }

        // else carry on.
        generator.Emit(OperationCode.Ldstr, operationAsString);
        generator.Emit(OperationCode.Call, this.blockCounterDotVisitInstruction);
        base.RewriteAny(op);
      }

      protected override void Start() {
        foreach (var op in base.methodBody.Operations) {
          switch (op.OperationCode) {
            case OperationCode.Beq:
            case OperationCode.Bge:
            case OperationCode.Bge_Un:
            case OperationCode.Bgt:
            case OperationCode.Bgt_Un:
            case OperationCode.Ble:
            case OperationCode.Ble_Un:
            case OperationCode.Blt:
            case OperationCode.Blt_Un:
            case OperationCode.Bne_Un:
            case OperationCode.Br:
            case OperationCode.Brfalse:
            case OperationCode.Brtrue:
            case OperationCode.Leave:
            case OperationCode.Beq_S:
            case OperationCode.Bge_S:
            case OperationCode.Bge_Un_S:
            case OperationCode.Bgt_S:
            case OperationCode.Bgt_Un_S:
            case OperationCode.Ble_S:
            case OperationCode.Ble_Un_S:
            case OperationCode.Blt_S:
            case OperationCode.Blt_Un_S:
            case OperationCode.Bne_Un_S:
            case OperationCode.Br_S:
            case OperationCode.Brfalse_S:
            case OperationCode.Brtrue_S:
            case OperationCode.Leave_S:
              uint x = (uint)op.Value;
              if (!this.anchors.ContainsKey(x)) this.anchors[x] = op.Offset;
              break;
            case OperationCode.Switch:
              uint[] offsets = op.Value as uint[];
              foreach (var offset in offsets) {
                if (!this.anchors.ContainsKey(offset)) this.anchors[offset] = op.Offset;
              }
              break;
            default:
              break;
          }

        }
        base.Start();
      }
    }

    public class BlockInstrumentator : MetadataRewriter {
      public BlockInstrumentator(IMetadataHost host) : base(host) {}

      public override List<IMethodDefinition> Rewrite(List<IMethodDefinition> methods) {
        foreach (var method in methods) {
          if (method.IsAbstract == false) {
            new DumbBlockMethodVisitor(
              this.host,
              method.Body as MethodBody,
              MemberHelper.GetMemberSignature(method, NameFormattingOptions.None)
            ).Rewrite();
          }
        }
        return base.Rewrite(methods);
      }
    }
  }
}
