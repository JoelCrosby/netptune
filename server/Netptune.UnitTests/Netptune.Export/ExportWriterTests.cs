using Netptune.Transfer.Enums;
using System.Text;
using System.Text.Json;

using ClosedXML.Excel;

using FluentAssertions;

using Netptune.Transfer.Services;
using Netptune.Transfer;
using Netptune.Transfer.Export;
using Netptune.Export;

using Xunit;

namespace Netptune.UnitTests.Netptune.Export;

public class ExportWriterTests
{
    private static readonly IExportWriterFactory Writers = new ExportWriterFactory();

    [Fact]
    public async Task CsvWriter_WritesAHeaderRowAndOneRowPerRecord()
    {
        var text = await WriteText(ExportFormat.Csv, new ExportOptionsModel());
        var lines = text.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

        lines.Should().HaveCount(3);
        lines[0].Should().Be("System id,Name,Status,Due date,Tags");
        lines[1].Should().Be("acme-1,First task,in-progress,2026-08-18,backend|urgent");
        lines[2].Should().Be("acme-2,\"Second, task\",done,,");
    }

    [Fact]
    public async Task CsvWriter_CanOmitTheHeaderRow()
    {
        var text = await WriteText(ExportFormat.Csv, new ExportOptionsModel { IncludeHeaderRow = false });

        text.Should().StartWith("acme-1,");
    }

    [Fact]
    public async Task CsvWriter_HonoursTheDelimiterAndCollectionSeparator()
    {
        var options = new ExportOptionsModel { Delimiter = ';', CollectionSeparator = "," };
        var text = await WriteText(ExportFormat.Csv, options);

        text.Should().Contain("acme-1;First task;in-progress;2026-08-18;backend,urgent");
    }

    [Fact]
    public async Task TsvWriter_AlwaysUsesTabRegardlessOfTheConfiguredDelimiter()
    {
        var text = await WriteText(ExportFormat.Tsv, new ExportOptionsModel { Delimiter = ';' });

        text.Should().Contain("acme-1\tFirst task\tin-progress");
    }

    [Fact]
    public async Task CsvWriter_ExpandsCollectionsToRowsWhenAskedTo()
    {
        var options = new ExportOptionsModel { ExpandCollectionsToRows = true };
        var text = await WriteText(ExportFormat.Csv, options);
        var lines = text.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

        lines.Should().HaveCount(4);
        lines[1].Should().EndWith(",backend");
        lines[2].Should().EndWith(",urgent");
        lines[3].Should().EndWith(",");
    }

    [Fact]
    public async Task CsvWriter_ReportsTheNumberOfRowsItWrote()
    {
        var expanded = await WriteRowCount(ExportFormat.Csv, new ExportOptionsModel { ExpandCollectionsToRows = true });
        var flat = await WriteRowCount(ExportFormat.Csv, new ExportOptionsModel());

        flat.Should().Be(2);
        expanded.Should().Be(3);
    }

    [Fact]
    public async Task JsonWriter_WritesAnArrayOfRecordsKeyedByTheirRef()
    {
        var text = await WriteText(ExportFormat.Json, new ExportOptionsModel());
        var document = JsonDocument.Parse(text);
        var records = document.RootElement.EnumerateArray().ToList();

        records.Should().HaveCount(2);
        records[0].GetProperty("ref").GetString().Should().Be("task:acme-1");
        records[0].GetProperty("status").GetString().Should().Be("status:in-progress");
        records[0].GetProperty("tags").EnumerateArray().Select(tag => tag.GetString())
            .Should().Equal("tag:backend", "tag:urgent");
        records[1].GetProperty("due_date").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task NdjsonWriter_WritesOneJsonObjectPerLine()
    {
        var text = await WriteText(ExportFormat.Ndjson, new ExportOptionsModel());
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        lines.Should().HaveCount(2);
        JsonDocument.Parse(lines[0]).RootElement.GetProperty("ref").GetString().Should().Be("task:acme-1");
        JsonDocument.Parse(lines[1]).RootElement.GetProperty("ref").GetString().Should().Be("task:acme-2");
    }

    [Fact]
    public async Task XlsxWriter_WritesAHeaderRowAndTypedCells()
    {
        await using var output = new MemoryStream();

        await Write(ExportFormat.Xlsx, new ExportOptionsModel(), output);

        output.Seek(0, SeekOrigin.Begin);

        using var workbook = new XLWorkbook(output);
        var sheet = workbook.Worksheets.First();

        sheet.Name.Should().Be("Task");
        sheet.Cell(1, 1).GetString().Should().Be("System id");
        sheet.Cell(2, 1).GetString().Should().Be("acme-1");
        sheet.Cell(2, 4).GetDateTime().Should().Be(new DateTime(2026, 8, 18, 0, 0, 0, DateTimeKind.Unspecified));
        sheet.Cell(3, 4).IsEmpty().Should().BeTrue();
    }

    private static async Task<string> WriteText(ExportFormat format, ExportOptionsModel options)
    {
        await using var output = new MemoryStream();

        await Write(format, options, output);

        return Encoding.UTF8.GetString(output.ToArray());
    }

    private static async Task<long> WriteRowCount(ExportFormat format, ExportOptionsModel options)
    {
        await using var output = new MemoryStream();

        return await Write(format, options, output);
    }

    private static Task<long> Write(ExportFormat format, ExportOptionsModel options, Stream output)
    {
        var writer = Writers.Resolve(format);
        var request = new ExportWriteRequest
        {
            RecordTypeName = "Task",
            Fields = Fields(),
            Records = Records(),
            Options = options,
        };

        return writer.Write(request, output, TestContext.Current.CancellationToken);
    }

    private static IReadOnlyList<TransferField> Fields()
    {
        var keys = new[]
        {
            TaskFieldKeys.SystemId,
            TaskFieldKeys.Name,
            TaskFieldKeys.Status,
            TaskFieldKeys.DueDate,
            TaskFieldKeys.Tags,
        };

        return keys.Select(key => TransferFieldCatalog.FindField(key)!).ToList();
    }

    private static async IAsyncEnumerable<ExportRecord> Records()
    {
        yield return new ExportRecord
        {
            Ref = EntityRefBuilder.ForTask("acme", 1),
            Values = new Dictionary<string, object?>
            {
                [TaskFieldKeys.SystemId] = "acme-1",
                [TaskFieldKeys.Name] = "First task",
                [TaskFieldKeys.Status] = EntityRefBuilder.ForStatus("in-progress"),
                [TaskFieldKeys.DueDate] = new DateOnly(2026, 8, 18),
                [TaskFieldKeys.Tags] = new List<EntityRef>
                {
                    EntityRefBuilder.ForTag("backend"),
                    EntityRefBuilder.ForTag("urgent"),
                },
            },
        };

        yield return new ExportRecord
        {
            Ref = EntityRefBuilder.ForTask("acme", 2),
            Values = new Dictionary<string, object?>
            {
                [TaskFieldKeys.SystemId] = "acme-2",
                [TaskFieldKeys.Name] = "Second, task",
                [TaskFieldKeys.Status] = EntityRefBuilder.ForStatus("done"),
                [TaskFieldKeys.DueDate] = null,
                [TaskFieldKeys.Tags] = new List<EntityRef>(),
            },
        };

        await Task.CompletedTask;
    }
}
