using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Netptune.Ai.Tools;

internal static class AiToolJson
{
    // The default encoder writes every non-ASCII character and every <, >, & and ' as a six character
    // escape, which can more than triple a page of text. Results go to a model, not into HTML.
    public static readonly JsonSerializerOptions Options = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static string Relax(string content)
    {
        var looksLikeJson = content.Length > 0 && content[0] is '{' or '[';

        if (!looksLikeJson)
        {
            return content;
        }

        try
        {
            var node = JsonNode.Parse(content);

            return node is null ? content : node.ToJsonString(Options);
        }
        catch (JsonException)
        {
            return content;
        }
    }
}
