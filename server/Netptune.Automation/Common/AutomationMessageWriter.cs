using Netptune.Core.Entities;
using Netptune.Core.Models.Automations;

namespace Netptune.Automation.Common;

internal static class AutomationMessageWriter
{
    public static void Write(AutomationRun run, AutomationMessage? message)
    {
        run.Message = message?.Text;
        run.MessageCode = message?.Code;
        run.MessageArguments = [.. message?.Arguments ?? []];
    }

    public static void Write(AutomationActionResult result, AutomationMessage? message)
    {
        result.Message = message?.Text;
        result.MessageCode = message?.Code;
        result.MessageArguments = [.. message?.Arguments ?? []];
    }
}
