import { BoardViewModel } from './board-view-model';
import { BoardGroup } from '../board-group';

export interface BoardGroupsViewModel {
  board: BoardViewModel;
  groups: BoardGroup[];
}
