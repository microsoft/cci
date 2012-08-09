using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Cci;
using ILGarbageCollect.Mark;
using System.Diagnostics.Contracts;

/* Proposed summaries format.
    * Tabs are for legibility only; they don't matter.
    * 
    * summarize foo!M:Foo.Bar(System.String,System.Object)
    *    construct App!T:Blam
    *    construct subtypes bar!T:Baz
    *    
    *    
    *    construct attributes App!F:Blam.field2
    *    
    * # this is a comment
    * 
    *    call App!M:Blam.Stuff
    *    call virtual App!M:Blam.Morestuff(Blam)
    *    call anypublic App!T:Blam
    *    
    *    read App!F:Blam.field1
    *    write App!F:Blam.field2
    * 
    *    
    */

namespace ILGarbageCollect.Summaries {
  public class TextFileMethodSummarizer : IMethodSummarizer {

    IDictionary<IMethodDefinition, ReachabilitySummary> summariesByMethod = new Dictionary<IMethodDefinition, ReachabilitySummary>(new MethodDefinitionEqualityComparer());


    enum SummarizeOperation {
      Summarize,
      
      Construct,

      ConstructAttributes,

      Call,
      CallVirtual,

      CallAnyPublic,
      CallAny,

      ReadField,
      WriteField
    }

    struct SummarizeCommand {
      public SummarizeOperation Operation { get; set; }
      public object Argument { get; set; }
    }


    struct TypeSpecifier {
      public TypeSpecifierKind Kind { get; set; }

      public string TypeIdentifier { get; set; }
    }

    enum TypeSpecifierKind {
      Exactly,
      Matches,
      Subtypes
    }

    private TextFileMethodSummarizer() {
     

    }

  

    public static TextFileMethodSummarizer CreateSummarizerFromPath(string path, WholeProgram wholeProgram) {
      TextFileMethodSummarizer summarizer = new TextFileMethodSummarizer();

      ReachabilitySummary summaryForCurrentMethod = null;

      foreach (string line in System.IO.File.ReadAllLines(path)) {

        string trimmedLine = line.Trim();

        if (trimmedLine == "" || trimmedLine.StartsWith("#")) {
          continue;
        }

        SummarizeCommand command = ParseCommand(trimmedLine);

        switch (command.Operation) {
          case SummarizeOperation.Summarize:
            summaryForCurrentMethod = new ReachabilitySummary(); 
            summarizer.summariesByMethod[LookupMethodWithIdentifier((string)command.Argument, wholeProgram)] = summaryForCurrentMethod;
            
            break;

          case SummarizeOperation.Construct:
            InterpretConstruct(summaryForCurrentMethod, (TypeSpecifier)command.Argument, wholeProgram);
            break;

          case SummarizeOperation.ConstructAttributes:
            InterpretConstructAttributes(summaryForCurrentMethod, (string)command.Argument, wholeProgram);
            break;

          case SummarizeOperation.Call:
            InterpretCall(summaryForCurrentMethod, (string)command.Argument, wholeProgram);
            break;

          case SummarizeOperation.CallVirtual:
            InterpretCallVirtual(summaryForCurrentMethod, (string)command.Argument, wholeProgram);
            break;

            // We should really introduce a notion of a method specifier analogous to TypeSpecifier
            // that specifies a method, rather than have these separate operations for different ways to
            // specify methods

          case SummarizeOperation.CallAnyPublic:
            InterpretCallAnyPublic(summaryForCurrentMethod, (TypeSpecifier)command.Argument, wholeProgram);
            break;

          case SummarizeOperation.CallAny:
            InterpretCallAny(summaryForCurrentMethod, (TypeSpecifier)command.Argument, wholeProgram);
            break;

          case SummarizeOperation.ReadField:
            InterpretReadField(summaryForCurrentMethod, (string)command.Argument, wholeProgram);
            break;

          case SummarizeOperation.WriteField:
            InterpretWriteField(summaryForCurrentMethod, (string)command.Argument, wholeProgram);
            break;

          default:
            throw new Exception("Unhandled summarize command: " + command.Operation + " " + command.Argument);
        }
      }

      return summarizer;
    }


    private static void InterpretConstruct(ReachabilitySummary summary, TypeSpecifier typeSpecifier, WholeProgram wholeProgram) {
      if (summary != null) {

        foreach (ITypeDefinition typeToConstruct in LookupTypesWithSpecifier(typeSpecifier, wholeProgram)) {
          if (GarbageCollectHelper.TypeIsConstructable(typeToConstruct)) {
            summary.ConstructedTypes.Add(typeToConstruct);

            // now we mark ALL the non-private constructors as reachable. This is perhaps too imprecise
            // an alternative would be to allow the user to specify the constructor signature.
            MarkNonPrivateConstructorsReachableForType(summary, typeToConstruct);
          }
        }       
      }
      else {
        throw new Exception("Cannot construct type outside of a summarized method.");
      }
    }

