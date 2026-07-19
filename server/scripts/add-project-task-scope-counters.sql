BEGIN;

LOCK TABLE public.projects IN ACCESS EXCLUSIVE MODE;
LOCK TABLE public.project_tasks IN SHARE ROW EXCLUSIVE MODE;

ALTER TABLE public.projects
    ADD COLUMN IF NOT EXISTS next_task_scope_id integer;

UPDATE public.projects AS project
SET next_task_scope_id = scope.next_task_scope_id
FROM (
    SELECT project_row.id,
           COALESCE(MAX(task.project_scope_id) + 1, 1) AS next_task_scope_id
    FROM public.projects AS project_row
    LEFT JOIN public.project_tasks AS task ON task.project_id = project_row.id
    GROUP BY project_row.id
) AS scope
WHERE project.id = scope.id
  AND (
      project.next_task_scope_id IS NULL OR
      project.next_task_scope_id < scope.next_task_scope_id
  );

ALTER TABLE public.projects
    ALTER COLUMN next_task_scope_id SET DEFAULT 1,
    ALTER COLUMN next_task_scope_id SET NOT NULL;

COMMIT;
