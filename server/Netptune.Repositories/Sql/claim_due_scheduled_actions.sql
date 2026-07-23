WITH candidates AS (
    SELECT id
    FROM scheduled_automation_actions
    WHERE is_deleted = FALSE
      AND (
          (status = @pendingStatus AND execute_at <= @dueBefore)
          OR (status = @processingStatus AND lease_expires_at <= @dueBefore)
      )
    ORDER BY execute_at, id
    FOR UPDATE SKIP LOCKED
    LIMIT @batchSize
)
UPDATE scheduled_automation_actions AS scheduled_action
SET status = @processingStatus,
    claim_id = @claimId,
    lease_expires_at = @leaseExpiresAt,
    attempt_count = scheduled_action.attempt_count + 1,
    updated_at = @dueBefore
FROM candidates
WHERE scheduled_action.id = candidates.id
RETURNING scheduled_action.id;
