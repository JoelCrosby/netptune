using System.Globalization;
using System.Security.Cryptography;

using Microsoft.Extensions.Logging;

using Netptune.Core.Localization;
using Netptune.Core.SourceControl;
using Netptune.Core.SourceControl.Checks;
using Netptune.SourceControl.Providers.AzureDevOps.Dtos;

namespace Netptune.SourceControl.Providers.AzureDevOps;

internal sealed record AzureDevOpsCheckRun
{
    public required IReadOnlyList<ScmConnectionCheck> Checks { get; init; }

    public string? InstanceId { get; init; }
}

internal sealed class AzureDevOpsConnectionChecker : IScmConnectionChecker
{
    public const string OrganisationKey = "organisation";
    public const string TokenKey = "token";
    public const string AdministratorKey = "administrator";
    public const string ProjectsKey = "projects";
    public const string RepositoriesKey = "repositories";
    public const string WorkItemsKey = "work-items";
    public const string BuildsKey = "builds";

    public const string OrganisationFact = "organisation";
    public const string UserFact = "user";
    public const string RepositoriesFact = "repositories";
    public const string ProjectsFact = "projects";
    public const string ReasonFact = "reason";

    public const string DeniedReason = "denied";
    public const string UnavailableReason = "unavailable";
    public const string UnansweredReason = "unanswered";
    public const string NoProjectsReason = "noProjects";
    public const string UnreadableReason = "unreadable";

    private static readonly string[] CheckOrder =
    [
        OrganisationKey,
        TokenKey,
        AdministratorKey,
        ProjectsKey,
        RepositoriesKey,
        WorkItemsKey,
        BuildsKey,
    ];

    private static readonly HashSet<string> OptionalChecks = [WorkItemsKey, BuildsKey];

    private const string ConnectionDataPath = "_apis/connectionData";
    // Projects - List needs vso.project, the scope behind Project and Team (Read).
    private const string ProjectsPath = "_apis/projects?$top=1&api-version=7.1";
    private const string RepositoriesPath = "_apis/git/repositories?api-version=7.1";

    // Fields - Get at organisation level needs only vso.work, the scope behind Work Items (Read).
    private const string WorkItemsProbePath = "_apis/wit/fields/System.Title?api-version=7.1";

    // Definitions - List is project-scoped and needs only vso.build, the scope behind Build (Read).
    private const string BuildsProbePathFormat = "{0}/_apis/build/definitions?$top=1&api-version=7.1";

    // The Collection security namespace and its GENERIC_WRITE bit, which only Project Collection
    // Administrators hold. The namespace token's form isn't settled, so both are tried.
    private const string CollectionAdministratorPathFormat =
        "_apis/permissions/3e65f728-f8bc-4ecd-8764-7e378b19bfa7/2?tokens={0}&api-version=7.1";

    private static readonly string[] AdministratorNamespaceTokens = ["NAMESPACE:", "NAMESPACE"];

    private readonly AzureDevOpsApi Api;
    private readonly IScmCredentialProtector CredentialProtector;
    private readonly ILogger<AzureDevOpsConnectionChecker> Logger;

    public AzureDevOpsConnectionChecker(
        AzureDevOpsApi api,
        IScmCredentialProtector credentialProtector,
        ILogger<AzureDevOpsConnectionChecker> logger)
    {
        Api = api;
        CredentialProtector = credentialProtector;
        Logger = logger;
    }

    public async ValueTask<IReadOnlyList<ScmConnectionCheck>> Check(
        ScmConnection connection,
        CancellationToken cancellationToken)
    {
        if (connection.ProtectedCredential is null)
        {
            var tokenMissing = Create(TokenKey, ScmConnectionCheckStatus.Failed, OrganisationFact, connection.AccountLogin);

            return CompleteWithSkipped([Skipped(OrganisationKey), tokenMissing]);
        }

        var token = ReadStoredToken(connection.ProtectedCredential);

        if (token is null)
        {
            var tokenUnreadable = Create(TokenKey, ScmConnectionCheckStatus.Failed, ReasonFact, UnreadableReason);

            return CompleteWithSkipped([Skipped(OrganisationKey), tokenUnreadable]);
        }

        var credential = new AzureDevOpsCredential
        {
            Organisation = connection.AccountLogin,
            Token = token,
        };

        var run = await Run(credential, cancellationToken);

        return run.Checks;
    }

