namespace Netptune.Repositories.RowMaps;

public class BoardGroupAncestorRow
{
    public int Board_group_id { get; set; }

    public int Board_id { get; set; }

    public string? Board_key { get; set; }

    public int Project_id { get; set; }

    public string? Project_key { get; set; }

    public int Workspace_id { get; set; }

    public string? Workspace_key { get; set; }
}
