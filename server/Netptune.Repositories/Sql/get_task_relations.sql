-- Every relation touching @TaskId, from that task's point of view.
--
-- A relation is stored once, as source -> target. The requested task may sit on either end, so
-- `is_source` tells the caller which label to read: the relation type's name when the requested
-- task is the source, its inverse_name when it is the target.
--
-- Soft-deleted tasks are filtered out rather than having their relations deleted. Task deletion is
-- reversible, so the rows survive and a restored task comes back with its links intact.
SELECT relation.id AS relation_id
     , relation.relation_type_id
     , (relation.source_task_id = @TaskId) AS is_source
     , relation_type.name AS relation_type_name
     , relation_type.inverse_name AS relation_type_inverse_name
     , relation_type.key AS relation_type_key
     , relation_type.color AS relation_type_color
     , relation_type.category AS relation_type_category
     , relation_type.sort_order AS relation_type_sort_order
     , other.id AS other_task_id
     , other.name AS other_task_name
     , other.project_scope_id AS other_task_scope_id
     , other_project.key AS other_task_project_key
     , other_status.name AS other_task_status_name
     , other_status.color AS other_task_status_color
     , other_status.category AS other_task_status_category
FROM project_task_relations relation
         INNER JOIN relation_types relation_type
                    ON relation.relation_type_id = relation_type.id
                        AND NOT relation_type.is_deleted
         INNER JOIN project_tasks other
                    ON other.id = CASE
                                      WHEN relation.source_task_id = @TaskId THEN relation.target_task_id
                                      ELSE relation.source_task_id
                        END
                        AND NOT other.is_deleted
         INNER JOIN statuses other_status ON other.status_id = other_status.id
         LEFT JOIN projects other_project ON other.project_id = other_project.id
WHERE relation.workspace_id = @WorkspaceId
  AND (relation.source_task_id = @TaskId OR relation.target_task_id = @TaskId)
ORDER BY relation_type.sort_order, relation_type.id, is_source DESC, other.id;
