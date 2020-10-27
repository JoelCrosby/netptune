namespace Netptune.Core.Storage
{
    public static class PathConstants
    {
        public const string ProfilePicturePath = "user/profile/";

        public static string MediaPath(string workspaceIdentifier)
        {
            return $"workspace/{workspaceIdentifier}/media/task/";
        }
    }
}
