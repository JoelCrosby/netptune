using System;

using Netptune.Core.BaseEntities;

namespace Netptune.Core.Entities
{
    public class HashCode : AuditableEntity<int>
    {
        public string Salt { get; set; }

        public string Code { get; set; }

        public DateTime Expires { get; set; }
    }
}
