using Microsoft.Cci;
using Microsoft.Cci.Analysis;
using Microsoft.Cci.MetadataReader;
using Microsoft.Cci.UtilityDataStructures;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace CDFG
{
    using Microsoft.Cci.Immutable;
    using CFG = ControlAndDataFlowGraph<EnhancedBasicBlock<Instruction>, Instruction>;

    public class CallCloseOrDisposeAnalysisVisitor : MetadataVisitor
    {
        private IMetadataHost host;
        private PdbReader pdbReader;
        private IMethodDefinition currentMethod;
        private readonly NamespaceTypeReference iDisposable;

        public CallCloseOrDisposeAnalysisVisitor(IMetadataHost host, PdbReader pdbReader)
        {
            this.host = host;
            this.pdbReader = pdbReader;
            this.iDisposable = new NamespaceTypeReference(this.host, this.host.PlatformType.SystemObject.ContainingUnitNamespace,
            this.host.NameTable.GetNameFor("IDisposable"), 0, false, false, true, PrimitiveTypeCode.Reference);

        }

        public override void Visit(IMethodBody methodBody)
        {

            var method = methodBody.MethodDefinition;
            this.currentMethod = method;
            if (methodBody.Operations != null)
            {
            var cdfg = ControlAndDataFlowGraph<EnhancedBasicBlock<Instruction>, Instruction>.GetControlAndDataFlowGraphFor(this.host, methodBody, this.pdbReader);
            var cfgQueries = new ControlGraphQueries<EnhancedBasicBlock<Instruction>, Instruction>(cdfg);
                this.FindObjectSourcesWithoutSinks(method, cdfg);
            }
        

        return ;
        }

        private void EmitViolation(Instruction source, IMethodDefinition method)
        {
            var module = TypeHelper.GetDefiningUnit(method.ContainingTypeDefinition);
            var Module = module.Name.Value;
            string methodName = MemberHelper.GetMethodSignature(method, NameFormattingOptions.DocumentationId);
            var Type = TypeHelper.GetTypeName(method.ContainingType);
            var Member = methodName;
            var Statement = source.ToString();
            string allocationTypeName = "<unknown>";
            string Comments;
            string Source;
            int Line;

            var methodRef = source.Operation.Value as IMethodReference;
            if (methodRef != null)
            {
                if (source.Operation.OperationCode == OperationCode.Newobj)
                {
                    allocationTypeName = TypeHelper.GetTypeName(methodRef.ContainingType); // a constructor
                    Comments = "new " + allocationTypeName;
                }
                else
                {
                    allocationTypeName = TypeHelper.GetTypeName(methodRef.Type); // a returned object
                    Comments = "return value of " + MemberHelper.GetMethodSignature(methodRef);
                }
            }
            var initialParameter = source.Operation as InitialParameterAssignment;
            if (initialParameter != null)
            {
                var p = (IParameterDefinition)source.Operation.Value;
                allocationTypeName = TypeHelper.GetTypeName(p.Type);
                Comments = "initial method byref parameter";
            }
            var callbyref = source.Operation as CallByRefAssignment;
            if (callbyref != null)
            {
                var p = callbyref.Parameter;
                allocationTypeName = TypeHelper.GetTypeName(p.Type);
                Comments = "from method byref parameter: " + callbyref.Parameter.ToString();
            }
            var SourceType = allocationTypeName;

            if (null != source.Operation.Location)
            {
                IPrimarySourceLocation psl = this.pdbReader.GetPrimarySourceLocationsFor(source.Operation.Location).FirstOrDefault();
                if (null != psl)
                {
                    Source = psl.Document.Location;
                    Line = psl.EndLine;
                }
            }

            Console.WriteLine("Violation: ");

        }

        /// <summary>
        /// Add sources that are disposable on entry, such as byref disposables
        /// </summary>
        /// <param name="dictionary"></param>
        private IEnumerable<Instruction> InitialSources(FMap<IParameterDefinition, Instruction> initialParameters)
        {
            foreach (var p in this.currentMethod.Parameters)
            {
                Instruction initialDummyInit;
                if (p.IsByReference && !p.IsOut && IsDisposableType(p.Type) && initialParameters.TryGetValue(p, out initialDummyInit))
                {
                    yield return initialDummyInit;
                }
            }
        }

        private IEnumerable<Instruction> Sources(Instruction instruction, CFG cdfg)
        {
            switch (instruction.Operation.OperationCode)
            {
                case OperationCode.Newobj:
                    var constructorReference = instruction.Operation.Value as IMethodReference;
                    var type = constructorReference.ContainingType;
                    if (constructorReference != null && !type.IsCompilerGenerated() && IsDisposableType(type))
                    {
                        yield return instruction; // produces the source object
                    }
                    break;

                case OperationCode.Call:
                case OperationCode.Callvirt:
                    var methodRef = instruction.Operation.Value as IMethodReference;
                    if (methodRef != null)
                    {
                        if (methodRef != null && !methodRef.IsGetter())
                        {
                            if (!methodRef.Type.IsCompilerGenerated() && IsDisposableType(methodRef.Type))
                            {
                                yield return instruction; // produces the source object
                            }

                            var args = instruction.Operand2 as Instruction[];
                            if (args != null)
                            {
                                foreach (var p in methodRef.Parameters)
                                {
                                    if (p.IsByReference && !p.Type.IsCompilerGenerated() && IsDisposableType(p.Type) && p.Index < args.Length)
                                    {
                                        var arg = args[p.Index];
                                        if (arg != null)
                                        {
                                            var loc = cdfg.LocalOrParameter(arg);
                                            var pseudoDef = instruction[loc];
                                            if (pseudoDef != null)
                                            {
                                                yield return pseudoDef;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    break;

            }
        }

        private IEnumerable<Instruction> Sinks(BasicBlock<Instruction> block, Instruction instruction, CFG cdfg)
        {
            switch (instruction.Operation.OperationCode)
            {
                case OperationCode.Call:
                case OperationCode.Callvirt:
                    {
                        var methodRef = instruction.Operation.Value as IMethodReference;
                        if (methodRef != null)
                        {
                            if (IsDisposeMethod(methodRef))
                            {
                                var constraint = block.CallConstraint(instruction);
                                if (methodRef.ContainingType.IsValueType || constraint != null && (constraint.IsValueType || constraint is IGenericParameter))
                                {
                                    // passing the address, figure out what local
                                    var loc = cdfg.LocalOrParameter(instruction.Operand1);
                                    var toDispose = instruction[loc];
                                    yield return toDispose;
                                }
                                else
                                {
                                    yield return instruction.Operand1; // consumes the "this" parameter
                                }
                            }

                            var args = instruction.Operand2 as Instruction[];
                            if (args != null)
                            {
                                foreach (var p in methodRef.Parameters)
                                {
                                    if (p.IsByReference && IsDisposableType(p.Type) && p.Index < args.Length || PassingObligationTo(methodRef, p))
                                    {
                                        var arg = args[p.Index];
                                        if (arg != null) yield return arg;
                                    }
                                }
                            }
                        }
                        break;
                    }
                case OperationCode.Stfld:
                    // consider fields escaping
                    yield return instruction.Operand2 as Instruction;
                    break;

                case OperationCode.Stsfld:
                    // consider fields escaping
                    yield return instruction.Operand1;
                    break;

                case OperationCode.Ret:
                    if (instruction.Operand1 != null && IsDisposableType(this.currentMethod.Type))
                    {
                        yield return instruction.Operand1; // consumes the returned value
                    }
                    // by ref parameters are also sinks
                    foreach (var p in this.currentMethod.Parameters)
                    {
                        Instruction finalParameterValue;
                        if ((p.IsByReference || p.IsOut) && IsDisposableType(p.Type) && instruction.PostParamDefs != null && instruction.PostParamDefs.TryGetValue(p, out finalParameterValue))
                        {
                            yield return finalParameterValue;
                        }
                    }
                    break;


            }
        }

        private bool PassingObligationTo(IMethodReference method, IParameterTypeInformation p)
        {
            switch (method.Name.Value)
            {
                case "Add": return true;
            }
            return false;
        }

        private static bool IsDisposeMethod(IMethodReference methodRef)
        {
            // TODO: make this more appropriate
            if (methodRef == null) return false;
            var name = methodRef.Name.Value;
            if (name == "Dispose" || name.EndsWith(".Dispose")) return true;
            if (name == "Close" || name.EndsWith(".Close")) return true;
            return false;
        }

        private bool IsDisposableType(ITypeReference type)
        {
            return TypeHelper.Type1ImplementsType2(type.ResolvedType, this.iDisposable);
        }

        private void FindObjectSourcesWithoutSinks(IMethodDefinition method, CFG cdfg)
        {
            var sources = new HashSet<Instruction>();
            var sinks = new HashSet<Instruction>();

            sources.Add(InitialSources(cdfg.AllBlocks[0].ParamDefs));
            // visit each instruction and determine if it is a call to dispose or an allocation
            foreach (var block in cdfg.AllBlocks)
            {
                foreach (var instruction in block.Instructions)
                {
                    sources.Add(Sources(instruction, cdfg));
                    sinks.Add(Sinks(block, instruction, cdfg));
                }
            }

            // Now follow the data flow paths from all the deallocations and mark all instructions thus reached
            var reachingDefs = new FindReachingMayDefinitions(null);

            foreach (var sink in sinks) { reachingDefs.StartFrom(sink); }
            reachingDefs.IterateFlowPaths();

            // now see which allocations reach no deallaction
            foreach (var source in sources)
            {
                if (reachingDefs.IsReachable(source)) continue;

                this.EmitViolation(source, method);
            }
        }

    }

    public static class Extensions
    {
        public static void Add<T>(this HashSet<T> set, IEnumerable<T> data)
        {
            foreach (var d in data) { set.Add(d); }
        }

        public static bool IsCompilerGenerated(this ITypeReference type)
        {
            return TypeHelper.IsCompilerGenerated(type.ResolvedType);
        }
        public static bool IsGetter(this IMethodReference method)
        {
            return MemberHelper.IsGetter(method.ResolvedMethod);
        }

        public static bool IsSetter(this IMethodReference method)
        {
            return MemberHelper.IsSetter(method.ResolvedMethod);
        }
    }
}
