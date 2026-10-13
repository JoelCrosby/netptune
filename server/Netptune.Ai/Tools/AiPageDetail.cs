using Netptune.Ai.Web;
using Netptune.Core.ViewModels.Pages;

namespace Netptune.Ai.Tools;

public static class AiPageDetail
{
    public const int DefaultTake = 8000;
    public const int MaximumTake = 20000;

    public static object Read(PageViewModel page, int offset, int take)
    {
        var fullBody = page.Body ?? string.Empty;
        var body = WebDocumentPage.Read(fullBody, offset, take);
        var format = AiTextFormatReader.Read(fullBody);

        return new
        {
            id = page.Id,
            slug = page.Slug,
            name = page.Title,
            kind = page.Kind.ToString(),
            status = page.Status.ToString(),
            parentId = page.ParentId,
            tags = page.Tags.Select(tag => tag.Name),
            revision = page.Revision,
            updatedAt = page.UpdatedAt ?? page.CreatedAt,
            updatedBy = page.ModifiedByDisplayName ?? page.CreatedByDisplayName,
            totalCharacters = fullBody.Length,
            format = AiTextFormatJson.From(format),
            offset = body.Offset,
            nextOffset = body.NextOffset,
            hasMore = body.HasMore,
            body = body.Text,
        };
    }
}
