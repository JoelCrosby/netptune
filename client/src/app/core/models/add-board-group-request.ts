export interface AddBoardGroupRequest {
  name: string;
  boardId: number;
  statusId?: number | null;
  sortOrder?: number;
}
