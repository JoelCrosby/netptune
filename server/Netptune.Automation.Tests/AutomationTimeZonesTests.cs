using FluentAssertions;

using Netptune.Automation.Matching;
using Netptune.Core.Entities;
using Netptune.Core.Meta;

using Xunit;

namespace Netptune.Automation.Tests;

public sealed class AutomationTimeZonesTests
{
    [Fact]
    public void Today_uses_workspace_timezone()
    {
        var rule = new AutomationRule
        {
            Workspace = new Workspace
            {
                MetaInfo = new WorkspaceMeta
                {
                    TimeZone = "Pacific/Kiritimati",
                },
            },
        };
        var utcNow = new DateTime(2026, 7, 24, 12, 0, 0, DateTimeKind.Utc);

        var today = AutomationTimeZones.Today(rule, utcNow);

        today.Should().Be(new DateOnly(2026, 7, 25));
    }
}
