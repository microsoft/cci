using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Diagnostics.Contracts;

using Microsoft.Cci;

using ILGarbageCollect.Mark;

namespace ILGarbageCollect.Summaries {

    internal class CompleteBytecodeMethodSummarizer : IMethodSummarizer {
        public CompleteBytecodeMethodSummarizer() { }

        public ReachabilitySummary SummarizeMethod(IMethodDefinition method, WholeProgram wholeProgram) {

            if (method.IsExternal == false && method.IsAbstract == false) {
                ReachabilitySummary summary = new ReachabilitySummary();
                IMethodDefinition target;


                // foreach MSIL instruction in the method
                foreach (var op in method.Body.Operations) {
                    switch (op.OperationCode) {
                        // non virtual method calls: just add static type
                        case OperationCode.Newobj:
                        case OperationCode.Call:
                        case OperationCode.Calli:
                            target = (op.Value as IMethodReference).ResolvedMethod;
                            summary.NonvirtuallyCalledMethods.Add(target);
                            break;

                        case OperationCode.Ldvirtftn:
                        case OperationCode.Callvirt:
                            target = (op.Value as IMethodReference).ResolvedMethod;            

                            if (target.IsVirtual == false) {
                                summary.NonvirtuallyCalledMethods.Add(target);
                            } else {
                                ITypeDefinition typeDefiningTarget = target.ContainingTypeDefinition;                                
                                IMethodDefinition targetUnspecialized = GarbageCollectHelper.UnspecializeAndResolveMethodReference(op.Value as IMethodReference);

                                // find all possible implementations of virtual call
                                foreach (ITypeDefinition subType in new ITypeDefinition[] { typeDefiningTarget }.Concat(wholeProgram.ClassHierarchy().AllSubClassesOfClass(typeDefiningTarget))) {
                                    if (GarbageCollectHelper.TypeIsConstructable(subType)) {

                                        // walk class hierarchy from subType up to (including) typeDefiningTarget, looking for targetUnspecialized.
                                        ICollection<IMethodDefinition> implementationsOfMethodDefinitionForSubType = GarbageCollectHelper.Implements(subType, typeDefiningTarget, targetUnspecialized);

                                        // we have to have found at least 1 implementation
                                        Contract.Assert(implementationsOfMethodDefinitionForSubType.Count() > 0);

                                        // add all of them as virtually called methods
                                        foreach (IMethodDefinition implementationOfTarget in implementationsOfMethodDefinitionForSubType) {
                                            summary.VirtuallyCalledMethods.Add(implementationOfTarget);
                                        }
                                    }
                                }

                            }
                            break;

                        default:
                            break;
                    }
                }

                return summary;
            } else {
                return null;
            }
        }
    }


  internal class SimpleBytecodeMethodSummarizer : IMethodSummarizer {
    public SimpleBytecodeMethodSummarizer() {

    }

    public ReachabilitySummary SummarizeMethod(IMethodDefinition methodDefinition, WholeProgram wholeProgram) {
        
      // if there is an implementation available (e.g. we can get to opcodes)
      if (methodDefinition.IsExternal == false && methodDefinition.IsAbstract == false) {
        BytecodeVisitor visitor = new BytecodeVisitor();

        // foreach MSIL instruction in the method
        foreach (var op in methodDefinition.ResolvedMethod.Body.Operations) {
          visitor.Visit(op); // handle the opcode
        }

        return visitor.GetSummary();
      }
      else {
        return null;
      }
    }

    internal class BytecodeVisitor {
      readonly ReachabilitySummary summary;

      internal BytecodeVisitor() {
        summary = new ReachabilitySummary();
      }

      internal ReachabilitySummary GetSummary() {
        return summary;
      }