    public async Task<AzureDevOpsCheckRun> Run(AzureDevOpsCredential credential, CancellationToken cancellationToken)
    {
        var organisation = credential.Organisation;
        var connectionRequest = credential.CreateRequest(ConnectionDataPath);
        var connectionData = await Api.Get<AzureDevOpsConnectionDataDto>(connectionRequest, cancellationToken);

        if (connectionData.Kind == AzureDevOpsResponseKind.NotFound)
        {
            var notFound = Create(OrganisationKey, ScmConnectionCheckStatus.Failed, OrganisationFact, organisation);

            return new AzureDevOpsCheckRun { Checks = CompleteWithSkipped([notFound]) };
        }

        var organisationFound = Create(OrganisationKey, ScmConnectionCheckStatus.Passed, OrganisationFact, organisation);
        var instanceId = connectionData.Payload?.InstanceId;
        var hasInstance = connectionData.IsSuccess && !string.IsNullOrWhiteSpace(instanceId);

        if (!hasInstance)
        {
            var rejected = Create(TokenKey, ScmConnectionCheckStatus.Failed, OrganisationFact, organisation);

            return new AzureDevOpsCheckRun { Checks = CompleteWithSkipped([organisationFound, rejected]) };
        }

        var tokenAccepted = CreateTokenAccepted(connectionData.Payload!);
        var administrator = await CheckAdministrator(credential, cancellationToken);
        var projects = await CheckProjects(credential, cancellationToken);
        var repositories = await CheckRepositories(credential, cancellationToken);
        var workItems = await Probe(WorkItemsKey, credential, WorkItemsProbePath, cancellationToken);
        var builds = await CheckBuilds(credential, projects, cancellationToken);

        return new AzureDevOpsCheckRun
        {
            Checks = [organisationFound, tokenAccepted, administrator, projects.Check, repositories, workItems, builds],
            InstanceId = instanceId,
        };
    }

    public ScmConnectionCheckDescription Describe(ScmConnectionCheck check)
    {
        var title = DescribeTitle(check.Key);
        var detail = DescribeDetail(check);
        var isRequired = !OptionalChecks.Contains(check.Key);

        return new ScmConnectionCheckDescription(title, detail, isRequired);
    }

    private string? ReadStoredToken(byte[] protectedCredential)
    {
        try
        {
            return CredentialProtector.Unprotect(protectedCredential);
        }
        catch (CryptographicException exception)
        {
            Logger.LogWarning(exception, "The stored Azure DevOps token could not be unprotected");

            return null;
        }
    }

    private static ScmConnectionCheck CreateTokenAccepted(AzureDevOpsConnectionDataDto connectionData)
    {
        var user = connectionData.AuthenticatedUser;
        var displayName = user?.CustomDisplayName ?? user?.ProviderDisplayName;

        if (string.IsNullOrWhiteSpace(displayName))
        {
            return Create(TokenKey, ScmConnectionCheckStatus.Passed);
        }

        return Create(TokenKey, ScmConnectionCheckStatus.Passed, UserFact, displayName);
    }

    // A plain organisation member could otherwise attach the organisation to a workspace and show every
    // workspace member what they can see. A wrong namespace token answers false rather than failing, so
    // anything but true moves on to the other form.
    private async Task<ScmConnectionCheck> CheckAdministrator(
        AzureDevOpsCredential credential,
        CancellationToken cancellationToken)
    {
        var hasAnyAnswer = false;

        foreach (var namespaceToken in AdministratorNamespaceTokens)
        {
            var answer = await ReadAdministratorAnswer(credential, namespaceToken, cancellationToken);

            if (answer is true)
            {
                Logger.LogInformation(
                    "Azure DevOps confirmed an administrator of {Organisation} with the {NamespaceToken} namespace token",
                    credential.Organisation,
                    namespaceToken);

                return Create(AdministratorKey, ScmConnectionCheckStatus.Passed);
            }

            hasAnyAnswer |= answer is not null;
        }

        Logger.LogInformation(
            "Azure DevOps did not confirm an administrator of {Organisation} with either namespace token; answered: {HasAnswer}",
            credential.Organisation,
            hasAnyAnswer);

        var reason = hasAnyAnswer ? DeniedReason : UnavailableReason;
        var facts = new Dictionary<string, string>
        {
            [OrganisationFact] = credential.Organisation,
            [ReasonFact] = reason,
        };

        return new ScmConnectionCheck
        {
            Key = AdministratorKey,
            Status = ScmConnectionCheckStatus.Failed,
            Facts = facts,
        };
    }

    private async Task<bool?> ReadAdministratorAnswer(
        AzureDevOpsCredential credential,
        string namespaceToken,
        CancellationToken cancellationToken)
    {
        var path = string.Format(CultureInfo.InvariantCulture, CollectionAdministratorPathFormat, namespaceToken);
        var request = credential.CreateRequest(path);

        try
        {
            var response = await Api.Get<AzureDevOpsListDto<bool>>(request, cancellationToken);
            var answers = response.Payload?.Value ?? [];
            var hasSingleAnswer = response.IsSuccess && answers.Count == 1;

            return hasSingleAnswer ? answers[0] : null;
        }
        catch (ScmProviderException)
        {
            return null;
        }
    }

