import { BoardViewModel } from './board-view-model';
import { AppUser } from '../appuser';
import { BoardGroupViewModel } from './board-group-view-model';

export interface BoardGroupsViewModel {
  board: BoardViewModel;
  groups: BoardGroupViewModel[];
  users: AppUser[];
}
