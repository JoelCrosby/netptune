namespace Netptune.Transfer;

public sealed class TransferOptions
{
    public const string SectionName = "Transfer";

    public int MaxConcurrentJobsPerWorkspace { get; set; } = 2;

    public int ExportArtefactRetentionDays { get; set; } = 7;

    public int DownloadUriLifetimeMinutes { get; set; } = 10;

    public int InlineRowLimit { get; set; } = 10_000;

    public int PreviewSampleSize { get; set; } = 20;

    public long UploadSizeBytes { get; set; } = 50L * 1024 * 1024;

    public int SessionRetentionDays { get; set; } = 7;

    public int MaxRowsPerImport { get; set; } = 250_000;

    public int PreviewRowCap { get; set; } = 5_000;
}
