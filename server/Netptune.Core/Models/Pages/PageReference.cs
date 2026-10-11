using Netptune.Core.Enums;

namespace Netptune.Core.Models.Pages;

public sealed record PageReference(PageLinkTargetType Kind, string Key, string Snippet);
