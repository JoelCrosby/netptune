import { BoardGroupType } from './view-models/board-group-view-model';

export interface AddBoardGroupRequest {
  name: string;
  boardId: number;
  type?: BoardGroupType;
  sortOrder?: number;
}