    private static void InterpretConstructAttributes(ReachabilitySummary summary, string fieldIdentifier, WholeProgram wholeProgram) {
      if (summary != null) {
        // For now we assume the argument is a field -- we really should support types and methods too
        IFieldDefinition fieldWithAttributes = LookupFieldWithIdentifier(fieldIdentifier, wholeProgram);


        foreach (ICustomAttribute customAttribute in fieldWithAttributes.Attributes) {
          IMethodDefinition constructorDefinition = GarbageCollectHelper.UnspecializeAndResolveMethodReference(customAttribute.Constructor);

          ITypeDefinition constructorType = constructorDefinition.ContainingTypeDefinition;

          // Mark attribute constructor reachable
          summary.NonvirtuallyCalledMethods.Add(constructorDefinition);


          // Mark named argument property setters reachable
          foreach (IMetadataNamedArgument namedArgument in customAttribute.NamedArguments) {
            IName setterName = wholeProgram.Host().NameTable.GetNameFor("set_" + namedArgument.ArgumentName.Value);

            IMethodDefinition setterMethod = TypeHelper.GetMethod(constructorType, setterName, namedArgument.ArgumentValue.Type);

            if (!(setterMethod is Dummy)) {
              // We treat this as a non-virtual call because we know the exact runtime-type of the attribute
              summary.NonvirtuallyCalledMethods.Add(setterMethod);
            }
            else {
              // Note this won't find a property defined in a super class of the attribute (unsound).
              // We'll want to fix this if try to generalize this code to handle arbitrary attributes

              throw new Exception("Couldn't find setter " + setterName + " for type " + namedArgument.ArgumentValue.Type + " in " + constructorType);
            }
          }        
        }
      }
      else {
        throw new Exception("Cannot construct subtypes outside of a summarized method.");
      }
    }

    private static void InterpretCall(ReachabilitySummary summary, string methodIdentifier, WholeProgram wholeProgram) {
      if (summary != null) {
        IMethodDefinition method = LookupMethodWithIdentifier(methodIdentifier, wholeProgram);
        summary.NonvirtuallyCalledMethods.Add(method);
      }
      else {
        throw new Exception("Cannot use 'call' outside of a summarized method.");
      }
    }

    private static void InterpretCallVirtual(ReachabilitySummary summary, string methodIdentifier, WholeProgram wholeProgram) {
      if (summary != null) {
        IMethodDefinition method = LookupMethodWithIdentifier(methodIdentifier, wholeProgram);
        summary.VirtuallyCalledMethods.Add(method);
      }
      else {
        throw new Exception("Cannot call virtual  outside of a summarized method.");
      }
    }

    private static void InterpretCallAnyPublic(ReachabilitySummary summary, TypeSpecifier typeSpecifier, WholeProgram wholeProgram) {
      if (summary != null) {
        foreach (ITypeDefinition type in LookupTypesWithSpecifier(typeSpecifier, wholeProgram)) {
          // Note, for now this only looks at methods directly defined on that type
          // not on any methods defined on super types (and inherited).
          // This is probably not what we really want to expose to the user.

          foreach (IMethodDefinition method in type.Methods) {
            if (method.Visibility == TypeMemberVisibility.Public && !method.IsAbstract) {
              summary.NonvirtuallyCalledMethods.Add(method);

              // If there is a public constructor, we treat the type as constructed
              if (method.IsConstructor && GarbageCollectHelper.TypeIsConstructable(type)) {
                summary.ConstructedTypes.Add(type);
              }
            }
          }
        }
      }
      else {
        throw new Exception("Cannot call anypublic outside of a summarized method.");
      }
    }

    private static void InterpretCallAny(ReachabilitySummary summary, TypeSpecifier typeSpecifier, WholeProgram wholeProgram) {
      if (summary != null) {
        foreach (ITypeDefinition type in LookupTypesWithSpecifier(typeSpecifier, wholeProgram)) {
          // Note, for now this only looks at methods directly defined on that type
          // not on any methods defined on super types (and inherited).
          // This is probably not what we really want to expose to the user.
          foreach (IMethodDefinition method in type.Methods) {
            if (!method.IsAbstract) {
              summary.NonvirtuallyCalledMethods.Add(method);

              // If there is a constructor, we treat the type as constructed
              if (method.IsConstructor && GarbageCollectHelper.TypeIsConstructable(type)) {
                summary.ConstructedTypes.Add(type);
              }
            }
          }
        }
      }
      else {
        throw new Exception("Cannot call any outside of a summarized method.");
      }
    }

