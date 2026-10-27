using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Models.ProjectTasks;
using Netptune.Core.Models.Search;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Ai;
using Netptune.Core.ViewModels.Comments;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Handlers.Comments.Queries;
using Netptune.Handlers.Tasks.Queries;

namespace Netptune.Ai.Tools;

public sealed class FindInTasksTool : IAiTool
{
    private const int DefaultPageSize = 25;
    private const int MaximumPageSize = 100;
    private const int DefaultMatchesPerTask = 5;
    private const int MaximumMatchesPerTask = 20;

    private const string InName = "name";
    private const string InDescription = "description";
    private const string InComments = "comments";

    private static readonly string[] DefaultFields = [InName, InDescription];
    private static readonly string[] KnownFields = [InName, InDescription, InComments];

    private readonly IMediator Mediator;

    public FindInTasksTool(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string Name => "find_in_tasks";

    public string Description =>
        "Scan every task in the workspace, or in one project, for a literal substring or a .NET regular expression "
        + "in its name, its whole markdown description and, when in includes comments, its comment bodies. "
        + "Unlike search_tasks it is exhaustive and exact, not ranked, and reads past the start of each description. "
        + AiTextMatcher.RegexDescription
        + "Returns each matching task's taskId, systemId, name and project, with where each of its first matches is "
        + "(name, description or a comment by commentId), its line number and the text of that line, and when "
        + "scanning comments, how many of its comments match. "
        + "tasksMatched counts every matching task; ask for the next page until hasMore is false. "
        + "omittedForSize counts matching tasks on this page that did not fit; ask again with a smaller pageSize.";

    public AiToolKind Kind => AiToolKind.Read;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Tasks.Read };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "pattern": { "type": "string", "description": "The text, or the regular expression when regex is true." },
          "regex": { "type": "boolean", "description": "Treat pattern as a regular expression. Defaults to false." },
          "caseSensitive": { "type": "boolean", "description": "Match case exactly. Defaults to false." },
          "in": {
            "type": "array",
            "items": { "type": "string", "enum": ["name", "description", "comments"] },
            "description": "Which text to scan. Defaults to name and description."
          },
          "projectId": { "type": "integer", "description": "Only scan tasks in this project." },
          "page": { "type": "integer", "description": "Which page of results to return, starting at 1." },
          "pageSize": { "type": "integer", "description": "How many matching tasks to return per page, up to 100. Defaults to 25." },
          "matchesPerTask": { "type": "integer", "description": "How many matches to show per task, up to 20. Defaults to 5." }
        }
        """,
        "pattern");

    public IReadOnlySet<string> GetRequiredPermissions(JsonElement arguments)
    {
        var fields = ReadFields(arguments);
        var required = new HashSet<string>(RequiredPermissions, StringComparer.Ordinal);
        var scansComments = fields.Contains(InComments);

        if (scansComments)
        {
            required.Add(NetptunePermissions.Comments.Read);
        }

        return required;
    }

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var pattern = AiToolSchema.GetString(arguments, "pattern");
        var hasPattern = !string.IsNullOrEmpty(pattern);

        if (!hasPattern)
        {
            return AiToolExecution.Failed("A pattern is required.");
        }

        var fields = ReadFields(arguments);
        var unknownField = fields.FirstOrDefault(field => !KnownFields.Contains(field));

        if (unknownField is not null)
        {
            return AiToolExecution.Failed($"“{unknownField}” cannot be scanned. Use name, description or comments.");
        }

        var isRegex = AiToolSchema.GetBool(arguments, "regex") ?? false;
        var isCaseSensitive = AiToolSchema.GetBool(arguments, "caseSensitive") ?? false;
        var matcher = AiTextMatcher.Create(pattern!, isRegex, isCaseSensitive);

        if (matcher is null)
        {
            return AiToolExecution.Failed(AiTextMatcher.InvalidMessage(pattern!));
        }

        var requestedPageSize = AiToolSchema.GetInt(arguments, "pageSize") ?? DefaultPageSize;
        var pageSize = Math.Clamp(requestedPageSize, 1, MaximumPageSize);
        var page = Math.Max(AiToolSchema.GetInt(arguments, "page") ?? 1, 1);
        var requestedMatchesPerTask = AiToolSchema.GetInt(arguments, "matchesPerTask") ?? DefaultMatchesPerTask;
        var matchesPerTask = Math.Clamp(requestedMatchesPerTask, 1, MaximumMatchesPerTask);
        var textPattern = new TextPattern(pattern!, isRegex, isCaseSensitive);
        var scansComments = fields.Contains(InComments);

        var search = new TaskTextSearch
        {
            Pattern = textPattern,
            ProjectId = AiToolSchema.GetInt(arguments, "projectId"),
            InName = fields.Contains(InName),
            InDescription = fields.Contains(InDescription),
            InComments = scansComments,
            Page = page,
            PageSize = pageSize,
        };

        var result = await Mediator.Send(new FindTasksByTextQuery(search), cancellationToken);

        var hasTasks = result.IsSuccess && result.Payload is not null;

        if (!hasTasks)
        {
            return AiToolExecution.Failed(result.Message ?? "Tasks could not be searched.");
        }

        var found = result.Payload!;
        var taskIds = found.Items.Select(task => task.Id).ToList();
        var readsComments = scansComments && taskIds.Count > 0;
        var commentMatches = new TaskCommentMatches([], []);

        if (readsComments)
        {
            var commentResult = await ReadComments(taskIds, textPattern, pageSize * matchesPerTask, cancellationToken);

            var hasComments = commentResult.IsSuccess && commentResult.Payload is not null;

            if (!hasComments)
            {
                return AiToolExecution.Failed(commentResult.Message ?? "Comments could not be searched.");
            }

            commentMatches = commentResult.Payload!;
        }

        var context = new TaskContext
        {
            Search = search,
            CommentsByTask = commentMatches.Comments.ToLookup(comment => comment.TaskId),
            CommentCounts = commentMatches.CountsByTask,
            Matcher = matcher,
            MatchesPerTask = matchesPerTask,
        };
        List<object> rows;

        try
        {
            rows = found.Items.Select(task => DescribeTask(task, context)).ToList();
        }
        catch (RegexMatchTimeoutException)
        {
            return AiToolExecution.Failed(AiTextMatcher.TimeoutMessage);
        }

        var hasMore = AiToolSchema.HasMore(page, pageSize, found.TotalCount);
        var fitted = AiResultBudget.Fit(rows);
        var listing = new JsonObject
        {
            ["tasksMatched"] = found.TotalCount,
            ["page"] = page,
            ["hasMore"] = hasMore,
            ["tasks"] = fitted.Rows,
        };

        if (fitted.Omitted > 0)
        {
            listing["omittedForSize"] = fitted.Omitted;
        }

        return AiToolExecution.Success(listing.ToJsonString());
    }

    private static List<string> ReadFields(JsonElement arguments)
    {
        var requested = AiToolSchema.GetStringArray(arguments, "in")
            .Select(field => field.Trim().ToLowerInvariant())
            .Where(field => field.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return requested.Count > 0 ? requested : [.. DefaultFields];
    }

    private async Task<ClientResponse<TaskCommentMatches>> ReadComments(
        IReadOnlyCollection<int> taskIds,
        TextPattern pattern,
        int take,
        CancellationToken cancellationToken)
    {
        var query = new FindTaskCommentsByTextQuery(taskIds, pattern, take);

        return await Mediator.Send(query, cancellationToken);
    }

    private static List<object> FindMatches(Regex matcher, List<TextSource> sources, int take)
    {
        var shown = new List<object>();

        foreach (var source in sources)
        {
            var remaining = take - shown.Count;

            if (remaining <= 0)
            {
                break;
            }

            var matches = AiTextMatcher.Find(matcher, source.Text, remaining);
            var described = matches.Shown.Select(match => new
            {
                @in = source.In,
                commentId = source.CommentId,
                line = match.Line,
                text = match.Text,
            });

            shown.AddRange(described);
        }

        return shown;
    }

    private static object DescribeTask(TaskTextViewModel task, TaskContext context)
    {
        var sources = new List<TextSource>();

        if (context.Search.InName)
        {
            sources.Add(new TextSource(InName, null, task.Name));
        }

        if (context.Search.InDescription)
        {
            sources.Add(new TextSource(InDescription, null, task.Description));
        }

        var commentSources = context.CommentsByTask[task.Id]
            .Select(comment => new TextSource("comment", comment.Id, comment.Body));

        sources.AddRange(commentSources);

        var shown = FindMatches(context.Matcher, sources, context.MatchesPerTask);
        int? matchingComments = context.Search.InComments ? context.CommentCounts.GetValueOrDefault(task.Id) : null;

        return new
        {
            taskId = task.Id,
            systemId = task.SystemId,
            name = task.Name,
            project = task.ProjectName,
            matchingComments,
            matches = shown,
        };
    }

    private sealed record TaskContext
    {
        public required TaskTextSearch Search { get; init; }

        public required ILookup<int, TaskCommentTextViewModel> CommentsByTask { get; init; }

        public required Dictionary<int, int> CommentCounts { get; init; }

        public required Regex Matcher { get; init; }

        public int MatchesPerTask { get; init; }
    }

    private sealed record TextSource(string In, int? CommentId, string Text);
}
