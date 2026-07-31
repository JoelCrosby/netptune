-- Paged relations of a single relation type, for the workspace settings usage page.
--
-- A relation is stored once, as source -> target, so each row reads in the type's own direction:
-- source_task <relation type name> target_task.
--
-- Task deletion is reversible and a relation survives it, so soft-deleted tasks are kept here and
-- flagged instead of filtered out. That keeps this list consistent with the relation count that
-- decides whether the type can be deleted.
--
-- count(*) OVER () carries the unpaged total on every row.
-- Named parameters:
--   @RelationTypeId  relation type id
--   @WorkspaceId     workspace id
--   @Limit           page size
--   @Offset          rows to skip
SELECT relation.id AS relation_id
     , source.id AS source_task_id
     , source.name AS source_task_name
     , source.project_scope_id AS source_task_scope_id
     , source.is_deleted AS source_task_is_archived
     , source_project.key AS source_task_project_key
     , source_status.name AS source_task_status_name
     , source_status.color AS source_task_status_color
     , source_status.category AS source_task_status_category
     , target.id AS target_task_id
     , target.name AS target_task_name
     , target.project_scope_id AS target_task_scope_id
     , target.is_deleted AS target_task_is_archived
     , target_project.key AS target_task_project_key
     , target_status.name AS target_task_status_name
     , target_status.color AS target_task_status_color
     , target_status.category AS target_task_status_category
     , count(*) OVER () AS total_count
FROM project_task_relations relation
         INNER JOIN project_tasks source ON source.id = relation.source_task_id
         INNER JOIN statuses source_status ON source.status_id = source_status.id
         LEFT JOIN projects source_project ON source.project_id = source_project.id
         INNER JOIN project_tasks target ON target.id = relation.target_task_id
         INNER JOIN statuses target_status ON target.status_id = target_status.id
         LEFT JOIN projects target_project ON target.project_id = target_project.id
WHERE relation.workspace_id = @WorkspaceId
  AND relation.relation_type_id = @RelationTypeId
ORDER BY relation.id
LIMIT @Limit
OFFSET @Offset;
