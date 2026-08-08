import { Injectable, Signal, WritableSignal, signal } from '@angular/core';
import { allRefreshScopes, RefreshScope } from '@core/models/refresh-scope';

type ScopeVersions = Record<RefreshScope, WritableSignal<number>>;

function createVersions(): ScopeVersions {
  const versions = allRefreshScopes.map((scope) => [scope, signal(0)]);

  return Object.fromEntries(versions) as ScopeVersions;
}

@Injectable({ providedIn: 'root' })
export class WorkspaceRefreshService {
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
  }
}
