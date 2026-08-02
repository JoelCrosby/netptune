using FluentAssertions;

using Netptune.Core.Services.Ai;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class AiChangeFieldsTests
{
    [Fact]
    public void Values_ShouldRenderTheDisplayTextFromTheValuesBehindIt()
    {
        var field = AiChangeFields.Values(
            "assignees",
            AiChangeValueKind.User,
            [AiChangeFields.User("user-1", "Ada Lovelace")],
            [
                AiChangeFields.User("user-2", "Alan Turing"),
                AiChangeFields.User("user-3", "Grace Hopper"),
            ]);

        field.Kind.Should().Be(AiChangeValueKind.User);
        field.Before.Should().Be("Ada Lovelace");
        field.After.Should().Be("Alan Turing, Grace Hopper");
        field.AfterValues!.Select(value => value.Id).Should().BeEquivalentTo(["user-2", "user-3"]);
    }

    [Fact]
    public void Values_ShouldRenderAnEmptySideAsNothingRatherThanASentinel()
    {
        var field = AiChangeFields.Values(
            "tags",
            AiChangeValueKind.Tag,
            [AiChangeFields.Tag("urgent")],
            []);

        field.After.Should().BeNull();
        field.AfterValues.Should().BeEmpty();
        field.BeforeValues.Should().ContainSingle();
    }

    [Fact]
    public void Date_ShouldCarryTheDateAsAnIsoValue()
    {
        var field = AiChangeFields.Date("dueDate", null, new DateOnly(2026, 7, 21));

        field.Kind.Should().Be(AiChangeValueKind.Date);
        field.After.Should().Be("2026-07-21");
        field.AfterValues!.Single().Display.Should().Be("2026-07-21");
        field.BeforeValues.Should().BeEmpty();
    }

    [Fact]
    public void Date_ShouldReadAClearedDateAsAnEmptySide()
    {
        var field = AiChangeFields.Date("startDate", new DateOnly(2026, 7, 1), (DateOnly?)null);

        field.Before.Should().Be("2026-07-01");
        field.After.Should().BeNull();
        field.AfterValues.Should().BeEmpty();
    }

    [Fact]
    public void Status_ShouldCarryTheIdAndColourAlongsideTheName()
    {
        var value = AiChangeFields.Status(4, "In progress", "#3b82f6");

        value.Id.Should().Be("4");
        value.Display.Should().Be("In progress");
        value.Color.Should().Be("#3b82f6");
    }

    [Fact]
    public void Task_ShouldLeadWithTheSystemIdWhenThereIsOne()
    {
        AiChangeFields.Task(9, "NPT-9", "Fix login").Display.Should().Be("NPT-9 · Fix login");
        AiChangeFields.Task(null, null, "Fix login").Display.Should().Be("Fix login");
    }

    [Fact]
    public void Serializer_ShouldCarryTypedValuesThroughStorageAndBack()
    {
        var fields = new List<AiChangeField>
        {
            AiChangeFields.Values(
                "status",
                AiChangeValueKind.Status,
                [AiChangeFields.Status(1, "New", "#94a3b8")],
                [AiChangeFields.Status(4, "In progress", "#3b82f6")]),
            AiChangeFields.Values(
                "assignees",
                AiChangeValueKind.User,
                [],
                [AiChangeFields.User("user-1", "Ada Lovelace", "https://example.test/ada.png")]),
        };

        using var stored = AiChangeFieldSerializer.Serialize(fields);

        var parsed = AiChangeFieldSerializer.Deserialize(stored);

        parsed.Should().HaveCount(2);

        var status = parsed[0];

        status.Kind.Should().Be(AiChangeValueKind.Status);
        status.BeforeValues!.Single().Color.Should().Be("#94a3b8");
        status.AfterValues!.Single().Id.Should().Be("4");
        status.After.Should().Be("In progress");

        var assignees = parsed[1];

        assignees.Kind.Should().Be(AiChangeValueKind.User);
        assignees.BeforeValues.Should().BeEmpty();
        assignees.AfterValues!.Single().PictureUrl.Should().Be("https://example.test/ada.png");
    }

    [Fact]
    public void Serializer_ShouldReadAFieldStoredBeforeValuesWereTyped()
    {
        using var stored = System.Text.Json.JsonDocument.Parse(
            """[{"name":"tags","before":"none","after":"urgent, backend"}]""");

        var parsed = AiChangeFieldSerializer.Deserialize(stored);

        parsed.Single().Kind.Should().Be(AiChangeValueKind.Text);
        parsed.Single().BeforeValues.Should().BeNull();
        parsed.Single().After.Should().Be("urgent, backend");
    }

    [Fact]
    public void Text_ShouldStayUntyped()
    {
        var field = AiChangeFields.Text("name", "Old", "New");

        field.Kind.Should().Be(AiChangeValueKind.Text);
        field.BeforeValues.Should().BeNull();
        field.AfterValues.Should().BeNull();
    }
}
