using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;

using Xunit;

namespace Netptune.IntegrationTests.TestServices;

public sealed class MemberPermissions : IAsyncDisposable
{
    private readonly HttpClient OwnerClient;
    private readonly string UserId;
    private readonly string Permission;
    private readonly bool HeldPermission;

    private MemberPermissions(HttpClient ownerClient, string userId, string permission, bool heldPermission)
    {
        OwnerClient = ownerClient;
        UserId = userId;
        Permission = permission;
        HeldPermission = heldPermission;
    }

    public static async Task<MemberPermissions> Revoke(HttpClient ownerClient, string userId, string permission)
    {
        var afterFirstToggle = await Toggle(ownerClient, userId, permission);
        var heldPermission = !afterFirstToggle.Contains(permission);

        if (!heldPermission)
        {
            await Toggle(ownerClient, userId, permission);
        }

        return new MemberPermissions(ownerClient, userId, permission, heldPermission);
    }

    public async ValueTask DisposeAsync()
    {
        if (HeldPermission)
        {
            await Toggle(OwnerClient, UserId, Permission);
        }
    }

    private static async Task<List<string>> Toggle(HttpClient client, string userId, string permission)
    {
        var request = new ToggleUserPermissionRequest { UserId = userId, Permission = permission };
        var response = await client.PostAsJsonAsync("api/users/toggle-permission", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<List<string>>>(TestContext.Current.CancellationToken);

        return result!.Payload!;
    }
}
