using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Netptune.Core.Authorization;
using Netptune.Core.Requests.ServiceAccounts;
using Netptune.Core.ViewModels.ServiceAccounts;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

public sealed class ServiceAccountsEndpointTests
{
    private readonly HttpClient Client;

    public ServiceAccountsEndpointTests(NetptuneFixture fixture)
    {
        Client = fixture.CreateNetptuneClient();
    }

    [Fact]
    public async Task ManagementFlow_ShouldCreateListAndRevokeCredential()
    {
        var accountName = $"Integration agent {Guid.NewGuid():N}";
        var createAccountResponse = await Client.PostAsJsonAsync(
            "api/service-accounts",
            new CreateServiceAccountRequest
            {
                Name = accountName,
                Description = "Created by the service account integration test.",
                Permissions =
                [
                    NetptunePermissions.Projects.Read,
                    NetptunePermissions.Sprints.Read,
                    NetptunePermissions.Sprints.Create,
                    NetptunePermissions.Tasks.Read,
                    NetptunePermissions.Tasks.Create,
                ],
            });

        createAccountResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var account = await createAccountResponse.Content.ReadFromJsonAsync<ServiceAccountViewModel>();
        account.Should().NotBeNull();
        account!.Name.Should().Be(accountName);
        account.Permissions.Should().Contain(NetptunePermissions.Sprints.Read);
        account.Permissions.Should().Contain(NetptunePermissions.Sprints.Create);

        var createCredentialResponse = await Client.PostAsJsonAsync(
            $"api/service-accounts/{account.Id}/credentials",
            new CreateApiCredentialRequest
            {
                Name = "Integration credential",
                Scopes = [NetptunePermissions.Tasks.Read, NetptunePermissions.Tasks.Create],
            });

        createCredentialResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var createdCredential = await createCredentialResponse.Content
            .ReadFromJsonAsync<ApiCredentialCreatedViewModel>();
        createdCredential.Should().NotBeNull();
        createdCredential!.Token.Should().StartWith("ntp_");

        var accounts = await Client.GetFromJsonAsync<List<ServiceAccountViewModel>>(
            "api/service-accounts");
        var listedAccount = accounts.Should().ContainSingle(item => item.Id == account.Id).Subject;
        listedAccount.Credentials.Should().ContainSingle(item => item.Id == createdCredential.Id);

        var revokeResponse = await Client.DeleteAsync(
            $"api/service-accounts/{account.Id}/credentials/{createdCredential.Id}");
        revokeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        accounts = await Client.GetFromJsonAsync<List<ServiceAccountViewModel>>(
            "api/service-accounts");
        accounts!
            .Single(item => item.Id == account.Id)
            .Credentials.Single(item => item.Id == createdCredential.Id)
            .RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Delete_ShouldDisableAccountAndRevokeCredentials()
    {
        var createAccountResponse = await Client.PostAsJsonAsync(
            "api/service-accounts",
            new CreateServiceAccountRequest
            {
                Name = $"Deleted agent {Guid.NewGuid():N}",
                Permissions = [NetptunePermissions.Tasks.Read],
            });
        var account = await createAccountResponse.Content.ReadFromJsonAsync<ServiceAccountViewModel>();

        var createCredentialResponse = await Client.PostAsJsonAsync(
            $"api/service-accounts/{account!.Id}/credentials",
            new CreateApiCredentialRequest
            {
                Name = "Credential to revoke",
                Scopes = [NetptunePermissions.Tasks.Read],
            });
        var credential = await createCredentialResponse.Content
            .ReadFromJsonAsync<ApiCredentialCreatedViewModel>();

        var deleteResponse = await Client.DeleteAsync($"api/service-accounts/{account.Id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var accounts = await Client.GetFromJsonAsync<List<ServiceAccountViewModel>>(
            "api/service-accounts");
        var deletedAccount = accounts.Should().ContainSingle(item => item.Id == account.Id).Subject;
        deletedAccount.DisabledAt.Should().NotBeNull();
        deletedAccount.Credentials
            .Single(item => item.Id == credential!.Id)
            .RevokedAt.Should().NotBeNull();

        var recreateCredentialResponse = await Client.PostAsJsonAsync(
            $"api/service-accounts/{account.Id}/credentials",
            new CreateApiCredentialRequest
            {
                Name = "Rejected credential",
                Scopes = [NetptunePermissions.Tasks.Read],
            });
        recreateCredentialResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
