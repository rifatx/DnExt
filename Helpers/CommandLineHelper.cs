using CommandLine;
using CommandLine.Text;
using System;
using System.Globalization;

namespace DnExt.Helpers
{
    internal class CommandLineOptions<T>
    {
        internal T Options { get; set; } = default;
        internal string Message { get; set; } = null;
        internal bool IsValid => Options != null && string.IsNullOrEmpty(Message);
    }

    internal static class CommandLineHelper
    {
        internal static CommandLineOptions<T> ParseAsCommandLine<T>(this string args)
        {
            var options = default(T);
            var parser = new Parser(s => s.ParsingCulture = CultureInfo.CurrentCulture);
            var pr = parser.ParseArguments<T>(args.CleanArgs().Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries))
                .WithParsed(o => options = o);

            if (pr.Tag == ParserResultType.NotParsed)
            {
                return new CommandLineOptions<T>
                {
                    Message = HelpText.AutoBuild(pr).ToString()
                };
            }

            return new CommandLineOptions<T>
            {
                Options = options
            };
        }
    }
}
