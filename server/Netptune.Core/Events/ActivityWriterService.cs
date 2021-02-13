using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Netptune.Core.Entities;
using Netptune.Core.UnitOfWork;

namespace Netptune.Core.Events
{
    public class ActivityWriterService : IActivityWriterService, IHostedService
    {
        private readonly IActivityObservable ActivityObservable;
        private readonly IServiceProvider ServiceProvider;

        private IDisposable Subscription { get; set; }

        public ActivityWriterService(IActivityObservable activityObservable, IServiceProvider serviceProvider)
        {
            ActivityObservable = activityObservable;
            ServiceProvider = serviceProvider;
        }

        private async Task WriteActivity(IActivityEvent activityEvent)
        {
            using var scope = ServiceProvider.CreateScope();

            var unitOfWork = scope.ServiceProvider.GetRequiredService<INetptuneUnitOfWork>();

            await unitOfWork.ActivityLogs.AddAsync(new ActivityLog
            {
                OwnerId = activityEvent.UserId,
                Type = activityEvent.Type,
                EntityType = activityEvent.EntityType,
                EntityId = activityEvent.EntityId,
                UserId = activityEvent.UserId,
                CreatedByUserId = activityEvent.UserId,
                WorkspaceId = activityEvent.WorkspaceId,
            });

            await unitOfWork.CompleteAsync();
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(IActivityEvent value)
        {
            WriteActivity(value).GetAwaiter().GetResult();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Subscription = ActivityObservable.Subscribe(this);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Subscription.Dispose();

            return Task.CompletedTask;
        }
    }
}
