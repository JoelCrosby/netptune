-- Writes one branding key of a project's meta document for ProjectRepository.SetBrandingFile.
-- See set_workspace_branding_file.sql for why jsonb_set is used.
UPDATE projects
SET meta_info = jsonb_set(
    COALESCE(meta_info, '{}'::jsonb),
    ARRAY[@metaKey],
    COALESCE(to_jsonb(@fileId::text), 'null'::jsonb),
    true)
WHERE id = @projectId AND workspace_id = @workspaceId
