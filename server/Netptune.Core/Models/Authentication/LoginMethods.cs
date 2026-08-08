namespace Netptune.Core.Models.Authentication;

public record LoginMethods
{
    public IList<string> Providers { get; init; } = [];

    public bool HasPassword { get; init; }
}
