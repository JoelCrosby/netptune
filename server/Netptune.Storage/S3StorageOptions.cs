using System.ComponentModel.DataAnnotations;

namespace Netptune.Storage;

public class S3StorageOptions
{
    [Required]
    public string BucketName { get; set; } = null!;

    [Required]
    public string Region { get; set; } = null!;

    [Required]
    public string AccessKeyID { get; set; } = null!;

    [Required]
    public string SecretAccessKey { get; set; } = null!;
}
