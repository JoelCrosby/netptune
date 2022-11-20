using AngleSharp.Html.Parser;

using FluentAssertions;

using Netptune.Core.Services.Integration;
using Netptune.Services;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services;

public class WebServiceTests
{
    private readonly WebService Service;

    private readonly IHtmlDocumentService DocumentService = Substitute.For<IHtmlDocumentService>();
    private readonly HtmlParser Parser = new (new ()
    {
        IsKeepingSourceReferences = true,
    });

    public WebServiceTests()
    {
        Service = new(DocumentService);
    }

    [Fact]
    public async Task GetMetaDataFromUrl_ShouldReturnCorrectly_WhenInputValid()
    {
        const string document = """
            <!DOCTYPE html>
            <html lang="en">
            <head>
              <meta charset="UTF-8">
              <meta http-equiv="X-UA-Compatible" content="IE=edge">
              <meta name="viewport" content="width=device-width, initial-scale=1.0">
              <meta name="description" content="Document description."/>
              <title>Document</title>
            </head>
            <body>

            </body>
            </html>
            """;

        DocumentService.OpenAsync("https://github.com").Returns(Parser.ParseDocument(document));

        var result = await Service.GetMetaDataFromUrl("https://github.com");

        result.Should().NotBeNull();

        result.Title.Should().Be("Document");
        result.Description.Should().Be("Document description.");
    }
}
