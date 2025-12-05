import { HttpErrorResponse } from '@angular/common/http';

export interface BuildInfo {
  gitHash?: string;
  gitHashShort?: string;
  gitHubRef?: string;
  buildNumber?: string;
  runId?: string;
}

export const initialState: MetaState = {
  loading: true,
};

export interface MetaState {
  buildInfo?: BuildInfo;
  loading: boolean;
  loadingError?: HttpErrorResponse | Error;
}
