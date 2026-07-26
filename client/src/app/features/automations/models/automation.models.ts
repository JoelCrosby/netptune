import { TaskPriority } from '@core/enums/task-priority';
import { EstimateType } from '@core/enums/estimate-type';
import { EntityType } from '@core/models/entity-type';
import { WorkspaceRole } from '@core/enums/workspace-role';

export enum AutomationTriggerType {
  taskUnassignedFor = 1,
  taskChanged = 2,
  taskDueDateApproaching = 3,
  taskCreated = 4,
  taskOverdue = 5,
  taskHasNoDueDate = 6,
  taskInactiveFor = 7,
  sprintStarted = 8,
  sprintCompleted = 9,
  sprintEndingSoon = 10,
  taskBlocked = 11,
  taskUnblocked = 12,
  subtasksCompleted = 13,
}

export enum TaskChangeField {
  name = 0,
  description = 1,
  status = 2,
  assignees = 3,
  owner = 4,
  priority = 5,
  estimate = 6,
  dueDate = 7,
  tags = 8,
  startDate = 9,
  sprint = 10,
  boardGroup = 11,
}

export enum AutomationDateUpdateMode {
  absolute = 0,
  relativeDays = 1,
  relativeBusinessDays = 2,
  clear = 3,
}

export interface AutomationDateUpdate {
  mode: AutomationDateUpdateMode;
  date?: string | null;
  offset?: number | null;
}

export enum AutomationConditionOperator {
  any = 0,
  equals = 1,
  notEquals = 2,
  contains = 3,
  isEmpty = 4,
  isNotEmpty = 5,
  added = 6,
  removed = 7,
}

export interface AutomationFieldCondition {
  field: TaskChangeField;
  operator: AutomationConditionOperator;
  value?: string | null;
}

export enum AutomationConditionGroupOperator {
  all = 0,
  any = 1,
  none = 2,
}

export interface AutomationConditionGroup {
  operator: AutomationConditionGroupOperator;
  conditions: AutomationFieldCondition[];
  groups: AutomationConditionGroup[];
}

export enum AutomationActionType {
  notifyTaskAssignees = 0,
  flagTask = 1,
  updateTask = 2,
  addComment = 3,
  deleteTask = 4,
  createTask = 5,
  manageTaskRelation = 6,
}

export enum AutomationRelationOperation {
  add = 0,
  remove = 1,
}

export enum AutomationRelationDirection {
  taskIsSource = 0,
  taskIsTarget = 1,
}

export enum AutomationNotificationRecipient {
  assignees = 0,
  taskOwner = 1,
  triggeringUser = 2,
  specificUsers = 3,
  projectMembers = 4,
  workspaceRoles = 5,
}

export enum AutomationDelayUnit {
  minutes = 0,
  hours = 1,
  days = 2,
}

export enum AutomationRunStatus {
  succeeded = 0,
  failed = 1,
  skipped = 2,
}

export enum AutomationActionResultStatus {
  pending = 0,
  succeeded = 1,
  failed = 2,
  skipped = 3,
  scheduled = 4,
}

export interface AutomationTrigger {
  type: AutomationTriggerType;
  fields?: TaskChangeField[] | null;
  conditionGroup?: AutomationConditionGroup | null;
  durationDays?: number | null;
}

export interface AutomationAction {
  id?: number;
  type: AutomationActionType;
  sortOrder?: number;
  message?: string | null;
  recipients?: AutomationNotificationRecipient[];
  recipientUserIds?: string[];
  recipientRoles?: WorkspaceRole[];
  comment?: string | null;
  flagName?: string | null;
  flagDescription?: string | null;
  statusId?: number | null;
  priority?: TaskPriority | null;
  taskName?: string | null;
  taskDescription?: string | null;
  clearDescription?: boolean;
  ownerId?: string | null;
  clearOwner?: boolean;
  assigneeIds?: string[] | null;
  addTags?: string[];
  removeTags?: string[];
  startDate?: AutomationDateUpdate | null;
  dueDate?: AutomationDateUpdate | null;
  estimateType?: EstimateType | null;
  estimateValue?: number | null;
  clearEstimate?: boolean;
  sprintId?: number | null;
  clearSprint?: boolean;
  boardGroupId?: number | null;
  copyAssignees?: boolean;
  linkRelationTypeId?: number | null;
  relationOperation?: AutomationRelationOperation | null;
  relationDirection?: AutomationRelationDirection | null;
  relationTypeId?: number | null;
  relatedTaskId?: number | null;
  delayAmount?: number | null;
  delayUnit?: AutomationDelayUnit | null;
}

export enum AutomationWarningCode {
  missingProject = 0,
  missingBoard = 1,
  missingSprint = 2,
  missingExecutionUser = 3,
  missingStatus = 4,
  missingBoardGroup = 5,
  missingRelationType = 6,
  missingUser = 7,
  missingTag = 8,
  missingTask = 9,
}

