using System.Globalization;

using FluentAssertions;

using Netptune.Core.Enums;
using Netptune.Core.Localization;

using Xunit;

namespace Netptune.UnitTests.Netptune.Core.Localization;

public class LocalizedEnumTests
{
    [Fact]
    public void EveryLabelledEnumMember_ShouldHaveAnEnglishLabel()
    {
        List<IEnumerable<string>> keysByEnum =
        [
            KeysFor<StatusCategory>(),
            KeysFor<SprintStatus>(),
            KeysFor<RelationCategory>(),
            KeysFor<PageKind>(),
            KeysFor<FlagResolutionType>(),
            KeysFor<AutomationTriggerType>(),
            KeysFor<AutomationActionType>(),
        ];
        var missing = keysByEnum
            .SelectMany(keys => keys)
            .Where(key => AppText.ResourceManager.GetString(key, CultureInfo.InvariantCulture) is null)
            .ToList();

        missing.Should().BeEmpty("an unlabelled member falls back to its C# name");
    }

    [Fact]
    public void Label_ShouldFollowTheCurrentUiCulture()
    {
        using var culture = CultureScope.Use("fr");

        LocalizedEnum.Label(SprintStatus.Cancelled).Should().Be("Annulé");
    }

    private static IEnumerable<string> KeysFor<TEnum>() where TEnum : struct, Enum
    {
        return Enum.GetValues<TEnum>().Select(LocalizedEnum.KeyFor);
    }
}
