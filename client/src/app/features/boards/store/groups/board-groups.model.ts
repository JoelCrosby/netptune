import { BoardGroup } from '@core/models/board-group';
import { AsyncEntityState } from '@core/util/entity/async-entity-state';
import { createEntityAdapter } from '@ngrx/entity';
import { BoardViewModel } from '@core/models/view-models/board-view-model';
import { AppUser } from '@core/models/appuser';
import { BoardViewGroup } from '@core/models/view-models/board-view';

export const sortBySortOrder = (a: BoardViewGroup, b: BoardViewGroup): number =>
  a.sortOrder - b.sortOrder;

export const adapter = createEntityAdapter<BoardViewGroup>({
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
  selectedTasks: [],
});

export interface BoardGroupsState extends AsyncEntityState<BoardViewGroup> {
  board?: BoardViewModel;
  users: AppUser[];
  selectedUsers: AppUser[];
  currentBoardGroup?: BoardGroup;
  isDragging: boolean;
  inlineActive?: number;
  onlyFlagged?: boolean;
  selectedTasks: number[];
  searchTerm?: string;
}
