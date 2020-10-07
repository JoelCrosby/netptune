using Netptune.Core.Enums;

namespace Netptune.Repositories.RowMaps
{
    public class BoardViewRowMap
    {
        public string Name { get; set; }

        public string Identifier { get; set; }

        public int Task_Id { get; set; }

        public string Task_Name { get; set; }

        public ProjectTaskStatus Task_Status { get; set; }

        public bool Is_Flagged { get; set; }

        public double Sort_Order { get; set; }

        public int Project_Id { get; set; }

        public string Project_Scope_Id { get; set; }

        public int Workspace_Id { get; set; }

        public int Board_Group_Id { get; set; }

        public string Board_Group_Name { get; set; }

        public BoardGroupType Board_Group_Type { get; set; }

        public string Firstname { get; set; }

        public string Lastname { get; set; }

        public string Picture_Url { get; set; }

        public string Assignee_Id { get; set; }

        public string Tag { get; set; }
    }

    public class BoardViewMetaRowMap
    {
        public string Workspace_Identifier { get; set; }

        public string Project_Key { get; set; }
    }
}
