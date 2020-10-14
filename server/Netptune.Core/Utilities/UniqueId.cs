using System;

using HashidsNet;

namespace Netptune.Core.Utilities
{
    public static class UniqueId
    {
        public static string Generate(string seed)
        {
            var random = new Random();

            var hash = new Hashids(seed);
            var id = hash.Encode(random.Next(0, int.MaxValue));

            return id;
        }
    }
}
