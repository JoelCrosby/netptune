using HashidsNet;

using shortid;

namespace Netptune.Core.Utilities;

public static class UniqueIdBuilder
{
    private static readonly ShortIdOptions ShortIdOptions = new(true, false, 12);

    public static string Generate(ShortIdOptions? options = null)
    {
        options ??= ShortIdOptions;

        return ShortId.Generate(options);
    }

    public static string Generate(string seed)
    {
        var random = new Random();

        var hash = new Hashids(seed);
        var id = hash.Encode(random.Next(0, int.MaxValue));

        return id;
    }
}
