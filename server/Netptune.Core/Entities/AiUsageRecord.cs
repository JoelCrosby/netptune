using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.Enums;

namespace Netptune.Core.Entities;

// Tokens spent by an assistant call made outside a conversation, such as rewriting page text. Conversation
// turns record theirs on their messages; these rows let the spend cap and summary count the rest.
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
}
