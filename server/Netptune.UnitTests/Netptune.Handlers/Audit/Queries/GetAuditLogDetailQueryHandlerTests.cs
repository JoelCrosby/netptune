using FluentAssertions;

using Netptune.Core.Repositories;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Audit;
using Netptune.Handlers.Audit.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Audit.Queries;

public sealed class GetAuditLogDetailQueryHandlerTests
{
    private const int WorkspaceId = 7;

    private readonly IEventRecordRepository EventRecords = Substitute.For<IEventRecordRepository>();
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly GetAuditLogDetailQueryHandler Handler;

    public GetAuditLogDetailQueryHandlerTests()
    {
        UnitOfWork.EventRecords.Returns(EventRecords);
        Identity.GetWorkspaceId().Returns(WorkspaceId);
        Handler = new GetAuditLogDetailQueryHandler(UnitOfWork, Identity);
    }

    [Fact]
    public async Task Handle_ShouldReturnWorkspaceScopedDetail()
    {
        const long id = 24;
        var detail = new AuditLogDetailViewModel
        {
            Id = id,
            EventKey = "entity.field-transitioned",
            RetentionClass = "permanent",
        };
        EventRecords.GetAuditLogDetail(WorkspaceId, id, TestContext.Current.CancellationToken).Returns(detail);

        var result = await Handler.Handle(new GetAuditLogDetailQuery(id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Payload.Should().BeSameAs(detail);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenDetailDoesNotExistInWorkspace()
    {
        const long id = 25;
        EventRecords.GetAuditLogDetail(WorkspaceId, id, TestContext.Current.CancellationToken)
            .Returns((AuditLogDetailViewModel?)null);

        var result = await Handler.Handle(new GetAuditLogDetailQuery(id), TestContext.Current.CancellationToken);

        result.IsNotFound.Should().BeTrue();
    }
}
