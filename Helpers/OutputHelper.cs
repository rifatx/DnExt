using Microsoft.Diagnostics.Runtime;
using System;

namespace DnExt.Helpers
{
    internal static class OutputHelper
    {
        internal static string FormatAddress(this DataTarget dataTarget, ulong addr)
        {
            switch (dataTarget.Architecture)
            {
                case Architecture.X86:
                case Architecture.Arm:
                    return $"{addr:00000000X}";
                default:
                    var l = addr >> 32;
                    var r = addr & 0xffffffff;
                    return $"{l:X8}`{r:X8}";
            }
        }

        internal static string FormatAddress(this IntPtr debugClient, ulong addr) =>
            debugClient
                .GetDataTarget()
                .FormatAddress(addr);

        internal static string MakeDml(string command, string text, string rest = null) => @$"<link cmd=""{command}"">{text}</link>{rest}";

        internal static string CleanWindbgOutput(this string text) => text.Replace("%", "%%");
    }
}
