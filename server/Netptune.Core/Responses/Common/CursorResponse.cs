namespace Netptune.Core.Responses.Common;

public sealed record CursorResponse<T>(
    IReadOnlyList<T> Items,
    int Take,
    string? NextCursor);
