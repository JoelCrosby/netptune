using AutoFixture;

using FluentAssertions;

using Netptune.Core.UnitOfWork;
using Netptune.Services.ProjectTasks;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.ProjectTasks;

public class TaskReferenceResolverTests
{
    private const int WorkspaceId = 1;

    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly TaskReferenceResolver Resolver;

    public TaskReferenceResolverTests()
    {
        Resolver = new TaskReferenceResolver(UnitOfWork);
    }

    [Fact]
    public async Task ResolveAssignees_ShouldReportNoChange_AndQueryNothing_WhenIdsAreNull()
    {
        var resolution = await Resolver.ResolveAssignees(null, WorkspaceId, TestContext.Current.CancellationToken);

        resolution.IsValid.Should().BeTrue();
        resolution.ShouldUpdate.Should().BeFalse();
        resolution.UserIds.Should().BeEmpty();

        await UnitOfWork.Users.DidNotReceive().IsUserInWorkspaceRange(
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResolveAssignees_ShouldAskForEveryoneToBeCleared_AndQueryNothing_WhenIdsAreEmpty()
    {
        var resolution = await Resolver.ResolveAssignees([], WorkspaceId, TestContext.Current.CancellationToken);

        resolution.IsValid.Should().BeTrue();
        resolution.ShouldUpdate.Should().BeTrue();
        resolution.UserIds.Should().BeEmpty();

        await UnitOfWork.Users.DidNotReceive().IsUserInWorkspaceRange(
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResolveAssignees_ShouldReturnEveryMember_WhenAllAreInTheWorkspace()
    {
        SetupMembers("user-a", "user-b");

        var resolution = await Resolver.ResolveAssignees(
            ["user-a", "user-b"],
            WorkspaceId,
            TestContext.Current.CancellationToken);

        resolution.IsValid.Should().BeTrue();
        resolution.ShouldUpdate.Should().BeTrue();
        resolution.UserIds.Should().BeEquivalentTo(["user-a", "user-b"]);
    }

    [Fact]
    public async Task ResolveAssignees_ShouldFail_WhenAnIdIsNotAWorkspaceMember()
    {
        SetupMembers("user-a");

        var resolution = await Resolver.ResolveAssignees(
            ["user-a", "outsider"],
            WorkspaceId,
            TestContext.Current.CancellationToken);

        resolution.IsValid.Should().BeFalse();
        resolution.Error.Should().Contain("outsider");
    }

    [Theory]
    [InlineData("user-a", "user-a")]
    [InlineData("user-a", " ")]
    public async Task ResolveAssignees_ShouldFail_WhenIdsAreDuplicatedOrBlank(string first, string second)
    {
        var resolution = await Resolver.ResolveAssignees(
            [first, second],
            WorkspaceId,
            TestContext.Current.CancellationToken);

        resolution.IsValid.Should().BeFalse();
        resolution.Error.Should().Contain("empty or duplicated");
    }

    [Fact]
    public async Task ResolveAssignees_ShouldQueryMembershipOnce_HoweverManyIds()
    {
        SetupMembers("user-a", "user-b", "user-c");

        await Resolver.ResolveAssignees(
            ["user-a", "user-b", "user-c"],
            WorkspaceId,
            TestContext.Current.CancellationToken);

        await UnitOfWork.Users.Received(1).IsUserInWorkspaceRange(
            Arg.Any<IEnumerable<string>>(),
            WorkspaceId,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResolveTags_ShouldReportNoChange_AndQueryNothing_WhenNamesAreNull()
    {
        var resolution = await Resolver.ResolveTags(null, WorkspaceId, TestContext.Current.CancellationToken);

        resolution.IsValid.Should().BeTrue();
        resolution.ShouldUpdate.Should().BeFalse();
        resolution.Tags.Should().BeEmpty();

        await UnitOfWork.Tags.DidNotReceive().GetTagsByValueInWorkspace(
            Arg.Any<int>(),
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResolveTags_ShouldAskForEveryTagToBeCleared_AndQueryNothing_WhenNamesAreEmpty()
    {
        var resolution = await Resolver.ResolveTags([], WorkspaceId, TestContext.Current.CancellationToken);

        resolution.IsValid.Should().BeTrue();
        resolution.ShouldUpdate.Should().BeTrue();
        resolution.Tags.Should().BeEmpty();

        await UnitOfWork.Tags.DidNotReceive().GetTagsByValueInWorkspace(
            Arg.Any<int>(),
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResolveTags_ShouldReturnTheWorkspaceTags_WhenNamesAllExist()
    {
        SetupTags("backend", "frontend");

        var resolution = await Resolver.ResolveTags(
            ["backend", "frontend"],
            WorkspaceId,
            TestContext.Current.CancellationToken);

        resolution.IsValid.Should().BeTrue();
        resolution.ShouldUpdate.Should().BeTrue();
        resolution.Tags.Select(tag => tag.Name).Should().BeEquivalentTo(["backend", "frontend"]);
    }

    [Fact]
    public async Task ResolveTags_ShouldTrimNames_BeforeMatching()
    {
        SetupTags("backend");

        var resolution = await Resolver.ResolveTags(
            ["  backend  "],
            WorkspaceId,
            TestContext.Current.CancellationToken);

        resolution.IsValid.Should().BeTrue();
        resolution.Tags.Should().ContainSingle(tag => tag.Name == "backend");
    }

    [Fact]
    public async Task ResolveTags_ShouldFail_WhenANameIsNotAWorkspaceTag()
    {
        SetupTags("backend");

        var resolution = await Resolver.ResolveTags(
            ["backend", "missing-tag"],
            WorkspaceId,
            TestContext.Current.CancellationToken);

        resolution.IsValid.Should().BeFalse();
        resolution.Error.Should().Contain("missing-tag");
    }

    [Theory]
    [InlineData("backend", "backend")]
    [InlineData("backend", "   ")]
    public async Task ResolveTags_ShouldFail_WhenNamesAreDuplicatedOrBlank(string first, string second)
    {
        var resolution = await Resolver.ResolveTags([first, second], WorkspaceId, TestContext.Current.CancellationToken);

        resolution.IsValid.Should().BeFalse();
        resolution.Error.Should().Contain("empty or duplicated");
    }

    [Fact]
    public async Task ResolveTags_ShouldQueryTagsOnce_HoweverManyNames()
    {
        SetupTags("backend", "frontend", "infra");

        await Resolver.ResolveTags(
            ["backend", "frontend", "infra"],
            WorkspaceId,
            TestContext.Current.CancellationToken);

        await UnitOfWork.Tags.Received(1).GetTagsByValueInWorkspace(
            WorkspaceId,
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
    }

    private void SetupMembers(params string[] userIds)
    {
        var members = userIds
            .Select(id => AutoFixtures.AppUserFixture.With(user => user.Id, id).Create())
            .ToList();

        UnitOfWork.Users.IsUserInWorkspaceRange(
            Arg.Any<IEnumerable<string>>(),
            WorkspaceId,
            Arg.Any<CancellationToken>())
            .Returns(members);
    }

    private void SetupTags(params string[] names)
    {
        var tags = names.Select((name, index) => AutoFixtures.Tag with { Id = index + 1, Name = name }).ToList();

        UnitOfWork.Tags.GetTagsByValueInWorkspace(
            WorkspaceId,
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>())
            .Returns(tags);
    }
}
