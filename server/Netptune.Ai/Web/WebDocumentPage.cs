namespace Netptune.Ai.Web;

public sealed record WebDocumentPage
{
    public string Text { get; init; } = string.Empty;

    public int Offset { get; init; }

    public int NextOffset { get; init; }

    public bool HasMore { get; init; }

    public static WebDocumentPage Read(string content, int offset, int take)
    {
        var start = Math.Clamp(offset, 0, content.Length);
        var length = Math.Min(take, content.Length - start);
        var end = start + length;
        var boundary = FindBoundary(content, start, end);
        var text = content[start..boundary];

        return new WebDocumentPage
        {
            Text = text,
            Offset = start,
            NextOffset = boundary,
            HasMore = boundary < content.Length,
        };
    }

    private static int FindBoundary(string content, int start, int end)
    {
        var isEndOfContent = end >= content.Length;

        if (isEndOfContent)
        {
            return content.Length;
        }

        var minimum = start + ((end - start) / 2);
        var newline = content.LastIndexOf('\n', end - 1, end - start);

        if (newline > minimum)
        {
            return newline + 1;
        }

        var space = content.LastIndexOf(' ', end - 1, end - start);

        return space > minimum ? space + 1 : end;
    }
}
