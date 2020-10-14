export interface MoveTasksToGroupRequest {
  boardId: string;
  taskIds: number[];
  newGroupId: number;
}
