using System.Text.Json;

namespace Netptune.Core.Preferences;

public interface IPreferenceDefinitionRegistry
{
    IReadOnlyList<PreferenceDefinitionGroup> GetGroups();

    PreferenceDefinition? Find(string key);
}

public sealed class PreferenceDefinitionRegistry : IPreferenceDefinitionRegistry
{
    private readonly IReadOnlyList<PreferenceGroupDefinition> Groups =
    [
        new()
        {
            Key = "appearance",
            Label = "Appearance",
            Order = 5,
        },
        new()
        {
            Key = "commandPalette",
            Label = "Command Palette",
            Order = 10,
        },
        new()
        {
            Key = "boards",
            Label = "Boards",
            Order = 15,
        },
        new()
        {
            Key = "workspace",
            Label = "Workspace",
            Order = 20,
        },
    ];

    private readonly IReadOnlyList<PreferenceDefinition> Definitions =
    [
        new()
        {
            Key = PreferenceKeys.AppearanceTheme,
            GroupKey = "appearance",
            Label = "Theme",
            ControlType = "select",
            ValueType = "string",
            DefaultValue = JsonSerializer.SerializeToElement("light"),
            AllowedScopes = [PreferenceScopes.Global],
            Options =
            [
                new() { Value = "light", Label = "Light" },
                new() { Value = "dark", Label = "Dark" },
            ],
            Order = 10,
        },
        new()
        {
            Key = PreferenceKeys.CommandPaletteRecentItemsScope,
            GroupKey = "commandPalette",
            Label = "Recent items scope",
            ControlType = "select",
            ValueType = "string",
            DefaultValue = JsonSerializer.SerializeToElement("workspace"),
            AllowedScopes = [PreferenceScopes.Global, PreferenceScopes.Workspace],
            Options =
            [
                new() { Value = "workspace", Label = "Current workspace" },
                new() { Value = "global", Label = "All workspaces" },
            ],
            Order = 10,
        },
        new()
        {
            Key = PreferenceKeys.BoardHiddenGroupIds,
            GroupKey = "boards",
            Label = "Hidden board groups",
            ControlType = "hidden",
            // Map of board id -> hidden board-group ids, so each board keeps its
            // own hidden set within a single workspace-scoped preference.
            ValueType = "number-array-map",
            DefaultValue = JsonSerializer.SerializeToElement(new Dictionary<string, int[]>()),
            AllowedScopes = [PreferenceScopes.Workspace],
            Internal = true,
            Order = 10,
        },
        new()
        {
            Key = PreferenceKeys.BoardTaskSort,
            GroupKey = "boards",
            Label = "Board task sort",
            ControlType = "hidden",
            // Map of board id -> "field:direction" (e.g. "priority:desc"), so each
            // board keeps its own task sort within a single workspace-scoped preference.
            ValueType = "string-map",
            DefaultValue = JsonSerializer.SerializeToElement(new Dictionary<string, string>()),
            AllowedScopes = [PreferenceScopes.Workspace],
            Internal = true,
            Order = 20,
        },
        new()
        {
            Key = PreferenceKeys.WorkspaceLastVisited,
            GroupKey = "workspace",
            Label = "Last visited workspace",
            ControlType = "hidden",
            // Slug of the workspace the user was last in. Drives the redirect from a
            // blank url, and the badge on the workspace picker. Empty means none.
            ValueType = "string",
            DefaultValue = JsonSerializer.SerializeToElement(string.Empty),
            AllowedScopes = [PreferenceScopes.Global],
            Internal = true,
            Order = 10,
        },
    ];

    public IReadOnlyList<PreferenceDefinitionGroup> GetGroups()
    {
        return Groups
            .OrderBy(group => group.Order)
            .ThenBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new PreferenceDefinitionGroup
            {
                Key = group.Key,
                Label = group.Label,
                Order = group.Order,
                Preferences = Definitions
                    .Where(definition => definition.GroupKey == group.Key)
                    .OrderBy(definition => definition.Order)
                    .ThenBy(definition => definition.Key, StringComparer.Ordinal)
                    .ToList(),
            })
            .Where(group => group.Preferences.Count > 0)
            .ToList();
    }

    public PreferenceDefinition? Find(string key)
    {
        return Definitions.FirstOrDefault(definition =>
            string.Equals(definition.Key, key, StringComparison.Ordinal));
    }
}
