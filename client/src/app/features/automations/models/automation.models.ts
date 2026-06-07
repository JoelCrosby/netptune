import { TaskStatus } from '@core/enums/project-task-status';
import { EntityType } from '@core/models/entity-type';

export enum AutomationTriggerType {
  taskStatusChanged = 0,
  taskUnassignedFor = 1,
}

export enum AutomationActionType {
  notifyTaskAssignees = 0,
  flagTask = 1,
}

export enum AutomationRunStatus {
  succeeded = 0,
  failed = 1,
  skipped = 2,
}

export interface AutomationTrigger {
  type: AutomationTriggerType;
  status?: TaskStatus | null;
  durationDays?: number | null;
}

export interface AutomationAction {
  id?: number;
  type: AutomationActionType;
  sortOrder?: number;
  message?: string | null;
  flagName?: string | null;
  flagDescription?: string | null;
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
  flagName?: string | null;
  flagDescription?: string | null;
}

export interface AutomationRuleListItem extends AutomationRule {
  lastRun?: AutomationRun | null;
}
