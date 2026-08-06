using Netptune.Transfer.Enums;
using ClosedXML.Excel;

using Netptune.Transfer.Services;
using Netptune.Transfer.Export;

namespace Netptune.Export.Writers;

public sealed class XlsxExportWriter : IExportWriter
{
    private const int MaxSheetNameLength = 31;

    public ExportFormat Format => ExportFormat.Xlsx;

    public string ContentType => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public string FileExtension => "xlsx";

    public async Task<long> Write(ExportWriteRequest request, Stream output, CancellationToken cancellationToken = default)
    {
        var formatter = new ExportValueFormatter(request.Options);

        using var workbook = new XLWorkbook();

        var worksheet = workbook.AddWorksheet(SheetName(request.RecordTypeName));
        var row = 1;

        if (request.Options.IncludeHeaderRow)
        {
            for (var column = 0; column < request.Fields.Count; column++)
            {
                worksheet.Cell(row, column + 1).SetValue(request.Fields[column].Name);
            }

            worksheet.Row(row).Style.Font.Bold = true;
            row++;
        }

        var rowCount = 0L;

        await foreach (var record in request.Records.WithCancellation(cancellationToken))
        {
            for (var column = 0; column < request.Fields.Count; column++)
            {
                var field = request.Fields[column];
                var value = record.Values.GetValueOrDefault(field.Key);

                SetCell(worksheet.Cell(row, column + 1), value, formatter);
            }

            row++;
            rowCount++;
        }

        worksheet.Columns().AdjustToContents(1, 200);
        workbook.SaveAs(output);

        return rowCount;
    }

    private static void SetCell(IXLCell cell, object? value, ExportValueFormatter formatter)
    {
        switch (value)
        {
            case null:
                return;
            case decimal number:
                cell.SetValue(number);
                return;
            case int number:
                cell.SetValue(number);
                return;
            case bool flag:
                cell.SetValue(flag);
                return;
            case DateOnly date:
                cell.SetValue(date.ToDateTime(TimeOnly.MinValue));
                return;
            case DateTime dateTime:
                cell.SetValue(formatter.ToExportZone(dateTime));
                return;
            default:
                cell.SetValue(formatter.Format(value));
                return;
        }
    }

    private static string SheetName(string recordTypeName)
    {
        var invalid = new[] { '\\', '/', '*', '?', ':', '[', ']' };
        var cleaned = new string(recordTypeName.Where(character => !invalid.Contains(character)).ToArray()).Trim();

        if (cleaned.Length == 0)
        {
            return "Export";
        }

        if (cleaned.Length <= MaxSheetNameLength)
        {
            return cleaned;
        }

        return cleaned[..MaxSheetNameLength];
    }
}
