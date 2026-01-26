using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Netptune.Core.Enums;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Activity;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

public class ActivityEndpointTests(NetptuneFixture fixture)
{
    [Fact]
    public async Task Get_ShouldReturnCorrectly_WhenInputValid()
    {
        var response = await fixture.Client.GetAsync($"api/activity/{EntityType.Task}/1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<List<ActivityViewModel>>>();

        result!.IsSuccess.Should().BeTrue();
        result.Payload.Should().NotBeEmpty();
    }
}
