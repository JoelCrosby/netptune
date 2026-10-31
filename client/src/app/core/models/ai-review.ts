import { AiProvider } from '@core/models/ai-credential';
import { EntityType } from '@core/models/entity-type';
import { RelationCategory } from '@core/models/relation-type';
import { TaskPriority } from '@core/enums/task-priority';

export enum AiReviewRunStatus {
  succeeded = 0,
  failed = 1,
}

export enum AiReviewFindingCategory {
  gap = 0,
  assumption = 1,
  ambiguity = 2,
  scope = 3,
  overlap = 4,
  testability = 5,
}

export enum AiReviewFindingStatus {
  open = 0,
  accepted = 1,
  dismissed = 2,
}

export enum AiReviewReferenceRole {
  related = 0,
  overlaps = 1,
  created = 2,
}

export enum AiReviewReadiness {
  unreviewed = 0,
  needsWork = 1,
  ready = 2,
  stale = 3,
}

export enum AiReviewDecision {
  accept = 0,
  dismiss = 1,
  reopen = 2,
}

export enum AiReviewStreamEventType {
  progress = 0,
  completed = 1,
  failed = 2,
  tool = 3,
}

export interface AiReviewToolCall {
  name: string;
  label: string;
  isError: boolean;
  truncated: boolean;
}

export interface AiReviewRun {
  id: number;
  subjectType: EntityType;
  subjectId: number;
  status: AiReviewRunStatus;
  provider: AiProvider;
  model: string;
  summary?: string | null;
  errorCode?: string | null;
  startedAt: string;
  createdAt: string;
  runByUserId?: string | null;
  runByDisplayName?: string | null;
  runByPictureUrl?: string | null;
  openCount: number;
  acceptedCount: number;
  dismissedCount: number;
  toolCalls?: AiReviewToolCall[] | null;
}

export interface AiReviewReference {
  role: AiReviewReferenceRole;
  entityType: EntityType;
  entityId: number;
  systemId?: string | null;
  name?: string | null;
}

export interface AiReviewEditSuggestion {
  kind: 'edit';
  field: string;
  find: string | null;
  replace: string;
}

export interface AiReviewSplitTask {
  name: string;
  description?: string | null;
}

export interface AiReviewSplitSuggestion {
  kind: 'split';
  tasks: AiReviewSplitTask[];
}

export interface AiReviewLinkSuggestion {
  kind: 'link';
  systemId: string;
  taskId: number;
  category: RelationCategory;
}

export interface AiReviewSuggestedTag {
  name: string;
  isNew: boolean;
}

export interface AiReviewMetadataSuggestion {
  kind: 'metadata';
  tags: AiReviewSuggestedTag[];
  estimateValue?: number | null;
  priority?: TaskPriority | null;
}

export type AiReviewSuggestion =
  | AiReviewEditSuggestion
  | AiReviewSplitSuggestion
  | AiReviewLinkSuggestion
  | AiReviewMetadataSuggestion;

export interface AiReviewAppliedEdit {
  field: string;
  before: string;
  after: string;
  edited: boolean;
}

export type AiReviewAppliedChange =
  AiReviewAppliedEdit | { kind: 'split' | 'link' | 'metadata' };

export const isAppliedEdit = (
  change: AiReviewAppliedChange | null | undefined
): change is AiReviewAppliedEdit => {
  return !!change && 'before' in change;
};

export interface AiReviewFinding {
  id: number;
  sequence: number;
  targetType: EntityType;
  targetId: number;
  category: AiReviewFindingCategory;
  title: string;
  explanation: string;
  quote?: string | null;
  suggestion?: AiReviewSuggestion | null;
  status: AiReviewFindingStatus;
  decidedByUserId?: string | null;
  decidedByDisplayName?: string | null;
  decidedAt?: string | null;
  dismissReason?: string | null;
  appliedChange?: AiReviewAppliedChange | null;
  references: AiReviewReference[];
}

export interface AiReviewRunDetail {
  run: AiReviewRun;
  findings: AiReviewFinding[];
}

export interface AiReviewMention {
  runId: number;
  subjectType: EntityType;
  subjectId: number;
  subjectSystemId?: string | null;
  subjectName?: string | null;
  findingId: number;
  findingTitle: string;
  category: AiReviewFindingCategory;
}

export interface AiReviewOrigin {
  runId: number;
  findingTitle: string;
  parentTaskId: number;
  parentSystemId?: string | null;
  parentName?: string | null;
  acceptedByDisplayName?: string | null;
  acceptedAt?: string | null;
}

export interface AiReviewLatest {
  current?: AiReviewRunDetail | null;
  latestFailure?: AiReviewRun | null;
  isStale: boolean;
  readiness: AiReviewReadiness;
  openCount: number;
  mentionedIn: AiReviewMention[];
  origin?: AiReviewOrigin | null;
}

export interface AiReviewStreamEvent {
  type: AiReviewStreamEventType;
  characters?: number | null;
  runId?: number | null;
  message?: string | null;
  tool?: string | null;
}

export interface DecideAiReviewFindingRequest {
  decision: AiReviewDecision;
  reason?: string | null;
  replace?: string | null;
  revert?: boolean;
  splitTasks?: AiReviewSplitTask[] | null;
  tags?: string[] | null;
  applyEstimate?: boolean;
  applyPriority?: boolean;
}
