import { Board } from '@app/core/models/board';
import { AsyncEntityState } from '@core/entity/async-entity-state';
import { createEntityAdapter } from '@ngrx/entity';

export const adapter = createEntityAdapter<Board>();

export const initialState: BoardsState = adapter.getInitialState({
  loading: true,
  loaded: false,
  loadingCreate: false,
});

export interface BoardsState extends AsyncEntityState<Board> {
  currentBoard?: Board;
}
