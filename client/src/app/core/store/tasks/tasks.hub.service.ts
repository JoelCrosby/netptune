import { Injectable, computed, inject, signal } from '@angular/core';
import { WorkspaceRefreshService } from '@core/services/workspace-refresh.service';
import { setCurrentGroupId } from '@core/store/hub-context/hub-context.actions';
import { WorkspaceEventsService } from '@core/sse/workspace-events.service';
import { Store } from '@ngrx/store';

/** Tracks which realtime group the open view belongs to, and when its task lists went stale. */
@Injectable({
  providedIn: 'root',
})
export class ProjectTasksHubService {
  private store = inject(Store);
  private workspaceEvents = inject(WorkspaceEventsService);
  private workspaceRefresh = inject(WorkspaceRefreshService);

  private readonly localReloads = signal(0);

  /** Task lists reload for a write made here and for one that arrived from elsewhere. */
  readonly updateVersion = computed(() => {
    return this.localReloads() + this.workspaceRefresh.version('tasks')();
  });

  addToGroup(groupId: string) {
    this.store.dispatch(setCurrentGroupId({ groupId }));
    this.workspaceEvents.joinGroup(groupId);
  }

  leaveGroup() {
    this.store.dispatch(setCurrentGroupId({ groupId: null }));
    this.workspaceEvents.leaveGroup();
  }

  reloadTaskList() {
    this.localReloads.update((version) => version + 1);
  }
}
