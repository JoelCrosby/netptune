using Mediator;

using Microsoft.Extensions.Options;

using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Transfer;
using Netptune.Transfer.Definitions;
using Netptune.Transfer.Services;

namespace Netptune.Handlers.Transfer.Queries;

public sealed record ExportPreviewRequest
{
    public required ExportDefinitionModel Definition { get; init; }
}

public sealed record GetExportPreviewQuery(ExportPreviewRequest Request) : IRequest<ClientResponse<ExportPreviewResult>>;

public sealed class GetExportPreviewQueryHandler : IRequestHandler<GetExportPreviewQuery, ClientResponse<ExportPreviewResult>>
{
    private readonly IIdentityService Identity;
    private readonly IExportRunner Runner;
    private readonly TransferOptions Options;

    public GetExportPreviewQueryHandler(IIdentityService identity, IExportRunner runner, IOptions<TransferOptions> options)
    {
        Identity = identity;
        Runner = runner;
        Options = options.Value;
    }

    public async ValueTask<ClientResponse<ExportPreviewResult>> Handle(GetExportPreviewQuery request, CancellationToken cancellationToken)
    {
        var definition = request.Request.Definition;
        var validation = ExportDefinitionValidator.Validate(definition);

        if (!validation.IsValid)
        {
            return ClientResponse<ExportPreviewResult>.Failed(string.Join(" ", validation.Errors));
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
        var preview = await Runner.Preview(runRequest, Options.PreviewSampleSize, cancellationToken);

        return ClientResponse<ExportPreviewResult>.Success(preview);
    }
}
