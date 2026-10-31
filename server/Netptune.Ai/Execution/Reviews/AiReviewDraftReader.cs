using System.Text;
using System.Text.Json;

using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;

namespace Netptune.Ai.Execution.Reviews;

// Reads the reply while it streams, so the summary and each finding's title show before the reply is done.
// The reply is read from the start each time: it is short, and a token cut off mid-stream is left for later.
internal sealed class AiReviewDraftReader
{
    private static readonly JsonReaderOptions ReaderOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip,
    };

    private readonly IReadOnlySet<AiReviewFindingCategory> AllowedCategories;
    private readonly HashSet<int> EmittedPositions = [];

    private bool HasEmittedSummary;

    public AiReviewDraftReader(IReadOnlySet<AiReviewFindingCategory> allowedCategories)
    {
        AllowedCategories = allowedCategories;
    }

    public List<AiReviewStreamEvent> Read(string reply)
    {
        var events = new List<AiReviewStreamEvent>();
        var start = reply.IndexOf('{');

        if (start < 0)
        {
            return events;
        }

        var bytes = Encoding.UTF8.GetBytes(reply[start..]);

        try
        {
            ReadTokens(bytes, events);
        }
        catch (JsonException)
        {
            // Whatever was read before the reply stopped being valid JSON has already been reported.
        }

        return events;
    }

    private void ReadTokens(byte[] bytes, List<AiReviewStreamEvent> events)
    {
        var reader = new Utf8JsonReader(bytes, isFinalBlock: false, new JsonReaderState(ReaderOptions));
        var isInFindings = false;
        var position = -1;
        string? property = null;
        DraftFinding? finding = null;

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.PropertyName:
                    property = reader.GetString();
                    break;

                case JsonTokenType.StartArray when reader.CurrentDepth == 1:
                    isInFindings = property == "findings";
                    break;

                case JsonTokenType.EndArray when reader.CurrentDepth == 1:
                    isInFindings = false;
                    break;

                case JsonTokenType.StartObject when isInFindings && reader.CurrentDepth == 2:
                    position += 1;
                    finding = new DraftFinding(position);
                    break;

                case JsonTokenType.EndObject when reader.CurrentDepth == 2:
                    finding = null;
                    break;

                case JsonTokenType.EndObject when reader.CurrentDepth == 0:
                    return;

                case JsonTokenType.String when reader.CurrentDepth == 1 && property == "summary":
                    ReadSummary(reader.GetString(), events);
                    break;

                case JsonTokenType.String when finding is not null && reader.CurrentDepth == 3:
                    ReadFindingField(finding, property, reader.GetString(), events);
                    break;
            }
        }
    }

    private void ReadSummary(string? value, List<AiReviewStreamEvent> events)
    {
        var summary = AiReviewValidator.Clean(value, AiReviewValidator.MaxSummaryLength);

        if (HasEmittedSummary || summary is null)
        {
            return;
        }

        HasEmittedSummary = true;

        events.Add(AiReviewStreamEvent.Summary(summary));
    }

    private void ReadFindingField(
        DraftFinding finding,
        string? property,
        string? value,
        List<AiReviewStreamEvent> events)
    {
        switch (property)
        {
            case "title":
                finding.Title = AiReviewValidator.Clean(value, AiReviewValidator.MaxTitleLength);
                break;

            case "category":
                finding.Category = AiReviewValidator.ParseCategory(value);
                break;

            default:
                return;
        }

        var isComplete = finding is { Title: not null, Category: not null };
        var isAllowedCategory = isComplete && AllowedCategories.Contains(finding.Category!.Value);
        var hasRoom = EmittedPositions.Count < AiReviewValidator.MaxFindings;
        var isNew = !EmittedPositions.Contains(finding.Position);
        var isReady = isAllowedCategory && hasRoom && isNew;

        if (!isReady)
        {
            return;
        }

        EmittedPositions.Add(finding.Position);

        events.Add(AiReviewStreamEvent.Finding(finding.Category!.Value, finding.Title!));
    }

    private sealed class DraftFinding
    {
        public DraftFinding(int position)
        {
            Position = position;
        }

        public int Position { get; }

        public string? Title { get; set; }

        public AiReviewFindingCategory? Category { get; set; }
    }
}
