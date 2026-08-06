using Netptune.Transfer.Repositories;
using Netptune.Transfer.Entities;
using System.Text.Json;

using Mediator;

using Netptune.Core.Encoding;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Transfer.Export;
using Netptune.Core.UnitOfWork;
using Netptune.Transfer.ViewModels;

namespace Netptune.Handlers.Transfer.Commands;

public sealed record SaveExportDefinitionRequest
{
    public int? Id { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    public bool IsShared { get; init; }

    public required ExportDefinitionModel Definition { get; init; }
}

public sealed record SaveExportDefinitionCommand(SaveExportDefinitionRequest Request) : IRequest<ClientResponse<ExportDefinitionViewModel>>;

public sealed class SaveExportDefinitionCommandHandler : IRequestHandler<SaveExportDefinitionCommand, ClientResponse<ExportDefinitionViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IExportDefinitionRepository ExportDefinitions;
    private readonly IIdentityService Identity;

    public SaveExportDefinitionCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity,
        IExportDefinitionRepository exportDefinitions)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        ExportDefinitions = exportDefinitions;
    }

    public async ValueTask<ClientResponse<ExportDefinitionViewModel>> Handle(SaveExportDefinitionCommand request, CancellationToken cancellationToken)
    {
        var input = request.Request;

        if (string.IsNullOrWhiteSpace(input.Name))
        {
            return ClientResponse<ExportDefinitionViewModel>.Failed("A name is required.");
        }

        var validation = ExportDefinitionValidator.Validate(input.Definition);

        if (!validation.IsValid)
        {
            return ClientResponse<ExportDefinitionViewModel>.Failed(string.Join(" ", validation.Errors));
        }

        var workspaceId = await Identity.GetWorkspaceId();
        var name = input.Name.Trim();
        var nameTaken = await ExportDefinitions.NameExists(workspaceId, name, input.Id, cancellationToken);

        if (nameTaken)
        {
            return ClientResponse<ExportDefinitionViewModel>.Failed($"An export definition named '{name}' already exists.");
        }

        var userId = Identity.GetCurrentUserId();
        var document = JsonSerializer.SerializeToDocument(input.Definition, JsonOptions.Default);
        var definition = await Resolve(input, workspaceId, cancellationToken);

        if (definition is null)
        {
            return ClientResponse<ExportDefinitionViewModel>.NotFound;
        }

        definition.Name = name;
        definition.Description = input.Description;
        definition.RecordType = input.Definition.RecordType;
        definition.Format = input.Definition.Format;
        definition.Definition = document;
        definition.IsShared = input.IsShared;
        definition.ModifiedByUserId = userId;

        if (definition.Id == 0)
        {
            definition.WorkspaceId = workspaceId;
            definition.CreatedByUserId = userId;
            definition.OwnerId = userId;

            await ExportDefinitions.AddAsync(definition, cancellationToken);
        }

        await UnitOfWork.CompleteAsync(cancellationToken);

        var viewModel = ExportDefinitionMapper.ToViewModel(definition);

        return ClientResponse<ExportDefinitionViewModel>.Success(viewModel);
    }

    private async Task<ExportDefinition?> Resolve(SaveExportDefinitionRequest input, int workspaceId, CancellationToken cancellationToken)
    {
        if (input.Id is null)
        {
            return new ExportDefinition();
        }

        return await ExportDefinitions.GetInWorkspace(input.Id.Value, workspaceId, cancellationToken: cancellationToken);
    }
}
