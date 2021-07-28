using Microsoft.Diagnostics.Runtime;
using System;
using System.Text.RegularExpressions;

namespace DnExt.Helpers
{
    internal static class OutputHelper
    {
        private static Regex _dmlRegex = new Regex("<link cmd=\".*\">(.*)</link>");

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

        internal static string GetDmlInnerText(string dml) =>
            _dmlRegex.Match(dml) is var m
            && m.Success
        ? m.Groups[1].Value
        : dml;

        internal static string CleanWindbgOutput(this string text) => text.Replace("%", "%%");
    }
}
