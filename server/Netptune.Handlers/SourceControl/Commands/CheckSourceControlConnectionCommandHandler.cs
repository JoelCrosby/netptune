using Mediator;

using Microsoft.Extensions.Logging;

using Netptune.Core.Localization;
using Netptune.Core.Repositories;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.SourceControl;
using Netptune.Core.SourceControl.Checks;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.SourceControl;

namespace Netptune.Handlers.SourceControl.Commands;

public sealed record CheckSourceControlConnectionCommand(int ConnectionId)
    : IRequest<ClientResponse<SourceControlConnectionViewModel>>;

public sealed class CheckSourceControlConnectionCommandHandler
    : IRequestHandler<CheckSourceControlConnectionCommand, ClientResponse<SourceControlConnectionViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly ISourceControlProviderRegistry Registry;
    private readonly TimeProvider Clock;
    private readonly ILogger<CheckSourceControlConnectionCommandHandler> Logger;

    public CheckSourceControlConnectionCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        ISourceControlProviderRegistry registry,
        TimeProvider clock,
        ILogger<CheckSourceControlConnectionCommandHandler> logger)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Registry = registry;
        Clock = clock;
        Logger = logger;
    }

    public async ValueTask<ClientResponse<SourceControlConnectionViewModel>> Handle(
        CheckSourceControlConnectionCommand command,
        CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var connection = await UnitOfWork.SourceControlConnections.GetOwnedReadOnly(
            command.ConnectionId,
            workspaceId,
            cancellationToken);

        if (connection is null)
        {
            return ClientResponse<SourceControlConnectionViewModel>.NotFound;
        }

        var checker = SourceControlConnectionMapper.FindChecker(Registry, connection.Provider);

        if (checker is null)
        {
            var message = Localized.Format(AppText.ErrorProviderCannotCheckConnections, connection.Provider);

            return ClientResponse<SourceControlConnectionViewModel>.Failed(message);
        }

        var scmConnection = connection.ToScmConnection();
        var checks = await RunChecks(checker, scmConnection, cancellationToken);

        if (checks is null)
        {
            var message = Localized.Format(AppText.ErrorCouldNotCheckConnectionTryAgainShortly, connection.Provider);

            return ClientResponse<SourceControlConnectionViewModel>.Failed(message);
        }

        var record = new SourceControlConnectionCheckRecord
        {
            ConnectionId = connection.Id,
            CheckedCredential = connection.Credential,
            CheckedAt = Clock.GetUtcNow().UtcDateTime,
            Checks = ScmConnectionChecks.ToJson(checks),
        };

        var isRecorded = await UnitOfWork.SourceControlConnections.RecordCheck(record, cancellationToken);

        if (!isRecorded)
        {
            Logger.LogInformation(
                "Connection {ConnectionId} changed while it was checked, so the result was not stored",
                connection.Id);
        }

        var current = await UnitOfWork.SourceControlConnections.GetOwnedReadOnly(
            connection.Id,
            workspaceId,
            cancellationToken);

        if (current is null)
        {
            return ClientResponse<SourceControlConnectionViewModel>.NotFound;
        }

        var linkedRepos = await UnitOfWork.SourceControlRepos.GetLinkedForWorkspace(workspaceId, cancellationToken);
        var linkedCount = linkedRepos.Count(repo => repo.ConnectionId == current.Id);
        var viewModel = SourceControlConnectionMapper.ToViewModel(current, Registry, linkedCount);

        return ClientResponse<SourceControlConnectionViewModel>.Success(viewModel);
    }

    private async Task<IReadOnlyList<ScmConnectionCheck>?> RunChecks(
        IScmConnectionChecker checker,
        ScmConnection connection,
        CancellationToken cancellationToken)
    {
        try
        {
            var checks = await checker.Check(connection, cancellationToken);

            return checks;
        }
        catch (Exception exception) when (exception is ScmProviderException or ScmRateLimitedException)
        {
            Logger.LogWarning(exception, "Checking connection {ConnectionId} failed", connection.Id);

            return null;
        }
    }
}
