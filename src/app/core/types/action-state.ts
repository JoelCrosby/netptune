import { HttpErrorResponse } from '@angular/common/http';

export interface ActionState {
  loading: boolean;
  error?: HttpErrorResponse;
}

export const DefaultActionState: ActionState = {
  loading: false,
};