export interface AutomationRuleWarning {
  code: AutomationWarningCode;
  message: string;
  actionId?: number | null;
}

export interface AutomationRule {
  id: number;
  workspaceId: number;
  projectId?: number | null;
  boardId?: number | null;
  sprintId?: number | null;
  name: string;
  isEnabled: boolean;
  autoDisabledAt?: string | null;
  autoDisabledReason?: string | null;
  executionUserId: string | null;
  trigger: AutomationTrigger;
  actions: AutomationAction[];
  createdAt: Date;
  updatedAt?: Date | null;
  warnings: AutomationRuleWarning[];
}

export interface AutomationRun {
  id: number;
  automationRuleId: number;
  entityId?: number | null;
  entityType?: EntityType | null;
  triggerType: AutomationTriggerType;
  status: AutomationRunStatus;
  idempotencyKey: string;
  message?: string | null;
  createdAt: Date;
  actionResults: AutomationActionResult[];
}

export interface AutomationActionResult {
  id: number;
  automationActionId: number;
  actionType: AutomationActionType;
  sortOrder: number;
  status: AutomationActionResultStatus;
  idempotencyKey: string;
  startedAt?: Date | null;
  completedAt?: Date | null;
  message?: string | null;
  output?: Record<string, unknown> | null;
}

export interface AutomationRuleRequest {
  projectId?: number | null;
  boardId?: number | null;
  sprintId?: number | null;
  name: string;
  isEnabled: boolean;
  executionUserId: string;
  trigger: AutomationTrigger;
  actions: AutomationActionRequest[];
}

export interface AutomationActionRequest {
  type: AutomationActionType;
  message?: string | null;
  recipients?: AutomationNotificationRecipient[];
  recipientUserIds?: string[];
  recipientRoles?: WorkspaceRole[];
  comment?: string | null;
  flagName?: string | null;
  flagDescription?: string | null;
  statusId?: number | null;
  priority?: TaskPriority | null;
  taskName?: string | null;
  taskDescription?: string | null;
  clearDescription?: boolean;
  ownerId?: string | null;
  clearOwner?: boolean;
  assigneeIds?: string[] | null;
  addTags?: string[];
  removeTags?: string[];
  startDate?: AutomationDateUpdate | null;
  dueDate?: AutomationDateUpdate | null;
  estimateType?: EstimateType | null;
  estimateValue?: number | null;
  clearEstimate?: boolean;
  sprintId?: number | null;
  clearSprint?: boolean;
  boardGroupId?: number | null;
  copyAssignees?: boolean;
  linkRelationTypeId?: number | null;
  relationOperation?: AutomationRelationOperation | null;
  relationDirection?: AutomationRelationDirection | null;
  relationTypeId?: number | null;
  relatedTaskId?: number | null;
  delayAmount?: number | null;
  delayUnit?: AutomationDelayUnit | null;
}

export interface AutomationRuleSummary {
  ruleCount: number;
  enabledCount: number;
  recentFailureCount: number;
}

export interface AutomationRuleListItem extends AutomationRule {
  lastRun?: AutomationRun | null;
}

export interface AutomationConditionExplanation {
  field: TaskChangeField;
  operator: AutomationConditionOperator;
  value: string | null;
  actualValue: string | null;
  isMatch: boolean;
  isEvaluable: boolean;
}

export interface AutomationConditionGroupExplanation {
  operator: AutomationConditionGroupOperator;
  isMatch: boolean;
  conditions: AutomationConditionExplanation[];
  groups: AutomationConditionGroupExplanation[];
}

export interface AutomationDryRunAction {
  actionId: number;
  type: AutomationActionType;
  hasEffect: boolean;
  message: string | null;
  recipientUserIds: string[];
  includeProjectMembers: boolean;
  recipientRoles: WorkspaceRole[];
  comment: string | null;
  flagName: string | null;
  updatedFields: string[];
  createdTaskName: string | null;
  relationOperation: AutomationRelationOperation | null;
  relationTypeId: number | null;
  relatedTaskId: number | null;
  delayMinutes: number | null;
}

export interface AutomationManualRun {
  ruleId: number;
  taskCount: number;
}

export interface AutomationDryRun {
  ruleId: number;
  ruleName: string;
  isEnabled: boolean;
  triggerType: AutomationTriggerType;
  taskId: number;
  taskName: string;
  scopeMatches: boolean;
  triggerMatches: boolean;
  triggerIsEvaluable: boolean;
  conditionsMatch: boolean;
  hasUnevaluableConditions: boolean;
  conditionGroup: AutomationConditionGroupExplanation | null;
  actions: AutomationDryRunAction[];
}
