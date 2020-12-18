import { AsyncEntityState } from '@core/util/entity/async-entity-state';
import { createEntityAdapter } from '@ngrx/entity';
import { BoardViewModel } from '@core/models/view-models/board-view-model';

export const adapter = createEntityAdapter<BoardViewModel>();

export const initialState: BoardsState = adapter.getInitialState({
  loading: true,
  loaded: false,
  loadingCreate: false,
});

export type BoardsState = AsyncEntityState<BoardViewModel>;
