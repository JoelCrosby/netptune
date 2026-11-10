using FluentAssertions;

using Netptune.SourceControl.Providers.AzureDevOps;

using Xunit;

namespace Netptune.UnitTests.Netptune.SourceControl.Providers.AzureDevOps;

public sealed class AzureDevOpsOrganisationTests
{
    [Theory]
    [InlineData("acme")]
    [InlineData("  acme  ")]
    [InlineData("https://dev.azure.com/acme")]
    [InlineData("https://dev.azure.com/acme/")]
    [InlineData("https://dev.azure.com/acme/Platform/_git/api")]
    [InlineData("dev.azure.com/acme")]
    [InlineData("acme.visualstudio.com")]
    [InlineData("https://acme.visualstudio.com")]
    [InlineData("https://acme.visualstudio.com/DefaultCollection")]
    public void Parse_ShouldReadTheOrganisationName(string input)
    {
        var organisation = AzureDevOpsOrganisation.Parse(input);

        organisation.Should().Be("acme");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("https://dev.azure.com")]
    [InlineData("https://dev.azure.com/")]
    [InlineData("https://github.com/acme")]
    [InlineData("ftp://dev.azure.com/acme")]
    [InlineData("a.b.visualstudio.com")]
    [InlineData("-acme")]
    [InlineData("acme corp")]
    [InlineData("acme/../other")]
    [InlineData("https://dev.azure.com/acme%20corp")]
    public void Parse_ShouldRejectAnythingElse(string? input)
    {
        var organisation = AzureDevOpsOrganisation.Parse(input);

        organisation.Should().BeNull();
    }
}
