using Netptune.Transfer.Repositories;
using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Responses.Common;
using Netptune.Transfer.Services;
using Netptune.Transfer;
using Netptune.Transfer.Archive;
using Netptune.Transfer.Catalog;
using Netptune.Transfer.Export;
using Netptune.Core.UnitOfWork;
using Netptune.Transfer.ViewModels;
using Netptune.Import.Archive;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

// §11's load-bearing test: export a workspace, clone-import it into a fresh one, and assert the two
// graphs are equal under EntityRef — never under database id, which is the whole point of the
// natural keys.
[Collection(WorkspaceMutationCollection.Name)]
public sealed class ArchiveRoundTripTests
{
    private readonly NetptuneFixture Fixture;
    private readonly HttpClient Client;

    public ArchiveRoundTripTests(NetptuneFixture fixture)
    {
        Fixture = fixture;
        Client = fixture.CreateNetptuneClient();
    }

    [Fact]
    public async Task RoundTrip_ShouldRebuildTheSameGraphUnderEntityRef()
    {
        var slug = Slug();
        var archive = await ExportArchive(1);
        var result = await CloneInto(archive, slug);

        result.WorkspaceSlug.Should().Be(slug);
        result.WorkspaceId.Should().BeGreaterThan(0);

        // Compare archive against archive rather than against the live source workspace: both sides
        // then come from the same snapshot, so a concurrent test mutating workspace 1 cannot make a
        // correct round trip look wrong.
        var reExported = await ExportArchive(result.WorkspaceId, slug);
        var source = await RefsByType(archive);
        var restored = await RefsByType(reExported);

        foreach (var type in RoundTrippedTypes)
        {
            var expected = source.GetValueOrDefault(type, []);
            var actual = restored.GetValueOrDefault(type, []);

            expected.Should().NotBeEmpty($"the seeded workspace should contain at least one {type}");
            actual.Should().BeEquivalentTo(expected, $"every {type} should survive the round trip by ref");
        }
    }

    [Fact]
    public async Task RoundTrip_ShouldCarryTasksAndTheirLinks()
    {
        var slug = Slug();
        var archive = await ExportArchive(1);
        var result = await CloneInto(archive, slug);

        result.CreatedByType.Should().ContainKey(TransferRecordTypes.Task)
            .WhoseValue.Should().BeGreaterThan(0, "an archive without tasks cannot rebuild a workspace");

        await using var scope = Fixture.Services.CreateAsyncScope();

        var archives = scope.ServiceProvider.GetRequiredService<IArchiveRepository>();
        var placements = await Count(archives.ReadTaskPlacements(result.WorkspaceId));
        var assignees = await Count(archives.ReadTaskAssignees(result.WorkspaceId));
        var sourcePlacements = await Count(archives.ReadTaskPlacements(1));
        var sourceAssignees = await Count(archives.ReadTaskAssignees(1));

        placements.Should().Be(sourcePlacements);
        assignees.Should().Be(sourceAssignees);
    }

