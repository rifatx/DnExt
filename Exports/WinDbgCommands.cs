using DnExt.Commands;
using DnExt.Constants;
using DnExt.Helpers;
using RGiesecke.DllExport;
using System;
using System.Runtime.InteropServices;

namespace DnExt.Exports
{
    public partial class WinDbgCommands
    {
        [DllExport(CommandNames.LIST_CLR_VERSIONS)]
        public static void ListClrVersions([In] IntPtr client, [In, MarshalAs(UnmanagedType.LPStr)] string args) =>
            client.WriteDmlLine(client.GetDataTarget().ListClrVersions(args).CleanWindbgOutput());

        [DllExport(CommandNames.SET_CLR_VERSIONS)]
        public static void SetClrVersion([In] IntPtr client, [In, MarshalAs(UnmanagedType.LPStr)] string args) =>
            client.WriteDmlLine(client.GetDataTarget().SetClrVersion(args).CleanWindbgOutput());

        [DllExport(CommandNames.GET_MODULES)]
        public static void GetModules([In] IntPtr client, [In, MarshalAs(UnmanagedType.LPStr)] string args) =>
            client.WriteDmlLine(client.GetDataTarget().GetModules(client, args).CleanWindbgOutput());

        [DllExport(CommandNames.SAVE_MODULE)]
        public static void SaveModule([In] IntPtr client, [In, MarshalAs(UnmanagedType.LPStr)] string args) =>
            client.WriteDmlLine(client.GetDataTarget().SaveModule(args).CleanWindbgOutput());


        [DllExport(CommandNames.DUMP_DATASET)]
        public static void DumpDataSet([In] IntPtr client, [In, MarshalAs(UnmanagedType.LPStr)] string args) =>
            client.WriteDmlLine(client.GetDataTarget().DumpDataSet(args).CleanWindbgOutput());

        [DllExport(CommandNames.DUMP_DATATABLE)]
        public static void DumpDataTable([In] IntPtr client, [In, MarshalAs(UnmanagedType.LPStr)] string args) =>
            client.WriteDmlLine(client.GetDataTarget().DumpDataTable(args).CleanWindbgOutput());

        [DllExport(CommandNames.HEAP_STAT)]
        public static void HeapStat([In] IntPtr client, [In, MarshalAs(UnmanagedType.LPStr)] string args) =>
            client.WriteDmlLine(client.GetDataTarget().HeapStat(args).CleanWindbgOutput());
    }
}
