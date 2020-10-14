using System;
using Netptune.Core.Enums;

namespace Netptune.Repositories.RowMaps
{
    public class TasksViewRowMap
    {
        public string Board_Name { get; set; }

        public string Board_Identifier { get; set; }

        public string Task_Description { get; set; }

        public int Task_Id { get; set; }

        public string Task_Name { get; set; }

        public ProjectTaskStatus Task_Status { get; set; }

        public bool Task_Is_Flagged { get; set; }

        public double Task_Sort_Order { get; set; }

        public string Project_Scope_Id { get; set; }

        public string Board_Group_Name { get; set; }

        public BoardGroupType Board_Group_Type { get; set; }

        public double Board_Group_Sort_Order { get; set; }

        public string Assignee_Firstname { get; set; }

        public string Assignee_Lastname { get; set; }

        public string Assignee_Email { get; set; }

        public string Owner_Firstname { get; set; }

        public string Owner_Lastname { get; set; }

        public string Owner_Email { get; set; }

        public string Tag { get; set; }

        public DateTime Task_Created_At { get; set; }

        public DateTime? Task_Updated_At { get; set; }

        public string Workspace_Name { get; set; }

        public string Workspace_Identifier { get; set; }

        public string Project_Key { get; set; }

        public string Project_Name { get; set; }
    }
}
