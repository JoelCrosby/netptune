export interface BuildInfo {
  gitHash?: string;
  gitHashShort?: string;
  gitHubRef?: string;
  buildNumber?: string;
  runId?: string;
}

export const initialState: MetaState = {};

export interface MetaState {
  buildInfo?: BuildInfo;
}
