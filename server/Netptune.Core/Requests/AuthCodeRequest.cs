namespace Netptune.Core.Requests;

public class AuthCodeRequest
{
    public required string userId { get; init; }

    public required string code { get; init; }
}
