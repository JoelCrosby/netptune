using System.Collections.Generic;

namespace Netptune.Core.Models.Import
{
    public class HeaderValidationResult
    {
        public bool IsSuccess { get; set; }

        public IEnumerable<string> InvalidHeaders { get; set; }

        public IEnumerable<string> MissingHeaders { get; set; }

        public static HeaderValidationResult Success()
        {
            return new HeaderValidationResult
            {
                IsSuccess = true,
            };
        }
    }
}
