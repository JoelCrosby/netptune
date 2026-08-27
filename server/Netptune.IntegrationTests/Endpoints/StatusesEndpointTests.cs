using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Netptune.Core.Enums;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Statuses;
using Netptune.Core.ViewModels.Usage;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

public sealed class StatusesEndpointTests
{
    private readonly HttpClient Client;

    public StatusesEndpointTests(NetptuneFixture fixture)
    {
        Client = fixture.CreateNetptuneClient();
    }

    [Fact]
    public async Task Get_ShouldReturnWorkspaceStatuses_WhenInputValid()
    {
        var response = await Client.GetAsync("api/statuses?entityType=Task");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<List<StatusViewModel>>();

        result!.Should().NotBeEmpty();
        result.Should().OnlyContain(status => status.EntityType == EntityType.Task);
    }

    [Fact]
    public async Task Create_ShouldReturnCorrectly_WhenInputValid()
    {
        var name = $"Status {Guid.NewGuid():N}";

        var response = await Client.PostAsJsonAsync("api/statuses", new CreateStatusRequest
        {
            Name = name,
            Description = "Created by integration tests",
            Color = "#ff0000",
            Category = StatusCategory.Todo,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<StatusViewModel>>();

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Name.Should().Be(name);
        result.Payload.Category.Should().Be(StatusCategory.Todo);
    }

    [Fact]
    public async Task Create_ShouldReturnBadRequest_WhenNameMissing()
    {
        var response = await Client.PostAsJsonAsync("api/statuses", new { });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_ShouldReturnCorrectly_WhenInputValid()
    {
        var status = await CreateStatus();
        var name = $"Renamed {Guid.NewGuid():N}";

        var response = await Client.PutAsJsonAsync("api/statuses", new UpdateStatusRequest
        {
            Id = status.Id,
            Name = name,
            Description = "Updated by integration tests",
            Color = "#00ff00",
            Category = StatusCategory.Active,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<StatusViewModel>>();

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Name.Should().Be(name);
        result.Payload.Category.Should().Be(StatusCategory.Active);
    }

    [Fact]
    public async Task Update_ShouldReturnNotFound_WhenStatusDoesNotExist()
    {
        var response = await Client.PutAsJsonAsync("api/statuses", new UpdateStatusRequest
        {
            Id = 999999,
            Name = "Missing status",
            Category = StatusCategory.Todo,
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Reorder_ShouldAssignSortOrderInRequestOrder_WhenInputValid()
    {
        var first = await CreateStatus();
        var second = await CreateStatus();

        var response = await Client.PostAsJsonAsync("api/statuses/reorder", new ReorderStatusesRequest
        {
            EntityType = EntityType.Task,
            StatusIds = [second.Id, first.Id],
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse>();

        result.IsSuccess.Should().BeTrue();

        var statuses = await Client.GetFromJsonAsync<List<StatusViewModel>>("api/statuses?entityType=Task");
        var reordered = statuses!.ToDictionary(status => status.Id);

        reordered[second.Id].SortOrder.Should().BeLessThan(reordered[first.Id].SortOrder);
    }

    [Fact]
    public async Task GetPage_ShouldReturnAPagedEnvelope_WhenInputValid()
    {
        var response = await Client.GetAsync("api/statuses/page?entityType=Task&page=1&pageSize=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<PagedResponse<StatusViewModel>>>();

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Page.Should().Be(1);
        result.Payload.PageSize.Should().Be(2);
        result.Payload.Items.Should().HaveCountLessThanOrEqualTo(2);
        result.Payload.Items.Should().OnlyContain(status => status.EntityType == EntityType.Task);
        result.Payload.TotalCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetPage_ShouldDefaultToSortOrder_WhenNoSortRequested()
    {
        var response = await Client.GetAsync("api/statuses/page?entityType=Task&pageSize=100");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<PagedResponse<StatusViewModel>>>();
        var sortOrders = result.Payload!.Items.Select(status => status.SortOrder).ToList();

        sortOrders.Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetPage_ShouldOnlyMatchTheSearchTerm_WhenSearchProvided()
    {
        var status = await CreateStatus();

        var response = await Client.GetAsync($"api/statuses/page?entityType=Task&search={status.Name}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<PagedResponse<StatusViewModel>>>();

        result.Payload!.Items.Should().ContainSingle(match => match.Id == status.Id);
    }

    [Fact]
    public async Task GetPage_ShouldCountTasks_WhenStatusIsUsed()
    {
        var response = await Client.GetAsync("api/statuses/page?entityType=Task&pageSize=100");

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<PagedResponse<StatusViewModel>>>();

        result.Payload!.Items.Should().Contain(status => status.TaskCount > 0);
    }

    [Fact]
    public async Task Move_ShouldSwapWithTheNeighbour_WhenMovingUp()
    {
        await CreateStatus();

        var before = await GetOrderedIds();
        var moving = before[^1];
        var expected = before.ToList();
        (expected[^2], expected[^1]) = (expected[^1], expected[^2]);

        var response = await Client.PostAsJsonAsync("api/statuses/move", new MoveStatusRequest
        {
            Id = moving,
            Direction = SortMoveDirection.Up,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse>();

        result.IsSuccess.Should().BeTrue();

        var after = await GetOrderedIds();

        after.Should().Equal(expected);
    }

    [Fact]
    public async Task Move_ShouldLeaveTheOrderUnchanged_WhenAlreadyAtTheTop()
    {
        var before = await GetOrderedIds();

        var response = await Client.PostAsJsonAsync("api/statuses/move", new MoveStatusRequest
        {
            Id = before[0],
            Direction = SortMoveDirection.Up,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var after = await GetOrderedIds();

        after.Should().Equal(before);
    }

    [Fact]
    public async Task Move_ShouldReturnNotFound_WhenStatusDoesNotExist()
    {
        var response = await Client.PostAsJsonAsync("api/statuses/move", new MoveStatusRequest
        {
            Id = int.MaxValue,
            Direction = SortMoveDirection.Down,
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ShouldReturnCorrectly_WhenStatusUnused()
    {
        var status = await CreateStatus();

        var response = await Client.DeleteAsync($"api/statuses/{status.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse>();

        result.IsSuccess.Should().BeTrue();

        var statuses = await Client.GetFromJsonAsync<List<StatusViewModel>>("api/statuses?entityType=Task");

        statuses!.Should().NotContain(item => item.Id == status.Id);
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenStatusDoesNotExist()
    {
        var response = await Client.DeleteAsync("api/statuses/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetUsage_ShouldReturnUnusedStatus_WhenStatusIsNew()
    {
        var status = await CreateStatus();

        var response = await Client.GetAsync($"api/statuses/{status.Id}/usage");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<EntityUsageViewModel>();

        result!.Id.Should().Be(status.Id);
        result.Kind.Should().Be(UsageSubjectKind.Status);
        result.UsageCount.Should().Be(0);
        result.References.Should().BeEmpty();
        result.CanDelete.Should().BeTrue();
    }

    [Fact]
    public async Task GetUsage_ShouldCountTasks_WhenStatusIsUsed()
    {
        var statuses = await Client.GetFromJsonAsync<List<StatusViewModel>>("api/statuses?entityType=Task");
        var used = statuses!.First(status => status.TaskCount > 0);

        var response = await Client.GetAsync($"api/statuses/{used.Id}/usage");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<EntityUsageViewModel>();

        result!.UsageCount.Should().Be(used.TaskCount);
        result.CanDelete.Should().BeFalse();
        result.BlockedReason.Should().NotBeNull();
    }

    [Fact]
    public async Task GetUsage_ShouldReturnNotFound_WhenStatusDoesNotExist()
    {
        var response = await Client.GetAsync("api/statuses/999999/usage");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<List<int>> GetOrderedIds()
    {
        var statuses = await Client.GetFromJsonAsync<List<StatusViewModel>>("api/statuses?entityType=Task");

        return statuses!
            .OrderBy(status => status.SortOrder)
            .ThenBy(status => status.Id)
            .Select(status => status.Id)
            .ToList();
    }

    private async Task<StatusViewModel> CreateStatus()
    {
        var response = await Client.PostAsJsonAsync("api/statuses", new CreateStatusRequest
        {
            Name = $"Status {Guid.NewGuid():N}",
            Category = StatusCategory.Todo,
        });

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<StatusViewModel>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.IsSuccess.Should().BeTrue();

        return result.Payload!;
    }
}
