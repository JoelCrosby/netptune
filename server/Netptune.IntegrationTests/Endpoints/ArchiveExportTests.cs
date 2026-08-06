using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using Netptune.Transfer.Services;
using Netptune.Transfer;
using Netptune.Transfer.Archive;
using Netptune.Transfer.Catalog;
using Netptune.Transfer.Export;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

[Collection(WorkspaceMutationCollection.Name)]
public sealed class ArchiveExportTests
{
    private readonly NetptuneFixture Fixture;

    public ArchiveExportTests(NetptuneFixture fixture)
    {
        Fixture = fixture;
    }

    [Fact]
    public async Task Archive_ShouldContainOneNdjsonEntryPerRecordTypeAndAManifest()
    {
        using var archive = await BuildArchive(new ExportOptionsModel());
        var entries = archive.Entries.Select(entry => entry.FullName).ToList();

        entries.Should().Contain(ArchiveManifest.FileName);
        entries.Should().Contain("data/workspace.ndjson");
        entries.Should().Contain("data/projects.ndjson");
        entries.Should().Contain("data/statuses.ndjson");
    }

    [Fact]
    public async Task Archive_ShouldRecordAnAccurateChecksumAndCountForEveryEntry()
    {
        using var archive = await BuildArchive(new ExportOptionsModel());
        var manifest = await ReadManifest(archive);

        manifest.SchemaVersion.Should().Be(ArchiveManifest.CurrentSchemaVersion);
        manifest.Contents.Should().NotBeEmpty();

        foreach (var content in manifest.Contents)
        {
            var entry = archive.GetEntry(content.File);

            entry.Should().NotBeNull($"{content.File} is declared in the manifest");

            await using var stream = entry.Open();
            using var memory = new MemoryStream();

            await stream.CopyToAsync(memory, TestContext.Current.CancellationToken);

            var bytes = memory.ToArray();
            var checksum = Convert.ToHexStringLower(SHA256.HashData(bytes));
            var lines = System.Text.Encoding.UTF8.GetString(bytes)
                .Split('\n', StringSplitOptions.RemoveEmptyEntries);

            checksum.Should().Be(content.Sha256);
            lines.Length.Should().Be((int)content.Count);
        }
    }

    [Fact]
    public async Task Archive_ShouldWriteEntriesInTheDeclaredDependencyOrder()
    {
        using var archive = await BuildArchive(new ExportOptionsModel());
        var manifest = await ReadManifest(archive);
        var expectedOrder = ArchiveCatalog.InDependencyOrder
            .Select(definition => definition.FileName)
            .ToList();
        var actualOrder = manifest.Contents.Select(content => content.File).ToList();
        var positions = actualOrder.Select(file => expectedOrder.IndexOf(file)).ToList();

        actualOrder.Should().BeSubsetOf(expectedOrder);
        positions.Should().NotContain(-1);
        positions.Should().BeInAscendingOrder(
            "the import applier walks manifest.contents in order and refuses an archive that violates the dependency graph");
    }

    [Fact]
    public async Task Archive_ShouldOmitMembersAndHistoryUnlessTheyAreOptedIn()
    {
        using var withoutOptIns = await BuildArchive(new ExportOptionsModel());
        var lean = await ReadManifest(withoutOptIns);

        lean.Contents.Should().NotContain(content => content.File == "data/members.ndjson");
        lean.Contents.Should().NotContain(content => content.File == "data/events.ndjson");
        lean.Scope.IncludeMembers.Should().BeFalse();
        lean.Scope.IncludeHistory.Should().BeFalse();

        using var withOptIns = await BuildArchive(new ExportOptionsModel
        {
            IncludeMembers = true,
            IncludeHistory = true,
        });
        var full = await ReadManifest(withOptIns);

        full.Contents.Should().Contain(content => content.File == "data/members.ndjson");
        full.Contents.Should().Contain(content => content.File == "data/events.ndjson");
    }

    [Fact]
    public async Task Archive_ShouldNameEveryRecordByItsEntityRefAndNeverByADatabaseId()
    {
        using var archive = await BuildArchive(new ExportOptionsModel());
        var entry = archive.GetEntry("data/projects.ndjson");

        entry.Should().NotBeNull();

        await using var stream = entry.Open();
        using var reader = new StreamReader(stream);

        var line = await reader.ReadLineAsync(TestContext.Current.CancellationToken);

        line.Should().NotBeNull();

        var document = JsonDocument.Parse(line);

        document.RootElement.GetProperty("ref").GetString().Should().StartWith($"{EntityRefTypes.Project}:");
        document.RootElement.TryGetProperty("id", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Archive_ShouldListEveryRedactionKeyItEnforces()
    {
        using var archive = await BuildArchive(new ExportOptionsModel());
        var manifest = await ReadManifest(archive);

        manifest.Redactions.Should().BeEquivalentTo(TransferRedaction.RedactionKeys);
        manifest.Redactions.Should().Contain(TransferRedactionKeys.AiCredentials);
        manifest.Redactions.Should().Contain(TransferRedactionKeys.UserAccounts);
    }

    private async Task<ZipArchive> BuildArchive(ExportOptionsModel options)
    {
        await using var scope = Fixture.Services.CreateAsyncScope();

        var exporter = scope.ServiceProvider.GetRequiredService<IArchiveExporter>();
        var request = new ArchiveExportRequest
        {
            WorkspaceId = 1,
            WorkspaceSlug = "netptune",
            Options = options,
        };
        var result = await exporter.Write(request, (_, _) => Task.CompletedTask, TestContext.Current.CancellationToken);
        var buffer = new MemoryStream();

        await result.Content.CopyToAsync(buffer, TestContext.Current.CancellationToken);

        buffer.Seek(0, SeekOrigin.Begin);

        return new ZipArchive(buffer, ZipArchiveMode.Read);
    }

    private static async Task<ArchiveManifest> ReadManifest(ZipArchive archive)
    {
        var entry = archive.GetEntry(ArchiveManifest.FileName);

        entry.Should().NotBeNull();

        await using var stream = entry.Open();
        var manifest = await JsonSerializer.DeserializeAsync<ArchiveManifest>(
            stream,
            Core.Encoding.JsonOptions.Default,
            TestContext.Current.CancellationToken);

        manifest.Should().NotBeNull();

        return manifest;
    }
}
