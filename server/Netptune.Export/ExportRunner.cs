using Netptune.Transfer.Services;
using Netptune.Transfer;
using Netptune.Transfer.Catalog;
using Netptune.Transfer.Export;

using Netptune.Core.Requests;

using Netptune.Core.Responses.Common;

namespace Netptune.Export;

public sealed class ExportRunner : IExportRunner
{
    private const int ProgressStride = 5000;

    private readonly IEnumerable<IExportRecordSource> Sources;
    private readonly IExportWriterFactory Writers;
    private readonly IArchiveExporter Archives;

    public ExportRunner(IEnumerable<IExportRecordSource> sources, IExportWriterFactory writers, IArchiveExporter archives)
    {
        Sources = sources;
        Writers = writers;
        Archives = archives;
    }

    public async Task<ExportRunResult> Run(ExportRunRequest request, ExportProgressReporter reportProgress, CancellationToken cancellationToken = default)
    {
        var definition = request.Definition;
        var validation = ExportDefinitionValidator.Validate(definition);

        if (!validation.IsValid)
        {
            throw new NotSupportedException(string.Join(" ", validation.Errors));
        }

        if (definition.Format == Transfer.Enums.ExportFormat.Archive)
        {
            var archiveRequest = new ArchiveExportRequest
            {
                WorkspaceId = request.WorkspaceId,
                WorkspaceSlug = request.WorkspaceSlug,
                Options = definition.Options,
            };

            return await Archives.Write(archiveRequest, reportProgress, cancellationToken);
        }

        var source = ResolveSource(definition.RecordType);
        var writer = Writers.Resolve(definition.Format);
        var recordType = TransferFieldCatalog.FindRecordType(definition.RecordType);
        var fields = source.ResolveFields(definition);

        if (fields.Count == 0)
        {
            throw new NotSupportedException("The export definition selects no fields.");
        }

        var query = new ExportRecordQuery
        {
            WorkspaceId = request.WorkspaceId,
            WorkspaceSlug = request.WorkspaceSlug,
            Definition = definition,
            MaxRecords = request.MaxRecords,
        };
        var estimate = await source.EstimateCount(query, cancellationToken);

        await reportProgress(new ExportRunProgress { Percent = 5, Message = $"Reading {estimate} records" }, cancellationToken);

        var content = ExportSpool.Create();

        try
        {
            var records = ReportingRecords(source.Read(query, cancellationToken), estimate, reportProgress, cancellationToken);
            var writeRequest = new ExportWriteRequest
            {
                RecordTypeName = recordType?.Name ?? definition.RecordType,
                Fields = fields,
                Records = records,
                Options = definition.Options,
            };
            var rowCount = await writer.Write(writeRequest, content, cancellationToken);

            await reportProgress(new ExportRunProgress { Percent = 90, Message = "Uploading artefact" }, cancellationToken);

            content.Seek(0, SeekOrigin.Begin);

            return new ExportRunResult
            {
                Content = content,
                ContentType = writer.ContentType,
                FileName = BuildFileName(request.WorkspaceSlug, definition, writer.FileExtension),
                RowCount = rowCount,
            };
        }
        catch
        {
            // The caller only takes ownership of the spool file on a successful return.
            await content.DisposeAsync();

            throw;
        }
    }

    public async Task<ExportPreviewResult> Preview(ExportRunRequest request, int sampleSize, CancellationToken cancellationToken = default)
    {
        var definition = request.Definition;
        var validation = ExportDefinitionValidator.Validate(definition);

        if (!validation.IsValid)
        {
            throw new NotSupportedException(string.Join(" ", validation.Errors));
        }

        if (definition.Format == Transfer.Enums.ExportFormat.Archive)
        {
            return await PreviewArchive(request, cancellationToken);
        }

        var source = ResolveSource(definition.RecordType);
        var fields = source.ResolveFields(definition);
        var formatter = new ExportValueFormatter(definition.Options);
        var countQuery = new ExportRecordQuery
        {
            WorkspaceId = request.WorkspaceId,
            WorkspaceSlug = request.WorkspaceSlug,
            Definition = definition,
        };
        var estimate = await source.EstimateCount(countQuery, cancellationToken);
        var sampleQuery = countQuery with { MaxRecords = sampleSize };
        var rows = new List<IReadOnlyList<string>>(sampleSize);

        await foreach (var record in source.Read(sampleQuery, cancellationToken))
        {
            rows.Add(fields.Select(field => formatter.Format(record.Values.GetValueOrDefault(field.Key))).ToList());
        }

        return new ExportPreviewResult
        {
            FieldKeys = fields.Select(field => field.Key).ToList(),
            Headers = fields.Select(field => field.Name).ToList(),
            Rows = rows,
            EstimatedRowCount = estimate,
            CanRunInline = definition.Format != Transfer.Enums.ExportFormat.Archive && estimate <= request.InlineRowLimit,
        };
    }

