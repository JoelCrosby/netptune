using Netptune.Core.Relations;
using Netptune.Core.ViewModels.RelationTypes;

namespace Netptune.Ai.Tools;

internal sealed record AiRelationTypeMatch(RelationTypeViewModel RelationType, bool IsInverse);

internal static class AiRelationTypeLookup
{
    public const string EntityType = "relationType";

    public static AiRelationTypeMatch? Match(IReadOnlyList<RelationTypeViewModel> relationTypes, string name)
    {
        var wanted = Normalize(name);
        var isNamed = relationTypes.FirstOrDefault(relationType =>
            Normalize(relationType.Name) == wanted || Normalize(relationType.Key) == wanted);

        if (isNamed is not null)
        {
            return new AiRelationTypeMatch(isNamed, false);
        }

        var inverse = relationTypes.FirstOrDefault(relationType => Normalize(relationType.InverseName) == wanted);

        if (inverse is null)
        {
            return null;
        }

        var isDirected = !RelationTypeRules.IsSymmetric(inverse.Category);

        return new AiRelationTypeMatch(inverse, isDirected);
    }

    public static bool Exists(IReadOnlyList<RelationTypeViewModel> relationTypes, string name)
    {
        var wanted = Normalize(name);

        return relationTypes.Any(relationType =>
            Normalize(relationType.Name) == wanted
            || Normalize(relationType.Key) == wanted
            || Normalize(relationType.InverseName) == wanted);
    }

    public static string Describe(IReadOnlyList<RelationTypeViewModel> relationTypes)
    {
        if (relationTypes.Count == 0)
        {
            return "This workspace has no relation types. Propose one with propose_create_relation_type first.";
        }

        var described = relationTypes.Select(Summarize);

        return $"The relation types in this workspace are: {string.Join(", ", described)}.";
    }

    private static string Summarize(RelationTypeViewModel relationType)
    {
        return $"{relationType.Id} “{relationType.Name}” (inverse “{relationType.InverseName}”)";
    }

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var letters = value.Where(char.IsLetterOrDigit).ToArray();

        return new string(letters).ToLowerInvariant();
    }
}
