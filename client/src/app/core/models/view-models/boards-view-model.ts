import { BoardViewModel } from './board-view-model';

export interface BoardsViewModel {
  projectId: number;
  projectName: string;
  boards: BoardViewModel[];
}
