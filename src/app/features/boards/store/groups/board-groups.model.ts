import { BoardGroup } from '@app/core/models/board-group';
import { AsyncEntityState } from '@core/entity/async-entity-state';
import { createEntityAdapter } from '@ngrx/entity';

export function sortBySortOrder(a: BoardGroup, b: BoardGroup): number {
  return a.sortOrder - b.sortOrder;
}

export const adapter = createEntityAdapter<BoardGroup>({
  sortComparer: sortBySortOrder,
});

export const initialState: BoardGroupsState = adapter.getInitialState({
  loading: false,
  loaded: false,
  loadingCreate: false,
  isDragging: false,
  isInlineActive: false,
});

export interface BoardGroupsState extends AsyncEntityState<BoardGroup> {
  currentBoardGroup?: BoardGroup;
  isDragging: boolean;
  inlineActive?: number;
}
