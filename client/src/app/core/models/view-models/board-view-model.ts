import { BoardMeta, BoardType } from '../board';
import { AssigneeViewModel } from './board-view';

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
  taskCount: number;
  lastUpdated: Date;
  assignees: AssigneeViewModel[];
}
