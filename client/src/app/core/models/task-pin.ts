import { TaskViewModel } from './view-models/project-task-dto';

export enum TaskPinScope {
  user = 0,
  board = 1,
  project = 2,
  workspace = 3,
}

export interface TaskPin {
  id: number;
  taskId: number;
  scope: TaskPinScope;
  scopeEntityId: number;
  scopeName: string;
  sortOrder: number;
  canUnpin: boolean;
  createdAt: string;
  createdByUserId?: string | null;
}

export interface PinnedTask {
  task: TaskViewModel;
  pins: TaskPin[];
}

export interface CreateTaskPinRequest {
  taskId: number;
  scope: TaskPinScope;
  scopeEntityId?: number | null;
}

export interface TaskPinOrder {
  id: number;
  sortOrder: number;
}

export interface ReorderTaskPinsRequest {
  items: TaskPinOrder[];
}
