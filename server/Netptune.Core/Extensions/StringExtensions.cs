using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Netptune.Core.Extensions
{
    public static class StringExtensions
    {
        public static string ToSentenceCase(this string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return string.Empty;
            }

            return Regex.Replace(str, "[a-z][A-Z]", m => $"{m.Value[0]} {char.ToLower(m.Value[1])}");
        }

        public static IEnumerable<string> GetLines(this string str, bool removeEmptyLines = false)
        {
            if (string.IsNullOrEmpty(str))
            {
                return new List<string>();
            }

            var separators = new[] {"\r\n", "\r", "\n"};
            var options = removeEmptyLines ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None;

            return str.Split(separators, options);
        }

        public static string Capitalize(this string input)
        {
            if (string.IsNullOrEmpty(input)) return null;

            if (input.Length == 1) return input.ToUpper();

            return $"{input.FirstOrDefault().ToString().ToUpper()}{input[1..]}";
        }

        public static string Truncate(this string str, int length)
        {
            if (string.IsNullOrEmpty(str))
            {
                return string.Empty;
            }

            var output = new StringBuilder();

            for (var i = 0; i < length; i++)
            {
                if (i < str.Length)
                {
                    output.Append(str[i]);
                }
            }

            return output.ToString();
        }

        public static string StripNonAscii(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            return Regex.Replace(input, @"[^\u0020-\u007E]", string.Empty);
        }
    }
}
