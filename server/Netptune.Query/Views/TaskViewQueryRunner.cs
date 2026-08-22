using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Query.Model;
using Netptune.Query.Tasks;
using Netptune.Query.Validation;

namespace Netptune.Query.Views;

public sealed record TaskViewQueryRequest
{
    public QueryGroup? Query { get; init; }

    public int? Page { get; init; }

    public int? PageSize { get; init; }

    public string? SortBy { get; init; }

    public string? SortDirection { get; init; }
}

public sealed class TaskViewQueryRunner
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly TaskReferenceValidator ReferenceValidator;

    public TaskViewQueryRunner(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        TaskReferenceValidator referenceValidator)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        ReferenceValidator = referenceValidator;
    }

    public async Task<ClientResponse<TaskViewResultViewModel>> Run(TaskViewQueryRequest request, CancellationToken cancellationToken)
    {
        var structural = QueryValidator.Validate(TaskFieldCatalog.Instance, request.Query);

        if (!structural.IsValid)
        {
            return Invalid(structural);
        }

        var workspaceId = await Identity.GetWorkspaceId();
        var workspaceKey = Identity.GetWorkspaceKey();
        var scope = new QueryWorkspaceScope(workspaceId, workspaceKey);
        var references = await ReferenceValidator.Validate(request.Query, scope, cancellationToken);

        if (!references.IsValid)
        {
            return Invalid(references);
        }

        var filter = new TaskQueryFilter
        {
            Query = request.Query,
            Page = request.Page,
            PageSize = request.PageSize,
            SortBy = request.SortBy,
            SortDirection = request.SortDirection,
        };

        var tasks = await UnitOfWork.Tasks.GetTasksAsync(workspaceKey, filter, true, cancellationToken: cancellationToken);
        var result = TaskViewResultViewModel.FromPage(tasks);

        return ClientResponse<TaskViewResultViewModel>.Success(result);
    }

    private static ClientResponse<TaskViewResultViewModel> Invalid(QueryValidationResult validation)
    {
        var result = new TaskViewResultViewModel { Errors = validation.Errors };

        return ClientResponse<TaskViewResultViewModel>.Failed(result, validation.ToMessage());
    }
}
