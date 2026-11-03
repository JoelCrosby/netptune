using Netptune.Core.Localization;

namespace Netptune.Core.Models.ProjectTasks;

public static class ProjectTaskDescription
{
    public const int MaxLength = 32768;

    public static string TooLongMessage => Localized.Format(AppText.ErrorTaskDescriptionTooLong, MaxLength);

    public static bool IsValid(string? description)
    {
        return description is null || description.Length <= MaxLength;
    }

    // Must leave anything the editor writes unchanged: syncedDraft spots its own saves by exact match, so
    // altering one raises a false "changed elsewhere" conflict. That rules out trimming each line, where
    // trailing spaces also carry meaning (hard breaks, code blocks, an empty "- [ ] " item).
    public static string? Normalize(string? description)
    {
        return description?.Replace("\r\n", "\n").Replace('\r', '\n').TrimEnd(' ', '\t', '\n');
    }
}
