import {
  Injectable,
  Signal,
  WritableSignal,
  inject,
  signal,
} from '@angular/core';
import { allRefreshScopes, RefreshScope } from '@core/models/refresh-scope';
import * as groupsActions from '@core/store/groups/board-groups.actions';
import * as tasksActions from '@core/store/tasks/tasks.actions';
import { selectDetailTask } from '@core/store/tasks/tasks.selectors';
import { Store } from '@ngrx/store';

type ScopeVersions = Record<RefreshScope, WritableSignal<number>>;

function createVersions(): ScopeVersions {
  const versions = allRefreshScopes.map((scope) => [scope, signal(0)]);

  return Object.fromEntries(versions) as ScopeVersions;
}

@Injectable({ providedIn: 'root' })
export class WorkspaceRefreshService {
  private readonly store = inject(Store);

  private readonly versions = createVersions();

  private readonly detailTask = this.store.selectSignal(selectDetailTask);

  version(scope: RefreshScope): Signal<number> {
    return this.versions[scope];
  }

  refreshAll() {
    this.refresh(allRefreshScopes);
  }

  refresh(scopes: Iterable<RefreshScope>) {
    const requested = new Set(scopes);

    if (!requested.size) return;

    for (const scope of requested) {
      this.versions[scope].update((version) => version + 1);
    }

    this.reloadStores(requested);
  }

  private reloadStores(scopes: ReadonlySet<RefreshScope>) {
    const touchesTasks = scopes.has('tasks') || scopes.has('boardGroups');

    if (touchesTasks) {
      this.reloadTaskViews();
    }
  }

  private reloadTaskViews() {
    this.store.dispatch(groupsActions.loadBoardGroups.init());
    this.reloadDetailTask();
  }

  private reloadDetailTask() {
    const task = this.detailTask();

    if (!task) return;

    this.store.dispatch(
      tasksActions.loadTaskDetails.init({ systemId: task.systemId })
    );
  }
}
