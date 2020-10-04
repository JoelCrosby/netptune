import { BoardMeta } from '../board';

export interface AddBoardRequest {
  name: string;
  identifier: string;
  projectId: number;
  meta?: BoardMeta;
}
