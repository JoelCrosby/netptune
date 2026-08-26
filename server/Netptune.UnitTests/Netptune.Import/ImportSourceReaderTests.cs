using System.Text;

using ClosedXML.Excel;

using FluentAssertions;

using Netptune.Import;
using Netptune.Transfer;
using Netptune.Transfer.Enums;
using Netptune.Transfer.Mapping;
using Netptune.Transfer.Services;

using Xunit;

namespace Netptune.UnitTests.Netptune.Import;

public class ImportSourceReaderTests
{
    [Fact]
    public async Task CsvReader_SniffsASemicolonDelimiterAndProfilesEveryColumn()
    {
        var reader = new CsvImportSourceReader();
        var source = Utf8("""
            Name;points;due;owner
            first;3;2026-08-18;a@acme.co.uk
            second;5;2026-09-18;b@acme.co.uk
            """);
        var profile = await reader.Profile(source, new ImportReadOptions(), TestContext.Current.CancellationToken);

        profile.Delimiter.Should().Be(';');
        profile.EstimatedRowCount.Should().Be(2);
        profile.Columns.Select(column => column.Name).Should().Equal("Name", "points", "due", "owner");
        profile.Columns[1].InferredType.Should().Be(TransferValueType.Decimal);
        profile.Columns[2].InferredType.Should().Be(TransferValueType.DateTime);
        profile.Columns[3].InferredType.Should().Be(TransferValueType.Ref);
    }

    [Fact]
    public async Task CsvReader_ReadsAUtf16FileWithAByteOrderMark()
    {
        var reader = new CsvImportSourceReader();
        var text = "Name,points\r\nfirst,3\r\n";
        var bytes = Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes(text)).ToArray();
        var profile = await reader.Profile(new MemoryStream(bytes), new ImportReadOptions(), TestContext.Current.CancellationToken);

        profile.Encoding.Should().Be(Encoding.Unicode.WebName);
        profile.Columns.Select(column => column.Name).Should().Equal("Name", "points");
        profile.EstimatedRowCount.Should().Be(1);
    }

    [Fact]
    public async Task CsvReader_NamesColumnsPositionallyWhenThereIsNoHeaderRow()
    {
        var reader = new CsvImportSourceReader();
        var source = Utf8("first,3\nsecond,5");
        var options = new ImportReadOptions { HasHeaderRow = false };
        var profile = await reader.Profile(source, options, TestContext.Current.CancellationToken);

        profile.Columns.Select(column => column.Name).Should().Equal("Column 1", "Column 2");
        profile.EstimatedRowCount.Should().Be(2);
    }

    [Fact]
    public async Task XlsxReader_ListsEverySheetAndReadsTheSelectedOne()
    {
        var reader = new XlsxImportSourceReader();
        var source = Workbook();
        var profile = await reader.Profile(source, new ImportReadOptions { SelectedSheet = "Second" }, TestContext.Current.CancellationToken);

        profile.SheetNames.Should().Equal("First", "Second");
        profile.SelectedSheet.Should().Be("Second");
        profile.Columns.Select(column => column.Name).Should().Equal("Other");
        profile.EstimatedRowCount.Should().Be(1);
    }

    [Fact]
    public async Task XlsxReader_FallsBackToTheFirstSheetAndKeepsTypedCells()
    {
        var reader = new XlsxImportSourceReader();
        var source = Workbook();
        var profile = await reader.Profile(source, new ImportReadOptions(), TestContext.Current.CancellationToken);

        profile.SelectedSheet.Should().Be("First");
        profile.Columns.Select(column => column.Name).Should().Equal("Name", "points", "due");
        profile.Columns[1].InferredType.Should().Be(TransferValueType.Decimal);
        profile.Columns[2].SampleValues.Should().ContainSingle().Which.Should().StartWith("2026-08-18");
    }

    [Fact]
    public async Task JsonReader_TakesTheFirstArrayAndUnionsTheKeysItFinds()
    {
        var reader = new JsonImportSourceReader(false);
        var source = Utf8("""
            { "cards": [ { "Name": "first", "points": 3 }, { "Name": "second", "tags": ["a", "b"] } ] }
            """);
        var profile = await reader.Profile(source, new ImportReadOptions(), TestContext.Current.CancellationToken);

        profile.Columns.Select(column => column.Name).Should().Equal("Name", "points", "tags");
        profile.EstimatedRowCount.Should().Be(2);

        var rows = await ReadAll(reader, source);

        rows[1].Values[0].Should().Be("second");
        rows[1].Values[1].Should().BeNull();
        rows[1].Values[2].Should().Be("a|b");
    }

    [Fact]
    public async Task NdjsonReader_ReadsOneRecordPerLineAndSkipsBlankLines()
    {
        var reader = new JsonImportSourceReader(true);
        var source = Utf8("""
            { "Name": "first", "points": 3 }

            { "Name": "second", "points": 5 }
            """);
        var profile = await reader.Profile(source, new ImportReadOptions(), TestContext.Current.CancellationToken);

        profile.Kind.Should().Be(ImportSourceKind.Ndjson);
        profile.EstimatedRowCount.Should().Be(2);

        var rows = await ReadAll(reader, source);

        rows.Should().HaveCount(2);
        rows[0].Values[0].Should().Be("first");
        rows[1].Values[1].Should().Be("5");
    }

    [Fact]
    public void Readers_ClaimTheExtensionsTheyCanActuallyRead()
    {
        new CsvImportSourceReader().CanRead("tasks.csv").Should().BeTrue();
        new CsvImportSourceReader().CanRead("tasks.xlsx").Should().BeFalse();
        new XlsxImportSourceReader().CanRead("tasks.xlsx").Should().BeTrue();
        new JsonImportSourceReader(false).CanRead("tasks.json").Should().BeTrue();
        new JsonImportSourceReader(false).CanRead("tasks.ndjson").Should().BeFalse();
        new JsonImportSourceReader(true).CanRead("tasks.ndjson").Should().BeTrue();
        new JsonImportSourceReader(true).CanRead("tasks.jsonl").Should().BeTrue();
    }

    private static async Task<List<ImportRow>> ReadAll(IImportSourceReader reader, Stream source)
    {
        var rows = new List<ImportRow>();

        await foreach (var row in reader.ReadRows(source, new ImportReadOptions(), TestContext.Current.CancellationToken))
        {
            rows.Add(row);
        }

        return rows;
    }

    private static MemoryStream Utf8(string content)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(content));
    }

    private static MemoryStream Workbook()
    {
        using var workbook = new XLWorkbook();

        var first = workbook.AddWorksheet("First");

        first.Cell(1, 1).SetValue("Name");
        first.Cell(1, 2).SetValue("points");
        first.Cell(1, 3).SetValue("due");
        first.Cell(2, 1).SetValue("first");
        first.Cell(2, 2).SetValue(3);
        first.Cell(2, 3).SetValue(new DateTime(2026, 8, 18, 0, 0, 0, DateTimeKind.Unspecified));

        var second = workbook.AddWorksheet("Second");

        second.Cell(1, 1).SetValue("Other");
        second.Cell(2, 1).SetValue("value");

        var buffer = new MemoryStream();

        workbook.SaveAs(buffer);
        buffer.Seek(0, SeekOrigin.Begin);

        return buffer;
    }
}
