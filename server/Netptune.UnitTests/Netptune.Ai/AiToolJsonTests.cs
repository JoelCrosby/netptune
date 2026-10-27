using FluentAssertions;

using Netptune.Ai.Tools;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class AiToolJsonTests
{
    [Fact]
    public void Relax_ShouldWriteTextAsItIs_InsteadOfAsEscapes()
    {
        var escaped = """{"title":"\u6771\u4EAC \u003Cdiv\u003E"}""";

        var relaxed = AiToolJson.Relax(escaped);

        relaxed.Should().Be("""{"title":"東京 <div>"}""");
    }

    [Theory]
    [InlineData("Proposed updating task 42.")]
    [InlineData("{not json")]
    public void Relax_ShouldLeaveAnythingThatIsNotJsonAlone(string content)
    {
        AiToolJson.Relax(content).Should().Be(content);
    }
}
