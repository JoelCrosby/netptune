import { BoardMeta } from '../board';

export interface UpdateBoardRequest {
  id: number;
  name?: string;
  identifier?: string;
  meta?: BoardMeta;
}
