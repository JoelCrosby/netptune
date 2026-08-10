using System.Text.Json;

using Netptune.Core.Authorization;
using Netptune.Core.Models.Ai;
using Netptune.Core.Services.Ai;

namespace Netptune.Ai.Tools;

public sealed class AskQuestionTool : IAiTool
{
    private const int MaxQuestionLength = 300;
    private const int MaxHeaderLength = 12;
    private const int MaxLabelLength = 60;
    private const int MaxDescriptionLength = 120;
    private const int MinimumOptions = 2;
    private const int MaximumOptions = 4;

    private readonly IAiQuestionSink Questions;
    private readonly IAiChangeSetBuilder ChangeSet;

    public AskQuestionTool(IAiQuestionSink questions, IAiChangeSetBuilder changeSet)
    {
        Questions = questions;
        ChangeSet = changeSet;
    }

    public string Name => "ask_question";

    public string Description =>
        "Ask the user a multiple choice question, when their answer decides what you do next and no other tool can "
        + "tell you. Offer two to four options, each one a thing you would actually do. The user can also type an "
        + "answer of their own. Asking ends your turn — their answer arrives as their next message.";

    public AiToolKind Kind => AiToolKind.Question;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Workspace.Read };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "question": { "type": "string", "description": "The question, in one sentence." },
          "header": { "type": "string", "description": "A two or three word label for what is being decided, such as “Project”." },
          "options": {
            "type": "array",
            "minItems": 2,
            "maxItems": 4,
            "description": "Between two and four answers to choose from.",
            "items": {
              "type": "object",
              "properties": {
                "label": { "type": "string", "description": "The answer, in a few words." },
                "description": { "type": "string", "description": "One line saying what choosing this would mean." }
              },
              "required": ["label"],
              "additionalProperties": false
            }
          },
          "multiSelect": { "type": "boolean", "description": "Set when more than one option can be chosen together." }
        }
        """,
        "question",
        "options");

    public Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var hasProposals = ChangeSet.Changes.Count > 0;

        if (hasProposals)
        {
            return Failed(
                "You have already proposed changes this turn, so there is nothing left to ask about. "
                + "Ask before proposing, or let the user review what you have.");
        }

        var hasQuestion = Questions.Pending is not null;

        if (hasQuestion)
        {
            return Failed("You have already asked a question this turn. Wait for the answer before asking another.");
        }

        var read = Read(arguments);

        if (read.Question is null)
        {
            return Failed(read.Error ?? "The question could not be read.");
        }

        Questions.Ask(read.Question);

        return Task.FromResult(AiToolExecution.Success(
            "The question is with the user. Stop here — their answer arrives as their next message."));
    }

    private static Task<AiToolExecution> Failed(string message)
    {
        return Task.FromResult(AiToolExecution.Failed(message));
    }

    private sealed record QuestionRead(AiQuestion? Question, string? Error);

    private static QuestionRead Read(JsonElement arguments)
    {
        var text = AiToolSchema.GetString(arguments, "question")?.Trim();
        var hasText = !string.IsNullOrWhiteSpace(text);

        if (!hasText)
        {
            return new QuestionRead(null, "A question is required.");
        }

        if (text!.Length > MaxQuestionLength)
        {
            return new QuestionRead(null, $"The question must be {MaxQuestionLength} characters or fewer.");
        }

        var options = ReadOptions(arguments);
        var hasEnoughOptions = options.Count >= MinimumOptions && options.Count <= MaximumOptions;

        if (!hasEnoughOptions)
        {
            return new QuestionRead(
                null,
                $"Offer between {MinimumOptions} and {MaximumOptions} options. The user can always type their own answer instead.");
        }

        var labelError = FindLabelError(options);

        if (labelError is not null)
        {
            return new QuestionRead(null, labelError);
        }

        var question = new AiQuestion
        {
            Id = Guid.NewGuid(),
            Text = text,
            Header = ReadHeader(arguments),
            Options = options,
            MultiSelect = AiToolSchema.GetBool(arguments, "multiSelect") ?? false,
        };

        return new QuestionRead(question, null);
    }

    // The header and the descriptions only decorate the card, so an over-long one is trimmed rather
    // than bounced back for a retry the user would wait through.
    private static string? ReadHeader(JsonElement arguments)
    {
        var header = AiToolSchema.GetString(arguments, "header")?.Trim();
        var hasHeader = !string.IsNullOrWhiteSpace(header);

        if (!hasHeader)
        {
            return null;
        }

        return Shorten(header!, MaxHeaderLength);
    }

    private static string? FindLabelError(List<AiQuestionOption> options)
    {
        var isAnyEmpty = options.Any(option => string.IsNullOrWhiteSpace(option.Label));

        if (isAnyEmpty)
        {
            return "Every option needs a label.";
        }

        var isAnyTooLong = options.Any(option => option.Label.Length > MaxLabelLength);

        if (isAnyTooLong)
        {
            return $"Option labels must be {MaxLabelLength} characters or fewer.";
        }

        var labels = options.Select(option => option.Label).ToList();
        var distinctLabels = labels.Distinct(StringComparer.OrdinalIgnoreCase).Count();
        var hasDuplicates = distinctLabels != labels.Count;

        if (hasDuplicates)
        {
            return "Every option must read differently, so the answer says which one was chosen.";
        }

        return null;
    }

    private static List<AiQuestionOption> ReadOptions(JsonElement arguments)
    {
        var isObject = arguments.ValueKind == JsonValueKind.Object;

        if (!isObject)
        {
            return [];
        }

        var hasOptions = arguments.TryGetProperty("options", out var value) && value.ValueKind == JsonValueKind.Array;

        if (!hasOptions)
        {
            return [];
        }

        return value.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.Object)
            .Select(ReadOption)
            .ToList();
    }

    private static AiQuestionOption ReadOption(JsonElement item)
    {
        var description = AiToolSchema.GetString(item, "description")?.Trim();
        var hasDescription = !string.IsNullOrWhiteSpace(description);

        return new AiQuestionOption
        {
            Label = AiToolSchema.GetString(item, "label")?.Trim() ?? string.Empty,
            Description = hasDescription ? Shorten(description!, MaxDescriptionLength) : null,
        };
    }

    private static string Shorten(string value, int maximumLength)
    {
        var isWithinLimit = value.Length <= maximumLength;

        if (isWithinLimit)
        {
            return value;
        }

        return value[..maximumLength].TrimEnd();
    }
}
