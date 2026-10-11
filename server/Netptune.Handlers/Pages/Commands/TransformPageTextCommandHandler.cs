using System.Text.RegularExpressions;

using Mediator;

using Netptune.Core.Entities;
using Netptune.Core.Models.Ai;
using Netptune.Core.Requests.Pages;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Ai;
using Netptune.Core.UnitOfWork;
using Netptune.Handlers.Ai;

namespace Netptune.Handlers.Pages.Commands;

public sealed record TransformPageTextResult
{
    public required string Text { get; init; }
}

public sealed record TransformPageTextCommand(TransformPageTextRequest Request) : IRequest<ClientResponse<TransformPageTextResult>>;

// Rewrites a passage of a page with the assistant and hands the text back; nothing is saved, the editor
// applies it as an ordinary, undoable edit.
public sealed partial class TransformPageTextCommandHandler
    : IRequestHandler<TransformPageTextCommand, ClientResponse<TransformPageTextResult>>
{
    public const int MaxTextLength = 20_000;
    public const int MaxLanguageLength = 40;

    private static readonly Dictionary<string, AiPageTransformAction> Actions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["summarise"] = AiPageTransformAction.Summarise,
        ["tighten"] = AiPageTransformAction.Tighten,
        ["translate"] = AiPageTransformAction.Translate,
        ["continue"] = AiPageTransformAction.Continue,
    };

    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IAiPageTransformer Transformer;
    private readonly IAiCredentialProtector Protector;
    private readonly IAiSpendService Spend;

    public TransformPageTextCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IAiPageTransformer transformer,
        IAiCredentialProtector protector,
        IAiSpendService spend)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Transformer = transformer;
        Protector = protector;
        Spend = spend;
    }

    public async ValueTask<ClientResponse<TransformPageTextResult>> Handle(
        TransformPageTextCommand request,
        CancellationToken cancellationToken)
    {
        var input = request.Request;
        var isKnownAction = Actions.TryGetValue(input.Action ?? string.Empty, out var action);

        if (!isKnownAction)
        {
            return ClientResponse<TransformPageTextResult>.Failed("Unknown action.");
        }

        var text = input.Text?.Trim() ?? string.Empty;

        if (text.Length == 0)
        {
            return ClientResponse<TransformPageTextResult>.Failed("There is no text to work on.");
        }

        if (text.Length > MaxTextLength)
        {
            return ClientResponse<TransformPageTextResult>.Failed("Select a shorter passage; the assistant works on up to 20,000 characters.");
        }

        var language = input.Language?.Trim();
        var needsLanguage = action == AiPageTransformAction.Translate;
        var isLanguageName = IsLanguageName(language);
        var lacksLanguage = needsLanguage && !isLanguageName;

        if (lacksLanguage)
        {
            return ClientResponse<TransformPageTextResult>.Failed("Choose a language to translate into.");
        }

        var workspaceId = await Identity.GetWorkspaceId();
        var page = await UnitOfWork.Pages.GetLiveInWorkspace(input.PageId, workspaceId, true, cancellationToken);

        if (page is null)
        {
            return ClientResponse<TransformPageTextResult>.NotFound;
        }

        var workspace = await UnitOfWork.Workspaces.GetAsync(workspaceId, true, cancellationToken);

        var assistantEnabled = workspace?.AssistantEnabled == true;

        if (!assistantEnabled)
        {
            return ClientResponse<TransformPageTextResult>.Failed("The assistant is turned off for this workspace.");
        }

        var spend = await Spend.GetStatus(workspace!, cancellationToken);

        if (spend.IsOverCap)
        {
            return ClientResponse<TransformPageTextResult>.Failed(AiSpendMessages.CapReached);
        }

        var userId = Identity.GetCurrentUserId();
        var credential = await AiHandlerCredentials.Resolve(UnitOfWork, userId, workspaceId, cancellationToken);

        if (credential is null)
        {
            return ClientResponse<TransformPageTextResult>.Failed("No assistant key is set up for this workspace.");
        }

        var transformRequest = new AiPageTransformRequest
        {
            Provider = credential.Provider,
            ApiKey = Protector.Unprotect(credential.Secret),
            Action = action,
            Text = text,
            Language = needsLanguage ? language : null,
            PageTitle = page.Title,
        };
        var result = await Transformer.Transform(transformRequest, cancellationToken);
        var origin = new AiUsageOrigin(workspaceId, userId, AiUsagePurposes.PageTransform);

        // Booked whatever came back: the tokens were spent either way.
        await AiUsageRecorder.Record(UnitOfWork, origin, credential, result.Model, result.Usage, cancellationToken);
        await AiHandlerCredentials.MarkUsed(UnitOfWork, credential, cancellationToken);
        await UnitOfWork.CompleteAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(result.Text))
        {
            return ClientResponse<TransformPageTextResult>.Failed("The assistant returned nothing. Try again.");
        }

        return ClientResponse<TransformPageTextResult>.Success(new TransformPageTextResult { Text = result.Text });
    }

    // The language goes into the instructions, so only a plain language name is accepted.
    private static bool IsLanguageName(string? language)
    {
        var hasLanguage = !string.IsNullOrWhiteSpace(language);
        var isShort = language?.Length <= MaxLanguageLength;

        return hasLanguage && isShort && LanguageNamePattern().IsMatch(language!);
    }

    [GeneratedRegex(@"^[\p{L}][\p{L} \-]*$")]
    private static partial Regex LanguageNamePattern();
}
