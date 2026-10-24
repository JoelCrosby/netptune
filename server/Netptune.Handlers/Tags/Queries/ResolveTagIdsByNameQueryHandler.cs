using Mediator;

using Netptune.Core.Localization;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Tags.Queries;

public sealed record ResolveTagIdsByNameQuery(IReadOnlyCollection<string?> Names) : IRequest<TagNameResolution>;

public sealed record TagNameResolution
{
    public List<int> Ids { get; init; } = [];

    public List<string> UnknownNames { get; init; } = [];

    public string? Error { get; init; }
}

public sealed class ResolveTagIdsByNameQueryHandler : IRequestHandler<ResolveTagIdsByNameQuery, TagNameResolution>
{
    public const int MaximumNames = 50;
    public static string BlankNameMessage => AppText.ErrorTagsCannotEmpty;

    public static string TooManyNamesMessage => Localized.Format(AppText.ErrorSendAtMostTagNames, MaximumNames);

    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public ResolveTagIdsByNameQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public static string UnknownNamesMessage(IEnumerable<string> unknownNames)
    {
        var namesList = string.Join(", ", unknownNames);

        return Localized.Format(AppText.ErrorTagNamesNotFoundWorkspace, namesList);
    }

    public async ValueTask<TagNameResolution> Handle(ResolveTagIdsByNameQuery request, CancellationToken cancellationToken)
    {
        var hasTooManyNames = request.Names.Count > MaximumNames;

        if (hasTooManyNames)
        {
            return new TagNameResolution { Error = TooManyNamesMessage };
        }

        var hasBlankName = request.Names.Any(string.IsNullOrWhiteSpace);

        if (hasBlankName)
        {
            return new TagNameResolution { Error = BlankNameMessage };
        }

        var requestedNames = request.Names
            .Select(name => name!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (requestedNames.Count == 0)
        {
            return new TagNameResolution();
        }

        var workspaceId = await Identity.GetWorkspaceId();
        var existingTags = await UnitOfWork.Tags.GetViewModelsByNames(workspaceId, requestedNames, cancellationToken);
        var idsByName = existingTags
            .GroupBy(tag => tag.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Id, StringComparer.OrdinalIgnoreCase);
        var unknownNames = requestedNames.Where(name => !idsByName.ContainsKey(name)).ToList();

        if (unknownNames.Count > 0)
        {
            return new TagNameResolution { UnknownNames = unknownNames, Error = UnknownNamesMessage(unknownNames) };
        }

        var ids = requestedNames.Select(name => idsByName[name]).Distinct().ToList();

        return new TagNameResolution { Ids = ids };
    }
}
