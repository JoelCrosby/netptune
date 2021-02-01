import { BoardMeta, BoardType } from '../board';

export interface BoardViewModel {
  id: number;
  name: string;
  identifier: string;
  projectId: number;
  projectName: string;
  boardType: BoardType;
  createdAt: Date;
  updatedAt?: Date;
  ownerUsername: string;
  metaInfo: BoardMeta;
}
