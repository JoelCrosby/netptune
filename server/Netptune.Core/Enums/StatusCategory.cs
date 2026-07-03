namespace Netptune.Core.Enums;

public enum StatusCategory
{
    New = 0,
    Backlog = 1,
    Todo = 2,
    Active = 3,
    Done = 4,
    Inactive = 5,
}

public static class StatusCategoryExtensions
{
    public static BoardGroupType GetGroupTypeFromStatusCategory(this StatusCategory category)
    {
        return category switch
        {
            StatusCategory.New => BoardGroupType.Basic,
            StatusCategory.Backlog => BoardGroupType.Backlog,
            StatusCategory.Todo => BoardGroupType.Todo,
            StatusCategory.Done => BoardGroupType.Done,
            _ => BoardGroupType.Backlog,
        };
    }
}
