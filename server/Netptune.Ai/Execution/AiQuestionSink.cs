using Netptune.Core.Models.Ai;
using Netptune.Core.Services.Ai;

namespace Netptune.Ai.Execution;

public sealed class AiQuestionSink : IAiQuestionSink
{
    public AiQuestion? Pending { get; private set; }

    public void Ask(AiQuestion question)
    {
        Pending = question;
    }
}
