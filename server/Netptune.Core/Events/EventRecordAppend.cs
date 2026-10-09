using Netptune.Core.Entities;

namespace Netptune.Core.Events;

public sealed record EventRecordAppend(EventRecord Record, bool Publish);
