-- Tag ids rather than names, so renaming a tag doesn't change every task carrying it. The JSON array
-- keeps neighbouring fields from running into each other.
SELECT pt.id AS taskid
     , encode(sha256(convert_to(jsonb_build_array(
           pt.name,
           COALESCE(pt.description, ''),
           pt.estimate_type,
           pt.estimate_value,
           pt.priority,
           COALESCE((
               SELECT array_agg(ptt.tag_id ORDER BY ptt.tag_id)
               FROM project_task_tags ptt
                        INNER JOIN tags t ON ptt.tag_id = t.id AND NOT t.is_deleted
               WHERE ptt.project_task_id = pt.id
           ), '{}')
       )::text, 'UTF8')), 'hex') AS fingerprint
FROM project_tasks pt
WHERE pt.workspace_id = @workspaceId
  AND pt.id = ANY(@taskIds)
  AND NOT pt.is_deleted;
