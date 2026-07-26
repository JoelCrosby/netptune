using System.Text.RegularExpressions;

using Netptune.Core.Entities;

namespace Netptune.Core.Models.Automations;

public static partial class AutomationMessageTemplate
{
    public static IReadOnlySet<string> Variables { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "task.name",
        "task.key",
        "task.status",
        "task.priority",
        "task.startDate",
        "task.dueDate",
        "project.name",
        "workspace.name",
        "rule.name",
    };

    public static string? Validate(string? template)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            return null;
        }

        var unknownVariables = VariablePattern()
            .Matches(template)
            .Select(match => match.Groups[1].Value.Trim())
            .Where(variable => !Variables.Contains(variable))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (unknownVariables.Count > 0)
        {
            return $"Unknown message variables: {string.Join(", ", unknownVariables)}.";
        }

        return null;
    }

    public static string Render(string template, ProjectTask task, AutomationRule rule)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            return template;
        }

        return VariablePattern().Replace(template, match =>
        {
            var variable = match.Groups[1].Value.Trim();

            return ResolveVariable(variable, task, rule);
        });
    }

    private static string ResolveVariable(string variable, ProjectTask task, AutomationRule rule)
    {
        return variable.ToLowerInvariant() switch
        {
            "task.name" => task.Name,
            "task.key" => BuildTaskKey(task),
            "task.status" => task.Status.Name,
            "task.priority" => task.Priority?.ToString() ?? string.Empty,
            "task.startdate" => FormatDate(task.StartDate),
            "task.duedate" => FormatDate(task.DueDate),
            "project.name" => task.Project?.Name ?? string.Empty,
            "workspace.name" => task.Workspace?.Name ?? string.Empty,
            "rule.name" => rule.Name,
            _ => string.Empty,
        };
    }

    private static string BuildTaskKey(ProjectTask task)
    {
        if (task.Project is null)
        {
            return task.Id.ToString();
        }

        return $"{task.Project.Key}-{task.ProjectScopeId}";
    }

    private static string FormatDate(DateOnly? value)
    {
        return value?.ToString("yyyy-MM-dd") ?? string.Empty;
    }

    [GeneratedRegex(@"\{\{([^{}]*)\}\}")]
    private static partial Regex VariablePattern();
}
