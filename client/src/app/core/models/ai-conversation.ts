import { AiProvider } from './ai-credential';

export enum AiMessageRole {
  user = 0,
  assistant = 1,
  tool = 2,
}

export enum AiStreamEventType {
  textDelta = 0,
  toolStarted = 1,
  toolCompleted = 2,
  turnCompleted = 3,
  error = 4,
  conversationStarted = 5,
  changeSetProposed = 6,
  entitiesReferenced = 7,
  replyReset = 8,
  stopped = 9,
  historyCompacted = 10,
  usageUpdated = 11,
  turnUsage = 12,
  questionAsked = 13,
}

export interface AiStreamEvent {
  type: AiStreamEventType;
  text?: string;
  toolName?: string;
  message?: string;
  conversationId?: string;
  changeSetId?: string;
  references?: AiEntityReference[];
  droppedMessages?: number;
  usage?: AiTokenUsage;
  question?: AiQuestion;
}

export interface AiQuestionOption {
  label: string;
  description?: string | null;
}

export interface AiQuestion {
  id: string;
  text: string;
  header?: string | null;
  options: AiQuestionOption[];
  multiSelect: boolean;
}

export interface AiQuestionAnswer {
  questionId: string;
  selectedLabels: string[];
  text?: string | null;
}

export interface AiTokenUsage {
  inputTokens: number;
  outputTokens: number;
  cacheReadTokens: number;
  cacheCreationTokens: number;
  cost: number;
}

export interface AiConversation {
  id: string;
  title: string;
  provider: AiProvider;
  model: string;
  requestedModel?: string | null;
  lastMessageAt: string;
  messageCount: number;
  usage: AiTokenUsage;
}

export interface AiEntityReference {
  type: string;
  id: string;
  name: string;
}

export interface AiMessage {
  id: number;
  sequence: number;
  role: AiMessageRole;
  text?: string;
  toolNames: string[];
  references: AiEntityReference[];
  changeSetId?: string | null;
  question?: AiQuestion | null;
  answer?: AiQuestionAnswer | null;
  createdAt: string;
}

export interface AiConversationDetail {
  conversation: AiConversation;
  messages: AiMessage[];
  pendingChangeSet?: AiChangeSet | null;
}

export enum AiChangeSetStatus {
  pending = 0,
  applied = 1,
  discarded = 2,
  partiallyApplied = 3,
}

export enum AiChangeValidationStatus {
  valid = 0,
  invalid = 1,
}

export enum AiChangeApplyStatus {
  pending = 0,
  applied = 1,
  skipped = 2,
  failed = 3,
}

export enum AiChangeValueKind {
  text = 0,
  date = 1,
  user = 2,
  status = 3,
  tag = 4,
  task = 5,
  sprint = 6,
}

export interface AiChangeValue {
  display: string;
  id?: string | null;
  color?: string | null;
  pictureUrl?: string | null;
}

export interface AiChangeField {
  name: string;
  before?: string | null;
  after?: string | null;
  kind: AiChangeValueKind;
  beforeValues?: AiChangeValue[] | null;
  afterValues?: AiChangeValue[] | null;
}

export interface AiProposedChange {
  id: number;
  sequence: number;
  toolName: string;
  entityType: string;
  entityId?: number | null;
  refKey?: string | null;
  summary: string;
  fields: AiChangeField[];
  validationStatus: AiChangeValidationStatus;
  validationMessage?: string | null;
  applyStatus: AiChangeApplyStatus;
  applyError?: string | null;
  appliedEntityId?: number | null;
  entitySystemId?: string | null;
  undoneAt?: string | null;
  canUndo: boolean;
}

export interface AiChangeSet {
  id: string;
  conversationId: string;
  status: AiChangeSetStatus;
  appliedAt?: string | null;
  undoneAt?: string | null;
  changes: AiProposedChange[];
}
