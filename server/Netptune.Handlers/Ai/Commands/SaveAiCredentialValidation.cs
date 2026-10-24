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
}

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

        var model = request.Model?.Trim();
        var hasModel = !string.IsNullOrWhiteSpace(model);
        var isUnsupportedModel = hasModel && !AiModels.IsSupported(request.Provider, model);

        if (isUnsupportedModel)
        {
            return (null, AppText.ErrorModelNotSupportedProvider);
        }

        var credential = new ValidatedAiCredential
        {
            Secret = secret,
            Label = label,
            Model = hasModel ? model : null,
        };

        return (credential, null);
    }
}
