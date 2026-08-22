using Netptune.Transfer.Entities;
using System.Collections.Frozen;

using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

using Netptune.Core.Entities;
using Netptune.Core.Relationships;

namespace Netptune.Transfer;

public static class TransferRedaction
{
    private static readonly TransferEntityClassification[] Declared =
    [
        Exported<Workspace>(),
        Exported<Project>(),
        Exported<Board>(),
        Exported<BoardGroup>(),
        Exported<Sprint>(),
        Exported<Status>(),
        Exported<Tag>(),
        Exported<RelationType>(),
        Exported<ProjectTask>(),
        Exported<ProjectTaskAppUser>(),
        Exported<ProjectTaskTag>(),
        Exported<ProjectTaskInBoardGroup>(),
        Exported<ProjectTaskRelation>(),
        Exported<Comment>(),
        Exported<CommentMention>(),
        Exported<Reaction>(),
        Exported<Flag>(),
        Exported<AutomationRule>(),
        Exported<AutomationAction>(),
        Exported<WorkspaceFile>(),
        Exported<TaskFile>(),
        Exported<WorkspaceAppUser>(),
        Exported<ProjectUser>(),
        Exported<EventRecord>(),
        Exported<EventReference>(),

        ReferenceOnly<AppUser>(TransferRedactionKeys.UserAccounts),

        Redacted<IdentityRole>(TransferRedactionKeys.UserAccounts),
        Redacted<IdentityUserRole<string>>(TransferRedactionKeys.UserAccounts),
        Redacted<IdentityUserClaim<string>>(TransferRedactionKeys.UserAccounts),
        Redacted<IdentityUserLogin<string>>(TransferRedactionKeys.UserAccounts),
        Redacted<IdentityUserToken<string>>(TransferRedactionKeys.UserAccounts),
        Redacted<IdentityRoleClaim<string>>(TransferRedactionKeys.UserAccounts),
        Redacted<IdentityUserPasskey<string>>(TransferRedactionKeys.UserAccounts),
        Redacted<RefreshToken>(TransferRedactionKeys.RefreshTokens),
        Redacted<ApiCredential>(TransferRedactionKeys.ApiCredentials),
        Redacted<ServiceAccount>(TransferRedactionKeys.ServiceAccounts),
        Redacted<ServiceAccountOwner>(TransferRedactionKeys.ServiceAccounts),
        Redacted<UserAiCredential>(TransferRedactionKeys.AiCredentials),
        Redacted<WorkspaceAiCredential>(TransferRedactionKeys.AiCredentials),
        Redacted<AiConversation>(TransferRedactionKeys.AiConversations),
        Redacted<AiMessage>(TransferRedactionKeys.AiConversations),
        Redacted<AiToolInvocation>(TransferRedactionKeys.AiConversations),
        Redacted<AiChangeSet>(TransferRedactionKeys.AiConversations),
        Redacted<AiProposedChange>(TransferRedactionKeys.AiConversations),
        Redacted<AiWebDocument>(TransferRedactionKeys.AiConversations),
        Redacted<WorkspaceSearchCredential>(TransferRedactionKeys.AiCredentials),
        Redacted<DataProtectionKey>(TransferRedactionKeys.EncryptionKeys),
        Redacted<AutomationRun>(TransferRedactionKeys.AutomationHistory),
        Redacted<AutomationActionResult>(TransferRedactionKeys.AutomationHistory),
        Redacted<ScheduledAutomationAction>(TransferRedactionKeys.AutomationHistory),
        Redacted<EventOutbox>(TransferRedactionKeys.EventInfrastructure),
        Redacted<EventStreamHead>(TransferRedactionKeys.EventInfrastructure),
        Redacted<EventConsumerReceipt>(TransferRedactionKeys.EventInfrastructure),
        Redacted<ActivityEntry>(TransferRedactionKeys.DerivedProjections),
        Redacted<Notification>(TransferRedactionKeys.PerUserState),
        Redacted<UserPreferenceValue>(TransferRedactionKeys.PerUserState),
        Redacted<CommandPaletteRecentItem>(TransferRedactionKeys.PerUserState),
        Redacted<WorkspaceInvite>(TransferRedactionKeys.PendingInvites),
        Redacted<ExportJob>(TransferRedactionKeys.TransferHistory),
        Redacted<ExportDefinition>(TransferRedactionKeys.TransferHistory),
        Redacted<ImportSession>(TransferRedactionKeys.TransferHistory),
        Redacted<ImportSessionEntry>(TransferRedactionKeys.TransferHistory),
        Redacted<ImportDefinition>(TransferRedactionKeys.TransferHistory),
        Redacted<TaskView>(TransferRedactionKeys.SavedViews),
    ];

    private static readonly FrozenDictionary<Type, TransferEntityClassification> ClassificationsByType =
        Declared.ToFrozenDictionary(classification => classification.EntityType);

    public static IReadOnlyList<TransferEntityClassification> All { get; } = Declared;

    public static IReadOnlyList<string> RedactionKeys { get; } = Declared
        .Where(classification => classification.RedactionKey is not null)
        .Select(classification => classification.RedactionKey!)
        .Distinct()
        .Order(StringComparer.Ordinal)
        .ToList();

    public static TransferEntityClassification? Classify(Type entityType)
    {
        return ClassificationsByType.GetValueOrDefault(entityType);
    }

    public static bool IsClassified(Type entityType)
    {
        return ClassificationsByType.ContainsKey(entityType);
    }

    public static bool IsExportable(Type entityType)
    {
        var classification = Classify(entityType);

        return classification?.Disposition == TransferEntityDisposition.Exported;
    }

    private static TransferEntityClassification Exported<TEntity>()
    {
        return new TransferEntityClassification
        {
            EntityType = typeof(TEntity),
            Disposition = TransferEntityDisposition.Exported,
        };
    }

    private static TransferEntityClassification ReferenceOnly<TEntity>(string redactionKey)
    {
        return new TransferEntityClassification
        {
            EntityType = typeof(TEntity),
            Disposition = TransferEntityDisposition.ReferenceOnly,
            RedactionKey = redactionKey,
        };
    }

    private static TransferEntityClassification Redacted<TEntity>(string redactionKey)
    {
        return new TransferEntityClassification
        {
            EntityType = typeof(TEntity),
            Disposition = TransferEntityDisposition.Redacted,
            RedactionKey = redactionKey,
        };
    }
}
