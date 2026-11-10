import {
  ScmConnectionCheck,
  ScmConnectionCheckStatus,
} from '@core/models/source-control';
import {
  StatusChecklistItem,
  StatusChecklistStatus,
} from '@static/components/status-checklist/status-checklist.component';

const checklistStatuses: Record<
  ScmConnectionCheckStatus,
  StatusChecklistStatus
> = {
  [ScmConnectionCheckStatus.passed]: 'passed',
  [ScmConnectionCheckStatus.warning]: 'warning',
  [ScmConnectionCheckStatus.failed]: 'failed',
  [ScmConnectionCheckStatus.skipped]: 'skipped',
};

export interface ConnectionCheckCounts {
  warnings: number;
  failures: number;
}

export function toChecklistItems(
  checks: readonly ScmConnectionCheck[]
): StatusChecklistItem[] {
  return checks.map((check) => {
    return {
      key: check.key,
      status: checklistStatuses[check.status],
      title: check.title,
      detail: check.detail,
    };
  });
}

export function countCheckProblems(
  checks: readonly ScmConnectionCheck[]
): ConnectionCheckCounts {
  const warnings = checks.filter(
    (check) => check.status === ScmConnectionCheckStatus.warning
  ).length;
  const failures = checks.filter(
    (check) => check.status === ScmConnectionCheckStatus.failed
  ).length;

  return { warnings, failures };
}
