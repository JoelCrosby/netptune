using Netptune.Transfer.Repositories;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Services;
using Netptune.Transfer.Services;
using Netptune.Transfer;
using Netptune.Transfer.Archive;
using Netptune.Transfer.Catalog;
using Netptune.Transfer.Records;

namespace Netptune.Export;

public sealed class ArchiveExporter : IArchiveExporter
{
    private const string FileEntryPrefix = "files/";

    private readonly IArchiveRepository Archives;
    private readonly IStorageService Storage;

    public ArchiveExporter(IStorageService storage, IArchiveRepository archives)
    {
        Storage = storage;
        Archives = archives;
    }

    public Task<long> EstimateFileBytes(int workspaceId, CancellationToken cancellationToken = default)
    {
        return Archives.GetFileBytes(workspaceId, cancellationToken);
    }

    public async Task<ExportRunResult> Write(
        ArchiveExportRequest request,
        ExportProgressReporter reportProgress,
        CancellationToken cancellationToken = default)
    {
        var workspace = await Archives.GetWorkspace(request.WorkspaceId, cancellationToken)
            ?? throw new InvalidOperationException("The workspace could not be resolved.");

        var content = ExportSpool.Create();

        try
        {
            var contents = new List<ArchiveContent>();
            var fileBytes = 0L;
            var recordCount = 0L;

            using (var zip = new ZipArchive(content, ZipArchiveMode.Create, leaveOpen: true))
            {
                var sections = BuildSections(request, workspace);
                var sectionIndex = 0;

                foreach (var section in sections)
                {
                    var percent = Math.Clamp(5 + sectionIndex * 70 / Math.Max(sections.Count, 1), 5, 75);

                    await reportProgress(new ExportRunProgress { Percent = percent, Message = $"Writing {section.Key}" }, cancellationToken);

                    var written = await WriteSection(zip, section, cancellationToken);

                    contents.Add(written);
                    recordCount += written.Count;
                    sectionIndex++;
                }

                if (request.Options.IncludeFiles)
                {
                    await reportProgress(new ExportRunProgress { Percent = 80, Message = "Adding files" }, cancellationToken);

                    fileBytes = await WriteFileBlobs(zip, request.WorkspaceId, cancellationToken);
                }

                var manifest = new ArchiveManifest
                {
                    CreatedAt = DateTime.UtcNow,
                    Workspace = new ArchiveWorkspace { Slug = workspace.Slug, Name = workspace.Name },
                    Scope = new ArchiveScope
                    {
                        IncludeHistory = request.Options.IncludeHistory,
                        IncludeFiles = request.Options.IncludeFiles,
                        IncludeMembers = request.Options.IncludeMembers,
                    },
                    Contents = contents,
                    Redactions = TransferRedaction.RedactionKeys,
                    FileBytes = fileBytes,
                };

                await WriteManifest(zip, manifest, cancellationToken);
            }

            content.Seek(0, SeekOrigin.Begin);

            return new ExportRunResult
            {
                Content = content,
                ContentType = "application/zip",
                FileName = $"Netptune-Workspace_{workspace.Slug}-{DateTime.UtcNow:yy-MMM-dd-HH-mm}.nptz",
                RowCount = recordCount,
            };
        }
        catch
        {
            // The caller only takes ownership of the spool file on a successful return.
            await content.DisposeAsync();

            throw;
        }
    }

    private IReadOnlyList<ArchiveSection> BuildSections(ArchiveExportRequest request, Workspace workspace)
    {
        var workspaceId = request.WorkspaceId;
        var sections = new List<ArchiveSection>
        {
            Section(ArchiveCatalog.Workspace, One(workspace)),
        };

        if (request.Options.IncludeMembers)
        {
            sections.Add(Section(ArchiveCatalog.Member, Archives.ReadMembers(workspaceId)));
        }

        sections.Add(Section(ArchiveCatalog.Status, Archives.ReadStatuses(workspaceId)));
        sections.Add(Section(ArchiveCatalog.Tag, Archives.ReadTags(workspaceId)));
        sections.Add(Section(ArchiveCatalog.RelationType, Archives.ReadRelationTypes(workspaceId)));
        sections.Add(Section(ArchiveCatalog.Project, Archives.ReadProjects(workspaceId)));
        sections.Add(Section(ArchiveCatalog.Board, Archives.ReadBoards(workspaceId)));
        sections.Add(Section(ArchiveCatalog.BoardGroup, Archives.ReadBoardGroups(workspaceId)));
        sections.Add(Section(ArchiveCatalog.Sprint, Archives.ReadSprints(workspaceId)));
        sections.Add(Section(ArchiveCatalog.Task, Archives.ReadTasks(workspaceId)));
        sections.Add(Section(ArchiveCatalog.TaskAssignee, Archives.ReadTaskAssignees(workspaceId)));
        sections.Add(Section(ArchiveCatalog.TaskTag, Archives.ReadTaskTags(workspaceId)));
        sections.Add(Section(ArchiveCatalog.TaskPlacement, Archives.ReadTaskPlacements(workspaceId)));
        sections.Add(Section(ArchiveCatalog.TaskRelation, Archives.ReadTaskRelations(workspaceId)));
        sections.Add(Section(ArchiveCatalog.Comment, Archives.ReadComments(workspaceId)));
        sections.Add(Section(ArchiveCatalog.Reaction, Archives.ReadReactions(workspaceId)));
        sections.Add(Section(ArchiveCatalog.Flag, Archives.ReadFlags(workspaceId)));
        sections.Add(Section(ArchiveCatalog.Automation, Archives.ReadAutomations(workspaceId)));
        sections.Add(Section(ArchiveCatalog.WorkspaceFile, Archives.ReadFiles(workspaceId)));

        if (request.Options.IncludeHistory)
        {
            sections.Add(Section(ArchiveCatalog.Event, Archives.ReadEvents(workspaceId)));
        }

        return sections;
    }

