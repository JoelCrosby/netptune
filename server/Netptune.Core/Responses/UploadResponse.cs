namespace Netptune.Core.Responses;

public class UploadResponse
{
    public string Name { get; set; } = null!;

    public string Key { get; set; } = null!;

    public string Path { get; set; } = null!;

    public string Uri { get; set; } = null!;

    public long Size { get; set; }
}
