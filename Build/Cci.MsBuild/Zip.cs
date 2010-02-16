//-----------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.  All Rights Reserved.
// This code is licensed under the Microsoft Public License.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//-----------------------------------------------------------------------------
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
        public ITaskItem[] Files { get; set; }

        public ITaskItem[] Directories { get; set; }

        [Required]
        public ITaskItem OutputFile { get; set; }

        public override bool Execute()
        {
            using (var zip = new ZipFile())
            {
                this.Log.LogMessage("creating zip file");

                if (this.Files != null)
                    foreach (var file in this.Files)
                    {
                        this.Log.LogMessage("adding {0}", file);
                        zip.AddFile(file.ItemSpec, ".");
                    }
                if (this.Directories != null)
                    foreach (var directory in this.Directories)
                    {
                        if (!Directory.Exists(directory.ItemSpec))
                        {
                            this.Log.LogError("directory {0} does not exist", directory);
                            return false;
                        }
                        this.Log.LogMessage("adding directory {0}", directory);
                        zip.AddDirectory(directory.ItemSpec, ".");
                    }

                if (zip.Count == 0)
                {                  
                    this.Log.LogMessage("no files added to the zip");
                    return false;
                }

                this.Log.LogMessage("saving zip to {0}", this.OutputFile.ItemSpec);
                zip.Save(this.OutputFile.ItemSpec);
            }

            return true;
        }
    }
}
