using Mediator;

using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Tags;

namespace Netptune.Services.Tags.Commands.CreateTag;

public sealed record CreateTagCommand(AddTagRequest Request) : IRequest<ClientResponse<TagViewModel>>;
