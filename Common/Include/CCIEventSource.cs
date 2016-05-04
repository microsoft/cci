// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.Tracing;
using Microsoft.Cci;

/// <summary>
/// This eventSource can be used for Telemetry/tracing of the CCI assembly.   
///         PerfView /Providers=*Microsoft-CCI collect 
/// turns it on.  
/// </summary>
[EventSource(Name = "Microsoft-CCI")]
internal class CciEventSource : EventSource
{
    static public CciEventSource Log = new CciEventSource();

    // Used to specify which events to turn on.    
    public class Keywords
    {
        /// <summary>
        /// Events associated with reading IL DLLs
        /// </summary>
        public const EventKeywords PERead = (EventKeywords)1;
        public const EventKeywords PEReadDetailed = (EventKeywords)2;
        /// <summary>
        /// Events associated with writing IL DLLs
        /// </summary>
        public const EventKeywords PEWrite = (EventKeywords)4;
        public const EventKeywords PEWriteDetailed = (EventKeywords)8;
    }

    // Generally useful
    [Event(1)]
    public void Message(string Message) { WriteEvent(1, Message); }

    // Write Instrumentation
    [Event(2, Keywords = Keywords.PEWrite | Keywords.PEWriteDetailed)]
    public void ModuleWritten(string Name, int ModuleId, string FileName) { WriteEvent(2, Name, ModuleId, FileName); }
    [Event(3, Keywords = Keywords.PEWrite | Keywords.PEWriteDetailed)]
    public void ModuleWrittenSize(string Name, int ModuleId, string FileName, int Size) { WriteEvent(3, Name, ModuleId, FileName, Size); }
    [Event(4, Keywords = Keywords.PEWriteDetailed)]
    public void TypeWritten(string Name, int TypeId, int ModuleId) { WriteEvent(4, Name, TypeId, ModuleId); }
    [Event(5, Keywords = Keywords.PEWriteDetailed)]
    public void MethodWritten(string Name, int TypeId, int Size) { WriteEvent(5, Name, TypeId, Size); }

    // Read Instrumentation
    [Event(6, Keywords = Keywords.PERead | Keywords.PEReadDetailed)]
    private void ModuleOpened(string Name, string FileName, int ModuleId, int Size) { WriteEvent(6, Name, FileName, ModuleId, Size); }
    [NonEvent]
    public void ModuleOpened(IModule module, ModuleIdentity moduleIdentity, uint size)
    {
        ModuleOpened(module.Name.Value, moduleIdentity == null ? "" : moduleIdentity.Location, module.GetHashCode(), (int) size);
    }
}


