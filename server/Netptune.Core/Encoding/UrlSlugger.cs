using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

using Netptune.Core.Utilities;

namespace Netptune.Core.Encoding
{
    public static class UrlSlugger
    {
        // white space, em-dash, en-dash, underscore
        private static readonly Regex WordDelimiters = new(@"[\s—–_]", RegexOptions.Compiled);

        // characters that are not valid
        private static readonly Regex InvalidChars = new(@"[^a-z0-9\-]", RegexOptions.Compiled);

        // multiple hyphens
        private static readonly Regex MultipleHyphens = new("-{2,}", RegexOptions.Compiled);

        public static string ToUrlSlug(this string value, bool appendUniqueId = false)
        {
            if (value is null) return null;

            // convert to lower case
            value = value.ToLowerInvariant();

            // remove diacritics (accents)
            value = RemoveDiacritics(value);

            // ensure all word delimiters are hyphens
            value = WordDelimiters.Replace(value, "-");

            // strip out invalid characters
            value = InvalidChars.Replace(value, "");

            // replace multiple hyphens (-) with a single hyphen
            value = MultipleHyphens.Replace(value, "-");

            // trim hyphens (-) from ends
            var result = value.Trim('-');

            if (!appendUniqueId) return result;

            return $"{result}-{UniqueIdBuilder.Generate()}";
        }

        private static string RemoveDiacritics(string value)
        {
            var stFormD = value.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (var character in stFormD)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(character);

                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(character);
                }
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
