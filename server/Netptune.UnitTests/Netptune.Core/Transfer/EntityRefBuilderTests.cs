using FluentAssertions;

using Netptune.Transfer;

using Xunit;

namespace Netptune.UnitTests.Netptune.Core.Transfer;

public class EntityRefBuilderTests
{
    [Fact]
    public void SingleValueKeys_AreTrimmedAndLowerCased()
    {
        EntityRefBuilder.ForWorkspace("  Acme  ").ToString().Should().Be("workspace:acme");
        EntityRefBuilder.ForUser("Person@Acme.CO.UK").ToString().Should().Be("user:person@acme.co.uk");
        EntityRefBuilder.ForStatus("In-Progress").ToString().Should().Be("status:in-progress");
        EntityRefBuilder.ForTag(" Backend ").ToString().Should().Be("tag:backend");
        EntityRefBuilder.ForRelationType("Blocks").ToString().Should().Be("relation-type:blocks");
        EntityRefBuilder.ForProject("ACME").ToString().Should().Be("project:acme");
        EntityRefBuilder.ForBoard("Acme-Default-Board").ToString().Should().Be("board:acme-default-board");
        EntityRefBuilder.ForWorkspaceFile("AbC123").ToString().Should().Be("workspace-file:abc123");
    }

    [Fact]
    public void CompositeKeys_CombineTheirParents()
    {
        EntityRefBuilder.ForBoardGroup("acme-default-board", "In Progress")
            .ToString().Should().Be("board-group:acme-default-board/in-progress");

        EntityRefBuilder.ForSprint("acme", "Sprint 1")
            .ToString().Should().Be("sprint:acme/sprint-1");

        EntityRefBuilder.ForTask("ACME", 14)
            .ToString().Should().Be("task:acme-14");

        var taskRef = EntityRefBuilder.ForTask("acme", 14);

        EntityRefBuilder.ForComment(taskRef, 3)
            .ToString().Should().Be("comment:acme-14#3");
    }

    [Fact]
    public void SlugSegments_FallBackWhenTheNameHasNoSluggableCharacters()
    {
        EntityRefBuilder.ForAutomation("!!!").ToString()
            .Should().Be($"automation:{EntityRefBuilder.UnnamedSegment}");

        EntityRefBuilder.ForBoardGroup("acme-default-board", "???").ToString()
            .Should().Be($"board-group:acme-default-board/{EntityRefBuilder.UnnamedSegment}");
    }

    [Fact]
    public void Disambiguator_LeavesTheFirstOccurrenceUntouched()
    {
        var disambiguator = new EntityRefDisambiguator();
        var first = disambiguator.Disambiguate(EntityRefBuilder.ForSprint("acme", "Sprint 1"));

        first.ToString().Should().Be("sprint:acme/sprint-1");
        disambiguator.DisambiguatedCount.Should().Be(0);
    }

    [Fact]
    public void Disambiguator_SuffixesRepeatedRefsAndCountsThem()
    {
        var disambiguator = new EntityRefDisambiguator();
        var sprintRef = EntityRefBuilder.ForSprint("acme", "Sprint 1");

        disambiguator.Disambiguate(sprintRef).ToString().Should().Be("sprint:acme/sprint-1");
        disambiguator.Disambiguate(sprintRef).ToString().Should().Be("sprint:acme/sprint-1~2");
        disambiguator.Disambiguate(sprintRef).ToString().Should().Be("sprint:acme/sprint-1~3");

        disambiguator.DisambiguatedCount.Should().Be(2);
    }

    [Fact]
    public void Disambiguator_SkipsSuffixesAlreadyTakenByANaturalKey()
    {
        var disambiguator = new EntityRefDisambiguator();
        var tagRef = EntityRefBuilder.ForTag("Done");
        var collidingRef = EntityRefBuilder.ForTag("Done~2");

        disambiguator.Disambiguate(tagRef).ToString().Should().Be("tag:done");
        disambiguator.Disambiguate(collidingRef).ToString().Should().Be("tag:done~2");
        disambiguator.Disambiguate(tagRef).ToString().Should().Be("tag:done~3");

        disambiguator.DisambiguatedCount.Should().Be(1);
    }

    [Fact]
    public void Disambiguator_TracksEachRefTypeSeparately()
    {
        var disambiguator = new EntityRefDisambiguator();

        disambiguator.Disambiguate(EntityRefBuilder.ForTag("done")).ToString().Should().Be("tag:done");
        disambiguator.Disambiguate(EntityRefBuilder.ForStatus("done")).ToString().Should().Be("status:done");

        disambiguator.DisambiguatedCount.Should().Be(0);
    }
}
