namespace Netptune.Core.Enums;

public enum BoardGroupType
{
    Basic = 0,

    Backlog = 1,

    Done = 2,

    Todo = 3,
}

public static class BoardGroupTypeExtensions
{
    public static StatusCategory GetStatusCategoryFromGroupType(this BoardGroupType type)
    {
        return type switch
        {
            BoardGroupType.Todo => StatusCategory.Active,
            BoardGroupType.Done => StatusCategory.Done,
            _ => StatusCategory.Backlog,
        };
    }
}
