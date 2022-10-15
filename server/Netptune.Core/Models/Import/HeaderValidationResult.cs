using System.Collections.Generic;

namespace Netptune.Core.Models.Import;

public class HeaderValidationResult
{
    public bool IsSuccess { get; init; }

    public IEnumerable<string> InvalidHeaders { get; init; } = null!;

    public IEnumerable<string> MissingHeaders { get; init; } = null!;

    public static HeaderValidationResult Success()
    {
        return new()
        {
            IsSuccess = true,
        };
    }
}
