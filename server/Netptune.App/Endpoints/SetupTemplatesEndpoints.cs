using Netptune.Core.Onboarding.Templates;

namespace Netptune.App.Endpoints;

public static class SetupTemplatesEndpoints
{
    public static RouteGroupBuilder MapSetupTemplatesEndpoints(this RouteGroupBuilder builder)
    {
        builder.MapGet("setup-templates", HandleGet)
            .RequireAuthorization();

        return builder;
    }

    private static IResult HandleGet()
    {
        var templates = WorkspaceSetupTemplateCatalog.All
            .Select(WorkspaceSetupTemplateCatalog.ToViewModel)
            .ToList();

        return Results.Ok(templates);
    }
}
