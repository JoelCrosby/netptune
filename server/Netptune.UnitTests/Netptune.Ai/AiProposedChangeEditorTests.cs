using System.Text.Json;

using FluentAssertions;

using Netptune.Core.Services.Ai;
using Netptune.Core.ViewModels.Ai;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class AiProposedChangeEditorTests
{
    [Fact]
    public void Apply_ShouldRewriteTheProposedValueInBothTheFieldsAndThePayload()
    {
        var result = Edit(
            "Update name on “Old name”",
            [AiChangeFields.Text("name", "Old name", "Assistant name")],
            """{"taskId":12,"name":"Assistant name"}""",
            [new AiChangeFieldEdit { Name = "name", Value = "Reviewer name" }]);

        result.IsSuccess.Should().BeTrue();
        After(result, "name").Should().Be("Reviewer name");
        Payload(result, "name").Should().Be("Reviewer name");
    }

    [Fact]
    public void Apply_ShouldLeaveTheValuesItWasNotAskedToChangeAlone()
    {
        var result = Edit(
            "Update name, description on “Old name”",
            [
                AiChangeFields.Text("name", "Old name", "Assistant name"),
                AiChangeFields.Text("description", null, "Assistant description"),
            ],
            """{"taskId":12,"name":"Assistant name","description":"Assistant description"}""",
            [new AiChangeFieldEdit { Name = "description", Value = "Reviewer description" }]);

        After(result, "name").Should().Be("Assistant name");
        After(result, "description").Should().Be("Reviewer description");
        Payload(result, "name").Should().Be("Assistant name");
    }

    [Fact]
    public void Apply_ShouldKeepTheBeforeSideOfTheDiffPointingAtWhatIsThereToday()
    {
        var result = Edit(
            "Update name on “Old name”",
            [AiChangeFields.Text("name", "Old name", "Assistant name")],
            """{"taskId":12,"name":"Assistant name"}""",
            [new AiChangeFieldEdit { Name = "name", Value = "Reviewer name" }]);

        Fields(result).Single().Before.Should().Be("Old name");
    }

    [Fact]
    public void Apply_ShouldRequoteASummaryThatNamesTheValueTheChangeCreates()
    {
        var result = Edit(
            "Create task “Assistant name”",
            [AiChangeFields.Text("name", null, "Assistant name")],
            """{"name":"Assistant name"}""",
            [new AiChangeFieldEdit { Name = "name", Value = "Reviewer name" }]);

        result.Summary.Should().Be("Create task “Reviewer name”");
    }

    [Fact]
    public void Apply_ShouldLeaveASummaryThatNamesTheEntityBeingChanged()
    {
        var result = Edit(
            "Update name on “Old name”",
            [AiChangeFields.Text("name", "Old name", "Assistant name")],
            """{"taskId":12,"name":"Assistant name"}""",
            [new AiChangeFieldEdit { Name = "name", Value = "Reviewer name" }]);

        result.Summary.Should().Be("Update name on “Old name”");
    }

    [Fact]
    public void Apply_ShouldRefuseAFieldTheChangeDoesNotPropose()
    {
        var result = Edit(
            "Update name on “Old name”",
            [AiChangeFields.Text("name", "Old name", "Assistant name")],
            """{"taskId":12,"name":"Assistant name"}""",
            [new AiChangeFieldEdit { Name = "priority", Value = "High" }]);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("priority");
    }

    [Fact]
    public void Apply_ShouldRefuseAValueThatIsNotPlainText()
    {
        var status = AiChangeFields.Values(
            "status",
            AiChangeValueKind.Status,
            [AiChangeFields.Status(1, "Todo")],
            [AiChangeFields.Status(2, "In progress")]);

        var result = Edit(
            "Update status on “Old name”",
            [status],
            """{"taskId":12,"statusId":2}""",
            [new AiChangeFieldEdit { Name = "status", Value = "Done" }]);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("text value");
    }

    [Fact]
    public void Apply_ShouldRefuseAnEmptyValue()
    {
        var result = Edit(
            "Update name on “Old name”",
            [AiChangeFields.Text("name", "Old name", "Assistant name")],
            """{"taskId":12,"name":"Assistant name"}""",
            [new AiChangeFieldEdit { Name = "name", Value = "   " }]);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("empty");
    }

    [Fact]
    public void Apply_ShouldRefuseAnEditWithNoFields()
    {
        var result = Edit(
            "Update name on “Old name”",
            [AiChangeFields.Text("name", "Old name", "Assistant name")],
            """{"taskId":12,"name":"Assistant name"}""",
            []);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Apply_ShouldRefuseAValueLongerThanTheColumnHolds()
    {
        var result = Edit(
            "Update description on “Old name”",
            [AiChangeFields.Text("description", null, "Assistant description")],
            """{"taskId":12,"description":"Assistant description"}""",
            [new AiChangeFieldEdit { Name = "description", Value = new string('a', 8001) }]);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("longer than");
    }

    [Fact]
    public void Apply_ShouldLeaveTheRestOfThePayloadIntact()
    {
        var result = Edit(
            "Update name on “Old name”",
            [AiChangeFields.Text("name", "Old name", "Assistant name")],
            """{"taskId":12,"name":"Assistant name","clear":["dueDate"]}""",
            [new AiChangeFieldEdit { Name = "name", Value = "Reviewer name" }]);

        var payload = result.Payload.RootElement;

        payload.GetProperty("taskId").GetInt32().Should().Be(12);
        payload.GetProperty("clear").EnumerateArray().Single().GetString().Should().Be("dueDate");
    }

    private static AiChangeEditResult Edit(
        string summary,
        List<AiChangeField> fields,
        string payload,
        List<AiChangeFieldEdit> edits)
    {
        var serialized = AiChangeFieldSerializer.Serialize(fields);
        var parsed = JsonDocument.Parse(payload);

        return AiProposedChangeEditor.Apply(summary, serialized, parsed, edits);
    }

    private static List<AiChangeFieldViewModel> Fields(AiChangeEditResult result)
    {
        return AiChangeFieldSerializer.Deserialize(result.Fields);
    }

    private static string? After(AiChangeEditResult result, string name)
    {
        return Fields(result).Single(field => field.Name == name).After;
    }

    private static string? Payload(AiChangeEditResult result, string name)
    {
        return result.Payload.RootElement.GetProperty(name).GetString();
    }
}
