using System.Security.Cryptography;

using FluentAssertions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Netptune.Core.Services;
using Netptune.Storage;

using Xunit;

namespace Netptune.UnitTests.Netptune.Storage;

public class ServiceCollectionExtensionsTests
{
    private const string BaseUrl = "https://files.example.test";
    private const string KeyPairId = "K2JCJMDEHXQW5F";

    [Theory]
    [InlineData("files.example.test")]
    [InlineData("http://files.example.test")]
    [InlineData("/files")]
    public void AddS3StorageService_ShouldFailValidation_WhenBaseUrlIsNotAbsoluteHttps(string baseUrl)
    {
        using var rsa = RSA.Create(2048);
        using var provider = BuildProvider(baseUrl, KeyPairId, rsa.ExportPkcs8PrivateKeyPem());

        var read = () => provider.GetRequiredService<IOptions<S3StorageOptions>>().Value;

        read.Should().Throw<OptionsValidationException>().WithMessage("*must be an absolute https URL*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not a pem")]
    public void AddS3StorageService_ShouldFailValidation_WhenPrivateKeyIsNotAPem(string? privateKey)
    {
        using var provider = BuildProvider(BaseUrl, KeyPairId, privateKey);

        var read = () => provider.GetRequiredService<IOptions<S3StorageOptions>>().Value;

        read.Should().Throw<OptionsValidationException>().WithMessage("*PEM-encoded RSA PrivateKey*");
    }

    [Fact]
    public void AddS3StorageService_ShouldFailValidation_WhenPemNewlinesAreLiteral()
    {
        using var rsa = RSA.Create(2048);
        var privateKey = rsa.ExportPkcs8PrivateKeyPem().Replace("\n", "\\n");
        using var provider = BuildProvider(BaseUrl, KeyPairId, privateKey);

        var read = () => provider.GetRequiredService<IOptions<S3StorageOptions>>().Value;

        read.Should().Throw<OptionsValidationException>().WithMessage("*PEM-encoded RSA PrivateKey*");
    }

    [Fact]
    public void AddS3StorageService_ShouldFailValidation_WhenKeyPairIdIsMissing()
    {
        using var rsa = RSA.Create(2048);
        using var provider = BuildProvider(BaseUrl, null, rsa.ExportPkcs8PrivateKeyPem());

        var read = () => provider.GetRequiredService<IOptions<S3StorageOptions>>().Value;

        read.Should().Throw<OptionsValidationException>().WithMessage("*requires KeyPairId*");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddS3StorageService_ShouldResolveTheService_WhenCloudFrontIsConfigured(bool usePkcs8)
    {
        using var rsa = RSA.Create(2048);
        var privateKey = usePkcs8 ? rsa.ExportPkcs8PrivateKeyPem() : rsa.ExportRSAPrivateKeyPem();
        using var provider = BuildProvider(BaseUrl, KeyPairId, privateKey);

        var service = provider.GetRequiredService<IStorageService>();

        service.Should().BeOfType<S3StorageService>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void AddS3StorageService_ShouldNotRequireASigningKey_WhenBaseUrlIsUnset(string? baseUrl)
    {
        using var provider = BuildProvider(baseUrl, null, null);

        var service = provider.GetRequiredService<IStorageService>();

        service.Should().BeOfType<S3StorageService>();
    }

    private static ServiceProvider BuildProvider(string? baseUrl, string? keyPairId, string? privateKey)
    {
        var settings = new Dictionary<string, string?>
        {
            ["Storage:S3:BucketName"] = "bucket",
            ["Storage:S3:Region"] = "eu-west-2",
            ["Storage:S3:AccessKeyId"] = "AKIAIOSFODNN7EXAMPLE",
            ["Storage:S3:SecretAccessKey"] = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
            ["Storage:S3:CloudFront:BaseUrl"] = baseUrl,
            ["Storage:S3:CloudFront:KeyPairId"] = keyPairId,
            ["Storage:S3:CloudFront:PrivateKey"] = privateKey,
        };

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddS3StorageService();

        return services.BuildServiceProvider();
    }
}
