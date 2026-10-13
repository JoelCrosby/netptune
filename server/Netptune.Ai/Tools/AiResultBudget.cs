using System.Text.Json;
using System.Text.Json.Nodes;

namespace Netptune.Ai.Tools;

public sealed record AiBudgetedRows(JsonArray Rows, int Omitted);

public static class AiResultBudget
{
    // Below Ai:MaxToolResultCharacters, so the runner never cuts a result off mid-JSON.
    public const int Characters = 28_000;

    public static AiBudgetedRows Fit(IEnumerable<object> rows, JsonSerializerOptions? options = null)
    {
        var fitted = new JsonArray();
        var usedCharacters = 0;
        var omitted = 0;

        foreach (var row in rows)
        {
            var node = JsonSerializer.SerializeToNode(row, options);
            var rowCharacters = node?.ToJsonString().Length ?? 0;
            var exceedsBudget = omitted > 0 || usedCharacters + rowCharacters > Characters;

            if (exceedsBudget)
            {
                omitted++;

                continue;
            }

            usedCharacters += rowCharacters;
            fitted.Add(node);
        }

        return new AiBudgetedRows(fitted, omitted);
    }
}
