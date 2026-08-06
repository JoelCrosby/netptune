using System.Collections.Frozen;
using System.Reflection;

namespace Netptune.Transfer;

public static class EntityRefTypes
{
    public const string Workspace = "workspace";
    public const string User = "user";
    public const string Status = "status";
    public const string Tag = "tag";
    public const string RelationType = "relation-type";
    public const string Project = "project";
    public const string Board = "board";
    public const string BoardGroup = "board-group";
    public const string Sprint = "sprint";
    public const string Task = "task";
    public const string Comment = "comment";
    public const string Automation = "automation";
    public const string WorkspaceFile = "workspace-file";

    public static IReadOnlySet<string> All { get; } = typeof(EntityRefTypes)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(field => field.IsLiteral && field.FieldType == typeof(string))
        .Select(field => (string)field.GetRawConstantValue()!)
        .ToFrozenSet();
}
