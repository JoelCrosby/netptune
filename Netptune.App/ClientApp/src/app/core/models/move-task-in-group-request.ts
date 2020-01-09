export interface MoveTaskInGroupRequest {
  taskId: number;
  newGroupId: number;
  oldGroupId: number;
  sortOrder: number;
}
