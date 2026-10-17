using Netptune.Core.Entities;

namespace Netptune.Core.SourceControl;

// Matches a link's author to a workspace member by provider account id first, then username, then
// commit email. An author that matches nothing stays unattributed.
public sealed class ScmAuthorMatcher
{
    private readonly Dictionary<(SourceControlProvider, string), AppUser> ByExternalId = [];
    private readonly Dictionary<(SourceControlProvider, string), AppUser> ByUsername = [];
    private readonly Dictionary<string, AppUser> ByEmail = new(StringComparer.Ordinal);

    public ScmAuthorMatcher(IEnumerable<UserSourceControlIdentity> identities, IEnumerable<AppUser> membersByEmail)
    {
        foreach (var identity in identities)
        {
            if (identity.ExternalId is not null)
            {
                ByExternalId[(identity.Provider, identity.ExternalId)] = identity.User;
            }

            if (identity.NormalizedUsername is not null)
            {
                ByUsername[(identity.Provider, identity.NormalizedUsername)] = identity.User;
            }
        }

        foreach (var member in membersByEmail)
        {
            if (member.NormalizedEmail is not null)
            {
                ByEmail[member.NormalizedEmail] = member;
            }
        }
    }

    public static string NormalizeEmail(string email)
    {
        return email.Trim().ToUpperInvariant();
    }

    public AppUser? Match(SourceControlProvider provider, string? externalId, string? username, string? email)
    {
        var byExternalId = externalId is null ? null : ByExternalId.GetValueOrDefault((provider, externalId));

        if (byExternalId is not null)
        {
            return byExternalId;
        }

        var normalizedUsername = username is null ? null : UserSourceControlIdentity.Normalize(username);
        var byUsername = normalizedUsername is null
            ? null
            : ByUsername.GetValueOrDefault((provider, normalizedUsername));

        if (byUsername is not null)
        {
            return byUsername;
        }

        return email is null ? null : ByEmail.GetValueOrDefault(NormalizeEmail(email));
    }
}
