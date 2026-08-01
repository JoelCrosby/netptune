using System.Net;
using System.Text.Json;

using Microsoft.AspNetCore.Http;

using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.Services.Ai;
using Netptune.Core.UnitOfWork;

namespace Netptune.Services.Activity;

public sealed class EventRecordWriter : IEventRecordWriter
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService? Identity;
    private readonly IHttpContextAccessor? HttpContextAccessor;
    private readonly ICanonicalEventCapture? Capture;
    private readonly IAiExecutionContext? AiExecution;

    public EventRecordWriter(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService? identity = null,
        IHttpContextAccessor? httpContextAccessor = null,
        ICanonicalEventCapture? capture = null,
        IAiExecutionContext? aiExecution = null)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        HttpContextAccessor = httpContextAccessor;
        Capture = capture;
        AiExecution = aiExecution;
    }

    public async Task<EventRecord> Append<TPayload>(
        EventWriteRequest<TPayload> request,
        CancellationToken cancellationToken = default)
        where TPayload : class
    {
        EventDefinitionRegistry.Validate(request);

        var context = HttpContextAccessor?.HttpContext;
        var workspaceId = request.WorkspaceId;
        var actorUserId = request.ActorUserId;

        if (actorUserId is null && request.ResolveActorFromIdentity)
        {
            actorUserId = Identity?.GetCurrentUserId();
        }
        var eventKey = request.EventKey;
        var isAssistantExecution = AiExecution?.IsActive == true;
        var originType = isAssistantExecution ? AiExecution!.OriginType : EventOriginType.User;
        var agent = isAssistantExecution ? AiExecution!.Agent : null;
        var assistantCorrelationId = isAssistantExecution ? AiExecution!.CorrelationId : null;

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
            OriginType = originType,
            Agent = agent,
            CorrelationId = request.CorrelationId ?? assistantCorrelationId ?? GetCorrelationId(context),
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
        var hasWorkspace = workspaceId.HasValue;
        var hasSubjectType = request.SubjectType is not null;
        var hasSubjectId = request.SubjectId is not null;
        var hasCapturableSubject = hasWorkspace && hasSubjectType && hasSubjectId;

        if (hasCapturableSubject)
        {
            Capture?.Record(
                workspaceId.GetValueOrDefault(),
                request.SubjectType!,
                request.SubjectId!);
        }

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
