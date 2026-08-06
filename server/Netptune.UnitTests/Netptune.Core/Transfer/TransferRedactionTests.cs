using FluentAssertions;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Transfer;
using Netptune.Entities.Contexts;

using Xunit;

namespace Netptune.UnitTests.Netptune.Core.Transfer;

public class TransferRedactionTests
{
    private static readonly IReadOnlyList<Type> PersistedEntityTypes = typeof(DataContext)
        .GetProperties()
        .Where(property => property.PropertyType.IsGenericType)
        .Where(property => property.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
        .Select(property => property.PropertyType.GetGenericArguments()[0])
        .Distinct()
        .ToList();

    [Fact]
    public void EveryPersistedEntityType_IsExplicitlyClassified()
    {
        PersistedEntityTypes.Should().NotBeEmpty();

        var unclassified = PersistedEntityTypes
            .Where(entityType => !TransferRedaction.IsClassified(entityType))
            .Select(entityType => entityType.Name)
            .ToList();

        unclassified.Should().BeEmpty(
            "every persisted entity must be explicitly exported, reference-only or redacted in TransferRedaction");
    }

    [Fact]
    public void EveryClassification_TargetsAPersistedEntityType()
    {
        var persisted = PersistedEntityTypes.ToHashSet();
        var stale = TransferRedaction.All
            .Where(classification => !persisted.Contains(classification.EntityType))
            .Select(classification => classification.EntityType.Name)
            .ToList();

        stale.Should().BeEmpty("TransferRedaction must not classify types the data context no longer persists");
    }

    [Fact]
    public void Classifications_AreUniquePerEntityType()
    {
        TransferRedaction.All.Select(classification => classification.EntityType).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void ExportedTypes_CarryNoRedactionKeyAndWithheldTypesDo()
    {
        foreach (var classification in TransferRedaction.All)
        {
            var isExported = classification.Disposition == TransferEntityDisposition.Exported;

            if (isExported)
            {
                classification.RedactionKey.Should().BeNull();
                continue;
            }

            classification.RedactionKey.Should().NotBeNull();
            TransferRedactionKeys.All.Should().Contain(classification.RedactionKey!);
        }
    }

    [Fact]
    public void CredentialAndAccountEntities_AreNeverExported()
    {
        var withheld = new[]
        {
            "UserAiCredential",
            "WorkspaceAiCredential",
            "ApiCredential",
            "RefreshToken",
            "Notification",
            "UserPreferenceValue",
            "CommandPaletteRecentItem",
            "AutomationRun",
            "AutomationActionResult",
            "ScheduledAutomationAction",
        };

        foreach (var name in withheld)
        {
            var entityType = PersistedEntityTypes.SingleOrDefault(type => type.Name == name);

            entityType.Should().NotBeNull($"{name} should still be a persisted entity");
            TransferRedaction.IsExportable(entityType).Should().BeFalse();
        }
    }

    [Fact]
    public void AppUser_IsExportedOnlyAsAnIdentityReference()
    {
        var classification = TransferRedaction.Classify(typeof(AppUser));

        classification.Should().NotBeNull();
        classification.Disposition.Should().Be(TransferEntityDisposition.ReferenceOnly);
        classification.RedactionKey.Should().Be(TransferRedactionKeys.UserAccounts);
    }

    [Fact]
    public void RedactionKeys_AreDistinctSortedAndKnown()
    {
        TransferRedaction.RedactionKeys.Should().NotBeEmpty();
        TransferRedaction.RedactionKeys.Should().OnlyHaveUniqueItems();
        TransferRedaction.RedactionKeys.Should().BeInAscendingOrder(StringComparer.Ordinal);
        TransferRedaction.RedactionKeys.Should().BeSubsetOf(TransferRedactionKeys.All);
    }
}
