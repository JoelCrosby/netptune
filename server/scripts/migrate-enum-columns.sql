-- Migrates integer-backed EF enum columns to PostgreSQL enum types.
--
-- Intended for production databases that still have:
--   sprints.status              integer
--   workspace_app_users.role    integer
--
-- Historical WorkspaceRole integer values were:
--   0 = owner, 1 = admin, 2 = member, 3 = viewer
--
-- Current CLR WorkspaceRole numeric values are:
--   viewer = 0, member = 10, admin = 100, owner = 1000
--
-- The CASE below supports both sets for every value except 0. For production
-- data created before the enum change, 0 is treated as owner.

BEGIN;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_type t
        JOIN pg_namespace n ON n.oid = t.typnamespace
        WHERE n.nspname = 'public'
          AND t.typname = 'sprint_status'
    ) THEN
        CREATE TYPE public.sprint_status AS ENUM (
            'active',
            'cancelled',
            'completed',
            'planning'
        );
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM pg_type t
        JOIN pg_namespace n ON n.oid = t.typnamespace
        WHERE n.nspname = 'public'
          AND t.typname = 'workspace_role'
    ) THEN
        CREATE TYPE public.workspace_role AS ENUM (
            'admin',
            'member',
            'owner',
            'viewer'
        );
    END IF;
END
$$;

DO $$
DECLARE
    invalid_sprint_statuses text;
    invalid_workspace_roles text;
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'sprints'
          AND column_name = 'status'
          AND udt_name <> 'sprint_status'
    ) THEN
        SELECT string_agg(status::text, ', ' ORDER BY status)
        INTO invalid_sprint_statuses
        FROM (
            SELECT DISTINCT status
            FROM public.sprints
            WHERE status NOT IN (0, 1, 2, 3)
        ) invalid_values;

        IF invalid_sprint_statuses IS NOT NULL THEN
            RAISE EXCEPTION 'Cannot migrate sprints.status. Unexpected integer value(s): %', invalid_sprint_statuses;
        END IF;
    END IF;

    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'workspace_app_users'
          AND column_name = 'role'
          AND udt_name <> 'workspace_role'
    ) THEN
        SELECT string_agg(role::text, ', ' ORDER BY role)
        INTO invalid_workspace_roles
        FROM (
            SELECT DISTINCT role
            FROM public.workspace_app_users
            WHERE role NOT IN (0, 1, 2, 3, 10, 100, 1000)
        ) invalid_values;

        IF invalid_workspace_roles IS NOT NULL THEN
            RAISE EXCEPTION 'Cannot migrate workspace_app_users.role. Unexpected integer value(s): %', invalid_workspace_roles;
        END IF;
    END IF;
END
$$;

DROP INDEX IF EXISTS public.ix_sprints_project_id;

DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'sprints'
          AND column_name = 'status'
          AND udt_name <> 'sprint_status'
    ) THEN
        ALTER TABLE public.sprints
            ALTER COLUMN status DROP DEFAULT,
            ALTER COLUMN status TYPE public.sprint_status
            USING CASE status
                WHEN 0 THEN 'planning'::public.sprint_status
                WHEN 1 THEN 'active'::public.sprint_status
                WHEN 2 THEN 'completed'::public.sprint_status
                WHEN 3 THEN 'cancelled'::public.sprint_status
            END,
            ALTER COLUMN status SET DEFAULT 'planning'::public.sprint_status;
    END IF;
END
$$;

DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'workspace_app_users'
          AND column_name = 'role'
          AND udt_name <> 'workspace_role'
    ) THEN
        ALTER TABLE public.workspace_app_users
            ALTER COLUMN role DROP DEFAULT,
            ALTER COLUMN role TYPE public.workspace_role
            USING CASE role
                -- Legacy values from the old CLR enum.
                WHEN 0 THEN 'owner'::public.workspace_role
                WHEN 1 THEN 'admin'::public.workspace_role
                WHEN 2 THEN 'member'::public.workspace_role
                WHEN 3 THEN 'viewer'::public.workspace_role

                -- Current CLR numeric values, in case any were already written.
                WHEN 10 THEN 'member'::public.workspace_role
                WHEN 100 THEN 'admin'::public.workspace_role
                WHEN 1000 THEN 'owner'::public.workspace_role
            END,
            ALTER COLUMN role SET DEFAULT 'member'::public.workspace_role;
    END IF;
END
$$;

CREATE UNIQUE INDEX IF NOT EXISTS ix_sprints_project_id
    ON public.sprints (project_id)
    WHERE status = 'active'::public.sprint_status
      AND is_deleted = false;

COMMIT;
