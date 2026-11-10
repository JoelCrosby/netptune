using System.Globalization;
using System.Net;

using Netptune.Core.Localization;
using Netptune.Core.Responses.Common;
using Netptune.Core.SourceControl;
using Netptune.Core.SourceControl.Connect;
using Netptune.SourceControl.Providers.AzureDevOps.Dtos;

namespace Netptune.SourceControl.Providers.AzureDevOps;

internal sealed class AzureDevOpsConnector : ISourceControlConnector
{
    public const string OrganisationParameter = "organisation";
    public const string TokenParameter = "token";
    public const string ExpiresOnParameter = "expiresOn";

    public const SourceControlCapabilities GrantedCapabilities = SourceControlCapabilities.ListRepositories;

    private const string ExpiresOnFormat = "yyyy-MM-dd";
    private const string ConnectionDataPath = "_apis/connectionData";

    // The Collection security namespace, its GENERIC_WRITE bit, and the token for the whole organisation:
    // only Project Collection Administrators hold it.
    private const string CollectionAdministratorPath =
        "_apis/permissions/3e65f728-f8bc-4ecd-8764-7e378b19bfa7/2?tokens=NAMESPACE:&api-version=7.1";

    private static readonly TimeOnly EndOfDay = new(23, 59, 59);

    private readonly AzureDevOpsApi Api;
    private readonly IScmCredentialProtector CredentialProtector;
    private readonly TimeProvider Clock;

    public AzureDevOpsConnector(AzureDevOpsApi api, IScmCredentialProtector credentialProtector, TimeProvider clock)
    {
        Api = api;
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

        var instance = await ReadInstance(credential, cancellationToken);

        if (!instance.IsSuccess)
        {
            return ClientResponse<ScmConnectOutcome>.Failed(instance.Message);
        }

        var administrator = await CheckAdministrator(credential, cancellationToken);

        if (administrator == AdministratorCheck.Denied)
        {
            return ClientResponse<ScmConnectOutcome>.Failed(AppText.ErrorAzureDevOpsNotAdministrator);
        }

        if (administrator == AdministratorCheck.Unavailable)
        {
            return ClientResponse<ScmConnectOutcome>.Failed(AppText.ErrorAzureDevOpsAdminCheckUnavailable);
        }

        var instanceId = instance.Payload!.InstanceId!;

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

        return ClientResponse<ScmConnectOutcome>.Success(ScmConnectOutcome.Connected(result));
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

    private async Task<ClientResponse<AzureDevOpsConnectionDataDto>> ReadInstance(
        AzureDevOpsCredential credential,
        CancellationToken cancellationToken)
    {
        var request = credential.CreateRequest(ConnectionDataPath);
        var response = await Api.Get<AzureDevOpsConnectionDataDto>(request, cancellationToken);

        if (response.Kind == AzureDevOpsResponseKind.NotFound)
        {
            var message = Localized.Format(AppText.ErrorAzureDevOpsOrganisationNotFound, credential.Organisation);

            return ClientResponse<AzureDevOpsConnectionDataDto>.Failed(message);
        }

        var instanceId = response.Payload?.InstanceId;
        var hasInstance = response.IsSuccess && !string.IsNullOrWhiteSpace(instanceId);

        if (!hasInstance)
        {
            return ClientResponse<AzureDevOpsConnectionDataDto>.Failed(AppText.ErrorAzureDevOpsTokenRejected);
        }

        return ClientResponse<AzureDevOpsConnectionDataDto>.Success(response.Payload!);
    }

    // A plain organisation member could otherwise attach the organisation to a workspace and show every
    // workspace member what they can see. When the token can't read the permission, connecting is refused.
    private async Task<AdministratorCheck> CheckAdministrator(
        AzureDevOpsCredential credential,
        CancellationToken cancellationToken)
    {
        var request = credential.CreateRequest(CollectionAdministratorPath);
        var response = await Api.Get<AzureDevOpsListDto<bool>>(request, cancellationToken);
        var answers = response.Payload?.Value ?? [];
        var hasSingleAnswer = response.IsSuccess && answers.Count == 1;

        if (!hasSingleAnswer)
        {
            return AdministratorCheck.Unavailable;
        }

        return answers[0] ? AdministratorCheck.Allowed : AdministratorCheck.Denied;
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

    private enum AdministratorCheck
    {
        Allowed = 0,
        Denied = 1,
        Unavailable = 2,
    }
}
