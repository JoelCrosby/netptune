using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Constants;
using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Repositories;
using Netptune.Core.Responses.Common;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Query.Model;
using Netptune.Query.Tasks;
using Netptune.Query.ViewModels;
using Netptune.Query.Views;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

[Collection(WorkspaceMutationCollection.Name)]
public sealed class TaskViewsEndpointTests
{
    private readonly HttpClient Client;
    private readonly NetptuneFixture Fixture;

    public TaskViewsEndpointTests(NetptuneFixture fixture)
    {
        Fixture = fixture;
        Client = fixture.CreateNetptuneClient();
    }

    [Fact]
    public async Task GetFields_ShouldReturnTheCatalogAndItsLimits()
    {
        var response = await Client.GetAsync("api/task-views/fields", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var catalog = await response.Content.ReadFromJsonAsync<QueryCatalogViewModel>(TestContext.Current.CancellationToken);

        catalog!.Fields.Should().HaveCount(TaskFieldCatalog.Instance.Fields.Count);
        catalog.Fields.Select(field => field.Key).Should().Contain(TaskFieldKeys.DueDate);
        catalog.Fields.Should().OnlyContain(field => field.Operators.Count > 0);
        catalog.MaximumDepth.Should().Be(ConditionGroupLimits.MaximumDepth);
        catalog.MaximumConditionCount.Should().Be(ConditionGroupLimits.MaximumConditionCount);
    }

    [Fact]
    public async Task Preview_ShouldReturnTheSameTasks_TheEquivalentFilterWould()
    {
        var expected = await GetTaskNamesMatching("OpenTelemetry");
        var query = Group(Condition(TaskFieldKeys.Name, QueryOperator.Contains, "opentelemetry"));
        var result = await Preview(query);

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Items.Select(task => task.Name).Should().BeEquivalentTo(expected);
        result.Payload.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Preview_ShouldCombineNestedGroups()
    {
        var query = new QueryGroup
        {
            Operator = QueryGroupOperator.All,
            Conditions = [Condition(TaskFieldKeys.Name, QueryOperator.Contains, "a")],
            Groups =
            [
                new QueryGroup
                {
                    Operator = QueryGroupOperator.Any,
                    Conditions =
                    [
                        Condition(TaskFieldKeys.Name, QueryOperator.Contains, "opentelemetry"),
                        Condition(TaskFieldKeys.Name, QueryOperator.Contains, "kubernetes"),
                    ],
                },
            ],
        };
        var result = await Preview(query);

        result.IsSuccess.Should().BeTrue();

        var names = result.Payload!.Items.Select(task => task.Name).ToList();

        names.Should().NotBeEmpty();
        names.Should().OnlyContain(name =>
            name.Contains("OpenTelemetry", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Kubernetes", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Preview_ShouldNegate_WhenTheGroupOperatorIsNone()
    {
        var query = new QueryGroup
        {
            Operator = QueryGroupOperator.None,
            Conditions = [Condition(TaskFieldKeys.Name, QueryOperator.Contains, "opentelemetry")],
        };
        var result = await Preview(query);

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Items.Should()
            .NotContain(task => task.Name.Contains("OpenTelemetry", StringComparison.OrdinalIgnoreCase));
    }

    // Date fields bind DateOnly parameters, which the text and status queries above never exercise.
    [Fact]
    public async Task Preview_ShouldRunADateComparison()
    {
        var query = Group(Condition(TaskFieldKeys.DueDate, QueryOperator.GreaterThanOrEqual, "2000-01-01"));
        var result = await Preview(query);

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Preview_ShouldRunARelativeDateQuery()
    {
        var query = Group(Condition(TaskFieldKeys.DueDate, QueryOperator.InNextDays, "3650"));
        var result = await Preview(query);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Preview_ShouldRunADateRange()
    {
        var query = Group(Condition(TaskFieldKeys.StartDate, QueryOperator.Between, "2000-01-01", "2100-01-01"));
        var result = await Preview(query);

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Preview_ShouldRunATimestampQuery()
    {
        var query = Group(Condition(TaskFieldKeys.CreatedAt, QueryOperator.GreaterThanOrEqual, "2000-01-01"));
        var result = await Preview(query);

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Items.Should().NotBeEmpty();
    }

    // Set membership binds an int[] through ANY(). Date fields deliberately have no In operator, so
    // this is the reachable array path.
    [Fact]
    public async Task Preview_ShouldRunASetMembershipOverStatuses()
    {
        var statuses = await GetAllTasks();
        var statusIds = statuses.Items.Select(task => task.StatusId).Distinct().Take(2).ToList();
        var values = statusIds.Select(id => id.ToString()).ToArray();
        var query = Group(Condition(TaskFieldKeys.Status, QueryOperator.In, values));
        var result = await Preview(query);

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Items.Should().NotBeEmpty();
        result.Payload.Items.Should().OnlyContain(task => statusIds.Contains(task.StatusId));
    }

    [Fact]
    public async Task Preview_ShouldRejectSetMembershipOnADateField()
    {
        var query = Group(Condition(TaskFieldKeys.DueDate, QueryOperator.In, "2000-01-01"));
        var result = await Preview(query);

        result.IsSuccess.Should().BeFalse();
        result.Payload!.Errors.Should().ContainSingle();
    }

    [Fact]
    public async Task Preview_ShouldReturnNothing_WhenTheQueryIsAnEmptyGroup()
    {
        var result = await Preview(new QueryGroup());

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Items.Should().BeEmpty();
        result.Payload.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Preview_ShouldReturnEverything_WhenNoQueryIsSupplied()
    {
        var all = await GetAllTasks();
        var result = await Preview(null);

        result.IsSuccess.Should().BeTrue();
        result.Payload!.TotalCount.Should().Be(all.TotalCount);
    }

    [Fact]
    public async Task Preview_ShouldRejectAnUnknownField_AndSayWhichConditionIsWrong()
    {
        var response = await PostPreview(Group(new QueryCondition
        {
            Field = "task.colour",
            Operator = QueryOperator.Equals,
            Values = ["blue"],
        }));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await ReadResult(response);

        result.IsSuccess.Should().BeFalse();
        result.Payload!.Errors.Should().ContainSingle();
        result.Payload.Errors[0].Path.Should().Be("query.conditions[0]");
        result.Payload.Errors[0].Field.Should().Be("task.colour");
    }

    [Fact]
    public async Task Preview_ShouldRejectAReferenceToAnEntityThatIsGone()
    {
        var response = await PostPreview(Group(Condition(TaskFieldKeys.Status, QueryOperator.Equals, "999999")));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await ReadResult(response);

        result.IsSuccess.Should().BeFalse();
        result.Payload!.Errors.Should().ContainSingle();
        result.Payload.Errors[0].Message.Should().Contain("no longer exists");
    }

    [Fact]
    public async Task TaskViews_ShouldRoundTripThroughCreateReadUpdateAndDelete()
    {
        var created = await CreateView("Round trip view");

        created.Name.Should().Be("Round trip view");
        created.Slug.Should().StartWith("round-trip-view-");
        created.IsOwn.Should().BeTrue();
        created.Definition!.Query.Conditions.Should().ContainSingle();

        var listed = await GetViews();

        listed.Should().Contain(view => view.Id == created.Id);

        var fetched = await GetView(created.Slug);

        fetched.Payload!.Name.Should().Be("Round trip view");

        var updateResponse = await Client.PutAsJsonAsync("api/task-views", new
        {
            id = created.Id,
            name = "Renamed view",
            description = "Now with a description",
            icon = "list",
            isShared = false,
            definition = Definition(Group(Condition(TaskFieldKeys.Name, QueryOperator.Contains, "kubernetes"))),
        }, TestContext.Current.CancellationToken);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await ReadViewResult(updateResponse);

        updated.Payload!.Id.Should().Be(created.Id);
        updated.Payload.Name.Should().Be("Renamed view");
        updated.Payload.Description.Should().Be("Now with a description");
        updated.Payload.Definition!.Query.Conditions[0].Values.Should().Equal("kubernetes");

        var deleteResponse = await Client.DeleteAsync($"api/task-views/{created.Slug}", TestContext.Current.CancellationToken);

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var afterDelete = await Client.GetAsync($"api/task-views/{created.Slug}", TestContext.Current.CancellationToken);

        afterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Rename_ShouldLeaveTheSlugAlone_SoExistingLinksKeepResolving()
    {
        var created = await CreateView("Link stability view");

        var response = await Client.PutAsJsonAsync("api/task-views", new
        {
            id = created.Id,
            name = "Renamed entirely",
            isShared = false,
            definition = Definition(Group(Condition(TaskFieldKeys.Name, QueryOperator.Contains, "opentelemetry"))),
        }, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await ReadViewResult(response);

        updated.Payload!.Name.Should().Be("Renamed entirely");
        updated.Payload.Slug.Should().Be(created.Slug);

        var refetched = await GetView(created.Slug);

        refetched.IsSuccess.Should().BeTrue();

        await Client.DeleteAsync($"api/task-views/{created.Slug}", TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Create_ShouldRefuseADuplicateName()
    {
        var created = await CreateView("Duplicate name view");
        var response = await PostView("Duplicate name view");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        await Client.DeleteAsync($"api/task-views/{created.Slug}", TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetTasks_ShouldRunTheSavedQuery()
    {
        var expected = await GetTaskNamesMatching("OpenTelemetry");
        var created = await CreateView("Saved query view");

        var response = await Client.GetAsync($"api/task-views/{created.Slug}/tasks", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await ReadResult(response);

        result.Payload!.Items.Select(task => task.Name).Should().BeEquivalentTo(expected);
        result.Payload.PageSize.Should().Be(TaskViewDisplay.DefaultPageSize);

        await Client.DeleteAsync($"api/task-views/{created.Slug}", TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetTasks_ShouldNotFound_WhenTheViewIsMissing()
    {
        var response = await Client.GetAsync("api/task-views/no-such-view-000000000000/tasks", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PrivateViews_ShouldBeInvisible_ToEveryoneButTheirOwner()
    {
        var slug = await SeedForeignView(false);

        var listed = await GetViews();

        listed.Should().NotContain(view => view.Slug == slug);

        var response = await Client.GetAsync($"api/task-views/{slug}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SharedViews_ShouldBeVisible_ToTheWholeWorkspace()
    {
        var slug = await SeedForeignView(true);

        var listed = await GetViews();

        listed.Should().Contain(view => view.Slug == slug);

        var fetched = await GetView(slug);

        fetched.IsSuccess.Should().BeTrue();
        fetched.Payload!.IsOwn.Should().BeFalse();
        fetched.Payload.CanEdit.Should().BeTrue("the seeded caller is the workspace owner");
    }

    [Fact]
    public async Task ExistingTaskQueries_ShouldBeUnaffected_ByTheQueryPredicatePlaceholder()
    {
        var all = await GetAllTasks();
        var searched = await GetTaskNamesMatching("OpenTelemetry");

        all.Items.Should().NotBeEmpty();
        all.TotalCount.Should().BeGreaterThan(searched.Count);
        searched.Should().NotBeEmpty();
    }

    private async Task<string> SeedForeignView(bool isShared)
    {
        using var scope = Fixture.CreateScope();

        var unitOfWork = scope.ServiceProvider.GetRequiredService<INetptuneUnitOfWork>();
        var taskViews = scope.ServiceProvider.GetRequiredService<ITaskViewRepository>();
        var workspace = await unitOfWork.Workspaces.GetBySlug("netptune", cancellationToken: TestContext.Current.CancellationToken);
        var name = $"Foreign {(isShared ? "shared" : "private")} view {Guid.NewGuid():N}";
        var view = new TaskView
        {
            Name = name,
            Slug = name.ToLowerInvariant().Replace(' ', '-'),
            WorkspaceId = workspace!.Id,
            IsShared = isShared,
            CreatedByUserId = "c3d4e5f6-a7b8-9012-cdef-123456789012",
            OwnerId = "c3d4e5f6-a7b8-9012-cdef-123456789012",
            Definition = JsonSerializer.SerializeToDocument(Definition(Group(
                Condition(TaskFieldKeys.Name, QueryOperator.Contains, "opentelemetry"))), JsonOptions.Default),
        };

        await taskViews.AddAsync(view, TestContext.Current.CancellationToken);
        await unitOfWork.CompleteAsync(TestContext.Current.CancellationToken);

        return view.Slug;
    }

    private async Task<TaskViewViewModel> CreateView(string name)
    {
        var response = await PostView(name);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await ReadViewResult(response);

        result.IsSuccess.Should().BeTrue();

        return result.Payload!;
    }

    private Task<HttpResponseMessage> PostView(string name)
    {
        return Client.PostAsJsonAsync("api/task-views", new
        {
            name,
            isShared = false,
            definition = Definition(Group(Condition(TaskFieldKeys.Name, QueryOperator.Contains, "opentelemetry"))),
        }, TestContext.Current.CancellationToken);
    }

    private async Task<List<TaskViewViewModel>> GetViews()
    {
        var response = await Client.GetAsync("api/task-views", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return (await response.Content.ReadFromJsonAsync<List<TaskViewViewModel>>(TestContext.Current.CancellationToken))!;
    }

    private async Task<ClientResponse<TaskViewViewModel>> GetView(string slug)
    {
        var response = await Client.GetAsync($"api/task-views/{slug}", TestContext.Current.CancellationToken);

        return await ReadViewResult(response);
    }

    private async Task<ClientResponse<TaskViewResultViewModel>> Preview(QueryGroup? query)
    {
        var response = await PostPreview(query);

        return await ReadResult(response);
    }

    private Task<HttpResponseMessage> PostPreview(QueryGroup? query)
    {
        return Client.PostAsJsonAsync("api/task-views/preview", new { query }, TestContext.Current.CancellationToken);
    }

    private static async Task<ClientResponse<TaskViewResultViewModel>> ReadResult(HttpResponseMessage response)
    {
        var result = await response.Content.ReadFromJsonAsync<ClientResponse<TaskViewResultViewModel>>(TestContext.Current.CancellationToken);

        return result;
    }

    private static async Task<ClientResponse<TaskViewViewModel>> ReadViewResult(HttpResponseMessage response)
    {
        var result = await response.Content.ReadFromJsonAsync<ClientResponse<TaskViewViewModel>>(TestContext.Current.CancellationToken);

        return result;
    }

    private async Task<PagedResponse<TaskViewModel>> GetAllTasks()
    {
        var response = await Client.GetAsync("api/tasks?pageSize=100", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<PagedResponse<TaskViewModel>>>(TestContext.Current.CancellationToken);

        return result.Payload!;
    }

    private async Task<List<string>> GetTaskNamesMatching(string term)
    {
        var all = await GetAllTasks();

        return all.Items
            .Where(task => task.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
            .Select(task => task.Name)
            .ToList();
    }

    private static TaskViewDefinition Definition(QueryGroup query)
    {
        return new TaskViewDefinition { Query = query };
    }

    private static QueryGroup Group(QueryCondition condition)
    {
        return new QueryGroup
        {
            Operator = QueryGroupOperator.All,
            Conditions = [condition],
        };
    }

    private static QueryCondition Condition(string field, QueryOperator queryOperator, params string[] values)
    {
        return new QueryCondition
        {
            Field = field,
            Operator = queryOperator,
            Values = [.. values],
        };
    }
}
