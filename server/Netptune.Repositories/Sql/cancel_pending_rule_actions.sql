UPDATE scheduled_automation_actions AS scheduled_action
SET status = @cancelledStatus
  , processed_at = @cancelledAt
  , claim_id = NULL
  , lease_expires_at = NULL
  , modified_by_user_id = rule.execution_user_id
  , updated_at = @cancelledAt
FROM automation_rules AS rule
WHERE rule.id = scheduled_action.automation_rule_id
  AND scheduled_action.automation_rule_id = ANY(@ruleIds)
  AND scheduled_action.status IN (@pendingStatus, @processingStatus)
RETURNING scheduled_action.automation_rule_id AS "Value";
