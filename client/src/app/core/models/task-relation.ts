import { RelationCategory } from './relation-type';
import { StatusCategory } from './status';

export interface RelatedTask {
  id: number;
  systemId: string;
  name: string;
  statusName: string;
  statusColor?: string | null;
  statusCategory: StatusCategory;
}

export interface TaskRelation {
  id: number;
  relationTypeId: number;
  relationTypeName: string;
  relationTypeKey: string;
  relationTypeColor?: string | null;
  relationTypeCategory: RelationCategory;
  label: string;
  isSource: boolean;
  relatedTask: RelatedTask;
}

export interface CreateTaskRelationRequest {
  sourceSystemId: string;
  targetSystemId: string;
  relationTypeId: number;
}
