using System.Reflection;

namespace Netptune.TestData;

// Real-shaped vendor exports checked in as golden files, so a scoring tweak that regresses vendor
// detection fails loudly instead of quietly.
public static class VendorSamples
{
    public const string Jira = "jira-issues.csv";
    public const string Asana = "asana-tasks.csv";
    public const string Trello = "trello-board.json";
    public const string Netptune = "netptune-tasks.csv";

    public static Stream Open(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .SingleOrDefault(candidate => candidate.EndsWith($".{name}", StringComparison.Ordinal));

        if (resourceName is null)
        {
            throw new InvalidOperationException($"The vendor sample '{name}' is not embedded in {assembly.GetName().Name}.");
        }

        var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"The vendor sample '{name}' could not be read.");
        var buffer = new MemoryStream();

        stream.CopyTo(buffer);
        stream.Dispose();
        buffer.Seek(0, SeekOrigin.Begin);

        return buffer;
    }
}
