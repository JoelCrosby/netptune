using Netptune.Core.Models.Ai;
using Netptune.Core.Services.Ai;

namespace Netptune.Ai.Tools;

public sealed class AiToolRegistry : IAiToolRegistry
{
    private readonly Dictionary<string, IAiTool> ToolsByName;

    public AiToolRegistry(IEnumerable<IAiTool> tools)
    {
        All = tools.ToList();
        ToolsByName = All.ToDictionary(tool => tool.Name, StringComparer.Ordinal);
    }

    public IReadOnlyList<IAiTool> All { get; }

    public IAiTool? Find(string name)
    {
        var found = ToolsByName.TryGetValue(name, out var tool);

        return found ? tool : null;
    }

    public IReadOnlyList<AiToolDefinition> GetDefinitions()
    {
        return All
            .Select(tool => new AiToolDefinition
            {
                Name = tool.Name,
                Description = tool.Description,
                InputSchema = tool.InputSchema,
            })
            .ToList();
    }
}
