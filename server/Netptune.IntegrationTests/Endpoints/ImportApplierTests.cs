using System.Text;

using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Cache;
using Netptune.Core.Constants;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Events.Sprints;
using Netptune.Core.Relationships;
using Netptune.Core.Storage;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Entities.Contexts;
using Netptune.Import;
using Netptune.Services.Activity;
using Netptune.Transfer;
using Netptune.Transfer.Entities;
using Netptune.Transfer.Enums;
using Netptune.Transfer.Mapping;
using Netptune.Transfer.Repositories;
using Netptune.Transfer.Services;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

// The commit stage runs on the job server, so no endpoint reaches it. These drive the applier directly
// against the real database, which is the only place the batching, the task-number reservation and the
// undo bookkeeping can actually be observed.
[Collection(WorkspaceMutationCollection.Name)]
public sealed class ImportApplierTests
{
    private const string BoardIdentifier = "neovim";

    private readonly NetptuneFixture Fixture;

    public ImportApplierTests(NetptuneFixture fixture)
    {
        Fixture = fixture;
    }

    [Fact]
    public async Task Commit_ShouldCreateEveryRow_AndTakeTheTaskNumbersOffTheProject()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();

        var token = TestContext.Current.CancellationToken;
        var unitOfWork = scope.ServiceProvider.GetRequiredService<INetptuneUnitOfWork>();
        var board = await unitOfWork.Boards.GetByIdentifier(BoardIdentifier, 1, cancellationToken: token);
        var before = await ReadNextTaskScopeId(unitOfWork, board!.ProjectId, token);

        var result = await Commit(scope, """
            Name,group
            applier first,Backlog
            applier second,Backlog
            applier third,Backlog
            """, token);

        result.Created.Should().Be(3);
        result.Failed.Should().Be(0);

        var after = await ReadNextTaskScopeId(unitOfWork, board.ProjectId, token);

        after.Should().Be(before + 3, "the import has to reserve its task numbers rather than reuse them");

        var tasks = await unitOfWork.Tasks.GetAllInWorkspace(1, cancellationToken: token);
        var created = tasks.Where(task => task.Name.StartsWith("applier ", StringComparison.Ordinal)).ToList();

