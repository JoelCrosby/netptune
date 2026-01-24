using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Netptune.Core.Authorization;
using Netptune.Core.Services.Import;

namespace Netptune.App.Endpoints;

public static class ImportEndpoints
{
    public static RouteGroupBuilder Map(this RouteGroupBuilder builder)
    {
        var group = builder.MapGroup("import")
            .RequireAuthorization(NetptunePolicies.Workspace);

        group.MapPost("/tasks/{boardId}", HandleImportWorkspaceTasks);

        return group;
    }

    public static async Task<IResult> HandleImportWorkspaceTasks(
        ITaskImportService taskImportService,
        HttpRequest request,
        string boardId)
    {
        var file = request.Form.Files.FirstOrDefault();

        if (file is null)
        {
            return Results.BadRequest("Import File must be provided. Only one file can be uploaded at a time.");
        }

        if (file.Length > 50 * 1024 * 1024)
        {
            return Results.BadRequest("Request file size exceeds maximum of 50MB.");
        }

        var stream = file.OpenReadStream();

        var result = await taskImportService.ImportWorkspaceTasks(boardId, stream);

        return Results.Ok(result);
    }
}
