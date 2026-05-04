import { HttpErrorResponse } from '@angular/common/http';
import { ActivityViewModel } from '@core/models/view-models/activity-view-model';

export const initialState: ActivityState = {
  activities: [],
  loading: true,
  loaded: false,
  nextCursor: undefined,
  pageSize: 50,
  loadingMore: false,
};

export interface ActivityState {
  activities: ActivityViewModel[];
  loading: boolean;
  loaded: boolean;
  loadingError?: HttpErrorResponse | Error;
  nextCursor?: string;
  pageSize: number;
  loadingMore: boolean;
}
