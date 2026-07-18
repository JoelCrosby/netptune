using Microsoft.OpenApi;

namespace Netptune.PublicApi.Configuration;

public static class PublicApiOpenApi
{
    public const string SecuritySchemeName = "ApiKey";

    public static IServiceCollection AddPublicApiOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi("v1", options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = "Netptune Public API",
                    Version = "v1",
                    Description = """
                        Use Netptune's public API to query workspace data and automate task management.

                        ## Authentication

                        Create a user-owned service account and credential in Netptune, then send the credential with every request:

                        `Authorization: ApiKey ntp_<credential-id>_<secret>`

                        When using **Try it** below, enter the complete value including the `ApiKey ` prefix.

                        Credentials are restricted to one workspace and can only use permissions granted to both the service account and credential.
                        """,
                };

                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
                {
                    [SecuritySchemeName] = new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.ApiKey,
                        Name = "Authorization",
                        In = ParameterLocation.Header,
                        Description = "Enter the complete authorization value: `ApiKey ntp_<credential-id>_<secret>`.",
                    },
                };

                foreach (var operation in document.Paths.Values
                             .Where(path => path.Operations is not null)
                             .SelectMany(path => path.Operations!.Values))
                {
                    operation.Security ??= [];
                    operation.Security.Add(new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecuritySchemeReference(SecuritySchemeName, document)] = [],
                    });
                }

                return Task.CompletedTask;
            });
        });

        return services;
    }
}
