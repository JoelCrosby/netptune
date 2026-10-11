SELECT
      page.id
    , page.kind
    , page.title
    , page.slug
    , page.status
    , LEFT(page.body, @ExcerptSourceLength) AS excerpt_source
    , COALESCE(char_length(page.body) > @ExcerptSourceLength, false) AS is_body_truncated
    , (
        SELECT count(*)
        FROM regexp_split_to_table(COALESCE(page.body, ''), '\s+') AS word
        WHERE word <> ''
      ) AS word_count
    , COALESCE(page.updated_at, page.created_at) AS updated_at
    , editor.display_name AS editor_display_name
    , editor.picture_url AS editor_picture_url
    , (
        SELECT count(*)
        FROM pages child
        WHERE child.parent_id = page.id
          AND child.workspace_id = page.workspace_id
          AND child.is_deleted = false
          AND child.is_template = false
      ) AS child_count
FROM pages page
LEFT JOIN users editor ON editor.id = COALESCE(page.modified_by_user_id, page.created_by_user_id)
WHERE page.workspace_id = @WorkspaceId
  AND page.parent_id = @ParentId
  AND page.is_deleted = false
  AND page.is_template = false
ORDER BY page.sort_order, page.id;

SELECT
      page_tag.page_id
    , tag.id
    , tag.name
FROM page_tags page_tag
INNER JOIN tags tag ON tag.id = page_tag.tag_id AND tag.is_deleted = false
INNER JOIN pages page ON page.id = page_tag.page_id
WHERE page.workspace_id = @WorkspaceId
  AND page.parent_id = @ParentId
  AND page.is_deleted = false
  AND page.is_template = false
ORDER BY tag.name;
