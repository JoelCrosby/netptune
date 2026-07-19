UPDATE projects
SET next_task_scope_id = next_task_scope_id + @count
WHERE id = @projectId
RETURNING next_task_scope_id - @count;
