using System.Net;
using System.Text.Json;

using Microsoft.AspNetCore.Http;

using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Events;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Services.Activity;

public sealed class EventRecordWriter : IEventRecordWriter
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService? Identity;
    private readonly IHttpContextAccessor? HttpContextAccessor;

    public EventRecordWriter(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService? identity = null,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        HttpContextAccessor = httpContextAccessor;
    }

    public async Task<EventRecord> Append<TPayload>(
        EventWriteRequest<TPayload> request,
        CancellationToken cancellationToken = default)
        where TPayload : class
    {
        EventDefinitionRegistry.Validate(request);

        var context = HttpContextAccessor?.HttpContext;
        var workspaceId = request.WorkspaceId;
        var actorUserId = request.ActorUserId ?? Identity?.GetCurrentUserId();
        var eventKey = request.EventKey;

        var record = new EventRecord
        {
            EventId = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            EventKey = eventKey,
            SchemaVersion = request.SchemaVersion,
            SubjectType = request.SubjectType,
            SubjectId = request.SubjectId,
            OccurredAt = request.OccurredAt ?? DateTime.UtcNow,
            RecordedAt = DateTime.UtcNow,
            ActorUserId = actorUserId,
            CorrelationId = request.CorrelationId ?? GetCorrelationId(context),
            CausationEventId = request.CausationEventId,
            IpAddress = GetIpAddress(context),
            UserAgent = context?.Request.Headers.UserAgent.ToString(),
            RetentionClass = EventKeys.RetentionFor(eventKey),
            Payload = JsonSerializer.SerializeToDocument(request.Payload, JsonOptions.Default),
            References = request.References.Select(reference => new EventReference
            {
                Role = reference.Role,
                EntityType = reference.EntityType,
                EntityId = reference.EntityId,
            }).ToHashSet(),
        };

        var appendedRecord = await UnitOfWork.EventRecords.AppendAsync(record, request.Publish, cancellationToken);

        return appendedRecord;
    }

    private static Guid? GetCorrelationId(HttpContext? context)
    {
        return Guid.TryParse(context?.TraceIdentifier, out var correlationId) ? correlationId : null;
    }

    private static IPAddress? GetIpAddress(HttpContext? context)
    {

        if (context is null)
        {
            return null;
        }

        var value = context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwarded)
            ? forwarded.ToString().Split(',')[0].Trim()
            : context.Connection.RemoteIpAddress?.ToString();

        return IPAddress.TryParse(value, out var ipAddress) ? ipAddress : null;
    }
}
