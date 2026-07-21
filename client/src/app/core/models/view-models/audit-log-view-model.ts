import { PageQuery } from '../pagination';
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
  summary: string;
  meta?: Record<string, unknown>;
}

export interface AuditLogReferenceViewModel {
  role: string;
  entityType: string;
  entityId: string;
}

export interface AuditLogDetailViewModel extends AuditLogViewModel {
  eventId: string;
  eventKey: string;
  schemaVersion: number;
  subjectType?: string;
  subjectId?: string;
  subjectSequence?: number;
  recordedAt: string;
  correlationId?: string;
  causationEventId?: string;
  ipAddress?: string;
  userAgent?: string;
  retentionClass: string;
  references: AuditLogReferenceViewModel[];
}

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
