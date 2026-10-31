import {
  AiReviewFindingCategory,
  AiReviewReadiness,
  AiReviewToolCall,
} from '@core/models/ai-review';
import { numberFormat } from '@core/util/locale';
import { BadgeColor } from '@static/components/badge/badge.component';
import { StepTrailItem } from '@static/components/step-trail/step-trail.component';

export const aiReviewCategoryLabels: Record<AiReviewFindingCategory, string> = {
  [AiReviewFindingCategory.gap]: $localize`:Review finding category for something required that is missing:Gap`,
  [AiReviewFindingCategory.assumption]: $localize`:Review finding category for something the task relies on without stating it:Assumption`,
  [AiReviewFindingCategory.ambiguity]: $localize`:Review finding category for wording that can be read more than one way:Ambiguity`,
  [AiReviewFindingCategory.scope]: $localize`:Review finding category for a task that is too large or bundles unrelated work:Scope`,
  [AiReviewFindingCategory.overlap]: $localize`:Review finding category for a task that overlaps another one:Overlap`,
  [AiReviewFindingCategory.testability]: $localize`:Review finding category for a task with no way to tell it is done:Testability`,
};

export const aiReviewCategoryColors: Record<
  AiReviewFindingCategory,
  BadgeColor
> = {
  [AiReviewFindingCategory.gap]: 'warn',
  [AiReviewFindingCategory.assumption]: 'pending',
  [AiReviewFindingCategory.ambiguity]: 'pending',
  [AiReviewFindingCategory.scope]: 'info',
  [AiReviewFindingCategory.overlap]: 'info',
  [AiReviewFindingCategory.testability]: 'primary',
};

export const aiReviewReadinessLabels: Record<AiReviewReadiness, string> = {
  [AiReviewReadiness.unreviewed]: $localize`:Readiness of a task that has never been reviewed:Not reviewed`,
  [AiReviewReadiness.needsWork]: $localize`:Readiness of a task whose review has open findings:Needs work`,
  [AiReviewReadiness.ready]: $localize`:Readiness of a task whose review has no open findings:Ready`,
  [AiReviewReadiness.stale]: $localize`:Readiness of a task that changed after its review:Changed since review`,
};

export const aiReviewReadinessColors: Record<AiReviewReadiness, BadgeColor> = {
  [AiReviewReadiness.unreviewed]: 'neutral',
  [AiReviewReadiness.needsWork]: 'warn',
  [AiReviewReadiness.ready]: 'success',
  [AiReviewReadiness.stale]: 'pending',
};

const errorMessages: Record<string, string> = {
  timed_out: $localize`:Why a stored review failed:The assistant ran out of time.`,
  provider_error: $localize`:Why a stored review failed:The assistant provider returned an error.`,
  unreadable_reply: $localize`:Why a stored review failed:The assistant's reply could not be read.`,
  output_limit: $localize`:Why a stored review failed:The assistant's reply grew too long and was cut off.`,
  empty_reply: $localize`:Why a stored review failed:The assistant replied without any review text.`,
  refused: $localize`:Why a stored review failed:The assistant declined to review this item.`,
  tool_rounds: $localize`:Why a stored review failed:The assistant used up its rounds of tool calls before finishing.`,
  tool_budget: $localize`:Why a stored review failed:What the assistant read grew too large to finish the review.`,
  spend_cap: $localize`:Why a stored review failed:The workspace reached its monthly assistant spend cap.`,
};

export const aiReviewErrorMessage = (
  code: string | null | undefined
): string => {
  const fallback = $localize`:Why a stored review failed, when the reason is unknown:The review failed.`;

  return (code && errorMessages[code]) || fallback;
};

type ToolCallGroup =
  'files' | 'code' | 'pullRequests' | 'repositories' | 'web' | 'workspace';

const toolCallGroups: Record<string, ToolCallGroup> = {
  read_repository_file: 'files',
  search_repository_code: 'code',
  get_pull_request: 'pullRequests',
  list_repositories: 'repositories',
  list_repository_files: 'repositories',
  list_repository_commits: 'repositories',
  web_search: 'web',
  web_fetch: 'web',
  read_web_document: 'web',
};

const toolCallGroupOrder: ToolCallGroup[] = [
  'files',
  'code',
  'pullRequests',
  'repositories',
  'web',
  'workspace',
];

const toolCallCount = numberFormat();

const toolCallGroupLabels: Record<ToolCallGroup, (count: string) => string> = {
  files: (count) => {
    return $localize`:How many repository files a review read. COUNT is a number:Files read: ${count}:COUNT:`;
  },
  code: (count) => {
    return $localize`:How many code searches a review ran. COUNT is a number:Code searches: ${count}:COUNT:`;
  },
  pullRequests: (count) => {
    return $localize`:How many pull requests a review read. COUNT is a number:Pull requests read: ${count}:COUNT:`;
  },
  repositories: (count) => {
    return $localize`:How many repository listings a review made, of repositories, files or commits. COUNT is a number:Repository listings: ${count}:COUNT:`;
  },
  web: (count) => {
    return $localize`:How many web searches and page reads a review made. COUNT is a number:Web lookups: ${count}:COUNT:`;
  },
  workspace: (count) => {
    return $localize`:How many reads of tasks, pages and other workspace records a review made. COUNT is a number:Workspace lookups: ${count}:COUNT:`;
  },
};

export const aiReviewToolCallSummary = (calls: AiReviewToolCall[]): string => {
  const counts = new Map<ToolCallGroup, number>();

  for (const call of calls) {
    const group = toolCallGroups[call.name] ?? 'workspace';

    counts.set(group, (counts.get(group) ?? 0) + 1);
  }

  return toolCallGroupOrder
    .filter((group) => counts.has(group))
    .map((group) => {
      const count = toolCallCount.format(counts.get(group) ?? 0);

      return toolCallGroupLabels[group](count);
    })
    .join(' · ');
};

export const aiReviewToolSteps = (
  calls: AiReviewToolCall[]
): StepTrailItem[] => {
  return calls.map((call) => ({ label: call.label, failed: call.isError }));
};
