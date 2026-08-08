import { Injectable, computed, inject, signal } from '@angular/core';
import { WorkspaceRefreshService } from '@core/services/workspace-refresh.service';
import { WorkspaceEventsService } from '@core/sse/workspace-events.service';

/** Tracks which realtime group the open view belongs to, and when its task lists went stale. */
@Injectable({
  providedIn: 'root',
})
export class ProjectTasksHubService {
  private workspaceEvents = inject(WorkspaceEventsService);
  private workspaceRefresh = inject(WorkspaceRefreshService);

  private readonly localReloads = signal(0);

  private readonly groupId = signal<string | null>(null);

  readonly onlineUserIds = this.workspaceEvents.onlineUserIds;

  /** The board or project that task writes from the open view are addressed to. */
  readonly currentGroupId = this.groupId.asReadonly();

  /** Task lists reload for a write made here and for one that arrived from elsewhere. */
  readonly updateVersion = computed(() => {
    return this.localReloads() + this.workspaceRefresh.version('tasks')();
  });

  addToGroup(groupId: string) {
    this.groupId.set(groupId);
    this.workspaceEvents.joinGroup(groupId);
  }

  leaveGroup() {
    this.groupId.set(null);
    this.workspaceEvents.leaveGroup();
  }

  reloadTaskList() {
    this.localReloads.update((version) => version + 1);
  }
}
