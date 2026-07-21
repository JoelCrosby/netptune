import { Page, PageQuery } from '../pagination';
import { EntityType } from '../entity-type';
import { ActivityType } from './activity-view-model';

export interface AuditLogViewModel {
  id: number;
  occurredAt: Date;
  userId: string;
  userDisplayName: string;
  userPictureUrl?: string;
  type: ActivityType;
  entityType: EntityType;
  entityId?: number;
  workspaceSlug?: string;
  projectSlug?: string;
  boardSlug?: string;
  meta?: Record<string, unknown>;
}

export type AuditLogPage = Page<AuditLogViewModel>;

export interface AuditActivityPoint {
  date: string;
  count: number;
}

export interface AuditLogFilter extends PageQuery {
  userId?: string;
  entityType?: EntityType;
  activityType?: ActivityType;
  from?: string;
  to?: string;
}
