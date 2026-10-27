using FluentAssertions;

using Netptune.Ai.Web;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class AiWebFetchAllowanceTests
{
    [Fact]
    public void IsAllowed_ShouldLetThroughAnyPageOnASiteTheUserNamed()
    {
        var allowance = new AiWebFetchAllowance();

        allowance.AllowFromUserText("Can you summarise https://docs.example.com/guide/start.");

        allowance.IsAllowed(new Uri("https://docs.example.com/guide/start")).Should().BeTrue();
        allowance.IsAllowed(new Uri("https://docs.example.com/guide/next?page=2")).Should().BeTrue();
    }

    [Fact]
    public void IsAllowed_ShouldLetThroughASearchResult_ButNotOtherPagesOnItsSite()
    {
        var allowance = new AiWebFetchAllowance();

        allowance.AllowSearchResult("https://blog.example.org/post?id=7");

        allowance.IsAllowed(new Uri("https://blog.example.org/post?id=7#comments")).Should().BeTrue();
        allowance.IsAllowed(new Uri("https://blog.example.org/post?id=8")).Should().BeFalse();
    }

    [Fact]
    public void IsAllowed_ShouldRefuseAnAddressTheModelMadeUp()
    {
        var allowance = new AiWebFetchAllowance();

        allowance.AllowFromUserText("Read the pull request and tell me what it changes.");

        allowance.IsAllowed(new Uri("https://attacker.example/?c=secret")).Should().BeFalse();
    }
}
