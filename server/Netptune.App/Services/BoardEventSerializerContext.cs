using System.Text.Json.Serialization;

using Netptune.Core.Services.Realtime;

namespace Netptune.App.Services;

[JsonSerializable(typeof(WorkspaceEvent))]
[JsonSerializable(typeof(WorkspaceUpdateFrame))]
[JsonSerializable(typeof(PresenceMessage))]
[JsonSerializable(typeof(string[]))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class BoardEventSerializerContext : JsonSerializerContext;
