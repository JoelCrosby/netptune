import { AsyncEntityState } from '@core/util/entity/async-entity-state';
import { createEntityAdapter } from '@ngrx/entity';
import { BoardViewModel } from '@core/models/view-models/board-view-model';
import { AppUser } from '@core/models/appuser';
import { BoardViewGroup } from '@core/models/view-models/board-view';
import { BoardGroupViewModel } from '@core/models/view-models/board-group-view-model';
import {
  AsyncDataState,
  initialAsyncDataState,
} from '@core/types/async-data-state';

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
  isInlineDirty: false,
  users: [],
  selectedUsers: [],
  selectedTasks: [],
  deleteState: initialAsyncDataState(),
});

export interface BoardGroupsState extends AsyncEntityState<BoardViewGroup> {
  board?: BoardViewModel;
  users: AppUser[];
  selectedUsers: AppUser[];
  currentBoardGroup?: BoardGroupViewModel;
  isDragging: boolean;
  isInlineActive: boolean;
  isInlineDirty: boolean;
  inlineActive?: number;
  onlyFlagged?: boolean;
  selectedTasks: number[];
  searchTerm?: string | null;
  inlineTaskContent?: string | null;
  deleteState: AsyncDataState;
}

export interface BorderFilterParams {
  users?: string[];
  tags?: string[];
  flagged?: boolean;
  term?: string | null;
}
