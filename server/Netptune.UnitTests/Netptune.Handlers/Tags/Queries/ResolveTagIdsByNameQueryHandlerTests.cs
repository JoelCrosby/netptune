using FluentAssertions;

using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Tags;
using Netptune.Handlers.Tags.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Tags.Queries;

public class ResolveTagIdsByNameQueryHandlerTests
{
    private const int WorkspaceId = 3;

    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly ResolveTagIdsByNameQueryHandler Handler;

    public ResolveTagIdsByNameQueryHandlerTests()
    {
        Identity.GetWorkspaceId().Returns(WorkspaceId);

        List<TagViewModel> tags =
        [
            new() { Id = 1, Name = "Backend" },
            new() { Id = 2, Name = "Frontend" },
        ];

        UnitOfWork.Tags
            .GetViewModelsByNames(WorkspaceId, Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(tags);

        Handler = new ResolveTagIdsByNameQueryHandler(UnitOfWork, Identity);
    }

    [Fact]
    public async Task Handle_ShouldResolveNamesIgnoringCaseAndDuplicates()
    {
        var result = await Send(["backend", " Frontend ", "BACKEND"]);

        result.Error.Should().BeNull();
        result.Ids.Should().Equal(1, 2);
    }

    [Fact]
    public async Task Handle_ShouldListTheUnknownNames()
    {
        var result = await Send(["Backend", "Ops"]);

        result.UnknownNames.Should().Equal("Ops");
        result.Error.Should().Be("Tags were not found in the workspace: Ops.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public async Task Handle_ShouldRefuseABlankName(string? name)
    {
        var result = await Send(["Backend", name]);

        result.Error.Should().Be(ResolveTagIdsByNameQueryHandler.BlankNameMessage);
    }

    [Fact]
    public async Task Handle_ShouldRefuseTooManyNames()
    {
        var names = Enumerable.Range(0, ResolveTagIdsByNameQueryHandler.MaximumNames + 1).Select(index => $"tag {index}").ToList();

        var result = await Send([.. names]);

        result.Error.Should().Be(ResolveTagIdsByNameQueryHandler.TooManyNamesMessage);
        await UnitOfWork.Tags.DidNotReceive().GetViewModelsByNames(
            Arg.Any<int>(),
            Arg.Any<IReadOnlyCollection<string>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldResolveNothing_ForAnEmptyList()
    {
        var result = await Send([]);

        result.Error.Should().BeNull();
        result.Ids.Should().BeEmpty();
    }

    private async Task<TagNameResolution> Send(List<string?> names)
    {
        return await Handler.Handle(new ResolveTagIdsByNameQuery(names), TestContext.Current.CancellationToken);
    }
}
