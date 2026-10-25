using Netptune.Core.Enums;
using Netptune.Core.Localization;
using Netptune.Core.Models.Ai;
using Netptune.Core.Requests.Ai;

namespace Netptune.Handlers.Ai.Commands;

// The trimmed values the caller should store, or the reason the request cannot be stored. Both the
// per-user and the per-workspace save apply the same rules; only the owner differs.
public sealed record ValidatedAiCredential
{
    public required string Secret { get; init; }

    public required string Label { get; init; }

    public string? Model { get; init; }

    public string? ReviewModel { get; init; }
}

public sealed record AiCredentialModels(string? Model, string? ReviewModel);

public static class SaveAiCredentialValidation
{
    private const int MinimumSecretLength = 8;
    private const int MaximumLabelLength = 128;

    public static (ValidatedAiCredential? Credential, string? Error) Validate(SaveAiCredentialRequest request)
    {
        var secret = request.Secret.Trim();
        var label = request.Label.Trim();
        var isKnownProvider = Enum.IsDefined(request.Provider);

        if (!isKnownProvider)
        {
            return (null, AppText.ErrorUnknownAIProvider);
        }

        if (secret.Length < MinimumSecretLength)
        {
            return (null, AppText.ErrorAPIKeyNotValid);
        }

        if (label.Length is 0 or > MaximumLabelLength)
        {
            return (null, Localized.Format(AppText.ErrorLabelMustBetweenCharacters, MaximumLabelLength));
        }

        var models = ValidateModels(request.Provider, request.Model, request.ReviewModel);

        if (models is null)
        {
            return (null, AppText.ErrorModelNotSupportedProvider);
        }

        var credential = new ValidatedAiCredential
        {
            Secret = secret,
            Label = label,
            Model = models.Model,
            ReviewModel = models.ReviewModel,
        };

        return (credential, null);
    }

    public static AiCredentialModels? ValidateModels(AiProvider provider, string? model, string? reviewModel)
    {
        var requested = new AiCredentialModels(model, reviewModel);
        var noStoredModels = new AiCredentialModels(null, null);

        return ValidateModelUpdate(provider, noStoredModels, requested);
    }

    // A model left as it is isn't checked against the catalogue again, so one that has since been retired
    // doesn't stop the other model from being changed.
    public static AiCredentialModels? ValidateModelUpdate(
        AiProvider provider,
        AiCredentialModels stored,
        AiCredentialModels requested)
    {
        var chatModel = NormaliseModel(requested.Model);
        var reviewModel = NormaliseModel(requested.ReviewModel);
        var isChatModelAccepted = IsAcceptedModel(provider, chatModel, stored.Model);
        var isReviewModelAccepted = IsAcceptedModel(provider, reviewModel, stored.ReviewModel);

        if (!isChatModelAccepted || !isReviewModelAccepted)
        {
            return null;
        }

        return new AiCredentialModels(chatModel, reviewModel);
    }

    private static bool IsAcceptedModel(AiProvider provider, string? model, string? storedModel)
    {
        var usesDefault = model is null;
        var isUnchanged = model == storedModel;
        var isSupported = AiModels.IsSupported(provider, model);

        return usesDefault || isUnchanged || isSupported;
    }

    private static string? NormaliseModel(string? model)
    {
        var trimmed = model?.Trim();

        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}
