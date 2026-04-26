using Mediator;

using Netptune.Core.ViewModels.Projects;

namespace Netptune.Services.Projects.Queries.GetProjects;

public sealed record GetProjectsQuery : IRequest<List<ProjectViewModel>>;
