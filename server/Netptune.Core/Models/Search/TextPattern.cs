namespace Netptune.Core.Models.Search;

public sealed record TextPattern(string Pattern, bool IsRegex, bool IsCaseSensitive);
