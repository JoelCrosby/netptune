import {
  Injectable,
  Signal,
  WritableSignal,
  inject,
  signal,
} from '@angular/core';
import { allRefreshScopes, RefreshScope } from '@core/models/refresh-scope';
import * as groupsActions from '@core/store/groups/board-groups.actions';
import * as sprintsActions from '@core/store/sprints/sprints.actions';
import {
  selectSprintDetail,
  selectSprintsFilter,
  selectSprintsLoaded,
} from '@core/store/sprints/sprints.selectors';
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

  private readonly sprintsLoaded = this.store.selectSignal(selectSprintsLoaded);
  private readonly sprintsFilter = this.store.selectSignal(selectSprintsFilter);
  private readonly sprintDetail = this.store.selectSignal(selectSprintDetail);
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

    const touchesSprints = scopes.has('sprints');

    if (touchesSprints) {
      this.reloadSprints();
    }
  }

  /* The board groups effect ignores the action off a board route, so it is safe to always ask. */
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

  /* Dispatching without the stored filter would reset it to the unfiltered list. */
  private reloadSprints() {
    if (this.sprintsLoaded()) {
      this.store.dispatch(
        sprintsActions.loadSprints.init({ filter: this.sprintsFilter() })
      );
    }

    const detail = this.sprintDetail();

    if (!detail) return;

    this.store.dispatch(
      sprintsActions.loadSprintDetail.init({ sprintId: detail.id })
    );
  }
}
