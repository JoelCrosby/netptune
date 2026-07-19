using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Netptune.Core.Enums;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Core.ViewModels.RelationTypes;
using Netptune.Core.ViewModels.Relations;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

[Collection(WorkspaceMutationCollection.Name)]
public sealed class TaskRelationsEndpointTests
{
    private readonly HttpClient Client;

    public TaskRelationsEndpointTests(NetptuneFixture fixture)
    {
        Client = fixture.CreateNetptuneClient();
    }

    [Fact]
    public async Task GetRelationTypes_ShouldSeedDefaults()
    {
        var relationTypes = await GetRelationTypes();

        relationTypes.Should().NotBeEmpty();
        relationTypes.Should().Contain(type => type.Key == "blocks" && type.Category == RelationCategory.Dependency);
        relationTypes.Should().Contain(type => type.Key == "parent-of" && type.Category == RelationCategory.Hierarchy);
        relationTypes.Should().OnlyContain(type => type.IsSystem);

        var related = relationTypes.Single(type => type.Key == "relates-to");

        // Symmetric types read the same both ways.
        related.InverseName.Should().Be(related.Name);
    }

    [Fact]
    public async Task Link_ShouldReadForwardFromSource_AndInverseFromTarget()
    {
        var blocks = await GetRelationType("blocks");
        var tasks = await GetTasks(2);

        var response = await Link(tasks[0].SystemId, tasks[1].SystemId, blocks.Id);

        response.IsSuccess.Should().BeTrue();

        var fromSource = await GetRelations(tasks[0].SystemId);
        var fromTarget = await GetRelations(tasks[1].SystemId);

        // One stored row, read two ways.
        fromSource.Should().ContainSingle(relation => relation.Id == response.Payload!.Id)
            .Which.Label.Should().Be("Blocks");

        fromTarget.Should().ContainSingle(relation => relation.Id == response.Payload!.Id)
            .Which.Label.Should().Be("Is Blocked By");

        fromSource.Single(relation => relation.Id == response.Payload!.Id)
            .RelatedTask.SystemId.Should().Be(tasks[1].SystemId);

        fromTarget.Single(relation => relation.Id == response.Payload!.Id)
            .RelatedTask.SystemId.Should().Be(tasks[0].SystemId);

        await Unlink(response.Payload!.Id);
    }

    [Fact]
    public async Task Link_ShouldRejectCycle_AcrossAChainOfTasks()
    {
        var blocks = await GetRelationType("blocks");
        var tasks = await GetTasks(3);

        var first = await Link(tasks[0].SystemId, tasks[1].SystemId, blocks.Id);
        var second = await Link(tasks[1].SystemId, tasks[2].SystemId, blocks.Id);

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();

        // Closing the loop C -> A must be refused; the walk has to traverse two existing hops to see it.
        var closing = await Link(tasks[2].SystemId, tasks[0].SystemId, blocks.Id);

        closing.IsSuccess.Should().BeFalse();
        closing.Message.Should().Contain("circular");

        await Unlink(first.Payload!.Id);
        await Unlink(second.Payload!.Id);
    }

    [Fact]
    public async Task Link_ShouldRejectSelfRelation()
    {
        var blocks = await GetRelationType("blocks");
        var tasks = await GetTasks(1);

        var response = await Link(tasks[0].SystemId, tasks[0].SystemId, blocks.Id);

        response.IsSuccess.Should().BeFalse();
        response.Message.Should().Contain("cannot be related to itself");
    }

    [Fact]
    public async Task Link_ShouldRejectSecondParent_ForHierarchy()
    {
        var parentOf = await GetRelationType("parent-of");
        var tasks = await GetTasks(3);

        var first = await Link(tasks[0].SystemId, tasks[2].SystemId, parentOf.Id);

        first.IsSuccess.Should().BeTrue();

        // A different parent for the same child.
        var second = await Link(tasks[1].SystemId, tasks[2].SystemId, parentOf.Id);

        second.IsSuccess.Should().BeFalse();
        second.Message.Should().Contain("can only have one");

        await Unlink(first.Payload!.Id);
    }

    [Fact]
    public async Task Link_ShouldRejectDuplicate_RegardlessOfOrder_ForSymmetricTypes()
    {
        var relatesTo = await GetRelationType("relates-to");
        var tasks = await GetTasks(2);

        var first = await Link(tasks[0].SystemId, tasks[1].SystemId, relatesTo.Id);

        first.IsSuccess.Should().BeTrue();

        // Same pair, opposite order. A symmetric relation has no direction, so this is the same link.
        var reversed = await Link(tasks[1].SystemId, tasks[0].SystemId, relatesTo.Id);

        reversed.IsSuccess.Should().BeFalse();
        reversed.Message.Should().Contain("already linked");

        await Unlink(first.Payload!.Id);
    }

    [Fact]
    public async Task Unlink_ShouldRemoveTheRelation_FromBothTasks()
    {
        var blocks = await GetRelationType("blocks");
        var tasks = await GetTasks(2);

        var created = await Link(tasks[0].SystemId, tasks[1].SystemId, blocks.Id);

        created.IsSuccess.Should().BeTrue();

        var deleteResponse = await Unlink(created.Payload!.Id);

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        (await GetRelations(tasks[0].SystemId)).Should().NotContain(relation => relation.Id == created.Payload.Id);
        (await GetRelations(tasks[1].SystemId)).Should().NotContain(relation => relation.Id == created.Payload.Id);
    }

    private async Task<ClientResponse<TaskRelationViewModel>> Link(string sourceSystemId, string targetSystemId, int relationTypeId)
    {
        var response = await Client.PostAsJsonAsync("api/task-relations", new CreateTaskRelationRequest
        {
            SourceSystemId = sourceSystemId,
            TargetSystemId = targetSystemId,
            RelationTypeId = relationTypeId,
        });

        return (await response.Content.ReadFromJsonAsync<ClientResponse<TaskRelationViewModel>>());
    }

    private Task<HttpResponseMessage> Unlink(int id)
    {
        return Client.DeleteAsync($"api/task-relations/{id}");
    }

    private async Task<List<TaskRelationViewModel>> GetRelations(string systemId)
    {
        var response = await Client.GetAsync($"api/task-relations/{systemId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return (await response.Content.ReadFromJsonAsync<List<TaskRelationViewModel>>())!;
    }

    private async Task<List<RelationTypeViewModel>> GetRelationTypes()
    {
        var response = await Client.GetAsync("api/relation-types");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return (await response.Content.ReadFromJsonAsync<List<RelationTypeViewModel>>())!;
    }

    private async Task<RelationTypeViewModel> GetRelationType(string key)
    {
        var relationTypes = await GetRelationTypes();

        return relationTypes.Single(relationType => relationType.Key == key);
    }

    /// <summary>
    /// Distinct tasks to link. Each test unlinks what it created, so they don't collide.
    /// </summary>
    private async Task<List<TaskViewModel>> GetTasks(int count)
    {
        var response = await Client.GetAsync("api/tasks?pageSize=50");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<PagedResponse<TaskViewModel>>>();

        // Ordered by id so repeated calls pick the same tasks. The endpoint's default ordering is
        // not something these tests should be leaning on.
        var tasks = result.Payload!.Items.OrderBy(task => task.Id).ToList();

        tasks.Should().HaveCountGreaterThanOrEqualTo(count);

        return tasks.Take(count).ToList();
    }
}
