using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Enums;
using Netptune.Core.ViewModels.Tags;
using Netptune.Core.ViewModels.Usage;
using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

public sealed class TagsEndpointTests
{
    private readonly HttpClient Client;

    public TagsEndpointTests(NetptuneFixture fixture)
    {
        Client = fixture.CreateNetptuneClient();
    }

    [Fact]
    public async Task GetByWorkspace_ShouldReturnCorrectly_WhenInputValid()
    {
        var response = await Client.GetAsync("api/tags/workspace");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<List<TagViewModel>>();

        result!.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetByTask_ShouldReturnCorrectly_WhenInputValid()
    {
        var response = await Client.GetAsync("api/tags/task/neo-1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<List<TagViewModel>>();

        result!.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByTask_ShouldReturnNotFound_WhenInputDoesNotExist()
    {
        var response = await Client.GetAsync("api/tags/task/systemd-id");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

     [Fact]
    public async Task Update_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = new UpdateTagRequest
        {
            CurrentValue = "Go",
            NewValue = "Update value",
        };

        var response = await Client.PatchAsJsonAsync("api/tags", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<TagViewModel>>();

        result.IsSuccess.Should().BeTrue();

        result.Payload.Should().NotBeNull();
        result.Payload!.Name.Should().Be(request.NewValue);
    }

    [Fact]
    public async Task Update_ShouldReturnNotFound_WhenInputDoesNotExist()
    {
        var request = new UpdateTagRequest
        {
            CurrentValue = "not-a-tag",
            NewValue = "Updated name",
        };

        var response = await Client.PatchAsJsonAsync("api/tags", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = new AddTagRequest
        {
            Tag = "New Tag",
        };

        var response = await Client.PostAsJsonAsync("api/tags", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<TagViewModel>>();

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Name.Should().Be(request.Tag);
    }

    [Fact]
    public async Task Create_ShouldReturnBadRequest_WhenInputNotValid()
    {
        var response = await Client.PostAsJsonAsync("api/tags", new {});

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetUsage_ShouldReturnTaskCount_WhenTagExists()
    {
        var created = await Client.PostAsJsonAsync("api/tags", new AddTagRequest { Tag = $"Usage {Guid.NewGuid():N}" });
        var tag = await created.Content.ReadFromJsonAsync<ClientResponse<TagViewModel>>();

        var response = await Client.GetAsync($"api/tags/{tag.Payload!.Id}/usage");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<EntityUsageViewModel>();

        result!.Id.Should().Be(tag.Payload.Id);
        result.Kind.Should().Be(UsageSubjectKind.Tag);
        result.UsageCount.Should().Be(0);
        result.CanDelete.Should().BeTrue();
    }

    [Fact]
    public async Task GetUsage_ShouldReturnNotFound_WhenTagDoesNotExist()
    {
        var response = await Client.GetAsync("api/tags/999999/usage");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateTaskTag_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = new AddTagToTaskRequest
        {
            Tag = "New Task Tag",
            SystemId = "neo-1",
        };

        var response = await Client.PostAsJsonAsync("api/tags/task", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<TagViewModel>>();

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Name.Should().Be(request.Tag);
    }

    [Fact]
    public async Task CreateTaskTag_ShouldReturnNotFound_WhenTaskIdInvalid()
    {
        var request = new AddTagToTaskRequest
        {
            Tag = "New Task Tag",
            SystemId = "neo-10000",
        };

        var response = await Client.PostAsJsonAsync("api/tags/task", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteFromTask_ShouldReturnCorrectly_WhenInputValid()
    {
        var tag = $"Detach target {Guid.NewGuid():N}";
        var request = new DeleteTagFromTaskRequest
        {
            Tag = tag,
            SystemId = "neo-1",
        };

        var attached = await Client.PostAsJsonAsync("api/tags/task", new AddTagToTaskRequest
        {
            Tag = tag,
            SystemId = request.SystemId,
        });

        attached.StatusCode.Should().Be(HttpStatusCode.OK, await attached.Content.ReadAsStringAsync());

        var response = await Client.SendAsync(new ()
        {
            Method = HttpMethod.Delete,
            RequestUri = new ("api/tags/task", UriKind.RelativeOrAbsolute),
            Content = JsonContent.Create(request),
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse>();

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteTags_ShouldReturnNotFound_WhenInputDoesNotExist()
    {
        var request = new DeleteTagsRequest
        {
            Tags = new () { "Python4", "Java6" },
        };

        var response = await Client.SendAsync(new ()
        {
            Method = HttpMethod.Delete,
            RequestUri = new ("api/tags", UriKind.RelativeOrAbsolute),
            Content = JsonContent.Create(request),
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse>();

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteFromTask_ShouldReturnNotFound_WhenInputDoesNotExist()
    {
        var request = new DeleteTagFromTaskRequest
        {
            Tag = "New Tag",
            SystemId = "non-existing-systemId",
        };

        var response = await Client.SendAsync(new ()
        {
            Method = HttpMethod.Delete,
            RequestUri = new ("api/tags/task", UriKind.RelativeOrAbsolute),
            Content = JsonContent.Create(request),
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
