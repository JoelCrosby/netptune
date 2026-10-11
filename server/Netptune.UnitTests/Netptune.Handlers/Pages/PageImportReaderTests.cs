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

    [Fact]
    public async Task Read_ShouldCountOversizeReadsTowardTheTotal_WhenTheirLengthIsNotDeclared()
    {
        var oversize = PageBody.MaxBytes + 10;
        var uploadCount = (int)(PageImportReader.MaxTotalBytes / PageBody.MaxBytes) + 1;
        var uploads = Enumerable.Range(0, uploadCount)
            .Select(index => new PageImportUpload($"big-{index}.md", new UnseekableStream(oversize)))
            .ToList();

        var read = await PageImportReader.Read(uploads, TestContext.Current.CancellationToken);

        read.Error.Should().Be("The files add up to more than 30 MB.");
    }

    [Fact]
    public async Task Read_ShouldRefuse_AZipWithMoreEntriesThanItExamines()
    {
        var entries = Enumerable.Range(0, PageImportReader.MaxEntries + 1)
            .Select(index => ($"folder-{index}/", ""))
            .ToArray();
        using var zip = CreateZip(entries);

        var read = await PageImportReader.Read([new PageImportUpload("many.zip", zip)], TestContext.Current.CancellationToken);

        read.Error.Should().Be($"A zip file can hold at most {PageImportReader.MaxEntries} entries.");
    }

    [Fact]
    public async Task Read_ShouldListTheFirstSkippedFiles_AndCountTheRest()
    {
        var unlisted = 10;
        var entries = Enumerable.Range(0, PageImportReader.MaxSkippedListed + unlisted)
            .Select(index => ($"image-{index:D3}.png", "png"))
            .ToArray();
        using var zip = CreateZip(entries);

        var read = await PageImportReader.Read([new PageImportUpload("images.zip", zip)], TestContext.Current.CancellationToken);

        read.Skipped.Should().HaveCount(PageImportReader.MaxSkippedListed + 1);
        read.Skipped[0].Should().StartWith("image-000.png");
        read.Skipped[^1].Should().Be($"and {unlisted} more");
    }

    [Fact]
    public async Task Read_ShouldReportAnEncryptedZipAsUnreadable()
    {
        using var zip = CreateZip(("secret.md", "# Secret"));

        MarkEncrypted(zip);

        var read = await PageImportReader.Read([new PageImportUpload("secret.zip", zip)], TestContext.Current.CancellationToken);

        read.Error.Should().Be(PageImportReader.UnreadableZipMessage);
    }

    private static void MarkEncrypted(MemoryStream zip)
    {
        const int encryptedFlag = 0x1;
        const int localHeaderFlagOffset = 6;
        const int centralHeaderFlagOffset = 8;
        var bytes = zip.GetBuffer();
        var length = (int)zip.Length;

        for (var offset = 0; offset < length - 4; offset++)
        {
            var signature = BitConverter.ToUInt32(bytes, offset);

            if (signature == 0x04034b50)
            {
                bytes[offset + localHeaderFlagOffset] |= encryptedFlag;
            }
            else if (signature == 0x02014b50)
            {
                bytes[offset + centralHeaderFlagOffset] |= encryptedFlag;
            }
        }
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

    private sealed class UnseekableStream(long length) : Stream
    {
        private long Remaining = length;

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = (int)Math.Min(count, Remaining);

            buffer.AsSpan(offset, read).Fill((byte)'a');
            Remaining -= read;

            return read;
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
