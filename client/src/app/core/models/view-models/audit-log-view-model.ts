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

export interface AuditLogPage {
  items: AuditLogViewModel[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface AuditLogFilter {
  userId?: string;
  entityType?: EntityType;
  activityType?: ActivityType;
  from?: string;
  to?: string;
  page?: number;
  pageSize?: number;
}
