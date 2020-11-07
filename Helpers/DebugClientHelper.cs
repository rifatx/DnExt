using DnExt.Commands;
using Microsoft.Diagnostics.Runtime;
using Microsoft.Diagnostics.Runtime.Interop;
using System;
using System.Runtime.InteropServices;

namespace DnExt.Helpers
{
    internal static class DebugClientHelper
    {
        private static object _IUnknown;
        private static IDebugControl _debugControl;
        private static IDebugSymbols _debugSymbols;
        private static DataTarget _dataTarget;

        private static object GetIUnknown(this IntPtr debugClient)
        {
            if (_IUnknown == null)
            {
                _IUnknown = Marshal.GetUniqueObjectForIUnknown(debugClient);
            }

            return _IUnknown;
        }

        internal static IDebugControl GetDebugControl(this IntPtr debugClient)
        {
            if (_debugControl == null)
            {
                _debugControl = (IDebugControl)debugClient.GetIUnknown();
            }

            return _debugControl;
        }

        internal static IDebugSymbols GetDebugSymbols(this IntPtr debugClient)
        {
            if (_debugSymbols == null)
            {
                _debugSymbols = (IDebugSymbols)debugClient.GetIUnknown();
            }

            return _debugSymbols;
        }

        internal static DataTarget GetDataTarget(this IntPtr debugClient)
        {
            if (_dataTarget == null)
            {
                _dataTarget = DataTarget.CreateFromDebuggerInterface((IDebugClient)debugClient.GetIUnknown());
            }

            return _dataTarget;
        }

        internal static ClrRuntime GetRuntime(this IntPtr debugClient) =>
            debugClient.GetDataTarget()
                .ClrVersions[ClrCommands.ClrVersionIndex]
                .CreateRuntime();

        internal static void Write(this IntPtr debugClient, string text) => debugClient.GetDebugControl().ControlledOutput(DEBUG_OUTCTL.ALL_CLIENTS, DEBUG_OUTPUT.NORMAL, text);

        internal static void WriteLine(this IntPtr debugClient, string text) => debugClient.Write($"{text}{Environment.NewLine}");

        internal static void WriteDml(this IntPtr debugClient, string text) => debugClient.GetDebugControl().ControlledOutput(DEBUG_OUTCTL.AMBIENT_DML, DEBUG_OUTPUT.NORMAL, text);

        internal static void WriteDmlLine(this IntPtr debugClient, string text) => debugClient.WriteDml($"{text}{Environment.NewLine}");
    }
}
