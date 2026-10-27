using FluentAssertions;

using Netptune.Ai.Tools;
using Netptune.Core.Enums;
using Netptune.Core.ViewModels.RelationTypes;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class AiRelationTypeLookupTests
{
    private static readonly List<RelationTypeViewModel> RelationTypes =
    [
        new()
        {
            Id = 1,
            Name = "Blocks",
            InverseName = "Is blocked by",
            Key = "blocks",
            Category = RelationCategory.Dependency,
        },
        new()
        {
            Id = 2,
            Name = "Relates to",
            InverseName = null!,
            Key = "relates-to",
            Category = RelationCategory.Related,
        },
    ];

    [Theory]
    [InlineData("--")]
    [InlineData("   ")]
    [InlineData("!?")]
    public void Match_ShouldMatchNothing_WhenTheNameHasNoLettersOrDigits(string name)
    {
        AiRelationTypeLookup.Match(RelationTypes, name).Should().BeNull();
        AiRelationTypeLookup.Exists(RelationTypes, name).Should().BeFalse();
    }

    [Theory]
    [InlineData("relates-to", 2, false)]
    [InlineData("Relates To", 2, false)]
    [InlineData("is blocked by", 1, true)]
    public void Match_ShouldFindATypeByNameKeyOrInverse(string name, int expectedId, bool isInverse)
    {
        var match = AiRelationTypeLookup.Match(RelationTypes, name);

        match!.RelationType.Id.Should().Be(expectedId);
        match.IsInverse.Should().Be(isInverse);
    }
}