    public async Task<PagedResponse<ExportPreviewRow>> PreviewRows(
        ExportRunRequest request,
        Pagination pagination,
        CancellationToken cancellationToken = default)
    {
        var definition = request.Definition;
        var validation = ExportDefinitionValidator.Validate(definition);

        if (!validation.IsValid)
        {
            throw new NotSupportedException(string.Join(" ", validation.Errors));
        }

        // An archive has no tabular shape to page through.
        if (definition.Format == Transfer.Enums.ExportFormat.Archive)
        {
            return new PagedResponse<ExportPreviewRow>([], pagination.Page, pagination.PageSize, 0);
        }

        var source = ResolveSource(definition.RecordType);
        var fields = source.ResolveFields(definition);
        var formatter = new ExportValueFormatter(definition.Options);
        var query = new ExportRecordQuery
        {
            WorkspaceId = request.WorkspaceId,
            WorkspaceSlug = request.WorkspaceSlug,
            Definition = definition,
        };
        var estimate = await source.EstimateCount(query, cancellationToken);
        var skip = pagination.Skip;

        // The source streams in key order, so take the page's worth after skipping past earlier pages
        // rather than materialising the whole export.
        var pageQuery = query with { MaxRecords = skip + pagination.PageSize };
        var rows = new List<ExportPreviewRow>(pagination.PageSize);
        var seen = 0;

        await foreach (var record in source.Read(pageQuery, cancellationToken))
        {
            if (seen++ < skip)
            {
                continue;
            }

            rows.Add(new ExportPreviewRow
            {
                Ref = record.Ref.ToString(),
                Values = fields.ToDictionary(
                    field => field.Key,
                    field => formatter.Format(record.Values.GetValueOrDefault(field.Key)),
                    StringComparer.OrdinalIgnoreCase),
            });

            if (rows.Count == pagination.PageSize)
            {
                break;
            }
        }

        return new PagedResponse<ExportPreviewRow>(rows, pagination.Page, pagination.PageSize, (int)Math.Min(estimate, int.MaxValue));
    }

    private static async IAsyncEnumerable<ExportRecord> ReportingRecords(
        IAsyncEnumerable<ExportRecord> records,
        long estimate,
        ExportProgressReporter reportProgress,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var read = 0L;

        await foreach (var record in records.WithCancellation(cancellationToken))
        {
            yield return record;

            read++;

            if (read % ProgressStride != 0)
            {
                continue;
            }

            var percent = estimate == 0 ? 50 : (int)Math.Clamp(10 + read * 80 / estimate, 10, 89);

            await reportProgress(new ExportRunProgress { Percent = percent, Message = $"Written {read} of {estimate}" }, cancellationToken);
        }
    }

    private async Task<ExportPreviewResult> PreviewArchive(ExportRunRequest request, CancellationToken cancellationToken)
    {
        var fileBytes = await Archives.EstimateFileBytes(request.WorkspaceId, cancellationToken);

        return new ExportPreviewResult
        {
            FieldKeys = ArchiveCatalog.InDependencyOrder.Select(definition => definition.Key).ToList(),
            Headers = ArchiveCatalog.InDependencyOrder.Select(definition => definition.RecordType.Name).ToList(),
            Rows = [],
            EstimatedRowCount = 0,
            CanRunInline = false,
            ArchiveFileBytes = fileBytes,
        };
    }

    private IExportRecordSource ResolveSource(string recordType)
    {
        var source = Sources.FirstOrDefault(candidate => candidate.CanRead(recordType));

        if (source is null)
        {
            throw new NotSupportedException($"Export record type '{recordType}' is not supported yet.");
        }

        return source;
    }

    private static string BuildFileName(string workspaceSlug, ExportDefinitionModel definition, string extension)
    {
        var timestamp = DateTime.UtcNow.ToString("yy-MMM-dd-HH-mm");
        var boardIdentifier = definition.Filter?.BoardIdentifiers.FirstOrDefault();
        var scope = boardIdentifier is null ? workspaceSlug : $"{workspaceSlug}-{boardIdentifier}";

        return $"Netptune-{definition.RecordType}-Export_{scope}-{timestamp}.{extension}";
    }
}
