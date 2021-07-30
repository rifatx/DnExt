using CommandLine;
using DnExt.Helpers;
using Microsoft.Diagnostics.Runtime;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace DnExt.Commands
{
    public static partial class ClrCommands
    {
        class DumpDataTableOptions
        {
            [Option('n', "maxrowcount", Required = false, Default = 100, HelpText = "Maximum number of rows")]
            public int MaxRows { get; set; }
            [Value(0)]
            public string Address { get; set; }
        }

        public static string DumpDataTable(this DataTarget dataTarget, string args)
        {
            if (args.ParseAsCommandLine<DumpDataTableOptions>() is var clo && !clo.IsValid)
            {
                return clo.Message;
            }

            var rt = dataTarget.GetRuntime();
            var options = clo.Options;

            if (!ulong.TryParse(options.Address, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var address))
            {
                return "invalid address";
            }

            if (rt.Heap.GetObjectType(address) is var ot && (!ot?.Name?.Equals("System.Data.DataTable") ?? true))
            {
                return $"invalid type: {ot.Name}";
            }

            var o = rt.Heap.GetObject(address);
            var columnList = o.GetFieldFrom("columnCollection")
                    .GetFieldFrom("_list")
                    .GetFieldFrom("_items");
            var rowCount = o.GetField<long>("nextRowID") - 1;
            var columnArray = rt.Heap.GetObject(columnList.Address);
            var columnArrayType = columnArray.Type;
            var table = new List<List<string>>();

            for (int i = 0; i < columnArray.Length; i++)
            {
                var colAddress = columnArrayType.GetArrayElementAddress(columnArray.Address, i);

                if (!rt.Heap.ReadPointer(colAddress, out colAddress))
                {
                    return $"Error reading address {colAddress:X}";
                }

                if (colAddress == 0)
                {
                    break;
                }

                var col = rt.Heap.GetObject(colAddress);

                if (col.Address == 0)
                {
                    break;
                }

                var colName = col.GetStringField("_columnName");
                var colData = new List<string> { colName };
                var colValues = col.GetFieldFrom("_storage")
                    .GetFieldFrom("values");
                var colValueArray = rt.Heap.GetObject(colValues.Address);

                for (int j = 0; j < Math.Min(rowCount, options.MaxRows); j++)
                {
                    object? colValue;

                    if (colValueArray.Type.ComponentType.IsValueClass)
                    {
                        var colValueAddress = colValueArray.Type.GetArrayElementAddress(colValueArray.Address, j);

                        var buffer = new byte[colValueArray.Type.ElementSize];
                        var res = rt.ReadMemory(colValueAddress, buffer, buffer.Length, out var bytesRead);
                        ref var bufferRef = ref MemoryMarshal.GetReference(buffer.AsSpan());

                        object colObj = colValueArray.Type.ToString().Replace("[]", string.Empty) switch
                        {
                            "System.Byte" => Unsafe.As<byte, byte>(ref bufferRef),
                            "System.SByte" => Unsafe.As<byte, sbyte>(ref bufferRef),
                            "System.Int16" => Unsafe.As<byte, short>(ref bufferRef),
                            "System.UInt16" => Unsafe.As<byte, ushort>(ref bufferRef),
                            "System.Int32" => Unsafe.As<byte, int>(ref bufferRef),
                            "System.UInt32" => Unsafe.As<byte, uint>(ref bufferRef),
                            "System.Int64" => Unsafe.As<byte, long>(ref bufferRef),
                            "System.UInt64" => Unsafe.As<byte, ulong>(ref bufferRef),
                            "System.Single" => Unsafe.As<byte, float>(ref bufferRef),
                            "System.Double" => Unsafe.As<byte, double>(ref bufferRef),
                            "System.Decimal" => Unsafe.As<byte, decimal>(ref bufferRef),
                            "System.Char" => Unsafe.As<byte, char>(ref bufferRef),
                            "System.Boolean" => Unsafe.As<byte, bool>(ref bufferRef),
                            "System.DateTime" => Unsafe.As<byte, DateTime>(ref bufferRef),
                            _ => OutputHelper.MakeDml($"db {rt.DataTarget.FormatAddress(colValueAddress)}", $"{rt.DataTarget.FormatAddress(colValueAddress)} ({colValueArray.Type.Name.Replace("[]", string.Empty)})")
                        };

                        colValue = colObj.ToString();
                    }
                    else
                    {
                        colValue = colValueArray.Type.GetArrayElementValue(colValueArray.Address, j)?.ToString();
                    }

                    colData.Add(colValue?.ToString() ?? string.Empty);
                }

                var maxLen = colData
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Select(s => OutputHelper.GetDmlInnerText(s).Length)
                    .Max();

                colData.Insert(1, new string('─', maxLen));

                for (int j = 0; j < colData.Count; j++)
                {
                    colData[j] = colData[j]?.PadLeft(maxLen);
                }

                table.Add(colData);
            }

            var sbr = new StringBuilder();

            foreach (var l in table
                .SelectMany(inner => inner.Select((t, i) => new { Item = t, Index = i }))
                .GroupBy(i => i.Index, i => i.Item)
                .Select((g, i) => (List: g.ToList(), Index: i)))
            {
                sbr.AppendLine(string.Join(l.Index == 1 ? "┼" : "│", l.List));
            }

            return sbr.ToString();
        }

        public static string DumpDataSet(this DataTarget dataTarget, string args)
        {
            var rt = dataTarget.GetRuntime();

            args = args.CleanArgs();

            if (!ulong.TryParse(args, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var address))
            {
                return "invalid address";
            }

            if (rt.Heap.GetObjectType(address) is var ot && (!ot?.Name?.Equals("System.Data.DataSet") ?? true))
            {
                return $"invalid type: {ot?.Name ?? "<unk>"}";
            }

            var tableList = rt.Heap.GetObject(address)
                    .GetFieldFrom("tableCollection")
                    .GetFieldFrom("_list")
                    .GetFieldFrom("_items");
            var tableArray = rt.Heap.GetObject(tableList.Address);
            var tableArrayType = tableArray.Type;

            var sbr = new StringBuilder();

            for (int i = 0; i < tableArray.Length; i++)
            {
                var dtAddress = tableArrayType.GetArrayElementAddress(tableArray.Address, i);

                if (rt.Heap.ReadPointer(dtAddress, out dtAddress))
                {
                    var dt = rt.Heap.GetObject(dtAddress);

                    if (dt.Address > 0)
                    {
                        var a = dataTarget.FormatAddress(dtAddress);
                        sbr.AppendLine(@$"{dt.GetStringField("tableName")}: {OutputHelper.MakeDml($"!dumpdatatable {a}", $"{a}")}");
                    }
                }
            }

            return sbr.ToString();
        }
    }
}
