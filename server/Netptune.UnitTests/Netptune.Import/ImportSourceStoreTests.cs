using FluentAssertions;

using Netptune.Core.Responses;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Storage;
using Netptune.Import;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Import;

public class ImportSourceStoreTests
{
    [Fact]
    public async Task Save_ShouldNotPutAnUploadCalledCommentsWhereTheSidecarIsRead()
    {
        var storage = Substitute.For<IStorageService>();
        var upload = ClientResponse<UploadResponse>.Success(new UploadResponse());

        storage.UploadFileAsync(Arg.Any<Stream>(), Arg.Any<StorageUploadOptions>(), Arg.Any<CancellationToken>())
            .Returns(upload);

        var store = new ImportSourceStore(storage);
        var storageKey = await store.Save(
            "acme",
            Guid.NewGuid(),
            ImportSourceStore.CommentsFileName,
            new MemoryStream(),
            "application/x-ndjson",
            TestContext.Current.CancellationToken);

        storageKey.Should().EndWith($"/{ImportSourceStore.CommentsFileName}");
        ImportSourceStore.CommentsKey(storageKey).Should().NotBe(storageKey);
    }

    [Fact]
    public async Task Save_ShouldDropAnyFolderTheUploadNameCarries()
    {
        var storage = Substitute.For<IStorageService>();
        var upload = ClientResponse<UploadResponse>.Success(new UploadResponse());

        storage.UploadFileAsync(Arg.Any<Stream>(), Arg.Any<StorageUploadOptions>(), Arg.Any<CancellationToken>())
            .Returns(upload);

        var store = new ImportSourceStore(storage);
        var publicId = Guid.NewGuid();
        var storageKey = await store.Save(
            "acme",
            publicId,
            "sidecar/comments.ndjson",
            new MemoryStream(),
            "application/x-ndjson",
            TestContext.Current.CancellationToken);

        storageKey.Should().EndWith($"{publicId:N}/comments.ndjson");
        ImportSourceStore.CommentsKey(storageKey).Should().NotBe(storageKey);
    }

    [Fact]
    public void CommentsKey_ShouldSitInTheSidecarFolderBesideTheRowsFile()
    {
        var commentsKey = ImportSourceStore.CommentsKey("imports/acme/abc/rows.csv");

        commentsKey.Should().Be("imports/acme/abc/sidecar/comments.ndjson");
    }
}
