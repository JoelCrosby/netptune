using System.Text.Json.Serialization;

namespace Netptune.App.Services;

[JsonSerializable(typeof(WorkspaceEvent))]
[JsonSerializable(typeof(PresenceMessage))]
[JsonSerializable(typeof(string[]))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class BoardEventSerializerContext : JsonSerializerContext;
