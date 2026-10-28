import {
  AiReviewFindingCategory,
  AiReviewReadiness,
} from '@core/models/ai-review';
import { BadgeColor } from '@static/components/badge/badge.component';

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
};

export const aiReviewErrorMessage = (
  code: string | null | undefined
): string => {
  const fallback = $localize`:Why a stored review failed, when the reason is unknown:The review failed.`;

  return (code && errorMessages[code]) || fallback;
};
