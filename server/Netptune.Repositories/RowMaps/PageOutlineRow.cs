namespace Netptune.Repositories.RowMaps;

public class PageOutlineRow
{
    public int Id { get; set; }

    public int? Parent_id { get; set; }

    public string Title { get; set; } = null!;
}
