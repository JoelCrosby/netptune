-- Writes one branding key of a workspace's meta document for WorkspaceRepository.SetBrandingFile.
-- jsonb_set is used rather than replacing the document so that concurrent writes to sibling keys
-- serialise on the row instead of overwriting each other.
UPDATE workspaces
SET meta_info = jsonb_set(
    COALESCE(meta_info, '{}'::jsonb),
    ARRAY[@metaKey],
    COALESCE(to_jsonb(@fileId::text), 'null'::jsonb),
    true)
WHERE id = @workspaceId
