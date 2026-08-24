namespace Netptune.Core.ViewModels.Branding;

public sealed record BrandingImageViewModel
{
    public required string FileId { get; init; }

    public required string ContentUrl { get; init; }

    public required long SizeBytes { get; init; }
}
