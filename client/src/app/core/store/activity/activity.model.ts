import { ActivityViewModel } from '@core/models/view-models/activity-view-model';

export const initialState: ActivityState = {
  activities: [],
  loading: true,
  loaded: false,
};

export interface ActivityState {
  activities: ActivityViewModel[];
  loading: boolean;
  loaded: boolean;
}
