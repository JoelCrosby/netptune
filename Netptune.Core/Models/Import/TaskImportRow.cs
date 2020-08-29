namespace Netptune.Core.Models.Import
{
    public class TaskImportRow
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public string Status { get; set; }

        public string IsFlagged { get; set; }

        public string SortOrder { get; set; }

        public string CreatedAt { get; set; }

        public string UpdatedAt { get; set; }

        public string AssigneeEmail { get; set; }

        public string OwnerEmail { get; set; }

        public string Group { get; set; }
    }
}
