using System.ComponentModel.DataAnnotations;

namespace Netptune.Storage;

public class S3StorageOptions
{
    public const string SectionName = "Storage:S3";

    [Required]
    public string BucketName { get; set; } = null!;

    [Required]
    public string Region { get; set; } = null!;

    [Required]
    public string AccessKeyId { get; set; } = null!;

    [Required]
    public string SecretAccessKey { get; set; } = null!;

    public string? AuditArchiveBucketName { get; set; }

    public CloudFrontOptions CloudFront { get; set; } = new();
}
