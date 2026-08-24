using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Boards;
using Netptune.Core.ViewModels.Branding;
using Netptune.Core.ViewModels.Files;
using Netptune.Core.ViewModels.Projects;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

public sealed class BrandingEndpointTests
{
    private const string WorkspaceLogoUrl = "api/workspaces/branding/logo";

    private readonly HttpClient Client;

    public BrandingEndpointTests(NetptuneFixture fixture)
    {
        Client = fixture.CreateNetptuneClient();
    }

    [Fact]
    public async Task WorkspaceLogo_ShouldUploadRemoveAndTrackStorageUsage()
    {
        var before = await GetUsedBytes();

        var image = await Upload(WorkspaceLogoUrl, "logo.png", [1, 2, 3, 4, 5]);

        image.FileId.Should().NotBeNullOrWhiteSpace();
        image.SizeBytes.Should().Be(5);
        image.ContentUrl.Should().Be($"/api/workspaces/netptune/files/{image.FileId}/content?disposition=inline");

        var workspace = await Client.GetFromJsonAsync<Workspace>("api/workspaces/netptune");

        workspace!.MetaInfo!.LogoFileId.Should().Be(image.FileId);
        (await GetUsedBytes()).Should().Be(before + 5);

        var removeResponse = await Client.DeleteAsync(WorkspaceLogoUrl);

        removeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var cleared = await Client.GetFromJsonAsync<Workspace>("api/workspaces/netptune");

        cleared!.MetaInfo!.LogoFileId.Should().BeNull();
        (await GetUsedBytes()).Should().Be(before);
    }

    [Fact]
    public async Task WorkspaceLogo_ShouldReleaseThePreviousImage_WhenReplaced()
    {
        var before = await GetUsedBytes();

        await Upload(WorkspaceLogoUrl, "first.png", [1, 2, 3, 4]);
        var replacement = await Upload(WorkspaceLogoUrl, "second.png", [1, 2, 3, 4, 5, 6]);

        var workspace = await Client.GetFromJsonAsync<Workspace>("api/workspaces/netptune");

        workspace!.MetaInfo!.LogoFileId.Should().Be(replacement.FileId);
        (await GetUsedBytes()).Should().Be(before + 6);

        await Client.DeleteAsync(WorkspaceLogoUrl);

        (await GetUsedBytes()).Should().Be(before);
    }

