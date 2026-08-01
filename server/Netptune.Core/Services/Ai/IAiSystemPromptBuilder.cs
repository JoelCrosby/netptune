namespace Netptune.Core.Services.Ai;

public interface IAiSystemPromptBuilder
{
    Task<string> Build(CancellationToken cancellationToken);
}
