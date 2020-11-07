using CommandLine;
using DnExt.Commands.Utils;
using DnExt.Helpers;
using Microsoft.Diagnostics.Runtime;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DnExt.Commands
{
    public static partial class ClrCommands
    {
        class HeapStatOptions
        {
            [Option('m', "min", Required = false, Default = 1, HelpText = "Minimum number of occurences on heap")]
            public int MinOccurences { get; set; }
            [Option('n', "max", Required = false, Default = 100, HelpText = "Maximum number of occurences on heap")]
            public int MaxOccurences { get; set; }
            [Option('p', "pattern", Required = false, HelpText = "Filter pattern for type names")]
            public string FilterPattern { get; set; }
            [Option('r', "regex", Required = false, Default = false, HelpText = "Enable/disable regex filtering")]
            public bool IsRegex { get; set; }
        }

        public static string HeapStat(this DataTarget dataTarget, string args)
        {
            if (args.ParseAsCommandLine<HeapStatOptions>() is var clo && !clo.IsValid)
            {
                return clo.Message;
            }

            var rt = dataTarget.GetRuntime();
            var options = clo.Options;
            var m = new Matcher(options.IsRegex, options.FilterPattern);
            var objectDictionary = new Dictionary<ulong, int>();

            foreach (var o in rt.Heap.EnumerateObjects())
            {
                if (m.IsMatch(o.Type.Name))
                {
                    if (!objectDictionary.ContainsKey(o.Type.MethodTable))
                    {
                        objectDictionary.Add(o.Type.MethodTable, 0);
                    }

                    objectDictionary[o.Type.MethodTable]++;
                }
            }

            var mtList = objectDictionary
                .Where(kvp => kvp.Value >= options.MinOccurences && kvp.Value <= options.MaxOccurences)
                .OrderBy(kvp => kvp.Value)
                .ToList();
            var sbr = new StringBuilder();

            foreach (var mt in mtList)
            {
                var a = dataTarget.FormatAddress(mt.Key);
                sbr.AppendLine($"{OutputHelper.MakeDml($"!dumpmt /d {a}", $"{a}")}: {rt.Heap.GetTypeByMethodTable(mt.Key).Name} ({mt.Value})");
            }

            sbr.AppendLine($"Total {mtList.Count} types");

            return sbr.ToString();
        }

        // todo: dump with limiting # of obj
        public static string DumpHeap(this DataTarget dataTarget, string args)
        {
            return "hi";
        }
    }
}
