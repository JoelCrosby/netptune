using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.Enums;

namespace Netptune.Core.Entities;

public sealed record AiUsageRecord : KeyedEntity<long>
{
    public int WorkspaceId { get; set; }

    public required string UserId { get; set; }

    public AiProvider Provider { get; set; }

    public required string Model { get; set; }

    public required string Purpose { get; set; }

    public int InputTokens { get; set; }

    public int OutputTokens { get; set; }

    public int CacheReadTokens { get; set; }

    public int CacheCreationTokens { get; set; }

    public DateTime CreatedAt { get; set; }

    #region NavigationProperties

    [JsonIgnore]
    public Workspace? Workspace { get; set; }

    [JsonIgnore]
    public AppUser? User { get; set; }

    #endregion
}

public static class AiUsagePurposes
{
    public const string PageTransform = "page_transform";

    public const string ImportMapping = "import_mapping";

    public const string TaskDescriptionWrite = "task_description_write";

    public const string PageWrite = "page_write";

    public const string TaskReview = "task_review";

    public const string ConversationRetry = "conversation_retry";
}
