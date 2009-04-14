//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using Microsoft.Cci.Ast;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci.SmallBasic {

  public sealed class SmallBasicAssembly : Assembly {

    public SmallBasicAssembly(IName name, string location, ISourceEditHost hostEnvironment, IDictionary<string,string> options,
      IEnumerable<IAssemblyReference> assemblyReferences, IEnumerable<IModuleReference> moduleReferences, IEnumerable<SmallBasicDocument> programSources)
      : base(name, location, name, assemblyReferences, moduleReferences, new List<IResourceReference>(0).AsReadOnly(), new List<IFileReference>(0).AsReadOnly())
    {
      this.options = options;
      this.hostEnvironment = hostEnvironment;
      this.programSources = programSources;
    }

    internal SmallBasicAssembly(IName name, string location, ISourceEditHost hostEnvironment, IDictionary<string, string> options,
      IEnumerable<IAssemblyReference> assemblyReferences, IEnumerable<IModuleReference> moduleReferences, IEnumerable<CompilationPart> compilationParts)
      : base(name, location, name, assemblyReferences, moduleReferences, new List<IResourceReference>(0).AsReadOnly(), new List<IFileReference>(0).AsReadOnly()) 
    {
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
                this.compilation = new SmallBasicCompilation(this.hostEnvironment, this, this.ProvideCompilationParts());
              } else
                this.compilation = new SmallBasicCompilation(this.hostEnvironment, this, this.compilationParts);
              //TODO: construct unit sets from references. Associate these with the compilation.
            }
          }
        }
        return this.compilation;
      }
    }
    SmallBasicCompilation/*?*/ compilation;

    readonly IEnumerable<CompilationPart>/*?*/ compilationParts;

    protected override RootUnitNamespace CreateRootNamespace() {
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

    readonly IDictionary<string,string> options;

    readonly IEnumerable<SmallBasicDocument>/*?*/ programSources;

    IEnumerable<CompilationPart> ProvideCompilationParts() {
      IEnumerable<SmallBasicDocument> programSources;
      if (this.programSources == null)
        yield break;
      else
        programSources = this.programSources;
      foreach (SmallBasicDocument programSource in programSources)
        yield return programSource.SmallBasicCompilationPart;
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
      if (method.Name.Value == "EntryPoint" && method.Visibility == TypeMemberVisibility.Public && method.IsStatic &&
        (method.Type.TypeCode == PrimitiveTypeCode.Int32 || method.Type.TypeCode == PrimitiveTypeCode.Void)) {
        this.entryPoints.Add(method);
      }
    }

  }

  public sealed class SmallBasicModule : Module {

    public SmallBasicModule(IName name, string location, ISourceEditHost hostEnvironment, IDictionary<string,string> options, IAssembly containingAssembly,
      IEnumerable<IAssemblyReference> assemblyReferences, IEnumerable<IModuleReference> moduleReferences, IEnumerable<SmallBasicDocument> programSources)
      //TODO: pass in information about which assemblies belong to which named unit sets
      : base(name, location, containingAssembly, assemblyReferences, moduleReferences)
    {
      this.options = options;
      this.hostEnvironment = hostEnvironment;
      this.programSources = programSources;
    }

    internal SmallBasicModule(IName name, string location, ISourceEditHost hostEnvironment, IDictionary<string,string> options, IAssembly containingAssembly,
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
            this.compilation = new SmallBasicCompilation(this.hostEnvironment, this, this.ProvideCompilationParts());
          }else
            this.compilation = new SmallBasicCompilation(this.hostEnvironment, this, this.compilationParts);
          //TODO: construct unit sets from references. Associate these with the compilation.
        }
        return this.compilation;
      }
    }
    SmallBasicCompilation/*?*/ compilation;

    readonly IEnumerable<CompilationPart>/*?*/ compilationParts;

    protected override RootUnitNamespace CreateRootNamespace() {
      return new RootUnitNamespace(this.Compilation.NameTable.EmptyName, this);
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public override IMethodReference EntryPoint {
      get { 
        throw new Exception("The method or operation is not implemented."); 
      } //TODO: get from compiler option via the symbol table, using Main as the default
    }

    readonly ISourceEditHost hostEnvironment;

    readonly IDictionary<string,string> options;

    readonly IEnumerable<SmallBasicDocument>/*?*/ programSources;

    IEnumerable<CompilationPart> ProvideCompilationParts() {
      IEnumerable<SmallBasicDocument> programSources;
      if (this.programSources == null)
        yield break;
      else
        programSources = this.programSources;
      foreach (SmallBasicDocument programSource in programSources)
        yield return programSource.SmallBasicCompilationPart;
    }

    public override ModuleIdentity ModuleIdentity {
      get {
        if (this.moduleIdentity == null) {
          this.moduleIdentity = UnitHelper.GetModuleIdentity(this);
        }
        return this.moduleIdentity;
      }
    }
    ModuleIdentity/*?*/ moduleIdentity;
  }
}