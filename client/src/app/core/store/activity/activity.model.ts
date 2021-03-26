import { ActivityViewModel } from '@core/models/view-models/activity-view-model';

export const initialState: ActivityState = {
  loading: true,
};

export interface ActivityState {
  activities?: ActivityViewModel[];
  loading: boolean;
}
