using System.Text;

using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Cache;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
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
        // The numbers used to come from a cursor seeded when the import loaded, so a second run into the
        // same project started again at the same number and collided with the first run's rows.
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
            activity);
    }

    private static async Task<ImportCommitResult> Commit(
        IServiceScope scope,
        string csv,
        CancellationToken cancellationToken,
        ImportSession? session = null)
    {
        var actor = new ActorContext();
        var applier = BuildApplier(scope, actor);

        session ??= await NewSession(scope, cancellationToken);

        using var actorScope = actor.Begin(new ActorIdentity(session.CreatedBy, 1, "netptune"));

        var mapping = new ImportMappingModel
        {
            RecordType = EntityRefTypes.Task,
            Bindings =
            [
                new ImportFieldBinding { FieldKey = TaskFieldKeys.Name, ColumnIndex = 0 },
            ],
        };
        var request = new ImportApplyRequest
        {
            WorkspaceId = 1,
            WorkspaceSlug = "netptune",
            UserId = session.CreatedBy,
            Session = session,
            Mapping = mapping,
            Source = new MemoryStream(Encoding.UTF8.GetBytes(csv.Trim())),
            ColumnNames = ["Name", "group"],
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
