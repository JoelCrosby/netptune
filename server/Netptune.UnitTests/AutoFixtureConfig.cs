using AutoFixture.Dsl;

using Netptune.Core.BaseEntities;

namespace Netptune.UnitTests;

public static class AutoFixtureConfig
{
    public static IPostprocessComposer<T> WithoutAuditable<T>(this IPostprocessComposer<T> fixture)
        where T : IAuditableEntity
    {
        return fixture
            .Without(p => p.CreatedByUser)
            .Without(p => p.ModifiedByUser)
            .Without(p => p.DeletedByUser)
            .Without(p => p.Owner);
    }

    public static IPostprocessComposer<T> WithoutWorkspace<T>(this IPostprocessComposer<T> fixture)
        where T : IWorkspaceEntity
    {
        return fixture
            .Without(p => p.Workspace)
            .WithoutAuditable();
    }
}
