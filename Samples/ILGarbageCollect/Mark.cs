using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Cci;

namespace ILGarbageCollect.Mark {
  internal class AssemblyReport {
    readonly IAssembly assembly;

    // Perhaps a better design would
    // have the assembly report not know about CCI but instead just
    // have sets of string identifiers.

    readonly ISet<ITypeDefinition> reachableTypes;
    readonly ISet<ITypeDefinition> unreachableTypes;

    readonly ISet<IFieldDefinition> reachableFields;
    readonly ISet<IFieldDefinition> unreachableFields;

    readonly ISet<IMethodDefinition> reachableMethods;
    readonly ISet<IMethodDefinition> unreachableMethods;


    IAssembly Assembly { get { return assembly; } }

    internal ISet<ITypeDefinition> ReachableTypes { get { return reachableTypes; } }
    internal ISet<ITypeDefinition> UnreachableTypes { get { return unreachableTypes; } }

    internal ISet<IFieldDefinition> ReachableFields { get { return reachableFields; } }
    internal ISet<IFieldDefinition> UnreachableFields { get { return unreachableFields; } }

    internal ISet<IMethodDefinition> ReachableMethods { get { return reachableMethods; } }
    internal ISet<IMethodDefinition> UnreachableMethods { get { return unreachableMethods; } }



    static internal AssemblyReport CreateAssemblyReportFromRTA(IAssembly assembly, RapidTypeAnalysis rta) {
      AssemblyReport report = new AssemblyReport(assembly);

      int totalTypes = 0;
      int totalFields = 0;
      int totalMethods = 0;

      foreach (var t in assembly.GetAllTypes()) {

        totalTypes++;
        if (rta.ReachableTypes().Contains(t) == false) {
          report.UnreachableTypes.Add(t);
          //Console.WriteLine("Unused type {0}", t);
        }
        else {
          report.ReachableTypes.Add(t);
        }

        if (t.IsClass || t.IsStruct) {
          foreach (var f in t.Fields) {
            totalFields++;
            if (rta.ReachableFields().Contains(f) == false) {
              report.UnreachableFields.Add(f);
              //Console.WriteLine("Unused field {0}", f);
            }
            else {
              report.ReachableFields.Add(f);
            }
          }

          foreach (var m in t.Methods) {
            if (!m.IsAbstract) {
              totalMethods++;
              if (rta.MethodIsReachable(m) == false) {
                report.UnreachableMethods.Add(m);
                //Console.WriteLine("Unreachable method {0}", m);
              }
              else {
                report.ReachableMethods.Add(m);
              }
            }
          }
        }
      }

      return report;
    }

    const string ReachableTypesFileName = "ReachableTypes.txt";
    const string ReachableMethodsFileName = "ReachableMethods.txt";
    const string ReachableFieldsFileName = "ReachableFields.txt";

    const string UnusedTypesFileName = "UnusedTypes.txt";
    const string UnusedMethodsFileName = "UnusedMethods.txt";
    const string UnusedFieldsFileName = "UnusedFields.txt";

    internal void WriteReportToDirectory(string reportParentDirectory) {

      string assemblyReportingDirectory = GetAssemblyReportDirectory(assembly, reportParentDirectory);

      System.IO.Directory.CreateDirectory(assemblyReportingDirectory);

      string savedDirectory = Directory.GetCurrentDirectory();

      Directory.SetCurrentDirectory(assemblyReportingDirectory);


      using (StreamWriter outfile = new StreamWriter("TargetSummary.txt")) {
        outfile.WriteLine("UnusedTypesCount\t{0}", UnreachableTypes.Count);
        outfile.WriteLine("UnusedFieldsCount\t{0}", UnreachableFields.Count);
        outfile.WriteLine("UnusedMethodsCount\t{0}", UnreachableMethods.Count);

        outfile.WriteLine("TotalTypesCount\t{0}", UnreachableTypes.Count + ReachableTypes.Count);
        outfile.WriteLine("TotalFieldsCount\t{0}", UnreachableFields.Count + ReachableFields.Count);
        outfile.WriteLine("TotalMethodsCount\t{0}", UnreachableMethods.Count + ReachableMethods.Count);
      }

      // Output reachable types
      WriteDefinitionIdentifiersToFile(ReachableTypes, ReachableTypesFileName);
      WriteDefinitionIdentifiersToFile(ReachableMethods, ReachableMethodsFileName);
      WriteDefinitionIdentifiersToFile(ReachableFields, ReachableFieldsFileName);

      // Output unreachable types
      WriteDefinitionIdentifiersToFile(UnreachableTypes, UnusedTypesFileName);
      WriteDefinitionIdentifiersToFile(UnreachableMethods, UnusedMethodsFileName);
      WriteDefinitionIdentifiersToFile(UnreachableFields, UnusedFieldsFileName);


      Directory.SetCurrentDirectory(savedDirectory);
    }

