using FluentAssertions;

using Netptune.Core.Models.Repository;
using Netptune.Core.Repositories.Common;
using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Users;
using Netptune.Handlers.Users.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Users.Queries;

public class GetAssigneesQueryHandlerTests
{
    private readonly GetAssigneesQueryHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();

    public GetAssigneesQueryHandlerTests()
    {
        Handler = new GetAssigneesQueryHandler(UnitOfWork, Identity);
    }

    [Fact]
    public async Task GetAssignees_ShouldReturnWorkspaceScopedPage()
    {
        const int workspaceId = 42;
        var filter = new AssigneeFilter
        {
            Page = 2,
            PageSize = 10,
            Search = "joel",
        };
        var assignee = new AssigneeViewModel
        {
            Id = "user-1",
            DisplayName = "Joel Crosby",
            PictureUrl = "https://example.com/joel.png",
        };
        IPagedResult<AssigneeViewModel> page = new PagedResult<AssigneeViewModel>
        {
            Results = [assignee],
            CurrentPage = 2,
            PageSize = 10,
            RowCount = 11,
            PageCount = 2,
        };

        Identity.GetWorkspaceId().Returns(workspaceId);
        UnitOfWork.Users
            .GetWorkspaceAssigneesPaged(
                workspaceId,
                filter,
                Arg.Any<CancellationToken>())
            .Returns(page);

        var result = await Handler.Handle(
            new GetAssigneesQuery(filter),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Items.Should().ContainSingle().Which.Should().BeEquivalentTo(assignee);
        result.Payload.Page.Should().Be(2);
        result.Payload.TotalCount.Should().Be(11);
        result.Payload.TotalPages.Should().Be(2);
    }
}
