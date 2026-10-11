using System.IO.Compression;
using System.Text;

using FluentAssertions;

using Netptune.Core.Models.Pages;
using Netptune.Handlers.Pages;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Pages;

public class PageImportReaderTests
{
    [Fact]
    public async Task Read_ShouldTakeTheMarkdownFromAZip_AndSkipEverythingElse()
    {
        using var zip = CreateZip(
            ("docs/readme.md", "# Readme"),
            ("docs/logo.png", "png"),
            ("__MACOSX/docs/._readme.md", "junk"),
            (".hidden/notes.md", "secret"));

        var read = await PageImportReader.Read([new PageImportUpload("export.zip", zip)], TestContext.Current.CancellationToken);

        read.Error.Should().BeNull();
        read.Files.Select(file => file.Path).Should().Equal("docs/readme.md");
        read.Skipped.Should().ContainSingle(skipped => skipped.StartsWith("docs/logo.png"));
    }

    [Fact]
    public async Task Read_ShouldSkipAFileOverTheBodyLimit_WithoutReadingAllOfIt()
    {
        var large = new MemoryStream(Encoding.UTF8.GetBytes(new string('a', PageBody.MaxBytes + 10)));

        var read = await PageImportReader.Read([new PageImportUpload("big.md", large)], TestContext.Current.CancellationToken);

        read.Files.Should().BeEmpty();
        read.Skipped.Should().ContainSingle(skipped => skipped.StartsWith("big.md"));
    }

    [Fact]
    public async Task Read_ShouldRefuse_MoreFilesThanOneImportHolds()
    {
        var entries = Enumerable.Range(0, PageImportReader.MaxFiles + 1)
            .Select(index => ($"page-{index}.md", "x"))
            .ToArray();
        using var zip = CreateZip(entries);

        var read = await PageImportReader.Read([new PageImportUpload("many.zip", zip)], TestContext.Current.CancellationToken);

        read.Error.Should().Contain(PageImportReader.MaxFiles.ToString());
        read.Files.Should().BeEmpty();
    }

    [Fact]
    public async Task Read_ShouldReportAZipThatIsNotOne()
    {
        var notZip = new MemoryStream(Encoding.UTF8.GetBytes("not a zip"));

        var read = await PageImportReader.Read([new PageImportUpload("broken.zip", notZip)], TestContext.Current.CancellationToken);

        read.Error.Should().Be("The zip file could not be read.");
    }

    private static MemoryStream CreateZip(params (string Path, string Content)[] entries)
    {
        var stream = new MemoryStream();

        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (path, content) in entries)
            {
                var entry = archive.CreateEntry(path);

                using var writer = new StreamWriter(entry.Open());

                writer.Write(content);
            }
        }

        stream.Position = 0;

        return stream;
    }
}
