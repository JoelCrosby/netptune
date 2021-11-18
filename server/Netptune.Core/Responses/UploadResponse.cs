namespace Netptune.Core.Responses;

public class UploadResponse
{
    public string Key { get; set; }

    public string Path { get; set; }

    public string Uri { get; set; }

    public long Size { get; set; }
}