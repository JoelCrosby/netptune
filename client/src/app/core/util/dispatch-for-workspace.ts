import { effect, inject } from '@angular/core';
import { selectCurrentWorkspaceId } from '@core/store/workspaces/workspaces.selectors';
import { Action, Store } from '@ngrx/store';

export function dispatchForWorkspace(actionFactory: () => Action): void {
  const store = inject(Store);
  const workspaceId = store.selectSignal(selectCurrentWorkspaceId);

  effect(() => {
    if (workspaceId() === undefined) {
      return;
    }

    store.dispatch(actionFactory());
  });
}
