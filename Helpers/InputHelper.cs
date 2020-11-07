namespace DnExt.Helpers
{
    internal static class InputHelper
    {
        internal static string CleanArgs(this string args) =>
             args
                .Replace("`", string.Empty)
                .Trim();
    }
}
