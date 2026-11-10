using Netptune.Core.Enums;

namespace Netptune.Import;

public static class ImportStatusCategories
{
    private static readonly Dictionary<string, StatusCategory> CategoriesByName = new(StringComparer.OrdinalIgnoreCase)
    {
        ["proposed"] = StatusCategory.New,
        ["new"] = StatusCategory.New,
        ["backlog"] = StatusCategory.Backlog,
        ["to do"] = StatusCategory.Todo,
        ["todo"] = StatusCategory.Todo,
        ["in progress"] = StatusCategory.Active,
        ["active"] = StatusCategory.Active,
        ["resolved"] = StatusCategory.Done,
        ["completed"] = StatusCategory.Done,
        ["done"] = StatusCategory.Done,
        ["closed"] = StatusCategory.Done,
        ["removed"] = StatusCategory.Inactive,
        ["cancelled"] = StatusCategory.Inactive,
        ["inactive"] = StatusCategory.Inactive,
    };

    public static StatusCategory? Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var found = CategoriesByName.TryGetValue(value.Trim(), out var category);

        return found ? category : null;
    }
}
