using System.Text.RegularExpressions;

namespace DnExt.Commands.Utils
{
    internal class Matcher
    {
        private bool _isRegex;
        string _pattern;
        Regex _regex;

        internal Matcher(bool isRegex, string pattern)
        {
            _isRegex = isRegex;
            _pattern = pattern;

            if (_isRegex)
            {
                _regex = new Regex(_pattern, RegexOptions.IgnoreCase);
            }
        }

        internal bool IsMatch(string input) => string.IsNullOrEmpty(_pattern) || (_isRegex ? _regex.IsMatch(input) : input.Contains(_pattern));
    }
}
