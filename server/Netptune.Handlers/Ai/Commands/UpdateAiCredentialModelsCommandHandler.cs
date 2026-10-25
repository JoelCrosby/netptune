using Mediator;

using Netptune.Core.Localization;
using Netptune.Core.Requests.Ai;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Ai.Commands;

public sealed record UpdateAiCredentialModelsCommand(Guid CredentialId, UpdateAiCredentialModelsRequest Request)
    : IRequest<ClientResponse>;

public sealed class UpdateAiCredentialModelsCommandHandler
    : IRequestHandler<UpdateAiCredentialModelsCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public UpdateAiCredentialModelsCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse> Handle(
        UpdateAiCredentialModelsCommand command,
        CancellationToken cancellationToken)
    {
        var userId = Identity.GetCurrentUserId();
        var credential = await UnitOfWork.AiCredentials.GetOwned(command.CredentialId, userId, cancellationToken);

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
