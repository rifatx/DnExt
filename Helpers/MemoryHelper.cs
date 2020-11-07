using Microsoft.Diagnostics.Runtime;
using Microsoft.Diagnostics.Runtime.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DnExt.Helpers
{
    internal static class MemoryHelper
    {
        internal static T ReadMemory<T>(this DataTarget dataTarget, ulong address)
        {
            var dds = (IDebugDataSpaces4)dataTarget.DebuggerInterface;
            uint read = (uint)Marshal.SizeOf(typeof(T));

            byte[] buffer = new byte[read];
            var res = default(T);
         
            if (dds.ReadVirtual(address, buffer, read, out read) != 0)
            {
                return res;
            }

            GCHandle pinnedTarget = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            res = (T)Marshal.PtrToStructure(pinnedTarget.AddrOfPinnedObject(), typeof(T));
            pinnedTarget.Free();

            return res;
        }
    }
}
