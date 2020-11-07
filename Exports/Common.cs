using DnExt.Constants;
using RGiesecke.DllExport;
using DnExt.Common;

namespace DnExt.Exports
{
    public partial class WinDbgCommands
    {
        static WinDbgCommands()
        {
            DependencyResolver.RegisterAssemblyResolve();
        }

        private static uint DEBUG_EXTENSION_VERSION(uint major, uint minor)
        {
            return ((((major) & 0xffff) << 16) | ((minor) & 0xffff));
        }

        [DllExport(CommandNames.DEBUG_EXTENSION_INITIALIZE)]
        public static int DebugExtensionInitialize(ref uint version, ref uint flags)
        {
            version = DEBUG_EXTENSION_VERSION(1, 0);
            flags = 0;
            return 0;
        }

        [DllExport(CommandNames.DEBUG_EXTENSION_UNINITIALIZE)]
        public static void DebugExtensionUninitialize() { }

        [DllExport(CommandNames.DEBUG_EXTENSION_NOTIFY)]
        public static void DebugExtensionNotify(uint notify, ulong argument) { }


    }
}
