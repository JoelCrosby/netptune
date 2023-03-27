namespace Netptune.Core.ViewModels.Web;

public record MetaInfoResponse
{
    public bool Success { get; init; }

    public MetaInfo? Meta { get; init; }
}

public record MetaInfo
{
    public bool HasData { get; set; }

    public string? SiteName { get; set; }

    public string Url { get; set; } = null!;

    public string? Title { get; set; }

    public string? Description { get; set; }

    public string? Keywords { get; set; }

    public MetaInfoImage Image { get; set; } = new();
}

public record MetaInfoImage
{
    public string Url { get; set; } = null!;
}
