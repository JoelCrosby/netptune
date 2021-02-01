import { BoardsViewModel } from '@core/models/view-models/boards-view-model';

export const initialState: BoardsState = {
  boards: [],
  loading: true,
  loaded: false,
  loadingCreate: false,
};

export interface BoardsState {
  boards: BoardsViewModel[];
  loading: boolean;
  loaded: boolean;
  loadingCreate: boolean;
}
