namespace Netptune.Core.Meta;

public static class MetaMerge
{
    public static WorkspaceMeta? Apply(WorkspaceMeta? current, WorkspaceMeta? incoming)
    {
        if (incoming is null)
        {
            return current;
        }

        return new WorkspaceMeta
        {
            Color = incoming.Color,
            TimeZone = incoming.TimeZone,
            LogoFileId = incoming.LogoFileId ?? current?.LogoFileId,
        };
    }

    public static BoardMeta? Apply(BoardMeta? current, BoardMeta? incoming)
    {
        if (incoming is null)
        {
            return current;
        }

        return new BoardMeta
        {
            Color = incoming.Color,
            LogoFileId = incoming.LogoFileId ?? current?.LogoFileId,
            BackgroundFileId = incoming.BackgroundFileId ?? current?.BackgroundFileId,
        };
    }
}
