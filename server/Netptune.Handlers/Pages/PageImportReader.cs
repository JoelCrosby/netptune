using System.IO.Compression;
using System.Text;

using Netptune.Core.Models.Pages;

namespace Netptune.Handlers.Pages;

public sealed record PageImportUpload(string FileName, Stream Content);

public sealed record PageImportRead
{
    public List<PageImportFile> Files { get; init; } = [];

    public List<string> Skipped { get; init; } = [];

    public string? Error { get; init; }
}

public static class PageImportReader
{
    public const int MaxFiles = 300;
    public const long MaxTotalBytes = 30L * 1024 * 1024;

    private static readonly HashSet<string> MarkdownExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".md",
        ".markdown",
        ".txt",
    };

    public static async Task<PageImportRead> Read(IReadOnlyList<PageImportUpload> uploads, CancellationToken cancellationToken)
    {
        var isZip = uploads.Count == 1 && HasExtension(uploads[0].FileName, ".zip");

        if (isZip)
        {
            return await ReadZip(uploads[0].Content, cancellationToken);
        }

        var collector = new Collector();

        foreach (var upload in uploads)
        {
            var path = Path.GetFileName(upload.FileName);
            var error = await collector.Add(path, upload.Content, upload.Content.CanSeek ? upload.Content.Length : null, cancellationToken);

            if (error is not null)
            {
                return collector.Fail(error);
            }
        }

        return collector.Finish();
    }

    private static async Task<PageImportRead> ReadZip(Stream content, CancellationToken cancellationToken)
    {
        var collector = new Collector();

        try
        {
            await using var archive = new ZipArchive(content, ZipArchiveMode.Read, leaveOpen: true);

            foreach (var entry in archive.Entries)
            {
                var isDirectory = entry.FullName.EndsWith('/');

                if (isDirectory)
                {
                    continue;
                }

                await using var entryStream = await entry.OpenAsync(cancellationToken);

                var error = await collector.Add(entry.FullName, entryStream, entry.Length, cancellationToken);

                if (error is not null)
                {
                    return collector.Fail(error);
                }
            }
        }
        catch (InvalidDataException)
        {
            return collector.Fail("The zip file could not be read.");
        }

        return collector.Finish();
    }

    private static bool HasExtension(string fileName, string extension)
    {
        return string.Equals(Path.GetExtension(fileName), extension, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class Collector
    {
        private readonly List<PageImportFile> Files = [];
        private readonly List<string> Skipped = [];
        private long TotalBytes;

        public async Task<string?> Add(string path, Stream content, long? declaredLength, CancellationToken cancellationToken)
        {
            var segments = PageImportPlan.Segments(path);
            var isHidden = segments.Count == 0 || segments.Any(segment => segment.StartsWith('.') || segment == "__MACOSX");

            if (isHidden)
            {
                return null;
            }

            var isMarkdown = MarkdownExtensions.Contains(Path.GetExtension(path));

            if (!isMarkdown)
            {
                Skipped.Add($"{path}: not a markdown file");
                return null;
            }

            var isTooLarge = declaredLength > PageBody.MaxBytes;

            if (isTooLarge)
            {
                Skipped.Add($"{path}: larger than 1 MB");
                return null;
            }

            if (Files.Count >= MaxFiles)
            {
                return $"An import can hold at most {MaxFiles} files.";
            }

            var text = await ReadBounded(content, cancellationToken);

            if (text is null)
            {
                Skipped.Add($"{path}: larger than 1 MB");
                return null;
            }

            TotalBytes += Encoding.UTF8.GetByteCount(text);

            if (TotalBytes > MaxTotalBytes)
            {
                return "The files add up to more than 30 MB.";
            }

            Files.Add(new PageImportFile(path, text));

            return null;
        }

        public PageImportRead Finish()
        {
            return new PageImportRead { Files = Files, Skipped = Skipped };
        }

        public PageImportRead Fail(string error)
        {
            return new PageImportRead { Skipped = Skipped, Error = error };
        }

        // A declared size can lie, so reading stops one byte past the limit whatever the header said.
        private static async Task<string?> ReadBounded(Stream content, CancellationToken cancellationToken)
        {
            var buffer = new MemoryStream();
            var chunk = new byte[81920];
            var limit = PageBody.MaxBytes + 1;

            while (buffer.Length < limit)
            {
                var read = await content.ReadAsync(chunk.AsMemory(0, (int)Math.Min(chunk.Length, limit - buffer.Length)), cancellationToken);

                if (read == 0)
                {
                    break;
                }

                buffer.Write(chunk, 0, read);
            }

            if (buffer.Length > PageBody.MaxBytes)
            {
                return null;
            }

            buffer.Position = 0;

            using var reader = new StreamReader(buffer, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

            return await reader.ReadToEndAsync(cancellationToken);
        }
    }
}
