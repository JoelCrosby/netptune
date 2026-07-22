using FluentAssertions;

using Netptune.Core.Enums;
using Netptune.Core.Preferences;

using Xunit;

namespace Netptune.UnitTests.Netptune.Core.Preferences;

public class PreferenceDefinitionRegistryTests
{
    private readonly PreferenceDefinitionRegistry Registry = new();

    [Fact]
    public void GetGroups_ReturnsCommandPaletteDefinition()
    {
        var groups = Registry.GetGroups();

        var group = groups.Single(group => group.Key == "commandPalette");

        group.Key.Should().Be("commandPalette");
        group.Preferences.Should().ContainSingle();

        var preference = group.Preferences[0];
        preference.Key.Should().Be(PreferenceKeys.CommandPaletteRecentItemsScope);
        preference.DefaultValue.GetString().Should().Be("workspace");
        preference.AllowedScopes.Should().Equal(PreferenceScopes.Global, PreferenceScopes.Workspace);
        preference.Options.Select(option => option.Value).Should().Equal("workspace", "global");
    }

    [Fact]
    public void GetGroups_ReturnsAppearanceThemeSelectDefinition()
    {
        var group = Registry.GetGroups().Single(group => group.Key == "appearance");

        var preference = group.Preferences.Should().ContainSingle().Subject;

        preference.Key.Should().Be(PreferenceKeys.AppearanceTheme);
        preference.ControlType.Should().Be("select");
        preference.DefaultValue.GetString().Should().Be("light");
        preference.AllowedScopes.Should().Equal(PreferenceScopes.Global);
        preference.Options.Select(option => option.Value).Should().Equal("light", "dark");
    }

    [Fact]
    public void GetGroups_ReturnsNotificationToggle_ForEachNotifiableActivityType()
    {
        var group = Registry.GetGroups().Single(group => group.Key == "notifications");
        var mention = group.Preferences.Single(preference =>
            preference.Key == PreferenceKeys.NotificationEvent(ActivityType.Mention));

        mention.ControlType.Should().Be("toggle");
        mention.ValueType.Should().Be("boolean");
        mention.DefaultValue.GetBoolean().Should().BeTrue();
        mention.AllowedScopes.Should().Equal(PreferenceScopes.Global, PreferenceScopes.Workspace);
    }

    [Fact]
    public void Find_ReturnsNull_ForUnknownPreference()
    {
        Registry.Find("unknown.preference").Should().BeNull();
    }
}
