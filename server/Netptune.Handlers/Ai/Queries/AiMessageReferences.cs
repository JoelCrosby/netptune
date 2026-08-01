using System.Text.Json;

using Netptune.Core.Entities;
using Netptune.Core.Models.Ai;

namespace Netptune.Handlers.Ai.Queries;

public static class AiMessageReferences
{
    public static Dictionary<long, List<AiEntityReference>> Group(IEnumerable<AiToolInvocation> invocations)
    {
        var grouped = new Dictionary<long, List<AiEntityReference>>();

        foreach (var group in invocations.GroupBy(invocation => invocation.MessageId))
        {
            var results = group.Select(invocation => new AiToolResultText
            {
                ToolName = invocation.ToolName,
                Content = ReadResult(invocation.Result),
            });

            grouped[group.Key] = AiEntityReferenceReader.Read(results);
        }

        return grouped;
    }

    private static string ReadResult(JsonDocument? result)
    {
        if (result is null)
        {
            return string.Empty;
        }

        var element = result.RootElement;
        var isText = element.ValueKind == JsonValueKind.String;

        return isText ? element.GetString() ?? string.Empty : element.GetRawText();
    }
}
