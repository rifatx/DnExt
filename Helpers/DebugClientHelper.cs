using DnExt.Commands;
using Microsoft.Diagnostics.Runtime;
using Microsoft.Diagnostics.Runtime.Interop;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace DnExt.Helpers
{
    internal static class DebugClientHelper
    {
        private static bool _initialized = false;
        private static object _IUnknown;
        private static IDebugClient6 _debugClient;
        private static IDebugControl6 _debugControl;
        private static IDebugSymbols _debugSymbols;
        private static DataTarget _dataTarget;

        internal static string DumpFilePath { get; private set; }

        static void Init(IntPtr debugClientPtr)
        {
            if (_initialized)
            {
                return;
            }

            var debugClient = debugClientPtr.GetDebugClient();

            var sbr = new StringBuilder();

            if (debugClient.GetDumpFile(0, sbr, 1024, out _, out var _, out var _) != 0)
            {
                return;
            }

            DumpFilePath = sbr.ToString();
            _initialized = true;
        }

        private static object GetIUnknown(this IntPtr debugClient)
        {
            if (_IUnknown == null)
            {
                _IUnknown = Marshal.GetUniqueObjectForIUnknown(debugClient);
            }

            return _IUnknown;
        }

        internal static IDebugClient6 GetDebugClient(this IntPtr debugClient)
        {
            if (_debugControl == null)
            {
                _debugClient = (IDebugClient6)debugClient.GetIUnknown();
            }

            return _debugClient;
        }

        internal static IDebugControl6 GetDebugControl(this IntPtr debugClient)
        {
            if (_debugControl == null)
            {
                _debugControl = (IDebugControl6)debugClient.GetIUnknown();
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
            Init(debugClient);

            if (_dataTarget == null)
            {
                _dataTarget = DataTarget.CreateFromDebuggerInterface(debugClient.GetDebugClient());
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
