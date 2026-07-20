using Netptune.SeedData.Seeders;

namespace Netptune.SeedData;

public static class SeedDataExtensions
{
    public static IServiceCollection AddNetptuneSeedData(this IServiceCollection services)
    {
        services.AddSingleton<ISeeder, UserSeeder>();
        services.AddSingleton<ISeeder, WorkspaceSeeder>();
        services.AddSingleton<ISeeder, WorkspaceUserSeeder>();
        services.AddSingleton<ISeeder, StatusSeeder>();
        services.AddSingleton<ISeeder, ProjectSeeder>();
        services.AddSingleton<ISeeder, ProjectUserSeeder>();
        services.AddSingleton<ISeeder, SprintSeeder>();
        services.AddSingleton<ISeeder, BoardSeeder>();
        services.AddSingleton<ISeeder, BoardGroupSeeder>();
        services.AddSingleton<ISeeder, TaskSeeder>();

        services.AddSingleton<ISeeder, BoardGroupTaskSeeder>();
        services.AddSingleton<ISeeder, TaskAssigneeSeeder>();
        services.AddSingleton<ISeeder, EventRecordSeeder>();
        services.AddSingleton<ISeeder, CommentSeeder>();
        services.AddSingleton<ISeeder, TagSeeder>();
        services.AddSingleton<ISeeder, TaskTagSeeder>();

        services.AddSingleton<ISeeder, SprintReportingSeeder>();
        services.AddSingleton<ISeeder, NotificationSeeder>();

        services.AddHostedService<SeedDataService>();

        return services;
    }
}
