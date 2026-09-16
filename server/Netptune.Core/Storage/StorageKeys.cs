namespace Netptune.Core.Storage;

public static class StorageKeys
{

    public static string? TryResolveProfilePictureKey(string? pictureUrl)
    {
        if (string.IsNullOrWhiteSpace(pictureUrl))
        {
            return null;
        }

        if (!Uri.TryCreate(pictureUrl, UriKind.Absolute, out var uri))
        {
            return null;
        }

        // A profile picture URL can also be an external provider avatar,
        // which has no storage key and must never be treated as one.

        var key = uri.AbsolutePath.TrimStart('/');
        var isProfilePicture = key.StartsWith(PathConstants.ProfilePicturePath, StringComparison.Ordinal);

        return isProfilePicture ? key : null;
    }
}
