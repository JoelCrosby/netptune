using System.Collections.Generic;
using System.Linq;

using CsvHelper;

using Netptune.Core.Models.Import;

namespace Netptune.Core.Import
{
    public class HeaderValidator
    {
        private readonly List<string> InValidHeaders = new();
        private readonly List<string> MissingHeaders = new();

        public void ValidateHeaderRow(IEnumerable<InvalidHeader> invalidHeaders)
        {
            var headerNames = invalidHeaders.SelectMany(header => header.Names);

            InValidHeaders.AddRange(headerNames);
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
