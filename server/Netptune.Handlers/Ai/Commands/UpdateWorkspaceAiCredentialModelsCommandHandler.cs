using Mediator;

using Netptune.Core.Localization;
using Netptune.Core.Requests.Ai;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Ai.Commands;

public sealed record UpdateWorkspaceAiCredentialModelsCommand(
    Guid CredentialId,
    UpdateAiCredentialModelsRequest Request)
    : IRequest<ClientResponse>;

public sealed class UpdateWorkspaceAiCredentialModelsCommandHandler
    : IRequestHandler<UpdateWorkspaceAiCredentialModelsCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public UpdateWorkspaceAiCredentialModelsCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse> Handle(
        UpdateWorkspaceAiCredentialModelsCommand command,
        CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var credential = await UnitOfWork.WorkspaceAiCredentials.GetOwned(
            command.CredentialId,
            workspaceId,
            cancellationToken);

        if (credential is null)
        {
            return ClientResponse.NotFound;
        }

        var stored = new AiCredentialModels(credential.Model, credential.ReviewModel);
        var requested = new AiCredentialModels(command.Request.Model, command.Request.ReviewModel);
        var models = SaveAiCredentialValidation.ValidateModelUpdate(credential.Provider, stored, requested);

        if (models is null)
        {
            return ClientResponse.Failed(AppText.ErrorModelNotSupportedProvider);
        }

        credential.Model = models.Model;
        credential.ReviewModel = models.ReviewModel;

        await UnitOfWork.CompleteAsync(cancellationToken);

        return ClientResponse.Success;
    }
}
