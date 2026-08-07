using Netptune.Transfer.Enums;
using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Netptune.Core.Extensions;
using Netptune.Transfer.Services;
using Netptune.Core.Responses.Common;
using Netptune.Transfer;
using Netptune.Transfer.Mapping;
using Netptune.Transfer.ViewModels;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

[Collection(WorkspaceMutationCollection.Name)]
public sealed class ImportEndpointTests
{
    private readonly HttpClient Client;

    public ImportEndpointTests(NetptuneFixture fixture)
    {
        Client = fixture.CreateNetptuneClient();
    }

    [Fact]
    public async Task Upload_ShouldOpenASessionAndInspectTheFile()
    {
        var session = await Upload();

        session.OriginalName.Should().Be("import.csv");
        session.TargetBoardIdentifier.Should().Be("neovim");

        var profile = await Inspect(session.PublicId);

        profile.Delimiter.Should().Be(',');
        profile.HasHeaderRow.Should().BeTrue();
        profile.Columns.Select(column => column.Name)
            .Should().Equal("Name", "assignees", "group", "due");
        profile.EstimatedRowCount.Should().Be(2, "the header row is not an importable row");
    }

    [Fact]
    public async Task Preview_ShouldCountRowsAndReportUnknownAssignees()
    {
        var session = await Upload();

        await Inspect(session.PublicId);
        await SetMapping(session.PublicId);

        var response = await Client.PostAsync($"api/import/sessions/{session.PublicId}/preview", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<ImportPreviewResult>>();
        var preview = result.Payload!;

        preview.TotalRows.Should().Be(2);
        preview.WillCreate.Should().Be(2);
        preview.SampleRows.Should().HaveCount(2);
        preview.UsersToInvite.Should().Contain("nobody@example.com");
        preview.Diagnostics.Should().Contain(diagnostic => diagnostic.Code == ImportDiagnosticCodes.UnresolvedUser);
    }

    [Fact]
    public async Task Preview_ShouldFlagARowWhoseDateCannotBeParsed()
    {
        var import = """
            Name,due
            bad schedule,not-a-date
            """;
        var session = await Upload(import);

        await Inspect(session.PublicId);
        await SetMapping(session.PublicId, includeAssignees: false, includeGroup: false);

        var response = await Client.PostAsync($"api/import/sessions/{session.PublicId}/preview", null);
        var result = await response.Content.ReadFromJsonAsync<ClientResponse<ImportPreviewResult>>();
        var preview = result.Payload!;

        preview.WillError.Should().Be(1);
        preview.Diagnostics.Should().Contain(diagnostic => diagnostic.Code == ImportDiagnosticCodes.InvalidDate);
    }

    [Fact]
    public async Task Preview_ShouldWarnWhenAPriorityCannotBeParsed()
    {
        // A priority that does not match the enum used to be dropped in silence, unlike an unparseable
        // date or number, so the field just arrived empty with nothing to explain it.
        var import = """
            Name,priority
            urgent work,Highest
            """;
        var session = await Upload(import);

        await Inspect(session.PublicId);
        await SetPriorityMapping(session.PublicId);

        var response = await Client.PostAsync($"api/import/sessions/{session.PublicId}/preview", null);
        var result = await response.Content.ReadFromJsonAsync<ClientResponse<ImportPreviewResult>>();
        var preview = result.Payload!;

        preview.WillCreate.Should().Be(1, "an unusable priority leaves the field empty rather than failing the row");
        preview.Diagnostics.Should().Contain(diagnostic => diagnostic.Code == ImportDiagnosticCodes.InvalidPriority);
    }

    private async Task SetPriorityMapping(Guid publicId)
    {
        var mapping = new
        {
            recordType = EntityRefTypes.Task,
            bindings = new List<object>
            {
                new { fieldKey = TaskFieldKeys.Name, columnIndex = 0 },
                new { fieldKey = TaskFieldKeys.Priority, columnIndex = 1 },
            },
        };
        var response = await Client.PutAsJsonAsync($"api/import/sessions/{publicId}/mapping", mapping);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task State_ShouldReturnEverythingTheWizardNeedsToResume()
    {
        var session = await Upload();

        await Inspect(session.PublicId);
        await SetMapping(session.PublicId);
        await Client.PostAsync($"api/import/sessions/{session.PublicId}/preview", null);

        var response = await Client.GetAsync($"api/import/sessions/{session.PublicId}/state");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<ImportSessionStateViewModel>>();
        var state = result.Payload!;

        state.Session.Stage.Should().Be(ImportStage.Previewed);
        state.Session.TargetBoardIdentifier.Should().Be("neovim");
        state.SourceProfile!.Columns.Select(column => column.Name)
            .Should().Equal("Name", "assignees", "group", "due");
        state.Mapping!.Bindings.Should().Contain(binding => binding.FieldKey == TaskFieldKeys.Name);
        state.PreviewResult!.TotalRows.Should().Be(2, "the stored preview is replayed rather than recomputed");
        state.PreviewResult.WillCreate.Should().Be(2);
    }

    [Fact]
    public async Task State_ShouldOmitTheMappingAndPreviewOfASessionThatHasOnlyBeenUploaded()
    {
        var session = await Upload();

        var response = await Client.GetAsync($"api/import/sessions/{session.PublicId}/state");
        var result = await response.Content.ReadFromJsonAsync<ClientResponse<ImportSessionStateViewModel>>();
        var state = result.Payload!;

        state.Session.Stage.Should().Be(ImportStage.Uploaded);
        state.SourceProfile.Should().BeNull();
        state.Mapping.Should().BeNull();
        state.PreviewResult.Should().BeNull();
    }

    [Fact]
    public async Task Delete_ShouldRemoveTheSession()
    {
        var session = await Upload();
        var response = await Client.DeleteAsync($"api/import/sessions/{session.PublicId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var state = await Client.GetAsync($"api/import/sessions/{session.PublicId}/state");

        state.StatusCode.Should().Be(HttpStatusCode.NotFound, "the session is gone");

        var listed = await Client.GetAsync("api/import/sessions?pageSize=100&page=1");
        var sessions = await listed.Content.ReadFromJsonAsync<ClientResponse<PagedResponse<ImportSessionViewModel>>>();

        sessions.Payload!.Items.Should().NotContain(item => item.PublicId == session.PublicId);
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenTheSessionDoesNotExist()
    {
        var response = await Client.DeleteAsync($"api/import/sessions/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task State_ShouldNotFindASessionThatDoesNotExist()
    {
        var response = await Client.GetAsync($"api/import/sessions/{Guid.NewGuid()}/state");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Suggest_ShouldPreFillAMappingFromTheHeaders()
    {
        var session = await Upload();

        await Inspect(session.PublicId);

        var response = await Client.PostAsync($"api/import/sessions/{session.PublicId}/suggest", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<ImportMappingSuggestion>>();
        var suggestion = result.Payload!;

        suggestion.Mapping.Bindings.Select(binding => binding.FieldKey)
            .Should().Contain([TaskFieldKeys.Name, TaskFieldKeys.Assignees, TaskFieldKeys.DueDate]);
        suggestion.Mapping.Bindings.Should().OnlyContain(binding => binding.Confidence >= 0.55);
    }

    [Fact]
    public async Task Suggest_ShouldRecogniseAJiraExportAndFoldItsRepeatedHeaders()
    {
        var import = string.Join('\n',
            "Issue key,Summary,Status,Assignee,Story Points,Labels,Labels",
            "PROJ-1,Fix the fan-out,In Progress,nobody@example.com,3,backend,urgent");
        var session = await Upload(import);

        await Inspect(session.PublicId);

        var response = await Client.PostAsync($"api/import/sessions/{session.PublicId}/suggest", null);
        var result = await response.Content.ReadFromJsonAsync<ClientResponse<ImportMappingSuggestion>>();
        var suggestion = result.Payload!;

        suggestion.Vendor.Should().Be(ImportVendorProfile.Jira);
        suggestion.Mapping.Bindings.Single(binding => binding.FieldKey == TaskFieldKeys.Tags)
            .AdditionalColumnIndexes.Should().ContainSingle();
    }

    [Fact]
    public async Task SetMapping_ShouldRejectAMappingWithoutTheRequiredFields()
    {
        var session = await Upload();

        await Inspect(session.PublicId);

        var mapping = new
        {
            recordType = EntityRefTypes.Task,
            bindings = new[] { new { fieldKey = TaskFieldKeys.Description, columnIndex = 0 } },
        };
        var response = await Client.PutAsJsonAsync($"api/import/sessions/{session.PublicId}/mapping", mapping);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<ImportSessionViewModel> Upload(string? content = null)
    {
        var import = content ?? """
            Name,assignees,group,due
            imported task,nobody@example.com,Imported,2030-01-05
            second task,nobody@example.com,Imported,2030-02-05
            """;
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri("api/import/sessions?boardIdentifier=neovim", UriKind.RelativeOrAbsolute),
            Content = new MultipartFormDataContent
            {
                { new StreamContent(import.Trim().ToStream()), "file", "import.csv" },
            },
        };
        var response = await Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<ImportSessionViewModel>>();

        return result.Payload!;
    }

    private async Task<ImportSourceProfile> Inspect(Guid publicId)
    {
        var response = await Client.PostAsync($"api/import/sessions/{publicId}/inspect", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<ImportSourceProfile>>();

        return result.Payload!;
    }

    private async Task SetMapping(Guid publicId, bool includeAssignees = true, bool includeGroup = true)
    {
        var bindings = new List<object>
        {
            new { fieldKey = TaskFieldKeys.Name, columnIndex = 0 },
        };

        if (includeAssignees)
        {
            bindings.Add(new { fieldKey = TaskFieldKeys.Assignees, columnIndex = 1 });
        }

        if (includeGroup)
        {
            bindings.Add(new { fieldKey = TaskFieldKeys.BoardGroup, columnIndex = 2 });
        }

        bindings.Add(new { fieldKey = TaskFieldKeys.DueDate, columnIndex = includeGroup ? 3 : 1 });

        var mapping = new
        {
            recordType = EntityRefTypes.Task,
            bindings,
        };
        var response = await Client.PutAsJsonAsync($"api/import/sessions/{publicId}/mapping", mapping);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
