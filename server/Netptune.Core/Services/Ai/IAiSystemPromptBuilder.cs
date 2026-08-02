namespace Netptune.Core.Services.Ai;

public interface IAiSystemPromptBuilder
{
    Task<string> Build(string? locale, CancellationToken cancellationToken);
}
