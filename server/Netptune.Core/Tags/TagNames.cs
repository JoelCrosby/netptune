using Netptune.Core.Extensions;

namespace Netptune.Core.Tags;

public static class TagNames
{
    public static string Normalize(string value) => value.Trim().Capitalize();
}
