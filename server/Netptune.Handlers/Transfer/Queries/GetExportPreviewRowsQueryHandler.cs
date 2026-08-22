using Mediator;

using Microsoft.Extensions.Options;

using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Transfer;
using Netptune.Transfer.Definitions;
using Netptune.Transfer.Services;

namespace Netptune.Handlers.Transfer.Queries;

public sealed record GetExportPreviewRowsQuery(ExportPreviewRowsRequest Request)
    : IRequest<ClientResponse<PagedResponse<ExportPreviewRow>>>;

public sealed class GetExportPreviewRowsQueryHandler
    : IRequestHandler<GetExportPreviewRowsQuery, ClientResponse<PagedResponse<ExportPreviewRow>>>
{
    private readonly IIdentityService Identity;
    private readonly IExportRunner Runner;
    private readonly TransferOptions Options;

    public GetExportPreviewRowsQueryHandler(IIdentityService identity, IExportRunner runner, IOptions<TransferOptions> options)
    {
        Identity = identity;
        Runner = runner;
        Options = options.Value;
    }

    public async ValueTask<ClientResponse<PagedResponse<ExportPreviewRow>>> Handle(
        GetExportPreviewRowsQuery request,
        CancellationToken cancellationToken)
    {
        var definition = request.Request.ToDefinition();
        var validation = ExportDefinitionValidator.Validate(definition);

        if (!validation.IsValid)
        {
            return ClientResponse<PagedResponse<ExportPreviewRow>>.Failed(string.Join(" ", validation.Errors));
        }

        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await Identity.GetWorkspaceId();
        var runRequest = new ExportRunRequest
        {
            WorkspaceId = workspaceId,
            WorkspaceSlug = workspaceKey,
            Definition = definition,
            InlineRowLimit = Options.InlineRowLimit,
        };
        var rows = await Runner.PreviewRows(runRequest, request.Request.GetPagination(), cancellationToken);

        return ClientResponse<PagedResponse<ExportPreviewRow>>.Success(rows);
    }
}
