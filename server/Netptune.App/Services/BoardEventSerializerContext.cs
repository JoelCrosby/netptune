using System.Text.Json.Serialization;

namespace Netptune.App.Services;

[JsonSerializable(typeof(ClientChannel))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class BoardEventSerializerContext : JsonSerializerContext;
