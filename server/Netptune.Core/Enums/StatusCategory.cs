namespace Netptune.Core.Enums;

public enum StatusCategory
{
    Backlog = 0,
    Todo = 1,
    Active = 2,
    Done = 3,
    Inactive = 4,
}

public static class StatusCategoryExtensions
{
    public static BoardGroupType GetGroupTypeFromStatusCategory(this StatusCategory category)
    {
        return category switch
        {
            StatusCategory.Todo => BoardGroupType.Todo,
            StatusCategory.Done => BoardGroupType.Done,
            _ => BoardGroupType.Backlog,
        };
    }
}
