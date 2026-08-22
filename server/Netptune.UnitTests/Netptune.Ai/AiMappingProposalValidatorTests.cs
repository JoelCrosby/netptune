using Netptune.Transfer.Enums;
using FluentAssertions;

using Netptune.Ai.Execution;
using Netptune.Transfer;
using Netptune.Transfer.Mapping;

using Xunit;
using Netptune.Core.Constants;

namespace Netptune.UnitTests.Netptune.Ai;

// The model is untrusted input. Everything here is output a real model could plausibly produce.
public class AiMappingProposalValidatorTests
{
    [Fact]
    public void Validate_KeepsABindingThatNamesARealFieldAndARealColumn()
    {
        var result = Validate(Binding(TaskFieldKeys.Name, 0));

        result.Mapping.Bindings.Should().ContainSingle()
            .Which.Should().Match<ImportFieldBinding>(binding =>
                binding.FieldKey == TaskFieldKeys.Name &&
                binding.ColumnIndex == 0 &&
                binding.Origin == ImportBindingOrigin.Assistant);
        result.DiscardedBindings.Should().Be(0);
    }

    [Fact]
    public void Validate_DropsAnInventedFieldKey()
    {
        var result = Validate(Binding("task.does_not_exist", 0));

        result.Mapping.Bindings.Should().BeEmpty();
        result.DiscardedBindings.Should().Be(1);
        result.DiscardReasons.Should().ContainSingle().Which.Should().Contain("does_not_exist");
    }

    [Fact]
    public void Validate_DropsAFieldBorrowedFromAnotherRecordType()
    {
        var result = Validate(Binding("project.key", 0));

        result.Mapping.Bindings.Should().BeEmpty();
        result.DiscardedBindings.Should().Be(1);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(99)]
    [InlineData(null)]
    public void Validate_DropsAColumnIndexTheFileDoesNotHave(int? columnIndex)
    {
        var result = Validate(Binding(TaskFieldKeys.Name, columnIndex));

        result.Mapping.Bindings.Should().BeEmpty();
        result.DiscardedBindings.Should().Be(1);
    }

    [Fact]
    public void Validate_KeepsOnlyTheFirstOfADuplicatedFieldOrColumn()
    {
        var duplicateField = Validate(Binding(TaskFieldKeys.Name, 0), Binding(TaskFieldKeys.Name, 1));
        var duplicateColumn = Validate(Binding(TaskFieldKeys.Name, 0), Binding(TaskFieldKeys.Description, 0));

        duplicateField.Mapping.Bindings.Should().ContainSingle().Which.ColumnIndex.Should().Be(0);
        duplicateField.DiscardedBindings.Should().Be(1);
        duplicateColumn.Mapping.Bindings.Should().ContainSingle().Which.FieldKey.Should().Be(TaskFieldKeys.Name);
        duplicateColumn.DiscardedBindings.Should().Be(1);
    }

    [Fact]
    public void Validate_DropsAnInjectedTransformButKeepsTheBinding()
    {
        var binding = Binding(TaskFieldKeys.Name, 0) with
        {
            Transforms =
            [
                new AiMappingProposalTransform { Kind = "Trim" },
                new AiMappingProposalTransform { Kind = "DropDatabase" },
            ],
        };
        var result = Validate(binding);

        result.Mapping.Bindings.Should().ContainSingle()
            .Which.Transforms.Should().ContainSingle()
            .Which.Kind.Should().Be(ImportTransformKind.Trim);
        result.DiscardReasons.Should().ContainSingle().Which.Should().Contain("DropDatabase");
    }

    [Fact]
    public void Validate_DropsAValueMapTargetTheWorkspaceDoesNotHave()
    {
        var vocabulary = new ImportSuggestionVocabulary
        {
            StatusKeysByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["In Progress"] = "in-progress",
            },
        };
        var binding = Binding(TaskFieldKeys.Status, 0) with
        {
            ValueMap = new Dictionary<string, string>
            {
                ["In Progress"] = "in-progress",
                ["Blocked"] = "a-status-that-does-not-exist",
            },
        };
        var result = Validate(vocabulary, binding);

        result.Mapping.Bindings.Should().ContainSingle()
            .Which.ValueMap.Should().ContainSingle()
            .Which.Value.Should().Be("in-progress");
        result.DiscardReasons.Should().ContainSingle().Which.Should().Contain("a-status-that-does-not-exist");
    }

    [Fact]
    public void Validate_ClampsAConfidenceOutsideTheUnitRange()
    {
        var result = Validate(Binding(TaskFieldKeys.Name, 0) with { Confidence = 42 });

        result.Mapping.Bindings.Should().ContainSingle().Which.Confidence.Should().Be(1);
    }

    [Fact]
    public void Validate_ReturnsAnEmptyMappingForNullOrUnknownRecordType()
    {
        var nullProposal = AiMappingProposalValidator.Validate(null, TransferRecordTypes.Task, Profile());
        var unknownRecordType = AiMappingProposalValidator.Validate(new AiMappingProposal(), "unicorn", Profile());

        nullProposal.Mapping.Bindings.Should().BeEmpty();
        unknownRecordType.Mapping.Bindings.Should().BeEmpty();
    }

    [Theory]
    [InlineData("```json\n{\"bindings\":[]}\n```")]
    [InlineData("Here is the mapping you asked for: {\"bindings\":[]} Hope that helps!")]
    [InlineData("{\"bindings\":[]}")]
    public void Parse_RecoversJsonEvenWhenTheModelWrapsIt(string raw)
    {
        AiImportMappingAdvisor.Parse(raw).Should().NotBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("I could not work out a mapping.")]
    [InlineData("{ this is not json ]")]
    public void Parse_ReturnsNullWhenThereIsNoUsableJson(string raw)
    {
        AiImportMappingAdvisor.Parse(raw).Should().BeNull();
    }

    private static AiMappingValidationResult Validate(params AiMappingProposalBinding[] bindings)
    {
        return Validate(null, bindings);
    }

    private static AiMappingValidationResult Validate(
        ImportSuggestionVocabulary? vocabulary,
        params AiMappingProposalBinding[] bindings)
    {
        var proposal = new AiMappingProposal { Bindings = bindings.ToList() };

        return AiMappingProposalValidator.Validate(proposal, TransferRecordTypes.Task, Profile(), vocabulary);
    }

    private static AiMappingProposalBinding Binding(string fieldKey, int? columnIndex)
    {
        return new AiMappingProposalBinding
        {
            FieldKey = fieldKey,
            ColumnIndex = columnIndex,
            Confidence = 0.9,
        };
    }

    private static ImportSourceProfile Profile()
    {
        return new ImportSourceProfile
        {
            Kind = ImportSourceKind.Csv,
            HasHeaderRow = true,
            Columns =
            [
                new ImportSourceColumn { Index = 0, Name = "Summary", InferredType = TransferValueType.Text },
                new ImportSourceColumn { Index = 1, Name = "Notes", InferredType = TransferValueType.Text },
            ],
        };
    }
}
