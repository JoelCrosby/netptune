namespace Netptune.Events.Configuration;

public sealed class EventConsumerOptions
{
    public const string SectionName = "Events:Consumer";

    public List<string> FilterSubjects { get; set; } = [];

    public string DurableName { get; set; } = string.Empty;

    internal void Validate()
    {
        if (FilterSubjects.Count == 0 || FilterSubjects.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException($"{SectionName}:{nameof(FilterSubjects)} must contain at least one non-empty subject.");
        }

        if (string.IsNullOrWhiteSpace(DurableName))
        {
            throw new InvalidOperationException($"{SectionName}:{nameof(DurableName)} cannot be empty.");
        }
    }
}
