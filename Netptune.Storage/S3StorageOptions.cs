namespace Netptune.Storage
{
    public class S3StorageOptions
    {
        public string BucketName { get; set; }

        public string Region { get; set; }

        public string AccessKeyID { get; set; }

        public string SecretAccessKey { get; set; }
    }
}
