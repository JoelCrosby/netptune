using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Netptune.SourceControl.Providers.GitHub;

internal sealed class GitHubAppTokenMinter : IDisposable
{
    // GitHub rejects app JWTs that live longer than ten minutes, and recommends backdating iat a
    // minute to absorb clock drift between Netptune and GitHub.
    private static readonly TimeSpan Backdate = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(9);

    private readonly GitHubAppOptions Options;
    private readonly TimeProvider Clock;
    private readonly RSA Rsa;
    private readonly SigningCredentials Credentials;

    public GitHubAppTokenMinter(IOptions<GitHubAppOptions> options, TimeProvider clock)
    {
        Options = options.Value;
        Clock = clock;
        Rsa = RSA.Create();

        Rsa.ImportFromPem(Options.PrivateKeyPem);

        // IdentityModel caches signature providers by public key across key instances, which would
        // hand a later minter a provider wrapping this RSA after it has been disposed.
        var signingKey = new RsaSecurityKey(Rsa)
        {
            CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false },
        };

        Credentials = new SigningCredentials(signingKey, SecurityAlgorithms.RsaSha256);
    }

    public string CreateAppJwt()
    {
        var now = Clock.GetUtcNow();
        var issuedAt = (now - Backdate).UtcDateTime;

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = Options.AppId,
            IssuedAt = issuedAt,
            NotBefore = issuedAt,
            Expires = (now + Lifetime).UtcDateTime,
            SigningCredentials = Credentials,
        };

        var handler = new JwtSecurityTokenHandler { SetDefaultTimesOnTokenCreation = false };

        return handler.CreateEncodedJwt(descriptor);
    }

    public void Dispose()
    {
        Rsa.Dispose();
    }
}