      internal virtual void Visit(IOperation op) {
        IMethodDefinition target;

        switch (op.OperationCode) {
          case OperationCode.Newobj:
            target = ResolveMethodReference(op.Value as IMethodReference);

            if (target != null) {
              Contract.Assert(!(target is Dummy));

              if (target.ContainingType is INamedTypeReference) {
                //Console.WriteLine("Got newobj to {0}", ((INamedTypeReference)target.ContainingType).Name);
              }

              ITypeDefinition constructedType = target.ContainingType.ResolvedType;
              Contract.Assert(GarbageCollectHelper.TypeIsConstructable(constructedType));

              summary.ReachableTypes.Add(constructedType);
              summary.ConstructedTypes.Add(constructedType);
              summary.NonvirtuallyCalledMethods.Add(target);
            }
            break;

          case OperationCode.Box:
            // Boxing a struct means a callvirt could possibly
            // dispatch to one of its methods.

            ITypeDefinition boxedType = ResolveTypeReference(op.Value as ITypeReference);

            if (boxedType != null) {
              // Guard for structs here because box to a reference type is a no-op but allowed and
              // the Code Contracts instrumentation does this.
              if (boxedType.IsStruct) {
                Contract.Assert(GarbageCollectHelper.TypeIsConstructable(boxedType));
                summary.ConstructedTypes.Add(boxedType);
              }
            }
            break;


          case OperationCode.Ldtoken:
            if (op.Value is IFieldReference) {
              IFieldDefinition field = ResolveFieldReference(op.Value as IFieldReference);

              if (field != null) {
                summary.ReachableFields.Add(field);
              }
            }

            // t-devinc: Need to handle: method and type tokens, as well as generics.
            // Fully supporting generics here may be very tricky -- this is a place
            // where operating over specialized bytecode could perhaps help.
            break;

          /* Notes on call/callvirt:
           * 
           * We can get a call on a virtual method when that method is called via 'base'
           * We can also get a callvirt on a non-virtual method whenever the compiler
           * feels like it? This seems a little wonky.
           */
          case OperationCode.Ldftn:
          case OperationCode.Call:
            target = ResolveMethodReference(op.Value as IMethodReference);

            if (target != null) {
              if (target.ContainingType is INamedTypeReference) {
                //Console.WriteLine("Got call to {0}", target);
              }

              summary.NonvirtuallyCalledMethods.Add(target);

              if (target.Name.Value == "CreateInstance") {

                // t-devinc: gross, clean this up

                INamespaceTypeDefinition containingTypeDefinition = target.ContainingTypeDefinition as INamespaceTypeDefinition;
                if (containingTypeDefinition != null && containingTypeDefinition.Name.Value == "Activator") {
                  if (containingTypeDefinition.ContainingNamespace.Name.Value == "System") {
                    IGenericMethodInstance targetAsGenericMethodInstance = target as IGenericMethodInstance;

                    if (targetAsGenericMethodInstance != null) {
                      // We have a call to 'new T()';
                      ITypeDefinition definitionForT = targetAsGenericMethodInstance.GenericArguments.First().ResolvedType;

                      summary.ConstructedTypeParameters.Add((IGenericParameter)definitionForT);
                    }
                  }
                }

              }
            }
            break;

          case OperationCode.Ldvirtftn:
          case OperationCode.Callvirt:

            target = ResolveMethodReference(op.Value as IMethodReference);

            if (target != null) {
              if (target.ContainingType is INamedTypeReference) {
                //Console.WriteLine("Got callvirt to {0}", target);
              }

              if (target.IsVirtual) {
                
                summary.VirtuallyCalledMethods.Add(target);
              }
              else {
                // In its infinite wisdom, sometimes the compiler gives us a callvirt on a non-virtual method

                summary.NonvirtuallyCalledMethods.Add(target);
              }
            }
            break;

          case OperationCode.Ldfld:
          case OperationCode.Ldflda:
          case OperationCode.Ldsfld:
          case OperationCode.Ldsflda:

          /* For now we treat loads and stores as the same -- we'll want to be smarter about this in the future */
          case OperationCode.Stfld:
          case OperationCode.Stsfld:
            IFieldDefinition fieldDefinition = ResolveFieldReference(op.Value as IFieldReference);

            if (fieldDefinition != null) {
              summary.ReachableTypes.Add(fieldDefinition.ContainingTypeDefinition);
              summary.ReachableFields.Add(fieldDefinition);
            }
            break;

          default:
            break;
        }
      }

      // Perhaps summarizers should work solely on references
      // and not involve definitions at all?

      /// <summary>
      /// Resolve a reference, adding a note in the summary if the resolution failed.
      /// </summary>
      protected IMethodDefinition ResolveMethodReference(IMethodReference reference) {
        IMethodDefinition methodDefinition = reference.ResolvedMethod;

        if (!(methodDefinition is Dummy)) {
          return methodDefinition;
        }
        else {
          summary.UnresolvedReferences.Add(reference);
          return null;
        }
      }

      protected ITypeDefinition ResolveTypeReference(ITypeReference reference) {
        ITypeDefinition typeDefinition = reference.ResolvedType;

        if (!(typeDefinition is Dummy)) {
          return typeDefinition;
        }
        else {
          summary.UnresolvedReferences.Add(reference);
          return null;
        }
      }

      protected IFieldDefinition ResolveFieldReference(IFieldReference reference) {
        IFieldDefinition fieldDefinition = reference.ResolvedField;

        if (!(fieldDefinition is Dummy)) {
          return fieldDefinition;
        }
        else {
          summary.UnresolvedReferences.Add(reference);
          return null;
        }
      }
    }
  }
}
