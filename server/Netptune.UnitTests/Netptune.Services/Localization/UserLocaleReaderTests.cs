using System.Text.Json;

using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Preferences;
using Netptune.Core.Repositories;
using Netptune.Core.UnitOfWork;
using Netptune.Services.Localization;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Localization;

public class UserLocaleReaderTests
{
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IUserPreferenceRepository Preferences = Substitute.For<IUserPreferenceRepository>();
    private readonly UserLocaleReader Reader;

    public UserLocaleReaderTests()
    {
        UnitOfWork.UserPreferences.Returns(Preferences);
        Preferences
            .GetGlobalValues(
                Arg.Any<IReadOnlyCollection<string>>(),
                PreferenceKeys.AppearanceLocale,
                Arg.Any<CancellationToken>())
            .Returns(
            [
                Stored("french", "\"fr\""),
                Stored("retired", "\"ja\""),
                Stored("broken", "42"),
            ]);

        Reader = new UserLocaleReader(UnitOfWork);
    }

    [Fact]
    public async Task GetLocales_ShouldUseTheStoredLocale_AndEnglishForEveryoneElse()
    {
        var locales = await Reader.GetLocales(
            ["french", "retired", "broken", "never-chose"],
            TestContext.Current.CancellationToken);

        locales.Should().Equal(new Dictionary<string, string>
        {
            ["french"] = "fr",
            ["retired"] = "en-GB",
            ["broken"] = "en-GB",
            ["never-chose"] = "en-GB",
        });
    }

    [Fact]
    public async Task GetLocales_ShouldReadEveryUserInOneQuery()
    {
        await Reader.GetLocales(["a", "b", "c"], TestContext.Current.CancellationToken);

        await Preferences.Received(1).GetGlobalValues(
            Arg.Any<IReadOnlyCollection<string>>(),
            PreferenceKeys.AppearanceLocale,
            Arg.Any<CancellationToken>());
    }

    private static UserPreferenceValue Stored(string userId, string json)
    {
        return new UserPreferenceValue
        {
            UserId = userId,
            Key = PreferenceKeys.AppearanceLocale,
            Value = JsonDocument.Parse(json),
        };
    }
}
