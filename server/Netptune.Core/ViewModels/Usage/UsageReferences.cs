using Netptune.Core.Enums;
using Netptune.Core.Models.Usage;

namespace Netptune.Core.ViewModels.Usage;

public static class UsageReferences
{
    public static List<UsageReferenceGroupViewModel> Build(
        params (UsageReferenceKind Kind, List<UsageReference> Items)[] groups)
    {
        var populatedGroups = groups.Where(group => group.Items.Count > 0);

        return populatedGroups
            .Select(group => new UsageReferenceGroupViewModel
            {
                Kind = group.Kind,
                Items = group.Items.ConvertAll(ToViewModel),
            })
            .ToList();
    }

    private static UsageReferenceViewModel ToViewModel(UsageReference reference)
    {
        return new UsageReferenceViewModel
        {
            Id = reference.Id,
            Name = reference.Name,
            Context = reference.Context,
        };
    }
}
