import { BoardGroupType } from './board-group';

export interface AddBoardGroupRequest {
  name: string;
  boardId: number;
  type?: BoardGroupType;
  sortOrder?: number;
}
