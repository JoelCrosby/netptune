using System.Text.Json;

using FluentAssertions;

using Netptune.Activity.Services;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Models.Activity;

using Xunit;

namespace Netptune.UnitTests.Netptune.Activity;

public class ActivityEntryMetaTests
{
    #region Build

    [Fact]
    public void Build_KeepsTheOldestOldValueAndTheNewestNewValue_PerField()
    {
        var meta = Parse(ActivityEntryMeta.Build([
            Change(TaskChangeField.Description, "v0", "v1"),
            Change(TaskChangeField.Description, "v1", "v2"),
            Change(TaskChangeField.Description, "v2", "v3"),
        ]));

        Field(meta, "description", "old").Should().Be("v0");
        Field(meta, "description", "new").Should().Be("v3");
    }

    [Fact]
    public void Build_TracksEachFieldSeparately()
    {
        var meta = Parse(ActivityEntryMeta.Build([
            Change(TaskChangeField.Description, "a", "b"),
            Change(TaskChangeField.Priority, "None", "High"),
        ]));

        Field(meta, "description", "new").Should().Be("b");
        Field(meta, "priority", "new").Should().Be("High");
    }

    [Fact]
    public void ChangedFields_ListsEachFieldOnce_InTheOrderItWasTouched()
    {
        var fields = ActivityEntryMeta.ChangedFields([
            Change(TaskChangeField.Description, "a", "b"),
            Change(TaskChangeField.Priority, "None", "High"),
            Change(TaskChangeField.Description, "b", "c"),
        ]);

        fields.Should().Equal("description", "priority");
    }

    [Fact]
    public void Build_CollectsExplicitRecipientsAcrossEvents()
    {
        var first = Change(TaskChangeField.Description, "a", "b", ["one", "two"]);
        var second = Change(TaskChangeField.Description, "b", "c", ["two", "three"]);

        var meta = Parse(ActivityEntryMeta.Build([first, second]));
        var recipients = meta.GetProperty("recipientUserIds")
            .EnumerateArray()
            .Select(item => item.GetString());

        recipients.Should().Equal("one", "two", "three");
    }

    #endregion

    #region NoOp

    [Fact]
    public void IsNoOpBurst_IsTrue_WhenEveryFieldEndedWhereItStarted()
    {
        var meta = JsonDocument.Parse(ActivityEntryMeta.Build([
            Change(TaskChangeField.Description, "original", "typo"),
            Change(TaskChangeField.Description, "typo", "original"),
        ]));

        ActivityEntryMeta.IsNoOpBurst(meta).Should().BeTrue();
    }

    [Fact]
    public void IsNoOpBurst_IsFalse_WhenAnyFieldActuallyMoved()
    {
        var meta = JsonDocument.Parse(ActivityEntryMeta.Build([
            Change(TaskChangeField.Description, "original", "typo"),
            Change(TaskChangeField.Description, "typo", "original"),
            Change(TaskChangeField.Priority, "None", "High"),
        ]));

        ActivityEntryMeta.IsNoOpBurst(meta).Should().BeFalse("one real change is enough to make the burst worth announcing");
    }

    [Fact]
    public void IsNoOpBurst_IsFalse_WhenThereIsNothingToCompare()
    {
        ActivityEntryMeta.IsNoOpBurst(null).Should().BeFalse();
        ActivityEntryMeta.IsNoOpBurst(JsonDocument.Parse("""{"fields":{}}""")).Should().BeFalse();
        ActivityEntryMeta.IsNoOpBurst(JsonDocument.Parse("""{"ipAddress":"127.0.0.1"}""")).Should().BeFalse();
    }

    [Fact]
    public void IsNoOpBurst_IsFalse_WhenTruncatedValuesMatchButTheFullValuesDoNot()
    {
        var prefix = new string('x', ActivityValue.MaxLength);

        var before = prefix + " the original fifth paragraph";
        var after = prefix + " a completely rewritten fifth paragraph";

        var meta = JsonDocument.Parse(ActivityEntryMeta.Build([Truncated(before, after)]));

        Field(meta.RootElement, "description", "old")
            .Should().Be(Field(meta.RootElement, "description", "new"), "the stored prefixes really are identical");

        ActivityEntryMeta.IsNoOpBurst(meta).Should().BeFalse("the hashes of the full values differ");
    }

    [Fact]
    public void IsNoOpBurst_IsTrue_WhenALongValueIsGenuinelyRestored()
    {
        var original = new string('x', ActivityValue.MaxLength) + " the original fifth paragraph";
        var edited = new string('x', ActivityValue.MaxLength) + " a rewritten fifth paragraph";

        var meta = JsonDocument.Parse(ActivityEntryMeta.Build([
            Truncated(original, edited),
            Truncated(edited, original),
        ]));

        ActivityEntryMeta.IsNoOpBurst(meta).Should().BeTrue("typed into a long description and undid it — nobody needs to hear about it");
    }

    [Fact]
    public void IsNoOpBurst_IsFalse_WhenOnlyOneSideWasTruncated()
    {
        var meta = JsonDocument.Parse(ActivityEntryMeta.Build([
            Truncated("short", new string('x', ActivityValue.MaxLength) + " and then some"),
        ]));

        ActivityEntryMeta.IsNoOpBurst(meta).Should().BeFalse();
    }

    #endregion

    private static ActivityEvent Change(
        TaskChangeField field,
        string? oldValue,
        string? newValue,
        List<string>? recipientUserIds = null) => new()
    {
        Field = field,
        OldValue = oldValue,
        NewValue = newValue,
        OccurredAt = DateTime.UtcNow,
        RecipientUserIds = recipientUserIds,
    };

    private static ActivityEvent Truncated(string? oldValue, string? newValue) => new ()
    {
        Field = TaskChangeField.Description,
        OldValue = ActivityValue.Truncate(oldValue),
        NewValue = ActivityValue.Truncate(newValue),
        OldValueHash = ActivityValue.HashIfTruncated(oldValue),
        NewValueHash = ActivityValue.HashIfTruncated(newValue),
        OccurredAt = DateTime.UtcNow,
    };

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;

    private static string? Field(JsonElement meta, string field, string key)
    {
        var value = meta.GetProperty("fields").GetProperty(field).GetProperty(key);

        return value.ValueKind is JsonValueKind.String ? value.GetString() : null;
    }
}
