using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;

namespace Microsoft.Research {
  public class CCRefGen : Task {
    #region Properties

    bool sourceText = true;
    public bool IncludeSourceTextInContracts { get { return sourceText; } set { sourceText = value; } }

    public bool Verifiable { get; set; }

    public string Output { get; set; }

    public string Input { get; set; }

    public bool WritePDB { get; set; }

    public ITaskItem[] LibPaths { get; set; }

    public ITaskItem[] ResolvedPaths { get; set; }

    public bool Break { get; set; }

    #endregion


    public override bool Execute() {

#if DEBUG
      if (!System.Diagnostics.Debugger.IsAttached)
      {
        System.Diagnostics.Debugger.Launch();
      }
#endif

      try {
        #region Setup parameters

        if (Input == null) { this.Log.LogError("Input parameter must be specified"); return false; }

        AsmMeta.AsmMeta asmmeta = new AsmMeta.AsmMeta(this.ErrorLogger);
        asmmeta.options.GeneralArguments.Add(Input);
        asmmeta.options.output = Output;
        asmmeta.options.includeSourceTextInContract = IncludeSourceTextInContracts;
        asmmeta.options.writePDB = WritePDB;
        if (ResolvedPaths!= null) {
          foreach (var lp in ResolvedPaths)
          {
            asmmeta.options.resolvedPaths.Add(lp.ItemSpec);
          }
        }
        if (LibPaths != null)
        {
          foreach (var lp in LibPaths)
          {
            asmmeta.options.libPaths.Add(lp.ItemSpec);
          }
        }
        asmmeta.options.doBreak = Break;
        #endregion

        var result = asmmeta.Run();

        return (result == 0);
      } catch (Exception e) {
        this.Log.LogError("Exception: {0} caught", e.Message);
        return false;
      }
    }

    void ErrorLogger(string format, params string[] args) {
      this.Log.LogError(format, args);
    }

  }

}