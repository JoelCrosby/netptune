-- Adds the optional per-board-group assigned status.
--
-- A board group may now reference a task status (board_groups.status_id). When a
-- task is moved into (or created in) that group, it is set to that status. The
-- foreign key is nullable and uses ON DELETE SET NULL so deleting a status simply
-- clears the mapping rather than blocking the delete.
--
-- Also drops the now-unused board_groups.type column (the BoardGroupType concept was
-- removed from the application in favour of this explicit status assignment).

BEGIN;

ALTER TABLE public.board_groups
    ADD COLUMN IF NOT EXISTS status_id integer NULL;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'fk_board_groups_statuses_status_id'
          AND conrelid = 'public.board_groups'::regclass
    ) THEN
        ALTER TABLE public.board_groups
            ADD CONSTRAINT fk_board_groups_statuses_status_id
            FOREIGN KEY (status_id) REFERENCES public.statuses (id) ON DELETE SET NULL;
    END IF;
END
$$;

CREATE INDEX IF NOT EXISTS ix_board_groups_status_id
    ON public.board_groups (status_id);

ALTER TABLE public.board_groups
    DROP COLUMN IF EXISTS type;

COMMIT;
