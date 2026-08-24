namespace Netptune.Core.Storage;

public static class BrandingContent
{
    public static string Url(string workspaceKey, string contentId)
    {
        return $"/api/workspaces/{workspaceKey}/files/{contentId}/content?disposition=inline";
    }
}
