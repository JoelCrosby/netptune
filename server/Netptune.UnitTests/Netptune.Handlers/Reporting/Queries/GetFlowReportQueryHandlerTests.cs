using FluentAssertions;

using Netptune.Core.Models.Reporting;
using Netptune.Core.Repositories;
using Netptune.Core.Services.Reporting;
using Netptune.Core.UnitOfWork;
using Netptune.Handlers.Reporting.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Reporting.Queries;

public sealed class GetFlowReportQueryHandlerTests
{
    private readonly IReportingRepository Reports = Substitute.For<IReportingRepository>();
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IReportingScopeResolver ScopeResolver = Substitute.For<IReportingScopeResolver>();
    private readonly ReportingScope Scope = new(5, new HashSet<int> { 9 });

    public GetFlowReportQueryHandlerTests()
    {
        UnitOfWork.Reports.Returns(Reports);
    }

    [Fact]
    public async Task Handle_ShouldReturnReport_ForResolvedWorkspaceScope()
    {
        var filter = new ReportingFilter();
        var report = new FlowReport
        {
            Throughput = 3,
            MedianCycleTimeHours = 12,
            P85CycleTimeHours = 18,
            CycleTimeSampleSize = 3,
            Coverage = new ReportingCoverage(DateTime.UtcNow, false),
        };
        ScopeResolver.Resolve(TestContext.Current.CancellationToken).Returns(Scope);
        Reports.GetFlow(
                Arg.Is<ReportingScope>(scope => scope.WorkspaceId == Scope.WorkspaceId && scope.ProjectIds.SetEquals(Scope.ProjectIds)),
                filter,
                TestContext.Current.CancellationToken)
            .Returns(report);
        var handler = new GetFlowReportQueryHandler(UnitOfWork, ScopeResolver);

        var result = await handler.Handle(new GetFlowReportQuery(filter), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Payload.Should().BeSameAs(report);
    }

    [Fact]
    public async Task Handle_ShouldReturnBadRequest_WhenFilterIsInvalid()
    {
        var filter = new ReportingFilter();
        ScopeResolver.Resolve(TestContext.Current.CancellationToken).Returns(Scope);
        Reports.GetFlow(
                Arg.Any<ReportingScope>(),
                filter,
                TestContext.Current.CancellationToken)
            .Returns<Task<FlowReport>>(_ => throw new InvalidReportingFilterException("Invalid range"));
        var handler = new GetFlowReportQueryHandler(UnitOfWork, ScopeResolver);

        var result = await handler.Handle(new GetFlowReportQuery(filter), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.IsNotFound.Should().BeFalse();
        result.Message.Should().Be("Invalid range");
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenScopeCannotBeResolved()
    {
        ScopeResolver.Resolve(TestContext.Current.CancellationToken).Returns((ReportingScope?)null);
        var handler = new GetFlowReportQueryHandler(UnitOfWork, ScopeResolver);

        var result = await handler.Handle(new GetFlowReportQuery(new ReportingFilter()), TestContext.Current.CancellationToken);

        result.IsNotFound.Should().BeTrue();
    }
}
