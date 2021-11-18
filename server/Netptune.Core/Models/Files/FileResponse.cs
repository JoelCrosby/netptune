using System.IO;

namespace Netptune.Core.Models.Files;

public class FileResponse
{
    public Stream Stream { get; set; }

    public string ContentType { get; set; }

    public string Filename { get; set; }
}