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
            return (null, "Unknown AI provider.");
        }

        if (secret.Length < MinimumSecretLength)
        {
            return (null, "API key is not valid.");
        }

        if (label.Length is 0 or > MaximumLabelLength)
        {
            return (null, $"Label must be between 1 and {MaximumLabelLength} characters.");
        }

        var model = request.Model?.Trim();
        var hasModel = !string.IsNullOrWhiteSpace(model);
        var isUnsupportedModel = hasModel && !AiModels.IsSupported(request.Provider, model);

        if (isUnsupportedModel)
        {
            return (null, "Model is not supported for this provider.");
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
