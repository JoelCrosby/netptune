namespace Netptune.Storage;

public class S3StorageOptions
{
    public string BucketName { get; set; } = null!;

    public string Region { get; set; } = null!;

    public string AccessKeyID { get; set; } = null!;

    public string SecretAccessKey { get; set; } = null!;
}
