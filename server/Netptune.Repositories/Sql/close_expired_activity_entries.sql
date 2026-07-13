-- `notified_at IS NULL`, and NOT `is_open`, is what means "the sweeper has not finished with this entry".
-- ActivityHandler legitimately closes a burst whose window has expired to free the unique-index slot before
-- it has been notified. A claim keyed on is_open would never see those entries again and would drop their
-- notifications in silence.
UPDATE activity_entries
SET is_open = FALSE
  , notified_at = NOW()
  , updated_at = NOW()
WHERE id IN (
    SELECT id
    FROM activity_entries
    WHERE window_expires_at <= NOW()
      AND notified_at IS NULL
    ORDER BY window_expires_at
    LIMIT @limit
    FOR UPDATE SKIP LOCKED
)
RETURNING *;
