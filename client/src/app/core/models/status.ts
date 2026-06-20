import { EntityType } from './entity-type';

export enum StatusCategory {
  backlog = 0,
  todo = 1,
  active = 2,
  done = 3,
  inactive = 4,
}

export const statusCategoryLabels: Record<StatusCategory, string> = {
  [StatusCategory.backlog]: 'Backlog',
  [StatusCategory.todo]: 'Todo',
  [StatusCategory.active]: 'Active',
  [StatusCategory.done]: 'Done',
  [StatusCategory.inactive]: 'Inactive',
};

export const statusCategoryOptions = [
  StatusCategory.backlog,
  StatusCategory.todo,
  StatusCategory.active,
  StatusCategory.done,
  StatusCategory.inactive,
];

export interface Status {
  id: number;
  workspaceId: number;
  entityType: EntityType;
  name: string;
  key: string;
  description?: string | null;
  color?: string | null;
  sortOrder: number;
  category: StatusCategory;
  isSystem: boolean;
}

export interface CreateStatusRequest {
  entityType: EntityType;
  name: string;
  description?: string | null;
  color?: string | null;
  category: StatusCategory;
}

export interface UpdateStatusRequest extends CreateStatusRequest {
  id: number;
}

export interface ReorderStatusesRequest {
  entityType: EntityType;
  statusIds: number[];
}
