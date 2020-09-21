import { BoardGroup } from '@core/models/board-group';
import { AsyncEntityState } from '@core/util/entity/async-entity-state';
import { createEntityAdapter } from '@ngrx/entity';
import { BoardViewModel } from '@core/models/view-models/board-view-model';
import { AppUser } from '@core/models/appuser';

export function sortBySortOrder(a: BoardGroup, b: BoardGroup): number {
  return a.sortOrder - b.sortOrder;
}

export const adapter = createEntityAdapter<BoardGroup>({
  sortComparer: sortBySortOrder,
});

export const initialState: BoardGroupsState = adapter.getInitialState({
  loading: true,
  loaded: false,
  loadingCreate: false,
  isDragging: false,
  isInlineActive: false,
  users: [],
  selectedUsers: [],
});

export interface BoardGroupsState extends AsyncEntityState<BoardGroup> {
  board?: BoardViewModel;
  users: AppUser[];
  selectedUsers: AppUser[];
  currentBoardGroup?: BoardGroup;
  isDragging: boolean;
  inlineActive?: number;
  onlyFlagged?: boolean;
}
