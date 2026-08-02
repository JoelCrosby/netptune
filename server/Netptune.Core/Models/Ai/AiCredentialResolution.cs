using Netptune.Core.Entities;
using Netptune.Core.Enums;

namespace Netptune.Core.Models.Ai;

public sealed record AiResolvedCredential
{
    public Guid Id { get; init; }

    public AiProvider Provider { get; init; }

    public AiCredentialSource Source { get; init; }

    public string? Model { get; init; }

    public required byte[] Secret { get; init; }
}

public static class AiCredentialResolution
{
    public static List<AiResolvedCredential> Resolve(
        IEnumerable<UserAiCredential> userCredentials,
        IEnumerable<WorkspaceAiCredential> workspaceCredentials)
    {
        var resolved = new Dictionary<AiProvider, AiResolvedCredential>();

        foreach (var credential in workspaceCredentials)
        {
            resolved[credential.Provider] = new AiResolvedCredential
            {
                Id = credential.Id,
                Provider = credential.Provider,
                Source = AiCredentialSource.Workspace,
                Model = credential.Model,
                Secret = credential.Secret,
            };
        }

        foreach (var credential in userCredentials)
        {
            resolved[credential.Provider] = new AiResolvedCredential
            {
                Id = credential.Id,
                Provider = credential.Provider,
                Source = AiCredentialSource.User,
                Model = credential.Model,
                Secret = credential.Secret,
            };
        }

        return resolved.Values.OrderBy(credential => credential.Provider).ToList();
    }
}
