using System.Globalization;
using System.Net;

using Netptune.Core.Localization;
using Netptune.Core.Responses.Common;
using Netptune.Core.SourceControl;
using Netptune.Core.SourceControl.Checks;
using Netptune.Core.SourceControl.Connect;

namespace Netptune.SourceControl.Providers.AzureDevOps;

internal sealed class AzureDevOpsConnector : ISourceControlConnector
{
    public const string OrganisationParameter = "organisation";
    public const string TokenParameter = "token";
    public const string ExpiresOnParameter = "expiresOn";

    public const SourceControlCapabilities GrantedCapabilities = SourceControlCapabilities.ListRepositories;

    private const string ExpiresOnFormat = "yyyy-MM-dd";

    private static readonly TimeOnly EndOfDay = new(23, 59, 59);

    private readonly AzureDevOpsConnectionChecker Checker;
    private readonly IScmCredentialProtector CredentialProtector;
    private readonly TimeProvider Clock;

    public AzureDevOpsConnector(
        AzureDevOpsConnectionChecker checker,
        IScmCredentialProtector credentialProtector,
        TimeProvider clock)
    {
        Checker = checker;
        CredentialProtector = credentialProtector;
        Clock = clock;
    }

    public ClientResponse<ScmConnectStart> BeginConnect(ScmConnectRequest request)
    {
        var message = Localized.Format(AppText.ErrorProviderConnectsWithToken, SourceControlProvider.AzureDevOps);

        return ClientResponse<ScmConnectStart>.Failed(message);
    }

    public async ValueTask<ClientResponse<ScmConnectOutcome>> CompleteConnect(
        ScmConnectCallback callback,
        CancellationToken cancellationToken)
    {
        var parameters = callback.Parameters;
        var organisation = AzureDevOpsOrganisation.Parse(parameters.GetValueOrDefault(OrganisationParameter));

        if (organisation is null)
        {
            return ClientResponse<ScmConnectOutcome>.Failed(AppText.ErrorAzureDevOpsOrganisationNotRecognised);
        }

        var token = parameters.GetValueOrDefault(TokenParameter)?.Trim();

        if (string.IsNullOrEmpty(token))
        {
            return ClientResponse<ScmConnectOutcome>.Failed(AppText.ErrorAzureDevOpsTokenRequired);
        }

        var expiry = ReadExpiry(parameters.GetValueOrDefault(ExpiresOnParameter));

        if (!expiry.IsValid)
        {
            return ClientResponse<ScmConnectOutcome>.Failed(AppText.ErrorAzureDevOpsExpiryInvalid);
        }

        var credential = new AzureDevOpsCredential
        {
            Organisation = organisation,
            Token = token,
        };

        var run = await Checker.Run(credential, cancellationToken);
        var hasFailure = ScmConnectionChecks.HasFailure(run.Checks);

        if (hasFailure || run.InstanceId is null)
        {
            var refused = ScmConnectOutcome.Checked(run.Checks, null);

            return ClientResponse<ScmConnectOutcome>.Success(refused);
        }

        var instanceId = run.InstanceId;

        var result = new ScmConnectResult
        {
            ExternalAccountId = instanceId,
            Account = new ScmAccount
            {
                ExternalId = instanceId,
                Login = organisation,
                AccountType = ScmAccountType.Organisation,
            },
            CredentialKind = ScmCredentialKind.PersonalAccessToken,
            ProtectedCredential = CredentialProtector.Protect(token),
            CredentialHint = CredentialProtector.CreateHint(token),
            CredentialExpiresAt = expiry.ExpiresAt,
            GrantedCapabilities = GrantedCapabilities,
        };

        var connected = ScmConnectOutcome.Checked(run.Checks, result);

        return ClientResponse<ScmConnectOutcome>.Success(connected);
    }

    public ValueTask<ScmAccessToken> GetAccessToken(ScmConnection connection, CancellationToken cancellationToken)
    {
        if (connection.ProtectedCredential is null)
        {
            throw new ScmProviderException(
                SourceControlProvider.AzureDevOps,
                (int)HttpStatusCode.Unauthorized,
                "The connection has no stored personal access token.");
        }

        var token = CredentialProtector.Unprotect(connection.ProtectedCredential);
        var expiresAt = ToExpiresAt(connection.CredentialExpiresAt);

        return ValueTask.FromResult(new ScmAccessToken(token, expiresAt));
    }

    public ValueTask<SourceControlCapabilities> GetGrantedCapabilities(
        ScmConnection connection,
        CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(GrantedCapabilities);
    }

    // Netptune can't revoke a personal access token; its owner deletes it in Azure DevOps.
    public ValueTask Disconnect(ScmConnection connection, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    private ExpiryInput ReadExpiry(string? expiresOn)
    {
        if (string.IsNullOrWhiteSpace(expiresOn))
        {
            return new ExpiryInput(true, null);
        }

        var isDate = DateOnly.TryParseExact(
            expiresOn.Trim(),
            ExpiresOnFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var date);

        var todayUtc = DateOnly.FromDateTime(Clock.GetUtcNow().UtcDateTime);
        var earliestLocalToday = todayUtc.AddDays(-1);
        var hasNotPassed = isDate && date >= earliestLocalToday;

        if (!hasNotPassed)
        {
            return new ExpiryInput(false, null);
        }

        var expiresAt = date.ToDateTime(EndOfDay, DateTimeKind.Utc);

        return new ExpiryInput(true, expiresAt);
    }

    private static DateTimeOffset ToExpiresAt(DateTime? credentialExpiresAt)
    {
        if (credentialExpiresAt is not { } expiresAt)
        {
            return DateTimeOffset.MaxValue;
        }

        var utcExpiresAt = DateTime.SpecifyKind(expiresAt, DateTimeKind.Utc);

        return new DateTimeOffset(utcExpiresAt);
    }

    private sealed record ExpiryInput(bool IsValid, DateTime? ExpiresAt);

}
