UPDATE automation_rules AS rule
SET is_enabled = false
  , auto_disabled_at = @disabledAt
  , auto_disabled_reason = disabled.reason
  , auto_disabled_reason_code = disabled.code
  , auto_disabled_reason_arguments = ARRAY(SELECT jsonb_array_elements_text(disabled.arguments::jsonb))
FROM unnest(@ruleIds, @reasons, @codes, @arguments) AS disabled(id, reason, code, arguments)
WHERE rule.id = disabled.id;
