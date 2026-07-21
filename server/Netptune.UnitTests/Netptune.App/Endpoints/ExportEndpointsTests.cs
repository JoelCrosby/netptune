using Netptune.App.Endpoints;
using Netptune.Core.Events;
using Netptune.Core.Models.Files;
using Netptune.Core.Services;
using Netptune.Core.Services.Export;
using Netptune.Core.UnitOfWork;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.App.Endpoints;

public sealed class ExportEndpointsTests
{
    [Fact]
    public async Task HandleExportWorkspaceTasks_ShouldEmitExportRequested()
    {
        var exportService = Substitute.For<ITaskExportService>();
        var eventRecords = Substitute.For<IEventRecordWriter>();
        var unitOfWork = Substitute.For<INetptuneUnitOfWork>();
        var identity = Substitute.For<IIdentityService>();

        exportService.ExportWorkspaceTasks().Returns(new FileResponse
        {
            Stream = new MemoryStream(),
            ContentType = "text/csv",
            Filename = "tasks.csv",
        });
        identity.GetWorkspaceId().Returns(42);

        await ExportEndpoints.HandleExportWorkspaceTasks(
            exportService,
            eventRecords,
            unitOfWork,
            identity,
            TestContext.Current.CancellationToken);

        await eventRecords.Received(1).Append(
            Arg.Is<EventWriteRequest<ExportRequestedPayload>>(eventRequest =>
                eventRequest.EventKey == EventKeys.ExportRequested &&
                eventRequest.WorkspaceId == 42 &&
                eventRequest.Payload.ExportType == "workspace-tasks"),
            TestContext.Current.CancellationToken);
        await unitOfWork.Received(1).CompleteAsync(TestContext.Current.CancellationToken);
    }
}
