using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Netptune.Core.Onboarding.Templates;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

public sealed class SetupTemplatesEndpointTests
{
    private readonly HttpClient Client;

    public SetupTemplatesEndpointTests(NetptuneFixture fixture)
    {
        Client = fixture.CreateNetptuneClient();
    }

    [Fact]
    public async Task Get_ShouldReturnTheTemplateCatalog()
    {
        var response = await Client.GetAsync("api/setup-templates");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<List<WorkspaceSetupTemplateViewModel>>();

        result!.Should().HaveCount(WorkspaceSetupTemplateCatalog.All.Count);
        result.Should().Contain(template => template.Key == WorkspaceSetupTemplateCatalog.DefaultKey);
        result.Should().OnlyContain(template => template.Statuses.Count > 0);
    }
}
