using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Netptune.Core.Requests;
using Netptune.Core.Responses;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Users;
using Netptune.TestData;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

[Collection(UserMutationCollection.Name)]
public sealed class UsersEndpointTests
{
    private readonly HttpClient Client;

    public UsersEndpointTests(NetptuneFixture fixture)
    {
        Client = fixture.CreateNetptuneClient();
    }

    [Fact]
    public async Task Get_ShouldReturnCorrectly_WhenInputValid()
    {
        var response = await Client.GetAsync("api/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<PagedResponse<WorkspaceUserViewModel>>>();

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Items.Should().NotBeEmpty();
        result.Payload.Page.Should().Be(1);
        result.Payload.PageSize.Should().Be(50);
        result.Payload.TotalCount.Should().BeGreaterThanOrEqualTo(result.Payload.Items.Count);
    }

    [Fact]
    public async Task GetById_ShouldReturnCorrectly_WhenInputValid()
    {
        var response = await Client.GetAsync($"api/users/{SeedData.Users.First().Id}");

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
        var response = await Client.GetAsync($"api/users/get-by-email?email={SeedData.Users.First().Email}");

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
        var userId = SeedData.Users.First().Id;
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

        result.IsSuccess.Should().BeTrue();
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
            EmailAddresses = ["janedoe@gmail.com"],
        };

        var response = await Client.PostAsJsonAsync("api/users/invite", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<InviteUserResponse>>();

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Emails.Should().ContainSingle(request.EmailAddresses.First());
    }

    [Fact]
    public async Task Invite_ShouldAppearAsPendingInUserList()
    {
        const string inviteEmail = "pending-user@gmail.com";

        await Client.PostAsJsonAsync("api/users/invite", new InviteUsersRequest
        {
            EmailAddresses = [inviteEmail],
        });

        var response = await Client.GetAsync("api/users");
        var users = await response.Content.ReadFromJsonAsync<ClientResponse<PagedResponse<WorkspaceUserViewModel>>>();

        users.IsSuccess.Should().BeTrue();
        users.Payload!.Items.Should().Contain(u => u.Email == inviteEmail && u.IsPending);
    }

    [Fact]
    public async Task Invite_ShouldRefreshPendingInvite_WhenInvitingSameEmailTwice()
    {
        const string inviteEmail = "repeat-invite@gmail.com";

        var request = new InviteUsersRequest { EmailAddresses = [inviteEmail] };

        await Client.PostAsJsonAsync("api/users/invite", request);
        var secondResponse = await Client.PostAsJsonAsync("api/users/invite", request);

        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var users = await (await Client.GetAsync("api/users")).Content.ReadFromJsonAsync<ClientResponse<PagedResponse<WorkspaceUserViewModel>>>();
        users.IsSuccess.Should().BeTrue();
        users.Payload!.Items.Count(u => u.Email == inviteEmail).Should().Be(1);
    }

    [Fact]
    public async Task ResendInvite_ShouldReturnCorrectly_WhenPendingInviteExists()
    {
        const string inviteEmail = "resend-target@gmail.com";

        await Client.PostAsJsonAsync("api/users/invite", new InviteUsersRequest
        {
            EmailAddresses = [inviteEmail],
        });

        var response = await Client.PostAsJsonAsync("api/users/resend-invite", new InviteUsersRequest
        {
            EmailAddresses = [inviteEmail],
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse>();

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ResendInvite_ShouldReturnBadRequest_WhenNoPendingInviteExists()
    {
        var response = await Client.PostAsJsonAsync("api/users/resend-invite", new InviteUsersRequest
        {
            EmailAddresses = ["no-invite-for-this@gmail.com"],
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ResendInvite_ShouldReturnBadRequest_WhenEmailListIsEmpty()
    {
        var response = await Client.PostAsJsonAsync("api/users/resend-invite", new InviteUsersRequest
        {
            EmailAddresses = [],
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RemoveFromWorkspace_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = new InviteUsersRequest
        {
            EmailAddresses = [SeedData.Users.Last().Email],
        };

        var response = await Client.PostAsJsonAsync("api/users/remove", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<RemoveUsersWorkspaceResponse>>();

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Emails.Should().ContainSingle(request.EmailAddresses.First());
    }
}
