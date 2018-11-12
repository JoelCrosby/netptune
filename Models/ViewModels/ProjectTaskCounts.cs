using Netptune.Models.Relationships;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace Netptune.Models.ViewModels
{
    public class ProjectTaskCounts
    {
        public int AllTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int BacklogTasks { get; set; }
    }
}
