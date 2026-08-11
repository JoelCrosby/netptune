using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Netptune.Core.Responses;
using Netptune.Core.Responses.Common;
using Netptune.Core.Storage;
using Netptune.Core.ViewModels.Files;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Entities.Contexts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

[Collection(UserMutationCollection.Name)]
public sealed class StorageEndpointTests
{
    private readonly HttpClient Client;
    private readonly NetptuneFixture Fixture;

    public StorageEndpointTests(NetptuneFixture fixture)
    {
        Fixture = fixture;
        Client = fixture.CreateNetptuneClient();
    }

    [Fact]
    public async Task UploadProfilePicture_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new ("api/storage/profile-picture", UriKind.RelativeOrAbsolute),
            Content = new MultipartFormDataContent
            {
                { new StreamContent(Stream.Null), "file", "picture.png" },
            },
        };

        var response = await Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<UploadResponse>>();

        result.IsSuccess.Should().BeTrue();
    }
    [Fact]
    public async Task UploadMedia_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new ("api/storage/media", UriKind.RelativeOrAbsolute),
            Content = new MultipartFormDataContent
            {
                { new ByteArrayContent([1, 2, 3]), "file", "media.jpg" },
            },
        };

        var response = await Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<UploadResponse>>();

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Uri.Should().StartWith("/api/workspaces/netptune/files/");
    }

    [Fact]
    public async Task TaskFile_ShouldUploadListRedirectAndDelete_WhileUpdatingUsage()
    {
        var tasksResponse = await Client.GetFromJsonAsync<ClientResponse<PagedResponse<TaskViewModel>>>("api/tasks?pageSize=1");
        var systemId = tasksResponse.Payload!.Items.Single().SystemId;
        var before = await Client.GetFromJsonAsync<ClientResponse<WorkspaceStorageUsageViewModel>>("api/storage/usage");

        using var content = new MultipartFormDataContent();

        content.Add(new ByteArrayContent([10, 20, 30, 40]), "files", "evidence.pdf");
        content.First().Headers.ContentType = new("application/pdf");
        var uploadResponse = await Client.PostAsync($"api/tasks/{systemId}/files", content);
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var uploaded = await uploadResponse.Content.ReadFromJsonAsync<ClientResponse<FileUploadResult>>();
        uploaded.Payload.Should().Match<FileUploadResult>(item => item.IsSuccess && item.File!.OriginalName == "evidence.pdf");
        var file = uploaded.Payload!.File!;

        var list = await Client.GetFromJsonAsync<ClientResponse<IReadOnlyList<WorkspaceFileViewModel>>>($"api/tasks/{systemId}/files");
        list.Payload.Should().Contain(item => item.Id == file.Id);
        file.ContentUrl.Should().MatchRegex("^/api/workspaces/netptune/files/[A-Za-z0-9]{12}/content$");

        var noRedirectClient = Fixture.CreateNetptuneClient(new() { AllowAutoRedirect = false });
        noRedirectClient.DefaultRequestHeaders.Remove("workspace");
        var contentResponse = await noRedirectClient.GetAsync($"{file.ContentUrl}?disposition=inline");
        contentResponse.StatusCode.Should().Be(HttpStatusCode.Redirect);
        contentResponse.Headers.Location!.Host.Should().Be("storage.test");

        var after = await Client.GetFromJsonAsync<ClientResponse<WorkspaceStorageUsageViewModel>>("api/storage/usage");
        after.Payload!.UsedBytes.Should().Be(before.Payload!.UsedBytes + 4);

        var deleteResponse = await Client.DeleteAsync($"api/tasks/{systemId}/files/{file.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var final = await Client.GetFromJsonAsync<ClientResponse<WorkspaceStorageUsageViewModel>>("api/storage/usage");
        final.Payload!.UsedBytes.Should().Be(before.Payload.UsedBytes);

        using var deletedScope = Fixture.CreateScope();
        var deletedContext = deletedScope.ServiceProvider.GetRequiredService<DataContext>();
        (await deletedContext.TaskFiles.AnyAsync(link => link.WorkspaceFileId == file.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task GetFiles_ShouldListWorkspaceFiles_AndFilterByTask()
    {
        var tasksResponse = await Client.GetFromJsonAsync<ClientResponse<PagedResponse<TaskViewModel>>>("api/tasks?pageSize=1");
        var systemId = tasksResponse.Payload!.Items.Single().SystemId;
        var file = await UploadTaskFile(systemId, "listed.pdf");

        var response = await Client.GetAsync("api/storage/files?page=1&pageSize=100");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<PagedResponse<WorkspaceFileViewModel>>>();

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Items.Should().Contain(item => item.Id == file.Id);

        var filtered = await Client.GetFromJsonAsync<ClientResponse<PagedResponse<WorkspaceFileViewModel>>>(
            $"api/storage/files?taskSystemId={systemId}&pageSize=100");

        filtered.Payload!.Items.Should().Contain(item => item.Id == file.Id);
        filtered.Payload.Items.Should().OnlyContain(item => item.TaskSystemId == systemId);
    }

    [Fact]
    public async Task DeleteFile_ShouldReleaseQuota_AndRemoveTheFileFromTheListing()
    {
        var tasksResponse = await Client.GetFromJsonAsync<ClientResponse<PagedResponse<TaskViewModel>>>("api/tasks?pageSize=1");
        var systemId = tasksResponse.Payload!.Items.Single().SystemId;
        var before = await Client.GetFromJsonAsync<ClientResponse<WorkspaceStorageUsageViewModel>>("api/storage/usage");
        var file = await UploadTaskFile(systemId, "deleted.pdf");

        var response = await Client.DeleteAsync($"api/storage/files/{file.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var listing = await Client.GetFromJsonAsync<ClientResponse<PagedResponse<WorkspaceFileViewModel>>>(
            "api/storage/files?page=1&pageSize=100");

        listing.Payload!.Items.Should().NotContain(item => item.Id == file.Id);

        var after = await Client.GetFromJsonAsync<ClientResponse<WorkspaceStorageUsageViewModel>>("api/storage/usage");

        after.Payload!.UsedBytes.Should().Be(before.Payload!.UsedBytes);
    }

    [Fact]
    public async Task DeleteFile_ShouldReturnNotFound_WhenFileDoesNotExist()
    {
        var response = await Client.DeleteAsync("api/storage/files/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TaskFile_ShouldReturnPerFileQuotaFailure_WithoutIncreasingUsage()
    {
        using var scope = Fixture.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var workspace = await context.Workspaces.SingleAsync(item => item.Slug == "netptune");
        var originalLimit = workspace.StorageLimitBytes;

        workspace.StorageLimitBytes = workspace.StorageUsedBytes;

        await context.SaveChangesAsync();

        try
        {
            var tasksResponse = await Client.GetFromJsonAsync<ClientResponse<PagedResponse<TaskViewModel>>>("api/tasks?pageSize=1");
            var systemId = tasksResponse.Payload!.Items.Single().SystemId;

            using var content = new MultipartFormDataContent();

            content.Add(new ByteArrayContent([1]), "files", "over-limit.txt");

            var response = await Client.PostAsync($"api/tasks/{systemId}/files", content);

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ClientResponse<FileUploadResult>>();

            result.Payload.Should().Match<FileUploadResult>(item => !item.IsSuccess && item.Error!.Contains("limit exceeded"));
            workspace.StorageUsedBytes.Should().Be(workspace.StorageLimitBytes);
        }
        finally
        {
            workspace.StorageLimitBytes = originalLimit;

            await context.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task TaskFile_ShouldRejectAFileOverTheWorkspaceUploadLimit()
    {
        using var scope = Fixture.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var workspace = await context.Workspaces.SingleAsync(item => item.Slug == "netptune");
        var originalLimit = workspace.MaxUploadBytes;

        workspace.MaxUploadBytes = UploadLimits.MinimumMaxUploadBytes;

        await context.SaveChangesAsync();

        try
        {
            var tasksResponse = await Client.GetFromJsonAsync<ClientResponse<PagedResponse<TaskViewModel>>>("api/tasks?pageSize=1");
            var systemId = tasksResponse.Payload!.Items.Single().SystemId;
            var oversized = new byte[UploadLimits.MinimumMaxUploadBytes + 1];

            using var content = new MultipartFormDataContent();

            content.Add(new ByteArrayContent(oversized), "files", "oversized.txt");

            var response = await Client.PostAsync($"api/tasks/{systemId}/files", content);

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ClientResponse<FileUploadResult>>();

            result.Payload.Should().Match<FileUploadResult>(item => !item.IsSuccess && item.Error!.Contains("exceeds the 1 MiB limit"));
        }
        finally
        {
            workspace.MaxUploadBytes = originalLimit;

            await context.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task TaskFile_ShouldRejectMultipleFiles()
    {
        using var content = new MultipartFormDataContent();

        content.Add(new ByteArrayContent([1]), "files", "first.txt");
        content.Add(new ByteArrayContent([2]), "files", "second.txt");

        var response = await Client.PostAsync("api/tasks/NET-1/files", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task WorkspaceFileEnums_ShouldBeNativePostgresEnums()
    {
        using var scope = Fixture.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var connection = context.Database.GetDbConnection();

        await connection.OpenAsync();

        await using var command = connection.CreateCommand();

        command.CommandText = "SELECT count(*) FROM pg_type WHERE typtype = 'e' AND typname IN ('workspace_file_purpose', 'workspace_file_status')";
        Convert.ToInt32(await command.ExecuteScalarAsync()).Should().Be(2);
    }

    private async Task<WorkspaceFileViewModel> UploadTaskFile(string systemId, string fileName)
    {
        using var content = new MultipartFormDataContent();

        content.Add(new ByteArrayContent([10, 20, 30, 40]), "files", fileName);
        content.First().Headers.ContentType = new("application/pdf");

        var response = await Client.PostAsync($"api/tasks/{systemId}/files", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<FileUploadResult>>();

        result.Payload!.IsSuccess.Should().BeTrue();

        return result.Payload.File!;
    }
}
