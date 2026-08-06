namespace Netptune.Export;

// An export artefact is written to a temp file rather than to memory. A whole-workspace archive
// carries every file blob in the workspace and a job export has no row cap, so neither fits in a
// MemoryStream. The file removes itself when the stream is disposed, which the caller does once the
// artefact has been uploaded or written to the response.
internal static class ExportSpool
{
    private const int BufferSize = 81920;

    public static FileStream Create()
    {
        return new FileStream(
            Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()),
            FileMode.Create,
            FileAccess.ReadWrite,
            FileShare.None,
            BufferSize,
            FileOptions.DeleteOnClose | FileOptions.Asynchronous);
    }
}
