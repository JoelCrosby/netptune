using System.Net.Http.Json;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Constants;
using Netptune.Core.Entities;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Comments;
using Netptune.Entities.Contexts;

using Npgsql;

using Xunit;

namespace Netptune.IntegrationTests.Repositories;

public sealed class ReactionRepositoryTests
{
    private readonly NetptuneFixture Fixture;
    private readonly HttpClient Client;

    public ReactionRepositoryTests(NetptuneFixture fixture)
    {
        Fixture = fixture;
        Client = fixture.CreateNetptuneClient();
    }

    [Fact]
    public async Task TryAdd_ShouldRecordTheOwnerAsCreator_WhenTheReactionIsNew()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var comment = await CreateComment();
        await using var scope = Fixture.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<INetptuneUnitOfWork>();
        var reaction = await NewReaction(context, comment, ReactionValues.Rocket);

        var inserted = await unitOfWork.Reactions.TryAdd(reaction, cancellationToken);
        var stored = await context.Reactions
            .AsNoTracking()
            .SingleAsync(row => row.CommentId == comment.Id, cancellationToken);

        inserted.Should().BeTrue();
        stored.OwnerId.Should().Be(comment.UserId);
        stored.CreatedByUserId.Should().Be(comment.UserId);
    }

    [Fact]
    public async Task TryAdd_ShouldRollBackWithTheCurrentTransaction_WhenTheTransactionIsRolledBack()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var comment = await CreateComment();
        await using var scope = Fixture.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<INetptuneUnitOfWork>();
        var reaction = await NewReaction(context, comment, ReactionValues.Heart);

        await using (var transaction = await context.Database.BeginTransactionAsync(cancellationToken))
        {
            var inserted = await unitOfWork.Reactions.TryAdd(reaction, cancellationToken);
            var duplicate = await unitOfWork.Reactions.TryAdd(reaction, cancellationToken);

            inserted.Should().BeTrue();
            duplicate.Should().BeFalse();

            await transaction.RollbackAsync(cancellationToken);
        }

        var exists = await context.Reactions.AnyAsync(row => row.CommentId == comment.Id, cancellationToken);

        exists.Should().BeFalse();
    }

    [Fact]
    public async Task SaveChanges_ShouldRejectTheReaction_WhenItHasNoOwner()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var comment = await CreateComment();
        await using var scope = Fixture.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var reaction = await NewReaction(context, comment, ReactionValues.Eyes);

        context.Reactions.Add(reaction with { OwnerId = null });

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync(cancellationToken));

        exception.InnerException.Should().BeOfType<PostgresException>()
            .Which.SqlState.Should().Be(PostgresErrorCodes.NotNullViolation);
    }

    [Fact]
    public async Task SaveChanges_ShouldRejectTheReaction_WhenTheOwnerAlreadyReactedWithThatValue()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var comment = await CreateComment();
        await using var scope = Fixture.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var reaction = await NewReaction(context, comment, ReactionValues.Eyes);

        context.Reactions.Add(reaction);
        await context.SaveChangesAsync(cancellationToken);
        context.ChangeTracker.Clear();
        context.Reactions.Add(reaction with { Id = 0 });

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync(cancellationToken));

        exception.InnerException.Should().BeOfType<PostgresException>()
            .Which.ConstraintName.Should().Be(DatabaseConstraints.ReactionCommentOwnerValue);
    }

    private static async Task<Reaction> NewReaction(DataContext context, CommentViewModel comment, string value)
    {
        var workspaceId = await context.Comments
            .Where(row => row.Id == comment.Id)
            .Select(row => row.WorkspaceId)
            .SingleAsync(TestContext.Current.CancellationToken);

        return new Reaction
        {
            CommentId = comment.Id,
            WorkspaceId = workspaceId,
            OwnerId = comment.UserId,
            Value = value,
        };
    }

    private async Task<CommentViewModel> CreateComment()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var response = await Client.PostAsJsonAsync("api/comments/task", new AddCommentRequest
        {
            SystemId = "neo-1",
            Comment = $"Reaction repository {Guid.NewGuid():N}",
        }, cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<CommentViewModel>>(cancellationToken);

        return result.Payload!;
    }
}
