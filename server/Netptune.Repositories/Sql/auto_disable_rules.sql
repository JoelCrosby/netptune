UPDATE automation_rules AS rule
SET is_enabled = false
  , auto_disabled_at = @disabledAt
  , auto_disabled_reason = disabled.reason
FROM unnest(@ruleIds, @reasons) AS disabled(id, reason)
WHERE rule.id = disabled.id;
