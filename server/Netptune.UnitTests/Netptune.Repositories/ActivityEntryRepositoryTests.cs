using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Activity;
using Netptune.Entities.Contexts;
using Netptune.Entities.EntityMaps;
using Netptune.Repositories;
using Netptune.Repositories.Sql;

using Xunit;

namespace Netptune.UnitTests.Netptune.Repositories;

public class ActivityEntryRepositoryTests
{
    private readonly DataContext Context = new();

    // The read-optimised runtime model drops index metadata such as IsDescending; the design-time model is
    // the one the migration is generated from.
    private IModel Model => Context.GetService<IDesignTimeModel>().Model;

    private static readonly DateTime Now = new (2026, 7, 12, 10, 0, 0, DateTimeKind.Utc);

    private const int WorkspaceId = 1;
    private const int EntityId = 99;
    private const int ProjectId = 31;
    private const int BoardId = 32;
    private const int BoardGroupId = 33;
    private const int TaskId = 34;
    private const string ActorUserId = "actor-user-id";
    private const string OtherUserId = "other-user-id";

    #region MergeCandidate

    [Fact]
    public void MergeCandidate_MatchesAnOpenEntryWithALiveWindow()
    {
        Matches(MergeCandidate(), CreateEntry()).Should().BeTrue();
    }

    [Fact]
    public void MergeCandidate_RejectsAClosedEntry()
    {
        Matches(MergeCandidate(), CreateEntry() with { IsOpen = false }).Should().BeFalse();
    }

    [Fact]
    public void MergeCandidate_RejectsAnEntryWhoseWindowHasExpired()
    {
        var expired = CreateEntry() with { WindowExpiresAt = Now.AddSeconds(-1) };

        Matches(MergeCandidate(), expired).Should().BeFalse();
    }

    [Fact]
    public void MergeCandidate_RejectsAnEntryWhoseWindowExpiresExactlyNow()
    {
        var expiring = CreateEntry() with { WindowExpiresAt = Now };

        Matches(MergeCandidate(), expiring).Should().BeFalse();
    }

    [Fact]
    public void MergeCandidate_RejectsASoftDeletedEntry()
    {
        var deleted = CreateEntry() with { IsDeleted = true };

        Matches(MergeCandidate(), deleted).Should().BeFalse();
    }

    [Fact]
    public void MergeCandidate_RejectsAnotherUsersEntry()
    {
        Matches(MergeCandidate(), CreateEntry() with { UserId = OtherUserId }).Should().BeFalse();
    }

    [Fact]
    public void MergeCandidate_RejectsAnEntryOnAnotherEntity()
    {
        Matches(MergeCandidate(), CreateEntry() with { EntityId = EntityId + 1 }).Should().BeFalse();
        Matches(MergeCandidate(), CreateEntry() with { EntityType = EntityType.Board }).Should().BeFalse();
        Matches(MergeCandidate(), CreateEntry() with { WorkspaceId = WorkspaceId + 1 }).Should().BeFalse();
    }

    #endregion

    #region OtherUsersOpenEntries

    [Fact]
    public void OtherUsersOpenEntries_MatchesAnotherUsersLiveEntry()
    {
        var other = CreateEntry() with { UserId = OtherUserId };

        Matches(OtherUsersOpenEntries(), other).Should().BeTrue();
    }

    [Fact]
    public void OtherUsersOpenEntries_NeverMatchesTheIncomingUsersOwnEntry()
    {
        Matches(OtherUsersOpenEntries(), CreateEntry()).Should().BeFalse();
    }

    [Fact]
    public void OtherUsersOpenEntries_IgnoresEntriesThatAreAlreadyClosedOrExpired()
    {
        var closed = CreateEntry() with { UserId = OtherUserId, IsOpen = false };
        var expired = CreateEntry() with { UserId = OtherUserId, WindowExpiresAt = Now.AddSeconds(-1) };

        Matches(OtherUsersOpenEntries(), closed).Should().BeFalse();
        Matches(OtherUsersOpenEntries(), expired).Should().BeFalse();
    }

