import { HttpErrorResponse } from '@angular/common/http';
import { BoardsViewModel } from '@core/models/view-models/boards-view-model';
import {
  AsyncDataState,
  initialAsyncDataState,
} from '@core/types/async-data-state';

export const initialState: BoardsState = {
  boards: [],
  loading: true,
  loaded: false,
  loadingCreate: false,
  deleteState: initialAsyncDataState(),
};

export interface BoardsState {
  boards: BoardsViewModel[];
  loading: boolean;
  loadingError?: HttpErrorResponse | Error;
  loaded: boolean;
  loadingCreate: boolean;
  deleteState: AsyncDataState;
}
