import { TaskPriority } from '@core/enums/task-priority';
import { EntityType } from '@core/models/entity-type';

export enum AutomationTriggerType {
  taskStatusChanged = 0,
  taskUnassignedFor = 1,
  taskChanged = 2,
  taskDueDateApproaching = 3,
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

export enum AssigneeChangeMode {
  addedOrRemoved = 0,
  added = 1,
  removed = 2,
}

export enum AutomationActionType {
  notifyTaskAssignees = 0,
  flagTask = 1,
  updateTask = 2,
  addComment = 3,
  deleteTask = 4,
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
  statusId?: number | null;
  assigneeChangeMode?: AssigneeChangeMode | null;
  conditions?: AutomationFieldCondition[] | null;
  conditionGroup?: AutomationConditionGroup | null;
  durationDays?: number | null;
}

export interface AutomationAction {
  id?: number;
  type: AutomationActionType;
  sortOrder?: number;
  message?: string | null;
  comment?: string | null;
  flagName?: string | null;
  flagDescription?: string | null;
  statusId?: number | null;
  priority?: TaskPriority | null;
  delayAmount?: number | null;
  delayUnit?: AutomationDelayUnit | null;
}

export interface AutomationRule {
  id: number;
  workspaceId: number;
  name: string;
  isEnabled: boolean;
  trigger: AutomationTrigger;
  actions: AutomationAction[];
  createdAt: Date;
  updatedAt?: Date | null;
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
  name: string;
  isEnabled: boolean;
  trigger: AutomationTrigger;
  actions: AutomationActionRequest[];
}

export interface AutomationActionRequest {
  type: AutomationActionType;
  message?: string | null;
  comment?: string | null;
  flagName?: string | null;
  flagDescription?: string | null;
  statusId?: number | null;
  priority?: TaskPriority | null;
  delayAmount?: number | null;
  delayUnit?: AutomationDelayUnit | null;
}

export interface AutomationRuleListItem extends AutomationRule {
  lastRun?: AutomationRun | null;
}