    private static void InterpretReadField(ReachabilitySummary summary, string methodIdentifier, WholeProgram wholeProgram) {
      if (summary != null) {
        IFieldDefinition field = LookupFieldWithIdentifier(methodIdentifier, wholeProgram);
        summary.ReachableFields.Add(field);
      }
      else {
        throw new Exception("Cannot use 'read' field outside of a summarized method.");
      }
    }

    private static void InterpretWriteField(ReachabilitySummary summary, string methodIdentifier, WholeProgram wholeProgram) {
      if (summary != null) {
        IFieldDefinition field = LookupFieldWithIdentifier(methodIdentifier, wholeProgram);
        summary.ReachableFields.Add(field);
      }
      else {
        throw new Exception("Cannot use 'write' field outside of a summarized method.");
      }
    }

    private static IMethodDefinition LookupMethodWithIdentifier(string identifier, WholeProgram wholeProgram) {

      WholeProgramSearchQuery query = CreateQueryForIdentifier(identifier);

      ISet<IMethodDefinition> methods = wholeProgram.FindMethodsMatchingWholeProgramQuery(query);

      if (methods.Count() == 1) {
        return methods.First();
      }
      else if (methods.Count() > 1) {
        throw new Exception("Couldn't find unique method with identifier " + identifier + " (found " + methods.Count() + ")");
      }
      else {
        throw new Exception("Couldn't find method with identifier: " + identifier);
      }
    }

    private static ITypeDefinition LookupExactTypeWithIdentifier(string identifier, WholeProgram wholeProgram) {
      WholeProgramSearchQuery query = CreateQueryForIdentifier(identifier);

      ISet<ITypeDefinition> types = LookupTypesMatchingQuery(query, wholeProgram);

      if (types.Count() == 1) {
        return types.First();
      }
      else {
        throw new Exception("Couldn't find unique type with identifier: " + identifier + " (found " + types.Count() + ")");
      }
    }

    private static ISet<ITypeDefinition> LookupTypesMatchingRegexpIdentifier(string identifier, WholeProgram wholeProgram) {
      WholeProgramSearchQuery query = CreateQueryForIdentifier(identifier);
      query.PerformRegexpMatch = true;

      return LookupTypesMatchingQuery(query, wholeProgram);
    }

    private static ISet<ITypeDefinition> LookupTypesMatchingQuery(WholeProgramSearchQuery query, WholeProgram wholeProgram) {
      ISet<ITypeDefinition> types = wholeProgram.FindTypesMatchingWholeProgramQuery(query);

      if (types.Count() > 0) {
        return types;
      }
      else {
        // Really should have function that turns query into a human readable string
        throw new Exception("Couldn't find any types matching query: " + query.AssemblySpecifier + "!" + query.DefinitionSpecifier);
      }
    }

    private static WholeProgramSearchQuery CreateQueryForIdentifier(string identifier) {
      WholeProgramSearchQuery query;

      // assemblyname!M:Foo.Bar.GetBlam(System.String)

      string[] components = identifier.Split('!');

      if (components.Count() == 2) {
        query = new WholeProgramSearchQuery() { AssemblySpecifier = components[0], DefinitionSpecifier = components[1] };
      }
      else if (components.Count() == 1) {
        query = new WholeProgramSearchQuery() { DefinitionSpecifier = components[0] };
      }
      else {
        throw new Exception("Malformed type identifier: " + identifier);
      }

      return query;
    }

    private static ISet<ITypeDefinition> LookupTypesWithIdentifier(string identifier, WholeProgram wholeProgram) {

      WholeProgramSearchQuery query = CreateQueryForIdentifier(identifier);

      ISet<ITypeDefinition> types = wholeProgram.FindTypesMatchingWholeProgramQuery(query);

      if (types.Count() > 0) {
        return types;
      }
      else {
        throw new Exception("Couldn't find type with identifier: " + identifier);
      }
    }

    private static ISet<ITypeDefinition> LookupTypesWithSpecifier(TypeSpecifier specifier, WholeProgram wholeProgram ) {
      HashSet<ITypeDefinition> result = new HashSet<ITypeDefinition>(new TypeDefinitionEqualityComparer());

      string typeIdentifier = specifier.TypeIdentifier;


      switch (specifier.Kind) {
        case TypeSpecifierKind.Exactly:
          result.Add(LookupExactTypeWithIdentifier(typeIdentifier, wholeProgram));
          break;
        case TypeSpecifierKind.Subtypes:
          // t-devinc: We really out to change this to include the type itself, not just all of its proper subtypes
          result.UnionWith(wholeProgram.ClassHierarchy().AllSubClassesOfClass(LookupExactTypeWithIdentifier(typeIdentifier, wholeProgram)));
          break;
        case TypeSpecifierKind.Matches:
          return LookupTypesMatchingRegexpIdentifier(typeIdentifier, wholeProgram);
      }

      return result;
    }

