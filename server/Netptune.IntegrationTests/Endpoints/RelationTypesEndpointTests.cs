using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Netptune.Core.Colors;
using Netptune.Core.Enums;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Relations;
using Netptune.Core.ViewModels.RelationTypes;
using Netptune.Core.ViewModels.Usage;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

public sealed class RelationTypesEndpointTests
{
    private static readonly SemaphoreSlim WorkspaceLock = new(1, 1);

    private static string? WorkspaceSlug;

    private readonly NetptuneFixture Fixture;

    public RelationTypesEndpointTests(NetptuneFixture fixture)
    {
        Fixture = fixture;
    }

    [Fact]
    public async Task Get_ShouldReturnWorkspaceRelationTypes_WhenInputValid()
    {
        var client = await CreateClient();

        var response = await client.GetAsync("api/relation-types");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<List<RelationTypeViewModel>>();

        result!.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Create_ShouldReturnCorrectly_WhenInputValid()
    {
        var client = await CreateClient();

        var name = $"Relation {Guid.NewGuid():N}";

        var response = await client.PostAsJsonAsync("api/relation-types", new CreateRelationTypeRequest
        {
            Name = name,
            InverseName = $"Inverse {name}",
            Description = "Created by integration tests",
            Color = "#ff0000",
            Category = RelationCategory.Dependency,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<RelationTypeViewModel>>();

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Name.Should().Be(name);
        result.Payload.Category.Should().Be(RelationCategory.Dependency);
        result.Payload.Key.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Create_ShouldReturnBadRequest_WhenNameMissing()
    {
        var client = await CreateClient();

        var response = await client.PostAsJsonAsync("api/relation-types", new { });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ShouldFail_WhenKeyAlreadyExists()
    {
        var client = await CreateClient();

        var existing = await CreateRelationType();

        var response = await client.PostAsJsonAsync("api/relation-types", new CreateRelationTypeRequest
        {
            Name = existing.Name,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<RelationTypeViewModel>>();

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Update_ShouldReturnCorrectly_WhenInputValid()
    {
        var client = await CreateClient();

        var relationType = await CreateRelationType();
        var name = $"Renamed {Guid.NewGuid():N}";

        var response = await client.PutAsJsonAsync("api/relation-types", new UpdateRelationTypeRequest
        {
            Id = relationType.Id,
            Name = name,
            InverseName = $"Inverse {name}",
            Description = "Updated by integration tests",
            Color = "#00ff00",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<RelationTypeViewModel>>();

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Name.Should().Be(name);
        result.Payload.InverseName.Should().Be($"Inverse {name}");
    }

    [Fact]
    public async Task Update_ShouldReturnNotFound_WhenRelationTypeDoesNotExist()
    {
        var client = await CreateClient();

        var response = await client.PutAsJsonAsync("api/relation-types", new UpdateRelationTypeRequest
        {
            Id = 999999,
            Name = "Missing relation type",
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Reorder_ShouldAssignSortOrderInRequestOrder_WhenInputValid()
    {
        var client = await CreateClient();

        var first = await CreateRelationType();
        var second = await CreateRelationType();

        var response = await client.PostAsJsonAsync("api/relation-types/reorder", new ReorderRelationTypesRequest
        {
            RelationTypeIds = [second.Id, first.Id],
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse>();

        result.IsSuccess.Should().BeTrue();

        var relationTypes = await client.GetFromJsonAsync<List<RelationTypeViewModel>>("api/relation-types");
        var reordered = relationTypes!.ToDictionary(item => item.Id);

        reordered[second.Id].SortOrder.Should().BeLessThan(reordered[first.Id].SortOrder);
    }

    [Fact]
    public async Task Delete_ShouldReturnCorrectly_WhenRelationTypeUnused()
    {
        var client = await CreateClient();

        var relationType = await CreateRelationType();

        var response = await client.DeleteAsync($"api/relation-types/{relationType.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse>();

        result.IsSuccess.Should().BeTrue();

        var relationTypes = await client.GetFromJsonAsync<List<RelationTypeViewModel>>("api/relation-types");

        relationTypes!.Should().NotContain(item => item.Id == relationType.Id);
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenRelationTypeDoesNotExist()
    {
        var client = await CreateClient();

        var response = await client.DeleteAsync("api/relation-types/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetUsage_ShouldReturnUnusedRelationType_WhenRelationTypeIsNew()
    {
        var client = await CreateClient();
        var relationType = await CreateRelationType();

        var response = await client.GetAsync($"api/relation-types/{relationType.Id}/usage");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<EntityUsageViewModel>();

        result!.Id.Should().Be(relationType.Id);
        result.Kind.Should().Be(UsageSubjectKind.RelationType);
        result.UsageCount.Should().Be(0);
        result.CanDelete.Should().BeTrue();
    }

    [Fact]
    public async Task GetUsage_ShouldReturnNotFound_WhenRelationTypeDoesNotExist()
    {
        var client = await CreateClient();

        var response = await client.GetAsync("api/relation-types/999999/usage");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetRelations_ShouldReturnEmptyPage_WhenRelationTypeIsUnused()
    {
        var client = await CreateClient();
        var relationType = await CreateRelationType();

        var response = await client.GetAsync($"api/relation-types/{relationType.Id}/relations");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResponse<RelationTypeRelationViewModel>>();

        result!.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetRelations_ShouldReturnNotFound_WhenRelationTypeDoesNotExist()
    {
        var client = await CreateClient();

        var response = await client.GetAsync("api/relation-types/999999/relations");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<HttpClient> CreateClient()
    {
        var client = Fixture.CreateNetptuneClient();

        client.DefaultRequestHeaders.Remove("workspace");
        client.DefaultRequestHeaders.Add("workspace", await GetWorkspaceSlug());

        return client;
    }

    private async Task<string> GetWorkspaceSlug()
    {
        if (WorkspaceSlug is not null)
        {
            return WorkspaceSlug;
        }

        await WorkspaceLock.WaitAsync();

        try
        {
            WorkspaceSlug ??= await CreateWorkspace();

            return WorkspaceSlug;
        }
        finally
        {
            WorkspaceLock.Release();
        }
    }

    private async Task<string> CreateWorkspace()
    {
        var slug = $"relation-types-{Guid.NewGuid():N}"[..24];
        var client = Fixture.CreateNetptuneClient();

        var response = await client.PostAsJsonAsync("api/workspaces", new AddWorkspaceRequest
        {
            Name = slug,
            Description = "Workspace for the relation type integration tests.",
            Slug = slug,
            MetaInfo = new() { Color = NamedColors.FallbackColor },
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        return slug;
    }

    private async Task<RelationTypeViewModel> CreateRelationType()
    {
        var client = await CreateClient();
        var name = $"Relation {Guid.NewGuid():N}";

        var response = await client.PostAsJsonAsync("api/relation-types", new CreateRelationTypeRequest
        {
            Name = name,
            InverseName = $"Inverse {name}",
            Category = RelationCategory.Dependency,
        });

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<RelationTypeViewModel>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.IsSuccess.Should().BeTrue();

        return result.Payload!;
    }
}
