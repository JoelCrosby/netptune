import { HttpErrorResponse } from '@angular/common/http';

export interface ActionState {
  loading: boolean;
  error?: HttpErrorResponse;
}

export const DEFAULT_ACTION_STATE: ActionState = {
  loading: false,
};
