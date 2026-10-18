namespace Netptune.Core.SourceControl.Models;

public sealed record ScmCodeMatch(string Path, IReadOnlyList<string> Fragments, string WebUrl);
