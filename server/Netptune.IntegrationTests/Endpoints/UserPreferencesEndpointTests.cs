using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Netptune.Core.Preferences;
using Netptune.Core.Responses.Common;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

[Collection(UserPreferencesCollection.Name)]
public sealed class UserPreferencesEndpointTests
{
    private readonly HttpClient Client;

    public UserPreferencesEndpointTests(NetptuneFixture fixture)
    {
        Client = fixture.CreateNetptuneClient();
    }

    [Fact]
    public async Task Definitions_ShouldReturnRenderableCommandPaletteMetadata()
    {
        var response = await Client.GetAsync("api/user-preferences/definitions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PreferenceDefinitionsResponse>();
        var group = result!.Groups.Single(group => group.Key == "commandPalette");
        var preference = group.Preferences.Single();

        group.Key.Should().Be("commandPalette");
        preference.Key.Should().Be(PreferenceKeys.CommandPaletteRecentItemsScope);
        preference.ControlType.Should().Be("select");
        preference.DefaultValue.GetString().Should().Be("workspace");
        preference.Options.Select(option => option.Value).Should().Equal("workspace", "global");
    }

    [Fact]
    public async Task Definitions_ShouldReturnThemeAsGlobalSelectPreference()
    {
        var response = await Client.GetAsync("api/user-preferences/definitions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PreferenceDefinitionsResponse>();
        var preference = result!.Groups
            .Single(group => group.Key == "appearance")
            .Preferences
            .Single(item => item.Key == PreferenceKeys.AppearanceTheme);

        preference.ControlType.Should().Be("select");
        preference.AllowedScopes.Should().Equal("global");
        preference.DefaultValue.GetString().Should().Be("light");
        preference.Options.Select(option => option.Value).Should().Equal("light", "dark");
    }

    [Fact]
    public async Task Values_ShouldResolveMissingValuesToDefaults()
    {
        await ClearPreference("workspace");
        await ClearPreference("global");

        var preference = await GetCommandPalettePreference();

        preference.Source.Should().Be("default");
        preference.EffectiveValue.GetString().Should().Be("workspace");
        preference.GlobalValue.Should().BeNull();
        preference.WorkspaceValue.Should().BeNull();
    }

    [Fact]
    public async Task Values_ShouldReturnRenderablePreferenceGroups()
    {
        var response = await Client.GetAsync("api/user-preferences/values");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PreferenceValuesResponse>();
        var preferences = result!.Groups.SelectMany(group => group.Preferences).ToList();

        result.Groups.Select(group => group.Key).Should().Equal("appearance", "commandPalette", "boards", "workspace");
        preferences.Should().Contain(preference => preference.Definition.Key == PreferenceKeys.AppearanceTheme);
        preferences.Should().Contain(preference => preference.Definition.Key == PreferenceKeys.CommandPaletteRecentItemsScope);
        preferences.Should().Contain(preference => preference.Definition.Key == PreferenceKeys.BoardHiddenGroupIds);
        preferences
            .Where(preference => !preference.Definition.Internal)
            .Should().OnlyContain(preference => preference.Definition.Options.Count > 0);
    }

    [Fact]
    public async Task PutValue_ShouldResolveWorkspaceValueOverGlobalValue()
    {
        await ClearPreference("workspace");
        await ClearPreference("global");

        await SetPreference("global", "global");

        var globalPreference = await GetCommandPalettePreference();
        globalPreference.Source.Should().Be("global");
        globalPreference.EffectiveValue.GetString().Should().Be("global");

        await SetPreference("workspace", "workspace");

        var workspacePreference = await GetCommandPalettePreference();
        workspacePreference.Source.Should().Be("workspace");
        workspacePreference.GlobalValue!.Value.GetString().Should().Be("global");
        workspacePreference.WorkspaceValue!.Value.GetString().Should().Be("workspace");
        workspacePreference.EffectiveValue.GetString().Should().Be("workspace");
    }

    [Fact]
    public async Task DeleteValue_ShouldClearOnlyRequestedScope()
    {
        await ClearPreference("workspace");
        await ClearPreference("global");
        await SetPreference("global", "global");
        await SetPreference("workspace", "workspace");

        var response = await Client.DeleteAsync(
            $"api/user-preferences/values/{PreferenceKeys.CommandPaletteRecentItemsScope}?scope=workspace");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<ResolvedPreferenceValue>>();

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Source.Should().Be("global");
        result.Payload.EffectiveValue.GetString().Should().Be("global");
        result.Payload.GlobalValue!.Value.GetString().Should().Be("global");
        result.Payload.WorkspaceValue.Should().BeNull();
    }

    [Fact]
    public async Task PutValue_ShouldRejectInvalidOptionValue()
    {
        var response = await Client.PutAsJsonAsync(
            $"api/user-preferences/values/{PreferenceKeys.CommandPaletteRecentItemsScope}",
            new { scope = "workspace", value = "somewhere-else" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<ResolvedPreferenceValue>>();

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task PutValue_ShouldRejectInvalidKey()
    {
        var response = await Client.PutAsJsonAsync(
            "api/user-preferences/values/unknown.preference",
            new { scope = "global", value = "dark" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<ResolvedPreferenceValue>>();

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task PutValue_ShouldRejectInvalidScope()
    {
        var response = await Client.PutAsJsonAsync(
            $"api/user-preferences/values/{PreferenceKeys.CommandPaletteRecentItemsScope}",
            new { scope = "somewhere", value = "workspace" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<ResolvedPreferenceValue>>();

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task PutValue_ShouldRejectWorkspaceScope_WhenPreferenceAllowsGlobalOnly()
    {
        var response = await Client.PutAsJsonAsync(
            $"api/user-preferences/values/{PreferenceKeys.AppearanceTheme}",
            new { scope = "workspace", value = "dark" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<ResolvedPreferenceValue>>();

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task PutValue_ShouldRejectWrongValueType()
    {
        var response = await Client.PutAsJsonAsync(
            $"api/user-preferences/values/{PreferenceKeys.AppearanceTheme}",
            new { scope = "global", value = 123 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<ResolvedPreferenceValue>>();

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ThemePreference_ShouldUpdateAsGlobalSelectValue()
    {
        await Client.DeleteAsync($"api/user-preferences/values/{PreferenceKeys.AppearanceTheme}?scope=global");

        var response = await Client.PutAsJsonAsync(
            $"api/user-preferences/values/{PreferenceKeys.AppearanceTheme}",
            new { scope = "global", value = "dark" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<ResolvedPreferenceValue>>();

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Source.Should().Be("global");
        result.Payload.EffectiveValue.GetString().Should().Be("dark");
    }

    private async Task<ResolvedPreferenceValue> GetCommandPalettePreference()
    {
        var response = await Client.GetAsync("api/user-preferences/values");
        var result = await response.Content.ReadFromJsonAsync<PreferenceValuesResponse>();

        return result!.Groups
            .SelectMany(group => group.Preferences)
            .Single(preference => preference.Definition.Key == PreferenceKeys.CommandPaletteRecentItemsScope);
    }

    private async Task SetPreference(string scope, string value)
    {
        var response = await Client.PutAsJsonAsync(
            $"api/user-preferences/values/{PreferenceKeys.CommandPaletteRecentItemsScope}",
            new { scope, value });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task ClearPreference(string scope)
    {
        await Client.DeleteAsync(
            $"api/user-preferences/values/{PreferenceKeys.CommandPaletteRecentItemsScope}?scope={scope}");
    }
}
