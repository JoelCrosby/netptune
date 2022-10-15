using System;

using HashidsNet;

using shortid;
using shortid.Configuration;

namespace Netptune.Core.Utilities;

public static class UniqueIdBuilder
{
    private static readonly GenerationOptions ShortIdOptions = new(true, false, 12);

    public static string Generate(GenerationOptions? options = null)
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
