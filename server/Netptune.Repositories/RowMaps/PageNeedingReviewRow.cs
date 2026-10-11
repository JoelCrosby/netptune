namespace Netptune.Repositories.RowMaps;

public class PageNeedingReviewRow
{
    public int Id { get; set; }

    public int? Parent_id { get; set; }

    public string Title { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public DateTime Updated_at { get; set; }

    public int Reason { get; set; }
}
