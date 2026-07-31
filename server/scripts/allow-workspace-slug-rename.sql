-- Workspace identifiers are now editable from workspace settings.
-- EF Core refuses to modify a property that backs an alternate key, so slug uniqueness
-- moves from a unique constraint to a unique index. The constraint name is looked up
-- rather than assumed, since it depends on the naming convention in force when it was created.
DO $$
DECLARE
    slug_constraint text;
BEGIN
    SELECT con.conname
    INTO slug_constraint
    FROM pg_constraint con
    JOIN pg_class rel ON rel.oid = con.conrelid
    JOIN pg_namespace nsp ON nsp.oid = rel.relnamespace
    WHERE rel.relname = 'workspaces'
      AND nsp.nspname = current_schema()
      AND con.contype = 'u'
      AND con.conkey = ARRAY[(
          SELECT att.attnum
          FROM pg_attribute att
          WHERE att.attrelid = rel.oid
            AND att.attname = 'slug'
      )];

    IF slug_constraint IS NOT NULL THEN
        EXECUTE format('ALTER TABLE workspaces DROP CONSTRAINT %I', slug_constraint);
    END IF;
END $$;

CREATE UNIQUE INDEX IF NOT EXISTS ix_workspaces_slug ON workspaces (slug);
