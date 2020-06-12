using Netptune.Core.Enums;

using System.ComponentModel.DataAnnotations;

namespace Netptune.Core.Requests
{
    public class AddBoardGroupRequest
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public int? BoardId { get; set; }

        public BoardGroupType? Type { get; set; }

        public double? SortOrder { get; set; }
    }
}
