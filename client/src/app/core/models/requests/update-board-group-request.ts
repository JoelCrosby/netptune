export interface UpdateBoardGroupRequest {
  boardGroupId: number;
  name?: string;
  sortOrder?: number;
  statusId?: number | null;
  clearStatus?: boolean;
}
