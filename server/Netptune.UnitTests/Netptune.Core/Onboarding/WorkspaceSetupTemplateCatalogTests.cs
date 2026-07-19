using FluentAssertions;

using Netptune.Core.Enums;
using Netptune.Core.Onboarding.Templates;
using Netptune.Core.Tags;

using Xunit;

namespace Netptune.UnitTests.Netptune.Core.Onboarding;

public class WorkspaceSetupTemplateCatalogTests
{
    [Fact]
    public void Catalog_HasValidUniqueTemplateKeys()
    {
        var templates = WorkspaceSetupTemplateCatalog.All;

        templates.Select(template => template.Key)
            .Should().OnlyHaveUniqueItems()
            .And.OnlyContain(key =>
                !string.IsNullOrWhiteSpace(key) &&
                key == key.ToLowerInvariant() &&
                key.All(character => char.IsLower(character) || char.IsDigit(character) || character == '-'));
    }

    [Fact]
    public void EveryTemplate_HasValidDefaultsAndBoardBindings()
    {
        foreach (var template in WorkspaceSetupTemplateCatalog.All)
        {
            template.Name.Should().NotBeNullOrWhiteSpace();
            template.Statuses.Select(status => status.Key).Should().OnlyHaveUniqueItems();
            template.RelationTypes.Select(relation => relation.Key).Should().OnlyHaveUniqueItems();
            template.BoardGroups.Select(group => group.Name).Should().OnlyHaveUniqueItems();
            template.BoardGroups.Should().OnlyContain(group => !string.IsNullOrWhiteSpace(group.Name));

            template.Statuses.Should().ContainSingle(status =>
                status.Key == WorkspaceSetupTemplateCatalog.NewStatusKey &&
                status.Category == StatusCategory.Todo);

            var statusKeys = template.Statuses.Select(status => status.Key).ToHashSet();
            template.BoardGroups
                .Where(group => group.StatusKey is not null)
                .Should().OnlyContain(group => statusKeys.Contains(group.StatusKey!));

            var normalizedTags = template.Tags.Select(TagNames.Normalize).ToList();
            normalizedTags.Should().Equal(template.Tags);
            normalizedTags.Should().OnlyHaveUniqueItems();
        }
    }

    [Fact]
    public void RecommendedTemplates_HaveBoardGroups()
    {
        WorkspaceSetupTemplateCatalog.All
            .Where(template => template.IsRecommended)
            .Should().NotBeEmpty()
            .And.OnlyContain(template => template.BoardGroups.Count > 0);
    }

    [Fact]
    public void BasicCompatibilityDefaults_AreStable()
    {
        var basic = WorkspaceSetupTemplateCatalog.Find(null);

        basic.Should().NotBeNull();
        basic.Key.Should().Be(WorkspaceSetupTemplateCatalog.DefaultKey);
        basic.BoardGroups.Select(group => group.Name)
            .Should().Equal("Backlog", "Todo", "Done");
    }
}
