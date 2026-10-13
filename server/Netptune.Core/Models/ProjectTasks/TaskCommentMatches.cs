using Netptune.Core.ViewModels.Comments;

namespace Netptune.Core.Models.ProjectTasks;

public sealed record TaskCommentMatches(List<TaskCommentTextViewModel> Comments, Dictionary<int, int> CountsByTask);
