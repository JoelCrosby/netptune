using Netptune.Core.Models.Ai;

namespace Netptune.Core.Services.Ai;

public interface IAiQuestionSink
{
    AiQuestion? Pending { get; }

    void Ask(AiQuestion question);
}
