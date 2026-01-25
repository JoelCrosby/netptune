using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Netptune.Core.Requests;
using Netptune.Core.Responses;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Users;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

[Collection(Collections.Database)]
public sealed class UsersEndpointTests
{
    private readonly HttpClient Client;

    public UsersEndpointTests(NetptuneApiFactory factory)
    {
        Client = factory.CreateNetptuneClient();
    }

    [Fact]
    public async Task Get_ShouldReturnCorrectly_WhenInputValid()
    {
        var response = await Client.GetAsync("api/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<List<UserViewModel>>();

        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetById_ShouldReturnCorrectly_WhenInputValid()
    {
        var response = await Client.GetAsync($"api/users/{TestData.Users.First().Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<UserViewModel>();

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenInputDoesNotExist()
    {
        var response = await Client.GetAsync("api/users/not-a-user-id");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetByEmail_ShouldReturnCorrectly_WhenInputValid()
    {
        var response = await Client.GetAsync($"api/users/get-by-email?email={TestData.Users.First().Email}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<UserViewModel>();

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByEmail_ShouldReturnNotFound_WhenInputDoesNotExist()
    {
        var response = await Client.GetAsync("api/users/get-by-email?email=not-an-email");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAll_ShouldReturnCorrectly_WhenInputValid()
    {
        var response = await Client.GetAsync("api/users/all");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<List<UserViewModel>>();

        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Update_ShouldReturnCorrectly_WhenInputValid()
    {
        var userId = TestData.Users.First().Id;
        var request = new UpdateUserRequest
        {
            Id = userId,
            Firstname = "Updated Firstname",
            Lastname = "Updated Lastname",
            PictureUrl = "https://some-picture.co.uk/picture.png",
        };

        var response = await Client.PutAsJsonAsync($"api/users/{userId}", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<UserViewModel>>();

        result!.IsSuccess.Should().BeTrue();
        result.Payload!.Firstname.Should().Be(request.Firstname);
        result.Payload.Lastname.Should().Be(request.Lastname);
        result.Payload.PictureUrl.Should().Be(request.PictureUrl);
    }

    [Fact]
    public async Task Update_ShouldReturnNotFound_WhenInputDoesNotExist()
    {
        const string userId = "not-a-user-id";
        var request = new UpdateUserRequest
        {
            Id = userId,
            Firstname = "Updated Firstname",
            Lastname = "Updated Lastname",
            PictureUrl = "https://some-picture.co.uk/picture.png",
        };

        var response = await Client.PutAsJsonAsync($"api/users/{userId}", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Invite_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = new InviteUsersRequest
        {
            EmailAddresses = new () { "janedoe@gmail.com" },
        };

        var response = await Client.PostAsJsonAsync("api/users/invite", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<InviteUserResponse>>();

        result!.IsSuccess.Should().BeTrue();
        result.Payload!.Emails.Should().ContainSingle(request.EmailAddresses.First());
    }

    [Fact]
    public async Task RemoveFromWorkspace_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = new InviteUsersRequest
        {
            EmailAddresses = new () { TestData.Users.Last().Email },
        };

        var response = await Client.PostAsJsonAsync("api/users/remove", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<RemoveUsersWorkspaceResponse>>();

        result!.IsSuccess.Should().BeTrue();
        result.Payload!.Emails.Should().ContainSingle(request.EmailAddresses.First());
    }
}
