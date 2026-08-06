using Netptune.Transfer;
using Netptune.Transfer.ViewModels;

namespace Netptune.App.Endpoints;

public static class TransferEndpoints
{
    public static RouteGroupBuilder MapTransferEndpoints(this RouteGroupBuilder builder)
    {
        var group = builder.MapGroup("transfer");

        group.MapGet("/catalog", HandleGetCatalog)
            .RequireAuthorization();

        return group;
    }

    private static IResult HandleGetCatalog()
    {
        var catalog = new TransferCatalogViewModel
        {
            RecordTypes = TransferFieldCatalog.All,
        };

        return Results.Ok(catalog);
    }
}
