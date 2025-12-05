import { HttpErrorResponse } from '@angular/common/http';

export interface AsyncDataState {
  loading: boolean;
  error?: HttpErrorResponse | Error;
}

export const initialAsyncDataState = (): AsyncDataState => ({
  loading: false,
});
