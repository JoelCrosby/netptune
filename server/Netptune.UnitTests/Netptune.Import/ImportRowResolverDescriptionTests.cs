using FluentAssertions;

using Netptune.Core.Constants;
using Netptune.Core.Models.ProjectTasks;
using Netptune.Import;
using Netptune.Transfer;
using Netptune.Transfer.Enums;
using Netptune.Transfer.Mapping;

using Xunit;

namespace Netptune.UnitTests.Netptune.Import;

public class ImportRowResolverDescriptionTests
{
    private static readonly ImportRowResolver Resolver = new(new ImportMappingModel
    {
        RecordType = EntityRefTypes.Task,
        Bindings =
        [
            new ImportFieldBinding { FieldKey = TaskFieldKeys.Name, ColumnIndex = 0 },
            new ImportFieldBinding { FieldKey = TaskFieldKeys.Description, ColumnIndex = 1 },
        ],
    }, ["Name", "Description"]);

    [Fact]
    public void Resolve_ShouldNormalizeTheDescription()
    {
        var resolved = Resolver.Resolve(Row("first\r\nsecond\r\n"));

        resolved.Description.Should().Be("first\nsecond");
        resolved.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void Resolve_ShouldReportAnError_WhenTheDescriptionIsTooLong()
    {
        var resolved = Resolver.Resolve(Row(new string('a', ProjectTaskDescription.MaxLength + 1)));

        resolved.HasErrors.Should().BeTrue();
        resolved.Diagnostics.Should().ContainSingle(diagnostic => diagnostic.Code == ImportDiagnosticCodes.DescriptionTooLong);
    }

    private static ImportRow Row(string description)
    {
        return new ImportRow { RowNumber = 1, Values = ["Task", description] };
    }
}
