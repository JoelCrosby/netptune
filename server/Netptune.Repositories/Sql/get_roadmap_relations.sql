SELECT relation.id
     , relation.source_task_id
     , relation.target_task_id
     , relation.relation_type_id
     , relation_type.key AS relation_type_key
     , relation_type.category
FROM project_task_relations relation
         INNER JOIN relation_types relation_type
                    ON relation.relation_type_id = relation_type.id
                        AND NOT relation_type.is_deleted
WHERE relation.workspace_id = @workspaceId
  AND relation.source_task_id = ANY(@taskIds)
  AND relation.target_task_id = ANY(@taskIds)
  AND relation_type.category = ANY(@categories)
ORDER BY relation_type.sort_order, relation.id;
