-- Counts non-deleted tasks in a workspace grouped by their status, for the
-- dashboard "tasks by status" breakdown chart. Only statuses that have at
-- least one task appear; the caller sums the counts for the total.
SELECT st.id       AS statusid
     , st.name     AS name
     , st.color    AS color
     , st.category AS category
     , COUNT(pt.id) AS count
FROM project_tasks pt
         INNER JOIN workspaces w ON pt.workspace_id = w.id
         INNER JOIN statuses st ON pt.status_id = st.id
WHERE w.slug = @workspaceKey
  AND NOT pt.is_deleted
GROUP BY st.id, st.name, st.color, st.category, st.sort_order
ORDER BY st.sort_order, st.id;