    static private string GetAssemblyReportDirectory(IAssembly assembly, string allReportsDirectory) {
      string assemblyFileName = Path.GetFileName(assembly.Location);

      string assemblyReportDirectory = allReportsDirectory + @"\" + assemblyFileName + "-" + assembly.AssemblyIdentity.Version + ".report";

      return assemblyReportDirectory;
    }

    static internal AssemblyReport CreateAssemblyReportFromPath(IAssembly assembly, string pathToReportsDirectory, DocumentationCommentDefinitionIdStringMap idMap) {
      AssemblyReport report = new AssemblyReport(assembly);

      string assemblyReportDirectory = GetAssemblyReportDirectory(assembly, pathToReportsDirectory);

      report.reachableTypes.UnionWith(ReadDefinitionsFromIdentifierFile<ITypeDefinition>(assemblyReportDirectory + @"\" + ReachableTypesFileName, idMap));
      report.unreachableTypes.UnionWith(ReadDefinitionsFromIdentifierFile<ITypeDefinition>(assemblyReportDirectory + @"\" + UnusedTypesFileName, idMap));

      report.reachableMethods.UnionWith(ReadDefinitionsFromIdentifierFile<IMethodDefinition>(assemblyReportDirectory + @"\" + ReachableMethodsFileName, idMap));
      report.unreachableMethods.UnionWith(ReadDefinitionsFromIdentifierFile<IMethodDefinition>(assemblyReportDirectory + @"\" + UnusedMethodsFileName, idMap));

      report.reachableFields.UnionWith(ReadDefinitionsFromIdentifierFile<IFieldDefinition>(assemblyReportDirectory + @"\" + ReachableFieldsFileName, idMap));
      report.unreachableFields.UnionWith(ReadDefinitionsFromIdentifierFile<IFieldDefinition>(assemblyReportDirectory + @"\" + UnusedFieldsFileName, idMap));

      return report;
    }

    private void WriteDefinitionIdentifiersToFile(IEnumerable<IDefinition> definitions, string path) {
      using (StreamWriter outfile = new StreamWriter(path)) {
        foreach (var definition in definitions) {
          string definitionID = GarbageCollectHelper.GetIDStringForReference(definition);

          outfile.WriteLine(definitionID);
        }
      }
    }


    static private ISet<T> ReadDefinitionsFromIdentifierFile<T>(string path, DocumentationCommentDefinitionIdStringMap idMap) {
      ISet<T> result = new HashSet<T>();

      try {
        using (StreamReader infile = new StreamReader(path)) {
          string identifier;
          while (null != (identifier = infile.ReadLine())) {
            if (identifier != "") {

              IEnumerable<IDefinition> definitionsWithIdentifier = idMap.GetDefinitionsWithIdentifier(identifier);

              if (definitionsWithIdentifier.Count() == 1) {
                IDefinition definition = definitionsWithIdentifier.First();
                result.Add((T)definition);
              }
              else {
                throw new System.Exception("Found " + definitionsWithIdentifier.Count() + " definitions with identifier " + identifier);
              }
            }
          }
        }
      }
      catch (IOException) {
        Console.WriteLine("Couldn't read definitions from {0}", path);
        Environment.Exit(-1);
      }

      return result;
    }






    private AssemblyReport(IAssembly assembly) {
      this.assembly = assembly;

      this.reachableTypes = new HashSet<ITypeDefinition>(new TypeDefinitionEqualityComparer());
      this.unreachableTypes = new HashSet<ITypeDefinition>(new TypeDefinitionEqualityComparer());

      this.reachableFields = new HashSet<IFieldDefinition>(new FieldDefinitionEqualityComparer());
      this.unreachableFields = new HashSet<IFieldDefinition>(new FieldDefinitionEqualityComparer());

      this.reachableMethods = new HashSet<IMethodDefinition>(new MethodDefinitionEqualityComparer());
      this.unreachableMethods = new HashSet<IMethodDefinition>(new MethodDefinitionEqualityComparer());
    }



  }

  internal class DocumentationCommentDefinitionIdStringMap {

    private readonly IDictionary<string, ISet<IDefinition>> definitionsByStringId = new Dictionary<string, ISet<IDefinition>>();

    internal DocumentationCommentDefinitionIdStringMap(IEnumerable<IAssembly> assemblies) {
      foreach (IAssembly assembly in assemblies) {
        foreach (ITypeDefinition typeDefinition in assembly.GetAllTypes()) {


          AddIdStringForDefinition(GarbageCollectHelper.GetIDStringForReference(typeDefinition), typeDefinition);

          foreach (IMethodDefinition methodDefinition in typeDefinition.Methods) {
            AddIdStringForDefinition(GarbageCollectHelper.GetIDStringForReference(methodDefinition), methodDefinition);
          }

          foreach (IFieldDefinition fieldDefinition in typeDefinition.Fields) {
            AddIdStringForDefinition(GarbageCollectHelper.GetIDStringForReference(fieldDefinition), fieldDefinition);
          }
        }
      }
    }


    private void AddIdStringForDefinition(string idString, IDefinition definition) {
      Contract.Ensures(definitionsByStringId[idString].Contains(definition));

      if (!definitionsByStringId.ContainsKey(idString)) {
        definitionsByStringId[idString] = new HashSet<IDefinition>(new DefinitionEqualityComparer());
      }

      definitionsByStringId[idString].Add(definition);
    }



    internal ISet<IDefinition> GetDefinitionsWithIdentifier(string idString) {
      if (definitionsByStringId.ContainsKey(idString)) {
        return definitionsByStringId[idString];
      }
      else {
        throw new KeyNotFoundException("Couldn't find definition with string ID '" + idString + "'");
      }
    }


    internal IEnumerable<IMethodDefinition> GetMethodDefinitionsWithIdentifier(string idString) {
      ISet<IMethodDefinition> result = new HashSet<IMethodDefinition>();

      foreach (IDefinition definition in GetDefinitionsWithIdentifier(idString)) {
        if (definition is IMethodDefinition) {
          result.Add((IMethodDefinition)definition);
        }
        else {
          throw new KeyNotFoundException(idString + "' is not a method; it is a " + definition.GetType());
        }
      }

      return result;
    }

    internal IEnumerable<IMethodDefinition> CreateEntryPointListFromString(string entryPointsInString) {
      //entry points in string separated by whitespace

      foreach (string entryPointString in entryPointsInString.Split()) {
        if (entryPointString.Length > 0) {
          IEnumerable<IMethodDefinition> definitionsForEntryPoint = GetMethodDefinitionsWithIdentifier(entryPointString);

          if (definitionsForEntryPoint.Count() > 1) {
            Console.WriteLine("Warning: found multiple methods with ID string {0}. Treating both as possible entry points.", entryPointString);
          }

          foreach (IMethodDefinition entryPoint in definitionsForEntryPoint) {
            yield return entryPoint;
          }
        }
      }
    }
  }

  public class WholeProgram {
    ISet<IAssembly> rootAssemblies;

    ICollection<IAssembly> allAssemblies;

    MetadataReaderHost host;

    ClassHierarchy classHierarchy;


    public WholeProgram(IEnumerable<IAssembly> rootAssemblies, MetadataReaderHost host) {
      this.rootAssemblies = new HashSet<IAssembly>(rootAssemblies);

      this.allAssemblies = GarbageCollectHelper.CloseAndResolveOverReferencedAssemblies(rootAssemblies);

      this.host = host;
    }

    public MetadataReaderHost Host() {
      return host;
    }

    public IEnumerable<IAssembly> AllAssemblies() {
      return allAssemblies;
    }
    public IEnumerable<ITypeDefinition> AllDefinedTypes() {
      foreach (IAssembly assembly in allAssemblies) {
        foreach (ITypeDefinition typeDefinition in assembly.GetAllTypes()) {
          yield return typeDefinition;
        }
      }
    }


    public IEnumerable<IMethodDefinition> AllDefinedMethods() {
      foreach (IAssembly assembly in allAssemblies) {
        foreach (IMethodDefinition methodDefinition in GetAllMethodsInAssembly(assembly)) {
          yield return methodDefinition;
        }
      }
    }

    public IEnumerable<IFieldDefinition> AllDefinedFields() {
      foreach (IAssembly assembly in allAssemblies) {
        foreach (ITypeDefinition typeDefinition in assembly.GetAllTypes()) {
          foreach (IFieldDefinition fieldDefinition in typeDefinition.Fields) {
            yield return fieldDefinition;
          }
        }
      }
    }

    public ClassHierarchy ClassHierarchy() {
      if (this.classHierarchy == null) {
        classHierarchy = new ClassHierarchy(this.AllDefinedTypes(), host);
      }

      return classHierarchy;
    }

    public IEnumerable<IAssembly> RootAssemblies() {
      return rootAssemblies;
    }

    private IEnumerable<IMethodDefinition> GetAllMethodsInAssembly(IAssembly assembly) {
      foreach (ITypeDefinition typeDefinition in assembly.GetAllTypes()) {
        foreach (IMethodDefinition method in typeDefinition.Methods) {
          yield return method;
        }
      }
    }

    private IEnumerable<IFieldDefinition> GetAllFieldsInAssembly(IAssembly assembly) {
      foreach (ITypeDefinition typeDefinition in assembly.GetAllTypes()) {
        foreach (IFieldDefinition field in typeDefinition.Fields) {
          yield return field;
        }
      }
    }



    // Note: This returns the first type with the name found; different types with the same name
    // may be in different assemblies; that is, a fully qualified name is NOT UNIQUE for a whole
    // program.

    public INamedTypeDefinition FindTypeWithName(string fullyQualifiedName, int genericParameterCount) {
      foreach (IAssembly assembly in allAssemblies) {
        INamedTypeDefinition foundType = UnitHelper.FindType(host.NameTable, assembly, fullyQualifiedName, genericParameterCount);

        if (!(foundType is Dummy)) {
          return foundType;
        }
      }

      return null;
    }

    /*
     * Should eventually replace these Find* methods with something more efficient, to avoid making multiple passes over the entire program.
     * 
     */

    public ISet<IMethodDefinition> FindMethodsMatchingWholeProgramQuery(WholeProgramSearchQuery query) {
      return FindDefinitionsMatchingWholeProgramQuery<IMethodDefinition, MethodDefinitionEqualityComparer>(query);
    }

    public ISet<ITypeDefinition> FindTypesMatchingWholeProgramQuery(WholeProgramSearchQuery query) {
      return FindDefinitionsMatchingWholeProgramQuery<ITypeDefinition, TypeDefinitionEqualityComparer>(query);
    }

    public ISet<IFieldDefinition> FindFieldsMatchingWholeProgramQuery(WholeProgramSearchQuery query) {
      return FindDefinitionsMatchingWholeProgramQuery<IFieldDefinition, FieldDefinitionEqualityComparer>(query);
    }

    private WholeProgramDefinitionSearchResult SearchWholeProgram(WholeProgramSearchQuery query) {
      query.Validate();


      ISet<IAssembly> assembliesToSearch = new HashSet<IAssembly>();

      if (query.AssemblySpecifier != null) {
        string assemblyName = query.AssemblySpecifier;

        ISet<IAssembly> assemblies = FindAssembliesWithName(assemblyName);

        int foundAssemblyCount = assemblies.Count();

        if (foundAssemblyCount == 1) {
          assembliesToSearch.Add(assemblies.First());
        }
        else if (foundAssemblyCount == 0) {
          // Need to turn these exceptions into flags on the result so the client can deal with them appropriately

          throw new Exception("Couldn't find assembly with name " + assemblyName);
        }
        else {
          throw new Exception("Found " + foundAssemblyCount + " assemblies with name " + assemblyName + ". Unfortunately we don't support unification at this time.");
        }
      }
      else {
        assembliesToSearch.UnionWith(AllAssemblies());
      }

      ISet<IDefinition> foundDefinitions = new HashSet<IDefinition>(new DefinitionEqualityComparer());

      foreach (IAssembly assembly in allAssemblies) {

        if (query.PerformRegexpMatch) {

          Regex regexp = new Regex(query.DefinitionSpecifier);

          ISet<IDefinition> matchingDefinitions = FindDefinitionsMatchingRegularExpressionInAssembly(regexp, assembly);

          foundDefinitions.UnionWith(matchingDefinitions);
        }
        else {
          IDefinition foundDefinition = FindDefinitionWithIdentifierInAssembly(query.DefinitionSpecifier, assembly);

          if (foundDefinition != null) {
            foundDefinitions.Add(foundDefinition);
          }
        }
      }

      return new WholeProgramDefinitionSearchResult() { AssemblySpecificationAmbiguous = false, FoundDefinitions = foundDefinitions };
    }

    /*
     * A whole program identifier consists of an (optional) dll name and a doc comment identifier, separated by a '!'.
     * 
     * assemblyname!M:Foo.Bar.GetBlam(System.String)
     */
    private ISet<D> FindDefinitionsMatchingWholeProgramQuery<D, E>(WholeProgramSearchQuery query)
      where D : IDefinition
      where E : IEqualityComparer<D>, new() {
      WholeProgramDefinitionSearchResult queryResult = SearchWholeProgram(query);

      // WholeProgramDefinitionSearchResult works over fields, methods, and types
      // but clients usually only care about one of these, so we do
      // some casting for them. This may be too clever for its own good.

      HashSet<D> foundDefinitions = new HashSet<D>(new E());

      foundDefinitions.UnionWith(from d in queryResult.FoundDefinitions select (D)d);

      return foundDefinitions;
    }

    // Note: assembly names may not be unique with a whole program.
    //
    // Should get rid of this method.
    public IMethodDefinition FindMethodWithIdentifierInAssemblyWithName(string docCommentMethodIdentifier, string assemblyName) {
      IAssembly assembly = FindUniqueAssemblyWithName(assemblyName);

      return FindMethodWithIdentifierInAssembly(docCommentMethodIdentifier, assembly);
    }

    public IMethodDefinition FindMethodWithIdentifierInAssembly(string docCommentMethodIdentifier, IAssembly assembly) {


      // We could be more efficient here, but for now we go with simplicity
      foreach (IMethodDefinition method in GetAllMethodsInAssembly(assembly)) {
        if (GarbageCollectHelper.GetIDStringForReference(method) == docCommentMethodIdentifier) {
          return method;
        }
      }

      return null;
    }

    // The name should not have an extension
    public ISet<IAssembly> FindAssembliesWithName(string name) {
      ISet<IAssembly> foundAssemblies = new HashSet<IAssembly>();

      foreach (IAssembly assembly in AllAssemblies()) {
        if (assembly.Name.Value == name) {
          foundAssemblies.Add(assembly);
        }
      }

      return foundAssemblies;
    }

    public IAssembly FindUniqueAssemblyWithName(string name) {
      ISet<IAssembly> allAssembliesWithName = FindAssembliesWithName(name);

      Contract.Assert(allAssembliesWithName.Count <= 1);

      if (allAssembliesWithName.Count == 1) {
        return allAssembliesWithName.First();
      }
      else {
        return null;
      }
    }

    public ISet<IDefinition> FindAllDefinitionsWithDocCommentIdentifier(string identifier) {
      ISet<IDefinition> foundDefinitions = new HashSet<IDefinition>(new DefinitionEqualityComparer());

      foreach (IAssembly assembly in allAssemblies) {
        IDefinition foundDefinition = FindDefinitionWithIdentifierInAssembly(identifier, assembly);

        if (foundDefinition != null) {
          foundDefinitions.Add(foundDefinition);
        }
      }

      return foundDefinitions;
    }

    public IDefinition FindDefinitionWithIdentifierInAssembly(string docCommentIdentifier, IAssembly assembly) {


      // We could be more efficient here, but for now we go with simplicity

      if (docCommentIdentifier.StartsWith("M:")) {
        foreach (IMethodDefinition method in GetAllMethodsInAssembly(assembly)) {
          if (GarbageCollectHelper.GetIDStringForReference(method) == docCommentIdentifier) {
            return method;
          }
        }
      }
      else if (docCommentIdentifier.StartsWith("T:")) {
        foreach (ITypeDefinition type in assembly.GetAllTypes()) {
          if (GarbageCollectHelper.GetIDStringForReference(type) == docCommentIdentifier) {
            return type;
          }
        }
      }
      else if (docCommentIdentifier.StartsWith("F:")) {
        foreach (IFieldDefinition field in GetAllFieldsInAssembly(assembly)) {
          if (GarbageCollectHelper.GetIDStringForReference(field) == docCommentIdentifier) {
            return field;
          }
        }
      }
      else {
        throw new Exception("Un recognized doc comment definition identifier prefix in: " + docCommentIdentifier + " (expected T:, M:, or F:)");
      }


      return null;
    }

    public ISet<IDefinition> FindDefinitionsMatchingRegularExpressionInAssembly(Regex regex, IAssembly assembly) {

      // Maybe figure out how to factor out commons parts of this with FindDefinitionWithIdentifierInAssembly?

      ISet<IDefinition> results = new HashSet<IDefinition>(new DefinitionEqualityComparer());

      string regexPattern = regex.ToString();

      // We could be more efficient here, but for now we go with simplicity

      if (regexPattern.StartsWith("M:")) {
        foreach (IMethodDefinition method in GetAllMethodsInAssembly(assembly)) {
          if (regex.IsMatch(GarbageCollectHelper.GetIDStringForReference(method))) {
            results.Add(method);
          }
        }
      }
      else if (regexPattern.StartsWith("T:")) {
        foreach (ITypeDefinition type in assembly.GetAllTypes()) {
          if (regex.IsMatch(GarbageCollectHelper.GetIDStringForReference(type))) {
            results.Add(type);
          }
        }
      }
      else if (regexPattern.StartsWith("F:")) {
        foreach (IFieldDefinition field in GetAllFieldsInAssembly(assembly)) {
          if (regex.IsMatch(GarbageCollectHelper.GetIDStringForReference(field))) {
            results.Add(field);
          }
        }
      }
      else {
        throw new Exception("Un recognized doc comment definition identifier prefix in: " + regexPattern + " (expected T:, M:, or F:)");
      }


      return results;
    }

    public IAssembly HeuristicFindCoreAssemblyForProfile(TargetProfile profile) {

      IAssembly coreAssembly = null;

      switch (profile) {
        case TargetProfile.Desktop:
        case TargetProfile.Phone:
          foreach (IAssembly assembly in allAssemblies) {
            if (assembly.AssemblyIdentity.Equals(host.CoreAssemblySymbolicIdentity)) {
              coreAssembly = assembly;
              break;
            }
          }
          break;
      }

      return coreAssembly;
    }
  }

  /// <summary>
  /// Maps a class/interface to its direct subclasses.
  /// This only makes sense if we have the whole program available.
  /// 
  /// </summary>
  public class ClassHierarchy {
    private IDictionary<ITypeDefinition, ISet<ITypeDefinition>> subclassSetsBySuperClass = new Dictionary<ITypeDefinition, ISet<ITypeDefinition>>(new TypeDefinitionEqualityComparer());

    private IMetadataReaderHost host;

    internal ClassHierarchy(IEnumerable<ITypeDefinition> allTypes, IMetadataReaderHost host) {
      this.host = host;

      foreach (ITypeDefinition typeDefinition in allTypes) {

        Contract.Assert(GarbageCollectHelper.TypeDefinitionIsUnspecialized(typeDefinition));

        foreach (ITypeDefinition superType in GarbageCollectHelper.BaseClasses(typeDefinition)) {
          NoteClassIsSubClassOfClass(typeDefinition, GarbageCollectHelper.UnspecializeAndResolveTypeReference(superType));
        }

        // We treat every interface type as a subtype of System.Object
        if (typeDefinition.IsInterface) {
          //NoteClassIsSubClassOfClass(instantiatedSubType, host.PlatformType.SystemObject.ResolvedType);
          NoteClassIsSubClassOfClass(typeDefinition, host.PlatformType.SystemObject.ResolvedType);
        }
      }
    }

    private void NoteClassIsSubClassOfClass(ITypeDefinition subClass, ITypeDefinition superClass) {
      Contract.Requires(subClass != null);
      Contract.Requires(superClass != null);


      Contract.Ensures(subclassSetsBySuperClass.ContainsKey(superClass));
      Contract.Ensures(subclassSetsBySuperClass[superClass].Contains(subClass));

      if (!subclassSetsBySuperClass.ContainsKey(superClass)) {
        subclassSetsBySuperClass[superClass] = new HashSet<ITypeDefinition>(new TypeDefinitionEqualityComparer());
      }

      subclassSetsBySuperClass[superClass].Add(subClass);
    }


    public IEnumerable<ITypeDefinition> DirectSubClassesOfClass(ITypeDefinition typeDefinition) {
      Contract.Requires(typeDefinition != null);
      Contract.Ensures(Contract.Result<IEnumerable<ITypeDefinition>>() != null);
      /*Contract.Ensures(Contract.ForAll<ITypeDefinition>(Contract.Result<IEnumerable<ITypeDefinition>>(), t =>
        typeDefinition == host.PlatformType.SystemObject.ResolvedType || GarbageCollectHelper.BaseClasses(t).Contains(typeDefinition)));*/

      if (subclassSetsBySuperClass.ContainsKey(typeDefinition)) {
        return subclassSetsBySuperClass[typeDefinition];
      }
      else {
        return Enumerable.Empty<ITypeDefinition>();
      }
    }

    public IEnumerable<ITypeDefinition> AllSubClassesOfClass(ITypeDefinition typeDefinition) {
      // Result does not include the class itself

      ISet<ITypeDefinition> allSubClasses = new HashSet<ITypeDefinition>();

      CollectAllSubClassesOfClass(typeDefinition, allSubClasses);

      return allSubClasses;
    }

    private void CollectAllSubClassesOfClass(ITypeDefinition typeDefinition, ISet<ITypeDefinition> collectedSubClasses) {
      // t-devinc: Might consider generalizing to have a visitor for sub classes

      foreach (ITypeDefinition directSubClass in DirectSubClassesOfClass(typeDefinition)) {
        collectedSubClasses.Add(directSubClass);
        CollectAllSubClassesOfClass(directSubClass, collectedSubClasses);
      }
    }
  }

  internal interface IEntryPointDetector {
    ISet<IMethodReference> GetEntryPoints(WholeProgram wholeProgram);
  }


  // Returns the designated entry points of the
  // designated root assemblies
  internal class RootAssembliesEntryPointDetector : IEntryPointDetector {
    public ISet<IMethodReference> GetEntryPoints(WholeProgram wholeProgram) {
      // The set of entry points is the designated entry points
      // for each of the root assemblies.
      ISet<IMethodReference> rootsEntryPoints = new HashSet<IMethodReference>();

      foreach (IAssembly rootAssembly in wholeProgram.RootAssemblies()) {
        IMethodReference assemblyEntryPoint = rootAssembly.EntryPoint;
        if (!(assemblyEntryPoint is Dummy)) {
          rootsEntryPoints.Add(assemblyEntryPoint);
        }
      }

      return rootsEntryPoints;
    }
  }

  // Returns the designated entry points of the
  // designated root assemblies
  internal class DocCommentFileEntryPointDetector : IEntryPointDetector {
    string pathToFile;

    internal DocCommentFileEntryPointDetector(string pathToFile) {
      this.pathToFile = pathToFile;
    }

    public ISet<IMethodReference> GetEntryPoints(WholeProgram wholeProgram) {
      // Read entry points from provided file
      string entrypointsFileContents = System.IO.File.ReadAllText(pathToFile);

      // Creating the map is expensive; perhaps should cache in wholeProgram?

      DocumentationCommentDefinitionIdStringMap idMap = new DocumentationCommentDefinitionIdStringMap(wholeProgram.AllAssemblies());
      return new HashSet<IMethodReference>(idMap.CreateEntryPointListFromString(entrypointsFileContents));
    }
  }

  internal class AttributeFileEntryPointDetector : IEntryPointDetector {
    string pathToFile;

    // Any method 
    internal AttributeFileEntryPointDetector(string pathToFile) {
      this.pathToFile = pathToFile;


    }

    public ISet<IMethodReference> GetEntryPoints(WholeProgram wholeProgram) {
      ISet<string> entryPointAttributeIDs = new HashSet<string>(System.IO.File.ReadAllLines(pathToFile));

      ISet<IMethodReference> foundEntryPoints = new HashSet<IMethodReference>();
      ISet<string> foundEntryPointIDs = new HashSet<string>();

      foreach (IMethodDefinition methodDefinition in wholeProgram.AllDefinedMethods()) {
        foreach (ICustomAttribute attribute in methodDefinition.Attributes) {
          string attributeDefinitionID = GarbageCollectHelper.GetIDStringForReference(attribute.Type);

          if (entryPointAttributeIDs.Contains(attributeDefinitionID)) {
            foundEntryPoints.Add(methodDefinition);
            foundEntryPointIDs.Add(attributeDefinitionID);
          }
        }
      }

      // A poor attempt at slightly more humane error-reporting
      foreach (string desiredEntryPointID in entryPointAttributeIDs) {
        if (!foundEntryPointIDs.Contains(desiredEntryPointID)) {
          Console.WriteLine("Couldn't find any entry points with attribute {0}", desiredEntryPointID);
          Environment.Exit(-1);
        }
      }


      return foundEntryPoints;
    }
  }

  public class WholeProgramSearchQuery {

    public WholeProgramSearchQuery() {
      PerformRegexpMatch = false;
    }

    // For now this is an (optional) assembly name
    // In the future we'll want to support (optional) version, culture, etc.
    public string AssemblySpecifier { get; set; }


    // A doc-comment style definition specifier
    // This must start with M: or T: or F:
    //
    // In the (near) future, we will extend this to allow regular expressions
    // for the parts beyond the colon.

    public string DefinitionSpecifier { get; set; }


    // Make sure the query is in an acceptable format.
    // For now we throw an exception if it fails, in the future we 
    // should do something more humane.

    public bool PerformRegexpMatch { get; set; }

    public void Validate() {
      if (DefinitionSpecifier != null) {
        if (DefinitionSpecifier.StartsWith("M:") || DefinitionSpecifier.StartsWith("T:") || DefinitionSpecifier.StartsWith("F:")) {
          // Everything is fine
          return;
        }
      }

      throw new Exception("Unacceptable WholeProgramSearchQuery DefinitionSpecifier: '" + DefinitionSpecifier + "'");
    }
  }

  public class WholeProgramDefinitionSearchResult {

    // For now this means an assembly name was specified and
    // two assemblies had that name.

    public bool AssemblySpecificationAmbiguous { get; internal set; }

    public ISet<IDefinition> FoundDefinitions { get; internal set; }
  }


  internal enum TypeVariableCreateInstanceStrategy {
    Ignore,
    ConstructAll,
    ConstructAllConcreteParameters,
    ConstructFlowingParametes
  }

  internal class TypeVariableGraph {
    // For now we only support class type variables

    //IDictionary<IGenericTypeParameter, ISet<IGenericTypeParameter>> outEdgesByVariable;

    //IDictionary<IGenericTypeParameter, ISet<ITypeDefinition>> reachingUnspecializedTypesByVariable;

  }
}
