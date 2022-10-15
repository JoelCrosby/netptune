using System.IO;

namespace Netptune.Core.Models.Files;

public class FileResponse
{
    public Stream Stream { get; init; } = null!;

    public string ContentType { get; init; } = null!;

    public string Filename { get; init; } = null!;
}
