using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Authentication.Models;
using Netptune.Core.Authorization;
using Netptune.Core.Entities;
using Netptune.Core.Localization;
using Netptune.Core.Relationships;
using Netptune.Core.Requests;
using Netptune.Core.Requests.ServiceAccounts;
using Netptune.Core.Responses;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.ServiceAccounts;
using Netptune.Core.ViewModels.Users;
using Netptune.Entities.Contexts;
using Netptune.IntegrationTests.TestServices;
using Netptune.TestData;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

[Collection(UserMutationCollection.Name)]
public sealed class UsersEndpointTests
{
    private const string WorkspaceSlug = "netptune";

    private readonly HttpClient Client;
    private readonly NetptuneFixture Fixture;

    public UsersEndpointTests(NetptuneFixture fixture)
    {
        Fixture = fixture;
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
    public async Task Get_ShouldOnlyReturnMatches_WhenSearchProvided()
    {
        var target = SeedData.Users.Last();

        var response = await Client.GetAsync($"api/users?search={target.Lastname}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<PagedResponse<WorkspaceUserViewModel>>>();

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Items.Should().ContainSingle(item => item.Id == target.Id);
        result.Payload.Items.Should().OnlyContain(item => item.DisplayName.Contains(target.Lastname));
        result.Payload.TotalCount.Should().Be(result.Payload.Items.Count);
    }

    [Fact]
    public async Task Get_ShouldOnlyReturnMembersWithRole_WhenRoleProvided()
    {
        var response = await Client.GetAsync($"api/users?role={(int)WorkspaceRole.Owner}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<PagedResponse<WorkspaceUserViewModel>>>();

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Items.Should().NotBeEmpty();
        result.Payload.Items.Should().OnlyContain(item => item.Role == WorkspaceRole.Owner && !item.IsPending);
    }

    [Fact]
    public async Task Get_ShouldOnlyReturnPendingInvites_WhenIsPendingProvided()
    {
        const string inviteEmail = "list-pending-user@gmail.com";

        await Client.PostAsJsonAsync("api/users/invite", new InviteUsersRequest
        {
            EmailAddresses = [inviteEmail],
        });

        var response = await Client.GetAsync("api/users?isPending=true");
        var result = await response.Content.ReadFromJsonAsync<ClientResponse<PagedResponse<WorkspaceUserViewModel>>>();

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Items.Should().Contain(item => item.Email == inviteEmail);
        result.Payload.Items.Should().OnlyContain(item => item.IsPending);
    }

    [Fact]
    public async Task GetSelectOptions_ShouldReturnWorkspaceMembers_WhenNoSearchProvided()
    {
        var response = await Client.GetAsync("api/users/select");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<PagedResponse<UserSelectOptionViewModel>>>();

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Items.Should().NotBeEmpty();
        result.Payload.Items.Should().OnlyContain(item => !string.IsNullOrWhiteSpace(item.DisplayName));
        result.Payload.Page.Should().Be(1);
    }

    [Fact]
    public async Task GetSelectOptions_ShouldOnlyReturnMatches_WhenSearchProvided()
    {
        var target = SeedData.Users.Last();

        var response = await Client.GetAsync($"api/users/select?search={target.Lastname}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<PagedResponse<UserSelectOptionViewModel>>>();

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Items.Should().ContainSingle(item => item.Id == target.Id);
        result.Payload.Items.Should().OnlyContain(item => item.DisplayName.Contains(target.Lastname));
    }

    [Fact]
    public async Task GetSelectOptions_ShouldMatchOnEmail_WhenSearchIsAnEmailFragment()
    {
        var target = SeedData.Users.First();

        var response = await Client.GetAsync($"api/users/select?search={target.Email}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<PagedResponse<UserSelectOptionViewModel>>>();

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Items.Should().ContainSingle(item => item.Id == target.Id);
    }

    [Fact]
    public async Task GetSelectOptions_ShouldReturnEmpty_WhenSearchMatchesNobody()
    {
        var response = await Client.GetAsync("api/users/select?search=no-such-person-here");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<PagedResponse<UserSelectOptionViewModel>>>();

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Items.Should().BeEmpty();
        result.Payload.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetSelectOptions_ShouldExcludePendingInvites()
    {
        const string inviteEmail = "select-pending-user@gmail.com";

        await Client.PostAsJsonAsync("api/users/invite", new InviteUsersRequest
        {
            EmailAddresses = [inviteEmail],
        });

        var response = await Client.GetAsync("api/users/select");
        var result = await response.Content.ReadFromJsonAsync<ClientResponse<PagedResponse<UserSelectOptionViewModel>>>();

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Items.Should().NotContain(item => item.Email == inviteEmail);
    }

    [Fact]
    public async Task GetSelectOptions_ShouldExcludeServiceAccounts_WhenRequested()
    {
        var accountName = $"select-agent-{Guid.NewGuid():N}";

        var createResponse = await Client.PostAsJsonAsync("api/service-accounts", new CreateServiceAccountRequest
        {
            Name = accountName,
            Permissions = [NetptunePermissions.Tasks.Read],
        }, TestContext.Current.CancellationToken);

        createResponse.StatusCode.Should().Be(HttpStatusCode.OK, await createResponse.Content.ReadAsStringAsync());

        var result = await createResponse.Content
            .ReadFromJsonAsync<ClientResponse<ServiceAccountViewModel>>(TestContext.Current.CancellationToken);
        var account = result.Payload;

        var includedResponse = await Client.GetAsync(
            $"api/users/select?search={accountName}",
            TestContext.Current.CancellationToken);

        var included = await includedResponse.Content
            .ReadFromJsonAsync<ClientResponse<PagedResponse<UserSelectOptionViewModel>>>(
                TestContext.Current.CancellationToken);

        included.Payload!.Items.Should().ContainSingle(item => item.Id == account!.UserId);

        var excludedResponse = await Client.GetAsync(
            $"api/users/select?search={accountName}&excludeServiceAccounts=true",
            TestContext.Current.CancellationToken);

        var excluded = await excludedResponse.Content
            .ReadFromJsonAsync<ClientResponse<PagedResponse<UserSelectOptionViewModel>>>(
                TestContext.Current.CancellationToken);

        excluded.Payload!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetById_ShouldReturnCorrectly_WhenInputValid()
    {
        var response = await Client.GetAsync($"api/users/{SeedData.Users.First().Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<WorkspaceUserViewModel>();

        result.Should().NotBeNull();
        result.Role.Should().Be(WorkspaceRole.Owner);
        result.Permissions.Should().BeEquivalentTo(NetptunePermissions.All);
    }

    [Fact]
    public async Task UpdateRole_ShouldReplaceTheMembersRoleAndPermissions()
    {
        var userId = SeedData.Users.ElementAt(1).Id;
        var previous = await Client.GetFromJsonAsync<WorkspaceUserViewModel>($"api/users/{userId}");

        try
        {
            var response = await Client.PutAsJsonAsync("api/users/role", new UpdateWorkspaceRoleRequest
            {
                UserId = userId,
                Role = WorkspaceRole.Viewer,
            });

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content
                .ReadFromJsonAsync<ClientResponse<WorkspaceRoleUpdateViewModel>>();

            result.IsSuccess.Should().BeTrue();
            result.Payload!.Role.Should().Be(WorkspaceRole.Viewer);
            result.Payload.Permissions.Should().BeEquivalentTo(
                WorkspaceRolePermissions.GetDefaultPermissions(WorkspaceRole.Viewer));

            var getResponse = await Client.GetAsync($"api/users/{userId}");
            var updatedUser = await getResponse.Content.ReadFromJsonAsync<WorkspaceUserViewModel>();

            updatedUser!.Role.Should().Be(WorkspaceRole.Viewer);
            updatedUser.Permissions.Should().BeEquivalentTo(
                WorkspaceRolePermissions.GetDefaultPermissions(WorkspaceRole.Viewer));
        }
        finally
        {
            await Client.PutAsJsonAsync("api/users/role", new UpdateWorkspaceRoleRequest
            {
                UserId = userId,
                Role = previous!.Role,
            });
        }
    }

    [Fact]
    public async Task TogglePermission_ShouldGrantThenRevokeTheSamePermission()
    {
        var userId = SeedData.Users.ElementAt(1).Id;
        var request = new ToggleUserPermissionRequest
        {
            UserId = userId,
            Permission = NetptunePermissions.Automations.Manage,
        };

        var granted = await TogglePermission(request);

        granted.Should().Contain(NetptunePermissions.Automations.Manage);

        var revoked = await TogglePermission(request);

        revoked.Should().NotContain(NetptunePermissions.Automations.Manage);
    }

    [Fact]
    public async Task TogglePermission_ShouldReturnBadRequest_WhenUserIsNotAMember()
    {
        var response = await Client.PostAsJsonAsync("api/users/toggle-permission", new ToggleUserPermissionRequest
        {
            UserId = "not-a-user-id",
            Permission = NetptunePermissions.Automations.Manage,
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenInputDoesNotExist()
    {
        var response = await Client.GetAsync("api/users/not-a-user-id");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_ShouldReturnCorrectly_WhenInputValid()
    {
        var currentUser = await Client.GetFromJsonAsync<CurrentUserResponse>("api/auth/current-user");
        var userId = currentUser!.UserId;
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
    public async Task Update_ShouldReturnNotFound_WhenTargetIsAnotherUser()
    {
        var currentUser = await Client.GetFromJsonAsync<CurrentUserResponse>("api/auth/current-user");
        var otherUserId = SeedData.Users.First(user => user.Id != currentUser!.UserId).Id;
        var request = new UpdateUserRequest
        {
            Id = otherUserId,
            Firstname = "Should Not Apply",
            Lastname = "Should Not Apply",
        };

        var response = await Client.PutAsJsonAsync($"api/users/{otherUserId}", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var scope = Fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var otherUser = await context.Users.AsNoTracking()
            .FirstAsync(user => user.Id == otherUserId, TestContext.Current.CancellationToken);

        otherUser.Firstname.Should().NotBe(request.Firstname);
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
    public async Task Invite_ShouldEmailEachInviteeALinkCarryingTheirInviteCode()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        List<string> inviteEmails = [NewInviteEmail(), NewInviteEmail()];
        var request = new InviteUsersRequest { EmailAddresses = inviteEmails };

        var response = await Client.PostAsJsonAsync("api/users/invite", request, cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        foreach (var inviteEmail in inviteEmails)
        {
            var sentEmail = Fixture.Emails.SentTo(inviteEmail).Should().ContainSingle().Subject;
            var query = TestEmailService.LinkQuery(sentEmail);
            var pendingCode = await GetPendingInviteCode(inviteEmail, cancellationToken);

            sentEmail.Subject.Should().Be(AppText.InviteEmailSubject);
            new Uri(sentEmail.Link!).AbsolutePath.Should().Be(TestEmailService.RegisterPath);
            query["code"].ToString().Should().Be(pendingCode);
            query["refer"].ToString().Should().Be("invite");
        }
    }

    [Fact]
    public async Task Invite_ShouldNotEmailExistingMembers()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var memberEmail = SeedData.Users.First().Email;
        var emailsBefore = Fixture.Emails.SentTo(memberEmail).Count;
        var request = new InviteUsersRequest { EmailAddresses = [memberEmail] };

        var response = await Client.PostAsJsonAsync("api/users/invite", request, cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Fixture.Emails.SentTo(memberEmail).Should().HaveCount(emailsBefore);
    }

    [Fact]
    public async Task Invite_ShouldEmailTheRefreshedCode_WhenInvitingTheSameEmailAgain()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var inviteEmail = NewInviteEmail();
        var request = new InviteUsersRequest { EmailAddresses = [inviteEmail] };

        (await Client.PostAsJsonAsync("api/users/invite", request, cancellationToken)).EnsureSuccessStatusCode();
        (await Client.PostAsJsonAsync("api/users/invite", request, cancellationToken)).EnsureSuccessStatusCode();

        var sentEmails = Fixture.Emails.SentTo(inviteEmail);

        sentEmails.Should().HaveCount(2);

        var firstCode = TestEmailService.LinkQuery(sentEmails[0])["code"].ToString();
        var secondCode = TestEmailService.LinkQuery(sentEmails[1])["code"].ToString();
        var pendingCode = await GetPendingInviteCode(inviteEmail, cancellationToken);

        secondCode.Should().NotBe(firstCode);
        secondCode.Should().Be(pendingCode);
    }

    [Fact]
    public async Task ResendInvite_ShouldEmailANewCode_AndRetireTheOldOne()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var inviteEmail = NewInviteEmail();
        var request = new InviteUsersRequest { EmailAddresses = [inviteEmail] };

        (await Client.PostAsJsonAsync("api/users/invite", request, cancellationToken)).EnsureSuccessStatusCode();

        var response = await Client.PostAsJsonAsync("api/users/resend-invite", request, cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var sentEmails = Fixture.Emails.SentTo(inviteEmail, TestEmailService.RegisterPath);

        sentEmails.Should().HaveCount(2);

        var originalCode = TestEmailService.LinkQuery(sentEmails[0])["code"].ToString();
        var resentCode = TestEmailService.LinkQuery(sentEmails[1])["code"].ToString();
        var pendingCode = await GetPendingInviteCode(inviteEmail, cancellationToken);

        resentCode.Should().NotBe(originalCode);
        resentCode.Should().Be(pendingCode);

        var originalValidation = await ValidateInviteCode(originalCode, cancellationToken);
        var resentValidation = await ValidateInviteCode(resentCode, cancellationToken);

        originalValidation.Should().Be(HttpStatusCode.Unauthorized);
        resentValidation.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Invite_ShouldSendExistingAccountsAnAcceptLink_RatherThanAddingThem()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var accountEmail = await RegisterAccount(cancellationToken);
        var request = new InviteUsersRequest { EmailAddresses = [accountEmail] };

        var response = await Client.PostAsJsonAsync("api/users/invite", request, cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var inviteEmail = Fixture.Emails.SentTo(accountEmail, TestEmailService.AcceptInvitePath).Should().ContainSingle().Subject;
        var pendingCode = await GetPendingInviteCode(accountEmail, cancellationToken);
        var isMember = await IsWorkspaceMember(accountEmail, cancellationToken);

        TestEmailService.LinkQuery(inviteEmail)["code"].ToString().Should().Be(pendingCode);
        isMember.Should().BeFalse();
    }

    [Fact]
    public async Task AcceptInvite_ShouldJoinTheWorkspace_AndConfirmTheEmail()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var accountEmail = await RegisterAccount(cancellationToken);
        var request = new InviteUsersRequest { EmailAddresses = [accountEmail] };

        (await Client.PostAsJsonAsync("api/users/invite", request, cancellationToken)).EnsureSuccessStatusCode();

        var inviteEmail = Fixture.Emails.SentTo(accountEmail, TestEmailService.AcceptInvitePath).Single();
        var code = TestEmailService.LinkQuery(inviteEmail)["code"].ToString();
        var inviteeClient = Fixture.CreateNetptuneClientAs(accountEmail, workspaceKey: null);
        var acceptRequest = new AcceptWorkspaceInviteRequest { Code = code };

        var response = await inviteeClient.PostAsJsonAsync("api/auth/accept-workspace-invite", acceptRequest, cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(cancellationToken));

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<AcceptWorkspaceInviteResponse>>(cancellationToken);
        var isMember = await IsWorkspaceMember(accountEmail, cancellationToken);
        var isConfirmed = await IsEmailConfirmed(accountEmail, cancellationToken);

        result.Payload!.WorkspaceSlug.Should().Be(WorkspaceSlug);
        isMember.Should().BeTrue();
        isConfirmed.Should().BeTrue();

        var reuse = await inviteeClient.PostAsJsonAsync("api/auth/accept-workspace-invite", acceptRequest, cancellationToken);

        reuse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AcceptInvite_ShouldBeRejected_WhenSignedInWithADifferentEmail()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var invitedEmail = NewInviteEmail();
        var otherAccountEmail = await RegisterAccount(cancellationToken);
        var request = new InviteUsersRequest { EmailAddresses = [invitedEmail] };

        (await Client.PostAsJsonAsync("api/users/invite", request, cancellationToken)).EnsureSuccessStatusCode();

        var code = await GetPendingInviteCode(invitedEmail, cancellationToken);
        var otherClient = Fixture.CreateNetptuneClientAs(otherAccountEmail, workspaceKey: null);
        var acceptRequest = new AcceptWorkspaceInviteRequest { Code = code };

        var response = await otherClient.PostAsJsonAsync("api/auth/accept-workspace-invite", acceptRequest, cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var isMember = await IsWorkspaceMember(otherAccountEmail, cancellationToken);
        var stillPendingCode = await GetPendingInviteCode(invitedEmail, cancellationToken);

        isMember.Should().BeFalse();
        stillPendingCode.Should().Be(code);
    }

    [Fact]
    public async Task AcceptInvite_ShouldBeRejected_WhenTheWorkspaceWasDeleted()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var accountEmail = await RegisterAccount(cancellationToken);
        var invite = await SeedInviteToDeletedWorkspace(accountEmail, cancellationToken);
        var inviteeClient = Fixture.CreateNetptuneClientAs(accountEmail, workspaceKey: null);
        var acceptRequest = new AcceptWorkspaceInviteRequest { Code = invite.Code };

        var response = await inviteeClient.PostAsJsonAsync("api/auth/accept-workspace-invite", acceptRequest, cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var scope = Fixture.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var isMember = await context.WorkspaceAppUsers.AnyAsync(
            member => member.User.Email == accountEmail && member.WorkspaceId == invite.WorkspaceId,
            cancellationToken);
        var acceptedAt = await context.WorkspaceInvites
            .Where(pending => pending.Id == invite.Id)
            .Select(pending => pending.AcceptedAt)
            .SingleAsync(cancellationToken);

        isMember.Should().BeFalse();
        acceptedAt.Should().BeNull();
    }

    [Fact]
    public async Task RemoveFromWorkspace_ShouldRevokeAPendingInvite()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var inviteEmail = NewInviteEmail();
        var request = new InviteUsersRequest { EmailAddresses = [inviteEmail] };

        (await Client.PostAsJsonAsync("api/users/invite", request, cancellationToken)).EnsureSuccessStatusCode();

        var code = await GetPendingInviteCode(inviteEmail, cancellationToken);

        var response = await Client.PostAsJsonAsync("api/users/remove", request, cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(cancellationToken));

        var validation = await ValidateInviteCode(code, cancellationToken);

        validation.Should().Be(HttpStatusCode.Unauthorized);
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
        Fixture.Emails.SentTo("no-invite-for-this@gmail.com").Should().BeEmpty();
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
        var target = SeedData.Users.Last();
        var request = new InviteUsersRequest
        {
            EmailAddresses = [target.Email],
        };

        try
        {
            var response = await Client.PostAsJsonAsync("api/users/remove", request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ClientResponse<RemoveUsersWorkspaceResponse>>();

            result.IsSuccess.Should().BeTrue();
            result.Payload!.Emails.Should().ContainSingle(request.EmailAddresses.First());
        }
        finally
        {
            await RestoreWorkspaceMembership(target.Id);
        }
    }

    private static string NewInviteEmail() => $"invitee-{Guid.NewGuid():N}@netptune.test";

    private async Task<string> GetPendingInviteCode(string email, CancellationToken cancellationToken)
    {
        using var scope = Fixture.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        return await context.WorkspaceInvites
            .Where(invite => invite.Email == email && invite.AcceptedAt == null)
            .Select(invite => invite.Code)
            .SingleAsync(cancellationToken);
    }

    private async Task<WorkspaceInvite> SeedInviteToDeletedWorkspace(string email, CancellationToken cancellationToken)
    {
        using var scope = Fixture.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var suffix = Guid.NewGuid().ToString("N")[..12];
        var workspace = new Workspace
        {
            Name = "Deleted",
            Slug = $"deleted-{suffix}",
            CreatedAt = DateTime.UtcNow,
            MetaInfo = new(),
            IsDeleted = true,
        };
        var invite = new WorkspaceInvite
        {
            Email = email,
            Workspace = workspace,
            Code = Guid.NewGuid().ToString("N"),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
        };

        context.WorkspaceInvites.Add(invite);

        await context.SaveChangesAsync(cancellationToken);

        return invite;
    }

    private async Task<string> RegisterAccount(CancellationToken cancellationToken)
    {
        var email = $"account-{Guid.NewGuid():N}@netptune.test";
        var client = Fixture.CreateNetptuneClient();
        var request = new RegisterRequest
        {
            Email = email,
            Password = "Integration-Password-1!",
            Firstname = "Existing",
            Lastname = "Account",
            Turnstile = TestTurnstileService.ValidToken,
        };

        var response = await client.PostAsJsonAsync("api/auth/register", request, cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(cancellationToken));

        return email;
    }

    private async Task<bool> IsWorkspaceMember(string email, CancellationToken cancellationToken)
    {
        using var scope = Fixture.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        return await context.WorkspaceAppUsers.AnyAsync(
            member => member.User.Email == email && member.Workspace.Slug == WorkspaceSlug,
            cancellationToken);
    }

    private async Task<bool> IsEmailConfirmed(string email, CancellationToken cancellationToken)
    {
        using var scope = Fixture.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        return await context.Users
            .Where(user => user.Email == email)
            .Select(user => user.EmailConfirmed)
            .SingleAsync(cancellationToken);
    }

    private async Task<HttpStatusCode> ValidateInviteCode(string code, CancellationToken cancellationToken)
    {
        var escapedCode = Uri.EscapeDataString(code);
        var response = await Client.GetAsync($"api/auth/validate-workspace-invite?code={escapedCode}", cancellationToken);

        return response.StatusCode;
    }

    private async Task RestoreWorkspaceMembership(string userId)
    {
        using var scope = Fixture.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var workspaceId = await context.Workspaces
            .Where(workspace => workspace.Slug == WorkspaceSlug)
            .Select(workspace => workspace.Id)
            .FirstAsync(TestContext.Current.CancellationToken);

        var isMember = await context.WorkspaceAppUsers.AnyAsync(
            member => member.WorkspaceId == workspaceId && member.UserId == userId,
            TestContext.Current.CancellationToken);

        if (isMember)
        {
            return;
        }

        context.WorkspaceAppUsers.Add(new WorkspaceAppUser
        {
            WorkspaceId = workspaceId,
            UserId = userId,
            Role = WorkspaceRole.Member,
            Permissions = [],
        });

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private async Task<List<string>> TogglePermission(ToggleUserPermissionRequest request)
    {
        var response = await Client.PostAsJsonAsync("api/users/toggle-permission", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<List<string>>>();

        result.IsSuccess.Should().BeTrue();

        return result.Payload!;
    }
}
