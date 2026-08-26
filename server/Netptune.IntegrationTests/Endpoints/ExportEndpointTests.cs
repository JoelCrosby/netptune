using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Constants;
using Netptune.Core.Relationships;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Storage;
using Netptune.Core.UnitOfWork;
using Netptune.Transfer;
using Netptune.Transfer.Definitions;
using Netptune.Transfer.Entities;
using Netptune.Transfer.Enums;
using Netptune.Transfer.Repositories;
using Netptune.Transfer.Services;
using Netptune.Transfer.ViewModels;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

[Collection(WorkspaceMutationCollection.Name)]
public sealed class ExportEndpointTests
{
    private readonly HttpClient Client;
    private readonly NetptuneFixture Fixture;

    public ExportEndpointTests(NetptuneFixture fixture)
    {
        Client = fixture.CreateNetptuneClient();
        Fixture = fixture;
    }

    [Fact]
    public async Task RunInline_ShouldStreamCsv_WhenTheDefinitionIsValid()
    {
        var response = await Client.PostAsJsonAsync("api/export/run", new
        {
            definition = TaskDefinition(ExportFormat.Csv),
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        response.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
        response.Content.Headers.ContentLength!.Value.Should().BeGreaterThan(0);

        response.Content.Headers.ContentDisposition!.FileName
            .Should()
            .MatchRegex(@"Netptune-task-Export_.*-.{0,16}\.csv");
    }

    [Fact]
    public async Task RunInline_ShouldScopeTheExport_WhenABoardFilterIsGiven()
    {
        var definition = TaskDefinition(ExportFormat.Csv) with
        {
            Filter = new ExportFilterModel { BoardIdentifiers = ["does-not-exist"] },
        };
        var response = await Client.PostAsJsonAsync("api/export/run", new { definition });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();

        content.Split('\n', StringSplitOptions.RemoveEmptyEntries).Should().HaveCount(1);
    }

    [Fact]
    public async Task RunInline_ShouldFindATask_WhenTheBoardFilterMatchesAnyOfItsPlacements()
    {
        // A task can sit in groups on more than one board, and the filter matches against every
        // placement rather than a single one.
        var placement = await PlaceATaskOnTwoBoards();
        var content = await RunScopedToBoard(placement.SecondBoardIdentifier);
        var row = FindRow(content, placement.SystemId);

        row.Should().NotBeNull("the task is on the board asked for, even though it was placed elsewhere first");
        row.Should().Contain(placement.SecondBoardGroupRef, "the group reported should be the one on that board");
    }

    [Fact]
    public async Task RunInline_ShouldReportTheEarliestPlacement_WhenNoBoardFilterNarrowsIt()
    {
        var placement = await PlaceATaskOnTwoBoards();
        var content = await RunScopedToBoard(placement.FirstBoardIdentifier);
        var row = FindRow(content, placement.SystemId);

        row.Should().NotBeNull();
        row.Should().Contain(placement.FirstBoardGroupRef);
    }

    private async Task<string> RunScopedToBoard(string boardIdentifier)
    {
        var definition = TaskDefinition(ExportFormat.Csv) with
        {
            Fields = [TaskFieldKeys.SystemId, TaskFieldKeys.BoardGroup],
            Filter = new ExportFilterModel { BoardIdentifiers = [boardIdentifier] },
        };
        var response = await Client.PostAsJsonAsync("api/export/run", new { definition });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return await response.Content.ReadAsStringAsync();
    }

    private static string? FindRow(string content, string systemId)
    {
        return content
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(line => line.StartsWith(systemId, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<PlacedTask> PlaceATaskOnTwoBoards()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();

        var unitOfWork = scope.ServiceProvider.GetRequiredService<INetptuneUnitOfWork>();
        var token = TestContext.Current.CancellationToken;
        // Seeded projects alternate between the two workspaces, so neovim and emacs are the pair of
        // boards that live together in workspace one.
        var first = await unitOfWork.Boards.GetByIdentifier("neovim", 1, cancellationToken: token);
        var second = await unitOfWork.Boards.GetByIdentifier("emacs", 1, cancellationToken: token);
        var firstGroup = (await unitOfWork.BoardGroups.GetBoardGroupsInBoard(first!.Id, cancellationToken: token))[0];
        var secondGroup = (await unitOfWork.BoardGroups.GetBoardGroupsInBoard(second!.Id, cancellationToken: token))[0];
        var tasks = await unitOfWork.Tasks.GetAllInWorkspace(1, cancellationToken: token);
        var task = tasks.First(candidate => candidate.ProjectId == first.ProjectId);
        var project = await unitOfWork.Projects.GetAsync(first.ProjectId, cancellationToken: token);

        // Saved one at a time so the placement on the first board is unambiguously the earlier of the
        // two, which is the placement an unfiltered export reports.
        await Place(unitOfWork, task.Id, firstGroup.Id, token);
        await Place(unitOfWork, task.Id, secondGroup.Id, token);

        return new PlacedTask(
            EntityRefBuilder.ForTask(project!.Key, task.ProjectScopeId).Value,
            first.Identifier,
            EntityRefBuilder.ForBoardGroup(first.Identifier, firstGroup.Name).Value,
            second.Identifier,
            EntityRefBuilder.ForBoardGroup(second.Identifier, secondGroup.Name).Value);
    }

    private static async Task Place(INetptuneUnitOfWork unitOfWork, int taskId, int boardGroupId, CancellationToken token)
    {
        var existing = await unitOfWork.ProjectTasksInGroups.GetProjectTaskInGroup(taskId, boardGroupId, token);

        if (existing is not null)
        {
            return;
        }

        await unitOfWork.ProjectTasksInGroups.AddAsync(new ProjectTaskInBoardGroup
        {
            ProjectTaskId = taskId,
            BoardGroupId = boardGroupId,
            SortOrder = 1,
        }, token);

        await unitOfWork.CompleteAsync(token);
    }

    private sealed record PlacedTask(
        string SystemId,
        string FirstBoardIdentifier,
        string FirstBoardGroupRef,
        string SecondBoardIdentifier,
        string SecondBoardGroupRef);

    [Fact]
    public async Task RunInline_ShouldReject_WhenTheDefinitionIsInvalid()
    {
        var response = await Client.PostAsJsonAsync("api/export/run", new
        {
            definition = new { recordType = "unicorn", format = ExportFormat.Csv },
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Preview_ShouldReturnHeadersRowsAndAnEstimate()
    {
        var response = await Client.PostAsJsonAsync("api/export/preview", new
        {
            definition = TaskDefinition(ExportFormat.Csv),
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<ExportPreviewResult>>();
        var preview = result.Payload!;

        preview.Headers.Should().NotBeEmpty();
        preview.FieldKeys.Should().Contain(TaskFieldKeys.Name);
        preview.CanRunInline.Should().BeTrue();
        preview.Rows.Should().OnlyContain(row => row.Count == preview.Headers.Count);
    }

    [Fact]
    public async Task Preview_ShouldResolveEveryRequestedField_NotJustTheSingleWordColumns()
    {
        // get_transfer_tasks.sql returns snake_case columns. Dapper matches case-insensitively but does
        // not bridge underscores, so mapping straight onto a PascalCase row type empties every multi-word
        // column without failing — the preview keeps its shape and loses project, status and the dates.
        var definition = TaskDefinition(ExportFormat.Csv) with
        {
            Fields =
            [
                TaskFieldKeys.SystemId,
                TaskFieldKeys.Name,
                TaskFieldKeys.Status,
                TaskFieldKeys.Project,
                TaskFieldKeys.DueDate,
            ],
        };
        var response = await Client.PostAsJsonAsync("api/export/preview", new { definition });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<ExportPreviewResult>>();
        var preview = result.Payload!;

        preview.Rows.Should().NotBeEmpty("the seeded workspace has tasks to preview");

        var keys = preview.FieldKeys.ToList();
        var systemIds = preview.Rows.Select(row => row[keys.IndexOf(TaskFieldKeys.SystemId)]);
        var projects = preview.Rows.Select(row => row[keys.IndexOf(TaskFieldKeys.Project)]);
        var statuses = preview.Rows.Select(row => row[keys.IndexOf(TaskFieldKeys.Status)]);

        // A task ref is "<projectKey>-<scopeId>", so an unmapped project key and scope id read as "-0".
        systemIds.Should().OnlyContain(value => value.Length > 0 && value != "-0");
        projects.Should().OnlyContain(value => value.Length > 0);
        statuses.Should().OnlyContain(value => value.Length > 0);
    }

    [Fact]
    public async Task PreviewRows_ShouldPageKeyedRows_ForTheDatatable()
    {
        var fields = $"fields={TaskFieldKeys.SystemId}&fields={TaskFieldKeys.Name}&fields={TaskFieldKeys.Status}";
        var response = await Client.GetAsync($"api/export/preview/rows?recordType=task&{fields}&page=1&pageSize=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content
            .ReadFromJsonAsync<ClientResponse<PagedResponse<ExportPreviewRow>>>();
        var page = result.Payload!;

        page.Items.Should().HaveCount(2, "the page size was two");
        page.TotalCount.Should().BeGreaterThan(2, "the seeded workspace has more tasks than one page");
        page.Items.Should().OnlyContain(row => row.Ref.Length > 0);
        page.Items.Should().OnlyContain(row => row.Values[TaskFieldKeys.Status].Length > 0);
        page.Items.Should().OnlyContain(row => row.Values[TaskFieldKeys.SystemId] != "-0");
    }

    [Fact]
    public async Task PreviewRows_ShouldMoveOnToTheNextRows_WhenThePageAdvances()
    {
        const string fields = "fields=task.system_id&fields=task.name";
        var first = await Client.GetAsync($"api/export/preview/rows?recordType=task&{fields}&page=1&pageSize=3");
        var second = await Client.GetAsync($"api/export/preview/rows?recordType=task&{fields}&page=2&pageSize=3");

        var firstPage = (await first.Content.ReadFromJsonAsync<ClientResponse<PagedResponse<ExportPreviewRow>>>()).Payload!;
        var secondPage = (await second.Content.ReadFromJsonAsync<ClientResponse<PagedResponse<ExportPreviewRow>>>()).Payload!;

        secondPage.Items.Should().NotBeEmpty();
        secondPage.Items.Select(row => row.Ref).Should()
            .NotIntersectWith(firstPage.Items.Select(row => row.Ref), "each page holds different rows");
    }

    [Fact]
    public async Task PreviewRows_ShouldRejectAFieldThatIsNotInTheCatalog()
    {
        var response = await Client.GetAsync("api/export/preview/rows?recordType=task&fields=task.not_a_field");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Download_ShouldReturnTheSignedUrl_RatherThanRedirectingToIt()
    {
        // A redirect can only be followed by a top level navigation, and a navigation cannot carry the
        // workspace header the permission check needs, so redirecting would make every download a 403.
        var publicId = await SeedCompletedJob();
        var response = await Client.GetAsync($"api/export/jobs/{publicId}/download");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<string>>();

        result.Payload.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Download_ShouldReturnNotFound_WhenTheJobDoesNotExist()
    {
        var response = await Client.GetAsync($"api/export/jobs/{Guid.NewGuid()}/download");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ShouldRemoveTheJobAndItsArtefact()
    {
        var publicId = await SeedCompletedJob();
        var response = await Client.DeleteAsync($"api/export/jobs/{publicId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var download = await Client.GetAsync($"api/export/jobs/{publicId}/download");

        download.StatusCode.Should().Be(HttpStatusCode.NotFound, "the job is gone");

        var listed = await Client.GetAsync("api/export/jobs?pageSize=100&page=1");
        var jobs = await listed.Content.ReadFromJsonAsync<ClientResponse<PagedResponse<ExportJobViewModel>>>();

        jobs.Payload!.Items.Should().NotContain(job => job.PublicId == publicId);
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenTheJobDoesNotExist()
    {
        var response = await Client.DeleteAsync($"api/export/jobs/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<Guid> SeedCompletedJob()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();

        var unitOfWork = scope.ServiceProvider.GetRequiredService<INetptuneUnitOfWork>();
        var exportJobs = scope.ServiceProvider.GetRequiredService<IExportJobRepository>();
        var storage = scope.ServiceProvider.GetRequiredService<IStorageService>();
        var owner = await unitOfWork.Workspaces.GetAsync(1, cancellationToken: TestContext.Current.CancellationToken);
        var key = $"exports/{Guid.NewGuid():N}.csv";

        await storage.UploadFileAsync(
            new MemoryStream("name\nfirst"u8.ToArray()),
            new StorageUploadOptions
            {
                Name = "export.csv",
                Key = key,
                ContentType = "text/csv",
            },
            TestContext.Current.CancellationToken);

        var job = await exportJobs.AddAsync(new ExportJob
        {
            WorkspaceId = 1,
            Status = ExportJobStatus.Succeeded,
            RecordType = EntityRefTypes.Task,
            Format = ExportFormat.Csv,
            Definition = JsonSerializer.SerializeToDocument(TaskDefinition(ExportFormat.Csv)),
            RequestedBy = owner!.OwnerId!,
            StorageKey = key,
            FileName = "export.csv",
            ContentType = "text/csv",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            CompletedAt = DateTime.UtcNow,
        }, TestContext.Current.CancellationToken);

        await unitOfWork.CompleteAsync(TestContext.Current.CancellationToken);

        return job.PublicId;
    }

    [Fact]
    public async Task Definitions_ShouldRoundTripThroughSaveListAndDelete()
    {
        var save = await Client.PostAsJsonAsync("api/export/definitions", new
        {
            name = "Monthly task report",
            isShared = true,
            definition = TaskDefinition(ExportFormat.Csv),
        });

        save.StatusCode.Should().Be(HttpStatusCode.OK);

        var saved = await save.Content.ReadFromJsonAsync<ClientResponse<ExportDefinitionViewModel>>();
        var id = saved.Payload!.Id;

        id.Should().BeGreaterThan(0);

        var list = await Client.GetFromJsonAsync<List<ExportDefinitionViewModel>>("api/export/definitions");

        list.Should().ContainSingle(definition => definition.Id == id);

        var delete = await Client.DeleteAsync($"api/export/definitions/{id}");

        delete.StatusCode.Should().Be(HttpStatusCode.OK);

        var listAfterDelete = await Client.GetFromJsonAsync<List<ExportDefinitionViewModel>>("api/export/definitions");

        listAfterDelete.Should().NotContain(definition => definition.Id == id);
    }

    private static ExportDefinitionModel TaskDefinition(ExportFormat format)
    {
        return new ExportDefinitionModel
        {
            RecordType = EntityRefTypes.Task,
            Format = format,
        };
    }
}
