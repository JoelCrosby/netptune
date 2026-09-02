export interface ReassignTasksRequest {
  boardId: string;
  taskIds: number[];
  assigneeIds: string[];
}
