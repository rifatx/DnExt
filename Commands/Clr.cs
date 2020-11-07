using DnExt.Helpers;
using Microsoft.Diagnostics.Runtime;
using System.Text;

namespace DnExt.Commands
{
    public static partial class ClrCommands
    {
        internal static int ClrVersionIndex { get; private set; } = 0;

        internal static ClrRuntime GetRuntime(this DataTarget dataTarget) =>
            dataTarget
                .ClrVersions[ClrVersionIndex]
                .CreateRuntime();

        public static string ListClrVersions(this DataTarget dataTarget, string args)
        {
            var clrVersions = dataTarget.ClrVersions;
            var sbr = new StringBuilder();

            for (var i = 0; i < clrVersions.Count; ++i)
            {
                sbr.AppendLine(OutputHelper.MakeDml($"!setclrversion {i + 1}", $"{i + 1}", $": {clrVersions[i].Version}"));
            }

            return sbr.ToString();
        }

        public static string SetClrVersion(this DataTarget dataTarget, string args)
        {
            var clrVersions = dataTarget.ClrVersions;

            if (!int.TryParse(args.CleanArgs(), out var i) || i < 1 || i > clrVersions.Count)
            {
                return $"Invalid index: {args}";
            }

            ClrVersionIndex = i - 1;

            return string.Empty;
        }
    }
}
