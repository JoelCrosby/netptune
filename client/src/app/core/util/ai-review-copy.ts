import {
  AiReviewFindingCategory,
  AiReviewReadiness,
  AiReviewToolCall,
} from '@core/models/ai-review';
import { numberFormat } from '@core/util/locale';
import { BadgeColor } from '@static/components/badge/badge.component';
import { StepLogItem } from '@static/components/step-log/step-log.component';

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

export const aiReviewRunningLabel = $localize`:Tooltip and accessible label of the mark shown on a task while an assistant review of it is running:Review running`;

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

const countFormat = numberFormat();

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
      const count = countFormat.format(counts.get(group) ?? 0);

      return toolCallGroupLabels[group](count);
    })
    .join(' · ');
};

export interface AiReviewUnreadSource {
  label: string;
  reason: string;
}

const unreadReasons = {
  failed: $localize`:Shown beside a source a review could not read, because the tool call failed:failed`,
  truncated: $localize`:Shown beside a source a review read only part of, because what came back was too large:cut short`,
};

// What the review could not read in full. It changes how much the findings are
// worth, so it is reported next to them rather than left in the step list.
export const aiReviewUnreadSources = (
  calls: AiReviewToolCall[]
): AiReviewUnreadSource[] => {
  return calls
    .filter((call) => call.isError || call.truncated)
    .map((call) => ({
      label: call.label,
      reason: call.isError ? unreadReasons.failed : unreadReasons.truncated,
    }));
};

// Each whole sentence is one message. $localize cannot evaluate ICU, so the
// plural is a ternary; fr/de/es share English's one/other split, and a locale
// with more plural categories would need these moved into a template ICU.
export const aiReviewUnreadSourcesLabel = (count: number): string => {
  if (count === 1) {
    return $localize`:Opens the list of sources a review could not read in full, when there is one:1 source was not read in full`;
  }

  const sources = countFormat.format(count);

  return $localize`:Opens the list of sources a review could not read in full. COUNT is a count greater than one:${sources}:COUNT: sources were not read in full`;
};

export const aiReviewToolSteps = (calls: AiReviewToolCall[]): StepLogItem[] => {
  const items: StepLogItem[] = [];

  // Calls made in one batch share the note written before them, so it shows once,
  // above the first — the same order a running review streams them in.
  for (const [index, call] of calls.entries()) {
    const previousNote = index > 0 ? calls[index - 1].note : null;
    const note = call.note;

    if (note && note !== previousNote) {
      items.push({ kind: 'note', text: note, state: 'done' });
    }

    items.push({
      kind: 'step',
      text: call.label,
      state: call.isError ? 'failed' : 'done',
    });
  }

  return items;
};
