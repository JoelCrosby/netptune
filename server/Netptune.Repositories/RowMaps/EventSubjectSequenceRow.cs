namespace Netptune.Repositories.RowMaps;

public class EventSubjectSequenceRow
{
    public int Workspace_id { get; set; }

    public string Subject_type { get; set; } = null!;

    public string Subject_id { get; set; } = null!;

    public long Current_sequence { get; set; }
}
