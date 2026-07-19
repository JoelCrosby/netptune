SELECT sprint.id
     , sprint.name
     , sprint.start_date
     , sprint.end_date
     , sprint.status
     , sprint.project_id
FROM sprints sprint
WHERE sprint.workspace_id = @workspaceId
  AND NOT sprint.is_deleted
  AND sprint.project_id = ANY(@allowedProjectIds)
  AND (CARDINALITY(@projectIds) = 0 OR sprint.project_id = ANY(@projectIds))
  AND (CARDINALITY(@sprintIds) = 0 OR sprint.id = ANY(@sprintIds))
  AND sprint.start_date::date <= @to
  AND sprint.end_date::date >= @from
ORDER BY sprint.start_date, sprint.id;
