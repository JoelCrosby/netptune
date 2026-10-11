using System.Text;

namespace Netptune.Core.Models.Pages;

public sealed record PageTemplate(string Key, string Title, string Body);

public sealed record ReleaseNoteTask(string SystemId, string Name);

public static class PageTemplates
{
    public const string Blank = "blank";
    public const string FromSprint = "from-sprint";

    public const string UnknownMessage = "Unknown template.";

    public static readonly IReadOnlyList<PageTemplate> BuiltIn =
    [
        new(Blank, PageTitle.Untitled, string.Empty),
        new("meeting-notes", "Meeting notes", """
            ## Attendees

            - Name

            ## Agenda

            1. Topic

            ## Notes

            Write notes here.

            ## Action items

            - [ ] Action and owner
            """),
        new("rfc", "RFC", """
            ## Problem

            What is wrong today, and who it affects.

            ## Proposal

            What we will do.

            ## Alternatives considered

            What else we looked at, and why not.

            ## Open questions

            - Question
            """),
        new("runbook", "Runbook", """
            > [!WARNING]
            > Read the whole runbook before starting.

            ## When to use this

            The situation this runbook is for.

            ## Steps

            1. First step

            ## Rollback

            1. How to undo it

            ## Owners

            - Team or person
            """),
        new("retro", "Retro", """
            ## What went well

            - Item

            ## What didn't

            - Item

            ## Actions

            - [ ] Action and owner
            """),
        new("spec", "Spec", """
            ## Goals

            - Goal

            ## Non-goals

            - Out of scope

            ## Requirements

            - [ ] Requirement

            ## Linked tasks

            Mention tasks with @ to link them here.
            """),
    ];

    public static PageTemplate? Find(string key)
    {
        return BuiltIn.FirstOrDefault(template => string.Equals(template.Key, key, StringComparison.Ordinal));
    }

    public static string ReleaseNotesTitle(string sprintName)
    {
        var title = $"Release notes: {sprintName}";
        var isTooLong = PageTitle.IsTooLong(title);

        return isTooLong ? title[..PageTitle.MaxLength].TrimEnd() : title;
    }

    public static string ReleaseNotes(string sprintName, IReadOnlyCollection<ReleaseNoteTask> completed)
    {
        var body = new StringBuilder();

        body.AppendLine($"Completed in {sprintName}.");
        body.AppendLine();
        body.AppendLine("## Completed");
        body.AppendLine();

        if (completed.Count == 0)
        {
            body.AppendLine("Nothing was completed in this sprint.");
        }

        foreach (var task in completed)
        {
            var label = task.Name.Replace("]", ")", StringComparison.Ordinal).Replace("|", "/", StringComparison.Ordinal);

            body.AppendLine($"- [[task:{task.SystemId}|{label}]]");
        }

        return body.ToString().TrimEnd();
    }
}