    [Fact]
    public async Task Clone_ShouldRefuseASlugThatIsAlreadyTaken()
    {
        var archive = await ExportArchive(1);
        var import = () => CloneInto(archive, "netptune");

        await import.Should().ThrowAsync<ArchiveSchemaException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task Restore_ShouldRefuseAWorkspaceThatAlreadyHasProjects()
    {
        var archive = await ExportArchive(1);

        await using var scope = Fixture.Services.CreateAsyncScope();

        var importer = scope.ServiceProvider.GetRequiredService<IArchiveImporter>();
        var request = new ArchiveImportRequest
        {
            Archive = archive,
            UserId = await OwnerId(),
            Mode = ArchiveImportMode.Restore,
            WorkspaceId = 1,
        };
        var import = () => importer.Import(request, TestContext.Current.CancellationToken);

        await import.Should().ThrowAsync<ArchiveSchemaException>()
            .WithMessage("*empty workspace*");
    }

    [Fact]
    public async Task Preview_ShouldReportPerTypeCountsWithoutWritingAnything()
    {
        var archive = await ExportArchive(1);

        await using var scope = Fixture.Services.CreateAsyncScope();

        var importer = scope.ServiceProvider.GetRequiredService<IArchiveImporter>();
        var archives = scope.ServiceProvider.GetRequiredService<IArchiveRepository>();
        var before = await Count(archives.ReadTasks(1));
        var preview = await importer.Preview(new ArchiveImportRequest
        {
            Archive = archive,
            UserId = await OwnerId(),
            Mode = ArchiveImportMode.Clone,
            TargetSlug = $"preview-{Guid.NewGuid():N}"[..20],
        }, TestContext.Current.CancellationToken);
        var after = await Count(archives.ReadTasks(1));

        preview.Manifest.SchemaVersion.Should().Be(ArchiveManifest.CurrentSchemaVersion);
        preview.CountsByType.Should().ContainKey(TransferRecordTypes.Task);
        preview.Blockers.Should().BeEmpty();
        after.Should().Be(before, "a preview never writes");
    }

    [Fact]
    public void SchemaUpgrader_RefusesAnArchiveFromANewerBuild()
    {
        var manifest = Manifest(ArchiveManifest.CurrentSchemaVersion + 1);
        var upgrade = () => ArchiveSchemaUpgrader.Upgrade(manifest);

        upgrade.Should().Throw<ArchiveSchemaException>().WithMessage("*newer version*");
    }

    [Fact]
    public void SchemaUpgrader_PassesACurrentArchiveThroughUnchanged()
    {
        var result = ArchiveSchemaUpgrader.Upgrade(Manifest(ArchiveManifest.CurrentSchemaVersion));

        result.WasUpgraded.Should().BeFalse();
        result.Applied.Should().BeEmpty();
        result.ToVersion.Should().Be(ArchiveManifest.CurrentSchemaVersion);
    }

    [Fact]
    public async Task ArchivePreviewEndpoint_ShouldReportWhatTheArchiveHolds()
    {
        var archive = await ExportArchive(1);
        var response = await PostArchive("api/import/archive/preview?mode=clone&targetSlug=" + Slug(), archive);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<ArchiveImportPreviewViewModel>>();

        result.Payload!.SchemaVersion.Should().Be(ArchiveManifest.CurrentSchemaVersion);
        result.Payload.WorkspaceSlug.Should().Be("netptune");
        result.Payload.CountsByType.Should().ContainKey(TransferRecordTypes.Task);
    }

    [Fact]
    public async Task ArchiveImportEndpoint_ShouldCloneIntoTheRequestedSlug()
    {
        var slug = Slug();
        var archive = await ExportArchive(1);
        var response = await PostArchive($"api/import/archive?mode=clone&targetSlug={slug}", archive);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<ArchiveImportResultViewModel>>();

        result.Payload!.WorkspaceSlug.Should().Be(slug);
        result.Payload.CreatedByType.Should().ContainKey(TransferRecordTypes.Task);
    }

    [Fact]
    public async Task ArchiveImportEndpoint_ShouldRejectAFileThatIsNotAnArchive()
    {
        var response = await PostArchive(
            $"api/import/archive?mode=clone&targetSlug={Slug()}",
            new MemoryStream("not a zip"u8.ToArray()));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ArchiveImportEndpoint_ShouldRequireASlugWhenCloning()
    {
        var archive = await ExportArchive(1);
        var response = await PostArchive("api/import/archive?mode=clone", archive);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static string Slug()
    {
        return $"round-trip-{Guid.NewGuid():N}"[..24];
    }

    private async Task<HttpResponseMessage> PostArchive(string url, Stream archive)
    {
        archive.Seek(0, SeekOrigin.Begin);

        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri(url, UriKind.RelativeOrAbsolute),
            Content = new MultipartFormDataContent
            {
                { new StreamContent(archive), "file", "workspace.nptz" },
            },
        };

        return await Client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private static readonly string[] RoundTrippedTypes =
    [
        TransferRecordTypes.Status,
        TransferRecordTypes.Tag,
        TransferRecordTypes.RelationType,
        TransferRecordTypes.Project,
        TransferRecordTypes.Board,
        TransferRecordTypes.BoardGroup,
        TransferRecordTypes.Sprint,
        TransferRecordTypes.Task,
    ];

    private static ArchiveManifest Manifest(int schemaVersion)
    {
        return new ArchiveManifest
        {
            SchemaVersion = schemaVersion,
            CreatedAt = new DateTime(2026, 8, 4, 0, 0, 0, DateTimeKind.Utc),
            Workspace = new ArchiveWorkspace { Slug = "acme", Name = "Acme" },
            Scope = new ArchiveScope(),
        };
    }

    // Every ref an archive declares, grouped by record type.
    private static async Task<Dictionary<string, List<string>>> RefsByType(Stream archive)
    {
        archive.Seek(0, SeekOrigin.Begin);

        using var reader = new ArchiveReader(archive);
        var refs = new Dictionary<string, List<string>>();

        foreach (var definition in ArchiveCatalog.InDependencyOrder)
        {
            var section = new List<string>();

            await foreach (var row in reader.ReadSection(definition.FileName, TestContext.Current.CancellationToken))
            {
                section.Add(row.Ref.ToString());
            }

            refs[definition.Key] = section;
        }

        return refs;
    }

    private static async Task<int> Count<TEntity>(IAsyncEnumerable<TEntity> entities)
    {
        var count = 0;

        await foreach (var _ in entities)
        {
            count++;
        }

        return count;
    }

    private async Task<MemoryStream> ExportArchive(int workspaceId, string slug = "netptune")
    {
        await using var scope = Fixture.Services.CreateAsyncScope();

        var exporter = scope.ServiceProvider.GetRequiredService<IArchiveExporter>();
        var result = await exporter.Write(new ArchiveExportRequest
        {
            WorkspaceId = workspaceId,
            WorkspaceSlug = slug,
            Options = new ExportOptionsModel { IncludeMembers = true },
        }, (_, _) => Task.CompletedTask, TestContext.Current.CancellationToken);
        var buffer = new MemoryStream();

        await result.Content.CopyToAsync(buffer, TestContext.Current.CancellationToken);

        buffer.Seek(0, SeekOrigin.Begin);

        return buffer;
    }

    private async Task<ArchiveImportResult> CloneInto(Stream archive, string slug)
    {
        archive.Seek(0, SeekOrigin.Begin);

        await using var scope = Fixture.Services.CreateAsyncScope();

        var importer = scope.ServiceProvider.GetRequiredService<IArchiveImporter>();

        return await importer.Import(new ArchiveImportRequest
        {
            Archive = archive,
            UserId = await OwnerId(),
            Mode = ArchiveImportMode.Clone,
            TargetSlug = slug,
        }, TestContext.Current.CancellationToken);
    }

    private async Task<string> OwnerId()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();

        var unitOfWork = scope.ServiceProvider.GetRequiredService<INetptuneUnitOfWork>();
        var workspace = await unitOfWork.Workspaces.GetAsync(1, cancellationToken: TestContext.Current.CancellationToken);

        return workspace!.OwnerId!;
    }
}
