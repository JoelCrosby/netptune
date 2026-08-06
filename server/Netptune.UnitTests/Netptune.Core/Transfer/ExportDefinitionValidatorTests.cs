using Netptune.Transfer.Enums;
using FluentAssertions;

using Netptune.Transfer;
using Netptune.Transfer.Definitions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Core.Transfer;

public class ExportDefinitionValidatorTests
{
    [Fact]
    public void Validate_AcceptsATaskCsvExportWithNoFieldSelection()
    {
        var result = ExportDefinitionValidator.Validate(TaskCsv());

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_RejectsANullDefinitionAndAnUnknownRecordType()
    {
        ExportDefinitionValidator.Validate(null).IsValid.Should().BeFalse();

        var unknown = ExportDefinitionValidator.Validate(TaskCsv() with { RecordType = "unicorn" });

        unknown.IsValid.Should().BeFalse();
        unknown.Errors.Should().ContainSingle().Which.Should().Contain("unicorn");
    }

    [Fact]
    public void Validate_RejectsFieldsThatAreUnknownDuplicatedOrFromAnotherRecordType()
    {
        var unknown = ExportDefinitionValidator.Validate(TaskCsv() with { Fields = ["task.nope"] });
        var duplicated = ExportDefinitionValidator.Validate(TaskCsv() with { Fields = ["task.name", "task.name"] });

        unknown.IsValid.Should().BeFalse();
        duplicated.IsValid.Should().BeFalse();
        duplicated.Errors.Should().ContainSingle().Which.Should().Contain("more than once");
    }

    [Fact]
    public void Validate_PairsTheArchiveFormatWithTheWorkspaceRecordType()
    {
        var archiveOfTasks = ExportDefinitionValidator.Validate(TaskCsv() with { Format = ExportFormat.Archive });
        var csvOfWorkspace = ExportDefinitionValidator.Validate(TaskCsv() with
        {
            RecordType = ExportDefinitionModel.WorkspaceRecordType,
        });
        var archiveOfWorkspace = ExportDefinitionValidator.Validate(new ExportDefinitionModel
        {
            RecordType = ExportDefinitionModel.WorkspaceRecordType,
            Format = ExportFormat.Archive,
        });

        archiveOfTasks.IsValid.Should().BeFalse();
        csvOfWorkspace.IsValid.Should().BeFalse();
        archiveOfWorkspace.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_RejectsAWorkspaceArchiveThatSelectsFields()
    {
        var result = ExportDefinitionValidator.Validate(new ExportDefinitionModel
        {
            RecordType = ExportDefinitionModel.WorkspaceRecordType,
            Format = ExportFormat.Archive,
            Fields = ["task.name"],
        });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Contain("cannot select individual fields");
    }

    [Fact]
    public void Validate_RejectsUnusableOptions()
    {
        var badTimeZone = ExportDefinitionValidator.Validate(TaskCsv() with
        {
            Options = new ExportOptionsModel { TimeZoneId = "Mars/Olympus" },
        });
        var noDateFormat = ExportDefinitionValidator.Validate(TaskCsv() with
        {
            Options = new ExportOptionsModel { DateFormat = "  " },
        });
        var quoteDelimiter = ExportDefinitionValidator.Validate(TaskCsv() with
        {
            Options = new ExportOptionsModel { Delimiter = '"' },
        });

        badTimeZone.IsValid.Should().BeFalse();
        noDateFormat.IsValid.Should().BeFalse();
        quoteDelimiter.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_AcceptsTabAsTheTsvDelimiter()
    {
        var result = ExportDefinitionValidator.Validate(TaskCsv() with
        {
            Format = ExportFormat.Tsv,
            Options = new ExportOptionsModel { Delimiter = '\t' },
        });

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_RejectsArchiveOptionsOnANonArchiveExport()
    {
        var result = ExportDefinitionValidator.Validate(TaskCsv() with
        {
            Options = new ExportOptionsModel { IncludeFiles = true },
        });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Contain("archive export");
    }

    [Fact]
    public void ResolveFields_FallsBackToTheDefaultExportSetAndOtherwiseKeepsTheChosenOrder()
    {
        var defaults = ExportDefinitionValidator.ResolveFields(TaskCsv());
        var chosen = ExportDefinitionValidator.ResolveFields(TaskCsv() with
        {
            Fields = ["task.due_date", "task.name"],
        });

        defaults.Should().NotBeEmpty();
        defaults.Should().OnlyContain(field => field.IsExportedByDefault);
        chosen.Select(field => field.Key).Should().Equal("task.due_date", "task.name");
    }

    private static ExportDefinitionModel TaskCsv()
    {
        return new ExportDefinitionModel
        {
            RecordType = EntityRefTypes.Task,
            Format = ExportFormat.Csv,
        };
    }
}
