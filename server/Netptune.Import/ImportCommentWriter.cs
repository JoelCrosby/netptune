using System.Text.Json;
using System.Text.Json.Serialization;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Localization;
using Netptune.Core.UnitOfWork;
using Netptune.Transfer.Entities;
using Netptune.Transfer.Enums;
using Netptune.Transfer.Repositories;
using Netptune.Transfer.Services;

namespace Netptune.Import;

public sealed record ImportedComment
{
    [JsonConverter(typeof(StringOrNumberJsonConverter))]
    public string? Task { get; init; }

    [JsonConverter(typeof(StringOrNumberJsonConverter))]
    public string? Id { get; init; }

    public string? AuthorEmail { get; init; }

    public string? AuthorName { get; init; }

    public DateTimeOffset? CreatedAt { get; init; }

    public string? Body { get; init; }
}

internal sealed class StringOrNumberJsonConverter : JsonConverter<string?>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            using var number = JsonDocument.ParseValue(ref reader);

            return number.RootElement.GetRawText();
        }

        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Expected a string or number, found {reader.TokenType}.");
        }

        return reader.GetString();
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}

public sealed class ImportCommentWriter
{
    private const int BatchSize = 500;
    private const int MaxBodyLength = 32768;
    private const int MaxExternalIdLength = 256;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IImportSessionRepository ImportSessions;

    public ImportCommentWriter(INetptuneUnitOfWork unitOfWork, IImportSessionRepository importSessions)
    {
        UnitOfWork = unitOfWork;
        ImportSessions = importSessions;
    }

    public async Task<int> Write(
        Stream source,
        IReadOnlyDictionary<string, int> taskIdsBySourceId,
        ImportContext context,
        ImportApplyRequest request,
        CancellationToken cancellationToken)
    {
        var comments = await Read(source, taskIdsBySourceId, cancellationToken);

        if (comments.Count == 0)
        {
            return 0;
        }

        var taskIds = comments.Select(comment => comment.TaskId).Distinct().ToList();
        var imported = await UnitOfWork.Comments.GetTaskCommentExternalIds(request.WorkspaceId, taskIds, cancellationToken);
        var created = 0;

        var fresh = comments
            .Where(comment => imported.Add($"{comment.TaskId}:{comment.ExternalId}"))
            .ToList();

        foreach (var batch in fresh.Chunk(BatchSize))
        {
            var entities = batch.Select(comment => ToEntity(comment, context, request)).ToList();

            await UnitOfWork.Comments.AddRangeAsync(entities, cancellationToken);
            await UnitOfWork.CompleteAsync(cancellationToken);

            var entries = entities.Select(entity => new ImportSessionEntry
            {
                SessionId = request.Session.Id,
                EntityType = EventEntityTypes.From(EntityType.Comment),
                EntityId = entity.Id,
                EntityUpdatedAt = entity.UpdatedAt,
                Operation = ImportEntryOperation.Created,
            });

            await ImportSessions.AddEntries(entries, cancellationToken);
            await UnitOfWork.CompleteAsync(cancellationToken);

            created += entities.Count;
        }

        return created;
    }

    private static async Task<List<PendingComment>> Read(
        Stream source,
        IReadOnlyDictionary<string, int> taskIdsBySourceId,
        CancellationToken cancellationToken)
    {
        var comments = new List<PendingComment>();
        using var reader = new StreamReader(source);

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            var comment = Parse(line);
            var isUsable = comment is { Task: not null, Body: not null }
                && comment.Id is { Length: > 0 and <= MaxExternalIdLength };

            if (!isUsable)
            {
                continue;
            }

            var isImportedTask = taskIdsBySourceId.TryGetValue(comment!.Task!.Trim(), out var taskId);

            if (isImportedTask)
            {
                comments.Add(new PendingComment(taskId, comment.Id!, comment));
            }
        }

        return comments;
    }

    private static ImportedComment? Parse(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<ImportedComment>(line, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static Comment ToEntity(PendingComment pending, ImportContext context, ImportApplyRequest request)
    {
        var comment = pending.Comment;
        var author = context.Vocabulary.FindUser(comment.AuthorEmail);
        var authorUserId = author?.Id ?? request.UserId;
        var originalAuthor = comment.AuthorName ?? comment.AuthorEmail;
        var body = author is null && originalAuthor is not null
            ? $"{Localized.Format(AppText.ImportCommentOriginalAuthor, originalAuthor)}\n\n{comment.Body}"
            : comment.Body!;

        return new Comment
        {
            Body = body.Length > MaxBodyLength ? body[..MaxBodyLength] : body,
            EntityType = EntityType.Task,
            EntityId = pending.TaskId,
            WorkspaceId = request.WorkspaceId,
            ExternalId = pending.ExternalId,
            OwnerId = authorUserId,
            CreatedByUserId = authorUserId,
            CreatedAt = comment.CreatedAt?.UtcDateTime ?? default,
        };
    }

    private sealed record PendingComment(int TaskId, string ExternalId, ImportedComment Comment);
}
