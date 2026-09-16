namespace Netptune.Core.Requests;

public static class PaginationDefaults
{
    public const int DefaultPage = 1;

    public const int DefaultPageSize = 50;

    public const int MaxPageSize = 100;

    public const int MaxAdminPageSize = 200;

    public const int MaxExportRows = 10_000;

    public const int MaxUnpagedRows = 500;

    public const int MaxConversationMessages = 1_000;
}
