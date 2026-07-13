-- Determines whether adding the edge @SourceTaskId -> @TargetTaskId for relation type
-- @RelationTypeId would close a cycle, by walking forward from the prospective target and
-- asking whether the prospective source is reachable.
--
-- UNION (not UNION ALL) dedupes the frontier, so this still terminates if the data already
-- contains a cycle rather than spinning forever.
WITH RECURSIVE reachable AS (
    SELECT relation.target_task_id AS task_id
    FROM project_task_relations relation
    WHERE relation.relation_type_id = @RelationTypeId
      AND relation.source_task_id = @TargetTaskId

    UNION

    SELECT relation.target_task_id
    FROM project_task_relations relation
    INNER JOIN reachable ON relation.source_task_id = reachable.task_id
    WHERE relation.relation_type_id = @RelationTypeId
)
SELECT EXISTS (
    SELECT 1
    FROM reachable
    WHERE reachable.task_id = @SourceTaskId
);
