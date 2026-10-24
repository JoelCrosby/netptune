using FluentAssertions;

using Netptune.Ai.Tools;
using Netptune.Core.Localization;
using Netptune.Core.Services.Ai;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class AiFieldNamesTests
{
    [Fact]
    public void Describe_ShouldJoinTheLabelsOfEachField()
    {
        var fields = new List<AiChangeField>
        {
            new() { Name = "name" },
            new() { Name = "startDate" },
        };

        AiFieldNames.Describe(fields).Should().Be("name, start date");
    }

    [Fact]
    public void Label_ShouldFallBackToTheFieldName_WhenItHasNoLabel()
    {
        AiFieldNames.Label("somethingNew").Should().Be("somethingNew");
    }

    [Fact]
    public void Label_ShouldFollowTheCurrentUiCulture()
    {
        using var culture = CultureScope.Use("de");

        AiFieldNames.Label("startDate").Should().Be("Startdatum");
    }
}
