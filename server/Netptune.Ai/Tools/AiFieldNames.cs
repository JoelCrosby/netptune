using System.Globalization;

using Netptune.Core.Localization;
using Netptune.Core.Services.Ai;

namespace Netptune.Ai.Tools;

internal static class AiFieldNames
{
    public static string Describe(IEnumerable<AiChangeField> fields)
    {
        var labels = fields.Select(field => Label(field.Name));

        return string.Join(", ", labels);
    }

    public static string DescribePair(List<AiChangeField> fields)
    {
        var isPair = fields.Count == 2;

        if (!isPair)
        {
            return Describe(fields);
        }

        var first = Label(fields[0].Name);
        var second = Label(fields[1].Name);

        return Localized.Format(AppText.ListPair, first, second);
    }

    public static string Label(string name)
    {
        var key = KeyFor(name);

        return AppText.ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? name;
    }

    public static string KeyFor(string name)
    {
        return $"AiFieldName{char.ToUpperInvariant(name[0])}{name[1..]}";
    }
}
