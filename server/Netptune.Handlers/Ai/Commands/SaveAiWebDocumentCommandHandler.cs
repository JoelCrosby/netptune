using Mediator;

using Netptune.Core.Entities;
using Netptune.Core.Exceptions;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Ai.Commands;

public sealed record SaveAiWebDocumentCommand : IRequest<Guid?>
{
    public required string RequestedUrl { get; init; }

    public required string FinalUrl { get; init; }

    public string? Title { get; init; }

    public string? ContentType { get; init; }

    public required string Content { get; init; }

    public int RetentionHours { get; init; }
}

public sealed class SaveAiWebDocumentCommandHandler : IRequestHandler<SaveAiWebDocumentCommand, Guid?>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public SaveAiWebDocumentCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<Guid?> Handle(SaveAiWebDocumentCommand request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(workspaceKey, cancellationToken);

        if (workspaceId is null)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var document = new AiWebDocument
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId.Value,
            RequestedUrl = request.RequestedUrl,
            FinalUrl = request.FinalUrl,
            Title = request.Title,
            ContentType = request.ContentType,
            Content = request.Content,
            CharacterCount = request.Content.Length,
            FetchedAt = now,
            ExpiresAt = now.AddHours(request.RetentionHours),
        };

        await Store(document, cancellationToken);

        return document.Id;
    }

    private async Task Store(AiWebDocument document, CancellationToken cancellationToken)
    {
        try
        {
            await UnitOfWork.Transaction(async () =>
            {
                await UnitOfWork.AiWebDocuments.AddAsync(document, cancellationToken);
                await UnitOfWork.CompleteAsync(cancellationToken);
            });
        }
        catch (UniqueConstraintException)
        {
            UnitOfWork.AiWebDocuments.Detach(document);
        }
    }
}
