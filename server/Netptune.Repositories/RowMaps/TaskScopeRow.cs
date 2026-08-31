namespace Netptune.Repositories.RowMaps;

public class TaskScopeRow
{
    public int Task_id { get; set; }

    public int? Project_id { get; set; }

    public int? Sprint_id { get; set; }

    public int? Board_id { get; set; }

    public int? Board_group_id { get; set; }
}