    private async Task<ProjectsCheck> CheckProjects(
        AzureDevOpsCredential credential,
        CancellationToken cancellationToken)
    {
        var request = credential.CreateRequest(ProjectsPath);

        try
        {
            var response = await Api.Get<AzureDevOpsListDto<AzureDevOpsProjectDto>>(request, cancellationToken);

            if (!response.IsSuccess)
            {
                var denied = Create(ProjectsKey, ScmConnectionCheckStatus.Failed, ReasonFact, DeniedReason);

                return new ProjectsCheck(denied, null);
            }

            var firstProjectId = response.Payload!.Value.Select(project => project.Id).FirstOrDefault();
            var passed = Create(ProjectsKey, ScmConnectionCheckStatus.Passed);

            return new ProjectsCheck(passed, firstProjectId);
        }
        catch (ScmProviderException)
        {
            var unanswered = Create(ProjectsKey, ScmConnectionCheckStatus.Failed, ReasonFact, UnansweredReason);

            return new ProjectsCheck(unanswered, null);
        }
    }

    private async Task<ScmConnectionCheck> CheckRepositories(
        AzureDevOpsCredential credential,
        CancellationToken cancellationToken)
    {
        var request = credential.CreateRequest(RepositoriesPath);

        try
        {
            var response = await Api.Get<AzureDevOpsListDto<AzureDevOpsRepositoryDto>>(request, cancellationToken);

            if (!response.IsSuccess)
            {
                return Create(RepositoriesKey, ScmConnectionCheckStatus.Failed, ReasonFact, DeniedReason);
            }

            var repositories = response.Payload!.Value;
            var enabledCount = repositories.Count(repository => !repository.IsDisabled);
            var projectCount = repositories.Select(repository => repository.Project.Id).Distinct().Count();
            var facts = new Dictionary<string, string>
            {
                [RepositoriesFact] = enabledCount.ToString(CultureInfo.InvariantCulture),
                [ProjectsFact] = projectCount.ToString(CultureInfo.InvariantCulture),
            };

            return new ScmConnectionCheck
            {
                Key = RepositoriesKey,
                Status = ScmConnectionCheckStatus.Passed,
                Facts = facts,
            };
        }
        catch (ScmProviderException)
        {
            return Create(RepositoriesKey, ScmConnectionCheckStatus.Failed, ReasonFact, UnansweredReason);
        }
    }

    private async Task<ScmConnectionCheck> CheckBuilds(
        AzureDevOpsCredential credential,
        ProjectsCheck projects,
        CancellationToken cancellationToken)
    {
        var projectsPassed = projects.Check.Status == ScmConnectionCheckStatus.Passed;

        if (!projectsPassed)
        {
            return Skipped(BuildsKey);
        }

        if (projects.FirstProjectId is null)
        {
            return Create(BuildsKey, ScmConnectionCheckStatus.Skipped, ReasonFact, NoProjectsReason);
        }

        var projectSegment = Uri.EscapeDataString(projects.FirstProjectId);
        var path = string.Format(CultureInfo.InvariantCulture, BuildsProbePathFormat, projectSegment);

        return await Probe(BuildsKey, credential, path, cancellationToken);
    }

    private async Task<ScmConnectionCheck> Probe(
        string key,
        AzureDevOpsCredential credential,
        string path,
        CancellationToken cancellationToken)
    {
        var request = credential.CreateRequest(path);

        try
        {
            var response = await Api.Get<AzureDevOpsProbeDto>(request, cancellationToken);

            if (response.IsSuccess)
            {
                return Create(key, ScmConnectionCheckStatus.Passed);
            }

            return Create(key, ScmConnectionCheckStatus.Warning, ReasonFact, DeniedReason);
        }
        catch (ScmProviderException)
        {
            return Create(key, ScmConnectionCheckStatus.Warning, ReasonFact, UnansweredReason);
        }
    }

    private static IReadOnlyList<ScmConnectionCheck> CompleteWithSkipped(List<ScmConnectionCheck> ran)
    {
        var ranKeys = ran.Select(check => check.Key).ToHashSet();
        var skipped = CheckOrder.Where(key => !ranKeys.Contains(key)).Select(Skipped);

        return ran.Concat(skipped).ToList();
    }

    private static ScmConnectionCheck Skipped(string key)
    {
        return Create(key, ScmConnectionCheckStatus.Skipped);
    }

    private static ScmConnectionCheck Create(string key, ScmConnectionCheckStatus status)
    {
        return new ScmConnectionCheck { Key = key, Status = status };
    }

