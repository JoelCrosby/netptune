using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Requests;
using Netptune.Core.UnitOfWork;
using Netptune.Entities.Contexts;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

public sealed class AssigneeRepositoryTests
{
    private readonly NetptuneFixture Fixture;

    public AssigneeRepositoryTests(NetptuneFixture fixture)
    {
        Fixture = fixture;
    }

    [Fact]
    public async Task GetWorkspaceAssigneesPaged_ShouldSearchAndRemainWorkspaceScoped()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        using var scope = Fixture.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<INetptuneUnitOfWork>();

        var workspaceId = await context.Workspaces
            .Where(workspace => workspace.Slug == "netptune")
            .Select(workspace => workspace.Id)
            .SingleAsync(cancellationToken);

        var result = await unitOfWork.Users.GetWorkspaceAssigneesPaged(
            workspaceId,
            new AssigneeFilter
            {
                Search = "joel",
                Page = 1,
                PageSize = 10,
            },
            cancellationToken);

        result.Results.Should().ContainSingle();
        result.Results[0].DisplayName.Should().Be("joel crosby");
        result.Results[0].Id.Should().NotBeNullOrWhiteSpace();
        result.RowCount.Should().Be(1);
    }
}
