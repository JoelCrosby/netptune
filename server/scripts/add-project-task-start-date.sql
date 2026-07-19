ALTER TABLE project_tasks
    ADD COLUMN IF NOT EXISTS start_date date NULL;
