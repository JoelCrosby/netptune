using System;
using System.Collections.Generic;

using Netptune.Core.Entities;
using Netptune.Core.Enums;

namespace Netptune.Core.ViewModels.Comments
{
    public class CommentViewModel
    {
        public int Id { get; set; }

        public string UserDisplayName { get; set; }

        public string UserId { get; set; }

        public string Body { get; set; }

        public int EntityId { get; set; }

        public EntityType EntityType { get; set; }

        public List<Reaction> Reactions { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}
