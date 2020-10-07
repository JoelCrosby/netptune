﻿using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.Entities;

namespace Netptune.Core.Relationships
{
    public class WorkspaceProject : KeyedEntity<int>
    {
        public int WorkspaceId { get; set; }

        public int ProjectId { get; set; }

        #region NavigationProperties

        [JsonIgnore]
        public Workspace Workspace { get; set; }

        [JsonIgnore]
        public Project Project { get; set; }

        #endregion
    }
}