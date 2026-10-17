using System.ComponentModel.DataAnnotations;

using Netptune.Core.SourceControl;

namespace Netptune.Core.Requests.SourceControl;

public sealed record AddSourceControlIdentityRequest
{
    public SourceControlProvider Provider { get; init; }

    [Required]
    [MaxLength(100)]
    public required string Username { get; init; }
}
