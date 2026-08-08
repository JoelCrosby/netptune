import {
  Injectable,
  Signal,
  WritableSignal,
  inject,
  signal,
} from '@angular/core';
import { allRefreshScopes, RefreshScope } from '@core/models/refresh-scope';
import * as groupsActions from '@core/store/groups/board-groups.actions';
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
  }
}
