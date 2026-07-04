import { EntityType } from './entity-type';

export enum StatusCategory {
  new = 0,
  backlog = 1,
  todo = 2,
  active = 3,
  done = 4,
  inactive = 5,
}

export const statusCategoryLabels: Record<StatusCategory, string> = {
  [StatusCategory.new]: 'new',
  [StatusCategory.backlog]: 'Backlog',
  [StatusCategory.todo]: 'Todo',
  [StatusCategory.active]: 'Active',
  [StatusCategory.done]: 'Done',
  [StatusCategory.inactive]: 'Inactive',
};

export const statusCategoryOptions = [
  StatusCategory.new,
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