    private static IFieldDefinition LookupFieldWithIdentifier(string identifier, WholeProgram wholeProgram) {
      WholeProgramSearchQuery query = CreateQueryForIdentifier(identifier);

      ISet<IFieldDefinition> fields = wholeProgram.FindFieldsMatchingWholeProgramQuery(query);

      if (fields.Count() == 1) {
        return fields.First();
      }
      else if (fields.Count() > 1) {
        throw new Exception("Couldn't find unique field with identifier " + identifier + " (found " + fields.Count() + ")");
      }
      else {
        throw new Exception("Couldn't find fields with identifier: " + identifier);
      }
    }

    private static void MarkNonPrivateConstructorsReachableForType(ReachabilitySummary summary, ITypeDefinition type) {
      foreach (IMethodDefinition method in type.Methods) {
        if (method.IsConstructor && (method.Visibility != TypeMemberVisibility.Private)) {
          summary.NonvirtuallyCalledMethods.Add(method);
        }
      }
    }

   
    static SummarizeCommand ParseCommand(string line) {
      Contract.Requires(line.Trim() == line);

      TokenList tokens = new TokenList(
        from c in line.Split()
        where c != ""
        select c
      );

      if (tokens.Count() >= 2 ) {
        SummarizeOperation operation = ConsumeOperation(tokens);

        object argument;

        switch (operation) {
          case SummarizeOperation.Construct:
          case SummarizeOperation.CallAny:
          case SummarizeOperation.CallAnyPublic:
            argument = ConsumeTypeSpecifier(tokens);
            break;
          default:
            argument = argument = tokens.ConsumeToken(); // string argument
            break;
        }   

        if (tokens.Count == 0) {
          return new SummarizeCommand() { Operation = operation, Argument = argument };
        }
      }

      throw new Exception("Couldn't parse command: '" + line + "'");
    }

    static SummarizeOperation ConsumeOperation(TokenList tokens) {
      string firstToken = tokens.ConsumeToken().ToLower();
     

      SummarizeOperation operation;

      switch (firstToken) {
        case "summarize":
          operation = SummarizeOperation.Summarize;
          break;

        case "construct":       
          if (tokens.PeekToken().ToLower() == "attributes") {
            tokens.RemoveFirst();
            operation = SummarizeOperation.ConstructAttributes;
          } else {
            operation = SummarizeOperation.Construct;
          }
          break;

        case "call":
          if (tokens.PeekToken().ToLower() == "virtual") {
            tokens.RemoveFirst();
            operation = SummarizeOperation.CallVirtual;
          }
          else if (tokens.PeekToken().ToLower() == "anypublic") {
            tokens.RemoveFirst();
            operation = SummarizeOperation.CallAnyPublic;
          }
          else if (tokens.PeekToken().ToLower() == "any") {
            tokens.RemoveFirst();
            operation = SummarizeOperation.CallAny;
          }
          else {
            operation = SummarizeOperation.Call;
          }
          break;

        case "read":
          operation = SummarizeOperation.ReadField;
          break;

        case "write":
          operation = SummarizeOperation.WriteField;
          break;
        default:
          throw new Exception("Unrecognized summarize operation: '" + firstToken + "'");
      }

      return operation;
    }


    static TypeSpecifier ConsumeTypeSpecifier(TokenList tokens) {
      string firstToken = tokens.ConsumeToken();
      string firstTokenLower = firstToken.ToLower();

      if (firstTokenLower == "subtypes") {
        return new TypeSpecifier() { Kind = TypeSpecifierKind.Subtypes, TypeIdentifier = tokens.ConsumeToken() };
      }
      else if (firstTokenLower == "matches") {
        return new TypeSpecifier() { Kind = TypeSpecifierKind.Matches, TypeIdentifier = tokens.ConsumeToken() };
      }
      else {
        return new TypeSpecifier() {Kind = TypeSpecifierKind.Exactly, TypeIdentifier = firstToken};
      }
    }

    public ReachabilitySummary SummarizeMethod(IMethodDefinition methodDefinition, WholeProgram wholeProgram) {
      ReachabilitySummary summary = null;

      summariesByMethod.TryGetValue(methodDefinition, out summary);

      return summary;
    }
  }

  internal class TokenList : LinkedList<string> {

    internal TokenList(IEnumerable<string> tokens) : base(tokens) {    
    }

    internal string PeekToken() {
      return this.First();
    }

    internal string ConsumeToken() {
      string firstToken = this.First();
      this.RemoveFirst();

      return firstToken;
    }
  }
}
