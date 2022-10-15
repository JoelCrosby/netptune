namespace Netptune.Core.ViewModels.Web;

public class MetaInfo
{
    public bool HasData { get; set; }

    public string? SiteName { get; set; } = null!;

    public string Url { get; set; } = null!;

    public string? Title { get; set; } = null!;

    public string? Description { get; set; }

    public string? Keywords { get; set; }

    public MetaInfoImage Image { get; set; } = new();
}

public class MetaInfoImage
{
    public string Url { get; set; } = null!;
}
