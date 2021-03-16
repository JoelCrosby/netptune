namespace Netptune.Repositories.RowMaps
{
    public class TaskAncestorRow
    {
        public int Task_id { get; set; }

        public int Board_group_id { get; set; }

        public int Board_id { get; set; }

        public int Project_id { get; set; }

        public int Workspace_id { get; set; }
    }
}