        created.Should().HaveCountGreaterThanOrEqualTo(3);
        created.Select(task => task.ProjectScopeId).Should().OnlyHaveUniqueItems();
        created.Should().OnlyContain(task => task.ProjectScopeId >= before);
    }

    [Fact]
    public async Task Commit_ShouldNumberASecondImportPastTheFirst()
    {
        // Task numbers are reserved against the project's current counter at commit time, not against a
        // cursor captured when the session loaded.
        await using var scope = Fixture.Services.CreateAsyncScope();

        var token = TestContext.Current.CancellationToken;

        await Commit(scope, "Name\nrepeat run one\n", token);

        var second = await Commit(scope, "Name\nrepeat run two\n", token);

        second.Created.Should().Be(1);
        second.Failed.Should().Be(0);

        var unitOfWork = scope.ServiceProvider.GetRequiredService<INetptuneUnitOfWork>();
        var tasks = await unitOfWork.Tasks.GetAllInWorkspace(1, cancellationToken: token);
        var runs = tasks.Where(task => task.Name.StartsWith("repeat run ", StringComparison.Ordinal)).ToList();

        runs.Should().HaveCount(2);
        runs.Select(task => task.ProjectScopeId).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task Commit_ShouldRecordAnUndoEntryPerCreatedTask()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();

        var token = TestContext.Current.CancellationToken;
        var sessions = scope.ServiceProvider.GetRequiredService<IImportSessionRepository>();
        var session = await NewSession(scope, token);

        await Commit(scope, "Name\nundo entry one\nundo entry two\n", token, session);

        var entries = await sessions.GetEntries(session.Id, token);

        entries.Should().HaveCount(2);
        entries.Should().OnlyContain(entry => entry.Operation == ImportEntryOperation.Created);
        entries.Should().OnlyContain(entry => entry.EntityId > 0, "the entries are written after the tasks have ids");
    }

    [Fact]
    public async Task Commit_ShouldCreateEachMissingSprintOnce_WithTheDatesTheRowsGive()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();

        var token = TestContext.Current.CancellationToken;
        var sprintName = $"Import sprint {Guid.NewGuid():N}"[..24];
        var mapping = Bind(TaskFieldKeys.Name, TaskFieldKeys.Sprint, TaskFieldKeys.SprintStartDate, TaskFieldKeys.SprintEndDate);

        var result = await Commit(scope, $"""
            Name,Sprint,Sprint start,Sprint end
            sprint task one,{sprintName},2026-01-05,2026-01-19
            sprint task two,{sprintName},2026-01-05,2026-01-19
            """, mapping, token);

        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var sprint = await context.Sprints.AsNoTracking().SingleAsync(candidate => candidate.Name == sprintName, token);
        var board = await context.Boards.AsNoTracking().SingleAsync(candidate => candidate.Identifier == BoardIdentifier, token);
        var sprintTasks = await context.ProjectTasks.AsNoTracking().CountAsync(task => task.SprintId == sprint.Id, token);

        result.SprintsCreated.Should().Be(1);
        sprint.ProjectId.Should().Be(board.ProjectId);
        sprint.StartDate.Should().Be(new DateTime(2026, 1, 5));
        sprint.EndDate.Should().Be(new DateTime(2026, 1, 19));
        sprint.Status.Should().Be(SprintStatus.Completed, "the sprint ended before the import ran");
        sprintTasks.Should().Be(2);
    }

    [Fact]
    public async Task Commit_ShouldLinkParentsAndBlockers_ThatComeLaterInTheFile()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();

        var token = TestContext.Current.CancellationToken;
        var prefix = $"rel-{Guid.NewGuid():N}"[..12];
        var mapping = Bind(TaskFieldKeys.SystemId, TaskFieldKeys.Name, TaskFieldKeys.Parent, TaskFieldKeys.BlockedBy);

        var result = await Commit(scope, $"""
            Id,Name,Parent,Blocked by
            {prefix}-child,link child,{prefix}-parent,{prefix}-blocker|{prefix}-missing
            {prefix}-parent,link parent,,
            {prefix}-blocker,link blocker,,
            """, mapping, token);

        var relations = await RelationsAmong(scope, prefix, token);

        result.RelationsCreated.Should().Be(2, "the missing blocker is skipped");
        relations.Should().BeEquivalentTo([
            ("parent-of", $"{prefix}-parent", $"{prefix}-child"),
            ("blocks", $"{prefix}-blocker", $"{prefix}-child"),
        ]);
    }

    [Fact]
    public async Task Commit_ShouldImportCommentsOnce_AndCreditMembersByEmail()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();

        var token = TestContext.Current.CancellationToken;
        var prefix = $"com-{Guid.NewGuid():N}"[..12];
        var mapping = Bind(TaskFieldKeys.SystemId, TaskFieldKeys.Name, TaskFieldKeys.Comments) with
        {
            Dedupe = new ImportDedupeModel { KeyFieldKey = TaskFieldKeys.SystemId, Action = ImportDedupeAction.UpdateExisting },
        };
        var csv = $"Id,Name,Comments\n{prefix}-a,comment target,2\n";
        var first = await NewSession(scope, token);
        var owner = await OwnerOf(scope, first, token);
        var comments = $$"""
            {"task":"{{prefix}}-a","id":"c1","authorEmail":"{{owner.Email}}","authorName":"Owner","createdAt":"2025-02-01T10:00:00Z","body":"From a member"}
            {"task":"{{prefix}}-a","id":"c2","authorEmail":"stranger@example.com","authorName":"Jane Stranger","body":"From someone else"}
            {"task":"{{prefix}}-elsewhere","id":"c3","authorName":"Nobody","body":"Not in this import"}
            """;

        await WriteComments(scope, first, comments, token);

        var imported = await Commit(scope, csv, mapping, token, first);
        var second = await NewSession(scope, token);

        await WriteComments(scope, second, comments, token);

        var reimported = await Commit(scope, csv, mapping, token, second);

        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var task = await context.ProjectTasks.AsNoTracking().SingleAsync(candidate => candidate.ExternalId == $"{prefix}-a", token);
        var stored = await context.Comments
            .AsNoTracking()
            .Where(comment => comment.EntityType == EntityType.Task && comment.EntityId == task.Id)
            .OrderBy(comment => comment.ExternalId)
            .ToListAsync(token);

        imported.CommentsCreated.Should().Be(2);
        reimported.Updated.Should().Be(1);
        reimported.CommentsCreated.Should().Be(0);
        stored.Select(comment => comment.ExternalId).Should().Equal("c1", "c2");
        stored[0].OwnerId.Should().Be(owner.Id);
        stored[0].Body.Should().Be("From a member");
        stored[0].CreatedAt.Should().Be(new DateTime(2025, 2, 1, 10, 0, 0));
        stored[1].OwnerId.Should().Be(first.CreatedBy);
        stored[1].Body.Should().StartWith("Originally by Jane Stranger").And.EndWith("From someone else");
    }

    [Fact]
    public async Task Undo_ShouldRemoveTheSprintsLinksAndCommentsTheImportCreated()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();

        var token = TestContext.Current.CancellationToken;
        var prefix = $"und-{Guid.NewGuid():N}"[..12];
        var sprintName = $"Undo sprint {prefix}";
        var mapping = Bind(TaskFieldKeys.SystemId, TaskFieldKeys.Name, TaskFieldKeys.Sprint, TaskFieldKeys.Parent, TaskFieldKeys.Comments);
        var session = await NewSession(scope, token);

        await WriteComments(scope, session, $$"""{"task":"{{prefix}}-child","id":"u1","authorName":"Someone","body":"Undo me"}""", token);

        var result = await Commit(scope, $"""
            Id,Name,Sprint,Parent,Comments
            {prefix}-child,undo child,{sprintName},{prefix}-parent,1
            {prefix}-parent,undo parent,{sprintName},,0
            """, mapping, token, session);

        // Undo is its own request, so it runs on a context that isn't still tracking what the import wrote.
        await using var undoScope = Fixture.Services.CreateAsyncScope();

        var undo = undoScope.ServiceProvider.GetRequiredService<IImportUndoService>();
        var undoSession = await undoScope.ServiceProvider.GetRequiredService<IImportSessionRepository>()
            .GetAsync(session.Id, cancellationToken: token);
        var undone = await undo.Undo(undoSession!, session.CreatedBy, token);

        var context = undoScope.ServiceProvider.GetRequiredService<DataContext>();
        var taskIds = await context.ProjectTasks
            .AsNoTracking()
            .Where(task => task.ExternalId!.StartsWith(prefix))
            .Select(task => new { task.Id, task.IsDeleted })
            .ToListAsync(token);
        var ids = taskIds.Select(task => task.Id).ToList();
        var sprint = await context.Sprints.AsNoTracking().SingleAsync(candidate => candidate.Name == sprintName, token);

        result.SprintsCreated.Should().Be(1);
        result.RelationsCreated.Should().Be(1);
        result.CommentsCreated.Should().Be(1);
        undone.Blocked.Should().BeEmpty();
        taskIds.Should().HaveCount(2).And.OnlyContain(task => task.IsDeleted);
        sprint.IsDeleted.Should().BeTrue();
        (await context.ProjectTaskRelations.AnyAsync(relation => ids.Contains(relation.TargetTaskId), token)).Should().BeFalse();
        (await context.Comments.AnyAsync(comment => comment.EntityType == EntityType.Task && ids.Contains(comment.EntityId), token)).Should().BeFalse();
    }

    [Fact]
    public async Task Commit_ShouldNotReuseASprintFromAnotherProject()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();

        var token = TestContext.Current.CancellationToken;
        var sprintName = $"Elsewhere {Guid.NewGuid():N}"[..24];
        var session = await NewSession(scope, token);
        var destinationProjectId = await DestinationProjectId(scope, token);
        var otherProjectId = await OtherProjectId(scope, destinationProjectId, token);
        var elsewhere = await AddSprint(
            scope,
            otherProjectId,
            sprintName,
            SprintStatus.Planning,
            session.CreatedBy,
            token);

        var result = await Commit(scope, $"""
            Name,Sprint
            scoped sprint task {sprintName},{sprintName}
            """, Bind(TaskFieldKeys.Name, TaskFieldKeys.Sprint), token, session);

        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var created = await context.Sprints.AsNoTracking().SingleAsync(
            sprint => sprint.Name == sprintName && sprint.ProjectId == destinationProjectId,
            token);
        var task = await context.ProjectTasks.AsNoTracking().SingleAsync(
            candidate => candidate.Name == $"scoped sprint task {sprintName}",
            token);

        result.SprintsCreated.Should().Be(1);
        created.Id.Should().NotBe(elsewhere.Id);
        task.SprintId.Should().Be(created.Id);
    }

    [Fact]
    public async Task Commit_ShouldNeitherAssignToNorDuplicate_AClosedSprintInTheProject()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();

        var token = TestContext.Current.CancellationToken;
        var sprintName = $"Closed {Guid.NewGuid():N}"[..24];
        var session = await NewSession(scope, token);
        var projectId = await DestinationProjectId(scope, token);
        var closed = await AddSprint(scope, projectId, sprintName, SprintStatus.Completed, session.CreatedBy, token);

        var result = await Commit(scope, $"""
            Name,Sprint
            closed sprint task {sprintName},{sprintName}
            """, Bind(TaskFieldKeys.Name, TaskFieldKeys.Sprint), token, session);

        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var sameNamed = await context.Sprints.AsNoTracking().CountAsync(sprint => sprint.Name == sprintName, token);
        var task = await context.ProjectTasks.AsNoTracking().SingleAsync(
            candidate => candidate.Name == $"closed sprint task {sprintName}",
            token);
        var closedMembers = await context.ProjectTasks.AsNoTracking().CountAsync(
            candidate => candidate.SprintId == closed.Id,
            token);

        result.Created.Should().Be(1);
        result.SprintsCreated.Should().Be(0);
        sameNamed.Should().Be(1);
        task.SprintId.Should().BeNull();
        closedMembers.Should().Be(0);
    }

    [Fact]
    public async Task Preview_ShouldWarnWhenARowNamesAClosedSprint()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();

        var token = TestContext.Current.CancellationToken;
        var sprintName = $"Closed {Guid.NewGuid():N}"[..24];
        var session = await NewSession(scope, token);
        var projectId = await DestinationProjectId(scope, token);

        await AddSprint(scope, projectId, sprintName, SprintStatus.Cancelled, session.CreatedBy, token);

        var preview = await Preview(scope, $"""
            Name,Sprint
            closed preview task,{sprintName}
            """, Bind(TaskFieldKeys.Name, TaskFieldKeys.Sprint), session, token);

        preview.Diagnostics.Should().ContainSingle(diagnostic => diagnostic.Code == ImportDiagnosticCodes.UnresolvedSprint)
            .Which.Severity.Should().Be(ImportDiagnosticSeverity.Warning);
        preview.NewEntities.Should().NotContain(entity => entity.EntityType == "sprint");
    }

    [Fact]
    public async Task Preview_ShouldListASprintTheImportWillCreate_AndWarnAboutMissingLinks()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();

        var token = TestContext.Current.CancellationToken;
        var prefix = $"pre-{Guid.NewGuid():N}"[..12];
        var sprintName = $"Preview sprint {prefix}";
        var session = await NewSession(scope, token);
        var mapping = Bind(TaskFieldKeys.SystemId, TaskFieldKeys.Name, TaskFieldKeys.Sprint, TaskFieldKeys.Parent);

        var preview = await Preview(scope, $"""
            Id,Name,Sprint,Parent
            {prefix}-a,preview a,{sprintName},{prefix}-b
            {prefix}-b,preview b,{sprintName},{prefix}-missing
            """, mapping, session, token);

        preview.NewEntities.Should().ContainSingle(entity => entity.EntityType == "sprint")
            .Which.Name.Should().Be(sprintName);
        preview.Diagnostics.Should().ContainSingle(diagnostic => diagnostic.Code == ImportDiagnosticCodes.UnresolvedLink)
            .Which.Value.Should().Be($"{prefix}-missing");
    }

    [Fact]
    public async Task Preview_ShouldNotWarnAboutLinks_WhenItStoppedShortOfTheWholeFile()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();

        var token = TestContext.Current.CancellationToken;
        var prefix = $"cap-{Guid.NewGuid():N}"[..12];
        var session = await NewSession(scope, token);
        var mapping = Bind(TaskFieldKeys.SystemId, TaskFieldKeys.Name, TaskFieldKeys.Parent);

        var preview = await Preview(scope, $"""
            Id,Name,Parent
            {prefix}-a,capped a,{prefix}-b
            {prefix}-b,capped b,
            """, mapping, session, token, previewRowCap: 1);

        preview.IsExtrapolated.Should().BeTrue();
        preview.Diagnostics.Should().NotContain(diagnostic => diagnostic.Code == ImportDiagnosticCodes.UnresolvedLink);
    }

    [Fact]
    public async Task Commit_ShouldKeepARunningSprintInPlanning_WhenItEndsToday()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();

        var token = TestContext.Current.CancellationToken;
        var sprintName = $"Ends today {Guid.NewGuid():N}"[..24];
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var start = today.AddDays(-13).ToString("yyyy-MM-dd");
        var end = today.ToString("yyyy-MM-dd");
        var mapping = Bind(TaskFieldKeys.Name, TaskFieldKeys.Sprint, TaskFieldKeys.SprintStartDate, TaskFieldKeys.SprintEndDate);

        await Commit(scope, $"""
            Name,Sprint,Sprint start,Sprint end
            ends today task,{sprintName},{start},{end}
            """, mapping, token);

        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var sprint = await context.Sprints.AsNoTracking().SingleAsync(candidate => candidate.Name == sprintName, token);
        var sprintTasks = await context.ProjectTasks.AsNoTracking().CountAsync(task => task.SprintId == sprint.Id, token);

        sprint.Status.Should().Be(SprintStatus.Planning, "the sprint's last day has not passed yet");
        sprint.StartedAt.Should().BeNull();
        sprint.CompletedAt.Should().BeNull();
        sprintTasks.Should().Be(1);
    }

    [Fact]
    public async Task Commit_ShouldStillImportTheRow_WhenASprintDateIsNotADate()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();

        var token = TestContext.Current.CancellationToken;
        var sprintName = $"Bad date {Guid.NewGuid():N}"[..24];
        var mapping = Bind(TaskFieldKeys.Name, TaskFieldKeys.Sprint, TaskFieldKeys.SprintStartDate);
        var csv = $"""
            Name,Sprint,Sprint start
            bad sprint date task,{sprintName},next tuesday
            """;
        var previewSession = await NewSession(scope, token);

        var preview = await Preview(scope, csv, mapping, previewSession, token);
        var result = await Commit(scope, csv, mapping, token);

        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var sprint = await context.Sprints.AsNoTracking().SingleAsync(candidate => candidate.Name == sprintName, token);
        var sprintTasks = await context.ProjectTasks.AsNoTracking().CountAsync(task => task.SprintId == sprint.Id, token);

        preview.WillError.Should().Be(0);
        preview.Diagnostics.Should().ContainSingle(diagnostic => diagnostic.Code == ImportDiagnosticCodes.InvalidDate)
            .Which.Severity.Should().Be(ImportDiagnosticSeverity.Warning);
        result.Created.Should().Be(1);
        result.Failed.Should().Be(0);
        sprintTasks.Should().Be(1);
    }

    [Fact]
    public async Task Commit_ShouldMoveAnUpdatedTaskIntoAnActiveSprint_AndUndoShouldMoveItBack()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();

        var token = TestContext.Current.CancellationToken;
        var prefix = $"mov-{Guid.NewGuid():N}"[..12];
        var firstSession = await NewSession(scope, token);
        var projectId = await DestinationProjectId(scope, token);
        var userId = firstSession.CreatedBy;
        var planning = await AddSprint(scope, projectId, $"Planning {prefix}", SprintStatus.Planning, userId, token);
        var active = await AddSprint(scope, projectId, $"Active {prefix}", SprintStatus.Active, userId, token);
        var mapping = Bind(TaskFieldKeys.SystemId, TaskFieldKeys.Name, TaskFieldKeys.Sprint) with
        {
            Dedupe = new ImportDedupeModel { KeyFieldKey = TaskFieldKeys.SystemId, Action = ImportDedupeAction.UpdateExisting },
        };

        try
        {
            var firstCsv = $"Id,Name,Sprint\n{prefix}-t,moving task,{planning.Name}\n";

            await Commit(scope, firstCsv, mapping, token, firstSession);

            await using var secondScope = Fixture.Services.CreateAsyncScope();

            var secondSession = await NewSession(secondScope, token);
            var secondCsv = $"Id,Name,Sprint\n{prefix}-t,moving task renamed,{active.Name}\n";
            var moved = await Commit(secondScope, secondCsv, mapping, token, secondSession);

            var context = scope.ServiceProvider.GetRequiredService<DataContext>();
            var task = await context.ProjectTasks
                .AsNoTracking()
                .SingleAsync(candidate => candidate.ExternalId == $"{prefix}-t", token);
            var addedToActive = await MemberChanges(scope, active.Id, task.Id, token);

            moved.Updated.Should().Be(1);
            task.SprintId.Should().Be(active.Id);
            addedToActive.Should().Equal(SprintMemberChanges.Added);

            var undone = await Undo(secondSession.Id, token);

            await using var checkScope = Fixture.Services.CreateAsyncScope();

            var checkContext = checkScope.ServiceProvider.GetRequiredService<DataContext>();
            var restored = await checkContext.ProjectTasks
                .AsNoTracking()
                .SingleAsync(candidate => candidate.Id == task.Id, token);
            var activeChanges = await MemberChanges(checkScope, active.Id, task.Id, token);

            undone.Blocked.Should().BeEmpty();
            restored.SprintId.Should().Be(planning.Id);
            activeChanges.Should().Equal(SprintMemberChanges.Added, SprintMemberChanges.Removed);
        }
        finally
        {
            await CloseSprint(active.Id, token);
        }
    }

    [Fact]
    public async Task Commit_ShouldRecordJoiningAnActiveSprint_ForACreatedTask()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();

        var token = TestContext.Current.CancellationToken;
        var prefix = $"act-{Guid.NewGuid():N}"[..12];
        var session = await NewSession(scope, token);
        var projectId = await DestinationProjectId(scope, token);
        var userId = session.CreatedBy;
        var active = await AddSprint(scope, projectId, $"Active {prefix}", SprintStatus.Active, userId, token);

        try
        {
            await Commit(
                scope,
                $"Id,Name,Sprint\n{prefix}-t,joining task,{active.Name}\n",
                Bind(TaskFieldKeys.SystemId, TaskFieldKeys.Name, TaskFieldKeys.Sprint),
                token,
                session);

            var context = scope.ServiceProvider.GetRequiredService<DataContext>();
            var task = await context.ProjectTasks
                .AsNoTracking()
                .SingleAsync(candidate => candidate.ExternalId == $"{prefix}-t", token);
            var changes = await MemberChanges(scope, active.Id, task.Id, token);

            task.SprintId.Should().Be(active.Id);
            changes.Should().Equal(SprintMemberChanges.Added);
        }
        finally
        {
            await CloseSprint(active.Id, token);
        }
    }

    [Fact]
    public async Task Commit_ShouldKeepTheFirstParent_WhenRowsGiveAChildTwo()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();

        var token = TestContext.Current.CancellationToken;
        var prefix = $"two-{Guid.NewGuid():N}"[..12];
        var mapping = LinkMapping() with
        {
            Dedupe = new ImportDedupeModel { KeyFieldKey = TaskFieldKeys.SystemId, Action = ImportDedupeAction.UpdateExisting },
        };

        await Commit(scope, $"{LinkHeader}\n{prefix}-c,two parents child,,,,\n", mapping, token);

        var result = await Commit(scope, $"""
            {LinkHeader}
            {prefix}-p1,first parent,,,,
            {prefix}-p2,second parent,,,,
            {prefix}-c,two parents child,{prefix}-p1,,,
            {prefix}-c,two parents child,{prefix}-p2,,,
            """, mapping, token);

        var relations = await RelationsAmong(scope, prefix, token);

        result.RelationsCreated.Should().Be(1);
        relations.Should().BeEquivalentTo([("parent-of", $"{prefix}-p1", $"{prefix}-c")]);
    }

    [Fact]
    public async Task Commit_ShouldRefuseTheBlockerThatClosesACycle_WithinOneImport()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();

        var token = TestContext.Current.CancellationToken;
        var prefix = $"cyc-{Guid.NewGuid():N}"[..12];

        var result = await Commit(scope, $"""
            {LinkHeader}
            {prefix}-a,cycle a,,,{prefix}-b,
            {prefix}-b,cycle b,,,{prefix}-a,
            """, LinkMapping(), token);

        var relations = await RelationsAmong(scope, prefix, token);

        result.RelationsCreated.Should().Be(1);
        relations.Should().BeEquivalentTo([("blocks", $"{prefix}-a", $"{prefix}-b")]);
    }

    [Fact]
    public async Task Commit_ShouldCreateALinkOnce_WhenBothEndsStateIt()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();

        var token = TestContext.Current.CancellationToken;
        var prefix = $"dup-{Guid.NewGuid():N}"[..12];

        var result = await Commit(scope, $"""
            {LinkHeader}
            {prefix}-a,both ends a,,,{prefix}-b,{prefix}-b
            {prefix}-b,both ends b,,{prefix}-a,,{prefix}-a
            """, LinkMapping(), token);

        var relations = await RelationsAmong(scope, prefix, token);
        var blocks = relations.Where(relation => relation.Type == "blocks").ToList();
        var related = relations.Where(relation => relation.Type == "relates-to").ToList();

        result.RelationsCreated.Should().Be(2);
        blocks.Should().BeEquivalentTo([("blocks", $"{prefix}-a", $"{prefix}-b")]);
        related.Should().ContainSingle();
    }

    [Fact]
    public async Task Commit_ShouldRefuseABlockerThatClosesACycleThroughAnExistingLink()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();

        var token = TestContext.Current.CancellationToken;
        var prefix = $"old-{Guid.NewGuid():N}"[..12];

        await Commit(scope, $"""
            {LinkHeader}
            {prefix}-b,existing b,,,{prefix}-c,
            {prefix}-c,existing c,,,,
            """, LinkMapping(), token);

        // C blocks A is read first (blocked-by comes before blocks), so A blocks B is the edge that
        // would close B -> C -> A -> B.
        var result = await Commit(scope, $"""
            {LinkHeader}
            {prefix}-a,new a,,{prefix}-c,{prefix}-b,
            """, LinkMapping(), token);

        var relations = await RelationsAmong(scope, prefix, token);

        result.RelationsCreated.Should().Be(1);
        relations.Should().BeEquivalentTo([
            ("blocks", $"{prefix}-b", $"{prefix}-c"),
            ("blocks", $"{prefix}-c", $"{prefix}-a"),
        ]);
    }

    [Fact]
    public async Task Commit_ShouldNotGiveAChildASecondParent_OrRepeatAnExistingLink()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();

        var token = TestContext.Current.CancellationToken;
        var prefix = $"rep-{Guid.NewGuid():N}"[..12];
        var mapping = LinkMapping() with
        {
            Dedupe = new ImportDedupeModel { KeyFieldKey = TaskFieldKeys.SystemId, Action = ImportDedupeAction.UpdateExisting },
        };

        await Commit(scope, $"""
            {LinkHeader}
            {prefix}-p1,old parent,,,,
            {prefix}-c,repeat child,{prefix}-p1,,{prefix}-d,
            {prefix}-d,repeat blocked,,,,
            """, mapping, token);

        var result = await Commit(scope, $"""
            {LinkHeader}
            {prefix}-p2,new parent,,,,
            {prefix}-c,repeat child,{prefix}-p2,,{prefix}-d,
            """, mapping, token);

        var relations = await RelationsAmong(scope, prefix, token);

        result.Updated.Should().Be(1);
        result.RelationsCreated.Should().Be(0);
        relations.Should().BeEquivalentTo([
            ("parent-of", $"{prefix}-p1", $"{prefix}-c"),
            ("blocks", $"{prefix}-c", $"{prefix}-d"),
        ]);
    }

    [Fact]
    public async Task Commit_ShouldRecordOneEntryPerLink_AcrossChunks_AndUndoShouldRemoveThemAll()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();

        var token = TestContext.Current.CancellationToken;
        var prefix = $"big-{Guid.NewGuid():N}"[..12];
        var session = await NewSession(scope, token);
        var rows = Enumerable.Range(1, 600).Select(index => $"{prefix}-{index},spoke {index},,,,{prefix}-hub");
        var csv = $"{LinkHeader}\n{prefix}-hub,hub,,,,\n{string.Join('\n', rows)}\n";

        var result = await Commit(scope, csv, LinkMapping(), token, session);

        var sessions = scope.ServiceProvider.GetRequiredService<IImportSessionRepository>();
        var entries = await sessions.GetEntries(session.Id, token);
        var relationEntries = entries.Where(entry => entry.EntityType == ImportEntryTypes.TaskRelation).ToList();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var relationIds = await context.ProjectTaskRelations
            .AsNoTracking()
            .Where(relation => relation.TargetTask!.ExternalId!.StartsWith(prefix))
            .Select(relation => relation.Id)
            .ToListAsync(token);

        result.RelationsCreated.Should().Be(600);
        relationIds.Should().HaveCount(600);
        relationEntries.Select(entry => entry.EntityId).Should().BeEquivalentTo(relationIds);

        var undone = await Undo(session.Id, token);

        await using var checkScope = Fixture.Services.CreateAsyncScope();

        var remaining = await RelationsAmong(checkScope, prefix, token);

        undone.Blocked.Should().BeEmpty();
        remaining.Should().BeEmpty();
    }

    [Fact]
    public async Task Commit_ShouldImportCommentsWithNumericIds_WithoutACommentsColumn()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();

        var token = TestContext.Current.CancellationToken;
        var taskNumber = Random.Shared.Next(100_000_000, 999_999_999);
        var taskName = $"numeric comments {Guid.NewGuid():N}";
        var session = await NewSession(scope, token);

        await WriteComments(scope, session, $$"""
            {"task":{{taskNumber}},"id":456,"authorName":"Numbers","body":"Numeric ids"}
            {"task":"{{taskNumber}}","id":true,"authorName":"Broken","body":"Not an id"}
            """, token);

        var result = await Commit(
            scope,
            $"Id,Name\n{taskNumber},{taskName}\n",
            Bind(TaskFieldKeys.SystemId, TaskFieldKeys.Name),
            token,
            session);

        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var task = await context.ProjectTasks.AsNoTracking().SingleAsync(candidate => candidate.Name == taskName, token);
        var stored = await context.Comments
            .AsNoTracking()
            .Where(comment => comment.EntityType == EntityType.Task && comment.EntityId == task.Id)
            .ToListAsync(token);
        var sessions = scope.ServiceProvider.GetRequiredService<IImportSessionRepository>();
        var entries = await sessions.GetEntries(session.Id, token);
        var commentType = EventEntityTypes.From(EntityType.Comment);
        var commentEntry = entries.Single(entry => entry.EntityType == commentType);

        result.CommentsCreated.Should().Be(1);
        stored.Should().ContainSingle().Which.ExternalId.Should().Be("456");
        commentEntry.EntityId.Should().Be(stored[0].Id);
        commentEntry.EntityUpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Undo_ShouldKeepACommentEditedSinceTheImport_AndRemoveTheRest()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();

        var token = TestContext.Current.CancellationToken;
        var prefix = $"edt-{Guid.NewGuid():N}"[..12];
        var session = await NewSession(scope, token);

        await WriteComments(scope, session, $$"""
            {"task":"{{prefix}}-t","id":"keep","authorName":"Someone","body":"Edited later"}
            {"task":"{{prefix}}-t","id":"drop","authorName":"Someone","body":"Untouched"}
            """, token);

        var mapping = Bind(TaskFieldKeys.SystemId, TaskFieldKeys.Name);

        await Commit(scope, $"Id,Name\n{prefix}-t,edited comments\n", mapping, token, session);

        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var task = await context.ProjectTasks
            .AsNoTracking()
            .SingleAsync(candidate => candidate.ExternalId == $"{prefix}-t", token);
        var editedAt = DateTime.UtcNow.AddHours(1);

        await context.Comments
            .Where(comment => comment.EntityType == EntityType.Task && comment.EntityId == task.Id)
            .Where(comment => comment.ExternalId == "keep")
            .ExecuteUpdateAsync(setters => setters.SetProperty(comment => comment.UpdatedAt, editedAt), token);

        var undone = await Undo(session.Id, token);

        await using var checkScope = Fixture.Services.CreateAsyncScope();

        var checkContext = checkScope.ServiceProvider.GetRequiredService<DataContext>();
        var remaining = await checkContext.Comments
            .AsNoTracking()
            .Where(comment => comment.EntityType == EntityType.Task && comment.EntityId == task.Id)
            .Select(comment => comment.ExternalId)
            .ToListAsync(token);

        undone.Blocked.Should().ContainSingle();
        remaining.Should().Equal("keep");
    }

    private const string LinkHeader = "Id,Name,Parent,Blocked by,Blocks,Related to";

    private static ImportMappingModel LinkMapping()
    {
        return Bind(
            TaskFieldKeys.SystemId,
            TaskFieldKeys.Name,
            TaskFieldKeys.Parent,
            TaskFieldKeys.BlockedBy,
            TaskFieldKeys.Blocks,
            TaskFieldKeys.RelatedTo);
    }

    private async Task<ImportUndoResult> Undo(int sessionId, CancellationToken cancellationToken)
    {
        // Undo is its own request, so it runs on a context that isn't still tracking what the import wrote.
        await using var undoScope = Fixture.Services.CreateAsyncScope();

        var undo = undoScope.ServiceProvider.GetRequiredService<IImportUndoService>();
        var sessions = undoScope.ServiceProvider.GetRequiredService<IImportSessionRepository>();
        var session = await sessions.GetAsync(sessionId, cancellationToken: cancellationToken);
        var result = await undo.Undo(session!, session!.CreatedBy, cancellationToken);

        return result;
    }

    private static async Task<int> DestinationProjectId(IServiceScope scope, CancellationToken cancellationToken)
    {
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var board = await context.Boards
            .AsNoTracking()
            .SingleAsync(candidate => candidate.Identifier == BoardIdentifier, cancellationToken);

        return board.ProjectId;
    }

    private static async Task<int> OtherProjectId(
        IServiceScope scope,
        int destinationProjectId,
        CancellationToken cancellationToken)
    {
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var project = await context.Projects
            .AsNoTracking()
            .Where(candidate => candidate.WorkspaceId == 1 && candidate.Id != destinationProjectId && !candidate.IsDeleted)
            .OrderBy(candidate => candidate.Id)
            .FirstAsync(cancellationToken);

        return project.Id;
    }

    private static async Task<Sprint> AddSprint(
        IServiceScope scope,
        int projectId,
        string name,
        SprintStatus status,
        string userId,
        CancellationToken cancellationToken)
    {
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var today = DateTime.UtcNow.Date;
        var isClosed = status is SprintStatus.Completed or SprintStatus.Cancelled;
        var sprint = new Sprint
        {
            Name = name,
            Identifier = $"import-test-{Guid.NewGuid():N}",
            Status = status,
            StartDate = today.AddDays(-7),
            EndDate = today.AddDays(7),
            StartedAt = status == SprintStatus.Planning ? null : today.AddDays(-7),
            CompletedAt = isClosed ? today : null,
            ProjectId = projectId,
            WorkspaceId = 1,
            OwnerId = userId,
            CreatedByUserId = userId,
        };

        context.Sprints.Add(sprint);

        await context.SaveChangesAsync(cancellationToken);

        context.Entry(sprint).State = EntityState.Detached;

        return sprint;
    }

    // An active sprint left behind would be picked up as the workspace's current sprint by other tests.
    private async Task CloseSprint(int sprintId, CancellationToken cancellationToken)
    {
        await using var scope = Fixture.Services.CreateAsyncScope();

        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        await context.Sprints
            .Where(sprint => sprint.Id == sprintId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(sprint => sprint.Status, SprintStatus.Completed),
                cancellationToken);
    }

    private static async Task<List<string?>> MemberChanges(
        IServiceScope scope,
        int sprintId,
        int taskId,
        CancellationToken cancellationToken)
    {
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var sprintSubjectType = EventEntityTypes.From(EntityType.Sprint);
        var sprintSubjectId = sprintId.ToString();
        var memberId = taskId.ToString();
        var changes = await context.EventRecords
            .AsNoTracking()
            .Where(record => record.SubjectType == sprintSubjectType
                && record.SubjectId == sprintSubjectId
                && record.EventKey == EventKeys.ScopeMemberChanged
                && record.Payload.RootElement.GetProperty("memberId").GetString() == memberId)
            .OrderBy(record => record.Id)
            .Select(record => record.Payload.RootElement.GetProperty("change").GetString())
            .ToListAsync(cancellationToken);

        return changes;
    }

    private static async Task<ImportPreviewResult> Preview(
        IServiceScope scope,
        string csv,
        ImportMappingModel mapping,
        ImportSession session,
        CancellationToken cancellationToken,
        int previewRowCap = 5_000)
    {
        var actor = new ActorContext();
        var applier = BuildApplier(scope, actor);
        var trimmed = csv.Trim();

        using var actorScope = actor.Begin(new ActorIdentity(session.CreatedBy, 1, "netptune"));

        var request = new ImportApplyRequest
        {
            WorkspaceId = 1,
            WorkspaceSlug = "netptune",
            UserId = session.CreatedBy,
            Session = session,
            Mapping = mapping,
            Source = new MemoryStream(Encoding.UTF8.GetBytes(trimmed)),
            ColumnNames = trimmed.Split('\n')[0].Trim().Split(','),
            ReadOptions = new ImportReadOptions { Delimiter = ',', HasHeaderRow = true },
            PreviewRowCap = previewRowCap,
        };
        var preview = await applier.Preview(request, cancellationToken);

        return preview;
    }

    private static async Task<List<(string Type, string Source, string Target)>> RelationsAmong(
        IServiceScope scope,
        string prefix,
        CancellationToken cancellationToken)
    {
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var relations = await context.ProjectTaskRelations
            .AsNoTracking()
            .Where(relation => relation.TargetTask!.ExternalId!.StartsWith(prefix))
            .Select(relation => new
            {
                relation.RelationType!.Key,
                Source = relation.SourceTask!.ExternalId!,
                Target = relation.TargetTask!.ExternalId!,
            })
            .ToListAsync(cancellationToken);

        return relations.Select(relation => (relation.Key, relation.Source, relation.Target)).ToList();
    }

    private static async Task<AppUser> OwnerOf(IServiceScope scope, ImportSession session, CancellationToken cancellationToken)
    {
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var owner = await context.AppUsers.AsNoTracking().SingleAsync(user => user.Id == session.CreatedBy, cancellationToken);

        return owner;
    }

    private static async Task WriteComments(
        IServiceScope scope,
        ImportSession session,
        string ndjson,
        CancellationToken cancellationToken)
    {
        var storage = scope.ServiceProvider.GetRequiredService<IStorageService>();
        var lines = string.Join('\n', ndjson.Split('\n').Select(line => line.Trim()));

        await storage.UploadFileAsync(new MemoryStream(Encoding.UTF8.GetBytes(lines)), new StorageUploadOptions
        {
            Name = ImportSourceStore.CommentsFileName,
            Key = ImportSourceStore.CommentsKey(session.StorageKey),
            ContentType = "application/x-ndjson",
            Access = StorageAccess.Private,
        }, cancellationToken);
    }

    // Built by hand so it has the shape the job server gives it: a background identity taking its actor
    // from the queued message rather than from an HTTP request.
    private static IImportApplier BuildApplier(IServiceScope scope, IActorContext actor)
    {
        var identity = new BackgroundIdentityService(actor, scope.ServiceProvider.GetRequiredService<IUserCache>());
        var activity = new ActivityLogger(
            scope.ServiceProvider.GetRequiredService<IEventPublisher>(),
            identity,
            scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>());

        return new ImportApplier(
            scope.ServiceProvider.GetRequiredService<INetptuneUnitOfWork>(),
            scope.ServiceProvider.GetServices<IImportSourceReader>(),
            scope.ServiceProvider.GetRequiredService<IEventRecordWriter>(),
            scope.ServiceProvider.GetRequiredService<IEventPublisher>(),
            scope.ServiceProvider.GetRequiredService<IImportSessionRepository>(),
            activity,
            scope.ServiceProvider.GetRequiredService<IImportSourceStore>());
    }

    private static Task<ImportCommitResult> Commit(
        IServiceScope scope,
        string csv,
        CancellationToken cancellationToken,
        ImportSession? session = null)
    {
        return Commit(scope, csv, Bind(TaskFieldKeys.Name), cancellationToken, session);
    }

    private static ImportMappingModel Bind(params string[] fieldKeys)
    {
        return new ImportMappingModel
        {
            RecordType = EntityRefTypes.Task,
            Bindings = fieldKeys
                .Select((fieldKey, index) => new ImportFieldBinding { FieldKey = fieldKey, ColumnIndex = index })
                .ToList(),
        };
    }

    private static async Task<ImportCommitResult> Commit(
        IServiceScope scope,
        string csv,
        ImportMappingModel mapping,
        CancellationToken cancellationToken,
        ImportSession? session = null)
    {
        var actor = new ActorContext();
        var applier = BuildApplier(scope, actor);
        var trimmed = csv.Trim();

        session ??= await NewSession(scope, cancellationToken);

        using var actorScope = actor.Begin(new ActorIdentity(session.CreatedBy, 1, "netptune"));

        var request = new ImportApplyRequest
        {
            WorkspaceId = 1,
            WorkspaceSlug = "netptune",
            UserId = session.CreatedBy,
            Session = session,
            Mapping = mapping,
            Source = new MemoryStream(Encoding.UTF8.GetBytes(trimmed)),
            ColumnNames = trimmed.Split('\n')[0].Trim().Split(','),
            ReadOptions = new ImportReadOptions { Delimiter = ',', HasHeaderRow = true },
        };

        return await applier.Commit(request, (_, _) => Task.CompletedTask, cancellationToken);
    }

    private static async Task<ImportSession> NewSession(IServiceScope scope, CancellationToken cancellationToken)
    {
        var unitOfWork = scope.ServiceProvider.GetRequiredService<INetptuneUnitOfWork>();
        var sessions = scope.ServiceProvider.GetRequiredService<IImportSessionRepository>();
        var workspace = await unitOfWork.Workspaces.GetAsync(1, cancellationToken: cancellationToken);
        var userId = workspace!.OwnerId!;
        var session = await sessions.AddAsync(new ImportSession
        {
            WorkspaceId = 1,
            Stage = ImportStage.Committing,
            SourceKind = ImportSourceKind.Csv,
            OriginalName = "applier.csv",
            StorageKey = $"imports/{Guid.NewGuid():N}/applier.csv",
            SizeBytes = 64,
            TargetRecordType = EntityRefTypes.Task,
            TargetBoardIdentifier = BoardIdentifier,
            CreatedBy = userId,
            CreatedByUserId = userId,
            OwnerId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
        }, cancellationToken);

        await unitOfWork.CompleteAsync(cancellationToken);

        return session;
    }

    private static async Task<int> ReadNextTaskScopeId(INetptuneUnitOfWork unitOfWork, int? projectId, CancellationToken cancellationToken)
    {
        var project = await unitOfWork.Projects.GetAsync(projectId!.Value, true, cancellationToken);

        return project!.NextTaskScopeId;
    }
}
