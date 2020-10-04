import { BoardViewModel } from './board-view-model';
import { BoardGroup } from '../board-group';
import { AppUser } from '../appuser';

export interface BoardGroupsViewModel {
  board: BoardViewModel;
  groups: BoardGroup[];
  users: AppUser[];
}
