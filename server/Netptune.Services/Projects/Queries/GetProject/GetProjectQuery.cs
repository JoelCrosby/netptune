using Mediator;
using Netptune.Core.ViewModels.Projects;

namespace Netptune.Services.Projects.Queries;

public sealed record GetProjectQuery(string Key) : IRequest<ProjectViewModel?>;
