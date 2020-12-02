export interface ReassignTasksRequest {
  boardId: string;
  taskIds: number[];
  assigneeId: string;
}
