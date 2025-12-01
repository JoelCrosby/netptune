import { Action } from '@ngrx/store';

export const redirectAction = <V extends Action = Action>(action: V) => {
  action.type = `[Hub]${action.type}`;

  return action;
};

export const hubAction = <T>(action: T & { type: string }): T => ({
  ...action,
  type: `[Hub]${action.type}`,
});
