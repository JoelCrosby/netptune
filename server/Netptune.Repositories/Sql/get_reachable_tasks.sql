-- Walks forward from every task in @FromTaskIds along relation type @RelationTypeId and returns
-- every task reachable from any of them. Callers asking "would linking these close a cycle?" pass
-- the prospective targets and look for a prospective source in the result.
--
-- UNION (not UNION ALL) dedupes the frontier, so this still terminates if the data already
-- contains a cycle rather than spinning forever.
WITH RECURSIVE reachable AS (
    SELECT relation.target_task_id AS task_id
    FROM project_task_relations relation
    WHERE relation.relation_type_id = @RelationTypeId
      AND relation.source_task_id = ANY(@FromTaskIds)

    UNION

    SELECT relation.target_task_id
    FROM project_task_relations relation
    INNER JOIN reachable ON relation.source_task_id = reachable.task_id
    WHERE relation.relation_type_id = @RelationTypeId
)
SELECT task_id
FROM reachable;
