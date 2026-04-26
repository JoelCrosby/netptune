using Mediator;
using Netptune.Core.Enums;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Activity;

namespace Netptune.Services.Activity.Queries;

public sealed record GetActivitiesQuery(EntityType EntityType, int EntityId) : IRequest<ClientResponse<List<ActivityViewModel>>>;
