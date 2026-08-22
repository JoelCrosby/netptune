using Microsoft.Extensions.DependencyInjection;

using Netptune.Query.Tasks;
using Netptune.Query.Views;

namespace Netptune.Query;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNetptuneQuery(this IServiceCollection services)
    {
        services.AddScoped<TaskReferenceValidator>();
        services.AddScoped<TaskViewQueryRunner>();

        return services;
    }
}
