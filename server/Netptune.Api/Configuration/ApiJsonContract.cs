using System.Reflection;
using System.Text.Json.Serialization.Metadata;

using Netptune.Core.Models.Reporting;
using Netptune.Core.ViewModels.Boards;
using Netptune.Core.ViewModels.Comments;
using Netptune.Core.ViewModels.Pages;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Core.ViewModels.Relations;
using Netptune.Core.ViewModels.Search;

namespace Netptune.Api.Configuration;

public static class ApiJsonContract
{
    private const string TaskSearchResultType = "task";

    private static readonly (Type Type, string[] Members)[] HiddenMembers =
    [
        (typeof(TaskViewModel), [nameof(TaskViewModel.Id), nameof(TaskViewModel.WorkspaceId)]),
        (typeof(BoardViewTask), [nameof(BoardViewTask.Id)]),
        (typeof(RelatedTaskViewModel), [nameof(RelatedTaskViewModel.Id)]),
        (typeof(PageLinkedTaskViewModel), [nameof(PageLinkedTaskViewModel.Id)]),
        (typeof(CommentViewModel), [nameof(CommentViewModel.EntityId)]),
        (typeof(SprintBurndownReport), [nameof(SprintBurndownReport.AddedTaskIds)]),
    ];

    public static IServiceCollection AddApiJsonContract(this IServiceCollection services)
    {
        return services.ConfigureHttpJsonOptions(options =>
        {
            var resolver = options.SerializerOptions.TypeInfoResolver ?? new DefaultJsonTypeInfoResolver();

            options.SerializerOptions.TypeInfoResolver = resolver.WithAddedModifier(HideTaskDatabaseIds);
        });
    }

    private static void HideTaskDatabaseIds(JsonTypeInfo typeInfo)
    {
        var isObject = typeInfo.Kind == JsonTypeInfoKind.Object;

        if (!isObject)
        {
            return;
        }

        var hidden = HiddenMembers
            .Where(entry => typeInfo.Type.IsAssignableTo(entry.Type))
            .SelectMany(entry => entry.Members)
            .ToHashSet(StringComparer.Ordinal);

        var removed = typeInfo.Properties.Where(property => hidden.Contains(MemberName(property))).ToList();

        foreach (var property in removed)
        {
            typeInfo.Properties.Remove(property);
        }

        var isSearchResult = typeInfo.Type.IsAssignableTo(typeof(SearchResultViewModel));

        if (!isSearchResult)
        {
            return;
        }

        var resultId = typeInfo.Properties.FirstOrDefault(property =>
            MemberName(property) == nameof(SearchResultViewModel.Id));

        if (resultId is not null)
        {
            resultId.ShouldSerialize = (result, _) => ((SearchResultViewModel)result).Type != TaskSearchResultType;
        }
    }

    private static string MemberName(JsonPropertyInfo property)
    {
        var member = property.AttributeProvider as MemberInfo;

        return member?.Name ?? property.Name;
    }
}
