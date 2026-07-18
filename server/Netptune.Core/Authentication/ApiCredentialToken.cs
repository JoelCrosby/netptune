using System.Security.Cryptography;

namespace Netptune.Core.Authentication;

public sealed record ApiCredentialToken(string Token, string Prefix, byte[] SecretHash)
{
    private const string TokenMarker = "ntp";
    private const int SecretSize = 32;
    private const int EncodedSecretLength = 43;
    private const int PrefixIdLength = 16;

    public static ApiCredentialToken Create(Guid credentialId)
    {
        var secret = RandomNumberGenerator.GetBytes(SecretSize);
        var encodedSecret = Base64UrlEncode(secret);
        var id = credentialId.ToString("N");

        return new ApiCredentialToken(
            $"{TokenMarker}_{id}_{encodedSecret}",
            $"{TokenMarker}_{id[..PrefixIdLength]}",
            SHA256.HashData(secret));
    }

    public static bool TryParse(string value, out Guid credentialId, out byte[] secretHash)
    {
        credentialId = default;
        secretHash = [];

        var parts = value.Split('_', 3, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 3
            || parts[0] != TokenMarker
            || parts[2].Length != EncodedSecretLength
            || !Guid.TryParseExact(parts[1], "N", out credentialId))
        {
            return false;
        }

        try
        {
            var secret = Base64UrlDecode(parts[2]);

            if (secret.Length != SecretSize || Base64UrlEncode(secret) != parts[2])
            {
                credentialId = default;
                return false;
            }

            secretHash = SHA256.HashData(secret);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string Base64UrlEncode(byte[] value)
    {
        return Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static byte[] Base64UrlDecode(string value)
    {
        var base64 = value.Replace('-', '+').Replace('_', '/');
        base64 += new string('=', (4 - base64.Length % 4) % 4);
        return Convert.FromBase64String(base64);
    }
}
