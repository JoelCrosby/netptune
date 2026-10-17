using System.Security.Cryptography;

namespace Netptune.Core.SourceControl.Events;

public static class ScmEventIds
{
    public static Guid Create(SourceControlProvider provider, string deliveryId, int index)
    {
        var name = $"{provider}:{deliveryId}:{index}";
        var hash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(name));
        var bytes = hash[..16];

        // RFC 9562 version 8 (custom) with the RFC variant bits, so the id is a valid UUID that is
        // stable for every redelivery of the same webhook.
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x80);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);

        return new Guid(bytes, bigEndian: true);
    }
}
