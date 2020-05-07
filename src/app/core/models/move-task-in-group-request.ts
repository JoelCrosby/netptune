import { TaskViewModel } from './view-models/project-task-dto';

export interface MoveTaskInGroupRequest {
  taskId: number;
  tasks: TaskViewModel[];
  newGroupId: number;
  oldGroupId: number;
  sortOrder?: number;
  previousIndex: number;
  currentIndex: number;
}
