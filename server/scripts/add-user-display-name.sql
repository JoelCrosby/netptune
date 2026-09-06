-- Display name as a stored generated column, so projections and sorts read one
-- column instead of repeating the first/last/username fallback in every query.
-- Mirrors AppUser.GetDisplayName: a blank first and last name falls back to the
-- username. EnsureCreated builds this from the model on a fresh database; this
-- script is for an existing one.

ALTER TABLE users
    ADD COLUMN IF NOT EXISTS display_name text
    GENERATED ALWAYS AS (
        CASE
            WHEN btrim(coalesce(firstname, '')) = '' AND btrim(coalesce(lastname, '')) = ''
                THEN user_name
            ELSE coalesce(firstname, '') || ' ' || coalesce(lastname, '')
        END
    ) STORED;
