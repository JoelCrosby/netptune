using Mediator;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Tags;

namespace Netptune.Services.Tags.Commands;

public sealed record AddTagToTaskCommand(AddTagToTaskRequest Request) : IRequest<ClientResponse<TagViewModel>>;
