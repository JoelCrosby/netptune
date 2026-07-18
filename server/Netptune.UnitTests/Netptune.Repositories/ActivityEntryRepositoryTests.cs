using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

using Netptune.Core.Entities;
using Netptune.Entities.Contexts;
using Netptune.Entities.EntityMaps;
using Netptune.Repositories.Sql;

using Xunit;

namespace Netptune.UnitTests.Netptune.Repositories;

public class ActivityEntryRepositoryTests
{
    private readonly DataContext Context = new();

    // The read-optimised runtime model drops index metadata such as IsDescending; the design-time model is
    // the one the migration is generated from.
    private IModel Model => Context.GetService<IDesignTimeModel>().Model;

    #region Model

    [Fact]
    public void ActivityEntry_IsMappedToItsOwnTable()
    {
        var entityType = Model.FindEntityType(typeof(ActivityEntry));

        entityType.Should().NotBeNull();
        entityType.GetTableName().Should().Be("activity_entries");
    }

    [Fact]
    public void ActivityEntry_HasUniquePartialIndexOnOpenEntries()
    {
        var index = FindIndex("ux_activity_entries_open");

        index.Should().NotBeNull("the unique partial index is what makes the merge safe across replicas");
        index.IsUnique.Should().BeTrue();

        index.GetFilter().Should().Be("is_open AND NOT is_deleted");

        index.Properties
            .Select(property => property.Name)
            .Should()
            .Equal(
            nameof(ActivityEntry.WorkspaceId),
            nameof(ActivityEntry.EntityType),
            nameof(ActivityEntry.EntityId),
            nameof(ActivityEntry.UserId));
    }

    [Fact]
    public void ActivityEntry_HasSweeperClaimIndex_FilteredOnUnnotifiedEntries()
    {
        var index = FindIndex("ix_activity_entries_pending_window_expires");

        index.Should().NotBeNull();
        index.GetFilter().Should().Be("notified_at IS NULL");

        index.Properties
            .Select(property => property.Name)
            .Should()
            .Equal(nameof(ActivityEntry.WindowExpiresAt));
    }

    [Fact]
    public void ActivityEntry_HasFeedReadIndexOrderedNewestFirst()
    {
        var index = FindIndex("ix_activity_entries_entity_last_occurred_id");

        index.Should().NotBeNull();

        index.Properties
            .Select(property => property.Name)
            .Should()
            .Equal(
            nameof(ActivityEntry.EntityType),
            nameof(ActivityEntry.EntityId),
            nameof(ActivityEntry.LastOccurredAt),
            nameof(ActivityEntry.Id));

        index.IsDescending.Should().Equal(
            false,
            false,
            true,
            true);
    }

    [Fact]
    public void ActivityEntry_StoresChangedFieldsAsATextArray_AndMetaAsJsonb()
    {
        ColumnTypeOf(nameof(ActivityEntry.ChangedFields)).Should().Be("text[]");
        ColumnTypeOf(nameof(ActivityEntry.LastEventRecordId)).Should().Be("bigint");
        ColumnTypeOf(nameof(ActivityEntry.Meta)).Should().Be("jsonb");
    }

    #endregion

    #region ClaimScript

    [Fact]
    public void UpsertScript_IsASingleAtomicUpsertAgainstTheUniquePartialIndex()
    {
        var sql = ActivityEntryScripts.UpsertActivityEntry;

        Statements(sql).Should().ContainSingle("a read-then-write merge lets two replicas both open an entry for the same task");

        sql.Should().Contain("DO UPDATE SET");
        sql.Should().Contain("WHERE activity_entries.window_expires_at > @now");
    }

    [Fact]
    public void UpsertScript_ArbitratesOnExactlyTheFilterOfTheUniquePartialIndex()
    {
        var index = FindIndex(ActivityEntryEntityMap.OpenEntryIndexName);

        index!.GetFilter().Should().Be(ActivityEntryEntityMap.OpenEntryIndexFilter);

        ActivityEntryScripts.UpsertActivityEntry
            .Should().Contain(
                $"ON CONFLICT (workspace_id, entity_type, entity_id, user_id) WHERE {ActivityEntryEntityMap.OpenEntryIndexFilter}",
                "Postgres infers the arbiter index by proving the supplied predicate implies the index's");

        SqlScripts.UpsertActivityEntry
            .Should().NotContain(ActivityEntryEntityMap.OpenEntryIndexFilter, "the script must take the filter from the entity map, not spell it out");
    }

    [Fact]
    public void UpsertScript_BindsEveryValueByName()
    {
        ActivityEntryScripts.UpsertActivityEntry
            .Should().NotContain("{", "a positional parameter is a transposition waiting to happen")
            .And.NotContain("}");
    }

    [Fact]
    public void ClaimScript_IsASingleAtomicStatement()
    {
        Statements(ActivityEntryScripts.CloseExpiredActivityEntries)
            .Should().ContainSingle("a SELECT-then-UPDATE claim would let two replicas claim the same entry");
    }

    [Fact]
    public void ClaimScript_ClosesAndStampsOnlyExpiredUnnotifiedOpenEntries()
    {
        var sql = ActivityEntryScripts.CloseExpiredActivityEntries;

        sql.Should().Contain("is_open = FALSE");
        sql.Should().Contain("notified_at = NOW()");
        sql.Should().Contain("window_expires_at <= NOW()");
        sql.Should().Contain("notified_at IS NULL");
        sql.Should().Contain("FOR UPDATE SKIP LOCKED");
        sql.Should().Contain("RETURNING *");

        sql.Should().NotContain("WHERE is_open");
        sql.Should().NotContain("AND is_open");
    }

    #endregion

    private static string Generated(string sql)
    {
        return string.Join('\n', Lines(sql));
    }

    private static List<string> Statements(string sql)
    {
        return Generated(sql)
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(statement => statement.Length > 0)
            .ToList();
    }

    private static IEnumerable<string> Lines(string sql)
    {
        return sql
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith("--", StringComparison.Ordinal));
    }

    private IIndex? FindIndex(string name)
    {
        return Model
            .FindEntityType(typeof(ActivityEntry))!
            .GetIndexes()
            .FirstOrDefault(index => index.GetDatabaseName() == name);
    }

    private string ColumnTypeOf(string propertyName)
    {
        return Model
            .FindEntityType(typeof(ActivityEntry))!
            .FindProperty(propertyName)!
            .GetColumnType();
    }
}
