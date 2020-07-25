import { BoardType } from '../board';

export interface BoardViewModel {
  id: number;
  name: string;
  identifier: string;
  projectId: number;
  boardType: BoardType;
  createdAt: Date;
  UpdatedAt?: Date;
  ownerUsername: string;
}
