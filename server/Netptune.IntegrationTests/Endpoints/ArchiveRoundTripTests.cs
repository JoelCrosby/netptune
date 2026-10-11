using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Enums;
using Netptune.Core.Requests;
using Netptune.Core.Requests.Pages;
using Netptune.Core.Responses.Common;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Pages;
using Netptune.Core.ViewModels.Projects;
using Netptune.Core.ViewModels.Tags;
using Netptune.Entities.Contexts;
using Netptune.Import.Archive;
using Netptune.Transfer;
using Netptune.Transfer.Archive;
using Netptune.Transfer.Catalog;
using Netptune.Transfer.Definitions;
using Netptune.Transfer.Repositories;
using Netptune.Transfer.Services;
using Netptune.Transfer.ViewModels;

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
        result.TaskIds.Should().HaveCount(result.CreatedByType[TransferRecordTypes.Task], "every imported task is handed to search");
        result.ProjectIds.Should().HaveCount(result.CreatedByType[TransferRecordTypes.Project], "every imported project is handed to search");

        await using var scope = Fixture.Services.CreateAsyncScope();

        var archives = scope.ServiceProvider.GetRequiredService<IArchiveRepository>();
        var placements = await Count(archives.ReadTaskPlacements(result.WorkspaceId));
        var assignees = await Count(archives.ReadTaskAssignees(result.WorkspaceId));

        // Counted from the archive rather than from workspace 1 as it stands now, for the reason
        // the round trip above compares two archives: a concurrent test adding a task to the source
        // moves the live count out from under a clone that was built before it.
        var source = await RefsByType(archive);

        placements.Should().Be(source[TransferRecordTypes.TaskPlacement].Count);
        assignees.Should().Be(source[TransferRecordTypes.TaskAssignee].Count);
    }

    [Fact]
    public async Task RoundTrip_ShouldCarryEveryExportedLink_WhenAProjectWithTasksWasDeleted()
    {
        var project = await CreateProject();

        await CreateTask(project.Id);

        var deleted = await Client.DeleteAsync($"api/projects/{project.Id}", TestContext.Current.CancellationToken);

        deleted.EnsureSuccessStatusCode();

        var slug = Slug();
        var archive = await ExportArchive(1);
        var result = await CloneInto(archive, slug);

        await using var scope = Fixture.Services.CreateAsyncScope();

        var archives = scope.ServiceProvider.GetRequiredService<IArchiveRepository>();
        var placements = await Count(archives.ReadTaskPlacements(result.WorkspaceId));
        var assignees = await Count(archives.ReadTaskAssignees(result.WorkspaceId));
        var source = await RefsByType(archive);

        placements.Should().Be(source[TransferRecordTypes.TaskPlacement].Count);
        assignees.Should().Be(source[TransferRecordTypes.TaskAssignee].Count);
    }

    [Fact]
    public async Task ArchiveImportEndpoint_ShouldCarryPagesWithTheirTreeTagsHistoryAndMentions()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var marker = Guid.NewGuid().ToString("N")[..8];
        var folder = await CreatePage(new CreatePageRequest { Kind = PageKind.Folder, Title = $"Archive folder {marker}" });
        var sibling = await CreatePage(new CreatePageRequest { Kind = PageKind.Document, Title = $"Archive sibling {marker}" });
        var child = await CreatePage(new CreatePageRequest { Kind = PageKind.Document, Title = $"Archive child {marker}", ParentId = folder.Id });
        var liveTask = await AnyLiveTask();
        var taskSystemId = liveTask.SystemId;
        var body = $"See [[page:{sibling.Id}|Sibling]] and [[task:{taskSystemId}]] in [[project:{liveTask.ProjectId}|Project]]";
        var saved = await Client.PutAsJsonAsync($"api/pages/{child.Id}", new SavePageRequest { Title = child.Title, Body = body, Revision = child.Revision }, cancellationToken);
        var tags = await Client.GetFromJsonAsync<List<TagViewModel>>("api/tags/workspace", cancellationToken);
        var tagged = await Client.PatchAsJsonAsync($"api/pages/{child.Id}", new UpdatePageRequest { TagIds = [tags![0].Id] }, cancellationToken);

        saved.EnsureSuccessStatusCode();
        tagged.EnsureSuccessStatusCode();

        var slug = Slug();
        var archive = await ExportArchive(1, includeHistory: true);
        var response = await PostArchive($"api/import/archive?mode=clone&targetSlug={slug}", archive);
        var result = await response.Content.ReadFromJsonAsync<ClientResponse<ArchiveImportResultViewModel>>(cancellationToken);
        var workspaceId = result!.Payload!.WorkspaceId;

        await using var scope = Fixture.Services.CreateAsyncScope();

        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var pages = await context.Pages
            .AsNoTracking()
            .Include(page => page.PageTags)
            .Where(page => page.WorkspaceId == workspaceId && page.Title.EndsWith(marker))
            .ToListAsync(cancellationToken);
        var clonedFolder = pages.Single(page => page.Title == folder.Title);
        var clonedSibling = pages.Single(page => page.Title == sibling.Title);
        var clonedChild = pages.Single(page => page.Title == child.Title);
        var childVersions = await context.PageVersions.CountAsync(version => version.PageId == clonedChild.Id, cancellationToken);
        var childLinks = await context.PageLinks.AsNoTracking().Where(link => link.PageId == clonedChild.Id).ToListAsync(cancellationToken);
        var clonedProject = await context.Projects.AsNoTracking().SingleAsync(project => project.WorkspaceId == workspaceId && project.Key == liveTask.ProjectKey, cancellationToken);

        clonedChild.ParentId.Should().Be(clonedFolder.Id);
        clonedChild.Body.Should().Be($"See [[page:{clonedSibling.Id}|Sibling]] and [[task:{taskSystemId}]] in [[project:{clonedProject.Id}|Project]]");
        clonedChild.PageTags.Should().ContainSingle();
        childVersions.Should().Be(2);
        childLinks.Should().Contain(link => link.TargetType == PageLinkTargetType.Page && link.TargetId == clonedSibling.Id.ToString());
        childLinks.Should().Contain(link => link.TargetType == PageLinkTargetType.Task);
    }

    [Fact]
    public async Task Clone_ShouldGiveEachPageOneVersion_AndDropADeletedProject_WhenHistoryIsLeftOut()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var marker = Guid.NewGuid().ToString("N")[..8];
        var project = await CreateProject();
        var page = await CreatePage(new CreatePageRequest { Kind = PageKind.Document, Title = $"Project page {marker}", ProjectId = project.Id });
        var deleted = await Client.DeleteAsync($"api/projects/{project.Id}", cancellationToken);

        deleted.EnsureSuccessStatusCode();

        var archive = await ExportArchive(1);
        var result = await CloneInto(archive, Slug());

        await using var scope = Fixture.Services.CreateAsyncScope();

        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var cloned = await context.Pages.AsNoTracking().SingleAsync(item => item.WorkspaceId == result.WorkspaceId && item.Title == page.Title, cancellationToken);
        var revisionsByPage = await context.PageVersions
            .AsNoTracking()
            .Where(version => version.Page!.WorkspaceId == result.WorkspaceId)
            .GroupBy(version => version.PageId)
            .Select(group => new { Count = group.Count(), Revision = group.Max(version => version.Revision), Current = group.Max(version => version.Page!.Revision) })
            .ToListAsync(cancellationToken);

        cloned.ProjectId.Should().BeNull();
        revisionsByPage.Should().NotBeEmpty();
        revisionsByPage.Should().OnlyContain(item => item.Count == 1 && item.Revision == item.Current);
    }

    [Fact]
    public async Task Restore_ShouldRefuseLivePages_ButNotArchivedOnes()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await CreatePage(new CreatePageRequest { Kind = PageKind.Document, Title = $"Restore page {Guid.NewGuid():N}" });

        var archive = await ExportArchive(1);
        var target = await CloneInto(archive, Slug());

        await using var scope = Fixture.Services.CreateAsyncScope();

        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        await context.Projects
            .Where(project => project.WorkspaceId == target.WorkspaceId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(project => project.IsDeleted, true), cancellationToken);

        var withLivePages = await PreviewRestore(archive, target.WorkspaceId);

        await context.Pages
            .Where(page => page.WorkspaceId == target.WorkspaceId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(page => page.IsDeleted, true), cancellationToken);

        var withArchivedPages = await PreviewRestore(archive, target.WorkspaceId);

        withLivePages.Blockers.Should().ContainSingle(blocker => blocker.Contains("already has pages"));
        withArchivedPages.Blockers.Should().BeEmpty();
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
    public void SchemaUpgrader_UpgradesAVersionOneArchive_WhichHasNoPages()
    {
        var result = ArchiveSchemaUpgrader.Upgrade(Manifest(1));

        result.WasUpgraded.Should().BeTrue();
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

    private async Task<MemoryStream> ExportArchive(int workspaceId, string slug = "netptune", bool includeHistory = false)
    {
        await using var scope = Fixture.Services.CreateAsyncScope();

        var exporter = scope.ServiceProvider.GetRequiredService<IArchiveExporter>();
        var result = await exporter.Write(new ArchiveExportRequest
        {
            WorkspaceId = workspaceId,
            WorkspaceSlug = slug,
            Options = new ExportOptionsModel { IncludeMembers = true, IncludeHistory = includeHistory },
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

    private async Task<ArchiveImportPreview> PreviewRestore(Stream archive, int workspaceId)
    {
        archive.Seek(0, SeekOrigin.Begin);

        await using var scope = Fixture.Services.CreateAsyncScope();

        var importer = scope.ServiceProvider.GetRequiredService<IArchiveImporter>();

        return await importer.Preview(new ArchiveImportRequest
        {
            Archive = archive,
            UserId = await OwnerId(),
            Mode = ArchiveImportMode.Restore,
            WorkspaceId = workspaceId,
        }, TestContext.Current.CancellationToken);
    }

    private async Task<ProjectViewModel> CreateProject()
    {
        var response = await Client.PostAsJsonAsync("api/projects", new AddProjectRequest
        {
            Name = $"{Guid.NewGuid():N} Archive Test",
            Description = "Project deleted before an archive round trip",
            MetaInfo = new() { Color = "blue" },
        }, TestContext.Current.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ClientResponse<ProjectViewModel>>(TestContext.Current.CancellationToken);

        return result!.Payload!;
    }

    private async Task CreateTask(int projectId)
    {
        var response = await Client.PostAsJsonAsync("api/tasks", new AddProjectTaskRequest
        {
            Name = $"Archive task {Guid.NewGuid():N}",
            ProjectId = projectId,
        }, TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
    }

    private async Task<PageViewModel> CreatePage(CreatePageRequest request)
    {
        var response = await Client.PostAsJsonAsync("api/pages", request, TestContext.Current.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ClientResponse<PageViewModel>>(TestContext.Current.CancellationToken);

        return result!.Payload!;
    }

    private sealed record LiveTask(string SystemId, int ProjectId, string ProjectKey);

    private async Task<LiveTask> AnyLiveTask()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();

        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var task = await context.ProjectTasks
            .AsNoTracking()
            .Include(item => item.Project)
            .Where(item => item.WorkspaceId == 1 && !item.IsDeleted && !item.Project!.IsDeleted)
            .OrderBy(item => item.Id)
            .FirstAsync(TestContext.Current.CancellationToken);

        return new LiveTask($"{task.Project!.Key}-{task.ProjectScopeId}", task.Project.Id, task.Project.Key);
    }

    private async Task<string> OwnerId()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();

        var unitOfWork = scope.ServiceProvider.GetRequiredService<INetptuneUnitOfWork>();
        var workspace = await unitOfWork.Workspaces.GetAsync(1, cancellationToken: TestContext.Current.CancellationToken);

        return workspace!.OwnerId!;
    }
}