    [Fact]
    public async Task Upload_ShouldFail_WhenTheFileIsNotASupportedImage()
    {
        var response = await Post(WorkspaceLogoUrl, "notes.pdf", "application/pdf", [1, 2, 3]);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<BrandingImageViewModel>>();

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task BoardBranding_ShouldPersistTheLogoAndBackgroundSeparately()
    {
        var board = await CreateBoard();

        var logo = await Upload($"api/boards/{board.Id}/branding/logo", "board-logo.png", [1, 2, 3]);
        var background = await Upload($"api/boards/{board.Id}/branding/background", "board-bg.png", [4, 5, 6, 7]);

        var updated = await GetBoard(board.Id);

        updated.MetaInfo!.LogoFileId.Should().Be(logo.FileId);
        updated.MetaInfo.BackgroundFileId.Should().Be(background.FileId);

        var removeResponse = await Client.DeleteAsync($"api/boards/{board.Id}/branding/background");

        removeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var cleared = await GetBoard(board.Id);

        cleared.MetaInfo!.BackgroundFileId.Should().BeNull();
        cleared.MetaInfo.LogoFileId.Should().Be(logo.FileId);
    }

    [Fact]
    public async Task ProjectLogo_ShouldPersistAgainstTheProject()
    {
        var image = await Upload("api/projects/1/branding/logo", "project-logo.png", [9, 9, 9]);

        var project = await Client.GetFromJsonAsync<ProjectViewModel>("api/projects/neo");

        project!.LogoFileId.Should().Be(image.FileId);

        var removeResponse = await Client.DeleteAsync("api/projects/1/branding/logo");

        removeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var cleared = await Client.GetFromJsonAsync<ProjectViewModel>("api/projects/neo");

        cleared!.LogoFileId.Should().BeNull();
    }

    [Fact]
    public async Task BoardBranding_ShouldSurvive_WhenTheBoardIsSavedWithOnlyItsColour()
    {
        var board = await CreateBoard();
        var logo = await Upload($"api/boards/{board.Id}/branding/logo", "kept-logo.png", [1, 2, 3]);
        var background = await Upload($"api/boards/{board.Id}/branding/background", "kept-bg.png", [4, 5, 6]);

        var saveResponse = await Client.PutAsJsonAsync("api/boards", new UpdateBoardRequest
        {
            Id = board.Id,
            Name = "Renamed board",
            Meta = new()
            {
                Color = "blue",
            },
        });

        saveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var saved = await GetBoard(board.Id);

        saved.MetaInfo!.Color.Should().Be("blue");
        saved.MetaInfo.LogoFileId.Should().Be(logo.FileId);
        saved.MetaInfo.BackgroundFileId.Should().Be(background.FileId);
    }

    [Fact]
    public async Task WorkspaceLogo_ShouldSurvive_WhenWorkspaceDetailsAreSaved()
    {
        var logo = await Upload(WorkspaceLogoUrl, "kept-workspace-logo.png", [7, 8, 9]);

        var saveResponse = await Client.PutAsJsonAsync("api/workspaces", new UpdateWorkspaceRequest
        {
            Slug = "netptune",
            Name = "Netptune",
            MetaInfo = new()
            {
                Color = "green",
                TimeZone = "Europe/London",
            },
        });

        saveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var saved = await Client.GetFromJsonAsync<Workspace>("api/workspaces/netptune");

        saved!.MetaInfo!.Color.Should().Be("green");
        saved.MetaInfo.LogoFileId.Should().Be(logo.FileId);

        await Client.DeleteAsync(WorkspaceLogoUrl);
    }

    [Fact]
    public async Task BoardBranding_ShouldKeepBoth_WhenTheLogoAndBackgroundAreUploadedTogether()
    {
        var board = await CreateBoard();

        var logoTask = Post($"api/boards/{board.Id}/branding/logo", "both-logo.png", "image/png", [1, 2, 3]);
        var backgroundTask = Post($"api/boards/{board.Id}/branding/background", "both-bg.png", "image/png", [4, 5, 6]);

        var responses = await Task.WhenAll(logoTask, backgroundTask);

        foreach (var response in responses)
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        }

        var logo = await ReadImage(responses[0]);
        var background = await ReadImage(responses[1]);
        var saved = await GetBoard(board.Id);

        saved.MetaInfo!.LogoFileId.Should().Be(logo.FileId);
        saved.MetaInfo.BackgroundFileId.Should().Be(background.FileId);
    }

    [Fact]
    public async Task Upload_ShouldReturnNotFound_WhenTheTargetDoesNotExist()
    {
        var response = await Post("api/boards/1000/branding/logo", "logo.png", "image/png", [1]);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<BrandingImageViewModel> Upload(string url, string fileName, byte[] content)
    {
        var response = await Post(url, fileName, "image/png", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        return await ReadImage(response);
    }

    private static async Task<BrandingImageViewModel> ReadImage(HttpResponseMessage response)
    {
        var result = await response.Content.ReadFromJsonAsync<ClientResponse<BrandingImageViewModel>>();

        result.IsSuccess.Should().BeTrue();

        return result.Payload!;
    }

    private Task<HttpResponseMessage> Post(string url, string fileName, string contentType, byte[] content)
    {
        var body = new ByteArrayContent(content);

        body.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        var form = new MultipartFormDataContent
        {
            { body, "image", fileName },
        };

        return Client.PostAsync(url, form);
    }

    private async Task<BoardViewModel> GetBoard(int id)
    {
        var response = await Client.GetFromJsonAsync<ClientResponse<BoardViewModel>>($"api/boards/{id}");

        return response.Payload!;
    }

    private async Task<long> GetUsedBytes()
    {
        var usage = await Client.GetFromJsonAsync<ClientResponse<WorkspaceStorageUsageViewModel>>("api/storage/usage");

        return usage.Payload!.UsedBytes;
    }

    private async Task<BoardViewModel> CreateBoard()
    {
        var identifier = $"branding-target-{Guid.NewGuid():N}"[..24];
        var response = await Client.PostAsJsonAsync("api/boards", new AddBoardRequest
        {
            Name = "Branding target",
            Identifier = identifier,
            ProjectId = 1,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<BoardViewModel>>();

        return result.Payload!;
    }
}
