import { BoardType } from '../board';
import { Project } from '../project';

export interface BoardViewModel {
  id: number;
  name: string;
  identifier: string;
  projectId: number;
  project: Project;
  boardType: BoardType;
  createdAt: Date;
  UpdatedAt?: Date;
  ownerUsername: string;
}
