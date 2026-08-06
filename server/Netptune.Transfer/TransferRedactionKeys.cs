using System.Collections.Frozen;
using System.Reflection;

namespace Netptune.Transfer;

public static class TransferRedactionKeys
{
    public const string UserAccounts = "user-accounts";
    public const string RefreshTokens = "refresh-tokens";
    public const string ApiCredentials = "api-credentials";
    public const string ServiceAccounts = "service-accounts";
    public const string AiCredentials = "ai-credentials";
    public const string AiConversations = "ai-conversations";
    public const string AutomationHistory = "automation-history";
    public const string EventInfrastructure = "event-infrastructure";
    public const string DerivedProjections = "derived-projections";
    public const string PerUserState = "per-user-state";
    public const string PendingInvites = "pending-invites";
    public const string TransferHistory = "transfer-history";

    public static IReadOnlySet<string> All { get; } = typeof(TransferRedactionKeys)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(field => field.IsLiteral && field.FieldType == typeof(string))
        .Select(field => (string)field.GetRawConstantValue()!)
        .ToFrozenSet();
}
