namespace Netptune.Core.Responses;

public class IsSlugUniqueResponse
{
    public string Request { get; set; } = null!;

    public string Slug { get; set; }= null!;

    public bool IsUnique { get; set; }
}
