-- Writes one branding key of a board's meta document for BoardRepository.SetBrandingFile.
-- See set_workspace_branding_file.sql for why jsonb_set is used.
UPDATE boards
SET meta_info = jsonb_set(
    COALESCE(meta_info, '{}'::jsonb),
    ARRAY[@metaKey],
    COALESCE(to_jsonb(@fileId::text), 'null'::jsonb),
    true)
WHERE id = @boardId AND workspace_id = @workspaceId
