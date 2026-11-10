namespace Netptune.SourceControl.Providers.AzureDevOps;

internal sealed record AzureDevOpsCredential
{
    public required string Organisation { get; init; }

    public required string Token { get; init; }

    public AzureDevOpsRequest CreateRequest(string path)
    {
        return new AzureDevOpsRequest
        {
            Organisation = Organisation,
            Path = path,
            Token = Token,
        };
    }

    // A record prints every property, and the token must never reach a log.
    public override string ToString()
    {
        return $"{nameof(AzureDevOpsCredential)} {{ {nameof(Organisation)} = {Organisation} }}";
    }
}
