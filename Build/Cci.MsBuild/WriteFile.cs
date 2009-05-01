// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;
using System.IO;

namespace Microsoft.Cci.MsBuild
{
    /// <summary>
    /// Over-writes content to a file
    /// </summary>
    public sealed class WriteFile
        : Task
    {
        /// <summary>
        /// Gets or sets the file.
        /// </summary>
        /// <value>The file.</value>
        [Required]
        public ITaskItem File { get; set; }

        /// <summary>
        /// Gets or sets the content.
        /// </summary>
        /// <value>The content.</value>
        [Required]
        public string Content { get; set; }

        /// <summary>
        /// When overridden in a derived class, executes the task.
        /// </summary>
        /// <returns>
        /// true if the task successfully executed; otherwise, false.
        /// </returns>
        public override bool Execute()
        {
            var file = new FileInfo(this.File.ItemSpec);
            if (file.Exists && file.IsReadOnly)
                file.IsReadOnly = false;
            System.IO.File.WriteAllText(file.FullName, this.Content);

            return true;
        }
    }
}