    private static async IAsyncEnumerable<TEntity> One<TEntity>(TEntity entity)
    {
        yield return entity;

        await Task.CompletedTask;
    }

    private static ArchiveSection Section<TEntity>(ArchiveRecordDefinition<TEntity> definition, IAsyncEnumerable<TEntity> entities)
    {
        return new ArchiveSection
        {
            Key = definition.Key,
            FileName = definition.FileName,
            Records = entities.Select(definition.ToRecord),
        };
    }

    private static async Task<ArchiveContent> WriteSection(ZipArchive zip, ArchiveSection section, CancellationToken cancellationToken)
    {
        var entry = zip.CreateEntry(section.FileName, CompressionLevel.Optimal);
        var count = 0L;

        await using var entryStream = entry.Open();
        using var hash = SHA256.Create();

        await using (var digest = new CryptoStream(entryStream, hash, CryptoStreamMode.Write, leaveOpen: true))
        {
            await using var writer = new StreamWriter(digest, new System.Text.UTF8Encoding(false), leaveOpen: true);

            await foreach (var record in section.Records.WithCancellation(cancellationToken))
            {
                var line = JsonSerializer.Serialize(ToDocument(record), JsonOptions.Default);

                await writer.WriteLineAsync(line.AsMemory(), cancellationToken);
                count++;
            }

            await writer.FlushAsync(cancellationToken);
        }

        return new ArchiveContent
        {
            Type = section.Key,
            File = section.FileName,
            Count = count,
            Sha256 = Convert.ToHexStringLower(hash.Hash ?? []),
        };
    }

    private async Task<long> WriteFileBlobs(ZipArchive zip, int workspaceId, CancellationToken cancellationToken)
    {
        var totalBytes = 0L;

        await foreach (var file in Archives.ReadFiles(workspaceId).WithCancellation(cancellationToken))
        {
            var blob = await Storage.OpenReadAsync(file.StorageKey, cancellationToken);

            if (blob is null)
            {
                continue;
            }

            var entry = zip.CreateEntry($"{FileEntryPrefix}{file.ContentId}/{file.OriginalName}", CompressionLevel.Optimal);

            await using var entryStream = entry.Open();

            totalBytes += await CopyBlob(blob, entryStream, cancellationToken);
        }

        return totalBytes;
    }

    private static async Task<long> CopyBlob(Stream blob, Stream destination, CancellationToken cancellationToken)
    {
        await using (blob)
        {
            var counting = new CountingStream(destination);

            await blob.CopyToAsync(counting, cancellationToken);

            return counting.BytesWritten;
        }
    }

    private static async Task WriteManifest(ZipArchive zip, ArchiveManifest manifest, CancellationToken cancellationToken)
    {
        var entry = zip.CreateEntry(ArchiveManifest.FileName, CompressionLevel.Optimal);

        await using var entryStream = entry.Open();

        await JsonSerializer.SerializeAsync(entryStream, manifest, JsonOptions.Default, cancellationToken);
    }

    private static Dictionary<string, object?> ToDocument(ExportRecord record)
    {
        var document = new Dictionary<string, object?>(record.Values.Count + 1)
        {
            ["ref"] = record.Ref.ToString(),
        };

        foreach (var value in record.Values)
        {
            document[PropertyName(value.Key)] = Normalize(value.Value);
        }

        return document;
    }

    private static object? Normalize(object? value)
    {
        return value switch
        {
            EntityRef entityRef => entityRef.ToString(),
            IEnumerable<EntityRef> refs => refs.Select(item => item.ToString()).ToList(),
            IEnumerable<AutomationAction> actions => actions.Select(ToActionDocument).ToList(),
            Enum enumValue => enumValue.ToString(),
            _ => value,
        };
    }

    private static Dictionary<string, object?> ToActionDocument(AutomationAction action)
    {
        return new Dictionary<string, object?>
        {
            ["type"] = action.Type.ToString(),
            ["sortOrder"] = action.SortOrder,
            ["config"] = action.Config,
        };
    }

    private static string PropertyName(string fieldKey)
    {
        var separatorIndex = fieldKey.IndexOf('.');

        if (separatorIndex < 0)
        {
            return fieldKey;
        }

        return fieldKey[(separatorIndex + 1)..];
    }

    private sealed record ArchiveSection
    {
        public required string Key { get; init; }

        public required string FileName { get; init; }

        public required IAsyncEnumerable<ExportRecord> Records { get; init; }
    }

    private sealed class CountingStream(Stream inner) : Stream
    {
        public long BytesWritten { get; private set; }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => BytesWritten;

        public override long Position
        {
            get => BytesWritten;
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
            inner.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            inner.Write(buffer, offset, count);
            BytesWritten += count;
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await inner.WriteAsync(buffer, cancellationToken);

            BytesWritten += buffer.Length;
        }
    }
}
