using System.Text.Json;

namespace Netptune.Core.Preferences;

public static class PreferenceKeys
{
    public const string AppearanceTheme = "appearance.theme";
    public const string CommandPaletteRecentItemsScope = "commandPalette.recentItems.scope";
    public const string BoardHiddenGroupIds = "boards.hiddenGroupIds";
    public const string BoardTaskSort = "boards.taskSort";
}

public static class PreferenceScopes
{
    public const string Global = "global";
    public const string Workspace = "workspace";
}

public sealed record PreferenceGroupDefinition
{
    public required string Key { get; init; }

    public required string Label { get; init; }

    public int Order { get; init; }
}

public sealed record PreferenceOption
{
    public required string Value { get; init; }

    public required string Label { get; init; }
}

public sealed record PreferenceDefinition
{
    public required string Key { get; init; }

    public required string GroupKey { get; init; }

    public required string Label { get; init; }

    public required string ControlType { get; init; }

    public required string ValueType { get; init; }

    public JsonElement DefaultValue { get; init; }

    public IReadOnlyList<string> AllowedScopes { get; init; } = [];

    public IReadOnlyList<PreferenceOption> Options { get; init; } = [];

    public int Order { get; init; }

    // Internal preferences are persisted and resolved like any other, but are
    // driven by a dedicated UI (not the generic settings screen).
    public bool Internal { get; init; }
}

public sealed record PreferenceDefinitionsResponse
{
    public IReadOnlyList<PreferenceDefinitionGroup> Groups { get; init; } = [];
}

public sealed record PreferenceDefinitionGroup
{
    public required string Key { get; init; }

    public required string Label { get; init; }

    public int Order { get; init; }

    public IReadOnlyList<PreferenceDefinition> Preferences { get; init; } = [];
}

public sealed record PreferenceValuesResponse
{
    public IReadOnlyList<PreferenceValueGroup> Groups { get; init; } = [];
}

public sealed record PreferenceValueGroup
{
    public required string Key { get; init; }

    public required string Label { get; init; }

    public int Order { get; init; }

    public IReadOnlyList<ResolvedPreferenceValue> Preferences { get; init; } = [];
}

public sealed record ResolvedPreferenceValue
{
    public required PreferenceDefinition Definition { get; init; }

    public JsonElement? GlobalValue { get; init; }

    public JsonElement? WorkspaceValue { get; init; }

    public JsonElement EffectiveValue { get; init; }

    public required string Source { get; init; }
}

public sealed class UpdatePreferenceValueRequest
{
    public required string Scope { get; init; }

    public JsonElement Value { get; init; }
}

public sealed record CommandPaletteRecentItemResponse
{
    public required string Type { get; init; }

    public string? EntityId { get; init; }

    public required string Title { get; init; }

    public required string Url { get; init; }

    public DateTime LastAccessedAt { get; init; }
}

public sealed record CommandPaletteRecentItemsResponse
{
    public required string Scope { get; init; }

    public IReadOnlyList<CommandPaletteRecentItemResponse> Items { get; init; } = [];
}

public sealed record UpsertCommandPaletteRecentItemRequest
{
    public required string Type { get; init; }

    public string? EntityId { get; init; }

    public required string Title { get; init; }

    public required string Url { get; init; }
}
