//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using Microsoft.Cci.Ast;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci.SpecSharp {

  internal sealed class DummyUnit : Unit {

    internal DummyUnit(ICompilation compilation, IMetadataHost compilationHost)
      : base(compilation.Result.Name, compilation.Result.Location) {
      this.compilation = compilation;
      this.compilationHost = compilationHost;
    }

    public override Compilation Compilation {
      get { return new DummySpecSharpCompilation(this.compilation, this.compilationHost); }
    }
    readonly ICompilation compilation;

    readonly IMetadataHost compilationHost;

    public override void Dispatch(IMetadataVisitor visitor) {
    }

    public override IRootUnitNamespace UnitNamespaceRoot {
      get { return this.Compilation.Result.UnitNamespaceRoot; }
    }

    public override IEnumerable<IUnitReference> UnitReferences {
      get { return this.Compilation.Result.UnitReferences; }
    }

    public override UnitIdentity UnitIdentity {
      get {
        return new ModuleIdentity(Dummy.Name, string.Empty);
      }
    }
  }

  internal sealed class EntryPointFinder : BaseMetadataTraverser {

    internal EntryPointFinder(ICompilation compilation) {
      this.compilation = compilation;
      this.entryPoints = new List<IMethodDefinition>();
    }

    ICompilation compilation;
    internal readonly List<IMethodDefinition> entryPoints;

    public override void Visit(IMethodDefinition method) {
      if (method.Name.Value == "Main" && method.IsStatic &&
        (TypeHelper.TypesAreEquivalent(method.Type.ResolvedType, this.compilation.PlatformType.SystemInt32) || 
        TypeHelper.TypesAreEquivalent(method.Type.ResolvedType, this.compilation.PlatformType.SystemVoid)) &&
        ParametersAreOkForMainMethod(method.Parameters)) {
        this.entryPoints.Add(method);
      }
    }

    private bool ParametersAreOkForMainMethod(IEnumerable<IParameterDefinition> parameters) {
      bool ok = true;
      int count = 0;
      foreach (IParameterDefinition parameter in parameters) {
        if (count++ > 0) { ok = false; break; }
        IArrayType/*?*/ ptype = parameter.Type.ResolvedType as IArrayType;
        ok = ptype != null && ptype.IsVector && TypeHelper.TypesAreEquivalent(ptype.ElementType, ptype.PlatformType.SystemString);
      }
      return ok;
    }

  }

  public sealed class SpecSharpAssembly : Assembly {

    public SpecSharpAssembly(IName name, string location, ISourceEditHost hostEnvironment, SpecSharpOptions options,
      IEnumerable<IAssemblyReference> assemblyReferences, IEnumerable<IModuleReference> moduleReferences, IEnumerable<SpecSharpSourceDocument> programSources)
      : base(name, location, name, assemblyReferences, moduleReferences, new List<IResourceReference>(0).AsReadOnly(), new List<IFileReference>(0).AsReadOnly()) {
      this.options = options;
      this.hostEnvironment = hostEnvironment;
      this.programSources = programSources;
    }

    internal SpecSharpAssembly(IName name, string location, ISourceEditHost hostEnvironment, SpecSharpOptions options,
      IEnumerable<IAssemblyReference> assemblyReferences, IEnumerable<IModuleReference> moduleReferences, IEnumerable<CompilationPart> compilationParts)
      : base(name, location, name, assemblyReferences, moduleReferences, new List<IResourceReference>(0).AsReadOnly(), new List<IFileReference>(0).AsReadOnly()) {
      this.options = options;
      this.hostEnvironment = hostEnvironment;
      this.compilationParts = compilationParts;
    }

    public override Compilation Compilation {
      get
        //^ ensures result is Compilation;
      {
        if (this.compilation == null) {
          lock (GlobalLock.LockingObject) {
            if (this.compilation == null) {
              if (this.compilationParts == null) {
                //^ assume this.programSources != null;
                this.compilation = new SpecSharpCompilation(this.hostEnvironment, this, this.options, this.ProvideCompilationParts());
              } else
                this.compilation = new SpecSharpCompilation(this.hostEnvironment, this, this.options, this.compilationParts);
              //TODO: construct unit sets from references. Associate these with the compilation.
            }
          }
        }
        return this.compilation;
      }
    }
    SpecSharpCompilation/*?*/ compilation;

    readonly IEnumerable<CompilationPart>/*?*/ compilationParts;

    protected override RootUnitNamespace CreateRootNamespace()
      //^^ ensures result.RootOwner == this;
    {
      //^ assume false; //constructor can't provide the necessary post condition until Spec# non delayed constructors get fixed
      return new RootUnitNamespace(this.Compilation.NameTable.EmptyName, this);
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public override IMethodReference EntryPoint {
      get {
        if (this.entryPoint == null) {
          lock (GlobalLock.LockingObject) {
            if (this.entryPoint == null) {
              //TODO: move this to static helper so that Module can share
              EntryPointFinder entryPointFinder = new EntryPointFinder(this.Compilation);
              entryPointFinder.Visit(this);
              IMethodDefinition entryPoint = Dummy.Method;
              foreach (IMethodDefinition ep in entryPointFinder.entryPoints) {
                entryPoint = ep; //TODO: check for dups, invalid args, generics etc. 
              }
              this.entryPoint = entryPoint;
            }
          }
          //report any errors found above
        }
        return this.entryPoint;
      }
    }
    IMethodReference/*?*/ entryPoint;

    readonly ISourceEditHost hostEnvironment;

    public override bool ILOnly {
      get { return true; }
    }

    readonly SpecSharpOptions options;

    readonly IEnumerable<SpecSharpSourceDocument>/*?*/ programSources;

    IEnumerable<CompilationPart> ProvideCompilationParts() {
      IEnumerable<SpecSharpSourceDocument> programSources;
      if (this.programSources == null)
        yield break;
      else
        programSources = this.programSources;
      foreach (SpecSharpSourceDocument programSource in programSources)
        yield return programSource.SpecSharpCompilationPart;
    }

  }

  public sealed class SpecSharpModule : Module {

    public SpecSharpModule(IName name, string location, ISourceEditHost hostEnvironment, SpecSharpOptions options, IAssembly containingAssembly,
      IEnumerable<IAssemblyReference> assemblyReferences, IEnumerable<IModuleReference> moduleReferences, IEnumerable<SpecSharpSourceDocument> programSources)
      //TODO: pass in information about which assemblies belong to which named unit sets
      : base(name, location, containingAssembly, assemblyReferences, moduleReferences)
    {
      this.options = options;
      this.hostEnvironment = hostEnvironment;
      this.programSources = programSources;
    }

    internal SpecSharpModule(IName name, string location, ISourceEditHost hostEnvironment, SpecSharpOptions options, IAssembly containingAssembly,
      IEnumerable<IAssemblyReference> assemblyReferences, IEnumerable<IModuleReference> moduleReferences, IEnumerable<CompilationPart> compilationParts)
      //TODO: pass in information about which assemblies belong to which named unit sets
      : base(name, location, containingAssembly, assemblyReferences, moduleReferences) {
      this.options = options;
      this.hostEnvironment = hostEnvironment;
      this.compilationParts = compilationParts;
    }

    public override Compilation Compilation {
      get 
        //^ ensures result is Compilation;
      {
        if (this.compilation == null) {
          if (this.compilationParts == null) {
            //^ assume this.programSources != null;
            this.compilation = new SpecSharpCompilation(this.hostEnvironment, this, this.options, this.ProvideCompilationParts());
          }else
            this.compilation = new SpecSharpCompilation(this.hostEnvironment, this, this.options, this.compilationParts);
          //TODO: construct unit sets from references. Associate these with the compilation.
        }
        return this.compilation;
      }
    }
    SpecSharpCompilation/*?*/ compilation = null;

    readonly IEnumerable<CompilationPart>/*?*/ compilationParts;

    protected override RootUnitNamespace CreateRootNamespace()
      //^^ ensures result.RootOwner == this;
    {
      //^ assume false; //constructor can't provide the necessary post condition until Spec# non delayed constructors get fixed
      return new RootUnitNamespace(this.Compilation.NameTable.EmptyName, this);
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public override IMethodReference EntryPoint {
      get {
        throw new NotImplementedException(); 
      } //TODO: get from compiler option via the symbol table, using Main as the default
    }

    readonly ISourceEditHost hostEnvironment;

    readonly SpecSharpOptions options;

    readonly IEnumerable<SpecSharpSourceDocument>/*?*/ programSources;

    IEnumerable<CompilationPart> ProvideCompilationParts() {
      IEnumerable<SpecSharpSourceDocument> programSources;
      if (this.programSources == null)
        yield break;
      else
        programSources = this.programSources;
      foreach (SpecSharpSourceDocument programSource in programSources)
        yield return programSource.SpecSharpCompilationPart;
    }

    public override ModuleIdentity ModuleIdentity {
      get {
        if (this.moduleIdentity == null) {
          moduleIdentity = UnitHelper.GetModuleIdentity(this);
        }
        return moduleIdentity;
      }
    }
    ModuleIdentity/*?*/ moduleIdentity;
  }
}