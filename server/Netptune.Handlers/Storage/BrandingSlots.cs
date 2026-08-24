using Netptune.Core.Enums;
using Netptune.Core.Storage;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Storage;

// The four branding slots live in three different jsonb documents. Resolving them to a common
// read/assign pair keeps the upload and remove handlers free of per-target branching.
internal sealed record BrandingSlot
{
    public required string? CurrentFileId { get; init; }

    // Writes the single key rather than the whole document, so uploading a logo and a background
    // together cannot leave one of them overwritten by the other's stale copy of the meta.
    public required Func<string?, CancellationToken, Task> Assign { get; init; }
}

internal static class BrandingSlots
{
    public static async Task<BrandingSlot?> Resolve(
        INetptuneUnitOfWork unitOfWork,
        BrandingImageTarget target,
        int? targetId,
        int workspaceId,
        CancellationToken cancellationToken)
    {
        switch (target)
        {
            case BrandingImageTarget.WorkspaceLogo:
                return await ResolveWorkspace(unitOfWork, workspaceId, cancellationToken);

            case BrandingImageTarget.ProjectLogo:
                return await ResolveProject(unitOfWork, targetId, workspaceId, cancellationToken);

            case BrandingImageTarget.BoardLogo:
            case BrandingImageTarget.BoardBackground:
                return await ResolveBoard(unitOfWork, target, targetId, workspaceId, cancellationToken);

            default:
                return null;
        }
    }

    private static async Task<BrandingSlot?> ResolveWorkspace(INetptuneUnitOfWork unitOfWork, int workspaceId, CancellationToken cancellationToken)
    {
        var workspace = await unitOfWork.Workspaces.GetAsync(workspaceId, isReadonly: true, cancellationToken);

        if (workspace is null)
        {
            return null;
        }

        return new BrandingSlot
        {
            CurrentFileId = workspace.MetaInfo?.LogoFileId,
            Assign = (fileId, token) => unitOfWork.Workspaces.SetBrandingFile(workspaceId, BrandingMetaKeys.Logo, fileId, token),
        };
    }

    private static async Task<BrandingSlot?> ResolveProject(
        INetptuneUnitOfWork unitOfWork,
        int? targetId,
        int workspaceId,
        CancellationToken cancellationToken)
    {
        if (targetId is not { } projectId)
        {
            return null;
        }

        var project = await unitOfWork.Projects.GetInWorkspace(projectId, workspaceId, isReadonly: true, cancellationToken);

        if (project is null)
        {
            return null;
        }

        return new BrandingSlot
        {
            CurrentFileId = project.MetaInfo?.LogoFileId,
            Assign = (fileId, token) => unitOfWork.Projects.SetBrandingFile(projectId, workspaceId, BrandingMetaKeys.Logo, fileId, token),
        };
    }

    private static async Task<BrandingSlot?> ResolveBoard(
        INetptuneUnitOfWork unitOfWork,
        BrandingImageTarget target,
        int? targetId,
        int workspaceId,
        CancellationToken cancellationToken)
    {
        if (targetId is not { } boardId)
        {
            return null;
        }

        var board = await unitOfWork.Boards.GetInWorkspace(boardId, workspaceId, isReadonly: true, cancellationToken);

        if (board is null)
        {
            return null;
        }

        var isBackground = target is BrandingImageTarget.BoardBackground;
        var metaKey = isBackground ? BrandingMetaKeys.Background : BrandingMetaKeys.Logo;
        var currentFileId = isBackground ? board.MetaInfo?.BackgroundFileId : board.MetaInfo?.LogoFileId;

        return new BrandingSlot
        {
            CurrentFileId = currentFileId,
            Assign = (fileId, token) => unitOfWork.Boards.SetBrandingFile(boardId, workspaceId, metaKey, fileId, token),
        };
    }
}