    #endregion

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

        index.IsDescending.Should().Equal(false, false, true, true);
    }

    [Fact]
    public void ActivityEntry_StoresChangedFieldsAsATextArray_AndMetaAsJsonb()
    {
        ColumnTypeOf(nameof(ActivityEntry.ChangedFields)).Should().Be("text[]");
        ColumnTypeOf(nameof(ActivityEntry.LastActivityLogId)).Should().Be("integer");
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

    #region Composition

    [Fact]
    public void UpsertQuery_IsSentToPostgresVerbatim_NeverWrappedInASubquery()
    {
        var sql = ActivityEntryRepository
            .UpsertQuery(Context.ActivityEntries, Upsert(), Now, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(30))
            .ToQueryString();

        Generated(sql).Should().StartWith("INSERT INTO activity_entries",
            "an EF operator composed onto the upsert wraps it in a subquery, and the INSERT ... ON CONFLICT stops being the whole statement");

        Statements(sql).Should().ContainSingle();
    }

    [Fact]
    public void UpsertQuery_BindsEachAncestorToTheParameterOfTheSameName()
    {
        var sql = ActivityEntryRepository
            .UpsertQuery(Context.ActivityEntries, Upsert(), Now, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(30))
            .ToQueryString();

        sql.Should().Contain($"-- project_id='{ProjectId}'");
        sql.Should().Contain($"-- board_id='{BoardId}'");
        sql.Should().Contain($"-- board_group_id='{BoardGroupId}'");
        sql.Should().Contain($"-- task_id='{TaskId}'");
    }

    [Fact]
    public void ClaimQuery_IsSentToPostgresVerbatim_NeverWrappedInASubquery()
    {
        var sql = ActivityEntryRepository
            .ClaimQuery(Context.ActivityEntries, 200)
            .ToQueryString();

        Generated(sql).Should().StartWith("UPDATE activity_entries",
            "an EF operator composed onto the claim wraps it in a subquery, and two replicas can then claim the same entry");

        Statements(sql).Should().ContainSingle();
    }

    #endregion

    private static ActivityEntry CreateEntry()
    {
        return new ()
        {
            WorkspaceId = WorkspaceId,
            EntityType = EntityType.Task,
            EntityId = EntityId,
            UserId = ActorUserId,
            ActivityType = ActivityType.ModifyDescription,
            IsOpen = true,
            FirstOccurredAt = Now.AddMinutes(-1),
            LastOccurredAt = Now.AddMinutes(-1),
            WindowExpiresAt = Now.AddMinutes(4),
            RevisionCount = 1,
        };
    }

    private static ActivityEntryUpsert Upsert()
    {
        return new ()
        {
            WorkspaceId = WorkspaceId,
            EntityType = EntityType.Task,
            EntityId = EntityId,
            UserId = ActorUserId,
            ActivityType = ActivityType.ModifyDescription,
            ChangedFields = ["description"],
            MetaJson = """{"fields":{}}""",
            LastActivityLogId = 1,
            FirstOccurredAt = Now,
            LastOccurredAt = Now,
            RevisionCount = 1,
            ProjectId = ProjectId,
            BoardId = BoardId,
            BoardGroupId = BoardGroupId,
            TaskId = TaskId,
        };
    }

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

    private static Func<ActivityEntry, bool> MergeCandidate()
    {
        return ActivityEntryRepository
            .MergeCandidatePredicate(WorkspaceId, EntityType.Task, EntityId, ActorUserId, Now)
            .Compile();
    }

    private static Func<ActivityEntry, bool> OtherUsersOpenEntries()
    {
        return ActivityEntryRepository
            .OtherUsersOpenEntriesPredicate(WorkspaceId, EntityType.Task, EntityId, ActorUserId, Now)
            .Compile();
    }

    private static bool Matches(Func<ActivityEntry, bool> predicate, ActivityEntry entry) => predicate(entry);

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
