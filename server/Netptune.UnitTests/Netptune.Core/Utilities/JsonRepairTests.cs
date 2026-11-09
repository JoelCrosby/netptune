using System.Text.Json;

using FluentAssertions;

using Netptune.Core.Utilities;

using Xunit;

namespace Netptune.UnitTests.Netptune.Core.Utilities;

public class JsonRepairTests
{
    [Theory]
    [InlineData("""{"summary":"Unclear what "full page" means.","findings":[]}""", "Unclear what \"full page\" means.")]
    [InlineData("""{"summary":"A 5" wide panel.","findings":[]}""", "A 5\" wide panel.")]
    [InlineData("""{"summary":""Full page" is vague.","findings":[]}""", "\"Full page\" is vague.")]
    [InlineData("""{"summary":"Vague about "full page"","findings":[]}""", "Vague about \"full page\"")]
    [InlineData("""{"summary":"Both "a" and "b" and "c" apply.","findings":[]}""", "Both \"a\" and \"b\" and \"c\" apply.")]
    [InlineData("""{"summary":"Says "full page", then stops.","findings":[]}""", "Says \"full page\", then stops.")]
    [InlineData("""{"summary":"Compare "a", "b" here.","findings":[]}""", "Compare \"a\", \"b\" here.")]
    [InlineData("""{"summary":"The "scope": unclear.","findings":[]}""", "The \"scope\": unclear.")]
    [InlineData("""{"summary":"See ["a"] there.","findings":[]}""", "See [\"a\"] there.")]
    [InlineData("""{"summary":"Has { and "a" and }.","findings":[]}""", "Has { and \"a\" and }.")]
    [InlineData("""{"summary":"Kept \"this\" and "that".","findings":[]}""", "Kept \"this\" and \"that\".")]
    [InlineData("""{"summary":"Path C:\\temp and "that".","findings":[]}""", "Path C:\\temp and \"that\".")]
    [InlineData("""{"summary":"Café "naïve" ✓","findings":[]}""", "Café \"naïve\" ✓")]
    public void EscapeStrayQuotes_ShouldEscapeQuotes_InsideAValue(string raw, string summary)
    {
        using var document = Repair(raw);

        document.RootElement.GetProperty("summary").GetString().Should().Be(summary);
    }

    [Fact]
    public void EscapeStrayQuotes_ShouldLeaveValidJsonUnchanged()
    {
        const string raw = """{"a":"Say \"b\", \\","c":[1,-2.5e3,true,false,null,"d"],"e":{"f":null}}""";

        var repaired = JsonRepair.EscapeStrayQuotes(raw);

        repaired.Should().Be(raw);
    }

    [Fact]
    public void EscapeStrayQuotes_ShouldRepairAValue_NestedInsideAnArrayOfObjects()
    {
        const string raw = """{"tasks":[{"name":"The "first" part","note":"One"},{"name":"Second","note":"The "other" part"}]}""";

        using var document = Repair(raw);

        var tasks = document.RootElement.GetProperty("tasks");

        tasks[0].GetProperty("name").GetString().Should().Be("The \"first\" part");
        tasks[1].GetProperty("note").GetString().Should().Be("The \"other\" part");
    }

    [Fact]
    public void EscapeStrayQuotes_ShouldKeepEveryArrayItem_WhenOnlyOneHasStrayQuotes()
    {
        const string raw = """{"items":[{"title":"First"},{"title":"The "second""},{"title":"Third"}]}""";

        using var document = Repair(raw);

        var titles = document.RootElement.GetProperty("items").EnumerateArray()
            .Select(item => item.GetProperty("title").GetString());

        titles.Should().Equal("First", "The \"second\"", "Third");
    }

    [Fact]
    public void EscapeStrayQuotes_ShouldLeaveArrayItemsAlone_WhenRepairingElsewhere()
    {
        const string raw = """{"summary":"The "a" one.","related":[715, "netp-1", null, "netp-2"]}""";

        using var document = Repair(raw);

        var related = document.RootElement.GetProperty("related");

        related.GetArrayLength().Should().Be(4);
        related[0].GetInt32().Should().Be(715);
        related[1].GetString().Should().Be("netp-1");
        related[2].ValueKind.Should().Be(JsonValueKind.Null);
        related[3].GetString().Should().Be("netp-2");
    }

    [Fact]
    public void EscapeStrayQuotes_ShouldKeepLiterals_WhenValuesAreNotStrings()
    {
        const string raw = """{"summary":"The "a" one.","value":-1.5e2,"missing":null,"flag":true,"off":false}""";

        using var document = Repair(raw);

        var root = document.RootElement;

        root.GetProperty("value").GetDouble().Should().Be(-150);
        root.GetProperty("missing").ValueKind.Should().Be(JsonValueKind.Null);
        root.GetProperty("flag").GetBoolean().Should().BeTrue();
        root.GetProperty("off").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public void EscapeStrayQuotes_ShouldRepairPrettyPrintedJson()
    {
        const string raw = """
            {
              "summary" : "The "a" one.",
              "items" : [
                {
                  "title" : "The "b" one"
                }
              ]
            }
            """;

        using var document = Repair(raw);

        var root = document.RootElement;

        root.GetProperty("summary").GetString().Should().Be("The \"a\" one.");
        root.GetProperty("items")[0].GetProperty("title").GetString().Should().Be("The \"b\" one");
    }

    [Fact]
    public void EscapeStrayQuotes_ShouldLeaveTextAroundTheJsonUnchanged()
    {
        const string raw = """
            Here is the "review" you wanted:
            ```json
            {"summary":"The "a" one."}
            ```
            I hope "that" helps {you}.
            """;

        const string expected = """
            Here is the "review" you wanted:
            ```json
            {"summary":"The \"a\" one."}
            ```
            I hope "that" helps {you}.
            """;

        var repaired = JsonRepair.EscapeStrayQuotes(raw);

        repaired.Should().Be(expected);
    }

    [Theory]
    [InlineData("There is no object here.")]
    [InlineData("""{"summary":"Ends with "a"} and more.","findings":[]}""")]
    [InlineData("""{"summary":"Lists "a", "b": together.","findings":[]}""")]
    [InlineData("""{"sum"mary":"Bad key.","findings":[]}""")]
    [InlineData("""{"summary":"Missing comma" "findings":[]}""")]
    [InlineData("""{"summary":unquoted "text","findings":[]}""")]
    [InlineData("""{"summary":"The "a" one.","findings":[]} and "more" on this line""")]
    [InlineData("""{"summary":"Ok.","findings":[{"title":"The "a" one"}}]}""")]
    public void EscapeStrayQuotes_ShouldGiveUp_WhenTheTextDoesNotFitTheJsonAroundIt(string raw)
    {
        var repaired = JsonRepair.EscapeStrayQuotes(raw);

        repaired.Should().BeNull();
    }

    [Fact]
    public void EscapeStrayQuotes_ShouldGiveUp_RatherThanSwallowingWhatFollowsAStrayBrace()
    {
        const string raw = """{"items":[{"tasks":[{"name":"A"}]}},"related":[]},{"title":"Second"},{"title":"Third"}]}""";

        var repaired = JsonRepair.EscapeStrayQuotes(raw);

        repaired.Should().BeNull();
    }

    private static JsonDocument Repair(string raw)
    {
        var repaired = JsonRepair.EscapeStrayQuotes(raw);

        return JsonDocument.Parse(repaired!);
    }
}
