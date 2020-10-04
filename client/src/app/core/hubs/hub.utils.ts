import { Action } from '@ngrx/store';
import { FunctionIsNotAllowed } from '@ngrx/store/src/models';

export function redirectAction<V extends Action = Action>(
  action: V &
    FunctionIsNotAllowed<
      V,
      'Functions are not allowed to be dispatched. Did you forget to call the action creator function?'
    >
) {
  action.type = `[Hub]${action.type}`;

  return action;
}

export function hubAction<T>(action: T & { type: string }): T {
  return { ...action, type: `[Hub]${action.type}` };
}