    private static ScmConnectionCheck Create(string key, ScmConnectionCheckStatus status, string fact, string value)
    {
        return new ScmConnectionCheck
        {
            Key = key,
            Status = status,
            Facts = new Dictionary<string, string> { [fact] = value },
        };
    }

    private static string DescribeTitle(string key)
    {
        return key switch
        {
            OrganisationKey => AppText.AzureDevOpsCheckOrganisationTitle,
            TokenKey => AppText.AzureDevOpsCheckTokenTitle,
            AdministratorKey => AppText.AzureDevOpsCheckAdministratorTitle,
            ProjectsKey => AppText.AzureDevOpsCheckProjectsTitle,
            RepositoriesKey => AppText.AzureDevOpsCheckRepositoriesTitle,
            WorkItemsKey => AppText.AzureDevOpsCheckWorkItemsTitle,
            BuildsKey => AppText.AzureDevOpsCheckBuildsTitle,
            _ => key,
        };
    }

    private static string DescribeDetail(ScmConnectionCheck check)
    {
        var reason = check.Facts.GetValueOrDefault(ReasonFact);

        if (check.Status == ScmConnectionCheckStatus.Skipped)
        {
            return reason == NoProjectsReason ? AppText.AzureDevOpsCheckNoProjects : AppText.AzureDevOpsCheckSkipped;
        }

        var hasPassed = check.Status == ScmConnectionCheckStatus.Passed;

        if (!hasPassed && reason == UnansweredReason)
        {
            return AppText.AzureDevOpsCheckUnanswered;
        }

        var organisation = check.Facts.GetValueOrDefault(OrganisationFact) ?? string.Empty;

        return check.Key switch
        {
            OrganisationKey when hasPassed => Localized.Format(AppText.AzureDevOpsCheckOrganisationPassed, organisation),
            OrganisationKey => Localized.Format(AppText.AzureDevOpsCheckOrganisationFailed, organisation),
            TokenKey when hasPassed => DescribeTokenAccepted(check),
            TokenKey when reason == UnreadableReason => AppText.AzureDevOpsCheckTokenUnreadable,
            TokenKey => Localized.Format(AppText.AzureDevOpsCheckTokenFailed, organisation),
            AdministratorKey when hasPassed => AppText.AzureDevOpsCheckAdministratorPassed,
            AdministratorKey => DescribeAdministratorFailed(reason, organisation),
            ProjectsKey when hasPassed => AppText.AzureDevOpsCheckProjectsPassed,
            ProjectsKey => AppText.AzureDevOpsCheckProjectsFailed,
            RepositoriesKey when hasPassed => DescribeRepositories(check),
            RepositoriesKey => AppText.AzureDevOpsCheckRepositoriesFailed,
            WorkItemsKey when hasPassed => AppText.AzureDevOpsCheckWorkItemsPassed,
            WorkItemsKey => AppText.AzureDevOpsCheckWorkItemsWarning,
            BuildsKey when hasPassed => AppText.AzureDevOpsCheckBuildsPassed,
            BuildsKey => AppText.AzureDevOpsCheckBuildsWarning,
            _ => string.Empty,
        };
    }

    private static string DescribeTokenAccepted(ScmConnectionCheck check)
    {
        var user = check.Facts.GetValueOrDefault(UserFact);

        if (user is null)
        {
            return AppText.AzureDevOpsCheckTokenPassedWithoutName;
        }

        return Localized.Format(AppText.AzureDevOpsCheckTokenPassed, user);
    }

    private static string DescribeAdministratorFailed(string? reason, string organisation)
    {
        var isUnavailable = reason == UnavailableReason;
        var format = isUnavailable
            ? AppText.AzureDevOpsCheckAdministratorUnavailable
            : AppText.AzureDevOpsCheckAdministratorFailed;

        return Localized.Format(format, organisation);
    }

    private static string DescribeRepositories(ScmConnectionCheck check)
    {
        var repositoryCount = ReadCount(check, RepositoriesFact);
        var projectCount = ReadCount(check, ProjectsFact);
        var repositories = repositoryCount == 1
            ? AppText.AzureDevOpsRepositoryCountOne
            : Localized.Format(AppText.AzureDevOpsRepositoryCountOther, repositoryCount);
        var projects = projectCount == 1
            ? AppText.AzureDevOpsProjectCountOne
            : Localized.Format(AppText.AzureDevOpsProjectCountOther, projectCount);

        return Localized.Format(AppText.AzureDevOpsCheckRepositoriesPassed, repositories, projects);
    }

    private static int ReadCount(ScmConnectionCheck check, string fact)
    {
        var value = check.Facts.GetValueOrDefault(fact);
        var isCount = int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var count);

        return isCount ? count : 0;
    }

    private sealed record ProjectsCheck(ScmConnectionCheck Check, string? FirstProjectId);
}
