SELECT sprint.id
FROM sprints sprint
WHERE sprint.workspace_id = @workspaceId
  AND NOT sprint.is_deleted
  AND sprint.project_id = ANY(@allowedProjectIds)
  AND sprint.id = ANY(@sprintIds)
ORDER BY sprint.id;
