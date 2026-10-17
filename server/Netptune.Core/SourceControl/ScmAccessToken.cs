namespace Netptune.Core.SourceControl;

public sealed record ScmAccessToken(string Token, DateTimeOffset ExpiresAt);
