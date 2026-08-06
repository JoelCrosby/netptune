using System.Text;
using System.Text.Json;

using Netptune.Core.Encoding;
using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;
using Netptune.Core.Services.Ai;
using Netptune.Transfer;
using Netptune.Transfer.Import;
using Netptune.Transfer.Services;

namespace Netptune.Ai.Execution;

// One shot, non-conversational, no tools, no change set — the same shape as AiTitleGenerator. It
// refines a mapping the heuristic suggester already produced and its answer is validated against the
// catalog before anything downstream sees it.
public sealed class AiImportMappingAdvisor : IAiImportMappingAdvisor
{
    private const int TokensPerColumn = 120;
    private const int MinimumOutputTokens = 512;
    private const int MaximumOutputTokens = 4096;

    private const string SystemPrompt =
        "You map columns from an uploaded file onto a fixed set of target fields. "
        + "Reply with JSON only — no prose, no code fences — shaped as "
        + "{\"bindings\":[{\"fieldKey\":string,\"columnIndex\":number,\"transforms\":[{\"kind\":string,\"argument\":string}],"
        + "\"valueMap\":{\"source\":\"target\"},\"confidence\":number,\"rationale\":string}],\"unmapped\":[number],\"notes\":string}. "
        + "Only ever use a fieldKey from the target fields listed, and a columnIndex from the source columns listed. "
        + "Bind each field at most once and each column at most once. Leave a column out rather than guessing. "
        + "A starting mapping is provided: keep what is right and change only what is wrong.";

    private readonly IAiChatProviderFactory ProviderFactory;

    public AiImportMappingAdvisor(IAiChatProviderFactory providerFactory)
    {
        ProviderFactory = providerFactory;
    }

    public async Task<AiImportMappingResult> Suggest(AiImportMappingRequest request, CancellationToken cancellationToken)
    {
        var provider = ProviderFactory.Resolve(request.Provider);
        var chatRequest = new AiChatRequest
        {
            Model = AiModels.TitleModelFor(request.Provider),
            SystemPrompt = SystemPrompt,
            Messages = [new AiChatMessage { Role = AiMessageRole.User, Text = BuildPrompt(request) }],
            Tools = [],
            MaxOutputTokens = OutputTokensFor(request.Profile),
        };

        var text = new StringBuilder();
        var usage = new AiUsage();

        await foreach (var providerEvent in provider.Stream(chatRequest, request.ApiKey, cancellationToken))
        {
            var turn = providerEvent.CompletedTurn;

            if (turn is null)
            {
                continue;
            }

            text.Append(turn.Text);
            usage = turn.Usage;
        }

        var proposal = Parse(text.ToString());
        var validated = AiMappingProposalValidator.Validate(
            proposal,
            request.RecordType,
            request.Profile,
            request.Vocabulary);

        return new AiImportMappingResult
        {
            Mapping = validated.Mapping,
            DiscardedBindings = validated.DiscardedBindings,
            DiscardReasons = validated.DiscardReasons,
            Notes = validated.Notes,
            Usage = usage,
        };
    }

    public static AiMappingProposal? Parse(string? raw)
    {
        var json = ExtractJson(raw);

        if (json is null)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<AiMappingProposal>(json, JsonOptions.Default);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    // Models fence their JSON or add a sentence around it more often than they should.
    public static string? ExtractJson(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var start = raw.IndexOf('{');
        var end = raw.LastIndexOf('}');
        var isBracketed = start >= 0 && end > start;

        if (!isBracketed)
        {
            return null;
        }

        return raw[start..(end + 1)];
    }

    private static int OutputTokensFor(ImportSourceProfile profile)
    {
        var scaled = profile.Columns.Count * TokensPerColumn;

        return Math.Clamp(scaled, MinimumOutputTokens, MaximumOutputTokens);
    }

    private static string BuildPrompt(AiImportMappingRequest request)
    {
        var builder = new StringBuilder();
        var recordType = TransferFieldCatalog.FindRecordType(request.RecordType);

        builder.AppendLine("Target fields:");

        foreach (var field in recordType?.Fields ?? [])
        {
            var traits = new List<string> { field.ValueType.ToString() };

            if (field.IsCollection)
            {
                traits.Add("multi-value");
            }

            if (field.IsRequiredForImport)
            {
                traits.Add("required");
            }

            if (field.RefType is not null)
            {
                traits.Add($"references {field.RefType}");
            }

            builder.AppendLine($"- {field.Key} \"{field.Name}\" ({string.Join(", ", traits)})");
        }

        builder.AppendLine();
        builder.AppendLine("Source columns:");

        foreach (var column in request.Profile.Columns)
        {
            var samples = request.AllowDataSampling && column.SampleValues.Count > 0
                ? $" examples: {string.Join(" | ", column.SampleValues)}"
                : string.Empty;

            builder.AppendLine(
                $"- [{column.Index}] \"{column.Name}\" ({column.InferredType}, {column.DistinctCount} distinct){samples}");
        }

        AppendVocabulary(builder, request.Vocabulary);

        builder.AppendLine();
        builder.AppendLine("Starting mapping:");

        foreach (var binding in request.HeuristicMapping.Bindings)
        {
            builder.AppendLine($"- {binding.FieldKey} <- column {binding.ColumnIndex} (confidence {binding.Confidence})");
        }

        return builder.ToString();
    }

    private static void AppendVocabulary(StringBuilder builder, ImportSuggestionVocabulary? vocabulary)
    {
        if (vocabulary is null)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("Values the mapping has to land on:");

        AppendValues(builder, "status keys", vocabulary.StatusKeysByName.Values);
        AppendValues(builder, "tags", vocabulary.TagNames);
        AppendValues(builder, "member emails", vocabulary.MemberEmails);
        AppendValues(builder, "project keys", vocabulary.ProjectKeys);
    }

    private static void AppendValues(StringBuilder builder, string name, IEnumerable<string> values)
    {
        var listed = values.Take(50).ToList();

        if (listed.Count == 0)
        {
            return;
        }

        builder.AppendLine($"- {name}: {string.Join(", ", listed)}");
    }
}
