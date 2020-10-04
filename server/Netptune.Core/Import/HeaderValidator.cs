using System.Collections.Generic;
using System.Linq;

using Netptune.Core.Models.Import;

namespace Netptune.Core.Import
{
    public class HeaderValidator
    {
        private readonly List<string> InValidHeaders = new List<string>();
        private readonly List<string> MissingHeaders = new List<string>();

        public void ValidateHeaderRow(bool isValid, IList<string> headerNames, int index, ICollection<string> optionalHeaders)
        {
            if (isValid || optionalHeaders.Contains(headerNames[index])) return;

            InValidHeaders.Add(headerNames[index]);
        }

        public void AddMissingField(string field)
        {
            MissingHeaders.Add(field);
        }

        public HeaderValidationResult GetResult()
        {
            if (!InValidHeaders.Any() && !MissingHeaders.Any()) return HeaderValidationResult.Success();

            return new HeaderValidationResult
            {
                InvalidHeaders = InValidHeaders,
                MissingHeaders = MissingHeaders,
            };
        }
    }
}
