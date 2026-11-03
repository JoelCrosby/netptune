using System.Security.Cryptography;
using System.Text;
using System.Web;

using Amazon.S3;
using Amazon.S3.Model;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Netptune.Core.Storage;
using Netptune.Storage;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Storage;

public class S3StorageServiceTests
{
    private const string KeyPairId = "K2JCJMDEHXQW5F";
    private const string CloudFrontBaseUrl = "https://files.example.test";

    private static readonly StorageReadOptions ReadOptions = new()
    {
        Key = "workspace/1/files/9/abc",
        FileName = "日本語 it's résumé.pdf",
        Disposition = StorageDisposition.Attachment,
        Lifetime = TimeSpan.FromMinutes(5),
    };

    private readonly IAmazonS3 S3Client = Substitute.For<IAmazonS3>();

    public S3StorageServiceTests()
    {
        S3Client.Config.Returns(new AmazonS3Config());

        S3Client
            .PutObjectAsync(Arg.Any<PutObjectRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PutObjectResponse());

        S3Client
            .GetObjectMetadataAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new GetObjectMetadataResponse { ContentLength = 3 });
    }

    [Fact]
    public async Task GetReadUriAsync_ShouldUseCloudFront_WhenConfigured()
    {
        using var rsa = RSA.Create(2048);
        var service = CreateService(CreateCloudFrontOptions(rsa.ExportPkcs8PrivateKeyPem()));

        var uri = await service.GetReadUriAsync(ReadOptions, TestContext.Current.CancellationToken);

        uri!.Host.Should().Be("files.example.test");
        uri.AbsolutePath.Should().Be("/workspace/1/files/9/abc");
    }

    [Fact]
    public async Task GetReadUriAsync_ShouldPresignAgainstTheBucket_WhenCloudFrontIsNotConfigured()
    {
        var service = CreateService(new CloudFrontOptions());

        var uri = await service.GetReadUriAsync(ReadOptions, TestContext.Current.CancellationToken);

        uri!.Host.Should().Contain("amazonaws.com");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetReadUriAsync_ShouldKeepTheDispositionHeaderAscii_WhenTheFileNameIsNot(bool useCloudFront)
    {
        using var rsa = RSA.Create(2048);
        var cloudFront = CreateCloudFrontOptions(rsa.ExportPkcs8PrivateKeyPem());
        cloudFront.BaseUrl = useCloudFront ? CloudFrontBaseUrl : null;
        var service = CreateService(cloudFront);

        var uri = await service.GetReadUriAsync(ReadOptions, TestContext.Current.CancellationToken);

        var query = HttpUtility.ParseQueryString(uri!.Query);
        var disposition = query["response-content-disposition"];

        disposition.Should().Be(
            "attachment; filename=\"___ it's r_sum_.pdf\"; "
            + "filename*=UTF-8''%E6%97%A5%E6%9C%AC%E8%AA%9E%20it%27s%20r%C3%A9sum%C3%A9.pdf");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetReadUriAsync_ShouldProduceAVerifiableCannedSignature_ForEitherKeyFormat(bool usePkcs8)
    {
        using var rsa = RSA.Create(2048);
        var privateKey = usePkcs8 ? rsa.ExportPkcs8PrivateKeyPem() : rsa.ExportRSAPrivateKeyPem();
        var service = CreateService(CreateCloudFrontOptions(privateKey));

        var uri = await service.GetReadUriAsync(ReadOptions, TestContext.Current.CancellationToken);

        ShouldHaveValidSignature(uri!, rsa);
    }

    [Fact]
    public async Task GetReadUriAsync_ShouldKeepTheUrlAsSigned_WhenTheKeyAndFileNameNeedEscaping()
    {
        using var rsa = RSA.Create(2048);
        var service = CreateService(CreateCloudFrontOptions(rsa.ExportPkcs8PrivateKeyPem()));
        var readOptions = ReadOptions with
        {
            Key = "workspace/acme/exports/abc/my export + q3 (final).zip",
            FileName = "it's (final)! a&b=c+d;e%20f#g?.zip",
        };

        var uri = await service.GetReadUriAsync(readOptions, TestContext.Current.CancellationToken);

        uri!.AbsoluteUri.Should().Be(uri.OriginalString);
        uri.AbsolutePath.Should().Be("/workspace/acme/exports/abc/my%20export%20%2B%20q3%20%28final%29.zip");
        ShouldHaveValidSignature(uri, rsa);
    }

    [Theory]
    [InlineData(CloudFrontBaseUrl)]
    [InlineData(CloudFrontBaseUrl + "/")]
    public async Task UploadFileAsync_ShouldReturnACloudFrontUrl_WhenConfigured(string baseUrl)
    {
        using var rsa = RSA.Create(2048);
        var cloudFront = CreateCloudFrontOptions(rsa.ExportPkcs8PrivateKeyPem());
        cloudFront.BaseUrl = baseUrl;
        var service = CreateUploadService(cloudFront);

        var result = await Upload(service, "user/profile/abc-123.png", StorageAccess.PublicRead);

        result.Should().Be("https://files.example.test/user/profile/abc-123.png");
    }

    [Fact]
    public async Task UploadFileAsync_ShouldEscapeSegmentsButKeepSeparators_WhenCloudFrontIsConfigured()
    {
        using var rsa = RSA.Create(2048);
        var service = CreateUploadService(CreateCloudFrontOptions(rsa.ExportPkcs8PrivateKeyPem()));

        var result = await Upload(service, "workspace/1/exports/q3 report.zip", StorageAccess.Private);

        result.Should().Be("https://files.example.test/workspace/1/exports/q3%20report.zip");
    }

    [Theory]
    [InlineData(StorageAccess.PublicRead)]
    [InlineData(StorageAccess.Private)]
    public async Task UploadFileAsync_ShouldSendNoAcl_WhenCloudFrontIsConfigured(StorageAccess access)
    {
        using var rsa = RSA.Create(2048);
        var service = CreateUploadService(CreateCloudFrontOptions(rsa.ExportPkcs8PrivateKeyPem()));

        await Upload(service, "user/profile/abc-123.png", access);

        await S3Client.Received(1).PutObjectAsync(
            Arg.Is<PutObjectRequest>(request => request.CannedACL == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadFileAsync_ShouldSendAPublicAcl_WhenPublicAndCloudFrontIsNotConfigured()
    {
        var service = CreateUploadService(new CloudFrontOptions());

        await Upload(service, "user/profile/abc-123.png", StorageAccess.PublicRead);

        await S3Client.Received(1).PutObjectAsync(
            Arg.Is<PutObjectRequest>(request => request.CannedACL!.Value == "public-read"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadFileAsync_ShouldSendNoAcl_WhenPrivateAndCloudFrontIsNotConfigured()
    {
        var service = CreateUploadService(new CloudFrontOptions());

        await Upload(service, "workspace/1/files/9/abc", StorageAccess.Private);

        await S3Client.Received(1).PutObjectAsync(
            Arg.Is<PutObjectRequest>(request => request.CannedACL == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadFileAsync_ShouldWriteToTheAuditBucket_WhenConfigured()
    {
        using var rsa = RSA.Create(2048);
        var options = CreateOptions(CreateCloudFrontOptions(rsa.ExportPkcs8PrivateKeyPem()));

        options.Value.AuditArchiveBucketName = "audit-bucket";

        var service = new S3StorageService(options, NullLogger<S3StorageService>.Instance, S3Client);

        var result = await UploadAuditArchive(service);

        result.Should().Be("https://audit-bucket.s3.eu-west-2.amazonaws.com/audit-archive/run/000001.ndjson");

        await S3Client.Received(1).PutObjectAsync(
            Arg.Is<PutObjectRequest>(request => request.BucketName == "audit-bucket"),
            Arg.Any<CancellationToken>());
        await S3Client.Received(1).GetObjectMetadataAsync(
            "audit-bucket",
            "audit-archive/run/000001.ndjson",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadFileAsync_ShouldWriteAuditArchivesToTheMainBucket_WhenNoAuditBucketIsConfigured()
    {
        var service = CreateUploadService(new CloudFrontOptions());

        await UploadAuditArchive(service);

        await S3Client.Received(1).PutObjectAsync(
            Arg.Is<PutObjectRequest>(request => request.BucketName == "bucket"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadFileAsync_ShouldKeepUserUploadsInTheMainBucket_WhenAnAuditBucketIsConfigured()
    {
        var options = CreateOptions(new CloudFrontOptions());

        options.Value.AuditArchiveBucketName = "audit-bucket";

        var service = new S3StorageService(options, NullLogger<S3StorageService>.Instance, S3Client);

        await Upload(service, "workspace/1/files/9/abc", StorageAccess.Private);

        await S3Client.Received(1).PutObjectAsync(
            Arg.Is<PutObjectRequest>(request => request.BucketName == "bucket"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadFileAsync_ShouldReturnABucketUrl_WhenCloudFrontIsNotConfigured()
    {
        var service = CreateUploadService(new CloudFrontOptions());

        var result = await Upload(service, "user/profile/abc-123.png", StorageAccess.PublicRead);

        result.Should().Be("https://bucket.s3.eu-west-2.amazonaws.com/user/profile/abc-123.png");
    }

    private static async Task<string> UploadAuditArchive(S3StorageService service)
    {
        var uploadOptions = new StorageUploadOptions
        {
            Name = "audit-archive/run/000001.ndjson",
            Key = "audit-archive/run/000001.ndjson",
            ContentType = "application/x-ndjson",
            Bucket = StorageBucket.AuditArchive,
        };

        using var content = new MemoryStream([1, 2, 3]);

        var result = await service.UploadFileAsync(content, uploadOptions, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();

        return result.Payload!.Uri;
    }

    private static async Task<string> Upload(S3StorageService service, string key, StorageAccess access)
    {
        var uploadOptions = new StorageUploadOptions
        {
            Name = key,
            Key = key,
            ContentType = "image/png",
            Access = access,
        };

        using var content = new MemoryStream([1, 2, 3]);

        var result = await service.UploadFileAsync(content, uploadOptions, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();

        return result.Payload!.Uri;
    }

    private static void ShouldHaveValidSignature(Uri uri, RSA rsa)
    {
        var signedUrl = uri.AbsoluteUri;
        var expiresIndex = signedUrl.IndexOf("&Expires=", StringComparison.Ordinal);
        var resource = signedUrl[..expiresIndex];
        var parameters = signedUrl[(expiresIndex + 1)..]
            .Split('&')
            .Select(parameter => parameter.Split('=', 2))
            .ToDictionary(pair => pair[0], pair => pair[1]);

        var condition = $$$"""{"DateLessThan":{"AWS:EpochTime":{{{parameters["Expires"]}}}}}""";
        var policy = $$$"""{"Statement":[{"Resource":"{{{resource}}}","Condition":{{{condition}}}}]}""";
        var base64Signature = parameters["Signature"].Replace('-', '+').Replace('_', '=').Replace('~', '/');

        parameters["Key-Pair-Id"].Should().Be(KeyPairId);

        var isValid = rsa.VerifyData(
            Encoding.UTF8.GetBytes(policy),
            Convert.FromBase64String(base64Signature),
            HashAlgorithmName.SHA1,
            RSASignaturePadding.Pkcs1);

        isValid.Should().BeTrue();
    }

    private static CloudFrontOptions CreateCloudFrontOptions(string privateKey)
    {
        return new CloudFrontOptions
        {
            BaseUrl = CloudFrontBaseUrl,
            KeyPairId = KeyPairId,
            PrivateKey = privateKey,
        };
    }

    private static S3StorageService CreateService(CloudFrontOptions cloudFront)
    {
        return new S3StorageService(CreateOptions(cloudFront), NullLogger<S3StorageService>.Instance);
    }

    private S3StorageService CreateUploadService(CloudFrontOptions cloudFront)
    {
        return new S3StorageService(CreateOptions(cloudFront), NullLogger<S3StorageService>.Instance, S3Client);
    }

    private static IOptions<S3StorageOptions> CreateOptions(CloudFrontOptions cloudFront)
    {
        var options = new S3StorageOptions
        {
            BucketName = "bucket",
            Region = "eu-west-2",
            AccessKeyId = "AKIAIOSFODNN7EXAMPLE",
            SecretAccessKey = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
            CloudFront = cloudFront,
        };

        return Options.Create(options);
    }
}
