using System.Text.Json;
using System.Text.Json.Nodes;

using Netptune.Core.Services.Ai;

namespace Netptune.Ai.Tools;

// One kind of change a tool can propose. It keeps its own change name and permissions, so handlers,
// stored change sets and the apply-time check are the same whichever tool offered it.
public interface IAiToolAction
{
    string ChangeName { get; }

    IReadOnlySet<string> RequiredPermissions { get; }

    Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken);
}

// Lets one tool offer several actions behind an action argument, so the model sees one schema
// instead of one per action.
public sealed class AiToolActions
{
    private const string ActionArgument = "action";

    private readonly Dictionary<string, IAiToolAction> ActionsByName;

    public AiToolActions(Dictionary<string, IAiToolAction> actions)
    {
        ActionsByName = actions;
        ProposedChanges = actions.Values.Select(action => action.ChangeName).ToList();
        AllPermissions = actions.Values
            .SelectMany(action => action.RequiredPermissions)
            .ToHashSet(StringComparer.Ordinal);
    }

    public IReadOnlyList<string> ProposedChanges { get; }

    public IReadOnlySet<string> AllPermissions { get; }

    public bool IsAvailable(IReadOnlySet<string> permissions)
    {
        return ActionsByName.Values.Any(action => action.RequiredPermissions.All(permissions.Contains));
    }

    // An unknown action proposes nothing, and Execute says which actions there are.
    public IReadOnlySet<string> GetRequiredPermissions(JsonElement arguments)
    {
        var action = Find(arguments);

        return action?.RequiredPermissions ?? new HashSet<string>(StringComparer.Ordinal);
    }

    // A change this tool never proposes asks for every permission, so it cannot slip through.
    public IReadOnlySet<string> GetChangePermissions(string changeName)
    {
        var action = ActionsByName.Values.FirstOrDefault(item => item.ChangeName == changeName);

        return action?.RequiredPermissions ?? AllPermissions;
    }

    public string DescribeCall(string toolName, JsonElement arguments)
    {
        return AiToolSchema.DescribeCall(toolName, arguments, ActionArgument, ActionsByName.Keys);
    }

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var action = Find(arguments);

        if (action is null)
        {
            return AiToolExecution.Failed($"action must be one of: {string.Join(", ", ActionsByName.Keys)}.");
        }

        // Some actions store their arguments as the change payload, which never carried an action.
        var withoutAction = JsonNode.Parse(arguments.GetRawText())!.AsObject();

        withoutAction.Remove(ActionArgument);

        var forwarded = JsonSerializer.SerializeToElement(withoutAction);

        return await action.Execute(forwarded, cancellationToken);
    }

    private IAiToolAction? Find(JsonElement arguments)
    {
        var name = AiToolSchema.GetString(arguments, ActionArgument) ?? string.Empty;

        return ActionsByName.GetValueOrDefault(name);
    }
}
