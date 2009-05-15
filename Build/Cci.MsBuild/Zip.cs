using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;
using Ionic.Zip;
using System.IO;

namespace Microsoft.Cci.MsBuild
{
    /// <summary>
    /// Zips files 
    /// </summary>
    public sealed class Zip
        : Task
    {
        [Required]
        public ITaskItem[] Files { get; set; }

        [Required]
        public ITaskItem OutputFile { get; set; }

        public override bool Execute()
        {
            using (var zip = new ZipFile())
            {
                this.Log.LogMessage("creating zip file");
                foreach (var file in this.Files)
                {
                    this.Log.LogMessage("adding {0}", file);
                    zip.AddFile(file.ItemSpec, ".");
                }

                this.Log.LogMessage("saving zip to {0}", this.OutputFile.ItemSpec);
                zip.Save(this.OutputFile.ItemSpec);
            }

            return true;
        }
    }
}
