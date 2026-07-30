-- Per-workspace selection of what an anonymous visitor may read while the workspace is public.
-- NULL means "never configured" and resolves to the full set on offer (NetptunePermissions.PublicReadable).
ALTER TABLE workspaces
    ADD COLUMN IF NOT EXISTS public_permissions jsonb NULL;
