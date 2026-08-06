using Netptune.Core.Entities;

namespace Netptune.Transfer.Import;

public sealed class ImportVocabulary
{
    public required Dictionary<string, Status> StatusesByKey { get; init; }

    public required Dictionary<string, Status> StatusesByName { get; init; }

    public required Dictionary<string, Tag> TagsByName { get; init; }

    public required Dictionary<string, AppUser> UsersByEmail { get; init; }

    public required Dictionary<string, BoardGroup> BoardGroupsByName { get; init; }

    public required Dictionary<string, Sprint> SprintsByName { get; init; }

    public Status? FindStatus(string? value)
    {
        var normalized = Normalize(value);

        if (normalized is null)
        {
            return null;
        }

        return StatusesByKey.GetValueOrDefault(normalized) ?? StatusesByName.GetValueOrDefault(normalized);
    }

    public AppUser? FindUser(string? value)
    {
        var normalized = Normalize(value);

        if (normalized is null)
        {
            return null;
        }

        return UsersByEmail.GetValueOrDefault(normalized);
    }

    public Tag? FindTag(string? value)
    {
        var normalized = Normalize(value);

        if (normalized is null)
        {
            return null;
        }

        return TagsByName.GetValueOrDefault(normalized);
    }

    public BoardGroup? FindBoardGroup(string? value)
    {
        var normalized = Normalize(value);

        if (normalized is null)
        {
            return null;
        }

        return BoardGroupsByName.GetValueOrDefault(normalized);
    }

    public Sprint? FindSprint(string? value)
    {
        var normalized = Normalize(value);

        if (normalized is null)
        {
            return null;
        }

        return SprintsByName.GetValueOrDefault(normalized);
    }

    public void Register(Tag tag)
    {
        TagsByName[tag.Name.ToLowerInvariant()] = tag;
    }

    public void Register(BoardGroup boardGroup)
    {
        BoardGroupsByName[boardGroup.Name.ToLowerInvariant()] = boardGroup;
    }

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim().ToLowerInvariant();
    }
}
