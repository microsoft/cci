using System;
using System.Collections.Generic;
using System.Text;

namespace CciSharp.Framework
{
    class CcsErrorReporter
    {
        private CcsErrorReporter() { }
        public static readonly CcsErrorReporter Instance = new CcsErrorReporter();
    }
}
