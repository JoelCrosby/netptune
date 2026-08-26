namespace Netptune.Core.Storage;

public static class PathConstants
{
    public const string ProfilePicturePath = "user/profile/";

    public static string MediaPath(string workspaceIdentifier)
    {
        return $"workspace/{workspaceIdentifier}/media/task/";
    }

    public static string BrandingPath(int workspaceId)
    {
        return $"workspace/{workspaceId}/branding/";
    }

    public static string ExportPath(string workspaceIdentifier)
    {
        return $"workspace/{workspaceIdentifier}/exports/";
    }

    public static string ImportPath(string workspaceIdentifier)
    {
        return $"workspace/{workspaceIdentifier}/imports/";
    }
}
