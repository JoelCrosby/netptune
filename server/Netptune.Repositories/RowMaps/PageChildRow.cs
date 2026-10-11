namespace Netptune.Repositories.RowMaps;

public class PageChildRow
{
    public int Id { get; set; }

    public int Kind { get; set; }

    public string Title { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public int Status { get; set; }

    public string? Excerpt_source { get; set; }

    public bool Is_body_truncated { get; set; }

    public long Word_count { get; set; }

    public DateTime Updated_at { get; set; }

    public string? Editor_display_name { get; set; }

    public string? Editor_picture_url { get; set; }

    public long Child_count { get; set; }
}
