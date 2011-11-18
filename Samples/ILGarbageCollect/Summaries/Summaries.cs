using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Cci;
using System.Diagnostics.Contracts;
using ILGarbageCollect.Mark;

namespace ILGarbageCollect.Summaries {


  // For now, summaries contain consist of sets of definitions.
  // It may make more sense to have summaries consist of sets of
  // references and only resolve those references in the core analysis.

  public class ReachabilitySummary {
    public ISet<IMethodDefinition> NonvirtuallyCalledMethods { get; private set; }

    public ISet<IMethodDefinition> VirtuallyCalledMethods { get; private set; } // the direct method itself, not any potential overrides

    public ISet<IFieldDefinition> ReachableFields { get; private set; }

    public ISet<ITypeDefinition> ReachableTypes { get; private set; }

    public ISet<ITypeDefinition> ConstructedTypes { get; private set; }

    public ISet<IGenericParameter> ConstructedTypeParameters { get; private set; }

    public ISet<IReference> UnresolvedReferences { get; private set; }

    public ReachabilitySummary() {
      this.NonvirtuallyCalledMethods = new HashSet<IMethodDefinition>(new MethodDefinitionEqualityComparer());
      this.VirtuallyCalledMethods = new HashSet<IMethodDefinition>(new MethodDefinitionEqualityComparer());
      this.ReachableFields = new HashSet<IFieldDefinition>(new FieldDefinitionEqualityComparer());
      this.ReachableTypes = new HashSet<ITypeDefinition>(new TypeDefinitionEqualityComparer());
      this.ConstructedTypes = new HashSet<ITypeDefinition>(new TypeDefinitionEqualityComparer());
      this.ConstructedTypeParameters = new HashSet<IGenericParameter>(new TypeDefinitionEqualityComparer());

      this.UnresolvedReferences = new HashSet<IReference>(new ReferenceEqualityComparer());
    }

  }

  internal class SummariesHelper {
    // This ignores parameter types, which we'll need at some point
    internal static bool MethodMatchesFullyQualifiedName(string fullyQualifiedName, IMethodDefinition definition) {
      string[] nameComponents = fullyQualifiedName.Split('.');


      INamedEntity nameIterator = definition;

      for (int componentIndex = nameComponents.Length - 1; componentIndex >= 0; componentIndex--) {
        string nameComponent = nameComponents[componentIndex];

        if (nameIterator.Name.Value.Equals(nameComponent)) {
          if (nameIterator is ITypeDefinitionMember) {
            ITypeDefinition containingTypeDefinition = ((ITypeDefinitionMember)nameIterator).ContainingTypeDefinition;

            if (containingTypeDefinition is INamedTypeDefinition) {
              nameIterator = ((INamedTypeDefinition)containingTypeDefinition);
            }
            else {
              return false;
            }
          }
          else if (nameIterator is INamespaceMember) {
            INamespaceDefinition containingNamespace = ((INamespaceMember)nameIterator).ContainingNamespace;

            nameIterator = containingNamespace;
          }
          else {
            return false;
          }
        }
        else {
          return false;
        }
      }

      return true;
    }

    /// <summary>
    /// Finds all the com objects in a given namespace and marks the methods of their subclasses as reachable.
    /// 
    /// Perhaps too blunt.
    /// </summary>
    /// <param name="wholeProgram"></param>
    /// <param name="namespacePrefix"></param>
    /// <returns></returns>
    internal static ReachabilitySummary COMSummary(WholeProgram wholeProgram, String namespacePrefix) {

      // The ToString() here is probably not the best idea.
      IEnumerable<ITypeDefinition> comInterfaces = wholeProgram.AllDefinedTypes().Where(type => type.IsComObject &&
                                                                                          type.IsInterface &&
                                                                                          type.ToString().StartsWith("Microsoft.Cci"));

      ReachabilitySummary summary = new ReachabilitySummary();

      foreach (ITypeDefinition comInterface in comInterfaces) {
        summary.ReachableTypes.Add(comInterface);

        foreach (ITypeDefinition subtype in wholeProgram.ClassHierarchy().AllSubClassesOfClass(comInterface)) {
          summary.ConstructedTypes.Add(subtype);

          foreach (IMethodDefinition method in subtype.Methods) {
            summary.NonvirtuallyCalledMethods.Add(method);
          }
        }
      }

      return summary;
    }
  }



  public interface IMethodSummarizer {
    ReachabilitySummary SummarizeMethod(IMethodDefinition methodDefinition, WholeProgram wholeProgram);
  }


}

